# Observatory polish — QA report

> Authoring update, 3 October 2026: the live HUD is now a saved native Canvas prefab with editable Hierarchy objects. See [Scene authoring](SCENE_AUTHORING.md). UI Toolkit and earlier visual/device results below describe the pre-conversion version; they are not validation of the new Canvas. Full-scene regeneration replaces manual edits and is not needed to build the saved scene.

Started 3 October 2026. Branch `codex/observatory-polish`, baseline commit `0913934`. Earlier HANDOFF.md changes preserved. No Unity/package upgrade or new framework.

## Baseline

Unity 6000.3.0f1 / URP 17.3 / Cinemachine 3.1.5 / Input System 1.16. Shared UI Toolkit HUD; ten logical chapters (2–11) in HumanEye_Observatory. Windows and Android configured. Existing 66 narration assets and exact ledger retained. Existing curated offline Ask Niko retained.

Source inventory: Hud.uxml / Hud.uss / Hud.cs (all screens); ChapterRunner (flow); Stage partials (world actions); Builder partials (generated assets/scene); CameraDirector (Cinemachine); Narrator/AudioDirector; SaveSystem (PlayerPrefs).

Reference corrections: progress must be 2…11 exactly once; only relevant actions receive emphasis; lens selection must match lens narration; Scene 2 stays exterior until Inside View; internal SupplementalAdaptive labels are not learner copy.

Runtime baseline: `Builds/Checks/polish-before/`, `Logs/polish/before.log`. The first sandboxed attempt failed on PlayerPrefs write, then produced follow-on uninitialised profiler errors. Execution was rerun with host access. A relative capture path also exposed a driver defect; AutoPilot now resolves its path before screenshot capture.

## Required rounds

| Round | Focus | Status |
|---|---|---|
| 1 | Shared UI, layouts, native event bindings | Four native-player runs finished; editable Figma tokens/components/overlay frames now authored; remaining sub-states and visual acceptance incomplete |
| 2 | Flow, save/resume, rapid actions, modals, cameras | Automated 1024×768 stress dry run passed; manual/visual acceptance incomplete |
| 3 | Presentation, lighting, materials, SFX, VFX cleanup | Implemented scene/material/camera pass; 1600 and reduced-motion 1024 full runs passed; floor defect fixed and retested |
| 4 | Full regression, Windows/Android builds | Windows/Android development builds succeeded; 21 edit-mode tests and one full play-mode test passed; full 1920/1280 walkthroughs and final 1024/1600 menu smoke checks passed |

Four complete implementation/playtest/fix rounds have **not** been delivered. Automated player actions are not physical mouse/touch tests. Captures do not certify listening quality. See the root HANDOFF.md for exact continuation steps.

## Access limits

The new Figma account can edit https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg . It contains tokens/styles, reusable button and panel components and 15 UI specification frames. The old file is inaccessible to this account and remains empty. No physical tablet or Windows touchscreen has been tested.

## Issues

| ID | Reproduction / actual vs expected | Severity | Fix / source | Retest |
|---|---|---|---|---|
| UI-01 | Long resume label spills past button in baseline menu; should wrap | Medium | Shared wrapping button style, bounded start card; Hud.uss | Passed sampled menu captures in four size runs |
| UI-02 | Completed aqueous entry stays accented during lens explanation; selected and completed must differ | Medium | Focused-part arrow/selected state and done checkmark; Hud.cs | Passed: r1-1600/030_Scene03_S03_Part_lens.png shows lens selected and aqueous complete |
| UI-03 | Optional card exposes internal authoring tag | Low | Learner-facing optional-practice label; Hud.cs | Changed; optional flow exercised in full runs; final visual acceptance pending |
| UI-04 | Scaling makes 48 logical px controls smaller on low resolutions | Medium | ConstantPixelSize, compact layout; Builder.cs/Hud.uss | Passed sampled non-scroll button bounds at all four sizes; progress indicators excluded |
| UI-05 | Settings opened behind the start menu | Medium | Bring newly displayed modal to front; Hud.Show | Passed subsequent size-specific captures and submit-event checks |
| UI-06 | Two world labels cross right edge at 1024×768, Scene 2 hero | Medium | Clamp after separation; WorldLabels.cs | Dry run: no out-of-window controls across 118 samples; not every label state captured; overlap review remains |
| VIS-01 | Menu shows giant apple; intended eye/Niko/brain absent | Medium | Stage.ShowMenu startup reset added; dedicated shot still needed | Dry-run menu has brain/eye/Niko; Niko cropped and eye obscured by menu, composition still incomplete |
| VIS-02 | Niko cropped in close shots; lens opaque; labels overlap view controls | Medium | Presentation/camera work remains | Not fixed |
| QA-01 | Relative capture paths fail under player data folder | Medium | Full path in AutoPilot.Start | Passed four complete runs with absolute paths |
| QA-02 | Hidden-window desktop screenshots black; initial render target retains stale UI | Medium | Camera/native-panel render-target capture; clear panel texture per capture | Passed size-specific captures; initial captures superseded |
| DATA-01 | Rebuilding post profiles deletes .meta/GUID | Medium | Update existing profile/subassets in place; Builder.Environment.cs | Build passed and main .meta unchanged; no dedicated asset-reference regression test |

