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
        Camera captureUi;
        Material captureComposite;

        /// Hidden-window QA captures. A screenshot file matches what the learner sees: the 3D view through the full pipeline
        /// (volumes, so grading and depth of field are there), and the saved Canvas rendered on its own, unprocessed, laid over
        /// it as the overlay canvas draws in play. A single-camera request skips the volume update (no depth of field), and a
        /// camera-space canvas drawn by the lesson camera would take its depth of field and lens shift. The 3D view renders into
        /// an HDR target, as on screen (an 8-bit target clamps every highlight before tonemapping), and the overlay is laid on in
        /// linear light, as the sRGB back buffer blends it. Recordings use the same two renders and composite them on the GPU.
        IEnumerator CaptureOffscreen(string file, RenderTexture destination = null)
        {
            var cam = hud.mainCamera; var canvas = hud.canvas;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance;
            var root = hud.Element("root"); var rootPosition = root.anchoredPosition;
            var scene = destination ? destination : RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            RenderTexture ui = null, hdr = null; var active = RenderTexture.active; Texture2D pixels = null, overlay = null;
            try
            {
                if (!captureUi)
                {
                    captureUi = new GameObject("capture-ui-camera").AddComponent<Camera>();
                    captureUi.enabled = false; captureUi.transform.position = new Vector3(0, -500, 0);   // nothing but the canvas in view
                    captureUi.clearFlags = CameraClearFlags.SolidColor; captureUi.backgroundColor = Color.clear;
                    captureUi.cullingMask = 1 << canvas.gameObject.layer; captureUi.nearClipPlane = .01f; captureUi.farClipPlane = 10;
                    var data = captureUi.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
                }
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = captureUi; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                // Recording runs at end-of-frame: restore the Canvas before any subsequent input or label layout.
                if (!destination) { yield return null; yield return new WaitForEndOfFrame(); }
                if (!scene) yield break; // Recording may finish while the layout wait is in flight.
                // A hidden window does not run the normal camera loop, so render-texture cameras (the retinal image)
                // would stay blank in captures; render them first so the capture shows what a visible window shows.
                foreach (var c in Camera.allCameras)
                    if (c != cam && c.targetTexture) RenderPipeline.SubmitRenderRequest(c, new UniversalRenderPipeline.SingleCameraRequest { destination = c.targetTexture });
                hdr = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR);
                RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest { destination = hdr });
                ui = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                RenderPipeline.SubmitRenderRequest(captureUi, new UniversalRenderPipeline.SingleCameraRequest { destination = ui });
                if (destination)
                {
                    if (!captureComposite)
                    {
                        var shader = Resources.Load<Shader>("CaptureComposite");
                        if (!shader || !shader.isSupported) throw new System.InvalidOperationException("The recording composite shader is unavailable.");
                        captureComposite = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    }
                    captureComposite.SetTexture("_UI", ui);
                    Graphics.Blit(hdr, scene, captureComposite);
                    yield break;
                }
                Graphics.Blit(hdr, scene);   // encodes to sRGB
                if (file == null) yield break;
                pixels = Read(scene); overlay = Read(ui);
                var a = pixels.GetPixels32(); var b = overlay.GetPixels32();
                for (int i = 0; i < a.Length; i++)
                {   // the canvas draws premultiplied: out = ui + scene * (1 - ui alpha), in linear light
                    float k = 1 - b[i].a / 255f;
                    a[i] = new Color32(Over(b[i].r, a[i].r, k), Over(b[i].g, a[i].g, k), Over(b[i].b, a[i].b, k), 255);
                }
                pixels.SetPixels32(a); File.WriteAllBytes(file, pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; root.anchoredPosition = rootPosition;
                RenderTexture.active = active; if (pixels) Destroy(pixels); if (overlay) Destroy(overlay);
                if (ui) RenderTexture.ReleaseTemporary(ui); if (hdr) RenderTexture.ReleaseTemporary(hdr); if (!destination) RenderTexture.ReleaseTemporary(scene);
            }
        }

        void DisposeCaptureResources()
        {
            if (captureComposite) Destroy(captureComposite);
            if (captureUi) Destroy(captureUi.gameObject);
        }

        static readonly float[] Linear = Enumerable.Range(0, 256).Select(i => Mathf.GammaToLinearSpace(i / 255f)).ToArray();
        static byte Over(byte ui, byte scene, float k) => (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Linear[ui] + Linear[scene] * k) * 255), 0, 255);

        static Texture2D Read(RenderTexture target)
        {
            RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); tex.Apply(); return tex;
        }
    }
}
