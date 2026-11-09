# Farming Simulator 94

A small farming game built in Unity. Drive around the map, buy fields, plant crops, wait for them to grow, harvest them, and sell the harvest for money. It is a 3D remake of my finished 2D game, [Sim94](https://github.com/anthony3974/Sim94).

## Features
- Main menu (Play > New Game or Load Game) with the version number (now 0.6.8), and a pause menu (Esc or PAUSE)
- Save / load: the farm saves by itself (every minute, when you pause, go to the main menu or leave the app); Load Game puts it back
- Three vehicles: a tractor (45 km/h), plus a van (56 km/h) and a harvester (34 km/h, +60% harvest) bought at the car dealer. The van is for the road: it can buy and sell fields but cannot plant, harvest or fertilize
- Fields you plant, fertilize and harvest with one button (E / USE); you pick the crop to plant
- Fertilizer: two levels per crop, +50% harvest each; a freshly fertilized field turns blue (deeper blue at level 2) until its next growth stage, like the 2D game
- Three crops (wheat, barley, oat), each with growth stages shown by colour (about 83 s from planting to ready)
- Field ownership: buy a field from its AI owner, or sell yours back (80% of its price)
- AI owners work their own fields: they plant, sometimes fertilize and harvest them, so the fields change over time; a field you buy comes with whatever grows on it
- Sell stations for each crop; you have to drive there to sell
- Money, seeds and fertilizer counters, a speedometer, and a field details pop-up
- The wheels turn with the steering (the harvester steers with its back wheels, like a real combine) and roll as you drive, with a V-pattern tread on every tire
- Help menu with the controls list (H, or the HELP button on touch)
- Planting costs seeds; the seed shop and the fertilizer shop sell more. Your own field starts planted with wheat
- Engine sound for each vehicle (pitch follows the speed; only the vehicle you drive is heard)
- Sounds for button clicks, menus opening and closing, money in and out, and planting, harvesting and fertilizing your fields
- Diesel: one 100 L tank shared by all vehicles, used as you drive, with a fuel gauge above the speedometer and a low-fuel warning. An empty tank only crawls (15% speed) so you can still reach the station
- Jerrys Gas Station at the bottom of the map, beside the main road: fills the tank slowly at $2 per litre (press again to stop); the price shows on the pumps
- App icon (Android adaptive icon)
- Android support with on-screen touch controls (runs on a real phone). A SETTINGS button (below HELP) picks how to steer (`<` `>` buttons, a steering wheel, or a slider), how to drive (GAS / BRAKE buttons, or a pedal slider: 80% gas, 20% brake) and square or rounded buttons; the wheel and sliders steer and drive gently, not just full on or off. A GAME tab in SETTINGS turns the mirrors, the minimap and the day / night cycle on or off and sets how long a day and a night last (default 10 and 5 minutes); street lamps light up at night. On PC, O opens SETTINGS. The game keeps running while SETTINGS is open, unlike PAUSE

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
| Mouse | Turn the camera all the way around the vehicle and tilt it (click to lock the cursor); it stays where you leave it |
| Mouse wheel | Zoom the camera in and out |
| Esc | Pause menu (resume, or save and go back to the main menu); also frees the mouse |
| E | Plant, fertilize (while it grows; two levels, +50% harvest each) or harvest your field, sell at a sell station, buy seeds or fertilizer at the shops, start / stop filling up at the gas station, open the dealer's shop |
| B | Buy or sell the field you are standing in (press twice to confirm) |
| C | Choose the crop to plant |
| 1 / 2 / 3 | Drive the tractor / van / harvester (once you own them) |
| Tab | Next vehicle you own (same as the `CAR` touch button) |
| O | Open / close SETTINGS |
| H | Open or close the help menu (the controls list) |
| F1 | Toggle debug (does nothing yet) |
| On-screen controls (Android) | Steer with `<` `>`, the wheel or the slider, and drive with `GAS` / `BRAKE` or the pedal slider (picked in `SETTINGS`; held buttons sink while pressed, sliding off and back on keeps working); drag the screen to turn the camera around the vehicle (full 360) and tilt it, pinch with two fingers to zoom, double-tap to bring the camera back behind the vehicle; `USE` (same as E), `BUY SELL` (same as B), `CROP` (same as C), `CAR` (next vehicle); top-left: `HELP` (same as H), `PAUSE` (same as Esc), `SETTINGS` |

## Roadmap
The 3D version started from the feature set of the 2D game (buy fields, grow and sell crops, spend the money on vehicles) and now goes its own way. Done so far: planting, growing, harvesting, selling crops, money, buying and selling fields, seeds and fertilizer with their shops, the map border, the car dealer (van and harvester), the main and pause menus, look around on phones, the menus moved into the scene, the tractor and harvester redesigns, the app icon, engine sound, fuel with the gas station, save / load, fertilized fields turning blue, the touch controls rework (steering wheel, sliders, rounded buttons, SETTINGS), AI owners working their own fields, the day / night cycle, the first street lamp, tire tread with rolling wheels, and the menu, money and farming sounds. Next up, in order:

- Engine start sound
- Street lamps across the map (bigger lamp first)
- A minimap
- Clearer error messages
- Soon after: vehicle headlights, lights on the house and garage, engine quieter when zoomed out, vehicle mirrors
- Later: drive across a field to harvest it

The full feature checklist from the 2D game, known issues and design notes are in [`PLAN.md`](PLAN.md).

## Project layout
- `Assets/Scripts`: game code (`FieldController`, `Building`, `Dealer`, `PlayerValues`, `HelpMenu`, `MainMenu`, `PauseMenu`, `SaveGame`, `GameCharacter`, `Vehicle`, `VehicleCatalog`, `EngineSound`, `GameSounds` (click, menu, money and farming sounds), `FuelGauge`, `Speedometer`, `ScreenPrompt` (message box), `InputManager`, `TouchControls`, `TouchButtonInput`, `TouchSettings`, `SettingsMenu`, `GameSettings`, `DayNightCycle`, `NightLight`, `SteeringWheel`, `TouchSlider` (`SteeringSlider`, `PedalSlider`), `TouchLook`, `FieldView`, `RoadTiling`)
- `Assets/Scenes`: `MainMenu` (opens first) and `GameScene`
- `Assets/Prefabs`: tractor, van, harvester (each with its `Vehicle` and `EngineSound`), and `UI/UI Button` (every menu button is a copy of it, so they all match)
- `Assets/Sounds`: engine clips and the one-shot sounds; `Loops/` holds the loop-ready copies the game uses (see `AUDIO_NOTES.md`)
- Building and vehicle materials: `Tractor`, `Vehicles`, `Dealer`, `Shops`, `SellStations`, `GasStation`, `House`, `Garage`, `MapBorder`; HUD images in `Images` (speedometer, app icon, `Touch`: the steering wheel and the rounded button shapes)
- `Assets/AllSkyFree`: 4 skyboxes (`Epic_BlueSunset` is the one in use)
- `Assets/TerrainSampleAssets`: terrain textures (kept layers: `Grass_B`, `Grass_A`, `Grass_Moss`, `Grass_Soil`, `Pebbles_B`, `Pebbles_C`)
- `Assets/Editor`: `AndroidVersionCode.cs` (kept: every Android build sets the version code from the version, 0.6.8 -> 608) and one-off editor tools while they are in use. **Now: Build Minimap** (Tools > Build Minimap: the minimap in GameScene, `MinimapBuilder.cs`) and **Street Lamp Builder** (Tools > Street Lamp Builder: the street lamp prefab and one test lamp in the middle of the map, `StreetLampBuilder.cs` + `StreetLampModel.cs`). The builders so far (map border, dealer, tractor and tractor V2, sell stations, shops, road look, house, garage, vehicle prefabs, menus, help menu, touch look, button prefab, harvester V2, app icon, engine sound, gas station, fuel gauge, touch controls, help menu rows, settings game tab, tire tread, vehicle ground fix, game sounds, engine start, vehicle lights) were run and removed; they are in the git history
- `Art`: source files kept outside `Assets` (Unity does not import them): `Icon/make_icon.py` draws the app icon, `Sounds/make_loops.py` makes the seamless engine loops, `Sounds/make_money_in.py`, `make_money_out.py` and `make_farm_sounds.py` make the money and farming sounds, `Touch/make_touch_sprites.py` draws the steering wheel and rounded button sprites
- `PLAN.md`: roadmap, 2D game analysis and notes

## Status
Work in progress.
