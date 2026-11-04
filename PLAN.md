# Farming Simulator 94: Improvement List and Initial Plan

> ## >>> NEXT UP: Look around on phones <<<
>
> **When asked "what's next?", the answer is the first unfinished item in this list** (set by the owner, updated 2026-11-04). The seed shop is finished (the owner may revise the economy later).
> remmind me to update the version number
> 1. ~~**Map border.**~~ **DONE (2026-11-03).** Built with `Assets/Editor/MapBorderBuilder.cs` (Tools > Map Border Builder). The owner is happy with it, so the tool was removed (2026-11-04; git history, last version `9f800c5`, to rebuild or resize the wall). The scene has a `Map Border` object with 572 grey concrete blocks (scaled about 7.5 x 3.3 x 1.0 m, 0.3 m of it buried), each with a collider, 1 m inside the terrain edge. Nothing in the scene is near the wall. A real barrier prefab can be dropped into the tool later. Checked by the owner in Play mode (2026-11-03): the truck cannot pass.
> 1. ~~**Car dealer.**~~ **DONE (2026-11-03).** Built in GameScene and tested in Play mode by the owner. The builder tool was removed afterwards (kept in git history, last version `3340302`). Owner's decisions (2026-11-03): the dealer is its **own new building**, a red L-shaped building on a grey concrete lot in the **top-left corner of the map**, the same place as in the 2D game (not the `House` or `Garage EX`). The **harvester is its own vehicle.** There are no vehicle models, so the van and the harvester are placeholders built from boxes. What was built:
>    - `Vehicle` (was `GameCharacterCar`): one per vehicle, holds the driving code and stats (top speed, harvest bonus, camera position, paint parts) and remembers which fields and buildings it is parked in. `GameCharacter` is now the player only: input, camera, and the vehicles you own. The unused walking mode (`drive` off) was removed; there is no getting out on foot.
>    - Keys **1 / 2 / 3** drive the truck / van / harvester (if owned), touch button **CAR** picks the next one. The camera and speedometer move to that vehicle; the others stay parked where you left them.
>    - `Dealer` + `Building` type Dealer: press E / USE anywhere on the lot to open the shop menu (van and harvester, $5,000 each). **Red only (owner, 2026-11-03): the colour choice was removed.** The van and the harvester are prefabs (`Assets/Prefabs/Van.prefab`, `Harvester.prefab`, materials in `Assets/Vehicles`) standing on `Parking` by the house; buying one makes that vehicle yours where it stands. Made with the one-off Vehicle Prefabs tool, then removed (git history, `530c871`) (the old runtime `VehicleFactory` builder is gone; the names, prices and descriptions are in `VehicleCatalog.cs`). The dealer menu was built in code in `Dealer.cs`; it is now a scene object (`Dealer Menu`, see item 4).
>    - Van: top speed 62 km/h (truck 50). Harvester: 38 km/h and **+60% harvest** (2D speed ratios 8 / 10 / 6).
>    - **The van cannot work fields (owner, 2026-11-03):** in the van, E / F / B in a field only show "The van can't work fields". It still sells crops and buys seeds and fertilizer at the buildings, and opens the dealer.
>    - The dealer was built by `Assets/Editor/DealerBuilder.cs` (now only in git history, `3340302`) from the 2D map rectangles (the 3D terrain is the 2D map turned on its side: 2D up = +X, 2D right = -Z), which also added a `Vehicle` to the truck.
>    - **Shop menu checked on a phone by the owner (2026-11-04): looks good.**
> 2. ~~**Home menu.**~~ **DONE (2026-11-04, branch `home-menu`): main menu and pause menu built in the scenes and working (owner). Version now 0.3.** Owner's decisions (2026-11-04): a **separate `MainMenu` scene**; the background is a **game screenshot** the owner will take; **no Quit button**; **version number on the home menu only** (from `Application.version`, Project Settings > Player > Version, now 0.2); a **TERMS OF SERVICE button that does nothing yet** (a reminder for Google Play, link it with `Application.OpenURL` when publishing). What was built:
>    - **The menu is real objects in the `MainMenu` scene** (see the rule below the NEXT UP box); `MainMenu.cs` only switches screens and runs the buttons. Home screen: screenshot background (fills the screen, cropped not stretched, darkened by the `Darken` object), gold title, big PLAY, small TERMS OF SERVICE bottom-right, version bottom-left. PLAY opens a Play panel: NEW GAME (loads GameScene), LOAD GAME (greyed out until save/load, item 18), BACK (Escape / Android back also goes back). Kept inside the phone safe area.
>    - `Assets/Editor/MainMenuBuilder.cs` (Tools > Build Main Menu Scene, run by the owner, then removed: git history `8ae0ee6`) created `Assets/Scenes/MainMenu.unity` with the whole menu (camera, EventSystem, canvas, screens, buttons) and put it first in Build Settings, GameScene second.
>    - To change the background: drag a screenshot into the **Texture** of `Main Menu > Background > Screenshot`. Still worth checking the layout on a phone.
>    - **Pause menu (2026-11-04, same branch, working).** Owner wants it as **real objects in the scene hierarchy** so it can be rearranged in the editor (see the rule below the NEXT UP box). `Assets/Editor/PauseMenuBuilder.cs` (Tools > Build Pause Menu, run by the owner, then removed: git history `a4b97f9`) added a `Pause Menu` canvas: PAUSE touch button (top-left beside HELP, touch only), and a `Menu` part with PAUSED, RESUME, MAIN MENU (press twice: the farm is not saved) and an "Esc to resume" hint. Plain objects, no layout groups. `PauseMenu.cs` only shows/hides it and runs the buttons; Esc toggles it, game time stops while paused (`Time.timeScale` 0). The `Menu` part stays visible in the editor and hides itself on Play. Esc no longer just frees the mouse: it pauses (which frees it), and an Esc that closes the dealer does not also pause (`InputManager.MenuJustClosed`). Help list: "Pause (releases the mouse) / Esc / PAUSE".
> 3. **Look around on phones (owner, 2026-11-04: soon).** A drag-to-look area so the camera can turn on touch (today the camera stays still on phones; mouse look needs a locked cursor). See Android port item 4. Keep it clear of the buttons.
   - **Built (2026-11-04).** The owner ran Tools > Build Touch Look Area in GameScene; the tool was then removed (git history, `58931f8`). Still to check on the phone: does the drag feel right (`touchLookSensitivity`, `invertTouchLook`). Owner's decision: **tilt only, like PC** (drag up / down; no turning left / right). The tool adds a `Touch Look` canvas (sort order -100, below every other canvas so buttons and panels get the touch first) with an invisible full-screen `Look Area` + `TouchLook.cs`. `TouchLook` sends drags (in screen heights) to `InputManager.AddTouchLook`; `GameCharacter` tilts by `touchLookSensitivity` (120 degrees per full-screen drag) within the same pitch limits as the mouse, `invertTouchLook` flips it. One finger at a time, so another can hold GAS. Ignored while a menu is open (pause, dealer). Turned off on PC unless Show Touch Controls is ticked. Help list: "Look up / down / Mouse / DRAG".
