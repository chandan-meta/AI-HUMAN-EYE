using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye
{
    /// Per-shot look and focus. Anatomy close-ups ease into the eye-macro grade; their depth of field is a gentle background
    /// blur that starts behind the whole eye, so whatever is explained stays sharp. URP's depth of field reads the opaque depth
    /// buffer: wherever a see-through surface shows the room behind it (a faded or sliding wall, the cut eye's clear media, the
    /// light rays, which write no depth), that pixel would take the room's blur. So the blur is used only on the closed eye
    /// with every wall whole and no rays drawn, and it drops on the very frame that stops being true. Wide shots, the
    /// eye-to-brain compositions, the micro world and the intro stay sharp. The floating Niko sits nearer than the subject,
    /// in front of the blur's start, so he is never soft (NikoFloatingCompanion).
    /// The blur fades by its circle of confusion (URP clamps the Gaussian radius to 0.5 and over) and lives in its own volume
    /// at full weight: inside the macro grade, a partly faded weight blended it toward the defaults' 1.0 radius and 10 m start.
    [DefaultExecutionOrder(900)]
    public class ShotLook : MonoBehaviour
    {
        public CameraDirector cams;
        public Stage stage;
        public Volume macro;
        [Tooltip("Shot-name fragments that frame the eye up close.")]
        public string[] closeUps = { "_Part_", "_Close", "Lens", "PupilFront", "S05_Cornea", "AqueousPupil", "EyeInSkull", "EyeHero", "InsideHero", "Retina_Close", "Choroid", "Vitreous_Back", "S07_Retina", "S11_Eye" };
        [Tooltip("Shots that keep everything sharp (eye-to-brain, micro world, overviews).")]
        public string[] sharp = { "S04_", "S08_", "S09_", "S10_", "Wide", "Far", "Near" };
        [Tooltip("Gaussian blur radius (URP accepts 0.5-1.5); small, so the room softens rather than smears.")]
        [Range(.5f, 1.5f)] public float blur = .6f;
        static readonly string[] ClearMedia = { "cornea", "aqueous", "lens", "vitreous" };   // see-through: backed by a wall only on the closed eye
        DepthOfField dof;
        float fade;

        /// Where the background blur starts (m from the lens); infinity while nothing is blurred.
        public float BlurStart { get; private set; } = float.PositiveInfinity;

        void Start()
        {
            if (!macro || !macro.profile.TryGet(out dof)) return;
            macro.profile.components.Remove(dof);   // the runtime copy only; the asset keeps it
            var focus = new GameObject("Eye Macro Focus").AddComponent<Volume>();
            focus.transform.SetParent(transform, false);
            focus.isGlobal = true; focus.priority = macro.priority; focus.weight = 1;
            focus.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            focus.profile.components.Add(dof);
            dof.mode.value = DepthOfFieldMode.Off;
        }

        void LateUpdate()
        {
            if (!macro || !cams || !cams.brain) return;
            var shot = cams.Active; var cam = cams.brain.OutputCamera;
            string n = shot ? shot.name : "";
            bool close = shot && !stage.CinematicPlaying && Array.Exists(closeUps, k => n.Contains(k)) && !Array.Exists(sharp, k => n.Contains(k));
            float dt = Time.unscaledDeltaTime;
            macro.weight = Mathf.MoveTowards(macro.weight, close ? 1 : 0, dt * 1.4f);
            if (dof == null || !cam) return;
            fade = Backed() ? Mathf.MoveTowards(fade, close ? 1 : 0, dt * 2.5f) : 0;   // no depth behind a pixel: off at once
            var eye = stage.eye.transform;
            float start = Vector3.Distance(cam.transform.position, eye.position) + eye.lossyScale.x * 1.6f;   // the globe (radius 1 unit) and its nerve stub
            bool on = fade > .01f;
            dof.mode.value = on ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off;
            dof.gaussianMaxRadius.value = blur; dof.gaussianStart.value = start;
            dof.gaussianEnd.value = start + 4 / Mathf.Max(fade, .01f);   // the blur's strength grows with the circle of confusion
            BlurStart = on ? start : float.PositiveInfinity;
        }

        /// Every pixel of the eye has an opaque surface behind it: the eye is closed, no rays are drawn, every wall is whole
        /// and no half is sliding.
        bool Backed()
        {
            if (stage.eye.IsCut || stage.rays && stage.rays.gameObject.activeInHierarchy && stage.rays.reach > 0) return false;
            foreach (var p in stage.eye.parts)
                if (p && p.Visible && Array.IndexOf(ClearMedia, p.id) < 0
                    && (Stage.AlphaOf(p) < .995f || p.removable.Length > 0 && p.Cut > .01f && p.Cut < .995f)) return false;
            return true;
        }
    }
}
