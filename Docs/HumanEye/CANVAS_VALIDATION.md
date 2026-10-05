# Canvas authoring validation — 3 October 2026

The live HUD is now native Unity Canvas/UI. All 526 Canvas RectTransforms are stored in `Assets/HumanEye/UI/ObservatoryCanvas.prefab`, instantiated in the saved launch scene under HUD. The 770 GameObjects in the pre-conversion scene were all retained; none were removed. A Menu Eye Point was added for editable menu staging.

## Checks completed

- Unity compilation and scene/prefab save succeeded.
- Edit Mode: 22 passed, 0 failed (`Logs/authoring/editmode.xml`). Includes saved Canvas presence, prefab connection, assigned/unique bindings, pre-authored dynamic-content slots and rename-safe references.
- Play Mode: full 10-chapter / 66-cue walkthrough passed (`Logs/authoring/playmode.xml`). The initial conversion's 518 UI instance IDs were unchanged before/after gameplay. Eight saved scrollbar objects were then added in the editor.
- Final Canvas runtime: full 10 chapters / 66 cues, 21 interaction checks passed, 0 failures, no small/outside button findings at 1024×768 (`Builds/Checks/canvas-final-1024/walkthrough.log`). No rendering errors or runtime exceptions in `Logs/authoring/final-1024.log`.
- Windows build succeeded, 0 errors, 1 symbol-upload warning (`Logs/authoring/windows-authored.log`). This final build also includes the dock-anchor and Replay-label corrections found during visual inspection.
- Exact dialogue ledger and chapter content source remain unchanged.
- No `new GameObject`, `Instantiate`, or runtime UI element construction remains in the production UI scripts. Variable lists reuse saved slots.

Screens inspected: Start, Settings, Help, Review, part exploration, quiz and later-scene UI. Final layout follow-up: `Builds/Checks/canvas-layout-1024/`. These are real camera/Canvas captures; automation exercises native submit events, not physical mouse/touch.

The first Windows capture attempt failed because required rendering resources were absent after switching from the Android context. Rebuilding with `-buildTarget Win64` resolved it; black captures under `canvas-first-1024` are invalid and superseded. Use an explicit build target for batch builds. No rendering features were disabled to hide the failure.

The Android APK from the preceding polish task has not been rebuilt for this Canvas conversion. Physical touch, phone/tablet safe areas and an exhaustive new visual acceptance pass remain unverified. Earlier Figma layouts and polish captures describe the pre-conversion UI.

See [Scene authoring](SCENE_AUTHORING.md) for editing instructions. Full regeneration replaces manual changes; ordinary player builds use the saved scene.
