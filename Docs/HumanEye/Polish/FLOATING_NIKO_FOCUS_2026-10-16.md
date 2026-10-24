# Floating Niko, camera focus and the owner's rod and cone (16 October 2026)

Brief: replace the rectangular Niko card with the rigged 3D Niko as an immersive floating companion, fix the blurred camera
and wrong focus across the module, keep every existing function, dry-run and screenshot-QA the whole module at 16:9 and 4:3.
Added the same day: replace the Scene 8 rods and cones with the owner's `D:\AI unity project\models\rod cone .blend`.
All batch work ran in the sandbox copy (`C:\he-sandbox`); the owner's Editor was never operated.

## What changed

| Area | Files |
| --- | --- |
| Floating companion (new) | `Scripts/Niko/NikoFloatingCompanion.cs` (was `NikoPresenter.cs`, same GUID, still on the HUD object) |
| Niko rig | `Scripts/Niko/NikoRig.cs`: `Home*` spot, `floating`, `lens`, `MirrorPoint`; head/neck/hip turns about his own up; floating gaze on the line of sight with a 20° head limit |
| Focus | `Scripts/Core/ShotLook.cs` (rewritten), `Scripts/Stage/Stage.Opening.cs` (no depth of field in the opening) |
| Stage / HUD | `Stage.cs` (`Companion`, pointing toward the middle), `UI/Hud.cs` (portrait line gone), `UI/WorldLabels.cs` (`ScreenRects`), `Anatomy/AnatomyPart.cs` (`Cut`) |
| Editor patches | `Editor/Builder.FloatingNiko.cs` (new, `ApplyFloatingNiko`), `Editor/Builder.RodCone.cs` (new, `ApplyRodConeModels`), `Builder.Niko.cs`, `Builder.ReferenceV2.cs`, `UiPass.cs`, `Validator.cs` |
| Rod and cone export (new) | `Tools/HumanEye/export_rod_cone.py` → `Assets/HumanEye/ThirdParty/UserModels/RodCone/` (two FBX, three albedo JPEGs, report) |
| QA harness | `AutoPilot.Capture.cs` (HDR scene render, UI rendered apart and laid on in linear light), `AutoPilot.cs` (`niko_frame` log line, `-heNoCapture`), `AutoPilot.UI.cs` (labels made this frame are not audited), `AutoPilot.Presenter.cs`, `Shaders/UIGlass.shader` (alpha written as coverage) |
| Tests | `Tests/PlayMode/NikoCompanionTests.cs` (was `NikoPresenterTests.cs`), `CinematicTests.cs`, `WalkthroughTests.cs` |
| Scene and assets (patch output, copied back) | `Scenes/HumanEye_Observatory.unity`, `UI/ObservatoryCanvas.prefab` (card removed), `Generated/Materials/Niko_Body.mat`, `Niko_FloatGlow.mat`, `Niko_FloatMotes.mat`, `RodCell.mat`, `ConeCell.mat`, `ConeCell_Outer_L/M/S.mat`, `Generated/Meshes/NikoFloatGlowQuad`, `RodCell_Outer/Body`, `ConeCell_Outer/Body`; `Generated/UI/NikoPortrait.renderTexture` removed |

## How the floating Niko works

There is one Niko (`NikoRig`), always drawn by the lesson camera; no card, portrait camera or render texture remains.

- **Where he is.** In his own shots (`*Niko*` shots, the end of the opening) he stands at his spot in the room. Otherwise
  he floats beside the lens. The opening's gate shot and the menu put him beside the gate. `NikoRig.Place/MoveTo` now set
  his spot (`Home*`); while he floats, the companion owns his transform.
- **Distance and size.** `NikoFloatingCompanion` (execution order 1100, after Cinemachine and the lens shift) puts his feet
  at a viewport slot, at a depth of 0.45 × the nearest visible subject surface, never past the blur start, at most 0.9 m.
  He is scaled so he is always 28 % of the view height, so FOV changes and near-plane shots never change his size.
