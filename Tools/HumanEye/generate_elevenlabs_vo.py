"""Niko's voice through the owner's ElevenLabs account (15 October 2026 brief: a cute, curious, playful, childlike science
companion). Regenerates every Niko line (Docs/HumanEye/VO/HumanEye_VO.csv, same file names, so cue ids, subtitles and
playback events are untouched) plus the closing line, with per-line mood, and saves the character timing beside each clip
(<name>.align.json) for Niko's lip sync (Builder.MouthKeys reads it on the next ApplyNikoCharacter / content build; the
closing line's through Builder.ClosingKeys). Docs/HumanEye/VO/ELEVENLABS_LOG.csv keeps one row per voiced file across runs.

The key is read from ELEVENLABS_API_KEY (process or Windows user environment); it is never printed or written anywhere.

  python -I Tools/HumanEye/generate_elevenlabs_vo.py voices [--search child]      # account voices + shared-library candidates
  python -I Tools/HumanEye/generate_elevenlabs_vo.py samples --voices ID1,ID2     # the same four lines per voice -> Logs/vo-elevenlabs
  python -I Tools/HumanEye/generate_elevenlabs_vo.py all --voice ID [--force]      # every line -> Assets/HumanEye/Audio/VO (+ align)
  add --dry to any command to print the plan without calling the API.
Audio: mp3_44100_128 from the API -> ffmpeg loudnorm (I -16, TP -1.5, the existing VO level), 10/30 ms fades, 48 kHz mono s16.
No trimming (it would shift the timing)."""
import argparse, base64, csv, json, os, pathlib, subprocess, sys, tempfile, time, urllib.error, urllib.parse, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
LOG = ROOT / "Docs/HumanEye/VO/ELEVENLABS_LOG.csv"
LOG_FIELDS = ["filename", "mood", "model", "voice", "seconds", "aligned"]
SCRIPT = ROOT / "Docs/HumanEye/VO/HumanEye_VO.csv"
VO = ROOT / "Assets/HumanEye/Audio/VO"
CLOSING = ("niko_closing", "Now you've seen the whole journey. Every part has a role, and together they help you see the world.", ROOT / "Assets/HumanEye/Audio/SFX")
API = "https://api.elevenlabs.io"
SAMPLE_LINES = ["Hey! I'm Niko. Your Vision Investigator Officer.", "So you want to know how light becomes sight?",
                "This transparent, curved layer at the front is the cornea. It protects the eye and bends incoming light.",
                "The electrical signals leave the eye through the optic nerve."]
# Same moods as Stage.CueMood (by cue id, through the ledger); otherwise "?" curious, "!" excited, else explaining.
CUE_MOOD = {"S02_L01": "happy", "S02_L02": "curious", "S02_L04": "excited", "S02_L08": "excited", "S02_L15": "excited", "S03_L12": "encouraging",
            "S04_L01": "curious", "S04_L04": "excited", "S04_L06": "excited", "S04_L08": "happy", "S05_L02": "excited", "S07_L03": "curious",
            "S08_L05": "excited", "S09_L01": "excited", "S09_L03": "curious", "S09_L04": "curious", "S10_L01": "happy", "S10_L02": "encouraging",
            "S11_L01": "happy", "S11_L02": "curious", "S11_L03": "curious"}
CUE_OF = {c["filename"]: c["id"] for c in json.loads((ROOT / "Assets/HumanEye/Data/DialogueLedger.json").read_text(encoding="utf-8"))["cues"]}
# Expressive but stable: lower stability and more style for excitement, steadier for explanations (clear science words).
SETTINGS = {"happy": (.45, .80, .45), "curious": (.45, .80, .40), "excited": (.32, .78, .65), "encouraging": (.45, .80, .45), "explaining": (.55, .82, .30)}
V3_TAG = {"happy": "[cheerfully]", "curious": "[curious]", "excited": "[excited]", "encouraging": "[warmly]", "explaining": ""}

def key():
    k = os.environ.get("ELEVENLABS_API_KEY")
    if not k and sys.platform == "win32":
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as h: k = winreg.QueryValueEx(h, "ELEVENLABS_API_KEY")[0]
        except OSError: k = None
    k = (k or "").strip()   # a value set from a file often ends in a line break, which http.client would quote, key and all
    if not k: sys.exit("ELEVENLABS_API_KEY is not set (Windows user environment variable). Nothing was generated.")
    return k

