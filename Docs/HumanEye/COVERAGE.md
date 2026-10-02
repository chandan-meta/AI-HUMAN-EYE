# Source-to-implementation coverage checklist

Legend:
- **✔ implemented**: built, and exercised by the automated walkthrough/tests.
- **◐ implemented, needs human/device review**: built but not yet reviewed by a person or on a device.
- **✖ not done**: listed in MISSING.md.

Every storyboard line (66 cues) is placed exactly once in its own chapter, in ledger order. This is checked by `Validator` and `LedgerTests`. Cue-by-cue camera and visual detail is in `CUE_SHEET.md`.

## Global

| Requirement | Implementation | Status |
|---|---|---|
| Start directly at Scene 2 in the Observatory of Vision (no Scene 1) | `ChapterRunner.Begin(0)` → `Scene02`; there is no Scene 1 content | ✔ |
| Ten inspectable chapter definitions, IDs Scene02–Scene11 | `Assets/HumanEye/Generated/Data/Chapters/*.asset` | ✔ |
| 66 exact cues, per-scene counts, 3 PROMPT VO, repeats kept with own IDs | `DialogueLedger.json` (extracted from the docx); `Validator`; `LedgerTests` | ✔ |
| Exact text on screen (subtitles/instructions); stage directions never spoken | `Narrator.SubtitleText`, prompt banner uses cue text; ledger holds spoken text only | ✔ |
| Timings follow the real clip length and action completion | `Narrator.Play`, `CameraDirector.MoveTo` (waits for blend end), Await/Explore steps | ✔ |
| StoryboardCore vs SupplementalAdaptive separation | Ledger vs `QuestionBank` / `NikoKnowledge` (items tagged) | ✔ |
| Mouse and touch parity; no hover-only info; no mandatory multitouch | `PointerSelector` (tap/drag), buttons everywhere, pinch optional | ◐ (touch not tested on a device) |
| UI consumes its pointer first; one tap → one system | `Hud.IsOverUI` checked at press time for the whole gesture | ✔ (logic) ◐ (device) |
| Enlarged targets + labelled part list | Label hotspots (`WorldLabels`), Parts list, enlarged pupil/retina/choroid pick zones | ✔ |
| Responsive 16:9 / 16:10 / 4:3, safe area, portrait fallback | UI Toolkit scale-with-screen, `.portrait` layout, safe-area padding | ✔ (all four captured in the Windows player) ◐ (device safe areas) |
| Pause/Resume, Replay explanation, Reset view, Help, audio controls, progress | Top bar, View panel, Settings, progress dots | ✔ |
| Unavailable actions explain why | `Stage.Ability/Control` reasons → toast | ✔ |
| Reduced motion / skip camera travel keep all dialogue and interactions | `CameraDirector.shortMoves` → fade cut; same steps run | ✔ |
| One camera controller, one AudioListener | `CameraDirector` + single `CinemachineBrain`; validator checks one AudioListener | ✔ |
| States Entering/Cinematic/Narrating/AwaitingAction/Exploring/SupplementalCheck/Transitioning/Complete + valid transitions | `LessonState.cs` | ✔ |
| Exit cancels routines/tweens/audio/callbacks; replays idempotent | `ChapterRunner.Cancel`, `Routine.Cancel`, `NikoGuide.Invalidate`, `SaveSystem.AddOnce` | ✔ |
| Pause preserves position; FREEZE freezes only the biology | `Clock.Paused` / `Clock.Frozen`, `UniformDeltaTimeOverride`; timeScale untouched (asserted in the PlayMode test) | ✔ |
| Reset to documented checkpoint | `Stage.Enter` restores materials, pupil, lens, objects, cameras, UI ("Restart this scene") | ✔ |
| Missing audio never softlocks | Read & Continue, recorded as "read" (not audio) | ✔ (logic) |
| Anonymous local saves, corrupt-save recovery, reset | `SaveSystem` + `SaveTests` | ✔ |
| Web Start interaction initialises audio | Start screen button | ✔ (no Web build) |

## Scene 2 — Meet Niko

