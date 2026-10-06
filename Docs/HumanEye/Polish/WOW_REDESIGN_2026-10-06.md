# Presentation redesign — 6 October 2026

Branch `claude/wow-moments`. Checkpoint before this work: git tag `checkpoint/pre-wow-2026-10-06`; file backups of the scene, Canvas prefab, generated data and narration in `Logs/backup/pre-wow/` (local).

The learning sequence (ten chapters, 66 storyboard cues in order, the Scan and Explore Inside actions, the brightness and photoreceptor experiments, the adaptive checks) is unchanged. Everything below is presentation, pacing, companion presence, sound and the ending.

## What was preserved, redesigned and removed

**Preserved:** chapter runner and state machine, the 66 ledger cues (text untouched, validator exact-text check still passes), scan → inside → explore flow, the pupil brightness experiment, the near/distant lens demonstrations, the photoreceptor light experiment, adaptive questions and the learner model, Ask Niko (local curated), saves, pause/freeze rules, Unity 6000.3.0f1 / URP 17.3 / Cinemachine 3.1.5, Windows and Android targets.

**Redesigned:** the introduction (three directed shots, ~8.5 s, skippable), Niko's staging and gaze/pointing, narration pacing (clip trimming plus async camera moves), the HUD (right-hand information panel above a View / Activities panel), label behaviour (lesson-driven, fading, gliding, clear of the panels), structure isolation during the light path, the retinal-image popup, the rods/cones set and its lighting, eye materials and mesh density, the ending (journey replay → overhead overview replay → Niko's closing line), the sound plan.

**Removed:** the "Rebuild the journey" order activity (`rebuild_journey`, `QuestionMode.Order`), the Skip button on every check, the TRACE and SIMULATE buttons, the lens-shaping focus challenge (`Free("focus")`), the Continue-gated free modes `routeTrace`, `inspectImage`, `signals` and `summary`, the mid-sequence questions in Scene 7 (`img_predict`) and Scene 9 (`brain_predict`), the apple picture painted on the anatomical retina, the depth-of-field blur ending and the fade-to-black.

## Where the changes live

| Area | Files |
|---|---|
| Chapter content (steps, labels, isolation, ending) | `Assets/HumanEye/Editor/HumanEyeContent.cs` |
| Intro, scan, Niko gaze/pointing per cue | `Scripts/Stage/Stage.Scene02.cs` |
| Ending: journey replay, overview, closing line | `Scripts/Stage/Stage.Ending.cs` |
| Panel actions (Scan Eye, Explore Inside, Freeze, Replay), labels sets, isolation sound | `Scripts/Stage/Stage.cs` |
| Light path, experiments (focus challenge removed) | `Scripts/Stage/Stage.Optics.cs` |
| Rods/cones focus, retina dive, micro light sounds | `Scripts/Stage/Stage.Signals.cs` |
| Quick checks without Skip | `Scripts/Learning/ActivityController.cs` |
| Right-hand panels, Niko status, popups, info block | `Scripts/UI/Hud.cs`, `Scripts/UI/WorldLabels.cs` |
| Ducking, no stacked one-shots | `Scripts/Core/AudioDirector.cs` |
| Scene + prefab patch (idempotent) | `Editor/WowPatch.cs` (menu **Tools > Human Eye > Authoring > Apply WOW redesign**) |
| Rods/cones set and in-scene rebuild | `Editor/Builder.Micro.cs` |
| Narration regeneration with prosody and silence trimming | `Tools/HumanEye/generate_tts.ps1` |
| Lesson sound set | `Tools/HumanEye/generate_ui_sfx.py` → `Assets/HumanEye/Audio/SFX/ui_*.wav`, `fx_*.wav` |

## Introduction

Shot 1 (0–2.6 s): the closed gate from the corridor; it unlocks and lifts. Shot 2 (2.6–6.4 s): one crane through the entrance while the laboratory powers up, settling on the spotlit subject. Shot 3 (6.4–8.5 s): the establishing view holds while the music rises. Captions: "OBSERVATORY OF VISION · Access granted", "Laboratory powering up", "Your subject today: the human eye". Niko materialises on his medium shot and speaks his first two lines looking at the learner, turns and presents the human for lines 3–4, raises his hand for the scan prompt. Reduced-motion users get the 1.5 s still as before. The Skip button is unchanged.

## Pacing

- Every clip was regenerated: 60 ms lead-in, 120 ms tail, internal pauses capped at 220 ms (`ffmpeg silenceremove`), per-sentence prosody (exclamations lift, questions rise, prompts slow down, anatomy terms get moderate emphasis). Total narration 287.5 s → 240.2 s with identical words. Example: "Here it is. The human eye." 3.07 s → 2.00 s.
- Camera moves are async wherever the subject is already framed, so narration starts while the camera settles; the explicit `Hold` steps after part explanations and the 0.5 s wait before returning from a part are gone.
- The next line follows actual clip completion (unchanged mechanism); no arbitrary timers were added.

## Right-hand interface

