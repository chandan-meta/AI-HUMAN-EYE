# Canvas glow validation — 3 October 2026

The saved Canvas now uses editable rounded glass surfaces, cyan active/hover/focus halos, vector icon children, circular progress markers, menu arrows, and compact narrow-button layouts. This supersedes the flat Canvas styling documented in CANVAS_VALIDATION.md.

- `Logs/glow/playmode.xml`: complete Play Mode test passed all 10 chapters and 66 cues, checking that the initial 562 UI RectTransform instance IDs are unchanged after gameplay. Three static menu-arrow children were subsequently saved (565 total); no runtime creation code was added.
- `Builds/Checks/glow-final-1672/walkthrough.log`: full Windows walkthrough, 10 chapters, 66 cues, 21 interaction checks passed, zero failures. This visual iteration exposed a 30px Home target; it was enlarged to 48px before the final build.
- `Logs/glow/editmode-final.xml`: 22 Edit Mode checks passed after documenting optional glass references on plain text slots. Includes saved material/channel checks and active/unavailable glow-state checks.
- `Builds/Checks/glow-verified-1024`: first chapter, 16 cues, no small or offscreen controls. Visual inspection caught narrow header text wrapping, subsequently fixed with Compact Button Layout.
- 771 non-prefab scene GameObjects existed before and after styling; none were removed. Backups are in `Logs/glow`.

Checks use native submit events and accelerated playback, not physical mouse/touch automation. Windows build only; the existing Android APK predates this work. Reference images are art-direction targets: 3D assets, lighting, typography and exact composition still differ. The glass effect is a translucent gradient and soft rim, not real-time background blur. No pixel-identical claim is made.

## Delivered build checks

`Logs/glow/delivery-build.log`: Windows Development build succeeded, 0 errors and 1 symbol-upload warning. `Logs/glow/delivery-editmode.xml`: all 22 checks passed on the final 565-object prefab.

Final visual captures were inspected at 1672×941 (`Builds/Checks/glow-delivery-1672`, menu and Scene 3 including all five parts) and 1024×768 (`Builds/Checks/glow-delivery-1024`, menu, Scene 2 and dialogs). Both runs reported no small/offscreen controls or failed interaction checks, and no runtime/shader exceptions. Compact top-button labels are readable and the Parts panel encloses its rows. The final captures, not earlier glow iterations, are the current visual evidence.

## Pointer repair — 4 October 2026

The user found a real input failure that the earlier submit-only checks missed. Home and 24 other custom graphics lacked saved CanvasRenderer components. HudIcon/GlassPanel now require CanvasRenderer, and all 506 renderers are saved in the prefab. All 565 UI objects and 139 Buttons were retained.

Verification: `Logs/pointer-fix/delivery-editmode.xml` passes all 22 checks, including renderer presence on every saved Graphic and raycast-enabled click surfaces on all Buttons. `Logs/pointer-fix/playmode-final.xml` passes the full 10-chapter/66-cue test with Start, SCAN and Inside View reached through EventSystem pointer hits; unchanged UI instance IDs are also verified.

The wider player pointer audit exposed comparison cards blocking Turn/Zoom in later scenes. The saved cards, prompt and view column have been separated. Non-selectable world annotations are now marked non-interactable, matching their pre-existing non-raycast behaviour. Menus are scrolled to their target before test clicks; the audit does not pretend off-viewport controls are visible.

The batch Editor's 640x480 Game view is below the tested layout size, so its all-controls audit is explicitly skipped; standalone players at 1024x768 and above provide that coverage. Initial failing diagnostic runs are retained in Logs/pointer-fix and Builds/Checks/pointer-1024 / pointer-final-1024. They are not passing acceptance evidence. The latest Windows executable was rebuilt after both fixes; Android was not rebuilt.

Final pointer result: `Builds/Checks/pointer-delivery-1024/walkthrough.log` completed all 10 chapters / 66 cues with zero pointer-audit failures, zero layout-target failures and 21 passing interaction checks. It reached 90 distinct active Buttons across the walkthrough; unused pooled/inactive slots were checked structurally among the 139 saved Buttons rather than clicked in nonexistent lesson states. Start, SCAN, Inside, TRACE, Home, settings, Help, Review, Ask, pause/resume, confirmation and saved-resume paths used raycast-selected pointer clicks. `Logs/pointer-fix/delivery-1024.log` contains no runtime exceptions. Visual inspection confirmed comparison cards no longer cover Turn/Zoom. These are in-engine pointer events, not OS mouse automation.
