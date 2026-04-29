# Blender Quick Start

## Generate the Monitor Blockout

1. Install Blender 3.x or newer from [blender.org](https://www.blender.org).
2. Open Blender.
3. Switch to the **Scripting** workspace.
4. Open `assets/blender/create_monitor_blockout.py` via **Text > Open**.
5. Press **Run Script**.
6. The script clears the scene and creates the monitor assembly.

## Objects Created

The script produces these mesh objects:

| Object | Type | Description |
|--------|------|-------------|
| `Monitor_Body` | Cube | Main housing (~450 x 60 x 280 mm) |
| `Monitor_Bezel` | Cube | Front frame |
| `Monitor_Screen` | Cube | Display area |
| `Button_F1` ... `Button_F4` | Cube | Function buttons |
| `Button_Home` | Cube | Home button |
| `Button_Back` | Cube | Back button |
| `Knob_Rotary` | Cylinder | Rotary encoder knob |
| `LED_Green` | Sphere | Status LED |
| `LED_Amber` | Sphere | Status LED |
| `LED_Red` | Sphere | Status LED |
| `Bracket` | Cube | Mounting bracket |
| `Monitor_Body_Collider` | Cube | Collision proxy |
| `Monitor_Bezel_Collider` | Cube | Collision proxy |
| `Monitor_Screen_Collider` | Cube | Collision proxy |
| `Bracket_Collider` | Cube | Collision proxy |

## Export to Unity

1. Create a collection named `GradeMonitor`.
2. Move all objects into it.
3. File > Export > FBX.
4. Settings: `-Y Forward`, `Z Up`, **Apply Transform** enabled.
5. Import into `Assets/Models/` in Unity.

See `docs/BlenderMonitorAssetGuide.md` for full details.
