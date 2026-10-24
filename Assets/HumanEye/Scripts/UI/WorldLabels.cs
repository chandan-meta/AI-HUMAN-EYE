using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace HumanEye
{
    // Saved label/leader slots under Canvas/root/labels-layer. No runtime UI creation. Labels fade in when introduced,
    // fade out when retired or when their structure leaves the frame (never pop), glide (never jump) when the view
    // changes, and the layer's own rect keeps them clear of the right-hand panels.
    public class WorldLabels
    {
        class Item { public Button button; public RectTransform rect, line; public CanvasGroup group; public Transform anchor, target; public Vector2 at, to, shown; public bool visible, placed; public float alpha; }
        const float FadeIn = .35f, FadeOut = .25f;   // seconds
        readonly RectTransform layer;
        readonly Func<Camera> camera;
        readonly Action<string> select;
        readonly List<Item> items = new(), retiring = new();   // live labels, and labels fading out that still hold their slot
        public int Count => items.Count;
        readonly Vector3[] corners = new Vector3[4];

        /// Screen rectangles (pixels) of the labels on screen.
        public void ScreenRects(List<Rect> into)
        {
            foreach (var i in items)
            {
                if (!i.visible || !i.rect.gameObject.activeInHierarchy) continue;
                i.rect.GetWorldCorners(corners); into.Add(Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y));
            }
        }
        public WorldLabels(RectTransform layer, Func<Camera> camera, Action<string> select)
        { this.layer = layer; this.camera = camera; this.select = select; Clear(); }

        /// Retires every live label: each fades where it stands and gives its slot back once it is gone.
        public void Clear()
        {
            foreach (var i in items) Retire(i);
            items.Clear();
            foreach (Transform child in layer) if (!InUse(child)) child.gameObject.SetActive(false);
        }

        bool InUse(Transform slot)
        {
            foreach (var i in items) if (i.rect == slot) return true;
            foreach (var i in retiring) if (i.rect == slot) return true;
            return false;
        }

        void Retire(Item i)
        {
            if (!i.rect.gameObject.activeSelf || i.alpha <= 0) { Release(i); return; }   // never drawn: the slot is free at once
            i.button.onClick.RemoveAllListeners(); i.button.interactable = false;
            if (i.group) i.group.interactable = i.group.blocksRaycasts = false;   // a label on its way out never blocks what is underneath
            else if (i.button.targetGraphic) i.button.targetGraphic.raycastTarget = false;
            i.visible = false; retiring.Add(i);
        }

        static void Release(Item i)
        {
            i.alpha = 0; if (i.group) i.group.alpha = 0;
            i.rect.gameObject.SetActive(false);
        }

        /// The first saved slot nobody holds; when every slot is still fading out, the faintest one is released early.
        RectTransform FreeSlot()
        {
            for (int k = 0; k < layer.childCount; k++) { var slot = layer.GetChild(k); if (!InUse(slot)) return (RectTransform)slot; }
            if (retiring.Count == 0) throw new InvalidOperationException("Add more saved world-label slots to the Canvas.");
            int faintest = 0;
            for (int k = 1; k < retiring.Count; k++) if (retiring[k].alpha < retiring[faintest].alpha) faintest = k;
            var reuse = retiring[faintest]; retiring.RemoveAt(faintest); Release(reuse);
            return reuse.rect;
        }

        public void Add(string text, Transform anchor, Transform target, string partId)
        {
            if (!anchor) return;
            var rect = FreeSlot();
            var b = rect.GetComponent<Button>(); b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { if (partId != null) select(partId); });
            var label = b.GetComponentInChildren<Text>(true); label.text = text;
            rect.sizeDelta = new Vector2(Mathf.Clamp(label.preferredWidth + 36, 104, 260), Mathf.Max(48, rect.sizeDelta.y));   // the pill hugs its name (fewer collisions on 4:3)
            b.targetGraphic.raycastTarget = partId != null;
            b.interactable = partId != null;
            var group = rect.GetComponent<CanvasGroup>();
            if (group) { group.alpha = 0; group.interactable = group.blocksRaycasts = true; }
            items.Add(new Item { button = b, rect = rect, line = (RectTransform)rect.Find("Leader"), group = group, anchor = anchor, target = target ? target : anchor });
        }

        /// Moves a label's alpha toward its target; false once it has completely faded out.
        static bool Fade(Item i, float target, float dt)
        {
            i.alpha = Mathf.MoveTowards(i.alpha, target, dt / (target > 0 ? FadeIn : FadeOut));
            if (i.group) i.group.alpha = i.alpha;
            return i.alpha > 0;
        }

        public void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // Retiring labels fade where they were last drawn, then release their slot.
            for (int k = retiring.Count - 1; k >= 0; k--) if (!Fade(retiring[k], 0, dt)) { Release(retiring[k]); retiring.RemoveAt(k); }
            var cam = camera(); if (!cam || items.Count == 0) return;
            var canvas = layer.GetComponentInParent<Canvas>();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (var i in items)
            {
                var s = i.anchor ? cam.WorldToScreenPoint(i.anchor.position) : Vector3.back;
                var t = i.target ? cam.WorldToScreenPoint(i.target.position) : Vector3.back;
                i.visible = s.z > 0 && t.z > 0 && s.x > 0 && s.x < Screen.width && s.y > 0 && s.y < Screen.height;
                if (!i.visible)
                {
                    // Off screen: fade out in place (the same exit as a retired label), then rest until the structure returns.
                    if (!Fade(i, 0, dt)) { i.rect.gameObject.SetActive(false); i.placed = false; }
                    continue;
                }
                if (!i.rect.gameObject.activeSelf) { i.rect.gameObject.SetActive(true); i.placed = false; }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, s, uiCamera, out i.at);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, t, uiCamera, out i.to);
                i.at.y = Mathf.Clamp(i.at.y, layer.rect.yMin + 140, layer.rect.yMax - 160);
            }
            // Separate overlapping labels: relaxation passes, each followed by the bounds clamp, so a label pushed against an
            // edge is pushed back the other way instead of ending on top of its neighbour (crowded 4:3 cutaways).
            for (int pass = 0; pass < 10; pass++)
            {
                for (int a = 0; a < items.Count; a++) for (int b = a + 1; b < items.Count; b++)
                {
                    var x = items[a]; var y = items[b]; if (!x.visible || !y.visible) continue;
                    float dx = x.at.x - y.at.x, dy = x.at.y - y.at.y;
                    float w = (x.rect.rect.width + y.rect.rect.width) * .5f + 8, h = (x.rect.rect.height + y.rect.rect.height) * .5f + 6;
                    if (Mathf.Abs(dx) > w || Mathf.Abs(dy) > h) continue;
                    float push = (h - Mathf.Abs(dy)) * .5f * (dy >= 0 ? 1 : -1);
                    x.at.y += push; y.at.y -= push;
                    float side = Mathf.Min(24, (w - Mathf.Abs(dx)) * .25f) * (dx >= 0 ? 1 : -1);
                    x.at.x += side; y.at.x -= side;
                }
                foreach (var i in items)
                {
                    if (!i.visible) continue;
                    var half = i.rect.rect.size * .5f;
                    i.at.x = Mathf.Clamp(i.at.x, layer.rect.xMin + half.x + 12, layer.rect.xMax - half.x - 12);
                    i.at.y = Mathf.Clamp(i.at.y, layer.rect.yMin + 120, layer.rect.yMax - 140);
                }
            }
            foreach (var i in items)
            {
                if (!i.visible) continue;
                var size = i.rect.rect.size;
                i.at.x = Mathf.Clamp(i.at.x, layer.rect.xMin + size.x / 2 + 12, layer.rect.xMax - size.x / 2 - 12);
                i.at.y = Mathf.Clamp(i.at.y, layer.rect.yMin + 120, layer.rect.yMax - 140);
                // Purposeful motion: snap on first placement, then glide to the new spot when the camera or model moves.
                i.shown = i.placed ? Vector2.Lerp(i.shown, i.at, 1 - Mathf.Exp(-dt * 10)) : i.at; i.placed = true;
                Fade(i, 1, dt);
                i.rect.anchoredPosition = i.shown;
                var delta = i.to - i.shown; var dir = delta.sqrMagnitude > 1e-4f ? delta.normalized : Vector2.right;
                // the hairline starts at the pill's edge (never across the text) and its end dot marks the structure
                float edge = Mathf.Min(Mathf.Abs(dir.x) > 1e-3f ? size.x * .5f / Mathf.Abs(dir.x) : 1e6f, Mathf.Abs(dir.y) > 1e-3f ? size.y * .5f / Mathf.Abs(dir.y) : 1e6f);
                i.line.anchoredPosition = dir * edge;
                i.line.sizeDelta = new Vector2(Mathf.Max(0, delta.magnitude - edge), 1.5f);
                i.line.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }
    }
}
