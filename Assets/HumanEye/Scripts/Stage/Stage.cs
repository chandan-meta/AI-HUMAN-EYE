using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanEye
{
    /// The observatory's world actions. Chapter steps may only call actions registered here (a whitelist), and
    /// Enter() restores each chapter's documented checkpoint so restart/resume is deterministic.
    public partial class Stage : MonoBehaviour
    {
        [Header("Systems")]
        public ChapterRunner runner;
        public CameraDirector cams;
        public Narrator narrator;
        public AudioDirector sfx;
        public Hud hud;
        public ActivityController activities;
        public NikoGuide guide;

        [Header("Eye")]
        public EyeModel eye;
        public EyeModel otherEye;
        public Transform eyeRig, heroPoint, cutPoint, menuPoint;
        Vector3 authoredNikoScale;

        [Header("Cast")]
        public HumanDisplay human;
        public NikoRig niko;
        public Transform[] nikoAnchors;
        public Transform lookCamera;

        [Header("Brain and signals")]
        public Transform brainRoot;
        public AnatomyPart brain, visualArea;
        public SignalPulses signals;
        public PhotoreceptorField micro;

        [Header("Optics")]
        public LightRays rays;
        public Transform appleRoot, treeRoot, lampRoot, pedestal;
        public Light keyLight, displayLight;
        public Volume post;
        public GameObject observatory;
        public Renderer[] beamRenderers;

        [Header("Intro (cinematic power-up)")]
        public Volume introVolume;          // ColorAdjustments post-exposure; weight 0 outside the intro
        public Light[] roomLights;          // the room's realtime lights, ramped up during the intro
        public LabEntrance entrance;
        public LabNavigation navigation;
        [Tooltip("Lab brightness: multiplies the room's authored lights (set by the look pass)."), Range(.5f, 3)] public float roomBoost = 1;
        float[] roomBase;

        public string Simulation { get; private set; }  // which experiment controls the panel shows
        public float Brightness { get; private set; } = .5f;
        public float ObjectDistance { get; private set; } = 7;

        readonly Dictionary<string, Func<string, float, IEnumerator>> actions = new();
        Quaternion orbit = Quaternion.identity;
        float keyIntensity, displayIntensity, ambientBase = 1;
        string currentHint;
        Routine demo;
        string demoShot;
        float orbitYaw, orbitPitch;
        ParticleSystem[] ambientParticles;
        public bool DemoBusy => demo != null && demo.Running;

        public IEnumerable<string> ActionNames => actions.Keys;
        public bool Short => SaveSystem.Data.settings.reducedMotion || SaveSystem.Data.settings.skipCameraMotion;

        void Awake()
        {
            authoredNikoScale = niko.transform.localScale;
            if (RenderSettings.skybox && Camera.main) Camera.main.clearFlags = CameraClearFlags.Skybox;   // the Lighting window's skybox always shows through the windows, whatever the saved camera clears to
            keyIntensity = keyLight.intensity; displayIntensity = displayLight.intensity; ambientBase = RenderSettings.ambientIntensity;
            appleBaseScale = appleRoot.localScale.x;
            roomBase = roomLights.Select(l => l.intensity * roomBoost).ToArray();
            CaptureLighting();   // Stage.Lighting.cs: authored levels of the cinematic lights and the ambient colour
            ambientParticles = observatory.GetComponentsInChildren<ParticleSystem>(true);
            EnsureActions();
        }

        /// Builds the action whitelist (also used by the editor validator outside Play mode).
        public void EnsureActions()
        {
            if (actions.Count > 0) return;
            RegisterCommon(); RegisterScene02(); RegisterScene03(); RegisterOptics(); RegisterSignals(); RegisterEnding();
        }

        void OnEnable() { narrator.Completed += LineDone; }
        void OnDisable() { narrator.Completed -= LineDone; }
        /// After every line Niko looks back at the learner, so he is never left staring at a part.
        void LineDone(DialogueDatabase.Cue cue, NarrationSource source, string how) { niko.lookTarget = lookCamera; }

        public IEnumerator Run(string action, string arg, float value)
        {
            if (!actions.TryGetValue(action, out var f)) { Debug.LogError("Stage action not whitelisted: " + action); return null; }
            return f(arg, value);
        }

        public bool Has(string action) => actions.ContainsKey(action);

        void RegisterCommon()
        {
            actions["niko"] = (gesture, seconds) => { niko.Play(gesture, seconds > 0 ? seconds : 1.6f); return null; };
            actions["nikoMove"] = (anchor, seconds) => niko.MoveTo(Anchor(anchor), seconds > 0 ? seconds : 1.4f, Short);
            actions["nikoLook"] = (target, _) => { niko.lookTarget = target == "camera" ? lookCamera : Find(target); niko.gestureTarget = niko.lookTarget; return null; };
            actions["sfx"] = (key, volume) => { sfx.Play(key, volume > 0 ? volume : 1); return null; };
            actions["music"] = (key, _) => { bed = key; return null; };   // the chapter's music bed from here on (Stage.Audio)
            actions["wait"] = (_, seconds) => Waiting(seconds);
            actions["glow"] = (ids, value) => { foreach (var id in ids.Split(',')) Part(id)?.SetGlow(value); return null; };
            actions["isolate"] = (id, _) => { Isolate(id); return null; };
            actions["restore"] = (_, __) => { RestoreParts(); return null; };
            actions["sequence"] = (ids, seconds) => HighlightSequence(ids.Split(','), seconds > 0 ? seconds : .6f);
            actions["labels"] = (set, _) => { ShowLabels(set); return null; };
            actions["turntable"] = (on, _) => { spin = on == "on"; return null; };
            actions["replayCue"] = (cue, _) => narrator.Play(cue, NarrationSource.Replay);   // worked demonstration: hear the source line again
        }

        IEnumerator Waiting(float seconds) { yield return new Wait(seconds); }

        NikoFloatingCompanion companion;
        public NikoFloatingCompanion Companion => companion ? companion : companion = hud.GetComponent<NikoFloatingCompanion>();

        /// Point at a structure: in the room at the structure itself; floating beside the lens he faces the learner with the
        /// structure behind him, so he glances at it and points toward the middle of the frame, where it is.
        void NikoPoint(Transform target, float seconds = 2.4f)
        {
            niko.gestureTarget = niko.lookTarget = target;
            niko.Play(Companion && Companion.Floating ? Companion.OnRight ? "PointLeft" : "PointRight" : "Point", seconds);
        }

        public Transform Anchor(string name) => Array.Find(nikoAnchors, a => a.name == name) ?? nikoAnchors[0];

        Transform Find(string id)
        {
            var p = Part(id);
            if (p) return eye.FocusOf(p);
            return id switch { "eye" => eye.transform, "human" => human.transform, "brain" => brainRoot, "apple" => appleRoot, "tree" => treeRoot, "micro" => micro.transform, _ => eye.transform };
        }

        public AnatomyPart Part(string id)
        {
            if (EyeBrainPlaying && human.pathway != null && human.pathway.Length == 6)   // Scene 9's cinematic shows the head's pathway
            {
                if (id == "optic_nerve") return human.pathway[0];
                if (id == "visual_area") return human.pathway[5];
            }
            var p = eye.Get(id);
            if (p) return p;
            if (id == "brain") return brain;
            if (id == "visual_area") return visualArea;
            if (id == "rods") return micro.rods;
            if (id == "cones") return micro.cones;
            return null;
        }

        IEnumerable<AnatomyPart> AllParts() => eye.parts.Concat(new[] { brain, visualArea, micro.rods, micro.cones });

        // ---------- checkpoints ----------
        /// Restores the documented start state of a chapter: materials, pupil, lens, object distance, cast, cameras, UI.
        public void Enter(ChapterDefinition c)
        {
            if (navigation) navigation.Leave();
            demo = null; orbitYaw = orbitPitch = 0;
            brightnessLive = false;
            guide.Invalidate();
            signals.StopAll();
            ShowHint(null, 0);
            Clock.Frozen = false;
            spin = false; orbit = Quaternion.identity;
            hud.labels.Clear();
            Simulation = null;
            // The supplied exterior is static; use the animated teaching geometry for the pupil response.
            foreach (var p in eye.parts) p.UseImportedExterior = c.id != "Scene05";
            foreach (var p in AllParts()) { p.ResetVisual(); p.Selectable = false; }
            eye.SetPupil(EyeGeometry.PupilDefault, true);
            eye.SetAccommodation(.5f, true);
            eye.SetNerveReveal(1);
            otherEye.gameObject.SetActive(false);
            human.ResetVisual(false);
            niko.gameObject.SetActive(!NikoWaiting); niko.transform.localScale = authoredNikoScale; niko.lookTarget = lookCamera; niko.gestureTarget = eye.transform;
            micro.gameObject.SetActive(false);
            brainRoot.gameObject.SetActive(false);
            appleRoot.gameObject.SetActive(false); treeRoot.gameObject.SetActive(false); pedestal.gameObject.SetActive(false);
            rays.reach = 0; rays.showImage = false; rays.showLamp = false; rays.gameObject.SetActive(false);
            Brightness = .5f; rays.brightness = .5f;
            keyLight.intensity = keyIntensity; displayLight.intensity = displayIntensity;
            RestoreLighting();   // the cinematic lights, the isolate emphasis and the ambient colour back to authored
            IntroReset();
            Exhibit(c.id == "Scene02" ? 1 : 0);   // the lab's exhibit eye stands in the room only before the opening hands over
            RenderSettings.ambientIntensity = ambientBase;   // the authored lab level (look pass), restored at every checkpoint
            observatory.SetActive(true);
            hud.Fade(0);
            hud.HideInsets(); hud.Info(null);
            bool marked = sfx.Recently("quizcomplete", 1.5f) || sfx.Recently("unlock", 1.5f);   // a check's fanfare or the unlock already marks the change
            if (c.id != "Scene02" && runner.Started && !marked) sfx.Play("chapter", .5f);
            bed = BedFor(c.id);
            switch (c.id)
            {
                case "Scene02": EnterScene02(); break;
                case "Scene03": PlaceEye(heroPoint); eye.SetCutInstant(false); eye.SetNerveReveal(.12f); niko.Place(Anchor("EyeLeft")); cams.Snap("S02_EyeHero"); break;
                default: PlaceEye(cutPoint); eye.SetCutInstant(true); EnterLater(c.id); break;
            }
        }

        public void ShowMenu()
        {
            NikoWait();   // the start screen is without Niko: his next line brings him in
            Enter(runner.chapters[2]);
            bed = "start";   // so the frame between BEGIN and the first chapter's entry keeps the start theme
            eye.SetCutInstant(false); eye.SetNerveReveal(.12f); PlaceEye(heroPoint);
            if (menuPoint) PlaceEye(menuPoint);
            brainRoot.gameObject.SetActive(false);
            human.gameObject.SetActive(false); appleRoot.gameObject.SetActive(false); lampRoot.gameObject.SetActive(false);
            pedestal.gameObject.SetActive(false); rays.gameObject.SetActive(false); signals.StopAll();
            cams.Snap(cams.Has("Menu") ? "Menu" : "S04_Wide");
            if (entrance)
            {
                entrance.SetGate(0); entrance.SetAccents(0);
                eyeRig.gameObject.SetActive(false);
                Room(.12f); cams.Snap("S02_IntroThreshold");
            }
            Exhibit(1);
            hud.HideInsets(); hud.labels.Clear();
        }

        void RunDemo(IEnumerator routine)
        {
            if (DemoBusy) return;
            demoShot = cams.Active ? cams.Active.name : null;
            demo = runner.RunSide(routine);
        }

        void StopDemo()
        {
            if (!DemoBusy) return;
            demo.Cancel(); demo = null; signals.StopAll();
            if (demoShot != null) cams.Snap(demoShot);
        }

        void PlaceEye(Transform point)
        {
            eyeRig.SetPositionAndRotation(point.position, point.rotation);
            eyeRig.localScale = point.localScale;
            eye.transform.localPosition = Vector3.zero; eye.transform.localRotation = Quaternion.identity;
            eye.transform.localScale = Vector3.one;
        }

        // ---------- exploration ----------
        bool spin;
        float swayTime;

        public void OnExplore(bool on)
        {
            var set = runner.Chapter.explore.Select(e => e.partId).ToArray();
            foreach (var p in AllParts()) p.Selectable = on && set.Contains(p.id);
            hud.ShowPartList(on ? runner.Chapter.explore : null);
            ShowLabels(on ? "explore" : null);
            if (on) foreach (var p in set) Part(p)?.SetGlow(runner.ExploreDone.Contains(p) ? 0 : .22f);
            if (on && runner.ExploreDone.Count == 0) hud.Toast("Inspect mode · Drag to rotate · Scroll or pinch to zoom");   // the interaction mode changed with the camera
            if (on && spin)
            {
                // the learner never chases a moving target: the turntable stops and the eye eases back to face them
                spin = false; var from = eye.transform.localRotation;
                runner.RunSide(Routine.Tween(Short ? .2f : .7f, t => eye.transform.localRotation = Quaternion.Slerp(from, Quaternion.identity, Mathf.SmoothStep(0, 1, t))));
            }
        }

        public IEnumerator FocusPart(string id)
        {
            orbitYaw = orbitPitch = 0;
            foreach (var p in AllParts()) p.Selectable = false;
            hud.labels.Clear();
            sfx.Play("select");
            spin = false;
            yield return Routine.Tween(Short ? .2f : .5f, t => eye.transform.localRotation = Quaternion.Slerp(eye.transform.localRotation, Quaternion.identity, t));
            orbit = Quaternion.identity; cams.ResetZoom();
            Isolate(id);
            var shot = (runner.Chapter.id == "Scene02" ? "S02_Part_" : "S03_Part_") + id;
            if (cams.Has(shot)) runner.RunSide(cams.MoveTo(shot, 1.1f));   // the label and the line start while the camera settles
            yield return new Wait(Short ? .1f : .35f);
            ShowLabels("focus:" + id);
        }

        public void Discovered(string chapterId, ExploreEntry e)
        {
            if (SaveSystem.AddOnce(SaveSystem.Data.explored, chapterId + ":" + e.partId))
            {
                SaveSystem.AddOnce(SaveSystem.Data.stamps, e.label);
                hud.Toast("Discovery stamp: " + e.label + " ✓");
                sfx.Play("discovery", .8f);
            }
        }

        public IEnumerator ReturnFromPart(string id)
        {
            hud.labels.Clear();
            RestoreParts();
            yield return cams.MoveTo(runner.Chapter.id == "Scene02" ? "S02_EyeHero" : "S03_InsideHero", 1.0f);
            OnExplore(true);
        }

        void Isolate(string id)
        {
            sfx.Play(sfx.Clip("hl_" + id) ? "hl_" + id : "isolate", .45f);   // cornea, iris and retina have their own highlight
            Emphasis(true);   // hero key up a quarter, hall to 85 % (Stage.Lighting.cs)
            // A clear medium on the cut eye (aqueous, lens, vitreous) only reads inside its section: it lifts to a tinted
            // 35 % with a stronger glow and the section around it stays at 60 %, instead of a ghost around an invisible target.
            var target = eye.Get(id);
            bool clear = eye.IsCut && target && target.baseAlpha < 1;
            foreach (var p in eye.parts)
            {
                bool me = p.id == id;
                float a = me ? (clear ? .35f / p.baseAlpha : 1) : p.baseAlpha < 1 ? .35f : clear ? .6f : .18f;
                if (id == "iris" && p.id == "pupil") a = 1;
                if (id == "pupil" && p.id != "cornea") a = 1;   // an opening only reads as dark if the iris and everything behind it stay solid
                if (!eye.IsCut && p.id is "aqueous" or "lens" or "vitreous" or "retina" or "choroid") a = 1;   // closed eye: the interior is only seen through the pupil, where it must stay dark
                p.SetAlpha(a);
                p.SetGlow(me ? (clear ? .85f : .55f) : 0);
            }
        }

        void RestoreParts() { Emphasis(false); foreach (var p in AllParts()) { p.SetAlpha(1); p.SetGlow(0); } }

        IEnumerator HighlightSequence(string[] ids, float each)
        {
            foreach (var id in ids)
            {
                var p = Part(id); if (!p) continue;
                p.SetGlow(.9f); sfx.Play("tick", .5f);
                yield return new Wait(each);
                p.SetGlow(.15f);
            }
            yield return new Wait(each);
            foreach (var id in ids) Part(id)?.SetGlow(0);
        }

        // ---------- input routed from PointerSelector / HUD ----------
        public void OnPartTapped(AnatomyPart part)
        {
            if (activities.PartTapped(part.id)) return;
            if (runner.State == LessonState.Exploring && runner.ExploreSet != null)
            {
                if (!runner.RequestPart(part.id, out var reason)) hud.Toast(reason);
                return;
            }
            InspectTapped(part);
        }

        public void RequestPartFromList(string id)
        {
            if (activities.PartTapped(id)) return;
            if (!runner.RequestPart(id, out var reason)) hud.Toast(reason);
        }

        public void Orbit(Vector2 delta)
        {
            if (runner.Chapter == null || Clock.Paused || !LessonRules.Orbit(runner.State) || runner.Focused != null) return;
            if (runner.Chapter.id is "Scene02" or "Scene03")
            {
                spin = false;
                orbitYaw = Mathf.Clamp(orbitYaw - delta.x * .25f, -40, 40);
                orbitPitch = Mathf.Clamp(orbitPitch + delta.y * .2f, -25, 25);
                eye.transform.localRotation = Quaternion.AngleAxis(orbitYaw, eyeRig.InverseTransformDirection(Vector3.up)) *
                    Quaternion.AngleAxis(orbitPitch, eyeRig.InverseTransformDirection(cams.brain.transform.right));
            }
        }

        public void Zoom(float delta) { if (!Clock.Paused && LessonRules.Orbit(runner.State) && runner.Focused == null) cams.Zoom(delta); }

        public void ResetView()
        {
            if (Clock.Paused) return;
            if (navigation && navigation.Active) { navigation.Leave(); return; }
            spin = false; orbitYaw = orbitPitch = 0;
            if (runner.Chapter?.id is "Scene02" or "Scene03")
            {
                eye.SetCutInstant(runner.Chapter.id == "Scene03");
                RestoreParts();
                if (runner.ExploreSet != null) ShowLabels("explore");
            }
            cams.ResetZoom();
            runner.RunSide(Routine.Tween(.6f, t => eye.transform.localRotation = Quaternion.Slerp(eye.transform.localRotation, Quaternion.identity, t)));
        }

        public bool CanInspectLayers => !Clock.Paused && runner.State == LessonState.Exploring && runner.Focused == null &&
            runner.Chapter != null && (runner.Chapter.id is "Scene02" or "Scene03") && !(navigation && navigation.Active);
        public void ToggleSection()
        {
            if (!CanInspectLayers) return;
            eye.SetCutInstant(!eye.IsCut); ShowLabels("explore"); hud.Refresh();
        }
        public void ToggleShell()
        {
            if (!CanInspectLayers) return;
            var shell = eye.Get("sclera"); shell.SetAlpha(shell.Visible ? 0 : 1, true); hud.Refresh();
        }

        void Update()
        {
            if (NikoWaiting && narrator.Speaking) NikoArrive();   // a lesson line or an answer: either is his first
            AudioFrame();
            if (spin) { swayTime += Clock.VizDelta; eye.transform.localRotation = Quaternion.AngleAxis(Mathf.Sin(swayTime * .35f) * (Short ? 8 : 28), eyeRig.InverseTransformDirection(Vector3.up)); }
            foreach (var particles in ambientParticles)
            {
                if (Short) { if (!particles.isStopped) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
                else if (Clock.Paused || Clock.Frozen) { if (particles.isPlaying) particles.Pause(); }
                else if (particles.isPaused) particles.Play();
            }
        }

        // ---------- labels ----------
        /// Label sets: "explore" (tappable part labels), "focus:<id>[,<id>…]" (the structures being explained), "scan" (every visible
        /// structure), "journey" (the stages of the whole process), "" or null (none). Only lesson-driven; no manual toggle.
        public void ShowLabels(string set)
        {
            hud.labels.Clear();
            if (string.IsNullOrEmpty(set) || !SaveSystem.Data.settings.labels) return;
            if (set == "explore")
            {
                foreach (var e in runner.Chapter.explore)
                {
                    var p = Part(e.partId);
                    if (p) hud.labels.Add(e.label + (runner.ExploreDone.Contains(e.partId) ? " ✓" : ""), eye.LabelOf(p), eye.FocusOf(p), e.partId);
                }
                return;
            }
            sfx.Soft("label", .5f);
            if (set.StartsWith("focus:"))   // one structure, or a few named together ("focus:optic_nerve,brain")
            {
                foreach (var id in set.Substring(6).Split(',')) { var p = Part(id); if (p) hud.labels.Add(p.label, eye.LabelOf(p), eye.FocusOf(p), null); }
                return;
            }
            var structures = set == "journey" ? JourneyStructures() : VisibleStructures();
            foreach (var p in structures) hud.labels.Add(p.label, eye.LabelOf(p), eye.FocusOf(p), null);
            foreach (var extra in ExtraLabels(set)) hud.labels.Add(extra.text, extra.anchor, extra.target, null);
        }

        /// From above the eye is small, so the overview labels the eye once (at its lens), the nerve and the brain; the chips list every stage.
        IEnumerable<AnatomyPart> JourneyStructures() =>
            new[] { "lens", "optic_nerve" }.Select(eye.Get).Where(p => p && p.Visible).Append(brain);

        IEnumerable<AnatomyPart> VisibleStructures()
        {
            string c = runner.Chapter.id;
            if (c == "Scene08") return new[] { micro.rods, micro.cones };
            var ids = c == "Scene02" ? new[] { "sclera", "cornea", "iris", "pupil" }
                    : new[] { "cornea", "aqueous", "iris", "pupil", "lens", "vitreous", "retina", "choroid", "sclera", "optic_nerve" };
            var list = ids.Select(eye.Get).Where(p => p && p.Visible).ToList();
            if (brainRoot.gameObject.activeInHierarchy) { list.Add(brain); list.Add(visualArea); }
            return list;
        }

        // ---------- hints (SupplementalAdaptive) ----------
        public void ShowHint(QuestionBank.Item item, int tier)
        {
            if (currentHint != null) foreach (var p in AllParts()) p.SetGlow(0);
            currentHint = item?.id;
            if (item == null || tier <= 0) return;
            var targets = item.options.Where(o => o.correct && !string.IsNullOrEmpty(o.partId)).Select(o => Part(o.partId)).Where(p => p).ToList();
            if (tier == 1) foreach (var p in targets) p.SetGlow(.35f);
            if (tier >= 2) foreach (var p in targets) p.SetGlow(.8f);
        }

        // ---------- panel actions: INSIDE (Explore Inside), FREEZE, REPLAY (the only scan is the opening's) ----------
        public bool Ability(string ability, out string reason)
        {
            reason = null;
            if (Clock.Paused) { reason = "Resume the investigation to use this."; return false; }
            if (activities.Card != null) { reason = "Finish the quick check first."; return false; }
            if (DemoBusy && ability is "REPLAY" or "INSIDE") { reason = "The demonstration is playing. You can freeze it or wait for it to finish."; return false; }
            var c = runner.Chapter?.id;
            switch (ability)
            {
                case "INSIDE":
                    if (runner.Awaiting == "inside") return true;
                    reason = c == "Scene02" ? "Explore the sclera, cornea, iris and pupil first." : "We are already inside the eye."; return false;
                case "FREEZE":
                    if (Clock.Frozen) return true;
                    if (runner.FreeMode == null) { reason = "Available during the experiments."; return false; }
                    return true;
                case "REPLAY":
                    if (runner.FreeMode == null) { reason = "Available during the experiments."; return false; }
                    if (ReplayFor(c) == null) { reason = "Use Replay explanation to hear Niko again."; return false; }
                    return true;
            }
            reason = "Unknown action."; return false;
        }

        public string AbilityLabel(string ability) => ability switch
        {
            "INSIDE" => "Explore Inside", "FREEZE" => Clock.Frozen ? "Unfreeze" : "Freeze", "REPLAY" => "Replay demo", _ => ability,
        };

        public void UseAbility(string ability)
        {
            if (navigation && navigation.Active) navigation.Leave();
            if (!Ability(ability, out var reason)) { hud.Toast(reason); sfx.Play("deny", .6f); return; }
            switch (ability)
            {
                case "INSIDE": runner.Press("inside"); break;
                case "FREEZE": runner.SetFrozen(!Clock.Frozen); hud.Toast(Clock.Frozen ? "Frozen: the biology is paused. Menus still work." : "Unfrozen."); break;
                case "REPLAY": Clock.Frozen = false; RunDemo(Run(ReplayFor(runner.Chapter.id), "replay", 0)); break;
            }
        }

        string SimulationFor(string c) => c switch { "Scene05" => "brightness", "Scene06" => "distance", "Scene08" => "photoreceptors", _ => null };
        static string ReplayFor(string c) => c switch { "Scene04" => "traceNerve", "Scene05" => "replayLight", "Scene06" => "replayAccommodation", "Scene07" => "replayImage", "Scene08" => "replayDetection", "Scene09" => "replaySignals", "Scene10" => "journey", _ => null };

        // ---------- Ask Niko: whitelisted guide actions ----------
        public bool CanGuideAction(string action, out string reason)
        {
            reason = null;
            switch (action)
            {
                case "replay": if (narrator.LastCore == null) { reason = "There's nothing to replay yet."; return false; } return true;
                case "brighter": case "dimmer":
                    if (runner.Chapter?.id != "Scene05") { reason = "Brightness is something we can change when we follow light into the eye."; return false; }
                    if (runner.FreeMode != "brightness") { reason = "Use the brightness controls yourself — that's the experiment!"; return false; }
                    return true;
                case "near": case "distant":
                    if (runner.Chapter?.id != "Scene06") { reason = "We'll compare near and distant objects when we study the lens."; return false; }
                    if (runner.FreeMode != "focus") { reason = "Use the Near and Distant buttons to run Niko's demonstration."; return false; }
                    return true;
                case "INSIDE": case "FREEZE": case "REPLAY":
                    return Ability(action, out reason);
                case "resetView": return true;
            }
            reason = "I can't do that here."; return false;
        }

        public void GuideAction(string action, string part)
        {
            switch (action)
            {
                case "explore": if (!runner.RequestPart(part, out var why)) hud.Toast(why); break;
                case "show": ShowPart(part); break;
                case "replay": hud.ReplayExplanation(); break;
                case "brighter": SetBrightness(Mathf.Min(1, Brightness + .3f)); break;
                case "dimmer": SetBrightness(Mathf.Max(0, Brightness - .3f)); break;
                case "near": case "distant": Simulation = SimulationFor(runner.Chapter.id); UseControl(action); break;
                case "resetView": ResetView(); break;
                default: UseAbility(action); break;
            }
        }

        /// "Show the cornea": highlight a taught, visible structure and frame it, without jumping during narration.
        void ShowPart(string id)
        {
            var p = Part(id);
            if (!p || !p.Visible) { hud.Toast("That structure isn't in view right now."); return; }
            p.SetGlow(.8f);
            runner.RunSide(FadeGlow(p));
            if (!LessonRules.FreeDemo(runner.State)) return;
            var shot = (runner.Chapter.id == "Scene02" ? "S02_Part_" : "S03_Part_") + id;
            if (cams.Has(shot) && runner.Chapter.id is "Scene02" or "Scene03") runner.RunSide(cams.MoveTo(shot, 1.2f));
            hud.labels.Clear(); hud.labels.Add(p.label, eye.LabelOf(p), eye.FocusOf(p), null);
        }

        IEnumerator FadeGlow(AnatomyPart p) { yield return new Wait(3); p.SetGlow(0); }
    }
}
