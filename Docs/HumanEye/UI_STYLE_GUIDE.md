# Observatory of Vision — UI style guide

Native implementation: the saved Canvas prefab `Assets/HumanEye/UI/ObservatoryCanvas.prefab` owns the hierarchy (edited by the idempotent authoring patches); `Scripts/UI/Hud.cs` binds state and events by element name. Chapter text stays in the authoritative ledger/content assets.

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

## UX/UI system (intro pass, 6 October 2026)

**Screen structure (desktop and tablet, landscape).** Top bar: chapter title, progress dots, Pause/Help/Review/Settings. Right-hand column (380 px + 16 px margins): information panel (chapter, Niko portrait + status, instruction, captions, Talk to Niko) above the View / Activities panel (Scan Eye / Explore Inside, experiment controls, parts, checks, popups). Bottom strip: lab tools (Explore lab, Replay intro, Sound). The 3D subject is composed in the area left of the column (`ViewportShift`, lens shift ≤ 20 % of the width).

**Rule: the interface never covers the subject.** Persistent controls stay at the edges; nothing floats over the centre. World labels avoid the column, the characters and each other.

**Cinematic moments.** During the intro the whole interface is hidden; only the intro overlay shows: a slim 56 px strip 24 px above the bottom edge with a small caption (18 px, 82 % ink) on the left and a ghost "Skip intro" button (160×48) on the right. Reduced motion shows the same strip without Skip (nothing to skip). When the intro ends the top bar and the column fade in over ~0.6 s and the lens shift eases in with them, so the picture re-composes as the panels arrive.

**Panels.** Navy glass (#091625 at 92 %), 18 px radius, 1 px #3F6377 border, 10 % cyan glow on focus. 14 px inner padding, 8 px between rows, 12 px between panels. Titles 15–16 px uppercase tracking in the panel header; body 18–20 px; captions 20–32 px (four learner sizes).

**Buttons and states.** Default raised #13283B with #3F6377 border; *primary* (the one required action) cyan #69E0F0 fill with navy text and a pulse; *selected* left cyan bar + arrow; *done* checkmark prefix; *unavailable* 45 % opacity, still visible, explains itself on press; *ghost* (overlay/skip) transparent with a 1 px border. Minimum 48 px hit target; icons 20 px line icons (HudIcon) left of the label; never icon-only for primary actions.

**Typography.** Atkinson Hyperlegible throughout. Weights: regular for body, bold for titles and the current instruction. Line length ≤ 60 characters inside the column; long labels wrap, never clip.

**Responsive.** Constant-pixel canvas; column width fixed, 3D area takes the rest. 16:9, 16:10 and 4:3 are verified (1920×1080, 1600×900, 1366×768, 1024×768). Below 1360 px wide the top bar uses compact labels; the column never exceeds 40 % of the width. Camera FOV is aspect-fitted so authored horizontal framing survives 4:3.

**Motion.** Panel fades 0.2 s (popups) / 0.6 s (main UI reveal); label glides 0.3 s; no bounce. Reduced motion removes glides and the intro travel.

**Figma.** The connected Figma file (`U4QDtnkYPFaBZBUVYIL4Dg`) holds the variables, button variants and overlay frames from the earlier passes. The intro overlay and the fade-reveal states are implemented in Unity and documented here; mirroring them into Figma needs the Figma connector authorised in this environment (not available on 6 October 2026).