## Round 1 — first runtime pass

`polish-r1-first`: actual Windows development player, D3D11, 1600x900, 6x walkthrough. GPU camera + native UI Toolkit render-target captures (hidden-window screen capture returns black). Menu settings button event was delivered, but screenshot exposed Settings behind Start. Fixed modal stacking at the shared Show() method and corrected stale UI texture clearing; reran all four sizes below. Initial first-pass screenshots are superseded.

The initial menu world shows the wrong large apple because there was no authored startup camera/state. This is tracked for the presentation round; UI work alone does not fix it.

## Round 1 — full runs after shared UI fixes

All used the same round-one Windows development binary, D3D11, hidden window, walkthrough speed 6x, camera/native-UI render-target captures. The machine is the current DELL Windows host; no independent hardware/performance certification was performed.

| Evidence directory under Builds/Checks | Window | Result |
|---|---|---|
| polish-r1-1920 | 1920×1080 | Passed journey: 10 chapters, 66 cues, exit 0 |
| polish-r1-1600 | 1600×900 | Passed journey: 10 chapters, 66 cues, exit 0 |
| polish-r1-1280 | 1280×720 | Passed journey: 10 chapters, 66 cues, exit 0 |
| polish-r1-1024 | 1024×768 | Passed journey: 10 chapters, 66 cues, exit 0; failed two label bounds |

Each directory contains screenshots and walkthrough.log. Player logs: Logs/polish/polish-r1-*.log. No logged FAIL assertions or matched runtime exceptions were found in these four runs. Layout audit excludes scroll contents and passive scene progress indicators. It does not detect every visual overlap. These runs preceded the new Figma file and later scene changes.

Before visuals: Docs/HumanEye/Screenshots/INDEX.md maps prior 2 October player captures. The black polish-before captures are not a valid visual baseline. Current render-target captures are not screenshots from the physical desktop or a tablet. Hidden-window FPS/draw-call samples are not representative of ordinary continuous rendering and must not be used to claim performance gains.

## Round 2 — compiled checkpoint and requested dry run

Changes include modal pause/restore and input blocking, progress-overwrite confirmation, partial Scene 2 resume, return-to-menu cleanup, duplicate-binding protection, owned single demonstration for rapid Trace/Replay, paused-input guards, bounded orbit, label clamping, and development-only automated launch. AutoPilot.UI.cs adds stress assertions for these paths.

Latest Windows development build: Logs/polish/round2-build.log; succeeded 3 October 2026, 01:42 IST; 207.2 MB; 0 errors, 1 warning (native symbols could not be uploaded to Unity Cloud). This existing binary was tested in the following dry run; no runtime code or build changes were made for the dry run.

Command: `Tools/HumanEye/playtest.ps1 -Run dry-run-20261003 -Width 1024 -Height 768 -Stress`.

Conditions: 3 October 2026; Windows development player, D3D11, NVIDIA GeForce RTX 3050 Laptop GPU, PC quality, 1024×768, hidden-window GPU/native-UI captures, 6× walkthrough, isolated `.QA` PlayerPrefs. This is automated runtime testing, not physical pointer/touch input or a performance benchmark.

**Passed:** exit 0; Scenes 2–11 complete; 66 cue records; 20 explicit assertions passed and 0 failed. Completion logged after 188.25 seconds; 117 PNG captures; 118 layout audits with no logged undersized or out-of-window non-scroll buttons. No runtime exceptions matched in the player log.

Passed assertions cover menu Settings, Help pause/restore, modal world-input blocking, caption setting, unsupported Ask Niko response and hold release, pause/resume, rapid repeated Trace, paused camera, Freeze, return-to-menu effect cleanup, checkpoint/cue persistence, overwrite confirmation/cancel, chapter resume and completed-save resume. Additional logs show voice time unchanged during pause (0.92→0.92 s) and lens accommodation unchanged during Freeze (0.590→0.590). Save/reload is within one process; application-restart persistence remains untested. Flow checks do not prove audio quality or every possible control combination.

Evidence: `Builds/Checks/dry-run-20261003/walkthrough.log`, PNGs alongside it, and `Logs/polish/dry-run-20261003.log`. Visually inspected captures: `001_start_screen.png`, `006_help_modal.png`, `007_settings_xl.png`, `013_Scene02_S02_EyeHero.png`, `048_new_session_confirmation.png`. Confirmation is readable and protects the save; the initial menu correctly hides Continue. Remaining visible defects: cropped Niko, eye hidden behind the main menu, and anatomy/view-panel overlap. The dry run passes automated flow checks, not visual acceptance.

## Remaining acceptance

Rounds three/four, fresh EditMode/PlayMode/validator results, updated Android build, separate-process save/resume, real pointer/touch input, physical tablet performance, narration/SFX listening and final before/after review are not tested in this polish pass. The existing Android APK and historical validation reports predate these changes. No external assets or new dependencies were added in this pass. Required ledger/content/package files have no diff.
