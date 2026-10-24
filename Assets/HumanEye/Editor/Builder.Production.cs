using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using G = HumanEye.EyeGeometry;

namespace HumanEye.EditorTools
{
    /// Production upgrade (7 October 2026), idempotent and save-guarded:
    /// • the owner-supplied cross-section eye (models/eye/eye model/half eye.fbx, baked by Tools/HumanEye/export_local_eye.py)
    ///   becomes the visible eye on both eyes: its kept half and section caps stay, a mirrored copy is the slide-away half;
    ///   the clear media (cornea, aqueous, vitreous), picks and the optic nerve are rebuilt on its measured landmarks;
    /// • the Human package's Rocketbox person (Tools/HumanEye/export_rocketbox_display.py) replaces the scan subject.
    /// Batch: -executeMethod HumanEye.EditorTools.Builder.ApplyProduction
    public static partial class Builder
    {
        const string LocalEye = Root + "ThirdParty/LocalEye/";
        static readonly string[] DetailParts = { "sclera", "choroid", "retina", "iris", "lens", "optic_nerve" };

        [Serializable] class LocalEyeFile { public LocalEyeMesh[] meshes; }
        [Serializable] class LocalEyeMesh { public string part, layer, material; public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles; }
        class Detail { public string part, material; public Mesh keep, cap, mirror; }

        [MenuItem("Tools/Human Eye/Authoring/Apply production eye + human (keeps scene edits)", priority = 7)]
        public static void ApplyProduction()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            hud = stage.hud; cams = stage.cams; narrator = stage.narrator; runner = stage.runner;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");

            var mats = LocalEyeMaterials();
            var details = LocalEyeMeshes();
            ClearMedia();
            foreach (var eye in new[] { stage.eye, stage.otherEye }) DetailEye(eye, details, mats);
            Anchors(stage.eye);
            Nerve();
            ClearGlass();
            EyeShots();
            if (File.Exists(UM + "Rocketbox/Rocketbox_Display.fbx")) HumanRocketbox();   // stays where the old figure stood
            FaceShot();
            BuildData();   // content: the scan is framed on head and shoulders

            EditorUtility.SetDirty(stage);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_PRODUCTION_APPLIED detail meshes {details.Count}, triangles {details.Sum(d => (d.keep.triangles.Length + (d.cap ? d.cap.triangles.Length : 0)) / 3)} per half");
        }

