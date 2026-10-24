using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    // One-time editor conversion. Players only use the saved prefab/scene objects.
    public static class CanvasAuthoring
    {
        public const string PrefabPath = "Assets/HumanEye/UI/ObservatoryCanvas.prefab";
        static readonly Color Navy = new(.035f, .086f, .145f, 1), Raised = new(.075f, .157f, .231f, 1), Cyan = new(.412f, .878f, .941f), Ink = new(.933f, .965f, .98f);
        static Font font;
        static readonly Dictionary<string, RectTransform> named = new();
        static readonly List<Hud.Binding> bindings = new();
        static Sprite rounded;

        [MenuItem("Tools/Human Eye/Authoring/Convert saved HUD to Canvas (once)")]
        public static void ConvertMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConvertScene();
        }
        public static void ConvertScene()
        {
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>();
            if (!hud.canvas) Create(hud);
            var stage = hud.stage;
            if (!stage.menuPoint)
                stage.menuPoint = Builder.Obj("Menu Eye Point", null, pos: new Vector3(0, 2.3f, .6f), rot: stage.heroPoint.rotation, scale: Vector3.one * .65f).transform;
            EditorUtility.SetDirty(stage);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_CANVAS_SAVED " + hud.canvas.GetComponentsInChildren<RectTransform>(true).Length + " editable objects");
        }
        public static void Create(Hud hud)
        {
            font = hud.font;
            rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            named.Clear(); bindings.Clear();
            var canvasRect = Rect("Observatory Canvas", hud.transform);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            canvasRect.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            var xml = XDocument.Load(Builder.Root + "UI/Hud.uxml");
            Parse(xml.Root.Elements().First(e => e.Name.LocalName == "VisualElement"), canvasRect);
            Populate(hud);
            Position();
            AddScrollbars(canvasRect);
            PinSettingsFooter(canvasRect);
            var old = hud.GetComponent<UnityEngine.UIElements.UIDocument>();
            if (old) Object.DestroyImmediate(old);
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasRect.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            hud.canvas = canvas; hud.bindings = bindings.ToArray();
            EditorUtility.SetDirty(hud);
        }

        [MenuItem("Tools/Human Eye/Authoring/Pin Settings Close button")]
        public static void FixSettingsLayout()
        {
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                PinSettingsFooter(prefab.transform);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            Debug.Log("HE_SETTINGS_LAYOUT saved scrollable settings with pinned Reset / Close footer");
        }

        static void PinSettingsFooter(Transform canvas)
        {
            var panel = canvas.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "settings-panel");
            var scroll = panel.GetComponentInChildren<ScrollRect>(true);
            var card = (RectTransform)scroll.transform;
            var close = panel.GetComponentsInChildren<Button>(true).Single(b => b.name == "settings-close");
            var footer = (RectTransform)close.transform.parent;
            var viewport = card.Find("Settings Viewport") as RectTransform;
            if (!viewport) { viewport = Rect("Settings Viewport", card); viewport.gameObject.AddComponent<RectMask2D>(); }
            Stretch(viewport); viewport.offsetMin = new Vector2(0, 92);
            scroll.content.SetParent(viewport, false);
            scroll.content.anchoredPosition = new Vector2(scroll.content.anchoredPosition.x, 0);
            scroll.viewport = viewport;
            footer.SetParent(card, false);
            footer.anchorMin = Vector2.zero; footer.anchorMax = Vector2.right; footer.pivot = new Vector2(.5f, 0);
            footer.offsetMin = new Vector2(24, 16); footer.offsetMax = new Vector2(-24, 80);
            if (scroll.verticalScrollbar)
            {
                var bar = (RectTransform)scroll.verticalScrollbar.transform;
                bar.offsetMin = new Vector2(bar.offsetMin.x, 100);
                bar.SetAsLastSibling();
            }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); return r;
        }
        static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static Image Background(RectTransform r, Color colour, bool raycast = true)
        {
            var image = r.gameObject.AddComponent<Image>(); image.color = colour; image.raycastTarget = raycast;
            image.sprite = colour == Navy ? null : rounded; image.type = Image.Type.Sliced; return image;
        }
        static void Size(RectTransform r, float height, float width = -1)
        {
            var l = r.GetComponent<LayoutElement>() ?? r.gameObject.AddComponent<LayoutElement>();
            l.minHeight = height; l.preferredHeight = height; if (width >= 0) { l.minWidth = width; l.preferredWidth = width; }
        }
        static void Group(RectTransform r, bool horizontal, int padding = 0)
        {
            HorizontalOrVerticalLayoutGroup l = horizontal ? r.gameObject.AddComponent<HorizontalLayoutGroup>() : r.gameObject.AddComponent<VerticalLayoutGroup>();
            l.childControlHeight = l.childControlWidth = true; l.childForceExpandHeight = false; l.childForceExpandWidth = true;
            l.spacing = 8; l.padding = new RectOffset(padding, padding, padding, padding); l.childAlignment = TextAnchor.MiddleCenter;
        }
        static Text Text(RectTransform r, string text, int size = 20, bool centre = false)
        {
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = size; t.color = Ink;
            t.raycastTarget = false; t.supportRichText = false; t.alignment = centre ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
        static void State(RectTransform r, Graphic target)
        { var s = r.gameObject.AddComponent<HudState>(); s.target = target; if (target is Text) { s.selected = Cyan; s.complete = new Color(.6f, .95f, .72f); } }
        static Button Button(RectTransform r, string text, bool accent = false)
        {
            var image = Background(r, accent ? new Color(.02f, .36f, .46f) : Raised);
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colours = button.colors; colours.highlightedColor = new Color(.65f, .95f, 1); colours.pressedColor = new Color(.45f, .8f, .9f); colours.selectedColor = new Color(.75f, .95f, 1); button.colors = colours;
            var label = Rect("Text", r); Stretch(label); label.offsetMin = new Vector2(10, 5); label.offsetMax = new Vector2(-10, -5);
            Text(label, text, 18, true); Size(r, 52, 48); State(r, image); return button;
        }
        static void Slider(RectTransform r, string label)
        {
            Size(r, 56, 150);
            var bar = Rect("Track", r); bar.anchorMin = new Vector2(.03f, .25f); bar.anchorMax = new Vector2(.97f, .25f); bar.sizeDelta = new Vector2(0, 8);
            Background(bar, Raised);
            var fillArea = Rect("Fill Area", bar); Stretch(fillArea);
            var fill = Rect("Fill", fillArea); Stretch(fill); Background(fill, Cyan);
            var handleArea = Rect("Handle Area", r); handleArea.anchorMin = new Vector2(.04f, .25f); handleArea.anchorMax = new Vector2(.96f, .25f); handleArea.sizeDelta = Vector2.zero;
            var handle = Rect("Handle", handleArea); handle.sizeDelta = new Vector2(26, 26); var graphic = Background(handle, Ink);
            var slider = r.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = graphic; slider.minValue = 0; slider.maxValue = 1;
            if (!string.IsNullOrEmpty(label)) { var title = Rect("Label", r); title.anchorMin = new Vector2(0, .55f); title.anchorMax = Vector2.one; title.offsetMin = title.offsetMax = Vector2.zero; Text(title, label, 18); }
        }
        static void Toggle(RectTransform r, string label)
        {
            Size(r, 52);
            var bg = Background(r, Navy);
            var box = Rect("Box", r); box.anchorMin = box.anchorMax = new Vector2(0, .5f); box.pivot = new Vector2(0, .5f); box.anchoredPosition = new Vector2(4, 0); box.sizeDelta = new Vector2(28, 28); Background(box, Raised);
            var mark = Rect("Check", box); Stretch(mark); mark.offsetMin = Vector2.one * 5; mark.offsetMax = Vector2.one * -5; var check = Background(mark, Cyan, false);
            var caption = Rect("Label", r); Stretch(caption); caption.offsetMin = new Vector2(44, 0); Text(caption, label, 18);
            var toggle = r.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = check;
        }
        static RectTransform Parse(XElement e, RectTransform parent)
        {
            string key = (string)e.Attribute("name"), classes = (string)e.Attribute("class") ?? "", type = e.Name.LocalName;
            bool Has(string c) => classes.Split(' ').Contains(c);
            var r = Rect(key ?? (Has("card") ? "Card" : Has("title-block") ? "Chapter Navigation" : type), parent);
            if (key != null) { named.Add(key, r); bindings.Add(new Hud.Binding { key = key, target = r }); }
            string text = (string)e.Attribute("text") ?? "";
            if (type == "Button") Button(r, text, Has("btn-accent"));
            else if (type == "Label")
            {
                int size = Has("start-title") ? 40 : Has("card-title") ? 28 : Has("chapter-title") ? 24 : Has("note") || Has("tag") || Has("eyebrow") ? 16 : 20;
                var t = Text(r, text, size, Has("start-title") || Has("eyebrow") || Has("subtitle-text") || Has("speaker"));
                if (Has("speaker") || Has("eyebrow")) t.color = Cyan;
                Size(r, size + 10); r.GetComponent<LayoutElement>().preferredHeight = -1;
            }
            else if (type == "Slider") Slider(r, (string)e.Attribute("label"));
            else if (type == "Toggle") Toggle(r, (string)e.Attribute("label"));
            else if (type == "TextField")
            {
                Size(r, 52, 200); var bg = Background(r, Raised); var tr = Rect("Text", r); Stretch(tr); tr.offsetMin = new Vector2(12, 4); tr.offsetMax = new Vector2(-12, -4);
                var field = r.gameObject.AddComponent<InputField>(); field.textComponent = Text(tr, "", 18); field.targetGraphic = bg; field.characterLimit = 200;
            }
            else if (Has("inset-image")) { r.gameObject.AddComponent<RawImage>().raycastTarget = false; Size(r, 120, 120); }
            else if (Has("spacer")) { Size(r, 0, 0); r.GetComponent<LayoutElement>().flexibleWidth = 1; }
            else
            {
                if (Has("panel")) Background(r, Navy);
                if (Has("modal-layer") && !Has("start")) Background(r, Has("black") ? Color.black : new Color(.008f, .025f, .05f, .65f), key != "activity");
                bool absolute = key is "root" or "safe" or "labels-layer" or "middle" or "fade" || Has("modal-layer") || type == "ScrollView";
                if (!absolute) Group(r, Has("row") || Has("row-wrap") || Has("topbar") || Has("top-buttons") || Has("progress") || Has("abilities") || Has("action-row") || Has("insets") || Has("journey"), Has("panel") ? 12 : 0);
                if (Has("card") && type != "ScrollView") GroupIfMissing(r, 24);
                if (type == "ScrollView")
                {
                    r.gameObject.AddComponent<RectMask2D>(); var scroll = r.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
                    var content = Rect("Content", r); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
                    Group(content, false, 24); content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    scroll.viewport = r; scroll.content = content;
                    foreach (var child in e.Elements()) Parse(child, content);
                    return r;
                }
                if (key == "fade") { Background(r, Color.black, false); var group = r.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; }
            }
            foreach (var child in e.Elements()) Parse(child, r);
            return r;
        }
        static void GroupIfMissing(RectTransform r, int padding)
        { if (!r.GetComponent<LayoutGroup>()) Group(r, false, padding); }
        static void Populate(Hud hud)
        {
            void Pool(string key, int count, bool buttons, float height = 52)
            {
                var parent = named[key];
                // These lists grow vertically as slots become active; they are saved, never spawned.
                var layout = parent.GetComponent<LayoutGroup>(); if (layout) Object.DestroyImmediate(layout);
                Group(parent, key is "progress" or "journey" or "act-buttons", 0);
                for (int i = 0; i < count; i++)
                {
                    var r = Rect($"{key} {i + 1:00}", parent);
                    if (buttons) Button(r, "Editable slot " + (i + 1));
                    else { var text = Text(r, key == "progress" ? (i + 2).ToString() : "Step " + (i + 1), key == "progress" ? 16 : 18, true); Size(r, height); State(r, text); }
                    if (key != "progress") r.gameObject.SetActive(false);
                }
            }
            Pool("progress", hud.runner.chapters.Length, false, 32);
            Pool("part-buttons", Math.Max(10, hud.runner.chapters.Max(c => c.explore.Length)), true);
            Pool("journey", Stage.Journey.Length, false);
            Pool("act-options", 12, true); Pool("act-slots", 12, false, 32); Pool("act-buttons", 8, true);
            Pool("ask-suggestions", hud.guide.knowledge.suggestions.Length, true);
            var review = named["review-topics"];
            for (int i = 0; i < LearnerModel.Topics.Length; i++)
            {
                var row = Rect("Topic " + (i + 1), review); Group(row, true); Size(row, 64);
                Text(Rect("Topic", row), "Topic", 17); Text(Rect("Status", row), "Not started", 16);
                Button(Rect("Replay", row), "Replay scene"); Button(Rect("Practice", row), "Practice (optional)");
            }
            var labels = named["labels-layer"];
            for (int i = 0; i < 24; i++)
            {
                var r = Rect("World label " + (i + 1), labels); r.sizeDelta = new Vector2(190, 52); Button(r, "Structure");
                var leader = Rect("Leader", r); leader.pivot = new Vector2(0, .5f); leader.sizeDelta = new Vector2(80, 2); Background(leader, Cyan, false); leader.SetAsFirstSibling();
                r.gameObject.SetActive(false);
            }
        }
        static void At(string key, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { var r = named[key]; r.anchorMin = min; r.anchorMax = max; r.offsetMin = offsetMin; r.offsetMax = offsetMax; }
        static void AddScrollbars(RectTransform root)
        {
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                scroll.scrollSensitivity = 35;
                if (scroll.verticalScrollbar) continue;
                var bar = Rect("Scrollbar", scroll.transform);
                bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one;
                bar.offsetMin = new Vector2(-16, 8); bar.offsetMax = new Vector2(-4, -8);
                Background(bar, Raised);
                var handle = Rect("Handle", bar); Stretch(handle); var graphic = Background(handle, Cyan);
                var control = bar.gameObject.AddComponent<Scrollbar>(); control.targetGraphic = graphic; control.handleRect = handle;
                control.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = control;
                scroll.content.offsetMax = new Vector2(-22, scroll.content.offsetMax.y);
            }
        }
        static void Position()
        {
            foreach (string key in new[] { "root", "safe", "labels-layer", "middle", "fade" }) Stretch(named[key]);
            At("topbar", new Vector2(0, 1), Vector2.one, new Vector2(16, -128), new Vector2(-16, -16));
            named["topbar"].GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            named["topbar"].GetChild(1).GetComponent<LayoutElement>().flexibleWidth = 0;
            named["top-buttons"].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            foreach (var text in named["top-buttons"].GetComponentsInChildren<Text>(true)) text.fontSize = 16;
            var replay = named["btn-replay-expl"].GetComponent<LayoutElement>(); replay.minWidth = replay.preferredWidth = 126;
            var nav = (RectTransform)named["topbar"].GetChild(0); Size(nav, 104, 320); nav.GetComponent<LayoutElement>().flexibleWidth = 0;
            At("prompt-banner", new Vector2(.2f, 1), new Vector2(.8f, 1), new Vector2(0, -210), new Vector2(0, -142));
            At("lens-state", new Vector2(.3f, 1), new Vector2(.7f, 1), new Vector2(0, -252), new Vector2(0, -216));
            At("part-list", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, -180), new Vector2(236, 180));
            At("view-controls", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-176, -180), new Vector2(-16, 180));
            At("insets", new Vector2(.7f, 1), new Vector2(.98f, 1), new Vector2(0, -410), new Vector2(0, -214));
            At("journey", new Vector2(.04f, 1), new Vector2(.96f, 1), new Vector2(0, -220), new Vector2(0, -142));
            At("bottom", new Vector2(.06f, 0), new Vector2(.94f, 0), new Vector2(0, 16), new Vector2(0, 260));
            named["bottom"].pivot = new Vector2(.5f, 0); named["bottom"].anchoredPosition = new Vector2(0, 16); named["bottom"].gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var key in new[] { "activity", "help-panel", "settings-panel", "review-panel", "pause-overlay", "confirm-panel", "end-menu", "start-screen" })
            {
                var r = named[key]; Stretch(r); var card = (RectTransform)r.GetChild(0);
                bool scroll = card.GetComponent<ScrollRect>();
                card.anchorMin = new Vector2(.18f, .5f); card.anchorMax = new Vector2(.82f, .5f); card.pivot = new Vector2(.5f, .5f);
                card.sizeDelta = new Vector2(0, scroll ? 660 : 480);
                if (!scroll) card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                r.gameObject.SetActive(key == "start-screen");
            }
            At("ask-panel", new Vector2(.55f, .14f), new Vector2(.99f, .83f), Vector2.zero, Vector2.zero);
            // Long answers/suggestions are scrollable rather than clipped off-screen.
            var ask = named["ask-panel"]; var oldLayout = ask.GetComponent<LayoutGroup>(); Object.DestroyImmediate(oldLayout);
            var content = Rect("Content", ask); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            foreach (Transform child in ask.Cast<Transform>().Where(c => c != content).ToArray()) child.SetParent(content, false);
            Group(content, false, 16); content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ask.gameObject.AddComponent<RectMask2D>(); var sr = ask.gameObject.AddComponent<ScrollRect>(); sr.content = content; sr.viewport = ask; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped;
            At("toast", new Vector2(.27f, 1), new Vector2(.73f, 1), new Vector2(0, -285), new Vector2(0, -220));
            foreach (string key in new[] { "safe", "ask-panel", "activity", "toast", "insets", "journey", "sim-panel", "subtitle-panel", "part-list", "lens-state" }) named[key].gameObject.SetActive(false);
            // Keep the safe-area hierarchy active; gameplay groups hide independently.
            named["safe"].gameObject.SetActive(true);
            foreach (string key in new[] { "topbar", "bottom", "middle", "prompt-banner" }) named[key].gameObject.SetActive(false);
        }
    }
}
