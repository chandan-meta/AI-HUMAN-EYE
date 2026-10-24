# ANATOMY.blend models replace the scan subject's anatomy (15 October 2026)

The owner supplied `D:\AI unity project\models\ANATOMY.blend` (saved by Blender 5.1): a skin character with shorts and
eyelashes, a muscular system, a skeleton, internal organs, blood vessels, a brain and a pair of eyeballs, arranged side
by side along X for inspection. The reference screenshot of that arrangement was not attached; the .blend itself was the
source of truth (every system was identified from its parent empties, materials and geometry, and the overlap was
verified by rendering all systems at one origin before anything was exported). These models now replace the Rocketbox
skin and the Rack 01 / 02 / 04 layers in the production opening and in every later shot that used them (the Scene 9
X-ray head, the legacy reference choreography). The opening's structure, timings (gate → face → blink at 15.7 s →
skin 15–17 → muscle 17–22 → organs 22–25.5 → eye extraction), electric breakup, motes, haze, sounds, narration, gameplay
and the camera rail are unchanged; only the meshes, materials and the registration that depends on them changed.

## What the .blend contains and how it maps

| .blend objects (parent empty → mesh) | Found as | Replaces | Exported as |
|---|---|---|---|
| `HumanSkin` → mesh `9` (27,084 tris, 4096 maps with opacity), `Shorts` → `10`, `Eyelash` → `Eyelash_Eyelash_0` | skin column at x = −146.7 | Rocketbox `Business_Male_06` skin, trousers, `Rocketbox_Blink` | `Anatomy/Skin.fbx`: `Body` (skin + lashes, Blink blend shape) and `Shorts` |
| `Musclespart_1/2/3` → `6`, `5`, `4` (57,784 tris, three 4096 albedo/normal/roughness sets) | muscles column at x = +124.2 | Rack 02 muscle extract (2048 atlas, 35 % decimated) | `Anatomy/Muscles.fbx` (3 material slots) |
| `Skull`, `Spine`, `Ribcage` → `3`, `Pelvis`, `Legs`, `Hands` (85,276 tris) | skeleton column at x = −1.0 | Rack 01 skeleton extract; the Z-Anatomy hollow skull as the X-ray layer | `Anatomy/Skeleton.fbx` (6 slots); the skull slot also becomes `Human_Skull` |
| `Humanlungs` → `1`, `Diafragma` → `0`, `Heart` → `11`, `Liver` → `2`, `Gallbladder`, `Digestivesystem`, `Urinary_system` (79,509 tris) | organ columns at x = −89 and −51.7 | Rack 04 organ extract | `Anatomy/Organs.fbx` (7 slots) |
| `Arteriasmesh` → `7`, `Venasmesh` → `8` (48,330 tris, one `CirculatorySystem` material) | vessel column at x = +61.2 | nothing (no vascular layer existed) | `Anatomy/Vessels.fbx` (1 slot), shown with the organ view |
| `Brain` → `Brain_Brain_0` (29,920 tris, 2048 maps) | organ column | not used: Rack 06 (77,882 tris, lobe-split, per-region maps) is the more detailed brain and the lesson's visual-area split depends on it | measured only: its bounding box (`BrainMin/BrainMax`) places Rack 06 inside the new skull |
| `Eye` → `Eye_Eye_0` (27,604 tris) | organ column | not used: the lesson's cross-section eye sits in the sockets | measured only: sphere fits give the socket centres, the cornea apex gives the socket radius (apex / 1.126, as for Rocketbox) |

