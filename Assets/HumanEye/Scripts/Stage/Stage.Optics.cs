using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        bool brightnessLive;
        float appleBaseScale = 1;
        string viewed = "apple";
        Vector3 treeBaseScale = Vector3.one, pedestalBaseScale = Vector3.one;
        bool objectScalesCached;

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
            actions["imageInset"] = (mode, _) => { if (mode == "hide") hud.HideInsets(); else hud.ShowInsets(rays.objectCamera.targetTexture, mode == "perception"); return null; };
            actions["replayLight"] = (mode, __) => ReplayLight();
            actions["replayAccommodation"] = (mode, __) => ReplayAccommodation();
            actions["replayImage"] = (mode, __) => ReplayImage();
            actions["pupilTrial"] = (mode, __) => PupilTrial();
            RegisterFraming();   // Stage.Framing.cs: isolateKeep, nearApproach, distantApproach, lensFocus
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

        /// Instant show/hide (checkpoints and resets). Scales are restored so a cancelled transition never leaves an object small.
        void ShowObject(string which, bool on)
        {
            CacheObjectScales();
            var root = which == "tree" ? treeRoot : appleRoot;
            root.gameObject.SetActive(on); root.localScale = BaseScale(root);
            if (which == "apple") { pedestal.gameObject.SetActive(on); pedestal.localScale = pedestalBaseScale; lampRoot.gameObject.SetActive(on); }
        }

        // ---------- object transitions (no pops): objects grow in from their base and shrink away ----------
        void CacheObjectScales()
        {
            if (objectScalesCached) return;
            treeBaseScale = treeRoot.localScale; pedestalBaseScale = pedestal.localScale; objectScalesCached = true;
        }

        Vector3 BaseScale(Transform root) => root == treeRoot ? treeBaseScale : root == pedestal ? pedestalBaseScale : Vector3.one * appleBaseScale;

        IEnumerator Grow(Transform root, float seconds, string sound = null, float volume = .5f)
        {
            CacheObjectScales();
            var full = BaseScale(root);
            root.gameObject.SetActive(true);
            if (Short || seconds <= 0) { root.localScale = full; yield break; }
            if (sound != null) sfx.Play(sound, volume);
            yield return Routine.Tween(seconds, t => root.localScale = full * Mathf.Max(.01f, t), true);
            root.localScale = full;
        }

        IEnumerator Shrink(Transform root, float seconds, string sound = null, float volume = .4f)
        {
            CacheObjectScales();
            var full = BaseScale(root);
            if (!root.gameObject.activeSelf) yield break;
            if (!Short && seconds > 0)
            {
                if (sound != null) sfx.Play(sound, volume);
                yield return Routine.Tween(seconds, t => root.localScale = full * Mathf.Max(.01f, 1 - t), true);
            }
            root.gameObject.SetActive(false); root.localScale = full;
        }

        IEnumerator Delayed(float seconds, IEnumerator then) { yield return new Wait(seconds, true); yield return then; }

        /// The pedestal rises first, then the lit apple (with its lamp) grows in on top of it.
        IEnumerator AppleIn(float seconds)
        {
            lampRoot.gameObject.SetActive(true);
            yield return Routine.All(Grow(pedestal, seconds * .7f, "layerreveal", .5f), Delayed(seconds * .3f, Grow(appleRoot, seconds * .7f, "softwhoosh", .4f)));
        }

        IEnumerator AppleOut(float seconds)
        {
            yield return Routine.All(Shrink(appleRoot, seconds, "softwhoosh", .35f), Delayed(seconds * .3f, Shrink(pedestal, seconds * .7f)));
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

        public string Readout() => Simulation switch
        {
            "brightness" => $"Brightness: {Mathf.RoundToInt(Brightness * 100)}%   Pupil: {(eye.PupilRadius > .2f ? "larger" : eye.PupilRadius < .12f ? "smaller" : "medium")}",
            "distance" => FreeExploring ? "" : "Illustrative model — not a clinical optical simulator.",
            _ => "",
        };

        void LateUpdate()
        {
            if (!brightnessLive) return;
            rays.brightness = Brightness;
            eye.SetPupil(Mathf.Lerp(EyeGeometry.PupilMax * .95f, EyeGeometry.PupilMin * 1.15f, Brightness), false);
            RenderSettings.ambientIntensity = ambientBase * Mathf.Lerp(.35f, 1.15f, Brightness);
            displayLight.intensity = displayIntensity * Mathf.Lerp(.3f, 1.3f, Brightness);
            if (runner.Awaiting == "brighter" && Brightness >= .8f) runner.Press("brighter");
            if (runner.Awaiting == "dimmer" && Brightness <= .2f) runner.Press("dimmer");
        }

        /// Panel controls. Each returns a reason when it can't be used, so no button is ever dead.
        public bool Control(string id, out string reason)
        {
            reason = null;
            if (Clock.Paused) { reason = "Resume to change the demonstration."; return false; }
            if (DemoBusy) { reason = "Wait for the current demonstration to finish."; return false; }
            if (FreeExploring)
            {
                if (id is "near" or "distant" or "distanceSlider") return !fxBusy;
                reason = "Not part of Free Explore."; return false;
            }
            switch (id)
            {
                case "brighter": case "presetBright":
                    if (runner.Awaiting == "dimmer") { reason = "Now try decreasing the brightness."; return false; }
                    return brightnessLive;
                case "dimmer": case "presetDim":
                    if (runner.Awaiting == "brighter") { reason = "First, try increasing the brightness."; return false; }
                    return brightnessLive;
                case "near":
                    if (runner.Awaiting == "near") return true;
                    reason = runner.Awaiting == "distant" ? "Now try the distant object." : "Available during the lens demonstration."; return false;
                case "distant":
                    if (runner.Awaiting == "distant") return true;
                    reason = runner.Awaiting == "near" ? "Try the nearby object first." : "Available during the lens demonstration."; return false;
                case "distanceSlider":
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
            if (FreeExploring)
            {
                if (id == "distanceSlider")
                {
                    if (!rays.showImage) { rays.showImage = true; hud.ShowInsets(rays.objectCamera.targetTexture, false); }
                    SetObjectDistance(Mathf.Lerp(rays.nearDistance, rays.farDistance, value));
                }
                else RunDemo(FreeFocus(id == "near" ? rays.nearDistance : rays.farDistance * .7f));
                hud.Refresh(); return;
            }
            switch (id)
            {
                case "brighter": SetBrightness(Brightness + .15f); break;
                case "dimmer": SetBrightness(Brightness - .15f); break;
                case "presetBright": SetBrightness(.95f); break;
                case "presetDim": SetBrightness(.08f); break;
                case "near": if (!runner.Press("near")) RunDemo(NearDemo(true)); break;
                case "distant": if (!runner.Press("distant")) RunDemo(DistantDemo()); break;
                case "distanceSlider": SetObjectDistance(Mathf.Lerp(rays.nearDistance, rays.farDistance, value)); break;
                case "lightDim": micro.SetLight(true, .1f); sfx.Play("cell", .4f); break;
                case "lightBright": micro.SetLight(true, .95f); sfx.Play("cell", .7f); break;
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
            ShowObject("tree", false);
            PlaceObject("apple", 7);
            rays.gameObject.SetActive(true); rays.reach = 0; rays.showLamp = true;
            yield return AppleIn(seconds);
        }

        IEnumerator LightTravel(float to, float seconds)
        {
            rays.gameObject.SetActive(true);
            float from = rays.reach;
            if (to > from && seconds > .6f) sfx.Play("lightmove", .55f);
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
        // Each demonstration is split so the lens change happens on the lens close-up while its line is spoken: the object
        // moves (nearApproach / distantApproach, on the wide shot), the content cuts to S06_LensClose, then lensFocus runs
        // beside the line. NearDemo / DistantDemo keep the complete beat for the panel fallback and the question replays.
        const string LensShot = "S06_LensClose";

        /// The apple comes close (the tree, if shown, goes first). The lens is not touched here.
        IEnumerator NearApproach()
        {
            if (ciliary) { ciliary.Shown = false; ShowLabels("focus:lens"); }   // the muscle's label waits for the lens to work
            yield return Shrink(treeRoot, .4f);
            if (!appleRoot.gameObject.activeSelf) yield return AppleIn(.8f);
            float u0 = ObjectDistance;
            if (!Short) sfx.Play("softwhoosh", .3f);
            yield return Routine.Tween(Short ? .5f : 1.6f, t => PlaceObject("apple", Mathf.Lerp(u0, rays.nearDistance, t)), true);
        }

        /// The apple and its pedestal shrink away; the distant tree grows in at the far distance.
        IEnumerator DistantApproach()
        {
            if (ciliary) { ciliary.Shown = false; ShowLabels("focus:lens"); }
            yield return AppleOut(.6f);
            PlaceObject("tree", rays.farDistance);
            yield return Grow(treeRoot, .7f, "layerreveal", .5f);
        }

        /// The lens changes shape slowly enough to watch and glows while it does (the content clears the glow after S06_L04).
        IEnumerator LensFocus(bool near)
        {
            float target = near ? 1 : 0;
            hud.SetLensState(near ? "Near object · the lens is rounding up…" : "Distant object · the lens is flattening…");
            var lens = eye.Get("lens"); if (lens) lens.SetGlow(1);
            if (ciliary) { ShowLabels("focus:lens"); CiliaryLabel(near ? "Ciliary muscle tightens" : "Ciliary muscle relaxes"); }
            sfx.Play("lensfocus", .5f);
            eye.accommodationRate = Short ? 1.2f : .3f;
            eye.SetAccommodation(target, false);
            yield return Settle(() => Mathf.Abs(eye.Accommodation - target) < .01f || !Mathf.Approximately(eye.accommodationTarget, target));   // a newer focus takes over
            eye.accommodationRate = .45f;
            if (Mathf.Approximately(eye.accommodationTarget, target))
            {
                hud.SetLensState(near ? "Lens more curved · image sharp ✓" : "Lens less curved · image sharp ✓");
                sfx.Play("tick", .5f);   // a soft click of focus
            }
        }

        /// Puts the camera on the lens close-up when a demonstration runs outside the authored content order.
        IEnumerator OnLensShot()
        {
            if (cams.Has(LensShot) && (!cams.Active || cams.Active.name != LensShot)) yield return cams.MoveTo(LensShot, 1.0f);
        }

        IEnumerator NearDemo(bool quiet)
        {
            yield return NearApproach();
            yield return OnLensShot();
            yield return LensFocus(true);
        }

        IEnumerator DistantDemo()
        {
            yield return DistantApproach();
            yield return OnLensShot();
            yield return LensFocus(false);
        }

        IEnumerator ReplayAccommodation()
        {
            // Restores the known start state (medium distance), repeats both authored demonstrations in order on the lens
            // close-up, then returns to the shot the replay started on.
            var previous = cams.Active ? cams.Active.name : null;
            ShowObject("tree", false); ShowObject("apple", true); PlaceObject("apple", 7);
            eye.SetAccommodation(LightRays.RequiredAccommodation(7, rays.nearDistance, rays.farDistance), true);
            rays.reach = 5;
            yield return new Wait(.6f);
            yield return NearDemo(true);
            yield return new Wait(1.2f, true);
            yield return DistantDemo();
            var lens = eye.Get("lens"); if (lens) lens.SetGlow(0);
            if (previous != null && previous != LensShot) yield return cams.MoveTo(previous, 1);
        }

        // ---------- Scene 7 ----------
        /// The image is shown in the retinal popup (right panel), never as a picture painted on the anatomical retina.
        IEnumerator ImageForm(float seconds)
        {
            if (rays.showImage) yield break;
            rays.showImage = true;
            yield return new Wait(seconds * .5f, true);
        }

        IEnumerator ReplayImage()
        {
            rays.showImage = false; rays.reach = 3;
            yield return LightTravel(5, 3);
            yield return ImageForm(1.5f);
            hud.ShowInsets(rays.objectCamera.targetTexture, false);
        }
    }
}
