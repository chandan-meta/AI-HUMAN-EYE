# Centered anatomy opening — 13 October 2026

The later [organ portrait update](ORGAN_PORTRAIT_2026-10-13.md) adds the owner's requested head/shoulder framing, lower-body fade, Rack 04 organ stage and transition particles. The verification below describes this earlier full-body version.

## Audit before editing

The enabled build scene is `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`. It runs ten chapter assets (Scene02–11), 65 spoken ledger cues, exploration, adaptive checks, optics experiments, nerve/brain demonstration, and a final recap. `ChapterRunner` owns cancellable `Routine` sequences. `Stage` restores checkpoints; `Narrator` owns voice/subtitles; `Hud` binds the saved Canvas; `SaveSystem` uses PlayerPrefs. The two `_Project` scenes and their S02 Timelines are disabled in build settings. SampleScene, the source environment scene, and the recovery scene are not part of the lesson build.

Unity 6000.3.0f1, URP 17.3, Cinemachine 3.1.5, Input System and uGUI are already installed. The active intro is code driven on `S02_IntroDolly`, not a Timeline. Cameras are named shots under `CameraDirector`; reference-match timing remains available. The character is a static Rocketbox presentation mesh; Niko uses the existing procedural gesture system. The room already has baked lightmaps, light probes and four reflection probes. Post-processing and per-shot background depth of field already exist. No pipeline conversion or full scene regeneration is needed.

Baseline capture exposed compressed head-only wipes (skin → muscle → bone), abrupt gate shots, flat white sclera, and a late title. Code inspection found no dedicated intro score, cinematic Pause/Settings hidden with the topbar, stopped particles restarted by `Stage.Update`, incomplete depth-prepass clipping, recurring UI raycast allocations, and walkthrough checks that logged failures without failing the run.

Safe scope: new production opening partial, a small anatomy reveal component, additive scene authoring patch, existing audio/HUD/shader fixes and relevant tests. Preserve the curriculum, quiz checking, narrator, subtitle text, chapter assets, scene progression and user-authored scene positions. A pre-change backup is at `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-13_pre-opening`.

## Model audit and integration

Blender imported and rendered both original files before extraction. Rack 01 contains the skeleton (99,798 source triangles); Rack 02 contains 14 muscle meshes (445,804 source triangles). Both include cabinet/glass geometry, baked world offsets, UVs and embedded textures, with no armature or animation. Texture paths refer to other machines, but the embedded images are recoverable. Cabinet geometry is excluded from the animated subject.

`export_rack_anatomy.py` extracts static metre-scale FBXs, removes the muscle figure's 25-degree cabinet heading, preserves diffuse UVs and packs the muscle images into a 2048 atlas. Exported geometry is 64,868 skeleton triangles and 156,019 muscle triangles, one material each. Registration landmarks fit eye spacing, eye height, crown and feet to the current human. The authoring patch leaves the human transform unchanged. These are independently modeled bodies; they are presentation meshes, not a shared animation rig.

Source audit renders/JSON: `Logs/RackAudit`. Imported derivatives: `Assets/HumanEye/ThirdParty/UserModels/RackAnatomy`. Unity fitted meshes: `Generated/Meshes/RackSkeleton_Fitted.asset` and `RackMuscles_Fitted.asset`. Original supplied files remain untouched.

## Implemented presentation

The production opening is 32.5 seconds: near-black anticipation, gate/lab reveal, centered body approach, THE HUMAN EYE at 11–14 seconds, skin → muscles at 17–18.6 seconds, muscle hold to 20 seconds, muscles → skeleton at 20–21.6 seconds, skeleton hold to 24 seconds, orbital eye focus, continuous eye extraction to the existing display, then Niko and the existing lesson UI. The 7.5-second reduced-motion version uses still compositions of each layer. Skip, cancel, menu and restart share the existing finished-state restoration.

`AnatomyRevealController` drives complementary world-height shader wipes on skin/clothes and the supplied layers. Hidden anatomy is inactive outside its beat. No large particle bursts or new full-screen effects were added. Lighting uses the existing practical, key, fill and rim lights; the grade and sclera response are tuned for detail. The existing camera spline is reused with a continuous production path and the same gameplay handoff shot. No new Timeline or virtual-camera asset is required.

