#!/usr/bin/env python3
"""Makes the engine start one-shots from the downloaded clips in Assets/Sounds.

Writes mono WAV copies to Assets/Sounds/Starts:
  1. pitch and clean up the clip where it needs it, and cut the near-silent MP3 padding off the start,
  2. level the clip to the same loudness as the others (LEVEL_RMS), never above PEAK_LIMIT,
  3. a very short fade in and fade out, so it starts and ends at zero (EngineSound does the crossfade into the idle loop).

Needs ffmpeg and numpy. Run from the project folder:  python3 Art/Sounds/make_starts.py
The originals are not changed.
"""
import os
import subprocess
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Assets", "Sounds")
OUT = os.path.join(SRC, "Starts")
LEVEL_RMS = -17.0   # dBFS over the whole clip; the idle loops are about -20 and play at 0.8 volume
PEAK_LIMIT = -1.0   # dBFS
FADE_IN = 0.005     # seconds
FADE_OUT = 0.05     # seconds

# source file, output name, seconds to cut from the start (after pitching), pitch, high-pass Hz (0 = none)
CLIPS = [
    # tractor and harvester. Pitch 1.3: as recorded it ran at an 8.3 Hz pulse and the idle loop runs at 10.9, and its
    # 25 Hz rumble made it sound deeper than the idle (owner, 2026-11-08). Pitching also makes it shorter (2.0 s to 1.5 s).
    ("freesound_community-engine-start-86242_3.mp3", "EngineStartDiesel.wav", 0.03, 1.3, 50),
    ("u_rq4ihf0g2m-volvo-engine-431665_1.mp3", "EngineStartVan.wav", 0.0, 1.0, 0),
]


def decode(path, pitch, highpass):
    """Mono float32 samples, pitched and high-passed, and the file's own sample rate."""
    rate = int(subprocess.check_output(
        ["ffprobe", "-v", "error", "-select_streams", "a:0", "-show_entries", "stream=sample_rate",
         "-of", "default=nw=1:nk=1", path]).strip())
    filters = []
    if pitch != 1.0:
        filters += [f"asetrate={rate * pitch:.0f}", f"aresample={rate}"]
    if highpass > 0:
        filters += [f"highpass=f={highpass}"]
    af = ["-af", ",".join(filters)] if filters else []
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", *af, "-f", "f32le", "-"])
    return np.frombuffer(raw, dtype=np.float32).copy(), rate


def db(value):
    return 20.0 * np.log10(max(float(value), 1e-9))


def make_start(x, rate, cut_start):
    x = x[int(cut_start * rate):]
    gain = min(LEVEL_RMS - db(np.sqrt(np.mean(x ** 2))), PEAK_LIMIT - db(np.abs(x).max()))
    x = x * 10.0 ** (gain / 20.0)
    n_in, n_out = int(FADE_IN * rate), int(FADE_OUT * rate)
    x[:n_in] *= np.linspace(0.0, 1.0, n_in, endpoint=False)
    x[-n_out:] *= np.cos(np.linspace(0.0, 1.0, n_out) * np.pi / 2)
    return x


def write_wav(path, x, rate):
    pcm = (np.clip(x, -1.0, 1.0) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, dst, cut_start, pitch, highpass in CLIPS:
        x, rate = decode(os.path.join(SRC, src), pitch, highpass)
        out = make_start(x, rate, cut_start)
        write_wav(os.path.join(OUT, dst), out, rate)
        print(f"{dst}: {len(out) / rate:.2f} s at {rate} Hz, rms {db(np.sqrt(np.mean(out ** 2))):.1f} dBFS, "
              f"peak {db(np.abs(out).max()):.1f} dBFS, ends at {abs(float(out[-1])):.4f}")


if __name__ == "__main__":
    sys.exit(main())
