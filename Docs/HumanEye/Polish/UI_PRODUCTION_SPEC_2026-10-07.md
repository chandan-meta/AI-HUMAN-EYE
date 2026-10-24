# Human Eye — production UI specification (7 October 2026)

Source of truth for the production UI pass. Figma mirror: https://www.figma.com/design/FFA1bcD7hMTxg6QbjA34iR
(account-owned file; the earlier file `U4QDtnkYPFaBZBUVYIL4Dg` belongs to another account and is no longer editable).
Unity implementation: the saved Canvas prefab `Assets/HumanEye/UI/ObservatoryCanvas.prefab`, restyled and re-laid-out by an
idempotent editor patch; `Scripts/UI/Hud.cs` keeps binding elements by their kebab-case keys.

Reference behaviour: the Human Brain lesson (letterboxed cinematics, bottom-centre captions, a quiet stage pill, a slim
companion dock). The eye module must read calmer and more premium: medical, minimal, cinematic. The 3D eye is the hero.

## 1. Principles

1. The subject is never covered. Persistent UI lives in the four corners; the right-hand activity panel only exists while
   there is something to do, inside a reserved zone the camera composes around (`ViewportShift`).
2. One required action at a time, and it is the only amber-filled control on screen.
3. Cinematic moments show captions and Skip only, inside letterbox bars.
4. Every press responds within 100 ms (pressed state + sound); unavailable actions explain why (toast).
5. No neon: no outer glows, no bloom on UI, no glass blur. Hairline borders, soft shadow, solid navy surfaces.

## 2. Tokens

| Token | Value | Use |
|---|---|---|
| `surface/scrim` | #050A12 | letterbox bars, modal scrim (70 %) |
| `surface/panel` | #0B1522 at 94 % | panels, cards, caption backing gradient start |
| `surface/raised` | #13202F | default buttons, list rows, inputs |
| `surface/hover` | #1A2B3E | hover / pressed-secondary |
| `border/hairline` | #22344A | 1 px panel and control borders |
| `border/strong` | #35516C | focused inputs, dividers in dense lists |
| `text/primary` | #F2F6FA | titles, captions, body |
| `text/secondary` | #A6B5C5 | descriptions, helper text |
| `text/tertiary` | #6C7E92 | metadata, disabled labels |
| `accent/cyan` | #4FD8EB | focus ring, selected, active progress, speaker label, label anchors |
| `accent/cyan-dim` | #1F6E7C | selected row fill (at 35 %) |
| `accent/teal` | #5EE6C8 | correct answer, completed, discovery stamp |
| `accent/amber` | #F2B54A | THE primary action (Begin, Scan Eye, Explore Inside, Check answer, Got it) |
| `accent/amber-pressed` | #D49A2E | pressed primary |
| `on/amber` | #1B1305 | text/icon on amber |
| `feedback/coral` | #F0907E | gentle "not quite" feedback (never red alarm) |

Radii: panel 16, control 12, chip/pill 999, label 8. Spacing scale 4/8/12/16/24/32/48. Shadow: 0 8 24 #000 at 35 % on
floating panels only. Focus: 2 px `accent/cyan` ring offset 2 px (keyboard only).

Typography: Atkinson Hyperlegible (already licensed in the project, SIL OFL), two weights.

| Style | Size / line | Weight | Notes |
|---|---|---|---|
| Display | 104 / 104 | Bold | start title, tracking −1 % |
| Subtitle | 30 / 38 | Regular | start subtitle |
| H1 | 40 / 48 | Bold | modal titles |
| H2 | 26 / 32 | Bold | structure name, quiz question |
| Overline | 14 / 18 | Bold | uppercase, +14 % tracking (`accent/cyan` or `text/secondary`) |
| Body | 20 / 28 | Regular | panel text |
| Caption | 28 / 38 (learner sizes 22, 28, 32, 36) | Regular | narration subtitles |
| Button | 18 / 22 | Bold | sentence case; Begin/primary CTA uppercase +6 % |
| Small | 15 / 20 | Regular | helper, metadata (never below 15 px at 1080p) |

## 3. Layout grid (1920 × 1080 reference, constant pixel size; 1024 × 768 must also work)

- Safe margin 32 px (24 px below 1366 wide).
- **Top-left — lesson pill** (max 440 wide, 56 tall): overline `SCENE 02 OF 11` + title `Meet Niko`, and a 10-segment
  progress bar underneath (segments: done = teal, current = cyan, next = hairline). Non-interactive; Chapters live in the menu.
- **Top-left, under the pill — structure card** (420 wide, appears only while a structure is the subject): overline
  `STRUCTURE`, H2 name (e.g. `Cornea`), one-sentence description (Body, `text/secondary`, ≤ 2 lines), optional meta
  (`2 of 4 explored`). Fades in 0.25 s with the focus, out when the focus ends.
- **Top-right — controls**: 48 × 48 icon buttons, 8 px apart, each with a text tooltip and an accessible name:
  Replay line, Pause, Help, Settings, Menu (Menu opens: Chapters, Review discoveries, Restart scene, Main menu).
- **Right — activity panel** (380 wide, top 112 px, bottom ≥ 120 px, auto height): only while there is an activity.
  Header overline `YOUR TURN` (amber) or `EXPLORE` / `EXPERIMENT` / `QUICK CHECK` (cyan); instruction in Body Bold;
  then the controls (primary amber button, part list rows, sliders, near/far toggles, quiz options, retinal-image popup,
  journey chips). Lab tools (Explore lab, Replay intro, Sound) become small secondary rows here in Scene 2 only.
