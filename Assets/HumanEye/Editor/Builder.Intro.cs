using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye.EditorTools
{
    /// Cinematic pass step: scene objects the opening needs (Stage.Scene02.cs `Intro`). Idempotent, find-or-create by
    /// name, absolute values: the lab establishing shot `S02_Arrival`, the "Depth dust" and "Niko sparkle" systems (under
    /// the observatory group so Stage pauses them; inactive until their beat), and the IntroProfile overrides the intro
    /// drives per frame (grade, vignette, chromatic aberration, bloom, Gaussian depth of field). The pupil-entry rays and
    /// motes of the 7 October opening were retired by the reference pass (Builder.Reference.cs removes their objects).
    /// Called by Builder.ApplyCinematic (Builder.Cinematic.cs).
    public static partial class Builder
    {
        static void CinematicIntro()
        {
            ArrivalShot();
            var soft = ProceduralTextures.Soft();
            var group = stage.observatory.transform;
            stage.depthDust = DepthDust(group, soft);
            stage.nikoSparkle = NikoSparkle(group, soft);
            IntroProfile();
            EditorUtility.SetDirty(stage);
            Debug.Log($"CINEMATIC_INTRO arrival={stage.cams.shots.Length} shots, particles dust/sparkle set, profile overrides set");
        }

        /// Lab establishing view after the interior: the human on the pedestal right of centre, Niko's Intro anchor clear on
        /// the left, from the front of the hall (the subject side faces −z, like every Scene 2 front shot).
        static void ArrivalShot()
        {
            var anchor = stage.Anchor("Intro"); var head = stage.human.eyeRight.position; var human = stage.human.transform.position;
            var look = Vector3.Lerp(anchor.position + Vector3.up * .55f, new Vector3(human.x, head.y - .45f, human.z), .5f);
            var pos = new Vector3(look.x - .35f, head.y + .15f, Mathf.Min(anchor.position.z, human.z) - 4.2f);
            WowPatch.Shot(stage, "S02_Arrival", pos, look, 40);
        }



        /// Fine drifting dust: in front of the face during the portrait, then inside the eye volume (radius .3 m).
        static ParticleSystem DepthDust(Transform parent, Texture soft)
        {
            var ps = IntroParticles(parent, "Depth dust", "IntroDust", soft, new Color(1f, .96f, .9f, .45f), 1.3f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 10f); main.startSpeed = new ParticleSystem.MinMaxCurve(.015f, .03f);
            main.startSize = new ParticleSystem.MinMaxCurve(.003f, .007f); main.maxParticles = 150; main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 22;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .3f; sh.radiusThickness = 1;
            ps.transform.position = stage.heroPoint ? stage.heroPoint.position : Vector3.zero;
            return ps;
        }

        /// Burst-only system (rate 0, Emit at runtime): a soft cool sparkle where Niko appears.
        static ParticleSystem NikoSparkle(Transform parent, Texture soft)
        {
            var ps = IntroParticles(parent, "Niko sparkle", "IntroSparkle", soft, new Color(.6f, .9f, 1f, .7f), 2f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.1f); main.startSpeed = new ParticleSystem.MinMaxCurve(.25f, .5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.02f, .045f); main.maxParticles = 60; main.prewarm = false;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .12f; sh.radiusThickness = 1;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            ps.transform.position = stage.Anchor("Intro").position + Vector3.up * .55f;
            return ps;
        }

        /// Shared setup: looping world-space billboard system with a soft sprite and an alpha fade in/out over its life,
        /// parented under the observatory group and left inactive (Stage switches it on only for its beat; Stage.Update would
        /// otherwise keep every active system under the group playing).
        static ParticleSystem IntroParticles(Transform parent, string name, string materialName, Texture soft, Color colour, float intensity)
        {
            var t = parent.Find(name);
            var go = t ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.GetComponent<ParticleSystem>(); if (!ps) ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.useUnscaledTime = false; main.startColor = colour; main.gravityModifier = 0;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Local;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sharedMaterial = Glow(materialName, colour, soft, intensity);
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            go.SetActive(false);
            EditorUtility.SetDirty(ps); EditorUtility.SetDirty(r);
            return ps;
        }

        /// IntroProfile (the "Intro Exposure" volume, priority 2): every override Stage.Look drives, saved at the lab grade's
        /// own values so weight 1 with nothing ramped looks exactly like the lab. Components are added to the existing asset.
        static void IntroProfile()
        {
            var volume = stage.introVolume;
            var profile = volume ? volume.sharedProfile : AssetDatabase.LoadAssetAtPath<VolumeProfile>(Gen + "Data/IntroProfile.asset");
            if (!profile) { Debug.LogWarning("CinematicIntro: IntroProfile missing (run the production pass first); the intro falls back to lights and fades only."); return; }
            var lab = stage.post ? stage.post.sharedProfile : null;
            float labExposure = .25f, labContrast = 10, labSaturation = 4, labVignette = .18f, labSmooth = .45f, bloomThreshold = 1.15f, bloomIntensity = .14f, bloomScatter = .6f;
            if (lab)
            {
                if (lab.TryGet(out ColorAdjustments g)) { labExposure = g.postExposure.value; labContrast = g.contrast.value; labSaturation = g.saturation.value; }
                if (lab.TryGet(out Vignette v)) { labVignette = v.intensity.value; labSmooth = v.smoothness.value; }
                if (lab.TryGet(out Bloom b)) { bloomThreshold = b.threshold.value; bloomIntensity = b.intensity.value; bloomScatter = b.scatter.value; }
            }
            var grade = ProfileComponent<ColorAdjustments>(profile);
            grade.postExposure.Override(labExposure); grade.contrast.Override(labContrast); grade.saturation.Override(labSaturation);
            var vignette = ProfileComponent<Vignette>(profile);
            vignette.intensity.Override(labVignette); vignette.smoothness.Override(labSmooth);
            var aberration = ProfileComponent<ChromaticAberration>(profile);
            aberration.intensity.Override(0);
            var bloom = ProfileComponent<Bloom>(profile);
            bloom.threshold.Override(bloomThreshold); bloom.intensity.Override(bloomIntensity); bloom.scatter.Override(bloomScatter);
            var dof = ProfileComponent<DepthOfField>(profile);
            dof.mode.Override(DepthOfFieldMode.Off); dof.gaussianStart.Override(3); dof.gaussianEnd.Override(7);
            dof.gaussianMaxRadius.Override(1); dof.highQualitySampling.Override(false);
            if (volume) { volume.isGlobal = true; volume.priority = 2; volume.weight = 0; EditorUtility.SetDirty(volume); }
            EditorUtility.SetDirty(profile);
        }

        static T ProfileComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(component);
            return component;
        }
    }
}
