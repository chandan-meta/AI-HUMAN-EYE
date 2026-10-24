# Human Eye — project handoff

User preference: use the Ponytail skill for coding work until explicitly disabled.

## Reference eye finish and live landing screen (24 October 2026)

The existing scene now has dielectric eye surfaces, transmitting cornea/media, a static studio reflection, refined
lighting and a separate brighter landing composition. The supplied geometry/UVs, lesson sequence and narration references
remain in place. Landing copy is live Unity text over the original Figma PNG surfaces; guide placement now clears the
activity, Ask and next-step panels. Start with the [natural-speed preview, before/after, full-module QA movie and evidence](Docs/HumanEye/LandingReference20261024/README.md).
Final Windows build succeeded; 33 EditMode checks and eight distinct presentation cases passed across isolated runs.
The final desktop movie and full tablet walkthrough cover all ten chapters and 65 cues, with zero blocked or off-screen
controls. All 3,684 QA-movie frames were measured automatically, with sampled/native visual review; this is not a claim
that every frame was individually human-reviewed. Some spoken-subtitle views still cover the guide's lower body and some
micro labels approach his head; the quiz/next-step clearance fix does not establish zero cosmetic overlap everywhere.
WebGL runtime and physical tablet testing remain unverified.

## Direct Figma PNG integration (24 October 2026)

Exported Pearl + Iris PNG panels and buttons are now applied to the existing saved Canvas. The scene Canvas was already unpacked and remains the UI authority; no runtime UI generation was added. See [integration evidence and validation status](Docs/HumanEye/FigmaIntegration/README.md). The design-only and glass-style descriptions below record earlier revisions and do not establish runtime validation of this integration.

## Pearl + Iris Figma design and material candidates (23 October 2026)

**Design:** https://www.figma.com/design/wLgTj7N6mpfRONuYkmZac0 (Vaibhav Sharma's account).
Twelve desktop compositions, eight tablet adaptations, native components, illustrative prototype states,
PNG previews, SVG icons and texture maps are documented in `Docs/HumanEye/PearlIrisRefresh/HANDOFF.txt`.
The retina candidate is `Assets/HumanEye/PearlIrisPreview/Retina_PearlIris_Candidate.mat`.
It was compared with the original using the actual Unity anatomy shader at two scene cameras; the vitreous-hidden
diagnostic identified the grey boundary as a transparency/cutaway effect. Production scene assignments were preserved.
Mobile compression and runtime performance remain unchecked. The runtime redesign below remains the installed UI.

## Redesign: eye-awakens opening, journey HUD, voiced checks, Free Explore (22 October 2026, latest)

**What changed** (owner brief: the module was dull, confusing and broke immersion):
- **Opening:** "the eye awakens" (21 s, no human figure); the start screen is its first frame.
- **Niko:** navy investigator cap with an eye badge; the red veins on his head are painted out.
- **Selection:** a selected part gets a strong cyan rim, tint, pulse and scan lines.
- **HUD:** journey bar, 3 top tools, one breathing Next-step card, parts as chips, centred finale card.
- **Quick checks:** voiced by Niko, lettered tiles, gentle feedback, a right answer moves on by itself.
- **Accommodation:** ciliary muscle ring, retinal image that blurs and sharpens, one new line.
- **Complete working:** shown once (Scene 10), the camera riding with the light.
- **Free Explore** after the finale.
- **Voice:** Niko's voice is local Kokoro (am_fenrir, 1.05).

**Patch:** `Builder.ApplyRedesign` (final; sandbox step `redesign`). It unpacks the scene Canvas from its prefab.

**Verification:** Validator 73/0/0, EditMode 27/27, PlayMode 8/8, Windows + WebGL builds, every walkthrough 0 FAIL
(full module at 16:9 stress and 4:3 reduced, Free Explore at both sizes).

**Report and evidence:** `Docs/HumanEye/Polish/REDESIGN_2026-10-22.md`, `Captures/redesign-2026-10-22/`.

**Open:** re-trace the WebGL shader warm-up before publishing; nothing committed or published.

## Web loading page, production WebGL build, GitHub Pages (16 October 2026)

**Live:** https://chandan-meta.github.io/AI-HUMAN-EYE/ (`gh-pages`; the repo was made public).

**What changed**
- **Loader.** The loading page is the WebGL template `HumanEyeLoader`:
  - the owner's artwork exactly, as a cover background that never crops the eye, nerve, title or bar;
  - a live bar pinned over the drawn one and driven by Unity's real loading progress;
  - light depth particles and an iris ring.

  The module fades in once it has drawn the start screen (`HumanEyePage.jslib`, called from `ModuleBoot`).
- **Build.** `PlayerBuilds.WebGL -heRelease` makes a release build (gzip with decompression fallback, hashed names,
  web-only texture overrides). Size: 129.5 MB, down from the 401 MB Development build.
- **Publishing.** `C:\he-sandbox\publish_pages.ps1 -Push` splits the data file into parts under GitHub's 100 MB limit;
  the loader joins and caches them.

**Verification.** Tested live in Chrome and Edge at 1920×1080 and at a 1024×768 tablet size, and in the app's browser:
load, start screen, BEGIN, opening, refresh with resume. No 404s or errors.

**Remaining.**
- On first play the opening's shaders compile, so the screen is black for a few seconds after BEGIN.
- There is no ASTC variant for mobile browsers.

**Report:** `Docs/HumanEye/Polish/WEB_LOADER_PAGES_2026-10-16.md`.

## Floating 3D Niko, camera focus, the owner's rod and cone (16 October 2026)

**What changed**
- **Niko.** The rectangular Niko card is gone, along with its portrait camera and render texture. `NikoFloatingCompanion`
  draws the one rigged Niko beside the lens in a clear lower corner:
  - He keeps a constant 28 % of the view height.
  - He stays nearer than the subject and in front of the blur.
  - He is scored off captions, panels, labels, every visible structure and the explained one.
  - He bobs and sways and looks three-quarters toward the subject.
  - He flies to his spot in his own shots.
