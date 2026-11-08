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
- The loops are imported as Force To Mono, Decompress On Load, Vorbis (set by the one-off Engine Sound Setup tool, git history `d58093e`).
- Chain (built, `EngineSound.cs`): the idle loop crossfades into the drive loop as the speed rises, with pitch following speed.
- Missing sounds: engine start (cranking), shut-down, and rev-up/acceleration.

## The "maybe" folder, briefly
- `se_lzglo57553gp6cgj.wav`: 15 s, very steady, ~19 Hz pulse. Peaks at 0 dBFS, so turn it down ~6 dB. It clicks at the loop point and needs a crossfade.
- `freesound_community-engine-6000.mp3`: 1.94 s and repeats every 0.53 s, so it sounds synthetic and buzzy. Background use only.
- `se_z7yhdul3hr4orwg6.wav`: 4 s clip with ~2.9 s of mid-range engine that starts and stops abruptly. No bass and no crank. Sounds like a small engine (mower or ATV).

# Money-in sound: notes

**Goal:** a short "cha-ching" for selling crops (checklist item "Money in"). It's made from scratch in Python with the `wave` and `math` modules, so it has no copyright issues. Notes from 2026-11-07.

## What stayed the same in versions 1 to 11
- **Ka:** a 50 ms snap of band-pass noise at about 1.5 kHz, plus a 210 to 230 Hz thud (fixed pitch) that fades in about 35 ms. (Version 9 changed this into a bigger "cha" and version 10 removed the thud, see the table.)
- **Level:** peak -8 dBFS. (Earlier notes said this matched the click and menu sounds; it doesn't, they peak at -2.5 to -6 dBFS. -8 was kept because it sounded right next to them.)
- **Ending:** the last 50 to 80 ms fades to zero, so it can't cut off.
- **Soft saturation:** `tanh` at about 1.3 to 1.8, which adds punch.

## Versions tried
| # | Version | Verdict | Why |
|---|---|---|---|
| 1 | Two bell strikes (880 / 1320 Hz), 1 s | OK start | Plain bell |
| 2 | Same bell, brighter and shorter | "make it sharper" | Needed more attack |
| 3 | Added the thud, saturation, louder bells | "punchier", then "quieter" | Fine, but loud (-1.9 dB) |
| 4 | Same sound, 6 dB quieter | "too bell like" | A ringing bell reads as a toy |
| 5 | Three coin clinks, 50 ms apart | "too robotic" | Perfect sine tones in even timing |
| 6 | Seven random coins, noise hits, detuning, echoes | "broken pottery" | Random, dull, short fades sound like ceramic |
| 7 | Four clean metal pings, slight uneven pitch and timing | remake asked for | Middle ground |
| 8 | Ka, then two coin strikes (1480 Hz, then 1235 Hz quieter, 73 ms later), split mode pairs that shimmer, tiny bright tick on each strike, 600 ms | "more cha" | Metal from shimmer and attack, not from a long ring |
| 9 | Same as 8, but the ka became a "cha": noise 1 to 4.5 kHz, 90 ms instead of 50 ms, slower fade, louder thud. The coins come 27 ms later to make room, 620 ms | "remove the drum effect, sharper cha, then king" | The ka was a short snap, quiet next to the coins |
| 10 | No thud. Cha starts instantly (0.3 ms), 1.5 to 6 kHz, 70 ms, fades faster, as loud as the coins. A short silent gap, then the coins with an instant attack and a louder bright tick for a hard "k". 600 ms | "more coin effect" | The 220 Hz thud read as a drum |
| 11 | Same as 10, plus the second coin bouncing and settling: 7 more strikes, each about 4 dB quieter and with a gap about a quarter shorter than the last (85 ms down to ~15 ms), small random jitter, same coin pitch within 1%. 660 ms | "make it a desk bell sound" | An accelerating rattle is the sound of a coin landing |
| 12 | Desk bell (shop counter "ding"), replacing the ka-ching: a 6 ms hammer tick, then a dome ringing at 1650 Hz with partials at 2.76x and 5.4x. The high partials fade first (bright to mellow). Each partial is an uneven pair of sines a few Hz apart, for a gentle ~5 dB wobble. 1.5 s, the last 0.35 s fades out | "shorten it" | A bell on purpose this time |
| 13 | Same desk bell, rings about half as long (decay times 0.22 / 0.10 / 0.045 s instead of 0.42 / 0.16 / 0.06). 0.8 s, the last 0.2 s fades out | "double it so there is 2 after an other" | 1.5 s was too long |
| 14 | Version 13 struck twice, "ding-ding": second strike 220 ms after the first at 85% level; the hammer damps the first ring to 30% when it lands. The overall level only rises ~2 dB at the second strike, but above 3 kHz it jumps ~17 dB, so the bright attack marks it. 1.0 s, the last 0.2 s fades out | "slightly further apart" | |
| 15 | Same, second strike 270 ms after the first instead of 220 ms. 1.05 s | "change the tone of the first one" | |
| 16 | First ding a perfect fourth lower (~1236 Hz), second stays at 1650 Hz, so it rises like a reward. 1.05 s | "make the first one shorter" | Two pitches instead of the same ding twice |
| 17 | First ding rings half as long (decay times 0.11 / 0.05 / 0.0225 s), so it's a quick lead-in to the second. 1.05 s | "good!" (final) | |

## What we learned
- **Bell:** a long, tonal ring means bell. Keep the ring under about 150 ms.
- **Robotic:** exact pitches and even timing mean machine. A little unevenness helps.
- **Pottery:** noise hits, strong random detuning and short fades mean ceramic. Metal needs clean, slightly sustained partials.
- **Bloop:** a pitch sweep means Pac-Man. A fixed pitch under a transient is fine.
- **Too high:** above about 2 kHz was "too high" for the click. Coins sit in the 1.1 to 1.7 kHz range.
- **Metal partials:** ratios of 1, 2.76 and 5.4 are the bell-like ones that work here.

## Limits and status
- The generator can only check numbers (peak, loudness, start and end); judging the sound is done by ear.
- Real coins are very complex, so a CC0 recording from freesound ("cash register", "coin pickup", "ka-ching") would probably still sound best.
- Generator: `python3 Art/Sounds/make_money_in.py` writes `Assets/Sounds/MoneyIn.wav` (fixed random seed, so the same file every run). Versions 1 to 7 had no saved script, and versions 8 to 11 were replaced by the desk bell (rebuild them from the table if needed).
- Desk bell lesson: equal-level split pairs cancel fully and make a deep ~20 dB pulse; a 65/35 split keeps the wobble gentle.
- Final: version 17, in `Assets/Sounds/MoneyIn.wav`. Not played from code yet.

# UI sounds: notes

These were made from scratch earlier (commits `16379ca`, `43ac49c`), but the generator wasn't saved, so these numbers come from measuring the files on 2026-11-07. None of them is played from code yet.

## ButtonClick.wav
| | |
|---|---|
| Format | WAV PCM 16-bit, 44.1 kHz, mono |
| Length | 100 ms, starts and ends at zero |
| Level | Peak -2.5 dBFS, RMS -23.4 dB. Loudest of the UI sounds, so it may need turning down in the mixer |
| Shape | Two clicks, like a press and a release: a hit at 0 ms that is gone by 20 ms, then a second, quieter hit at about 30 ms. Silent after 60 ms |
| Tone | A short tone at about 1.25 kHz under a bright noise snap (spectral centroid ~6.3 kHz) |
| Lessons | An earlier try with its main tone above about 2 kHz was "too high" |

## MenuOpen.wav and MenuClose.wav
| | MenuOpen | MenuClose |
|---|---|---|
| Length | 200 ms | 180 ms |
| Peak | -4.4 dBFS | -6.0 dBFS |
| Shape | Swells in, loudest at about 100 ms, fades out | Swells in, loudest at about 70 ms, fades out |
| Pitch | Rises, about 480 Hz to 1.4 kHz | Falls, about 1.15 kHz to 380 Hz |
