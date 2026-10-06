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
            FrameEyeInSkull();
            otherEye.gameObject.SetActive(true);
            foreach (var part in otherEye.parts) part.ResetVisual();
            Socket(human.eyeLeft, otherEye.transform);
            eye.SetCutInstant(false); otherEye.SetCutInstant(false);
            eye.SetNerveReveal(.12f); otherEye.SetNerveReveal(.12f);
            niko.Place(Anchor("Intro"));
            niko.gameObject.SetActive(false);
            // Start black at the gate: the frame between Enter and the intro step must not flash the lit room.
            cams.Snap(entrance ? "S02_IntroThreshold" : "S02_EstablishA");
            if (entrance) hud.Fade(1);
        }

        /// Scan close-up: in front of the face, aimed a little above the eye so the whole head stays in frame while the eye
        /// region fills it. Shared with the editor patch so the Scene-view preview matches.
        public static readonly Vector3 CloseUpOffset = new(.06f, .03f, -.65f);
        public static void AimCloseUp(Transform shot, Vector3 eye)
        {
            var pos = eye + CloseUpOffset;
            shot.SetPositionAndRotation(pos, Quaternion.LookRotation(eye + Vector3.up * .03f - pos));
        }
        /// The saved human carries an Editor scale that ResetVisual discards, so the close-up is aimed from the live socket,
        /// never from an author-time position.
        void FrameEyeInSkull() { var shot = cams.Get("S02_EyeInSkull"); if (shot) AimCloseUp(shot.transform, human.eyeRight.position); }

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
            actions["illuminate"] = (_, __) => { foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(1); sfx.Play("ping"); hud.Info("Scan complete · sclera, cornea, iris and pupil identified."); return null; };
            actions["pinch"] = (_, __) => Pinch();
            actions["enlarge"] = (_, seconds) => Enlarge(seconds > 0 ? seconds : 2.4f);
            actions["intro"] = (_, __) => Intro();
            actions["info"] = (text, _) => { hud.Info(text); return null; };
        }

        // ---------- cinematic intro: three directed shots, ~8.5 s, skippable (Clock.Delta driven, so Pause holds it) ----------
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

        /// Shot 1 (0–2.6 s): the closed gate from the corridor; it unlocks and lifts. Shot 2 (2.6–6.4 s): one crane through
        /// the entrance while the laboratory powers up, settling on the spotlit subject. Shot 3 (6.4–8.5 s): the establishing
        /// view holds as the music rises. No drifting camera while nothing happens: every move has a subject.
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
                    (0f, () => introFade = runner.RunSide(hud.Fade(0, .9f))),
                    (.8f, () => { sfx.Play("clunk1"); sfx.Play("riser", .45f); }),
                    (2.6f, () => { sfx.Play("clunk2", .7f); cams.Snap("S02_IntroCrane"); introMove = runner.RunSide(cams.MoveTo("S02_EstablishB", 3.6f)); }),
                    (5.6f, () => { sfx.Play("boom", .55f); foreach (var b in beamRenderers) b.enabled = true; }),
                    (6.4f, () => sfx.Play("shimmer", .4f)),
                };
                int next = 0;
                for (float t = 0; t < 8.5f && !skipIntro; t += Clock.Delta)
                {
                    while (next < beats.Length && t >= beats[next].t) beats[next++].a();
                    IntroCaption = t < 2.6f ? "OBSERVATORY OF VISION · Access granted" : t < 5.6f ? "Laboratory powering up" : "Your subject today: the human eye";
                    if (entrance) { entrance.SetGate(Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .8f) / 1.9f))); entrance.SetAccents(Mathf.Clamp01((t - 4.5f) / 1.2f)); }
                    Room(Mathf.Lerp(.12f, 1, Mathf.Clamp01((t - 3f) / 2.2f)));
                    Exposure(Mathf.Lerp(-1.5f, 0, Mathf.Clamp01((t - 3f) / 3f)));
                    keyLight.intensity = keyBase * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 5.6f) / 1.2f));
                    displayLight.intensity = displayIntensity * Mathf.Clamp01((t - 5.8f) / 1.4f);
                    sfx.musicGain = Mathf.Clamp01((t - 5.6f) / 2.5f);
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

        /// Niko performs the scan: he turns to the subject, points his scanner, and the wave sweeps the head while the panel
        /// reports what is happening. The camera is already on the full figure (S02_ScanFront) when this starts.
        IEnumerator ScanSequence()
        {
            if (navigation) navigation.Leave();
            niko.lookTarget = human.eyeRight; niko.gestureTarget = human.eyeRight;
            niko.Play("Point", 3.2f);
            hud.Info("Scanning the subject…");
            sfx.Play("nikoscan", .8f); sfx.Play("scan", .6f);
            yield return human.Scan(Short ? .6f : .85f);
            niko.lookTarget = lookCamera;
        }

        IEnumerator Pinch()
        {
            niko.gestureTarget = eye.transform;
            niko.Play("Pinch", 1.4f);
            yield return new Wait(.6f);
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

        /// Which structure a line is about, so Niko looks at it and points while he explains it.
        static readonly System.Collections.Generic.Dictionary<string, string> CuePart = new()
        {
            ["S02_L11"] = "sclera", ["S02_L12"] = "cornea", ["S02_L13"] = "iris", ["S02_L14"] = "pupil",
            ["S03_L02"] = "aqueous", ["S03_L04"] = "lens", ["S03_L07"] = "vitreous", ["S03_L09"] = "retina", ["S03_L11"] = "choroid",
            ["S04_L05"] = "optic_nerve", ["S04_L07"] = "visual_area", ["S05_L03"] = "cornea", ["S05_L04"] = "aqueous", ["S05_L08"] = "iris",
            ["S06_L01"] = "lens", ["S07_L01"] = "vitreous", ["S07_L02"] = "retina", ["S08_L02"] = "rods", ["S08_L04"] = "cones",
            ["S09_L01"] = "optic_nerve", ["S09_L03"] = "visual_area",
        };

        public void OnCue(string cue)
        {
            if (CuePart.TryGetValue(cue, out var id) && Part(id) is { } part)
            {
                niko.gestureTarget = niko.lookTarget = eye.FocusOf(part);
                niko.Play("Point", 2.6f);
            }
            else if (cue.EndsWith("_L01") || cue == "S02_L09" || cue == "S03_L12" || cue == "S04_L08") { niko.lookTarget = lookCamera; niko.Play("Present", 2.2f); }
            else if (narrator.database.Get(cue)?.IsPrompt == true) { niko.lookTarget = lookCamera; niko.Play("Talk", 2); }
            else niko.lookTarget = lookCamera;
        }
    }
}
