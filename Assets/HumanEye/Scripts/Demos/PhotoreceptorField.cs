using UnityEngine;

namespace HumanEye
{
    /// Magnified retina (Scene 8). Light arrives from the vitreous side, passes the transparent inner layers and
    /// reaches rods and cones; the cells respond (pulsing glow) and electrical pulses pass through the retinal
    /// cells toward the optic nerve. Photons never travel to the brain; only the zig-zag signals continue.
    public class PhotoreceptorField : MonoBehaviour
    {
        public AnatomyPart rods, cones;
        public Renderer[] rodTips, coneTips;
        public LineRenderer[] incoming;
        public SignalPulses signals;
        public string[] signalPaths;
        public Transform[] extraLabels;
        [Range(0, 1)] public float lightLevel = .6f;
        public bool LightOn { get; private set; }
        public bool SignalsOn { get; private set; }

        /// Grounded in the two source statements only: rods respond well even in dim light; cones work best in bright light.
        public float RodResponse => LightOn ? Mathf.Lerp(.95f, .6f, lightLevel) : 0;
        public float ConeResponse => LightOn ? Mathf.Lerp(.08f, 1f, lightLevel) : 0;

        MaterialPropertyBlock block;
        float t;

        public void ResetState() { SetLight(false, .6f); SetSignals(false); rods.SetGlow(0, true); cones.SetGlow(0, true); }

        public void SetLight(bool on, float level) { LightOn = on; lightLevel = level; }

        public void SetSignals(bool on)
        {
            SignalsOn = on;
            if (!on) foreach (var p in signalPaths) signals.Emit(p, false);
        }

        void Update()
        {
            t += Clock.VizDelta;
            var light = new Color(1, .92f, .65f, LightOn ? Mathf.Lerp(.15f, .9f, lightLevel) : 0);
            foreach (var l in incoming) { l.enabled = LightOn; l.startColor = l.endColor = light; }
            Pulse(rodTips, RodResponse, new Color(.75f, .55f, 1f), 0);
            Pulse(coneTips, ConeResponse, new Color(1f, .8f, .45f), 1.3f);
            for (int i = 0; i < signalPaths.Length; i++)
            {
                bool rodPath = i % 3 != 2;   // two of every three paths start at rods
                float response = rodPath ? RodResponse : ConeResponse;
                signals.Emit(signalPaths[i], SignalsOn && response > .25f);
            }
        }

        void Pulse(Renderer[] tips, float response, Color c, float phase)
        {
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < tips.Length; i++)
            {
                float flicker = .55f + .45f * Mathf.Sin(t * 5 + i * .7f + phase);
                tips[i].GetPropertyBlock(block);
                block.SetColor("_Emission", c * (response * flicker * 1.4f));
                tips[i].SetPropertyBlock(block);
            }
        }
    }
}
