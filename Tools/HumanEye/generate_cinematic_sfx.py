"""Generates the project-owned cinematic SFX set in Assets/HumanEye/Audio/SFX/ (pure Python, deterministic,
48 kHz 16-bit mono, DC-blocked, faded, peak-limited). Companion to generate_ui_sfx.py / generate_intro_sfx.py; same
conventions, no numpy. Keys (wired by Builder.Sfx.cs -> CinematicSfx): irisglow, approach, pupilenter, interior,
lightsweep, nikoappear, signaltravel, signalarrive, layerreveal, softwhoosh, plus the cleaner interface replacements
ui_select2 / ui_tap2 / ui_confirm2 / ui_deny2 (keys select, tap, confirm, deny).
Design: modern, clean, airy, low-level. Reveals are layered (low impact + airy rise + detail); no harsh clicks; beds sit
around -24..-18 dBFS RMS with peaks <= -6 dBFS. Run: python Tools/HumanEye/generate_cinematic_sfx.py"""
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
def ramp(t, t0, t1): return min(max((t - t0) / (t1 - t0), 0.0), 1.0)          # linear 0 -> 1 between t0 and t1
def hann(t, dur): return 0.5 - 0.5 * math.cos(TAU * min(max(t / dur, 0.0), 1.0))  # bell 0 -> 1 -> 0 over dur


def write(name, samples, peak_db=-6.0, rms_db=None, fade_in=0.005, fade_out=0.02):
    """DC-block (5 Hz), raised-cosine fades, normalise to peak_db (and never above rms_db RMS when given), write, report."""
    r = 1 - TAU * 5 / RATE
    x1 = y1 = 0.0
    for i, x in enumerate(samples):
        y1 = x - x1 + r * y1; x1 = x
        samples[i] = y1
    n, ni, no = len(samples), int(fade_in * RATE), int(fade_out * RATE)
    for i in range(ni): samples[i] *= 0.5 - 0.5 * math.cos(math.pi * i / ni)
    for i in range(no): samples[n - 1 - i] *= 0.5 - 0.5 * math.cos(math.pi * i / no)
    if any(math.isnan(s) or math.isinf(s) for s in samples): raise ValueError(name + ": NaN/inf in synthesis")
    peak = max(abs(s) for s in samples) or 1.0
    rms = math.sqrt(sum(s * s for s in samples) / n) or 1.0
    scale = db(peak_db) / peak
    if rms_db is not None: scale = min(scale, db(rms_db) / rms)
    ints = [max(-32767, min(32767, int(round(s * scale * 32767)))) for s in samples]
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(struct.pack("<%dh" % n, *ints))
    pk = max(abs(v) for v in ints) / 32767
    rm = math.sqrt(sum(v * v for v in ints) / n) / 32767
    print("%-20s %5.2f s  peak %6.1f dBFS  rms %6.1f dBFS" % (name, n / RATE, 20 * math.log10(max(pk, 1e-6)), 20 * math.log10(max(rm, 1e-6))))


def tone(f, t, h2=0.0, h3=0.0): return math.sin(TAU * f * t) + h2 * math.sin(TAU * 2 * f * t) + h3 * math.sin(TAU * 3 * f * t)


def irisglow():
    """1.6 s soft shimmer swell: 7 high sines (1.3-3.6 kHz) each doubled with a 1.5-4 Hz detune, swelling in over 0.8 s
    and settling away, with a whisper of 1.2-2.5 kHz air under it. The iris catching the light."""
    random.seed(41); n = int(1.6 * RATE); out = []; lp = hp = 0.0
    freqs = [1320.0, 1650.0, 1980.0, 2310.0, 2640.0, 3080.0, 3630.0]
    det = [random.uniform(1.5, 4.0) for _ in freqs]
    ph1 = [random.random() * TAU for _ in freqs]; ph2 = [random.random() * TAU for _ in freqs]
    gain = [(1320.0 / f) ** 0.8 for f in freqs]
    for i in range(n):
        t = i / RATE; s = 0.0
        for k in range(len(freqs)):
            s += gain[k] * (math.sin(TAU * freqs[k] * t + ph1[k]) + math.sin(TAU * (freqs[k] + det[k]) * t + ph2[k]))
        lp += lp_coef(2500.0) * (noise() - lp); hp += lp_coef(1200.0) * (lp - hp)
        swell = ramp(t, 0.0, 0.8) ** 2 if t < 0.8 else math.exp(-(t - 0.8) / 0.45)
        out.append((0.12 * s + 0.6 * (lp - hp)) * swell)
    write("fx_irisglow.wav", out, peak_db=-12.0, rms_db=-26.0, fade_in=0.03, fade_out=0.2)


