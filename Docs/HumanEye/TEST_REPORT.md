# Test report: Human Eye module (Scenes 2–11)

Run on 2 October 2026 on the development PC only:

- Windows 11 Home 22631
- Unity 6000.3.0f1
- NVIDIA GeForce RTX 3050 Laptop GPU, driver 32.0.16.1060

Raw logs live in `Logs/` and walkthrough captures in `Builds/Checks/`. Both folders are git-ignored, so the key numbers are copied here and a curated set of screenshots is in `Screenshots/`.

## Summary

| Check | Result | Evidence |
|---|---|---|
| Module build (`Builder.Build`, batch) | Succeeded | `Logs/he/build6.log` |
| Validator (final, Windows target) | **70 passed · 0 warnings · 0 errors** | `VALIDATION_REPORT.md`, `Logs/he/validate_final.log` |
| EditMode tests | **20 / 20 passed** | `Logs/tests/editmode-results.xml` |
| PlayMode test `FirstRunReachesFinalCuriosity` | **Passed** (165.0 s): Scene 2 → Scene 11, 66 cues, Pause keeps `timeScale` = 1 | `Logs/tests/playmode-results.xml` |
| Windows player (Development) | **Succeeded**, 207.2 MB | `Logs/he_player_builds.txt` |
| Android APK (Development, ARM64, API 29) | **Succeeded**: `HumanEye.apk`, 80.9 MB (Unity reports 1,097 MB of uncompressed content). The first build took 15 min (IL2CPP); incremental rebuilds take 1–2 min. **Not installed on any device.** | `Logs/he/android3.log` |
| Full first-run walkthrough, Windows player, 1600×900 (run5) | **10 / 10 chapters, 66 / 66 cues started and completed with audio** | `Builds/Checks/run5/walkthrough.log` |
| Layout captures 4:3 (1440×1080), 16:10 (1600×1000), portrait (540×960) | Completed; layouts reviewed (see below) | `Builds/Checks/run6_4x3`, `run6_16x10`, `run6_16x9`, `layout_portrait` |
| Reduced motion (`-heReduced`), Scenes 2–3 | Completed; all 28 cues kept; Scene 3 reached after 43 s instead of 61 s | `Builds/Checks/reduced_motion` |
| Clean exit after a full run | **Intermittent D3D12 crash at process exit** (see Known defect); D3D11 exits cleanly | Windows Application log, event 1000 |

## EditMode tests (20)

- **Geometry and optics:** `AccommodationFocusesOnRetina`, `PupilIsAnOpening`, `RevolvedShellNormalsPointOutward`, `TriangulateSquare`.
- **Learner model:** `AssistedAnswersAndExposureNeverProveMastery`, `NextNeverRepeatsLastItemAndStartsLow`, `SupportTiers`, `TwoDistinctUnassistedCorrectMakeSecure`.
- **Ledger:** `CsvHasExactColumnsAndOrder`, `FilenameRule`, `IntentionalRepeatsKeepSeparateIds`, `SixtySixCuesWithSceneCounts`, `TextMatchesStoryboardExactly`.
- **Lesson and module:** `GeneratedModuleValidates`, `ScheduledRoutineCancelsNestedWork`, `StateRules`.
- **Ask Niko:** `MapsRequestsToWhitelistedIntents`, `UnsupportedAndInjectionTextMatchNothing`.
- **Saves:** `CorruptSaveStartsFreshWithNotice`, `RoundTrip`.

One PlayMode run today **failed**. It was a regression from an early `ProfilerRecorder` dispose added to the walkthrough driver (`InvalidOperationException` after the run finished). The change was reverted and the re-run passed.

## Behaviour checks from the walkthrough log (run5)

The walkthrough driver presses the same public actions as the HUD buttons. It does not generate real mouse or touch events.

| Check | Logged result |
|---|---|
| Pause preserves position | `timeScale=1 voiceTimeBefore=0.38 after=0.38 stillPaused=True` |
| FREEZE stops only the biology | Accommodation 0.565 before and 0.565 after 1.5 s, frozen mid-change |
| Brightness: the opposite direction is refused with a reason | "dimmer during 'brighter' was refused (reason toast)" |
| Ask Niko routes to a whitelisted action | "What does the coloured part do?" → explore iris; "make it brighter" → brighter |
| Ask Niko refuses an unsupported question | "why is the sky blue?" → unsupported |
| Completion | `completed=Scene02…Scene11 cues=66 evidence=7` |

## Performance (run5)

Windows Development player, 1600×900 windowed, VSync on, default D3D12, PC quality level. Values come from `ProfilerRecorder`, averaged per chapter. The walkthrough runs at 3× narrative speed, which only shortens waits and does not change rendering.

