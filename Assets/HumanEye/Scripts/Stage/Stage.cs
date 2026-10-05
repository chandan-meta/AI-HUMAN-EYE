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
        public float menuNikoScale = 1.35f;
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
        float[] roomBase;

        public bool LabelsOn { get; private set; }
        public string Simulation { get; private set; }  // which SIMULATE panel is showing
        public float Brightness { get; private set; } = .5f;
        public float ObjectDistance { get; private set; } = 7;

        readonly Dictionary<string, Func<string, float, IEnumerator>> actions = new();
        Quaternion orbit = Quaternion.identity;
        float keyIntensity, displayIntensity;
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
            keyIntensity = keyLight.intensity; displayIntensity = displayLight.intensity;
            appleBaseScale = appleRoot.localScale.x;
            roomBase = roomLights.Select(l => l.intensity).ToArray();
            ambientParticles = observatory.GetComponentsInChildren<ParticleSystem>(true);
            EnsureActions();
        }

        /// Builds the action whitelist (also used by the editor validator outside Play mode).
        public void EnsureActions()
        {
            if (actions.Count > 0) return;
            RegisterCommon(); RegisterScene02(); RegisterScene03(); RegisterOptics(); RegisterSignals(); RegisterEnding();
        }

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

        public Transform Anchor(string name) => Array.Find(nikoAnchors, a => a.name == name) ?? nikoAnchors[0];

        Transform Find(string id)
        {
            var p = Part(id);
            if (p) return eye.FocusOf(p);
            return id switch { "eye" => eye.transform, "human" => human.transform, "brain" => brainRoot, "apple" => appleRoot, "tree" => treeRoot, "micro" => micro.transform, _ => eye.transform };
        }

        public AnatomyPart Part(string id)
        {
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
            demo = null; orbitYaw = orbitPitch = 0;
            brightnessLive = false;
            guide.Invalidate();
            signals.StopAll();
            ShowHint(null, 0);
            Clock.Frozen = false;
            spin = false; orbit = Quaternion.identity;
            LabelsOn = false; hud.labels.Clear();
            Simulation = null;
            foreach (var p in AllParts()) { p.ResetVisual(); p.Selectable = false; }
            eye.SetPupil(EyeGeometry.PupilDefault, true);
            eye.SetAccommodation(.5f, true);
            eye.SetNerveReveal(1);
            otherEye.gameObject.SetActive(false);
            human.ResetVisual(false);
            niko.gameObject.SetActive(true); niko.transform.localScale = authoredNikoScale; niko.lookTarget = lookCamera; niko.gestureTarget = eye.transform;
            micro.gameObject.SetActive(false);
            brainRoot.gameObject.SetActive(false);
            appleRoot.gameObject.SetActive(false); treeRoot.gameObject.SetActive(false); pedestal.gameObject.SetActive(false);
            rays.reach = 0; rays.showImage = false; rays.showLamp = false; rays.gameObject.SetActive(false);
            Brightness = .5f; rays.brightness = .5f;
            keyLight.intensity = keyIntensity; displayLight.intensity = displayIntensity;
            IntroReset();
            RenderSettings.ambientIntensity = 1;
            observatory.SetActive(true);
            SetBlur(0);
            SetEyeFade(1);
            hud.Fade(0);
            hud.HideInsets();
            switch (c.id)
            {
                case "Scene02": EnterScene02(); break;
                case "Scene03": PlaceEye(heroPoint); eye.SetCutInstant(false); eye.SetNerveReveal(.12f); niko.Place(Anchor("EyeLeft")); cams.Snap("S02_EyeHero"); break;
                default: PlaceEye(cutPoint); eye.SetCutInstant(true); EnterLater(c.id); break;
            }
        }

        public void ShowMenu()
        {
            Enter(runner.chapters[2]);
            eye.SetCutInstant(false); eye.SetNerveReveal(.12f); PlaceEye(heroPoint);
            if (menuPoint) PlaceEye(menuPoint);
            brainRoot.gameObject.SetActive(true);
            human.gameObject.SetActive(false); appleRoot.gameObject.SetActive(false); lampRoot.gameObject.SetActive(false);
            pedestal.gameObject.SetActive(false); rays.gameObject.SetActive(false); signals.StopAll();
            niko.Place(Anchor("Menu"));
            niko.transform.localScale = authoredNikoScale * menuNikoScale;
            cams.Snap(cams.Has("Menu") ? "Menu" : "S04_Wide");
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
        }

        public IEnumerator FocusPart(string id)
        {
            orbitYaw = orbitPitch = 0;
            foreach (var p in AllParts()) p.Selectable = false;
            hud.labels.Clear();
            sfx.Play("select");
            spin = false;
            yield return Routine.Tween(Short ? .2f : .8f, t => eye.transform.localRotation = Quaternion.Slerp(eye.transform.localRotation, Quaternion.identity, t));
            orbit = Quaternion.identity; cams.ResetZoom();
            Isolate(id);
            var shot = (runner.Chapter.id == "Scene02" ? "S02_Part_" : "S03_Part_") + id;
            if (cams.Has(shot)) yield return cams.MoveTo(shot, 1.3f);
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
            yield return new Wait(.5f);
            RestoreParts();
            yield return cams.MoveTo(runner.Chapter.id == "Scene02" ? "S02_EyeHero" : "S03_InsideHero", 1.2f);
            OnExplore(true);
            if (runner.Chapter.id == "Scene02") spin = true;
        }

        void Isolate(string id)
        {
            foreach (var p in eye.parts)
            {
                bool me = p.id == id;
                float a = me ? 1 : p.baseAlpha < 1 ? .35f : .18f;
                if (id == "iris" && p.id == "pupil") a = 1;
                if (id == "pupil" && p.id != "cornea") a = 1;   // an opening only reads as dark if the iris and everything behind it stay solid
                if (!eye.IsCut && p.id is "aqueous" or "lens" or "vitreous" or "retina" or "choroid") a = 1;   // closed eye: the interior is only seen through the pupil, where it must stay dark
                p.SetAlpha(a);
                p.SetGlow(me ? .55f : 0);
            }
        }

        void RestoreParts() { foreach (var p in AllParts()) { p.SetAlpha(1); p.SetGlow(0); } }

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
            spin = false; orbitYaw = orbitPitch = 0;
            cams.ResetZoom();
            runner.RunSide(Routine.Tween(.6f, t => eye.transform.localRotation = Quaternion.Slerp(eye.transform.localRotation, Quaternion.identity, t)));
        }

        void Update()
        {
            if (spin) { swayTime += Clock.VizDelta; eye.transform.localRotation = Quaternion.AngleAxis(Mathf.Sin(swayTime * .35f) * (Short ? 8 : 28), eyeRig.InverseTransformDirection(Vector3.up)); }
            foreach (var particles in ambientParticles)
            {
                if (Short) { if (!particles.isStopped) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
                else if (Clock.Paused || Clock.Frozen) { if (particles.isPlaying) particles.Pause(); }
                else if (!particles.isPlaying) particles.Play();
            }
        }

        // ---------- labels ----------
        public void ToggleLabels() { LabelsOn = !LabelsOn; ShowLabels(LabelsOn ? "scan" : null); }

        public void ShowLabels(string set)
        {
            hud.labels.Clear();
            if (set == null || !SaveSystem.Data.settings.labels) return;
            if (set == "explore")
            {
                foreach (var e in runner.Chapter.explore)
                {
                    var p = Part(e.partId);
                    if (p) hud.labels.Add(e.label + (runner.ExploreDone.Contains(e.partId) ? " ✓" : ""), eye.LabelOf(p), eye.FocusOf(p), e.partId);
                }
                return;
            }
            if (set.StartsWith("focus:")) { var p = Part(set.Substring(6)); if (p) hud.labels.Add(p.label, eye.LabelOf(p), eye.FocusOf(p), null); return; }
            foreach (var p in VisibleStructures()) hud.labels.Add(p.label, eye.LabelOf(p), eye.FocusOf(p), null);
            foreach (var extra in ExtraLabels(set)) hud.labels.Add(extra.text, extra.anchor, extra.target, null);
        }

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

        // ---------- abilities: SCAN, INSIDE, SIMULATE, TRACE, FREEZE, REPLAY ----------
        public bool Ability(string ability, out string reason)
        {
            reason = null;
            if (Clock.Paused) { reason = "Resume the investigation to use this tool."; return false; }
            if (activities.Card != null) { reason = "Finish or skip the optional question first."; return false; }
            if (DemoBusy && ability is "TRACE" or "REPLAY" or "INSIDE") { reason = "The demonstration is playing. You can freeze it or wait for it to finish."; return false; }
            var s = runner.State; var c = runner.Chapter?.id;
            bool free = runner.FreeMode != null || s == LessonState.Exploring || s == LessonState.AwaitingAction;
            switch (ability)
            {
                case "SCAN":
                    if (runner.Awaiting == "scan") return true;
                    if (c == "Scene02" && !SaveSystem.Data.cues.Exists(r => r.id == "S02_L05")) { reason = "Niko will ask you to scan in a moment."; return false; }
                    if (!free) { reason = "Available when Niko finishes speaking."; return false; }
                    return true;
                case "INSIDE":
                    if (runner.Awaiting == "inside") return true;
                    if (c == "Scene02") { reason = "Explore the sclera, cornea, iris and pupil first."; return false; }
                    if (c == "Scene08") { reason = "We're already inside the retina."; return false; }
                    if (!free) { reason = "Available when Niko finishes speaking."; return false; }
                    return true;
                case "SIMULATE":
                    if (SimulationFor(c) == null) { reason = "There's nothing to simulate in this part of the investigation."; return false; }
                    if (runner.Awaiting is "brighter" or "dimmer" or "near" or "distant") return true;
                    if (runner.FreeMode == null) { reason = "Unlocks after Niko's demonstration."; return false; }
                    return true;
                case "TRACE":
                    if (TraceRoute(c) == null) { reason = "There is no light or signal route to trace here yet."; return false; }
                    if (runner.FreeMode == null) { reason = "Unlocks after Niko's explanation."; return false; }
                    return true;
                case "FREEZE":
                    if (Clock.Frozen) return true;
                    if (runner.FreeMode == null && runner.State != LessonState.AwaitingAction) { reason = "Available after Niko's explanation."; return false; }
                    if (c is "Scene02" or "Scene03" or "Scene11") { reason = "Nothing is moving right now."; return false; }
                    return true;
                case "REPLAY":
                    if (runner.FreeMode == null) { reason = "Available after Niko's explanation."; return false; }
                    if (ReplayFor(c) == null) { reason = "Use Replay explanation to hear Niko again."; return false; }
                    return true;
            }
            reason = "Unknown ability."; return false;
        }

        public string AbilityLabel(string ability) =>
            ability == "INSIDE" && (runner.Awaiting == "inside" || runner.Chapter?.id == "Scene02") ? "Inside View"
            : ability == "FREEZE" && Clock.Frozen ? "UNFREEZE" : ability;

        public void UseAbility(string ability)
        {
            if (!Ability(ability, out var reason)) { hud.Toast(reason); sfx.Play("deny", .6f); return; }
            sfx.Play("tap", .6f);
            switch (ability)
            {
                case "SCAN": if (!runner.Press("scan")) ToggleLabels(); break;
                case "INSIDE": if (!runner.Press("inside")) RunDemo(cams.MoveTo(InsideShot(runner.Chapter.id), 1.2f)); break;
                case "SIMULATE": Simulation = Simulation == null ? SimulationFor(runner.Chapter.id) : null; hud.Refresh(); break;
                case "TRACE": Clock.Frozen = false; RunDemo(Trace(TraceRoute(runner.Chapter.id))); break;
                case "FREEZE": runner.SetFrozen(!Clock.Frozen); hud.Toast(Clock.Frozen ? "Frozen: the biology is paused. Menus still work." : "Unfrozen."); break;
                case "REPLAY": Clock.Frozen = false; RunDemo(Run(ReplayFor(runner.Chapter.id), "replay", 0)); break;
            }
        }

        string InsideShot(string chapter) => chapter switch { "Scene05" or "Scene06" or "Scene07" => "S06_Vitreous", "Scene04" or "Scene09" or "Scene10" => "S03_InsideHero", _ => "S03_InsideHero" };

        string SimulationFor(string c) => runner.FreeMode == "focus" ? "lens" : c switch { "Scene05" => "brightness", "Scene06" => "distance", "Scene08" => "photoreceptors", _ => null };
        static string ReplayFor(string c) => c switch { "Scene04" => "traceNerve", "Scene05" => "replayLight", "Scene06" => "replayAccommodation", "Scene07" => "replayImage", "Scene08" => "replayDetection", "Scene09" => "replaySignals", "Scene10" => "journey", _ => null };
        static string TraceRoute(string c) => c switch { "Scene04" or "Scene09" => "nerve", "Scene05" or "Scene06" or "Scene07" or "Scene10" => "light", "Scene08" => "retina", _ => null };

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
                case "SCAN": case "INSIDE": case "TRACE": case "FREEZE": case "REPLAY": case "SIMULATE":
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
                case "near": case "distant": Simulation = SimulationFor(runner.Chapter.id); UseControl(action); break;   // moves the object; focusing stays the learner's job
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
