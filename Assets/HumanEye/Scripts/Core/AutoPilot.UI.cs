using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HumanEye
{
    public partial class AutoPilot
    {


        public static int PointerFailures { get; private set; }
        readonly HashSet<int> pointerChecked = new();
        // Exercise the real EventSystem hit ordering before delivering a left pointer click.
        // This is in-engine pointer evidence, not OS mouse automation.
        IEnumerator ClickUI(string name)
        {
            var b = hud.Control<Button>(name);
            if (b == null) { Log("FAIL", "Missing button " + name); yield break; }
            yield return ClickUI(b);
        }
        PointerEventData PointerAt(Button b, out GameObject hit)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)b.transform;
            var camera = hud.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            hit = hits.Count > 0 ? hits[0].gameObject : null;
            if (hits.Count > 0) pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            return pointer;
        }
        IEnumerator ClickUI(Button b)
        {
            yield return null;
            // The lesson can move on during that frame (e.g. the intro ends and Skip is hidden); that is a state change, not a blocked pointer.
            if (!b.gameObject.activeInHierarchy) { Log("check", b.name + " was no longer shown when the click arrived (state changed)"); yield break; }
            // Scroll to controls below a modal's viewport before attempting a real hit.
            var scroll = b.GetComponentInParent<ScrollRect>();
            if (scroll && scroll.vertical)
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)b.transform;
                var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                if (!scroll.viewport.rect.Contains(local))
                {
                    scroll.StopMovement();
                    scroll.content.anchoredPosition += Vector2.up * (scroll.viewport.rect.center.y - local.y);
                    yield return null;
                }
            }
            var pointer = PointerAt(b, out var hit);
            if (!hit || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) != b.gameObject)
            { PointerFailures++; Log("FAIL", "Pointer blocked: " + b.name + " by " + (hit ? hit.name : "no hit")); Exit(1); yield break; }
            pointer.pointerPress = b.gameObject; pointer.eligibleForClick = true;
            ExecuteEvents.Execute(b.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(b.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(b.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            Log("ui", "Pointer clicked " + b.name);
        }

        Transform Layer(Transform t) { var root = hud.Element("root"); while (t && t.parent && t.parent != root) t = t.parent; return t; }
        void AuditPointers(string context)
        {
            int checkedCount = 0;
            foreach (var b in hud.canvas.GetComponentsInChildren<Button>())
            {
                if (!b.IsInteractable()) continue;
                if (((RectTransform)b.transform).rect.width < 1) continue;   // activated this frame; its layout group has not placed it yet
                var pointer = PointerAt(b, out var hit);
                var scroll = b.GetComponentInParent<ScrollRect>();
                var cam = hud.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.canvas.worldCamera;
                if (scroll && !RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, pointer.position, cam)) continue;
                if (hit && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == b.gameObject)
                {
                    checkedCount++;
                    if (pointerChecked.Add(b.GetInstanceID())) Log("pointer-reachable", b.transform.parent.name + "/" + b.name);
                    continue;
                }
                // Buttons behind another open modal are deliberately covered.
                if (hit && Layer(hit.transform) != Layer(b.transform) && Layer(hit.transform)?.name is not ("safe" or "labels-layer")) continue;
                PointerFailures++; Log("FAIL", context + " pointer blocked: " + b.name + " by " + (hit ? hit.name : "no hit"));
            }
            Log("pointer-audit", context + " reachable=" + checkedCount + " cumulativeFailures=" + PointerFailures);
        }

        bool Visible(string name) => hud.Element(name).gameObject.activeInHierarchy;
        void Check(bool result, string description) => Log(result ? "PASS" : "FAIL", description);

        IEnumerator CheckMenuUI()
        {
            yield return ClickUI("start-settings");
            Check(Visible("settings-panel"), "Settings opens from menu");
            yield return Capture("menu_settings");
            yield return ClickUI("settings-close");
            Check(!Visible("settings-panel"), "Settings closes");
            AuditLayout("menu");
        }

        bool flowChecked, demoChecked, savedChecked;
        bool Stress => System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-heStress");

        IEnumerator CheckFlowUI()
        {
            flowChecked = true;
            yield return ClickUI("btn-help");
            Check(Visible("help-panel") && Clock.Paused && !Visible("pause-overlay"), "Help pauses behind the modal without covering it");
            Check(hud.IsOverUI(new Vector2(10, 10)), "Modal blocks world input outside card");
            var cue = narrator.Current?.id;
            yield return new WaitForSecondsRealtime(.35f);
            Check(cue == narrator.Current?.id, "Narration held while Help is open");
            Check(stage.observatory.GetComponentsInChildren<ParticleSystem>().All(p => !p.isPlaying), "Pause holds ambient particles behind Help");
            yield return Capture("help_modal");
            yield return ClickUI("help-close");
            Check(!Clock.Paused, "Closing Help restores running state");
            yield return ClickUI("btn-settings");
            yield return ClickUI("set-size-3");
            Check(SaveSystem.Data.settings.subtitleSize == 3 && Clock.Paused, "XL caption setting stays paused and saves");
            yield return Capture("settings_xl");
            yield return ClickUI("set-size-1");
            yield return ClickUI("settings-close");
            yield return ClickUI("btn-review");
            yield return Capture("review_modal");
            yield return ClickUI("review-close");
            yield return ClickUI("btn-ask");
            hud.Control<InputField>("ask-input").text = "why is the sky blue?";
            yield return ClickUI("ask-submit");
            Check(guide.Answer?.unsupported == true, "Ask Niko unavailable answer retains usable text feedback");
            yield return Capture("ask_unsupported");
            yield return ClickUI("ask-close");
            Check(!runner.AskHold, "Closing Ask Niko releases flow hold");
            yield return ClickUI("btn-pause");
            yield return ClickUI("pause-resume");
            Check(!Clock.Paused && Time.timeScale == 1, "Pause/resume UI bindings preserve timeScale");
        }

        IEnumerator CheckDemoStress()
        {
            demoChecked = true;
            yield return ClickUI("ab-REPLAY");
            for (int i = 0; i < 8; i++) stage.UseAbility("REPLAY");
            Check(stage.DemoBusy && !stage.Ability("REPLAY", out _), "Rapid REPLAY requests cannot start overlapping demonstrations");
            yield return Real(.5f);
            runner.SetPaused(true);
            var position = cams.brain.transform.position; float pupil = stage.eye.PupilRadius;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Vector3.Distance(position, cams.brain.transform.position) < .005f && Mathf.Approximately(pupil, stage.eye.PupilRadius), "Pause holds camera and biology during REPLAY");
            runner.SetPaused(false);
            stage.UseAbility("FREEZE");
            Check(Clock.Frozen, "FREEZE works while a demonstration plays");
            stage.UseAbility("FREEZE");
            // Leave while delayed demo work exists, then resume its chapter checkpoint from saved data.
            string checkpoint = SaveSystem.Data.checkpoint;
            int index = runner.Index, cues = SaveSystem.Data.cues.Count;
            yield return ClickUI("btn-home");
            Check(!runner.Started && stage.signals.Live == 0 && !Clock.Paused && !Clock.Frozen, "Return to menu cancels active effects and restores clocks");
            SaveSystem.Load();
            Check(SaveSystem.Data.checkpoint == checkpoint && SaveSystem.Data.cues.Count == cues, "Save/reload preserves checkpoint and completed cues");
            hud.ShowStart();
            yield return Capture("saved_resume_menu");
            yield return ClickUI("start-begin");
            Check(Visible("confirm-panel") && !runner.Started, "Starting again requests confirmation before overwriting progress");
            yield return Capture("new_session_confirmation");
            yield return ClickUI("confirm-cancel");
            Check(SaveSystem.Data.cues.Count == cues, "Cancel protects existing progress");
            yield return ClickUI("start-continue");
            yield return null;
            Check(runner.Started && runner.Index == index, "Resume button returns to saved chapter");
        }

        IEnumerator CheckSavedCompletion()
        {
            savedChecked = true;
            yield return ClickUI("end-menu-button");
            SaveSystem.Load(); hud.ShowStart();
            Check(SaveSystem.Data.completedChapters.Count == 10 && SaveSystem.Data.cues.Count == 66, "Full saved investigation reloads with all chapters and cues");
            yield return Capture("completed_resume_menu");
            yield return ClickUI("start-continue");
            Check(runner.Started && runner.Index == 9, "Completed save resumes Scene 11");
        }

        void AuditLayout(string context)
        {
            int count = 0, small = 0, outside = 0;
            foreach (var b in hud.canvas.GetComponentsInChildren<Button>())
            {
                if (!b.gameObject.activeInHierarchy || b.GetComponentInParent<ScrollRect>()) continue;
                var corners = new Vector3[4]; ((RectTransform)b.transform).GetWorldCorners(corners);
                var r = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                count++;
                if (r.width < 47.5f || r.height < 47.5f) { small++; Log("layout-small", context + " " + b.name + " " + r); }
                if (r.xMin < -1 || r.yMin < -1 || r.xMax > Screen.width + 1 || r.yMax > Screen.height + 1) { outside++; Log("layout-outside", context + " " + b.name + " " + r); }
            }
            Log("layout", $"{context}: controls={count} small={small} outside={outside} size={Screen.width}x{Screen.height}");
        }
    }
}
