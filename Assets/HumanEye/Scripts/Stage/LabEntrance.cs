using UnityEngine;

namespace HumanEye
{
    /// Futuristic lifting gate. Leaves are driven by absolute local poses, so skip, replay and the menu are deterministic.
    /// Convention (IntroPatch bakes the leaves, Builder.ApplyReference sets their open poses): leaves[0] and [1] are the
    /// two panel halves, which rise together (the room's own Door01Up motion, as in the reference); a third leaf, when
    /// present, is the floor bolt that retracts first. Each stage eases in and out.
    public class LabEntrance : MonoBehaviour
    {
        public Transform[] leaves;
        public Vector3[] closed, open;
        public Light[] accents;
        public float accentIntensity = .7f;
        [Tooltip("Share of the opening spent on the bolt, then a short mechanical pause before the halves part.")]
        public float boltShare = .25f, pauseShare = .1f;
        public float OpenAmount { get; private set; }

        public void SetGate(float amount)
        {
            OpenAmount = Mathf.Clamp01(amount);
            bool hasBolt = leaves.Length == 3;
            for (int i = 0; i < leaves.Length; i++)
            {
                if (!leaves[i]) continue;   // a leaf removed from the scene (the retired lifting door) must not stop the menu or the opening
                bool bolt = hasBolt && i == 2;
                float k = !hasBolt ? OpenAmount
                        : bolt ? Mathf.InverseLerp(0, boltShare, OpenAmount)
                        : Mathf.InverseLerp(boltShare + pauseShare, 1, OpenAmount);
                leaves[i].localPosition = Vector3.Lerp(closed[i], open[i], Mathf.SmoothStep(0, 1, k));
            }
        }

        public void SetAccents(float amount)
        {
            foreach (var light in accents) light.intensity = accentIntensity * Mathf.Clamp01(amount);
        }
    }
}
