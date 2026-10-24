using UnityEngine;

namespace HumanEye
{
    /// The opening's scan subject: layered human (skin, muscles, skull, brain) with two anatomical eyes in their sockets.
    /// The reference pass adds the Z-Anatomy vision structures: the orbicularis oculi apart from the other facial
    /// muscles, and a "Visual System" (eye muscles, optic pathway, brain) that sits in the head at rest and is floated out
    /// by the eye-to-brain cinematic.
    public class HumanDisplay : MonoBehaviour
    {
        public AnatomyPart skin, muscles, skull, brain;
        [Tooltip("Stays opaque during the scan (dissolves only with the whole figure).")] public AnatomyPart garment;
        public Transform scanPlane, eyeLeft, eyeRight;
        public Renderer scanRenderer;
        public AnatomyRevealController reveal;
        public SkinnedMeshRenderer blinkingSkin;
        public void SetBlink(float amount) { if (blinkingSkin) blinkingSkin.SetBlendShapeWeight(0, Mathf.Clamp01(amount) * 100); }
        public float headTop = 1.75f, footY = 0, headY = 1.5f;

        [Header("Vision anatomy (Builder.ApplyReference)")]
        [Tooltip("Orbital and palpebral orbicularis oculi: glows when the skin wipes off.")] public AnatomyPart orbicularis;
        [Tooltip("The six extra-ocular muscles, levator, trochlea and tendinous ring of both orbits.")] public AnatomyPart eyeMuscles;
        [Tooltip("Optic nerve, chiasm, tract, lateral geniculate nucleus, radiation, primary visual cortex, in signal order.")] public AnatomyPart[] pathway;
        [Tooltip("Eye muscles, pathway and brain: aligned with the head at rest; the eye-to-brain cinematic floats it out.")] public Transform visualSystem;
        [Tooltip("The eye sockets inside the Visual System (the eyes ride them when it moves).")] public Transform systemSocketL, systemSocketR;
        [Tooltip("Space of the signal routes vp_* (scaled .1, so pulse speed and width suit the head).")] public Transform pathwaySpace;
        [Tooltip("Height of the skin mesh's head/body seam above the root (the collar line): layer wipes stop there, the shirt stays.")] public float neckY = 1.45f;

        Quaternion restRotation; bool rested;

        /// Every layer the cinematics may wipe or fade.
        public AnatomyPart[] Layers => new[] { skin, garment, muscles, orbicularis, skull, brain, eyeMuscles };

        void Awake() { restRotation = transform.localRotation; rested = true; }

        public void ResetVisual(bool visible, float skinAlpha = 1)
        {
            SetBlink(0);
            gameObject.SetActive(visible);
            if (!rested) { restRotation = transform.localRotation; rested = true; }
            transform.localRotation = restRotation;   // the intro turns the subject toward the gate
            transform.localScale = Vector3.one;
            foreach (var p in Layers) if (p) { p.ClearWipe(); p.SetBodyFade(0, 0, 0); }
            skin.SetAlpha(skinAlpha, true); skin.SetGlow(0, true);
            garment.SetAlpha(skinAlpha, true);
            foreach (var p in new[] { muscles, orbicularis, skull, brain, eyeMuscles }) if (p) { p.SetAlpha(0, true); p.SetGlow(0, true); }
            if (pathway != null) foreach (var p in pathway) if (p) { p.SetAlpha(0, true); p.SetGlow(0, true); }
            if (visualSystem) { visualSystem.localPosition = Vector3.zero; visualSystem.localRotation = Quaternion.identity; visualSystem.localScale = Vector3.one; }
            scanPlane.gameObject.SetActive(false);
            if (reveal) reveal.ResetVisual();
        }
    }
}
