# Human Eye loading page, production WebGL build and GitHub Pages (16 October 2026)

**Live:** https://chandan-meta.github.io/AI-HUMAN-EYE/ (branch `gh-pages`, GitHub's "pages build and deployment", source
"Deploy from a branch", `/ (root)`). Opened and tested live in Chrome and Edge (desktop, 1920×1080), at tablet-landscape
size (1024×768), and in the desktop app's Chromium pane; see *Verification* below.

## What the loader is, and why it is a web page

The module had no Unity-side loader: one scene, no `LoadSceneAsync`/`AsyncOperation`. Its real wait is the browser
downloading and starting the Unity build, which only the web page can show (a Unity Canvas cannot draw before the engine
has downloaded). So the loader is the WebGL template **`Assets/WebGLTemplates/HumanEyeLoader/`**, bound to Unity's own
loading progress (`createUnityInstance`'s progress callback: the download, then the engine's start-up).

- **The exact artwork.** The supplied image is `TemplateData/human-eye-loading.webp`, byte for byte the attachment
  (1672×941). It is never redrawn or recoloured. It is laid out as cover, and the eye, optic nerve, title and bar always
  stay in view: at 4:3 only the outer sides are cropped, and at extreme aspects it shifts or letterboxes on the
  artwork's edge navy rather than crop them. At 1920×1080 the page matches the reference, measured pixel by pixel outside
  the bar: median difference 0, mean 1/255, 96% of pixels within 6 levels. The pupil is unchanged (difference 0.08).
- **The real bar** is pinned exactly over the drawn one:
  - **Position:** capsule x 605–1067, y 669–699 in artwork pixels, measured from the image.
  - **Track:** opaque in the artwork's own empty colour (`#092E54`) with a thin cyan-blue outline, so the drawn static
    fill never shows through.
  - **Fill:** the gradient `#10DDF4 → #4BEAFF → #EAFBFF`, with a soft cyan glow, a glossy top highlight and a slow sheen.
  - **Motion:** progress is eased (exponential smoothing on unscaled time). It is real progress, never faked: the bar
    reaches full only once the engine has started.
  - **Finish:** a small glow pulse, a 0.25 s hold, then a 0.65 s fade into the module.
- **Into the module without a hard cut.** `Assets/HumanEye/Plugins/WebGL/HumanEyePage.jslib`: `ModuleBoot` calls
  `HumanEyePageReady` once the start screen has been drawn twice, and the page then fades. If that call never comes, it
  fades 5 s after the engine started.
- **Light, depth effects** (`TemplateData/loader.js`, one 2D canvas, about 50 particles, one animation loop that stops
  when the page has faded):
  - three depth layers at different speeds: far specks, mid glints, near soft blue orbs;
  - a hint of pointer parallax;
  - a barely-there breathing ring over the iris (opacity 0.10–0.18, 3.2 s), drawn as a ring so the pupil stays black;
  - a faint glint twinkle on the cornea's rim.

  There is no bloom, no blur and no motion of the artwork. With reduced motion the particles slow and the glows stop.
- **Text and accessibility.** No duplicate title (the artwork carries "Loading Human Eye Module"). The bar is an ARIA
  `progressbar` with live values. Errors show in a small navy banner: no WebGL 2, a failed download, or a Unity error.

## Production WebGL build

- **Settings, applied by `PlayerBuilds.WebGL` at build time** (so the Windows player and the Editor keep theirs):
  - template `HumanEyeLoader`;
  - gzip with **decompression fallback** (GitHub Pages sends no `Content-Encoding` for these files, so the loader
    decompresses them itself);
  - hashed file names (a new deployment never mixes with cached files), browser data caching on, high-performance GPU
    preference, no diagnostics overlay;
  - the output folder is cleaned before each build;
  - `config.autoSyncPersistentDataPath = true`, so saved progress persists.