- **Which corner.** Each frame the slots are scored by what would lie under him:
  - captions, panels, buttons, the toast and the top bar (weight 4);
  - world labels and every visible structure or prop (2);
  - the explained structure, part by part (3).
  A small thing hidden whole counts as much as a big thing half covered, and a 6 % margin stops things from seeming to
  sit on his head.

  The slots themselves:
  - Lessons: the lower-left of the 3D area (preferred), or just left of the instruction column.
  - Cinematics: the screen's lower corners above the letterbox, the right one unless the brain floats there.
  - Each slot sits low, or is raised in three steps.

  He slides up and down on his side. A change of side, a cut or the letterbox arriving makes him reappear in the new
  corner; he never slides across the picture.
- **Motion.** He bobs, drifts and sways, is turned three-quarters toward the middle, and lags a touch behind camera
  turns. He flies 0.8 s between his spot and the corner when his spot is in view; after a cut he appears instead.
  Reduced motion gives no flights and a calmer float.
- **Animation.** `NikoRig` is unchanged as the one driver (idle, talk styles, gestures, expressions, blinks, lip sync on
  his blend shapes):
  - floating, he looks at things where the learner sees them (on the line of sight, at his own distance) with at most a
    20° head turn, the eyes doing the rest;
  - pointing uses the arm toward the middle, and the arm flips if he changes side mid-gesture;
  - head, neck and hip turns are now about his own up axis, so a tilted camera no longer rolls his head.
- **Look.** While he floats:
  - a faint emission over his own albedo (0.05), so a dark set never turns him black;
  - a soft cyan hover glow under his feet and a few slow motes;
  - a rim light on his own rendering layer. It sits above and behind him as the camera sees him, sized with him, and its
    intensity scales with distance squared so it stays the same when he floats close to the lens.
  - He throws no shadow from the lens position.
- **Audits.** `Visible` checks the box his figure actually covers (feet, crown, head sides, hands), not the chosen slot:
  - inside the screen;
  - his head below the top;
  - at most a sixth of him under the letterbox.

  The walkthrough checks it every frame (`presence_coverage`) and logs a `niko_frame` line per capture.

## Why the camera was blurred, and what fixed it

1. **The opening's eye extraction (26.5–29.5 s) blurred the whole frame, the eye included.** The opening set a Gaussian
   depth of field focused at the eye. URP blurs by the opaque depth buffer, so the extracted eye, fading in see-through,
   took the room's blur. Fix: no depth of field in the opening.
2. **Close-ups blurred the explained part (the lens, isolated walls).** The macro look's blur ran whenever a close-up was
   active. Isolated parts and the clear media (cornea, aqueous, lens, vitreous) write no depth, and neither do the light
   rays or a sliding cut-away half, so those pixels took the room's blur. Fix: `ShotLook` blurs only when every pixel of
   the eye has a wall behind it:
   - the eye is closed;
   - no rays are drawn;
   - every non-clear part is opaque;
   - no half is moving.

   The blur switches off on the very frame that stops being true. It starts 1.6 eye radii behind the eye's centre, so
   the whole globe is in front of it.
