using System;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    public static partial class Builder
    {
        const string NikoPath = Root + "ThirdParty/UserModels/Niko/";

        /// The owner's rigged Niko (Tools/HumanEye/export_niko.py: 1.2 m, Mixamo-named skeleton, the supplied Close/Ou/Blink/
        /// Eye_L/R/U/D shape keys plus BrowUp) under the same "Niko" root, so Stage, the presenter, the HE_Niko layer, the
        /// anchors and the snap ring keep working. NikoRig poses the bones and drives the shapes; the live portrait camera
        /// frames his upper body (gestures and head movement read in the companion picture); a start-screen anchor stands him
        /// beside the corridor gate. Additive and idempotent, after ApplyGateFaceOpening (re-run it after any later patch).
        [MenuItem("Tools/Human Eye/Authoring/Apply supplied Niko character (keeps scene edits)", priority = 17)]
        public static void ApplyNikoCharacter()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            const string fbx = NikoPath + "Niko.fbx", tex = NikoPath + "Niko_BaseColor.jpg";
            var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Export the supplied Niko first: Tools/HumanEye/export_niko.py");
            // Generic keeps the skeleton and the skinned renderer (None would flatten it to a static mesh); no clips, no optimisation.
            importer.animationType = ModelImporterAnimationType.Generic; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false; importer.importAnimation = importer.importCameras = importer.importLights = false;
            importer.importBlendShapes = true; importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.isReadable = true;   // readable: the gaze shapes are measured below
            importer.SaveAndReimport();
            var ti = (TextureImporter)AssetImporter.GetAtPath(tex);
            if (ti.maxTextureSize != 2048 || !ti.sRGBTexture) { ti.maxTextureSize = 2048; ti.sRGBTexture = true; ti.SaveAndReimport(); }

            var rig = stage.niko; var root = rig.transform;
            foreach (var child in root.Cast<Transform>().ToArray())   // the model is replaced; the floating companion's dressing stays
                if (child.name != "Snap Ring" && !child.name.StartsWith("Niko Float")) Object.DestroyImmediate(child.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Niko_Armature"; model.transform.localPosition = Vector3.zero;
            var bones = model.GetComponentsInChildren<Transform>(true);
            Transform Find(string n) => bones.FirstOrDefault(t => t.name == n || t.name == "mixamorig:" + n);
            Transform B(string n) { var t = Find(n); if (!t) throw new InvalidOperationException("Niko.fbx lacks the bone " + n); return t; }
            // Face the root's forward (both feet together say where the character looks: each toe splays 23° outward) and
            // stand on the anchor.
            var toes = B("LeftToe_End").position - B("LeftToeBase").position + B("RightToe_End").position - B("RightToeBase").position; toes.y = 0;
            model.transform.Rotate(0, Vector3.SignedAngle(toes.normalized, root.forward, Vector3.up), 0, Space.World);
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            var bounds = TrueBounds(skin);
            model.transform.position += Vector3.up * (root.position.y - bounds.min.y);
            bounds = TrueBounds(skin);
            if (Mathf.Abs(bounds.size.y - 1.2f) > .08f) throw new InvalidOperationException("Niko export is not 1.2 m tall: " + bounds.size);
            skin.updateWhenOffscreen = true; skin.shadowCastingMode = ShadowCastingMode.On; skin.receiveShadows = true;
            skin.sharedMaterial = Lit("Niko_Body", Color.white, .3f, 0, Tex(tex));
            int layer = LayerMask.NameToLayer("HE_Niko");
            if (layer < 0) throw new InvalidOperationException("HE_Niko layer missing (WowPatch)");
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            rig.floater = model.transform; rig.hips = B("Hips"); rig.spine = B("Spine1"); rig.neck = B("Neck"); rig.head = B("Head");
            // NikoRig's gesture sides are the viewer's: "R" is the arm on Niko's -X side. Measure which anatomical arm that is.
            string R = root.InverseTransformPoint(B("LeftArm").position).x < 0 ? "Left" : "Right", L = R == "Left" ? "Right" : "Left";
            rig.clavicleR = B(R + "Shoulder"); rig.shoulderR = B(R + "Arm"); rig.elbowR = B(R + "ForeArm"); rig.handR = B(R + "Hand");
            rig.clavicleL = B(L + "Shoulder"); rig.shoulderL = B(L + "Arm"); rig.elbowL = B(L + "ForeArm"); rig.handL = B(L + "Hand");
            rig.thumbR = B(R + "HandThumb1"); rig.fingerR = B(R + "HandIndex1");
            // The cartoon hand has an index and a ring finger (no middle or little finger): take the fingers that exist, index first.
            Transform[] Fingers(string side) => new[] { "Index", "Middle", "Ring", "Pinky" }.Where(f => (bool)Find(side + "Hand" + f + "1"))
                .SelectMany(f => new[] { 1, 2, 3 }.Select(i => B(side + "Hand" + f + i))).ToArray();
            Transform[] Thumb(string side) => new[] { 1, 2, 3 }.Select(i => B(side + "HandThumb" + i)).ToArray();
            rig.fingersR = Fingers(R); rig.fingersL = Fingers(L); rig.thumbR3 = Thumb(R); rig.thumbL3 = Thumb(L);
            rig.skin = skin;
            FloatingNikoSkin(rig);   // a re-run keeps the floating companion's self-light slot and rim layer
            var mesh = skin.sharedMesh;
            int Shape(string n) { int i = mesh.GetBlendShapeIndex(n); if (i < 0) throw new InvalidOperationException("Niko.fbx lacks the " + n + " blend shape"); return i; }
            rig.blinkShape = Shape("Blink"); rig.closeShape = Shape("Close"); rig.ouShape = Shape("Ou"); rig.browShape = Shape("BrowUp");
            // Which eye shape turns the pupils toward which side: the mean vertex delta of each, in Niko's own frame.
            Vector3 Delta(string n)
            {
                var d = new Vector3[mesh.vertexCount]; mesh.GetBlendShapeFrameVertices(Shape(n), 0, d, null, null);
                var moved = d.Where(v => v.sqrMagnitude > 1e-12f).ToArray();
                var mean = moved.Aggregate(Vector3.zero, (a, v) => a + v) / Mathf.Max(1, moved.Length);
                return root.InverseTransformDirection(skin.transform.TransformDirection(mean));
            }
            var gaze = new[] { "Eye_L", "Eye_R", "Eye_U", "Eye_D" }.Select(n => (n, d: Delta(n))).ToArray();
            rig.gazeMinusX = Shape(gaze.OrderBy(g => g.d.x).First().n); rig.gazePlusX = Shape(gaze.OrderByDescending(g => g.d.x).First().n);
            rig.gazeUp = Shape(gaze.OrderByDescending(g => g.d.y).First().n); rig.gazeDown = Shape(gaze.OrderBy(g => g.d.y).First().n);
            Transform Proxy(string n) { var t = rig.head.Find(n); return t ? t : Obj(n, rig.head).transform; }
            rig.eyeL = Proxy("Eye L"); rig.eyeR = Proxy("Eye R"); rig.smile = Proxy("Smile");
            foreach (var p in new[] { rig.eyeL, rig.eyeR, rig.smile }) p.gameObject.layer = layer;
            rig.lookPitch = 10;   // the eyes cover steeper looks; a deeper head pitch drops the jaw onto the arms in front
            NikoCap(rig);
            MenuGateAnchor();
            BuildDialogue();   // mouth keys for every clip: *.align.json timing beside it, otherwise its own audio
            ClosingKeys();
            foreach (var o in new Object[] { rig, skin, model, root, stage }) EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_NIKO_APPLIED height={bounds.size.y:F3} head={rig.head.position - root.position} bones={bones.Length} shapes={mesh.blendShapeCount} " +
                      $"fingers={rig.fingersR.Length} gaze-X={mesh.GetBlendShapeName(rig.gazeMinusX)} +X={mesh.GetBlendShapeName(rig.gazePlusX)} up={mesh.GetBlendShapeName(rig.gazeUp)}");
        }

        /// The closing line is not a ledger cue: give it mouth keys of its own (timing beside it, otherwise its audio).
        static void ClosingKeys()
        {
            var clip = stage.closingLine;
            stage.closingCue = null;
            if (!clip) return;
            var path = AssetDatabase.GetAssetPath(clip);
            var cue = new DialogueDatabase.Cue { id = "closing", speaker = "Niko", text = Stage.ClosingText, clip = clip };
            var align = System.IO.Path.ChangeExtension(path, ".align.json");
            if (System.IO.File.Exists(align)) MouthKeys(cue, System.IO.File.ReadAllText(align)); else AudioKeys(cue, path);
            stage.closingCue = cue;
        }

        /// The start screen looks down the corridor at the closed gate (S02_IntroThreshold): Niko stands on the corridor floor
        /// to the right of the gate, clear of the menu panel on the left, facing the learner.
        static void MenuGateAnchor()
        {
            var shot = stage.cams.shots.FirstOrDefault(s => s && s.name == "S02_IntroThreshold");
            var corridor = GameObject.Find("gate ");
            if (!shot || !corridor) { Debug.LogWarning("HE_NIKO no start-screen shot or corridor gate: MenuGate anchor skipped"); return; }
            var floor = corridor.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Plane")).Select(r => r.bounds.min.y).DefaultIfEmpty(0).Min();
            var cam = shot.transform; var fwd = cam.forward; fwd.y = 0; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            // Solve against the shot's own lens (16:9): the nearest spot where his whole 1.2 m figure stands inside the frame,
            // feet above the bottom edge, at about three quarters of the width (clear of the menu panel on the left).
            var probe = new GameObject("menu-gate-probe").AddComponent<Camera>();
            Vector3 at = Vector3.zero;
            try
            {
                probe.transform.SetPositionAndRotation(cam.position, cam.rotation);
                probe.fieldOfView = shot.Lens.FieldOfView; probe.aspect = 16 / 9f;
                bool found = false;
                for (float d = 4; d <= 14 && !found; d += .25f)
                    for (float r = 0; r <= 5 && !found; r += .05f)
                    {
                        var p = cam.position + fwd * d + right * r; p.y = floor;
                        var feet = probe.WorldToViewportPoint(p); var top = probe.WorldToViewportPoint(p + Vector3.up * 1.25f);
                        if (feet.y >= .1f && top.y <= .78f && feet.x >= .74f) { at = p; found = true; }
                    }
                if (!found) { at = cam.position + fwd * 7 + right * 2.2f; at.y = floor; }
            }
            finally { Object.DestroyImmediate(probe.gameObject); }
            var toCam = cam.position - at; toCam.y = 0;
            var root = stage.nikoAnchors[0].parent;
            var anchor = stage.nikoAnchors.FirstOrDefault(a => a && a.name == "MenuGate");
            if (!anchor)
            {
                anchor = new GameObject("MenuGate").transform; anchor.SetParent(root, false);
                stage.nikoAnchors = stage.nikoAnchors.Append(anchor).ToArray();
            }
            anchor.SetPositionAndRotation(at, Quaternion.LookRotation(toCam.normalized));
            EditorUtility.SetDirty(anchor); EditorUtility.SetDirty(stage);
            Debug.Log($"HE_NIKO MenuGate anchor {at:F2} floor {floor:F2}");
        }
    }
}
