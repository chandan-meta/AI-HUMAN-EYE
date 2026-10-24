# Publishing the existing WebGL module

Build and browser-check `Builds/HumanEye/WebGL` first. Then run from the project:

```powershell
./Tools/HumanEye/publish_pages.ps1 -Site 'D:/AI unity project/AI-HUMAN-EYE/Builds/HumanEye/WebGL' -WorkDir 'C:/he-sandbox/pages-release-20261024' -Push
```

Choose a new `WorkDir` each time, outside the source project and build. Omitting `-Push` prepares a local commit only.
The script clones the current origin `gh-pages` branch, keeps its history, replaces generated Unity site paths, and
preserves manual files such as `CNAME`. It copies plain files without LFS attribute rules. Data files above 90 MiB become
45 MiB parts; the loader-compatible manifest also records SHA-256 hashes, verified against the reconstructed bytes.
Every prepared file must be at most 95 MiB. Other oversized files are rejected because the loader only joins data files.

Publishing uses an ordinary push and verifies that the remote branch matches the local commit. A concurrent remote
update causes rejection; use a new checkout and retry from that updated history. The script never changes or stages the
source repository. Git's configured commit identity and existing authentication are used.

Run `./Tools/HumanEye/test_publish_pages.ps1` for the local fake-remote check. It tests preparation, split hashes, retained
history/manual files, existing-directory refusal, and ordinary push verification without contacting GitHub.