- **Focus.** `ShotLook` blurs only the background of closed-eye close-ups whose every pixel has an opaque wall behind it.
  It uses its own full-weight volume and fades by circle of confusion. The opening has no depth of field, so the
  extraction is sharp again.
- **Scene 8.** The rods and cones are the owner's `models/rod cone .blend`, exported by
  `Tools/HumanEye/export_rod_cone.py` and installed by `Builder.ApplyRodConeModels`:
  - outer segments on the pigment layer, open fronts toward the cameras;
  - cone disc stacks tinted L/M/S.

**Re-applying.** The final patches are `ApplyFloatingNiko` and `ApplyRodConeModels`; the sandbox steps are `floatniko`
and `rodcone`.

**Verification.** Final round 18:
- Validator 72/0/0, EditMode 27/27, PlayMode 8/8.
- Windows and WebGL builds succeeded.
- Every player run had 0 Niko-presence failures over about 26,700 frames. There was one intermittent 4:3 pointer-audit
  line set, timing only (see the report).

**Copy-back.** 35 files copied, 2 moved to `D:\AI unity project\_backups\floating-niko-2026-10-16`; the scene is now
`a8b34737…`. The owner's Editor opened this project at 15:04, before the 15:10 copy. Reload the scene when Unity asks;
don't save over it.

**Report and evidence:** `Docs/HumanEye/Polish/FLOATING_NIKO_FOCUS_2026-10-16.md` and
`Captures/floating-niko-2026-10-16/` (24 before/after sheets).

## Wait for Continue no longer stalls Scene 9 (16 October 2026)

**The bug.** With Settings > Wait for Continue on, the eye-to-brain cinematic stopped after its first line until Skip was
pressed. The line waited for Read & Continue, but that button lives in the caption panel, which a cinematic hides.

**The fix.** `ModuleBoot` now waits for Continue only when no cinematic plays. Lines that end inside a cinematic move on
as they do with the setting off, and every other line still waits.

**Verification (sandbox).**
- `AutoPilot -heWaitContinue` turns the setting on and presses the real button; it fails if a line waits behind a
  cinematic. Sandbox steps: `s09wait`, `walkwait`.
- Before the fix, it failed at 9.9 s in Scene 9.
- After the fix:
  - Scene 9 passes 8/0 and the full run with the setting on passes 81/0, with 64 real Continue clicks.
  - Validator 72/0/0, EditMode 27/27, PlayMode 8/8.
  - The regular walks pass unchanged.

**Copy-back.** Code only (`ModuleBoot.cs`, `AutoPilot.cs`), so nothing was copied back and the scene is untouched.

**Evidence:** `Docs/HumanEye/Polish/Captures/wait-continue-2026-10-16/`.

## Audio overhaul: CC0 music and effects on every event (15 October 2026)

Eight CC0 music beds and about 60 effects replace the single pad. The sources are OpenGameArt tracks and Kenney, Magic
Spell, Teleport and "18 random" packs, downloaded by `Tools/HumanEye/fetch_audio_sources.py` and processed by
`Tools/HumanEye/build_audio_assets.py` into `Assets/HumanEye/Audio/{Music,Ambience,SFX/*}`. Every source has a row in the
licence ledger `Audio/Licenses/AUDIO_SOURCES.csv`.

**How it works.**
- **Patch.** **Apply audio overhaul (keeps scene edits)** (`Builder.ApplyAudioOverhaul`, sandbox step `audio`, the last
  patch) routes every source through `Audio/HumanEyeMixer.mixer` and registers the beds and the 63-key `CueClips` table.
- **Music.** `AudioDirector` crossfades the beds and ducks music about 10 dB under Niko. `Stage.Audio` picks the bed,
  ambience and travelling-pulse loop from the lesson state every frame.
- **Cues.** Effects fire on real events: the opening's clock and state, content steps, the HUD, checks, signals and skip.

**Documents.** The cue map is `Docs/HumanEye/Audio/AUDIO_CUE_SHEET_IMPLEMENTED.csv` (69 rows) and the full account is
`Docs/HumanEye/Audio/AUDIO_IMPLEMENTATION_REPORT.md`.

**Still open.**
- A listening pass by a person: selection and levels were judged by measurement.
- WebGL playback in a browser.
- The ElevenLabs voice, as below.

**Verified in the sandbox (round 7b).**
- Validator 72/0/0; EditMode 27/27; PlayMode 8/8; Windows and WebGL builds.
- Six walkthroughs with 0 failures: both openings, a full 1600×900 stress run, a full 1024×768 reduced-motion run, and
  Scene 9 and Scenes 2–3 at real time.
- Zero double triggers at real speed, and music ducked -9.9 dB under Niko.

**Review.** An independent multi-agent review raised 23 findings: 14 confirmed and fixed, 9 rejected.

**Copied back** with originals under `_backups/AI-HUMAN-EYE_2026-10-15_pre-audio/`. If `HumanEye_Observatory.unity` is open
in the Editor, reopen it before Play.

**Evidence:** `Docs/HumanEye/Polish/Captures/audio-2026-10-15/`, including `opening_cues.png`.

## Niko: rigged model, gestures, lip sync and permanent presence (15 October 2026)

