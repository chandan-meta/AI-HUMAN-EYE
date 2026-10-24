# Cinematic intro and start banner — 24 October 2026

The 21-second “eye awakens” opening now emphasizes the eye with controlled rim, key and fill lighting, a darker hall and emissive sky, and background depth of field. Light triggers the pupil response; the camera reveals the cutaway, follows light to the retina and presents the title before restoring the lesson view.

Existing particles provide depth dust, cutaway motes, cyan reveal rings and a small warm retinal spark. Pause holds the clock and particles; skip, replay and reduced motion retain cleanup. A private runtime sky copy preserves the saved sky asset. Lighting and camera changes restore the lesson state.

## Start banner and source artwork

The saved start card is 720 px wide with 36/36/36/32 px padding and 20 px spacing. Begin Journey is 80 px high; Continue and secondary actions are 64 px. Existing Figma icons and a faint iris watermark enrich the composition. `StartBrand.png` supplies the static title; live controls retain their behavior.

Figma connector access was unauthorized and the browser was view-only. The banner uses the 43 existing native Figma PNGs, with no artwork redrawn or new Figma art created. The saved scene retains 165 Buttons and 159 HUD bindings.

`Logs/cinematic-20261024/preservation-check.json` confirms all saved scene changes are within the start card: 6,326 of 6,358 blocks unchanged, 30 modified and two replaced. Saved cameras, lights, models, lab and other Canvas content are unchanged, as are all 88 PNG, metadata and manifest files.

Implementation: `Stage.Opening.cs`, cleanup in `Stage.Scene02.cs`, the focused `Editor/IntroBannerPolish.cs` authoring command and `CinematicTests.cs`. The saved launch scene is `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`; module regeneration is unnecessary.

## Verification

Evidence is under `Logs/cinematic-20261024/` and the listed `Builds/Checks/` runs.

| Check | Result |
|---|---|
| EditMode, `editmode-final.xml` | 30/30 passed, 5.915 s |
| PlayMode, `playmode-final.xml` | 4/4 passed, 86.623 s; pause, particles, skip/replay, sky restoration and reduced motion covered |
| Windows development build, `build-verified.log` | Succeeded, 797.6 MB, 14.899 s, zero errors, two warnings; 13:18:32 IST |
| Normal intro, 1920×1080, `cinematic-final-desktop` | 28.18 s, 2 PASS / 0 FAIL, 31 pointer audits, 37 captures |
| Normal intro, 1024×768, `cinematic-final-tablet` | 26.01 s, 2 PASS / 0 FAIL, 31 pointer audits, 37 captures |
| Reduced motion, 1024×768, `cinematic-final-reduced` | 12.99 s, 2 PASS / 0 FAIL, 14 pointer audits, 20 captures |
| Three-chapter regression, `cinematic-accepted-stress` | 56.43 s, 25 PASS / 0 FAIL, 36 pointer audits, 36 captures |
| Final tablet check, 1024×768, `cinematic-verified-tablet` | 22.70 s, one chapter / 15 cues, 21 PASS / 0 FAIL, 22 pointer audits, 22 captures |

All five runs exited successfully with zero blocked targets or controls outside the screen. Intro runs had no small-target diagnostics; the three-chapter and final tablet checks recorded 104 and 36 repeated observations of existing small lesson controls. Timings include capture and post-intro checks. Editor tests preceded the QA-only one-frame audit correction; rebuilds and stress regressions verified that correction.

The first stress run (`cinematic-final-stress`, 66.07 s) had 25 PASS and one pointer-audit FAIL before a newly activated graphic's first drawn frame. Waiting one frame before raycast checks fixed that audit timing; assertions remain intact. The accepted regression above passed.

## Captures

These PNGs are unchanged copies of actual final Unity player captures:

| View | Desktop, 1920×1080 | Tablet layout, 1024×768 |
|---|---|---|
| Start banner | [Start](Captures/cinematic-2026-10-24/start-desktop-1920x1080.png) | [Start](Captures/cinematic-2026-10-24/start-tablet-1024x768.png) |
| Cutaway | [10.7 s](Captures/cinematic-2026-10-24/cutaway-desktop-10.7s.png) | [10.7 s](Captures/cinematic-2026-10-24/cutaway-tablet-10.7s.png) |
| Title | [16.0 s](Captures/cinematic-2026-10-24/title-desktop-16.0s.png) | [16.7 s](Captures/cinematic-2026-10-24/title-tablet-16.7s.png) |

Physical tablet touch and sustained performance remain untested. The captured tablet layout is a Windows player at 1024×768.
