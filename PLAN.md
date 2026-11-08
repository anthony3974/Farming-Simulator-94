# Farming Simulator 94: Plan

**When asked "what's next?", the answer is the first open item under NEXT UP** (order set by the owner).
**To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."

Cleaned up on 2026-11-08: finished work is one line each under "Done"; the long notes on it (how each tool worked, test steps, old decisions) are in the git history of this file (`git log -p PLAN.md`, last long version in `356ea6b`). Sound details are in `Assets/Sounds/AUDIO_NOTES.md`.

Unity 6000.0.85f1, Built-in render pipeline. Scenes: `Assets/Scenes/MainMenu.unity` (opens first), `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`. Version 0.6.8.

## NEXT UP (in order)

1. **Sounds (in progress).** Done and in the game (`GameSounds.cs`): button click, menu open / close, money in (double desk bell), money out (deep bell), planting, harvesting, fertilizing (one-shots, made by `Art/Sounds/make_*.py`). **Next: engine start sound (owner, 2026-11-08).** Today the engine loop just fades in when you get in (`EngineSound.cs`). Play a short starter crank + catch when you get into or switch to a vehicle, then hand over to the idle loop; maybe a shut-down sound when you get out. One sound per vehicle, or a shared one pitched per vehicle. **Owner: the sounds come from the same place as the engine loops ("Sound FX Engine", owner thinks;** the current files are `freesound_community-...`). Check the licence of each pick, then cut and level them like the loops (`Art/Sounds/make_loops.py`).
   - Not asked for yet: engine shut-down and rev-up, music, ambience, a volume setting.
   - Tidy: `Assets/Editor/GameSoundsSetup.cs` has been run; delete it when no more sounds are added to the prefab.
