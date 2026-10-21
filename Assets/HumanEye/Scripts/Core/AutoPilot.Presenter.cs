using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye
{
    public partial class AutoPilot
    {
        public static int PresenterFrames { get; private set; }
        public static int PresenterFailures { get; private set; }
        /// Every frame of the running module (narrated or silent) must show Niko; and his mouth must move only while he speaks.
        public static int PresenceFrames { get; private set; }
        public static int PresenceFailures { get; private set; }
        public static float MouthSpeakingMean => mouthSpeakingFrames > 0 ? mouthSpeakingSum / mouthSpeakingFrames : 0;
        public static float MouthSilentMax { get; private set; }
        static float mouthSpeakingSum, silentFor; static int mouthSpeakingFrames;
        NikoFloatingCompanion presenter;
        bool auditPresenter;
        bool closingSampled, nikoWasWaiting = true;

        void LateUpdate()
        {
            if (!runner || log == null || Done) return;
            if (!presenter) presenter = stage.Companion;
            AudioSample();
            if (nikoWasWaiting && !stage.NikoWaiting)
                Log("niko_arrive", $"{narrator.Current?.id ?? narrator.Source.ToString()} {runner.Chapter?.id} shot={stage.cams.Active?.name} {(presenter ? presenter.Location : "missing")}");
            nikoWasWaiting = stage.NikoWaiting;
            if ((runner.Started || stage.IntroPlaying) && !Clock.Paused)
            {
                PresenceFrames++;
                // Niko is off until his first line (21 October 2026 brief), then on screen in every frame
                if (stage.NikoWaiting ? stage.niko.gameObject.activeInHierarchy : !presenter || !presenter.Visible && presenter.Grow >= 1)   // growing in: arriving
                {
                    PresenceFailures++;
                    if (PresenceFailures < 10) Log("presence_FAIL", $"{(stage.NikoWaiting ? "shown before his first line " : "")}{runner.Chapter?.id} {runner.State} shot={stage.cams.Active?.name} intro={stage.IntroTime:0.0} {(presenter ? presenter.Location : "missing")}");
                }
                if (stage.niko.Speaking && narrator.voice.isPlaying) { mouthSpeakingSum += stage.niko.MouthOpen; mouthSpeakingFrames++; silentFor = 0; }
                else if (!narrator.Speaking && (silentFor += Time.unscaledDeltaTime) > .3f) MouthSilentMax = Mathf.Max(MouthSilentMax, stage.niko.MouthOpen);   // after the release
            }
            if (!narrator.Speaking) return;
            if (auditPresenter && narrator.Current == null && !closingSampled)
            {
                closingSampled = true; StartCoroutine(ClosingSamples());
            }
            PresenterFrames++;
            if (!presenter || !presenter.Visible && presenter.Grow >= 1)   // growing in: arriving
            {
                PresenterFailures++;
                if (PresenterFailures < 10) Log("presenter_FAIL", (narrator.Current?.id ?? "supplemental_closing") + " " + (presenter ? presenter.Location : "missing component"));
            }
        }

        /// -heNikoPoses: every gesture on the start screen, rendered by a temporary camera framing his upper body from the front
        /// and from 45° to his left (his -X side, where the R arm gestures), with elbow and wrist positions in his own frame (to
        /// compare with the mesh clearance check of the poses). Each capture lands 1.6 s into the gesture on the rig's own clock.
        IEnumerator NikoPoses()
        {
            var niko = stage.niko; var root = niko.transform;
            var portrait = new GameObject("pose-front-cam").AddComponent<Camera>(); portrait.enabled = false;
            portrait.fieldOfView = 35; portrait.nearClipPlane = .05f; portrait.farClipPlane = 4; portrait.cullingMask = 1 << niko.gameObject.layer;
            portrait.clearFlags = CameraClearFlags.SolidColor; portrait.backgroundColor = new Color(.03f, .1f, .18f, 1);
            portrait.transform.SetPositionAndRotation(root.TransformPoint(new Vector3(0, .7f, 1.75f)), root.rotation * Quaternion.Euler(0, 180, 0));
            var rt = RenderTexture.GetTemporary(640, 640, 24, RenderTextureFormat.ARGB32);
            var side = new GameObject("pose-side-cam").AddComponent<Camera>(); side.CopyFrom(portrait); side.enabled = false; side.targetTexture = null;
            side.transform.SetPositionAndRotation(portrait.transform.position, portrait.transform.rotation);
            side.transform.RotateAround(root.position, Vector3.up, -45);
            niko.lookTarget = portrait.transform;
            string P(Transform t) { var v = root.InverseTransformPoint(t.position) / Mathf.Max(1e-4f, root.lossyScale.y); return $"({v.x:0.000},{v.y:0.000},{v.z:0.000})"; }
            foreach (var g in new[] { "Idle", "Wave", "Greet", "RaiseHand", "Present", "Point", "PointLeft", "PointRight", "Pinch", "Snap", "Think", "Surprise", "Celebrate", "Encourage", "Listen" })
            {
                if (g != "Idle") niko.Play(g, 4);
                for (float t = 0; t < 1.6f; t += Clock.Delta) yield return null;
                foreach (var (cam, view) in new[] { (portrait, "front"), (side, "side45") })
                {
                    RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                    File.WriteAllBytes(Path.Combine(dir, $"niko_pose_{g}_{view}.png"), Read(rt).EncodeToPNG());
                }
                Log("niko_pose", $"{g} state={niko.State} expression={niko.Expression} elbowR={P(niko.elbowR)} handR={P(niko.handR)} elbowL={P(niko.elbowL)} handL={P(niko.handL)}");
            }
            Destroy(side.gameObject); Destroy(portrait.gameObject); RenderTexture.ReleaseTemporary(rt);
        }

        IEnumerator ClosingSamples()
        {
            float elapsed = 0;
            foreach (float share in new[] { .02f, .5f, .94f })
            {
                while (narrator.Speaking && elapsed < stage.closingLine.length * share) { elapsed += Clock.Delta; yield return null; }
                if (!narrator.Speaking) yield break;
                Log("presenter_sample", $"supplemental_closing Supplemental share={share:0.00} visible={(presenter && presenter.Visible ? "YES" : "NO")} mode={(presenter ? presenter.Location : "missing")} eyeScale={stage.niko.eyeL.localScale.y:0.00} gesture={stage.niko.Gesture} state={stage.niko.State} mouth={stage.niko.MouthOpen:0.00}");
                yield return Capture($"presenter_closing_{share:0.00}");
            }
        }

        IEnumerator PresenterSamples(string cue)
        {
            float length = narrator.Current.clip ? narrator.Current.clip.length : 3;
            foreach (float share in new[] { .02f, .5f, .94f })
            {
                while (narrator.Current != null && narrator.Current.id == cue && narrator.voice.time < length * share && !narrator.AwaitingContinue) yield return null;
                if (narrator.Current == null || narrator.Current.id != cue) yield break;
                yield return new WaitForEndOfFrame();
                Log("presenter_sample", $"{cue} {narrator.Source} share={share:0.00} visible={(presenter && presenter.Visible ? "YES" : "NO")} mode={(presenter ? presenter.Location : "missing")} eyeScale={stage.niko.eyeL.localScale.y:0.00} gesture={stage.niko.Gesture} state={stage.niko.State} expression={stage.niko.Expression} mouth={stage.niko.MouthOpen:0.00}");
                yield return Capture($"presenter_{cue}_{share:0.00}");
            }
        }
    }
}
