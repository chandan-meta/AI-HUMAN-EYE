using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        bool brightnessLive;
        float appleBaseScale = 1;
        string viewed = "apple";

        void RegisterOptics()
        {
            actions["objectAppear"] = (_, seconds) => ObjectAppear(seconds > 0 ? seconds : 1.2f);
            actions["lightTravel"] = (to, seconds) => LightTravel(float.Parse(to, System.Globalization.CultureInfo.InvariantCulture), seconds > 0 ? seconds : 2.5f);
            actions["pupilView"] = (_, __) => PupilView();
            actions["brightnessLive"] = (_, __) => { brightnessLive = true; Simulation = "brightness"; hud.Refresh(); return null; };
            actions["pupilSettle"] = (_, __) => Settle(() => Mathf.Abs(eye.PupilRadius - eye.pupilTarget) < .002f);
            actions["distanceControls"] = (_, __) => { Simulation = "distance"; hud.Refresh(); return null; };
            actions["nearDemo"] = (mode, __) => NearDemo(mode == "replay" || mode == "hint");
            actions["distantDemo"] = (mode, __) => DistantDemo();
            actions["imageForm"] = (_, seconds) => ImageForm(seconds > 0 ? seconds : 1.5f);
            actions["imageInset"] = (mode, _) => { hud.ShowInsets(rays.objectCamera.targetTexture, mode == "perception"); return null; };
            actions["replayLight"] = (mode, __) => ReplayLight();
            actions["replayAccommodation"] = (mode, __) => ReplayAccommodation();
            actions["replayImage"] = (mode, __) => ReplayImage();
            actions["pupilTrial"] = (mode, __) => PupilTrial();
        }

        // ---------- placement ----------
        void PlaceObject(string which, float u)
        {
            viewed = which;
            ObjectDistance = u;
            var root = which == "tree" ? treeRoot : appleRoot;
            var pos = eye.transform.TransformPoint(new Vector3(0, 0, EyeGeometry.LensZ + u));
            if (which == "tree") pos.y = treeRoot.position.y;   // the tree stands on the floor
            root.position = pos;
            if (which == "apple") pedestal.position = new Vector3(pos.x, pedestal.position.y, pos.z);
            var markers = root.GetComponentsInChildren<Transform>(true);
            rays.targetTop = System.Array.Find(markers, m => m.name == "Top");
            rays.targetBottom = System.Array.Find(markers, m => m.name == "Bottom");
            rays.lamp = which == "apple" ? lampRoot : null;
            rays.showLamp = which == "apple";
        }

        void ShowObject(string which, bool on)
        {
            (which == "tree" ? treeRoot : appleRoot).gameObject.SetActive(on);
            if (which == "apple") { pedestal.gameObject.SetActive(on); lampRoot.gameObject.SetActive(on); }
        }

        public void SetObjectDistance(float u)
        {
            u = Mathf.Clamp(u, rays.nearDistance, rays.farDistance);
            ShowObject("tree", false); ShowObject("apple", true);
            PlaceObject("apple", u);
            eye.SetAccommodation(LightRays.RequiredAccommodation(u, rays.nearDistance, rays.farDistance), false);
        }

        public void SetBrightness(float b)
        {
            Brightness = Mathf.Clamp01(b);
        }

        void LateUpdate()
        {
            if (!brightnessLive) return;
            rays.brightness = Brightness;
            eye.SetPupil(Mathf.Lerp(EyeGeometry.PupilMax * .95f, EyeGeometry.PupilMin * 1.15f, Brightness), false);
            RenderSettings.ambientIntensity = Mathf.Lerp(.35f, 1.15f, Brightness);
            displayLight.intensity = displayIntensity * Mathf.Lerp(.3f, 1.3f, Brightness);
            if (runner.Awaiting == "brighter" && Brightness >= .8f) runner.Press("brighter");
            if (runner.Awaiting == "dimmer" && Brightness <= .2f) runner.Press("dimmer");
        }

        /// SIMULATE controls. Each returns a reason when it can't be used, so no button is ever dead.
        public bool Control(string id, out string reason)
        {
            reason = null;
            if (Clock.Paused) { reason = "Resume to change the demonstration."; return false; }
            if (DemoBusy) { reason = "Wait for the current demonstration to finish."; return false; }
            switch (id)
            {
                case "brighter": case "presetBright":
                    if (runner.Awaiting == "dimmer") { reason = "Now try decreasing the brightness."; return false; }
                    return brightnessLive;
                case "dimmer": case "presetDim":
                    if (runner.Awaiting == "brighter") { reason = "First, try increasing the brightness."; return false; }
                    return brightnessLive;
                case "near":
                    if (runner.Awaiting == "near" || runner.FreeMode == "distance") return true;
                    reason = runner.Awaiting == "distant" ? "Now try the distant object." : "Available during the lens demonstration."; return false;
                case "distant":
                    if (runner.Awaiting == "distant" || runner.FreeMode == "distance") return true;
                    reason = runner.Awaiting == "near" ? "Try the nearby object first." : "Available during the lens demonstration."; return false;
                case "distanceSlider":
                    if (runner.FreeMode == "distance") return true;
                    reason = "The distance slider unlocks after the Near and Distant demonstrations."; return false;
                case "lightDim": case "lightBright":
                    if (runner.FreeMode == "photoreceptors") return true;
                    reason = "Available after Niko's explanation."; return false;
            }
            reason = "Unavailable."; return false;
        }

        public void UseControl(string id, float value = 0)
        {
            if (!Control(id, out var reason)) { hud.Toast(reason); sfx.Play("deny", .6f); return; }
            switch (id)
            {
                case "brighter": SetBrightness(Brightness + .15f); break;
                case "dimmer": SetBrightness(Brightness - .15f); break;
                case "presetBright": SetBrightness(.95f); break;
                case "presetDim": SetBrightness(.08f); break;
                case "near": if (!runner.Press("near")) RunDemo(NearDemo(true)); break;
                case "distant": if (!runner.Press("distant")) RunDemo(DistantDemo()); break;
                case "distanceSlider": SetObjectDistance(Mathf.Lerp(rays.nearDistance, rays.farDistance, value)); break;
                case "lightDim": micro.SetLight(true, .1f); break;
                case "lightBright": micro.SetLight(true, .95f); break;
            }
            hud.Refresh();
        }

        public void SetBrightnessFromSlider(float v)
        {
            if (!brightnessLive) return;
            if (runner.Awaiting == "brighter" && v < Brightness) v = Brightness;   // only increases count first
            if (runner.Awaiting == "dimmer" && v > Brightness) v = Brightness;
            SetBrightness(v);
        }

        // ---------- Scene 5 ----------
        IEnumerator ObjectAppear(float seconds)
        {
            ShowObject("apple", true); PlaceObject("apple", 7);
            rays.gameObject.SetActive(true); rays.reach = 0; rays.showLamp = true;
            yield return Routine.Tween(seconds, t => appleRoot.localScale = Vector3.one * appleBaseScale * Mathf.Max(.01f, t));
        }

        IEnumerator LightTravel(float to, float seconds)
        {
            rays.gameObject.SetActive(true);
            float from = rays.reach;
            yield return Routine.Tween(Short ? seconds * .5f : seconds, t => rays.reach = Mathf.Lerp(from, to, t), true);
        }

        IEnumerator PupilView()
        {
            rays.reach = 0;
            yield return Routine.All(eye.Cutaway(false, Short ? .3f : 1.2f), cams.MoveTo("S05_PupilFront", 2));
        }

        IEnumerator Settle(System.Func<bool> done)
        {
            yield return new Until(done);
            yield return new Wait(.3f);
        }

        IEnumerator PupilTrial()
        {
            // Prediction trial: dim first, then brighter again so the change is clearly visible.
            SetBrightness(.15f);
            yield return Settle(() => Mathf.Abs(eye.PupilRadius - eye.pupilTarget) < .002f);
            SetBrightness(.95f);
            yield return Settle(() => Mathf.Abs(eye.PupilRadius - eye.pupilTarget) < .002f);
        }

        IEnumerator ReplayLight()
        {
            if (brightnessLive && !eye.IsCut) { yield return PupilTrial(); yield break; }
            rays.reach = 0;
            yield return LightTravel(5, 4);
        }

        // ---------- Scene 6 ----------
        IEnumerator NearDemo(bool quiet)
        {
            ShowObject("tree", false); ShowObject("apple", true);
            float u0 = ObjectDistance;
            yield return Routine.Tween(Short ? .5f : 1.6f, t => PlaceObject("apple", Mathf.Lerp(u0, rays.nearDistance, t)), true);
            eye.SetAccommodation(1, false);
            hud.SetLensState("Lens: more curved (near object)");
            yield return Settle(() => Mathf.Abs(eye.Accommodation - 1) < .01f);
        }

        IEnumerator DistantDemo()
        {
            yield return Routine.Tween(.6f, t => appleRoot.localScale = Vector3.one * Mathf.Max(.01f, 1 - t) * appleBaseScale, true);
            ShowObject("apple", false); appleRoot.localScale = Vector3.one * appleBaseScale;
            ShowObject("tree", true); PlaceObject("tree", rays.farDistance);
            eye.SetAccommodation(0, false);
            hud.SetLensState("Lens: less curved (distant object)");
            yield return Settle(() => eye.Accommodation < .01f);
        }

        IEnumerator ReplayAccommodation()
        {
            // Restores the known start state (medium distance) and repeats both authored demonstrations in order.
            ShowObject("tree", false); ShowObject("apple", true); PlaceObject("apple", 7);
            eye.SetAccommodation(LightRays.RequiredAccommodation(7, rays.nearDistance, rays.farDistance), true);
            rays.reach = 5;
            yield return new Wait(.6f);
            yield return NearDemo(true);
            yield return new Wait(1.2f, true);
            yield return DistantDemo();
        }

        // ---------- Scene 7 ----------
        IEnumerator ImageForm(float seconds)
        {
            rays.showImage = true;
            var quad = rays.imageQuad.GetComponent<Renderer>();
            yield return Routine.Tween(seconds, t => quad.material.SetFloat("_Alpha", t), true);
        }

        IEnumerator ReplayImage()
        {
            rays.showImage = false; rays.reach = 3;
            yield return LightTravel(5, 3);
            yield return ImageForm(1.5f);
        }
    }
}