The original 32-second `intro_discovery.wav` score adds room tone, soft pad, pulse and restrained melody. Its peak is −10 dBFS and RMS −24.19 dBFS before game volume. `AudioDirector` uses a 1.2-second equal-power crossfade with the existing gameplay pad, respects volume/mute/pause, and ducks under narration. Existing quiet transition sounds are reused.

The saved UI visual language and interactions remain. Pause and Settings stay visible and reachable during cinematics; the chapter chip and lesson controls return after the opening. The title sits in the upper letterbox, clear of the face, and fades out. Cached pointer raycasts remove repeated pointer-hover allocations. Test checks now fail the walkthrough when behavior fails. Stopped VFX are no longer restarted each frame; the depth prepass obeys reveal and alpha clipping.

The scene's Main Camera had a missing `ViewportShift` script caused by a stale GUID. That reference is repaired, restoring the intended composition beside the lesson panel. Validation now detects missing scripts in active and inactive scene objects. An audit mapping incorrectly treated the apple/perception comparison (`S09_L04`) as a brain close-up; the audit now checks the actual apple and two insets.

## Files and assets

| Area | Files |
|---|---|
| New runtime code | `Scripts/Stage/Stage.Opening.cs`, `Scripts/Demos/AnatomyRevealController.cs` |
| Runtime changes | `Stage.Scene02.cs`, `Stage.cs`, `Stage.Framing.cs`, `HumanDisplay.cs`, `AudioDirector.cs`, `CameraDirector.cs`, `Hud.cs`, `AutoPilot.cs`, `AutoPilot.UI.cs` |
| Editor/shader | New `Editor/Builder.Opening.cs`; `Editor/CanvasAuthoring.cs`, `Editor/Validator.cs`; `Shaders/Anatomy.shader` |
| Scene/UI/look | `Scenes/HumanEye_Observatory.unity`, `UI/ObservatoryCanvas.prefab`, `Generated/Data/PostProfile.asset`; Rocketbox head/body, LocalEye sclera/iris and two new Rack materials |
| Anatomy | New `ThirdParty/UserModels/RackAnatomy/` exports/atlases/report and two `Generated/Meshes/Rack*_Fitted.asset` meshes |
| Audio | New `Audio/Music/intro_discovery.wav`; no new narration or curriculum text |
| Tools | New `Tools/HumanEye/audit_rack_models.py`, `export_rack_anatomy.py`, `generate_intro_music.py` |
| Tests | New `CinematicControlTests.cs`, `ActivityRecoveryTests.cs`, `IntroAudioTests.cs`; updated `CinematicTests.cs`, `WalkthroughTests.cs` |
| Documentation | This report, `CLAUDE.md`, `HANDOFF.md`, credits/notices/asset register and generated validation report |

All runtime/editor/test paths above are under `Assets/HumanEye/` unless a full relative path is shown. Unity `.meta` files accompany new assets. No Timeline or Cinemachine assets were added. Existing named cameras, progression, questions, VO/subtitles, highlights, demonstrations and activity animations are reused.

## Review passes and corrections

1. **Gameplay:** preserved the ten-chapter path; made walkthrough failures fatal; tested repeated incorrect answers, demonstrations, rapid/stale clicks and recovery.
2. **Cinematic:** extended readable holds, removed production gate cuts, moved the title away from the face, retained one continuous eye flight and added score/crossfade. The final anatomy holds frame the feet above the lower letterbox.
3. **Technical art:** inspected both source racks, removed cabinet geometry, fitted anatomical landmarks, reduced source triangle/material costs, corrected depth clipping and reduced eye key/rim lighting.
4. **Engineering:** repaired the missing camera script and optional exhibit reference; added missing-script validation; tested cancellation, repeated skip, restart, pause, settings and audio restoration.
5. **Student experience:** retained familiar controls and existing teaching interactions. Actual player review caught black letterbox pixels hiding otherwise raycast-reachable Pause/Settings; fixed sibling draw order and added visibility/order assertions. At 1366×768, Settings Close was below the scrolling content; pinned the existing Reset/Close row below a scroll viewport and constrained the card height without shrinking text or controls. The pointer test checks Close at both scroll extremes.

