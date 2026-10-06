using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanEye
{
    public partial class AutoPilot
    {
        // Render the real saved Canvas through the camera for hidden-window QA captures.
        IEnumerator CaptureOffscreen(string file)
        {
            var cam = hud.mainCamera; var canvas = hud.canvas;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance;
            var scene = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active; Texture2D pixels = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .1f;
                Canvas.ForceUpdateCanvases();
                yield return null; yield return new WaitForEndOfFrame();
                // The camera's lens shift (ViewportShift) moves everything it renders left; the overlay UI is unaffected in play,
                // so move the camera-space canvas right by the same amount to capture what the learner sees.
                var shift = cam.GetComponent<ViewportShift>();
                if (shift && shift.enabled)
                {
                    float f = Mathf.Min(shift.columnPixels / Mathf.Max(1, Screen.width), ViewportShift.MaxShift);
                    float halfWidth = canvas.planeDistance * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * .5f) * cam.aspect;
                    canvas.transform.position += cam.transform.right * (f * halfWidth);
                }
                // A hidden window does not run the normal camera loop, so render-texture cameras (the retinal image)
                // would stay blank in captures; render them first so the capture shows what a visible window shows.
                foreach (var c in Camera.allCameras)
                    if (c != cam && c.targetTexture) RenderPipeline.SubmitRenderRequest(c, new UniversalRenderPipeline.SingleCameraRequest { destination = c.targetTexture });
                RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = scene });
                pixels = Read(scene); File.WriteAllBytes(file, pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                RenderTexture.active = active; if (pixels) Destroy(pixels); RenderTexture.ReleaseTemporary(scene);
            }
        }

        static Texture2D Read(RenderTexture target)
        {
            RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); tex.Apply(); return tex;
        }
    }
}