> 4. ~~**Move the UI still built in code into the hierarchy**~~ **DONE (2026-11-04) except `TouchControls`, which moves with the touch rework (item 20).** (see the rule below): `HelpMenu`, the `Dealer` shop menu, the `Building` message box, `Speedometer`, and `TouchControls` (with the touch rework, item 20). Buttons become copies of the `UI Button` prefab. One at a time.
   - ~~**HelpMenu.**~~ **DONE (2026-11-04): built in GameScene (`56167c5`) and tested by the owner. The tool was removed (git history, `58fad2d`).**
   - ~~**Dealer shop menu.**~~ **DONE (2026-11-04): built in GameScene (`4db11ce`) and tested by the owner ("dealer is good"). The tool was removed (git history, `47e5a43`).**
   - ~~**Message box and speedometer.**~~ **DONE (2026-11-04): built in GameScene and tested by the owner ("they look great"). The tool `HudBuilder.cs` was removed (git history, `8fdfbc3`); run it again from there to redraw the dial after changing Dial Max Kmh.**
     - `Message Box` canvas (sort order 50, no raycaster so it never blocks a touch) + `ScreenPrompt`: `Box` (yellow border, screen centre, turned off in the editor) > `Panel` (brown, inset 4) > `Text`. `ScreenPrompt` moved out of `Building.cs` into its own `ScreenPrompt.cs` (Unity needs the file name to match to keep it on a scene object). It finds the scene box through `ScreenPrompt.Instance` (same calls as before) and, with Fit Height To Text, sets the box height to fit the text (one or two lines); width and position stay as set. With no box in the scene it logs one error and sends messages to the Console only.
     - `Speedometer` canvas (sort order 40, no raycaster) + `Speedometer`: `Dial` (bottom-left) > `Numbers` (0 to 100), `Needle`, `Hub`, `Number`. The dial face and hub are PNGs the tool draws into `Assets/Images/Speedometer` (same look as before); the tool (now in git history) keeps the existing Dial Max Kmh when run again. `Speedometer.cs` only turns the needle and writes the number (Number Format in the Inspector); `GameCharacter` finds it in the scene instead of adding it to the tractor. The `Dealer Menu` canvas (sort order 60) holds `Menu` (turned off in the editor, tick it to work on it) > `Dimmer` (tap closes) and `Border > Panel` with Title, Money, one `<Vehicle> Row` per vehicle for sale (Name, Description, `Buy Button` = a `UI Button` prefab copy) and two footers (Keyboard / Touch, the Dealer shows the right one). Plain positioned, no layout groups. The tool fills in the menu slots on the scene's `Dealer` (Menu Root, Close Button, Money Text, footers, Rows). `Dealer.cs` no longer builds anything; it opens / closes the menu, writes the money and BUY / OWNED (texts in the Inspector: Money Format, Buy Format, Owned Text) and buys. Prices still come from `VehicleCatalog.cs`; the names and descriptions now live in the scene. The old `dealerName` field is gone (the title is a scene text). OWNED greys the button's Fill (70, 70, 70) and turns the button off. The `Help Menu` canvas (sort order 60) holds `Menu` (full-screen dimmer, a tap closes it; turned off in the editor, tick it to work on it) with two plain-positioned panels: `Keyboard Panel` (PC) and `Touch Panel` (touch controls on; the debug row is left out). The help text now lives in the scene (each `Row` has `Action` and `Control` texts), not in code. `HelpMenu.cs` only shows the right panel and opens / closes it; `InputManager` no longer adds it. With Show Touch Controls ticked in the editor you now see the touch panel (before: both columns). No buttons on it, so no `UI Button` copies.
