# Reference pass — Human Brain camera choreography in the Human Eye module (8 October 2026)

Branch `claude/wow-moments`. Brief: recreate the Human Brain reference's intro camera path, environmental reveal, layered
anatomy transition, organ extraction and hero presentation for the eye, then use the same language for an eye → optic
nerve → brain cinematic. Shot analysis and the implementation mapping: [REFERENCE_SHOTLIST_2026-10-08](REFERENCE_SHOTLIST_2026-10-08.md).

## What changed

**Opening (Scene 2, `Stage.Scene02.cs`)** — rewritten on the reference clock τ (video seconds 6 → 97). One code-driven
dolly `S02_IntroDolly`: corridor → cut to a low angle on the gate → cut: the gate lifts (bolt first, both baked halves
rise together; sparks and a dust stream through the opening) → push through the door while the lab lights come up → long
dolly to the subject (turned to face the gate for the opening) → full body → face close-up (decelerating push, FOV
constant) → skin → muscle → bone → glass wipes (top-down fronts with a warm edge line, stopping at the collar seam) with
the orbicularis oculi glowing, the eyes shrinking to anatomical size in their orbits, then the extra-ocular muscles, optic
nerves and chiasm fading in through the glass skull → slow orbit toward the selected eye → the eye rises out of its orbit,
grows and floats in the hall under the eye key and rim while the head fades → gold burst, "INSIDE YOUR EYES" typed letter
by letter, subtitle → cut: Niko materialises from a gold vortex → blend to `S02_NikoMedium`. Two profiles on `Stage.timing`
(Inspector): **Production** 29.8 s (every beat's τ span squeezed into its production length through a monotone cubic
time warp, so holds shorten and the path never changes) and **Reference match** 91 s (τ = 6 + t). Skip lands on the
finished state from anywhere (verified from the corridor and from the title); Pause holds everything; reduced motion is
four stills (7.3 s).

**Eye → brain (Scene 9, `Stage.EyeBrain.cs`, new)** — light (warm rays from the apple) reaches the retina; cyan comets
leave along the optic nerve (L01); the eye flies back into its socket in an X-ray head and pulses run from both eyes along
the optic nerves, cross at the chiasm (nasal fibres), continue along the optic tracts to the lateral geniculate nuclei and
out along the optic radiations to the primary visual cortex, each stage labelled as the pulse reaches it, camera rising
over the head (L02); the whole visual system rises out of the head, grows, turns to a lateral view and floats under
"FROM EYE TO BRAIN" while the cortex lights on every arrival (L03); fade to the Scene 9 checkpoint. The ledger lines
stay content steps: `cineBeat` waits for the picture and the cinematic waits for the line, so they never drift.

**Anatomy** — `Tools/HumanEye/export_zanatomy_vision.py` (headless Blender 5.1) exports from Z-Anatomy: real facial and
epicranial muscles (replacing the illustrative shell), the orbicularis oculi as its own part, the six extra-ocular muscles
with levator, trochlea and tendinous ring, optic nerves, chiasm, tracts, LGN, calcarine cortex and script-drawn optic
radiations, with four signal routes (each eye to each hemisphere) written in the eye-marker frame. `Builder.ApplyReference`
fits them to the scan subject by its eye sockets (same registration as the skull) into a "Visual System" that can float
out of the head. The eye got a brown/amber iris (`amber_iris.py`) and stronger fine vessels (`whiten_sclera.py` ×1.7).

**Rendering** — `Anatomy.shader` gained a world-height wipe (`_Wipe`, `_WipeFloor`, `_WipeEdge`, `_WipeColor`) applied
identically in the colour, depth, shadow and depth-normals passes (a wiped layer leaves no shadow or SSAO behind);
`AnatomyPart.SetWipe/ClearWipe/SetColor`. The glass skull uses the clear-medium mode with a bright fresnel rim.

**Sound** — `generate_reference_sfx.py`: gatelift, labreveal, wipe, xray, extract, titlehit, vortex, brainrise (pure
Python, −5 to −12 dBFS peaks). Existing keys reused for the rest; music ducks under lines as before.

