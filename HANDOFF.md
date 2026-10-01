# Human Eye — Scene 2 “Meet Niko” handoff

Updated: 1 October 2026. Status: **runnable development module; production acceptance is incomplete**.

Repository: https://github.com/chandan-meta/AI-HUMAN-EYE
Working branch: `feat/s02-meet-niko`.
The user explicitly requested this handoff file and a push on 1 October 2026. That authorizes publishing this unfinished development snapshot; it does not certify the master prompt's acceptance criteria. Do not tag `S02_v1` yet.

## Open and run

Use **Unity 6000.3.0f1**. The existing project's Unity/URP versions were retained instead of downgrading to the master prompt's assumed versions. URP 17.3, Input System 1.16, Cinemachine 3.1.5, Timeline and Addressables 2.7.6 are installed.

1. Clone this branch with Git LFS installed, then run `git lfs pull`.
2. Open this repository in Unity Hub and let the package/asset imports finish.
3. Open `Assets/_Project/Scenes/Core_VisionLab.unity` and press Play. Core loads `S02_MeetNiko` additively.
4. SCAN opens the layered scan. In Explore, select a part using the chips or Ask Niko. Explore all four parts to unlock INSIDE VIEW.

Local build outputs, deliberately excluded from Git:

- `Builds/Windows/MeetNiko.exe` plus its adjacent data/runtime folders. Copy the entire Windows folder to distribute it.
- `Builds/Android/MeetNiko.apk`: development Android ARM64/Vulkan/ASTC build, minimum API 29.

Use the editor menu **Vision Lab > Build Scene 2 module** to regenerate authored scenes/assets. Batch entry points are `VisionLab.Editor.S02Builder.BuildAll`, `BuildWindows`, and `BuildAndroid`. Rebuilding deletes and recreates generated scene/timeline/data assets, so preserve intentional manual edits first.

## Implemented

- Persistent Core scene, additive Scene 2, state machine with explicit SCAN/Explore/INSIDE VIEW wait points.
- Native Timeline camera/audio/animation/signal tracks and Cinemachine cameras; Timeline sub-assets persist across editor restarts.
- Imported human skin, sourced muscle/skull/brain layers, separated eye meshes, and a sourced rigged hologram robot for Niko.
- Four part-focus routes, visited-set unlock, two-parts/90-second Continue fallback, scripted question resolution, idle/repeat responses and local timestamped JSONL telemetry.
- Mouse/touch input router, orbit/pinch/wheel zoom, chips, question bar, skip/replay/pause, captions, subtitle sizes and reduced-motion controls.
- Twenty exact subtitle/VO IDs with **development TTS**, generated development SFX/ambience, runtime ambience ducking and PC/tablet quality assets.
- Transform-preserving `EyeState` handoff. Scene 3 is not included; `S03_InsideEye` loads additively only if installed in build settings.

## Validation and evidence

- Windows x64 development compilation/build succeeds.
- `MeetNiko.exe --s02-smoke` passes state gates, pause with unchanged timeScale, eye isolation, question parsing, selection API routes, four-part visitation, transition and handoff.
- `--s02-smoke --s02-cinematic` plays Intro, Scan, EyeReveal and the first part focus normally; later focus/transition beats use Skip to keep the check bounded. It reached Handoff successfully. This is not verification of every authored frame or every physical input path.
- Android development APK builds successfully; the handoff revision is rebuilt before this snapshot is published. Installation/performance on a real tablet is unverified.
- Local logs are under `Logs/`. Automated render captures and `SMOKE_RESULT.txt` are under `Builds/Windows/Builds/Checks/`. Neither folder is committed.
- Runtime telemetry is under Unity's `Application.persistentDataPath` (on this machine: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/AI HUMAN EYE/`). Question text is included in local telemetry.

Automated selection checks call the shared API; they do **not** simulate or certify physical mouse/touch hit testing. Background native screen capture returns black on this host. Offscreen URP render requests provide scene evidence, but do not certify native Overlay UI rendering or device safe areas.

## Known issues and next work

1. **Finish visual QA first.** Offscreen snapshots show a grey iris/pupil region through the cornea and inconsistent interface text visibility across shots. Inspect eye depth/culling/transparency, material property blocks and the UI render path in the editor. `EYE_CHECK` diagnostics are included in the smoke-check log. Do not call the eye's final appearance or the UI layouts accepted.
2. The source eye extraction originally lost rig deformation. It now bakes evaluated geometry before export; the human is rotated to face the camera. Review skin/muscle/skull/brain anatomical registration and socket placement visually. Portable Blender preparation script: `Assets/_Project/Editor/Tools/BakeEye.py` (`blender --background --python ... -- --source <FitnessMan.fbx> --output <Eye_Hero.fbx>`).
3. **Replace development audio.** `HUMAN_EYE.docx` and final Niko recordings were not present in the supplied archive. Supply them, compare the storyboard, replace all 20 TTS files, and retime audio/camera beats.
4. Author and verify dedicated Niko pinch/snap/point/turn/talk/lip/look-at animation. Several gesture names currently map to existing robot clips as temporary development mappings.
5. Complete the observatory/art pass and requested Shader Graph assets. Current Scan/Highlight/Holo/Beam effects use custom HLSL/material logic. The extra UI distance-field shader is also experimental and needs review.
6. Add the requested Master/VO/SFX/Amb mixer groups and complete volume controls, production SFX/room ambience, LODs and Addressables streaming. Installing Addressables alone does not implement streaming.
7. Resolve supplied-archive redistribution rights and the brain subset's component provenance; retain the sourced assets' attribution/share-alike notices. See `Assets/_Project/Docs/ATTRIBUTIONS.md`.
8. Validate every beat, replay, captions/font settings, reduced motion, idle/repeat cases, actual raycasts and fallback timing. Check B18 prompt/telemetry coverage explicitly.
9. Test Windows mouse, Windows touchscreen, Android tablet and iPad; 16:9, 16:10, 4:3 and 3:2 layouts, safe areas and subtitle/control overlap. No Android tablet is connected here. iPad requires Unity iOS support plus macOS/Xcode and signing.
10. Profile PC 1440p/60 fps and tablet sustained 30 fps for five minutes with memory <=1.5 GB. Those targets are **not certified**.

The full acceptance matrix is `Assets/_Project/Docs/ACCEPTANCE.md`. `AssetAudit` deliberately rejects non-development builds while production acceptance remains pending. Optional remote AI/STT features are disabled; no API secrets are supplied.

## Sources and repository hygiene

The original 2.15 GB `G10_The Human Eye_VR1.zip`, its full extraction, and downloaded source archives remain outside this repository. The project contains selected source assets and derived exports only. The inventory covers 1,942 archive entries (1,478 project assets) in `Assets/_Project/Docs/ZIP_INVENTORY.json`.

Binary asset patterns use Git LFS. Unity caches, logs, player builds, `.utmp` and `.env` files are ignored. No production release tag or pull request is created by this handoff.

The pre-existing local edit in `ProjectSettings/ShaderGraphSettings.asset` is preserved and excluded from the handoff commit. It adds `overrideShaderVariantLimit: 0`; evaluate it separately if needed.

Additional notes: `Assets/_Project/Docs/BUILD_NOTES.md`, `ASSET_AUDIT.md`, and `ATTRIBUTIONS.md`.
