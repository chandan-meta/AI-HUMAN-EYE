# Visual quality pass — 14 October 2026

Brief: the owner's `Human_Eye_Visual_Reference_and_Implementation.pdf` (seven pages: current screenshots next to a
concept target; intro camera and environment; muscle and layer transitions; brain and optic-nerve effect; photoreceptors;
target art direction; acceptance criteria) and the master prompt (camera → lighting → anatomy layers → brain → pulse →
photoreceptors → transition VFX → polish, with a mandatory dry-run/critique loop). Branch `claude/wow-moments`.
Evidence: `Docs/HumanEye/Polish/Captures/quality-2026-10-14/` (boards and sheets) and `Builds/Checks/quality-20261014/`
(recording, logs, test results). Pre-change backup: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-quality`.

## Audit on arrival

- Scene `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`; Unity 6000.3.0f1, URP 17.3 (PC renderer Forward+, Mobile
  Forward), Cinemachine 3.1.5, Input System, uGUI Canvas prefab. Target: desktop/tablet web (the WebGL module is not
  installed; Windows Development players stand in). Budget: 60 fps desktop.
- Controllers found (not guessed): `Stage` (+ partials `Stage.Opening.cs` for the production opening on the code-driven
  `S02_IntroDolly`, `Stage.EyeBrain.cs` for the Scene 9 cinematic on `S09_Follow`, `Stage.Signals.cs`), `ChapterRunner`,
  `CameraDirector` (named Cinemachine shots), `AnatomyRevealController` (Rack 02/01/04 layer wipes), `HumanDisplay`,
  `SignalPulses` (comet pulses), `PhotoreceptorField` (Scene 8), `Hud`. No Timeline drives the intro.
- The screenshots in the PDF are the 13 October organ-portrait opening: the subject framed head-and-shoulders at 1.45 m
  with a fade at the shoulders (a slab under the muscles), a skull with the eyes and a ghosted skeleton behind the organ
  layer, floating teeth from Rack 04's mouth, a near-black corridor hold, a pale translucent brain in Scene 9 and a row of
  plain capsules in Scene 8.
- Found while re-creating the Scene 9 complaint: the `nerve` pulse route is stored in **world space** on `SignalPulses`
  and was never moved when the visual-match pass of 8 October moved the lesson rig onto the stand. Since then the
  Scene 4 and Scene 9 pulses (and the head light that rides them) have travelled 1.45 m below the display, invisibly;
  what the owner saw as "static bright blobs" on 8 October was the head light and bloom over-exposing the nerve tube.
  The PlayMode test only asserted that pulses were alive, not where.

## What changed

**P0 camera and intro** (`Stage.Opening.cs`). The corridor fade-in is 0.2–1.8 s instead of a 1.8 s black hold, and the
corridor is lifted +0.45 EV while the gate opens. The dolly now stops at the threshold for a wide establishing shot (the
stand, the host, both infographic displays), arcs gently toward the stand, holds the full body under THE HUMAN EYE at
11–14 s and pushes to a medium bust (2.05 m, 35°) for the layer reveals; the eye close-up and extraction are unchanged.
Three-point portrait light: `faceKey` as key (front-left high), `eyeFill` as a soft fill (front-right low, restored with
the other moved lights), `eyeRim` behind-right at 1.1× so the figure separates from the windows. Shot distances, lenses
and both exposure lifts are Inspector fields on `Stage` (Opening shots). Durations and the 32.5 s length are unchanged,
so the caption and VO beats still line up.

**P0 anatomy layers** (`AnatomyRevealController`, `Builder.Quality.cs`, `export_rack_organs.py`). The soft fade moves
from the shoulders (`neckY − 0.06`) to the navel (`neckY − 0.42`, 25 cm wide): the chest, deltoids and clavicles read
whole and the dissolve sits at the frame's lower edge. Bone leaves completely before the organs (no ghosted skeleton
behind them). Rack 04 is re-extracted without its mouth and the 14 teeth (90,754 triangles instead of 107,507; the
supplied FBX is untouched) and the organ view adds the Z-Anatomy optic pathway (nerves, chiasm, tracts) between the
eyes and the brain. Tissue materials: warm matte muscle with ivory tendons (`_Specular .11`, `_Gloss .3`), matte ivory
bone, moist organs; no fluorescent coral, no plastic sheen. The brain and pathway clear within 0.8 s of the eye close-up
so that shot is about the eye.

**P0 transition VFX** (`Stage.ScanFront`, `Builder.Quality.OpeningRing/OpeningMotes`). A thin cyan energy ring
(20 mm band, additive, scaled to the body's silhouette at the front height: crown → neck → shoulders → chest → waist)
and cyan sparkle motes with a quarter of gold ride every wipe front; the existing warm burst stays as the gold accent.
Both are saved, pooled systems, emitted only while a wipe runs, cleared by Skip/menu/restart with the other intro
particles (`RestoreIntroState`), paused with the cinematic, and absent under reduced motion.

**P0 brain** (`Builder.Quality`, `Stage.EyeBrain.cs`, `Stage.Signals.cs`). The owner's Brain.fbx display and the
Z-Anatomy head brain are kept (both inspected; no download). Materials: deeper coral, less wrap (shaded sulci), moderate
wet sheen, lower rim; the lesson ghost is 50 % instead of 30 %; in the eye-to-brain cinematic the head brain is 35 % only
during the pathway beat (over a 5 % bone ghost; the skull's glass mode is off because its full-strength highlights read
as a milky shell) and the floating brain firms to 96 % — pulses are drawn with the depth test off, so they still show
entering and reaching the cortex. The grade returns to contrast 12 / saturation 8, bloom 0.22 at threshold 1.0.

**P0 eye-to-brain pulse** (`SignalPulses`, `Glow.shader`, `Builder.Quality`). Electric-blue comets as the brief asks:
white-cyan head, blue trail, intensity 2.2 (HDR head, no white chunks), head width 0.07, trail 0.6; `Glow.shader` gains
a per-material `_ZTest` and the signal material uses Always, so the comet rides inside the opaque optic-nerve tube and
through the solid brain. The `nerve` route is re-anchored to the eye's cut frame (`EyeGeometry.NerveStub[0]`) by the
patch, idempotently (moved by (0, 1.45, −0.60) once). Pathway nodes (nerve, chiasm, tract, LGN, radiation) highlight gold
as the pulse passes; the visual cortex keeps its blue glow; the head light is cyan-white. The light beams stay warm-white:
light and signals never share a colour or a path. The lesson's own text already describes the retina → ganglion-cell
simplification; no narration changed.

**P1 photoreceptors** (`Builder.Micro.cs`, `RebuildMicro`). Rods: disc-stacked (ridged) outer segment, fatter inner
segment, nucleus, axon, spherule terminal; cones: ridged outer segment narrowing toward the pigment layer, inner
segment, nucleus, axon, pedicle with its flat face toward the bipolar dendrites, in the three cone classes' colours.
Labels on one rod (outer segment (discs), inner segment, nucleus, synaptic terminal) and the pigment epithelium join the
existing light/signal/optic-nerve labels. Cooler backdrop, calmer key, matte cell materials (no blown inner segments).
Light still arrives from the vitreous side and only signals leave toward the optic nerve.

**Reduced-motion handshake** (`Stage.EyeBrain.cs`). The cinematic's "end" beat is now released by `EyeBrainEnd` only:
before, the next line and the lesson interface could start while the last still was held, and the top-bar controls
audited as unreachable (seven failures in the 1024×768 reduced-motion walkthrough of round 3).

Not changed: curriculum, 65 spoken cues, questions, Niko, HUD layout, pause/skip/save, named shots other than the
code-driven ones, the owner's environment and transforms.

## Files

| Area | Files (under `Assets/HumanEye/` unless shown) |
|---|---|
| Runtime | `Scripts/Stage/Stage.Opening.cs` (rewritten path, lights, exposure, `ScanFront`), `Stage.Scene02.cs` (fill light restore, VFX cleanup), `Stage.EyeBrain.cs`, `Stage.Signals.cs`, `Scripts/Demos/AnatomyRevealController.cs`, `Scripts/Demos/SignalPulses.cs` |
| Shaders | `Shaders/Glow.shader` (`_ZTest`) |
| Editor | new `Editor/Builder.Quality.cs` (`ApplyQuality`), `Editor/Builder.Micro.cs` (cells, terminals, labels, lights), `Editor/Builder.VisualMatch.cs` (signal palette) |
| Assets | `ThirdParty/UserModels/RackAnatomy/Organs.fbx` + atlas/report (re-extracted), `Generated/Meshes/RackOrgans_Fitted`, `RodOuter`, `ConeOuter`, `RodSpherule`, `ConePedicle`, `AnatomyScanRing`, `Generated/Materials/Rack_*`, `Brain*`, `Signal`, `AnatomyScanRing`, `IntroAnatomyMotes`, `Rod_*`, `Cone_Outer_*`, `Micro*`, `Generated/Data/PostProfile.asset`, the saved scene |
| Tools | `Tools/HumanEye/export_rack_organs.py` (teeth excluded; UV-layer removal by name) |
| Tests | `Tests/PlayMode/CinematicTests.cs` (scan ring and motes: alive during the first wipe, gone after it and after Skip) |
| Docs | this report, `CLAUDE.md`, `HANDOFF.md`, `THIRD_PARTY_ASSET_CREDITS.md`, `Captures/INDEX.md` |

Apply order on a scene that already has the centered opening: **Tools > Human Eye > Authoring > Apply visual quality
pass (keeps scene edits)**. Sandbox: `C:\he-sandbox\round22.ps1` (steps `sync,quality,validate,editmode,playmode,build,
qintro,qintro1024,ps08,qs09,qwalk,qwalk1024,record`), copy-back `C:\he-sandbox\copy_back22.ps1`.

## Dry-run loop (played in the sandbox player, 1600×900 and 1024×768; scores 1–10)

Columns: camera/framing · lighting/contrast · material/anatomy · VFX legibility · pacing · educational clarity · UI readability.

| Sequence | Baseline (13 Oct build) | Round 1 | Round 2 | Round 3 | Final (round 4) |
|---|---|---|---|---|---|
| Opening (corridor → bust reveals → eye) | 5·5·4·3·6·6·7 | 7·7·6·5·7·7·7 | 8·7·7·7·8·8·7 | 8·8·7·7·8·8·7 | 8·8·7·7·8·8·7 |
| Eye-to-brain cinematic (Scene 9) | 6·6·4·2·7·4·7 | 6·6·5·2·7·5·7 | 7·6·6·5·7·6·7 | 7·6·7·7·7·7·7 | 7·6·7·7·7·7·7 |
| Magnified retina (Scene 8) | 6·5·3·5·7·5·7 | 6·5·7·6·7·7·7 | 7·7·8·7·7·8·7 | 7·7·8·7·7·8·7 | 7·7·8·7·7·8·7 |

Three biggest problems found each round, and the action taken:

1. **Round 1** — (a) no pulse visible anywhere in Scene 9: the world-space `nerve` route had been left 1.45 m below the
   display since 8 October → re-anchored by the patch, pulses drawn with the depth test off; (b) a 1.8 s black hold before
   the corridor, a hula-hoop ring at the waist and invisible motes → short fade-in, silhouette table down to the waist,
   brighter ring, larger motes; (c) blown inner segments in Scene 8 and a milky x-ray head → calmer key and matte cells.
2. **Round 2** — (a) comets as fat white chunks (width 0.16 m, intensity 5) → 0.07 m, intensity 2.2, narrower beat scale;
   (b) the x-ray head still milky → skull glass rim halved; (c) the floating brain at 85 % still showed its inner pathway →
   96 % with depth-test-off pulses carrying the "signal inside" reading.
3. **Round 3** — (a) the pathway beat's head still read as a white shell: the skull's clear-medium mode multiplies its
   highlights by up to 4× → glass mode off, 5 % bone ghost; (b) the comet chain inside the solid brain too wide → beat
   width 2.6× → 1.6×; (c) seven pointer-audit failures in the reduced-motion walkthrough at `S09_Perception` → the "end"
   beat handshake fixed (above).

Round 4 re-ran everything (tests, build, captures, both walkthroughs, the real-time recording) on the final code.

## Verification (sandbox copy `C:\he-sandbox\AI-HUMAN-EYE`, same sources, final round 4)

| Check | Result |
|---|---|
| Compile | 0 errors (every round) |
| `Builder.ApplyQuality` | organ refit 90,754 triangles; `nerve` route re-anchored by (0, 1.45, −0.60); subject transform unchanged (0, 1.79, −0.05) |
| `Validator.RunBatch` | 72 passed / 0 warnings / 0 errors |
| EditMode | 24 / 24 |
| PlayMode | 6 / 6 (opening checkpoints incl. scan ring and motes alive during the first wipe and off after it and after Skip, pause/settings/repeated skip, activity recovery, intro audio, the eye-to-brain cinematic with live pulses, the full walkthrough) |
| Windows Development build | succeeded, 447 MB (`Builds/HumanEye/Windows-Quality-20261014/`) |
| Player opening captures | 1600×900 every 0.5 s (65 frames) and 1024×768 every 1 s (35 frames); 0 exceptions, 0 layout failures |
| Player chapter captures | Scene 8 (19 frames) and Scene 9 with the cinematic every 0.7 s (46 frames); 0 exceptions |
| Walkthrough 1600×900, stress actions | 35 PASS / 0 FAIL; 10 chapters, 65 cues, 14 learner-evidence events; 0 exceptions; 132 s accelerated |
| Walkthrough 1024×768, reduced motion | 15 PASS / 0 FAIL; 10 chapters, 65 cues, 13 evidence events; 0 exceptions; 83 s accelerated (round 3 had 7 pointer-audit failures at `S09_Perception`; fixed by the end-beat handshake, re-run clean) |
| Real-time recorded run, 1280×720 with audio | 444.6 s, 10 chapters, 65 cues, 0 exceptions (`Builds/Checks/quality-20261014/walkthrough_full.mp4`) |

Performance (Development player, RTX 5060 Ti, capture and audit overhead included; not a tablet or WebGL number):
stress run 54.8–60.0 fps per chapter (worst single frames 71–133 ms, chapter-entry hitches as before; draw calls
average ≤ 10 with peaks to 718 while a chapter loads), reduced-motion run 55.0–58.8 fps (worst frames 51–85 ms). The
new VFX add two small pooled particle systems (≤ 400 motes) and one 72-segment ring; the organ layer dropped 16,753
triangles; the retina scene adds about 40 small lathe meshes. Transparent overdraw is lower than before: no ghosted
skeleton behind the organs, the floating brain at 96 % instead of 45 %.

## Evidence

- `Captures/quality-2026-10-14/before_after_opening.jpg` — 13 October frames and final frames at the same intro times
  (1024×768, 1 s steps: corridor, gate, establishing, approach, title, skin, muscle, bone, organs, eye).
- `before_after_retina.jpg` — the five Scene 8 shots, same named cameras, before and after.
- `before_after_eye_to_brain.jpg` — Scene 9 light, signals, pathway and brain beats at matched cinematic times.
- `pulse_sequence.jpg` — the optic pulse at its start (retina/optic disc), mid-nerve and arriving at the brain.
- `opening_final_sheet.jpg`, `scene9_final_sheet.jpg`, `scene8_final_sheet.jpg` — contact sheets of the final captures.
- `Builds/Checks/quality-20261014/walkthrough_full.mp4` — the final build's real-time run with audio (1280×720), plus
  `opening.mp4` (the opening alone), the walkthrough logs and the test XML.
- Runnable Development player: `Builds/HumanEye/Windows-Quality-20261014/HumanEye.exe`.

## Not done / open items

- **Rack bodies are three different people.** Rack 01's skeleton is registered by eye spacing, orbit height, crown and
  feet, but its torso is shorter and narrower than Rack 02's musculature, so the bone beat reads as a slighter figure and
  the muscle-to-bone wipe briefly shows muscle below a narrower ribcage. A shared body would need the owner's modeller.
- **Rack 02's cranium** is the model's own pale epicranial aponeurosis; it is warmed and matted here, not re-textured.
- **Scene 9 pathway beat (from above the crown)** is the weakest shot (7/10): the x-ray head is now a faint bone ghost
  with a 35 % brain, but a lateral head view with the pathway drawn as one ribbon would read better; the current
  Z-Anatomy pathway meshes are kept as the owner's accurate geometry.
- **No microcamera drift** was added in Scene 8 (named static shots; the brief's "microcamera movement" would be a new
  code-driven move, which the project's camera rules reserve for the two cinematics).
- WebGL module not installed, no tablet, no listening review; the owner's Editor was left open and untouched; the
  licence gates for the supplied environment, eye and Rack files remain (`THIRD_PARTY_ASSET_CREDITS.md`). Nothing was
  downloaded. Nothing is committed (`*.fbx` still needs Git LFS before the environment can be pushed).
