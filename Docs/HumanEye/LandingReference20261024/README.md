# Human Eye reference finish

24 October 2026

The existing Unity scene has been refined against the supplied [landing](Reference-Landing.png) and [cutaway](Reference-Cutaway.png) references. The original models, UVs, scene workflow, lesson sequence and dialogue remain in use.

Watch the [natural-speed intro preview](Intro_Review.mp4) first: 22.46 seconds, 1920 × 1080, with sound. Only 1.5 seconds of startup were trimmed; the intro runs at its authored speed. The separate [complete-module QA movie](../../../Builds/Checks/reference-complete-record/Full_Module_Review.mp4) is accelerated at 6× and includes every chapter and Free Explore.

## Visual comparison

| Task-start baseline, 1920 × 1080 | Updated landing, 1920 × 1080 | Tablet layout, 1024 × 768 |
|---|---|---|
| [Before.png](Before.png) | [After.png](After.png) | [After-Tablet.png](After-Tablet.png) |

[Crystalline lens](Lens.png), [retina](Retina.png) and [photoreceptors](Photoreceptors.png) show the shared finish in the lesson. The desktop after images are exact native-resolution frames extracted from the final complete movie; the before and tablet images are original player PNG captures. No images were retouched or resized. `capture-provenance.json` records source hashes and exact frame indices.

## Implementation

| Area | Saved implementation |
|---|---|
| Eye surfaces | Optional dielectric mode in `Anatomy.shader`, using URP BRDF and environment reflection sampling. Existing pupil, cutaway, highlight, reveal, depth and shadow behavior is retained. |
| Sclera | Nonmetallic warm off-white; smoothness 0.94, wet coat 0.35, coat smoothness 0.96 and subtle normal scale 0.08. |
| Iris and veins | Original UV-matched 2048 × 2048 `Eye_Diff_Blue.jpg`, `Eye_Diff_Sclera.jpg` and `Eye_NM.jpg` retained. The iris and changing pupil remain behind the separate convex cornea. No new 4K iris is claimed. |
| Clear media | Cornea IOR 1.376 / smoothness 0.985; lens IOR 1.406; aqueous/vitreous IOR 1.336. Lightweight transmission with Fresnel reflections preserves the visible anatomy; no physical ray-traced refraction. |
| Reflections | Fixed 256px HDR studio cubemap and custom `Landing Studio Reflection` probe, with no per-frame cubemap updates. Its mip chain is not a full GGX-prefiltered environment. |
| Retinal membrane | New native 1254 × 1254 image-tool output based on the original 800px texture and normal reference. It adds membrane microdetail, not new vessel anatomy; original files and 3D vessels remain unchanged. Provenance: `Logs/landing-reference-20261024/retina-texture-generation.json`. |
| Lighting/environment | Warm key, cyan rim, low fill, restrained bloom/ACES, background focus and a warm exhibit accent. Existing floor/metal material smoothness and reflections were refined. Shared material improvements apply throughout the module. |
| Menu | Dedicated brighter composition, gentle eye float/rotation and reduced-motion still pose. The compact card scales to 75% below 1200px, preserving 48px secondary-button targets and clearing the iris. |
| Live UI | Eyebrow, navy/violet title and tagline are editable Unity text. The baked `StartBrand` image is disabled; all 43 original Figma PNGs remain. Existing control sprites, 165 Buttons and 159 HUD bindings are retained. Panel entry and button feedback respect reduced motion. |
| Guide placement | Shared placement reserves visible activity, Ask and next-step panels. Final native desktop/tablet checks confirm the whole guide stays clear of the relevant quiz/objective cards. |

`Logs/landing-reference-20261024/preservation.json` verifies all 352 MeshFilters, two SkinnedMeshRenderers and the Animator are byte-identical; all 826 original 3D poses/parents and the serialized EyeModel, HumanDisplay, NikoRig, ChapterRunner, Narrator and AudioDirector components are preserved. Two visual transforms were added. The final `preservation-final.json` verifies all 353 tracked original mesh/audio files and all 88 Figma baseline entries match their exact hashes. This proves the recorded scene references and tracked bytes, not a hash comparison of every external project file.

