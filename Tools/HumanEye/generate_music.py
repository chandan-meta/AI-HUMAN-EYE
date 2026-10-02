"""Generates the project-owned ambient loop Assets/HumanEye/Audio/Music/observatory_pad.wav.
A soft, seamless 32 s pad (every partial completes whole cycles over the loop). Pure Python, deterministic."""
import math, os, random, struct, wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RATE, SECONDS = 32000, 32
N = RATE * SECONDS
# A2 E3 A3 C#4 E4 B4 rounded so each completes an integer number of cycles in the loop
notes = [110.0, 164.8, 220.0, 277.2, 329.6, 493.9]
freqs = [round(f * SECONDS) / SECONDS for f in notes]
gains = [.28, .2, .16, .1, .08, .035]
random.seed(5)
phase = [random.random() * 2 * math.pi for _ in freqs]
out = bytearray()
peak = 0.0
samples = []
for i in range(N):
    t = i / RATE
    s = 0.0
    for k, (f, g) in enumerate(zip(freqs, gains)):
        lfo = 0.6 + 0.4 * math.sin(2 * math.pi * t * (k + 1) / SECONDS + phase[k])
        s += g * lfo * (math.sin(2 * math.pi * f * t + phase[k]) + 0.18 * math.sin(4 * math.pi * f * t))
    samples.append(s)
    peak = max(peak, abs(s))
scale = 0.32 / peak
path = os.path.join(ROOT, "Assets", "HumanEye", "Audio", "Music", "observatory_pad.wav")
with wave.open(path, "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
    w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * scale)) * 32767)) for s in samples))
print("wrote", path)
