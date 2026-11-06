# Farming Simulator 94

A small farming game built in Unity. Drive around the map, buy fields, plant crops, wait for them to grow, harvest them, and sell the harvest for money. It is a 3D remake of my finished 2D game, [Sim94](https://github.com/anthony3974/Sim94).

## Features
- Main menu (Play > New Game; Load Game comes with save/load) with the version number, and a pause menu (Esc or PAUSE)
- Three vehicles: a tractor, plus a van and a harvester bought at the car dealer
- Fields you can plant and harvest with one button; you pick the crop to plant
- Three crops (wheat, barley, oat), each with growth stages shown by colour
- Field ownership: buy a field from its AI owner, or sell yours back (80% of its price)
- Sell stations for each crop; you have to drive there to sell
- Money, seeds and fertilizer counters, a speedometer, and a field details pop-up
- Tractor front wheels turn with the steering buttons
- Help menu with the controls list (H, or the HELP button on touch)
- Planting costs seeds; the seed shop and the fertilizer shop sell more. Your own field starts planted with wheat
- Engine sound for each vehicle (pitch follows the speed)
- Diesel: one tank shared by all vehicles, a fuel gauge above the speedometer, and Jerrys Gas Station at the bottom of the map to fill up
- Android support with on-screen touch controls (runs on a real phone)

## Requirements
- Unity **6000.0.85f1** (Unity 6), installed through Unity Hub
- Linux, Windows or macOS
- To build for Android: the Android Build Support module (OpenJDK, Android SDK and NDK)

## Getting started
1. Clone the repository.
2. In Unity Hub, choose **Add > Add project from disk** and select the project folder.
3. Open it with Unity 6000.0.85f1. The first import can take a few minutes.
4. Open `Assets/Scenes/MainMenu.unity` and press **Play**, then **PLAY > NEW GAME**. (You can also open `GameScene.unity` directly to skip the menu.)

## Controls
| Input | Action |
| --- | --- |
| W / A / S / D or arrow keys | Drive |
| Mouse | Look around (click to lock the cursor) |
| Esc | Pause menu (resume, or go back to the main menu); also frees the mouse |
| E | Plant or harvest your field, sell at a sell station, buy seeds at the seed shop, fill up at the gas station, open the dealer's shop |
| B | Buy or sell the field you are standing in (press twice to confirm) |
| C | Choose the crop to plant |
| F | Fertilize your field while the crop grows (two levels, +50% harvest each) |
| 1 / 2 / 3 | Drive the tractor / van / harvester (once you own them) |
| H | Open or close the help menu (the controls list) |
| F1 | Toggle debug (does nothing yet) |
| On-screen buttons (Android) | `<` `>` steer, `GAS`, `BRAKE`, `USE` (same as E), `BUY/SELL` (same as B), `CROP` (same as C), `FERT` (same as F), `CAR` (next vehicle), `HELP` (same as H, top-left), `PAUSE` (same as Esc, top-left) |

## Roadmap
The 3D version started from the feature set of the 2D game (buy fields, grow and sell crops, spend the money on vehicles) and now goes its own way. Done so far: planting, growing, harvesting, selling crops, money, buying and selling fields, seeds and fertilizer with their shops, the map border, the car dealer (van and harvester, placeholder models), and the main and pause menus. Next up (not in a fixed order):

- Look around on phones (drag to turn the camera)
- Move the menus still built in code into the scene (help, dealer, message box, speedometer, touch buttons)
- Tractor redesign
- Save / load

The full feature checklist from the 2D game, known issues and design notes are in [`PLAN.md`](PLAN.md).

## Project layout
- `Assets/Scripts`: game code (`FieldController`, `Building`, `Dealer`, `PlayerValues`, `HelpMenu`, `MainMenu`, `PauseMenu`, `GameCharacter`, `Vehicle`, `VehicleCatalog`, `EngineSound`, `FuelGauge`, `InputManager`, `TouchControls`, `FieldView`, `Speedometer`, `RoadTiling`)
- `Assets/Scenes`: `MainMenu` (opens first) and `GameScene`
- `Assets/Prefabs`: tractor, van, harvester, and `UI/UI Button` (every menu button is a copy of it, so they all match)
- `Assets/AllSkyFree`: 4 skyboxes (`Epic_BlueSunset` is the one in use)
- `Assets/TerrainSampleAssets`: terrain textures (kept layers: `Grass_B`, `Grass_A`, `Grass_Moss`, `Grass_Soil`, `Pebbles_B`, `Pebbles_C`)
- `Assets/Editor`: one-off editor tools while they are in use (none right now, so the folder is gone; a new tool recreates it). The builders so far (map border, dealer, tractor and tractor V2, sell stations, shops, road look, house, garage, vehicle prefabs, menus, help menu, touch look, button prefab) were run and removed; they are in the git history
- `PLAN.md`: roadmap, 2D game analysis and notes

## Status
Work in progress.