Niko is the owner's updated rigged `niko.fbx` (Mixamo bones, shape keys Close, Ou, Blink, Eye_L/R/U/D, plus BrowUp from
`Tools/HumanEye/export_niko.py`), installed by **Apply supplied Niko character** (`Builder.ApplyNikoCharacter`). `NikoRig` is the
one procedural driver: idle, talk and gesture states, expressions, gaze from his eyes, eased blinks and nods, and lip sync
from mouth keys on every cue (the clip's ElevenLabs timing when an `.align.json` exists, otherwise keys precomputed from the
WAV by `Builder.AudioKeys`, which also works on WebGL). His head is wider than his shoulders, so arm poses keep raised hands
beside the head; any pose change must pass `Tools/HumanEye/niko_arm_clearance.py` in Blender. `NikoPresenter` keeps him on
screen in every running frame (main view or the live companion picture, never twice). Verified in the sandbox: validator
72/0/0, EditMode 27/27, PlayMode 7/7, both full walkthroughs with Niko visible in every frame. The patched scene was copied
back (previous files under `_backups/AI-HUMAN-EYE_2026-10-15_pre-niko-v2/`); the open Editor must reopen
`HumanEye_Observatory.unity`, or its old buffer binds the new mesh to the old bones and Niko looks distorted. **Still
open:** the cute childlike ElevenLabs voice is prepared (`Tools/HumanEye/generate_elevenlabs_vo.py`) but not generated, because
no `ELEVENLABS_API_KEY` exists; set it as a Windows user environment variable, run `voices`, `samples` and `all`, then re-run
Apply supplied Niko character. Report and evidence: [NIKO_COMPANION_2026-10-15](Docs/HumanEye/Polish/NIKO_COMPANION_2026-10-15.md).

## The corridor gate opens the module (15 October 2026)

The owner added a sliding double gate with its own corridor (`Assets/THE HUMAN EYE/Fbx/gate .fbx`, instance "gate ") and
removed the lab's lifting door. `Builder.ApplyCorridorGate` (**Tools > Human Eye > Authoring > Apply corridor gate**, sandbox
step `corridor`, the final additive patch after the ANATOMY.blend models) makes the gate `Stage.entrance` (a `LabEntrance`
whose leaves slide apart along the gate's cross axis), so the menu, skip, replay and chapter entry keep their gate
behaviour; `Stage.Opening` starts `gateApproach` (8 m) behind the doorway at `gateFov` (50°), holds a second, parts the
leaves over 1–3.5 s and enters on the unchanged rail (the lens eases to the bust lens by 8 s; nothing after 9 s changed).
The patch also restores the lab's walk colliders (`Builder.LabWalkColliders`), which the owner's save with another Open
Dome FBX loaded had dropped. Verified in the sandbox (validator 72/0/0, CinematicTests 2/2, PlayMode 7/7, 1600×900 intro
captures) and the scene copied back (previous save under `_backups/AI-HUMAN-EYE_2026-10-15_pre-corridor-gate/`); the
open Editor must reopen `HumanEye_Observatory.unity` before Play, or its old buffer still points at the deleted door and
the gate will not move. Report and evidence: [CORRIDOR_GATE_2026-10-15](Docs/HumanEye/Polish/CORRIDOR_GATE_2026-10-15.md).

## ANATOMY.blend models replace the scan subject's anatomy (15 October 2026)

The owner's `D:\AI unity project\models\ANATOMY.blend` (skin character with shorts and lashes, muscles, skeleton,
internal organs, blood vessels; brain and eyeballs measured only) now supplies the opening's layers and every later shot
that used the Rocketbox skin or the Rack 01/02/04 extracts (the Scene 9 X-ray skull, the legacy choreography).
`Tools/HumanEye/export_anatomy_blend.py` (Blender 5.1) takes the side-by-side columns back to one origin, scales to
metres, drops the breathing morphs, builds a Blink blend shape, writes per-material albedo/normal/spec maps (DirectX
normals flipped) and the registration empties into `Assets/HumanEye/ThirdParty/UserModels/Anatomy/`;
`Builder.ApplyAnatomyModels` (**Tools > Human Eye > Authoring > Apply ANATOMY.blend models**, the final additive patch after
gate-to-face and Niko; sandbox step `anatomy`) swaps the skin under the same `Human Display` root, moves the sockets to
the new eyeballs, re-fits the Z-Anatomy head structures, the Rack 06 head brain and the X-ray skull, and fits the
muscle, skeleton, organ and new vessel layers through the existing `RackLayer` (now per-slot materials). Opening
timings, breakup, camera, sounds, narration and gameplay are unchanged. Rack 06 stays the brain (more detailed than the
.blend's single mesh, and the lesson's visual-area split depends on it). Report, mapping table, verification and
limitations: [ANATOMY_MODELS_2026-10-15](Docs/HumanEye/Polish/ANATOMY_MODELS_2026-10-15.md). Verified in the sandbox
(round 24): validator 72/0/0, CinematicTests 2/2, PlayMode 7/7, walkthrough 1600×900 stress 35 PASS / 0 FAIL (10 chapters,
65 cues, 54–60 fps), intro captures at 16:9 and 4:3 and the Scene 9 cinematic reviewed; the verified scene, generated
meshes/materials and the Anatomy meta files were copied back to this project (`C:\he-sandbox\copy_anatomy.py`), so reload
the saved scene before editing the open Editor's old buffer. Backup before the change:
`D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-15_pre-anatomy` (replaced originals under `copyback-originals/`).
Licence of the .blend (textures come from a ripped "Animated Full Human Body Anatomy" package) is an owner release gate
(THIRD_PARTY_ASSET_CREDITS.md). The superseded Rocketbox and RackAnatomy folders remain until the owner accepts the result.
Nothing committed. Later the same day the owner re-imported `Assets/THE HUMAN EYE/Fbx/Open Dome.fbx` (26 MB, 15:16, external
materials) and asked for the previous file back: the 8 October 8.4 MB FBX and its meta (embedded materials, same GUID) were
restored from the sandbox mirror; the new import with its `Open Dome.fbm/` and `Materials/` by-products is kept in
`_backups/AI-HUMAN-EYE_2026-10-15_open-dome-import-1516/`; the two by-product folders were then removed from the project
at the owner's request, so the `Fbx` folder matches its pre-import state exactly.

## Skybox through the windows and the muscle head (15 October 2026)