Upper **information panel**: Niko's live portrait (a second camera on his head rendering to `Generated/UI/NikoPortrait.renderTexture`), chapter title with Home, Niko status line, **Talk to Niko** (always in the same place), the current instruction (prompt banner), the short information block (`Do("info", …)`, scan feedback, closing line) and the captions with the Read & Continue fallback.

Lower **View / Activities panel**: Scan Eye / Explore Inside / Freeze / Replay demo (only when meaningful), the experiment controls, the Parts list, the view controls, the retinal-image popup, the journey stages. The quick-check card, the end card and the Ask Niko sheet overlay the lower half of the column. The top bar keeps progress dots and Pause/Help/Review/Settings/Replay explanation and stops where the column begins; the world-label layer excludes the column.

No UI objects are created at runtime; the patch adds the new saved objects (`right-column`, `info-panel`, `info-header`, `info-titles`, `niko-portrait`, `niko-status`, `info-text`, `activity-panel`, `activity-header`) to the Canvas prefab and binds them on the scene HUD.

## Niko

- Looks at the learner for greetings and prompts, at the structure he explains (per-cue map in `Stage.Scene02.cs`), and back at the learner after every line.
- Performs the scan: turns to the subject, points the scanner, scanner tone plus sweep, panel reports "Scanning…" then "Scan complete · sclera, cornea, iris and pupil identified."
- Moves to the Brain anchor for Scene 4, the Wide anchor for the overview, the new **Closing** anchor for the last line, and stays available through the portrait during every close-up and the microscopic retina.
- Status line is honest: speaking / thinking / answering / waiting / "Type a question for Niko (no microphone on this build)". There is no microphone or remote AI in this project; Ask Niko remains the local curated text system with suggestions.

## Labels and isolation

Label sets are lesson-driven (`labels` action): `focus:<id>` while a structure is explained, `scan` for every visible structure, `journey` during the overview, `""` to clear. The optic nerve is labelled in Scene 4 (its reveal), Scene 9 (signals) and the overview. Labels fade in over 0.35 s, glide when the view changes, and are clamped inside the 3D area. During the light path the explained structure is isolated (`isolate` / `restore`) with a soft settle sound; the pupil isolation keeps the iris and interior solid so the opening still reads as an opening.

## Retinal image

The quad on the retina is deleted (`LightRays.imageQuad = null`); the popup in the lower panel shows the object camera's view rotated 180° ("Image on the retina: real and inverted") when `S07_L02` plays, fades out after it, and returns beside the upright perception view in Scene 9. Replay demo in Scene 7 re-forms the image and reopens the popup.

## Rods and cones

Rebuilt by `Builder.RebuildMicro`: two tidy rows of ten cells, a cone every third cell; rods are tall slender capsules (lavender), cones short broad tapers (orange), each with inner segment, nucleus and axon; six bipolar and four ganglion cells feed the fibre layer and nerve. Three point lights (key, fill, rim) and a light backdrop replace the dark cavern. Rods are introduced in dim light (0.3) with the cones faded back, cones in bright light (0.95) with the rods faded back, then both in medium light as the signals start. Shots re-aimed on the new layout.

## Eye

Procedural model kept (the lesson needs its live pupil, accommodating lens and clean cutaway): layer meshes regenerated at 72 segments (was 40), materials refined (warmer sclera with subtle rim, glassier cornea, less plastic iris, clearer lens, stronger retina/choroid vessel detail). The MisterH exterior remains the closed-eye surface. No licensed replacement model with separable, animatable layers was available; see limitations.

## Ending

Scene 10: "So seeing is a complete process" → journey replay on `S10_Journey` (chips in the panel, each stage glowing in turn) → pull back to `S10_Overview` (overhead teaching layout: apple, cutaway eye, light path, optic nerve, brain) → the journey replays once more from above with concise labels. Scene 11: the eye closes, the three curiosity lines play over the complete eye and the near/far objects (both sharp), then Niko moves beside the eye on `S11_Closing`, waves, and says "Now you've seen the whole journey. Every part has a role, and together they help you see the world." (supplemental line, text in the panel plus clip `Audio/SFX/niko_closing.wav` on the narration channel), followed by the completion chord and the end card in the right column (Niko stays in frame).

## Sound plan

| Moment | Key | Source |
|---|---|---|
| Intro gate / lights / strike | drone, clunk1-3, riser, boom, shimmer | existing |
| Niko materialises, apple appears | materialise | existing |
| Chapter change | chapter | new |
| Niko's scan | nikoscan + scan hum | new + existing |
| Structure identified / discovery | ping, discovery | existing |
| Label appears | label | new |
| Structure isolated / cell type focused | isolate | new |
| Cutaway, brain reveal, human reveal | reveal, whoosh | existing |
| Light travels | lightmove | new |
| Into the retina | dive | new |
| Light reaches cells, light level change | cell | new |
| Signals start / pulse | pulse | existing (now used) |
| Popup / card open and close | popup, popdown | new |
| Check feedback | confirm, deny | existing |
| Overview pull-back | overview | new |
| Closing | complete | new |

Music ducks to 40% and effects to 75% while Niko speaks (including the closing clip); a one-shot key cannot retrigger within 120 ms, so repeated clicks never stack.

