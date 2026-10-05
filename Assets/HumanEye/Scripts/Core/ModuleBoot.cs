using System;
using UnityEngine;

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
