"""Human Eye audio overhaul (15 October 2026): turn the owner-listed CC0 downloads into the module's music, ambience and
effect clips. Reproducible: re-run after re-downloading the sources (Tools/HumanEye/fetch_audio_sources.py writes them to
SRC). Every output gets a .meta with a GUID derived from its path, so the owner's Editor and the sandbox import the same
asset IDs. Writes Assets/HumanEye/Audio/Licenses/AUDIO_SOURCES.csv (one row per used source file).

  python -I Tools/HumanEye/build_audio_assets.py [SRC]     (default SRC: C:/he-sandbox/audio_src)

Processing: ffmpeg decodes, filters (high-pass, low-shelf, low-pass, band limits), trims encoder silence and normalises
(music to -18 LUFS integrated, effects to a peak level); loops are made seamless by crossfading their tail into their head;
layered effects are mixed in numpy. No sound is synthesised from nothing: every clip is built from the listed downloads."""
import csv, hashlib, io, json, math, pathlib, struct, subprocess, sys, wave
import numpy as np

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "C:/he-sandbox/audio_src")
AUD = ROOT / "Assets/HumanEye/Audio"
SR = 44100
rng = np.random.default_rng(20261015)   # fixed seed: the scattered crackle is identical on every run

def meta(path, folder=False):
    """A minimal .meta with a GUID derived from the asset path (Unity fills in the importer settings on import)."""
    m = pathlib.Path(str(path) + ".meta")
    if m.exists(): return
    rel = path.relative_to(ROOT).as_posix()
    guid = hashlib.md5(("HumanEyeAudio:" + rel).encode()).hexdigest()
    m.write_text(f"fileFormatVersion: 2\nguid: {guid}\n" + ("folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" if folder else ""), encoding="utf-8")

def folder(rel):
    p = AUD / rel; parts = p.relative_to(ROOT).parts
    for i in range(1, len(parts) + 1):
        q = ROOT.joinpath(*parts[:i]); q.mkdir(exist_ok=True)
        if i > 1: meta(q, folder=True)
    return p

def load(src, filters=None, stereo=True):
    """Decode a source (relative to SRC) to float32 [n, ch] at 44.1 kHz through optional ffmpeg filters."""
    args = ["ffmpeg", "-v", "error", "-i", str(SRC / src)]
    if filters: args += ["-af", filters]
    args += ["-ac", "2" if stereo else "1", "-ar", str(SR), "-f", "f32le", "-"]
    x = np.frombuffer(subprocess.run(args, capture_output=True, check=True).stdout, np.float32)
    return x.reshape(-1, 2 if stereo else 1).copy()

def trim_silence(x, db=-60):
    a = np.abs(x).max(1); keep = np.nonzero(a > 10 ** (db / 20))[0]
    return x[keep[0]: keep[-1] + 1] if len(keep) else x

def loop(x, seconds):
    """Seamless loop: the last `seconds` fade into the first (equal-power), and the result is that much shorter."""
    n = int(seconds * SR); t = np.linspace(0, 1, n)[:, None]
    y = x[:-n].copy(); y[:n] = x[:n] * np.sin(t * np.pi / 2) + x[-n:] * np.cos(t * np.pi / 2)
    return y

def lufs(x):
    """Integrated loudness (EBU R128) through ffmpeg."""
    k = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-f", "f32le", "-ar", str(SR), "-ac", str(x.shape[1]), "-i", "-", "-af", "ebur128", "-f", "null", "-"],
                       input=x.astype(np.float32).tobytes(), capture_output=True).stderr.decode(errors="replace")
    import re
    m = re.findall(r"I:\s+(-?[\d.]+) LUFS", k)
    return float(m[-1]) if m else -70.0

def to_lufs(x, target, peak_db=-1.5):
    g = 10 ** ((target - lufs(x)) / 20); y = x * g
    p = np.abs(y).max()
    if p > 10 ** (peak_db / 20): y *= 10 ** (peak_db / 20) / p   # a peak ceiling instead of a limiter
    return y

def to_peak(x, peak_db):
    p = np.abs(x).max(); return x * (10 ** (peak_db / 20) / p) if p > 0 else x

