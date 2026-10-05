using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        [Header("Follow / trace")]
        public Unity.Cinemachine.CinemachineCamera followShot;
        public Vector3 followOffset = new(0, .35f, -1.1f);

        void RegisterSignals()
        {
            actions["nerveReveal"] = (_, seconds) => Routine.Tween(Short ? .3f : (seconds > 0 ? seconds : 2.5f), t => eye.SetNerveReveal(Mathf.Lerp(.12f, 1, t)), true);
            actions["brainReveal"] = (_, seconds) => BrainReveal(seconds > 0 ? seconds : 1.6f);
            actions["signals"] = (path, on) => { signals.Emit(path, on > 0); return null; };
            actions["followSignal"] = (path, seconds) => FollowPulse(path, seconds > 0 ? seconds : 1);
            actions["traceNerve"] = (_, __) => Trace("nerve");
            actions["intoRetina"] = (_, __) => IntoRetina();
            actions["microLight"] = (level, on) => { micro.SetLight(on > 0, float.Parse(level, System.Globalization.CultureInfo.InvariantCulture)); return null; };
            actions["microSignals"] = (_, on) => { micro.SetSignals(on > 0); return null; };
            actions["replayDetection"] = (_, __) => ReplayDetection();
            actions["replaySignals"] = (_, __) => Trace("nerve");
            actions["perception"] = (_, __) => Perception();
        }

        IEnumerator BrainReveal(float seconds)
        {
            brainRoot.gameObject.SetActive(true);
            sfx.Play("reveal", .6f);
            yield return Routine.Tween(seconds, t => { brain.SetAlpha(t, true); visualArea.SetAlpha(t, true); });
        }

        /// TRACE: one slow pulse (or light front) with the camera following it; reduced motion keeps the camera still.
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
            string path = route == "retina" ? micro.signalPaths[0] : "nerve";
            if (route == "retina") micro.SetLight(true, micro.lightLevel);
            bool done = false;
            if (!signals.Fire(path, .35f, () => done = true)) yield break;
            if (!Short && followShot)
            {
                runner.RunSide(cams.MoveTo(followShot.name, .8f));
                while (!done)
                {
                    if (signals.Lead(path, out var lead))
                        followShot.transform.SetPositionAndRotation(lead + followOffset * (route == "retina" ? 1 : 1), Quaternion.LookRotation(lead - (lead + followOffset)));
                    if (path == "nerve" && signals.Lead(path, out var p) && Vector3.Distance(p, visualArea.transform.position) < .6f) visualArea.SetGlow(.9f);
                    yield return null;
                }
                if (previous != null) yield return cams.MoveTo(previous, 1);
            }
            else yield return new Until(() => done);
            if (path == "nerve") { visualArea.SetGlow(.9f); yield return new Wait(1.2f); visualArea.SetGlow(0); }
        }

        IEnumerator FollowPulse(string path, float seconds)
        {
            signals.Emit(path, true);
            if (Short || !followShot) yield break;
            var p = signals.Get(path);
            yield return cams.MoveTo(followShot.name, .6f);
            yield return Routine.Tween(p.total / Mathf.Max(.01f, signals.speed) * seconds, t =>
            {
                var a = signals.PointAt(p, p.total * t);
                followShot.transform.SetPositionAndRotation(a + followOffset, Quaternion.LookRotation(-followOffset));
            });
        }

        IEnumerator IntoRetina()
        {
            yield return cams.MoveTo("S08_RetinaPush", Short ? 0 : 2.2f);
            yield return hud.Fade(1, .35f);
            micro.gameObject.SetActive(true);
            micro.ResetState();
            niko.Place(Anchor("Micro"));
            cams.Snap("S08_MicroWide");
            rays.gameObject.SetActive(false);
            yield return null;
            yield return hud.Fade(0, .6f);
        }

        IEnumerator ReplayDetection()
        {
            micro.SetSignals(false); micro.SetLight(false, micro.lightLevel);
            yield return new Wait(.8f, true);
            micro.SetLight(true, micro.lightLevel);
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
                micro.rods.SetGlow(part.id == "rods" ? .9f : 0); micro.cones.SetGlow(part.id == "cones" ? .9f : 0);
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
                yield return ("Object (lit by the lamp)", rays.targetTop, rays.targetTop);
        }
    }
}
