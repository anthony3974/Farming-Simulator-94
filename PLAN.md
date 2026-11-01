# Farming Simulator 94: Improvement List and Initial Plan

Unity 2022.3.55f1. Scene: `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`.
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
- **Two existing buildings have no purpose yet** (`Assets/Prefabs`: `House`, `Garage EX`, plus `houseTop.fbx`). Candidates: one becomes the **sell point / market**; the other a **barn / storage** or a **shop** (buy fields, fertiliser). Decide when starting Phase 3.
- **Map:** the old 2D map is not being copied. The 3D map is being laid out fresh, so sell point placement is open.
- **Background:** the game was finished as a 2D version before. The 3D version should aim for the 2D feature set. Open question: which features did the 2D version have (prices, upgrades, vehicles, buying land)? Use it as the feature checklist.
- **Money:** add money to `PlayerValues` and the counters panel; sell prices per crop; maybe price changes over time.

## Repo housekeeping
- The `apk/` build folder is ignored (`*.apk`), but two small files under `apk/FarmSim943D_BurstDebugInformation_DoNotShip/` are tracked. Untrack them and ignore the folder (`git rm -r --cached apk/FarmSim943D_BurstDebugInformation_DoNotShip` and add `apk/` plus `*_BurstDebugInformation_DoNotShip/` to `.gitignore`).
- **The repo is about 2 GB**, mostly large textures in `AllSkyFree` (Cold Sunset 76 MB, Deep Dusk 48 MB) and `TerrainSampleAssets` normal maps (about 34 MB each, 5+ files). GitHub works but warns above 1 GB. Options later: Git LFS for `*.tif`, `*.png`, `*.exr`, `*.hdr` (this needs a history rewrite to shrink existing size), or delete unused sky/terrain textures. Not urgent.

