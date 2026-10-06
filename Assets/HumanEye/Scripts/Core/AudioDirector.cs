using System;
using UnityEngine;

namespace HumanEye
{
    /// Narration / music / SFX volumes from settings, with music ducked under speech. One-shot effects never stack:
    /// a key that is already sounding is not retriggered within 120 ms (repeated clicks, label bursts).
    public class AudioDirector : MonoBehaviour
    {
        [Serializable] public struct Sfx { public string key; public AudioClip clip; }

        public Narrator narrator;
        public AudioSource music, sfx;
        public Sfx[] effects;
        float duck = 1;
        public float musicGain = 1;   // intro swell; Stage resets it to 1
        string lastKey; float lastAt;

        public AudioClip Clip(string key) => Array.Find(effects, x => x.key == key).clip;

        public void Play(string key, float volume = 1)
        {
            var clip = Clip(key);
            if (!clip) return;
            if (key == lastKey && Time.unscaledTime - lastAt < .12f) return;
            lastKey = key; lastAt = Time.unscaledTime;
            sfx.PlayOneShot(clip, volume);
        }

        public void SetPaused(bool paused)
        {
            if (paused) { music.Pause(); sfx.Pause(); }
            else { music.UnPause(); sfx.UnPause(); }
        }

        void Update()
        {
            var s = SaveSystem.Data.settings;
            narrator.voice.volume = s.muted ? 0 : s.narration;
            bool speaking = narrator.Busy || narrator.voice.isPlaying;
            sfx.volume = s.muted ? 0 : s.sfx * (speaking ? .75f : 1);   // effects sit under the voice while Niko talks
            duck = Mathf.MoveTowards(duck, speaking ? .4f : 1, Time.unscaledDeltaTime * 1.5f);
            music.volume = s.muted ? 0 : s.music * duck * musicGain;
        }
    }
}
