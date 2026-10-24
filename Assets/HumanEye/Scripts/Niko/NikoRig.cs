using System.Collections;
using UnityEngine;

namespace HumanEye
{
    /// Niko's body and face on the owner's rigged character (15 October 2026: Mixamo-named skeleton, shape keys Close, Ou,
    /// Blink, Eye_L/R/U/D, plus the export's BrowUp; Builder.ApplyNikoCharacter). One procedural driver poses the bones from
    /// the rest pose every frame, in world space, so nothing else fights it for the same bones or shapes:
    /// • idle states Niko_Idle_Breathing / _Curious / _Happy / _Thinking (breath, weight shift, sway, look-around, blinks);
    /// • talk states Niko_Talk_Normal / _Excited / _Explaining / _Question / _Encouraging, chosen from the expression, with beat
    ///   gestures that change pose every few seconds (and sometimes rest) so the hands never repeat one wave;
    /// • gestures (Wave, Greet, Goodbye, RaiseHand, Present, Point, PointLeft, PointRight, Pinch, Snap, Think, Surprise,
    ///   Celebrate, Encourage, Listen, Excited) that override the talk beats for their duration and lay their own mood over
    ///   the held expression while they last;
    /// • expressions (Happy, Curious, Excited, Surprised, Thinking, Explaining, Encouraging, Listening) on mouth rest shape,
    ///   brows, head tilt, lean, gaze and bounce;
    /// • lip sync: mouth keys on the cue (character timing from the TTS, otherwise precomputed from the clip's audio), with
    ///   loudness and spectral roundness as the last resort, blended on Close and Ou with attack/release smoothing.
    /// Gesture sides are the viewer's: "R" is the arm on Niko's -X side, which is his anatomical left (he faces +Z).
    public class NikoRig : MonoBehaviour
    {
        [Header("Bones (R = the arm on Niko's -X side, L = +X)")]
        public Transform floater, hips, spine, neck, head;
        public Transform clavicleL, clavicleR, shoulderL, shoulderR, elbowL, elbowR, handL, handR, thumbR, fingerR;
        [Tooltip("Finger segments (three each, base first; the index finger first) and the three thumb segments, per hand.")]
        public Transform[] fingersR, fingersL, thumbR3, thumbL3;
        [Header("Face: the supplied shape keys and the export's BrowUp")]
        public SkinnedMeshRenderer skin;
        public int blinkShape = -1, closeShape = -1, ouShape = -1, browShape = -1;
        [Tooltip("Gaze shapes measured at install: the one that turns the pupils toward Niko's -X side, +X, up, down.")]
        public int gazeMinusX = -1, gazePlusX = -1, gazeUp = -1, gazeDown = -1;
        [Tooltip("Mouth opening and closing speed (1/s).")] public float mouthAttack = 24, mouthRelease = 15;
        [Tooltip("Largest gaze shape weight (stronger weights show the socket rim).")] [Range(0, 1)] public float gazeMax = .32f;
        [Tooltip("Eye centre above the head joint (m at scale 1; 0.24 on the supplied Niko): looks are aimed from the eyes.")] public float eyeAbove = .24f;
        [Tooltip("Proxies carrying the blink and talk state (autopilot logs, tests).")] public Transform eyeL, eyeR, smile;
        public Transform snapRing;
        public Renderer snapRingRenderer;
        public Narrator narrator;
        public Transform lookTarget, gestureTarget;
        [Tooltip("Head look-at limits (degrees); the eyes cover the rest. Neck and head pitch together stay within 12° up and 10° down (nods included): further down, the big head's jaw meets the arms in front.")] public float lookYaw = 50, lookPitch = 10;

        /// His spot in the room, set by Place and MoveTo. While he floats beside the lens (NikoFloatingCompanion sets `floating`)
        /// a move changes only this spot, which he returns to in his own shots; otherwise it also moves him at once.
        public Vector3 HomePosition { get; private set; }
        public Quaternion HomeRotation { get; private set; } = Quaternion.identity;
        public float HomeScale { get; private set; } = 1;
        [System.NonSerialized] public bool floating;
        /// While he floats: the lens he floats beside. He looks at things where the learner sees them (on the line of sight, at
        /// his own distance), with a short head turn, so he never turns round to the room behind him.
        [System.NonSerialized] public Vector3 lens;

