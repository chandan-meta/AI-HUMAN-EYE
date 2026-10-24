using System.Collections;
using UnityEngine;

namespace HumanEye
{
    /// Scene 9's eye-to-brain cinematic (reference pass, 8 October 2026), in the Human Brain reference's language:
    /// LIGHT — warm-white rays from the apple cross the cut-open eye and land on the retina; SIGNALS — the retina answers with
    /// gold comets that leave along the optic nerve (light and signals never share a colour or a path); PATHWAY — the eye flies back
    /// into its socket inside an X-ray head and pulses run from both eyes along the optic nerves, cross at the chiasm (nasal
    /// fibres), continue along the optic tracts to the lateral geniculate nuclei and out along the optic radiations to the
    /// primary visual cortex, each stage labelled as the pulse reaches it, the camera riding over the head; BRAIN — the whole
    /// visual system (eyes, pathway, ghosted brain) rises out of the head and floats in the hall, the visual cortex lighting
    /// with every arrival, under FROM EYE TO BRAIN. Ledger lines S09_L01–L03 stay content steps: the cinematic waits at each
    /// beat until the line before has finished, and the content waits for the beat (`cineBeat`), so pictures and lines never
    /// drift apart. Letterboxed, Skip-able, Pause-safe; reduced motion holds a still per beat. Camera: S09_Follow (code-driven).
    public partial class Stage
    {
        public bool EyeBrainPlaying { get; private set; }
        static readonly string[] EyeBrainBeats = { "light", "signals", "pathway", "brain", "end" };
        static readonly string[] EyeBrainTitle = { "FROM EYE TO BRAIN", "Where Visual Signals Become Perception" };
        static readonly string[] Routes = { "vp_RR", "vp_RL", "vp_LL", "vp_LR" };
        int eyeBrainBeat = -1;

        void RegisterEyeBrain()
        {
            actions["eyeToBrain"] = (_, __) => { EyeBrainPlaying = true; eyeBrainBeat = -1; return EyeToBrain(); };   // raised now: the next step's cineBeat runs before the side routine first ticks
            actions["cineBeat"] = (beat, _) => CineBeat(beat);
        }

        /// Content side of the handshake: wait until the cinematic has reached `beat` (immediately when none is playing).
        IEnumerator CineBeat(string beat)
        {
            int i = System.Array.IndexOf(EyeBrainBeats, beat);
            while (EyeBrainPlaying && eyeBrainBeat < i) yield return null;
        }

