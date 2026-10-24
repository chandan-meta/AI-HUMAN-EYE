using UnityEngine;

namespace HumanEye
{
    /// Lighting hierarchy for the cinematic pass (7 October 2026). The lights are created and assigned by
    /// Builder.CinematicLights (Editor/Builder.Lighting.cs); runtime code must test each with `if (light)`.
    public partial class Stage
    {
        [Header("Cinematic lights (Builder.CinematicLights)")]
        [Tooltip("Soft key on the scan subject's face (portrait, face shot, intro).")] public Light faceKey;
        [Tooltip("Key spot on the hero eye: form and the main corneal catchlight.")] public Light eyeKey;
        [Tooltip("Cool rim behind the hero eye: separation from the hall.")] public Light eyeRim;
        [Tooltip("Low fill on the hero eye so shadowed anatomy stays readable.")] public Light eyeFill;
        [Tooltip("Rim behind the brain for the optic-nerve sequence (off outside it).")] public Light brainRim;
        [Tooltip("Warm light inside the eye for the interior reveal (off outside it).")] public Light interiorLight;

        // Authored levels, remembered once in Awake so every ramp has an exact value to return to.
        float faceKeyBase, eyeKeyBase, eyeRimBase, eyeFillBase, brainRimBase, interiorBase;
        bool faceKeyOn, eyeKeyOn, eyeRimOn, eyeFillOn, brainRimOn, interiorOn;
        Color ambientColourBase;
        bool emphasisOn; float roomBeforeEmphasis = 1; Routine emphasisTween;

        /// Awake: capture the authored light levels and the Flat ambient colour (CinematicLights / the look pass set them).
        void CaptureLighting()
        {
            if (faceKey) { faceKeyBase = faceKey.intensity; faceKeyOn = faceKey.enabled; }
            if (eyeKey) { eyeKeyBase = eyeKey.intensity; eyeKeyOn = eyeKey.enabled; }
            if (eyeRim) { eyeRimBase = eyeRim.intensity; eyeRimOn = eyeRim.enabled; }
            if (eyeFill) { eyeFillBase = eyeFill.intensity; eyeFillOn = eyeFill.enabled; }
            if (brainRim) { brainRimBase = brainRim.intensity; brainRimOn = brainRim.enabled; }
            if (interiorLight) { interiorBase = interiorLight.intensity; interiorOn = interiorLight.enabled; }
            ambientColourBase = RenderSettings.ambientLight;
        }

        /// Enter: instant reset of everything a beat or the emphasis may have changed, so every checkpoint starts from the
        /// authored hierarchy (brainRim and interiorLight back to their authored off state, ambient colour restored).
        void RestoreLighting()
        {
            if (emphasisTween != null) { emphasisTween.Cancel(); emphasisTween = null; }
            emphasisOn = false; roomBeforeEmphasis = 1;
            if (faceKey) { faceKey.intensity = faceKeyBase; faceKey.enabled = faceKeyOn; }
            if (eyeKey) { eyeKey.intensity = eyeKeyBase; eyeKey.enabled = eyeKeyOn; }
            if (eyeRim) { eyeRim.intensity = eyeRimBase; eyeRim.enabled = eyeRimOn; }
            if (eyeFill) { eyeFill.intensity = eyeFillBase; eyeFill.enabled = eyeFillOn; }
            if (brainRim) { brainRim.intensity = brainRimBase; brainRim.enabled = brainRimOn; }
            if (interiorLight) { interiorLight.intensity = interiorBase; interiorLight.enabled = interiorOn; }
            RenderSettings.ambientLight = ambientColourBase;
        }

        /// Flat-mode ambient (the mode the scene uses) ignores RenderSettings.ambientIntensity; this scales the authored
        /// ambient colour instead. Restored by RestoreLighting at every checkpoint.
        public void AmbientScale(float k) => RenderSettings.ambientLight = ambientColourBase * k;

        /// Isolate: the hero key lifts a quarter and the hall settles to 85 %, so the one structure being explained is also
        /// the brightest thing on screen; RestoreParts brings both back to the level they had before. Eased over .6 s
        /// (instant with reduced motion), pause-aware, never a lighting cue of its own.
        void Emphasis(bool on)
        {
            if (on == emphasisOn) return;
            emphasisOn = on;
            bool fading = emphasisTween != null && emphasisTween.Running;
            if (on && !fading) roomBeforeEmphasis = RoomLevel();   // mid-restore: keep the level we are returning to
            float roomFrom = RoomLevel(), roomTo = on ? roomBeforeEmphasis * .85f : roomBeforeEmphasis;
            float keyFrom = eyeKey ? eyeKey.intensity : 0, keyTo = eyeKeyBase * (on ? 1.25f : 1);
            if (emphasisTween != null) emphasisTween.Cancel();
            emphasisTween = null;
            if (Short || !runner) { Room(roomTo); if (eyeKey) eyeKey.intensity = keyTo; return; }
            emphasisTween = runner.RunSide(Routine.Tween(.6f, t =>
            {
                Room(Mathf.Lerp(roomFrom, roomTo, t));
                if (eyeKey) eyeKey.intensity = Mathf.Lerp(keyFrom, keyTo, t);
            }));
        }

        /// The room's current level relative to its authored base (1 = lit lab), read from the first live room light.
        float RoomLevel()
        {
            if (roomBase == null || roomLights == null) return 1;
            for (int i = 0; i < roomLights.Length && i < roomBase.Length; i++)
                if (roomLights[i] && roomBase[i] > 1e-4f) return roomLights[i].intensity / roomBase[i];
            return 1;
        }
    }
}
