# Ocean Explorer VR — Project Documentation

## Overview

Ocean Explorer is an interactive underwater VR experience built in Unity 2022.3.53f1 using the Universal Render Pipeline (URP). The project features a procedurally generated infinite underwater world with realistic fish behaviour, coral and kelp ecosystems, dynamic lighting and an interactive fish information system.

---

## Base Project

The project was built on top of Reef Explorer, an open-source Unity project that included:

- Procedurally generated infinite terrain using the Marching Cubes algorithm on the GPU
- GPU-accelerated fish flocking simulation (Boids algorithm)
- Procedural coral, kelp and tube coral placement
- Swimming player controller with first-person and third-person cameras
- URP post-processing effects: volumetric god rays, underwater caustics, depth fog
- Main menu and pause menu systems

---

## Modifications Made

### 1. Fish Replacement — Emperor Angelfish

The original Nemo (clownfish) model was replaced with the **Emperor Angelfish** model by Mikhail Nesterov (Unity Asset Store, free).

**Steps taken:**
- Downloaded Emperor Angelfish asset from Unity Asset Store
- Imported into `Assets/Mikhail Nesterov/Emperor Angelfish/`
- In the BoidsManager Inspector, replaced the Nemo prefab in Models → Element 0 with `EmperorAngelfish_swim1` prefab
- Adjusted scale settings:
  - Max Scale: `0.3`
  - Min Scale: `0.2`
  - Count: reduced from `100` to `30` for better performance and visual clarity
- The prefab includes 6 swim animations that play automatically on each fish instance

---

### 2. Fish Information UI Feature

Added an interactive feature where clicking on a fish displays an information board about that fish species.

#### New Scripts Created

**`Assets/Scripts/Fish/FishData.cs`**
A ScriptableObject that stores fish information:
- Fish Name
- Description
- Habitat
- Diet
- Size
- Icon (optional sprite)

**`Assets/Scripts/Fish/FishClickable.cs`**
A marker component automatically attached to each fish when it spawns. Stores the species index and a reference to its FishData asset.

**`Assets/Scripts/Fish/FishInfoPanel.cs`**
Controls the UI info board. Exposes `Show(FishData)` and `Hide()` methods to display or hide the panel with the relevant fish information.

**`Assets/Scripts/Fish/FishSelector.cs`**
Attached to the Player GameObject. On left mouse click, fires a `Physics.RaycastAll` from the camera through all objects. If a fish with a `FishClickable` component is hit, the info panel is shown.

#### Modified Scripts

**`Assets/Scripts/Managers/BoidsManager.cs`**
- Added `FishData Info` field to the `ModelData` struct so each fish species can have its own data asset assigned in the Inspector
- On fish instantiation, automatically adds a `SphereCollider` (radius 1.5) to each fish for click detection
- Automatically adds and populates a `FishClickable` component on each fish

#### Unity Editor Setup

1. Created `EmperorAngelfishData` ScriptableObject (right-click → Create → Fish → Fish Data) with:
   - Name: Emperor Angelfish
   - Description: A vibrant reef fish known for its striking blue and yellow stripes
   - Habitat: Coral reefs, Indo-Pacific ocean
   - Diet: Algae, sponges, small invertebrates
   - Size: Up to 40 cm
2. Assigned `EmperorAngelfishData` to BoidsManager → Models → Element 0 → Info
3. Created a Canvas (Screen Space Overlay) in MainScene with:
   - A Panel named `FishInfoPanel` containing TextMeshPro text fields for Name, Description, Habitat, Diet and Size
   - A Close button wired to `FishInfoPanel.Hide()`
4. Added `FishInfoPanel` script to Canvas and wired all text fields
5. Added `FishSelector` script to Player with Main Camera and Info Panel references

#### How It Works

```
Player left-clicks
→ FishSelector fires RaycastAll from camera
→ Checks all hit objects for FishClickable component
→ If found, calls FishInfoPanel.Show(fishData)
→ Info board appears on screen
→ Clicking elsewhere or pressing Close hides it
```

---

### 3. Audio — Oceanic Ambience

The original menu music (`demo_menu1.mp3`) was replaced with an underwater oceanic ambient sound.

**How audio works in the project:**
- `AudioManager.cs` is a singleton with `DontDestroyOnLoad`
- It holds a public `AudioClip Background` field assigned in the Inspector
- The clip plays on loop at 0.5 volume from startup
- To change the audio: import a new audio file into `Assets/Audio/` and assign it to the AudioManager's Background field in the Inspector

---

### 4. Meta Quest 3 VR Support

Added support for viewing the project on Meta Quest 3 via Meta Horizon Link (PC VR).

