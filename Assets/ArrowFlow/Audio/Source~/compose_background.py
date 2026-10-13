"""Compose Arrow Attack's background loop: a soft, mellow 76 BPM track (8 bars, about 25 s, seamless).

Run from the project root:  python Assets/ArrowFlow/Audio/Source~/compose_background.py
Writes Assets/ArrowFlow/Audio/BackgroundLoop.wav (44.1 kHz, 16-bit mono). Needs numpy.
Warm pad, gentle electric-piano arpeggio, round sine bass and a quiet flute-like melody; no drums.
Every note is written modulo the loop length, so tails wrap to the start and the seam is silent.
"""
import os, wave
import numpy as np

RATE = 44100
BPM = 76
BEAT = 60 / BPM
BARS = 8
LENGTH = int(round(BARS * 4 * BEAT * RATE))
OUT = os.path.join("Assets", "ArrowFlow", "Audio", "BackgroundLoop.wav")
mix = np.zeros(LENGTH)


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def place(start_seconds, samples):
    """Add a sound at a time, wrapping its tail around the loop end."""
    start = int(round(start_seconds * RATE)) % LENGTH
    idx = (start + np.arange(len(samples))) % LENGTH
    np.add.at(mix, idx, samples)


def envelope(n, attack, decay, release=0.08):
    """Raised-cosine attack, exponential decay and a smooth release, so nothing clicks."""
    t = np.arange(n) / RATE
    k = max(1, int(attack * RATE))
    rise = np.ones(n)
    rise[:k] = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, min(k, n)))[:k]
    env = rise * np.exp(-t / decay)
    tail = min(n, int(release * RATE))
    env[-tail:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, tail))
    return env


def keys(freq, n):
    """Mellow electric-piano tone: mostly sine, a touch of octave that fades quickly."""
    t = np.arange(n) / RATE
    return np.sin(2 * np.pi * freq * t) + 0.12 * np.sin(4 * np.pi * freq * t) * np.exp(-t / 0.15)


def pad(freq, n):
    """Three slightly detuned sines for a slow, warm chorus."""
    t = np.arange(n) / RATE
    return sum(np.sin(2 * np.pi * freq * d * t + p) for d, p in ((1.0, 0.0), (1.0012, 1.3), (0.9988, 2.6))) / 3


def flute(freq, n):
    """Soft sine melody with a gentle 5 Hz vibrato."""
    t = np.arange(n) / RATE
    vibrato = 1 + 0.003 * np.sin(2 * np.pi * 5 * t) * np.minimum(1, t / 0.3)
    phase = 2 * np.pi * np.cumsum(freq * vibrato) / RATE
    return np.sin(phase) + 0.06 * np.sin(2 * phase)


def lowpass(x, cutoff):
    """One-pole low-pass on the loop, run twice around it so the seam carries the filter state."""
    a = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    state = 0.0
    for _ in range(2):
        for i, v in enumerate(x):
            state += a * (v - state)
            y[i] = state
    return y


# C | G | Am | F | C | G | F | G  — the last bar leads straight back into the first.
CHORDS = [
    (48, (60, 64, 67, 72)),
    (43, (59, 62, 67, 71)),
    (45, (60, 64, 69, 72)),
    (41, (60, 65, 69, 72)),
    (48, (60, 64, 67, 72)),
    (43, (59, 62, 67, 71)),
    (41, (60, 65, 69, 72)),
    (43, (59, 62, 67, 71)),
]
ARP = (0, 1, 2, 3, 2, 1, 2, 1)
# Quiet melody in the second half of the loop: (beat in bar, midi, beats long).
MELODY = {
    4: [(0, 79, 1), (1, 88, 1), (2, 86, 1), (3, 84, 1)],
    5: [(0, 83, 2), (2, 86, 1), (3, 79, 1)],
    6: [(0, 81, 1), (1, 84, 1), (2, 89, 1), (3, 88, 1)],
    7: [(0, 86, 2), (2, 83, 1), (3, 79, 1)],
}

for bar, (root, chord) in enumerate(CHORDS):
    bar_start = bar * 4 * BEAT
    # Warm pad holds the chord through the bar and fades into the next one.
    n = int((4 * BEAT + 1.2) * RATE)
    for note in chord[:3]:
        place(bar_start, 0.05 * pad(hz(note), n) * envelope(n, 0.6, 6.0, release=1.2))
    # Gentle electric-piano arpeggio on eighth notes.
    for step, index in enumerate(ARP):
        n = int(1.4 * RATE)
        level = 0.07 if step % 2 == 0 else 0.05
        place(bar_start + step * BEAT / 2, level * keys(hz(chord[index]), n) * envelope(n, 0.012, 0.45, release=0.3))
    # Round sine bass on beats 1 and 3, with a soft octave so phone speakers can carry it.
    for beat in (0, 2):
        n = int(2 * BEAT * RATE)
        t = np.arange(n) / RATE
        tone = np.sin(2 * np.pi * hz(root) * t) + 0.3 * np.sin(4 * np.pi * hz(root) * t)
        place(bar_start + beat * BEAT, 0.11 * tone * envelope(n, 0.02, 0.9, release=0.25))
    # Melody.
    for beat, note, beats in MELODY.get(bar, []):
        n = int((beats * BEAT + 0.4) * RATE)
        place(bar_start + beat * BEAT, 0.05 * flute(hz(note), n) * envelope(n, 0.06, 0.9 * beats, release=0.35))

# Take off any remaining brightness, then normalise to a calm level.
mix = lowpass(mix, 3500)
mix *= 0.5 / np.max(np.abs(mix))
rms = np.sqrt(np.mean(mix ** 2))
# Notes wrap around, so the seam must be no rougher than the rest of the track.
steps = np.abs(np.diff(mix))
assert abs(mix[0] - mix[-1]) <= np.percentile(steps, 99.9), "Loop seam discontinuity"
data = (np.clip(mix, -1, 1) * 32767).astype("<i2")
with wave.open(OUT, "wb") as f:
    f.setnchannels(1)
    f.setsampwidth(2)
    f.setframerate(RATE)
    f.writeframes(data.tobytes())
print(f"{OUT}: {LENGTH / RATE:.2f} s, peak {20 * np.log10(np.max(np.abs(mix))):.1f} dBFS, rms {20 * np.log10(rms):.1f} dBFS")
