# Human Eye — project handoff

User preference: use the Ponytail skill for coding work until explicitly disabled.

## Cinematic entrance update (5 October 2026)

The saved scene now starts at the closed gate and includes a 15-second entrance, human spotlight, face-first scan, collision-backed lab walking with an authored joystick, and staged anatomy controls. Use `CinematicPatch.Apply` / **Tools > Human Eye > Authoring > Apply cinematic entrance (keeps scene edits)** for this patch; do not regenerate the module. The user selected MisterH's Sketchfab eye `f2370371544c4b029123da0b7d7e2457`; CC BY 4.0/downloadability verified and the user-supplied GLB is integrated as the closed-eye exterior. Cutaway and pupil-response teaching retain procedural anatomy. Apply with MisterHEyeImport.Apply; conversion source and original GLB are retained. Current changes, evidence and remaining acceptance work: [cinematic report](Docs/HumanEye/Polish/CINEMATIC_ENTRANCE_2026-10-05.md).

Updated: 5 October 2026 (Asia/Calcutta). **Source implementation is saved and tested; final Windows rebuild and production acceptance remain incomplete.**

## Resume here

- Current branch: `claude/wow-moments`; remote: `https://github.com/chandan-meta/AI-HUMAN-EYE.git`. This handoff accompanies the cinematic entrance and supplied-eye integration commit.
- Open `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` in Unity 6000.3.0f1. Preserve the authored scene and Canvas; do not run full `Builder.Build` regeneration. The saved scene already contains both patches.
- Completed: 15-second gate entrance, human spotlight, face-first scan, lab navigation/joystick with collision, layer controls, muted narration captions, environment lighting/material copies, and MisterH's user-supplied eye exterior. Ten chapters and 66 original narration cues remain. The model's CC BY 4.0 attribution is in `Docs/HumanEye/THIRD_PARTY_NOTICES.md` and is copied alongside Windows builds.
- Verified locally: Edit Mode 25/25; content validator 70/70; Play Mode 2/2 after model integration; final mesh winding/cutaway/pupil-fallback check 1/1. Full standalone walkthroughs at 1600×900 and 1024×768 passed before the final mesh correction. Logs/captures under `Logs` and `Builds/Checks` are ignored local evidence, not pushed artifacts; detailed results are recorded in the cinematic report above.
- **Next action:** save and close the interactive Unity Editor, then rebuild via `HumanEye.EditorTools.PlayerBuilds.Windows`, or use **Tools > Human Eye > Build Player > Windows** in that Editor. Batch build was prevented when an interactive Editor opened the project at 18:20 local. The current local EXE predates the corrected mesh winding; it is not the final deliverable. After rebuilding, repeat one standalone lesson walkthrough and inspect the imported exterior, cutaway and Scene 5 pupil response.
- Remaining acceptance: physical-tablet performance and OS multitouch, sustained desktop performance, final artistic review, and existing third-party release gates. Accelerated capture profiling averaged 55.3–58.1 fps on RTX 5060 Ti; the 60 fps desktop target is not yet established by a sustained benchmark.
- The pre-existing owner edit to `Assets/human brain env/human brain env/human brain env/Materials/Mt8.mat` is preserved locally and excluded from this commit.

This file records the current workspace for the next developer/chat. The attached implementation brief is reference material for continuation; it is not evidence that its requested work has been completed.

## Latest change: staged on center_rotator, user models, cinematic intro (5 October 2026, branch `claude/wow-moments`, uncommitted)

The module now plays inside the owner-placed HUMAN BRAIN ENV room: the whole room prefab instance (and its baked Area Light) was translated so the `center_rotator` disc top sits directly under the hero eye, which keeps every shot, anchor, nerve path and light valid. The human figure is the owner's Reallusion "Matt" (static export, 28k body) with the owner's skeleton as the X-ray layer; the Brain Display is the owner's Brain.fbx with a generated visual-area mesh. Scene 2 opens with a skippable 11.5 s cinematic power-up (`Do("intro")`, shots S02_IntroThreshold/Ignite/Crane, project-generated SFX in `Assets/HumanEye/Audio/SFX`). All of it is applied by the idempotent, save-guarded **Tools > Human Eye > Authoring > Stage on center_rotator (keeps scene edits)** (`Assets/HumanEye/Editor/Builder.Display.cs`); never use the full regeneration. Brief: `Docs/HumanEye/MASTER_PROMPT_center_rotator.txt`; evidence: `Docs/HumanEye/Polish/ROTATOR_PASS_2026-10-05.md`. New release gates: licences of Matt (Reallusion EULA), Brain.fbx, the skeleton and the room package (THIRD_PARTY_NOTICES §7).

