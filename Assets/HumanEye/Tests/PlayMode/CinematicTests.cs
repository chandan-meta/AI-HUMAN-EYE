using System.Linq;
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
        public IEnumerator SkipReplaySingleScanAndNavigationRestoreOwnedState()
        {
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>(); var runner = stage.runner;
            // The supplied cross-section eye: kept half + section caps + mirrored slide-away half on every detailed structure.
            foreach (var id in new[] { "sclera", "choroid", "retina", "iris", "lens", "optic_nerve" })
            {
                var part = stage.eye.Get(id);
                var detail = part.main.Where(r => r.name.StartsWith("Detail ·")).Concat(part.removable).ToArray();
                Assert.IsTrue(part.main.Any(r => r.name.StartsWith("Detail ·")) && part.removable.Length > 0, "Detailed model must be saved on " + id);
                foreach (var r in detail)
                {
                    var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                    // A flipped half shows up as many faces pointing against their normals; slivers in the source don't count.
                    var vertices = mesh.vertices; var normals = mesh.normals; var triangles = mesh.triangles; int flipped = 0, faces = 0;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                        var face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                        if (face.sqrMagnitude < 1e-12f) continue;
                        faces++; if (Vector3.Dot(face.normalized, (normals[a] + normals[b] + normals[c]).normalized) < -.2f) flipped++;
                    }
                    Assert.Less(flipped, Mathf.Max(1, faces / 100), r.name + ": face winding must agree with its normals (" + flipped + " of " + faces + " flipped)");
                }
            }
            Assert.AreEqual(0, stage.eye.Get("sclera").exterior.Length, "The superseded closed-eye surface must not hide the detailed eye");
            stage.Enter(runner.chapters[1]); stage.eye.SetCutInstant(false); yield return null;
            var shell = stage.eye.Get("sclera");
            var cap = shell.main.First(r => r.name.StartsWith("Detail cap ·"));
            Assert.IsTrue(shell.removable.All(r => r.enabled), "Closed eye: the mirrored half completes the globe");
            Assert.IsFalse(cap.enabled, "Closed eye: section caps hidden");
            Capture("detail-closed");
            stage.eye.SetCutInstant(true); yield return null;
            Assert.IsTrue(shell.removable.All(r => !r.enabled), "Cutaway: the mirrored half is gone");
            Assert.IsTrue(cap.enabled, "Cutaway: the section shows its cut faces");
            Assert.AreEqual(1, shell.main[0].sharedMaterial.GetFloat("_Alpha"));
            Capture("detail-cutaway");
            stage.Enter(runner.chapters[3]); stage.eye.SetPupil(.1f, true); yield return null;
            Assert.AreEqual(.1f, stage.eye.Get("iris").main.First(r => r.name.StartsWith("Detail ·")).sharedMaterial.GetFloat("_PupilNow"), .001f, "The detailed iris must follow the live pupil");
            var labAmbient = RenderSettings.ambientLight;
            var labSky = RenderSettings.skybox;
            bool adjustableSky = labSky && labSky.HasProperty("_Exposure");
            float labSkyExposure = adjustableSky ? labSky.GetFloat("_Exposure") : 0;
            stage.hud.ReturnToMenu(); yield return null;
            // The landing has a brighter dedicated composition; starting the lesson restores the dark-adapted opening.
            Assert.IsFalse(stage.human.gameObject.activeSelf, "No human figure on the start screen");
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .001f, "The start screen shows the eye at the display");
            Assert.IsFalse(stage.displayEye && stage.displayEye.gameObject.activeSelf, "The exhibit eye stays folded away");
            Assert.AreEqual("Menu", stage.cams.Active?.name, "The start screen uses its dedicated composition");
            Assert.IsTrue(stage.LandingActive);
            Assert.AreEqual(EyeGeometry.PupilDefault, stage.eye.pupilTarget, .001f);
            Assert.IsFalse(stage.brainRoot.gameObject.activeSelf);
            SaveSystem.Data.settings.reducedMotion = false; SaveSystem.Data.settings.skipCameraMotion = false;
            var dolly = stage.cams.Get("S02_IntroDolly");
            Assert.IsNotNull(dolly, "The opening needs its code-driven dolly");
            float dollyNear = dolly.Lens.NearClipPlane;
            // The production opening, "the eye awakens" (22 October 2026): light arrives and the pupil tightens, the eye opens
            // into its cross-section, light from the apple lands on the retina, the title, the hand-over to Niko's first shot.
            // Skip from late and from early must both land on the same finished state.
            IEnumerator Until(float introTime) { for (float guard = Time.realtimeSinceStartup + 45; stage.IntroPlaying && stage.IntroTime < introTime && Time.realtimeSinceStartup < guard;) yield return null; yield return null; }
            void Finished(string when)
            {
                Assert.IsFalse(stage.IntroPlaying, when);
                Assert.Greater(stage.displayLight.intensity, 0, when);
                Assert.AreEqual(0, stage.introVolume.weight, when + ": intro grade off");
                Assert.IsTrue(!stage.human.gameObject.activeSelf && (stage.NikoWaiting ? !stage.niko.gameObject.activeSelf : stage.narrator.Speaking && stage.niko.gameObject.activeSelf), when + ": no figure, Niko is in only with his first line");
                Assert.AreEqual("S02_NikoMedium", stage.cams.Active?.name, when + ": the opening ends on the first lesson shot");
                Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .001f, when + ": the eye rests on the central display");
                Assert.AreEqual(stage.heroPoint.lossyScale.x, stage.eyeRig.localScale.x, .0005f, when + ": at its lesson size");
                Assert.IsFalse(stage.displayEye && stage.displayEye.gameObject.activeSelf, when + ": no exhibit eye");
                Assert.IsFalse(stage.otherEye.gameObject.activeSelf, when + ": one eye");
                Assert.IsFalse(stage.eye.IsCut, when + ": the eye is whole again");
                Assert.IsFalse(stage.appleRoot.gameObject.activeSelf || stage.rays.gameObject.activeSelf, when + ": the apple and its light are gone");
                Assert.IsTrue(string.IsNullOrEmpty(stage.CineTitle), when + ": no title");
                Assert.AreEqual(1, Shader.GetGlobalFloat("_HE_InteriorDark"), .01f, when + ": dark interior");
                Assert.AreEqual(EyeGeometry.PupilDefault, stage.eye.pupilTarget, .001f, when + ": resting pupil");
                Assert.AreEqual(dollyNear, dolly.Lens.NearClipPlane, .0005f, when + ": dolly lens restored");
                Assert.Less(Vector4.Distance(RenderSettings.ambientLight, labAmbient), .001f, when + ": authored ambient restored");
                Assert.AreSame(labSky, RenderSettings.skybox, when + ": authored sky restored");
                if (adjustableSky) Assert.AreEqual(labSkyExposure, labSky.GetFloat("_Exposure"), .0001f, when + ": saved sky exposure unchanged");
                foreach (var ps in new[] { stage.gateSparks, stage.gateDust, stage.extractMotes, stage.titleBurst, stage.nikoVortex, stage.depthDust, stage.anatomyTransition, stage.anatomyMotes })
                    if (ps) Assert.IsFalse(ps.gameObject.activeSelf, when + ": " + ps.name + " off");
            }
            stage.hud.StartModule(0); yield return null; yield return null;
            yield return Until(.25f);
            Assert.IsTrue(stage.IntroPlaying); Assert.IsFalse(stage.LandingActive); Assert.AreEqual("S02_IntroDolly", stage.cams.Active?.name, "The opening is shot on the code-driven dolly");
            if (adjustableSky)
            {
                Assert.AreNotSame(labSky, RenderSettings.skybox, "The opening dims a private sky copy");
                Assert.AreEqual(labSkyExposure, labSky.GetFloat("_Exposure"), .0001f, "The opening does not alter the saved sky exposure");
            }
            var companion = stage.Companion;
            Assert.IsTrue(stage.NikoWaiting && !stage.niko.gameObject.activeSelf && !companion.Visible, "The opening starts without Niko: his first line brings him in");
            Assert.AreNotEqual(0, stage.hud.mainCamera.cullingMask & (1 << stage.niko.gameObject.layer), "One Niko, drawn in 3D by the lesson camera");
            Assert.IsFalse(stage.human.gameObject.activeSelf, "No human figure in the opening");
            Assert.Greater(stage.eye.PupilRadius, EyeGeometry.PupilMax * .9f, "In the dark the pupil is wide");
            var eyeView = Camera.main.WorldToViewportPoint(stage.eye.transform.position);
            Assert.IsTrue(eyeView.z > 0 && eyeView.x > .25f && eyeView.x < .75f && eyeView.y > .25f && eyeView.y < .75f, "The eye fills the middle of the dark frame, got " + eyeView);
            Capture("intro-dark");
            yield return Until(5f);
            Assert.Less(stage.eye.pupilTarget, EyeGeometry.PupilMin * 1.3f, "Light arrives: the pupil tightens");
            Assert.IsTrue(stage.eyeKey && stage.eyeKey.enabled, "the key light is the arriving light");
            Assert.IsFalse(string.IsNullOrEmpty(stage.IntroCaption), "the pupil's response is captioned");
            Assert.IsTrue(stage.depthDust && stage.depthDust.gameObject.activeSelf && stage.depthDust.isPlaying, "The opening has drifting depth dust");
            Assert.Greater(stage.depthDust.particleCount, 0, "Depth dust contains live particles");
            runner.SetPaused(true); yield return null;
            float pausedIntro = stage.IntroTime, pausedDust = stage.depthDust.time;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(pausedIntro, stage.IntroTime, .001f, "Pause holds the cinematic clock");
            Assert.IsTrue(stage.depthDust.isPaused, "Pause holds the atmospheric particles");
            Assert.AreEqual(pausedDust, stage.depthDust.time, .001f, "Paused atmosphere does not keep simulating");
            runner.SetPaused(false); yield return null;
            Capture("intro-light");
            yield return Until(10.4f);
            Assert.IsTrue(stage.eye.IsCut, "The eye opens into its cross-section");
            Assert.IsTrue(stage.extractMotes && stage.extractMotes.gameObject.activeSelf && stage.extractMotes.isPlaying, "The cutaway has drifting reveal motes");
            Assert.Greater(stage.extractMotes.particleCount, 0, "The reveal motes contain live particles");
            Capture("intro-cutaway");
            yield return Until(14.6f);
            Assert.IsTrue(stage.appleRoot.gameObject.activeSelf && stage.rays.gameObject.activeSelf, "Light from the apple crosses the eye");
            Assert.Greater(stage.rays.reach, 4.9f, "and reaches the retina");
            Assert.Greater(stage.anatomyTransition.particleCount, 0, "A spark where the light lands");
            Capture("intro-retina");
            yield return Until(16.4f);
            Assert.AreEqual("THE HUMAN EYE", stage.CineTitle, "The title follows the light's arrival");
            Assert.IsTrue(stage.titleBurst && stage.titleBurst.gameObject.activeSelf, "The title has its reveal effect");
            Capture("intro-title");
            yield return Until(20.75f);
            Assert.IsFalse(stage.eye.IsCut, "The eye closes for the lesson, as the camera leaves for Niko's shot");
            Capture("intro-handover");
            stage.SkipIntro(); yield return null; yield return null;
            Finished("Skip at the hand-over");
            yield return null; Capture("intro-finished");
            stage.hud.ReturnToMenu(); yield return null;
            stage.hud.StartModule(0); yield return null; yield return null;
            yield return Until(2f);
            stage.SkipIntro(); yield return null; yield return null;
            Finished("Skip in the dark");
            stage.hud.ReturnToMenu(); yield return null;
            // One scan only: after the opening the lesson goes straight on to Explore Eye (no Scan Eye action, no second scan).
            Assert.IsFalse(stage.Ability("SCAN", out _), "There is no Scan Eye action after the opening");
            SaveSystem.Data.settings.reducedMotion = true;
            stage.hud.StartModule(0); Clock.FastForward = 20;
            float limit = Time.realtimeSinceStartup + 40;
            while (runner.ExploreSet == null && Time.realtimeSinceStartup < limit)
            {
                if (stage.IntroPlaying)
                    foreach (var ps in new[] { stage.depthDust, stage.extractMotes, stage.titleBurst })
                        if (ps) Assert.IsFalse(ps.gameObject.activeSelf, "Reduced motion omits " + ps.name);
                stage.narrator.Continue(); yield return null;
            }
            Assert.IsNotNull(runner.ExploreSet, "The lesson reaches Explore Eye without another scan");
            Assert.IsFalse(stage.human.gameObject.activeSelf, "The scan subject is gone after the opening");
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .01f, "The eye waits on the display");
            Assert.IsFalse(stage.displayEye && stage.displayEye.gameObject.activeSelf, "The exhibit eye has handed over");
            Capture("explore-eye");
            // Explore lab: the walker stands on the new floor and the stand's ring stops it.
            stage.navigation.Enter(); yield return null;
            Assert.IsTrue(stage.navigation.Active); Assert.IsFalse(stage.cams.brain.enabled);
            var target = Vector3.zero;
            for (int i = 0; i < 180; i++)
            {
                var direction = target - stage.navigation.transform.position; direction.y = 0;
                stage.navigation.body.Move(direction.normalized * .1f + Vector3.down * .01f);
            }
            var distance = stage.navigation.transform.position - target; distance.y = 0;
            Assert.Greater(distance.magnitude, 3.5f, "The stand's ring must stop the walker");
            Assert.Greater(stage.navigation.transform.position.y, 0, "The walker stands on the lab floor");
            runner.SetPaused(true); yield return null;
            Assert.AreEqual(Vector2.zero, stage.navigation.joystick.Value);
            runner.SetPaused(false);
            stage.navigation.Leave();
            Assert.IsFalse(stage.navigation.Active); Assert.IsTrue(stage.cams.brain.enabled);
            stage.hud.ReturnToMenu(); yield return null;
            Assert.IsFalse(stage.IntroPlaying); Assert.IsFalse(stage.navigation.Active);
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .001f, "Back at the menu the eye waits at the display");
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator EyeToBrainCinematicSyncsAndRestores()
        {
            SceneManager.LoadScene("HumanEye_Observatory"); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<Stage>(); var runner = stage.runner;
            SaveSystem.Data.settings.reducedMotion = false; SaveSystem.Data.settings.skipCameraMotion = false;
            int scene9 = System.Array.FindIndex(runner.chapters, c => c.id == "Scene09");
            stage.hud.StartModule(scene9);
            for (float guard = Time.realtimeSinceStartup + 10; !stage.EyeBrainPlaying && Time.realtimeSinceStartup < guard;) yield return null;
            Assert.IsTrue(stage.EyeBrainPlaying, "Scene 9 opens with the eye-to-brain cinematic");
            Assert.IsTrue(stage.CinematicPlaying, "the interface is letterboxed while it plays");
            for (float guard = Time.realtimeSinceStartup + 40; stage.narrator.Current?.id != "S09_L02" && Time.realtimeSinceStartup < guard;) yield return null;
            Assert.AreEqual("S09_L02", stage.narrator.Current?.id, "The second line plays with the pathway beat");
            // the stages fade in one after another across the beat (the cortex last, at 60 % of it)
            for (float guard = Time.realtimeSinceStartup + 12; !stage.human.pathway.All(p => p.Visible) && stage.EyeBrainPlaying && Time.realtimeSinceStartup < guard;) yield return null;
            Assert.IsTrue(stage.human.gameObject.activeSelf && stage.human.pathway.All(p => p.Visible), "Every stage of the visual pathway is shown");
            Assert.Greater(stage.signals.Live, 0, "Pulses run along the pathway");
            var presenter = stage.Companion;
            Assert.IsNotNull(presenter); Assert.IsTrue(presenter.Visible && presenter.Floating, "Narrated head close-ups keep the floating Niko on screen");
            Assert.Greater(stage.human.brain.main[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, 70000, "The head uses the supplied Rack 06 anatomy");
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.human.eyeRight.position), .01f, "The eye is back in its socket for the pathway");
            Capture("eye-to-brain-pathway");
            stage.SkipIntro();
            for (float guard = Time.realtimeSinceStartup + 5; stage.EyeBrainPlaying && Time.realtimeSinceStartup < guard;) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(stage.EyeBrainPlaying, "Skip ends the cinematic");
            Assert.IsFalse(stage.human.gameObject.activeSelf, "The subject leaves with it");
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.cutPoint.position), .001f, "The eye is back at the cut point");
            Assert.IsTrue(stage.eye.IsCut, "cut open, as the chapter's checkpoint expects");
            Assert.IsTrue(stage.brain.Visible && stage.brainRoot.gameObject.activeSelf, "The brain display is back");
            Assert.AreEqual(0, stage.hud.labels.Count, "No pathway label is left behind");
            Assert.IsTrue(string.IsNullOrEmpty(stage.CineTitle), "No title is left behind");
            stage.hud.ReturnToMenu(); yield return null;
            Assert.IsFalse(stage.EyeBrainPlaying);
        }
    }
}
