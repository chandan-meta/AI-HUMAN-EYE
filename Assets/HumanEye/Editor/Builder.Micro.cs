using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        static Transform microRoot;

        static Mesh Box(string name, Vector3 size)
        {
            var m = new Mesh(); var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            var h = size * .5f;
            foreach (var axis in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = Mathf.Abs(axis.y) > .5f ? Vector3.right : Vector3.up; var w = Vector3.Cross(axis, u);
                int b = v.Count;
                foreach (var (a, c) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                    v.Add(Vector3.Scale(axis + u * a + w * c, h));
                for (int k = 0; k < 4; k++) n.Add(axis);
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            // orient each face outward
            var tri = m.triangles; var vert = m.vertices;
            for (int i = 0; i < tri.Length; i += 3)
                if (Vector3.Dot(Vector3.Cross(vert[tri[i + 1]] - vert[tri[i]], vert[tri[i + 2]] - vert[tri[i]]), n[tri[i]]) < 0) (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]);
            m.triangles = tri; m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        // Cell layout (micro-local metres): two tidy rows, a cone every third cell, rods tall and slender, cones short and
        // broad, so the two types read at a glance. Light arrives from the top (vitreous side); signals leave to the right.
        const int Columns = 10;
        const float CellPitch = .76f, RowZ = .55f;
        static bool IsCone(int column, int row) => (column + row * 2) % 3 == 1;
        static Vector3 CellPos(int column, int row) => new(-3.4f + column * CellPitch + (row == 1 ? CellPitch * .5f : 0), 0, (row - .5f) * 2 * RowZ);

        /// Replaces the saved magnified retina with the current design and re-aims its shots; nothing else in the scene changes.
        public static void RebuildMicro(Stage s)
        {
            stage = s; cams = s.cams;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var old = s.micro ? s.micro.transform : null;
            var shotNames = new[] { "S08_MicroWide", "S08_Rods", "S08_Cones", "S08_Signals", "S08_PullBack" };
            var shots = cams.shots.Where(c => shotNames.Contains(c.name)).ToList();
            foreach (var shot in shots) shot.transform.SetParent(null, true);
            s.signals.paths = s.signals.paths.Where(p => !p.id.StartsWith("micro")).ToArray();
            if (old) Object.DestroyImmediate(old.gameObject);
            BuildMicro();
            var poses = new Dictionary<string, (Vector3 pos, Vector3 look, float fov)>
            {
                ["S08_MicroWide"] = (new Vector3(.4f, 2.4f, -7.8f), new Vector3(.4f, 1.6f, 0), 42),
                ["S08_Rods"] = (new Vector3(-1.9f, 1.35f, -3.3f), new Vector3(-1.9f, .85f, 0), 30),
                ["S08_Cones"] = (new Vector3(-.36f, 1.2f, -3.3f), new Vector3(-.36f, .72f, 0), 30),
                ["S08_Signals"] = (new Vector3(2.2f, 3.0f, -4.9f), new Vector3(2.2f, 2.2f, 0), 38),
                ["S08_PullBack"] = (new Vector3(1.0f, 2.3f, -10.8f), new Vector3(1.0f, 1.4f, 0), 48),
            };
            foreach (var shot in shots)
            {
                shot.transform.SetParent(microRoot, false);
                var (pos, look, fov) = poses[shot.name];
                shot.transform.localPosition = pos;
                shot.transform.rotation = Quaternion.LookRotation(microRoot.TransformPoint(look) - shot.transform.position, Vector3.up);
                var lens = shot.Lens; lens.FieldOfView = fov; shot.Lens = lens;
                EditorUtility.SetDirty(shot);
            }
            EditorUtility.SetDirty(s); EditorUtility.SetDirty(s.signals);
        }

        /// Outer segment: the stack of membrane discs that hold the photopigment, read as fine ridges along a capsule that
        /// may taper (a cone narrows toward the pigment layer). Lathe +Z from 0 to length; r0 at the base, r1 at the tip.
        static Mesh Ridged(string name, float r0, float r1, float length, int discs)
        {
            float cap0 = Mathf.Min(r0, .04f), cap1 = Mathf.Min(r1, .04f);
            var pts = new List<Vector2> { new(0, 0), new(r0 * .8f, cap0 * .4f) };
            float y0 = cap0, y1 = length - cap1, step = (y1 - y0) / discs;
            for (int i = 0; i <= discs; i++)
            {
                float y = y0 + step * i, r = Mathf.Lerp(r0, r1, i / (float)discs);
                pts.Add(new Vector2(r, y));
                if (i < discs) pts.Add(new Vector2(r * .86f, y + step * .5f));
            }
            pts.Add(new Vector2(r1 * .8f, length - cap1 * .4f)); pts.Add(new Vector2(0, length));
            return SaveMesh(Lathe.Revolve(new List<Vector2[]> { pts.ToArray() }, 0, 360, 20), name);
        }

        static void BuildMicro()
        {
            microRoot = new GameObject("Retina (magnified, Scene 8)").transform;
            microRoot.position = MicroPosition;
            var field = microRoot.gameObject.AddComponent<PhotoreceptorField>();
            // Bright, organised microscope stage: a light backdrop and layered base instead of a dark red cavern.
            var back = new List<Vector2[]> { Lathe.Arc(0, 12, 0, 180, 24) };
            var sphere = Lathe.Revolve(back, 0, 360, 32); Flip(sphere);
            Obj("Backdrop", microRoot, SaveMesh(sphere, "MicroBackdrop"), Lit("MicroBackdrop", new Color(.3f, .34f, .45f), .12f, emission: new Color(.06f, .08f, .13f)), new Vector3(.5f, 1.5f, 0));
            Obj("Pigment layer", microRoot, Box("PigmentLayer", new Vector3(9.4f, .14f, 3.0f)), Lit("Pigment", new Color(.34f, .2f, .14f), .3f), new Vector3(.3f, .06f, 0));
            Obj("Choroid (beneath)", microRoot, Box("MicroChoroid", new Vector3(9.4f, .5f, 3.0f)), Lit("MicroChoroid", new Color(.52f, .17f, .15f), .25f), new Vector3(.3f, -.26f, 0));

            Material Cell(string name, Color colour, Color rim) => Anatomy(name, colour, m =>
            {
                m.SetFloat("_Wrap", .45f); m.SetFloat("_Gloss", .4f); m.SetFloat("_Specular", .18f); m.SetFloat("_Rim", .3f); m.SetColor("_RimColor", rim);
            });
            var rodOuter = Cell("Rod_Outer", new Color(.55f, .47f, .9f), new Color(.8f, .75f, 1f));
            var rodInner = Cell("Rod_Inner", new Color(.68f, .62f, .9f), new Color(.8f, .75f, 1f));
            var coneOuter = Cell("Cone_Outer", new Color(1f, .68f, .36f), new Color(1f, .85f, .6f));
            // the three cone classes in the colours they sense best (long, medium, short wavelength), as on the art boards
            var coneOuters = new[] { Cell("Cone_Outer_L", new Color(.95f, .27f, .22f), new Color(1f, .7f, .6f)), Cell("Cone_Outer_M", new Color(.3f, .82f, .38f), new Color(.7f, 1f, .75f)), Cell("Cone_Outer_S", new Color(.28f, .47f, 1f), new Color(.7f, .8f, 1f)) };
            var coneInner = Cell("Cone_Inner", new Color(1f, .8f, .5f), new Color(1f, .85f, .6f));
            var neuron = Cell("Neuron", new Color(.6f, .76f, .94f), new Color(.7f, .9f, 1f));
            var ganglion = Cell("Ganglion", new Color(.95f, .88f, .6f), new Color(1f, .95f, .7f));
            var fibre = Anatomy("NerveFibre", new Color(.96f, .86f, .58f), m => { m.SetFloat("_Wrap", .6f); m.SetFloat("_Flow", 1); m.SetFloat("_Rim", .25f); });

            var rodOuterMesh = Ridged("RodOuter", .085f, .085f, .78f, 18); var rodInnerMesh = Ellipsoid("RodInner", .08f, .2f, 20);
            var coneOuterMesh = Ridged("ConeOuter", .035f, .15f, .52f, 9);   // narrow toward the pigment layer, as a real cone outer segment
            // synaptic terminals: the rod spherule, and the cone pedicle whose flat face meets the bipolar dendrites
            var spherule = Ellipsoid("RodSpherule", .065f, .06f, 14);
            var pedicle = SaveMesh(Lathe.Revolve(new List<Vector2[]> { Lathe.Line(new Vector2(0, 0), new Vector2(.05f, 0)), Lathe.Line(new Vector2(.05f, 0), new Vector2(.13f, .12f)), Lathe.Line(new Vector2(.13f, .12f), new Vector2(0, .12f)) }, 0, 360, 20), "ConePedicle");
            var coneInnerMesh = Ellipsoid("ConeInner", .13f, .2f, 20);
            var nucleus = Ellipsoid("Nucleus", .09f, .09f, 16); var coneNucleus = Ellipsoid("ConeNucleus", .1f, .1f, 16);
            var axonMesh = SaveMesh(Lathe.Tube(new List<Vector3> { new(0, 0, 0), new(0, .55f, 0) }, .02f, 8), "PhotoreceptorAxon");
            var rods = NewPart(microRoot, "rods", "Rods", 3, false, 1, 0); var cones = NewPart(microRoot, "cones", "Cones", 3, false, 1, 0);
            List<Renderer> rodR = new(), coneR = new(), rodTips = new(), coneTips = new(); List<Collider> rodPick = new(), conePick = new();
            var cells = new List<(Vector3 tip, bool cone)>();
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < Columns; i++)
                {
                    bool cone = IsCone(i, row);
                    var cell = Obj(cone ? "Cone" : "Rod", (cone ? cones : rods).transform, pos: CellPos(i, row));
                    Renderer R(string name, Mesh mesh, Material mat, float y, Quaternion rot) => Obj(name, cell.transform, mesh, mat, new Vector3(0, y, 0), rot).GetComponent<Renderer>();
                    Renderer tipR;
                    if (cone)
                    {
                        tipR = R("Outer segment", coneOuterMesh, coneOuters[coneTips.Count % 3], .12f, Upright);
                        coneR.Add(tipR); coneTips.Add(tipR);
                        coneR.Add(R("Inner segment", coneInnerMesh, coneInner, .86f, Upright));
                        coneR.Add(R("Nucleus", coneNucleus, coneInner, 1.26f, Upright));
                        coneR.Add(R("Axon", axonMesh, coneInner, 1.36f, Quaternion.identity));
                        coneR.Add(R("Synaptic terminal", pedicle, coneInner, 1.9f, Upright));
                    }
                    else
                    {
                        tipR = R("Outer segment", rodOuterMesh, rodOuter, .12f, Upright);
                        rodR.Add(tipR); rodTips.Add(tipR);
                        rodR.Add(R("Inner segment", rodInnerMesh, rodInner, 1.1f, Upright));
                        rodR.Add(R("Nucleus", nucleus, rodInner, 1.45f, Upright));
                        rodR.Add(R("Axon", axonMesh, rodInner, 1.5f, Quaternion.identity));
                        rodR.Add(R("Synaptic terminal", spherule, rodInner, 2.07f, Upright));
                    }
                    var col = Obj("pick", cell.transform, pos: new Vector3(0, .95f, 0), layer: PickLayer).AddComponent<CapsuleCollider>();
                    col.height = 1.9f; col.radius = .22f;
                    (cone ? conePick : rodPick).Add(col);
                    cells.Add((cell.transform.position + new Vector3(0, .45f, 0), cone));
                }
            rods.main = rodR.ToArray(); cones.main = coneR.ToArray(); rods.pick = rodPick.ToArray(); cones.pick = conePick.ToArray();
            rods.focus = Obj("Focus", rods.transform, pos: new Vector3(-1.9f, .85f, 0)).transform; rods.labelAnchor = Obj("Label", rods.transform, pos: new Vector3(-3.2f, 1.75f, -1.1f)).transform;
            cones.focus = Obj("Focus", cones.transform, pos: new Vector3(-.36f, .72f, 0)).transform; cones.labelAnchor = Obj("Label", cones.transform, pos: new Vector3(.05f, 1.9f, -1.2f)).transform;

            // Bipolar and ganglion cells; ganglion axons form the nerve-fibre layer that leaves as the optic nerve.
            var bip = Ellipsoid("Bipolar", .1f, .13f, 16); var gang = Ellipsoid("GanglionCell", .16f, .16f, 16);
            var cellLayer = Obj("Retinal neurons", microRoot).transform;
            var bipolars = new List<Vector3>(); var ganglions = new List<Vector3>();
            for (int i = 0; i < 6; i++) { var p = new Vector3(-2.9f + i * 1.05f, 2.2f, 0); bipolars.Add(p); Obj("Bipolar cell", cellLayer, bip, neuron, p, Upright); }
            for (int i = 0; i < 4; i++) { var p = new Vector3(-2.5f + i * 1.5f, 2.85f, 0); ganglions.Add(p); Obj("Ganglion cell", cellLayer, gang, ganglion, p, Upright); }
            var exit = new Vector3(3.9f, 3.15f, 0);
            var nerveLocal = new List<Vector3> { exit, new(4.1f, 1.9f, 0), new(4.3f, .5f, 0), new(4.45f, -1.3f, 0) };
            Obj("Optic nerve (toward the brain)", microRoot, SaveMesh(Lathe.Tube(nerveLocal, .3f, 16), "MicroNerve"), fibre);
            foreach (var g in ganglions)
                Obj("Fibre", cellLayer, SaveMesh(Lathe.Tube(new List<Vector3> { g, g + Vector3.up * .3f, new(g.x + .3f, 3.15f, 0), exit }, .03f, 6), "Fibre" + Mathf.RoundToInt(g.x * 10)), fibre);
            foreach (var b in bipolars)
                Obj("Bipolar process", cellLayer, SaveMesh(Lathe.Tube(new List<Vector3> { b + Vector3.down * .32f, b + Vector3.up * .32f }, .02f, 6), "BipolarProcess"), neuron);

            // Light arriving from the vitreous side (top), through the transparent inner layers, to the outer segments.
            var lightMat = Glow("MicroLight", new Color(1, .9f, .62f, 1), ProceduralTextures.Streak(), 2, new Vector2(-1.5f, 0));
            var incoming = new List<LineRenderer>();
            foreach (var (tip, _) in cells.Where((c, i) => i % 4 == 0))
            {
                var l = Obj("Incoming light", microRoot).AddComponent<LineRenderer>();
                l.sharedMaterial = lightMat; l.useWorldSpace = true; l.textureMode = LineTextureMode.Tile; l.widthMultiplier = .07f;
                l.positionCount = 2; l.SetPosition(0, tip + Vector3.up * 4.0f); l.SetPosition(1, tip); l.shadowCastingMode = ShadowCastingMode.Off;
                incoming.Add(l);
            }

            // Signal paths: photoreceptor → bipolar → ganglion → fibre layer → optic nerve (micro-local coordinates).
            var paths = new List<SignalPulses.Path>();
            int k = 0;
            foreach (var (tip, cone) in cells.Where((c, i) => i % 3 == 1).Take(9))
            {
                var t = microRoot.InverseTransformPoint(tip);
                var b = bipolars.OrderBy(p => Mathf.Abs(p.x - t.x)).First(); var g = ganglions.OrderBy(p => Mathf.Abs(p.x - t.x)).First();
                paths.Add(new SignalPulses.Path { id = "micro" + k++, space = microRoot, points = new[] { t, new Vector3(t.x, 1.9f, t.z), b + Vector3.down * .2f, b, b + Vector3.up * .25f, g, g + Vector3.up * .3f, new Vector3(g.x + .3f, 3.15f, 0), exit, nerveLocal[1], nerveLocal[2], nerveLocal[3] } });
            }
            stage.signals.paths = stage.signals.paths.Concat(paths).ToArray();

            // Three-point lighting keeps every cell readable (the cone explanation happens in bright light by design).
            Light L(string name, Vector3 pos, float intensity, float range, Color colour)
            {
                var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(microRoot, false); l.transform.localPosition = pos;
                l.type = LightType.Point; l.intensity = intensity; l.range = range; l.color = colour; l.shadows = LightShadows.None; return l;
            }
            L("Micro Key", new Vector3(0, 4.5f, -3), 7, 14, new Color(1, .96f, .9f));
            L("Micro Fill", new Vector3(-4.5f, 2.5f, -3.5f), 3.5f, 12, new Color(.6f, .78f, 1f));
            L("Micro Rim", new Vector3(4, 3, 3), 3, 10, new Color(.9f, .8f, 1f));

            field.rods = rods; field.cones = cones; field.rodTips = rodTips.ToArray(); field.coneTips = coneTips.ToArray();
            field.incoming = incoming.ToArray(); field.signals = stage.signals; field.signalPaths = paths.Select(p => p.id).ToArray();
            field.extraLabels = new[]
            {
                Obj("Light (from the vitreous side)", microRoot, pos: new Vector3(-1.6f, 4.3f, -.7f)).transform,
                Obj("Electrical signals", microRoot, pos: new Vector3(1.4f, 3.55f, -.7f)).transform,
                Obj("Toward the optic nerve", microRoot, pos: new Vector3(4.7f, .2f, -.5f)).transform,
                // one rod (column 5, front row) carries the part labels: outer segment, inner segment, nucleus, terminal
                Obj("Outer segment (discs)", microRoot, pos: new Vector3(.75f, .5f, -.85f)).transform,
                Obj("Inner segment", microRoot, pos: new Vector3(.75f, 1.1f, -.85f)).transform,
                Obj("Nucleus", microRoot, pos: new Vector3(.75f, 1.45f, -.85f)).transform,
                Obj("Synaptic terminal", microRoot, pos: new Vector3(.75f, 2.07f, -.85f)).transform,
                Obj("Pigment epithelium", microRoot, pos: new Vector3(-2.4f, .08f, -1.5f)).transform,
            };
            stage.micro = field;
            microRoot.gameObject.SetActive(false);
        }
    }
}