def fade(x, fin=.005, fout=.02):
    n_in, n_out = int(fin * SR), int(fout * SR); y = x.copy()
    if n_in: y[:n_in] *= np.linspace(0, 1, n_in)[:, None]
    if n_out: y[-n_out:] *= np.linspace(1, 0, n_out)[:, None]
    return y

def mix(parts, length=None):
    """Layer (clip, offset seconds, gain dB) parts into one buffer."""
    ch = max(p[0].shape[1] for p in parts)
    n = length or max(int(o * SR) + len(c) for c, o, g in parts)
    y = np.zeros((n, ch), np.float32)
    for c, o, g in parts:
        s = int(o * SR); c = c if c.shape[1] == ch else np.repeat(c, ch, 1); e = min(n, s + len(c))
        y[s:e] += c[: e - s] * 10 ** (g / 20)
    return y

def write(rel, x, ogg=False):
    p = folder(pathlib.Path(rel).parent.as_posix()) / pathlib.Path(rel).name
    x = np.clip(x, -1, 1)
    if ogg:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", str(x.shape[1]), "-i", "-", "-c:a", "libvorbis", "-q:a", "7", "-fflags", "+bitexact", "-flags:a", "+bitexact", str(p)],
                       input=x.astype(np.float32).tobytes(), check=True)
    else:
        with wave.open(str(p), "wb") as w:
            w.setnchannels(x.shape[1]); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x * 32767).astype("<i2").tobytes())
    meta(p); print(f"{rel}  {len(x) / SR:6.2f}s  {x.shape[1]}ch"); return p

LEDGER = []
def used(src_key, src_file, out, usage):
    LEDGER.append(dict(source_key=src_key, source_file=src_file, output=out, usage=usage))

# ---------------- music: one primary bed at a time, level-matched at -18 LUFS so crossfades do not jump. The crossfade
# column: None = a lossless source its author made loopable, used sample for sample; a number = trim encoder/edge silence
# and crossfade that many seconds of the tail into the head.
LIGHT = "highpass=f=35,equalizer=f=160:t=q:w=1:g=-5"   # the bass-heavy beds lose their boom (small speakers, children)
MUSIC = [
    ("M01", "M01/unzipped/Exploration Theme/Exploration Theme.wav", None, None, "Music/bgm_start_exploration_theme.ogg", "start screen, gate and the opening's first beats (author: loops seamlessly)"),
    ("M02", "M02/The Cogs.mp3", LIGHT, .05, "Music/bgm_lab_the_cogs.ogg", "the lab after the opening's hand-over: Meet Niko's welcome"),
    ("M03", "M03/first_light_particles_0.wav", None, 4, "Music/bgm_niko_first_light_particles.ogg", "teaching chapters (Niko explains)"),
    ("M04", "M04/claimed_by_the_void_loop.flac", "lowpass=f=9000", None, "Music/bgm_transition_claimed_by_the_void.ogg", "the opening's skin -> muscle -> organs layer changes"),
    ("M05", "M05/outer_space_2.mp3", LIGHT, .05, "Music/bgm_eye_outer_space_loop.ogg", "eye exploration and the inside of the eye"),
    ("M06", "M06/unzipped/ogg/airy.ogg", None, 3, "Music/bgm_pulse_airy.ogg", "retina -> optic nerve -> brain (Airy: the pack's Pulse is sub-bass tension, rejected)"),
    ("M07", "M07/LoopWIP1_0.ogg", "atrim=start=0.045,asetpts=PTS-STARTPTS,lowpass=f=11000", .01, "Music/bgm_quiz_simple_bgm_loop.ogg", "checks and activities"),   # its 45 ms silent head would drop out at every loop (48 beats remain)
    ("M08", "M08/space_city_bpm115_0.ogg", None, None, "Music/bgm_finale_space_city.ogg", "the ending and the end menu"),
]
def build_music():
    for key, src, filt, xfade, out, usage in MUSIC:
        x = load(src, filt)
        if xfade is not None: x = loop(trim_silence(x, -66), xfade) if xfade else trim_silence(x, -66)
        write(out, to_lufs(x, -18), ogg=True); used(key, src, "Assets/HumanEye/Audio/" + out, usage)

