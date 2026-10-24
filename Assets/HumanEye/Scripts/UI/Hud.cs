using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Label = UnityEngine.UI.Text;
using VisualElement = UnityEngine.RectTransform;
using TextField = UnityEngine.UI.InputField;

namespace HumanEye
{
    /// Scene-authored Canvas HUD. The right-hand column holds the information panel (chapter, instruction, captions, Niko's
    /// portrait and status, Talk to Niko) above the View / Activities panel (Scan Eye / Explore Inside, experiment controls,
    /// parts, quick checks, the retinal popup, the journey stages). Every unavailable action explains why.
    public class Hud : MonoBehaviour
    {
        [Serializable] public struct Binding { public string key; public RectTransform target; }
        public Canvas canvas;
        public Binding[] bindings;
        Dictionary<string, RectTransform> elements;
        readonly Dictionary<Button, string> partIds = new();
        public RectTransform Element(string key)
        {
            if (elements == null) elements = bindings.ToDictionary(b => b.key, b => b.target);
            return elements.TryGetValue(key, out var value) ? value : null;
        }
        public T Control<T>(string key) where T : Component => Element(key).GetComponentInChildren<T>(true);
        static Text Caption(Component c) => c.GetComponentInChildren<Text>(true);
        void State(Component c, string state, bool enabled) => c.GetComponent<HudState>()?.Set(state, enabled);
        static void Click(Button b, string text, Action action)
        {
            Caption(b).text = text; b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => action()); b.gameObject.SetActive(true);
        }
        static T[] Slots<T>(RectTransform parent) where T : Component =>
            Enumerable.Range(0, parent.childCount).Select(i => parent.GetChild(i).GetComponent<T>()).Where(c => c).ToArray();
        static void HideChildren(RectTransform parent) { foreach (Transform child in parent) child.gameObject.SetActive(false); }
        static void Capacity(int needed, int available, string group)
        { if (needed > available) throw new InvalidOperationException($"Author more {group} slots in the saved Canvas: need {needed}, have {available}."); }
        public Font font;
        public ChapterRunner runner;
        public Stage stage;
        public Narrator narrator;
        public NikoGuide guide;
        public ActivityController activities;
        public CameraDirector cams;
        public Camera mainCamera;

        public WorldLabels labels;
        VisualElement root, fade, progress, partButtons, journey, insets;
        Label toast;
        Label title, prompt, speaker, subtitle, lensState, info, nikoStatus;
        readonly Dictionary<string, float> popTargets = new(), popSpeeds = new();
        readonly Dictionary<string, CanvasGroup> popGroups = new();
        readonly List<string> popScratch = new();
        readonly List<RaycastResult> pointerHits = new();
        PointerEventData pointerEvent;
        EventSystem pointerEventSystem;
        float toastLeft, refreshIn;
        string pulse;
        string modal;
        bool pausedBeforeModal;
        Action confirmAction;
        VisualElement boundRoot;
        Material insetBlur;
        static readonly Color FeedbackInk = new(26 / 255f, 43 / 255f, 73 / 255f);
        static readonly int BlurId = Shader.PropertyToID("_Blur");
        const float RevealSpeed = 1.6f;   // main interface fade after the intro (~0.6 s), shared with the lens-shift ease
        static readonly string[] Abilities = { "INSIDE", "FREEZE", "REPLAY" };
        /// The journey bar's node names, one per chapter: what each step of the investigation is about (UiJourney).
        public static readonly string[] StageNames = { "Outside", "Inside", "Nerve", "Light in", "Focus", "Image", "Cells", "To brain", "Journey", "Wonder" };
        static readonly string[] Topics = LearnerModel.Topics;
        static readonly string[] TopicNames = { "Outer parts of the eye", "Inside the eye", "Optic nerve", "Pupil response", "Accommodation", "Inverted retinal image", "Rods and cones", "The complete pathway" };
        static readonly string[] TopicChapters = { "Scene02", "Scene03", "Scene04", "Scene05", "Scene06", "Scene07", "Scene08", "Scene10" };

        T Q<T>(string name) where T : Component => Control<T>(name);
        void On(string name, Action a)
        {
            var b = Q<Button>(name); b.onClick.AddListener(() => a());
        }
        void Show(string name, bool on)
        {
            var e = Element(name);
            if (!e) return;
            bool opening = on && !e.gameObject.activeSelf;
            e.gameObject.SetActive(on);
            if (opening && e.parent == root && name is not ("labels-layer" or "safe" or "fade")) e.SetAsLastSibling();
        }
        /// Visibility as the learner sees it: an element fading out already counts as hidden, one fading in as shown.
        bool IsShown(string key)
        {
            if (popTargets.TryGetValue(key, out var target)) return target > 0;
            var e = Element(key); return e && e.gameObject.activeSelf;
        }

        void OnEnable()
        {
            root = Element("root");
            foreach (var key in new[] { "activity", "ask-panel", "help-panel", "settings-panel", "review-panel", "pause-overlay", "confirm-panel", "end-menu", "toast", "intro-overlay", "btn-ask" }) Show(key, false);   // nothing is fading yet: instant hides
            fade = Element("fade"); toast = Q<Label>("toast"); progress = Element("progress");
            partButtons = Element("part-buttons"); journey = Element("journey"); insets = Element("insets");
            title = Q<Label>("chapter-title"); prompt = Q<Label>("prompt-banner"); speaker = Q<Label>("speaker");
            subtitle = Q<Label>("subtitle-text"); lensState = Q<Label>("lens-state");
            info = Element("info-text") ? Q<Label>("info-text") : null; nikoStatus = Element("niko-status") ? Q<Label>("niko-status") : null;
            var retina = Q<RawImage>("inset-retina-image");   // its own copy of the blur material (UIBlur.shader): the focus error drives it
            if (!insetBlur && retina && retina.material && retina.material.HasProperty(BlurId)) retina.material = insetBlur = new Material(retina.material);
            if (boundRoot != root)
            {
                labels = new WorldLabels(Element("labels-layer"), () => mainCamera, id => stage.RequestPartFromList(id));
                popGroups.Clear(); Bind(); boundRoot = root;
            }
            narrator.Changed += Refresh; runner.Changed += Refresh; activities.Changed += RefreshActivity; guide.Changed += RefreshAsk;
            HudState.Hovered += stage.sfx.Hover; HudState.Clicked += stage.sfx.Click;
        }