        IEnumerator EyeToBrain()
        {
            EyeBrainPlaying = true; skipIntro = false; eyeBrainBeat = -1; hud.Refresh();
            CaptureIntroState(); BindGrade(); GradeOn(true);
            var cam = cams.Get(FollowShotName); if (!cam) cam = followShot;
            var h = human; var head = (h.eyeLeft.position + h.eyeRight.position) * .5f;
            float slow = timing == CinematicTiming.ReferenceMatch ? 1.6f : 1;   // the reference's own pacing for side-by-side checks
            var eyeC = eye.transform.position; var front = eye.transform.forward; var cut = -eye.transform.right; float r = eye.transform.lossyScale.x;
            var centre = head + h.transform.forward * .07f;   // between the eyes and the occipital lobe, the pathway's middle
            var present = centre + Vector3.up * .6f; const float Grow = 3.2f;
            var routeCentre = Vector3.Lerp(eyeC, brain.focus.position, .5f);
            float span = Vector3.Distance(eyeC, brain.focus.position) + r * 2.2f;
            float distance = span / (2 * Mathf.Tan(20 * Mathf.Deg2Rad) * (16f / 9)) * 1.4f;
            Vector3 camA = routeCentre + new Vector3(-.12f, .35f, -distance), lookA = routeCentre;
            Vector3 camB = camA + Vector3.right * .24f;
            Vector3 camC = head + new Vector3(-.14f, .32f, -.5f), lookC = centre + new Vector3(0, -.01f, -.02f);   // over the forehead, the whole head in frame
            Vector3 camD = centre + new Vector3(.09f, .44f, .25f), lookD = centre + new Vector3(0, -.03f, .02f);    // over the crown, looking back to the cortex
            var side = Vector3.Cross(Vector3.up, h.transform.forward).normalized;   // the subject's right: a lateral view, eyes at one end, the cortex at the other
            Vector3 camE = present - side * 1.75f + Vector3.up * .3f + h.transform.forward * .1f;
            if (cam) { Point(cam, camA, lookA, 40); runner.RunSide(cams.MoveTo(cam.name, Short ? 0 : 1f)); }
            {
                // ---- LIGHT: warm rays from the apple cross the eye and land on the retina
                rays.gameObject.SetActive(true); rays.reach = 0; rays.showLamp = true; rays.showImage = false;
                sfx.Play("lightmove", .5f);
                yield return Beat(0, 3f * slow, k =>
                {
                    rays.reach = Mathf.Lerp(0, 5, Mathf.SmoothStep(0, 1, Mathf.Clamp01(k * 1.25f)));
                    if (cam) Point(cam, camA, lookA, 40);
                    IntroCaption = k > .15f ? "Light reaches the retina" : null;
                });
                // ---- SIGNALS: hold eye, nerve and brain together while gold packets leave the retina (S09_L01).
                IntroCaption = null;
                SetTitle("RETINA TO BRAIN", "Neural signal pathway · schematic", 1, 1);
                eye.Get("retina").SetGlow(.4f, true);
                signals.widthScale = 1.2f; signals.Emit("nerve", true); if (!skipIntro) sfx.Play("signaltravel", .45f);
                var bFrom = cam ? cam.transform.position : camA;
                yield return Beat(1, 6.5f * slow, k =>
                {
                    rays.reach = 5 * (1 - Ease(k, .35f, .8f));
                    if (k > .8f && rays.gameObject.activeSelf) rays.gameObject.SetActive(false);
                    eye.Get("retina").SetGlow(.4f * (1 - Ease(k, 0, .2f)), true);
                    if (cam) Point(cam, Vector3.Lerp(bFrom, camB, Ease(k, .12f, .78f)), lookA, 40);
                });
                // ---- PATHWAY: back into the head, then along the visual pathway (S09_L02)
                ClearTitle(); eye.Get("retina").SetGlow(0, true);
                signals.StopAll(); rays.gameObject.SetActive(false);
                ShowObject("apple", false);   // the light's object steps aside while the head and pathway hold the frame
                h.ResetVisual(true, .12f);
                h.skull.SetAlpha(XrayAlpha, true); h.garment.SetAlpha(.12f, true);   // a faint bone ghost (no glass mode: its full-strength highlights read as a milky shell)
                h.brain.SetAlpha(0, true);
                otherEye.gameObject.SetActive(true); foreach (var p in otherEye.parts) p.ResetVisual();
                Socket(h.eyeLeft, otherEye.transform); otherEye.SetCutInstant(false); otherEye.SetNerveReveal(.12f);
                eye.SetNerveReveal(.12f);
                if (!Short) runner.RunSide(eye.Cutaway(false, .8f)); else eye.SetCutInstant(false);
                var e0 = eyeRig.position; var r0 = eyeRig.rotation; var s0 = eyeRig.localScale.x;
                var cFrom = cam ? cam.transform.position : camB;
                var labels = new[] { (.18f, 0), (.36f, 1), (.5f, 2), (.64f, 3), (.78f, 4), (.9f, 5) };   // (share of the beat, pathway part)
                int shown = -1; bool emitting = false;
                if (!skipIntro) sfx.Play("softwhoosh", .45f);
                yield return Beat(2, 7f * slow, k =>
                {
                    float travel = Ease(k, 0, .3f), fade = Ease(k, .05f, .3f);
                    var socket = h.eyeRight;
                    eyeRig.SetPositionAndRotation(Bezier(e0, e0 + Vector3.up * .5f, socket.position - socket.forward * .4f, socket.position, travel), Quaternion.Slerp(r0, socket.rotation, travel));
                    eyeRig.localScale = Vector3.one * (s0 * Mathf.Pow(socket.lossyScale.x / s0, travel));
                    brain.SetAlpha(1 - fade, true); visualArea.SetAlpha(1 - fade, true);
                    h.eyeMuscles.SetAlpha(fade, true);
                    float clear = Ease(k, .12f, .4f);   // above the head only the pathway should read: skin gone, bone a whisper
                    h.skin.SetAlpha(.12f * (1 - clear), true); h.garment.SetAlpha(.12f * (1 - clear), true); h.skull.SetAlpha(Mathf.Lerp(XrayAlpha, .05f, clear), true);
                    for (int i = 0; i < h.pathway.Length; i++) h.pathway[i].SetAlpha(Ease(k, .2f + .05f * i, .35f + .05f * i), true);
                    h.brain.SetAlpha(.35f * Ease(k, .3f, .45f), true);   // a readable brain, thin enough to show the pathway inside
                    if (!emitting && k > .3f) { emitting = true; signals.widthScale = 1.6f; foreach (var id in Routes) signals.Emit(id, true); if (!Short) sfx.Play("signaltravel", .4f); }
                    int was = shown;
                    while (shown + 1 < labels.Length && k >= labels[shown + 1].Item1)
                    {
                        shown++; var part = h.pathway[labels[shown].Item2];
                        hud.labels.Clear(); hud.labels.Add(part.label, part.labelAnchor, part.focus, null); part.SetGlow(.6f, true);
                        if (shown > 0) h.pathway[labels[shown - 1].Item2].SetGlow(0, true);
                    }
                    if (shown != was) { if (shown == 5) sfx.Play("cortex", .55f); else sfx.Soft("label", .4f); }   // one cue for the label left on screen
                    cortexPulse = Mathf.MoveTowards(cortexPulse, 0, Clock.Delta * 1.4f);
                    if (shown >= 5) h.pathway[5].SetGlow(Mathf.Max(.35f, cortexPulse), true);
                    var look = Vector3.Lerp(lookC, lookD, Ease(k, .35f, 1));
                    if (signals.Lead("vp_RR", out var lead) && k > .35f) look = Vector3.Lerp(look, lead, .3f);
                    var pos = k < .35f ? Bezier(cFrom, cFrom + Vector3.up * .8f, camC + Vector3.up * .3f, camC, Ease(k, 0, .35f)) : Bezier(camC, camC + Vector3.up * .45f, camD + Vector3.back * .2f, camD, Ease(k, .35f, 1));
                    if (cam) Point(cam, pos, look, 36);
                });
                // ---- BRAIN: the visual system rises out of the head and floats under the title (S09_L03)
                hud.labels.Clear(); foreach (var p in h.pathway) p.SetGlow(0, true);
                if (!skipIntro) sfx.Play("brainrise", .7f);
                var dFrom = cam ? cam.transform.position : camD; var dLook = lookD;
                var system = h.visualSystem; var homeLocal = h.transform.InverseTransformPoint(centre);
                bool titled = false;
                yield return Beat(3, 7f * slow, k =>
                {
                    float rise = Ease(k, 0, .55f), gone = Ease(k, 0, .3f);
                    cortexPulse = Mathf.MoveTowards(cortexPulse, 0, Clock.Delta * 1.4f);
                    float s = Mathf.Lerp(1, Grow, rise);
                    var spin = Quaternion.AngleAxis(-10 * Ease(k, .1f, 1), Vector3.up);
                    system.localScale = Vector3.one * s; system.localRotation = spin;
                    system.position = Vector3.Lerp(centre, present, rise) - h.transform.rotation * (spin * (homeLocal * s));
                    Socket(h.systemSocketR, eyeRig); Socket(h.systemSocketL, otherEye.transform);
                    h.skin.SetAlpha(0, true); h.garment.SetAlpha(0, true); h.skull.SetAlpha(.02f * (1 - gone), true);
                    h.brain.SetAlpha(Mathf.Lerp(.35f, .96f, Ease(k, .15f, .4f)), true);   // the brain firms up as it rises; the pulses (depth test off) still show reaching the cortex
                    h.pathway[5].SetGlow(Mathf.Max(.8f * Ease(k, .4f, .6f), cortexPulse), true);
                    if (!titled && k > .5f) { titled = true; sfx.Play("titlehit", .85f); Burst(titleBurst, present + Vector3.down * .35f, 140); }   // the cortex was labelled in the pathway beat; here its glow and the title speak
                    if (k > .5f) SetTitle(EyeBrainTitle[0], EyeBrainTitle[1], Ease(k, .52f, .7f), Ease(k, .74f, .82f));
                    if (cam) Point(cam, Bezier(dFrom, dFrom + Vector3.up * .5f, camE + Vector3.up * .4f, camE, Ease(k, 0, .65f)), Vector3.Lerp(dLook, present + Vector3.up * .05f, Ease(k, 0, .5f)), 38);
                });
                // The "end" beat is released by EyeBrainEnd (eyeBrainBeat = 5), never here: releasing it before the fades let the
                // next line and the lesson interface start while the last still was held (reduced motion), so the top-bar
                // controls audited as unreachable.
            }
            if (skipIntro) { signals.StopAll(); sfx.StopEffects(); sfx.Play("skip", .6f); }   // streams first, or the travel loop restarts
            yield return hud.Fade(1, Short ? .1f : .35f);
            EyeBrainEnd();
            yield return hud.Fade(0, Short ? .1f : .45f);
        }

