using UnityEngine;

namespace HumanEye
{
    public sealed class MenuPanelEntrance : MonoBehaviour
    {
        RectTransform rect; CanvasGroup group; Vector2 resting; float elapsed;
        void Awake() { rect = (RectTransform)transform; group = GetComponent<CanvasGroup>(); resting = rect.anchoredPosition; }
        void OnEnable() { elapsed = 0; }
        void Update()
        {
            bool reduced = SaveSystem.Data.settings.reducedMotion || SaveSystem.Data.settings.skipCameraMotion;
            elapsed += Time.unscaledDeltaTime;
            float t = reduced ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .55f));
            bool compact = Screen.width < 1200;
            rect.localScale = Vector3.one * (compact ? .75f : 1);
            rect.anchoredPosition = new Vector2(compact ? 24 : resting.x, resting.y) + Vector2.left * (18 * (1 - t));
            group.alpha = t;
        }
        void OnDisable() { if (rect) { rect.anchoredPosition = resting; rect.localScale = Vector3.one; } }
    }
}
