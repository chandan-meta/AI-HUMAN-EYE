using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HumanEye.EditorTools
{
    [CustomEditor(typeof(Hud))]
    public class HudEditor : Editor
    {
        static readonly string[] Panels = { "start-screen", "Gameplay", "settings-panel", "help-panel", "review-panel", "ask-panel", "activity", "pause-overlay", "confirm-panel", "end-menu" };
        int preview;
        public override void OnInspectorGUI()
        {
            var hud = (Hud)target;
            EditorGUILayout.HelpBox("All controls are saved under HUD / Observatory Canvas. Preview a panel below, then edit its RectTransform, Text, Image or layout components in the Hierarchy. Edit outside Play mode to keep changes.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying || !hud.canvas))
            {
                preview = EditorGUILayout.Popup("Panel to edit", preview, Panels);
                if (GUILayout.Button("Show panel in Hierarchy and Scene"))
                {
                    foreach (var key in Panels.Where(k => k != "Gameplay"))
                    {
                        var r = hud.Element(key); Undo.RecordObject(r.gameObject, "Preview HUD panel"); r.gameObject.SetActive(key == Panels[preview]);
                    }
                    foreach (var key in new[] { "topbar", "bottom", "middle", "subtitle-panel", "part-list", "view-controls" })
                    {
                        var r = hud.Element(key); Undo.RecordObject(r.gameObject, "Preview gameplay HUD"); r.gameObject.SetActive(preview == 1);
                    }
                    Canvas.ForceUpdateCanvases(); EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                    EditorGUIUtility.PingObject(preview == 1 ? hud.Element("bottom") : hud.Element(Panels[preview]));
                    SceneView.RepaintAll();
                }
            }
            DrawDefaultInspector();
        }
    }
}