| Storyboard | Implementation | Status |
|---|---|---|
| Observatory: starlight beam, central human figure, reflections, beams, apertures, lenses | Dome + stars, giant beam through an aperture ring and lens, dust, optical benches, HDRI reflections, display plinth | ✔ ◐ (art review) |
| Subdued display, human appears after S02_L03 | `EnterScene02` (silhouette 6%), `humanReveal` | ✔ |
| Niko appears; S02_L01–L03; S02_L04 | `nikoAppear` (wave), look at human | ✔ |
| He raises his hand; PROMPT S02_L05; wait for SCAN | `RaiseHand`, prompt banner, `Await scan` (SCAN pulses) | ✔ |
| Blue scan: skin translucent → muscles → skull → brain; both eyes remain | `HumanDisplay.Scan` (Z-Anatomy skull/brain registered to the eye sockets; shorts stay opaque) | ✔ ◐ |
| "There." with one eye illuminating; "Your first destination." with the eye in the skull | `illuminate` at the line's start; `S02_EyeInSkull` close-up + inspection hold | ✔ |
| Pinch; human freezes and disappears; eye enlarges and slowly rotates | `pinch`, `Dissolve`, `enlarge`, gentle sway | ✔ |
| S02_L08, gesture, S02_L09, snap + successive highlights, S02_L10 | `Present`, `Snap`, `sequence` | ✔ |
| Sclera/cornea/iris/pupil: close-up, isolate, explanation, return | Explore entries + `FocusPart`/`Isolate`/`ReturnFromPart`; any order; Ask Niko works too | ✔ |
| Completion only after the explanation finishes | `ChapterRunner.Explore` marks after the steps | ✔ |
| Supplemental function-to-part check (skippable) | `Offer topic:external_structures` | ✔ |
| S02_L15; PROMPT S02_L16; **Inside View** enabled only then | Ability label "Inside View"; `Await inside` | ✔ |

## Scene 3 — Inside the Eye

| Storyboard | Implementation | Status |
|---|---|---|
| Match orientation; cornea transparent; clean cutaway | `corneaClear`, `turnToSide`, `cutaway` (removable half slides away; section caps) | ✔ |
| Successive subtle highlights; S03_L01 | `sequence` | ✔ |
| Aqueous → lens → vitreous → retina → choroid (guided order, free choice) | Explore entries; multi-line explanations kept together | ✔ |
| Lens close-up, biconvex form, slight shape change, view widens | `S03_Lens_Close`, `lensFlex`, back to `S03_Part_lens` | ✔ |
| Through the vitreous toward the back | `S03_Vitreous_Back` | ✔ |
| Retina detail; move slightly deeper to the layer behind | `retinaDetail`, `S03_Choroid_Deep` | ✔ |
| Choroid via a connecting shot from the retina | Choroid focus starts at the retina close-up and then moves deeper | ✔ |
| Gate on all five; supplemental job-to-structure match with visual hints | Explore gate; `Offer topic:internal_structures` (hint tiers highlight structures) | ✔ |
| End on the complete eye with S03_L12 | `S03_InsideHero` | ✔ |

## Scene 4 — Eye to brain

| Storyboard | Implementation | Status |
|---|---|---|
| Move around the rear during L01–L02; the bundle appears; highlight; "Let's follow it." | `S04_EyeRear(Close)`, `nerveReveal`, glow | ✔ |
| Optic nerve close-up; follow it to a realistic brain | `S04_NerveClose` → `S04_NerveMid`, `brainReveal` (Z-Anatomy brain) | ✔ ◐ |
| Visual processing area at the back highlighted | Occipital lobe as its own part, cyan highlight, label "Visual processing area" | ✔ |
| Wide view of eye, nerve and brain; S04_L08 | `S04_Wide` | ✔ |
| TRACE + slow REPLAY afterwards; optional tap-to-trace | `Free routeTrace`; `Offer item:nerve_tap` | ✔ |

## Scene 5 — Light enters

| Storyboard | Implementation | Status |
|---|---|---|
| Recognisable object, lit (not self-luminous); light travels to the eye | Poly Haven apple on a pedestal, lamp + illumination rays, then reflected rays | ✔ |
| Cornea bends (L03), aqueous + pupil (L04) synchronised | Staged `lightTravel` reach + highlights | ✔ |
| PROMPT L05; increase first → pupil shrinks → L06; decrease → pupil grows → L07; L08 | `Await brighter/dimmer` with thresholds, `pupilSettle`, opposite direction refused with reason | ✔ |
| Bright/Dim presets, −/+, slider; no narration while dragging | SIMULATE panel; narration only fires once per Await | ✔ |
| Free experimentation; prediction before a further trial (supplemental) | `Offer item:pupil_predict` (trial after answer), `Free brightness` | ✔ |
| Exposure doesn't cancel the change | No auto-exposure in URP; light intensity and ambient change | ✔ |

## Scene 6 — Lens focuses

| Storyboard | Implementation | Status |
|---|---|---|
| Pupil → lens close-up; rays bend | `S06_LensClose`, rays through the lens | ✔ |
| Near object, more curved lens (L02); distant object, less curved (L03); accommodation (L04); pull back through the vitreous | Near/Distant controls in order; `nearDemo`/`distantDemo`; lens scale = accommodation = ray focus | ✔ |
| Distance slider afterwards; FREEZE/REPLAY expose the shape change; "illustrative model" label | `Free distance`; FREEZE checked in the walkthrough log | ✔ |

