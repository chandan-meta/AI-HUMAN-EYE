using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Repositions the existing Figma artwork and controls on the saved start card only.
    public static class IntroBannerPolish
    {
        const string Evidence = "Logs/cinematic-20261024/banner-apply.txt";

        [MenuItem("Tools/Human Eye/Authoring/Polish start banner (existing Figma PNGs)")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            if (!hud || !hud.canvas) throw new InvalidOperationException("Saved HUD is missing.");
            var card = hud.Element("start-screen")?.Find("Card") as RectTransform;
            var brand = card ? card.Find("Figma Brand") as RectTransform : null;
            var row = card ? card.Find("Figma Secondary Actions") as RectTransform : null;
            if (!card || !brand || !row || !card.GetComponent<VerticalLayoutGroup>())
                throw new InvalidOperationException("The saved Figma start-card structure is missing.");
            var bindings = hud.bindings.Select(b => (b.key, b.target)).ToArray();
            var buttons = hud.canvas.GetComponentsInChildren<Button>(true);
            var begin = Required(hud, "start-begin", card);
            var resume = Required(hud, "start-continue", card);
            var help = Required(hud, "start-help", card);
            var chapters = Required(hud, "start-chapters", card);
            var settings = Required(hud, "start-settings", card);
            var next = Sprite("IconNext");
            var list = Sprite("IconChapters");
            var gear = Sprite("IconSettings");
            var replay = Sprite("IconReset");
            var iris = Sprite("IrisCrest");

            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 720);
            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 36, 32);
            layout.spacing = 20;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var brandLayout = brand.GetComponent<LayoutElement>();
            brandLayout.ignoreLayout = false;
            brandLayout.minHeight = brandLayout.preferredHeight = 648f * 265 / 668;
            Height(begin, 80);
            Height(resume, 64);
            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = rowLayout.preferredHeight = 64;
            var actions = row.GetComponent<HorizontalLayoutGroup>();
            actions.spacing = 12;
            actions.childControlWidth = actions.childControlHeight = true;
            actions.childForceExpandWidth = true;
            actions.childForceExpandHeight = false;

            Caption(begin, 28, 36, 64);
            Icon(begin, "Chevron", next, true);
            Caption(resume, 19, 48, 12);
            Icon(resume, "Icon", replay, false);
            Secondary(help, next);
            Secondary(chapters, list);
            Secondary(settings, gear);

            var watermark = card.Find("Figma Start Orbits") as RectTransform;
            if (watermark)
            {
                var image = watermark.GetComponent<Image>();
                image.sprite = iris;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(1, 1, 1, .055f);
                watermark.anchorMin = watermark.anchorMax = watermark.pivot = Vector2.one;
                watermark.anchoredPosition = new Vector2(-12, -12);
                watermark.sizeDelta = new Vector2(182, 182);
                watermark.SetAsFirstSibling();
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            if (bindings.Any(b => hud.Element(b.key) != b.target) ||
                hud.canvas.GetComponentsInChildren<Button>(true).Length != buttons.Length)
                throw new InvalidOperationException("Start-card polish changed a HUD binding or button count.");
            foreach (var button in new[] { begin, resume, help, chapters, settings })
                if (!button.GetComponent<Button>().targetGraphic || !button.GetComponent<Button>().targetGraphic.raycastTarget)
                    throw new InvalidOperationException("A start action lost its pointer surface.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(Evidence));
            var result = $"Existing Figma PNG start-card composition\nCard width: 720; padding: 36/36/36/32; spacing: 20\nBrand: original StartBrand PNG, 648 x {648f * 265 / 668:0.00}\nBegin: 80 px; Continue and secondary actions: 64 px\nDecorative icons: exported IconNext, IconReset, IconChapters, IconSettings; faint IrisCrest watermark\nButtons retained: {buttons.Length}\nHUD bindings retained: {bindings.Length}\nScene: {Builder.ScenePath}\nSource PNGs and sprite metadata unchanged.\n";
            File.WriteAllText(Evidence, result);
            Debug.Log("HUMAN_EYE_START_BANNER_POLISHED\n" + result);
        }

        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(FigmaSkin.Folder + name + ".png")
            ?? throw new InvalidOperationException("Exported Figma sprite missing: " + name);

        static RectTransform Required(Hud hud, string key, RectTransform card)
        {
            var rect = hud.Element(key);
            if (!rect || !rect.IsChildOf(card) || !rect.GetComponent<Button>() || !rect.GetComponentInChildren<Text>(true))
                throw new InvalidOperationException("Saved start action missing: " + key);
            return rect;
        }

        static void Height(RectTransform rect, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            element.minWidth = 48;
        }

        static void Caption(RectTransform button, int size, float left, float right)
        {
            var compact = button.GetComponent<CompactButtonLayout>();
            if (compact) Object.DestroyImmediate(compact);
            var text = button.GetComponentInChildren<Text>(true);
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(left, 4);
            text.rectTransform.offsetMax = new Vector2(-right, -4);
        }

        static void Secondary(RectTransform button, Sprite sprite)
        {
            Height(button, 64);
            Caption(button, 17, 48, 12);
            Icon(button, "Icon", sprite, false);
        }

        static void Icon(RectTransform button, string name, Sprite sprite, bool trailing)
        {
            var rect = button.Find(name) as RectTransform;
            if (!rect) throw new InvalidOperationException("Saved button icon is missing: " + button.name + "/" + name);
            var old = rect.GetComponent<HudIcon>();
            if (old) Object.DestroyImmediate(old);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            image.enabled = true;
            rect.gameObject.SetActive(true);
            rect.gameObject.layer = button.gameObject.layer;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(trailing ? 1 : 0, .5f);
            rect.anchoredPosition = new Vector2(trailing ? -24 : 16, 0);
            rect.sizeDelta = Vector2.one * (trailing ? 26 : 24);
        }
    }
}
