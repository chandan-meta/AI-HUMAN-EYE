using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using G = HumanEye.EyeGeometry;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        static EyeModel heroEye;
        static Transform cutFrame;
        public static readonly Vector3 BrainCentre = new(2.4f, 1.72f, 1.15f);
        public static Vector3 BrainEntry = new(1.95f, 1.36f, 1.12f);   // recomputed by BuildBrain from the placed brain

        class PartSpec
        {
            public string id, label; public Material mat; public List<Vector2[]> profile, cutZone;
            public int priority, queue; public bool seeThrough; public float baseAlpha = 1;
            public Vector3 focus, labelAt, focusCut, labelCut;
        }

        static Vector2 P(float radius, float phi) => new(radius * Mathf.Sin(phi * Mathf.Deg2Rad), radius * Mathf.Cos(phi * Mathf.Deg2Rad));

        static List<Vector2[]> Band(float outer, float inner, float frontPhi) => new()
        {
            Lathe.Arc(0, outer, 180, frontPhi, 36), Lathe.Line(P(outer, frontPhi), P(inner, frontPhi)), Lathe.Arc(0, inner, frontPhi, 180, 36),
        };

        static List<Vector2[]> Shift(List<Vector2[]> profile, float dz) => profile.Select(c => c.Select(p => new Vector2(p.x, p.y + dz)).ToArray()).ToList();

        static Dictionary<string, Material> EyeMaterials()
        {
            var vesselsSclera = ProceduralTextures.Vessels("VesselsSclera", 4, 22, .55f, 2.2f, 2, 14);
            var vesselsRetina = ProceduralTextures.Vessels("VesselsRetina", 9, 4, .04f, 7f, 4, 9);
            var vesselsChoroid = ProceduralTextures.Vessels("VesselsChoroid", 13, 34, .02f, 4f, 4, 30);
            var iris = ProceduralTextures.Iris();
            void Glass(Material m, float gloss, float spec, float rim) { m.SetFloat("_Gloss", gloss); m.SetFloat("_Specular", spec); m.SetFloat("_Rim", rim); m.SetFloat("_Cull", 0); m.SetFloat("_F0", .04f); }
            return new Dictionary<string, Material>
            {
                ["sclera"] = Anatomy("Eye_Sclera", new Color(.95f, .93f, .9f), m =>
                {
                    m.SetTexture("_DetailMap", vesselsSclera); m.SetColor("_DetailColor", new Color(.72f, .12f, .1f)); m.SetVector("_DetailAxis", new Vector4(0, 0, 1, 0));
                    m.SetFloat("_DetailSpan", 1.75f); m.SetFloat("_DetailStrength", .32f); m.SetFloat("_Gloss", .62f); m.SetFloat("_Specular", .35f); m.SetFloat("_Wrap", .55f); m.SetFloat("_Rim", .08f); m.SetFloat("_Cull", 0);
                }),
                ["cornea"] = Anatomy("Eye_Cornea", new Color(.3f, .34f, .36f), m => Glass(m, .97f, .9f, .45f)),   // dark albedo: clear glass reads through highlights, not a milky film
                ["iris"] = Anatomy("Eye_Iris", Color.white, m => { m.SetTexture("_BaseMap", iris); m.SetFloat("_Gloss", .55f); m.SetFloat("_Specular", .2f); m.SetFloat("_Wrap", .65f); m.SetFloat("_Cull", 0); m.SetColor("_Emission", new Color(.015f, .035f, .05f)); }),
                ["pupil"] = Anatomy("Eye_PupilRim", new Color(.03f, .025f, .02f), m => { m.SetFloat("_Gloss", .3f); m.SetFloat("_Cull", 0); }),
                ["aqueous"] = Anatomy("Eye_Aqueous", new Color(.55f, .85f, 1f), m => { Glass(m, .9f, .3f, .2f); m.SetFloat("_InteriorDark", 1); }),
                ["lens"] = Anatomy("Eye_Lens", new Color(.42f, .55f, .62f), m => { Glass(m, .97f, .65f, .22f); m.SetFloat("_InteriorDark", 1); m.SetFloat("_HighlightTint", .05f); }),
                ["vitreous"] = Anatomy("Eye_Vitreous", new Color(.78f, .88f, .92f), m => { Glass(m, .5f, .2f, .15f); m.SetFloat("_InteriorDark", 1); }),
                ["retina"] = Anatomy("Eye_Retina", new Color(.86f, .36f, .16f), m =>
                {
                    m.SetTexture("_DetailMap", vesselsRetina); m.SetColor("_DetailColor", new Color(.45f, .05f, .04f)); m.SetVector("_DetailAxis", (Vector4)G.OpticDisc);
                    m.SetFloat("_DetailSpan", 2.0f); m.SetFloat("_DetailStrength", .55f); m.SetFloat("_InteriorDark", 1); m.SetFloat("_Gloss", .35f); m.SetFloat("_Specular", .25f); m.SetFloat("_Wrap", .65f); m.SetFloat("_Cull", 0);
                }),
                ["choroid"] = Anatomy("Eye_Choroid", new Color(.42f, .06f, .07f), m =>
                {
                    m.SetTexture("_DetailMap", vesselsChoroid); m.SetColor("_DetailColor", new Color(.75f, .1f, .12f)); m.SetVector("_DetailAxis", new Vector4(0, 0, -1, 0));
                    m.SetFloat("_DetailSpan", 2.4f); m.SetFloat("_DetailStrength", .8f); m.SetFloat("_InteriorDark", 1); m.SetFloat("_Wrap", .6f); m.SetFloat("_Cull", 0);
                }),
                ["optic_nerve"] = Anatomy("Eye_OpticNerve", new Color(.96f, .84f, .55f), m => { m.SetFloat("_HighlightTint", .5f); m.SetFloat("_Flow", 1); m.SetFloat("_Gloss", .35f); m.SetFloat("_Wrap", .5f); m.SetFloat("_Rim", .25f); m.SetColor("_RimColor", new Color(1, .8f, .4f)); }),
                ["disc"] = Anatomy("Eye_OpticDisc", new Color(1f, .88f, .62f), m => { m.SetFloat("_InteriorDark", 1); m.SetFloat("_Cull", 0); }),
            };
        }

        static void BuildEye()
        {
            var mats = EyeMaterials();
            cutFrame = Obj("Cut Frame (eye-radius units)", null, pos: EyeCentre, rot: CutRotation, scale: Vector3.one * EyeRadius).transform;
            var nerveEnd = cutFrame.InverseTransformPoint(BrainEntry);
            var specs = new List<PartSpec>
            {
                new() { id = "sclera", label = "Sclera", mat = mats["sclera"], profile = G.ScleraProfile(), priority = 1,
                        focus = new(.62f, .38f, .62f), labelAt = new(1.15f, .95f, .35f), focusCut = new(-.02f, -.42f, -.88f), labelCut = new(-.1f, -.62f, -1.35f) },
                new() { id = "choroid", label = "Choroid", mat = mats["choroid"], profile = G.ChoroidProfile(), cutZone = Band(.99f, .905f, 50), priority = 6,
                        focus = new(0, .6f, -.65f), labelAt = new(0, 1.2f, -.7f), focusCut = new(-.02f, .69f, -.6f), labelCut = new(-.1f, 1.12f, -.78f) },
                new() { id = "retina", label = "Retina", mat = mats["retina"], profile = G.RetinaProfile(), cutZone = Band(.915f, .78f, 58), priority = 4,
                        focus = new(0, -.6f, -.62f), labelAt = new(0, -1.15f, -.7f), focusCut = new(-.02f, -.62f, -.62f), labelCut = new(-.1f, -1.12f, -.72f) },
                new() { id = "vitreous", label = "Vitreous humour", mat = mats["vitreous"], profile = G.VitreousProfile(), priority = 1, seeThrough = true, baseAlpha = .09f,
                        focus = new(0, .15f, -.15f), labelAt = new(0, .3f, -.2f), focusCut = new(-.02f, .22f, -.15f), labelCut = new(-.1f, .62f, .05f) },
                new() { id = "aqueous", label = "Aqueous humour", mat = mats["aqueous"], profile = G.AqueousProfile(), priority = 3, seeThrough = true, baseAlpha = .1f, queue = 2,
                        focus = new(0, .2f, .92f), labelAt = new(-.8f, .8f, 1.2f), focusCut = new(-.02f, .22f, .9f), labelCut = new(-.1f, .95f, 1.4f) },
                new() { id = "cornea", label = "Cornea", mat = mats["cornea"], profile = G.CorneaProfile(), priority = 2, seeThrough = true, baseAlpha = .12f, queue = 3,
                        focus = new(.02f, .38f, 1.02f), labelAt = new(.75f, .95f, 1.15f), focusCut = new(-.02f, .48f, .95f), labelCut = new(-.1f, .45f, 1.55f) },
            };

            var rig = Obj("Eye Rig", null, pos: EyeCentre, rot: HeroRotation, scale: Vector3.one * EyeRadius).transform;
            heroEye = EyeModel("Eye", rig, specs, mats, nerveEnd, true);
            var other = EyeModel("Other Eye (scan only)", null, specs, mats, nerveEnd, false);
            stage.eye = heroEye; stage.otherEye = other; stage.eyeRig = rig;
            stage.menuPoint = Obj("Menu Eye Point", null, pos: new Vector3(0, 2.3f, .6f), rot: HeroRotation, scale: Vector3.one * .65f).transform;
            stage.heroPoint = Obj("Hero Point", null, pos: EyeCentre, rot: HeroRotation, scale: Vector3.one * EyeRadius).transform;
            stage.cutPoint = Obj("Cut Point", null, pos: EyeCentre, rot: CutRotation, scale: Vector3.one * EyeRadius).transform;
        }

        static AnatomyPart NewPart(Transform parent, string id, string label, int priority, bool seeThrough, float baseAlpha, int queue)
        {
            var go = Obj(id, parent);
            var p = go.AddComponent<AnatomyPart>();
            p.id = id; p.label = label; p.priority = priority; p.seeThrough = seeThrough; p.baseAlpha = baseAlpha; p.queueOffset = queue;
            p.main = new Renderer[0]; p.removable = new Renderer[0]; p.pick = new Collider[0];
            return p;
        }

        static Transform Anchor(Transform parent, string name, Vector3 local) => Obj(name, parent, pos: local).transform;

        static Collider Pick(Transform parent, string name, Mesh mesh)
        {
            var go = Obj(name, parent, layer: PickLayer);
            var c = go.AddComponent<MeshCollider>(); c.sharedMesh = mesh;
            return c;
        }

        static EyeModel EyeModel(string name, Transform parent, List<PartSpec> specs, Dictionary<string, Material> mats, Vector3 nerveEnd, bool interactive)
        {
            var root = Obj(name, parent).transform;
            var eye = root.gameObject.AddComponent<EyeModel>();
            var removable = Obj("Removable Half", root).transform;
            var pickClosed = Obj("Pick · closed eye", root); var pickCut = Obj("Pick · cutaway", root); pickCut.SetActive(false);
            eye.removableRoot = removable; eye.pickClosed = pickClosed; eye.pickCut = pickCut;
            var parts = new List<AnatomyPart>(); var caps = new List<Renderer>();
            string tag = interactive ? "" : "_";

            AnatomyPart Layer(PartSpec s, Transform keepParent, Transform cutParent, float dz)
            {
                var part = NewPart(root, s.id, s.label, s.priority, s.seeThrough, s.baseAlpha, s.queue);
                var profile = Shift(s.profile, -dz);
                var keep = Obj("Keep", keepParent ?? part.transform, SaveMesh(Lathe.Revolve(profile, G.KeepFrom, G.KeepTo, 40), s.id + "_Keep"), s.mat);
                var cut = Obj(s.id + " (removable)", cutParent ?? removable, SaveMesh(Lathe.Revolve(profile, G.CutFrom, G.CutTo, 40), s.id + "_Cut"), s.mat);
                var cap = Obj("Section", keepParent ?? part.transform, SaveMesh(Lathe.Caps(profile, G.KeepFrom, G.KeepTo), s.id + "_Caps"), s.mat);
                part.main = new[] { keep.GetComponent<Renderer>(), cap.GetComponent<Renderer>() };
                part.removable = new[] { cut.GetComponent<Renderer>() };
                caps.Add(cap.GetComponent<Renderer>());
                if (interactive)
                {
                    var picks = new List<Collider> { Pick(pickClosed.transform, s.id, SaveMesh(Lathe.Revolve(s.profile, 0, 360, 20), s.id + "_PickClosed")) };
                    picks.Add(Pick(pickCut.transform, s.id, SaveMesh(Lathe.Caps(s.cutZone ?? s.profile, G.KeepFrom, G.KeepTo), s.id + "_PickCut")));
                    part.pick = picks.ToArray();
                    part.focus = Anchor(part.transform, "Focus", s.focus); part.labelAnchor = Anchor(part.transform, "Label", s.labelAt);
                    part.focusCut = Anchor(part.transform, "Focus (cut)", s.focusCut); part.labelAnchorCut = Anchor(part.transform, "Label (cut)", s.labelCut);
                }
                parts.Add(part);
                return part;
            }

            foreach (var s in specs) Layer(s, null, null, 0);

            // Crystalline lens: pivots at its centre so accommodation can change its curvature.
            var lensPivot = Obj("Lens Pivot", root, pos: new Vector3(0, 0, G.LensZ)).transform;
            var lensCut = Obj("Lens Pivot (removable)", removable, pos: new Vector3(0, 0, G.LensZ)).transform;
            Layer(new PartSpec
            {
                id = "lens", label = "Crystalline lens", mat = mats["lens"], profile = G.LensProfile(), priority = 5, seeThrough = false, queue = 1,
                focus = new(0, .15f, .66f), labelAt = new(-.9f, -.8f, .8f), focusCut = new(-.02f, -.18f, .66f), labelCut = new(-.1f, -.98f, .72f), baseAlpha = .34f
            }, lensPivot, lensCut, G.LensZ);
            eye.lens = lensPivot; eye.lensCut = lensCut;

            // Iris: dynamic meshes (the pupil is its inner edge — an opening, not an object).
            var iris = NewPart(root, "iris", "Iris", 3, false, 1, 0);
            var irisProfile = G.IrisProfile(G.PupilDefault);
            var ik = Obj("Keep", iris.transform, SaveMesh(Lathe.Revolve(irisProfile, G.KeepFrom, G.KeepTo, 36), "iris_Keep" + tag), mats["iris"]);
            var ic = Obj("iris (removable)", removable, SaveMesh(Lathe.Revolve(irisProfile, G.CutFrom, G.CutTo, 36), "iris_Cut" + tag), mats["iris"]);
            var icap = Obj("Section", iris.transform, SaveMesh(Lathe.Caps(irisProfile, G.KeepFrom, G.KeepTo), "iris_Caps" + tag), mats["iris"]);
            iris.main = new[] { ik.GetComponent<Renderer>(), icap.GetComponent<Renderer>() }; iris.removable = new[] { ic.GetComponent<Renderer>() };
            eye.irisKeep = ik.GetComponent<MeshFilter>(); eye.irisCut = ic.GetComponent<MeshFilter>(); eye.irisCaps = icap.GetComponent<MeshFilter>();
            caps.Add(icap.GetComponent<Renderer>());
            if (interactive)
            {
                var c = Pick(pickClosed.transform, "iris", SaveMesh(Lathe.Revolve(irisProfile, 0, 360, 32), "iris_Pick")) as MeshCollider;
                eye.irisPick = c; iris.pick = new Collider[] { c };
                iris.focus = Anchor(iris.transform, "Focus", new Vector3(-.3f, -.12f, .8f)); iris.labelAnchor = Anchor(iris.transform, "Label", new Vector3(-1.05f, -.75f, .85f));
                iris.focusCut = Anchor(iris.transform, "Focus (cut)", new Vector3(-.02f, .33f, .79f)); iris.labelAnchorCut = Anchor(iris.transform, "Label (cut)", new Vector3(-.1f, .98f, .6f));
            }
            parts.Add(iris);

            // Pupil: a thin dark rim at the iris margin (scaled to the current pupil radius) plus an enlarged tap disc.
            var pupil = NewPart(root, "pupil", "Pupil", 4, false, 1, 0);
            float pz = .797f;
            var rimProfile = new List<Vector2[]> { Lathe.Line(new Vector2(.9f, .004f), new Vector2(1.02f, .004f)), Lathe.Line(new Vector2(1.02f, .004f), new Vector2(1.02f, -.004f)), Lathe.Line(new Vector2(1.02f, -.004f), new Vector2(.9f, -.004f)), Lathe.Line(new Vector2(.9f, -.004f), new Vector2(.9f, .004f)) };
            var pupilT = Obj("Pupil (opening)", root, pos: new Vector3(0, 0, pz)).transform;
            var pupilCut = Obj("Pupil (removable)", removable, pos: new Vector3(0, 0, pz)).transform;
            var rk = Obj("Rim", pupilT, SaveMesh(Lathe.Revolve(rimProfile, G.KeepFrom, G.KeepTo, 36), "pupil_RimKeep"), mats["pupil"]);
            var rc = Obj("Rim (removable)", pupilCut, SaveMesh(Lathe.Revolve(rimProfile, G.CutFrom, G.CutTo, 36), "pupil_RimCut"), mats["pupil"]);
            pupil.main = new[] { rk.GetComponent<Renderer>() }; pupil.removable = new[] { rc.GetComponent<Renderer>() };
            eye.pupil = pupilT; eye.pupilCut = pupilCut;
            if (interactive)
            {
                pupil.pick = new[] { Pick(pupilT, "pupil (tap disc)", SaveMesh(Lathe.Revolve(new List<Vector2[]> { Lathe.Line(Vector2.zero, new Vector2(1.25f, 0), 2) }, 0, 360, 32), "pupil_PickDisc")) };
                pupil.pickWhenInvisible = true;
                pupil.focus = Anchor(pupil.transform, "Focus", new Vector3(0, 0, .8f)); pupil.labelAnchor = Anchor(pupil.transform, "Label", new Vector3(-.9f, .62f, 1.05f));
                pupil.focusCut = Anchor(pupil.transform, "Focus (cut)", new Vector3(-.02f, 0, .8f)); pupil.labelAnchorCut = Anchor(pupil.transform, "Label (cut)", new Vector3(-.1f, -.55f, 1.5f));
            }
            parts.Add(pupil);

            // Optic nerve: leaves the back of the eye 15° nasal of the posterior pole and runs to the brain (cut pose).
            var nerve = NewPart(root, "optic_nerve", "Optic nerve", 5, false, 1, 0);
            var path = G.NervePath(nerveEnd);
            var tube = Obj("Nerve", nerve.transform, SaveMesh(Lathe.Tube(path, .13f, 18, null, .85f), "optic_nerve" + tag), mats["optic_nerve"]);
            nerve.main = new[] { tube.GetComponent<Renderer>() };
            if (interactive)
            {
                nerve.pick = new[] { Pick(nerve.transform, "nerve pick", SaveMesh(Lathe.Tube(path, .2f, 8), "optic_nerve_Pick")) };
                nerve.focus = Anchor(nerve.transform, "Focus", path[8]); nerve.labelAnchor = Anchor(nerve.transform, "Label", path[8] + new Vector3(0, .7f, 0));
            }
            parts.Add(nerve);
            var disc = Obj("Optic disc", parts.First(p => p.id == "retina").transform, SaveMesh(Lathe.Revolve(new List<Vector2[]> { Lathe.Line(Vector2.zero, new Vector2(.085f, 0), 2) }, 0, 360, 24), "OpticDisc"), mats["disc"],
                G.OpticDisc * (G.RetinaIn - .004f), Quaternion.LookRotation(-G.OpticDisc));

            foreach (var c in caps) c.enabled = false;
            eye.caps = caps.ToArray();
            eye.parts = parts.ToArray();
            return eye;
        }
    }
}
