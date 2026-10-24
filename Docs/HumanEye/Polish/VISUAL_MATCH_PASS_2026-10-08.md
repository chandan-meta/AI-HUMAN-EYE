# Visual-match pass — the art boards in the owner's new lab (8 October 2026)

Branch `claude/wow-moments`. Brief: the FINAL MASTER PROMPT with the seven art boards (`REF_01`–`REF_07`), the one-page
sheet and the Visual Reference Bible; keep the owner's new lab environment; daylight outside with a galaxy projection
overhead; relight and bake; bright blue iris, gold neural signals, coral brain, pearl/navy/cyan Niko, navy captions; one
scan only; better lesson cameras; four QA passes. Evidence: `Captures/visual-match-2026-10-08/`.

## Found on arrival

- The owner's new environment (`Assets/THE HUMAN EYE`, the `Open Dome` prefab instance: glass dome lab, the "THE EYE"
  stand with a 2.4 m exhibit eye, info panels, plants, side exhibits) was in the module scene with the old room switched
  off. The saved scene **did not start**: `Stage.roomLights` still pointed at 19 deleted lights of the old room
  (`UnassignedReferenceException` in `Stage.Awake`, then `ShowMenu` failed).
- The module's lesson rig stood where the old platform was: the hero eye 0.3 m inside the new 1.78 m stand, the scan
  subject's legs inside it, Niko's anchors below its top; a glass wall panel stood in the gate's opening.
- No skybox (black windows, flat dark ambient), the owner's bake from the old lighting, a realtime reflection probe.
- The previous pass's amber iris and cyan signals (the boards want a blue iris and gold signals); the Scan Eye button
  and a second scan in Scene 2.

## What changed

**Layout (new patch `Builder.VisualMatch.cs`, Tools > Human Eye > Authoring > Apply visual match).** The lesson rig moves
as one onto the stand: hero/cut point (0, 2.95, 0) — 1.17 m above the stand's top, where the exhibit eye floats — with
the eye-local shots riding the eye; Niko and his anchors stand on the stand's top; the brain display, the apple and its
pedestal (now rising from the ring tier), the display lights and the world lesson shots move with it. The scan subject
stands on the stand's 0.78 m ring tier between the gate and the stand. The glass panel in the gate's opening is
switched off; mesh colliders make the new floor, walls, stands and planters walkable for Explore lab.
`Stage.roomLights` is cleared (the new lab is baked).

**One scan.** The opening is the module's only scan. It now ends with the extracted eye flying to the central display
(the hero point) instead of a presentation point in front of the face; the lab's exhibit eye (`Stage.displayEye`,
made fully dynamic) folds into the stand during the lab dolly, so the face and scan close-ups have a clean background,
and the learner's eye takes the empty display. Niko materialises beside it, framed together (REF_01). Scene 2 continues
straight to Explore Eye: L01–L04, the eye hero, L06–L10, exploration. Removed: the Scan Eye ability (panel, Ask Niko
intent, await, AutoPilot step), the human reveal/scan/pinch/enlarge sequence. `S02_L05` ("Click on the SCAN button.") stays
in the ledger and its audio but is retired (`HumanEyeContent.RetiredCues`; the validator checks it is never spoken);
65 cues are spoken.

**Sky and ceiling, two layers.** A generated daylight panorama (`Tools/HumanEye/generate_daylight_sky.py`: blue gradient,
sun aligned to the key light, cumulus, a hazy skyline and treeline; nothing downloaded) is the skybox, the ambient source
and the reflection fallback. The boards' galaxy is an interior projection: `GalaxyDome.shader` on a shell 1.5 m outside
the roof, cut below the ceiling ring so the side windows never show it, sampling the owner's Poly Haven night sky as a
high-passed starfield over a deep-blue base with the Milky Way as a soft glow, tilted off the texture's pole, slowly
turning.

**Light.** The key light is the sun (mixed, soft shadows); the owner's ceiling area light, the sky through windows and
roof and the cyan light strips are baked (GPU progressive, 12 texels/m, 2048 maps, baked AO, non-directional, indirect
only: 3 lightmaps); 278 light probes (stand, ring tier, subject, lab rings, corridor) light everything that moves;
baked box-projected reflection probes for the lab, the stand and the corridor replace the realtime probe. The scene has
its own lighting settings (`Scenes/HumanEye_Observatory_Lighting.lighting`); the glass is clear ice blue and casts no
shadow; the floor is polished. `Anatomy.shader` now samples the local box-projected probes (positional
`GlossyEnvironmentReflection`, reflection-probe keywords). Grade: exposure −0.1, contrast 14, saturation 12, white
balance −8, bloom 0.3 (threshold 0.95), vignette 0.16, Neutral tonemapping.

**Palette.** Blue iris with a thin amber ring at the pupil (`Tools/HumanEye/blue_iris.py`, sclera regenerated); gold
signal comets (#FFC15E head, amber trail) and paler warm-white light beams; golden fibre optic nerves (eye-to-brain tube
and the Z-Anatomy nerves); coral brain tissue with blue only on the visual cortex; Niko pearl #ECF7FF, deep navy face
screen, cyan #26C6F5 eyes and details; UI panels caption navy #07385D (92 %), controls #092D50, cyan rim.

