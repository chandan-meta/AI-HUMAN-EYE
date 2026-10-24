using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanEye.EditorTools
{
    /// Cinematic pass step: the signal pulse look (HDR comet), the head light, the inside-brain leg of the nerve route,
    /// and the follow / ghosted-brain shots. Called by Builder.ApplyCinematic (Builder.Cinematic.cs). Idempotent:
    /// find-or-create by name, absolute values.
    public static partial class Builder
    {
        static readonly Color SignalColour = new(.6f, .96f, 1f, 1);

        static void CinematicNerve()
        {
            var signals = stage.signals;
            if (!signals) { Debug.LogWarning("CinematicNerve: Stage.signals is missing; step skipped"); return; }

            // 1. Comet look: the head is HDR (bloom threshold 1.15) and the pulses draw after the ghosted brain (its
            //    transparent queue is 3000-3003), so a pulse inside the ghost is never hidden by it.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Signal.mat");
            if (!mat) mat = Glow("Signal", SignalColour, null, 4);
            mat.SetColor("_Color", SignalColour); mat.SetFloat("_Intensity", 4); mat.SetFloat("_Alpha", 1);
            mat.SetFloat("_EdgeFade", 0); mat.SetFloat("_LengthFade", 0); mat.renderQueue = 3100;
            EditorUtility.SetDirty(mat);
            foreach (var l in signals.pool)
            {
                if (!l) continue;
                l.sharedMaterial = mat; l.useWorldSpace = true; l.alignment = LineAlignment.View; l.textureMode = LineTextureMode.Stretch;
                l.numCapVertices = 4; l.numCornerVertices = 2; l.shadowCastingMode = ShadowCastingMode.Off; l.receiveShadows = false;
                l.widthMultiplier = .07f; l.positionCount = 0; l.enabled = false;
                EditorUtility.SetDirty(l);
            }
            signals.speed = .75f; signals.interval = .55f; signals.trailLength = .5f; signals.headWidth = .07f; signals.headLightIntensity = 6;

            // 2. The head light rides the lead nerve pulse (SignalPulses drives position, intensity and enabled).
            var lightT = signals.transform.Find("Signal Head Light");
            if (!lightT) { lightT = new GameObject("Signal Head Light").transform; lightT.SetParent(signals.transform, false); }
            var head = lightT.GetComponent<Light>(); if (!head) head = lightT.gameObject.AddComponent<Light>();
            head.type = LightType.Point; head.color = new Color(.7f, .95f, 1f); head.range = .9f; head.intensity = 6;
            head.shadows = LightShadows.None; head.enabled = false;
            signals.headLight = head;

            // 3. The inside-brain leg. The brain pieces share one vertex array, so Renderer.bounds (and the saved route's
            //    end, 2.30 1.63 1.15) is the whole brain's centre, not the visual area's. Re-aim the last two points at
            //    the occipital piece itself: an arc over the brain's centre ending just inside the mouth of the visual area.
            var nerve = signals.paths.FirstOrDefault(p => p.id == "nerve");
            Vector3 start = Vector3.zero, centre = Vector3.zero, end = Vector3.zero, visual = Vector3.zero; bool haveArea = false;
            if (nerve != null && nerve.points.Length >= 5)
            {
                int n = nerve.points.Length;
                start = nerve.points[0];
                centre = nerve.points[n - 1];
                var brainR = PartRenderer(stage.brain); var areaR = PartRenderer(stage.visualArea);
                if (brainR) centre = brainR.bounds.center;
                var area = UsedWorldVertices(areaR);
                end = centre;
                if (area.Count > 0)
                {
                    visual = Vector3.zero; foreach (var v in area) visual += v; visual /= area.Count;
                    var d = visual - centre; float len = d.magnitude;
                    if (len > .05f)
                    {
                        d /= len;
                        float mouth = area.Min(v => Vector3.Dot(v - centre, d));   // nearest extent of the occipital piece along the route
                        end = centre + d * Mathf.Clamp(mouth + .02f, len * .25f, len * .9f);
                        haveArea = true;
                    }
                }
                var entryLeg = nerve.points[n - 3];                                  // first point inside the brain (kept)
                nerve.points[n - 2] = Vector3.Lerp(entryLeg, end, .5f) + Vector3.up * .2f;
                nerve.points[n - 1] = end;
                EditorUtility.SetDirty(signals);
            }
            else Debug.LogWarning("CinematicNerve: 'nerve' path missing or too short; route left unchanged");

            // 4. Shots. S09_Follow is code-driven (Stage.Follow); its authored pose is only the first frame of the ride.
            //    S09_BrainInside frames the visual area with the inside route arriving from the lower left.
            WowPatch.Shot(stage, Stage.FollowShotName, start + new Vector3(0, .25f, -Stage.FollowDistance), start, 34);
            var look = haveArea ? Vector3.Lerp(centre, visual, .6f) : centre;
            WowPatch.Shot(stage, Stage.BrainInsideShot, look + new Vector3(.15f, .33f, -1.42f), look, 30);
            stage.followShot = stage.cams.shots.First(s => s.name == Stage.FollowShotName);
            EditorUtility.SetDirty(stage); EditorUtility.SetDirty(stage.cams);
            Debug.Log($"HE_NERVE route end {end} visual area centre {visual} brain centre {centre} shots {Stage.FollowShotName}/{Stage.BrainInsideShot}");
        }

        static Renderer PartRenderer(AnatomyPart part)
        {
            if (!part || part.main == null || part.main.Length == 0) return null;
            return part.main[0];
        }

        /// World-space vertices a renderer's mesh actually references (the brain pieces share one vertex array).
        static List<Vector3> UsedWorldVertices(Renderer r)
        {
            var list = new List<Vector3>();
            var filter = r ? r.GetComponent<MeshFilter>() : null;
            var mesh = filter ? filter.sharedMesh : null;
            if (!mesh) return list;
            var verts = mesh.vertices;
            foreach (var i in new HashSet<int>(mesh.triangles)) list.Add(r.transform.TransformPoint(verts[i]));
            return list;
        }
    }
}
