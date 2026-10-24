# The corridor gate opens the module (15 October 2026)

The owner added a new gate to the lab entrance (`Assets/THE HUMAN EYE/Fbx/gate .fbx`, instance "gate " at the scene
root: a sci-fi double door with "HUMAN EYE" panels and an eye emblem, two leaves `gate 1` / `gate 2`, a frame, a neon floor
and a 25 m corridor piece running outward from it), removed the lab's old lifting door (its baked leaves and bolt are gone
from the saved scene, so the entrance's leaf references were empty) and asked for the opening to start in that corridor,
hold a second, open the gate cinematically, enter and continue as before.

## What changed

- **The gate is the entrance.** `Builder.ApplyCorridorGate` (Tools > Human Eye > Authoring > Apply corridor gate; sandbox
  step `corridor`, after the ANATOMY.blend patch) puts a `LabEntrance` on the "gate " instance: leaves `gate 1` and `gate 2`
  at their saved local poses as `closed`, `open` = each leaf moved outward along the gate's own cross axis by its width plus
  0.1 m (2.9 m, into the frame), no bolt share, the old entrance's accent light carried over; `Stage.entrance` now points at
  it, so the menu closes it, the opening animates it, skip / replay / chapter entry / menu return open or close it exactly
  as the lifting door before. `LabEntrance.SetGate` skips a missing leaf, so a scene whose leaves were deleted cannot stop
  the menu or the opening with an exception.
- **The opening starts in the corridor.** `Stage.BuildOpeningPath` measures the doorway from the entrance's leaves (not the
  retired lifting door's `GateLook` constant) and starts `gateApproach` (8 m) behind it on the same straight rail at eye
  height; the lens opens from `gateFov` (50°, the whole gate with its planters as in the owner's Scene view) and eases to
  the bust lens between 3.5 and 8 s as the camera passes through. Timeline: 0–1 s still on the closed gate ("stop a sec"),
  1–3.5 s the leaves part (SmoothStep, `gatelift` sound moved from 0.6 s to 1 s, the entrance lamp ramps 1–2 s), 3.5 s the
  dolly starts as before and passes the doorway at about 6 s while the lab reveal caption shows; everything from the title
  at 9 s on is unchanged (face hold 15 s, blink, breakup, organs, extraction, Niko at 30 s, 32.5 s total).
- **The lab's walk colliders are restored.** The owner's scene save was made while a different Open Dome FBX was loaded, so
  the added components on the Open Dome instance (25 mesh colliders, 2 box colliders, the switched-off `side_glass20` panel)
  were dropped and the Explore-lab walker passed through the stand. The collider block of the visual-match pass is now
  `Builder.LabWalkColliders`, called by `ApplyCorridorGate` as well (idempotent), so re-running the corridor patch repairs
  them without re-running the visual-match pass.

Not changed: the straight rail, its height and easing, the face hold and layer beats, the sounds other than the first
accent's time, the reduced-motion stills, Niko, narration, gameplay. The legacy reference-match choreography (`IntroFull`)
and the `S02_IntroThreshold` menu shot (at x = −25.5 in the new corridor, facing the gate) are untouched.

## Verification (sandbox round 25, 16:20–16:39; the owner's open Editor was not operated)

| Check | Result |
|---|---|
| Player captures, 1600×900 (`Captures/corridor-gate-2026-10-15/`) | `opening-first-beats-1600.png`: whole gate with planters at 0.4–0.9 s (50° lens), leaves parting from 1.6 s with the lab showing through at 2.4 s, open by 3.2 s, the camera at the doorway at 5 s and inside at 6–7 s, title on the figure at 9.9 s, face hold at 16.8 s unchanged. `first-pass-35deg-lens.png` is the first pass before the lens change (the door overflowed the frame). |
| `CinematicTests` | 2 / 2 (the opening starts behind the closed gate on the corridor side, open by 4.4 s, straight rail, holds, breakup, extraction; skip / replay / menu return close and reopen the gate; Explore lab's walker is stopped by the stand's ring again) |
| Full Play Mode suite | 7 / 7 |
| `Validator.RunBatch` | 72 passed / 0 warnings / 0 errors (a first run flagged the retired lifting door's empty `LabEntrance` leaf slots on "Cinematic Laboratory"; the patch now removes that component) |
| Windows development build | succeeded (16:27) |

The runtime suites ran on the patched scene before the orphaned component's removal; the only difference since is that
unreferenced component. The scene was copied back to the owner's project with `C:\he-sandbox\copy_corridor.py`
(the previous saved scene is under `_backups/AI-HUMAN-EYE_2026-10-15_pre-corridor-gate/`). The owner's open Editor
still holds the earlier scene: reopen `HumanEye_Observatory.unity` (or run **Apply corridor gate** there) before pressing
Play, otherwise the entrance still points at the deleted leaves and nothing moves.

Owner check: after reopening the saved scene in their Editor, the gate animates in Play mode (confirmed 15 October 2026, 16:45).

## Limitations

- The gate opens as sliding leaves (the model's two door halves); if the owner intended a different motion (lift, swing),
  `LabEntrance.open` on the "gate " instance is the only thing to re-author.
- The `S02_IntroThreshold` menu shot and the legacy reference-match choreography were not re-authored for the new gate.
- The first `gatelift` sound now plays at 1 s (was 0.6 s); no new sound was made for the sliding leaves.
