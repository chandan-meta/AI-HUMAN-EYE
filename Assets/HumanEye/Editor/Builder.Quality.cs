using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Visual quality pass (14 October 2026), additive over the saved centered opening: the organ layer without its mouth
    /// and teeth plus the optic pathway, tissue materials, the scan ring and motes, electric-blue signals, a firmer brain,
    /// the grade, and the magnified retina rebuilt with recognisable rods and cones. Idempotent; the subject's transform
    /// is never moved.
    public static partial class Builder
    {
        [MenuItem("Tools/Human Eye/Authoring/Apply visual quality pass (keeps scene edits)", priority = 14)]
        public static void ApplyQuality()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include); cams = stage.cams;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var human = stage.human; var reveal = human.reveal;
            if (!reveal) throw new InvalidOperationException("Apply the centered anatomy opening first (Builder.ApplyOpening)");
            var pose = human.transform.localToWorldMatrix;

            // 1. Layers: the organ mesh re-extracted without the mouth and teeth; the optic pathway joins the organ view;
            //    the fade drops from the shoulders to the lower chest so the bust reads whole.
            reveal.organs = RackLayer(human, "Organs", Color.white);
            reveal.pathway = human.pathway; reveal.shoulder = human.neckY - .42f; reveal.fadeWidth = .25f;

            // 2. Tissue: warm matte muscle with ivory tendons, matte bone, moist organs (no fluorescent coral, no plastic sheen).
            Tissue("Rack_Muscles", new Color(.97f, .86f, .82f), .11f, .3f, .42f, .1f, new Color(1, .8f, .7f));
            Tissue("Rack_Skeleton", new Color(.93f, .89f, .8f), .07f, .22f, .4f, .07f, new Color(1, .95f, .85f));
            Tissue("Rack_Organs", new Color(.96f, .9f, .88f), .16f, .38f, .4f, .1f, new Color(1, .85f, .8f));

            // 3. Brain: deeper coral, shaded sulci, a moderate wet sheen; the visual cortex keeps its blue glow.
            foreach (var name in new[] { "Brain_User", "Brain_VisualArea_User", "Brain" })
            {
                var m = Mat(name); if (!m) continue;
                m.SetColor("_BaseColor", name == "Brain" ? new Color(.86f, .56f, .58f) : new Color(.96f, .72f, .76f));
                m.SetFloat("_Wrap", .3f); m.SetFloat("_Gloss", .42f); m.SetFloat("_Specular", .22f); m.SetFloat("_Rim", .1f); m.SetFloat("_Wet", .18f);
                EditorUtility.SetDirty(m);
            }

            // 4. Signals: electric-blue comets with warm active nodes (the quality brief); the light beams stay warm-white.
            var signal = Mat("Signal");
            if (signal) { signal.SetColor("_Color", new Color(.55f, .9f, 1f)); signal.SetFloat("_Intensity", 2.2f); signal.SetFloat("_ZTest", 8); EditorUtility.SetDirty(signal); }   // ZTest Always: the comet rides inside the opaque nerve tube
            // The "nerve" route is stored in world space and was left behind when the lesson rig moved onto the stand (8 October):
            // re-anchor it to the eye's cut frame (its first point is the modelled nerve stub) so pulses ride the visible nerve.
            var nerve = stage.signals.paths.FirstOrDefault(p => p.id == "nerve");
            var cutFrameGo = GameObject.Find("Cut Frame (eye-radius units)");
            if (nerve != null && nerve.points.Length > 0 && cutFrameGo)
            {
                var delta = cutFrameGo.transform.TransformPoint(EyeGeometry.NerveStub[0]) - nerve.points[0];
                if (delta.magnitude > .01f) { for (int i = 0; i < nerve.points.Length; i++) nerve.points[i] += delta; Debug.Log($"HE_NERVE route re-anchored by {delta:F3}"); }
            }
            else Debug.LogWarning("ApplyQuality: nerve route or cut frame missing; pulses may run off the nerve");
            stage.signals.headWidth = .07f; stage.signals.trailLength = .6f;
            if (stage.signals.headLight) { stage.signals.headLight.color = new Color(.6f, .9f, 1f); EditorUtility.SetDirty(stage.signals.headLight); }
            if (human.pathway != null)
                foreach (var part in human.pathway.Take(5))   // nerve, chiasm, tract, LGN, radiation light up gold as the pulse passes; V1 stays blue
                    foreach (var r in part.main) if (r && r.sharedMaterial) { r.sharedMaterial.SetColor("_HighlightColor", new Color(1, .85f, .55f)); EditorUtility.SetDirty(r.sharedMaterial); }

            // 5. Transition VFX: a thin cyan ring and sparkle motes ride the wipe front; the warm burst remains the gold accent.
            stage.anatomyMotes = OpeningMotes();   // the scan ring of this pass is retired by ApplyGateFaceOpening (electric breakup)

            // 6. Grade: back toward the art boards' contrast without the earlier flattening.
            var profile = stage.post.sharedProfile;
            if (profile.TryGet(out ColorAdjustments grade)) { grade.contrast.Override(12); grade.saturation.Override(8); }
            if (profile.TryGet(out Bloom bloom)) { bloom.intensity.Override(.22f); bloom.threshold.Override(1.0f); }
            EditorUtility.SetDirty(profile);

            // 7. Magnified retina rebuilt with disc-stacked outer segments, inner segments, nuclei and synaptic terminals.
            RebuildMicro(stage);

            if (pose != human.transform.localToWorldMatrix) throw new InvalidOperationException("The subject's authored transform changed");
            foreach (var o in new Object[] { stage, human, reveal, stage.signals }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_QUALITY_APPLIED subject={human.transform.position:F4} organs={reveal.organs.main[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3}");
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/" + name + ".mat");

        static void Tissue(string name, Color tint, float specular, float gloss, float wrap, float rim, Color rimColour)
        {
            var m = Mat(name); if (!m) return;
            m.SetColor("_BaseColor", tint); m.SetFloat("_Specular", specular); m.SetFloat("_Gloss", gloss); m.SetFloat("_Wrap", wrap);
            m.SetFloat("_Rim", rim); m.SetColor("_RimColor", rimColour); m.SetFloat("_F0", .025f);
            EditorUtility.SetDirty(m);
        }

        /// Sparkle motes along the wipe front: cyan with a quarter of gold, drifting up a little, emitted only while a wipe runs.
        static ParticleSystem OpeningMotes()
        {
            var cyan = new Color(.62f, .95f, 1f, .85f); var gold = new Color(1f, .82f, .5f, .75f);
            var ps = IntroParticles(stage.observatory.transform, "Anatomy scan motes", "IntroAnatomyMotes", ProceduralTextures.Soft(), cyan, 2f);
            var main = ps.main;
            main.duration = 1; main.maxParticles = 400; main.prewarm = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .16f);
            main.startSize = new ParticleSystem.MinMaxCurve(.015f, .03f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(cyan, 0), new GradientColorKey(cyan, .72f), new GradientColorKey(gold, .78f), new GradientColorKey(gold, 1) },
                new[] { new GradientAlphaKey(.85f, 0), new GradientAlphaKey(.85f, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(.5f, .02f, .35f);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            EditorUtility.SetDirty(ps);
            return ps;
        }
    }
}
