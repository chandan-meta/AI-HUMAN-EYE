# Human Eye visual reference v2 — implementation and QA

The opening described below is superseded by the owner's later
[gate-to-face, blink and lip correction](GATE_FACE_OPENING_2026-10-14.md). Other v2 lesson improvements remain.

The existing `HumanEye_Observatory` lesson now uses the owner's Rack 06 brain, a revised lab opening,
matching waist-up anatomy transitions, warm neural packets and a live Niko presenter during close-ups.
The six images embedded in `Human_Eye_Visual_Reference_Book_v2.pdf` were the visual references; separate image
attachments were not present. This is an adaptation to the existing lab and Niko, not an exact image reproduction.

## Scope and reproduction

- Active launch scene: `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`.
- Unity 6000.3.0f1, URP 17.3, Cinemachine 3.1.5, existing saved UGUI Canvas.
- Apply last: **Tools > Human Eye > Authoring > Apply visual reference v2 (keeps scene edits)**,
  `HumanEye.EditorTools.Builder.ApplyReferenceV2`. Earlier full-generation commands restore earlier art direction;
  reapply v2 after using them. The additive patch preserves subject transforms and semantic parts.
- Build: `HumanEye.EditorTools.PlayerBuilds.Windows` or `.WebGL`. Existing release gating is retained.
- Hidden Windows evidence: `HumanEye.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900
  -heAutopilot -heOffscreen -heFast 1 -hePresenterAudit -heCapture <folder> -heRecord <folder>`.
  The development driver performs the lesson's public actions and saves progress in its player preference store.
  Use `-heIntroOnly -heIntroStep 1` for timed opening captures; `-heChapter 7 -heStop 1` for Scene 9.
- Existing sandbox `C:\he-sandbox\AI-HUMAN-EYE` was used for Unity execution while the owner's Editors stayed open.
  Source changes belong to the existing workspace, not a replacement project. Pre-change disk backup:
  `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-v2`.

## Camera and timing

The [shot plan](V2_SHOT_PLAN_2026-10-14.md) records the camera type, target, presenter mode, transitions and sounds.
Opening duration remains 32.5 seconds; reduced motion retains the existing short version. This opening is a music
and caption prelude, with educational VO starting on Niko's medium shot. It was not rewritten into new narration.
The start is 14.49 metres from the subject along the viewing axis, followed by a 1.2-second composition hold and
an eased curved approach. After the full-body title, the camera eases into matching 2.65-metre, 35-degree waist-up
framing for skin, muscles, skeleton and organs, following the owner's later correction. A soft mask removes the lower
body before the first wipe; the scan ring and motes travel down the visible upper body. The organ phase keeps the same
composition before the eye is extracted; a faint body silhouette supplies anatomical context.
The opening uses a 54-degree field of view. Brain and torso organs clear before the eye close-up to prevent occlusion.

Scene 9 holds the eye, nerve and display brain in one frame, with a restrained 24-centimetre lateral move.
The light beat is 3 seconds and the signal beat at least 6.5 seconds. Pathway and brain beats retain their 7-second
minimums. Each beat waits for the preceding VO, and the content waits for the corresponding beat. Skip, pause,
reduced motion and chapter cleanup use the existing cinematic ownership and reset logic. The duplicate scan cue
S02_L05 remains retired; all 65 active ledger cues and the supplemental closing clip are retained.

## Rack 06 import and replacement inventory

Exact original: `C:\Users\ADMIN\Documents\Received Files\Rack 06.fbx`.
Imported original and extracted derivative: `Assets/HumanEye/ThirdParty/UserModels/RackBrain/`.
SHA-256 of the original: `2ecc840ce5fe81aedf01e902ac667486fe1d7883fa672f03c68cd722050c3d41`.
`Tools/HumanEye/export_rack_brain.py` extracts eight structures, excludes cabinet/glass, keeps original UVs and
77,882 triangles, smooths normals, normalizes scale/orientation and adds registration markers. Original diffuse,
normal and AO maps are extracted at up to 2048 pixels. Unity imports normals and computes Mikk tangents.

