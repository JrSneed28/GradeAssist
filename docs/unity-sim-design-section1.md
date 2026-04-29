# GradeAssist.UnitySim — Project Architecture Design (Section 1)

## Overview

This document defines the overall Unity project architecture for `GradeAssist.UnitySim`, a standalone cab-monitor simulator that consumes the math and controller models from `GradeAssist.Core`.

The simulator is **not** a Construction Simulator mod. It runs as an independent Unity executable and uses keyboard input to move a mock excavator cutting-edge reference while a cab monitor displays real-time grade error computed by the shared core library.

---

## A. Folder Structure

```text
src/GradeAssist.UnitySim/
├── Assets/
│   ├── Scenes/
│   │   └── GradeAssistSimulator.unity          # Primary simulation scene
│   ├── Scripts/
│   │   ├── Core/                               # Mirror of GradeAssist.Core (see §B)
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
│   │   │   └── BucketReferenceTracker.cs         # Reports world pos to monitors
│   │   ├── Monitor/
│   │   │   ├── GradeMonitorSimulator.cs        # Grade math → UI text
│   │   │   ├── RenderTextureMonitorBinder.cs   # Camera RT → screen mesh
│   │   │   ├── MonitorInputRouter.cs           # Mouse / button event routing
│   │   │   └── MonitorPageController.cs          # Page state machine
│   │   └── UI/
│   │       └── GradeStatusDisplay.cs             # Visual status indicator
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

---

## B. Core Math Integration Options

`GradeAssist.Core` targets `net8.0`. Unity must consume these types at runtime. Three integration patterns are evaluated below.

### Option 1 — Pre-built DLL Reference

Build `GradeAssist.Core` as a class-library DLL and place it in `Assets/Plugins/`.

| Pros | Cons |
|------|------|
| Single artifact; no source duplication | Unity Mono runtime may not fully support `net8.0` APIs (records, `init`-only properties, etc.) depending on Unity version |
| Easy version pinning | IL2CPP builds can strip required types if not careful |
| Clean separation of concerns | Requires the DLL to be re-copied on every Core change |

**Verdict:** Risky for a prototype. Modern Unity (2022.3 LTS+) supports .NET Standard 2.1, but `net8.0`-specific language features can still cause runtime surprises.

### Option 2 — Source Mirror

Copy the `.cs` files from `src/GradeAssist.Core/` into `Assets/Scripts/Core/` and let Unity compile them directly.

| Pros | Cons |
|------|------|
| Zero runtime compatibility risk | Source duplication |
| Unity compiles with its own Roslyn; all C# 11 features work | Must be re-copied when Core changes |
| Debugging is seamless | |

**Verdict:** Safest for a prototype. The Core project is small (≈ 10 files) and has zero external dependencies, so duplication cost is minimal.

### Option 3 — Assembly Definition (`asmdef`) + DLL

Create a Unity Assembly Definition that references the pre-built DLL.

| Pros | Cons |
|------|------|
| Logical grouping in Unity | Still carries the `net8.0` / Mono compatibility risk of Option 1 |
| Faster incremental compile times | Extra Unity-specific metadata file to maintain |

**Verdict:** Adds complexity without removing the compatibility risk.

### Recommendation

**Use Option 2 (Source Mirror) with an automated copy step.**

A PowerShell script `scripts/sync-core-to-unity.ps1` should:

1. Mirror `src/GradeAssist.Core/*.cs` and `src/GradeAssist.Core/Controllers/*.cs` into `src/GradeAssist.UnitySim/Assets/Scripts/Core/`.
2. Strip or adjust the namespace `using` declarations if needed (they are already plain C# with no external deps, so no change is required).
3. Add a `.gitignore` entry so the mirrored folder is not committed to source control.

This keeps the prototype robust while preserving the ability to switch to Option 1 later once Unity runtime compatibility is verified.

---

## C. Scene Hierarchy

Exact GameObject names and parent-child relationships for `GradeAssistSimulator.unity`.

```text
GradeAssistSimulator
├── Environment
│   ├── GroundPlane                 # MeshRenderer + collider for visual reference
│   └── DirectionalLight            # Scene lighting
├── MockExcavator
│   ├── BoomPivot                   # Empty parent for boom articulation
│   │   └── BoomMesh                # Visual mesh (cylinder or box)
│   ├── StickPivot                  # Empty parent for stick articulation
│   │   └── StickMesh               # Visual mesh
│   ├── BucketPivot                 # Empty parent for bucket articulation
│   │   └── BucketMesh              # Visual mesh
│   └── CuttingEdgeReference        # Transform used for grade math
│       └── VisualMarker            # Small sphere gizmo for visibility
├── CabMonitor
│   ├── Monitor_Body                # Main housing mesh
│   ├── Monitor_Bezel               # Front frame mesh
│   ├── Monitor_Screen              # Quad mesh that displays the RenderTexture
│   ├── Button_F1                   # Physical button mesh (collider for raycast)
│   ├── Button_F2
│   ├── Button_F3
│   ├── Button_F4
│   ├── Button_Home
│   ├── Button_Back
│   └── Knob_Rotary               # Rotary encoder mesh
├── UIRoot
│   └── MonitorCanvas               # World Space Canvas
│       ├── GradePanel              # Background panel
│       ├── GradeText               # Text element updated by GradeMonitorSimulator
│       ├── StatusIndicator         # Color-coded status image
│       └── UICamera                # Camera rendering this canvas to RT
└── SimulationDirector
    └── GradePlaneVisualizer        # Draws debug lines showing the grade plane
```

### Rationale

- **MockExcavator** groups all rig geometry. Only `CuttingEdgeReference` matters for math; the pivots and meshes are visual-only and may be animated later.
- **CabMonitor** is a static prop. `Monitor_Screen` is the only dynamic visual element; its material is updated by `RenderTextureMonitorBinder`.
- **UIRoot / MonitorCanvas** is a `World Space` canvas positioned just in front of `Monitor_Screen`. The `UICamera` looks at the canvas and outputs to a `RenderTexture`.
- **SimulationDirector** hosts scene-level managers (grade plane visualization, benchmarking shortcuts) that do not belong to individual props.

---

## D. Input Routing

### Rig Control — Keyboard

| Key | Action |
|-----|--------|
| `UpArrow` | Move cutting-edge reference +Z (forward) |
| `DownArrow` | Move cutting-edge reference -Z (backward) |
| `LeftArrow` | Move cutting-edge reference -X (left) |
| `RightArrow` | Move cutting-edge reference +X (right) |
| `PageUp` | Move cutting-edge reference +Y (up) |
| `PageDown` | Move cutting-edge reference -Y (down) |
| `B` | Set current cutting-edge position as new benchmark |
| `R` | Reset rig to origin |

**Handler:** `MockExcavatorRig.Update()` reads `Input.GetKey` for continuous movement and `Input.GetKeyDown` for discrete actions (`B`, `R`).

### Monitor Navigation — Mouse

| Interaction | Action |
|-------------|--------|
| Click `Button_F1` … `Button_F4` | Trigger soft-key actions (context-dependent per page) |
| Click `Button_Home` | Return to main grade page |
| Click `Button_Back` | Go to previous page |
| Scroll / drag `Knob_Rotary` | Adjust numeric values (depth, slope, tolerance) |

**Handler:** `MonitorInputRouter` uses Unity’s `Physics.Raycast` from `Camera.main` on `MouseDown`. When a button collider is hit, it broadcasts a `MonitorButton` enum value to `MonitorPageController`.

### Separation of Concerns

```text
Input devices
    ├─ Keyboard → MockExcavatorRig (rig physics / transform)
    └─ Mouse    → MonitorInputRouter → MonitorPageController (UI state)
```

No input handler directly touches both rig and monitor state. This prevents accidental coupling and makes the simulator testable in pieces.

---

## E. MonoBehaviour Script-to-GameObject Mapping

| Script | GameObject | Purpose |
|--------|------------|---------|
| `MockExcavatorRig` | `MockExcavator` | Reads keyboard, moves `CuttingEdgeReference` |
| `BucketReferenceTracker` | `CuttingEdgeReference` | Exposes world position to other scripts via event |
| `GradeMonitorSimulator` | `MonitorCanvas` | Computes grade error, updates `GradeText` |
| `RenderTextureMonitorBinder` | `Monitor_Screen` | Creates RT, assigns to screen material, links `UICamera` |
| `MonitorInputRouter` | `CabMonitor` | Raycasts mouse clicks against button colliders |
| `MonitorPageController` | `MonitorCanvas` | Manages page stack, updates visible UI panels |
| `GradeStatusDisplay` | `StatusIndicator` (child of `MonitorCanvas`) | Changes color/image based on `GradeStatus` |
| `GradePlaneVisualizer` | `SimulationDirector` | Draws debug gizmos showing benchmark point and grade plane |

### Component Dependencies (wiring diagram)

```text
MockExcavatorRig
    └─ cuttingEdgeReference → Transform (CuttingEdgeReference)

GradeMonitorSimulator
    ├─ rig                  → MockExcavatorRig
    ├─ gradeText            → Text (GradeText)
    ├─ statusDisplay        → GradeStatusDisplay
    └─ settings             → GradeTargetSettings (ScriptableObject or serialized)

RenderTextureMonitorBinder
    ├─ uiCamera             → Camera (UICamera)
    └─ screenRenderer       → Renderer (Monitor_Screen)

MonitorInputRouter
    └─ pageController       → MonitorPageController

MonitorPageController
    ├─ monitorSimulator   → GradeMonitorSimulator (to update settings)
    └─ pages[]              → Panel GameObjects (children of MonitorCanvas)

GradePlaneVisualizer
    ├─ monitorSimulator   → GradeMonitorSimulator (reads benchmark / settings)
    └─ lineMaterial         → Material (debug line)
```

### Execution Order

1. `RenderTextureMonitorBinder.Start()` — allocates RenderTexture, binds material.
2. `MockExcavatorRig.Start()` — caches initial reference position.
3. `GradeMonitorSimulator.Start()` — captures benchmark from rig, initializes UI.
4. `MonitorPageController.Start()` — shows default page, hides others.
5. `MonitorInputRouter.Update()` — polls mouse, dispatches button events.
6. `MockExcavatorRig.Update()` — polls keyboard, applies movement.
7. `GradeMonitorSimulator.Update()` — recomputes grade error, refreshes text.
8. `GradePlaneVisualizer.Update()` — draws debug plane (Editor / Development builds only).

---

## Appendix — ScriptableObject Settings Asset

To allow designers to tweak grade parameters without editing scene objects, a `GradeTargetSettingsAsset` ScriptableObject should live in `Assets/Resources/`:

```text
Assets/
└── Resources/
    └── DefaultGradeSettings.asset
```

Fields: `TargetCutDepthMeters`, `SlopePercent`, `CrossSlopePercent`, `ToleranceMeters`.

`GradeMonitorSimulator` references this asset. Changing the asset updates all monitors in all scenes that use it.
