using System.Collections;
using UnityEngine;

namespace HumanEye
{
    public partial class AutoPilot
    {
        bool freeChecked;

        /// -heFreeExplore: from the end card, a tour of Free Explore through the real buttons (orbit and zoom through the
        /// stage's input entry points, as a drag and the wheel would), with a capture at each stop.
        IEnumerator FreeExploreTour()
        {
            freeChecked = true;
            yield return ClickUI("end-explore"); yield return Real(2.5f);
            Check(stage.FreeExploring && cams.Active && cams.Active.name == Stage.OrbitShot, "Free Explore starts on the orbit view");
            yield return Capture("free_start");
            for (int i = 0; i < 12; i++) { stage.Orbit(new Vector2(40, -6)); yield return null; }
            yield return Real(.8f); yield return Capture("free_orbit");
            stage.RequestPartFromList("iris"); yield return Real(2.5f); yield return Capture("free_iris");
            yield return ClickUI("btn-section"); yield return Real(2.2f); yield return Capture("free_cut");
            stage.RequestPartFromList("lens"); yield return Real(2.5f); yield return Capture("free_lens");
            for (int i = 0; i < 8; i++) { stage.Zoom(-2.5f); yield return null; }
            yield return Real(.8f); yield return Capture("free_zoom");
            stage.UseControl("near"); yield return Real(3f); yield return Capture("free_focus_near");
            yield return ClickUI("free-inside"); yield return Real(1.5f);
            Check(stage.FreeInside, "Look from inside puts the view inside the eye");
            yield return Capture("free_inside");
            for (int i = 0; i < 10; i++) { stage.Orbit(new Vector2(-30, 8)); yield return null; }
            yield return Real(.6f); yield return Capture("free_inside_look");
            yield return ClickUI("free-inside"); yield return Real(1.2f);
            yield return ClickUI("free-journey");
            for (int i = 0; i < 6; i++) { yield return Real(2.5f); yield return Capture($"free_journey_{i}"); }
            for (float w = 0; w < 30 && cams.Active && cams.Active.name != Stage.OrbitShot; w += Time.unscaledDeltaTime) yield return null;
            yield return Real(1); yield return Capture("free_after_journey");
            yield return ClickUI("free-exit"); yield return Real(2);
            Check(!stage.FreeExploring && runner.State == LessonState.Complete, "Finish exploring returns to the end card");
            yield return Capture("free_exit");
        }
    }
}
