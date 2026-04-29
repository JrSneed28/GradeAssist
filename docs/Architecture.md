# Architecture

## Overview

GradeAssist is a standalone excavator grade-assist engineering prototype. It has **no dependency on, and makes no modifications to, any game runtime** (see [RuntimeFeasibilityResult.md](RuntimeFeasibilityResult.md)).

The system splits into four assemblies:

- **GradeAssist.Core** — deterministic grade plane math.
- **GradeAssist.Tests** — dependency-free console regression harness.
- **GradeAssist.Replay** — offline telemetry CSV/JSON replay and reporting.
- **GradeAssist.UnitySim** — standalone Unity simulator with a mock rig and cab monitor.

## Project Structure

```text
src/
├── GradeAssist.Core/
│   ├── GradePlane.cs
│   ├── GradeError.cs, GradeStatus.cs
│   ├── GradeTargetSettings.cs
│   ├── Vector3D.cs
│   └── Controllers/           # Simulated assist controller stubs
├── GradeAssist.Tests/
│   ├── GradePlaneTests.cs
│   ├── ConfigParsingTests.cs
│   ├── ControllerSafetyTests.cs
│   └── TelemetryReplayTests.cs
├── GradeAssist.Replay/
│   ├── TelemetryReplayEngine.cs
│   ├── CsvTelemetryReader.cs
│   ├── CsvReportWriter.cs
│   └── MarkdownReportWriter.cs
└── GradeAssist.UnitySim/
    ├── Assets/Scripts/
    │   ├── Core/             # Source mirror of GradeAssist.Core
    │   ├── Math/
    │   ├── Rig/
    │   ├── Monitor/
    │   └── UI/
    ├── Assets/Scenes/
    └── Assets/Prefabs/
```

## Data Flow

```
Bucket reference point (World 3D)
            |
            v
    GradePlane.ComputeError()
            |
   +--------+--------+
   |                 |
   v                 v
GradeError      GradeStatus
   |                 |
   v                 v
 numeric text    color + icon
   +-------+-------+
           |
           v
   Cab monitor display (RenderTexture)
```

1. The rig exposes a **bucket reference point** (a `Transform` position in Unity, or a `Vector3D` in Core).
2. `GradePlane.HeightAt()` computes the target elevation on the grade plane.
3. `GradePlane.ComputeError()` returns a `GradeError` value and a `GradeStatus` (`BelowGrade`, `OnGrade`, `AboveGrade`).
4. The monitor UI formats the error string and updates the status indicator.

## Grade Math Pipeline

The canonical formula lives in `GradePlane.cs` and is documented in [GradeMath.md](GradeMath.md):

```text
targetY = benchmarkY - targetCutDepthMeters
          + slopeDecimal * alongDistance
          + crossSlopeDecimal * crossDistance

errorMeters = referencePoint.y - targetY
```

- Positive `targetCutDepthMeters` means the desired grade is **below** the benchmark.
- `slopeDecimal = slopePercent / 100`.
- `crossSlopeDecimal = crossSlopePercent / 100`.
- Grade direction is a flattened normalized XZ vector; cross-slope is perpendicular to it.
- Positive error means the bucket reference point is **above** the target grade.

## Unity Simulator Architecture

### Source Mirror Strategy (Option B)

Unity consumes Core math via a **partial source mirror**: the `.cs` files from `src/GradeAssist.Core/` are copied into `src/GradeAssist.UnitySim/Assets/Scripts/Core/`. Currently 5 files are mirrored (grade math types); Controllers/ and config classes are not yet mirrored. See [UnitySimulatorDesign.md](UnitySimulatorDesign.md) §1.3 for the full options analysis.

### Scene Hierarchy

```text
GradeAssistSimulator
├── Environment
│   └── GroundPlane
├── MockExcavator
│   └── Body / SwingPivot / BoomPivot / StickPivot / BucketPivot
│       └── CuttingEdgeReference
└── CabMonitor
    ├── Monitor_Body / Monitor_Bezel / Monitor_Screen
    ├── Monitor_UI_Camera
    └── Monitor_Canvas
```

### Key MonoBehaviour Mapping

| Script                       | Host GameObject        | Responsibility                       |
| ---------------------------- | ---------------------- | ------------------------------------ |
| `MockExcavatorRig`           | `MockExcavator`        | Keyboard-driven bucket ref mover     |
| `BucketReferenceTracker`     | `CuttingEdgeReference` | Exposes world position               |
| `UnityGradePlane`            | `SimulationDirector`   | Computes grade error every frame     |
| `GradeMonitorSimulator`      | `Monitor_Canvas`       | Formats and drives UI text           |
| `RenderTextureMonitorBinder` | `Monitor_Screen`       | Creates RT, binds to screen material |
| `MonitorPageManager`         | `Monitor_Canvas`       | Tab/page switching                   |
| `GradeStatusDisplay`         | Status indicator       | Swaps color and sprite by status     |
| `GradePlaneVisualizer`       | `SimulationDirector`   | Debug gizmos (Editor only)           |

### RenderTexture Setup

`RenderTextureMonitorBinder` creates a 1024x600 `RenderTexture` at runtime, assigns it to an orthographic UI camera, and binds it to the monitor screen mesh material (`_MainTex` / `_EmissionMap`). See [UnitySimulatorDesign.md](UnitySimulatorDesign.md) §3.1.

## Technology Stack

| Layer            | Technology                                 |
| ---------------- | ------------------------------------------ |
| Core library     | .NET 8, C# 11                              |
| Test harness     | xUnit (dependency-free console runner)     |
| Replay tool      | .NET 8 console app                         |
| Simulator        | Unity 2022.3 LTS, Built-in Render Pipeline |
| Blender blockout | Blender 3.x+ (generic monitor assembly)    |

Core compiles with `Nullable` enabled and zero external package dependencies.

## Key Design Decisions

1. **Source mirror over DLL reference** — Unity's Mono runtime may not fully support `net8.0` APIs, and IL2CPP can strip types. Copying source eliminates both risks for a small (~10 file) surface area.

2. **Dependency-free tests** — `GradeAssist.Tests` references only Core and xUnit. It requires no Unity Editor, no game install, and no GPU, so it runs in CI in seconds.
