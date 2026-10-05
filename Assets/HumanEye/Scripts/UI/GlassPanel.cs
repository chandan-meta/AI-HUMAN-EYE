using UnityEngine;
using UnityEngine.UI;
namespace HumanEye
{
    /// Editable Canvas surface. Its mesh includes the soft halo outside the clickable rectangle.
    [AddComponentMenu("UI/Observatory Glass Panel")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GlassPanel : MaskableGraphic
    {
        [Min(0)] public float radius = 18;
        [Min(.5f)] public float border = 1.3f;
        [Min(1)] public float haloWidth = 14;
        [Range(0, 2)] public float glow = .12f;
        public void SetGlow(float value) { if (Mathf.Approximately(glow, value)) return; glow = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; float pad = haloWidth;
            var positions = new[] { new Vector2(-pad, -pad), new Vector2(-pad, r.height + pad), new Vector2(r.width + pad, r.height + pad), new Vector2(r.width + pad, -pad) };
            foreach (var p in positions)
            {
                var v = UIVertex.simpleVert; v.position = new Vector3(r.xMin + p.x, r.yMin + p.y); v.color = color;
                v.uv0 = p; v.uv1 = new Vector2(r.width, r.height); v.uv2 = new Vector2(Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f), border); v.uv3 = new Vector2(haloWidth, glow); vh.AddVert(v);
            }
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
        }
    }
}
