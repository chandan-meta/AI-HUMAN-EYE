# Third-Party Notices — HumanEye

Every third-party file in `Assets/HumanEye/ThirdParty/` is listed here. The machine-readable register is `Docs/HumanEye/ASSET_REGISTER.csv`.
Licences were checked on the source pages on 2026-10-02.

---

## 1. Food Apple 01 (3D model)

- **Creator:** Oliver Harries (Poly Haven)
- **Source:** https://polyhaven.com/a/food_apple_01 (files from https://api.polyhaven.com/files/food_apple_01)
- **Licence:** CC0 1.0 (public domain dedication). Verified at https://polyhaven.com/license ("Our assets are all licensed as CC0").
- **Local path:** `Assets/HumanEye/ThirdParty/PolyHaven/Apple/`
  - `food_apple_01_2k.fbx`
  - `textures/food_apple_01_diff_2k.jpg`, `textures/food_apple_01_rough_2k.jpg`, `textures/food_apple_01_nor_gl_2k.jpg`
- **Modifications:** None. Poly Haven supplies the FBX, so no conversion was needed. The textures are unchanged. The JPG OpenGL normal map was downloaded instead of the EXR normal that the FBX references, so in Unity the normal map has to be assigned by hand. MD5 hashes match the Poly Haven API.
- **Credit (optional under CC0):** "Food Apple 01" by Oliver Harries, Poly Haven (polyhaven.com), CC0.

## 2. Searsia Lucida (3D model, distant-object stand-in)

- **Creator:** James Ray Cock, Jenelle van Heerden (Poly Haven)
- **Source:** https://polyhaven.com/a/searsia_lucida
- **Licence:** CC0 1.0. Verified at https://polyhaven.com/license.
- **Local path:** `Assets/HumanEye/ThirdParty/PolyHaven/Tree/`
  - `searsia_lucida_1k.fbx` (7 variants, `searsia_lucida_a_LOD0` to `searsia_lucida_g_LOD0`)
  - `textures/searsia_lucida_diff_1k.jpg`, `textures/searsia_lucida_rough_1k.jpg`, `textures/searsia_lucida_nor_gl_1k.jpg`, `textures/searsia_lucida_alpha_1k.png`
- **Modifications:** None. The FBX comes from Poly Haven, so there was no conversion. The JPG roughness and normal maps were used instead of the EXR versions that the FBX references. MD5 hashes match the Poly Haven API.
- **Credit (optional under CC0):** "Searsia Lucida" by James Ray Cock and Jenelle van Heerden, Poly Haven (polyhaven.com), CC0.

## 3. Ferndale Studio 09 (HDRI)

- **Creator:** Dimitrios Savva (photography), Jarod Guest (processing) (Poly Haven)
- **Source:** https://polyhaven.com/a/ferndale_studio_09
- **Licence:** CC0 1.0. Verified at https://polyhaven.com/license.
- **Local path:** `Assets/HumanEye/ThirdParty/PolyHaven/HDRI/ferndale_studio_09_1k.hdr`
- **Modifications:** None.
- **Credit (optional under CC0):** "Ferndale Studio 09" by Dimitrios Savva and Jarod Guest, Poly Haven (polyhaven.com), CC0.

## 4. Atkinson Hyperlegible (font, Regular and Bold)

- **Creator:** Braille Institute of America, Inc. (design by Applied Design Works: Elliott Scott, Megan Eiswerth, Linus Boman, Theodore Petrosky)
- **Source:** https://github.com/google/fonts/tree/95f4904fc8bcf26d3420fe315560c96417c6dec7/ofl/atkinsonhyperlegible
  (google/fonts commit `95f4904fc8bcf26d3420fe315560c96417c6dec7`, 2026-03-03, which was the latest commit for that path. Font version 1.006.)
