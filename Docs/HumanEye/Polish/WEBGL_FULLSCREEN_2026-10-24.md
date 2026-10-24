# WebGL fullscreen and Pages release — 24 October 2026

[Live module](https://chandan-meta.github.io/AI-HUMAN-EYE/) · [Pages deployment](https://github.com/chandan-meta/AI-HUMAN-EYE/actions/runs/40090657876)

The WebGL player has a small fullscreen icon at the bottom-left. Its 48px touch target respects device safe areas and matches the pearl/violet controls. The player container enters fullscreen, keeping the canvas and exit button together. Escape updates the button state; keyboard focus, denied requests and unsupported browsers have explicit handling. This is a browser control, so it does not appear in the native Unity Game view.

The stylesheet URL is versioned to avoid serving the previous control styles from browser cache. The original Unity Canvas, Figma sprites, anatomy and learning flow are retained.

## Build size

| Release | Published bytes | Size |
|---|---:|---:|
| Previous live Pages commit `36f9118` | 135,839,535 | 129.55 MiB |
| New Pages commit `41705e9` | 95,648,205 | 91.22 MiB |

The new download is **29.59% smaller**. It uses the existing release optimizer: Brotli with Unity's decompression fallback, size-optimized IL2CPP, engine stripping, compressed web textures, mesh compression and audio compression. Hero eye textures and the Figma interface retain their designated quality. No new packages were added.

The release build succeeded with zero errors and one warning in 10 minutes 34.5 seconds. It was made from a saved source snapshot in `C:/he-sandbox/AI-HUMAN-EYE`; the owner's open Editor was not closed or used for the build. All 353 recorded original source mesh/audio files still match their hashes, and the saved scene remains `c34b58f077b6c37f636e423c083dc90dfbf266c6aef8c5c2fab9ec4969c5bcc7`.

## Verification and publication

- Fullscreen behavior checks pass for gesture-driven entry, exit, Escape, focus, denial, unavailable APIs and the WebKit fallback.
- The compressed Wasm decompresses and passes V8's `WebAssembly.validate`; generated loader and framework JavaScript parse successfully. Local HTTP serves the page, stylesheet and fullscreen script successfully.
- The publisher's local fake-remote tests cover retained history/manual files, split/reconstructed hashes, refusal to overwrite an existing checkout and ordinary push verification. The Windows line-ending regression is covered using multiline LF templates under `autocrlf=true`.
- Module source is pushed on `claude/wow-moments` at `7a61449`. Pages commit `41705e9` retains parent `36f9118`; publication uses an ordinary push with remote revision verification. The new data file is below the splitting threshold.
- GitHub's Pages deployment completed successfully. All 11 release files downloaded from the live site match their build byte lengths and SHA-256 hashes; `.nojekyll` is the additional empty publishing file.
- Evidence is in `Logs/webgl-fullscreen-20261024/`: release log/manifest, Wasm validation, source preservation, publication result and live-file verification.

Use [the repository publisher](../../../Tools/HumanEye/PUBLISHING.md) for future releases. Build output is served as plain files, without source-repository LFS rules. Local capture archives, recovery files, caches and the large delivery ZIP were excluded from the source push.

## Unverified

Actual browser fullscreen clicks and WebGL gameplay were not exercised in this run: the browser-control tool reports **“Local browser use is disabled in Work settings.”** Automated API checks do not establish a real browser interaction pass. Physical tablet/touch performance is also untested. The earlier visual report documents remaining minor subtitle/label overlaps.
