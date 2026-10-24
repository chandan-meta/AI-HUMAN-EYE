using NUnit.Framework;
using UnityEngine;

namespace HumanEye.Tests
{
    /// Character alignment (ElevenLabs with-timestamps format) becomes mouth keys: "m" closes, "a" opens, "o" rounds, a
    /// real pause closes the mouth, a short word gap does not.
    public class NikoLipSyncTests
    {
        [Test]
        public void AlignmentBecomesMouthKeys()
        {
            const string json = "{\"characters\":[\"m\",\"a\",\" \",\"o\",\".\",\"b\"]," +
                "\"character_start_times_seconds\":[0.0,0.1,0.3,0.32,0.5,0.9]," +
                "\"character_end_times_seconds\":[0.1,0.3,0.32,0.5,0.9,1.0]}";
            var cue = new DialogueDatabase.Cue();
            HumanEye.EditorTools.Builder.MouthKeys(cue, json);
            Assert.IsNotNull(cue.mouthTimes);
            CollectionAssert.IsOrdered(cue.mouthTimes);
            float At(float t, float[] values) { int i = System.Array.BinarySearch(cue.mouthTimes, t); if (i < 0) i = ~i - 1; return values[Mathf.Clamp(i, 0, values.Length - 1)]; }
            Assert.AreEqual(0, At(.04f, cue.mouthOpen), 1e-4, "m is closed");
            Assert.AreEqual(1, At(.2f, cue.mouthOpen), 1e-4, "a is open");
            Assert.Greater(At(.42f, cue.mouthRound), .5f, "o is rounded");
            Assert.AreEqual(0, At(.6f, cue.mouthOpen), 1e-4, "a 0.4 s pause closes the mouth");
            Assert.AreEqual(0, cue.mouthOpen[cue.mouthOpen.Length - 1], "it ends closed");
            Assert.False(System.Array.Exists(cue.mouthTimes, t => t > .3f && t < .32f), "a 20 ms word gap adds no closing key");
        }

        /// A clip without timing gets keys from its own audio: silence closes the mouth, a loud low tone opens and rounds it, a
        /// loud high tone opens it without rounding.
        [Test]
        public void AudioBecomesMouthKeys()
        {
            const int rate = 48000;
            var samples = new System.Collections.Generic.List<short>();
            void Add(float seconds, float hz) { int n = (int)(seconds * rate); for (int i = 0; i < n; i++) samples.Add((short)(hz > 0 ? 16000 * Mathf.Sin(2 * Mathf.PI * hz * i / rate) : 0)); }
            Add(.3f, 0); Add(.4f, 300); Add(.3f, 0); Add(.4f, 2500); Add(.2f, 0);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "he_mouth_test.wav");
            using (var w = new System.IO.BinaryWriter(System.IO.File.Create(path)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + samples.Count * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(samples.Count * 2); foreach (var s in samples) w.Write(s);
            }
            var cue = new DialogueDatabase.Cue();
            HumanEye.EditorTools.Builder.AudioKeys(cue, path);
            System.IO.File.Delete(path);
            Assert.IsNotNull(cue.mouthTimes);
            CollectionAssert.IsOrdered(cue.mouthTimes);
            float At(float t, float[] values) { int i = System.Array.BinarySearch(cue.mouthTimes, t); if (i < 0) i = ~i - 1; return values[Mathf.Clamp(i, 0, values.Length - 1)]; }
            Assert.Less(At(.15f, cue.mouthOpen), .05f, "leading silence is closed");
            Assert.Greater(At(.5f, cue.mouthOpen), .7f, "the low tone opens the mouth");
            Assert.Greater(At(.5f, cue.mouthRound), .7f, "the low tone rounds the lips");
            Assert.Less(At(.85f, cue.mouthOpen), .05f, "the gap closes it");
            Assert.Greater(At(1.2f, cue.mouthOpen), .7f, "the high tone opens the mouth");
            Assert.Less(At(1.2f, cue.mouthRound), .2f, "the high tone does not round the lips");
            Assert.AreEqual(0, cue.mouthOpen[cue.mouthOpen.Length - 1], "it ends closed");
        }
    }
}
