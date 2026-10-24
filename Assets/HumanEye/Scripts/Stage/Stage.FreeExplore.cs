using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HumanEye
{
    /// Free Explore (22 October 2026 brief): after the investigation the eye is the learner's own. A dedicated shot
    /// (FX_Orbit, Builder.ApplyRedesign) orbits the eye under the learner's drag and zooms from the whole set-up to close
    /// detail; zooming past the cornea, or "Look from inside", puts the lens in the vitreous looking out through the lens and
    /// pupil. The eye can be cut open; any part tapped, labelled or picked from the list is isolated, labelled and explained
    /// again in Niko's own lesson lines; the light journey replays on request; Near / Distant and the slider move the apple
    /// so the lens and the retinal image show accommodation live.
    public partial class Stage
    {
        public const string OrbitShot = "FX_Orbit";
        public bool FreeExploring { get; private set; }
        public bool FreeInside { get; private set; }
        public string FreeSelected { get; private set; }
        bool fxBusy;
        float fxYaw, fxPitch = 12, fxDistance = 4, fxLookYaw, fxLookPitch;
        Coroutine fxTalk;

        /// Every structure with its lesson lines: the explored parts of Scenes 2 and 3, then the nerve and the visual area.
        IEnumerable<(string id, string label, string[] cues)> FreeParts() =>
            runner.chapters.Where(c => c.id is "Scene02" or "Scene03").SelectMany(c => c.explore)
                .Select(e => (e.partId, e.label, e.steps.Where(s => s.kind == StepKind.Say).Select(s => s.key).ToArray()))
                .Append(("optic_nerve", "Optic nerve", new[] { "S04_L05", "S04_L06" }))
                .Append(("visual_area", "Visual area", new[] { "S04_L07" }));

        float EyeRadius => eye.transform.lossyScale.x;

        public void StartFreeExplore()
        {
            if (FreeExploring || !cams.Has(OrbitShot)) return;
            FreeExploring = true; FreeInside = false; fxBusy = false; FreeSelected = null;
            narrator.Stop();
            // The complete set-up (the apple and its light, the eye, its nerve and the brain), the eye closed and whole.
            brainRoot.gameObject.SetActive(true); eye.SetNerveReveal(1);
            ShowObject("tree", false); Optic("apple", 7, 5); rays.showImage = false;
            RestoreParts();
            foreach (var p in FreeParts()) { var part = Part(p.id); if (part) part.Selectable = true; }
            hud.ShowPartList(FreeParts().Select(p => new ExploreEntry { partId = p.id, label = p.label }).ToArray());
            Simulation = "distance";
            // Start the orbit from where the camera is now, so the move into it is short.
            var from = hud.mainCamera.transform.position - eye.transform.position;
            fxDistance = Mathf.Clamp(from.magnitude / EyeRadius, 2.4f, 10); fxYaw = Mathf.Atan2(from.x, from.z) * Mathf.Rad2Deg;
            fxPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(from.normalized.y, -1, 1)) * Mathf.Rad2Deg, -30, 50);
            FreeFrame(true);
            runner.RunSide(cams.MoveTo(OrbitShot, Short ? 0 : 1.6f));
            FreeLabels();
            sfx.Play("unlock", .5f);
            if (narrator.database.Get("free_intro")?.clip) StartCoroutine(narrator.Play("free_intro", NarrationSource.Answer));
            hud.Refresh();
        }

        public void EndFreeExplore()
        {
            if (!FreeExploring) return;
            if (fxTalk != null) StopCoroutine(fxTalk);
            narrator.Stop();
            SetFreeInside(false);
            FreeExploring = false; FreeSelected = null; fxBusy = false;
            foreach (var p in AllParts()) p.Selectable = false;
            RestoreParts(); ShowLabels(null); hud.ShowPartList(null); hud.Info(null); Simulation = null;
            rays.showImage = false; hud.HideInsets();
            if (eye.IsCut) runner.RunSide(eye.Cutaway(false, Short ? .2f : .8f));
            runner.RunSide(cams.MoveTo("S11_Closing", Short ? 0 : 1.4f));
            hud.Refresh();
        }

        /// Clickable labels on every structure in view (the inner ones once the eye is cut open).
        void FreeLabels()
        {
            hud.labels.Clear();
            if (!SaveSystem.Data.settings.labels || FreeInside) return;
            foreach (var (id, label, _) in FreeParts())
            {
                var p = Part(id);
                if (p && p.Visible && (eye.IsCut || id is "sclera" or "cornea" or "iris" or "pupil" or "optic_nerve" or "visual_area"))
                    hud.labels.Add(label + (id == FreeSelected ? "  ●" : ""), eye.LabelOf(p), eye.FocusOf(p), id);
            }
        }

        void FreeSelect(string id)
        {
            if (fxBusy) return;
            var part = Part(id); if (!part) return;
            var entry = FreeParts().FirstOrDefault(p => p.id == id);
            FreeSelected = id;
            RestoreParts(); part.SetGlow(1); sfx.Play(sfx.Clip("hl_" + id) ? "hl_" + id : "select", .5f);   // the whole eye stays solid while it turns: the part lights up
            FreeLabels();
            hud.Info(entry.label.ToUpperInvariant() + "\n" + string.Join(" ", entry.cues.Select(c => narrator.database.Get(c)?.text)));
            if (fxTalk != null) StopCoroutine(fxTalk);
            narrator.Stop();
            fxTalk = StartCoroutine(Explain(entry.cues));
            hud.Refresh();
        }

        IEnumerator Explain(string[] cues)
        {
            foreach (var c in cues) { OnCue(c); yield return narrator.Play(c, NarrationSource.Answer); }
        }

        public void ToggleFreeInside() { if (FreeExploring && !fxBusy) { SetFreeInside(!FreeInside); hud.Refresh(); } }

        /// Inside the closed eye: the interior is lit (its own warm light, no closed-eye darkening) and the labels step aside.
        void SetFreeInside(bool inside)
        {
            if (FreeInside == inside) return;
            FreeInside = inside; fxLookYaw = fxLookPitch = 0;
            if (inside) { var shot = cams.Get(OrbitShot); if (shot) shot.transform.position = eye.transform.TransformPoint(new Vector3(0, 0, -.35f)); }   // through the wall at once, never a glide across it
            if (inside && eye.IsCut) eye.SetCutInstant(false);   // inside a whole eye, looking out through lens and pupil
            Shader.SetGlobalFloat("_HE_InteriorDark", inside ? 0 : eye.IsCut ? 0 : 1);
            if (interiorLight) { interiorLight.enabled = inside || interiorOn; if (inside) interiorLight.transform.position = eye.transform.position; }
            if (!inside) { fxDistance = Mathf.Max(fxDistance, 2.4f); cams.ResetZoom(); }
            sfx.Play(inside ? "softwhoosh" : "isolate", .45f);
            FreeLabels();
        }

        public void FreeToggleSection()
        {
            if (!FreeExploring || fxBusy) return;
            SetFreeInside(false);
            runner.RunSide(FreeCut(!eye.IsCut));
        }

        IEnumerator FreeCut(bool open)
        {
            fxBusy = true;
            yield return eye.Cutaway(open, Short ? .2f : 1.1f);
            fxBusy = false; FreeLabels(); hud.Refresh();
        }

        /// The light's whole journey, once, with the lesson's own camera ride; then back to the learner's view.
        public void FreeJourney()
        {
            if (!FreeExploring || fxBusy) return;
            runner.RunSide(FreeJourneyRun());
        }

        IEnumerator FreeJourneyRun()
        {
            fxBusy = true;
            if (fxTalk != null) StopCoroutine(fxTalk);
            narrator.Stop(); SetFreeInside(false); RestoreParts(); hud.labels.Clear(); hud.Info(null);
            bool wasCut = eye.IsCut;
            if (!wasCut) yield return eye.Cutaway(true, Short ? .2f : 1f);
            Optic("apple", 7, 0);
            yield return JourneyReplay(false);
            if (!wasCut) yield return eye.Cutaway(false, Short ? .2f : 1f);
            cams.ResetZoom();
            fxBusy = false; FreeLabels(); hud.Refresh();   // FreeFrame glides back to the learner's view
        }

        /// Near / Distant / slider in Free Explore: the apple glides there and the lens adapts at its own pace, while the
        /// retinal image shows the blur and the refocus.
        IEnumerator FreeFocus(float to)
        {
            if (!rays.showImage) { rays.showImage = true; hud.ShowInsets(rays.objectCamera.targetTexture, false); }
            float from = ObjectDistance;
            yield return Routine.Tween(Short ? .3f : 1.4f, t => SetObjectDistance(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t))), true);
            hud.SetLensState(to < 10 ? "Near apple · lens rounder" : "Distant apple · lens flatter");
        }

        bool FreeOrbit(Vector2 delta)
        {
            if (!FreeExploring) return false;
            if (fxBusy) return true;
            if (FreeInside) { fxLookYaw = Mathf.Clamp(fxLookYaw + delta.x * .12f, -70, 70); fxLookPitch = Mathf.Clamp(fxLookPitch - delta.y * .12f, -55, 55); }
            else { fxYaw += delta.x * .3f; fxPitch = Mathf.Clamp(fxPitch - delta.y * .25f, -80, 80); }
            return true;
        }

        bool FreeZoom(float delta)
        {
            if (!FreeExploring) return false;
            if (fxBusy) return true;
            if (FreeInside) { if (delta > 0 && cams.Active && cams.Active.Lens.FieldOfView >= cams.Fitted(cams.AuthoredFov(cams.Active)) * 1.2f) SetFreeInside(false); else cams.Zoom(delta); return true; }
            if (delta < 0 && fxDistance <= 1.75f) { SetFreeInside(true); return true; }   // zoom right through the cornea
            fxDistance = Mathf.Clamp(fxDistance * (1 + delta * .04f), 1.7f, 16);
            return true;
        }

        void FreeResetView()
        {
            SetFreeInside(false); FreeSelected = null; RestoreParts(); hud.Info(null);
            fxDistance = 4; fxPitch = 12; cams.ResetZoom(); FreeLabels();
        }

        /// The orbit shot follows the learner's view every frame (Stage.Update), outside around the eye or inside it, gliding
        /// (a short exponential ease) so drags, zooms and the way back from the journey never jump.
        void FreeFrame(bool instant = false)
        {
            var shot = cams.Get(OrbitShot); if (!shot) return;
            var e = eye.transform; Vector3 pos; Quaternion rot;
            if (FreeInside)
            {
                var look = Quaternion.AngleAxis(fxLookYaw, e.up) * Quaternion.AngleAxis(-fxLookPitch, e.right) * e.forward;
                pos = e.TransformPoint(new Vector3(0, 0, -.35f)); rot = Quaternion.LookRotation(look, e.up);
            }
            else
            {
                var dir = Quaternion.Euler(-fxPitch, fxYaw, 0) * Vector3.forward;
                pos = e.position + dir * fxDistance * EyeRadius; rot = Quaternion.LookRotation(-dir, Vector3.up);
            }
            float k = instant || Short ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 9);
            shot.transform.SetPositionAndRotation(Vector3.Lerp(shot.transform.position, pos, k), Quaternion.Slerp(shot.transform.rotation, rot, k));
        }
    }
}