## Previous change: experience pass (5 October 2026, branch `claude/wow-moments`, uncommitted)

Focus challenge in Scene 6, predictions in Scenes 7 and 9, adaptive challenge items for Secure topics, grounded Ask Niko "what now?", end-card recap, and fixes for untappable tap-part questions, the card covering the model, blank Scene 9 insets and the menu hiding the eye. Scene edits go through `Tools > Human Eye > Authoring > Apply polish patch (keeps scene edits)`; content through `Update content data only (keeps scene)` — never the full regeneration. Details, asset-library findings and test evidence: [WOW_PASS_2026-10-05](Docs/HumanEye/Polish/WOW_PASS_2026-10-05.md). New release gate: the microscope exhibit's licence (THIRD_PARTY_NOTICES §7).

## Previous change: pointer-click repair (4 October 2026)

The user reported SCAN unclickable and a MissingComponentException for btn-home's CanvasRenderer. HudIcon and GlassPanel now explicitly require CanvasRenderer. The repair saves renderers on all graphics without restyling/rebuilding the scene: the prefab went from 481 to 506 saved renderers (25 missing renderers, including Home and icon children). All 565 RectTransforms are retained. Backups and verification logs are in `Logs/pointer-fix`.

Previous submit-only interaction checks did not validate pointer raycasts. AutoPilot now raycasts through EventSystem before its UI clicks and audits active button centres throughout player walkthroughs. Batch Editor's 640x480 Game view is below the tested 1024x768 minimum, so the all-controls layout/pointer audit runs in standalone players; explicit Start/SCAN/Inside pointer clicks also run in Play Mode tests. Hidden, clipped scroll entries and controls deliberately covered by a modal are excluded until reachable. This is in-engine pointer evidence, not OS mouse/touch automation.

## Previous change: reference glow on the saved Canvas

The user rejected the flat Canvas appearance. Saved UI now has GlassPanel surfaces with a shared UI shader, cyan active/hover/focus halos, HudIcon children, progress circles, a Home button, menu arrows and compact narrow-button layouts. The prefab contains 565 editable RectTransforms. No runtime UI objects are generated. The scene's 771 non-prefab GameObjects were preserved. See `Docs/HumanEye/GLOW_VALIDATION.md` and the glow-editing section of `Docs/HumanEye/SCENE_AUTHORING.md`.

The 3D models/environment still differ substantially from the reference illustrations; exact visual acceptance is not achieved. Glass is a translucent gradient/rim effect, not real-time background blur. Do not claim a pixel-identical match. Normal builds use the saved prefab; the explicit ReferenceUiStyle authoring command resets style/layout defaults and should not be rerun over manual styling without intent.

## Previous change: editable Hierarchy / Canvas

The user requested every UI object to be editable in the Unity Hierarchy before Play. The active scene now uses a saved native Canvas prefab instance with 526 RectTransforms, including inactive list/quiz/label slots. `Hud.cs` binds serialized scene references; no runtime buttons or panels are created. UI Toolkit is no longer the live HUD.

- Edit **HUD > Observatory Canvas > root**. Select HUD for the Inspector panel-preview control.
- Prefab: `Assets/HumanEye/UI/ObservatoryCanvas.prefab`; guide: `Docs/HumanEye/SCENE_AUTHORING.md`.
- The 3D scene objects already existed before Play. Added an editable Menu Eye Point and preserved authored Niko scale.
- Full Play Mode walkthrough passed: all 10 chapters / 66 cues, with the same initial 518 UI instance IDs before and after gameplay; eight authored scrollbar objects were subsequently added (`Logs/authoring/playmode.xml`).
- Final native-Canvas Windows walkthrough passed 21 interaction checks with all 66 cues. The latest Windows build includes the dock/header corrections. See `Docs/HumanEye/CANVAS_VALIDATION.md`; earlier screenshots and Android APK predate this conversion unless explicitly refreshed below.
- Full regeneration is now labelled **Authoring > Regenerate entire module (replaces manual edits)** with a warning. Normal player builds use the saved scene. Preserve all manual overrides.

