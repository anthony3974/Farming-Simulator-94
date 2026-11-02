# Farming Simulator 94: Improvement List and Initial Plan

> ## >>> NEXT UP: Seed Shop, then Map Border, then Car Dealer, then Home Menu <<<
>
> **When asked "what's next?", the answer is the first unfinished item in this list** (set by the owner, 2026-11-02):
> 1. **Seed shop.** The `Seed Buying` station already works (E buys 10,000 seeds for about $5,000). The owner wants more done here, but the details are not written down yet. **Ask the owner what is wanted** (for example a proper building or model, a new spot, or different prices) before starting.
> 2. **Map border.** Keep the player inside the map. Details not decided yet; ask the owner (invisible walls, a fence or hedge, or something else).
> 3. **Car dealer.** The dealer building and buying vehicles. 2D reference (Case Dealer): van and harvester cost 5,000 each, tractor is the starting vehicle, keys 1 / 2 / 3 switch vehicle, the van is faster, the harvester adds 60% yield, four colours each. In 3D this needs more than one drivable vehicle, so see the `GameCharacter` / `GameCharacterCar` note under "Other things noticed" (split into a base controller plus a vehicle controller, add enter/exit).
> 4. **Home menu.** A start/home screen shown before the game. Details not decided yet; ask the owner (buttons such as Play and Quit, a title, a background). **Also include a version number** shown either on the home menu or on the game screen (owner's request; ask which one, or show it on both). Read it from `Application.version` (Project Settings > Player > Version) so there is one place to change it.
>
> **Moved down the list (not dropped):** fuel and the gas station (2D: diesel tank 100, 1 unit about every 200 movement ticks, 2 dollars per unit, game over at 0), fertiliser and compost, paint shop, dial/touch polish.
>
> **To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."


Unity 6000.0.85f1 (upgraded from 2022.3.55f1). Scene: `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`.
This is an initial plan from reading the code. It is not the plan from the earlier chats. Edit it as you remember more.

## Improvements needed

### Bugs and risks
1. ~~**Overlapping fields.** Pressing E triggers every field the player is inside. Only the nearest or most recently entered field should respond.~~ **DONE**
2. ~~**Cleanup of `isPlayerInside`.** `InputKey` checks `inputManager.Interact` in every field's `Update`. Move the check to the field the player is in.~~ **DONE (only the active field reacts)**
3. ~~**`FieldView.cs` has stray `using` lines** (`System.Drawing`, `UnityEngine.Rendering.DebugUI`, `GridLayoutGroup`). These can cause ambiguous-type errors or build problems. Remove them.~~ **DONE**
4. ~~**`FieldController` imports `Unity.VisualScripting`** without using it. Remove it.~~ **DONE**
5. ~~**Pop-up goes stale.** The field details pop-up only refreshes when the player enters the field. The crop stage doesn't update while the player stands inside, so call `UpdateUI` after every stage change.~~ **DONE**
6. ~~**Harvest order.** In `FieldStageChanger`, `crop` becomes `Dirt` before `HarvestField()` runs. This works now, but it is fragile.~~ **DONE (the crop is read before the field is reset)**
7. ~~**`ReadyCrop` starts a growth timer** (`StartCoroutine(Grow(7))` after reaching Ready) that does nothing useful. Remove it.~~ **DONE**
8. ~~**Local package edits** (`Packages/manifest.json`, `packages-lock.json`) are uncommitted. Check them before committing.~~ **DONE (committed; the deprecated VS Code package was removed)**

### Current bugs*
(*Not a real bug yet, a limit we will hit once there are enough fields. Not numbered so the item numbers below and the references to them stay valid.)
- **Field names run out.** Non-player fields get a random unused name from a list of 12 names taken from the 2D game (`FieldNames` in `FieldController`). Once all 12 are taken, the next field keeps whatever name is typed in the Inspector, which can be blank or a duplicate. Fix: when the list is empty, build a unique name (for example "Dahl Ranch 2"), or add more names. Not urgent: there are only 3 fields now and the number of fields in the final map is not decided.

### Code quality
9. ~~**Replace the long if/else chains** in `FieldStageChanger` and `SetColor` with a crop-type enum plus a stage index (0 to 5), and one colour table (`Color32[,]`).~~ **DONE**
10. ~~**Create 16 materials per field.** Share them across fields, or use a `MaterialPropertyBlock` or colour property instead.~~ **DONE**
11. ~~**Make settings tunable.** The growth time (7 s) and the size divisor (`0.043029259f`) are hard-coded. Expose them as `[SerializeField]` fields.~~ **DONE**
12. ~~**Rename the enum values.** `Crops` mixes crop types (`TypeWheat`) with growth states, so split it into `CropType` and `GrowthStage`.~~ **DONE**
13. ~~**`InputManager`.** Update the `Debug` property name (it shadows `UnityEngine.Debug`) and remove unused stored fields.~~ **DONE** (property renamed to `DebugMode`; every other stored field is used, so none were removed. The F1 toggle still has nothing listening to it).
14. ~~**`GameCharacter` camera clamp** uses Euler angle tricks and a "MIGHT BE A ISSUE LATER" comment. Track pitch as a float and clamp it.~~ **DONE**

### Unfinished features
15. **Fertiliser** (`Fert1`, `Fert2`, `fertStage`): states and colours exist, nothing uses them.
16. ~~**Selling and money.**~~ **DONE** (sell stations for wheat, barley and oat, money in the counters panel, field `value` is the buy price).
17. ~~**Field ownership and buying.**~~ **DONE** (B key to buy or sell, press twice to confirm; sell pays 80%; other fields get random AI owners and names).
18. **Save/load** of crops, stock and money.
19. **Help menu with the controls list.** **Phase 1 DONE (code written, not yet tested in Unity):** `HelpMenu` is a pop-up with a controls table (action, keyboard, touch button) and nothing else (no tips). H opens and closes it; on touch a HELP button sits top-left and tapping the pop-up closes it. Keyboard-only controls (mouse look, Esc, F1) are hidden on phones. Check the HELP button position on a real phone. Phase 2 (if wanted): icons instead of text, a controller or gamepad layout.

## Initial plan (in order)

**Phase 1: Stabilise (small, safe)**
- Confirm the project opens and `GameScene` plays with no Console errors.
- Fix items 3, 4, 7, 5 and 1.
- Commit.

**Phase 2: Refactor `FieldController`**
- Items 9, 10, 11, 12.
- Verify behaviour is unchanged: plant, grow through 4 stages, harvest, stock count goes up.

**Phase 3: Economy**
- ~~Money in `PlayerValues`, sell prices, sell points, money in the UI~~ DONE.
- ~~Field buying and selling~~ DONE.
- ~~Seeds and seed shop~~ DONE.
- Next (owner's order, 2026-11-02): seed shop work, map border, the car dealer, then the home menu (with a version number). After that: fuel and gas station, fertiliser, paint shop (see the 2D checklist below).

**Phase 4: Fields and fertiliser**
- ~~Buying fields~~ DONE (moved into Phase 3).
- Fertiliser: apply during growth to speed up growth or increase yield.

**Phase 5: Polish**
- Save/load (18). (Camera clamp (14) and input cleanup (13) are already done.)

**Phase 6: Vehicle mirrors**
- Add mirrors to the vehicle (left and right side mirrors, maybe a rear-view mirror).
- Likely approach: a small extra camera per mirror drawing into a `RenderTexture`, shown on a quad on the mirror, or a small on-screen panel. Keep the cost low on phones (small texture, lower frame rate or resolution for mirror cameras, off when not needed).
- Decide: real mirrors on the model, or a screen overlay. Check the Android frame rate after adding them.

## Waiting on the owner (decisions)
- **Sky:** keep the pink sky (`Epic_GloriousPink`) or just use the regular blue one. Kept now: `Epic_BlueSunset` (in use), `Day_BlueSky_Nothing`, `Cold Sunset`, `Epic_GloriousPink`.
- **Terrain layers:** which of `Grass_A`, `Grass_Moss`, `Grass_Soil`, `Pebbles_B` and `Pebbles_C` to keep for good (`Grass_B` is in use). Then trim the rest, then decide about the git history cleanup (see Repo housekeeping).
- **Camera:** `staticCamera` is still ticked on `GameCharacter` for testing. Untick it to restore mouse look.
- **Touch layout:** fixed from a phone screenshot (2026-11-02): steering arrows moved right of the speedometer, right-hand buttons shrunk to 130 px so CROP no longer covers the Money/Seeds/Fertilizer panel. Needs a re-check on the phone (not yet seen after the change).

## Open questions
- What was in the original plan from the earlier chats?
- Is multiplayer or an AI helper planned, or is this single-player only?

## Noted from play-testing (all done, see Progress below)
- **Crop type shows "TypeOat" / "TypeWheat".** `FieldController` sends `cropType.ToString()`, which is the enum name. Show "Oat" / "Wheat" / "Barley" instead (and a friendly status such as "Growing 2/3", "Ready to harvest").
- **Field pop-up redesign.** Currently one semi-transparent white panel with 7 plain text objects and no labels. Wanted: dark-gray background, yellow border, coloured labels with white values (for example `Size:` label coloured, value white), possibly a split layout.
- **Camera looks up and gets stuck after leaving and re-entering the Game window.** Causes found in `GameCharacter.RotateY`:
  1. the cursor is never locked, so moving the mouse out of and back into the window gives a huge `Mouse Y` delta in one frame;
  2. the upper limit check `camY < 0 && camY > 350` can never be true, so only the lower limit is enforced;
  3. the clamp works on Euler angles, which wrap at 360.
  Fix: keep a `pitch` float, clamp it (for example -80 to 80), lock the cursor on click and unlock on Escape, and ignore mouse input while unlocked or for the first frame after focus.

## Progress so far
- [x] Baseline pushed and tagged (`baseline-2022.3.55`)
- [x] Upgraded Unity 2022.3.55f1 to 6000.0.85f1; removed unused HDRP/URP packages (project uses the Built-in render pipeline)
- [x] Phase 1 fixes: overlapping fields, stale pop-up, wasted growth timer, unused `using` lines
- [x] "TypeOat" text bug fixed (pop-up now shows Wheat / Barley / Oat / None and "Growing n/3", "Ready!")
- [x] Field pop-up redesign and matching crop-counter panel (dark background, yellow border, gold labels, white values)
- [x] Camera fix: `pitch` float with clamp, cursor lock (Esc releases), mouse ignored on focus change
- [x] `FieldController` refactor: enums + colour table, shared materials, editable `growTime` (now `secondsPerStage`) and `sizeDivisor`, missing-reference check
- [x] Android controls phase 1: `InputManager` takes touch input, `TouchControls` builds on-screen buttons at runtime
- [x] Touch buttons tested in the editor, resized and re-laid out; main Canvas now *Scale With Screen Size* (1920x1080, match 0.5)
- [x] Android Player Settings configured, first APK built and running (tested on an Android VM; not yet on a real phone)
- [x] Phase 3 economy, part 1: money, `Building` script (type + trigger + prompt), sell stations for Wheat, Barley and Oat
- [x] Centre message box (gold border, grows with its text), used for prompts and confirmations
- [x] Crop selection with the C key (plant what you picked; no more random crop)
- [x] Field buying and selling with the B key (confirm on the second press); selling pays 80% and the crop stays on the field
- [x] Field ownership: only fields owned by the player respond to E; other fields get random AI owners and names from the 2D game
- [x] Dial speedometer (bottom-left, 0 to 100 km/h, truck top speed reads 50)
- [x] Touch buttons added for BUY/SELL and CROP (positions temporary)
- [x] **Counters layout:** Wheat / Barley / Oat are in a centred bar along the top of the screen, and the right-hand panel (`Canvas > Statistics`) shows Money, Seeds and Fertilizer (fertilizer shows 0 until fertiliser exists). Built with a one-off editor menu script that is kept only in the git history (`4b30c1c`).
- [x] Seeds and seed shop: planting costs seeds, `Seed Buying` station sells them (E), start with $5,000 and 25,000 seeds; economy rebalanced (see Economy balance). Seeds are finished for now. The one-off setup tool (`AddSeedShop`) is kept only in the git history (`9cff3cd`)
- [x] The player's field starts planted with wheat; growth time raised 35% (9.45 s per stage)
- [x] Fields recreated and resized to a standard starting size by the owner (the scene now has 7 random fields). AI fields now total 11 of the 12 available names, so only one more fits before names run out (see "Field names run out").
- [x] Help menu phase 1: controls list pop-up, H key and a touch HELP button (untested)
- [x] Truck: the front tires turn with the steering button (visual only, `maxSteerAngle` 30 and `steerAnglePerSecond` 120 on `GameCharacter`; untested)
- [x] Field layout: the home field (Tony) is split into four (2 x 2, even gaps). The two facing the house are the player's and the one nearest the house starts planted; the back two are AI. Six random AI fields were added on free ground (owner checked and saved the scene, 2026-11-02). The layout tool is kept only in git history (`bc4629e`).
- [ ] Camera is currently set to **static** for testing (`staticCamera` on `GameCharacter`). Untick it to restore mouse look.

## Android port (notes for revising later)
Phase 1 is done (see above). Still to do:
1. ~~**Test the touch buttons in the editor.**~~ DONE. Tick `Show Touch Controls` on `InputManager`, check layout, drive, and plant/harvest with USE. Adjust button sizes/positions from screenshots.
2. ~~**Make the existing UI scale.**~~ DONE. The scene Canvas uses *Constant Pixel Size* (reference 800x600), so the counters and field pop-up will be the wrong size on phones. Switch it to *Scale With Screen Size* (for example 1920x1080, match 0.5) and re-check both panels.
3. (Partly done: no overlaps at 1920x1080.) **Check the touch layout against the pop-up.** GAS / BRAKE / USE sit above the bottom-right pop-up (260 px tall + 20 px margin); verify on a real screen, including phones with a notch or rounded corners (safe area).
4. **Touch look.** When `staticCamera` is turned off again, add a drag-to-look area (the cursor is not locked in touch mode, so `GameCharacter` mouse look does not apply on a phone).
5. ~~**Player Settings for Android:**~~ DONE (landscape, API 23+, IL2CPP, ARM64, package name).
   - minimum API level is 22 but Unity 6 needs 23 or higher; set it deliberately
   - force landscape orientation
   - IL2CPP + ARM64 for release builds
   - set a proper package name and app name (the default is a placeholder)
6. ~~**Install Android Build Support**~~ DONE. (Unity Hub > Installs > gear on 6000.0.85f1 > Add modules: Android Build Support, OpenJDK, Android SDK & NDK Tools). Close the editor while it installs.
7. **Performance on a phone.** Likely cost: terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`. Test and lower the quality settings for the Android quality level if needed.
8. (APK built and runs on an Android VM; still to do on a real phone.) **First device build.** Build an APK, install it on a phone (enable "install unknown apps"), then test controls, UI size and frame rate.
9. **Sharing options.** APK only works for Android friends. A WebGL build on itch.io would be easier for friends with a PC (just a link), but has its own limits.
10. **Optional later:** replace `<` `>` `GAS` `BRAKE` `USE` text labels with icons; haptic feedback; a pause/settings button.

## Other things noticed (not yet scheduled)
- `InputManager.Debug` hides `UnityEngine.Debug`; rename it (item 13, still open).
- `fertStage` in `FieldController` is never set (compiler warning). It will be used when fertiliser is built.
- `PlayerValues` counters and `FieldView` references are set by hand in the Inspector; the new missing-reference check in `FieldController` helps, but the other scripts have no such check.
- `GameCharacter` and `GameCharacterCar` are two halves of one partial class with walking and driving mixed together. When the harvester is added, consider splitting into a base controller plus a vehicle controller, and add enter/exit-vehicle.
- Fields are not planted or harvested by the AI owners (they only get a name and owner); decide later if they should.
- Touch layout: overlaps fixed (see Waiting on the owner), re-check on the phone. BUY/SELL and CROP positions are still not final.

## Economy balance (set 2026-11-02, owner may revise)
In 3D, a field's `size` is also its harvest amount (about 4,900 to 23,300), not the 2D size of 1 to 3.5, so the 2D prices per size could not be used directly (planting the 23,300 field wanted 11 million seeds). All prices are now per unit of harvest, keeping the 2D ratios:
- Seeds per planting: 0.75 to 0.9 x harvest (2D: 500 per 600). Starter field "Tony" (23,300) needs about 17,500 to 21,000 seeds.
- Seed shop: 10,000 seeds for 4,500 to 5,500 dollars (same 0.5 dollars per seed as 2D; 1,000 would mean 20 trips).
- Selling: 0.65 to 0.75 dollars per unit (unchanged, same as 2D). Tony pays about 16,000 per harvest, so about 6,000 profit after buying the seeds.
- Land: 2.1 dollars per unit of harvest (2D: 1,250 per 600), so Tony is about 49,000 (was 11,650) and the smallest field about 10,000. Selling back pays 80%.
- Start: 5,000 dollars (money used to start at 0) and 25,000 seeds.
- **Time:** growth was 4 x 7 s = 28 s; the 2D game took 160 to 205 s. It is now 4 x 9.45 s (7 s + 35%) = 37.8 s per crop, set by `secondsPerStage` on `FieldController` (renamed from `growTime`, so the old 7 saved in the scene no longer applies). Still about 4.5x the 2D pace, so raise it further if it feels too fast.
- **Starting field:** the player's own field ("Tony") starts the game planted with wheat, in the Planted stage (`startCrop` on `FieldController`; None = empty). The owner had to sell it to afford the first planting, so it now starts growing for free.

## Code review notes (`/code-review high Assets/Scripts`, 2026-11-02)
Fixed (untested): stale B-key confirmation, building prompt overwriting other messages and rebuilding every frame, speedometer text rebuilt every frame, seed price rerolled on every E press, touch button null checks. Left for later (minor, nothing triggers them today):
- One E press can act on a field and a building if their trigger zones overlap (each keeps its own "last entered" list).
- `GameCharacter.Move()` (walking mode, `drive` off) strafes along `transform.right` instead of `characterLocation.transform.right`.
- `PlayerValues` has no null checks on the wheat, barley and oat labels.
- A field's growth coroutine stops if the field is ever disabled and nothing restarts it.
- A scene-set field name that is also in the AI name list is not reserved, so an AI field could get the same name.

## Phase 3 design decisions (economy)
- **Selling needs a drive.** The player must drive to a sell point to sell crops (not instant selling). Interaction uses the USE button / E key, same as fields.
- **Sell point: the `Wheat Selling Station`** (object `Wheat Selling` in the scene; decided). The other unused buildings (`House`, `Garage EX`) can become the seed shop, fertilizer shop, dealer or paint shop later.
- **Map:** the old 2D map is not being copied. The 3D map is being laid out fresh, so sell point placement is open.
- **Background:** the game was finished as a 2D version before. The 3D version should aim for the 2D feature set. Open question: which features did the 2D version have (prices, upgrades, vehicles, buying land)? Use it as the feature checklist (see the 2D analysis below).
- **Money:** add money to `PlayerValues` and the counters panel; sell prices per crop; maybe price changes over time.

## Repo housekeeping
- [x] Android build output is no longer tracked (committed and pushed); `apk/` and `*_BurstDebugInformation_DoNotShip/` are in `.gitignore`.
- [x] **UserSettings stays tracked**, and `UserSettings/Layouts/` (the window layouts) is now tracked too, so the editor layout follows the repo.
- [x] **`AllSkyFree` trimmed** from 321 MB to 37 MB (commit `ee878d3`). Kept 5 skyboxes, each with only its material and six face images: `Epic_BlueSunset` (the one in use), `Day_BlueSky_Nothing` (in the `Cartoon Base BlueSky` folder), `Cold Sunset`, `Cartoon Base NightSky` and `Epic_GloriousPink`. Still to decide: keep the pink sky or just use the regular blue one.
- [x] **Terrain texture cleanup (done in the working tree, commits `3d7c62f`, `061efda`, `3883848`).** Removed the sample heightmaps and detail textures and the layers Snow, Sand, Rock, Tidal_Pools, Soil_Rocks, Muddy, Heather, Grass_Dry, Black_Sand and Pebbles_A. `Assets/TerrainSampleAssets/Textures` went from 1.9 GB to 505 MB. Kept layers: `Grass_B` (in use on the terrain), `Grass_A`, `Grass_Moss` (possible extra grass), `Grass_Soil` (dirt path contender), `Pebbles_B` (dirt path contender) and `Pebbles_C` (likely gravel). Still to decide: which of these to keep for good, then trim the rest. The terrain data (`Assets/Terrain/New Terrain.asset`, binary) was checked for each removed layer before deleting.
- **Git history cleanup (to do, later).** The files above are gone from the working folder, but the **`.git` folder is still about 2 GB**, because the old versions stay in the history. A fresh clone downloads all of it. Shrinking it means rewriting history, which is the risky part:
  1. Finish choosing the terrain layers and skyboxes first, so the cleanup is done once.
  2. Make a backup copy of the whole repo folder (or a `git clone --mirror`) before anything else.
  3. Install `git-filter-repo` (not installed yet) and run it to drop the deleted paths from all commits: `Assets/TerrainSampleAssets/Textures/Heightmaps` and `Details`, the removed terrain layers, the removed `AllSkyFree` folders, and the old Android build output if it is still in the history.
  4. Re-add the `origin` remote (`filter-repo` removes it) and **force push**. Every commit hash changes, so the commit numbers in this plan become out of date.
  5. The tag `baseline-2022.3.55` must be re-created or re-pushed, because it points at an old commit.
  6. Any other copy or clone of the repo (a laptop, a friend's copy) must be deleted and cloned again. GitHub may keep the old data for a while; contacting GitHub support removes it for sure.
  7. Open the project in Unity afterwards and check that nothing is missing.
  - Difficulty: moderate. It is a few commands, but it cannot be undone after the force push, so the backup matters. Not needed to keep working. Only worth it if clone size matters.
  - Git LFS only makes sense for large files we keep.
- Ask before shrinking anything.

## Next up
- **Seed shop, then the map border, then the car dealer, then the home menu with a version number** (see NEXT UP at the top). After those: fuel and the gas station, fertiliser, paint shop.
- **2D version analysis:** done, see the section below.

## 2D game analysis (Sim94, C# Windows Forms, ~1,800 lines in one file)
Source: `anthony3974/Sim94`. Everything below was read from the code, not played.

**Goal of the 2D game:** buy many fields, take care of them, harvest, sell, and use the money to buy and paint vehicles.

### What the 2D game has (checklist for the 3D version)
| Feature | 2D behaviour |
|---|---|
| Crops | Wheat, Barley, Oats. Stages: Empty, Planted (about 30 s), Stage 1/2/3, Ready. Total 160 to 205 s. Stage 1 takes 40% of the random time, stage 2 35%, stage 3 25%. |
| Fields | 13 fields, each with a name, an owner and a size (for example 3.35, 1.41, 1.92). You start owning field 19 by the house. The other 12 belong to random AI names and are planted automatically. |
| Planting | Press the interact key in your own empty field, then J / K / L for wheat / barley / oats. Costs seeds (size x 450 to 550). |
| Fertilizer | Two levels, one per growth stage. Costs compost (size x 250 to 350) each time. Each level adds +50% yield. |
| Harvest | Yield = size x 550 to 650. The **harvester** adds another +60%. |
| Selling crops | At one **selling station per crop** (Wheat, Barley, Oat). Pays 0.65 to 0.75 dollars per unit and empties your stock. |
| Buying fields | Cost = size x 1,250. Selling a field back returns the same amount (no loss); an AI owner takes it and replants. |
| Shops | **Sids Seed Shop** (1,000 seeds for 450 to 550 dollars), **Franks Fert Shop** (1,000 compost for 400 to 500), **Joes Paint Shop** (repaint a vehicle, 1,750), **Case Dealer** (buy van or harvester, 5,000 each), **Jerrys Gas Station** (diesel, 2 dollars per unit). |
| Vehicles | Tractor (speed 8, start), Van (speed 10, fast travel and selling), Harvester (speed 6, +60% yield). Four colours each (red, yellow, blue, green). Keys 1 / 2 / 3 switch vehicle. |
| Fuel | Diesel tank of 100. Moving uses 1 unit about every 200 movement ticks. At 0 it is game over. |
| Start values | 5,000 dollars, 100 diesel, 2,000 seeds, 1,000 each of wheat / barley / oats, 1,000 compost. |
| Save / load | Text files: fields, owners, resources, colours. Also saves on close. |
| Start screen | Name entry (used for the house address), Start (new game) and Load. |

### How the 2D game does "hitboxes"
There is **no collision**; the README says so ("there is no collision yet"). The only "hitbox" is a rectangle for each field and building. Each frame, the game checks whether the player's centre point is inside one of the rectangles and sets `isIn`. Then the key press (interact, fertilize, harvest, buy, sell) does what that place's type says. Buildings have a `Business` type: Selling, Buying, Painting, Dealing, Gas.

### What this means for the 3D version
- The 3D fields already work the same way: a trigger collider plus `fieldsInside`. **Buildings should use the same pattern**: a `Building` component with a trigger collider and a type enum (Sell, Buy, Paint, Dealer, Gas). The `USE` button and E key then do what the building says.
- **Economy numbers to reuse** (they were balanced for the 2D game): the prices above. Keep them as editable Inspector fields, not hard-coded.
- **Selling needs a drive** (already decided). The 2D game had one station per crop; for the first 3D version one sell point that buys all crops is enough.
- **Now in the 3D game:** money, sell stations, field ownership, buying and selling fields, seeds and the seed shop.
- **Still not in the 3D game:** compost, diesel, vehicle switching, painting, dealer, save/load, AI owners planting fields.
- **The harvester already exists in 2D as art** (`h/` folder, four colours). In 3D it still needs a model.
- **Suggested order for Phase 3:** ~~(1) money, (2) a sell building, (4) field buying~~ done; (3) seeds and a seed shop, ~~done~~; the owner now wants the dealer (7) before fuel (5) and fertilizer (6).

### Assets in the 2D repo that could be reused
- `backround.png` (3500x3000), the full map. A layout reference only; the 3D map is built fresh.
- Tractor, van and harvester sprites (four colours each) and `garage.png`, `tittle.png` (title screen).
- `Read Me.txt`, `Saves/*.txt` (save format).