The owner's skybox (`Starlit Galaxy Over Snowy Peaks.mat`, assigned in Lighting) never drew because the Main Camera
cleared to a solid colour: the scene's camera flag, `Builder.Build` and `Builder.VisualMatch.Sky` (which now keeps an
assigned skybox) are fixed, and `Stage.Awake` sets the main camera to Skybox at runtime so the owner's unsaved Editor
scene shows it in Play mode too. The Rack 02 muscle head read as a white dome because `AnatomyPart.Configure` switched
ZWrite off for a solid layer under the shoulder body fade, so the mesh's own far surfaces drew over the near ones; a
solid layer now keeps depth writes while blending, and the muscle tint is a neutral 0.72 below white (wrap 0.18, normal
strength 1.6). Diagnosis trail, captures and verification:
[MUSCLE_HEAD_DEPTH_2026-10-15](Docs/HumanEye/Polish/MUSCLE_HEAD_DEPTH_2026-10-15.md). Nothing committed.

## Supplied Niko character and Rack 02 muscle maps (14 October 2026)

Niko is now the owner's cartoon character (`niko.fbx`, Tripo model on a Mixamo skeleton, no clips or blend shapes in the
source). `Tools/HumanEye/export_niko.py` scales it to 1.2 m and builds `Blink` and `MouthOpen` blend shapes from the
painted eyes and mouth, plus `MouthWide`, `MouthRound` and `BrowUp`; `NikoRig` is a procedural control rig on the
bones (breathing, weight shift, look-at with nod on the voice, pole-vector two-bone arms with clavicle lift and finger
curls, wave / raise hand / present / point / pinch / snap, blinks, and visemes: jaw from loudness, wide / round on
syllable onsets, brow lifts) so every narrated beat keeps its gesture and the live portrait keeps working. Gesture
sides are the viewer's ("R" = Niko's -X arm); the patch measures which skeleton arm that is. Apply with **Tools > Human Eye > Authoring > Apply supplied Niko character**
(`Builder.ApplyNikoCharacter`, the final additive patch after the gate-to-face patch). The Rack 02 muscle layer now
uses full-resolution albedo and normal atlases (`export_rack_muscle_maps.py`) under a near-white tint
(`ApplyGateFaceOpening.MuscleLook`), matching the supplied model instead of the flat red of the breakup captures.
Report, rig table, evidence and limitations: [NIKO_CHARACTER_2026-10-14](Docs/HumanEye/Polish/NIKO_CHARACTER_2026-10-14.md).
Backup of the owned assets before this change: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-niko`.

## Electric breakup transitions (14 October 2026, latest)

The owner's v3 brief replaces the flat top-down layer wipe with the reference clip's electric-cyan patchy disintegration
(`20261014-0921-28.5960490.mp4`). The camera, the gate-to-face opening, its timings and the skin → muscle → organ order
are unchanged (the owner fixed the camera and asked for it not to be touched). What changed: one seeded Worley + fractal
breakup field in subject-root space (`Assets/HumanEye/Shaders/AnatomyDissolve.hlsl`) now drives the `_Wipe` test in every
pass of `Anatomy.shader`, so the departing layer erodes in scattered islands with thin flickering cyan crack filaments
while the next layer appears in exactly the same places (no gap, no hard swap, correct depth and shadows); the motes emit
from the departing layer's mesh surface and `DissolveMote.shader` keeps only the ones born on breaking patches (a Custom1
birth stamp written by `AnatomyRevealController`); a soft blue haze quad glows behind the bust and lingers; the burst is
cyan; the scan ring is gone; a new `crackle` sound (`Tools/HumanEye/generate_reference_sfx.py`) plays under each breakup.
All knobs are Inspector fields on `AnatomyRevealController` (amplitude, cells per metre, seed, filament reach, colour,
intensity, mote rate, haze peak). Skip, replay, pause and menu return use the existing cleanup (`ResetVisual`).

Apply with the existing final patch **Tools > Human Eye > Authoring > Apply gate-to-face opening (keeps scene edits)**
(`Builder.ApplyGateFaceOpening`, idempotent; `OpeningMagic` authors the breakup). Do not regenerate the scene.
Report, timing table, evidence and limitations: [ELECTRIC_BREAKUP_2026-10-14](Docs/HumanEye/Polish/ELECTRIC_BREAKUP_2026-10-14.md).
Backup of the owned assets before this change: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-breakup`.

## Gate, face hold, blink and lip correction (14 October 2026, latest)

The latest opening begins behind the closed gate, waits for it to lift, then approaches the character on a straight
rail. It holds a shoulder/head portrait for two seconds, blinks the original eyelids, and reveals skin → muscle →
organs with cyan/gold wisps. No skeleton beat. The 32.5-second opening still ends with eye extraction and Niko.
The mouth defect in the owner's screenshot is fixed: the solid head material retains depth during the shoulder fade,
and the new blink derivative preserves the original lips/chin. The faint organ outline remains transparent.

Final additive patch: **Tools > Human Eye > Authoring > Apply gate-to-face opening (keeps scene edits)**
(`Builder.ApplyGateFaceOpening`, after ApplyReferenceV2). Do not regenerate the scene. Source and verified scene assets
are copied back. The owner's open Editor stayed untouched; reload the saved scene before editing its old buffer.

[Shot timing, implementation and QA](Docs/HumanEye/Polish/GATE_FACE_OPENING_2026-10-14.md).
[Opening preview with audio](Builds/Checks/gate-face-20261014/HumanEye_Gate_Face_Opening.mp4).
Windows delivery: `Builds/HumanEye/Windows-GateFace-20261014/HumanEye.exe`.
WebGL delivery: `Builds/HumanEye/WebGL-GateFace-20261014/index.html` (serve over HTTP).
Verification: 7/7 Play Mode tests, 2/2 final cinematic regressions after the lip fix, and rendered 16:9/4:3 openings
plus a 16:10 reduced-motion run. Both development builds succeeded; this WebGL revision was not browser replayed.
The older opening descriptions below are historical.

## Visual reference book v2 (14 October 2026)