- **Release, not Development.** `-heRelease` passes the `_Project/Editor/AssetAudit` production gate. The gate still
  rejects every other non-development build (owner's decision, 16 October 2026). The code size fell from 113 MB to 13 MB.
- **Web-only texture overrides** (`PlayerBuilds.WebGLTextures`, logged as `HUMAN_EYE_WEB_TEXTURES considered=142 changed=142`):

  | Textures | Web size | Crunch |
  | --- | --- | --- |
  | Lesson eye (`LocalEye`) | 2048 | none |
  | Hero albedos: skin, muscle parts, brain, rod, cone, Niko, the wall infographic, the sky | 2048 | quality 85 |
  | Every other map of 512 px or more: normals, masks, the lab, organs | at most 1024 | normals 80, others 70 |
  | UI sprites, maps under 512 px, HDR sources, baked lighting | untouched | none |

- **Size.**

  | | Before (Development) | After (release) |
  | --- | --- | --- |
  | Total | 401 MB uncompressed | **129.5 MB** |
  | `.data` | 287 MB | 116 MB (gzip) |
  | `.wasm` | 113 MB | 12.9 MB (gzip) |
  | `framework.js` | n/a | 0.09 MB |
  | loader page and artwork | n/a | 0.14 MB |

  Audio was already lean (music AAC at quality 0.55, voice at 0.7) and was not touched.
- **GitHub's 100 MB file limit.** The publish step splits the 116 MB data file into 45 + 45 + 26 MB parts with a manifest
  (`<data>.parts.json`).
  - The loader downloads the parts in parallel with real byte progress, keeps them in Cache Storage (a refresh starts in
    about 10 s instead of 15–30 s), and hands Unity the joined file.
  - Without a manifest it lets Unity download as usual.

## Publishing

- **Repository.** github.com/chandan-meta/AI-HUMAN-EYE. The owner made it public on 16 October 2026 after a history scan
  for secrets: 12 commits, no keys or tokens, no blob over 50 MB.
- **Branch.** `gh-pages`, an orphan branch containing only the site plus `.nojekyll`.
  - It is built by `C:\he-sandbox\publish_pages.ps1` in a scratch repository with `core.autocrlf false` and no LFS
    rules, so files go up byte for byte; Pages does not serve LFS content.
  - The module's working tree is never switched or touched.
  - It is force-pushed on each publish: the branch holds only the latest site.
- **Pages.** GitHub enabled Pages on the first push, and its "pages build and deployment" workflow deploys the branch.
  The prompt's preferred "GitHub Actions" source would need the `github-pages` environment to allow a non-default branch.
  Branch deployment is GitHub's own Actions workflow with no extra settings.
- **Paths.** Everything in the page is relative (`Build/…`, `TemplateData/…`), so it works under `/AI-HUMAN-EYE/`.

## Verification

1. **Local test** of the exact site folder, over HTTP (`python -m http.server`, no `Content-Encoding`, like Pages):
   - The loader appears at once and the bar follows the real download.
   - It fades into the start screen. BEGIN starts the opening (portrait, blink, layer reveals).
   - Refresh works. No console errors beyond Unity's standard WebGL notices.
2. **Five sizes** (headless Chrome, `Captures/web-loader-2026-10-16/loader_*.png`): 1920×1080 (the reference match above),
   1600×900, 1366×768, 1280×720, 1024×768. The eye, nerve, title and bar are intact at every size; at 4:3 only the outer
   sides are cropped.
3. **Live**, https://chandan-meta.github.io/AI-HUMAN-EYE/:
   - **Files.** Every file returns 200 with the right type (wasm, data and framework as `application/vnd.unity` or
     octet-stream, served raw: gzip magic `1f 8b`). No 404s, MIME, compression or CORS errors.
   - **Chrome (desktop, 1920×1080, DevTools-driven)** (`live_chrome_*.png`, `live_chrome_log.txt`):
     - bar 19% at 2.6 s, 76% at 7.7 s, engine started at about 14 s;
     - start screen, then BEGIN, then the opening's muscle reveal;
     - console: only the pre-existing light-probe warning; JS heap 209 MB.
   - **Edge (desktop, 1920×1080):** the same result (`live_edge_*.png`); JS heap 208 MB.
   - **Desktop app's Chromium pane:** load, BEGIN and the opening work; a refresh resumes with "Continue from Chapter 1",
     so the save persists.
   - **Tablet landscape (1024×768, Chrome)** (`live_tablet_*.png`, `live_tablet_log.txt`): bar 18% at 2.5 s, 68% at
     7.5 s, 99% at 12.6 s; start screen, then BEGIN, then the muscle reveal; the same single warning; JS heap 209 MB. At
     4:3, Niko stands behind the translucent start-menu card (pre-existing layout).
   - **Fade timing.** Headless Chrome and Edge render WebGL on the CPU (SwiftShader). Their first frames are slow, so the
     fade came at 32–33 s there. On a GPU, in the app's pane, it came after about 10–14 s.

## Shader warm-up: BEGIN without the black frame (16 October 2026, evening)

- **The stall, measured.** WebGL compiles a shader variant the first time it is drawn, on the page's main thread. The
  measurements use a real Chrome on the GPU (RTX 5060 Ti, ANGLE on Direct3D 11) with a fresh profile, so neither Unity's
  nor Chrome's shader cache is warm. On the published build, BEGIN held one black frame of 1.2–1.3 s, and the gate showed
  at 1.5 s. Later, with the machine more loaded, the same build stalled 3.0 s. Nothing else in the 32.5 s opening went
  over 100 ms.
- **What compiles after BEGIN.** A development build opened with `?heTrace` logs every compile. The variants first used
  after BEGIN are:
  - `HumanEye/Anatomy` ForwardLit: 2.7 s in the development build, and most of the stall;
  - its DepthNormals and punctual-shadow ShadowCaster passes: 0.12 s each;
  - `HumanEye/DissolveMote` at the first layer change: 0.11 s;
  - URP's Gaussian depth of field in the first lesson: five passes of 10–20 ms.

  The start screen compiles everything else itself.
- **The fix.** `ModuleBoot.WarmShaders` runs on WebGL only. It loads `Assets/HumanEye/Resources/ShaderWarmup.txt`: 10
  variants, one per line, as shader, subshader, pass and keywords, traced from the web player itself. It adds them to a
  `GraphicsStateCollection` with their exact passes and warms them in `Start`, before the first frame. That puts the cost
  inside the start-up compile the loading page already covers. WebGL has no PSOs, so Unity logs once that it falls back to
  a plain variant warm-up.
- **Rejected, after measuring:**
  - **Warming on the visible start screen, a few variants per frame.** The anatomy variant is one compile that cannot be
    split, so the start screen froze for 1.1–2.7 s.
  - **`ShaderVariantCollection`.** URP's passes share one pass type, so it warmed approximate variants as well, such as
    `_ADDITIONAL_LIGHTS` ones that are never drawn. 42 entries took 7 s.
  - **Warming everything the start screen and the opening draw (64 variants).** A warm-up compiles the start screen's own
    variants more slowly than its first frame does, so the loading page stayed up about 5 s longer.

### Result

Release build, fresh Chrome profile, GPU. The two columns were measured minutes apart on the same machine.

| | Before (the previous live build) | After (local HTTP and live) |
| --- | --- | --- |
| Longest frame after BEGIN | 3,004 ms (1,297 ms on a quieter machine earlier) | 37–43 ms |
| Compiles after BEGIN (40–60 s) | the anatomy and mote variants | none |
| Gate on screen after BEGIN | 1.5–3 s, black before it | by 0.3–0.6 s, the opening's own 0.6 s fade-in |
| Start-up frame behind the loader | 16.3 s | 2.0–3.2 s warm-up + 13.8 s ≈ 16.9 s |
| Loader fade, start screen | clean (≤ 29 ms frames) | clean (≤ 24 ms and ≤ 29 ms) |

The live site was re-verified the same way after the publish: the longest frame after BEGIN was 37 ms, nothing compiled
after BEGIN, and the loader's fade went no longer than 20 ms. The console shows only the known probe warning, plus
`[UnityCache] … CacheStorage` in a fresh automated profile, which the previous build logs too.

### Keeping the list current

Regenerate the list when the opening's or the first lesson's materials, shaders, lights or post effects change. Tracing
works only in a development build, because a release build records nothing.

1. Build a development player with `round23.ps1 -Steps sync,compile,webgl`, and serve `Builds/HumanEye/WebGL`. The
   `human-eye-webgl-build` entry in `.claude/launch.json` serves it on port 8781.
2. Run:

   ```bash
   node Tools/HumanEye/web_begin_check.mjs "http://127.0.0.1:8781/?heTrace" <out> --trace Assets/HumanEye/Resources/ShaderWarmup.txt
   ```

   The tool dumps the traced variants on the start screen and again 60 s after BEGIN, and writes the difference. Its
   report also lists the compiles after BEGIN, which should come to zero.

   On 16 October its list held the 10 shipped entries plus five `Universal Render Pipeline/Lit` shadow and depth
   variants. Those five differ only in keywords their passes ignore, so they are programs the start screen already
   compiles; they were left out of the shipped list.
3. Rebuild with `-heRelease` and run the same tool without `?heTrace` and `--trace` as the release check.

## Files changed

| Kind | Files |
| --- | --- |
| New | `Assets/WebGLTemplates/HumanEyeLoader/` (`index.html`, `TemplateData/loader.css`, `loader.js`, `human-eye-loading.webp`, `favicon.ico`); `Assets/HumanEye/Plugins/WebGL/HumanEyePage.jslib` |
| Changed | `Assets/HumanEye/Scripts/Core/ModuleBoot.cs` (page-ready call); `Assets/HumanEye/Editor/PlayerBuilds.cs` (WebGL settings, web textures, cleaned output, notices file); `Assets/_Project/Editor/AssetAudit.cs` (`-heRelease`) |
| Tooling (sandbox, outside the repo) | `C:\he-sandbox\round23.ps1` step `webglrel`; `publish_pages.ps1`; `split_parts.py`; `.claude/launch.json` (local static server) |
| Shader warm-up (evening) | New: `Assets/HumanEye/Resources/ShaderWarmup.txt`, `Tools/HumanEye/web_begin_check.mjs`. Changed: `ModuleBoot.cs` (`WarmShaders`, `?heTrace` and `TraceDump`); `.claude/launch.json` (the build server on port 8781, because 8766 is another project's) |

## Remaining issues

- **Later chapters' first use.** The list covers the opening and the first minute of the lesson. A learner who resumes
  with "Continue from Chapter N" may still meet a first-use compile in that chapter. A longer trace, or one per resumed
  chapter, would extend the list.
- **Download size.** 129.5 MB on a first visit is still heavy on slow connections; repeat visits use the cache.
  Further cuts would mean crunching the lesson eye or dropping unused legacy meshes from the scene.
- **Tablets and phones.** Tablet browsers without DXT support (iOS Safari, many Android GPUs) would decompress textures in
  memory. An ASTC build variant for mobile browsers is not made.
- **Pre-existing console warning.** "Inconsistent counts in appended probe data" comes from the baked lighting data.