**Interface** — the letterbox gained the cinematic title (`intro-title`, `intro-rule`, `intro-subtitle`, typed without the
line moving); narration shows as the letterbox caption during a cinematic; `Hud`, `ShotLook` and `LabNavigation` key off
`Stage.CinematicPlaying`. Spec in `UI_STYLE_GUIDE.md`.

**Tooling** — AutoPilot `-heReferenceTiming`, `-heIntroStep`, `-heCineStep`; `CameraDirector.AspectOverride` so batch
captures are fitted for 16:9; `EditorCapture.Views` (camera blocking without Play). Sandbox round `C:\he-sandbox\round20.ps1`.

## Reference accuracy (pass 1)

Side-by-side sheets of the reference frames against the Reference-match capture at the same τ:
`Captures/reference-2026-10-08/compare_1.jpg`, `compare_2.jpg`. The twelve beats of the brief's sheet line up in order,
framing and motion: corridor dolly, low-angle cut, lift and dust, push into the lab, lab establish, full body, face dolly,
face close-up, muscle, skull, translucent, extraction, floating organ, hero title, companion vortex. Differences that
remain: our lab is darker and cooler than the reference's white dome (room asset); the reference's human has a wider lab
behind him at the full-body beat (our platform is nearer the camera); the brain's extraction rises further than the eye's
because the eye is small — it rises out of the orbit then comes forward to a presentation point 1.1 m in front of the face.

## Verification (sandbox copy `C:\he-sandbox\AI-HUMAN-EYE`, same sources, 8 October)

| Check | Result |
|---|---|
| Compile | 0 errors |
| `Validator.RunBatch` | 70 passed / 0 warnings / 0 errors |
| EditMode | 24 / 24 |
| PlayMode | 3 / 3 (`CinematicTests`: opening checkpoints at the gate, face, muscle, glass skull and title, Skip from the title and from the corridor both reach the finished state; the eye-to-brain cinematic syncs with L02, shows every pathway stage, Skip restores the checkpoint; `WalkthroughTests`) |
| Windows development build | Succeeded, 0 errors, 2 warnings (cloud symbol upload), 502 MB |
| Player intro captures | 1600×900 (0.5 s steps) and 1024×768: letterbox, captions, title, Skip; 34 layout samples at 4:3, 0 small / 0 outside |
| Player Scene 9 capture | 0.7 s steps through the eye-to-brain cinematic, lines L01–L03 in their beats |
| Full walkthrough 1600×900 stress | 10 chapters, 66 cues, 35 PASS / 0 FAIL, intro skip exercised, 0 exceptions, 0 pointer failures, 56–59 fps |
| Full walkthrough 1024×768 reduced motion | 10 chapters, 66 cues, 15 PASS / 0 FAIL, 0 exceptions, 57–59 fps |

Evidence frames: `Captures/reference-2026-10-08/` (selected player frames of both cinematics, the comparison sheets).
Raw runs stay local under `C:\he-sandbox\AI-HUMAN-EYE\Builds\Checks\ref-*`.

## Not done / owner decisions

- The brief's eye image `image(20261008-050928).png` was not on this machine; the eye follows the written description
  (off-white sclera with fine vessels, amber iris, black pupil, wet cornea, extra-ocular muscles, optic nerve stub).
- Niko's voice: the brief asks for a childlike voice. Audition clips with the installed local voices at a raised pitch
  are in `Logs/vo-alt/child-zira` and `child-heera` (four lines each); the shipped narration is unchanged (Microsoft Mark)
  until the owner picks one — regenerating all 66 lines is `generate_tts.ps1 -Voice "Microsoft Zira" -Pitch +24% -Rate 1.07`.
- Figma: the connector is not authorised on this machine; the title spec lives in `UI_STYLE_GUIDE.md`.
- The optic radiations are drawn by the export script (not atlas geometry) — an illustrative approximation, disclosed in
  `THIRD_PARTY_NOTICES.md` §6.
- No physical tablet, OS touch, audio listening or WebGL run (module not installed) — as before.
