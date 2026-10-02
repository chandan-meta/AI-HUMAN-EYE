using System.Collections;
using UnityEngine;

namespace HumanEye
{
    /// Niko's body language: hover, look-at, blink, voice-driven face and readable arm gestures
    /// (wave, raise hand, present, point, pinch, snap). Procedural so gestures can aim at any structure.
    public class NikoRig : MonoBehaviour
    {
        public Transform floater, head, shoulderL, shoulderR, elbowL, elbowR, thumbR, fingerR, handR;
        public Transform eyeL, eyeR, smile, snapRing;
        public Renderer snapRingRenderer;
        public Narrator narrator;
        public Transform lookTarget, gestureTarget;

        public string Gesture { get; private set; } = "Idle";
        float gestureLeft, time, blinkIn = 3, blink, pinch, ring = -1;
        Vector3 armDirR = new(-.3f, -1, .05f), armDirL = new(.3f, -1, .05f);
        float elbowBendR = 15, elbowBendL = 15;

        public void Play(string gesture, float seconds = 1.6f) { Gesture = gesture; gestureLeft = seconds; if (gesture == "Snap") ring = 0; }

        public IEnumerator MoveTo(Transform anchor, float seconds, bool quick)
        {
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            if (quick)
            {
                yield return Routine.Tween(.2f, t => transform.localScale = Vector3.one * (1 - t));
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                yield return Routine.Tween(.25f, t => transform.localScale = Vector3.one * t);
                yield break;
            }
            yield return Routine.Tween(seconds, t => transform.SetPositionAndRotation(Vector3.Lerp(p0, anchor.position, t), Quaternion.Slerp(r0, anchor.rotation, t)));
        }

        public void Place(Transform anchor) { transform.SetPositionAndRotation(anchor.position, anchor.rotation); transform.localScale = Vector3.one; }

        public IEnumerator Appear(float seconds)
        {
            gameObject.SetActive(true);
            Play("Wave", 2.4f);
            yield return Routine.Tween(seconds, t => transform.localScale = Vector3.one * Mathf.SmoothStep(0, 1, t) * (1 + .08f * Mathf.Sin(t * Mathf.PI)));
        }

        void LateUpdate()
        {
            float dt = Clock.Delta;
            time += dt;
            gestureLeft -= dt;
            if (gestureLeft <= 0 && Gesture != "Idle") Gesture = "Idle";
            floater.localPosition = new Vector3(0, .035f * Mathf.Sin(time * 1.7f), 0);

            Vector3 targetLocal = gestureTarget ? transform.InverseTransformPoint(gestureTarget.position) - shoulderR.localPosition : new Vector3(-.4f, 0, 1);
            Vector3 aim = targetLocal.normalized;
            Vector3 dirR, dirL = new(.3f, -1, .05f);
            float bendR = 15, bendL = 15, pinchTarget = 0;
            switch (Gesture)
            {
                case "Wave": dirR = new Vector3(-.65f, .8f, .1f); bendR = 35 + 25 * Mathf.Sin(time * 9); break;
                case "RaiseHand": dirR = new Vector3(-.25f, 1, .1f); bendR = 10; break;
                case "Present": dirR = Vector3.Lerp(aim, new Vector3(-.6f, -.2f, .7f), .35f); bendR = 25; dirL = new Vector3(.5f, -.6f, .4f); break;
                case "Point": dirR = aim; bendR = 2; break;
                case "Pinch": dirR = Vector3.Lerp(aim, new Vector3(0, .2f, 1), .5f); bendR = 30; pinchTarget = 1; break;
                case "Snap": dirR = new Vector3(-.45f, .45f, .75f); bendR = 55; pinchTarget = gestureLeft > .9f ? 1 : 0; break;
                case "Talk": dirR = new Vector3(-.45f, -.6f, .55f); bendR = 40 + 10 * Mathf.Sin(time * 3); break;
                default: dirR = new Vector3(-.3f, -1, .05f); break;
            }
            float k = 1 - Mathf.Exp(-dt * 7);
            armDirR = Vector3.Slerp(armDirR, dirR.normalized, k); armDirL = Vector3.Slerp(armDirL, dirL.normalized, k);
            elbowBendR = Mathf.Lerp(elbowBendR, bendR, k); elbowBendL = Mathf.Lerp(elbowBendL, bendL, k);
            pinch = Mathf.Lerp(pinch, pinchTarget, 1 - Mathf.Exp(-dt * 12));
            shoulderR.localRotation = Quaternion.FromToRotation(Vector3.down, armDirR);
            shoulderL.localRotation = Quaternion.FromToRotation(Vector3.down, armDirL);
            elbowR.localRotation = Quaternion.Euler(-elbowBendR, 0, 0);
            elbowL.localRotation = Quaternion.Euler(-elbowBendL, 0, 0);
            thumbR.localRotation = Quaternion.Euler(0, 0, -35 + 40 * pinch);
            fingerR.localRotation = Quaternion.Euler(-10 - 50 * pinch, 0, 0);

            if (lookTarget)
            {
                var local = transform.InverseTransformPoint(lookTarget.position) - head.localPosition;
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -55, 55);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -25, 25);
                var want = Quaternion.Euler(pitch, yaw, Mathf.Sin(time * .9f) * 2);
                if (narrator && narrator.Busy) want *= Quaternion.Euler(Mathf.Sin(time * 5.5f) * 2.5f * narrator.Level, 0, 0);
                head.localRotation = Quaternion.Slerp(head.localRotation, want, 1 - Mathf.Exp(-dt * 5));
            }

            blinkIn -= dt;
            if (blinkIn <= 0) { blink = 1; blinkIn = Random.Range(2.5f, 5.5f); }
            blink = Mathf.MoveTowards(blink, 0, dt * 7);
            float talk = narrator && narrator.Busy ? narrator.Level : 0;
            var eyeScale = new Vector3(1, Mathf.Lerp(1, .12f, Mathf.Sin(blink * Mathf.PI)) * (1 - .15f * talk), 1);
            eyeL.localScale = eyeR.localScale = eyeScale;
            smile.localScale = new Vector3(1, 1 + 1.6f * talk, 1);

            if (ring >= 0 && snapRing)
            {
                ring += dt * 1.8f;
                snapRing.position = handR.position;
                snapRing.localScale = Vector3.one * (.05f + ring * .5f);
                snapRingRenderer.enabled = ring < 1;
                snapRingRenderer.material.SetFloat("_Alpha", 1 - ring);
                if (ring >= 1) ring = -1;
            }
        }
    }
}
