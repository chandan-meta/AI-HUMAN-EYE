"""Original 32-second educational intro score; deterministic, standard-library synthesis.

Room tone 0-3s, warm pad 3-7s, soft pulse 7-12s, melody 12-20s,
gentle lift 20-28s and a four-second tail. No recordings or external music.
Run: python Tools/HumanEye/generate_intro_music.py
"""
from array import array
import math
from pathlib import Path
import random
import sys
import wave

RATE, SECONDS = 32000, 32
TAU = 2 * math.pi
COUNT = RATE * SECONDS
OUT = Path(__file__).resolve().parents[2] / "Assets/HumanEye/Audio/Music/intro_discovery.wav"


def ramp(t, length):
    return .5 - .5 * math.cos(math.pi * min(1, max(0, t / length)))


def frequency(midi):
    return 440 * 2 ** ((midi - 69) / 12)


def add_pad(left, right, start, duration, notes, gain):
    for voice, note in enumerate(notes):
        hz = frequency(note)
        phase = voice * 1.71
        pan = .25 + .5 * (voice % 3) / 2
        for i in range(int(start * RATE), min(COUNT, int((start + duration) * RATE))):
            t = i / RATE - start
            env = ramp(t, 2) * ramp(duration - t, 2.6)
            breath = .86 + .14 * math.sin(TAU * .13 * t + phase)
            a = TAU * hz * t
            # Slightly detuned sine partials give the pad width without a buzzy saw wave.
            sample = gain * env * breath * (math.sin(a + phase)
                + .45 * math.sin(a * 1.0018 - phase) + .13 * math.sin(2 * a + phase))
            left[i] += sample * math.sqrt(1 - pan)
            right[i] += sample * math.sqrt(pan)


def add_note(left, right, start, note, gain, duration=3, pan=.5):
    hz = frequency(note)
    for i in range(int(start * RATE), min(COUNT, int((start + duration) * RATE))):
        t = i / RATE - start
        env = ramp(t, .055) * math.exp(-t / (duration * .32)) * ramp(duration - t, .5)
        phase = TAU * hz * t
        sample = gain * env * (math.sin(phase)
            + .24 * math.sin(phase * 2.002) * math.exp(-t / .45)
            + .065 * math.sin(phase * 3.006) * math.exp(-t / .18))
        left[i] += sample * math.sqrt(1 - pan)
        right[i] += sample * math.sqrt(pan)


def render():
    left, right = array("f", [0]) * COUNT, array("f", [0]) * COUNT
    rng = random.Random(131026)
    air = 0
    for i in range(COUNT):
        t = i / RATE
        air += .021 * (rng.uniform(-1, 1) - air)
        room = ramp(t, 2) * (.004 * math.sin(TAU * 55 * t)
            + .002 * math.sin(TAU * 110.06 * t) + .017 * air)
        left[i] = right[i] = room

    for start, duration, notes in (
        (3, 12, (45, 52, 57, 59, 61, 64)),
        (12, 11, (42, 49, 52, 57, 61, 64)),
        (20, 9, (50, 57, 61, 64, 66, 69)),
        (26, 6, (45, 52, 57, 59, 61, 64)),
    ):
        add_pad(left, right, start, duration, notes, .017)

    pulse = (57, 64, 59, 64)
    for beat in range(16):
        add_note(left, right, 7 + beat * 1.3333, pulse[beat % 4], .018, 1.5, .4 + .2 * (beat % 2))
    for start, note, pan in (
        (12, 64, .4), (14, 71, .6), (16, 73, .45), (18.5, 76, .55),
        (21, 78, .4), (23.5, 76, .6), (26, 73, .45), (28, 71, .55), (29, 69, .5),
    ):
        add_note(left, right, start, note, .048, 3.8, pan)
    add_pad(left, right, 20, 10, (69, 73, 76), .008)

    # Quiet stereo early reflections; read the dry mix so tails cannot feed back.
    dry_left, dry_right = left[:], right[:]
    for delay, amount in ((.193, .14), (.317, .10), (.537, .065), (.797, .04)):
        offset = int(delay * RATE)
        for i in range(offset, COUNT):
            left[i] += dry_right[i - offset] * amount
            right[i] += dry_left[i - offset] * amount

    dc_factor = math.exp(-TAU * 12 / RATE)
    for channel in (left, right):
        prior = filtered = 0
        for i, value in enumerate(channel):
            filtered = value - prior + dc_factor * filtered
            prior = value
            channel[i] = filtered * ramp(i / RATE, .1) * ramp((COUNT - 1 - i) / RATE, 4)

    peak = max(max(map(abs, left)), max(map(abs, right)))
    rms = math.sqrt((sum(v * v for v in left) + sum(v * v for v in right)) / (COUNT * 2))
    gain = min(10 ** (-10 / 20) / peak, 10 ** (-23 / 20) / rms)
    pcm = array("h", (round(v * gain * 32767) for pair in zip(left, right) for v in pair))
    assert len(pcm) == 2 * COUNT and max(map(abs, pcm)) < 32767, "Invalid length or clipped score"
    assert pcm[0] == pcm[1] == pcm[-1] == pcm[-2] == 0, "Score must fade cleanly at both ends"
    for start, end in ((0, 3), (3, 7), (7, 12), (12, 20), (20, 28), (28, 32)):
        segment = pcm[2 * start * RATE:2 * end * RATE]
        power = sum(v * v for v in segment) / len(segment)
        assert power > 1, "Silent musical section"
        print(f"{start:2}-{end:2}s RMS {20 * math.log10(math.sqrt(power) / 32767):6.2f} dBFS")
    print(f"32.00s stereo {RATE} Hz; peak {20 * math.log10(peak * gain):.2f} dBFS; RMS {20 * math.log10(rms * gain):.2f} dBFS")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUT), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())
    print(OUT)


if __name__ == "__main__":
    render()
