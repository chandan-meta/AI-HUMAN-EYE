# Human Eye — production upgrade (7 October 2026)

Branch `claude/wow-moments`. The learning flow (10 chapters, 66 ledger cues, scan → inside → explore, checks at the
checkpoints) is unchanged. Everything below is applied by idempotent editor patches on the saved scene and Canvas, in
this order: **Apply production eye + human** (`Builder.ApplyProduction`) → **Apply UI pass** (`UiPass.Apply`) →
**Apply look pass** (`Builder.ApplyLook`), menu *Tools > Human Eye > Authoring*. Verification ran in the sandbox copy
(`C:\he-sandbox\AI-HUMAN-EYE`); the results were copied back. Numbers: see *Verification* at the end.

## References studied

- Human Brain lesson (public build `vaibhavgit31.github.io/Human_Brain_AI_Lesson`, `Human_Brain_AI.txt`, the supplied
  screenshots): letterboxed opening (corridor → gate crane → push through particles and title → person → face → head
  layers), a slim stage chip top-left, a narrow right column, captions bottom-centre, a single menu, a chapters modal
  with a play button, a bright clean hall. Used for movement, pacing and UI placement only — no brain content copied.
- *What Happens Inside Your Eyes* (eySkNWTI03Q) — the supplied frames: the eyeball with rectus muscles, photographic
  iris, the cross-section with lens and zonules, the retina and blind spot, photoreceptor layers on black, eye-to-brain.
  Used for the visual treatment only.

## Local model audit (`D:\AI unity project\models\eye`, recursive)

| Asset | Format | Content | Tris | Textures | Verdict |
|---|---|---|---|---|---|
| `eye model/half eye.fbx` | FBX, 123 MB (embedded) | cross-section: sclera shell (outer + inner), choroid, retina, retinal arteries/veins, optic nerve stub with central vessels, iris, lens, zonules, 3 rectus muscles; "Cornea2/3" are one solid anterior volume; a stray grey pupil disc | 35 980 | 2048 eye diffuse + normal, inner-wall diffuse, retina/choroid diffuse + normal (800–2048), muscle 1024; lens carried a wardrobe wood texture | **Used** (the eye) |
| `eye model/full eye.fbx` | FBX, 115 MB (embedded) | closed globe with painted iris, cornea cap, 4 rectus muscles | 14 744 | same family (blue-green iris with amber collarette) | Evaluated, not used: different proportions from the cross-section (iris 0.32 vs 0.42 eye radii) — one consistent model chosen |
| `realistic_human_eye.glb` | glTF | MisterH closed-eye exterior (already in the project) | 5 312 | 3 × 2048 | Superseded |
| `Human.unitypackage` | Unity package | Microsoft Rocketbox Business_Male_06 (facial rig, 176 shape keys, eye/lid/jaw bones), 2048 colour/normal/specular TGA, brain-lesson prefabs/scripts/shaders | 6 950 | 6 × 2048 | **Used** (the person; model + textures only) |
| `Brain.fbx`, `Brain_Textures/` | FBX + PNG | brain (already in the project) | — | 4 maps | Already in use |

Provenance: Rocketbox is MIT; the eye models have no recorded author or licence (release gate, THIRD_PARTY_NOTICES §7).

## What changed

**Eye model.** `Tools/HumanEye/export_local_eye.py` bakes the cross-section into the lesson frame (eye radius 1, optical
axis +Z, kept half x ≥ 0) and splits it by structure. `Builder.ApplyProduction` gives each structure (sclera with the
muscles, choroid, retina with vessels, iris, lens with zonules, optic nerve stub) its kept half, its cut-plane faces
(shown only while the eye is open) and a mirrored copy as the slide-away half — so the closed eye, the cutaway and the
optics are one model and the cutaway still slides apart. The anterior landmarks of `EyeGeometry` were re-measured from
the model (cornea sphere, iris plane and root, pupil, lens centre/radius/thickness, retina, optic disc), so the light
rays, the pupil plane, the procedural clear media (cornea, aqueous, vitreous) and every pick collider sit on the visible
anatomy. The optic nerve tube now continues the modelled stub to the brain (calm tissue colour, no permanent stripes);
the signal pulses run from the retina through the stub. Lens, iris, pupil, cornea and aqueous close-ups were re-aimed.

**Materials (`Anatomy.shader`).** Normal maps, a specular mask, a sharp wet tear-film highlight on the smooth surface
over bumpy tissue, a *clear medium* mode (cornea/lens/aqueous/vitreous show full-strength highlights over a nearly
invisible body, so the pupil reads black and the cornea as a wet dome) and a vertex remap that slides the iris's inner
edge — the pupil is a real opening that dilates and constricts with the lesson. The sclera texture is a whitened
derivative of the supplied one (warm white with fine veins at the front, a soft limbal ring, pink toward the back:
`Tools/HumanEye/whiten_sclera.py`).

**Human.** `Tools/HumanEye/export_rocketbox_display.py` lowers the arms, removes the eye caps, splits the trousers
(opaque through the X-ray, like the jeans before) and writes eye sockets at the eye bones; the anatomical eyes sit
flush (cornea apex where the cap's front was). The X-ray layer is the fitted Z-Anatomy skull (another figure's body
skeleton would not fit).

