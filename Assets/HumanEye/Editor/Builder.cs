using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// Explicit full regeneration (replaces manual scene and Canvas edits). Reproducibly generates the launch scene, data, meshes, materials and UI wiring.
    /// Writes generated assets, the launch scene, the Canvas prefab and module data;
    /// existing generated assets are updated in place, so GUIDs and references stay stable across re-runs.
    public static partial class Builder
    {
        public const string Root = "Assets/HumanEye/", Gen = Root + "Generated/", ScenePath = Root + "Scenes/HumanEye_Observatory.unity";
        public const string PickLayer = "HE_Pick", ObjectLayer = "HE_Object";

        static ChapterRunner runner; static Stage stage; static Narrator narrator; static Hud hud; static CameraDirector cams;
        static readonly List<CinemachineCamera> shots = new();
        static Transform shotRoot;
        static Shader anatomyShader, glowShader;

        [MenuItem("Tools/Human Eye/Authoring/Regenerate entire module (replaces manual edits)", priority = 1)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Replace authored scene?", "This regenerates the scene, Canvas prefab and generated assets, replacing manual edits. Normal player builds do not require this. Back up your work first.", "Regenerate", "Cancel")) return;
            Build();
            EditorUtility.DisplayDialog("Human Eye", "Module built:\n" + ScenePath + "\n\nPress Play, or run Tools > Human Eye > Validate Module.", "OK");
        }

        /// Batch entry: Unity -batchmode -executeMethod HumanEye.EditorTools.Builder.Build -quit
        public static void Build()
        {
            foreach (var f in new[] { "Meshes", "Materials", "Textures", "Data/Chapters", "UI" }) Directory.CreateDirectory(Gen + f);
            Directory.CreateDirectory(Root + "Scenes"); Directory.CreateDirectory(Root + "Data");
            AssetDatabase.Refresh();
            EnsureLayer(PickLayer); EnsureLayer(ObjectLayer);
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            if (!anatomyShader || !glowShader) throw new Exception("HumanEye shaders failed to compile/import.");
            shots.Clear();

            var dialogue = BuildDialogue();
            var chapters = BuildChapters();
            var bank = BuildQuestions();
            var knowledge = BuildKnowledge();
            var config = AssetDatabase.LoadAssetAtPath<ModuleConfig>(Root + "Data/ModuleConfig.asset");
            if (!config) { config = ScriptableObject.CreateInstance<ModuleConfig>(); AssetDatabase.CreateAsset(config, Root + "Data/ModuleConfig.asset"); }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSystems(dialogue, chapters, bank, knowledge, config);
            BuildEnvironment();
            BuildBrain();   // first: the optic nerve is generated to end at the brain's actual underside
            BuildEye();
            BuildCast();
            BuildObjects();
            BuildMicro();
            BuildShots();
            cams.shots = shots.ToArray();
            ConfigureLighting();
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_BUILT " + ScenePath);
        }

        /// Content-only update (chapters, questions, Ask Niko, dialogue): assets are upserted in place, so the saved
        /// scene and its hand-edited Canvas keep their references and are not regenerated.
        [MenuItem("Tools/Human Eye/Authoring/Update content data only (keeps scene)", priority = 2)]
        public static void BuildData()
        {
            BuildDialogue(); BuildChapters(); BuildQuestions(); BuildKnowledge();
            AssetDatabase.SaveAssets();
            Debug.Log("HUMAN_EYE_DATA_BUILT");
        }

        // ---------------- assets ----------------
        public static T Upsert<T>(T fresh, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(fresh, path); return fresh; }
            EditorUtility.CopySerialized(fresh, existing);
            EditorUtility.SetDirty(existing);
            if (fresh != existing) Object.DestroyImmediate(fresh);
            return existing;
        }

        internal static Mesh SaveMesh(Mesh m, string name) { m.name = name; return Upsert(m, Gen + "Meshes/" + name + ".asset"); }

        static Material Anatomy(string name, Color colour, Action<Material> setup = null)
        {
            var m = new Material(anatomyShader) { name = name };
            m.SetColor("_BaseColor", colour);
            setup?.Invoke(m);
            return Upsert(m, Gen + "Materials/" + name + ".mat");
        }

        static Material Glow(string name, Color colour, Texture tex = null, float intensity = 1, Vector2 scroll = default, float edgeFade = 0, float lengthFade = 0)
        {
            var m = new Material(glowShader) { name = name };
            m.SetColor("_Color", colour); m.SetFloat("_Intensity", intensity); m.SetVector("_Scroll", scroll);
            m.SetFloat("_EdgeFade", edgeFade); m.SetFloat("_LengthFade", lengthFade);
            if (tex) m.SetTexture("_MainTex", tex);
            return Upsert(m, Gen + "Materials/" + name + ".mat");
        }

        static Material Lit(string name, Color colour, float smoothness, float metallic = 0, Texture baseMap = null, Texture normal = null, Color? emission = null, bool alphaClip = false)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", colour); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", metallic);
            if (baseMap) m.SetTexture("_BaseMap", baseMap);
            if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            if (emission.HasValue) { m.SetColor("_EmissionColor", emission.Value); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            if (alphaClip) { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .45f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0); m.doubleSidedGI = true; }
            return Upsert(m, Gen + "Materials/" + name + ".mat");
        }

        internal static void EnsureLayer(string name)
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 8; i < 32; i++) if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
            for (int i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layers.GetArrayElementAtIndex(i).stringValue = name; tags.ApplyModifiedProperties(); return; }
            throw new Exception("No free layer slot for " + name);
        }

        public static GameObject Obj(string name, Transform parent, Mesh mesh = null, Material mat = null, Vector3? pos = null, Quaternion? rot = null, Vector3? scale = null, string layer = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (pos.HasValue) go.transform.localPosition = pos.Value;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            if (scale.HasValue) go.transform.localScale = scale.Value;
            if (mesh)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off; r.lightProbeUsage = LightProbeUsage.Off;
            }
            if (layer != null) go.layer = LayerMask.NameToLayer(layer);
            return go;
        }

        // ---------------- data ----------------
        static DialogueDatabase BuildDialogue()
        {
            var ledger = JsonUtility.FromJson<DialogueDatabase.Ledger>(File.ReadAllText(Root + "Data/DialogueLedger.json"));
            foreach (var c in ledger.cues)
            {
                var path = $"{Root}Audio/VO/{c.filename}.wav";
                c.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (!c.clip) Debug.LogWarning("VO missing (subtitle + Read & Continue fallback will be used): " + path);
                else
                {
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    var s = importer.defaultSampleSettings;
                    if (s.loadType != AudioClipLoadType.CompressedInMemory || s.compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .7f;
                        importer.defaultSampleSettings = s; importer.forceToMono = true; importer.SaveAndReimport();
                        c.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
                var align = Path.ChangeExtension(path, ".align.json");   // character timing from the TTS (generate_elevenlabs_vo.py)
                if (c.clip && File.Exists(align)) MouthKeys(c, File.ReadAllText(align));
                else if (c.clip) AudioKeys(c, path);
            }
            var db = ScriptableObject.CreateInstance<DialogueDatabase>();
            db.cues = ledger.cues;
            db.extra = ExtraLines();
            return Upsert(db, Gen + "Data/DialogueDatabase.asset");
        }

        /// Supplemental lines (Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv: key,text; clips Audio/VO/Extra/<key>.wav, Tools/HumanEye/
        /// generate_kokoro_vo.py): spoken by key like a cue, with the same import settings and mouth keys.
        static DialogueDatabase.Cue[] ExtraLines()
        {
            const string csv = "Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv";
            if (!File.Exists(csv)) return new DialogueDatabase.Cue[0];
            var list = new List<DialogueDatabase.Cue>();
            foreach (var row in File.ReadAllLines(csv).Skip(1).Where(l => l.Length > 0))
            {
                int comma = row.IndexOf(',');
                var key = row.Substring(0, comma); var text = row.Substring(comma + 1).Trim();
                if (text.Length > 1 && text[0] == '"') text = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
                var path = $"{Root}Audio/VO/Extra/{key}.wav";
                var c = new DialogueDatabase.Cue { id = key, speaker = "Niko", type = "Supplemental", text = text, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path) };
                if (c.clip)
                {
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    var s = importer.defaultSampleSettings;
                    if (s.loadType != AudioClipLoadType.CompressedInMemory || s.compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .7f;
                        importer.defaultSampleSettings = s; importer.forceToMono = true; importer.SaveAndReimport();
                        c.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                    AudioKeys(c, path);
                }
                else Debug.LogWarning("HE_VO supplemental line without a clip (caption only): " + path);
                list.Add(c);
            }
            return list.ToArray();
        }

        [Serializable] class Alignment { public string[] characters; public float[] character_start_times_seconds, character_end_times_seconds; }

        /// Mouth keys from character timing: each letter becomes an approximate viseme (jaw opening, lip rounding) a third of the
        /// way into its span; spaces close the mouth only on a real pause (> 80 ms). An approximation from spelling, not phonemes.
        public static void MouthKeys(DialogueDatabase.Cue c, string json)
        {
            var a = JsonUtility.FromJson<Alignment>(json);
            if (a?.characters == null || a.character_start_times_seconds == null || a.characters.Length != a.character_start_times_seconds.Length) return;
            (float open, float round) Pose(char ch) => char.ToLowerInvariant(ch) switch
            {
                'a' => (1f, 0f), 'e' => (.55f, 0f), 'i' => (.5f, 0f), 'y' => (.45f, 0f), 'o' => (.75f, .75f), 'u' => (.5f, 1f),
                'w' => (.35f, 1f), 'q' => (.35f, .9f), 'r' => (.4f, .35f), 'l' => (.5f, 0f), 'm' or 'b' or 'p' => (0f, 0f),
                'f' or 'v' => (.12f, 0f), _ when char.IsLetterOrDigit(ch) => (.3f, 0f), _ => (-1f, 0f),
            };
            var times = new List<float>(); var opens = new List<float>(); var rounds = new List<float>();
            void Key(float t, float o, float r) { if (times.Count > 0 && t <= times[^1]) t = times[^1] + .001f; times.Add(t); opens.Add(o); rounds.Add(r); }
            Key(0, 0, 0);
            for (int i = 0; i < a.characters.Length; i++)
            {
                if (string.IsNullOrEmpty(a.characters[i])) continue;
                float s = a.character_start_times_seconds[i], e = a.character_end_times_seconds[i];
                var (o, r) = Pose(a.characters[i][0]);
                if (o >= 0) Key(s + (e - s) * .33f, o, r);
                else if (e - s > .08f) { Key(s + .02f, 0, 0); Key(e - .02f, 0, 0); }
            }
            Key(a.character_end_times_seconds[^1] + .05f, 0, 0);
            c.mouthTimes = times.ToArray(); c.mouthOpen = opens.ToArray(); c.mouthRound = rounds.ToArray();
        }

        /// Mouth keys from a clip's own audio (16-bit PCM WAV) when no TTS timing exists: loudness against the clip's voiced
        /// level opens the jaw; the share of energy around 200-800 Hz against 1-4 kHz rounds the lips (rounded vowels sit low).
        /// Precomputed because WebGL cannot read audio output at runtime. Audio-driven, not phoneme-accurate.
        public static void AudioKeys(DialogueDatabase.Cue c, string wavPath)
        {
            var b = File.ReadAllBytes(wavPath);
            int rate = 0, channels = 0, bits = 0, data = -1, size = 0;
            for (int p = 12; p + 8 <= b.Length;)
            {
                var id = System.Text.Encoding.ASCII.GetString(b, p, 4); int len = BitConverter.ToInt32(b, p + 4);
                if (id == "fmt ") { channels = BitConverter.ToInt16(b, p + 10); rate = BitConverter.ToInt32(b, p + 12); bits = BitConverter.ToInt16(b, p + 22); }
                else if (id == "data") { data = p + 8; size = Math.Min(len, b.Length - data); break; }
                p += 8 + len + (len & 1);
            }
            if (data < 0 || bits != 16 || rate <= 0 || channels < 1) { Debug.LogWarning("HE_MOUTH not a 16-bit PCM WAV, no mouth keys: " + wavPath); return; }
            var x = new float[size / (2 * channels)];
            for (int i = 0; i < x.Length; i++) x[i] = BitConverter.ToInt16(b, data + i * 2 * channels) / 32768f;   // first channel
            float[] Band(float f0, float q)   // RBJ band-pass
            {
                double w = 2 * Math.PI * f0 / rate, al = Math.Sin(w) / (2 * q), a0 = 1 + al, a1 = -2 * Math.Cos(w) / a0, a2 = (1 - al) / a0, b0 = al / a0, x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                var y = new float[x.Length];
                for (int i = 0; i < x.Length; i++) { double v = b0 * (x[i] - x2) - a1 * y1 - a2 * y2; x2 = x1; x1 = x[i]; y2 = y1; y1 = v; y[i] = (float)v; }
                return y;
            }
            float[] low = Band(400, .67f), high = Band(2000, .67f);
            int hop = rate * 3 / 100, win = hop * 2;   // 30 ms steps over 60 ms windows
            var at = new List<float>(); var level = new List<float>(); var share = new List<float>();
            for (int s = 0; s + win <= x.Length; s += hop)
            {
                double e = 0, lo = 0, hi = 0;
                for (int i = s; i < s + win; i++) { e += x[i] * x[i]; lo += low[i] * low[i]; hi += high[i] * high[i]; }
                at.Add((s + win * .5f) / rate); level.Add((float)Math.Sqrt(e / win)); share.Add((float)(lo / Math.Max(1e-12, lo + hi)));
            }
            var sorted = level.OrderBy(v => v).ToArray();
            float peak = sorted.Length > 0 ? sorted[(int)(.9f * (sorted.Length - 1))] : 0;
            if (at.Count < 2 || peak <= 1e-5f) return;
            // ponytail: the rounding range (0.87-0.93 low-band share) is calibrated on the OneCore VO (voiced p60-p95); a new
            // voice gets character timing from the TTS instead, which takes precedence (MouthKeys).
            var t = new List<float> { 0 }; var o = new List<float> { 0 }; var r = new List<float> { 0 };
            for (int i = 0; i < at.Count; i++)
            {
                float open = Mathf.Clamp01((level[i] / peak - .12f) * 1.15f), round = level[i] > peak * .25f ? Mathf.Clamp01((share[i] - .87f) / .06f) : 0;
                if (Mathf.Abs(open - o[^1]) > .06f || Mathf.Abs(round - r[^1]) > .1f || at[i] - t[^1] >= .15f) { t.Add(Mathf.Max(at[i], t[^1] + .001f)); o.Add(open); r.Add(round); }
            }
            t.Add(t[^1] + .05f); o.Add(0); r.Add(0);
            c.mouthTimes = t.ToArray(); c.mouthOpen = o.ToArray(); c.mouthRound = r.ToArray();
        }

        static ChapterDefinition[] BuildChapters() => HumanEyeContent.Chapters().Select(c =>
        {
            var d = ScriptableObject.CreateInstance<ChapterDefinition>();
            d.id = c.id; d.title = c.title; d.checkpoint = c.checkpoint; d.topics = c.topics; d.steps = c.steps; d.explore = c.explore ?? new ExploreEntry[0];
            return Upsert(d, $"{Gen}Data/Chapters/{c.id}.asset");
        }).ToArray();

        static QuestionBank BuildQuestions()
        {
            var b = ScriptableObject.CreateInstance<QuestionBank>();
            b.items = HumanEyeContent.Questions().ToArray();
            return Upsert(b, Gen + "Data/QuestionBank.asset");
        }

        static NikoKnowledge BuildKnowledge()
        {
            var k = ScriptableObject.CreateInstance<NikoKnowledge>();
            k.intents = HumanEyeContent.Intents(); k.suggestions = HumanEyeContent.Suggestions(); k.fallback = HumanEyeContent.Fallback;
            return Upsert(k, Gen + "Data/NikoKnowledge.asset");
        }

        // ---------------- systems ----------------
        static AudioSource Source(Transform parent, string name, bool loop = false)
        {
            var s = new GameObject(name).AddComponent<AudioSource>();
            s.transform.SetParent(parent, false);
            s.playOnAwake = loop; s.loop = loop; s.spatialBlend = 0;
            return s;
        }

        static void BuildSystems(DialogueDatabase dialogue, ChapterDefinition[] chapters, QuestionBank bank, NikoKnowledge knowledge, ModuleConfig config)
        {
            var sys = new GameObject("HumanEye_Systems").transform;
            var boot = sys.gameObject.AddComponent<ModuleBoot>();
            runner = sys.gameObject.AddComponent<ChapterRunner>();
            narrator = sys.gameObject.AddComponent<Narrator>();
            var audio = sys.gameObject.AddComponent<AudioDirector>();
            stage = sys.gameObject.AddComponent<Stage>();
            var activities = sys.gameObject.AddComponent<ActivityController>();
            var guide = sys.gameObject.AddComponent<NikoGuide>();
            var pointer = sys.gameObject.AddComponent<PointerSelector>();

            narrator.database = dialogue; narrator.voice = Source(sys, "Voice");
            audio.narrator = narrator; audio.music = Source(sys, "Music", true); audio.sfx = Source(sys, "Effects");
            audio.music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Music/observatory_pad.wav");
            string K(string f) => Root + "ThirdParty/Kenney/Audio/" + f;
            var map = new (string key, string file)[]
            {
                ("select", "ui_tap.ogg"), ("tap", "ui_click_soft.ogg"), ("confirm", "ui_confirm.ogg"), ("deny", "ui_error_soft_deny.ogg"),
                ("scan", "scan_sweep_hum.ogg"), ("whoosh", "transition_whoosh.ogg"), ("reveal", "transition_whoosh.ogg"),
                ("discovery", "chime_discovery.ogg"), ("unlock", "chime_soft_bong.ogg"), ("ping", "chime_discovery.ogg"),
                ("tick", "signal_tick_beep.ogg"), ("pulse", "signal_pulse.ogg"),
            };
            audio.effects = map.Select(m => new AudioDirector.Sfx { key = m.key, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(K(m.file)) }).ToArray();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = .01f; cam.farClipPlane = 140; cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = new Color(.008f, .012f, .025f);
            cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>(); camData.renderPostProcessing = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.5f);
            brain.IgnoreTimeScale = true;
            cams = sys.gameObject.AddComponent<CameraDirector>(); cams.brain = brain;
            shotRoot = new GameObject("Shots (cue sheet cameras)").transform;

            var uiGo = new GameObject("HUD");
            hud = uiGo.AddComponent<Hud>();
            hud.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "ThirdParty/Fonts/AtkinsonHyperlegible/AtkinsonHyperlegible-Regular.ttf");
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            runner.chapters = chapters; runner.narrator = narrator; runner.cameras = cams; runner.stage = stage; runner.audioDirector = audio; runner.activities = activities;
            stage.runner = runner; stage.cams = cams; stage.narrator = narrator; stage.sfx = audio; stage.hud = hud; stage.activities = activities; stage.guide = guide;
            stage.lookCamera = camGo.transform;
            activities.bank = bank; activities.stage = stage; activities.narrator = narrator;
            guide.knowledge = knowledge; guide.runner = runner; guide.stage = stage; guide.narrator = narrator; guide.dialogue = dialogue;
            pointer.cam = cam; pointer.runner = runner; pointer.stage = stage; pointer.hud = hud; pointer.pickMask = LayerMask.GetMask(PickLayer);
            hud.runner = runner; hud.stage = stage; hud.narrator = narrator; hud.guide = guide; hud.activities = activities; hud.cams = cams; hud.mainCamera = cam;
            CanvasAuthoring.Create(hud);
            boot.config = config; boot.hud = hud; boot.runner = runner; boot.narrator = narrator; boot.cams = cams; boot.stage = stage;
        }

        // ---------------- cameras ----------------
        public static CinemachineCamera Shot(string name, Transform parent, Vector3 localPos, Vector3 localLook, float fov)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ? parent : shotRoot, false);
            go.transform.localPosition = localPos;
            var p = parent ? parent : shotRoot;
            go.transform.rotation = Quaternion.LookRotation(p.TransformPoint(localLook) - go.transform.position, Vector3.up);
            var c = go.AddComponent<CinemachineCamera>();
            var lens = c.Lens; lens.FieldOfView = fov; lens.NearClipPlane = .01f; lens.FarClipPlane = 140; c.Lens = lens;
            c.Priority = 0;
            shots.Add(c);
            return c;
        }

        static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene> { new(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) list.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 2", priority = 20)] static void P2() => Preview(0);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 3")] static void P3() => Preview(1);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 4")] static void P4() => Preview(2);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 5")] static void P5() => Preview(3);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 6")] static void P6() => Preview(4);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 7")] static void P7() => Preview(5);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 8")] static void P8() => Preview(6);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 9")] static void P9() => Preview(7);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 10")] static void P10() => Preview(8);
        [MenuItem("Tools/Human Eye/Preview Chapter (editor only)/Scene 11")] static void P11() => Preview(9);

        /// Editor-only preview: jumps straight to a chapter checkpoint in Play mode. Not available in player builds.
        static void Preview(int index)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorPrefs.SetInt("HumanEye.Preview", index);
            EditorApplication.EnterPlaymode();
        }
    }
}