## Current state

- Repository: https://github.com/chandan-meta/AI-HUMAN-EYE
- Current branch: `codex/observatory-polish`, based on `0913934` (`feat(human-eye): playable Scenes 2–11 module, verified builds and docs`). The earlier branch was `feat/s02-meet-niko`.
- Changes are **uncommitted and unpushed**. Preserve the working tree, new AutoPilot partials and their .meta files. The original handoff edit predated the polish changes.
- Active module: `Assets/HumanEye/`, covering storyboard Scenes 2–11 through ten chapter definitions in one Unity scene.
- The earlier `Assets/_Project/` Scene 2 prototype is superseded, but its shared source assets and release audit are still used. Its scenes are disabled in Build Settings.
- Unity **6000.3.0f1**, URP 17.3, Input System, Cinemachine 3.1.5. Preserve the existing versions.

## Open and run

1. Obtain the repository and Git LFS assets (`git lfs pull`).
2. Open in Unity 6000.3.0f1 and allow imports to finish.
3. Open `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` (the enabled launch scene), press Play, then **Start**.
4. Existing anonymous progress offers Continue; Settings can reset progress.

Local development outputs (ignored by Git):

- Windows: `Builds/HumanEye/Windows/HumanEye.exe`. Rebuilt for the native Canvas conversion; see latest validation below. Distribute its entire adjacent build folder.
- Android: `Builds/HumanEye/Android/HumanEye.apk` (ARM64, minimum API 29). Refreshed successfully on 3 October at 07:06 UTC with the earlier polish sources (before the Canvas conversion); compressed APK is 78.0 MiB. Not tested on a physical device.
- Windows exit-crash workaround: launch `HumanEye.exe -force-d3d11`. The recorded D3D11 checks exited cleanly; the project-wide graphics setting remains unchanged.

## Where to work

- `Assets/HumanEye/Editor/HumanEyeContent.cs`: authored chapter steps and content.
- `Assets/HumanEye/Scripts/Core/ChapterRunner.cs`: chapter interpreter; `Routine.cs`: owned scheduling; `Narrator.cs`: narration sequencing.
- `Assets/HumanEye/Scripts/Stage/`: world actions; `Anatomy/` and `Demos/`: eye geometry and demonstrations.
- `Assets/HumanEye/Scripts/UI/` and `Assets/HumanEye/UI/`: HUD; `Interaction/PointerSelector.cs`: mouse/touch arbitration.
- `Assets/HumanEye/Data/DialogueLedger.json`: 66 exact storyboard cues. Keep supplemental learning content separate.
- `Docs/HumanEye/VO/HumanEye_VO.csv` and `VO_MANIFEST.csv`: recording script and cue-to-file mapping.
- Ask Niko uses local curated intents. Progress/settings use anonymous local PlayerPrefs; no remote AI, backend or microphone is configured.

Use **Tools > Human Eye > Build Module** after authored content changes. It updates generated assets in place under `Assets/HumanEye/Generated` and `Assets/HumanEye/Scenes`, and creates ModuleConfig if missing. Prefer editing source authoring code over generated output.

Then run **Tools > Human Eye > Validate Module**, the `HumanEye.Tests.EditMode` and `HumanEye.Tests.PlayMode` suites, and **Tools > Human Eye > Build Player > Windows / Android (tablet)** as relevant. Batch commands and walkthrough capture flags are in [README](Docs/HumanEye/README.md).

Keep Pause independent of `Time.timeScale`, FREEZE limited to the visualization clock, and chapter exit/reset cancelling owned routines. Narration timing follows clip length.

## Current polish handoff — start here

The supplied brief describes four implement/playtest/fix rounds: UI with editable Figma designs; gameplay and cinematic flow; art/audio/VFX; final regression and target builds. Rounds one/two native checks passed earlier. Figma now works in a new account-owned file. Round three includes real scene/material/camera changes and two completed full walkthroughs. Round-four automated regression and Windows/Android development builds passed. The full requested visual, physical-device and production acceptance remains incomplete.

