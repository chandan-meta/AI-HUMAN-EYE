# Intro pass — sliding gate, intro dolly, Niko entry, intro-time interface (6 October 2026)

Branch `claude/wow-moments`, on top of the presentation redesign (`WOW_REDESIGN_2026-10-06.md`). Scope of this pass, as briefed: the UX/UI foundation, first-screen behaviour, the sliding gate, the intro camera and the character-entry synchronisation. Nothing else in the application was redesigned.

## What changed

**Entrance order (fixed, code-driven, `Stage.Scene02.cs`).** Initial corridor view (fade from black, slow 2 m drift) → hold on the closed gate → the floor bolt retracts (clunk) → short mechanical pause → the two gate halves slide apart (riser; clunk when fully open) → 0.8 s pause with the gate open → the camera moves in through the gate while the room lights rise and the display spot strikes the subject → the camera rests inside → **only then** Niko flies in through the open gate, passes the camera and settles at his anchor beside the subject → the camera follows and settles on the first lesson shot (`S02_NikoMedium`, pose and lens) → intro ends → the top bar and the right-hand column fade in (~0.6 s) and the lens shift eases in with them. Total 24.6 s; Skip jumps to the finished state; reduced motion shows a 1.5 s still with Niko in place.

| Beat | Time (s) |
|---|---|
| Initial view, drift toward the gate | 0 – 4.5 |
| Bolt retracts | 5.0 – 6.15 |
| Halves slide apart | 6.6 – 9.6 |
| Pause, gate fully open | 9.6 – 10.4 |
| Camera moves through the gate | 10.4 – 16.0 |
| Niko enters (after full open) | 16.4 – 21.4 |
| Camera settles on the first lesson shot | 17.5 – 24.0 |
| Intro ends, interface fades in | 24.6 |

**Gate (`LabEntrance.cs`, `Editor/IntroPatch.cs`).** The room's single lifting panel is split at its centre line into two halves (`GateLeafL` / `GateLeafR`, saved meshes in `Generated/Meshes`, interpolated normals/uvs, an edge plate closes each cut) that slide sideways into the walls by half the opening plus 15 cm. The floor bolt retracts first. The room's door nodes are static-batched in a player build (moving them has no visible effect), so all three leaves are baked into world-space, non-static objects and the authored nodes are left inactive. Each stage uses SmoothStep easing; no hinge rotation anywhere. A lamp over the gate (`Gate lamp`, outside the room-light ramp) makes the closed gate readable while the room is still dark.

**Camera (`S02_IntroDolly`).** One Cinemachine camera moved by code: every segment eases in and out, no segment starts before the previous beat has finished, and the final pose/lens equals the authored `S02_NikoMedium` so the cut at the end is invisible. The lens is aspect-fitted (`CameraDirector.Fitted`) like every authored shot. `S02_NikoMedium` itself is re-aimed so Niko sits left of centre: at 4:3 the column no longer covers his wave.

**Interface (`Hud.cs`, Canvas prefab).** New `intro-overlay`: a slim strip at the bottom edge with a small caption on the left and the Skip button on the right; nothing else is shown during the intro (top bar, column, sound button hidden). The top bar and the column are CanvasGroups and fade in after the intro (`Hud.RevealSpeed`). `ViewportShift.Weight` keeps the picture composed for the full frame while the column is hidden and eases the shift in as the column arrives.

**Content.** The `nikoAppear` step is gone (Niko is already in the lab when the intro ends); he waves on his first line (`Do("niko", "Wave")`).

**Project memory.** `CLAUDE.md` at the repository root is the permanent operating guide (vision, UX/UI principles, flow/camera rules, performance, locations, conventions, never-repeat list). `Docs/HumanEye/UI_STYLE_GUIDE.md` gains the UX/UI system (structure, the never-cover rule, cinematic-moment rule, panels, button states, typography, responsive rules, motion).

## Verification (sandbox copy, Unity 6000.3.0f1, hidden-window Windows development player)

Final round on the committed sources (`C:\he-sandbox\logs`, 6 October 2026, 16:27–16:40):

| Check | Result |
|---|---|
| Intro patch (`IntroPatch.Apply`, batch) | applied; halves 332 / 302 vertices, bolt 1296, travel 2.38 m |
| Validator | 70 passed · 0 warnings · 0 errors |
| EditMode tests | 24 / 24 |
| PlayMode tests (`CinematicTests`: three skip beats, Niko present after the intro, replay, gate reset, scan close-up) | 2 / 2 |
| Windows development build | success |
| Intro-only runs, real speed, 1920×1080 / 1600×900 / 1366×768 / 1024×768 | gate = 1, Niko present, shot = `S02_NikoMedium` at the end; 0 pointer failures, 0 small or out-of-window controls |
| Full walkthrough 1600×900 (stress, 6×) | 10 / 10 chapters, 66 / 66 cues, 35 PASS / 0 FAIL, intro skip exercised (ends on `S02_NikoMedium`, key light restored), 0 exceptions |
| Full walkthrough 1024×768 (reduced motion, 6×) | 10 / 10 chapters, 66 / 66 cues, 15 PASS / 0 FAIL, 0 exceptions; the reduced-motion intro is the 1.5 s still with Niko in place |

Captures: `Captures/intro-2026-10-06/` (17 frames with an index). Review passes that led to fixes: (1) the bolt never moved because the room's door nodes are static-batched → all leaves baked into non-static objects; (2) the closed gate was unreadable in the dark corridor → gate lamp; (3) at 4:3 the column covered Niko's wave → `S02_NikoMedium` re-aimed; (4) the follow segment turned away from Niko for ~3 s → the camera now tracks him until it converges on the authored look point; (5) a first-frame pointer audit hit the not-yet-laid-out overlay → the audit waits two frames; (6) lab-tool buttons were 44 px → 48 px.

Known, pre-existing and out of scope: the Explore part-list buttons are 46 px tall (audit threshold 47.5) — unchanged from the presentation redesign.

## Not done / pending

- **Figma.** The Figma connector is not authorised in this environment, so the new overlay/reveal states are documented in the style guide but not mirrored into the Figma file. Authorise the connector and the frames can be added.
- **WebGL.** The WebGL module is not installed; Windows development builds at web-typical resolutions stand in. Browser-specific behaviour (audio unlock, touch on tablets) is unverified here.