# ---------------- effects ----------------
K1, K2, K3 = "K1/unzipped/Audio/", "K2/unzipped/Audio/", "K3/unzipped/Audio/"
O1, O3 = "O1/", "O3/unzipped/sound_effects/"
def s(src, filt=None, stereo=False): return trim_silence(load(src, filt, stereo), -70)
def pitch(x, semitones):   # resample: shifts pitch and length together (fine for short effects)
    f = 2 ** (semitones / 12); idx = np.arange(0, len(x) - 1, f)
    return np.stack([np.interp(idx, np.arange(len(x)), x[:, c]) for c in range(x.shape[1])], 1).astype(np.float32)
def key_of(path): return path.split("/")[0]
def one(out, src, usage, peak=-3, filt=None, st=0, lead=0):
    x = s(src, filt); x = pitch(x, st) if st else x
    if lead: x = np.vstack([np.zeros((int(lead * SR), x.shape[1]), np.float32), x])   # a breath before it (lands behind another cue)
    y = to_peak(x, peak); n = int(lead * SR)
    y[n:] = fade(y[n:], .002, .03)
    write(out, y); used(key_of(src), src, "Assets/HumanEye/Audio/" + out, usage)
def layered(out, parts, usage, peak=-3):
    """parts: (source, offset s, gain dB, ffmpeg filter or None, semitones)"""
    clips = []
    for src, off, g, filt, st in parts:
        x = s(src, filt); clips.append((pitch(x, st) if st else x, off, g)); used(key_of(src), src, "Assets/HumanEye/Audio/" + out, usage)
    write(out, fade(to_peak(mix(clips), peak), .002, .05))
def looped(out, x, xfade, usage, peak, srcs, lead=2048):
    """A seamless loop with a lead-in of its own tail and a RIFF smpl loop from the end of the lead-in to the last sample:
    Unity Web's AAC encoder alters a clip's first 1024 samples (Audio in Web manual), so they must fall outside the loop.
    Unity loops at the smpl points on Windows too (sandbox probe, 15 October 2026); the lead-in plays once."""
    y = to_peak(loop(x, xfade), peak); n = len(y)
    p = write(out, np.vstack([y[-lead:], y]))
    smpl = struct.pack("<9I", 0, 0, round(1e9 / SR), 60, 0, 0, 0, 1, 0) + struct.pack("<6I", 0, 0, lead, lead + n - 1, 0, 0)
    b = bytearray(p.read_bytes()) + b"smpl" + struct.pack("<I", len(smpl)) + smpl
    b[4:8] = struct.pack("<I", len(b) - 8); p.write_bytes(b)
    for src in srcs: used(key_of(src), src, "Assets/HumanEye/Audio/" + out, usage)

CRACKLE = [K2 + f"glitch_00{i}.ogg" for i in (1, 2, 3, 4)] + [K3 + f"switch{i}.ogg" for i in (13, 14)]   # the grains, in scatter order
def crackle(seconds, rate, st=0):
    """The anatomy breakup's electric crackle: the packs' tiny glitch and switch clicks scattered at random (fixed seed)
    over a faint band-limited air bed from the teleport swell. A built texture of downloaded clicks, looped."""
    clicks = [s(c, "highpass=f=1200" if "glitch" in c else "highpass=f=1500") for c in CRACKLE]
    n = int((seconds + .4) * SR); y = np.zeros((n, 2), np.float32); t = 0.0
    while True:
        t += rng.exponential(1 / rate)
        if t > seconds + .3: break
        c = clicks[rng.integers(len(clicks))]; c = pitch(c, rng.uniform(-4, 5) + st); g = 10 ** (rng.uniform(-20, -6) / 20); p = rng.uniform(.15, .85)
        i = int(t * SR); e = min(n, i + len(c)); y[i:e, 0] += c[: e - i, 0] * g * (1 - p); y[i:e, 1] += c[: e - i, 0] * g * p
    bed = load("O2/teleport.wav", "bandpass=f=3500:w=4000", True)
    bed = np.tile(bed, (int(np.ceil(n / len(bed))) + 1, 1))[:n] * 10 ** (-24 / 20)
    return y + bed