- **Bottom-centre — captions**: centred on the composition area (screen width minus the reserved right zone), max 960
  wide, ≤ 2 lines, 40 px above the bottom; speaker overline `NIKO` in `accent/cyan` above the text; backing is a soft
  vertical gradient (`surface/scrim` 0 → 55 %) over the bottom 220 px, never a box.
- **Bottom-right — Talk to Niko**: pill button 56 tall with Niko's avatar, label and a status dot (listening / speaking /
  ready). Opens the companion drawer.
- **World labels**: chip (navy 88 %, hairline border, radius 8, 16 px padding × 8, Body 18 Bold) + optional one-line
  description (Small) + 1 px leader line (`accent/cyan` 70 %) to a 6 px anchor dot with a 2 px ring. States: idle,
  focused (cyan border), explored (teal check). At most 4 labels at once; they never overlap each other, the panels or
  the characters; they glide 0.3 s and fade 0.25 s.

## 4. States / screens

1. **Start screen** — keeps the existing gate composition (camera `S02_IntroThreshold`). Left scrim (navy 0 → 85 % over
   the left 50 %). Left column at x = 120: overline `AN INTERACTIVE LESSON WITH NIKO`, Display `THE HUMAN EYE`, Subtitle
   `How light becomes sight`, CTA row: **BEGIN** (amber, 240 × 64), `How to play`, `Chapters` (secondary 64 tall);
   `Continue · Scene 5 · Light Enters the Eye` (secondary, only with saved progress); helper Small
   `Sound on for Niko's voice · Mouse, touch and keyboard`; Settings icon top-right; credits Small at the bottom-left.
2. **How to play** — modal card 960 wide: 6 tiles (Watch and listen, Rotate: drag, Zoom: scroll or pinch, Tap a
   structure, Talk to Niko, Pause or replay any time) with line icons; keyboard line; **GOT IT** amber.
3. **Chapters** — right sheet 560 wide over a scrim: 10 rows (number, title, one-line description, state chip:
   `DONE` teal / `CURRENT` cyan / `LOCKED` tertiary with reason). Locked rows explain: "Finish Scene 4 first".
4. **Cinematic** — letterbox bars (top/bottom 9 % of height, ease 0.6 s), caption inside the bottom bar, ghost `Skip`
   48 tall bottom-right. Nothing else.
5. **Explanation** — subject + lesson pill + top-right controls + captions + Talk to Niko; structure card when a
   structure is named. No activity panel.
6. **Interaction (explore)** — activity panel `EXPLORE`: instruction, part rows (48 tall, state icons), view tools
   (rotate left/right, zoom in/out, reset) as a 5-icon row; world labels for the explorable parts; toast for the mode.
7. **Activity (experiment)** — `EXPERIMENT`: instruction, slider with min/max labels or Near / Distant segmented
   toggle, live readout ("Pupil: 3.1 mm"), Freeze / Replay demo secondary.
8. **Quick check + feedback** — `QUICK CHECK`: H2 question, 2–4 option rows; selected = cyan border; correct = teal
   border + check + one-line why; wrong = coral border + "Not quite — look at …" + hint; tap-part questions show the
   instruction and highlight-on-hover in 3D.
9. **Structure information** — the structure card (top-left) with name, description and a "Show me" link that frames
   the part when the camera is free.
10. **AI companion** — drawer 420 wide from the right, over the activity zone: header (avatar, `Niko`, status, close),
    message list (Niko bubbles `surface/raised`, learner bubbles `accent/cyan-dim`), 3 suggestion chips, input with send.
    Labelled "Answers from the lesson notes" (local, no remote AI).
11. **Pause** — scrim + card: H1 `Paused`, **Resume** (amber), Restart scene, Chapters, Main menu.
12. **Settings** — card: Captions (on/off + 4 sizes), Labels, Reduced motion, Skip camera motion, Voice / Music /
    Effects volume sliders, Reset progress (destructive, confirm).
13. **Result / end** — `INVESTIGATION COMPLETE`, recap chips of the 10 topics with teal checks, **Replay** amber,
    Review discoveries, Main menu.

## 5. Motion

Panels 0.25 s ease-out in / 0.18 s ease-in out; captions cross-fade 0.2 s; letterbox 0.6 s; main UI reveal after a
cinematic 0.6 s; label glide 0.3 s; button press scale 0.97 for 80 ms. Reduced motion: fades only, no glides.

## 6. Mapping to existing element keys (Hud bindings stay valid)

| Spec region | Existing keys |
|---|---|
| Lesson pill | `chapter-title`, `progress` |
| Top-right controls | `btn-replay-expl`, `btn-pause`, `btn-help`, `btn-settings`, `btn-review`, `btn-home` |
| Captions | `subtitle-panel`, `speaker`, `subtitle-text`, `btn-read-continue` |
| Structure card / info | `info-text`, `prompt-banner` |
| Activity panel | `activity`, `ab-SCAN`, `ab-INSIDE`, `ab-FREEZE`, `ab-REPLAY`, `part-buttons`, `view-*`, `sim-*`, `insets`, `journey` |
| Talk to Niko | `btn-ask`, `ask-panel`, `ask-input`, `ask-submit`, `ask-close`, `niko-status` |
| Cinematic | `intro-overlay`, `btn-skip-intro` (+ new letterbox bars) |
| Start | `start-screen`, `start-begin`, `start-continue`, `start-settings`, `start-notice` (+ new How to play / Chapters) |
| Modals | `help-panel`, `settings-panel`, `review-panel`, `pause-overlay`, `confirm-panel`, `end-menu` |