        public string Gesture { get; private set; } = "Idle";
        public string Expression { get; private set; } = "Happy";
        /// The state shown in logs and tests: a talk style while he speaks, a gesture, or an idle variant.
        public string State => Gesture != "Idle" ? "Niko_" + Gesture : speaking ? "Niko_Talk_" + talkStyle : "Niko_Idle_" + (Expression == "Thinking" ? "Thinking" : idle);
        public bool Speaking => speaking;
        public float MouthOpen => open;

        struct Look { public float smile, brow, tilt, gazeUp, side, bounce, lean; public bool ou; }
        static Look Of(string expression) => expression switch
        {
            "Happy" => new Look { smile = .55f, brow = .12f, tilt = 3, bounce = .35f },
            "Curious" => new Look { smile = .2f, brow = .55f, tilt = 9, side = .25f },
            "Excited" => new Look { smile = 1, brow = .75f, bounce = 1 },
            "Surprised" => new Look { ou = true, brow = 1, lean = -5 },
            "Thinking" => new Look { smile = 0, brow = .25f, tilt = -7, gazeUp = .7f, side = .45f },
            "Explaining" => new Look { smile = .3f, brow = .2f, tilt = 2 },
            "Encouraging" => new Look { smile = .75f, brow = .35f, tilt = 5, bounce = .3f },
            "Listening" => new Look { smile = .35f, brow = .3f, tilt = 8, lean = 3 },
            _ => new Look { smile = .4f },
        };

        /// One arm's target: the upper arm along `dir` (character space), the forearm bent `bend` degrees toward `toward`.
        struct Pose { public Vector3 dir, toward; public float bend, curl, index; }
        static Vector3 V(float side, float outward, float y, float z) => new(side * outward, y, z);   // side -1 = the R arm (-X)
        static Vector3 Up(float side) => V(side, .35f, 1, .25f);
        static Pose P(float side, float outward, float y, float z, float bend, float curl = .12f, float index = -1, Vector3? toward = null)
            => new() { dir = V(side, outward, y, z), bend = bend, curl = curl, index = index < 0 ? curl : index, toward = toward ?? V(side, -.35f, .2f, .85f) };

        float gestureLeft, expressionLeft, time, blinkIn = 3, blink, blinkTwice = -1, pinch, ring = -1, yawNow, pitchNow, eyeYaw, eyePitch;
        float open, round, brow, browKick, smileNow, tilt, lean, bounce, accent, accentS, beatLeft, idleLeft = 8, bow, happyW, questionW, nodW, talkW, ooW;
        int syllable, viseme, beatPose; bool armed = true, speaking, wasSpeaking;
        string talkStyle = "Normal", idle = "Breathing", heldExpression, gestureMood;
        Pose armR = P(-1, .8f, -1, .15f, 6, .3f), armL = P(1, .8f, -1, .15f, 6, .3f);
        Transform[] driven; Quaternion[] rest; Vector3 palmR, palmL;
        static readonly int[] Pattern = { 0, 1, 0, 2, 1, 0, 2, 0, 1, 2, 0, 1 };   // last-resort flavours: open, narrow (ee), round (oo)
        static readonly string[] Idles = { "Breathing", "Curious", "Breathing", "Happy", "Thinking" };

        /// A gesture for `seconds` (it overrides the talk beats; its mood lies over the held expression while it lasts);
        /// "Talk" only colours the talk beats with curiosity when no expression is held.
        public void Play(string gesture, float seconds = 1.6f)
        {
            if (gesture == "Talk") { if (heldExpression == null) Express("Curious", seconds + 1.5f); return; }
            Gesture = gesture; gestureLeft = seconds;
            if (gesture == "Snap") ring = 0;
            gestureMood = gesture switch
            {
                "Surprise" => "Surprised", "Celebrate" => "Excited", "Excited" => "Excited", "Encourage" => "Encouraging",
                "Think" => "Thinking", "Listen" => "Listening", "Wave" or "Greet" or "Goodbye" => "Happy", _ => null,
            };
            if (gestureMood != null && blinkIn > .25f) blinkIn = .12f;
        }