## Verification

| Check | Accepted evidence / status |
|---|---|
| EditMode | 33/33 passed (`editmode-final.xml`), including live-title glyph generation, retained input/artwork and physical materials/static reflection assignment. |
| Cinematic lifecycle | Isolated 5/5 passed (`playmode-accepted-isolated.xml`, 81.055 s) after fixing dolly near-plane restoration. |
| Presentation cases | Eight distinct presentation cases have passed across isolated runs; this is not a single 8/8 run. The supported Scene 6 clearance reproduction passed 1/1 at 1366px (`niko-supported.xml`, 4.560 s), and fresh native 1024px previews show the complete guide clear of the quiz and next-step cards. |
| Desktop interaction regression | Before the final placement correction: ten chapters / 65 cues / 14 evidence events; 144.03 s, 45 PASS / 0 FAIL, 93 pointer audits and 93 PNGs, zero blocked/off-screen controls (`reference-delivery-desktop`). |
| Final Windows build | Succeeded, 797.0 MB, 18.177 s, zero errors / two warnings, 15:42:33 IST (`build-complete-final.log`). |
| Final complete desktop movie | Ten chapters / 65 cues / 13 evidence events plus Free Explore; 122.48 s walkthrough, 20 PASS / 0 FAIL, 196 pointer audits, zero blocked/off-screen controls. Presence coverage: 7,208 frames / zero failures; presenter coverage: 3,404 / zero failures (`reference-complete-record-captures`). |
| Final tablet walkthrough, 1024 × 768 | Ten chapters / 65 cues / 14 evidence events; 95.91 s, 44 PASS / 0 FAIL, 112 pointer audits and 112 PNGs, zero blocked/off-screen controls (`reference-complete-tablet`). |
| Final frame review | All 3,684 decoded frames measured at 30fps / 1920 × 1080; complete QA movie duration 122.8 s. Timeline, flag and native-resolution review accepted with the capture-startup note below (`frame-audit-complete/Reviewed-findings.json`). |

Earlier runs and failures remain in the evidence logs. A genuine dolly restoration issue was fixed; an earlier parallel-player save-store race required serial stress runs. Native tablet review caught guide/next-step overlap despite earlier test passes. The stronger correction now passes the supported reproduction and fresh native preview; older movies are not presented as acceptance evidence for that fix.

Every decoded QA-movie frame was measured for full-resolution luminance/clipping and 320 × 180 edge/temporal metrics. Raw flags are retained: three black frames are confined to capture initialization at 0–0.0667 s, with first visible content at 0.1 s; 82 large temporal changes correspond to camera, focus, overlay, cinematic and travel transitions. There are no highlight-clipping or very-low-detail flags at the configured thresholds. Review covered 62 timeline samples, all 34 saved candidates and all 74 outside-window outliers, plus native desktop/tablet clearance frames, with no new blocking issue identified. These sets overlap. Automated measurement is not human review of every frame, does not inspect frames rendered between movie samples, and cannot alone certify localized UI clearance.

## Remaining differences and limits

This is an in-engine approximation, not an exact match or an AAA-quality guarantee. Existing geometry and display content, the 2K iris/sclera sources, reflection rig and floor response constrain the match. Transparent media do not physically refract rays; generated retinal microdetail is visual artwork, not a medically validated cellular reconstruction.

Minor cosmetic guide/UI interactions remain: some spoken-subtitle compositions can cover Niko's lower body, and some photoreceptor labels sit near his head. The quiz/next-step-card full-body clearance fix passed its final native checks; this does not establish zero overlap with every label and subtitle in the module.

Existing 42–44px lesson controls produced repeated small-target diagnostics (114 desktop / 120 tablet in the earlier complete runs), despite zero blocked or off-screen controls. This is separate from the compact menu's 48px secondary buttons. Physical tablet touch/device performance and a complete WebGL player remain untested. GLES3 shader compilation was reached, but the slow WebGL build was canceled before a complete player was produced.
