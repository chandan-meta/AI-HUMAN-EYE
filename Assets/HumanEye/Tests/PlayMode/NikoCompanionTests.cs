using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    public class NikoCompanionTests
    {
        [UnityTest, Timeout(90000)]
        public IEnumerator ReplaysAnswersAndPauseKeepTheFloatingNiko()
        {
            string backup = PlayerPrefs.GetString(SaveSystem.Key, "");
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>();
            try
            {
                stage.hud.StartModule(1); yield return null; yield return null;
                stage.runner.enabled = false; stage.narrator.Stop(); stage.cams.Snap("S03_Part_lens");
                var presenter = stage.Companion;
                foreach (var source in new[] { NarrationSource.Replay, NarrationSource.Answer })
                {
                    stage.StartCoroutine(stage.narrator.Play("S03_L01", source));
                    yield return null; yield return null;
                    for (float guard = 0; presenter.Grow < 1 && guard < 2; guard += Time.unscaledDeltaTime) yield return null;   // the first line brings him in
                    Assert.IsTrue(presenter.Visible && presenter.Floating, source.ToString());
                    Assert.AreNotEqual(0, stage.hud.mainCamera.cullingMask & (1 << stage.niko.gameObject.layer), "The lesson camera draws the one 3D Niko");
                    Assert.IsTrue(stage.hud.mainCamera.WorldToViewportPoint(stage.niko.head.position).z > stage.hud.mainCamera.nearClipPlane, "He floats in front of the lens");
                    stage.runner.SetPaused(true); float voiceTime = stage.narrator.voice.time;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.IsTrue(presenter.Visible, "Pausing never hides the guide");
                    Assert.AreEqual(voiceTime, stage.narrator.voice.time, .03f);
                    stage.runner.SetPaused(false); stage.narrator.Stop(); yield return null;
                }
                stage.narrator.voice.PlayOneShot(stage.closingLine); yield return null; yield return null;
                Assert.IsTrue(presenter.Visible && presenter.Floating, "Supplemental speech keeps the same floating Niko");
                stage.runner.SetPaused(true); yield return null;
                Assert.IsTrue(presenter.Visible && stage.narrator.Speaking, "Paused supplemental speech retains Niko");
                stage.runner.SetPaused(false); yield return null;
                Assert.IsTrue(stage.narrator.voice.isPlaying, "Supplemental speech resumes after pause");
                stage.narrator.Stop();
                stage.runner.ReturnToMenu(); yield return null;
                Assert.IsFalse(presenter.Floating || stage.niko.gameObject.activeSelf, "The menu is without Niko until his next line");
                Assert.AreEqual(1, Object.FindObjectsByType<NikoFloatingCompanion>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
                Assert.AreEqual(1, Object.FindObjectsByType<NikoRig>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "One Niko");
                Assert.IsFalse(System.Array.Exists(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None), c => c.targetTexture && (c.cullingMask & (1 << stage.niko.gameObject.layer)) != 0), "No camera renders him into a picture");
            }
            finally
            {
                stage.runner.SetPaused(false); stage.runner.enabled = true; stage.narrator.Stop();
                PlayerPrefs.SetString(SaveSystem.Key, backup);
            }
        }
    }
}
