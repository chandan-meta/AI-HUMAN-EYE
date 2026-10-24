using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HumanEye.Tests
{
    public class CinematicControlTests
    {
        string backup;
        bool hadSave;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
        int previousSizeIndex;
        System.Reflection.MethodInfo selectSize;

        void SetTestViewport()
        {
            // Screen.SetResolution does not resize an Editor Game view. Select Unity's built-in desktop viewport.
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
                if ((int)sizeType.GetProperty("width").GetValue(size) != 1366 ||
                    (int)sizeType.GetProperty("height").GetValue(size) != 768) continue;
                selectSize.Invoke(gameView, new object[] { i, null });
                return;
            }
            Assert.Fail("Unity's 1366 x 768 desktop Game view size must be available for pointer tests");
        }
#endif

        [SetUp]
        public void Backup()
        {
            hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
            backup = PlayerPrefs.GetString(SaveSystem.Key, "");
            Clock.Paused = Clock.Frozen = false;
            Clock.FastForward = 1;
#if UNITY_EDITOR
            SetTestViewport();
#endif
        }

        [TearDown]
        public void Restore()
        {
            var runner = Object.FindFirstObjectByType<ChapterRunner>();
            if (runner) runner.ReturnToMenu();
            if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
            else PlayerPrefs.DeleteKey(SaveSystem.Key);
            SaveSystem.Load();
            Clock.Paused = Clock.Frozen = false;
            Clock.FastForward = 1;
#if UNITY_EDITOR
            if (gameView && selectSize != null) selectSize.Invoke(gameView, new object[] { previousSizeIndex, null });
#endif
        }

        static IEnumerator Click(Hud hud, string key)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var button = hud.Control<Button>(key);
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.IsInteractable(), key + " must be usable");
            float deadline = Time.realtimeSinceStartup + 1;
            while (button.targetGraphic.canvasRenderer.GetInheritedAlpha() < .95f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.Greater(button.targetGraphic.canvasRenderer.GetInheritedAlpha(), .9f, key + " must be visible");
            var rect = (RectTransform)button.transform;
            var camera = hud.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, $"{key} must receive pointer input at {pointer.position} in {Screen.width} x {Screen.height}");
            Assert.AreEqual(button.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), key + " is covered");
            pointer.pointerPress = button.gameObject;
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.eligibleForClick = true;
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator IntroPauseSettingsAndRepeatedSkipRestoreLesson()
        {
            SceneManager.LoadScene("HumanEye_Observatory");
            yield return null; yield return null;
            Assert.GreaterOrEqual(Screen.width, 1024, "Pointer test requires a supported desktop viewport");
            Assert.GreaterOrEqual(Screen.height, 768, "Pointer test requires a supported desktop viewport");
            var stage = Object.FindFirstObjectByType<Stage>();
            var hud = stage.hud;
            SaveSystem.Data.settings.reducedMotion = false;
            SaveSystem.Data.settings.skipCameraMotion = false;
            stage.timing = Stage.CinematicTiming.Production;

            foreach (var introPoint in new[] { 0f, 8.75f, 19.5f })   // the dark, the cutaway burst, the hand-over
            {
                hud.StartModule(0);
                yield return null; yield return null;
                Assert.IsTrue(stage.IntroPlaying);
                float deadline = Time.realtimeSinceStartup + 60;
                while (stage.IntroTime < introPoint && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.IsTrue(stage.IntroPlaying, "The requested skip point must occur inside the intro");
                Assert.GreaterOrEqual(stage.IntroTime, introPoint);

                if (introPoint > 0)
                {
                    bool transition = introPoint < 18;
                    if (transition)
                    {
                        Assert.IsNotNull(stage.anatomyTransition, "The cutaway needs particles");
                        Assert.Greater(stage.anatomyTransition.particleCount, 0, "The cutaway burst is alive before Pause");
                    }
                    var overlay = hud.Element("intro-overlay"); var topbar = hud.Element("topbar");
                    Assert.AreEqual(overlay.parent, topbar.parent);
                    Assert.Less(overlay.GetSiblingIndex(), topbar.GetSiblingIndex(), "The opaque letterbox must render behind Pause and Settings");
                    yield return Click(hud, "btn-settings");
                    Assert.IsTrue(Clock.Paused && hud.Element("settings-panel").gameObject.activeInHierarchy);
                    float held = stage.IntroTime;
                    yield return null;
                    float particleTime = transition ? stage.anatomyTransition.time : 0;
                    if (transition) Assert.IsTrue(stage.anatomyTransition.isPaused, "Settings pauses the anatomy burst");
                    yield return new WaitForSecondsRealtime(.2f);
                    Assert.AreEqual(held, stage.IntroTime, "Settings holds the cinematic");
                    if (transition) Assert.AreEqual(particleTime, stage.anatomyTransition.time, .001f, "Settings holds particle simulation time");
                    if (introPoint > 19)
                    {
                        hud.Element("settings-panel").GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
                        yield return null;
                    }
                    yield return Click(hud, "settings-close");
                    Assert.IsFalse(Clock.Paused);

                    yield return Click(hud, "btn-pause");
                    Assert.IsTrue(Clock.Paused && hud.Element("pause-overlay").gameObject.activeInHierarchy);
                    held = stage.IntroTime;
                    stage.SkipIntro(); stage.SkipIntro();
                    // The pointer callback runs before LateUpdate, when the brain applies this frame's already-authored dolly pose.
                    yield return null;
                    Assert.AreEqual(held, stage.IntroTime, "Pause holds the cinematic clock immediately");
                    var position = stage.cams.brain.transform.position;
                    particleTime = transition ? stage.anatomyTransition.time : 0;
                    yield return new WaitForSecondsRealtime(.2f);
                    Assert.IsTrue(stage.IntroPlaying, "A skip requested while paused is ignored");
                    Assert.AreEqual(held, stage.IntroTime, "Pause holds the cinematic clock");
                    Assert.Less(Vector3.Distance(position, stage.cams.brain.transform.position), .005f, "Pause holds the camera");
                    if (transition) Assert.AreEqual(particleTime, stage.anatomyTransition.time, .001f, "Pause holds the anatomy burst");
                    yield return Click(hud, "pause-resume");
                    Assert.IsFalse(Clock.Paused);
                    yield return null;
                    Assert.IsTrue(stage.IntroPlaying, "Paused skip must not fire after Resume");
                    yield return Click(hud, "btn-skip-intro");
                }
                for (int repeat = 0; repeat < 8; repeat++) stage.SkipIntro();
                yield return null; yield return null;

                Assert.IsFalse(stage.IntroPlaying);
                Assert.IsFalse(Clock.Paused);
                Assert.AreEqual("S02_NikoMedium", stage.cams.Active.name);
                Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .001f);
                Assert.IsFalse(stage.human.gameObject.activeSelf);
                Assert.IsFalse(stage.anatomyTransition.gameObject.activeSelf, "Skip clears anatomy particles");
                Assert.IsFalse(stage.human.reveal.organs.gameObject.activeSelf, "Skip clears Rack 04");
                foreach (var part in new[] { stage.human.skin, stage.human.garment, stage.human.reveal.muscles,
                    stage.human.reveal.skeleton, stage.human.reveal.organs, stage.human.reveal.brain })
                    foreach (var renderer in part.main)
                        foreach (var material in renderer.sharedMaterials)
                            Assert.AreEqual(0, material.GetVector("_BodyFade").z, .001f, "Skip clears the shoulder fade on " + part.name);
                Assert.IsTrue(stage.NikoWaiting ? !stage.niko.gameObject.activeSelf : stage.narrator.Speaking && stage.niko.gameObject.activeSelf, "After Skip Niko is in only with his first line");
                Assert.AreEqual(0, stage.introVolume.weight);
                Assert.AreEqual(0, hud.Element("fade").GetComponent<CanvasGroup>().alpha);
                Assert.IsTrue(hud.Element("chapter-title").gameObject.activeInHierarchy, "Lesson heading returns after Skip");
                Assert.IsTrue(hud.Element("btn-chapters").gameObject.activeInHierarchy, "The tools return after Skip");
                for (float wait = 0; !stage.narrator.Speaking && wait < 8; wait += Time.unscaledDeltaTime) yield return null;
                yield return null; yield return null;
                Assert.IsTrue(stage.narrator.Speaking && !stage.NikoWaiting && stage.niko.gameObject.activeSelf, "His first line brings Niko in");
                if (!stage.Short) Assert.Greater(stage.nikoSparkle.particleCount, 100, "He arrives in a burst of sparkles");
                hud.ReturnToMenu(); yield return null;
                Assert.IsFalse(stage.IntroPlaying);
                Assert.IsTrue(stage.NikoWaiting && !stage.niko.gameObject.activeSelf, "The start screen is without Niko");
            }
        }
    }
}
