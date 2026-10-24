# Supplied Niko character and Rack 02 muscle maps (14 October 2026)

Two owner requests after the electric breakup pass: replace Niko with the supplied cartoon character
(`C:\Users\ADMIN\Downloads\cartoon+character+3d+model\niko.fbx` + `niko\cartoon_character_3d_model_basecolor.JPEG`)
with proper body and facial animation at every beat, and make the Rack 02 muscle layer look like its source (pale,
fibrous) instead of the saturated flat red seen in the breakup captures.

## The supplied Niko

Inspected with Blender 4.5: a Tripo-generated cartoon character (eyeball head, lab coat, "SEE LEARN EXPLORE TOGETHER"
badge) on a standard Mixamo skeleton (65 bones with fingers, skin weights on 52), 9,597 vertices / 18,880 faces, one
material with a 4096² base colour, no blend shapes, no animation clips, 1.0 unit tall.

`Tools/HumanEye/export_niko.py` (Blender 4.5) prepares it: scales mesh and bones to 1.2 m (the old robot's head sat
0.96 m over its root; the character's eyes now sit at 0.85 m and its crown at 1.2 m, so the existing Niko shots keep
their framing), finds the painted eyes and mouth by sampling the base colour at every vertex (blue irises, red mouth)
and builds two blend shapes: `Blink` (459 vertices, the eye bulges squash to their lid line) and `MouthOpen`
(380 vertices, the lower lip and jaw drop 3 cm). Output: `Assets/HumanEye/ThirdParty/UserModels/Niko/Niko.fbx`,
`Niko_BaseColor.jpg`, `Niko_ImportReport.json`. The source files are untouched.

## Rig and animation

The supplied skeleton and weights are sound (65 joints at the right places, 0 unweighted vertices, at most 4
influences per vertex), so they are kept; what was wrong in the first pass was the control layer: the gesture table's
"R" arm (the viewer's right, Niko's -X side) had been bound to the anatomical right arm, so the arm folded across the
coat, and the minimal-rotation aiming let the bones twist. The control rig was rebuilt (second pass, same day):
two-bone arms solved against a pole vector behind the elbow with the roll kept relative to that plane (no twist),
clavicle lift as an arm rises, finger curls per gesture from the measured palm normal (relaxed, open, point with the
index straight, pinch with index and thumb closing), the arm sides measured from the model at patch time, and five
blend shapes for the face (`Blink`, `MouthOpen`, `MouthWide`, `MouthRound`, `BrowUp`) played as visemes: the jaw
follows the narrator's loudness, the mouth alternates wide / round / neutral on each syllable onset, the brows lift at
the start of speech and every fifth syllable.

No clips exist, so `NikoRig` poses procedurally on the bones (world-space, from the rest pose every frame):

| Beat | What Niko does |
|---|---|
| Appear (after the opening, replay) | Scale-in with overshoot, `Wave` (right arm up, forearm oscillating) |
| Every narrated line | Head and neck turn to `lookTarget` (camera or the structure), nod on the voice level, `MouthOpen` follows the narrator's loudness, blinks every 2.5–5.5 s |
| Explanations of a structure | `Point` at the structure's focus (`gestureTarget`), `Present` (open hand toward it), `Talk` gesture |
| Interactions | `Pinch`, `Snap` (index and thumb close, the existing snap ring at the hand), `RaiseHand` |
| Idle | Breathing (spine), slow weight shift (hips), 6 mm body lift |
| Moves | `MoveTo` (slide) and the quick scale hop, unchanged |

