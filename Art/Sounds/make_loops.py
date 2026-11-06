#!/usr/bin/env python3
"""Makes seamless engine loops from the downloaded clips in Assets/Sounds.

The two freesound MP3s have about 45 ms of near-silence at the start and 15-20 ms at the end (MP3 encoder
padding), so looping them as they are gives a short gap every loop. Unity 6 has no "gapless" MP3 option,
so this script writes loop-ready WAV copies next to them in Assets/Sounds/Loops:
  1. trim the padding off both ends,
  2. crossfade the end into the start (equal-power, LOOP_FADE seconds), so the loop point does not click,
  3. mix down to mono (both channels are identical anyway).
The WAV clip has a clean seam already, it only gets step 2 and 3.

Needs ffmpeg and numpy. Run from the project folder:  python3 Art/Sounds/make_loops.py
The originals are not changed.
"""
import os
import subprocess
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Assets", "Sounds")
OUT = os.path.join(SRC, "Loops")
LOOP_FADE = 0.15  # seconds of crossfade at the loop point

# source file, output name, seconds to cut from the start, seconds to cut from the end
CLIPS = [
    ("freesound_community-diesel-idle-1-26317.mp3", "DieselIdle1Loop.wav", 0.050, 0.025),
    ("freesound_community-diesel-idle-2-62716.mp3", "DieselIdle2Loop.wav", 0.050, 0.030),
    ("se_vosj67tpgqy24tkw.wav", "DieselDriveLoop.wav", 0.0, 0.0),
]


def decode(path):
    """Mono float32 samples and the file's own sample rate."""
    rate = int(subprocess.check_output(
        ["ffprobe", "-v", "error", "-select_streams", "a:0", "-show_entries", "stream=sample_rate",
         "-of", "default=nw=1:nk=1", path]).strip())
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-f", "f32le", "-"])
    return np.frombuffer(raw, dtype=np.float32).copy(), rate


def make_loop(x, rate, cut_start, cut_end):
    x = x[int(cut_start * rate): len(x) - int(cut_end * rate)]
    n = int(LOOP_FADE * rate)
    t = np.linspace(0.0, 1.0, n, endpoint=False)
    fade_in, fade_out = np.sin(t * np.pi / 2), np.cos(t * np.pi / 2)
    # the last n samples fade into the first n; the loop then restarts at sample n, right where the fade ended
    tail = x[-n:] * fade_out + x[:n] * fade_in
    return np.concatenate([x[n:-n], tail])


def write_wav(path, x, rate):
    pcm = (np.clip(x, -1.0, 1.0) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, dst, cut_start, cut_end in CLIPS:
        x, rate = decode(os.path.join(SRC, src))
        loop = make_loop(x, rate, cut_start, cut_end)
        write_wav(os.path.join(OUT, dst), loop, rate)
        seam = abs(float(loop[-1]) - float(loop[0]))
        print(f"{dst}: {len(loop) / rate:.2f} s at {rate} Hz, seam jump {seam:.4f}")


if __name__ == "__main__":
    sys.exit(main())
