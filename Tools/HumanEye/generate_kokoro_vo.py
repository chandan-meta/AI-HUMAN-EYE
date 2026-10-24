"""Niko's voice, generated offline with Kokoro-82M (Apache-2.0) through kokoro-onnx (MIT); espeak-ng is the build-time
phonemizer only (22 October 2026 brief: the Windows "Microsoft Mark" voice and its pronunciation sounded odd). Regenerates
every Niko line under the same file names, so cue ids, subtitles and playback events are untouched:
  Docs/HumanEye/VO/HumanEye_VO.csv (filename,text)  -> Assets/HumanEye/Audio/VO/<filename>.wav
  the closing line (as generate_elevenlabs_vo.py)    -> Assets/HumanEye/Audio/SFX/niko_closing.wav
  Docs/HumanEye/VO/SUPPLEMENTAL_VO.csv (key,text)   -> Assets/HumanEye/Audio/VO/Extra/<key>.wav
No .align.json: Unity keys the mouth from the clip's amplitude (Builder.AudioKeys), and a stale one beside a new clip is
deleted. The environment and model files live on C: (D: is nearly full); setup and licences: Docs/HumanEye/VO/README_VOICE.md.

  C:\\he-tools\\kokoro\\env312\\Scripts\\python.exe -I Tools/HumanEye/generate_kokoro_vo.py samples     # 4 lines per voice -> Docs/HumanEye/VO/samples
  C:\\he-tools\\kokoro\\env312\\Scripts\\python.exe -I Tools/HumanEye/generate_kokoro_vo.py all [--voice V] [--speed S] [--force]
  --voice also takes a blend ("am_puck*0.6+am_michael*0.4"); --dry prints the pronunciation round trip and each line's
  phonemes without synthesising.
Audio: Kokoro 24 kHz float -> silence trimmed to 60 ms at each end -> ffmpeg loudnorm (I -16, TP -1.5, the existing VO
level), 10/30 ms fades, 48 kHz mono s16. Docs/HumanEye/VO/KOKORO_LOG.csv keeps one row per voiced file; TTS_LOG.csv follows."""
import argparse, csv, os, pathlib, re, subprocess, sys, wave
import numpy as np
from kokoro_onnx import Kokoro
from kokoro_onnx.tokenizer import Tokenizer

ROOT = pathlib.Path(__file__).resolve().parents[2]
MODELS = pathlib.Path(os.environ.get("KOKORO_MODELS", r"C:\he-tools\kokoro\models"))
DOCS = ROOT / "Docs/HumanEye/VO"
VO = ROOT / "Assets/HumanEye/Audio/VO"
CLOSING = ("niko_closing", "Now you've seen the whole journey. Every part has a role, and together they help you see the world.", ROOT / "Assets/HumanEye/Audio/SFX")
VOICE, SPEED = "am_fenrir", 1.05   # the audition of 22 October 2026: Docs/HumanEye/VO/README_VOICE.md
CANDIDATES = ["am_puck", "am_michael", "am_fenrir", "am_echo", "am_eric", "am_liam", "am_adam", "am_onyx",
              "bm_fable", "bm_george", "bm_lewis", "bm_daniel", "am_puck*0.6+am_michael*0.4", "am_puck*0.6+am_fenrir*0.4"]
SAMPLE_LINES = ["Hey! I'm Niko. Your Vision Investigator Officer.", "12_this_transparent_curved_layer", "27_behind_the_retina_is",
                "So close! Look at the glowing part."]   # file names are read from the ledger
