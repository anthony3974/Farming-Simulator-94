# Good engine sounds: analysis notes

These are the picks for a farming sim (tractor engine). The notes come from analyzing the files with ffmpeg, numpy and spectrograms on 2026-11-05. Nobody listened to them by ear, so give each one a quick listen to confirm.

## freesound_community-diesel-idle-1-26317.mp3: MAIN IDLE LOOP
| | |
|---|---|
| Format | MP3, 24 kHz, 160 kbps, stereo (both channels identical, so effectively mono) |
| Length | 43.27 s |
| Loudness | -20.6 LUFS, peak -8.0 dBFS, no clipping (quiet, so turn it up in the mixer) |
| Steadiness | Very steady: varies by about 0.7 dB over time, with no revs or pitch changes |
| Tone | Spectral centroid ~534 Hz. Energy: 34% below 120 Hz, 40% at 120-500 Hz, 23% at 500 Hz-2 kHz (diesel clatter), 3% above 2 kHz. Nothing above 12 kHz because of the 24 kHz sample rate |
| Pulse | Steady ~10.7 Hz firing pulse (slow, lumpy idle) |
| Looping | About 40 ms of MP3 padding (near-silence) at both ends, which makes a gap on each loop. Fixed by trimming: see Loops below (Unity 6 has no gapless MP3 option) |
| Use | Tractor idle; the long length hides repetition |

## se_vosj67tpgqy24tkw.wav: DRIVING / UNDER-LOAD LOOP
| | |
|---|---|
| Format | WAV PCM 16-bit, 44.1 kHz, stereo (nearly mono) |
| Length | 15.00 s |
| Loudness | -17.1 LUFS, peak -3.4 dBFS, no clipping (room to turn it up) |
| Steadiness | Steady: varies by about 0.6 dB. It gets slightly brighter over the clip (centroid ~110 Hz rising to ~140 Hz) |
| Tone | Very bassy: 88% of energy below 120 Hz, centroid ~125 Hz. Heavy tractor or combine. Tonal and clean (likely synthetic or AI-generated) |
| Pulse | ~23 Hz firing pulse; harmonics at 23 / 47 / 70 / 93 Hz |
| Looping | Clean seam (end and start levels within 0.4 dB, small sample jump). The brightness drift may make the loop point faintly audible |
| Use | Driving or working layer. Raise its pitch with RPM (about 0.8 to 1.6); it holds up well when pitched |

## freesound_community-diesel-idle-2-62716.mp3: VARIETY / SECOND VEHICLE
| | |
|---|---|
| Format | MP3, 24 kHz, 160 kbps, stereo (both channels identical, so effectively mono) |
| Length | 17.66 s |
| Loudness | -16.8 LUFS, peak -5.4 dBFS, no clipping |
| Steadiness | Steady: varies by about 0.6 dB |
| Tone | Centroid ~354 Hz. Energy: 36% below 120 Hz, 41% at 120-500 Hz, 22% at 500 Hz-2 kHz. Punchier and less clattery than idle-1 |
| Pulse | Faster idle, ~37 Hz dominant pulse |
| Looping | Same MP3 padding gap as idle-1 (about 40 ms at the ends), fixed the same way (see Loops below) |
| Use | Idle for a second vehicle, or a "working" layer between idle and driving |

## Loops (what the game uses)
`Assets/Sounds/Loops` has loop-ready copies made by `python3 Art/Sounds/make_loops.py` (the originals are unchanged): the padding is trimmed, the end crossfades into the start (0.15 s) and they are mono. `DieselIdle1Loop.wav` (from idle-1), `DieselIdle2Loop.wav` (from idle-2), `DieselDriveLoop.wav` (from vosj). Checked: no level dip at the seam, and the seam jump is the size of a normal sample step.

## Unity setup
- Tools > Engine Sound Setup sets the loops to Force To Mono, Decompress On Load, Vorbis.
- Chain (built, `EngineSound.cs`): the idle loop crossfades into the drive loop as the speed rises, with pitch following speed.
- Missing sounds: engine start (cranking), shut-down, and rev-up/acceleration.

## The "maybe" folder, briefly
- `se_lzglo57553gp6cgj.wav`: 15 s, very steady, ~19 Hz pulse. Peaks at 0 dBFS, so turn it down ~6 dB. It clicks at the loop point and needs a crossfade.
- `freesound_community-engine-6000.mp3`: 1.94 s and repeats every 0.53 s, so it sounds synthetic and buzzy. Background use only.
- `se_z7yhdul3hr4orwg6.wav`: 4 s clip with ~2.9 s of mid-range engine that starts and stops abruptly. No bass and no crank. Sounds like a small engine (mower or ATV).