        /// The floating companion changed sides: a sideways point keeps pointing toward the middle of the screen.
        public void MirrorPoint(bool onRight) { if (Gesture is "PointLeft" or "PointRight") Gesture = onRight ? "PointLeft" : "PointRight"; }

        /// An expression for `seconds` (negative: until the next one); afterwards he returns to Explaining while speaking,
        /// otherwise to his idle mood. A change of expression brings a natural blink.
        public void Express(string expression, float seconds = -1)
        {
            if (heldExpression != expression && blinkIn > .25f) blinkIn = .12f;
            heldExpression = expression; expressionLeft = seconds < 0 ? float.MaxValue : seconds;
        }

        public IEnumerator MoveTo(Transform anchor, float seconds, bool quick)
        {
            Vector3 p0 = HomePosition; Quaternion r0 = HomeRotation;
            if (quick)
            {
                yield return Routine.Tween(.2f, t => Home(p0, r0, 1 - t));
                yield return Routine.Tween(.25f, t => Home(anchor.position, anchor.rotation, t));
                yield break;
            }
            yield return Routine.Tween(seconds, t => Home(Vector3.Lerp(p0, anchor.position, t), Quaternion.Slerp(r0, anchor.rotation, t), HomeScale));
        }

        public void Place(Transform anchor) => Home(anchor.position, anchor.rotation, 1);

        void Home(Vector3 position, Quaternion rotation, float scale)
        {
            HomePosition = position; HomeRotation = rotation; HomeScale = scale;
            if (floating) return;
            transform.SetPositionAndRotation(position, rotation); transform.localScale = Vector3.one * scale;
        }

        void Awake() { HomePosition = transform.position; HomeRotation = transform.rotation; HomeScale = transform.localScale.x; }

        void StoreRest()
        {
            var list = new System.Collections.Generic.List<Transform> { hips, spine, neck, head, clavicleL, clavicleR, shoulderL, shoulderR, elbowL, elbowR, handL, handR };
            foreach (var set in new[] { fingersR, fingersL, thumbR3, thumbL3 }) if (set != null) list.AddRange(set);
            list.RemoveAll(t => !t);
            driven = list.ToArray(); rest = new Quaternion[driven.Length];
            for (int i = 0; i < driven.Length; i++) rest[i] = driven[i].localRotation;
            palmR = Palm(handR, fingersR, thumbR3); palmL = Palm(handL, fingersL, thumbL3);
        }

        // The palm normal in hand space, from the plane of the first and last finger; the relaxed thumb lies on the palm side.
        static Vector3 Palm(Transform hand, Transform[] fingers, Transform[] thumb)
        {
            if (!hand || fingers == null || fingers.Length < 6 || thumb == null || thumb.Length < 2) return Vector3.zero;
            int n = fingers.Length;
            var c = Vector3.Cross(fingers[1].position - fingers[0].position, fingers[n - 2].position - fingers[n - 3].position);
            if (c.sqrMagnitude < 1e-12f) c = Vector3.Cross(fingers[1].position - fingers[0].position, fingers[n - 3].position - fingers[0].position);
            var nrm = c.normalized;
            if (Vector3.Dot(nrm, thumb[1].position - thumb[0].position) < 0) nrm = -nrm;
            return hand.InverseTransformDirection(nrm);
        }

        static void Turn(Transform bone, Vector3 axis, float degrees) { if (bone && axis.sqrMagnitude > 1e-8f) bone.rotation = Quaternion.AngleAxis(degrees, axis.normalized) * bone.rotation; }
        static Quaternion Frame(Vector3 dir, Vector3 pole) { var up = pole - dir * Vector3.Dot(pole, dir); return Quaternion.LookRotation(dir, up.sqrMagnitude > 1e-6f ? up : Vector3.up); }
        // Point a bone at a world direction, keeping its roll relative to the pole plane (no twist).
        static void Reframe(Transform bone, Transform child, Vector3 want, Vector3 pole)
        {
            if (!bone || !child) return;
            var now = child.position - bone.position;
            if (now.sqrMagnitude < 1e-8f || want.sqrMagnitude < 1e-8f) return;
            bone.rotation = Frame(want.normalized, pole) * Quaternion.Inverse(Frame(now.normalized, pole)) * bone.rotation;
        }
        static float Approach(float now, float target, float rate, float dt) => Mathf.Lerp(now, target, 1 - Mathf.Exp(-dt * rate));

