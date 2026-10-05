using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HumanEye
{
    public class NikoAnswer
    {
        public string text, action, part;
        public string[] cues;            // StoryboardCore cues quoted/replayed (never paraphrased)
        public bool supplemental, unsupported;
    }

    /// "Ask Niko" — LOCAL CURATED Q&A (keyword/regex intents, works offline). This is not a generative AI.
    /// Answers quote already-taught storyboard lines, map requests to whitelisted actions validated against the
    /// current state, and admit limits instead of inventing science. Free text never runs code or marks completion.
    public class NikoGuide : MonoBehaviour
    {
        public NikoKnowledge knowledge;
        public ChapterRunner runner;
        public Stage stage;
        public Narrator narrator;
        public DialogueDatabase dialogue;

        public string Question { get; private set; }
        public NikoAnswer Answer { get; private set; }
        public bool Thinking { get; private set; }
        public event Action Changed;
        int token;

        static readonly Regex QuestionWord = new(@"^(what|why|how|which|where|when|who|does|do|is|are|can|could|will|would|should)\b");
        static readonly Regex ShowWord = new(@"\b(show|where|point|go to|look at|find|zoom to|take me)\b");

        public static string Normalise(string s)
        {
            s = (s ?? "").ToLowerInvariant().Replace('’', '\'');
            s = Regex.Replace(s, @"[^a-z0-9' ]+", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s.Length > 200 ? s.Substring(0, 200) : s;
        }

        public static NikoKnowledge.Intent Match(NikoKnowledge k, string raw)
        {
            var q = Normalise(raw);
            if (q.Length == 0) return null;
            bool question = QuestionWord.IsMatch(q) || raw.Contains("?");
            var kinds = question
                ? new[] { IntentKind.OpenQuestion, IntentKind.Concept, IntentKind.Part, IntentKind.Greeting, IntentKind.Help }
                : new[] { IntentKind.Action, IntentKind.OpenQuestion, IntentKind.Concept, IntentKind.Part, IntentKind.Greeting, IntentKind.Help };
            foreach (var kind in kinds)
                foreach (var intent in k.intents)
                    if (intent.kind == kind && Regex.IsMatch(q, intent.pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)))
                        return intent;
            return null;
        }

        /// Only lines the learner has already heard (or read) are quoted, so nothing future is spoiled.
        static string[] Taught(string[] cues) => (cues ?? new string[0]).Where(c => SaveSystem.Data.cues.Exists(r => r.id == c)).ToArray();

        public NikoAnswer Resolve(string raw)
        {
            var intent = Match(knowledge, raw);
            if (intent == null) return new NikoAnswer { text = knowledge.fallback, supplemental = true, unsupported = true };
            bool wantsShow = ShowWord.IsMatch(Normalise(raw));
            switch (intent.kind)
            {
                case IntentKind.Action:
                    return stage.CanGuideAction(intent.target, out var reason)
                        ? new NikoAnswer { text = intent.supplemental, action = intent.target, supplemental = true }
                        : new NikoAnswer { text = reason, supplemental = true };
                case IntentKind.Part:
                    if (runner.ExploreSet != null && runner.ExploreSet.Any(e => e.partId == intent.target) && !runner.ExploreDone.Contains(intent.target))
                        return new NikoAnswer { text = intent.supplemental, action = "explore", part = intent.target, supplemental = true };
                    var heard = Taught(intent.cues);
                    if (heard.Length > 0)
                        return new NikoAnswer { text = string.Join(" ", heard.Select(c => dialogue.Get(c).text)), cues = heard, action = wantsShow ? "show" : null, part = intent.target };
                    return new NikoAnswer { text = intent.notYet, supplemental = true };
                case IntentKind.Concept:
                    var known = Taught(intent.cues);
                    if (known.Length > 0)
                        return new NikoAnswer { text = string.Join(" ", known.Select(c => dialogue.Get(c).text)), cues = known, part = intent.target, action = wantsShow && intent.target != null ? "show" : null };
                    return new NikoAnswer { text = intent.notYet, supplemental = true };
                case IntentKind.Help:
                    return new NikoAnswer { text = Status() ?? intent.supplemental, supplemental = true };
                default:
                    return new NikoAnswer { text = intent.supplemental, supplemental = true };
            }
        }

        /// "What now?" grounded in the live state: an open question gets its first (attention) hint, never the answer;
        /// otherwise the current objective plus the simulation's actual readout. Null when nothing is in progress.
        string Status()
        {
            var item = stage.activities.Item;
            if (item != null && item.hints.Length > 0) return "Hint: " + item.hints[0];
            var objective = stage.hud.Objective();
            var readout = stage.Readout();
            if (string.IsNullOrEmpty(objective)) return string.IsNullOrEmpty(readout) ? null : readout;
            return string.IsNullOrEmpty(readout) ? objective : objective + " (" + readout + ")";
        }

        public void Ask(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            int mine = ++token, chapter = runner.Index;
            Question = raw.Trim(); Thinking = true; Answer = null;
            Changed?.Invoke();
            var answer = Resolve(Question);
            // Local answers are immediate; the token/chapter check is what stops a stale (e.g. remote) answer
            // from landing in a later chapter.
            if (mine != token || chapter != runner.Index) return;
            Thinking = false; Answer = answer;
            Changed?.Invoke();
            if (answer.action != null) stage.GuideAction(answer.action, answer.part);
            if (answer.cues != null && answer.action != "explore") runner.RunSide(Replay(answer.cues, mine));
        }

        IEnumerator Replay(string[] cues, int mine)
        {
            foreach (var c in cues)
            {
                if (mine != token) yield break;
                yield return narrator.Play(c, NarrationSource.Answer);
            }
        }

        /// Called on chapter change / panel close so an old answer's replay can't continue into new content.
        public void Invalidate() { token++; Thinking = false; Changed?.Invoke(); }
    }
}
