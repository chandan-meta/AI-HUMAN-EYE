using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace HumanEye
{
    // Each saved control owns its editable state colours; no layout is overwritten at runtime.
    public sealed class HudState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Graphic target;
        public GlassPanel surface;
        public bool persistentGlow;
        bool hovered, focused;
        public Color selected = new(.035f, .39f, .47f, 1), complete = new(.11f, .28f, .24f, 1), unavailable = new(.07f, .1f, .14f, 1);
        Color normal; bool captured;
        readonly HashSet<string> states = new();
        public void Set(string state, bool enabled)
        {
            if (!target) return;
            if (!captured) { normal = target.color; captured = true; }
            if (enabled) states.Add(state); else states.Remove(state);
            target.color = states.Contains("selected") || states.Contains("current") || states.Contains("pulse") ? selected :
                states.Contains("unavailable") ? unavailable : states.Contains("complete") || states.Contains("done") ? complete : normal;
            RefreshGlow();
        }
        void RefreshGlow()
        {
            if (surface) surface.SetGlow(states.Contains("unavailable") ? .08f : persistentGlow || states.Contains("selected") || states.Contains("current") || states.Contains("pulse") ? 1.4f : hovered || focused ? .8f : .12f);
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; RefreshGlow(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; RefreshGlow(); }
        public void OnSelect(BaseEventData e) { focused = true; RefreshGlow(); }
        public void OnDeselect(BaseEventData e) { focused = false; RefreshGlow(); }
    }
}