### Latest work (3 October, daytime session)

- Active Figma: https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg . Shared tokens/styles, ten button states, eleven panel/navigation components, 15 UI specification frames including 4:3. These are editable overlays, not finished 3D art. Detailed limits and IDs: Docs/HumanEye/Polish/FIGMA_STATE.md.
- Actual Unity edits: centred menu and dedicated menu shot; safer Niko/eye framing and aspect-aware camera FOV; eye plinth; warm room fixtures and nebula; anatomy additional-light response and restrained transparent highlights; floor facing correction; flatter Niko eye lights; ambient particle pause/freeze/reduced-motion handling; one low-resolution reflection probe captured on load.
- Round-three first run: Builds/Checks/polish-r3-1600, all ten chapters/66 cues, 20 PASS / zero FAIL, 118 layout samples, no out-of-window or small controls. Visual review caught a downward floor mesh.
- Round-three correction run: Builds/Checks/polish-r3-fixed-1024, all ten chapters/66 cues with reduced motion. Floor correction verified; menu Niko placement prompted a further fix.
- Edit-mode regression: Logs/polish/round4-editmode.xml, 21 passed / zero failed, including a new generated-floor normal check.
- Latest Windows build: 208.5MB, zero errors, one warning; Logs/polish/round4-build.log. Android succeeded with zero errors/two warnings; Logs/polish/round4-android.log. BuildSummary total includes other outputs; the APK itself is 78.0 MiB.
- Final candidate: full 1920×1080 stress journey passed 21 checks; full 1280×720 reduced-motion journey passed; final menu smoke runs passed at 1024×768 and 1600×900. No sampled small/outside controls. All full journeys include all 66 cues.
- Play-mode full walkthrough test passed (164.5 seconds). Exact evidence and limitations: Docs/HumanEye/Polish/VALIDATION_2026-10-03.md. Curated before/after capture index: Docs/HumanEye/Polish/Captures/INDEX.md.

### Implemented and exercised in round one

- Shared native UI Toolkit hierarchy/styles: navy opaque panels, restrained cyan emphasis, wrapping long labels, compact layouts, visible focus, selected-part arrows and separate completed checkmarks.
- Constant-pixel panel scaling and 48 px buttons. Optional practice uses learner-facing labels; required dialogue remains in the unchanged ledger.
- Settings/menu stacking corrected after an actual capture exposed Settings behind Start.
- Automated camera/UI render-target captures, actual UI Toolkit submit-event checks and layout measurements. Development walkthroughs use a separate `.QA` PlayerPrefs key.
- Generated post profiles update in place to retain their asset GUIDs.

Four full D3D11 development-player runs completed Scenes 2–11 and all 66 cues at 1920×1080, 1600×900, 1280×720 and 1024×768. Logs/captures: `Builds/Checks/polish-r1-1920/`, `polish-r1-1600/`, `polish-r1-1280/`, `polish-r1-1024/`. Each has `walkthrough.log`; player logs are `Logs/polish/polish-r1-*.log`.

The round-one 1024×768 audit found two world labels beyond the screen edge. After the shared clamp change, the dry run logged no out-of-window or undersized controls across 118 layout samples. Audits cover visible non-scroll buttons; they do not establish that every label state was captured or that every panel is free of overlap.

### Round-two changes and dry-run result

- Hud: pause/restore when opening Help, Settings or Review; modal world-input blocking; return-to-menu actions; confirmation before overwriting progress; partial Scene 2 resume; listener rebinding protection; Ask Niko hold cleanup.
- ChapterRunner/Stage: paused input guards; a single owned demonstration for rapid TRACE/REPLAY; cleanup when leaving demonstrations or returning to menu; bounded orbit; menu startup state.
- AnatomyPart/WorldLabels: pause highlight animation and clamp labels inside the window.
- AutoPilot/UI: `-heStress` covers modal flow, unsupported Ask Niko responses, repeated TRACE, pause/freeze, leaving a live demonstration, reload and saved completion. These checks passed in `dry-run-20261003`.