def approach():
    """7 s low airy pad, gentle rise: detuned low oscillators (A1 55 / E2 82.4 / A2 110 with a slow 0.07 Hz wander) under
    3-pole noise whose cutoff opens 240 -> 1400 Hz across the clip; level grows about +6 dB over the length. The approach."""
    random.seed(42); n = int(7.0 * RATE); out = []
    pa = pb = pc = lp1 = lp2 = lp3 = 0.0
    for i in range(n):
        t = i / RATE; u = t / 7.0
        drift = 0.25 * math.sin(TAU * 0.07 * t)
        pa += TAU * (55.0 + drift) / RATE; pb += TAU * (82.4 - drift) / RATE; pc += TAU * (110.3 + 0.5 * drift) / RATE
        osc = math.sin(pa) + 0.35 * math.sin(2 * pa + 0.4) + 0.55 * math.sin(pb) + 0.3 * math.sin(pc) * ramp(t, 2.0, 6.0)
        a = lp_coef(glide(240.0, 1400.0, t, 6.4)); lp1 += a * (noise() - lp1); lp2 += a * (lp1 - lp2); lp3 += a * (lp2 - lp3)
        breathe = 0.85 + 0.15 * math.sin(TAU * 0.18 * t - math.pi / 2)
        rise = 0.5 + 0.5 * u * u
        out.append(breathe * rise * (0.45 * osc + 4.0 * lp3))
    write("fx_approach.wav", out, peak_db=-8.0, rms_db=-22.0, fade_in=1.2, fade_out=1.0)


def pupilenter():
    """2.4 s airy descending whoosh into a soft low impact: band noise falls 4.2 kHz -> 160 Hz over 1.5 s under a bell
    envelope, then at 1.45 s a soft sine thump 85 -> 42 Hz (6 ms attack, tau .32) with a dark noise tail. Entering the pupil."""
    random.seed(43); n = int(2.4 * RATE); out = []; lp = hp = d1 = d2 = d3 = 0.0; ph = 0.0; t0 = 1.45; ad = lp_coef(140.0)
    for i in range(n):
        t = i / RATE
        fc = glide(4200.0, 160.0, t, 1.5)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 3) * (lp - hp)
        whoosh = (lp - hp) * hann(t, 1.6) ** 0.7
        x = noise(); d1 += ad * (x - d1); d2 += ad * (d1 - d2); d3 += ad * (d2 - d3)
        te = t - t0; thump = 0.0
        if te >= 0:
            ph += TAU * glide(85.0, 42.0, te, 0.35) / RATE
            thump = (math.sin(ph) + 0.25 * math.sin(2 * ph) * math.exp(-te / 0.1)) * (1 - math.exp(-te / 0.006)) * math.exp(-te / 0.32) + 3.0 * d3 * math.exp(-te / 0.4)
        out.append(1.4 * whoosh + 0.9 * thump)
    write("fx_pupilenter.wav", out, peak_db=-6.0, rms_db=-20.0, fade_in=0.02, fade_out=0.25)


def interior():
    """3 s interior reveal, layered: an airy rise (band noise 220 -> 3200 Hz over 1.2 s, then easing away), soft detail
    (nine sparse 2-4 kHz sine ticks between 0.7 and 2.4 s) and a warm pad (A2 E3 A3 C#4) swelling from 0.5 s. Light landing inside."""
    random.seed(44); n = int(3.0 * RATE); out = []; lp = hp = 0.0
    chord = [110.0, 164.8, 220.0, 277.2]
    ticks = sorted(random.uniform(0.7, 2.4) for _ in range(9)); tf = [random.uniform(2000.0, 4000.0) for _ in ticks]
    for i in range(n):
        t = i / RATE
        fc = glide(220.0, 3200.0, t, 1.2)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 3.5) * (lp - hp)
        air = (lp - hp) * (ramp(t, 0.0, 1.2) ** 1.5 if t < 1.2 else math.exp(-(t - 1.2) / 0.6))
        detail = sum(env_ad(t - tk, 0.002, 0.045) * math.sin(TAU * f * (t - tk)) for tk, f in zip(ticks, tf) if t >= tk)
        pad = sum(math.sin(TAU * f * t + k * 0.9) * (0.85 ** k) for k, f in enumerate(chord)) * 0.3
        swell = ramp(t, 0.5, 1.7) ** 2 * (1 if t < 2.2 else max(0.0, 1 - (t - 2.2) / 0.8))
        out.append(1.3 * air + 0.22 * detail + pad * swell)
    write("fx_interior.wav", out, peak_db=-8.0, rms_db=-22.0, fade_in=0.03, fade_out=0.35)


