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
        /// A ledger cue, or the supplemental line while it plays: replays and answers queue behind either.
        public bool Busy => Current != null || (Supplemental != null && voice.clip == Supplemental.clip && (voice.isPlaying || pausedVoice));
        public bool Speaking => Busy || voice.isPlaying || pausedVoice;
        public float Level { get; private set; }  // rough loudness for Niko's face
        /// Share of low-formant energy (rounded vowels such as "oo"/"oh" read high, "ee" low): the lip-sync fallback's lip rounding.
        public float Roundness { get; private set; }

        /// A line spoken outside the ledger through this voice (the closing line): its mouth keys while its clip plays.
        public DialogueDatabase.Cue Supplemental { get; set; }

        /// Mouth pose for the playing clip from its keys (Builder.MouthKeys / Builder.AudioKeys); false when the cue has none.
        public bool TryMouth(out float open, out float round)
        {
            open = round = 0;
            var c = Current != null && voice.clip == Current.clip ? Current : Supplemental != null && voice.clip == Supplemental.clip ? Supplemental : null;
            if (c == null || c.mouthTimes == null || c.mouthTimes.Length < 2 || !voice.isPlaying) return false;
            float t = voice.time; var times = c.mouthTimes;
            int i = System.Array.BinarySearch(times, t); if (i < 0) i = ~i - 1;
            if (i < 0) return true;
            if (i >= times.Length - 1) { open = c.mouthOpen[^1]; round = c.mouthRound[^1]; return true; }
            float u = Mathf.InverseLerp(times[i], times[i + 1], t);
            open = Mathf.Lerp(c.mouthOpen[i], c.mouthOpen[i + 1], u); round = Mathf.Lerp(c.mouthRound[i], c.mouthRound[i + 1], u);
            return true;
        }

        public Func<bool> waitForContinue = () => false;
        public event Action Changed;
        public event Action<DialogueDatabase.Cue, NarrationSource, string> Completed;

        int token;
        bool continueRequested;
        bool pausedVoice;
        readonly float[] samples = new float[128], spectrum = new float[256];

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

        public void SetPaused(bool paused)
        {
            if (paused) { pausedVoice |= voice.isPlaying; voice.Pause(); }
            else { voice.UnPause(); pausedVoice = false; }
        }

        /// Cancels the current cue (chapter exit, restart). Waiting callers simply stop with their routine.
        public void Stop()
        {
            token++;
            voice.Stop();
            pausedVoice = false;
            Current = null; Supplemental = null; AwaitingContinue = false; Level = 0;
            Changed?.Invoke();
        }

        void Update()
        {
            if (!voice.isPlaying) { Level = Mathf.MoveTowards(Level, 0, Time.unscaledDeltaTime * 4); Roundness = 0; return; }
            voice.GetOutputData(samples, 0);
            float sum = 0;
            foreach (var s in samples) sum += s * s;
            Level = Mathf.Lerp(Level, Mathf.Clamp01(Mathf.Sqrt(sum / samples.Length) * 6), .5f);
            // 256 bins over half the output rate: low band ~200-800 Hz (rounded-vowel F1/F2), high band ~1-4 kHz ("ee" F2, fricatives)
            voice.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            float bin = AudioSettings.outputSampleRate * .5f / spectrum.Length, low = 0, high = 0;
            if (bin <= 0) return;
            int top = spectrum.Length - 1;   // an 8 kHz output (a headset's hands-free profile) puts 4 kHz past the last bin
            for (int i = Mathf.RoundToInt(200 / bin); i <= Mathf.Min(top, Mathf.RoundToInt(800 / bin)); i++) low += spectrum[i];
            for (int i = Mathf.RoundToInt(1000 / bin); i <= Mathf.Min(top, Mathf.RoundToInt(4000 / bin)); i++) high += spectrum[i];
            Roundness = Mathf.Lerp(Roundness, Mathf.Clamp01((low / Mathf.Max(1e-6f, low + high) - .55f) / .35f), .35f);
        }
    }
}