SR = 24000
# General American IPA in espeak's notation (what the tokenizer emits for the rest of the sentence). Check with --dry.
# retina keeps a secondary stress on its middle vowel: without it Kokoro says "RET-nuh" (heard as "right now").
PRONOUNCE = {
    "niko": "nˈiːkoʊ", "investigator": "ɪnvˈɛstɪɡˌeɪɾɚ", "sclera": "sklˈɪɹə", "cornea": "kˈɔːɹniə", "iris": "ˈaɪɹɪs",
    "pupil": "pjˈuːpəl", "pupils": "pjˈuːpəlz", "aqueous": "ˈeɪkwiəs", "humour": "hjˈuːmɚ", "crystalline": "kɹˈɪstəlɪn",
    "biconvex": "baɪkˈɑːnvɛks", "vitreous": "vˈɪtɹiəs", "retina": "ɹˈɛtˌɪnə", "retinal": "ɹˈɛtˌɪnəl", "choroid": "kˈɔːɹɔɪd",
    "photoreceptors": "fˌoʊtoʊɹisˈɛptɚz", "optic": "ˈɑːptɪk", "ciliary": "sˈɪliˌɛɹi", "accommodation": "əkˌɑːmədˈeɪʃən",
    "chiasm": "kˈaɪæzəm", "geniculate": "dʒɛnˈɪkjʊlət",
}
WORDS = re.compile(r"\b(" + "|".join(sorted(PRONOUNCE, key=len, reverse=True)) + r")\b", re.I)
TOK = Tokenizer()

def clean(text):
    """Straight quotes; an em dash or ellipsis becomes a short pause (comma); a leading pause is dropped, a final one ends."""
    t = text.translate(str.maketrans({"’": "'", "‘": "'", "“": '"', "”": '"', "—": ", ", "–": ", ", "…": ", "}))
    t = re.sub(r"\s+([,.!?])", r"\1", re.sub(r"\s+", " ", t))
    t = re.sub(r",([,.!?])", r"\1", t)
    return re.sub(r",$", ".", t.strip()).lstrip(" ,")

def lang_of(voice): return "en-gb" if voice.startswith("b") else "en-us"

def phonemes(text, lang):
    """espeak for the sentence, PRONOUNCE for the listed words (the text is split around them, punctuation kept)."""
    out = []
    for i, part in enumerate(WORDS.split(clean(text))):
        if i % 2:
            ph = PRONOUNCE[part.lower()]
            if out and out[-1].endswith("ðə") and ph.lstrip("ˈˌ")[0] in "aeiouæɑɐɔəɛɪʊʌ": out[-1] = out[-1][:-1] + "ɪ"   # "thee iris", as espeak says "the eye"
            out.append(ph)
        elif re.search(r"[A-Za-z]", part): out.append(TOK.phonemize(part, lang))
        elif part.strip(): out.append(part.strip())
    return re.sub(r" ([,.!?])", r"\1", " ".join(out))

def style(kokoro, voice):
    """A voice name, or a weighted blend of style vectors: "am_puck*0.6+am_michael*0.4"."""
    if "+" not in voice and "*" not in voice: return voice
    return sum(kokoro.get_voice_style(n) * float(w[0] if w else 1) for n, *w in (p.split("*") for p in voice.split("+")))

def speak(kokoro, text, voice, speed):
    audio, sr = kokoro.create(phonemes(text, lang_of(voice)), voice=style(kokoro, voice), speed=speed, lang=lang_of(voice), is_phonemes=True)
    loud = np.flatnonzero(np.abs(audio) > .01 * np.abs(audio).max())   # Kokoro pads; keep 60 ms beyond the first/last sound
    return audio[max(0, loud[0] - int(.06 * sr)): loud[-1] + int(.06 * sr) + 1]

def to_wav(audio, out):
    dur = len(audio) / SR
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-",
                    "-af", f"loudnorm=I=-16:TP=-1.5:LRA=11,afade=t=in:d=0.01,afade=t=out:st={max(0, dur - .03):.3f}:d=0.03",
                    "-ar", "48000", "-ac", "1", "-sample_fmt", "s16", str(out)], input=audio.astype("<f4").tobytes(), check=True)
    return dur

def measure(path):
    with wave.open(str(path)) as w: x = np.frombuffer(w.readframes(w.getnframes()), "<i2"); sr = w.getframerate()
    return len(x) / sr, float(20 * np.log10(max(np.abs(x.astype(np.int32)).max(), 1) / 32768))