- **Licence:** SIL Open Font License 1.1. Verified from the `OFL.txt` in that folder and from `METADATA.pb` (`license: "OFL"`).
- **Local path:** `Assets/HumanEye/ThirdParty/Fonts/AtkinsonHyperlegible/`
  - `AtkinsonHyperlegible-Regular.ttf`, `AtkinsonHyperlegible-Bold.ttf`, `OFL.txt`
- **Modifications:** None. The git blob SHAs match the repository.
- **Required notice (OFL-1.1):**
  - Copyright 2020 Braille Institute of America, Inc. This Font Software is licensed under the SIL Open Font License, Version 1.1.
  - The font files are bundled in the build. Any redistribution of them (including inside the app) must include the copyright notice and the licence text. Keep `OFL.txt` next to the fonts and reproduce this notice in the app's credits/licences screen or in the distributed documentation.
  - The font must not be sold on its own. A modified version (including a subset or conversion done outside Unity's normal font asset generation) must not use any Reserved Font Name and must stay under OFL-1.1.

## 5. Kenney audio (Interface Sounds 1.0, Sci-fi Sounds 1.0)

- **Creator:** Kenney (www.kenney.nl)
- **Sources:** https://kenney.nl/assets/interface-sounds and https://kenney.nl/assets/sci-fi-sounds (direct zip downloads)
- **Licence:** CC0 1.0. Verified on both asset pages ("License: Creative Commons CC0") and in each pack's `License.txt`.
- **Local path:** `Assets/HumanEye/ThirdParty/Kenney/Audio/`. Only 10 files were copied, plus both licence files (`Kenney_InterfaceSounds_License.txt`, `Kenney_SciFiSounds_License.txt`).

| Local file | Original file | Pack |
|---|---|---|
| `ui_click_soft.ogg` | `Audio/click_001.ogg` | Interface Sounds |
| `ui_tap.ogg` | `Audio/select_001.ogg` | Interface Sounds |
| `ui_confirm.ogg` | `Audio/confirmation_003.ogg` | Interface Sounds |
| `ui_error_soft_deny.ogg` | `Audio/error_008.ogg` | Interface Sounds |
| `scan_sweep_hum.ogg` | `Audio/forceField_000.ogg` | Sci-fi Sounds |
| `transition_whoosh.ogg` | `Audio/maximize_006.ogg` | Interface Sounds |
| `chime_discovery.ogg` | `Audio/glass_001.ogg` | Interface Sounds |
| `chime_soft_bong.ogg` | `Audio/bong_001.ogg` | Interface Sounds |
| `signal_pulse.ogg` | `Audio/laserSmall_000.ogg` | Sci-fi Sounds |
| `signal_tick_beep.ogg` | `Audio/tick_004.ogg` | Interface Sounds |

- **Modifications:** Renamed only. The audio data is unchanged.
- **Credit (optional under CC0):** Sound effects by Kenney (www.kenney.nl), CC0.


## 6. Z-Anatomy head structures (skull, brain, occipital lobe)

- **Creators:** Z-Anatomy (Gauthier Kervyn: design, 3D, anatomy; Marcin Zielinski: Blender add-on; Lluis Vinent: Unity). Models derived from **BodyParts3D**, © The Database Center for Life Science (DBCLS), Kousaku Okubo. The brain references "Brainder" / University of Washington.
- **Source:** https://github.com/Z-Anatomy/Models-of-human-anatomy, `Z-Anatomy.zip` at commit `bb293be456f6d2245191f37752d14ec76894740d` (2024-06-07).
- **Licence:** CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/). BodyParts3D is CC BY-SA 2.1 Japan. Verified in the repository's `License.txt` and `Readme.md`.
- **Excluded on purpose:** "Anatomy of the Inner Ear" (CC BY-NC-SA 4.0) and "Kidney" (CC BY-NC 4.0) components are never selected by the export script.
- **Local path:** `Assets/HumanEye/ThirdParty/ZAnatomy/ZAnatomy_Head.fbx`
- **Modifications:** exported by `Tools/HumanEye/export_zanatomy_head.py` (headless Blender 5.1, no add-ons, autoexec disabled). Objects were joined into ZA_Skull, ZA_Brain and ZA_VisualArea, merged, partly decimated (skull 30%, cerebellum/brainstem/midbrain 14%; cerebral lobes at full resolution), smooth-shaded, and four registration marker cubes were added. In Unity they are scaled/registered (by eyeball markers) and re-materialised.
- **Share-alike:** the Z-Anatomy FBX and generated `Assets/HumanEye/Generated/Meshes/Human_Skull*` and `ZA_*` assets remain under CC BY-SA 4.0. The historical `Human_Brain*` and `Brain*` derivatives were replaced with the independently supplied Rack 06 geometry on 14 October 2026; see section 7 for its separate provenance.
- **Required credit:** "Anatomy models: Z-Anatomy – The libre 3D atlas of anatomy (CC BY-SA 4.0), derived from BodyParts3D, The Database Center for Life Science (CC BY-SA 2.1 Japan)."