def lightsweep():
    """1.8 s soft bright sweep: band noise arcing 700 -> 6500 -> 1800 Hz (a light front passing) under a sine pair gliding
    1100 -> 3300 Hz with a 4 Hz shimmer; one smooth bell envelope."""
    random.seed(45); n = int(1.8 * RATE); out = []; lp = hp = 0.0; p1 = p2 = 0.0
    for i in range(n):
        t = i / RATE
        fc = glide(700.0, 6500.0, t, 0.9) if t < 0.9 else glide(6500.0, 1800.0, t - 0.9, 0.9)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 2.5) * (lp - hp)
        f = glide(1100.0, 3300.0, t, 1.5)
        p1 += TAU * f / RATE; p2 += TAU * f * 1.502 / RATE
        shimmer = (math.sin(p1) + 0.5 * math.sin(p2)) * (0.8 + 0.2 * math.sin(TAU * 4 * t))
        env = hann(t, 1.8) ** 0.8
        out.append((1.5 * (lp - hp) + 0.28 * shimmer) * env)
    write("fx_lightsweep.wav", out, peak_db=-12.0, rms_db=-26.0, fade_in=0.03, fade_out=0.15)


def nikoappear():
    """1.1 s warm soft chime + short air: a close major triad (C5 E5 G5, 35 ms apart, gentle detune, decay .32 s) over a
    puff of 1-3 kHz air (12 ms attack, 120 ms decay). Niko arrives, kindly."""
    random.seed(46); n = int(1.1 * RATE); out = []; lp = hp = 0.0
    notes = [523.25, 659.26, 783.99]
    for i in range(n):
        t = i / RATE; s = 0.0
        for k, f in enumerate(notes):
            te = t - 0.012 - k * 0.035
            if te < 0: break
            s += env_ad(te, 0.004, 0.32) * (math.sin(TAU * f * te) + 0.35 * math.sin(TAU * f * 1.003 * te + 0.6) + 0.15 * math.sin(TAU * 2 * f * te) * math.exp(-te / 0.08))
        lp += lp_coef(3000.0) * (noise() - lp); hp += lp_coef(1000.0) * (lp - hp)
        out.append(0.45 * s + 0.8 * (lp - hp) * env_ad(t, 0.012, 0.12))
    write("fx_nikoappear.wav", out, peak_db=-10.0, fade_out=0.15)


def signaltravel():
    """3.6 s soft travelling tone, rising: sine 196 -> 392 Hz (G3 -> G4, harmonics shaded in) pulsing gently at 5 Hz
    (depth .35, sinusoidal so it never clicks) with a quiet air band riding three octaves above it. The impulse on its way."""
    random.seed(47); n = int(3.6 * RATE); out = []; ph = 0.0; lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        f = glide(196.0, 392.0, t, 3.2)
        ph += TAU * f / RATE
        core = math.sin(ph) + 0.3 * math.sin(2 * ph) + 0.12 * math.sin(3 * ph)
        pulse = 0.65 + 0.35 * (0.5 + 0.5 * math.sin(TAU * 5 * t - math.pi / 2))
        lp += lp_coef(f * 16) * (noise() - lp); hp += lp_coef(f * 8) * (lp - hp)
        env = ramp(t, 0.0, 0.25) * (1 if t < 3.1 else max(0.0, 1 - (t - 3.1) / 0.5))
        out.append((core + 0.7 * (lp - hp)) * pulse * env)
    write("fx_signaltravel.wav", out, peak_db=-12.0, rms_db=-23.0, fade_in=0.05, fade_out=0.3)


def signalarrive():
    """1 s soft neural activation chime + low pad: a bright dyad (C6 E6, 25 ms apart, 1.004 detune, decay .22 s) over a
    low pad (A2 + E3 sines) swelling in 30 ms and fading over .4 s. The signal lands."""
    random.seed(48); n = int(1.0 * RATE); out = []
    for i in range(n):
        t = i / RATE; s = 0.0
        for k, f in enumerate([1046.5, 1318.5]):
            te = t - 0.01 - k * 0.025
            if te < 0: break
            s += env_ad(te, 0.003, 0.22) * (math.sin(TAU * f * te) + 0.4 * math.sin(TAU * f * 1.004 * te + 0.5))
        pad = (math.sin(TAU * 110.0 * t) + 0.6 * math.sin(TAU * 164.8 * t + 0.8)) * env_ad(t, 0.03, 0.4)
        out.append(0.5 * s + 0.6 * pad)
    write("fx_signalarrive.wav", out, peak_db=-10.0, fade_out=0.15)