def call(method, path, body=None, query=None, attempts=4):
    url = API + path + ("?" + urllib.parse.urlencode(query) if query else "")
    data = json.dumps(body).encode() if body is not None else None
    for i in range(attempts):
        req = urllib.request.Request(url, data=data, method=method, headers={"xi-api-key": key(), "Content-Type": "application/json", "Accept": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=120) as r: return json.loads(r.read())
        except urllib.error.HTTPError as e:
            detail = e.read().decode(errors="replace")[:400]
            if e.code in (429, 500, 502, 503, 504) and i < attempts - 1: time.sleep(2 ** (i + 1)); continue
            sys.exit(f"ElevenLabs {method} {path} -> HTTP {e.code}: {detail}")   # the key is never part of the message
        except urllib.error.URLError as e:
            if i < attempts - 1: time.sleep(2 ** (i + 1)); continue
            sys.exit(f"ElevenLabs {method} {path} unreachable: {e.reason}")
        except ValueError:   # never print the exception: its text would hold the header value, i.e. the key
            sys.exit(f"ElevenLabs {method} {path}: request rejected locally (does ELEVENLABS_API_KEY contain a stray character?)")

def mood_of(name, text):
    if name == CLOSING[0]: return "happy"   # the warm goodbye (Stage.Closing plays it with Goodbye and Happy)
    return CUE_MOOD.get(CUE_OF.get(name, "")) or ("curious" if "?" in text else "excited" if "!" in text else "explaining")

def lines():
    with open(SCRIPT, encoding="utf-8") as f: rows = list(csv.DictReader(f))
    return [(r["filename"], r["text"], VO) for r in rows] + [CLOSING]

def tts(voice, model, text, mood, prev=None, nxt=None):
    stability, similarity, style = SETTINGS[mood]
    spoken = (V3_TAG[mood] + " " + text).strip() if model == "eleven_v3" and V3_TAG[mood] else text
    body = {"text": spoken, "model_id": model, "voice_settings": {"stability": stability, "similarity_boost": similarity, "style": style, "use_speaker_boost": True}}
    if model != "eleven_v3":
        if prev: body["previous_text"] = prev
        if nxt: body["next_text"] = nxt
    r = call("POST", f"/v1/text-to-speech/{voice}/with-timestamps", body, {"output_format": "mp3_44100_128"})
    align = r.get("alignment") or r.get("normalized_alignment")
    if align and spoken != text:   # drop the leading audio tag's characters so the timing covers the subtitle text only
        n = len(spoken) - len(text)
        align = {k: v[n:] for k, v in align.items()}
    return base64.b64decode(r["audio_base64"]), align

def to_wav(mp3, out):
    with tempfile.NamedTemporaryFile(suffix=".mp3", delete=False) as t: t.write(mp3); src = t.name
    try:
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-af", "loudnorm=I=-16:TP=-1.5:LRA=11,afade=t=in:d=0.01",
                        "-ar", "48000", "-ac", "1", "-sample_fmt", "s16", str(out)], check=True)
        dur = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(out)], capture_output=True, text=True, check=True).stdout)
        tail = out.with_suffix(".tail.wav")   # a 30 ms fade-out without moving any sample in time
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(out), "-af", f"afade=t=out:st={max(0, dur - .03):.3f}:d=0.03", str(tail)], check=True)
        tail.replace(out); return dur
    finally: os.unlink(src)

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("command", choices=["voices", "samples", "all"])
    ap.add_argument("--voice"); ap.add_argument("--voices"); ap.add_argument("--search", default="")
    ap.add_argument("--model", default="eleven_multilingual_v2"); ap.add_argument("--force", action="store_true"); ap.add_argument("--dry", action="store_true")
    a = ap.parse_args()
    if a.command == "voices":
        if a.dry: print("would list GET /v1/voices and GET /v1/shared-voices", a.search); return
        for v in call("GET", "/v1/voices").get("voices", []):
            print("ACCOUNT", v["voice_id"], v["name"], json.dumps(v.get("labels", {})))
        for v in call("GET", "/v1/shared-voices", query={"page_size": 40, "search": a.search or "child", "language": "en"}).get("voices", []):
            print("LIBRARY", v.get("voice_id"), v.get("name"), v.get("age"), v.get("gender"), v.get("accent"), v.get("descriptive"), v.get("use_case"), v.get("preview_url"))
        return
    if a.command == "samples":
        out = ROOT / "Logs/vo-elevenlabs"; out.mkdir(parents=True, exist_ok=True)
        for vid in (a.voices or "").split(","):
            for i, text in enumerate(SAMPLE_LINES):
                mood = mood_of(["01_hey_im_niko_your", "02_so_you_want_to", "12_this_transparent_curved_layer", "58_the_electrical_signals_leave"][i], text)
                if a.dry: print("SAMPLE", vid, mood, text); continue
                mp3, _ = tts(vid, a.model, text, mood); (out / f"{vid}_{i + 1}_{mood}.mp3").write_bytes(mp3); print("SAMPLE", vid, i + 1, mood)
        return
    if not a.voice: sys.exit("--voice ID is required for 'all'")
    rows = lines()
    rec = {}   # the log survives an interrupted run: earlier rows are kept, each voiced line rewrites it at once
    if LOG.exists():
        with open(LOG, encoding="utf-8") as f: rec = {r["filename"]: r for r in csv.DictReader(f)}
    for i, (name, text, folder) in enumerate(rows):
        wav, align_path = folder / f"{name}.wav", folder / f"{name}.align.json"
        mood = mood_of(name, text)
        if a.dry: print("LINE", name, mood, SETTINGS[mood], text[:70]); continue
        if align_path.exists() and wav.exists() and not a.force: print("SKIP", name); continue
        prev = rows[i - 1][1] if i > 0 and rows[i - 1][2] == folder else None
        nxt = rows[i + 1][1] if i + 1 < len(rows) and rows[i + 1][2] == folder else None
        mp3, align = tts(a.voice, a.model, text, mood, prev, nxt)
        dur = to_wav(mp3, wav)
        if align: align_path.write_text(json.dumps(align), encoding="utf-8")
        rec[name] = dict(filename=name, mood=mood, model=a.model, voice=a.voice, seconds=round(dur, 3), aligned=bool(align))
        with open(LOG, "w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=LOG_FIELDS); w.writeheader(); w.writerows(rec.values())
        print("VOICED", name, mood, f"{dur:.2f}s", "aligned" if align else "NO ALIGNMENT")

if __name__ == "__main__":
    main()