def build_sfx():
    # gate and lab (the corridor gate opens the module)
    one("SFX/Gate/gate_start_press.wav", K2 + "confirmation_003.ogg", "GATE_START_PRESS: Begin on the start screen", -6)
    layered("SFX/Gate/gate_iris_scan.wav", [(O1 + "magical_1_0.ogg", 0, 0, None, 0), (K1 + "computerNoise_003.ogg", .1, -16, "lowpass=f=2500,atrim=0:0.9,afade=t=out:st=0.6:d=0.3", 0)], "GATE_IRIS_SCAN: the gate's eye wakes as the opening starts")
    layered("SFX/Gate/gate_unlock.wav", [(K1 + "doorOpen_001.ogg", 0, 0, "lowpass=f=7000", 0), (K2 + "glass_002.ogg", .12, -7, None, 0)], "GATE_UNLOCK: the bolt releases before the leaves move")
    looped("SFX/Gate/gate_servo_loop.wav", s(K1 + "engineCircular_001.ogg", "highpass=f=90,lowpass=f=1200"), .5, "GATE_OPEN_SERVO: loops only while the leaves slide", -9, [K1 + "engineCircular_001.ogg"])
    one("SFX/Gate/gate_open_whoosh.wav", O3 + "whoosh2.wav", "GATE_OPEN_WHOOSH: the leaves reach full travel", -8, "lowpass=f=5000")
    layered("SFX/Gate/gate_entry.wav", [("O2/teleport.wav", 0, 0, "highpass=f=200,lowpass=f=7000", 0), (O1 + "magical_2.ogg", .1, -8, None, 0)], "GATE_ENTRY: the camera crosses the doorway", -6)
    one("SFX/Gate/lab_reveal_sting.wav", O1 + "magical_3.ogg", "LAB_REVEAL_STING: the lab and the title appear", -4)
    layered("SFX/Gate/hologram_on.wav", [(K2 + "glass_006.ogg", 0, 0, None, 0), (K2 + "glass_002.ogg", .09, -2, None, 0), (K2 + "glass_001.ogg", .18, -4, None, 0)], "HOLOGRAM_ON: panels and labels switch on", -9)
    one("SFX/Gate/camera_sweep.wav", O3 + "whoosh.wav", "CAMERA_CINEMATIC_SWEEP: authored camera moves (hand-over cut, object approaches, eye-to-brain, closing overview)", -10, "lowpass=f=3500")
    # ambience: near-subliminal room tones, looped
    looped("Ambience/amb_lab_hum.wav", s(K1 + "spaceEngineSmall_001.ogg", "highpass=f=50,lowpass=f=900", True), 1, "AMB_LAB / AMB_GATE: lab and corridor room tone", -12, [K1 + "spaceEngineSmall_001.ogg"])
    looped("Ambience/amb_eye_hum.wav", s(K1 + "engineCircular_002.ogg", "highpass=f=40,lowpass=f=500", True), 1, "AMB_EYE: inside the eye and the retina close-ups", -12, [K1 + "engineCircular_002.ogg"])
    # Niko and Talk to Niko (subtle)
    one("SFX/Companion/niko_hello.wav", O1 + "magical_7_0.ogg", "NIKO_HELLO: Niko's first appearance and welcome", -6)
    one("SFX/Companion/niko_listen.wav", K2 + "pluck_001.ogg", "NIKO_LISTEN: the Ask Niko panel opens", -9)
    one("SFX/Companion/niko_think.wav", K2 + "bong_001.ogg", "NIKO_THINK: a question was sent (once)", -10, "lowpass=f=3000")
    one("SFX/Companion/niko_reply.wav", K2 + "pluck_002.ogg", "NIKO_REPLY_BEGIN: the answer appears", -12)
    one("SFX/Companion/niko_error.wav", K2 + "question_002.ogg", "NIKO_RESPONSE_ERROR: no answer for that question", -10)
    layered("SFX/Companion/ending_farewell.wav", [(O1 + "magical_4.ogg", 0, 0, None, 0), (K2 + "glass_001.ogg", 0, -8, None, 0)], "ENDING_FAREWELL: after Niko's closing line", -5)
    # anatomy layer changes (electric-cyan breakup): ignition, crackle following the dissolve, completion shimmers
    layered("SFX/Anatomy/dissolve_start.wav", [(K1 + "forceField_002.ogg", 0, -2, "highpass=f=140,lowpass=f=6000", 0), (K2 + "glitch_003.ogg", .03, -6, None, 0), (K2 + "glitch_001.ogg", .09, -8, None, 0)], "SKIN_/MUSCLE_DISSOLVE_START: a layer change ignites", -4)
    looped("SFX/Anatomy/dissolve_crackle_loop.wav", crackle(3.0, 26), .3, "SKIN_DISSOLVE_LOOP: crackle whose level follows the dissolve", -6, CRACKLE + ["O2/teleport.wav"])
    looped("SFX/Anatomy/dissolve_crackle_loop_high.wav", crackle(3.0, 30, 2), .3, "MUSCLE_DISSOLVE_LOOP: the second change, a brighter variant", -6, CRACKLE + ["O2/teleport.wav"])
    one("SFX/Anatomy/layer_reveal.wav", O1 + "magical_5.ogg", "MUSCLE_REVEAL: the next layer stands complete", -5)
    layered("SFX/Anatomy/organs_reveal.wav", [("O2/teleport.wav", 0, -4, "lowpass=f=4000", 0), (O1 + "magical_6_0.ogg", .2, 0, None, 0)], "ORGANS_REVEAL: the organs materialise", -5)
    # the eye
    one("SFX/Eye/eye_appear.wav", O1 + "magical_2.ogg", "EYE_MODEL_APPEAR: the eye materialises (extraction, display)", -5)
    one("SFX/Eye/eye_zoom.wav", O3 + "whoosh.wav", "EYE_ZOOM_ENTER: a meaningful move into the eye", -9, "lowpass=f=3000")
    layered("SFX/Eye/part_scan.wav", [(K2 + "select_004.ogg", 0, 0, None, 0), (K2 + "glass_006.ogg", .05, -6, None, 0)], "PART_SCAN: a structure is isolated", -9)
    one("SFX/Eye/cornea_ping.wav", K2 + "glass_002.ogg", "CORNEA_HIGHLIGHT", -8)
    one("SFX/Eye/iris_shimmer.wav", O1 + "magical_6_0.ogg", "IRIS_HIGHLIGHT", -7)
    one("SFX/Eye/pupil_dilate.wav", K2 + "maximize_006.ogg", "PUPIL_APERTURE: the pupil widens (dimmer light)", -9)
    one("SFX/Eye/pupil_constrict.wav", K2 + "minimize_006.ogg", "PUPIL_APERTURE: the pupil narrows (brighter light)", -9)
    one("SFX/Eye/lens_focus.wav", K2 + "maximize_005.ogg", "LENS_FOCUS: the lens changes shape", -9)
    one("SFX/Eye/retina_shimmer.wav", O1 + "magical_7_0.ogg", "RETINA_HIGHLIGHT: the retina isolated; light on the photoreceptors (phototransduction)", -7, None, -3)
    one("SFX/Eye/label_appear.wav", K2 + "glass_006.ogg", "LABEL_APPEAR: labels and info cards (debounced)", -16)
    # retina -> optic nerve -> brain (gold signals: living information, not lasers)
    layered("SFX/OpticNerveBrain/neural_signal_start.wav", [(O1 + "magical_1_0.ogg", 0, 0, None, 2), (K2 + "pluck_001.ogg", 0, -6, None, 0)], "NEURAL_SIGNAL_START: the retina sends the first signal", -6)
    tone = s(O1 + "magical_6_0.ogg", None, True)[int(.15 * SR): int(.95 * SR)]
    tone = np.concatenate([tone, tone[::-1], tone, tone[::-1]])   # palindromic repeats: no clicks at the joins
    trem = (1 - .55 * (.5 + .5 * np.sin(2 * np.pi * 5 * np.arange(len(tone)) / SR)))[:, None]
    air = load("O2/teleport.wav", "bandpass=f=4000:w=3000", True); air = np.tile(air, (3, 1))[: len(tone)] * 10 ** (-22 / 20)
    looped("SFX/OpticNerveBrain/optic_pulse_travel_loop.wav", tone * trem + air, .25, "OPTIC_PULSE_TRAVEL: follows the lead gold pulse (3D)", -8, [O1 + "magical_6_0.ogg", "O2/teleport.wav"])
    one("SFX/OpticNerveBrain/pulse_near_brain.wav", K2 + "maximize_004.ogg", "OPTIC_PULSE_NEAR_BRAIN: the pulse's last stretch", -12)
    layered("SFX/OpticNerveBrain/pulse_brain_arrival.wav", [(K2 + "glass_001.ogg", 0, 0, None, 0), (O1 + "magical_5.ogg", .05, -6, None, 0)], "PULSE_BRAIN_ARRIVAL: the signal reaches the brain", -6)
    layered("SFX/OpticNerveBrain/brain_reveal.wav", [(O1 + "magical_3.ogg", 0, 0, None, -2), ("O2/teleport.wav", 0, -8, "lowpass=f=2500", 0)], "BRAIN_REVEAL: the brain appears", -5)
    one("SFX/OpticNerveBrain/cortex_highlight.wav", K2 + "glass_004.ogg", "VISUAL_CORTEX_HIGHLIGHT: the visual area lights", -10, "lowpass=f=9000")
    # interface (one family, quiet) and checks (supportive, never a buzzer)
    one("SFX/UI/ui_hover.wav", K3 + "rollover2.ogg", "UI_HOVER: pointer enters a control (cooldown)", -14)
    one("SFX/UI/ui_click.wav", K3 + "click3.ogg", "UI_CLICK: buttons", -10)
    one("SFX/UI/ui_open.wav", K2 + "open_002.ogg", "UI_OPEN_PANEL: a panel opens", -12)
    one("SFX/UI/ui_close.wav", K2 + "close_002.ogg", "UI_CLOSE_PANEL: a panel closes", -12)
    one("SFX/UI/ui_skip.wav", K2 + "minimize_004.ogg", "UI_SKIP_INTRO: Skip intro", -10)
    one("SFX/UI/return_menu.wav", K2 + "minimize_005.ogg", "RETURN_MENU: back to the start screen", -10)
    one("SFX/Quiz/quiz_open.wav", K2 + "question_003.ogg", "QUIZ_OPEN: a check starts", -9)
    layered("SFX/Quiz/quiz_correct.wav", [(K2 + "confirmation_001.ogg", 0, 0, None, 0), (O1 + "magical_7_0.ogg", .05, -12, None, 0)], "QUIZ_CORRECT: a right answer", -6)
    one("SFX/Quiz/quiz_wrong.wav", K2 + "question_004.ogg", "QUIZ_WRONG: a gentle try-again", -10)
    one("SFX/Quiz/quiz_hint.wav", K2 + "pluck_002.ogg", "QUIZ_HINT: a hint (after the try-again tone)", -10, None, -3, .3)
    one("SFX/UI/ui_unavailable.wav", K2 + "question_002.ogg", "unavailable action, locked chapter (a soft no, never a buzzer)", -12)
    layered("SFX/Quiz/quiz_complete.wav", [(K2 + "confirmation_002.ogg", 0, 0, None, 0), (O1 + "magical_3.ogg", .15, -6, None, 0)], "QUIZ_COMPLETE: a check is finished", -5)

