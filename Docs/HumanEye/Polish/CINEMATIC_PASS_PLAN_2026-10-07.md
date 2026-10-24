# Cinematic pass — plan and stream brief (7 October 2026)

The owner's brief (verbatim priorities): 1 fix camera focus during every explanation; 2 a ~20 s cinematic opening
(face → eye → iris → pupil → enter the eye → interior reveal → settle), Niko appears only after it; 3 use the Bright Side
video (eySkNWTI03Q) for the face → eye direction; 4 Niko's entrance intentional; 5–7 the optic-nerve signal clearly
visible, camera follows it, visible inside the brain (ghost/cutaway), synchronised with the VO, with subtle sound;
8 lighting hierarchy (key/fill/rim/practical/accent), eye lit as the hero, brain/nerve lighting guides attention;
9 better SFX; 10 purposeful VFX; 11 transitions (no pops); 12–13 audit and retest. Style: educational + cinematic +
premium; never a sci-fi portal; "visualise more, talk less". The 66 ledger cues and the 10-chapter flow do not change.

Reference study (frames captured from the video): `C:\Users\ADMIN\AppData\Local\Temp\claude\...\scratchpad\reference-study.md`
— darkness around a lit subject; the eyes become the brightest thing before the camera goes to them; lateral slide +
push rather than a straight dolly; a small motivating object (dust / light) leads the camera into the pupil; entry is
shown as light entering, not a portal; scale steps head → eye in socket → eyeball → iris/pupil → inside.

## Hard rules (all streams)

- Work only in `D:\AI unity project\AI-HUMAN-EYE`. Edit only the files you own (matrix below). On shared files use the
  Edit tool (exact replacements), never Write. You cannot run Unity: the orchestrator compiles and runs the sandbox
  rounds. Write code that compiles first time: real API names, `using`s present, no C# features newer than C# 9.
- Unity fake-null: `if (!x)` on UnityEngine.Object; never `?.`/`??` on them.
- No runtime UI creation; no new scene objects at runtime except particles/lines created by the editor patch and
  toggled at runtime. All scene/prefab changes happen in idempotent editor patches (find-or-create by name, set
  absolute values), under `Builder.ApplyCinematic` (Editor/Builder.Cinematic.cs) in the stub file you own.
- Timing: all lesson/cinematic timing uses `Clock.Delta` / `Routine.Tween` / `new Wait(s)` (pause-aware), never
  `Time.deltaTime`, `WaitForSeconds`, `Time.timeScale`. Reduced motion (`Stage.Short`) needs a still equivalent.
- Camera: named CinemachineCameras are the cue sheet (`WowPatch.Shot(stage, name, pos, look, fov)` creates/re-aims
  idempotently; `cams.MoveTo(name, seconds)` blends; `cams.Snap`). Code-driven per-frame camera motion is allowed only
  on `S02_IntroDolly`, `Follow (TRACE)`/`S09_Follow`, `S10_Overview`, `S11_Closing`. Lens FOV written by code must use
  `cams.Fitted(fov)`. New shot names: `S0N_Name`; add a one-line entry to `Validator.Subjects` (Editor/Validator.cs
  ~223-244) using the Edit tool at your assigned anchor line (see matrix).
- Actions: every `Do("key")` in content must be registered in a `Register*` method reached from `Stage.EnsureActions`.
  Cue ids/text are fixed; `Say` steps may be reordered relative to `Shot`/`Do` steps but never between chapters.
- Sounds: `stage.sfx.Play(key, volume)`; keys are exact strings registered in the scene by a patch; unknown keys are
  silent. New keys below are produced by stream S3 — use them freely with `Play`; they will exist.
- Lights: use the fields on `Stage` (Scripts/Stage/Stage.Lighting.cs): `faceKey, eyeKey, eyeRim, eyeFill, brainRim,
  interiorLight` — always `if (stage.eyeKey)`. S4 creates them. Every light you ramp must be restored at the end of
  the beat AND in `Stage.Enter`/`IntroReset` paths you own.
- Performance: no full-screen blur, no per-frame allocations in Update loops, particle counts ≤ 200 per system,
  keep realtime shadow casters few.
- Validator: public UnityEngine.Object fields on HumanEye MonoBehaviours must be assigned by the patch (or the field
  name added to `Validator.allowNull`).
