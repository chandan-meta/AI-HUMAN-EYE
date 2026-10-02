using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HumanEye
{
    /// Screen-space labels anchored to 3D structures, with leader lines. Labels for selectable parts are
    /// large buttons (enlarged targets for small structures); others are passive and let taps through.
    public class WorldLabels
    {
        class Item { public Button button; public Transform anchor, target; public Vector2 at, to; public bool visible; }

        readonly VisualElement layer;
        readonly Func<Camera> camera;
        readonly Action<string> select;
        readonly List<Item> items = new();
        public int Count => items.Count;

        public WorldLabels(VisualElement layer, Func<Camera> camera, Action<string> select)
        {
            this.layer = layer; this.camera = camera; this.select = select;
            layer.generateVisualContent += Draw;
        }

        public void Clear()
        {
            foreach (var i in items) i.button.RemoveFromHierarchy();
            items.Clear();
            layer.MarkDirtyRepaint();
        }

        public void Add(string text, Transform anchor, Transform target, string partId)
        {
            if (!anchor) return;
            var b = new Button(() => { if (partId != null) select(partId); }) { text = text };
            b.AddToClassList("world-label");
            if (partId == null) { b.AddToClassList("passive"); b.pickingMode = PickingMode.Ignore; }
            b.style.display = DisplayStyle.None;
            layer.Add(b);
            items.Add(new Item { button = b, anchor = anchor, target = target ? target : anchor });
        }

        public void Update()
        {
            var cam = camera();
            if (!cam || layer.panel == null || items.Count == 0) return;
            foreach (var i in items)
            {
                if (!i.anchor) { i.visible = false; i.button.style.display = DisplayStyle.None; continue; }
                var s = cam.WorldToScreenPoint(i.anchor.position);
                var t = cam.WorldToScreenPoint(i.target.position);
                i.visible = s.z > 0 && t.z > 0 && s.x > 0 && s.x < Screen.width && s.y > 0 && s.y < Screen.height;
                i.button.style.display = i.visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!i.visible) continue;
                i.at = RuntimePanelUtils.ScreenToPanel(layer.panel, new Vector2(s.x, Screen.height - s.y));
                i.to = RuntimePanelUtils.ScreenToPanel(layer.panel, new Vector2(t.x, Screen.height - t.y));
                // Keep labels out of the top bar/prompt banner and the subtitle/action bands; the leader line still reaches the structure.
                var H = layer.panel.visualTree.layout.height;
                if (H > 500) i.at.y = Mathf.Clamp(i.at.y, 280, H - 225);   // label centre; 205–255 is the toast band
                var w = i.button.resolvedStyle.width; var h = i.button.resolvedStyle.height;
                if (float.IsNaN(w)) w = 120; if (float.IsNaN(h)) h = 40;
                i.button.style.left = i.at.x - w * .5f;
                i.button.style.top = i.at.y - h * .5f;
            }
            Separate();
            layer.MarkDirtyRepaint();
        }

        /// Push overlapping labels apart vertically so every target stays tappable.
        void Separate()
        {
            for (int a = 0; a < items.Count; a++)
                for (int b = a + 1; b < items.Count; b++)
                {
                    var A = items[a]; var B = items[b];
                    if (!A.visible || !B.visible) continue;
                    float dx = Mathf.Abs(A.at.x - B.at.x), dy = A.at.y - B.at.y;
                    if (dx > 150 || Mathf.Abs(dy) > 46) continue;
                    float push = (46 - Mathf.Abs(dy)) * .5f * (dy >= 0 ? 1 : -1);
                    A.at.y += push; B.at.y -= push;
                    A.button.style.top = A.at.y - 20; B.button.style.top = B.at.y - 20;
                }
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.lineWidth = 2;
            p.strokeColor = new Color(.43f, .88f, .98f, .9f);
            p.fillColor = new Color(.43f, .88f, .98f, 1);
            foreach (var i in items)
            {
                if (!i.visible || Vector2.Distance(i.at, i.to) < 8) continue;
                p.BeginPath(); p.MoveTo(i.at); p.LineTo(i.to); p.Stroke();
                p.BeginPath(); p.Arc(i.to, 5, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
            }
        }
    }
}
