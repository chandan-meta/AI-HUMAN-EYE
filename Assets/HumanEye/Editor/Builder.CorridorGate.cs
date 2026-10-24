using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HumanEye.EditorTools
{
    /// The owner's corridor gate (15 October 2026, `Assets/THE HUMAN EYE/Fbx/gate .fbx`, instance "gate " at the scene root,
    /// placed just inside where the lab's lifting door used to be; the owner removed that door's leaves). The gate becomes
    /// the opening's `Stage.entrance`: a `LabEntrance` whose two leaves slide apart along the gate's own cross axis, so the
    /// menu closes it, the opening holds a second, parts it and enters, and skip / replay / menu return keep working.
    public static partial class Builder
    {
        const string CorridorGateName = "gate ";   // the owner's instance name carries a trailing space

        static GameObject CorridorGate()
        {
            var go = GameObject.Find(CorridorGateName);
            if (!go) throw new InvalidOperationException("The corridor gate instance \"" + CorridorGateName + "\" is not in the saved scene");
            return go;
        }

        [MenuItem("Tools/Human Eye/Authoring/Apply corridor gate (keeps scene edits)", priority = 19)]
        public static void ApplyCorridorGate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var gate = CorridorGate();
            var leaves = new[] { "gate 1", "gate 2" }.Select(n => Child(gate.transform, n)).ToArray();
            if (leaves.Any(l => !l)) throw new InvalidOperationException("The corridor gate needs its 'gate 1' and 'gate 2' leaves");
            var entrance = gate.GetComponent<LabEntrance>();
            if (entrance && entrance.leaves != null && entrance.leaves.Length == 2 && entrance.closed != null && entrance.closed.Length == 2) entrance.SetGate(0);   // re-run: measure the closed pose
            if (!entrance) entrance = gate.AddComponent<LabEntrance>();
            var across = gate.transform.right; across.y = 0; across.Normalize();   // the leaves part along the gate's cross axis
            var doors = leaves.Select(l => l.GetComponentInChildren<Renderer>().bounds).ToArray();
            var split = (doors[0].center + doors[1].center) * .5f;
            entrance.leaves = leaves; entrance.closed = new Vector3[2]; entrance.open = new Vector3[2];
            for (int i = 0; i < 2; i++)
            {
                float width = Mathf.Abs(doors[i].size.x * across.x) + Mathf.Abs(doors[i].size.z * across.z);
                float side = Mathf.Sign(Vector3.Dot(doors[i].center - split, across));
                entrance.closed[i] = leaves[i].localPosition;
                entrance.open[i] = leaves[i].localPosition + leaves[i].parent.InverseTransformVector(across * (width + .1f) * side);   // clears the opening into the frame
            }
            entrance.boltShare = entrance.pauseShare = 0;   // two leaves, no bolt
            var old = stage.entrance;
            if (old && old != entrance)
            {   // the lifting door's driver: its leaves are gone from the scene, so it goes too (the validator flags empty leaf slots)
                entrance.accents = old.accents; entrance.accentIntensity = old.accentIntensity;
                Object.DestroyImmediate(old);
            }
            stage.entrance = entrance; entrance.SetGate(0);
            var lab = GameObject.Find(LabRoot);   // the owner's save while another Open Dome FBX was loaded dropped the lab's walk colliders
            if (lab) LabWalkColliders(lab.transform); else Debug.LogWarning("HE_CORRIDOR no '" + LabRoot + "' in the scene: walk colliders not restored");
            foreach (var o in new Object[] { stage, entrance, gate }) EditorUtility.SetDirty(o);
            foreach (var l in leaves) EditorUtility.SetDirty(l);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"HUMAN_EYE_CORRIDOR_GATE_APPLIED split={split:F3} across={across:F3} leaf widths={doors[0].size.z:F2}/{doors[1].size.z:F2} open={entrance.open[0]:F3}/{entrance.open[1]:F3} accents={(entrance.accents != null ? entrance.accents.Length : 0)}");
        }

        /// Measurements only: node poses, renderer bounds and where the gate sits on the face axis (the opening's rail).
        [MenuItem("Tools/Human Eye/Authoring/Probe corridor gate")]
        public static void ProbeCorridorGate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            stage = Object.FindFirstObjectByType<Stage>(FindObjectsInactive.Include);
            var gate = CorridorGate();
            foreach (var t in gate.GetComponentsInChildren<Transform>(true))
                Debug.Log($"HE_GATE node '{t.name}' parent='{(t.parent ? t.parent.name : "-")}' pos={t.position:F3} euler={t.rotation.eulerAngles:F1} lossy={t.lossyScale:F3} local={t.localPosition:F3} {t.localRotation.eulerAngles:F1} {t.localScale:F3}");
            var human = stage.human; var root = human.transform;
            var frame = Matrix4x4.TRS(root.position, root.rotation, Vector3.one);   // the runtime pose: ResetVisual forces scale 1
            var eyes = frame.MultiplyPoint3x4((human.eyeLeft.localPosition + human.eyeRight.localPosition) * .5f);
            var face = frame.MultiplyVector(human.eyeRight.localRotation * Vector3.forward); face.y = 0; face.Normalize();
            var right = Vector3.Cross(Vector3.up, -face).normalized;
            Debug.Log($"HE_GATE eyes={eyes:F3} face={face:F3} right={right:F3} GateLook={Stage.GateLook:F3} labGateAlong={Vector3.Dot(Stage.GateLook - eyes, face):F3} CamStart={Stage.CamStart:F3}");
            foreach (var r in gate.GetComponentsInChildren<Renderer>(true))
            {
                var c = r.bounds.center; var d = c - eyes;
                var mesh = r.GetComponent<MeshFilter>() ? r.GetComponent<MeshFilter>().sharedMesh : null;
                Debug.Log($"HE_GATE renderer '{r.name}' centre={c:F3} size={r.bounds.size:F3} along={Vector3.Dot(d, face):F3} lateral={Vector3.Dot(d, right):F3} up={c.y - eyes.y:F3} tris={(mesh ? mesh.triangles.Length / 3 : 0)} mats={string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "-"))}");
            }
        }
    }
}
