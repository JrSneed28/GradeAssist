import bpy
from mathutils import Vector

# =============================================================================
# Generic Grade Monitor Blockout Script
# =============================================================================
# Purpose:
#   Create a generic cab-mounted monitor assembly for use as a visual
#   reference or prefab source in the Unity simulator.
#
# Scale Convention:
#   - All units are in meters.
#   - The monitor is modeled at approximate real-world size:
#     body ~450 mm wide x 60 mm deep x 280 mm tall.
#   - 1 Blender unit = 1 meter.
#
# Origin Convention:
#   - The scene origin (0, 0, 0) is placed at the center of the monitor body
#     front face. The mounting bracket extends backward (positive Y).
#   - When exporting to Unity, the origin becomes the GameObject pivot.
#   - Recommended: after import, parent to an empty at the mounting point
#     so the pivot can be adjusted without re-exporting.
#
# Export Notes:
#   - Export to FBX: File > Export > FBX
#   - Forward axis: -Y Forward
#   - Up axis: Z Up
#   - Apply Transform: enabled (so scale is 1,1,1 in Unity)
#   - Include: Selected Objects (or use a collection)
#   - Scale: set Apply Scalings to FBX Units Scale
#
# Collision Proxy Naming (for Unity):
#   - Create simplified mesh colliders named <ObjectName>_Collider.
#   - These are hidden in renderers but included in physics/collision.
#   - Example: Monitor_Body_Collider
#
# Material Naming (for Unity import):
#   - Materials are assigned in Blender with placeholder slots.
#   - Unity creates materials named "MAT_<Component>" on import.
#   - Replace placeholder materials in Unity with proper shaders.
#
# Branding Policy:
#   - NO Cat/Caterpillar logos, marks, or proprietary assets.
#   - Generic styling only.
# =============================================================================


def clear_scene():
    """Remove all mesh objects and materials to start clean."""
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.select_by_type(type='MESH')
    bpy.ops.object.delete()
    for material in bpy.data.materials:
        if material.users == 0:
            bpy.data.materials.remove(material)


def make_material(name, color_rgb):
    """Create a simple Blender material with a diffuse color."""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color_rgb, 1.0)
    return mat


def create_cube(name, location, dimensions, material=None):
    """Create a cube with the given name, location, and dimensions."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    return obj


def create_cylinder(name, location, radius, depth, rotation, material=None, vertices=32):
    """Create a cylinder with the given parameters."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    return obj


