using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanEye
{
    /// Niko, the floating 3D companion (16 October 2026 brief; it replaces the companion picture card). One live character,
    /// always drawn by the lesson camera, never a card or a render texture:
    /// • not at all before his first line (Stage.NikoWaiting: the start screen and the opening, 21 October 2026 brief);
    /// • in his own shots (his medium shot, the closing) he stands at his spot in the room (NikoRig.Home*);
    /// • otherwise he floats beside the lens in a clear lower corner: camera-relative and the same size on screen at any lens,
    ///   turned three-quarters toward the middle, bobbing and swaying, lagging a touch behind camera turns so he is not glued
    ///   to the glass. He stays nearer than anything the shot shows, so nothing in the room can hide him and the background-only
    ///   depth of field, which starts behind the subject, never reaches him. He rises above captions, labels and panels that
    ///   would sit under him and keeps off the explained structure;
    /// • he flies between his spot and his corner, or fades in there after a cut. Reduced motion: no flights, a calmer float.
    [DefaultExecutionOrder(1100)]   // after the camera brain and ViewportShift (1000): the frame's final camera
    public class NikoFloatingCompanion : MonoBehaviour
    {
        public Stage stage;
        [Tooltip("Floating height as a share of the view height.")] [Range(.15f, .45f)] public float heightShare = .28f;
        [Tooltip("Largest distance from the lens (m); he comes nearer when the shot's subject is near.")] public float maxDepth = .9f;
        [Tooltip("His height at scale 1 (m).")] public float height = 1.2f;
        [Tooltip("Self-light while floating, so a dark set never turns him black (emission over his own albedo).")] [Range(0, .5f)] public float glowLift = .05f;
        [Tooltip("Soft hover glow under his feet while he floats.")] public Renderer hoverGlow;
        [Tooltip("A few sparkles around him while he floats.")] public ParticleSystem motes;
        [Tooltip("Rim light that only lights Niko (its own rendering layer).")] public Light rim;

        public bool Floating { get; private set; }
        public bool MainReadable { get; private set; }
        /// He floats on the right (cinematics, where the lesson column is hidden); pointing goes toward the middle.
        public bool OnRight { get; private set; }
        /// Where he floats, in screen pixels: the chosen slot, and the box his mesh actually covers this frame (audits).
        public Rect ScreenRect { get; private set; }
        public Rect DrawnRect { get; private set; }
        /// Floating, his drawn box must lie in the picture: inside the screen, his head clear of the top, and no more than a
        /// sixth of him under the letterbox during a cinematic.
        public bool Visible => MainReadable || Floating && stage.niko.gameObject.activeInHierarchy && stage.niko.skin && stage.niko.skin.enabled
                                                && DrawnRect.width > 0 && DrawnRect.xMin >= -2 && DrawnRect.xMax <= Screen.width + 2 && DrawnRect.yMax <= Screen.height
                                                && DrawnRect.yMin >= (stage.CinematicPlaying ? Letterbox * Screen.height : 0) - DrawnRect.height / 6;
        public float Depth { get; private set; }
        /// His arrival growth, 0 to 1 (Stage.NikoArrive grows him from nothing): applied on top of wherever he stands or floats,
        /// so a half-grown Niko is still judged at his full size and never sent to the corner.
        public float Grow { get; set; } = 1;
        /// His scale this frame before the arrival growth (at his spot or floating).
        public float Size { get; private set; } = 1;
        const float Letterbox = .115f;   // the cinematic bars' share of the screen height (UiPass)
        const float RimSize = .18f;      // his floating height (m) at which the rim's intensity was tuned (lesson shots, 0.9 m out)
        public string Location => MainReadable ? "MAIN" : Floating ? "FLOAT" : "OFF";

        float blend, presence = 1, clock, rimBase, headUp = .62f;
        Vector2 slot = new(-1, 0), slotVelocity, lag;
        Vector3 lastPosition, lastHome; Quaternion lastRotation; bool primed, wasCinematic;
        Renderer[] subjects;
        ShotLook look;
        readonly List<(Rect rect, float weight)> avoid = new(), ui = new();
        float columnLeft = float.MaxValue;
        readonly Plane[] frustum = new Plane[6];
        readonly Vector3[] corners = new Vector3[4];
        MaterialPropertyBlock block;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor"), AlphaId = Shader.PropertyToID("_Alpha");
        static readonly string[] Panels = { "subtitle-panel", "right-column", "btn-ask", "toast", "topbar" };

        void Start()
        {
            block = new MaterialPropertyBlock();
            if (rim) rimBase = rim.intensity;
            var roots = new List<Renderer>();   // what a shot can show: the eye, the person, the brain, the retina world, the light's objects
            foreach (var t in new Transform[] { stage.eye.transform, stage.human.transform, stage.brainRoot, stage.micro.transform, stage.appleRoot, stage.treeRoot, stage.lampRoot })
                if (t) roots.AddRange(t.GetComponentsInChildren<Renderer>(true));
            subjects = roots.ToArray();
            look = FindFirstObjectByType<ShotLook>();
            var cam = stage.hud.mainCamera; cam.cullingMask |= 1 << stage.niko.gameObject.layer;   // the lesson camera always draws him
        }

        void LateUpdate()
        {
            var niko = stage.niko; var cam = stage.hud.mainCamera;
            if (!niko || !cam) return;
            float dt = Time.unscaledDeltaTime, calm = stage.Short ? .35f : 1; clock += dt;
            bool running = stage.IntroPlaying || stage.runner.Started;
            var shot = stage.cams.Active;
            // His own shots (the closing shot frames the eye: he floats there, as the picture did before him)
            bool ownShot = shot && (shot.name.Contains("Niko") || stage.IntroPlaying && stage.IntroTime >= 29.5f);
            // His spot in the room seen through this frame's camera (the head joint's live height, so a gesture changes nothing).
            if (niko.head && niko.transform.localScale.y > .01f) headUp = niko.transform.InverseTransformPoint(niko.head.position).y;   // not while he grows from nothing
            var head = niko.HomePosition + niko.HomeRotation * Vector3.up * (headUp * niko.HomeScale);
            var face = cam.WorldToViewportPoint(head);
            float faceHeight = Mathf.Abs(cam.WorldToViewportPoint(head + cam.transform.up * .3f * niko.HomeScale).y - face.y);
            float give = Floating ? 0 : .06f;   // once in the room he stays a little longer, so a moving camera never flips him back and forth
            MainReadable = niko.gameObject.activeInHierarchy && ownShot && face.z > cam.nearClipPlane && faceHeight >= .07f - give * .3f && faceHeight <= .32f
                           && face.x > .12f - give && face.x < .74f + give && face.y > .23f - give && face.y < .8f + give;
            bool wanted = running && !MainReadable && !stage.NikoWaiting;   // until his first line he is not shown at all
            if (wanted && !niko.gameObject.activeSelf) niko.gameObject.SetActive(true);
            var rotation = cam.transform.rotation;
            bool cut = !primed || (cam.transform.position - lastPosition).sqrMagnitude > 1 || Quaternion.Angle(rotation, lastRotation) > 15;
            if (wanted != Floating)
            {
                Floating = niko.floating = wanted;
                // Into the corner: he flies there from his spot when it is in view; after a cut (or in reduced motion) he
                // appears there instead. Home again: he flies back (reduced motion: at once).
                bool homeSeen = face.z > 0 && face.x > -.1f && face.x < 1.1f && face.y > -.1f && face.y < 1.1f;
                if (wanted) slot.x = -1;   // a fresh corner for this shot, never the last one's
                if (wanted && (cut || !homeSeen || stage.Short || !primed || Grow < 1)) { blend = 1; presence = stage.Short || !primed ? 1 : 0; }   // arriving: there at once
                if (!wanted && (stage.Short || !running || stage.NikoWaiting)) blend = 0;   // the menu cuts to the gate: no swoop across the room
            }
            if (Floating && stage.CinematicPlaying != wasCinematic) { slot.x = -1; presence = 0; }   // the letterbox came or went: his corner for it, at once
            wasCinematic = stage.CinematicPlaying;
            if (Floating && (niko.HomePosition - lastHome).sqrMagnitude > 1) blend = 1;   // his spot jumped (a chapter's Place): never fly from it through the room
            lastHome = niko.HomePosition;
            blend = Mathf.MoveTowards(blend, Floating ? 1 : 0, dt / .8f);
            presence = Mathf.MoveTowards(presence, 1, dt / .35f);

            // ---- the camera this frame: a cut resets the lag, a turn leaves him a touch behind
            Vector2 wantLag = Vector2.zero;
            if (!cut && dt > 0)
            {
                (Quaternion.Inverse(lastRotation) * rotation).ToAngleAxis(out float angle, out var axis);
                if (angle > 180) angle -= 360;
                var spin = axis * (angle / dt);   // degrees per second about the camera's own axes
                wantLag = Vector2.ClampMagnitude(new Vector2(-spin.y, spin.x) * .0006f, .025f) * calm;
            }
            lag = cut ? Vector2.zero : Vector2.Lerp(lag, wantLag, 1 - Mathf.Exp(-dt * 4));
            lastPosition = cam.transform.position; lastRotation = rotation;

            // ---- where he floats: nearer than the subject, the same size on screen, in the clearest corner slot
            float near = Scan(cam);
            if (look) near = Mathf.Min(near, look.BlurStart);   // and in front of the background blur
            float depth = Depth = Mathf.Clamp(near * .45f, cam.nearClipPlane * 8, maxDepth);
            float halfHeight = depth * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
            float scale = 2 * halfHeight * heightShare / height;
            float w = heightShare * .9f / cam.aspect;   // his width on screen: the big head is 0.87 of his height
            if (Floating) Choose(cam, w, cut, stage.CinematicPlaying && !(stage.EyeBrainPlaying && stage.brain.Visible && !stage.human.gameObject.activeSelf));
            bool wasRight = OnRight; OnRight = targetRight;
            if (OnRight != wasRight) niko.MirrorPoint(OnRight);
            ScreenRect = new Rect((slot.x - w * .5f) * Screen.width, slot.y * Screen.height, w * Screen.width, heightShare * Screen.height);

            float bob = (Mathf.Sin(clock * 1.25f) * .022f + Mathf.Sin(clock * 3.1f + 1) * .006f) * heightShare * calm;
            float drift = Mathf.Sin(clock * .37f) * .006f * calm;
            var feet = cam.ViewportToWorldPoint(new Vector3(slot.x + drift + lag.x, slot.y + bob + lag.y, depth));
            var local = cam.transform.InverseTransformPoint(feet + cam.transform.up * (height * scale * .5f));
            var facing = Quaternion.AngleAxis(OnRight ? 30 : -30, Vector3.up) * new Vector3(-local.x, 0, -local.z).normalized;   // three-quarters toward the middle
            var sway = Quaternion.Euler(1.5f * Mathf.Sin(clock * .65f) * calm, 4 * Mathf.Sin(clock * .5f) * calm, 2 * Mathf.Sin(clock * .8f) * calm);
            var floatRotation = rotation * Quaternion.LookRotation(facing, Vector3.up) * sway;

            float e = Mathf.SmoothStep(0, 1, blend), show = Mathf.Lerp(.7f, 1, 1 - Mathf.Pow(1 - presence, 3));
            niko.transform.SetPositionAndRotation(Vector3.Lerp(niko.HomePosition, feet, e), Quaternion.Slerp(niko.HomeRotation, floatRotation, e));
            Size = Mathf.Lerp(niko.HomeScale, scale, e) * (Floating ? show : 1);
            niko.transform.localScale = Vector3.one * (Size * Grow);
            niko.lens = cam.transform.position;
            DrawnRect = Drawn(cam, niko);
            primed = true;

            // ---- floating look: self-light, hover glow, motes, a rim that lights only him; no shadow thrown from the lens
            float f = e;
            if (niko.skin)
            {
                niko.skin.shadowCastingMode = f > .5f ? ShadowCastingMode.Off : ShadowCastingMode.On;
                niko.skin.GetPropertyBlock(block); block.SetColor(EmissionId, Color.white * (glowLift * f)); niko.skin.SetPropertyBlock(block);
            }
            if (hoverGlow)
            {
                hoverGlow.enabled = f > .01f;
                hoverGlow.GetPropertyBlock(block); block.SetFloat(AlphaId, f * (.45f + .1f * Mathf.Sin(clock * 2.1f))); hoverGlow.SetPropertyBlock(block);
            }
            if (motes) { if (f > .5f && !motes.isEmitting) motes.Play(); else if (f <= .5f && motes.isEmitting) motes.Stop(); }
            if (rim)
            {
                float size = height * niko.transform.localScale.y;   // behind and above him as the camera sees him, sized with him
                // The light comes nearer as he shrinks toward the lens: keep the light reaching him constant (inverse square)
                rim.enabled = f > .01f; rim.intensity = rimBase * f * (size * size) / (RimSize * RimSize);
                rim.transform.position = niko.transform.position + cam.transform.up * (size * 1.05f) + cam.transform.forward * (size * .7f);
                rim.range = size * 2.2f;
            }
        }

        /// What the shot shows: the nearest surface (he floats in front of it), and where each structure or prop lies on screen
        /// (he keeps off them; the ones that fill much of the frame, the backdrop or a close-up's globe, cannot be avoided).
        float Scan(Camera cam)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, frustum);
            float best = maxDepth / .45f, screen = Screen.width * Screen.height; var eye = cam.transform.position;
            seen.Clear();
            foreach (var r in subjects)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(frustum, r.bounds)) continue;
                float d = Vector3.Distance(eye, r.bounds.ClosestPoint(eye));
                if (d > .02f && d < best) best = d;   // 0: the lens is inside a big bound (the retina world); its own parts decide
                if (!Floating || r is ParticleSystemRenderer) continue;
                var box = Projected(cam, r.bounds);
                if (box.width > 0 && box.width * box.height < screen * .4f) seen.Add(box);
            }
            return best;
        }
        readonly List<Rect> seen = new(), labelRects = new();

        /// The clearest slot for his feet (viewport), scored by what would lie under him: panels and captions, labels, every
        /// structure and prop in view, most of all the explained one. Two lower corners: in a cinematic the screen's (the right
        /// unless the brain floats there), in a lesson the 3D area's (the left preferred, or just left of the lesson column);
        /// each low above the margin or the letterbox, or raised. He slides up or down on his side; a change of side, rarer,
        /// makes him reappear there (never a slide across the picture); a cut places him at once.
        void Choose(Camera cam, float w, bool cut, bool preferRight)
        {
            // The interface in pixels, read only while the canvas overlays the screen: a QA capture lends it to its own camera
            // for a frame, when its corners are world positions (then the last reading holds).
            if (stage.hud.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                ui.Clear();
                foreach (var name in Panels) Add(stage.hud.Element(name), 4);
                labelRects.Clear(); stage.hud.labels?.ScreenRects(labelRects);   // a plain class, not a UnityEngine.Object
                foreach (var r in labelRects) ui.Add((r, 2));
                var column = stage.hud.Element("right-column");
                if (column && column.gameObject.activeInHierarchy) { column.GetWorldCorners(corners); columnLeft = corners[0].x; }
                else columnLeft = Screen.width;
            }
            avoid.Clear(); avoid.AddRange(ui);
            foreach (var r in seen) avoid.Add((r, 2));
            var focus = stage.CurrentFocus;   // the explained structure, piece by piece (one box round all the rods would cover everything)
            if (focus && focus.main != null)
                foreach (var r in focus.main)
                    if (r && r.enabled && r.gameObject.activeInHierarchy) { var box = Projected(cam, r.bounds); if (box.width > 0) avoid.Add((box, 3)); }
            bool cinematic = stage.CinematicPlaying;
            float px = 24f / Screen.width, floor = cinematic ? Letterbox + .02f : 24f / Screen.height;
            float right = Mathf.Max((cinematic ? 1 : Mathf.Clamp01(columnLeft / Screen.width)) - px - w * .5f, px + w * .5f);
            Vector2 best = slot; float bestScore = float.MaxValue; bool bestRight = targetRight;
            for (int side = 0; side < 2; side++)
                foreach (float lift in Lifts)
                {
                    bool onRight = side == 1;
                    var at = new Vector2(onRight ? right : px + w * .5f, floor + lift);
                    float score = Score(at, w, floor, onRight, preferRight);
                    if (score < bestScore) { bestScore = score; best = at; bestRight = onRight; }
                }
            if (cut || slot.x < 0) { slot = target = best; targetRight = bestRight; slotVelocity = Vector2.zero; return; }
            float now = slot.y < floor - .005f ? float.MaxValue : Score(slot, w, floor, targetRight, preferRight);   // under a letterbox that came in: move
            if (bestRight != targetRight)
            {
                if (bestScore < now - .35f) { presence = 0; slot = target = best; targetRight = bestRight; slotVelocity = Vector2.zero; }
                return;
            }
            if (bestScore < now - .1f) target = best;
            slot = Vector2.SmoothDamp(slot, target, ref slotVelocity, stage.Short ? .15f : .45f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
        Vector2 target = new(-1, 0);
        bool targetRight;
        static readonly float[] Lifts = { 0, .08f, .16f, .24f };

        float Score(Vector2 at, float w, float floor, bool onRight, bool preferRight) => Covered(at, w) + (at.y - floor) * .5f + (onRight != preferRight ? .2f : 0);

        float Covered(Vector2 at, float w)
        {
            var me = new Rect((at.x - w * .5f) * Screen.width, at.y * Screen.height, w * Screen.width, heightShare * Screen.height);
            float area = me.width * me.height, sum = 0, margin = me.height * .06f;   // touching reads as sitting on him
            me = Rect.MinMaxRect(me.xMin - margin, me.yMin, me.xMax + margin, me.yMax + margin);
            foreach (var (r, weight) in avoid)
            {
                float x = Mathf.Min(me.xMax, r.xMax) - Mathf.Max(me.xMin, r.xMin), y = Mathf.Min(me.yMax, r.yMax) - Mathf.Max(me.yMin, r.yMin);
                // his share covered, or half the thing's own share hidden: a small thing hidden whole (the apple, a button,
                // a label) counts as much as a big one half under him
                if (x > 0 && y > 0) sum += weight * Mathf.Max(x * y / area, .5f * x * y / Mathf.Max(1, r.width * r.height));
            }
            return sum;
        }

        void Add(RectTransform e, float weight)
        {
            if (!e || !e.gameObject.activeInHierarchy) return;
            e.GetWorldCorners(corners);   // a screen-space overlay canvas: world corners are pixels
            ui.Add((Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y), weight));
        }

        static Rect Projected(Camera cam, Renderer[] renderers)
        {
            var all = Rect.zero;
            if (renderers == null) return all;
            foreach (var r in renderers)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var one = Projected(cam, r.bounds);
                if (one.width <= 0) continue;
                all = all.width <= 0 ? one : Rect.MinMaxRect(Mathf.Min(all.xMin, one.xMin), Mathf.Min(all.yMin, one.yMin), Mathf.Max(all.xMax, one.xMax), Mathf.Max(all.yMax, one.yMax));
            }
            return all;
        }

        /// The box his figure covers on screen (pixels): feet, crown, the sides of his big head and both hands. (His mesh's
        /// bounding box is far looser than his silhouette, most of all close to the lens.)
        Rect Drawn(Camera cam, NikoRig niko)
        {
            var t = niko.transform; float s = t.lossyScale.y;
            var eyes = niko.head ? niko.head.position + t.up * (niko.eyeAbove * s) : t.position + t.up * (.86f * s);
            figure[0] = t.position; figure[1] = t.position + t.up * (height * s);
            figure[2] = eyes + t.right * (.34f * s); figure[3] = eyes - t.right * (.34f * s);
            figure[4] = niko.handL ? niko.handL.position : eyes; figure[5] = niko.handR ? niko.handR.position : eyes;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var w in figure)
            {
                var p = cam.WorldToScreenPoint(w);
                if (p.z <= 0) return Rect.zero;
                x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
        readonly Vector3[] figure = new Vector3[6];

        /// A world box's screen rectangle (pixels; its corners behind the lens are left out).
        static Rect Projected(Camera cam, Bounds b)
        {
            bool any = false; float x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            for (int i = 0; i < 8; i++)
            {
                var p = cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, (i & 4) / 2 - 1)));
                if (p.z <= 0) continue;
                if (!any) { x0 = x1 = p.x; y0 = y1 = p.y; any = true; }
                else { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); }
            }
            return any ? Rect.MinMaxRect(x0, y0, x1, y1) : Rect.zero;
        }

        void OnDisable()
        {
            if (stage && stage.niko) stage.niko.floating = false;
            if (rim) rim.enabled = false;
            if (hoverGlow) hoverGlow.enabled = false;
        }
    }
}
