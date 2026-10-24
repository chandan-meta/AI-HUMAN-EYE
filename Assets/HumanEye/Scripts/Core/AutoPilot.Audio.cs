using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HumanEye
{
    /// Audio evidence in the walkthrough log (audio overhaul, 15 October 2026): every cue, loop start, music bed and ambience
    /// change as it happens with the chapter and lesson state; the same cue again within 350 ms (a double trigger; hover and
    /// click excepted, they have their own guard); mix-state changes; and a summary with the cues that never played.
    public partial class AutoPilot
    {
        readonly Dictionary<string, int> heard = new();
        readonly Dictionary<string, float> lastHeard = new();
        int audioRepeats, maxLoops;
        string mixState;

        void Heard(string key)
        {
            if (log == null || !runner) return;
            heard[key] = heard.TryGetValue(key, out var n) ? n + 1 : 1;
            float now = Time.unscaledTime;
            if (lastHeard.TryGetValue(key, out var at) && now - at < .35f && key is not ("hover" or "click") && !key.Contains(':'))
            { audioRepeats++; Log("audio_repeat", $"{key} again after {(now - at) * 1000:0} ms"); }
            lastHeard[key] = now;
            Log("audio", $"{key} chapter={(runner.Chapter ? runner.Chapter.id : "menu")} state={runner.State}{(stage.IntroPlaying ? $" intro={stage.IntroTime:0.0}" : "")}");
        }

        void AudioSample()
        {
            maxLoops = Mathf.Max(maxLoops, stage.sfx.ActiveLoops);
            if (stage.sfx.MixState != mixState) { mixState = stage.sfx.MixState; Log("audio_mix", mixState); }
        }

        /// The settings panel is open (stress walk): the music and effects sliders at zero silence their channels, Sound off
        /// silences every channel including the voice, and both come back.
        IEnumerator CheckAudioSettings()
        {
            var a = stage.sfx; var settings = SaveSystem.Data.settings;
            Slider music = hud.Control<Slider>("set-music"), effects = hud.Control<Slider>("set-sfx");
            float m = music.value, e = effects.value;
            music.value = 0; effects.value = 0; yield return null;
            Check(a.music.volume == 0 && (!a.musicB || a.musicB.volume == 0) && a.sfx.volume == 0 && (!a.uiSfx || a.uiSfx.volume == 0) && (!a.ambience || a.ambience.volume == 0),
                "Music and effects sliders at zero silence the bed, ambience and every effect channel");
            music.value = m; effects.value = e;
            settings.muted = true; yield return null;
            Check(a.narrator.voice.volume == 0 && a.music.volume == 0 && (!a.musicB || a.musicB.volume == 0) && a.sfx.volume == 0 && (!a.cinematicSfx || a.cinematicSfx.volume == 0),
                "Sound off silences voice, music and effects");
            settings.muted = false; yield return null;
            Check(a.narrator.voice.volume > 0 && a.music.volume + (a.musicB ? a.musicB.volume : 0) > 0 && a.sfx.volume > 0, "Sound on restores voice, music and effects");
        }

        void LogAudioSummary()
        {
            var cues = heard.Where(h => !h.Key.Contains(':')).ToArray();
            Log("audio_summary", $"cues={cues.Sum(h => h.Value)} distinct={cues.Length} repeats={audioRepeats} maxLoops={maxLoops} " +
                $"music=[{string.Join(",", heard.Keys.Where(k => k.StartsWith("music:")).Select(k => k.Substring(6)))}] ambience=[{string.Join(",", heard.Keys.Where(k => k.StartsWith("ambience:")).Select(k => k.Substring(9)))}]");
            var unplayed = stage.sfx.effects.Select(e => e.key).Where(k => !heard.ContainsKey(k) && !heard.ContainsKey(k + " (loop)") && !heard.ContainsKey("ambience:" + k)).ToArray();
            Log("audio_unplayed", string.Join(",", unplayed));
        }
    }
}
