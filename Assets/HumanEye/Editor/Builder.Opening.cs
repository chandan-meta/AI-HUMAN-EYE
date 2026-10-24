using System;
using System.IO;
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
        const string RackPath = Root + "ThirdParty/UserModels/RackAnatomy/";

        [MenuItem("Tools/Human Eye/Authoring/Apply centered anatomy opening (keeps scene edits)", priority = 13)]
        public static void ApplyOpening()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            if (!stage.displayEye) stage.displayEye = null; // The owner's centered layout may omit the optional exhibit eye.
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var human = stage.human;
            var pose = human.transform.localToWorldMatrix;
            var reveal = human.GetComponent<AnatomyRevealController>();
            if (!reveal) reveal = human.gameObject.AddComponent<AnatomyRevealController>();
            reveal.skin = human.skin; reveal.garment = human.garment; reveal.height = human.headTop;
            reveal.skeleton = RackLayer(human, "Skeleton", new Color(.88f, .86f, .81f));
            reveal.muscles = RackLayer(human, "Muscles", new Color(.82f, .78f, .75f));
            reveal.organs = RackLayer(human, "Organs", Color.white);
            reveal.brain = human.brain; reveal.shoulder = human.neckY - .06f;
            human.reveal = reveal;
            stage.timing = Stage.CinematicTiming.Production;
            if (pose != human.transform.localToWorldMatrix) throw new InvalidOperationException("The subject's authored transform changed");
            stage.anatomyTransition = OpeningParticles();

            var audio = stage.sfx;
            if (!audio.introMusic)
            {
                var go = new GameObject("Intro Music"); go.transform.SetParent(audio.transform, false);
                audio.introMusic = go.AddComponent<AudioSource>();
            }
            audio.introMusic.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Music/intro_discovery.wav");
            if (!audio.introMusic.clip) throw new InvalidOperationException("Generate intro_discovery.wav first");
            audio.introMusic.playOnAwake = false; audio.introMusic.loop = false; audio.introMusic.spatialBlend = 0;
            audio.introMusic.volume = 0;
            // The supplied sclera loses its vessels under the older 14-intensity hero key.
            if (stage.eyeKey) { stage.eyeKey.intensity = 5; EditorUtility.SetDirty(stage.eyeKey); }
            if (stage.eyeRim) { stage.eyeRim.intensity = 4; EditorUtility.SetDirty(stage.eyeRim); }
            OpeningTitle();
            // Keep glossy cornea highlights, while broad white sclera and skin retain surface detail.
            foreach (string name in new[] { "Rocketbox_head", "Rocketbox_body", "LocalEye_ScleraOuter" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/" + name + ".mat");
                if (!mat) continue;
                mat.SetFloat("_Specular", .18f); mat.SetFloat("_F0", .028f); mat.SetFloat("_Rim", .06f);
                EditorUtility.SetDirty(mat);
            }
            var sclera = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/LocalEye_ScleraOuter.mat");
            sclera.SetColor("_BaseColor", new Color(.62f, .60f, .58f));
            sclera.SetFloat("_Wrap", .18f); sclera.SetFloat("_Wet", .16f); sclera.SetFloat("_Specular", .10f);
            var iris = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/LocalEye_Iris.mat");
            iris.SetColor("_BaseColor", new Color(.82f, .82f, .82f)); iris.SetFloat("_Wrap", .3f);
            EditorUtility.SetDirty(sclera); EditorUtility.SetDirty(iris);
            var profile = stage.post.sharedProfile;
            if (profile.TryGet(out ColorAdjustments grade)) { grade.contrast.Override(8); grade.saturation.Override(3); }
            if (profile.TryGet(out Bloom bloom)) { bloom.intensity.Override(.16f); bloom.threshold.Override(1.1f); }
            EditorUtility.SetDirty(profile);
            foreach (var o in new Object[] { stage, human, reveal, audio, audio.introMusic }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_OPENING_APPLIED subject={human.transform.position:F4} scale={human.transform.localScale:F4}");
        }

        static ParticleSystem OpeningParticles()
        {
            var ps = IntroParticles(stage.observatory.transform, "Anatomy transition", "IntroAnatomyTransition", ProceduralTextures.Soft(), new Color(1, .9f, .72f, .65f), .9f);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.08f, .18f);
            main.startSize = new ParticleSystem.MinMaxCurve(.006f, .016f); main.maxParticles = 64; main.prewarm = false;
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .23f; shape.radiusThickness = 1;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            EditorUtility.SetDirty(ps);
            return ps;
        }

        static void OpeningTitle()
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                var elements = prefab.GetComponentsInChildren<RectTransform>(true);
                foreach (var spec in new[] { ("intro-title", .945f, 44f), ("intro-subtitle", .904f, 22f), ("intro-rule", .925f, 1f) })
                {
                    var rect = elements.First(r => r.name == spec.Item1);
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, spec.Item2);
                    rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(spec.Item1 == "intro-rule" ? 260 : 720, spec.Item3);
                    var label = rect.GetComponent<UnityEngine.UI.Text>();
                    if (label) label.fontSize = spec.Item1 == "intro-title" ? 36 : 16;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        /// A whole-body layer fitted to the subject by its eye-socket landmarks: `folder` holds `<name>.fbx` plus either one
        /// `<name>_Albedo.png` atlas (the Rack exports) or per-slot `<slot>_Albedo/_Normal/_Spec` maps (the ANATOMY.blend export,
        /// one material slot per source material).
        static AnatomyPart RackLayer(HumanDisplay human, string name, Color tint, string folder = RackPath)
        {
            string path = folder + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer.importAnimation || importer.importCameras || importer.importLights || importer.animationType != ModelImporterAnimationType.None)
            {
                importer.importAnimation = importer.importCameras = importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.None; importer.SaveAndReimport();
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Missing extracted Rack model: " + path);
            var source = Object.Instantiate(prefab);
            try
            {
                var left = Child(source.transform, "EyeSocket_L").position;
                var right = Child(source.transform, "EyeSocket_R").position;
                var mid = (left + right) * .5f;
                var forward = Child(source.transform, "FaceDirection").position - mid; forward.y = 0; forward.Normalize();
                var across = (left - right).normalized;
                var sourceMesh = source.GetComponentInChildren<MeshFilter>();
                var bounds = source.GetComponentInChildren<Renderer>().bounds;
                var bodyBase = source.transform.Find("BodyBase"); var bodyTop = source.transform.Find("BodyTop");
                // Organs have no feet or crown; the export carries inferred full-body registration landmarks.
                float bottom = bodyBase ? bodyBase.position.y : bounds.min.y, top = bodyTop ? bodyTop.position.y : bounds.max.y;
                var targetLeft = human.transform.InverseTransformPoint(human.eyeLeft.position);
                var targetRight = human.transform.InverseTransformPoint(human.eyeRight.position);
                var targetMid = (targetLeft + targetRight) * .5f;
                var targetAcross = (targetLeft - targetRight).normalized;
                var targetForward = human.transform.InverseTransformDirection(human.eyeRight.forward).normalized;
                float ratio = Vector3.Distance(targetLeft, targetRight) / Vector3.Distance(left, right);
                var mesh = Object.Instantiate(sourceMesh.sharedMesh); mesh.name = "Rack" + name + "_Fitted";
                var vertices = mesh.vertices; var normals = mesh.normals;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var world = sourceMesh.transform.TransformPoint(vertices[i]); var d = world - mid;
                    float sy = world.y > mid.y ? (human.headTop - targetMid.y) / (top - mid.y) : targetMid.y / (mid.y - bottom);
                    vertices[i] = targetMid + targetAcross * (Vector3.Dot(d, across) * ratio) + targetForward * (Vector3.Dot(d, forward) * ratio) + Vector3.up * (d.y * sy);
                    var n = sourceMesh.transform.TransformDirection(normals[i]);
                    normals[i] = (targetAcross * (Vector3.Dot(n, across) / ratio) + targetForward * (Vector3.Dot(n, forward) / ratio) + Vector3.up * (n.y / sy)).normalized;
                }
                mesh.vertices = vertices; mesh.normals = normals; mesh.RecalculateBounds(); mesh.RecalculateTangents();
                mesh = SaveMesh(mesh, "Rack" + name + "_Fitted");
                var mats = sourceMesh.GetComponent<Renderer>().sharedMaterials.Select(s => LayerMaterial(folder, name, s ? s.name : name, tint)).ToArray();
                var existing = human.transform.Find("rack_" + name.ToLowerInvariant());
                var part = existing ? existing.GetComponent<AnatomyPart>() : NewPart(human.transform, "rack_" + name.ToLowerInvariant(), name, 0, false, 1, 0);
                var rendered = part.transform.Find("Anatomy");
                if (!rendered) rendered = Obj("Anatomy", part.transform, mesh, mats[0]).transform;
                rendered.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = rendered.GetComponent<MeshRenderer>(); renderer.sharedMaterials = mats;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                part.main = new Renderer[] { renderer }; part.removable = Array.Empty<Renderer>(); part.pick = Array.Empty<Collider>();
                if (!part.focus) part.focus = Anchor(part.transform, "Focus", targetMid);
                if (!part.labelAnchor) part.labelAnchor = part.focus;
                part.gameObject.SetActive(false);
                EditorUtility.SetDirty(part);
                Debug.Log($"HE_RACK {name} triangles={mesh.triangles.Length / 3} slots={mats.Length} bounds={mesh.bounds} registeredEyes={targetMid:F4} ipd={Vector3.Distance(targetLeft,targetRight):F4}");
                return part;
            }
            finally { Object.DestroyImmediate(source); }
        }

        /// One layer material per source slot when the export supplies `<slot>_Albedo` maps; otherwise the single Rack atlas.
        static Material LayerMaterial(string folder, string layer, string slot, Color tint)
        {
            string albedo = new[] { folder + slot + "_Albedo.png", folder + slot + "_Albedo.jpg" }.FirstOrDefault(File.Exists);
            bool supplied = albedo != null;
            if (!supplied) albedo = folder + layer + "_Albedo.png";
            string normal = folder + slot + "_Normal.png", spec = folder + slot + "_Spec.jpg";
            return Anatomy(supplied ? "Anatomy_" + slot : "Rack_" + layer, tint, m =>
            {
                m.SetTexture("_BaseMap", Tex(albedo, slot == "HumanSkin"));
                if (File.Exists(normal)) { m.SetTexture("_BumpMap", NormalTex(normal)); m.SetFloat("_BumpScale", 1); }
                if (File.Exists(spec)) m.SetTexture("_SpecMap", Tex(spec));
                m.SetFloat("_RevealUV", 0); m.SetFloat("_F0", .028f); m.SetFloat("_Specular", .16f);
                m.SetFloat("_Gloss", .3f); m.SetFloat("_Wrap", .3f); m.SetFloat("_Rim", .08f);
                m.SetFloat("_WipeEdge", .008f); m.SetFloat("_Cull", 2);
                if (supplied) AnatomyLook(m, slot);
            });
        }
    }
}
