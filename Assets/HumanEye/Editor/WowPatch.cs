using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using G = HumanEye.EyeGeometry;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Idempotent, save-guarded presentation redesign of the SAVED scene and Canvas prefab (never a regeneration):
    /// right-hand information + activities column, Niko portrait camera, no apple on the retina, rebuilt rods and cones,
    /// refined eye materials and mesh density, overhead/closing shots and anchor, lesson SFX, closing line, content data.
    /// Batch: -executeMethod HumanEye.EditorTools.WowPatch.Apply
    public static class WowPatch
    {
        // High enough that apple, eye, nerve and brain share the frame left of the panel column (the lens shift centres them at 0.4 of the width).
        public static readonly Vector3 OverviewPos = new(.5f, 9.5f, -2.2f), OverviewLook = new(.5f, 1.45f, .8f);
        public const float OverviewFov = 48;
        public static readonly Vector3 ClosingPos = new(-.5f, 1.6f, -2.5f), ClosingLook = new(-.25f, 1.3f, .4f), ClosingAnchor = new(-1.05f, .3f, -.35f);
        public const float ClosingFov = 36;
        const float ColumnWidth = 380, ColumnMargin = 16;
        const string SfxFolder = Builder.Root + "Audio/SFX/";

        [MenuItem("Tools/Human Eye/Authoring/Apply WOW redesign (keeps scene edits)", priority = 5)]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Builder.BuildData();
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var hud = stage.hud;
            var portrait = PortraitTexture();
            Canvas(hud, portrait);
            Instance(hud);
            Portrait(stage, portrait);
            RetinaImage(stage);
            Builder.RebuildMicro(stage);
            Eye();
            Shots(stage);
            Anchors(stage);
            Sfx(stage);
            foreach (var o in new Object[] { stage, stage.cams, stage.hud, stage.sfx, stage.signals, stage.rays }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_WOW_APPLIED");
        }

        // ---------------- Canvas: right-hand column (information above, view/activities below) ----------------
        static RectTransform Find(GameObject prefab, string name) => prefab.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == name);

        static RectTransform Ensure(GameObject prefab, string name, RectTransform parent, params Type[] components)
        {
            var r = Find(prefab, name);
            if (!r) { r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); }
            foreach (var t in components) if (!r.GetComponent(t)) r.gameObject.AddComponent(t);
            if (r.parent != parent) r.SetParent(parent, false);
            return r;
        }

        static T Swap<T>(RectTransform r) where T : HorizontalOrVerticalLayoutGroup
        {
            var existing = r.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (existing is T t) return t;
            int spacing = existing ? (int)existing.spacing : 8; var pad = existing ? existing.padding : new RectOffset();
            if (existing) Object.DestroyImmediate(existing);
            var group = r.gameObject.AddComponent<T>();
            group.spacing = spacing; group.padding = pad; group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false; group.childAlignment = TextAnchor.UpperCenter;
            return group;
        }

        static void Column(RectTransform r, float yMin = 0, float yMax = 1, float bottom = ColumnMargin)
        {
            r.anchorMin = new Vector2(1, yMin); r.anchorMax = new Vector2(1, yMax); r.pivot = new Vector2(1, .5f);
            r.anchoredPosition = new Vector2(-ColumnMargin, 0); r.sizeDelta = new Vector2(ColumnWidth, -2 * bottom);
            r.offsetMin = new Vector2(r.offsetMin.x, bottom); r.offsetMax = new Vector2(r.offsetMax.x, -bottom);
        }

        static LayoutElement Layout(RectTransform r, float minH = -1, float prefH = -1, float flexH = -1, float prefW = -1, float minW = -1, float flexW = -1)
        {
            var l = r.GetComponent<LayoutElement>(); if (!l) l = r.gameObject.AddComponent<LayoutElement>();
            l.minHeight = minH; l.preferredHeight = prefH; l.flexibleHeight = flexH; l.preferredWidth = prefW; l.minWidth = minW; l.flexibleWidth = flexW;
            return l;
        }

        static Text Label(GameObject prefab, string name, RectTransform parent, Font font, int size, string text, Color colour, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = Ensure(prefab, name, parent, typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
            var t = r.GetComponent<Text>(); t.font = font; t.fontSize = size; t.text = text; t.color = colour; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = false;
            Layout(r, minH: size * 1.4f);
            return t;
        }

        static void Canvas(Hud hud, RenderTexture portrait)
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                RectTransform R(string n) => Find(prefab, n) ?? throw new Exception("Canvas element missing: " + n);
                var safe = R("safe"); var subtitleText = R("subtitle-text").GetComponent<Text>();
                var font = subtitleText.font; var ink = subtitleText.color; var dim = new Color(ink.r, ink.g, ink.b, .72f);
                var glassSource = R("subtitle-panel").GetComponent<GlassPanel>();
                void Glass(RectTransform r) { var g = r.GetComponent<GlassPanel>(); if (!g) g = r.gameObject.AddComponent<GlassPanel>(); EditorUtility.CopySerialized(glassSource, g); g.radius = 18; g.glow = .1f; }

                // The column: information panel (fixed height) above the activities panel (takes the rest).
                var column = Ensure(prefab, "right-column", safe, typeof(VerticalLayoutGroup));
                Column(column);
                var vlg = column.GetComponent<VerticalLayoutGroup>(); vlg.spacing = 12; vlg.childControlWidth = vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false; vlg.childAlignment = TextAnchor.UpperCenter;

                var info = Ensure(prefab, "info-panel", column, typeof(CanvasRenderer)); Glass(info);
                var infoGroup = Swap<VerticalLayoutGroup>(info); infoGroup.padding = new RectOffset(14, 14, 12, 12); infoGroup.spacing = 8;
                Layout(info, minH: 0, flexH: 0);

                var header = Ensure(prefab, "info-header", info, typeof(HorizontalLayoutGroup));
                var hlg = header.GetComponent<HorizontalLayoutGroup>(); hlg.spacing = 10; hlg.childControlWidth = hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false; hlg.childAlignment = TextAnchor.MiddleLeft;
                Layout(header, minH: 80, prefH: 80, flexH: 0);
                var portraitRect = Ensure(prefab, "niko-portrait", header, typeof(CanvasRenderer), typeof(RawImage));
                var raw = portraitRect.GetComponent<RawImage>(); raw.texture = portrait; raw.raycastTarget = false; raw.color = Color.white;
                Layout(portraitRect, minH: 80, prefH: 80, prefW: 80, minW: 80, flexW: 0);
                var titles = Ensure(prefab, "info-titles", header, typeof(VerticalLayoutGroup));
                var tg = titles.GetComponent<VerticalLayoutGroup>(); tg.spacing = 2; tg.childControlWidth = tg.childControlHeight = true; tg.childForceExpandWidth = true; tg.childForceExpandHeight = false; tg.childAlignment = TextAnchor.MiddleLeft;
                Layout(titles, flexW: 1);
                var chapterTitle = R("chapter-title"); chapterTitle.SetParent(titles, false); Layout(chapterTitle, minH: 52, prefH: 52, flexH: 0);
                var titleCaption = chapterTitle.Find("Caption")?.GetComponent<Text>(); if (titleCaption) { titleCaption.fontSize = 15; titleCaption.horizontalOverflow = HorizontalWrapMode.Wrap; titleCaption.verticalOverflow = VerticalWrapMode.Truncate; titleCaption.alignment = TextAnchor.MiddleLeft; }
                var status = Label(prefab, "niko-status", titles, font, 13, "", dim); Layout((RectTransform)status.transform, minH: 24, prefH: 24, flexH: 0);
                var ask = R("btn-ask"); ask.SetParent(header, false); ask.SetAsLastSibling(); Layout(ask, minH: 64, prefH: 64, prefW: 86, minW: 86, flexW: 0);
                ask.GetComponentInChildren<Text>(true).text = "Talk to Niko";

                var prompt = R("prompt-banner"); prompt.SetParent(info, false); Layout(prompt, minH: 48);
                var promptText = prompt.GetComponentInChildren<Text>(true); promptText.fontSize = 18; promptText.alignment = TextAnchor.MiddleLeft;
                var infoText = Label(prefab, "info-text", info, font, 17, "", ink); infoText.gameObject.SetActive(false);
                var subtitles = R("subtitle-panel"); subtitles.SetParent(info, false); subtitles.SetAsLastSibling();
                var sg = Swap<VerticalLayoutGroup>(subtitles); sg.padding = new RectOffset(12, 12, 10, 10); sg.spacing = 4; sg.childAlignment = TextAnchor.UpperLeft;
                Layout(subtitles, minH: 60);
                R("speaker").GetComponent<Text>().fontSize = 13; subtitleText.alignment = TextAnchor.UpperLeft;
                var readContinue = R("btn-read-continue"); Layout(readContinue, minH: 48, prefH: 48);

                var activity = Ensure(prefab, "activity-panel", column, typeof(CanvasRenderer)); Glass(activity);
                var ag = Swap<VerticalLayoutGroup>(activity); ag.padding = new RectOffset(12, 12, 10, 10); ag.spacing = 6;
                Layout(activity, minH: 200, flexH: 1);
                var activityHeader = Label(prefab, "activity-header", activity, font, 13, "VIEW / ACTIVITIES", dim); Layout((RectTransform)activityHeader.transform, minH: 16, prefH: 16, flexH: 0);

                foreach (var name in new[] { "action-row", "sim-panel", "part-list", "view-controls", "lab-tools", "insets", "journey", "lens-state" })
                {
                    var r = R(name); r.SetParent(activity, false); r.SetAsLastSibling();
                    Layout(r, flexH: 0);
                }
                var toolsRect = R("lab-tools"); var oldTools = toolsRect.GetComponent<HorizontalOrVerticalLayoutGroup>(); if (oldTools) Object.DestroyImmediate(oldTools);
                var grid = toolsRect.GetComponent<GridLayoutGroup>(); if (!grid) grid = toolsRect.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(112, 44); grid.spacing = new Vector2(6, 6); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3; grid.childAlignment = TextAnchor.UpperCenter;
                foreach (var b in toolsRect.GetComponentsInChildren<Button>(true))
                {
                    var le = b.GetComponent<LayoutElement>(); if (le) Object.DestroyImmediate(le);
                    var t = b.GetComponentInChildren<Text>(true); t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = 15; t.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                Layout(R("abilities"), flexH: 0);
                // Rows that were horizontal across the old bottom bar stack vertically in the column.
                foreach (var name in new[] { "action-row", "sim-panel", "part-list", "part-buttons", "journey" }) Swap<VerticalLayoutGroup>(R(name));
                var abg = Swap<HorizontalLayoutGroup>(R("abilities")); abg.padding = new RectOffset(6, 6, 6, 6); abg.spacing = 6; abg.childForceExpandWidth = true; abg.childAlignment = TextAnchor.MiddleCenter;
                foreach (var key in new[] { "ab-SCAN", "ab-INSIDE", "ab-FREEZE", "ab-REPLAY" }) Layout(R(key), minH: 56, prefH: 56, minW: 48, flexW: 1);
                foreach (var key in new[] { "ab-TRACE", "ab-SIMULATE" }) { var r = Find(prefab, key); if (r) r.gameObject.SetActive(false); }
                foreach (var key in new[] { "btn-skip-intro", "btn-free-continue" }) Layout(R(key), minH: 50, prefH: 50);
                foreach (var key in new[] { "sim-brightness-slider", "sim-distance-slider" }) Layout(R(key), minH: 48, prefH: 48, prefW: 80, minW: 70, flexW: 1);
                foreach (var row in new[] { "sim-brightness", "sim-distance", "sim-photo" }) { var g = Swap<HorizontalLayoutGroup>(R(row)); g.spacing = 6; g.childForceExpandWidth = false; g.childAlignment = TextAnchor.MiddleCenter; }
                foreach (var key in new[] { "sim-minus", "sim-plus" }) Layout(R(key), minH: 48, prefH: 48, minW: 48, prefW: 48, flexW: 0);
                foreach (var key in new[] { "sim-dim-preset", "sim-bright-preset", "sim-near", "sim-distant", "sim-light-dim", "sim-light-bright" }) Layout(R(key), minH: 48, prefH: 48, minW: 80, flexW: 1);
                R("sim-title").GetComponent<Text>().fontSize = 16; R("sim-note").GetComponent<Text>().fontSize = 14; R("sim-response").GetComponent<Text>().fontSize = 14;
                var partList = R("part-list"); var plg = partList.GetComponent<VerticalLayoutGroup>(); plg.padding = new RectOffset(10, 10, 8, 8);
                foreach (var b in R("part-buttons").GetComponentsInChildren<Button>(true)) Layout((RectTransform)b.transform, minH: 46, prefH: 46, flexH: 0);
                var partsLabel = R("part-list").Find("Label")?.GetComponent<Text>(); if (partsLabel) { partsLabel.fontSize = 14; Layout((RectTransform)partsLabel.transform, minH: 22, prefH: 22, flexH: 0); }
                var view = R("view-controls"); var vg = Swap<HorizontalLayoutGroup>(view); vg.spacing = 4; vg.padding = new RectOffset(6, 6, 6, 6); vg.childForceExpandWidth = true; vg.childAlignment = TextAnchor.MiddleCenter;
                var viewLabel = view.Find("Label"); if (viewLabel) viewLabel.gameObject.SetActive(false);
                foreach (var key in new[] { "view-out", "view-in", "view-left", "view-right", "view-reset" })
                {
                    var b = R(key); Layout(b, minH: 48, prefH: 48, minW: 48, flexW: 1);
                    var compact = b.GetComponent<CompactButtonLayout>(); if (compact) Object.DestroyImmediate(compact);
                    var caption = b.Find("Text"); if (caption) caption.gameObject.SetActive(false);
                    var icon = (RectTransform)b.Find("Icon"); if (icon) { icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f); icon.anchoredPosition = Vector2.zero; icon.sizeDelta = new Vector2(24, 24); }
                }
                foreach (var chip in R("journey").GetComponentsInChildren<Text>(true)) { chip.fontSize = 14; Layout((RectTransform)chip.transform, minH: 26, prefH: 26, flexH: 0); }
                var insets = R("insets"); var ig = Swap<HorizontalLayoutGroup>(insets); ig.spacing = 6; ig.childForceExpandWidth = true; ig.childAlignment = TextAnchor.UpperCenter;
                foreach (var key in new[] { "inset-retina", "inset-perception" }) { var g = Swap<VerticalLayoutGroup>(R(key)); g.padding = new RectOffset(8, 8, 8, 8); g.spacing = 4; g.childAlignment = TextAnchor.UpperCenter; }
                foreach (var key in new[] { "inset-retina-image", "inset-perception-image" }) Layout(R(key), minH: 110, prefH: 110, prefW: 110, minW: 90);
                foreach (var t in insets.GetComponentsInChildren<Text>(true)) t.fontSize = 13;
                var retinaLabel = R("inset-retina").GetComponentInChildren<Text>(true); retinaLabel.text = "Image on the retina: real and inverted";
                foreach (var key in new[] { "insets", "journey" }) if (!R(key).GetComponent<CanvasGroup>()) R(key).gameObject.AddComponent<CanvasGroup>();
                foreach (var label in R("labels-layer").GetComponentsInChildren<RectTransform>(true).Where(r => r.name.StartsWith("World label") && r.parent == R("labels-layer")))
                    if (!label.GetComponent<CanvasGroup>()) label.gameObject.AddComponent<CanvasGroup>();

                // Panels that overlay the column: the quick check, the end card and the Ask Niko sheet.
                var activityCard = R("activity"); var cardImage = activityCard.GetComponent<Image>(); if (cardImage) { cardImage.color = new Color(0, 0, 0, 0); cardImage.raycastTarget = false; }
                Column(activityCard);
                var card = (RectTransform)activityCard.Find("Card"); card.anchorMin = new Vector2(0, 0); card.anchorMax = new Vector2(1, .56f); card.pivot = new Vector2(.5f, .5f); card.offsetMin = Vector2.zero; card.offsetMax = Vector2.zero;
                var content = card.Find("Content"); var cg = content.GetComponent<VerticalLayoutGroup>(); if (cg) cg.padding = new RectOffset(16, 16, 14, 14);
                R("act-title").GetComponent<Text>().fontSize = 21; R("act-prompt").GetComponent<Text>().fontSize = 18; R("act-tag").GetComponent<Text>().fontSize = 13;
                foreach (var b in R("act-options").GetComponentsInChildren<Button>(true).Concat(R("act-buttons").GetComponentsInChildren<Button>(true))) Layout((RectTransform)b.transform, minH: 48, prefH: 48);
                var slots = Find(prefab, "act-slots"); if (slots) slots.gameObject.SetActive(false);

                var endMenu = R("end-menu"); var endImage = endMenu.GetComponent<Image>(); if (endImage) { endImage.color = new Color(0, 0, 0, 0); endImage.raycastTarget = false; }
                Column(endMenu);
                var endCard = (RectTransform)endMenu.Find("Card"); endCard.anchorMin = new Vector2(0, 0); endCard.anchorMax = new Vector2(1, .6f); endCard.pivot = new Vector2(.5f, .5f); endCard.offsetMin = Vector2.zero; endCard.offsetMax = Vector2.zero;
                foreach (var b in endCard.GetComponentsInChildren<Button>(true)) Layout((RectTransform)b.transform, minH: 48, prefH: 48);
                var endTitle = endCard.Find("end-title")?.GetComponent<Text>(); if (endTitle) { endTitle.fontSize = 20; endTitle.text = Stage.ClosingText; }
                var endRecap = endCard.Find("end-recap")?.GetComponent<Text>(); if (endRecap) endRecap.fontSize = 15;

                var askPanel = R("ask-panel");
                askPanel.anchorMin = new Vector2(1, 0); askPanel.anchorMax = new Vector2(1, .56f); askPanel.pivot = new Vector2(1, 0);
                askPanel.anchoredPosition = new Vector2(-ColumnMargin, ColumnMargin); askPanel.sizeDelta = new Vector2(ColumnWidth, -ColumnMargin);
                var askSuggest = R("ask-suggestions").GetComponent<HorizontalOrVerticalLayoutGroup>(); if (askSuggest) askSuggest.spacing = 6;
                foreach (var b in R("ask-suggestions").GetComponentsInChildren<Button>(true)) Layout((RectTransform)b.transform, minH: 42, prefH: 42);
                var askInput = R("ask-input"); Layout(askInput, minH: 48, prefH: 48, minW: 120, flexW: 1);

                // The top bar and label layer stop where the column begins; the toast stays over the 3D view.
                var topbar = R("topbar"); topbar.anchoredPosition = new Vector2(-(ColumnWidth + 2 * ColumnMargin) / 2 + ColumnMargin / 2, topbar.anchoredPosition.y); topbar.sizeDelta = new Vector2(-(ColumnWidth + 3 * ColumnMargin), topbar.sizeDelta.y);
                var nav = R("Chapter Navigation"); Layout(nav, minH: 112, prefH: 112, prefW: 290, minW: 230, flexW: 0);
                var topButtons = R("top-buttons"); Layout(topButtons, prefW: -1, minW: -1, flexW: 0);
                var tbg = topButtons.GetComponent<HorizontalLayoutGroup>(); if (tbg) { tbg.childForceExpandWidth = false; tbg.spacing = 6; tbg.childAlignment = TextAnchor.MiddleRight; }
                Layout(R("btn-replay-expl"), minH: 56, prefH: 56, prefW: 104, minW: 104, flexW: 0);
                var mainCamera = hud.mainCamera; if (mainCamera && !mainCamera.GetComponent<ViewportShift>()) mainCamera.gameObject.AddComponent<ViewportShift>().columnPixels = ColumnWidth + 2 * ColumnMargin;
                var labels = R("labels-layer"); labels.anchorMin = Vector2.zero; labels.anchorMax = Vector2.one; labels.pivot = new Vector2(.5f, .5f);
                labels.anchoredPosition = new Vector2(-(ColumnWidth + 2 * ColumnMargin) / 2, 0); labels.sizeDelta = new Vector2(-(ColumnWidth + 2 * ColumnMargin), 0);
                var toast = R("toast"); toast.anchorMin = new Vector2(.06f, 1); toast.anchorMax = new Vector2(.58f, 1); toast.pivot = new Vector2(.5f, .5f); toast.anchoredPosition = new Vector2(0, -160); toast.sizeDelta = new Vector2(0, 56);

                // Bottom and middle keep their (now empty) containers so existing bindings and the HUD editor stay valid.
                foreach (var r in prefab.GetComponentsInChildren<RectTransform>(true)) EditorUtility.SetDirty(r);
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        /// Scene instance: bind the new keys and clear stale layout overrides on the panels the prefab re-anchored.
        static void Instance(Hud hud)
        {
            var all = hud.canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var key in new[] { "right-column", "info-panel", "info-text", "niko-status", "niko-portrait", "activity-panel", "activity-header", "info-header" })
                if (!hud.bindings.Any(b => b.key == key)) hud.bindings = hud.bindings.Append(new Hud.Binding { key = key, target = all.First(r => r.name == key) }).ToArray();
            foreach (var key in new[] { "activity", "end-menu", "ask-panel", "topbar", "labels-layer", "toast", "Chapter Navigation", "insets", "journey", "part-list", "view-controls", "sim-panel", "action-row", "subtitle-panel", "prompt-banner" })
                foreach (var r in all.Where(r => r.name == key))
                {
                    PrefabUtility.RevertObjectOverride(r, InteractionMode.AutomatedAction);
                    var le = r.GetComponent<LayoutElement>(); if (le) PrefabUtility.RevertObjectOverride(le, InteractionMode.AutomatedAction);
                    var card = r.Find("Card"); if (card) PrefabUtility.RevertObjectOverride(card, InteractionMode.AutomatedAction);
                }
            EditorUtility.SetDirty(hud);
        }

        // ---------------- Niko portrait: a small camera on his head feeds the information panel ----------------
        static RenderTexture PortraitTexture()
        {
            var path = Builder.Gen + "UI/NikoPortrait.renderTexture";
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing) return existing;
            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32) { name = "NikoPortrait" };
            AssetDatabase.CreateAsset(rt, path);
            return rt;
        }

        static void Portrait(Stage stage, RenderTexture portrait)
        {
            const string layerName = "HE_Niko";
            Builder.EnsureLayer(layerName);
            int layer = LayerMask.NameToLayer(layerName);
            foreach (var t in stage.niko.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            var head = stage.niko.head;
            var existing = head.Find("Niko Portrait Cam");
            var go = existing ? existing.gameObject : new GameObject("Niko Portrait Cam");
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0, .02f, .85f); go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            go.layer = layer;
            var cam = go.GetComponent<Camera>(); if (!cam) cam = go.AddComponent<Camera>();
            cam.cullingMask = 1 << layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 30; cam.nearClipPlane = .05f; cam.farClipPlane = 4; cam.targetTexture = portrait; cam.depth = -6; cam.allowHDR = false;
            var data = go.GetComponent<UniversalAdditionalCameraData>(); if (!data) data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false; data.renderShadows = false; data.antialiasing = AntialiasingMode.None;
            var main = stage.lookCamera.GetComponent<Camera>(); if (main) main.cullingMask |= 1 << layer;
        }

        // ---------------- no picture on the anatomical retina: the popup carries the retinal image ----------------
        static void RetinaImage(Stage stage)
        {
            if (stage.rays.imageQuad) { Object.DestroyImmediate(stage.rays.imageQuad.gameObject); stage.rays.imageQuad = null; }
        }

        // ---------------- eye: smoother silhouettes and more believable surfaces, same lesson geometry ----------------
        static void Eye()
        {
            Material M(string name) => AssetDatabase.LoadAssetAtPath<Material>(Builder.Gen + "Materials/" + name + ".mat");
            void Set(Material m, params (string key, object value)[] values)
            {
                if (!m) return;
                foreach (var (key, value) in values) { if (value is float f) m.SetFloat(key, f); else if (value is Color c) m.SetColor(key, c); }
                EditorUtility.SetDirty(m);
            }
            Set(M("Eye_Sclera"), ("_Wrap", .72f), ("_Gloss", .6f), ("_Specular", .42f), ("_Rim", .16f), ("_RimColor", new Color(1f, .9f, .85f)), ("_DetailStrength", .27f), ("_BaseColor", new Color(.96f, .94f, .9f)));
            Set(M("Eye_Cornea"), ("_Gloss", .985f), ("_Specular", 1f), ("_Rim", .5f), ("_F0", .06f), ("_BaseColor", new Color(.33f, .4f, .45f)));
            Set(M("Eye_Iris"), ("_Gloss", .42f), ("_Specular", .18f), ("_Wrap", .72f));
            Set(M("Eye_Lens"), ("_BaseColor", new Color(.55f, .7f, .8f)), ("_Gloss", .96f), ("_Specular", .75f), ("_Rim", .3f));
            Set(M("Eye_Vitreous"), ("_Gloss", .6f), ("_Rim", .18f));
            Set(M("Eye_Retina"), ("_BaseColor", new Color(.84f, .38f, .2f)), ("_DetailStrength", .6f), ("_Gloss", .4f));
            Set(M("Eye_Choroid"), ("_DetailStrength", .75f));
            var profiles = new Dictionary<string, List<Vector2[]>>
            {
                ["sclera"] = G.ScleraProfile(), ["choroid"] = G.ChoroidProfile(), ["retina"] = G.RetinaProfile(), ["vitreous"] = G.VitreousProfile(),
                ["aqueous"] = G.AqueousProfile(), ["cornea"] = G.CorneaProfile(), ["lens"] = Builder.Shift(G.LensProfile(), -G.LensZ),
            };
            foreach (var (id, profile) in profiles)
            {
                Builder.SaveMesh(Lathe.Revolve(profile, G.KeepFrom, G.KeepTo, 72), id + "_Keep");
                Builder.SaveMesh(Lathe.Revolve(profile, G.CutFrom, G.CutTo, 72), id + "_Cut");
            }
        }

        // ---------------- shots and anchor for the overview and the closing line ----------------
        static void Shots(Stage stage)
        {
            Shot(stage, "S10_Overview", OverviewPos, OverviewLook, OverviewFov);
            Shot(stage, "S11_Closing", ClosingPos, ClosingLook, ClosingFov);
        }

        static void Shot(Stage stage, string name, Vector3 pos, Vector3 look, float fov)
        {
            var shot = stage.cams.shots.FirstOrDefault(s => s.name == name);
            if (!shot)
            {
                var go = new GameObject(name); go.transform.SetParent(GameObject.Find("Shots (cue sheet cameras)").transform, false);
                shot = go.AddComponent<CinemachineCamera>(); shot.Priority = 0;
                stage.cams.shots = stage.cams.shots.Append(shot).ToArray();
            }
            shot.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            var lens = shot.Lens; lens.FieldOfView = fov; lens.NearClipPlane = .03f; lens.FarClipPlane = 140; shot.Lens = lens;
            EditorUtility.SetDirty(shot);
        }

        static void Anchors(Stage stage)
        {
            var anchor = stage.nikoAnchors.FirstOrDefault(a => a.name == "Closing");
            if (!anchor)
            {
                anchor = new GameObject("Closing").transform; anchor.SetParent(stage.nikoAnchors[0].parent, true);
                stage.nikoAnchors = stage.nikoAnchors.Append(anchor).ToArray();
            }
            var face = ClosingPos - ClosingAnchor; face.y = 0;
            anchor.SetPositionAndRotation(ClosingAnchor, Quaternion.LookRotation(face.normalized));
        }

        // ---------------- lesson sound set (Tools/HumanEye/generate_ui_sfx.py) and the closing line ----------------
        static void Sfx(Stage stage)
        {
            var map = new (string key, string file)[]
            {
                ("label", "ui_label"), ("isolate", "ui_isolate"), ("popup", "ui_popup"), ("popdown", "ui_popdown"), ("lightmove", "fx_lightmove"),
                ("cell", "fx_cell"), ("dive", "fx_dive"), ("overview", "fx_overview"), ("complete", "fx_complete"), ("nikoscan", "fx_nikoscan"), ("chapter", "fx_chapter"),
            };
            var effects = stage.sfx.effects.ToList();
            foreach (var (key, file) in map)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + file + ".wav");
                if (!clip) { Debug.LogWarning("WOW sfx missing: " + file); continue; }
                effects.RemoveAll(e => e.key == key);
                effects.Add(new AudioDirector.Sfx { key = key, clip = clip });
            }
            stage.sfx.effects = effects.ToArray();
            stage.closingLine = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "niko_closing.wav");
            if (!stage.closingLine) Debug.LogWarning("WOW closing line missing: run Tools/HumanEye/generate_tts.ps1 -Line");
            EditorUtility.SetDirty(stage.sfx);
        }
    }
}
