using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HumanEye
{
    /// What the HUD should draw for the current SupplementalAdaptive activity.
    public class ActivityCard
    {
        public string title, tag, prompt, feedback, hint;
        public readonly List<(string id, string label, bool selected)> options = new();
        public readonly List<string> slots = new();      // Order mode: placed stage labels
        public readonly List<string> buttons = new();
        public bool tapParts;
    }

    /// Runs optional checks between storyboard beats. Never replaces a source explanation, never locks progress:
    /// every card can be skipped or continued, and help moves from attention → comparison → worked demonstration.
    public class ActivityController : MonoBehaviour
    {
        public QuestionBank bank;
        public Stage stage;
        public Narrator narrator;
        public ActivityCard Card { get; private set; }
        public QuestionBank.Item Item { get; private set; }
        public event Action Changed;

        string clicked;
        readonly List<string> placed = new();

        public void Cancel() { Card = null; Item = null; clicked = null; placed.Clear(); Changed?.Invoke(); }

        public void Click(string id) { if (Card != null) clicked = id; }

        /// A structure tapped in 3D (or chosen from the part list) while a tap-part question is showing.
        public bool PartTapped(string partId)
        {
            if (Card == null || !Card.tapParts) return false;
            clicked = "part:" + partId; return true;
        }

        IEnumerator WaitClick() { clicked = null; Changed?.Invoke(); yield return new Until(() => clicked != null); }

        // Outcome of the last Ask: skipped the whole check / answered correctly on the first try without help.
        bool skipped, firstTry;

        /// Goes straight to the first question (no "Try it?" gate); "Skip" on any card skips the rest of the check.
        public IEnumerator Offer(string key, ChapterDefinition chapter)
        {
            var items = Plan(key, chapter);
            QuestionBank.Item previous = null;
            foreach (var item in items)
            {
                if (LearnerModel.Repetitive(previous, firstTry, item)) break;
                Card = new ActivityCard { title = Title(item) };
                yield return Ask(item);
                if (skipped) break;
                previous = item;
            }
            Cancel();
        }

        static string Title(QuestionBank.Item item) =>
            item.mode == QuestionMode.Order ? "Optional: Rebuild the journey" : item.trialAfterAnswer ? "Predict, then test"
            : item.level == QuestionLevel.Application ? "Challenge · apply it" : "Optional quick check";

        List<QuestionBank.Item> Plan(string key, ChapterDefinition chapter)
        {
            var d = SaveSystem.Data;
            if (key.StartsWith("item:")) return bank.items.Where(i => i.id == key.Substring(5)).ToList();
            var topic = key.StartsWith("topic:") ? key.Substring(6) : chapter.topics.FirstOrDefault();
            if (LearnerModel.Status(d, topic, true) == TopicStatus.Secure)
            {
                var challenge = LearnerModel.Challenge(bank, d, topic);
                return challenge == null ? new List<QuestionBank.Item>() : new List<QuestionBank.Item> { challenge };
            }
            var list = new List<QuestionBank.Item>();
            string last = null;
            for (int n = 0; n < 2; n++)
            {
                var next = LearnerModel.Next(bank, d, topic, last);
                if (next == null || list.Contains(next) || next.mode == QuestionMode.Order || next.trialAfterAnswer) break;
                list.Add(next); last = next.id;
            }
            return list;
        }

        IEnumerator Ask(QuestionBank.Item item)
        {
            Item = item;
            int mistakes = 0; bool assisted = false;
            skipped = firstTry = false;
            // Tap-part questions: the answer structures must be pickable on the model, not only via the option buttons.
            var tappable = item.mode != QuestionMode.TapPart ? new List<AnatomyPart>()
                : item.options.Select(o => stage.Part(o.partId)).Where(p => p && !p.Selectable).ToList();
            foreach (var p in tappable) p.Selectable = true;
            placed.Clear();
            var order = item.mode == QuestionMode.Order ? Shuffle(item.options) : item.options.ToList();
            string feedback = null, hint = null;
            while (true)
            {
                Card = new ActivityCard { title = Card?.title ?? "Optional quick check", tag = item.tag, prompt = item.prompt, feedback = feedback, hint = hint, tapParts = item.mode == QuestionMode.TapPart };
                if (item.mode == QuestionMode.Order)
                {
                    foreach (var o in order) Card.options.Add((o.text, o.text, placed.Contains(o.text)));
                    Card.slots.AddRange(placed);
                    if (placed.Count > 0) Card.buttons.Add("Undo");
                    if (placed.Count == item.options.Count(o => o.correct)) Card.buttons.Add("Check");
                }
                else foreach (var o in order) Card.options.Add((o.text, o.text, false));   // TapPart: buttons are the accessible alternative to tapping the model
                if (mistakes >= 2 && !string.IsNullOrEmpty(item.demo)) Card.buttons.Add("Show me");
                Card.buttons.Add(mistakes > 0 ? "Continue anyway" : "Skip");
                yield return WaitClick();

                if (clicked == "Skip") { skipped = true; break; }
                if (clicked == "Continue anyway") { MarkNeedsPractice(item); break; }
                if (clicked == "Show me") { assisted = true; hint = item.hints.Length > 2 ? item.hints[2] : hint; feedback = null; yield return Demo(item, "hint"); continue; }
                if (clicked == "Undo") { if (placed.Count > 0) placed.RemoveAt(placed.Count - 1); continue; }
                if (item.mode == QuestionMode.Order && clicked != "Check")
                {
                    if (!placed.Contains(clicked)) placed.Add(clicked);
                    continue;
                }

                bool correct; string wrongFeedback;
                if (item.mode == QuestionMode.Order) correct = CheckOrder(item, out wrongFeedback);
                else
                {
                    var answer = clicked.StartsWith("part:")
                        ? Array.Find(item.options, o => o.partId == clicked.Substring(5))
                        : Array.Find(item.options, o => o.text == clicked);
                    correct = answer != null && answer.correct;
                    wrongFeedback = answer?.feedback ?? "That structure isn't one of the choices here — have another look.";
                }

                if (item.trialAfterAnswer && !string.IsNullOrEmpty(item.demo))
                {
                    Card = new ActivityCard { title = Card.title, tag = item.tag, prompt = item.prompt, feedback = "Let's test your prediction…" };
                    Changed?.Invoke();
                    yield return Demo(item, "trial");
                }

                if (correct)
                {
                    LearnerModel.Record(SaveSystem.Data, item, true, assisted || mistakes > 0);
                    firstTry = !assisted && mistakes == 0;
                    stage.sfx.Play("confirm", .7f);
                    SaveSystem.Save();
                    Card = new ActivityCard { title = Card.title, tag = item.tag, prompt = item.prompt, feedback = "✓ " + item.correctFeedback };
                    Card.buttons.Add("Next");
                    yield return WaitClick();
                    break;
                }

                mistakes++;
                stage.sfx.Play("deny", .45f);
                LearnerModel.Record(SaveSystem.Data, item, false, assisted);
                int tier = LearnerModel.SupportTier(mistakes);
                feedback = wrongFeedback;
                if (item.trialAfterAnswer) feedback = "The test showed something different. " + feedback;
                if (tier >= 3)
                {
                    MarkNeedsPractice(item);
                    var right = item.mode == QuestionMode.Order
                        ? string.Join(" → ", item.options.Where(o => o.correct).Select(o => o.text))
                        : string.Join(", ", item.options.Where(o => o.correct).Select(o => o.text));
                    Card = new ActivityCard { title = Card.title, tag = item.tag, prompt = item.prompt, feedback = feedback, hint = "Answer: " + right + ". We've noted this topic for a little more practice in the Review panel." };
                    Card.buttons.Add("Continue");
                    yield return WaitClick();
                    break;
                }
                hint = item.hints.Length >= tier ? item.hints[tier - 1] : null;
                if (tier == 2) assisted = true;   // a retry after comparison help counts as assisted
                if (item.mode == QuestionMode.Order) placed.Clear();
                stage.ShowHint(item, tier);
            }
            stage.ShowHint(null, 0);
            foreach (var p in tappable) p.Selectable = false;
            Item = null;
        }

        /// Demo strings are "action" or "action:argument" (e.g. "replayCue:S02_L13"), always a whitelisted stage action.
        IEnumerator Demo(QuestionBank.Item item, string mode)
        {
            var parts = item.demo.Split(':');
            var routine = stage.Run(parts[0], parts.Length > 1 ? parts[1] : mode, 0);
            if (routine != null) yield return routine;
        }

        bool CheckOrder(QuestionBank.Item item, out string feedback)
        {
            var path = item.options.Where(o => o.correct).ToList();
            for (int i = 0; i < placed.Count; i++)
            {
                var o = Array.Find(item.options, x => x.text == placed[i]);
                // Partial understanding: acknowledge the stages already in the right order, then guide the missing link.
                var kept = i == 0 ? "" : $"Your first {i} stage{(i > 1 ? "s are" : " is")} in the right order. ";
                if (!o.correct) { feedback = kept + o.feedback; return false; }
                if (i >= path.Count || path[i].text != placed[i]) { feedback = kept + path[Mathf.Min(i, path.Count - 1)].feedback; return false; }
            }
            feedback = null;
            return placed.Count == path.Count;
        }

        static void MarkNeedsPractice(QuestionBank.Item item)
        {
            if (LearnerModel.UnassistedCorrect(SaveSystem.Data, item.topic) < LearnerModel.SecureThreshold)
                SaveSystem.AddOnce(SaveSystem.Data.needsPractice, item.topic);
        }

        static List<QuestionBank.Option> Shuffle(QuestionBank.Option[] options)
        {
            // Deterministic shuffle (stable between runs) so tests and screenshots are reproducible.
            return options.OrderBy(o => (o.text.Aggregate(7, (h, ch) => h * 31 + ch) & 0x7fffffff) % 97).ThenBy(o => o.text).ToList();
        }
    }
}
