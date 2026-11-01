# Farming Simulator 94

A small first-person farming game built in Unity. Walk or drive around the map, plant crops in fields, wait for them to grow, and harvest them.

## Features
- First-person character movement and a drivable vehicle
- Fields you can plant and harvest with a key press
- Three crops (wheat, barley, oat), each with its own growth stages shown by colour
- Field details pop-up (name, owner, value, size, crop, status)
- Harvest counters in the UI

## Requirements
- Unity **2022.3.55f1** (LTS), installed through Unity Hub
- Linux, Windows or macOS

## Getting started
1. Clone the repository.
2. Open Unity Hub, choose **Add > Add project from disk**, and select the project folder.
3. Open the project with Unity 2022.3.55f1. The first import can take a few minutes.
4. Open `Assets/Scenes/GameScene.unity` and press **Play**.

## Controls
| Key | Action |
| --- | --- |
| W / A / S / D or arrow keys | Move / drive |
| Mouse | Look around |
| E | Plant or harvest the field you are standing in |
| F1 | Toggle debug |

## Project layout
- `Assets/Scripts`: game code (`FieldController`, `PlayerValues`, `GameCharacter`, `InputManager`, `FieldView`)
- `Assets/Scenes`: game scene
- `Assets/Prefabs`: houses, garage, truck
- `PLAN.md`: improvement list and roadmap

## Status
Work in progress. See `PLAN.md` for known issues and planned features.