def lines():
    with open(DOCS / "HumanEye_VO.csv", encoding="utf-8") as f: ledger = [(r["filename"], r["text"], VO) for r in csv.DictReader(f)]
    with open(DOCS / "SUPPLEMENTAL_VO.csv", encoding="utf-8") as f: extra = [(r["key"], r["text"], VO / "Extra") for r in csv.DictReader(f)]
    return ledger + [CLOSING] + extra

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("command", choices=["samples", "all"])
    ap.add_argument("--voice", default=VOICE); ap.add_argument("--speed", type=float, default=SPEED)
    ap.add_argument("--force", action="store_true"); ap.add_argument("--dry", action="store_true")
    a = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")   # IPA on a Windows console (-I ignores PYTHONIOENCODING)
    rows = lines()
    if a.dry:
        assert (clean("light…"), clean("…but how"), clean("you—even")) == ("light.", "but how", "you, even")
        for word, ph in PRONOUNCE.items(): print(f"WORD {word:15} espeak {TOK.phonemize(word, 'en-us'):20} -> {ph}")
    kokoro = None if a.dry else Kokoro(str(MODELS / "kokoro-v1.0.onnx"), str(MODELS / "voices-v1.0.bin"))
    if a.command == "samples":
        text = {n: t for n, t, _ in rows}
        out = DOCS / "samples"; out.mkdir(exist_ok=True)
        for voice in CANDIDATES:
            said = [text.get(s, s) for s in SAMPLE_LINES]
            if a.dry: print("SAMPLE", voice, *(phonemes(s, lang_of(voice)) for s in said), sep="\n  "); continue
            gap = np.zeros(SR // 2, np.float32)
            audio = np.concatenate([x for s in said for x in (speak(kokoro, s, voice, a.speed), gap)][:-1])
            name = re.sub(r"\*([\d.]+)", lambda m: str(round(float(m[1]) * 100)), voice).replace("+", "_")
            print("SAMPLE", name, f"{to_wav(audio, out / f'{name}.wav'):.2f}s")
        return
    log = DOCS / "KOKORO_LOG.csv"
    rec = {}   # survives an interrupted run: a line already voiced with this voice and speed is skipped without --force
    if log.exists():
        with open(log, encoding="utf-8") as f: rec = {r["filename"]: r for r in csv.DictReader(f)}
    (VO / "Extra").mkdir(exist_ok=True)
    for name, text, folder in rows:
        wav = folder / f"{name}.wav"
        if a.dry: print("LINE", name, "|", phonemes(text, lang_of(a.voice))); continue
        r = rec.get(name, {})
        if wav.exists() and r.get("voice") == a.voice and r.get("speed") == str(a.speed) and not a.force: print("SKIP", name); continue
        dur = to_wav(speak(kokoro, text, a.voice, a.speed), wav)
        wav.with_suffix(".align.json").unlink(missing_ok=True)   # ElevenLabs timing would no longer match this clip
        rec[name] = dict(filename=name, voice=a.voice, speed=a.speed, seconds=round(dur, 3))
        with open(log, "w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=["filename", "voice", "speed", "seconds"]); w.writeheader(); w.writerows(rec.values())
        print("VOICED", name, f"{dur:.2f}s")
    if a.dry: return
    bad = 0
    print(f"{'file':40} {'sec':>6} {'chars/s':>7} {'peak dBFS':>9}")
    for name, text, folder in rows:
        wav = folder / f"{name}.wav"
        if not wav.exists(): print(f"{name:40} MISSING"); bad += 1; continue
        sec, peak = measure(wav); rate = len(clean(text)) / sec
        flag = " SHORT" * (sec < .4) + " SLOW" * (rate < 8) + " FAST" * (rate > 25) + " CLIPPED" * (peak > -1)
        bad += bool(flag); print(f"{name:40} {sec:6.2f} {rate:7.1f} {peak:9.2f}{flag}")
    with open(DOCS / "TTS_LOG.csv", "w", newline="", encoding="utf-8-sig") as f:   # the ledger rows, in the existing format
        w = csv.writer(f); w.writerow(["filename", "voice", "language", "status"])
        w.writerows([n, a.voice, "en-" + lang_of(a.voice)[3:].upper(), f"kokoro_{a.voice}"] for n, _, fo in rows if fo == VO)
    print(f"{len(rows)} lines checked, {bad} flagged")
    sys.exit(1 if bad else 0)

if __name__ == "__main__":
    main()
