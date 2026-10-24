using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanEye
{
    /// -heRecord <dir>: real-time recording of the walkthrough (development tool, never for learners). The finished frame
    /// (3D + interface) is grabbed from the back buffer at 30 fps and piped into ffmpeg (H.264 → video.mp4); the final audio
    /// mix is written to audio.wav and silenced on the speakers. When the game runs slower than 30 fps a frame is repeated,
    /// so picture and sound both stay on real time. Mux afterwards: ffmpeg -i video.mp4 -i audio.wav -c:v copy -c:a aac out.mp4
    public partial class AutoPilot
    {
        const int RecordFps = 30;
        Process encoder; Stream pipe; RenderTexture grab; byte[] frame; float recordStart; int framesDue, framesWritten;
        readonly Queue<(AsyncGPUReadbackRequest request, int upTo)> grabs = new();
        AudioTap tap;

        void StartRecording(string dir)
        {
            Directory.CreateDirectory(dir);
            CapturesEnabled = false;   // PNG captures would hitch the picture
            int w = Screen.width, h = Screen.height;
            grab = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            frame = new byte[w * h * 4];
            string filters = (System.Array.IndexOf(Environment.GetCommandLineArgs(), "-heOffscreen") >= 0 ? "vflip," : "")
                + "scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p";
            encoder = Process.Start(new ProcessStartInfo("ffmpeg",
                $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {w}x{h} -r {RecordFps} -i - -vf \"{filters}\" -c:v libx264 -preset veryfast -crf 20 \"{Path.Combine(dir, "video.mp4")}\"")
                { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true });
            pipe = encoder.StandardInput.BaseStream;
            tap = FindFirstObjectByType<AudioListener>().gameObject.AddComponent<AudioTap>();
            tap.Open(Path.Combine(dir, "audio.wav"));
            recordStart = Time.realtimeSinceStartup;
            StartCoroutine(Record());
            Log("record", $"{w}x{h} at {RecordFps} fps -> {dir}");
        }

        IEnumerator Record()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (encoder != null)
            {
                yield return endOfFrame;
                if (encoder == null) yield break;
                int due = Mathf.FloorToInt((Time.realtimeSinceStartup - recordStart) * RecordFps) + 1;
                if (due > framesDue)
                {
                    if (System.Array.IndexOf(Environment.GetCommandLineArgs(), "-heOffscreen") >= 0)
                    {
                        while (capturing) yield return null;
                        capturing = true;
                        try { yield return CaptureOffscreen(null, grab); }
                        finally { capturing = false; }
                    }
                    else ScreenCapture.CaptureScreenshotIntoRenderTexture(grab);
                    if (encoder == null || !grab) yield break;
                    grabs.Enqueue((AsyncGPUReadback.Request(grab, 0, TextureFormat.RGBA32), due));
                    framesDue = due;
                }
                Flush(false);
            }
        }

        /// Writes finished grabs in order; each fills the timeline up to its frame (a slow frame is repeated, never skipped).
        void Flush(bool all)
        {
            while (grabs.Count > 0 && (all || grabs.Peek().request.done))
            {
                var (request, upTo) = grabs.Dequeue();
                if (all) request.WaitForCompletion();
                if (!request.hasError) request.GetData<byte>().CopyTo(frame);   // on error the previous frame is repeated
                for (; framesWritten < upTo; framesWritten++) pipe.Write(frame, 0, frame.Length);
            }
        }

        void StopRecording()
        {
            if (encoder == null) return;
            Flush(true);
            tap.Close(); Destroy(tap);
            pipe.Close(); encoder.WaitForExit(120000); encoder = null;
            Destroy(grab);
            Log("record", $"stopped: {framesWritten} frames ({framesWritten / (float)RecordFps:0.0} s)");
        }

        void OnApplicationQuit() => StopRecording();
    }

    /// The final mix, written as 16-bit WAV from the audio thread and zeroed so the speakers stay quiet while recording.
    class AudioTap : MonoBehaviour
    {
        BinaryWriter wav; int channels = 2, rate; long samples; bool open;
        readonly object gate = new();

        public void Open(string path)
        {
            rate = AudioSettings.outputSampleRate;
            wav = new BinaryWriter(File.Create(path)); wav.Write(new byte[44]);   // header written on Close
            open = true;
        }

        void OnAudioFilterRead(float[] data, int ch)
        {
            lock (gate)
            {
                if (!open) return;
                channels = ch;
                foreach (var s in data) wav.Write((short)(Math.Max(-1f, Math.Min(1f, s)) * 32767));
                samples += data.Length;
            }
            Array.Clear(data, 0, data.Length);
        }

        public void Close()
        {
            lock (gate)
            {
                if (!open) return;
                open = false;
                int bytes = (int)(samples * 2);
                wav.Seek(0, SeekOrigin.Begin);
                wav.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); wav.Write(36 + bytes); wav.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                wav.Write(16); wav.Write((short)1); wav.Write((short)channels); wav.Write(rate); wav.Write(rate * channels * 2);
                wav.Write((short)(channels * 2)); wav.Write((short)16);
                wav.Write(System.Text.Encoding.ASCII.GetBytes("data")); wav.Write(bytes);
                wav.Close();
            }
        }
    }
}
