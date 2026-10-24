# Niko's voice (Kokoro, offline)

Since 22 October 2026 every Niko clip is generated locally with **Kokoro-82M** through **kokoro-onnx**, replacing the
Windows "Microsoft Mark" voice. Script: `Tools/HumanEye/generate_kokoro_vo.py`. Default voice **am_fenrir**, speed **1.05**.

## What it writes

| Source | Output |
|---|---|
| `Docs/HumanEye/VO/HumanEye_VO.csv` (filename,text, 66 ledger lines) | `Assets/HumanEye/Audio/VO/<filename>.wav` |
| the closing line (same text as `generate_elevenlabs_vo.py`) | `Assets/HumanEye/Audio/SFX/niko_closing.wav` |
| `Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv` (key,text, 44 lines) | `Assets/HumanEye/Audio/VO/Extra/<key>.wav` |

Audio: Kokoro 24 kHz, silence trimmed to 60 ms at each end, ffmpeg loudnorm I -16 LUFS / TP -1.5 dBTP, 10 ms fade-in,
30 ms fade-out, 48 kHz mono 16-bit. No `.align.json` is written (a stale one beside a new clip is deleted); Unity keys
Niko's mouth from the clip's amplitude (`Builder.AudioKeys`). The keys are baked when `Builder.ApplyNikoCharacter` (or any
patch that runs `Builder.BuildData`) runs in the Editor, so run one after regenerating. `KOKORO_LOG.csv` keeps one row per
voiced file (voice, speed, seconds); `TTS_LOG.csv` keeps the ledger rows.

## Regenerate

The environment lives on C: because D: is nearly full:
`C:\he-tools\kokoro\env312` (Python 3.12 from `uv`, kokoro-onnx 0.6.1, onnxruntime 1.31) and
`C:\he-tools\kokoro\models`, which holds `kokoro-v1.0.onnx` (325,532,387 bytes, sha256 `7d5df8ec…36a6c5`) and `voices-v1.0.bin`
(28,214,398 bytes, sha256 `bca610b8…1fbf7d`) from the kokoro-onnx GitHub release `model-files-v1.0`.
Set `KOKORO_MODELS` to use another folder.

```
C:\he-tools\kokoro\env312\Scripts\python.exe -I Tools/HumanEye/generate_kokoro_vo.py all --force
```

Without `--force`, a line already voiced with the same voice and speed is skipped, so an interrupted run resumes. The run
ends with a table of every clip (seconds, characters per second, peak dBFS) and exits 1 if any is missing, too short,
oddly paced, or peaks above -1 dBFS.

To rebuild the environment: get `uv` (the astral-sh/uv release zip), then
`uv venv --python 3.12 --managed-python C:\he-tools\kokoro\env312` and
`uv pip install --python C:\he-tools\kokoro\env312\Scripts\python.exe kokoro-onnx`, with `UV_CACHE_DIR` and
`UV_PYTHON_INSTALL_DIR` under `C:\he-tools\kokoro`. kokoro-onnx has no Python 3.14 build.

## Change the voice

One command (any Kokoro voice name, or a blend of style vectors):

```
C:\he-tools\kokoro\env312\Scripts\python.exe -I Tools/HumanEye/generate_kokoro_vo.py all --voice am_puck --speed 1.0 --force
C:\he-tools\kokoro\env312\Scripts\python.exe -I Tools/HumanEye/generate_kokoro_vo.py all --voice "am_puck*0.6+am_fenrir*0.4" --force
```

To make it the default, edit `VOICE, SPEED` at the top of the script. `samples` writes the same four lines for every
candidate to `Docs/HumanEye/VO/samples/<voice>.wav`, with 0.5 s gaps. Voices starting with `b` are phonemized as British English.

### Audition (22 October 2026)

The candidates were the twelve male voices plus two blends. The scores come from 17 anatomy lines at speed 1.0. Grade is
Kokoro's published grade (VOICES.md). Recall is the Windows desktop recogniser's word recall
(`check_tts_intelligibility.ps1` method). For comparison, Microsoft Mark scored 0.855 and 16/24 terms. Pitch is the median
F0, with the 10th to 90th percentile spread in brackets.

| Voice | Grade | Recall | Terms | Pitch Hz | Chars/s |
|---|---|---|---|---|---|
| **am_fenrir** | C+ | 0.851 | 13/24 | 139 (101–205) | 16.9 |
| am_puck | C+ | 0.842 | 14/24 | 113 (90–165) | 17.8 |
| am_michael | C+ | 0.816 | 14/24 | 120 (96–157) | 15.3 |
| bm_fable | C | 0.823 | 16/24 | 124 | 15.5 |
| bm_george | C | 0.761 | 15/24 | 143 | 14.7 |
| am_puck 60 % + am_fenrir 40 % | – | 0.858 | 14/24 | 123 (94–180) | 17.4 |
| am_puck 60 % + am_michael 40 % | – | 0.831 | 15/24 | 117 | 17.0 |
| am_echo / eric / liam / onyx / adam, bm_lewis / daniel | D+ to F+ | 0.77–0.86 | 13–16 | 89–158 | 15.5–18.7 |

am_fenrir won on three counts. It shares the best published male grade. Its pitch is the brightest and most varied of the
top-graded voices, which suits a small, cheerful character (am_onyx and bm_lewis are deep and flat). Its recall equals the
best single voice. The recogniser does not know Niko, sclera, choroid, biconvex or the British spelling humour, so those
terms miss for every voice, Mark included. Listen to the samples before relying on the table.

The repo check `Tools/HumanEye/check_tts_intelligibility.ps1` was run on the final 66 ledger clips. am_fenrir at 1.05 scored
0.859 word recall and recognised 34 of 43 anatomy terms (`TTS_TERMS.csv`). Mark scored 0.890 and 33 of 43. This
recogniser is a coarse, old engine, so treat it as a smoke test rather than a verdict.

## Pronunciation

`PRONOUNCE` in the script maps a word to General American IPA, written in espeak's notation like the rest of the sentence.
The text is split around those words: espeak phonemizes the pieces between them, the override phonemes are inserted, and
Kokoro gets the whole string with `is_phonemes=True`. A "the" before an overridden vowel becomes "thee". The covered words
are Niko, investigator, sclera, cornea, iris, pupil(s), aqueous, humour, crystalline, biconvex, vitreous, retina, retinal,
choroid, photoreceptors, optic, ciliary, accommodation, chiasm and geniculate.

- To add a word, add `"word": "phonemes"` to the dict. Only characters in Kokoro's vocabulary count; the rest are dropped.
- To check your work, add `--dry` to `samples` or `all`. It prints each overridden word as espeak would say it next to
  the override, then every line's final phoneme string, and synthesises nothing.
- retina carries a secondary stress on its middle vowel (`ɹˈɛtˌɪnə`). Without it Kokoro says "RET-nuh", which the
  recogniser heard as "right now".

Before phonemizing, the text is normalised: curly quotes become straight ones, and an em dash or ellipsis becomes a comma
pause. A leading ellipsis is dropped and a trailing one ends the sentence.

## Licences

- Kokoro-82M model and voices: Apache-2.0 (hexgrad).
- kokoro-onnx: MIT. onnxruntime: MIT.
- espeak-ng (bundled in espeakng-loader, via phonemizer): GPL-3.0. It is a build-time tool that turns text into phonemes
  on this PC. Nothing from it ships: the module contains only the generated WAVs.

The Windows-voice clips that were replaced are backed up in `C:\he-tools\vo_backup_windows_tts\` (66 ledger clips plus
`niko_closing.wav`).
