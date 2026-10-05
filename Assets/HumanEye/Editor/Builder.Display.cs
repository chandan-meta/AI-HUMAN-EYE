using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Idempotent, save-guarded patch of the SAVED scene: stages the module on the HUMAN BRAIN ENV pedestal
    /// (center_rotator), swaps in the user's models and authors the cinematic intro. Never regenerates the scene.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.StageOnRotator
    public static partial class Builder
    {
        const string UM = Root + "ThirdParty/UserModels/";
        // The pedestal's glowing disc top goes where the old eye-plinth top was, directly under the hero eye (0, 1.5, 0.6).
        // Moving the room (one rigid translation, bake included) keeps every shot, anchor, path and light valid.
        static readonly Vector3 DiscTarget = new(0, .89f, .6f);

        [MenuItem("Tools/Human Eye/Authoring/Stage on center_rotator (keeps scene edits)", priority = 4)]
        public static void StageOnRotator()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // never discards unsaved editor work
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            hud = stage.hud; cams = stage.cams; narrator = stage.narrator; runner = stage.runner;
            shotRoot = GameObject.Find("Shots (cue sheet cameras)").transform;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var env = GameObject.Find("human brain env")?.transform ?? throw new Exception("The HUMAN BRAIN ENV prefab instance is not in the saved scene.");

            MoveRoom(env);
            RemoveMicroscope();
            DisplayLights();
            PushAnchorsOffPedestal();
            Human();
            UserBrain();
            RoomFixes(env);
            IntroShots();
            IntroSfx();
            SkipButton();
            AndroidCaps("Assets/human brain env", UM);

            EditorUtility.SetDirty(stage); EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_STAGED_ON_ROTATOR");
        }

        static Transform Child(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static void MoveRoom(Transform env)
        {
            var disc = Child(env, "emission_center").GetComponent<Renderer>();
            var top = disc.bounds.center; top.y = disc.bounds.max.y;
            var delta = DiscTarget - top;
            env.position += delta;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Rectangle && !l.transform.IsChildOf(env)) l.transform.position += delta;   // the user's baked area light travels with the room
            Debug.Log($"HE_ROTATOR disc top {top} -> {DiscTarget} (moved {delta}); disc radius {disc.bounds.extents.x:0.00}; room floor y {Child(env, "floor").GetComponent<Renderer>().bounds.max.y:0.00}");
        }

        /// The earlier microscope exhibit duplicated the room's own microscopes; its source folder is gone, so drop the instance.
        static void RemoveMicroscope()
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true).ToArray())
                    if (t && (t.name == "Microscope exhibit (HUMAN BRAIN ENV)" || PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset)) Object.DestroyImmediate(t.gameObject);
        }

        /// The reveals and the ending drive these lights; they lived under the (now hidden) procedural dome.
        static void DisplayLights()
        {
            var group = GameObject.Find("HE Display Lights") ?? new GameObject("HE Display Lights");
            var names = new HashSet<string> { "Key Light", "Display Light", "Rim Light", "Observatory Reflection", "Starlight Beam", "Focused Beam", "Beam Dust" };
            foreach (Transform t in stage.observatory.transform.Cast<Transform>().ToArray()) if (names.Contains(t.name)) t.SetParent(group.transform, true);
            group.SetActive(true);
            stage.observatory = group;
            // Starlight beam: vertical from the oculus down onto the disc (the strike in the intro, the glow afterwards).
            var oculus = Child(GameObject.Find("human brain env").transform, "top_center_light");
            float topY = oculus ? oculus.GetComponent<Renderer>().bounds.min.y : 9.2f;
            var beam1 = Child(group.transform, "Starlight Beam"); var beam2 = Child(group.transform, "Focused Beam"); var dust = Child(group.transform, "Beam Dust");
            float l1 = beam1.GetComponent<MeshFilter>().sharedMesh.bounds.size.z, l2 = beam2.GetComponent<MeshFilter>().sharedMesh.bounds.size.z;
            float k = (topY - DiscTarget.y) / (l1 + l2);
            var from = new Vector3(DiscTarget.x, topY, DiscTarget.z);
            beam1.SetPositionAndRotation(from, Quaternion.LookRotation(Vector3.down)); beam1.localScale = new Vector3(.55f, .55f, k);
            beam2.SetPositionAndRotation(from + Vector3.down * l1 * k, Quaternion.LookRotation(Vector3.down)); beam2.localScale = new Vector3(.55f, .55f, k);
            dust.SetPositionAndRotation(from + Vector3.down * (topY - DiscTarget.y) * .5f, Quaternion.LookRotation(Vector3.down));
            stage.beamRenderers = new[] { beam1, beam2, dust }.Select(t => t.GetComponent<Renderer>()).ToArray();
        }

        /// Six anchors stood inside the pedestal body; Niko now hovers at its rim. Shots that frame him follow.
        static void PushAnchorsOffPedestal()
        {
            foreach (var a in stage.nikoAnchors)
            {
                var d = new Vector2(a.position.x - DiscTarget.x, a.position.z - DiscTarget.z);
                if (d.magnitude >= 2.3f) continue;
                var n = d.normalized * 2.35f;
                var moved = new Vector3(DiscTarget.x + n.x, a.position.y, DiscTarget.z + n.y);
                if (a.name == "Intro") foreach (var s in new[] { "S02_NikoMedium", "S02_PinchNiko" }) ShotByName(s).transform.position += moved - a.position;
                a.position = moved;
            }
        }

        static Unity.Cinemachine.CinemachineCamera ShotByName(string name) => cams.shots.First(s => s.name == name);

        /// Absolute (re)authoring of a world shot: idempotent on re-runs.
        static void Aim(string name, Vector3 pos, Vector3 look, float fov = 0)
        {
            var s = cams.shots.FirstOrDefault(x => x.name == name);
            if (!s) { s = Shot(name, null, pos, look, fov); cams.shots = cams.shots.Append(s).ToArray(); return; }
            s.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            if (fov > 0) { var lens = s.Lens; lens.FieldOfView = fov; s.Lens = lens; }
        }

        // ---------------- human on the pedestal ----------------
        static void Human()
        {
            float dy = DiscTarget.y - HumanPosition.y;
            var hd = File.Exists(UM + "Matt/Matt_Display.fbx") ? HumanMatt() : stage.human;
            hd.transform.position = new Vector3(0, DiscTarget.y, HumanPosition.z);   // feet on the disc, toward its back edge
            var head = hd.eyeRight.position;
            Aim("S02_EstablishA", new Vector3(0, 2.7f + dy, -10.5f), new Vector3(0, 2.6f + dy, 3));
            Aim("S02_EstablishB", new Vector3(0, 2.0f + dy, -7.2f), new Vector3(0, 1.6f + dy, 2));
            Aim("S02_DisplayWide", new Vector3(-.45f, 1.55f + dy, -3.7f), new Vector3(-.4f, 1.15f + dy, 1.7f));
            Aim("S02_ScanFront", new Vector3(0, 1.2f + dy, -2.8f), new Vector3(0, 1.0f + dy, 2));
            Aim("S02_EyeInSkull", head + new Vector3(.12f, .04f, -.8f), head + new Vector3(.03f, -.005f, 0));
        }

        static Texture2D Tex(string path, bool alpha = false)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new Exception("Missing texture " + path);
            if (alpha && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// The user's Reallusion figure (static export of the 28k base body + clothes + hair) with the user's skeleton inside as
        /// the X-ray layer. Source files unaltered; Z-Anatomy keeps the hollow skull for the eye-in-skull close-up.
        static HumanDisplay HumanMatt()
        {
            var old = stage.human ? stage.human.gameObject : null;
            var human = new GameObject("Human Display").transform;
            var hd = human.gameObject.AddComponent<HumanDisplay>();
            var temp = new GameObject("temp-import").transform;
            var matt = Instantiate(UM + "Matt/Matt_Display.fbx", temp);
            var skel = Instantiate(UM + "Matt/Skeleton_Display.fbx", temp);
            Renderer[] R(GameObject g) => g.GetComponentsInChildren<Renderer>(true);
            Vector3 Socket(string n) => Child(matt.transform, n).position;
            // Face the learner (-Z) and square the eye axis, exactly as the FitnessMan path did; both models share one frame.
            var b = WorldBounds(R(matt)); var mid = (Socket("EyeSocket_L") + Socket("EyeSocket_R")) * .5f;
            temp.rotation = Quaternion.FromToRotation(new Vector3(mid.x - b.center.x, 0, mid.z - b.center.z).normalized, Vector3.back);
            var across = Socket("EyeSocket_L") - Socket("EyeSocket_R"); across.y = 0;
            temp.rotation = Quaternion.FromToRotation(across.normalized, Vector3.right) * temp.rotation;
            b = WorldBounds(R(matt)); temp.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 eyeL = Socket("EyeSocket_L"), eyeR = Socket("EyeSocket_R");
            Debug.Log($"HE_HUMAN Matt eyes {eyeL} / {eyeR} bounds {WorldBounds(R(matt))}");

            Material Skin(string slot)
            {
                bool clip = slot is "Hair" or "Scalp";   // card strips carry their opacity in the alpha channel
                return Anatomy("Matt_" + slot, Color.white, m =>
                {
                    m.SetTexture("_BaseMap", Tex(UM + "Matt/" + slot + (clip ? "_DA.png" : "_D.png"), clip));
                    m.SetFloat("_Wrap", .55f); m.SetFloat("_Gloss", clip ? .45f : .3f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .3f); m.SetColor("_RimColor", new Color(.4f, .8f, 1f));
                    if (clip) { m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0); }
                });
            }
            foreach (var r in R(matt)) r.sharedMaterials = r.sharedMaterials.Select(m => Skin(m.name)).ToArray();
            var body = R(matt).First(r => r.name == "Body"); var jeans = R(matt).First(r => r.name == "Jeans");
            matt.transform.SetParent(human, true);
            hd.skin = NewPart(human, "skin", "Skin", 0, false, 1, 4); hd.skin.main = R(matt).Where(r => r != jeans).ToArray();
            hd.garment = NewPart(human, "garment", "Jeans", 0, false, 1, 5); hd.garment.main = new[] { jeans };   // stays opaque through the scan

            var muscleMat = Anatomy("Human_Muscle", new Color(.62f, .15f, .13f), m => { m.SetFloat("_Wrap", .5f); m.SetFloat("_Gloss", .5f); m.SetFloat("_Specular", .35f); m.SetFloat("_Rim", .3f); m.SetColor("_RimColor", new Color(1, .5f, .4f)); });
            var muscle = Combine("Muscles", new[] { body }, human, muscleMat, "Human_Muscles");
            var mm = muscle.GetComponent<MeshFilter>().sharedMesh; var mv = mm.vertices; var mn = mm.normals;
            for (int i = 0; i < mv.Length; i++) mv[i] -= mn[i] * .004f;
            mm.vertices = mv; mm.RecalculateBounds();
            hd.muscles = NewPart(human, "muscles", "Muscles", 0, false, 1, 3); hd.muscles.main = new[] { muscle.GetComponent<Renderer>() };

            // X-ray layer: the user's skeleton (minus its solid cranium and mandible) plus the hollow Z-Anatomy skull.
            var (skull, brain, visual) = PlaceZAnatomy(temp, eyeL, eyeR, Vector3.back);
            var boneMat = Anatomy("Human_Skull", new Color(.88f, .85f, .78f), m => { m.SetFloat("_Wrap", .45f); m.SetFloat("_Gloss", .3f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .25f); m.SetFloat("_Cull", 0); });   // Cull off: one hand mesh is inside-out in the source
            var bones = R(skel).Where(r => r.name is not ("Skeleton.083" or "Skeleton.146")).Append(skull);
            hd.skull = NewPart(human, "skull", "Skeleton", 0, false, 1, 2); hd.skull.main = new[] { Combine("Skeleton", bones, human, boneMat, "Human_Skull").GetComponent<Renderer>() };
            hd.brain = NewPart(human, "brain_layer", "Brain", 0, false, 1, 1);
            hd.brain.main = new[] { Combine("Brain", new[] { brain, visual }, human, AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Brain.mat"), "Human_Brain").GetComponent<Renderer>() };

            const float eyeRadius = .0158f;   // Matt's sockets are larger than FitnessMan's
            hd.eyeLeft = Obj("Socket (other eye)", human, pos: human.InverseTransformPoint(eyeL), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.eyeRight = Obj("Socket (selected eye)", human, pos: human.InverseTransformPoint(eyeR), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.headTop = WorldBounds(R(matt)).max.y; hd.headY = eyeL.y;
            var scan = Obj("Scan Wave", human, Disc("ScanDisc", .9f, 64), Glow("ScanWave", new Color(.2f, .7f, 1f, .55f), ProceduralTextures.Soft(), 2.2f), new Vector3(0, 1, 0), Quaternion.Euler(90, 0, 0));
            Obj("Scan Ring", scan.transform, Ring("ScanRing", .9f, .012f, 64), Glow("ScanRingGlow", new Color(.35f, .9f, 1f, 1), null, 3), rot: Quaternion.Euler(-90, 0, 0));
            hd.scanPlane = scan.transform; hd.scanRenderer = scan.GetComponent<Renderer>();
            foreach (var r in human.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            Object.DestroyImmediate(temp.gameObject);
            if (old) Object.DestroyImmediate(old);
            stage.human = hd;
            return hd;
        }

        // ---------------- the user's brain as the Brain Display ----------------
        /// Brain.fbx is one mesh. Its occipital faces become a separate "visual processing area" mesh (glow, tap target) and the
        /// model is placed so its chiasm region meets the existing optic nerve; nothing in the source file changes.
        static void UserBrain()
        {
            var path = UM + "Brain/Brain.fbx";
            if (!File.Exists(path)) return;
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && (mi.importLights || mi.importAnimation || mi.importCameras || mi.materialImportMode != ModelImporterMaterialImportMode.None))
            {
                mi.importLights = mi.importCameras = mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.SaveAndReimport();
            }
            var temp = new GameObject("temp-brain").transform;
            var src = Instantiate(path, temp).GetComponentInChildren<MeshFilter>();
            var mesh = src.sharedMesh; var xf = src.transform.localToWorldMatrix;   // model space: Y up, front +Z, metres
            var v = mesh.vertices.Select(p => xf.MultiplyPoint3x4(p)).ToArray();
            var bb = new Bounds(v[0], Vector3.zero); foreach (var p in v) bb.Encapsulate(p);
            bool Occipital(Vector3 c) => c.z < bb.min.z + .20f * bb.size.z && (Mathf.Abs(c.x - bb.center.x) > .025f * bb.size.x || c.y > bb.min.y + .427f * bb.size.y);
            var tri = mesh.triangles; var rest = new List<int>(); var occ = new List<int>();
            for (int i = 0; i < tri.Length; i += 3)
            {
                var c = (v[tri[i]] + v[tri[i + 1]] + v[tri[i + 2]]) / 3;
                (Occipital(c) ? occ : rest).AddRange(new[] { tri[i], tri[i + 1], tri[i + 2] });
            }
            Mesh Part(List<int> t, string name)
            {
                var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetUVs(0, mesh.uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
                return SaveMesh(m, name);
            }
            var restMesh = Part(rest, "UserBrain_Rest"); var occMesh = Part(occ, "UserBrain_Visual");
            Object.DestroyImmediate(temp.gameObject);
            Debug.Log($"HE_BRAIN user model {bb.size} tris {tri.Length / 3} occipital tris {occ.Count / 3}");

            var nerve = stage.signals.paths.First(p => p.id == "nerve"); int n = nerve.points.Length;
            var entry = nerve.points[n - 4];   // last point of the generated optic-nerve curve (the existing nerve mesh ends here)
            float s = 1.75f / bb.size.z;        // illustrative display length, as before
            var rot = Quaternion.LookRotation(Vector3.left);   // front (+Z) faces the eye at -X
            var chiasm = new Vector3(bb.center.x, bb.min.y + .27f * bb.size.y, bb.max.z - .36f * bb.size.z);
            var pos = entry - rot * (chiasm * s);
            var tex = Tex(UM + "Brain/BrainDiff03.png");
            var whole = stage.brain; var area = stage.visualArea;
            var wholeR = whole.main[0]; var areaR = area.transform.Find("Visual processing area").GetComponent<Renderer>();
            area.main = new[] { areaR };   // the generator never assigned it, so the area's glow/alpha had no renderer to drive
            wholeR.GetComponent<MeshFilter>().sharedMesh = restMesh; areaR.GetComponent<MeshFilter>().sharedMesh = occMesh;
            foreach (var r in new[] { wholeR, areaR }) { r.transform.SetPositionAndRotation(pos, rot); r.transform.localScale = Vector3.one * s; r.shadowCastingMode = ShadowCastingMode.On; }
            wholeR.sharedMaterial = Anatomy("Brain_User", Color.white, m => { m.SetTexture("_BaseMap", tex); m.SetFloat("_Wrap", .6f); m.SetFloat("_Gloss", .25f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .22f); });
            areaR.sharedMaterial = Anatomy("Brain_VisualArea_User", Color.white, m => { m.SetTexture("_BaseMap", tex); m.SetFloat("_Wrap", .6f); m.SetFloat("_Gloss", .25f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .22f); m.SetColor("_HighlightColor", new Color(.3f, .62f, 1f)); m.SetFloat("_HighlightTint", .65f); });
            var all = WorldBounds(new[] { wholeR, areaR }); var ab = areaR.bounds;
            var box = (BoxCollider)whole.pick[0]; box.center = whole.transform.InverseTransformPoint(all.center); box.size = all.size * .9f;
            var abox = (BoxCollider)area.pick[0]; abox.center = area.transform.InverseTransformPoint(ab.center); abox.size = ab.size;
            whole.focus.position = all.center; whole.labelAnchor.position = all.center + new Vector3(-.2f, all.extents.y + .25f, 0);
            area.focus.position = ab.center; area.labelAnchor.position = ab.center + new Vector3(.45f, .6f, -.3f);
            var vis = ab.center;
            nerve.points[n - 3] = entry + new Vector3(.25f, .12f, 0); nerve.points[n - 2] = (entry + vis) * .5f + Vector3.up * .15f; nerve.points[n - 1] = vis;
            EditorUtility.SetDirty(stage.signals);
            Aim("S04_BrainClose", vis + new Vector3(1.0f, .55f, -2.1f), vis + new Vector3(-.3f, 0, 0));
        }

        // ---------------- room package fixes ----------------
        static void RoomFixes(Transform env)
        {
            var players = env.GetComponentsInChildren<VideoPlayer>(true);
            foreach (var p in players) p.audioOutputMode = VideoAudioOutputMode.None;      // the clip's music bed played twice over the narration
            for (int i = 1; i < players.Length; i++) players[i].enabled = false;           // both decode into the same render texture
            foreach (var g in env.GetComponentsInChildren<GraphicRaycaster>(true)) g.enabled = false;   // world-space screens blocked part taps
            foreach (var t in env.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);   // deleted DynamicLightController
            stage.roomLights = env.GetComponentsInChildren<Light>(true).Where(l => l.type != LightType.Rectangle).ToArray();
            var volume = GameObject.Find("Intro Exposure")?.GetComponent<Volume>();
            if (!volume)
            {
                volume = new GameObject("Intro Exposure").AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 2; volume.weight = 0;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.Add<ColorAdjustments>().postExposure.Override(0);
                volume.sharedProfile = SaveProfile(profile, "IntroProfile");
            }
            stage.introVolume = volume;
        }

        /// Three intro shots sweep a quarter circle around the pedestal (door side → front), ending on S02_EstablishB.
        static void IntroShots()
        {
            var c = DiscTarget;
            Aim("S02_IntroThreshold", c + new Vector3(-14, .75f, 0), c + new Vector3(0, .6f, 0), 50);
            Aim("S02_IntroIgnite", c + new Vector3(-8.5f, .7f, -1.6f), c + new Vector3(0, .5f, 0), 44);
            Aim("S02_IntroCrane", c + new Vector3(-5, 4.2f, -5.6f), c + new Vector3(0, .2f, 0), 40);
        }

        /// Project-owned intro sounds (Tools/HumanEye/generate_intro_sfx.py); keys are skipped silently until the files exist.
        static void IntroSfx()
        {
            var map = new (string key, string file)[] { ("drone", "intro_drone"), ("riser", "intro_riser"), ("boom", "intro_boom"), ("clunk1", "intro_clunk_1"), ("clunk2", "intro_clunk_2"), ("clunk3", "intro_clunk_3"), ("shimmer", "intro_shimmer"), ("materialise", "intro_materialise") };
            var effects = stage.sfx.effects.ToList();
            foreach (var (key, file) in map)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/SFX/" + file + ".wav");
                if (!clip) continue;
                effects.RemoveAll(e => e.key == key);
                effects.Add(new AudioDirector.Sfx { key = key, clip = clip });
            }
            stage.sfx.effects = effects.ToArray();
            EditorUtility.SetDirty(stage.sfx);
        }

        /// Authored in the saved Canvas prefab (the walkthrough test requires every UI object to exist before Play).
        static void SkipButton()
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                if (!prefab.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "btn-skip-intro"))
                {
                    var src = prefab.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "btn-free-continue");
                    var copy = Object.Instantiate(src.gameObject, src.parent); copy.name = "btn-skip-intro";
                    copy.transform.SetSiblingIndex(src.GetSiblingIndex() + 1);
                    copy.GetComponentInChildren<Text>(true).text = "Skip intro ▶"; copy.SetActive(false);
                    PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var inst = hud.canvas.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "btn-skip-intro");
            if (!hud.bindings.Any(b => b.key == "btn-skip-intro")) hud.bindings = hud.bindings.Append(new Hud.Binding { key = "btn-skip-intro", target = inst }).ToArray();
        }

        /// Tablet budget: the room ships 4K textures with no overrides.
        static void AndroidCaps(params string[] folders)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", folders))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is not TextureImporter ti) continue;
                var s = ti.GetPlatformTextureSettings("Android");
                if (s.overridden && s.maxTextureSize == 1024 && s.format == TextureImporterFormat.ASTC_6x6) continue;
                s.overridden = true; s.maxTextureSize = 1024; s.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(s); ti.SaveAndReimport();
            }
        }
    }
}
