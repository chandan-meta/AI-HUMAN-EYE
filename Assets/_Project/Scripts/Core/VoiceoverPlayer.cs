using UnityEngine;
using TMPro;
namespace VisionLab {
    public class VoiceoverPlayer : MonoBehaviour {
        public VOManifest manifest;
        public AudioSource source, ambience;
        public TMP_Text subtitle;
        public bool Captions = true;
        public bool IsPlaying { get; private set; }
        public bool Paused;
        public int LastId { get; private set; }
        public event System.Action<int> OnComplete;
        float remaining, ambienceVolume = .22f;
        string currentText;
        public void Play(int id, string suffix = "", bool playAudio = true) {
            var line = manifest.Find(id); if (line == null) return;
            if(playAudio)source.Stop(); LastId = id;
            currentText = line.text.Replace("|", " ") + suffix;
            subtitle.text = Captions ? currentText : "";
            remaining = line.clip ? line.clip.length : Mathf.Max(1, line.text.Length * .06f);
            if(playAudio){source.clip = line.clip; if (line.clip) source.Play();}
            if (Paused) source.Pause();
            IsPlaying = true;
            if (ambience) ambience.volume = ambienceVolume * .3981f;
        }
        public void Stop() { var wasPlaying=IsPlaying; source.Stop(); IsPlaying = false; subtitle.text = ""; if (ambience) ambience.volume = ambienceVolume;if(wasPlaying)OnComplete?.Invoke(LastId); }
        public void SetPaused(bool paused) { Paused = paused; if (paused) source.Pause(); else if (IsPlaying) source.UnPause(); }
        public void SetCaptions(bool enabled) { Captions = enabled; subtitle.text = enabled && IsPlaying ? currentText : ""; }
        public void SetAmbienceVolume(float value) { ambienceVolume = value; if (ambience) ambience.volume = value * (IsPlaying ? .3981f : 1); }
        void Update() { if (IsPlaying && !Paused) { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) Stop(); } }
    }
}
