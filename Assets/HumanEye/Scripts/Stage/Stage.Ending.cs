using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        [Header("Ending")]
        public AudioClip closingLine;   // supplemental spoken closing (not a ledger cue); the text is shown in the info panel
        public const string ClosingText = "Now you've seen the whole journey. Every part has a role, and together they help you see the world.";
        public static readonly string[] Journey =
            { "Light from the object", "Cornea", "Aqueous humour", "Pupil", "Crystalline lens", "Vitreous humour", "Retina (photoreceptors)", "Optic nerve", "Brain" };
        static readonly string[] JourneyParts = { null, "cornea", "aqueous", "pupil", "lens", "vitreous", "retina", "optic_nerve", "visual_area" };

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
            if (key == "scan" && on) { niko.Play("RaiseHand", 30); hud.Pulse("SCAN"); }
            if (key == "scan" && !on) niko.Play("Idle", .1f);
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
        IEnumerator JourneyReplay(bool overview)
        {
            hud.ShowJourney(Journey, -1);
            if (overview) ShowLabels("journey");
            rays.gameObject.SetActive(true); rays.reach = 0; rays.showImage = false;
            for (int i = 0; i < Journey.Length; i++)
            {
                hud.ShowJourney(Journey, i);
                var part = JourneyParts[i] == null ? null : Part(JourneyParts[i]);
                if (part) part.SetGlow(.85f);
                sfx.Play("tick", .35f);
                if (i <= 5) yield return LightTravel(Mathf.Min(5, new[] { 1f, 1.5f, 1.8f, 2f, 4f, 5f }[i]), overview ? 1.0f : 1.1f);
                if (i == 6) { rays.showImage = true; sfx.Play("cell", .6f); yield return new Wait(1.0f, true); }
                if (i == 7) { bool done = false; sfx.Play("pulse", .5f); signals.Fire("nerve", 1, () => done = true); yield return new Until(() => done); }
                if (i == 8) { visualArea.SetGlow(.9f); yield return new Wait(1.0f, true); }
                if (part) part.SetGlow(.2f);
            }
            yield return new Wait(.8f, true);
            foreach (var id in JourneyParts) if (id != null) Part(id)?.SetGlow(0);
            hud.ShowJourney(null, -1);
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
            runner.RunSide(niko.MoveTo(Anchor("Closing"), Short ? .2f : 1.2f, Short));
            yield return cams.MoveTo("S11_Closing", Short ? 0 : 1.8f);
            niko.lookTarget = lookCamera; niko.Play("Wave", 3.5f);
            hud.Info(ClosingText);
            if (closingLine) { narrator.voice.PlayOneShot(closingLine); yield return new Wait(closingLine.length + .3f); }
            else yield return new Wait(2.5f);
            sfx.Play("complete", .8f);
            yield return new Wait(1.2f);
        }
    }
}
