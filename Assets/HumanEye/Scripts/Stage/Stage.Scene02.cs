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
        [Header("Opening effects (Builder.CinematicIntro, Builder.ApplyReference)")]
        [Tooltip("Fine drifting dust around the subject under the platform beam.")] public ParticleSystem depthDust;
        [Tooltip("Soft burst at Niko's position when he appears.")] public ParticleSystem nikoSparkle;
        [Tooltip("Sparks at the gate's bottom edge as it lifts (burst).")] public ParticleSystem gateSparks;
        [Tooltip("Warm dust streaming out of the lab through the opening.")] public ParticleSystem gateDust;
        [Tooltip("Gold motes trailing the eye as it leaves the orbit and floats.")] public ParticleSystem extractMotes;
        [Tooltip("Horizontal gold burst under the title (burst).")] public ParticleSystem titleBurst;
        [Tooltip("Gold vortex Niko materialises from.")] public ParticleSystem nikoVortex;
        [Header("New lab (visual-match pass)")]
        [Tooltip("The lab's exhibit eye above the central stand: shown while the lab is introduced, it collapses into the learner's eye as that reaches the display, and stays away afterwards.")]
        public Transform displayEye;

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
            NikoWait();   // the opening is without him (replays too): he arrives with S02_L01
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
        /// Head and shoulders for the scan (the wave sweeps the head): the camera then pushes on into the eye close-up.
        public static void AimFace(Transform shot, Vector3 eyeLeft, Vector3 eyeRight)
        {
            var mid = (eyeLeft + eyeRight) * .5f; var pos = mid + new Vector3(.08f, -.03f, -1.2f);
            shot.SetPositionAndRotation(pos, Quaternion.LookRotation(mid + Vector3.down * .1f - pos));
        }

        static void Socket(Transform socket, Transform t)
        {
            t.SetPositionAndRotation(socket.position, socket.rotation);
            t.localScale = socket.lossyScale;
        }

        void RegisterScene02()
        {
            actions["intro"] = (_, __) => Intro();
            actions["info"] = (text, _) => { hud.Info(text); return null; };
        }

        // ---------- shared opening lifecycle and optional legacy ReferenceMatch choreography ----------
        // Production now uses Stage.Opening.cs (32.5 s, centered body, skin -> skeleton -> muscle -> eye).
        // Shot list and timings: Docs/HumanEye/Polish/REFERENCE_SHOTLIST_2026-10-08.md. One code-driven dolly (S02_IntroDolly)
        // runs on the reference video's own clock τ (seconds into the recording, 6 → 97). ReferenceMatch: corridor → (cut) low angle on the gate →
        // (cut) the gate lifts, dust pours out, push through the door, the lab lights up → dolly to the subject: full body →
        // face → the skin wipes to muscle (orbicularis oculi) → bone (the orbits) → glass (eyes, eye muscles, optic nerves),
        // slow orbit to the selected eye → the eye leaves its orbit and flies to the central display, where the lab's exhibit
        // eye collapses into it → title → (cut) Niko materialises from a vortex beside it → his first lesson shot. This is the
        // module's only scan (visual-match pass): the lesson continues straight to Explore Eye. Clock.Delta timed (Pause holds
        // it); Skip jumps to the finished state; reduced motion shows four stills (≤ 8 s). The subject stands on the stand's
        // ring tier facing the gate and is gone after the opening.
        public enum CinematicTiming { Production, ReferenceMatch }
        [Header("Cinematics (reference pass)")]
        [Tooltip("Production: the ~30 s student cut. Reference match: the Human Brain reference's own shot durations (video 0:06–1:37), for side-by-side camera checks.")]
        public CinematicTiming timing = CinematicTiming.Production;

        public bool IntroPlaying { get; private set; }
        public bool CinematicPlaying => IntroPlaying || EyeBrainPlaying;
        public string IntroCaption { get; private set; }
        public float IntroTime { get; private set; }
        /// The cinematic title (opening and eye-to-brain): text, share of letters shown, subtitle and its opacity (Hud draws it).
        public string CineTitle { get; private set; }
        public string CineSubtitle { get; private set; }
        public float CineTitleAlpha { get; private set; } = 1;
        public float CineLetters { get; private set; }
        public float CineSubtitleAlpha { get; private set; }

        // (beat, reference start τ in the video, production length in seconds); the last row closes the table.
        static readonly (string name, float at, float prod)[] IntroBeats =
        {
            ("corridor", 6f, 2.0f), ("gate-low", 10.15f, 1.2f), ("gate-lift", 12.65f, 1.6f), ("gate-push", 14.5f, 2.0f),
            ("threshold", 22f, 1.0f), ("lab", 24f, 2.8f), ("body", 35f, .8f), ("face", 41f, 2.8f), ("skin", 49f, 1.8f),
            ("muscle", 54.5f, 1.8f), ("xray", 60.5f, 2.8f), ("extract", 67.5f, 2.2f), ("float", 72f, 1.2f), ("title", 76f, 2.8f),
            ("niko", 84.5f, 3.0f), ("end", 97f, 0),
        };
        public static float IntroLengthFor(CinematicTiming t) => t == CinematicTiming.ReferenceMatch ? IntroBeats[^1].at - IntroBeats[0].at : OpeningLength;
        public float IntroLength => IntroLengthFor(timing);
        public const float ShortLength = 7.5f, DollyFov = 50, TCutGateLow = 10.15f, TCutGateLift = 12.65f, TExtract = 67.5f, TCutNiko = 84.5f;
        public static readonly Vector3 CamStart = new(-29.5f, 2f, .67f), GateLook = new(-17.16f, 3.3f, .67f);
        const string SettleShot = "S02_NikoMedium";   // the first lesson shot: Niko beside the platform, so the intro ends where the lesson starts
        const float XrayAlpha = .06f, WipeEdge = .005f, AnatomicalEye = .78f;   // the module's eye is 16 mm; with the skin gone it shows at ~12.5 mm, as in a real orbit
        static readonly Color GlassRim = new(.92f, .96f, 1f), Bone = new(.84f, .77f, .64f);
        static readonly string[] IntroTitle = { "INSIDE YOUR EYES", "Discover How You See the World" };
        bool skipIntro;
        Routine introSide, introMove;   // Niko's scale-in and the settle blend: cancelled by Skip and IntroReset

        // Captured when an intro starts, restored by the end block and IntroReset (lights, their poses, the dolly lens, the skull rim).
        bool introStored, gradeOn;
        float dollyNear0 = -1, skullRim0 = .25f; Color skullRimColour0 = new(.55f, .8f, 1f), skullColour0 = new(.88f, .85f, .78f);
        readonly Light[] movedLights = new Light[4]; readonly Vector3[] lightPos = new Vector3[4]; readonly Quaternion[] lightRot = new Quaternion[4];
        // IntroProfile overrides (runtime instance) and the lab grade they sit over while the intro volume is on.
        ColorAdjustments fxGrade; Vignette fxVignette; ChromaticAberration fxAberration; Bloom fxBloom; DepthOfField fxDof;
        float labExposure, labContrast, labSaturation, labVignette = .18f, labBloom = .14f;
        // The subject, measured when the intro starts (after turning to the gate): eyes, facing, screen-right, heights.
        Vector3 iEye, iFace, iRight, iMid, iCentre, iPresent; float iR = .016f, iSide = 1, iTop, iFloor, iOrbit = 1;
        Quaternion iEyeRot, iPresentRot;
        CamKey[][] introPath;   // continuous camera segments, a new segment is a cut
        Vector3 xrayEndPos, xrayEndLook, presentPos, presentLook;

        struct CamKey { public float t; public Vector3 pos, look; public float fov; public CamKey(float t, Vector3 pos, Vector3 look, float fov) { this.t = t; this.pos = pos; this.look = look; this.fov = fov; } }

        public void SkipIntro() { if (CinematicPlaying && !Clock.Paused) skipIntro = true; }

        void Room(float k) { for (int i = 0; i < roomLights.Length; i++) roomLights[i].intensity = roomBase[i] * k; }
        /// Exposure over the lab grade; outside the cinematic grade the intro volume is on only while the picture is pulled down.
        void Exposure(float ev)
        {
            if (!introVolume) return;
            if (!fxGrade) BindGrade();
            Look(ev, labVignette, 0, 0, 0, 1, 0);
            if (!gradeOn) introVolume.weight = ev < 0 ? 1 : 0;
        }
        static float Ease(float t, float from, float to) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - from) / (to - from)));

        /// Intro clock → reference time τ. Reference match: τ = 6 + t. Production: each beat's reference span squeezed into its
        /// production length, the rate blended across beat boundaries (monotone cubic, Fritsch–Butland) so a shortened hold
        /// never jerks the dolly.
        float RefTime(float t)
        {
            if (timing == CinematicTiming.ReferenceMatch) return Mathf.Min(IntroBeats[0].at + t, IntroBeats[^1].at);
            int n = IntroBeats.Length, i = 0; float x0 = 0;
            for (; i < n - 2 && t >= x0 + IntroBeats[i].prod; i++) x0 += IntroBeats[i].prod;
            float h = IntroBeats[i].prod, u = Mathf.Clamp01((t - x0) / h), y0 = IntroBeats[i].at, y1 = IntroBeats[i + 1].at;
            float Slope(int k) => (IntroBeats[k + 1].at - IntroBeats[k].at) / IntroBeats[k].prod;
            float Tangent(int k) { if (k == 0) return Slope(0); if (k >= n - 1) return Slope(n - 2); float a = Slope(k - 1), b = Slope(k); return 2 * a * b / (a + b); }
            float m0 = Tangent(i) * h, m1 = Tangent(i + 1) * h, u2 = u * u, u3 = u2 * u;
            return (2 * u3 - 3 * u2 + 1) * y0 + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * y1 + (u3 - u2) * m1;
        }

        /// The dolly starts every entrance from the same corridor view (it is moved by code during the intro).
        bool ResetDolly()
        {
            var dolly = cams.Get("S02_IntroDolly"); if (!dolly) return false;
            dolly.transform.SetPositionAndRotation(CamStart, Quaternion.LookRotation(GateLook - CamStart));
            var lens = dolly.Lens; lens.FieldOfView = cams.Fitted(DollyFov); if (dollyNear0 > 0) lens.NearClipPlane = dollyNear0; dolly.Lens = lens;
            return true;
        }

        /// Chapter entry, restart and menu all pass through here, so a cancelled intro never leaves the room dark.
        void IntroReset()
        {
            sfx.EndIntroMusic(true);
            RestoreIntroState();
            sfx.sfx.Stop(); sfx.StopLoops();   // the interface and cinematic channels keep their last cue (a click that started this chapter)
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            eyeRig.gameObject.SetActive(true);
            if (roomBase != null) Room(1);
            keyLight.intensity = keyIntensity;
            IntroCaption = null; IntroTime = 0; ClearTitle(); spin = false;
            Exposure(0); sfx.musicGain = 1; IntroPlaying = EyeBrainPlaying = skipIntro = false; eyeBrainBeat = -1; signals.widthScale = 1;
            foreach (var b in beamRenderers) b.enabled = true;
        }

        IEnumerator Intro()
        {
            IntroPlaying = true; skipIntro = false; IntroTime = 0; hud.Refresh();   // only the letterbox (caption + Skip) is shown from here
            CaptureIntroState(); BindGrade();
            var anchor = Anchor("Intro");
            // Common start: black frame, dark hall, beams off, music out, Niko's spot at his anchor (he waits for his first line), both eyes closed in their sockets.
            hud.Fade(1); Room(.08f); keyLight.intensity = 0; displayLight.intensity = 0;
            foreach (var b in beamRenderers) b.enabled = false;
            human.ResetVisual(true, 1);
            niko.Place(anchor);
            ReturnEye();
            FrameIntro();
            sfx.SetMusic("start", 1.2f);   // the start screen's theme carries on through the gate (Stage.Opening switches at the face hold)
            gateCrossed = false; revealHeard = AnatomyRevealController.RevealState.Skin;
            var dolly = cams.Get("S02_IntroDolly");
            if (Short || !dolly) yield return IntroShort(dolly, anchor);
            else if (timing == CinematicTiming.Production) yield return Opening(dolly, anchor);
            else yield return IntroFull(dolly, anchor);
            IntroEnd(anchor);
        }

        IEnumerator IntroFull(CinemachineCamera dolly, Transform anchor)
        {
            if (ResetDolly()) cams.Snap("S02_IntroDolly");
            if (entrance) { entrance.SetGate(0); entrance.SetAccents(0); }
            GradeOn(true);
            var beats = new (float t, Action a)[]
            {
                (6.1f, () => sfx.Play("drone", .5f)),
                (TCutGateLow, () => sfx.Play("softwhoosh", .3f)),
                (12.7f, () => sfx.Play("clunk1", .8f)),
                (12.8f, () => { sfx.Play("gatelift", .8f); Burst(gateSparks, new Vector3(GateLook.x - .3f, .1f, GateLook.z), 40); }),
                (13.3f, () => Burst(gateSparks, new Vector3(GateLook.x - .3f, .6f, GateLook.z), 25)),
                (13.9f, () => Particles(gateDust, true, new Vector3(GateLook.x - .1f, 2.6f, GateLook.z))),
                (14.6f, () => sfx.Play("labreveal", .7f)),
                (21f, () => StopEmitting(gateDust)),
                (22.2f, () => sfx.Play("softwhoosh", .45f)),
                (22.9f, () => sfx.Play("lightsweep", .5f)),
                (24.5f, () => Particles(depthDust, true, new Vector3(iCentre.x, iMid.y - .7f, iCentre.z))),
                (30.2f, () => sfx.Play("lightsweep", .3f)),
                (33.7f, () => Burst(nikoSparkle, ExhibitSink + Vector3.up * .1f, 36)),   // the display has folded into the stand
                (41f, () => sfx.Play("approach", .45f)),
                (49f, () => StopEmitting(depthDust)),
                (50.4f, () => sfx.Play("wipe", .7f)),
                (51.3f, () => sfx.Play("irisglow", .35f)),
                (54.4f, () => sfx.Play("wipe", .55f)),
                (60.4f, () => sfx.Play("xray", .7f)),
                (TExtract + .1f, () => { sfx.Play("extract", .8f); Particles(extractMotes, true, iEye); }),
                (71.2f, () => { sfx.Play("lightsweep", .35f); Burst(nikoSparkle, iPresent, 40); }),   // the eye settles on the display
                (72f, () => spin = true),
                (76f, () => { sfx.Play("titlehit", .9f); Burst(titleBurst, iPresent + Vector3.down * .25f, 160); }),
                (80.5f, () => StopEmitting(extractMotes)),
                (TCutNiko, () => NikoCut()),   // his shot stays empty: he arrives with his first line (NikoArrive)
                (93.5f, () => { if (cams.Has(SettleShot)) introMove = runner.RunSide(cams.MoveTo(SettleShot, Mathf.Min(1.6f, IntroLength - IntroTime))); }),
            };
            int next = 0;
            for (float t = 0; t < IntroLength && !skipIntro; t += Clock.Delta)
            {
                IntroTime = t;
                float tau = RefTime(t);
                while (next < beats.Length && tau >= beats[next].t) beats[next++].a();
                IntroFrame(dolly, tau);
                yield return null;
            }
            if (skipIntro) { sfx.StopEffects(); sfx.Play("skip", .6f); }
        }

        /// One frame of the opening at reference time τ: dolly, gate, lights, layers, grade, caption and title.
        void IntroFrame(CinemachineCamera dolly, float tau)
        {
            // ---- camera
            if (tau < TExtract || tau >= TCutNiko) { var k = Pose(tau); Dolly(dolly, k.pos, k.look, k.fov, .03f); }
            else
            {
                // the selected eye leaves its orbit and flies to the central display; the camera cranes up and round with it
                // like the reference's brain: it rises out of the head first (growing fast), then glides to the display
                float u = Ease(tau, TExtract, 72f), grow = 1 - (1 - u) * (1 - u), r0 = iR * AnatomicalEye;
                var eyeAt = Bezier(iEye, iEye + Vector3.up * .45f + iFace * .05f, iPresent + Vector3.up * .55f, iPresent, grow);
                eyeRig.SetPositionAndRotation(eyeAt, Quaternion.Slerp(iEyeRot, iPresentRot, grow));
                eyeRig.localScale = Vector3.one * (r0 * Mathf.Pow(heroPoint.lossyScale.x / r0, grow));
                if (extractMotes && extractMotes.gameObject.activeSelf) extractMotes.transform.position = eyeAt;
                var away = (xrayEndPos - iCentre).normalized;
                var pos = Bezier(xrayEndPos, xrayEndPos + Vector3.up * .8f - away * .1f, presentPos + Vector3.up * .65f, presentPos, u);
                var look = Vector3.Lerp(xrayEndLook, eyeAt + Vector3.up * (.1f * eyeRig.localScale.x / heroPoint.lossyScale.x), Ease(tau, TExtract, TExtract + .8f));
                if (tau >= 72f) { float hold = Ease(tau, 72f, TCutNiko); pos = presentPos + (presentLook - presentPos).normalized * (.25f * hold); look = presentLook; }   // float and title: a slow push
                Dolly(dolly, pos, look, 40, .03f);
            }
            Exhibit(1 - Ease(tau, 30f, 34f));   // the lab's exhibit eye folds into the stand while the camera travels to the subject
            // ---- the gate, the hall and the lights
            if (entrance) { entrance.SetGate(Ease(tau, 12.7f, 14.7f)); entrance.SetAccents(Ease(tau, 12.7f, 13.6f)); }
            float lit = Ease(tau, 22.4f, 23.6f);
            Room(tau < TCutGateLift ? .08f : Mathf.Lerp(.3f, 1, lit) * (tau >= 49f && tau < TCutNiko ? .75f : 1));
            keyLight.intensity = keyIntensity * lit;
            displayLight.intensity = displayIntensity * Mathf.Max(.5f * Ease(tau, 14.5f, 16f), lit);
            sfx.musicGain = Mathf.Lerp(.25f, .8f, Ease(tau, 14.5f, 23f));
            if (faceKey)
            {
                var at = iMid + iFace * .9f - iRight * .45f + Vector3.up * .5f;
                faceKey.enabled = tau >= 41f && tau < TExtract + 1.5f;
                faceKey.transform.SetPositionAndRotation(at, Quaternion.LookRotation(iMid - at));
                faceKey.intensity = faceKeyBase * .7f * Ease(tau, 41f, 47f) * (1 - Ease(tau, TExtract, TExtract + 1.5f));
            }
            if (eyeKey)
            {
                float toPresent = Ease(tau, TExtract, 72f);
                var at = Vector3.Lerp(iEye + iFace * .75f - iRight * .35f + Vector3.up * .45f, iPresent + iFace * 1.6f - iRight * 1.1f + Vector3.up * 1.2f, toPresent);
                var target = Vector3.Lerp(iEye, iPresent, toPresent);
                eyeKey.enabled = tau >= 60.5f && tau < TCutNiko;
                eyeKey.transform.SetPositionAndRotation(at, Quaternion.LookRotation(target - at));
                eyeKey.intensity = eyeKeyBase * Ease(tau, 61f, 63f) * Mathf.Lerp(.6f, 1.1f, toPresent);
            }
            if (eyeRim)
            {
                var at = iPresent - iFace * 1.3f + iRight * .9f + Vector3.up * 1.0f;
                eyeRim.enabled = tau >= 70f && tau < TCutNiko;
                eyeRim.transform.SetPositionAndRotation(at, Quaternion.LookRotation(iPresent - at));
                eyeRim.intensity = eyeRimBase * 1.3f * Ease(tau, 70f, 72.5f);
            }
            // ---- anatomy layers: skin → muscle → bone → glass, every frame from τ (Pause- and Skip-safe)
            if (tau < TCutNiko) Layers(tau);
            // ---- fade, grade, caption, title
            hud.Fade(1 - Ease(tau, 6f, 6.7f));
            float subject = tau >= 49f && tau < TExtract ? Vector3.Distance(dolly.transform.position, iMid) + .25f : tau >= 72f && tau < TCutNiko ? Vector3.Distance(dolly.transform.position, iPresent) + .7f : 0;
            Look(tau >= 41f && tau < TExtract ? -.25f * Ease(tau, 41f, 45f) : 0, tau >= 49f && tau < TCutNiko ? .34f : .26f, 0, 0, 0, 1, subject);   // the white shirt never blows out in the close-ups
            IntroCaption = tau >= 36f && tau < 40.6f ? "Every second, your eyes are taking in the world."
                         : tau >= 41f && tau < 48.5f ? "But how do you actually see it?"
                         : tau >= 55.5f && tau < 60.2f ? "Protected inside your skull…"
                         : tau >= 61f && tau < 67.2f ? "…sit two of the most remarkable organs in your body. Your eyes." : null;
            if (tau >= 76f && tau < TCutNiko) SetTitle(IntroTitle[0], IntroTitle[1], Ease(tau, 76.2f, 78.6f), Ease(tau, 79.4f, 80.4f) * (1 - Ease(tau, 84f, TCutNiko)));
            else ClearTitle();
        }

        /// Layer state at τ. The wipe front runs from the crown to the collar line (the floor); the shirt below stays.
        void Layers(float tau)
        {
            var h = human;
            float W(float from, float to) => Mathf.Lerp(iTop, iFloor, Ease(tau, from, to));
            h.skin.SetAlpha(1, true); h.garment.SetAlpha(1, true);
            if (tau < 50.5f) { h.skin.ClearWipe(); Hide(h.muscles, h.orbicularis, h.skull, h.eyeMuscles, h.pathway[0], h.pathway[1]); return; }
            h.skull.SetColor("_BaseColor", Bone);
            if (tau < TExtract)   // under the skin line the eyes settle to anatomical size, deeper in their orbits
            {
                var size = Vector3.one * (iR * Mathf.Lerp(1, AnatomicalEye, Ease(tau, 50.6f, 51.2f)));
                eyeRig.localScale = size; otherEye.transform.localScale = size;
            }
            h.skin.SetWipe(W(50.5f, 51.2f), 0, 1);
            if (tau < 54.5f)
            {
                float front = W(50.5f, 51.2f);
                foreach (var p in new[] { h.muscles, h.orbicularis, h.skull }) { p.SetAlpha(1, true); p.SetWipe(front, 1, 0, iFloor); }
                h.orbicularis.SetGlow(.5f * Ease(tau, 51.3f, 52f) * (1 - Ease(tau, 53.2f, 54.4f)), true);
                Hide(h.eyeMuscles, h.pathway[0], h.pathway[1]);
            }
            else if (tau < 60.5f)
            {
                float front = W(54.5f, 55.1f);
                foreach (var p in new[] { h.muscles, h.orbicularis }) { p.SetAlpha(1, true); p.SetWipe(front, 0, 1, iFloor); p.SetGlow(0, true); }
                h.skull.SetAlpha(1, true); h.skull.SetWipe(iFloor, 1, 0, iFloor);
                Hide(h.eyeMuscles, h.pathway[0], h.pathway[1]);
            }
            else
            {
                Hide(h.muscles, h.orbicularis);
                float gone = 1 - Ease(tau, TExtract + .5f, TExtract + 2.5f);   // the head fades as the eye leaves
                h.skull.SetAlpha(gone, true); h.skull.SetWipe(W(60.5f, 61.5f), XrayAlpha, 1, iFloor);
                GlassSkull(Ease(tau, 60.5f, 61.5f));
                h.eyeMuscles.SetAlpha(Ease(tau, 61f, 62.4f) * gone, true);
                h.pathway[0].SetAlpha(Ease(tau, 61.4f, 62.8f) * gone, true);   // optic nerves
                h.pathway[1].SetAlpha(Ease(tau, 61.8f, 63.2f) * gone, true);   // chiasm
                h.skin.SetAlpha(gone, true); h.garment.SetAlpha(gone, true);
                foreach (var p in otherEye.parts) p.SetAlpha(gone, true);
            }
        }

        static void Hide(params AnatomyPart[] parts) { foreach (var p in parts) if (p) { p.SetAlpha(0, true); p.ClearWipe(); p.SetGlow(0, true); } }

        /// Behind the cut to Niko: the learner's eye rests on the display (hero point), the subject and the exhibit are gone.
        void NikoCut(bool sound = true)
        {
            spin = false; ClearTitle();
            Particles(extractMotes, false); Particles(depthDust, false);
            RestoreSkull(); human.ResetVisual(false);
            otherEye.gameObject.SetActive(false);
            Exhibit(0);
            PlaceEye(heroPoint); eye.SetCutInstant(false); eye.SetPupil(EyeGeometry.PupilDefault, true);
            var rim = eye.Get("pupil"); if (rim) rim.SetGlow(0);
            if (sound) sfx.Play("softwhoosh", .4f);
        }

        Vector3 exhibitPos, exhibitScale; bool exhibitStored;
        /// Where the exhibit eye folds away: the centre of the stand's top, under the display point.
        Vector3 ExhibitSink => heroPoint.position + Vector3.down * 1.15f;
        /// The lab's exhibit eye: 1 in place, 0 gone; in between it shrinks down into the stand, leaving the display free for
        /// the learner's eye. Not batching-static (the visual-match patch clears the flag), so it can move in the player.
        void Exhibit(float k)
        {
            if (!displayEye) return;
            if (!exhibitStored) { exhibitPos = displayEye.position; exhibitScale = displayEye.localScale; exhibitStored = true; }
            displayEye.gameObject.SetActive(k > .001f);
            displayEye.position = Vector3.Lerp(ExhibitSink, exhibitPos, k);
            displayEye.localScale = exhibitScale * Mathf.Max(k * k, .001f);
        }

        /// Camera pose on the authored path at τ (non-uniform Catmull–Rom per segment: velocity is continuous inside a
        /// segment and zero at its ends; a new segment is a cut).
        CamKey Pose(float tau)
        {
            var seg = introPath[0];
            foreach (var s in introPath) if (tau >= s[0].t) seg = s;
            int n = seg.Length;
            if (tau <= seg[0].t || n == 1) return seg[0];
            if (tau >= seg[n - 1].t) return seg[n - 1];
            int i = 0; while (i < n - 2 && tau >= seg[i + 1].t) i++;
            CamKey K(int j) => j < 0 ? Phantom(seg[0], seg[1]) : j >= n ? Phantom(seg[n - 1], seg[n - 2]) : seg[j];
            CamKey a = K(i - 1), b = K(i), c = K(i + 1), d = K(i + 2);
            var p = CatmullRom(a.pos, b.pos, c.pos, d.pos, a.t, b.t, c.t, d.t, tau);
            var l = CatmullRom(a.look, b.look, c.look, d.look, a.t, b.t, c.t, d.t, tau);
            var f = CatmullRom(Vector3.one * a.fov, Vector3.one * b.fov, Vector3.one * c.fov, Vector3.one * d.fov, a.t, b.t, c.t, d.t, tau).x;
            return new CamKey(tau, p, l, f);
        }
        /// A mirrored knot past the end of a segment that repeats the neighbour: zero velocity at the end.
        static CamKey Phantom(CamKey end, CamKey neighbour) => new(2 * end.t - neighbour.t, neighbour.pos, neighbour.look, neighbour.fov);

        /// Barry–Goldman form of the Catmull–Rom spline with the key times as knots.
        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1, float t2, float t3, float t)
        {
            Vector3 L(Vector3 a, Vector3 b, float ta, float tb) => tb - ta < 1e-5f ? a : (tb - t) / (tb - ta) * a + (t - ta) / (tb - ta) * b;
            var a1 = L(p0, p1, t0, t1); var a2 = L(p1, p2, t1, t2); var a3 = L(p2, p3, t2, t3);
            return L(L(a1, a2, t0, t2), L(a2, a3, t1, t3), t1, t2);
        }
        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) { float s = 1 - t; return s * s * s * a + 3 * s * s * t * b + 3 * s * t * t * c + t * t * t * d; }

        /// Reduced motion: four stills through black (the lab from the door, the glass skull with the eyes in their orbits, the
        /// floating eye under its title, Niko), no travel, no particles; ≤ 8 s.
        IEnumerator IntroShort(CinemachineCamera dolly, Transform anchor)
        {
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            if (dolly) cams.Snap("S02_IntroDolly");
            bool production = timing == CinematicTiming.Production;
            if (production) BuildOpeningPath();
            sfx.SetMusic("transition", 1);   // reduced motion: the layer stills on the transition bed, the lab bed for Niko
            var stills = production ? new[] { (15f, 1.5f), (19f, 1.4f), (24.7f, 1.6f), (29.5f, 1.5f) }
                : new[] { (29.5f, 1.6f), (64f, 1.8f), (80.5f, 2.4f) };
            foreach (var (tau, seconds) in stills)
            {
                if (dolly) { if (production) OpeningFrame(dolly, tau); else IntroFrame(dolly, tau); }
                spin = false; LabLevels(); GradeOn(true);
                yield return Still(seconds);
                if (skipIntro) yield break;
            }
            NikoCut(); IntroCaption = null;
            RestoreIntroState(); LabLevels();
            cams.Snap(SettleShot); sfx.SetMusic("lab", 1.5f);
            niko.Place(anchor);   // his spot, empty until his first line
            yield return Still(1.5f, false);
        }

        /// A still: fade up over .5 s, hold, fade down over the last .5 s (intro clock, so Pause holds it and Skip ends it).
        IEnumerator Still(float seconds, bool fadeOut = true)
        {
            for (float t = 0; t < seconds && !skipIntro; t += Clock.Delta)
            {
                IntroTime += Clock.Delta;
                hud.Fade(Mathf.Max(1 - Ease(t, 0, .5f), fadeOut ? Ease(t, seconds - .5f, seconds) : 0));
                yield return null;
            }
        }

        /// The finished state every path ends in: lit lab, the learner's eye on the display, the subject gone, Niko at his
        /// Intro anchor on the first lesson shot.
        void IntroEnd(Transform anchor)
        {
            bed = "lab"; sfx.SetMusic("lab", 1.2f); sfx.EndIntroMusic();   // every path (full, skipped, reduced) lands on the lab bed
            NikoCut(!Short && !skipIntro);   // reduced motion already cut to Niko (and sounded it); Skip has its own sound
            RestoreIntroState(); LabLevels();
            cams.Snap(SettleShot);
            if (entrance) { entrance.SetGate(1); entrance.SetAccents(1); }
            Exposure(0); sfx.musicGain = 1; hud.Fade(0);
            niko.Place(anchor); niko.gameObject.SetActive(!NikoWaiting); niko.lookTarget = lookCamera;
            IntroPlaying = false; IntroCaption = null; ClearTitle(); IntroTime = IntroLength; hud.Refresh();   // the main interface fades in from here
        }

        // ---------- Niko's arrival ----------
        /// Niko first appears when he first speaks (21 October 2026 brief). The start screen and the opening are without him;
        /// his first line (S02_L01 after the opening, or the first line after Continue or a chapter picked on the menu) brings
        /// him in: he grows from nothing at his spot with a slight overshoot while cyan sparkles spiral up round him and a ring
        /// sweeps out from his feet (floating beside the lens, he fades in there inside the same burst). Reduced motion: he is
        /// simply there, with the chime.
        public bool NikoWaiting { get; private set; }
        void NikoWait() { NikoWaiting = true; niko.gameObject.SetActive(false); }

        void NikoArrive()
        {
            NikoWaiting = false;
            if (Companion && !Short) Companion.Grow = 0;   // before the companion draws him this frame: no full-size flash
            niko.gameObject.SetActive(true); niko.lookTarget = lookCamera;
            sfx.Play("nikoappear", .7f);
            StartCoroutine(Arrival());
        }

        IEnumerator Arrival()
        {
            yield return null;   // the companion has placed him: at his spot, or floating beside the lens
            if (Short || NikoWaiting || !Companion) { if (Companion) Companion.Grow = 1; yield break; }
            SparkleBurst(niko.transform, Companion.height * Companion.Size);
            for (float t = 0; t < 1 && !NikoWaiting;)
            {
                t = Mathf.Min(1, t + Clock.Delta / .55f);
                float u = t - 1; Companion.Grow = 1 + 2.2f * u * u * u + 1.2f * u * u;   // ease out with a slight overshoot
                yield return null;
            }
            Companion.Grow = 1;
        }

        static readonly Color32 SparkCyan = new(38, 198, 245, 255), SparkWhite = new(235, 250, 255, 255);

        /// The arrival burst on nikoSparkle (a world-space soft glow), sized to his height `tall` and turning about his up axis:
        /// sparkles spiral from his feet to his crown, a ring sweeps out from his feet, and a soft flash at his chest.
        void SparkleBurst(Transform at, float tall)
        {
            var ps = nikoSparkle; if (!ps) return;
            Particles(ps, true);
            ps.transform.SetPositionAndRotation(at.position, at.rotation);
            var main = ps.main; main.maxParticles = Mathf.Max(main.maxParticles, 256);
            var swirl = ps.velocityOverLifetime; swirl.enabled = true; swirl.space = ParticleSystemSimulationSpace.Local;
            swirl.x = swirl.y = swirl.z = new ParticleSystem.MinMaxCurve(0);
            swirl.orbitalX = swirl.orbitalZ = new ParticleSystem.MinMaxCurve(0); swirl.orbitalY = new ParticleSystem.MinMaxCurve(4);
            swirl.radial = new ParticleSystem.MinMaxCurve(.12f * tall);
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            Vector3 up = at.up, side = at.right, front = at.forward;
            for (int i = 0; i < 120; i++)
            {
                float a = i * 2.4f, h = (i + UnityEngine.Random.value) / 120;   // golden-angle turns, feet to crown
                var radial = side * Mathf.Cos(a) + front * Mathf.Sin(a);
                e.position = at.position + up * (h * tall * 1.05f) + radial * (tall * Mathf.Lerp(.4f, .24f, h));
                e.velocity = up * (tall * UnityEngine.Random.Range(.3f, .6f));
                e.startSize = tall * UnityEngine.Random.Range(.06f, .11f); e.startLifetime = UnityEngine.Random.Range(1f, 1.6f);
                e.startColor = i % 3 == 0 ? SparkWhite : SparkCyan;
                ps.Emit(e, 1);
            }
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI / 24; var radial = side * Mathf.Cos(a) + front * Mathf.Sin(a);
                e.position = at.position + up * (.03f * tall) + radial * (.12f * tall);
                e.velocity = radial * (1.4f * tall) + up * (.15f * tall);
                e.startSize = .07f * tall; e.startLifetime = .8f; e.startColor = SparkCyan;
                ps.Emit(e, 1);
            }
            e.position = at.position + up * (.45f * tall); e.velocity = Vector3.zero;
            e.startSize = 1.3f * tall; e.startLifetime = .6f; e.startColor = new Color32(120, 220, 255, 140);
            ps.Emit(e, 3);
        }

        /// Closed eye back in the selected socket, resting pupil, no highlights.
        void ReturnEye()
        {
            Socket(human.eyeRight, eyeRig);
            eye.transform.localPosition = Vector3.zero; eye.transform.localRotation = Quaternion.identity; eye.transform.localScale = Vector3.one;
            eye.SetCutInstant(false);
            eye.SetPupil(EyeGeometry.PupilDefault, true);
            var rim = eye.Get("pupil"); if (rim) rim.SetGlow(0);
        }

        void LabLevels()
        {
            Room(1); keyLight.intensity = keyIntensity; displayLight.intensity = displayIntensity;
            foreach (var b in beamRenderers) b.enabled = true;
            GradeOn(false);
        }

        void SetTitle(string title, string subtitle, float letters, float subtitleAlpha, float alpha = 1) { CineTitle = title; CineSubtitle = subtitle; CineLetters = letters; CineSubtitleAlpha = subtitleAlpha; CineTitleAlpha = alpha; }
        void ClearTitle() { CineTitle = CineSubtitle = null; CineLetters = CineSubtitleAlpha = 0; CineTitleAlpha = 0; }

        static void Burst(ParticleSystem ps, Vector3 at, int count) { if (!ps) return; Particles(ps, true, at); ps.Emit(count); }
        static void StopEmitting(ParticleSystem ps) { if (ps && ps.gameObject.activeSelf) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }

        // ---------- state capture / restore ----------
        void CaptureIntroState()
        {
            if (introStored) return;
            introStored = true;
            var dolly = cams.Get("S02_IntroDolly"); if (dolly) dollyNear0 = dolly.Lens.NearClipPlane;
            movedLights[0] = faceKey; movedLights[1] = eyeKey; movedLights[2] = eyeRim; movedLights[3] = eyeFill;
            for (int i = 0; i < movedLights.Length; i++) if (movedLights[i]) { lightPos[i] = movedLights[i].transform.position; lightRot[i] = movedLights[i].transform.rotation; }
            var skullMat = human.skull.main.Length > 0 && human.skull.main[0] ? human.skull.main[0].sharedMaterial : null;
            if (skullMat) { skullRim0 = skullMat.GetFloat("_Rim"); skullRimColour0 = skullMat.GetColor("_RimColor"); skullColour0 = skullMat.GetColor("_BaseColor"); }
            foreach (var p in human.Layers) if (p) p.SetFloat("_WipeEdge", WipeEdge);
        }

        /// Everything the opening ramps, moves or switches on goes back to its authored state (end block, Skip, IntroReset).
        void RestoreIntroState()
        {
            introSide?.Cancel(); introSide = null; introMove?.Cancel(); introMove = null;
            if (introStored)
            {
                RestoreLighting();
                for (int i = 0; i < movedLights.Length; i++) if (movedLights[i]) movedLights[i].transform.SetPositionAndRotation(lightPos[i], lightRot[i]);
                RestoreSkull();
                var dolly = cams.Get("S02_IntroDolly");
                if (dolly && dollyNear0 > 0) { var lens = dolly.Lens; lens.NearClipPlane = dollyNear0; dolly.Lens = lens; }
                introStored = false;
            }
            foreach (var ps in new[] { gateSparks, gateDust, extractMotes, titleBurst, nikoVortex, depthDust, nikoSparkle, anatomyTransition, anatomyMotes }) Particles(ps, false);
            if (human && human.reveal) human.reveal.ResetVisual();   // clears the breakup field, motes and haze deterministically
            GradeOn(false);
        }

        void RestoreSkull() { if (!human || !human.skull) return; human.skull.SetFloat("_Rim", skullRim0); human.skull.SetFloat("_Glass", 0); human.skull.SetColor("_RimColor", skullRimColour0); human.skull.SetColor("_BaseColor", skullColour0); }
        /// Bone turned to glass: a clear body under bright fresnel edges (the clear-medium mode keeps highlights at full strength).
        void GlassSkull(float k)
        {
            human.skull.SetFloat("_Glass", k > 0 ? 1 : 0); human.skull.SetFloat("_Rim", Mathf.Lerp(skullRim0, .9f, k));
            human.skull.SetColor("_RimColor", Color.Lerp(skullRimColour0, GlassRim, k));
        }

        /// Turns the subject toward the gate and measures it once per intro: eyes, facing, screen-right, the crown and the
        /// collar line for the wipes, the presentation point; then authors the camera path from those.
        void FrameIntro()
        {
            var h = human.transform;
            var face = human.eyeRight.forward; face.y = 0;
            var toGate = new Vector3(GateLook.x, h.position.y, GateLook.z) - h.position;
            if (face.sqrMagnitude > 1e-6f && toGate.sqrMagnitude > 1e-6f) h.rotation = Quaternion.FromToRotation(face.normalized, toGate.normalized) * h.rotation;
            Socket(human.eyeRight, eyeRig); Socket(human.eyeLeft, otherEye.transform);
            iEye = eyeRig.position; iR = Mathf.Max(.005f, eyeRig.lossyScale.x); iEyeRot = eyeRig.rotation;
            iFace = eyeRig.forward; iFace.y = 0; if (iFace.sqrMagnitude < 1e-6f) iFace = Vector3.left; iFace.Normalize();
            iRight = Vector3.Cross(Vector3.up, -iFace).normalized;
            iMid = (human.eyeLeft.position + human.eyeRight.position) * .5f;
            iSide = Vector3.Dot(iEye - iMid, iRight) < 0 ? -1 : 1;
            iCentre = new Vector3(h.position.x, iMid.y, h.position.z);
            iTop = h.position.y + human.headTop * h.lossyScale.y + .02f; iFloor = h.position.y + human.neckY * h.lossyScale.y + .004f;   // the collar seam: the shirt is never cut
            float floorY = h.position.y;   // the platform top under the subject's feet
            iPresent = heroPoint.position; iPresentRot = heroPoint.rotation;   // the central display, in the eye's lesson pose
            iOrbit = Vector3.Dot(Quaternion.AngleAxis(10, Vector3.up) * iFace - iFace, iEye - iMid) > 0 ? 1 : -1;   // the orbit turns toward the selected eye

            Vector3 F(float d, float y) => new Vector3(iMid.x, y, iMid.z) + iFace * d;   // d metres in front of the eyes, at height y
            Vector3 L(float y) => new(iCentre.x, y, iCentre.z);
            float E = iMid.y, gx = GateLook.x, gz = GateLook.z;
            Vector3 Orbit(float deg, float d, float y) => iCentre - iFace * .05f + Quaternion.AngleAxis(deg * iOrbit, Vector3.up) * (iFace * (d + .05f)) + Vector3.up * (y - iMid.y);
            var focus = Vector3.Lerp(iMid, iEye, .5f);
            xrayEndPos = Orbit(34, .76f, E - .04f); xrayEndLook = focus;
            // REF_07's hero: straight in from the stand's front (world -Z, the "THE EYE" side), a little below the eye, so the
            // info panels frame the eye at the edges and the title reads over the dome rather than over a panel
            presentPos = iPresent + new Vector3(0, -.25f, -4f); presentLook = iPresent + Vector3.up * .3f;
            float gd = Vector3.Dot(new Vector3(gx, iMid.y, gz) - iMid, iFace);   // eyes to the gate along the facing
            introPath = new[]
            {
                new[] { new CamKey(6f, CamStart, GateLook, 50), new CamKey(TCutGateLow, new Vector3(-27.3f, 2f, gz), new Vector3(gx, 3.35f, gz), 50) },
                new[] { new CamKey(TCutGateLow, new Vector3(-19.25f, .85f, gz - .62f), new Vector3(gx, 4.6f, gz + .28f), 52), new CamKey(TCutGateLift, new Vector3(-19.15f, .9f, gz - .12f), new Vector3(gx, 4.7f, gz + .43f), 52) },
                new[]
                {
                    new CamKey(TCutGateLift, new Vector3(-25f, 1.85f, gz), new Vector3(gx, 3.3f, gz), 50),
                    new CamKey(14.5f, new Vector3(-23.9f, 1.85f, gz), new Vector3(gx, 3.1f, gz), 50),
                    new CamKey(18f, new Vector3(-21.4f, 1.95f, gz), new Vector3(gx, 2.7f, gz), 48),
                    new CamKey(22f, new Vector3(-19.4f, 2.1f, gz), L(floorY + 1.1f), 46),
                    new CamKey(24f, F(Mathf.Min(14.6f, gd - 2.4f), floorY + 1.45f), L(floorY + .95f), 44),
                    new CamKey(29.5f, F(Mathf.Min(9.2f, gd * .58f), floorY + 1.3f), L(floorY + .92f), 42),
                    new CamKey(35f, F(4.5f, floorY + 1.08f), L(floorY + .9f), 40),
                    new CamKey(41f, F(3.3f, floorY + 1.12f), L(floorY + .98f), 40),
                    new CamKey(43f, F(2.2f, E - .26f), L(E - .3f), 40),
                    new CamKey(45f, F(1.1f, E - .05f), L(E - .08f), 40),
                    new CamKey(47f, F(.84f, E - .02f), L(E - .05f), 40),
                    new CamKey(49f, F(.78f, E - .01f), L(E - .045f), 40),
                    new CamKey(54.5f, F(.76f, E - .01f), L(E - .035f), 40),
                    new CamKey(60.5f, F(.74f, E), L(E - .025f), 40),
                    new CamKey(62.5f, Orbit(10, .76f, E - .01f), Vector3.Lerp(L(E - .02f), focus, .3f), 40),
                    new CamKey(65f, Orbit(24, .76f, E - .03f), Vector3.Lerp(L(E - .02f), focus, .7f), 40),
                    new CamKey(TExtract, xrayEndPos, xrayEndLook, 40),
                },
                new[] { new CamKey(TCutNiko, NikoCam(0), NikoMid(), 41), new CamKey(93.5f, NikoCam(.2f), NikoMid(), 41) },
            };
        }

        /// Niko's materialisation shot: from the front of the stand, Niko at the left and the learner's eye on its display at the
        /// right (REF_01), with a slow push.
        Vector3 NikoCam(float push)
        {
            var mid = NikoMid(); var toward = heroPoint.forward; toward.y = 0; toward.Normalize();   // the eye's front, toward the lab's front
            return mid + toward * (3.3f - push) + Vector3.up * .22f;
        }
        Vector3 NikoLook() => Anchor("Intro").position + Vector3.up * .72f;
        Vector3 NikoMid() => Vector3.Lerp(NikoLook(), heroPoint.position, .54f);

        void Dolly(CinemachineCamera dolly, Vector3 pos, Vector3 look, float fov, float near)
        {
            dolly.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            var lens = dolly.Lens; lens.FieldOfView = cams.Fitted(fov); lens.NearClipPlane = near; dolly.Lens = lens;
        }

        /// Particle systems live under the observatory group (Stage pauses them); they are switched on only for their beat.
        static void Particles(ParticleSystem ps, bool on, Vector3? at = null)
        {
            if (!ps) return;
            if (!on)
            {
                if (ps.gameObject.activeSelf) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); }
                return;
            }
            if (at.HasValue) ps.transform.position = at.Value;
            if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
            if (!ps.isPlaying) ps.Play(true);
        }

        // ---------- the intro grade (IntroProfile overrides over the lab grade) ----------
        void BindGrade()
        {
            if (!introVolume) return;
            var p = introVolume.profile;
            p.TryGet(out fxGrade); p.TryGet(out fxVignette); p.TryGet(out fxAberration); p.TryGet(out fxBloom); p.TryGet(out fxDof);
            if (post && post.sharedProfile)
            {
                if (post.sharedProfile.TryGet(out ColorAdjustments lab))
                {
                    labExposure = lab.postExposure.overrideState ? lab.postExposure.value : 0;
                    labContrast = lab.contrast.overrideState ? lab.contrast.value : 0;
                    labSaturation = lab.saturation.overrideState ? lab.saturation.value : 0;
                }
                if (post.sharedProfile.TryGet(out Vignette v) && v.intensity.overrideState) labVignette = v.intensity.value;
                if (post.sharedProfile.TryGet(out Bloom b) && b.intensity.overrideState) labBloom = b.intensity.value;
            }
        }

        void GradeOn(bool on)
        {
            gradeOn = on;
            if (!introVolume) return;
            if (on && !fxGrade) BindGrade();
            if (!on) Look(0, labVignette, 0, 0, 0, 1, 0);
            introVolume.weight = on ? 1 : 0;
        }

        /// Sets every IntroProfile override at once: exposure (EV over the lab grade), vignette, extra contrast and
        /// saturation over the lab grade, chromatic aberration, a bloom multiplier and the background-only depth-of-field
        /// start (0 switches the blur off). Missing overrides (patch not applied) are skipped.
        void Look(float ev, float vignette, float contrast, float saturation, float aberration, float bloom, float dofStart)
        {
            if (fxGrade) { fxGrade.postExposure.Override(labExposure + ev); fxGrade.contrast.Override(labContrast + contrast); fxGrade.saturation.Override(labSaturation + saturation); }
            if (fxVignette) fxVignette.intensity.Override(vignette);
            if (fxAberration) fxAberration.intensity.Override(aberration);
            if (fxBloom) fxBloom.intensity.Override(labBloom * bloom);
            if (fxDof)
            {
                bool blur = dofStart > 0;
                fxDof.mode.Override(blur ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off);
                if (blur) { fxDof.gaussianStart.Override(dofStart); fxDof.gaussianEnd.Override(dofStart + Mathf.Max(.6f, dofStart * 1.2f)); }
            }
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

        /// The voice's mood per line where the wording calls for it (otherwise a question reads curious, an exclamation
        /// excited, the rest explaining): the face, the talk beats and the brows follow it.
        static readonly System.Collections.Generic.Dictionary<string, string> CueMood = new()
        {
            ["S02_L01"] = "Happy", ["S02_L02"] = "Curious", ["S02_L04"] = "Excited", ["S02_L08"] = "Excited", ["S02_L15"] = "Excited",
            ["S03_L12"] = "Encouraging", ["S04_L01"] = "Curious", ["S04_L04"] = "Excited", ["S04_L06"] = "Excited", ["S04_L08"] = "Happy",
            ["S05_L02"] = "Excited", ["S07_L03"] = "Curious", ["S08_L05"] = "Excited", ["S09_L01"] = "Excited", ["S09_L03"] = "Curious",
            ["S09_L04"] = "Curious", ["S10_L01"] = "Happy", ["S10_L02"] = "Encouraging", ["S11_L01"] = "Happy", ["S11_L02"] = "Curious", ["S11_L03"] = "Curious",
        };

        public void OnCue(string cue)
        {
            if (NikoWaiting) NikoArrive();   // the frame his line starts (answers arrive him through Update)
            var line = narrator.database.Get(cue);
            var text = line?.text ?? "";
            niko.Express(CueMood.TryGetValue(cue, out var mood) ? mood : text.Contains("?") ? "Curious" : text.Contains("!") ? "Excited" : "Explaining", 8);
            bool free = niko.Gesture == "Idle";   // an authored gesture of the same beat (Snap, Wave) is not replaced
            if (CuePart.TryGetValue(cue, out var id) && Part(id) is { } part) NikoPoint(eye.FocusOf(part), 2.6f);
            else if (cue == "S02_L01") { niko.lookTarget = lookCamera; niko.Play("Greet", 2.4f); }
            else if (cue == "S11_L03") { niko.lookTarget = lookCamera; niko.Play("Think", 2.6f); }
            else if (free && (cue.EndsWith("_L01") || cue == "S02_L09" || cue == "S03_L12" || cue == "S04_L08")) { niko.lookTarget = lookCamera; niko.Play("Present", 2.2f); }
            else if (free && line?.IsPrompt == true) { niko.lookTarget = lookCamera; niko.Play("Talk", 2); }
            else niko.lookTarget = lookCamera;
        }
    }
}
