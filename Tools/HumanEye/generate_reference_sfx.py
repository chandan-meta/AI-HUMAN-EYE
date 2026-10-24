"""Project-owned sounds for the reference cinematics (8 October 2026), same conventions as generate_cinematic_sfx.py
(pure Python, deterministic, 48 kHz 16-bit mono, DC-blocked, faded, peak-limited; its helpers are reused).
Keys (wired by Builder.Reference.cs -> ReferenceSfx): gatelift, labreveal, wipe, xray, extract, titlehit, vortex, brainrise.
Design: mechanical but soft for the gate; airy, warm and low-level for the reveals; nothing harsh, nothing sci-fi loud.
Run: python Tools/HumanEye/generate_reference_sfx.py"""
import math, os, random
from generate_cinematic_sfx import OUT, RATE, TAU, write, tone, glide, noise, env_ad, ramp, hann, lp_coef


def gatelift():
    """2.4 s: a motor hum rising 55 -> 85 Hz, a servo whine 380 -> 610 Hz with a slight wobble, hydraulic hiss (noise
    through a 3 kHz band) swelling and settling, and a soft end stop (70 Hz thump) at 2.05 s."""
    random.seed(61); n = int(2.4 * RATE); out = []; lp = bp_l = bp_h = 0.0; ph = 0.0
    for i in range(n):
        t = i / RATE
        f = glide(55.0, 85.0, t, 2.0); ph += TAU * f / RATE
        hum = (math.sin(ph) + .35 * math.sin(2 * ph) + .15 * math.sin(3 * ph)) * ramp(t, 0, .25) * (1 - ramp(t, 1.9, 2.1))
        whine = math.sin(TAU * glide(380.0, 610.0, t, 2.0) * t + .6 * math.sin(TAU * 5.5 * t)) * .22 * hann(t, 2.1)
        x = noise(); bp_l += lp_coef(4200.0) * (x - bp_l); bp_h += lp_coef(1800.0) * (bp_l - bp_h)
        hiss = (bp_l - bp_h) * .9 * hann(min(t, 2.0), 2.0)
        te = t - 2.05
        stop = env_ad(te, .004, .16) * tone(70.0, max(te, 0), .3) * 1.2 if te >= 0 else 0.0
        s = .55 * hum + whine + hiss + stop
        lp += lp_coef(6000.0) * (s - lp); out.append(lp)
    write("fx_gatelift.wav", out, peak_db=-6.0)


def labreveal():
    """3.5 s airy swell as the lab appears: a warm detuned pad (A2 E3 A3 C#4 E4) swelling over 1.4 s and settling, with a
    breath of high air and a few soft glints."""
    random.seed(62); n = int(3.5 * RATE); out = []; air = air2 = 0.0
    chord = [110.0, 164.8, 220.0, 277.2, 329.6]
    glints = [(random.uniform(.6, 2.6), random.choice([1760.0, 2217.0, 2637.0, 3520.0])) for _ in range(6)]
    for i in range(n):
        t = i / RATE
        pad = sum(math.sin(TAU * f * t) + math.sin(TAU * f * 1.004 * t + .7) for f in chord) / (2 * len(chord))
        env = ramp(t, 0, 1.4) ** 1.5 * (1 - ramp(t, 2.0, 3.5)) ** 1.2
        x = noise(); air += lp_coef(7000.0) * (x - air); air2 += lp_coef(2500.0) * (air - air2)
        sparkle = sum(env_ad(t - g, .005, .35) * math.sin(TAU * f * (t - g)) for g, f in glints if t >= g)
        out.append(.7 * pad * env + .18 * (air - air2) * env + .12 * sparkle)
    write("fx_labreveal.wav", out, peak_db=-8.0, rms_db=-22.0)


def wipe():
    """0.9 s soft descending shimmer for a layer wipe: five sines gliding 3.2 kHz -> 1.4 kHz with a 4 Hz flutter over a band
    of air; fast in, smooth out."""
    random.seed(63); n = int(0.9 * RATE); out = []; air = air2 = 0.0
    offs = [1.0, 1.26, 1.5, 1.68, 2.0]
    for i in range(n):
        t = i / RATE
        base = glide(1600.0, 700.0, t, .8)
        s = sum(math.sin(TAU * base * o * t) * (1.0 / (k + 1)) for k, o in enumerate(offs)) * (.8 + .2 * math.sin(TAU * 4 * t))
        x = noise(); air += lp_coef(8000.0) * (x - air); air2 += lp_coef(3000.0) * (air - air2)
        env = ramp(t, 0, .06) * (1 - ramp(t, .25, .9)) ** 1.6
        out.append((.35 * s + .5 * (air - air2)) * env)
    write("fx_wipe.wav", out, peak_db=-12.0)


def xray():
    """1.6 s glassy swell as bone turns to glass: inharmonic bell partials (1.2 / 1.87 / 2.73 / 3.9 kHz) with a slow 0.4 s
    attack over a soft 110 Hz pad."""
    random.seed(64); n = int(1.6 * RATE); out = []
    bells = [(1200.0, 1.0), (1870.0, .6), (2730.0, .4), (3900.0, .25)]
    for i in range(n):
        t = i / RATE
        glass = sum(a * math.sin(TAU * f * t + .3 * math.sin(TAU * 3 * t)) for f, a in bells)
        pad = tone(110.0, t, .3) * .5
        env = ramp(t, 0, .4) ** 2 * (1 - ramp(t, .5, 1.6)) ** 1.3
        out.append((.25 * glass + pad) * env)
    write("fx_xray.wav", out, peak_db=-11.0)