        void LateUpdate()
        {
            float dt = Clock.Delta;
            bool paused = Clock.Paused;
            float life = paused ? Time.unscaledDeltaTime : dt;   // breathing, blinks and gaze go on while the lesson is paused
            time += life;
            gestureLeft -= dt; expressionLeft -= dt;
            if (gestureLeft <= 0 && Gesture != "Idle") Gesture = "Idle";
            if (driven == null) StoreRest();
            for (int i = 0; i < driven.Length; i++) driven[i].localRotation = rest[i];

            speaking = narrator && narrator.Speaking && !narrator.AwaitingContinue && !paused;   // a line held for Read & Continue is silent
            if (expressionLeft <= 0) heldExpression = null;
            var mood = gestureLeft > -.4f ? gestureMood : null;
            if (speaking && mood is "Listening" or "Thinking") mood = null;   // a silent gesture's mood cannot show while he speaks: keep the line's
            var wanted = mood ?? heldExpression;
            Expression = speaking && wanted is "Listening" or "Thinking" ? "Explaining"
                : wanted ?? (speaking ? "Explaining" : idle is "Curious" or "Thinking" ? idle : "Happy");
            var look = Of(Expression);
            if (speaking && !wasSpeaking) { beatLeft = 0; browKick = .6f; }
            wasSpeaking = speaking;
            if (!speaking && Gesture == "Idle" && (idleLeft -= life) <= 0) { idle = Idles[Random.Range(0, Idles.Length)]; idleLeft = Random.Range(7f, 12f); }
            talkStyle = Expression switch { "Excited" or "Surprised" => "Excited", "Curious" => "Question", "Encouraging" => "Encouraging", _ => syllable / 9 % 2 == 0 ? "Explaining" : "Normal" };

            // Every additive offset is eased, so nothing pops when speech, a gesture or the idle variant changes; gesture-driven
            // motion fades out while paused (the arms hold still then).
            float kLife = 1 - Mathf.Exp(-life * 5), kGesture = 1 - Mathf.Exp(-(dt > 0 ? dt : life) * 6);
            smileNow = Mathf.Lerp(smileNow, look.smile, kLife); tilt = Mathf.Lerp(tilt, look.tilt + (idle == "Curious" && !speaking ? 6 : 0), kLife);
            lean = Mathf.Lerp(lean, look.lean, kLife); bounce = Mathf.Lerp(bounce, look.bounce + (Gesture is "Celebrate" or "Excited" && !paused ? 1 : 0), kLife);
            bow = Mathf.Lerp(bow, Gesture == "Greet" ? 8 * Mathf.Clamp01(gestureLeft - .8f) : 0, kGesture);
            happyW = Mathf.Lerp(happyW, idle == "Happy" && !speaking ? 1 : 0, kLife);
            questionW = Mathf.Lerp(questionW, talkStyle == "Question" && speaking ? 5 : 0, kLife);
            nodW = Mathf.Lerp(nodW, Gesture == "Encourage" && !paused ? 4 : 0, kLife);

            // ---- body: breath, weight shift, bounce, lean
            float breath = Mathf.Sin(time * 1.7f);
            if (floater) floater.localPosition = new Vector3(0, .006f * breath + .018f * bounce * Mathf.Abs(Mathf.Sin(time * 3.1f)), 0);
            Turn(hips, transform.up, 1.5f * Mathf.Sin(time * .55f) + happyW * 3 * Mathf.Sin(time * 1.1f));   // his own up: floating, it follows the lens
            Turn(spine, transform.right, 1.2f * breath - .6f + lean + bow);
            Turn(spine, transform.forward, happyW * 2.5f * Mathf.Sin(time * 1.1f));

            Arms(dt);

            // ---- head and eyes: look-at from the eyes, the eyes cover what the head has not turned yet; tilt; nod on the voice
            var target = lookTarget;
            float yawAll = 0, pitchAll = 0;
            if (target && head)
            {
                var eyes = head.position + transform.up * (eyeAbove * transform.lossyScale.y);
                var at = target.position;
                if (floating) { var sight = at - lens; float d = Vector3.Distance(lens, eyes); if (sight.magnitude > d) at = lens + sight.normalized * d; }
                var local = transform.InverseTransformDirection(at - eyes);
                yawAll = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                pitchAll = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            }
            if (!speaking && Gesture == "Idle" && idle == "Curious") { yawAll += 14 * Mathf.Sin(time * .45f); pitchAll -= 4 * Mathf.Sin(time * .3f); }
            float kLook = 1 - Mathf.Exp(-life * 4.5f), kEye = 1 - Mathf.Exp(-life * 14);
            float yawLimit = floating ? 20 : lookYaw;   // floating he is already turned three-quarters to the middle: the eyes do the rest
            yawNow = Mathf.Lerp(yawNow, Mathf.Clamp(yawAll, -yawLimit, yawLimit), kLook); pitchNow = Mathf.Lerp(pitchNow, Mathf.Clamp(pitchAll, -lookPitch, lookPitch), kLook);
            eyeYaw = Mathf.Lerp(eyeYaw, yawAll - yawNow + look.side * 18, kEye); eyePitch = Mathf.Lerp(eyePitch, pitchAll - pitchNow - look.gazeUp * 18, kEye);
            float nod = Mathf.Clamp(Mathf.Sin(time * 5.5f) * 2.5f * open + nodW * Mathf.Sin(time * 6) + accentS * 3, -12 - pitchNow, 10 - pitchNow);
            Turn(neck, transform.right, pitchNow * .3f); Turn(neck, transform.up, yawNow * .3f);
            Turn(head, transform.right, pitchNow * .7f + nod); Turn(head, transform.up, yawNow * .7f);
            // The big head sits on the shoulders: past about 8° of roll it would sink into the shoulder it leans toward.
            Turn(head, transform.forward, Mathf.Clamp(Mathf.Sin(time * .9f) * 2 + tilt + questionW, -8, 8));

            Face(dt, life, look);

            if (ring >= 0 && snapRing && handR)
            {
                ring += dt * 1.8f;
                snapRing.position = handR.position;
                snapRing.localScale = Vector3.one * (.05f + ring * .5f);
                snapRingRenderer.enabled = ring < 1;
                snapRingRenderer.material.SetFloat("_Alpha", 1 - ring);
                if (ring >= 1) ring = -1;
            }
        }