# Sounds from before the overhaul that the production flow still plays (Builder.Audio keeps their keys).
_GEN = dict(author="Human Eye project", licence="Project-owned (synthesised in-project, no third-party audio)", download_url="", download_date="", attribution="")
_KENNEY_IS = dict(source_key="K2", title="Interface Sounds 1.0 (imported before the overhaul)", author="Kenney (www.kenney.nl)", licence="CC0 1.0 (public domain dedication)",
                  source_page="https://kenney.nl/assets/interface-sounds", download_url="", download_date="", attribution="Credit appreciated, not required")
RETAINED = [
    dict(_KENNEY_IS, original_file="glass_001.ogg", processed_file="Assets/HumanEye/ThirdParty/Kenney/Audio/chime_discovery.ogg", usage="DISCOVERY_STAMP (key discovery): a part's discovery stamp"),
    dict(_KENNEY_IS, original_file="bong_001.ogg", processed_file="Assets/HumanEye/ThirdParty/Kenney/Audio/chime_soft_bong.ogg", usage="EXPLORE_INSIDE_UNLOCK (key unlock): Explore Inside opens the eye"),
    dict(_GEN, source_key="GEN", title="fx_chapter (Tools/HumanEye/generate_ui_sfx.py)", source_page="Tools/HumanEye/generate_ui_sfx.py", original_file="-", processed_file="Assets/HumanEye/Audio/SFX/fx_chapter.wav", usage="CHAPTER_START (key chapter): a chapter begins"),
    dict(_GEN, source_key="GEN", title="fx_lightmove (Tools/HumanEye/generate_ui_sfx.py)", source_page="Tools/HumanEye/generate_ui_sfx.py", original_file="-", processed_file="Assets/HumanEye/Audio/SFX/fx_lightmove.wav", usage="LIGHT_TRAVEL (key lightmove): light travels to the retina, the light level rises"),
    dict(_GEN, source_key="GEN", title="fx_titlehit (Tools/HumanEye/generate_reference_sfx.py)", source_page="Tools/HumanEye/generate_reference_sfx.py", original_file="-", processed_file="Assets/HumanEye/Audio/SFX/fx_titlehit.wav", usage="EYE_BRAIN_TITLE (key titlehit): FROM EYE TO BRAIN"),
]