| Use | Generated mesh | Triangles |
|---|---|---:|
| Isolated display and eye-to-brain demonstrations | `UserBrain_Rest` + `UserBrain_Visual` | 72,146 + 5,736 |
| Intro organs, translucent head, floating visual system | `Human_Brain` | 77,882 |
| Older generated preview geometry | `Brain`, `Brain_VisualArea` | Rack 06 derivatives |
| Legacy `Human_Layered.prefab` | `Rack06_LegacyHuman` | 77,882 |
| Legacy `Core_VisionLab.unity` | `Rack06_LegacyLab` | 77,882 |

Mesh GUIDs, anatomy parts, transforms, focus/pick behavior and runtime links are preserved. Recovery scenes inherit
the shared geometry but their old material assignments were not reauthored; they are backups, not launch scenes.
Searches found no applicable brain Timeline or Addressables instantiation. Unused originals are retained as source
history. Runtime mesh references are listed in `Logs/rack06-references.txt`.

Four URP Anatomy materials use the original albedo and normals, with AO controlling the specular mask. Pink tissue
tint, restrained specular (.06), low wetness (.02), wrap (.24), rim (.06) and gloss (.3) reduce plastic highlights.
This is the existing lightweight anatomy shader, not a volumetric subsurface scattering implementation.

## Signals, eye, lighting and sound

The nerve sheath, three thin supporting fibres and pulse centerline share the fitted route from the supplied eye stub.
Pooled 16-point tapered LineRenderers carry warm-white heads, gold middles and orange tails at .8 metres/second,
emitting every .8 seconds with .32-metre trails. A restrained warm light follows the lead packet; the destination
uses the existing arrival glow. Emission scheduling clears on stop, making replay deterministic.
The wide enlarged nerve connection is explicitly labeled **schematic**. The subsequent head sequence retains
optic nerves, chiasm, tracts, LGN, illustrative radiations and visual cortex; it does not teach the wide display
as a literal axon entering an arbitrary cortical surface.

Retina tint is warmer; the source eye's sapphire iris, vessels and transparent cornea/lens are retained.
Bloom is .18. Existing tone mapping, quality tiers, reflections, lab lighting and mobile settings are retained.
The room opens visibly lit, with restrained scan rings/motes and a soft waist-level reveal mask. Existing synthesized
intro music, ambience, reveal swishes, arrival accents and UI sounds are reused through the existing mixer/ducking.
No new packages, online assets or replacement voice recordings were introduced. No lightmap bake was warranted
for the moving anatomy and material/camera changes.

## Live Niko and coverage method

`NikoPresenter` reuses the original Niko model, procedural eyes/face/gestures and existing portrait camera.
Its 384-square RenderTexture appears in a soft-blue saved Canvas panel when narration is active and the main
camera cannot present a readable Niko. Main-camera visibility requires a suitable shot, safe projected position,
positive depth and a minimum projected head size. PiP appears at full opacity on speech onset, with a short scale
settle, so initial words cannot precede visibility. The main camera excludes Niko's layer during PiP to avoid clones.
The panel uses safe margins and moves left for the wide eye-to-brain shot, away from the brain. Other cinematics
use the opposite corner; normal lessons use the upper-left space beside the existing right-hand controls.

Core, replay, answer and supplemental speech share this logic, including paused audio. The development audit checks
every narrated frame and samples opening/middle/end (.02/.50/.94) of each clip. Logs also record presenter mode,
eye scale and gesture. These checks establish runtime state; rendered sample frames are separately inspected for
occlusion/readability. They do not by themselves prove visual quality.

## Verification and remaining limits

The complete 446.5-second development recording completed all 10 chapters and 65 active cues, with 13 activity
evidence records, 14,789 narrated frames, zero presenter failures and zero pointer failures. It includes the driver's
pause/replay checks and required interactions, with no human input. That recording predates the later waist-up
opening correction; narration and the rest of the lesson are unchanged. The final opening has its own fresh recording,
16:9/4:3 rendered checks and two passing cinematic regression tests, including skip/replay and Scene 9 cleanup.

