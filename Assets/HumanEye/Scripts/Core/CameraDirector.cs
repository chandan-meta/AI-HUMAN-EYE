using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace HumanEye
{
    /// The one authoritative camera controller (Cinemachine 3 API). Each shot is a named CinemachineCamera in
    /// the scene (inspectable cue sheet); this class only changes priorities and blend definitions.
    public class CameraDirector : MonoBehaviour
    {
        public CinemachineBrain brain;
        public CinemachineCamera[] shots;
        public Func<bool> shortMoves = () => false;
        public Func<float, float, IEnumerator> fade;   // (targetAlpha, seconds), provided by the HUD
        public CinemachineCamera Active { get; private set; }

        Dictionary<string, CinemachineCamera> byName;
        readonly Dictionary<CinemachineCamera, float> baseFov = new();

        void Awake()
        {
            byName = new Dictionary<string, CinemachineCamera>();
            foreach (var s in shots) { byName[s.name] = s; s.Priority = 0; baseFov[s] = s.Lens.FieldOfView; }
        }

        public bool Has(string name) => byName.ContainsKey(name);

        public IEnumerator MoveTo(string name, float seconds)
        {
            if (!byName.TryGetValue(name, out var cam)) { Debug.LogError("Unknown shot " + name); yield break; }
            if (cam == Active) yield break;
            bool quick = Active == null || seconds <= 0 || shortMoves();
            bool faded = quick && Active != null && seconds > 0 && fade != null;
            if (faded) yield return fade(1, .22f);
            brain.DefaultBlend = quick ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0)
                                       : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds);
            if (Active) Active.Priority = 0;
            cam.Priority = 10; Active = cam;
            yield return null; yield return null;
            if (faded) { yield return fade(0, .28f); yield break; }
            for (float guard = seconds + 3; brain.IsBlending && guard > 0; guard -= Clock.Delta) yield return null;
        }

        /// Instant cut, used for checkpoints / resets.
        public void Snap(string name)
        {
            if (!byName.TryGetValue(name, out var cam)) { Debug.LogError("Unknown shot " + name); return; }
            foreach (var s in shots) { s.Priority = 0; ResetZoom(s); }
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            cam.Priority = 10; Active = cam;
        }

        public void SetPaused(bool paused) => CinemachineCore.UniformDeltaTimeOverride = paused ? 0 : -1;

        /// Interaction zoom on the active shot (buttons, wheel or pinch), clamped around the authored framing.
        public void Zoom(float delta)
        {
            if (!Active) return;
            var lens = Active.Lens;
            var b = baseFov[Active];
            lens.FieldOfView = Mathf.Clamp(lens.FieldOfView + delta, b * .55f, b * 1.25f);
            Active.Lens = lens;
        }

        public void ResetZoom(CinemachineCamera cam = null)
        {
            cam ??= Active;
            if (!cam) return;
            var lens = cam.Lens; lens.FieldOfView = baseFov[cam]; cam.Lens = lens;
        }

        void OnDestroy() => CinemachineCore.UniformDeltaTimeOverride = -1;
    }
}
