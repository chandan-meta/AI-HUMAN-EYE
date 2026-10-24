using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace HumanEye
{
    /// Development walkthrough driver, active ONLY with the -heAutopilot command-line flag (never for learners).
    /// It presses the same public actions the HUD buttons call, logs every state/cue change with real timestamps,
    /// samples frame times and captures screenshots. It does not simulate physical mouse/touch hit-testing.
    /// Flags: -heCapture <dir>  -heFast <factor>  -heChapter <index>  -heReduced  -heReferenceTiming (cinematics on the reference
    /// video's clock)  -heIntroStep <seconds> (capture interval with -heIntroOnly)  -heRecord <dir> (real-time video + audio)
    [DefaultExecutionOrder(1200)]
    public partial class AutoPilot : MonoBehaviour
    {
        ChapterRunner runner; Stage stage; Narrator narrator; ActivityController activities; Hud hud; CameraDirector cams; NikoGuide guide;
        string dir; StreamWriter log;
        float t0, frameSum; int frames, shots; float worst;
        ProfilerRecorder drawCalls, setPass, triangles, systemMemory, mainThread, gcAllocated, gpuFrame;
        double drawSum, setPassSum, triSum, mainSum, gcSum, gpuSum, gpuMax;
        long drawMax, memMax, gcMax; int gcSamples, gpuSamples;
        string lastShot, lastCue; LessonState lastState; int lastChapter = -1;
        bool triedWrong, pausedChecked, askedChecked;
        bool capturing;
        float lastCine = -10, cineStep = 1;   // -heCineStep <seconds>: frames during the eye-to-brain cinematic
        ViewportShift viewportShift;   // the lens shift: the visible 3D area is the part of the frame left of the panel column

        // Test hooks (PlayMode tests drive the same walkthrough without captures or quitting).
        public static float? FastOverride;
        public static bool ExitWhenDone = true, CapturesEnabled = true;
        public static bool Done { get; private set; }
        public static string Summary { get; private set; }

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }

        void Start()
        {
            runner = FindFirstObjectByType<ChapterRunner>(); stage = runner.stage; narrator = runner.narrator;
            AudioDirector.Played += Heard;
            activities = runner.activities; hud = stage.hud; cams = runner.cameras; guide = stage.guide;
            viewportShift = hud.mainCamera ? hud.mainCamera.GetComponent<ViewportShift>() : null;
            dir = Path.GetFullPath(Arg("-heCapture", Path.Combine(Application.dataPath, "..", "Builds", "Checks", DateTime.Now.ToString("yyyyMMdd_HHmmss"))));
            Directory.CreateDirectory(dir);
            Done = false;
            PresenterFrames = PresenterFailures = 0; PresenceFrames = PresenceFailures = 0; MouthSilentMax = 0; mouthSpeakingSum = 0; mouthSpeakingFrames = 0;
            auditPresenter = Environment.GetCommandLineArgs().Contains("-hePresenterAudit");
            if (Environment.GetCommandLineArgs().Contains("-heNoCapture")) CapturesEnabled = false;   // performance runs: no screenshot hitches
            PointerFailures = 0;
            SaveSystem.ResetProgress(true);   // walkthrough evidence always starts from a first run
            hud.ShowStart(); // Reset the displayed resume label as well as the data.
            Clock.FastForward = FastOverride ?? float.Parse(Arg("-heFast", "1"), System.Globalization.CultureInfo.InvariantCulture);
            if (Application.isEditor && Application.isBatchMode) Clock.MaxStep = 1 / 15f;   // batch Play captures: shader stalls never skip a beat
            SaveSystem.Data.settings.reducedMotion = Environment.GetCommandLineArgs().Contains("-heReduced");   // explicit both ways: settings persist in PlayerPrefs
            SaveSystem.Data.settings.waitForContinue = Environment.GetCommandLineArgs().Contains("-heWaitContinue");   // Read & Continue through the real button
            if (Environment.GetCommandLineArgs().Contains("-heReferenceTiming")) stage.timing = Stage.CinematicTiming.ReferenceMatch;
            if (Application.isBatchMode) CameraDirector.AspectOverride = 16f / 9;   // RenderToFile writes 1600x900
            cineStep = float.Parse(Arg("-heCineStep", "1"), System.Globalization.CultureInfo.InvariantCulture);
            log = new StreamWriter(Path.Combine(dir, "walkthrough.log")) { AutoFlush = true };
            // Render/memory counters exist in Development players and the Editor (0 elsewhere).
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            systemMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            gpuFrame = new ProfilerRecorder("GPU Frame Time", 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately);
            t0 = Time.realtimeSinceStartup;
            Log("start", $"{Application.platform} {SystemInfo.graphicsDeviceName} {Screen.width}x{Screen.height} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} fast={Clock.FastForward}");
            Log("perf-counters", $"GC allocated/frame supported={gcAllocated.Valid}; GPU frame counter supported={gpuFrame.Valid}" +
                (gpuFrame.Valid ? $" unit={gpuFrame.UnitType}; positive samples required before reporting GPU time" : "; GPU time unavailable") +
                "; measurements include walkthrough and capture overhead");
            var recordDir = Arg("-heRecord", null); if (recordDir != null) StartRecording(recordDir);   // after the log exists
            StartCoroutine(Drive());
        }

        void OnDestroy() { DisposeCaptureResources(); AudioDirector.Played -= Heard; drawCalls.Dispose(); setPass.Dispose(); triangles.Dispose(); systemMemory.Dispose(); mainThread.Dispose(); gcAllocated.Dispose(); gpuFrame.Dispose(); }

        void Log(string kind, string detail) => log?.WriteLine($"{Time.realtimeSinceStartup - t0,8:0.00}s\t{kind}\t{detail}");

        /// Captures the next rendered frame and waits for it, so the action that follows can't change what's captured.
        IEnumerator Capture(string name, bool pointers = true)
        {
            while (capturing) yield return null;
            capturing = true;
            try
            {
            // Newly enabled controls need a rendered Canvas frame before their raycast depth is valid.
            yield return null;
            // Batch Editor uses a 640x480 Game view; the full layout audit runs in the
            // standalone player at the supported 1024x768+ sizes. Explicit clicks still run here.
            if (pointers && (!Application.isEditor || Screen.width >= 1024)) AuditPointers(name);
            if (!CapturesEnabled) yield break;
            var file = Path.Combine(dir, $"{++shots:000}_{name}.png");
            if (Application.isBatchMode) { yield return null; RenderToFile(file); yield return null; }   // batch editor: camera only, no UI overlay (WaitForEndOfFrame never fires in batch mode)
            else if (Environment.GetCommandLineArgs().Contains("-heOffscreen")) yield return CaptureOffscreen(file);
            else { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(file); yield return null; yield return new WaitForEndOfFrame(); }
            Log("capture", file);
            var c = stage.Companion;
            if (c) Log("niko_frame", $"{name} {c.Location} slot={c.ScreenRect} drawn={c.DrawnRect} depth={c.Depth:0.00} fov={stage.hud.mainCamera.fieldOfView:0.0} visible={c.Visible}");
            if (Environment.GetCommandLineArgs().Contains("-hePolish")) AuditLayout(name);
            }
            finally { capturing = false; }
        }

        static void RenderToFile(string file)
        {
            var cam = Camera.main;
            var rt = RenderTexture.GetTemporary(1600, 900, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = previous;
            var active = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(file, tex.EncodeToPNG()); Destroy(tex);
        }

        void Exit(int code)
        {
            StopRecording();
            Done = true;
            if (!ExitWhenDone) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }

        IEnumerator Settled()
        {
            for (int calm = 0; calm < 6;) { calm = cams.brain.IsBlending ? 0 : calm + 1; yield return null; }
        }

        IEnumerator Real(float seconds) { yield return new WaitForSecondsRealtime(seconds / Mathf.Max(1, Clock.FastForward * .5f)); }

        /// Wait for Continue: the learner's way past a finished line is the visible Read & Continue button. A cinematic hides
        /// that button with the caption panel, so no line may wait there (the Scene 9 stall found on 15 October 2026).
        IEnumerator ReadOn()
        {
            Check(!stage.CinematicPlaying, "Read & Continue never waits behind a cinematic (its button is hidden there)");
            yield return Real(.3f);
            yield return ClickUI("btn-read-continue");
        }

        void LogPerformance()
        {
            if (lastChapter < 0 || frames == 0) return;
            string gc = gcSamples > 0 ? $"GC allocated/frame avg {gcSum / gcSamples:0} max {gcMax} bytes ({gcSamples} samples)" : "GC allocated/frame unavailable";
            string gpu = gpuSamples > 0 ? $"GPU avg {gpuSum / gpuSamples:0.00} max {gpuMax:0.00} ms ({gpuSamples} positive samples)"
                : gpuFrame.Valid ? "GPU unavailable (counter registered, no usable timing samples)" : "GPU unavailable (counter unsupported)";
            Log("presenter_coverage", $"frames={PresenterFrames} failures={PresenterFailures}");
            LogAudioSummary();
            Log("presence_coverage", $"frames={PresenceFrames} failures={PresenceFailures} mouthSpeakingMean={MouthSpeakingMean:0.000} mouthSilentMax={MouthSilentMax:0.000}");
            Log("perf", $"{runner.chapters[lastChapter].id} avg {frames / Mathf.Max(.001f, frameSum):0.0} fps, worst frame {worst * 1000:0} ms, " +
                $"main thread {mainSum / frames:0.0} ms, draw calls avg {drawSum / frames:0} max {drawMax}, SetPass {setPassSum / frames:0}, triangles {triSum / frames / 1000:0}k, system memory peak {memMax / 1048576} MB, {gc}, {gpu}");
        }

        void Update()
        {
            if (!runner || log == null || Done) return;
            frameSum += Time.unscaledDeltaTime; frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime);
            drawSum += drawCalls.LastValue; setPassSum += setPass.LastValue; triSum += triangles.LastValue; mainSum += mainThread.LastValue * 1e-6;
            drawMax = System.Math.Max(drawMax, drawCalls.LastValue); memMax = System.Math.Max(memMax, systemMemory.LastValue);
            if (gcAllocated.Valid && gcAllocated.Count > 0) { long bytes = gcAllocated.LastValue; gcSum += bytes; gcMax = Math.Max(gcMax, bytes); gcSamples++; }
            if (gpuFrame.Valid && gpuFrame.Count > 0 && gpuFrame.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds)
            {
                double ms = gpuFrame.LastValueAsDouble * 1e-6;
                if (ms > 0) { gpuSum += ms; gpuMax = Math.Max(gpuMax, ms); gpuSamples++; }
            }
            if (runner.Index != lastChapter)
            {
                LogPerformance();
                frameSum = 0; frames = 0; worst = 0; drawSum = setPassSum = triSum = mainSum = 0; drawMax = memMax = 0;
                gcSum = gpuSum = gpuMax = 0; gcMax = 0; gcSamples = gpuSamples = 0;
                lastChapter = runner.Index; Log("chapter", runner.Chapter ? runner.Chapter.id : "-");
            }
            if (runner.State != lastState) { lastState = runner.State; Log("state", lastState.ToString()); }
            var cue = narrator.Current?.id;
            if (cue != lastCue)
            {
                if (lastCue != null) Log("cue_end", lastCue);
                if (cue != null)
                {
                    Log("cue_start", $"{cue} [{narrator.Source}] clip={(narrator.MissingAudio ? "missing" : "ok")}");
                    LogFraming("framing", cue); StartCoroutine(FramingSettled(cue));
                    if (auditPresenter) StartCoroutine(PresenterSamples(cue));
                }
                lastCue = cue;
            }
        }

        // ---------- framing audit: is the structure a line names framed, inside the visible area and actually drawn? ----------
        /// Re-samples the same cue once the camera has settled (or notes that the line ended before it did).
        IEnumerator FramingSettled(string cue)
        {
            yield return Settled();
            if (narrator.Current != null && narrator.Current.id == cue) LogFraming("framing_settled", cue);
            else Log("framing_settled", cue + " line ended before the camera settled");
        }

        /// One line per cue: shot, blend state, the named structure (Stage.PartOf, else the explored part), its focus anchor
        /// in viewport space, its offset from the centre of the visible 3D area (left of the panel column: x in
        /// [0, 1 − column/width × shift weight]), its projected height as a fraction of the frame, and whether it is drawn.
        void LogFraming(string kind, string cue)
        {
            var cam = hud ? hud.mainCamera : null;
            if (!cam || string.IsNullOrEmpty(cue)) return;
            string shot = cams.Active ? cams.Active.name : "-";
            bool blending = cams.brain && cams.brain.IsBlending;
            float colFrac = viewportShift ? Mathf.Clamp01(viewportShift.columnPixels / Mathf.Max(1, Screen.width)) * ViewportShift.Weight : 0;
            float visibleWidth = 1 - colFrac;
            bool InView(Vector3 point) => point.z > 0 && point.x >= 0 && point.x <= visibleWidth && point.y >= 0 && point.y <= 1;
            if (cue == "S09_L04")
            {
                // The storyboard calls for the original object and two distinct views here; the brain was L03's shot.
                var apple = stage.appleRoot;
                var appleVp = cam.WorldToViewportPoint(apple.position);
                string Inset(string key)
                {
                    var image = hud.Control<UnityEngine.UI.RawImage>(key);
                    return $"active={image.gameObject.activeInHierarchy},texture={(image.texture ? "bound" : "missing")},alpha={image.canvasRenderer.GetInheritedAlpha():0.00}";
                }
                Log(kind, $"{cue} shot={shot} blending={blending} object=apple vp=({appleVp.x:0.00},{appleVp.y:0.00}) depth={appleVp.z:0.00} " +
                          $"sizeFrac={ProjectedHeight(cam, apple.GetComponentsInChildren<Renderer>()):0.00} inView={InView(appleVp)} active={apple.gameObject.activeInHierarchy} " +
                          $"retinalInset=[{Inset("inset-retina-image")}] perceptionInset=[{Inset("inset-perception-image")}]");
                return;
            }
            string id = Stage.PartOf(cue);
            if (id == null && runner.Focused != null) id = runner.Focused.partId;
            var part = id != null ? stage.Part(id) : null;
            if (!part) { Log(kind, $"{cue} shot={shot} blending={blending} part=- (no structure named)"); return; }
            var vp = cam.WorldToViewportPoint(stage.eye.FocusOf(part).position);
            Log(kind, $"{cue} shot={shot} blending={blending} part={id} vp=({vp.x:0.00},{vp.y:0.00}) depth={vp.z:0.00} visibleCentreDx={vp.x - visibleWidth * .5f:+0.00;-0.00} " +
                      $"sizeFrac={ProjectedHeight(cam, part.main):0.00} inView={InView(vp)} visible={part.Visible} alpha={Stage.AlphaOf(part):0.00} glow={Stage.GlowOf(part):0.00}");
        }

        /// Projected height of the part's drawn bounds as a fraction of the frame height (0 when nothing is in front of the camera).
        static float ProjectedHeight(Camera cam, IEnumerable<Renderer> renderers)
        {
            bool any = false; var b = new Bounds();
            foreach (var r in renderers)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return 0;
            float min = float.MaxValue, max = float.MinValue; var c = b.center; var e = b.extents;
            for (int i = 0; i < 8; i++)
            {
                var v = cam.WorldToViewportPoint(new Vector3(c.x + ((i & 1) == 0 ? -e.x : e.x), c.y + ((i & 2) == 0 ? -e.y : e.y), c.z + ((i & 4) == 0 ? -e.z : e.z)));
                if (v.z <= 0) continue;
                min = Mathf.Min(min, v.y); max = Mathf.Max(max, v.y);
            }
            return max > min ? Mathf.Min(max - min, 9) : 0;
        }

        IEnumerator Drive()
        {
            yield return Real(1.5f);
            yield return Capture("start_screen");
            if (Environment.GetCommandLineArgs().Contains("-hePolish")) yield return CheckMenuUI();
            if (Environment.GetCommandLineArgs().Contains("-heNikoPoses"))
            {
                yield return NikoPoses();
                Summary = "niko poses"; Log("done", Summary); log.Close(); log = null; Exit(0); yield break;
            }
            int chapter = int.Parse(Arg("-heChapter", "0"));
            if (chapter == 0) yield return ClickUI("start-begin"); else hud.StartModule(chapter);
            Log("action", "Start pressed");
            if (Environment.GetCommandLineArgs().Contains("-heIntroOnly"))
            {
                // Evidence of the entrance itself: a frame every second at real speed, then the revealed interface.
                for (float wait = 0; !stage.IntroPlaying && wait < 5; wait += Time.unscaledDeltaTime) yield return null;
                yield return null; yield return null;   // the overlay was shown this frame; let the canvas lay it out before the pointer audit
                float lastShot = -10, step = float.Parse(Arg("-heIntroStep", "1"), System.Globalization.CultureInfo.InvariantCulture);
                while (stage.IntroPlaying)
                {
                    if (stage.IntroTime - lastShot >= step) { lastShot = stage.IntroTime; yield return Capture($"intro_{stage.IntroTime:00.0}s"); }
                    yield return null;
                }
                Log("check", $"intro finished: gate={stage.entrance.OpenAmount} niko={stage.niko.gameObject.activeSelf} waiting={stage.NikoWaiting} shot={cams.Active?.name}");
                for (float wait = 0; stage.NikoWaiting && wait < 8; wait += Time.unscaledDeltaTime) yield return null;   // his first line brings him in
                for (int i = 0; i < 6; i++) yield return Capture($"niko_arrive_{i}", false);   // the arrival burst, as fast as captures go (the lesson UI is still fading in)
                yield return new WaitForSecondsRealtime(1.2f); yield return Capture("intro_done_ui");
                LogPerformance(); Summary = "intro only"; Log("done", Summary); log.Close(); log = null; Exit(0); yield break;
            }
            int stopAfter = int.Parse(Arg("-heStop", "99"));
            while (true)
            {
                if (runner.Index >= chapter + stopAfter)
                {
                    LogPerformance();
                    Summary = $"stopped after {stopAfter} chapter(s); cues={SaveSystem.Data.cues.Count} evidence={SaveSystem.Data.evidence.Count}";
                    Log("done", Summary);
                    log.Close(); log = null; Exit(0); yield break;
                }
                if (runner.State == LessonState.Complete)
                {
                    yield return Real(3); yield return Capture("end_menu");
                    if (System.Environment.GetCommandLineArgs().Contains("-heFreeExplore") && !freeChecked) { yield return FreeExploreTour(); continue; }
                    if (Stress && !savedChecked) { yield return CheckSavedCompletion(); continue; }
                    LogPerformance();
                    Summary = $"completed={string.Join(",", SaveSystem.Data.completedChapters)} cues={SaveSystem.Data.cues.Count} evidence={SaveSystem.Data.evidence.Count}";
                    Log("done", Summary);
                    Done = true; yield return Real(1); log.Close(); log = null; Exit(0); yield break;
                }
                if ((stage.EyeBrainPlaying || stage.JourneyPlaying) && Time.realtimeSinceStartup - lastCine >= cineStep)
                {
                    lastCine = Time.realtimeSinceStartup; yield return Capture($"cine_{runner.Chapter.id}_{lastCine - t0:000.0}s");
                    continue;
                }
                if (cams.Active && cams.Active.name != lastShot)
                {
                    lastShot = cams.Active.name; yield return Settled(); yield return Real(.3f);
                    yield return Capture(runner.Chapter.id + "_" + lastShot);
                }
                if (narrator.AwaitingContinue)
                {
                    if (!SaveSystem.Data.settings.waitForContinue) narrator.Continue();
                    else { yield return ReadOn(); continue; }
                }
                if (stage.IntroPlaying && Stress && !stage.Short && !introChecked)
                {
                    introChecked = true; yield return Real(3); yield return Capture("intro_mid");
                    if (!stage.IntroPlaying) { Log("check", "intro ended before the skip check (fast-forward); skip not exercised"); continue; }
                    yield return ClickUI("btn-skip-intro"); yield return Real(.5f);
                    Check(stage.sfx.ActiveLoops == 0 && stage.sfx.MusicKey == "lab", "Skip stops the opening's loops and hands over to the lab bed");
                    Log("check", $"intro skip: playing={stage.IntroPlaying} fade={hud.Element("fade").GetComponent<CanvasGroup>().alpha:0.00} shot={cams.Active?.name} key={stage.keyLight.intensity:0.00}");
                    yield return Capture("intro_skipped");
                }
                if (!pausedChecked && runner.Chapter?.id == "Scene03" && narrator.Busy && narrator.Source == NarrationSource.Core)
                {
                    pausedChecked = true;
                    for (float w = 0; w < 2 && narrator.Busy && stage.sfx.Duck > stage.sfx.musicDuck + .01f; w += Time.unscaledDeltaTime) yield return null;   // the duck eases in
                    if (narrator.Busy) Check(stage.sfx.MixState == "NikoSpeaking" && stage.sfx.Duck <= stage.sfx.musicDuck + .01f, $"Music ducks under Niko ({20 * Mathf.Log10(stage.sfx.Duck):0.0} dB)");
                    runner.SetPaused(true); float before = narrator.voice.time;
                    yield return new WaitForSecondsRealtime(1.5f);
                    Check(!stage.sfx.MusicPlaying && stage.sfx.ActiveLoops == 0, "Pause holds the music and loops");
                    yield return Capture("pause_overlay");
                    Log("check", $"pause: timeScale={Time.timeScale} voiceTimeBefore={before:0.00} after={narrator.voice.time:0.00} stillPaused={Clock.Paused}");
                    runner.SetPaused(false);
                }
                if (activities.Card != null) { yield return Activity(); continue; }
                if (runner.Awaiting != null) { yield return Await(runner.Awaiting); continue; }
                if (runner.ExploreSet != null && runner.State == LessonState.Exploring && runner.Focused == null)
                {
                    var next = runner.ExploreSet.First(e => !runner.ExploreDone.Contains(e.partId));
                    if (!askedChecked && next.partId == "iris")
                    {
                        askedChecked = true;
                        guide.Ask("What does the coloured part do?");
                        Log("ask", $"'What does the coloured part do?' -> action={guide.Answer?.action} part={guide.Answer?.part}");
                    }
                    else stage.RequestPartFromList(next.partId);
                    yield return Real(1);
                    continue;
                }
                if (runner.FreeMode != null) { yield return Free(runner.FreeMode); continue; }
                yield return null;
            }
        }

        bool introChecked;

        IEnumerator Await(string key)
        {
            if (Stress && !flowChecked) yield return CheckFlowUI();
            yield return Real(1.2f);
            yield return Capture("await_" + key);
            switch (key)
            {
                case "inside": yield return ClickUI("ab-INSIDE"); break;
                case "brighter": stage.UseControl("dimmer"); Log("check", "dimmer during 'brighter' was refused (reason toast)"); stage.UseControl("presetBright"); break;
                case "dimmer": stage.UseControl("presetDim"); break;
                case "near": stage.UseControl("near"); break;
                case "distant": stage.UseControl("distant"); break;
                default: runner.Press(key); break;
            }
            Log("action", "await " + key);
            yield return Real(.5f);
        }

        IEnumerator Free(string mode)
        {
            if (Stress && !demoChecked && mode == "brightness") { yield return CheckDemoStress(); yield break; }
            Log("free", mode);
            yield return Real(1.5f);
            yield return Capture("free_" + mode);
            switch (mode)
            {
                case "brightness":
                    guide.Ask("make it brighter"); Log("ask", $"'make it brighter' -> {guide.Answer?.action} '{guide.Answer?.text}'");
                    guide.Ask("why is the sky blue?"); Log("ask", $"'why is the sky blue?' -> unsupported={guide.Answer?.unsupported}");
                    stage.UseControl("presetDim"); yield return Real(3); yield return Capture("brightness_dim");
                    stage.UseControl("presetBright"); yield return Real(3); yield return Capture("brightness_bright");
                    guide.Ask("what should I do now?"); Log("ask", $"'what should I do now?' -> '{guide.Answer?.text}'");
                    stage.UseAbility("REPLAY"); yield return Real(1);
                    stage.UseAbility("FREEZE");   // mid-change: the pupil must hold still while frozen
                    float frozenPupil = stage.eye.PupilRadius; yield return Capture("frozen_mid_pupil"); yield return new WaitForSecondsRealtime(1.5f);
                    Log("check", $"freeze mid-change: pupil before={frozenPupil:0.000} after 1.5 s={stage.eye.PupilRadius:0.000} (equal = frozen)");
                    stage.UseAbility("FREEZE"); yield return Real(4);
                    break;
                case "photoreceptors":
                    stage.OnPartTapped(stage.micro.rods); yield return Real(4);
                    stage.UseControl("lightDim"); yield return Real(2); yield return Capture("photoreceptors_dim");
                    stage.UseControl("lightBright"); yield return Real(2); yield return Capture("photoreceptors_bright");
                    while (narrator.Busy) yield return narrator.AwaitingContinue && SaveSystem.Data.settings.waitForContinue ? ReadOn() : null;
                    break;
                default:
                    yield return Real(1);
                    break;
            }
            Log("action", "continue from " + mode);
            runner.Continue();
            yield return Real(.5f);
        }

        IEnumerator Activity()
        {
            var card = activities.Card;
            yield return Real(1);
            if (activities.Card != card) yield break;
            yield return Capture("activity_" + (activities.Item?.id ?? "offer"));
            foreach (var b in new[] { "Next", "Continue" }) if (card.buttons.Contains(b)) { activities.Click(b); yield break; }
            var item = activities.Item;
            if (item == null) { activities.Click(card.buttons.LastOrDefault()); yield break; }
            Check(!card.buttons.Contains("Skip"), "Quick checks have no Skip button");
            if (!triedWrong)
            {
                triedWrong = true;
                var wrong = item.options.First(o => !o.correct);
                activities.Click(wrong.text); Log("activity", $"{item.id} deliberate wrong answer '{wrong.text}'");
                yield return Real(1.5f); yield return Capture("activity_hint_" + item.id);
                yield break;
            }
            var right = item.options.First(o => o.correct);
            activities.Click(right.text); Log("activity", $"{item.id} answered '{right.text}'");
        }
    }
}
