"""Compose Arrow Attack's soft move sounds.

Run from the project root:  python Assets/ArrowFlow/Audio/Source~/compose_effects.py
Writes Assets/ArrowFlow/Audio/CorrectMove.wav, WrongMove.wav, LevelComplete.wav and LevelFailed.wav
(44.1 kHz, 16-bit mono).
Needs numpy. All are rounded sine tones with gentle fade-ins and no harsh highs, so they stay soft.
"""
import os, wave
import numpy as np

RATE = 44100
OUT = os.path.join("Assets", "ArrowFlow", "Audio")


def fade_in(n, seconds):
    """Raised-cosine fade-in: no click, no hard edge."""
    k = min(n, int(seconds * RATE))
    env = np.ones(n)
    env[:k] = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, k))
    return env


def fade_out(x, seconds):
    k = min(len(x), int(seconds * RATE))
    x[-k:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, k))
    return x


def glide(start_hz, end_hz, glide_seconds, n):
    """Instantaneous phase for a short exponential pitch bend that then holds."""
    t = np.arange(n) / RATE
    ratio = np.minimum(t / glide_seconds, 1.0)
    freq = start_hz * (end_hz / start_hz) ** ratio
    return 2 * np.pi * np.cumsum(freq) / RATE


def lowpass(x, cutoff):
    a = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    state = 0.0
    for i, v in enumerate(x):
        state += a * (v - state)
        y[i] = state
    return y


def write(name, x, peak_db):
    x = x / np.max(np.abs(x)) * 10 ** (peak_db / 20)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(data.tobytes())
    print(f"{path}: {len(x) / RATE:.2f} s, peak {peak_db} dBFS")


# Correct move: a short, rounded "bloop" that bends gently upward (A4 to D5), like a soft bubble.
n = int(0.24 * RATE)
t = np.arange(n) / RATE
phase = glide(440, 587.3, 0.06, n)
tone = np.sin(phase) + 0.18 * np.sin(2 * phase) * np.exp(-t / 0.03)
correct = tone * fade_in(n, 0.012) * np.exp(-t / 0.075)
correct = fade_out(lowpass(correct, 2500), 0.03)
write("CorrectMove", correct, -14)

# Wrong move: two low, muted notes stepping down (E4 then B3), plucked rather than buzzing.
n = int(0.46 * RATE)
wrong = np.zeros(n)
for start, hz in ((0.0, 329.6), (0.15, 246.9)):
    k = n - int(start * RATE)
    t = np.arange(k) / RATE
    # A soft second harmonic keeps the low notes audible on phone speakers.
    note = np.sin(2 * np.pi * hz * t) + 0.35 * np.sin(4 * np.pi * hz * t) * np.exp(-t / 0.08)
    wrong[n - k:] += note * fade_in(k, 0.015) * np.exp(-t / 0.11)
wrong = fade_out(lowpass(wrong, 1800), 0.05)
write("WrongMove", wrong, -12)

# Level complete: three bell-like "dings" rising (E5, G5, C6) in time with the three stars popping in
# 0.22 s apart, then a warm C major chord that blooms and fades.
def ding(hz, n, decay):
    t = np.arange(n) / RATE
    tone = (np.sin(2 * np.pi * hz * t)
            + 0.30 * np.sin(4 * np.pi * hz * t) * np.exp(-t / 0.09)
            + 0.08 * np.sin(6 * np.pi * hz * t) * np.exp(-t / 0.04))
    return tone * fade_in(n, 0.005) * np.exp(-t / decay)


n = int(1.9 * RATE)
win = np.zeros(n)
for start, hz in ((0.0, 659.3), (0.22, 784.0), (0.44, 1046.5)):
    k = int(0.9 * RATE)
    i = int(start * RATE)
    win[i:i + k] += 0.8 * ding(hz, k, 0.28)
chord_start = int(0.66 * RATE)
k = n - chord_start
t = np.arange(k) / RATE
bloom = np.minimum(1, t / 0.05)
for hz, level in ((523.3, 0.5), (659.3, 0.42), (784.0, 0.38), (1046.5, 0.3)):
    win[chord_start:] += level * ding(hz, k, 0.55) * bloom
win = fade_out(lowpass(win, 4500), 0.25)
write("LevelComplete", win, -10)

# Level failed: a gentle "you lose" phrase. Three soft notes fall through a minor chord (G4, E-flat4, C4)
# in time with the three gray stars, then a low B3 sighs down to B-flat3 with a slight wobble.
def soft(hz_start, hz_end, n, decay, bend_seconds=0.0, vibrato=0.0):
    t = np.arange(n) / RATE
    ratio = np.minimum(t / bend_seconds, 1.0) if bend_seconds > 0 else np.zeros(n)
    freq = hz_start * (hz_end / hz_start) ** ratio
    freq = freq * (1 + vibrato * np.sin(2 * np.pi * 5.5 * t) * np.minimum(1, t / 0.25))
    phase = 2 * np.pi * np.cumsum(freq) / RATE
    tone = np.sin(phase) + 0.25 * np.sin(2 * phase) * np.exp(-t / 0.12)
    return tone * fade_in(n, 0.02) * np.exp(-t / decay)


n = int(1.9 * RATE)
lose = np.zeros(n)
for start, hz in ((0.0, 392.0), (0.22, 311.1), (0.44, 261.6)):
    k = int(0.7 * RATE)
    i = int(start * RATE)
    lose[i:i + k] += 0.7 * soft(hz, hz, k, 0.22)
last = int(0.66 * RATE)
lose[last:] += 0.9 * soft(246.9, 233.1, n - last, 0.6, bend_seconds=0.5, vibrato=0.006)
lose = fade_out(lowpass(lose, 2000), 0.3)
write("LevelFailed", lose, -12)
