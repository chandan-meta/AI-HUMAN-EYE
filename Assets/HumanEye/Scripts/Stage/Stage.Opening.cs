using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        public const float OpeningLength = 32.5f;
        [Tooltip("Small cyan burst at each layer change and at the eye extraction.")] public ParticleSystem anatomyTransition;
        [Tooltip("Electric breakup motes, born on the departing layer's surface (driven by AnatomyRevealController).")] public ParticleSystem anatomyMotes;
        [Header("Opening shots (metres from the subject's eyes, degrees)")]
        [Tooltip("Wide establishing shot after the gate: distance, height over the stand top, lens.")] public float wideDistance = 10.5f, wideHeight = 1.75f, wideFov = 46;
        [Tooltip("Full-body title shot: distance, lens.")] public float fullDistance = 4.6f, fullFov = 38;
        [Tooltip("Medium bust for the anatomy reveals: distance, look point below the eyes, lens.")] public float bustDistance = 2.05f, bustLookDrop = .33f, bustFov = 35;
        [Tooltip("Exposure (EV over the lab grade) in the corridor before the gate opens, and the lab's own level after it.")] public float corridorLift = .45f, labLift = -.1f;
        [Tooltip("Metres the opening starts behind the gate's doorway, and the lens there (the still before the leaves part; corridor gate, 15 October 2026). The lens eases to the bust lens as the camera enters.")] public float gateApproach = 8f, gateFov = 50f;

        // One continuous production path, measured from the owner's subject position. No scene transforms are relocated.
        IEnumerator Opening(CinemachineCamera dolly, Transform anchor)
        {
            BuildOpeningPath();
            cams.Snap("S02_IntroDolly"); GradeOn(true);
            int beat = 0;
            float[] accents = { 1f, 3.5f, 9f, 17f, 22f, 26.5f };
            // Audio overhaul (15 October 2026): the leaves start, are fully open, the lab and title, the two breakups, the
            // extraction. Indices also drive the bursts below, so entries change in place only. Niko is not in the opening
            // (21 October 2026): he arrives with his first line, after it (Stage.NikoArrive).
            string[] sounds = { "gateunlock", "gatewhoosh", "labsting", "dissolve", "dissolve", "eyeappear" };
            float[] volumes = { .7f, .6f, .65f, .6f, .55f, .6f };
            for (float t = 0; t < OpeningLength && !skipIntro; t += Clock.Delta)
            {
                IntroTime = t;
                while (beat < accents.Length && t >= accents[beat])
                {
                    sfx.Play(sounds[beat], volumes[beat]); beat++;
                    if (beat >= 4 && beat <= 6) Burst(anatomyTransition, beat == 6 ? iEye : iMid - Vector3.up * .12f, 48);
                }
                bool At(float s) => t >= s && t - Clock.Delta < s;   // one frame, whatever the frame rate
                if (At(.3f)) sfx.Play("gatescan", .55f);              // the gate's eye wakes in the corridor still
                if (At(15)) { sfx.Play("gatescan", .4f); sfx.BeginIntroMusic("transition"); }   // the face hold: the layer scan begins
                if (At(20)) sfx.SetMusic("lab", .1f);                 // under the transition score, so the hand-over returns to the lab bed
                if (At(25.5f)) { sfx.Play("eyezoom", .55f); sfx.EndIntroMusic(); }   // the push into the eye; the score hands back
                if (At(29.5f)) sfx.Play("hologram", .5f);             // the eye settles on the display
                OpeningFrame(dolly, t);
                yield return null;
            }
            if (skipIntro) { sfx.StopEffects(); sfx.Play("skip", .6f); }
        }

        bool gateCrossed; AnatomyRevealController.RevealState revealHeard;
        void OpeningLoop(string key, bool on, float level, Transform at = null) { if (on) sfx.LoopOn(key, level, at); else sfx.LoopOff(key); }

        /// Closed gate, straight approach, two-second face hold, then matched shoulder/head layer reveals.
        void BuildOpeningPath()
        {
            float e = iMid.y;
            Vector3 F(float d, float y) => new Vector3(iMid.x, y, iMid.z) + iFace * d;
            float gateDistance = Vector3.Dot(new Vector3(GateLook.x, e, GateLook.z) - iMid, iFace);
            if (entrance && entrance.leaves != null && entrance.leaves.Length > 0 && entrance.leaves[0])
            {   // the corridor gate's doorway (its leaves, closed at this point) sets the start, not the retired lifting door's constant
                var doorway = new Bounds(entrance.leaves[0].position, Vector3.zero);
                foreach (var leaf in entrance.leaves) if (leaf) foreach (var r in leaf.GetComponentsInChildren<Renderer>()) doorway.Encapsulate(r.bounds);
                gateDistance = Vector3.Dot(new Vector3(doorway.center.x, e, doorway.center.z) - iMid, iFace);
            }
            var start = F(gateDistance + gateApproach, e + .04f);
            xrayEndPos = iEye + iFace * .8f + iRight * (iSide * .08f) + Vector3.up * .01f;
            xrayEndLook = iEye;
            presentPos = iPresent + new Vector3(0, .25f, -4.3f);
            presentLook = iPresent + Vector3.up * .1f;
            var bustLook = iMid - Vector3.up * bustLookDrop;
            introPath = new[] { new[] {
                new CamKey(0, start, bustLook, bustFov),
                new CamKey(15, F(bustDistance, e + .04f), bustLook, bustFov),
            }, new[] { // Separate segments prevent spline overshoot during the matching layer holds.
                new CamKey(15, F(bustDistance, e + .04f), bustLook, bustFov),
                new CamKey(25.5f, F(bustDistance, e + .04f), bustLook, bustFov),
            }, new[] {
                new CamKey(25.5f, F(bustDistance, e + .04f), bustLook, bustFov),
                new CamKey(26.5f, xrayEndPos, xrayEndLook, 35),
            } };
        }

        void OpeningFrame(CinemachineCamera dolly, float t)
        {
            if (t < 26.5f)
            {
                var pose = Pose(t);
                if (t < 15)
                {
                    // Ease distance logarithmically so the straight dolly slows as the face fills the frame.
                    float start = Vector3.Dot(introPath[0][0].pos - iMid, iFace);
                    float distance = Mathf.Exp(Mathf.Lerp(Mathf.Log(start), Mathf.Log(bustDistance), Ease(t, 3.5f, 15)));
                    pose = new CamKey(t, iMid + iFace * distance + Vector3.up * .04f,
                        iMid - Vector3.up * bustLookDrop, Mathf.Lerp(gateFov, bustFov, Ease(t, 3.5f, 8f)));   // the whole gate in the corridor still, the bust lens by the time the camera is through it
                    if (!gateCrossed && distance <= start - gateApproach) { gateCrossed = true; sfx.Play("gateentry", .6f); }   // the camera crosses the doorway
                }
                Dolly(dolly, pose.pos, pose.look, pose.fov, .015f);
            }
            else
            {
                float u = Ease(t, 26.5f, 29.5f), radius = iR * AnatomicalEye;
                Vector3 at = Bezier(iEye, iEye + iFace * .22f + Vector3.up * .22f,
                    iPresent + iFace * .3f + Vector3.up * .25f, iPresent, u);
                eyeRig.SetPositionAndRotation(at, Quaternion.Slerp(iEyeRot, iPresentRot, u));
                eyeRig.localScale = Vector3.one * (radius * Mathf.Pow(heroPoint.lossyScale.x / radius, u));
                var pos = Bezier(xrayEndPos, xrayEndPos + iFace * .7f + Vector3.up * .45f,
                    presentPos + Vector3.up * .3f, presentPos, u);
                var look = Vector3.Lerp(at, presentLook, u);
                float handover = Ease(t, 29.5f, OpeningLength);
                var settle = cams.Get(SettleShot);
                if (settle)
                {
                    pos = Vector3.Lerp(pos, settle.transform.position, handover);
                    var rotation = Quaternion.Slerp(Quaternion.LookRotation(look - pos), settle.transform.rotation, handover);
                    Dolly(dolly, pos, pos + rotation * Vector3.forward, Mathf.Lerp(35, cams.AuthoredFov(settle), handover), .015f);
                }
                else Dolly(dolly, pos, look, 35, .015f);
            }
            if (entrance) { entrance.SetGate(Ease(t, 1f, 3.5f)); entrance.SetAccents(Ease(t, 1f, 2f)); }   // a one-second still, then the leaves part before the dolly moves at 3.5 s
            human.SetBlink(Ease(t, 15.7f, 15.82f) * (1 - Ease(t, 15.9f, 16.12f)));
            Exhibit(1 - Ease(t, 6.5f, 9));
            float light = Mathf.Lerp(.8f, 1, Ease(t, 0, 4));
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i]) roomLights[i].intensity = roomBase[i] * Mathf.Lerp(.72f, .9f, Ease(t, 0, 4));
            keyLight.intensity = keyIntensity * light * .85f;
            displayLight.intensity = displayIntensity * light * .65f;
            sfx.musicGain = 1;
            // Three-point portrait light: key front-left high, a soft fill front-right low, a rim behind-right so the
            // figure separates from the windows.
            bool portrait = t >= 7 && t < 28.5f; float ramp = Ease(t, 7, 9);
            if (faceKey)
            {
                var at = iMid + iFace * 1.2f - iRight * .75f + Vector3.up * .65f;
                faceKey.enabled = portrait;
                faceKey.transform.SetPositionAndRotation(at, Quaternion.LookRotation(iMid - at));
                faceKey.intensity = faceKeyBase * .45f * ramp;
            }
            if (eyeFill)
            {
                var at = iMid + iFace * 1.5f + iRight * .95f - Vector3.up * .15f;
                eyeFill.enabled = portrait;
                eyeFill.transform.SetPositionAndRotation(at, Quaternion.LookRotation(iMid - Vector3.up * .3f - at));
                eyeFill.intensity = eyeFillBase * .5f * ramp;
            }
            if (eyeRim)
            {
                var target = t < 26.5f ? iMid : eyeRig.position;
                var at = target - iFace * .9f + iRight * .65f + Vector3.up * .55f;
                eyeRim.enabled = t >= 7;
                eyeRim.transform.SetPositionAndRotation(at, Quaternion.LookRotation(target - at));
                eyeRim.intensity = eyeRimBase * 1.1f * ramp;
            }
            if (eyeKey) eyeKey.enabled = t >= 28;
            if (human.reveal)
            {
                human.reveal.Evaluate(t);
                var state = human.reveal.State;   // a layer stands complete: a resolving shimmer (also marks the reduced-motion stills)
                if (state != revealHeard)
                {
                    if (state == AnatomyRevealController.RevealState.Muscle) sfx.Play("layerdone", .55f);
                    else if (state == AnatomyRevealController.RevealState.Organs) sfx.Play("organs", .55f);
                    revealHeard = state;
                }
            }
            // Loops that follow the real motion: the leaves' servo (from the gate, 3D) and each breakup's crackle.
            OpeningLoop("gateservo", t > 1f && t < 3.6f, Mathf.Sin(Mathf.PI * Ease(t, 1f, 3.5f)), entrance ? entrance.transform : null);
            OpeningLoop("crackle1", t > 17f && t < 18.7f, Mathf.Sin(Mathf.PI * Ease(t, 17f, 18.6f)));
            OpeningLoop("crackle2", t > 22f && t < 23.7f, Mathf.Sin(Mathf.PI * Ease(t, 22f, 23.6f)));
            if (t < 26.5f)
            {
                eyeRig.localScale = Vector3.one * (iR * Mathf.Lerp(1, AnatomicalEye, Ease(t, 17, 18.6f)));
                otherEye.transform.localScale = eyeRig.localScale;
            }
            foreach (var p in otherEye.parts) p.SetAlpha(1 - Ease(t, 26.5f, 28.5f), true);
            if (t >= 28.5f) { human.gameObject.SetActive(false); otherEye.gameObject.SetActive(false); }
            hud.Fade(1 - Ease(t, 0, .6f));   // a short fade-in: the corridor is seen, not a black hold
            // The corridor is lifted, not black; the lab sits a touch under its lesson level and returns to it by the hand-over.
            float lab = Ease(t, 3.5f, 6.5f);
            float ev = labLift * (1 - Ease(t, 29.5f, OpeningLength));
            Look(ev, .2f, 0, 2 * lab, 0, .85f, 0);   // no depth of field: the extracted eye fades in see-through, and a blur took it too
            IntroCaption = t < 3.5f ? null : t < 9 ? "A world of detail. One remarkable sense."
                : t < 15 ? null : t < 17 ? "SKIN · The surface we see"
                : t < 22 ? "MUSCULAR SYSTEM · Muscles move the eyes and eyelids"
                : t < 25.5f ? "INTERNAL ORGANS · The eyes and brain work together"
                : t < 29.5f ? "THE EYE · Let's look closer" : null;
            if (t >= 9 && t < 12)
                SetTitle("THE HUMAN EYE", "DISCOVER HOW VISION WORKS", Ease(t, 9, 9.8f), Ease(t, 9.5f, 10.2f) * (1 - Ease(t, 11.5f, 12)), 1 - Ease(t, 11.5f, 12));
            else ClearTitle();
        }
    }
}
