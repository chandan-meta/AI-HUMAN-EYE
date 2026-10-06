using System;
using UnityEngine;

namespace HumanEye
{
    /// Application = a new situation, offered only once a topic is already Secure (challenge, not repetition).
    public enum QuestionLevel { Recognition, Relationship, Prediction, Application }
    public enum QuestionMode { Choice, TapPart }

    /// SupplementalAdaptive content: newly authored practice items. Never part of the storyboard ledger.
    [CreateAssetMenu(menuName = "Human Eye/Question Bank")]
    public class QuestionBank : ScriptableObject
    {
        [Serializable]
        public class Option
        {
            public string text, partId, misconception;
            public bool correct;
            [TextArea] public string feedback;
        }

        [Serializable]
        public class Item
        {
            public string id, topic, chapter;
            public QuestionLevel level;
            public QuestionMode mode;
            [TextArea] public string prompt;
            public Option[] options;
            public string[] hints;            // tier 1 direct attention, tier 2 comparison, tier 3 worked demonstration
            public string demo;               // stage action replayed for the worked demonstration / prediction trial
            public bool trialAfterAnswer;     // prediction items: run the demo after answering, then compare
            [TextArea] public string correctFeedback;
            public string tag = "SupplementalAdaptive · newly authored practice (not storyboard dialogue)";
        }

        public Item[] items;

        public Item Get(string id) => Array.Find(items, i => i.id == id);
    }
}
