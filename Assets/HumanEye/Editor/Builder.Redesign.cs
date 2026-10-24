using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        /// The 22 October 2026 redesign (owner brief: a flowing, visually rich module): Niko's cap, the vision-journey HUD,
        /// the accommodation rig, the eye-awakens opening and Free Explore. Additive and idempotent; the final patch, after
        /// ApplyRodConeModels.
        [MenuItem("Tools/Human Eye/Authoring/Apply redesign (keeps scene edits)", priority = 22)]
        public static void ApplyRedesign()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");   // this entry point runs without Build's cache
            NikoCap(stage.niko);
            CiliaryRing();
            FreeExploreShot();
            UiJourney.Apply(stage.hud);
            InsetBlur();
            BuildData();   // chapters, questions and the dialogue database with its supplemental lines
            ClosingKeys();   // the closing line's mouth keys follow the regenerated voice
            EditorUtility.SetDirty(stage);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_REDESIGN_APPLIED");
        }

        // ---------------- Free Explore ----------------
        /// The learner-driven view (Stage.FreeExplore moves it every frame): a cue-sheet camera like the others, with a near
        /// clip small enough to sit inside the eye.
        static void FreeExploreShot()
        {
            var eye = stage.eye.transform.position;
            WowPatch.Shot(stage, Stage.OrbitShot, eye + new Vector3(-.4f, .5f, -2.2f), eye, 40);
            var shot = stage.cams.shots.First(s => s.name == Stage.OrbitShot);
            var lens = shot.Lens; lens.NearClipPlane = .005f; shot.Lens = lens;
            EditorUtility.SetDirty(shot); EditorUtility.SetDirty(stage.cams);
        }

        /// The retinal-image popup blurs with the focus error (UIBlur.shader, driven by Hud).
        static void InsetBlur()
        {
            var mat = new Material(Shader.Find("HumanEye/UIBlur")) { name = "UIBlur_RetinaInset" };
            mat = Upsert(mat, Gen + "Materials/UIBlur_RetinaInset.mat");
            var img = stage.hud.Element("inset-retina-image").GetComponent<UnityEngine.UI.RawImage>();
            img.material = mat; EditorUtility.SetDirty(img);
        }

        // ---------------- accommodation ----------------
        /// The ciliary muscle: a ring on the kept half just outside the lens equator (eye units), section caps where the eye
        /// is cut. CiliaryRig narrows it with the accommodation and lights it while it works.
        static void CiliaryRing()
        {
            var eye = stage.eye.transform;
            var old = eye.Find("Ciliary"); if (old) Object.DestroyImmediate(old.gameObject);
            var profile = new List<Vector2[]> { Enumerable.Range(0, 25).Select(i => new Vector2(.395f, .8f) + new Vector2(.05f * Mathf.Cos(i * Mathf.PI / 12), .075f * Mathf.Sin(i * Mathf.PI / 12))).ToArray() };
            var mat = Anatomy("Eye_Ciliary", new Color(.86f, .42f, .48f), m =>
            {
                m.SetFloat("_Gloss", .45f); m.SetFloat("_Specular", .2f); m.SetFloat("_Wrap", .55f); m.SetFloat("_Rim", .05f);
                m.SetFloat("_InteriorDark", 1); m.SetFloat("_Cull", 0); m.SetFloat("_HighlightTint", .25f);
            });
            var root = new GameObject("Ciliary").transform; root.SetParent(eye, false);
            var ring = Obj("Ring", root, SaveMesh(Lathe.Revolve(profile, EyeGeometry.KeepFrom, EyeGeometry.KeepTo, 36), "Eye_CiliaryRing"), mat).GetComponent<MeshRenderer>();
            var caps = Obj("Caps", root, SaveMesh(Lathe.Caps(profile, EyeGeometry.KeepFrom, EyeGeometry.KeepTo), "Eye_CiliaryCaps"), mat).GetComponent<MeshRenderer>();
            foreach (var r in new[] { ring, caps }) { r.lightProbeUsage = LightProbeUsage.BlendProbes; r.gameObject.layer = stage.eye.gameObject.layer; }
            var rig = root.gameObject.AddComponent<CiliaryRig>();
            rig.eye = stage.eye; rig.renderers = new Renderer[] { ring, caps };
            rig.focus = Obj("Ciliary Focus", root, pos: new Vector3(0, .395f, .8f)).transform;      // the section facing the cut
            rig.labelAnchor = Obj("Ciliary Label", root, pos: new Vector3(-.02f, .56f, .8f)).transform;
            stage.ciliary = rig;
            EditorUtility.SetDirty(rig); EditorUtility.SetDirty(stage);
        }

        // ---------------- Niko's cap ----------------
        /// A navy investigator cap tilted back on the head bone (the bare eyeball head looked creepy): sized from his own head
        /// vertices (bind pose, so it fits in any pose), its band and visor kept above his raised brows, with a cyan piping
        /// ring and a glowing eye badge. ApplyNikoCharacter rebuilds the model, so it calls this too.
        static void NikoCap(NikoRig rig)
        {
            var old = rig.head.Find("Niko Cap"); if (old) Object.DestroyImmediate(old.gameObject);
            var skin = rig.skin; var mesh = skin.sharedMesh;
            int head = Array.IndexOf(skin.bones, rig.head);
            if (head < 0) throw new InvalidOperationException("Niko's skin is not bound to the head bone");
            var bind = mesh.bindposes[head]; var w = mesh.boneWeights; var verts = mesh.vertices;
            var pts = Enumerable.Range(0, verts.Length).Where(i => w[i].boneIndex0 == head && w[i].weight0 > .6f).Select(i => bind.MultiplyPoint3x4(verts[i])).ToArray();
            var topBone = rig.head.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("HeadTop_End"));
            var up = (topBone ? rig.head.InverseTransformPoint(topBone.position) : Vector3.up).normalized;
            var fwd = Vector3.ProjectOnPlane(rig.head.InverseTransformPoint(rig.head.position + rig.transform.forward), up).normalized;
            var right = Vector3.Cross(up, fwd);
            float Span(Vector3 axis) => pts.Max(p => Vector3.Dot(p, axis)) - pts.Min(p => Vector3.Dot(p, axis));
            float Mid(Vector3 axis) => (pts.Max(p => Vector3.Dot(p, axis)) + pts.Min(p => Vector3.Dot(p, axis))) / 2;
            float R = Span(right) / 2;
            var c = right * Mid(right) + up * Mid(up) + fwd * Mid(fwd);
            // The raised brows (BrowUp at full weight) are the lowest the cap may come at the front.
            int brow = mesh.GetBlendShapeIndex("BrowUp"); var delta = new Vector3[verts.Length];
            mesh.GetBlendShapeFrameVertices(brow, mesh.GetBlendShapeFrameCount(brow) - 1, delta, null, null);
            var browTops = Enumerable.Range(0, verts.Length).Where(i => delta[i].sqrMagnitude > 1e-10f).Select(i => Vector3.Dot(bind.MultiplyPoint3x4(verts[i] + delta[i]) - c, up)).ToArray();
            float browTop = browTops.Length > 0 ? browTops.Max() : .45f * R;

            const float tilt = 15, visorSpan = 70, droop = 18;
            float vw = .42f * R, ct = Mathf.Cos(tilt * Mathf.Deg2Rad), st = Mathf.Sin(tilt * Mathf.Deg2Rad);
            float d = .25f * R;
            for (; d < .85f * R; d += .01f * R)   // the biggest cap whose band and visor tip both clear the brows
            {
                float rb0 = Mathf.Sqrt(R * R - d * d);
                bool band = d * ct + rb0 * st >= browTop + .05f * R;
                bool tip = (d - vw * Mathf.Tan(droop * Mathf.Deg2Rad)) * ct + (rb0 + vw) * st >= browTop + .03f * R;
                if (band && tip) break;
            }
            float rb = Mathf.Sqrt(R * R - d * d), hb = .18f * R, topZ = (R - d) + .1f * R;
            var axis = up * ct - fwd * st; var front = up * st + fwd * ct;

            var cap = new GameObject("Niko Cap").transform; cap.SetParent(rig.head, false);
            cap.localPosition = c + axis * d; cap.localRotation = Quaternion.LookRotation(axis, front);   // local Z: the cap's axis, Y: its front
            var crownProfile = new List<Vector2[]>
            {
                new[] { new Vector2(0, hb), new Vector2(rb * 1.03f, hb) },
                new[] { new Vector2(rb * 1.03f, hb), new Vector2(rb * 1.09f, hb + .4f * (topZ - hb)), new Vector2(rb * 1.15f, topZ - .07f * R), new Vector2(rb * 1.16f, topZ - .035f * R),
                        new Vector2(rb * 1.13f, topZ - .008f * R), new Vector2(rb, topZ + .004f * R), new Vector2(rb * .6f, topZ + .016f * R), new Vector2(0, topZ + .02f * R) },
            };
            float bo = rb * 1.035f, lo = -.005f * R;
            var bandProfile = new List<Vector2[]> { new[] { new Vector2(rb, lo), new Vector2(bo, lo) }, new[] { new Vector2(bo, lo), new Vector2(bo, hb) },
                                                    new[] { new Vector2(bo, hb), new Vector2(rb, hb) }, new[] { new Vector2(rb, hb), new Vector2(rb, lo) } };
            var piping = new List<Vector2[]> { Enumerable.Range(0, 13).Select(i => new Vector2(rb * 1.045f, hb) + .025f * R * new Vector2(Mathf.Cos(i * Mathf.PI / 6), Mathf.Sin(i * Mathf.PI / 6))).ToArray() };
            var cyan = new Color(.15f, .78f, 1f);
            var badgeTex = CapBadgeTexture();
            var parts = new (string name, Mesh mesh, Material mat)[]
            {
                ("Crown", SaveMesh(Lathe.Revolve(crownProfile, 0, 360, 48), "Niko_CapCrown"), Lit("Niko_Cap", new Color(.08f, .17f, .36f), .35f)),
                ("Band", SaveMesh(Lathe.Revolve(bandProfile, 0, 360, 48), "Niko_CapBand"), Lit("Niko_CapBand", new Color(.03f, .05f, .1f), .45f)),
                ("Piping", SaveMesh(Lathe.Revolve(piping, 0, 360, 48), "Niko_CapPiping"), Lit("Niko_CapPiping", cyan, .5f, 0, null, null, cyan * 1.6f)),
                ("Visor", SaveMesh(VisorMesh(rb * 1.03f, vw, visorSpan, droop, .035f * R, 24), "Niko_CapVisor"), Lit("Niko_CapVisor", new Color(.02f, .03f, .06f), .85f)),
                ("Badge", SaveMesh(BadgeMesh(rb, hb, topZ, R), "Niko_CapBadge"), Lit("Niko_CapBadge", Color.white, .5f, 0, badgeTex, null, Color.white * 1.2f, true)),
            };
            parts[4].mat.SetTexture("_EmissionMap", badgeTex); EditorUtility.SetDirty(parts[4].mat);
            foreach (var (name, m, mat) in parts)
            {
                var r = Obj(name, cap, m, mat).GetComponent<MeshRenderer>();
                r.lightProbeUsage = LightProbeUsage.BlendProbes; r.renderingLayerMask |= NikoRenderingLayer;
            }
            foreach (var t in cap.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = rig.gameObject.layer;
            EditorUtility.SetDirty(rig);
            Debug.Log($"HE_NIKO_CAP R={R:F3} browTop={browTop / R:F2}R bandPlane={d / R:F2}R rb={rb / R:F2}R");
        }

        /// The visor: a crescent at the front of the band (deepest in the middle), drooping a little, with an edge.
        static Mesh VisorMesh(float rb, float depth, float halfSpan, float droop, float thick, int seg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var top = new List<(Vector3 i, Vector3 o)>();
            for (int k = 0; k <= seg; k++)
            {
                float s = k / (float)seg, a = Mathf.Lerp(90 - halfSpan, 90 + halfSpan, s) * Mathf.Deg2Rad;
                float dd = depth * Mathf.Pow(Mathf.Max(0, Mathf.Cos((s - .5f) * Mathf.PI)), .7f) + .01f * depth;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                top.Add((dir * rb, dir * (rb + dd) + Vector3.back * (dd * Mathf.Tan(droop * Mathf.Deg2Rad))));
            }
            void Strip(Func<int, Vector3> a, Func<int, Vector3> b, Func<int, Vector3> want)
            {
                int s0 = v.Count;
                for (int k = 0; k <= seg; k++) { v.Add(a(k)); v.Add(b(k)); }
                for (int k = 0; k < seg; k++)
                {
                    int i0 = s0 + 2 * k; var n = want(k);
                    Face(t, v, i0, i0 + 1, i0 + 3, n); Face(t, v, i0, i0 + 3, i0 + 2, n);
                }
            }
            var down = Vector3.back * thick;
            Strip(k => top[k].i, k => top[k].o, k => Vector3.forward);
            Strip(k => top[k].i + down, k => top[k].o + down, k => Vector3.back);
            Strip(k => top[k].o, k => top[k].o + down, k => new Vector3(top[k].o.x, top[k].o.y, 0));
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// One triangle wound so that its face points along `want` (Unity front faces: normal = cross(b - a, c - a)).
        static void Face(List<int> t, List<Vector3> v, int a, int b, int c, Vector3 want)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), want) < 0) (b, c) = (c, b);
            t.Add(a); t.Add(b); t.Add(c);
        }

        /// The badge: a quad on the front of the crown's flare, facing out along it.
        static Mesh BadgeMesh(float rb, float hb, float topZ, float R)
        {
            var lo = new Vector2(rb * 1.03f, hb); var hi = new Vector2(rb * 1.15f, topZ - .07f * R);
            var mid = Vector2.Lerp(lo, hi, .45f); var tan = (hi - lo).normalized; var nrm = new Vector2(tan.y, -tan.x);
            var centre = new Vector3(0, mid.x + .012f * R, mid.y); var n = new Vector3(0, nrm.x, nrm.y); var up = new Vector3(0, tan.x, tan.y);
            var side = Vector3.Cross(up, n).normalized; float h = .15f * R;
            var v = new List<Vector3> { centre - side * h - up * h, centre + side * h - up * h, centre + side * h + up * h, centre - side * h + up * h };
            var t = new List<int>(); Face(t, v, 0, 1, 2, n); Face(t, v, 0, 2, 3, n);
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0);
            bool flip = Vector3.Dot(Vector3.Cross(side, up), n) < 0;   // keep the emblem the right way round seen from the front
            m.SetUVs(0, new List<Vector2> { new(flip ? 1 : 0, 0), new(flip ? 0 : 1, 0), new(flip ? 0 : 1, 1), new(flip ? 1 : 0, 1) });
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// A round navy badge with a cyan rim and a cyan eye (almond lids, iris, pupil, catchlight); clear outside the disc.
        static Texture2D CapBadgeTexture()
        {
            const int N = 128; const string path = Gen + "Textures/NikoCapBadge.png";
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            Color navy = new(.05f, .12f, .26f), cyan = new(.2f, .85f, 1f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + .5f) / N * 2 - 1, v = (y + .5f) / N * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                    var col = r > .96f ? Color.clear : r > .84f ? cyan : navy;
                    if (Mathf.Abs(u) < .78f && Mathf.Abs(Mathf.Abs(v) - .5f * (1 - u * u)) < .06f) col = cyan;
                    if (r < .27f) col = r < .12f ? navy : cyan;
                    if (Vector2.Distance(new Vector2(u, v), new Vector2(.09f, .1f)) < .055f) col = Color.white;
                    tex.SetPixel(x, y, col);
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
