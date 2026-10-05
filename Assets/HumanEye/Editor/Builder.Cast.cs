using System;
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
        const string Src = "Assets/_Project/SourceAssets/";

        static Mesh Ellipsoid(string name, float rx, float rz, int seg = 32) =>
            SaveMesh(Lathe.Revolve(new List<Vector2[]> { Ellipse(rx, rz, 24) }, 0, 360, seg), name);

        static Vector2[] Ellipse(float rx, float rz, int steps)
        {
            var p = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++) { float a = Mathf.PI * i / steps; p[i] = new Vector2(rx * Mathf.Sin(a), rz * Mathf.Cos(a)); }
            return p;
        }

        /// Capsule along +Z from 0 to length (lathe), used for Niko's limbs.
        static Mesh Capsule(string name, float radius, float length)
        {
            var profile = new List<Vector2[]>
            {
                Lathe.Arc(radius, radius, 180, 90, 8),
                Lathe.Line(new Vector2(radius, radius), new Vector2(radius, length - radius)),
                Lathe.Arc(length - radius, radius, 90, 0, 8),
            };
            return SaveMesh(Lathe.Revolve(profile, 0, 360, 20), name);
        }

        static readonly Quaternion Down = Quaternion.Euler(90, 0, 0);   // lathe +Z → -Y (hanging limbs)

        // ---------------- Niko ----------------
        static void BuildCast()
        {
            BuildNiko();
            BuildHuman();
        }

        static void BuildNiko()
        {
            var soft = ProceduralTextures.Soft();
            var white = Lit("Niko_White", new Color(.93f, .94f, .96f), .8f);
            var visor = Lit("Niko_Visor", new Color(.012f, .016f, .025f), .96f);
            var glow = Glow("Niko_Glow", new Color(.3f, .88f, 1f, 1), null, 2.6f);
            var glowSoft = Glow("Niko_Thruster", new Color(.3f, .8f, 1f, .7f), soft, 2);

            var root = new GameObject("Niko").transform;
            var rig = root.gameObject.AddComponent<NikoRig>();
            var floater = Obj("Float", root).transform;
            Obj("Torso", floater, Ellipsoid("Niko_Torso", .19f, .25f), white, new Vector3(0, .5f, 0), Upright);
            Obj("Chest Light", floater, Ring("Niko_Chest", .065f, .013f, 32), glow, new Vector3(0, .58f, .175f), Quaternion.Euler(90, 0, 0));
            Obj("Thruster", floater, Disc("Niko_ThrusterDisc", .13f, 24), glowSoft, new Vector3(0, .24f, 0), Quaternion.Euler(90, 0, 0));
            var head = Obj("Head", floater, pos: new Vector3(0, .96f, 0)).transform;
            Obj("Head Shell", head, Ellipsoid("Niko_Head", .245f, .215f), white, Vector3.zero, Upright);
            var visorProfile = new List<Vector2[]> { Lathe.Arc(0, .205f, 0, 64, 16) };
            Obj("Visor", head, SaveMesh(Lathe.Revolve(visorProfile, 0, 360, 40), "Niko_Visor"), visor, new Vector3(0, -.005f, .095f), Quaternion.identity, new Vector3(1.15f, .9f, .8f));
            var eyeMesh = Ellipsoid("Niko_Eye", .028f, .042f, 16);
            rig.eyeL = Obj("Eye L", head, eyeMesh, glow, new Vector3(.072f, .03f, .256f), Upright).transform;
            rig.eyeR = Obj("Eye R", head, eyeMesh, glow, new Vector3(-.072f, .03f, .256f), Upright).transform;
            rig.eyeL.localScale = rig.eyeR.localScale = new Vector3(1, .25f, 1);
            var smilePath = new List<Vector3>();
            for (int i = 0; i <= 16; i++) { float a = Mathf.Lerp(-55, 55, i / 16f) * Mathf.Deg2Rad; smilePath.Add(new Vector3(Mathf.Sin(a) * .06f, -Mathf.Cos(a) * .028f, 0)); }
            rig.smile = Obj("Smile", head, SaveMesh(Lathe.Tube(smilePath, .007f, 8), "Niko_Smile"), glow, new Vector3(0, -.04f, .252f)).transform;
            foreach (var side in new[] { 1, -1 })
            {
                Obj("Ear", head, Cylinder("Niko_Ear", .065f, .05f, 24), white, new Vector3(.2f * side, 0, 0), Quaternion.Euler(0, 90 * side, 0));
                Obj("Ear Light", head, Ring("Niko_EarRing", .045f, .009f, 24), glow, new Vector3(.277f * side, 0, 0), Quaternion.Euler(0, 0, 90));
            }
            var upper = Capsule("Niko_UpperArm", .057f, .21f); var fore = Capsule("Niko_Forearm", .053f, .19f);
            var palm = Ellipsoid("Niko_Palm", .05f, .045f, 16); var digit = Capsule("Niko_Digit", .014f, .06f);
            Transform Arm(string side, float x, out Transform elbow, out Transform hand)
            {
                var shoulder = Obj("Shoulder " + side, floater, pos: new Vector3(x, .63f, 0)).transform;
                Obj("Shoulder joint", shoulder, Ellipsoid("Niko_Joint", .058f, .058f), visor);
                Obj("Upper Arm", shoulder, upper, white, Vector3.zero, Down);
                elbow = Obj("Elbow", shoulder, pos: new Vector3(0, -.2f, 0)).transform;
                Obj("Elbow joint", elbow, Ellipsoid("Niko_Elbow", .049f, .049f), visor);
                Obj("Forearm", elbow, fore, white, Vector3.zero, Down);
                hand = Obj("Hand", elbow, pos: new Vector3(0, -.2f, 0)).transform;
                Obj("Palm", hand, palm, white, new Vector3(0, -.035f, 0), Upright);
                return shoulder;
            }
            rig.shoulderR = Arm("R", -.215f, out rig.elbowR, out rig.handR);
            rig.shoulderL = Arm("L", .215f, out rig.elbowL, out _);
            rig.thumbR = Obj("Thumb", rig.handR, pos: new Vector3(.035f, -.03f, .02f)).transform;
            Obj("Thumb Mesh", rig.thumbR, digit, white, Vector3.zero, Down);
            rig.fingerR = Obj("Finger", rig.handR, pos: new Vector3(-.01f, -.07f, .02f)).transform;
            Obj("Finger Mesh", rig.fingerR, digit, white, Vector3.zero, Down);
            rig.floater = floater; rig.head = head;
            var ring = Obj("Snap Ring", root, Ring("Niko_SnapRing", 1, .02f, 48), Glow("Niko_SnapGlow", new Color(.4f, .9f, 1f, 1), null, 2), rot: Quaternion.Euler(90, 0, 0));
            rig.snapRing = ring.transform; rig.snapRingRenderer = ring.GetComponent<Renderer>(); rig.snapRingRenderer.enabled = false;
            rig.narrator = narrator;
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;

            Transform A(string name, Vector3 pos, Vector3 face) => Obj(name, null, pos: pos, rot: Quaternion.LookRotation(new Vector3(face.x, 0, face.z).normalized)).transform;
            var cam = new Vector3(0, 1.4f, -6);
            var anchors = new List<Transform>
            {
                A("Intro", new Vector3(-1.3f, .38f, .8f), cam - new Vector3(-1.3f, 0, .8f)),
                A("EyeLeft", new Vector3(-.9f, .18f, .75f), new Vector3(.35f, 0, -1)),
                A("Menu", new Vector3(-2.65f, .08f, .5f), new Vector3(.25f, 0, -1)),
                A("Optics", new Vector3(1.05f, .02f, -.45f), new Vector3(-.35f, 0, -1)),
                A("Eleven", new Vector3(1.55f, .25f, -.15f), new Vector3(-.4f, 0, -1)),
                A("Brain", new Vector3(.95f, .25f, -.75f), new Vector3(-.2f, 0, -1)),
                A("Wide", new Vector3(-1.5f, .32f, -.45f), new Vector3(.3f, 0, -1)),
                A("Micro", MicroPosition + new Vector3(-3.7f, .15f, -1.7f), new Vector3(.35f, 0, -1)),
            };
            var anchorRoot = new GameObject("Niko Anchors").transform;
            foreach (var a in anchors) a.SetParent(anchorRoot, true);
            stage.niko = rig; stage.nikoAnchors = anchors.ToArray();
            root.SetPositionAndRotation(anchors[0].position, anchors[0].rotation);
        }

        // ---------------- helpers for imported models ----------------
        static GameObject Instantiate(string path, Transform parent)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && path.Contains("ZAnatomy") && (mi.importNormals != ModelImporterNormals.Calculate || mi.normalSmoothingAngle < 180))
            {
                mi.importNormals = ModelImporterNormals.Calculate; mi.normalSmoothingAngle = 180; mi.SaveAndReimport();   // fully smooth: decimation leaves creases far sharper than real gyri
            }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) throw new Exception("Missing source model: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return go;
        }

        static Bounds WorldBounds(IEnumerable<Renderer> rs)
        {
            var list = rs.ToList();
            var b = TrueBounds(list[0]); foreach (var r in list) b.Encapsulate(TrueBounds(r)); return b;
        }

        /// Skinned meshes report stale bounds in edit mode, so measure the baked (posed) vertices instead.
        static Bounds TrueBounds(Renderer r)
        {
            if (r is not SkinnedMeshRenderer s) return r.bounds;
            var m = new Mesh(); s.BakeMesh(m, true);
            var xf = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
            var v = m.vertices;
            var b = new Bounds(xf.MultiplyPoint3x4(v[0]), Vector3.zero);
            foreach (var p in v) b.Encapsulate(xf.MultiplyPoint3x4(p));
            Object.DestroyImmediate(m);
            return b;
        }

        /// Merges renderers into one static mesh (local to `space`) — fewer draw calls on tablets.
        static GameObject Combine(string name, IEnumerable<Renderer> renderers, Transform space, Material mat, string meshName)
        {
            var combine = new List<CombineInstance>();
            foreach (var r in renderers)
            {
                Mesh m; Matrix4x4 xf;
                if (r is SkinnedMeshRenderer s) { m = new Mesh(); s.BakeMesh(m, true); xf = space.worldToLocalMatrix * Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one); }
                else { var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; m = mf.sharedMesh; xf = space.worldToLocalMatrix * r.transform.localToWorldMatrix; }
                for (int i = 0; i < m.subMeshCount; i++) combine.Add(new CombineInstance { mesh = m, subMeshIndex = i, transform = xf });
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(combine.ToArray(), true, true);
            mesh.RecalculateBounds();
            return Obj(name, space, SaveMesh(mesh, meshName), mat);
        }

        // ---------------- human for the scan ----------------
        const string ZA = Root + "ThirdParty/ZAnatomy/ZAnatomy_Head.fbx";

        /// Z-Anatomy head structures (CC BY-SA 4.0) registered by their eyeball markers onto the target eyes:
        /// full frame alignment (forward + the person's left-right axis), uniform scale from the eye spacing.
        static (Renderer skull, Renderer brain, Renderer visual) PlaceZAnatomy(Transform parent, Vector3 leftEye, Vector3 rightEye, Vector3 forward)
        {
            var go = Instantiate(ZA, parent);
            Renderer R(string n) => go.GetComponentsInChildren<Renderer>(true).First(r => r.name == n);
            Vector3 C(string n) => R(n).bounds.center;
            var zl = C("ZA_EyeMarker_L"); var zr = C("ZA_EyeMarker_R");
            var zFwd = ((C("ZA_EyeFront_L") + C("ZA_EyeFront_R")) * .5f - (zl + zr) * .5f).normalized;
            Quaternion Frame(Vector3 f, Vector3 right) => Quaternion.LookRotation(f, Vector3.Cross(f, right.normalized));
            var rot = Frame(forward, leftEye - rightEye) * Quaternion.Inverse(Frame(zFwd, zl - zr));
            float scale = Vector3.Distance(leftEye, rightEye) / Vector3.Distance(zl, zr);
            go.transform.rotation = rot * go.transform.rotation;
            go.transform.localScale *= scale;
            var zmid = (C("ZA_EyeMarker_L") + C("ZA_EyeMarker_R")) * .5f;
            go.transform.position += (leftEye + rightEye) * .5f - zmid;
            foreach (var m in new[] { "ZA_EyeMarker_L", "ZA_EyeMarker_R", "ZA_EyeFront_L", "ZA_EyeFront_R" }) R(m).gameObject.SetActive(false);
            return (R("ZA_Skull"), R("ZA_Brain"), R("ZA_VisualArea"));
        }

        static void BuildHuman()
        {
            var human = new GameObject("Human Display").transform;   // assembled at the origin, moved onto the plinth at the end
            var hd = human.gameObject.AddComponent<HumanDisplay>();
            var temp = new GameObject("temp-import").transform;

            // Supplied FitnessMan figure (project archive; redistribution rights unverified, see ASSET_REGISTER).
            var fm = Instantiate(Src + "Zip/Extra/FitnessMan.fbx", temp);
            foreach (var a in fm.GetComponentsInChildren<Animator>()) a.enabled = false;
            var all = fm.GetComponentsInChildren<Renderer>(true);
            bool IsEyePart(string n) { n = n.ToLowerInvariant(); return new[] { "cornea", "iris", "pupil", "vitreous", "choroid", "retina", "lense", "vain", "eye_ball", "muscle", "ligement", "sclera" }.Any(n.Contains); }
            var eyeBalls = all.Where(r => r.name.ToLowerInvariant().Contains("eye_ball")).ToList();
            var bodyParts = all.Where(r => !IsEyePart(r.name)).ToList();
            float scale = 1.76f / WorldBounds(bodyParts).size.y;
            fm.transform.localScale *= scale;
            fm.transform.position -= new Vector3(0, WorldBounds(bodyParts).min.y, 0);
            var bodyCentre = WorldBounds(bodyParts).center;
            var eyesNow = eyeBalls.Select(r => TrueBounds(r).center).ToList();
            var eyeMid = eyesNow.Aggregate(Vector3.zero, (x, y) => x + y) / eyesNow.Count;
            var forward = new Vector3(eyeMid.x - bodyCentre.x, 0, eyeMid.z - bodyCentre.z).normalized;
            var turn = Quaternion.FromToRotation(forward, Vector3.back);   // face the learner (world -Z)
            fm.transform.rotation = turn * fm.transform.rotation;
            fm.transform.position = turn * fm.transform.position;
            var centred = WorldBounds(bodyParts).center;
            fm.transform.position -= new Vector3(centred.x, 0, centred.z);
            // Square the face to the learner using the eye axis (the figure's left eye ends up at +X when facing -Z).
            var axis = eyeBalls.Select(r => TrueBounds(r).center).OrderBy(p => p.x).ToList();
            var across = axis.Last() - axis.First(); across.y = 0;
            var square = Quaternion.FromToRotation(across.normalized, Vector3.right);
            fm.transform.rotation = square * fm.transform.rotation;
            fm.transform.position = square * fm.transform.position;
            var recentred = WorldBounds(bodyParts).center;
            fm.transform.position -= new Vector3(recentred.x, 0, recentred.z);
            var eyes = eyeBalls.Select(r => TrueBounds(r).center).OrderBy(p => p.x).ToList();
            Debug.Log($"HE_HUMAN eyes {eyes.First()} / {eyes.Last()} body {WorldBounds(bodyParts)}");
            Vector3 eyeL = eyes.Last(), eyeR = eyes.First();   // person's left (+X) and right (-X)

            // Untextured materials per garment group, so no texture atlas can leak across sub-meshes.
            var skinMat = Anatomy("Human_Skin", new Color(.86f, .67f, .56f), m => { m.SetFloat("_Wrap", .55f); m.SetFloat("_Gloss", .3f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .35f); m.SetColor("_RimColor", new Color(.4f, .8f, 1f)); });
            var fabricMat = Anatomy("Human_Fabric", new Color(.12f, .2f, .32f), m => { m.SetFloat("_Wrap", .5f); m.SetFloat("_Gloss", .15f); m.SetFloat("_Specular", .05f); m.SetFloat("_Rim", .25f); m.SetColor("_RimColor", new Color(.4f, .8f, 1f)); });
            var hairMat = Anatomy("Human_Hair", new Color(.08f, .06f, .05f), m => { m.SetFloat("_Gloss", .4f); m.SetFloat("_Rim", .2f); });
            bool Shorts(Renderer r) => r.name.ToLowerInvariant().Contains("short");
            bool Cloth(Renderer r) { var n = r.name.ToLowerInvariant(); return !Shorts(r) && (n.Contains("shirt") || n.Contains("shoe") || n.Contains("sneaker") || n.Contains("sock") || n.Contains("cloth")); }
            bool Hair(Renderer r) { var n = r.name.ToLowerInvariant(); return n.Contains("hair") || n.Contains("brow") || n.Contains("lash"); }
            var bodyOnly = bodyParts.Where(r => !Cloth(r) && !Hair(r) && !Shorts(r)).ToList();
            Debug.Log("HE_HUMAN parts: " + string.Join(", ", bodyParts.Select(r => r.name + (Cloth(r) ? "[cloth]" : Hair(r) ? "[hair]" : "[skin]"))));
            var skinRenderers = new List<Renderer> { Combine("Skin", bodyOnly, human, skinMat, "Human_Skin").GetComponent<Renderer>() };
            if (bodyParts.Any(Cloth)) skinRenderers.Add(Combine("Clothing", bodyParts.Where(Cloth), human, fabricMat, "Human_Clothing").GetComponent<Renderer>());
            if (bodyParts.Any(Hair)) skinRenderers.Add(Combine("Hair", bodyParts.Where(Hair), human, hairMat, "Human_Hair").GetComponent<Renderer>());
            hd.skin = NewPart(human, "skin", "Skin", 0, false, 1, 4); hd.skin.main = skinRenderers.ToArray();
            // Shorts stay opaque through the whole scan, so the inner layers never show an undressed figure.
            hd.garment = NewPart(human, "garment", "Shorts", 0, false, 1, 5);
            hd.garment.main = new[] { Combine("Shorts", bodyParts.Where(Shorts), human, fabricMat, "Human_Shorts").GetComponent<Renderer>() };

            // Muscle layer: the same body surface pulled 4 mm inward, so it always fits the figure.
            var muscleMat = Anatomy("Human_Muscle", new Color(.62f, .15f, .13f), m => { m.SetFloat("_Wrap", .5f); m.SetFloat("_Gloss", .5f); m.SetFloat("_Specular", .35f); m.SetFloat("_Rim", .3f); m.SetColor("_RimColor", new Color(1, .5f, .4f)); });
            var muscle = Combine("Muscles", bodyOnly, human, muscleMat, "Human_Muscles");
            var mm = muscle.GetComponent<MeshFilter>().sharedMesh; var mv = mm.vertices; var mn = mm.normals;
            for (int i = 0; i < mv.Length; i++) mv[i] -= mn[i] * .004f;
            mm.vertices = mv; mm.RecalculateBounds();
            hd.muscles = NewPart(human, "muscles", "Muscles", 0, false, 1, 3); hd.muscles.main = new[] { muscle.GetComponent<Renderer>() };

            // Skull and brain (Z-Anatomy), registered so the Z-Anatomy eyeballs sit exactly on the figure's eye centres.
            var (skull, brain, visual) = PlaceZAnatomy(temp, eyeL, eyeR, Vector3.back);
            var boneMat = Anatomy("Human_Skull", new Color(.88f, .85f, .78f), m => { m.SetFloat("_Wrap", .45f); m.SetFloat("_Gloss", .3f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .25f); });
            var brainMat = AssetDatabase.LoadAssetAtPath<Material>(Gen + "Materials/Brain.mat");
            hd.skull = NewPart(human, "skull", "Skull", 0, false, 1, 2); hd.skull.main = new[] { Combine("Skull", new[] { skull }, human, boneMat, "Human_Skull").GetComponent<Renderer>() };
            hd.brain = NewPart(human, "brain_layer", "Brain", 0, false, 1, 1);
            hd.brain.main = new[] { Combine("Brain", new[] { brain, visual }, human, brainMat, "Human_Brain").GetComponent<Renderer>() };

            const float eyeRadius = .0122f;
            hd.eyeLeft = Obj("Socket (other eye)", human, pos: human.InverseTransformPoint(eyeL), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.eyeRight = Obj("Socket (selected eye)", human, pos: human.InverseTransformPoint(eyeR), rot: Quaternion.LookRotation(Vector3.back), scale: Vector3.one * eyeRadius).transform;
            hd.headTop = WorldBounds(bodyParts).max.y; hd.headY = eyeL.y;
            var scan = Obj("Scan Wave", human, Disc("ScanDisc", .9f, 64), Glow("ScanWave", new Color(.2f, .7f, 1f, .55f), ProceduralTextures.Soft(), 2.2f), new Vector3(0, 1, 0), Quaternion.Euler(90, 0, 0));
            Obj("Scan Ring", scan.transform, Ring("ScanRing", .9f, .012f, 64), Glow("ScanRingGlow", new Color(.35f, .9f, 1f, 1), null, 3), rot: Quaternion.Euler(-90, 0, 0));
            hd.scanPlane = scan.transform; hd.scanRenderer = scan.GetComponent<Renderer>();
            Object.DestroyImmediate(temp.gameObject);
            human.position = HumanPosition;
            stage.human = hd;
        }

        // ---------------- brain (Scenes 4, 9, 10) ----------------
        static void BuildBrain()
        {
            var root = new GameObject("Brain Display").transform;
            var temp = new GameObject("temp-brain").transform;
            // Register as if the head faced -X (toward the eye), then scale the brain to an illustrative 1.75 m length.
            var (skull, brainR, visualR) = PlaceZAnatomy(temp, new Vector3(0, 0, -.031f), new Vector3(0, 0, .031f), Vector3.left);   // facing -X: person's left is -Z
            skull.gameObject.SetActive(false);
            var b = WorldBounds(new[] { brainR, visualR });
            temp.localScale = Vector3.one * (1.75f / b.size.x);
            b = WorldBounds(new[] { brainR, visualR });
            temp.position += BrainCentre - b.center;
            var brainMat = Anatomy("Brain", new Color(.88f, .62f, .64f), m => { m.SetFloat("_Wrap", .6f); m.SetFloat("_Gloss", .25f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .22f); });
            var visualMat = Anatomy("Brain_VisualArea", new Color(.88f, .62f, .64f), m => { m.SetFloat("_Wrap", .6f); m.SetFloat("_Gloss", .25f); m.SetFloat("_Specular", .12f); m.SetFloat("_Rim", .22f); m.SetColor("_HighlightColor", new Color(.3f, .62f, 1f)); m.SetFloat("_HighlightTint", .65f); });
            var whole = NewPart(root, "brain", "Brain", 2, false, 1, 0);
            whole.main = new[] { Combine("Brain", new[] { brainR }, whole.transform, brainMat, "Brain").GetComponent<Renderer>() };
            var area = NewPart(root, "visual_area", "Visual processing area", 3, false, 1, 0);
            var areaGo = Combine("Visual processing area", new[] { visualR }, area.transform, visualMat, "Brain_VisualArea");
            foreach (var r in new[] { whole.main[0], areaGo.GetComponent<Renderer>() }) r.shadowCastingMode = ShadowCastingMode.On;
            var bb = WorldBounds(new[] { whole.main[0], areaGo.GetComponent<Renderer>() });
            var box = Obj("pick", whole.transform, layer: PickLayer).AddComponent<BoxCollider>();
            box.center = whole.transform.InverseTransformPoint(bb.center); box.size = bb.size * .9f;
            whole.pick = new Collider[] { box };
            var areaBounds = areaGo.GetComponent<Renderer>().bounds;
            var areaBox = Obj("pick", area.transform, layer: PickLayer).AddComponent<BoxCollider>();
            areaBox.center = area.transform.InverseTransformPoint(areaBounds.center); areaBox.size = areaBounds.size;
            area.pick = new Collider[] { areaBox };
            whole.focus = Obj("Focus", whole.transform, pos: bb.center).transform; whole.labelAnchor = Obj("Label", whole.transform, pos: bb.center + new Vector3(-.2f, bb.extents.y + .25f, 0)).transform;
            area.focus = Obj("Focus", area.transform, pos: areaBounds.center).transform; area.labelAnchor = Obj("Label", area.transform, pos: areaBounds.center + new Vector3(.45f, .6f, -.3f)).transform;
            Object.DestroyImmediate(temp.gameObject);
            stage.brainRoot = root; stage.brain = whole; stage.visualArea = area;
            visualCentreWorld = areaBounds.center;
            // Where the optic nerve meets the brain: on the underside, a third of the way back from the front.
            BrainEntry = new Vector3(bb.min.x + bb.size.x * .3f, bb.min.y + bb.size.y * .22f, bb.center.z);
        }

        static Vector3 visualCentreWorld;
    }
}
