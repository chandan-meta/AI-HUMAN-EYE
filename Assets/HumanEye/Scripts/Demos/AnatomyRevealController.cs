using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Skin, muscles and organs share a shoulder/head composition with an independent lower-edge fade. Layer changes are the
    /// reference clip's electric-cyan patchy disintegration (14 October 2026): one seeded noise field, shared by every layer
    /// through the _HE_Dissolve globals (Shaders/AnatomyDissolve.hlsl), erodes the departing layer in scattered islands while
    /// the next layer appears in exactly the same places; motes are born on the departing surface and a blue haze lingers.
    public class AnatomyRevealController : MonoBehaviour
    {
        public AnatomyPart skin, garment, skeleton, muscles, organs, brain;
        [Tooltip("Arteries and veins (ANATOMY.blend, 15 October 2026): shown and wiped with the organs.")] public AnatomyPart vessels;
        [Tooltip("Optic nerves, chiasm, tracts and the rest of the visual pathway: shown with the brain in the organ view.")] public AnatomyPart[] pathway;
        [Tooltip("Crown height and the fade level (m above the root): the lower edge of the bust frame, not the chest, carries the fade.")] public float height = 1.75f, shoulder = 1.4f;
        public float fadeWidth = .25f;
        [Header("Electric breakup (reference clip, 14 October 2026)")]
        [Tooltip("Noise amplitude (m): how far the patchy breakup departs from a flat sweep (about the bust's height makes it break everywhere at once). 0 = the plain horizontal wipe.")] public float breakupAmplitude = .45f;
        [Tooltip("Crack cells per metre, and a fixed seed so skip and replay repeat the same breakup.")] public float breakupFrequency = 20, breakupSeed = 7;
        [Tooltip("Reach (m) of the crack filaments around the breaking edge.")] public float filamentReach = .09f;
        public Color breakupColor = new Color(.15f, .8f, 1f);
        [Tooltip("HDR emission of the cracks (above the bloom threshold so they halo; too high and they clip to white).")] public float breakupIntensity = 2.2f;
        [Tooltip("Motes born on the departing surface: a mesh-shape emitter re-parented to that layer; DissolveMote.shader keeps only motes born where the field was breaking.")] public ParticleSystem motes;
        public float moteRate = 4000;
        [Tooltip("Soft blue glow behind the bust during each breakup (Glow quad; alpha driven here).")] public Renderer haze;
        public float hazePeak = .8f;
        public enum RevealState { Skin, TransitionToMuscle, Muscle, TransitionToOrgans, Organs, TransitionToEye, Complete }
        public RevealState State { get; private set; }
        /// Whether a layer breakup is in progress.
        public bool Wiping { get; private set; }
        AnatomyPart[] layers;
        ParticleSystem.Particle[] moteBuffer; readonly List<Vector4> stamps = new();
        MaterialPropertyBlock hazeBlock;

        static float Ease(float t, float a, float b) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));

        public void ResetVisual()
        {
            if (layers == null)
            {
                var all = new List<AnatomyPart> { skin, garment, skeleton, muscles, organs, vessels, brain };
                if (pathway != null) all.AddRange(pathway);
                layers = all.ToArray();
            }
            foreach (var part in layers) if (part) { part.ClearWipe(); part.SetBodyFade(0, 0, 0); }
            skeleton.SetAlpha(0, true); skeleton.ClearWipe();
            muscles.SetAlpha(0, true); muscles.ClearWipe();
            skeleton.gameObject.SetActive(false); muscles.gameObject.SetActive(false);
            foreach (var part in new[] { organs, vessels }) if (part) { part.SetAlpha(0, true); part.gameObject.SetActive(false); }
            if (brain) brain.SetAlpha(0, true);
            if (pathway != null) foreach (var p in pathway) if (p) p.SetAlpha(0, true);
            State = RevealState.Skin; Wiping = false;
            Breakup(false);
            if (motes && motes.gameObject.activeSelf) { motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); motes.gameObject.SetActive(false); }
            if (haze) haze.enabled = false;
        }

        public void Evaluate(float time)
        {
            if (layers == null) ResetVisual();
            float pad = breakupAmplitude * 1.05f;   // headroom: the noisy front starts with every layer intact and ends with it gone
            float top = transform.TransformPoint(Vector3.up * (height + .025f)).y + pad;
            float floor = transform.TransformPoint(Vector3.up * (shoulder - fadeWidth)).y - .025f - pad;
            float muscle = Ease(time, 17, 18.6f), organ = Ease(time, 22, 23.6f);
            float eyeFast = Ease(time, 25.5f, 26.4f);   // Clear the brain before the camera reaches the eye.
            State = time < 17 ? RevealState.Skin : time < 18.6f ? RevealState.TransitionToMuscle
                : time < 22 ? RevealState.Muscle : time < 23.6f ? RevealState.TransitionToOrgans
                : time < 25.5f ? RevealState.Organs : time < 28.5f ? RevealState.TransitionToEye : RevealState.Complete;
            Wiping = State is RevealState.TransitionToMuscle or RevealState.TransitionToOrgans;
            if (time < 17) ResetVisual();
            float fadeLevel = transform.TransformPoint(Vector3.up * shoulder).y;
            foreach (var part in layers) if (part) part.SetBodyFade(fadeLevel, fadeWidth * transform.lossyScale.y, Ease(time, 13, 15));
            if (time < 17) return;
            skeleton.gameObject.SetActive(false);
            muscles.gameObject.SetActive(time < 23.6f);
            foreach (var part in new[] { organs, vessels }) if (part) part.gameObject.SetActive(time >= 22 && time < 28.5f);
            float front = Mathf.Lerp(top, floor, muscle);
            skin.SetAlpha(1, true); garment.SetAlpha(1, true);
            float silhouette = organ * (1 - Ease(time, 26.5f, 27));
            skin.SetWipe(front, .085f * silhouette, 1); garment.SetWipe(front, .04f * silhouette, 1);
            muscles.SetAlpha(1, true);
            float organFront = Mathf.Lerp(top, floor, organ);
            if (time < 22) muscles.SetWipe(front, 1, 0);
            else muscles.SetWipe(organFront, 0, 1);
            foreach (var part in new[] { organs, vessels }) if (part) { part.SetAlpha(1 - eyeFast, true); part.SetWipe(organFront, 1, 0); }
            if (brain) { brain.SetAlpha(time >= 22 ? 1 - eyeFast : 0, true); brain.SetWipe(organFront, 1, 0); }
            if (pathway != null) foreach (var p in pathway) if (p) { p.SetAlpha(time >= 22 ? 1 - eyeFast : 0, true); p.SetWipe(organFront, 1, 0); }
            Breakup(Wiping);
            Motes(time < 22 ? muscle : organ, time < 22 ? front : organFront);
            Haze(time);
        }

        void Breakup(bool on)
        {
            Shader.SetGlobalVector("_HE_Dissolve", on ? new Vector4(breakupAmplitude, breakupFrequency, breakupSeed, filamentReach) : Vector4.zero);
            Shader.SetGlobalVector("_HE_DissolveOrigin", transform.position);
            Shader.SetGlobalVector("_HE_DissolveColor", new Vector4(breakupColor.r, breakupColor.g, breakupColor.b, breakupIntensity));
        }

        /// The emitter rides the departing layer's renderer (its mesh is the shape), emission peaks mid-breakup and every newborn
        /// mote is stamped with its birth position and the front, which the mote shader tests against the shared field.
        void Motes(float progress, float front)
        {
            if (!motes) return;
            var emission = motes.emission;
            if (!Wiping)
            {
                emission.rateOverTime = 0;   // survivors linger over the revealed layer, then the system rests
                if (motes.gameObject.activeSelf && motes.particleCount == 0) motes.gameObject.SetActive(false);
                return;
            }
            var surface = (State == RevealState.TransitionToMuscle ? skin : muscles).main[0];
            if (motes.transform.parent != surface.transform)
            {
                var shape = motes.shape;
                shape.mesh = surface is SkinnedMeshRenderer skinned ? skinned.sharedMesh : surface.GetComponent<MeshFilter>().sharedMesh;
                motes.transform.SetParent(surface.transform, false);
                motes.transform.localPosition = Vector3.zero; motes.transform.localRotation = Quaternion.identity; motes.transform.localScale = Vector3.one;
            }
            if (!motes.gameObject.activeSelf) motes.gameObject.SetActive(true);
            if (!motes.isPlaying) motes.Play(true);
            emission.rateOverTime = moteRate * Mathf.Sin(progress * Mathf.PI);
            moteBuffer ??= new ParticleSystem.Particle[motes.main.maxParticles];
            int count = motes.GetParticles(moteBuffer);
            motes.GetCustomParticleData(stamps, ParticleSystemCustomData.Custom1);
            float window = Time.deltaTime * 1.5f + .002f;
            for (int i = 0; i < count && i < stamps.Count; i++)
                if (moteBuffer[i].startLifetime - moteBuffer[i].remainingLifetime <= window)
                { var p = moteBuffer[i].position; stamps[i] = new Vector4(p.x, p.y, p.z, front); }
            motes.SetCustomParticleData(stamps, ParticleSystemCustomData.Custom1);
        }

        void Haze(float time)
        {
            if (!haze) return;
            float a = hazePeak * Mathf.Max(Pulse(time, 17, 18.6f), Pulse(time, 22, 23.6f));
            haze.enabled = a > .002f;
            if (!haze.enabled) return;
            hazeBlock ??= new MaterialPropertyBlock();
            hazeBlock.SetFloat("_Alpha", a); haze.SetPropertyBlock(hazeBlock);
        }

        // Rises through the first half of a breakup and fades about a second after the layer has gone.
        static float Pulse(float t, float from, float to) => Ease(t, from, from + (to - from) * .45f) * (1 - Ease(t, to - .2f, to + .9f));
    }
}
