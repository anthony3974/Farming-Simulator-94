# Farming Simulator 94: Improvement List and Initial Plan

> ## >>> NEXT UP: Wheat Selling Station (the first sell point) <<<
>
> **When asked "what's next?", the answer is this task.** It is the first step of Phase 3 (economy).
>
> **The building:** the **Wheat Selling Station**. In `GameScene` it is the object `Wheat Selling` (Hierarchy: Terrain > Buildings > Wheat Selling, position about 923, 13, 63). It already has a *solid* `BoxCollider` (not a trigger), so vehicles collide with it. **It has no trigger zone yet. The owner will create that trigger/collision area themselves.**
>
> **Plan for this task**
> 1. *(Owner)* Add the trigger zone: a child object of the building with a `BoxCollider` ticked **Is Trigger**, placed and sized where the truck should stand to sell (in front of the building).
> 2. *(Claude)* Add a `Building` script with a type (Sell, Buy, Paint, Dealer, Gas) and a trigger. It copies the pattern in `FieldController` (the `fieldsInside` list, so only the building the player is in reacts, and the E key / USE button run its action). The Wheat Selling Station is type Sell, crop Wheat.
> 3. *(Claude)* Add money to `PlayerValues` and show it in the counters panel (same style as the other lines).
> 4. *(Claude)* Sell action: pressing E / USE inside the trigger sells **all stored wheat** for money and empties the wheat counter. Price starts at the 2D game's value, **0.65 to 0.75 dollars per unit** (random each sale), exposed as Inspector fields. Show a short message like "Wheat Sold, Profit (xxx$)" (the 2D game showed one).
> 5. *(Both)* Test: harvest wheat, drive to the station, press USE, check the money.
>
> **Open choice:** the 2D game had one station per crop (Wheat, Barley, Oat). For now only the Wheat Selling Station exists, so only wheat sells there. Barley and oats need their own stations (or one station that buys everything) later.
>
> **To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."


Unity 6000.0.85f1 (upgraded from 2022.3.55f1). Scene: `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`.
This is an initial plan from reading the code. It is not the plan from the earlier chats. Edit it as you remember more.

## Improvements needed

### Bugs and risks
1. **Overlapping fields.** Pressing E triggers every field the player is inside. Only the nearest or most recently entered field should respond.
2. **Cleanup of `isPlayerInside`.** `InputKey` checks `inputManager.Interact` in every field's `Update`. Move the check to the field the player is in.
3. **`FieldView.cs` has stray `using` lines** (`System.Drawing`, `UnityEngine.Rendering.DebugUI`, `GridLayoutGroup`). These can cause ambiguous-type errors or build problems. Remove them.
4. **`FieldController` imports `Unity.VisualScripting`** without using it. Remove it.
5. **Pop-up goes stale.** The field details pop-up only refreshes when the player enters the field. The crop stage doesn't update while the player stands inside, so call `UpdateUI` after every stage change.
6. **Harvest order.** In `FieldStageChanger`, `crop` becomes `Dirt` before `HarvestField()` runs. This works now, but it is fragile.
7. **`ReadyCrop` starts a growth timer** (`StartCoroutine(Grow(7))` after reaching Ready) that does nothing useful. Remove it.
8. **Local package edits** (`Packages/manifest.json`, `packages-lock.json`) are uncommitted. Check them before committing.

### Code quality
9. **Replace the long if/else chains** in `FieldStageChanger` and `SetColor` with a crop-type enum plus a stage index (0 to 5), and one colour table (`Color32[,]`).
10. **Create 16 materials per field.** Share them across fields, or use a `MaterialPropertyBlock` or colour property instead.
11. **Make settings tunable.** The growth time (7 s) and the size divisor (`0.043029259f`) are hard-coded. Expose them as `[SerializeField]` fields.
12. **Rename the enum values.** `Crops` mixes crop types (`TypeWheat`) with growth states, so split it into `CropType` and `GrowthStage`.
13. **`InputManager`.** Update the `Debug` property name (it shadows `UnityEngine.Debug`) and remove unused stored fields.
14. **`GameCharacter` camera clamp** uses Euler angle tricks and a "MIGHT BE A ISSUE LATER" comment. Track pitch as a float and clamp it.

### Unfinished features
15. **Fertiliser** (`Fert1`, `Fert2`, `fertStage`): states and colours exist, nothing uses them.
16. **Selling and money.** Crops only accumulate as counters, and field `value` is never used.
17. **Field ownership and buying.** `ownerName` and `value` are displayed but there is no way to buy a field.
18. **Save/load** of crops, stock and money.

## Initial plan (in order)

**Phase 1: Stabilise (small, safe)**
- Confirm the project opens and `GameScene` plays with no Console errors.
- Fix items 3, 4, 7, 5 and 1.
- Commit.

