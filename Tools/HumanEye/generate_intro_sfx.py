"""Generates the project-owned intro SFX set in Assets/HumanEye/Audio/SFX/ ("dark observatory powers up").
48 kHz 16-bit mono WAVs. Pure Python (math/wave/struct/random), deterministic, no numpy: plain per-sample
loops with one-pole filters. Every file is DC-blocked (5 Hz one-pole), has >= 10 ms raised-cosine fades and
is normalised to a peak ceiling of -1 dBFS or lower. Run: python Tools/HumanEye/generate_intro_sfx.py"""
import math, os, random, struct, wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "HumanEye", "Audio", "SFX")
RATE = 48000
TAU = 2 * math.pi


def db(x):  # dB -> linear gain
    return 10 ** (x / 20)


def lp_coef(hz):  # one-pole low-pass coefficient for y += a * (x - y)
    return 1 - math.exp(-TAU * hz / RATE)


def glide(f0, f1, t, dur):  # exponential sweep f0 -> f1 over dur seconds, then holds f1
    return f0 * (f1 / f0) ** min(max(t / dur, 0.0), 1.0)


def noise():
    return random.random() * 2 - 1


def write(name, samples, peak_db=-3.0, rms_db=None, fade_in=0.01, fade_out=0.01):
    """DC-block, fade (raised cosine), normalise to an RMS target if given (never above peak_db peak) and write."""
    r = 1 - TAU * 5 / RATE  # 5 Hz DC blocker: y = x - x1 + r * y1
    x1 = y1 = 0.0
    for i, x in enumerate(samples):
        y1 = x - x1 + r * y1; x1 = x
        samples[i] = y1
    n, ni, no = len(samples), int(fade_in * RATE), int(fade_out * RATE)
    for i in range(ni):
        samples[i] *= 0.5 - 0.5 * math.cos(math.pi * i / ni)
    for i in range(no):
        samples[n - 1 - i] *= 0.5 - 0.5 * math.cos(math.pi * i / no)
    if any(math.isnan(s) or math.isinf(s) for s in samples):
        raise ValueError(name + ": NaN/inf in synthesis")
    peak = max(abs(s) for s in samples)
    rms = math.sqrt(sum(s * s for s in samples) / n)
    scale = db(peak_db) / peak
    if rms_db is not None:
        scale = min(scale, db(rms_db) / rms)
    ints = [max(-32767, min(32767, int(round(s * scale * 32767)))) for s in samples]
    path = os.path.join(OUT, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(struct.pack("<%dh" % n, *ints))
    pk = max(abs(v) for v in ints) / 32767
    rm = math.sqrt(sum(v * v for v in ints) / n) / 32767
    print("%-22s %5.2f s  peak %6.2f dBFS  rms %6.2f dBFS" % (name, n / RATE, 20 * math.log10(pk), 20 * math.log10(rm)))


def drone():
    """12 s low evolving drone: two detuned low oscillators (~57-58 Hz, slowly wandering) + soft filtered noise."""
    random.seed(1)
    n, out = 12 * RATE, []
    pa = pb = lp1 = lp2 = lp3 = 0.0
    for i in range(n):
        t = i / RATE
        drift = 0.15 * math.sin(TAU * 0.05 * t)  # +-0.15 Hz wander -> beat rate moves between 0.05 and 0.65 Hz
        pa += TAU * (57.0 + drift) / RATE
        pb += TAU * (57.35 - drift) / RATE
        osc = (math.sin(pa) + 0.35 * math.sin(2 * pa) + 0.12 * math.sin(3 * pa)
               + 0.65 * (math.sin(pb) + 0.30 * math.sin(2 * pb + 1.0))) * 0.6  # unequal pair: beats never null fully
        a = lp_coef(475 + 225 * math.sin(TAU * 0.1 * t + 2.0))  # noise bed breathing 250..700 Hz, 3 poles (18 dB/oct)
        lp1 += a * (noise() - lp1); lp2 += a * (lp1 - lp2); lp3 += a * (lp2 - lp3)
        move = 0.75 + 0.25 * math.sin(TAU * 0.2 * t - math.pi / 2)  # 0.2 Hz amplitude movement
        out.append(move * (osc + 1.6 * lp3))
    write("intro_drone.wav", out, peak_db=-1.0, rms_db=-22.0, fade_in=1.5, fade_out=1.5)


def riser():
    """3.5 s rising sweep: tone 80 -> 900 Hz (exponential) + band-limited noise opening up, peak at 3.44 s, 60 ms tail."""
    random.seed(2)
    dur, tail = 3.5, 0.06
    rise = dur - tail
    n, out = int(dur * RATE), []
    ph = tph = lp = hp = 0.0
    for i in range(n):
        t = i / RATE
        u = min(t / rise, 1.0)
        ph += TAU * glide(80.0, 900.0, t, rise) / RATE
        tone = math.sin(ph) + 0.4 * math.sin(2 * ph) + 0.15 * math.sin(3 * ph)
        fc = glide(400.0, 5000.0, t, rise)
        lp += lp_coef(fc) * (noise() - lp)
        hp += lp_coef(fc / 4) * (lp - hp)
        band = lp - hp
        tph += TAU * (3 + 13 * u * u) / RATE  # flutter accelerating 3 -> 16 Hz
        trem = 1 - 0.25 * u * (0.5 + 0.5 * math.sin(tph))
        env = db(-42 * (1 - u)) if t < rise else max(0.0, 1 - (t - rise) / tail)
        out.append(env * trem * (0.8 * tone + 1.6 * band * (0.3 + 0.7 * u)))
    write("intro_riser.wav", out, peak_db=-1.0, fade_in=0.02, fade_out=0.03)


def boom():
    """2.5 s impact: 1 ms click + sine thump sliding 120 -> 40 Hz over 0.4 s (tau 0.3 s) + low noise tail."""
    random.seed(3)
    n, out, t0 = int(2.5 * RATE), [], 0.012  # onset after the 10 ms fade-in so the transient stays hard
    ph = lp1 = lp2 = lp3 = 0.0
    a = lp_coef(160)
    for i in range(n):
        te = i / RATE - t0
        x = noise()
        lp1 += a * (x - lp1); lp2 += a * (lp1 - lp2); lp3 += a * (lp2 - lp3)  # dark 3-pole noise tail
        if te < 0:
            out.append(0.0); continue
        ph += TAU * glide(120.0, 40.0, te, 0.4) / RATE
        thump = (math.sin(ph) + 0.3 * math.sin(2 * ph) * math.exp(-te / 0.12)) * math.exp(-te / 0.3)
        click = 0.5 * x * math.exp(-te / 0.0012) if te < 0.01 else 0.0
        out.append(thump + click + 3.5 * lp3 * math.exp(-te / 0.5))
    write("intro_boom.wav", out, peak_db=-1.0, fade_in=0.01, fade_out=0.05)


def clunk(name, base, seed):
    """0.45 s mechanical relay clunk: hard tick + inharmonic resonant body at `base` Hz, a softer bounce, a touch of noise."""
    random.seed(seed)
    n, out = int(0.45 * RATE), []
    hits = ((0.012, 1.0), (0.012 + 0.055 + random.uniform(-0.01, 0.01), 0.45))  # (onset, gain)
    lp = 0.0
    a = lp_coef(1500)
    for i in range(n):
        t = i / RATE
        x = noise()
        lp += a * (x - lp)
        s = 0.0
        for tk, g in hits:
            te = t - tk
            if te < 0:
                continue
            s += g * (0.9 * x * math.exp(-te / 0.0015)
                      + math.sin(TAU * base * te) * math.exp(-te / 0.06)
                      + 0.5 * math.sin(TAU * base * 2.76 * te) * math.exp(-te / 0.025)
                      + 0.3 * math.sin(TAU * base * 5.4 * te) * math.exp(-te / 0.012)
                      + 0.6 * lp * math.exp(-te / 0.03))
        out.append(s)
    write(name, out, peak_db=-3.0, fade_in=0.01, fade_out=0.02)


def shimmer():
    """1.5 s soft hologram shimmer: 9 high sines (1.2-4 kHz), each doubled with a 2-5 Hz detune (chorus), random flicker."""
    random.seed(4)
    n, out = int(1.5 * RATE), []
    freqs = [1200, 1500, 1800, 2000, 2400, 2700, 3000, 3600, 4000]
    det = [random.uniform(2.0, 5.0) for _ in freqs]
    ph1 = [random.random() * TAU for _ in freqs]
    ph2 = [random.random() * TAU for _ in freqs]
    gain = [(1200.0 / f) ** 0.7 for f in freqs]
    amp = [random.random() for _ in freqs]
    target = list(amp)
    a, nxt = lp_coef(4), 0
    for i in range(n):
        t = i / RATE
        if i >= nxt:  # new flicker targets every 60-120 ms, smoothed by a 4 Hz one-pole
            target = [random.random() ** 1.5 for _ in freqs]
            nxt = i + random.randint(int(0.06 * RATE), int(0.12 * RATE))
        s = 0.0
        for k in range(len(freqs)):
            amp[k] += a * (target[k] - amp[k])
            s += gain[k] * amp[k] * (math.sin(TAU * freqs[k] * t + ph1[k]) + math.sin(TAU * (freqs[k] + det[k]) * t + ph2[k]))
        out.append(s * (1 - math.exp(-t / 0.05)) * math.exp(-t / 0.75))
    write("intro_shimmer.wav", out, peak_db=-8.0, fade_in=0.02, fade_out=0.25)


def materialise():
    """1.2 s digital materialise: 5-note sine arpeggio A4 C#5 E5 A5 C#6 (0.18 s apart, soft decay) + rising noise swell."""
    random.seed(5)
    n, out, t0 = int(1.2 * RATE), [], 0.012
    notes = [440.0, 554.37, 659.26, 880.0, 1108.73]
    lp1 = lp2 = 0.0
    for i in range(n):
        t = i / RATE
        s = 0.0
        for k, f in enumerate(notes):
            te = t - (t0 + k * 0.18)
            if te < 0:
                break
            env = (1 - math.exp(-te / 0.004)) * math.exp(-te / 0.16)
            s += env * (math.sin(TAU * f * te) + 0.5 * math.sin(TAU * f * 1.003 * te + 1.0)
                        + 0.25 * math.sin(TAU * 2 * f * te) * math.exp(-te / 0.06))
        a = lp_coef(glide(600.0, 6000.0, t, 0.9))
        lp1 += a * (noise() - lp1); lp2 += a * (lp1 - lp2)
        swell = (t / 0.9) ** 2 if t < 0.9 else max(0.0, 1 - (t - 0.9) / 0.3)
        out.append(0.6 * s + 0.35 * lp2 * swell)
    write("intro_materialise.wav", out, peak_db=-4.0, fade_in=0.01, fade_out=0.1)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    drone(); riser(); boom()
    clunk("intro_clunk_1.wav", 180.0, 31); clunk("intro_clunk_2.wav", 150.0, 32); clunk("intro_clunk_3.wav", 210.0, 33)
    shimmer(); materialise()
    print("wrote", OUT)