- Tests: PlayMode `CinematicTests` must pass (S1 updates the intro assertions; nobody else edits tests).
- Report honestly: list exactly what you changed and what you could not verify.

## Shared names

Shots: `S02_IntroDolly` (intro, code-driven), `S02_Arrival` (new: lab establishing after the interior, S1),
`S02_NikoMedium` (settle), `S02_Face`, `S02_EyeInSkull`, `S09_Follow` (new tracking camera, S2; code-driven),
`S09_BrainInside` (new: ghosted brain, visual area framed, S2).
Actions (new): `brainGhost` (arg "on"/"off", S2), `signalScene` (arg "on"/"off": lighting + ghost for the nerve sequence,
S2). S5a may add `focusLight` only if needed (register in Stage.Framing.cs).
SFX keys (S3 produces all; others may call them): `irisglow` (1.6 s soft shimmer swell), `approach` (7 s low airy pad,
gentle rise), `pupilenter` (2.4 s airy descending whoosh into a soft low impact), `interior` (3 s airy rise + detail +
warm pad), `lightsweep` (1.8 s soft bright sweep), `nikoappear` (1.1 s warm soft chime + short air), `signaltravel`
(3.6 s soft travelling tone with a gentle 5 Hz pulse, rising), `signalarrive` (1 s soft neural activation chime + low
pad), `layerreveal` (1.3 s low airy rise + soft detail), `softwhoosh` (.7 s very soft transition air), plus cleaner
replacements for the interface keys `select`, `tap`, `confirm`, `deny` (short, clean, soft transients).

## Streams and file ownership

| Stream | Owns (edit) | Must not touch |
|---|---|---|
| S1 intro | Scripts/Stage/Stage.Scene02.cs; Editor/Builder.Intro.cs; Tests/PlayMode/CinematicTests.cs; Validator.Subjects entries inserted after the `["S02_IntroDolly"]` line; HumanEyeContent.cs Scene02 steps only (Edit) | Stage.cs, Stage.Signals.cs, Hud.cs, Builder.Look.cs, shaders |
| S2 nerve | Scripts/Demos/SignalPulses.cs; Scripts/Stage/Stage.Signals.cs; Editor/Builder.Nerve.cs; HumanEyeContent.cs Scene04 + Scene09 steps only (Edit); Validator.Subjects entries inserted after the `["S09_Wide"]` line; Stage.Ending.cs only the journey-replay nerve step if needed | Stage.cs, Stage.Scene02.cs, shaders, Hud.cs |
| S3 sfx | Tools/HumanEye/generate_cinematic_sfx.py (new); Assets/HumanEye/Audio/SFX/fx_*.wav it generates (+ .meta copied from an existing wav meta with a fresh GUID is NOT needed — leave metas to Unity); Editor/Builder.Sfx.cs | everything else |
| S4 lighting | Shaders/Anatomy.shader; Editor/Builder.Lighting.cs; Editor/Builder.Look.cs; Scripts/Stage/Stage.cs (Isolate/RestoreParts/Enter emphasis only, Edit); Scripts/Stage/Stage.Lighting.cs (may add helpers below the fields) | Stage.Scene02.cs, Stage.Signals.cs, Hud.cs, content |
| S5a framing | Scripts/Core/AutoPilot.cs; Scripts/Core/ShotLook.cs; Scripts/Stage/Stage.Framing.cs (new partial: `PartOf(cue)` accessor, any focus helpers); Scripts/Stage/Stage.Optics.cs; Scripts/Stage/Stage.Scene03.cs; Editor/Builder.Framing.cs; HumanEyeContent.cs Scene03/05/06/07/08/10/11 steps only (Edit); Validator.Subjects entries appended before the dictionary's closing `};` | Stage.Scene02.cs, Stage.Signals.cs, Hud.cs, shaders |
| S5b ui-fades | Scripts/UI/Hud.cs; Scripts/UI/WorldLabels.cs; Editor/UiPass.cs | everything else |

## Stream designs

### S1 — the 20 s opening (Stage.Scene02.cs `Intro()` rewrite)