The new v2 brief supersedes the earlier blue pulse direction. The owner's later correction requires waist-up anatomy
transitions with the scan effect; the full-body title remains before the transition sequence. Apply with **Tools > Human Eye > Authoring >
Apply visual reference v2 (keeps scene edits)** (`Builder.ApplyReferenceV2`, after the existing opening/quality patches).
This imports the supplied Rack 06 brain, reuses Niko's live portrait camera for narration PiP, widens the lab opening,
holds matching waist-up anatomy reveals and changes the neural packets to gold-white. The complete route is labeled schematic;
the head demonstration still shows the optic chiasm/tract/LGN/radiation/V1. Original cues and interactions remain.

Shot plan: [V2_SHOT_PLAN_2026-10-14](Docs/HumanEye/Polish/V2_SHOT_PLAN_2026-10-14.md).
Backup: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-v2`.
Source: `C:\Users\ADMIN\Documents\Received Files\Rack 06.fbx`; imported original and anatomy-only derivative are in
`Assets/HumanEye/ThirdParty/UserModels/RackBrain/`. `Tools/HumanEye/export_rack_brain.py` extracts the supplied maps and
preserves all 77,882 triangles and original UVs, excluding the cabinet. No external download.

Implemented assets are copied back and both development builds include the final waist-up correction.
[Final report and evidence](Docs/HumanEye/Polish/REFERENCE_V2_2026-10-14.md): EditMode 26/26, PlayMode 7/7,
final cinematic regressions 2/2, validator 72/72. Complete run: 65 cues, 14,789 narrated frames, zero presenter or
pointer failures; 204 recorded samples visually reviewed. That full run predates the last unvoiced opening crop;
[the final opening recording](Builds/Checks/v2-20261014/after-intro-halfbody/HumanEye_UpperBody_Opening.mp4) shows the correction.
The owner's Editor stayed open and untouched: reload the saved scene before editing to avoid overwriting it with the old buffer.

## Visual quality pass (14 October 2026)

Brief: the owner's `Human_Eye_Visual_Reference_and_Implementation.pdf` and master prompt (camera, lighting, anatomy layers,
brain, eye-to-brain pulse, photoreceptors, transition VFX). The opening now starts with a lifted corridor and a wide
establishing shot from the threshold, arcs toward the stand, holds the full body under the title and pushes to a medium
bust for the layer reveals (soft fade below the navel; a cyan scan ring and motes ride each wipe; bone leaves completely
before the organs; the organ layer was re-extracted without the mouth and teeth and now shows the optic pathway with the
brain and eyes). Signals are electric-blue comets drawn with the depth test off, so they ride the visible nerve; the
saved `nerve` pulse route, left in world space when the rig moved onto the stand on 8 October, is re-anchored to the eye's
cut frame (pulses had been running invisibly below the stand since then). The floating brain is solid in the eye-to-brain
cinematic; the magnified retina has disc-stacked outer segments, inner segments, nuclei and synaptic terminals with labels.

Apply with **Tools > Human Eye > Authoring > Apply visual quality pass (keeps scene edits)** (`Builder.ApplyQuality`, after
`ApplyOpening`; idempotent; the subject is never moved). Shot distances, lenses and the exposure lifts are Inspector
fields on `Stage` (Opening shots). Organ re-extract: `Tools/HumanEye/export_rack_organs.py` (Blender 4.5). Details,
evidence, scores and open items: [VISUAL_QUALITY_PASS_2026-10-14](Docs/HumanEye/Polish/VISUAL_QUALITY_PASS_2026-10-14.md).
Pre-change backup: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-quality`. The owner's open Editor was not
touched; verified scene and generated assets were copied back from the sandbox (`C:\he-sandbox\copy_back22.ps1`).

## Organ portrait update (13 October 2026, later request)

After the full-body title frame the existing dolly pushes into a head/shoulder portrait. A default-off shader mask
softly fades the body below the shoulders. Production order is skin → Rack 02 muscles → Rack 01 bone → Rack 04
organs with the existing brain and eyes, followed by eye extraction and Niko. Four small saved particle bursts accompany
the transitions; Pause, Skip, menu/restart and reduced motion use the existing cleanup. Total duration is still 32.5 s;
the 7.5 s reduced-motion version now includes an organ still.

Rack 04 has internal torso organs and mouth/teeth, but no eye or brain mesh. `export_rack_organs.py` extracts 107,507
triangles into one material/2048 atlas and adds inferred body registration markers. Fit those markers, not mouth-to-pelvis
bounds. `Builder.ApplyOpening` remains the additive authoring command. The owner's open Editor remains untouched;
verified serialized assets are copied back from the sandbox after checks, with a pre-change disk backup retained.

Details, verification and limitations: [ORGAN_PORTRAIT_2026-10-13](Docs/HumanEye/Polish/ORGAN_PORTRAIT_2026-10-13.md).
## Centered anatomy opening (13 October 2026)

The current production opening is 32.5 seconds and preserves the owner's centered human transform. It uses inspected
Rack 01 skeleton and Rack 02 muscle derivatives for whole-body skin → muscle → bone wipes, then a continuous eye
extraction into the existing lesson. The title sits above the subject; a generated discovery score crossfades into
gameplay; Pause and Settings remain visible. The older ReferenceMatch choreography is retained separately.

Use **Tools > Human Eye > Authoring > Apply centered anatomy opening (keeps scene edits)** (`Builder.ApplyOpening`)
to reapply this pass. Do not regenerate the scene or rerun older placement patches over the owner's centered layout.
The patch also tunes the eye lighting/materials and updates the existing Canvas title. The optional exhibit eye can
remain absent. The Main Camera's stale `ViewportShift` script reference has been repaired in the saved scene.

Implementation, exact file groups, source model audit, QA results and limitations:
[CENTERED_OPENING_2026-10-13](Docs/HumanEye/Polish/CENTERED_OPENING_2026-10-13.md).
The pre-change backup is `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-13_pre-opening`.
The older pass descriptions below are historical; the October 13 production order supersedes their intro order.

## Visual-match pass in the new lab (8 October 2026, branch `claude/wow-moments`)

