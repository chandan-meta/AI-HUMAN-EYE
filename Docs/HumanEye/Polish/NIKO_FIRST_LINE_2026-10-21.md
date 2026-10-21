# Niko appears with his first line (21 October 2026)

**Brief (owner):** "Niko should appear when his first dialog starts; remove him appearing before it; add some particle
effects when he appears."

This replaces two rules from the 15 October brief: Niko on screen in every frame of the opening, and Niko waiting beside
the gate on the start screen.

## What changed

- **Before his first line he is not shown.**
  - `Stage.NikoWaiting` is set by `ShowMenu`, so the start screen has no Niko and no wave at the gate.
  - It is also set by `EnterScene02`: the opening and its replays, including the reduced-motion stills.
  - `Enter`, `IntroEnd` and `NikoFloatingCompanion` respect it, so the companion no longer floats him through the opening.
  - The opening lost what only served him: his gestures, the 30 s `nikoappear` accent, and the closing caption
    "Niko · Your guide to vision".
  - The camera still settles on his medium shot, and his spot is empty until he arrives.
- **His first line brings him in.** `Stage.Update` calls `NikoArrive` as soon as the narrator speaks, whether a lesson
  line or a Talk to Niko answer:
  - After BEGIN, that is `S02_L01`, which starts as the opening ends on his medium shot.
  - After Continue or a chapter picked on the menu, it is that chapter's first line.
- **The arrival.**
  - The `nikoappear` chime plays, and `S02_L01` keeps its Greet gesture.
  - He grows from nothing in 0.55 s with a slight overshoot. The growth is `NikoFloatingCompanion.Grow`, applied over
    wherever he stands or floats, so a half-grown Niko is never sent to the floating corner. When his first line plays in
    a shot that is not his, he appears in his floating corner at once instead of flying in from his spot.
  - A cyan burst plays on the existing `nikoSparkle` system (world space, additive glow), sized to his height and turning
    about his up axis:
    - 120 sparkles spiral from his feet to his crown, one in three white;
    - a ring of 48 sweeps out from his feet;
    - a soft flash sits at his chest.
  - The colours are the module's accent cyan (#26C6F5) and white. The gold `nikoVortex` of the older reference intro is
    not used, because gold is reserved for the neural signal.
  - Reduced motion: no growth and no burst, only the chime.
- **Audits and tests.**
  - The AutoPilot presence audit fails a frame where Niko shows before his first line; from then on it keeps the
    every-frame check. It logs `niko_arrive` with the cue, the shot and where he stands.
  - The intro-only walkthrough now waits for the arrival and captures it (`niko_arrive_0..5`, without the pointer audit while the lesson UI fades in).
  - The PlayMode assertions moved to the new rule: `CinematicTests`, `CinematicControlTests` (which also checks the burst
    and the start screen) and `NikoCompanionTests`.
- **Code only.** No scene change: the owner's open Editor picks it up on recompile. `Stage.menuNikoScale` and the
  legacy `Appear` are gone; the `MenuGate` anchor stays in the scene, unused.

## Verification (sandbox, 21 October 2026)

- **Tests:** PlayMode 8/8, EditMode 27/27, `Validator` 72 passed with no warnings or errors.
- **Intro walkthrough** (`round23.ps1 -Steps intro`):
  - no `presence_FAIL` and no `FAIL` lines;
  - `niko_arrive S02_L01 Scene02 shot=S02_NikoMedium MAIN`.
- **Real-time recording** (`record`): the opening ends on his empty medium shot and the lesson UI fades in. With
  "Hey! I'm Niko…" he grows in out of the cyan flash, sparkles spiral round him for about a second, and the ring sweeps
  out at his feet. Frames and a clip: `Captures/niko-first-line-2026-10-21/`.
- **Web** (development WebGL, fresh GPU Chrome profile, `web_begin_check.mjs --trace`):
  - nothing compiles after BEGIN, and the worst frame across the opening and his arrival is 27 ms;
  - his shaders are programs the start screen already compiles, so the shipped warm-up list (`Resources/ShaderWarmup.txt`)
    needed no change.

  The tool's list again adds `Universal Render Pipeline/Lit` shadow and depth entries that differ only in keywords their
  passes ignore, now six.

## Published (21 October 2026)

- **Build.** Release WebGL `PlayerBuilds.WebGL -heRelease`, 129.5 MB, from the sandbox after a sync. Code commit
  `f7bb31b` on `claude/wow-moments`; site `gh-pages` `36f9118`.
- **Local HTTP**, fresh profile, GPU:
  - no Niko on the start screen or in the opening;
  - he arrives about 32.7 s after BEGIN, with the burst;
  - worst frame after BEGIN 32 ms (26 ms at the arrival), nothing compiled after BEGIN;
  - loader fade and start screen at 17-19 ms per frame.
- **Live** (https://chandan-meta.github.io/AI-HUMAN-EYE/), the same check, headless Chrome on the GPU:
  - the same behaviour;
  - worst frame after BEGIN 38 ms, at the arrival;
  - loader fade at most 24 ms per frame.
- **Console.** Only the known notices: the probe-data warning and URP's three "not supported on this GPU" internal
  shaders.
- **Publishing.** GitHub answered the single 130 MB push with HTTP 408, so the site was uploaded in pieces to a
  temporary branch, then `gh-pages` was moved to the finished commit. `publish_pages.ps1` now does this and checks the
  remote afterwards.
- **The check tool.** `web_begin_check.mjs` now runs headless on the GPU (`--headless=new --enable-gpu
  --use-angle=d3d11`). A visible window that gets minimised throttles the page to about 1 fps and ruins the timing.
