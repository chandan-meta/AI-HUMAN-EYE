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
    /// Scene-authored Canvas HUD: subtitles, prompts, progress, abilities, simulation controls, part list, activities,
    /// Ask Niko, settings, review, pause, start and end menus. Every unavailable action explains why.
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
        Label title, prompt, speaker, subtitle, lensState;
        float toastLeft, refreshIn;
        string pulse;
        string modal;
        bool pausedBeforeModal;
        Action confirmAction;
        VisualElement boundRoot;
        static readonly string[] Abilities = { "SCAN", "INSIDE", "SIMULATE", "TRACE", "FREEZE", "REPLAY" };
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
        bool Shown(string name) => Element(name).gameObject.activeSelf;

        void OnEnable()
        {
            root = Element("root");
            foreach (var key in new[] { "activity", "ask-panel", "help-panel", "settings-panel", "review-panel", "pause-overlay", "confirm-panel", "end-menu", "toast" }) Show(key, false);
            fade = Element("fade"); toast = Q<Label>("toast"); progress = Element("progress");
            partButtons = Element("part-buttons"); journey = Element("journey"); insets = Element("insets");
            title = Q<Label>("chapter-title"); prompt = Q<Label>("prompt-banner"); speaker = Q<Label>("speaker");
            subtitle = Q<Label>("subtitle-text"); lensState = Q<Label>("lens-state");
            if (boundRoot != root)
            {
                labels = new WorldLabels(Element("labels-layer"), () => mainCamera, id => stage.RequestPartFromList(id));
                Bind(); boundRoot = root;
            }
            narrator.Changed += Refresh; runner.Changed += Refresh; activities.Changed += RefreshActivity; guide.Changed += RefreshAsk;
        }

        void OnDisable()
        {
            narrator.Changed -= Refresh; runner.Changed -= Refresh; activities.Changed -= RefreshActivity; guide.Changed -= RefreshAsk;
        }

        void Bind()
        {
            if (Element("btn-home")) On("btn-home", ReturnToMenu);
            On("btn-ask", () => OpenAsk(!Shown("ask-panel")));
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
            On("start-continue", () => StartModule(Array.FindIndex(runner.chapters, c => c.id == SaveSystem.Data.checkpoint)));
            On("end-replay", () => { Show("end-menu", false); Fade(0); runner.Begin(0); });
            On("end-review", () => OpenReview(true));
            On("end-exit", () => { Toast("You can close this window now."); Application.Quit(); });
            On("pause-menu", ReturnToMenu); On("end-menu-button", ReturnToMenu);
            On("confirm-cancel", () => { confirmAction = null; OpenModal("confirm-panel", false); });
            On("confirm-accept", () => { var action = confirmAction; confirmAction = null; OpenModal("confirm-panel", false); action?.Invoke(); });
            BindSettings();
        }

        // ---------- start ----------
        public void ShowStart()
        {
            Show("toast", false); toastLeft = 0;
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
            runner.Begin(Mathf.Max(0, index));   // the click also unlocks audio on web/mobile browsers
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

        void OpenModal(string name, bool open)
        {
            if (open)
            {
                if (modal == null) pausedBeforeModal = Clock.Paused;
                else if (modal != name) Show(modal, false);
                modal = name;
                runner.SetPaused(true); Show("pause-overlay", false); Show(name, true);
            }
            else
            {
                Show(name, false);
                if (modal == name) { modal = null; runner.SetPaused(pausedBeforeModal); }
            }
        }

        public void ReturnToMenu()
        {
            if (modal != null) { Show(modal, false); modal = null; }
            OpenAsk(false); Show("pause-overlay", false); Show("end-menu", false);
            runner.ReturnToMenu(); ShowPartList(null); ShowStart(); Refresh();
        }

        static string Pretty(ChapterDefinition c) => "Scene " + int.Parse(c.id.Substring(5)) + " · " + c.title;

        // ---------- per-frame ----------
        void Update()
        {
            labels.Update();
            if (toastLeft > 0 && (toastLeft -= Time.unscaledDeltaTime) <= 0) Show("toast", false);
            if ((refreshIn -= Time.unscaledDeltaTime) <= 0) { refreshIn = .2f; Refresh(); }
        }

        public void Refresh()
        {
            if (root == null) return;
            var c = runner.Chapter;
            var set = SaveSystem.Data.settings;
            title.text = c ? Pretty(c) : "Observatory of Vision";
            RefreshProgress();
            subtitle.fontSize = new[] { 20, 24, 28, 32 }[Mathf.Clamp(set.subtitleSize, 0, 3)];

            var cue = narrator.Current;
            bool subs = cue != null && (set.subtitles || narrator.MissingAudio || narrator.AwaitingContinue);
            Show("subtitle-panel", subs);
            if (subs)
            {
                speaker.text = cue.IsPrompt ? "PROMPT" : narrator.Source == NarrationSource.Core ? "NIKO" : "NIKO · replay";
                if (narrator.MissingAudio) speaker.text += " · audio unavailable (provisional text)";
                subtitle.text = narrator.SubtitleText;
            }
            Show("btn-read-continue", narrator.AwaitingContinue);

            prompt.text = Objective();
            Show("prompt-banner", !string.IsNullOrEmpty(prompt.text));
            bool started = runner.Started;
            Show("topbar", started); Show("bottom", started); Show("middle", started);

            foreach (var a in Abilities)
            {
                var b = Q<Button>("ab-" + a);
                bool ok = stage.Ability(a, out var reason);
                Caption(b).text = stage.AbilityLabel(a);
                State(b, "unavailable", !ok);
                State(b, "pulse", ok && pulse == a && runner.Awaiting != null);
                State(b, "selected", ok && (a == "FREEZE" && Clock.Frozen || a == "SIMULATE" && stage.Simulation != null || a == "SCAN" && stage.LabelsOn));
            }
            Show("abilities", started);
            Show("btn-free-continue", runner.FreeMode != null && activities.Card == null);
            Show("btn-skip-intro", stage.IntroPlaying && !Clock.Paused && !stage.Short);   // the reduced-motion intro is a 1.5 s still: nothing to skip
            Caption(Q<Button>("btn-pause")).text = Clock.Paused ? "Resume" : "Pause";
            var replay = Q<Button>("btn-replay-expl");
            State(replay, "unavailable", narrator.LastCore == null || narrator.Busy);
            bool explore = runner.ExploreSet != null || runner.FreeMode != null || runner.State == LessonState.AwaitingAction;
            Show("view-controls", started && explore && c != null && c.id is not ("Scene08" or "Scene11"));
            Show("part-list", partIds.Count > 0 && runner.ExploreSet != null);
            foreach (var b in Slots<Button>(partButtons).Where(b => b.gameObject.activeSelf))
            {
                var id = partIds[b];
                bool done = runner.ExploreDone.Contains(id), selected = runner.Focused?.partId == id;
                State(b, "done", done);
                State(b, "selected", selected);
                var entry = Array.Find(runner.Chapter.explore, e => e.partId == id);
                Caption(b).text = (selected ? "› " : done ? "✓ " : "") + entry.label;
            }
            RefreshSimulation();
            Show("end-menu", runner.State == LessonState.Complete);
            if (runner.State == LessonState.Complete && Element("end-recap")) Q<Label>("end-recap").text = Recap();
            Show("pause-overlay", Clock.Paused && modal == null);
            RefreshActivity();
        }

        /// The learner's current objective (also what Ask Niko answers to "what now?"), derived from the live lesson state.
        public string Objective()
        {
            if (runner.Awaiting == "scan") return Cue("S02_L05");
            if (runner.Awaiting == "inside") return Cue("S02_L16");
            if (runner.Awaiting == "brighter") return Cue("S05_L05");
            if (runner.Awaiting == "dimmer") return "Now decrease the brightness and watch the pupil.";
            if (runner.Awaiting == "near") return "Tap “Near object” to bring an object close.";
            if (runner.Awaiting == "distant") return "Now tap “Distant object”.";
            if (runner.ExploreSet != null && runner.State == LessonState.Exploring && runner.Focused == null)
                return Cue(runner.Chapter.id == "Scene02" ? "S02_L10" : "S03_L01") + $"  ({runner.ExploreDone.Count} of {runner.ExploreSet.Length} explored)";
            return runner.FreeMode switch
            {
                "brightness" => "Experiment freely with the brightness, then tap Continue.",
                "focus" => stage.FocusPrompt,
                "photoreceptors" => "Tap the rods or cones to inspect them, or compare dim and bright light.",
                "inspectImage" => "Inspect the image on the retina. FREEZE or REPLAY it, then tap Continue.",
                "routeTrace" => "Use TRACE or REPLAY to follow the route again, then tap Continue.",
                "signals" => "TRACE the signals or REPLAY them, then tap Continue.",
                "summary" => "Open Review to see your discoveries, then tap Continue.",
                _ => null
            };
        }

        string Cue(string id) => narrator.database.Get(id)?.text;

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
            var dots = Slots<RectTransform>(progress);
            Capacity(runner.chapters.Length, dots.Length, "progress");
            for (int i = 0; i < runner.chapters.Length; i++)
            {
                bool done = SaveSystem.Data.completedChapters.Contains(runner.chapters[i].id);
                State(dots[i], "current", i == runner.Index);
                State(dots[i], "complete", done && i != runner.Index);
                Caption(dots[i]).text = (i + 2) + (done && i != runner.Index ? "✓" : "");
            }
        }

        void RefreshSimulation()
        {
            var sim = stage.Simulation;
            Show("sim-panel", sim != null && activities.Card == null);
            if (sim == null) return;
            bool lens = sim == "lens";   // focus challenge: the brightness row becomes the lens-shape control
            Show("sim-brightness", sim is "brightness" or "lens"); Show("sim-distance", sim is "distance" or "lens"); Show("sim-photo", sim == "photoreceptors");
            Q<Label>("sim-title").text = sim switch { "brightness" => "SIMULATE · Brightness", "distance" => "SIMULATE · Object distance", "lens" => "SIMULATE · Shape the lens (illustrative model)", _ => "SIMULATE · Light level" };
            Caption(Q<Button>("sim-dim-preset")).text = lens ? "Flatter" : "Dim"; Caption(Q<Button>("sim-bright-preset")).text = lens ? "Rounder" : "Bright";
            Q<Label>("sim-note").text = stage.Readout();
            Q<Slider>("sim-brightness-slider").SetValueWithoutNotify(lens ? stage.eye.accommodationTarget : stage.Brightness);
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
            HideChildren(partButtons); partIds.Clear();
            if (entries != null)
            {
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
            lensState.text = text ?? ""; Show("lens-state", !string.IsNullOrEmpty(text));
        }

        // ---------- insets / journey ----------
        public void ShowInsets(RenderTexture texture, bool perception)
        {
            Show("insets", true);
            Q<RawImage>("inset-retina-image").texture = texture;
            Element("inset-retina-image").localEulerAngles = new Vector3(0, 0, 180);
            Q<RawImage>("inset-perception-image").texture = texture;
            Show("inset-perception", perception);
        }

        public void HideInsets()
        {
            Show("insets", false); SetLensState(null); ShowJourney(null, -1);
        }

        public void ShowJourney(string[] stages, int index)
        {
            if (!journey) return;
            HideChildren(journey); Show("journey", stages != null);
            if (stages == null) return;
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
            toast.text = text; Show("toast", true); Element("toast").SetAsLastSibling(); toastLeft = 3.2f;   // transient feedback always draws above open cards
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
            if (!EventSystem.current) return false;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, results);
            return results.Any(r => r.module is GraphicRaycaster);
        }

        // ---------- activity card ----------
        ActivityCard shownCard;

        void RefreshActivity()
        {
            var card = activities.Card;
            Show("activity", card != null); Show("labels-layer", card == null);
            if (card == shownCard) return;
            shownCard = card; if (card == null) return;
            Q<Label>("act-title").text = card.title; Q<Label>("act-tag").text = "Optional practice · You can skip any question";
            Q<Label>("act-prompt").text = card.prompt;
            Q<Label>("act-feedback").text = card.feedback ?? ""; Q<Label>("act-hint").text = card.hint ?? "";
            Show("act-tap-hint", card.tapParts);
            var options = Element("act-options"); HideChildren(options);
            var optionButtons = Slots<Button>(options); Capacity(card.options.Count, optionButtons.Length, "activity options");
            int n = 0;
            foreach (var (id, label, selected) in card.options)
            {
                var b = optionButtons[n++]; Click(b, selected ? "✓ " + label : label, () => activities.Click(id));
                State(b, "selected", selected); b.interactable = !selected;
            }
            var slots = Element("act-slots"); HideChildren(slots);
            var texts = Slots<Label>(slots); Capacity(card.slots.Count, texts.Length, "activity answer");
            for (int i = 0; i < card.slots.Count; i++) { texts[i].gameObject.SetActive(true); texts[i].text = (i + 1) + ". " + card.slots[i]; }
            var buttons = Element("act-buttons"); HideChildren(buttons);
            var actions = Slots<Button>(buttons); Capacity(card.buttons.Count, actions.Length, "activity actions");
            n = 0; foreach (var name in card.buttons) Click(actions[n++], name, () => activities.Click(name));
        }

        // ---------- Ask Niko ----------
        void OpenAsk(bool open)
        {
            if (open && !LessonRules.AskNiko(runner.State)) { Toast("Ask Niko opens once the scene has started."); return; }
            Show("ask-panel", open); runner.AskHold = open;
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
            guide.Ask(field.text);
            if (guide.Answer?.action == "explore") OpenAsk(false);
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
                    OpenReview(false); Show("end-menu", false); Fade(0); runner.Begin(index);
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