The module now plays in the owner's new lab (`Assets/THE HUMAN EYE`, kept as modelled) and follows the seven art boards:
the lesson rig sits on the "THE EYE" stand (hero point 0, 2.95, 0), daylight outside the windows with a galaxy
projection over the roof, a full re-bake with light and reflection probes, a bright blue iris, gold neural signals, a
coral brain, pearl/navy/cyan Niko and the boards' caption navy. The opening is the module's only scan: the extracted eye
flies to the central display (the lab's exhibit eye folds into the stand beforehand) and Scene 2 goes straight on to
Explore Eye; `S02_L05` is retired (65 cues spoken). The saved scene did not start on arrival (`Stage.roomLights` pointed
at deleted lights); that is fixed. Details, evidence and open items:
[VISUAL_MATCH_PASS_2026-10-08](Docs/HumanEye/Polish/VISUAL_MATCH_PASS_2026-10-08.md); credits:
[THIRD_PARTY_ASSET_CREDITS.md](THIRD_PARTY_ASSET_CREDITS.md). `CLAUDE.md` carries the new rules.

**How it is applied:** after the earlier passes, **Tools > Human Eye > Authoring > Apply visual match (new lab)** (batch
`Builder.ApplyVisualMatch`, idempotent, must stay last) then **Bake lab lighting** (`Builder.BakeLab`, GPU, ~1 min). Sources:
`Tools/HumanEye/blue_iris.py` then `whiten_sclera.py`; `generate_daylight_sky.py`. The patched and baked scene, its lighting
folder and settings, the Canvas prefab, generated assets, three tuned environment materials and the new metas were copied
back from the sandbox (`C:\he-sandbox\copy_back21.ps1`). A file backup of the module, settings, docs and the environment's
materials from before the pass is in `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-08_pre-visual-match`.

**Verification (sandbox, round 21x):** validator 71/0/0; EditMode 24/24; PlayMode 3/3 (single scan → Explore Eye, Explore lab
on the new floor, eye-to-brain, full walkthrough); GPU bake 3 lightmaps / 278 probes / 4 reflection probes; Windows build
450 MB; walkthroughs 1600×900 stress 35 PASS / 0 FAIL and 1024×768 reduced 15 PASS / 0 FAIL (10 chapters, 65 cues,
0 exceptions, 53–60 fps); a real-time recorded run; board-by-board comparison sheets and a before/after board in
`Docs/HumanEye/Polish/Captures/visual-match-2026-10-08/`.

**Owner decisions:** Niko's childlike voice; Figma authorisation; licences of the new environment and the eye models;
add `*.fbx` to Git LFS before committing (two environment FBX files exceed 100 MB).

## Reference cinematics (8 October 2026, branch `claude/wow-moments`)

The opening now reproduces the Human Brain reference's camera choreography (corridor → gate lift → lab → full body → face →
skin/muscle/bone/glass wipes → the eye floats out → INSIDE YOUR EYES → Niko from a vortex) on one code-driven dolly with two
timing profiles (`Stage.timing`: Production ~30 s, Reference match 91 s), and Scene 9 opens with an eye → optic nerve →
chiasm → tract → LGN → radiation → visual cortex cinematic (FROM EYE TO BRAIN) that stays in step with the ledger lines.
Z-Anatomy facial muscles, extra-ocular muscles and the visual pathway were exported and fitted to the scan subject; the
anatomy shader gained a world-height wipe; the eye has an amber iris and stronger vessels; eight new sounds; the letterbox
title. Details, shot list, evidence and open items: [REFERENCE_PASS_2026-10-08](Docs/HumanEye/Polish/REFERENCE_PASS_2026-10-08.md),
[REFERENCE_SHOTLIST_2026-10-08](Docs/HumanEye/Polish/REFERENCE_SHOTLIST_2026-10-08.md). `CLAUDE.md` carries the new flow rules.

**How it is applied:** after the production, UI, look and cinematic passes, **Tools > Human Eye > Authoring > Apply reference
cinematics** (batch `Builder.ApplyReference`), idempotent. Sources: `Tools/HumanEye/export_zanatomy_vision.py` (Blender 5.1 on
the Z-Anatomy `Startup.blend`), `amber_iris.py` then `whiten_sclera.py`, `generate_reference_sfx.py`. The patched scene, Canvas
prefab, generated assets and the new assets' meta files were copied back from the sandbox; the next Editor open reimports them.

**Verification (sandbox, round 20):** validator 70/0/0; EditMode 24/24; PlayMode 3/3; Windows build 502 MB;
walkthroughs 1600×900 stress 35 PASS / 0 FAIL and 1024×768 reduced 15 PASS / 0 FAIL (10 chapters, 66 cues, 0 exceptions,
56–59 fps); player intro captures at 1600×900 and 1024×768 and the Scene 9 cinematic capture.

**Owner decisions:** Niko's childlike voice (audition sets in `Logs/vo-alt/child-zira`, `child-heera`); the eye reference image
was not found on this machine; Figma stays unauthorised.

## Production upgrade (7 October 2026, branch `claude/wow-moments`)

The visible eye is now the owner-supplied cross-section model (`D:\AI unity project\models\eye\eye model\half eye.fbx`):
kept half + section caps + a mirrored slide-away half on each structure, so the closed eye, the cutaway and the optics
are one model; `EyeGeometry` landmarks were re-measured from it (rays, picks, clear media and the nerve follow). The
anatomy shader gained normal maps, a wet tear film, a clear-medium mode (black pupil, wet cornea) and a dilating iris
opening. The scan subject is the Human package's Microsoft Rocketbox person (MIT). The UI was redesigned for the edges
(chapter chip, icon tools, captions bottom-centre, Talk to Niko bottom-right, content-sized right column, Chapters modal,
calm surfaces); the intro is letterboxed; the scan is framed on head and shoulders before the push into the eye;
close-ups get background-only depth of field; the lab grade and hall lighting were reworked. Details, model audit,
verification and open items: [FINAL_UPGRADE_2026-10-07](Docs/HumanEye/Polish/FINAL_UPGRADE_2026-10-07.md).

