using UnityEngine;

namespace HumanEye
{
    /// Keeps every authored shot composed inside the 3D area left of the right-hand panel column: an oblique projection
    /// (a lens shift) moves the image left by half the column width, so subjects framed at the centre land in the
    /// centre of the visible area. Applied after Cinemachine each frame; screen-space queries (labels, taps) stay consistent.
    [DefaultExecutionOrder(1000), RequireComponent(typeof(Camera))]
    public class ViewportShift : MonoBehaviour
    {
        public const float MaxShift = .2f;   // 0.2 keeps a subject framed at the left third (Niko) inside the 4:3 view
        public float columnPixels = 412;   // panel width + margins (constant pixel size canvas)
        Camera cam;
        void Awake() => cam = GetComponent<Camera>();
        void LateUpdate()
        {
            cam.ResetProjectionMatrix();
            float fraction = Mathf.Clamp01(columnPixels / Mathf.Max(1, Screen.width));
            if (fraction <= 0) return;
            var m = cam.projectionMatrix;
            // View space looks down -z, so a positive m02 moves the image left. NDC spans 2 units: a shift of `fraction`
            // moves the image by half the column width. Capped so a small game view (tests) never over-shifts.
            m.m02 += Mathf.Min(fraction, MaxShift);
            cam.projectionMatrix = m;
        }
        void OnDisable() { if (cam) cam.ResetProjectionMatrix(); }
    }
}