> 5. ~~**Tractor redesign.**~~ **DONE (2026-11-04, `f3feb95`): the owner ran Tractor Builder V2, "looks amazing", and deleted the old model from the Tractor in GameScene.** Left to tidy: the old `Model` is still in `Tractor.prefab` (turned off, removed only on the scene copy); delete it in prefab mode when wanted. The tool was removed (2026-11-04; git history, `1cfcae2`). Original notes: **(owner, 2026-11-04).** The current tractor (Tractor Builder, `8c1c8b6`) is better than the original but has **too many gaps and floating parts**. Redo it so every part touches or overlaps its neighbour (no visible gaps, nothing hovering), as a prefab the owner can edit in the hierarchy. **Owner (2026-11-04): keep the old tractor, make everything look connected.**
   - What Tractor Builder V2 did (`Assets/Editor/TractorV2Builder.cs`, removed, git history `1cfcae2`): added a `Model V2` beside the old `Model` in `Tractor.prefab`; the old one is **only turned off, not removed**, and the buttons **Use new (V2)** / **Use old** switch between them. Running it again rebuilds only `Model V2`. Same look, same layout and the same `Assets/Tractor` materials, but every part overlaps the one it hangs on (a few cm) and they all join the frame: the axles run into the wheel centres (a mount joins the front axle to the frame), the hood and cab base sit on the frame (the hood runs into the cab base), the fenders join the cab sides with skirts in front of and behind the wheels, the corner posts run from the cab base into the roof, and the grille, lights, exhaust (on a collar), roof beacon and hitch are set into their parts. All four tires touch the ground. The `Vehicle` paint list follows the model in use. Once the owner picks one, delete the other model and the tool (keep it in git history).
   - **Fixed (`0fa0a5d`):** front wheel steering also picked up tires from a turned-off model and moved them onto pivots outside it, so they showed again. Only active tires steer now.
>
> 6. **App icon (owner, 2026-11-04).** The phone shows Unity's default icon. Make a Farming Simulator 94 icon (square, at least 1024 x 1024 PNG; Android also wants an adaptive icon: a foreground layer and a background layer) and set it in Project Settings > Player > Icon (Android and Default). Needed before publishing on Google Play (512 x 512 for the store listing).

7. **Sounds (owner, 2026-11-04).** The game has no sound yet. Ask the owner which sounds and where they come from before starting. Likely list: engine for each vehicle (pitch follows speed), button clicks in the menus, planting / harvesting / fertilizing, money in and out at the shops and sell stations, and maybe background music and ambient farm noise. A volume setting (pause menu) could come with it.
>
> **Later (not soon):** fuel and the gas station (2D: diesel tank 100, 1 unit about every 200 movement ticks, 2 dollars per unit, game over at 0), save/load (item 18), the touch buttons rework (item 20).
> **Distant, not a priority (owner, 2026-11-04):** working lights on the house and garage.
> **Dropped (owner, 2026-11-04):** the paint shop. Vehicle colour does not matter.
>
> **Building and tractor redesigns (2026-11-03, built and saved in GameScene).** The tools were run, then removed (kept in git history): Tractor Builder `8c1c8b6` (truck renamed to tractor, new red tractor model, front wheel angle 30 to 20), Sell Station Builder `d60593b` (wheat / barley / oat grain elevators), Shop Builder `10298e7` (new seed shop look, and a new **fertilizer shop** at X 340, Z 601 next to the seed shop because the 2D spot is taken by fields). **Shop amounts (owner, 2026-11-03): both shops sell 2,000 at a time, same price per unit:** 2,000 seeds for $900 to $1,100 and 2,000 fertilizer for $800 to $1,000. The `Building` fields were renamed (`seedsPerBuy`, `minSeedPrice`, `fertilizerPerBuy`, ...) so the old Inspector values in the scene are ignored.
>
> **Tests and tidy (updated 2026-11-04).**
> - [x] **Van in a field:** E, F and B only show "The van can't work fields" (owner, 2026-11-04).
> - [ ] **Test growth time:** about 75 s per crop (4 x 18.9 s).
> - [x] **Van number plate:** Overrides > Apply All done by the owner, `VehiclePlate.mat` deleted (2026-11-04).
> - [x] **Van scaled up** by the owner (scale 1.2808 on the scene copy). The camera then sat 1.28x further back (the camera is a child of the van, so it scales too); `cameraOffset` on `Van.prefab` was divided by 1.28 (`(0, 5, -8.5)` to `(0, 3.9, -6.64)`), so it sits as far away as before. **Owner to check it feels right**, adjust `cameraOffset` on the Van if not.
> - [x] **Unused assets deleted:** `House.prefab`, `houseTop.fbx`, `Garage EX.prefab` (2026-11-04).
> - [x] **Phone check:** the HELP and PAUSE buttons (top-left) look good on the phone (owner, 2026-11-04).
>
> **Fixed (2026-11-04):** ~~**On phones the grass looks like water.**~~ The two layers the terrain uses (`Grass_B`, `Pebbles_B`) took smoothness from the texture's alpha, but those textures are imported with **no alpha**, so alpha reads as 1 = fully glossy, and the sky reflects off the ground like water. Both layers are now **Smoothness Source: Constant** (smoothness 0). **Confirmed on the phone by the owner.** Android stays on Medium quality (High would mainly add soft shadows, at a frame-rate and battery cost).
>
> **Tested by the owner in Play mode (2026-11-03):** fertilizer (buy, F twice in two stages, harvest bonus), buying the van and harvester from Parking and driving them with 2 / 3, the 2,000-unit shops, mouse look on PC. Cleaned up in the scene by the owner: the turned-off old `House` and `Garage EX` and the dealer's old parking-spot objects are deleted (their prefabs were deleted 2026-11-04).
>
> **Also pending (owner, 2026-11-03):** rework the touch buttons (they are a little confusing, see item 20). Ask the owner when. The field sell confirmation text is fixed ("Press again to confirm").
>
> **To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."

