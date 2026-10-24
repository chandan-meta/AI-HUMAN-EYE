using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Cinematic pass (7 October 2026), idempotent and save-guarded. Runs after the production, UI and look passes.
    /// Each step lives in its own partial file: Builder.Lighting.cs (lights + shader-facing settings),
    /// Builder.Sfx.cs (new sound keys), Builder.Intro.cs (the 20 s opening), Builder.Nerve.cs (signal → brain),
    /// Builder.Framing.cs (shot re-aims so every explained structure is framed). Content data is rebuilt at the end.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.ApplyCinematic
    public static partial class Builder
    {
        [MenuItem("Tools/Human Eye/Authoring/Apply cinematic pass (keeps scene edits)", priority = 10)]
        public static void ApplyCinematic()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            hud = stage.hud; cams = stage.cams; narrator = stage.narrator; runner = stage.runner;
            shotRoot = GameObject.Find("Shots (cue sheet cameras)").transform;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");

            CinematicLights();
            CinematicSfx();
            CinematicIntro();
            CinematicNerve();
            CinematicFraming();
            BuildData();

            foreach (var o in new Object[] { stage, stage.cams, stage.hud, stage.sfx, stage.signals }) if (o) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_CINEMATIC_APPLIED");
        }
    }
}