        void OnDisable()
        {
            narrator.Changed -= Refresh; runner.Changed -= Refresh; activities.Changed -= RefreshActivity; guide.Changed -= RefreshAsk;
            HudState.Hovered -= stage.sfx.Hover; HudState.Clicked -= stage.sfx.Click;
        }

        void Bind()
        {
            if (Element("btn-home")) On("btn-home", ReturnToMenu);
            On("btn-ask", () => OpenAsk(!IsShown("ask-panel"))); Caption(Q<Button>("btn-ask")).text = "Talk to Niko";
            On("ask-close", () => OpenAsk(false));
            On("ask-submit", SubmitAsk);
            Q<TextField>("ask-input").onSubmit.AddListener(_ => SubmitAsk());
            On("btn-replay-expl", ReplayExplanation);
            On("btn-pause", () => SetPaused(true));
            On("pause-resume", () => SetPaused(false));
            On("pause-restart", () => { SetPaused(false); runner.RestartChapter(); });
            On("btn-help", () => OpenModal("help-panel", true)); On("help-close", () => OpenModal("help-panel", false));
            On("btn-settings", () => OpenSettings(true)); On("start-settings", () => OpenSettings(true)); On("settings-close", () => OpenSettings(false));
            On("btn-review", () => OpenReview(true)); On("review-close", () => OpenReview(false));
            On("review-restart", () => { OpenReview(false); runner.RestartChapter(); });
            On("btn-read-continue", narrator.Continue);
            On("btn-free-continue", () => { runner.Continue(); stage.sfx.Play("confirm", .6f); });
            if (Element("btn-skip-intro")) On("btn-skip-intro", stage.SkipIntro);
            if (Element("btn-lab")) On("btn-lab", () => stage.navigation.Toggle());
            if (Element("btn-intro-replay")) On("btn-intro-replay", () => runner.Begin(0));
            if (Element("btn-sound")) On("btn-sound", () => { SaveSystem.Data.settings.muted = !SaveSystem.Data.settings.muted; SaveSystem.Save(); Refresh(); });
            if (Element("btn-section")) On("btn-section", stage.ToggleSection);
            if (Element("btn-shell")) On("btn-shell", stage.ToggleShell);
            foreach (var a in Abilities) { var ability = a; On("ab-" + a, () => stage.UseAbility(ability)); }
            On("view-left", () => stage.Orbit(new Vector2(-60, 0))); On("view-right", () => stage.Orbit(new Vector2(60, 0)));
            On("view-in", () => stage.Zoom(-4)); On("view-out", () => stage.Zoom(4)); On("view-reset", stage.ResetView);
            On("sim-minus", () => stage.UseControl("dimmer")); On("sim-plus", () => stage.UseControl("brighter"));
            On("sim-dim-preset", () => stage.UseControl("presetDim")); On("sim-bright-preset", () => stage.UseControl("presetBright"));
            On("sim-near", () => stage.UseControl("near")); On("sim-distant", () => stage.UseControl("distant"));
            On("sim-light-dim", () => stage.UseControl("lightDim")); On("sim-light-bright", () => stage.UseControl("lightBright"));
            Q<Slider>("sim-brightness-slider").onValueChanged.AddListener(value => stage.SetBrightnessFromSlider(value));
            Q<Slider>("sim-distance-slider").onValueChanged.AddListener(value => stage.UseControl("distanceSlider", value));
            On("start-begin", RequestNewSession);
            if (Element("start-help")) On("start-help", () => OpenModal("help-panel", true));
            if (Element("start-chapters")) On("start-chapters", () => OpenChapters(true));
            if (Element("btn-chapters")) On("btn-chapters", () => OpenChapters(true));
            if (Element("chapters-close")) On("chapters-close", () => OpenChapters(false));
            if (Element("chapters-list")) { var slots = Slots<Button>(Element("chapters-list")); for (int i = 0; i < slots.Length; i++) { int k = i; slots[k].onClick.AddListener(() => PlayChapter(k)); } }
            On("start-continue", () => StartModule(Array.FindIndex(runner.chapters, c => c.id == SaveSystem.Data.checkpoint)));
            On("end-replay", () => { Pop("end-menu", false); Fade(0); runner.Begin(0); });
            if (Element("end-explore")) On("end-explore", () => { Pop("end-menu", false); stage.StartFreeExplore(); });
            if (Element("free-journey")) On("free-journey", stage.FreeJourney);
            if (Element("free-inside")) On("free-inside", stage.ToggleFreeInside);
            if (Element("free-exit")) On("free-exit", stage.EndFreeExplore);
            On("end-review", () => OpenReview(true));
            On("end-exit", () => { Toast("You can close this window now."); Application.Quit(); });
            On("pause-menu", ReturnToMenu); On("end-menu-button", ReturnToMenu);
            On("confirm-cancel", () => { confirmAction = null; OpenModal("confirm-panel", false); });
            On("confirm-accept", () => { var action = confirmAction; confirmAction = null; OpenModal("confirm-panel", false, false); action?.Invoke(); });
            BindSettings();
        }

        // ---------- start ----------
        public void ShowStart()
        {
            Pop("toast", false); toastLeft = 0;
            Show("start-screen", true);
            var d = SaveSystem.Data;
            int i = Array.FindIndex(runner.chapters, c => c.id == d.checkpoint);
            bool canContinue = i >= 0 && (i > 0 || d.completedChapters.Count > 0 || d.cues.Count > 0);
            Show("start-continue", canContinue);
            if (canContinue) Caption(Q<Button>("start-continue")).text = "Continue from " + Pretty(runner.chapters[Mathf.Max(0, i)]);
            Q<Label>("start-notice").text = SaveSystem.Notice ?? "";
            Show("start-notice", !string.IsNullOrEmpty(SaveSystem.Notice));
        }

        public void StartModule(int index)
        {
            Show("start-screen", false);
            OpenSettings(false);
            stage.sfx.Play("gatestart", .7f);   // on the interface channel: chapter entry stops the effects channel, not this one
            runner.Begin(Mathf.Max(0, index));   // the click also unlocks audio on web/mobile browsers
        }

        // ---------- chapters ----------
        /// The furthest chapter the learner has reached: the saved checkpoint or the one after the last completed chapter.
        int Reached()
        {
            var d = SaveSystem.Data; int reached = Mathf.Max(0, Array.FindIndex(runner.chapters, c => c.id == d.checkpoint));
            for (int i = 0; i < runner.chapters.Length; i++) if (d.completedChapters.Contains(runner.chapters[i].id)) reached = Mathf.Max(reached, i + 1);
            return Mathf.Min(reached, runner.chapters.Length - 1);
        }

