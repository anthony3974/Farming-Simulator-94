#!/usr/bin/env python3
"""Makes the three farming one-shots: Planting.wav, Harvesting.wav, Fertilizing.wav. Built from scratch, so no
copyright issues. One-shots for now (owner, 2026-11-08: "no looping, one shot for now").

All three are shaped noise (no tones, so nothing sounds like a bell or a game "bloop"):
  Planting:    (version 4) a shovel digging in: the steel blade slides into the soil with a long, rough scrape
               (noise through the blade's few ringing frequencies, so it sounds like metal and not just hiss)
               and a tiny dull "chk", then the dirt crumbles off it: hundreds of tiny dull grain crunches
               over a soft low soil bed, kept below ~2.8 kHz so the dirt itself stays earthy.
  Harvesting:  a "swish" whose brightness rises as the blade swings, with a burst of short crackles in the middle
               (stalks snapping), then a dry rustle.
  Fertilizing: a short "pff" as the spreader opens, then a bright spray hiss with pellets pattering through it.
Each is normalised to PEAK_DB, starts at zero and fades to zero at the end.

Planting is version 4 (v2: "more like dirt, no shovel noise"; v3: shovel noise wanted after all; v4: v3 "sounds
the same", so the scrape is longer, rougher and louder); the others are version 1. Needs numpy. Run from the project folder:  python3 Art/Sounds/make_farm_sounds.py
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sounds")

SR = 44100
PEAK_DB = -8.0    # same as the money sounds
FADE_OUT = 0.06   # seconds faded to zero at the end of each

rng = np.random.default_rng(94)  # fixed seeds: the same files every run (each sound sets its own below, so
                                 # changing one sound never changes the others)


def band_noise(n, low, high):
    """White noise kept only between low and high Hz, peak 1."""
    spec = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / SR)
    spec[(freqs < low) | (freqs > high)] = 0
    x = np.fft.irfft(spec, n)
    return x / np.abs(x).max()


def swept_band(n, centre, q=1.2):
    """Noise through a band-pass filter whose centre (Hz, one value per sample) moves over time. Peak 1."""
    x = rng.standard_normal(n)
    out = np.zeros(n)
    low = band = 0.0
    damp = 1 / q
    for i in range(n):  # Chamberlin state-variable filter
        f = 2 * np.sin(np.pi * min(centre[i], SR / 6) / SR)
        low += f * band
        high = x[i] - low - damp * band
        band += f * high
        out[i] = band
    return out / np.abs(out).max()


def env(n, attack, tau):
    """Attack (s) to full, then an exponential fall with time constant tau (s)."""
    t = np.arange(n) / SR
    return np.minimum(t / attack, 1) * np.exp(-np.maximum(t - attack, 0) / tau)


def ticks(out, count, start, end, low, high, gain, length=0.002, falloff=1.0):
    """count tiny noise clicks at random times between start and end (s); each later one quieter by falloff."""
    times = np.sort(rng.uniform(start, end, count))
    m = int(length * SR)
    shape = np.exp(-np.arange(m) / SR / (length / 3))
    g = gain
    for when in times:
        i = int(when * SR)
        if i + m > len(out):
            continue
        out[i:i + m] += g * rng.uniform(0.5, 1.0) * band_noise(m, low, high) * shape
        g *= falloff


def lowpass(x, cutoff):
    """Removes everything above cutoff Hz."""
    spec = np.fft.rfft(x)
    spec[np.fft.rfftfreq(len(x), 1 / SR) > cutoff] = 0
    return np.fft.irfft(spec, len(x))


def grains(out, count, start, end, low, high, gain):
    """count tiny dull crunches (1-4 ms) at random times, crowded near start and thinning out towards end,
    each at a random place in low..high Hz and a random loudness: the sound of soil grains moving."""
    times = start + (end - start) * rng.uniform(0, 1, count) ** 2  # squared: more near the start
    for when in times:
        m = int(rng.uniform(0.001, 0.004) * SR)
        i = int(when * SR)
        if i + m > len(out):
            continue
        centre = rng.uniform(low, high)
        g = gain * rng.lognormal(0, 0.6) * (1 - 0.7 * (when - start) / (end - start))
        grain = band_noise(1024, centre * 0.7, centre * 1.4)[:m]  # filter a longer piece: a few ms is too short
        out[i:i + m] += g * grain / np.abs(grain).max() * np.hanning(m)


def finish(out, name):
    out = out / np.abs(out).max()
    out = np.tanh(1.3 * out)
    out *= 10 ** (PEAK_DB / 20) / np.abs(out).max()
    f = int(FADE_OUT * SR)
    out[-f:] *= np.cos(np.linspace(0, np.pi / 2, f)) ** 2
    out[0] = 0.0
    path = os.path.join(OUT, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((out * 32767).round().astype("<i2").tobytes())
    print("wrote", path)


# --- planting: dirt crunch and crumble ---------------------------------------------------------
rng = np.random.default_rng(1)
n = int(0.55 * SR)
t = np.arange(n) / SR
BLADE = 0.15  # s: the scrape lasts this long; the dirt starts as the blade stops
bed = band_noise(n, 120, 900) * env(n, 0.02, 0.09)          # soft low push of the soil
dirt = 0.5 * bed
grains(dirt, 320, 0.01, 0.34, 300, 2200, 0.35)              # the crunch and crumble
dirt = lowpass(dirt, 2800)                                   # dull: dirt has no sparkle
out = np.zeros(n)
out[int(BLADE * SR):] = dirt[:n - int(BLADE * SR)]
# the blade: a scrape through the steel's ringing frequencies, rising in then cut off as it stops in the soil
scrape_env = np.clip(t / BLADE, 0, 1) ** 1.5 * np.exp(-np.maximum(t - BLADE, 0) / 0.02)
grit = lowpass(rng.standard_normal(n), 60)                  # the blade catching on little stones
scrape_env *= 0.55 + 0.45 * np.abs(grit) / np.abs(grit).max()
blade = np.zeros(n)
for f, level in ((1850, 1.0), (3100, 0.7), (4650, 0.4)):
    blade += level * band_noise(n, f * 0.96, f * 1.04)  # narrow bands: metal, not hiss
blade = blade / np.abs(blade).max() * scrape_env
chk = band_noise(n, 300, 1500) * env(n, 0.001, 0.012)        # tiny dull bite as the edge hits the soil
out += 1.6 * blade
out[int((BLADE - 0.005) * SR):] += 0.5 * chk[:n - int((BLADE - 0.005) * SR)]
finish(out, "Planting.wav")

# --- harvesting: rising swish + stalk crackles + rustle ---------------------------------------
rng = np.random.default_rng(2)
n = int(0.55 * SR)
t = np.arange(n) / SR
centre = 600 + 2000 * np.clip(t / 0.22, 0, 1) ** 1.5
swish = swept_band(n, centre, q=1.5) * np.minimum(t / 0.12, 1) * np.exp(-np.maximum(t - 0.12, 0) / 0.07)
rustle = band_noise(n, 1500, 6000) * np.clip((t - 0.12) / 0.03, 0, 1) * np.exp(-np.maximum(t - 0.15, 0) / 0.09)
out = swish + 0.25 * rustle
ticks(out, 14, 0.11, 0.26, 1500, 4500, 0.9, length=0.003, falloff=0.93)
finish(out, "Harvesting.wav")

# --- fertilizing: "pff" + spray hiss + pellet patter -------------------------------------------
rng = np.random.default_rng(3)
n = int(0.6 * SR)
t = np.arange(n) / SR
pff = band_noise(n, 400, 1200) * env(n, 0.005, 0.03)
hiss = band_noise(n, 2500, 8000) * np.clip((t - 0.02) / 0.05, 0, 1) * np.exp(-np.maximum(t - 0.12, 0) / 0.15)
out = 0.6 * pff + 0.55 * hiss
ticks(out, 22, 0.05, 0.45, 2000, 6000, 0.45, length=0.0015, falloff=0.96)
finish(out, "Fertilizing.wav")