**Vision structures (8 October 2026).** `Assets/HumanEye/ThirdParty/ZAnatomy/ZAnatomy_Vision.fbx` (+ `.json` signal
routes) comes from the same Z-Anatomy release and licence, exported by `Tools/HumanEye/export_zanatomy_vision.py`
(headless Blender 5.1, no add-ons, autoexec disabled): facial and epicranial muscles with temporalis, masseter, platysma
and sternocleidomastoid (decimated to 30 %; the orbicularis oculi apart at 60 %), the extra-ocular muscles with levator,
trochlea and common tendinous ring (50 %), the optic nerves (curves given a 1.6 mm radius), optic chiasm, optic tracts,
lateral geniculate bodies and calcarine sulci (full resolution); muscle and tendon faces keep separate material slots;
box-projected UVs added. The optic radiations are **not** atlas geometry: three fibres per side were drawn by the script
from the LGN to the calcarine cortex (the inferior one through Meyer's loop) as an illustrative approximation. The derived
meshes (`Assets/HumanEye/Generated/Meshes/ZA_*`) remain CC BY-SA 4.0; the same credit line applies.

The earlier prototype's Z-Anatomy exports in `Assets/_Project/SourceAssets/Anatomy/` are still credited in `Assets/_Project/Docs/ATTRIBUTIONS.md`. They are not used by this module because the "Skull" export contained only the upper jaw and the brain export was over-decimated.

## 7. Supplied project-archive assets (licence unverified)

- **Rack 06 brain, 14 October 2026:** exact source `C:\Users\ADMIN\Documents\Received Files\Rack 06.fbx`, copied unchanged to `Assets/HumanEye/ThirdParty/UserModels/RackBrain/Rack 06.fbx` (SHA-256 `2ecc840ce5fe81aedf01e902ac667486fe1d7883fa672f03c68cd722050c3d41`). `Tools/HumanEye/export_rack_brain.py` excludes the cabinet and exports eight brain structures, 77,882 triangles, as `Rack06_Brain.fbx`. Original UVs and topology are retained, normals smoothed, scale/orientation normalized, and embedded albedo/normal/AO textures extracted at up to 2048 pixels. Unity fits these to the display, anatomical head and legacy brain instances. Author and redistribution terms were not supplied; no licence is inferred. The active display no longer uses the older `UserModels/Brain/` asset. The optic chiasm, tract, LGN, radiations and cortex guides retain the separate Z-Anatomy provenance above. No new online assets were downloaded.

- `Assets/_Project/SourceAssets/Zip/Extra/FitnessMan.fbx` (scan figure) came from the project owner's archive `G10_The Human Eye_VR1.zip`. Its author and redistribution licence are **not documented**. This is a release gate: confirm the rights or replace the figure before public distribution. Its body texture is not used.

- `Assets/HumanEye/ThirdParty/UserModels/Matt/` (scan figure on the pedestal, replacing FitnessMan) is a static export derived on a copy from the owner-supplied Reallusion Character Creator 4 files `D:\AI unity project\models\human character\Blender\Matt.Fbx` / `Matt__Motion.Fbx`. The Reallusion EULA governs CC content; the raw CC files are **not** in the repository and the derived export must not be redistributed until the owner's licence is confirmed.
- `Assets/HumanEye/ThirdParty/UserModels/Brain/` (Brain Display) is the owner-supplied `D:\AI unity project\models\Brain.fbx` with `BrainDiff03.png` from `Brain_Textures.zip`, unaltered; author and licence unknown (native file name `Brain_03.blend` from a "Free Downloads" folder).
- `Assets/HumanEye/ThirdParty/UserModels/Matt/Skeleton_Display.fbx` (X-ray layer) is a static export on a copy of the owner-supplied `D:\AI unity project\models\skeleton_pose_01.blend`; author and licence unknown.
- `Assets/human brain env/` (the sci-fi lab room the module now plays in), `Assets/Scenes/Garden/` and the skeleton animation were imported by the owner from `HUMAN BRAIN ENV.unitypackage`, which carries no licence file; several parts look like third-party downloads (microscope, glassware, door, power supply), the doctor is Tripo-generated and the screen video looks like stock footage. Confirm the rights before public distribution.

- `Assets/HumanEye/ThirdParty/LocalEye/` and the meshes `Assets/HumanEye/Generated/Meshes/LocalEye_*` (the visible eye
  since 7 October 2026) are baked by `Tools/HumanEye/export_local_eye.py` from the owner-supplied
  `D:\AI unity project\models\eye\eye model\half eye.fbx` (cross-section: sclera, choroid, retina with vessels, iris,
  lens, zonules, optic nerve stub, extraocular muscles) and its embedded textures; `Eye_Diff_Sclera.jpg` is a project
  derivative of `Eye_Diff.jpg` (`Tools/HumanEye/whiten_sclera.py`). The same family appears inside the archive's
  FitnessMan.fbx. Author and licence are **not documented**: a release gate, like the other supplied assets. The stray
  wardrobe texture on the source lens (`Almirah_Diff.jpg`) is not used. `full eye.fbx` was evaluated and not used.
- `Assets/HumanEye/ThirdParty/UserModels/Rocketbox/` (the scan figure since 7 October 2026) is a static, arms-lowered
  display export (eyeballs removed, trousers split) by `Tools/HumanEye/export_rocketbox_display.py` of the owner-supplied
  `D:\AI unity project\models\eye\Human.unitypackage`, which contains Microsoft Rocketbox *Business_Male_06*
  (Microsoft Rocketbox avatar library, MIT licence — keep the MIT notice with any redistribution). The package's
  brain-lesson scripts and shaders are not imported.

## 8. Project-owned generated content

**Opening update, 13 October 2026:** the owner supplied `Rack 01.fbx` (skeleton), `Rack 02.fbx` (muscles), and `Rack 04.fbx` (internal organs)
from `C:\Users\ADMIN\Documents\Received Files\`. Their author/licence is not documented, matching the supplied-asset
status in section 7. `Tools/HumanEye/export_rack_anatomy.py` extracts only anatomy, normalizes scale/orientation/pivot,
decimates it, packs diffuse textures into 2048 atlases, and adds registration markers. The derivatives are
`Assets/HumanEye/ThirdParty/UserModels/RackAnatomy/` and `Generated/Meshes/Rack*_Fitted.asset`.
The original files are unchanged. No redistribution licence is inferred. Rack 04 contains torso organs and mouth/teeth, but no eye or brain mesh; its derivative uses inferred whole-body registration markers and the organ presentation retains the existing project eyes/brain.

The dedicated opening score `Audio/Music/intro_discovery.wav` is original pad/pulse/melody/room-tone synthesis
from `Tools/HumanEye/generate_intro_music.py`; it uses no samples or external recordings.

The procedural eye, Niko, observatory, retina micro-set, iris and vessel textures, ray and dash textures, ambient music loop, the intro and lesson sound-effect sets (`Audio/SFX/intro_*.wav`, `ui_*.wav`, `fx_*.wav`, from `Tools/HumanEye/generate_intro_sfx.py` and `generate_ui_sfx.py`, pure synthesis, no samples) and the provisional TTS narration (including the supplemental closing line `Audio/SFX/niko_closing.wav`) are generated by this project's tools (`Assets/HumanEye/Editor`, `Tools/HumanEye`). The TTS clips use a Windows-licensed OneCore voice for development only and are listed as provisional.

## 8. Realistic human eye — MisterH

“Realistic human eye” by MisterH (TGVMisterH), Sketchfab.
Source: https://sketchfab.com/3d-models/realistic-human-eye-f2370371544c4b029123da0b7d7e2457
Author: https://sketchfab.com/TGVMisterH
Licence: Creative Commons Attribution 4.0 International (CC BY 4.0), https://creativecommons.org/licenses/by/4.0/

The user supplied realistic_human_eye.glb. Modifications: normalized scale and optical-axis alignment; baked native Unity meshes; split baked exterior into sclera, iris and pupil regions by UV islands; adapted corneal transparency and base colour to the project's URP material. Normal/roughness maps and physical transmission are not reproduced. The imported exterior fades into the project's schematic internal anatomy for cutaway teaching. The animated pupil-response lesson uses the original procedural geometry. No creator endorsement is implied.

## 9. Audio overhaul music and effects (15 October 2026)

- **Licence:** every source is CC0 1.0, verified on its page on 2026-10-15 (`C:/he-sandbox/audio_src/pages.json`) and, for the
  Kenney packs, in each zip's `License.txt` (copied to `Assets/HumanEye/Audio/Licenses/`). No attribution is required; the
  credits page `Assets/HumanEye/Audio/Licenses/AUDIO_CREDITS.md` names everyone anyway.
- **Ledger:** `Assets/HumanEye/Audio/Licenses/AUDIO_SOURCES.csv` has one row per source file per processed clip (source page,
  download URL and date, original file, processed file, use).
- **Modifications:** filtered, trimmed, looped, layered and loudness-normalised by `Tools/HumanEye/build_audio_assets.py`
  (downloads by `Tools/HumanEye/fetch_audio_sources.py`); processed clips live in `Assets/HumanEye/Audio/{Music,Ambience,SFX}`.

| Source | Creator | Page |
|---|---|---|
| Exploration Theme | CleytonKauffman | https://opengameart.org/content/exploration-theme |
| The Cogs | turtletooth | https://opengameart.org/content/the-cogs |
| First Light Particles | Yoiyami | https://opengameart.org/content/first-light-particles-%E2%80%93-cc0-atmospheric-pianoambient-track |
| Claimed by the Void | vitalezzz | https://opengameart.org/content/claimed-by-the-void |
| Outer Space Loop | wipics | https://opengameart.org/content/outer-space-loop |
| Dark Sci-Fi Audio Pack ("Airy") | SRG774 | https://opengameart.org/content/dark-sci-fi-audio-pack |
| Simple BGM Loop | Theforeshadower | https://opengameart.org/content/simple-bgm-loop |
| Space City | MintoDog | https://opengameart.org/content/space-city |
| Sci-Fi Sounds | Kenney | https://kenney.nl/assets/sci-fi-sounds |
| Interface Sounds | Kenney | https://kenney.nl/assets/interface-sounds |
| UI Audio | Kenney | https://kenney.nl/assets/ui-audio |
| Magic Spell SFX | JaggedStone | https://opengameart.org/content/magic-spell-sfx |
| Teleport Spell | Ogrebane | https://opengameart.org/content/teleport-spell |
| 18 random video game sound effects | bart | https://opengameart.org/content/18-random-video-game-sound-effects |
