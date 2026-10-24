using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static class LandingReferencePass
    {
        public const string Folder = "Assets/HumanEye/Generated/LandingReference/";
        const string Materials = "Assets/HumanEye/Generated/Materials/";

        [MenuItem("Tools/Human Eye/Authoring/Apply reference eye finish (keeps scene)")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>();
            if (!stage || !stage.eye || !stage.hud) throw new InvalidOperationException("Saved eye scene is incomplete.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            EyeMaterials();
            foreach (var eye in new[] { stage.eye, stage.otherEye })
            {
                eye.Get("cornea").baseAlpha = .10f;
                eye.Get("lens").baseAlpha = .14f;
                foreach (var part in eye.parts)
                {
                    foreach (var renderer in part.main.Concat(part.removable).Concat(part.exterior))
                        if (renderer) renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                    EditorUtility.SetDirty(part);
                }
            }
            stage.eyeKey.intensity = 5.5f; stage.eyeRim.intensity = 4; stage.eyeFill.intensity = 1.5f;
            stage.eyeKey.color = new Color(1, .96f, .9f); stage.eyeRim.color = new Color(.3f, .78f, 1);
            stage.keyLight.intensity = .55f;
            if (stage.interiorLight) { stage.interiorLight.color = new Color(1, .8f, .57f); EditorUtility.SetDirty(stage.interiorLight); }
            stage.roomBoost = 1.15f;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.105f, .12f, .15f);
            var lab = stage.post.sharedProfile;
            if (lab.TryGet(out Bloom bloom)) { bloom.intensity.Override(.14f); bloom.threshold.Override(1.2f); }
            if (lab.TryGet(out ColorAdjustments grade)) { grade.postExposure.Override(.1f); grade.contrast.Override(14); grade.saturation.Override(5); }
            if (lab.TryGet(out Tonemapping tone)) tone.mode.Override(TonemappingMode.ACES);
            if (lab.TryGet(out Vignette vignette)) vignette.intensity.Override(.13f);
            EditorUtility.SetDirty(lab);
            EnvironmentFinish();
            StudioReflection(stage);
            ExhibitAccent(stage);
            LandingReferenceUI.ApplyTo(stage.hud);
            var card = stage.hud.Element("start-screen").Find("Card");
            if (!card.GetComponent<CanvasGroup>()) card.gameObject.AddComponent<CanvasGroup>();
            if (!card.GetComponent<MenuPanelEntrance>()) card.gameObject.AddComponent<MenuPanelEntrance>();
            var begin = stage.hud.Element("start-begin");
            if (!begin.GetComponent<MenuButtonMotion>()) begin.gameObject.AddComponent<MenuButtonMotion>();
            foreach (var o in new Object[] { stage, stage.eyeKey, stage.eyeRim, stage.eyeFill, stage.keyLight, stage.hud }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/landing-reference-20261024");
            File.WriteAllText("Logs/landing-reference-20261024/apply.txt", "Physical dielectric eye finish; original geometry/UVs retained.\nStatic 256px HDR studio cubemap and custom probe; no realtime reflection updates.\nLive landing title, eyebrow and tagline; original Figma control sprites retained.\nScene: " + Builder.ScenePath);
            Debug.Log("LANDING_REFERENCE_APPLIED");
        }

        static Material Eye(string name, Color tint, float smoothness, float normal, float coat, float ior = 1.4f)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat");
            if (!m || !m.HasProperty("_PhysicalEye")) throw new InvalidOperationException("Physical eye material is missing: " + name);
            m.SetFloat("_PhysicalEye", 1); m.SetFloat("_IOR", ior); m.SetFloat("_Gloss", smoothness);
            m.SetFloat("_Specular", 1); m.SetFloat("_BumpScale", normal); m.SetFloat("_Wet", coat);
            m.SetFloat("_CoatSmoothness", .96f); m.SetFloat("_Rim", 0); m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m); return m;
        }

        static void EyeMaterials()
        {
            Eye("LocalEye_ScleraOuter", new Color(.94f, .89f, .84f), .94f, .08f, .35f);
            Eye("LocalEye_ScleraInner", new Color(1, .94f, .91f), .62f, .16f, .18f);
            Eye("LocalEye_Iris", Color.white, .42f, .18f, .08f);
            var retina = Eye("LocalEye_Retina", new Color(1, .88f, .68f), .57f, .18f, .2f);
            var membrane = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Retina_Membrane_BaseColor.png");
            if (membrane) retina.SetTexture("_BaseMap", membrane);
            retina.SetFloat("_Wrap", .65f); retina.SetFloat("_Specular", .5f); retina.SetFloat("_Wet", .12f);
            Eye("LocalEye_Choroid", new Color(.86f, .62f, .58f), .52f, .3f, .16f);
            Eye("LocalEye_Muscle", new Color(1, .93f, .91f), .58f, .4f, .24f);
            Eye("LocalEye_ArteryRed", new Color(.65f, .065f, .04f), .66f, 0, .22f);
            Eye("LocalEye_VeinBlue", new Color(.08f, .16f, .4f), .66f, 0, .22f);
            foreach (var name in new[] { "Eye_Lens", "LocalEye_Lens", "Eye_Cornea" })
            {
                var m = Eye(name, new Color(.92f, .97f, 1), name == "Eye_Cornea" ? .985f : .975f, 0, 0, name == "Eye_Cornea" ? 1.376f : 1.406f);
                m.SetFloat("_Glass", 1); m.SetFloat("_Wrap", .1f);
            }
            foreach (var name in new[] { "Eye_Aqueous", "Eye_Vitreous" })
            {
                var m = Eye(name, new Color(.92f, .97f, 1), .97f, 0, 0, 1.336f);
                m.SetFloat("_Glass", 1); m.SetFloat("_Specular", .18f);
            }
            foreach (var name in new[] { "Eye_Sclera", "Eye_Iris", "Eye_Retina", "Eye_Choroid", "Eye_OpticNerve" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat");
                if (!m) continue;
                m.SetFloat("_PhysicalEye", 1); m.SetFloat("_IOR", 1.4f); m.SetFloat("_Specular", .8f);
                m.SetFloat("_Wet", .18f); m.SetFloat("_CoatSmoothness", .96f); EditorUtility.SetDirty(m);
            }
        }

        static void EnvironmentFinish()
        {
            foreach (var entry in new[] { ("Floor", .52f, .84f), ("BrushedMetal", .8f, .7f), ("Brass", .8f, .78f) })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Materials + entry.Item1 + ".mat");
                if (!material) continue;
                material.SetFloat("_Metallic", entry.Item2); material.SetFloat("_Smoothness", entry.Item3);
                material.SetFloat("_EnvironmentReflections", 1); material.SetFloat("_GlossyReflections", 1); EditorUtility.SetDirty(material);
            }
            var floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/HumanEye/Generated/Cinematic/Cinematic_MT3.mat");
            if (floor) { floor.SetFloat("_Metallic", .42f); floor.SetFloat("_Smoothness", .72f); EditorUtility.SetDirty(floor); }
        }

        static void ExhibitAccent(Stage stage)
        {
            var source = stage.observatory.transform.Find("Landing exhibit accent");
            if (!source) { source = new GameObject("Landing exhibit accent").transform; source.SetParent(stage.observatory.transform, false); }
            var light = source.GetComponent<Light>(); if (!light) light = source.gameObject.AddComponent<Light>();
            light.type = LightType.Spot; light.color = new Color(1, .77f, .43f); light.intensity = 3.5f;
            light.range = 8; light.spotAngle = 65; light.innerSpotAngle = 40; light.shadows = LightShadows.None;
            var target = stage.displayEye ? stage.displayEye.position : stage.heroPoint.position;
            source.position = target + new Vector3(0, 2, -1.3f); source.rotation = Quaternion.LookRotation(target - source.position);
            light.enabled = false; stage.landingAccent = light;
        }

        // Fixed image-based lighting from three soft rectangular studio sources; sampled through the real surface normals.
        static void StudioReflection(Stage stage)
        {
            string path = Folder + "StudioReflection.cubemap";
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (!cube) { cube = new Cubemap(256, TextureFormat.RGBAHalf, true) { name = "Studio Reflection" }; AssetDatabase.CreateAsset(cube, path); }
            var directions = new[] { stage.heroPoint.forward * 3 + stage.heroPoint.right * 2 + Vector3.up * 2.5f,
                stage.heroPoint.forward * 3 - stage.heroPoint.right * 2 + Vector3.up * 1.2f, -stage.heroPoint.forward + Vector3.up + stage.heroPoint.right };
            for (int face = 0; face < 6; face++)
            {
                var pixels = new Color[256 * 256];
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float u = 2 * (x + .5f) / 256 - 1, v = 2 * (y + .5f) / 256 - 1;
                    var d = FaceDirection(face, u, v).normalized;
                    Color c = new Color(.045f, .065f, .105f);
                    for (int k = 0; k < directions.Length; k++)
                    {
                        var forward = directions[k].normalized; var right = Vector3.Cross(Vector3.up, forward).normalized; var up = Vector3.Cross(forward, right);
                        float z = Vector3.Dot(d, forward); if (z <= 0) continue;
                        float sx = Mathf.Abs(Vector3.Dot(d, right) / z), sy = Mathf.Abs(Vector3.Dot(d, up) / z);
                        float edge = Mathf.Max(sx / (k == 0 ? .19f : .07f), sy / (k == 0 ? .28f : .22f));
                        float mask = 1 - Mathf.SmoothStep(.94f, 1, edge);
                        c += (k == 2 ? new Color(.12f, .7f, 1) * 3 : new Color(1, .97f, .91f) * (k == 0 ? 8 : 4)) * mask;
                    }
                    pixels[y * 256 + x] = c;
                }
                cube.SetPixels(pixels, (CubemapFace)face);
            }
            cube.Apply(true, false); cube.filterMode = FilterMode.Trilinear; EditorUtility.SetDirty(cube);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            var go = GameObject.Find("Landing Studio Reflection"); if (!go) go = new GameObject("Landing Studio Reflection");
            var probe = go.GetComponent<ReflectionProbe>(); if (!probe) probe = go.AddComponent<ReflectionProbe>();
            go.transform.position = stage.heroPoint.position;
            probe.mode = ReflectionProbeMode.Custom; probe.customBakedTexture = cube; probe.importance = 10;
            probe.size = new Vector3(80, 40, 80); probe.blendDistance = 4; probe.boxProjection = false;
            probe.intensity = 1; probe.resolution = 256; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            EditorUtility.SetDirty(probe);
        }

        static Vector3 FaceDirection(int face, float u, float v) => face switch
        {
            0 => new Vector3(1, -v, -u), 1 => new Vector3(-1, -v, u),
            2 => new Vector3(u, 1, v), 3 => new Vector3(u, -1, -v),
            4 => new Vector3(u, -v, 1), _ => new Vector3(-u, -v, -1)
        };
    }
}