        Vector3 Aim(Transform shoulder) => gestureTarget && shoulder ? (transform.InverseTransformPoint(gestureTarget.position) - transform.InverseTransformPoint(shoulder.position)).normalized : new Vector3(-.4f, 0, 1).normalized;

        /// Pointing and presenting toward the gesture target within his reach: from a low front reach to an outward one, by how
        /// far out on that arm's side the target lies (his short arms cannot point up or across his own head).
        Pose Reach(float side, float bend, float curl, float index = -1)
        {
            var p = P(side, .76f, -.35f, .54f, bend, curl, index);
            float t = Mathf.Clamp01(side * Aim(side < 0 ? shoulderR : shoulderL).x + .3f);
            p.dir = Vector3.Slerp(p.dir.normalized, V(side, .95f, -.25f, .2f).normalized, t);
            return p;
        }

        /// Arm targets for the gesture, the talk beat or the idle state, eased toward and solved as two-bone arms.
        /// Niko's head is wider than his shoulders and rests on them, each arm segment is 13 cm and his belly is round: every
        /// pose here was checked on the shipped mesh (the outer upper arm, forearm, hand and fingers clear of head and torso, with
        /// the head anywhere in its range: rolled ±8°, pitched 12° up to 10° down, talk accents included:
        /// Tools/HumanEye/niko_arm_clearance.py, whose pose table must follow any change here). Upper arms stay at or below the
        /// horizontal; raised hands go up beside the head, never in front of it.
        void Arms(float dt)
        {
            Pose r = P(-1, .8f, -1, .15f, 6, .3f), l = P(1, .8f, -1, .15f, 6, .3f);
            float pinchTarget = 0, beat = Mathf.Sin(time * 3);
            if (speaking && Gesture == "Idle" && (beatLeft -= dt) <= 0) { beatPose = (beatPose + 1 + Random.Range(0, 3)) % 4; beatLeft = Random.Range(2.2f, 3.6f); }
            switch (Gesture)
            {
                case "Wave": case "Greet": case "Goodbye": r = P(-1, 1, -.1f, .12f, 45 + 10 * Mathf.Sin(time * 9), .08f, toward: Up(-1)); break;
                case "RaiseHand": r = P(-1, 1, -.05f, .15f, 50, .08f, toward: Up(-1)); break;
                case "Present": r = Reach(-1, 25, .1f); l = P(1, .6f, -.55f, .4f, 12, .1f); break;
                case "Point":
                    if (Aim(shoulderR).x > .25f) l = Reach(1, 2, .85f, 0);   // a target on his +X side: the near hand points
                    else r = Reach(-1, 2, .85f, 0);
                    break;
                case "PointLeft": l = P(1, .95f, -.25f, .2f, 2, .85f, 0); break;     // the viewer's left (his +X)
                case "PointRight": r = P(-1, .95f, -.25f, .2f, 2, .85f, 0); break;  // the viewer's right
                case "Pinch": r = Reach(-1, 40, .8f, .2f); pinchTarget = 1; break;
                case "Snap": r = P(-1, .95f, -.15f, .3f, 50, .8f, .2f, V(-1, .2f, 1, .45f)); pinchTarget = gestureLeft > .9f ? 1 : 0; break;
                case "Think": r = P(-1, 1, -.25f, .2f, 62, .7f, .35f, V(-1, -.1f, 1, .3f)); l = P(1, .9f, -.45f, -.1f, 40, .5f, toward: V(1, -.2f, -1, .1f)); break;   // hand up by the cheek, the other on the hip
                case "Surprise": r = P(-1, .95f, -.2f, .3f, 54, 0, toward: Up(-1)); l = P(1, .95f, -.2f, .3f, 54, 0, toward: Up(1)); break;
                case "Celebrate": case "Excited":
                    float pump = 3 * Mathf.Sin(time * 8);
                    r = P(-1, 1, -.12f, .15f, 47 + pump, .85f, toward: Up(-1)); l = P(1, 1, -.12f, .15f, 47 - pump, .85f, toward: Up(1)); break;
                case "Encourage": r = P(-1, .65f, -.4f, .65f, 60 + 8 * Mathf.Sin(time * 7), .9f); break;
                case "Listen": r = P(-1, .65f, -.8f, .35f, 35, .5f); l = P(1, .65f, -.8f, .35f, 35, .5f); break;
                default:
                    if (speaking) TalkBeat(beat, ref r, ref l);
                    else if (Expression == "Thinking") goto case "Think";
                    break;
            }
            float k = 1 - Mathf.Exp(-dt * 7);
            Ease(ref armR, r, Gesture == "Idle" ? accent * 6 : 0, k); Ease(ref armL, l, Gesture == "Idle" ? accent * 4 : 0, k);
            pinch = Mathf.Lerp(pinch, pinchTarget, 1 - Mathf.Exp(-dt * 12));
            Arm(clavicleR, shoulderR, elbowR, handR, armR, -1);
            Arm(clavicleL, shoulderL, elbowL, handL, armL, 1);
            Curl(fingersR, handR, palmR, armR.curl, armR.index); Curl(thumbR3, handR, palmR, Mathf.Lerp(.15f, .45f, armR.curl), -1);
            Curl(fingersL, handL, palmL, armL.curl, armL.index); Curl(thumbL3, handL, palmL, Mathf.Lerp(.15f, .45f, armL.curl), -1);
            if (pinch > .01f && fingerR && thumbR && fingerR.childCount > 0 && thumbR.childCount > 0)
            {
                var toThumb = thumbR.position - fingerR.position;
                Reframe(fingerR, fingerR.GetChild(0), Vector3.Slerp(fingerR.GetChild(0).position - fingerR.position, toThumb, pinch * .8f), transform.forward);
                Reframe(thumbR, thumbR.GetChild(0), Vector3.Slerp(thumbR.GetChild(0).position - thumbR.position, -toThumb, pinch * .6f), transform.forward);
            }
        }