2. **Street lamps across the map (owner, 2026-11-05).** One lamp is built (`Assets/Prefabs/Street Lamp.prefab`, `Street Lamp 1` under `Terrain > Street Lamps`, by Tools > Street Lamp Builder: `StreetLampBuilder.cs` + `StreetLampModel.cs`). It has a real Spot `Light` (no shadows, Render Mode Important) and `NightLight.cs` turns it on at night.
   - [ ] **Make the lamp bigger (owner, 2026-11-08).** At 8.1 m it is shorter than the house. Scale it up (the owner picks the size: clearly taller than the house roof) with one number in the builder (`Lamp Scale`). A `Light`'s Range ignores scale, so the tool multiplies Range (18 m at 1x) by the same factor; keep the 120 degree cone and check Intensity by eye so the ground is as bright as before.
   - [ ] Then place lamps along the roads (also in the builder), at the buildings and the gas station (its own lamps were removed for this).
   - Phones: many real lights are costly (Android Medium has 1 pixel light, the sun's). Few lit lamps, short ranges, no shadows; far lamps could show only the glowing lens. Check the Android frame rate.
   - The builder stays until the owner is happy with the lamps.
3. **Minimap (owner, 2026-11-05).** A small map in a corner showing the vehicle you drive, the fields and the buildings (a top-down camera into a RenderTexture, or a drawn map image with markers). It reads `GameSettings.Minimap` (the SETTINGS > GAME switch exists already); when off, nothing of it runs (camera disabled).
4. **Error messages (owner, 2026-11-08).** When something can't be done, say why. Most cases already flash a plain message in the centre box, so this is mostly making errors look and sound like errors and filling the gaps.
   - **Already shown:** field not yours (`FieldController.NotYours`); not enough seeds / fertilizer; fertilize an empty field / a ready crop / fully fertilized / already fertilized this stage; the van can't work fields; not enough money (field, seeds, fertilizer, diesel, vehicle); no crop to sell (`Building.Sell`); tank already full; pumping stopped (out of money, tank full, drove off); no vehicle of that kind on the map (dealer).
   - **Gaps (nothing happens now):** E / USE away from any field or building; B / BUY SELL away from a field; harvesting with no bonus (tractor, unfertilized) shows no message, so give it a plain "Harvested 1,234 Wheat"; buying a vehicle you already own (`Dealer.Buy` returns silently; check whether the row is already greyed out); C in the van (maybe fine).
   - **Decide with the owner:** a shared error look (for example a red or orange border, `ScreenPrompt.Error(text)` next to `Flash`); a short error sound (a soft low "bonk", `GameSounds.Sound.Error`, below money in importance); the same wording everywhere ("Not enough X. You need N, you have M.", money as "$ 1,234"; the crop sale still says "Profit (1,234$)"); whether E away from everything should say anything (could be noisy).

**Soon (owner, 2026-11-06 / 11-08), after the minimap:**
- **Tractor floats when driving (owner, 2026-11-08): fix written, owner to run.** The ground check (Tools > Debug: Vehicle Ground Check) showed nothing odd under it: in Play the tractor stood at y 0.130 against 0.019 where it was placed, because a CharacterController rests Skin Width (0.08 m) above the ground and the tractor's controller bottom was 5 cm below its origin. **Owner:** run **Tools > Vehicle Ground Fix** (`Assets/Editor/VehicleGroundFix.cs`, changes `Tractor.prefab`, `Van.prefab`, `Harvester.prefab`; no scene save needed). First run (2026-11-08) set the lowest lug 1 cm into flat ground and **removed the van's mud flaps** (`Mud Flap L` / `R`). **Owner then: "I want the tractor lugs in the ground like it is for the van and harvester"** (as they look parked), so the tool now sets each controller's Center Y so the tires' rubber sinks 1 cm into flat ground and the lugs are in it. **Run Tools > Vehicle Ground Fix again.** Then Play, drive each vehicle, look at the tires; if good, commit the prefabs and delete `VehicleGroundFix.cs` and `VehicleGroundCheck.cs`. Still possible later: real falling gravity (also for parked vehicles), a flatter controller under the middle of the wheels (it is a ball now, so bumps and slopes can still lift it), tilting the model with the ground.
- **Vehicle headlights (owner, 2026-11-08).** On at night with `NightLight.cs` like the lamps; **automatic only, no button** ("it's really dark in the nighttime"). Two glowing lenses on each vehicle; only the vehicle you drive gets one real forward Spot `Light` (no shadows, short range, Important). Maybe red rear lights and a work light on the tractor and harvester. Built into the three prefabs by a one-off tool.
- **Working lights on the house and garage** (same `NightLight.cs`).
- **Engine quieter when the camera is zoomed out (owner, 2026-11-08).** The engine is a 2D sound, so the zoom (`GameCharacter`, `minZoom` 0.4 to `maxZoom` 2.5) does not change it. Multiply both loops (and the start sound) by a zoom factor: full volume at the default distance and closer, down to `Far Volume` (about 40%) at full zoom out, eased. Settings on each Engine Sound; Show Debug shows the factor. (A 3D sound would pan as the camera orbits; ask the owner if that is wanted.)
- **Vehicle mirrors.** Side mirrors, maybe a rear-view mirror: a small camera per mirror into a small RenderTexture on the mirror (or a screen overlay; decide). Reads `GameSettings.Mirrors` (switch exists); cameras off when off. Check the Android frame rate.

**Possible later (owner, 2026-11-07); ask before starting:**
- **Trailers.** What for (carry grain, seeds / fertilizer, or looks), bought at the dealer, a hitch (physics joint or a simple follow-the-hitch trailer, steadier on phones), reversing, a capacity limit, saved in SaveGame.
- **More tractors.** New `VehicleKind`s in `VehicleCatalog` plus prefabs (built like Tractor V2); keys 1 / 2 / 3 only cover three (Tab and CAR cycle); the dealer menu needs more rows.

**Distant:** advanced crop harvesting (drive across the field to harvest it; design below).

**Dropped (owner, 2026-11-04):** the paint shop.

## Waiting on the owner (built, not yet tried or decided)
- **Fertilizer colour:** fertilizing turns the field light blue (100, 185, 235) at level 1, deeper blue (80, 155, 235) at level 2, until its next growth stage (`FieldController.CurrentColor()`; saved). Owner: check it turns blue and back.
- **Camera orbit (built 2026-11-07):** full 360 orbit around the vehicle, zoom (wheel / pinch), double-tap recentres on touch, sphere cast so it does not clip into walls. Owner: test on PC and the phone; check the help rows' text fits.
- **Tab = next vehicle on PC (2026-11-07):** like the CAR button; "You only own the tractor" if there is no other. Not yet tried.
- **SETTINGS > GAME tab leftovers:** the help menu's PC panel has no row for O (SETTINGS) yet; check the night brightness on the phone; the time of day is not saved (ask if it should be).
- **Empty tank:** the vehicle crawls at 15% (owner: fine for now; may become game over later).
- **Sky and terrain layers:** final for now; one or two may be deleted later.
- **Old history backup:** `../Farming-Simulator-94-mirror-backup.git` (outside the project) can be deleted; the rewritten history was pushed and checked on 2026-11-03.

## Smaller open notes
- Android: check the touch layout against the field pop-up on phones with a notch / rounded corners (safe area); performance on a phone (terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`); sharing (APK for Android friends, maybe a WebGL build on itch.io); optional icons instead of `<` `>` `GAS` `BRAKE` `USE` labels, haptic feedback.
- Help menu phase 2 (if wanted): icons instead of text, a gamepad layout.
- One E press can act on a field and a building if their trigger zones overlap.
- `PlayerValues` has no null checks on the wheat, barley and oat labels; other scripts besides `FieldController` and `Dealer` have no missing-reference check.
- A scene-set field name that is also in the AI name list is not reserved, so an AI field could get the same name.
- Getting out of the vehicle on foot is not built (ask if wanted).

## Done (newest first)
One line each; tools named "(git `abc1234`)" were run and then removed, and can be brought back from that commit.
- **Tire tread and rolling wheels (2026-11-08):** V-pattern cube lugs on every tire of all three vehicles (Tire Tread Builder, git `26cd78b`; lug colour `Assets/Vehicles/TireTread.mat`); the tires roll by the distance really driven (`Roll Wheels` on `Vehicle`).
- **Farming sounds (2026-11-08):** planting (shovel scrape, then dirt), harvesting, fertilizing; only on your own fields.
- **Money and UI sounds (2026-11-07 / 08):** click, menu open / close, money in, money out (`GameSounds.cs`, prefab `Assets/Resources/GameSounds.prefab`). Owner: "sounds are great".
- **SETTINGS > GAME tab and day / night cycle (2026-11-07):** mirrors, minimap, cycle on / off, day and night length (default 10 and 5 min), remembered on the device (`GameSettings.cs`); O opens SETTINGS on PC. `DayNightCycle.cs` on the `Directional Light` (sun, moon, dimmed sky copy; skip to sunset / night from its menu); `NightLight.cs` lights the lamps at night. Builder: git `f640005`'s parent.
- **First street lamp (2026-11-07):** see NEXT UP 2.
- **Fuel and the gas station (2026-11-06):** one shared 100 L tank, 10 L per km, Jerrys Gas Station fills at 4 L/s for $2 / L (E / USE starts and stops), fuel gauge above the speedometer, low-fuel warning at 20% (Gas Station Builder git `ad4b1a4`, Fuel Gauge Builder git `d7abd45`).
- **AI owners work their own fields (2026-11-06):** random start states, they plant, sometimes fertilize and harvest (`FieldController.AiWork`, settings under "AI owners").
- **Touch controls rework (2026-11-06):** buttons are scene objects; SETTINGS panel picks steering (`< >`, wheel, slider), pedals (buttons or a pedal slider, 80% gas / 20% brake) and square or rounded buttons (`TouchSettings.cs`); held buttons sink; sliding off and back on works; FERT button removed, USE fertilizes (Build Touch Controls git `a94cdbb`).
- **Save / load (2026-11-06):** `SaveGame.cs`, `farm.json` in `persistentDataPath`; saves every 60 s, on pause, main menu and app close. Saves money, stock, fuel, selected crop, every field (matched by hierarchy path), owned vehicles and their places. Pressing Play in GameScene directly starts a new farm and its autosave replaces the editor's save.
- **Engine sound (2026-11-05):** idle + drive loops per vehicle, pitch follows speed, only the driven vehicle is heard (`EngineSound.cs`, loops in `Assets/Sounds/Loops`; Engine Sound Setup git `d58093e`).
- **App icon (2026-11-05):** red tractor on a field (`Art/Icon/make_icon.py`; App Icon Setter git `77c80f2`; `Art/Icon/PlayStoreIcon.png` for the store).
- **Harvester redesign (2026-11-05):** 122-part combine, steers with the back wheels (Harvester Builder V2 git `0c610f7`).
- **Speeds cut 10% (2026-11-05):** tractor 45, van 56, harvester 34 km/h; braking 15% stronger. Tractor camera moved back to `(0, 5.36, -8.5)`.
- **Tractor redesign (2026-11-04):** connected Model V2 (Tractor Builder V2 git `1cfcae2`).
- **UI moved into the scene (2026-11-04):** help menu (git `58fad2d`), dealer menu (git `47e5a43`), message box and speedometer (HUD Builder git `8fdfbc3`; run again to redraw the dial).
- **Look around on phones (2026-11-04):** drag to look (Build Touch Look Area git `58931f8`).
- **Main menu and pause menu (2026-11-04):** `MainMenu` scene with PLAY > NEW GAME / LOAD GAME, TERMS OF SERVICE (does nothing yet; link it when publishing), version number; pause menu with RESUME and MAIN MENU (Main Menu Builder git `8ae0ee6`, Pause Menu Builder git `a4b97f9`). To change the background: the **Texture** of `Main Menu > Background > Screenshot`.
- **UI Button prefab (2026-11-04):** every button is a copy (git `d203505`).
- **Grass looked like water on phones (fixed 2026-11-04):** terrain layers `Grass_B` and `Pebbles_B` set to Smoothness Source: Constant (0).
- **Car dealer and vehicles (2026-11-03):** red L-shaped dealer top-left; van and harvester $5,000 each, parked by the house; keys 1 / 2 / 3, CAR button; the van can't work fields but can buy and sell them (Dealer Builder git `3340302`, Vehicle Prefabs git `530c871`).
- **Map border (2026-11-03):** 572 concrete blocks 1 m inside the terrain edge (Map Border Builder git `9f800c5`).
- **Buildings (2026-11-03):** tractor model (git `8c1c8b6`), grain elevator sell stations (git `d60593b`), seed shop and new fertilizer shop (git `10298e7`), house (git `b572742`), garage (git `e605aa2`), road look (git `48d5ead`), field layout (git `d5b10c0`), counters layout (git `bb1cd99`), seed shop setup (git `2632790`).
- **Fertilizer (2026-11-03):** two levels, one per growth stage, +50% harvest each; E / USE on a growing field (the F key was removed 2026-11-06).
- **Economy:** sell stations per crop, money, field buying and selling (B, press twice; sell pays 80%), seeds and shops, AI owners with random names (numbered once the 12 names run out).
- **Early fixes (2026-10-31 to 11-03):** Unity 6 upgrade, field pop-up redesign, "TypeOat" text, camera pitch clamp and cursor lock, `FieldController` refactor, reverse steering, crop selection with C, help menu, dial speedometer, Android build running on a real phone (landscape, API 23+, IL2CPP, ARM64).
- **Repo housekeeping (2026-11-03):** history rewritten to start at the Unity 6 upgrade (`.git` 2 GB to 527 MB), skyboxes trimmed to 4, terrain layers trimmed to 6, `UserSettings/`, `.utmp/` and Android build output untracked. Ask before shrinking anything else.

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