#### Packages Added to `Packages/manifest.json`

```json
"com.unity.xr.management": "4.4.0",
"com.unity.xr.openxr": "1.9.1",
"com.unity.xr.interaction.toolkit": "2.5.2"
```

#### New Script

**`Assets/Scripts/Player/VRInputManager.cs`**
Reads input from Quest 3 controllers using Unity's XR Input system and maps it to the same interface used by the existing player controller:

| Quest 3 Controller | Action |
|-------------------|--------|
| Left thumbstick | Swim (replaces WASD) |
| Right thumbstick up | Surface (replaces Space) |
| Right trigger | Speed boost (replaces Shift) |
| X button | Toggle flashlight (replaces F) |

#### Modified Scripts

**`Assets/Scripts/Player/Player.cs`**
Updated `Surface()`, `GetSpeed()` and `GetDirection()` methods to check for `VRInputManager.Instance` first. Falls back to keyboard input if VR is not active. This keeps the project fully playable on desktop without a headset.

**`Assets/Scripts/Player/Flashlight.cs`**
Updated to check `VRInputManager.Instance` in `Update()` for the flashlight toggle when in VR mode. Falls back to keyboard F key on desktop.

#### Unity Editor Setup for Quest Link

1. **Edit → Project Settings → XR Plugin Management** → Install → under PC tab tick **OpenXR**
2. In OpenXR settings → add **Meta Quest Touch Pro Controller** interaction profile → enable **Meta Quest Support** feature
3. In MainScene → Hierarchy → right-click → **XR → XR Origin (VR)**
4. Assign XR Origin Camera as the Main Camera reference on FishSelector
5. Add VRInputManager component to Player GameObject

#### Connection Steps

1. Install Meta Horizon Link on PC
2. Enable Developer Mode on Quest 3 (Meta app → Headset Settings → Developer Mode)
3. Connect Quest 3 to PC via USB-C
4. In headset: allow access → enable Quest Link
5. Hit Play in Unity → put on headset

---

### 5. Bug Fixes

**`Assets/Scripts/Managers/InputManager.cs`**
Fixed a `NullReferenceException` where `OnEnable()` was called before `Controls` was initialised in `Awake()`. Added null-conditional operator:
```csharp
private void OnEnable() { Controls?.Enable(); }
private void OnDisable() { Controls?.Disable(); }
```

---

### 6. GitHub Repository

The project is hosted at: **https://github.com/DevikaaMenon/ocean-explorer**

A `.gitignore` was configured to exclude Unity auto-generated folders (`Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`) keeping the repository size manageable while retaining all source assets and scripts.

---

## Project Structure

```
Assets/
├── Audio/                  # Background music and ambient sound
├── GUI/                    # Logo and UI graphics
├── Images/                 # Background image and icons
├── Materials/              # URP materials for terrain, fish, coral
├── Mikhail Nesterov/       # Emperor Angelfish asset
├── Models/                 # Fish and coral 3D models
├── Scenes/
│   ├── MainMenu.unity      # Main menu scene
│   └── MainScene.unity     # Gameplay scene
├── Scriptable Objects/     # Settings and FishData assets
├── Scripts/
│   ├── Fish/               # FishData, FishClickable, FishInfoPanel, FishSelector
│   ├── Managers/           # WorldManager, BoidsManager, AudioManager, etc.
│   ├── Player/             # PlayerController, PlayerCamera, Flashlight, VRInputManager
│   ├── Menu/               # MainMenu, PauseMenu, SettingsMenu
│   └── Settings/           # ScriptableObject definitions
└── Textures/               # Terrain and UI textures
```

---

## Controls

### Desktop
| Key | Action |
|-----|--------|
| W / A / S / D | Swim |
| Space | Ascend / Surface |
| Shift | Speed boost |
| F | Toggle flashlight |
| C | Switch camera (FP / TP) |
| Esc | Pause |
| Left Click | View fish information |

### Meta Quest 3
| Controller Input | Action |
|----------------|--------|
| Left thumbstick | Swim |
| Right thumbstick up | Surface |
| Right trigger | Speed boost |
| X button | Toggle flashlight |
| Gaze + trigger | View fish information |

---

## Tools and Assets Used

- **Unity 2022.3.53f1** — Game engine
- **Universal Render Pipeline (URP) 14.0.12** — Rendering
- **Emperor Angelfish by Mikhail Nesterov** — Fish model (Unity Asset Store, free)
- **Meta XR SDK / OpenXR** — VR support
- **TextMeshPro** — UI text rendering
- **Cinemachine 2.10.3** — Camera system
- **Blender** — Original coral and kelp models