## UI colours (owner, 2026-11-04): keep them the same everywhere
HUD and buttons: **brown fill `(60, 45, 20)` opaque, yellow border `(255, 204, 0)`, gold text `(255, 201, 51)`**. Buttons are a yellow square with the brown fill inset 5 px (the touch-button style); not an Outline effect. **Every button is a copy of the prefab `Assets/Prefabs/UI/UI Button.prefab`** (owner, 2026-11-04: a prefab guarantees they match and can be restyled in one place). New buttons: drag in the prefab, then change only the name, place, size and text. Change colours or the border in the prefab, not on one button. Made with a one-off UI Button tool (git history, `d203505`); the pause menu and main menu buttons are prefab copies (done 2026-11-04). The touch buttons are still built in code; they move to the prefab when they move into the hierarchy. Pop-up panels (help, dealer, menus) are dark grey `(31, 31, 31)` with the same buttons on them. Pressed touch button: `(110, 85, 10)`. Greyed-out button: `(70, 70, 70)`.

## Rule: build things in the scene hierarchy, not in code (owner, 2026-11-04)
New objects (menus and their canvases, buildings, vehicles, anything visible) go **into the scene hierarchy or a prefab as real objects**, so the owner can select, move, resize and restyle them in the editor. Do **not** build them from code at runtime (the tractor felt like that and the owner did not like it). Scripts only hold references to those objects (Inspector fields) and run the behaviour.
- The usual way: a one-off editor tool (Tools menu) that creates the objects in the open scene, fills in the references and is then deleted (kept in git history).
- Use plain positioned objects for UI (no layout groups) unless asked, so dragging things around works.
- Still built in code, to move into the hierarchy when next touched: `TouchControls` (part of the touch rework, item 20).

Unity 6000.0.85f1. Scenes: `Assets/Scenes/MainMenu.unity` (opens first), `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`.
This is an initial plan from reading the code. It is not the plan from the earlier chats. Edit it as you remember more.

## Improvements needed

### Current bugs*
(*Not a real bug yet, a limit we will hit once there are enough fields. Not numbered so the item numbers below and the references to them stay valid.)
- ~~**Field names run out.**~~ **FIXED (2026-11-03).** The 14 fields in the scene (2 the player's, 12 AI) used all 12 names from the 2D game. Now, once the list is used up, an AI field gets a random base name with the next free number ("Dahl Ranch 2", "Dahl Ranch 3", ...), so more fields can be added. (A player field's scene name that is also in the list is still not reserved; minor.)


### Unfinished features
15. **Fertilizer** (written and tested by the owner, 2026-11-03): press **F** (touch: **FERT**, a third button column beside CAR, temporary place) in your own field while the crop grows. Two levels, at most one per growth stage, each **+50% harvest** (2D game). Each use costs fertilizer of about 0.42 to 0.58 x the field's harvest (the 2D ratio, Inspector values on `FieldController`), rolled once per visit like the seed price. The field pop-up shows only the bonus: "Fertilized: +50%" (level 1) or "+100%" (level 2). Harvest adds the fertilizer bonus to the harvester's +60%. Start stock is `startFertilizer` on `PlayerValues` (0); buy it at the fertilizer shop. Fertilizing does not speed up growth.
16. ~~**Selling and money.**~~ **DONE** (sell stations for wheat, barley and oat, money in the counters panel, field `value` is the buy price).
17. ~~**Field ownership and buying.**~~ **DONE** (B key to buy or sell, press twice to confirm; sell pays 80%; other fields get random AI owners and names).
18. **Save/load** of crops, stock and money.
19. **Help menu with the controls list.** **Phase 1 DONE (code written, not yet tested in Unity):** `HelpMenu` is a pop-up with a controls table (action, keyboard, touch button) and nothing else (no tips). H opens and closes it; on touch a HELP button sits top-left and tapping the pop-up closes it. Keyboard-only controls (mouse look, Esc, F1) are hidden on phones. Check the HELP button position on a real phone. Phase 2 (if wanted): icons instead of text, a controller or gamepad layout.
20. **Touch controls rework.** The owner ran the phone build (2026-11-03) and it works, but the buttons are a little confusing and need reworking. ~~Also: when selling a field the message says "press B to confirm", but a phone has no B key.~~ **Fixed (2026-11-03, `a92f85d`):** it now says "Press again to confirm". Other messages that name a key already show the touch name on phones (E / USE, 2 / CAR).

## Direction
The owner is no longer cloning the 2D game one for one. The 3D game is its own new game and is built as it goes. The 2D analysis below is a reference and an idea source, not a checklist.

## Initial plan (in order)

**Phases 1 and 2 (stabilise, refactor `FieldController`): DONE.** See Progress below.