**How it is applied:** *Tools > Human Eye > Authoring* → **Apply production eye + human** → **Apply UI pass** →
**Apply look pass** (batch: `Builder.ApplyProduction`, `UiPass.Apply`, `Builder.ApplyLook`), idempotent, in that order.
Source data: `Tools/HumanEye/export_local_eye.py` (→ `LocalEye.json.gz`), `whiten_sclera.py`,
`export_rocketbox_display.py` (Blender 5.1 / Python). The patched scene, Canvas prefab, generated assets and the new
assets' meta files were copied back from the sandbox (`C:\he-sandbox\copy_back.ps1`); the next Editor open reimports them.

**Verification (sandbox, round 18):** validator 70/0/0; Edit Mode 24/24; Play Mode 2/2; Windows build; walkthroughs
1600×900 stress 35 PASS / 0 FAIL and 1024×768 reduced 15 PASS / 0 FAIL (10 chapters, 66 cues, 0 exceptions, 0 pointer
failures); 55–60 fps. Figma: not built (Starter/View seat MCP limit reached); the design system of record is
`Docs/HumanEye/UI_STYLE_GUIDE.md`.

## Presentation redesign (6 October 2026, branch `claude/wow-moments`)

The module's presentation was rebuilt without changing the learning sequence: a directed 8.5 s entrance, Niko who looks at and points to what he explains and stays present through a live portrait, narration regenerated with prosody and trimmed silences (288 s → 240 s of audio, same words), a right-hand information panel above a View / Activities panel with **Talk to Niko** fixed in place, lesson-driven fading labels (optic nerve included), structure isolation on the light path, the retinal image as a panel popup instead of a picture on the retina, rebuilt and brightly lit rods and cones, refined eye materials/meshes, an ending that replays the journey, pulls back to an overhead layout and ends with Niko's closing line, and a 19-clip project-generated sound set. Removed: "Rebuild the journey", Skip on checks, TRACE/SIMULATE buttons, the lens-shaping challenge, the Continue-only free modes, the mid-sequence questions in Scenes 7 and 9, the blur/fade ending. Full description, sound plan and evidence: [WOW_REDESIGN_2026-10-06](Docs/HumanEye/Polish/WOW_REDESIGN_2026-10-06.md).

**How it is applied:** source edits plus the idempotent **Tools > Human Eye > Authoring > Apply WOW redesign (keeps scene edits)** (`HumanEye.EditorTools.WowPatch.Apply`, batch `-executeMethod HumanEye.EditorTools.WowPatch.Apply -quit`), which also runs the content data build. The patched scene, Canvas prefab, generated assets, the new layer `HE_Niko` (TagManager) and the new audio clips were copied into this project from the verification sandbox; the Editor that was open during the session (unsaved scene changes, hence no batch runs here) will offer to reload them. If it reports a conflict with its unsaved scene, keep the disk version or re-run the menu item above on the owner's version — it is idempotent.

**Verification (sandbox copy `C:\he-sandbox\AI-HUMAN-EYE`, same sources, 6 October):** validator 70 passed / 0 warnings / 0 errors; Edit Mode 24/24; Play Mode 2/2 (full walkthrough 112 s at 6×, cinematic/skip/scan test); Windows development build 480 MB, 0 errors; standalone walkthroughs 1600×900 stress (35 PASS / 0 FAIL, 90 buttons reached) and 1024×768 reduced motion (15 PASS / 0 FAIL), both 10 chapters / 66 cues, 0 exceptions, 55–60 fps. Captures: `Docs/HumanEye/Polish/Captures/wow-2026-10-06/`. Automated intelligibility of the regenerated narration: average word recall 0.89 (was 0.81).

**Not done / owner decisions:** recorded narration (still provisional TTS; a "Microsoft Zira" audition set is in `Logs/vo-alt/zira`); no veterinarian exists in this module (Niko is the only speaker); no microphone or remote AI (Talk to Niko is the local text system and says so); the eye remains the procedural model with better surfaces (no licensed separable replacement was available); physical tablet/touch and listening review remain open as before.

## Cinematic entrance update (6 October 2026)

The saved scene starts at the closed gate and plays a 15-second entrance, human spotlight, face-first scan, collision-backed lab walking with an authored joystick, and staged anatomy controls. On 6 October the intro shots were re-authored (eye-level corridor view of the closed gate, a wider frame for the gate lift, a high crane through the entrance that settles on the spotlit human), the one-frame lit-room flash before the intro faded up was removed, and an inspect-mode hint toast now appears when anatomy exploration begins. Shots are written by **Tools > Human Eye > Authoring > Rebuild cinematic shots (keeps scene edits)** (`CinematicPatch.Shots`; batch `-executeMethod HumanEye.EditorTools.CinematicPatch.Shots -quit`). The full `CinematicPatch.Apply` calls the same shot block. Do not regenerate the module. The user selected MisterH's Sketchfab eye `f2370371544c4b029123da0b7d7e2457`; CC BY 4.0/downloadability verified and the user-supplied GLB is integrated as the closed-eye exterior (reapply with `MisterHEyeImport.Apply` only if the source changes). Cutaway and pupil-response teaching retain procedural anatomy. Evidence: [cinematic report](Docs/HumanEye/Polish/CINEMATIC_ENTRANCE_2026-10-05.md).

Updated: 6 October 2026 (Asia/Calcutta). **Source, saved scene and the Windows development build are current and verified by automated walkthroughs; production acceptance remains incomplete.**

## Scan close-up fix (6 October 2026, source committed, scene write pending)

