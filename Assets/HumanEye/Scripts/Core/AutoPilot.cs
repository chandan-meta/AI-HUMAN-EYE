using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace HumanEye
{
    /// Development walkthrough driver, active ONLY with the -heAutopilot command-line flag (never for learners).
    /// It presses the same public actions the HUD buttons call, logs every state/cue change with real timestamps,
    /// samples frame times and captures screenshots. It does not simulate physical mouse/touch hit-testing.
    /// Flags: -heCapture <dir>  -heFast <factor>  -heChapter <index>  -heReduced
    public partial class AutoPilot : MonoBehaviour
    {
        ChapterRunner runner; Stage stage; Narrator narrator; ActivityController activities; Hud hud; CameraDirector cams; NikoGuide guide;
        string dir; StreamWriter log;
        float t0, frameSum; int frames, shots; float worst;
        ProfilerRecorder drawCalls, setPass, triangles, systemMemory, mainThread;
        double drawSum, setPassSum, triSum, mainSum; long drawMax, memMax;
        string lastShot, lastCue; LessonState lastState; int lastChapter = -1;
        bool triedWrong, pausedChecked, askedChecked;

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
            activities = runner.activities; hud = stage.hud; cams = runner.cameras; guide = stage.guide;
            dir = Path.GetFullPath(Arg("-heCapture", Path.Combine(Application.dataPath, "..", "Builds", "Checks", DateTime.Now.ToString("yyyyMMdd_HHmmss"))));
            Directory.CreateDirectory(dir);
            Done = false;
            PointerFailures = 0;
            SaveSystem.ResetProgress(true);   // walkthrough evidence always starts from a first run
            hud.ShowStart(); // Reset the displayed resume label as well as the data.
            Clock.FastForward = FastOverride ?? float.Parse(Arg("-heFast", "1"), System.Globalization.CultureInfo.InvariantCulture);
            SaveSystem.Data.settings.reducedMotion = Environment.GetCommandLineArgs().Contains("-heReduced");   // explicit both ways: settings persist in PlayerPrefs
            SaveSystem.Data.settings.waitForContinue = false;
            log = new StreamWriter(Path.Combine(dir, "walkthrough.log")) { AutoFlush = true };
            // Render/memory counters exist in Development players and the Editor (0 elsewhere).
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            systemMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            t0 = Time.realtimeSinceStartup;
            Log("start", $"{Application.platform} {SystemInfo.graphicsDeviceName} {Screen.width}x{Screen.height} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} fast={Clock.FastForward}");
            StartCoroutine(Drive());
        }

        void OnDestroy() { drawCalls.Dispose(); setPass.Dispose(); triangles.Dispose(); systemMemory.Dispose(); mainThread.Dispose(); }

        void Log(string kind, string detail) => log.WriteLine($"{Time.realtimeSinceStartup - t0,8:0.00}s\t{kind}\t{detail}");

        /// Captures the next rendered frame and waits for it, so the action that follows can't change what's captured.
        IEnumerator Capture(string name)
        {
            // Batch Editor uses a 640x480 Game view; the full layout audit runs in the
            // standalone player at the supported 1024x768+ sizes. Explicit clicks still run here.
            if (!Application.isEditor || Screen.width >= 1024) AuditPointers(name);
            if (!CapturesEnabled) yield break;
            var file = Path.Combine(dir, $"{++shots:000}_{name}.png");
            if (Application.isBatchMode) { yield return null; RenderToFile(file); yield return null; }   // batch editor: camera only, no UI overlay (WaitForEndOfFrame never fires in batch mode)
            else if (Environment.GetCommandLineArgs().Contains("-heOffscreen")) yield return CaptureOffscreen(file);
            else { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(file); yield return null; yield return new WaitForEndOfFrame(); }
            Log("capture", file);
            if (Environment.GetCommandLineArgs().Contains("-hePolish")) AuditLayout(name);
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

        void Update()
        {
            if (!runner || log == null || Done) return;
            frameSum += Time.unscaledDeltaTime; frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime);
            drawSum += drawCalls.LastValue; setPassSum += setPass.LastValue; triSum += triangles.LastValue; mainSum += mainThread.LastValue * 1e-6;
            drawMax = System.Math.Max(drawMax, drawCalls.LastValue); memMax = System.Math.Max(memMax, systemMemory.LastValue);
            if (runner.Index != lastChapter)
            {
                if (lastChapter >= 0 && frames > 0)
                    Log("perf", $"{runner.chapters[lastChapter].id} avg {frames / Mathf.Max(.001f, frameSum):0.0} fps, worst frame {worst * 1000:0} ms, " +
                        $"main thread {mainSum / frames:0.0} ms, draw calls avg {drawSum / frames:0} max {drawMax}, SetPass {setPassSum / frames:0}, triangles {triSum / frames / 1000:0}k, system memory peak {memMax / 1048576} MB");
                frameSum = 0; frames = 0; worst = 0; drawSum = setPassSum = triSum = mainSum = 0; drawMax = memMax = 0;
                lastChapter = runner.Index; Log("chapter", runner.Chapter ? runner.Chapter.id : "-");
            }
            if (runner.State != lastState) { lastState = runner.State; Log("state", lastState.ToString()); }
            var cue = narrator.Current?.id;
            if (cue != lastCue) { if (lastCue != null) Log("cue_end", lastCue); if (cue != null) Log("cue_start", $"{cue} [{narrator.Source}] clip={(narrator.MissingAudio ? "missing" : "ok")}"); lastCue = cue; }
        }

        IEnumerator Drive()
        {
            yield return Real(1.5f);
            yield return Capture("start_screen");
            if (Environment.GetCommandLineArgs().Contains("-hePolish")) yield return CheckMenuUI();
            int chapter = int.Parse(Arg("-heChapter", "0"));
            if (chapter == 0) yield return ClickUI("start-begin"); else hud.StartModule(chapter);
            Log("action", "Start pressed");
            int stopAfter = int.Parse(Arg("-heStop", "99"));
            while (true)
            {
                if (runner.Index >= chapter + stopAfter)
                {
                    Summary = $"stopped after {stopAfter} chapter(s); cues={SaveSystem.Data.cues.Count} evidence={SaveSystem.Data.evidence.Count}";
                    Log("done", Summary);
                    log.Close(); Exit(0); yield break;
                }
                if (runner.State == LessonState.Complete)
                {
                    yield return Real(3); yield return Capture("end_menu");
                    if (Stress && !savedChecked) { yield return CheckSavedCompletion(); continue; }
                    Summary = $"completed={string.Join(",", SaveSystem.Data.completedChapters)} cues={SaveSystem.Data.cues.Count} evidence={SaveSystem.Data.evidence.Count}";
                    Log("done", Summary);
                    Done = true; yield return Real(1); log.Close(); Exit(0); yield break;
                }
                if (cams.Active && cams.Active.name != lastShot)
                {
                    lastShot = cams.Active.name; yield return Settled(); yield return Real(.3f);
                    yield return Capture(runner.Chapter.id + "_" + lastShot);
                }
                if (narrator.AwaitingContinue) narrator.Continue();
                if (stage.IntroPlaying && Stress && !stage.Short && !introChecked)
                {
                    introChecked = true; yield return Real(3); yield return Capture("intro_mid");
                    if (!stage.IntroPlaying) { Log("check", "intro ended before the skip check (fast-forward); skip not exercised"); continue; }
                    yield return ClickUI("btn-skip-intro"); yield return Real(.5f);
                    Log("check", $"intro skip: playing={stage.IntroPlaying} fade={hud.Element("fade").GetComponent<CanvasGroup>().alpha:0.00} shot={cams.Active?.name} key={stage.keyLight.intensity:0.00}");
                    yield return Capture("intro_skipped");
                }
                if (!pausedChecked && runner.Chapter?.id == "Scene03" && narrator.Busy && narrator.Source == NarrationSource.Core)
                {
                    pausedChecked = true;
                    runner.SetPaused(true); float before = narrator.voice.time;
                    yield return new WaitForSecondsRealtime(1.5f);
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
                case "scan": yield return ClickUI("ab-SCAN"); break;
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
            if (Stress && !demoChecked && mode == "routeTrace") { yield return CheckDemoStress(); yield break; }
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
                    break;
                case "focus":
                    // Round 1: a wrong (too flat) lens first, then the learner's fix; round 2 refocuses on the distant tree.
                    stage.SetBrightnessFromSlider(0); yield return Real(1.5f); yield return Capture("focus_blurred");
                    Log("check", $"focus round 0: flat lens -> state {stage.FocusState} (-1 = light meets behind the retina) readout '{stage.Readout()}'");
                    guide.Ask("what should I do now?"); Log("ask", $"'what should I do now?' -> '{guide.Answer?.text}'");
                    stage.SetBrightnessFromSlider(stage.FocusNeeded); yield return Real(1); yield return Capture("focus_sharp_near");
                    yield return Real(2.5f);
                    stage.SetBrightnessFromSlider(stage.FocusNeeded); yield return Real(1); yield return Capture("focus_sharp_far");
                    yield return Real(2.5f);
                    Log("check", $"focus challenge rounds completed={stage.FocusRound} (2 = both)");
                    stage.UseAbility("REPLAY");
                    for (float t = 0; t < 20 && !(stage.eye.accommodationTarget > .9f && stage.eye.Accommodation > .55f && stage.eye.Accommodation < .92f); t += Time.unscaledDeltaTime) yield return null;
                    stage.UseAbility("FREEZE");   // mid-change: the lens shape and ray focus must hold still
                    float frozenAcc = stage.eye.Accommodation; yield return Capture("frozen_mid_accommodation"); yield return new WaitForSecondsRealtime(1.5f);
                    Log("check", $"freeze mid-change: accommodation before={frozenAcc:0.000} after 1.5 s={stage.eye.Accommodation:0.000} (equal = frozen)");
                    stage.UseAbility("FREEZE"); yield return Real(5);
                    break;
                case "photoreceptors":
                    stage.OnPartTapped(stage.micro.rods); yield return Real(4);
                    stage.UseControl("lightDim"); yield return Real(2); yield return Capture("photoreceptors_dim");
                    stage.UseControl("lightBright"); yield return Real(2); yield return Capture("photoreceptors_bright");
                    yield return new WaitUntil(() => !narrator.Busy);
                    break;
                case "inspectImage":
                    stage.UseAbility("REPLAY"); yield return Real(4); yield return Capture("image_replay");
                    break;
                case "routeTrace": case "signals":
                    stage.UseAbility("TRACE"); yield return Real(3); yield return Capture("trace_" + mode); yield return Real(6);
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
            if (runner.Chapter.id is "Scene03" or "Scene08" && card.buttons.Contains("Skip")) { activities.Click("Skip"); Log("activity", item.id + " skipped (whole check)"); yield break; }
            if (item.mode == QuestionMode.Order)
            {
                foreach (var o in item.options.Where(o => o.correct)) { activities.Click(o.text); yield return null; }
                activities.Click("Check"); Log("activity", item.id + " order placed"); yield break;
            }
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
