using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// The neural signal from the retina to the brain: comet pulses on the optic nerve, the tracking shot that rides
    /// one into the ghosted brain, the dimmed "signal scene" lighting, and the visual area glowing on each arrival.
    /// Everything ramped here is restored by SignalSceneReset, reached from the off actions and from every
    /// Stage.Enter / ShowMenu / StopDemo through SignalPulses.Stopped (they all call signals.StopAll()).
    public partial class Stage
    {
        [Header("Follow / trace")]
        [Tooltip("Code-driven tracking camera: S09_Follow after the cinematic patch (the older Follow (TRACE) shot until then).")]
        public Unity.Cinemachine.CinemachineCamera followShot;

        public const string FollowShotName = "S09_Follow", BrainInsideShot = "S09_BrainInside";
        public const float GhostAlpha = .5f, FollowDistance = 1.0f, FollowRearAngle = 50f, FollowLookAhead = .25f;

        bool brainGhosted, brainRevealing, signalScene, arrivedOnce, lightsCaptured, eyeRimLent;
        float sceneK, sceneBrainRimBase, sceneEyeRimBase;
        SignalPulses.Pulse tracked;   // the Trace demo's pulse: its arrival always sounds
        Routine arrivalGlow;

        void RegisterSignals()
        {
            actions["nerveReveal"] = (_, seconds) => Routine.Tween(Short ? .3f : (seconds > 0 ? seconds : 2.5f), t => eye.SetNerveReveal(Mathf.Lerp(.12f, 1, t)), true);
            actions["brainReveal"] = (_, seconds) => BrainReveal(seconds > 0 ? seconds : 1.6f);
            actions["signals"] = (path, on) =>
            {
                signals.Emit(path, on > 0);
                if (on > 0) { arrivedOnce = false; sfx.Soft("signaltravel", .4f); }   // gives way to a reveal in the same frame; the travel loop follows
                if (path == "nerve") bed = on > 0 ? "pulse" : BedFor(runner.Chapter ? runner.Chapter.id : "");   // Scene 4: the stream's own bed
                return null;
            };
            actions["followSignal"] = (path, _) => FollowPulse(path);
            actions["traceNerve"] = (_, __) => Trace("nerve");
            actions["brainGhost"] = (arg, _) => { BrainGhost(arg == "on"); return null; };
            actions["signalScene"] = (arg, _) => SignalScene(arg == "on");
            actions["intoRetina"] = (_, __) => IntoRetina();
            actions["microLight"] = (level, on) => { micro.SetLight(on > 0, float.Parse(level, System.Globalization.CultureInfo.InvariantCulture)); if (on > 0) sfx.Play("cell", .5f); return null; };
            actions["microFocus"] = (which, _) => { MicroFocus(which); return null; };
            actions["microSignals"] = (_, on) => { micro.SetSignals(on > 0); if (on > 0) sfx.Play("pulse", .5f); return null; };
            actions["replayDetection"] = (_, __) => ReplayDetection();
            actions["replaySignals"] = (_, __) => Trace("nerve");
            actions["perception"] = (_, __) => Perception();
            RegisterEyeBrain();
            if (signals)
            {
                signals.Arrived -= SignalArrived; signals.Arrived += SignalArrived;
                signals.Stopped -= SignalSceneReset; signals.Stopped += SignalSceneReset;
            }
        }

        /// Introduce one cell type at a time: the other type fades back and the subject gets the restrained highlight.
        void MicroFocus(string which)
        {
            bool all = which == "all";
            micro.rods.SetAlpha(all || which == "rods" ? 1 : .22f); micro.cones.SetAlpha(all || which == "cones" ? 1 : .22f);
            micro.rods.SetGlow(which == "rods" ? .6f : 0); micro.cones.SetGlow(which == "cones" ? .6f : 0);
            if (!all) sfx.Play("isolate", .5f);
        }

        IEnumerator BrainReveal(float seconds)
        {
            brainRoot.gameObject.SetActive(true);
            sfx.Play("brainreveal", .6f);
            brainRevealing = true;   // a ghost requested during the reveal becomes the reveal's target instead of fighting it
            yield return Routine.Tween(seconds, t => { brain.SetAlpha(t * (brainGhosted ? GhostAlpha : 1), true); visualArea.SetAlpha(t, true); });
            brainRevealing = false;
        }

        // ---------- brain ghost and the signal scene ----------
        /// Ghost the brain so the signal is seen travelling inside it; the visual area stays solid and glows on each arrival.
        void BrainGhost(bool on)
        {
            brainGhosted = on;
            visualArea.SetAlpha(1);
            if (!brainRevealing) brain.SetAlpha(on ? GhostAlpha : 1);
            if (on) { arrivedOnce = false; if (!brainRevealing && brainRoot.gameObject.activeInHierarchy) sfx.Play("layerreveal", .3f); }
            else if (arrivalGlow != null) { arrivalGlow.Cancel(); arrivalGlow = null; }
        }

        /// Lighting for the nerve sequence: the hall and key dim, the brain rim and eye rim come up, so the signal is the
        /// brightest moving thing. Eased over .9 s (instant under reduced motion); "off" restores exactly.
        IEnumerator SignalScene(bool on)
        {
            if (on == signalScene) yield break;
            signalScene = on;
            if (on)
            {
                CaptureSignalLights();
                if (eyeRim && !eyeRim.enabled) { eyeRim.enabled = true; eyeRimLent = true; }
                if (brainRim) { brainRim.intensity = 0; brainRim.enabled = true; }
                sfx.Play("softwhoosh", .35f);
            }
            else if (arrivalGlow != null) { arrivalGlow.Cancel(); arrivalGlow = null; }
            float from = sceneK, to = on ? 1 : 0;
            yield return Routine.Tween(Short ? 0 : .9f, t => ApplySignalScene(Mathf.Lerp(from, to, t)));
            if (!on) SignalLightsOff();
        }

        void CaptureSignalLights()
        {
            if (lightsCaptured) return;
            lightsCaptured = true;
            sceneBrainRimBase = brainRim ? brainRim.intensity : 0;
            sceneEyeRimBase = eyeRim ? eyeRim.intensity : 0;
        }

        void ApplySignalScene(float k)
        {
            sceneK = k;
            if (roomBase != null) Room(Mathf.Lerp(1, .55f, k));
            keyLight.intensity = keyIntensity * Mathf.Lerp(1, .7f, k);
            if (brainRim) brainRim.intensity = sceneBrainRimBase * k;
            if (eyeRim && eyeRimLent) eyeRim.intensity = sceneEyeRimBase * k;
        }

        void SignalLightsOff()
        {
            if (brainRim) { brainRim.intensity = sceneBrainRimBase; brainRim.enabled = false; }
            if (eyeRim && eyeRimLent) { eyeRim.intensity = sceneEyeRimBase; eyeRim.enabled = false; }
            eyeRimLent = false;
        }

        /// Instant restore of everything the signal sequence changes (lights, ghost, glow tween, trace state).
        void SignalSceneReset()
        {
            tracked = null; arrivedOnce = false; brainRevealing = false;
            if (arrivalGlow != null) { arrivalGlow.Cancel(); arrivalGlow = null; }
            if (brainGhosted) { brainGhosted = false; if (brain) brain.SetAlpha(1); }
            if (!signalScene && sceneK <= 0 && !eyeRimLent) return;
            signalScene = false;
            if (keyLight) ApplySignalScene(0);
            SignalLightsOff();
        }

        /// A nerve pulse reached the brain: the visual area flashes and eases to a held glow while signals keep arriving.
        /// The arrival chime plays once per stream (first arrival) and for the Trace demo's pulse.
        void SignalArrived(string path)
        {
            if (!arrivedOnce && (path.StartsWith("vp_") || path == "nerve")) niko.Play("Surprise", 1.4f);   // the first signal reaches the brain
            if (path.StartsWith("vp_")) { cortexPulse = 1; if (!arrivedOnce) sfx.Play("signalarrive", .3f); arrivedOnce = true; return; }   // the eye-to-brain cinematic
            if (path != "nerve") return;
            if (!(brainGhosted || signalScene || tracked != null))
            {   // the stream's first arrival has been greeted; in the eye-to-brain cinematic it also connects audibly
                if (!arrivedOnce && EyeBrainPlaying) sfx.Play("signalarrive", .4f);
                arrivedOnce = true; return;
            }
            bool trace = tracked != null && tracked.HeadArrived;
            visualArea.SetGlow(1, true);
            if (arrivalGlow != null) arrivalGlow.Cancel();
            arrivalGlow = runner.RunSide(Routine.Tween(.7f, t => visualArea.SetGlow(Mathf.Lerp(1, .5f, t), true)));
            if (!arrivedOnce || trace) sfx.Play("signalarrive", trace ? .5f : .3f);
            arrivedOnce = true;
        }

        // ---------- tracking shot ----------
        /// Lesson follow (Scene 9, L02): ride with the newest pulse of the running stream (or launch one) from the eye into the
        /// brain, then hand over to the ghosted-brain shot. Reduced motion: no tracking; stream, ghost, glow and sounds remain.
        IEnumerator FollowPulse(string path)
        {
            signals.Emit(path, true);
            var pulse = signals.Newest(path);
            if (pulse == null) pulse = signals.Launch(path);
            if (pulse == null || Short) yield break;
            yield return Follow(pulse, path == "nerve");
        }

        /// Tracking shot on the code-driven follow camera: a side view of the route at the start, easing to a 3/4 rear
        /// view as the pulse nears the end, critically damped so path corners never jolt, looking a little ahead of the
        /// head. With intoBrain, hands over to S09_BrainInside the moment the pulse crosses from the nerve into the brain.
        IEnumerator Follow(SignalPulses.Pulse pulse, bool intoBrain)
        {
            var cam = cams.Get(FollowShotName); if (!cam) cam = followShot;
            if (!cam) { yield return new Until(() => !pulse.Alive || pulse.HeadArrived); yield break; }
            var path = pulse.Path;
            float entry = intoBrain && path.points.Length > 4 ? path.lengths[path.points.Length - 4] : path.total;   // the nerve curve ends 3 points before the route does
            var axis = signals.PointAt(path, path.total) - signals.PointAt(path, 0); axis.y = 0;
            if (axis.sqrMagnitude < 1e-4f) axis = Vector3.right;
            axis.Normalize();
            var side = Vector3.Cross(Vector3.up, axis);   // the camera side of the route (−z for a route running +x)
            Vector3 pos = Vector3.zero, look = Vector3.zero, vPos = Vector3.zero, vLook = Vector3.zero;
            bool first = true, handed = false;
            runner.RunSide(cams.MoveTo(cam.name, .8f));
            while (pulse.Alive && !pulse.HeadArrived)
            {
                float s = pulse.Distance;
                var head = signals.PointAt(path, s);
                var ahead = signals.PointAt(path, s + FollowLookAhead);
                float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(s / Mathf.Max(.01f, entry)));
                float yaw = Mathf.Lerp(0, FollowRearAngle, u) * Mathf.Deg2Rad;
                var dir = side * Mathf.Cos(yaw) - axis * Mathf.Sin(yaw);
                var want = head + dir * Mathf.Lerp(FollowDistance, FollowDistance * .85f, u) + Vector3.up * Mathf.Lerp(.24f, .36f, u);
                float dt = Clock.Delta;
                if (first) { pos = want; look = ahead; first = false; }
                else if (dt > 0)
                {
                    pos = Vector3.SmoothDamp(pos, want, ref vPos, .35f, float.PositiveInfinity, dt);
                    look = Vector3.SmoothDamp(look, ahead, ref vLook, .18f, float.PositiveInfinity, dt);
                }
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
                if (intoBrain && !handed && s >= entry && cams.Has(BrainInsideShot)) { handed = true; runner.RunSide(cams.MoveTo(BrainInsideShot, 1.2f)); }
                yield return null;
            }
            if (intoBrain && !handed && cams.Has(BrainInsideShot)) yield return cams.MoveTo(BrainInsideShot, 1.2f);
        }

        /// Worked demonstration (REPLAY): one slow pulse (or light front) with the camera riding it, then back to the
        /// previous shot. The nerve route ghosts the brain and chimes on arrival; reduced motion keeps the camera still.
        IEnumerator Trace(string route)
        {
            var previous = cams.Active ? cams.Active.name : null;
            if (route == "light")
            {
                rays.gameObject.SetActive(true);
                float keep = rays.reach; rays.reach = 0;
                yield return LightTravel(5, 6);
                rays.reach = Mathf.Max(keep, 5);
                yield break;
            }
            bool nerve = route != "retina";
            string path = nerve ? "nerve" : micro.signalPaths[0];
            if (!nerve) micro.SetLight(true, micro.lightLevel);
            var pulse = signals.Launch(path, nerve ? .55f : .35f);
            if (pulse == null) yield break;
            sfx.Play(nerve ? "signaltravel" : "pulse", .5f);
            if (nerve) { tracked = pulse; if (brainRoot.gameObject.activeInHierarchy) BrainGhost(true); }
            if (!Short) yield return Follow(pulse, nerve);
            else yield return new Until(() => pulse.HeadArrived || !pulse.Alive);
            if (nerve)
            {
                yield return new Wait(1.2f);
                tracked = null;
                if (brainGhosted) BrainGhost(false);
                visualArea.SetGlow(0);
            }
            if (!Short && previous != null) yield return cams.MoveTo(previous, 1);
        }

        IEnumerator IntoRetina()
        {
            hud.Info("Zooming into the retina…");
            sfx.Play("dive", .7f);
            yield return cams.MoveTo("S08_RetinaPush", Short ? 0 : 2.0f);
            yield return hud.Fade(1, .3f);
            micro.gameObject.SetActive(true);
            micro.ResetState();
            MicroFocus("all");
            niko.Place(Anchor("Micro"));
            cams.Snap("S08_MicroWide");
            rays.gameObject.SetActive(false);
            yield return null;
            yield return hud.Fade(0, .5f);
            hud.Info(null);
        }

        IEnumerator ReplayDetection()
        {
            micro.SetSignals(false); micro.SetLight(false, micro.lightLevel);
            yield return new Wait(.8f, true);
            micro.SetLight(true, micro.lightLevel); sfx.Play("cell", .5f);
            yield return new Wait(1.2f, true);
            micro.SetSignals(true);
            yield return Trace("retina");
        }

        IEnumerator Perception()
        {
            ShowObject("apple", true); PlaceObject("apple", 7);
            // The ray rig stays active (rays and lamp beams hidden) because it is what aims the retinal-image camera at the
            // object every frame; deactivating it left both insets blank on a fresh or resumed Scene 9.
            rays.gameObject.SetActive(true); rays.reach = 0; rays.showLamp = false; rays.showImage = true;
            yield return cams.MoveTo("S09_Perception", 1.8f);
            hud.ShowInsets(rays.objectCamera.targetTexture, true);
        }

        void InspectTapped(AnatomyPart part)
        {
            if (!LessonRules.FreeDemo(runner.State) || runner.FreeMode == null) return;
            if (runner.FreeMode == "photoreceptors" && part.id is "rods" or "cones")
            {
                MicroFocus(part.id);
                if (!narrator.Busy) runner.RunSide(ReplayCues(part.id == "rods" ? new[] { "S08_L02", "S08_L03" } : new[] { "S08_L04" }));
                return;
            }
            part.SetGlow(.8f); runner.RunSide(FadeGlow(part));
            hud.labels.Clear(); hud.labels.Add(part.label, eye.LabelOf(part), eye.FocusOf(part), null);
            hud.Toast(part.label);
        }

        IEnumerator ReplayCues(string[] cues) { foreach (var c in cues) yield return narrator.Play(c, NarrationSource.Replay); }

        IEnumerable<(string text, Transform anchor, Transform target)> ExtraLabels(string set)
        {
            var c = runner.Chapter.id;
            if (c == "Scene08") foreach (var l in micro.extraLabels) yield return (l.name, l, l);
            if ((c is "Scene05" or "Scene06" or "Scene07" or "Scene10") && rays.targetTop && rays.targetTop.gameObject.activeInHierarchy)
                yield return (set == "journey" ? "Light from the object" : "Object (lit by the lamp)", rays.targetTop, rays.targetTop);
        }
    }
}
