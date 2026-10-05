using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Incremental, idempotent art/UI pass applied to the SAVED scene. It never regenerates the scene, so the
    /// hand-edited Canvas and other manual overrides survive. Batch: -executeMethod HumanEye.EditorTools.PolishPatch.Apply
    public static class PolishPatch
    {

        [MenuItem("Tools/Human Eye/Authoring/Apply polish patch (keeps scene edits)", priority = 3)]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var hud = stage.hud;
            var env = stage.observatory.transform;

            // UI: the question card docks left so the model it asks about stays visible and tappable (tap-part questions,
            // hint glows); the backdrop no longer dims the scene to near-black.
            Rect(hud, "activity", "Card", r => { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, .5f); r.offsetMin = new Vector2(16, 110); r.offsetMax = new Vector2(16 + 560, -140); });
            Graphic(hud, "activity", g => g.color = new Color(.008f, .025f, .05f, .18f));
            // Menu: the title card sits low so the hero eye above it is visible (it was fully hidden behind the card).
            Rect(hud, "start-screen", "Card", r => { r.anchorMin = new Vector2(.24f, 0); r.anchorMax = new Vector2(.76f, 0); r.pivot = new Vector2(.5f, 0); r.anchoredPosition = new Vector2(0, 28); r.sizeDelta = new Vector2(0, 480); });
            var menuCam = Array.Find(stage.cams.shots, s => s.name == "Menu");
            stage.menuPoint.position = OnScreen(menuCam.transform, menuCam.Lens.FieldOfView, new Vector2(.5f, .76f), stage.menuPoint.position.z);
            stage.menuPoint.localScale = Vector3.one * .55f;


            // Background quieter than the subject: the distant brass gallery rails and ribs reflected brightly and
            // crossed every shot at eye height. Near brass (plinth rim, lens frames) keeps the original material.
            var quiet = Lit("BrassQuiet", new Color(.26f, .18f, .09f), .35f, .6f);
            foreach (Transform t in env)
                if (t.name.StartsWith("Rib") || t.name.EndsWith("Rail")) t.GetComponent<MeshRenderer>().sharedMaterial = quiet;

            // Retinal-image insets: the square render texture was stretched into a wide frame, flattening the apple
            // so its orientation (the point of the comparison) was hard to read.
            foreach (var key in new[] { "inset-retina-image", "inset-perception-image" })
            {
                var image = hud.Element(key);
                var fit = image.GetComponent<AspectRatioFitter>(); if (fit) Object.DestroyImmediate(fit, true);   // fights the layout group; superseded
                var le = image.GetComponent<LayoutElement>();
                float side = le.preferredHeight > 0 ? le.preferredHeight : le.minHeight;
                le.preferredWidth = le.minWidth = side; le.flexibleWidth = 0;   // square, like the render texture
                var group = image.parent.GetComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = false; group.childAlignment = TextAnchor.UpperCenter;
                EditorUtility.SetDirty(le); EditorUtility.SetDirty(group);
                Debug.Log($"HUMAN_EYE_INSET {key} square side {side}");
            }

            EndRecap(hud);

            EditorUtility.SetDirty(stage); EditorUtility.SetDirty(stage.menuPoint);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            PrefabUtility.SavePrefabAsset(AssetDatabase.LoadAssetAtPath<GameObject>(CanvasAuthoring.PrefabPath));
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_POLISH_PATCHED");
        }

        /// Conclusion: the end card was four bare buttons. Adds an authored title and recap line (filled from saved progress by Hud).
        static void EndRecap(Hud hud)
        {
            var card = hud.Element("end-menu").Find("Card");
            var sample = card.GetComponentInChildren<Text>(true);
            Text Line(string name, int index, Font font, int size, string text)
            {
                var t = card.Find(name)?.GetComponent<Text>();
                if (!t)
                {
                    t = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement)).GetComponent<Text>();
                    t.transform.SetParent(card, false);
                }
                t.transform.SetSiblingIndex(index);
                t.font = font; t.fontSize = size; t.text = text; t.color = sample.color; t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
                t.GetComponent<LayoutElement>().minHeight = size * 1.6f;
                EditorUtility.SetDirty(t);
                return t;
            }
            var bold = AssetDatabase.LoadAssetAtPath<Font>(Builder.Root + "ThirdParty/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Bold.ttf");
            Line("end-title", 0, bold ? bold : sample.font, 30, "Investigation complete");
            var recap = Line("end-recap", 1, sample.font, 18, "(recap of your investigation)");
            if (!Array.Exists(hud.bindings, b => b.key == "end-recap"))
            {
                var list = new System.Collections.Generic.List<Hud.Binding>(hud.bindings) { new() { key = "end-recap", target = (RectTransform)recap.transform } };
                hud.bindings = list.ToArray(); EditorUtility.SetDirty(hud);
            }
        }

        /// Sets a Canvas element on the scene instance and on its prefab source, so neither a stale override nor the prefab disagrees.
        static void Rect(Hud hud, string key, string child, Action<RectTransform> set)
        {
            var inst = hud.Element(key); if (child != null) inst = (RectTransform)inst.Find(child);
            set(inst); EditorUtility.SetDirty(inst);
            var src = PrefabUtility.GetCorrespondingObjectFromSource(inst);
            if (src) { set(src); EditorUtility.SetDirty(src); }
        }

        static void Graphic(Hud hud, string key, Action<Graphic> set)
        {
            var g = hud.Element(key).GetComponent<Graphic>();
            set(g); EditorUtility.SetDirty(g);
            var src = PrefabUtility.GetCorrespondingObjectFromSource(g);
            if (src) { set(src); EditorUtility.SetDirty(src); }
        }

        /// World point at a viewport position of a 16:9 shot, on the plane z = depth.
        static Vector3 OnScreen(Transform cam, float fov, Vector2 viewport, float depth)
        {
            float ty = Mathf.Tan(fov * Mathf.Deg2Rad * .5f), tx = ty * 16f / 9;
            var dir = cam.rotation * new Vector3((viewport.x * 2 - 1) * tx, (viewport.y * 2 - 1) * ty, 1);
            return cam.position + dir * ((depth - cam.position.z) / dir.z);
        }

        static Material Lit(string name, Color colour, float smoothness, float metallic)
        {
            var path = Builder.Gen + "Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", colour); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// Layout probe for placement decisions: shot cameras, Niko anchors and the main subjects.
        public static void Dump()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var sb = new System.Text.StringBuilder();
            foreach (var c in stage.cams.shots) sb.AppendLine($"SHOT {c.name} pos {c.transform.position} fwd {c.transform.forward} fov {c.Lens.FieldOfView:0.0}");
            foreach (var a in stage.nikoAnchors) sb.AppendLine($"ANCHOR {a.name} pos {a.position} rot {a.eulerAngles}");
            foreach (var t in new[] { stage.menuPoint, stage.heroPoint, stage.cutPoint, stage.brainRoot, stage.niko.transform, stage.pedestal, stage.appleRoot, stage.treeRoot, stage.lampRoot })
                sb.AppendLine($"OBJ {t.name} pos {t.position} scale {t.lossyScale}");
            foreach (Transform t in stage.observatory.transform)
                if (t.name.StartsWith("Bench") || t.name.Contains("Plinth") || t.name.Contains("Rail")) sb.AppendLine($"ENV {t.name} pos {t.position} active {t.gameObject.activeSelf}");
            System.IO.Directory.CreateDirectory("Logs/wow");
            System.IO.File.WriteAllText("Logs/wow/layout.txt", sb.ToString());
            Debug.Log("HUMAN_EYE_LAYOUT_DUMPED");
        }
    }
}
