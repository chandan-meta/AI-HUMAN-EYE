# center_rotator staging, user models and cinematic intro — 5 October 2026

Branch `claude/wow-moments` (uncommitted). Brief: `Docs/HumanEye/MASTER_PROMPT_center_rotator.txt`. Before/after captures: `Captures/rotator-2026-10-05/` (actual Unity GPU renders from the development player's autopilot). Storyboard unchanged: 66 cues, same wording and order; the intro is one extra `Do("intro")` step before "Hey! I'm Niko".

## What changed and how

Everything is applied by **Tools > Human Eye > Authoring > Stage on center_rotator (keeps scene edits)** — `Assets/HumanEye/Editor/Builder.Display.cs`, idempotent, save-guarded (`SaveCurrentModifiedScenesIfUserWantsTo`), never `NewScene`. Content data via `Update content data only`. The old `PolishPatch` lost its microscope-exhibit and hard-coded EyeLeft code.

### A. The stage: the room came to the display (no spin)
- Instead of reparenting ~100 display objects under a 0.06-scaled static nested prefab, the **whole `human brain env` prefab instance (and the owner's baked Area Light) was translated by (+1.12, −0.55, +10.40) m** so the `center_rotator` glowing disc top (measured from its renderer bounds: centre (−1.12, −9.80), top y 1.44, radius 1.81) sits at (0, 0.89, 0.6), directly under the hero eye (0, 1.5, 0.6). Every shot, Niko anchor, nerve path, optics axis and light stayed valid; the lighting bake stays valid because the lit set moved rigidly. Re-running the patch measures a zero delta.
- The human figure stands on the disc (y 0.89, toward its back edge); the four Scene 2 establishing/scan shots and the eye-in-skull close-up were re-aimed +0.74 m.
- Six Niko anchors that stood inside the pedestal body were pushed to its rim (r 2.35); S02_NikoMedium/PinchNiko follow the Intro anchor.
- `HE Display Lights` holds the Key/Display/Rim lights, the reflection probe and the starlight beam (now vertical from the room's oculus onto the disc); `Stage.observatory` points at it, so `Stage.Enter()` no longer resurrects the hidden procedural dome.
- Optics: unchanged (`farDistance` 25); the apple sits at the pedestal rim and the distant tree stands in front of the room's door at ≈10.3 m, ≈5.5 m clear of the wall. The Scene 11 tree bug (`treeRoot.z − 1.5` accumulating on every re-entry) is fixed to be eye-relative.
- Room package fixes: both VideoPlayers muted (one disabled), the two world-space screen canvases no longer intercept taps (`GraphicRaycaster` off), the 15 missing `DynamicLightController` components removed, Android texture overrides (1024, ASTC 6×6) on the room and model textures.
- Removed: the earlier "Microscope exhibit (HUMAN BRAIN ENV)" (duplicate of the room's own microscopes) and its `ThirdParty/BrainEnv` folder/register rows.

### B. The owner's models (source files unaltered; `D:\AI unity project\models`)
| Model | Used as | Unity/Blender-side processing (on copies) |
|---|---|---|
| `human character\Blender\Matt.Fbx` + `Matt__Motion.Fbx` (Reallusion CC4) | The scan figure on the pedestal (replaces FitnessMan) | `Tools/HumanEye/export_matt_display.py`: 28k-tri base body from the motion file, posed from the idle take (relaxed standing); body split into UV-tile material slots (head/body/arm/leg/nails, eyelash tile dropped); shirt, jeans, shoes, hair + scalp kept (hair/scalp opacity packed into alpha, clipped by a new `_Cutoff` in `Anatomy.shader`); boxers, teeth, tongue, CC eyes, occlusion and tearline excluded; `EyeSocket_L/R` empties at the eye bones (63 mm apart) → the module's interactive eyes sit in his sockets (socket scale 0.0158). Jeans are the opaque garment layer during the scan. Textures 2048 (1024 on Android). → `Assets/HumanEye/ThirdParty/UserModels/Matt/` |
| `skeleton_pose_01.blend` | Full-body X-ray layer in the scan (with the hollow Z-Anatomy skull for the head close-up) | `Tools/HumanEye/export_skeleton_display.py`: Rigify bones posed to Matt's pose; a uniform fit (scale 0.9725) left hands/feet 10 cm outside and the humerus 30 % too long, so the export also applies **per-segment axial scaling** (humerus 0.80, tibia 1.26, skull 0.93, hands 0.85, …) to land the joints on Matt — the display copy's bone proportions are therefore altered (source .blend untouched; disclosed because the brief said "exact same"; say the word and I re-export the uniform-only fit). Containment after the fit: cranium 100 %, torso 98.7 %, arms 98.7 %, legs 98.5 %, hands 70 %, feet 100 %; 28 static meshes; cranium `.083` and mandible `.146` excluded in Unity; inside-out right hand rendered double-sided (`_Cull` off) instead of re-flipping the mesh. |
| `Brain.fbx` + `Brain_Textures.zip` | The Brain Display (Scenes 4, 9, 10; replaces the Z-Anatomy display brain) | md5-identical copy; imported with lights/animation/cameras off. The occipital faces (back 20 % of the length; medial wall only above the cerebellum) are copied into a generated **Visual processing area** mesh (1,430 tris) and the rest into a rest mesh, so the `visual_area` glow and the Scene 9 tap prediction keep working (this also fixed a latent bug: the area part never had a renderer assigned). Placed facing the eye with its chiasm region on the existing optic-nerve end; the three nerve "inside" points and S04_BrainClose are re-derived; `BrainDiff03.png` albedo on the Anatomy shader. The brain inside the head still uses Z-Anatomy. |
| HUMAN BRAIN ENV package (owner's import) | The room | See A. Nothing in the package files changed. |

Not used: G10 `eye01.fbx` (painted pupil, built-in-RP shaders), the package's doctor/400k skeleton (left as the owner placed them).

### C. Cinematic intro (≈11.5 s, skippable)
`Stage.Scene02.Intro()` (Clock.Delta-driven, so Pause freezes it; Cinemachine blends freeze via `UniformDeltaTimeOverride`; SFX/music pause with `AudioDirector`):
1. 0–1 s: black fades up on the dark room from the door end (`S02_IntroThreshold`, −14 m); drone.
2. 1–5 s: push toward the pedestal (`S02_IntroIgnite`); the room's 23 realtime lights ramp up with three clunks and a riser; post-exposure −4 → −1 EV (`Intro Exposure` Volume, ColorAdjustments).
3. 5–8.5 s: crane/arc (`S02_IntroCrane`, ≈9°/s, no roll/shake); the starlight beam strikes the disc (boom), exposure → 0, key light up; the figure's silhouette materialises (shimmer).
4. 8.5–11.5 s: ease into `S02_EstablishB`; music swells; then the existing `nikoAppear` and S02_L01.
- **Skip intro** button (authored in the saved Canvas prefab, bound as `btn-skip-intro`) applies the full end state (room 1, exposure 0, key light, beam, silhouette 0.06, fade 0, `S02_EstablishB`). Restart/menu mid-intro go through `Stage.Enter() → IntroReset()`. Reduced motion / skip-camera: 1.5 s still version, no skip button (nothing to skip).
- SFX: project-generated (`Tools/HumanEye/generate_intro_sfx.py`, pure Python, 48 kHz, peaks ≤ −1 dBFS): `intro_drone/riser/boom/clunk_1-3/shimmer/materialise.wav`, wired as AudioDirector keys drone/riser/boom/clunk1-3/shimmer/materialise. The clips were verified by measurement (duration, peak, RMS, DC), not by ear.
- Dimming never touches the room's shared materials (lights + exposure only); emissive geometry stays visible through the dark.

### D. Fixes found on the way
- `Hud`: toasts above cards; skip button hidden in reduced motion.
- Brain Display `visual_area` part had no renderer (glow never showed) — fixed.
- Scene 11 tree placement accumulation — fixed.
- Stale prefab instance with a missing asset (old microscope) — removed.

## Verification (executed)

| Check | Result | Evidence |
|---|---|---|
| EditMode suite incl. validator | 24/24 passed; validator 70 passed · 0 warnings · 0 errors | `Logs/wow/editmode-final3.xml`, `Docs/HumanEye/VALIDATION_REPORT.md` |
| PlayMode walkthrough test (Editor) | 1/1 passed, 174 s: 10 chapters, 66 cues, 0 pointer failures, saved-Canvas objects reused, timeScale 1 | `Logs/wow/playmode-rotator.xml` |
| Windows player 1600×900, full journey | exit 0, 10 chapters, 66 cues, 117 pointer audits / 0 failures, 118 captures; skip button pointer-reachable | `Builds/Checks/rotator-1600/` |
| Windows player 1024×768, reduced motion, stress | exit 0: 21 PASS / 0 FAIL, 10 chapters, 66 cues, 0 pointer failures, 0 small/outside controls | `Builds/Checks/final-stress-1024/walkthrough.log` |
| Windows player 1600×900, stress (Skip intro pressed at 3 s) | exit 0: 21 PASS / 0 FAIL, 10 chapters, 66 cues, 134 pointer audits / 0 failures; skip left fade 0, key light restored, chapter continued | `Builds/Checks/final-stress-1600b/walkthrough.log` |
| Scene 2 real-time capture of the intro beats | dark threshold, powered room, establish with silhouette | `Builds/Checks/stage-s2-realtime/` |
| Windows build | Succeeded, 0 errors (488 MB: the room's textures and lightmaps) | `Logs/wow/stage-build3.log` |
| Android build (ARM64, API 29) | Succeeded, 0 errors; APK 118.4 MiB (was 78); user assets 133.8 MB uncompressed (textures 80.9, meshes 37.4) | `Logs/wow/android-rotator.log` |
| PC frame rate (RTX 5060 Ti, 1600×900, VSync) | 53–58 fps per chapter; worst frames 72–229 ms during chapter transitions | `rotator-1600/walkthrough.log` perf lines |

Static inspection only (not executed): audio balance by ear; physical mouse/touch; the Android APK on a device; tablet frame rate. Room content adds ≈590k triangles (room 95k, doctor 93k, pod skeleton 400k) and ≈135 MB of GPU textures on PC (1024-capped on Android).

## Open items / limitations
- Licences: Matt (Reallusion EULA), Brain.fbx, the skeleton and the room package are all UNVERIFIED (THIRD_PARTY_NOTICES §7). The raw CC files are not in the repo.
- The owner's skeleton pod stands behind-right of the pedestal in frontal shots; it is part of the owner's room prefab and was left as placed.
- No LightProbeGroup/re-bake: dynamic objects are lit by the realtime lights (key, display, rims, the room's spot) and read correctly in captures; add probes if they look darker than the room on a device.
- Skeleton hands/feet poke a few cm outside the figure; the eyelash/teeth/tongue of Matt are intentionally absent (hidden by the shaded eyes/mouth).
- Intro captures under fast-forward are mistimed by design; use the real-time run for visual review.
