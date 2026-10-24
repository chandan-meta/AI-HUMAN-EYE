using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        const string RackBrainPath = Root + "ThirdParty/UserModels/RackBrain/";

        [MenuItem("Tools/Human Eye/Authoring/Apply visual reference v2 (keeps scene edits)", priority = 15)]
        public static void ApplyReferenceV2()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include); cams = stage.cams;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var subjectPose = stage.human.transform.localToWorldMatrix;
            RackBrains();   // the companion picture it once authored is gone: Niko floats in 3D (ApplyFloatingNiko)
            stage.wideDistance = 12.6f; stage.wideFov = 54;
            stage.fullDistance = 4.9f; stage.fullFov = 40;
            stage.bustDistance = 2.65f; stage.bustLookDrop = .45f; stage.bustFov = 35;
            stage.human.reveal.shoulder = 1.05f; // The owner's latest direction: waist-up transitions only.
            stage.human.reveal.fadeWidth = .12f;
            Tissue("Rack_Muscles", new Color(.67f, .44f, .39f), .09f, .27f, .3f, .06f, new Color(1, .8f, .7f));
            Tissue("Rack_Skeleton", new Color(.83f, .82f, .79f), .07f, .22f, .35f, .05f, Color.white);
            stage.signals.speed = .8f; stage.signals.interval = .8f;
            stage.signals.headWidth = .045f; stage.signals.trailLength = .32f; stage.signals.headLightIntensity = 1.4f;
            var signal = Mat("Signal"); signal.SetColor("_Color", Color.white); signal.SetFloat("_Intensity", 2.3f);
            signal.SetFloat("_ZTest", 8); EditorUtility.SetDirty(signal);
            stage.signals.headLight.color = new Color(1, .79f, .4f); EditorUtility.SetDirty(stage.signals.headLight);
            var sheath = Mat("Eye_OpticNerve");
            if (sheath)
            {
                sheath.SetColor("_BaseColor", new Color(.6f, .57f, .49f)); sheath.SetColor("_Emission", Color.black);
                sheath.SetFloat("_Flow", 0); sheath.SetFloat("_Specular", .04f); sheath.SetFloat("_F0", .025f);
                sheath.SetFloat("_Rim", .03f); sheath.SetFloat("_Wrap", .22f); EditorUtility.SetDirty(sheath);
            }
            var retina = Mat("LocalEye_Retina");
            if (retina) { retina.SetColor("_BaseColor", new Color(1, .7f, .48f)); EditorUtility.SetDirty(retina); }
            if (stage.post.sharedProfile.TryGet(out Bloom bloom)) bloom.intensity.Override(.18f);
            EditorUtility.SetDirty(stage.post.sharedProfile);
            if (subjectPose != stage.human.transform.localToWorldMatrix) throw new InvalidOperationException("Subject moved");
            foreach (var obj in new Object[] { stage, stage.human, stage.human.reveal, stage.signals, stage.hud }) EditorUtility.SetDirty(obj);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            AuditBrainReferences();
            Debug.Log("HUMAN_EYE_REFERENCE_V2_APPLIED");
        }

        static void RackBrains()
        {
            foreach (var path in new[] { RackBrainPath + "Rack06_Brain.fbx", RackBrainPath + "Rack 06.fbx" })
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Missing audited model: " + path);
                importer.importAnimation = importer.importCameras = importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.None; importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.isReadable = true; importer.SaveAndReimport();
            }
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RackBrainPath + "Rack06_Brain.fbx"));
            try
            {
                var filters = source.GetComponentsInChildren<MeshFilter>();
                if (filters.Length != 8) throw new InvalidOperationException("Rack 06 must contain exactly eight extracted brain meshes");
                var origin = Child(source.transform, "BrainCenter").position;
                var front = Child(source.transform, "BrainFront").position - origin;
                var up = Child(source.transform, "BrainUp").position - origin;
                var sourceFrame = Matrix4x4.TRS(origin, Quaternion.LookRotation(front, up), Vector3.one).inverse;
                var materials = new[] {
                    BrainMaterial("Forebrain", "Forebrain_Brain_Diff", "Forebrain_Brain_NM", "Forebrain_AO"),
                    BrainMaterial("Hindbrain", "Hindbrain_Diff", "Hindbrain_NM", "HindBrain_AO"),
                    BrainMaterial("HindbrainCenter", "Hindbrain_Center_Diff", "Hindbrain_Center_NM", "Hindbrain_Center_AO"),
                    BrainMaterial("Midbrain", "Midbrain_Diff", "Midbrain_NM", "Midbrain_AO") };
                var display = stage.brain.main[0]; var visual = stage.visualArea.main[0];
                var displayBounds = WorldBounds(new[] { display, visual });
                var head = stage.human.brain.main[0];
                var headBounds = head.bounds;
                // Re-use the existing mesh assets/GUIDs and transforms: colliders, animations and other referencing prefabs survive.
                FitBrain(filters, sourceFrame, display, displayBounds, Vector3.left, false, "UserBrain_Rest", materials);
                FitBrain(filters, sourceFrame, visual, displayBounds, Vector3.left, true, "UserBrain_Visual", materials);
                FitBrain(filters, sourceFrame, head, headBounds, stage.human.eyeRight.forward, null, "Human_Brain", materials);
                foreach (var part in new[] { stage.brain, stage.visualArea })
                {
                    var bb = WorldBounds(part.main);
                    part.focus.position = bb.center;
                    foreach (var box in part.pick.OfType<BoxCollider>()) { box.center = box.transform.InverseTransformPoint(bb.center); box.size = Vector3.Scale(bb.size, new Vector3(1 / box.transform.lossyScale.x, 1 / box.transform.lossyScale.y, 1 / box.transform.lossyScale.z)); }
                    EditorUtility.SetDirty(part);
                }
                // Older generated previews use these two meshes; keep their original local bounds and share the new anatomy.
                ReplacePreview("Brain", filters, sourceFrame, false); ReplacePreview("Brain_VisualArea", filters, sourceFrame, true);
                ConnectNerve(displayBounds);
                LegacyBrains(filters, sourceFrame, materials);
                Debug.Log($"HE_RACK06 display={display.bounds} head={head.bounds} sourceTriangles={filters.Sum(f => f.sharedMesh.triangles.Length / 3)}");
            }
            finally { Object.DestroyImmediate(source); }
        }

        static void LegacyBrains(MeshFilter[] source, Matrix4x4 frame, Material[] materials)
        {
            const string oldModel = "Assets/_Project/SourceAssets/Anatomy/Brain.fbx";
            void Replace(GameObject root, string prefix)
            {
                foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (AssetDatabase.GetAssetPath(f.sharedMesh) != oldModel) continue;
                    var renderer = f.GetComponent<Renderer>();
                    FitBrain(source, frame, renderer, renderer.bounds, root.transform.forward, null, prefix, materials);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    Debug.Log("HE_RACK06 legacy replacement " + prefix + "/" + f.name);
                }
            }
            const string prefabPath = "Assets/_Project/Prefabs/Human_Layered.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try { Replace(prefab, "Rack06_LegacyHuman"); PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Core_VisionLab.unity", OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects()) Replace(root, "Rack06_LegacyLab");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); UnityEngine.SceneManagement.SceneManager.SetActiveScene(active); }
        }

        static void ConnectNerve(Bounds brainBounds)
        {
            var cutFrame = GameObject.Find("Cut Frame (eye-radius units)").transform;
            var entry = new Vector3(brainBounds.min.x + brainBounds.size.x * .25f,
                brainBounds.center.y + brainBounds.size.y * .12f, brainBounds.center.z);
            var path = EyeGeometry.NervePath(cutFrame.InverseTransformPoint(entry));
            foreach (var name in new[] { "optic_nerve", "optic_nerve_" }) SaveMesh(Lathe.Tube(path, .115f, 18, null, .85f), name);
            SaveMesh(Lathe.Tube(path, .18f, 8), "optic_nerve_Pick");
            var nerve = stage.signals.paths.First(p => p.id == "nerve");
            var world = path.Select(cutFrame.TransformPoint).ToList();
            var destination = stage.visualArea.focus.position;
            world.Add(entry + new Vector3(.12f, .12f, 0));
            world.Add(Vector3.Lerp(entry, destination, .6f) + Vector3.up * .1f); world.Add(destination);
            nerve.space = null; nerve.points = world.ToArray();
            // Three understated fibres share the nerve part's reveal/alpha lifecycle, including skip and the head close-up.
            var part = stage.eye.Get("optic_nerve"); var tube = part.transform.Find("Nerve");
            var material = Anatomy("NerveFibres", new Color(.68f, .4f, .18f), m => {
                m.SetFloat("_RevealUV", 1); m.SetFloat("_RevealFrom", .23f); m.SetFloat("_Specular", .08f); m.SetFloat("_Wrap", .4f); });
            var renderers = part.main.Where(r => !r.name.StartsWith("Nerve fibre ")).ToList();
            for (int i = 0; i < 3; i++)
            {
                string name = "Nerve fibre " + i; var old = part.transform.Find(name);
                var points = path.Select(p => p + new Vector3(.12f, (i - 1) * .045f, 0)).ToList();
                var mesh = SaveMesh(Lathe.Tube(points, .008f, 6), "NerveFibre" + i);
                var go = old ? old.gameObject : Obj(name, part.transform, mesh, material);
                go.transform.localPosition = tube.localPosition; go.transform.localRotation = tube.localRotation; go.transform.localScale = tube.localScale;
                go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = material; renderers.Add(go.GetComponent<Renderer>());
            }
            part.main = renderers.ToArray(); EditorUtility.SetDirty(part);
        }

        static Material BrainMaterial(string name, string albedo, string normal, string ao)
        {
            var npath = RackBrainPath + normal + ".png";
            var importer = AssetImporter.GetAtPath(npath) as TextureImporter;
            importer.textureType = TextureImporterType.NormalMap; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            return Anatomy("Rack06_" + name, new Color(.88f, .61f, .58f), m =>
            {
                m.SetTexture("_BaseMap", Tex(RackBrainPath + albedo + ".png"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(npath)); m.SetFloat("_BumpScale", .55f);
                m.SetTexture("_SpecMap", Tex(RackBrainPath + ao + ".png"));
                m.SetFloat("_RevealUV", 0); m.SetFloat("_F0", .028f); m.SetFloat("_Specular", .06f); m.SetFloat("_Gloss", .3f);
                m.SetFloat("_Wrap", .24f); m.SetFloat("_Rim", .06f); m.SetFloat("_Wet", .02f);
                m.SetColor("_HighlightColor", new Color(1, .73f, .25f));
            });
        }

        static void FitBrain(MeshFilter[] source, Matrix4x4 sourceFrame, Renderer target, Bounds bounds, Vector3 front,
            bool? occipital, string name, Material[] materials)
        {
            var orient = Matrix4x4.Rotate(Quaternion.LookRotation(front, Vector3.up)) * sourceFrame;
            var all = source.SelectMany(f => f.sharedMesh.vertices.Select(v => orient.MultiplyPoint3x4(f.transform.TransformPoint(v)))).ToArray();
            var bb = new Bounds(all[0], Vector3.zero); foreach (var v in all) bb.Encapsulate(v);
            float scale = Mathf.Min(bounds.size.x / bb.size.x, bounds.size.y / bb.size.y, bounds.size.z / bb.size.z);
            var fit = target.transform.worldToLocalMatrix * Matrix4x4.Translate(bounds.center) * Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(-bb.center) * orient;
            var mesh = BrainMesh(source, fit, occipital);
            target.GetComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, name);
            target.sharedMaterials = materials; target.shadowCastingMode = ShadowCastingMode.On;
            EditorUtility.SetDirty(target); EditorUtility.SetDirty(target.GetComponent<MeshFilter>());
        }

        static Mesh BrainMesh(MeshFilter[] filters, Matrix4x4 fit, bool? occipital)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
            var groups = new List<int>[4]; for (int i = 0; i < 4; i++) groups[i] = new();
            foreach (var f in filters)
            {
                if (occipital.HasValue && (f.name == "Occipital_Lobe") != occipital.Value) continue;
                var source = f.sharedMesh;
                if (source.uv.Length != source.vertexCount) throw new InvalidOperationException("Missing UVs: " + f.name);
                var matrix = fit * f.transform.localToWorldMatrix; var normalMatrix = matrix.inverse.transpose;
                int offset = vertices.Count;
                vertices.AddRange(source.vertices.Select(matrix.MultiplyPoint3x4));
                normals.AddRange(source.normals.Select(n => normalMatrix.MultiplyVector(n).normalized)); uv.AddRange(source.uv);
                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    int material = f.name == "HINDBRAIN" ? 1 + sub : f.name.EndsWith("_Lobe") ? 0 : 3;
                    groups[material].AddRange(source.GetTriangles(sub).Select(i => i + offset));
                }
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, subMeshCount = 4 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
            for (int i = 0; i < 4; i++) mesh.SetTriangles(groups[i], i);
            mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }

        static void ReplacePreview(string name, MeshFilter[] source, Matrix4x4 frame, bool occipital)
        {
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(Gen + "Meshes/" + name + ".asset"); if (!old) return;
            var bb = old.bounds;
            var mesh = BrainMesh(source, frame, occipital); var b = mesh.bounds;
            float scale = Mathf.Min(bb.size.x / b.size.x, bb.size.y / b.size.y, bb.size.z / b.size.z);
            mesh.vertices = mesh.vertices.Select(v => (v - b.center) * scale + bb.center).ToArray();
            mesh.RecalculateBounds(); SaveMesh(mesh, name);
        }

        static void AuditBrainReferences()
        {
            var lines = new List<string> { "Rack 06 source derivatives; active scene brain renderers:" };
            foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (f.sharedMesh && (f.sharedMesh.name.Contains("Brain") || f.name.Contains("Brain"))) lines.Add(f.name + " | " + AssetDatabase.GetAssetPath(f.sharedMesh) + " | " + f.sharedMesh.triangles.Length / 3);
            var paths = AssetDatabase.FindAssets("t:Scene").Concat(AssetDatabase.FindAssets("t:Prefab")).Select(AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
            {
                var brains = AssetDatabase.GetDependencies(path).Where(p => p.EndsWith(".asset") && p.Contains("Meshes/") && (p.Contains("Brain") || p.Contains("Rack06_Legacy")));
                foreach (var brain in brains) lines.Add(path + " -> " + brain);
            }
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/rack06-references.txt", lines);
        }
    }
}
