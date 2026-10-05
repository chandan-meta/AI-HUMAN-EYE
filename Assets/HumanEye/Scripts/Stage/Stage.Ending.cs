using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye
{
    public partial class Stage
    {
        [Header("Ending")]
        public Volume blurVolume;
        public static readonly string[] Journey =
            { "Light from the object", "Cornea", "Aqueous humour", "Pupil", "Crystalline lens", "Vitreous humour", "Retina (photoreceptors)", "Optic nerve", "Brain" };
        static readonly string[] JourneyParts = { null, "cornea", "aqueous", "pupil", "lens", "vitreous", "retina", "optic_nerve", "visual_area" };

        void RegisterEnding()
        {
            actions["journey"] = (_, __) => JourneyReplay();
            actions["blurDemo"] = (_, seconds) => BlurDemo(seconds > 0 ? seconds : 3.5f);
            actions["fadeBlurred"] = (_, seconds) => FadeBlurred(seconds > 0 ? seconds : 2.5f);
            actions["eyeFade"] = (_, seconds) => EyeFadeOut(seconds > 0 ? seconds : 3);
            actions["cutToBlack"] = (_, __) => hud.Fade(1, .04f);
            actions["closeEye"] = (_, seconds) => eye.Cutaway(false, Short ? .3f : (seconds > 0 ? seconds : 1.4f));
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
                case "focus": Simulation = on ? "lens" : null; hud.SetLensState(null); if (on) FocusSetup(0); else eye.Get("lens").SetGlow(0); break;
                case "photoreceptors": Simulation = on ? "photoreceptors" : null; micro.rods.Selectable = micro.cones.Selectable = on; break;
                case "inspectImage": if (on) ShowLabels("scan"); break;
            }
            if (!on) { StopDemo(); hud.labels.Clear(); Clock.Frozen = false; }
            hud.Refresh();
        }

        IEnumerator JourneyReplay()
        {
            hud.ShowJourney(Journey, -1);
            rays.gameObject.SetActive(true); rays.reach = 0;
            for (int i = 0; i < Journey.Length; i++)
            {
                hud.ShowJourney(Journey, i);
                var part = JourneyParts[i] == null ? null : Part(JourneyParts[i]);
                if (part) part.SetGlow(.85f);
                if (i <= 5) yield return LightTravel(Mathf.Min(5, new[] { 1f, 1.5f, 1.8f, 2f, 4f, 5f }[i]), 1.1f);
                if (i == 6) { rays.showImage = true; yield return new Wait(1.2f, true); }
                if (i == 7) { bool done = false; signals.Fire("nerve", 1, () => done = true); yield return new Until(() => done); }
                if (i == 8) yield return new Wait(1.2f, true);
                if (part) part.SetGlow(.2f);
            }
            yield return new Wait(1, true);
            foreach (var id in JourneyParts) if (id != null) Part(id)?.SetGlow(0);
            hud.ShowJourney(null, -1);
        }

        IEnumerator BlurDemo(float hold)
        {
            ShowObject("tree", true);
            yield return cams.MoveTo("S11_NearFar", 2);
            if (blurVolume.profile.TryGet(out DepthOfField dof))
            {
                var cam = cams.brain.transform.position;
                float near = Vector3.Distance(cam, appleRoot.position), far = Vector3.Distance(cam, treeRoot.position);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(near + .6f);
                dof.gaussianEnd.Override(Mathf.Max(near + 1.2f, far * .7f));
                dof.gaussianMaxRadius.Override(1.0f);   // "slightly blurred"
            }
            yield return Routine.Tween(1.5f, SetBlur);
            yield return new Wait(hold);
        }

        void SetBlur(float w) { if (blurVolume) blurVolume.weight = w; }

        IEnumerator FadeBlurred(float seconds)
        {
            var renderers = treeRoot.GetComponentsInChildren<Renderer>();
            var scale = treeRoot.localScale;
            yield return Routine.Tween(seconds, t => treeRoot.localScale = new Vector3(scale.x, scale.y * Mathf.Max(.001f, 1 - t), scale.z));
            ShowObject("tree", false); treeRoot.localScale = scale;
        }

        /// The eye fades into the dark learning environment.
        IEnumerator EyeFadeOut(float seconds)
        {
            float k0 = keyLight.intensity, d0 = displayLight.intensity, a0 = RenderSettings.ambientIntensity;
            yield return Routine.Tween(seconds, t =>
            {
                SetEyeFade(1 - t);
                keyLight.intensity = Mathf.Lerp(k0, .02f, t); displayLight.intensity = Mathf.Lerp(d0, 0, t);
                RenderSettings.ambientIntensity = Mathf.Lerp(a0, .05f, t);
                foreach (var b in beamRenderers) b.enabled = t < .9f;
            });
        }

        void SetEyeFade(float a)
        {
            foreach (var p in eye.parts) p.SetAlpha(a, true);
            appleRoot.gameObject.SetActive(appleRoot.gameObject.activeSelf && a > .05f);
            pedestal.gameObject.SetActive(pedestal.gameObject.activeSelf && a > .05f);
            if (a > .99f) { RenderSettings.ambientIntensity = 1; foreach (var b in beamRenderers) b.enabled = true; }
        }
    }
}
