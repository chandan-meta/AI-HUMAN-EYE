# Observatory of Vision — project operating guide

Permanent rules for anyone (human or agent) working on this Unity module. Keep it short; put task details, evidence and
history in `Docs/HumanEye/` instead. When a new instruction conflicts with a rule here, follow the new instruction and
update this file in the same change.

## Vision

A web-first (desktop + tablet, mouse + touch) learning module: Niko, the Vision Investigator Officer, takes a learner
through the human eye in one connected, cinematic exploration. Tone: modern, futuristic, educational, clean, premium.
No excessive sci-fi noise. The 3D subject is the hero; the interface serves it.

## UX / UI principles

- The UI never interrupts or covers the main subject. Persistent controls live at the screen edges (top bar, right-hand
  column); the subject is composed inside the area left of the column (`ViewportShift` lens shift).
- During cinematic moments the full interface is hidden. The intro shows only a slim bottom strip (caption + Skip);
  the main UI fades in afterwards (`Hud.RevealSpeed`, ~0.6 s). Never place the full UI over the intro.
- Hierarchy: subject → current instruction / Niko → supporting tools. Cyan filled = the one required action.
- Every unavailable action explains why (toast or disabled state), never a dead end.
- Design system and component rules: `Docs/HumanEye/UI_STYLE_GUIDE.md`. Figma IDs: `Docs/HumanEye/Polish/FIGMA_STATE.md`.

## Flow, animation and camera rules

- Intro order is fixed: initial view → establishing approach → gate opens (bolt retracts, halves slide apart) → gate
  fully open → camera moves forward → Niko enters → camera settles on the first lesson shot → intro ends → UI appears.
  Niko never enters before the gate is fully open. Timeline constants live in `Stage.Scene02.cs` (`TBolt`, `TGateOpen`,
  `TMoveIn`, `TInside`, `TNiko`, `TSettled`, `IntroLength`).
- Gate = sliding door, no hinged rotation, SmoothStep ease per stage (`LabEntrance.SetGate`).
- Camera moves are eased, motivated, and never drift while nothing happens. Named Cinemachine shots are the cue sheet;
  code-driven motion is allowed only on `S02_IntroDolly` and the authored replay/overview moves.
- Reduced motion / skip camera motion (`Stage.Short`) must always have a short, still equivalent.
- Learning flow (10 chapters, 66 ledger cues, scan → inside → explore, checks only at checkpoints) is approved: do not
  rewrite the state machine or add activities without an explicit brief.

## Performance and platform

- Unity 6000.3.0f1, URP 17.3, Cinemachine 3.1.5, Input System, uGUI saved Canvas prefab. Supported sizes 1024×768 and
  up; test 16:9, 16:10 and 4:3 (1920×1080, 1600×900, 1366×768, 1024×768). Controls ≥ 48 px; no clipping, no overlap.
- Target 60 fps on desktop. Keep realtime lights few; no per-button bloom; no full-screen blur.
- WebGL module is not installed on this machine; Windows development builds stand in for web verification.

## Where things live

- Content (steps, labels, isolation, ending): `Assets/HumanEye/Editor/HumanEyeContent.cs` → `Builder.BuildData()`.
- Intro, scan, Niko per cue: `Assets/HumanEye/Scripts/Stage/Stage.Scene02.cs`; gate: `Scripts/Stage/LabEntrance.cs`.
- HUD binding/state: `Scripts/UI/Hud.cs` (no runtime UI creation; elements are bound by name from the saved Canvas).
- Scene/prefab changes go through idempotent editor patches (`Editor/CinematicPatch.cs`, `WowPatch.cs`,
  `IntroPatch.cs`), menu **Tools > Human Eye > Authoring**. Never run the full `Builder.Build` regeneration.
- Verification: `Validator.RunBatch`, EditMode/PlayMode tests, `AutoPilot` walkthrough flags (`-heAutopilot
  -heOffscreen -hePolish -heFast N -heReduced -heStress -heIntroOnly -heCapture <dir>`).
- Evidence and reports: `Docs/HumanEye/Polish/`; handoff: `HANDOFF.md`.

## Conventions

- Element keys are kebab-case (`btn-skip-intro`, `intro-overlay`); shots are `S0N_Name`; anchors are PascalCase.
- Unity fake-null: never `??` / `?.` on `UnityEngine.Object`; use `if (!x)`.
- Saved meshes/materials go through `Builder.SaveMesh` / `Upsert` so GUIDs stay stable.
- Ledger cue text is authoritative and validated exactly; never paraphrase a cue.

## Never do this again

- Do not close or touch the owner's open Unity Editor; verify in the sandbox copy (`C:\he-sandbox`, see memory notes).
- Do not claim a change, asset, voice or test is done unless it ran; report failures with their output.
- Do not add Continue buttons, Skip on quizzes, mid-sequence questions, or a "rebuild the journey" ending.
- Do not use unlicensed online TTS; the authorised local workflow is `Tools/HumanEye/generate_tts.ps1`.
- Do not put the apple image on the retina model; the retinal image is a panel popup.
