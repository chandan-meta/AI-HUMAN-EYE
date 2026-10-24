# Human Eye — Observatory of Vision (Storyboard Scenes 2–11)

> Authoring update, 3 October 2026: the live HUD is now a saved native Canvas prefab with editable Hierarchy objects. See [Scene authoring](SCENE_AUTHORING.md). UI Toolkit and earlier visual/device results below describe the pre-conversion version; they are not validation of the new Canvas. Full-scene regeneration replaces manual edits and is not needed to build the saved scene.

A playable Unity learning module. Niko, the Vision Investigator Officer, guides a learner through the eye, from the first scan to the final curiosity, with click/tap interaction throughout. It starts directly at Scene 2 (no park, friends, book or portal).

## Requirements

- **Unity 6000.3.0f1** (the project's existing version; nothing was upgraded). URP 17.3, Input System 1.16, Cinemachine 3.1.5, UI Toolkit, Test Framework. No new packages were added.
- **Windows build support** (installed) for PC. **Android build support** (installed) for the configured native tablet target (API 29, ARM64).
- **WebGL build support is installed** for 6000.3 as of 14 October 2026. See the reference-v2 report for the current build and browser test result; earlier reports predate this installation.
- Optional, only to regenerate source data: Python 3.11, ffmpeg, Blender 5.1, and Windows OneCore voices (TTS).

## Run it

1. Open the project in Unity 6000.3.0f1.
2. Open `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` (first in Build Settings) and press **Play**.
3. Press **Start**. The learner begins at Scene 2. "Continue from Scene N" appears only if anonymous saved progress exists.

Editor menu, under **Tools > Human Eye**:

| Menu item | What it does |
|---|---|
| **Build Module** | Regenerates the launch scene, chapter/dialogue/question/knowledge assets, meshes, materials, textures and UI wiring. Idempotent: it only writes `Assets/HumanEye/Generated`, `Assets/HumanEye/Scenes`, and `Assets/HumanEye/Data/ModuleConfig.asset` (once, if missing). Existing generated assets are updated in place, so GUIDs stay stable. |
| **Validate Module** | Runs the integrity checks and writes `Docs/HumanEye/VALIDATION_REPORT.md` and `Docs/HumanEye/CUE_SHEET.md`. |
| **Build Player > Windows / Android (tablet)** | Player builds of the launch scene. These are Development builds because the prototype's existing `AssetAudit` gate rejects release builds. |
| **Preview Chapter (editor only) > Scene N** | Editor-only jump to a chapter checkpoint for authoring. Not available in players and never a learner shortcut. |

Batch entry points (run from the project folder):

```bash
Unity.exe -batchmode -nographics -projectPath . -executeMethod HumanEye.EditorTools.Builder.Build -quit
```
```bash
Unity.exe -batchmode -nographics -projectPath . -executeMethod HumanEye.EditorTools.Validator.RunBatch
```
```bash
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -assemblyNames HumanEye.Tests.EditMode -testResults Logs/tests/editmode-results.xml
```
```bash
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames HumanEye.Tests.PlayMode -testResults Logs/tests/playmode-results.xml
```
```bash
Unity.exe -batchmode -nographics -projectPath . -executeMethod HumanEye.EditorTools.PlayerBuilds.Windows
```
```bash
Unity.exe -batchmode -nographics -projectPath . -executeMethod HumanEye.EditorTools.PlayerBuilds.Android
```

Walkthrough capture (development only; presses the same public actions as the HUD buttons and writes PNGs plus `walkthrough.log`):

```bash
Builds/HumanEye/Windows/HumanEye.exe -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -heAutopilot -heFast 3 -heCapture Builds/Checks/run
```

Known issue: the Windows player sometimes crashes inside `D3D12Core.dll` while quitting, after Scenes 6 or 11. Progress is already saved when this happens. Add `-force-d3d11` to avoid it (see `TEST_REPORT.md`).

Regenerating source data:

```bash
python Tools/HumanEye/extract_ledger.py
```
```bash
powershell -File Tools/HumanEye/generate_tts.ps1
```
```bash
python Tools/HumanEye/generate_music.py
```
```bash
blender -b --factory-startup --disable-autoexec Startup.blend --python Tools/HumanEye/export_zanatomy_head.py -- Assets/HumanEye/ThirdParty/ZAnatomy/ZAnatomy_Head.fbx
```

## Scene-to-chapter mapping

There is one launch scene (`HumanEye_Observatory`) that reuses a single observatory, eye, cast and brain. The storyboard chapters are ten `ChapterDefinition` assets, `Assets/HumanEye/Generated/Data/Chapters/Scene02.asset` … `Scene11.asset`, authored in `Assets/HumanEye/Editor/HumanEyeContent.cs`. `ChapterRunner` plays them in source order. Each chapter's checkpoint state (restart/resume) is documented in the asset and in `CUE_SHEET.md`.

## How it is put together (`Assets/HumanEye/Scripts`, assembly `HumanEye.Runtime`)

| Area | Files |
|---|---|
| Chapter flow and state machine | `Core/ChapterRunner.cs`, `Core/LessonState.cs`, `Core/Routine.cs` (owned scheduler: chapter exit/reset cancels every routine; Pause never changes `Time.timeScale`; FREEZE stops only the visualization clock) |
| Narration arbiter, audio | `Core/Narrator.cs` (one line at a time; completion from real clip length; missing audio → explicit Read & Continue), `Core/AudioDirector.cs` (narration/music/SFX volumes, ducking) |
| Camera | `Core/CameraDirector.cs`: the one camera controller, Cinemachine 3 API; shots are named `CinemachineCamera`s in the scene |
| Input and selection | `Interaction/PointerSelector.cs` (mouse/touch tap, drag, wheel/pinch; UI consumes its gesture first; explicit pick priorities) |
| Anatomy | `Anatomy/Lathe.cs`, `EyeGeometry.cs`, `EyeModel.cs` (live pupil opening, accommodating lens, cutaway), `AnatomyPart.cs` |
| Demonstrations | `Demos/LightRays.cs` (thin-lens model tied to lens shape), `SignalPulses.cs`, `PhotoreceptorField.cs`, `HumanDisplay.cs` |
| World actions (whitelist) | `Stage/Stage*.cs` |
| Niko | `Niko/NikoRig.cs` (gestures, face), `Niko/NikoGuide.cs` (Ask Niko — local curated intents, offline, not a generative AI) |
| Adaptive layer | `Learning/LearnerModel.cs` (inspectable rules), `Learning/ActivityController.cs`, `Data/QuestionBank.cs` |
| UI | `UI/Hud.cs`, `UI/WorldLabels.cs`, `Assets/HumanEye/UI/Hud.uxml`, `Hud.uss` |
| Saves and configuration | `Core/SaveSystem.cs` (anonymous PlayerPrefs JSON; corrupt-save recovery; reset), `Data/ModuleConfig.cs` |

Content layers:

- **StoryboardCore:** `Assets/HumanEye/Data/DialogueLedger.json` holds the 66 cues, extracted verbatim from the storyboard.
- **SupplementalAdaptive:** the question bank, hints, activities and Ask Niko answers. All are tagged as newly authored and never enter the ledger.

## Controls (mouse and touch are equivalent)

- **Right-hand column.** The upper information panel shows Niko's portrait and status, the chapter, the current instruction, short information blocks and the captions, with **Talk to Niko** always in the same place. The lower **View / Activities** panel holds the actions, experiment controls, the Parts list, the view controls, the retinal-image popup and the journey stages; quick checks, the end card and the Ask Niko sheet overlay its lower half.
- **Tap or click** a glowing part, its label, or the **Parts** list to explore it. **Drag** on the model to turn it. **View** buttons rotate, zoom and reset; the mouse wheel and two-finger pinch are optional.
- **Scan Eye** (Scene 2, when Niko asks) and **Explore Inside** (after the four outer parts) are the only gated actions. **Freeze** and **Replay demo** appear during the two hands-on experiments (brightness in Scene 5, light level on the photoreceptors in Scene 8); each experiment ends with Continue.
- **Quick checks** have no Skip: a wrong answer gets a hint, then a comparison, then a worked demonstration, and the answer is shown after three misses with Continue.
- **Top bar:** progress dots, Replay explanation, Pause/Resume, Help (includes credits), Review (topic status, replay links, optional practice), Settings.
- **Settings:** subtitle on/off and size, separate narration/music/effects volumes, reduced motion, skip camera travel, wait-for-Continue after each line, labels, quality (Auto/PC/Tablet), and reset all progress.
- Unavailable buttons stay visible but dimmed. Tapping one explains why.

See `Polish/WOW_REDESIGN_2026-10-06.md` for the presentation redesign (introduction, pacing, Niko, labels, rods/cones, ending, sound).
See `Polish/INTRO_PASS_2026-10-06.md` for the sliding-gate entrance, the intro dolly, Niko's entry and the intro-time interface rules; the permanent operating guide is the repository's `CLAUDE.md`.

## Configuration (`Assets/HumanEye/Data/ModuleConfig.asset`)

Learner grade label (default "Not specified"), suggested session length (0 = self-paced), PC/tablet target FPS (60/30; targets, not guarantees), mastery threshold (2 distinct unassisted correct items), and the PC/tablet quality level names.

## Privacy

There are no accounts, names, voice recording, microphone or network calls. Progress, accessibility settings and the checkpoint are stored anonymously in local PlayerPrefs and can be reset in Settings. Ask Niko runs entirely offline.

## Related documents

`PROGRESS.md`, `CUE_SHEET.md` (generated), `COVERAGE.md`, `STORYBOARD_CONFLICTS.md`, `VALIDATION_REPORT.md` (generated), `TEST_REPORT.md`, `MISSING.md`, `ASSET_REGISTER.csv`, `THIRD_PARTY_NOTICES.md`, `VO/HumanEye_VO.csv`, `VO/VO_MANIFEST.csv`, `VO/LEDGER_CHECK.md`.
