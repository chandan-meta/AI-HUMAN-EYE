using System;
using System.Collections.Generic;
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
            public bool IsPrompt => type == "PromptVO";
        }

        [Serializable] public class Ledger { public string source; public Cue[] cues; }

        public Cue[] cues;
        Dictionary<string, Cue> byId;

        public Cue Get(string id)
        {
            if (byId == null || byId.Count != cues.Length)
            {
                byId = new Dictionary<string, Cue>();
                foreach (var c in cues) byId[c.id] = c;
            }
            return id != null && byId.TryGetValue(id, out var cue) ? cue : null;
        }
    }
}
