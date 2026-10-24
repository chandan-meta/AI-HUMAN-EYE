using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    public class NikoCompanionTests
    {
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
        int previousSizeIndex;
        System.Reflection.MethodInfo selectSize;

        [SetUp]
        public void SupportedViewport()
        {
            var assembly = typeof(UnityEditor.EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView", true);
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            previousSizeIndex = (int)type.GetProperty("selectedSizeIndex").GetValue(gameView);
            selectSize = type.GetMethod("SizeSelectionCallback");
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            for (int i = 0; i < count; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                var sizeType = size.GetType();
                if ((int)sizeType.GetProperty("width").GetValue(size) != 1366 || (int)sizeType.GetProperty("height").GetValue(size) != 768) continue;
                selectSize.Invoke(gameView, new object[] { i, null }); return;
            }
            Assert.Fail("Unity's supported1366 x768 Game view must be available for guide clearance tests");
        }

        [TearDown]
        public void RestoreViewport()
        {
            if (gameView && selectSize != null) selectSize.Invoke(gameView, new object[] { previousSizeIndex, null });
        }
#endif

        [UnityTest, Timeout(90000)]
        public IEnumerator SceneSixQuizAndObjectiveLeaveActualNikoClear()
        {
            bool hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
            string backup = PlayerPrefs.GetString(SaveSystem.Key, "");
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>();
            var runner = stage.runner; var original = runner.chapters[4];
            var fixture = Object.Instantiate(original);
            try
            {
                SaveSystem.Data.settings.reducedMotion = true;
                fixture.steps = new[] {
                    new Step { kind = StepKind.Shot, key = "S06_Vitreous", value = 0 },
                    new Step { kind = StepKind.Do, key = "restore" },
                    new Step { kind = StepKind.Offer, key = "item:acc_near" }
                };
                runner.chapters[4] = fixture;
                stage.hud.StartModule(4);
                float deadline = Time.realtimeSinceStartup + 15;
                while ((stage.activities.Card == null || !stage.Companion.Floating || stage.Companion.Grow < 1) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsNotNull(stage.activities.Card, "The actual Scene06 accommodation check must open");
                yield return new WaitForSecondsRealtime(.8f);
                Assert.IsTrue(stage.Companion.Visible && stage.Companion.Floating);
                foreach (var key in new[] { "next-step", "activity" })
                {
                    var panel = stage.hud.Element(key);
                    Assert.IsTrue(panel.gameObject.activeInHierarchy, key + " must actually be displayed");
                    var group = panel.GetComponent<CanvasGroup>();
                    if (group) Assert.Greater(group.alpha, .9f);
                    var corners = new Vector3[4]; panel.GetWorldCorners(corners);
                    var rect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var cachedObjective = typeof(NikoFloatingCompanion).GetField("objective", flags).GetValue(stage.Companion);
                    var cachedSlot = typeof(NikoFloatingCompanion).GetField("slot", flags).GetValue(stage.Companion);
                    Debug.Log($"NIKO_CLEARANCE {key} panel={rect} cachedObjective={cachedObjective} slot={cachedSlot} chosen={stage.Companion.ScreenRect} drawn={stage.Companion.DrawnRect} mode={stage.Companion.Location} camera={stage.hud.mainCamera.pixelRect} screen={Screen.width}x{Screen.height}");
                    Assert.IsFalse(stage.Companion.DrawnRect.Overlaps(rect), key + " must leave the actual Scene06 guide's hands and boots clear");
                }
            }
            finally
            {
                runner.chapters[4] = original; runner.ReturnToMenu(); Object.Destroy(fixture);
                if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
                else PlayerPrefs.DeleteKey(SaveSystem.Key);
                SaveSystem.Load();
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ActivityAndAskPanelsReserveCompanionSpace()
        {
            bool hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
            string backup = PlayerPrefs.GetString(SaveSystem.Key, "");
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>();
            var hud = stage.hud; var presenter = stage.Companion;
            try
            {
                hud.StartModule(1); yield return null; yield return null;
                stage.runner.enabled = hud.enabled = presenter.enabled = false;
                stage.narrator.Stop();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var type = typeof(NikoFloatingCompanion);
                var choose = type.GetMethod("Choose", flags);
                var slot = type.GetField("slot", flags);
                var columns = new[] { "right-column", "activity", "ask-panel" };
                foreach (var key in columns) hud.Element(key).gameObject.SetActive(false);
                float w = presenter.heightShare * .9f / hud.mainCamera.aspect;
                foreach (var key in columns)
                {
                    var panel = hud.Element(key);
                    var group = panel.GetComponent<CanvasGroup>();
                    panel.gameObject.SetActive(group != null);
                    if (group) group.alpha = 0;
                    Canvas.ForceUpdateCanvases();
                    choose.Invoke(presenter, new object[] { hud.mainCamera, w, true, false });
                    Assert.AreEqual((float)Screen.width, (float)type.GetField("columnLeft", flags).GetValue(presenter), .01f, key + " must not reserve space while hidden");

                    // Reproduce a guide already at the right edge when a check replaces the hidden tools column.
                    slot.SetValue(presenter, new Vector2(1 - 24f / Screen.width - w * .5f, 24f / Screen.height));
                    type.GetField("targetRight", flags).SetValue(presenter, true);
                    panel.gameObject.SetActive(true);
                    if (group) group.alpha = 1;
                    var next = hud.Element("next-step");
                    next.gameObject.SetActive(key == "activity");
                    var nextGroup = next.GetComponent<CanvasGroup>();
                    if (nextGroup) nextGroup.alpha = 1;
                    Canvas.ForceUpdateCanvases();
                    choose.Invoke(presenter, new object[] { hud.mainCamera, w, false, false });
                    var corners = new Vector3[4]; panel.GetWorldCorners(corners);
                    Assert.AreEqual(corners[0].x, (float)type.GetField("columnLeft", flags).GetValue(presenter), .01f, key + " must reserve its own screen rectangle");
                    var moved = (Vector2)slot.GetValue(presenter);
                    Assert.LessOrEqual((moved.x + w * .5f) * Screen.width, corners[0].x - 20, key + " must clear the whole guide immediately, without sliding behind it");
                    if (key == "activity")
                    {
                        next.GetWorldCorners(corners);
                        var objective = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                        var guide = new Rect((moved.x - w * .5f) * Screen.width, moved.y * Screen.height, w * Screen.width, presenter.heightShare * Screen.height);
                        Assert.IsFalse(guide.Overlaps(objective), "The quiz and bottom instruction must both leave the whole guide clear");
                    }
                    panel.gameObject.SetActive(false);
                }
            }
            finally
            {
                hud.enabled = presenter.enabled = stage.runner.enabled = true;
                stage.runner.ReturnToMenu();
                if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
                else PlayerPrefs.DeleteKey(SaveSystem.Key);
                SaveSystem.Load();
            }
        }

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
