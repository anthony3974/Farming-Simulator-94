#!/usr/bin/env python3
"""Makes MoneyOut.wav, a short, deep bell "dong" played when money is spent. Built from scratch, so no
copyright issues. It pairs with the bright desk-bell "ding-ding" of MoneyIn.wav (make_money_in.py).

Version 2: shorter and punchier than version 1, same pitch.

A big bell is tuned differently from the desk bell in make_money_in.py, so it uses church-bell partials, named and set as ratios of the prime (PRIME): hum 0.5, prime 1, tierce 1.2 (a minor third, the
bell's typical sad colour), quint 1.5, nominal 2. The hum rings longest and the high partials die first.
Like the desk bell, each partial is an uneven pair of sines a little apart, for a slow gentle wobble.
The strike is a duller, lower tick than the desk bell's, like a heavier clapper. It rings much shorter than a
real big bell (SHORT scales every decay) so it works as a game sound.
It is normalised to PEAK_DB and the end fades to zero.

Needs numpy. Run from the project folder:  python3 Art/Sounds/make_money_out.py
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sounds", "MoneyOut.wav")

SR = 44100
LENGTH = 0.55     # seconds
PEAK_DB = -8.0    # final peak level, same as the money sound
FADE_OUT = 0.18   # seconds faded to zero at the end
DRIVE = 1.7       # tanh saturation: squashes the first hit for punch
PRIME = 330.0     # Hz; the hum sits an octave lower at 165 Hz
SHORT = 0.6       # scales how long it rings (below 1 = shorter)

rng = np.random.default_rng(94)  # fixed seed: the same file every run
t = np.arange(int(SR * LENGTH)) / SR
out = np.zeros_like(t)


def band_noise(n, low, high):
    """White noise kept only between low and high Hz."""
    spec = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / SR)
    spec[(freqs < low) | (freqs > high)] = 0
    x = np.fft.irfft(spec, n)
    return x / np.abs(x).max()


# --- ring --------------------------------------------------------------------------------------
# name: ratio to the prime, level, decay time constant (s), split between the mode pair (Hz)
PARTIALS = [
    (0.5, 0.8, 0.30, 0.8),   # hum
    (1.0, 1.0, 0.22, 1.5),   # prime
    (1.2, 0.6, 0.16, 2.0),   # tierce
    (1.5, 0.35, 0.12, 2.5),  # quint
    (2.0, 0.7, 0.10, 3.5),   # nominal (louder: a brighter, harder hit)
    (2.6, 0.2, 0.05, 5.0),   # upper partial, gives the strike its metal edge
]
for ratio, level, tau, split in PARTIALS:
    f = PRIME * ratio
    for df, part in ((-split / 2, 0.65), (split / 2, 0.35)):  # uneven pair: a gentle wobble, not a full pulse
        phase = rng.uniform(0, 2 * np.pi)
        out += part * level * np.sin(2 * np.pi * (f + df) * t + phase) * np.exp(-t / (tau * SHORT))
out *= np.minimum(t / 0.0003, 1)  # instant attack for punch, but not a click
out /= np.abs(out).max()

# --- strike: clapper tick ----------------------------------------------------------------------
n = int(0.010 * SR)
out[:n] += 0.55 * band_noise(n, 800, 4000) * np.exp(-np.arange(n) / SR / 0.002)

# --- finish ------------------------------------------------------------------------------------
out = np.tanh(DRIVE * out / np.abs(out).max())
out *= 10 ** (PEAK_DB / 20) / np.abs(out).max()
f = int(FADE_OUT * SR)
out[-f:] *= np.cos(np.linspace(0, np.pi / 2, f)) ** 2
out[0] = 0.0

with wave.open(OUT, "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes((out * 32767).round().astype("<i2").tobytes())
print("wrote", OUT)
