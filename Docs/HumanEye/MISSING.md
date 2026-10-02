# Missing assets, audio, services and validation (precise list)

## Audio
- **Recorded narration: 0 of 66 supplied.** All 66 clips in `Assets/HumanEye/Audio/VO/` are **provisional** local TTS (Windows OneCore "Microsoft Mark", en-US), generated from the exact text. Replace each file with the recording of the same name. `Docs/HumanEye/VO/HumanEye_VO.csv` is the recording script; `VO_MANIFEST.csv` maps cue → file. Rebuild the module afterwards: clip lengths drive the timing automatically.
- **Pronunciation of anatomical terms was not reviewed by ear.** No human listening was possible in this session. The only check was automatic: offline en-US dictation scored average word recall 0.81 (`VO/TTS_INTELLIGIBILITY.csv`, `VO/TTS_TERMS.csv`), which is a proxy, not a listening test. Check at least: sclera, cornea, aqueous humour, vitreous humour, crystalline lens, biconvex, choroid, photoreceptors, accommodation, optic nerve.
- **Ask Niko answers that are not storyboard quotes are text-only.** Answers that quote taught lines replay the source audio. Added answers have no audio.
- **Music** is a generated placeholder pad. **SFX** are 10 Kenney CC0 sounds chosen by filename and loudness, not by listening; audition them.

## Visual assets and art direction
- **Human scan figure:** `FitnessMan.fbx` comes from the project owner's archive. **Licence and author are unverified** (release gate). Its texture is not used (untextured skin/clothing).
- **Muscle layer** is an inward offset of the body surface tinted as muscle. It is illustrative, not individual muscles.
- **Niko** is procedural. Gestures (wave, raise hand, present, point, pinch, snap, talk) are procedural poses. There is no lip-sync beyond voice-level-driven mouth/eye scaling. Art-direction review is pending.
- **Eye** is a procedural, illustrative model. Proportions are near-anatomical, thin layers are thickened for legibility, the optic disc is 15° nasal and there is no ciliary body or zonules. Needs specialist review.
- **Brain/skull:** Z-Anatomy (CC BY-SA 4.0) exports. The cerebral lobes and occipital lobe are kept at full source resolution; only the cerebellum, brainstem and midbrain are decimated (to 14%). The skull is decimated to 30%. Normals are fully smoothed on import, and the brain reads as gyri and sulci at close range.
- **The distant object** is a Poly Haven shrub-tree. The apple has a stem but no leaf; inversion is read from the stem.
- **Drag-and-drop** alternative for "Rebuild the journey" is not built (tap placement is).

## Services (intentionally not configured)
- No remote AI provider, no speech-to-text/push-to-talk, no analytics, no backend. Ask Niko is the labelled local curated mode. A remote provider would need a server proxy for any secret. None is supplied, so none is wired.

## Platforms and devices
- **Web build:** the WebGL module is not installed for Unity 6000.3.0f1, so no Web build was made or tested. Install it via Unity Hub (Add modules > Web Build Support) only if a Web target is wanted.
- **Android tablet:** a Development APK builds (`Builds/HumanEye/Android/HumanEye.apk`, about 80 MB, ARM64, API 29). **No physical tablet was available**, so installation, touch, safe areas, audio start and frame rate on a tablet are **unverified**.
- **iPad/iOS:** not configured (requires macOS/Xcode/signing).
- **Windows touchscreen:** not tested (no touch hardware used).
- **Aspect ratios:** 16:9 (1600×900), 4:3 (1440×1080), 16:10 (1600×1000) and portrait (540×960) were captured and reviewed in the Windows player (see TEST_REPORT.md). Real tablet safe-area insets (notches, rounded corners) were not checked.
- **Performance:** per-chapter FPS, main-thread time, draw calls, SetPass, triangles and memory were logged on this development PC (RTX 3050 Laptop): about 58 FPS with VSync at 60 Hz (TEST_REPORT.md). The 30 FPS tablet target is **unmeasured**, and overdraw and GPU frame time were not profiled.
- **Intermittent crash at exit on D3D12:** after Scenes 6/11 the Windows player sometimes faults inside `D3D12Core.dll` during shutdown. It never happens with D3D11, and saves are already flushed. The workaround is `-force-d3d11` or D3D11 first in the Windows Graphics APIs. The project setting was left unchanged pending the owner's decision (TEST_REPORT.md).
- **Physical input:** automated runs press the same public actions as the buttons. Real mouse and touch hit-testing, double taps and taps over UI still need a manual pass.

## Release gates already in the project
- The prototype's `Assets/_Project/Editor/AssetAudit.cs` rejects **non-development** builds until production acceptance. It was left untouched, so all builds here are Development builds.
