using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Eye proportions in eye-radius units (radius 1 ≈ 12 mm), optical axis +Z (cornea at front). The anterior landmarks
    /// (cornea, iris, pupil, lens, retina, optic disc) are measured from the supplied cross-section model
    /// (Tools/HumanEye/LocalEye_report.json), so rays, picks and the procedural clear media sit on the visible anatomy.
    public static class EyeGeometry
    {
        public const float Limbus = .446f, CorneaCenterZ = .489f, CorneaOuter = .637f, CorneaInner = .594f;
        public const float Sclera = 1f, ScleraIn = .96f, ChoroidIn = .94f, RetinaIn = .925f;
        public const float LensZ = .81f, LensRadius = .256f, LensFront = .145f, LensBack = .127f;
        public const float IrisRoot = .428f, IrisThick = .05f, IrisZ = .935f, PupilMin = .075f, PupilMax = .30f, PupilDefault = .18f;
        public const float KeepFrom = -90, KeepTo = 90, CutFrom = 90, CutTo = 270;   // keep half x ≥ 0; removable half x ≤ 0
        public static readonly Vector3 OpticDisc = new Vector3(0, -.1f, -1).normalized;   // the cross-section's nerve leaves on the cut plane

        static float Phi(float radius, float r) => Mathf.Asin(r / radius) * Mathf.Rad2Deg;
        static Vector2 P(float radius, float phi, float cz = 0) => new(radius * Mathf.Sin(phi * Mathf.Deg2Rad), cz + radius * Mathf.Cos(phi * Mathf.Deg2Rad));

        static List<Vector2[]> Shell(float outer, float inner, float frontPhi, int steps) => new()
        {
            Lathe.Arc(0, outer, 180, frontPhi, steps),
            Lathe.Line(P(outer, frontPhi), P(inner, frontPhi)),
            Lathe.Arc(0, inner, frontPhi, 180, steps),
        };

        public static List<Vector2[]> ScleraProfile()
        {
            float po = Phi(Sclera, Limbus), pi = Phi(ScleraIn, .45f);
            return new() { Lathe.Arc(0, Sclera, 180, po, 44), Lathe.Line(P(Sclera, po), P(ScleraIn, pi)), Lathe.Arc(0, ScleraIn, pi, 180, 44) };
        }

        public static List<Vector2[]> ChoroidProfile() => Shell(ScleraIn, ChoroidIn, 50, 40);
        public static List<Vector2[]> RetinaProfile() => Shell(ChoroidIn, RetinaIn, 58, 40);

        public static List<Vector2[]> CorneaProfile()
        {
            float po = Phi(CorneaOuter, Limbus), pi = Phi(CorneaInner, .45f);
            return new()
            {
                Lathe.Arc(CorneaCenterZ, CorneaOuter, 0, po, 20),
                Lathe.Line(P(CorneaOuter, po, CorneaCenterZ), P(CorneaInner, pi, CorneaCenterZ)),
                Lathe.Arc(CorneaCenterZ, CorneaInner, pi, 0, 20),
            };
        }

        static float IrisFront(float r, float rp)
        {
            float u = Mathf.InverseLerp(rp, IrisRoot, r);
            return IrisZ + .008f * u + .006f * Mathf.Sin(Mathf.PI * u);
        }

        /// The iris is an annulus whose inner edge IS the pupil: the pupil is an opening, not geometry.
        public static List<Vector2[]> IrisProfile(float rp)
        {
            const int n = 14;
            var front = new Vector2[n + 1]; var back = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float r = Mathf.Lerp(rp, IrisRoot, i / (float)n);
                front[i] = new Vector2(r, IrisFront(r, rp));
                back[n - i] = new Vector2(r, IrisFront(r, rp) - IrisThick);
            }
            return new()
            {
                front,
                Lathe.Line(front[n], back[0]),
                back,
                Lathe.Line(back[n], front[0]),
            };
        }

        static Vector2 LensPoint(float t, bool frontSide) =>
            new(LensRadius * Mathf.Sin(t * Mathf.Deg2Rad), LensZ + (frontSide ? LensFront : -LensBack) * Mathf.Cos(t * Mathf.Deg2Rad));

        public static List<Vector2[]> LensProfile()
        {
            var front = new Vector2[17]; var back = new Vector2[17];
            for (int i = 0; i <= 16; i++) { front[i] = LensPoint(i * 90 / 16f, true); back[i] = LensPoint(90 - i * 90 / 16f, false); }
            return new() { front, back };
        }

        public static List<Vector2[]> AqueousProfile()
        {
            const float rp = PupilDefault;
            float pi = Phi(CorneaInner - .004f, .445f);
            var iris = new Vector2[9];
            for (int i = 0; i <= 8; i++) { float r = Mathf.Lerp(IrisRoot - .003f, rp, i / 8f); iris[i] = new Vector2(r, IrisFront(r, rp) + .003f); }
            var lens = new Vector2[9];
            for (int i = 0; i <= 8; i++)
            {
                float r = Mathf.Lerp(rp, 0, i / 8f);
                lens[i] = new Vector2(r, LensZ + LensFront * Mathf.Sqrt(1 - r * r / (LensRadius * LensRadius)) + .004f);
            }
            return new() { Lathe.Arc(CorneaCenterZ, CorneaInner - .004f, 0, pi, 18), Lathe.Line(P(CorneaInner - .004f, pi, CorneaCenterZ), iris[0]), iris, lens };
        }

        public static List<Vector2[]> VitreousProfile()
        {
            var lensBack = new Vector2[13];
            for (int i = 0; i <= 12; i++) { var p = LensPoint(i * 77 / 12f, false); lensBack[i] = new Vector2(p.x, p.y - .006f); }
            float wall = RetinaIn - .005f;
            var wallTop = P(wall, 61);
            return new() { lensBack, Lathe.Line(lensBack[12], wallTop, 4), Lathe.Arc(0, wall, 61, 180, 40) };
        }

        /// Optic nerve centreline: through the cross-section model's nerve stub (disc → sheath end), then a smooth curve
        /// to the end point, eye-local. The tube continues the modelled stub, so the nerve reads as one structure.
        public static readonly Vector3[] NerveStub = { OpticDisc * .9f, new(0, -.045f, -1.2f), new(0, -.08f, -1.8f), new(0, -.28f, -2.55f) };

        public static List<Vector3> NervePath(Vector3 endLocal, int steps = 48)
        {
            const int stub = 12;
            var path = new List<Vector3>();
            for (int i = 0; i < stub; i++)
            {
                float u = i / (float)stub * (NerveStub.Length - 1); int k = Mathf.Min((int)u, NerveStub.Length - 2);
                path.Add(Vector3.Lerp(NerveStub[k], NerveStub[k + 1], u - k));
            }
            var a = NerveStub[^1]; var b = a + (NerveStub[^1] - NerveStub[^2]).normalized * 1.1f;
            var c = Vector3.Lerp(b, endLocal, .5f) + Vector3.down * .25f;
            for (int i = 0; i <= steps - stub; i++)
            {
                float t = i / (float)(steps - stub);
                path.Add((1 - t) * (1 - t) * (1 - t) * a + 3 * (1 - t) * (1 - t) * t * b + 3 * (1 - t) * t * t * c + t * t * t * endLocal);
            }
            return path;
        }
    }
}
