using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static class MisterHEyeImport
    {
        const string Folder = "Assets/HumanEye/ThirdParty/MisterH/";
        [Serializable] class Source { public Surface[] meshes; }
        [Serializable] class Surface { public string name; public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles; }

        [MenuItem("Tools/Human Eye/Authoring/Apply supplied MisterH eye")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var source = JsonUtility.FromJson<Source>(File.ReadAllText("Tools/HumanEye/MisterH_Eye.json"));
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>();
            foreach (var data in source.meshes)
            {
                string path = Folder + data.name + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (!mesh) { mesh = new Mesh { name = "MisterH " + data.name }; AssetDatabase.CreateAsset(mesh, path); }
                mesh.Clear(); mesh.vertices = data.vertices; mesh.normals = data.normals; mesh.uv = data.uv; mesh.triangles = data.triangles;
                mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                path = Folder + data.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(Shader.Find("HumanEye/Anatomy")); AssetDatabase.CreateAsset(material, path); }
                material.SetColor("_BaseColor", data.name == "cornea" ? new Color(.12f, .14f, .16f) : Color.white);
                material.SetTexture("_BaseMap", data.name == "cornea" ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Image_0.png"));
                material.SetFloat("_Gloss", data.name == "cornea" ? .97f : .55f);
                material.SetFloat("_Specular", data.name == "cornea" ? .9f : .25f);
                material.SetFloat("_F0", .04f); material.SetFloat("_Cull", 2);
                material.SetFloat("_Rim", .08f); EditorUtility.SetDirty(material);
                foreach (var eye in new[] { stage.eye, stage.otherEye })
                {
                    var part = eye.Get(data.name);
                    var child = part.transform.Find("MisterH exterior");
                    if (!child) { child = new GameObject("MisterH exterior").transform; child.SetParent(part.transform, false); }
                    var filter = child.GetComponent<MeshFilter>(); if (!filter) filter = child.gameObject.AddComponent<MeshFilter>();
                    var renderer = child.GetComponent<MeshRenderer>(); if (!renderer) renderer = child.gameObject.AddComponent<MeshRenderer>();
                    filter.sharedMesh = mesh; renderer.sharedMaterial = material;
                    part.exterior = new Renderer[] { renderer };
                    EditorUtility.SetDirty(part);
                }
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Debug.Log("MISTERH_EYE_IMPORTED: 5312 triangles, four addressable exterior regions on both eyes.");
        }
    }
}
