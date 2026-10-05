# Observatory of Vision — UI style guide

Native implementation: `Assets/HumanEye/UI/Hud.uxml` owns the shared hierarchy; `Hud.uss` owns styles; `Scripts/UI/Hud.cs` binds state and events. Chapter text stays in the authoritative ledger/content assets.

- Palette: navy #091625 panels, raised #13283B controls, #3F6377 borders, #69E0F0 cyan focus, #EEF6FA text. Amber keyboard focus; green checkmarks for completed items. Important text has 96% opaque backing.
- Typeface: existing Atkinson Hyperlegible. Body 20–24 px; control 16–18 px; titles 30–44 px. Long labels wrap.
- Shape/spacing: 16 px panels, 10 px buttons, 8/12/16/24 spacing. No full-screen blur or per-button bloom.
- Attention: teaching subject first, instruction/current required action second, supporting tools third. Cyan filled controls signal the current required action; selected parts use a left border and arrow. Completed parts use checkmarks.
- Input: native click/touch, minimum 48 px button targets. Constant-pixel panel scale, compact rules below 1360 px, portrait fallback. Progress dots are noninteractive labels.
- Accessibility: captions and four sizes, visible keyboard focus, status text/checkmarks, reduced-motion transitions. Essential interaction never depends on hover.
- Modal/card content stays editable. Optional practice is labelled as optional; internal authoring tags remain in content data only.

Figma file: https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg
The new connected account owns this file. It contains shared variables, typography styles, ten button variants, reusable panel/navigation families and editable UI overlay frames, including a 4:3 lens layout. The old file remains empty and should not be used.

Menu composition: centred navy card, 780px desktop / 600px compact, maximum 68% width; 24px corners, cyan rim and 8% lower safe margin. Niko stands outside the card on the left; eye/brain frame the card above and to the right. CameraDirector expands vertical FOV at narrower landscape aspect ratios to preserve the authored horizontal field.

World presentation: warm brass ribs and fixtures; one 128px room reflection capture on load; restrained cyan inlays; deterministic project-owned nebula texture; opaque exterior eye before the inside-view lesson. Anatomy responds to display/rim lights. Selection uses a restrained rim, preserving transparent structures. Ambient particles pause/freeze and stop under reduced motion.

Figma is a UI specification, not a flattened render of the 3D environment or proof of pixel parity with the supplied concept images. Exact IDs and visual-review limits are in Polish/FIGMA_STATE.md.
