using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace HumanEye
{
    /// The module's one audio manager. Settings volumes (narration, music, effects, mute) and pause apply to every source.
    /// • Music: one primary bed at a time; SetMusic crossfades the two bed sources (music, musicB) with equal power. The
    ///   opening's score (introMusic) still crossfades over the bed (Begin/EndIntroMusic).
    /// • Ducking while Niko speaks: music about -10 dB, ambience -5 dB, effects -2.5 dB, eased both ways; music -4 dB while
    ///   Ask Niko is open.
    /// • One-shots by key on three channels (effects, interface, cinematic), each routed to its mixer bus by the editor
    ///   patch; a key already sounding is not retriggered within 120 ms. The interface channel is never paused or stopped
    ///   (modal and pause-menu clicks, a click that starts a chapter); hover (HudState) has a 200 ms cooldown.
    /// • Loops by key (gate servo, breakup crackle, travelling pulse) belong to the event that starts them: it levels,
    ///   moves (3D) and stops them; pause pauses them and StopLoops ends them all (menu, restart, chapter change).
    /// • Ambience: one near-subliminal room tone at a time.
    /// The mixer (Master > Voice, Music, Ambience, SFX, UI, Cinematic) carries routing and bus trims. Unity Web supports group
    /// volume only, so ducking and fades stay on the sources and sound the same on every platform. Every field below the
    /// header is optional: without them the director behaves as it did before the audio overhaul.
    public class AudioDirector : MonoBehaviour
    {
        public enum Bus { Sfx, UI, Cinematic }
        [Serializable] public struct Sfx { public string key; public AudioClip clip; [Tooltip("Trim in dB.")] public float gain; public Bus bus; }
        [Serializable] public struct Track { public string key; public AudioClip clip; [Tooltip("Trim in dB.")] public float gain; }

        public Narrator narrator;
        public AudioSource music, introMusic, sfx;
        public Sfx[] effects;
        public float musicGain = 1;   // intro swell; Stage resets it to 1
        [Header("Audio overhaul (optional)")]
        public AudioSource musicB;
        public AudioSource uiSfx, cinematicSfx, ambience;
        [Tooltip("Mixer bus for the loops created at runtime.")] public AudioMixerGroup loopGroup;
        public Track[] tracks;
        [Tooltip("Music level while Niko speaks (0.32 = -10 dB).")] [Range(0, 1)] public float musicDuck = .32f;
        [Tooltip("Music level while Ask Niko is open (0.63 = -4 dB).")] [Range(0, 1)] public float listenDuck = .63f;
        /// Ask Niko is open (Stage.Audio): the bed steps back while the learner types.
        [NonSerialized] public bool listening;

        float duck = 1, speakW;
        float hoverAt;
        readonly Dictionary<string, float> lastAt = new();   // per key, so interleaved keys cannot stack either
        string softKey; float softVolume; int playedFrame = -1;
        bool introActive, musicPaused;
        float introBlend, introGain = 1;
        const float MusicCrossfade = 1.2f;
        AudioSource current; float wA = 1, wB, gainA = 1, gainB = 1, bedFade = 2;
        AudioClip ambTarget; float ambW, ambGain = 1, ambFade = 1.5f;
        sealed class Loop { public AudioSource src; public float target, gain; public bool on; }   // level 0 keeps it running; off stops it
        readonly Dictionary<string, Loop> loops = new();

        public bool IntroMusicActive => introActive || introBlend > 0;
        public string MusicKey { get; private set; }
        public string AmbienceKey { get; private set; }
        /// The mix state, standing in for mixer snapshots (Unity Web has none): Paused, NikoSpeaking, ChatbotListening,
        /// Transition (the opening's score), Intro (start theme), Quiz, Finale, otherwise Exploration.
        public string MixState => musicPaused ? "Paused" : speakW > .5f ? "NikoSpeaking" : listening ? "ChatbotListening" : introActive ? "Transition"
            : MusicKey switch { "start" => "Intro", "quiz" => "Quiz", "finale" => "Finale", _ => "Exploration" };
        public float Duck => duck;
        public bool MusicPlaying => music.isPlaying || musicB && musicB.isPlaying || introMusic && introMusic.isPlaying;
        public int ActiveLoops { get { int n = 0; foreach (var l in loops.Values) if (l.src && l.src.isPlaying) n++; return n; } }

        static float Db(float db) => Mathf.Pow(10, db / 20);
        static float Eq(float w) => Mathf.Sin(Mathf.Clamp01(w) * Mathf.PI * .5f);   // equal-power fade curve

        public AudioClip Clip(string key) => Array.Find(effects, x => x.key == key).clip;
        /// Every cue, loop start, music bed and ambience change, for the walkthrough's audio log ("music:key", "ambience:key", "key (loop)").
        public static event Action<string> Played;

        public void Play(string key, float volume = 1)
        {
            int i = Array.FindIndex(effects, x => x.key == key);
            if (i < 0 || !effects[i].clip) return;
            if (lastAt.TryGetValue(key, out var at) && Time.unscaledTime - at < .12f) return;
            lastAt[key] = Time.unscaledTime; playedFrame = Time.frameCount;
            var e = effects[i];
            Source(e.bus).PlayOneShot(e.clip, volume * Db(e.gain));
            Played?.Invoke(key);
        }

        /// A low-priority cue (a label tick, a stream's start): it plays at the end of the frame, and only if no other cue
        /// started in that frame, so it never stacks on a highlight, a reveal or a click.
        public void Soft(string key, float volume = 1) { softKey = key; softVolume = volume; }

        void LateUpdate()
        {
            if (softKey != null && playedFrame != Time.frameCount) Play(softKey, softVolume);
            softKey = null;
        }

        /// `key` played less than `seconds` ago (a finished check's fanfare stands alone: no open or chapter sound on top).
        public bool Recently(string key, float seconds) => lastAt.TryGetValue(key, out var at) && Time.unscaledTime - at < seconds;

        AudioSource Source(Bus b) => b == Bus.UI && uiSfx ? uiSfx : b == Bus.Cinematic && cinematicSfx ? cinematicSfx : sfx;

        public void SetPaused(bool paused)
        {
            musicPaused = paused;
            foreach (var s in Sources())
                if (s == uiSfx) continue;   // the pause menu and modals still click
                else if (paused) s.Pause(); else s.UnPause();
        }

        /// Skip: every effect and loop of the cinematic stops at once (the interface channel keeps the skip click).
        public void StopEffects() { sfx.Stop(); if (cinematicSfx) cinematicSfx.Stop(); StopLoops(); }

        /// The pointer reached another available control (HudState.Hovered): a quiet tick, at most every 200 ms.
        public void Hover() { if (Time.unscaledTime - hoverAt > .2f) { hoverAt = Time.unscaledTime; Play("hover", .5f); } }
        /// An available control was clicked (HudState.Clicked): the shared soft click under the control's own sound.
        public void Click() => Play("click", .55f);

        IEnumerable<AudioSource> Sources()
        {
            foreach (var s in new[] { music, musicB, introMusic, sfx, uiSfx, cinematicSfx, ambience }) if (s) yield return s;
            foreach (var l in loops.Values) if (l.src) yield return l.src;
        }

        // ---------------- music ----------------

        /// Crossfade to the bed `key` (a Track) over `fade` seconds; the same key again does nothing, so callers may repeat it.
        public void SetMusic(string key, float fade = 2)
        {
            if (key == MusicKey || tracks == null) return;
            var t = Array.Find(tracks, x => x.key == key);
            if (!t.clip) return;
            MusicKey = key; bedFade = Mathf.Max(.05f, fade); Played?.Invoke("music:" + key);
            if (!current) current = music;
            if (current.clip == t.clip && current.isPlaying) { if (current == music) gainA = Db(t.gain); else gainB = Db(t.gain); return; }   // already the bed (the start theme at boot)
            if (!musicB) { music.clip = t.clip; music.loop = true; music.Play(); gainA = Db(t.gain); if (musicPaused) music.Pause(); return; }
            var next = current == music ? musicB : music;
            if (next.isPlaying && next.clip != t.clip && (next == music ? wA : wB) > (current == music ? wA : wB)) next = current;   // two changes inside one crossfade: the loud bed keeps fading out
            if (next.clip != t.clip || !next.isPlaying) { next.clip = t.clip; next.loop = true; next.time = 0; next.Play(); if (musicPaused) next.Pause(); }
            if (next == music) gainA = Db(t.gain); else gainB = Db(t.gain);
            current = next;
        }

        /// The opening's score over the bed (non-looping); EndIntroMusic crossfades back to the bed.
        public void BeginIntroMusic()
        {
            if (!introMusic || !introMusic.clip) return;
            introActive = true;
            introMusic.Stop();
            introMusic.loop = false;
            ApplyVolumes(0);
            introMusic.Play();
            if (musicPaused) introMusic.Pause();
        }

        /// Play `key` (a Track) as the score over the bed, crossfading in; EndIntroMusic returns to the bed.
        public void BeginIntroMusic(string key)
        {
            var t = tracks == null ? default : Array.Find(tracks, x => x.key == key);
            if (!introMusic || !t.clip) { BeginIntroMusic(); return; }
            if (introActive && introMusic.clip == t.clip && introMusic.isPlaying) return;
            introMusic.clip = t.clip; introGain = Db(t.gain); Played?.Invoke("music:" + key + " (score)");
            introActive = true; introMusic.Stop(); introMusic.loop = true; introMusic.Play();
            if (musicPaused) introMusic.Pause();
        }

        public void EndIntroMusic(bool immediate = false)
        {
            introActive = false;
            if (!immediate) return;
            introBlend = 0;
            if (introMusic) introMusic.Stop();
            ApplyVolumes(0);
        }

        // ---------------- ambience and loops ----------------

        /// Fade the room tone to the looping effect `key` (null: silence).
        public void SetAmbience(string key, float fade = 1.5f)
        {
            if (key == AmbienceKey || !ambience) return;
            AmbienceKey = key; ambFade = Mathf.Max(.05f, fade); Played?.Invoke("ambience:" + (key ?? "off"));
            int i = key == null ? -1 : Array.FindIndex(effects, x => x.key == key);
            ambTarget = i < 0 ? null : effects[i].clip;
            if (i >= 0) ambGain = Db(effects[i].gain);
        }

        /// Start (or re-level) the looping effect `key` at `level` 0..1; `at` places it in the world (3D), else it plays flat.
        public void LoopOn(string key, float level = 1, Transform at = null)
        {
            int i = Array.FindIndex(effects, x => x.key == key);
            if (i < 0 || !effects[i].clip) return;
            if (!loops.TryGetValue(key, out var l) || !l.src)
            {
                var go = new GameObject("loop " + key); go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.loop = true; src.clip = effects[i].clip; src.outputAudioMixerGroup = loopGroup ? loopGroup : sfx.outputAudioMixerGroup;
                src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 3; src.maxDistance = 40; src.dopplerLevel = 0; src.volume = 0;
                loops[key] = l = new Loop { src = src, gain = Db(effects[i].gain) };
            }
            l.on = true; l.target = Mathf.Clamp01(level);
            if (!l.src.isPlaying && !musicPaused) { l.src.time = 0; l.src.Play(); Played?.Invoke(key + " (loop)"); }
            if (at) { l.src.spatialBlend = 1; l.src.transform.position = at.position; }
        }

        public void LoopLevel(string key, float level) { if (loops.TryGetValue(key, out var l)) l.target = Mathf.Clamp01(level); }
        public void LoopAt(string key, Vector3 position) { if (loops.TryGetValue(key, out var l) && l.src) { l.src.spatialBlend = 1; l.src.transform.position = position; } }
        /// Fades the loop out and stops it (a loop at level 0 keeps running until it is switched off).
        public void LoopOff(string key) { if (loops.TryGetValue(key, out var l)) { l.on = false; l.target = 0; } }

        /// Ends every loop at once (menu, restart, chapter change) so nothing keeps sounding into the next scene.
        public void StopLoops()
        {
            foreach (var l in loops.Values) { l.on = false; l.target = 0; if (l.src) { l.src.Stop(); l.src.volume = 0; } }
        }

        // ---------------- per frame ----------------

        void ApplyVolumes(float dt)
        {
            var s = SaveSystem.Data.settings;
            float m = s.muted ? 0 : s.music * duck;
            float bed = musicGain * Mathf.Cos(introBlend * Mathf.PI * .5f);
            if (!current) current = music;
            wA = Mathf.MoveTowards(wA, current == music ? 1 : 0, dt / bedFade);
            wB = Mathf.MoveTowards(wB, current == musicB ? 1 : 0, dt / bedFade);
            music.volume = m * bed * Eq(wA) * gainA;
            if (musicB) { musicB.volume = m * bed * Eq(wB) * gainB; if (wB == 0 && musicB.isPlaying && current != musicB) musicB.Stop(); }
            if (wA == 0 && music.isPlaying && current != music) music.Stop();
            if (introMusic) introMusic.volume = m * Mathf.Sin(introBlend * Mathf.PI * .5f) * introGain;
        }

        void Update()
        {
            var s = SaveSystem.Data.settings;
            narrator.voice.volume = s.muted ? 0 : s.narration;
            bool speaking = narrator.Busy || narrator.voice.isPlaying;
            float dt = musicPaused ? 0 : Time.unscaledDeltaTime;
            speakW = Mathf.MoveTowards(speakW, speaking ? 1 : 0, Time.unscaledDeltaTime * 2.5f);
            float fx = s.muted ? 0 : s.sfx;
            sfx.volume = fx * Mathf.Lerp(1, .75f, speakW);   // effects sit under the voice while Niko talks
            if (uiSfx) uiSfx.volume = fx;
            if (cinematicSfx) cinematicSfx.volume = fx * Mathf.Lerp(1, .7f, speakW);
            duck = Mathf.MoveTowards(duck, speaking ? musicDuck : listening ? listenDuck : 1, dt * 1.5f);
            introBlend = Mathf.MoveTowards(introBlend, introActive ? 1 : 0, dt / MusicCrossfade);
            if (!introActive && introBlend == 0 && introMusic && introMusic.isPlaying) introMusic.Stop();
            ApplyVolumes(dt);

            if (ambience)
            {   // fade out, swap, fade in
                bool swap = ambience.clip != ambTarget;
                ambW = Mathf.MoveTowards(ambW, swap || !ambTarget ? 0 : 1, dt / ambFade);
                if (swap && ambW == 0) { ambience.Stop(); ambience.clip = ambTarget; ambience.loop = true; if (ambTarget) { ambience.Play(); if (musicPaused) ambience.Pause(); } }
                ambience.volume = fx * ambGain * Eq(ambW) * Mathf.Lerp(1, .56f, speakW);
            }

            foreach (var l in loops.Values)
            {
                if (!l.src) continue;
                l.src.volume = Mathf.MoveTowards(l.src.volume, fx * l.gain * l.target * Mathf.Lerp(1, .75f, speakW), dt * 6);
                if (!l.on && l.src.volume == 0 && l.src.isPlaying) l.src.Stop();
            }

        }
    }
}
