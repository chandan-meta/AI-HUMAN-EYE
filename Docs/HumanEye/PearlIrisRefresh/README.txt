HUMAN EYE — PEARL + IRIS DELIVERY

UNITY FOLLOW-UP — 24 OCTOBER 2026
Exported Figma PNG panels and buttons are now applied to the existing saved Canvas.
The already-unpacked scene Canvas remains the UI authority; no runtime UI generation
was added. See Docs/HumanEye/FigmaIntegration/README.md for evidence and validation
status. The design-only and material-preview notes below record earlier revisions.

Editable Figma design (Vaibhav Sharma's account):
https://www.figma.com/design/wLgTj7N6mpfRONuYkmZac0?node-id=4-9

Representative prototype:
https://www.figma.com/proto/wLgTj7N6mpfRONuYkmZac0?node-id=4-9&starting-point-node-id=4%3A9&scaling=scale-down

START HERE
Previews/ReviewContactSheet.jpg shows all 12 desktop compositions.
Previews/ also contains the eight tablet adaptations, menus and illustrative states.
UIAssets/ contains ten editable SVG icons and the new iris crest.
HANDOFF.txt explains implementation, texture channels, calibration and limitations.
VERIFICATION.txt records the checks; design-tokens.json contains the actual Figma tokens.
figma-state.json records current page and frame IDs for follow-up editing.

MATERIALS
EyeInterior_* contains the generated source and aligned derived maps.
The source is 1254x1254; 2048 variants are upscaled and 1024 variants downsampled.
Normal and roughness are estimates, not geometry bakes or measured material data.
Unity_*_Original/Candidate.png are actual Unity comparisons at matching cameras.
Retina_ModelPreview_*.png are neutral Blender comparisons on the original UVs.
Retina_Candidate_Preview.blend includes its active texture packed into the file.
CleanPlate and *_Concept images are artwork studies, distinct from actual Unity evidence.

UNITY PROJECT
The candidate material is already installed at:
Assets/HumanEye/PearlIrisPreview/Retina_PearlIris_Candidate.mat
It is separate from the production retina material. Scene assignments were preserved.
The preview helper is Assets/HumanEye/Editor/PearlIrisPreview.cs.

ARCHIVE LAYOUT
Design/ contains this folder's documents, maps, artwork and previews.
Unity/Assets/ preserves the preview material, textures and editor helper with their .meta files.
For another checkout of this same project, place Design/ contents in
Docs/HumanEye/PearlIrisRefresh/ and merge Unity/Assets/ into the project's Assets/.
The custom HumanEye/Anatomy shader and original scene/meshes come from the existing project.

Prototype controls illustrate states; they do not run Unity or connect to a live AI service.
Mobile compression, complete seam/pole/mip review and runtime performance remain untested.
The editable design is in the Figma link. A native .fig binary export was not available.

VISUAL POLISH — 24 OCTOBER 2026
Editable iris crest and orbital line motifs; layered pearl fills, lilac edges and
surface depth; raised violet and amber actions with distinct hover/pressed states.
Start screen has an iris brand row, violet EYE emphasis, full-width amber action
and one secondary action row. Chapter and panel headers carry iris details.
Selected part rows have pearl markers and violet borders. Menus use icon badges;
success and retry retain separate mint and warm surfaces, with supporting copy
as plain text. Tablet tabs, reset labels, slider rails and lens callouts fit.
All 38 preview nodes were re-exported; IrisCrest.svg is included in UIAssets/.
Six paint styles and two effect styles are recorded in visual-polish-styles.json.
Start display title is 64 px; existing lesson typography remains in use.
Final structural check: zero horizontal text overflow, zero missing destinations
across 139 prototype routes. New gradient/text contrast pairs meet 4.5:1.
Secondary text was refined to 5E6B82 for contrast on the pearl gradient.
This revision changed Figma/UI artifacts only. Unity scenes, materials and
texture maps were not changed or retested. Full Present-mode click-through
remains unperformed. See design-polish-check.json for exact evidence.
