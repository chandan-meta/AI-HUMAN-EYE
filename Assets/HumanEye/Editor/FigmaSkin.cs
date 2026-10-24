using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Imports the actual Figma PNGs onto the saved Canvas. No runtime UI generation or drawing.
    public static class FigmaSkin
    {
        public const string Folder = "Assets/HumanEye/UI/Figma/";
        const string Evidence = "Docs/HumanEye/FigmaIntegration/";
        [Serializable] class Source { public Asset[] assets; }
        [Serializable] class Asset { public string name; public float[] border; public float scale; }
        static readonly Color Ink = new(26 / 255f, 43 / 255f, 73 / 255f), Violet = new(102 / 255f, 87 / 255f, 217 / 255f);
        static readonly HashSet<string> Primary = new() { "btn-free-continue", "btn-read-continue", "ab-INSIDE", "ask-submit", "pause-resume", "end-explore", "confirm-accept", "settings-close", "help-close", "chapters-close", "review-close" };
        static readonly Dictionary<HudIcon.Symbol, string> Icons = new()
        {
            [HudIcon.Symbol.Home] = "Home", [HudIcon.Symbol.Pause] = "Pause", [HudIcon.Symbol.Settings] = "Settings",
            [HudIcon.Symbol.Chevron] = "Next", [HudIcon.Symbol.ZoomIn] = "ZoomIn", [HudIcon.Symbol.ZoomOut] = "ZoomOut",
            [HudIcon.Symbol.List] = "Chapters", [HudIcon.Symbol.Replay] = "Reset", [HudIcon.Symbol.TurnLeft] = "Reset"
        };
        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + ".png") ?? throw new InvalidOperationException("Figma sprite missing: " + name);

        [MenuItem("Tools/Human Eye/Figma/Apply exported PNG artwork")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);
            if (!File.Exists(Evidence + "BeforeFigma.unity")) File.Copy(Builder.ScenePath, Evidence + "BeforeFigma.unity");
            Import();
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            if (!hud || !hud.canvas) throw new InvalidOperationException("Saved HUD Canvas missing; do not regenerate it.");
            var canvas = hud.canvas.transform;
            var bindings = hud.bindings.Select(b => (b.key, b.target)).ToArray();
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            int surfaces = 0, icons = 0;
            foreach (var panel in canvas.GetComponentsInChildren<GlassPanel>(true))
            {
                if (panel.GetComponent<Button>()) continue;
                var rect = panel.rectTransform;
                if (!panel.enabled) { Object.DestroyImmediate(panel); continue; }
                string art = rect.name == "subtitle-panel" ? "PanelCaption" :
                    rect.name.Contains("inset-") ? "PanelInset" : rect.name == "speaker" ? "SpeakerTag" :
                    rect.name.StartsWith("progress ", StringComparison.Ordinal) ? "ProgressIdle" : "PanelPearl";
                var image = Surface(rect, art, false);
                var state = rect.GetComponent<HudState>();
                if (state)
                {
                    state.target = image; state.surface = null;
                    state.normalSprite = image.sprite;
                    state.selectedSprite = art == "ProgressIdle" ? Sprite("ProgressActive") : image.sprite;
                    state.disabledSprite = image.sprite;
                }
                surfaces++;
            }
            foreach (var b in buttons)
            {
                string family = b.name == "start-begin" ? "Start" : Primary.Contains(b.name) || b.name.StartsWith("act-buttons ", StringComparison.Ordinal) ? "Primary" : "Secondary";
                var image = Surface((RectTransform)b.transform, "Button" + family + "_Default", true);
                b.targetGraphic = image; b.transition = Selectable.Transition.None;
                var state = b.GetComponent<HudState>() ?? b.gameObject.AddComponent<HudState>();
                state.target = image; state.surface = null;
                state.normalSprite = image.sprite;
                state.hoverSprite = Sprite("Button" + family + "_Hover"); state.pressedSprite = Sprite("Button" + family + "_Pressed");
                state.focusSprite = Sprite("Button" + family + "_Focus"); state.selectedSprite = Sprite("Button" + family + "_Selected");
                state.disabledSprite = Sprite("Button" + family + "_Disabled");
                if (b.transform.parent.name == "part-buttons" || b.transform.parent.name == "act-options")
                {
                    image.sprite = state.normalSprite = Sprite("PartNormal"); state.selectedSprite = Sprite("PartSelected");
                }
                foreach (var text in b.GetComponentsInChildren<Text>(true)) text.color = family == "Primary" ? Color.white : Ink;
                if (family == "Primary")
                {
                    var compact = b.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
                    foreach (var child in b.GetComponentsInChildren<Graphic>(true).Where(g => g is not Text && g.transform != b.transform)) child.gameObject.SetActive(false);
                    var text = b.GetComponentInChildren<Text>(true);
                    if (text) { text.rectTransform.offsetMin = new Vector2(14, 2); text.rectTransform.offsetMax = new Vector2(-14, -2); text.alignment = TextAnchor.MiddleCenter; }
                }
                surfaces++;
            }
            foreach (var icon in canvas.GetComponentsInChildren<HudIcon>(true))
            {
                if (!Icons.TryGetValue(icon.symbol, out var name)) { icon.color = Violet; continue; }
                var rect = icon.rectTransform; bool enabled = icon.enabled;
                Object.DestroyImmediate(icon);
                var image = rect.gameObject.AddComponent<Image>(); image.sprite = Sprite("Icon" + name);
                image.preserveAspect = true; image.raycastTarget = false; image.color = Color.white; image.enabled = enabled; icons++;
            }
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.GetComponentInParent<Button>(true)) continue;
                bool caption = text.transform.IsChildOf(hud.Element("subtitle-panel"));
                bool cinematic = hud.Element("intro-overlay") && text.transform.IsChildOf(hud.Element("intro-overlay"));
                var surface = text.GetComponentsInParent<Image>(true).FirstOrDefault(image => image.sprite &&
                    (image.sprite.name.StartsWith("Panel", StringComparison.Ordinal) || image.sprite.name == "SpeakerTag"));
                bool dark = surface ? surface.sprite.name is "PanelCaption" or "PanelInset" : caption || cinematic;
                text.color = dark ? Color.white : Ink;
            }
            foreach (var slider in canvas.GetComponentsInChildren<Slider>(true))
            {
                var track = slider.transform.Find("Track") as RectTransform;
                if (track) Surface(track, "SliderTrack", true);
                if (slider.fillRect) Surface(slider.fillRect, "SliderFill", false);
                if (slider.handleRect) slider.targetGraphic = Surface(slider.handleRect, "SliderHandle", true);
                slider.transition = Selectable.Transition.None;
            }
            foreach (var toggle in canvas.GetComponentsInChildren<Toggle>(true))
            {
                var background = toggle.GetComponent<Image>();
                if (background) { toggle.targetGraphic = background; background.raycastTarget = true; }
                var check = toggle.transform.Find("Box/Check");
                if (check && check.TryGetComponent<Image>(out var mark)) { toggle.graphic = mark; mark.raycastTarget = false; }
                if (toggle.targetGraphic) toggle.targetGraphic.color = Color.white;
                if (toggle.graphic) toggle.graphic.color = Violet;
            }
            foreach (var field in canvas.GetComponentsInChildren<InputField>(true))
            {
                var background = field.GetComponent<Image>();
                if (background) { field.targetGraphic = background; background.raycastTarget = true; }
                field.customCaretColor = true; field.caretColor = Ink; field.selectionColor = new Color(.86f, .84f, .95f);
            }
            foreach (var scroll in canvas.GetComponentsInChildren<ScrollRect>(true))
                if (scroll.TryGetComponent<Image>(out var background)) background.raycastTarget = true;
            foreach (var key in new[] { "progress-line", "progress-fill" })
            {
                var r = hud.Element(key); if (r && r.TryGetComponent<Graphic>(out var g)) g.color = key == "progress-fill" ? Violet : new Color(.86f, .84f, .95f);
            }
            foreach (var key in new[] { "info-panel", "activity-panel", "ask-panel" })
            {
                var panel = hud.Element(key); if (panel) Decorate(panel, "Header Art", "HeaderArt", 150);
            }
            foreach (var key in new[] { "settings-panel", "help-panel", "chapters-panel", "review-panel", "pause-overlay", "confirm-panel", "end-menu" })
            {
                var panel = hud.Element(key); var card = panel ? panel.Find("Card") as RectTransform : null;
                if (card) Decorate(card, "Header Art", "HeaderArt", 150);
            }
            StartCard(hud);
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
                if (graphic.name.StartsWith("Figma ", StringComparison.Ordinal)) graphic.raycastTarget = false;
            if (bindings.Any(b => hud.Element(b.key) != b.target)) throw new InvalidOperationException("Figma import changed a saved HUD binding.");
            if (buttons.Any(b => !b.targetGraphic || !b.targetGraphic.raycastTarget)) throw new InvalidOperationException("A button lost its click surface.");
            EditorUtility.SetDirty(hud); EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence + "apply.txt", $"Direct Figma PNG integration\nSprites: {JsonUtility.FromJson<Source>(File.ReadAllText(Folder + "FigmaSource.json")).assets.Length}\nSurfaces: {surfaces}\nExported icons: {icons}\nButtons retained: {buttons.Length}\nBindings retained: {bindings.Length}\nScene: {Builder.ScenePath}\n");
            Debug.Log($"HUMAN_EYE_FIGMA_APPLIED sprites=43 surfaces={surfaces} icons={icons} buttons={buttons.Length} bindings={bindings.Length}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Import()
        {
            AssetDatabase.Refresh();
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(Folder + "FigmaSource.json"));
            foreach (var asset in source.assets)
            {
                string path = Folder + asset.name + ".png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) throw new FileNotFoundException("Exported Figma PNG missing", path);
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100 * Mathf.Max(1, asset.scale);
                ti.spriteBorder = new Vector4(asset.border[0], asset.border[1], asset.border[2], asset.border[3]);
                ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.npotScale = TextureImporterNPOTScale.None;
                ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); ti.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
        }

        static Image Surface(RectTransform rect, string name, bool raycast)
        {
            var selectable = rect.GetComponent<Selectable>();
            var glass = rect.GetComponent<GlassPanel>(); if (glass) Object.DestroyImmediate(glass);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(name); image.type = image.sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            image.material = null; image.color = Color.white; image.raycastTarget = raycast || selectable || rect.GetComponent<ScrollRect>();
            if (selectable) selectable.targetGraphic = image;
            return image;
        }

        static RectTransform Artwork(RectTransform parent, string name, string sprite)
        {
            var rect = parent.Find("Figma " + name) as RectTransform;
            if (!rect) { rect = new GameObject("Figma " + name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>(); rect.SetParent(parent, false); }
            rect.gameObject.layer = parent.gameObject.layer;
            var image = rect.GetComponent<Image>(); image.sprite = Sprite(sprite); image.preserveAspect = true; image.raycastTarget = false;
            rect.GetComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        static void Decorate(RectTransform parent, string name, string sprite, float size)
        {
            var rect = Artwork(parent, name, sprite); rect.SetAsFirstSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = new Vector2(-4, -4); rect.sizeDelta = Vector2.one * size;
            rect.GetComponent<Image>().color = new Color(1, 1, 1, .6f);
        }

        static void StartCard(Hud hud)
        {
            var card = hud.Element("start-screen").Find("Card") as RectTransform;
            if (!card) throw new InvalidOperationException("Saved start card missing.");
            foreach (var text in card.GetComponentsInChildren<Text>(true))
                if (!text.GetComponentInParent<Button>(true) && text.transform != hud.Element("start-notice")) text.gameObject.SetActive(false);
            var brand = Artwork(card, "Brand", "StartBrand"); brand.SetAsFirstSibling();
            var brandLayout = brand.GetComponent<LayoutElement>(); brandLayout.ignoreLayout = false; brandLayout.minHeight = brandLayout.preferredHeight = (card.rect.width - 64) * 265 / 668;
            var group = card.GetComponent<VerticalLayoutGroup>(); if (group) { group.spacing = 16; group.padding = new RectOffset(32, 32, 28, 28); group.childForceExpandWidth = true; }
            var begin = hud.Element("start-begin"); begin.SetParent(card, false); begin.SetSiblingIndex(brand.GetSiblingIndex() + 1);
            var beginLayout = begin.GetComponent<LayoutElement>(); beginLayout.ignoreLayout = false; beginLayout.minWidth = 48; beginLayout.preferredWidth = -1; beginLayout.flexibleWidth = 1; beginLayout.minHeight = beginLayout.preferredHeight = 72;
            var resume = hud.Element("start-continue"); resume.SetParent(card, false); resume.SetSiblingIndex(begin.GetSiblingIndex() + 1);
            var resumeLayout = resume.GetComponent<LayoutElement>(); resumeLayout.ignoreLayout = false; resumeLayout.minWidth = 48; resumeLayout.preferredWidth = -1; resumeLayout.flexibleWidth = 1;
            foreach (var name in new[] { "start-actions", "start-more" }) { var oldRow = card.Find(name); if (oldRow) oldRow.gameObject.SetActive(false); }
            var row = card.Find("Figma Secondary Actions") as RectTransform;
            if (!row) { row = new GameObject("Figma Secondary Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>(); row.SetParent(card, false); }
            var layout = row.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var rowLayout = row.GetComponent<LayoutElement>(); rowLayout.minHeight = rowLayout.preferredHeight = 60;
            foreach (var key in new[] { "start-help", "start-chapters", "start-settings" })
            {
                var button = hud.Element(key); if (!button) continue; button.SetParent(row, false);
                var le = button.GetComponent<LayoutElement>(); le.ignoreLayout = false; le.minWidth = 48; le.preferredWidth = 140; le.flexibleWidth = 1; le.minHeight = le.preferredHeight = 60;
                foreach (var icon in button.GetComponentsInChildren<Graphic>(true).Where(g => g is not Text && g.transform != button)) icon.gameObject.SetActive(false);
                var text = button.GetComponentInChildren<Text>(true); text.fontSize = 17; text.resizeTextForBestFit = false; text.alignment = TextAnchor.MiddleCenter;
                text.rectTransform.offsetMin = new Vector2(6, 2); text.rectTransform.offsetMax = new Vector2(-6, -2);
                var compact = button.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
            }
            row.SetAsLastSibling();
            var beginText = hud.Element("start-begin").GetComponentInChildren<Text>(true); beginText.text = "Begin Journey"; beginText.fontSize = 26; beginText.resizeTextMaxSize = 26; beginText.color = Ink;
            foreach (var icon in begin.GetComponentsInChildren<Graphic>(true).Where(g => g is not Text && g.transform != begin)) icon.gameObject.SetActive(false);
            beginText.rectTransform.offsetMin = new Vector2(14, 2); beginText.rectTransform.offsetMax = new Vector2(-14, -2); beginText.alignment = TextAnchor.MiddleCenter;
            var beginCompact = begin.GetComponent<CompactButtonLayout>(); if (beginCompact) Object.DestroyImmediate(beginCompact);
            var notice = hud.Element("start-notice"); if (notice) notice.SetAsLastSibling();
            Decorate(card, "Start Orbits", "HeaderArt", 150);
        }
    }
}
