using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Reference pass (8 October 2026): the scene objects the Human Brain reference choreography needs. Idempotent and
    /// save-guarded; runs after the cinematic pass (Tools > Human Eye > Authoring > Apply reference cinematics).
    /// • Z-Anatomy vision structures (Tools/HumanEye/export_zanatomy_vision.py, CC BY-SA 4.0) fitted to the scan subject by
    ///   its eyes: real facial muscles replace the illustrative shell, the orbicularis oculi is its own part, and a "Visual
    ///   System" (extra-ocular muscles, optic nerves, chiasm, tracts, LGN, radiations, V1 and the head's brain) that can float
    ///   out of the head; the four signal routes vp_LL / vp_LR / vp_RL / vp_RR (each eye to each hemisphere) in its space.
    /// • The gate lifts as one panel (the room asset's own Door01Up motion, as in the reference).
    /// • Particles: gate sparks and dust, extraction motes, the title burst, Niko's vortex; the lab dust retuned.
    /// • The cinematic title (intro-title, intro-rule, intro-subtitle) in the letterbox overlay.
    /// • Sound keys made by Tools/HumanEye/generate_reference_sfx.py. Content data is rebuilt at the end.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.ApplyReference
    public static partial class Builder
    {
        const string ZAV = Root + "ThirdParty/ZAnatomy/ZAnatomy_Vision";
        [Serializable] class RouteFile { public Route[] routes; }
        [Serializable] class Route { public string id; public float[] points; }

        [MenuItem("Tools/Human Eye/Authoring/Apply reference cinematics (keeps scene edits)", priority = 11)]
        public static void ApplyReference()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            hud = stage.hud; cams = stage.cams; narrator = stage.narrator; runner = stage.runner;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");

            VisionAnatomy(stage.human);
            EyeLook();
            GateLift(stage.entrance);
            ReferenceParticles(stage.observatory.transform);
            TitleCanvas(stage.hud);
            ReferenceSfx();
            WowPatch.Shot(stage, "S02_IntroDolly", Stage.CamStart, Stage.GateLook, Stage.DollyFov);
            BuildData();

            foreach (var o in new Object[] { stage, stage.cams, stage.hud, stage.sfx, stage.signals, stage.human, stage.entrance }) if (o) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_REFERENCE_APPLIED");
        }

        // ---------------- Z-Anatomy vision structures on the scan subject ----------------
        static void VisionAnatomy(HumanDisplay hd)
        {
            var human = hd.transform;
            var brainR = hd.brain.main[0];
            // a re-run starts clean: the head's brain goes back to the subject, then the earlier assembly and layers go
            var oldSystem = human.Find("Visual System");
            if (oldSystem) { brainR.transform.SetParent(human, true); Object.DestroyImmediate(oldSystem.gameObject); }
            var oldFace = hd.muscles.transform.Find("Face muscles"); if (oldFace) Object.DestroyImmediate(oldFace.gameObject);
            var oldOculi = human.Find("orbicularis"); if (oldOculi) Object.DestroyImmediate(oldOculi.gameObject);
            var shell = human.Find("Muscles"); if (shell) shell.gameObject.SetActive(false);   // the illustrative body shell, kept for provenance

            var temp = new GameObject("temp-vision").transform;
            var za = FitZAnatomy(ZAV + ".fbx", temp, hd.eyeLeft.position, hd.eyeRight.position, hd.eyeRight.forward);
            Renderer R(string n) => za.GetComponentsInChildren<Renderer>(true).First(r => r.name == n);

            void Common(Material m, float gloss, float spec, float rim, Color? rimColour = null)
            {
                m.SetFloat("_RevealUV", 0); m.SetFloat("_Cull", 0); m.SetFloat("_F0", .03f);   // box-projected uvs: never the uv.y reveal clip
                m.SetFloat("_Gloss", gloss); m.SetFloat("_Specular", spec); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", rim);
                if (rimColour.HasValue) m.SetColor("_RimColor", rimColour.Value);
            }
            var muscle = Anatomy("ZA_Muscle", new Color(.56f, .15f, .13f), m =>
            {
                m.SetTexture("_BaseMap", null); m.SetTexture("_BumpMap", null); m.SetFloat("_BumpScale", 0);   // smooth like the reference: any tiled map reads as a grid on the box-projected uvs
                Common(m, .55f, .3f, .05f); m.SetFloat("_Wet", .35f);
            });
            var tendon = Anatomy("ZA_Tendon", new Color(.93f, .9f, .84f), m => { m.SetTexture("_BumpMap", null); m.SetFloat("_BumpScale", 0); Common(m, .55f, .3f, .05f); m.SetFloat("_Wet", .2f); });
            var nerve = Anatomy("ZA_Nerve", new Color(.93f, .85f, .72f), m => Common(m, .4f, .18f, .15f, new Color(1, .88f, .72f)));
            var lgn = Anatomy("ZA_LGN", new Color(.96f, .78f, .55f), m => Common(m, .4f, .18f, .18f, new Color(1, .88f, .72f)));
            var radiation = Anatomy("ZA_Radiation", new Color(.97f, .92f, .82f), m => Common(m, .45f, .2f, .25f, new Color(1, .92f, .8f)));
            var cortex = Anatomy("ZA_V1", new Color(.88f, .62f, .64f), m => { Common(m, .25f, .12f, .22f); m.SetColor("_HighlightColor", new Color(.3f, .62f, 1f)); m.SetFloat("_HighlightTint", .65f); });

            Renderer Layer(string name, Transform parent, string source, params Material[] mats)
            {
                var go = Obj(name, parent, Bake(R(source), parent, source), mats[0]);
                var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterials = mats; mr.shadowCastingMode = ShadowCastingMode.On;
                return mr;
            }
            AnatomyPart Part(Transform parent, string id, string label, string source, params Material[] mats)
            {
                var p = NewPart(parent, id, label, 0, false, 1, 2);
                p.main = new[] { Layer(label, p.transform, source, mats) };
                var c = p.transform.InverseTransformPoint(p.main[0].bounds.center);
                p.focus = Anchor(p.transform, "Focus", c); p.labelAnchor = Anchor(p.transform, "Label", c + p.transform.InverseTransformVector(Vector3.up * .06f));
                p.SetAlpha(0, true);
                EditorUtility.SetDirty(p);
                return p;
            }

            hd.neckY = NeckSeam(hd);
            hd.muscles.label = "Facial muscles";
            hd.muscles.main = new[] { Layer("Face muscles", hd.muscles.transform, "ZA_FaceMuscles", muscle, tendon) };
            var oculiMuscle = Anatomy("ZA_Muscle_Oculi", new Color(.56f, .15f, .13f), m =>
            {
                m.CopyPropertiesFromMaterial(muscle); m.SetColor("_HighlightColor", new Color(1f, .78f, .5f)); m.SetFloat("_HighlightTint", .35f);   // a warm emphasis, not the cyan selection rim
            });
            hd.orbicularis = Part(human, "orbicularis", "Orbicularis oculi", "ZA_Orbicularis", oculiMuscle, tendon);

            var vs = Obj("Visual System", human).transform;   // local identity: aligned with the head at rest
            brainR.transform.SetParent(vs, true);
            hd.visualSystem = vs;
            hd.systemSocketL = Obj("Socket L", vs, pos: hd.eyeLeft.localPosition, rot: hd.eyeLeft.localRotation, scale: hd.eyeLeft.localScale).transform;
            hd.systemSocketR = Obj("Socket R", vs, pos: hd.eyeRight.localPosition, rot: hd.eyeRight.localRotation, scale: hd.eyeRight.localScale).transform;
            hd.eyeMuscles = Part(vs, "eye_muscles", "Extraocular muscles", "ZA_EyeMuscles", muscle, tendon);
            hd.pathway = new[]
            {
                Part(vs, "vp_nerve", "Optic nerve", "ZA_OpticNerve", nerve), Part(vs, "vp_chiasm", "Optic chiasm", "ZA_Chiasm", nerve),
                Part(vs, "vp_tract", "Optic tract", "ZA_OpticTract", nerve), Part(vs, "vp_lgn", "Lateral geniculate nucleus", "ZA_LGN", lgn),
                Part(vs, "vp_radiation", "Optic radiation", "ZA_Radiation", radiation), Part(vs, "vp_cortex", "Primary visual cortex", "ZA_V1", cortex),
            };

            // signal routes: the export's eye-marker frame rebuilt from the subject's sockets (no axis or unit conversion)
            var space = Obj("Pathway space", vs, scale: Vector3.one * .1f).transform;
            hd.pathwaySpace = space;
            var file = JsonUtility.FromJson<RouteFile>(File.ReadAllText(ZAV + ".json"));
            Vector3 o = (hd.eyeLeft.position + hd.eyeRight.position) * .5f; float unit = Vector3.Distance(hd.eyeLeft.position, hd.eyeRight.position);
            var fwd = hd.eyeRight.forward; var left = hd.eyeLeft.position - hd.eyeRight.position; left = (left - fwd * Vector3.Dot(left, fwd)).normalized;
            var up = Vector3.Cross(left, fwd).normalized; if (up.y < 0) up = -up;
            var paths = stage.signals.paths.Where(p => !p.id.StartsWith("vp_")).ToList();
            foreach (var r in file.routes)
            {
                var pts = new Vector3[r.points.Length / 3];
                for (int i = 0; i < pts.Length; i++) pts[i] = space.InverseTransformPoint(o + (left * r.points[3 * i] + fwd * r.points[3 * i + 1] + up * r.points[3 * i + 2]) * unit);
                paths.Add(new SignalPulses.Path { id = r.id, space = space, points = pts });
            }
            stage.signals.paths = paths.ToArray();
            Object.DestroyImmediate(temp.gameObject);
            EditorUtility.SetDirty(hd); EditorUtility.SetDirty(stage.signals);
            Debug.Log($"HE_VISION fitted: face {hd.muscles.main[0].bounds.size:F3} eye muscles {hd.eyeMuscles.main[0].bounds.size:F3} routes {file.routes.Length} " +
                      $"V1 {hd.pathway[5].main[0].bounds.center:F3} chiasm {hd.pathway[1].main[0].bounds.center:F3}");
        }

        /// The lowest point of the skin mesh's head material (the Rocketbox head/body seam sits at the collar), as a height
        /// above the subject's root: the layer wipes stop there so the shirt is never cut.
        static float NeckSeam(HumanDisplay hd)
        {
            var r = hd.skin.main[0]; var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh;
            var mats = r.sharedMaterials; float min = float.MaxValue; var v = mesh.vertices;
            for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
            {
                if (!mats[s] || !mats[s].name.Contains("head")) continue;
                foreach (int i in mesh.GetTriangles(s)) min = Mathf.Min(min, r.transform.TransformPoint(v[i]).y);
            }
            if (min == float.MaxValue) { Debug.LogWarning("HE_VISION no head sub-mesh on the skin: collar line from the eyes"); return hd.eyeRight.position.y - .25f - hd.transform.position.y; }
            Debug.Log($"HE_VISION collar seam at {min:F3} (eyes {hd.eyeRight.position.y:F3})");
            return min - hd.transform.position.y;
        }

        /// The art boards' eye: a bright blue iris with a thin amber ring at the pupil (Tools/HumanEye/blue_iris.py) and
        /// stronger fine vessels in the sclera (whiten_sclera.py, regenerated from the blue texture); the iris takes the blue map.
        internal static void EyeLook()
        {
            var iris = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/LocalEye_Iris.mat");
            if (iris) { iris.SetTexture("_BaseMap", Tex(LocalEye + "Eye_Diff_Blue.jpg")); EditorUtility.SetDirty(iris); }
            else Debug.LogWarning("HE_VISION LocalEye_Iris.mat missing (run the production pass first)");
        }

        /// A renderer's mesh baked into `space` with its sub-meshes kept (muscle and tendon slots), saved as an asset.
        static Mesh Bake(Renderer r, Transform space, string name)
        {
            var src = r.GetComponent<MeshFilter>().sharedMesh;
            var xf = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(Enumerable.Range(0, src.subMeshCount).Select(i => new CombineInstance { mesh = src, subMeshIndex = i, transform = xf }).ToArray(), false, true);
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        // ---------------- the gate lifts as one panel ----------------
        static void GateLift(LabEntrance entrance)
        {
            if (!entrance || entrance.leaves == null || entrance.leaves.Length < 2) return;
            var lift = Vector3.up * 6.2f;   // the 5.8 m panel clears the opening and rises behind the wall over the frame
            for (int i = 0; i < 2; i++) entrance.open[i] = entrance.closed[i] + lift;
            entrance.SetGate(0);
            EditorUtility.SetDirty(entrance);
        }

        // ---------------- particles ----------------
        static void ReferenceParticles(Transform group)
        {
            foreach (var n in new[] { "Intro ray 1", "Intro ray 2", "Intro ray 3", "Intro ray 4", "Entry particles" }) { var t = group.Find(n); if (t) Object.DestroyImmediate(t.gameObject); }
            var soft = ProceduralTextures.Soft();
            Color gold = new(1f, .84f, .55f, .8f);

            var sparks = IntroParticles(group, "Gate sparks", "RefSpark", soft, new Color(1f, .78f, .45f, 1), 3);
            Setup(sparks, life: (.4f, .9f), speed: (.4f, 1.4f), size: (.012f, .03f), max: 120, rate: 0, gravity: 1.2f);
            Shape(sparks, ParticleSystemShapeType.Box, scale: new Vector3(.1f, .05f, 4.4f), random: 1);
            Velocity(sparks, new Vector3(-2.5f, 1.2f, 0), ParticleSystemSimulationSpace.World);
            stage.gateSparks = sparks;

            var dust = IntroParticles(group, "Gate dust", "RefDust", soft, gold, 2.2f);
            Setup(dust, life: (2.2f, 3.2f), speed: (.3f, 1.2f), size: (.012f, .035f), max: 500, rate: 170, gravity: 0);
            Shape(dust, ParticleSystemShapeType.Box, scale: new Vector3(.8f, 4.8f, 4.2f), random: 1);
            Velocity(dust, new Vector3(-3.6f, .15f, 0), ParticleSystemSimulationSpace.World);   // out through the doorway and past the camera
            var noise = dust.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .45f; noise.scrollSpeed = .2f;
            stage.gateDust = dust;

            var motes = IntroParticles(group, "Extraction motes", "RefMotes", soft, new Color(1f, .9f, .7f, .8f), 2.4f);
            Setup(motes, life: (1.2f, 2f), speed: (.05f, .2f), size: (.006f, .016f), max: 220, rate: 90, gravity: 0);
            Shape(motes, ParticleSystemShapeType.Sphere, radius: .35f, random: 0);
            var orbit = motes.velocityOverLifetime; orbit.enabled = true; orbit.space = ParticleSystemSimulationSpace.Local;
            orbit.orbitalY = new ParticleSystem.MinMaxCurve(1.2f); orbit.x = orbit.y = orbit.z = new ParticleSystem.MinMaxCurve(0);
            stage.extractMotes = motes;

            var burst = IntroParticles(group, "Title burst", "RefBurst", soft, gold, 3);
            Setup(burst, life: (.9f, 1.6f), speed: (2.5f, 5.5f), size: (.02f, .05f), max: 220, rate: 0, gravity: .25f);
            Shape(burst, ParticleSystemShapeType.Circle, radius: .15f, random: 0, rotation: new Vector3(90, 0, 0));
            var drag = burst.limitVelocityOverLifetime; drag.enabled = true; drag.limit = new ParticleSystem.MinMaxCurve(20); drag.dampen = .12f;
            stage.titleBurst = burst;

            var vortex = IntroParticles(group, "Niko vortex", "RefVortex", soft, gold, 2.6f);
            Setup(vortex, life: (1.4f, 2f), speed: (0, .05f), size: (.03f, .06f), max: 240, rate: 150, gravity: 0);
            Shape(vortex, ParticleSystemShapeType.Circle, radius: .5f, random: 0, rotation: new Vector3(90, 0, 0));
            var swirl = vortex.velocityOverLifetime; swirl.enabled = true; swirl.space = ParticleSystemSimulationSpace.Local;
            swirl.orbitalY = new ParticleSystem.MinMaxCurve(3.5f); swirl.radial = new ParticleSystem.MinMaxCurve(-.3f);
            swirl.x = new ParticleSystem.MinMaxCurve(0); swirl.y = new ParticleSystem.MinMaxCurve(.9f); swirl.z = new ParticleSystem.MinMaxCurve(0);
            stage.nikoVortex = vortex;

            if (stage.depthDust)   // fine motes around the subject under the platform beam during the lab dolly
            {
                Setup(stage.depthDust, life: (6, 9), speed: (.02f, .05f), size: (.004f, .01f), max: 150, rate: 25, gravity: 0);
                Shape(stage.depthDust, ParticleSystemShapeType.Sphere, radius: 1.4f, random: 0);
            }
            foreach (var ps in new[] { sparks, dust, motes, burst, vortex }) { ps.gameObject.SetActive(false); EditorUtility.SetDirty(ps); }
        }

        static void Setup(ParticleSystem ps, (float a, float b) life, (float a, float b) speed, (float a, float b) size, int max, float rate, float gravity)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.a, life.b); main.startSpeed = new ParticleSystem.MinMaxCurve(speed.a, speed.b);
            main.startSize = new ParticleSystem.MinMaxCurve(size.a, size.b); main.maxParticles = max; main.gravityModifier = gravity; main.prewarm = false;
            var em = ps.emission; em.rateOverTime = rate;
        }

        static void Shape(ParticleSystem ps, ParticleSystemShapeType type, Vector3? scale = null, float radius = 1, float random = 0, Vector3? rotation = null)
        {
            var sh = ps.shape; sh.enabled = true; sh.shapeType = type; sh.radius = radius; sh.radiusThickness = 1; sh.randomDirectionAmount = random;
            sh.scale = scale ?? Vector3.one; sh.rotation = rotation ?? Vector3.zero; sh.arc = 360;
        }

        static void Velocity(ParticleSystem ps, Vector3 v, ParticleSystemSimulationSpace space)
        {
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = space;
            vel.x = new ParticleSystem.MinMaxCurve(v.x); vel.y = new ParticleSystem.MinMaxCurve(v.y); vel.z = new ParticleSystem.MinMaxCurve(v.z);
        }

        // ---------------- the cinematic title in the letterbox overlay ----------------
        static void TitleCanvas(Hud hud)
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                RectTransform Find(string n) => prefab.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == n);
                RectTransform R(string n) => Find(n) ?? throw new Exception("Canvas element missing: " + n);
                RectTransform Ensure(string n, RectTransform parent, params Type[] components)
                {
                    var r = Find(n);
                    if (!r) r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>();
                    foreach (var t in components) if (!r.GetComponent(t)) r.gameObject.AddComponent(t);
                    r.SetParent(parent, false); return r;
                }
                void Band(RectTransform r, float from, float to) { r.anchorMin = new Vector2(.08f, from); r.anchorMax = new Vector2(.92f, to); r.pivot = new Vector2(.5f, .5f); r.offsetMin = r.offsetMax = Vector2.zero; }
                var overlay = R("intro-overlay"); var ink = R("subtitle-text").GetComponent<Text>();
                var title = Ensure("intro-title", overlay, typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
                Band(title, .745f, .855f);
                var t = title.GetComponent<Text>(); t.font = ink.font; t.fontSize = 46; t.fontStyle = FontStyle.Bold; t.color = Color.white;
                t.alignment = TextAnchor.MiddleCenter; t.supportRichText = true; t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.text = "I N S I D E   Y O U R   E Y E S";
                var shadow = title.GetComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .55f); shadow.effectDistance = new Vector2(0, -2);
                var rule = Ensure("intro-rule", overlay, typeof(CanvasRenderer), typeof(Image));
                rule.anchorMin = rule.anchorMax = new Vector2(.5f, .742f); rule.pivot = new Vector2(.5f, .5f); rule.anchoredPosition = Vector2.zero; rule.sizeDelta = new Vector2(280, 2);
                var line = rule.GetComponent<Image>(); line.color = new Color(1f, .93f, .8f, .75f); line.raycastTarget = false;
                var sub = Ensure("intro-subtitle", overlay, typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
                Band(sub, .69f, .735f);
                var s = sub.GetComponent<Text>(); s.font = ink.font; s.fontSize = 20; s.fontStyle = FontStyle.Normal; s.color = new Color(ink.color.r, ink.color.g, ink.color.b, .9f);
                s.alignment = TextAnchor.MiddleCenter; s.supportRichText = false; s.raycastTarget = false; s.text = "Discover How You See the World";
                sub.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, .5f);
                foreach (var r in new[] { title, rule, sub }) r.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var instances = hud.canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var key in new[] { "intro-title", "intro-rule", "intro-subtitle" })
                if (!hud.bindings.Any(b => b.key == key)) hud.bindings = hud.bindings.Append(new Hud.Binding { key = key, target = instances.First(r => r.name == key) }).ToArray();
            EditorUtility.SetDirty(hud);
        }

        // ---------------- sound keys ----------------
        static void ReferenceSfx()
        {
            var map = new (string key, string file)[]
            {
                ("gatelift", "fx_gatelift"), ("labreveal", "fx_labreveal"), ("wipe", "fx_wipe"), ("xray", "fx_xray"),
                ("extract", "fx_extract"), ("titlehit", "fx_titlehit"), ("vortex", "fx_vortex"), ("brainrise", "fx_brainrise"),
            };
            var effects = stage.sfx.effects.ToList();
            foreach (var (key, file) in map)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/" + file + ".wav");
                if (!clip) { Debug.LogWarning("REFERENCE sfx missing: " + file + " (run python Tools/HumanEye/generate_reference_sfx.py)"); continue; }
                effects.RemoveAll(e => e.key == key);
                effects.Add(new AudioDirector.Sfx { key = key, clip = clip });
            }
            stage.sfx.effects = effects.ToArray();
            EditorUtility.SetDirty(stage.sfx);
        }
    }
}
