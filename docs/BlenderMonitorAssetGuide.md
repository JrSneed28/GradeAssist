# Blender Monitor Asset Guide

This guide covers the generic cab monitor blockout script and the recommended pipeline for importing the resulting asset into the Unity simulator.

## Purpose

The Blender monitor blockout provides a dimensionally accurate, generically branded reference model of a cab-mounted grade-control display. It is intended as:

- A visual placeholder in the Unity simulator scene.
- A prefab source that artists can replace with a finished monitor model.
- A collision-proxied mesh for hit-testing or mounting-point alignment.

**Branding policy:** No Caterpillar, Cat, or other proprietary logos, marks, or assets may be added to this model or its materials.

## Running the Script

1. Open Blender 3.x or newer.
2. Switch to the **Scripting** workspace.
3. Open `assets/blender/create_monitor_blockout.py` via **Text > Open**.
4. Press **Run Script**.

The script will clear the scene and create the monitor assembly.

## Scale and Origin Conventions

- **Unit system:** Metric. 1 Blender unit = 1 meter.
- **Real-world reference:** The monitor body is approximately **450 mm wide x 60 mm deep x 280 mm tall**.
- **Origin:** The scene origin `(0, 0, 0)` is placed at the center of the monitor body front face.
  - This makes the screen and bezel lie near negative Y.
  - The mounting bracket extends toward positive Y.
- **Pivot recommendation:** After importing into Unity, parent the model to an empty GameObject positioned at the bracket mounting point. This lets you rotate the monitor around the mount rather than the body center without re-exporting from Blender.

## Assembly Components

| Object Name | Type | Description |
|-------------|------|-------------|
| `Monitor_Body` | Cube | Main housing |
| `Monitor_Bezel` | Cube | Front frame |
| `Monitor_Screen` | Cube | Display area (very thin) |
| `Button_F1` ... `Button_F4` | Cube | Function buttons |
| `Button_Home` | Cube | Home button |
| `Button_Back` | Cube | Back button |
| `Knob_Rotary` | Cylinder | Rotary encoder knob |
| `LED_Green` | Sphere | Status LED (green) |
| `LED_Amber` | Sphere | Status LED (amber) |
| `LED_Red` | Sphere | Status LED (red) |
| `Bracket` | Cube | Simple mounting bracket |

## Export Settings

Use **File > Export > FBX (.fbx)** with these settings:

| Setting | Value |
|---------|-------|
| Selected Objects | Optional (use a collection if preferred) |
| Forward | `-Y Forward` |
| Up | `Z Up` |
| Apply Transform | **Enabled** (ensures scale is `1, 1, 1` in Unity) |
| Apply Scalings | `FBX Units Scale` |
| Include | Mesh + Empty (if you added empties for pivot helpers) |

### Recommended workflow

1. Create a Blender collection named `GradeMonitor`.
2. Move all monitor objects into that collection.
3. Export **Selected Objects** from that collection.
4. Import the FBX into Unity's `Assets/Models/` folder.

## Collision Proxy Workflow

The script generates simplified collider proxies named with a `_Collider` suffix:

- `Monitor_Body_Collider`
- `Monitor_Bezel_Collider`
- `Monitor_Screen_Collider`
- `Bracket_Collider`

In Unity, for each collider object:

1. Disable the `MeshRenderer` component (uncheck it).
2. Add a `BoxCollider` or `MeshCollider` component.
3. Place collider objects on a dedicated layer (e.g., `MonitorCollision`) if your game uses layer-based raycasting.

This keeps high-poly visual meshes separate from low-poly collision geometry.

## Material Assignment Guide

The script creates placeholder materials with descriptive names:

- `MAT_Body`
- `MAT_Bezel`
- `MAT_Screen`
- `MAT_Button`
- `MAT_Knob`
- `MAT_Bracket`
- `MAT_LED_Green`
- `MAT_LED_Amber`
- `MAT_LED_Red`

When you import into Unity:

1. Unity creates materials from Blender material slots.
2. Replace the auto-generated materials with proper Unity shaders (e.g., URP/Lit).
3. For the screen, consider a custom shader or a `RenderTexture` target so the monitor UI can be rendered in real time.
4. LED materials can be emissive for a lit indicator look.

## Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| Objects are huge/tiny in Unity | Scale not applied on export | Re-export with **Apply Transform** enabled |
| Monitor faces wrong direction in Unity | Wrong Forward axis | Use `-Y Forward` |
| No materials in Unity | Materials not assigned in Blender | Ensure `obj.data.materials.append(mat)` is called |
| Collider proxies visible | Forgot to disable renderer in Unity | Disable `MeshRenderer` on `*_Collider` objects |

## Customization

The script is intentionally simple. To customize:

- Edit `dimensions` and `location` values in each `create_cube` / `create_cylinder` / `create_sphere` call.
- Add new materials in `make_material()` with your desired RGB values.
- Add more collider proxies by calling `create_collider_proxy(obj)` on any object.
