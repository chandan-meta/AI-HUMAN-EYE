using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Surface-of-revolution mesh generation around the local Z axis (the eye's optical axis).
    /// A profile is a closed outline in the (r, z) half-plane made of smooth curves; corners between curves stay sharp.
    /// Sweeping only part of a turn plus section caps gives a clean anatomical cutaway instead of stacked transparency.
    public static class Lathe
    {
        public static Mesh Revolve(IList<Vector2[]> curves, float fromDeg, float toDeg, int segments, Mesh mesh = null)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float sign = Mathf.Sign(SignedArea(curves));
            foreach (var c in curves)
            {
                if (c.Length < 2 || (c[0].x < 1e-4f && c[c.Length - 1].x < 1e-4f && OnAxis(c))) continue;
                float total = 0; for (int i = 1; i < c.Length; i++) total += Vector2.Distance(c[i], c[i - 1]);
                int start = v.Count; float run = 0;
                for (int i = 0; i < c.Length; i++)
                {
                    if (i > 0) run += Vector2.Distance(c[i], c[i - 1]);
                    var tan = (c[Mathf.Min(i + 1, c.Length - 1)] - c[Mathf.Max(i - 1, 0)]).normalized;
                    var nrm = new Vector2(tan.y, -tan.x) * sign;   // outward from the enclosed region
                    for (int j = 0; j <= segments; j++)
                    {
                        float a = Mathf.Lerp(fromDeg, toDeg, j / (float)segments) * Mathf.Deg2Rad;
                        float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                        v.Add(new Vector3(c[i].x * cs, c[i].x * sn, c[i].y));
                        n.Add(new Vector3(nrm.x * cs, nrm.x * sn, nrm.y).normalized);
                        uv.Add(new Vector2(j / (float)segments, total > 0 ? run / total : 0));
                    }
                }
                for (int i = 0; i < c.Length - 1; i++)
                    for (int j = 0; j < segments; j++)
                    {
                        int a = start + i * (segments + 1) + j, b = a + segments + 1;
                        Tri(t, v, n, a, b, b + 1); Tri(t, v, n, a, b + 1, a + 1);
                    }
            }
            return Finish(mesh, v, n, uv, t);
        }

        /// Section caps of a partial revolution: the profile region placed on both cut planes.
        public static Mesh Caps(IList<Vector2[]> curves, float fromDeg, float toDeg, Mesh mesh = null)
        {
            var poly = Outline(curves);
            var tris = Triangulate(poly);
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            foreach (var (deg, outward) in new[] { (fromDeg, -1f), (toDeg, 1f) })
            {
                float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                var normal = new Vector3(-sn, cs, 0) * outward;
                int start = v.Count;
                foreach (var p in poly) { v.Add(new Vector3(p.x * cs, p.x * sn, p.y)); n.Add(normal); uv.Add(new Vector2(p.x, p.y)); }
                for (int i = 0; i < tris.Count; i += 3) Tri(t, v, n, start + tris[i], start + tris[i + 1], start + tris[i + 2]);
            }
            return Finish(mesh, v, n, uv, t);
        }

        /// Tube along a polyline (optic nerve); uv.y runs 0..1 along the path for reveal/flow effects.
        public static Mesh Tube(IList<Vector3> path, float radius, int sides, Mesh mesh = null, float endTaper = 1)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float total = 0; for (int i = 1; i < path.Count; i++) total += Vector3.Distance(path[i], path[i - 1]);
            Vector3 up = Vector3.up; float run = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) run += Vector3.Distance(path[i], path[i - 1]);
                var fwd = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (Mathf.Abs(Vector3.Dot(fwd, up)) > .95f) up = Vector3.right;
                var side = Vector3.Cross(fwd, up).normalized; up = Vector3.Cross(side, fwd).normalized;
                float r = radius * Mathf.Lerp(1, endTaper, run / total);
                for (int j = 0; j <= sides; j++)
                {
                    float a = j * Mathf.PI * 2 / sides;
                    var dir = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    v.Add(path[i] + dir * r); n.Add(dir); uv.Add(new Vector2(j / (float)sides, run / total));
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * (sides + 1) + j, b = a + sides + 1;
                    Tri(t, v, n, a, b, b + 1); Tri(t, v, n, a, b + 1, a + 1);
                }
            return Finish(mesh, v, n, uv, t);
        }

        public static Vector2[] Arc(float centerZ, float radius, float fromDeg, float toDeg, int steps)
        {
            var p = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                p[i] = new Vector2(Mathf.Abs(radius * Mathf.Sin(a)), centerZ + radius * Mathf.Cos(a));
            }
            return p;
        }

        public static Vector2[] Line(Vector2 a, Vector2 b, int steps = 1)
        {
            var p = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++) p[i] = Vector2.Lerp(a, b, i / (float)steps);
            return p;
        }

        static bool OnAxis(Vector2[] c) { foreach (var p in c) if (p.x > 1e-4f) return false; return true; }

        static void Tri(List<int> t, List<Vector3> v, List<Vector3> n, int a, int b, int c)
        {
            var face = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(face, n[a] + n[b] + n[c]) < 0) (b, c) = (c, b);
            t.Add(a); t.Add(b); t.Add(c);
        }

        static Mesh Finish(Mesh mesh, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            mesh ??= new Mesh();
            mesh.Clear();
            mesh.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }

        static List<Vector2> Outline(IList<Vector2[]> curves)
        {
            var poly = new List<Vector2>();
            foreach (var c in curves) foreach (var p in c)
                if (poly.Count == 0 || Vector2.Distance(poly[^1], p) > 1e-5f) poly.Add(p);
            if (poly.Count > 1 && Vector2.Distance(poly[0], poly[^1]) < 1e-5f) poly.RemoveAt(poly.Count - 1);
            return poly;
        }

        public static float SignedArea(IList<Vector2[]> curves)
        {
            var p = Outline(curves); float a = 0;
            for (int i = 0; i < p.Count; i++) { var q = p[(i + 1) % p.Count]; a += p[i].x * q.y - q.x * p[i].y; }
            return a * .5f;
        }

        /// Ear clipping for a simple polygon (profiles are a few hundred points, so O(n²) is fine).
        public static List<int> Triangulate(List<Vector2> poly)
        {
            var idx = new List<int>(); for (int i = 0; i < poly.Count; i++) idx.Add(i);
            float area = 0; for (int i = 0; i < poly.Count; i++) { var q = poly[(i + 1) % poly.Count]; area += poly[i].x * q.y - q.x * poly[i].y; }
            if (area < 0) idx.Reverse();
            for (int i = idx.Count - 1; i >= 0 && idx.Count > 3; i--)   // collinear points can never be ears
                if (Mathf.Abs(Cross(poly[idx[(i + idx.Count - 1) % idx.Count]], poly[idx[i]], poly[idx[(i + 1) % idx.Count]])) < 1e-9f) idx.RemoveAt(i);
            var tris = new List<int>();
            for (int guard = 0; idx.Count > 3 && guard < poly.Count * poly.Count; guard++)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    if (Cross(poly[a], poly[b], poly[c]) <= 1e-9f) continue;
                    bool inside = false;
                    foreach (var k in idx)
                        if (k != a && k != b && k != c && InTri(poly[k], poly[a], poly[b], poly[c])) { inside = true; break; }
                    if (inside) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c); idx.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) break; // degenerate remainder; drop it rather than loop forever
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return tris;
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c) => Cross(a, b, p) >= 0 && Cross(b, c, p) >= 0 && Cross(c, a, p) >= 0;
    }
}