Build evidence: `Logs/polish/round2-build.log`, `StandaloneWindows64 Succeeded`, 207.2 MB, 0 errors, 1 warning (Unity Cloud native-symbol upload unavailable). Build finished 3 October 2026 at 01:42 IST. Compilation is not proof that these interactions pass.

Dry run requested by the user on 3 October: `Tools/HumanEye/playtest.ps1 -Run dry-run-20261003 -Width 1024 -Height 768 -Stress`. Existing Windows development binary; D3D11; NVIDIA GeForce RTX 3050 Laptop GPU; PC quality; 6× walkthrough; isolated QA save. Result: **exit 0, 10 chapters, 66 cues, 20 assertions passed, 0 failed**, 188.25 seconds to the final completion log. Save/reload and completion resume were tested within the same process. Narration time stayed fixed during pause; lens accommodation stayed fixed during Freeze. No runtime exceptions matched in the player log. No code/build changes were made for this dry run.

Evidence: `Builds/Checks/dry-run-20261003/walkthrough.log`, 117 PNG captures in that directory, and `Logs/polish/dry-run-20261003.log`. Main menu, Help, Settings and overwrite-confirmation captures were visually inspected. Menu Niko is still cropped and the eye is obscured by the menu; this is a functional pass, not visual/release acceptance.

### Immediate next steps if implementation is continued

1. Inspect the saved dry-run evidence before making more changes. The current round-two automated flow run passed; use the command above with a fresh run name when a subsequent edit warrants retesting.
2. Complete real mouse/touch checks, exhaustive label/panel overlap review, repeated Replay and review navigation. Add a separate-process save/resume check; loading saved data in the same process is not a restart test.
3. Figma access is restored. Finish all settings/conversation sub-states and interactive controls; review remaining screen compositions against Unity. See the active file above.
4. Review the new presentation captures against the concepts. Dedicated menu framing, eye plinth, warm fixtures, floor visibility, lens highlight and particle lifecycle fixes are implemented. Further art refinement is still needed to reach the reference's richness; do not describe the procedural environment as a photorealistic match.
5. Final regression: fresh full walkthrough, saved progress, reduced motion, all four layouts, current EditMode/PlayMode suites and validator, fresh Windows/Android builds. Physical device/touch and listening checks remain separate requirements.

Rebuild generated scene/content using `HumanEye.EditorTools.PlayerBuilds.RebuildWindows` or the Build Module menu followed by Windows build. For runtime-only edits, `HumanEye.EditorTools.PlayerBuilds.Windows` uses the saved scene. Never rebuild a player while a walkthrough is using its output directory.

### Known evidence limits and visual defects

- Figma: [active design file](https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg). See [FIGMA_STATE](Docs/HumanEye/Polish/FIGMA_STATE.md) for delivered objects and review limits. The old file remains empty.
- Menu now has a dedicated shot and eye/Niko/brain composition. The 1080p final-candidate render shows Niko clear of the menu. Eye/brain overlap behind the card is intentional.
- Niko can still be partially visible at the edge of anatomical close-ups; exhaustive label/control overlap review remains outstanding. Lens highlighting is reduced and transparent, but visual polish remains below the concept-image target.
- Captures are actual Unity GPU camera renders composited with native UI Toolkit render targets. Hidden-window desktop capture was black; `polish-before` PNGs are not usable visual evidence. Use the dated prior `Docs/HumanEye/Screenshots/` captures as the pre-polish visual baseline.
- `polish-r1-first` has stale UI texture artifacts and is superseded by the four size-specific runs. Early round-one menus can show a stale QA resume label; the dry-run initial menu correctly hides Continue after QA reset, and saved-progress menus show it.
- Hidden-window profiling does not continuously render normal frames; its FPS/draw-call numbers do not certify PC or tablet performance. No physical mouse/touch/device or audio-listening acceptance was performed in this polish pass.

Current supporting documents: [UI_STYLE_GUIDE](Docs/HumanEye/UI_STYLE_GUIDE.md), [QA_REPORT](Docs/HumanEye/QA_REPORT.md). Build outputs/raw captures/logs are ignored by Git and must be copied separately for another machine.

### Input references

Original attached brief: `C:/Users/DELL/.codex/attachments/4a385579-1d6d-4e16-a01a-5b9a5da3891a/Pasted text.txt`.