def riser(name, seed, f0, f1, cutoff1, dur, sparkle):
    """A rising whoosh (noise through a low-pass opening to cutoff1) with a tonal riser f0 -> f1 and, in the second half,
    soft high glints; peaks near the end, then lets go."""
    random.seed(seed); n = int(dur * RATE); out = []; lp = 0.0
    glints = [(random.uniform(dur * .45, dur * .9), random.uniform(2000.0, 4200.0)) for _ in range(sparkle)]
    for i in range(n):
        t = i / RATE; u = t / dur
        x = noise(); lp += lp_coef(400.0 + (cutoff1 - 400.0) * u ** 1.5) * (x - lp)
        rise = math.sin(TAU * glide(f0, f1, t, dur * .85) * t) * .4
        env = ramp(t, 0, dur * .75) ** 1.4 * (1 - ramp(t, dur * .8, dur))
        glint = sum(env_ad(t - g, .004, .25) * math.sin(TAU * f * (t - g)) for g, f in glints if t >= g)
        out.append((.6 * lp + rise) * env + .12 * glint)
    write(name, out, peak_db=-8.0)


def titlehit():
    """3.2 s title hit: a soft deep impact (45 Hz sub + 90 Hz body, 0.8 s decay) under a bright, slowly fading major chord
    (C5 E5 G5 C6) and a breath of air."""
    random.seed(65); n = int(3.2 * RATE); out = []; air = air2 = 0.0
    chord = [523.3, 659.3, 784.0, 1046.5]
    for i in range(n):
        t = i / RATE
        hit = env_ad(t, .006, .8) * (math.sin(TAU * 45.0 * t) + .6 * math.sin(TAU * 90.0 * t))
        shimmer = sum(math.sin(TAU * f * t) + .5 * math.sin(TAU * f * 1.003 * t) for f in chord) / 6 * env_ad(t, .02, 1.4)
        x = noise(); air += lp_coef(9000.0) * (x - air); air2 += lp_coef(4000.0) * (air - air2)
        out.append(.9 * hit + .35 * shimmer + .12 * (air - air2) * env_ad(t, .01, .5))
    write("fx_titlehit.wav", out, peak_db=-5.0)


def vortex():
    """2.6 s swirl Niko materialises from: air through a band sweeping 600 Hz -> 2.4 kHz with a tremolo speeding from 6 to
    12 Hz (the spin), a soft tone rising under it."""
    random.seed(66); n = int(2.6 * RATE); out = []; lo = hi = 0.0
    for i in range(n):
        t = i / RATE; u = t / 2.6
        x = noise(); centre = 600.0 * 4 ** u
        hi += lp_coef(centre * 1.6) * (x - hi); lo += lp_coef(centre * .6) * (hi - lo)
        spin = .6 + .4 * math.sin(TAU * (6 * t + 3 * t * u))
        rise = math.sin(TAU * glide(260.0, 520.0, t, 2.4) * t) * .25
        env = ramp(t, 0, 1.8) ** 1.3 * (1 - ramp(t, 2.2, 2.6))
        out.append(((hi - lo) * spin + rise) * env)
    write("fx_vortex.wav", out, peak_db=-9.0)


def crackle():
    """2.3 s electric breakup (14 October 2026): sparse electrical tickles (1 ms bursts at 2-6 kHz) thickening to a crackle
    with a fizz of high air, an airy digitisation glide rising 400 -> 1900 Hz, and a soft completion thump + glint at 1.75 s."""
    random.seed(64); n = int(2.3 * RATE); out = []; hp = hp2 = 0.0; ph = 0.0
    ticks = sorted(random.uniform(.0, 1.7) for _ in range(70))
    tick_f = [random.choice([2200.0, 3100.0, 4300.0, 5600.0]) for _ in ticks]
    for i in range(n):
        t = i / RATE
        dens = ramp(t, 0, .9) * (1 - ramp(t, 1.5, 1.9))
        s = sum(env_ad(t - g, .0008, .007) * math.sin(TAU * tick_f[k] * (t - g)) for k, g in enumerate(ticks) if 0 <= t - g < .03) * (.5 + .5 * dens)
        x = noise(); hp += lp_coef(6500.0) * (x - hp); hp2 += lp_coef(2800.0) * (hp - hp2)
        fizz = (hp - hp2) * .5 * dens * (.6 + .4 * math.sin(TAU * 31 * t))
        ph += TAU * glide(400.0, 1900.0, t, 1.7) / RATE
        rise = (math.sin(ph) + .3 * math.sin(2 * ph)) * .16 * hann(t, 1.8)
        te = t - 1.75
        hit = (env_ad(te, .005, .12) * tone(90.0, te, .3) * 1.1 + env_ad(te, .003, .25) * math.sin(TAU * 3520.0 * te) * .35) if te >= 0 else 0.0
        out.append(.8 * s + fizz + rise + hit)
    write("fx_crackle.wav", out, peak_db=-10.0)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    gatelift(); labreveal(); wipe(); xray(); titlehit(); vortex(); crackle()
    riser("fx_extract.wav", 67, 180.0, 420.0, 6000.0, 2.4, 8)
    riser("fx_brainrise.wav", 68, 120.0, 300.0, 4200.0, 2.6, 5)
    print("wrote", OUT)