**Phase 2: Refactor `FieldController`**
- Items 9, 10, 11, 12.
- Verify behaviour is unchanged: plant, grow through 4 stages, harvest, stock count goes up.

**Phase 3: Economy**
- Add a money value to `PlayerValues`, plus sell prices per crop.
- Add a sell point (for example a building or barn trigger).
- Show money in the UI.

**Phase 4: Fields and fertiliser**
- Buying fields (uses `ownerName` and `value`).
- Fertiliser: apply during growth to speed up growth or increase yield.
- **Advanced crop harvesting:** drive-over harvest cells with bonuses (see "Advanced crop harvesting" below).

**Phase 5: Polish**
- Save/load (18), camera clamp (14), input cleanup (13).

## Open questions
- What was in the original plan from the earlier chats?
- Is multiplayer or an AI helper planned, or is this single-player only?

## Noted from play-testing (to do, not yet started)
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
- [x] `FieldController` refactor: enums + colour table, shared materials, editable `growTime` and `sizeDivisor`, missing-reference check
- [x] Android controls phase 1: `InputManager` takes touch input, `TouchControls` builds on-screen buttons at runtime
- [x] Touch buttons tested in the editor, resized and re-laid out; main Canvas now *Scale With Screen Size* (1920x1080, match 0.5)
- [x] Android Player Settings configured, first APK built and running (tested on an Android VM; not yet on a real phone)
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
- `InputManager.Debug` hides `UnityEngine.Debug`; rename it.
- `fertStage` in `FieldController` is never set (compiler warning). It will be used when fertiliser is built.
- `PlayerValues` counters and `FieldView` references are set by hand in the Inspector; the new missing-reference check in `FieldController` helps, but the other scripts have no such check.
- Package manager shows `com.unity.ide.vscode` as deprecated; remove it (Visual Studio Editor package already supports VS Code).
- `GameCharacter` and `GameCharacterCar` are two halves of one partial class with walking and driving mixed together. When the harvester is added, consider splitting into a base controller plus a vehicle controller, and add enter/exit-vehicle.
- No sell point, money or prices yet (Phase 3 above).

## Phase 3 design decisions (economy)
- **Selling needs a drive.** The player must drive to a sell point to sell crops (not instant selling). Interaction uses the USE button / E key, same as fields.
- **Sell point: the `Wheat Selling Station`** (object `Wheat Selling` in the scene; decided). The other unused buildings (`House`, `Garage EX`) can become the seed shop, fertilizer shop, dealer or paint shop later.
- **Map:** the old 2D map is not being copied. The 3D map is being laid out fresh, so sell point placement is open.
- **Background:** the game was finished as a 2D version before. The 3D version should aim for the 2D feature set. Open question: which features did the 2D version have (prices, upgrades, vehicles, buying land)? Use it as the feature checklist (see the 2D analysis below).
- **Money:** add money to `PlayerValues` and the counters panel; sell prices per crop; maybe price changes over time.

## Advanced crop harvesting (design, not yet started)
Today harvesting is instant: press E / USE on a Ready field and the whole field's yield (`Convert.ToInt32(size)`) goes into the counters at once (`FieldController.Harvest`). The new version makes harvesting something you **drive across the field to do**, like the real game.

