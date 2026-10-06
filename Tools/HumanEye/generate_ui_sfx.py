"""Generates the project-owned interaction/lesson SFX set in Assets/HumanEye/Audio/SFX/ (pure Python, deterministic,
48 kHz 16-bit mono, DC-blocked, faded, peak-limited). Companion to generate_intro_sfx.py; same conventions.
Keys (wired by WowPatch): label, isolate, popup, popdown, lightmove, cell, dive, overview, complete, nikoscan, chapter.
Run: python Tools/HumanEye/generate_ui_sfx.py"""
import math, os, random, struct, wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "HumanEye", "Audio", "SFX")
RATE = 48000
TAU = 2 * math.pi


def db(x): return 10 ** (x / 20)
def lp_coef(hz): return 1 - math.exp(-TAU * hz / RATE)
def glide(f0, f1, t, dur): return f0 * (f1 / f0) ** min(max(t / dur, 0.0), 1.0)
def noise(): return random.random() * 2 - 1
def env_ad(t, a, d): return (1 - math.exp(-t / a)) * math.exp(-t / d) if t >= 0 else 0.0


def write(name, samples, peak_db=-6.0, fade_in=0.005, fade_out=0.02):
    r = 1 - TAU * 5 / RATE
    x1 = y1 = 0.0
    for i, x in enumerate(samples):
        y1 = x - x1 + r * y1; x1 = x
        samples[i] = y1
    n, ni, no = len(samples), int(fade_in * RATE), int(fade_out * RATE)
    for i in range(ni): samples[i] *= 0.5 - 0.5 * math.cos(math.pi * i / ni)
    for i in range(no): samples[n - 1 - i] *= 0.5 - 0.5 * math.cos(math.pi * i / no)
    peak = max(abs(s) for s in samples) or 1.0
    scale = db(peak_db) / peak
    ints = [max(-32767, min(32767, int(round(s * scale * 32767)))) for s in samples]
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(struct.pack("<%dh" % n, *ints))
    rms = math.sqrt(sum(v * v for v in ints) / n) / 32767
    print("%-18s %5.2f s  peak %6.1f dBFS  rms %6.1f dBFS" % (name, n / RATE, peak_db, 20 * math.log10(max(rms, 1e-6))))


def tone(f, t, h2=0.0, h3=0.0): return math.sin(TAU * f * t) + h2 * math.sin(TAU * 2 * f * t) + h3 * math.sin(TAU * 3 * f * t)


def label():
    """0.16 s label tick: 1.6 kHz sine blip + tiny click. Quiet, one per label."""
    random.seed(11); n = int(0.16 * RATE); out = []
    for i in range(n):
        t = i / RATE
        out.append(env_ad(t, 0.002, 0.035) * tone(1600, t, 0.3) + 0.35 * noise() * math.exp(-t / 0.0015))
    write("ui_label.wav", out, peak_db=-12.0)


def isolate():
    """0.6 s airy settle: band noise sweeping 3 kHz -> 500 Hz, then a soft low sine resolve. 'Everything else fades back'."""
    random.seed(12); n = int(0.6 * RATE); out = []; lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        fc = glide(3000.0, 500.0, t, 0.45)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 3) * (lp - hp)
        band = (lp - hp) * (1 - t / 0.6)
        out.append(0.9 * band + 0.5 * env_ad(t - 0.25, 0.02, 0.2) * tone(330, t, 0.2))
    write("ui_isolate.wav", out, peak_db=-12.0, fade_out=0.08)


def chime(name, notes, step, decay, peak):
    random.seed(13); dur = step * len(notes) + decay * 2.5; n = int(dur * RATE); out = []
    for i in range(n):
        t = i / RATE; s = 0.0
        for k, f in enumerate(notes):
            te = t - 0.01 - k * step
            if te < 0: break
            s += env_ad(te, 0.003, decay) * (math.sin(TAU * f * te) + 0.4 * math.sin(TAU * f * 1.004 * te + 0.7) + 0.2 * math.sin(TAU * 3 * f * te) * math.exp(-te / 0.05))
        out.append(s)
    write(name, out, peak_db=peak, fade_out=0.1)


def lightmove():
    """1.2 s warm shimmer glide: sine cluster 700 -> 2100 Hz with 6 Hz tremolo; the light front moving."""
    random.seed(14); n = int(1.2 * RATE); out = []; ph = [0.0, 0.0, 0.0]; det = [1.0, 1.5, 2.01]
    for i in range(n):
        t = i / RATE; s = 0.0
        f = glide(700.0, 2100.0, t, 1.0)
        for k in range(3):
            ph[k] += TAU * f * det[k] / RATE
            s += math.sin(ph[k]) * (0.8 ** k)
        trem = 0.75 + 0.25 * math.sin(TAU * 6 * t)
        env = min(t / 0.08, 1.0) * (1 if t < 0.95 else max(0.0, 1 - (t - 0.95) / 0.25))
        out.append(s * trem * env)
    write("fx_lightmove.wav", out, peak_db=-14.0, fade_in=0.02, fade_out=0.12)