        static void Ease(ref Pose now, Pose to, float accentBend, float k)
        {
            now.dir = Vector3.Slerp(now.dir, to.dir.normalized, k); now.toward = Vector3.Slerp(now.toward, to.toward.normalized, k);
            now.bend = Mathf.Lerp(now.bend, to.bend + accentBend, k); now.curl = Mathf.Lerp(now.curl, to.curl, k); now.index = Mathf.Lerp(now.index, to.index, k);
        }

        /// Talk beats: four poses per style (pose 0 rests the hands), changed every 2.2-3.6 s; syllable accents kick the elbows.
        void TalkBeat(float beat, ref Pose r, ref Pose l)
        {
            if (beatPose == 0) return;
            switch (talkStyle)
            {
                case "Explaining":
                    if (beatPose == 1) { r = P(-1, .6f, -.5f, .6f, 55 + 6 * beat); l = P(1, .64f, -.5f, .6f, 55 - 6 * beat); }
                    else if (beatPose == 2) r = P(-1, .8f, -.3f, .45f, 30 + 5 * beat, .1f);
                    else l = P(1, .66f, -.5f, .55f, 45 + 6 * beat);
                    break;
                case "Excited":
                    float fist = beatPose == 3 ? .8f : .1f;
                    r = P(-1, .85f, -.25f, .45f, 38 + 8 * beat, fist, toward: V(-1, .2f, 1, .4f));
                    l = P(1, .85f, -.25f, .45f, 38 - 8 * beat, fist, toward: V(1, .2f, 1, .4f));
                    break;
                case "Question":
                    r = P(-1, .55f, -.4f, .7f, 52 + 4 * beat, .05f);
                    if (beatPose == 3) l = P(1, .66f, -.4f, .62f, 64, .05f);
                    break;
                case "Encouraging":
                    r = P(-1, .5f, -.45f, .6f, 60 + 4 * beat, beatPose == 1 ? .9f : .15f);
                    if (beatPose == 2) l = P(1, .6f, -.5f, .55f, 50, .15f);
                    break;
                default:
                    if (beatPose == 2) l = P(1, .6f, -.6f, .55f, 40 + 10 * beat, .2f);
                    else r = P(-1, .55f, -.6f, .55f, 40 + 10 * beat, .2f);
                    break;
            }
        }