Nine reference PNGs are in `C:/Users/DELL/Downloads/`, named `ChatGPT Image Oct 3, 2026, ...`:

| Filename ending | Intended view |
|---|---|
| `12_15_28 AM-1.png` | Menu |
| `12_15_29 AM-2.png` | Scene 2 human display |
| `12_15_30 AM-3.png` | Scan |
| `12_15_31 AM-4.png` | Transparent head |
| `12_15_32 AM-5.png` | Exterior-eye teaching moment; reference itself incorrectly uses a cutaway |
| `12_15_33 AM-6.png` | Inside-eye overview |
| `12_15_34 AM-7.png` | Lens/part exploration; correct the mismatched selected part |
| `12_15_36 AM-8.png` | Signals to brain |
| `12_15_37 AM-9.png` | Optional quick check |

These input files have not been copied into the repository. Preserve storyboard authority over generated-image errors, including duplicate scene numbers and incorrect selected anatomy.

## Recorded validation — 2 October 2026

These are existing report results, not tests rerun for this handoff:

- Validator: **70 passed, 0 warnings, 0 errors**.
- EditMode: **20/20 passed**. PlayMode first-run test passed through 10 chapters and 66 cues.
- Windows and Android Development builds succeeded; Android was not installed on a device.
- Windows walkthrough completed 10/10 chapters and 66/66 audio cues. Layout captures include 16:9, 4:3, 16:10 and portrait; a reduced-motion check covered Scenes 2–3.
- Recorded PC averages were roughly 57–59 FPS at 1600×900 with VSync. Tablet performance is unmeasured.
- Automated walkthroughs call public actions; they do not verify real mouse/touch hit testing. Some captures/performance checks used an earlier HUD layout revision; see the report for exact coverage.

Evidence and limitations: [TEST_REPORT](Docs/HumanEye/TEST_REPORT.md), [VALIDATION_REPORT](Docs/HumanEye/VALIDATION_REPORT.md), [screenshots](Docs/HumanEye/Screenshots/INDEX.md). Raw logs and extra captures are local under `Logs/` and `Builds/Checks/`.

## Next work / release blockers

1. Reproduce and resolve the intermittent `D3D12Core.dll` crash during Windows shutdown; use `-force-d3d11` for current review runs and evaluate the default graphics API separately.
2. Manually test mouse and touch interaction, UI interception, saved progress/resume, and a physical Android tablet's safe areas, audio and sustained performance. Windows touchscreen and iOS are untested; WebGL support is not installed.
3. Replace all **66 provisional TTS clips** with recorded narration using the manifest filenames, then rebuild. Review anatomy pronunciation, placeholder music and SFX by ear.
4. Resolve the supplied FitnessMan asset's unverified author/licence before release; retain third-party attribution and share-alike obligations.
5. Obtain anatomy and art review. The muscle layer is illustrative, the procedural eye omits ciliary body/zonules, and Niko uses procedural poses rather than authored animation/lip-sync.
6. Complete production acceptance before release builds. `Assets/_Project/Editor/AssetAudit.cs` currently blocks non-development builds; do not bypass it to label this production-ready.

Full outstanding list: [MISSING](Docs/HumanEye/MISSING.md). Decisions and implementation gotchas: [PROGRESS](Docs/HumanEye/PROGRESS.md). Asset provenance: [THIRD_PARTY_NOTICES](Docs/HumanEye/THIRD_PARTY_NOTICES.md) and `Docs/HumanEye/ASSET_REGISTER.csv`.

The historical validation above predates the polish changes. It must not be presented as validation of the current binary. No commit or push has been performed.

Pointer repair acceptance: all 22 Edit Mode checks pass (`Logs/pointer-fix/delivery-editmode.xml`); full Play Mode test passes (`Logs/pointer-fix/playmode-final.xml`); the final 1024x768 Windows run completes all 10 chapters/66 cues with 21 passing interaction checks, 90 distinct active button targets reached, zero blocked targets/layout failures/runtime exceptions (`Builds/Checks/pointer-delivery-1024/walkthrough.log`). All 139 saved Button components were structurally checked, including unused inactive pool slots. The comparison-card overlap was fixed as part of this audit. Normal builds still use the saved scene; do not regenerate/restyle it to apply this repair.
