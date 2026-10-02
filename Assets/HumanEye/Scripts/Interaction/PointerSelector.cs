using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HumanEye
{
    /// Mouse and touch share one path: a short press without movement is a tap (select), a drag orbits,
    /// wheel / two-finger pinch zoom (optional; on-screen buttons do the same). A press that starts over UI
    /// belongs to the UI for its whole gesture, so one tap never triggers two systems.
    public class PointerSelector : MonoBehaviour
    {
        public Camera cam;
        public ChapterRunner runner;
        public Stage stage;
        public Hud hud;
        public LayerMask pickMask;
        public float tapSlop = 14, tapSeconds = .45f;

        InputAction point, press, scroll;
        Vector2 origin, last;
        float pressedAt, pinch;
        bool held, dragging, blocked;
        readonly RaycastHit[] hits = new RaycastHit[24];

        float Slop => tapSlop * Mathf.Max(1, Screen.dpi > 0 ? Screen.dpi / 160f : 1);

        void OnEnable()
        {
            point = new InputAction("Point", binding: "<Pointer>/position");
            press = new InputAction("Press", binding: "<Pointer>/press");
            scroll = new InputAction("Scroll", binding: "<Mouse>/scroll");
            point.Enable(); press.Enable(); scroll.Enable();
        }

        void OnDisable() { point.Dispose(); press.Dispose(); scroll.Dispose(); }

        void Update()
        {
            if (!runner.Started) return;
            var p = point.ReadValue<Vector2>();
            bool canOrbit = LessonRules.Orbit(runner.State) && !Clock.Paused;
            if (Pinching(canOrbit)) return;
            if (press.WasPressedThisFrame())
            {
                held = true; dragging = false; origin = last = p; pressedAt = Time.unscaledTime;
                blocked = hud.IsOverUI(p);
            }
            if (held && press.IsPressed())
            {
                if (!blocked && !dragging && (p - origin).magnitude > Slop) dragging = true;
                if (dragging && canOrbit) stage.Orbit(p - last);
                last = p;
            }
            if (held && press.WasReleasedThisFrame())
            {
                held = false;
                if (!blocked && !dragging && Time.unscaledTime - pressedAt < tapSeconds && !Clock.Paused) Tap(p);
            }
            var wheel = scroll.ReadValue<Vector2>().y;
            if (wheel != 0 && canOrbit && !hud.IsOverUI(p)) stage.Zoom(-Mathf.Sign(wheel) * 2.5f);
        }

        bool Pinching(bool canOrbit)
        {
            var ts = Touchscreen.current;
            if (ts == null || !ts.touches[1].isInProgress || !ts.touches[0].isInProgress) { pinch = 0; return false; }
            float d = Vector2.Distance(ts.touches[0].position.ReadValue(), ts.touches[1].position.ReadValue());
            if (pinch > 0 && canOrbit && !hud.IsOverUI(ts.touches[0].position.ReadValue())) stage.Zoom((pinch - d) * .05f);
            pinch = d; held = false; dragging = true;
            return true;
        }

        void Tap(Vector2 screen)
        {
            var part = Pick(screen);
            if (part) stage.OnPartTapped(part);
        }

        /// Explicit priority rules instead of "first collider wins": transparent shells (cornea, aqueous, vitreous)
        /// yield to an eligible structure just behind them; overlapping enlarged targets resolve by priority.
        public AnatomyPart Pick(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            int count = Physics.RaycastNonAlloc(ray, hits, 500, pickMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, Comparer);
            AnatomyPart best = null; float first = 0;
            for (int i = 0; i < count; i++)
            {
                var part = hits[i].collider.GetComponentInParent<AnatomyPart>();
                if (!part || !part.Selectable) continue;
                if (!best) { best = part; first = hits[i].distance; continue; }
                float scale = Mathf.Max(.01f, best.transform.lossyScale.x), gap = hits[i].distance - first;
                if (best.seeThrough && !part.seeThrough)
                {
                    if (gap > .6f * scale) break;
                    best = part; first = hits[i].distance; continue;
                }
                if (gap > .03f * scale) { if (best.seeThrough) continue; break; }
                if (part.priority > best.priority) best = part;
            }
            return best;
        }

        static readonly System.Collections.Generic.Comparer<RaycastHit> Comparer =
            System.Collections.Generic.Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    }
}
