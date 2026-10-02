using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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

        static void BuildMicro()
        {
            microRoot = new GameObject("Retina (magnified, Scene 8)").transform;
            microRoot.position = MicroPosition;
            var field = microRoot.gameObject.AddComponent<PhotoreceptorField>();
            var back = new List<Vector2[]> { Lathe.Arc(0, 11, 0, 180, 24) };
            var sphere = Lathe.Revolve(back, 0, 360, 32); Flip(sphere);
            Obj("Backdrop", microRoot, SaveMesh(sphere, "MicroBackdrop"), Lit("MicroBackdrop", new Color(.06f, .018f, .022f), .1f), new Vector3(.5f, 1.5f, 0));
            Obj("Pigment layer", microRoot, Box("PigmentLayer", new Vector3(8.6f, .14f, 2.8f)), Lit("Pigment", new Color(.2f, .08f, .045f), .3f), new Vector3(.3f, .07f, 0));
            Obj("Choroid (beneath)", microRoot, Box("MicroChoroid", new Vector3(8.6f, .5f, 2.8f)), Lit("MicroChoroid", new Color(.38f, .05f, .06f), .25f), new Vector3(.3f, -.25f, 0));

            var rodOuter = Anatomy("Rod_Outer", new Color(.58f, .48f, .9f), m => { m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", .3f); });
            var rodInner = Anatomy("Rod_Inner", new Color(.75f, .7f, .92f), m => m.SetFloat("_Wrap", .5f));
            var coneOuter = Anatomy("Cone_Outer", new Color(1f, .72f, .42f), m => { m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", .3f); });
            var coneInner = Anatomy("Cone_Inner", new Color(1f, .86f, .7f), m => m.SetFloat("_Wrap", .5f));
            var neuron = Anatomy("Neuron", new Color(.62f, .76f, .92f), m => { m.SetFloat("_Wrap", .6f); m.SetFloat("_Rim", .3f); });
            var ganglion = Anatomy("Ganglion", new Color(.95f, .86f, .58f), m => m.SetFloat("_Wrap", .6f));
            var fibre = Anatomy("NerveFibre", new Color(.96f, .86f, .58f), m => { m.SetFloat("_Wrap", .6f); m.SetFloat("_Flow", 1); });

            var rodOuterMesh = Capsule("RodOuter", .07f, .62f); var rodInnerMesh = Capsule("RodInner", .055f, .36f);
            var coneProfile = new List<Vector2[]> { Lathe.Line(new Vector2(0, 0), new Vector2(.1f, .32f), 6), Lathe.Line(new Vector2(.1f, .32f), new Vector2(0, .32f)) };
            var coneOuterMesh = SaveMesh(Lathe.Revolve(coneProfile, 0, 360, 20), "ConeOuter");
            var nucleus = Ellipsoid("Nucleus", .085f, .085f, 16); var inner = Ellipsoid("ConeInner", .1f, .13f, 16);
            var rods = NewPart(microRoot, "rods", "Rods", 3, false, 1, 0); var cones = NewPart(microRoot, "cones", "Cones", 3, false, 1, 0);
            List<Renderer> rodR = new(), coneR = new(), rodTips = new(), coneTips = new(); List<Collider> rodPick = new(), conePick = new();
            var cells = new List<(Vector3 tip, bool cone)>();
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 14; i++)
                {
                    float x = -3.0f + i * .45f + (row == 1 ? .2f : 0), z = (row - 1) * .62f;
                    bool cone = (i + row) % 3 == 1;
                    var cell = Obj(cone ? "Cone" : "Rod", (cone ? cones : rods).transform, pos: new Vector3(x, 0, z));
                    Renderer tipR;
                    if (cone)
                    {
                        tipR = Obj("Outer segment", cell.transform, coneOuterMesh, coneOuter, new Vector3(0, .2f, 0), Upright).GetComponent<Renderer>();
                        coneR.Add(tipR); coneTips.Add(tipR);
                        coneR.Add(Obj("Inner segment", cell.transform, inner, coneInner, new Vector3(0, .64f, 0), Upright).GetComponent<Renderer>());
                        coneR.Add(Obj("Nucleus", cell.transform, nucleus, coneInner, new Vector3(0, 1.0f, 0), Upright).GetComponent<Renderer>());
                    }
                    else
                    {
                        tipR = Obj("Outer segment", cell.transform, rodOuterMesh, rodOuter, new Vector3(0, .14f, 0), Quaternion.Euler(-90, 0, 0)).GetComponent<Renderer>();
                        rodR.Add(tipR); rodTips.Add(tipR);
                        rodR.Add(Obj("Inner segment", cell.transform, rodInnerMesh, rodInner, new Vector3(0, .76f, 0), Quaternion.Euler(-90, 0, 0)).GetComponent<Renderer>());
                        rodR.Add(Obj("Nucleus", cell.transform, nucleus, rodInner, new Vector3(0, 1.22f, 0), Upright).GetComponent<Renderer>());
                    }
                    var axon = Obj("Axon", cell.transform, SaveMesh(Lathe.Tube(new List<Vector3> { new(0, 1.05f, 0), new(0, 1.45f, 0) }, .018f, 6), "PhotoreceptorAxon"), cone ? coneInner : rodInner);
                    (cone ? coneR : rodR).Add(axon.GetComponent<Renderer>());
                    var col = Obj("pick", cell.transform, pos: new Vector3(0, .7f, 0), layer: PickLayer).AddComponent<CapsuleCollider>();
                    col.height = 1.5f; col.radius = .2f;
                    (cone ? conePick : rodPick).Add(col);
                    cells.Add((cell.transform.position + new Vector3(0, .42f, 0), cone));
                }
            rods.main = rodR.ToArray(); cones.main = coneR.ToArray(); rods.pick = rodPick.ToArray(); cones.pick = conePick.ToArray();
            rods.focus = Obj("Focus", rods.transform, pos: new Vector3(-1.65f, .5f, -.62f)).transform; rods.labelAnchor = Obj("Label", rods.transform, pos: new Vector3(-3.9f, .9f, -1.0f)).transform;
            cones.focus = Obj("Focus", cones.transform, pos: new Vector3(.9f, .4f, -.62f)).transform; cones.labelAnchor = Obj("Label", cones.transform, pos: new Vector3(.6f, 2.05f, -1.4f)).transform;

            // Bipolar and ganglion cells; ganglion axons form the nerve-fibre layer that leaves as the optic nerve.
            var bip = Ellipsoid("Bipolar", .1f, .12f, 16); var gang = Ellipsoid("GanglionCell", .15f, .15f, 16);
            var cellLayer = Obj("Retinal neurons", microRoot).transform;
            var bipolars = new List<Vector3>(); var ganglions = new List<Vector3>();
            for (int i = 0; i < 8; i++) { var p = new Vector3(-2.9f + i * .82f, 1.75f, 0); bipolars.Add(p); Obj("Bipolar cell", cellLayer, bip, neuron, p, Upright); }
            for (int i = 0; i < 5; i++) { var p = new Vector3(-2.6f + i * 1.35f, 2.35f, 0); ganglions.Add(p); Obj("Ganglion cell", cellLayer, gang, ganglion, p, Upright); }
            var exit = new Vector3(3.75f, 2.72f, 0);
            var nerveLocal = new List<Vector3> { exit, new(3.95f, 1.6f, 0), new(4.15f, .4f, 0), new(4.3f, -1.4f, 0) };
            Obj("Optic nerve (toward the brain)", microRoot, SaveMesh(Lathe.Tube(nerveLocal, .3f, 16), "MicroNerve"), fibre);
            foreach (var g in ganglions)
                Obj("Fibre", cellLayer, SaveMesh(Lathe.Tube(new List<Vector3> { g, g + Vector3.up * .32f, new(g.x + .3f, 2.72f, 0), exit }, .03f, 6), "Fibre" + Mathf.RoundToInt(g.x * 10)), fibre);
            foreach (var b in bipolars)
                Obj("Bipolar process", cellLayer, SaveMesh(Lathe.Tube(new List<Vector3> { b + Vector3.down * .3f, b + Vector3.up * .3f }, .02f, 6), "BipolarProcess"), neuron);

            // Light arriving from the vitreous side (top), through the transparent inner layers, to the outer segments.
            var lightMat = Glow("MicroLight", new Color(1, .9f, .62f, 1), ProceduralTextures.Streak(), 2, new Vector2(-1.5f, 0));
            var incoming = new List<LineRenderer>();
            foreach (var (tip, _) in cells.Where((c, i) => i % 5 == 0))
            {
                var l = Obj("Incoming light", microRoot).AddComponent<LineRenderer>();
                l.sharedMaterial = lightMat; l.useWorldSpace = true; l.textureMode = LineTextureMode.Tile; l.widthMultiplier = .07f;
                l.positionCount = 2; l.SetPosition(0, tip + Vector3.up * 3.8f); l.SetPosition(1, tip); l.shadowCastingMode = ShadowCastingMode.Off;
                incoming.Add(l);
            }

            // Signal paths: photoreceptor → bipolar → ganglion → fibre layer → optic nerve (micro-local coordinates).
            var paths = new List<SignalPulses.Path>();
            int k = 0;
            foreach (var (tip, cone) in cells.Where((c, i) => i % 4 == 1).Take(9))
            {
                var t = microRoot.InverseTransformPoint(tip);
                var b = bipolars.OrderBy(p => Mathf.Abs(p.x - t.x)).First(); var g = ganglions.OrderBy(p => Mathf.Abs(p.x - t.x)).First();
                paths.Add(new SignalPulses.Path { id = "micro" + k++, space = microRoot, points = new[] { t, new Vector3(t.x, 1.45f, t.z), b + Vector3.down * .2f, b, b + Vector3.up * .25f, g, g + Vector3.up * .32f, new Vector3(g.x + .3f, 2.72f, 0), exit, nerveLocal[1], nerveLocal[2], nerveLocal[3] } });
            }
            stage.signals.paths = stage.signals.paths.Concat(paths).ToArray();

            var warm = new GameObject("Micro Light").AddComponent<Light>(); warm.transform.SetParent(microRoot, false);
            warm.transform.localPosition = new Vector3(0, 4, -3); warm.type = LightType.Point; warm.intensity = 5; warm.range = 12; warm.color = new Color(1, .92f, .85f);
            var fill = new GameObject("Micro Fill").AddComponent<Light>(); fill.transform.SetParent(microRoot, false);
            fill.transform.localPosition = new Vector3(-4, 2, -3); fill.type = LightType.Point; fill.intensity = 2.5f; fill.range = 10; fill.color = new Color(.4f, .7f, 1f);

            field.rods = rods; field.cones = cones; field.rodTips = rodTips.ToArray(); field.coneTips = coneTips.ToArray();
            field.incoming = incoming.ToArray(); field.signals = stage.signals; field.signalPaths = paths.Select(p => p.id).ToArray();
            field.extraLabels = new[]
            {
                Obj("Light (from the vitreous side)", microRoot, pos: new Vector3(-1.2f, 4.1f, -.7f)).transform,
                Obj("Electrical signals", microRoot, pos: new Vector3(1.6f, 3.15f, -.7f)).transform,
                Obj("Toward the optic nerve", microRoot, pos: new Vector3(4.7f, .2f, -.5f)).transform,
            };
            stage.micro = field;
            microRoot.gameObject.SetActive(false);
        }
    }
}
