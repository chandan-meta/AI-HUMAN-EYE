using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HumanEye
{
    /// UI Toolkit HUD: subtitles, prompts, progress, abilities, simulation controls, part list, activities,
    /// Ask Niko, settings, review, pause, start and end menus. Every unavailable action explains why.
    public class Hud : MonoBehaviour
    {
        public UIDocument document;
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
        static readonly string[] Abilities = { "SCAN", "INSIDE", "SIMULATE", "TRACE", "FREEZE", "REPLAY" };
        static readonly string[] Topics = LearnerModel.Topics;
        static readonly string[] TopicNames = { "Outer parts of the eye", "Inside the eye", "Optic nerve", "Pupil response", "Accommodation", "Inverted retinal image", "Rods and cones", "The complete pathway" };
        static readonly string[] TopicChapters = { "Scene02", "Scene03", "Scene04", "Scene05", "Scene06", "Scene07", "Scene08", "Scene10" };

        T Q<T>(string name) where T : VisualElement => root.Q<T>(name);
        void On(string name, Action a) { var b = root.Q<Button>(name); if (b != null) b.clicked += a; else Debug.LogError("HUD element missing: " + name); }
        void Show(string name, bool on) { var e = root.Q(name); if (e != null) e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }
        bool Shown(string name) => root.Q(name)?.style.display == DisplayStyle.Flex;

        void OnEnable()
        {
            var docRoot = document.rootVisualElement;
            docRoot.pickingMode = PickingMode.Ignore;
            foreach (var c in docRoot.Children()) c.pickingMode = PickingMode.Ignore;
            root = docRoot.Q("root");
            if (font) root.style.unityFontDefinition = FontDefinition.FromFont(font);
            fade = Q<VisualElement>("fade"); toast = Q<Label>("toast"); progress = Q<VisualElement>("progress");
            partButtons = Q<VisualElement>("part-buttons"); journey = Q<VisualElement>("journey"); insets = Q<VisualElement>("insets");
            title = Q<Label>("chapter-title"); prompt = Q<Label>("prompt-banner"); speaker = Q<Label>("speaker");
            subtitle = Q<Label>("subtitle-text"); lensState = Q<Label>("lens-state");
            labels = new WorldLabels(Q<VisualElement>("labels-layer"), () => mainCamera, id => stage.RequestPartFromList(id));
            Bind();
            narrator.Changed += Refresh; runner.Changed += Refresh; activities.Changed += RefreshActivity; guide.Changed += RefreshAsk;
        }

        void OnDisable()
        {
            narrator.Changed -= Refresh; runner.Changed -= Refresh; activities.Changed -= RefreshActivity; guide.Changed -= RefreshAsk;
        }

        void Bind()
        {
            On("btn-ask", () => OpenAsk(!Shown("ask-panel")));
            On("ask-close", () => OpenAsk(false));
            On("ask-submit", SubmitAsk);
            Q<TextField>("ask-input").RegisterCallback<KeyDownEvent>(e => { if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter) SubmitAsk(); });
            On("btn-replay-expl", ReplayExplanation);
            On("btn-pause", () => SetPaused(true));
            On("pause-resume", () => SetPaused(false));
            On("pause-restart", () => { SetPaused(false); runner.RestartChapter(); });
            On("btn-help", () => Show("help-panel", true)); On("help-close", () => Show("help-panel", false));
            On("btn-settings", () => OpenSettings(true)); On("start-settings", () => OpenSettings(true)); On("settings-close", () => OpenSettings(false));
            On("btn-review", () => OpenReview(true)); On("review-close", () => OpenReview(false));
            On("review-restart", () => { OpenReview(false); runner.RestartChapter(); });
            On("btn-read-continue", narrator.Continue);
            On("btn-free-continue", () => { runner.Continue(); stage.sfx.Play("confirm", .6f); });
            foreach (var a in Abilities) { var ability = a; On("ab-" + a, () => stage.UseAbility(ability)); }
            On("view-left", () => stage.Orbit(new Vector2(-60, 0))); On("view-right", () => stage.Orbit(new Vector2(60, 0)));
            On("view-in", () => stage.Zoom(-4)); On("view-out", () => stage.Zoom(4)); On("view-reset", stage.ResetView);
            On("sim-minus", () => stage.UseControl("dimmer")); On("sim-plus", () => stage.UseControl("brighter"));
            On("sim-dim-preset", () => stage.UseControl("presetDim")); On("sim-bright-preset", () => stage.UseControl("presetBright"));
            On("sim-near", () => stage.UseControl("near")); On("sim-distant", () => stage.UseControl("distant"));
            On("sim-light-dim", () => stage.UseControl("lightDim")); On("sim-light-bright", () => stage.UseControl("lightBright"));
            Q<Slider>("sim-brightness-slider").RegisterValueChangedCallback(e => stage.SetBrightnessFromSlider(e.newValue));
            Q<Slider>("sim-distance-slider").RegisterValueChangedCallback(e => stage.UseControl("distanceSlider", e.newValue));
            On("start-begin", () => StartModule(0));
            On("start-continue", () => StartModule(Array.FindIndex(runner.chapters, c => c.id == SaveSystem.Data.checkpoint)));
            On("end-replay", () => { Show("end-menu", false); Fade(0); runner.Begin(0); });
            On("end-review", () => OpenReview(true));
            On("end-exit", () => { Toast("You can close this window now."); Application.Quit(); });
            BindSettings();
        }

        // ---------- start ----------
        public void ShowStart()
        {
            Show("start-screen", true);
            var d = SaveSystem.Data;
            int i = Array.FindIndex(runner.chapters, c => c.id == d.checkpoint);
            bool canContinue = i > 0 || d.completedChapters.Count > 0;
            Show("start-continue", canContinue);
            if (canContinue) Q<Button>("start-continue").text = "Continue from " + Pretty(runner.chapters[Mathf.Max(0, i)]);
            Q<Label>("start-notice").text = SaveSystem.Notice ?? "";
        }

        public void StartModule(int index)
        {
            Show("start-screen", false);
            OpenSettings(false);
            runner.Begin(Mathf.Max(0, index));   // the click also unlocks audio on web/mobile browsers
        }

        static string Pretty(ChapterDefinition c) => "Scene " + int.Parse(c.id.Substring(5)) + " · " + c.title;

        // ---------- per-frame ----------
        void Update()
        {
            labels.Update();
            if (toastLeft > 0 && (toastLeft -= Time.unscaledDeltaTime) <= 0) toast.RemoveFromClassList("show");
            if ((refreshIn -= Time.unscaledDeltaTime) <= 0) { refreshIn = .2f; Refresh(); }
            bool portrait = Screen.height > Screen.width;
            root.EnableInClassList("portrait", portrait);
            var safe = Screen.safeArea; var panel = root.panel;
            if (panel != null)
            {
                var min = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
                var max = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, Screen.height - safe.yMin));
                var size = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));
                var s = Q<VisualElement>("safe").style;
                s.paddingLeft = 14 + min.x; s.paddingTop = 14 + min.y; s.paddingRight = 14 + size.x - max.x; s.paddingBottom = 14 + size.y - max.y;
            }
        }

        public void Refresh()
        {
            if (root == null) return;
            var c = runner.Chapter;
            var set = SaveSystem.Data.settings;
            title.text = c ? Pretty(c) : "Observatory of Vision";
            RefreshProgress();
            for (int i = 0; i < 4; i++) root.EnableInClassList("subs-" + i, set.subtitleSize == i);

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

            prompt.text = PromptText();
            Show("prompt-banner", !string.IsNullOrEmpty(prompt.text));
            bool started = runner.Started;
            Show("topbar", started); Show("bottom", started); Show("middle", started);

            foreach (var a in Abilities)
            {
                var b = Q<Button>("ab-" + a);
                bool ok = stage.Ability(a, out var reason);
                b.text = stage.AbilityLabel(a);
                b.EnableInClassList("unavailable", !ok);
                b.EnableInClassList("pulse", ok && pulse == a && runner.Awaiting != null);
                b.tooltip = ok ? "" : reason;
            }
            Show("abilities", started);
            Show("btn-free-continue", runner.FreeMode != null && activities.Card == null);
            Q<Button>("btn-pause").text = Clock.Paused ? "Resume" : "Pause";
            var replay = Q<Button>("btn-replay-expl");
            replay.EnableInClassList("unavailable", narrator.LastCore == null || narrator.Busy);
            bool explore = runner.ExploreSet != null || runner.FreeMode != null || runner.State == LessonState.AwaitingAction;
            Show("view-controls", started && explore && c != null && c.id is not ("Scene08" or "Scene11"));
            Show("part-list", partButtons.childCount > 0 && runner.ExploreSet != null);
            foreach (var b in partButtons.Children().OfType<Button>())
                b.EnableInClassList("done", runner.ExploreDone.Contains((string)b.userData));
            RefreshSimulation();
            Show("end-menu", runner.State == LessonState.Complete);
            Show("pause-overlay", Clock.Paused);
            RefreshActivity();
        }

        string PromptText()
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
                "distance" => "Try different distances and watch the lens. Tap Continue when ready.",
                "photoreceptors" => "Tap the rods or cones to inspect them, or compare dim and bright light.",
                "inspectImage" => "Inspect the image on the retina. FREEZE or REPLAY it, then tap Continue.",
                "routeTrace" => "Use TRACE or REPLAY to follow the route again, then tap Continue.",
                "signals" => "TRACE the signals or REPLAY them, then tap Continue.",
                "summary" => "Open Review to see your discoveries, then tap Continue.",
                _ => null
            };
        }

        string Cue(string id) => narrator.database.Get(id)?.text;

        void RefreshProgress()
        {
            if (progress.childCount != runner.chapters.Length)
            {
                progress.Clear();
                foreach (var ch in runner.chapters) progress.Add(new Label { text = int.Parse(ch.id.Substring(5)).ToString() });
            }
            for (int i = 0; i < runner.chapters.Length; i++)
            {
                var dot = (Label)progress[i];
                bool done = SaveSystem.Data.completedChapters.Contains(runner.chapters[i].id);
                dot.ClearClassList(); dot.AddToClassList("dot");
                dot.EnableInClassList("current", i == runner.Index);
                dot.EnableInClassList("complete", done && i != runner.Index);
                dot.text = (i + 2) + (done && i != runner.Index ? "✓" : "");
            }
        }

        void RefreshSimulation()
        {
            var sim = stage.Simulation;
            Show("sim-panel", sim != null && activities.Card == null);
            if (sim == null) return;
            Show("sim-brightness", sim == "brightness"); Show("sim-distance", sim == "distance"); Show("sim-photo", sim == "photoreceptors");
            Q<Label>("sim-title").text = sim switch { "brightness" => "SIMULATE · Brightness", "distance" => "SIMULATE · Object distance", _ => "SIMULATE · Light level" };
            Q<Label>("sim-note").text = sim == "distance" ? "Illustrative model — not a clinical optical simulator." : sim == "brightness" ? $"Brightness: {Mathf.RoundToInt(stage.Brightness * 100)}%   Pupil: {(stage.eye.PupilRadius > .2f ? "larger" : stage.eye.PupilRadius < .12f ? "smaller" : "medium")}" : "";
            Q<Slider>("sim-brightness-slider").SetValueWithoutNotify(stage.Brightness);
            Q<Slider>("sim-distance-slider").SetValueWithoutNotify(Mathf.InverseLerp(stage.rays.nearDistance, stage.rays.farDistance, stage.ObjectDistance));
            foreach (var (name, id) in new[] { ("sim-minus", "dimmer"), ("sim-plus", "brighter"), ("sim-dim-preset", "presetDim"), ("sim-bright-preset", "presetBright"), ("sim-near", "near"), ("sim-distant", "distant"), ("sim-light-dim", "lightDim"), ("sim-light-bright", "lightBright") })
            {
                bool ok = stage.Control(id, out var reason);
                var b = Q<Button>(name); b.EnableInClassList("unavailable", !ok); b.tooltip = reason ?? "";
                b.EnableInClassList("pulse", ok && runner.Awaiting != null && (runner.Awaiting == id || (runner.Awaiting == "brighter" && id == "presetBright") || (runner.Awaiting == "dimmer" && id == "presetDim")));
            }
            Q<Slider>("sim-distance-slider").SetEnabled(stage.Control("distanceSlider", out _));
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
            partButtons.Clear();
            if (entries == null) { Refresh(); return; }
            foreach (var e in entries)
            {
                var id = e.partId;
                var b = new Button(() => stage.RequestPartFromList(id)) { text = e.label, userData = id };
                b.AddToClassList("btn");
                partButtons.Add(b);
            }
            Refresh();
        }

        public void Pulse(string ability) { pulse = ability; Refresh(); }

        public void SetLensState(string text) { lensState.text = text ?? ""; lensState.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }

        // ---------- insets / journey ----------
        public void ShowInsets(RenderTexture texture, bool perception)
        {
            insets.style.display = DisplayStyle.Flex;
            var retina = Q<VisualElement>("inset-retina-image");
            retina.style.backgroundImage = Background.FromRenderTexture(texture);
            retina.style.rotate = new Rotate(180);   // real, inverted
            var seen = Q<VisualElement>("inset-perception-image");
            seen.style.backgroundImage = Background.FromRenderTexture(texture);
            Show("inset-perception", perception);
        }

        public void HideInsets() { if (insets != null) insets.style.display = DisplayStyle.None; SetLensState(null); ShowJourney(null, -1); }

        public void ShowJourney(string[] stages, int index)
        {
            if (journey == null) return;
            journey.Clear();
            journey.style.display = stages == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (stages == null) return;
            for (int i = 0; i < stages.Length; i++)
            {
                var chip = new Label { text = (i < index ? "✓ " : i == index ? "▶ " : "") + stages[i] };
                chip.AddToClassList("stage-chip");
                chip.EnableInClassList("lit", i == index); chip.EnableInClassList("past", i < index);
                journey.Add(chip);
                if (i < stages.Length - 1) journey.Add(new Label { text = "→" });
            }
        }

        // ---------- toast / fade ----------
        public void Toast(string text)
        {
            if (string.IsNullOrEmpty(text) || toast == null) return;
            toast.text = text; toast.AddToClassList("show"); toastLeft = 3.2f;
        }

        public void Fade(float alpha) { if (fade != null) fade.style.opacity = alpha; }

        public IEnumerator Fade(float alpha, float seconds)
        {
            float from = fade.resolvedStyle.opacity;
            yield return Routine.Tween(seconds, t => fade.style.opacity = Mathf.Lerp(from, alpha, t));
        }

        // ---------- UI arbitration ----------
        /// True when a pointer position is over interactive UI; world selection then ignores the gesture.
        public bool IsOverUI(Vector2 screen)
        {
            var panel = root?.panel;
            if (panel == null) return false;
            var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y)));
            return picked != null && picked.pickingMode == PickingMode.Position;
        }

        // ---------- activity card ----------
        ActivityCard shownCard;

        void RefreshActivity()
        {
            var card = activities.Card;
            Show("activity", card != null);
            Show("labels-layer", card == null);   // world labels would show through the translucent card
            if (card == shownCard) return;   // rebuild only on change so a periodic refresh never swallows a tap
            shownCard = card;
            if (card == null) return;
            Q<Label>("act-title").text = card.title; Q<Label>("act-tag").text = card.tag;
            Q<Label>("act-prompt").text = card.prompt;
            Q<Label>("act-feedback").text = card.feedback ?? ""; Q<Label>("act-hint").text = card.hint ?? "";
            Show("act-tap-hint", card.tapParts);
            var options = Q<VisualElement>("act-options"); options.Clear();
            foreach (var (id, label, selected) in card.options)
            {
                var b = new Button(() => activities.Click(id)) { text = selected ? "✓ " + label : label };
                b.AddToClassList("btn"); b.EnableInClassList("selected", selected); b.SetEnabled(!selected);
                options.Add(b);
            }
            var slots = Q<VisualElement>("act-slots"); slots.Clear();
            for (int i = 0; i < card.slots.Count; i++) { var l = new Label { text = (i + 1) + ". " + card.slots[i] }; l.AddToClassList("stage-chip"); slots.Add(l); }
            var buttons = Q<VisualElement>("act-buttons"); buttons.Clear();
            foreach (var name in card.buttons)
            {
                var n = name;
                var b = new Button(() => activities.Click(n)) { text = n };
                b.AddToClassList("btn"); if (n is "Try it" or "Next" or "Check" or "Continue") b.AddToClassList("btn-accent");
                buttons.Add(b);
            }
        }

        // ---------- Ask Niko ----------
        void OpenAsk(bool open)
        {
            if (open && !LessonRules.AskNiko(runner.State)) { Toast("Ask Niko opens once the scene has started."); return; }
            Show("ask-panel", open);
            runner.AskHold = open;   // the chapter pauses at the next safe boundary, then resumes from the next step
            if (!open) return;
            var sugg = Q<VisualElement>("ask-suggestions"); sugg.Clear();
            var chapter = runner.Chapter ? runner.Chapter.id : "";
            foreach (var s in guide.knowledge.suggestions)
            {
                var parts = s.Split('|');
                if (parts.Length == 2 && parts[0] != "*" && parts[0] != chapter) continue;
                var text = parts[parts.Length - 1];
                var b = new Button(() => { Q<TextField>("ask-input").value = text; SubmitAsk(); }) { text = text };
                b.AddToClassList("btn"); b.AddToClassList("small");
                sugg.Add(b);
            }
            RefreshAsk();
        }

        void SubmitAsk()
        {
            var field = Q<TextField>("ask-input");
            if (string.IsNullOrWhiteSpace(field.value)) { Toast("Type a question or tap a suggestion."); return; }
            guide.Ask(field.value);
            field.value = "";
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
            void Toggle(string name, Func<bool> get, Action<bool> set) { var t = Q<Toggle>(name); t.RegisterValueChangedCallback(e => { set(e.newValue); SaveSystem.Save(); Refresh(); }); }
            Toggle("set-subtitles", () => s.subtitles, v => SaveSystem.Data.settings.subtitles = v);
            Toggle("set-reduced", () => s.reducedMotion, v => SaveSystem.Data.settings.reducedMotion = v);
            Toggle("set-skip-camera", () => s.skipCameraMotion, v => SaveSystem.Data.settings.skipCameraMotion = v);
            Toggle("set-wait", () => s.waitForContinue, v => SaveSystem.Data.settings.waitForContinue = v);
            Toggle("set-labels", () => s.labels, v => { SaveSystem.Data.settings.labels = v; if (!v) labels.Clear(); });
            Q<Slider>("set-narration").RegisterValueChangedCallback(e => { SaveSystem.Data.settings.narration = e.newValue; SaveSystem.Save(); });
            Q<Slider>("set-music").RegisterValueChangedCallback(e => { SaveSystem.Data.settings.music = e.newValue; SaveSystem.Save(); });
            Q<Slider>("set-sfx").RegisterValueChangedCallback(e => { SaveSystem.Data.settings.sfx = e.newValue; SaveSystem.Save(); });
            for (int i = 0; i < 4; i++) { int size = i; On("set-size-" + i, () => { SaveSystem.Data.settings.subtitleSize = size; SaveSystem.Save(); OpenSettings(true); }); }
            On("set-q-auto", () => SetQuality(-1)); On("set-q-pc", () => SetQuality(1)); On("set-q-tablet", () => SetQuality(0));
            On("set-reset", () =>
            {
                SaveSystem.ResetProgress(true);
                Toast("Progress reset. Settings were kept.");
                OpenSettings(false);
                if (runner.Started) runner.Begin(0); else ShowStart();
            });
        }

        void SetQuality(int q) { SaveSystem.Data.settings.quality = q; SaveSystem.Save(); ModuleBoot.ApplyQuality(); OpenSettings(true); }

        void OpenSettings(bool open)
        {
            Show("settings-panel", open);
            if (!open) return;
            var s = SaveSystem.Data.settings;
            Q<Toggle>("set-subtitles").SetValueWithoutNotify(s.subtitles); Q<Toggle>("set-reduced").SetValueWithoutNotify(s.reducedMotion);
            Q<Toggle>("set-skip-camera").SetValueWithoutNotify(s.skipCameraMotion); Q<Toggle>("set-wait").SetValueWithoutNotify(s.waitForContinue);
            Q<Toggle>("set-labels").SetValueWithoutNotify(s.labels);
            Q<Slider>("set-narration").SetValueWithoutNotify(s.narration); Q<Slider>("set-music").SetValueWithoutNotify(s.music); Q<Slider>("set-sfx").SetValueWithoutNotify(s.sfx);
            for (int i = 0; i < 4; i++) Q<Button>("set-size-" + i).EnableInClassList("selected", s.subtitleSize == i);
            Q<Button>("set-q-auto").EnableInClassList("selected", s.quality == -1);
            Q<Button>("set-q-pc").EnableInClassList("selected", s.quality == 1);
            Q<Button>("set-q-tablet").EnableInClassList("selected", s.quality == 0);
            Q<Label>("set-info").text = $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]} · target {Application.targetFrameRate} FPS (a target, not a measured guarantee). Progress is saved anonymously on this device only.";
        }

        // ---------- review ----------
        void OpenReview(bool open)
        {
            Show("review-panel", open);
            if (!open) return;
            var list = Q<VisualElement>("review-topics"); list.Clear();
            var d = SaveSystem.Data;
            for (int i = 0; i < Topics.Length; i++)
            {
                var topic = Topics[i]; var chapterId = TopicChapters[i];
                bool exposed = d.completedChapters.Contains(chapterId);
                var status = LearnerModel.Status(d, topic, exposed);
                var row = new VisualElement(); row.AddToClassList("topic-row");
                var name = new Label { text = TopicNames[i] }; name.AddToClassList("topic-name");
                var st = new Label { text = status switch { TopicStatus.Secure => "✓ Secure", TopicStatus.NeedsPractice => "! Needs practice", TopicStatus.Practising => "~ Practising", TopicStatus.Seen => "• Seen", _ => "○ Not started" } };
                st.AddToClassList("topic-status");
                row.Add(name); row.Add(st);
                int index = Array.FindIndex(runner.chapters, c => c.id == chapterId);
                var replay = new Button(() =>
                {
                    if (!exposed) { Toast("Unlocks after you complete " + Pretty(runner.chapters[index]) + "."); return; }
                    OpenReview(false); Show("end-menu", false); Fade(0); runner.Begin(index);
                }) { text = "Replay scene" };
                replay.AddToClassList("btn"); replay.AddToClassList("small"); replay.EnableInClassList("unavailable", !exposed);
                var practice = new Button(() =>
                {
                    if (!exposed) { Toast("Practice unlocks after the scene that teaches it."); return; }
                    if (activities.Card != null || (runner.FreeMode == null && runner.State != LessonState.Complete)) { Toast("Practice is available during free exploration or after the investigation."); return; }
                    OpenReview(false); runner.RunSide(activities.Offer("topic:" + topic, runner.chapters[index]));
                }) { text = "Practice (optional)" };
                practice.AddToClassList("btn"); practice.AddToClassList("small"); practice.EnableInClassList("unavailable", !exposed);
                row.Add(replay); row.Add(practice);
                list.Add(row);
            }
            Q<Label>("review-stamps").text = d.stamps.Count == 0 ? "Discovery stamps: none yet." : "Discovery stamps: " + string.Join(", ", d.stamps);
        }
    }
}
