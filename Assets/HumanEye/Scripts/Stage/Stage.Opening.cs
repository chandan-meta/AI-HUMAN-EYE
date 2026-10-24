using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace HumanEye
{
    public partial class Stage
    {
        public const float OpeningLength = 21f;
        Material openingSky, authoredSky;
        float authoredSkyExposure;
        [Tooltip("Small cyan burst at the cutaway and at the light's arrival on the retina.")] public ParticleSystem anatomyTransition;
        [Tooltip("Electric breakup motes, born on the departing layer's surface (driven by AnatomyRevealController).")] public ParticleSystem anatomyMotes;
        // Kept for the authoring patches that still set them (the gate-to-face opening they tuned is retired).
        [HideInInspector] public float wideDistance = 10.5f, wideHeight = 1.75f, wideFov = 46, fullDistance = 4.6f, fullFov = 38,
            bustDistance = 2.05f, bustLookDrop = .33f, bustFov = 35, corridorLift = .45f, labLift = -.1f, gateApproach = 8f, gateFov = 50f;

        /// The production opening, "the eye awakens" (22 October 2026 brief: the gate, the standing figure and the organ
        /// layers were dull; no human figure). The learner's eye floats at the display in the dark observatory, lit only at its
        /// rim (the start screen's own view). Light arrives and the wide pupil tightens; the camera swings to the side as the
        /// room wakes; the eye opens into its cross-section in a cyan burst; light from the apple is bent by the cornea and the
        /// lens to a spark on the retina; THE HUMAN EYE; the cutaway closes, the lab comes up and the camera settles on the first
        /// lesson shot, where Niko arrives with his first line. A pure function of time (AwakeningFrame) drives it, so Pause
        /// holds it and the reduced-motion stills are frames of it. No electric sound here is a buzzer: soft cues only.
        IEnumerator Opening(CinemachineCamera dolly)
        {
            AwakeningSetup();
            cams.Snap("S02_IntroDolly"); GradeOn(true);
            var cues = new (float at, System.Action act)[]
            {
                (.2f, () => sfx.Play("hologram", .35f)),
                (2.6f, () => sfx.Play("pupilnarrow", .6f)),
                (4.6f, () => sfx.Play("softwhoosh", .35f)),
                (8.6f, () => { sfx.Play("dissolve", .55f); AwakeningPulse(anatomyTransition, false); sfx.BeginIntroMusic("transition"); }),
                (11f, () => sfx.Play("layerreveal", .45f)),
                (12f, () => sfx.Play("lightmove", .55f)),
                (14.4f, () => { sfx.Play("cell", .7f); AwakeningPulse(anatomyTransition, true); }),
                (15.2f, () => { sfx.Play("labsting", .6f); AwakeningPulse(titleBurst, false); }),
                (17.6f, () => sfx.EndIntroMusic()),
                (19.2f, () => sfx.Play("softwhoosh", .35f)),
                (20.2f, () => sfx.Play("hologram", .4f)),
            };
            int next = 0;
            for (float t = 0; t < OpeningLength && !skipIntro; t += Clock.Delta)
            {
                IntroTime = t;
                while (next < cues.Length && t >= cues[next].at) cues[next++].act();
                AwakeningFrame(dolly, t);
                yield return null;
            }
            if (skipIntro) { sfx.StopEffects(); sfx.Play("skip", .6f); }
        }

        /// The opening's (and the start screen's) world: no figure, no exhibit, the learner's eye whole at the display.
        void AwakeningSetup()
        {
            human.gameObject.SetActive(false); otherEye.gameObject.SetActive(false); Exhibit(0);
            eyeRig.gameObject.SetActive(true); PlaceEye(heroPoint);
            eye.transform.localPosition = Vector3.zero; eye.transform.localRotation = Quaternion.identity; eye.transform.localScale = Vector3.one;
            eye.SetCut(0); eye.SetNerveReveal(.12f); eye.SetPupil(EyeGeometry.PupilMax * .95f, true);
            if (entrance) { entrance.SetGate(0); entrance.SetAccents(0); }
            // Reuse the authored systems so Pause, reduced motion and every exit share the existing cleanup.
            float r = eye.transform.lossyScale.x;
            if (depthDust)
            {
                var main = depthDust.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(5, 7); main.startSpeed = .025f * r;
                main.startSize = new ParticleSystem.MinMaxCurve(.006f * r, .014f * r); main.maxParticles = 120;
                main.startColor = new Color(.65f, .82f, 1, .65f);
                var emission = depthDust.emission; emission.rateOverTime = 18;
                var shape = depthDust.shape; shape.radius = 1.9f * r;
            }
            if (extractMotes)
            {
                var main = extractMotes.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f); main.startSpeed = .035f * r;
                main.startSize = new ParticleSystem.MinMaxCurve(.012f * r, .025f * r); main.maxParticles = 180;
                main.startColor = new Color(.35f, .8f, 1, .75f);
                var emission = extractMotes.emission; emission.rateOverTime = 65;
                var shape = extractMotes.shape; shape.radius = 1.08f * r; shape.radiusThickness = .08f;
                var velocity = extractMotes.velocityOverLifetime; velocity.orbitalY = .55f;
            }
        }

        static float Seg(float t, float from, float to) => Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - from) / (to - from)));

        // The panorama is emissive: dim its runtime copy independently of the eye's lights and leave the saved sky untouched.
        void AwakeningSky(float level)
        {
            if (!openingSky)
            {
                authoredSky = RenderSettings.skybox;
                if (!authoredSky || !authoredSky.HasProperty("_Exposure")) return;
                authoredSkyExposure = authoredSky.GetFloat("_Exposure");
                openingSky = new Material(authoredSky) { hideFlags = HideFlags.HideAndDontSave };
            }
            openingSky.SetFloat("_Exposure", authoredSkyExposure * level);
            RenderSettings.skybox = openingSky;
        }

        void RestoreAwakeningSky()
        {
            if (openingSky && RenderSettings.skybox == openingSky) RenderSettings.skybox = authoredSky;
        }

        void OnDestroy()
        {
            RestoreAwakeningSky();
            if (openingSky) Destroy(openingSky);
        }

        // A rim wave keeps the reveal readable; the retinal beat is a small, local spark rather than a white flash.
        void AwakeningPulse(ParticleSystem ps, bool retina)
        {
            if (!ps || Short) return;
            var e = eye.transform; float r = e.lossyScale.x;
            var centre = retina ? e.TransformPoint(new Vector3(0, 0, -EyeGeometry.RetinaIn)) : e.position;
            Particles(ps, true, centre);
            var particle = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            int count = retina ? 28 : 72;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                var radial = e.forward * Mathf.Cos(a) + e.up * Mathf.Sin(a);
                particle.position = centre + radial * (retina ? .035f : 1.04f) * r;
                particle.velocity = radial * (retina ? .18f : .32f) * r;
                particle.startSize = (retina ? .025f : .035f) * r;
                particle.startLifetime = retina ? .9f : 1.25f;
                particle.startColor = retina ? new Color(1, .8f, .35f, .85f) : new Color(.25f, .75f, 1, .65f);
                ps.Emit(particle, 1);
            }
        }

        void AwakeningFrame(CinemachineCamera dolly, float t)
        {
            var e = eye.transform; var E = e.position; float r = e.lossyScale.x;
            Vector3 f = e.forward, side = -e.right, up = Vector3.up;   // side: where the removable half slides to, the cutaway's viewer
            Vector3 Around(float deg, float dist, float h) => E + (f * Mathf.Cos(deg * Mathf.Deg2Rad) + side * Mathf.Sin(deg * Mathf.Deg2Rad)) * dist * r + up * h * r;
            var mid = E + f * 3.6f * r;   // between the eye and the apple
            // Camera keys: (time, position, look, lens). Each segment eases in and out; the holds read as stills.
            var keys = new (float t, Vector3 pos, Vector3 look, float fov)[]
            {
                (0, Around(12, 4.2f, .25f), E, 32),
                (4.4f, Around(15, 3.9f, .22f), E, 32),
                (8.6f, Around(78, 5.35f, .4f), E, 36),
                (11.2f, Around(86, 5.15f, .35f), E, 36),
                (12.8f, mid + side * 10.4f * r + up * 1.9f * r, mid - up * .1f * r, 38),
                (14.6f, mid + side * 10f * r + up * 1.8f * r, mid - up * .1f * r, 38),
                (17.6f, E + f * .3f * r + side * 4.8f * r + up * .3f * r, E - f * .15f * r, 36),
            };
            int k = 0; while (k < keys.Length - 2 && t >= keys[k + 1].t) k++;
            var a = keys[k]; var b = keys[k + 1]; float u = Seg(t, a.t, b.t);
            var pos = Vector3.Lerp(a.pos, b.pos, u); var look = Vector3.Lerp(a.look, b.look, u); float fov = Mathf.Lerp(a.fov, b.fov, u);
            // On the start screen (t = 0) the eye sits right of centre, clear of the title card; it glides to the centre as the light comes.
            var shift = Vector3.Cross((look - pos).normalized, Vector3.up) * (1.1f * r * (1 - Seg(t, 1, 4.4f)));   // the camera's left
            pos += shift; look += shift;
            var settle = cams.Get(SettleShot);
            float hand = Seg(t, 18.6f, OpeningLength);   // the hand-over to the first lesson shot
            if (settle && hand > 0)
            {
                var from = Quaternion.LookRotation(look - pos, Vector3.up);
                var rot = Quaternion.Slerp(from, settle.transform.rotation, hand);
                pos = Vector3.Lerp(pos, settle.transform.position, hand);
                look = pos + rot * Vector3.forward; fov = Mathf.Lerp(fov, cams.AuthoredFov(settle), hand);
            }
            if (dolly) Dolly(dolly, pos, look, fov, .01f);

            // The eye: the wide, dark-adapted pupil tightens when the light arrives; the cutaway opens, then closes for the lesson.
            if (t < 2.6f) eye.SetPupil(EyeGeometry.PupilMax * .95f, true); else eye.SetPupil(EyeGeometry.PupilMin * 1.25f, false);
            eye.SetCut(Seg(t, 8.6f, 10.2f) * (1 - Seg(t, 19.2f, 20.6f)));   // it closes only as the camera leaves for Niko's shot
            var retina = eye.Get("retina"); if (retina) retina.SetGlow(Seg(t, 14.3f, 14.6f) * (1 - Seg(t, 15.6f, 17.2f)), true);

            // The apple and its light: in from 11 s, the rays crossing cornea and lens to the retina by 14.4 s, gone for the lesson.
            float apple = Seg(t, 11, 11.7f) * (1 - Seg(t, 17.6f, 18.3f));
            if (apple > .001f)
            {
                Optic("apple", 7, 5 * Seg(t, 12, 14.4f)); rays.showImage = false;
                appleRoot.localScale = Vector3.one * appleBaseScale * Mathf.Max(.01f, apple);
                pedestal.localScale = BaseScale(pedestal) * Mathf.Max(.01f, apple);
            }
            else if (appleRoot.gameObject.activeSelf || rays.gameObject.activeSelf) { ShowObject("apple", false); rays.gameObject.SetActive(false); }

            // Keep the eye as the brightest subject; restore the hall only during the lesson handover.
            float wake = Seg(t, 4.4f, 9), lab = Seg(t, 18.2f, OpeningLength), cut = Seg(t, 8, 10.2f);
            Room(Mathf.Lerp(.045f, .14f, wake) * (1 - lab) + lab);
            AmbientScale(Mathf.Lerp(.28f, .48f, wake) * (1 - lab) + lab);
            AwakeningSky(Mathf.Lerp(.16f, .24f, wake) * (1 - lab) + lab);
            keyLight.intensity = keyIntensity * Mathf.Lerp(.06f, .22f, wake) * (1 - lab) + keyIntensity * lab;
            displayLight.intensity = displayIntensity * lab;
            float arrive = Seg(t, 2.4f, 3.1f);
            if (eyeKey)
            {
                var at = E + f * 2.4f * r + up * 1.7f * r - side * 1.3f * r;
                eyeKey.enabled = arrive > .001f; eyeKey.transform.SetPositionAndRotation(at, Quaternion.LookRotation(E - at));
                eyeKey.intensity = eyeKeyBase * Mathf.Lerp(.95f, .48f, cut) * arrive;
            }
            if (eyeRim)
            {
                var at = E - f * 1.4f * r + up * 1.1f * r + side * .4f * r;
                eyeRim.enabled = true; eyeRim.transform.SetPositionAndRotation(at, Quaternion.LookRotation(E - at));
                eyeRim.intensity = eyeRimBase * Mathf.Lerp(1.55f, 1.05f, cut);
            }
            if (eyeFill)
            {
                var at = E + side * 2f * r + f * .6f * r - up * .2f * r;
                eyeFill.enabled = true; eyeFill.transform.SetPositionAndRotation(at, Quaternion.LookRotation(E - at));
                eyeFill.intensity = eyeFillBase * Mathf.Lerp(.18f, .4f, wake);
            }
            float exposure = Mathf.Lerp(-1.65f, -.65f, Seg(t, 2.4f, 5));
            float focus = t < 12.2f || t > 15.1f ? Vector3.Distance(pos, E) + 1.65f * r : 0;
            Look(exposure * (1 - lab), Mathf.Lerp(.34f, labVignette, lab), 4 * (1 - lab), -3 * (1 - lab), 0, Mathf.Lerp(1.45f, 1, lab), Short ? 0 : focus * (1 - hand));
            if (!Clock.Paused && !Clock.Frozen)
            {
                Particles(depthDust, IntroPlaying && !Short && t > .35f && t < 18.5f, E);
                Particles(extractMotes, IntroPlaying && !Short && t > 7.8f && t < 11.8f, E);
            }
            sfx.musicGain = 1;
            hud.Fade(0);

            IntroCaption = t >= 2.8f && t < 6.4f ? "Light arrives, and the pupil tightens to let in just enough."
                : t >= 9.6f && t < 12.2f ? "Inside: a lens, clear jelly, and the light-sensitive retina."
                : t >= 12.4f && t < 15.1f ? "The cornea and the lens bend the light onto the retina."
                : null;
            if (t >= 15.2f && t < 18.6f)
                SetTitle("THE HUMAN EYE", "HOW LIGHT BECOMES SIGHT", Seg(t, 15.2f, 16), Seg(t, 15.7f, 16.4f) * (1 - Seg(t, 18, 18.6f)), 1 - Seg(t, 18, 18.6f));
            else ClearTitle();
        }
    }
}