        void OpenChapters(bool open, bool cue = true)
        {
            OpenModal("chapters-panel", open, cue);
            if (!open) return;
            var slots = Slots<Button>(Element("chapters-list")); Capacity(runner.chapters.Length, slots.Length, "chapter list");
            int reached = Reached();
            for (int i = 0; i < slots.Length; i++)
            {
                var b = slots[i]; b.gameObject.SetActive(i < runner.chapters.Length); if (i >= runner.chapters.Length) continue;
                bool done = SaveSystem.Data.completedChapters.Contains(runner.chapters[i].id), current = runner.Started && i == runner.Index;
                Caption(b).text = $"{i + 1}   {runner.chapters[i].title}" + (done ? "   ✓" : i > reached ? "   · locked" : current ? "   · now" : "");
                State(b, "unavailable", i > reached); State(b, "done", done && !current); State(b, "current", current);
            }
        }

        /// Every unavailable action explains itself: a locked chapter says how it opens.
        void PlayChapter(int index)
        {
            if (index > Reached()) { Toast("This chapter opens when you reach it in the investigation."); stage.sfx.Play("deny", .6f); return; }
            OpenChapters(false, false);   // the start press or the chapter's entry sounds instead
            if (runner.Started) { runner.SetPaused(false); runner.Begin(index); } else StartModule(index);
        }

        void RequestNewSession()
        {
            if (SaveSystem.Data.cues.Count == 0 && SaveSystem.Data.completedChapters.Count == 0) { StartModule(0); return; }
            Confirm("Start a new investigation?", "This replaces your saved discoveries and scene checkpoint. Your settings will be kept. Choose Cancel to continue your saved investigation.",
                () => { SaveSystem.ResetProgress(true); StartModule(0); });
        }

        void Confirm(string heading, string message, Action action)
        {
            confirmAction = action;
            Q<Label>("confirm-title").text = heading; Q<Label>("confirm-message").text = message;
            OpenModal("confirm-panel", true);
        }

        void OpenModal(string name, bool open, bool cue = true)   // cue false: the click that closes it makes its own sound
        {
            if (open)
            {
                if (modal == null) pausedBeforeModal = Clock.Paused;
                else if (modal != name) Show(modal, false);
                if (modal != name && cue) stage.sfx.Play("open", .5f);
                modal = name;
                runner.SetPaused(true); Show("pause-overlay", false); Show(name, true);
            }
            else
            {
                Show(name, false);
                if (modal == name) { modal = null; runner.SetPaused(pausedBeforeModal); if (cue) stage.sfx.Play("close", .45f); Refresh(); }   // back to the pause card at once
            }
        }

        public void ReturnToMenu()
        {
            if (modal != null) { Show(modal, false); modal = null; }
            OpenAsk(false, false); Show("pause-overlay", false); Pop("end-menu", false);
            runner.ReturnToMenu(); stage.sfx.Play("menu", .55f); ShowPartList(null); ShowStart(); Refresh();
        }

        string Pretty(ChapterDefinition c) => "Chapter " + (Array.IndexOf(runner.chapters, c) + 1) + " of " + runner.chapters.Length + " · " + c.title;

