using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    /// The audio overhaul's mixing rules on a bare AudioDirector (no scene): music beds crossfade, a repeated key changes
    /// nothing and a second change inside a crossfade never cuts the loud bed; Niko's voice ducks the music by about 10 dB and releases it; pause freezes music and loops but never the
    /// interface channel; mute silences every source; loops level, stop and all end on StopLoops; ambience fades in and out.
    public class AudioOverhaulTests
    {
        GameObject root;
        AudioClip a, b, tone;
        SettingsData savedSettings;

        [TearDown]
        public void Cleanup()
        {
            if (savedSettings != null) SaveSystem.Data.settings = savedSettings;
            if (root) Object.DestroyImmediate(root);
            foreach (var c in new[] { a, b, tone }) if (c) Object.DestroyImmediate(c);
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator BedsDuckPauseMuteLoopsAndAmbience()
        {
            savedSettings = SaveSystem.Data.settings;
            SaveSystem.Data.settings = new SettingsData { music = .5f, sfx = .8f };
            AudioClip Silent(string n) => AudioClip.Create(n, 32000 * 6, 1, 32000, false);
            a = Silent("bed a"); b = Silent("bed b"); tone = Silent("loop");
            root = new GameObject("Audio overhaul test");
            var d = root.AddComponent<AudioDirector>();
            AudioSource S() { var s = new GameObject("src").AddComponent<AudioSource>(); s.transform.SetParent(root.transform); s.playOnAwake = false; return s; }
            d.music = S(); d.musicB = S(); d.introMusic = S(); d.sfx = S(); d.uiSfx = S(); d.cinematicSfx = S(); d.ambience = S();
            d.narrator = root.AddComponent<Narrator>(); d.narrator.voice = S();
            d.tracks = new[] { new AudioDirector.Track { key = "a", clip = a }, new AudioDirector.Track { key = "b", clip = b }, new AudioDirector.Track { key = "c", clip = tone } };
            d.effects = new[] { new AudioDirector.Sfx { key = "loop", clip = tone }, new AudioDirector.Sfx { key = "amb", clip = tone, gain = -6 }, new AudioDirector.Sfx { key = "ui", clip = tone, bus = AudioDirector.Bus.UI } };

            d.SetMusic("a", .4f);
            yield return new WaitForSecondsRealtime(.7f);
            var bedA = d.musicB.clip == a ? d.musicB : d.music;
            Assert.AreEqual(.5f, bedA.volume, .02f, "the first bed plays at the music setting");
            d.SetMusic("b", .4f);
            yield return new WaitForSecondsRealtime(.15f);
            var bedB = bedA == d.music ? d.musicB : d.music;
            Assert.Greater(bedA.volume, 0, "a crossfade, not a cut"); Assert.Greater(bedB.volume, 0);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(.5f, bedB.volume, .02f); Assert.IsFalse(bedA.isPlaying, "the old bed stops once silent");
            float before = bedB.time; d.SetMusic("b", .4f);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.Greater(bedB.time, before, "the same key again neither restarts nor swaps the bed");
            d.SetMusic("a", .4f); yield return null;   // a chapter's bed, then the next chapter's one frame later
            d.SetMusic("c", .4f); yield return null;
            Assert.AreEqual(b, bedB.clip, "a second change inside a crossfade never cuts the loud bed"); Assert.AreEqual(tone, bedA.clip);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(.5f, bedA.volume, .02f); Assert.IsFalse(bedB.isPlaying);
            d.SetMusic("b", .4f);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(.5f, bedB.volume, .02f);

            d.narrator.voice.clip = tone; d.narrator.voice.Play();   // Niko speaks
            yield return new WaitForSecondsRealtime(.9f);
            Assert.AreEqual(.5f * d.musicDuck, bedB.volume, .02f, "about -10 dB under the voice");
            d.narrator.voice.Stop();
            yield return new WaitForSecondsRealtime(.9f);
            Assert.AreEqual(.5f, bedB.volume, .02f, "the bed comes back when he stops");

            d.LoopOn("loop", 1);
            yield return new WaitForSecondsRealtime(.6f);
            var loop = root.transform.Find("loop loop").GetComponent<AudioSource>();
            Assert.IsTrue(loop.isPlaying); Assert.AreEqual(.8f, loop.volume, .03f);
            d.LoopLevel("loop", .5f);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(.4f, loop.volume, .03f, "a loop follows its event's level");

            d.Play("ui");
            d.SetPaused(true);
            yield return null;
            Assert.IsFalse(bedB.isPlaying, "pause holds the music"); Assert.IsFalse(loop.isPlaying, "pause holds the loops");
            Assert.IsTrue(d.uiSfx.isPlaying, "the interface channel keeps playing under the pause menu");
            d.SetPaused(false);
            yield return null;
            Assert.IsTrue(bedB.isPlaying && loop.isPlaying, "both resume");

            d.SetAmbience("amb", .3f);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsTrue(d.ambience.isPlaying); Assert.AreEqual(.8f * Mathf.Pow(10, -6 / 20f), d.ambience.volume, .03f);

            SaveSystem.Data.settings.muted = true;
            yield return new WaitForSecondsRealtime(.4f);
            foreach (var s in new[] { bedB, loop, d.ambience, d.uiSfx, d.sfx, d.cinematicSfx, d.narrator.voice })
                Assert.AreEqual(0, s.volume, .001f, s.name + " is silent when muted");
            SaveSystem.Data.settings.muted = false;

            d.StopLoops();
            yield return null;
            Assert.IsFalse(loop.isPlaying, "StopLoops ends every loop at once (menu, restart)");
            d.SetAmbience(null, .3f);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsFalse(d.ambience.isPlaying, "the room tone fades out and stops");

            int uiPlays = 0; void Count(string key) { if (key == "ui") uiPlays++; }
            AudioDirector.Played += Count;
            d.Play("ui"); d.Play("loop"); d.Play("ui");
            AudioDirector.Played -= Count;
            Assert.AreEqual(1, uiPlays, "a key is not retriggered within 120 ms, even with another key in between");
            Assert.IsTrue(d.Recently("ui", 1) && !d.Recently("amb", 1));
        }
    }
}
