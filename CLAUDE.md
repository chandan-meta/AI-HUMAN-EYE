# Observatory of Vision — project operating guide

Permanent rules for anyone (human or agent) working on this Unity module. Keep it short; put task details, evidence and
history in `Docs/HumanEye/` instead. When a new instruction conflicts with a rule here, follow the new instruction and
update this file in the same change.

User preference: always apply the Ponytail skill to coding work unless the owner explicitly disables it.

## Vision

A web-first (desktop + tablet, mouse + touch) learning module: Niko, the Vision Investigator Officer, takes a learner
through the human eye in one connected, cinematic exploration. Tone: modern, futuristic, educational, clean, premium.
No excessive sci-fi noise. The 3D subject is the hero; the interface serves it.

## UX / UI principles

- The UI never interrupts or covers the main subject. Vision-journey HUD (redesign, 22 October 2026, `UiJourney`, applied
  by `Builder.ApplyRedesign` to the scene's Canvas, which it unpacks from `ObservatoryCanvas.prefab`: the scene Canvas is the
  HUD's authority now, and the older prefab passes (CanvasAuthoring, UiPass…) no longer reach it): top-left the journey bar (ten nodes on a line of light named
  for what each chapter teaches, `Hud.StageNames`; passed = warm light, current = cyan) with the chapter title; top-right
  only Chapters, Pause and Settings (Replay line, How to play, Review and Sound live in the pause card; Home is the pause
  card's Main menu); bottom-centre one breathing Next-step card (`Hud.Objective`, short and action-first) above Niko's
  caption bubble; Talk to Niko bottom-right; a right-hand column for the activity's tools only, hidden when empty. A quick
  check fills that column (big lettered answer tiles); the finale card is centred and leads with Free Explore. The
  subject is composed inside the area left of the column (`ViewportShift` lens shift, 440 px).
- During cinematic moments the lesson interface is hidden, but Pause and Settings stay visible in the upper bar.
  The intro is letterboxed (title above the subject; caption and Skip below); the lesson UI fades in afterwards
  (`Hud.RevealSpeed`, ~0.6 s). Never place the full UI over it. A cinematic hides the caption panel and its Read &
  Continue button, so Wait for Continue never holds a line inside a cinematic (`ModuleBoot`).
- Current visual direction (24 October 2026): retain the saved Pearl + Iris Figma PNG surfaces/icons, pearl-white and
  violet controls, navy text and amber BEGIN. The landing eyebrow/title/tagline are live Unity text; its old baked
  StartBrand image is disabled. Preserve the functional scene Canvas and bindings. Earlier navy/glass authoring passes
  are historical and must not overwrite this UI. Evidence: `Docs/HumanEye/LandingReference20261024/README.md`.
- Hierarchy: subject → current instruction / Niko → supporting tools. Cyan filled = the one required action.
- Every unavailable action explains why (toast or disabled state), never a dead end.
- Design system and component rules: `Docs/HumanEye/UI_STYLE_GUIDE.md`. Figma IDs: `Docs/HumanEye/Polish/FIGMA_STATE.md`.

## Flow, animation and camera rules

- Current opening (redesign, 22 October 2026, supersedes the gate-to-face opening below): "the eye awakens", 21 s,
  `Stage.Opening.cs` (`Opening`, `AwakeningFrame`, a pure function of time). No corridor, no human figure, no organ
  layers. The start screen is now a separate brighter `Stage.Landing.cs` composition with a resting pupil, controlled
  warm key/cyan rim and calm idle motion; reduced motion uses a still pose. BEGIN hands over to the darker awakening:
  light arrives and the wide pupil tightens; the camera swings to the side as the room wakes; the eye opens into its
  cross-section in a cyan burst; light from the apple is bent by the cornea and lens to a spark on the retina; THE HUMAN
  EYE / HOW LIGHT BECOMES SIGHT; the cutaway closes and the camera settles on S02_NikoMedium. Reduced motion: four stills
  of it. The gate-to-face text below describes the retired opening and the patches that still author its assets.
- Reference eye finish (24 October 2026): keep the supplied meshes and UV-matched 2K blue iris/sclera/normal sources.
  `Anatomy.shader` has optional physical dielectric surfaces and transmitting clear media, with a static studio probe;
  shared eye materials affect every chapter. Do not replace them with bloom-only highlights or redraw the anatomy.
  Niko's shared placement reserves visible activity, Ask and next-step panels; verify native tablet clearance after edits.
- Former production direction (gate-to-face correction, 14 October 2026): begin behind the closed gate,
  open it, then dolly slowly on a straight rail toward the character's face. Hold a shoulder/head portrait for two
  seconds with a real eyelid blink before skin -> muscle -> organ transitions. Omit the skeleton beat. Keep the portrait
  steady through the reveals, with the electric-cyan breakup on each layer change and a soft fade below the shoulders.
  The eye approach/extraction leads into the original Niko medium shot. Opening remains 32.5 s; its existing
  prelude has captions and music, with lesson VO beginning afterwards. No new duplicate scan or rewritten VO.
  `Builder.ApplyGateFaceOpening` is the additive authoring patch after ApplyReferenceV2, `Builder.ApplyNikoCharacter`
  follows it, `Builder.ApplyAnatomyModels` (15 October 2026) comes next, then `Builder.ApplyCorridorGate` (the gate rule
  below), `Builder.ApplyAudioOverhaul` (audio only), and `Builder.ApplyFloatingNiko` (16 October 2026: the companion card
  out, the floating companion in; it undoes what WowPatch/UiPass would re-create) and `Builder.ApplyRodConeModels` (16 October
  2026, Scene 8 cells; it undoes a later `RebuildMicro`) come next, and `Builder.ApplyRedesign` (22 October 2026: cap, ciliary ring, FX_Orbit, the journey HUD, the inset blur, the content rebuild) is the final patch. `ApplyAnatomyModels` installs the owner's
  `D:\AI unity project\models\ANATOMY.blend` systems (skin with shorts, lashes and a built Blink; muscles; skeleton;
  organs; vessels, exported into one frame by `Tools/HumanEye/export_anatomy_blend.py` into
  `ThirdParty/UserModels/Anatomy/`) in place of the Rocketbox skin and the Rack 01/02/04 layers, moves the eye sockets to
  the new eyeballs and re-fits everything registered to them (Z-Anatomy head structures by eye spacing, the Rack 06 head
  brain into the .blend brain's box, the X-ray skull from the new skull). Tints stay white over the supplied albedos
  (`AnatomyLook`); never tint the muscle layer red. Never regenerate the scene. The owner's subject transform and lab
  remain intact. See `Docs/HumanEye/Polish/GATE_FACE_OPENING_2026-10-14.md`, for the layer-change effect
  `ELECTRIC_BREAKUP_2026-10-14.md`, for Niko `NIKO_CHARACTER_2026-10-14.md`, and for the models `ANATOMY_MODELS_2026-10-15.md`.
- Niko wears a navy investigator cap with a cyan piping ring and a glowing eye badge, attached to his head bone and sized
  from his own head mesh (`Builder.NikoCap`, from ApplyNikoCharacter and ApplyRedesign); his head texture has the red
  veins painted out (`Tools/HumanEye/niko_no_veins.py`, run after export_niko.py). 22 October 2026 brief: he looked creepy.
- Niko first appears with his first line (21 October 2026 brief): the start screen and the opening are without him
  (`Stage.NikoWaiting`), and the first line after BEGIN, Continue or a chapter picked on the menu brings him in
  (`Stage.NikoArrive`: the `nikoappear` chime, he grows in with a slight overshoot through `NikoFloatingCompanion.Grow`
  inside a cyan burst on `nikoSparkle` (sparkles spiralling up round him, a ring from his feet, a soft chest flash); reduced
  motion: no growth or burst). From then on he is on screen in every frame, speaking or silent: at his spot in the room
  when the shot frames his face readably (his own shots), otherwise he
  floats beside the lens (`NikoFloatingCompanion`, 16 October 2026 brief: the same character drawn by the lesson camera,
  camera-relative in a clear lower corner at a constant share of the view, nearer than the subject and the blur, off
  captions, panels, labels and the explained part, flying between spot and corner). There is no picture card, portrait
  camera or render texture: never bring one back, never twice.
  Niko is the owner's rigged character (`niko.fbx` of 15 October 2026 via `Tools/HumanEye/export_niko.py`: Mixamo-named
  bones, shape keys Close, Ou, Blink, Eye_L/R/U/D, plus the export's BrowUp), installed by `Builder.ApplyNikoCharacter`
  under the same `Niko` root and posed only by `NikoRig` (one procedural driver: idle/talk/gesture states, expressions,
  gaze, blinks, lip sync on Close/Ou from mouth keys on each cue: clip timing when `<clip>.align.json` exists, else keys
  precomputed from the clip's audio by `Builder.AudioKeys`, since WebGL cannot analyse audio at runtime). Stage cues set his
  expression (`Stage.CueMood`) and gestures; floating, he points toward the middle of the screen (`Stage.NikoPoint`).
  His head is wider than his shoulders and rests on them: arm poses keep upper arms at or below the horizontal and raised
  hands beside the head; any pose change must pass `Tools/HumanEye/niko_arm_clearance.py` (Blender, the shipped mesh).
  Do not create a second guide, do not add an Animator that drives his bones, and do not bring back the procedural robot.
- One scan only (visual-match brief, 8 October 2026): the opening is the module's only scan. Scene 2 continues
  straight to Explore Eye; there is no Scan Eye action, and `S02_L05` ("Click on the SCAN button.") stays in the ledger
  but is retired (`HumanEyeContent.RetiredCues`, 65 cues spoken). Replay intro is the only way to see the scan again.
- Production layer changes (skin → muscle → organs, reference clip `20261014-0921-28.5960490.mp4`, 14 October 2026) are the
  electric-cyan patchy disintegration: one seeded Worley + fractal field in subject-root space
  (`Shaders/AnatomyDissolve.hlsl`, `_HE_Dissolve*` globals set by `AnatomyRevealController`) erodes the departing layer
  in scattered islands with thin cyan crack filaments while the next layer appears in the same places (complementary
  `_Wipe` test in `Anatomy.shader`, every pass); motes are born on the departing surface (`Stage.anatomyMotes`, mesh
  shape, `DissolveMote.shader` keeps births on breaking patches), a soft blue haze sits behind the bust and the `crackle`
  sound plays. No flat sweep line, no scan ring, no gold at the layer changes (gold is the neural signal). One primary
  layer is legible at a time: no ghosted layer behind another. The older ReferenceMatch head reveal still uses the
  collar seam and the flat wipe (`_HE_Dissolve.x = 0`).
- Gate = the owner's corridor gate (15 October 2026: `Assets/THE HUMAN EYE/Fbx/gate .fbx`, instance "gate ", two leaves
  that slide apart along the gate's cross axis; `Builder.ApplyCorridorGate` makes it `Stage.entrance`, after the ANATOMY
  patch), SmoothStep ease (`LabEntrance.SetGate`); the lifting door it replaced is gone from the scene. The opening starts
  `Stage.gateApproach` metres behind its doorway at `gateFov`, holds one second, parts the leaves over 1–3.5 s and enters on
  the same rail; it is the start screen's view, closed again by `ShowMenu`. `Builder.LabWalkColliders` restores the lab's
  walk colliders (a save made with another Open Dome FBX loaded drops them).
- Every explanation frames the structure it names: the camera moves first, the structure is isolated, highlighted
  and labelled, then the line plays. The walkthrough logs a `framing` line per cue (AutoPilot); keep it clean.
- Neural signals are separated gold-white packets with tapered trails (reference v2 supersedes the blue quality pass).
  Light remains a continuous beam. The enlarged eye-to-brain shot is labeled schematic; the following head shot labels
  the real optic pathway. The ivory sheath and packet route must share the same centreline. Rack 06 supplies every active
  brain and the legacy layered-human/lab brains; retain its original UVs and extracted normal maps.
- Scene 9 opens with the eye-to-brain cinematic (`Stage.EyeBrain.cs`): light to the retina → signals along the optic
  nerve (L01) → the eye back in an X-ray head, pulses along nerve, chiasm (nasal fibres cross), tract, LGN, radiation
  to V1, each stage labelled (L02) → the visual system floats out under FROM EYE TO BRAIN (L03). Light and signals
  never share a colour or a path. Lines stay content steps; `cineBeat` and the cinematic wait for each other.
- Camera moves are eased, motivated, and never drift while nothing happens. Named Cinemachine shots are the cue sheet;
  code-driven motion is allowed only on `S02_IntroDolly`, `S09_Follow` (signal tracking, the eye-to-brain cinematic),
  `FX_Orbit` (Free Explore, the learner's own view) and the authored replay/overview moves. Human → eye is one progression, inside the opening: closed gate → direct face
  approach → blink → skin/muscle/organs → eye in the orbit → the eye on the display.
- Gameplay never asks the learner to tap moving anatomy: the turntable stops when exploration starts.
- A selected part must read at a glance: `_Highlight` above one half adds a stronger cyan rim, a body tint, a pulse and
  scan lines (`Anatomy.shader`); `Stage.Isolate` gives the chosen part the full selection look.
- The complete working (light → cornea → … → brain) is shown once, in Scene 10, with the camera riding the light through
  the lesson's own shots (`Stage.JourneyShots`), then the labelled overview under S10_L02 (22 October 2026 brief).
- Accommodation is shown, not only said (Scene 6): the retinal-image popup blurs with the live focus error
  (`UIBlur.shader`, `Hud`), the ciliary muscle ring (`CiliaryRig`, `Builder.CiliaryRing`) tightens and relaxes with the
  lens and is labelled, and the supplemental line `acc_ciliary` names it before S06_L04.
- Free Explore (after the finale; `Stage.FreeExplore.cs`): orbit, zoom (through the cornea into the eye), cut open, look
  from inside, tap or pick any part to isolate it and hear its lesson lines again, replay the light journey, Near / Distant
  / slider for accommodation. `AutoPilot -heFreeExplore` tours it.
- Quick checks are voiced and gentle: Niko reads each question and answers every try (supplemental clips `q_<id>`,
  `fb_correct_N`, `fb_retry_N`); a wrong tile is crossed out, never a red "wrong"; a correct answer moves on by itself;
  after three misses the demonstration plays and the answer shows with Continue.
- Anatomy close-ups get background-only depth of field (`ShotLook`): a small Gaussian blur starting behind the whole
  globe, only while the eye's walls are opaque (URP blurs by opaque depth, so a faded or clear part would take the room's
  blur). The opening, cinematics, eye-to-brain and micro shots stay sharp. The explained structure and Niko are never blurred.
- Reduced motion / skip camera motion (`Stage.Short`) must always have a short, still equivalent.
- Learning flow (10 chapters, 66 ledger cues of which 65 are spoken, opening scan → explore → inside, checks only at
  checkpoints) is approved: do not rewrite the state machine or add activities without an explicit brief.

## Performance and platform

- Unity 6000.3.0f1, URP 17.3, Cinemachine 3.1.5, Input System, uGUI saved Canvas prefab. Supported sizes 1024×768 and
  up; test 16:9, 16:10 and 4:3 (1920×1080, 1600×900, 1366×768, 1024×768). Controls ≥ 48 px; no clipping, no overlap.
- Target 60 fps on desktop. Keep realtime lights few; no per-button bloom; no full-screen blur.
- Audio (overhaul of 15 October 2026, `Docs/HumanEye/Audio/AUDIO_IMPLEMENTATION_REPORT.md`): one manager, `AudioDirector`.
  Music beds play by key with crossfades, and `Stage.Audio` picks menu, chapter bed, quiz or finale every frame; the
  opening plays its own score (`BeginIntroMusic(key)`). One-shots play by key on three channels: effects (stopped on chapter
  entry and skip), interface (never paused or stopped) and cinematic (stopped by skip, menu, restart and chapter jumps). A
  key never retriggers within 120 ms; label ticks and stream starts use `Soft` (dropped when another cue starts that frame);
  a check's fanfare stands alone (`Recently`). Loops belong to the event that starts them; there is one ambience; built loops
  carry a lead-in and a WAV `smpl` loop point (Unity Web's AAC encoder alters the first 1024 samples). Niko's voice ducks
  music about 10 dB, Ask Niko 4 dB; `AudioDirector.MixState` names the snapshot equivalent. The mixer (`Audio/HumanEyeMixer.mixer`:
  Master > Voice, Music, Ambience, SFX, UI, Cinematic) carries routing and bus trims only, because Unity Web supports group
  volume alone, so fades and ducking stay on the sources. Clips are built from CC0 downloads by
  `Tools/HumanEye/build_audio_assets.py` (fetch: `fetch_audio_sources.py`; ledger `Audio/Licenses/AUDIO_SOURCES.csv`) and
  registered by `Builder.ApplyAudioOverhaul` (`CueClips`). New sounds must be game-licensed (CC0 preferred) with a ledger
  row; never Mixkit stock music. Never a punitive buzzer, weapon laser, horror or heartbeat sound in this children's module.
- The PC renderer is Forward+ and the Mobile one is Forward: `Anatomy.shader` must compile its additional-light loop
  for both (`_CLUSTER_LIGHT_LOOP` + `_ADDITIONAL_LIGHTS`) or spots and rims silently stop lighting anatomy.
- Lighting hierarchy (fields on `Stage`, `Stage.Lighting.cs`): key light (the sun: directional, mixed, soft shadows,
  aimed from the generated daylight sky's sun) · `faceKey` · `eyeKey` (hero catchlight) · `eyeRim` · `eyeFill` ·
  `brainRim` (nerve sequence only) · `interiorLight` (intro only); the room itself is baked (the owner's ceiling area
  light, the sky through windows and roof, the cyan light strips) with light probes for everything that moves and
  baked box-projected reflection probes (lab, stand, corridor). The scene uses its own lighting settings
  (`Scenes/HumanEye_Observatory_Lighting.lighting`); re-bake with Tools > Human Eye > Authoring > Bake lab lighting.
- Sky and ceiling are two layers: the skybox the owner assigned in Lighting (`THE HUMAN EYE/Texture/Materials/Starlit
  Galaxy Over Snowy Peaks.mat`, a cubemap; the Main Camera must clear with Skybox or the windows go black; the older
  `Generated/Textures/DaylightSky.hdr` stays only for the bake) and the galaxy as an interior projection on a shell
  outside the roof (`GalaxyDome.shader`, cut below the ceiling ring so side windows never show it). Custom opaque shaders need
  DepthOnly and DepthNormals passes: the PC renderer primes depth for SSAO and drops surfaces missing from the prepass.
- Web (24 October 2026): published at https://chandan-meta.github.io/AI-HUMAN-EYE/ from `gh-pages` (site only).
  Use `Tools/HumanEye/publish_pages.ps1` with an explicit verified build and a new external work directory; it retains
  current history, verifies file hashes, splits data over 90 MiB and uses an ordinary push. The old external force-push
  publisher is superseded. The WebGL template provides the bottom-left fullscreen/exit control; keep its 48px touch
  target, safe-area spacing and exit/Escape state handling. This browser control stays outside the saved Unity Canvas.
  The loading page is the WebGL template `Assets/WebGLTemplates/HumanEyeLoader` (the owner's artwork exactly, a live bar on
  Unity's real progress, light effects; `HumanEyePage.jslib` fades it into the module). Build it with
  `PlayerBuilds.WebGL -heRelease` (Brotli with decompression fallback, hashed names, no splash, IL2CPP for size, web-only
  texture overrides; generated meshes quantized and music/voice at quality 0.35 for the build only, their files restored
  afterwards, 91.22 MiB in the 24 October 2026 release); only
  `-heRelease` passes the AssetAudit gate. See `Docs/HumanEye/Polish/WEB_LOADER_PAGES_2026-10-16.md`. WebGL compiles a
  shader variant on first draw, so the variants first drawn after BEGIN (`Resources/ShaderWarmup.txt`) are warmed in
  `ModuleBoot.Start` behind the loader; re-trace the list (`Tools/HumanEye/web_begin_check.mjs --trace`, development build)
  when the opening's materials, shaders, lights or post effects change, or BEGIN goes black again.

## Where things live

- Content (steps, labels, isolation, ending): `Assets/HumanEye/Editor/HumanEyeContent.cs` → `Builder.BuildData()`.
- The lab is the owner's new environment (`Assets/THE HUMAN EYE`, the `Open Dome` prefab instance): keep it as
  modelled. The module sits on its central stand (top 1.78 m, ring tier 0.78 m): hero/cut point (0, 2.95, 0), Niko on
  the stand top, the scan subject on the ring tier toward the gate; the exhibit eye (`Stage.displayEye`) is dynamic and
  only shows before the opening's hand-over and on the menu.
- The eye is the owner-supplied cross-section model (`models/eye/eye model/half eye.fbx`, baked by
  `Tools/HumanEye/export_local_eye.py`): kept half + section caps + mirrored slide-away half on each `AnatomyPart`;
  bright blue iris from `Tools/HumanEye/blue_iris.py`, sclera from `whiten_sclera.py`.
  `EyeGeometry` landmarks (cornea, iris, pupil, lens, retina, optic disc) are measured from it; rays and picks follow.
  The scan subject is the ANATOMY.blend skin character (`Tools/HumanEye/export_anatomy_blend.py`, Blender 5.1; the Rocketbox
  person from `export_rocketbox_display.py` is superseded). Every anatomy layer FBX carries `EyeSocket_L/R`,
  `FaceDirection`, `BodyBase`, `BodyTop` so `Builder.RackLayer` can fit it; the ANATOMY export adds `EyeFront_L/R`
  (socket radius) and `BrainMin/Max`. Rack 06 remains the brain everywhere; the .blend brain and eyeballs are measured only.
- Intro, scan, Niko per cue: `Assets/HumanEye/Scripts/Stage/Stage.Scene02.cs` (production opening: `Stage.Opening.cs`);
  gate: `Scripts/Stage/LabEntrance.cs`. Magnified retina (Scene 8; light from the vitreous side, outer segments on the
  pigment layer): the layout is `Editor/Builder.Micro.cs` (`RebuildMicro`); its rods and cones are the owner's models
  (`D:\AI unity project\models\rod cone .blend` via `Tools/HumanEye/export_rod_cone.py` into `ThirdParty/UserModels/RodCone/`,
  installed by `Builder.ApplyRodConeModels`: textured cell only, the .blend's glass shell becomes the rim; the disc stack is
  its own renderer for the light response; cone stacks tinted L/M/S over a grey copy of their map; rods keep their map).
- HUD binding/state: `Scripts/UI/Hud.cs` (no runtime UI creation; elements are bound by name from the saved Canvas).
- Scene/prefab changes go through idempotent editor patches (`Editor/CinematicPatch.cs`, `WowPatch.cs`,
  `IntroPatch.cs`, then `Builder.Production.cs` → `UiPass.cs` → `Builder.Look.cs` → `Builder.Cinematic.cs`, whose
  steps live in `Builder.Lighting/Sfx/Intro/Nerve/Framing.cs`, → `Builder.Reference.cs`: Z-Anatomy vision layers,
  lifting gate, particles, title, sounds → `Builder.VisualMatch.cs`: the new lab's layout, sky, galaxy dome, probes,
  palette; then **Bake lab lighting**), menu **Tools > Human Eye > Authoring**. `ApplyVisualMatch` moves the lesson rig
  by the offset from the hero point, so it must stay the last patch: re-running an earlier patch (e.g. `IntroPatch`)
  needs the later ones re-applied after it, on a scene whose lesson rig has not been moved twice. Never run the full
  `Builder.Build` regeneration.
- Verification: `Validator.RunBatch`, EditMode/PlayMode tests, `AutoPilot` walkthrough flags (`-heAutopilot
  -heOffscreen -hePolish -heFast N -heReduced -heStress -heIntroOnly -heWaitContinue -heCapture <dir>`).
- Evidence and reports: `Docs/HumanEye/Polish/`; handoff: `HANDOFF.md`.

## Conventions

- Element keys are kebab-case (`btn-skip-intro`, `intro-overlay`); shots are `S0N_Name`; anchors are PascalCase.
- Unity fake-null: never `??` / `?.` on `UnityEngine.Object`; use `if (!x)`.
- `NikoRig` gesture sides are the viewer's: "R" is the arm on Niko's -X side (his anatomical left, he faces +Z);
  `Builder.ApplyNikoCharacter` measures which skeleton arm that is. Pose bones in world space from the rest pose with a
  pole vector (`Reframe`), never with a bare `FromToRotation`, or limbs twist.
- Saved meshes/materials go through `Builder.SaveMesh` / `Upsert` so GUIDs stay stable.
- `SignalPulses` routes with no `space` are world-space constants: anything that moves the lesson rig must move them
  too (`Builder.ApplyQuality` re-anchors `nerve` to the eye's cut frame; the 8 October move left it behind for six days).
- Ledger cue text is authoritative and validated exactly; never paraphrase a cue.
- `AnatomyPart.Configure`: a solid layer that only carries the shoulder body fade blends but keeps writing depth. With
  ZWrite off its own far surfaces drew over the near ones (15 October 2026: the muscle head showed the white inside of its
  skull cap; see `Docs/HumanEye/Polish/MUSCLE_HEAD_DEPTH_2026-10-15.md`).

## Never do this again

- Do not close or touch the owner's open Unity Editor; verify in the sandbox copy (`C:\he-sandbox`, see memory notes).
- Do not claim a change, asset, voice or test is done unless it ran; report failures with their output.
- Do not add Continue buttons, Skip on quizzes, mid-sequence questions, or a "rebuild the journey" ending.
- Do not use unlicensed online TTS. Authorised: Niko's voice is the local Kokoro-82M (Apache-2.0; voice am_fenrir at 1.05,
  environment `C:\he-tools\kokoro\env312`) through
  `Tools/HumanEye/generate_kokoro_vo.py` (owner's choice, 22 October 2026; pronunciation overrides in its PRONOUNCE map;
  supplemental lines in `Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv`, clips `Audio/VO/Extra/`, spoken by key from the dialogue
  database's `extra` set and never ledger cues); also the owner's ElevenLabs account through
  `Tools/HumanEye/generate_elevenlabs_vo.py` (key only from the ELEVENLABS_API_KEY user environment variable, never in
  files or logs) and the local `Tools/HumanEye/generate_tts.ps1` (superseded Windows voice).
- Do not put the apple image on the retina model; the retinal image is a panel popup.
- Do not re-import the MisterH closed-eye surface (superseded; it would hide the cross-section eye).
- Figma: the connected account is Starter/View (about 20 MCP calls a month); the design spec lives in
  `Docs/HumanEye/UI_STYLE_GUIDE.md` until a seat with edit access is available.
