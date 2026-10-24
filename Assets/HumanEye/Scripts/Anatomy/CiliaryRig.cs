using UnityEngine;

namespace HumanEye
{
    /// The ciliary muscle ring around the lens (22 October 2026 brief: show accommodation, don't only say it). It follows the
    /// eye's accommodation every frame: contracted (narrower ring) while the lens is rounded for a near object, relaxed for a
    /// distant one, and it glows while it works so the cause of the lens change reads at a glance.
    public class CiliaryRig : MonoBehaviour
    {
        public EyeModel eye;
        public Renderer[] renderers;
        public Transform labelAnchor, focus;
        [Tooltip("Ring scale toward the optical axis at full accommodation (a contracted muscle).")] public float contracted = .88f;

        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        Material[] live;
        float last, work;

        /// Set by the stage while the ring is being explained: a steady glow under the working glow.
        public bool Shown { get; set; }

        void Awake()
        {
            live = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) live[i] = renderers[i].material;   // own instances: the glow never leaks
            last = eye.Accommodation;
        }

        void OnDestroy() { foreach (var m in live) if (m) Destroy(m); }

        void Update()
        {
            float a = eye.Accommodation, d = Mathf.Max(Clock.VizDelta, 1e-4f);
            work = Mathf.MoveTowards(work, Mathf.Clamp01(Mathf.Abs(a - last) / d * 3), Time.unscaledDeltaTime * 2);
            last = a;
            float s = Mathf.Lerp(1, contracted, a);
            transform.localScale = new Vector3(s, s, 1);
            float glow = Mathf.Max(Shown ? .35f + .3f * a : 0, work);
            foreach (var m in live) m.SetFloat(HighlightId, glow);
        }
    }
}