All 204 extracted narration samples were visually reviewed: 195 core-cue samples, six samples from two replayed cues
and three from the supplemental closing line. Niko is recognizable in every sample. One very short cue's final sample
was recovered from the recording and manually confirmed; its raw status remains `REVIEW_FRAME`, not an automatic pass.
The extraction subtracts the recorded start offset (0.45 s) from log timestamps. See the
[visual-review record and contact sheets](Captures/v2-2026-10-14/narration/visual_review.md).

Delivered development builds and runtime evidence:

- [Windows player](../../../Builds/HumanEye/Windows-ReferenceV2-20261014/HumanEye.exe)
- [WebGL entry point](../../../Builds/HumanEye/WebGL-ReferenceV2-20261014/index.html) — serve over HTTP
- [Final upper-body opening, with audio](../../../Builds/Checks/v2-20261014/after-intro-halfbody/HumanEye_UpperBody_Opening.mp4)
- [Complete narration/interaction recording, before the final opening crop](../../../Builds/Checks/v2-20261014/full-run/HumanEye_ReferenceV2_Playthrough.mp4)
- [Test results](../../../Builds/Checks/v2-20261014/tests/)

The three passes comprise baseline/integrity, rendered comparison with corrections, and complete lesson/pacing QA.
Fixes found during those passes include first-frame ceiling cropping, eye cropping in the wide route, PiP covering
the brain, an overbright sheath, the disconnected nerve endpoint, legacy brain references, hidden-window capture UI
offset and inverted recording frames. The final camera assertions check waist-up framing and that legs remain masked,
including during the scan transitions.

### Captured comparison boards

- [Opening, approach, muscle and skeleton](Captures/v2-2026-10-14/intro_before_after_1.jpg)
- [Organs, eye approach, extraction and Niko](Captures/v2-2026-10-14/intro_before_after_2.jpg)
- [Signal teaching beat before/after](Captures/v2-2026-10-14/pulse_before_after.jpg)
- [Gold pathway and live PiP](Captures/v2-2026-10-14/hero_gold_pathway.png)
- [1024×768 eye lesson](Captures/v2-2026-10-14/tablet_025_Scene03_S03_InsideHero.png)
- [1024×768 lens close-up](Captures/v2-2026-10-14/tablet_030_Scene03_S03_Lens_Close.png)
- [Final upper-body scan transition](Captures/v2-2026-10-14/upper_body_transition.png)
- [Same transition at 1024×768](Captures/v2-2026-10-14/upper_body_transition_4x3.png)

Opening pairs use the nearest recorded intro timestamp, printed on each board. The signal pair shows the same VO
teaching beat at the printed run times; its views are 16:9 before and 16:10 after. Images are fitted without cropping.
The old player's hidden-window capture had a UI offset; compare its 3D composition rather than treating that offset
as a learner-facing UI defect. The revised capture tool compensates for the existing shifted lesson camera.

### Automated and platform results

| Check | Result |
|---|---|
| EditMode suite, including brain UVs/normals/tangents/material slots | 26/26 passed |
| PlayMode suite, including full walkthrough, replay/answer/closing speech, pause, skip and navigation | 7/7 passed |
| Cinematic regression after the final waist-up correction | 2/2 passed; fixed-camera holds, lower-body masking, skip/replay and Scene 9 |
| Scene/content validator | 72 passed, 0 warnings, 0 errors |
| Windows development build | Succeeded, 487.8 MB; 0 errors, 2 warnings |
| WebGL development build | Succeeded, 280.4 MB; 0 errors, 4 warnings |
| 1024×768 reduced-motion/stress run | 65 cues, 10 chapters, 14 activity evidence records; 2,755 narrated frames, 0 presenter failures; 0 pointer failures |
| 1600×1000 Scene 9 run | Completed; rendered route, brain, live PiP, subtitles and controls inspected |
| Final upper-body opening at 1600×900 and 1024×768 | Rendered and inspected; legs hidden, scan effect visible, safe controls; recorded opening completed with 0 presenter/pointer failures |

