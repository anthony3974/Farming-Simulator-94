# Farming Simulator 94: Plan

**When asked "what's next?", the answer is the first open item under NEXT UP** (order set by the owner).
**To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."

Cleaned up on 2026-11-08: finished work is one line each under "Done"; the long notes on it (how each tool worked, test steps, old decisions) are in the git history of this file (`git log -p PLAN.md`, last long version in `356ea6b`). Sound details are in `Assets/Sounds/AUDIO_NOTES.md`.

Unity 6000.0.85f1, Built-in render pipeline. Scenes: `Assets/Scenes/MainMenu.unity` (opens first), `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`. Version 0.6.10. **Releasing:** change only Project Settings > Player > Version; the Android version code is set from it on every Android build (`Assets/Editor/AndroidVersionCode.cs`: major x 10000 + minor x 100 + patch, 0.6.8 -> 608; it was the version without dots, 68, before).

## NEXT UP (in order)

1. **Street lamps across the map (owner, 2026-11-05).** 22 lamps are in GameScene under `Terrain > Street Lamps`, copies of `Assets/Prefabs/Street Lamp.prefab` placed by Tools > Street Lamp Builder (`StreetLampBuilder.cs` + `StreetLampModel.cs`): 15 along the main road (3 of them spares to move by hand) and 7 along the sub road, about 70 m apart. Each has a real Spot `Light` (no shadows, Render Mode Important) and `NightLight.cs` turns it on at night.
   - [x] **Make the lamp bigger (owner, 2026-11-08).** The owner picked 2.5x (8.1 m becomes 20.3 m, Range 18 m becomes 45 m), set by `Lamp Scale` in the builder (default 2.5), and a longer arm (`Head Distance` in the builder, default 10 m from the pole to the head). Intensity is unchanged at 2.5, 120 degree cone.
   - [x] Place lamps along the main road and the sub road (buttons in the builder).
   - [ ] Place lamps at the buildings and the gas station (its own lamps were removed for this).
   - Phones: many real lights are costly (Android Medium has 1 pixel light, the sun's). Few lit lamps, short ranges, no shadows; far lamps could show only the glowing lens. Check the Android frame rate. PC can keep real lights on every lamp. Plan (owner, 2026-11-08):
     - [ ] **Fake light pool on every lamp (biggest saving).** A soft, warm, see-through circle lying on the ground under the lamp, plus the glowing lens it already has. One simple extra draw per lamp, however many lamps there are, and it looks about the same from the driver's seat. Needs fairly flat ground, which the road is, and it won't light up vehicles or walls.
     - [ ] **One real light, on the nearest lamp only.** A small script turns on the real light for the lamp closest to the player and leaves the rest fake. Vehicles still get lit as you pass under a lamp, and the phone only ever pays for one light.
   - The builder stays until the owner is happy with the lamps.
2. **Minimap (owner, 2026-11-05).** A small map in a corner showing the vehicle you drive, the fields and the buildings (a top-down camera into a RenderTexture, or a drawn map image with markers). It reads `GameSettings.Minimap` (the SETTINGS > GAME switch exists already); when off, nothing of it runs (camera disabled).
3. **Error messages (owner, 2026-11-08).** When something can't be done, say why. Most cases already flash a plain message in the centre box, so this is mostly making errors look and sound like errors and filling the gaps.
   - **Already shown:** field not yours (`FieldController.NotYours`); not enough seeds / fertilizer; fertilize an empty field / a ready crop / fully fertilized / already fertilized this stage; the van can't work fields; not enough money (field, seeds, fertilizer, diesel, vehicle); no crop to sell (`Building.Sell`); tank already full; pumping stopped (out of money, tank full, drove off); no vehicle of that kind on the map (dealer).
   - **Gaps (nothing happens now):** E / USE away from any field or building; B / BUY SELL away from a field; harvesting with no bonus (tractor, unfertilized) shows no message, so give it a plain "Harvested 1,234 Wheat"; buying a vehicle you already own (`Dealer.Buy` returns silently; check whether the row is already greyed out); C in the van (maybe fine).
   - **Error sound: done (owner, 2026-11-08).** `ErrorKnock.wav` plays with the "Already shown" failures above, through `ScreenPrompt.Error` and `Building.ShowError` (`GameSounds.Sound.Error`, below money in importance; Tools > Game Sounds Setup fills its slot). A stopped diesel fill keeps its money sound and low fuel warnings stay silent. Check on the phone: the knock is deep and may be thin there.
   - **Decide with the owner:** a shared error look (for example a red or orange border, inside `ScreenPrompt.Error`); the same wording everywhere ("Not enough X. You need N, you have M.", money as "$ 1,234"; the crop sale still says "Profit (1,234$)"); whether E away from everything should say anything (could be noisy).

**Soon (owner, 2026-11-06 / 11-08), after the minimap:**
- **Working lights on the house and garage** (same `NightLight.cs`).
- **Vehicle mirrors.** Side mirrors, maybe a rear-view mirror: a small camera per mirror into a small RenderTexture on the mirror (or a screen overlay; decide). Reads `GameSettings.Mirrors` (switch exists); cameras off when off. Check the Android frame rate.

**Possible later (owner, 2026-11-07); ask before starting:**
- **Trailers.** What for (carry grain, seeds / fertilizer, or looks), bought at the dealer, a hitch (physics joint or a simple follow-the-hitch trailer, steadier on phones), reversing, a capacity limit, saved in SaveGame.
- **More tractors.** New `VehicleKind`s in `VehicleCatalog` plus prefabs (built like Tractor V2); keys 1 / 2 / 3 only cover three (Tab and CAR cycle); the dealer menu needs more rows.

**Distant:** advanced crop harvesting (drive across the field to harvest it; design below).

**Dropped (owner, 2026-11-04):** the paint shop.

## Waiting on the owner (built, not yet tried or decided)
- **TEMPORARY, put back before a release: the game starts at night** for lamp testing (`DayNightCycle.cs:92`). Put back `night = false; part = startDayPart;` when testing is done.
- **Vehicle headlights (owner, 2026-11-08):** built and on the three prefabs, not yet confirmed in the game. `VehicleLights.cs`, set up by the one-off Tools > Vehicle Lights Setup (`VehicleLightsSetup.cs`, delete it once the owner is happy). **Automatic only, no button** ("it's really dark in the nighttime"). On at night, and **only on the vehicle you drive** (owner: parked vehicles stay fully dark, lenses too). The models' own lens parts glow, plus one real forward Spot `Light` per vehicle (no shadows, Important, tune `Headlight Beam` on the prefab). Maybe later: red rear lights.
- **Sun rotation (owner, 2026-11-08):** check in the game that the sun (`Directional Light`) turns over the day. If it is not turning, make it rotate. `DayNightCycle.cs` already rotates it while the cycle is on, and holds it still when SETTINGS > GAME has the cycle off, so check that setting first.
- **Licences of the two engine start clips:** check their download pages (the filenames look like Pixabay's).
- **SETTINGS > GAME tab leftovers:** check the night brightness on the phone; the time of day is not saved (ask if it should be).
- **Empty tank:** the vehicle crawls at 15% (owner: fine for now; may become game over later).
- **Sky and terrain layers:** final for now; one or two may be deleted later.
- **Old history backup:** `../Farming-Simulator-94-mirror-backup.git` (outside the project) can be deleted; the rewritten history was pushed and checked on 2026-11-03.

## Code review findings (2026-11-08)
Whole-project review at 0.6.8: all 36 C# scripts, the scene, prefabs, project settings and the newest APK. No crash or save-corrupting bug found. The game was not run for it, a clean clone was not tried, phone performance was not measured, and the seven Python art scripts were not read. Not ordered against NEXT UP; ask the owner when to do these.

**To fix, most important first:**
- [ ] **A fresh clone is missing its fonts.** `.gitignore:21` excludes `Assets/TextMesh Pro`, and all 169 font references in GameScene point to `LiberationSans SDF` inside it. Text is missing until someone imports TMP Essentials, and the README doesn't say to. Either track the folder or add the step to Getting started.
- [ ] **Measure the lamp cost before placing more lamps.** At 2.5x each lamp lights a circle about 33 m in radius, 6.25 times the area of the 1x design, and everything inside gets an extra draw per lamp (`StreetLampBuilder.cs:559`). 22 lamps are placed now and the driven vehicle's headlight adds one more real light at night. Check the Android frame rate; the phone lighting plan in NEXT UP 1 is the fix if it is too slow.
- [ ] **Walls don't reduce the vehicle's speed.** `Vehicle.Drive` (`Vehicle.cs:185`) keeps `acceleration` while the vehicle is blocked. The engine stays at full pitch, the vehicle leaps away at top speed once it is free, and reversing has to count down from full speed first. Bring `acceleration` down to the ground really covered.
- [ ] **A save can be lost if the app is killed mid-save.** `SaveGame.cs:108` deletes the old file before moving the new one in. A kill between those two lines leaves only `farm.json.tmp`, which Load Game ignores. Use `File.Replace`, and pick up a leftover `.tmp` when loading.
- [ ] **H isn't blocked while menus are open.** `InputManager.cs:192` clears the other game keys but not `help`, so the help panel opens over the pause, dealer and settings menus.
- [ ] **Money is a `float` (`PlayerValues.cs:14`).** Cents start to wobble above about $130,000. Above about $4 million the pump's per-frame charge at 60 fps rounds to nothing, so diesel is free. A `double` fixes it; the save field (`SaveData.money`) has to change too.

**Before publishing:**
- [ ] No release keystore (`androidUseCustomKeystore: 0`), so builds are debug-signed. Google Play needs a release key.
- [ ] **Rename the game to "Ace's Farm" (owner's pick, 2026-11-08).** "Farming Simulator" is GIANTS Software's trademark, and "Farming Simulator 94" looks like an entry in their numbered series (Farming Simulator 22, 25). A web search on 2026-11-08 found no other game called Ace's Farm (the nearest is Fiona's Farm, a puzzle game from a publisher called Ace Games; different enough). Search Google Play once more just before publishing. Places to change:
  - The main menu title: `FARMING SIMULATOR 94` in `Assets/Scenes/MainMenu.unity`.
  - Player Settings > Product Name (`FarmSim943D`), which is also the app's name on the phone. It can be `Ace's Farm` with the apostrophe.
  - The Android package (`com.Tony3974.FarmSim943D`) can't have an apostrophe; use something like `com.Tony3974.AcesFarm`. Set it before the first Google Play upload: it can never change after that.
  - Changing the Product Name or the package moves the save folder (`Application.persistentDataPath`), so test saves from the old name won't load, and on Android the renamed app installs beside the old one (uninstall the old one).
  - The README and PLAN titles, and the comment at the top of `Art/Icon/make_icon.py`. The GitHub repo is also called Farming-Simulator-94 (can be renamed on GitHub; old links redirect).
- [ ] **Field names (`FieldController.FieldNames`).** They came from the 2D game. "John Deer" is one letter off the John Deere trademark, and several others are map and expansion names from GIANTS' games (Felsbrunn, Ravenport, Goldcrest Valley, Erlengrat, Estancia Lapacho, No Man's Land, Alpine Farming). Swap them for made-up names, so Ace's Farm doesn't read as a Farming Simulator fan game. Saves store each field's name, so an old save keeps the old names until a new game.

**Cleanup:**
- [ ] README lines 57 and 61 still list the engine start sound and the zoom-out volume as upcoming; both are done.
- [ ] Unused code: `topSpeed`, `harvestBonus`, `description` and `ForSale` in `VehicleCatalog` (the first two duplicate the prefab values), `Vehicle.SetColor` and `paintRenderers` (the paint shop was dropped).
- [ ] No automated tests, though the test framework is installed. The save and economy rules are the best place to start.

## Smaller open notes
- Sounds not asked for yet: engine shut-down and rev-up, music, ambience, a volume setting.
- Tidy: `Assets/Editor/GameSoundsSetup.cs` and `Assets/Editor/EngineStartSetup.cs` have been run; delete them when no more sounds are added.
- Android: check the touch layout against the field pop-up on phones with a notch / rounded corners (safe area); performance on a phone (terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`); sharing (APK for Android friends, maybe a WebGL build on itch.io); optional icons instead of `<` `>` `GAS` `BRAKE` `USE` labels, haptic feedback.
- Help menu phase 2 (if wanted): icons instead of text, a gamepad layout.
- One E press can act on a field and a building if their trigger zones overlap.
- `PlayerValues` has no null checks on the wheat, barley and oat labels; other scripts besides `FieldController` and `Dealer` have no missing-reference check.
- A scene-set field name that is also in the AI name list is not reserved, so an AI field could get the same name.
- Getting out of the vehicle on foot is not built (ask if wanted).

## Done (newest first)
One line each; tools named "(git `abc1234`)" were run and then removed, and can be brought back from that commit.
- **Engine quieter when zoomed out (2026-11-08):** both loops and the start sound go down to `Far Volume` (40%) at full zoom out, full volume at the default distance and closer (`EngineSound.cs`, `GameCharacter.ZoomOut01`; Show Debug shows it). It follows the zoom, not the pull-in from walls, so the sound doesn't jump near obstacles. Owner: "volume good".
- **Engine start sounds (2026-11-08):** a one-shot on getting in or switching (also when the game begins) that crossfades into the idle loop over its last 55% (`Start Blend`). Tractor and harvester share `Starts/EngineStartDiesel.wav` (harvester pitched 0.85), the van has `Starts/EngineStartVan.wav`; made by `Art/Sounds/make_starts.py`. Owner: "good enough".
- **Help menu PC row for O (2026-11-08):** "Settings (mirrors, minimap, day / night) / O" below the pause row (Tools > Help: PC Settings Row, run by the owner, then deleted; not committed before deletion).
- **Tried by the owner (2026-11-08):** fertilizer colour (blue at level 1, deeper blue at level 2, back to the crop colour at the next stage), camera orbit (full 360, zoom, double-tap recentre, no clipping into walls), Tab = next vehicle on PC. All good.
- **Vehicles no longer float when driven; van mud flaps removed (2026-11-08).** A CharacterController rests 8 cm (Skin Width) above the ground and the tractor's was set 5 cm low, so it rose 11 cm in Play. Tools > Vehicle Ground Fix (git `234fce5`) set each prefab's controller Center Y so the tire rubber sits 1 cm into flat ground with the lugs in it (owner: "lugs in the ground like the van and harvester"): tractor 2.69, van 2.59, harvester 3.49. Run it again after changing a tire size or the tread.
- **Tire tread and rolling wheels (2026-11-08):** V-pattern cube lugs on every tire of all three vehicles (Tire Tread Builder, git `26cd78b`; lug colour `Assets/Vehicles/TireTread.mat`); the tires roll by the distance really driven (`Roll Wheels` on `Vehicle`).
- **Farming sounds (2026-11-08):** planting (shovel scrape, then dirt), harvesting, fertilizing; only on your own fields. Planting remade louder and sharper (version 5, peak -5 dBFS); owner: "planting sound is good".
- **Money and UI sounds (2026-11-07 / 08):** click, menu open / close, money in, money out (`GameSounds.cs`, prefab `Assets/Resources/GameSounds.prefab`). Owner: "sounds are great".
- **SETTINGS > GAME tab and day / night cycle (2026-11-07):** mirrors, minimap, cycle on / off, day and night length (default 10 and 5 min), remembered on the device (`GameSettings.cs`); O opens SETTINGS on PC. `DayNightCycle.cs` on the `Directional Light` (sun, moon, dimmed sky copy; skip to sunset / night from its menu); `NightLight.cs` lights the lamps at night. Settings Game Tab builder: git `baef95c`.
- **First street lamp (2026-11-07):** see NEXT UP 1.
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
