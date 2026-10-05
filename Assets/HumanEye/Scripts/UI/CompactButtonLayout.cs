using UnityEngine;
using UnityEngine.UI;
namespace HumanEye
{
    // Reflows existing children only; no UI is created during Play.
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class CompactButtonLayout : MonoBehaviour
    {
        public RectTransform icon, caption;
        public float compactBelow = 115;
        void OnEnable() => Reflow();
        void OnRectTransformDimensionsChange() => Reflow();
        void Reflow()
        {
            if (!icon || !caption) return;
            bool compact = ((RectTransform)transform).rect.width < compactBelow;
            icon.anchorMin = icon.anchorMax = compact ? new Vector2(.5f,1) : new Vector2(0,.5f);
            icon.pivot = compact ? new Vector2(.5f,1) : new Vector2(0,.5f);
            icon.anchoredPosition = compact ? new Vector2(0,-5) : new Vector2(12,0);
            icon.sizeDelta = Vector2.one * (compact ? 18 : 24);
            caption.offsetMin = new Vector2(compact ? 4 : 43,4);
            caption.offsetMax = new Vector2(compact ? -4 : -10,compact ? -25 : -4);
        }
    }
}
