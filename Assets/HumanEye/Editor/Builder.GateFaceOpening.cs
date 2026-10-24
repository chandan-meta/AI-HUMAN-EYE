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
        [MenuItem("Tools/Human Eye/Authoring/Apply gate-to-face opening (keeps scene edits)", priority = 16)]
        public static void ApplyGateFaceOpening()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            stage.bustDistance = 1.15f; stage.bustLookDrop = .13f; stage.bustFov = 35;
            stage.human.reveal.shoulder = stage.human.neckY - .08f;
            stage.human.reveal.fadeWidth = .08f;
            OpeningBlink(); OpeningMagic(); MuscleLook();
            foreach (var name in new[] { "Rocketbox_head", "Rocketbox_body" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Generated/Materials/" + name + ".mat");
                if (!material) throw new InvalidOperationException("Missing portrait material: " + name);
                material.SetColor("_BaseColor", name == "Rocketbox_head" ? new Color(.8f, .78f, .76f) : new Color(.65f, .65f, .65f));
                material.SetFloat("_AboveBodyFade", name == "Rocketbox_head" ? 1 : 0);
                EditorUtility.SetDirty(material);
            }
            foreach (var obj in new Object[] { stage, stage.human, stage.human.reveal }) EditorUtility.SetDirty(obj);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_GATE_FACE_APPLIED");
        }

        /// Rack 02 as supplied: the full-resolution albedo and normal atlases (export_rack_muscle_maps.py; the 13 October 2048
        /// atlas halved every tile and dropped the normals) under a near-white tint, so the pale fibrous muscle reads as in the source.
        static void MuscleLook()
        {
            const string albedo = RackPath + "Muscles_Albedo.png", normal = RackPath + "Muscles_Normal.png";
            foreach (var (path, isNormal) in new[] { (albedo, false), (normal, true) })
            {
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!ti) throw new InvalidOperationException("Run Tools/HumanEye/export_rack_muscle_maps.py first: " + path);
                var type = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (ti.maxTextureSize != 4096 || ti.textureType != type) { ti.maxTextureSize = 4096; ti.textureType = type; ti.SaveAndReimport(); }
            }
            var m = Mat("Rack_Muscles"); if (!m) throw new InvalidOperationException("Rack_Muscles material missing (ApplyOpening)");
            m.SetTexture("_BaseMap", Tex(albedo)); m.SetTexture("_BumpMap", Tex(normal)); m.SetFloat("_BumpScale", 1.6f);
            m.SetColor("_BaseColor", new Color(.72f, .68f, .66f)); m.SetFloat("_Wrap", .18f); m.SetFloat("_Specular", .07f);   // neutral, below white: the portrait key otherwise clips the red frontalis to white
            m.SetFloat("_Gloss", .28f); m.SetFloat("_Rim", .05f); m.SetFloat("_F0", .03f);
            EditorUtility.SetDirty(m);
        }

        static void OpeningBlink()
        {
            if (stage.human.transform.Find("Anatomy Skin")) return;   // the ANATOMY.blend skin brings its own Blink (Builder.ApplyAnatomyModels)
            const string path = Root + "ThirdParty/UserModels/Rocketbox/Rocketbox_Blink.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Export the supplied Rocketbox model with --blink first");
            importer.importAnimation = importer.importCameras = importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = true; importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true; importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
            Debug.Log("HE_BLINK_IMPORT " + string.Join(", ", imported.Select(m => m.name + " shapes=" + m.blendShapeCount)));
            var source = imported.Single(m => m.blendShapeCount == 1);
            var old = stage.human.skin.main[0];
            var filter = old.GetComponent<MeshFilter>();
            var previous = filter ? filter.sharedMesh : ((SkinnedMeshRenderer)old).sharedMesh;
            if (Vector3.Distance(previous.bounds.center, source.bounds.center) > .001f ||
                Vector3.Distance(previous.bounds.size, source.bounds.size) > .001f)
                throw new InvalidOperationException("Blink derivative changed the character's rest geometry bounds");
            var mesh = SaveMesh(Object.Instantiate(source), "Rocketbox_Blink");
            var materials = old.sharedMaterials;
            var body = old.gameObject;
            int headSlot = Array.FindIndex(materials, m => m.name == "Rocketbox_head");
            var vertices = mesh.vertices;
            float lowestHead = mesh.GetIndices(headSlot).Min(i => stage.human.transform.InverseTransformPoint(body.transform.TransformPoint(vertices[i])).y);
            if (lowestHead < stage.human.reveal.shoulder)
                throw new InvalidOperationException("The opaque face material must stay entirely above the shoulder fade");
            var face = old as SkinnedMeshRenderer;
            if (!face)
            {
                Object.DestroyImmediate(old); Object.DestroyImmediate(filter);
                face = body.AddComponent<SkinnedMeshRenderer>();
            }
            face.sharedMesh = mesh; face.sharedMaterials = materials;
            face.localBounds = mesh.bounds; face.updateWhenOffscreen = true;
            stage.human.skin.main[0] = face; stage.human.blinkingSkin = face; stage.human.SetBlink(0);
            EditorUtility.SetDirty(face); EditorUtility.SetDirty(stage.human.skin);
        }

        /// Layer changes are the reference clip's electric breakup (14 October 2026): the motes emit from the departing layer's
        /// mesh surface (DissolveMote.shader keeps the ones born on breaking patches, from their Custom1 birth stamp), the
        /// burst turns cyan, the old scan ring goes, a soft blue haze quad sits behind the bust and the crackle sound joins in.
        /// The emitter stays under the observatory group (Stage pauses it) and rides the departing layer at run time.
        static void OpeningMagic()
        {
            var ps = stage.anatomyMotes; var human = stage.human; var reveal = human.reveal;
            if (!ps || !stage.anatomyTransition) throw new InvalidOperationException("The saved transition particles are missing");
            var cyan = new Color(.45f, .9f, 1f, .9f); var blue = new Color(.25f, .5f, 1f, .85f);
            var main = ps.main;
            main.maxParticles = 3000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.005f, .016f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.03f, .09f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(cyan, 0), new GradientColorKey(cyan, .6f), new GradientColorKey(blue, .65f), new GradientColorKey(blue, 1) },
                new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(.9f, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Mesh; shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.scale = Vector3.one; shape.position = Vector3.zero; shape.rotation = Vector3.zero; shape.normalOffset = 0; shape.alignToDirection = false;
            var skinRenderer = human.skin.main[0];
            shape.mesh = skinRenderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : skinRenderer.GetComponent<MeshFilter>().sharedMesh;
            var noise = ps.noise; noise.enabled = true; noise.strength = .03f; noise.frequency = 1.6f; noise.scrollSpeed = .5f; noise.octaveCount = 1;
            var velocity = ps.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World; velocity.y = .07f; velocity.orbitalY = .8f;
            var trails = ps.trails; trails.enabled = true; trails.ratio = .2f; trails.lifetime = .16f;
            trails.minVertexDistance = .005f; trails.dieWithParticles = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .6f, 1, 0));
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("HumanEye/DissolveMote")) { name = "IntroDissolveMotes" };
            mat.SetColor("_Color", Color.white); mat.SetTexture("_MainTex", ProceduralTextures.Soft()); mat.SetFloat("_Intensity", 3f); mat.SetFloat("_Band", .1f);
            renderer.sharedMaterial = renderer.trailMaterial = Upsert(mat, Gen + "Materials/IntroDissolveMotes.mat");
            var streams = new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1XYZW };
            renderer.SetActiveVertexStreams(streams); renderer.SetActiveTrailVertexStreams(streams);
            var burst = stage.anatomyTransition.main;
            burst.startSize = new ParticleSystem.MinMaxCurve(.004f, .012f); burst.maxParticles = 80;
            burst.startColor = new Color(.6f, .9f, 1f, .7f);   // no gold at the layer changes: gold is the neural signal's colour
            var burstMaterial = stage.anatomyTransition.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            if (burstMaterial) { burstMaterial.SetColor("_Color", new Color(.6f, .9f, 1f, .7f)); EditorUtility.SetDirty(burstMaterial); }
            var ring = stage.observatory.transform.Find("Anatomy scan ring");
            if (ring) Object.DestroyImmediate(ring.gameObject);   // the flat sweep line is gone: the breakup is patchy
            if (!glowShader) glowShader = Shader.Find("HumanEye/Glow");   // this entry point runs without the full builder's shader cache
            var hazeNode = human.transform.Find("Anatomy breakup haze");
            var hazeGo = hazeNode ? hazeNode.gameObject : Obj("Anatomy breakup haze", human.transform, Quad("AnatomyHazeQuad"), null);
            var hazeMaterial = Glow("IntroBreakupHaze", new Color(.2f, .55f, 1f, 1f), ProceduralTextures.Soft(), 1.1f);
            hazeMaterial.SetFloat("_Alpha", 0); EditorUtility.SetDirty(hazeMaterial);
            var face = human.transform.InverseTransformDirection(human.eyeRight.forward); face.y = 0; face.Normalize();
            var head = human.transform.InverseTransformPoint((human.eyeLeft.position + human.eyeRight.position) * .5f);
            hazeGo.transform.localPosition = head - face * .35f - Vector3.up * .05f;
            hazeGo.transform.localRotation = Quaternion.LookRotation(face);
            hazeGo.transform.localScale = new Vector3(1.3f, 1.3f, 1);
            var hazeRenderer = hazeGo.GetComponent<MeshRenderer>();
            hazeRenderer.sharedMaterial = hazeMaterial; hazeRenderer.enabled = false; hazeRenderer.receiveShadows = false;
            hazeRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            reveal.haze = hazeRenderer; reveal.motes = ps;
            // Tuned on the 1600x900 player captures (round 23): the patch is the record of these values.
            reveal.breakupAmplitude = .45f; reveal.breakupFrequency = 20; reveal.breakupSeed = 7; reveal.filamentReach = .09f;
            reveal.breakupColor = new Color(.15f, .8f, 1f); reveal.breakupIntensity = 2.2f; reveal.moteRate = 4000; reveal.hazePeak = .8f;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/fx_crackle.wav");
            if (clip)
            {
                var effects = stage.sfx.effects.ToList(); effects.RemoveAll(e => e.key == "crackle");
                effects.Add(new AudioDirector.Sfx { key = "crackle", clip = clip }); stage.sfx.effects = effects.ToArray();
                EditorUtility.SetDirty(stage.sfx);
            }
            else Debug.LogWarning("crackle sfx missing (run python Tools/HumanEye/generate_reference_sfx.py)");
            foreach (var o in new Object[] { ps, renderer, stage.anatomyTransition, hazeGo, hazeRenderer, reveal }) EditorUtility.SetDirty(o);
        }
    }
}
