using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        // Layout (metres). The anatomical display sits at the centre; the eye is shown at radius 0.4 m.
        public static readonly Vector3 EyeCentre = new(0, 1.5f, .6f);
        public const float EyeRadius = .4f;
        public static readonly Quaternion HeroRotation = Quaternion.LookRotation(new Vector3(-.42f, 0, -1).normalized, Vector3.up);
        public static readonly Quaternion CutRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        public static readonly Vector3 HumanPosition = new(0, .15f, 2.0f);
        public static readonly Vector3 MicroPosition = new(60, 0, 0);
        const float DomeRadius = 16;

        static Transform env;
        static Light keyLight, displayLight;
        static readonly List<Renderer> beams = new();

        static Mesh Disc(string name, float radius, int seg = 96) =>
            SaveMesh(Lathe.Revolve(new List<Vector2[]> { Lathe.Line(new Vector2(radius, 0), new Vector2(0, 0), 4) }, 0, 360, seg), name);

        static Mesh Ring(string name, float radius, float tube, int seg = 96)
        {
            var path = new List<Vector3>();
            for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2 / seg; path.Add(new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius)); }
            return SaveMesh(Lathe.Tube(path, tube, 10), name);
        }

        static Mesh Cylinder(string name, float radius, float height, int seg = 48)
        {
            var profile = new List<Vector2[]> { Lathe.Line(new Vector2(0, 0), new Vector2(radius, 0)), Lathe.Line(new Vector2(radius, 0), new Vector2(radius, height)), Lathe.Line(new Vector2(radius, height), new Vector2(0, height)) };
            return SaveMesh(Lathe.Revolve(profile, 0, 360, seg), name);
        }

        /// Lathe meshes revolve around Z; this rotation stands them upright (Z → Y).
        static readonly Quaternion Upright = Quaternion.Euler(-90, 0, 0);

        static void BuildEnvironment()
        {
            beams.Clear();
            env = new GameObject("Observatory of Vision").transform;
            var floor = Lit("Floor", new Color(.045f, .065f, .085f), .88f, .55f);
            var metal = Lit("BrushedMetal", new Color(.32f, .36f, .42f), .62f, .85f);
            var brass = Lit("Brass", new Color(.62f, .42f, .18f), .78f, .85f);
            var sky = ProceduralTextures.ObservatorySky();
            var domeMat = Lit("Dome", Color.white, .1f, baseMap: sky, emission: Color.white);
            domeMat.SetTexture("_EmissionMap", sky);
            var glass = Anatomy("DisplayGlass", new Color(.75f, .9f, 1f, 1), m => { m.SetFloat("_Gloss", .95f); m.SetFloat("_Specular", 1); m.SetFloat("_Rim", .6f); m.SetFloat("_Alpha", .22f); m.SetFloat("_SrcBlend", 5); m.SetFloat("_DstBlend", 10); m.SetFloat("_ZWrite", 0); m.renderQueue = 3010; });
            var soft = ProceduralTextures.Soft();
            var accent = Glow("AccentLine", new Color(.15f, .75f, .95f, .55f), soft, 1.2f);

            Obj("Floor", env, Disc("FloorDisc", DomeRadius), floor, Vector3.zero, Upright).GetComponent<Renderer>().receiveShadows = true;
            for (int i = 1; i <= 4; i++) Obj("FloorInlay" + i, env, Ring("InlayRing" + i, 2.2f + i * 2.6f, .012f), accent, new Vector3(0, .004f, 0));
            var plinth = Obj("Display Plinth", env, Cylinder("Plinth", 1.45f, .15f), metal, new Vector3(0, 0, 2), Upright);
            plinth.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            Obj("Plinth Ring", env, Ring("PlinthRing", 1.47f, .018f), Glow("DisplayRing", new Color(.2f, .85f, 1f, .9f), soft, 2), new Vector3(0, .15f, 2));
            Obj("Eye Display Ring", env, Ring("EyeDisplayRing", .62f, .01f), Glow("EyeRingGlow", new Color(.2f, .85f, 1f, .45f), soft, 1.5f), new Vector3(0, .9f, .6f));
            Obj("Eye Plinth Base", env, Cylinder("EyePlinthBase", .73f, .12f), metal, new Vector3(0, .65f, .6f), Upright);
            Obj("Eye Plinth Rim", env, Cylinder("EyePlinthRim", .72f, .035f), brass, new Vector3(0, .77f, .6f), Upright);
            Obj("Eye Plinth Top", env, Cylinder("EyePlinthTop", .67f, .09f), floor, new Vector3(0, .805f, .6f), Upright);

            // Dome (inner surface) with meridian ribs and a star field; an oculus lets the starlight in.
            var domeProfile = new List<Vector2[]> { Lathe.Arc(0, DomeRadius, 90, 9, 40), Lathe.Arc(0, DomeRadius - .3f, 9, 90, 40) };
            var dome = Lathe.Revolve(domeProfile, 0, 360, 96);
            Flip(dome);
            Obj("Dome", env, SaveMesh(dome, "Dome"), domeMat, Vector3.zero, Upright);
            for (int i = 0; i < 16; i++)
            {
                var path = new List<Vector3>();
                float a = i * Mathf.PI * 2 / 16;
                for (int k = 0; k <= 24; k++)
                {
                    float phi = Mathf.Lerp(90, 10, k / 24f) * Mathf.Deg2Rad, r = (DomeRadius - .35f) * Mathf.Sin(phi);
                    path.Add(new Vector3(Mathf.Cos(a) * r, (DomeRadius - .35f) * Mathf.Cos(phi), Mathf.Sin(a) * r));
                }
                Obj("Rib" + i, env, SaveMesh(Lathe.Tube(path, .11f, 8), "Rib" + i), brass);
            }
            var warm = Lit("WarmFixtures", new Color(1, .72f, .35f), .4f, emission: new Color(3, 1.6f, .55f));
            var fixture = Cylinder("WallFixture", .055f, .65f, 12);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16;
                foreach (float y in new[] { .6f, 3.3f, 6.0f })
                {
                    float r = Mathf.Sqrt((DomeRadius - .7f) * (DomeRadius - .7f) - y * y);
                    Obj("Warm wall fixture", env, fixture, warm, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), Upright);
                }
            }
            Obj("Lower Gallery Rail", env, Ring("GalleryRail", 15.1f, .09f), brass, new Vector3(0, 1.1f, 0));
            Obj("Window Rail", env, Ring("WindowRail", 14.9f, .065f), brass, new Vector3(0, 3.5f, 0));
            Obj("Stars", env, SaveMesh(Stars(2400, DomeRadius - .6f), "Stars"), Glow("Star", new Color(.75f, .88f, 1f, .9f), soft, 1.4f));

            // Giant starlight beam through an aperture ring and a lens, focusing toward the floor behind the display.
            var top = new Vector3(-4.5f, 15.2f, 9); var bottom = new Vector3(3.5f, 0, 7.5f);
            var noise = ProceduralTextures.BeamNoise();
            var beamMat = Glow("Starlight", new Color(.55f, .78f, 1f, .1f), noise, .9f, new Vector2(0, -.08f), 1.6f, 1);
            Beam("Starlight Beam", top, Vector3.Lerp(top, bottom, .62f), 1.25f, 1.05f, beamMat);
            Beam("Focused Beam", Vector3.Lerp(top, bottom, .62f), bottom, 1.0f, .25f, beamMat);
            var apertureAt = Vector3.Lerp(top, bottom, .38f); var lensAt = Vector3.Lerp(top, bottom, .62f);
            var axis = (bottom - top).normalized;
            Obj("Aperture", env, Ring("ApertureRing", 1.5f, .07f), brass, apertureAt, Quaternion.FromToRotation(Vector3.up, axis));
            Obj("Aperture Glow", env, Ring("ApertureGlowRing", 1.42f, .02f), accent, apertureAt, Quaternion.FromToRotation(Vector3.up, axis));
            var lensProfile = new List<Vector2[]> { Lathe.Arc(-1.6f, 1.75f, 0, 23.9f, 16), Lathe.Arc(1.6f, 1.75f, 156.1f, 180, 16) };   // rims meet at r≈0.71
            Obj("Starlight Lens", env, SaveMesh(Lathe.Revolve(lensProfile, 0, 360, 64), "StarLens"), glass, lensAt, Quaternion.FromToRotation(Vector3.forward, axis), Vector3.one * 1.55f);
            Obj("Lens Frame", env, Ring("LensFrame", 1.15f, .06f), brass, lensAt, Quaternion.FromToRotation(Vector3.up, axis));
            Dust(Vector3.Lerp(top, bottom, .5f), axis, Vector3.Distance(top, bottom), soft);

            // Optical benches around the room: lenses and apertures on stands (controlled reflections, not clutter).
            var standLens = SaveMesh(Lathe.Revolve(new List<Vector2[]> { Lathe.Arc(-.5f, .56f, 0, 26.7f, 10), Lathe.Arc(.5f, .56f, 153.3f, 180, 10) }, 0, 360, 48), "StandLens");
            var pole = Cylinder("StandPole", .035f, 1.2f, 16);
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Deg2Rad * (35 + i * 22) * (i % 2 == 0 ? 1 : -1) + Mathf.PI / 2;
                var p = new Vector3(Mathf.Cos(a) * 9.5f, 0, Mathf.Sin(a) * 9.5f + 2);
                Obj("Bench Pole " + i, env, pole, metal, p, Upright);
                var face = Quaternion.LookRotation(new Vector3(-p.x, 0, 2 - p.z).normalized);
                if (i % 2 == 0) Obj("Bench Lens " + i, env, standLens, glass, p + Vector3.up * 1.45f, face, Vector3.one * 1.4f);
                else Obj("Bench Aperture " + i, env, Ring("BenchAperture", .3f, .025f), brass, p + Vector3.up * 1.45f, face * Quaternion.Euler(90, 0, 0));
            }

            keyLight = new GameObject("Key Light").AddComponent<Light>();
            keyLight.transform.SetParent(env); keyLight.type = LightType.Directional; keyLight.color = new Color(.92f, .95f, 1f); keyLight.intensity = 1.25f;
            keyLight.transform.rotation = Quaternion.Euler(38, 28, 0); keyLight.shadows = LightShadows.Soft;
            displayLight = new GameObject("Display Light").AddComponent<Light>();
            displayLight.transform.SetParent(env); displayLight.type = LightType.Spot; displayLight.color = new Color(1f, .96f, .9f);
            displayLight.intensity = 9; displayLight.range = 9; displayLight.spotAngle = 52; displayLight.shadows = LightShadows.None;
            displayLight.transform.SetPositionAndRotation(new Vector3(0, 5.2f, -1.2f), Quaternion.LookRotation(new Vector3(0, -4, 2.6f)));
            foreach (var (pos, col) in new[] { (new Vector3(1.8f, 2.4f, 2.6f), new Color(.25f, .75f, 1f)), (new Vector3(-2.2f, 2.0f, 2.8f), new Color(.35f, .6f, 1f)) })
            {
                var rim = new GameObject("Rim Light").AddComponent<Light>();
                rim.transform.SetParent(env); rim.transform.position = pos; rim.type = LightType.Point; rim.color = col; rim.intensity = 3; rim.range = 6;
            }

            var post = new GameObject("Post (bloom, tonemapping)").AddComponent<Volume>();
            post.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(); bloom.threshold.Override(1.05f); bloom.intensity.Override(.55f); bloom.scatter.Override(.55f);
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            profile.Add<Vignette>().intensity.Override(.2f);
            post.sharedProfile = SaveProfile(profile, "PostProfile");
            stage.observatory = env.gameObject; stage.keyLight = keyLight; stage.displayLight = displayLight;
            stage.post = post; stage.beamRenderers = beams.ToArray();
            // One room capture on load, not a six-camera render every frame.
            var reflection = new GameObject("Observatory Reflection").AddComponent<ReflectionProbe>();
            reflection.transform.SetParent(env); reflection.transform.position = new Vector3(0, 2, 0);
            reflection.mode = ReflectionProbeMode.Realtime; reflection.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            reflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            reflection.resolution = 128; reflection.size = new Vector3(32, 18, 32);
            reflection.boxProjection = true; reflection.intensity = .8f;
        }

        static VolumeProfile SaveProfile(VolumeProfile p, string name)
        {
            var path = Gen + "Data/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing)
            {
                foreach (var old in existing.components.ToArray()) Object.DestroyImmediate(old, true);
                existing.components.Clear();
                foreach (var c in p.components) { AssetDatabase.AddObjectToAsset(c, existing); existing.components.Add(c); }
                p.components.Clear(); Object.DestroyImmediate(p);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(p, path);
            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, p);
            return p;
        }

        static void Beam(string name, Vector3 from, Vector3 to, float r0, float r1, Material mat)
        {
            float length = Vector3.Distance(from, to);
            var profile = new List<Vector2[]> { Lathe.Line(new Vector2(r0, 0), new Vector2(r1, length), 24) };
            var mesh = SaveMesh(Lathe.Revolve(profile, 0, 360, 40), name.Replace(" ", ""));
            var go = Obj(name, env, mesh, mat, from, Quaternion.LookRotation(to - from));
            beams.Add(go.GetComponent<Renderer>());
        }

        static void Dust(Vector3 centre, Vector3 axis, float length, Texture soft)
        {
            var ps = new GameObject("Beam Dust", typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            ps.transform.SetParent(env); ps.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(axis));
            var main = ps.main; main.startLifetime = 9; main.startSize = new ParticleSystem.MinMaxCurve(.006f, .015f); main.startSpeed = .04f;
            main.maxParticles = 180; main.startColor = new Color(.7f, .85f, 1f, .5f); main.prewarm = true; main.loop = true; main.useUnscaledTime = false;
            var em = ps.emission; em.rateOverTime = 20;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 1.6f, length);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .3f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Glow("Dust", new Color(.7f, .85f, 1f, .7f), soft, 1.2f);
            beams.Add(r);
        }

        static Mesh Stars(int count, float radius)
        {
            var rnd = new System.Random(3);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); var col = new List<Color>();
            for (int i = 0; i < count; i++)
            {
                float y = .015f + .985f * (float)rnd.NextDouble(), a = (float)rnd.NextDouble() * Mathf.PI * 2, r = Mathf.Sqrt(1 - y * y);
                var p = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r) * radius;
                var n = -p.normalized; var u = Vector3.Cross(n, Vector3.up).normalized; var w = Vector3.Cross(n, u);
                float s = .012f + .027f * Mathf.Pow((float)rnd.NextDouble(), 3);
                int b = v.Count;
                v.Add(p + (-u - w) * s); v.Add(p + (u - w) * s); v.Add(p + (u + w) * s); v.Add(p + (-u + w) * s);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                var c = Color.Lerp(new Color(.7f, .8f, 1f), new Color(1f, .95f, .85f), (float)rnd.NextDouble()); c.a = .4f + .6f * (float)rnd.NextDouble();
                for (int k = 0; k < 4; k++) col.Add(c);
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static void Flip(Mesh m)
        {
            var n = m.normals; for (int i = 0; i < n.Length; i++) n[i] = -n[i]; m.normals = n;
            var t = m.triangles; for (int i = 0; i < t.Length; i += 3) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]); m.triangles = t;
        }

        static void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.20f, .26f, .36f);
            RenderSettings.ambientEquatorColor = new Color(.11f, .13f, .17f);
            RenderSettings.ambientGroundColor = new Color(.04f, .04f, .05f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = false;
            var hdriPath = Root + "ThirdParty/PolyHaven/HDRI/ferndale_studio_09_1k.hdr";
            var ti = AssetImporter.GetAtPath(hdriPath) as TextureImporter;
            if (ti)
            {
                if (ti.textureShape != TextureImporterShape.TextureCube) { ti.textureShape = TextureImporterShape.TextureCube; ti.SaveAndReimport(); }
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(hdriPath);
                RenderSettings.reflectionIntensity = .55f;
            }
            RenderSettings.skybox = null;
        }
    }
}
