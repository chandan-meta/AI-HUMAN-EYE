using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HumanEye
{
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        bool hovered, pressed;
        void Update()
        {
            bool reduced = SaveSystem.Data.settings.reducedMotion || SaveSystem.Data.settings.skipCameraMotion;
            float target = reduced ? 1 : pressed ? .99f : hovered ? 1.015f : 1;
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * .16f);
        }
        public void OnPointerEnter(PointerEventData e) { hovered = GetComponent<Button>().IsInteractable(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        void OnDisable() { hovered = pressed = false; transform.localScale = Vector3.one; }
    }
}
