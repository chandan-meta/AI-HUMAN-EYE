using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace HumanEye.EditorTools
{
    // Explicit authoring operation only. All surfaces and icons are saved before Play.
    public static class ReferenceUiStyle
    {
        // Repair components only: never reset the user's saved styling or layout.
        [MenuItem("Tools/Human Eye/Authoring/Repair UI renderers")]
        public static void RepairRenderers()
        {
            int repaired = 0;
            void Repair(GameObject target)
            {
                foreach (var graphic in target.GetComponentsInChildren<Graphic>(true))
                    if (!graphic.GetComponent<CanvasRenderer>()) { graphic.gameObject.AddComponent<CanvasRenderer>(); repaired++; }
            }
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try { Repair(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            foreach (var go in scene.GetRootGameObjects()) Repair(go);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("UI_RENDERERS_REPAIRED " + repaired);
        }
        static GameObject root;
        static void SeparateComparisonControls()
        {
            var view = Find("view-controls");
            view.anchorMin = view.anchorMax = new Vector2(1,.36f); view.anchoredPosition = new Vector2(-106,0); view.sizeDelta = new Vector2(180,312);
            var group = view.GetComponent<VerticalLayoutGroup>(); group.spacing = 4; group.padding = new RectOffset(8,8,8,8);
            foreach (var b in view.GetComponentsInChildren<Button>(true)) Layout(b).minHeight = Layout(b).preferredHeight = 48;
            var insets = Find("insets"); insets.offsetMin = new Vector2(insets.offsetMin.x,-312); insets.offsetMax = new Vector2(insets.offsetMax.x,-132);
            foreach (var image in insets.GetComponentsInChildren<RawImage>(true)) { Layout(image).minHeight = 80; Layout(image).preferredHeight = 120; }
            foreach (var caption in insets.GetComponentsInChildren<Text>(true)) { caption.fontSize = 16; Layout(caption).minHeight = 24; }
            var prompt = Find("prompt-banner"); prompt.anchorMax = new Vector2(.68f,prompt.anchorMax.y);
        }
        public static void RepairPointerOverlap()
        {
            root = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try { SeparateComparisonControls(); PrefabUtility.SaveAsPrefabAsset(root, CanvasAuthoring.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static Material material;
        static readonly Color Navy = new(.025f, .065f, .11f, .92f);
        static RectTransform Find(string name) => root.GetComponentsInChildren<RectTransform>(true).First(x => x.name == name);
        static LayoutElement Layout(Component c) => c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
        static GlassPanel Surface(GameObject go)
        {
            var glass = go.GetComponent<GlassPanel>();
            if (glass) return glass;
            var old = go.GetComponent<Image>();
            bool raycast = old && old.raycastTarget;
            if (old) Object.DestroyImmediate(old);
            glass = go.AddComponent<GlassPanel>(); glass.material = material; glass.color = Navy; glass.raycastTarget = raycast;
            var control = go.GetComponent<Selectable>(); if (control) control.targetGraphic = glass;
            var state = go.GetComponent<HudState>();
            if (state) { state.surface = glass; if (!(state.target is Text)) state.target = glass; }
            return glass;
        }
        static void WrapText(RectTransform r)
        {
            var text = r.GetComponent<Text>(); if (!text) return;
            var child = new GameObject("Caption", typeof(RectTransform), typeof(Text)); child.transform.SetParent(r, false);
            EditorUtility.CopySerialized(text, child.GetComponent<Text>());
            var rect = (RectTransform)child.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(8, 4); rect.offsetMax = new Vector2(-8, -4);
            var state = r.GetComponent<HudState>(); if (state) state.target = child.GetComponent<Text>();
            Object.DestroyImmediate(text); Surface(r.gameObject);
        }
        static void Icon(RectTransform r, HudIcon.Symbol symbol, bool large = false)
        {
            if (r.Find("Icon")) return;
            var go = new GameObject("Icon", typeof(RectTransform), typeof(HudIcon), typeof(LayoutElement)); go.transform.SetParent(r, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = new Vector2(large ? 30 : 12, 0); rect.sizeDelta = Vector2.one * (large ? 32 : 24);
            var icon = go.GetComponent<HudIcon>(); icon.symbol = symbol; icon.raycastTarget = false; icon.color = new Color(.65f, .94f, 1);
            var text = r.GetComponentInChildren<Text>(true);
            if (text) { text.rectTransform.offsetMin = new Vector2(large ? 78 : 43, 4); text.rectTransform.offsetMax = new Vector2(-10, -4); text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = text.fontSize; }
        }
        [MenuItem("Tools/Human Eye/Authoring/Apply reference glass styling")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            const string path = "Assets/HumanEye/UI/ObservatoryGlass.mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("HumanEye/UI Glass")); AssetDatabase.CreateAsset(material, path); }
            root = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                root.GetComponent<Canvas>().additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    var n = image.name;
                    if (n is "fade" or "activity" or "help-panel" or "settings-panel" or "review-panel" or "pause-overlay" or "confirm-panel" or "end-menu" or "Leader" or "Check" or "Fill" or "Track" or "Handle" or "Scrollbar" or "Box") continue;
                    var glass = Surface(image.gameObject);
                    glass.radius = n == "Card" ? 32 : 18;
                    var b = glass.GetComponent<Button>();
                    if (b) { b.transition = Selectable.Transition.None; Layout(b).minHeight = Layout(b).preferredHeight = 56; }
                }
                foreach (string n in new[] { "prompt-banner", "lens-state", "toast" }) WrapText(Find(n));
                foreach (var g in root.GetComponentsInChildren<GlassPanel>(true)) { g.border = .8f; g.haloWidth = 18; }
                foreach (var b in root.GetComponentsInChildren<Button>(true)) { var caption = b.GetComponentInChildren<Text>(true); if (caption) caption.fontStyle = FontStyle.Normal; }
                var progress = Find("progress"); progress.GetComponent<HorizontalLayoutGroup>().spacing = 4;
                progress.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                foreach (RectTransform dot in progress)
                {
                    WrapText(dot); var s = dot.GetComponent<GlassPanel>(); s.radius = 99;
                    var l = Layout(dot); l.minWidth = l.preferredWidth = 30; l.minHeight = l.preferredHeight = 30;
                    var t = dot.GetComponentInChildren<Text>(true); t.fontSize = 14; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                }
                string[] buttons = { "btn-ask", "btn-replay-expl", "btn-pause", "btn-help", "btn-review", "btn-settings", "ab-SCAN", "ab-INSIDE", "ab-SIMULATE", "ab-TRACE", "ab-FREEZE", "ab-REPLAY", "view-left", "view-right", "view-in", "view-out", "view-reset", "start-begin", "start-continue", "start-settings" };
                HudIcon.Symbol[] symbols = { HudIcon.Symbol.Chat, HudIcon.Symbol.Play, HudIcon.Symbol.Pause, HudIcon.Symbol.Help, HudIcon.Symbol.Review, HudIcon.Symbol.Settings, HudIcon.Symbol.Scan, HudIcon.Symbol.Eye, HudIcon.Symbol.Play, HudIcon.Symbol.Trace, HudIcon.Symbol.Freeze, HudIcon.Symbol.Replay, HudIcon.Symbol.TurnLeft, HudIcon.Symbol.TurnRight, HudIcon.Symbol.ZoomIn, HudIcon.Symbol.ZoomOut, HudIcon.Symbol.Replay, HudIcon.Symbol.Play, HudIcon.Symbol.Book, HudIcon.Symbol.Settings };
                for (int i = 0; i < buttons.Length; i++) Icon(Find(buttons[i]), symbols[i], buttons[i].StartsWith("start-"));
                foreach (string n in buttons.Where(n => n.StartsWith("btn-") || n.StartsWith("ab-")))
                {
                    var r = Find(n); var compact = r.GetComponent<CompactButtonLayout>() ?? r.gameObject.AddComponent<CompactButtonLayout>();
                    compact.icon = (RectTransform)r.Find("Icon"); compact.caption = r.GetComponentInChildren<Text>(true).rectTransform;
                }
                foreach (string n in new[] { "start-begin", "btn-ask" }) { var r = Find(n); r.GetComponent<HudState>().persistentGlow = true; var g = r.GetComponent<GlassPanel>(); g.glow = 1.4f; g.color = new Color(.005f, .48f, .62f, .98f); }
                var nav = Find("Chapter Navigation"); Layout(nav).minWidth = 354; Layout(nav).preferredWidth = 490;
                Layout(nav).minHeight = Layout(nav).preferredHeight = 112;
                var title = Find("chapter-title");
                if (title.GetComponent<Text>()) { WrapText(title); Object.DestroyImmediate(title.GetComponent<GlassPanel>()); }
                var titleText = title.GetComponentInChildren<Text>(true); titleText.fontStyle = FontStyle.Bold; titleText.rectTransform.offsetMin = new Vector2(40, 0); titleText.rectTransform.offsetMax = Vector2.zero;
                Layout(title).minHeight = Layout(title).preferredHeight = 48;
                titleText.resizeTextForBestFit = true; titleText.resizeTextMinSize = 18; titleText.resizeTextMaxSize = 26;
                if (!title.Find("btn-home"))
                {
                    var home = new GameObject("btn-home", typeof(RectTransform), typeof(HudIcon), typeof(Button)); home.transform.SetParent(title, false);
                    var rect = (RectTransform)home.transform; rect.anchorMin = rect.anchorMax = new Vector2(0,.5f); rect.pivot = new Vector2(0,.5f); rect.sizeDelta = new Vector2(30,30);
                    home.GetComponent<HudIcon>().symbol = HudIcon.Symbol.Home; home.GetComponent<Button>().targetGraphic = home.GetComponent<HudIcon>();
                }
                ((RectTransform)title.Find("btn-home")).sizeDelta = new Vector2(48,48);
                titleText.rectTransform.offsetMin = new Vector2(48,0);
                var top = Find("topbar"); Layout(top.GetChild(1)).flexibleWidth = 1;
                top.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                var view = Find("view-controls");
                var parts = Find("part-list"); var fit = parts.GetComponent<ContentSizeFitter>() ?? parts.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                foreach (var b in view.GetComponentsInChildren<Button>(true)) { Layout(b).minHeight = Layout(b).preferredHeight = 52; var t = b.GetComponentInChildren<Text>(true); t.fontSize = t.resizeTextMaxSize = 16; }
                SeparateComparisonControls();
                var topButtons = Find("top-buttons"); Layout(topButtons).flexibleWidth = 0; Layout(topButtons).minWidth = 610; Layout(topButtons).preferredWidth = 840;
                foreach (var t in topButtons.GetComponentsInChildren<Text>(true)) { t.fontSize = 17; t.resizeTextMaxSize = 17; }
                var bottom = Find("bottom"); bottom.anchorMin = new Vector2(.14f, 0); bottom.anchorMax = new Vector2(.86f, 0);
                var subtitle = Find("subtitle-panel"); subtitle.GetComponent<GlassPanel>().glow = .75f;
                var bottomGroup = bottom.GetComponent<VerticalLayoutGroup>(); bottomGroup.childForceExpandWidth = false;
                Layout(subtitle).minWidth = 0; Layout(subtitle).preferredWidth = 720;
                Layout(Find("action-row")).minWidth = 0; Layout(Find("action-row")).preferredWidth = 1200;
                Layout(Find("sim-panel")).minWidth = 0; Layout(Find("sim-panel")).preferredWidth = 720;
                foreach (var b in Find("abilities").GetComponentsInChildren<Button>(true)) Layout(b).minHeight = Layout(b).preferredHeight = 64;
                var startCard = (RectTransform)Find("start-screen").GetChild(0);
                startCard.anchorMin = new Vector2(.24f, .46f); startCard.anchorMax = new Vector2(.76f, .46f);
                var group = startCard.GetComponent<VerticalLayoutGroup>(); group.padding = new RectOffset(48,48,34,34); group.spacing = 16;
                startCard.GetComponent<GlassPanel>().glow = .65f;
                foreach (var t in startCard.GetComponentsInChildren<Text>(true))
                {
                    if (t.text == "Observatory of Vision") { t.fontStyle = FontStyle.Bold; t.fontSize = 54; t.resizeTextForBestFit = true; t.resizeTextMinSize = 28; t.resizeTextMaxSize = 54; Layout(t).minHeight = Layout(t).preferredHeight = 68; }
                    if (t.text.StartsWith("A Human Eye")) { t.fontSize = 21; t.alignment = TextAnchor.MiddleCenter; Layout(t).minHeight = Layout(t).preferredHeight = 56; }
                    if (t.text.StartsWith("Sound on")) { t.text = "EXPLORE   ·   UNDERSTAND   ·   SEE FURTHER"; t.fontSize = 15; t.color = new Color(.5f,.77f,.9f); t.alignment = TextAnchor.MiddleCenter; }
                    else if (t.transform.parent == startCard && t.transform.GetSiblingIndex() == 0) { t.text = ""; Layout(t).minHeight = Layout(t).preferredHeight = 26;
                        if (!t.transform.Find("Eye ornament")) { var mark = new GameObject("Eye ornament", typeof(RectTransform), typeof(HudIcon)); mark.transform.SetParent(t.transform,false); var rect = (RectTransform)mark.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.sizeDelta = new Vector2(40,26); var icon = mark.GetComponent<HudIcon>(); icon.symbol = HudIcon.Symbol.Eye; icon.raycastTarget = false; icon.color = new Color(.5f,.8f,.95f); }
                    }
                }
                Layout(Find("start-begin")).minHeight = Layout(Find("start-begin")).preferredHeight = 80;
                Find("start-begin").GetComponentInChildren<Text>(true).fontSize = 32; Find("start-begin").GetComponentInChildren<Text>(true).resizeTextMaxSize = 32;
                Find("start-begin").GetComponentInChildren<Text>(true).fontStyle = FontStyle.Bold;
                foreach (string n in new[] { "start-continue", "start-settings" }) Layout(Find(n)).minHeight = Layout(Find(n)).preferredHeight = 66;
                foreach (string n in new[] { "start-begin", "start-continue", "start-settings" })
                {
                    var r = Find(n); var t = r.GetComponentInChildren<Text>(true); t.rectTransform.offsetMax = new Vector2(-78,-4); t.alignment = n == "start-begin" ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                    if (!r.Find("Chevron")) { var arrow = new GameObject("Chevron", typeof(RectTransform), typeof(HudIcon)); arrow.transform.SetParent(r,false); var rect = (RectTransform)arrow.transform; rect.anchorMin = rect.anchorMax = new Vector2(1,.5f); rect.anchoredPosition = new Vector2(-30,0); rect.sizeDelta = new Vector2(24,24); var icon = arrow.GetComponent<HudIcon>(); icon.symbol = HudIcon.Symbol.Chevron; icon.raycastTarget = false; icon.color = new Color(.6f,.88f,1); }
                }
                var settingsText = Find("start-settings").GetComponentInChildren<Text>(true); settingsText.fontSize = settingsText.resizeTextMaxSize = 22;
                PrefabUtility.SaveAsPrefabAsset(root, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            if (!hud.bindings.Any(b => b.key == "btn-home")) hud.bindings = hud.bindings.Append(new Hud.Binding { key = "btn-home", target = hud.canvas.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "btn-home") }).ToArray();
            EditorUtility.SetDirty(hud);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("REFERENCE_GLASS_SAVED");
        }
    }
}
