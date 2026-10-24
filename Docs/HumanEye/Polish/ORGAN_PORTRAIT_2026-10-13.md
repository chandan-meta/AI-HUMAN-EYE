# Organ portrait opening — 13 October 2026

This updates the earlier centered opening to the owner's new brief: keep the full-body title picture, move into a head-and-shoulders close-up as muscle appears, fade the torso below the shoulders, reveal skin → muscles → bone → organs including the eyes, and add particles at each transition.

## Presentation

The original centered human transform is preserved. The existing dolly moves closer over 14–17 seconds, then holds the head and shoulders through the muscle, bone and organ beats. The camera movement enlarges the subject without changing the alignment of the layers or detached eye rigs.

- Skin → muscle: 17–18.6 seconds, using Rack 02.
- Muscle → bone: 20–21.6 seconds, using Rack 01.
- Bone → organs: 23–24.2 seconds, using Rack 04 with the existing brain and eyes.
- Organ hold: 24.2–25.5 seconds; eye close-up then extraction at 26.5–29.5 seconds.
- Niko and the first lesson shot follow; total opening remains 32.5 seconds.

The shoulder fade is independent of the layer wipe. It blends from opaque shoulders into transparency over 24 cm below the shoulder boundary; the same mask controls colour, depth, shadows and normals. The mask is disabled by default and cleared on reset, so it does not affect the later lessons. Wipes travel through the visible portrait rather than spending most of their time below the camera.

One saved, bounded particle system emits a small warm burst at each of the four transitions. It pauses with the cinematic and is cleared by Skip, menu, restart and the normal ending. The 7.5-second reduced-motion version includes a still of the organ layer and follows the existing particle-free accessibility behavior.

## Rack 04 inspection and integration

Rack 04 contains 30 static organ meshes plus cabinet/glass. It has lungs, trachea, mouth/teeth, liver, stomach, intestines, kidneys/ureters, adrenals, thymus and spleen. It contains no eyes, brain, skull or external head. The presentation therefore retains the existing project eyes/brain instead of inventing missing source geometry.

`Tools/HumanEye/export_rack_organs.py` removes the cabinet, corrects the 105-degree heading, combines the anatomy, normalizes UV channels, packs embedded diffuse textures into one 2048 atlas, and reduces 261,689 source triangles to 107,507. The original supplied FBX is unchanged. Blender reimport checks passed for one mesh, one material, one UV channel and five registration markers; front/side renders are under `Logs/RackAudit/`.

The organ mesh has neither feet nor crown. Its inferred `BodyBase`/`BodyTop` markers provide the body frame for fitting; using the mouth-to-pelvis mesh bounds as whole-body bounds would stretch the anatomy. This is presentation registration, not a medically validated shared rig.

## Changed files

Runtime: `Stage.Opening.cs`, `Stage.Scene02.cs`, `AnatomyRevealController.cs`, `HumanDisplay.cs`, `AnatomyPart.cs`, and `Shaders/Anatomy.shader`. Authoring: `Builder.Opening.cs`, `Validator.cs`, and the saved `HumanEye_Observatory.unity` scene. Existing particle/material helpers and Cinemachine dolly are reused.

New assets: `ThirdParty/UserModels/RackAnatomy/Organs.fbx`, atlas/import report, `Generated/Meshes/RackOrgans_Fitted.asset`, `Generated/Materials/Rack_Organs.mat`, and `IntroAnatomyTransition.mat`, with Unity metadata. Tests: existing `CinematicTests.cs` and `CinematicControlTests.cs`. Credits, asset register, operating guide and handoff are updated. Paths without a prefix above are under `Assets/HumanEye/`.

The pre-change disk backup is `D:\AI unity project\_backups\AI-HUMAN-EYE_2026-10-13_pre-organ-closeup`. The additive editor command remains **Tools → Human Eye → Authoring → Apply centered anatomy opening (keeps scene edits)**. The owner's open Editor is left running; verification uses the sandbox project.

## Verification

- **Focused PlayMode:** all three cinematic cases passed: portrait/layer/organ/particle checks, pause/settings/repeated skip, and the later eye-to-brain cinematic. All four bursts were observed live; Pause held particle simulation time; Skip cleared organs, masks and particles.
- **Validation:** 72 passed, zero validator warnings/errors, including serialized references and missing scripts.
- **Final Windows player:** build succeeded with zero errors, 446.7 MB. Visible-player intro checks passed at 1600×900 and 1024×768; the 1600×1000 reduced-motion version passed with the added organ still.
- **Full module:** the final player completed all 10chapters, 65 spoken cues and 13 learner-evidence events in 117.44 seconds under the accelerated walkthrough. Zero walkthrough failures, zero pointer-audit failures, and no audited controls outside the viewport or below minimum size.
- **Motion/visual review:** a real-time H.264 recording confirms the full-body-to-portrait move, skin/muscle/bone/organ order, soft shoulder fade, particles and continuous eye extraction. Visual review caught depth-of-field blurring transparent anatomy; it is now disabled during the portrait reveals and retained for eye extraction. Final 16:9 and 4:3 images keep the anatomy sharp.
- **Performance on this desktop:** the recorded intro averaged 59.7 fps, 16.7 ms main thread and 2.74 ms GPU; worst frame 88 ms, peak system memory 584 MB, average 566 draw calls/84 SetPass calls, and 1,817 average GC bytes/frame. Recording/readback/audit overhead is included. Full-walkthrough chapter averages were 55.9–60.0fps, with screenshot hitches up to 87 ms; this is not a mobile or controlled before/after benchmark.
- **Copy-back:** the owner's disk scene was checked against its pre-change hash before replacement. Eight verified scene/generated-asset/report files were copied from the sandbox (`Logs/organ-copied-assets.json`); the existing centered human pose is unchanged. The open Editor was not closed or controlled.

Preview and QA evidence: `Builds/Checks/organ-portrait-20261013/`. Runnable Development player: `Builds/HumanEye/Windows-OrganPortrait-20261013/HumanEye.exe`. If the open Editor reports that the scene changed on disk, reload the disk version to see the saved organ/particle bindings; the additive authoring command is also available.

## Limits

Rack 04 has no eye or brain geometry and no source rig; the existing project anatomy completes that view. Supplied-asset licence status is recorded in the existing notices. Physical Android/tablet and WebGL are not tested in this change. The existing Unity 6000.3.0 light-probe load warning remains.
