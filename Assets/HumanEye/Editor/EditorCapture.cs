using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Batch visual check without a player build: opens the launch scene and enters Play mode; the -heAutopilot flag
    /// makes ModuleBoot add the AutoPilot, which renders camera frames to PNG (no UI overlay in batch mode) and exits.
    /// Unity.exe -batchmode -projectPath . -executeMethod HumanEye.EditorTools.EditorCapture.Run -heAutopilot -heChapter 0 -heStop 1 -heFast 4 -heCapture <dir>
    public static class EditorCapture
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            EditorApplication.EnterPlaymode();
        }

        /// Diagnostic renders of the scan subject's head, one anatomy layer at a time (edit mode, no Play).
        public static void Head()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var human = Object.FindFirstObjectByType<HumanDisplay>(FindObjectsInactive.Include);
            human.gameObject.SetActive(true);
            var eye = human.eyeRight.position;
            var cam = new GameObject("diag cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray; cam.fieldOfView = 30; cam.nearClipPlane = .01f;
            var light = new GameObject("diag light").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(30, 20, 0);
            var layers = new (string name, Renderer r)[] { ("skin", human.skin.main[0]), ("muscles", human.muscles.main[0]), ("skull", human.skull.main[0]), ("brain", human.brain.main[0]) };
            Directory.CreateDirectory("Builds/Checks/head");
            foreach (var (view, offset) in new[] { ("front", new Vector3(0, .02f, -.7f)), ("side", new Vector3(.7f, .02f, .02f)), ("body", new Vector3(0, -.6f, -3.2f)) })
                foreach (var show in new[] { "skin", "muscles", "skull", "brain", "skull+brain" })
                {
                    foreach (var l in layers) l.r.enabled = show.Contains(l.name);
                    cam.transform.position = eye + offset; cam.transform.LookAt(view == "body" ? eye + Vector3.down * .7f : eye + new Vector3(.03f, 0, .05f));
                    var rt = new RenderTexture(800, 800, 24); cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt; var tex = new Texture2D(800, 800, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); tex.Apply();
                    File.WriteAllBytes($"Builds/Checks/head/{view}_{show}.png", tex.EncodeToPNG());
                    RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
                }
            foreach (var r in human.GetComponentsInChildren<Renderer>(true)) Debug.Log($"HE_HEAD {r.name} bounds={r.bounds}");
        }
    }
}
