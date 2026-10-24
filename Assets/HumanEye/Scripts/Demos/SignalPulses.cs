using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Neural signals: separated warm-white packets with tapered gold trails along nerve/cell paths.
    /// Deliberately unlike the smooth, warm-white light beams (continuous straight streaks, never comets), and always
    /// labelled, so light and signals never rely on colour alone. A point light rides on the lead pulse of the "nerve" path so the nerve and
    /// the ghosted brain are lit by the signal as it passes. Motion uses the visualization clock (FREEZE/Pause hold it).
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

        /// A live pulse: the handle a tracking shot follows. HeadArrived turns true when the head reaches the end of the
        /// path (the trail then shrinks into that point); Alive turns false once the trail has gone.
        public class Pulse
        {
            public Path Path { get; internal set; }
            public float Distance { get; internal set; }      // head position along the path (path units)
            public bool HeadArrived { get; internal set; }
            public bool Alive { get; internal set; }
            public float Progress => Path != null && Path.total > 0 ? Mathf.Clamp01(Distance / Path.total) : 1;
            internal LineRenderer line; internal float speedScale = 1; internal Action done;
        }

        public Path[] paths;
        public LineRenderer[] pool;
        public float speed = .6f, interval = .55f;
        public float widthScale = 1;

        [Header("Comet")]
        [Tooltip("Trail behind the head, in path units (world metres on the nerve).")] public float trailLength = .5f;
        [Tooltip("Head width in path units; the trail tapers to zero.")] public float headWidth = .07f;
        [Tooltip("Point light riding on the lead pulse of the 'nerve' path (created by the cinematic patch); off when none is live.")]
        public Light headLight;
        public float headLightIntensity = 6;
        public const int TrailPoints = 16;

        /// Raised with the path id when a pulse's head reaches the end of its path.
        public event Action<string> Arrived;
        /// Raised by StopAll (chapter checkpoints, demo cancel, menu) so the stage can restore anything tied to the signals.
        public event Action Stopped;

        readonly List<Pulse> live = new();
        readonly HashSet<string> emitting = new();
        readonly Dictionary<string, float> nextEmit = new();
        readonly Vector3[] buffer = new Vector3[TrailPoints];
        Pulse lightPulse; float lightLevel;

        void Awake()
        {
            foreach (var p in paths) Measure(p);
            // The material supplies the restrained HDR core; taper and alpha keep packets distinct.
            var width = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.1f, .6f), new Keyframe(.45f, .22f), new Keyframe(1, 0));
            var colour = new Gradient();
            colour.SetKeys(
                new[] { new GradientColorKey(new Color(1f, .98f, .86f), 0), new GradientColorKey(new Color(1f, .77f, .27f), .18f), new GradientColorKey(new Color(1f, .43f, .07f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.75f, .2f), new GradientAlphaKey(.25f, .6f), new GradientAlphaKey(0, 1) });
            foreach (var l in pool)
            {
                if (!l) continue;
                l.enabled = false; l.positionCount = 0;
                l.widthCurve = width; l.colorGradient = colour;
                l.alignment = LineAlignment.View; l.textureMode = LineTextureMode.Stretch;
            }
            if (headLight) headLight.enabled = false;
        }

        static void Measure(Path p)
        {
            p.lengths = new float[p.points.Length]; p.total = 0;
            for (int i = 1; i < p.points.Length; i++) { p.total += Vector3.Distance(p.points[i], p.points[i - 1]); p.lengths[i] = p.total; }
        }

        public Path Get(string id) => Array.Find(paths, p => p.id == id);

        public void Emit(string id, bool on) { if (on) emitting.Add(id); else emitting.Remove(id); }

        public void StopAll()
        {
            emitting.Clear();
            nextEmit.Clear();
            foreach (var p in live) { if (p.line) p.line.enabled = false; p.Alive = false; }
            live.Clear();
            lightPulse = null; lightLevel = 0;
            if (headLight) headLight.enabled = false;
            Stopped?.Invoke();
        }

        public int Live => live.Count;

        /// One pulse with a handle; null if the pool is exhausted (repeated requests never pile up beyond the pool).
        public Pulse Launch(string id, float speedScale = 1, Action done = null)
        {
            var path = Get(id);
            var line = Array.Find(pool, l => l && !l.enabled);
            if (path == null || !line) return null;
            line.enabled = true;
            var pulse = new Pulse { Path = path, Distance = 0, Alive = true, line = line, speedScale = speedScale, done = done };
            live.Add(pulse);
            return pulse;
        }

        /// One pulse; returns false if the pool is exhausted.
        public bool Fire(string id, float speedScale = 1, Action done = null) => Launch(id, speedScale, done) != null;

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

        /// The pulse furthest along a path whose head is still travelling (what a camera or light should follow).
        public Pulse LeadPulse(string id)
        {
            Pulse best = null;
            foreach (var p in live) if (p.Path.id == id && !p.HeadArrived && (best == null || p.Distance > best.Distance)) best = p;
            return best;
        }

        /// Position of the lead pulse on a path.
        public bool Lead(string id, out Vector3 position)
        {
            var p = LeadPulse(id);
            position = p != null ? PointAt(p.Path, p.Distance) : default;
            return p != null;
        }

        /// The most recently launched pulse on a path (nearest its start).
        public Pulse Newest(string id)
        {
            for (int i = live.Count - 1; i >= 0; i--) if (live[i].Path.id == id) return live[i];
            return null;
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
                float scale = p.Path.space ? p.Path.space.lossyScale.x : 1;
                p.Distance += speed * p.speedScale * dt;
                if (!p.HeadArrived && p.Distance >= p.Path.total) { p.HeadArrived = true; Arrived?.Invoke(p.Path.id); }
                if (p.Distance - trailLength >= p.Path.total)
                {
                    // the head has sat at the end while the trail caught up: the signal is absorbed, not cut off
                    p.line.enabled = false; p.Alive = false; live.RemoveAt(i);
                    p.done?.Invoke();
                    continue;
                }
                for (int k = 0; k < TrailPoints; k++) buffer[k] = PointAt(p.Path, p.Distance - trailLength * k / (TrailPoints - 1));
                p.line.positionCount = TrailPoints;
                p.line.SetPositions(buffer);
                p.line.widthMultiplier = headWidth * scale * widthScale;
            }
            UpdateHeadLight(dt);
        }

        /// The light rides one nerve pulse to the end, fades out there, then picks up the next lead (no jumps back).
        void UpdateHeadLight(float dt)
        {
            if (!headLight) return;
            bool riding = lightPulse != null && lightPulse.Alive && !lightPulse.HeadArrived;
            if (!riding && lightLevel <= 0) { lightPulse = LeadPulse("nerve"); riding = lightPulse != null; }
            lightLevel = Mathf.MoveTowards(lightLevel, riding ? 1 : 0, dt * 5);
            if (lightPulse != null) headLight.transform.position = PointAt(lightPulse.Path, lightPulse.Distance);
            headLight.intensity = headLightIntensity * lightLevel;
            bool on = lightLevel > .001f;
            if (headLight.enabled != on) headLight.enabled = on;
        }
    }
}