Keep: `IntroPlaying`, `IntroCaption`, `IntroTime`, `SkipIntro`, `IntroReset`, Pause via Clock.Delta, the dolly
camera `S02_IntroDolly` driven per frame, the end state (eye back in the socket, lights restored, `cams.Snap(SettleShot)`,
Niko active at his Intro anchor, `IntroPlaying=false`). The gate stays for the start screen (ShowMenu) and the tests
(`entrance.OpenAmount` 0 at menu, 1 after the intro).
Timeline (seconds on the intro clock; constants as `public const` so tests/AutoPilot can read them):
- Prelude 0–1.6: from the start-screen gate view the gate opens (SetGate 0→1, accents, `clunk1` then `softwhoosh`)
  while the frame fades to black by 1.4 s. (Short: skip the prelude, start at black.)
- F0 1.6–5.6 FACE: fade up on a portrait of the scan subject (head and shoulders, camera ~1.7 m in front, slightly
  camera-right, FOV 32), slow lateral slide right→left ~0.3 m with a gentle push to 1.45 m. Room(.22), displayLight as
  a top key, `faceKey` ramps 0→1 (if present), vignette .35 (IntroProfile override), `approach` pad starts at 2 s,
  fine dust (`Beam Dust` style particles) on. Caption "THE HUMAN EYE" until 4.6 s.
