using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Intro pass (6 October 2026): the authored lifting gate panel becomes two halves that slide apart (the floor bolt
    /// retracts first), the entrance gets its code-driven dolly camera and a lamp over the gate, Niko's medium shot is
    /// re-aimed so he sits left of centre beside the panels, and the Canvas gets the minimal intro overlay (caption +
    /// Skip) that replaces the full interface during the cinematic. Idempotent; keeps every other scene edit.
    public static class IntroPatch
    {
        const float Clearance = .15f, CapDepth = .06f;

        [MenuItem("Tools/Human Eye/Authoring/Apply intro pass (keeps scene edits)", priority = 6)]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Builder.BuildData();
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var root = GameObject.Find("Cinematic Laboratory").transform;
            SplitGate(stage.entrance, root);
            GateLamp(root);
            WowPatch.Shot(stage, "S02_IntroDolly", Stage.CamStart, Stage.GateLook, Stage.DollyFov);
            // Niko's greeting shot: he sits left of centre so the right-hand column never covers him or his wave, at 4:3 too.
            var medium = stage.cams.shots.First(s => s.name == "S02_NikoMedium");
            WowPatch.Shot(stage, "S02_NikoMedium", medium.transform.position, stage.Anchor("Intro").position + new Vector3(.34f, .65f, .04f), 36);
            Canvas(stage.hud);
            foreach (var o in new Object[] { stage, stage.cams, stage.hud, stage.entrance }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_INTRO_APPLIED");
        }

        // ---------------- gate: two sliding halves + the retracting bolt ----------------
        // The room's door nodes are static-batched in a player build, so a moving node would not move on screen: every
        // leaf is baked into its own world-space, non-static object and the authored nodes are left inactive.
        static void SplitGate(LabEntrance entrance, Transform root)
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var panel = renderers.First(r => r.name.Contains("Door01Up_"));
            var lockBar = renderers.First(r => r.name.Contains("Door01lock_down_"));
            var gate = root.Find("Gate leaves");
            if (!gate) { gate = new GameObject("Gate leaves").transform; gate.SetParent(root, false); }
            gate.SetPositionAndRotation(Vector3.zero, Quaternion.identity); gate.localScale = Vector3.one;

            var m = panel.transform.localToWorldMatrix; var mesh = panel.GetComponent<MeshFilter>().sharedMesh;
            var bounds = WorldBounds(mesh.bounds, m);   // ~4.47 m wide along world z, 5.8 m tall, 0.33 m thick
            var left = Leaf(gate, "GateLeafL", panel, Clip(mesh, m, bounds.center, bounds.center.z, -1), bounds.center);
            var right = Leaf(gate, "GateLeafR", panel, Clip(mesh, m, bounds.center, bounds.center.z, 1), bounds.center);
            Cap(left, panel, bounds, -1); Cap(right, panel, bounds, 1);
            var lm = lockBar.transform.localToWorldMatrix; var lockMesh = lockBar.GetComponent<MeshFilter>().sharedMesh;
            var lockBounds = WorldBounds(lockMesh.bounds, lm);
            var bolt = Leaf(gate, "GateBolt", lockBar, Clip(lockMesh, lm, lockBounds.center, -1e6f, 1), lockBounds.center);
            panel.gameObject.SetActive(false); lockBar.gameObject.SetActive(false);   // replaced by the baked leaves (kept for provenance)

            float travel = bounds.size.z * .5f + Clearance;   // each half clears the opening and sits inside the wall
            entrance.leaves = new[] { left, right, bolt };
            entrance.closed = new[] { left.localPosition, right.localPosition, bolt.localPosition };
            entrance.open = new[] { left.localPosition + Vector3.back * travel, right.localPosition + Vector3.forward * travel, bolt.localPosition + Vector3.down * (lockBounds.size.y + .15f) };
            entrance.boltShare = .25f; entrance.pauseShare = .1f;
            entrance.SetGate(0);
            Debug.Log($"INTRO_GATE halves {bounds.size} travel {travel:0.00} bolt {lockBounds.size} vertices L={left.GetComponent<MeshFilter>().sharedMesh.vertexCount} R={right.GetComponent<MeshFilter>().sharedMesh.vertexCount} bolt={bolt.GetComponent<MeshFilter>().sharedMesh.vertexCount}");
        }

        static Transform Leaf(Transform gate, string name, MeshRenderer source, Mesh baked, Vector3 centre)
        {
            var saved = Builder.SaveMesh(baked, name);
            var t = gate.Find(name);
            if (!t) { t = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)).transform; t.SetParent(gate, false); }
            t.SetPositionAndRotation(centre, Quaternion.identity); t.localScale = Vector3.one;
            t.gameObject.layer = source.gameObject.layer; GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            t.GetComponent<MeshFilter>().sharedMesh = saved; t.GetComponent<MeshCollider>().sharedMesh = saved;
            var mr = t.GetComponent<MeshRenderer>(); mr.sharedMaterials = source.sharedMaterials;
            mr.shadowCastingMode = source.shadowCastingMode; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return t;
        }

        /// The cut reveals the panel's hollow interior: a thin plate closes each half's inner edge.
        static void Cap(Transform half, MeshRenderer source, Bounds bounds, int side)
        {
            var cap = half.Find("Edge cap");
            if (!cap) { cap = GameObject.CreatePrimitive(PrimitiveType.Cube).transform; cap.name = "Edge cap"; cap.SetParent(half, false); }
            cap.localRotation = Quaternion.identity; cap.localPosition = new Vector3(0, 0, side * CapDepth * .5f);
            cap.localScale = new Vector3(bounds.size.x - .01f, bounds.size.y - .01f, CapDepth);
            cap.gameObject.layer = source.gameObject.layer; GameObjectUtility.SetStaticEditorFlags(cap.gameObject, 0);
            var cr = cap.GetComponent<MeshRenderer>(); cr.sharedMaterials = source.sharedMaterials; cr.lightProbeUsage = LightProbeUsage.Off; cr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        /// A lamp over the gate on the corridor side, so the closed gate reads as the subject of the first beats while the
        /// room lights are still down. Not part of `roomLights`, so the intro's room ramp leaves it alone.
        static void GateLamp(Transform root)
        {
            var lamp = root.Find("Gate lamp");
            if (!lamp) { lamp = new GameObject("Gate lamp").transform; lamp.SetParent(root, false); }
            var l = lamp.GetComponent<Light>(); if (!l) l = lamp.gameObject.AddComponent<Light>();
            l.type = LightType.Spot; l.color = new Color(.72f, .84f, 1f); l.intensity = 8; l.range = 14;
            l.spotAngle = 80; l.innerSpotAngle = 45; l.shadows = LightShadows.None;
            lamp.position = new Vector3(-19.4f, 6.3f, .67f);
            lamp.rotation = Quaternion.LookRotation(new Vector3(-17.3f, 2.4f, .67f) - lamp.position);
        }

        static Bounds WorldBounds(Bounds local, Matrix4x4 m)
        {
            var b = new Bounds(m.MultiplyPoint3x4(local.min), Vector3.zero);
            for (int i = 0; i < 8; i++)
                b.Encapsulate(m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? local.min.x : local.max.x, (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z)));
            return b;
        }

        /// The part of a mesh on one side of the world plane z = planeZ (sign +1 keeps z >= planeZ, -1 keeps z <= planeZ),
        /// baked into world units around `origin` so the result sits at `origin` with identity rotation and unit scale.
        /// Triangles crossing the plane are clipped with interpolated normals, uvs and tangents. A plane far outside the
        /// mesh bakes the whole mesh.
        static Mesh Clip(Mesh mesh, Matrix4x4 m, Vector3 origin, float planeZ, float sign)
        {
            var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var tan = mesh.tangents;
            bool hasN = n.Length == v.Length, hasUv = uv.Length == v.Length, hasT = tan.Length == v.Length;
            var normalM = m.inverse.transpose; bool flip = m.determinant < 0;
            var wv = v.Select(p => m.MultiplyPoint3x4(p) - origin).ToArray();
            var wn = hasN ? n.Select(x => normalM.MultiplyVector(x).normalized).ToArray() : null;
            var wt = hasT ? tan.Select(x => { var d = m.MultiplyVector(new Vector3(x.x, x.y, x.z)).normalized; return new Vector4(d.x, d.y, d.z, x.w); }).ToArray() : null;
            var outV = new List<Vector3>(); var outN = new List<Vector3>(); var outUv = new List<Vector2>(); var outT = new List<Vector4>();
            float D(int i) => sign * (wv[i].z + origin.z - planeZ);
            int Add(Vector3 p, Vector3 nn, Vector2 u, Vector4 t) { outV.Add(p); if (hasN) outN.Add(nn); if (hasUv) outUv.Add(u); if (hasT) outT.Add(t); return outV.Count - 1; }
            int Keep(int i) => Add(wv[i], hasN ? wn[i] : default, hasUv ? uv[i] : default, hasT ? wt[i] : default);
            int Cut(int a, int b)
            {
                float k = D(a) / (D(a) - D(b));
                return Add(Vector3.Lerp(wv[a], wv[b], k), hasN ? Vector3.Lerp(wn[a], wn[b], k).normalized : default, hasUv ? Vector2.Lerp(uv[a], uv[b], k) : default, hasT ? Vector4.Lerp(wt[a], wt[b], k) : default);
            }
            var subs = new List<int[]>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tris = mesh.GetTriangles(s); var outTris = new List<int>(); var poly = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    poly.Clear();
                    for (int e = 0; e < 3; e++)   // Sutherland–Hodgman against the half-space D >= 0
                    {
                        int a = tris[i + e], b = tris[i + (e + 1) % 3];
                        bool inA = D(a) >= 0, inB = D(b) >= 0;
                        if (inA) poly.Add(Keep(a));
                        if (inA != inB) poly.Add(Cut(a, b));
                    }
                    for (int k = 2; k < poly.Count; k++)
                    {
                        outTris.Add(poly[0]);
                        outTris.Add(flip ? poly[k] : poly[k - 1]);
                        outTris.Add(flip ? poly[k - 1] : poly[k]);
                    }
                }
                subs.Add(outTris.ToArray());
            }
            var result = new Mesh { indexFormat = outV.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            result.SetVertices(outV); if (hasN) result.SetNormals(outN); if (hasUv) result.SetUVs(0, outUv); if (hasT) result.SetTangents(outT);
            result.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) result.SetTriangles(subs[s], s);
            result.RecalculateBounds();
            return result;
        }

        // ---------------- Canvas: minimal intro overlay, fading main interface ----------------
        static void Canvas(Hud hud)
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                RectTransform Find(string n) => prefab.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == n);
                RectTransform R(string n) => Find(n) ?? throw new Exception("Canvas element missing: " + n);
                RectTransform Ensure(string n, RectTransform parent, params Type[] components)
                {
                    var r = Find(n);
                    if (!r) { r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); }
                    foreach (var t in components) if (!r.GetComponent(t)) r.gameObject.AddComponent(t);
                    r.SetParent(parent, false); return r;
                }
                var safe = R("safe"); var subtitleText = R("subtitle-text").GetComponent<Text>(); var ink = subtitleText.color;
                // The top bar and the column fade as a whole (Hud.Pop needs CanvasGroups).
                foreach (var key in new[] { "topbar", "right-column" }) { var r = R(key); if (!r.GetComponent<CanvasGroup>()) r.gameObject.AddComponent<CanvasGroup>(); }
                // Lab tools keep the 48 px touch target.
                var tools = R("lab-tools").GetComponent<GridLayoutGroup>(); if (tools) tools.cellSize = new Vector2(tools.cellSize.x, 48);

                // Intro overlay: one slim strip along the bottom edge. Caption left, ghost Skip right; the picture stays clear.
                var overlay = Ensure("intro-overlay", safe, typeof(CanvasGroup));
                overlay.anchorMin = Vector2.zero; overlay.anchorMax = new Vector2(1, 0); overlay.pivot = new Vector2(.5f, 0);
                overlay.anchoredPosition = new Vector2(0, 24); overlay.sizeDelta = new Vector2(-64, 56);
                var group = overlay.GetComponent<CanvasGroup>(); group.alpha = 1; group.interactable = group.blocksRaycasts = true;
                var caption = Ensure("intro-caption", overlay, typeof(CanvasRenderer), typeof(Text));
                caption.anchorMin = Vector2.zero; caption.anchorMax = new Vector2(.65f, 1); caption.offsetMin = caption.offsetMax = Vector2.zero;
                var text = caption.GetComponent<Text>(); text.font = subtitleText.font; text.fontSize = 18; text.color = new Color(ink.r, ink.g, ink.b, .82f);
                text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false; text.supportRichText = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; text.text = "OBSERVATORY OF VISION";
                var skip = R("btn-skip-intro"); skip.SetParent(overlay, false);
                skip.anchorMin = skip.anchorMax = new Vector2(1, .5f); skip.pivot = new Vector2(1, .5f);
                skip.anchoredPosition = Vector2.zero; skip.sizeDelta = new Vector2(160, 48);
                skip.GetComponentInChildren<Text>(true).text = "Skip intro";
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var instances = hud.canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var key in new[] { "intro-overlay", "intro-caption" })
                if (!hud.bindings.Any(b => b.key == key)) hud.bindings = hud.bindings.Append(new Hud.Binding { key = key, target = instances.First(r => r.name == key) }).ToArray();
            foreach (var r in instances.Where(r => r.name is "btn-skip-intro" or "intro-overlay" or "intro-caption"))
            {
                PrefabUtility.RevertObjectOverride(r, InteractionMode.AutomatedAction);   // the instance follows the prefab's new placement
                var le = r.GetComponent<LayoutElement>(); if (le) PrefabUtility.RevertObjectOverride(le, InteractionMode.AutomatedAction);
            }
            EditorUtility.SetDirty(hud);
        }
    }
}
