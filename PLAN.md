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
