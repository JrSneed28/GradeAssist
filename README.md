# GradeAssist

> **⚠️ Project Status: INCOMPLETE**
>
> GradeAssist is an engineering prototype. Core grade math and the test harness are functional, but the Unity simulator, telemetry replay engine, and several wished & planned features are still under development. **This project is not production-ready and probably will never be.** See the [Incomplete Areas](#incomplete-areas) section for details.

---

## Table of Contents

- [Project Status](#project-status)
- [Features](#features)
- [Architecture](#architecture)
- [Grade Math](#grade-math)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Building](#building)
- [Running Tests](#running-tests)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [Telemetry Replay](#telemetry-replay)
- [Unity Simulator](#unity-simulator)
- [Incomplete Areas](#incomplete-areas)
- [Documentation](#documentation)

---

## Project Status

| Area | Status |
|---|---|
| Core grade math (`GradeAssist.Core`) | ✅ Functional/Complete|
| Unit & regression tests (`GradeAssist.Tests`) | ✅ Functional/Complete |
| Telemetry replay engine (`GradeAssist.Replay`) | 🚧 Functional/Incomplete |
| Unity simulator (`GradeAssist.UnitySim`) | 🚧 Functional/Incomplete |
| Blender monitor blockout | 🚧 Functional/Incomplete |
| Full controller state machine | 🚧 Functional/Incomplete |
| CI pipeline | 🚧 Functional/Incomplete |
| Documentation | 🚧 Partial |

---

## Features

- **Deterministic grade plane math** — computes target elevation from a benchmark point, cut depth, slope percent, and cross-slope percent.
- **Grade error & status** — reports signed error in metres (`BelowGrade` / `OnGrade` / `AboveGrade`) with a configurable tolerance band.
- **Simulated assist controllers** — stubbed state-machine controllers for grade assist, bucket assist, e-fence, and swing assist with kill switch, output clamp, rate limiting, and watchdog timeout.
- **Telemetry replay** — replays CSV/JSON telemetry files, computes grade errors, and writes Markdown/CSV reports.
- **Unity simulator** — keyboard-driven mock excavator rig with a cab monitor RenderTexture display of live grade error.
- **Schema-validated config** — JSON config files for machine definitions, grade targets, mount profiles, rig maps, assist tuning, safety policy, keybinds, and Unity settings.

---

## Architecture

The solution is split into four assemblies:

| Assembly | Description |
|---|---|
| `GradeAssist.Core` | Deterministic grade plane math — no Unity dependency |
| `GradeAssist.Tests` | Dependency-free xUnit regression harness |
| `GradeAssist.Replay` | Offline telemetry CSV/JSON replay and reporting |
| `GradeAssist.UnitySim` | Standalone Unity 2022.3 LTS simulator |

### Data Flow

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

See [`docs/Architecture.md`](docs/Architecture.md) for full detail, including the Unity scene hierarchy and MonoBehaviour mapping.

---

## Grade Math

The core formula, implemented in `GradeAssist.Core/GradePlane.cs`:

```
targetY = benchmarkY - targetCutDepthMeters
          + slopeDecimal * alongDistance
          + crossSlopeDecimal * crossDistance

errorMeters = referencePoint.y - targetY
```

| Convention | Meaning |
|---|---|
| Positive `targetCutDepthMeters` | Desired grade is below the benchmark |
| `slopeDecimal` | `slopePercent / 100` |
| `crossSlopeDecimal` | `crossSlopePercent / 100` |
| Positive error | Bucket reference is **above** target grade |
| Negative error | Bucket reference is **below** target grade (overcut) |

See [`docs/GradeMath.md`](docs/GradeMath.md) for full documentation.

---

## Prerequisites

| Tool | Version | Required |
|---|---|---|
| .NET SDK | 8.0+ | ✅ Yes (Core, Tests, Replay) |
| PowerShell | 7.x (`pwsh`) | ✅ Yes (scripts) |
| Unity Hub / Editor | 2022.3 LTS | ⚠️ Optional (UnitySim only) |
| Blender | 3.x+ | ⚠️ Optional (monitor blockout only) |

Verify required tools are on your `PATH`:

```powershell
pwsh .\scripts\verify-tools.ps1
```

---

## Getting Started

```powershell
# 1. Clone the repository
git clone https://github.com/JrSneed28/GradeAssist.git
cd GradeAssist

# 2. Verify tools
pwsh .\scripts\verify-tools.ps1

# 3. Build
pwsh .\scripts\build.ps1

# 4. Run tests
pwsh .\scripts\test.ps1
```

---

## Building

```powershell
pwsh .\scripts\build.ps1
```

This builds `GradeAssist.Core` and `GradeAssist.Tests` in Release configuration. The `GradeAssist.Replay` project is built separately:

```powershell
dotnet build .\src\GradeAssist.Replay\GradeAssist.Replay.csproj --configuration Release
```

---

## Running Tests

```powershell
pwsh .\scripts\test.ps1
```

Runs `dotnet test` on `GradeAssist.Tests` in Release with code coverage collection. Tests cover:

- **`GradePlaneTests`** — flat depth, cut depth, above/below-grade error, slope, cross-slope, zero-direction fallback, tolerance boundaries, large-coordinate precision.
- **`ControllerSafetyTests`** — kill switch, output clamps, rate limiting, watchdog timeout, manual override, state machine transitions.
- **`ConfigParsingTests`** — valid/invalid JSON for all config types; boundary and safety-value rejection.
- **`TelemetryReplayTests`** — CSV parsing, bad-row handling, replay engine error computation, report statistics, Markdown/CSV writers.

### Regenerate Telemetry Fixtures

```powershell
pwsh .\scripts\regen-fixtures.ps1          # regenerate
pwsh .\scripts\regen-fixtures.ps1 -DryRun  # preview without writing
pwsh .\scripts\regen-fixtures.ps1 -Force   # overwrite existing fixtures
```

### Unity Editor Tests

Open the project in Unity 2022.3 LTS and run `GradePlaneMirrorTests` via the Unity Test Runner (Edit Mode). These tests validate that the Unity `UnityGradePlane` MonoBehaviour matches `GradeAssist.Core.GradePlane` to within `0.001 m`.

---

## Configuration

All config files live in `config/`. JSON schemas are provided for editor validation.

| File | Schema | Purpose |
|---|---|---|
| `sample-machine.json` | `schema-machine.json` | Machine definition (IDs, rig paths) |
| `sample-grade-target.json` | `schema-grade-target.json` | Grade plane parameters |
| `MountProfiles.sample.json` | `schema-mount-profiles.json` | Monitor mount positions |
| `RigMaps.sample.json` | `schema-rig-maps.json` | Rig joint-to-bone path mapping |
| `assist-tuning.json` | `schema-assist-tuning.json` | Controller gain and rate limits |
| `safety-policy.json` | `schema-safety-policy.json` | Safety gate thresholds |
| `keybinds.json` | `schema-keybinds.json` | Keyboard bindings |
| `unity-settings.json` | `schema-unity-settings.json` | Unity simulator settings |

Validate config files:

```powershell
pwsh .\scripts\validate-config.ps1
```

### Sample Grade Target

```json
{
  "benchmarkPoint":       { "x": 0.0, "y": 10.0, "z": 0.0 },
  "gradeDirectionXZ":     { "x": 0.0, "y": 0.0,  "z": 1.0 },
  "targetCutDepthMeters": 1.5,
  "slopePercent":         2.0,
  "crossSlopePercent":    0.0,
  "toleranceMeters":      0.03
}
```

---

## Project Structure

```
GradeAssist/
├── src/
│   ├── GradeAssist.Core/          # Grade math, config models, controller stubs
│   │   ├── GradePlane.cs
│   │   ├── GradeError.cs
│   │   ├── GradeStatus.cs
│   │   ├── GradeTargetSettings.cs
│   │   ├── Vector3D.cs
│   │   └── Controllers/           # Simulated assist controller stubs
│   ├── GradeAssist.Tests/         # xUnit regression harness
│   │   └── Program.cs
│   ├── GradeAssist.Replay/        # Offline telemetry replay tool
│   │   ├── TelemetryReplayEngine.cs
│   │   ├── CsvTelemetryReader.cs
│   │   ├── CsvReportWriter.cs
│   │   └── MarkdownReportWriter.cs
│   └── GradeAssist.UnitySim/      # Unity 2022.3 LTS simulator scaffold
│       └── Assets/Scripts/
│           ├── MockExcavatorRig.cs
│           ├── GradeMonitorSimulator.cs
│           └── RenderTextureMonitorBinder.cs
├── config/                        # JSON config files and schemas
├── docs/                          # Architecture and design documentation
├── scripts/                       # PowerShell build, test, and release scripts
├── telemetry/
│   └── samples/                   # Sample telemetry CSV files
├── assets/
│   └── blender/                   # Blender monitor blockout scripts
└── GradeAssist.sln
```

---

## Telemetry Replay

The `GradeAssist.Replay` project replays telemetry CSV files against a grade plane and produces error reports.

Sample telemetry files are in `telemetry/samples/`. Run a replay:

```powershell
pwsh .\scripts\run-telemetry-replay.ps1
```

Output reports include per-row grade error, min/max/mean statistics, overcut counts, and above-grade counts in both Markdown and CSV format.

---

## Unity Simulator

`GradeAssist.UnitySim` is a standalone Unity 2022.3 LTS project — **not** a game mod.

### Setup

1. Open Unity Hub and create a 3D project named `GradeAssist.UnitySim` at `src/GradeAssist.UnitySim/`, or copy the `Assets/` folder into a fresh Unity project.
2. Add the following scripts to GameObjects in your scene:
   - `MockExcavatorRig` → `MockExcavator`
   - `GradeMonitorSimulator` → `Monitor_Canvas`
   - `RenderTextureMonitorBinder` → `Monitor_Screen`

### Controls

| Key | Action |
|---|---|
| `W` / `S` | Move bucket reference forward / back |
| `A` / `D` | Move bucket reference left / right |
| `Q` / `E` | Move bucket reference up / down |

See [`src/GradeAssist.UnitySim/README.md`](src/GradeAssist.UnitySim/README.md) and [`docs/UnitySimulatorDesign.md`](docs/UnitySimulatorDesign.md) for full scene setup instructions.

---

## Incomplete Areas

The following areas are **not yet complete**:

- **`GradeAssist.Replay`** — telemetry replay engine builds but end-to-end replay pipeline and report output are still being validated.
- **`GradeAssist.UnitySim`** — Unity scene, prefabs, and Blender monitor model are scaffolded but not fully assembled; scene setup requires manual steps in Unity Hub.
- **Unity Editor tests** — `GradePlaneMirrorTests.cs` exists in design but the Unity project has not been fully configured to run them in CI.
- **Controller state machine** — `SimulatedGradeAssistController` and related controllers are stubs; active output logic is not implemented.
- **Blender monitor blockout** — the `create_monitor_blockout.py` script generates a basic blockout mesh, but final texturing and materials are not complete.
- **CI pipeline** — scripts exist for build, test, and release validation, but no automated CI workflow file (e.g., GitHub Actions) is configured.
- **Mount profiles / rig maps** — config schemas are defined and sample files exist, but the runtime integration with the Unity simulator is not wired up.

---

## Documentation

| Document | Description |
|---|---|
| [`docs/Architecture.md`](docs/Architecture.md) | System architecture, data flow, scene hierarchy, tech stack |
| [`docs/GradeMath.md`](docs/GradeMath.md) | Grade math formulas and conventions |
| [`docs/TestPlan.md`](docs/TestPlan.md) | Test plan covering unit, replay, and Unity editor tests |
| [`docs/UnitySimulatorDesign.md`](docs/UnitySimulatorDesign.md) | Unity simulator detailed design |
| [`docs/UnityAssetPipeline.md`](docs/UnityAssetPipeline.md) | Unity asset pipeline guide |
| [`docs/BlenderMonitorAssetGuide.md`](docs/BlenderMonitorAssetGuide.md) | Blender monitor model guide |
| [`docs/Architecture.md`](docs/Architecture.md) | RenderTexture monitor binder setup |

---
