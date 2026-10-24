using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// The vision-journey HUD (22 October 2026 brief: the interface felt generic and cramped, and learners could not tell
    /// what to press next). Edits the scene's Canvas instance, the one that plays:
    ///  • top-left: the journey bar, ten nodes on a line of light named for what each chapter teaches (passed = warm light,
    ///    current = cyan), the chapter title under it; top-right: Chapters, Pause and Settings only (Replay line, How to
    ///    play, Review and Sound move into the pause card);
    ///  • bottom-centre: one breathing Next-step card (what to do now) above Niko's caption bubble;
    ///  • the right column keeps the activity's tools; a quick check fills the column with big lettered answer tiles;
    ///  • a centred finale card that leads with Free Explore; Free Explore's own buttons sit in the tools.
    /// Called by Builder.ApplyRedesign; idempotent.
    public static class UiJourney
    {
        public const float Column = 400, Margin = 24, TopBand = 104;
        static readonly Color Panel = new(.035f, .09f, .16f, .92f), Control = new(.05f, .13f, .22f, .96f), Ink = new(.94f, .96f, .98f),
            Muted = new(.66f, .74f, .8f), Cyan = new(.15f, .78f, 1f), Light = new(1f, .86f, .55f), Track = new(.22f, .32f, .42f, .9f),
            NextFill = new(.02f, .2f, .3f, .95f), OnCyan = new(.02f, .08f, .14f), Node = new(.13f, .2f, .3f, 1);
        public static readonly string[] NewKeys = { "next-step", "progress-fill", "end-explore", "free-journey", "free-inside", "free-exit", "pause-tools" };

        public static void Apply(Hud hud)
        {
            // The scene's Canvas was an instance of ObservatoryCanvas.prefab, whose children cannot be re-parented: it is
            // unpacked here, and from now on the scene instance (not the prefab) is the HUD's authority.
            var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(hud.canvas.gameObject);
            if (instanceRoot) PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var root = (RectTransform)hud.canvas.transform;
            RectTransform Find(string n) => root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == n);
            RectTransform R(string n) { var r = Find(n); if (!r) throw new Exception("Canvas element missing: " + n); return r; }
            var glass = root.GetComponentsInChildren<GlassPanel>(true).First(p => p.material).material;
            var bold = AssetDatabase.LoadAssetAtPath<Font>(Builder.Root + "ThirdParty/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Bold.ttf");
            LayoutElement LE(RectTransform r) { var le = r.GetComponent<LayoutElement>(); if (!le) le = r.gameObject.AddComponent<LayoutElement>(); return le; }
            void Size(RectTransform r, float w, float h)
            {
                var le = LE(r); le.ignoreLayout = false;
                le.minWidth = le.preferredWidth = w; le.minHeight = le.preferredHeight = h; le.flexibleWidth = w < 0 ? 1 : 0; le.flexibleHeight = 0;
            }
            void Style(Text t, int size, Color colour, TextAnchor align, bool strong = false)
            {
                if (!t) return;
                t.fontSize = size; t.color = colour; t.alignment = align; t.fontStyle = FontStyle.Normal; t.resizeTextForBestFit = false;
                if (strong && bold) t.font = bold;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            }
            RectTransform Ensure(string n, RectTransform parent, params Type[] components)
            {
                var r = Find(n);
                if (!r) r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>();
                foreach (var t in components) if (!r.GetComponent(t)) r.gameObject.AddComponent(t);
                r.SetParent(parent, false); return r;
            }
            RectTransform Clone(RectTransform source, string n, RectTransform parent)
            {
                var r = Find(n);
                if (!r) { r = (RectTransform)Object.Instantiate(source.gameObject, parent, false).transform; r.name = n; }
                r.SetParent(parent, false); return r;
            }
            GlassPanel Surface(RectTransform r, Color fill, float radius)
            {
                var g = r.GetComponent<GlassPanel>();
                if (!g && !r.GetComponent<Graphic>()) { g = r.gameObject.AddComponent<GlassPanel>(); g.material = glass; }
                if (g) { g.color = fill; g.radius = radius; g.glow = .12f; }
                return g;
            }
            Text Label(RectTransform r) => r.GetComponentsInChildren<Text>(true).FirstOrDefault();
            /// A pill button with an optional left icon and a visible label.
            void Pill(RectTransform b, string text, float w, float h, Color fill, Color ink, int size, HudIcon.Symbol? symbol = null)
            {
                Size(b, w, h); Surface(b, fill, h / 2);
                foreach (var c in b.GetComponentsInChildren<Text>(true)) c.gameObject.SetActive(true);
                var t = Label(b); if (t) { if (text != null) t.text = text; Style(t, size, ink, TextAnchor.MiddleCenter, true); t.raycastTarget = false; }
                var icon = b.GetComponentsInChildren<HudIcon>(true).FirstOrDefault(i => i.transform != b);
                if (icon)
                {
                    icon.gameObject.SetActive(symbol.HasValue);
                    if (symbol.HasValue) { icon.symbol = symbol.Value; icon.color = ink; var ir = (RectTransform)icon.transform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0, .5f); ir.anchoredPosition = new Vector2(18, 0); ir.sizeDelta = new Vector2(20, 20); var ile = ir.GetComponent<LayoutElement>(); if (ile) ile.ignoreLayout = true; }
                }
                if (t) { var tr = (RectTransform)t.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(icon && icon.gameObject.activeSelf ? 44 : 14, 2); tr.offsetMax = new Vector2(-14, -2); var tle = tr.GetComponent<LayoutElement>(); if (tle) tle.ignoreLayout = true; }
                var compact = b.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
                var st = b.GetComponent<HudState>(); if (st) st.persistentGlow = false;
            }

            // ---- journey bar (top-left): ten nodes on a line of light, the chapter title beneath
            var top = R("topbar"); top.sizeDelta = new Vector2(top.sizeDelta.x, 96);
            var nav = R("Chapter Navigation"); Size(nav, 590, 96); Surface(nav, Panel, 18);
            var nv = nav.GetComponent<VerticalLayoutGroup>(); nv.padding = new RectOffset(22, 22, 14, 10); nv.spacing = 26;
            nv.childControlWidth = nv.childControlHeight = true; nv.childForceExpandWidth = true; nv.childForceExpandHeight = false;
            var progress = R("progress"); progress.SetParent(nav, false); progress.SetSiblingIndex(0); Size(progress, -1, 24);
            var row = progress.GetComponent<HorizontalLayoutGroup>(); row.spacing = 34; row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            int n = 0;
            foreach (var dot in progress.Cast<RectTransform>().Where(d => d.name.StartsWith("progress ")).ToArray())
            {
                Size(dot, 24, 24);
                var g = dot.GetComponent<GlassPanel>(); if (g) { g.radius = 12; g.color = Node; g.border = 1; g.haloWidth = 10; }
                var st = dot.GetComponent<HudState>(); if (st) { st.selected = Cyan; st.complete = Light; }
                var cap = dot.GetComponentsInChildren<Text>(true).FirstOrDefault();
                if (!cap) continue;
                cap.gameObject.SetActive(true); cap.raycastTarget = false;
                var cr = (RectTransform)cap.transform; var cle = cr.GetComponent<LayoutElement>(); if (cle) cle.ignoreLayout = true;
                cr.anchorMin = cr.anchorMax = new Vector2(.5f, 0); cr.pivot = new Vector2(.5f, 1); cr.anchoredPosition = new Vector2(0, -5); cr.sizeDelta = new Vector2(76, 16);
                Style(cap, 12, Muted, TextAnchor.UpperCenter); cap.horizontalOverflow = HorizontalWrapMode.Overflow;
                cap.text = n < Hud.StageNames.Length ? Hud.StageNames[n] : ""; n++;
            }
            Image Bar(string name, Color c, int index)
            {
                var r = Ensure(name, progress, typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                r.GetComponent<LayoutElement>().ignoreLayout = true; r.SetSiblingIndex(index);
                var img = r.GetComponent<Image>(); img.color = c; img.raycastTarget = false; return img;
            }
            var line = Bar("progress-line", Track, 0).rectTransform;
            line.anchorMin = new Vector2(0, .5f); line.anchorMax = new Vector2(1, .5f); line.offsetMin = new Vector2(12, -1); line.offsetMax = new Vector2(-12, 1);
            var fill = Bar("progress-fill", Light, 1).rectTransform;   // Hud stretches it to the current node: the light has come this far
            fill.anchorMin = fill.anchorMax = new Vector2(0, .5f); fill.offsetMin = new Vector2(12, -1.5f); fill.offsetMax = new Vector2(12, 1.5f);
            var title = R("chapter-title"); title.SetParent(nav, false); title.SetSiblingIndex(1); Size(title, -1, 22);
            var titleText = Label(title); if (titleText) { Style(titleText, 16, Ink, TextAnchor.MiddleLeft, true); titleText.horizontalOverflow = HorizontalWrapMode.Overflow; }

            // ---- three tools at the top-right; the rest live in the pause card
            var tools = R("top-buttons");
            string[] keep = { "btn-chapters", "btn-pause", "btn-settings" };
            for (int i = 0; i < keep.Length; i++) { var b = R(keep[i]); b.SetParent(tools, false); b.SetSiblingIndex(i); }
            R("btn-home").gameObject.SetActive(false);
            var pauseCard = (RectTransform)R("pause-overlay").Find("Card");
            var ptools = Ensure("pause-tools", pauseCard, typeof(GridLayoutGroup), typeof(LayoutElement));
            var grid = ptools.GetComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(196, 52); grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2; grid.childAlignment = TextAnchor.UpperCenter;
            Size(ptools, -1, 2 * 52 + 10); ptools.SetAsLastSibling();
            foreach (var (key, text, symbol) in new[] { ("btn-replay-expl", "Replay line", HudIcon.Symbol.Replay), ("btn-help", "How to play", HudIcon.Symbol.Help),
                                                        ("btn-review", "Review", HudIcon.Symbol.Review), ("btn-sound", "Sound on", HudIcon.Symbol.Sound) })
            {
                var b = R(key); b.SetParent(ptools, false);
                Pill(b, text, 196, 52, Control, Ink, 15, symbol);
                var le = b.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = false;   // the grid sizes the cells
            }

            // ---- Next step: one breathing card at the bottom centre, above Niko's caption bubble
            var bottom = R("bottom");
            var bv = bottom.GetComponent<VerticalLayoutGroup>(); bv.spacing = 12;
            var next = Ensure("next-step", bottom, typeof(CanvasRenderer), typeof(GlassPanel), typeof(HorizontalLayoutGroup), typeof(CanvasGroup), typeof(HudState), typeof(LayoutElement));
            next.SetSiblingIndex(0);
            var ng = next.GetComponent<GlassPanel>(); ng.material = glass; ng.color = NextFill; ng.radius = 30; ng.haloWidth = 18; ng.border = 1.4f; ng.raycastTarget = false;
            var ns = next.GetComponent<HudState>(); ns.target = ng; ns.surface = ng; ns.selected = NextFill; ns.persistentGlow = false;
            var nh = next.GetComponent<HorizontalLayoutGroup>(); nh.padding = new RectOffset(22, 30, 14, 14); nh.spacing = 14; nh.childAlignment = TextAnchor.MiddleCenter;
            nh.childControlWidth = nh.childControlHeight = true; nh.childForceExpandWidth = nh.childForceExpandHeight = false;
            var nle = LE(next); nle.minHeight = 64; nle.minWidth = 280; nle.flexibleWidth = 0;
            var icon = Ensure("next-icon", next, typeof(CanvasRenderer), typeof(HudIcon), typeof(LayoutElement)); icon.SetSiblingIndex(0); Size(icon, 28, 28);
            var hi = icon.GetComponent<HudIcon>(); hi.symbol = HudIcon.Symbol.Play; hi.color = Cyan; hi.stroke = 2.5f; hi.raycastTarget = false;
            var prompt = R("prompt-banner"); prompt.SetParent(next, false); prompt.SetSiblingIndex(1);
            var ple = LE(prompt); ple.ignoreLayout = false; ple.minWidth = -1; ple.preferredWidth = -1; ple.flexibleWidth = 0; ple.minHeight = ple.preferredHeight = -1;
            var pg = prompt.GetComponent<CanvasGroup>(); if (pg) Object.DestroyImmediate(pg);   // the card fades, not its text
            prompt.gameObject.SetActive(true); next.gameObject.SetActive(false);
            // The banner was a small pill with its text as a child: the card is the surface now, and the banner takes its
            // text's size so the card fits the words.
            var bannerSurface = prompt.GetComponent<GlassPanel>(); if (bannerSurface) bannerSurface.enabled = false;
            var ph = prompt.GetComponent<HorizontalLayoutGroup>(); if (!ph) ph = prompt.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.padding = new RectOffset(0, 0, 0, 0); ph.childControlWidth = ph.childControlHeight = true; ph.childForceExpandWidth = ph.childForceExpandHeight = false;
            var promptText = prompt.GetComponentInChildren<Text>(true);
            Style(promptText, 22, Ink, TextAnchor.MiddleLeft, true);
            var ptle = LE((RectTransform)promptText.transform); ptle.ignoreLayout = false; ptle.minWidth = ptle.preferredWidth = -1;

            // ---- Niko's caption bubble
            var subs = R("subtitle-panel"); subs.SetSiblingIndex(1); Surface(subs, new Color(.03f, .1f, .18f, .9f), 22);
            var sv = subs.GetComponent<VerticalLayoutGroup>(); if (sv) { sv.padding = new RectOffset(32, 32, 12, 16); sv.spacing = 4; }
            Style(R("speaker").GetComponent<Text>(), 14, Cyan, TextAnchor.MiddleCenter, true);

            // ---- right column: the activity's tools only (the old status header goes; the next step is at the bottom)
            R("info-header").gameObject.SetActive(false);
            var infoText = R("info-text"); Style(infoText.GetComponent<Text>(), 16, Ink, TextAnchor.UpperLeft);
            var ile2 = LE(infoText); ile2.minHeight = ile2.preferredHeight = -1; ile2.flexibleHeight = 0;   // as tall as its words
            foreach (var key in new[] { "inset-retina-image", "inset-perception-image" }) { var le = LE(R(key)); le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = 96; }
            var header = R("activity-header"); header.gameObject.SetActive(false);
            var lab = R("lab-tools");
            foreach (var (key, text) in new[] { ("free-inside", "Look from inside"), ("free-journey", "▶  Play the light journey"), ("free-exit", "Finish exploring") })
            {
                var b = Clone(R("btn-section"), key, lab); b.SetAsLastSibling(); b.gameObject.SetActive(false);
                var t = Label(b); if (t) t.text = text;
            }

            // ---- parts as compact chips, three a row (Free Explore lists every structure: twelve slots); tools two a row
            var partButtons = R("part-buttons"); var firstPart = (RectTransform)partButtons.GetChild(0);
            for (int i = partButtons.childCount + 1; i <= 12; i++) { var slot = Clone(firstPart, $"part-buttons {i:00}", partButtons); slot.gameObject.SetActive(false); }
            void Grid(RectTransform r, int columns, Vector2 cell)
            {
                var old = r.GetComponent<HorizontalOrVerticalLayoutGroup>(); if (old) Object.DestroyImmediate(old);
                var g = r.GetComponent<GridLayoutGroup>(); if (!g) g = r.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = cell; g.spacing = new Vector2(6, 6); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = columns;
                g.childAlignment = TextAnchor.UpperLeft; g.startAxis = GridLayoutGroup.Axis.Horizontal;
                foreach (RectTransform c in r)
                {
                    var t = Label(c); if (!t) continue;
                    Style(t, 14, Ink, TextAnchor.MiddleCenter); t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = 14;
                    var tr = (RectTransform)t.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(6, 2); tr.offsetMax = new Vector2(-6, -2);
                    var g2 = c.GetComponent<GlassPanel>(); if (g2) g2.radius = 12;
                }
            }
            Grid(partButtons, 3, new Vector2(116, 42));
            Grid(lab, 2, new Vector2(178, 44));

            // ---- quick check: the whole column, a big question and lettered answer tiles
            var act = R("activity");
            act.anchorMin = new Vector2(1, 0); act.anchorMax = new Vector2(1, 1); act.pivot = new Vector2(1, 1);
            act.anchoredPosition = new Vector2(-Margin, -TopBand); act.sizeDelta = new Vector2(Column, -(TopBand + TopBand));
            var backdrop = act.GetComponent<Graphic>(); if (backdrop) { backdrop.color = Color.clear; backdrop.raycastTarget = false; }   // taps reach the model
            var acard = (RectTransform)act.Find("Card");
            acard.anchorMin = Vector2.zero; acard.anchorMax = Vector2.one; acard.pivot = new Vector2(.5f, .5f); acard.offsetMin = acard.offsetMax = Vector2.zero;
            Surface(acard, Panel, 20);
            var content = R("act-options").parent as RectTransform;
            var cv = content.GetComponent<VerticalLayoutGroup>(); if (cv) { cv.padding = new RectOffset(22, 22, 22, 22); cv.spacing = 14; }
            Style(R("act-title").GetComponent<Text>(), 14, Cyan, TextAnchor.MiddleLeft, true);
            Style(R("act-prompt").GetComponent<Text>(), 23, Ink, TextAnchor.UpperLeft, true);
            var options = R("act-options"); var ov = options.GetComponent<VerticalLayoutGroup>(); if (ov) { ov.spacing = 10; ov.childForceExpandHeight = false; }
            foreach (var o in options.Cast<RectTransform>().ToArray())
            {
                Size(o, -1, 60); Surface(o, Control, 16);
                var t = Label(o); if (!t) continue;
                Style(t, 18, Ink, TextAnchor.MiddleLeft); var tr = (RectTransform)t.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(18, 4); tr.offsetMax = new Vector2(-14, -4);
            }
            Style(R("act-tap-hint").GetComponent<Text>(), 16, Muted, TextAnchor.UpperLeft);
            Style(R("act-feedback").GetComponent<Text>(), 18, Ink, TextAnchor.UpperLeft, true);
            Style(R("act-hint").GetComponent<Text>(), 16, Muted, TextAnchor.UpperLeft);
            foreach (var b in R("act-buttons").Cast<RectTransform>().ToArray()) Pill(b, null, 170, 52, Control, Ink, 16);

            // ---- the finale card, centred, Free Explore first
            var end = R("end-menu");
            end.anchorMin = Vector2.zero; end.anchorMax = Vector2.one; end.pivot = new Vector2(.5f, .5f); end.offsetMin = end.offsetMax = Vector2.zero;
            var endBack = end.GetComponent<Graphic>(); if (endBack) endBack.color = new Color(.01f, .02f, .04f, .45f);
            var ecard = (RectTransform)end.Find("Card");
            ecard.anchorMin = ecard.anchorMax = new Vector2(.5f, .5f); ecard.pivot = new Vector2(.5f, .5f); ecard.anchoredPosition = Vector2.zero;
            ecard.sizeDelta = new Vector2(640, ecard.sizeDelta.y); Surface(ecard, Panel, 24);
            var ev = ecard.GetComponent<VerticalLayoutGroup>(); if (!ev) ev = ecard.gameObject.AddComponent<VerticalLayoutGroup>();
            ev.padding = new RectOffset(36, 36, 30, 30); ev.spacing = 12; ev.childAlignment = TextAnchor.UpperCenter;
            ev.childControlWidth = ev.childControlHeight = true; ev.childForceExpandWidth = true; ev.childForceExpandHeight = false;
            var efit = ecard.GetComponent<ContentSizeFitter>(); if (!efit) efit = ecard.gameObject.AddComponent<ContentSizeFitter>();
            efit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; efit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var explore = Clone(R("end-replay"), "end-explore", ecard);
            var endTitle = Find("end-title"); int after = endTitle ? endTitle.GetSiblingIndex() + 2 : 0;
            explore.SetSiblingIndex(Mathf.Min(after, ecard.childCount - 1));
            Pill(explore, "FREE EXPLORE  ·  spin, zoom, look inside", -1, 66, Cyan, OnCyan, 20, HudIcon.Symbol.Eye);
            foreach (var (key, text) in new[] { ("end-replay", "Replay the investigation"), ("end-review", "Review what you learned"), ("end-menu-button", "Main menu"), ("end-exit", "Exit") })
                Pill(R(key), text, -1, 52, Control, Ink, 16);
            if (endTitle) Style(endTitle.GetComponent<Text>(), 34, Ink, TextAnchor.MiddleCenter, true);
            var recap = Find("end-recap"); if (recap) Style(recap.GetComponent<Text>(), 17, Muted, TextAnchor.MiddleCenter);

            // ---- toast below the journey bar
            var toast = R("toast"); toast.anchoredPosition = new Vector2(Margin, -(TopBand + 28));

            // ---- bindings for the new keys
            foreach (var key in NewKeys)
                if (!hud.bindings.Any(b => b.key == key && b.target)) hud.bindings = hud.bindings.Where(b => b.key != key).Append(new Hud.Binding { key = key, target = R(key) }).ToArray();
            EditorUtility.SetDirty(hud);
            foreach (var r in root.GetComponentsInChildren<RectTransform>(true)) EditorUtility.SetDirty(r);
            Debug.Log("HUMAN_EYE_UI_JOURNEY_APPLIED");
        }
    }
}
