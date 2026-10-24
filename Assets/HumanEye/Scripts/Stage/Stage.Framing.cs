using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanEye
{
    /// Camera focus during every explanation (cinematic pass, S5a). Which structure a line is about (for the AutoPilot
    /// framing audit and ShotLook's depth of field), an isolation variant that keeps named neighbours solid, and the
    /// object / lens beats Scenes 5–7 use so the thing a line names is framed and visible while it is spoken.
    /// Registered through RegisterFraming(), called from RegisterOptics() (Stage.Optics.cs).
    public partial class Stage
    {
        /// Lines that name a structure Niko does not point at (not in CuePart) but which the framing audit should check.
        static readonly Dictionary<string, string> FramingPart = new()
        {
            ["S03_L03"] = "aqueous", ["S03_L05"] = "lens", ["S03_L06"] = "lens", ["S03_L08"] = "vitreous", ["S03_L10"] = "retina",
            ["S04_L02"] = "retina", ["S04_L04"] = "optic_nerve", ["S04_L06"] = "optic_nerve", ["S04_L08"] = "optic_nerve",
            ["S05_L06"] = "pupil", ["S05_L07"] = "pupil", ["S06_L02"] = "lens", ["S06_L03"] = "lens", ["S06_L04"] = "lens",
            ["S07_L03"] = "retina", ["S08_L01"] = "rods", ["S08_L03"] = "rods", ["S08_L05"] = "cones", ["S08_L06"] = "rods",
            ["S09_L02"] = "optic_nerve", // S09_L04 compares the original apple with the retinal/perception insets.
        };

        /// The structure a ledger line is about (Niko's pointing table first, then the audit table), or null.
        public static string PartOf(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return null;
            if (CuePart.TryGetValue(cue, out var id)) return id;
            return FramingPart.TryGetValue(cue, out id) ? id : null;
        }

        static readonly int HighlightId = Shader.PropertyToID("_Highlight"), AlphaId = Shader.PropertyToID("_Alpha");

        /// The live material of a part (AnatomyPart instances its materials in Awake, so sharedMaterial is the live copy).
        static Material LiveMaterial(AnatomyPart p)
        {
            if (!p || p.main == null) return null;
            foreach (var r in p.main) { if (!r) continue; var m = r.sharedMaterial; if (m) return m; }
            return null;
        }

        /// Current highlight (lesson glow or hover) of a part, read back from its material; 0 when unknown.
        public static float GlowOf(AnatomyPart p) { var m = LiveMaterial(p); return m && m.HasProperty(HighlightId) ? m.GetFloat(HighlightId) : 0; }

        /// Current rendered alpha of a part (base alpha × lesson alpha), read back from its material.
        public static float AlphaOf(AnatomyPart p) { var m = LiveMaterial(p); return m && m.HasProperty(AlphaId) ? m.GetFloat(AlphaId) : (p && p.Visible ? 1 : 0); }

        /// World-space radius of a part's visible geometry (union of its main renderer bounds); 0 when nothing is drawn.
        public static float BoundsRadius(AnatomyPart p)
        {
            if (!p || p.main == null) return 0;
            bool any = false; var b = new Bounds();
            foreach (var r in p.main)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? Mathf.Min(b.extents.magnitude, 1.5f) : 0;
        }

        /// The structure being explained right now: the explored part, else the eye layer with the strongest highlight
        /// (Isolate, glow, sequence and hints all raise it). Null when nothing is emphasised.
        public AnatomyPart CurrentFocus
        {
            get
            {
                if (runner && runner.Focused != null) { var f = Part(runner.Focused.partId); if (f) return f; }
                AnatomyPart best = null; float top = .08f;
                foreach (var p in eye.parts) { if (!p) continue; float g = GlowOf(p); if (g > top) { top = g; best = p; } }
                return best;
            }
        }

        void RegisterFraming()
        {
            actions["isolateKeep"] = (ids, glow) => { IsolateKeep(ids, glow); return null; };
            actions["nearApproach"] = (_, __) => NearApproach();
            actions["distantApproach"] = (_, __) => DistantApproach();
            actions["lensFocus"] = (mode, __) => LensFocus(mode != "distant");
            actions["focusView"] = (mode, __) => { FocusView(mode != "off"); return null; };
            actions["ciliaryDemo"] = (_, __) => CiliaryDemo();
        }

        [Tooltip("The ciliary muscle ring around the lens (Builder.ApplyRedesign); null in older scenes.")]
        public CiliaryRig ciliary;

        /// What the eye sees during the lens demonstration: the retinal-image popup, blurred by the live focus error (Hud),
        /// with the light carried on to the retina so the rays show where they meet.
        void FocusView(bool on)
        {
            rays.showImage = on;
            if (on) { hud.ShowInsets(rays.objectCamera.targetTexture, false); runner.RunSide(LightTravel(5, 1)); }
            else { hud.HideInsets(); if (ciliary) ciliary.Shown = false; }
        }

        void CiliaryLabel(string text)
        {
            if (!ciliary || !SaveSystem.Data.settings.labels) return;
            ciliary.Shown = true;
            hud.labels.Add(text, ciliary.labelAnchor, ciliary.focus, null);
        }

        /// Under the ciliary line: the muscle tightens (rounder lens) and relaxes (flatter lens) once, labelled as it goes.
        IEnumerator CiliaryDemo()
        {
            float start = eye.accommodationTarget;
            foreach (var (target, text) in new[] { (1f, "Ciliary muscle tightens · lens rounder"), (0f, "Ciliary muscle relaxes · lens flatter") })
            {
                ShowLabels("focus:lens"); CiliaryLabel(text);
                sfx.Play("lensfocus", .4f);
                eye.accommodationRate = Short ? 1.5f : .45f;
                eye.SetAccommodation(target, false);
                yield return Settle(() => Mathf.Abs(eye.Accommodation - target) < .01f);
                yield return new Wait(Short ? .2f : .6f, true);
            }
            eye.SetAccommodation(start, false);
            eye.accommodationRate = .45f;
        }

        /// Isolate the first structure but keep the named neighbours solid with a softer glow: the line names them too
        /// ("…through the aqueous humour and enters through the pupil"; "…the lens and vitreous humour… reaches the retina").
        void IsolateKeep(string ids, float glow)
        {
            var list = ids.Split(',');
            Isolate(list[0]);
            for (int i = 1; i < list.Length; i++)
            {
                var p = Part(list[i]); if (!p) continue;
                p.SetAlpha(1); p.SetGlow(glow > 0 ? glow : .3f);
            }
        }
    }
}
