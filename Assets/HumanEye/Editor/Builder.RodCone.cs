using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// The owner's rod and cone (models/rod cone .blend, 16 October 2026) replace the procedural photoreceptors of the
    /// magnified retina (Scene 8). Tools/HumanEye/export_rod_cone.py exports each cell as its disc stack (<Cell>_Outer) and the
    /// rest (<Cell>_Body), with marks on the four labelled parts. Additive and idempotent: inside each existing Rod/Cone cell
    /// the old pieces give way to the model, turned so the outer segment rests on the pigment layer and the terminal reaches
    /// toward the bipolar cells (light from the vitreous side, above), as before. The parts, their picks, the light response
    /// (the disc stacks), shots, signals and labels keep working. Rods keep their supplied purple map; the cones' disc stacks
    /// take the three cone classes' colours (long, medium, short wavelength, as on the boards) over a grey copy of theirs.
    public static partial class Builder
    {
        const string RodConePath = Root + "ThirdParty/UserModels/RodCone/";
        const float CellBase = .12f;   // micro-local metres: the outer-segment tips on the pigment layer, as the old cells

        class CellModel { public Mesh outer, body; public float height, width; public Dictionary<string, float> marks = new(); }

        [MenuItem("Tools/Human Eye/Authoring/Apply rod and cone models (keeps scene edits)", priority = 21)]
        public static void ApplyRodConeModels()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            anatomyShader = Shader.Find("HumanEye/Anatomy");
            var field = stage.micro;
            if (!field || !field.rods || !field.cones) throw new InvalidOperationException("The magnified retina is missing");
            foreach (var n in new[] { "RodCell", "ConeCell" }) ModelSettings(RodConePath + n + ".fbx");
            TextureSizes(RodConePath);
            var rod = CellFrom("Rod", 2f); var cone = CellFrom("Cone", 1.85f);   // rods tall and slender, cones shorter and broad

            Texture Map(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(RodConePath + file) is { } t ? t : throw new InvalidOperationException("Missing " + file);
            Material Cell(string name, Texture map, Color tint, Color rim) => Anatomy(name, tint, m =>
            {
                m.SetTexture("_BaseMap", map); m.SetFloat("_Wrap", .45f); m.SetFloat("_Gloss", .45f); m.SetFloat("_Specular", .2f);
                m.SetFloat("_Rim", .35f); m.SetColor("_RimColor", rim);   // the glassy membrane of the .blend, as a rim
            });
            var rodMat = Cell("RodCell", Map("Rod_Albedo.jpg"), Color.white, new Color(.8f, .75f, 1f));
            var coneMat = Cell("ConeCell", Map("Cone_Albedo.jpg"), Color.white, new Color(1f, .85f, .6f));
            var grey = Map("Cone_AlbedoGrey.jpg");
            var classes = new[]
            {
                Cell("ConeCell_Outer_L", grey, new Color(1f, .45f, .38f), new Color(1f, .7f, .6f)),
                Cell("ConeCell_Outer_M", grey, new Color(.45f, 1f, .52f), new Color(.7f, 1f, .75f)),
                Cell("ConeCell_Outer_S", grey, new Color(.45f, .62f, 1f), new Color(.7f, .8f, 1f)),
            };

            // Turned over about x: the outer segment down onto the pigment layer, the terminal up, and the cell's open front
            // (mitochondria, nucleus; -Y in the .blend, +z once imported) toward the shots, which look along +z. A small fixed
            // yaw per cell keeps the rows from reading as copies.
            List<Renderer> rodR = new(), coneR = new(), rodTips = new(), coneTips = new();
            int coneCount = 0, cellCount = 0;
            foreach (var (part, model, isCone) in new[] { (field.rods, rod, false), (field.cones, cone, true) })
                foreach (Transform cell in part.transform)
                {
                    if (cell.name != (isCone ? "Cone" : "Rod")) continue;   // the part's Focus and Label anchors
                    foreach (var child in cell.Cast<Transform>().Where(c => c.name != "pick").ToArray()) Object.DestroyImmediate(child.gameObject);
                    var turn = Quaternion.Euler(0, cellCount++ * 37 % 41 - 20, 0) * Quaternion.Euler(180, 0, 0);
                    var at = new Vector3(0, CellBase + model.height, 0);
                    var tip = Obj("Outer segment", cell, model.outer, isCone ? classes[coneCount++ % 3] : rodMat, at, turn).GetComponent<Renderer>();
                    var body = Obj("Cell body", cell, model.body, isCone ? coneMat : rodMat, at, turn).GetComponent<Renderer>();
                    (isCone ? coneR : rodR).AddRange(new[] { tip, body }); (isCone ? coneTips : rodTips).Add(tip);
                    var pick = cell.Find("pick");
                    if (pick && pick.GetComponent<CapsuleCollider>() is { } capsule)
                    {
                        pick.localPosition = new Vector3(0, CellBase + model.height * .5f, 0);
                        capsule.center = Vector3.zero; capsule.height = model.height + .1f; capsule.radius = Mathf.Max(.22f, model.width * .5f);
                    }
                }
            field.rods.main = rodR.ToArray(); field.cones.main = coneR.ToArray();
            field.rodTips = rodTips.ToArray(); field.coneTips = coneTips.ToArray();

            // The four part labels sit on the right flank of the rod in column 5 of the front row (as before, at its new heights)
            var micro = field.transform; var labelled = micro.TransformPoint(CellPos(5, 0));
            var rodCell = field.rods.transform.Cast<Transform>().Where(c => c.name == "Rod").OrderBy(c => (c.position - labelled).sqrMagnitude).First();
            foreach (var (label, mark) in new[] { ("Outer segment (discs)", "Mark_OuterSegment"), ("Inner segment", "Mark_InnerSegment"), ("Nucleus", "Mark_Nucleus"), ("Synaptic terminal", "Mark_SynapticTerminal") })
            {
                var anchor = field.extraLabels.FirstOrDefault(t => t && t.name == label);
                if (!anchor) throw new InvalidOperationException("Missing label anchor " + label);
                float y = rod.marks[mark];   // model height of the part (scaled), measured from the terminal feet
                anchor.position = rodCell.TransformPoint(new Vector3(RadiusAt(rod, y) + .03f, CellBase + rod.height - y, 0));
                EditorUtility.SetDirty(anchor);
            }
            foreach (var o in new Object[] { field, field.rods, field.cones }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            int Tris(Mesh m) => (int)(m.GetIndexCount(0) / 3);
            Debug.Log($"HUMAN_EYE_RODCONE_APPLIED rods={rodTips.Count} cones={coneTips.Count} rodHeight={rod.height:F2} coneHeight={cone.height:F2} " +
                      $"trianglesPerRod={Tris(rod.outer) + Tris(rod.body)} trianglesPerCone={Tris(cone.outer) + Tris(cone.body)}");
        }

        /// One exported cell, its two meshes baked into the cell's own frame (y up, feet at 0) and scaled to `height`.
        static CellModel CellFrom(string name, float height)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(RodConePath + name + "Cell.fbx");
            if (!root) throw new InvalidOperationException("Run Tools/HumanEye/export_rod_cone.py first");
            Matrix4x4 Frame(Transform t) => root.transform.worldToLocalMatrix * t.localToWorldMatrix;
            var nodes = root.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name);
            var c = new CellModel();
            Mesh Baked(string node, float scale)
            {
                var f = nodes[name + "_" + node]; var m = Matrix4x4.Scale(Vector3.one * scale) * Frame(f.transform);
                var source = f.sharedMesh; var mesh = Object.Instantiate(source);
                mesh.vertices = source.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                mesh.normals = source.normals.Select(n => m.MultiplyVector(n).normalized).ToArray();
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                return SaveMesh(mesh, name + "Cell_" + node);
            }
            float top = new[] { "Outer", "Body" }.Max(n => { var f = nodes[name + "_" + n]; var m = Frame(f.transform); return f.sharedMesh.vertices.Max(v => m.MultiplyPoint3x4(v).y); });
            float s = height / top;
            c.outer = Baked("Outer", s); c.body = Baked("Body", s); c.height = height;
            var all = c.outer.vertices.Concat(c.body.vertices).ToArray();
            c.width = 2 * all.Max(v => new Vector2(v.x, v.z).magnitude);
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Mark_")))
                c.marks[t.name] = Frame(t).GetColumn(3).y * s;
            return c;
        }

        /// The cell's radius around its axis at a height (its widest vertex within 1.5 cm of it).
        static float RadiusAt(CellModel c, float y) =>
            c.outer.vertices.Concat(c.body.vertices).Where(v => Mathf.Abs(v.y - y) < .015f).Select(v => new Vector2(v.x, v.z).magnitude).DefaultIfEmpty(.1f).Max();
    }
}
