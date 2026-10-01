# Scene 2 — Meet Niko

Open Assets/_Project/Scenes/Core_VisionLab.unity and press Play. Core loads S02_MeetNiko additively. Rebuild authored assets through Vision Lab > Build Scene 2 module. Asset audit is under Vision Lab > Audit assets.

Unity 6000.3.0f1 / installed URP 17.3 and Input System 1.16 are preserved. Cinemachine 3.1 and Addressables 2.7 are added. The prompt's Unity 2022/URP14 versions were assumptions. No DOTween dependency is required.

Mouse/touch: SCAN, then click/tap a part or use a chip. Drag rotates; wheel/pinch zooms. Ask Niko resolves exact part names and descriptive synonyms locally. The microphone and remote AI tutor are disabled; no API credentials or network requests are used. INSIDE VIEW saves EyeState and loads S03_InsideEye additively if that scene is installed.

Pause leaves timeScale untouched. Captions are on by default. Settings exposes captions, reduced motion, subtitle size and voice/ambience volumes. Telemetry JSONL is written to Application.persistentDataPath, one file per session. Question text is local telemetry.

All voice files are 48 kHz, mono, 24-bit PCM development TTS, normalised with FFmpeg loudnorm. Generated tones are development SFX. Final recording is not supplied. Do not tag S02_v1 or claim device/performance acceptance until ACCEPTANCE.md is completed.

The module is on feat/s02-meet-niko. The user explicitly authorized a handoff snapshot push on 1 October 2026; repository-root HANDOFF.md records its unfinished status and next work. This does not certify the production acceptance criteria.
