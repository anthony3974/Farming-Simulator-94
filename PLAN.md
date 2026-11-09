# Farming Simulator 94: Plan

**When asked "what's next?", the answer is the first open item under NEXT UP** (order set by the owner).
**To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."

Cleaned up on 2026-11-08: finished work is one line each under "Done"; the long notes on it (how each tool worked, test steps, old decisions) are in the git history of this file (`git log -p PLAN.md`, last long version in `356ea6b`). Sound details are in `Assets/Sounds/AUDIO_NOTES.md`.

Unity 6000.0.85f1, Built-in render pipeline. Scenes: `Assets/Scenes/MainMenu.unity` (opens first), `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`. Version 0.7. **Releasing:** change only Project Settings > Player > Version; the Android version code is set from it on every Android build (`Assets/Editor/AndroidVersionCode.cs`: major x 10000 + minor x 100 + patch, 0.6.8 -> 608; it was the version without dots, 68, before).

## NEXT UP (in order)

Nothing queued: the owner has moved on to a new game (2026-11-08) and may not be back for a while. If work resumes, start with the phone checks under "Waiting on the owner".

**Maybe, if the owner comes back to this game; ask before starting:**
- **More tractors.** New `VehicleKind`s in `VehicleCatalog` plus prefabs (built like Tractor V2); keys 1 / 2 / 3 only cover three (Tab and CAR cycle); the dealer menu needs more rows.
- **Vehicle mirrors.** Side mirrors, maybe a rear-view mirror: a small camera per mirror into a small RenderTexture on the mirror (or a screen overlay; decide). Reads `GameSettings.Mirrors` (switch exists); cameras off when off. Check the Android frame rate.
- **Spring-like camera lag at speed.** The camera trails a little as the vehicle speeds up and catches up as it slows, more at higher speed. Add it to `PlaceCamera` in `GameCharacter.cs` (no Cinemachine needed). The camera pivot is a child of the vehicle, so today the camera follows it rigidly. Keep the wall check (`ObstacleScale`) working on the lagged position, and don't let the lag carry over when switching vehicles.
- **Advanced crop harvesting (bottom of the queue).** Drive across the field to harvest it; design and notes below. The owner does want it if work on this game continues, but it is the complicated one, and it could lead to advanced seeds and advanced fertilizing too.


**Dropped (owner):** the paint shop (2026-11-04), trailers (2026-11-08).

## Waiting on the owner (built, not yet tried or decided)
- **Phone checks (2026-11-08, needs a new build):**
  1. **Save, the most important one:** play for over 2 minutes so it autosaves twice, close the app, then check that Load Game brings back recent progress. If it doesn't, `File.Replace` is failing there (`SaveGame.Save`), and saves are quietly not working: fix this first.
  2. **Minimap:** check that the letters are big enough to read, and that it doesn't crowd the fuel gauge or the Settings button.
  3. **Light pools at night:** confirm they look right on the phone too, if only seen in the editor.
  4. **Back button:** with the help menu open, it should close the help, the same as Esc.
- **SETTINGS > GAME tab leftovers:** check the night brightness on the phone.
- **Empty tank:** the vehicle crawls at 15% (owner: fine for now; may become game over later).
- **Sky and terrain layers:** final for now; one or two may be deleted later.