**Phase 3: Economy**
- ~~Money in `PlayerValues`, sell prices, sell points, money in the UI~~ DONE.
- ~~Field buying and selling~~ DONE.
- ~~Seeds and seed shop~~ DONE (seed shop complete; the economy may be revised later).
- Next (owner's order, 2026-11-03): map border, the car dealer, then the home menu (with a version number). All three done. After that (2026-11-04): phone look, UI into the hierarchy, tractor redesign; later fuel and gas station and save/load. The paint shop is dropped.

**Phase 4: Fields and fertiliser**
- ~~Buying fields~~ DONE (moved into Phase 3).
- ~~Fertiliser~~ written (see item 15): F during growth, +50% yield per level, two levels.
- **Advanced crop harvesting:** drive-over harvest cells with bonuses (see "Advanced crop harvesting" below).

**Phase 5: Polish**
- Save/load (18). (Camera clamp (14) and input cleanup (13) are already done.)

**Phase 6: Vehicle mirrors**
- Add mirrors to the vehicle (left and right side mirrors, maybe a rear-view mirror).
- Likely approach: a small extra camera per mirror drawing into a `RenderTexture`, shown on a quad on the mirror, or a small on-screen panel. Keep the cost low on phones (small texture, lower frame rate or resolution for mirror cameras, off when not needed).
- Decide: real mirrors on the model, or a screen overlay. Check the Android frame rate after adding them.

## Waiting on the owner (decisions)
- **Sky and terrain layers: decided (owner, 2026-11-03).** The current set is final for now (the owner may delete one or two later). See Repo housekeeping.
- **Camera: decided (owner, 2026-11-03).** The `staticCamera` testing switch is removed, so mouse look is back on PC. The phone side was left untouched on purpose: on touch the camera stays still (see Android port item 4, touch look).
- **Touch layout:** fixed from a phone screenshot (2026-11-02): steering arrows moved right of the speedometer, right-hand buttons shrunk to 130 px so CROP no longer covers the Money/Seeds/Fertilizer panel. Needs a re-check on the phone (not yet seen after the change).

## Noted from play-testing (all done, see Progress below)
- **Crop type shows "TypeOat" / "TypeWheat".** `FieldController` sends `cropType.ToString()`, which is the enum name. Show "Oat" / "Wheat" / "Barley" instead (and a friendly status such as "Growing 2/3", "Ready to harvest").
- **Field pop-up redesign.** Currently one semi-transparent white panel with 7 plain text objects and no labels. Wanted: dark-gray background, yellow border, coloured labels with white values (for example `Size:` label coloured, value white), possibly a split layout.
- **Camera looks up and gets stuck after leaving and re-entering the Game window.** Causes found in `GameCharacter.RotateY`:
  1. the cursor is never locked, so moving the mouse out of and back into the window gives a huge `Mouse Y` delta in one frame;
  2. the upper limit check `camY < 0 && camY > 350` can never be true, so only the lower limit is enforced;
  3. the clamp works on Euler angles, which wrap at 360.
  Fix: keep a `pitch` float, clamp it (for example -80 to 80), lock the cursor on click and unlock on Escape, and ignore mouse input while unlocked or for the first frame after focus.

## Progress so far
- [x] Baseline pushed. (The `baseline-2022.3.55` tag and all history before the Unity 6 upgrade were removed in the 2026-11-03 history cleanup.)
- [x] Upgraded Unity 2022.3.55f1 to 6000.0.85f1; removed unused HDRP/URP packages (project uses the Built-in render pipeline)
- [x] Phase 1 fixes: overlapping fields, stale pop-up, wasted growth timer, unused `using` lines
- [x] "TypeOat" text bug fixed (pop-up now shows Wheat / Barley / Oat / None and "Growing n/3", "Ready!")
- [x] Field pop-up redesign and matching crop-counter panel (dark background, yellow border, gold labels, white values)
- [x] Camera fix: `pitch` float with clamp, cursor lock (Esc releases), mouse ignored on focus change
- [x] `FieldController` refactor: enums + colour table, shared materials, editable `growTime` (now `secondsPerStage`) and `sizeDivisor`, missing-reference check
- [x] Android controls phase 1: `InputManager` takes touch input, `TouchControls` builds on-screen buttons at runtime
- [x] Touch buttons tested in the editor, resized and re-laid out; main Canvas now *Scale With Screen Size* (1920x1080, match 0.5)
- [x] Android Player Settings configured, first APK built and running (Android VM, then a real phone; owner reports it works well, 2026-11-03)
- [x] Phase 3 economy, part 1: money, `Building` script (type + trigger + prompt), sell stations for Wheat, Barley and Oat
- [x] Centre message box (gold border, grows with its text), used for prompts and confirmations
- [x] Crop selection with the C key (plant what you picked; no more random crop)
- [x] Field buying and selling with the B key (confirm on the second press); selling pays 80% and the crop stays on the field
- [x] Field ownership: only fields owned by the player respond to E; other fields get random AI owners and names from the 2D game
- [x] Dial speedometer (bottom-left, 0 to 100 km/h, truck top speed reads 50)
- [x] Touch buttons added for BUY/SELL and CROP (positions temporary)
- [x] **Counters layout:** Wheat / Barley / Oat are in a centred bar along the top of the screen, and the right-hand panel (`Canvas > Statistics`) shows Money, Seeds and Fertilizer (fertilizer shows 0 until fertiliser exists). Built with a one-off editor menu script that is kept only in the git history (`bb1cd99`).
- [x] Seeds and seed shop: planting costs seeds, `Seed Buying` station sells them (E), start with $5,000 and 25,000 seeds; economy rebalanced (see Economy balance). Seeds are finished for now. The one-off setup tool (`AddSeedShop`) is kept only in the git history (`2632790`)
- [x] The player's field starts planted with wheat; growth time raised 35% (9.45 s per stage)
- [x] Fields recreated and resized to a standard starting size by the owner (the scene now has 7 random fields). AI fields now total 11 of the 12 available names, so only one more fits before names run out (see "Field names run out").
- [x] House: the owner scaled the farmhouse Model to 1.7923 (the first build was smaller than the tractor) and removed the stepping stones and driveway; `HouseBuilder` was updated to match, then removed after the owner ran it (git history, `b572742`) (2026-11-03)
- [x] Garage: Garage EX (34 x 28 x 20 m) replaced by a 16 x 13 m garage matching the house; the owner scaled it 1.0738 and made the window and side door visible inside. Garage Builder removed after running (git history, `e605aa2`). The old `House` and `Garage EX` instances are turned off in the scene, not deleted; a second Garage EX copy sits inside a plain cube "Garage " at about X 830-855, Z 58-92
- [x] Help menu phase 1: controls list pop-up, H key and a touch HELP button (untested)
- [x] Truck: the front tires turn with the steering button (visual only, `maxSteerAngle` 30 and `steerAnglePerSecond` 120 on `GameCharacter`; untested)
- [x] Field layout: the home field (Tony) is split into four (2 x 2, even gaps). The two facing the house are the player's and the one nearest the house starts planted; the back two are AI. Six random AI fields were added on free ground (owner checked and saved the scene, 2026-11-02). The layout tool is kept only in git history (`d5b10c0`).
- [x] Reverse steering fixed, field confirm message says "Press again to confirm" (2026-11-03)
- [x] Car dealer, `Vehicle` split and vehicle switching: built and tested by the owner (2026-11-03)
- [x] Top road texture no longer stretched: `RoadTiling` tiles each road by metres (6 m per tile, set on the road's component), the road texture has 8x anisotropic filtering. The one-off Road Look tool is kept only in git history (`48d5ead`)
- [x] Van redesigned with rear doors, tail lights, number plate, the 2D sprite's roof ribs, mirrors and side detail (now `Assets/Prefabs/Van.prefab`, 87 parts; not yet checked in Play mode)
- [x] Static camera removed: mouse look is back on PC (2026-11-03). Phones unchanged (no touch look yet).

## Android port (notes for revising later)
Phase 1 is done (see above). Still to do:
1. ~~**Test the touch buttons in the editor.**~~ DONE. Tick `Show Touch Controls` on `InputManager`, check layout, drive, and plant/harvest with USE. Adjust button sizes/positions from screenshots.
2. ~~**Make the existing UI scale.**~~ DONE. The scene Canvas uses *Constant Pixel Size* (reference 800x600), so the counters and field pop-up will be the wrong size on phones. Switch it to *Scale With Screen Size* (for example 1920x1080, match 0.5) and re-check both panels.
3. (Partly done: no overlaps at 1920x1080.) **Check the touch layout against the pop-up.** GAS / BRAKE / USE sit above the bottom-right pop-up (260 px tall + 20 px margin); verify on a real screen, including phones with a notch or rounded corners (safe area).
4. **Touch look (not started, owner asked to leave the phone side as it is for now, 2026-11-03).** `staticCamera` is gone and mouse look is back on PC. To do: add a drag-to-look area (the cursor is not locked in touch mode, so `GameCharacter` mouse look does not apply on a phone).
5. ~~**Player Settings for Android:**~~ DONE (landscape, API 23+, IL2CPP, ARM64, package name).
   - minimum API level is 22 but Unity 6 needs 23 or higher; set it deliberately
   - force landscape orientation
   - IL2CPP + ARM64 for release builds
   - set a proper package name and app name (the default is a placeholder)
6. ~~**Install Android Build Support**~~ DONE. (Unity Hub > Installs > gear on 6000.0.85f1 > Add modules: Android Build Support, OpenJDK, Android SDK & NDK Tools). Close the editor while it installs.
7. **Performance on a phone.** Likely cost: terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`. Test and lower the quality settings for the Android quality level if needed.
8. ~~**First device build.**~~ DONE (runs on a real phone). Frame rate and UI size still worth a note. Original task: Build an APK, install it on a phone (enable "install unknown apps"), then test controls, UI size and frame rate.
9. **Sharing options.** APK only works for Android friends. A WebGL build on itch.io would be easier for friends with a PC (just a link), but has its own limits.
10. **Optional later:** replace `<` `>` `GAS` `BRAKE` `USE` text labels with icons; haptic feedback; a pause/settings button.

## Other things noticed (not yet scheduled)
- `PlayerValues` counters and `FieldView` references are set by hand in the Inspector; the new missing-reference check in `FieldController` helps, but the other scripts have no such check.
- ~~`GameCharacter` and `GameCharacterCar` are two halves of one partial class.~~ Split into `GameCharacter` (player) and `Vehicle` with the car dealer (2026-11-03). Getting out on foot (enter/exit) is not built; ask the owner if it is wanted.
- Vehicles are not saved yet: bought vehicles are lost when the game restarts (part of save/load, item 18).
- Fields are not planted or harvested by the AI owners (they only get a name and owner); decide later if they should.
- Touch layout: overlaps fixed (see Waiting on the owner), re-check on the phone. BUY/SELL and CROP positions are still not final.

## Economy balance (set 2026-11-02, owner may revise)
In 3D, a field's `size` is also its harvest amount (about 4,900 to 23,300), not the 2D size of 1 to 3.5, so the 2D prices per size could not be used directly (planting the 23,300 field wanted 11 million seeds). All prices are now per unit of harvest, keeping the 2D ratios:
- Seeds per planting: 0.75 to 0.9 x harvest (2D: 500 per 600). Starter field "Tony" (23,300) needs about 17,500 to 21,000 seeds.
- Seed shop: 2,000 seeds for 900 to 1,100 dollars (owner, 2026-11-03; same 0.5 dollars per seed as 2D). Fertilizer shop: 2,000 for 800 to 1,000 dollars (0.45 per unit).
- Selling: 0.65 to 0.75 dollars per unit (unchanged, same as 2D). Tony pays about 16,000 per harvest, so about 6,000 profit after buying the seeds.
- Land: 2.1 dollars per unit of harvest (2D: 1,250 per 600), so Tony is about 49,000 (was 11,650) and the smallest field about 10,000. Selling back pays 80%.
- Start: 5,000 dollars (money used to start at 0) and 25,000 seeds.
- Prices are intentional settings of the new game (for example selling a field pays 80%, where 2D paid back 100%), not mistakes.
- **Time:** growth was 4 x 7 s = 28 s, then 4 x 9.45 s = 37.8 s; the 2D game took 160 to 205 s. **Doubled by the owner (2026-11-03) to 4 x 18.9 s = 75.6 s per crop**, set by `secondsPerGrowthStage` on `FieldController` (renamed from `secondsPerStage`, so the 9.45 saved in the scene no longer applies). Still about 2 to 2.7x faster than the 2D game.
- **Starting field:** the player's own field ("Tony") starts the game planted with wheat, in the Planted stage (`startCrop` on `FieldController`; None = empty). The owner had to sell it to afford the first planting, so it now starts growing for free.

## Code review notes (`/code-review high Assets/Scripts`, 2026-11-02)
Fixed (untested): stale B-key confirmation, building prompt overwriting other messages and rebuilding every frame, speedometer text rebuilt every frame, seed price rerolled on every E press, touch button null checks. Left for later (minor, nothing triggers them today):
- One E press can act on a field and a building if their trigger zones overlap (each keeps its own "last entered" list, now per vehicle).
- ~~`GameCharacter.Move()` (walking mode) strafes along the wrong axis.~~ Walking mode removed (2026-11-03).
- `PlayerValues` has no null checks on the wheat, barley and oat labels.
- A field's growth coroutine stops if the field is ever disabled and nothing restarts it.
- A scene-set field name that is also in the AI name list is not reserved, so an AI field could get the same name.

## Phase 3 design decisions (economy)
- **Selling needs a drive.** The player must drive to a sell point to sell crops (not instant selling). Interaction uses the USE button / E key, same as fields.
- **Sell point: the `Wheat Selling Station`** (object `Wheat Selling` in the scene; decided). The other unused buildings (`House`, `Garage EX`) can become the fertilizer shop or paint shop later. The dealer is its own building (owner, 2026-11-03).
- **Map:** the old 2D map is not being copied. The 3D map is being laid out fresh, so sell point placement is open.
- **Background:** the game was finished as a 2D version before. The 3D version should aim for the 2D feature set. The 2D version is a reference only (see "Direction").
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
   - **Fertilizer:** +50% per level, two levels (2D game; `fertStage` now counts the levels, see item 15).
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
- ~~Is the harvester a separate vehicle or a mode on the truck?~~ **Its own vehicle** (owner, 2026-11-03). It is bought at the dealer and gives +60% harvest; the model is a placeholder.
- Should harvesting use fuel (the 2D game's diesel) per cell?
- Grid cell size vs. performance: test on the Android build before picking a default.
- Does the yield stay `size`-based (2D: size x 550 to 650) or keep the current `size` number?

**Suggested order:** (1) generate the cell grid and show it with gizmos, (2) harvesting-mode toggle, (3) per-cell percentage payout with the harvester bonus, (4) progress shown on the pop-up and field reset at 100%, (5) fertilizer bonus once fertilizer exists, (6) visuals for cut cells, (7) touch button.

## Repo housekeeping
- [x] Android build output is no longer tracked (committed and pushed); `apk/` and `*_BurstDebugInformation_DoNotShip/` are in `.gitignore`.
- [x] **`UserSettings/` and `.utmp/` are no longer tracked** (2026-11-03). Both change on their own (editor layout and state, Android build cache), so they are in `.gitignore`. The files stay on disk and Unity recreates them. This replaces the earlier decision to track the editor layouts.
- [x] **`AllSkyFree` trimmed** from 321 MB to 35 MB (later one more sky removed). **Final set: 4 skyboxes**, each with only its material and six face images: `Epic_BlueSunset` (the one in use), `Day_BlueSky_Nothing` (in the `Cartoon Base BlueSky` folder), `Cold Sunset` and `Epic_GloriousPink`. `Cartoon Base NightSky` was removed. Decided by the owner on 2026-11-03; one or two may be deleted later.
- [x] **Terrain texture cleanup.** Removed the sample heightmaps and detail textures and the layers Snow, Sand, Rock, Tidal_Pools, Soil_Rocks, Muddy, Heather, Grass_Dry, Black_Sand and Pebbles_A. `Assets/TerrainSampleAssets/Textures` went from 1.9 GB to 505 MB. **Final kept layers:** `Grass_B` (in use on the terrain), `Grass_A`, `Grass_Moss` (possible extra grass), `Grass_Soil` (dirt path contender), `Pebbles_B` (dirt path contender) and `Pebbles_C` (likely gravel). Decided by the owner on 2026-11-03; one or two may be deleted later. The terrain data (`Assets/Terrain/New Terrain.asset`, binary) was checked for each removed layer before deleting.
- [x] **Old branches deleted** (2026-11-03): `unity-upgrade`, `claude/local-commit-sycf2r`, `claude/advanced-crop-harvesting-avgvmn` and `ccr-7bfcb41e-sg21oy`. Every commit on them was already in `main`, so nothing was lost. Only `main` remains. The advanced harvesting design is in this file ("Advanced crop harvesting").
- **Git history cleanup. STATUS (2026-11-03): DONE. Rewritten, tested by the owner in Unity (opens, plays, Android build works), and force-pushed to GitHub.** Result: 63 commits (was 103), `.git` 527 MB (was 2.0 GB), and the file tree is identical to before. 527 MB is about the floor, because the 6 kept terrain layers are about 505 MB. A full mirror backup of the old history is at `../Farming-Simulator-94-mirror-backup.git` (outside the project; delete it once the push is verified). Approved by the owner 2026-11-03. The deleted files are gone from the working folder, but the **`.git` folder is still about 2 GB**, because the old versions stay in the history, and a fresh clone downloads all of it. Shrinking it means rewriting history, which is the risky part. The owner does not need the old Unity 2022 history, so the plan is to cut it. Agreed plan:
  1. Done: terrain layers and skies are final, old branches deleted, `.utmp/` and `UserSettings/` untracked (see above). `git-filter-repo` is installed.
  2. Back up first with `git clone --mirror` to a folder outside the project.
  3. Run `git-filter-repo` once, doing two things: (a) remove from every commit all paths that were deleted during the project's life (old terrain textures, heightmaps, detail textures, removed skies, old Android build output) plus `.utmp/` and `UserSettings/`; (b) cut the history so the **first commit is the Unity 6 upgrade (was `464a5ee`, now `f48f6f9`, 2026-10-31)**, with its message reworded to "Unity 6 upgrade (earlier history removed)". The 32 earlier commits (the Unity 2022.3 era, Feb 2025 to the upgrade) are dropped, and 69 are kept. The big files were added in the very first commit, so the cut alone would not shrink the repo; both passes are needed.
  4. Try it on a copy first (a dry run) and check the new commit count and `.git` size before touching the real repo.
  5. Done: the old commit hashes quoted in this file were translated with filter-repo's commit map. Four references (the three terrain removal commits and the sky trim commit) are gone, because those commits only touched deleted files and became empty; they are described in words instead.
  6. Re-add the `origin` remote (`filter-repo` removes it). The tag `baseline-2022.3.55` is **not** recreated: it is deleted locally and on GitHub (a tag left on GitHub would keep the old history reachable).
  7. **The owner opens the project in Unity, builds and plays it before anything is pushed.** Nothing is pushed until the owner says so.
  8. Force push (`--force-with-lease`). Every commit hash changes. No one else may push to `origin` at the same time.
  9. Any other clone of the repo must be deleted and cloned again (the owner already removed the old laptop copy). GitHub may keep the old data for a while; contacting GitHub support removes it for sure.
  - It cannot be undone after the force push, so the mirror backup matters.
  - Git LFS only makes sense for large files we keep.
- Ask before shrinking anything.

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
- The 3D fields already work the same way: a trigger collider plus a "last entered" list (now kept per vehicle). **Buildings should use the same pattern**: a `Building` component with a trigger collider and a type enum (Sell, Buy, Paint, Dealer, Gas). The `USE` button and E key then do what the building says.
- **Economy numbers to reuse** (they were balanced for the 2D game): the prices above. Keep them as editable Inspector fields, not hard-coded.
- **Selling needs a drive** (already decided). The 2D game had one station per crop; for the first 3D version one sell point that buys all crops is enough.
- **Now in the 3D game:** money, sell stations, field ownership, buying and selling fields, seeds and the seed shop, the dealer and vehicle switching (untested).
- **Still not in the 3D game:** compost, diesel, painting, save/load, AI owners planting fields.
- **The harvester already exists in 2D as art** (`h/` folder, four colours). In 3D it still needs a model.
- **Suggested order for Phase 3:** ~~(1) money, (2) a sell building, (4) field buying~~ done; (3) seeds and a seed shop, ~~done~~; the owner now wants the dealer (7) before fuel (5) and fertilizer (6).

### Assets in the 2D repo that could be reused
- `backround.png` (3500x3000), the full map. A layout reference only; the 3D map is built fresh.
- Tractor, van and harvester sprites (four colours each) and `garage.png`, `tittle.png` (title screen).
- `Read Me.txt`, `Saves/*.txt` (save format).

