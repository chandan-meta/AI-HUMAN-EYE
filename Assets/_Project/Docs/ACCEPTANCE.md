# Scene 2 acceptance — development build

Status: INCOMPLETE. Do not tag S02_v1 or describe this as production-ready. The user explicitly authorized pushing an unfinished handoff snapshot on 1 October 2026; see the repository-root HANDOFF.md.

## Verified locally

- Unity 6000.3.0f1 compiles and generates Core_VisionLab and additive S02_MeetNiko.
- Windows x64 development player builds. Android ARM64/Vulkan/ASTC development APK builds; no physical Android installation is verified.
- Runnable smoke check (`MeetNiko.exe --s02-smoke`) exercises intro and scan wait gates, pause without changing timeScale, eye isolation, question resolution, selection entry points, all-four visitation, transition unlock, and transform-preserving EyeState handoff.
- `--s02-smoke --s02-cinematic` additionally plays the authored intro/scan/reveal instead of skipping them and plays the first part focus.
- The visual check found and fixed an entry-fade soft-lock after Intro skip. A regression assertion checks that the fade is transparent in Explore.
- Full cinematic execution reached Handoff. Render evidence is incomplete: background native captures are black, and offscreen eye appearance/interface text still need visual QA. Passing a state check does not certify the final graphics.
- Scripted questions reject part names embedded inside unrelated words.
- Asset inventory covers 1,942 archive entries, including 1,478 project assets. Caches/package copies are identified separately. Hero source assets are imported; source copies outside the project remain intact.
- Local JSONL telemetry and all 20 exact subtitle IDs are implemented. Questions and touch/mouse/chip/ask routes are logged. Route tests call the common selection API; this is not a physical touch-device test.

## Section 11 matrix

| Criterion | Status / evidence still needed |
|---|---|
| B01–B18, cameras, VO, subtitles | Development timelines exist; native camera, camera animation, audio and completion-signal tracks are authored. Final storyboard DOCX/recordings are not provided. Exact timing and cinematic visual review remain pending. |
| Layered scan; opaque eyes; 3 s socket hold | Imported skin, sourced muscle/skull/brain, separate eye geometry; socket shot is authored for 5 s. Cross-model anatomical registration, body posing and visual layer review remain pending. |
| Pinch/isolation, rotation, sweep | Isolation and rotation exist; robot gesture clips are temporary mappings. Dedicated pinch/snap/point/turn and lip animation still require authoring/review. |
| Mouse, touch, chips, questions; focus/return | Common selection route and raycast/input router exist. Automated route checks pass. Physical mouse/touch hit testing and translucent collider priorities need manual QA. |
| Four visited, fallback, Inside View/EyeState | Four-part unlock and EyeState pass smoke checks. >=2 parts plus 90 s fallback is implemented but not yet timed in the full device matrix. Scene 3 itself is not part of this deliverable. |
| Four device classes | Windows automated execution only. No Android tablet is connected. Windows touchscreen and iPad are unavailable. iPad requires Unity iOS support and a macOS/Xcode signing/build environment. |
| Skip/replay/pause/captions/font/motion | Controls are implemented. Skip/pause are smoke-tested; full replay/accessibility manual matrix pending. |
| Nudges/repeats/JSONL | Implemented; JSONL event inspection available. Long idle/repeat cases still need dedicated timing checks. |
| FPS/memory | Not certified. PC 1440p target and five-minute tablet FPS/memory soak require real rendering/device profiling. |
| Four aspect ratios/safe areas | Anchor-based layout and safe-area fitting implemented. Rendered checks across all ratios/font sizes/devices are pending. |
| Audit/licences/no placeholders | No runtime placeholder hero primitives. Generated environment meshes and development TTS/SFX are used. Original zip asset redistribution rights and brain component provenance remain unresolved. Asset rows for final icons, optical-prop variety, refractive Shader Graphs, audio mixer/recordings, LODs and Addressables streaming remain incomplete. |

## Production gates

Replace development audio with final Niko recordings; complete dedicated character animations and observatory/art polish; provide the requested Shader Graph/mixer/LOD/Addressables production setup; resolve provenance; validate all input/layout/performance device cases. Non-development builds are deliberately blocked by AssetAudit until those gates are resolved. This is a runnable development module, not a completed §11 release.

## Git

Working branch: feat/s02-meet-niko. Existing origin: https://github.com/chandan-meta/AI-HUMAN-EYE.git. The master prompt §13 gates the completed release push on full acceptance; the user's subsequent explicit request authorizes the unfinished development handoff snapshot. No S02_v1 release tag is created.
