using System;
using UnityEngine;

namespace HumanEye
{
    public enum IntentKind { Part, Concept, Action, OpenQuestion, Help, Greeting }

    /// Curated local Q&A for "Ask Niko" (keyword/regex intents, offline). Answers quote StoryboardCore cues by id;
    /// any extra wording is SupplementalAdaptive text and is never added to the dialogue ledger.
    [CreateAssetMenu(menuName = "Human Eye/Niko Knowledge")]
    public class NikoKnowledge : ScriptableObject
    {
        [Serializable]
        public class Intent
        {
            public string id;
            public IntentKind kind;
            [Tooltip("Case-insensitive regular expression matched against the normalised question.")]
            public string pattern;
            public string target;             // part id or whitelisted action id
            public string[] cues;             // already-taught source cues quoted as the answer
            public int availableFromChapter;  // index 0 = Scene02
            [TextArea] public string supplemental; // optional added sentence (SupplementalAdaptive)
            [TextArea] public string notYet;       // reply before the topic is taught (never spoils future cues)
        }

        public Intent[] intents;
        public string[] suggestions;
        [TextArea] public string fallback;
    }
}
