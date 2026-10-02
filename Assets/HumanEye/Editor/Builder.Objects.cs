using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        const string PH = Root + "ThirdParty/PolyHaven/";

        static Texture2D NormalMap(string path)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Transform Model(string path, Transform parent, Material mat, float height, string layer, string pick = null)
        {
            var go = Instantiate(path, parent);
            var rs = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rs) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); r.gameObject.layer = LayerMask.NameToLayer(layer); r.shadowCastingMode = ShadowCastingMode.On; }
            var b = WorldBounds(rs);
            go.transform.localScale *= height / b.size.y;
            b = WorldBounds(rs);
            go.transform.position += parent.position - b.center;
            return go.transform;
        }

        static void BuildObjects()
        {
            // Apple (Poly Haven, CC0): a non-luminous object lit by a lamp, so the light it sends to the eye is reflected light.
            var appleRoot = new GameObject("Apple (viewed object)").transform;
            appleRoot.position = EyeCentre + Vector3.left * EyeRadius * (EyeGeometry.LensZ + 7);
            var appleMat = Lit("Apple", Color.white, .55f, 0, AssetDatabase.LoadAssetAtPath<Texture2D>(PH + "Apple/textures/food_apple_01_diff_2k.jpg"), NormalMap(PH + "Apple/textures/food_apple_01_nor_gl_2k.jpg"));
            var apple = Model(PH + "Apple/food_apple_01_2k.fbx", appleRoot, appleMat, .26f, ObjectLayer);
            var ab = WorldBounds(apple.GetComponentsInChildren<Renderer>());
            Obj("Top", appleRoot, pos: new Vector3(0, ab.max.y - appleRoot.position.y, 0));
            Obj("Bottom", appleRoot, pos: new Vector3(0, ab.min.y - appleRoot.position.y, 0));
            var lamp = Obj("Lamp", appleRoot, Ellipsoid("LampBulb", .045f, .045f, 16), Glow("LampBulb", new Color(1, .9f, .7f, 1), null, 3), new Vector3(.15f, .62f, -.5f)).transform;
            var spot = lamp.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot; spot.color = new Color(1, .93f, .8f); spot.intensity = 3; spot.range = 3; spot.spotAngle = 45; spot.shadows = LightShadows.Soft;
            lamp.rotation = Quaternion.LookRotation(appleRoot.position - lamp.position);
            Obj("Lamp Shade", lamp, Ring("LampShade", .07f, .012f, 24), Lit("LampMetal", new Color(.3f, .32f, .36f), .6f, .8f), rot: Quaternion.Euler(90, 0, 0));
            var pedestal = Obj("Pedestal", null, Cylinder("Pedestal", .13f, ab.min.y - .015f, 32), Lit("Pedestal", new Color(.18f, .2f, .24f), .7f, .6f), new Vector3(appleRoot.position.x, 0, appleRoot.position.z), Upright);
            Obj("Pedestal Ring", pedestal.transform, Ring("PedestalRing", .135f, .008f, 32), Glow("PedestalGlow", new Color(.2f, .85f, 1f, .7f), null, 1.6f), new Vector3(0, 0, ab.min.y - .02f), Quaternion.Euler(90, 0, 0));

            // Distant object (Poly Haven, CC0) — tallest variant, alpha-clipped foliage.
            var treeRoot = new GameObject("Tree (distant object)").transform;
            treeRoot.position = new Vector3(EyeCentre.x - EyeRadius * (EyeGeometry.LensZ + 25), 1.6f, EyeCentre.z);
            var treeTex = ProceduralTextures.CombineAlpha(PH + "Tree/textures/searsia_lucida_diff_1k.jpg", PH + "Tree/textures/searsia_lucida_alpha_1k.png", "TreeDiffuseAlpha");
            var treeMat = Lit("Tree", Color.white, .25f, 0, treeTex, NormalMap(PH + "Tree/textures/searsia_lucida_nor_gl_1k.jpg"), null, true);
            var treeGo = Instantiate(PH + "Tree/searsia_lucida_1k.fbx", null);
            var variants = treeGo.GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("LOD0") || !r.name.Contains("LOD")).ToList();
            var tallest = variants.OrderByDescending(r => r.bounds.size.y).First();
            var tree = Combine("Tree", new[] { tallest }, treeRoot, treeMat, "Tree");
            tree.layer = LayerMask.NameToLayer(ObjectLayer);
            Object.DestroyImmediate(treeGo);
            var tr = tree.GetComponent<Renderer>(); tr.shadowCastingMode = ShadowCastingMode.On;
            var tb = tr.bounds; tree.transform.localScale = Vector3.one * (3.2f / tb.size.y);
            tb = tr.bounds; tree.transform.position += new Vector3(treeRoot.position.x - tb.center.x, -tb.min.y, treeRoot.position.z - tb.center.z);
            tb = tr.bounds;
            treeRoot.position = new Vector3(treeRoot.position.x, 0, treeRoot.position.z);
            Obj("Top", treeRoot, pos: new Vector3(0, tb.max.y, 0));
            Obj("Bottom", treeRoot, pos: new Vector3(0, .05f, 0));

            stage.appleRoot = appleRoot; stage.treeRoot = treeRoot; stage.lampRoot = lamp; stage.pedestal = pedestal.transform;

            // Light rays (warm, continuous streaks; bottom-of-object rays dashed) + retinal image + object camera.
            var streak = ProceduralTextures.Streak(); var dash = ProceduralTextures.Dash();
            var rayMat = Glow("LightRay", new Color(1, .9f, .62f, 1), streak, 2.2f, new Vector2(-1.2f, 0));
            var rayDash = Glow("LightRayDashed", new Color(1, .86f, .55f, 1), dash, 2.2f, new Vector2(-1.2f, 0));
            var lampRay = Glow("LampRay", new Color(1, .9f, .65f, 1), streak, 1.4f, new Vector2(-.8f, 0));
            var raysGo = new GameObject("Light Rays");
            var rays = raysGo.AddComponent<LightRays>();
            LineRenderer Line(string name, Material m, Transform parent)
            {
                var l = Obj(name, parent).AddComponent<LineRenderer>();
                l.sharedMaterial = m; l.useWorldSpace = true; l.textureMode = LineTextureMode.Tile; l.alignment = LineAlignment.View;
                l.numCapVertices = 2; l.shadowCastingMode = ShadowCastingMode.Off; l.receiveShadows = false; l.positionCount = 0;
                return l;
            }
            rays.rays = Enumerable.Range(0, 6).Select(i => Line("Ray " + i, i < 3 ? rayMat : rayDash, raysGo.transform)).ToArray();
            rays.lampRays = Enumerable.Range(0, 3).Select(i => Line("Lamp Ray " + i, lampRay, raysGo.transform)).ToArray();
            rays.eye = heroEye; rays.lamp = lamp; rays.lampLight = spot;
            rays.targetTop = appleRoot.Find("Top"); rays.targetBottom = appleRoot.Find("Bottom");
            var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "ObjectView" };
            rt = Upsert(rt, Gen + "UI/ObjectView.renderTexture");
            var camGo = new GameObject("Object Camera (retinal image source)");
            var oc = camGo.AddComponent<Camera>();
            oc.targetTexture = rt; oc.cullingMask = LayerMask.GetMask(ObjectLayer); oc.clearFlags = CameraClearFlags.SolidColor; oc.backgroundColor = new Color(0, 0, 0, 0);
            oc.nearClipPlane = .05f; oc.farClipPlane = 60; oc.enabled = false; oc.depth = -5;
            var ocd = camGo.AddComponent<UniversalAdditionalCameraData>(); ocd.renderPostProcessing = false;
            rays.objectCamera = oc;
            var quadMesh = Quad("ImageQuad");
            var image = Obj("Retinal image (real, inverted)", heroEye.transform, quadMesh, Glow("RetinalImage", new Color(1, .95f, .85f, 1), rt, 1.5f),
                new Vector3(-.04f, 0, -.86f), Quaternion.LookRotation(Vector3.right, Vector3.down));
            rays.imageQuad = image.transform;
            raysGo.SetActive(false);
            stage.rays = rays;

            // Electrical signals: zig-zag pulses along the optic nerve into the brain's visual processing area (world space).
            var sigGo = new GameObject("Signals");
            var sig = sigGo.AddComponent<SignalPulses>();
            var sigMat = Glow("Signal", new Color(.3f, 1f, .85f, 1), null, 2.6f);
            sig.pool = Enumerable.Range(0, 48).Select(i => { var l = Line("Pulse " + i, sigMat, sigGo.transform); l.textureMode = LineTextureMode.Stretch; return l; }).ToArray();
            var nervePath = EyeGeometry.NervePath(cutFrame.InverseTransformPoint(BrainEntry)).Select(p => cutFrame.TransformPoint(p)).ToList();
            var inside = new[] { BrainEntry + new Vector3(.25f, .12f, 0), (BrainEntry + visualCentreWorld) * .5f + Vector3.up * .15f, visualCentreWorld };
            sig.paths = new List<SignalPulses.Path> { new() { id = "nerve", points = nervePath.Concat(inside).ToArray() } }.ToArray();
            sig.speed = .75f;
            stage.signals = sig;
        }

        /// Unit quad in the XY plane facing -Z (UVs 0..1).
        static Mesh Quad(string name)
        {
            var m = new Mesh();
            m.SetVertices(new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetNormals(Enumerable.Repeat(Vector3.back, 4).ToArray());
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }
    }
}
