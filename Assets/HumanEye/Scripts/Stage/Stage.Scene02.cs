using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HumanEye
{
    public partial class Stage
    {
        void EnterScene02()
        {
            human.ResetVisual(true, .06f);
            displayLight.intensity = displayIntensity * .15f;
            Socket(human.eyeRight, eyeRig);
            otherEye.gameObject.SetActive(true);
            Socket(human.eyeLeft, otherEye.transform);
            eye.SetCutInstant(false); otherEye.SetCutInstant(false);
            eye.SetNerveReveal(.12f); otherEye.SetNerveReveal(.12f);
            niko.Place(Anchor("Intro"));
            niko.gameObject.SetActive(false);
            cams.Snap("S02_EstablishA");
        }

        static void Socket(Transform socket, Transform t)
        {
            t.SetPositionAndRotation(socket.position, socket.rotation);
            t.localScale = socket.lossyScale;
        }

        void RegisterScene02()
        {
            actions["nikoAppear"] = (_, seconds) => niko.Appear(seconds > 0 ? seconds : 1.2f);
            actions["humanReveal"] = (_, seconds) => HumanReveal(seconds > 0 ? seconds : 1.6f);
            actions["scan"] = (_, __) => ScanSequence();
            actions["illuminate"] = (_, __) => { foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(1); sfx.Play("ping"); return null; };
            actions["pinch"] = (_, __) => Pinch();
            actions["enlarge"] = (_, seconds) => Enlarge(seconds > 0 ? seconds : 2.4f);
            actions["intro"] = (_, __) => Intro();
        }

        // ---------- cinematic intro: the dark room powers up around the pedestal (Clock.Delta driven, so Pause holds it) ----------
        public bool IntroPlaying { get; private set; }
        bool skipIntro;
        public void SkipIntro() { if (IntroPlaying && !Clock.Paused) skipIntro = true; }

        void Room(float k) { for (int i = 0; i < roomLights.Length; i++) roomLights[i].intensity = roomBase[i] * k; }
        void Exposure(float ev)
        {
            if (!introVolume) return;
            introVolume.weight = ev < 0 ? 1 : 0;
            if (introVolume.profile.TryGet(out ColorAdjustments c)) c.postExposure.Override(ev);
        }

        /// Chapter entry, restart and menu all pass through here, so a cancelled intro never leaves the room dark.
        void IntroReset()
        {
            if (roomBase != null) Room(1);
            Exposure(0); sfx.musicGain = 1; IntroPlaying = skipIntro = false;
            foreach (var b in beamRenderers) b.enabled = true;
        }

        IEnumerator Intro()
        {
            IntroPlaying = true; skipIntro = false; hud.Refresh();   // the Skip button appears in the same frame the intro starts
            float keyBase = keyLight.intensity;
            hud.Fade(1); Room(0); Exposure(-4); keyLight.intensity = 0; displayLight.intensity = 0;
            foreach (var b in beamRenderers) b.enabled = false;
            human.ResetVisual(true, 0); sfx.musicGain = 0;
            if (Short)
            {
                cams.Snap("S02_EstablishB"); sfx.Play("boom", .6f);
                yield return Routine.Tween(1.5f, k => { Room(k); Exposure(Mathf.Lerp(-4, 0, k)); keyLight.intensity = keyBase * k; hud.Fade(1 - k); });
            }
            else
            {
                cams.Snap("S02_IntroThreshold");
                sfx.Play("drone", .8f);
                var beats = new (float t, Action a)[]
                {
                    (0f, () => runner.RunSide(hud.Fade(0, 1f))),
                    (1f, () => { runner.RunSide(cams.MoveTo("S02_IntroIgnite", 4)); sfx.Play("clunk1"); }),
                    (2.2f, () => { sfx.Play("clunk2"); sfx.Play("riser", .8f); }),
                    (3f, () => sfx.Play("clunk3")),
                    (5f, () => runner.RunSide(cams.MoveTo("S02_IntroCrane", 3.5f))),
                    (5.4f, () => { sfx.Play("boom"); foreach (var b in beamRenderers) b.enabled = true; }),
                    (6.4f, () => sfx.Play("shimmer", .7f)),
                    (8.5f, () => { runner.RunSide(cams.MoveTo("S02_EstablishB", 3)); sfx.Play("whoosh", .5f); }),
                };
                int next = 0;
                for (float t = 0; t < 11.5f && !skipIntro; t += Clock.Delta)
                {
                    while (next < beats.Length && t >= beats[next].t) beats[next++].a();
                    Room(Mathf.Clamp01((t - 1) / 3.5f));                                                       // power-up 1 -> 4.5 s
                    Exposure(t < 5.4f ? Mathf.Lerp(-4, -1, Mathf.Clamp01((t - 1) / 4)) : Mathf.Lerp(-1, 0, Mathf.Clamp01(t - 5.4f)));
                    keyLight.intensity = keyBase * Mathf.Clamp01(t - 5.4f);                                   // beam strike
                    sfx.musicGain = Mathf.Clamp01((t - 5.4f) / 3);
                    human.skin.SetAlpha(.06f * Mathf.Clamp01((t - 6.4f) / 1.2f), true);                      // silhouette materialises
                    yield return null;
                }
                if (skipIntro) { sfx.sfx.Stop(); cams.Snap("S02_EstablishB"); }
            }
            Room(1); Exposure(0); keyLight.intensity = keyBase; displayLight.intensity = displayIntensity * .15f;
            foreach (var b in beamRenderers) b.enabled = true;
            human.ResetVisual(true, .06f); sfx.musicGain = 1; hud.Fade(0);
            IntroPlaying = false; hud.Refresh();
        }

        IEnumerator HumanReveal(float seconds)
        {
            sfx.Play("whoosh", .5f);
            yield return Routine.All(human.Reveal(seconds), Routine.Tween(seconds, t => displayLight.intensity = Mathf.Lerp(displayIntensity * .15f, displayIntensity, t)));
        }

        IEnumerator ScanSequence()
        {
            sfx.Play("scan");
            yield return human.Scan(Short ? .6f : 1);
        }

        IEnumerator Pinch()
        {
            niko.gestureTarget = eye.transform;
            niko.Play("Pinch", 1.4f);
            yield return new Wait(.7f);
            sfx.Play("whoosh");
        }

        /// The human freezes and disappears; the selected eye leaves its socket, enlarges in front of the learner.
        IEnumerator Enlarge(float seconds)
        {
            foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(0);
            var p0 = eyeRig.position; var r0 = eyeRig.rotation; var s0 = eyeRig.localScale;
            var dissolve = human.Dissolve(Short ? .3f : .9f);
            if (Short)
            {
                yield return dissolve;
                yield return hud.Fade(1, .2f);
                PlaceEye(heroPoint);
                yield return hud.Fade(0, .3f);
            }
            else
            {
                yield return Routine.All(dissolve, Routine.Tween(seconds, t =>
                {
                    float k = Mathf.SmoothStep(0, 1, t);
                    eyeRig.SetPositionAndRotation(Vector3.Lerp(p0, heroPoint.position, k), Quaternion.Slerp(r0, heroPoint.rotation, k));
                    eyeRig.localScale = Vector3.Lerp(s0, heroPoint.lossyScale, k * k);
                }));
            }
            otherEye.gameObject.SetActive(false);
            spin = true;
        }

        public void OnCue(string cue)
        {
            if (cue.EndsWith("_L01") || cue == "S02_L09" || cue == "S03_L12" || cue == "S04_L08") niko.Play("Present", 2.2f);
            else if (narrator.database.Get(cue)?.IsPrompt == true) niko.Play("Point", 2);
        }
    }
}
