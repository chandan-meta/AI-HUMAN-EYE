using System.Collections;
using UnityEngine;

namespace HumanEye
{
    /// Scene 2 scan subject: layered human (skin, muscles, skull, brain) with two anatomical eyes in their sockets.
    public class HumanDisplay : MonoBehaviour
    {
        public AnatomyPart skin, muscles, skull, brain;
        [Tooltip("Stays opaque during the scan (dissolves only with the whole figure).")] public AnatomyPart garment;
        public Transform scanPlane, eyeLeft, eyeRight;
        public Renderer scanRenderer;
        public float headTop = 1.75f, footY = 0, headY = 1.5f;

        public void ResetVisual(bool visible, float skinAlpha = 1)
        {
            gameObject.SetActive(visible);
            transform.localScale = Vector3.one;
            skin.SetAlpha(skinAlpha, true); skin.SetGlow(0, true);
            garment.SetAlpha(skinAlpha, true);
            foreach (var p in new[] { muscles, skull, brain }) { p.SetAlpha(0, true); p.SetGlow(0, true); }
            scanPlane.gameObject.SetActive(false);
        }

        public IEnumerator Reveal(float seconds) => Routine.Tween(seconds, t => { skin.SetAlpha(1, true); garment.SetAlpha(1, true); });

        IEnumerator Sweep(float from, float to, float seconds, System.Action<float> during)
        {
            scanPlane.gameObject.SetActive(true);
            yield return Routine.Tween(seconds, t =>
            {
                var p = scanPlane.localPosition; p.y = Mathf.Lerp(from, to, t); scanPlane.localPosition = p;
                during(t);
            });
        }

        /// Blue scanning wave: skin turns translucent, then muscles, skull and brain are revealed in turn.
        /// The eyes are separate opaque objects, so both stay visible throughout.
        public IEnumerator Scan(float pace = 1)
        {
            yield return Sweep(headTop + .05f, headY - .25f, 2.4f * pace, t => { skin.SetAlpha(Mathf.Lerp(1, .14f, t), true); skull.SetAlpha(.3f * t, true); });
            // Clean educational X-ray: the eye remains anchored in its socket, without exposed muscle or brain.
            muscles.SetAlpha(0, true); brain.SetAlpha(0, true);
            scanPlane.gameObject.SetActive(false);
        }

        /// The whole figure freezes (no motion) and disappears.
        public IEnumerator Dissolve(float seconds)
        {
            float s0 = skin.Visible ? .14f : 0, m0 = 0, k0 = .3f;
            yield return Routine.Tween(seconds, t =>
            {
                skin.SetAlpha(s0 * (1 - t), true); muscles.SetAlpha(m0 * (1 - t), true);
                skull.SetAlpha(k0 * (1 - t), true); brain.SetAlpha(0, true); garment.SetAlpha(1 - t, true);
            });
            gameObject.SetActive(false);
        }
    }
}
