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
            stage.hud.ReturnToMenu(); yield return null;
            Assert.AreEqual(0, stage.entrance.OpenAmount, "Menu starts at the closed gate");
            Assert.IsFalse(stage.brainRoot.gameObject.activeSelf);
            SaveSystem.Data.settings.reducedMotion = false; SaveSystem.Data.settings.skipCameraMotion = false;
            var dolly = stage.cams.Get("S02_IntroDolly");
            Assert.IsNotNull(dolly, "The opening needs its code-driven dolly");
            float dollyNear = dolly.Lens.NearClipPlane;
            // The production opening: closed gate → straight face approach → blink → muscles → organs → eye → Niko.
            // Checkpoints on the intro clock (production timing), then Skip from late and from early in the opening must both
            // land on the same finished state.
            IEnumerator Until(float introTime) { for (float guard = Time.realtimeSinceStartup + 45; stage.IntroPlaying && stage.IntroTime < introTime && Time.realtimeSinceStartup < guard;) yield return null; yield return null; }
            void BodyFade(AnatomyPart part, bool enabled, string when)
            {
                Assert.IsNotNull(part, when + ": anatomy layer is saved");
                foreach (var renderer in part.main)
                    foreach (var material in renderer.sharedMaterials)
                        if (enabled)
                        {
                            var fade = material.GetVector("_BodyFade");
                            Assert.AreEqual(1, fade.z, .001f, when + ": lower body is fully masked");
                            float knee = stage.human.transform.TransformPoint(Vector3.up * .5f).y;
                            Assert.Greater(fade.x - fade.y, knee, when + ": the transition cannot expose the legs");
                        }
                        else Assert.AreEqual(0, material.GetVector("_BodyFade").z, .001f, when + ": body fade is cleared");
            }
            Vector3? layerCameraPosition = null;
            void Portrait(string when)
            {
                if (layerCameraPosition.HasValue)
                    Assert.Less(Vector3.Distance(Camera.main.transform.position, layerCameraPosition.Value), .005f,
                        when + ": the anatomy layers share a steady composition");
                layerCameraPosition = Camera.main.transform.position;
                var human = stage.human;
                var points = new[] {
                    human.eyeRight.position,
                    human.transform.TransformPoint(Vector3.up * human.headTop),
                    human.transform.TransformPoint(new Vector3(-.21f, human.neckY - .05f, 0)),
                    human.transform.TransformPoint(new Vector3(.21f, human.neckY - .05f, 0)),
                };
                foreach (var point in points)
                {
                    var view = Camera.main.WorldToViewportPoint(point);
                    Assert.Greater(view.z, 0, when + ": the head and shoulders stay in front of the camera");
                    Assert.IsTrue(view.x > .1f && view.x < .9f && view.y > .13f && view.y < .89f,
                        when + ": head and shoulders stay inside the letterbox, got " + view);
                }
                Assert.Less(Camera.main.WorldToViewportPoint(human.transform.position).y, .13f,
                    when + ": the face portrait excludes the lower body");
                Assert.Less(Camera.main.WorldToViewportPoint(human.transform.TransformPoint(Vector3.up)).y, .13f,
                    when + ": the waist stays outside the shoulder/head portrait");
            }
            void Finished(string when)
            {
                Assert.IsFalse(stage.IntroPlaying, when);
                Assert.AreEqual(1, stage.entrance.OpenAmount, when + ": gate open");
                Assert.Greater(stage.displayLight.intensity, 0, when);
                Assert.AreEqual(0, stage.introVolume.weight, when + ": intro grade off");
                Assert.IsTrue(!stage.human.gameObject.activeSelf && (stage.NikoWaiting ? !stage.niko.gameObject.activeSelf : stage.narrator.Speaking && stage.niko.gameObject.activeSelf), when + ": the subject has gone, Niko is in only with his first line");
                Assert.AreEqual("S02_NikoMedium", stage.cams.Active?.name, when + ": the opening ends on the first lesson shot");
                Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .001f, when + ": the eye rests on the central display");
                Assert.AreEqual(stage.heroPoint.lossyScale.x, stage.eyeRig.localScale.x, .0005f, when + ": at its lesson size");
                Assert.IsFalse(stage.displayEye && stage.displayEye.gameObject.activeSelf, when + ": the exhibit eye has handed over");
                Assert.IsFalse(stage.otherEye.gameObject.activeSelf, when + ": the other eye left with the subject");
                Assert.IsTrue(string.IsNullOrEmpty(stage.CineTitle), when + ": no title");
                Assert.AreEqual(1, Shader.GetGlobalFloat("_HE_InteriorDark"), .01f, when + ": dark interior");
                Assert.AreEqual(EyeGeometry.PupilDefault, stage.eye.pupilTarget, .001f, when + ": resting pupil");
                Assert.AreEqual(dollyNear, dolly.Lens.NearClipPlane, .0005f, when + ": dolly lens restored");
                Assert.AreEqual(0, stage.human.blinkingSkin.GetBlendShapeWeight(0), .001f, when + ": eyelids reset");
                foreach (var ps in new[] { stage.gateSparks, stage.gateDust, stage.extractMotes, stage.titleBurst, stage.nikoVortex, stage.depthDust, stage.anatomyTransition, stage.anatomyMotes })
                    if (ps) Assert.IsFalse(ps.gameObject.activeSelf, when + ": " + ps.name + " off");
                Assert.AreEqual(0, Shader.GetGlobalVector("_HE_Dissolve").x, when + ": breakup field off");
                Assert.IsFalse(stage.human.reveal.haze && stage.human.reveal.haze.enabled, when + ": breakup haze off");
                foreach (var part in new[] { stage.human.skin, stage.human.garment, stage.human.reveal.muscles,
                    stage.human.reveal.skeleton, stage.human.reveal.organs, stage.human.reveal.brain })
                    BodyFade(part, false, when);
                Assert.IsFalse(stage.human.reveal.organs.gameObject.activeSelf, when + ": Rack 04 is reset");
            }
            stage.hud.StartModule(0); yield return null; yield return null;
            yield return Until(.25f); // Let Cinemachine render the opening shot before measuring its rail.
            Assert.IsTrue(stage.IntroPlaying); Assert.AreEqual("S02_IntroDolly", stage.cams.Active?.name, "The opening is shot on the code-driven dolly");
            Assert.Less(stage.entrance.OpenAmount, .01f, "The opening starts behind the closed gate");
            Assert.Less(Camera.main.transform.position.x, Stage.GateLook.x, "The camera begins on the corridor side of the gate");
            var railStart = Camera.main.transform.position;
            var rail = stage.human.eyeRight.forward; rail.y = 0; rail.Normalize();
            var companion = stage.Companion;
            Assert.IsTrue(stage.NikoWaiting && !stage.niko.gameObject.activeSelf && !companion.Visible, "The opening starts without Niko: his first line brings him in");
            Assert.AreNotEqual(0, stage.hud.mainCamera.cullingMask & (1 << stage.niko.gameObject.layer), "One Niko, drawn in 3D by the lesson camera");
            Assert.IsNotNull(stage.human.reveal.organs, "Rack 04 must be saved on the anatomy reveal");
            Assert.IsNotNull(stage.human.reveal.brain, "The organ view includes the brain");
            Assert.IsNotEmpty(stage.human.reveal.organs.main, "Rack 04 needs rendered geometry");
            Assert.IsNotNull(stage.anatomyTransition, "Anatomy transitions need their saved particle system");
            BodyFade(stage.human.skin, false, "Full-body opening");
            BodyFade(stage.human.garment, false, "Full-body opening");
            yield return Until(4.4f);
            Assert.Greater(stage.entrance.OpenAmount, .99f, "The lab remains open during the establishing hold");
            if (stage.displayEye) Assert.IsTrue(stage.displayEye.gameObject.activeSelf, "An optional exhibit eye is visible during the lab reveal");
            Capture("intro-gate");
            yield return Until(10.4f);
            Assert.AreEqual("THE HUMAN EYE", stage.CineTitle, "The title belongs to the character reveal");
            Assert.Less(Vector3.Cross(Camera.main.transform.position - railStart, rail).magnitude, .01f,
                "The camera travels straight toward the face without a lateral arc");
            Capture("intro-heading");
            yield return Until(15.1f);
            Portrait("Face hold");
            var faceMaterial = stage.human.skin.main[0].sharedMaterials[0];
            Assert.AreEqual(1, faceMaterial.GetFloat("_ZWrite"), "The solid face must hide its mouth interior while the shoulders fade");
            Assert.IsNotNull(stage.human.blinkingSkin, "The real character has its supplied eyelid blend shape");
            Assert.AreEqual(1, stage.human.blinkingSkin.sharedMesh.blendShapeCount);
            float blinkPeak = 0;
            while (stage.IntroTime < 16.4f)
            {
                blinkPeak = Mathf.Max(blinkPeak, stage.human.blinkingSkin.GetBlendShapeWeight(0));
                Assert.AreEqual(AnatomyRevealController.RevealState.Skin, stage.human.reveal.State);
                Assert.IsFalse(stage.anatomyMotes.gameObject.activeSelf, "Face hold precedes the transition effect");
                Assert.AreEqual(0, Shader.GetGlobalVector("_HE_Dissolve").x, "No breakup during the face hold");
                yield return null;
            }
            Assert.Greater(blinkPeak, 90, "The face hold includes a full blink");
            Assert.AreEqual(0, stage.human.blinkingSkin.GetBlendShapeWeight(0), .01f, "Eyes reopen before the transition");
            Portrait("End of face hold");
            var eyeView = Camera.main.WorldToViewportPoint(stage.human.eyeRight.position);
            Assert.Greater(eyeView.z, 0, "The selected eye is in front of the dolly in the face push");
            Assert.IsTrue(eyeView.x > .2f && eyeView.x < .8f && eyeView.y > .2f && eyeView.y < .95f, "The head stays centered during the face push, got " + eyeView);
            Capture("intro-face");
            yield return Until(17.15f);
            Assert.Greater(stage.anatomyTransition.particleCount, 0, "Skin-to-muscle transition emits particles");
            var breakup = Shader.GetGlobalVector("_HE_Dissolve");
            Assert.Greater(breakup.x, 0, "The electric breakup field drives the skin-to-muscle transition");
            Assert.AreEqual(stage.human.reveal.breakupSeed, breakup.z, "The breakup keeps its fixed seed");
            Assert.IsTrue(stage.anatomyMotes && stage.anatomyMotes.gameObject.activeSelf, "Breakup motes emit from the departing skin");
            Assert.AreEqual(ParticleSystemShapeType.Mesh, stage.anatomyMotes.shape.shapeType, "Motes are born on the layer's surface, not in a box");
            Assert.AreEqual(stage.human.skin.main[0].transform, stage.anatomyMotes.transform.parent, "The emitter rides the skin renderer");
            Assert.AreEqual("HumanEye/DissolveMote", stage.anatomyMotes.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name, "Motes keep only births on breaking patches");
            yield return Until(17.9f);
            Assert.Greater(stage.anatomyMotes.particleCount, 0, "Motes are alive mid-breakup");
            Assert.IsTrue(stage.human.reveal.haze && stage.human.reveal.haze.enabled, "The blue haze glows behind the bust mid-breakup");
            Capture("intro-breakup");
            yield return Until(19.2f);
            Assert.AreEqual(0, Shader.GetGlobalVector("_HE_Dissolve").x, "The breakup field clears for the muscle hold");
            Assert.AreEqual(AnatomyRevealController.RevealState.Muscle, stage.human.reveal.State);
            Assert.IsTrue(stage.human.reveal.muscles.Visible, "Rack 02 is the first anatomy layer");
            Assert.IsFalse(stage.human.reveal.skeleton.gameObject.activeSelf, "The opening omits the skeleton layer");
            Portrait("Muscle hold");
            BodyFade(stage.human.reveal.muscles, true, "Muscle hold");
            Capture("intro-muscle");
            yield return Until(22.15f);
            Assert.AreEqual(AnatomyRevealController.RevealState.TransitionToOrgans, stage.human.reveal.State);
            Assert.IsFalse(stage.human.reveal.skeleton.gameObject.activeSelf, "The new sequence goes directly from muscle to organs");
            Assert.Greater(stage.anatomyTransition.particleCount, 0, "Muscle-to-organ transition emits particles");
            Assert.IsTrue(stage.anatomyMotes.noise.enabled && stage.anatomyMotes.trails.enabled, "Magic wisps are saved on the transition");
            Assert.Greater(Shader.GetGlobalVector("_HE_Dissolve").x, 0, "The same breakup field drives the muscle-to-organ transition");
            Assert.AreEqual(stage.human.reveal.muscles.main[0].transform, stage.anatomyMotes.transform.parent, "The emitter rides the departing muscle layer");
            yield return Until(24.7f);
            Assert.AreEqual(AnatomyRevealController.RevealState.Organs, stage.human.reveal.State);
            Assert.IsTrue(stage.human.reveal.organs.Visible, "Rack 04 follows muscle in the organ view");
            Assert.IsFalse(stage.human.reveal.muscles.gameObject.activeSelf, "Muscles clear before the organ hold");
            Assert.IsTrue(stage.human.reveal.brain.Visible, "The organ view includes the brain above the eyes");
            Assert.AreEqual(0, faceMaterial.GetFloat("_ZWrite"), "The faint organ-view outline must let the brain show through");
            Assert.Greater(stage.human.skin.main[0].sharedMaterial.GetVector("_Wipe").y, .05f, "A faint body outline gives the organs anatomical context");
            Assert.IsTrue(stage.eyeRig.gameObject.activeSelf && stage.otherEye.gameObject.activeSelf, "The organ view includes both eyes");
            Portrait("Organ hold");
            BodyFade(stage.human.reveal.organs, true, "Organ hold");
            Capture("intro-organs");
            yield return Until(25.7f);
            Assert.AreEqual(AnatomyRevealController.RevealState.TransitionToEye, stage.human.reveal.State);
            Capture("intro-xray");
            yield return Until(26.65f);
            Assert.Greater(stage.anatomyTransition.particleCount, 0, "Organ-to-eye extraction emits particles");
            yield return Until(29.6f);
            Assert.Greater(Vector3.Distance(stage.eyeRig.position, stage.human.eyeRight.position), .3f, "The selected eye has left its orbit");   // the ANATOMY.blend figure's socket sits 0.45 m from the display
            Assert.Greater(stage.eyeRig.localScale.x, .3f, "and floats at presentation size");
            Assert.Less(Vector3.Distance(stage.eyeRig.position, stage.heroPoint.position), .05f, "on the central display");
            Assert.IsFalse(stage.displayEye && stage.displayEye.gameObject.activeSelf, "An optional exhibit eye has handed over");
            Assert.IsTrue(string.IsNullOrEmpty(stage.CineTitle));
            Assert.IsTrue(stage.NikoWaiting ? !stage.niko.gameObject.activeSelf && !companion.Visible : !stage.IntroPlaying && stage.narrator.Speaking, "Niko is not in the opening: he arrives with his first line");   // Until also returns once the opening has ended
            Capture("intro-extracted");
            stage.SkipIntro(); yield return null; yield return null;
            Finished("Skip after eye extraction");
            yield return null; Capture("intro-finished");
            stage.hud.ReturnToMenu(); yield return null;
            Assert.AreEqual(0, stage.entrance.OpenAmount);
            stage.hud.StartModule(0); yield return null; yield return null;
            yield return Until(2f);
            stage.SkipIntro(); yield return null; yield return null;
            Finished("Skip in the corridor");
            stage.hud.ReturnToMenu(); yield return null;
            Assert.AreEqual(0, stage.entrance.OpenAmount);
            // One scan only: after the opening the lesson goes straight on to Explore Eye (no Scan Eye action, no second scan).
            Assert.IsFalse(stage.Ability("SCAN", out _), "There is no Scan Eye action after the opening");
            SaveSystem.Data.settings.reducedMotion = true;
            stage.hud.StartModule(0); Clock.FastForward = 20;
            float limit = Time.realtimeSinceStartup + 40;
            while (runner.ExploreSet == null && Time.realtimeSinceStartup < limit) { stage.narrator.Continue(); yield return null; }
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
            Assert.AreEqual(0, stage.entrance.OpenAmount);
            if (stage.displayEye) Assert.IsTrue(stage.displayEye.gameObject.activeSelf, "Back at the menu an optional exhibit eye is restored");
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
