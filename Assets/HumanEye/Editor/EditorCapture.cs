using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Batch visual check without a player build: opens the launch scene and enters Play mode; the -heAutopilot flag
    /// makes ModuleBoot add the AutoPilot, which renders camera frames to PNG (no UI overlay in batch mode) and exits.
    /// Unity.exe -batchmode -projectPath . -executeMethod HumanEye.EditorTools.EditorCapture.Run -heAutopilot -heChapter 0 -heStop 1 -heFast 4 -heCapture <dir>
    public static class EditorCapture
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            EditorApplication.EnterPlaymode();
        }

        /// Diagnostic renders of the scan subject's head, one anatomy layer at a time (edit mode, no Play).
        public static void Head()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var human = Object.FindFirstObjectByType<HumanDisplay>(FindObjectsInactive.Include);
            human.gameObject.SetActive(true);
            var eye = human.eyeRight.position;
            var cam = new GameObject("diag cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray; cam.fieldOfView = 30; cam.nearClipPlane = .01f;
            var light = new GameObject("diag light").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(30, 20, 0);
            var layers = new (string name, Renderer r)[] { ("skin", human.skin.main[0]), ("muscles", human.muscles.main[0]), ("skull", human.skull.main[0]), ("brain", human.brain.main[0]) };
            Directory.CreateDirectory("Builds/Checks/head");
            foreach (var (view, offset) in new[] { ("front", new Vector3(0, .02f, -.7f)), ("side", new Vector3(.7f, .02f, .02f)), ("body", new Vector3(0, -.6f, -3.2f)) })
                foreach (var show in new[] { "skin", "muscles", "skull", "brain", "skull+brain" })
                {
                    foreach (var l in layers) l.r.enabled = show.Contains(l.name);
                    cam.transform.position = eye + offset; cam.transform.LookAt(view == "body" ? eye + Vector3.down * .7f : eye + new Vector3(.03f, 0, .05f));
                    var rt = new RenderTexture(800, 800, 24); cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt; var tex = new Texture2D(800, 800, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); tex.Apply();
                    File.WriteAllBytes($"Builds/Checks/head/{view}_{show}.png", tex.EncodeToPNG());
                    RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
                }
            foreach (var r in human.GetComponentsInChildren<Renderer>(true)) Debug.Log($"HE_HEAD {r.name} bounds={r.bounds}");
        }

        /// Layout probe (edit mode): logs every root's world pose with its children, the hierarchy under the roots named in
        /// Logs/probe.txt (renderer bounds and materials), and the height profile of the meshes named "profile: a b" (top
        /// of the mesh per 0.25 m ring around the world Y axis), so the module can be fitted to a new room.
        public static void Probe()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var lines = File.ReadAllLines("Logs/probe.txt");
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Debug.Log($"HE_ROOT {go.name} active={go.activeSelf} pos={go.transform.position} rot={go.transform.eulerAngles} scale={go.transform.lossyScale}");
                foreach (Transform c in go.transform) Debug.Log($"HE_CHILD {go.name}/{c.name} active={c.gameObject.activeSelf} pos={c.position} fwd={c.forward} scale={c.lossyScale}");
            }
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"HE_LIGHT {Path(l.transform)} active={l.isActiveAndEnabled} type={l.type} mode={l.lightmapBakeType} I={l.intensity:0.###} col={l.color} range={l.range:0.#} angle={l.spotAngle:0} shadows={l.shadows} pos={l.transform.position} fwd={l.transform.forward}");
            foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"HE_REFL {Path(p.transform)} active={p.isActiveAndEnabled} mode={p.mode} box={p.boxProjection} pos={p.transform.position} size={p.size} I={p.intensity}");
            foreach (var g in Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"HE_PROBES {Path(g.transform)} count={g.probePositions.Length}");
            foreach (var v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"HE_VOLUME {Path(v.transform)} active={v.isActiveAndEnabled} global={v.isGlobal} weight={v.weight} priority={v.priority} profile={(v.sharedProfile ? v.sharedProfile.name + " [" + string.Join(",", v.sharedProfile.components.Select(c => c.GetType().Name + (c.active ? "" : "(off)"))) + "]" : "none")}");
            Debug.Log($"HE_RENDER skybox={(RenderSettings.skybox ? RenderSettings.skybox.name + "/" + RenderSettings.skybox.shader.name : "none")} ambientMode={RenderSettings.ambientMode} ambientI={RenderSettings.ambientIntensity} sky={RenderSettings.ambientSkyColor} eq={RenderSettings.ambientEquatorColor} ground={RenderSettings.ambientGroundColor} reflI={RenderSettings.reflectionIntensity} fog={RenderSettings.fog} sun={(RenderSettings.sun ? RenderSettings.sun.name : "none")}");
            var lm = LightmapSettings.lightmaps; Debug.Log($"HE_LIGHTMAPS count={lm.Length} first={(lm.Length > 0 && lm[0].lightmapColor ? lm[0].lightmapColor.name + " " + lm[0].lightmapColor.width : "-")}");
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var line in lines.Where(l => l.StartsWith("tree:")))
                foreach (var root in all.Where(t => t.name == line.Substring(5).Trim()))
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        var r = t.GetComponent<Renderer>();
                        var mats = r ? string.Join(",", r.sharedMaterials.Select(m => m ? m.name + "/" + m.shader.name : "null")) : "";
                        Debug.Log($"HE_TREE {Path(t)} active={t.gameObject.activeInHierarchy} pos={t.position} static={t.gameObject.isStatic}" + (r ? $" bounds={r.bounds} mats={mats} lmIndex={r.lightmapIndex}" : ""));
                    }
            foreach (var line in lines.Where(l => l.StartsWith("profile:")))
                foreach (var name in line.Substring(8).Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
                    foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => m.name == name && m.sharedMesh))
                    {
                        var top = new float[72]; var low = new float[72];
                        for (int i = 0; i < top.Length; i++) { top[i] = float.NegativeInfinity; low[i] = float.PositiveInfinity; }
                        foreach (var v in mf.sharedMesh.vertices)
                        {
                            var w = mf.transform.TransformPoint(v);
                            int ring = Mathf.FloorToInt(new Vector2(w.x, w.z).magnitude / .25f);
                            if (ring < top.Length) { top[ring] = Mathf.Max(top[ring], w.y); low[ring] = Mathf.Min(low[ring], w.y); }
                        }
                        Debug.Log($"HE_PROFILE {name}: " + string.Join(" ", top.Select((y, i) => float.IsInfinity(y) ? null : $"{i * .25f:0.00}:{low[i]:0.00}/{y:0.00}").Where(x => x != null)));
                    }
        }

        static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;

        /// Camera blocking without Play: renders each line of Logs/views.txt ("name px py pz lx ly lz fov") from the saved
        /// scene with its own lights and sky to Builds/Checks/views/<name>.png (960x540). Other lines: "bounds: a b c" logs the
        /// bounds of renderers whose names contain a word; "hide: A|B" / "show: A|B" switch objects off/on by exact name;
        /// "shot: name S0x_Shot" renders from a named lesson shot with its own lens.
        public static void Views()
        {
            EditorSceneManager.OpenScene(Builder.ScenePath);
            var human = Object.FindFirstObjectByType<HumanDisplay>(FindObjectsInactive.Include); human.gameObject.SetActive(true);
            var cam = new GameObject("views cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = Color.black; cam.nearClipPlane = .01f; cam.farClipPlane = 200;
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).renderPostProcessing = true;   // the lab grade, as in Play
            Directory.CreateDirectory("Builds/Checks/views");
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var line in File.ReadAllLines("Logs/views.txt"))
            {
                if (line.StartsWith("bounds:"))
                {
                    var words = line.Substring(7).Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (words.Any(w => r.name.Contains(w))) Debug.Log($"HE_BOUNDS {r.name} active={r.gameObject.activeInHierarchy} bounds={r.bounds}");
                    continue;
                }
                if (line.StartsWith("texstats:"))   // HDR statistics and an LDR preview of an imported texture (Reinhard, 1/99.5th percentile)
                {
                    var path = line.Substring(9).Trim(); var src = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if (!src) { Debug.LogWarning("HE_TEXSTATS missing " + path); continue; }
                    var hdr = new RenderTexture(1024, 512, 0, RenderTextureFormat.ARGBFloat); Graphics.Blit(src, hdr);
                    RenderTexture.active = hdr; var read = new Texture2D(1024, 512, TextureFormat.RGBAFloat, false); read.ReadPixels(new Rect(0, 0, 1024, 512), 0, 0); read.Apply();
                    var px = read.GetPixels(); var lum = px.Select(c => .2126f * c.r + .7152f * c.g + .0722f * c.b).OrderBy(x => x).ToArray();
                    float P(float q) => lum[Mathf.Clamp((int)(q * lum.Length), 0, lum.Length - 1)];
                    var rows = Enumerable.Range(0, 8).Select(k => px.Skip(k * 64 * 1024).Take(64 * 1024).Average(c => .2126f * c.r + .7152f * c.g + .0722f * c.b));
                    Debug.Log($"HE_TEXSTATS {path} mean={lum.Average():0.####} p50={P(.5f):0.####} p90={P(.9f):0.####} p99={P(.99f):0.####} p999={P(.999f):0.###} max={lum[^1]:0.##} rows(bottom→top)={string.Join(",", rows.Select(x => x.ToString("0.####")))}");
                    float ex = 1 / Mathf.Max(1e-6f, P(.995f));
                    var ldr = new Texture2D(1024, 512, TextureFormat.RGB24, false);
                    ldr.SetPixels(px.Select(c => { var v = c * ex; return new Color(Mathf.Pow(v.r / (1 + v.r), 1 / 2.2f), Mathf.Pow(v.g / (1 + v.g), 1 / 2.2f), Mathf.Pow(v.b / (1 + v.b), 1 / 2.2f)); }).ToArray());
                    File.WriteAllBytes("Builds/Checks/views/texstats_" + System.IO.Path.GetFileNameWithoutExtension(path) + ".png", ldr.EncodeToPNG());
                    RenderTexture.active = null; Object.DestroyImmediate(hdr); Object.DestroyImmediate(read); Object.DestroyImmediate(ldr);
                    continue;
                }
                if (line.StartsWith("debug:"))
                {
                    foreach (var t in all.Where(t => t && t.name == line.Substring(6).Trim()))
                    {
                        var r = t.GetComponent<Renderer>(); var m = r ? r.sharedMaterial : null;
                        Debug.Log($"HE_DEBUG {Path(t)} active={t.gameObject.activeInHierarchy} renderer={(r ? r.enabled.ToString() : "none")} bounds={(r ? r.bounds.ToString() : "-")} mat={(m ? m.name : "none")} shader={(m ? m.shader.name : "-")} supported={(m && m.shader.isSupported)} passes={(m ? m.passCount : 0)} queue={(m ? m.renderQueue : 0)} layer={t.gameObject.layer}");
                    }
                    continue;
                }
                if (line.StartsWith("hide:") || line.StartsWith("show:"))
                {
                    var names = line.Substring(5).Split('|').Select(n => n.Trim()).ToArray();
                    foreach (var t in all.Where(t => t && names.Contains(t.name))) t.gameObject.SetActive(line.StartsWith("show:"));
                    continue;
                }
                var f = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (line.StartsWith("shot:") && f.Length >= 3)
                {
                    var shot = all.FirstOrDefault(t => t && t.name == f[2]);
                    var cm = shot ? shot.GetComponent<Unity.Cinemachine.CinemachineCamera>() : null;
                    if (!cm) { Debug.LogWarning("HE_VIEWS no shot " + f[2]); continue; }
                    cam.transform.SetPositionAndRotation(shot.position, shot.rotation); cam.fieldOfView = cm.Lens.FieldOfView;
                    f = new[] { f[1] };
                }
                else
                {
                    if (f.Length < 8) continue;
                    float F(int i) => float.Parse(f[i], System.Globalization.CultureInfo.InvariantCulture);
                    cam.transform.position = new Vector3(F(1), F(2), F(3)); cam.transform.LookAt(new Vector3(F(4), F(5), F(6))); cam.fieldOfView = F(7);
                }
                var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
                File.WriteAllBytes($"Builds/Checks/views/{f[0]}.png", tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            }
        }
    }
}
