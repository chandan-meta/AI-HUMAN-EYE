using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    /// Drives the complete first-run sequence (Scene 2 → Scene 11) through the same public actions the HUD calls,
    /// at accelerated narration speed. Proves the flow is completable and every source cue is reached; it does not
    /// prove physical touch hit-testing or real-time pacing.
    public class WalkthroughTests
    {
        string backup;

        [SetUp] public void SaveBackup() { backup = PlayerPrefs.GetString(SaveSystem.Key, null); PlayerPrefs.DeleteKey(SaveSystem.Key); }
        [TearDown] public void RestoreBackup() { if (backup != null) PlayerPrefs.SetString(SaveSystem.Key, backup); else PlayerPrefs.DeleteKey(SaveSystem.Key); AutoPilot.FastOverride = null; AutoPilot.ExitWhenDone = true; AutoPilot.CapturesEnabled = true; }

        [UnityTest, Timeout(1200000)]
        public IEnumerator FirstRunReachesFinalCuriosity()
        {
            SceneManager.LoadScene("HumanEye_Observatory");
            yield return null; yield return null;
            AutoPilot.FastOverride = 6; AutoPilot.ExitWhenDone = false; AutoPilot.CapturesEnabled = false;
            Object.FindFirstObjectByType<ModuleBoot>().gameObject.AddComponent<AutoPilot>();
            var runner = Object.FindFirstObjectByType<ChapterRunner>();
            float limit = Time.realtimeSinceStartup + 1100;
            while (!AutoPilot.Done && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsTrue(AutoPilot.Done, "walkthrough did not finish in time; state " + runner.State + " in " + runner.Chapter?.id);
            Assert.AreEqual(LessonState.Complete, runner.State);
            Assert.AreEqual(10, SaveSystem.Data.completedChapters.Count, AutoPilot.Summary);
            Assert.AreEqual(66, SaveSystem.Data.cues.Count, "every StoryboardCore cue completed exactly once: " + AutoPilot.Summary);
            Assert.Greater(SaveSystem.Data.evidence.Count, 0, "adaptive activities recorded evidence");
            Assert.AreEqual(1f, Time.timeScale, "pause/freeze never touch timeScale");
        }
    }
}
