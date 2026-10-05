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
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(hud.canvas), "Canvas should be an editable prefab instance");
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
            }
            Assert.IsTrue((hud.canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord3) != 0, "Glass meshes need their saved shape channels");
            var panels = hud.canvas.GetComponentsInChildren<GlassPanel>(true);
            Assert.Greater(panels.Length, 50);
            Assert.IsTrue(panels.All(p => p.material && p.material.shader.name == "HumanEye/UI Glass"));
            var scan = hud.Element("ab-SCAN").GetComponent<HudState>();
            scan.Set("selected", false); float idle = scan.surface.glow;
            scan.Set("selected", true); Assert.Greater(scan.surface.glow, idle);
            scan.Set("unavailable", true); Assert.Less(scan.surface.glow, .2f);
            scan.Set("unavailable", false); scan.Set("selected", false); Assert.AreEqual(idle, scan.surface.glow);
            foreach (var key in new[] { "part-buttons", "journey", "act-options", "act-slots", "act-buttons", "ask-suggestions", "review-topics", "labels-layer" })
                Assert.Greater(hud.Element(key).childCount, 0, key + " must contain saved slots before Play");
            var start = hud.Element("start-begin"); var oldName = start.name;
            try { start.name = "My renamed Start button"; Assert.AreSame(start, hud.Element("start-begin"), "Manual names must not break serialized bindings"); }
            finally { start.name = oldName; }
        }
    }
}
