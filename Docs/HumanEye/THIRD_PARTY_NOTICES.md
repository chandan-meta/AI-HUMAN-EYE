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
- **Share-alike:** the derived meshes (FBX and the generated `Assets/HumanEye/Generated/Meshes/Human_Skull*`, `Human_Brain*`, `Brain*` assets) remain under CC BY-SA 4.0.
- **Required credit:** "Anatomy models: Z-Anatomy – The libre 3D atlas of anatomy (CC BY-SA 4.0), derived from BodyParts3D, The Database Center for Life Science (CC BY-SA 2.1 Japan)."

The earlier prototype's Z-Anatomy exports in `Assets/_Project/SourceAssets/Anatomy/` are still credited in `Assets/_Project/Docs/ATTRIBUTIONS.md`. They are not used by this module because the "Skull" export contained only the upper jaw and the brain export was over-decimated.

## 7. Supplied project-archive assets (licence unverified)

- `Assets/_Project/SourceAssets/Zip/Extra/FitnessMan.fbx` (scan figure) came from the project owner's archive `G10_The Human Eye_VR1.zip`. Its author and redistribution licence are **not documented**. This is a release gate: confirm the rights or replace the figure before public distribution. Its body texture is not used.

## 8. Project-owned generated content

The procedural eye, Niko, observatory, retina micro-set, iris and vessel textures, ray and dash textures, ambient music loop and provisional TTS narration are generated by this project's tools (`Assets/HumanEye/Editor`, `Tools/HumanEye`). The TTS clips use a Windows-licensed OneCore voice for development only and are listed as provisional.