**Idea (carried over from the owner's earlier version):** split the field's area into many small hitboxes, created by a `for` loop. While the player is in **harvesting mode** and drives into a hitbox, that hitbox is harvested and pays out its share of the field's yield.

**How it works**
1. **Hitbox generation.** When a field is ready to harvest (or on `Start`), a `for` loop (nested, X by Z) creates a grid of small trigger `BoxCollider` objects as children of the field, covering the field's footprint. Grid resolution is an Inspector field (for example `harvestCellSize`, in metres), so a bigger field gets more cells.
2. **Share of the yield.** Each cell is worth `1 / cellCount` of the field's total yield (so cells add up to exactly 100%). When the harvester touches a cell, the player gets that percentage of the yield **plus any bonuses in effect**.
3. **Harvesting mode.** A toggle (E / USE, or a separate HARVEST button) that is only on while the player is in the harvester/vehicle. Cells only react when the mode is on and the field's stage is `Ready`. Driving over a cell in any other mode does nothing.
4. **One-shot cells.** A cell can be harvested once. When it is hit, it is disabled and the crop under it is visibly cut (see "Visuals"). Progress is shown on the field pop-up as "Harvested 45%".
5. **Finishing the field.** When all cells are harvested (100%), the field returns to `Dirt` / empty, same as the current `Harvest()` end state. Leaving a field half-harvested is allowed; the rest stays `Ready` and can be finished later.
6. **Bonuses.** Yield per cell = `baseYield / cellCount * (1 + sum of bonuses)`. Bonus sources, all kept as editable Inspector fields:
   - **Harvester:** +60% (the 2D game's number).
   - **Fertilizer:** +50% per level, two levels (2D game; uses `fertStage`, which exists but is unused).
   - Later: vehicle upgrades, crop type, etc. Add a small `HarvestBonus` list (name + multiplier) so new bonuses don't need code changes.
7. **Stock goes to `PlayerValues`** as it is harvested (the existing counters), so selling at the station (NEXT UP) works unchanged.

**Implementation notes**
- Add a small `HarvestCell` script on each generated cell (or one `HarvestCell` pattern in `FieldController`) that reports back to its field: `field.OnCellHarvested(cell)`. The field owns the percentage maths and the bonuses.
- Cells only need to be created once the field is `Ready` and can be destroyed when it resets, to avoid hundreds of idle colliders on every field. Use pooling if it is slow.
- Triggers need a Rigidbody on one side. The vehicle already has one, so cells can be plain trigger colliders. Filter by tag/layer (`Player` / harvester) as `FieldController` already does for entering a field.
- Keep the existing instant harvest as a fallback (for example on foot, with no harvester), but without the +60% harvester bonus. Decide whether on-foot harvesting should stay at all.
- Mobile: harvesting mode needs a touch button (add to `TouchControls`).

**Visuals**
- Cut cells should change the ground under them (swap to the `Dirt` colour for that cell, or place a stubble quad). The field currently uses one colour for the whole field via shared materials, so per-cell visuals need either a texture mask or small quads per cell. Start with disabled-cell-only (no visual) and add the cut look afterwards.

**Open questions**
- Is the harvester a separate vehicle (needs a model, enter/exit and vehicle switching) or a mode on the existing truck? The plan notes say the harvester model does not exist yet; the first version can use the existing vehicle with harvesting mode.
- Should harvesting use fuel (the 2D game's diesel) per cell?
- Grid cell size vs. performance: test on the Android build before picking a default.
- Does the yield stay `size`-based (2D: size x 550 to 650) or keep the current `size` number?

**Suggested order:** (1) generate the cell grid and show it with gizmos, (2) harvesting-mode toggle, (3) per-cell percentage payout with the harvester bonus, (4) progress shown on the pop-up and field reset at 100%, (5) fertilizer bonus once fertilizer exists, (6) visuals for cut cells, (7) touch button.

## Repo housekeeping
- [x] Android build output is no longer tracked (committed and pushed); `apk/` and `*_BurstDebugInformation_DoNotShip/` are in `.gitignore`.
- [x] **UserSettings stays tracked**, and `UserSettings/Layouts/` (the window layouts) is now tracked too, so the editor layout follows the repo.
- **`AllSkyFree` is left alone** (320 MB, to be dealt with later by the owner).
- **Repo size is about 2.2 GB** (tracked files), nearly all of it unused texture packs:
  - `Assets/TerrainSampleAssets/Textures` is 1.93 GB (1.3 GB terrain layer textures, 0.56 GB heightmaps, 0.05 GB detail textures) and **nothing in `GameScene` or the terrain data uses it** (the terrain has no terrain layers; the sky is the default skybox).
  - `Assets/AllSkyFree` is 320 MB, also not used by the scene.
  - Removing files only helps future clones if the history is rewritten too (`git filter-repo` plus a force push). Decide before doing anything. Git LFS only makes sense for large files we keep.
- Ask before shrinking anything.

## Next up
- **2D version analysis:** the finished 2D game is on GitHub. Clone it into a separate folder (outside this project) and read it to build the feature checklist for the 3D version (what is sold, prices, upgrades, vehicles, buying land).
- **Selling point:** the `Wheat Selling Station` (see NEXT UP at the top). The owner creates the trigger zone; Claude writes the `Building` script and the money/sell logic.

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
- **Not in the 3D game yet:** money, seeds, compost, diesel, field ownership, buying and selling fields, vehicle switching, painting, dealer, save/load, AI owners planting fields.
- **The harvester already exists in 2D as art** (`h/` folder, four colours). In 3D it still needs a model.
- **Suggested order for Phase 3:** (1) money in `PlayerValues` and the counters panel, (2) a `Building` trigger with a sell action on the chosen building, (3) seeds and a seed shop, (4) field buying, (5) fuel and the gas station, (6) fertilizer and compost, (7) the dealer and paint shop.

### Assets in the 2D repo that could be reused
- `backround.png` (3500x3000), the full map. A layout reference only; the 3D map is built fresh.
- Tractor, van and harvester sprites (four colours each) and `garage.png`, `tittle.png` (title screen).
- `Read Me.txt`, `Saves/*.txt` (save format).

