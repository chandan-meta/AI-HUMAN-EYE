using System.Linq;
using HumanEye.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HumanEye.Tests
{
    public class CanvasAuthoringTests
    {
        [Test]
        public void SavedSceneContainsEditableBoundCanvasBeforePlay()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            Assert.IsNotNull(hud.canvas);
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(hud.canvas), "The scene Canvas is unpacked by Builder.ApplyRedesign: it is the HUD's authority");
            Assert.IsNull(hud.GetComponent("UIDocument"), "The active HUD must not depend on runtime UI Toolkit elements");
            Assert.Greater(hud.canvas.GetComponentsInChildren<RectTransform>(true).Length, 300);
            CollectionAssert.AllItemsAreUnique(hud.bindings.Select(b => b.key));
            Assert.IsTrue(hud.bindings.All(b => b.target), "Every saved binding must point to a real Hierarchy object");
            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasAuthoring.PrefabPath);
            foreach (var graphic in savedPrefab.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.IsNotNull(graphic.GetComponent<CanvasRenderer>(), graphic.name + " must have a saved CanvasRenderer before any rendering or pointer raycast");
            foreach (var button in hud.canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Assert.IsNotNull(button.targetGraphic, button.name + " has no click surface");
                Assert.IsTrue(button.targetGraphic.raycastTarget, button.name + " cannot receive pointer hits");
                var image = button.targetGraphic as UnityEngine.UI.Image;
                Assert.IsNotNull(image, button.name + " must use an exported Figma image as its click surface");
                AssertFigmaSprite(image.sprite, button.name);
            }
            foreach (var decoration in hud.canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Where(g => g.name.StartsWith("Figma ")))
            {
                Assert.IsFalse(decoration.raycastTarget, decoration.name + " must not cover interactive controls");
                Assert.AreEqual(decoration.transform.parent.gameObject.layer, decoration.gameObject.layer, decoration.name + " must share its parent's rendering layer");
            }
            int insetLabels = 0;
            foreach (var text in hud.canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.GetComponentInParent<UnityEngine.UI.Button>(true)) continue;
                var surface = text.GetComponentsInParent<UnityEngine.UI.Image>(true).FirstOrDefault(image => image.sprite &&
                    (image.sprite.name.StartsWith("Panel") || image.sprite.name == "SpeakerTag"));
                if (!surface || surface.sprite.name != "PanelInset") continue;
                Assert.AreEqual(Color.white, text.color, text.name + " must remain readable on the dark inset artwork");
                insetLabels++;
            }
            Assert.Greater(insetLabels, 0, "Saved inset labels must be covered by the contrast check");
            foreach (var key in new[] { "settings-close", "ab-INSIDE" })
            {
                var label = hud.Element(key).GetComponentInChildren<UnityEngine.UI.Text>(true);
                Assert.IsNotNull(label, key + " must keep its label even inside an inactive panel");
                Assert.AreEqual(Color.white, label.color, key + " must retain white text on its primary Figma artwork");
            }
            foreach (var field in hud.canvas.GetComponentsInChildren<UnityEngine.UI.InputField>(true))
            {
                Assert.IsNotNull(field.targetGraphic, field.name + " has no input surface");
                Assert.IsTrue(field.targetGraphic.raycastTarget, field.name + " cannot receive pointer input");
            }
            foreach (var toggle in hud.canvas.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
            {
                Assert.IsNotNull(toggle.targetGraphic, toggle.name + " has no toggle surface");
                Assert.IsTrue(toggle.targetGraphic.raycastTarget, toggle.name + " cannot receive pointer input");
                Assert.IsNotNull(toggle.graphic, toggle.name + " has no visible checked state");
            }
            foreach (var slider in hud.canvas.GetComponentsInChildren<UnityEngine.UI.Slider>(true))
            {
                var track = slider.transform.Find("Track");
                Assert.IsNotNull(track, slider.name + " has no saved track");
                var trackImage = track.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(trackImage, slider.name + " has no track image");
                Assert.IsTrue(trackImage.raycastTarget, slider.name + " cannot receive clicks on its track");
                Assert.IsNotNull(slider.targetGraphic, slider.name + " has no handle surface");
                Assert.IsTrue(slider.targetGraphic.raycastTarget, slider.name + " cannot receive pointer input");
            }
            foreach (var scroll in hud.canvas.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true))
                if (scroll.TryGetComponent<UnityEngine.UI.Image>(out var background) && AssetDatabase.GetAssetPath(background.sprite).StartsWith("Assets/HumanEye/UI/Figma/"))
                    Assert.IsTrue(background.raycastTarget, scroll.name + " must receive drag input over its Figma background");
            var scan = hud.Element("ab-INSIDE").GetComponent<HudState>();
            Assert.IsNotNull(scan);
            Assert.IsNull(scan.GetComponent<CompactButtonLayout>(), "The primary Inside button must keep its centered Figma label layout");
            Assert.IsInstanceOf<UnityEngine.UI.Image>(scan.target);
            AssertFigmaSprite(scan.normalSprite, "Inside default state");
            AssertFigmaSprite(scan.selectedSprite, "Inside selected state");
            AssertFigmaSprite(scan.disabledSprite, "Inside unavailable state");
            var scanImage = (UnityEngine.UI.Image)scan.target;
            scan.Set("unavailable", false); scan.Set("selected", false);
            Assert.AreSame(scan.normalSprite, scanImage.sprite);
            scan.Set("selected", true); Assert.AreSame(scan.selectedSprite, scanImage.sprite);
            scan.Set("unavailable", true); Assert.AreSame(scan.disabledSprite, scanImage.sprite);
            scan.Set("unavailable", false); scan.Set("selected", false);
            Assert.AreSame(scan.normalSprite, scanImage.sprite);
            foreach (var key in new[] { "part-buttons", "journey", "act-options", "act-slots", "act-buttons", "ask-suggestions", "review-topics", "labels-layer" })
                Assert.Greater(hud.Element(key).childCount, 0, key + " must contain saved slots before Play");
            var start = hud.Element("start-begin"); var oldName = start.name;
            try { start.name = "My renamed Start button"; Assert.AreSame(start, hud.Element("start-begin"), "Manual names must not break serialized bindings"); }
            finally { start.name = oldName; }
        }

        static void AssertFigmaSprite(Sprite sprite, string context)
        {
            Assert.IsNotNull(sprite, context + " must have a saved Figma PNG sprite");
            var path = AssetDatabase.GetAssetPath(sprite);
            StringAssert.StartsWith("Assets/HumanEye/UI/Figma/", path, context);
            StringAssert.EndsWith(".png", path, context);
        }
    }
}
