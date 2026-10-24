using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Look pass (7 October 2026), idempotent: the clean lab grade (neutral tonemapping, gentle contrast, a touch cool,
    /// restrained bloom and vignette), a controlled hall that sits under the spot-lit subject (ApplyHallLevels, shared
    /// with the cinematic lights), and the eye-macro look (background-only depth of field, a little more contrast) that
    /// ShotLook eases in on anatomy close-ups. Render pipeline unchanged.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.ApplyLook
    public static partial class Builder
    {
        /// Hall levels once the cinematic lights exist (Builder.Lighting.cs). Either pass may run last; both call this.
        /// • Key Light .85 (was 1.05): the directional key no longer has to model the anatomy — the Eye Key (14) and Face Key
        ///   (10) spots do — so it drops to a hall level and the subject's own light reads above the room's.
        /// • roomBoost 1.5 (was 1.7): the hall's practicals recede a little ("darkness around a lit subject") without going murky.
        /// • Ambient: the scene is in Flat ambient mode (CinematicPatch), where RenderSettings.ambientIntensity does nothing —
        ///   it only multiplies a Skybox source. The authored level is RenderSettings.ambientLight = (.085, .10, .13), up from
        ///   (.055, .07, .095): a cool floor raised about half again so the shadow side of the anatomy stays readable now
        ///   that the spots cast shadows, still low enough that the warm key reads as the top of the hierarchy. The multiplier
        ///   is pinned at 1: Stage captures it as ambientBase, restores it at every checkpoint and scales it in the brightness
        ///   experiment, and in Flat mode those writes are visually inert (Stage.AmbientScale scales the colour instead).
        internal const float LabKeyIntensity = .85f, LabRoomBoost = 1.5f;
        internal static readonly Color LabAmbient = new Color(.085f, .10f, .13f);
        internal static void ApplyHallLevels()
        {
            stage.keyLight.intensity = LabKeyIntensity; stage.roomBoost = LabRoomBoost;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = LabAmbient; RenderSettings.ambientIntensity = 1;
            EditorUtility.SetDirty(stage); EditorUtility.SetDirty(stage.keyLight);
        }

        [MenuItem("Tools/Human Eye/Authoring/Apply look pass (keeps scene edits)", priority = 9)]
        public static void ApplyLook()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);

            // LAB_CLEAN: neutral whites, mild contrast, slightly cool clinical balance, bloom only on true highlights
            var lab = ScriptableObject.CreateInstance<VolumeProfile>();
            lab.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var bloom = lab.Add<Bloom>(); bloom.threshold.Override(1.15f); bloom.intensity.Override(.14f); bloom.scatter.Override(.6f);
            var grade = lab.Add<ColorAdjustments>(); grade.postExposure.Override(.25f); grade.contrast.Override(10); grade.saturation.Override(4);
            lab.Add<WhiteBalance>().temperature.Override(-4);
            // Vignette .22 (was .18): a touch more darkness around the lit subject, still under the eye-macro grade's .26.
            var vignette = lab.Add<Vignette>(); vignette.intensity.Override(.22f); vignette.smoothness.Override(.45f);
            stage.post.sharedProfile = SaveProfile(lab, "PostProfile");

            // EYE_MACRO: background-only Gaussian depth of field (ShotLook keeps the start behind the whole eye)
            var macro = ScriptableObject.CreateInstance<VolumeProfile>();
            var dof = macro.Add<DepthOfField>(); dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(3); dof.gaussianEnd.Override(7);
            dof.gaussianMaxRadius.Override(1.1f); dof.highQualitySampling.Override(false);
            var macroGrade = macro.Add<ColorAdjustments>(); macroGrade.contrast.Override(16); macroGrade.saturation.Override(8);
            macro.Add<Vignette>().intensity.Override(.26f);
            var holder = GameObject.Find("Eye Macro Look");
            if (!holder) holder = new GameObject("Eye Macro Look");
            var volume = holder.GetComponent<Volume>(); if (!volume) volume = holder.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 1; volume.weight = 0; volume.sharedProfile = SaveProfile(macro, "EyeMacroProfile");
            var look = stage.cams.GetComponent<ShotLook>(); if (!look) look = stage.cams.gameObject.AddComponent<ShotLook>();
            look.cams = stage.cams; look.stage = stage; look.macro = volume;

            // RETINA_MICRO: the magnified retina sits on a dark neutral backdrop so the lit cells carry the picture
            var backdrop = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/MicroBackdrop.mat");
            if (backdrop) { backdrop.SetColor("_BaseColor", new Color(.07f, .075f, .09f)); backdrop.SetColor("_EmissionColor", new Color(.012f, .014f, .02f)); EditorUtility.SetDirty(backdrop); }

            // Hall levels: shared with the cinematic lights (absolute values, so re-runs never compound)
            ApplyHallLevels();
            foreach (var o in new Object[] { stage.keyLight, look, volume, stage.post }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_LOOK_APPLIED");
        }
    }
}
