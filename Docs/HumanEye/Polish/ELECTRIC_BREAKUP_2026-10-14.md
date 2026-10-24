# Electric breakup transitions (reference clip, 14 October 2026)

The owner's v3 brief (`Human_Eye_Codex_Master_Prompt_v3.txt`, `Human_Eye_Visual_Reference_Book_v3.pdf` pages 1–3) makes the
uploaded clip `20261014-0921-28.5960490.mp4` the authoritative look for the anatomy layer changes: irregular, disconnected
electric-cyan crack filaments spread over the figure, the surface breaks into patches and specks, small blue motes drift
outward and upward, a faint local blue haze lingers; not a flat cross-fade, not a single sweep line, not neon skin, not a
full-screen flash. In the lesson the vanish becomes a hand-over: the next co-registered layer appears where the last one
breaks. The brief's six still references, the Rack 06 brain, the gold neural packets and the live Niko portrait were
delivered by the earlier passes and are unchanged; the owner fixed the camera and asked for it not to be touched, so the
gate-to-face opening, its rail, holds and timings are exactly as in `GATE_FACE_OPENING_2026-10-14.md`. The production
order stays skin → muscle → organs (the owner's 14 October direction omits the skeleton beat); both changes use the effect.

## Timing (unchanged beats, new effect)

| Intro time | Picture | Breakup |
|---|---|---|
| 15–17 s | Still shoulder/head portrait, blink at 15.7–16.1 s | Intact skin, no effect (the clip's 0.0 s) |
| 17.0–17.3 s | Same portrait | Scattered cyan crackle on the highest patches, first motes, haze rising, `crackle` sound |
| 17.3–18.3 s | Same portrait | Crack network joins, skin erodes in islands, muscle shows through the gaps, motes peak |
| 18.3–18.6 s | Same portrait | Last skin islands vanish, crack glow fades to zero on the muscle |
| 18.6–22 s | Still muscle portrait | Motes linger about a second, haze fades, clean study hold |
| 22.0–23.6 s | Same portrait | Same breakup, same seed, muscle → organs/brain/pathway, `crackle` + `xray` |
| 23.6–25.5 s | Still organ portrait | Faint skin outline as before (flat wipe at the floor, no field) |

The breakup field's progress is the existing SmoothStep over each 1.6 s window; its front is padded by the noise amplitude
so progress 0 leaves every layer intact and progress 1 leaves nothing of the departing layer. Pause freezes the intro
clock and the particles; skip, replay, chapter navigation and menu return call `AnatomyRevealController.ResetVisual`,
which zeroes the field, clears the motes and hides the haze.

## Implementation

- `Assets/HumanEye/Shaders/AnatomyDissolve.hlsl` (new): `_HE_Dissolve` (amplitude, cells per metre, seed, filament reach),
  `_HE_DissolveOrigin`, `_HE_DissolveColor`; a 3D Worley (F1/F2) plus two-octave value-noise field measured from the
  subject's root, returning world height plus a seeded offset. Cell borders sit high and open first, cell islands vanish
  last, the grain breaks the borders into jagged filaments.
- `Anatomy.shader`: `WipeAlpha` compares the front against the field when `_HE_Dissolve.x > 0` (ForwardLit, DepthOnly,
  ShadowCaster and DepthNormals share the test, so depth, SSAO and shadows follow the erosion). The forward pass adds the
  cyan emission: the breaking edge, the Voronoi crack network, thin contour filaments and a faint fragment rim, all within
  `filamentReach` of the edge and flickering on the visualisation clock. With the field off the older flat warm line and the
  ReferenceMatch collar-seam reveal behave as before.
- `AnatomyRevealController`: Inspector fields (`breakupAmplitude` .26 m, `breakupFrequency` 16 /m, `breakupSeed` 7,
  `filamentReach` .07 m, colour, `breakupIntensity` 3.5, `moteRate` 1400/s, `hazePeak` .5); sets the globals, pads the
  front, re-parents the mote emitter onto the departing layer's renderer with that layer's mesh as the emitter shape,
  shapes emission as sin(π·progress), stamps every newborn mote's Custom1 with its birth position and the front, drives the
  haze alpha through a property block.
- `DissolveMote.shader` (new): additive soft sprite; motes whose birth stamp lies farther than `_Band` (5 cm, field units)
  from the front at birth collapse behind the near plane, so sparks and shards leave only the breaking patches. Streams:
  Position, Color, UV, Custom1 (particles and trails).
- `Builder.GateFaceOpening.OpeningMagic` (the existing final patch): mesh-shape emitter, 1200 motes, cyan→blue random
  colours, noise, upward drift with a slow orbit, tapered trails, the `IntroDissolveMotes` material, the cyan burst, the
  scan ring removed, the `Anatomy breakup haze` quad (Glow, Soft texture) placed from the saved eye landmarks behind the
  head, the `crackle` sound registered on the audio director.
- `Stage.Opening.cs`: the ring and its front-tracking code are gone; `crackle` plays with `wipe` (17 s) and `xray` (22 s).
- `Tools/HumanEye/generate_reference_sfx.py`: `crackle()` (2.3 s: electrical tickles thickening to a crackle, a rising
  airy digitisation glide, a soft completion thump and glint). Project-generated, no external audio.
- Tests: `CinematicTests` now asserts the field is on with its fixed seed during both transitions and off during the
  holds, the emitter rides the skin then the muscle renderer with a mesh shape and the mote shader, motes and haze are alive
  mid-breakup, and everything is off after skip/replay/menu; it captures `intro-breakup`.

## Verification (sandbox round 23, `C:\he-sandbox\round23.ps1`; the owner's open Editor was not operated)

- Validator 72 passed / 0 warnings / 0 errors. EditMode 25/25. PlayMode 7/7 (pause, settings, repeated skip, replay,
  single scan, eye-to-brain, live presenter, full accelerated walkthrough). Cinematic regressions 2/2 with the new
  breakup assertions. The very first PlayMode run after the shader edit failed the 9–12 s title assertion because the
  cold shader-variant compile stalled the intro clock past the title; the warm re-run and every later run passed.
- Windows development build succeeded. Rendered player openings: 1600×900 (frame every 0.25 s, 130 captures),
  1024×768 and 1600×1000 reduced motion, each with 75 presenter frames / 0 failures and 0 pointer failures.
  Full 1600×900 stress walkthrough: 10 chapters, 65 cues, 35 PASS / 0 FAIL, 3,052 presenter frames / 0 failures,
  57–58 fps average in the lesson scenes. Intro-only captures ran at 46–58 fps while writing PNGs every 0.25 s.
- Before tuning (round 23a) the breakup read as a top-down sweep with white-hot edges; amplitude .26 → .45, cells
  16 → 20, intensity 3.5 → 2.2, saturated cyan, contour filaments reduced, motes 1200 → 3000 budget and 4000/s.
- Evidence: [first round](Captures/breakup-2026-10-14/before-tuning-skin-muscle.png) and
  [muscle → organ](Captures/breakup-2026-10-14/before-tuning-muscle-organ.png); final
  [breakup beats at 17.3 / 17.5 / 17.8 / 18.3 / 22.4 / 22.7 / 23.0 s](Captures/breakup-2026-10-14/breakup-beats-1600.png);
  the cinematic test's [mid-breakup frame](Captures/breakup-2026-10-14/test-intro-breakup.png).
  Test XML, walkthrough logs and the copy-back plan: `Builds/Checks/breakup-20261014/`.
- Copied back to the owner's project after the round: the scene, the recoloured burst material, the two new materials,
  the haze quad mesh and the new asset metas (`copyback-plan.json`); the pre-change scene hash matched the backup.

## Limitations and notes

- The field costs one Worley (27 cells) and two value-noise lookups per fragment in each of four passes while a breakup
  runs (about 3.2 s of the opening); it is a uniform branch and costs nothing otherwise. Not measured on tablets.
- Motes are emitted over the whole departing surface and filtered on the GPU; about 90 % collapse. The budget is 1200
  particles; a CPU-side field would let the controller cull them at birth if the count ever matters.
- Polygon shards and vertex jitter from the brief are not built: the specks, streaks (trails) and crack filaments carry
  the look at portrait distance. Add a shard sub-emitter if a wider shot ever needs them.
- The brief's skin → muscle → skeleton order is superseded by the owner's 14 October direction (no skeleton beat).