        /// A beat starts only once the line before it has finished (the previous picture holds meanwhile), then runs its
        /// frames for `seconds` (reduced motion: its last frame as a still). Skip ends it at once.
        IEnumerator Beat(int index, float seconds, System.Action<float> frame)
        {
            while (index > 0 && narrator.Busy && !skipIntro) yield return null;
            eyeBrainBeat = index;
            NikoEyeBrain(index);
            if (Short) { frame(1); for (float t = 0; t < Mathf.Max(1.6f, seconds * .4f) && !skipIntro; t += Clock.Delta) yield return null; yield break; }
            for (float t = 0; t < seconds && !skipIntro; t += Clock.Delta) { frame(t / seconds); yield return null; }
            if (!skipIntro) frame(1);
        }

        /// Niko in the companion picture during the eye-to-brain cinematic: excited as light and signals start, pointing toward the
        /// pathway and the brain as each is shown, thoughtful as the visual system floats out.
        void NikoEyeBrain(int beat)
        {
            niko.lookTarget = lookCamera;
            if (beat == 0) niko.Express("Excited", 4);
            else if (beat == 1) { niko.Express("Excited", 6); NikoPoint(eyeRig, 2.2f); }
            else if (beat == 2) { niko.Express("Curious", 7); NikoPoint(human.transform, 2.4f); }
            else if (beat == 3) { niko.Express("Curious", 7); NikoPoint(brainRoot, 2.4f); }
        }

