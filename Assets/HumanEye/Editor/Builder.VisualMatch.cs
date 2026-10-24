using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Visual-match pass (8 October 2026): fits the module into the owner's new lab (the "THE HUMAN EYE" environment under
    /// Open Dome, kept as modelled) and gives it the art boards' look. Idempotent; runs last, after Apply reference
    /// cinematics (Tools > Human Eye > Authoring > Apply visual match). The lightmap bake is its own step (Bake lab lighting).
    /// • Layout: the lesson rig (hero/cut/menu points, eye rig, Niko and his anchors, brain display, apple and its pedestal,
    ///   lesson shots, display lights) moves onto the central stand, the hero eye 1.17 m above the stand's 1.78 m top where
    ///   the lab's exhibit eye floats; the scan subject stands on the stand's ring tier, between the gate and the stand.
    ///   The one glass panel in the gate's opening is switched off; mesh colliders let Explore lab walk the new floor.
    /// • The exhibit eye is Stage.displayEye (fully dynamic: the opening hands it over to the learner's eye).
    /// • A daylight sky (Tools/HumanEye/generate_daylight_sky.py) for the windows, ambient and reflections; the art boards'
    ///   galaxy is an interior projection on a shell outside the roof (GalaxyDome.shader, the owner's Poly Haven night sky).
    /// • Light: the sun key (mixed), baked reflection probes (lab, stand, corridor), light probes for everything that
    ///   moves, the scene's own lighting settings; clear ice-blue glass, cyan light strips.
    /// • Palette (UI_STYLE_GUIDE tokens): blue iris, gold signals, coral brain, pearl/navy/cyan Niko, navy glass panels.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.ApplyVisualMatch
    public static partial class Builder
    {
        public static readonly Vector3 HeroCentre = new(0, 2.95f, 0);   // over the stand's centre, inside the exhibit eye's volume
        static readonly Vector3 SubjectFeet = new(-3.1f, .8f, 1.55f);   // ring tier (top .78) on the gate side, clear of the apple's pedestal (-3.06, 0)
        const float StandTop = 1.8f, RingTier = .8f, StandRadius = 2.55f, RingRadius = 4.05f, LabFloor = .11f;
        const string LabRoot = "Open Dome", LightingPath = Root + "Scenes/HumanEye_Observatory_Lighting.lighting";
        // UI_STYLE_GUIDE tokens (visual-match pass)
        static readonly Color CaptionNavy = new(.027f, .22f, .365f), Midnight = new(.035f, .176f, .314f), CyanGlow = new(.149f, .776f, .961f),
            NeuralGold = new(1f, .757f, .369f), BrainCoral = new(.788f, .463f, .475f), Pearl = new(.925f, .969f, 1f);

        [MenuItem("Tools/Human Eye/Authoring/Apply visual match (new lab, keeps scene edits)", priority = 12)]
        public static void ApplyVisualMatch()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            hud = stage.hud; cams = stage.cams; narrator = stage.narrator; runner = stage.runner;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var lab = GameObject.Find(LabRoot);
            if (!lab) { Debug.LogError("HE_VISUAL no '" + LabRoot + "' in the scene: the new lab is missing"); return; }

            FitLab(lab.transform);
            LessonShots();
            Sky(lab.transform);
            Probes();
            Grade();
            Palette();
            EyeLook();
            BuildData();

            foreach (var o in new Object[] { stage, stage.cams, stage.hud, stage.signals, stage.human, stage.niko }) if (o) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_VISUAL_MATCH_APPLIED");
        }

        static T Comp<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c ? c : go.AddComponent<T>(); }
        static Transform Deep(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        // ---------------- the module in the new room ----------------
        static void FitLab(Transform lab)
        {
            var delta = HeroCentre - stage.heroPoint.position;
            if (delta.sqrMagnitude > 1e-6f)
            {
                var moved = new HashSet<Transform>();
                void Move(Transform t) { if (t && moved.Add(t)) t.position += delta; }
                foreach (var t in new[] { stage.heroPoint, stage.cutPoint, stage.menuPoint, stage.eyeRig, stage.niko.transform, stage.appleRoot }) Move(t);
                foreach (var name in new[] { "Cut Frame (eye-radius units)", "Brain Display" }) { var go = GameObject.Find(name); if (go) Move(go.transform); }
                foreach (var a in stage.nikoAnchors) if (a && a.name != "Micro") Move(a);
                var lights = GameObject.Find("HE Display Lights");
                if (lights) foreach (Transform l in lights.transform)
                {
                    var light = l.GetComponent<Light>();   // Unity's fake null: test with the bool operator, never a C# pattern
                    if (light && light.type == LightType.Directional || l.GetComponent<ReflectionProbe>()) continue;
                    Move(l);
                }
                foreach (var shot in cams.shots)
                {
                    if (!shot || shot.name.StartsWith("S02_Intro") || shot.name is "S08_Transduce" or "Follow (TRACE)") continue;
                    if (shot.transform.parent && shot.transform.parent.name != "Shots (cue sheet cameras)") continue;   // eye- and cut-frame shots ride their frames
                    Move(shot.transform);
                }
                var tree = stage.treeRoot; if (tree) tree.position += new Vector3(0, 0, delta.z);
                Debug.Log($"HE_VISUAL lesson rig moved by {delta} ({moved.Count} objects)");
            }
            // Niko stands on the stand's top (or its ring tier), never inside it
            foreach (var a in stage.nikoAnchors.Append(stage.niko.transform))
            {
                if (!a || a.name == "Micro") continue;
                var p = a.position; float r = new Vector2(p.x, p.z).magnitude;
                if (r < StandRadius) p.y = Mathf.Max(p.y, StandTop); else if (r < RingRadius) p.y = Mathf.Max(p.y, RingTier);
                a.position = p;
            }
            // the subject on the ring tier; the apple's pedestal from the tier up to the apple; the far tree on the floor
            stage.human.transform.position = SubjectFeet;
            var bottom = stage.appleRoot.Find("Bottom");
            float appleBottom = bottom ? bottom.position.y : stage.appleRoot.position.y - .13f;
            stage.pedestal.position = new Vector3(stage.appleRoot.position.x, RingTier - .02f, stage.appleRoot.position.z);
            stage.pedestal.localScale = new Vector3(1, 1, (appleBottom - stage.pedestal.position.y) / 1.35f);   // the cylinder's height is its local Z
            if (stage.treeRoot) stage.treeRoot.position = new Vector3(stage.treeRoot.position.x, LabFloor, stage.treeRoot.position.z);
            var accent = GameObject.Find("Platform accent");
            if (accent) accent.transform.position = new Vector3(0, StandTop + .15f, 0);
            stage.roomLights = new Light[0];   // the old room's lights went with the old room; the new lab is baked

            // the exhibit eye: fully dynamic (no baked shadow under it once it has gone, never inside a probe capture)
            var exhibit = Deep(lab, "full eye");
            stage.displayEye = exhibit;
            if (exhibit)
                foreach (var t in exhibit.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    var r = t.GetComponent<Renderer>(); if (r) r.lightProbeUsage = LightProbeUsage.BlendProbes;
                }
            else Debug.LogWarning("HE_VISUAL no exhibit eye ('full eye') under " + LabRoot);

            LabWalkColliders(lab);
            // the small plants take probe light instead of lightmap space (fast bake, same look)
            var plants = Deep(lab, "mini plants");
            if (plants) foreach (var r in plants.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;
        }

        /// The lab's walk colliders and the switched-off glass panel in the gate's opening: added components on the Open Dome
        /// instance, which a save made while another Open Dome FBX was loaded drops (15 October 2026), so the corridor-gate
        /// patch re-applies them. Idempotent.
        internal static void LabWalkColliders(Transform lab)
        {
            var exhibit = Deep(lab, "full eye");
            // the gate's opening: the glass panel directly behind the door would stop the camera and the walker
            var glass = Deep(lab, "side_glass20"); if (glass) glass.gameObject.SetActive(false);

            // walkable lab: the floor, walls, glass, stands and planters collide (leaves and floating panels don't)
            int added = 0;
            foreach (var r in lab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (exhibit && r.transform.IsChildOf(exhibit)) continue;
                var names = r.sharedMaterials.Where(m => m).Select(m => m.name).ToArray();
                bool foliage = names.Any(n => n is "leaf" or "stem" or "soil1"), floating = r.bounds.min.y > 1.2f;
                var mf = r.GetComponent<MeshFilter>();
                if (foliage || floating || !mf || !mf.sharedMesh) continue;
                if (!r.GetComponent<MeshCollider>()) added++;
                Comp<MeshCollider>(r.gameObject).sharedMesh = mf.sharedMesh;
            }
            var corridor = GameObject.Find("walking_path");
            if (corridor) foreach (var r in corridor.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) Comp<MeshCollider>(r.gameObject).sharedMesh = mf.sharedMesh;
            }
            Debug.Log($"HE_VISUAL lab walk colliders: {added} added, glass panel {(glass ? "off" : "not found")}");
        }

        // ---------------- the shot bible (eye-local shots in eye radii: +Z cornea, cut face toward -X) ----------------
        static void LessonShots()
        {
            void Local(string name, Vector3 pos, Vector3 look, float fov)
            {
                var s = cams.shots.FirstOrDefault(x => x.name == name);
                if (!s) { Debug.LogWarning("HE_VISUAL shot missing " + name); return; }
                s.transform.localPosition = pos; s.transform.localRotation = Quaternion.LookRotation(look - pos, Vector3.up);
                var lens = s.Lens; lens.FieldOfView = fov; s.Lens = lens;
                EditorUtility.SetDirty(s); EditorUtility.SetDirty(s.transform);
            }
            float lz = EyeGeometry.LensZ;
            // outer eye (closed, hero pose): sclera a moderately wide 3/4 of the whole globe, cornea in side profile, iris on axis
            Local("S02_Part_sclera", new Vector3(2.6f, 1f, 3f), new Vector3(.15f, .05f, .15f), 34);
            Local("S02_Part_cornea", new Vector3(2.4f, .2f, .9f), new Vector3(0, 0, .82f), 30);
            Local("S02_Part_iris", new Vector3(0, .04f, 3.1f), new Vector3(0, 0, .85f), 26);
            // inner eye: uncluttered side cutaways (front half for the aqueous, the whole section for the vitreous, the
            // layered back wall for the choroid), never a frame filled by a transparent layer or a blurred wall
            Local("S03_Part_aqueous", new Vector3(-2.9f, .3f, .45f), new Vector3(0, 0, .45f), 34);
            Local("S03_Lens_Close", new Vector3(-2.5f, .02f, lz + .02f), new Vector3(0, -.08f, lz), 26);
            Local("S03_Vitreous_Back", new Vector3(-3.2f, .1f, -.05f), new Vector3(0, 0, -.1f), 38);
            Local("S03_Part_vitreous", new Vector3(-3.2f, .1f, -.05f), new Vector3(0, 0, -.1f), 38);
            Local("S03_Choroid_Deep", new Vector3(-1.25f, .62f, -.42f), new Vector3(0, .72f, -.62f), 30);
            Local("S03_Part_choroid", new Vector3(-1.25f, .62f, -.42f), new Vector3(0, .72f, -.62f), 30);
            // light path: the front of the section with the rays crossing cornea, aqueous, pupil and lens
            Local("S05_Cornea", new Vector3(-2.6f, .2f, .95f), new Vector3(0, 0, .6f), 32);
            Local("S05_AqueousPupil", new Vector3(-2.2f, .15f, .8f), new Vector3(0, 0, .62f), 30);
            // accommodation: one fixed side cutaway showing the lens curvature and the retina it focuses on (near / far)
            Local("S06_LensClose", new Vector3(-3.05f, .05f, .1f), new Vector3(0, -.05f, .05f), 38);
            // the closing words: Niko and the whole eye, its cornea turned to the learner; then the eye with the near apple
            // and the distant tree in one frame
            WowPatch.Shot(stage, "S11_Eye", HeroCentre + new Vector3(1.05f, .15f, -3.8f), HeroCentre + new Vector3(1.05f, -.35f, -.5f), 40);   // aimed between the eye and Niko (Eleven anchor)
            WowPatch.Shot(stage, "S11_NearFar", HeroCentre + new Vector3(2f, .25f, -1.9f), HeroCentre + new Vector3(-3.6f, -.35f, -.5f), 44);
        }

        // ---------------- daylight outside, a galaxy on the ceiling ----------------
        static void Sky(Transform lab)
        {
            var skyTex = Root + "Generated/Textures/DaylightSky.hdr";
            if (AssetImporter.GetAtPath(skyTex) is TextureImporter ti && (ti.maxTextureSize != 4096 || ti.mipmapEnabled == false))
            {
                ti.textureShape = TextureImporterShape.Texture2D; ti.maxTextureSize = 4096; ti.mipmapEnabled = true; ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "WebGL", overridden = true, maxTextureSize = 2048, textureCompression = TextureImporterCompression.Compressed });
                ti.SaveAndReimport();
            }
            var sky = new Material(Shader.Find("Skybox/Panoramic")) { name = "Daylight Sky" };
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(skyTex));
            sky.SetFloat("_Mapping", 1); sky.SetFloat("_ImageType", 0); sky.SetFloat("_Exposure", 1); sky.SetFloat("_Rotation", 0);
            sky.SetColor("_Tint", new Color(.5f, .5f, .5f));   // the shader's neutral tint
            sky = Upsert(sky, Gen + "Materials/DaylightSky.mat");
            if (!RenderSettings.skybox) RenderSettings.skybox = sky;   // keep the skybox the owner assigned in Lighting
            RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = 1;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox; RenderSettings.defaultReflectionResolution = 256; RenderSettings.reflectionIntensity = 1;
            RenderSettings.sun = stage.keyLight;
            RenderSettings.fog = false;

            // the planetarium shell: centred under the floor like the roof (a sphere of ~21.5 m around (0,-5.9,0)), 1.5 m outside it
            var galaxyMat = new Material(Shader.Find("HumanEye/GalaxyDome")) { name = "GalaxyDome" };
            galaxyMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/THE HUMAN EYE/qwantani_night_puresky_4k.exr"));
            galaxyMat = Upsert(galaxyMat, Gen + "Materials/GalaxyDome.mat");
            var dome = lab.Find("Galaxy Projection");
            if (!dome)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(tmp.GetComponent<Collider>());
                tmp.name = "Galaxy Projection"; dome = tmp.transform; dome.SetParent(lab, true);
            }
            dome.position = new Vector3(0, -5.9f, 0); dome.rotation = Quaternion.identity;
            dome.localScale = Vector3.one; dome.localScale = new Vector3(46 / dome.lossyScale.x, 46 / dome.lossyScale.y, 46 / dome.lossyScale.z);
            var dr = dome.GetComponent<MeshRenderer>(); dr.sharedMaterial = galaxyMat;
            dr.shadowCastingMode = ShadowCastingMode.Off; dr.receiveShadows = false; dr.lightProbeUsage = LightProbeUsage.Off; dr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(dome.gameObject, 0);

            // the sun: the key light carries direct sun in real time and its bounce in the bake
            var key = stage.keyLight;
            key.lightmapBakeType = LightmapBakeType.Mixed; key.color = new Color(1, .96f, .9f); key.intensity = 1.35f;
            key.shadows = LightShadows.Soft; key.shadowStrength = .8f;
            EditorUtility.SetDirty(key);

            // clear ice-blue glass (the walls show the day outside), cyan light strips (rings, the stand's band, the floor line)
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/THE HUMAN EYE/Material/mirror.mat");
            if (glassMat)
            {
                glassMat.SetColor("_BaseColor", new Color(.8f, .92f, 1f, .12f)); glassMat.SetFloat("_Metallic", .15f); glassMat.SetFloat("_Smoothness", .96f);
                EditorUtility.SetDirty(glassMat);
            }
            foreach (var r in lab.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterials.Contains(glassMat)))
                r.shadowCastingMode = ShadowCastingMode.Off;   // the sun and the area light pass through the glass
            var strip = AssetDatabase.LoadAssetAtPath<Material>("Assets/THE HUMAN EYE/Material/light1.mat");
            if (strip) { strip.SetColor("_EmissionColor", new Color(.55f, 2.6f, 4.2f)); strip.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; EditorUtility.SetDirty(strip); }
            var floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/THE HUMAN EYE/Material/FLoor.mat");
            if (floor) { floor.SetFloat("_Smoothness", .82f); floor.SetFloat("_Metallic", .1f); EditorUtility.SetDirty(floor); }   // a polished floor that mirrors the lab softly
        }

        // ---------------- probes and the scene's lighting settings ----------------
        static void Probes()
        {
            var group = GameObject.Find("HE Display Lights").transform;
            ReflectionProbe Probe(string name, Vector3 centre, Vector3 size, int importance, float blend)
            {
                var t = group.Find(name); if (!t) { t = new GameObject(name).transform; t.SetParent(group, false); }
                t.position = centre;
                var p = Comp<ReflectionProbe>(t.gameObject);
                p.mode = ReflectionProbeMode.Baked; p.boxProjection = true; p.size = size; p.center = Vector3.zero;
                p.importance = importance; p.blendDistance = blend; p.resolution = 256; p.hdr = true; p.intensity = 1;
                p.clearFlags = ReflectionProbeClearFlags.Skybox; p.nearClipPlane = .1f; p.farClipPlane = 80;
                return p;
            }
            var old = group.Find("Observatory Reflection"); if (old) old.name = "Lab Reflection";
            Probe("Lab Reflection", new Vector3(0, 5.2f, 0), new Vector3(31.5f, 10.4f, 31.5f), 1, 2);
            Probe("Stand Reflection", new Vector3(0, 2.6f, 0), new Vector3(8.6f, 5f, 8.6f), 2, 1);
            Probe("Corridor Reflection", new Vector3(-23, 3.6f, .6f), new Vector3(13, 7.2f, 6.2f), 1, 1);

            var pts = new List<Vector3>();
            void Ring(float r, float y, int n) { if (r < .01f) { pts.Add(new Vector3(0, y, 0)); return; } for (int i = 0; i < n; i++) { float a = i * 2 * Mathf.PI / n; pts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); } }
            foreach (var y in new[] { 1.95f, 2.6f, 3.4f, 4.4f }) { Ring(0, y, 1); Ring(1.2f, y, 8); Ring(2.4f, y, 8); }   // over the stand: eye, Niko, brain, exhibit
            foreach (var y in new[] { 1.1f, 2.2f, 3.4f }) Ring(3.3f, y, 12);                                             // over the ring tier: the subject, the apple
            foreach (var y in new[] { .45f, 2.2f, 4.6f }) Ring(5.4f, y, 12);
            foreach (var y in new[] { 1.7f, 4.6f, 7.6f }) Ring(9f, y, 14);
            foreach (var y in new[] { 2.2f, 5.6f }) Ring(12.8f, y, 18);
            foreach (var o in new[] { Vector3.zero, new Vector3(-.6f, 0, 0), new Vector3(.4f, 0, 0), new Vector3(0, 0, .6f), new Vector3(0, 0, -.6f) })
                foreach (var h in new[] { .4f, 1.2f, 1.9f }) pts.Add(SubjectFeet + o + Vector3.up * h);                  // around the subject's head and body
            for (float x = -29; x <= -16; x += 2.6f) foreach (var y in new[] { 1, 3.2f, 5.6f }) foreach (var z in new[] { -.6f, .67f, 1.9f }) pts.Add(new Vector3(x, y, z));   // the corridor
            var probesT = group.Find("Lab Light Probes"); if (!probesT) { probesT = new GameObject("Lab Light Probes").transform; probesT.SetParent(group, false); }
            probesT.position = Vector3.zero; probesT.rotation = Quaternion.identity; probesT.localScale = Vector3.one;
            Comp<LightProbeGroup>(probesT.gameObject).probePositions = pts.Select(p => probesT.InverseTransformPoint(p)).ToArray();
            Debug.Log($"HE_VISUAL light probes {pts.Count}");

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (!settings) { settings = new LightingSettings { name = "HumanEye_Observatory_Lighting" }; AssetDatabase.CreateAsset(settings, LightingPath); }
            settings.bakedGI = true; settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 12; settings.lightmapPadding = 4; settings.lightmapMaxSize = 2048;
            settings.lightmapCompression = LightmapCompression.HighQuality;
            settings.ao = true; settings.aoMaxDistance = 1; settings.aoExponentIndirect = 1; settings.aoExponentDirect = 0;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly; settings.directionalityMode = LightmapsMode.NonDirectional;
            settings.directSampleCount = 32; settings.indirectSampleCount = 256; settings.environmentSampleCount = 256; settings.maxBounces = 2; settings.minBounces = 1;
            settings.filteringMode = LightingSettings.FilterMode.Auto;
            settings.denoiserTypeDirect = settings.denoiserTypeIndirect = settings.denoiserTypeAO = LightingSettings.DenoiserType.OpenImage;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;
        }

        // ---------------- the lab grade (the bake made the room bright: less exposure, the boards' vivid blues) ----------------
        static void Grade()
        {
            var profile = stage.post ? stage.post.sharedProfile : null;
            if (!profile) { Debug.LogWarning("HE_VISUAL no lab post profile"); return; }
            if (profile.TryGet(out ColorAdjustments ca)) { ca.postExposure.Override(-.05f); ca.contrast.Override(14); ca.saturation.Override(16); }
            if (profile.TryGet(out WhiteBalance wb)) wb.temperature.Override(-8);
            if (profile.TryGet(out Bloom bloom)) { bloom.threshold.Override(.95f); bloom.intensity.Override(.3f); bloom.scatter.Override(.65f); }
            if (profile.TryGet(out Vignette v)) v.intensity.Override(.16f);
            EditorUtility.SetDirty(profile);
        }

        // ---------------- the art boards' palette ----------------
        static void Palette()
        {
            Material M(string name) => AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/" + name + ".mat");
            void Set(Material m, string prop, Color c) { if (m && m.HasProperty(prop)) { m.SetColor(prop, c); EditorUtility.SetDirty(m); } }
            void F(Material m, string prop, float v) { if (m && m.HasProperty(prop)) { m.SetFloat(prop, v); EditorUtility.SetDirty(m); } }
            // signals gold, light paler: an electrical signal is never a light beam
            Set(M("Signal"), "_Color", new Color(.55f, .9f, 1f)); F(M("Signal"), "_Intensity", 5);   // electric-blue pulses (quality brief, 14 October 2026)
            Set(M("LightRay"), "_Color", new Color(1, .95f, .82f)); Set(M("LightRayDashed"), "_Color", new Color(1, .95f, .82f));
            if (stage.signals.headLight) { stage.signals.headLight.color = new Color(.6f, .9f, 1f); EditorUtility.SetDirty(stage.signals.headLight); }
            // the brain: soft pink-coral tissue, a warm rim; blue only on the visual cortex
            foreach (var b in new[] { M("Brain_User"), M("Brain_VisualArea_User") })
            {
                Set(b, "_BaseColor", new Color(1, .8f, .84f)); F(b, "_Gloss", .35f); F(b, "_Specular", .15f); F(b, "_Wet", .25f);
                F(b, "_Rim", .16f); Set(b, "_RimColor", new Color(1, .86f, .84f));
            }
            Set(M("ZA_V1"), "_BaseColor", BrainCoral);
            // optic nerves: a golden fibre bundle (REF_04/05), never a white cable; the signal comets run on top in gold
            foreach (var n in new[] { M("Eye_OpticNerve"), M("ZA_Nerve") })
            {
                Set(n, "_BaseColor", new Color(.95f, .71f, .42f)); Set(n, "_Emission", new Color(.09f, .045f, .01f, 0));
                F(n, "_Rim", .32f); Set(n, "_RimColor", NeuralGold); F(n, "_Flow", 1); F(n, "_Gloss", .45f);
            }
            // Niko: glossy pearl shell, deep navy face screen, electric-cyan eyes and details
            Set(M("Niko_White"), "_BaseColor", Pearl); Set(M("Niko_White"), "_Color", Pearl); F(M("Niko_White"), "_Smoothness", .86f);
            var screen = new Color(.03f, .07f, .15f);
            Set(M("Niko_Visor"), "_BaseColor", screen); Set(M("Niko_Visor"), "_Color", screen);
            foreach (var g in new[] { "Niko_Glow", "Niko_SnapGlow" }) Set(M(g), "_Color", CyanGlow);
            // photoreceptors as on the boards: purple rods, cones in the three colours they sense (long, medium, short wavelength)
            var coneOuter = M("Cone_Outer");
            if (coneOuter && stage.micro)
            {
                Material Cone(string suffix, Color c)
                {
                    var m = new Material(coneOuter) { name = "Cone_Outer_" + suffix }; m.SetColor("_BaseColor", c);
                    return Upsert(m, Gen + "Materials/Cone_Outer_" + suffix + ".mat");
                }
                var kinds = new[] { Cone("L", new Color(.95f, .27f, .22f)), Cone("M", new Color(.3f, .82f, .38f)), Cone("S", new Color(.28f, .47f, 1f)) };
                int k = 0;
                foreach (var r in stage.micro.GetComponentsInChildren<Renderer>(true).Where(r => r.name == "Outer segment" && r.transform.parent && r.transform.parent.name == "Cone"))
                { r.sharedMaterial = kinds[k++ % 3]; EditorUtility.SetDirty(r); }
            }
            // navy glass panels with the style guide's cyan rim; the old Scan Eye button is gone from the panel
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                foreach (var g in prefab.GetComponentsInChildren<GlassPanel>(true))
                {
                    if (g.name is "start-begin" or "btn-ask") continue;   // the one required action keeps its own fill
                    bool control = g.GetComponent<UnityEngine.UI.Selectable>();
                    g.color = control ? new Color(Midnight.r, Midnight.g, Midnight.b, .95f) : new Color(CaptionNavy.r, CaptionNavy.g, CaptionNavy.b, .92f);   // near-opaque: the daylit lab is bright behind them
                    if (g.material && g.material.HasProperty("_RimColor")) { g.material.SetColor("_RimColor", new Color(.36f, .84f, 1f)); EditorUtility.SetDirty(g.material); }
                }
                var scan = prefab.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "ab-SCAN");
                if (scan) scan.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (var g in hud.canvas.GetComponentsInChildren<GlassPanel>(true)) PrefabUtility.RevertObjectOverride(g, InteractionMode.AutomatedAction);
            var sceneScan = hud.canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "ab-SCAN");
            if (sceneScan) { PrefabUtility.RevertObjectOverride(sceneScan.gameObject, InteractionMode.AutomatedAction); sceneScan.gameObject.SetActive(false); }
        }

        // ---------------- the bake (separate: minutes on the GPU) ----------------
        [MenuItem("Tools/Human Eye/Authoring/Bake lab lighting (lightmaps, probes)", priority = 13)]
        public static void BakeLab()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (settings) Lightmapping.lightingSettings = settings;
            var t0 = System.DateTime.Now;
            bool ok = Lightmapping.Bake();   // synchronous; lightmaps, light probes and the baked reflection probes
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_LAB_BAKED ok={ok} lightmaps={LightmapSettings.lightmaps.Length} probes={(LightmapSettings.lightProbes ? LightmapSettings.lightProbes.count : 0)} in {(System.DateTime.Now - t0).TotalSeconds:0}s");
        }
    }
}
