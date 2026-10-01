using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace VisionLab {
    public class PointerInputRouter : MonoBehaviour {
        public S02_Director director;
        public Transform eye;
        public Unity.Cinemachine.CinemachineCamera heroCamera;
        InputAction point, press, scroll;
        Vector2 origin, previous, velocity;
        float began, pinchDistance, pitch;
        bool held, dragging, blocked, pinching, touchGesture;
        readonly List<RaycastResult> uiHits = new();
        void OnEnable() {
            EnhancedTouchSupport.Enable();
            point = new InputAction("Point", binding: "<Pointer>/position");
            press = new InputAction("Press", binding: "<Pointer>/press");
            scroll = new InputAction("Scroll", binding: "<Mouse>/scroll");
            point.Enable(); press.Enable(); scroll.Enable();
        }
        void OnDisable() { point?.Dispose(); press?.Dispose(); scroll?.Dispose(); EnhancedTouchSupport.Disable(); }
        bool OverUI(Vector2 position) {
            if (!EventSystem.current) return false;
            uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            return uiHits.Count > 0;
        }
        void Update() {
            if (!director || director.Paused || !director.CanExplore) { held = false; velocity = Vector2.zero; return; }
            var touches = Touch.activeTouches;
            if (touches.Count >= 2) {
                var distance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
                if (!pinching) { blocked = OverUI(touches[0].screenPosition) || OverUI(touches[1].screenPosition); pinchDistance = distance; }
                if (!blocked) Zoom((pinchDistance - distance) * .04f);
                pinchDistance = distance; pinching = true; held = false; velocity = Vector2.zero; return;
            }
            if (pinching) { pinching = false; held = false; return; }
            var p = point.ReadValue<Vector2>();
            if (press.WasPressedThisFrame()) { held = true; dragging = false; touchGesture = touches.Count > 0; blocked = OverUI(p); origin = previous = p; began = Time.unscaledTime; velocity = Vector2.zero; }
            if (held && press.IsPressed() && !blocked) {
                var slop = 12 * Mathf.Max(1, Screen.dpi > 0 ? Screen.dpi / 160 : 1);
                if (!dragging && Vector2.Distance(origin, p) > slop) { dragging = true; director.telemetry.Emit("orbit", touchGesture ? "touch" : "mouse"); }
                if (dragging) { var delta = p - previous; Rotate(delta); velocity = delta / Mathf.Max(.001f, Time.unscaledDeltaTime); }
                previous = p;
            }
            if (held && press.WasReleasedThisFrame()) {
                if (!blocked && !dragging && Time.unscaledTime - began < .3f) {
                    var ray = Camera.main.ScreenPointToRay(p);
                    // Cornea encloses iris/pupil: choose the inner front part first, then cornea via its rim/chip.
                    var hits = Physics.RaycastAll(ray, 100, 1 << LayerMask.NameToLayer("EyePart"));
                    System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
                    EyePart selected = null;
                    foreach (var hit in hits) {
                        var part = hit.collider.GetComponentInParent<EyePart>();
                        if (!part) continue;
                        selected ??= part;
                        if (part.Id is "iris" or "pupil") { selected = part; break; }
                    }
                    if (selected) director.Select(selected.Id, touchGesture ? "touch" : "mouse");
                }
                held = false;
            }
            if (!held && !director.ReducedMotion && velocity.sqrMagnitude > 1) { Rotate(velocity * Time.unscaledDeltaTime); velocity *= Mathf.Exp(-Time.unscaledDeltaTime / .2f); }
            if (!OverUI(p)) { var wheel = scroll.ReadValue<Vector2>().y; if (wheel != 0) Zoom(-wheel * .01f); }
            if (!held && !director.ReducedMotion && velocity.sqrMagnitude <= 1) eye.Rotate(Vector3.up, 6 * Time.unscaledDeltaTime, Space.World);
        }
        void Rotate(Vector2 delta) {
            var next = Mathf.Clamp(pitch - delta.y * .15f, -35, 35);
            eye.Rotate(Vector3.right, next - pitch, Space.World); eye.Rotate(Vector3.up, -delta.x * .15f, Space.World); pitch = next;
        }
        void Zoom(float delta) { var lens = heroCamera.Lens; lens.FieldOfView = Mathf.Clamp(lens.FieldOfView + delta, 30, 55); heroCamera.Lens = lens; director.telemetry.Emit("zoom", lens.FieldOfView.ToString()); }
    }
}
