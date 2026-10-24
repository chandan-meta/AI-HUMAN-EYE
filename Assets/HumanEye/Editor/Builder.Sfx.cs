using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Cinematic pass step: registration of the new project-generated sound keys. Called by Builder.ApplyCinematic (Builder.Cinematic.cs).
    public static partial class Builder
    {
        /// Cinematic sound set (Tools/HumanEye/generate_cinematic_sfx.py): ten keys for the opening, the reveals and the
        /// optic-nerve signal, plus cleaner replacements for the interface keys select / tap / confirm / deny (ui_*2).
        /// Same pattern as WowPatch.Sfx: re-register by key (idempotent), warn and skip when a clip is missing.
        static void CinematicSfx()
        {
            if (!stage || !stage.sfx) { Debug.LogWarning("CINEMATIC sfx: no Stage / AudioDirector in the scene"); return; }
            var map = new (string key, string file)[]
            {
                ("irisglow", "fx_irisglow"), ("approach", "fx_approach"), ("pupilenter", "fx_pupilenter"), ("interior", "fx_interior"),
                ("lightsweep", "fx_lightsweep"), ("nikoappear", "fx_nikoappear"), ("signaltravel", "fx_signaltravel"), ("signalarrive", "fx_signalarrive"),
                ("layerreveal", "fx_layerreveal"), ("softwhoosh", "fx_softwhoosh"),
                ("select", "ui_select2"), ("tap", "ui_tap2"), ("confirm", "ui_confirm2"), ("deny", "ui_deny2"),
            };
            var effects = stage.sfx.effects.ToList();
            foreach (var (key, file) in map)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/" + file + ".wav");
                if (!clip) { Debug.LogWarning("CINEMATIC sfx missing: " + file + " (run python Tools/HumanEye/generate_cinematic_sfx.py)"); continue; }
                effects.RemoveAll(e => e.key == key);
                effects.Add(new AudioDirector.Sfx { key = key, clip = clip });
            }
            stage.sfx.effects = effects.ToArray();
            EditorUtility.SetDirty(stage.sfx);
        }
    }
}