## Verification

- **Validator:** 72 passed, zero validator warnings/errors. This includes missing scripts and serialized references.
- **EditMode:** 24/24 passed (`final-editmode.xml`).
- **PlayMode:** all six distinct cases passed across the final runs. The full run passed five; the pointer/pause test initially sampled the camera before LateUpdate, then passed on rerun after sampling the first paused frame (`final-controls2.xml`). The exact intro-clock freeze assertion stayed in place. The corrected skin → muscles → bone order and early/late skip/navigation restoration passed again (`corrected-order.xml`), then passed with explicit foot-framing assertions (`final-framing.xml`). No test was disabled.
- **Whole-module Windows runs:** 1600×900 normal motion with stress actions and 1024×768 reduced motion both completed all ten chapters and all 65 first-run cues. The runs recorded 14 and 13 learner-evidence events, respectively; zero walkthrough failures, zero pointer-audit failures, and no audited controls below minimum size or outside the viewport. These complete runs preceded the final reveal-order correction; the subsequent targeted cinematic test covers that correction.
- **Corrected intro presentation:** visible-player checks passed at 1600×900, 1024×768 reduced motion and 1600×1000. The final wider anatomy framing has a fresh 1600×900 recording and viewport assertions. The order is skin → muscles → skeleton → eye in both motion modes; captions follow the same beats.
- **Recovery/control checks:** early and late Skip, repeated Skip, Pause/Resume, Settings during cinematics, Settings Close at both scroll extremes, return to menu, restart, navigation, corrupted saves, repeated incorrect answers, Show Me, rapid/stale clicks, and music mute/pause/cancellation.
- **Preserved placement:** `Logs/opening-transform-proof.json` records identical saved human position, rotation and scale before and after the patch. `Logs/opening-copied-assets.json` identifies the 40 verified serialized assets/metas copied back from the sandbox.
- **Performance:** the final real-time 1600×900 intro recording averaged 59.9 fps, 16.7 ms main thread and 2.85 ms GPU, with a worst 87 ms frame and approximately 555 MB peak system memory (580 average draw calls, 86 SetPass calls and 1,812 average GC bytes/frame including recording/audit overhead). The full widescreen capture/stress run averaged 54.9–60.0 fps per chapter with up to 632 MB system memory. PNG capture, readback and stress actions affect those numbers; the largest frame was 133 ms. These are desktop Development-player observations, not a controlled before/after benchmark or a tablet performance claim.

The final Windows Development build succeeded with zero errors (440.7 MB). Its final intro finished normally on `S02_NikoMedium`; all pointer checks passed. The recorded mixed audio peaked around −4.7 dBFS before the final camera-only change.

Evidence and corrected preview are collected under `Builds/Checks/centered-opening-20261013`. The runnable Windows Development player is under `Builds/HumanEye/Windows-CenteredOpening-20261013`. Test XML retains failed intermediate results alongside the successful reruns so the verification history is explicit.

## Scope limits

- Physical tablet/Android and WebGL builds were not tested. The 1024×768 check used the Windows player on this desktop; no claim is made about touch hardware or mobile frame rate.

- Rack anatomy is static, with no source rig. Eye spacing, orbital height, crown and feet are registered; the independent source bodies are not vertex-identical or a medically validated shared skeleton/skin rig.
- The existing Unity 6000.3.0 warning about light-probe occlusion counts remains. All 278 authored positions match 278 baked coefficients. Unity identifies this Baked Indirect load warning as an engine issue fixed in [6000.3.3f1](https://unity.com/releases/editor/whats-new/6000.3.3f1); this pass does not change the project's editor version or discard its bake.
- Existing supplied-asset licence and provisional TTS release gates remain documented in the asset notices. The new music is original synthesis; Rack files inherit the owner's supplied-asset status.
