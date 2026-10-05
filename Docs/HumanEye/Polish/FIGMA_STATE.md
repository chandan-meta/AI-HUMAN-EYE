# Saved design state

Active file: https://www.figma.com/design/U4QDtnkYPFaBZBUVYIL4Dg
Created under the newly connected account after it lacked edit access to the old file.

Components page: 0:1. Screens page: 6:64.
Foundations: two collections; 10 primitive colours, 10 semantic colour aliases and five numeric tokens; five typography styles. Border reconciled to native #3F6377; button-radius 10px (VariableID:11:12), panel radius 16px.
Button set: 6:12, ten variants (Primary/Secondary × Default/Pressed/Focus/Disabled/Selected), editable Label property.
Screens: 14 initial UI specification frames, plus responsive 1024×768 lens screen 9:193. Initial frames use 181 component instances. All text uses Atkinson Hyperlegible.

These are editable UI overlay specifications. The Unity-rendered 3D scene is deliberately not represented as a flattened reference screenshot. Settings now include editable slider and toggle component sets (13:619 and 13:628; IDs in figma-settings.json). These are visual control specifications, not functioning Unity controls or a complete clickable prototype.
Visual checks: button-state review, 1600×900 lens, 1024×768 lens, menu and settings captures inspected. Remaining screen compositions still need individual acceptance.
Exact returned IDs: figma-state.json, figma-components.json, figma-screens.json, figma-panels.json. Responsive screen ID: 9:193. Eleven reusable panel/navigation components were subsequently added and matching full-size screen groups replaced with instances. IDs in figma-screens.json are creation history; use figma-panels.json for replacement instances.

Discovery: Simple Design System buttons were found but are not visually/token-compatible with the native Observatory UI; no matching Observatory surface variable. Local components created to match Hud.uss. Unity UI Toolkit has no Code Connect mapping in this project.
Old file u45UyNSwI31K6Oagcb81up remains empty; do not use it for implementation.