def layerreveal():
    """1.3 s low airy rise + soft detail: a low sine 68 -> 105 Hz swelling to .45 s and easing away, band noise opening
    260 -> 2600 Hz over .7 s, and one soft 2.2 kHz detail tick at .42 s. A structure coming forward."""
    random.seed(49); n = int(1.3 * RATE); out = []; ph = 0.0; lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        ph += TAU * glide(68.0, 105.0, t, 0.5) / RATE
        low = math.sin(ph) * (ramp(t, 0.0, 0.45) ** 2 if t < 0.45 else math.exp(-(t - 0.45) / 0.35))
        fc = glide(260.0, 2600.0, t, 0.7)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 3) * (lp - hp)
        air = (lp - hp) * (ramp(t, 0.0, 0.7) ** 1.5 if t < 0.7 else math.exp(-(t - 0.7) / 0.3))
        tick = env_ad(t - 0.42, 0.002, 0.06) * math.sin(TAU * 2200.0 * (t - 0.42))
        out.append(0.8 * low + 1.5 * air + 0.25 * tick)
    write("fx_layerreveal.wav", out, peak_db=-12.0, rms_db=-26.0, fade_in=0.02, fade_out=0.2)


def softwhoosh():
    """0.7 s very soft transition air: band noise arcing 500 -> 2800 -> 800 Hz with a slightly front-weighted bell envelope."""
    random.seed(50); n = int(0.7 * RATE); out = []; lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        fc = glide(500.0, 2800.0, t, 0.28) if t < 0.28 else glide(2800.0, 800.0, t - 0.28, 0.42)
        lp += lp_coef(fc) * (noise() - lp); hp += lp_coef(fc / 2.5) * (lp - hp)
        env = ramp(t, 0.0, 0.26) ** 1.2 if t < 0.26 else max(0.0, 1 - (t - 0.26) / 0.44) ** 1.6
        out.append((lp - hp) * env)
    write("fx_softwhoosh.wav", out, peak_db=-16.0, rms_db=-30.0, fade_in=0.02, fade_out=0.1)


def ui_select2():
    """0.12 s soft select: 1.25 kHz sine blip (2nd harmonic shaded) with a low-passed 4 kHz noise grain; modern, no click."""
    random.seed(51); n = int(0.12 * RATE); out = []; lp = 0.0
    for i in range(n):
        t = i / RATE
        lp += lp_coef(4000.0) * (noise() - lp)
        out.append(env_ad(t, 0.0025, 0.03) * tone(1250.0, t, 0.25) + 0.9 * lp * env_ad(t, 0.001, 0.006))
    write("ui_select2.wav", out, peak_db=-14.0, fade_in=0.002, fade_out=0.03)


def ui_tap2():
    """0.08 s soft tap: 900 Hz sine with a 2.5 kHz low-passed grain; quieter and shorter than select."""
    random.seed(52); n = int(0.08 * RATE); out = []; lp = 0.0
    for i in range(n):
        t = i / RATE
        lp += lp_coef(2500.0) * (noise() - lp)
        out.append(env_ad(t, 0.002, 0.018) * tone(900.0, t, 0.2) + 1.2 * lp * env_ad(t, 0.001, 0.005))
    write("ui_tap2.wav", out, peak_db=-16.0, fade_in=0.002, fade_out=0.02)


def ui_confirm2():
    """0.18 s soft confirm: a quick rising dyad A5 -> E6 (45 ms apart, decay 55 ms) with gentle detune; affirmative."""
    random.seed(53); n = int(0.18 * RATE); out = []
    for i in range(n):
        t = i / RATE; s = 0.0
        for k, f in enumerate([880.0, 1318.5]):
            te = t - 0.004 - k * 0.045
            if te < 0: break
            s += env_ad(te, 0.003, 0.055) * (math.sin(TAU * f * te) + 0.3 * math.sin(TAU * f * 1.004 * te + 0.5))
        out.append(s)
    write("ui_confirm2.wav", out, peak_db=-12.0, fade_in=0.002, fade_out=0.04)


def ui_deny2():
    """0.16 s soft deny: two muted low blips (330 Hz easing to 290 Hz, 65 ms apart, decay 35 ms) through a 1.6 kHz low-pass;
    a polite no, never a buzzer."""
    random.seed(54); n = int(0.16 * RATE); out = []; lp = 0.0
    for i in range(n):
        t = i / RATE; s = 0.0
        for k in range(2):
            te = t - 0.004 - k * 0.065
            if te < 0: break
            s += env_ad(te, 0.003, 0.035) * tone(glide(330.0, 290.0, te, 0.05), te, 0.35)
        lp += lp_coef(1600.0) * (s - lp)
        out.append(lp)
    write("ui_deny2.wav", out, peak_db=-14.0, fade_in=0.002, fade_out=0.03)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    irisglow(); approach(); pupilenter(); interior(); lightsweep(); nikoappear(); signaltravel(); signalarrive(); layerreveal(); softwhoosh()
    ui_select2(); ui_tap2(); ui_confirm2(); ui_deny2()
    print("wrote", OUT)