3. **The blur popped on and off and was stronger during transitions.** URP clamps the Gaussian radius to at least 0.5, so
   easing the radius was a jump. With the blur inside the weighted macro grade, a part-faded weight blended it toward the
   default profile (radius 1, start 10 m). Fix: the depth of field moved to its own volume at full weight (made at
   runtime from the grade's own settings) and fades by its circle of confusion.
4. **The Niko card itself was soft.** It was a 256 px render texture scaled up. The 3D Niko renders at full resolution.
5. **The QA screenshots did not show what the learner saw.** A single-camera render skipped the volumes (no depth of
   field), an 8-bit target clamped highlights before tonemapping, and the UI was blended in gamma space. Fix: the scene
   renders through the full pipeline into HDR and the UI is laid on in linear light. Every "before" in this report was
   captured with the corrected path.
6. **Not code: softness that remains by asset or setting.**
   - The window sky is a 1672×941 image used as a cubemap.
   - The retina and choroid textures are 800 px.
   - The Mobile/WebGL tier renders at 0.8 scale (`Mobile_RPAsset`).
   - No anti-aliasing: MSAA off, and the camera AA is None, so edges alias rather than blur.

### Focus per sequence

| Sequence | Grade | Depth of field |
| --- | --- | --- |
| Start screen, menus | lab | off |
| Opening (gate, face, layers, extraction, hand-over) | intro grade | off throughout |
| Lesson wide / overview shots (`Wide`, `Far`, `Near`, `S04_`, `S08_`, `S09_`, `S10_`) | lab | off |
| Closed-eye close-ups (`EyeHero`, part shots before isolation, `S05_PupilFront` without rays, `S11_Eye`) | macro | soft background blur (radius 0.6) from 1.6 eye radii behind the eye's centre |
| Cut-eye close-ups (`InsideHero`, `Part_lens/retina/…`, `LensClose`, `Retina_Close`, `Choroid`) | macro | off (clear media and cut faces have no depth behind them) |
| Any isolation fade, cut-away slide, ray diagram | macro | off at once |
| Eye-to-brain cinematic (Scene 9) | intro grade | off |
| Magnified retina (Scene 8) | lab | off |
| Niko | n/a | always in front of the blur start, so never blurred |

## The owner's rod and cone

`export_rod_cone.py` (Blender 5.1, background) produces the cells:
- From each cell it keeps the textured surface. The .blend's see-through glass membrane becomes the Anatomy shader's rim.
- Each cell is decimated from about 18,000 to 7,000 triangles, for the twenty-cell field and WebGL.
- Each cell is split at the connecting cilium into the disc stack and the rest, so the lesson's light response still
  pulses only the outer segments.
- Empties mark the outer segment, inner segment, nucleus and synaptic terminal.
- The packed albedos are written unchanged, plus a grey copy of the cone map.

`Builder.ApplyRodConeModels` installs them:
- It is additive and idempotent: it swaps the pieces inside the existing 14 rod and 6 cone cells.
- Rods are 2.0 m tall and cones 1.85 m, turned over so the outer segments rest on the pigment layer and the branched
  terminals reach toward the bipolar cells. Light still comes from the vitreous side, above.
- The cells' open fronts (mitochondria, nucleus) face the cameras, with a small fixed yaw per cell.
- Rods keep their purple map. The cones' disc stacks are tinted for the three cone classes (long, medium, short
  wavelength) over the grey map, as on the art boards.
- The four part labels move onto the labelled rod's flank at the measured heights.
- Picks, parts, shots and signal paths are unchanged.
- Textures import at 2048 (WebGL 2048).

## Verification (final round 18; logs `C:\he-sandbox\logs`, captures `C:\he-sandbox\after6-2026-10-16`)

- **Patches.** `HUMAN_EYE_FLOATING_NIKO_APPLIED portraitCams=0 bindings=152 card=gone emissionGI=RealtimeEmissive
  rimLayer=True`; `HUMAN_EYE_RODCONE_APPLIED rods=14 cones=6 rodHeight=2.00 coneHeight=1.85 trianglesPerRod=7000`.
- **Checks and builds.** Validator 72 passed / 0 warnings / 0 errors. EditMode 27/27. PlayMode 8/8. Windows build and
  WebGL build: `Build Finished, Result: Success`.
- **Player runs.** Every run had 0 exceptions:

  | Run | Checks | Niko presence frames / failures | Notes |
  | --- | --- | --- | --- |
  | Opening 1600×900 | 2/0 | 741 / 0 | |
  | Opening 1024×768 | 2/0 | 1,819 / 0 | |
  | Opening, reduced motion | 2/0 | 439 / 0 | |
  | Full walkthrough 1600×900 (stress) | 44/0 | 7,024 / 0 | narrated frames 2,909 / 0 |
  | Full walkthrough 1024×768 (reduced) | 17 / 4 | 4,280 / 0 | see below |
  | Scene 9 | 7/0 | 2,956 / 0 | |
  | Real speed, chapters 1–2 | 8/0 | 9,417 / 0 | |

  The four 1024 FAILs are one pointer-audit line set on the frame the lesson switches to Exploring: the activity panel
  re-lays out over its action row for that frame. They are intermittent (rounds 16 and 18, not 14 and 17), and the overlap
  is in the before set too. The mouth moves only while Niko speaks (silent maximum 0.000–0.004).
