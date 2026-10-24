using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        /// The rendering layer that only Niko and his rim light share (bit 0 is everything's default).
        const int NikoRenderingLayer = 1 << 1;

        /// Floating Niko (16 October 2026 brief): the companion picture card goes (its portrait camera, render texture and the
        /// niko-presenter / niko-portrait UI) and the presenter on the HUD becomes NikoFloatingCompanion, the 3D Niko beside the
        /// lens. Adds his floating dressing under the Niko root ("Niko Float …", kept by ApplyNikoCharacter): a soft hover glow,
        /// a few motes, a rim light on his own rendering layer, and an emission slot over his albedo that the companion lifts
        /// while he floats. Additive and idempotent; the last patch, after ApplyAudioOverhaul.
        [MenuItem("Tools/Human Eye/Authoring/Apply floating Niko (keeps scene edits)", priority = 20)]
        public static void ApplyFloatingNiko()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            if (!glowShader) glowShader = Shader.Find("HumanEye/Glow");
            var niko = stage.niko; var root = niko.transform;
            int layer = LayerMask.NameToLayer("HE_Niko");

            // ---- the card, its camera and its texture
            foreach (var c in root.GetComponentsInChildren<Camera>(true).Where(c => c.name == "Niko Portrait Cam").ToArray()) Object.DestroyImmediate(c.gameObject);
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                foreach (var t in prefab.GetComponentsInChildren<RectTransform>(true).Where(t => t.name is "niko-presenter" or "niko-portrait").ToArray())
                    if (t) Object.DestroyImmediate(t.gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            stage.hud.bindings = stage.hud.bindings.Where(b => b.target && b.key is not ("niko-presenter" or "live-image" or "niko-portrait")).ToArray();
            if (AssetDatabase.LoadAssetAtPath<RenderTexture>(Gen + "UI/NikoPortrait.renderTexture")) AssetDatabase.DeleteAsset(Gen + "UI/NikoPortrait.renderTexture");
            var main = stage.hud.mainCamera; main.cullingMask |= 1 << layer;

            // ---- the companion (the presenter's slot on the HUD)
            var companion = stage.hud.GetComponent<NikoFloatingCompanion>();
            if (!companion) companion = stage.hud.gameObject.AddComponent<NikoFloatingCompanion>();
            companion.stage = stage;
            // Tuned on the 1600x900 and 1024x768 player captures (16 October 2026): the patch is the record of these values.
            companion.heightShare = .28f; companion.maxDepth = .9f; companion.glowLift = .05f;

            FloatingNikoSkin(niko);

            // ---- hover glow: a soft cyan disc under his feet (flat, so the lens sees a thin halo)
            var glowNode = root.Find("Niko Float Glow");
            var glowGo = glowNode ? glowNode.gameObject : Obj("Niko Float Glow", root, Quad("NikoFloatGlowQuad"), null);
            glowGo.transform.localPosition = new Vector3(0, .015f, 0); glowGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
            glowGo.transform.localScale = new Vector3(1.1f, 1.1f, 1);
            var glow = glowGo.GetComponent<MeshRenderer>();
            glow.sharedMaterial = Glow("Niko_FloatGlow", new Color(.35f, .8f, 1f, 1), ProceduralTextures.Soft(), 1.3f);
            glow.enabled = false; glow.receiveShadows = false; glow.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // ---- motes: a few slow sparkles around him, in his own space so they travel with him
            var motes = IntroParticles(root, "Niko Float Motes", "Niko_FloatMotes", ProceduralTextures.Soft(), new Color(.65f, .92f, 1f, .8f), 1.8f);
            var pm = motes.main;
            pm.simulationSpace = ParticleSystemSimulationSpace.Local; pm.scalingMode = ParticleSystemScalingMode.Hierarchy; pm.playOnAwake = false;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f); pm.startSpeed = new ParticleSystem.MinMaxCurve(.03f, .09f);
            pm.startSize = new ParticleSystem.MinMaxCurve(.025f, .055f); pm.maxParticles = 24;
            var em = motes.emission; em.rateOverTime = 5;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .55f; sh.radiusThickness = .3f;
            motes.transform.localPosition = new Vector3(0, .62f, 0);
            var lift = motes.velocityOverLifetime; lift.enabled = true; lift.space = ParticleSystemSimulationSpace.Local;
            lift.x = lift.z = new ParticleSystem.MinMaxCurve(0, 0); lift.y = new ParticleSystem.MinMaxCurve(.04f, .1f);   // one curve mode on all three axes
            motes.gameObject.SetActive(true);

            // ---- rim: a soft cool light behind and above him that lights only his rendering layer
            var rimNode = root.Find("Niko Float Rim");
            var rimGo = rimNode ? rimNode.gameObject : new GameObject("Niko Float Rim");
            rimGo.transform.SetParent(root, false);
            var rim = rimGo.GetComponent<Light>(); if (!rim) rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Point; rim.color = new Color(.6f, .86f, 1f); rim.intensity = 1.6f; rim.range = 2.5f; rim.shadows = LightShadows.None;
            rim.lightmapBakeType = LightmapBakeType.Realtime; rim.renderingLayerMask = NikoRenderingLayer; rim.enabled = false;
            var rimData = rim.GetUniversalAdditionalLightData(); rimData.renderingLayers = (uint)NikoRenderingLayer;

            foreach (var t in new[] { glowGo.transform, motes.transform, rimGo.transform }) t.gameObject.layer = layer;
            companion.hoverGlow = glow; companion.motes = motes; companion.rim = rim;
            foreach (var o in new Object[] { companion, niko, niko.skin, glowGo, glow, motes, rim, rimData, main, stage.hud }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_FLOATING_NIKO_APPLIED portraitCams={root.GetComponentsInChildren<Camera>(true).Length} bindings={stage.hud.bindings.Length} " +
                      $"card={(stage.hud.canvas.GetComponentsInChildren<RectTransform>(true).Any(t => t.name == "niko-presenter") ? "present" : "gone")} " +
                      $"emissionGI={niko.skin.sharedMaterial.globalIlluminationFlags} rimLayer={(niko.skin.renderingLayerMask & NikoRenderingLayer) != 0}");
        }

        /// Self-light: an emission slot over his own albedo, black until he floats (the companion lifts it), and his own
        /// rendering layer for the rim. Realtime-emissive GI flags, or URP's material validation strips the keyword.
        /// ApplyNikoCharacter calls it too, since it rebuilds his material and renderer.
        static void FloatingNikoSkin(NikoRig niko)
        {
            var body = niko.skin.sharedMaterial;
            body.EnableKeyword("_EMISSION"); body.SetTexture("_EmissionMap", body.GetTexture("_BaseMap")); body.SetColor("_EmissionColor", Color.black);
            body.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; EditorUtility.SetDirty(body);
            niko.skin.renderingLayerMask |= NikoRenderingLayer;
        }
    }
}
