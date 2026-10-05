using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    [Serializable]
    public class SettingsData
    {
        public bool subtitles = true, reducedMotion, skipCameraMotion, waitForContinue, labels = true;
        public int subtitleSize = 1;            // 0 small .. 3 extra large
        public float narration = 1, music = .35f, sfx = .7f;
        public int quality = -1;                // -1 auto (PC on desktop, Mobile on tablets)
        public bool muted;
    }

    [Serializable] public class CueRecord { public string id, how; }
    [Serializable] public class Evidence { public string item, topic; public bool correct, assisted; }

    /// Anonymous local progress. No names, recordings or remote analytics are stored.
    [Serializable]
    public class ProgressData
    {
        public int version = 1;
        public string checkpoint = "Scene02";
        public List<string> completedChapters = new(), explored = new(), needsPractice = new(), stamps = new();
        public List<CueRecord> cues = new();
        public List<Evidence> evidence = new();
        public SettingsData settings = new();
    }

    public static class SaveSystem
    {
        public const string Key = "HumanEye.Progress.v1", CorruptKey = "HumanEye.Progress.corrupt";
        // Development walkthroughs must never overwrite a learner's real progress.
        static string StorageKey => Debug.isDebugBuild && Array.Exists(Environment.GetCommandLineArgs(), a => a == "-heAutopilot") ? Key + ".QA" : Key;
        public static ProgressData Data { get; private set; } = new();
        /// Set when a save could not be read; the HUD shows it once.
        public static string Notice;
        public static event Action Changed;

        public static void Load()
        {
            var json = PlayerPrefs.GetString(StorageKey, "");
            Data = Parse(json, out Notice);
            if (Notice != null) PlayerPrefs.SetString(StorageKey == Key ? CorruptKey : CorruptKey + ".QA", json);
        }

        public static ProgressData Parse(string json, out string notice)
        {
            notice = null;
            if (string.IsNullOrEmpty(json)) return new ProgressData();
            try
            {
                var d = JsonUtility.FromJson<ProgressData>(json);
                if (d == null || d.version != 1 || d.settings == null || d.completedChapters == null || d.cues == null ||
                    d.evidence == null || d.explored == null || d.needsPractice == null || d.stamps == null)
                    throw new FormatException("unexpected save layout");
                return d;
            }
            catch (Exception e)
            {
                notice = "Saved progress could not be read, so the module started fresh. (" + e.GetType().Name + ")";
                return new ProgressData();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ResetProgress(bool keepSettings)
        {
            var settings = Data.settings;
            Data = new ProgressData();
            if (keepSettings) Data.settings = settings;
            Save();
        }

        public static void MarkCue(string id, string how)
        {
            if (Data.cues.Exists(c => c.id == id)) return;
            Data.cues.Add(new CueRecord { id = id, how = how });
            Save();
        }

        public static bool AddOnce(List<string> list, string value)
        {
            if (list.Contains(value)) return false;
            list.Add(value); Save(); return true;
        }
    }
}