        // ---------- per-frame ----------
        int laidOutWidth, laidOutHeight;
        /// Responsive column: on narrow screens (4:3 tablets) the right column is 340 px instead of 400, and the caption
        /// area, label layer and camera lens shift follow, so the subject keeps its space. Existing elements only.
        void Responsive()
        {
            if (Screen.width == laidOutWidth && Screen.height == laidOutHeight) return;
            laidOutWidth = Screen.width; laidOutHeight = Screen.height;
            var settings = Element("settings-panel");
            if (settings) ((RectTransform)settings.GetChild(0)).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(660, Screen.height - 48));
            float column = Screen.width < 1200 ? 340 : 400;
            foreach (var key in new[] { "right-column", "activity", "end-menu", "ask-panel" }) { var r = Element(key); if (r) r.sizeDelta = new Vector2(column, r.sizeDelta.y); }
            var bottom = Element("bottom"); if (bottom) bottom.offsetMax = new Vector2(-(column + 72), bottom.offsetMax.y);
            var layer = Element("labels-layer"); if (layer) layer.offsetMax = new Vector2(-(column + 48), layer.offsetMax.y);
            var shift = mainCamera ? mainCamera.GetComponent<ViewportShift>() : null; if (shift) shift.columnPixels = column + 40;
            // the part chips (three a row) and the tools (two a row) fill the column's width
            if (partButtons && partButtons.TryGetComponent<GridLayoutGroup>(out var chips)) chips.cellSize = new Vector2((column - 44) / 3, chips.cellSize.y);
            var tools = Element("lab-tools"); if (tools && tools.TryGetComponent<GridLayoutGroup>(out var grid)) grid.cellSize = new Vector2((column - 38) / 2, grid.cellSize.y);
        }

        void Update()
        {
            Responsive();
            labels.Update();
            if (toastLeft > 0 && (toastLeft -= Time.unscaledDeltaTime) <= 0) Pop("toast", false);
            if ((refreshIn -= Time.unscaledDeltaTime) <= 0) { refreshIn = .2f; Refresh(); }
            ViewportShift.Weight = Mathf.MoveTowards(ViewportShift.Weight, stage.CinematicPlaying ? 0 : 1, Time.unscaledDeltaTime * RevealSpeed);   // the picture re-composes as the column arrives
            CinematicTitle();
            if (insetBlur && IsShown("insets")) insetBlur.SetFloat(BlurId, Mathf.Clamp01(stage.rays.FocusError / .6f));   // soft when the lens shape is wrong
            popScratch.Clear(); popScratch.AddRange(popTargets.Keys);   // reused scratch list: no per-frame allocation
            foreach (var key in popScratch)
            {
                var g = Group(key); float target = popTargets[key];
                if (!g) { popTargets.Remove(key); continue; }
                g.alpha = Mathf.MoveTowards(g.alpha, target, Time.unscaledDeltaTime * (popSpeeds.TryGetValue(key, out var speed) ? speed : 5));
                if (g.alpha <= 0 && target <= 0) { g.gameObject.SetActive(false); popTargets.Remove(key); }
                else if (g.alpha >= 1 && target >= 1) popTargets.Remove(key);   // settled: activeSelf tells the truth again
            }
        }

        /// The cinematic title over the letterboxed picture: widely tracked capitals typed left to right without the line moving
        /// (the untyped rest is drawn transparent), then the subtitle and its rule fade in. Rebuilt only when a letter appears.
        void CinematicTitle()
        {
            var title = Element("intro-title"); if (!title) return;
            bool on = !string.IsNullOrEmpty(stage.CineTitle);
            foreach (var key in new[] { "intro-title", "intro-subtitle", "intro-rule" }) { var e = Element(key); if (e && e.gameObject.activeSelf != on) e.gameObject.SetActive(on); }
            if (!on) { titleShown = -1; titleSource = null; return; }
            if (titleSource != stage.CineTitle) { titleSource = stage.CineTitle; titleTracked = string.Join(" ", stage.CineTitle.ToCharArray()); titleShown = -1; }
            int shown = Mathf.RoundToInt(titleTracked.Length * stage.CineLetters);
            if (shown != titleShown)
            {
                titleShown = shown;
                title.GetComponent<Label>().text = shown >= titleTracked.Length ? titleTracked : titleTracked.Substring(0, shown) + "<color=#00000000>" + titleTracked.Substring(shown) + "</color>";
            }
            var titleLabel = title.GetComponent<Label>(); var titleColor = titleLabel.color;
            titleColor.a = stage.CineTitleAlpha; titleLabel.color = titleColor;
            var sub = Element("intro-subtitle");
            if (sub) { var s = sub.GetComponent<Label>(); if (s.text != stage.CineSubtitle) s.text = stage.CineSubtitle; var c = s.color; c.a = .9f * stage.CineSubtitleAlpha; s.color = c; }
            var rule = Element("intro-rule"); if (rule) rule.localScale = new Vector3(stage.CineSubtitleAlpha, 1, 1);
        }
        string titleSource, titleTracked; int titleShown = -1;

        /// Everything that comes and goes during the lesson (cards, panels, captions, Talk to Niko, the letterbox) fades in
        /// and out instead of snapping; falls back to SetActive without a CanvasGroup. A group on its way out never blocks
        /// what is underneath; one on its way in is clickable at once. The layout collapses in one step when a fade ends.
        void Pop(string key, bool show, float speed = 5)
        {
            var e = Element(key); if (!e) return;
            var g = Group(key);
            if (!g) { Show(key, show); return; }
            if (show) { if (!e.gameObject.activeSelf) g.alpha = 0; Show(key, true); }
            else if (!e.gameObject.activeSelf) { popTargets.Remove(key); return; }   // already gone: nothing to fade
            popTargets[key] = show ? 1 : 0; popSpeeds[key] = speed;
            g.interactable = g.blocksRaycasts = show;
        }

        CanvasGroup Group(string key)
        {
            if (popGroups.TryGetValue(key, out var g)) return g;
            var e = Element(key); g = e ? e.GetComponent<CanvasGroup>() : null;
            popGroups[key] = g; return g;
        }

        /// True while a popped element is fading out (its object is still active, so activeSelf alone would say "shown").
        bool FadingOut(Transform t)
        {
            foreach (var pair in popTargets) if (pair.Value <= 0 && Element(pair.Key) == t) return true;
            return false;
        }

        /// Short information block in the upper panel (instruction, scan feedback, closing line). Null hides it.
        public void Info(string text)
        {
            if (!info) return;
            if (!string.IsNullOrEmpty(text) && info.text != text) stage.sfx.Soft("label", .3f);   // a new instruction, never per character, never over another cue
            info.text = text ?? ""; Pop("info-text", !string.IsNullOrEmpty(text));
        }

        public void Refresh()
        {
            if (root == null) return;
            var c = runner.Chapter;
            var set = SaveSystem.Data.settings;
            title.text = c ? Pretty(c) : "The Human Eye";
            RefreshProgress();
            subtitle.fontSize = new[] { 22, 26, 30, 34 }[Mathf.Clamp(set.subtitleSize, 0, 3)];

            var cue = narrator.Current;
            bool subs = cue != null && (set.subtitles || set.muted || narrator.MissingAudio || narrator.AwaitingContinue);
            Pop("subtitle-panel", subs && !stage.CinematicPlaying && activities.Card == null);   // a cinematic: the letterbox caption; a check: its card
            if (subs)
            {
                speaker.text = cue.IsPrompt ? "PROMPT" : narrator.Source == NarrationSource.Replay ? "NIKO · replay" : "NIKO";
                if (narrator.MissingAudio) speaker.text += " · audio unavailable (provisional text)";
                subtitle.text = narrator.SubtitleText;
            }
            Show("btn-read-continue", narrator.AwaitingContinue);

            bool started = runner.Started, intro = stage.CinematicPlaying, walking = stage.navigation && stage.navigation.Active;
            prompt.text = walking ? "Explore lab · WASD / joystick to walk · Right-drag / touch to look" : Objective();
            var next = Element("next-step");
            if (next) { Pop("next-step", !string.IsNullOrEmpty(prompt.text) && !stage.CinematicPlaying); State(next, "pulse", true); }
            else Pop("prompt-banner", !string.IsNullOrEmpty(prompt.text));
            // Keep Pause and Settings reachable during cinematics; the chapter chip and lesson tools return afterwards.
            bool free = stage.FreeExploring, complete = runner.State == LessonState.Complete;
            Pop("topbar", started, RevealSpeed);
            Pop("right-column", started && !intro && activities.Card == null && (!complete || free), RevealSpeed);   // a check or the finale card takes the place
            Element("chapter-title").parent.gameObject.SetActive(!intro);
            Show("btn-home", false); Show("btn-chapters", !intro);   // Home lives in the pause card (Main menu)
            Show("bottom", started); Show("middle", started);
            Pop("intro-overlay", intro && !Clock.Paused, RevealSpeed);   // the letterbox leaves at the pace the interface arrives
            // The letterbox must sit behind the existing Pause / Settings controls in the safe-area hierarchy.
            var overlay = Element("intro-overlay"); var topbar = Element("topbar");
            if (intro && overlay && topbar && overlay.parent == topbar.parent && overlay.GetSiblingIndex() > topbar.GetSiblingIndex())
                overlay.SetSiblingIndex(topbar.GetSiblingIndex());
            if (intro && Element("intro-caption")) Q<Label>("intro-caption").text = stage.IntroCaption ?? (subs ? narrator.SubtitleText : "");

            foreach (var a in Abilities)
            {
                var b = Q<Button>("ab-" + a);
                bool ok = stage.Ability(a, out var reason);
                Caption(b).text = stage.AbilityLabel(a);
                State(b, "unavailable", !ok);
                State(b, "pulse", ok && pulse == a && runner.Awaiting != null);
                State(b, "selected", ok && a == "FREEZE" && Clock.Frozen);
                b.gameObject.SetActive(ok);
                b.interactable = ok;
            }
            Pop("abilities", started && !walking && Abilities.Any(a => stage.Ability(a, out _)));
            Pop("info-panel", started && info && !string.IsNullOrEmpty(info.text));
            if (nikoStatus) nikoStatus.text = NikoStatus();
            Show("btn-free-continue", !walking && runner.FreeMode != null && activities.Card == null);
            Show("btn-skip-intro", intro && !Clock.Paused && !stage.Short);   // the reduced-motion versions are short stills: nothing to skip
            Caption(Q<Button>("btn-pause")).text = Clock.Paused ? "Resume" : "Pause";
            var replay = Q<Button>("btn-replay-expl");
            State(replay, "unavailable", narrator.LastCore == null || narrator.Busy);
            bool explore = runner.ExploreSet != null || runner.FreeMode != null || runner.State == LessonState.AwaitingAction;
            Pop("view-controls", started && !free && explore && !walking && c != null && c.id is not ("Scene08" or "Scene11"));   // Free Explore: drag, wheel and pinch (and Reset in the pause card's place)
            Pop("part-list", !walking && partIds.Count > 0 && (runner.ExploreSet != null || free && !stage.JourneyPlaying));   // the replay's stage list takes the room
            foreach (var b in Slots<Button>(partButtons).Where(b => b.gameObject.activeSelf))
            {
                if (!partIds.TryGetValue(b, out var id)) continue;   // a button left visible only for the list's fade-out
                bool done = !free && runner.ExploreDone.Contains(id), selected = free ? stage.FreeSelected == id : runner.Focused?.partId == id;
                State(b, "done", done);
                State(b, "selected", selected);
                var entry = runner.Chapter ? Array.Find(runner.Chapter.explore, e => e.partId == id) : null;
                if (entry == null && !free) continue;   // fading out after the chapter changed (e.g. Home during exploration)
                var name = entry != null ? entry.label : Caption(b).text.TrimStart('›', '✓', ' ');
                Caption(b).text = (selected ? "› " : done ? "✓ " : "") + name;
            }
            RefreshSimulation();
            Pop("end-menu", complete && !free);
            if (runner.State == LessonState.Complete && Element("end-recap")) Q<Label>("end-recap").text = Recap();
            if (runner.State == LessonState.Complete && Element("end-title")) Q<Label>("end-title").text = Stage.ClosingText;
            Show("pause-overlay", Clock.Paused && modal == null);
            RefreshActivity();
            foreach (var key in new[] { "btn-replay-expl", "btn-review", "btn-help" }) Show(key, !intro);
            Pop("btn-ask", started && !intro, RevealSpeed);
            Show("btn-sound", started && !intro);
            Show("btn-section", stage.CanInspectLayers); Show("btn-shell", stage.CanInspectLayers);
            if (Element("btn-section")) Caption(Q<Button>("btn-section")).text = stage.eye.IsCut ? "Exterior view" : "Cross-section";
            if (Element("btn-shell")) Caption(Q<Button>("btn-shell")).text = stage.eye.Get("sclera").Visible ? "Hide sclera" : "Show sclera";
            Show("btn-lab", stage.navigation && stage.navigation.Available && !Clock.Paused);
            if (Element("btn-lab")) Caption(Q<Button>("btn-lab")).text = walking ? "Return to lesson" : "Explore lab";
            Show("btn-intro-replay", started && !intro && !Clock.Paused && runner.State == LessonState.Exploring && !free);
            foreach (var key in new[] { "free-journey", "free-inside", "free-exit" }) Show(key, free);
            if (Element("free-inside")) Caption(Q<Button>("free-inside")).text = stage.FreeInside ? "Back outside" : "Look from inside";
            if (Element("btn-sound"))
            {
                Caption(Q<Button>("btn-sound")).text = set.muted ? "Sound off" : "Sound on";
                var icon = Element("btn-sound").GetComponentInChildren<HudIcon>(true);
                var symbol = set.muted ? HudIcon.Symbol.SoundOff : HudIcon.Symbol.Sound;
                if (icon && icon.symbol != symbol) { icon.symbol = symbol; icon.SetVerticesDirty(); }
            }
            var panel = Element("activity-panel");
            bool content = false;
            foreach (Transform child in panel) if (child.gameObject.activeSelf && child.name != "activity-header" && HasVisibleContent(child)) { content = true; break; }
            Pop("activity-panel", started && !intro && content);
        }

        bool HasVisibleContent(Transform t)
        {
            if (t.name.StartsWith("Figma ", StringComparison.Ordinal)) return false;
            if (FadingOut(t)) return false;   // a card on its way out no longer counts, so the panel leaves together with it
            var g = t.GetComponent<Graphic>();   // Unity fake-null: test with the bool operator, never `is` / `?.`
            bool panelArtwork = g is Image image && image.sprite && image.sprite.name.StartsWith("Panel", StringComparison.Ordinal);
            if (g && g.enabled && !(g is GlassPanel) && !panelArtwork) return true;
            if (t.GetComponent<Selectable>()) return true;
            foreach (Transform c in t) if (c.gameObject.activeSelf && HasVisibleContent(c)) return true;
            return false;
        }

        /// The learner's current objective (also what Ask Niko answers to "what now?"), derived from the live lesson state.
        public string Objective()
        {
            if (stage.FreeExploring)
                return stage.JourneyPlaying ? null : stage.FreeInside ? "Drag to look around inside the eye  ·  scroll out to leave"
                                        : "Drag to spin  ·  scroll or pinch to zoom  ·  tap any part";
            var card = activities.Card;
            if (card != null)
                return card.answer != null ? null : card.tapParts ? "Tap the right part on the eye  →  or pick it on the right" : "Choose an answer on the right";
            if (runner.Awaiting == "inside") return "Tap  “" + stage.AbilityLabel("INSIDE") + "”  on the right";
            if (runner.Awaiting == "brighter") return "Make it brighter and watch the pupil";
            if (runner.Awaiting == "dimmer") return "Now make it dimmer and watch the pupil";
            if (runner.Awaiting == "near") return "Tap  “Near object”  on the right";
            if (runner.Awaiting == "distant") return "Now tap  “Distant object”";
            if (runner.ExploreSet != null && runner.State == LessonState.Exploring && runner.Focused == null)
                return $"Tap a glowing part of the eye  ·  {runner.ExploreDone.Count} of {runner.ExploreSet.Length} explored";
            return runner.FreeMode switch
            {
                "brightness" => "Try the brightness, then tap Continue",
                "photoreceptors" => "Tap the rods or cones, try dim and bright light, then tap Continue",
                _ => null
            };
        }

        string Cue(string id) => narrator.database.Get(id)?.text;

        /// Honest companion status: there is no microphone in this build, so Niko is never shown as "listening".
        string NikoStatus()
        {
            if (!runner.Started || stage.CinematicPlaying) return "";
            if (guide.Thinking) return "Niko is thinking…";
            if (narrator.Busy) return narrator.Source == NarrationSource.Answer ? "Niko is answering you" : "Niko is speaking";
            if (IsShown("ask-panel")) return "Type a question for Niko (no microphone on this build)";
            if (runner.Awaiting != null) return "Niko is waiting for you";
            if (activities.Card != null) return "Niko is ready to help with this check";
            return "Niko is ready · Talk to Niko to ask a question";
        }

        /// Conclusion from the learner's actual record: the whole pathway, what they achieved, and what is worth revisiting.
        static string Recap()
        {
            var d = SaveSystem.Data;
            int secure = Topics.Count(t => LearnerModel.Status(d, t, true) == TopicStatus.Secure);
            var revisit = Enumerable.Range(0, Topics.Length).Where(i => LearnerModel.Status(d, Topics[i], true) == TopicStatus.NeedsPractice).Select(i => TopicNames[i]).ToArray();
            var s = "Light from an object → cornea → aqueous humour → pupil → lens → vitreous humour → retina → optic nerve → brain.\n" +
                    $"Discovery stamps: {d.stamps.Count}   ·   Topics secure: {secure} of {Topics.Length}";
            return revisit.Length == 0 ? s : s + "\nWorth another look in Review: " + string.Join(", ", revisit);
        }

        void RefreshProgress()
        {
            var dots = Slots<RectTransform>(progress).Where(d => d.name.StartsWith("progress ")).ToArray();
            Capacity(runner.chapters.Length, dots.Length, "progress");
            for (int i = 0; i < runner.chapters.Length; i++)
            {
                bool done = SaveSystem.Data.completedChapters.Contains(runner.chapters[i].id), current = i == runner.Index;
                State(dots[i], "current", current);
                State(dots[i], "complete", done && !current);
                var name = Caption(dots[i]);
                name.text = i < StageNames.Length ? StageNames[i] : (i + 1).ToString();
                var skin = dots[i].GetComponent<HudState>();
                name.color = skin && skin.normalSprite
                    ? current ? new Color(.4f, .34f, .85f) : done ? new Color(.05f, .42f, .41f) : new Color(.37f, .42f, .51f)
                    : current ? new Color(.15f, .78f, 1f) : done ? new Color(1f, .86f, .55f) : new Color(.66f, .74f, .8f);
            }
            var fill = Element("progress-fill");
            if (fill)
            {
                float t = runner.chapters.Length > 1 ? Mathf.Clamp01(Mathf.Max(0, runner.Index) / (float)(runner.chapters.Length - 1)) : 0;
                fill.anchorMax = new Vector2(t, .5f); fill.offsetMax = new Vector2(12 - 24 * t, 1.5f);
            }
        }

        void RefreshSimulation()
        {
            var sim = stage.Simulation;
            Pop("sim-panel", sim != null && activities.Card == null && !stage.JourneyPlaying);
            if (sim == null) return;
            Show("sim-brightness", sim == "brightness"); Show("sim-distance", sim == "distance"); Show("sim-photo", sim == "photoreceptors");
            Q<Label>("sim-title").text = sim switch { "brightness" => "Experiment · Brightness", "distance" => "Experiment · Object distance", _ => "Experiment · Light level" };
            Caption(Q<Button>("sim-dim-preset")).text = "Dim"; Caption(Q<Button>("sim-bright-preset")).text = "Bright";
            Q<Label>("sim-note").text = stage.Readout();
            Q<Slider>("sim-brightness-slider").SetValueWithoutNotify(stage.Brightness);
            Q<Slider>("sim-distance-slider").SetValueWithoutNotify(Mathf.InverseLerp(stage.rays.nearDistance, stage.rays.farDistance, stage.ObjectDistance));
            foreach (var (name, id) in new[] { ("sim-minus", "dimmer"), ("sim-plus", "brighter"), ("sim-dim-preset", "presetDim"), ("sim-bright-preset", "presetBright"), ("sim-near", "near"), ("sim-distant", "distant"), ("sim-light-dim", "lightDim"), ("sim-light-bright", "lightBright") })
            {
                bool ok = stage.Control(id, out var reason);
                var b = Q<Button>(name); State(b, "unavailable", !ok);
                State(b, "pulse", ok && runner.Awaiting != null && (runner.Awaiting == id || (runner.Awaiting == "brighter" && id == "presetBright") || (runner.Awaiting == "dimmer" && id == "presetDim")));
            }
            Q<Slider>("sim-distance-slider").interactable = stage.Control("distanceSlider", out _);
            var m = stage.micro;
            Q<Label>("sim-response").text = m.LightOn ? $"Rods respond: {Level(m.RodResponse)}   ·   Cones respond: {Level(m.ConeResponse)}" : "Light is off.";
        }

        static string Level(float v) => v > .75f ? "strongly" : v > .35f ? "moderately" : "weakly";

        // ---------- subtitles helpers ----------
        public void ReplayExplanation()
        {
            if (narrator.LastCore == null) { Toast("There's nothing to replay yet."); return; }
            if (narrator.Busy) { Toast("Niko is speaking — Replay explanation is available when he finishes."); return; }
            runner.RunSide(narrator.Play(narrator.LastCore.id, NarrationSource.Replay));
        }

        void SetPaused(bool p) { runner.SetPaused(p); Refresh(); }

        // ---------- parts ----------
        public void ShowPartList(ExploreEntry[] entries)
        {
            partIds.Clear();
            if (entries != null)
            {
                HideChildren(partButtons);   // with no entries the buttons stay in place while the list fades; the next list replaces them
                var buttons = Slots<Button>(partButtons); Capacity(entries.Length, buttons.Length, "parts");
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i]; var button = buttons[i]; partIds[button] = entry.partId;
                    Click(button, entry.label, () => stage.RequestPartFromList(entry.partId));
                }
            }
            Refresh();
        }

        public void Pulse(string ability) { pulse = ability; Refresh(); }

        public void SetLensState(string text)
        {
            lensState.text = text ?? ""; Pop("lens-state", !string.IsNullOrEmpty(text));
        }

        // ---------- insets / journey ----------
        public void ShowInsets(RenderTexture texture, bool perception)
        {
            if (!IsShown("insets")) stage.sfx.Play("popup", .5f);
            Pop("insets", true);
            Q<RawImage>("inset-retina-image").texture = texture;
            Element("inset-retina-image").localEulerAngles = new Vector3(0, 0, 180);
            Q<RawImage>("inset-perception-image").texture = texture;
            Show("inset-perception", perception);
        }

        public void HideInsets()
        {
            if (IsShown("insets") && runner.Started) stage.sfx.Play("popdown", .4f);
            Pop("insets", false); SetLensState(null); ShowJourney(null, -1);
        }

        public void ShowJourney(string[] stages, int index)
        {
            if (!journey) return;
            Pop("journey", stages != null);
            if (stages == null) return;   // the chips stay in place while the card fades; the next set replaces them
            HideChildren(journey);
            var chips = Slots<Label>(journey); Capacity(stages.Length, chips.Length, "journey");
            for (int i = 0; i < stages.Length; i++)
            {
                chips[i].gameObject.SetActive(true);
                chips[i].text = (i < index ? "✓ " : i == index ? "▶ " : "") + stages[i];
                State(chips[i], "selected", i == index);
            }
        }

        // ---------- toast / fade ----------
        public void Toast(string text)
        {
            if (string.IsNullOrEmpty(text) || toast == null) return;
            toast.text = text; Pop("toast", true); Element("toast").SetAsLastSibling(); toastLeft = 3.2f;   // transient feedback always draws above open cards
        }

        public void Fade(float alpha)
        {
            if (fade) fade.GetComponent<CanvasGroup>().alpha = alpha;
        }

        public IEnumerator Fade(float alpha, float seconds)
        {
            float from = fade.GetComponent<CanvasGroup>().alpha;
            yield return Routine.Tween(seconds, t => Fade(Mathf.Lerp(from, alpha, t)));
        }

        // ---------- UI arbitration ----------
        /// True when a pointer position is over interactive UI; world selection then ignores the gesture.
        public bool IsOverUI(Vector2 screen)
        {
            if (modal != null || Clock.Paused) return true;
            var system = EventSystem.current;
            if (!system) return false;
            if (pointerEventSystem != system)
            {
                pointerEventSystem = system;
                pointerEvent = new PointerEventData(system);
            }
            pointerEvent.Reset(); pointerEvent.position = screen;
            pointerHits.Clear();
            system.RaycastAll(pointerEvent, pointerHits);
            foreach (var hit in pointerHits) if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        // ---------- activity card ----------
        ActivityCard shownCard;

        void RefreshActivity()
        {
            var card = activities.Card;
            Pop("activity", card != null); Pop("labels-layer", card == null);
            if (card == shownCard) return;
            shownCard = card; if (card == null) return;
            Q<Label>("act-title").text = card.title.ToUpperInvariant(); Show("act-tag", false);
            Q<Label>("act-prompt").text = card.prompt;
            var feedback = Q<Label>("act-feedback");
            feedback.text = card.feedback ?? ""; feedback.color = FeedbackInk;
            Q<Label>("act-hint").text = card.hint ?? "";
            Show("act-tap-hint", card.tapParts && card.answer == null);
            var options = Element("act-options"); HideChildren(options);
            var optionButtons = Slots<Button>(options); Capacity(card.options.Count, optionButtons.Length, "activity options");
            int n = 0;
            foreach (var (id, label, _) in card.options)
            {
                bool tried = card.wrong.Contains(id), right = card.answer == id;
                var b = optionButtons[n]; char letter = (char)('A' + n++);
                Click(b, (right ? "✓   " : tried ? "✗   " : letter + "   ") + label, () => activities.Click(id));
                State(b, "selected", right); State(b, "unavailable", tried);
                b.interactable = !tried && card.answer == null;
            }
            if (Element("act-slots")) HideChildren(Element("act-slots"));
            var buttons = Element("act-buttons"); HideChildren(buttons);
            var actions = Slots<Button>(buttons); Capacity(card.buttons.Count, actions.Length, "activity actions");
            n = 0; foreach (var name in card.buttons) Click(actions[n++], name, () => activities.Click(name));
        }

        // ---------- Ask Niko ----------
        void OpenAsk(bool open, bool cue = true)
        {
            if (open && !LessonRules.AskNiko(runner.State)) { Toast("Ask Niko opens once the scene has started."); return; }
            if (cue && open != IsShown("ask-panel")) stage.sfx.Play(open ? "nikolisten" : "close", open ? .5f : .4f);
            Pop("ask-panel", open); runner.AskHold = open;
            stage.niko.Express(open ? "Listening" : "Happy", open ? -1 : .1f);   // he listens while the learner types
            if (open) stage.niko.Play("Listen", 1.6f);
            if (!open) { guide.Invalidate(); return; }
            var sugg = Element("ask-suggestions"); HideChildren(sugg);
            var buttons = Slots<Button>(sugg);
            var chapter = runner.Chapter ? runner.Chapter.id : ""; int n = 0;
            foreach (var suggestion in guide.knowledge.suggestions)
            {
                var parts = suggestion.Split('|');
                if (parts.Length == 2 && parts[0] != "*" && parts[0] != chapter) continue;
                var text = parts[parts.Length - 1]; Capacity(n + 1, buttons.Length, "Ask Niko suggestions");
                Click(buttons[n++], text, () => { Q<TextField>("ask-input").text = text; SubmitAsk(); });
            }
            RefreshAsk();
        }

        void SubmitAsk()
        {
            var field = Q<TextField>("ask-input");
            if (string.IsNullOrWhiteSpace(field.text)) { Toast("Type a question or tap a suggestion."); return; }
            stage.niko.Play("Think", NikoGuide.ThinkSeconds + .2f);   // a visible thought before the answer is voiced
            guide.Ask(field.text);
            if (guide.Answer != null && guide.Answer.cues == null) stage.niko.Express("Encouraging", 3);   // text-only answers: a friendly face, no fake lip sync
            // one soft cue per question: no answer for that, a thought before a voiced answer, or a text answer appearing
            bool explore = guide.Answer?.action == "explore";   // Niko takes the learner to the part: its selection cue answers
            if (!explore) stage.sfx.Play(guide.Answer?.unsupported == true ? "nikoerror" : guide.Answer?.cues != null ? "nikothink" : "nikoreply", .45f);
            if (explore) OpenAsk(false, false);
            field.text = "";
        }

        void RefreshAsk()
        {
            if (root == null) return;
            Q<Label>("ask-question").text = guide.Question == null ? "" : "You asked: " + guide.Question;
            Q<Label>("ask-answer").text = guide.Thinking ? "Niko is thinking…" : guide.Answer?.text ?? "";
            Q<Label>("ask-source").text = guide.Answer == null ? "" : guide.Answer.cues != null ? "Niko's own words from the investigation." : guide.Answer.unsupported ? "Outside what this investigation covers." : "Added help (not from the storyboard).";
        }

        // ---------- settings ----------
        void BindSettings()
        {
            var s = SaveSystem.Data.settings;
            void Toggle(string name, Func<bool> get, Action<bool> set) { var t = Q<Toggle>(name); t.onValueChanged.AddListener(value => { set(value); SaveSystem.Save(); Refresh(); }); }
            Toggle("set-subtitles", () => s.subtitles, v => SaveSystem.Data.settings.subtitles = v);
            Toggle("set-reduced", () => s.reducedMotion, v => SaveSystem.Data.settings.reducedMotion = v);
            Toggle("set-skip-camera", () => s.skipCameraMotion, v => SaveSystem.Data.settings.skipCameraMotion = v);
            Toggle("set-wait", () => s.waitForContinue, v => SaveSystem.Data.settings.waitForContinue = v);
            Toggle("set-labels", () => s.labels, v => { SaveSystem.Data.settings.labels = v; if (!v) labels.Clear(); });
            Q<Slider>("set-narration").onValueChanged.AddListener(value => { SaveSystem.Data.settings.narration = value; SaveSystem.Save(); });
            Q<Slider>("set-music").onValueChanged.AddListener(value => { SaveSystem.Data.settings.music = value; SaveSystem.Save(); });
            Q<Slider>("set-sfx").onValueChanged.AddListener(value => { SaveSystem.Data.settings.sfx = value; SaveSystem.Save(); });
            for (int i = 0; i < 4; i++) { int size = i; On("set-size-" + i, () => { SaveSystem.Data.settings.subtitleSize = size; SaveSystem.Save(); OpenSettings(true); }); }
            On("set-q-auto", () => SetQuality(-1)); On("set-q-pc", () => SetQuality(1)); On("set-q-tablet", () => SetQuality(0));
            On("set-reset", () =>
            {
                Confirm("Reset all progress?", "This removes saved discoveries and practice results on this device. Your settings will be kept.", () =>
                {
                    SaveSystem.ResetProgress(true); ReturnToMenu(); Toast("Progress reset. Settings were kept.");
                });
            });
        }

        void SetQuality(int q) { SaveSystem.Data.settings.quality = q; SaveSystem.Save(); ModuleBoot.ApplyQuality(); OpenSettings(true); }

        void OpenSettings(bool open)
        {
            OpenModal("settings-panel", open);
            if (!open) return;
            var s = SaveSystem.Data.settings;
            Q<Toggle>("set-subtitles").SetIsOnWithoutNotify(s.subtitles); Q<Toggle>("set-reduced").SetIsOnWithoutNotify(s.reducedMotion);
            Q<Toggle>("set-skip-camera").SetIsOnWithoutNotify(s.skipCameraMotion); Q<Toggle>("set-wait").SetIsOnWithoutNotify(s.waitForContinue);
            Q<Toggle>("set-labels").SetIsOnWithoutNotify(s.labels);
            Q<Slider>("set-narration").SetValueWithoutNotify(s.narration); Q<Slider>("set-music").SetValueWithoutNotify(s.music); Q<Slider>("set-sfx").SetValueWithoutNotify(s.sfx);
            for (int i = 0; i < 4; i++) State(Q<Button>("set-size-" + i), "selected", s.subtitleSize == i);
            State(Q<Button>("set-q-auto"), "selected", s.quality == -1);
            State(Q<Button>("set-q-pc"), "selected", s.quality == 1);
            State(Q<Button>("set-q-tablet"), "selected", s.quality == 0);
            Q<Label>("set-info").text = "Progress and settings are saved on this device. No account is needed.";
        }

        // ---------- review ----------
        void OpenReview(bool open)
        {
            OpenModal("review-panel", open); if (!open) return;
            var list = Element("review-topics"); var d = SaveSystem.Data;
            Capacity(Topics.Length, list.childCount, "review topics");
            for (int i = 0; i < Topics.Length; i++)
            {
                var row = list.GetChild(i); var topic = Topics[i]; var chapterId = TopicChapters[i];
                bool exposed = d.completedChapters.Contains(chapterId);
                var status = LearnerModel.Status(d, topic, exposed);
                row.GetChild(0).GetComponent<Text>().text = TopicNames[i];
                row.GetChild(1).GetComponent<Text>().text = status switch { TopicStatus.Secure => "✓ Secure", TopicStatus.NeedsPractice => "! Needs practice", TopicStatus.Practising => "~ Practising", TopicStatus.Seen => "• Seen", _ => "○ Not started" };
                int index = Array.FindIndex(runner.chapters, c => c.id == chapterId);
                var replay = row.GetChild(2).GetComponent<Button>();
                Click(replay, "Replay scene", () =>
                {
                    if (!exposed) { Toast("Unlocks after you complete " + Pretty(runner.chapters[index]) + "."); return; }
                    OpenReview(false); Pop("end-menu", false); Fade(0); runner.Begin(index);
                });
                var practice = row.GetChild(3).GetComponent<Button>();
                Click(practice, "Practice (optional)", () =>
                {
                    if (!exposed) { Toast("Practice unlocks after the scene that teaches it."); return; }
                    if (activities.Card != null || (runner.FreeMode == null && runner.State != LessonState.Complete)) { Toast("Practice is available during free exploration or after the investigation."); return; }
                    OpenReview(false); runner.RunSide(activities.Offer("topic:" + topic, runner.chapters[index]));
                });
                State(replay, "unavailable", !exposed); State(practice, "unavailable", !exposed);
            }
            Q<Label>("review-stamps").text = d.stamps.Count == 0 ? "Discovery stamps: none yet." : "Discovery stamps: " + string.Join(", ", d.stamps);
        }
    }
}
