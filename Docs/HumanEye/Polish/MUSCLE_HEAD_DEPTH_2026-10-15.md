# Muscle head drawn inside-out, and the skybox through the windows (15 October 2026)

Two owner reports on the production opening: the lab windows showed no sky in Play mode although a skybox was assigned in
Lighting, and the Rack 02 muscle layer's head read as a flat white dome with pale eye rings while the neck and shoulders
showed red fibres ([before](Captures/muscle-head-2026-10-15/muscle-head-before.png)).

## Skybox

Lighting had the owner's `THE HUMAN EYE/Texture/Materials/Starlit Galaxy Over Snowy Peaks.mat` (a 2048 cubemap on the
built-in Skybox/Cubemap shader), but the Main Camera cleared to the authored solid colour (0.008, 0.012, 0.025), so the
skybox was never drawn. Fixed in three places: the saved scene's camera clear flag (Skybox), `Builder.Build`'s camera
setup, and `Builder.VisualMatch.Sky`, which now keeps an assigned skybox instead of replacing it with the generated
daylight sky. Because the owner's Editor holds the scene unsaved in memory, `Stage.Awake` also sets the main camera to
Skybox whenever `RenderSettings.skybox` is assigned, so Play mode shows the sky without touching the Inspector.

## Muscle head

Not a texture problem. Ruled out in order: the 4096 atlas tiles and the exported `Muscles.fbx` UVs (Blender renders the
FBX with the atlas correctly: red frontalis, temporalis and orbicularis), the Unity-saved `RackMuscles_Fitted` mesh (dumped
to OBJ with its UV0 and rendered in Blender: correct), triangle winding against normals (100 % agreement), the Unity
texture import (4096 DXT1, 13 mips, tiling 1/offset 0), and an edit-mode render of the Rack renderer alone in the scene
(correct, lit and unlit). A one-shot runtime log of every renderer inside the head at the 18.9 s muscle hold showed the
lesson skull and the Z-Anatomy face parts at alpha 0 as intended, and the Rack layer itself drawn with `_SrcBlend` 5
(alpha blend): `AnatomyPart.Configure` moves a layer to the transparent path when the shoulder body fade is on, and the
transparent path switched ZWrite off. Without depth writes the mesh's own far surfaces draw over the near ones in index
order, so the white inside of the skull cap painted over the frontalis and the eye rings, and the shoulder tops read white.

Fix: a solid layer (alpha ≥ 0.995, no partial wipe) that only carries the body fade still blends but keeps ZWrite on
(`AnatomyPart.Configure`). Faded or partially wiped layers keep ZWrite off as before, so the dissolve and the organ ghost are
unchanged. With the overdraw gone the 14 October near-white tint read too bright, so `ApplyGateFaceOpening.MuscleLook`
and `Rack_Muscles.mat` now use a neutral tint below white (0.72, 0.68, 0.66), wrap 0.18, specular 0.07, gloss 0.28 and
normal strength 1.6; the layer reads like the source model
([Blender, source Rack 02](Captures/muscle-head-2026-10-15/source-rack02-head-blender.png)).

## Verification (sandbox round 23, `sync,build,intro`; the owner's open Editor was not operated)

- [Muscle hold, 18.9 s, 1600×900](Captures/muscle-head-2026-10-15/muscle-hold-fixed-1600.png): red frontalis across the
  forehead, temporalis at the temples, orbicularis rings and lips, white tendons on the crown and chest.
- Skin (16.9 s), the breakup transition (17.9 s), organs with the ghost silhouette (24.2 s) and the eye extraction (27.3 s)
  look as before; the player build and intro walk ran without errors.
- The skybox shows through every window in the sandbox player (start screen through to the hand-over).

All edits were made in the owner's project on D: (scripts, materials, the scene's camera flag) and the sandbox was synced
from it; nothing was copied back and nothing is committed.
