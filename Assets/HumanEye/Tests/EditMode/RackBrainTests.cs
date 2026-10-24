using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HumanEye.Tests
{
    public class RackBrainTests
    {
        [Test]
        public void SavedBrainsHaveValidUvNormalsMaterialsAndWinding()
        {
            EditorSceneManager.OpenScene(EditorTools.Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>();
            foreach (var part in new[] { stage.brain, stage.visualArea, stage.human.brain })
            foreach (var renderer in part.main)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(mesh.vertexCount, mesh.uv.Length, part.id + " UVs");
                Assert.AreEqual(mesh.vertexCount, mesh.normals.Length, part.id + " normals");
                Assert.AreEqual(mesh.vertexCount, mesh.tangents.Length, part.id + " tangents");
                Assert.AreEqual(mesh.subMeshCount, renderer.sharedMaterials.Length, part.id + " material slots");
                Assert.IsTrue(renderer.sharedMaterials.All(m => m && m.shader.name == "HumanEye/Anatomy" && m.GetTexture("_BumpMap")), "URP tissue and normal maps");
                int wrong = 0, count = 0; var v = mesh.vertices; var n = mesh.normals; var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    var normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]); if (normal.sqrMagnitude < 1e-15f) continue;
                    count++; if (Vector3.Dot(normal.normalized, (n[a] + n[b] + n[c]).normalized) < -.2f) wrong++;
                }
                Assert.Less(wrong, count * .01f, part.id + " source normals must agree with triangle winding");
            }
        }
    }
}
