using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HumanEye.Tests
{
    public class CinematicTests
    {
        string backup;
        [SetUp] public void Backup() { backup = PlayerPrefs.GetString(SaveSystem.Key, ""); }
        [TearDown] public void Restore() { PlayerPrefs.SetString(SaveSystem.Key, backup); Clock.FastForward = 1; Clock.Paused = false; }
        static void Capture(string name)
        {
            System.IO.Directory.CreateDirectory("Logs/cinematic");
            var cam = Camera.main; var previous = cam.targetTexture; var active = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            System.IO.File.WriteAllBytes("Logs/cinematic/" + name + ".png", texture.EncodeToPNG());
            cam.targetTexture = previous; RenderTexture.active = active;
            Object.Destroy(texture); RenderTexture.ReleaseTemporary(rt);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator SkipReplayScanAndNavigationRestoreOwnedState()
        {
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>(); var runner = stage.runner;
            foreach (var id in new[] { "sclera", "iris", "pupil", "cornea" })
            {
                Assert.AreEqual(1, stage.eye.Get(id).exterior.Length, "Supplied model must be saved on " + id);
                var mesh = stage.eye.Get(id).exterior[0].GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices; var normals = mesh.normals; var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    Assert.GreaterOrEqual(Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]), normals[a] + normals[b] + normals[c]), -.00001f, "Imported face winding must agree with its normals");
                }
            }
            stage.Enter(runner.chapters[1]); stage.eye.SetCutInstant(false); yield return null;
            var shell = stage.eye.Get("sclera");
            Assert.IsTrue(shell.exterior[0].enabled);
            Assert.AreEqual(1, shell.exterior[0].material.GetFloat("_Alpha"));
            Capture("misterh-exterior");
            stage.eye.SetCutInstant(true); yield return null;
            Assert.IsFalse(shell.exterior[0].enabled, "Imported surface must reveal the teaching cutaway");
            Assert.AreEqual(1, shell.main[0].material.GetFloat("_Alpha"));
            Capture("misterh-cutaway");
            stage.Enter(runner.chapters[3]); stage.eye.SetCutInstant(false); yield return null;
            Assert.IsFalse(stage.eye.Get("iris").exterior[0].enabled, "Static model must not obscure the animated pupil lesson");
            stage.hud.ReturnToMenu(); yield return null;
            Assert.AreEqual(0, stage.entrance.OpenAmount, "Menu starts at the closed gate");
            Assert.IsFalse(stage.brainRoot.gameObject.activeSelf);
            SaveSystem.Data.settings.reducedMotion = false; SaveSystem.Data.settings.skipCameraMotion = false;
            foreach (float beat in new[] { .1f, 3f, 7f })
            {
                stage.hud.StartModule(0); yield return null; yield return null;
                float end = Time.realtimeSinceStartup + beat;
                while (Time.realtimeSinceStartup < end) yield return null;
                Assert.IsTrue(stage.IntroPlaying);
                Capture("intro-" + beat.ToString(System.Globalization.CultureInfo.InvariantCulture));
                stage.SkipIntro(); yield return null; yield return null;
                Assert.IsFalse(stage.IntroPlaying);
                Assert.AreEqual(1, stage.entrance.OpenAmount);
                Assert.Greater(stage.displayLight.intensity, 0);
                Assert.AreEqual(0, stage.introVolume.weight);
                Assert.IsTrue(stage.human.gameObject.activeSelf);
                Assert.IsTrue(stage.niko.gameObject.activeSelf, "Niko has entered the lab when the intro ends");
                yield return null; Capture("human-reveal");
                stage.hud.ReturnToMenu(); yield return null;
                Assert.AreEqual(0, stage.entrance.OpenAmount);
            }
            SaveSystem.Data.settings.reducedMotion = true;
            stage.hud.StartModule(0); Clock.FastForward = 20;
            float limit = Time.realtimeSinceStartup + 25;
            while (runner.Awaiting != "scan" && Time.realtimeSinceStartup < limit) { stage.narrator.Continue(); yield return null; }
            Assert.AreEqual("scan", runner.Awaiting);
            stage.navigation.Enter(); yield return null;
            Assert.IsTrue(stage.navigation.Active); Assert.IsFalse(stage.cams.brain.enabled);
            var target = new Vector3(0, 0, .6f);
            for (int i = 0; i < 180; i++)
            {
                var direction = target - stage.navigation.transform.position; direction.y = 0;
                stage.navigation.body.Move(direction.normalized * .1f + Vector3.down * .01f);
            }
            var distance = stage.navigation.transform.position - target; distance.y = 0;
            Assert.Greater(distance.magnitude, 1.8f, "Platform collision must stop the walker");
            runner.SetPaused(true); yield return null;
            Assert.AreEqual(Vector2.zero, stage.navigation.joystick.Value);
            runner.SetPaused(false);
            stage.UseAbility("SCAN"); stage.UseAbility("SCAN");
            Assert.IsFalse(stage.navigation.Active); Assert.IsTrue(stage.cams.brain.enabled);
            Assert.AreEqual(0, stage.hud.labels.Count, "Duplicate scan must not add labels");
            for (float end = Time.realtimeSinceStartup + 6; stage.cams.Active?.name != "S02_EyeInSkull" && Time.realtimeSinceStartup < end;) yield return null;
            Assert.AreEqual("S02_EyeInSkull", stage.cams.Active?.name, "Scan must travel to the eye close-up");
            yield return null; yield return null;   // the brain applies the cut
            var socket = stage.human.eyeRight.position;
            var crown = new Vector3(socket.x, stage.human.transform.position.y + stage.human.headTop * stage.human.transform.lossyScale.y, socket.z);
            var eyeView = Camera.main.WorldToViewportPoint(socket); var crownView = Camera.main.WorldToViewportPoint(crown);
            Assert.Greater(eyeView.z, 0); Assert.Greater(crownView.z, 0);
            Assert.IsTrue(eyeView.x > .3f && eyeView.x < .7f && eyeView.y > .3f && eyeView.y < .7f, "Scan close-up must centre the live eye socket, got " + eyeView);
            Assert.IsTrue(crownView.y > 0 && crownView.y < 1, "Top of the head must stay in the scan frame, got " + crownView);
            Capture("scan-eye-in-skull");
            stage.hud.ReturnToMenu(); yield return null;
            Assert.IsFalse(stage.IntroPlaying); Assert.IsFalse(stage.navigation.Active);
            Assert.AreEqual(0, stage.entrance.OpenAmount);
            Assert.IsFalse(stage.human.scanPlane.gameObject.activeSelf);
        }
    }
}