def write_ledger():
    pages = json.loads((SRC / "pages.json").read_text(encoding="utf-8")) if (SRC / "pages.json").exists() else {}
    manifest = {m["file"].replace("\\", "/"): m for m in json.loads((SRC / "manifest.json").read_text(encoding="utf-8"))}
    KENNEY = {"K1": ("Sci-Fi Sounds", "https://kenney.nl/assets/sci-fi-sounds"), "K2": ("Interface Sounds", "https://kenney.nl/assets/interface-sounds"),
              "K3": ("UI Audio", "https://kenney.nl/assets/ui-audio"), "K4": ("Impact Sounds", "https://kenney.nl/assets/impact-sounds")}
    rows = []
    for r in LEDGER:
        k = r["source_key"]; p = pages.get(k, {})
        title, page = (KENNEY[k] if k in KENNEY else (p.get("title", ""), p.get("url", "")))
        author = "Kenney (www.kenney.nl)" if k in KENNEY else p.get("author", "")
        name = r["source_file"].split("/")[-1]   # a direct download, else the pack archive it came from
        archive = next((m for f, m in manifest.items() if f.endswith("/" + name)), None) or next((m for f, m in manifest.items() if f"/{k}/" in f), {})
        rows.append(dict(source_key=k, title=title, author=author, licence="CC0 1.0 (public domain dedication)", source_page=page,
                         download_url=archive.get("url", ""), download_date=archive.get("date", ""), original_file=r["source_file"].split("/")[-1],
                         processed_file=r["output"], usage=r["usage"], attribution=(p.get("attribution", "") or ("Credit appreciated, not required" if k in KENNEY else ""))))
    rows += RETAINED
    out = folder("Licenses") / "AUDIO_SOURCES.csv"
    with open(out, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)
    meta(out); print("ledger rows", len(rows))
    for k, (title, _) in KENNEY.items():   # the licence texts that came with the downloads
        lic = SRC / k / "unzipped/License.txt"
        if lic.exists() and any(r["source_key"] == k for r in rows):
            dst = folder("Licenses") / f"Kenney_{title.replace(' ', '')}_License.txt"; dst.write_bytes(lic.read_bytes()); meta(dst)
    readme = SRC / "M01/unzipped/Exploration Theme/readme.txt"
    if readme.exists(): dst = folder("Licenses") / "CleytonKauffman_ExplorationTheme_readme.txt"; dst.write_bytes(readme.read_bytes()); meta(dst)

if __name__ == "__main__":
    build_music(); build_sfx(); write_ledger()
