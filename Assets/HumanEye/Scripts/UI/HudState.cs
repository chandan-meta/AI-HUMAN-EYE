using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace HumanEye
{
    // Each saved control owns its colours or exported Figma sprites; no layout is overwritten at runtime.
    public sealed class HudState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        /// The interface sounds (Hud subscribes): the pointer enters an available control (never touch, which has no hover),
        /// or an available control is clicked.
        public static event System.Action Hovered, Clicked;
        public Graphic target;
        public GlassPanel surface;
        public bool persistentGlow;
        public Sprite normalSprite, hoverSprite, pressedSprite, focusSprite, selectedSprite, disabledSprite;
        bool hovered, focused, pressed;
        public Color selected = new(.035f, .39f, .47f, 1), complete = new(.11f, .28f, .24f, 1), unavailable = new(.07f, .1f, .14f, 1);
        Color normal; bool captured;
        readonly HashSet<string> states = new();
        public void Set(string state, bool enabled)
        {
            if (!target) return;
            if (!captured) { normal = target.color; captured = true; }
            if (enabled) states.Add(state); else states.Remove(state);
            if (!RefreshSprite())
                target.color = states.Contains("selected") || states.Contains("current") || states.Contains("pulse") ? selected :
                    states.Contains("unavailable") ? unavailable : states.Contains("complete") || states.Contains("done") ? complete : normal;
            RefreshGlow();
        }
        bool RefreshSprite()
        {
            if (!normalSprite || !target || !(target is Image image)) return false;
            var sprite = !Available ? disabledSprite : pressed ? pressedSprite : hovered ? hoverSprite : focused ? focusSprite :
                states.Contains("selected") || states.Contains("current") || states.Contains("pulse") || states.Contains("complete") || states.Contains("done") ? selectedSprite : normalSprite;
            sprite = sprite ? sprite : normalSprite;
            if (image.sprite != sprite) image.sprite = sprite;
            if (image.color != Color.white) image.color = Color.white;
            return true;
        }
        void RefreshGlow()
        {
            RefreshSprite();
            if (surface) surface.SetGlow(states.Contains("unavailable") ? .08f : persistentGlow || states.Contains("selected") || states.Contains("current") || states.Contains("pulse") ? 1.4f : hovered || focused ? .8f : .12f);
        }
        /// The one required action breathes, so the eye finds it (22 October 2026 brief: learners could not tell what to press).
        void Update()
        {
            RefreshSprite();
            if (surface && states.Contains("pulse") && !states.Contains("unavailable")) surface.SetGlow(1.15f + .55f * Mathf.Sin(Time.unscaledTime * 4.5f));
        }
        void OnDisable() { hovered = focused = pressed = false; RefreshGlow(); }
        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true; RefreshGlow();
            bool touch = e is UnityEngine.InputSystem.UI.ExtendedPointerEventData x && x.pointerType == UnityEngine.InputSystem.UI.UIPointerType.Touch;
            if (!touch && Available) Hovered?.Invoke();
        }
        public void OnPointerClick(PointerEventData e) { if (Available) Clicked?.Invoke(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = Available; RefreshGlow(); } }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = false; RefreshGlow(); } }
        bool Available => !states.Contains("unavailable") && (!TryGetComponent<Selectable>(out var s) || s.IsInteractable());
        public void OnPointerExit(PointerEventData e) { hovered = false; RefreshGlow(); }
        public void OnSelect(BaseEventData e) { focused = true; RefreshGlow(); }
        public void OnDeselect(BaseEventData e) { focused = false; RefreshGlow(); }
    }
}
