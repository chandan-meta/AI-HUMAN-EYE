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
        public readonly List<string> buttons = new();
        public bool tapParts;
    }

    /// Runs the quick checks at the approved checkpoints. There is no Skip: a wrong answer leads to help
    /// (attention → comparison → worked demonstration), and after three misses the answer is shown with Continue,
    /// so the check can never lock the learner. "Continue anyway" only appears after a real attempt.
    public class ActivityController : MonoBehaviour
    {
        public QuestionBank bank;
        public Stage stage;
        public Narrator narrator;
        public ActivityCard Card { get; private set; }
        public QuestionBank.Item Item { get; private set; }
        public event Action Changed;

        string clicked;

        public void Cancel() { Card = null; Item = null; clicked = null; Changed?.Invoke(); }

        public void Click(string id) { if (Card != null) clicked = id; }

        /// A structure tapped in 3D (or chosen from the part list) while a tap-part question is showing.
        public bool PartTapped(string partId)
        {
            if (Card == null || !Card.tapParts) return false;
            clicked = "part:" + partId; return true;
        }

        IEnumerator WaitClick() { clicked = null; Changed?.Invoke(); yield return new Until(() => clicked != null); }

        // Outcome of the last Ask: answered correctly on the first try without help.
        bool firstTry;

        public IEnumerator Offer(string key, ChapterDefinition chapter)
        {
            var items = Plan(key, chapter);
            QuestionBank.Item previous = null;
            foreach (var item in items)
            {
                if (LearnerModel.Repetitive(previous, firstTry, item)) break;
                Card = new ActivityCard { title = Title(item) };
                yield return Ask(item);
                previous = item;
            }
            Cancel();
        }

        static string Title(QuestionBank.Item item) =>
            item.trialAfterAnswer ? "Predict, then test" : item.level == QuestionLevel.Application ? "Challenge · apply it" : "Quick check";

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
                if (next == null || list.Contains(next) || next.trialAfterAnswer) break;
                list.Add(next); last = next.id;
            }
            return list;
        }

        IEnumerator Ask(QuestionBank.Item item)
        {
            Item = item;
            int mistakes = 0; bool assisted = false;
            firstTry = false;
            // Tap-part questions: the answer structures must be pickable on the model, not only via the option buttons.
            var tappable = item.mode != QuestionMode.TapPart ? new List<AnatomyPart>()
                : item.options.Select(o => stage.Part(o.partId)).Where(p => p && !p.Selectable).ToList();
            foreach (var p in tappable) p.Selectable = true;
            string feedback = null, hint = null;
            stage.sfx.Play("popup", .5f);
            while (true)
            {
                Card = new ActivityCard { title = Card?.title ?? "Quick check", tag = item.tag, prompt = item.prompt, feedback = feedback, hint = hint, tapParts = item.mode == QuestionMode.TapPart };
                foreach (var o in item.options) Card.options.Add((o.text, o.text, false));   // TapPart: buttons are the accessible alternative to tapping the model
                if (mistakes >= 2 && !string.IsNullOrEmpty(item.demo)) Card.buttons.Add("Show me");
                if (mistakes > 0) Card.buttons.Add("Continue anyway");
                yield return WaitClick();

                if (clicked == "Continue anyway") { MarkNeedsPractice(item); break; }
                if (clicked == "Show me") { assisted = true; hint = item.hints.Length > 2 ? item.hints[2] : hint; feedback = null; yield return Demo(item, "hint"); continue; }

                var answer = clicked.StartsWith("part:")
                    ? Array.Find(item.options, o => o.partId == clicked.Substring(5))
                    : Array.Find(item.options, o => o.text == clicked);
                bool correct = answer != null && answer.correct;
                string wrongFeedback = answer?.feedback ?? "That structure isn't one of the choices here — have another look.";

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
                    var right = string.Join(", ", item.options.Where(o => o.correct).Select(o => o.text));
                    Card = new ActivityCard { title = Card.title, tag = item.tag, prompt = item.prompt, feedback = feedback, hint = "Answer: " + right + ". We've noted this topic for a little more practice in the Review panel." };
                    Card.buttons.Add("Continue");
                    yield return WaitClick();
                    break;
                }
                hint = item.hints.Length >= tier ? item.hints[tier - 1] : null;
                if (tier == 2) assisted = true;   // a retry after comparison help counts as assisted
                stage.ShowHint(item, tier);
            }
            stage.ShowHint(null, 0);
            foreach (var p in tappable) p.Selectable = false;
            stage.sfx.Play("popdown", .4f);
            Item = null;
        }

        /// Demo strings are "action" or "action:argument" (e.g. "replayCue:S02_L13"), always a whitelisted stage action.
        IEnumerator Demo(QuestionBank.Item item, string mode)
        {
            var parts = item.demo.Split(':');
            var routine = stage.Run(parts[0], parts.Length > 1 ? parts[1] : mode, 0);
            if (routine != null) yield return routine;
        }

        static void MarkNeedsPractice(QuestionBank.Item item)
        {
            if (LearnerModel.UnassistedCorrect(SaveSystem.Data, item.topic) < LearnerModel.SecureThreshold)
                SaveSystem.AddOnce(SaveSystem.Data.needsPractice, item.topic);
        }
    }
}
