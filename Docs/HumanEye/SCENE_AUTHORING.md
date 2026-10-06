# Editing the authored Unity scene

Open `Assets/HumanEye/Scenes/HumanEye_Observatory.unity` outside Play mode.

## UI in the Hierarchy

Expand **HUD > Observatory Canvas > root**. Menus, buttons, captions, toggles, sliders, quiz options, part-list entries, review rows, labels and leader lines are saved GameObjects. They are native Canvas/UI components, not a UI Toolkit tree generated during Play.

Select **HUD** and use **Panel to edit > Show panel in Hierarchy and Scene** in the Inspector to reveal a normally hidden screen. Expand that panel and edit its Text, Image, RectTransform and layout components. Grey objects are inactive saved instances; enable them temporarily to edit. Stop Play before editing if you want changes to persist.

The Canvas is a prefab instance of `Assets/HumanEye/UI/ObservatoryCanvas.prefab`. Scene overrides are supported; save the scene after editing. Open the prefab for shared changes. Parent layout groups control child placement: use their padding/spacing and child LayoutElement values, or disable a layout group if you want to position its children freely.

HUD bindings use serialized references with stable keys. Renaming a GameObject does not break those references. Do not delete a referenced control without updating its HUD binding. Variable content uses saved inactive slots: play changes their text, visibility and listeners but never creates buttons or panels. To expand a list's capacity, duplicate an existing direct-child slot outside Play.

Runtime subtitles, progress, answers and simulation readouts are intentionally driven by lesson data. Edit dialogue and chapter assets to change that content; edit the saved Text components to change fonts, colour and alignment. Caption font size also follows the learner's Settings choice.

`Hud.uxml` and `Hud.uss` remain as the historical conversion source. Editing those files does not change the live Canvas. Use the scene or Canvas prefab now.

## 3D objects and staging

The environment, Niko, human, anatomy, lights, cameras and effect objects were already saved in the scene. Expand their existing roots, including inactive children, to edit them. The lesson activates and animates those instances.

Use **Hero Point**, **Cut Point** and **Menu Eye Point** for eye placement and scale, the saved Niko anchor transforms for his positions, and **Shots (cue sheet cameras)** for camera framing. The Stage component references these objects. Moving an animated object's temporary pose is not the same as editing its chapter's destination anchor. The menu Niko scale multiplier is editable on Stage.

Pupil/lens changes, light paths, signal movement and particle emission still animate during gameplay; their renderer, mesh-filter and effect objects already exist before Play.

## Building without losing edits

Use **Tools > Human Eye > Build Player > Windows / Android** to build the saved scene. Save your scene first.

**Authoring > Regenerate entire module (replaces manual edits)** is a destructive developer bootstrap command with an explicit warning. It recreates the scene and Canvas prefab. Do not run it after manual editing unless you intend to regenerate them. The one-time Canvas conversion skips a Canvas that already exists.

The pre-conversion scene backup is `Logs/authoring/HumanEye_Observatory.before-canvas.unity` (local, ignored by Git). The working tree also retains all earlier polish work.

## Reference glow styling

Panels and buttons now use the saved **Glass Panel** component and **ObservatoryGlass** material. Select an existing panel in the Hierarchy to edit its colour, radius, border width, halo width and glow. The effect renders directly on the Canvas, including Screen Space Overlay; it does not depend on camera bloom. It is a translucent gradient with a soft rim, not a real background-blur pass.

Each button's **Hud State** component controls selection/completion colours and the glow for hover, keyboard focus and lesson prompts. Start and Ask Niko have Persistent Glow enabled. **Hud Icon** children are separate editable vector graphics; change their Symbol, Stroke, Colour and RectTransform in the Inspector. Nothing is instantiated to create this styling during gameplay.

**Tools > Human Eye > Authoring > Apply reference glass styling** is an explicit restyling operation. It overwrites the shared prefab's style and layout defaults; do not rerun it after your own styling unless that is intended. Normal Play and Build do not call it. The existing scene and prefab were backed up in `Logs/glow` before applying this change.

The reference illustrations remain an art-direction target. The current room, Niko and anatomy models are different assets, and the rendered scene is not an exact visual reproduction of those illustrations.

At narrow widths, controls with **Compact Button Layout** move their existing icon above the label. Change its Compact Below threshold or disable the component to position those children manually. It never creates objects.

## UI click repair (4 October)

Every saved UI Graphic must have a CanvasRenderer. The custom Glass Panel and Hud Icon components now require it explicitly. The saved prefab was repaired without resetting any layout or styling (25 renderers added; 565 existing RectTransforms retained). Missing renderers can interrupt Unity's raycast over the entire UI, even when the broken graphic is a different button from the one being clicked.

If you create your own custom UI graphic, keep its CanvasRenderer. The explicit **Authoring > Repair UI renderers** command repairs missing components without applying the reference style again. All 139 saved Buttons are covered by the component/click-surface validation.

The pointer audit also found the comparison cards overlapping Turn/Zoom in later scenes. The saved comparison cards, instruction area and view-control column now occupy separate areas at 1024x768 and above. Informational world labels have Button.interactable disabled alongside their existing non-raycast state; selectable anatomy labels remain interactive.

## Right-hand column (6 October)

**Tools > Human Eye > Authoring > Apply WOW redesign (keeps scene edits)** (`HumanEye.EditorTools.WowPatch.Apply`) restructures the saved Canvas prefab into a right-hand column (`safe/right-column`: `info-panel` above `activity-panel`), adds the Niko portrait camera (`Niko/Float/Head/Niko Portrait Cam` → `Generated/UI/NikoPortrait.renderTexture`, layer `HE_Niko`), removes the retinal image quad, rebuilds `Retina (magnified, Scene 8)`, refines the eye materials and meshes, adds the `S10_Overview` and `S11_Closing` shots and the `Closing` Niko anchor, registers the lesson sound set and the closing clip, and updates the content data. It is idempotent and saves the scene and prefab. Column width is 380 px (constant pixel size); the label layer and top bar stop where the column begins. Edit the panels' Glass Panel, Text and layout components in the Hierarchy as before.
