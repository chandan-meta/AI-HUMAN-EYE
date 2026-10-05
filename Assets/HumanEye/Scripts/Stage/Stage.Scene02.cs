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
            eyeRig.gameObject.SetActive(true);
            human.ResetVisual(true, 1);
            displayLight.intensity = displayIntensity * .15f;
            Socket(human.eyeRight, eyeRig);
            otherEye.gameObject.SetActive(true);
            foreach (var part in otherEye.parts) part.ResetVisual();
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
        public string IntroCaption { get; private set; }
        bool skipIntro;
        Routine introMove, introFade;
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
            introMove?.Cancel(); introMove = null;
            introFade?.Cancel(); introFade = null;
            sfx.sfx.Stop();
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            eyeRig.gameObject.SetActive(true);
            if (roomBase != null) Room(1);
            keyLight.intensity = keyIntensity;
            IntroCaption = null;
            Exposure(0); sfx.musicGain = 1; IntroPlaying = skipIntro = false;
            foreach (var b in beamRenderers) b.enabled = true;
        }

        IEnumerator Intro()
        {
            IntroPlaying = true; skipIntro = false; hud.Refresh();   // the Skip button appears in the same frame the intro starts
            float keyBase = keyIntensity;
            hud.Fade(1); Room(0); Exposure(-4); keyLight.intensity = 0; displayLight.intensity = 0;
            foreach (var b in beamRenderers) b.enabled = false;
            human.ResetVisual(true, 1); sfx.musicGain = 0;
            if (entrance) { entrance.SetGate(0); entrance.SetAccents(0); }
            if (Short)
            {
                cams.Snap("S02_EstablishB"); sfx.Play("boom", .6f);
                if (entrance) entrance.SetGate(1);
                yield return Routine.Tween(1.5f, k => { Room(k); Exposure(Mathf.Lerp(-4, 0, k)); keyLight.intensity = keyBase * k; hud.Fade(1 - k); });
            }
            else
            {
                cams.Snap("S02_IntroThreshold");
                sfx.Play("drone", .8f);
                var beats = new (float t, Action a)[]
                {
                    (0f, () => introFade = runner.RunSide(hud.Fade(0, 1f))),
                    (0f, () => introMove = runner.RunSide(cams.MoveTo("S02_IntroIgnite", 3))),
                    (3f, () => { sfx.Play("clunk1"); sfx.Play("riser", .45f); }),
                    (5.5f, () => { sfx.Play("clunk2"); introMove = runner.RunSide(cams.MoveTo("S02_IntroCrane", 4)); }),
                    (7f, () => sfx.Play("clunk3", .4f)),
                    (9.5f, () => { sfx.Play("boom", .5f); foreach (var b in beamRenderers) b.enabled = true; }),
                    (10f, () => { introMove = runner.RunSide(cams.MoveTo("S02_EstablishB", 3)); sfx.Play("shimmer", .4f); }),
                };
                int next = 0;
                for (float t = 0; t < 15f && !skipIntro; t += Clock.Delta)
                {
                    while (next < beats.Length && t >= beats[next].t) beats[next++].a();
                    IntroCaption = t < 3 ? "OBSERVATORY OF VISION · Approaching the laboratory" : t < 5.5f ? "Access granted · Gate opening" : t < 9.5f ? "Laboratory lights activating" : "Meet your first subject · The human eye";
                    if (entrance) { entrance.SetGate(Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 3) / 2.5f))); entrance.SetAccents(Mathf.Clamp01((t - 8) / 1.5f)); }
                    Room(Mathf.Lerp(.12f, 1, Mathf.Clamp01((t - 7) / 2)));
                    Exposure(Mathf.Lerp(-1.5f, 0, Mathf.Clamp01((t - 7) / 4)));
                    keyLight.intensity = keyBase * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 9.5f) / 1.5f));
                    displayLight.intensity = displayIntensity * Mathf.Clamp01((t - 10) / 2);
                    sfx.musicGain = Mathf.Clamp01((t - 9.5f) / 3);
                    yield return null;
                }
                if (skipIntro) sfx.sfx.Stop();
            }
            introMove?.Cancel(); introMove = null; cams.Snap("S02_EstablishB");
            introFade?.Cancel(); introFade = null;
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            Room(1); Exposure(0); keyLight.intensity = keyBase; displayLight.intensity = displayIntensity;
            foreach (var b in beamRenderers) b.enabled = true;
            human.ResetVisual(true, 1); sfx.musicGain = 1; hud.Fade(0);
            IntroPlaying = false; IntroCaption = null; hud.Refresh();
        }

        IEnumerator HumanReveal(float seconds)
        {
            sfx.Play("whoosh", .5f);
            yield return Routine.All(human.Reveal(seconds), Routine.Tween(seconds, t => displayLight.intensity = displayIntensity));
        }

        IEnumerator ScanSequence()
        {
            if (navigation) navigation.Leave();
            yield return cams.MoveTo("S02_EyeInSkull", Short ? .2f : 2.2f);
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
            foreach (var part in otherEye.parts) part.SetAlpha(0);
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
