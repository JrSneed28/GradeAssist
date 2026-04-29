# Unity Simulator Design — GradeAssist.UnitySim

> Standalone Unity simulator for excavator grade-assist prototyping.

---

## Table of Contents

1. [Project Architecture and Scene Hierarchy](#1-project-architecture-and-scene-hierarchy)
2. [Mock Excavator Rig and Controls](#2-mock-excavator-rig-and-controls)
3. [Cab Monitor UI and RenderTexture System](#3-cab-monitor-ui-and-rendertexture-system)
4. [Grade Math Pipeline and Status Display](#4-grade-math-pipeline-and-status-display)
5. [Manual Test Checklist](#5-manual-test-checklist)
6. [Unity Version Assumptions](#6-unity-version-assumptions)
7. [Risks and Fallback Plan](#7-risks-and-fallback-plan)
8. [File Tree to Create](#8-file-tree-to-create)

---

# 1. Project Architecture and Scene Hierarchy

## 1.1 Overview

This section defines the overall Unity project architecture for `GradeAssist.UnitySim`, a standalone cab-monitor simulator that consumes the math and controller models from `GradeAssist.Core`.

The simulator runs as an independent Unity executable and uses keyboard input to move a mock excavator cutting-edge reference while a cab monitor displays real-time grade error computed by the shared core library.

## 1.2 Folder Structure

```text
src/GradeAssist.UnitySim/
├── Assets/
│   ├── Scenes/
│   │   └── GradeAssistSimulator.unity          # Primary simulation scene
│   ├── Scripts/
│   │   ├── Core/                               # Mirror of GradeAssist.Core
│   │   │   ├── Vector3D.cs
│   │   │   ├── GradePlane.cs
│   │   │   ├── GradeTargetSettings.cs
│   │   │   ├── GradeError.cs
│   │   │   ├── GradeStatus.cs
│   │   │   └── Controllers/
│   │   │       ├── SimulatedAssistController.cs
│   │   │       ├── SimulatedBucketAssistController.cs
│   │   │       ├── SimulatedGradeAssistController.cs
│   │   │       ├── SimulatedSwingAssistController.cs
│   │   │       ├── SimulatedEFenceController.cs
│   │   │       ├── ControllerState.cs
│   │   │       ├── ControllerOutput.cs
│   │   │       └── ControllerMockState.cs
│   │   ├── Rig/
│   │   │   ├── MockExcavatorRig.cs             # Keyboard-driven bucket ref mover
│   │   │   └── BucketReferenceTracker.cs       # Reports world pos to monitors
│   │   ├── Monitor/
│   │   │   └── MonitorInputRouter.cs           # Mouse / button event routing
│   │   └── UI/
│   │       └── GradeStatusDisplay.cs           # Visual status indicator
│   │   ├── GradeMonitorSimulator.cs            # Grade math -> UI text
│   │   ├── RenderTextureMonitorBinder.cs       # Camera RT -> screen mesh
│   │   └── MonitorPageManager.cs               # Page state machine
│   ├── Materials/
│   │   ├── MonitorScreen.mat                   # Emissive screen material
│   │   └── MonitorBezel.mat                    # Plastic housing material
│   ├── Prefabs/
│   │   ├── CabMonitor.prefab
│   │   └── MockExcavator.prefab
│   └── Resources/
│       └── (reserved for future asset-bundle loading)
├── Packages/
│   └── manifest.json
├── ProjectSettings/
└── README.md
```

## 1.3 Core Math Integration Options

`GradeAssist.Core` targets `net8.0`. Unity must consume these types at runtime. Three integration patterns are evaluated below.

### Option A — Pre-built DLL Reference

Build `GradeAssist.Core` as a class-library DLL and place it in `Assets/Plugins/`.

| Pros                                   | Cons                                                   |
| -------------------------------------- | ------------------------------------------------------ |
| Single artifact; no source duplication | Unity Mono runtime may not fully support `net8.0` APIs |
| Easy version pinning                   | IL2CPP builds can strip required types                 |
| Clean separation of concerns           | Requires the DLL to be re-copied on every Core change  |

**Verdict:** Risky for a prototype. Modern Unity (2022.3 LTS+) supports .NET Standard 2.1, but `net8.0`-specific language features can still cause runtime surprises.

### Option B — Source Mirror (Recommended)

Copy the `.cs` files from `src/GradeAssist.Core/` into `Assets/Scripts/Core/` and let Unity compile them directly.

| Pros                                                        | Cons                                |
| ----------------------------------------------------------- | ----------------------------------- |
| Zero runtime compatibility risk                             | Source duplication                  |
| Unity compiles with its own Roslyn; all C# 11 features work | Must be re-copied when Core changes |
| Debugging is seamless                                       |                                     |

**Verdict:** Safest for a prototype. The Core project is small (approx. 10 files) and has zero external dependencies, so duplication cost is minimal.

A PowerShell script `scripts/sync-core-to-unity.ps1` (not yet implemented) should:

1. Mirror `src/GradeAssist.Core/*.cs` and `src/GradeAssist.Core/Controllers/*.cs` into `src/GradeAssist.UnitySim/Assets/Scripts/Core/`.
2. Strip or adjust namespace `using` declarations if needed.
3. Add a `.gitignore` entry so the mirrored folder is not committed to source control.

### Option C — Assembly Definition (`asmdef`) + DLL

Create a Unity Assembly Definition that references the pre-built DLL.

| Pros                             | Cons                                                 |
| -------------------------------- | ---------------------------------------------------- |
| Logical grouping in Unity        | Still carries the `net8.0` / Mono compatibility risk |
| Faster incremental compile times | Extra Unity-specific metadata file to maintain       |

**Verdict:** Adds complexity without removing the compatibility risk.

### Recommendation

**Use Option B (Source Mirror) with an automated copy step.** This keeps the prototype robust while preserving the ability to switch to Option A later once Unity runtime compatibility is verified.

## 1.4 Scene Hierarchy

Exact GameObject names and parent-child relationships for `GradeAssistSimulator.unity`.

```text
GradeAssistSimulator
├── Environment
│   ├── GroundPlane                 # MeshRenderer + collider for visual reference
│   └── DirectionalLight            # Scene lighting
├── MockExcavator
│   ├── Body                        # Visual chassis
│   ├── Undercarriage               # Track base, fixed relative to Body
│   │   └── LeftTrack / RightTrack  # Cylinders
│   ├── SwingPivot                  # Rotates around Y axis
│   │   ├── Cab                     # Operator cabin
│   │   ├── EngineCompartment       # Counterweight
│   │   └── BoomPivot               # Boom raise/lower
│   │       └── BoomGeometry        # Scaled cube, ~5 m
│   │           └── StickPivot      # Stick curl
│   │               └── StickGeometry   # Scaled cube, ~3 m
│   │                   └── BucketPivot   # Bucket curl
│   │                       └── BucketGeometry
│   │                           └── CuttingEdgeReference
│   │                               └── VisualMarker    # Small sphere gizmo
│   └── ...
├── CabMonitor
│   ├── Monitor_Body                # Main housing mesh
│   ├── Monitor_Bezel               # Front frame mesh
│   ├── Monitor_Screen              # Quad mesh displaying RenderTexture
│   ├── Monitor_UI_Camera           # Orthographic camera for UI rendering
│   ├── Monitor_Canvas              # Screen Space - Camera Canvas
│   │   ├── MonitorPageManager      # Page switching logic
│   │   ├── HeaderRow               # Tab buttons
│   │   │   ├── Tab_Grade
│   │   │   ├── Tab_Settings
│   │   │   ├── Tab_Diagnostics
│   │   │   └── Txt_Title
│   │   ├── Page_Grade2D            # Default active page
│   │   ├── Page_Settings           # Inactive
│   │   └── Page_Diagnostics        # Inactive
│   └── ...
└── SimulationDirector
    └── GradePlaneVisualizer        # Debug lines showing the grade plane
```

### Rationale

- **MockExcavator** groups all rig geometry. Only `CuttingEdgeReference` matters for math.
- **CabMonitor** is a static prop. `Monitor_Screen` material is updated by `RenderTextureMonitorBinder`.
- **Monitor_Canvas** is a `Screen Space - Camera` canvas rendered by `Monitor_UI_Camera` to a `RenderTexture`.
- **SimulationDirector** hosts scene-level managers.

## 1.5 Input Routing

### Rig Control — Keyboard

| Key                | Action                                                |
| ------------------ | ----------------------------------------------------- |
| `UpArrow` / `W`    | Move cutting-edge reference +Z (forward)              |
| `DownArrow` / `S`  | Move cutting-edge reference -Z (backward)             |
| `LeftArrow` / `A`  | Move cutting-edge reference -X (left)                 |
| `RightArrow` / `D` | Move cutting-edge reference +X (right)                |
| `PageUp`           | Move cutting-edge reference +Y (up)                   |
| `PageDown`         | Move cutting-edge reference -Y (down)                 |
| `B`                | Set current cutting-edge position as new benchmark    |
| `M`                | Cycle monitor page (Grade -> Settings -> Diagnostics) |
| `Tab`              | Toggle Direct / Kinematic control mode                |

### Monitor Navigation — Mouse

| Interaction               | Action                         |
| ------------------------- | ------------------------------ |
| Click tab buttons         | Jump directly to selected page |
| Scroll / drag rotary knob | Adjust numeric values          |

### Separation of Concerns

```text
Input devices
    +- Keyboard -> MockExcavatorRig (rig physics / transform)
    +- Mouse    -> MonitorInputRouter -> MonitorPageManager (UI state)
```

No input handler directly touches both rig and monitor state.

## 1.6 MonoBehaviour Script-to-GameObject Mapping

| Script                       | GameObject             | Purpose                                        |
| ---------------------------- | ---------------------- | ---------------------------------------------- |
| `MockExcavatorRig`           | `MockExcavator`        | Reads keyboard, moves `CuttingEdgeReference`   |
| `BucketReferenceTracker`     | `CuttingEdgeReference` | Exposes world position via event               |
| `GradeMonitorSimulator`      | `Monitor_Canvas`       | Computes grade error, updates UI text          |
| `RenderTextureMonitorBinder` | `Monitor_Screen`       | Creates RT, assigns to screen material         |
| `MonitorInputRouter`         | `CabMonitor`           | Raycasts mouse clicks against button colliders |
| `MonitorPageManager`         | `Monitor_Canvas`       | Manages page stack, updates visible UI panels  |
| `GradeStatusDisplay`         | `StatusIndicator`      | Changes color/image based on `GradeStatus`     |
| `GradePlaneVisualizer`       | `SimulationDirector`   | Draws debug gizmos                             |

### Execution Order

1. `RenderTextureMonitorBinder.Start()` — allocates RenderTexture, binds material.
2. `MockExcavatorRig.Start()` — caches initial reference position.
3. `GradeMonitorSimulator.Start()` — captures benchmark from rig, initializes UI.
4. `MonitorPageManager.Start()` — shows default page, hides others.
5. `MockExcavatorRig.Update()` — polls keyboard, applies movement.
6. `GradeMonitorSimulator.Update()` — recomputes grade error, refreshes text.
7. `GradePlaneVisualizer.Update()` — draws debug plane (Editor builds only).

---

# 2. Mock Excavator Rig and Controls

## 2.1 Transform Hierarchy

The mock excavator rig follows a parented kinematic chain rooted at the machine body.

```text
MockExcavator (root)
├── Body (visual chassis)
├── Undercarriage (track base, fixed relative to Body)
│   └── LeftTrack / RightTrack (cylinders)
├── SwingPivot (rotates around Y axis; house swing)
│   ├── Cab (cube, local position 0, 1.5, 0)
│   ├── EngineCompartment (cube, local position -1.2, 1.0, 0)
│   └── BoomPivot (rotates around X axis; boom raise/lower)
│       └── BoomGeometry (scaled cube, length ~5 m)
│           └── StickPivot (rotates around X axis; stick curl)
│               └── StickGeometry (scaled cube, length ~3 m)
│                   └── BucketPivot (rotates around X axis; bucket curl)
│                       └── BucketGeometry (wedge/cube)
│                           └── CuttingEdgeReference (empty, tip of bucket)
```

### Suggested Local Positions (meters)

| Transform            | Parent        | Local Position | Local Rotation | Role                  |
| -------------------- | ------------- | -------------- | -------------- | --------------------- |
| MockExcavator        | —             | (0, 0, 0)      | (0, 0, 0)      | Scene root            |
| Body                 | MockExcavator | (0, 0.5, 0)    | (0, 0, 0)      | Chassis center        |
| SwingPivot           | Body          | (0, 1.0, 0)    | (0, 0, 0)      | House rotation point  |
| BoomPivot            | SwingPivot    | (0.8, 0.2, 0)  | (0, 20, 0)     | Boom base hinge       |
| StickPivot           | BoomPivot     | (0, 5.0, 0)    | (0, -20, 0)    | Stick base hinge      |
| BucketPivot          | StickPivot    | (0, 3.0, 0)    | (0, 0, 0)      | Bucket base hinge     |
| CuttingEdgeReference | BucketPivot   | (0, -0.3, 0.5) | (0, 0, 0)      | Grade reference point |

### Joint Axes

- **SwingPivot:** Rotates around world Y. Range -180 to +180 degrees.
- **BoomPivot:** Rotates around local X. Range -30 to +70 degrees from horizontal.
- **StickPivot:** Rotates around local X. Range -120 to +30 degrees relative to boom.
- **BucketPivot:** Rotates around local X. Range -90 to +45 degrees relative to stick.

## 2.2 Keyboard Control Mapping

Controls are split into two modes: **Direct** (default) and **Kinematic**.

### Direct Mode (default)

| Key(s)                            | Action                              |
| --------------------------------- | ----------------------------------- |
| `W` / `UpArrow`                   | Move cutting edge +Z                |
| `S` / `DownArrow`                 | Move cutting edge -Z                |
| `A` / `LeftArrow`                 | Move cutting edge -X                |
| `D` / `RightArrow`                | Move cutting edge +X                |
| `PageUp`                          | Move cutting edge +Y                |
| `PageDown`                        | Move cutting edge -Y                |
| `LeftShift` / `RightShift` (hold) | 0.25x speed modifier (fine control) |
| `LeftCtrl` / `RightCtrl` (hold)   | 4x speed modifier (fast traverse)   |

### Kinematic Mode (toggle with Tab)

| Key(s)    | Action                             |
| --------- | ---------------------------------- |
| `Q` / `E` | Swing left / right                 |
| `W` / `S` | Boom up / down                     |
| `A` / `D` | Stick in / out                     |
| `Z` / `X` | Bucket curl in / out               |
| `R`       | Reset all joints to default angles |

### Global Shortcuts

| Key             | Action                                   |
| --------------- | ---------------------------------------- |
| `Tab`           | Toggle between Direct and Kinematic mode |
| `Escape`        | Reset entire rig to origin pose          |
| `1` / `2` / `3` | Preset camera views (Iso / Top / Side)   |

## 2.3 Proposed `MockExcavatorRig` Redesign

`MockExcavatorRig` should expose:

```csharp
public sealed class MockExcavatorRig : MonoBehaviour
{
    [Header("Hierarchy")]
    public Transform swingPivot = null!;
    public Transform boomPivot = null!;
    public Transform stickPivot = null!;
    public Transform bucketPivot = null!;
    public Transform cuttingEdgeReference = null!;

    [Header("Direct Mode Speeds")]
    public float moveSpeedMetersPerSecond = 2.0f;
    public float verticalSpeedMetersPerSecond = 1.0f;

    [Header("Kinematic Mode Speeds (deg/s)")]
    public float swingSpeedDegreesPerSecond = 30.0f;
    public float boomSpeedDegreesPerSecond = 20.0f;
    public float stickSpeedDegreesPerSecond = 25.0f;
    public float bucketSpeedDegreesPerSecond = 35.0f;

    [Header("Joint Limits")]
    public float swingMin = -180f, swingMax = 180f;
    public float boomMin = -30f, boomMax = 70f;
    public float stickMin = -120f, stickMax = 30f;
    public float bucketMin = -90f, bucketMax = 45f;

    [Header("Control")]
    public RigControlMode controlMode = RigControlMode.Direct;

    public Vector3 CuttingEdgeWorldPosition =>
        cuttingEdgeReference != null ? cuttingEdgeReference.position : Vector3.zero;
}
```

### Design Decisions

- **Serialized Transform references** allow the rig to work with any naming convention.
- **Separate speed fields** for horizontal and vertical direct motion.
- **Joint clamping** prevents impossible poses.
- **Read-only `CuttingEdgeWorldPosition`** is the primary integration surface for grade math.

## 2.4 Visual Representation with Unity Primitives

| Segment                | Primitive    | Material Color          | Scale (m)          |
| ---------------------- | ------------ | ----------------------- | ------------------ |
| Body                   | Cube         | Safety Yellow (#F4C430) | (2.0, 1.0, 3.5)    |
| Cab                    | Cube         | Light Gray              | (1.2, 1.2, 1.0)    |
| LeftTrack / RightTrack | Cylinder     | Black                   | (0.4, 3.5, 0.4)    |
| BoomGeometry           | Cube         | Safety Yellow           | (0.35, 5.0, 0.35)  |
| StickGeometry          | Cube         | Safety Yellow           | (0.30, 3.0, 0.30)  |
| BucketGeometry         | Wedge        | Dark Gray               | (0.8, 0.4, 0.5)    |
| CuttingEdgeReference   | Small sphere | Red                     | (0.05, 0.05, 0.05) |

### Ground Plane

- Create a quad or plane at **Y = 0**.
- Scale to `(100, 1, 100)` for a 100 x 100 meter workspace.
- Material: semi-transparent grid shader or checkerboard texture at 1-meter intervals.

### Camera Presets

| Preset | Position     | Rotation      |
| ------ | ------------ | ------------- |
| Iso    | (20, 20, 20) | (35, -135, 0) |
| Top    | (0, 30, 0)   | (90, 0, 0)    |
| Side   | (15, 5, 0)   | (0, -90, 0)   |

## 2.5 Future IK Extension

A third control mode, `InverseKinematic`, can be added later:

1. The user defines a target transform.
2. The solver computes boom, stick, and bucket angles to place `CuttingEdgeReference` at the target.
3. A simple analytical solver (two-link IK with wrist adjustment) is sufficient.

---

# 3. Cab Monitor UI and RenderTexture System

## 3.1 RenderTexture Setup

The monitor display is driven by a `RenderTexture` bound to a dedicated UI camera.

| Property               | Value    | Notes                                           |
| ---------------------- | -------- | ----------------------------------------------- |
| Width                  | 1024     | Matches `RenderTextureMonitorBinder.width`      |
| Height                 | 600      | Matches `RenderTextureMonitorBinder.height`     |
| Color Format           | ARGB32   | Standard 32-bit color with alpha                |
| Depth Buffer           | 24       | For proper z-sorting of overlapping UI elements |
| Filter Mode            | Bilinear | Smooth scaling when viewed at angles            |
| Anti-aliasing          | None     | Disabled to reduce GPU cost                     |
| Auto Generate Mip Maps | False    | UI art does not benefit from mipmapping         |

The `RenderTextureMonitorBinder` MonoBehaviour creates the `RenderTexture` at runtime in `Start()` and assigns it to `uiCamera.targetTexture`. It then clones the screen mesh's material and binds the texture to `_MainTex`, `_BaseMap`, and `_EmissionMap` (with emission boost for visibility).

> **Recommendation:** Ensure the monitor screen mesh uses an Unlit or simple Lit material with emission so the display is visible under low-light scene conditions.

## 3.2 UI Camera Configuration

A dedicated `Camera` GameObject (`Monitor_UI_Camera`) renders the monitor UI to the RenderTexture.

| Property       | Value                   | Rationale                                                  |
| -------------- | ----------------------- | ---------------------------------------------------------- |
| Projection     | Orthographic            | UI is 2D; perspective distortion is undesirable            |
| Size           | 300                     | Half of render height (600) for 1:1 world-to-pixel mapping |
| Culling Mask   | UI only                 | Isolates monitor UI from world geometry                    |
| Clear Flags    | Solid Color             | Prevents scene bleed-through; use `#1A1A1A`                |
| Background     | `#1A1A1A`               | Matches industrial cab monitor aesthetic                   |
| Target Texture | `GradeAssist_MonitorRT` | Assigned by `RenderTextureMonitorBinder` at runtime        |
| Depth          | 0                       | Only camera in UI layer                                    |
| HDR            | Disabled                | Unnecessary for UI                                         |
| MSAA           | Disabled                | UI does not benefit from multisampling                     |

> **Important:** The UI camera must not be the same as the main scene camera. It is a child of the `CabMonitor` prefab root. Ensure `Monitor_UI_Camera` local rotation matches `Monitor_Screen` tilt (15-20 degrees backward toward operator seat).

## 3.3 Monitor Page System

### `MonitorPageManager`

A `MonoBehaviour` placed on the root `Canvas` GameObject (`Monitor_Canvas`) controls which page is visible.

```csharp
public sealed class MonitorPageManager : MonoBehaviour
{
    public GameObject grade2DPage = null!;
    public GameObject settingsPage = null!;
    public GameObject diagnosticsPage = null!;

    private GameObject? currentPage;

    private void Start() => ShowPage(grade2DPage);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) CyclePage();
    }

    public void ShowPage(GameObject? page)
    {
        if (currentPage != null) currentPage.SetActive(false);
        currentPage = page;
        if (currentPage != null) currentPage.SetActive(true);
    }

    private void CyclePage()
    {
        if (currentPage == grade2DPage) ShowPage(settingsPage);
        else if (currentPage == settingsPage) ShowPage(diagnosticsPage);
        else ShowPage(grade2DPage);
    }
}
```

### Page GameObjects

| Page GameObject    | Purpose                                           | Default Visibility |
| ------------------ | ------------------------------------------------- | ------------------ |
| `Page_Grade2D`     | Primary operating display with live grade data    | Active             |
| `Page_Settings`    | Target depth, slope, cross-slope, tolerance input | Inactive           |
| `Page_Diagnostics` | FPS, telemetry frame count, benchmark status      | Inactive           |

### Tab Navigation

Tab buttons are placed in a header row at the top of the canvas. Each tab is a `Button` with an `Image` background and a `Text` label.

| Tab   | Label  | Key Binding          |
| ----- | ------ | -------------------- |
| Tab 1 | Work   | M (cycles to Target) |
| Tab 2 | Target | M (cycles to System) |
| Tab 3 | System | M (cycles to Work)   |

Tab visuals:

- Active tab: background `#2D5F8A` (blue), text white, 2px bottom border
- Inactive tab: background `#333333`, text `#AAAAAA`

## 3.4 Grade 2D Page Layout

### Mock Layout Description

```text
+----------------------------------------------------------+
| [Work]  [Target]  [System]                               |
+----------------------------------------------------------+
|                                                          |
|   CUT DEPTH                           SLOPE              |
|   1.500 m                             0.00 %             |
|                                                          |
|   CROSS SLOPE                         DIRECTION          |
|   0.00 %                                 N               |
|                                                          |
|   +--------------------------------------------------+   |
|   |                                                  |   |
|   |           LIVE ERROR                             |   |
|   |           +0.023 m                               |   |
|   |                                                  |   |
|   |           [  ABOVE GRADE  ]                      |   |
|   |                                                  |   |
|   +--------------------------------------------------+   |
|                                                          |
|   Tolerance: 0.030 m          BENCHMARK: SET           |
|                                                          |
+----------------------------------------------------------+
```

### Field Specifications

| Field            | Data Type | Display Format                                                                | Color Coding                                                                                          | GameObject Name                        |
| ---------------- | --------- | ----------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | -------------------------------------- |
| Target cut depth | float     | `0.000 m` (3 decimals)                                                        | White                                                                                                 | `Txt_TargetCutDepth`                   |
| Slope %          | float     | `0.00 %` (2 decimals)                                                         | White                                                                                                 | `Txt_Slope`                            |
| Cross-slope %    | float     | `0.00 %` (2 decimals)                                                         | White                                                                                                 | `Txt_CrossSlope`                       |
| Grade direction  | Vector3   | Compass arrow or cardinal text                                                | White                                                                                                 | `Img_DirectionArrow` + `Txt_Direction` |
| Live error       | float     | Signed with prefix symbol: `▲ +0.000` / `▼ -0.000` / `= 0.000` m (3 decimals) | Green (#1DB954) / Red (#E63946) / Blue (#457B9D) text                                                 | `Txt_LiveError`                        |
| Status indicator | string    | `ON GRADE`, `ABOVE GRADE`, `BELOW GRADE`                                      | Green (#1DB954) / Red (#E63946) / Blue (#457B9D) banner fill, white bold text, pulsing when off-grade | `Pnl_StatusBanner` + `Txt_Status`      |
| Tolerance        | float     | `0.000 m` (3 decimals)                                                        | `#BBBBBB` (dim)                                                                                       | `Txt_Tolerance`                        |
| Benchmark set    | bool      | `SET` (green) / `NOT SET` (amber)                                             | `#1DB954` / `#F4A261` with gentle flash                                                               | `Txt_Benchmark`                        |

### Grade Direction Visual

The grade direction is displayed as a compass arrow (`Img_DirectionArrow`, a rotated `Image`) and a cardinal text label (`Txt_Direction`).

```csharp
float yaw = Mathf.Atan2(gradeDir.x, gradeDir.z) * Mathf.Rad2Deg;
imgDirectionArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -yaw);
```

Cardinal mapping:

| Yaw Range        | Label |
| ---------------- | ----- |
| -22.5 to 22.5    | N     |
| 22.5 to 67.5     | NE    |
| 67.5 to 112.5    | E     |
| 112.5 to 157.5   | SE    |
| 157.5 or -157.5  | S     |
| -157.5 to -112.5 | SW    |
| -112.5 to -67.5  | W     |
| -67.5 to -22.5   | NW    |

## 3.5 UI Framework Recommendation

Use **uGUI** (Unity's built-in `Canvas`, `Text`, `Image`, `Button` components) for all monitor UI.

### Rationale

| Factor                   | uGUI                                             | TextMeshPro (TMP)              |
| ------------------------ | ------------------------------------------------ | ------------------------------ |
| Version compatibility    | Works in Unity 2022.3 LTS without extra packages | Requires TMP package import    |
| External dependency      | Zero                                             | Adds package dependency        |
| Text clarity at 1024x600 | Acceptable for prototype                         | Superior, but not required     |
| Setup complexity         | Minimal                                          | Requires font asset generation |
| Future upgrade path      | Can migrate to TMP later                         | Baseline is heavier            |

For this prototype, uGUI keeps the Unity project self-contained and reduces setup friction.

### Canvas Configuration

| Property       | Value                 |
| -------------- | --------------------- |
| Render Mode    | Screen Space - Camera |
| Render Camera  | `Monitor_UI_Camera`   |
| Plane Distance | 100                   |
| Pixel Perfect  | **False**             |
| Sorting Layer  | UI                    |

> **Note:** Pixel Perfect is disabled because the monitor is viewed at a 15-30 degree angle, causing shimmering artifacts. If text appears blurry, increase RenderTexture resolution to 1280x720 instead of re-enabling Pixel Perfect.

## 3.6 Component Assignments

### CabMonitor Prefab Hierarchy

```text
CabMonitor
|-- Monitor_Body           (MeshRenderer, generic cab monitor housing mesh)
|-- Monitor_Bezel            (MeshRenderer, dark trim)
|-- Monitor_Screen           (MeshRenderer, screen surface; material gets RenderTexture)
|   |-- RenderTextureMonitorBinder (MonoBehaviour)
|-- Monitor_UI_Camera        (Camera, orthographic, UI culling mask)
|-- Monitor_Canvas           (Canvas, Screen Space - Camera)
|   |-- MonitorPageManager   (MonoBehaviour)
|   |-- HeaderRow            (Empty GO, layout group)
|   |   |-- Tab_Grade        (Button)
|   |   |-- Tab_Settings     (Button)
|   |   |-- Tab_Diagnostics  (Button)
|   |   |-- Txt_Title        (Text, "GradeAssist")
|   |-- Page_Grade2D         (GameObject, default active)
|   |   |-- Txt_TargetCutDepth
|   |   |-- Txt_Slope
|   |   |-- Txt_CrossSlope
|   |   |-- Img_DirectionArrow
|   |   |-- Txt_Direction
|   |   |-- Txt_LiveError
|   |   |-- Pnl_StatusBanner
|   |   |   |-- Txt_Status
|   |   |-- Txt_Tolerance
|   |   |-- Txt_Benchmark
|   |-- Page_Settings        (GameObject, inactive)
|   |-- Page_Diagnostics     (GameObject, inactive)
```

### MonoBehaviour to GameObject Mapping

| MonoBehaviour                | Host GameObject                       | Responsibility                                             |
| ---------------------------- | ------------------------------------- | ---------------------------------------------------------- |
| `RenderTextureMonitorBinder` | `Monitor_Screen`                      | Creates RenderTexture, binds to camera and screen material |
| `MonitorPageManager`         | `Monitor_Canvas`                      | Tab switching, page activation, input handling             |
| `GradeMonitorSimulator`      | `Monitor_Canvas` or `CabMonitor` root | Computes grade math, updates `Page_Grade2D` text fields    |
| `SettingsPageController`     | `Page_Settings`                       | Reads/writes target depth, slope, cross-slope, tolerance   |
| `DiagnosticsPageController`  | `Page_Diagnostics`                    | Shows FPS, frame time, telemetry replay progress           |

## 3.7 Audio Feedback (`GradeAudioFeedback`)

`GradeAudioFeedback` plays directional audio tones for eyes-free grade-status feedback.
It generates sine-wave clips at configurable frequencies for `AboveGrade`, `BelowGrade`, and `OnGrade`, pulsing them in a tone/pause cycle. An optional transition chime plays on every status change.

- **Host GameObject:** Attach to the `CabMonitor` root (requires an `AudioSource`).
- **Integration:** `GradeMonitorSimulator` calls `OnGradeStatusChanged(GradeStatus)` when status changes, driving both the visual `GradeStatusDisplay` and the audio tones.
- **Execution Order:** Updated in `GradeAudioFeedback.Update()` after `GradeMonitorSimulator.Update()`.

---

# 4. Grade Math Pipeline and Status Display

## 4.1 Core-to-Unity Mirroring Strategy

The Unity simulator must produce numerically identical grade results to the Core library. Unity uses `float`, `Vector3`, and `MonoBehaviour`; Core uses `double`, `Vector3D`, and plain C# classes. The mirroring strategy is **structural clone with precision downgrade at boundaries**.

### Mirrored Types

| Core Type             | Unity Mirror                           | Strategy                                                                                        |
| --------------------- | -------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `Vector3D`            | `UnityEngine.Vector3`                  | Used directly; precision loss from `double` to `float` is acceptable for visual simulation.     |
| `GradeTargetSettings` | `GradeTargetSettings` (plain C# class) | Duplicate fields and validation rules. Store as serializable plain class or `ScriptableObject`. |
| `GradePlane`          | `UnityGradePlane` (MonoBehaviour)      | Wrap the same math in a `MonoBehaviour` with serializable fields.                               |
| `GradeError`          | `GradeError` (readonly struct)         | Mirror exactly, using `float` instead of `double`.                                              |
| `GradeStatus`         | `GradeStatus` (enum)                   | Mirror enum values `BelowGrade`, `OnGrade`, `AboveGrade`.                                       |

### Why Not Reference Core Directly?

The Unity simulator is intentionally dependency-free. It may run in contexts where the Core assembly is not available. Duplicating the small surface area of grade math (~60 lines) is safer than adding an assembly reference.

### Precision Boundary

All math inside `UnityGradePlane` uses `float`. The Core tests use `double` with `Assert.Equal(expected, actual, 3)` (3 decimal places), so `float` precision is well within tolerance for visual simulation.

## 4.2 `UnityGradePlane` Class Design

`UnityGradePlane` is a `MonoBehaviour` that owns the grade plane state and performs live computation every frame.

### Serialized Fields

```csharp
public sealed class UnityGradePlane : MonoBehaviour
{
    [Header("Benchmark")]
    public Vector3 benchmarkPoint = Vector3.zero;

    [Header("Grade Direction")]
    public Vector3 gradeDirection = Vector3.forward; // XZ plane direction; Y ignored

    [Header("Target Settings")]
    public float targetCutDepthMeters = 1.5f;
    public float slopePercent = 0f;
    public float crossSlopePercent = 0f;
    public float toleranceMeters = 0.03f;

    private Vector3 cachedGradeDirXZ;
    private Vector3 cachedCrossDirXZ;
}
```

### Computation Methods

```csharp
public float HeightAt(Vector3 worldPoint)
{
    var delta = worldPoint - benchmarkPoint;
    var alongDistance = Vector3.Dot(delta, cachedGradeDirXZ);
    var crossDistance = Vector3.Dot(delta, cachedCrossDirXZ);

    return benchmarkPoint.y
        - targetCutDepthMeters
        + (slopePercent / 100f) * alongDistance
        + (crossSlopePercent / 100f) * crossDistance;
}

public GradeError ComputeError(Vector3 referencePoint)
{
    var targetY = HeightAt(referencePoint);
    var error = referencePoint.y - targetY;
    return new GradeError(referencePoint, targetY, error, toleranceMeters);
}
```

### Direction Caching

In `Start` and `OnValidate`:

```csharp
var flat = new Vector3(gradeDirection.x, 0, gradeDirection.z);
cachedGradeDirXZ = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
cachedCrossDirXZ = new Vector3(-cachedGradeDirXZ.z, 0, cachedGradeDirXZ.x);
```

The cross-direction is the perpendicular vector in the XZ plane: `(-z, 0, x)`. This matches the Core implementation `new Vector3D(-gradeDir.Z, 0, gradeDir.X)`.

### Inspector Helpers

- `OnDrawGizmosSelected()` draws the benchmark point (sphere), grade direction arrow, and cross-direction arrow.
- `OnValidate()` clamps `slopePercent` and `crossSlopePercent` to [-500, 500] and rejects non-finite values.

## 4.3 Live Computation Pipeline

```
MockExcavatorRig.cuttingEdgeReference.position
                    |
                    v
        UnityGradePlane.ComputeError()
                    |
       +------------+------------+
       |                         |
       v                         v
  HeightAt()               GradeError.Status
       |                         |
       v                         v
  targetY (float)        BelowGrade / OnGrade / AboveGrade
       |
       v
  error = refY - targetY
```

### Step-by-step

1. **Read cutting edge position** — `MockExcavatorRig` updates `cuttingEdgeReference.position` via keyboard input every `Update()`.
2. **Compute along/cross distances** — `UnityGradePlane` takes the delta from `benchmarkPoint` to the reference point and projects it onto `cachedGradeDirXZ` (along) and `cachedCrossDirXZ` (cross).
3. **Compute `targetY`** — Apply the full formula including cross-slope:
   ```
   targetY = benchmarkY - targetCutDepthMeters
           + slopeDecimal * alongDistance
           + crossSlopeDecimal * crossDistance
   ```
4. **Compute error** — `error = referencePoint.y - targetY`. Positive error means the bucket is above the target grade plane.
5. **Determine `GradeStatus`** — Compare `error` against `toleranceMeters`.

### Update Order

- `MockExcavatorRig` uses `Update()` to apply input-driven movement.
- `UnityGradePlane` uses `LateUpdate()` so it reads the final position after all movement is applied.
- UI reads from `UnityGradePlane` via UnityEvents fired from `UnityGradePlane`.

## 4.4 Status Display Mapping with Color-Blind Accessibility

The current `GradeMonitorSimulator` uses only text color, which fails for users with deuteranopia (~6% of males). The display must combine **shape, motion, and position cues** with color.

### Visual Encoding Table

| GradeStatus  | Color           | Shape              | Motion / Position                                         | Text Label    |
| ------------ | --------------- | ------------------ | --------------------------------------------------------- | ------------- |
| `AboveGrade` | Red (#E63946)   | Upward arrow (^)   | Arrow animates upward; indicator sits above center line   | "ABOVE GRADE" |
| `OnGrade`    | Green (#2A9D8F) | Circle / bullseye  | Static, centered                                          | "ON GRADE"    |
| `BelowGrade` | Blue (#457B9D)  | Downward arrow (v) | Arrow animates downward; indicator sits below center line | "BELOW GRADE" |

### Color-Blind Safe Palette

Chosen from a palette that remains distinguishable under protanopia, deuteranopia, and tritanopia simulations:

- Red: `#E63946`
- Green: `#2A9D8F` (teal-green, far from red on hue and brightness)
- Blue: `#457B9D` (muted steel blue)

These three colors are separated by > 40 in CIEDE2000 delta-E and have distinct brightness values.

### Implementation Notes

- Use a single `Image` for the status icon and swap its `Sprite` and `Color` based on `GradeStatus`.
- Arrow sprites should have distinct silhouettes.
- Add a subtle vertical bob animation to the arrow.
- The numeric error display always shows sign: `+0.052` or `-0.127` or `0.000`.

## 4.5 Data Flow Diagram

```
+------------------------+        +-----------------------+
| MockExcavatorRig       |        | UnityGradePlane       |
| - cuttingEdgeReference   |------>| - benchmarkPoint      |
| - moveSpeedMetersPerSec|        | - gradeDirection      |
| - Keyboard input (WASD)  |        | - slopePercent        |
+-----------+------------+        | - crossSlopePercent   |
            |                     | - toleranceMeters     |
            | position            |                       |
            v                     | ComputeError()        |
  +-------------------+          | HeightAt()            |
  | cuttingEdgeReference|-------->+                       |
  |  .position (Vector3)|          +----------+----------+
  +-------------------+                     |
                                            | GradeError
                                            v
                     +----------------------+----------------------+
                     |                                             |
                     v                                             v
            +----------------+                            +----------------+
            | MonitorPageManager|                           | Grade2D UI     |
            | - Receives error   |                           | - Status icon  |
            | - Formats strings  |                           | - Error text   |
            | - Drives UI events |                           | - Arrow sprite |
            +--------+---------+                           +--------+---------+
                     |                                              |
                     v                                              v
            +----------------+                            +----------------+
            | RenderTexture    |                            | Canvas (World) |
            | MonitorBinder    |                            | Screen space   |
            | - uiCamera       |                            +----------------+
            | - screenRenderer |
            +----------------+
```

### Event Contract

```csharp
public readonly struct GradeError
{
    public readonly Vector3 ReferencePoint;
    public readonly float TargetY;
    public readonly float ErrorMeters;
    public readonly float ToleranceMeters;
    public GradeStatus Status => ...;
}

// Fired by UnityGradePlane whenever error or status changes
public event Action<GradeError> OnGradeErrorChanged;
```

`MonitorPageManager` registers in `OnEnable`, unregisters in `OnDisable`.

## 4.6 Validation Strategy — Unity Math Matches Core Math Exactly

### Layer 1 — Unit Tests in Core (Already Exists)

The `GradeAssist.Tests` project verifies Core math with deterministic inputs.

### Layer 2 — Play-Mode Unit Tests in Unity

Add a Unity Test Framework play-mode test that:

1. Creates a `GameObject` with `UnityGradePlane`.
2. Sets the same inputs as a Core test case.
3. Calls `HeightAt()` and `ComputeError()`.
4. Asserts that `targetY` matches the Core expected value within `0.001f`.
5. Asserts that `GradeStatus` matches exactly.

Test path: `src/GradeAssist.UnitySim/Assets/Tests/Runtime/GradePlaneMirrorTests.cs`

### Layer 3 — Runtime Cross-Check Script

Add a debug-only `GradeMathValidator` MonoBehaviour that can be attached during play-mode testing. It:

1. Samples `UnityGradePlane.HeightAt()` for 100 random points.
2. Runs the same calculation with a verbatim copy of the Core `GradePlane` code (using `double` internally).
3. Logs any delta > `1e-4` as a warning.
4. Provides a "Validate Grade Math" button in the Inspector.

### Regression Guard

When the Core `GradePlane` formula changes, the change must be:

1. Implemented in Core with tests.
2. Ported to `UnityGradePlane`.
3. Verified by the play-mode test suite.
4. Documented in the change log with a note that Unity mirror was updated.

---

# 5. Manual Test Checklist

The following 14 checks must be performed manually in the Unity Editor Play mode before any milestone gate is signed off.

| #   | Test                             | Steps                                                                             | Expected Result                                                                | Pass Criteria                                                   |
| --- | -------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| 1   | **Scene load**                   | Open `Assets/Scenes/GradeAssistSimulator.unity`. Enter Play mode.                 | Scene renders without console errors.                                          | Zero exceptions in the first 5 seconds.                         |
| 2   | **Rig hierarchy**                | Inspect `MockExcavator` in the Hierarchy.                                         | Contains `Boom > Stick > Bucket > CuttingEdgeReference` in parent-child order. | All transforms exist and nested correctly.                      |
| 3   | **Keyboard bucket move**         | Hold Arrow keys and PageUp/PageDown.                                              | `CuttingEdgeReference` moves in world space.                                   | Movement is smooth at ~1 m/s. No drift when keys released.      |
| 4   | **Benchmark set**                | Press `B` while bucket is at an arbitrary height.                                 | Monitor text updates `benchmarkY` to the bucket's current Y.                   | `benchmarkY` matches the bucket Y within 0.001 m.               |
| 5   | **Grade display — ON GRADE**     | Position bucket so error is within `toleranceMeters` (default 0.03 m).            | Monitor reads `Status: ON GRADE`.                                              | Error text is green.                                            |
| 6   | **Grade display — ABOVE GRADE**  | Raise bucket above target plane by > 0.03 m.                                      | Monitor reads `Status: ABOVE GRADE`.                                           | Error text shows positive value. Indicator points up / red.     |
| 7   | **Grade display — BELOW GRADE**  | Lower bucket below target plane by > 0.03 m.                                      | Monitor reads `Status: BELOW GRADE`.                                           | Error text shows negative value. Indicator points down / blue.  |
| 8   | **Slope math**                   | Set `slopePercent` to 10%, move bucket 10 m along grade direction.                | `targetY` drops by 1.0 m relative to benchmark.                                | Computed error is within 0.001 m of expected.                   |
| 9   | **Cross-slope math**             | Set `crossSlopePercent` to 5%, move bucket 20 m perpendicular to grade direction. | `targetY` drops by 1.0 m.                                                      | Error sign and magnitude match formula.                         |
| 10  | **Monitor pages toggle**         | Press `M` (or click tab buttons).                                                 | Canvas switches between Grade, Settings, and Diagnostics pages.                | Only one page is visible at a time. No exceptions.              |
| 11  | **RenderTexture binding**        | Select `Monitor_Screen` in Scene view.                                            | Material shows live RenderTexture output from `Monitor_UI_Camera`.             | Screen texture is not pink or black.                            |
| 12  | **RenderTexture resolution**     | Inspect `RenderTextureMonitorBinder` in Inspector.                                | `width` and `height` are 1024 x 600.                                           | RT format is ARGB32, depth 24.                                  |
| 13  | **Safety gate — no game writes** | Confirm no script references `C:\XboxGames`, `Steam`, or game install paths.      | Search Project yields zero hits.                                               | Zero file-write calls outside `Application.persistentDataPath`. |
| 14  | **Telemetry replay driver**      | Load a sample telemetry CSV from `telemetry/samples/`. Enter Play mode.           | Bucket position is driven by timestamp-ordered samples.                        | Final position matches last sample within 0.001 m.              |

---

# 6. Unity Version Assumptions

## 6.1 Minimum LTS Version

- **Unity 2022.3 LTS** (or newer stable LTS).
- Rationale: `RenderTexture` binding via script is stable in 2022.3; IL2CPP and Mono backends are both supported; long-term patch support reduces toolchain churn.

## 6.2 Render Pipeline Compatibility

- **Built-in Render Pipeline** (default).
- Rationale: The `RenderTextureMonitorBinder` shader-property checks (`_MainTex`, `_BaseMap`, `_EmissionMap`) are written for Built-in Standard shaders. URP/HDRP would require material/shader upgrades.

## 6.3 Scripting Backend

- **Mono** (Editor / rapid iteration).
- **IL2CPP** is acceptable for final standalone builds but not required for the prototype.
- Core source is plain C# 11 / .NET Standard 2.1 compatible; no Unity-specific `#if` guards are needed.

## 6.4 Platform

- **Standalone Windows x86_64**.
- Input: keyboard only (`Input.GetKey`). No gamepad or joystick dependencies.

## 6.5 Known Compatibility Notes

| Feature                     | Minimum Version | Note                                                  |
| --------------------------- | --------------- | ----------------------------------------------------- |
| `RenderTexture` constructor | 2019.4+         | Used in `RenderTextureMonitorBinder`.                 |
| `Camera.targetTexture`      | 5.0+            | Standard API; no version risk.                        |
| `Material.HasProperty`      | 5.0+            | Used for shader-agnostic texture binding.             |
| uGUI                        | 2018.4+         | Standard `UnityEngine.UI.Text` works in all versions. |

---

# 7. Risks and Fallback Plan

### R-U-001 — Core Math Drift

| Field           | Value                                                                                                                                                                       |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description** | Grade math implemented in `GradeAssist.Core` may diverge from the Unity script implementation due to float-vs-double precision differences or formula transcription errors. |
| **Severity**    | High (silent wrong-grade errors).                                                                                                                                           |
| **Fallback**    | 1. Single-source-of-truth: Unity references Core `.cs` files directly (source mirror). 2. Add deterministic play-mode tests comparing Core and Unity outputs bit-for-bit.   |

### R-U-002 — RenderTexture Unsupported Hardware

| Field           | Value                                                                                                                                                                                                            |
| --------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description** | Target development or demo machine may lack GPU RT support.                                                                                                                                                      |
| **Severity**    | Medium (monitor screen stays black).                                                                                                                                                                             |
| **Fallback**    | 1. Detect failure in `RenderTextureMonitorBinder.Start()`; if RT creation fails, fall back to a standard `RawImage` on a world-space Canvas placed directly on the monitor mesh. 2. Lower resolution to 512x300. |

### R-U-003 — Project Bloat from Binary Assets

| Field           | Value                                                                                                                                                                           |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description** | Unity `.unitypackage` imports or large textures can bloat the repo.                                                                                                             |
| **Severity**    | Medium (CI and release friction).                                                                                                                                               |
| **Fallback**    | 1. `.gitignore` excludes `Library/`, `Temp/`, `Obj/`, `*.unitypackage`. 2. Enforce a 2 MB per-file limit in safety gates. 3. Use Unity primitives for all placeholder geometry. |

### R-U-004 — Input System Differences

| Field           | Value                                                                                                                                                              |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Description** | Unity's new Input System conflicts with the legacy `Input.GetKey` API.                                                                                             |
| **Severity**    | Medium (controls stop working).                                                                                                                                    |
| **Fallback**    | 1. Keep using legacy `Input` class; do not enable the new Input System package. 2. If required, create a thin `IInputSource` abstraction with two implementations. |

### R-U-005 — Assembly Reference Failure

| Field           | Value                                                                                                                                                                                                                                                           |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description** | `GradeAssist.Core` is a `net8.0` class library. Unity may fail to reference it.                                                                                                                                                                                 |
| **Severity**    | High (scripts will not compile).                                                                                                                                                                                                                                |
| **Fallback**    | 1. **Preferred:** Source mirror (copy Core `.cs` files into Unity Assets). 2. **Fallback A:** Compile Core as a .NET Standard 2.1 DLL. 3. **Fallback B:** If both fail, copy Core `.cs` files into `Assets/Scripts/GradeAssist/Core/` and document duplication. |

### R-U-006 — Scene Object Wiring Errors

| Field           | Value                                                                                                                                                                                                 |
| --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description** | Public fields (`rig`, `gradeText`, `uiCamera`, `screenRenderer`) must be wired in the Inspector. Missing references cause null-reference exceptions.                                                  |
| **Severity**    | Medium (runtime crash on scene start).                                                                                                                                                                |
| **Fallback**    | 1. Add `[SerializeField]` inspector tooltips. 2. Implement `OnValidate()` to log warnings for null required fields. 3. Provide a `SetupWizard` editor script that auto-finds components by name/type. |

---

# 8. File Tree to Create

The implementer must produce the following folders and files under `src/GradeAssist.UnitySim/`.

```text
src/GradeAssist.UnitySim/
├── README.md                              (update with wiring instructions)
├── .gitignore                             (exclude Library/, Temp/, Obj/, logs/)
├── Assets/
│   ├── Scenes/
│   │   └── GradeAssistSimulator.unity     (main simulator scene)
│   ├── Scripts/
│   │   ├── Core/                          # Mirror of GradeAssist.Core types
│   │   │   ├── Vector3D.cs
│   │   │   ├── GradePlane.cs
│   │   │   ├── GradeTargetSettings.cs
│   │   │   ├── GradeError.cs
│   │   │   ├── GradeStatus.cs
│   │   │   └── Controllers/
│   │   │       ├── SimulatedAssistController.cs
│   │   │       ├── SimulatedBucketAssistController.cs
│   │   │       ├── SimulatedGradeAssistController.cs
│   │   │       ├── SimulatedSwingAssistController.cs
│   │   │       ├── SimulatedEFenceController.cs
│   │   │       ├── ControllerState.cs
│   │   │       ├── ControllerOutput.cs
│   │   │       └── ControllerMockState.cs
│   │   ├── Rig/
│   │   │   ├── MockExcavatorRig.cs        (redesign with direct + kinematic modes)
│   │   │   └── BucketReferenceTracker.cs
│   │   ├── Monitor/
│   │   │   └── MonitorInputRouter.cs      (mouse raycast for buttons)
│   │   ├── UI/
│   │   │   ├── GradeStatusDisplay.cs        (color + arrow for ON/ABOVE/BELOW)
│   │   │   └── GradeDirectionIndicator.cs   (compass arrow + cardinal text)
│   │   ├── Math/
│   │   │   └── UnityGradePlane.cs           (MonoBehaviour wrapping grade formula)
│   │   ├── Editor/
│   │   │   └── SetupWizard.cs               (auto-wires public fields on scene objects)
│   │   ├── GradeMonitorSimulator.cs         (refactor to use UnityGradePlane)
│   │   ├── RenderTextureMonitorBinder.cs
│   │   └── MonitorPageManager.cs              (page switching)
│   ├── Materials/
│   │   ├── MonitorScreen.mat              (Standard shader with _MainTex / _EmissionMap)
│   │   ├── MonitorBezel.mat               (dark grey generic plastic)
│   │   └── MonitorBody.mat                (generic grey)
│   ├── Prefabs/
│   │   ├── MockExcavator.prefab           (root with Boom/Stick/Bucket/CuttingEdgeReference)
│   │   ├── CabMonitor.prefab              (Monitor_Body + Bezel + ScreenMesh + Buttons)
│   │   └── UIRoot.prefab                  (World-space Canvas + MonitorCamera)
│   ├── Resources/
│   │   └── GradeAssistConfig.json         (default benchmark, slope, tolerance values)
│   └── StreamingAssets/
│       └── TelemetrySamples/              (symlink or copy of telemetry/samples/)
│           └── sample_run_01.csv
├── Packages/
│   └── manifest.json                        (list: com.unity.ugui, optionally com.unity.probuilder)
├── ProjectSettings/
│   ├── ProjectSettings.asset                (company: GradeAssist, product: UnitySim)
│   ├── EditorSettings.asset                 (assembly definition mode: auto)
│   ├── TagManager.asset                     (tags: MonitorScreen, CuttingEdge)
│   ├── InputManager.asset                   (legacy keyboard axes)
│   └── GraphicsSettings.asset               (Built-in RP, color space: Linear)
└── docs/
    └── UnityWiring.md                       (step-by-step Inspector wiring guide)
```

### Notes on the tree

- `Core/` is a partial source mirror of `GradeAssist.Core` (5 files mirrored; Controllers/ are not yet mirrored). It should not be committed to source control; use `scripts/sync-core-to-unity.ps1` (not yet implemented) to refresh it.
- `SetupWizard.cs` lives in `Editor/` and is stripped from player builds.
- `MonitorPageManager.cs` implements `M` key page cycling and tab button clicks.
- `GradeStatusDisplay.cs` updates a UI `Image` color and sprite based on `GradeStatus`.
- All `.mat` files use the Built-in Standard shader with generic colors only (no proprietary textures or logos).
- `TelemetrySamples/` in `StreamingAssets` is copied verbatim to the build output and readable at runtime.

---

_Document generated by 5-architect team design session. Date: 2026-04-28_