**Shots (shot bible).** Sclera: a moderately wide 3/4 of the whole globe; cornea: side profile; iris: on axis; aqueous:
the front half of the cutaway; vitreous: the whole cutaway; choroid: the layered back wall; Scene 5 cornea and aqueous:
the front of the section with the rays; accommodation: one fixed side cutaway showing the lens and the retina; closing:
the eye turns its cornea back to the learner beside Niko (new `faceLearner` step), then the eye, the near apple and the
distant tree in one frame. Opening hero: frontal from the stand's front (REF_07), panels at the edges, the title over
the dome.

**Fixes found by the new tests.** `Hud.Refresh` threw when the part list was still fading after a chapter change (Home
during exploration); the batch-editor capture clock is capped so shader compilation stalls can't skip cinematic beats
(capture tool only).

## Four QA passes (sandbox copy `C:\he-sandbox\AI-HUMAN-EYE`, same sources, rounds 21a–x)

1. **Cinematic and flow** — batch Play and player captures of the opening every 0.5 s (1600×900, 1024×768); fixes from
   it: the subject moved off the apple's spot, the exhibit eye folds away before the face push (it loomed behind the
   head), the hero shot became frontal (the title sat on a panel), Niko's entrance frames the eye too. One scan in the
   whole run: the walkthrough reaches Explore Eye with no Scan Eye action (PlayMode test).
2. **Visual side-by-side** — `Captures/visual-match-2026-10-08/compare_REF_0x_*.jpg`: every art-board panel next to the
   final player frame of the same moment, and `before_after.jpg`. Tuned from it: galaxy brightness, grade, panel
   opacity, cone colours, golden nerves, label count on the eye–nerve–brain shot.
3. **Chapter cameras** — a real-time recorded run with a per-cue framing log (structure in view, centre offset, frame
   fraction): the sclera/cornea/iris, aqueous, vitreous, choroid, Scene 5 cornea/aqueous, accommodation and closing
   shots were re-aimed; isolated clear media became readable.
4. **End-to-end** — the final round below; the AutoPilot recorder bug (the player did not quit after a recording) and
   a HUD exception on Home during exploration were found and fixed in this pass.

| Check (final round 21x) | Result |
|---|---|
| Compile | 0 errors |
| `Validator.RunBatch` | 71 passed / 0 warnings / 0 errors (new: retired cues exist and are never spoken) |
| EditMode | 24 / 24 |
| PlayMode | 3 / 3 — opening checkpoints (gate with the exhibit eye, face, muscle, glass skull, the eye on the display, title), Skip from the title and the corridor, the single scan reaching Explore Eye, Explore lab on the new floor stopped by the stand; the eye-to-brain cinematic; the full walkthrough |
| Lightmap bake | GPU (RTX 5060 Ti), 3 × 2048 lightmaps, 278 light probes, 4 baked reflection probes, ~70 s cold |
| Windows development build | Succeeded, 450 MB |
| Player intro 1600×900 | 0.5 s captures; 63 layout samples, 0 small / 0 outside, 0 pointer failures |
| Walkthrough 1600×900 stress | 35 PASS / 0 FAIL; 10 chapters, 65 cues; 0 exceptions; average 53.5–60 fps per chapter |
| Walkthrough 1024×768 reduced motion | 15 PASS / 0 FAIL; 10 chapters, 65 cues; 0 exceptions; 53.3–58.4 fps |
| Recorded run 1280×720 (real time) | 443 s, 10 chapters, 65 cues, 0 exceptions; video + audio (`Builds/Checks/rec-full/walkthrough_full.mp4` in the sandbox) |

Performance: PC quality on the sandbox machine (RTX 5060 Ti) averages 53–60 fps; worst single frames (80–145 ms) are
chapter-entry hitches, as before. The tablet/WebGL profile is the existing Mobile quality level (render scale 0.8, one
shadow cascade, no soft shadows, Forward renderer) plus WebGL texture overrides for the new sky (2048); it was not
measured on a tablet.

## Not done / owner decisions

- **Niko's voice** is still the provisional local TTS (the brief asks for a childlike voice; no licensed childlike local
  voice is installed — the audition sets from the reference pass are in `Logs/vo-alt/`).
- **Figma** was not used: the connector needs authorising (Starter/View seat); the tokens are in `UI_STYLE_GUIDE.md`.
- **WebGL** module not installed: no WebGL build; no physical tablet, touch or listening review.
- **Closer to the boards, not identical:** the boards are saturated illustrations; the lab here is the owner's model
  under a physical bake. Not added: the holographic rings and light column around the eye (REF_07), a spiral galaxy
  (the ceiling projects the Poly Haven night sky), a lateral head view for the pathway beat of the eye-to-brain
  cinematic (it is seen from above the crown).
- **Licences:** the new environment, the exhibit eye and the cross-section eye have no documented licence (release
  gate, `THIRD_PARTY_ASSET_CREDITS.md`). Nothing was downloaded.
- **Git:** nothing committed. `full eye.fbx` (115 MB) and `retina.fbx` (120 MB) exceed GitHub's 100 MB limit and
  `.fbx` is not in `.gitattributes` LFS rules; add `*.fbx` to LFS before committing the environment.
- A file backup of the module, settings, docs and the environment's materials from before this pass is in
  `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-08_pre-visual-match`.
