using UnityEngine;

namespace HumanEye
{
    /// Authored gate leaves and lighting groups; absolute poses make skip/replay deterministic.
    public class LabEntrance : MonoBehaviour
    {
        public Transform[] leaves;
        public Vector3[] closed, open;
        public Light[] accents;
        public float accentIntensity = .7f;
        public float OpenAmount { get; private set; }
        public void SetGate(float amount)
        {
            OpenAmount = Mathf.Clamp01(amount);
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].localPosition = Vector3.Lerp(closed[i], open[i], OpenAmount);
        }
        public void SetAccents(float amount)
        {
            foreach (var light in accents) light.intensity = accentIntensity * Mathf.Clamp01(amount);
        }
    }
}
