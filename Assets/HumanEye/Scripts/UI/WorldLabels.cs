using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace HumanEye
{
    // Saved label/leader slots under Canvas/root/labels-layer. No runtime UI creation.
    public class WorldLabels
    {
        class Item { public Button button; public RectTransform rect, line; public Transform anchor, target; public Vector2 at, to; public bool visible; }
        readonly RectTransform layer;
        readonly Func<Camera> camera;
        readonly Action<string> select;
        readonly List<Item> items = new();
        public int Count => items.Count;
        public WorldLabels(RectTransform layer, Func<Camera> camera, Action<string> select)
        { this.layer = layer; this.camera = camera; this.select = select; Clear(); }
        public void Clear()
        {
            foreach (Transform child in layer) child.gameObject.SetActive(false);
            items.Clear();
        }
        public void Add(string text, Transform anchor, Transform target, string partId)
        {
            if (!anchor) return;
            if (items.Count >= layer.childCount) throw new InvalidOperationException("Add more saved world-label slots to the Canvas.");
            var rect = (RectTransform)layer.GetChild(items.Count);
            var b = rect.GetComponent<Button>(); b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { if (partId != null) select(partId); });
            b.GetComponentInChildren<Text>(true).text = text;
            b.targetGraphic.raycastTarget = partId != null;
            b.interactable = partId != null;
            items.Add(new Item { button = b, rect = rect, line = (RectTransform)rect.Find("Leader"), anchor = anchor, target = target ? target : anchor });
        }
        public void Update()
        {
            var cam = camera(); if (!cam || items.Count == 0) return;
            var canvas = layer.GetComponentInParent<Canvas>();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (var i in items)
            {
                var s = i.anchor ? cam.WorldToScreenPoint(i.anchor.position) : Vector3.back;
                var t = i.target ? cam.WorldToScreenPoint(i.target.position) : Vector3.back;
                i.visible = s.z > 0 && t.z > 0 && s.x > 0 && s.x < Screen.width && s.y > 0 && s.y < Screen.height;
                i.rect.gameObject.SetActive(i.visible); if (!i.visible) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, s, uiCamera, out i.at);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, t, uiCamera, out i.to);
                i.at.y = Mathf.Clamp(i.at.y, layer.rect.yMin + 225, layer.rect.yMax - 280);
            }
            for (int a = 0; a < items.Count; a++) for (int b = a + 1; b < items.Count; b++)
            {
                var x = items[a]; var y = items[b]; if (!x.visible || !y.visible) continue;
                float dy = x.at.y - y.at.y;
                if (Mathf.Abs(x.at.x - y.at.x) > 185 || Mathf.Abs(dy) > 52) continue;
                float push = (52 - Mathf.Abs(dy)) * .5f * (dy >= 0 ? 1 : -1);
                x.at.y += push; y.at.y -= push;
            }
            foreach (var i in items)
            {
                if (!i.visible) continue;
                var size = i.rect.rect.size;
                i.at.x = Mathf.Clamp(i.at.x, layer.rect.xMin + size.x / 2 + 12, layer.rect.xMax - size.x / 2 - 12);
                i.at.y = Mathf.Clamp(i.at.y, layer.rect.yMin + 180, layer.rect.yMax - 220);
                i.rect.anchoredPosition = i.at;
                var delta = i.to - i.at;
                i.line.sizeDelta = new Vector2(delta.magnitude, 2);
                i.line.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }
    }
}
