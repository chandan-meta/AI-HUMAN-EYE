using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        const string AudioRoot = Root + "Audio/";

        /// The audio overhaul (15 October 2026): the owner-listed CC0 music and effects (Tools/HumanEye/build_audio_assets.py)
        /// on a seven-bus mixer, the music beds, ambience, loops and keyed cues registered on the AudioDirector, every source
        /// routed to its bus. Additive and idempotent; run after the other patches (it only touches audio).
        [MenuItem("Tools/Human Eye/Authoring/Apply audio overhaul (keeps scene edits)", priority = 19)]
        public static void ApplyAudioOverhaul()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var director = stage.sfx;
            if (!director) throw new InvalidOperationException("No AudioDirector on the Stage");
            ImportAudio();
            var bus = AudioMixerBuses();

            AudioSource Child(string name)
            {
                var t = director.transform.Find(name); var go = t ? t.gameObject : new GameObject(name);
                go.transform.SetParent(director.transform, false);
                var s = go.GetComponent<AudioSource>(); if (!s) s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0; s.dopplerLevel = 0; return s;
            }
            director.musicB = Child("Music B"); director.uiSfx = Child("Interface Sfx"); director.cinematicSfx = Child("Cinematic Sfx"); director.ambience = Child("Ambience");
            director.musicB.loop = director.ambience.loop = true;
            foreach (var (s, b) in new (AudioSource, string)[] { (director.music, "Music"), (director.musicB, "Music"), (director.introMusic, "Music"), (director.sfx, "SFX"),
                         (director.uiSfx, "UI"), (director.cinematicSfx, "Cinematic"), (director.ambience, "Ambience"), (stage.narrator.voice, "Voice") })
                if (s) { s.outputAudioMixerGroup = bus[b]; EditorUtility.SetDirty(s); }
            director.loopGroup = bus["SFX"];
            // Any other source in the scene goes to the effects bus unless it already has one (no source bypasses the mixer).
            foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!s.outputAudioMixerGroup) { s.outputAudioMixerGroup = bus["SFX"]; EditorUtility.SetDirty(s); }

            AudioClip Load(string rel) { var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + rel); if (!c) throw new InvalidOperationException("Missing audio " + rel + " (python -I Tools/HumanEye/build_audio_assets.py)"); return c; }
            director.tracks = new (string key, string file, float gain)[]
            {
                ("start", "Music/bgm_start_exploration_theme.ogg", 0), ("lab", "Music/bgm_lab_the_cogs.ogg", -1.5f), ("niko", "Music/bgm_niko_first_light_particles.ogg", 0),
                ("transition", "Music/bgm_transition_claimed_by_the_void.ogg", -2), ("eye", "Music/bgm_eye_outer_space_loop.ogg", 0), ("pulse", "Music/bgm_pulse_airy.ogg", 0),
                ("quiz", "Music/bgm_quiz_simple_bgm_loop.ogg", -2.5f), ("finale", "Music/bgm_finale_space_city.ogg", -2),
            }.Select(t => new AudioDirector.Track { key = t.key, clip = Load(t.file), gain = t.gain }).ToArray();
            director.music.clip = director.tracks[0].clip; director.music.loop = true;   // the start screen's theme from the first frame
            BuildData();   // the content steps that name new cues (music beds, highlights) into the chapter assets

            var effects = director.effects.ToList();
            foreach (var (key, file, gain, b) in CueClips)
            {
                effects.RemoveAll(e => e.key == key);
                effects.Add(new AudioDirector.Sfx { key = key, clip = Load(file), gain = gain, bus = b });
            }
            int unlock = effects.FindIndex(e => e.key == "unlock");   // Explore Inside ends Scene 2: the chime must outlive Scene 3's entry (IntroReset stops effects)
            if (unlock >= 0) { var u = effects[unlock]; u.bus = AudioDirector.Bus.Cinematic; effects[unlock] = u; }
            director.effects = effects.ToArray();
            foreach (var o in new Object[] { director, director.musicB, director.uiSfx, director.cinematicSfx, director.ambience, stage }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_AUDIO_APPLIED tracks={director.tracks.Length} effects={director.effects.Length} keys={CueClips.Length} buses={string.Join(",", bus.Keys)}");
        }

        /// Cue keys (stable IDs used by the game code) -> clip, trim (dB) and channel. Existing keys are re-clipped in place, so
        /// every call site already in the game plays the new sound; each comment names the cue-sheet ID it serves. Five earlier
        /// keys keep their sounds (chapter, lightmove, titlehit: generated in-project; discovery, unlock: Kenney CC0), recorded
        /// in AUDIO_SOURCES.csv; the ReferenceMatch-only keys are not part of the production flow. The
        /// interface channel is never paused or stopped (modals, the pause menu, a click that starts a chapter, the check that
        /// ends a chapter); the cinematic channel is stopped only by Skip.
        static readonly (string key, string file, float gain, AudioDirector.Bus bus)[] CueClips =
        {
            // gate and lab (the opening's accent table and named cues, Stage.Opening)
            ("gatestart", "SFX/Gate/gate_start_press.wav", 0, AudioDirector.Bus.UI),   // GATE_START_PRESS
            ("gatescan", "SFX/Gate/gate_iris_scan.wav", 0, AudioDirector.Bus.Cinematic),   // GATE_IRIS_SCAN, ANATOMY_SCAN
            ("gateunlock", "SFX/Gate/gate_unlock.wav", 0, AudioDirector.Bus.Sfx),   // GATE_UNLOCK
            ("gateservo", "SFX/Gate/gate_servo_loop.wav", 0, AudioDirector.Bus.Sfx),   // GATE_OPEN_SERVO (loop, 3D)
            ("gatewhoosh", "SFX/Gate/gate_open_whoosh.wav", 0, AudioDirector.Bus.Sfx),   // GATE_OPEN_WHOOSH
            ("gateentry", "SFX/Gate/gate_entry.wav", 0, AudioDirector.Bus.Cinematic),   // GATE_ENTRY
            ("labsting", "SFX/Gate/lab_reveal_sting.wav", -2, AudioDirector.Bus.Cinematic),   // LAB_REVEAL_STING (the lab and the title)
            ("hologram", "SFX/Gate/hologram_on.wav", 0, AudioDirector.Bus.Sfx),   // HOLOGRAM_ON (the eye settles on the display)
            ("softwhoosh", "SFX/Gate/camera_sweep.wav", 0, AudioDirector.Bus.Cinematic),   // CAMERA_CINEMATIC_SWEEP (authored moves)
            ("overview", "SFX/Gate/camera_sweep.wav", 2, AudioDirector.Bus.Cinematic),   // CAMERA_CINEMATIC_SWEEP (the closing overview)
            ("amblab", "Ambience/amb_lab_hum.wav", -14, AudioDirector.Bus.Sfx),   // AMB_LAB, AMB_GATE (loop)
            ("ambeye", "Ambience/amb_eye_hum.wav", -14, AudioDirector.Bus.Sfx),   // AMB_EYE (loop)
            // Niko and Talk to Niko
            ("nikoappear", "SFX/Companion/niko_hello.wav", -2, AudioDirector.Bus.Sfx),   // NIKO_HELLO
            ("nikolisten", "SFX/Companion/niko_listen.wav", 0, AudioDirector.Bus.UI),   // NIKO_LISTEN
            ("nikothink", "SFX/Companion/niko_think.wav", 0, AudioDirector.Bus.UI),   // NIKO_THINK
            ("nikoreply", "SFX/Companion/niko_reply.wav", 0, AudioDirector.Bus.UI),   // NIKO_REPLY_BEGIN
            ("nikoerror", "SFX/Companion/niko_error.wav", 0, AudioDirector.Bus.UI),   // NIKO_RESPONSE_ERROR
            ("complete", "SFX/Companion/ending_farewell.wav", 0, AudioDirector.Bus.Cinematic),   // ENDING_FAREWELL (after the closing line)
            // anatomy layer changes
            ("dissolve", "SFX/Anatomy/dissolve_start.wav", -2, AudioDirector.Bus.Sfx),   // SKIN_/MUSCLE_DISSOLVE_START
            ("crackle1", "SFX/Anatomy/dissolve_crackle_loop.wav", -3, AudioDirector.Bus.Sfx),   // SKIN_DISSOLVE_LOOP (follows the breakup)
            ("crackle2", "SFX/Anatomy/dissolve_crackle_loop_high.wav", -3, AudioDirector.Bus.Sfx),   // MUSCLE_DISSOLVE_LOOP
            ("layerdone", "SFX/Anatomy/layer_reveal.wav", -2, AudioDirector.Bus.Sfx),   // MUSCLE_REVEAL
            ("layerreveal", "SFX/Anatomy/layer_reveal.wav", -3, AudioDirector.Bus.Sfx),   // the cut eye opens, objects appear
            ("organs", "SFX/Anatomy/organs_reveal.wav", -1, AudioDirector.Bus.Sfx),   // ORGANS_REVEAL
            // the eye
            ("eyeappear", "SFX/Eye/eye_appear.wav", -1, AudioDirector.Bus.Sfx),   // EYE_MODEL_APPEAR
            ("eyezoom", "SFX/Eye/eye_zoom.wav", 0, AudioDirector.Bus.Cinematic),   // EYE_ZOOM_ENTER (part close-ups, the push into the eye)
            ("dive", "SFX/Eye/eye_zoom.wav", 2, AudioDirector.Bus.Cinematic),   // EYE_ZOOM_ENTER (into the retina)
            ("select", "SFX/Eye/part_scan.wav", 0, AudioDirector.Bus.Sfx),   // PART_SCAN (a part is chosen)
            ("tick", "SFX/Eye/part_scan.wav", -3, AudioDirector.Bus.Sfx),   // PART_SCAN (the highlight sweep)
            ("isolate", "SFX/Eye/cornea_ping.wav", -2, AudioDirector.Bus.Sfx),   // a structure is isolated
            ("hl_cornea", "SFX/Eye/cornea_ping.wav", 0, AudioDirector.Bus.Sfx),   // CORNEA_HIGHLIGHT
            ("hl_iris", "SFX/Eye/iris_shimmer.wav", 0, AudioDirector.Bus.Sfx),   // IRIS_HIGHLIGHT (exploration)
            ("iris", "SFX/Eye/iris_shimmer.wav", 0, AudioDirector.Bus.Sfx),   // IRIS_HIGHLIGHT (Scene 5)
            ("hl_retina", "SFX/Eye/retina_shimmer.wav", 0, AudioDirector.Bus.Sfx),   // RETINA_HIGHLIGHT
            ("cell", "SFX/Eye/retina_shimmer.wav", -2, AudioDirector.Bus.Sfx),   // RETINA_HIGHLIGHT (light on the photoreceptors)
            ("pupilwide", "SFX/Eye/pupil_dilate.wav", 0, AudioDirector.Bus.Sfx),   // PUPIL_APERTURE (widens)
            ("pupilnarrow", "SFX/Eye/pupil_constrict.wav", 0, AudioDirector.Bus.Sfx),   // PUPIL_APERTURE (narrows)
            ("lensfocus", "SFX/Eye/lens_focus.wav", 0, AudioDirector.Bus.Sfx),   // LENS_FOCUS
            ("label", "SFX/Eye/label_appear.wav", 0, AudioDirector.Bus.UI),   // LABEL_APPEAR (labels, instructions)
            // retina -> optic nerve -> brain
            ("signaltravel", "SFX/OpticNerveBrain/neural_signal_start.wav", -1, AudioDirector.Bus.Sfx),   // NEURAL_SIGNAL_START (nerve stream)
            ("pulse", "SFX/OpticNerveBrain/neural_signal_start.wav", -3, AudioDirector.Bus.Sfx),   // NEURAL_SIGNAL_START (retina scale)
            ("pulsetravel", "SFX/OpticNerveBrain/optic_pulse_travel_loop.wav", -4, AudioDirector.Bus.Sfx),   // OPTIC_PULSE_TRAVEL (loop, 3D)
            ("pulsenear", "SFX/OpticNerveBrain/pulse_near_brain.wav", 0, AudioDirector.Bus.Sfx),   // OPTIC_PULSE_NEAR_BRAIN
            ("signalarrive", "SFX/OpticNerveBrain/pulse_brain_arrival.wav", 0, AudioDirector.Bus.Cinematic),   // PULSE_BRAIN_ARRIVAL
            ("brainreveal", "SFX/OpticNerveBrain/brain_reveal.wav", -1, AudioDirector.Bus.Cinematic),   // BRAIN_REVEAL (Scene 4)
            ("brainrise", "SFX/OpticNerveBrain/brain_reveal.wav", 0, AudioDirector.Bus.Cinematic),   // BRAIN_REVEAL (Scene 9)
            ("cortex", "SFX/OpticNerveBrain/cortex_highlight.wav", 0, AudioDirector.Bus.Sfx),   // VISUAL_CORTEX_HIGHLIGHT
            // interface and checks
            ("hover", "SFX/UI/ui_hover.wav", 0, AudioDirector.Bus.UI),   // UI_HOVER
            ("click", "SFX/UI/ui_click.wav", 0, AudioDirector.Bus.UI),   // UI_CLICK
            ("tap", "SFX/UI/ui_click.wav", 0, AudioDirector.Bus.UI),   // UI_CLICK (ability buttons)
            ("open", "SFX/UI/ui_open.wav", 0, AudioDirector.Bus.UI),   // UI_OPEN_PANEL (modals)
            ("popup", "SFX/UI/ui_open.wav", 0, AudioDirector.Bus.UI),   // UI_OPEN_PANEL (the retinal-image popup)
            ("close", "SFX/UI/ui_close.wav", 0, AudioDirector.Bus.UI),   // UI_CLOSE_PANEL (modals, Ask Niko)
            ("popdown", "SFX/UI/ui_close.wav", 0, AudioDirector.Bus.UI),   // UI_CLOSE_PANEL (the popup, a check card)
            ("skip", "SFX/UI/ui_skip.wav", 0, AudioDirector.Bus.UI),   // UI_SKIP_INTRO
            ("menu", "SFX/UI/return_menu.wav", 0, AudioDirector.Bus.UI),   // RETURN_MENU
            ("confirm", "SFX/Gate/gate_start_press.wav", -2, AudioDirector.Bus.UI),   // free-mode Continue
            ("deny", "SFX/UI/ui_unavailable.wav", 0, AudioDirector.Bus.UI),   // an unavailable action or locked chapter (never a buzzer)
            ("quizopen", "SFX/Quiz/quiz_open.wav", 0, AudioDirector.Bus.UI),   // QUIZ_OPEN
            ("quizcorrect", "SFX/Quiz/quiz_correct.wav", 0, AudioDirector.Bus.UI),   // QUIZ_CORRECT
            ("quizwrong", "SFX/Quiz/quiz_wrong.wav", 0, AudioDirector.Bus.UI),   // QUIZ_WRONG
            ("quizhint", "SFX/Quiz/quiz_hint.wav", 0, AudioDirector.Bus.UI),   // QUIZ_HINT
            ("quizcomplete", "SFX/Quiz/quiz_complete.wav", 0, AudioDirector.Bus.UI),   // QUIZ_COMPLETE
        };

        /// Music beds compressed in memory (the Web player's advice for music), effects decompressed on load (low latency; on
        /// Web the built loops' smpl loop points need it too).
        static void ImportAudio()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot + "Music", AudioRoot + "Ambience", AudioRoot + "SFX" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                bool bed = dir == AudioRoot + "Music" && System.IO.Path.GetFileName(path).StartsWith("bgm_");
                if (!bed && dir != AudioRoot + "Ambience" && !dir.StartsWith(AudioRoot + "SFX/")) continue;   // the earlier clips keep their settings
                var imp = (AudioImporter)AssetImporter.GetAtPath(path); var s = imp.defaultSampleSettings;
                var load = bed ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad; float q = bed ? .55f : .7f;
                if (s.loadType == load && s.compressionFormat == AudioCompressionFormat.Vorbis && Mathf.Abs(s.quality - q) < .01f && imp.loadInBackground == bed) continue;
                s.loadType = load; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = q;
                imp.defaultSampleSettings = s; imp.loadInBackground = bed; imp.SaveAndReimport();
            }
        }

        /// The module's mixer: Master > Voice, Music, Ambience, SFX, UI, Cinematic, created once through the editor's own mixer
        /// API (internal, reached by reflection; it fails loudly if a later Unity renames it) and completed idempotently.
        static Dictionary<string, AudioMixerGroup> AudioMixerBuses()
        {
            const string path = AudioRoot + "HumanEyeMixer.mixer";
            var type = typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            MethodInfo M(Type t, string n) => t.GetMethods(all).FirstOrDefault(m => m.Name == n) ?? throw new MissingMethodException(t.Name, n);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (!mixer) { mixer = (AudioMixer)M(type, "CreateMixerControllerAtPath").Invoke(null, new object[] { path }); AssetDatabase.ImportAsset(path); mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path); }
            var master = (AudioMixerGroup)type.GetProperty("masterGroup", all).GetValue(mixer);
            var snapshot = type.GetProperty("TargetSnapshot", all).GetValue(mixer);
            var groups = new Dictionary<string, AudioMixerGroup>();
            foreach (var (name, trim) in new[] { ("Voice", 0f), ("Music", 0f), ("Ambience", 0f), ("SFX", 0f), ("UI", -2f), ("Cinematic", 0f) })
            {
                var g = mixer.FindMatchingGroups(name).FirstOrDefault(x => x.name == name);
                if (!g)
                {
                    g = (AudioMixerGroup)M(type, "CreateNewGroup").Invoke(mixer, new object[] { name, false });
                    M(type, "AddChildToParent").Invoke(mixer, new object[] { g, master });
                    try { type.GetMethods(all).FirstOrDefault(m => m.Name == "AddGroupToCurrentView")?.Invoke(mixer, new object[] { g }); }
                    catch (TargetInvocationException) { }   // a fresh mixer has no group view yet; views only filter the Mixer window
                    M(g.GetType(), "SetValueForVolume").Invoke(g, new[] { mixer, snapshot, (object)trim });
                }
                groups[name] = g;
            }
            EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets();
            return groups;
        }
    }
}
