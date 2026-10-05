using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static class CinematicPatch
    {
        const string Folder = "Assets/HumanEye/Generated/Cinematic/";
        static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c ? c : go.AddComponent<T>(); }
        [MenuItem("Tools/Human Eye/Authoring/Apply cinematic entrance (keeps scene edits)")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Builder.ScenePath);
            var stage = Object.FindFirstObjectByType<Stage>();
            var env = GameObject.Find("human brain env");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var root = GameObject.Find("Cinematic Laboratory") ?? new GameObject("Cinematic Laboratory");
            var entrance = Ensure<LabEntrance>(root);
            stage.entrance = entrance;
            var renderers = env.GetComponentsInChildren<Renderer>(true);
            foreach (var t in env.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var video in env.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true)) { video.enabled = false; video.playOnAwake = false; }
            entrance.leaves = renderers.Where(r => r.name.Contains("Door01Up_") || r.name.Contains("Door01lock_down_")).Select(r => r.transform).ToArray();
            if (entrance.leaves.Length != 2) throw new Exception("Expected the two authored gate leaves.");
            // Use the stored closed positions on a repeat application, even if an editor preview left the gate open.
            if (entrance.closed == null || entrance.closed.Length != 2) entrance.closed = entrance.leaves.Select(t => t.localPosition).ToArray();
            entrance.open = entrance.leaves.Select((t, i) => entrance.closed[i] + t.parent.InverseTransformVector(Vector3.up * (t.name.Contains("Up_") ? 6.1f : -1.7f))).ToArray();
            entrance.SetGate(0);
            void Shot(string name, Vector3 position, Vector3 target, float fov)
            {
                var shot = stage.cams.shots.First(s => s.name == name);
                shot.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
                var lens = shot.Lens; lens.FieldOfView = fov; lens.NearClipPlane = .03f; shot.Lens = lens;
                EditorUtility.SetDirty(shot);
            }
            Shot("S02_IntroThreshold", new Vector3(-25, 2, .67f), new Vector3(-17.16f, 2.4f, .67f), 48);
            Shot("S02_IntroIgnite", new Vector3(-20.4f, 2, .67f), new Vector3(-17.16f, 2.4f, .67f), 48);
            Shot("S02_IntroCrane", new Vector3(-9, 2.2f, .67f), stage.human.eyeRight.position, 43);
            Shot("S02_EstablishB", new Vector3(-1.8f, 2.2f, -2.7f), stage.human.transform.position + Vector3.up * 1.05f, 36);
            Shot("S02_EyeInSkull", stage.human.eyeRight.position + new Vector3(.06f, .015f, -.55f), stage.human.eyeRight.position, 28);
            foreach (var l in env.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Rectangle) continue;
                l.intensity = l.type == LightType.Point ? 1.2f : 2.5f;
                l.shadows = LightShadows.None;
            }
            stage.roomLights = env.GetComponentsInChildren<Light>(true).Where(l => l.type != LightType.Rectangle).ToArray();
            stage.keyLight.intensity = .65f;
            stage.displayLight.type = LightType.Spot; stage.displayLight.intensity = 22; stage.displayLight.range = 12;
            stage.displayLight.spotAngle = 38; stage.displayLight.innerSpotAngle = 26;
            stage.displayLight.shadows = LightShadows.Soft;
            stage.displayLight.transform.position = stage.human.transform.position + new Vector3(-.8f, 5, -1.4f);
            stage.displayLight.transform.LookAt(stage.human.transform.position + Vector3.up);
            foreach (var light in stage.observatory.GetComponentsInChildren<Light>())
                if (light.name == "Rim Light") { light.intensity = 1.4f; light.shadows = LightShadows.None; }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.055f, .07f, .095f);
            var mats = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var r in renderers)
            {
                r.lightmapIndex = -1;
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (r.name is "pCube3" or "polySurface60" or "polySurface61") r.enabled = false;
                r.sharedMaterials = r.sharedMaterials.Select(src =>
                {
                    if (!src) return src;
                    if (mats.TryGetValue(src, out var existing)) return existing;
                    string name = src.name.Replace("Cinematic_", "");
                    string path = Folder + "Cinematic_" + name.Replace('/', '_').Replace(':', '_') + ".mat";
                    var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!copy) { copy = new Material(src); AssetDatabase.CreateAsset(copy, path); }
                    if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", r.name == "floor" ? .18f : .35f);
                    if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", name is "MT9" or "MT3" ? new Color(.24f, .29f, .34f) : new Color(.65f, .7f, .75f));
                    if (copy.HasProperty("_EmissionColor")) copy.SetColor("_EmissionColor", name.Contains("blue") ? new Color(.015f, .09f, .13f) : new Color(.08f, .10f, .12f));
                    EditorUtility.SetDirty(copy); mats[src] = copy; return copy;
                }).ToArray();
                var mf = r.GetComponent<MeshFilter>();
                // Static imported mesh collision preserves openings; no bounding-box wall that seals the entrance.
                if (mf && mf.sharedMesh && r.bounds.size.magnitude > .8f && !r.name.Contains("LOD") && r.name != "images")
                {
                    if (mf.sharedMesh.bounds.size.magnitude > 500 && r.bounds.size.magnitude < 4)
                    {
                        var meshCollider = r.GetComponent<MeshCollider>(); if (meshCollider) Object.DestroyImmediate(meshCollider);
                        var box = Ensure<BoxCollider>(r.gameObject); box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size;
                    }
                    else { var col = Ensure<MeshCollider>(r.gameObject); col.sharedMesh = mf.sharedMesh; }
                }
            }
            var accent = root.transform.Find("Platform accent");
            if (!accent) { accent = new GameObject("Platform accent").transform; accent.SetParent(root.transform); }
            var glow = Ensure<Light>(accent.gameObject);
            glow.type = LightType.Point; glow.color = new Color(.25f, .65f, .8f); glow.range = 4; glow.shadows = LightShadows.None;
            glow.transform.position = new Vector3(0, 1.05f, .6f); entrance.accents = new[] { glow }; entrance.SetAccents(0);
            Banners(root.transform, renderers);
            foreach (var screen in env.GetComponentsInChildren<RawImage>(true)) screen.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "IrisStudy.png");
            // The navigation mover is separate from authored gate/lighting/banner transforms.
            var mover = GameObject.Find("Lab Walker") ?? new GameObject("Lab Walker");
            var nav = Ensure<LabNavigation>(mover);
            nav.stage = stage; stage.navigation = nav;
            nav.body = Ensure<CharacterController>(mover);
            nav.body.height = 1.7f; nav.body.radius = .28f; nav.body.center = Vector3.up * .85f;
            nav.body.stepOffset = .2f; nav.body.slopeLimit = 40; nav.body.enabled = false;
            Canvas(stage.hud, nav);
            var chapter = stage.runner.chapters[0];
            var authored = HumanEyeContent.Chapters()[0]; chapter.steps = authored.steps;
            EditorUtility.SetDirty(chapter);
            foreach (var o in new Object[] { stage, stage.cams, stage.hud, entrance, nav }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CINEMATIC_PATCH_APPLIED");
        }

        static void Canvas(Hud hud, LabNavigation navigation)
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasAuthoring.PrefabPath);
            try
            {
                var all = prefab.GetComponentsInChildren<RectTransform>(true);
                var src = all.First(r => r.name == "btn-skip-intro");
                all.First(r => r.name == "action-row").GetComponent<LayoutElement>().preferredWidth = 720;
                var row = all.FirstOrDefault(r => r.name == "lab-tools");
                if (!row)
                {
                    row = new GameObject("lab-tools", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
                    row.SetParent(all.First(r => r.name == "bottom"), false);
                    row.GetComponent<LayoutElement>().preferredHeight = 48;
                    var group = row.GetComponent<HorizontalLayoutGroup>(); group.spacing = 10;
                    group.childAlignment = TextAnchor.MiddleCenter; group.childForceExpandWidth = false; group.childForceExpandHeight = true;
                }
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
                row.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
                foreach (var entry in new[] { ("btn-lab", "Explore lab"), ("btn-intro-replay", "Replay intro"), ("btn-sound", "Sound on"), ("btn-section", "Cross-section"), ("btn-shell", "Hide sclera") })
                {
                    var found = all.FirstOrDefault(r => r.name == entry.Item1);
                    var go = found ? found.gameObject : Object.Instantiate(src.gameObject, row);
                    {
                        go.transform.SetParent(row, false); go.name = entry.Item1;
                        var layout = go.GetComponent<LayoutElement>(); layout.minWidth = 120; layout.preferredWidth = 150; layout.flexibleWidth = 0;
                        layout.minHeight = layout.preferredHeight = 48;
                        go.GetComponentInChildren<Text>(true).text = entry.Item2; go.SetActive(false);
                    }
                }
                if (!all.Any(r => r.name == "lab-joystick"))
                {
                    var safe = all.First(r => r.name == "safe");
                    var pad = new GameObject("lab-joystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LabJoystick));
                    var rect = (RectTransform)pad.transform; rect.SetParent(safe, false);
                    rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = Vector2.one * .5f;
                    rect.anchoredPosition = new Vector2(100, -30); rect.sizeDelta = Vector2.one * 144;
                    var img = pad.GetComponent<Image>(); img.color = new Color(.035f, .12f, .17f, .85f);
                    img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                    var knob = new GameObject("thumb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var kr = (RectTransform)knob.transform; kr.SetParent(rect, false); kr.sizeDelta = Vector2.one * 52;
                    var ki = knob.GetComponent<Image>(); ki.sprite = img.sprite; ki.color = new Color(.3f, .7f, .8f); ki.raycastTarget = false;
                    pad.GetComponent<LabJoystick>().knob = kr; pad.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, CanvasAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var instances = hud.canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (string key in new[] { "btn-lab", "btn-intro-replay", "btn-sound", "btn-section", "btn-shell" })
                if (!hud.bindings.Any(b => b.key == key)) hud.bindings = hud.bindings.Append(new Hud.Binding { key = key, target = instances.First(r => r.name == key) }).ToArray();
            navigation.joystick = hud.canvas.GetComponentInChildren<LabJoystick>(true);
            navigation.joystickPanel = navigation.joystick.gameObject;
        }

        static void Banners(Transform root, Renderer[] renderers)
        {
            int i = 0;
            foreach (var old in renderers.Where(r => r.name == "images"))
            {
                old.enabled = false;
                string name = "Eye Banner " + i++;
                var existing = root.Find(name);
                var banner = existing ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
                banner.name = name; banner.transform.SetParent(root, false);
                var toward = new Vector3(0, old.bounds.center.y, .6f) - old.bounds.center;
                banner.transform.SetPositionAndRotation(old.bounds.center + toward.normalized * .03f, Quaternion.LookRotation(-toward));
                banner.transform.localScale = new Vector3(new Vector2(old.bounds.size.x, old.bounds.size.z).magnitude, old.bounds.size.y, 1);
                var collider = banner.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
                string study = i % 2 == 0 ? "LightStudy" : "IrisStudy";
                string path = Folder + study + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat)
                {
                    var texture = new Texture2D(1024, 384, TextureFormat.RGB24, false);
                    for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                    {
                        var p = new Vector2((x - 512) / 180f, (y - 192) / 180f);
                        float r = p.magnitude, angle = Mathf.Atan2(p.y, p.x);
                        var color = new Color(.018f, .04f, .055f);
                        if (r > .23f && r < .92f)
                        {
                            float fibers = .5f + .25f * Mathf.Sin(angle * 123 + r * 18) + .25f * Mathf.Sin(angle * 67 - r * 32);
                            color = Color.Lerp(new Color(.025f, .08f, .1f), new Color(.12f, .37f, .43f), fibers) * Mathf.Sin((r - .23f) / .69f * Mathf.PI);
                        }
                        if (Mathf.Abs(r - 1.01f) < .005f || (x % 64 == 0 || y % 64 == 0) && r > 1.1f) color = new Color(.045f, .12f, .15f);
                        if (study == "LightStudy")
                        {
                            color = new Color(.018f, .04f, .055f);
                            // Schematic parallel light rays converging behind a lens; decorative, not a retinal photograph.
                            float u = (x - 440) / 180f, v = (y - 192) / 180f;
                            if (Mathf.Abs(u * u / .04f + v * v / .8f - 1) < .09f) color = new Color(.08f, .3f, .4f);
                            foreach (float ray in new[] { -.55f, 0, .55f })
                                if (Mathf.Abs(v - (u < 0 ? ray : ray * (1 - u / 1.7f))) < .009f) color = new Color(.2f, .5f, .56f);
                        }
                        texture.SetPixel(x, y, color);
                    }
                    texture.Apply(); File.WriteAllBytes(Folder + study + ".png", texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(Folder + study + ".png");
                    mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + study + ".png"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                banner.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
        public static void Inspect()
        {
            EditorSceneManager.OpenScene("Assets/HumanEye/Scenes/HumanEye_Observatory.unity");
            var env = GameObject.Find("human brain env");
            var lines = env.GetComponentsInChildren<Renderer>(true).Select(r =>
                $"{r.name} | center {r.bounds.center.ToString("F3")} size {r.bounds.size.ToString("F3")} | materials {string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "NULL"))}");
            Directory.CreateDirectory("Logs/cinematic");
            File.WriteAllLines("Logs/cinematic/room.txt", lines);
            File.WriteAllLines("Logs/cinematic/lights.txt", Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(l => $"{l.name}: {l.type} {l.intensity} at {l.transform.position}"));
        }
    }
}

