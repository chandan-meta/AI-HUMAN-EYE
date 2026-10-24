using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    public class IntroAudioTests
    {
        GameObject root;
        AudioClip clip;
        SettingsData savedSettings;

        [TearDown]
        public void Cleanup()
        {
            if (savedSettings != null) SaveSystem.Data.settings = savedSettings;
            if (root) Object.DestroyImmediate(root);
            if (clip) Object.DestroyImmediate(clip);
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator IntroMusicCrossfadeRespectsPauseMuteSkipAndRestart()
        {
            savedSettings = SaveSystem.Data.settings;
            SaveSystem.Data.settings = new SettingsData { music = .5f };
            root = new GameObject("Intro audio test");
            clip = AudioClip.Create("Silent music fixture", 32000 * 8, 1, 32000, false);
            var director = root.AddComponent<AudioDirector>();
            director.music = root.AddComponent<AudioSource>();
            director.music.clip = clip;
            director.music.loop = true;
            director.sfx = root.AddComponent<AudioSource>();
            director.introMusic = root.AddComponent<AudioSource>();
            director.introMusic.clip = clip;
            director.narrator = root.AddComponent<Narrator>();
            director.narrator.voice = root.AddComponent<AudioSource>();

            director.BeginIntroMusic();
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsTrue(director.IntroMusicActive);
            Assert.AreEqual(.5f, director.introMusic.volume, .001f);
            Assert.AreEqual(0, director.music.volume, .001f);

            director.EndIntroMusic();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.Greater(director.music.volume, 0);
            Assert.Greater(director.introMusic.volume, 0, "Skip must crossfade, not cut the score");
            director.SetPaused(true);
            float beforePause = director.introMusic.volume;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(beforePause, director.introMusic.volume, .0001f, "Pause freezes the crossfade");
            director.SetPaused(false);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsFalse(director.IntroMusicActive);
            Assert.IsFalse(director.introMusic.isPlaying);
            Assert.AreEqual(0, director.introMusic.volume, .001f);
            Assert.AreEqual(.5f, director.music.volume, .001f);

            director.BeginIntroMusic();
            director.BeginIntroMusic();
            yield return new WaitForSecondsRealtime(1.4f);
            SaveSystem.Data.settings.muted = true;
            yield return null;
            Assert.AreEqual(0, director.music.volume);
            Assert.AreEqual(0, director.introMusic.volume);
            director.EndIntroMusic(true);
            Assert.IsFalse(director.IntroMusicActive, "Returning to the menu cancels intro audio immediately");
            SaveSystem.Data.settings.muted = false;
            director.EndIntroMusic(true);
            Assert.AreEqual(.5f, director.music.volume, .001f, "Repeated cancellation restores gameplay music");
        }
    }
}
