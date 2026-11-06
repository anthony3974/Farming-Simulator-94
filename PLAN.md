# Farming Simulator 94: Improvement List and Initial Plan

> ## >>> NEXT UP: AI owners' fields (item 10): built, owner testing; then other sounds (item 11) <<<
>
> **When asked "what's next?", the answer is the first unfinished item in this list** (set by the owner, updated 2026-11-05: the app icon, the engine sound and the harvester are done; 2026-11-06: save / load and fuel tested and working (owner); the touch rework is done and merged, phone test pending). The seed shop is finished (the owner may revise the economy later).
> 1. ~~**Map border.**~~ **DONE (2026-11-03).** Built with `Assets/Editor/MapBorderBuilder.cs` (Tools > Map Border Builder). The owner is happy with it, so the tool was removed (2026-11-04; git history, last version `9f800c5`, to rebuild or resize the wall). The scene has a `Map Border` object with 572 grey concrete blocks (scaled about 7.5 x 3.3 x 1.0 m, 0.3 m of it buried), each with a collider, 1 m inside the terrain edge. Nothing in the scene is near the wall. A real barrier prefab could be dropped into the tool (from git history) later. Checked by the owner in Play mode (2026-11-03): the truck cannot pass.
> 1. ~~**Car dealer.**~~ **DONE (2026-11-03).** Built in GameScene and tested in Play mode by the owner. The builder tool was removed afterwards (kept in git history, last version `3340302`). Owner's decisions (2026-11-03): the dealer is its **own new building**, a red L-shaped building on a grey concrete lot in the **top-left corner of the map**, the same place as in the 2D game (not the `House` or `Garage EX`). The **harvester is its own vehicle.** There are no vehicle models, so the van and the harvester are placeholders built from boxes. What was built:
>    - `Vehicle` (was `GameCharacterCar`): one per vehicle, holds the driving code and stats (top speed, harvest bonus, camera position, paint parts) and remembers which fields and buildings it is parked in. `GameCharacter` is now the player only: input, camera, and the vehicles you own. The unused walking mode (`drive` off) was removed; there is no getting out on foot.
>    - Keys **1 / 2 / 3** drive the tractor / van / harvester (if owned), touch button **CAR** picks the next one. The camera and speedometer move to that vehicle; the others stay parked where you left them.
>    - `Dealer` + `Building` type Dealer: press E / USE anywhere on the lot to open the shop menu (van and harvester, $5,000 each). **Red only (owner, 2026-11-03): the colour choice was removed.** The van and the harvester are prefabs (`Assets/Prefabs/Van.prefab`, `Harvester.prefab`, materials in `Assets/Vehicles`) standing on `Parking` by the house; buying one makes that vehicle yours where it stands. Made with the one-off Vehicle Prefabs tool, then removed (git history, `530c871`) (the old runtime `VehicleFactory` builder is gone; the names, prices and descriptions are in `VehicleCatalog.cs`). The dealer menu was built in code in `Dealer.cs`; it is now a scene object (`Dealer Menu`, see item 4).
>    - Van: top speed 62 km/h (tractor 50). Harvester: 38 km/h and **+60% harvest** (2D speed ratios 8 / 10 / 6). **All cut 10% (owner, 2026-11-05): tractor 45, van 56, harvester 34 (rounded from 55.8 and 34.2)** (on the prefabs; the speedometer still reads the tractor's top speed as its own). **Braking 15% stronger** (`brakeStrength` on Vehicle, 2.45; was 2).
>    - **The van cannot work fields (owner, 2026-11-03):** in the van, E / F in a field only show "The van can't work fields". **It can buy and sell fields with B / BUY/SELL (owner, 2026-11-05).** It still sells crops and buys seeds and fertilizer at the buildings, and opens the dealer.
>    - The dealer was built by `Assets/Editor/DealerBuilder.cs` (now only in git history, `3340302`) from the 2D map rectangles (the 3D terrain is the 2D map turned on its side: 2D up = +X, 2D right = -Z), which also added a `Vehicle` to the truck.
>    - **Shop menu checked on a phone by the owner (2026-11-04): looks good.**
> 2. ~~**Home menu.**~~ **DONE (2026-11-04, branch `home-menu`): main menu and pause menu built in the scenes and working (owner).** Owner's decisions (2026-11-04): a **separate `MainMenu` scene**; the background is a **game screenshot** the owner will take; **no Quit button**; **version number on the home menu only** (from `Application.version`, Project Settings > Player > Version, now 0.5); a **TERMS OF SERVICE button that does nothing yet** (a reminder for Google Play, link it with `Application.OpenURL` when publishing). What was built:
>    - **The menu is real objects in the `MainMenu` scene** (see the rule below the NEXT UP box); `MainMenu.cs` only switches screens and runs the buttons. Home screen: screenshot background (fills the screen, cropped not stretched, darkened by the `Darken` object), gold title, big PLAY, small TERMS OF SERVICE bottom-right, version bottom-left. PLAY opens a Play panel: NEW GAME (loads GameScene), LOAD GAME (live since save / load, item 8, 2026-11-06; greyed out when there is no save), BACK (Escape / Android back also goes back). Kept inside the phone safe area.
>    - `Assets/Editor/MainMenuBuilder.cs` (Tools > Build Main Menu Scene, run by the owner, then removed: git history `8ae0ee6`) created `Assets/Scenes/MainMenu.unity` with the whole menu (camera, EventSystem, canvas, screens, buttons) and put it first in Build Settings, GameScene second.
>    - To change the background: drag a screenshot into the **Texture** of `Main Menu > Background > Screenshot`. Still worth checking the layout on a phone.
>    - **Pause menu (2026-11-04, same branch, working).** Owner wants it as **real objects in the scene hierarchy** so it can be rearranged in the editor (see the rule below the NEXT UP box). `Assets/Editor/PauseMenuBuilder.cs` (Tools > Build Pause Menu, run by the owner, then removed: git history `a4b97f9`) added a `Pause Menu` canvas: PAUSE touch button (top-left beside HELP, touch only), and a `Menu` part with PAUSED, RESUME, MAIN MENU (was: press twice, the farm is not saved; since save / load, 2026-11-06, it saves and leaves) and an "Esc to resume" hint. Plain objects, no layout groups. `PauseMenu.cs` only shows/hides it and runs the buttons; Esc toggles it, game time stops while paused (`Time.timeScale` 0). The `Menu` part stays visible in the editor and hides itself on Play (the later help and dealer menus are turned off in the editor instead). Esc no longer just frees the mouse: it pauses (which frees it), and an Esc that closes the dealer does not also pause (`InputManager.MenuJustClosed`). Help list: "Pause (releases the mouse) / Esc / PAUSE".
> 3. ~~**Look around on phones.**~~ **DONE (2026-11-04): tested on the phone by the owner, the drag feels good.** A drag-to-look area so the camera can tilt on touch (mouse look needs a locked cursor, which touch mode never has). See Android port item 4.
>    - Built with Tools > Build Touch Look Area (run by the owner, then removed: git history, `58931f8`). To tune it: `touchLookSensitivity` or `invertTouchLook` on `GameCharacter`.
>    - Owner's decision: **tilt only, like PC** (drag up / down; no turning left / right). The tool adds a `Touch Look` canvas (sort order -100, below every other canvas so buttons and panels get the touch first) with an invisible full-screen `Look Area` + `TouchLook.cs`. `TouchLook` sends drags (in screen heights) to `InputManager.AddTouchLook`; `GameCharacter` tilts by `touchLookSensitivity` (120 degrees per full-screen drag) within the same pitch limits as the mouse, `invertTouchLook` flips it. One finger at a time, so another can hold GAS. Ignored while a menu is open (pause, dealer). Turned off on PC unless Show Touch Controls is ticked. Help list: "Look up / down / Mouse / DRAG".
> 4. ~~**Move the UI still built in code into the hierarchy**~~ **DONE (2026-11-04) except `TouchControls`, which moves with the touch rework (item 20).** (see the rule below). `HelpMenu`, the `Dealer` shop menu, the message box and the `Speedometer` are now scene objects, each made by a one-off tool that was then removed. Buttons are copies of the `UI Button` prefab.
>    - ~~**HelpMenu.**~~ **DONE (2026-11-04): built in GameScene (`56167c5`) and tested by the owner. The tool was removed (git history, `58fad2d`).**
>      - The `Help Menu` canvas (sort order 60) holds `Menu` (full-screen dimmer, a tap closes it; turned off in the editor, tick it to work on it) with two plain-positioned panels: `Keyboard Panel` (PC) and `Touch Panel` (touch controls on; the debug row is left out). The help text now lives in the scene (each `Row` has `Action` and `Control` texts), not in code. `HelpMenu.cs` only shows the right panel and opens / closes it; `InputManager` no longer adds it. With Show Touch Controls ticked in the editor you now see the touch panel (before: both columns). No buttons on it, so no `UI Button` copies.
>    - ~~**Dealer shop menu.**~~ **DONE (2026-11-04): built in GameScene (`4db11ce`) and tested by the owner ("dealer is good"). The tool was removed (git history, `47e5a43`).** A bug found while testing was fixed (`d5b49fa`): leaving Play mode threw a MissingReferenceException from `Dealer.OnDisable`.
>      - The `Dealer Menu` canvas (sort order 60) holds `Menu` (turned off in the editor, tick it to work on it) > `Dimmer` (tap closes) and `Border > Panel` with Title, Money, one `<Vehicle> Row` per vehicle for sale (Name, Description, `Buy Button` = a `UI Button` prefab copy) and two footers (Keyboard / Touch, the Dealer shows the right one). Plain positioned, no layout groups. The tool fills in the menu slots on the scene's `Dealer` (Menu Root, Close Button, Money Text, footers, Rows). `Dealer.cs` no longer builds anything; it opens / closes the menu, writes the money and BUY / OWNED (texts in the Inspector: Money Format, Buy Format, Owned Text) and buys. Prices still come from `VehicleCatalog.cs`; the names and descriptions now live in the scene. The old `dealerName` field is gone (the title is a scene text). OWNED greys the button's Fill (70, 70, 70) and turns the button off.
>    - ~~**Message box and speedometer.**~~ **DONE (2026-11-04): built in GameScene and tested by the owner ("they look great"). The tool `HudBuilder.cs` was removed (git history, `8fdfbc3`); run it again from there to redraw the dial after changing Dial Max Kmh.**
>      - `Message Box` canvas (sort order 50, no raycaster so it never blocks a touch) + `ScreenPrompt`: `Box` (yellow border, screen centre, turned off in the editor) > `Panel` (brown, inset 4) > `Text`. `ScreenPrompt` moved out of `Building.cs` into its own `ScreenPrompt.cs` (Unity needs the file name to match to keep it on a scene object). It finds the scene box through `ScreenPrompt.Instance` (same calls as before) and, with Fit Height To Text, sets the box height to fit the text (one or two lines); width and position stay as set. With no box in the scene it logs one error and sends messages to the Console only.
>      - `Speedometer` canvas (sort order 40, no raycaster) + `Speedometer`: `Dial` (bottom-left) > `Numbers` (0 to 100), `Needle`, `Hub`, `Number`. The dial face and hub are PNGs the tool draws into `Assets/Images/Speedometer` (same look as before); the tool (now in git history) keeps the existing Dial Max Kmh when run again. `Speedometer.cs` only turns the needle and writes the number (Number Format in the Inspector); `GameCharacter` finds it in the scene instead of adding it to the tractor.
> 5. ~~**Tractor redesign.**~~ **DONE (2026-11-04, `f3feb95`): the owner ran Tractor Builder V2, "looks amazing", and deleted the old model from the Tractor in GameScene.** The old `Model` was then deleted from `Tractor.prefab` too (owner, 2026-11-04), so only `Model V2` is left. The tool was removed (2026-11-04; git history, `1cfcae2`). Original notes: **(owner, 2026-11-04).** The current tractor (Tractor Builder, `8c1c8b6`) is better than the original but has **too many gaps and floating parts**. Redo it so every part touches or overlaps its neighbour (no visible gaps, nothing hovering), as a prefab the owner can edit in the hierarchy. **Owner (2026-11-04): keep the old tractor, make everything look connected.**
>    - What Tractor Builder V2 did (`Assets/Editor/TractorV2Builder.cs`, removed, git history `1cfcae2`): added a `Model V2` beside the old `Model` in `Tractor.prefab`; the old one is **only turned off, not removed**, and the buttons **Use new (V2)** / **Use old** switch between them. Running it again rebuilds only `Model V2`. Same look, same layout and the same `Assets/Tractor` materials, but every part overlaps the one it hangs on (a few cm) and they all join the frame: the axles run into the wheel centres (a mount joins the front axle to the frame), the hood and cab base sit on the frame (the hood runs into the cab base), the fenders join the cab sides with skirts in front of and behind the wheels, the corner posts run from the cab base into the roof, and the grille, lights, exhaust (on a collar), roof beacon and hitch are set into their parts. All four tires touch the ground. The `Vehicle` paint list follows the model in use. The owner picked V2; the tool and the old `Model` are gone.
>    - **Fixed (`0fa0a5d`):** front wheel steering also picked up tires from a turned-off model and moved them onto pivots outside it, so they showed again. Only active tires steer now.
>
> ~~**Side task: harvester redesign (owner, 2026-11-05).**~~ **DONE (2026-11-05): the owner ran Harvester Builder V2 and committed the new harvester (`1b336a3`). The tool was removed (git history, `0c610f7`: `HarvesterV2Builder.cs`, `HarvesterV2Model.cs`).** The old turned-off `Model` (12 boxes) was deleted from `Harvester.prefab` (2026-11-05); only `Model V2` is left. The tractor and van looked much better than the harvester (12 loose boxes), so it was redone like Tractor V2.
>    - [x] **Owner:** open GameScene, **Tools > Harvester Builder V2**, press **Build new harvester**, save the scene, commit the new `Assets/Vehicles/HarvesterGrain.mat` it makes. Look at it parked by the house and drive it (3, after buying it). **Use new (V2)** / **Use old** switch back and forth. If happy: delete the old `Model` from `Harvester.prefab` (prefab mode), delete `Assets/Editor/HarvesterV2Builder.cs` and `HarvesterV2Model.cs` (kept in git history) and mark this done. If not, say what to change.
>    - What the tool does: on the first run it gathers the old 12 boxes (they sat straight under the Harvester) into a `Model` object and turns it off; it is not deleted. It adds `Model V2` (122 parts, in groups Body, Wheels, Cab, Grain Tank, Unloading Auger, Ladder, Engine, Header) beside it. Running it again rebuilds only `Model V2`. The parts are in `HarvesterV2Model.cs` (one line per part, with comments giving what each part overlaps), the window and the prefab switching in `HarvesterV2Builder.cs`.
>    - The model: a combine harvester. A 6 m header in front (cutting table with a cutter bar, a cross auger with flights, a yellow reel on two arms, pointed crop dividers), the sloped feeder house up into the body, big drive wheels under the cab, a cab with posts, roof work lights, beacon and mirrors, the grain tank behind it (metal rim, grain inside), the engine cover with exhaust and air intake at the back, a louvred radiator grille, straw spreader, tail lights. The unloading auger lies folded along the left side, and a ladder and walkway with a hand rail go up the left side to the cab. Same rule as Tractor V2: every part overlaps the one it hangs on, all chains end at the body, all four tires touch the ground. Materials: the harvester's own (`Assets/Vehicles`) plus the tractor's tire rubber and yellow hubs so the two match; only `HarvesterGrain` is new. The `Vehicle` paint list is set to the `Paint ...` parts.
>    - **Steers with the back wheels**, like a real combine: new `Steer Rear Wheels` tick box on `Vehicle` (the BlTire / BrTire tires turn, the other way to the front ones). The tool ticks it for V2 and unticks it for the old model (front steering). The tractor and van are unchanged. The back tires sit outside the body, so they do not cut into it at full lock.
>    - The Harvester's own box collider, controller and camera offset are unchanged; the header has its own box collider (6 m wide) so a parked harvester's header is solid too.
>    - Checked outside Unity only: the model code was compiled against stand-in Unity classes, every part checked to touch its neighbour (none floating) and rendered for a look. **Not yet seen in Unity.**
>
> 6. ~~**App icon (owner, 2026-11-04).**~~ **DONE (2026-11-05): the owner checked it on the phone, "app icon good".** `Assets/Editor/AppIconSetter.cs` was removed (2026-11-05; git history `77c80f2`; run it again from there after redrawing the icon with `Art/Icon/make_icon.py`). The phone shows Unity's default icon. Make a Farming Simulator 94 icon (square, at least 1024 x 1024 PNG; Android also wants an adaptive icon: a foreground layer and a background layer) and set it in Project Settings > Player > Icon (Android and Default). Needed before publishing on Google Play (512 x 512 for the store listing).
>    - [x] **Drawn and set (2026-11-04):** the owner ran Tools > Set App Icons (`77c80f2`): default icon plus Android adaptive, round and legacy (6 sizes each).
>    - [x] **Test the icon on the phone (owner):** done 2026-11-05, looks good. build to the phone (version 0.5.1) and look at the icon on the home screen and in the app list (the phone cuts it to its own shape). If it looks good, delete `Assets/Editor/AppIconSetter.cs` and mark item 6 done; if not, say what to change and redraw with `Art/Icon/make_icon.py`.
>    - Details: The icon is the game's red tractor (side view, the `Assets/Tractor` colours, gold "94" on the hood) on a brown field under a blue sky with a gold sun. Drawn as SVG by `Art/Icon/make_icon.py` (outside `Assets`, so Unity does not import it; run `python3 Art/Icon/make_icon.py` to redraw every size after a change). Outputs: `Assets/Images/Icon/` `IconBackground.png` + `IconForeground.png` (432, Android adaptive layers; the tractor stays inside the 66 / 108 safe circle so no phone mask cuts it), `Icon.png` (1024, default and Android legacy) and `IconRound.png` (1024, Android round); `Art/Icon/PlayStoreIcon.png` (512) is for the Play Console listing. `AppIconSetter.cs` puts them into the Player settings and sets their import settings (uncompressed, no power-of-2 resize).
>
> 7. ~~**Engine sound (owner, 2026-11-04).**~~ **DONE (owner, 2026-11-05: "engine sounds are good").** Right after the app icon: look at what the Unity Asset Store has for engine sounds (check the licence allows use in a published game) before making or recording anything. One engine sound per vehicle (tractor, van, harvester), pitch following speed. Ask the owner before downloading or buying a pack.
>    - **Own branch (owner, 2026-11-05):** do the engine sound on a separate new branch (not the harvester branch `claude/gifted-brown-go9x1m`), so it can be tested and merged on its own.
>    - **Debug numbers on screen (owner, 2026-11-05):** while tuning, show the sound values live on screen: at least the **pitch**, plus speed (km/h) and volume, for the vehicle being driven. Easy to turn off (an Inspector tick box) so it never shows in a normal build.
    - **DONE (2026-11-05): owner ran the tool and tested it: "engine sound is amazing". Merged into main from branch `engine-sound`.** Show Debug is now off on the three prefabs (2026-11-05; tick it on a prefab's Engine Sound to see the numbers again). `Assets/Editor/EngineSoundSetup.cs` was removed (2026-11-05; git history `d58093e`). Tune on each prefab's Engine Sound.
    - Built on branch `engine-sound` (2026-11-05). The sounds are the owner's picks in `Assets/Sounds` (see `Assets/Sounds/AUDIO_NOTES.md`), not an Asset Store pack.
      - [x] **Owner:** in Unity, **Tools > Engine Sound Setup** (no scene save needed: it changes the three prefabs), then Play and drive each vehicle and listen. Tune on each prefab's `Engine Sound` (in Play mode for live changes, then copy the values back). When happy: untick **Show Debug** on the three prefabs, commit the new `.meta` files and the prefabs, delete `Assets/Editor/EngineSoundSetup.cs` (git history), merge the branch.
      - `EngineSound.cs` (on each vehicle): two loops play together, an idle loop (loudest standing still) and a drive loop that fades in with speed; both pitches rise with speed (km/h / top speed), holding the gas adds a little pitch and volume. Only the driven vehicle is heard (parked ones fade out in `fadeTime`); paused = silent. **Show Debug** draws the vehicle, speed, gas, and pitch and volume of both loops on screen (left side; move it with Debug Position).
      - Starting tune: tractor = slow idle (`DieselIdle1Loop`) + drive loop; van = quicker idle (`DieselIdle2Loop`), pitched up, a bit quieter; harvester = slow idle pitched down. All share `DieselDriveLoop`.
      - The two freesound MP3s had ~60 ms of silence at the loop point, so `Art/Sounds/make_loops.py` makes trimmed, crossfaded mono WAVs in `Assets/Sounds/Loops` (originals unchanged). `Vehicle` got `SpeedKmh` and `Throttle` for the sound. Compiled outside Unity only; **not yet heard in Unity.**
      - Still missing (AUDIO_NOTES): engine start, shut-down and rev-up sounds.
>
> **Order set by the owner (2026-11-05):** ~~save / load~~ (done), ~~touch rework~~ (done, phone test pending), AI owners' fields (built, owner testing), other sounds, ~~fuel testing~~ (done), street lamps, minimap.
>
> 8. ~~**Save / load.**~~ **DONE (2026-11-06): tested by the owner, "save and load works".** (Owner, 2026-11-05: first.) Save and load the farm: money, seeds, fertilizer, fuel, crops in stock, which fields you own and what grows on them (crop, stage, fertilizer), the vehicles you own and where they are parked. The main menu's LOAD GAME button is now live. Code only, no scene changes: the scripts compile outside Unity against stand-in Unity classes; **not yet run in Unity.**
>    - [x] **Owner:** Play from `MainMenu`: NEW GAME, plant / fertilize a field, buy a field, buy the van, park it somewhere, drive off in it, sell some crop. Pause, MAIN MENU, LOAD GAME: everything should be as you left it, in the van. Also: close the app on the phone (swipe it away) and LOAD GAME. Then NEW GAME (asks "Press again to replace your saved farm") gives a fresh farm.
>    - **`SaveGame.cs`** (new) on the `PlayerValues` object; `PlayerValues` adds it in Play mode if it is missing (add it in the editor to change its settings: Auto Save Seconds, Show Loaded Message). One save: `farm.json` in `Application.persistentDataPath` (JSON, readable; written to a `.tmp` first, then swapped in). A file that cannot be read is kept as `farm.json.bad` and a new farm starts.
>    - **When it saves (owner can change): by itself, no SAVE button**: every 60 s (real time), when you pause, when you press MAIN MENU in the pause menu (it saves and leaves; only if the save fails it asks for a second press, "NOT SAVED! PRESS AGAIN"), and when the app goes to the background or closes (`OnApplicationPause` / `OnApplicationQuit`: phones close background apps without asking). A SAVE button in the pause menu could be added (a `UI Button` prefab copy) if wanted.
>    - **What is saved:** money, seeds, fertilizer, fuel, wheat / barley / oat in stock, the selected crop (C); every field: name, owner, crop, growth stage **and the time into that stage** (growth is now a timer in `FieldController.Update` instead of a coroutine, so it can be saved; same speed, still stops while paused), fertilizer level and the stage it was last fertilized in; the vehicles you own (position, rotation) and which one you drive. Not saved: the camera tilt, half-done prices (seed / fertilizer quotes are rolled again), a running fuel pump.
>    - **Fields are matched by their place in the hierarchy** (for example `Terrain/Fields/Field Tony 1`), not by name, because AI fields get random names each new game; the saved name and owner are put back. A field added to the scene later keeps its new-game state; a renamed or moved field loses its saved state (it starts fresh).
>    - **Loading:** LOAD GAME sets `SaveGame.LoadRequested` and opens GameScene; one frame after the scene starts (after every other `Start`), the save is put back over the new-game farm and "Farm loaded (saved <date>)" shows (or the low-fuel warning). Vehicles are moved with `Vehicle.PlaceAt` (the CharacterController is turned off while moving it).
>    - **NEW GAME** with a save: the first press shows "Press again to replace your saved farm" under the buttons; the new farm's first save replaces the old one.
>    - **Editor note:** pressing Play in GameScene directly (not from `MainMenu`) starts a new farm, and its autosave replaces the editor's save (the phone's save is separate).
>
> 9. ~~**Touch controls rework.**~~ **DONE in the editor (2026-11-06, branch `touch-rework`, merged into main); phone test pending.**
>    - [x] Built in GameScene by the owner (2026-11-06, `3b61b22`), rebuilt after the layout and pedal changes.
>    - [x] **Owner:** ran Tools > Build Touch Controls (last rebuild 2026-11-06, `85746c5`), saved, deleted the builder (git history, `a94cdbb`); merged into main and pushed. **Still to do: try it on the phone** (WHEEL, SLIDER, pedal SLIDER, ROUNDED) and say what to change.
>    - [x] **Owner (done 2026-11-06):** Tools > **Help: Touch Settings Row** (`Assets/Editor/HelpTouchRowsFix.cs`), save, delete the file: adds a SETTINGS row to the help menu's touch panel (the panel grows one row) and the touch pause row says just "Pause". The touch steer and drive rows now name what SETTINGS picked (`< / >`, WHEEL or SLIDER; GAS / BRAKE or PEDAL SLIDER), set by `HelpMenu` when it opens.
>    - **Touch buttons are scene objects now** (`Touch Controls` canvas, sort 50): every button is a `UI Button` prefab copy with `TouchButtonInput` (the old hold / slide logic, unchanged). `TouchControls.cs` no longer builds anything: it hides the canvas on PC (unless Show Touch Controls), shows the picked steering and sets the button shape. `InputManager` no longer adds it. **Layout (owner, 2026-11-06), right side:** `CROP | CAR` / `BUY SELL | GAS` / `USE | BRAKE` (bottom row lowest). BUY/SELL is now "BUY" over "SELL", no slash (messages and the help say BUY SELL). **The held buttons (`<` `>` GAS BRAKE) sink while pressed** (0.9 scale, `pressedScale` on `TouchButtonInput`; the touch area stays full size).
>    - **Steering options** (STEERING column): `< >` buttons (as before); **Wheel** (`SteeringWheel.cs`: turn it with a finger around its centre, 120 degrees each way = full lock, springs back when let go; a plain picture that only turns, no shadow or press-down look (owner, 2026-11-06)); **Slider** (`SteeringSlider.cs`: the knob follows the finger, distance from the middle = how hard you steer, springs back to the middle when let go, small dead zone). The wheel and slider steer gently: `InputManager.Steer` (-1 to 1) and `Vehicle` turn by that fraction (the tires too); keys and buttons still give full steering.
>    - **Button shape** (BUTTON SHAPE column): square or **rounded** (9-sliced `Assets/Images/Touch/RoundedBorder.png` / `RoundedFill.png`, 24 px corners; drawn by `Art/Touch/make_touch_sprites.py`, which also draws `SteeringWheel.png`). Applies to every touch button, the slider and the PAUSE button (list `Shaped Parts` on Touch Controls).
>    - **SETTINGS is its own panel, not the pause menu (owner):** `Settings Menu` canvas (sort 58) + `SettingsMenu.cs`. The game keeps running; only driving and the game keys stop while it is open (`MenuOpen`, like the dealer). CLOSE, a tap outside, or Esc / Android back closes it. Choices show at once and are remembered on the phone (`TouchSettings.cs`, PlayerPrefs). The picked option has the pressed fill (110, 85, 10). **PEDALS (owner, 2026-11-06):** GAS / BRAKE buttons or a **pedal slider** (`PedalSlider.cs`) in their column (from BRAKE's bottom to GAS's top, 130 x 270): push up for gas, down to brake (then reverse), springs back to its rest point **20% up the bar: 80% of the travel is gas, 20% brake (owner, 2026-11-06; `Rest Point` on the slider, the Middle mark follows it)**; picking it hides the GAS and BRAKE buttons. Part gas drives up to that part of the top speed (eases off when lowered), part brake brakes that much less (`InputManager.Throttle` -1 to 1, `Vehicle` gas / brake); keys and buttons stay full. The two sliders share `TouchSlider.cs` (`SteeringSlider` left / right, `PedalSlider` up / down). Settings right column: BUTTON SHAPE (SQUARE | ROUNDED) above PEDALS (BUTTONS | SLIDER).
>    - Compiled against the real Unity 6 assemblies (Unity's own Roslyn) outside the editor; **not yet run in Unity.**
>
>    Original notes: **Touch controls rework (owner, 2026-11-05: second), including the new SETTINGS button.** See item 20 under Improvements needed: the buttons are a little confusing; move `TouchControls` into the scene hierarchy; the SETTINGS button below HELP (rounded buttons, other steering options such as a steering wheel with a pressing-down animation). The slide-off fix is done and tested.
>
> 10. **AI owners work their own fields: built (2026-11-06, branch `ai-fields`), owner testing.**
>    - [ ] **Owner:** NEW GAME and drive around: the AI fields start in all states (empty, green, yellow, ready, some fertilized earlier). Watch one: an empty AI field gets planted after 15 to 60 s, grows at your speed, sometimes turns blue (fertilized), and is harvested 10 to 45 s after it is ready. Buy a growing one and harvest it yourself. Sell one of yours: the new AI owner keeps working it (a ready crop on it gets harvested by them).
>    - Code only (`FieldController`): `AiRandomStart` (a new game puts each AI field at a random stage with a random crop and time into the stage, sometimes fertilized once in an earlier stage, so it does not start blue), `AiWork` (plant an empty field after Min / Max Ai Plant Delay, harvest a ready one after Min / Max Ai Harvest Delay, and in each growth stage fertilize with Ai Fertilize Chance 25%, two levels at most; game time, so it stops while paused). The AI harvest goes to the AI owner (nothing to your counters). Settings under "AI owners" on each field. Saved like your fields (crop, stage, time, fertilizer); the AI's wait starts again after loading. Compiled against the real Unity assemblies; **not yet run in Unity.**
>
>    Original note: **AI owners work their own fields (owner, 2026-11-05).** The AI owners (Fred, Maria, Hank, Dolores, Gus) plant, grow and harvest the fields they own, like the 2D game's AI owners planting; the fields then look alive and change crop over time, and you buy a field with whatever is growing on it.
>
> 11. **Other sounds (owner, 2026-11-04; moved down 2026-11-05).** Only the engines have sound so far. Ask the owner which sounds and where they come from before starting. Likely list: button clicks in the menus, planting / harvesting / fertilizing, money in and out at the shops and sell stations, and maybe background music and ambient farm noise. A volume setting (pause menu) could come with it.
>
> 12. ~~**Fuel and the gas station.**~~ **DONE (2026-11-06): tested by the owner, "fuel works".** (owner, 2026-11-05: "all vehicles share the same fuel amount like in the 2D game"). Built in GameScene (owner ran both tools, 2026-11-05); the tools were removed (git history: Gas Station Builder `ad4b1a4`, Fuel Gauge Builder `d7abd45`).**
>    - [x] **Owner:** test the slow filling and the pump prices (done 2026-11-06; the fuel gauge look can still be changed later).
>    - **Fuel:** one diesel tank in `PlayerValues` (`Fuel`, capacity 100, start 100), shared by the tractor, van and harvester. Each `Vehicle` burns `fuelPerKm` (10) litres per km actually driven (1 world unit = 1 m, the map is ~1 km), so a full tank is ~10 km, ~6 min at the tractor's (now 45 km/h) top speed (2D: 1 unit per ~200 movement ticks). No fuel used standing still. "Low fuel" message once at 20%.
>    - **Empty tank: the vehicle only crawls (`emptyTankSpeed`, 15% of top speed) instead of the 2D game over, so you can still reach the station. Owner, 2026-11-05: fine for now** (may become game over later).
>    - **Filling up is slow, like the 2D game (owner, 2026-11-05):** E / USE at the pumps starts filling at `fuelPerSecond` (4 L/s, an empty tank takes 25 s), paying `fuelPrice` ($2 per litre) as it goes; the message box shows the litres and cost so far. **E / USE again stops it.** It also stops by itself when the tank is full, the money runs out, the vehicle drives off (over 2 km/h) or leaves the zone, or you switch vehicle. A message then says how much was bought.
>    - **Price on the pumps only (owner, 2026-11-05):** the 8 pump displays (`Display Numbers`, 2 per pump) show "$ 2.00 / L" from `fuelPrice` (so they always match), and the litres while filling. The big sign by the road shows no price. Set on the station's Triggerbox `Building`: Pump Displays (empty = found by name), Pump Price Format.
>    - **Jerrys Gas Station** (designed by a sub-agent): `Terrain > Buildings > Gas Station` (Model + Triggerbox with the Building, type Gas), lot X 6 to 56, Z 568 to the road edge (~631.5), open to the road. A 28 x 16 m canopy (6.2 m high, harvester fits) over 2 pump islands with 4 diesel pumps, a kiosk "JERRYS SHOP", a 10.5 m sign pylon by the road, drums, tyres. Colliders only on the kiosk, canopy posts, pumps, sign posts and the 5 cm slab. 14 Standard materials in `Assets/GasStation/`. **Owner changes (2026-11-05): the trigger is a bit bigger (Z scale 1.28), and the station's two street lamps were removed** (see the map lamps below).
>    - **Fuel gauge** (`FuelGauge.cs`): `Fuel Gauge` canvas (sort 40, no raycaster): yellow-bordered brown box just above the speedometer (x 20, y 290, 260 x 64): "DIESEL" label, litres number, a bar green > 50%, yellow > 20%, red below; blinks when empty.
>
> 13. **Street lamps across the map (owner, 2026-11-05).** One street lamp design used across the whole map (roads, buildings, the gas station, whose own lamps were removed for this).
>
> 14. **Minimap (owner, 2026-11-05, last).** A small map in a corner showing the vehicle you drive, the fields and the buildings, for example a top-down camera into a RenderTexture or a drawn map image with markers.
>
> ~~**To fix (owner, 2026-11-05): the tractor's camera is too close**~~ **Fixed (2026-11-05):** `cameraOffset` on `Tractor.prefab` `(0, 5.36, -6.53)` to `(0, 5.36, -8.5)`, the same distance back as the van (its `(0, 3.9, -6.64)` x its 1.28 scale = `(0, 5, -8.5)`); the harvester's is `(0, 8, -11)` (bigger vehicle). **Owner checked (2026-11-05): "solid, done".**
>
> **Distant, not a priority (owner, 2026-11-04):** working lights on the house and garage.
> **Dropped (owner, 2026-11-04):** the paint shop. Vehicle colour does not matter.
>
> **Building and tractor redesigns (2026-11-03, built and saved in GameScene).** The tools were run, then removed (kept in git history): Tractor Builder `8c1c8b6` (truck renamed to tractor, new red tractor model, front wheel angle 30 to 20), Sell Station Builder `d60593b` (wheat / barley / oat grain elevators), Shop Builder `10298e7` (new seed shop look, and a new **fertilizer shop** at X 340, Z 601 next to the seed shop because the 2D spot is taken by fields). **Shop amounts (owner, 2026-11-03): both shops sell 2,000 at a time, same price per unit:** 2,000 seeds for $900 to $1,100 and 2,000 fertilizer for $800 to $1,000. The `Building` fields were renamed (`seedsPerBuy`, `minSeedPrice`, `fertilizerPerBuy`, ...) so the old Inspector values in the scene are ignored.
>
> **Tests and tidy (updated 2026-11-04).**
> - [x] **Van in a field:** E, F and B only show "The van can't work fields" (owner, 2026-11-04). B (buy / sell the field) works in the van since 2026-11-05.
> - [x] **Growth time:** about 83 s per crop (4 x 20.79 s); owner trusts the numbers, not timed (2026-11-05). **+10% (owner, 2026-11-05)**, was about 75 s (4 x 18.9 s). The field is now `growthStageSeconds` (renamed so the 18.9 saved in the scene is ignored).
> - [x] **Van number plate:** Overrides > Apply All done by the owner, `VehiclePlate.mat` deleted (2026-11-04).
> - [x] **Van scaled up** by the owner (scale 1.2808 on the scene copy). The camera then sat 1.28x further back (the camera is a child of the van, so it scales too); `cameraOffset` on `Van.prefab` was divided by 1.28 (`(0, 5, -8.5)` to `(0, 3.9, -6.64)`), so it sits as far away as before. **Owner to check it feels right**, adjust `cameraOffset` on the Van if not.
> - [x] **Unused assets deleted:** `House.prefab`, `houseTop.fbx`, `Garage EX.prefab` (2026-11-04).
> - [x] **Phone check:** the HELP and PAUSE buttons (top-left) look good on the phone (owner, 2026-11-04).
> - [x] **Touch look on the phone:** the drag feels good (owner, 2026-11-04).
> - [x] **Old tractor `Model` deleted from `Tractor.prefab`** by the owner in prefab mode (2026-11-04); only `Model V2` is left.
>
> **Fixed (2026-11-04):** ~~**On phones the grass looks like water.**~~ The two layers the terrain uses (`Grass_B`, `Pebbles_B`) took smoothness from the texture's alpha, but those textures are imported with **no alpha**, so alpha reads as 1 = fully glossy, and the sky reflects off the ground like water. Both layers are now **Smoothness Source: Constant** (smoothness 0). **Confirmed on the phone by the owner.** Android stays on Medium quality (High would mainly add soft shadows, at a frame-rate and battery cost).
>
> **Tested by the owner in Play mode (2026-11-03):** fertilizer (buy, F twice in two stages, harvest bonus), buying the van and harvester from Parking and driving them with 2 / 3, the 2,000-unit shops, mouse look on PC. Cleaned up in the scene by the owner: the turned-off old `House` and `Garage EX` and the dealer's old parking-spot objects are deleted (their prefabs were deleted 2026-11-04).
>
> **Added (owner, 2026-11-06), not yet in the order above:** fertilizer colour for fields; fertilizing (E on PC, USE on touch) changes the field's colour, so you can see it is fertilized. See item 15 under Unfinished features.
>
> **Also pending (owner, 2026-11-03):** rework the touch buttons (they are a little confusing, see item 20). Ask the owner when. The field sell confirmation text is fixed ("Press again to confirm").
>
> **To continue in a new chat:** "Read PLAN.md and start the NEXT UP task."

## UI colours (owner, 2026-11-04): keep them the same everywhere
HUD and buttons: **brown fill `(60, 45, 20)` opaque, yellow border `(255, 204, 0)`, gold text `(255, 201, 51)`**. Buttons are a yellow square with the brown fill inset 5 px (the touch-button style); not an Outline effect. **Every button is a copy of the prefab `Assets/Prefabs/UI/UI Button.prefab`** (owner, 2026-11-04: a prefab guarantees they match and can be restyled in one place). New buttons: drag in the prefab, then change only the name, place, size and text. Change colours or the border in the prefab, not on one button. Made with a one-off UI Button tool (git history, `d203505`); the pause menu and main menu buttons are prefab copies (done 2026-11-04). The touch buttons are prefab copies too since the touch rework (item 9, 2026-11-06). Pop-up panels (help, dealer, menus) are dark grey `(31, 31, 31)` with the same buttons on them. Pressed touch button: `(110, 85, 10)`. Greyed-out button: `(70, 70, 70)`.

## Rule: build things in the scene hierarchy, not in code (owner, 2026-11-04)
New objects (menus and their canvases, buildings, vehicles, anything visible) go **into the scene hierarchy or a prefab as real objects**, so the owner can select, move, resize and restyle them in the editor. Do **not** build them from code at runtime (the tractor felt like that and the owner did not like it). Scripts only hold references to those objects (Inspector fields) and run the behaviour.
- The usual way: a one-off editor tool (Tools menu) that creates the objects in the open scene, fills in the references and is then deleted (kept in git history).
- Use plain positioned objects for UI (no layout groups) unless asked, so dragging things around works.
- Nothing is built in code any more once Tools > Build Touch Controls has been run (touch rework, item 9, 2026-11-06).

Unity 6000.0.85f1. Scenes: `Assets/Scenes/MainMenu.unity` (opens first), `Assets/Scenes/GameScene.unity`. Scripts in `Assets/Scripts`.
This is an initial plan from reading the code. It is not the plan from the earlier chats. Edit it as you remember more.

## Improvements needed

### Current bugs*
(*Not a real bug yet, a limit we will hit once there are enough fields. Not numbered so the item numbers below and the references to them stay valid.)
- ~~**Field names run out.**~~ **FIXED (2026-11-03).** The 14 fields in the scene (2 the player's, 12 AI) used all 12 names from the 2D game. Now, once the list is used up, an AI field gets a random base name with the next free number ("Dahl Ranch 2", "Dahl Ranch 3", ...), so more fields can be added. (A player field's scene name that is also in the list is still not reserved; minor.)


### Unfinished features
15. **Fertilizer** (written and tested by the owner, 2026-11-03): press **E** on PC (touch: **USE**) in your own field while the crop grows. **The F key was removed (owner, 2026-11-06): on PC, E plants an empty field, fertilizes a growing one and harvests a ready one.** **USE fertilizes on phones too, and the FERT button was removed (owner, 2026-11-06).** The help menu's Fertilize rows are set to E (Keyboard Panel) and USE (Touch Panel) by Tools > Help: Fertilize Row to E and USE (`Assets/Editor/HelpFertilizeRowFix.cs`, delete after running). Two levels, at most one per growth stage, each **+50% harvest** (2D game). Each use costs fertilizer of about 0.42 to 0.58 x the field's harvest (the 2D ratio, Inspector values on `FieldController`), rolled once per visit like the seed price. The field pop-up shows only the bonus: "Fertilized: +50%" (level 1) or "+100%" (level 2). Harvest adds the fertilizer bonus to the harvester's +60%. Start stock is `startFertilizer` on `PlayerValues` (0); buy it at the fertilizer shop. Fertilizing does not speed up growth.
    - [ ] **Fertilizer colour for fields (owner, 2026-11-06): built, owner to test.** Like the 2D game (`Sim94/Sim/Fs94Values.cs`, `CropColors.Fert1` / `Fert2`): fertilizing (E on PC, USE on touch) turns the whole field **light blue (100, 185, 235) at level 1, deeper blue (80, 155, 235) at level 2**. It stays blue until the crop grows into its next stage, then shows the crop colour again (the 2D game did the same). In `FieldController.CurrentColor()`: blue while `fertStage > 0` and `fertilizedAt == stage`. Save / load keeps both, so a field saved while blue loads blue. **Owner:** fertilize a growing field, check it turns blue, then goes back to the crop colour at the next stage; the second fertilizing (next stage) gives the deeper blue.
16. ~~**Selling and money.**~~ **DONE** (sell stations for wheat, barley and oat, money in the counters panel, field `value` is the buy price).
17. ~~**Field ownership and buying.**~~ **DONE** (B key to buy or sell, press twice to confirm; sell pays 80%; other fields get random AI owners and names).
18. ~~**Save/load**~~ of crops, stock and money. **DONE (2026-11-06), tested by the owner:** see NEXT UP item 8.
19. **Help menu with the controls list.** **Phase 1 DONE and tested (2026-11-04); now a scene object (`Help Menu`, see NEXT UP item 4).** A pop-up with a controls table and nothing else (no tips): the Keyboard panel on PC, the Touch panel with touch controls. H opens and closes it; on touch a HELP button sits top-left and tapping the pop-up closes it. The touch panel leaves out F1 (no touch button); look is DRAG, pause is PAUSE. The HELP button looks good on the phone (owner). Phase 2 (if wanted): icons instead of text, a controller or gamepad layout.
20. **Touch controls rework.** The owner ran the phone build (2026-11-03) and it works, but the buttons are a little confusing and need reworking. ~~Also: when selling a field the message says "press B to confirm", but a phone has no B key.~~ **Fixed (2026-11-03, `a92f85d`):** it now says "Press again to confirm". Other messages that name a key already show the touch name on phones (E / USE, 2 / CAR).
    - [x] **FERT button removed (owner, 2026-11-06):** USE fertilizes a growing field (like E on PC). Gone from `TouchControls` and `InputManager`; the help Touch Panel's Fertilize row says USE (tool above, item 15).
    - [x] **Settings button (owner, 2026-11-05): built 2026-11-06, see NEXT UP item 9** (rounded buttons, wheel with no press-down look per the owner's later choice, slider, pedal slider). a SETTINGS button below HELP (top-left, a `UI Button` prefab copy, same style) that opens a settings panel to customize the touch buttons: rounded buttons (instead of square), and other steering options (for example a steering wheel / slider, tilt-to-steer, or `<` `>` moved or resized). **Steering wheel option (owner, 2026-11-05): give it a pressing-down animation** when touched (it sinks a little: slightly smaller and darker, maybe a small shadow shift, and springs back when let go), so it feels like a real button being pressed. Settings remembered on the phone (PlayerPrefs). Part of the touch rework (this item); the pause menu's planned settings screen (Android port item 10) could be the same panel.
    - [x] **Fixed (2026-11-05), tested on the phone by the owner (2026-11-05):** `HoldButton` now keeps a list of the fingers that are down on it: sliding off lets go, sliding back on presses again, and a finger sliding onto `<` `>` GAS BRAKE from another button presses it (slide from `<` to `>` to steer); a finger lifted anywhere is dropped, so nothing sticks. The one-press buttons (USE, BUY/SELL, CROP, CAR, HELP; FERT was removed 2026-11-06) only act on a real press, so passing over them does nothing. **Bug (owner, 2026-11-05): a held touch button lets go when the finger slides off it and does not come back.** Hold GAS, let the finger drift to the side or down off the button, then back onto it: the gas is off and stays off until the finger is lifted and put down again. Annoying while driving, since a thumb drifts. Cause: `TouchControls.HoldButton` releases on `OnPointerExit` but has no `OnPointerEnter`, so sliding back on never presses it again. Fix idea: also press on `OnPointerEnter` while that finger is still down (`eventData.eligibleForClick` / track the pointer id), or keep it held until the finger lifts (`OnPointerUp` only), maybe with a slightly bigger hit area. Applies to all hold buttons (`<` `>` GAS BRAKE).

## Direction
The owner is no longer cloning the 2D game one for one. The 3D game is its own new game and is built as it goes. The 2D analysis below is a reference and an idea source, not a checklist.

## Initial plan (in order)

**Phases 1 and 2 (stabilise, refactor `FieldController`): DONE.** See Progress below.

**Phase 3: Economy**
- ~~Money in `PlayerValues`, sell prices, sell points, money in the UI~~ DONE.
- ~~Field buying and selling~~ DONE.
- ~~Seeds and seed shop~~ DONE (seed shop complete; the economy may be revised later).
- Next (owner's order, 2026-11-03): map border, the car dealer, then the home menu (with a version number). All three done. After that (2026-11-04): phone look, UI into the hierarchy, tractor redesign, all done; next the app icon and sounds; later fuel and gas station and save/load. The paint shop is dropped.

**Phase 4: Fields and fertiliser**
- ~~Buying fields~~ DONE (moved into Phase 3).
- ~~Fertiliser~~ written (see item 15): F during growth, +50% yield per level, two levels.
- **Advanced crop harvesting:** drive-over harvest cells with bonuses (see "Advanced crop harvesting" below).

**Phase 5: Polish**
- ~~Save/load (18).~~ Built 2026-11-06 (owner testing). (Camera clamp (14) and input cleanup (13) are already done.)

**Phase 6: Vehicle mirrors**
- Add mirrors to the vehicle (left and right side mirrors, maybe a rear-view mirror).
- Likely approach: a small extra camera per mirror drawing into a `RenderTexture`, shown on a quad on the mirror, or a small on-screen panel. Keep the cost low on phones (small texture, lower frame rate or resolution for mirror cameras, off when not needed).
- Decide: real mirrors on the model, or a screen overlay. Check the Android frame rate after adding them.

## Waiting on the owner (decisions)
- **Sky and terrain layers: decided (owner, 2026-11-03).** The current set is final for now (the owner may delete one or two later). See Repo housekeeping.
- **Camera: decided (owner, 2026-11-03).** The `staticCamera` testing switch is removed, so mouse look is back on PC. Touch look was added later (2026-11-04): dragging on the game view tilts the camera, like the mouse (see Android port item 4).
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
- [x] Help menu phase 1: controls list pop-up, H key and a touch HELP button (tested; a scene object since 2026-11-04)
- [x] Truck: the front tires turn with the steering button (visual only, `maxSteerAngle` 30 and `steerAnglePerSecond` 120 on `GameCharacter`; untested)
- [x] Field layout: the home field (Tony) is split into four (2 x 2, even gaps). The two facing the house are the player's and the one nearest the house starts planted; the back two are AI. Six random AI fields were added on free ground (owner checked and saved the scene, 2026-11-02). The layout tool is kept only in git history (`d5b10c0`).
- [x] Reverse steering fixed, field confirm message says "Press again to confirm" (2026-11-03)
- [x] Car dealer, `Vehicle` split and vehicle switching: built and tested by the owner (2026-11-03)
- [x] Top road texture no longer stretched: `RoadTiling` tiles each road by metres (6 m per tile, set on the road's component), the road texture has 8x anisotropic filtering. The one-off Road Look tool is kept only in git history (`48d5ead`)
- [x] Van redesigned with rear doors, tail lights, number plate, the 2D sprite's roof ribs, mirrors and side detail (now `Assets/Prefabs/Van.prefab`, 87 parts; not yet checked in Play mode)
- [x] Static camera removed: mouse look is back on PC (2026-11-03).
- [x] Touch look: drag on the game view to tilt the camera on phones (2026-11-04, tested on the phone)
- [x] UI moved into the scene hierarchy: help menu, dealer shop menu, message box, speedometer (2026-11-04, tested); only the touch buttons are still built in code
- [x] Tractor redesign: connected model (Tractor Builder V2), 2026-11-04
- [x] Harvester redesign: Harvester Builder V2 run by the owner (2026-11-05), tool and old model removed

## Android port (notes for revising later)
Phase 1 is done (see above). Still to do:
1. ~~**Test the touch buttons in the editor.**~~ DONE. Tick `Show Touch Controls` on `InputManager`, check layout, drive, and plant/harvest with USE. Adjust button sizes/positions from screenshots.
2. ~~**Make the existing UI scale.**~~ DONE. The scene Canvas uses *Constant Pixel Size* (reference 800x600), so the counters and field pop-up will be the wrong size on phones. Switch it to *Scale With Screen Size* (for example 1920x1080, match 0.5) and re-check both panels.
3. (Partly done: no overlaps at 1920x1080.) **Check the touch layout against the pop-up.** GAS / BRAKE / USE sit above the bottom-right pop-up (260 px tall + 20 px margin); verify on a real screen, including phones with a notch or rounded corners (safe area).
4. ~~**Touch look.**~~ DONE (2026-11-04, NEXT UP item 3): drag on the game view to tilt the camera; tested on the phone.
5. ~~**Player Settings for Android:**~~ DONE (landscape, API 23+, IL2CPP, ARM64, package name).
   - minimum API level is 22 but Unity 6 needs 23 or higher; set it deliberately
   - force landscape orientation
   - IL2CPP + ARM64 for release builds
   - set a proper package name and app name (the default is a placeholder)
6. ~~**Install Android Build Support**~~ DONE. (Unity Hub > Installs > gear on 6000.0.85f1 > Add modules: Android Build Support, OpenJDK, Android SDK & NDK Tools). Close the editor while it installs.
7. **Performance on a phone.** Likely cost: terrain detail and grass, sky materials, shadows, `TerrainGrass.shadergraph`. Test and lower the quality settings for the Android quality level if needed.
8. ~~**First device build.**~~ DONE (runs on a real phone). Frame rate and UI size still worth a note. Original task: Build an APK, install it on a phone (enable "install unknown apps"), then test controls, UI size and frame rate.
9. **Sharing options.** APK only works for Android friends. A WebGL build on itch.io would be easier for friends with a PC (just a link), but has its own limits.
10. **Optional later:** replace `<` `>` `GAS` `BRAKE` `USE` text labels with icons; haptic feedback; a settings screen (the PAUSE button exists since 2026-11-04).

## Other things noticed (not yet scheduled)
- `PlayerValues` counters and `FieldView` references are set by hand in the Inspector; the new missing-reference check in `FieldController` helps, but the other scripts have no such check.
- ~~`GameCharacter` and `GameCharacterCar` are two halves of one partial class.~~ Split into `GameCharacter` (player) and `Vehicle` with the car dealer (2026-11-03). Getting out on foot (enter/exit) is not built; ask the owner if it is wanted.
- ~~Vehicles are not saved yet.~~ Saved since save / load (2026-11-06): the vehicles you own, where they are parked, and the one you drive.
- ~~Fields are not planted or harvested by the AI owners.~~ They are since 2026-11-06 (NEXT UP item 10).
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
- **Still not in the 3D game:** painting (dropped), AI owners planting fields (item 10). Save / load is built (item 8, 2026-11-06). Diesel is in (item 12); compost is the fertilizer.
- **The harvester already exists in 2D as art** (`h/` folder, four colours). In 3D it still needs a model.
- **Suggested order for Phase 3:** ~~(1) money, (2) a sell building, (4) field buying~~ done; (3) seeds and a seed shop, ~~done~~; the owner now wants the dealer (7) before fuel (5) and fertilizer (6).

### Assets in the 2D repo that could be reused
- `backround.png` (3500x3000), the full map. A layout reference only; the 3D map is built fresh.
- Tractor, van and harvester sprites (four colours each) and `garage.png`, `tittle.png` (title screen).
- `Read Me.txt`, `Saves/*.txt` (save format).

