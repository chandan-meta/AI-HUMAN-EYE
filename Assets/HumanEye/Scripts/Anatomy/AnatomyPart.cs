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
        public Renderer[] exterior = new Renderer[0]; // optional imported closed-eye surface
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
        public bool UseImportedExterior { get; set; } = true;
        /// Set by PointerSelector while the mouse rests on this (selectable) part: a soft rim, under any lesson highlight.
        public bool Hovered { get; set; }
        public bool Visible => gameObject.activeInHierarchy && Effective > .05f;
        /// Visibility of the removable half right now (1 = present; in between it is see-through and writes no depth).
        public float Cut => cut;
        float Effective => baseAlpha * alpha;

        float alpha = 1, alphaTarget = 1, glow, glowTarget, cut = 1, cutTarget = 1, hover;
        Vector4 wipe;   // x front (world y), y alpha above, z alpha below, w on (Anatomy.shader _Wipe)
        Vector4 bodyFade;
        float wipeFloor = -1000;
        readonly Dictionary<Material, Material> mainMats = new(), cutMats = new(), exteriorMats = new();

        void Awake()
        {
            foreach (var r in main) r.sharedMaterials = r.sharedMaterials.Select(m => Instance(mainMats, m)).ToArray();
            foreach (var r in removable) r.sharedMaterials = r.sharedMaterials.Select(m => Instance(cutMats, m)).ToArray();
            foreach (var r in exterior) r.sharedMaterials = r.sharedMaterials.Select(m => Instance(exteriorMats, m)).ToArray();
            Apply();
        }

        Material Instance(Dictionary<Material, Material> map, Material shared)
        {
            if (!shared) return shared;
            if (!map.TryGetValue(shared, out var copy)) map[shared] = copy = new Material(shared) { name = shared.name + " (" + id + ")" };
            return copy;
        }

        void OnDestroy() { foreach (var m in mainMats.Values.Concat(cutMats.Values).Concat(exteriorMats.Values)) Destroy(m); }

        public void SetAlpha(float a, bool instant = false) { alphaTarget = a; if (instant) alpha = a; }
        public void SetGlow(float g, bool instant = false) { glowTarget = g; if (instant) glow = g; }
        /// Visibility of the removable (cutaway) half, 1 = present.
        public void SetCut(float visible, bool instant = false) { cutTarget = visible; if (instant) cut = visible; }
        public void ResetVisual() { SetAlpha(1, true); SetGlow(0, true); SetCut(1, true); wipe = bodyFade = Vector4.zero; Apply(); }
        /// Horizontal wipe: the layer keeps `above` alpha over world height `front` and `below` under it (0 = cut away),
        /// and nothing under `floor`; a warm line rides the front. Applied at once (the caller animates the front).
        public void SetWipe(float front, float above, float below, float floor = -1000) { wipe = new Vector4(front, above, below, 1); wipeFloor = floor; Apply(); }
        public void ClearWipe() { if (wipe.w == 0) return; wipe = Vector4.zero; Apply(); }
        public void SetBodyFade(float level, float width, float strength) { bodyFade = new Vector4(level, width, strength, 0); Apply(); }
        public void SetFloat(string property, float value) { foreach (var m in mainMats.Values.Concat(cutMats.Values)) m.SetFloat(property, value); }
        public void SetColor(string property, Color value) { foreach (var m in mainMats.Values.Concat(cutMats.Values)) m.SetColor(property, value); }

        void Update()
        {
            var dt = Clock.Paused ? 0 : Time.unscaledDeltaTime * 4;   // FREEZE permits selection; Pause holds material transitions.
            alpha = Mathf.MoveTowards(alpha, alphaTarget, dt);
            glow = Mathf.MoveTowards(glow, glowTarget, dt);
            cut = Mathf.MoveTowards(cut, cutTarget, dt * .6f);
            hover = Mathf.MoveTowards(hover, Hovered && Selectable ? .32f : 0, Time.unscaledDeltaTime * 2.5f);
            Apply();
        }

        void Apply()
        {
            float imported = UseImportedExterior && exterior.Length > 0 ? Mathf.SmoothStep(0, 1, cut) : 0;
            foreach (var m in mainMats.Values) Configure(m, Effective * (1 - imported), queueOffset);
            foreach (var m in cutMats.Values) Configure(m, Effective * cut * (1 - imported), queueOffset);
            foreach (var m in exteriorMats.Values) Configure(m, Effective * imported, queueOffset);
            foreach (var r in exterior) r.enabled = imported > .001f;
            foreach (var r in removable) r.enabled = cut > .01f;
            bool canPick = Selectable && (pickWhenInvisible || Effective > .05f) && gameObject.activeInHierarchy;
            foreach (var c in pick) if (c) c.enabled = canPick;
        }

        void Configure(Material m, float a, int order)
        {
            m.SetFloat("_Alpha", a);
            m.SetFloat("_Highlight", Mathf.Max(glow, hover));
            m.SetVector("_Wipe", wipe); m.SetFloat("_WipeFloor", wipeFloor); m.SetVector("_BodyFade", bodyFade);
            // The face lies above the shoulder fade and must still occlude its own mouth interior.
            bool fadedBody = bodyFade.z > 0 && m.GetFloat("_AboveBodyFade") < .5f;
            bool partialWipe = wipe.w > 0 && (Partial(wipe.y) || Partial(wipe.z));
            bool transparent = a < .995f || fadedBody || partialWipe;
            m.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
            m.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            // A solid layer that only carries the shoulder fade keeps writing depth: blended without it, its own far
            // surfaces draw over the near ones (the muscle head showed the white inside of its skull cap).
            m.SetFloat("_ZWrite", a < .995f || partialWipe ? 0 : 1);
            m.renderQueue = transparent ? 3000 + order : 2000 + order;
        }

        static bool Partial(float a) => a > .001f && a < .995f;
    }
}