Facts established before exporting: the scene is metric but the figure is 170 units tall (centimetres in a metre
scene); every system's parent empty sits at y = −5.393, z ≈ 0 and only x differs (the heart's and eyelashes' empties are
scaled and borrow their column siblings' origin); mirror-symmetry midlines of the symmetric systems agree with their
parents' x to within 0.3 cm, so subtracting each parent's translation puts all systems on one origin (verified in Workbench
renders: eyeballs inside the lid openings, the orbits and the muscle rings; heart, lungs, brain and vessels in place); the
shape keys `target_0/1/2` are breathing and heartbeat morphs (chest, lungs, diaphragm, ribcage, vessels, heart), none a
blink, and are dropped at the Basis; the opacity map is dark only under the eyelash UVs (no skin UV samples below 0.91),
so the lashes stay in the skin mesh under alpha clip; textures are external JPEGs on `E:\` (all present).

## Export: `Tools/HumanEye/export_anatomy_blend.py` (Blender 5.1, background)

- Common frame: metres, feet on z = 0 (Unity y), the skin's bounding-box centre on x = y = 0, facing −Y (Unity −Z);
  every mesh is exported untouched (no decimation, custom normals kept, smooth shading).
- Landmarks in every FBX, so the existing `Builder.RackLayer` fit works unchanged: `EyeSocket_L/R` (eyeball centres;
  IPD 5.79 cm), `EyeFront_L/R` (cornea apex, socket radius 1.65 cm), `FaceDirection`, `BodyBase`, `BodyTop` (1.703 m),
  `BrainMin/BrainMax`.
- Blink: the skin had none, so the export builds one: within each eye's region the lid band (0.05–0.9 radii above the
  centre) drops onto −0.42 radii with 30 % compression, the brow travel fades out by 2 radii, the lower lid rises
  0.12 radii, and lid vertices keep their distance from the eye centre so the lid wraps the globe (584 vertices; the
  lashes ride along). `Stage` drives blend shape 0 exactly as before.
- Textures per source material: albedo JPEG at source size (the skin as RGBA PNG carrying the lash opacity), normal PNG
  (the maps wired through Blender's "Separate Color" are DirectX: their green channel is flipped for Unity), roughness
  inverted to a `_Spec` mask at ≤ 2048. 46 maps, 133 MB.
- `ImportReport.json` records triangles, slot order, landmarks, textures and the blink rule.

## Unity: `Builder.ApplyAnatomyModels` (Tools > Human Eye > Authoring > Apply ANATOMY.blend models)

Additive and idempotent, after the gate-to-face and Niko patches (`round23.ps1 -Steps sync,anatomy,build,intro`):

1. Import settings: static models, readable, blend-shape normals calculated; the skin and muscle maps at 4096 with a
   WebGL override at 2048, the rest at 2048.
2. Skin: `Skin.fbx` is instantiated under the same `Human Display` root as `Anatomy Skin` (face −Z, left +X, feet on the
   root, bounds centred like the Rocketbox figure); the sockets `Socket (other eye)` / `Socket (selected eye)` move to the
   new eyeballs and take the new radius; `headTop`, `headY` and `neckY` (the collar line keeps the Rocketbox offset of
   0.21 m under the eyes) follow; the Rocketbox figure and the muscle shell derived from it are removed.
3. The Z-Anatomy head structures (face muscles, orbicularis, eye muscles, optic pathway and its signal routes) were fitted to
   the old eyes: a similarity about the eye midpoint scaled by the eye-spacing ratio (0.907) follows the new ones.
4. The layers: `RackLayer` (now folder- and multi-material-aware) fits `Skeleton`, `Muscles`, `Organs` and the new
   `Vessels` by the shared landmarks (an exact rigid alignment, since all come from one frame), saving
   `RackSkeleton/Muscles/Organs/Vessels_Fitted` meshes and one `Anatomy_<slot>` material per source material;
   the vessels join `reveal.pathway` so they appear with the organ view at the existing timing.
5. The X-ray skull (`Human_Skull`, Scene 9's bone ghost and the legacy glass skull) is rebuilt from the new skeleton's skull
   slot; the Rack 06 head brain is re-fitted into the .blend brain's box (`FitBrain`, unchanged topology).
6. `reveal.height/shoulder` follow the new heights; `OpeningMagic` re-places the breakup haze and the mote emitter on the
   new head; `OpeningBlink` in the gate-to-face patch now skips when the ANATOMY skin is installed, so re-running earlier
   patches cannot bring the Rocketbox skin back.
7. Materials (`AnatomyLook`): white tints over the supplied albedos; skin wrap .45, gloss .42, specular .2, alpha clip .5;
   muscles specular .12, gloss .35, wet .05; bone specular .07, gloss .25; organs specular .18, gloss .4, wet .12;
   vessels specular .16, gloss .42; normal maps at strength 1.

## Review passes (sandbox `C:\he-sandbox`, rounds 24 e–g; the owner's open Editor was not operated)

Evidence folder: `Captures/anatomy-2026-10-15/` (`before-after-board.png` is the comparison sheet; `blender-*.png` are the
Workbench renders of the .blend systems overlapped at one origin; `pass1/` is the first Unity pass before the tint changes).

**Pass 1, import / position / scale / materials.** `Builder.ApplyAnatomyModels` logs the registration: eyes at
(0.025, 1.600, −0.089) / (−0.033, 1.600, −0.089) m under the subject root, socket radius 1.65 cm, head top 1.703 m,
collar line 1.391 m, previous Rocketbox eye spacing 6.38 cm → 5.79 cm (head-anatomy scale 0.908); skeleton 85,276,
muscles 57,784, organs 79,509 and vessels 48,330 triangles fitted to the same sockets; the Rack 06 brain re-fitted into
the .blend brain's box. First player captures showed every layer in place (`pass1/intro-beats-1600.png`): the muscle
face sits under the skin face with the eyes in its rings, the brain, chiasm, eyes, vessels, lungs and heart sit in the
organ view, the shoulder fade still carries the bust. Two corrections came out of this pass: the import faces +Z, so the
root is now squared by yaw only (an antiparallel `FromToRotation` had turned the figure upside down), and the skin read
pale and waxy under the portrait key, so its tint dropped below white with less wrap (`pass1/skin-hold-16.8s.png` →
`face-hold-16.9s.png`); the muscles took a slightly cooler tint and the blink's brow travel was shortened.

**Pass 2, transition shader / particles / continuity / timing.** `intro-beats-1600.png`: the electric breakup erodes the
new skin in islands with the cyan filaments at 17.4 s and the muscle at 22.6 s, the motes are born on the new meshes (the
emitter rides `Anatomy Skin/Body`, then the muscle layer), the haze sits behind the new head, the organ view keeps the faint
skin silhouette, the eye extraction at 27.3 s is unchanged; `blink-frames-1600.png`: lids closed at 15.8–16.0 s and open
again by 16.3 s. Timings are the saved 17 / 18.6 / 22 / 23.6 / 25.5 s beats (no controller change beyond the vessels field).

**Pass 3, presentation / camera / lighting / gameplay.** `intro-beats-1024.png` (4:3) keeps the title figure and the
bust inside the letterbox; `scene9-eye-to-brain-1600.png` shows Scene 9 unchanged in structure with the re-fitted head
(ghost figure on the stand, X-ray head with the new skull at 14.6 s, labelled lateral geniculate nucleus and primary
visual cortex, the visual system floating out under FROM EYE TO BRAIN). Camera rail, holds, lights and grade were not
touched (the owner fixed the camera on 14 October); Niko's presenter logic is untouched (his portrait is visible in the
Scene 9 frames as before).

**Automated.** Validator 72 passed / 0 warnings / 0 errors. Cinematic tests: `EyeToBrainCinematicSyncsAndRestores`
passed; `SkipReplaySingleScanAndNavigationRestoreOwnedState` failed once at its 29.6 s assertion ("the selected eye has
left its orbit", expected > 0.5 m, got 0.449 m): the shorter figure's socket sits 0.45 m from the display, so the
threshold is now 0.3 m (still far outside the 1.65 cm socket); the re-run and the full PlayMode and walkthrough results are
listed under Final gate below. The test that named the Rocketbox head material now checks the skin's first material
(`_ZWrite` 1 while the shoulders fade, 0 while the organ-view outline shows).

## Final gate (round 24 g, 15 October 2026, 13:31–13:39)

| Check | Result |
|---|---|
| `Validator.RunBatch` | 72 passed / 0 warnings / 0 errors |
| `CinematicTests` (Play Mode) | 2 / 2 passed (opening beats, blink, breakup field and motes, organ view, extraction, skip / replay / menu restore, eye-to-brain) |
| Full Play Mode suite (`HumanEye.Tests.PlayMode`) | 7 / 7 passed, including `FirstRunReachesFinalCuriosity` (114 s) |
| Standalone walkthrough, 1600×900, `-heFast 6 -heStress` | 35 PASS / 0 FAIL, 10 chapters, 65 cues (`S02_L05` retired), pointer audits 0 failures, 54–60 fps per chapter, 103 captures |
| Player captures | intro 1600×900 (130 frames), 1024×768 (49), Scene 9 cinematic (37) |
| Windows development build | succeeded (round f, 13:26) |
| WebGL build (`PlayerBuilds.WebGL`, sandbox) | succeeded, 370 MB, 0 errors / 4 warnings, 9 min 41 s (`Builds/HumanEye/WebGL` in the sandbox; not browser-replayed) |

**Copied back to the owner's project** (`C:\he-sandbox\copy_anatomy.py`, 57 files: the scene, `Human_Brain`, `Human_Skull`,
the four `Rack*_Fitted` meshes, the 19 `Anatomy_*` materials, the re-saved Rack 06 materials, the Anatomy folder's meta
files with the sandbox GUIDs). The D: scene is now `3dd7f9f7d4d9c5714e1629b6e73b906ec8c32ae09e85af36f838a1e96c1312cf`; the main session then re-set the Main Camera clear flag to Skybox in that scene (the copy-back had carried the owner's Solid Color; `Stage.Awake` forces Skybox at runtime either way), so D: now starts `58fd5f75dc7f1754` and `copy_anatomy.py` needs a fresh sync before any further copy-back;
the replaced originals are in `_backups/AI-HUMAN-EYE_2026-10-15_pre-anatomy/copyback-originals/`. The owner's Editor
holds the scene unsaved in memory: reload the saved scene before editing (or re-run the three final patches on the open
buffer: gate-to-face, Niko, ANATOMY.blend models — all idempotent).

## Deliverables summary

- Replaced models: Rocketbox skin and trousers → ANATOMY.blend skin, shorts and lashes (`Anatomy Skin/Body`, `Shorts`);
  Rack 02 → `rack_muscles` (3 slots); Rack 01 → `rack_skeleton` (6 slots) and the X-ray `Human_Skull`; Rack 04 →
  `rack_organs` (7 slots); new `rack_vessels` (arteries + veins). Blender objects used: listed in the mapping table above.
- Exported assets: `Assets/HumanEye/ThirdParty/UserModels/Anatomy/` (5 FBX, 46 maps, `ImportReport.json`);
  generated: `Generated/Meshes/RackSkeleton/Muscles/Organs/Vessels_Fitted.asset`, `Human_Skull.asset`, `Human_Brain.asset`;
  materials `Generated/Materials/Anatomy_*.mat`.
- Changes for the transition: none to the shaders or the breakup; `AnatomyRevealController` gained a `vessels` layer that
  shows and wipes with the organs; the mote emitter and haze are re-placed by the existing `OpeningMagic`.
- Camera: untouched (the owner fixed the rail on 14 October); the figure is 1.70 m instead of 1.81 m, so the rail, holds
  and lights, all measured from the live eye sockets, adapt by themselves (verified by the Portrait assertions and captures).
- Tests changed: `CinematicTests` reads the skin's first material instead of `Rocketbox_head`, and the extracted-eye
  distance threshold is 0.3 m.

## Limitations and owner decisions

- Licence: the .blend's maps live in a `3d_Ripper_Pro` extraction folder of an "Animated Full Human Body Anatomy" package;
  no author or terms were supplied (release gate in `THIRD_PARTY_ASSET_CREDITS.md`).
- The blink is built, not sculpted: the lids close fully in 0.4 s but the brow moves a little with them; a supplied blink
  shape would replace it by re-exporting with the key present.
- The eyelash cards survive under alpha clip but read as a thin line at the bust distance.
- The .blend brain (29,920 tris, one mesh) and eyeballs are not shown: Rack 06 and the cross-section eye remain (above).
- The skeleton beat stays omitted by the 14 October direction; the new skeleton is in the scene as `rack_skeleton`
  (hidden by the controller) and as the Scene 9 X-ray skull. Re-enabling a bone beat is a direction change, not a model change.
- No decimation: the subject carries up to about 210 k triangles in any one opening stage (skin + muscles, or muscles +
  organs + vessels + brain) at 54–60 fps on the test desktop; WebGL takes every map at 2048 through the platform override.
- The superseded Rocketbox and RackAnatomy folders are still in the project (nothing references them from the scene);
  delete them once the owner has accepted the result. The owner's own files in `RackAnatomy/` (today's "Photorealistic
  Muscle UV Atlas Texture.png", "ChatGPT Image…", "New Material.mat") were not touched.
