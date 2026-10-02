using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanEye
{
    /// A separately addressable structure (eye layer, nerve, brain area, cell type…). Owns its material instances,
    /// so transparency/highlight changes never leak, and keeps pick colliders in step with what is visible.
    public class AnatomyPart : MonoBehaviour
    {
        public string id, label;
        public Renderer[] main;          // keep half + section caps, or the whole mesh
        public Renderer[] removable;     // cutaway half (eye layers only)
        public Collider[] pick;
        [Tooltip("Higher wins when several eligible parts are under the pointer.")] public int priority;
        [Tooltip("Transparent shells/volumes yield to an eligible structure just behind them.")] public bool seeThrough;
        public bool pickWhenInvisible;
        public Transform focus, labelAnchor;
        [Tooltip("Optional alternatives used while the eye is cut open.")] public Transform focusCut, labelAnchorCut;
        [Range(0, 1)] public float baseAlpha = 1;
        public int queueOffset;

        /// Set by the stage: is this part a valid target right now?
        public bool Selectable { get; set; }
        public bool Visible => gameObject.activeInHierarchy && Effective > .05f;
        float Effective => baseAlpha * alpha;

        float alpha = 1, alphaTarget = 1, glow, glowTarget, cut = 1, cutTarget = 1;
        readonly Dictionary<Material, Material> mainMats = new(), cutMats = new();

        void Awake()
        {
            foreach (var r in main) r.sharedMaterials = r.sharedMaterials.Select(m => Instance(mainMats, m)).ToArray();
            foreach (var r in removable) r.sharedMaterials = r.sharedMaterials.Select(m => Instance(cutMats, m)).ToArray();
            Apply();
        }

        Material Instance(Dictionary<Material, Material> map, Material shared)
        {
            if (!shared) return shared;
            if (!map.TryGetValue(shared, out var copy)) map[shared] = copy = new Material(shared) { name = shared.name + " (" + id + ")" };
            return copy;
        }

        void OnDestroy() { foreach (var m in mainMats.Values.Concat(cutMats.Values)) Destroy(m); }

        public void SetAlpha(float a, bool instant = false) { alphaTarget = a; if (instant) alpha = a; }
        public void SetGlow(float g, bool instant = false) { glowTarget = g; if (instant) glow = g; }
        /// Visibility of the removable (cutaway) half, 1 = present.
        public void SetCut(float visible, bool instant = false) { cutTarget = visible; if (instant) cut = visible; }
        public void ResetVisual() { SetAlpha(1, true); SetGlow(0, true); SetCut(1, true); Apply(); }
        public void SetFloat(string property, float value) { foreach (var m in mainMats.Values.Concat(cutMats.Values)) m.SetFloat(property, value); }

        void Update()
        {
            var dt = Time.unscaledDeltaTime * 4;   // UI feedback: deliberately not frozen by FREEZE
            alpha = Mathf.MoveTowards(alpha, alphaTarget, dt);
            glow = Mathf.MoveTowards(glow, glowTarget, dt);
            cut = Mathf.MoveTowards(cut, cutTarget, dt * .6f);
            Apply();
        }

        void Apply()
        {
            foreach (var m in mainMats.Values) Configure(m, Effective, queueOffset);
            foreach (var m in cutMats.Values) Configure(m, Effective * cut, queueOffset);
            foreach (var r in removable) r.enabled = cut > .01f;
            bool canPick = Selectable && (pickWhenInvisible || Effective > .05f) && gameObject.activeInHierarchy;
            foreach (var c in pick) if (c) c.enabled = canPick;
        }

        void Configure(Material m, float a, int order)
        {
            m.SetFloat("_Alpha", a);
            m.SetFloat("_Highlight", glow);
            bool transparent = a < .995f;
            m.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            m.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            m.SetFloat("_ZWrite", transparent ? 0 : 1);
            m.renderQueue = transparent ? 3000 + order : 2000 + order;
        }
    }
}
