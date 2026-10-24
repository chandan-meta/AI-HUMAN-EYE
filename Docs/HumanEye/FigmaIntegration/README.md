# Figma artwork in the saved Unity HUD

24 October 2026

The saved `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` now uses actual PNG artwork exported from [Vaibhav's Pearl + Iris Figma file](https://www.figma.com/design/wLgTj7N6mpfRONuYkmZac0?node-id=75-478). The export board is `75:478`.

The 43 source PNGs are in `Assets/HumanEye/UI/Figma/`. They include pearl panels, caption surfaces, violet and amber button states, selected rows, progress marks, slider artwork, icons, the start brand and iris header decoration. `FigmaSource.json` records each original design node, export node, dimensions, export scale, slicing borders and export method. Labels remain live Unity text; `StartBrand.png` includes the static title typography.

The existing Canvas, lesson actions and input components are retained. The saved scene contains 165 Buttons and 159 HUD bindings. Button surfaces use the imported sprites; `HudState` switches their exported default, hover, pressed, focus, selected and disabled artwork. Input fields, toggles, slider tracks and scroll backgrounds retain pointer input. Decorative graphics do not intercept clicks. This integration adds no runtime UI generation or procedural artwork.

## Update exported artwork

For PNG pixel updates, replace the corresponding PNGs in `Assets/HumanEye/UI/Figma/` while preserving their filenames, dimensions and existing `.meta` files. Unity reimports the artwork into the existing sprites without changing the saved Canvas or its references. Update `FigmaSource.json`, provenance and alpha checks when the export source or method changes, then inspect representative desktop and tablet captures.

**Tools > Human Eye > Figma > Apply exported PNG artwork** in Unity 6000.3.0f1, or the batch entry point `HumanEye.EditorTools.FigmaSkin.Apply`, is an optional reset of the integration's UI styling. It imports sprites with alpha support and the manifest's slicing borders, reapplies panel and control styling and the start-card layout, and saves the launch scene. Save subsequent manual UI work before intentionally running it again. Normal Play and player builds use the saved result. Full module regeneration is unnecessary and would replace authored scene changes.

## Evidence

- `apply.txt`: completed import, 43 sprites, 165 retained Buttons and 159 retained bindings.
- `asset-provenance.json`: all 43 source paths, original/export node IDs, export methods and SHA-256 hashes; 743,447 PNG bytes in total. All 43 are native transparent exports and the pending list is empty: 36 use `native-exportAsync-contentsOnly` and seven use `figma-browser-native-png`.
- `png-alpha-checks.json`: decoded dimensions, alpha ranges and hashes for all 43 native exports. Independent verification confirmed RGBA PNGs, alpha minima of 0–2, alpha maxima of 115–255, nonempty artwork and matching provenance. `Library/FigmaPNGTransfer/final-checks.json` additionally records the seven completed browser exports, their source ZIP entries, dimensions, hashes and decoded alpha checks; every corner alpha in those seven is zero.
- `Figma_Source_Export.png`: the signed-in Figma export source and native PNG export settings used to complete the seven larger assets.
- `BeforeTransparentExports.json`: the scene, PNG and metadata hashes recorded before the transparency cleanup. All 43 sprite `.meta` files and the manifest `.meta` are unchanged; the saved scene is also byte-identical. The replacement PNGs retain their original dimensions and sprite references.
- `BeforeFigma.unity`: the scene snapshot saved before the first application.
- `scene-invariants.json`: before/after scene hashes and exact comparison of 1,532 complete serialized blocks. The tracked 2 Cameras, 29 Lights, 352 MeshRenderers, 2 SkinnedMeshRenderers, 352 MeshFilters, 8 AudioSources and 1 Animator, plus their 393 associated GameObjects and 393 Transforms, are unchanged.
- `Logs/figma/editmode-final.xml` and `Logs/figma/editmode-final.log`: 30 of 30 EditMode tests passed. These include saved sprite references, button and other input surfaces, decorative raycast safety and layer inheritance, inset text contrast, HUD binding integrity, saved slots and state-sprite switching.
- `Logs/figma/editmode-transparent.xml` and `Logs/figma/editmode-transparent.log`: all 30 EditMode tests passed again with the 36 native transparent PNGs installed.
- `Logs/figma/editmode-complete.xml` and `Logs/figma/editmode-complete.log`: all 30 EditMode tests passed with all 43 native transparent PNGs installed.

## Runtime validation

The integration's Windows development build succeeded before the transparency cleanup: 795.6 MB, 14.876 seconds, zero errors and two warnings. Evidence: `Logs/figma/windows-build-final.log`; build completed at 11:38:07 IST on 24 October 2026. The following completed walkthroughs preceded the PNG-only transparency cleanup and remain the full-lesson coverage record.

| Completed run | Coverage | Time | Passed / failed checks | Pointer audits | PNGs |
|---|---|---:|---:|---:|---:|
| Initial desktop, 1920×1080 | All ten chapters | 145.08 s | 45 / 0 | 93 | 93 |
| Final tablet layout, 1024×768 | All ten chapters | 133.80 s | 45 / 0 | 92 | 92 |
| Final desktop, 1920×1080 | Three-chapter regression | 57.20 s | 25 / 0 | 37 | 37 |

All three runs exited with code zero, zero blocked pointer targets and zero controls outside the screen. The initial full desktop run preceded the inactive primary-button label colour fix; the final tablet and desktop runs include that fix. Per-run evidence is in `Builds/Checks/figma-desktop/`, `Builds/Checks/figma-tablet-final/`, `Builds/Checks/figma-desktop-final/` and the corresponding `Logs/polish/` player logs.

### Transparent PNG regression

The Windows development build with 36 native transparent PNG exports succeeded: 797.0 MB, 15.234 seconds, zero errors and two warnings. Evidence: `Logs/figma/windows-build-transparent.log`; build completed at 12:14:42 IST on 24 October 2026.

| Completed run | Coverage | Time | Passed / failed checks | Pointer audits | PNGs |
|---|---|---:|---:|---:|---:|
| Desktop, 1920×1080 | Three-chapter regression | 54.58 s | 25 / 0 | 36 | 36 |
| Tablet layout, 1024×768 | Three-chapter regression | 48.13 s | 25 / 0 | 36 | 36 |

Both runs completed with zero blocked pointer targets and zero controls outside the screen. Their checks covered lesson activities, settings, volume and sound controls, XL captions, Help, pause/resume, Review and Ask Niko. Evidence is in `Builds/Checks/figma-transparent-desktop/`, `Builds/Checks/figma-transparent-tablet/` and the corresponding `Logs/polish/` player logs. Actual settings and iris-activity captures were visually reviewed at both sizes: panel and button corners now show the surrounding scene correctly, and the exported icons retain transparency.

Activity previews from the 36-export regression: [desktop iris activity](Unity_Desktop.png) and [tablet iris activity](Unity_Tablet.png), copied directly from Unity captures.

Existing 42 px and 44 px control heights produce small-target diagnostics: the historical 36-export regression recorded 100 desktop entries and 96 tablet entries, including repeated observations across captures. The earlier full tablet and desktop runs recorded 110 and 108 entries respectively. Automated pointer reachability passed; physical tablet touch validation remains pending.

### Complete 43-export regression

The final Windows development build with all 43 transparent PNGs succeeded: 797.5 MB, 12.843 seconds, zero errors and two warnings. Evidence: `Logs/figma/windows-build-complete.log` and the latest entry in `Logs/he_player_builds.txt`; build completed at 12:29:01 IST on 24 October 2026. All 30 EditMode tests also passed, as recorded in `Logs/figma/editmode-complete.xml`.

| Completed run | Coverage | Time | Passed / failed checks | Pointer audits | PNGs |
|---|---|---:|---:|---:|---:|
| Desktop, 1920×1080 | Three-chapter regression | 59.15 s | 25 / 0 | 37 | 37 |
| Tablet layout, 1024×768 | Three-chapter regression | 53.26 s | 25 / 0 | 37 | 37 |

Both runs exited with code zero, zero blocked pointer targets and zero controls outside the screen. The desktop run recorded 104 small-target diagnostics; the tablet run recorded 100, including repeated observations across captures of existing 42 px and 44 px controls. Evidence is in `Builds/Checks/figma-complete-desktop/`, `Builds/Checks/figma-complete-tablet/` and the corresponding `Logs/polish/` player logs. Final desktop and tablet captures were visually reviewed: rounded transparency is clean, with no obvious visual issue at either resolution. Physical tablet touch validation remains pending.

Current start-screen previews with all 43 transparent exports: [desktop](Unity_Start_Desktop.png) and [tablet](Unity_Start_Tablet.png). These are unchanged copies of each final run's `001_start_screen.png`.

## Completed transparent export cleanup

All 43 PNGs have been replaced with native Figma exports that preserve transparency. The original wrapper frames already had empty fills. The `download_assets` rendering path flattened the page backdrop into the PNGs; native `node.exportAsync({ format: 'PNG', contentsOnly: true, useAbsoluteBounds: true })` preserved alpha for 36 assets.

The remaining seven assets—`ButtonStart_Default`, `ButtonStart_Hover`, `ButtonStart_Focus`, `ButtonStart_Selected`, `ButtonStart_Disabled`, `IrisCrest` and `StartBrand`—were exported through the signed-in Figma browser's native PNG export. The connector remained unauthorized, but the browser export completed successfully. Every installed PNG is complete, validated against its recorded source and hash, and recorded in the updated provenance; there are no pending exports.

The source artwork was not redrawn or altered. Only exported PNG pixels and export records changed: the saved Unity UI and scene, all 43 PNG `.meta` files and the manifest `.meta` are unchanged from the transparency-cleanup baseline. The existing sprite references therefore remain intact. The final editor tests, Windows build and desktop/tablet regressions completed successfully as recorded above.

The prior Pearl + Iris material comparison and Figma preview documents remain in `Docs/HumanEye/PearlIrisRefresh/`. This UI integration does not apply the candidate retina material to production anatomy.

24 October 2026: later start-banner composition and cinematic lighting/VFX polish are recorded in [Cinematic intro and start banner](../Polish/CINEMATIC_INTRO_2026-10-24.md), with actual desktop/tablet Unity previews. The 43 Figma PNGs and their sprite metadata remain unchanged by that pass.