**Cinematic.** The intro is letterboxed (caption and Skip in the lower bar). The scan is framed on head and shoulders
(`S02_Face`, aimed live from the sockets), then one slower push into the eye in the skull, then the enlarged eye: wide →
face → eye → macro. The turntable stops when exploration begins (no moving targets). Anatomy close-ups ease into the
eye-macro look with background-only depth of field that starts behind the whole eye (`ShotLook`).

**Look.** Lab grade: neutral tonemapping, mild contrast, slightly cool balance, bloom only on true highlights, light
vignette, exposure +0.25; key light 1.05, ambient 1.3, room lights ×1.7 (`Stage.roomBoost`). Eye macro: contrast and a
little saturation, depth of field. The magnified retina sits on a dark neutral backdrop so the lit cells carry it.

**UI (`UiPass`, `UIGlass.shader`, `Hud`).** Start screen as a left title column (AN INTERACTIVE LESSON WITH NIKO ·
THE HUMAN EYE · subtitle · amber BEGIN · HOW TO PLAY · CHAPTERS · Continue · Settings); chapter chip with progress
segments top-left ("Chapter n of 10"); icon tools top-right (Home, Chapters, Replay, Pause, Help, Review, Sound,
Settings); a right column that holds only Niko's status, the instruction and the activity (sized to content, hidden
when empty, 340 px on narrow screens); captions bottom-centre under the subject; Talk to Niko bottom-right; a Chapters
modal (reached chapters play, later ones explain how they open); calm navy surfaces with a hairline border and cyan only
for hover, focus and the one required action; world labels with a hairline leader starting at the pill edge and an
anchor dot; a hover rim on selectable anatomy; 48 px part buttons. Design system of record: `UI_STYLE_GUIDE.md`.

**Figma.** Connected (account Starter plan, View seat). A new file was created (`UyMsflBNju6JlbE7tB9ftm`); the old file
is not editable by this account and the plan's MCP allowance (about 20 calls a month) was exhausted before any frame
was written. The foundations script is prepared; nothing exists in Figma yet.

## Verification (round 18, sandbox copy of these sources, 7 October 2026)

- Patches: production eye + human, UI pass, look pass — all exit 0 (`HUMAN_EYE_PRODUCTION_APPLIED detail meshes 18,
  triangles 35 980 per half`, `HUMAN_EYE_UI_PASS_APPLIED`, `HUMAN_EYE_LOOK_APPLIED`).
- Validator 70 passed / 0 warnings / 0 errors (66 of 66 cues, all chapter shots exist, no missing references).
- Edit Mode 24/24. Play Mode 2/2: the full walkthrough test and the cinematic test, which now asserts the detailed eye
  (winding agrees with normals on every detail mesh, the mirrored half completes the closed eye, section caps show only
  in the cutaway, the iris follows the live pupil), the intro beats, skip, replay, gate reset and the scan close-up.
- Windows development build: exit 0.
- Standalone walkthroughs (pointer raycasts through the EventSystem, layout audit of every visible control):
  1600×900 stress — 10 chapters, 66 cues, **35 PASS / 0 FAIL**, cumulative pointer failures 0, 0 exceptions;
  1024×768 reduced motion — 10 chapters, 66 cues, **15 PASS / 0 FAIL**, 0 exceptions;
  Scene 2 in real time (face shot, scan, push into the eye) — 5 PASS / 0 FAIL, 0 exceptions.
- Performance (offscreen walkthrough, frame-capped at 60): 55.4–60.0 fps average per chapter, main thread 16.7–18.0 ms,
  worst single frames 57–116 ms at chapter transitions — the same range as on 6 October (55–60 fps).
- Fixed during review: the Home tool was a bare 22 px icon overlapping Help (now a 48 px tool); the part buttons were
  46 px (now 48); labels collided at 4:3 in the cutaway (pills hug their names, relaxation with clamping); the anatomical
  eyes sat 1 cm forward and oversized in the Rocketbox sockets (now centred on the eye bones, cornea apex at the lid
  line); the mouth interior showed through the X-ray skin (removed from the display export); the procedural nerve tube
  covered the modelled stub in the cut view (now starts where the stub ends); the 400 px column covered the subject on
  4:3 (340 px below 1200 px wide).

Evidence: `Docs/HumanEye/Polish/Captures/final-2026-10-07/` — `compare_*` (6 October vs today, same walkthrough shots)
and single frames from the three runs.

## Open, not done

- Figma frames (plan limit; see above). Recorded narration (still the provisional local TTS). WebGL build (module not
  installed; Windows development builds stand in). A person watching and listening to a whole run, and physical tablet
  touch tests. The Rocketbox figure is a static display subject (no idle motion); Niko already blinks, hovers, looks and
  gestures. Timeline was not introduced (the cue sheet stays the named Cinemachine shots plus the code-driven intro).
  Audio set unchanged (19 project-generated clips; narration stays on top). Release gates: the supplied eye models'
  author/licence; Rocketbox MIT notice to be shipped with builds.