- **Performance.**
  - Capture-free runs (`-heNoCapture`, round 16, chapters 1–2 and Scene 9) at 1600×900: 60 fps, worst frame 18–20 ms,
    about 2 KB of garbage per frame. Rendering is skipped in a hidden player window, so this is the CPU side.
  - Round 18 capture runs, which render the frame for each screenshot: 47–57 fps per chapter while writing PNGs.
    Draw-call and triangle averages are diluted by the frames the hidden window does not render. Peaks: up to about 980
    draw calls (opening), 263–269 in Scene 8 with the new cells. Each cell is 7,000 triangles, so the field is about
    140,000.
  - GPU timing is unavailable in these runs. A visible-window timing run on the target hardware is still to do.
- **Visual QA.**
  - Two reviews, each claim checked by an independent second agent: 37 agents over round 11's 480 frames, then a final pass
    over round 18.
  - Code review: 22 agents, 15 confirmed findings, all fixed: yaw axis, side flips, cuts, five depth-of-field edge cases,
    patch re-run safety, the stripped emission keyword, capture accuracy.
  - Every fix was re-verified in rounds 12–18.

**Evidence** (before | after, 24 moments): [`Captures/floating-niko-2026-10-16/`](Captures/floating-niko-2026-10-16/)

| | |
| --- | --- |
| Start screen | `01-start-screen.jpg` |
| Opening | gate `02`, title `03`, face hold `04`, organs `05`, extraction `06` |
| Lessons | Niko speaking `07`, eye `08`, cornea `09`, iris `10`, lens `11`, retina `12`, optic nerve `13`, pupil `14` |
| Scene 8 rods and cones | wide `15`, rods `16`, cones `17` |
| Scene 9 | signal to brain `18`, pathway `19`, brain `20` |
| Ending | activity `21`, lens at 4:3 `22`, closing at 4:3 `23`, end menu `24` |

## Copy-back

`C:\he-sandbox\copy_back_floating.py` copied 35 files after a dry run that listed exactly them. It checked the owner's
scene against the pre-sync hash `5b459123…`; the scene is now `a8b34737…`.
- Two files the patch removed (`NikoPortrait.renderTexture` and its meta) were moved into the backup, not deleted.
- Originals: `D:\AI unity project\_backups\floating-niko-2026-10-16`.
- The owner opened the project in the Editor at 15:04, before the 15:10 copy. If Unity offers to reload the scene, choose
  **Reload**; saving the open scene first would replace the patched one. The Editor had generated its own metas for the
  new RodCone files and `Builder.RodCone.cs`; the copy replaced them with the sandbox metas the scene references.
- Re-applying is also safe: Tools > Human Eye > Authoring > *Apply floating Niko* and *Apply rod and cone models* are
  idempotent. They are the final patches; `WowPatch`, `UiPass` and `RebuildMicro` would otherwise bring back the card and
  the procedural cells.

## Remaining problems

- **Placement where a frame has no free corner.** In the Scene 8 wide shot the cells fill the bottom band, so Niko stands
  low with the caption over his legs while that line plays. In the 4:3 rod and cone close-ups he stands in front of a
  translucent neighbouring cell.
- **Scene 9 X-ray shot.** The frame is bright and hazy by design, so Niko reads paler there than elsewhere.
- **Corridor.** Before the gate opens, the corridor's warm light makes him grey-beige until the lab.
- **Pre-existing, not caused by this pass.**
  - Scene 8 micro labels linger into the start of the Scene 9 cinematic.
  - The Scene 9 skip button says "Skip intro".
  - Quick-check hint text clips.
  - The activity panel overlaps the status line at 1024×768.
  - `Builder.Lit` sets emissive materials' GI flags to None, so URP strips `_EMISSION` from those materials when they are
    inspected.
- **Not measured.** GPU frame time, and WebGL in a browser (the build succeeds; it was not run in a browser).