The first WebGL build was also launched in the in-app browser: Start, opening, eye lesson and live PiP worked, with
no console errors. This was a smoke test, not a full browser lesson or physical tablet test. The default Unity WebGL
template uses a 960×600 embedded canvas, below this module's supported 1024×768 minimum; use a correctly sized host
canvas for deployment. A viewport override/fullscreen test in the in-app browser did not establish a reliable
full-screen sizing result. Browser deployment/layout integration remains unverified. The final Windows and WebGL
builds include the later waist-up framing correction.

The existing appended-light-probe warning remains at runtime. Build warnings include unavailable Unity Cloud native
symbol upload; the first WebGL import also reported a Unity UI shader macro redefinition. Browser storage synchronization
reports a Unity deprecation warning. No project shader compatibility error was observed. The normal and stress runs
use the separate `.QA` PlayerPrefs key, preserving learner progress.

The first long recording aborted at 98 seconds because camera-space capture spanned frames and interfered with
the automated click. Continuous recording now submits and restores the Canvas in the same end-of-frame operation;
the ordinary lesson uses the original screen-space overlay. This was a capture-tool defect, not a new gameplay
requirement. It is retained in the issue history rather than presented as a completed playthrough.

### Measured performance

On the local RTX 5060 Ti at 1600×900, the baseline hidden-window, fast-6 screenshot run reported chapter averages
of 54.3–60.0 engine fps and 529–560 MB peak system memory. The complete real-time recording reported 59.6–60.0
engine fps, a worst frame of 84 ms and 814–891 MB chapter peak system memory, with 150–308 average draw calls
(maximum 1,056) and 109k–889k average triangles. The final upper-body opening recording reported 59.9 engine fps,
89 ms worst frame and 571 MB peak system memory. These are instrumented development runs, including QA/capture
overhead. The recorder submits images at 30 fps, while the earlier hidden screenshot run renders only for captures;
the two workloads do not establish a gameplay speedup or a 60 fps rendering guarantee. GPU timings were unavailable.
Physical tablet/browser performance still needs measurement on the deployment device.

### Changed source and saved assets

Authoring: `Builder.ReferenceV2.cs`, `PlayerBuilds.cs`, `export_rack_brain.py`. Runtime: `Stage.Opening.cs`,
`Stage.EyeBrain.cs`, `AnatomyRevealController.cs`, `SignalPulses.cs`, `Narrator.cs`, `NikoRig.cs`, `NikoPresenter.cs`
and `Hud.cs`. QA: `AutoPilot.cs`, `.Presenter.cs`, `.Capture.cs`, `.Record.cs`, `review_presenter_recording.py`,
`RackBrainTests`, `NikoPresenterTests`, `CinematicTests` and `WalkthroughTests`.
Saved assets include the launch scene, `ObservatoryCanvas.prefab`, RackBrain sources/maps, generated brain meshes,
four brain tissue materials, muscle/bone/retina/nerve/signal materials, the existing postprocessing profile and the two
legacy brain placements listed above. Exact serialized file paths and SHA-256 changes are in
[copyplan.json](../../../Builds/Checks/v2-20261014/copyplan.json) and the later
[halfbody-copyplan.json](../../../Builds/Checks/v2-20261014/halfbody-copyplan.json).
All copied assets were checked against the pre-copy workspace hash; no concurrent user changes were overwritten.
The owner's open Editor was not operated. Reload the saved scene before further editing so an older open buffer does
not overwrite the updated on-disk scene.

The final half-body review also caught spline overshoot during nominally identical camera holds. Separate matching
path segments now keep muscle, skeleton and organ compositions fixed; a regression assertion checks that they do not drift.

The body layers come from separate supplied models. Their stage registration and framing are consistent, but their
proportions are not identical and no shared skeleton or custom retopology was created. The opening still has a rapid
final approach from organs to eye within the unchanged 32.5-second prelude. The lab, Niko and source eye differ from
the reference art. Specialist anatomy review, physical tablet/touch performance and production voice replacement remain outside
the evidence obtained here. Supplied model redistribution rights remain undocumented, as in the existing release gate;
see `Docs/HumanEye/THIRD_PARTY_NOTICES.md` and `ASSET_REGISTER.csv`.