def create_sphere(name, location, radius, material=None):
    """Create a UV sphere."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=16,
        ring_count=8,
        radius=radius,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    return obj


def create_collider_proxy(source_obj, name_suffix="_Collider"):
    """Create a simple box collider proxy from an object's bounding box.

    The proxy is a simple cube named <source_name><suffix>.
    In Unity, disable the MeshRenderer on *_Collider objects and use
    MeshCollider or BoxCollider components for collision.
    """
    # Duplicate the object, then simplify to a box
    bpy.ops.object.select_all(action='DESELECT')
    source_obj.select_set(True)
    bpy.context.view_layer.objects.active = source_obj
    bpy.ops.object.duplicate()
    collider = bpy.context.object
    collider.name = source_obj.name + name_suffix
    # Reset to a simple cube scaled to match bounding box
    # For a true proxy, we keep it simple; here we just clear materials
    # and mark it visually as a proxy (wireframe display)
    collider.data.materials.clear()
    collider.display_type = 'WIRE'
    # Move slightly for visibility if needed, but keep overlapping
    return collider


# =============================================================================
# Main Assembly
# =============================================================================

clear_scene()

# Materials (placeholders for Unity import)
mat_body = make_material("MAT_Body", (0.15, 0.15, 0.15))
mat_bezel = make_material("MAT_Bezel", (0.10, 0.10, 0.10))
mat_screen = make_material("MAT_Screen", (0.05, 0.05, 0.05))
mat_button = make_material("MAT_Button", (0.30, 0.30, 0.30))
mat_knob = make_material("MAT_Knob", (0.25, 0.25, 0.25))
mat_bracket = make_material("MAT_Bracket", (0.40, 0.40, 0.40))
mat_led_green = make_material("MAT_LED_Green", (0.0, 1.0, 0.0))
mat_led_amber = make_material("MAT_LED_Amber", (1.0, 0.65, 0.0))
mat_led_red = make_material("MAT_LED_Red", (1.0, 0.0, 0.0))

# Monitor_Body: main housing
# Dimensions: 450 mm x 60 mm x 280 mm
body = create_cube(
    "Monitor_Body",
    location=(0.0, 0.0, 0.0),
    dimensions=(0.45, 0.06, 0.28),
    material=mat_body,
)

# Monitor_Bezel: thin frame in front of the body
# Slightly recessed in Y, larger in X/Z to frame the screen
bezel = create_cube(
    "Monitor_Bezel",
    location=(0.0, -0.035, 0.0),
    dimensions=(0.42, 0.02, 0.24),
    material=mat_bezel,
)

# Monitor_Screen: display area
# Very thin, centered within the bezel
screen = create_cube(
    "Monitor_Screen",
    location=(0.0, -0.048, 0.015),
    dimensions=(0.34, 0.01, 0.17),
    material=mat_screen,
)

# Function buttons F1-F4
# Placed below the screen, evenly spaced
button_width = 0.055
button_depth = 0.02
button_height = 0.025
for i, x in enumerate([-0.16, -0.05, 0.05, 0.16], start=1):
    create_cube(
        f"Button_F{i}",
        location=(x, -0.06, -0.115),
        dimensions=(button_width, button_depth, button_height),
        material=mat_button,
    )

# Home and Back buttons
# Placed above the screen on left/right edges
create_cube(
    "Button_Home",
    location=(-0.19, -0.06, 0.12),
    dimensions=(0.045, 0.02, 0.03),
    material=mat_button,
)
create_cube(
    "Button_Back",
    location=(0.19, -0.06, 0.12),
    dimensions=(0.045, 0.02, 0.03),
    material=mat_button,
)

# Knob_Rotary: rotary encoder knob
# Cylinder on its side, with a small cap on top
create_cylinder(
    "Knob_Rotary",
    location=(0.205, -0.06, -0.06),
    radius=0.035,
    depth=0.025,
    rotation=(1.5708, 0.0, 0.0),
    material=mat_knob,
)

# LED indicators
# Small spheres on the left side, vertically spaced
led_configs = [
    ("LED_Green", 0.11, mat_led_green),
    ("LED_Amber", 0.085, mat_led_amber),
    ("LED_Red", 0.06, mat_led_red),
]
for name, z, mat in led_configs:
    create_sphere(
        name,
        location=(-0.205, -0.06, z),
        radius=0.01,
        material=mat,
    )

# Bracket: simple mounting bracket
# Extends backward from the body center, lower half
bracket = create_cube(
    "Bracket",
    location=(0.0, 0.04, -0.22),
    dimensions=(0.08, 0.08, 0.18),
    material=mat_bracket,
)

# =============================================================================
# Collision Proxies
# =============================================================================
# Create simplified box colliders for key interactive parts.
# In Unity: disable MeshRenderer on these, keep MeshCollider or
# replace with BoxCollider for performance.
# =============================================================================

create_collider_proxy(body)
create_collider_proxy(bezel)
create_collider_proxy(screen)
create_collider_proxy(bracket)

# =============================================================================
# Cleanup / Finalize
# =============================================================================

# Deselect everything for a clean state
bpy.ops.object.select_all(action='DESELECT')

# Save (optional — comment out if running headless or in a pipeline)
bpy.ops.wm.save_as_mainfile(filepath="assets/blender/grade_monitor_blockout.blend")

print("Created generic grade monitor blockout.")
print("Objects created:")
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        print(f"  - {obj.name}")
