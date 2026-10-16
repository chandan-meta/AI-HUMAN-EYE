using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HumanEye
{
    /// Loads anonymous progress, applies quality/FPS presets and wires the few runtime-only delegates.
    public class ModuleBoot : MonoBehaviour
    {
        public ModuleConfig config;
        public Hud hud;
        public ChapterRunner runner;
        public Narrator narrator;
        public CameraDirector cams;
        public Stage stage;
        static ModuleConfig active;

        void Awake()
        {
            active = config;
            SaveSystem.Load();
            LearnerModel.SecureThreshold = config.secureThreshold;
            ApplyQuality();
            narrator.waitForContinue = () => SaveSystem.Data.settings.waitForContinue;
            // Exposure (heard via audio, or read via an explicit Continue) is tracked separately from mastery.
            narrator.Completed += (cue, source, how) => { if (source == NarrationSource.Core) SaveSystem.MarkCue(cue.id, how); };
            cams.shortMoves = () => stage.Short;
            cams.fade = hud.Fade;
        }

        void Start()
        {
            bool tracing = Web && Application.absoluteURL.Contains("heTrace");
            if (tracing) GraphicsSettings.logWhenShaderIsCompiled = true;   // what still compiles on first use (development players)
            if (Web) WarmShaders();
            if (tracing) { trace = new GraphicsStateCollection(); Debug.Log("HE_TRACE started=" + trace.BeginTrace()); }   // after the warm-up's own draws
            stage.ShowMenu();
            hud.ShowStart();
            hud.Refresh();
            if (Debug.isDebugBuild && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-heAutopilot")) gameObject.AddComponent<AutoPilot>();
#if UNITY_EDITOR
            // Editor-only chapter preview (Tools > Human Eye > Preview Chapter); never compiled into players.
            int preview = UnityEditor.EditorPrefs.GetInt("HumanEye.Preview", -1);
            if (preview >= 0) { UnityEditor.EditorPrefs.DeleteKey("HumanEye.Preview"); hud.StartModule(preview); }
#endif
        }

        // ---------- shader warm-up (web) ----------
        static bool Web => Application.platform == RuntimePlatform.WebGLPlayer;
        GraphicsStateCollection trace;

        /// WebGL compiles each shader variant the first time it is drawn: BEGIN held a black frame for 1.2-1.3 s on a fast GPU
        /// (16 October 2026), most of it one anatomy variant that cannot be split across frames. So the variants first drawn
        /// after BEGIN (the opening and the first lesson, traced from the web player itself: Resources/ShaderWarmup.txt) compile
        /// before the first frame, inside the start-up compile the loading page already covers. The start screen's own variants
        /// are left to its first frame, which compiles them faster than a warm-up does.
        static void WarmShaders()
        {
            var list = Resources.Load<TextAsset>("ShaderWarmup");
            if (!list) return;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var set = new GraphicsStateCollection { graphicsDeviceType = SystemInfo.graphicsDeviceType, runtimePlatform = Application.platform,
                qualityLevelName = QualitySettings.names[QualitySettings.GetQualityLevel()] };
            foreach (var line in list.text.Split('\n'))
            {
                var f = line.TrimEnd('\r').Split('\t');
                var shader = f.Length == 4 ? Shader.Find(f[0]) : null;
                if (shader) set.AddVariant(shader, new PassIdentifier(uint.Parse(f[1]), uint.Parse(f[2])),
                    f[3].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(k => new LocalKeyword(shader, k)).ToArray());
            }
            set.WarmUp(default).Complete();   // exact passes; without PSOs (WebGL) this is a plain variant warm-up
            Debug.Log($"HE_WARMUP {set.variantCount} shader variants in {clock.ElapsedMilliseconds} ms");
        }

        /// ?heTrace on the web page records every variant drawn; heUnityInstance.SendMessage('HumanEye_Systems', 'TraceDump')
        /// prints them in the line format Resources/ShaderWarmup.txt holds (one log line each: the browser console cuts a
        /// message at 8 KB) and keeps recording. Tools/HumanEye/web_begin_check.mjs --trace dumps on the start screen and after
        /// the opening: the list is the difference, since the start screen compiles its own variants behind the loading page.
        public void TraceDump()
        {
            if (trace == null) return;
            trace.EndTrace();
            var variants = new List<GraphicsStateCollection.ShaderVariant>(); trace.GetVariants(variants);
            foreach (var v in variants)
                Debug.Log($"HE_VARIANT\t{v.shader.name}\t{v.passId.SubshaderIndex}\t{v.passId.PassIndex}\t{string.Join(" ", v.keywords.Select(k => k.name))}");
            Debug.Log($"HE_TRACE_VARIANTS {variants.Count}");
            trace.BeginTrace();
        }

        public static bool IsTablet => Application.isMobilePlatform;

        public static void ApplyQuality()
        {
            var c = active;
            if (!c) return;
            int setting = SaveSystem.Data.settings.quality;
            string name = setting == 1 ? c.pcQuality : setting == 0 ? c.tabletQuality : IsTablet ? c.tabletQuality : c.pcQuality;
            int level = Array.IndexOf(QualitySettings.names, name);
            if (level >= 0) QualitySettings.SetQualityLevel(level, true);
            Application.targetFrameRate = name == c.tabletQuality ? c.tabletTargetFps : c.pcTargetFps;
        }
    }
}
