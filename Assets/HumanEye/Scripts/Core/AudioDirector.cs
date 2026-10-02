using System;
using UnityEngine;

namespace HumanEye
{
    /// Narration / music / SFX volumes from settings, with music ducked under narration.
    public class AudioDirector : MonoBehaviour
    {
        [Serializable] public struct Sfx { public string key; public AudioClip clip; }

        public Narrator narrator;
        public AudioSource music, sfx;
        public Sfx[] effects;
        float duck = 1;

        public void Play(string key, float volume = 1)
        {
            var e = Array.Find(effects, x => x.key == key);
            if (e.clip) sfx.PlayOneShot(e.clip, volume);
        }

        public void SetPaused(bool paused)
        {
            if (paused) { music.Pause(); sfx.Pause(); }
            else { music.UnPause(); sfx.UnPause(); }
        }

        void Update()
        {
            var s = SaveSystem.Data.settings;
            narrator.voice.volume = s.narration;
            sfx.volume = s.sfx;
            duck = Mathf.MoveTowards(duck, narrator.Busy ? .4f : 1, Time.unscaledDeltaTime * 1.5f);
            music.volume = s.music * duck;
        }
    }
}
