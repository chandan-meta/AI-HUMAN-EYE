using System;
using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public enum NarrationSource { Core, Replay, Answer }

    /// Single narration arbiter: only one cue plays at a time (core VO, prompts, replays and Niko answers queue here).
    /// Completion follows the clip's real length; a missing clip never softlocks — it waits for an explicit
    /// Read & Continue, recorded as "read", never as a fabricated audio completion.
    public class Narrator : MonoBehaviour
    {
        public DialogueDatabase database;
        public AudioSource voice;

        public DialogueDatabase.Cue Current { get; private set; }
        public DialogueDatabase.Cue LastCore { get; private set; }
        public NarrationSource Source { get; private set; }
        public int Segment { get; private set; }
        public bool AwaitingContinue { get; private set; }
        public bool MissingAudio { get; private set; }
        public bool Busy => Current != null;
        public float Level { get; private set; }  // rough loudness for Niko's face

        public Func<bool> waitForContinue = () => false;
        public event Action Changed;
        public event Action<DialogueDatabase.Cue, NarrationSource, string> Completed;

        int token;
        bool continueRequested;
        readonly float[] samples = new float[128];

        public IEnumerator Play(string id, NarrationSource source)
        {
            var cue = database.Get(id);
            if (cue == null) { Debug.LogError("Unknown dialogue cue " + id); yield break; }
            while (Busy) yield return null;
            int mine = ++token;
            Current = cue; Source = source; Segment = 0; continueRequested = false;
            MissingAudio = cue.clip == null;
            AwaitingContinue = MissingAudio;
            float length = MissingAudio ? 0 : cue.clip.length, elapsed = 0;
            if (!MissingAudio) { voice.clip = cue.clip; voice.time = 0; voice.Play(); if (Clock.Paused) voice.Pause(); }
            Changed?.Invoke();
            while (mine == token)
            {
                if (!AwaitingContinue)
                {
                    elapsed += Clock.Delta;
                    if (voice.isPlaying && Clock.FastForward <= 1) elapsed = Mathf.Max(elapsed, voice.time);
                    SetSegment(cue, Mathf.Clamp01(elapsed / Mathf.Max(.01f, length)));
                    if (elapsed >= length)
                    {
                        if (!waitForContinue()) break;
                        AwaitingContinue = true; Changed?.Invoke();
                    }
                }
                else if (continueRequested) break;
                yield return null;
            }
            if (mine != token) yield break; // cancelled by Stop(); no completion is recorded
            voice.Stop();
            var how = MissingAudio ? "read" : "audio";
            if (source == NarrationSource.Core) LastCore = cue;
            Current = null; AwaitingContinue = false; Level = 0;
            Completed?.Invoke(cue, source, how);
            Changed?.Invoke();
        }

        void SetSegment(DialogueDatabase.Cue cue, float progress)
        {
            if (cue.segments == null || cue.segments.Length < 2) return;
            int total = cue.text.Length, acc = 0, index = 0;
            for (; index < cue.segments.Length - 1; index++)
            {
                acc += cue.segments[index].Length;
                if (progress * total < acc) break;
            }
            if (index != Segment) { Segment = index; Changed?.Invoke(); }
        }

        public string SubtitleText => Current == null ? "" :
            Current.segments != null && Current.segments.Length > 1 ? Current.segments[Mathf.Min(Segment, Current.segments.Length - 1)] : Current.text;

        public void Continue() { if (AwaitingContinue) continueRequested = true; }

        public void SetPaused(bool paused) { if (paused) voice.Pause(); else if (Busy) voice.UnPause(); }

        /// Cancels the current cue (chapter exit, restart). Waiting callers simply stop with their routine.
        public void Stop()
        {
            token++;
            voice.Stop();
            Current = null; AwaitingContinue = false; Level = 0;
            Changed?.Invoke();
        }

        void Update()
        {
            if (!voice.isPlaying) { Level = Mathf.MoveTowards(Level, 0, Time.unscaledDeltaTime * 4); return; }
            voice.GetOutputData(samples, 0);
            float sum = 0;
            foreach (var s in samples) sum += s * s;
            Level = Mathf.Lerp(Level, Mathf.Clamp01(Mathf.Sqrt(sum / samples.Length) * 6), .5f);
        }
    }
}
