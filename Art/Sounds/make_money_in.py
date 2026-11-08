#!/usr/bin/env python3
"""Makes MoneyIn.wav, the sound played when crops are sold. Built from scratch, so no copyright issues.

Version 17: a short desk bell (the "ding" bell on a shop counter), struck twice: "ding-ding". Each ding has two parts:
  1. strike: the plunger's short metal tick as the hammer hits the dome.
  2. ring: the dome's inharmonic partials (ratios 1, 2.76, 5.4, the modes of a hemispherical shell). The low
     partial rings longest and the high ones die away first, so the ding goes from bright to mellow. Every
     partial is a pair of sines a few Hz apart; a real dome isn't perfectly round, so its modes split and beat,
     which gives the slow wobble of a desk bell.
The first ding is a perfect fourth lower (FIRST_F0), so the pair rises like a reward, and
rings about half as long (FIRST_RING), so it reads as a quick lead-in to the second. The second strike comes
GAP seconds after the first, a little quieter. The hammer touching the dome damps the
first ring, so that ring drops when the second strike lands. It is normalised to PEAK_DB and the end fades to zero.

Versions 1 to 11 were a "ka-ching" with coins; see Assets/Sounds/AUDIO_NOTES.md for what they were and why
they changed.
Needs numpy. Run from the project folder:  python3 Art/Sounds/make_money_in.py
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sounds", "MoneyIn.wav")

SR = 44100
LENGTH = 1.05     # seconds
GAP = 0.27        # seconds between the two strikes
SECOND = 0.85     # second strike's level compared to the first
DAMP = 0.3        # how much of the first ring is left once the hammer hits again
PEAK_DB = -8.0    # final peak level
FADE_OUT = 0.2    # seconds faded to zero at the end
DRIVE = 1.2       # light tanh saturation, only really touches the strike
F0 = 1650.0       # dome's lowest partial, Hz (second ding)
FIRST_F0 = F0 / 2 ** (5 / 12)  # first ding a perfect fourth lower, ~1236 Hz
FIRST_RING = 0.5  # first ding's ring length compared to the second

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


def ding(n, f0, ring=1.0):
    """One strike of the bell, n samples long, peak about 1."""
    tt = np.arange(n) / SR
    sig = np.zeros(n)
    # ring: the dome. ratio, level, decay time constant (s), split between the mode pair (Hz)
    for ratio, level, tau, split in [(1.0, 1.0, 0.22, 2.5), (2.76, 0.5, 0.10, 6.0), (5.4, 0.22, 0.045, 10.0)]:
        f = f0 * ratio
        for df, part in ((-split / 2, 0.65), (split / 2, 0.35)):  # uneven pair: a gentle wobble, not a full pulse
            phase = rng.uniform(0, 2 * np.pi)
            sig += part * level * np.sin(2 * np.pi * (f + df) * tt + phase) * np.exp(-tt / (tau * ring))
    sig *= np.minimum(tt / 0.0003, 1)  # instant attack, but not a click
    sig /= np.abs(sig).max()
    # strike: hammer tick
    m = int(0.006 * SR)
    sig[:m] += 0.35 * band_noise(m, 2500, 9000) * np.exp(-np.arange(m) / SR / 0.0012)
    return sig


g = int(GAP * SR)
first = ding(len(out), FIRST_F0, FIRST_RING)
first[g:] *= DAMP + (1 - DAMP) * np.exp(-np.arange(len(out) - g) / SR / 0.004)  # hammer damps it over ~4 ms
out += first
out[g:] += SECOND * ding(len(out) - g, F0)

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