def cell():
    """0.35 s soft electric blip: 420 Hz carrier with 38 Hz modulation, quick decay. A cell waking up."""
    random.seed(15); n = int(0.35 * RATE); out = []
    for i in range(n):
        t = i / RATE
        fm = 420 + 60 * math.sin(TAU * 38 * t)
        out.append(env_ad(t, 0.004, 0.09) * (math.sin(TAU * fm * t) + 0.3 * math.sin(TAU * fm * 2 * t)))
    write("fx_cell.wav", out, peak_db=-12.0)


def dive():
    """1.0 s descent into the retina: dark noise fall 2.5k -> 200 Hz plus a low sine drop 160 -> 55 Hz."""
    random.seed(16); n = int(1.0 * RATE); out = []; lp1 = lp2 = 0.0; ph = 0.0
    for i in range(n):
        t = i / RATE
        a = lp_coef(glide(2500.0, 200.0, t, 0.8)); lp1 += a * (noise() - lp1); lp2 += a * (lp1 - lp2)
        ph += TAU * glide(160.0, 55.0, t, 0.7) / RATE
        env = min(t / 0.05, 1.0) * (1 if t < 0.7 else max(0.0, 1 - (t - 0.7) / 0.3))
        out.append(env * (2.2 * lp2 + 0.5 * math.sin(ph)))
    write("fx_dive.wav", out, peak_db=-8.0, fade_in=0.02, fade_out=0.15)


def overview():
    """1.8 s warm rise and land: riser noise + chord A3 E4 A4 C#5 swelling in, landing with a soft low thump."""
    random.seed(17); n = int(1.8 * RATE); out = []; lp = 0.0
    chord = [220.0, 329.6, 440.0, 554.4]
    for i in range(n):
        t = i / RATE
        u = min(t / 0.9, 1.0)
        lp += lp_coef(glide(300.0, 4000.0, t, 0.9)) * (noise() - lp)
        s = sum(math.sin(TAU * f * t + k) * (0.9 ** k) for k, f in enumerate(chord)) * 0.25
        swell = u * u if t < 0.9 else max(0.0, 1 - (t - 0.9) / 0.9)
        land = env_ad(t - 0.9, 0.005, 0.25) * math.sin(TAU * glide(110.0, 55.0, t - 0.9, 0.3) * max(t - 0.9, 0))
        out.append(s * swell + 0.6 * lp * u * (1 if t < 0.9 else 0) + 0.8 * land)
    write("fx_overview.wav", out, peak_db=-6.0, fade_in=0.02, fade_out=0.3)


def complete():
    """2.4 s gentle resolve: A4 C#5 E5 A5 arpeggio over a sustained warm pad; the investigation is complete."""
    random.seed(18); n = int(2.4 * RATE); out = []
    notes = [440.0, 554.37, 659.26, 880.0]
    for i in range(n):
        t = i / RATE; s = 0.0
        for k, f in enumerate(notes):
            te = t - 0.02 - k * 0.16
            if te >= 0: s += env_ad(te, 0.004, 0.5) * (math.sin(TAU * f * te) + 0.3 * math.sin(TAU * 2 * f * te) * math.exp(-te / 0.1))
        pad = sum(math.sin(TAU * f * 0.5 * t + k) for k, f in enumerate(notes[:3])) * 0.12 * min(t / 0.6, 1.0) * max(0.0, 1 - max(t - 1.4, 0) / 1.0)
        out.append(0.6 * s + pad)
    write("fx_complete.wav", out, peak_db=-6.0, fade_out=0.4)


def nikoscan():
    """1.4 s Niko's scanner: alternating 880/1320 Hz beeps at 7 Hz over a rising band-pass noise sweep."""
    random.seed(19); n = int(1.4 * RATE); out = []; lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        beat = (t * 7) % 1.0
        f = 880.0 if int(t * 7) % 2 == 0 else 1320.0
        beep = math.sin(TAU * f * t) * (1 if beat < 0.35 else 0) * env_ad(beat / 7, 0.002, 0.05)
        fc = glide(500.0, 4000.0, t, 1.2)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 2.5) * (lp - hp)
        env = 1 if t < 1.2 else max(0.0, 1 - (t - 1.2) / 0.2)
        out.append((0.45 * beep + 0.9 * (lp - hp)) * env)
    write("fx_nikoscan.wav", out, peak_db=-10.0, fade_out=0.1)


def chapter():
    """0.9 s soft chapter turn: low wood-ish thud + breathy air, a page in the investigation."""
    random.seed(20); n = int(0.9 * RATE); out = []; lp = 0.0
    for i in range(n):
        t = i / RATE
        lp += lp_coef(1200.0) * (noise() - lp)
        out.append(env_ad(t, 0.003, 0.12) * tone(glide(180.0, 90.0, t, 0.2), t, 0.4) + 0.5 * lp * env_ad(t, 0.01, 0.3))
    write("fx_chapter.wav", out, peak_db=-10.0, fade_out=0.15)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    label(); isolate()
    chime("ui_popup.wav", [784.0, 1174.7], 0.09, 0.22, -10.0)
    chime("ui_popdown.wav", [1174.7, 784.0], 0.09, 0.18, -12.0)
    lightmove(); cell(); dive(); overview(); complete(); nikoscan(); chapter()
    print("wrote", OUT)