        // ---------------- the detailed eye ----------------
        static Texture2D NormalTex(string path)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new Exception("Missing texture " + path);
            if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Dictionary<string, Material> LocalEyeMaterials()
        {
            Material M(string name, Color c, string baseMap, string bump, float bumpScale, Action<Material> setup)
            {
                return Anatomy("LocalEye_" + name, c, m =>
                {
                    if (baseMap != null) m.SetTexture("_BaseMap", Tex(LocalEye + baseMap));
                    if (bump != null) { m.SetTexture("_BumpMap", NormalTex(LocalEye + bump)); m.SetFloat("_BumpScale", bumpScale); }
                    m.SetFloat("_RevealUV", 0); m.SetFloat("_Cull", 0); m.SetFloat("_F0", .03f);
                    setup(m);
                });
            }
            return new Dictionary<string, Material>
            {
                // warm biological white, fine veins from the texture, a wet tear film over a matt tissue response
                ["eye_outer"] = M("ScleraOuter", Color.white, "Eye_Diff_Sclera.jpg", "Eye_NM.jpg", .55f, m => { m.SetFloat("_Gloss", .55f); m.SetFloat("_Specular", .18f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", .03f); m.SetFloat("_Wet", .5f); }),
                ["eye_inner"] = M("ScleraInner", new Color(.95f, .9f, .88f), "Eye_inner_Diff.jpg", "Eye_NM.jpg", .4f, m => { m.SetFloat("_Gloss", .4f); m.SetFloat("_Specular", .12f); m.SetFloat("_Wrap", .55f); m.SetFloat("_Rim", 0); m.SetFloat("_Wet", .15f); m.SetFloat("_InteriorDark", 1); }),
                ["muscle"] = M("Muscle", new Color(.92f, .82f, .8f), "Muscle_Diff.jpg", "Muscle_NM.jpg", .9f, m => { m.SetFloat("_Gloss", .45f); m.SetFloat("_Specular", .2f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", .04f); m.SetFloat("_Wet", .25f); }),
                ["choroid"] = M("Choroid", new Color(.9f, .55f, .52f), "Choroid_Dif.jpg", "Choroid_NMM.jpg", .7f, m => { m.SetFloat("_Gloss", .45f); m.SetFloat("_Specular", .2f); m.SetFloat("_Wrap", .55f); m.SetFloat("_Rim", 0); m.SetFloat("_Wet", .3f); m.SetFloat("_InteriorDark", 1); }),
                ["retina"] = M("Retina", new Color(1f, .66f, .5f), "Retina_Diff.jpg", "Retina_NM.jpg", .45f, m => { m.SetFloat("_Gloss", .45f); m.SetFloat("_Specular", .18f); m.SetFloat("_Wrap", .6f); m.SetFloat("_Rim", 0); m.SetFloat("_Wet", .35f); m.SetFloat("_InteriorDark", 1); }),
                ["vessel_red"] = M("ArteryRed", new Color(.62f, .07f, .05f), null, null, 0, m => { m.SetFloat("_Gloss", .6f); m.SetFloat("_Specular", .3f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", 0); m.SetFloat("_Wet", .4f); m.SetFloat("_InteriorDark", 1); }),
                ["vessel_blue"] = M("VeinBlue", new Color(.16f, .22f, .5f), null, null, 0, m => { m.SetFloat("_Gloss", .6f); m.SetFloat("_Specular", .3f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", 0); m.SetFloat("_Wet", .4f); m.SetFloat("_InteriorDark", 1); }),
                // the iris keeps its photographic fibres; its inner edge follows the live pupil (shader remap)
                ["iris"] = M("Iris", Color.white, "Eye_Diff.jpg", "Eye_NM.jpg", 1f, m => { m.SetFloat("_Gloss", .4f); m.SetFloat("_Specular", .12f); m.SetFloat("_Wrap", .6f); m.SetFloat("_Rim", 0); m.SetFloat("_IrisRoot", G.IrisRoot); m.SetFloat("_PupilBase", .176f); m.SetFloat("_PupilNow", G.PupilDefault); }),
                ["lens"] = M("Lens", new Color(.62f, .66f, .68f), null, null, 0, m => { m.SetFloat("_Gloss", .97f); m.SetFloat("_Specular", .6f); m.SetFloat("_Wrap", .3f); m.SetFloat("_Rim", .22f); m.SetFloat("_F0", .04f); m.SetFloat("_Wet", .4f); m.SetFloat("_InteriorDark", 1); m.SetFloat("_HighlightTint", .05f); }),
                ["ligament"] = M("Zonule", new Color(.9f, .86f, .78f), null, null, 0, m => { m.SetFloat("_Gloss", .35f); m.SetFloat("_Specular", .12f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", 0); m.SetFloat("_InteriorDark", 1); }),
            };
        }

        /// Per (structure, material): the kept half without its cut-plane faces, the cut-plane faces (shown only while the
        /// eye is open) and the mirror image of the kept half (the removable half; its cut faces are never visible).
        static List<Detail> LocalEyeMeshes()
        {
            using var zip = new System.IO.Compression.GZipStream(File.OpenRead("Tools/HumanEye/LocalEye.json.gz"), System.IO.Compression.CompressionMode.Decompress);
            var file = JsonUtility.FromJson<LocalEyeFile>(new StreamReader(zip).ReadToEnd());
            var list = new List<Detail>();
            foreach (var g in file.meshes.Where(m => m.layer == "section"))
            {
                var body = new List<int>(); var cap = new List<int>();
                for (int i = 0; i < g.triangles.Length; i += 3)
                {
                    bool onCut = Mathf.Abs(g.vertices[g.triangles[i]].x) < .002f && Mathf.Abs(g.vertices[g.triangles[i + 1]].x) < .002f && Mathf.Abs(g.vertices[g.triangles[i + 2]].x) < .002f;
                    (onCut ? cap : body).Add(i);
                }
                string name = "LocalEye_" + g.part + "_" + g.material;
                list.Add(new Detail
                {
                    part = g.part, material = g.material,
                    keep = DetailMesh(g, body, false, name + "_Keep"),
                    cap = cap.Count > 0 ? DetailMesh(g, cap, false, name + "_Cap") : null,
                    mirror = DetailMesh(g, body, true, name + "_Mirror"),
                });
            }
            return list;
        }

        static Mesh DetailMesh(LocalEyeMesh g, List<int> tris, bool mirror, string name)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            var weld = new Dictionary<(Vector3, Vector3, Vector2), int>();   // the export is one vertex per corner
            var flip = new Vector3(mirror ? -1 : 1, 1, 1);
            foreach (int i in tris)
                for (int k = 0; k < 3; k++)
                {
                    int s = g.triangles[i + (mirror ? 2 - k : k)];   // a mirror image needs the opposite winding
                    var key = (Vector3.Scale(g.vertices[s], flip), Vector3.Scale(g.normals[s], flip), g.uv[s]);
                    if (!weld.TryGetValue(key, out int at)) { weld[key] = at = v.Count; v.Add(key.Item1); n.Add(key.Item2); uv.Add(key.Item3); }
                    t.Add(at);
                }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds(); m.RecalculateTangents();
            return SaveMesh(m, name);
        }

        /// Cornea, aqueous and vitreous stay procedural (clear media) but follow the measured landmarks; picks and the lens
        /// pivot move with them. The saved mesh assets are upserted in place, so every reference updates.
        static void ClearMedia()
        {
            foreach (var (id, profile) in new[] { ("cornea", G.CorneaProfile()), ("aqueous", G.AqueousProfile()), ("vitreous", G.VitreousProfile()) })
            {
                SaveMesh(Lathe.Revolve(profile, G.KeepFrom, G.KeepTo, 40), id + "_Keep");
                SaveMesh(Lathe.Revolve(profile, G.CutFrom, G.CutTo, 40), id + "_Cut");
                SaveMesh(Lathe.Caps(profile, G.KeepFrom, G.KeepTo), id + "_Caps");
                SaveMesh(Lathe.Revolve(profile, 0, 360, 20), id + "_PickClosed");
                SaveMesh(Lathe.Caps(profile, G.KeepFrom, G.KeepTo), id + "_PickCut");
            }
            SaveMesh(Lathe.Revolve(G.LensProfile(), 0, 360, 20), "lens_PickClosed");
            SaveMesh(Lathe.Caps(G.LensProfile(), G.KeepFrom, G.KeepTo), "lens_PickCut");
        }

        static void DetailEye(EyeModel eye, List<Detail> details, Dictionary<string, Material> mats)
        {
            eye.lens.localPosition = eye.lensCut.localPosition = new Vector3(0, 0, G.LensZ);
            eye.pupil.localPosition = eye.pupilCut.localPosition = new Vector3(0, 0, G.IrisZ + .014f);
            foreach (var t in eye.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Detail ·") || t.name.StartsWith("Detail cap ·")).ToArray())
                Object.DestroyImmediate(t.gameObject);
            foreach (var p in eye.parts)
            {
                foreach (var r in p.exterior.Where(r => r)) r.gameObject.SetActive(false);   // the earlier closed-eye surface (MisterH)
                p.exterior = new Renderer[0];
                EditorUtility.SetDirty(p);
            }
            var caps = eye.caps.Where(c => c).ToList();
            foreach (var id in DetailParts)
            {
                var part = eye.Get(id);
                var keepTube = id == "optic_nerve" ? part.main.Where(r => r && r.name == "Nerve").ToList() : new List<Renderer>();
                foreach (var r in part.main.Concat(part.removable).Where(r => r && !keepTube.Contains(r))) { r.gameObject.SetActive(false); caps.Remove(r); }
                if (id == "retina") { var disc = part.transform.Find("Optic disc"); if (disc) disc.gameObject.SetActive(false); }
                bool lens = id == "lens";
                var keepParent = lens ? eye.lens : part.transform; var cutParent = lens ? eye.lensCut : eye.removableRoot;
                var offset = lens ? new Vector3(0, 0, -G.LensZ) : Vector3.zero;
                var main = new List<Renderer>(keepTube); var removable = new List<Renderer>();
                foreach (var d in details.Where(d => d.part == id))
                {
                    var mat = mats[d.material];
                    main.Add(Detailed("Detail · " + id + " · " + d.material, keepParent, d.keep, mat, offset));
                    if (d.cap) { var c = Detailed("Detail cap · " + id + " · " + d.material, keepParent, d.cap, mat, offset); main.Add(c); caps.Add(c); c.enabled = eye.IsCut; }
                    removable.Add(Detailed("Detail · " + id + " · " + d.material + " (removable)", cutParent, d.mirror, mat, offset));
                }
                part.main = main.ToArray(); part.removable = removable.ToArray();
                EditorUtility.SetDirty(part);
            }
            eye.caps = caps.Distinct().ToArray();
            EditorUtility.SetDirty(eye);
        }

        static Renderer Detailed(string name, Transform parent, Mesh mesh, Material mat, Vector3 offset)
        {
            var r = Obj(name, parent, mesh, mat, pos: offset).GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            return r;
        }

        /// Focus and label anchors of the structures whose landmarks moved with the measured anatomy.
        static void Anchors(EyeModel eye)
        {
            void Set(string id, string anchor, Vector3 at) { var t = eye.Get(id).transform.Find(anchor); if (t) t.localPosition = at; }
            Set("lens", "Focus", new Vector3(0, .12f, G.LensZ)); Set("lens", "Focus (cut)", new Vector3(-.02f, -.12f, G.LensZ)); Set("lens", "Label (cut)", new Vector3(-.1f, -.95f, G.LensZ));
            Set("iris", "Focus", new Vector3(-.3f, -.1f, G.IrisZ + .01f)); Set("iris", "Focus (cut)", new Vector3(-.02f, .3f, G.IrisZ));
            Set("pupil", "Focus", new Vector3(0, 0, G.IrisZ + .015f)); Set("pupil", "Focus (cut)", new Vector3(-.02f, 0, G.IrisZ));
            Set("cornea", "Focus", new Vector3(.02f, .3f, 1.06f)); Set("cornea", "Focus (cut)", new Vector3(-.02f, .3f, 1.05f));
            Set("aqueous", "Focus", new Vector3(0, .18f, 1.0f)); Set("aqueous", "Focus (cut)", new Vector3(-.02f, .18f, 1.0f));
        }

        static void FaceShot()
        {
            var h = stage.human.transform;   // the runtime figure is at scale 1 (HumanDisplay.ResetVisual); author the preview the same way
            Vector3 Eye(Transform socket) => h.position + h.rotation * socket.localPosition;
            WowPatch.Shot(stage, "S02_Face", Vector3.zero, Vector3.forward, 30);
            var face = cams.shots.First(s => s.name == "S02_Face");
            Stage.AimFace(face.transform, Eye(stage.human.eyeLeft), Eye(stage.human.eyeRight));
            EditorUtility.SetDirty(face.transform);
        }

        /// Clear media show crisp highlights over a dark body, so the pupil reads black and the cornea reads as a wet dome.
        static void ClearGlass()
        {
            foreach (var (name, body) in new[] { ("Eye_Cornea", new Color(.05f, .06f, .07f)), ("Eye_Aqueous", new Color(.35f, .55f, .65f)), ("Eye_Vitreous", new Color(.6f, .7f, .74f)), ("Eye_Lens", new Color(.42f, .55f, .62f)), ("LocalEye_Lens", new Color(.5f, .55f, .58f)) })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/" + name + ".mat"); if (!m) continue;
                m.SetFloat("_Glass", 1); m.SetColor("_BaseColor", body); EditorUtility.SetDirty(m);
            }
        }

        /// Close-ups that framed the earlier (deeper, larger) lens and the earlier iris plane, re-aimed in the eye's own frame.
        static void EyeShots()
        {
            void Local(string name, Vector3 pos, Vector3 look, float fov)
            {
                var s = cams.shots.First(x => x.name == name);
                s.transform.localPosition = pos; s.transform.localRotation = Quaternion.LookRotation(look - pos, Vector3.up);
                var lens = s.Lens; lens.FieldOfView = fov; s.Lens = lens; EditorUtility.SetDirty(s); EditorUtility.SetDirty(s.transform);
            }
            float iz = G.IrisZ + .01f, lz = G.LensZ;
            Local("S02_Part_iris", new Vector3(.3f, .18f, 2.8f), new Vector3(0, 0, iz), 24);
            Local("S02_Part_pupil", new Vector3(.12f, .06f, 2.65f), new Vector3(0, 0, iz), 20);
            Local("S03_Part_aqueous", new Vector3(-1.35f, .22f, 1.1f), new Vector3(0, .1f, 1.0f), 25);
            Local("S03_Part_lens", new Vector3(-2.7f, .12f, lz), new Vector3(0, 0, lz), 30);
            Local("S03_Lens_Close", new Vector3(-2.15f, 0, lz + .02f), new Vector3(0, -.08f, lz), 24);
            Local("S05_Cornea", new Vector3(-1.6f, .25f, 1.2f), new Vector3(0, 0, 1.02f), 26);
            Local("S05_AqueousPupil", new Vector3(-1.2f, .12f, 1.05f), new Vector3(0, 0, .95f), 23);
            Local("S05_PupilFront", new Vector3(-1.35f, .32f, 2.85f), new Vector3(0, 0, iz), 30);
            Local("S06_LensClose", new Vector3(-2.6f, -.04f, lz - .05f), new Vector3(0, -.2f, lz - .12f), 30);
        }

        /// The tube continues the modelled stub to the brain (same end point), with its signal path; calm tissue colour,
        /// no permanent flow stripes: the moving pulses show the signal.
        static void Nerve()
        {
            var sig = stage.signals.paths.First(p => p.id == "nerve"); int n = sig.points.Length;
            var cut = stage.cutPoint;
            var end = cut.InverseTransformPoint(sig.points[n - 4]);   // the existing nerve end (the brain was placed to meet it)
            var path = G.NervePath(end);
            sig.points = path.Select(cut.TransformPoint).Concat(sig.points.Skip(n - 3)).ToArray();
            SaveMesh(Lathe.Tube(path, .135f, 18, null, .85f), "optic_nerve");
            SaveMesh(Lathe.Tube(path, .135f, 18, null, .85f), "optic_nerve_");
            SaveMesh(Lathe.Tube(path, .2f, 8), "optic_nerve_Pick");
            var nerve = stage.eye.Get("optic_nerve");
            nerve.focus.localPosition = path[14]; nerve.labelAnchor.localPosition = path[14] + new Vector3(0, .7f, 0);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Eye_OpticNerve.mat");
            mat.SetColor("_BaseColor", new Color(.93f, .84f, .74f)); mat.SetFloat("_Flow", 0); mat.SetFloat("_Gloss", .4f); mat.SetFloat("_Specular", .18f);
            mat.SetFloat("_Rim", .12f); mat.SetColor("_RimColor", new Color(1, .85f, .7f));
            // the modelled stub (with its central vessels) shows through the cut; the tube takes over where the stub ends
            float total = 0, stub = 0;
            for (int i = 1; i < path.Count; i++) { total += Vector3.Distance(path[i], path[i - 1]); if (i == 12) stub = total; }
            mat.SetFloat("_RevealFrom", stub / total - .004f);
            EditorUtility.SetDirty(mat); EditorUtility.SetDirty(stage.signals);
        }

        // ---------------- the Human package person ----------------
        /// Same layering as the earlier figure: skin (body + head, translucent in the scan), garment (trousers, opaque),
        /// a muscle shell, the hollow Z-Anatomy skull as the X-ray layer, the brain; the anatomical eyes sit in the sockets.
        static HumanDisplay HumanRocketbox()
        {
            var path = UM + "Rocketbox/Rocketbox_Display.fbx";
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && (mi.importAnimation || mi.importCameras || mi.importLights || mi.animationType != ModelImporterAnimationType.None))
            {
                mi.importAnimation = mi.importCameras = mi.importLights = false; mi.animationType = ModelImporterAnimationType.None; mi.SaveAndReimport();
            }
            var old = stage.human;
            var human = new GameObject("Human Display").transform;
            var hd = human.gameObject.AddComponent<HumanDisplay>();
            var temp = new GameObject("temp-import").transform;
            var rb = Instantiate(path, temp);
            Renderer[] R(GameObject g) => g.GetComponentsInChildren<Renderer>(true);
            Vector3 Socket(string name) => Child(rb.transform, name).position;
            var b = WorldBounds(R(rb)); var mid = (Socket("EyeSocket_L") + Socket("EyeSocket_R")) * .5f;
            temp.rotation = Quaternion.FromToRotation(new Vector3(mid.x - b.center.x, 0, mid.z - b.center.z).normalized, Vector3.back);
            var across = Socket("EyeSocket_L") - Socket("EyeSocket_R"); across.y = 0;
            temp.rotation = Quaternion.FromToRotation(across.normalized, Vector3.right) * temp.rotation;
            b = WorldBounds(R(rb)); temp.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 eyeL = Socket("EyeSocket_L"), eyeR = Socket("EyeSocket_R");

            Material Skin(string slot)
            {
                string k = slot.Contains("head") ? "head" : "body";
                return Anatomy("Rocketbox_" + k, Color.white, m =>
                {
                    m.SetTexture("_BaseMap", Tex(UM + $"Rocketbox/m025_{k}_color.png"));
                    m.SetTexture("_BumpMap", NormalTex(UM + $"Rocketbox/m025_{k}_normal.png")); m.SetFloat("_BumpScale", .9f);
                    m.SetTexture("_SpecMap", Tex(UM + $"Rocketbox/m025_{k}_specular.png"));
                    m.SetFloat("_Wrap", k == "head" ? .5f : .4f); m.SetFloat("_Gloss", k == "head" ? .45f : .35f); m.SetFloat("_Specular", k == "head" ? .3f : .2f);
                    m.SetFloat("_Rim", .12f); m.SetColor("_RimColor", new Color(.55f, .75f, .95f)); m.SetFloat("_F0", .03f);
                });
            }
            foreach (var r in R(rb)) r.sharedMaterials = r.sharedMaterials.Select(m => Skin(m ? m.name : "body")).ToArray();
            var body = R(rb).First(r => r.name == "Body"); var trousers = R(rb).First(r => r.name == "Trousers");
            rb.transform.SetParent(human, true);
            hd.skin = NewPart(human, "skin", "Skin", 0, false, 1, 4); hd.skin.main = new[] { body };
            hd.garment = NewPart(human, "garment", "Trousers", 0, false, 1, 5); hd.garment.main = new[] { trousers };

            var muscleMat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Human_Muscle.mat");
            var muscle = Combine("Muscles", new[] { body }, human, muscleMat, "Human_Muscles");
            var mm = muscle.GetComponent<MeshFilter>().sharedMesh; var mv = mm.vertices; var mn = mm.normals;
            for (int i = 0; i < mv.Length; i++) mv[i] -= mn[i] * .004f;
            mm.vertices = mv; mm.RecalculateBounds();
            hd.muscles = NewPart(human, "muscles", "Muscles", 0, false, 1, 3); hd.muscles.main = new[] { muscle.GetComponent<Renderer>() };

            // X-ray layer: the hollow Z-Anatomy skull fitted to the eyes (a body skeleton from another figure would not fit).
            var (skull, brain, visual) = PlaceZAnatomy(temp, eyeL, eyeR, Vector3.back);
            var boneMat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Human_Skull.mat");
            hd.skull = NewPart(human, "skull", "Skull", 0, false, 1, 2); hd.skull.main = new[] { Combine("Skull", new[] { skull }, human, boneMat, "Human_Skull").GetComponent<Renderer>() };
            hd.brain = NewPart(human, "brain_layer", "Brain", 0, false, 1, 1);
            hd.brain.main = new[] { Combine("Brain", new[] { brain, visual }, human, AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Brain.mat"), "Human_Brain").GetComponent<Renderer>() };

            // The anatomical eye's cornea apex is 1.126 radii from its centre: put it where the removed eye cap's front was.
            float eyeRadius = float.Parse(File.ReadAllText(UM + "Rocketbox/eye_radius.txt").Trim(), CultureInfo.InvariantCulture) / 1.126f;
            hd.eyeLeft = Obj("Socket (other eye)", human, pos: human.InverseTransformPoint(eyeL), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.eyeRight = Obj("Socket (selected eye)", human, pos: human.InverseTransformPoint(eyeR), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.headTop = WorldBounds(R(rb)).max.y; hd.headY = eyeL.y;
            var scan = Obj("Scan Wave", human, Disc("ScanDisc", .9f, 64), Glow("ScanWave", new Color(.2f, .7f, 1f, .55f), ProceduralTextures.Soft(), 2.2f), new Vector3(0, 1, 0), Quaternion.Euler(90, 0, 0));
            Obj("Scan Ring", scan.transform, Ring("ScanRing", .9f, .012f, 64), Glow("ScanRingGlow", new Color(.35f, .9f, 1f, 1), null, 3), rot: Quaternion.Euler(-90, 0, 0));
            hd.scanPlane = scan.transform; hd.scanRenderer = scan.GetComponent<Renderer>();
            foreach (var r in human.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            Object.DestroyImmediate(temp.gameObject);
            // same place in the room as the figure it replaces (shots were authored around that spot)
            if (old)
            {
                human.SetParent(old.transform.parent, false);
                human.SetPositionAndRotation(old.transform.position, old.transform.rotation);
                human.gameObject.SetActive(old.gameObject.activeSelf);
                Object.DestroyImmediate(old.gameObject);
            }
            stage.human = hd;
            Debug.Log($"HE_HUMAN Rocketbox eyes {hd.eyeLeft.position} / {hd.eyeRight.position} radius {eyeRadius:0.0000} head top {hd.headTop:0.00}");
            return hd;
        }
    }
}
