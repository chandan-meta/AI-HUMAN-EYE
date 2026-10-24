using System.Collections;
using System.Linq;
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
            var hud = Object.FindFirstObjectByType<Hud>();
            var authored = hud.canvas.GetComponentsInChildren<RectTransform>(true).Select(t => t.GetInstanceID()).ToArray();
            Assert.Greater(authored.Length, 300, "UI must be authored in the saved scene before gameplay");
            AutoPilot.FastOverride = 6; AutoPilot.ExitWhenDone = false; AutoPilot.CapturesEnabled = false;
            var pilot = Object.FindFirstObjectByType<ModuleBoot>().gameObject.AddComponent<AutoPilot>();
            var runner = Object.FindFirstObjectByType<ChapterRunner>();
            float limit = Time.realtimeSinceStartup + 1100;
            while (!AutoPilot.Done && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsTrue(AutoPilot.Done, "walkthrough did not finish in time; state " + runner.State + " in " + runner.Chapter?.id);
            Assert.AreEqual(0, pilot.CheckFailures, "A walkthrough check failed; inspect walkthrough.log");
            Assert.AreEqual(LessonState.Complete, runner.State);
            Assert.AreEqual(10, SaveSystem.Data.completedChapters.Count, AutoPilot.Summary);
            Assert.AreEqual(0, AutoPilot.PointerFailures, "Every active control must be reachable by an EventSystem pointer raycast");
            Assert.Greater(AutoPilot.PresenterFrames, 100, "Narration visibility was sampled throughout the walkthrough");
            Assert.AreEqual(0, AutoPilot.PresenterFailures, "Every sampled narration frame must show Niko, in the room or floating");
            Assert.AreEqual(65, SaveSystem.Data.cues.Count, "every StoryboardCore cue (66, S02_L05 retired with the second scan) completed exactly once: " + AutoPilot.Summary);
            Assert.Greater(SaveSystem.Data.evidence.Count, 0, "adaptive activities recorded evidence");
            CollectionAssert.AreEquivalent(authored, hud.canvas.GetComponentsInChildren<RectTransform>(true).Select(t => t.GetInstanceID()).ToArray(), "Gameplay must reuse exactly the saved Canvas objects");
            Assert.AreEqual(1f, Time.timeScale, "pause/freeze never touch timeScale");
        }
    }
}
