# Farming Simulator 94

A small first-person farming game built in Unity. Walk or drive around the map, buy fields, plant crops, wait for them to grow, harvest them, and sell the harvest for money. It is a 3D remake of my finished 2D game, [Sim94](https://github.com/anthony3974/Sim94).

## Features
- First-person character movement and a drivable vehicle
- Fields you can plant and harvest with one button; you pick the crop to plant
- Three crops (wheat, barley, oat), each with growth stages shown by colour
- Field ownership: buy a field from its AI owner, or sell yours back (80% of its price)
- Sell stations for each crop; you have to drive there to sell
- Money, seeds and fertilizer counters, a speedometer, and a field details pop-up
- Tractor front wheels turn with the steering buttons
- Help menu with the controls list (H, or the HELP button on touch)
- Planting costs seeds, and a seed shop sells more. Your own field starts planted with wheat
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
| E | Plant or harvest your field, sell at a sell station, buy seeds at the seed shop, open the dealer's shop |
| B | Buy or sell the field you are standing in (press twice to confirm) |
| C | Choose the crop to plant |
| 1 / 2 / 3 | Drive the tractor / van / harvester (once you own them) |
| H | Open or close the help menu (the controls list) |
| F1 | Toggle debug (does nothing yet) |
| On-screen buttons (Android) | `<` `>` steer, `GAS`, `BRAKE`, `USE` (same as E), `BUY/SELL` (same as B), `CROP` (same as C), `CAR` (next vehicle), `HELP` (same as H, top-left) |

## Roadmap
The 3D version aims for the same feature set as the 2D game: buy fields, grow and sell crops, and spend the money on vehicles. Done so far: planting, growing, harvesting, selling crops, money, buying and selling fields, seeds with a seed shop, and the map border. Also done: the car dealer (van and harvester, placeholder models). Planned, in order:

1. Home menu (with the version number)
2. Fuel and a gas station
3. Fertilizer
4. Paint shop
5. Save / load

The full feature checklist from the 2D game, known issues and design notes are in [`PLAN.md`](PLAN.md).

## Project layout
- `Assets/Scripts`: game code (`FieldController`, `Building`, `Dealer`, `PlayerValues`, `HelpMenu`, `GameCharacter`, `Vehicle`, `VehicleCatalog`, `InputManager`, `TouchControls`, `FieldView`, `Speedometer`)
- `Assets/Scenes`: game scene
- `Assets/Prefabs`: houses, garage, tractor
- `Assets/AllSkyFree`: 4 skyboxes (`Epic_BlueSunset` is the one in use)
- `Assets/TerrainSampleAssets`: terrain textures (kept layers: `Grass_B`, `Grass_A`, `Grass_Moss`, `Grass_Soil`, `Pebbles_B`, `Pebbles_C`)
- `Assets/Editor`: editor tools (Map Border Builder). Earlier one-off builders (dealer, tractor, sell stations, shops, road look) were run and removed; they are in the git history
- `PLAN.md`: roadmap, 2D game analysis and notes

## Status
Work in progress.
