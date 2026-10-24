using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        [Header("Ending")]
        public AudioClip closingLine;   // supplemental spoken closing (not a ledger cue); the text is shown in the info panel
        [Tooltip("The closing line's mouth keys (Builder.ApplyNikoCharacter), so Niko's lips follow it like a ledger line.")]
        public DialogueDatabase.Cue closingCue;
        public const string ClosingText = "Now you've seen the whole journey. Every part has a role, and together they help you see the world.";
        public static readonly string[] Journey =
            { "Light from the object", "Cornea", "Aqueous humour", "Pupil", "Crystalline lens", "Vitreous humour", "Retina (photoreceptors)", "Optic nerve", "Brain" };
        static readonly string[] JourneyParts = { null, "cornea", "aqueous", "pupil", "lens", "vitreous", "retina", "optic_nerve", "visual_area" };
        // The camera rides along with the light (FX_Orbit, placed from the eye wherever the chapter has put it): the object and
        // the eye together, each structure from the cut side (eye radii from its focus), the eye and brain, the visual area.
        static readonly float[] JourneyDistance = { 0, 2.2f, 2f, 2f, 2.2f, 2.6f, 2.4f, 0, 0 };
        bool riding;

        (Vector3 pos, Vector3 look) JourneyView(int i)
        {
            var e = eye.transform; float r = e.lossyScale.x; Vector3 side = -e.right, up = Vector3.up;
            if (i == 0) { var mid = e.TransformPoint(new Vector3(0, 0, EyeGeometry.LensZ + 3.6f)); return (mid + side * 12 * r + up * 2 * r, mid); }
            if (i <= 6) { var focus = eye.FocusOf(Part(JourneyParts[i])).position; return (focus + side * JourneyDistance[i] * r + up * .3f * r, focus); }
            var brainAt = visualArea ? visualArea.focus ? visualArea.focus.position : visualArea.transform.position : brainRoot.position;
            if (i == 8 && cams.Has("S04_BrainClose")) { var b = cams.Get("S04_BrainClose").transform; return (b.position, b.position + b.forward * 2); }
            var centre = Vector3.Lerp(e.position, brainAt, .5f); float sep = Vector3.Distance(e.position, brainAt);
            return (centre + side * sep * .95f + up * sep * .25f, centre);
        }

        /// Glide the code-driven FX_Orbit camera to a view (cutting onto it first, at the current picture, if another shot is live).
        IEnumerator Ride(Vector3 pos, Vector3 look, float seconds)
        {
            var shot = cams.Get(OrbitShot); if (!shot) yield break;
            riding = true;
            if (cams.Active != shot)
            {
                var b = hud.mainCamera.transform; shot.transform.SetPositionAndRotation(b.position, b.rotation);
                var lens = shot.Lens; lens.FieldOfView = hud.mainCamera.fieldOfView; shot.Lens = lens;
                yield return cams.MoveTo(OrbitShot, 0);
            }
            Vector3 p0 = shot.transform.position; var q0 = shot.transform.rotation; var q1 = Quaternion.LookRotation(look - pos, Vector3.up);
            float f0 = shot.Lens.FieldOfView, f1 = cams.Fitted(34);
            yield return Routine.Tween(Short ? .01f : seconds, t =>
            {
                float u = Mathf.SmoothStep(0, 1, t);
                shot.transform.SetPositionAndRotation(Vector3.Lerp(p0, pos, u), Quaternion.Slerp(q0, q1, u));
                var l = shot.Lens; l.FieldOfView = Mathf.Lerp(f0, f1, u); shot.Lens = l;
            }, true);
            riding = false;
        }

        void RegisterEnding()
        {
            actions["journey"] = (mode, __) => JourneyReplay(mode == "overview");
            actions["overview"] = (_, __) => Overview();
            actions["closing"] = (_, __) => Closing();
            actions["closeEye"] = (_, seconds) => eye.Cutaway(false, Short ? .3f : (seconds > 0 ? seconds : 1.2f));
        }

        void EnterLater(string id)
        {
            brightnessLive = false;
            eye.SetNerveReveal(id == "Scene04" ? .12f : 1);
            brainRoot.gameObject.SetActive(id is "Scene05" or "Scene06" or "Scene07" or "Scene09" or "Scene10");
            float medium = 7, near = rays.nearDistance;
            switch (id)
            {
                case "Scene04": niko.Place(Anchor("EyeLeft")); cams.Snap("S03_InsideHero"); break;
                case "Scene05": niko.Place(Anchor("Optics")); cams.Snap("S05_ObjectWide"); break;
                case "Scene06":
                    niko.Place(Anchor("Optics")); Optic("apple", medium, 2);
                    cams.Snap("S05_AqueousPupil"); break;
                case "Scene07":
                    niko.Place(Anchor("Optics")); Optic("apple", 6, 4);
                    cams.Snap("S06_Vitreous"); break;
                case "Scene08":
                    niko.Place(Anchor("Optics")); Optic("apple", 6, 5); rays.showImage = true;
                    cams.Snap("S07_RetinaCloser"); break;
                case "Scene09":
                    niko.Place(Anchor("Brain")); ShowObject("apple", true); PlaceObject("apple", medium);
                    cams.Snap("S09_RetinaNerve"); break;
                case "Scene10":
                    niko.Place(Anchor("Wide")); Optic("apple", medium, 5);
                    cams.Snap("S09_Wide"); break;
                case "Scene11":
                    niko.Place(Anchor("Eleven")); Optic("apple", near, 0);
                    ShowObject("tree", true); treeRoot.position = new Vector3(eye.transform.TransformPoint(new Vector3(0, 0, EyeGeometry.LensZ + rays.farDistance)).x, treeRoot.position.y, eye.transform.position.z - 1.5f);
                    cams.Snap("S10_Complete"); break;
            }
        }

        void Optic(string which, float u, float reach)
        {
            ShowObject(which, true); PlaceObject(which, u);
            eye.SetAccommodation(LightRays.RequiredAccommodation(u, rays.nearDistance, rays.farDistance), true);
            rays.gameObject.SetActive(true); rays.reach = reach; rays.showLamp = which == "apple";
        }

        public void OnAwait(string key, bool on)
        {
            if (key == "inside" && on) hud.Pulse("INSIDE");
            if (key is "brighter" or "dimmer" && on) { Simulation = "brightness"; brightnessLive = true; }
            if (key is "near" or "distant" && on) Simulation = "distance";
            hud.Refresh();
        }

        public void OnFree(string mode, bool on)
        {
            foreach (var p in VisibleStructures()) p.Selectable = on;
            switch (mode)
            {
                case "brightness": Simulation = on ? "brightness" : null; break;
                case "photoreceptors": Simulation = on ? "photoreceptors" : null; micro.rods.Selectable = micro.cones.Selectable = on; break;
            }
            if (on) hud.Info(mode == "brightness" ? "Experiment: change the brightness and watch the pupil. Press Continue when you are done."
                                                  : "Experiment: tap the rods or cones, and compare dim and bright light. Press Continue when you are done.");
            else { StopDemo(); hud.labels.Clear(); Clock.Frozen = false; hud.Info(null); }
            hud.Refresh();
        }

        /// The complete process as one connected demonstration: light from the object through every structure to the brain.
        /// In overview mode (the overhead teaching layout) each stage is also labelled on the model.
        public bool JourneyPlaying { get; private set; }
        IEnumerator JourneyReplay(bool overview)
        {
            JourneyPlaying = true;
            hud.ShowJourney(Journey, -1);
            if (overview) ShowLabels("journey");
            rays.gameObject.SetActive(true); rays.reach = 0; rays.showImage = false;
            for (int i = 0; i < Journey.Length; i++)
            {
                hud.ShowJourney(Journey, i);
                if (!overview && (i == 0 || JourneyParts[i] != null)) { var (pos, look) = JourneyView(i); runner.RunSide(Ride(pos, look, 1.2f)); }
                var part = JourneyParts[i] == null ? null : Part(JourneyParts[i]);
                if (part) part.SetGlow(.85f);   // one sound per step: the light's move (0-5), the retinal image, the signal, the visual area
                if (i <= 5) yield return LightTravel(Mathf.Min(5, new[] { 1f, 1.5f, 1.8f, 2f, 4f, 5f }[i]), overview ? 1.0f : 1.1f);
                if (i == 6) { rays.showImage = true; sfx.Play("cell", .6f); yield return new Wait(1.0f, true); }
                if (i == 7) { bool done = false; sfx.Play("pulse", .5f); signals.Fire("nerve", 1, () => done = true); yield return new Until(() => done); }
                if (i == 8) { visualArea.SetGlow(.9f); sfx.Play("cortex", .45f); yield return new Wait(1.0f, true); }
                if (part) part.SetGlow(.2f);
            }
            yield return new Wait(.8f, true);
            foreach (var id in JourneyParts) if (id != null) Part(id)?.SetGlow(0);
            hud.ShowJourney(null, -1);
            JourneyPlaying = false;
        }

        /// Pull back into the stable overhead layout where object, eye, route and brain read together.
        IEnumerator Overview()
        {
            hud.Info("The whole journey, seen from above: object → eye → optic nerve → brain.");
            sfx.Play("overview", .8f);
            niko.Place(Anchor("Wide"));
            yield return cams.MoveTo("S10_Overview", Short ? 0 : 2.6f);
            ShowLabels("journey");
            yield return new Wait(.6f);
        }

        /// Niko in frame beside the eye with a warm closing line (supplemental text + clip, spoken through the narration channel).
        IEnumerator Closing()
        {
            ShowLabels(null);
            bed = "finale";   // the warm ending bed through the closing and the end menu
            runner.RunSide(niko.MoveTo(Anchor("Closing"), Short ? .2f : 1.2f, Short));
            yield return cams.MoveTo("S11_Closing", Short ? 0 : 1.8f);
            niko.lookTarget = lookCamera; niko.Play("Goodbye", 3.5f); niko.Express("Happy", 6);
            hud.Info(ClosingText);
            if (closingLine)
            {   // through the voice's own clip (not a one-shot), so voice.time follows it and the mouth keys line up; after any
                // replay or answer still playing, and while it plays the narrator counts as busy, so those queue behind it
                yield return new Until(() => !narrator.Busy);
                narrator.voice.clip = closingLine; narrator.voice.time = 0; narrator.voice.Play(); narrator.Supplemental = closingCue;
                yield return new Wait(closingLine.length + .3f);
                narrator.Supplemental = null;
            }
            else yield return new Wait(2.5f);
            sfx.Play("complete", .8f);
            yield return new Wait(1.2f);
        }
    }
}
