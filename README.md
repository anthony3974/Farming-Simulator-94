# Farming Simulator 94

A small first-person farming game built in Unity. Walk or drive around the map, plant crops in fields, wait for them to grow, and harvest them. It is a 3D remake of my finished 2D game, [Sim94](https://github.com/anthony3974/Sim94).

## Features
- First-person character movement and a drivable vehicle
- Fields you can plant and harvest with one button
- Three crops (wheat, barley, oat), each with growth stages shown by colour
- Field details pop-up (name, owner, value, size, crop, status) and harvest counters
- Android support with on-screen touch controls (APK builds and runs on an Android VM)

## Requirements
- Unity **6000.0.85f1** (Unity 6), installed through Unity Hub
- Linux, Windows or macOS
- To build for Android: the Android Build Support module (OpenJDK, Android SDK and NDK)

## Getting started
1. Clone the repository.
2. In Unity Hub, choose **Add > Add project from disk** and select the project folder.
3. Open it with Unity 6000.0.85f1. The first import can take a few minutes.
4. Open `Assets/Scenes/GameScene.unity` and press **Play**.

## Controls
| Input | Action |
| --- | --- |
| W / A / S / D or arrow keys | Move / drive |
| Mouse | Look around (click to lock the cursor, Esc to release) |
| E | Plant or harvest the field you are standing in |
| F1 | Toggle debug |
| On-screen buttons (Android) | `<` `>` steer, `GAS`, `BRAKE`, `USE` (plant / harvest) |

## Roadmap
The 3D version aims for the same feature set as the 2D game: buy fields, grow and sell crops, and spend the money on vehicles. Done so far: planting, growing and harvesting. Planned, in order:

1. Money and a sell point (the Wheat Selling Station is next)
2. Seeds and a seed shop
3. Buying and selling fields
4. Fuel and a gas station
5. Fertilizer
6. Dealer (van, harvester) and paint shop
7. Save / load

The full feature checklist from the 2D game, known issues and design notes are in [`PLAN.md`](PLAN.md).

## Project layout
- `Assets/Scripts`: game code (`FieldController`, `PlayerValues`, `GameCharacter`, `InputManager`, `TouchControls`, `FieldView`)
- `Assets/Scenes`: game scene
- `Assets/Prefabs`: houses, garage, truck
- `PLAN.md`: roadmap, 2D game analysis and notes

## Status
Work in progress.
