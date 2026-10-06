using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

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
            if (entrance && ResetDolly()) cams.Snap("S02_IntroDolly"); else cams.Snap(entrance ? "S02_IntroThreshold" : "S02_EstablishA");
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
            actions["humanReveal"] = (_, seconds) => HumanReveal(seconds > 0 ? seconds : 1.6f);
            actions["scan"] = (_, __) => ScanSequence();
            actions["illuminate"] = (_, __) => { foreach (var id in new[] { "sclera", "iris", "cornea" }) eye.Get(id).SetGlow(1); sfx.Play("ping"); hud.Info("Scan complete · sclera, cornea, iris and pupil identified."); return null; };
            actions["pinch"] = (_, __) => Pinch();
            actions["enlarge"] = (_, seconds) => Enlarge(seconds > 0 ? seconds : 2.4f);
            actions["intro"] = (_, __) => Intro();
            actions["info"] = (text, _) => { hud.Info(text); return null; };
        }

        // ---------- cinematic intro ----------
        // Initial view → establishing approach → bolt retracts, gate halves slide apart → pause → camera moves in through
        // the gate → Niko enters once it is fully open → camera settles beside him → intro ends → the interface fades in.
        // One code-driven dolly (S02_IntroDolly), Clock.Delta timed (Pause holds it); Skip jumps to the finished state.
        public bool IntroPlaying { get; private set; }
        public string IntroCaption { get; private set; }
        public float IntroTime { get; private set; }
        public const float IntroLength = 24.6f, DollyFov = 46;
        public const float TApproachEnd = 4.5f, TBolt = 5f, TGateOpen = 9.6f, TMoveIn = 10.4f, TInside = 16f, TNiko = 16.4f, TFollow = 17.5f, TArrive = 21.4f, TSettled = 24f;
        public static readonly Vector3 CamStart = new(-25.5f, 1.75f, .67f), CamApproach = new(-23.5f, 1.75f, .67f), CamInside = new(-14.5f, 1.85f, .45f);
        public static readonly Vector3 GateLook = new(-17.16f, 2.4f, .67f), PedestalLook = new(0, 1.25f, .6f), NikoStart = new(-19.2f, 1.3f, 1.55f);
        const string SettleShot = "S02_NikoMedium";   // the first lesson shot: Niko beside the spotlit subject, so the intro ends where the lesson starts
        bool skipIntro;
        Routine introFade;
        public void SkipIntro() { if (IntroPlaying && !Clock.Paused) skipIntro = true; }

        void Room(float k) { for (int i = 0; i < roomLights.Length; i++) roomLights[i].intensity = roomBase[i] * k; }
        void Exposure(float ev)
        {
            if (!introVolume) return;
            introVolume.weight = ev < 0 ? 1 : 0;
            if (introVolume.profile.TryGet(out ColorAdjustments c)) c.postExposure.Override(ev);
        }
        static float Ease(float t, float from, float to) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - from) / (to - from)));

        /// The dolly starts every entrance from the same corridor view (it is moved by code during the intro).
        bool ResetDolly()
        {
            var dolly = cams.Get("S02_IntroDolly"); if (!dolly) return false;
            dolly.transform.SetPositionAndRotation(CamStart, Quaternion.LookRotation(GateLook - CamStart));
            var lens = dolly.Lens; lens.FieldOfView = cams.Fitted(DollyFov); dolly.Lens = lens;
            return true;
        }

        /// Chapter entry, restart and menu all pass through here, so a cancelled intro never leaves the room dark.
        void IntroReset()
        {
            introFade?.Cancel(); introFade = null;
            sfx.sfx.Stop();
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            eyeRig.gameObject.SetActive(true);
            if (roomBase != null) Room(1);
            keyLight.intensity = keyIntensity;
            IntroCaption = null; IntroTime = 0;
            Exposure(0); sfx.musicGain = 1; IntroPlaying = skipIntro = false;
            foreach (var b in beamRenderers) b.enabled = true;
        }

        IEnumerator Intro()
        {
            IntroPlaying = true; skipIntro = false; IntroTime = 0; hud.Refresh();   // only the small caption + Skip overlay is shown from here
            float keyBase = keyIntensity;
            hud.Fade(1); Room(.1f); Exposure(-1.6f); keyLight.intensity = 0; displayLight.intensity = 0;
            foreach (var b in beamRenderers) b.enabled = false;
            human.ResetVisual(true, 1); sfx.musicGain = 0;
            var anchor = Anchor("Intro");
            niko.gameObject.SetActive(false);
            if (entrance) { entrance.SetGate(0); entrance.SetAccents(0); }
            if (Short)
            {
                // Reduced motion: no travel. The lit lab with Niko already in place, revealed through a short fade.
                cams.Snap(SettleShot); sfx.Play("boom", .6f);
                if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
                niko.Place(anchor); niko.gameObject.SetActive(true);
                yield return Routine.Tween(1.5f, k => { Room(Mathf.Lerp(.1f, 1, k)); Exposure(Mathf.Lerp(-1.6f, 0, k)); keyLight.intensity = keyBase * k; displayLight.intensity = displayIntensity * k; hud.Fade(1 - k); });
            }
            else
            {
                var dolly = cams.Get("S02_IntroDolly"); var settle = cams.Get(SettleShot);
                if (ResetDolly()) cams.Snap("S02_IntroDolly"); else cams.Snap("S02_IntroThreshold");
                sfx.Play("drone", .8f);
                var beats = new (float t, Action a)[]
                {
                    (0f, () => introFade = runner.RunSide(hud.Fade(0, 1.6f))),
                    (TBolt, () => sfx.Play("clunk1", .8f)),
                    (TBolt + 1.5f, () => sfx.Play("riser", .4f)),
                    (TGateOpen, () => sfx.Play("clunk2", .6f)),
                    (TMoveIn, () => sfx.Play("whoosh", .25f)),
                    (TInside - 1.2f, () => { sfx.Play("boom", .45f); foreach (var b in beamRenderers) b.enabled = true; }),
                    (TNiko, () => { niko.gameObject.SetActive(true); niko.Play("Idle", .1f); sfx.Play("materialise", .45f); }),
                    (TSettled - .6f, () => sfx.Play("shimmer", .35f)),
                };
                int next = 0;
                for (float t = 0; t < IntroLength && !skipIntro; t += Clock.Delta)
                {
                    IntroTime = t;
                    while (next < beats.Length && t >= beats[next].t) beats[next++].a();
                    IntroCaption = t < TBolt ? "OBSERVATORY OF VISION" : t < TGateOpen ? "Access granted · gate opening" : t < TNiko ? "Entering the laboratory" : "Niko · Vision Investigator Officer";
                    if (entrance) { entrance.SetGate(Mathf.Clamp01((t - TBolt) / (TGateOpen - TBolt))); entrance.SetAccents(Ease(t, TBolt, TGateOpen)); }
                    Room(Mathf.Lerp(.1f, 1, Ease(t, TMoveIn, TInside)));
                    Exposure(Mathf.Lerp(-1.6f, 0, Ease(t, TMoveIn, TInside + 1)));
                    displayLight.intensity = displayIntensity * Ease(t, TInside - 1.2f, TInside + .6f);
                    keyLight.intensity = keyBase * Ease(t, TFollow, TSettled);
                    sfx.musicGain = Ease(t, TSettled - 2, IntroLength);
                    if (dolly && settle) IntroCamera(dolly, settle, t);
                    if (niko.gameObject.activeSelf) IntroNiko(anchor, t);
                    yield return null;
                }
                if (skipIntro) sfx.sfx.Stop();
            }
            introFade?.Cancel(); introFade = null; cams.Snap(SettleShot);
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            Room(1); Exposure(0); keyLight.intensity = keyBase; displayLight.intensity = displayIntensity;
            foreach (var b in beamRenderers) b.enabled = true;
            human.ResetVisual(true, 1); sfx.musicGain = 1; hud.Fade(0);
            niko.Place(anchor); niko.gameObject.SetActive(true); niko.lookTarget = lookCamera;
            IntroPlaying = false; IntroCaption = null; IntroTime = IntroLength; hud.Refresh();   // the main interface fades in from here
        }

        /// One continuous dolly: drift toward the closed gate, hold while it opens, move in through it, hold as Niko
        /// passes, then settle on the authored first lesson shot (pose and lens), so the final cut is invisible.
        void IntroCamera(CinemachineCamera dolly, CinemachineCamera settle, float t)
        {
            Vector3 pos; Quaternion rot; float fov = cams.Fitted(DollyFov);
            if (t < TMoveIn) { pos = Vector3.Lerp(CamStart, CamApproach, Ease(t, 0, TApproachEnd)); rot = Quaternion.LookRotation(GateLook - pos); }
            else if (t < TFollow)
            {
                pos = Vector3.Lerp(CamApproach, CamInside, Ease(t, TMoveIn, TInside));
                rot = Quaternion.LookRotation(Vector3.Lerp(GateLook, PedestalLook, Ease(t, TMoveIn, TMoveIn + 3f)) - pos);
            }
            else
            {
                // Follow: keep the pedestal until Niko has passed, swing onto him as he flies ahead, then converge on the
                // authored look point so he never leaves the frame while the camera travels to the settle pose.
                float k = Ease(t, TFollow, TSettled);
                pos = Vector3.Lerp(CamInside, settle.transform.position, k);
                var nikoHead = niko.transform.position + Vector3.up * .6f;
                var settleLook = settle.transform.position + settle.transform.forward * 2.5f;
                var look = Vector3.Lerp(Vector3.Lerp(PedestalLook, nikoHead, Ease(t, TFollow + 1f, TFollow + 2.5f)), settleLook, k);
                rot = Quaternion.LookRotation(look - pos, Vector3.up);
                fov = Mathf.Lerp(fov, settle.Lens.FieldOfView, k);
            }
            dolly.transform.SetPositionAndRotation(pos, rot);
            var lens = dolly.Lens; lens.FieldOfView = fov; dolly.Lens = lens;
        }

        /// Niko flies in through the open gate on a gentle arc, passes the camera and settles at his anchor beside the subject.
        void IntroNiko(Transform anchor, float t)
        {
            float u = Ease(t, TNiko, TArrive);
            var pos = Vector3.Lerp(NikoStart, anchor.position, u) + Vector3.up * (.45f * Mathf.Sin(u * Mathf.PI));
            var heading = anchor.position - NikoStart; heading.y = 0;
            var rot = Quaternion.Slerp(Quaternion.LookRotation(heading.normalized), anchor.rotation, Ease(t, TArrive - 1.2f, TArrive + .3f));
            niko.transform.SetPositionAndRotation(pos, rot);
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