## Smaller open notes
- Game Sounds Setup and Engine Start Setup were run and removed (git `105213b`); bring them back from there when adding sounds.
- Android: performance on a phone (terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`); sharing (APK for Android friends, maybe a WebGL build on itch.io); optional icons instead of `<` `>` `GAS` `BRAKE` `USE` labels, haptic feedback.
- `PlayerValues` has no null checks on the wheat, barley and oat labels; other scripts besides `FieldController` and `Dealer` have no missing-reference check.

## Done (newest first)
One line each; tools named "(git `abc1234`)" were run and then removed, and can be brought back from that commit.
- **House and garage lights (2026-11-08):** the farmhouse porch light and the light over the garage's side door work at night (`NightLight.cs`): the street lamps' glowing lens plus one small warm Point Light each, called "Night Light" under the fixture (no shadows, Important; range 14 and 10, intensity 2 and 1.5). Owner happy. House Lights Setup git `8e8ee69`.
- **Save file swapped in one step (2026-11-08):** `SaveGame.Save` uses `File.Replace`, so a kill mid-save can't leave no save.
- **Vehicle headlights (2026-11-08):** automatic at night and only on the vehicle you drive (`VehicleLights.cs`): the lens parts glow, plus one real forward Spot `Light` per vehicle (`Headlight Beam` on the prefab). Owner: "headlights look right". Vehicle Lights Setup git `105213b`.
- **Error messages (2026-11-08):** every failure says why in the centre box with `ErrorKnock.wav` (`ScreenPrompt.Error`, `Building.ShowError`). Full sentences end with a full stop, prices are written "$ 1,234", and seed and fertilizer errors give only the amount needed (what you have is on the Statistics panel). Every harvest of your own field shows a message. Left as they are on purpose (owner): E / USE or B / BUY SELL away from a field or building does nothing, buying a vehicle you already own, no special look for errors.
- **Minimap (2026-11-08):** middle of the left edge, between the fuel gauge and the Settings button, on phone and PC alike, turned 90 degrees so north points left. Plain UI objects under the `Minimap` canvas, no camera (`Minimap.cs`): roads, fields (real colour every 2 s, gold edge on your own), buildings as letters (W B O sell, S seeds, F fertilizer, G gas, D dealer, H home), red arrow for the driven vehicle. Follows `GameSettings.Minimap` during a game; takes no touches. A moved field or building keeps its old mark on the map until that mark is fixed by hand or the tool re-run. Owner: "minimap good". Build Minimap git `edb9979`.
- **Street lamps (2026-11-05 to 11-08):** 33 copies of `Assets/Prefabs/Street Lamp.prefab` under `Terrain > Street Lamps`: 15 along the main road (3 were spares to move by hand), 7 along the sub road about 70 m apart, 11 at the buildings. 2.5x size (20.3 m tall), 10 m arm. Every lamp shows a fake "Light Pool" on the ground at night (`LightPool.cs`, `Assets/StreetLamp/LightPool.shader`; brightness and colour on `Assets/StreetLamp/Light Pool.mat`), and `StreetLampLights.cs` keeps the real light on the lamp nearest the driven vehicle, cross-fading with a 5 m margin; the same on phone and PC. One flat quad works because the terrain is completely flat; hills would need a mesh that follows the ground. The owner saw it at night and is happy, and the phone ran great even with all 33 real lights. Street Lamp Builder git `edb9979`.
- **All 12 AI field names replaced with made-up ones (2026-11-08)** in `FieldController.FieldNames`, away from the John Deere brand and the map and expansion names from GIANTS' games. A save puts its own field names back, so an old save keeps the old names until a new game.
- **Money is a `double` (2026-11-08):** `PlayerValues.money` and the save's `SaveData.money` were floats, which lose cents above about $130,000, and above about $4 million the pump's per-frame charge rounded to nothing (free diesel). Older saves load unchanged (same JSON field, format still 1).
- **H blocked while a menu is open (2026-11-08):** `InputManager` clears `help` with the other game keys while the pause, dealer or settings menu is open, and `HelpMenu` hides itself if one of those menus opens while the help is up.
- **TMP Essentials tracked (2026-11-08):** `Assets/TextMesh Pro` was in `.gitignore`, so a fresh clone had no `LiberationSans SDF` and no text. The folder is now in git, taken from Unity's TextMeshPro 3.2.0-pre.15 package (the classic TMP that Unity 6's built-in `com.unity.ugui` 2.0.0 is based on; ugui 2.0.0 itself only ships inside the editor), plus the EmojiOne sprite asset that `TMP Settings` points to. Same GUIDs as Window > TextMeshPro > Import TMP Essential Resources. A working copy that already has the folder must move it away before pulling.
- **AI field "John Deer" renamed "Deer Creek" (2026-11-08)** in `FieldController.FieldNames`, away from the John Deere brand. Old saves keep "John Deer" on that field, because a save puts its own field names back.
- **Engine quieter when zoomed out (2026-11-08):** both loops and the start sound go down to `Far Volume` (40%) at full zoom out, full volume at the default distance and closer (`EngineSound.cs`, `GameCharacter.ZoomOut01`; Show Debug shows it). It follows the zoom, not the pull-in from walls, so the sound doesn't jump near obstacles. Owner: "volume good".
- **Engine start sounds (2026-11-08):** a one-shot on getting in or switching (also when the game begins) that crossfades into the idle loop over its last 55% (`Start Blend`). Tractor and harvester share `Starts/EngineStartDiesel.wav` (harvester pitched 0.85), the van has `Starts/EngineStartVan.wav`; made by `Art/Sounds/make_starts.py`. Owner: "good enough".
- **Help menu PC row for O (2026-11-08):** "Settings (mirrors, minimap, day / night) / O" below the pause row (Tools > Help: PC Settings Row, run by the owner, then deleted; not committed before deletion).
- **Tried by the owner (2026-11-08):** fertilizer colour (blue at level 1, deeper blue at level 2, back to the crop colour at the next stage), camera orbit (full 360, zoom, double-tap recentre, no clipping into walls), Tab = next vehicle on PC. All good.
- **Vehicles no longer float when driven; van mud flaps removed (2026-11-08).** A CharacterController rests 8 cm (Skin Width) above the ground and the tractor's was set 5 cm low, so it rose 11 cm in Play. Tools > Vehicle Ground Fix (git `234fce5`) set each prefab's controller Center Y so the tire rubber sits 1 cm into flat ground with the lugs in it (owner: "lugs in the ground like the van and harvester"): tractor 2.69, van 2.59, harvester 3.49. Run it again after changing a tire size or the tread.
- **Tire tread and rolling wheels (2026-11-08):** V-pattern cube lugs on every tire of all three vehicles (Tire Tread Builder, git `26cd78b`; lug colour `Assets/Vehicles/TireTread.mat`); the tires roll by the distance really driven (`Roll Wheels` on `Vehicle`).
- **Farming sounds (2026-11-08):** planting (shovel scrape, then dirt), harvesting, fertilizing; only on your own fields. Planting remade louder and sharper (version 5, peak -5 dBFS); owner: "planting sound is good".
- **Money and UI sounds (2026-11-07 / 08):** click, menu open / close, money in, money out (`GameSounds.cs`, prefab `Assets/Resources/GameSounds.prefab`). Owner: "sounds are great".
- **SETTINGS > GAME tab and day / night cycle (2026-11-07):** mirrors, minimap, cycle on / off, day and night length (default 10 and 5 min), remembered on the device (`GameSettings.cs`); O opens SETTINGS on PC. `DayNightCycle.cs` on the `Directional Light` (sun, moon, dimmed sky copy; skip to sunset / night from its menu); `NightLight.cs` lights the lamps at night. Settings Game Tab builder: git `baef95c`.
- **First street lamp (2026-11-07):** see the street lamps entry above.
- **Fuel and the gas station (2026-11-06):** one shared 100 L tank, 10 L per km, Jerrys Gas Station fills at 4 L/s for $2 / L (E / USE starts and stops), fuel gauge above the speedometer, low-fuel warning at 20% (Gas Station Builder git `ad4b1a4`, Fuel Gauge Builder git `d7abd45`).
- **AI owners work their own fields (2026-11-06):** random start states, they plant, sometimes fertilize and harvest (`FieldController.AiWork`, settings under "AI owners").
- **Touch controls rework (2026-11-06):** buttons are scene objects; SETTINGS panel picks steering (`< >`, wheel, slider), pedals (buttons or a pedal slider, 80% gas / 20% brake) and square or rounded buttons (`TouchSettings.cs`); held buttons sink; sliding off and back on works; FERT button removed, USE fertilizes (Build Touch Controls git `a94cdbb`).
- **Save / load (2026-11-06):** `SaveGame.cs`, `farm.json` in `persistentDataPath`; saves every 60 s, on pause, main menu and app close. Saves money, stock, fuel, selected crop, every field (matched by hierarchy path), owned vehicles and their places. Pressing Play in GameScene directly starts a new farm and its autosave replaces the editor's save.
- **Engine sound (2026-11-05):** idle + drive loops per vehicle, pitch follows speed, only the driven vehicle is heard (`EngineSound.cs`, loops in `Assets/Sounds/Loops`; Engine Sound Setup git `d58093e`).

## Rules and look
- **UI colours (owner, 2026-11-04):** brown fill `(60, 45, 20)`, yellow border `(255, 204, 0)`, gold text `(255, 201, 51)`. Pop-up panels dark grey `(31, 31, 31)`. Pressed `(110, 85, 10)`, greyed out `(70, 70, 70)`. **Every button is a copy of `Assets/Prefabs/UI/UI Button.prefab`**: change only the name, place, size and text on a copy; restyle in the prefab.
- **Build things in the scene hierarchy, not in code (owner, 2026-11-04).** Menus, buildings, vehicles, anything visible are real objects in the scene or a prefab, so the owner can edit them. Scripts hold references and run the behaviour. The usual way: a one-off Tools menu script that builds the objects, then is deleted (kept in git history). Plain positioned UI, no layout groups unless asked.
- **Direction:** the 3D game is its own game now; the 2D game below is a reference and idea source, not a checklist.

## Economy balance (set 2026-11-02, owner may revise)
A field's `size` is its harvest amount (about 4,900 to 23,300). Prices are per unit of harvest, keeping the 2D ratios:
- Seeds per planting: 0.75 to 0.9 x harvest. Seed shop: 2,000 seeds for $900 to $1,100. Fertilizer shop: 2,000 for $800 to $1,000. Fertilizer per use: 0.42 to 0.58 x harvest.
- Selling crops: $0.65 to $0.75 per unit. Land: $2.1 per unit of harvest; selling back pays 80%.
- Start: $5,000, 25,000 seeds (Inspector: 2,000 in the scene), 0 fertilizer, 100 L diesel. The player's field "Tony" starts planted with wheat.
- Growth: 4 stages of 20.79 s, about 83 s per crop (`growthStageSeconds` on each field).
- Harvest bonuses: harvester +60%, fertilizer +50% per level.

## Advanced crop harvesting (design, distant; owner 2026-11-06)
Today harvesting is instant (E / USE on a Ready field, `FieldController.Harvest`). The idea: drive across the field to harvest it.
1. **Cells.** When a field is Ready, a nested `for` loop makes a grid of small trigger `BoxCollider`s over it (`harvestCellSize` in metres); destroyed when the field resets (pool if slow).
2. **Payout.** Each cell pays `yield / cellCount * (1 + bonuses)` once (harvester +60%, fertilizer +50% per level, later a small `HarvestBonus` list). Stock goes to `PlayerValues` as it is cut.
3. **Harvesting mode.** A toggle (E / USE or a HARVEST button, also on touch), only in the harvester (or vehicle); cells react only with the mode on and the field Ready.
4. **Progress.** "Harvested 45%" on the field pop-up; at 100% the field goes back to empty; half-harvested fields stay Ready.
5. **Visuals.** Start with no visual, then stubble quads or a texture mask per cut cell.
- Open: fuel per cell? Cell size vs. phone performance. Keep instant harvest as a fallback?
- Order: cell grid with gizmos, mode toggle, payout, progress and reset, visuals, touch button.

## Notes from chat (Nov 8, 2026)

### Unity farming game: advanced harvesting
- One game with a harvesting mode setting (regular vs. advanced), not two games. Both modes share the same fields, crops, tractor, dealership and saves. Could be per save or per field.
- Advanced mode uses physical crop colliders:
  - The field stays one object, with no splitting into sub-fields.
  - Spawn real crop colliders only in a small zone around/ahead of the combine, and recycle them behind it.
  - Use object pooling to keep it smooth on phone.
  - Regular mode skips spawning and just uses the grid.
- Field memory: a grid over the field, each cell storing how much crop is still standing (full = untouched, 0 = harvested). The combine reduces cells under the header, and colliders only spawn where crop remains.
- Render the same grid as a texture so cut areas look bare. One data source drives both visuals and gameplay.
- Suggested first step: a tiny test field with one machine, get spawn-and-recycle working, then scale up.

### Open questions
- How big are the fields? That sets the grid resolution.
- Should cut crops fall or fly around, or just disappear as the header passes?

## 2D game reference (Sim94, `anthony3974/Sim94`, C# Windows Forms)
| Feature | 2D behaviour |
|---|---|
| Crops | Wheat, Barley, Oats. Empty, Planted, Stage 1/2/3, Ready; 160 to 205 s in total. |
| Fields | 13 fields with a name, owner and size; you start with one by the house, AI owners plant the rest. |
| Planting | Costs seeds (size x 450 to 550). |
| Fertilizer | Two levels, one per stage, compost (size x 250 to 350) each, +50% yield per level. |
| Harvest | Size x 550 to 650; the harvester adds +60%. |
| Selling | One station per crop, $0.65 to $0.75 per unit. |
| Fields | Buy at size x 1,250; selling back returns the same. |
| Shops | Sids Seed Shop, Franks Fert Shop, Joes Paint Shop, Case Dealer (van or harvester, 5,000), Jerrys Gas Station ($2 per unit). |
| Vehicles | Tractor (speed 8), van (10), harvester (6, +60%); four colours; keys 1 / 2 / 3. |
| Fuel | 100 tank, 1 unit per ~200 movement ticks, empty = game over. |
| Save | Text files; saves on close. Start screen with a name entry. |

No collision in 2D: each field and building is a rectangle, and the player's centre point inside it picks what the key does. Reusable 2D assets: `backround.png` (map, 3500 x 3000, layout reference), vehicle sprites in four colours, `garage.png`, `tittle.png`, the `Saves/*.txt` format.