Gesture directions are the old table in character space (-X is Niko's right); arms are solved as two bones with
`Quaternion.FromToRotation` on the upper arm and forearm, so the same code drives any rig with these joints.
`eyeL`/`eyeR`/`smile` remain as proxies under the head so the autopilot's presenter samples and tests keep reading the
blink and talk state.

`Builder.ApplyNikoCharacter` (**Tools > Human Eye > Authoring > Apply supplied Niko character**, the final additive
patch after ApplyGateFaceOpening, idempotent) imports the FBX (no animation, blend shapes on, no materials), replaces
the robot under the same `Niko` root (Stage, presenter, the `HE_Niko` layer, anchors and snap ring keep working), turns
the model so the toes point along the root's forward, stands it on the anchor, gives it a URP Lit material with the
supplied base colour, binds the bones and blend shapes on `NikoRig`, and moves the live portrait camera to the new head
(1.25 m in front of the face, 35°).

## Rack 02 muscle maps

The 13 October export packed the fourteen 1024 px muscle tiles into one 2048 px atlas (504 px each) and dropped the
fourteen supplied normal maps; the v2 pass then tinted the material dark red (.67, .44, .39). `Tools/HumanEye/
export_rack_muscle_maps.py` rebuilds `Muscles_Albedo.png` at 4096 px and adds `Muscles_Normal.png` with the same tile
layout and edge ratio (verified against the old atlas, max tile mean difference 0.0017), so the saved fitted mesh's UVs
stay valid. `ApplyGateFaceOpening.MuscleLook` imports both at 4096 (normal map type), assigns the normal map and sets a
near-white tint with moderate gloss, so the pale fibrous muscle reads as in the source viewer.
15 October 2026: the pale look was partly an overdraw bug (the layer blended without depth writes under the shoulder fade); see `MUSCLE_HEAD_DEPTH_2026-10-15.md` for the fix and the tint now used.

## Verification (sandbox round 23h; the owner's open Editor was not operated)

- Patches: `ApplyGateFaceOpening` (with `MuscleLook`) then `ApplyNikoCharacter`: height 1.200 m, head joint 0.65 m over
  the root, 72 transforms, 2 blend shapes. The first attempt imported the FBX with animation type None, which flattens
  a skinned model to a static mesh; Generic with no clips keeps the bones and the skinned renderer.
- Validator 72 passed / 0 warnings / 0 errors. EditMode 25/25. PlayMode 7/7 (including the live-presenter test:
  replay and answer audio keep the portrait, pause keeps it, menu clears it). Cinematic regressions 2/2.
- Windows development build succeeded. Rendered player openings at 1600×900, 1024×768 and 1600×1000 reduced motion:
  75 presenter frames / 0 failures and 0 pointer failures each. Full walkthroughs: 1600×900 stress 35 PASS / 0 FAIL,
  3,051 presenter frames / 0 failures; 1024×768 reduced 15 PASS / 0 FAIL, 2,491 presenter frames / 0 failures;
  10 chapters, 65 cues, 0 exceptions. Lesson scenes ran at 57–58 fps in the earlier identical runs; the intro-only
  captures ran at 41–58 fps while writing PNGs.
- Presenter audit (`-hePresenterAudit`, chapters 1–2 at real speed): 78 samples at 2 %, 50 % and 94 % of every line,
  78 visible (main stage in chapter 1, live portrait in chapter 2), 5,033 narrated frames / 0 failures;
  [samples](Captures/niko-2026-10-14/presenter-samples-lines.png), log `presenter-audit-chapters-1-2.log`.
- Second pass (rebuilt control rig and visemes, round 23j–k): patch height 1.200 m, 5 blend shapes; validator 72/0/0;
  cinematic 2/2; EditMode 25/25; PlayMode 7/7; build; presenter audit 75/75 samples visible (gestures logged: Point 16,
  Present 15, Talk 2, Snap 1, Idle 41), 4,863 narrated frames / 0 failures; stress walk 35 PASS / 0 FAIL, 3,013
  presenter frames / 0 failures; reduced 1024 walk 15 PASS / 0 FAIL, 2,440 / 0; 1024 and reduced-motion openings 75/0.
  Frames: [wave, present, idle](Captures/niko-2026-10-14/rig2-wave-present-idle.png),
  [both arms and mouth shapes mid-line](Captures/niko-2026-10-14/rig2-both-arms-visemes.png),
  [the five blend shapes in Blender](Captures/niko-2026-10-14/blend-shapes-visemes-blender.png).
- Evidence: [Niko's arrival and the first lesson shot](Captures/niko-2026-10-14/niko-arrival-1600.png),
  [chapter 2 shots with the live portrait](Captures/niko-2026-10-14/chapter2-shots-with-live-portrait.png),
  [portrait crops](Captures/niko-2026-10-14/live-portrait-crops.png),
  [source model views](Captures/niko-2026-10-14/source-model-views.png),
  [Blink and MouthOpen in Blender](Captures/niko-2026-10-14/blend-shapes-blender.png),
  [muscle hold before the tint tweak](Captures/niko-2026-10-14/muscle-hold-before-tint-tweak.png) and
  [final](Captures/niko-2026-10-14/muscle-hold-final-1600.png). Test XML, walkthrough logs and the copy-back plan:
  `Builds/Checks/niko-20261014/`.
- Copied back to the owner's project after the round: the scene, `Rack_Muscles.mat`, `Niko_Body.mat`, and the
  changed import metas (Niko FBX as Generic, muscle atlases at 4096 / normal map). The pre-change scene hash matched
  the version copied back after the breakup pass.

## Limitations

- No lip sync phonemes: the mouth opens with the narrator's loudness, the head nods, the hands gesture.
- Finger poses are approximate (index and thumb close on each other for Pinch/Snap); the palm twist is whatever the
  minimal rotation gives.
- `MoveTo` still slides the standing character between anchors (1.2 s, used once at the ending); a walk cycle would
  need clips or a procedural gait.
- 4096 px muscle atlases: about 42 MB of compressed texture on desktop; reduce `maxTextureSize` per platform if tablets
  struggle.
