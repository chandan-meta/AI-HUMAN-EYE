using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Cinematic pass step (7 October 2026): the lighting hierarchy. Six lights under the "HE Display Lights" group
    /// (stage.observatory), found or created by name and set to absolute values, so re-runs never compound:
    ///   Face Key       — warm soft-shadowed spot from camera-left-above on the scan subject's head (the portrait key of the intro and the face shot);
    ///   Eye Key        — the hero light: warm soft-shadowed spot from camera-left-above of the hero point, the main corneal catchlight;
    ///   Eye Rim        — cool spot from behind-right-above: separates the eye from the hall;
    ///   Eye Fill       — low cool point from camera-right: the shadow side of the anatomy stays readable;
    ///   Brain Rim      — cool spot behind-above the brain, off until the optic-nerve sequence (S2 switches it);
    ///   Interior Light — warm point at the hero point, intensity 0 and off until the interior reveal (S1 ramps it).
    /// Shadow casters are exactly Key Light, Display Light, Face Key and Eye Key; every other realtime light loses its
    /// shadows. The six lights stay out of stage.roomLights (the intro's room ramp) and the hall levels of the look pass
    /// are re-applied, so the look and cinematic passes leave the same hall whichever runs last.
    /// Called by Builder.ApplyCinematic (Builder.Cinematic.cs).
    public static partial class Builder
    {
        static void CinematicLights()
        {
            var group = stage.observatory.transform;   // "HE Display Lights": not under the room root, so never part of roomLights
            var hero = stage.heroPoint.position;
            var head = stage.human.transform.position + Vector3.up * stage.human.headY;   // runtime resets the figure to scale 1
            var brain = Centre(stage.brain, BrainCentre);
            // Camera-relative axes from the cue sheet: the hero shots look along +Z, so camera-left is -X. Derived from the
            // shots so a re-aimed cue sheet keeps the key on the camera-left side.
            Axes(ShotNamed("S02_EyeHero"), hero, out var right, out var fwd);
            var faceShot = ShotNamed("S02_Face"); if (!faceShot) faceShot = ShotNamed("S02_EyeHero");
            Axes(faceShot, head, out var faceRight, out var faceFwd);
            var up = Vector3.up;

            var faceKey = Lamp(group, "Face Key", LightType.Spot, new Color(1f, .95f, .88f), 10, 7, head + (-faceRight * 1.3f + up * 1.5f - faceFwd * 1.6f), head, 50, 35);
            SoftShadows(faceKey, .85f);
            var eyeKey = Lamp(group, "Eye Key", LightType.Spot, new Color(1f, .97f, .92f), 14, 6, hero + (-right * 1.1f + up * 1.35f - fwd * 1.25f), hero, 40, 28);
            SoftShadows(eyeKey, 1f);
            var eyeRim = Lamp(group, "Eye Rim", LightType.Spot, new Color(.6f, .8f, 1f), 9, 6, hero + (right * .9f + up * 1.1f + fwd * 1.4f), hero, 45, 32);
            var eyeFill = Lamp(group, "Eye Fill", LightType.Point, new Color(.8f, .85f, .95f), 1.6f, 4, hero + (right * 1.5f + up * .25f - fwd * 1.1f), hero);
            var brainRim = Lamp(group, "Brain Rim", LightType.Spot, new Color(.55f, .75f, 1f), 10, 6, brain + (fwd * 1.5f + up * 1.5f + right * .4f), brain, 50, 36, false);
            var interior = Lamp(group, "Interior Light", LightType.Point, new Color(1f, .9f, .75f), 0, 1.2f, hero, hero, 0, 0, false);

            stage.faceKey = faceKey; stage.eyeKey = eyeKey; stage.eyeRim = eyeRim; stage.eyeFill = eyeFill; stage.brainRim = brainRim; stage.interiorLight = interior;
            var mine = new HashSet<Light> { faceKey, eyeKey, eyeRim, eyeFill, brainRim, interior };
            if (stage.roomLights != null) stage.roomLights = stage.roomLights.Where(l => l && !mine.Contains(l)).ToArray();

            // Shadow casters: Key Light, Display Light, Face Key, Eye Key only (every other realtime light is cheaper without).
            var casters = new HashSet<Light> { stage.keyLight, stage.displayLight, faceKey, eyeKey };
            int stripped = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Rectangle || l.type == LightType.Disc || casters.Contains(l) || l.shadows == LightShadows.None) continue;
                l.shadows = LightShadows.None; EditorUtility.SetDirty(l); stripped++;
            }
            foreach (var l in casters) if (l && l.shadows == LightShadows.None) { l.shadows = LightShadows.Soft; EditorUtility.SetDirty(l); }

            ApplyHallLevels();   // Builder.Look.cs: key .85, roomBoost 1.5, Flat ambient colour (each number documented there)
            Debug.Log($"HE_CINEMATIC_LIGHTS faceKey {faceKey.transform.position:F2}->{head:F2} eyeKey {eyeKey.transform.position:F2}->{hero:F2} " +
                      $"eyeRim {eyeRim.transform.position:F2} eyeFill {eyeFill.transform.position:F2} brainRim {brainRim.transform.position:F2}->{brain:F2} " +
                      $"interior {interior.transform.position:F2}; shadows stripped from {stripped} light(s); roomLights {(stage.roomLights == null ? 0 : stage.roomLights.Length)}");
        }

        static CinemachineCamera ShotNamed(string name) => cams.shots.FirstOrDefault(s => s && s.name == name);

        /// Camera-right and camera-forward (flattened) of the shot that frames `subject`; the stage's +X/+Z when no shot exists.
        static void Axes(CinemachineCamera shot, Vector3 subject, out Vector3 right, out Vector3 forward)
        {
            forward = Vector3.forward; right = Vector3.right;
            if (!shot) return;
            var f = subject - shot.transform.position; f.y = 0;
            if (f.sqrMagnitude < 1e-4f) return;
            forward = f.normalized; right = Vector3.Cross(Vector3.up, forward);
        }

        /// Find-or-create a light by name under `parent` and set every value absolutely (idempotent on re-runs). No shadows
        /// unless SoftShadows() follows.
        static Light Lamp(Transform parent, string name, LightType type, Color colour, float intensity, float range, Vector3 position, Vector3 lookAt, float outerAngle = 0, float innerAngle = 0, bool on = true)
        {
            var t = parent.Find(name);
            if (!t) { t = new GameObject(name).transform; t.SetParent(parent, false); }
            var l = t.GetComponent<Light>(); if (!l) l = t.gameObject.AddComponent<Light>();
            l.type = type; l.color = colour; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None; l.enabled = on;
            if (type == LightType.Spot) { l.spotAngle = outerAngle; l.innerSpotAngle = innerAngle; }
            t.position = position;
            if ((lookAt - position).sqrMagnitude > 1e-6f) t.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            EditorUtility.SetDirty(l); EditorUtility.SetDirty(t);
            return l;
        }

        /// Soft shadows with the light's own bias: the PC asset's .1 / .5 texel biases suit the room, not shells a few
        /// millimetres apart, so a little more depth and normal bias keeps the nested eye layers free of acne.
        static void SoftShadows(Light l, float strength)
        {
            l.shadows = LightShadows.Soft; l.shadowStrength = strength; l.shadowNearPlane = .2f;
            l.shadowBias = .4f; l.shadowNormalBias = .6f;
            var data = l.GetUniversalAdditionalLightData(); data.usePipelineSettings = false;
            EditorUtility.SetDirty(data); EditorUtility.SetDirty(l);
        }

        /// World centre of a part from its mesh bounds (valid while the object is inactive, unlike Renderer.bounds).
        static Vector3 Centre(AnatomyPart part, Vector3 fallback)
        {
            if (!part || part.main == null) return fallback;
            bool any = false; var b = new Bounds();
            foreach (var r in part.main)
            {
                var mf = r ? r.GetComponent<MeshFilter>() : null;
                if (!mf || !mf.sharedMesh) continue;
                var lb = mf.sharedMesh.bounds; var m = r.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var p = m.MultiplyPoint3x4(new Vector3((c & 1) == 0 ? lb.min.x : lb.max.x, (c & 2) == 0 ? lb.min.y : lb.max.y, (c & 4) == 0 ? lb.min.z : lb.max.z));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return any ? b.center : fallback;
        }
    }
}
