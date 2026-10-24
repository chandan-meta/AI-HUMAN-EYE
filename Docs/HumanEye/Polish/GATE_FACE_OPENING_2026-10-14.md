# Gate-to-face opening

The owner's latest direction replaces the previous wide-room/curved approach and waist-up reveal. The camera begins
behind the closed gate, waits for it to open, then travels straight toward a shoulder/head portrait. A two-second face
hold includes a blink before the skin becomes muscle, then organs. There is no skeleton beat in this opening.

| Intro time | Picture and camera | Character and effects |
|---|---|---|
| 0–0.5 s | Fixed camera four metres behind the gate plane | Gate closed; short fade from black |
| 0.5–3.5 s | Camera holds while the existing gate lifts | Existing gate sound and accent lights |
| 3.5–15 s | Straight dolly at a fixed height and lens; eased logarithmic distance slows the apparent approach | Character faces the gate; title appears during the approach |
| 15–17 s | Shoulder/head composition, completely still | Original skin; real eyelid blink around 15.7–16.1 s; no transition particles yet |
| 17–18.6 s | Same portrait | Skin-to-muscle sweep, cyan/gold wisps, warm burst and reveal swish |
| 18.6–22 s | Still muscle portrait | Calm study hold |
| 22–23.6 s | Same portrait | Muscle-to-organ sweep, wisps and x-ray accent |
| 23.6–25.5 s | Still organ/brain portrait | Faint skin context; supplied Rack 06 brain and optic pathway |
| 25.5–29.5 s | Approach and extraction of the selected eye | Existing extraction and cleanup |
| 29.5–32.5 s | Ease into Niko's lesson shot | Original narration begins after the opening |

The reduced-motion version uses still skin, muscle, organ and extracted-eye views with the existing fades. Pause
freezes the intro clock; skip, replay, chapter navigation and menu return use the existing cleanup paths.

## Implementation

- `Builder.ApplyGateFaceOpening` is the final additive editor patch, after `ApplyReferenceV2`. It preserves the owner's
  scene transforms and existing gameplay. Do not run the full scene generator.
- `Stage.Opening.cs` drives the gate, straight approach, two-second hold, blink, revised captions and two anatomy wipes.
- `AnatomyRevealController` now transitions directly from muscle to organs, masking below the shoulders.
- `HumanDisplay.blinkingSkin` uses the supplied Rocketbox `AU_45_Blink` facial shape. The exporter bakes the existing
  relaxed pose and retains only Basis/Blink in a separate `Rocketbox_Blink.fbx` derivative. It changes 54 source vertices;
  the existing static source FBX and textures are retained. The saved `Rocketbox_Blink` mesh uses the original materials.
- The close-up retains the original lips/chin and mouth geometry. `Rocketbox_head` uses `_AboveBodyFade` so the solid
  face keeps its depth prepass while the shoulders fade; otherwise the internal mouth draws through the skin. The patch
  asserts that all head-material vertices lie above the fade. Partial organ-view opacity still disables depth writes.
  The portrait key and supplied skin/shirt material tints are softened for the closer camera.
- The existing transition particle systems gain gentle noise, orbital motion and short tapered trails. Their saved
  limits are 260 scan particles and 80 burst particles; face-hold particles remain off. No package or online asset was added.

Source derivative: `Assets/HumanEye/ThirdParty/UserModels/Rocketbox/Rocketbox_Blink.fbx`.
Re-export with Blender: `--background --factory-startup --python Tools/HumanEye/export_rocketbox_display.py -- --blink`.
The source is the already credited Microsoft Rocketbox Business_Male_06 in the owner's `Human.unitypackage`.
Existing licensing notices continue to apply.

Backup before this change: `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-14_pre-gate-face`.
Verification runs in the existing `C:\he-sandbox\AI-HUMAN-EYE` checkout; the owner's open Editor is not operated.

## Verification and delivery

- Play Mode: **7/7 passed**, including pause/settings/skip/replay and the full accelerated walkthrough: 65 cues,
  13 activity evidence entries, 2,611 narrated frames, zero presenter failures and zero pointer failures.
- Final cinematic regressions after the lip correction: **2/2 passed**. Added checks cover the straight rail,
  stationary portrait, full eyelid closure/reopening, skin-to-muscle-to-organ order, saved particle noise/trails,
  opaque facial depth during the shoulder fade, transparent organ context and cleanup.
- Rendered player checks: 1600×900 opening, 1024×768 opening, and 1600×1000 reduced-motion opening all completed
  with zero pointer/presenter failures. Head/shoulder framing and the repaired mouth were visually inspected.
- Windows development build succeeded with zero errors and two existing build warnings.
- WebGL development build succeeded with zero errors and four warnings (including Unity package UI Toolkit shader
  macro warnings). Delivery: `Builds/HumanEye/WebGL-GateFace-20261014`; Windows:
  `Builds/HumanEye/Windows-GateFace-20261014`. The updated WebGL binary was compiled, not browser replayed.
- [Opening video with audio](../../../Builds/Checks/gate-face-20261014/HumanEye_Gate_Face_Opening.mp4):
  H.264/AAC, 1600×900, 35.3 seconds including menu/lesson handover. Opening choreography itself is 32.5 seconds.
- [Corrected lips](../Captures/gate-face-2026-10-14/face-lips-fixed.png),
  [closed eyelids](../Captures/gate-face-2026-10-14/face-blink.png),
  [muscle wipe and particles](../Captures/gate-face-2026-10-14/muscle-wipe.png),
  [organ portrait](../Captures/gate-face-2026-10-14/organs.png).
- Evidence, test XML and the SHA-256 copyback manifest are in `Builds/Checks/gate-face-20261014`.
  Existing scene/material hashes matched the pre-change backup before copying six owned assets back.

The existing full narrated video predates this opening correction; the accelerated full-flow test was rerun for this
change. Physical tablets were not tested. Capture timings are not a cross-device performance guarantee.
Reload `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` from disk in the owner's Editor before editing so its old
in-memory scene does not overwrite the verified saved changes.