        /// Two-bone arm: the upper arm points along the pose's direction, the forearm bends toward the pose's `toward`; the
        /// bones keep their roll against a pole behind the elbow, and the clavicle lifts a little as the arm rises.
        void Arm(Transform clavicle, Transform upper, Transform fore, Transform hand, Pose pose, float side)
        {
            var dir = pose.dir.normalized;
            var want = transform.TransformDirection(dir);
            var pole = (-transform.forward * .85f + transform.right * side * .35f - transform.up * .2f).normalized;
            Turn(clavicle, transform.forward, side * 6 * Mathf.Clamp01(dir.y + .35f));
            Reframe(upper, fore, want, pole);
            var axis = Vector3.Cross(want, transform.TransformDirection(pose.toward));
            Reframe(fore, hand, axis.sqrMagnitude > 1e-6f ? Quaternion.AngleAxis(pose.bend, axis.normalized) * want : want, pole);
        }

        /// Curl finger segments toward the palm: `amount` 0..1 (90° over three segments); `index` overrides the first finger (-1 = same).
        void Curl(Transform[] segments, Transform hand, Vector3 palmLocal, float amount, float index)
        {
            if (segments == null || !hand || palmLocal == Vector3.zero) return;
            var n = hand.TransformDirection(palmLocal);
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i]; if (!seg || seg.childCount == 0) continue;
                float a = (i < 3 && index >= 0 ? index : amount) * 90 * (i % 3 == 0 ? .4f : i % 3 == 1 ? .35f : .25f);
                Turn(seg, Vector3.Cross(seg.GetChild(0).position - seg.position, n), a);
            }
        }

        /// The face: lip sync on Close/Ou, the expression's resting mouth, brows, gaze shapes and blinks (with an occasional double blink).
        void Face(float dt, float life, Look look)
        {
            float openT = 0, roundT = 0, drive = 0;
            bool keyed = speaking && narrator.TryMouth(out openT, out roundT);   // mouth keys on the cue (timing or precomputed audio)
            if (speaking && !keyed) drive = narrator.Level * 1.5f;
            else if (keyed) drive = openT;
            // Syllable onsets (either source): elbow and nod accents, an occasional brow lift, the last-resort mouth flavour.
            if (drive < .15f) armed = true;
            else if (armed && drive > .4f)
            {
                armed = false; syllable++; viseme = Pattern[syllable % Pattern.Length]; accent = 1;
                if (syllable % 5 == 0) browKick = Mathf.Max(browKick, .55f);
            }
            if (speaking && !keyed)
            {   // last resort (a cue without keys): loudness opens the jaw, spectral roundness and onsets vary the shape
                openT = Mathf.Clamp01((narrator.Level - .03f) * 1.9f) * (viseme == 1 ? .6f : 1);
                roundT = Mathf.Clamp01(Mathf.Max(viseme == 2 ? .8f : 0, narrator.Roundness * 1.2f - .2f));
            }
            accent = Mathf.MoveTowards(accent, 0, dt * 4);
            accentS = Approach(accentS, accent, 30, dt);   // the nod eases into each accent instead of jumping
            open = Approach(open, openT, openT > open ? mouthAttack : mouthRelease, dt > 0 ? dt : life);
            round = Approach(round, roundT, 18, dt > 0 ? dt : life);
            // Resting mouth from the expression (Close 1 = the closed smile line, 0 = the modelled open smile), blended with the
            // talking mouth by eased weights so the start and end of a line, pause and the Surprised "oo" never pop.
            talkW = Approach(talkW, speaking ? 1 : 0, 12, life);
            ooW = Approach(ooW, look.ou ? 1 : 0, 10, life);
            float close = Mathf.Lerp(Mathf.Lerp(1 - .5f * smileNow, .15f, ooW), Mathf.Max(1 - open, .3f * round), talkW);
            float ou = Mathf.Lerp(.75f * ooW, round * Mathf.Clamp01(open * 1.8f), talkW);
            float browT = Mathf.Max(look.brow, browKick);   // speech starts and every fifth syllable lift the brows, eased both ways
            brow = Approach(brow, browT, browT > brow ? 20 : 6, life); browKick = Mathf.MoveTowards(browKick, 0, life * 2);
            blinkIn -= life;
            if (blinkIn <= 0)
            {
                blink = 1;
                if (blinkTwice < 0 && Random.value < .18f) { blinkTwice = 1; blinkIn = .28f; }   // an occasional double blink
                else { blinkTwice = -1; blinkIn = Random.Range(2.4f, 5.6f); }
            }
            blink = Mathf.MoveTowards(blink, 0, life * 7);
            float lid = Mathf.Sin(blink * Mathf.PI) * .97f;   // the supplied Blink streaks a little at a full 100
            if (skin)
            {
                Set(blinkShape, lid); Set(closeShape, close); Set(ouShape, ou); Set(browShape, brow);
                float gx = Mathf.Clamp(eyeYaw / 25f, -1, 1) * gazeMax, gy = Mathf.Clamp(-eyePitch / 20f, -1, 1) * gazeMax;
                Set(gazePlusX, Mathf.Max(0, gx)); Set(gazeMinusX, Mathf.Max(0, -gx)); Set(gazeUp, Mathf.Max(0, gy)); Set(gazeDown, Mathf.Max(0, -gy));
            }
            var eyeScale = new Vector3(1, Mathf.Lerp(1, .12f, lid), 1);
            if (eyeL) eyeL.localScale = eyeScale;
            if (eyeR) eyeR.localScale = eyeScale;
            if (smile) smile.localScale = new Vector3(1, 1 + 1.6f * open, 1);
        }

        void Set(int shape, float weight) { if (shape >= 0) skin.SetBlendShapeWeight(shape, Mathf.Clamp01(weight) * 100); }
    }
}