## Review passes and evidence

All runs were made in a verification copy of the project (`C:\he-sandbox\AI-HUMAN-EYE`, same sources, Unity 6000.3.0f1, RTX 5060 Ti, D3D11) because the owner's Editor held unsaved scene changes and must not be closed by batch commands. Logs: `C:\he-sandbox\logs\` (local, not committed). The saved scene, Canvas prefab, generated assets, audio metadata and the `HE_Niko` layer were copied back into this project after the final run.

| Pass | What was checked | Result |
|---|---|---|
| 1 — flow and functionality | Validator (ledger exact text, 66 cues placed once in order, awaits, free modes, offers resolve, whitelisted actions, shots exist, no missing references, build settings) | **70 passed · 0 warnings · 0 errors** |
| 1 | Edit Mode suite (ledger, learner model, saves, Ask Niko intents, geometry/optics, routines, validator) | **24 / 24 passed** |
| 1 | Play Mode `WalkthroughTests` (Scene 2 → 11 through the public HUD actions at 6×, every control reached by an EventSystem raycast, same saved Canvas objects before/after, timeScale untouched) | **passed, 112 s** (was 165 s before the pacing work) — 10 chapters, 66 cues, adaptive evidence recorded |
| 3 | Play Mode `CinematicTests` (supplied eye exterior, menu gate state, intro skip at 0.1 / 3 / 7 s with lights and gate restored, lab walking collision, duplicate Scan press, scan close-up centred on the live socket) | **passed, 13 s** |
| 4 | Windows development build (`PlayerBuilds.Windows`) | **Succeeded, 480 MB, 0 errors, 1 warning** (the usual cloud symbol-upload notice) |
| 2 + 4 | Standalone walkthrough 1600×900, stress flags (`-heStress -hePolish`): Help/Settings/Review/Ask/Pause modals, unsupported Ask Niko question, rapid Replay requests, pause during a demonstration, freeze mid-change, return to menu, save/reload, overwrite confirmation, resume, completed-save resume, pointer raycast audit on every capture | **35 PASS / 0 FAIL**, 10 chapters, 66 cues, 14 adaptive answers, 90 distinct buttons reached by raycast, 0 controls outside the window, intro skip exercised, pause holds narration and camera, freeze holds the pupil; 56–60 fps per chapter (VSync), peak system memory 522 MB |
| 2 + 4 | Standalone walkthrough 1024×768, reduced motion (`-heReduced -hePolish`), pointer and layout audit on every capture | **15 PASS / 0 FAIL**, 10 chapters, 66 cues, 13 adaptive answers, 61 distinct buttons reached, 0 controls outside the window; 56–58 fps per chapter, peak 497 MB |
| 4 | Runtime exceptions in both player logs | **0** |
| voice | Automated intelligibility (Windows dictation recall over the 66 regenerated clips; a proxy, not a listening test) | **0.89 average recall** (0.81 before) |

Iterations: four fix-and-retest rounds were needed. Round 1 found the top-bar buttons overflowing under the new column at 1024 px and a stretched "Sound on" button (layout flex heights); round 2 found the lens shift sign reversed (caught by the scan close-up assertion) and world labels colliding in the narrower 3D area; round 3 added label relaxation passes and 48 px controls; round 4/5 corrected the offscreen capture tool, which drew the camera-space UI through the shifted projection, and a QA race where the intro ended between the skip check and its click.

Known informational audit notes: the lab-tools grid (Explore lab, Replay intro, Sound, Cross-section, Show/Hide sclera) uses 44 px cells and the Parts list 46 px rows, below the audit's 47.5 px flag but above the 44 px touch minimum; they fit the column at 768 px height with the five-part Scene 3 list.

### Captures

Representative frames from the final 1024×768 and 1600×900 walkthroughs are in `Captures/wow-2026-10-06/` (see its INDEX.md): entrance, Niko's introduction, scan, eye hero with the column, isolated sclera with labels, quick check in the panel, cornea on the light path, retinal popup, rods then cones, brightness experiment, overhead overview, closing with Niko, end card.

## Limitations (honest)

- Voice is still provisional local TTS (Microsoft Mark, OneCore). Prosody, pacing and silence are improved and measured; it is not a recorded performance. A Zira audition set exists locally. Online neural voices were available as Python packages but were not used (no licence for the service).
- No veterinarian exists in this module; the human figure is a silent scan subject and Niko is the only speaker. Nothing was invented.
- Talk to Niko is the local curated text system; no microphone, speech or remote model is wired (none is configured in the project), and the status line says so.
- The eye remains the procedural teaching model with better surfaces and density; no licensed replacement with separately animatable layers was available, and the lesson depends on the live pupil, lens and cutaway.
- At 4:3 the overhead overview keeps the apple, eye and nerve in the 3D area; part of the brain can sit behind the panel. At 16:9 everything is in view.
- Figma was not used (no Figma connector in this session); the UI was designed and built directly in the saved Canvas prefab via the patch.
- Physical tablet, touch hardware, listening review and the release licence gates are unchanged from the previous handoff.
