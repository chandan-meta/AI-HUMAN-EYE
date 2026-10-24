using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    // Isolated material comparison: never saves changes to the production scene.
    public static class PearlIrisPreview
    {
        const string Folder = "Assets/HumanEye/PearlIrisPreview/";
        const string Output = "Docs/HumanEye/PearlIrisRefresh/";

        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            foreach (var name in new[] { "BaseColor", "Normal", "SpecularMask" })
            {
                string path = Folder + "EyeInterior_" + name + ".png";
                File.Copy(Output + "EyeInterior_" + name + "_1024.png", path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = name == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = name == "BaseColor";
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/HumanEye/Generated/Materials/LocalEye_Retina.mat");
            if (!original || !original.shader.isSupported) throw new InvalidOperationException("Supported retina material required");
            var candidate = new Material(original) { name = "Retina_PearlIris_Candidate" };
            candidate.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "EyeInterior_BaseColor.png"));
            candidate.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "EyeInterior_Normal.png"));
            candidate.SetTexture("_SpecMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "EyeInterior_SpecularMask.png"));
            candidate.SetColor("_BaseColor", new Color(1f, .72f, .68f));
            candidate.SetFloat("_BumpScale", .12f);
            candidate.SetFloat("_Gloss", .38f);
            candidate.SetFloat("_Specular", .16f);
            candidate.SetFloat("_Wet", .18f);
            string matPath = Folder + "Retina_PearlIris_Candidate.mat";
            var saved = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (saved) { EditorUtility.CopySerialized(candidate, saved); Object.DestroyImmediate(candidate); candidate = saved; }
            else AssetDatabase.CreateAsset(candidate, matPath);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            if (!stage || !stage.eye || !stage.cutPoint) throw new InvalidOperationException("Eye and cutaway pose required");
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.enabled = false;
            stage.eyeRig.gameObject.SetActive(true);
            stage.eye.gameObject.SetActive(true);
            if (stage.human) stage.human.gameObject.SetActive(false);
            if (stage.otherEye) stage.otherEye.gameObject.SetActive(false);
            stage.eyeRig.SetPositionAndRotation(stage.cutPoint.position, stage.cutPoint.rotation);
            stage.eyeRig.localScale = stage.cutPoint.localScale;
            stage.eye.transform.localPosition = Vector3.zero;
            stage.eye.transform.localRotation = Quaternion.identity;
            stage.eye.transform.localScale = Vector3.one;
            foreach (var part in stage.eye.parts)
            {
                foreach (var r in part.removable) if (r) r.enabled = false;
                foreach (var r in part.main) if (r) r.enabled = true;
                foreach (var r in part.exterior) if (r) r.enabled = false;
                // Edit-mode rendering needs the same blending that AnatomyPart configures during Play.
                foreach (var r in part.main.Where(r => r))
                    r.sharedMaterials = r.sharedMaterials.Select(m =>
                    {
                        if (!m) return m;
                        var copy = new Material(m) { name = m.name };
                        bool transparent = part.baseAlpha < .995f;
                        copy.SetFloat("_Alpha", part.baseAlpha);
                        copy.SetFloat("_SrcBlend", transparent ? 5 : 1);
                        copy.SetFloat("_DstBlend", transparent ? 10 : 0);
                        copy.SetFloat("_ZWrite", transparent ? 0 : 1);
                        copy.renderQueue = (transparent ? 3000 : 2000) + part.queueOffset;
                        return copy;
                    }).ToArray();
            }
            foreach (var r in stage.eye.caps) if (r) r.enabled = true;
            Shader.SetGlobalFloat("_HE_InteriorDark", 0);
            var renderers = stage.eye.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterials.Any(m => m && m.name == original.name)).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No source retina renderer found");
            var camera = new GameObject("Pearl Iris preview camera").AddComponent<Camera>();
            camera.nearClipPlane = .01f; camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var name in new[] { "S03_Lens_Close", "S06_LensClose" })
            {
                var shot = all.FirstOrDefault(t => t.name == name);
                var cm = shot ? shot.GetComponent<Unity.Cinemachine.CinemachineCamera>() : null;
                if (!cm) throw new InvalidOperationException("Missing camera " + name);
                camera.transform.SetPositionAndRotation(shot.position, shot.rotation);
                camera.fieldOfView = cm.Lens.FieldOfView;
                foreach (bool useCandidate in new[] { false, true })
                {
                    foreach (var r in renderers)
                        r.sharedMaterials = r.sharedMaterials.Select(m => m && (m.name == original.name || m == candidate) ? (useCandidate ? candidate : original) : m).ToArray();
                    Render(camera, Output + "Unity_" + name + (useCandidate ? "_Candidate.png" : "_Original.png"));
                }
                var vitreous = stage.eye.Get("vitreous");
                if (vitreous)
                {
                    var visible = vitreous.main.Where(r => r && r.enabled).ToArray();
                    foreach (var r in visible) r.enabled = false;
                    Render(camera, Output + "Unity_" + name + "_NoVitreous_Diagnostic.png");
                    foreach (var r in visible) r.enabled = true;
                }
            }
            Debug.Log("PEARL_IRIS_PREVIEW_PASS retinaRenderers=" + renderers.Length + " shader=" + original.shader.name);
        }

        static void Render(Camera camera, string path)
        {
            var rt = new RenderTexture(1920, 1080, 24);
            var previous = RenderTexture.active;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            }
        }
    }
}