- F1 5.6–9.6 APPROACH: eased curve toward the right eye socket (`human.eyeRight.position`) ending 0.42 m in front of
  the eye at a 3/4 angle (camera-right 14 cm, up 3 cm), FOV 32→26. `eyeKey`/`faceKey` cross-fade so the eye becomes
  the brightest thing; iris emission ramps (`eye.Get("iris").SetFloat("_Emission")` is NOT a float — use the
  material's `_Emission` colour via a small helper on the iris materials, or `SetGlow(.25)` as the warm-up); `irisglow`
  at 8 s. Caption "Where seeing begins" 7–9.5 s.
- F2 9.6–13.6 EYE HERO: push 0.42 → 0.10 m, FOV 26→20; dolly near clip 0.004 (`dolly.Lens.NearClipPlane`); eye-macro
  grade through the IntroProfile (contrast +12, saturation +6); pupil dilates slowly (`eye.SetPupil(.26,false)` with
  `pupilRate` ×2 then restored). The iris should fill ~55–60 % of the frame height by 13.6 s.
- F3 13.6–17.4 INTO THE PUPIL: FOV fixed 22; distance to the iris plane shrinks until the pupil fills the frame
  (camera ≈ 41 mm from the eye centre at the end; the cornea apex is 20.5 mm out, never pass through it); four thin
  additive "Intro ray" lines converge into the pupil (fade in 14–16), "entry" particles stream from the camera into the
  pupil, pupil rim glow (`eye.Get("pupil").SetGlow(.6)`), vignette → .6, exposure 0 → −1.4, chromatic aberration .12
  (IntroProfile overrides), bloom ×2; `pupilenter` at 15.6; `hud.Fade(1, .6)` reaches black at 17.4.
- F4 17.4–21.6 INSIDE THE EYE: behind black: `PlaceEye(heroPoint)`, `eye.SetCutInstant(false)`,
  `Shader.SetGlobalFloat("_HE_InteriorDark", 0)`, `interiorLight` on (ramps .3→1.4 and glides from the lens toward
  the retina = light landing on the retina), "depth dust" particles inside the eye volume on; dolly placed inside the
  eye (eye-local (0.05,0.08,0.55) looking at (0.05,−0.05,−0.85); near clip 0.01, FOV 60) and dollying slowly back to
  (0,0,0.15); fade up 17.4–18.2 with `interior`; `lightsweep` at 18.6; caption "Inside the eye" 18–21; hold 21–21.6.
  Use `eye.transform.TransformPoint` for eye-local positions (the eye rig is at heroPoint, scale .4).
- F5 21.6–25.0 RETURN AND NIKO: fade to black (21.6–22.1, soft low `boom` .3), restore: eye back in its socket
  (`Socket(human.eyeRight, eyeRig)`, other eye too), `_HE_InteriorDark` via `eye.SetCutInstant(false)`, interior light
  off, particles off, Room(1), Exposure(0), key/display lights to lab levels; `cams.Snap("S02_Arrival")` (new shot:
  lab establishing, human on the pedestal, Niko's Intro anchor visible on the left, FOV 40) and fade up 22.1–22.9;
  at 23.0 Niko appears at `Anchor("Intro")`: `niko.Appear(1.0f)` (NikoRig scale-in, waves) + `nikoappear` + a short
  soft particle burst at his position; 23.4–25.0 `cams.MoveTo("S02_NikoMedium", 1.6f)`; at 25.0 the intro ends
  (`IntroPlaying=false`, UI reveals). `IntroLength` ≈ 25.0.
- Skip: jump straight to the end state (as today). Short (reduced motion): fades between four stills — face (2 s),
  eye close-up (2 s), interior (2 s), arrival with Niko (1.5 s) — no camera travel, no particles; ≤ 8 s.
- Everything you ramp must be restored in the end block, in `IntroReset`, and be safe if Pause happens mid-beat.
  Keep `ShowMenu`'s gate-closed start view working. `CinematicTests`: update the intro assertions to the new design
  (IntroPlaying true at .1/3/7 s; after Skip: gate 1, displayLight > 0, introVolume.weight 0, human and Niko active;
  the reduced-motion path must still reach `scan` within 25 s real at FastForward 20). Builder.Intro.cs creates:
  `S02_Arrival`, the "Intro ray" lines (Glow material), the "Entry particles" and "Depth dust" particle systems (Glow
  material, `ProceduralTextures.Soft()` sprite, parented under `stage.observatory` so Stage pauses them), the
  IntroProfile overrides (Vignette, ChromaticAberration, Bloom, DepthOfField Gaussian for the face/eye beats with
  the start set per frame from the camera–eye distance), and Stage fields it needs (declare them in Stage.Scene02.cs).
  Content: Scene02 may drop `Do("niko","Wave")` if `Appear` already waves — keep `Shot("S02_NikoMedium")`.

### S2 — the signal from the retina to the brain

- `SignalPulses`: replace the zig-zag with a comet: per pulse N=16 points from the head back along the path over
  `trailLength` (new field, default .5 world units), `widthCurve` tapering head (0.07) → tail (0), colour gradient
  HDR white-cyan at the head → transparent tail (set on the LineRenderer: `widthCurve`, `colorGradient`), material
  `_Intensity` 4 (bloom threshold is 1.15, so the head blooms); a `public Light headLight` (created by the patch,
  point light, cyan-white, range .9, intensity 6, no shadows) follows the LEAD pulse of the "nerve" path only, off
  when none. Add `public event System.Action<string> Arrived` (fired when a pulse reaches the end) and keep
  `Fire/Emit/StopAll/Lead/PointAt`. Pause/Freeze unchanged (Clock.VizDelta). Speed: keep .75; Trace uses .55.
- `Stage.Signals.cs`: actions `brainGhost` (brain.SetAlpha(.3) / 1; visual area stays 1 and glows on each arrival
  (SetGlow 1 → eases to .5 while emitting) — `signalarrive` at low volume only for the first arrival and for the Trace
  pulse); `signalScene` on: Room(.55), keyLight ×.7, `brainRim` on (if present), `eyeRim` on; off: restore (also in
  `Stage.Enter` via a reset you add in your own partial — do not edit Stage.cs; put the restore in a method called
  from your actions and from `EnterLater` if needed (Stage.Ending.cs is S5a's; coordinate by calling it from the
  `signalScene off` action and at the start of every one of your routines)).
  `FollowPulse` → a real tracking shot on `S09_Follow` (create in Builder.Nerve.cs via WowPatch.Shot): camera =
  lead + offset, where offset rotates from a side view (−z of the path) at the eye to a 3/4 rear view near the brain,
  critically damped (SmoothDamp) with the look point 0.25 m ahead along the path; blend in over .8 s; when the pulse
  enters the brain hand over to `S09_BrainInside` (ghosted brain, visual area framed, FOV 30) with `MoveTo(1.2)`.
  `Trace` (REPLAY): same tracking, one pulse at .55 speed, `signaltravel` at start, `signalarrive` on arrival, visual
  area glow, then return to the previous shot. Short: no tracking, but ghosting, glow and sounds still happen.
- Content: Scene04 — during `S04_L06` ("carries visual information from the retina to the brain") fire one visible
  pulse with the brain ghosted so transport is seen (add `Do("brainGhost","on")`, `Do("signals","nerve",1,true)` style
  steps around L06/L07; L07 (visual area) keeps `S04_BrainClose` but with the ghost on; restore at L08). Scene09 —
  `signalScene on` + `brainGhost on` before L01; L02 with the tracking follow (sync: `Do("followSignal","nerve",1,true)`
  stays async beside `Say`); L03 on `S09_BrainInside`; `signals off`, `brainGhost off`, `signalScene off` before the
  perception shot. Re-aim `S04_NerveMid`/`S04_BrainClose` only if the nerve is not framed.
- Lighting during the sequence must leave the signal the brightest moving thing.

### S3 — sounds

`Tools/HumanEye/generate_cinematic_sfx.py` in the exact style of `generate_ui_sfx.py` (pure Python, 48 kHz, 16-bit
mono, deterministic seeds, `write()` with DC block/fades/peak), one generator per clip above (file names
`fx_<key>.wav`; interface replacements `ui_select2.wav`, `ui_tap2.wav`, `ui_confirm2.wav`, `ui_deny2.wav`). Design
notes: modern, clean, airy, low-level; layered (low impact + airy rise + detail) for the reveals; no harsh clicks;
RMS around −24 to −18 dBFS for beds, peaks ≤ −6 dBFS. Run it (python is available) so the WAVs exist in
`Assets/HumanEye/Audio/SFX/`. `Builder.Sfx.cs` → `CinematicSfx()`: register every key with the WowPatch.Sfx pattern
(`stage.sfx.effects` ToList → LoadAssetAtPath<AudioClip>(Builder.Root + "Audio/SFX/" + file) → RemoveAll(key) → Add
→ ToArray → SetDirty), warning on a missing clip. Map `select/tap/confirm/deny` to the new ui_*2 files. Listen test
is impossible here: report durations/RMS from the generator output.

### S4 — lighting hierarchy and the anatomy shader

1. `Shaders/Anatomy.shader`: make additional lights work under Forward+ and add shadows. Read the installed URP
   package sources first (Library/PackageCache/com.unity.render-pipelines.universal@*/ShaderLibrary/RealtimeLights.hlsl,
   Shadows.hlsl, Input.hlsl; and Shaders/LitForwardPass.hlsl / ShadowCasterPass.hlsl / DepthNormalsPass.hlsl for
   the templates) and use their exact macro names. Required: `#pragma multi_compile _ _ADDITIONAL_LIGHTS` plus the
   cluster keyword (`_CLUSTER_LIGHT_LOOP`; if the package also defines `_FORWARD_PLUS`, include it), an `InputData
   inputData` with `positionWS` and `normalizedScreenSpaceUV` and the `LIGHT_LOOP_BEGIN(count)/LIGHT_LOOP_END`
   macros around the additional-light sum (keep the existing wrap-diffuse/spec maths per light; use the 3-arg
   `GetAdditionalLight(index, positionWS, shadowMask)` so additional-light shadows work with
   `_ADDITIONAL_LIGHT_SHADOWS`); main-light shadows (`_MAIN_LIGHT_SHADOWS`, `_MAIN_LIGHT_SHADOWS_CASCADE`,
   `_SHADOWS_SOFT`, `TransformWorldToShadowCoord`, multiply the main-light diffuse+spec by `light.shadowAttenuation`;
   keep a `_ReceiveShadows` float property default 1 so the intro can disable self-shadowing on the face if it looks
   harsh); a `ShadowCaster` pass (URP template; `clip(_Alpha - .5)` and the `_Reveal` clip so faded/revealing
   structures stop casting; honour `Dilate()` in its vertex function); a `DepthNormals` pass so SSAO sees anatomy.
   Keep every existing property and behaviour (wet film, _Glass, _InteriorDark, pupil remap, _RevealFrom).
2. `Builder.Lighting.cs` → `CinematicLights()`: under `stage.observatory` (the "HE Display Lights" group), find-or-create
   with absolute values: "Face Key" (spot, warm 1,.95,.88, intensity 10, range 7, angle 50/35, soft shadows, aimed at
   the human's head from camera-left-above); "Eye Key" (spot, 1,.97,.92, intensity 14, range 6, angle 40/28, soft
   shadows, from camera-left-above of `stage.heroPoint`, aimed at it — the main catchlight); "Eye Rim" (spot, cool
   .6,.8,1, intensity 9, range 6, angle 45, no shadows, from behind-right-above the hero point); "Eye Fill" (point,
   .8,.85,.95, intensity 1.6, range 4, no shadows, camera-right); "Brain Rim" (spot, cool .55,.75,1, intensity 10,
   range 6, angle 50, no shadows, behind-above the brain centre aimed at it, `enabled=false` by default); "Interior
   Light" (point, warm 1,.9,.75, intensity 0, range 1.2, no shadows, at the hero point, `enabled=false`). Assign the
   Stage fields. Keep these out of `stage.roomLights`. Tune `Builder.Look.cs` numbers (key 1.05 may drop to .85 once
   the spots work; ambient 1.3 is a Skybox multiplier — in Flat mode set `RenderSettings.ambientLight` instead and
   document it). Cast shadows: Key Light, Display Light, Face Key, Eye Key only.
3. Dynamic emphasis (Stage.cs, Edit only): in `Isolate(id)` raise `eyeKey.intensity` ×1.25 and Room(.85); in
   `RestoreParts` and `Enter` restore (store the authored intensity once in `Awake` via a helper in
   Stage.Lighting.cs). Subtle, never theatrical.
4. Review the captures in `C:\he-sandbox\AI-HUMAN-EYE\Builds\Checks\prod-1600\` (PNG) and
   `Docs/HumanEye/Polish/Captures/final-2026-10-07/` to judge flatness; write the intended hierarchy into the file
   header comment.

### S5a — camera focus audit tooling and shot/content fixes

1. `AutoPilot.cs`: after `Log("cue_start", ...)` add `LogFraming(cue)`: structure = `Stage.PartOf(cue)` (add
   `public static string PartOf(string cue) => CuePart.TryGetValue(cue, out var id) ? id : null;` in a new partial
   `Scripts/Stage/Stage.Framing.cs`) with fallback `runner.Focused?.partId`; part = `stage.Part(id)`; focus =
   `stage.eye.FocusOf(part)`; camera = `hud.mainCamera`; log `framing <cue> shot=<name> blending=<bool> part=<id>
   vp=(x,y) visibleCentreDx=<x-(1-colFrac)/2> sizeFrac=<projected bounds height / Screen.height> visible=<alpha>`;
   also re-sample after `Settled()` for the same cue once (`framing_settled`). Keep it allocation-light.
2. Fix the static-audit suspects now (content Edit + Builder.Framing.cs re-aims via `WowPatch.Shot` or eye-local
   `localPosition/localRotation` on existing shots): S04_L02 (retina named on the rear exterior → glow/label the
   retina region or a brief retina-rear shot), S05_L04 (pupil named but faded by isolate aqueous → keep pupil at 1 in
   that beat: use `Do("glow","pupil",.4)` after isolate or a new isolate variant you register in Stage.Framing.cs),
   S06_L02/L03 (the lens change must be visible: keep `S06_LensClose` during the near/distant demos or return before
   the line), S07_L01 (retina faded while named), S08_L05 (phototransduction on a wide shot → a closer shot on one
   cell column + microSignals), S10_L01/L02 (whole-pathway line on an eye medium → move `S10_Journey` before L01 or
   use the overview earlier), S09_L04 (brain named while on the apple; show the perception inset AND a glowing visual
   area or an eye-brain wide). Any shot you add gets a `Validator.Subjects` line.
3. `ShotLook.cs`: per-shot focus target so DoF never blurs the explained structure: focus on the isolated part's
   focus anchor when one exists (Stage exposes `CurrentFocus` from Isolate — add in Stage.Framing.cs by reading the
   last isolated id you track through a small hook: S4 owns Isolate, so instead poll `stage.eye.parts` for the part
   with the highest glow each frame), DoF start = distance to that point + its bounds radius.
4. Transitions in the chapters you own: object pops (`objectAppear`, apple/tree/lamp SetActive) → scale/alpha tweens
   with `softwhoosh`/`layerreveal`; label pops are S5b's.

### S5b — interface transitions

Per the UI map: add CanvasGroups (UiPass.cs, idempotent, `touched.Add`) to activity, ask-panel, end-menu, toast,
btn-ask, subtitle-panel, prompt-banner, info-text, part-list, sim-panel, view-controls, abilities, activity-panel,
info-panel, lens-state, labels-layer; convert every `Show` of those keys in Hud.cs to `Pop` (all occurrences; keep
`Shown()` callers correct with a popTargets-aware helper), journey chips hidden after the fade; `btn-ask` and the
intro-overlay fade with `RevealSpeed`; world labels fade OUT (retiring list in WorldLabels, slot allocation that
skips retiring slots). Fading-out groups must not block raycasts. Accept the one-step layout collapse at the end of
a fade. No new UI objects beyond CanvasGroups.
