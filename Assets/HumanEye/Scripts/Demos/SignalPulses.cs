using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Electrical signals: discrete cyan zig-zag pulses that hop along nerve/cell paths. Deliberately unlike the
    /// smooth warm light streaks, and always labelled, so light and signals never rely on colour alone.
    public class SignalPulses : MonoBehaviour
    {
        [Serializable]
        public class Path
        {
            public string id;
            public Transform space;
            public Vector3[] points;
            [NonSerialized] public float[] lengths;
            [NonSerialized] public float total;
        }

        public Path[] paths;
        public LineRenderer[] pool;
        public float speed = .6f, pulseLength = .12f, interval = .55f;
        public float widthScale = 1;

        class Pulse { public Path path; public float s; public LineRenderer line; public float speedScale = 1; public Action done; }
        readonly List<Pulse> live = new();
        readonly HashSet<string> emitting = new();
        readonly Dictionary<string, float> nextEmit = new();
        readonly Vector3[] buffer = new Vector3[6];

        void Awake()
        {
            foreach (var p in paths) Measure(p);
            foreach (var l in pool) l.enabled = false;
        }

        static void Measure(Path p)
        {
            p.lengths = new float[p.points.Length]; p.total = 0;
            for (int i = 1; i < p.points.Length; i++) { p.total += Vector3.Distance(p.points[i], p.points[i - 1]); p.lengths[i] = p.total; }
        }

        public Path Get(string id) => Array.Find(paths, p => p.id == id);

        public void Emit(string id, bool on) { if (on) emitting.Add(id); else emitting.Remove(id); }
        public void StopAll() { emitting.Clear(); foreach (var p in live) p.line.enabled = false; live.Clear(); }
        public int Live => live.Count;

        /// One pulse; returns false if the pool is exhausted (repeated requests never pile up beyond the pool).
        public bool Fire(string id, float speedScale = 1, Action done = null)
        {
            var path = Get(id);
            var line = Array.Find(pool, l => !l.enabled);
            if (path == null || !line) return false;
            line.enabled = true;
            live.Add(new Pulse { path = path, s = 0, line = line, speedScale = speedScale, done = done });
            return true;
        }

        public Vector3 PointAt(Path p, float s)
        {
            s = Mathf.Clamp(s, 0, p.total);
            int i = Array.BinarySearch(p.lengths, s);
            if (i < 0) i = ~i;
            i = Mathf.Clamp(i, 1, p.points.Length - 1);
            float seg = p.lengths[i] - p.lengths[i - 1];
            var local = Vector3.Lerp(p.points[i - 1], p.points[i], seg > 0 ? (s - p.lengths[i - 1]) / seg : 0);
            return p.space ? p.space.TransformPoint(local) : local;
        }

        /// Position of the newest pulse on a path (camera follow during TRACE).
        public bool Lead(string id, out Vector3 position)
        {
            position = default;
            for (int i = live.Count - 1; i >= 0; i--) if (live[i].path.id == id) { position = PointAt(live[i].path, live[i].s); return true; }
            return false;
        }

        void Update()
        {
            float dt = Clock.VizDelta;
            foreach (var id in emitting)
            {
                nextEmit.TryGetValue(id, out var t);
                t -= dt;
                if (t <= 0) { Fire(id); t = interval; }
                nextEmit[id] = t;
            }
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                float scale = p.path.space ? p.path.space.lossyScale.x : 1;
                p.s += speed * p.speedScale * dt;
                if (p.s - pulseLength > p.path.total) { p.line.enabled = false; live.RemoveAt(i); p.done?.Invoke(); continue; }
                for (int k = 0; k < buffer.Length; k++)
                {
                    float s = p.s - pulseLength * k / (buffer.Length - 1);
                    var a = PointAt(p.path, s);
                    var b = PointAt(p.path, s + .01f);
                    var side = Vector3.Cross((b - a).normalized, Camera.main ? Camera.main.transform.forward : Vector3.forward).normalized;
                    buffer[k] = a + side * ((k % 2 == 0 ? 1 : -1) * pulseLength * .18f * scale);
                }
                p.line.positionCount = buffer.Length;
                p.line.SetPositions(buffer);
                p.line.widthMultiplier = .02f * scale * widthScale;
            }
        }
    }
}
