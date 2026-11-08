#!/usr/bin/env python3
"""Makes the error sound: Assets/Sounds/ErrorKnock.wav. Built from scratch, so no copyright issues.

A deep, punchy knock on a hollow wooden box, 0.8 s, for when something can't be done (PLAN, error messages). No pure
tones, pitch sweep or clipping, because an earlier tone version was "too electric" (owner, 2026-11-08).

Needs numpy. Run from the project folder:  python3 Art/Sounds/make_error.py
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sounds", "ErrorKnock.wav")

SR = 44100

# A hard mallet strikes the box: the hit rings a few resonances that are not multiples of each other, as a real object's
# are, and the higher ones die first. All of them start together on the hit and a short crack of noise sits on top, so
# it lands as one blow. Hz, how fast it dies (s), level.
# Version 1 (softer, 0.6 s): 6 ms noise mallet, random start phases, no crack, modes (128, 0.130, 1.00), (197, 0.085, 0.75),
# (289, 0.055, 0.55), (431, 0.032, 0.35), (640, 0.018, 0.18), peak -5.
# Version 2 (punchy, 0.4 s): 2 ms mallet, crack 0.30, fade 0.08, modes (122, 0.075, 0.55), (197, 0.055, 0.85), (289, 0.040, 0.90),
# (431, 0.024, 0.70), (640, 0.013, 0.40), peak -4.
MODES = [(82, 0.220, 0.50), (131, 0.150, 0.70), (197, 0.100, 0.90), (296, 0.060, 0.90), (445, 0.030, 0.60)]
LENGTH = 0.80
PEAK_DB = -5.0
FADE = 0.15
CRACK = 0.20   # level of the crack, as a part of the body's peak


def env(t, attack, tau):
    """Attack (s) to full, then an exponential fall with time constant tau (s)."""
    return np.minimum(t / attack, 1) * np.exp(-np.maximum(t - attack, 0) / tau)


def band_noise(rng, n, low, high):
    """White noise kept only between low and high Hz, peak 1."""
    spec = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / SR)
    spec[(freqs < low) | (freqs > high)] = 0
    x = np.fft.irfft(spec, n)
    return x / np.abs(x).max()


def main():
    rng = np.random.default_rng(95)  # fixed seed: the same file every run
    n = int(LENGTH * SR)
    t = np.arange(n) / SR

    # the mallet: one 3 ms push, the same way all through, which is what lines the resonances up into a single hit
    mallet = np.hanning(int(0.003 * SR))

    x = np.zeros(n)
    for hz, tau, level in MODES:
        ring = np.sin(2 * np.pi * hz * t) * np.exp(-t / tau)
        x += level * np.convolve(mallet, ring)[:n]
    x /= np.abs(x).max()
    x += CRACK * band_noise(rng, n, 500, 3500) * env(t, 0.0004, 0.007)

    fade = int(FADE * SR)
    x[-fade:] *= np.linspace(1, 0, fade)
    x -= np.linspace(x[0], 0, n)  # starts at exactly zero
    x *= 10 ** (PEAK_DB / 20) / np.abs(x).max()

    with wave.open(OUT, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes((x * 32767).astype(np.int16).tobytes())
    print("wrote", OUT)


if __name__ == "__main__":
    main()
