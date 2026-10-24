using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// The owner's ANATOMY.blend models (15 October 2026) replace the Rocketbox skin and the Rack 01/02/04 layers: skin
    /// (with shorts, lashes and a Blink blend shape), muscles, skeleton, internal organs and vessels, all exported into one
    /// frame by Tools/HumanEye/export_anatomy_blend.py. Additive and idempotent, after the gate-to-face and Niko patches:
    /// the subject's authored transform, the opening's timings, breakup, camera and sound are untouched. The sockets move to
    /// the new eyes, so the Z-Anatomy head structures are re-fitted by the eye spacing, the X-ray skull becomes the new
    /// skull, and the Rack 06 brain is re-fitted into the new model's own brain box.
    public static partial class Builder
    {
        const string AnatomyPath = Root + "ThirdParty/UserModels/Anatomy/";

        [MenuItem("Tools/Human Eye/Authoring/Apply ANATOMY.blend models (keeps scene edits)", priority = 18)]
        public static void ApplyAnatomyModels()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include); cams = stage.cams;
            anatomyShader = Shader.Find("HumanEye/Anatomy"); glowShader = Shader.Find("HumanEye/Glow");
            var human = stage.human; var reveal = human.reveal;
            if (!reveal || !human.visualSystem) throw new InvalidOperationException("Apply the opening and reference passes first");
            var pose = human.transform.localToWorldMatrix;
            foreach (var name in new[] { "Skin", "Muscles", "Skeleton", "Organs", "Vessels" }) ModelSettings(AnatomyPath + name + ".fbx");
            TextureSizes(AnatomyPath);
            var oldMid = (human.eyeLeft.localPosition + human.eyeRight.localPosition) * .5f;
            float oldIpd = Vector3.Distance(human.eyeLeft.localPosition, human.eyeRight.localPosition);
            AnatomySkin(human, out var brainMin, out var brainMax);
            RefitHeadAnatomy(human, oldMid, oldIpd);
            reveal.skeleton = RackLayer(human, "Skeleton", Color.white, AnatomyPath);
            reveal.muscles = RackLayer(human, "Muscles", Color.white, AnatomyPath);
            reveal.organs = RackLayer(human, "Organs", Color.white, AnatomyPath);
            reveal.vessels = RackLayer(human, "Vessels", Color.white, AnatomyPath);   // shown and wiped with the organs
            reveal.pathway = human.pathway;
            SkullFromSkeleton(human, reveal.skeleton);
            BrainInHead(human, brainMin, brainMax);
            reveal.height = human.headTop; reveal.shoulder = human.neckY - .08f;
            OpeningMagic();   // the breakup haze and the mote emitter follow the new head
            if (pose != human.transform.localToWorldMatrix) throw new InvalidOperationException("The subject's authored transform changed");
            foreach (var o in new Object[] { stage, human, reveal, human.skin, human.garment, human.skull, human.eyeLeft, human.eyeRight, human.systemSocketL, human.systemSocketR })
                if (o) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_ANATOMY_APPLIED eyes {human.eyeLeft.localPosition:F4} / {human.eyeRight.localPosition:F4} radius {human.eyeLeft.localScale.x:F4} " +
                      $"headTop {human.headTop:F3} neckY {human.neckY:F3} previousIpd {oldIpd:F4}");
        }

        static void ModelSettings(string path)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!mi) throw new InvalidOperationException("Run Tools/HumanEye/export_anatomy_blend.py first: " + path);
            if (mi.importAnimation || mi.importCameras || mi.importLights || mi.animationType != ModelImporterAnimationType.None || !mi.isReadable || mi.importBlendShapeNormals != ModelImporterNormals.Calculate)
            {
                mi.importAnimation = mi.importCameras = mi.importLights = false; mi.animationType = ModelImporterAnimationType.None;
                mi.isReadable = true; mi.importBlendShapeNormals = ModelImporterNormals.Calculate; mi.SaveAndReimport();
            }
        }

        /// The skin and muscle maps are 4096 (desktop); WebGL takes every map at 2048.
        static void TextureSizes(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (!ti) continue;
                string file = Path.GetFileName(path); int max = file.StartsWith("HumanSkin") || file.StartsWith("Muscles") ? 4096 : 2048;
                var web = ti.GetPlatformTextureSettings("WebGL");
                if (ti.maxTextureSize == max && web.overridden && web.maxTextureSize == 2048) continue;
                ti.maxTextureSize = max; web.overridden = true; web.maxTextureSize = 2048; web.format = TextureImporterFormat.Automatic;
                ti.SetPlatformTextureSettings(web); ti.SaveAndReimport();
            }
        }

        /// Surface families: natural skin, dark red fibrous muscle with a mild organic sheen, ivory bone, moist organs,
        /// arterial red / venous blue vessels. Tints stay white: the supplied albedos carry the colour.
        static void AnatomyLook(Material m, string slot)
        {
            void Set(Color tint, float specular, float gloss, float wrap, float rim, float wet = 0, float bump = 1)
            {
                m.SetColor("_BaseColor", tint); m.SetFloat("_Specular", specular); m.SetFloat("_Gloss", gloss); m.SetFloat("_Wrap", wrap);
                m.SetFloat("_Rim", rim); m.SetFloat("_Wet", wet); m.SetFloat("_BumpScale", m.GetTexture("_BumpMap") ? bump : 0);
            }
            if (slot == "HumanSkin") { Set(new Color(.74f, .70f, .68f), .18f, .38f, .32f, .08f, 0, 1); m.SetFloat("_Cutoff", .5f); m.SetColor("_RimColor", new Color(.55f, .75f, .95f)); }   // below white: the portrait key otherwise bleaches the supplied tone to pink; alpha clip for the eyelash cards
            else if (slot == "Shorts") Set(Color.white, .06f, .2f, .35f, .04f);
            else if (slot.StartsWith("Muscles")) Set(new Color(.9f, .86f, .86f), .12f, .35f, .22f, .05f, .05f, 1);
            else if (slot is "Skull" or "Spine" or "Ribcage" or "Pelvis" or "Legs" or "Hands") Set(Color.white, .07f, .25f, .3f, .05f, 0, 1);
            else if (slot == "Vessels") Set(Color.white, .16f, .42f, .35f, .06f, .06f, 1);
            else Set(Color.white, .18f, .4f, .3f, .08f, .12f, 1);
        }

        static Bounds LocalBounds(Transform space, Mesh mesh, Transform at)
        {
            var xf = space.worldToLocalMatrix * at.localToWorldMatrix; var v = mesh.vertices;
            var b = new Bounds(xf.MultiplyPoint3x4(v[0]), Vector3.zero); foreach (var p in v) b.Encapsulate(xf.MultiplyPoint3x4(p)); return b;
        }

        /// The exported Body (skin + lashes, Blink) and Shorts replace the Rocketbox figure under the same root: face along -Z,
        /// the person's left at +X, feet on the root, bounds centred like the figure before; the sockets take the new eyes.
        static void AnatomySkin(HumanDisplay human, out Vector3 brainMin, out Vector3 brainMax)
        {
            const string path = AnatomyPath + "Skin.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!prefab) throw new InvalidOperationException("Missing " + path);
            foreach (var name in new[] { "Anatomy Skin", "Rocketbox_Display", "Muscles" })   // the previous figure and the muscle shell derived from it
            { var old = human.transform.Find(name); if (old) Object.DestroyImmediate(old.gameObject); }
            var root = Object.Instantiate(prefab).transform; root.name = "Anatomy Skin";
            root.SetParent(human.transform, false); root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity; root.localScale = Vector3.one;
            Vector3 L(string n)
            {
                var t = Child(root, n); if (!t) throw new InvalidOperationException("Skin.fbx lacks the landmark " + n);
                return human.transform.InverseTransformPoint(t.position);
            }
            var mid = (L("EyeSocket_L") + L("EyeSocket_R")) * .5f; var fwd = L("FaceDirection") - mid; fwd.y = 0;
            // yaw only (the import may face +Z: an antiparallel FromToRotation would pick any axis and turn the figure over)
            root.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(fwd, Vector3.back, Vector3.up), Vector3.up) * root.localRotation;
            var across = L("EyeSocket_L") - L("EyeSocket_R"); across.y = 0;
            root.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(across, Vector3.right, Vector3.up), Vector3.up) * root.localRotation;
            var bodyNode = Child(root, "Body"); if (!bodyNode) throw new InvalidOperationException("Skin.fbx must carry the Body mesh");
            var body = bodyNode.GetComponent<SkinnedMeshRenderer>(); var filter = bodyNode.GetComponent<MeshFilter>();
            var bodyMesh = body ? body.sharedMesh : filter ? filter.sharedMesh : null;
            if (!bodyMesh || bodyMesh.blendShapeCount < 1) throw new InvalidOperationException($"Skin.fbx Body carries {(bodyMesh ? bodyMesh.blendShapeCount : -1)} blend shapes; expected Blink");
            if (!body)   // a blend-shaped mesh without bones imports as a plain renderer: the blink needs a skinned one
            {
                var plain = bodyNode.GetComponent<MeshRenderer>(); var mats = plain.sharedMaterials;
                Object.DestroyImmediate(plain); Object.DestroyImmediate(filter);
                body = bodyNode.gameObject.AddComponent<SkinnedMeshRenderer>(); body.sharedMesh = bodyMesh; body.sharedMaterials = mats;
            }
            var shorts = root.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r => r.name.StartsWith("Shorts"));
            if (!shorts) throw new InvalidOperationException("Skin.fbx must carry the Shorts mesh");
            var bounds = LocalBounds(human.transform, body.sharedMesh, body.transform);
            bounds.Encapsulate(LocalBounds(human.transform, shorts.GetComponent<MeshFilter>().sharedMesh, shorts.transform));
            root.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            bounds = LocalBounds(human.transform, body.sharedMesh, body.transform); mid = (L("EyeSocket_L") + L("EyeSocket_R")) * .5f;
            float radius = (Vector3.Distance(L("EyeFront_L"), L("EyeSocket_L")) + Vector3.Distance(L("EyeFront_R"), L("EyeSocket_R"))) * .5f / 1.126f;   // the lesson eye's cornea apex sits 1.126 radii out
            human.eyeLeft.localPosition = L("EyeSocket_L"); human.eyeRight.localPosition = L("EyeSocket_R");
            human.eyeLeft.localScale = human.eyeRight.localScale = Vector3.one * radius;
            brainMin = L("BrainMin"); brainMax = L("BrainMax");
            foreach (var mark in new[] { "EyeSocket_L", "EyeSocket_R", "EyeFront_L", "EyeFront_R", "FaceDirection", "BodyBase", "BodyTop", "BrainMin", "BrainMax" })
            { var t = Child(root, mark); if (t) Object.DestroyImmediate(t.gameObject); }
            body.sharedMaterials = new[] { LayerMaterial(AnatomyPath, "Skin", "HumanSkin", Color.white) };
            shorts.sharedMaterial = LayerMaterial(AnatomyPath, "Skin", "Shorts", Color.white);
            body.localBounds = body.sharedMesh.bounds; body.updateWhenOffscreen = true;
            foreach (var r in new Renderer[] { body, shorts }) r.shadowCastingMode = ShadowCastingMode.On;
            human.skin.main = new Renderer[] { body }; human.garment.main = new Renderer[] { shorts };
            human.garment.label = "Shorts"; human.blinkingSkin = body; human.SetBlink(0);
            human.headTop = bounds.max.y; human.headY = mid.y; human.neckY = mid.y - .2095f;   // the collar line keeps the Rocketbox offset under the eyes
            Debug.Log($"HE_ANATOMY skin bounds={bounds} eyes={human.eyeLeft.localPosition:F4}/{human.eyeRight.localPosition:F4} radius={radius:F4} brain={brainMin:F3}..{brainMax:F3}");
        }

        /// The Z-Anatomy structures (face and eye muscles, orbicularis, the optic pathway and its signal routes) were fitted to
        /// the old eyes: a similarity about the eye midpoint, scaled by the eye spacing, follows the new ones. The in-head
        /// brain and the X-ray skull are rebuilt separately and keep identity transforms.
        static void RefitHeadAnatomy(HumanDisplay human, Vector3 oldMid, float oldIpd)
        {
            var newMid = (human.eyeLeft.localPosition + human.eyeRight.localPosition) * .5f;
            float k = Vector3.Distance(human.eyeLeft.localPosition, human.eyeRight.localPosition) / oldIpd;
            var brain = human.brain && human.brain.main.Length > 0 ? human.brain.main[0].transform : null;
            var set = new[] { human.skull, human.muscles, human.orbicularis }.Where(p => p).Select(p => p.transform).ToList();
            foreach (Transform child in human.visualSystem) if (child != brain) set.Add(child);
            foreach (var t in set) { t.localPosition = newMid + (t.localPosition - oldMid) * k; t.localScale *= k; EditorUtility.SetDirty(t); }
            foreach (var (socket, target) in new[] { (human.systemSocketL, human.eyeLeft), (human.systemSocketR, human.eyeRight) })
                if (socket) { socket.localPosition = target.localPosition; socket.localRotation = target.localRotation; socket.localScale = target.localScale; }
            Debug.Log($"HE_ANATOMY head anatomy refit k={k:F4} mid {oldMid:F4} -> {newMid:F4} ({set.Count} transforms)");
        }

        /// The X-ray skull (Scene 9's bone ghost, the legacy glass skull) is the new skeleton's skull slot, in the subject's space.
        static void SkullFromSkeleton(HumanDisplay human, AnatomyPart skeleton)
        {
            var r = skeleton.main[0]; var mesh = r.GetComponent<MeshFilter>().sharedMesh; var mats = r.sharedMaterials;
            int slot = Array.FindIndex(mats, m => m && m.name.EndsWith("Skull"));
            if (slot < 0 || !human.skull || human.skull.main.Length == 0) { Debug.LogWarning("HE_ANATOMY no skull slot or skull part: X-ray skull unchanged"); return; }
            var skull = new Mesh { indexFormat = IndexFormat.UInt32 };
            skull.vertices = mesh.vertices; skull.normals = mesh.normals; skull.uv = mesh.uv; skull.tangents = mesh.tangents;
            skull.triangles = mesh.GetTriangles(slot); skull.RecalculateBounds();
            var target = human.skull.main[0];
            target.GetComponent<MeshFilter>().sharedMesh = SaveMesh(skull, "Human_Skull");
            target.sharedMaterials = new[] { mats[slot] };
            target.transform.localPosition = Vector3.zero; target.transform.localRotation = Quaternion.identity; target.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(target); EditorUtility.SetDirty(target.GetComponent<MeshFilter>());
        }

        /// Rack 06 stays the brain (lobe-split, more detailed than the .blend's single mesh); it is fitted into the box the
        /// .blend's own brain occupies inside the new skull.
        static void BrainInHead(HumanDisplay human, Vector3 brainMin, Vector3 brainMax)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RackBrainPath + "Rack06_Brain.fbx");
            if (!asset || !human.brain || human.brain.main.Length == 0) { Debug.LogWarning("HE_ANATOMY Rack 06 or the head brain missing: brain unchanged"); return; }
            var source = Object.Instantiate(asset);
            try
            {
                var filters = source.GetComponentsInChildren<MeshFilter>();
                var origin = Child(source.transform, "BrainCenter").position;
                var front = Child(source.transform, "BrainFront").position - origin;
                var up = Child(source.transform, "BrainUp").position - origin;
                var sourceFrame = Matrix4x4.TRS(origin, Quaternion.LookRotation(front, up), Vector3.one).inverse;
                var materials = new[] {
                    BrainMaterial("Forebrain", "Forebrain_Brain_Diff", "Forebrain_Brain_NM", "Forebrain_AO"),
                    BrainMaterial("Hindbrain", "Hindbrain_Diff", "Hindbrain_NM", "HindBrain_AO"),
                    BrainMaterial("HindbrainCenter", "Hindbrain_Center_Diff", "Hindbrain_Center_NM", "Hindbrain_Center_AO"),
                    BrainMaterial("Midbrain", "Midbrain_Diff", "Midbrain_NM", "Midbrain_AO") };
                var bounds = new Bounds(human.transform.TransformPoint(brainMin), Vector3.zero);
                for (int i = 1; i < 8; i++)
                    bounds.Encapsulate(human.transform.TransformPoint(new Vector3((i & 1) == 0 ? brainMin.x : brainMax.x, (i & 2) == 0 ? brainMin.y : brainMax.y, (i & 4) == 0 ? brainMin.z : brainMax.z)));
                var head = human.brain.main[0];
                head.transform.localPosition = Vector3.zero; head.transform.localRotation = Quaternion.identity; head.transform.localScale = Vector3.one;
                FitBrain(filters, sourceFrame, head, bounds, human.eyeRight.forward, null, "Human_Brain", materials);
                Debug.Log($"HE_ANATOMY Rack 06 head brain refit into {bounds}");
            }
            finally { Object.DestroyImmediate(source); }
        }
    }
}