The user reported that pressing Scan sent the camera above the human's head. Cause: the saved `Human Display` root carries an Editor scale of 1.495 (a hand edit saved in commit 30cda6d), so `S02_EyeInSkull` was authored from an eye socket at y≈3.39, while `HumanDisplay.ResetVisual` forces scale 1 at runtime (socket y≈2.55, crown y≈2.71). Fix in source: `Stage.FrameEyeInSkull` (Stage.Scene02.cs) re-aims the `S02_EyeInSkull` CinemachineCamera from the live socket whenever Scene 2 is entered, using the shared `Stage.CloseUpOffset`/`AimCloseUp`, so the close-up sits at eye height, centred on the eye with the crown inside the frame; `CameraDirector.Get` exposes shots by name; `CinematicPatch.ApplyShots` authors the same pose at the runtime scale with a 32° lens; `CinematicTests` now asserts, through the live camera's viewport, that the socket is centred and the top of the head is in frame, and captures `Logs/cinematic/scan-eye-in-skull.png`. The open Editor recompiled these changes with no errors (Editor.log). **Still to run with the Editor closed:** `-executeMethod HumanEye.EditorTools.CinematicPatch.Shots -quit` (writes the preview pose and 32° lens into the saved scene), the Play Mode suite, a Windows rebuild and standalone walkthroughs; the runtime re-aim works without the scene write. Decision for the owner: the human is saved at scale 1.495 but shown at scale 1; say which is intended and the other side will be aligned (ResetVisual or the saved scale).

## Resume here

- Current branch: `claude/wow-moments` (last commit: intro pass, 6 October 2026); remote: `https://github.com/chandan-meta/AI-HUMAN-EYE.git`.
- Read `CLAUDE.md` first: it is the permanent operating guide (vision, UX/UI rules, intro order, camera rules, where things live, never-repeat list). Task reports live in `Docs/HumanEye/Polish/`.
- Open `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` in Unity 6000.3.0f1. Preserve the authored scene and Canvas; do not run full `Builder.Build` regeneration. The saved scene contains the cinematic, MisterH, shot, WOW and intro patches; every patch is idempotent under **Tools > Human Eye > Authoring**. If the Editor was open while the intro pass landed, reload the scene so the saved scene and Canvas prefab are picked up.
- The entrance is `Stage.Scene02.cs` (`Intro`, timeline constants, `IntroCamera`, `IntroNiko`), the gate is `LabEntrance.cs` driven by the baked leaves under `Cinematic Laboratory/Gate leaves` (`IntroPatch.SplitGate`; the room's own door nodes are static-batched and stay inactive), the intro-time interface is the `intro-overlay` element plus `Hud.Pop` fades and `ViewportShift.Weight`.
- Verified 6 October 2026 (intro pass, sandbox copy of these sources): validator 70 passed / 0 warnings / 0 errors; EditMode 24/24; PlayMode 2/2 (`CinematicTests`: three intro skip beats, Niko present after the intro, replay, gate reset, scan close-up); Windows development build; intro-only captures at 1920×1080, 1600×900, 1366×768 and 1024×768 (0 pointer failures, 0 small or out-of-window controls); full standalone walkthroughs at 1600×900 stress (10 chapters, 66 cues, 35 PASS / 0 FAIL, intro skip exercised, 0 exceptions) and 1024×768 reduced motion (10 chapters, 66 cues, 15 PASS / 0 FAIL, 0 exceptions). Evidence: `Docs/HumanEye/Polish/INTRO_PASS_2026-10-06.md` and `Docs/HumanEye/Polish/Captures/intro-2026-10-06/`. Raw logs and captures are local, ignored evidence.
- Verification workflow when the owner's Editor is open: edit the real project, mirror it into a sandbox copy, run the batch steps there (`Validator.RunBatch`, EditMode/PlayMode, `PlayerBuilds.Windows`, `AutoPilot` with `-heIntroOnly` or the full walkthrough flags), then copy the saved scene, Canvas prefab and `Generated/` back. Never close the owner's Editor.
- Known, pre-existing: the Explore part-list buttons are 46 px tall (48 px rule). Pending: Figma mirroring of the intro overlay and reveal states (connector not authorised on this machine); WebGL module not installed (Windows development builds stand in); physical-tablet performance and OS multitouch, a sustained on-screen desktop benchmark for the 60 fps target, final artistic review, recorded narration, and the third-party release gates (THIRD_PARTY_NOTICES §7–8).
- The pre-existing owner edits to `Assets/human brain env/human brain env/human brain env/Materials/Mt8.mat`, the URP settings assets and `ProjectSettings` are preserved locally and excluded from commits.

This file records the current workspace for the next developer/chat. The attached implementation brief is reference material for continuation; it is not evidence that its requested work has been completed.

## Latest change: intro pass — sliding gate, intro dolly, Niko entry, intro-time interface (6 October 2026, branch `claude/wow-moments`)

The entrance is one code-driven dolly (`S02_IntroDolly`, ~23.6 s, skippable): initial corridor view → slow approach → the floor bolt retracts and the gate halves slide apart (no hinge; the authored panel is split into `GateLeafL`/`GateLeafR` saved meshes with edge plates) → pause → the camera moves in through the gate → Niko flies in only after the gate is fully open → the camera settles on `S02_NikoMedium` → the main interface fades in. During the intro only the bottom strip (`intro-overlay`: caption + Skip) is visible; the top bar and the right-hand column are CanvasGroups faded by `Hud.Pop`, and the lens shift (`ViewportShift.Weight`) eases in with them. Applied by the idempotent **Tools > Human Eye > Authoring > Apply intro pass (keeps scene edits)** (`Assets/HumanEye/Editor/IntroPatch.cs`). The project-level operating guide is `CLAUDE.md`; the UX/UI system is in `Docs/HumanEye/UI_STYLE_GUIDE.md`; evidence in `Docs/HumanEye/Polish/INTRO_PASS_2026-10-06.md`. Figma mirroring of the new states is pending until the Figma connector is authorised.

## Previous change: staged on center_rotator, user models, cinematic intro (5 October 2026, branch `claude/wow-moments`)

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

24 October 2026: [Cinematic intro and start-banner polish](Docs/HumanEye/Polish/CINEMATIC_INTRO_2026-10-24.md) records the lighting/VFX changes, retained Figma PNG artwork, actual Unity previews and final validation: 30 EditMode + 4 PlayMode tests passed, Windows development build succeeded, and desktop/tablet/reduced-motion checks passed. Physical tablet touch remains untested.
