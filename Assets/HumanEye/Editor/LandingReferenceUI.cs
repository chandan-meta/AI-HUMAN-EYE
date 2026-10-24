using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HumanEye.EditorTools
{
    /// The reference keeps Figma surfaces and icons, with editable Unity text over them.
    public static class LandingReferenceUI
    {
        static readonly Color Navy = new Color32(16, 28, 62, 255);
        static readonly Color Muted = new Color32(98, 96, 142, 255);

        public static void ApplyTo(Hud hud)
        {
            var card = hud.Element("start-screen")?.Find("Card") as RectTransform;
            var brand = card ? card.Find("Figma Brand") as RectTransform : null;
            var row = card ? card.Find("Figma Secondary Actions") as RectTransform : null;
            if (!card || !brand || !row) throw new InvalidOperationException("Saved Figma start card is missing.");
            var bindings = hud.bindings.Select(b => (b.key, b.target)).ToArray();
            int buttonCount = hud.canvas.GetComponentsInChildren<Button>(true).Length;
            var labels = card.GetComponentsInChildren<Text>(true)
                .Where(t => !t.GetComponentInParent<Button>(true) && t.transform != hud.Element("start-notice")).ToArray();
            var eyebrow = labels.FirstOrDefault(t => t.text.Contains("INTERACTIVE"));
            var title = labels.FirstOrDefault(t => t.text.Contains("THE HUMAN"));
            var tagline = labels.FirstOrDefault(t => t.text.StartsWith("How you see") || t.text.StartsWith("Discover how light"));
            if (!eyebrow || !title || !tagline) throw new InvalidOperationException("The original live landing labels are missing.");
            var regular = AssetDatabase.LoadAssetAtPath<Font>("Assets/HumanEye/ThirdParty/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Regular.ttf");
            var bold = AssetDatabase.LoadAssetAtPath<Font>("Assets/HumanEye/ThirdParty/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Bold.ttf");
            if (!regular || !bold) throw new InvalidOperationException("The project's Atkinson Hyperlegible fonts are missing.");

            foreach (var label in labels) label.gameObject.SetActive(false);
            var baked = brand.GetComponent<Image>();
            if (baked) { baked.enabled = false; baked.raycastTarget = false; }
            brand.gameObject.SetActive(true);
            var brandLayout = brand.GetComponent<LayoutElement>();
            brandLayout.ignoreLayout = false;
            brandLayout.minHeight = brandLayout.preferredHeight = 262;
            var stack = brand.GetComponent<VerticalLayoutGroup>();
            if (!stack) stack = brand.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(); stack.spacing = 18; stack.childAlignment = TextAnchor.UpperLeft;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true; stack.childForceExpandHeight = false;

            var heading = brand.Find("Live heading") as RectTransform;
            if (!heading)
            {
                heading = new GameObject("Live heading", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
                heading.SetParent(brand, false);
            }
            heading.gameObject.layer = brand.gameObject.layer;
            heading.gameObject.SetActive(true);
            Height(heading, 96);
            var horizontal = heading.GetComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18; horizontal.padding = new RectOffset(); horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false; horizontal.childForceExpandHeight = false;
            var crest = heading.Find("Figma Iris crest") as RectTransform;
            if (!crest)
            {
                crest = new GameObject("Figma Iris crest", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
                crest.SetParent(heading, false);
            }
            crest.gameObject.layer = heading.gameObject.layer;
            var crestImage = crest.GetComponent<Image>();
            crestImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FigmaSkin.Folder + "IrisCrest.png");
            if (!crestImage.sprite) throw new InvalidOperationException("The exported Figma iris crest is missing.");
            crestImage.preserveAspect = true; crestImage.color = Color.white; crestImage.raycastTarget = false;
            var crestLayout = crest.GetComponent<LayoutElement>();
            crestLayout.minWidth = crestLayout.preferredWidth = crestLayout.minHeight = crestLayout.preferredHeight = 96;
            crestLayout.flexibleWidth = 0; crest.SetAsFirstSibling();
            Label(eyebrow, heading, regular, "AN INTERACTIVE\nLESSON WITH NIKO", 25, Muted, 68);
            var eyebrowLayout = eyebrow.GetComponent<LayoutElement>();
            eyebrowLayout.minWidth = 0; eyebrowLayout.preferredWidth = 500; eyebrowLayout.flexibleWidth = 1;
            heading.SetAsFirstSibling();
            Label(title, brand, bold, "THE HUMAN <color=#6847F5>EYE</color>", 72, Navy, 88);
            title.supportRichText = true; title.transform.SetSiblingIndex(1);
            // Font line metrics exceed the visible capitals; truncation can discard the whole single-line title.
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            Label(tagline, brand, regular, "Discover how light becomes sight", 32, Muted, 42);
            tagline.transform.SetSiblingIndex(2);

            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 720);
            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(36, 36, 36, 32); cardLayout.spacing = 20;
            cardLayout.childControlWidth = cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true; cardLayout.childForceExpandHeight = false;
            var begin = hud.Element("start-begin");
            Height(begin, 80);
            var beginText = begin.GetComponentInChildren<Text>(true);
            beginText.text = "Begin Journey"; beginText.font = bold; beginText.fontStyle = FontStyle.Normal;
            beginText.fontSize = 28; beginText.color = Navy; beginText.resizeTextForBestFit = false;
            foreach (var key in new[] { "start-help", "start-chapters", "start-settings" })
            {
                var button = hud.Element(key); Height(button, 64);
                var text = button.GetComponentInChildren<Text>(true);
                text.font = bold; text.fontStyle = FontStyle.Normal; text.fontSize = 17; text.color = Navy;
            }
            Height(row, 64);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            if (title.preferredWidth > 650 || tagline.preferredWidth > 650)
                throw new InvalidOperationException("Reference landing text exceeds its 648 px column.");
            if (bindings.Any(b => hud.Element(b.key) != b.target) || hud.canvas.GetComponentsInChildren<Button>(true).Length != buttonCount)
                throw new InvalidOperationException("Reference landing UI changed an action or HUD binding.");
            Debug.Log($"LANDING_REFERENCE_UI live labels; Figma surfaces/icons retained; {buttonCount} Buttons, {bindings.Length} bindings.");
        }

        static void Height(RectTransform rect, float height)
        {
            var layout = rect.GetComponent<LayoutElement>();
            if (!layout) layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = false; layout.minHeight = layout.preferredHeight = height;
        }

        static void Label(Text text, RectTransform parent, Font font, string value, int size, Color colour, float height)
        {
            text.transform.SetParent(parent, false); text.gameObject.SetActive(true);
            text.gameObject.layer = parent.gameObject.layer;
            text.text = value; text.font = font; text.fontStyle = FontStyle.Normal;
            text.fontSize = size; text.color = colour; text.alignment = TextAnchor.MiddleLeft;
            text.resizeTextForBestFit = false; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            var ornament = text.transform.Find("Eye ornament"); if (ornament) ornament.gameObject.SetActive(false);
            Height(text.rectTransform, height);
        }
    }
}
