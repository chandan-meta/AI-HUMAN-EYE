# October 3 implementation and validation

Active design: https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg

All runs below exited successfully. Full journeys completed Scenes 2–11 and all 66 core cues; smoke runs intentionally stopped after menu/initial narration. Content source files HumanEyeContent.cs and DialogueLedger.json have no diff from HEAD.

| Run | Scope | Explicit PASS / FAIL | Layout samples | Small/outside controls |
|---|---|---|---|---|
| polish-r3-1600 | Full 1600×900, stress | 20 / 0 | 118 | 0 |
| polish-r3-fixed-1024 | Full 1024×768, stress, reduced motion | 20 / 0 | 132 | 0 |
| polish-r4-1920 | Final full 1920×1080, stress | 21 / 0 | 119 | 0 |
| polish-r4-1280 | Final full 1280×720, reduced motion | 2 / 0 | 116 | 0 |
| polish-r4-1024-menu | Final 4:3 menu smoke | 2 / 0 | 4 | 0 |
| polish-r4-1600-menu | Final 1600×900 menu smoke | 2 / 0 | 4 | 0 |

The stress runs exercise the real UI Toolkit submit-event bindings. They are not physical mouse/touch hit tests. Layout audits exclude scroll content and noninteractive progress labels and do not detect all overlaps.

Unity suites: Logs/polish/round4-editmode.xml has 21 passed, zero failed; round4-playmode.xml has one passed full walkthrough, zero failed (164.5 seconds). The edit-mode suite includes the added floor-facing regression test. Source diff whitespace check passed with Windows CRLF handling.

Windows final candidate: Logs/polish/round4-build.log, 208.5 MB, zero errors, one warning. The existing Unity Cloud symbol-upload warning does not prevent the development build. Runs use D3D11.

Android final development build: Logs/polish/round4-android.log, succeeded 2026-10-03 07:06 UTC, zero errors/two warnings. APK at Builds/HumanEye/Android/HumanEye.apk is 81,794,946 bytes (78.0 MiB). Unity BuildSummary reports 1100.4 MB including other build outputs, not the compressed APK size. No physical Android installation/performance test was performed.

Visual defect/fix evidence: first round-three capture exposed downward-facing floor geometry; disc profile winding fixed, generated-normal test passed, floor then visible. 4:3 menu initially obscured Niko; menu anchor moved left and final 4:3/1080p captures show him beside the card. Ambient particles now honour Pause/Freeze and stop under reduced motion; final stress run checks Pause behind Help.

Presentation limits: procedurally built room and character remain less detailed than the concept renders. Physical tablet/Windows touch, separate-process save restoration, audio listening, exhaustive overlap review and production art acceptance remain outstanding. These checks do not certify device performance or completion of every requirement in the four-round brief.