## Scene 7 — Image on the retina

| Storyboard | Implementation | Status |
|---|---|---|
| Through the vitreous, rays converge, real inverted image (asymmetric apple) | `imageForm` (object-camera render, rotated 180°) | ✔ |
| Comparison view; inspect/freeze/replay; no mandatory quiz before L03 | Inset "Image on the retina: real, inverted"; `Free inspectImage` (Continue only) | ✔ |
| Image never shown travelling along the nerve | Only signals travel | ✔ |

## Scene 8 — Photoreceptors

| Storyboard | Implementation | Status |
|---|---|---|
| Magnified retina: individual rods and cones | Procedural micro-set (rods, cones, bipolar, ganglion, fibre layer, optic nerve) | ✔ |
| Rods highlighted for L02–L03; cones for L04 | Glow + close shots | ✔ |
| Light reaches cells; electrical response; signals through the retina toward the nerve; pull back | Warm streaks vs cyan zig-zag pulses with labels; `S08_PullBack` | ✔ |
| Tap rods/cones to inspect/replay; dim/bright presets; optional choice activity | `Free photoreceptors`; `Offer topic:rods_cones` | ✔ |

## Scene 9 — Signals to the brain

| Storyboard | Implementation | Status |
|---|---|---|
| Signals leave through the nerve (L01), travel to the brain (L02), visual area highlighted (L03) | Pulses on the nerve path; follow camera; glow | ✔ |
| Original object clearly again; L04 in full; perception vs retinal image as different views; no photo-flipping | `perception`: real upright apple + two labelled insets | ✔ |
| Continuity with Scene 4; slow TRACE/REPLAY | `S09_Wide` = `S04_Wide` pose; `Free signals` | ✔ |

## Scene 10 — Complete working

| Storyboard | Implementation | Status |
|---|---|---|
| Complete eye; L01, L02 | `S10_Complete` | ✔ |
| Non-narrated visual replay of the pathway | `journey` with stage strip (object → cornea → aqueous → pupil → lens → vitreous → retina → optic nerve → brain) | ✔ |
| Skippable "Rebuild the journey" by tap placement; sclera/choroid acknowledged but not on the path; targeted help | `Offer item:rebuild_journey` (Order mode, distractor feedback) | ✔ (drag-and-drop alternative not built) |
| Assessment/summary kept here | Inverted-image check + Review panel | ✔ |

## Scene 11 — Final curiosity

| Storyboard | Implementation | Status |
|---|---|---|
| L01, L02; distant object slightly blurred, near clear; L03 exactly | `blurDemo` (Gaussian DoF) | ✔ ◐ |
| Blurred object fades; eye fades into darkness; cut to black | `fadeBlurred`, `eyeFade`, `cutToBlack`, quiet beat | ✔ |
| No defect lesson / compulsory question; non-narrated menu: Replay, Review, Exit | End menu | ✔ |

## Adaptive learning and Niko

| Requirement | Implementation | Status |
|---|---|---|
| Deterministic, offline, inspectable rules (2 distinct unassisted correct → secure; scaffold → comparison + worked demo → assisted retry) | `LearnerModel`, `ActivityController`, `LearnerModelTests` | ✔ |
| Exposure separate from mastery; help never counts | `cues` vs `evidence` lists | ✔ |
| ≥2 distinct items per topic, three levels, misconception tags, feedback, hint tiers, demos | `HumanEyeContent.Questions()` (27 items) | ✔ |
| No lives/timers/locks; continue with "needs practice" recorded | "Continue anyway", `needsPractice` | ✔ |
| Discovery stamps; review panel with status + replay links | `Discovered`, Review panel | ✔ |
| SCAN, INSIDE, SIMULATE, TRACE, FREEZE, REPLAY (context-aware) | `Stage.Ability/UseAbility` | ✔ |
| Ask Niko: typed questions, suggestions, submit button, local curated mode labelled as not AI | Ask panel, `NikoGuide` | ✔ |
| Requests → whitelisted actions; camera to unlocked structure; validated; no spoilers; no completion from free text | `CanGuideAction`, `Taught()` quotes only heard lines, `notYet` | ✔ |
| Pause at a safe boundary, resume from the checkpoint; stale answers dropped | `AskHold`, request token + chapter check | ✔ |
| Optional push-to-talk / remote AI provider | Not built (no permitted speech/AI service configured) | ✖ (see MISSING.md) |
