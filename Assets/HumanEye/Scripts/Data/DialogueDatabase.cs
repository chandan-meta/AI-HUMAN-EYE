using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HumanEye
{
    /// The single authoritative dialogue database: 66 StoryboardCore cues generated from
    /// Data/DialogueLedger.json (itself extracted verbatim from HUMAN EYE.docx). Read-only at runtime.
    [CreateAssetMenu(menuName = "Human Eye/Dialogue Database")]
    public class DialogueDatabase : ScriptableObject
    {
        [Serializable]
        public class Cue
        {
            public string id, scene, speaker, type, layer, text, filename, visualCue, completionEvent;
            public int sequence;
            public string[] segments;
            public AudioClip clip;
            [Tooltip("Mouth keys from the clip's character alignment (Builder.MouthKeys): time (s), jaw opening and lip rounding, 0..1. Empty = audio-driven lip sync.")]
            public float[] mouthTimes, mouthOpen, mouthRound;
            public bool IsPrompt => type == "PromptVO";
        }

        [Serializable] public class Ledger { public string source; public Cue[] cues; }

        public Cue[] cues;
        [Tooltip("Supplemental lines, never ledger cues (Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv): quiz questions, feedback, the ciliary and Free Explore lines.")]
        public Cue[] extra = new Cue[0];
        Dictionary<string, Cue> byId;

        public Cue Get(string id)
        {
            if (byId == null || byId.Count != cues.Length + extra.Length)
            {
                byId = new Dictionary<string, Cue>();
                foreach (var c in cues.Concat(extra)) byId[c.id] = c;
            }
            return id != null && byId.TryGetValue(id, out var cue) ? cue : null;
        }
    }
}
