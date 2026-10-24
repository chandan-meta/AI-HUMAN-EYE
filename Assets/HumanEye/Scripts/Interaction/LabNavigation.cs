using UnityEngine;
using UnityEngine.InputSystem;

namespace HumanEye
{
    /// Free walking owns the output camera only while the lesson is waiting for learner input.
    public class LabNavigation : MonoBehaviour
    {
        public Stage stage;
        public CharacterController body;
        public LabJoystick joystick;
        public GameObject joystickPanel;
        public float speed = 2, eyeHeight = 1.65f, lookSpeed = .12f;
        public bool Active { get; private set; }
        public bool Available => stage.runner.Started && !stage.CinematicPlaying && !stage.DemoBusy &&
            stage.runner.Focused == null && stage.activities.Card == null &&
            stage.runner.State == LessonState.Exploring;
        Camera Camera => stage.cams.brain.GetComponent<Camera>();
        Vector3 savedPosition;
        Quaternion savedRotation;
        float yaw, pitch;
        int lookTouch = -1;
        bool mouseLook;
        Vector2 last;

        public void Toggle() { if (Active) Leave(); else Enter(); }
        public void Enter()
        {
            if (!Available || Clock.Paused) return;
            var cam = Camera.transform;
            savedPosition = cam.position; savedRotation = cam.rotation;
            stage.cams.brain.enabled = false;
            body.enabled = false;
            // Clear floor in front of the stand (the new lab's floor is at y 0.11), away from the side exhibits and hero shots.
            transform.position = new Vector3(-4, .16f, -6);
            body.enabled = true; yaw = 45; pitch = 0;
            Active = true; joystickPanel.SetActive(true);
            stage.hud.Refresh();
        }
        public void Leave()
        {
            if (!Active) return;
            Active = false; body.enabled = false;
            joystick.Clear(); lookTouch = -1; mouseLook = false;
            joystickPanel.SetActive(false);
            Camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            stage.cams.brain.enabled = true;
        }
        void OnDisable() => Leave();
        void LateUpdate()
        {
            if (!Active) return;
            if (!Available) { Leave(); return; }
            if (Clock.Paused) { joystick.Clear(); lookTouch = -1; mouseLook = false; return; }
            var movement = joystick.Value;
            var keys = Keyboard.current;
            if (keys != null)
            {
                movement += new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                    (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
                if (keys.escapeKey.wasPressedThisFrame) { Leave(); return; }
            }
            var delta = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame) mouseLook = !stage.hud.IsOverUI(mouse.position.ReadValue());
                if (mouseLook && mouse.rightButton.isPressed) delta = mouse.delta.ReadValue();
                if (mouse.rightButton.wasReleasedThisFrame) mouseLook = false;
            }
            var touch = Touchscreen.current;
            if (touch != null)
                foreach (var t in touch.touches)
                {
                    int id = t.touchId.ReadValue(); var p = t.position.ReadValue();
                    if (t.press.wasPressedThisFrame && lookTouch < 0 && !stage.hud.IsOverUI(p)) { lookTouch = id; last = p; }
                    if (id != lookTouch) continue;
                    if (t.press.isPressed) { delta += p - last; last = p; }
                    else lookTouch = -1;
                }
            yaw += delta.x * lookSpeed; pitch = Mathf.Clamp(pitch - delta.y * lookSpeed, -65, 65);
            var direction = Quaternion.Euler(0, yaw, 0) * new Vector3(movement.x, 0, movement.y);
            body.Move((Vector3.ClampMagnitude(direction, 1) * speed + Vector3.down * 2) * Mathf.Min(Time.unscaledDeltaTime, .05f));
            Camera.transform.SetPositionAndRotation(transform.position + Vector3.up * eyeHeight, Quaternion.Euler(pitch, yaw, 0));
        }
    }
}
