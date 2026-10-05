using UnityEngine;
using UnityEngine.EventSystems;

namespace HumanEye
{
    public class LabJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public Vector2 Value { get; private set; }
        int? owner;
        public void OnPointerDown(PointerEventData e) { if (owner.HasValue) return; owner = e.pointerId; OnDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            if (owner != e.pointerId) return;
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var p);
            float radius = rect.rect.width * .32f;
            Value = Vector2.ClampMagnitude(p / radius, 1); knob.anchoredPosition = Value * radius;
        }
        public void OnPointerUp(PointerEventData e) { if (owner == e.pointerId) Clear(); }
        public void Clear() { owner = null; Value = Vector2.zero; if (knob) knob.anchoredPosition = Vector2.zero; }
        void OnDisable() => Clear();
    }
}
