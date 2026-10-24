using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// UI pass (7 October 2026): premium medical layout at the screen edges. Start screen as a left title column
    /// (THE HUMAN EYE, amber BEGIN, How to play, Chapters); top-left chapter chip with progress; top-right icon tools;
    /// a right column that only holds the current instruction and activity (sized to its content); captions centred
    /// under the subject; Talk to Niko at the bottom-right; letterboxed intro; a Chapters modal. Calm navy surfaces
    /// (the glass shader draws cyan only for hover, focus and the one required action). Idempotent; keeps scene edits.
    /// Batch: -executeMethod HumanEye.EditorTools.UiPass.Apply
    public static class UiPass
    {
        public const float Column = 400, Margin = 24, TopBand = 104, BottomBand = 104;
        static readonly Color Panel = new(.039f, .071f, .125f, .9f), Control = new(.059f, .106f, .176f, .96f), Ink = new(.933f, .957f, .973f),
            Muted = new(.663f, .733f, .796f), Cyan = new(.133f, .827f, .933f), Amber = new(.961f, .714f, .247f), OnAmber = new(.102f, .071f, .012f),
            Scrim = new(.02f, .03f, .05f, .8f), Letterbox = new(.012f, .016f, .024f, 1);
        static readonly string[] NewKeys = { "start-help", "start-chapters", "btn-chapters", "chapters-panel", "chapters-list", "chapters-close" };

        [MenuItem("Tools/Human Eye/Authoring/Apply UI pass (keeps scene edits)", priority = 8)]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var hud = Object.FindFirstObjectByType<Hud>(FindObjectsInactive.Include);
            var touched = new HashSet<string>();
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try { Edit(prefab.transform, touched); PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            // The scene instance follows the prefab for everything this pass placed.
            var instances = hud.canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var r in instances.Where(r => touched.Contains(r.name)))
            {
                foreach (var c in r.GetComponents<Component>()) PrefabUtility.RevertObjectOverride(c, InteractionMode.AutomatedAction);
                PrefabUtility.RevertObjectOverride(r.gameObject, InteractionMode.AutomatedAction);
            }
            foreach (var key in NewKeys)
                if (!hud.bindings.Any(b => b.key == key)) hud.bindings = hud.bindings.Append(new Hud.Binding { key = key, target = instances.First(r => r.name == key) }).ToArray();
            var shift = hud.mainCamera ? hud.mainCamera.GetComponent<ViewportShift>() : null;
            if (shift) { shift.columnPixels = Column + 2 * Margin - 8; EditorUtility.SetDirty(shift); }
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_UI_PASS_APPLIED placed {touched.Count} elements");
        }

        static void Edit(Transform root, HashSet<string> touched)
        {
            RectTransform Find(string n) => root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == n);
            RectTransform R(string n) { var r = Find(n); if (!r) throw new Exception("Canvas element missing: " + n); touched.Add(n); return r; }
            RectTransform Child(RectTransform parent, string n) { var t = parent.Find(n); return t ? (RectTransform)t : null; }
            void Place(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
            { r.anchorMin = min; r.anchorMax = max; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; touched.Add(r.name); }
            LayoutElement LE(RectTransform r) { var le = r.GetComponent<LayoutElement>(); if (!le) le = r.gameObject.AddComponent<LayoutElement>(); return le; }
            void Size(RectTransform r, float w, float h)
            {
                var le = LE(r);
                le.minWidth = le.preferredWidth = w; le.minHeight = le.preferredHeight = h;
                le.flexibleWidth = w < 0 ? 1 : 0; le.flexibleHeight = 0;
            }
            RectTransform Ensure(string n, RectTransform parent, params Type[] components)
            {
                var r = Find(n);
                if (!r) r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>();
                foreach (var t in components) if (!r.GetComponent(t)) r.gameObject.AddComponent(t);
                r.SetParent(parent, false); touched.Add(n); return r;
            }
            RectTransform Clone(RectTransform source, string n, RectTransform parent)
            {
                var r = Find(n);
                if (!r) { r = (RectTransform)Object.Instantiate(source.gameObject, parent, false).transform; r.name = n; }
                r.SetParent(parent, false); touched.Add(n); return r;
            }
            HorizontalLayoutGroup Row(RectTransform r, float spacing, TextAnchor align)
            {
                var g = r.GetComponent<HorizontalLayoutGroup>(); if (!g) g = r.gameObject.AddComponent<HorizontalLayoutGroup>();
                g.spacing = spacing; g.childAlignment = align; g.childControlWidth = g.childControlHeight = true;
                g.childForceExpandWidth = false; g.childForceExpandHeight = true; g.padding = new RectOffset(0, 0, 0, 0);
                return g;
            }
            Text Caption(RectTransform r) => r.GetComponentsInChildren<Text>(true).FirstOrDefault();
            void Style(Text t, int size, Color colour, TextAnchor align, FontStyle style = FontStyle.Normal)
            { if (!t) return; t.fontSize = size; t.color = colour; t.alignment = align; t.fontStyle = style; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; }
            void Surface(RectTransform r, Color fill, float radius) { var g = r.GetComponent<GlassPanel>(); if (g) { g.color = fill; g.radius = radius; g.glow = .12f; } }
            void IconOnly(RectTransform b, HudIcon.Symbol? symbol = null)
            {
                var own = b.GetComponent<HudIcon>();
                if (own)
                {
                    // Home was a bare 22 px icon: give it a child icon and the same 48 px surface as the other tools
                    var child = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>(); child.SetParent(b, false);
                    var ci = child.gameObject.AddComponent<HudIcon>(); ci.symbol = own.symbol; ci.stroke = own.stroke; ci.raycastTarget = false;
                    var button = b.GetComponent<Button>(); Object.DestroyImmediate(own);
                    var gp = b.gameObject.AddComponent<GlassPanel>(); gp.material = root.GetComponentsInChildren<GlassPanel>(true).First(p => p != gp && p.material).material;
                    if (button) button.targetGraphic = gp;
                }
                Size(b, 48, 48); Surface(b, Control, 24); LE(b).ignoreLayout = false;   // Home used to float inside the old title
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(.5f, .5f); b.sizeDelta = new Vector2(48, 48);
                var compact = b.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
                foreach (var t in b.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false);
                var icon = b.GetComponentsInChildren<HudIcon>(true).FirstOrDefault(i => i.transform != b);
                if (!icon) return;
                var ir = (RectTransform)icon.transform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(.5f, .5f); ir.anchoredPosition = Vector2.zero; ir.sizeDelta = new Vector2(22, 22);
                var ile = ir.GetComponent<LayoutElement>(); if (ile) ile.ignoreLayout = true;
                if (symbol.HasValue) icon.symbol = symbol.Value;
                icon.color = Ink; icon.gameObject.SetActive(true);
            }

            // ---- calm surfaces everywhere: navy panels, raised controls
            foreach (var g in root.GetComponentsInChildren<GlassPanel>(true))
            {
                bool control = g.GetComponent<Selectable>();
                g.color = control ? Control : Panel; g.radius = control ? Mathf.Min(g.radius, 24) : 16; g.glow = .12f;
            }

            // ---- start screen: a title column on the left; the preserved 3D starting view keeps the rest of the screen
            var start = R("start-screen"); var card = Child(start, "Card"); touched.Add(card.name);
            Place(card, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(88, 0), new Vector2(690, 0));
            Surface(card, new Color(.02f, .035f, .06f, .62f), 22);
            var v = card.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(40, 40, 38, 34); v.spacing = 14; v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true; v.childForceExpandWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
            var labels = Enumerable.Range(0, card.childCount).Select(i => (RectTransform)card.GetChild(i)).Where(c => c.name == "Label").ToList();
            var ornament = Child(labels[0], "Eye ornament"); if (ornament) ornament.gameObject.SetActive(false);
            labels[0].GetComponent<Text>().text = "AN INTERACTIVE LESSON WITH NIKO"; Style(labels[0].GetComponent<Text>(), 15, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold); Size(labels[0], -1, 22);
            labels[1].GetComponent<Text>().text = "THE HUMAN EYE"; Style(labels[1].GetComponent<Text>(), 66, Ink, TextAnchor.MiddleLeft, FontStyle.Bold); Size(labels[1], -1, 78);
            labels[2].GetComponent<Text>().text = "How you see — from light to brain, with Niko, your Vision Investigator Officer."; Style(labels[2].GetComponent<Text>(), 21, Muted, TextAnchor.UpperLeft); Size(labels[2], -1, 56);
            var footer = labels[labels.Count - 1];
            footer.GetComponent<Text>().text = "Sound on for Niko's voice  ·  Mouse, touch and keyboard all work"; Style(footer.GetComponent<Text>(), 15, Muted, TextAnchor.MiddleLeft); Size(footer, -1, 22);
            var actions = Ensure("start-actions", card); Row(actions, 12, TextAnchor.MiddleLeft); Size(actions, -1, 64);
            var more = Ensure("start-more", card); Row(more, 12, TextAnchor.MiddleLeft); Size(more, -1, 52);
            var begin = R("start-begin"); var settings = R("start-settings"); var cont = R("start-continue");
            var help = Clone(settings, "start-help", actions); var chapters = Clone(settings, "start-chapters", actions);
            begin.SetParent(actions, false); begin.SetSiblingIndex(0); help.SetSiblingIndex(1); chapters.SetSiblingIndex(2);
            cont.SetParent(more, false); settings.SetParent(more, false); cont.SetSiblingIndex(0); settings.SetSiblingIndex(1);
            void StartButton(RectTransform b, string text, float w, float h, bool primary)
            {
                Size(b, w, h); Surface(b, primary ? Amber : Control, h / 2);
                var chevron = Child(b, "Chevron"); if (chevron) chevron.gameObject.SetActive(false);
                var t = b.GetComponentsInChildren<Text>(true).FirstOrDefault(); if (t && text != null) t.text = text;
                Style(t, primary ? 20 : 16, primary ? OnAmber : Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                var icon = b.GetComponentsInChildren<HudIcon>(true).FirstOrDefault(); if (icon) icon.color = primary ? OnAmber : Ink;
                if (t) { t.resizeTextForBestFit = false; var tr = (RectTransform)t.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(icon && icon.gameObject.activeSelf ? 50 : 16, 4); tr.offsetMax = new Vector2(-14, -4); }
                if (icon) { var ir = (RectTransform)icon.transform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0, .5f); ir.anchoredPosition = new Vector2(18, 0); ir.sizeDelta = new Vector2(22, 22); }
                var compact = b.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
                if (primary && icon) icon.symbol = HudIcon.Symbol.Play;
            }
            StartButton(begin, "BEGIN", 208, 64, true); StartButton(help, "HOW TO PLAY", 196, 64, false); StartButton(chapters, "CHAPTERS", 176, 64, false);
            StartButton(cont, null, 360, 52, false); StartButton(settings, "SETTINGS", 172, 52, false);
            var helpIcon = help.GetComponentsInChildren<HudIcon>(true).FirstOrDefault(); if (helpIcon) helpIcon.symbol = HudIcon.Symbol.Help;
            var chapterIcon = chapters.GetComponentsInChildren<HudIcon>(true).FirstOrDefault(); if (chapterIcon) chapterIcon.symbol = HudIcon.Symbol.List;
            actions.SetSiblingIndex(R("start-notice").GetSiblingIndex() + 1); more.SetSiblingIndex(actions.GetSiblingIndex() + 1); footer.SetAsLastSibling();

            // ---- top band: chapter chip (top-left), icon tools (top-right)
            var top = R("topbar");
            Place(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -Margin), new Vector2(-2 * Margin, 68));
            var tg = top.GetComponent<HorizontalLayoutGroup>(); tg.spacing = 12; tg.childAlignment = TextAnchor.MiddleLeft;
            tg.childControlWidth = tg.childControlHeight = true; tg.childForceExpandWidth = tg.childForceExpandHeight = false;
            var nav = R("Chapter Navigation"); Size(nav, 372, 68); Surface(nav, Panel, 16);
            var nv = nav.GetComponent<VerticalLayoutGroup>(); nv.padding = new RectOffset(16, 16, 9, 9); nv.spacing = 6; nv.childAlignment = TextAnchor.UpperLeft;
            nv.childControlWidth = true; nv.childForceExpandWidth = true; nv.childControlHeight = true; nv.childForceExpandHeight = false;
            var title = R("chapter-title"); title.SetParent(nav, false); title.SetSiblingIndex(0); Size(title, -1, 22);
            var titleText = Child(title, "Caption");
            if (titleText)
            {
                var tt = titleText.GetComponent<Text>(); Style(tt, 15, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                tt.resizeTextForBestFit = false; tt.horizontalOverflow = HorizontalWrapMode.Overflow; titleText.offsetMin = titleText.offsetMax = Vector2.zero;
            }
            var progress = R("progress"); Row(progress, 6, TextAnchor.MiddleLeft).childForceExpandHeight = false; Size(progress, -1, 22);
            // progress as slim segments (labels, not controls): upcoming line, current cyan, complete teal
            Size(progress, -1, 8); progress.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            foreach (RectTransform dot in progress)
            {
                Size(dot, 28, 6); touched.Add(dot.name);
                var c = Caption(dot); if (c) c.gameObject.SetActive(false);
                var g = dot.GetComponent<GlassPanel>(); if (g) { g.radius = 3; g.color = new Color(.2f, .29f, .38f, 1); g.border = .5f; g.haloWidth = 6; }
            }
            var tools = R("top-buttons"); Row(tools, 8, TextAnchor.MiddleRight).childForceExpandHeight = false;
            var home = R("btn-home"); home.SetParent(tools, false);
            var chapterButton = Clone(R("btn-help"), "btn-chapters", tools);
            var sound = R("btn-sound"); sound.SetParent(tools, false);
            if (!sound.GetComponentInChildren<HudIcon>(true)) Object.Instantiate(R("btn-help").GetComponentInChildren<HudIcon>(true).gameObject, sound, false).name = "Icon";
            var order = new[] { "btn-home", "btn-chapters", "btn-replay-expl", "btn-pause", "btn-help", "btn-review", "btn-sound", "btn-settings" };
            for (int i = 0; i < order.Length; i++)
            {
                var b = R(order[i]); b.SetParent(tools, false); b.SetSiblingIndex(i);
                IconOnly(b, order[i] switch { "btn-home" => HudIcon.Symbol.Home, "btn-chapters" => HudIcon.Symbol.List, "btn-sound" => HudIcon.Symbol.Sound, "btn-replay-expl" => HudIcon.Symbol.Replay, _ => (HudIcon.Symbol?)null });
            }

            // ---- right column: instruction + activity only, sized to content, clear of the top tools and Talk to Niko
            var col = R("right-column");
            Place(col, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -TopBand), new Vector2(Column, -(TopBand + BottomBand)));
            var cv = col.GetComponent<VerticalLayoutGroup>(); cv.childAlignment = TextAnchor.UpperCenter; cv.childControlHeight = true; cv.childForceExpandHeight = false; cv.spacing = 12;
            var info = R("info-panel"); var iv = info.GetComponent<VerticalLayoutGroup>(); iv.padding = new RectOffset(16, 16, 12, 14); iv.spacing = 8;
            Size(R("info-header"), -1, 56);   // the niko-portrait card is gone (Builder.ApplyFloatingNiko)
            var status = R("niko-status"); Style(status.GetComponent<Text>(), 14, Muted, TextAnchor.MiddleLeft); Size(status, -1, 40);
            foreach (RectTransform slot in R("part-buttons")) { var le = LE(slot); le.minHeight = le.preferredHeight = 48; touched.Add(slot.name); }   // 48 px touch rule
            var activity = R("activity-panel"); var ale = LE(activity); ale.minHeight = -1; ale.preferredHeight = -1; ale.flexibleHeight = 0;
            var header = R("activity-header"); header.GetComponent<Text>().text = "ACTIVITY"; Style(header.GetComponent<Text>(), 13, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            foreach (var key in new[] { "activity", "end-menu" }) Place(R(key), new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -TopBand), new Vector2(Column, -(TopBand + BottomBand)));
            Place(R("ask-panel"), new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Margin, -TopBand), new Vector2(Column, -(TopBand + 96)));
            var labelsLayer = R("labels-layer"); labelsLayer.offsetMin = Vector2.zero; labelsLayer.offsetMax = new Vector2(-(Column + 2 * Margin), 0);

            // ---- captions centred under the subject (the 3D area left of the column)
            var bottom = R("bottom");
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(.5f, 0);
            bottom.offsetMin = new Vector2(2 * Margin, Margin); bottom.offsetMax = new Vector2(-(Column + 3 * Margin), Margin + 10);
            var bv = bottom.GetComponent<VerticalLayoutGroup>(); bv.childAlignment = TextAnchor.LowerCenter; bv.childControlWidth = true; bv.childForceExpandWidth = false;
            bv.childControlHeight = true; bv.childForceExpandHeight = false; bv.padding = new RectOffset(0, 0, 0, 0);
            var subtitles = R("subtitle-panel"); subtitles.SetParent(bottom, false);
            var sle = LE(subtitles); sle.preferredWidth = 980; sle.flexibleWidth = 0; sle.minWidth = 200;
            if (!subtitles.GetComponent<GlassPanel>())
            {
                var backing = subtitles.gameObject.AddComponent<GlassPanel>();
                backing.material = root.GetComponentsInChildren<GlassPanel>(true).First(p => p != backing && p.material).material; backing.raycastTarget = false;
            }
            Surface(subtitles, Scrim, 14);
            var sv = subtitles.GetComponent<VerticalLayoutGroup>(); if (sv) { sv.padding = new RectOffset(28, 28, 10, 14); sv.spacing = 2; sv.childAlignment = TextAnchor.UpperCenter; }
            Style(R("speaker").GetComponent<Text>(), 13, Cyan, TextAnchor.MiddleCenter, FontStyle.Bold); Size(R("speaker"), -1, 20);
            var subtitleText = R("subtitle-text").GetComponent<Text>(); subtitleText.alignment = TextAnchor.UpperCenter; subtitleText.color = Ink;

            // ---- world labels: calm pill, hairline cyan leader, anchor dot on the structure
            var glass = root.GetComponentsInChildren<GlassPanel>(true).First(p => p.material).material;
            foreach (RectTransform label in labelsLayer)
            {
                var leader = Child(label, "Leader"); if (!leader) continue;
                var line = leader.GetComponent<Image>(); if (line) { line.color = new Color(Cyan.r, Cyan.g, Cyan.b, .75f); line.raycastTarget = false; }
                var dot = Child(leader, "Dot");
                if (!dot)
                {
                    dot = new GameObject("Dot", typeof(RectTransform)).GetComponent<RectTransform>(); dot.SetParent(leader, false);
                    var gp = dot.gameObject.AddComponent<GlassPanel>(); gp.material = glass; gp.raycastTarget = false;
                }
                dot.anchorMin = dot.anchorMax = new Vector2(1, .5f); dot.pivot = new Vector2(.5f, .5f); dot.anchoredPosition = Vector2.zero; dot.sizeDelta = new Vector2(8, 8);
                var dg = dot.GetComponent<GlassPanel>(); dg.color = Cyan; dg.radius = 4; dg.border = .5f; dg.haloWidth = 4; dg.glow = .12f;
                Style(label.GetComponentsInChildren<Text>(true).FirstOrDefault(), 16, Ink, TextAnchor.MiddleCenter);
            }

            // ---- Talk to Niko: bottom-right corner
            var ask = R("btn-ask"); ask.SetParent(R("safe"), false);
            Place(ask, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Margin, Margin), new Vector2(236, 56));
            var askLe = ask.GetComponent<LayoutElement>(); if (askLe) askLe.ignoreLayout = true;
            Surface(ask, Control, 28);

            // ---- toast under the chapter chip
            Place(R("toast"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(Margin, -TopBand), new Vector2(560, 52));

            // ---- cinematic letterbox: bars top and bottom; caption and Skip live in the lower bar
            var overlay = R("intro-overlay");
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.pivot = new Vector2(.5f, .5f); overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            RectTransform Bar(string n, float from, float to)
            {
                var r = Ensure(n, overlay, typeof(CanvasRenderer), typeof(Image));
                r.anchorMin = new Vector2(0, from); r.anchorMax = new Vector2(1, to); r.offsetMin = r.offsetMax = Vector2.zero;
                var img = r.GetComponent<Image>(); img.color = Letterbox; img.raycastTarget = false; return r;
            }
            var barTop = Bar("letterbox-top", .885f, 1); var barBottom = Bar("letterbox-bottom", 0, .115f);
            barTop.SetSiblingIndex(0); barBottom.SetSiblingIndex(1);
            var caption = R("intro-caption"); caption.SetParent(barBottom, false);
            caption.anchorMin = new Vector2(.14f, 0); caption.anchorMax = new Vector2(.86f, 1); caption.offsetMin = caption.offsetMax = Vector2.zero;
            Style(caption.GetComponent<Text>(), 20, new Color(Ink.r, Ink.g, Ink.b, .9f), TextAnchor.MiddleCenter);
            var skip = R("btn-skip-intro"); skip.SetParent(barBottom, false);
            Place(skip, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-32, 0), new Vector2(160, 48));
            Surface(skip, new Color(.059f, .106f, .176f, .7f), 24);

            // ---- Chapters modal (from the help card): two columns of chapter buttons, locked ones explain themselves
            var helpPanel = R("help-panel");
            var cp = Clone(helpPanel, "chapters-panel", (RectTransform)helpPanel.parent); cp.SetSiblingIndex(helpPanel.GetSiblingIndex() + 1); cp.gameObject.SetActive(false);
            var ccard = Child(cp, "Card"); touched.Add(ccard.name);
            Place(ccard, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(700, 0));
            var closeOld = Child(ccard, "help-close"); if (closeOld) closeOld.name = "chapters-close";
            var close = R("chapters-close"); var closeText = close.GetComponentsInChildren<Text>(true).FirstOrDefault(); if (closeText) closeText.text = "Close";
            var clabels = Enumerable.Range(0, ccard.childCount).Select(i => (RectTransform)ccard.GetChild(i)).Where(c => c.name == "Label").ToList();
            if (clabels.Count >= 2)
            {
                clabels[0].GetComponent<Text>().text = "CHAPTERS"; Style(clabels[0].GetComponent<Text>(), 28, Ink, TextAnchor.MiddleLeft, FontStyle.Bold); Size(clabels[0], -1, 40);
                clabels[1].GetComponent<Text>().text = "Replay any chapter you have reached. New chapters open as you go."; Style(clabels[1].GetComponent<Text>(), 17, Muted, TextAnchor.UpperLeft); Size(clabels[1], -1, 28);
                foreach (var extra in clabels.Skip(2)) Object.DestroyImmediate(extra.gameObject);
            }
            var list = Ensure("chapters-list", ccard); list.SetSiblingIndex(2);
            var grid = list.GetComponent<GridLayoutGroup>(); if (!grid) grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(304, 52); grid.spacing = new Vector2(12, 10); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter; Size(list, -1, 5 * 52 + 4 * 10);
            for (int i = 1; i <= 10; i++)
            {
                var slot = Clone(R("start-settings"), $"chapter {i:00}", list); slot.SetSiblingIndex(i - 1);
                var t = slot.GetComponentsInChildren<Text>(true).FirstOrDefault(); if (t) { t.text = $"Chapter {i}"; Style(t, 16, Ink, TextAnchor.MiddleLeft); var tr = (RectTransform)t.transform; tr.offsetMin = new Vector2(16, 4); tr.offsetMax = new Vector2(-12, -4); }
                foreach (var g in slot.GetComponentsInChildren<HudIcon>(true)) g.gameObject.SetActive(false);
                Surface(slot, Control, 14);
            }
            close.SetAsLastSibling();

            // ---- transitions: everything that appears and disappears during the lesson carries a CanvasGroup so Hud.Pop
            // fades it instead of snapping (Hud releases raycasts on a group that is on its way out). Idempotent, absolute.
            foreach (var key in new[] { "activity", "ask-panel", "end-menu", "toast", "btn-ask", "subtitle-panel", "prompt-banner", "info-text", "part-list", "sim-panel", "view-controls", "abilities", "activity-panel", "info-panel", "lens-state", "labels-layer" })
            {
                var r = R(key); var group = r.GetComponent<CanvasGroup>(); if (!group) group = r.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 1; group.interactable = group.blocksRaycasts = true; group.ignoreParentGroups = false;
            }
        }
    }
}