        float cortexPulse;   // 1 when a pulse reaches the visual cortex (SignalArrived), easing back to 0 over ~.7 s

        void Point(Unity.Cinemachine.CinemachineCamera cam, Vector3 pos, Vector3 look, float fov)
        {
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            var lens = cam.Lens; lens.FieldOfView = cams.Fitted(fov); cam.Lens = lens;
        }

        /// Behind a short black: the Scene 9 checkpoint picture again (cutaway eye at the cut point, its nerve to the brain
        /// display, the subject away), lights, labels, title and pulses cleared; the camera waits on the perception shot.
        /// Chapter entry, restart and the menu reach the same state through Enter (IntroReset clears the flags).
        void EyeBrainEnd()
        {
            signals.StopAll(); signals.widthScale = 1; hud.labels.Clear(); ClearTitle(); IntroCaption = null;
            human.ResetVisual(false);
            PlaceEye(cutPoint); eye.SetCutInstant(true); eye.SetNerveReveal(1);
            otherEye.gameObject.SetActive(false);
            brain.SetAlpha(1, true); visualArea.SetAlpha(1, true); brain.SetGlow(0, true); visualArea.SetGlow(0, true);
            rays.gameObject.SetActive(false); rays.reach = 0;
            ShowObject("apple", true); PlaceObject("apple", 7);   // the checkpoint's object again, for the perception line
            RestoreIntroState(); LabLevels();
            if (cams.Has("S09_Perception")) cams.Snap("S09_Perception");
            eyeBrainBeat = EyeBrainBeats.Length; EyeBrainPlaying = false; skipIntro = false; hud.Refresh();
        }
    }
}