| Chapter | Avg FPS | Main thread | Draw calls avg / max | SetPass | Triangles | System memory peak |
|---|---|---|---|---|---|---|
| Scene02 | 58.5 | 17.1 ms | 130 / 202 | 46 | 289k | 790 MB |
| Scene03 | 58.5 | 17.1 ms | 105 / 146 | 42 | 105k | 792 MB |
| Scene04 | 58.4 | 17.1 ms | 124 / 186 | 44 | 206k | 810 MB |
| Scene05 | 58.2 | 17.2 ms | 99 / 173 | 41 | 231k | 817 MB |
| Scene06 | 58.0 | 17.2 ms | 109 / 123 | 47 | 115k | 819 MB |
| Scene07 | 57.4 | 17.4 ms | 99 / 118 | 50 | 175k | 959 MB |
| Scene08 | 57.7 | 17.3 ms | 272 / 383 | 77 | 136k | 980 MB |
| Scene09 | 58.6 | 17.1 ms | 124 / 370 | 53 | 230k | 986 MB |
| Scene10 | 58.3 | 17.1 ms | 203 / 237 | 61 | 338k | 1014 MB |

- The main-thread figure includes the VSync wait at 60 Hz.
- Worst single frames (78–122 ms) happen at chapter transitions, during asset activation and the first use of shaders.
- Scene 11 has no row because the driver logs performance when a chapter changes, and Scene 11 ends the module.
- These numbers show the PC target is met on this laptop. They are **not** a tablet measurement.

## Build state the evidence comes from

- **Final Windows player (14:08 UTC):** run6_16x9, run6_16x10, run6_4x3 and the final validator ran on it.
- **Earlier build:** run5 (performance, full first run), layout_portrait and reduced_motion ran on a build that differs only in HUD layout:
  - the label band and toast position;
  - world labels hidden behind activity cards (run6_s08 shows this fix).
- **Tests:** EditMode and PlayMode ran on the final runtime code, except the label-band constant (268 → 280), which no test covers.
- **Android APK:** rebuilt last, from the final code.
- **Setting leak, fixed:** the `-heReduced` run saved reduced motion in PlayerPrefs, so the next runs inherited it (the D3D11 exit check and one 4:3 run). The walkthrough driver now sets the flag explicitly both ways. The affected 4:3 run was redone in normal motion (111 s for Scenes 2–3, versus 72 s with reduced motion).

## Layout review

| Layout | Result |
|---|---|
| 16:9, 1600×900 | Reviewed through the whole run |
| 4:3, 1440×1080 | Top bar, banner (one line), part list, view controls and action bar fit. Optional-check cards are centred. |
| 16:10, 1600×1000 | Same as 16:9 with more vertical room |
| Portrait, 540×960 | Fallback works: top buttons wrap, the action bar wraps to two rows, cards fit the width, subtitles stay above the action bar |

These problems were found during the review and fixed in this build:

- **Prompt and toast:** a two-line prompt banner collided with the discovery toast. The banner is now wider and the toast sits below it.
- **World labels:** labels could hide under the banner and the subtitles. They are now clamped between the toast band and the subtitle band.
- **Activity card:** world labels showed through the translucent card, so they are hidden while a card is open.

## Known defect: intermittent crash at process exit (D3D12)

`Application.Quit` after Scene 6 or Scene 11 sometimes ends in an access violation inside `D3D12Core.dll`, the Agility SDK bundled with the player. The fault offset is always 0xa1f5, and Windows Application Error event 1000 records it.

| Run | API | Crash on exit |
|---|---|---|
| run3 and run4, full first run | D3D12 | yes, both |
| Scene 11 only | D3D12 | yes |
| Scene 6 only | D3D12 | yes |
| run5, full first run | D3D12 | no |
| Scene 8 only; quit at start (×2) | D3D12 | no |
| Scene 11 only | **D3D11** (`-force-d3d11`) | **no** |
| Full first run (this run inherited the reduced-motion setting, see below) | **D3D11** | **no** |

What this means:

- The crash happens after the last frame, during graphics teardown.
- Progress is not affected, because `SaveSystem` calls `PlayerPrefs.Save()` on every write.
- No script code is on the crashing stack, and disposing the profiler recorders early did not change it.
- **Workaround:** launch with `-force-d3d11`, or list Direct3D 11 first under *Player Settings > Windows > Graphics APIs*. The project setting is still Automatic (D3D12), because it is project-wide and was not changed without the owner's approval.
- To verify the fix: test on another GPU or driver version and on the next Unity 6000.3 patch.

## Not tested

The items below were not tested. The reasons are in `MISSING.md`.

- A physical Android tablet: install, touch, safe areas, audio start and frame rate.
- Any Web build, because the WebGL module is not installed.
- A Windows touchscreen.
- Real mouse input as opposed to driven actions.
- A human listening pass of the provisional TTS.
- A specialist review of the anatomy.
