# The Human Eye — UI style guide

Native implementation: the saved Canvas prefab `Assets/HumanEye/UI/ObservatoryCanvas.prefab` owns the hierarchy (edited
by idempotent authoring patches, latest `Editor/UiPass.cs`); `Scripts/UI/Hud.cs` binds state and events by element name.
Chapter text stays in the authoritative ledger/content assets. This file is the design system of record: the Figma
account connected on 7 October 2026 is a Starter plan with a View seat (about 20 MCP calls a month), so the system could
not be built in Figma (file `UyMsflBNju6JlbE7tB9ftm` was created and is empty; a ready foundations script was prepared).

## Direction

Premium, medical, modern, educational, minimal, cinematic. The 3D subject is the hero; the interface sits at the edges.
No cyberpunk, no neon glow, no heavy glassmorphism, no giant floating panels.

## Tokens

Visual-match pass (8 October 2026): the seven art boards are the colour reference; panels moved from near-black navy to
the boards' caption navy, and the subject colours are tokens too, so anatomy and interface never borrow each other's hues.

| Token | Value | Use |
|---|---|---|
| ink/950 | #05080D | letterbox bars, scrims |
| caption navy | #07385D at 92 % | panels, caption and dialogue cards (art boards; was navy/900 #0A1220) |
| midnight | #092D50 at 95 % | controls, outlines (was navy/800 #0F1B2D) |
| line/600 | #24384F | hairline borders at rest |
| text/100 | #EEF4F8 | primary text |
| text/300 | #A9BBCB | secondary text |
| cyan glow | #26C6F5 (rim #5CD6FF) | focus, hover, the one required action, labels, captions speaker |
| luminous blue | #2388E8 | platform and ring light, info displays |
| ice blue / pearl | #CDEFFF / #ECF7FF | lab surfaces, Niko's shell |
| retina orange | #D97432 | inner eye (retina, choroid) only |
| neural gold | #FFC15E | optic-nerve signals only (light is a paler warm white) |
| brain coral | #C97679 | brain tissue; blue only on the visual cortex |
| teal | selected/current fill (HudState) | selected part, current chapter |
| amber/400 | #F5B63F (text #1A1203) | BEGIN only |
| coral | non-punishing wrong-answer accents | |

Type: Atkinson Hyperlegible. Title 66 bold caps (start); chip title 15 bold; body 18–21; captions 20/24/28/32 (four learner
sizes); labels 16; section labels 13–15 bold caps in cyan. Spacing 4-pt (8/12/16/24/32/48); safe margin 24 px.
Radius: panels 16, buttons pill (≤ 24), icon buttons 48 px circles. Minimum control 48 px; BEGIN 64 px.

## Surfaces and states (`UIGlass.shader`, `HudState`)

At rest a panel or control is a flat navy fill with a hairline line/600 border: no halo, no sheen. Hover/focus (glow ≈ 0.8)
fades in a cyan rim and a soft halo; *selected / current / pulse* (the one required action, glow 1.4) shows the full cyan
rim. *Unavailable* stays visible at low contrast and explains itself on press (toast). *Done* uses a checkmark.

## Layout (constant-pixel canvas; 1920×1080, 1600×900, 1366×768, 1024×768 verified by AutoPilot)

- **Top band (24 px from the top, 68 px high):** left, the chapter chip — "Chapter 3 of 10 · Title" and ten progress
  segments (upcoming line, current cyan, complete teal). Right, icon tools: Home, Chapters, Replay explanation, Pause,
  Help, Review, Sound, Settings (48 px circles).
- **Right column (400 px, 24 px margin, from 104 px below the top to 104 px above the bottom):** the information panel
  (Niko portrait + status, the current instruction) and the activity panel (Scan Eye / Explore Inside, experiment
  controls, parts list, view controls, insets). Panels hug their content; the activity panel is hidden when empty. The
  quick-check card and the end menu use the same column.
- **Captions:** bottom-centre of the 3D area (left of the column), max 980 px, ink scrim, speaker in cyan caps.
- **Talk to Niko:** a pill in the bottom-right corner (236×56); its panel opens above it, over the column.
- **Toasts:** under the chapter chip, top-left.
- **World labels:** navy pill, hairline cyan leader that starts at the pill edge, 8 px anchor dot on the structure;
  overlap relaxation, glide (no jumps), fade-in; only lesson-driven sets, never all labels at once.

## Screens

- **Start:** the preserved 3D starting view fills the screen; a left title column — "AN INTERACTIVE LESSON WITH NIKO",
  "THE HUMAN EYE", a one-line subtitle, BEGIN (amber) · HOW TO PLAY · CHAPTERS, then Continue from … · Settings, and the
  sound/input note. No chapter cards on the start screen.
- **Cinematic (intro, eye-to-brain):** letterbox bars (top and bottom 11.5 %); caption centred and Skip at the right in
  the lower bar (narration lines show there as the caption while a cinematic plays). Reduced motion shows the same bars
  without Skip.
- **Cinematic title** (`intro-title`, `intro-rule`, `intro-subtitle`, Builder.ApplyReference): the reference's hero title —
  bold 46 px capitals tracked with single spaces ("I N S I D E   Y O U R   E Y E S"), white, 2 px shadow, centred in the
  band 74.5–85.5 % of the height so the floating eye sits under it; a 280×2 px warm rule (#FFEDCC at 75 %) below it;
  the subtitle 20 px ink at 90 % under the rule. Typed letter by letter (the untyped rest is drawn transparent, so the
  line never moves), then the rule grows from its centre and the subtitle fades in. Titles: INSIDE YOUR EYES /
  "Discover How You See the World"; FROM EYE TO BRAIN / "Where Visual Signals Become Perception".
- **Explanation:** chip, tools, captions, Talk to Niko; the column shows Niko's status only.
- **Interaction / activity:** instruction + activity controls in the column; labels on the model; inspect hint as a toast.
- **Quick check and feedback:** card in the column; correct (check + teal), wrong (gentle reason + hint, then a
  demonstration — never a dead end).
- **Chapters:** modal with ten chapter buttons in two columns; reached chapters play, later ones say how they open.
- **Pause and settings:** existing modals on calm surfaces.

## Motion

Panel fades 0.2 s (popups) / 0.6 s (main reveal after the intro); labels glide; hover rims 0.4 s. Reduced motion removes
camera travel and glides. Cameras never move while the learner is asked to tap. Cinematic titles type over 2.4 s (production: the
same share of a shorter beat), the subtitle fades over 1 s; a gold disc burst marks the title hit.
