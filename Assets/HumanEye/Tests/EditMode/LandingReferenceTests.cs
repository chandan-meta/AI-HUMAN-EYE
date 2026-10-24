using System.Linq;
using HumanEye.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HumanEye.Tests
{
    public class LandingReferenceTests
    {
        [Test]
        public void LandingBrandUsesLiveTextAndItsTitleGeneratesVisibleGlyphs()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            var brand = hud.Element("start-screen").Find("Card/Figma Brand");
            Assert.IsNotNull(brand);
            Assert.IsFalse(brand.GetComponent<Image>().enabled, "The previous baked title image must not be displayed.");
            var labels = brand.GetComponentsInChildren<Text>(true);
            var title = labels.Single(t => t.text.Contains("THE HUMAN"));
            foreach (var text in labels)
            {
                Assert.IsTrue(text.gameObject.activeSelf, "Landing copy remains editable, active Unity text.");
                Assert.IsNotNull(text.font);
                Assert.IsFalse(text.raycastTarget, "Decorative copy must not intercept menu input.");
            }
            Assert.IsTrue(labels.Any(t => t.text.Contains("LESSON WITH NIKO")));
            Assert.IsTrue(labels.Any(t => t.text == "Discover how light becomes sight"));
            Assert.IsTrue(title.supportRichText, "The title keeps its violet EYE accent without baking text.");
            var generated = title.cachedTextGenerator;
            Assert.IsTrue(generated.Populate(title.text, title.GetGenerationSettings(new Vector2(648, 88))));
            Assert.Greater(generated.vertexCount, 4, "Font line metrics must not truncate the whole title.");
            Assert.AreEqual(1, generated.lineCount, "The title must stay on one line inside its reference column.");
        }

        [Test]
        public void LandingRetainsBoundInteractiveActionsAndOriginalFigmaArtwork()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            Assert.AreEqual(159, hud.bindings.Length);
            CollectionAssert.AllItemsAreUnique(hud.bindings.Select(b => b.key));
            Assert.IsTrue(hud.bindings.All(b => b.target));
            Assert.AreEqual(165, hud.canvas.GetComponentsInChildren<Button>(true).Length);
            foreach (var key in new[] { "start-begin", "start-continue", "start-help", "start-chapters", "start-settings" })
            {
                var button = hud.Element(key).GetComponent<Button>();
                var surface = button.targetGraphic as Image;
                Assert.IsNotNull(surface, key + " keeps its original Image input surface.");
                Assert.IsTrue(surface.raycastTarget, key + " must still accept pointer input.");
                Assert.IsNotNull(surface.sprite);
                StringAssert.StartsWith(FigmaSkin.Folder, AssetDatabase.GetAssetPath(surface.sprite));
                Assert.IsNotNull(button.GetComponentInChildren<Text>(true), key + " keeps its live label.");
            }
            Assert.AreEqual(43, AssetDatabase.FindAssets("t:Texture2D", new[] { FigmaSkin.Folder.TrimEnd('/') }).Length,
                "The original exported texture set remains available.");
        }

        [Test]
        public void GlossyEyeUsesDielectricMaterialsAndAStaticStudioReflection()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            const string materials = "Assets/HumanEye/Generated/Materials/";
            var cornea = AssetDatabase.LoadAssetAtPath<Material>(materials + "Eye_Cornea.mat");
            var sclera = AssetDatabase.LoadAssetAtPath<Material>(materials + "LocalEye_ScleraOuter.mat");
            Assert.IsNotNull(cornea); Assert.IsNotNull(sclera);
            Assert.AreEqual(1, cornea.GetFloat("_PhysicalEye"));
            Assert.AreEqual(1.376f, cornea.GetFloat("_IOR"), .001f);
            Assert.GreaterOrEqual(cornea.GetFloat("_Gloss"), .97f);
            Assert.AreEqual(1, sclera.GetFloat("_PhysicalEye"));
            Assert.GreaterOrEqual(sclera.GetFloat("_Gloss"), .9f);
            Assert.IsNotNull(sclera.GetTexture("_BaseMap"), "The sclera keeps its vein texture and existing UVs.");
            var probeObject = GameObject.Find("Landing Studio Reflection");
            Assert.IsNotNull(probeObject);
            var probe = probeObject.GetComponent<ReflectionProbe>();
            Assert.IsNotNull(probe);
            Assert.AreEqual(ReflectionProbeMode.Custom, probe.mode, "Menu reflections must not require a realtime cubemap every frame.");
            Assert.IsNotNull(probe.customBakedTexture);
            StringAssert.StartsWith("Assets/HumanEye/Generated/LandingReference/", AssetDatabase.GetAssetPath(probe.customBakedTexture));
        }
    }
}
