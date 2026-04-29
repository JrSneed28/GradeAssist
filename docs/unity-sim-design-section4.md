# Unity Simulator Design — Section 4: Verification, Risks & Fallback Plan

## 1. Manual Test Checklist

The following 14 checks must be performed manually in the Unity Editor Play mode before any milestone 3 gate is signed off.

| # | Test | Steps | Expected Result | Pass Criteria |
|---|------|-------|-----------------|---------------|
| 1 | **Scene load** | Open `Assets/Scenes/CabMonitor.unity`. Enter Play mode. | Scene renders without console errors (red or yellow). | Zero exceptions in the first 5 seconds. |
| 2 | **Rig hierarchy** | Inspect `MockExcavator` in the Hierarchy. | Contains `Boom > Stick > Bucket > CuttingEdgeReference` in that parent-child order. | All four transforms exist and are nested correctly. |
| 3 | **Keyboard bucket move** | Hold Arrow keys and PageUp/PageDown. | `CuttingEdgeReference` moves in world space: X = Left/Right, Z = Up/Down arrows, Y = PageUp/PageDown. | Movement is smooth at ~1 m/s. No drift when keys are released. |
| 4 | **Benchmark set** | Press `B` while bucket is at an arbitrary height. | Monitor text updates `benchmarkY` to the bucket’s current Y. | `GradeMonitorSimulator.benchmarkY` matches the bucket Y within 0.001 m. |
| 5 | **Grade display — ON GRADE** | Position bucket so error is within `toleranceMeters` (default 0.03 m). | Monitor reads `Status: ON GRADE`. | Grade error text is green or neutral. |
| 6 | **Grade display — ABOVE GRADE** | Raise bucket above target plane by > 0.03 m. | Monitor reads `Status: ABOVE GRADE`. | Grade error text shows positive value. Indicator points up / amber. |
| 7 | **Grade display — BELOW GRADE** | Lower bucket below target plane by > 0.03 m. | Monitor reads `Status: BELOW GRADE`. | Grade error text shows negative value. Indicator points down / red. |
| 8 | **Slope math** | Set `slopePercent` to 10 %, move bucket 10 m along grade direction. | `targetY` drops by 1.0 m relative to benchmark. | Computed error is within 0.001 m of expected. |
| 9 | **Cross-slope math** | Set `crossSlopeDecimal` to 0.05, move bucket 20 m perpendicular to grade direction. | `targetY` drops by 1.0 m. | Error sign and magnitude match formula. |
| 10 | **Monitor pages toggle** | Press `F1` through `F4` on the keyboard (or UI buttons). | Canvas switches between Grade, Diagnostics, Settings, and Replay pages. | Only one page is visible at a time. No exceptions. |
| 11 | **RenderTexture binding** | Select `Monitor_ScreenMesh` in Scene view. | Material shows live RenderTexture output from `UICamera`. | Screen texture is not pink (missing) or black (unbound). |
| 12 | **RenderTexture resolution** | Inspect `RenderTextureMonitorBinder` in Inspector. | `width` and `height` are 1024 x 600 (or project-configured). | RT format is ARGB32, depth 24. |
| 13 | **Safety gate — no game writes** | Confirm no script references `C:\XboxGames`, `Steam`, or game install paths. | Search Project yields zero hits. | Zero file-write calls outside `Application.persistentDataPath`. |
| 14 | **Telemetry replay driver** | Load a sample telemetry CSV from `telemetry/samples/` into `MockExcavatorRig` replay driver. Enter Play mode. | Bucket position is driven by timestamp-ordered samples. | Final position matches last sample within 0.001 m. Report generator produces deterministic output. |

---

## 2. Unity Version Assumptions

### Minimum LTS Version
- **Unity 2022.3 LTS** (or newer stable LTS).
- Rationale: `RenderTexture` binding via script is stable in 2022.3; IL2CPP and Mono backends are both supported; long-term patch support reduces toolchain churn.

### Render Pipeline Compatibility
- **Built-in Render Pipeline** (default).
- Rationale: The `RenderTextureMonitorBinder` shader-property checks (`_MainTex`, `_BaseMap`, `_EmissionMap`) are written for Built-in Standard shaders. URP/HDRP would require material/shader upgrades.
- If the team later switches to URP, the binder must be updated to use URP Lit material keywords.

### Scripting Backend
- **Mono** (Editor / rapid iteration).
- **IL2CPP** is acceptable for final standalone builds but not required for the prototype.
- Core source is plain C# 11 / .NET Standard 2.1 compatible; no Unity-specific `#if` guards are needed.

### Platform
- **Standalone Windows x86_64**.
- Input: keyboard only (`Input.GetKey`). No gamepad or joystick dependencies.

### Known Compatibility Notes
| Feature | Minimum Version | Note |
|---------|----------------|------|
| `RenderTexture` constructor | 2019.4+ | Used in `RenderTextureMonitorBinder`. |
| `Camera.targetTexture` | 5.0+ | Standard API; no version risk. |
| `Material.HasProperty` | 5.0+ | Used for shader-agnostic texture binding. |
| TextMeshPro / uGUI | 2018.4+ | If TMP is used for monitor text; otherwise standard `UnityEngine.UI.Text` works in all versions. |

---

## 3. Risks and Fallback Plan

### R-U-001 — Core Math Drift

| Field | Value |
|-------|-------|
| **Description** | Grade math implemented in `GradeAssist.Core` may diverge from the Unity script implementation due to float-vs-double precision differences or formula transcription errors. |
| **Severity** | High (silent wrong-grade errors). |
| **Likelihood** | Likely (two code paths: Core DLL and Unity source mirror). |
| **Fallback** | 1. Single-source-of-truth: Unity references Core `.cs` files directly (not a re-implementation). 2. If direct reference is impossible, generate Core DLL and place it in `Assets/Plugins/`. 3. Add a deterministic replay test that runs the same telemetry through both Core unit tests and the Unity Play-mode test runner; compare outputs bit-for-bit. |

### R-U-002 — RenderTexture Unsupported Hardware

| Field | Value |
|-------|-------|
| **Description** | Target development or demo machine may lack GPU RT support, or integrated graphics drivers may fail to create a 1024x600 RenderTexture. |
| **Severity** | Medium (monitor screen stays black). |
| **Likelihood** | Unlikely (most modern GPUs support RTs). |
| **Fallback** | 1. Detect failure in `RenderTextureMonitorBinder.Start()`; if `renderTexture == null` or `!renderTexture.IsCreated()`, fall back to a standard `RawImage` on a world-space Canvas placed directly on the monitor mesh. 2. Lower resolution to 512x300. 3. Provide an editor-only toggle on the binder component: `UseFallbackRawImage`. |

### R-U-003 — Project Bloat from Binary Assets

| Field | Value |
|-------|-------|
| **Description** | Unity `.unitypackage` imports, large textures, or accidental `.fbx` imports can bloat the repo, slowing clone times and bloating the release ZIP. |
| **Severity** | Medium (CI and release friction). |
| **Likelihood** | Likely (placeholder geometry can still carry large textures). |
| **Fallback** | 1. `.gitignore` already excludes `Library/`, `Temp/`, `Obj/`, and `*.unitypackage`. 2. Enforce a 2 MB per-file limit in `scripts/check-safety-gates.ps1` (text scan for files > 2 MB). 3. Use ProBuilder or Unity primitives for all placeholder geometry; no external high-res models. 4. If a large asset is needed, store it in `assets/` (outside Unity project) and document it as optional. |

### R-U-004 — Input System Differences

| Field | Value |
|-------|-------|
| **Description** | Unity’s new Input System (package) conflicts with the legacy `Input.GetKey` API used in `MockExcavatorRig`. If the project is upgraded to the new Input System, legacy calls will fail at runtime. |
| **Severity** | Medium (controls stop working). |
| **Likelihood** | Unlikely (project is scaffold-level and unlikely to upgrade mid-prototype). |
| **Fallback** | 1. Keep using legacy `Input` class; do not enable the new Input System package in Project Settings. 2. If new Input System is required, create a thin `IInputSource` abstraction with two implementations: `LegacyInputSource` and `NewInputSource`. Default to legacy. |

### R-U-005 — Assembly Reference Failure

| Field | Value |
|-------|-------|
| **Description** | `GradeAssist.Core` is a `net8.0` class library. Unity may fail to reference it if assembly definitions, target framework mismatches, or namespace conflicts occur. |
| **Severity** | High (scripts will not compile). |
| **Likelihood** | Likely (dual-path build is already a known risk in R-002 of the Risk Register). |
| **Fallback** | 1. **Preferred:** Reference Core source directly via Unity Assembly Definition (`GradeAssist.Core.asmdef`) pointing to the shared `.cs` files. 2. **Fallback A:** Compile Core as a .NET Standard 2.1 DLL and place it in `Assets/Plugins/GradeAssist.Core.dll` with its `.xml`/`.pdb`. 3. **Fallback B:** If both fail, copy Core `.cs` files into `Assets/Scripts/GradeAssist/Core/` as a last resort and document the duplication in `docs/UnityWiring.md`. |

### R-U-006 — Scene Object Wiring Errors

| Field | Value |
|-------|-------|
| **Description** | `GradeMonitorSimulator` and `RenderTextureMonitorBinder` expose public fields (`rig`, `gradeText`, `uiCamera`, `screenRenderer`) that must be wired in the Inspector. Missing references cause null-reference exceptions in Play mode. |
| **Severity** | Medium (runtime crash on scene start). |
| **Likelihood** | Likely (manual wiring is error-prone). |
| **Fallback** | 1. Add `[SerializeField]` inspector tooltips for every exposed reference. 2. Implement `OnValidate()` in each script to log a warning if a required field is null. 3. Provide a `SetupWizard` editor script (optional) that auto-finds components by name/type and assigns them. 4. Document the exact Hierarchy naming convention in `docs/UnityWiring.md`. |

---

## 4. Exact File Tree to Create

The implementer must produce the following folders and files under `src/GradeAssist.UnitySim/`.

```text
src/GradeAssist.UnitySim/
├── README.md                              (exists — update with wiring instructions)
├── .gitignore                             (exclude Library/, Temp/, Obj/, logs/)
├── Assets/
│   ├── Scenes/
│   │   └── CabMonitor.unity               (main simulator scene)
│   ├── Scripts/
│   │   ├── GradeAssist.Core.asmdef          (Assembly Definition referencing Core source)
│   │   ├── CoreSourceLink.meta              (optional meta for folder link)
│   │   ├── MockExcavatorRig.cs              (exists — bucket keyboard movement)
│   │   ├── GradeMonitorSimulator.cs         (exists — grade math + UI text)
│   │   ├── RenderTextureMonitorBinder.cs    (exists — RT binding + material setup)
│   │   ├── TelemetryReplayDriver.cs         (reads CSV/JSON, drives bucket position)
│   │   ├── MonitorPageController.cs         (F1-F4 page switching)
│   │   ├── GradeStatusIndicator.cs          (color + arrow for ON/ABOVE/BELOW)
│   │   └── Editor/
│   │       └── SetupWizard.cs               (auto-wires public fields on scene objects)
│   ├── Materials/
│   │   ├── MonitorScreen.mat                (Standard shader with _MainTex / _EmissionMap)
│   │   ├── MonitorBezel.mat                 (dark grey generic plastic)
│   │   └── MonitorBody.mat                  (generic grey)
│   ├── Prefabs/
│   │   ├── MockExcavator.prefab             (root with Boom/Stick/Bucket/CuttingEdgeReference)
│   │   ├── CabMonitor.prefab                (Monitor_Body + Bezel + ScreenMesh + Buttons)
│   │   └── UIRoot.prefab                    (World-space Canvas + MonitorCamera)
│   ├── Resources/
│   │   └── GradeAssistConfig.json           (default benchmark, slope, tolerance values)
│   └── StreamingAssets/
│       └── TelemetrySamples/                (symlink or copy of telemetry/samples/)
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

- `GradeAssist.Core.asmdef` is the recommended integration path. It should include the shared Core `.cs` files (grade plane, error calculation, settings) and set `autoReferenced: true` so Unity scripts can use it without manual assembly references.
- `TelemetryReplayDriver.cs` must read from `StreamingAssets/TelemetrySamples/` at runtime. Files in `StreamingAssets` are copied verbatim to the build output.
- `SetupWizard.cs` lives in `Editor/` and is stripped from player builds. It should be a simple `EditorWindow` or `MonoBehaviour` context-menu item that assigns `MockExcavatorRig`, `GradeMonitorSimulator`, and `RenderTextureMonitorBinder` references based on `GameObject.Find` or `GetComponentInChildren`.
- `MonitorPageController.cs` implements the F1-F4 page toggle. It should expose four `GameObject` fields (one per page Canvas) and enable only the active page.
- `GradeStatusIndicator.cs` updates a UI Image color and rotation based on the current grade status string. It should subscribe to `GradeMonitorSimulator.OnStatusChanged` (event) or poll every frame.
- All `.mat` files use the Built-in Standard shader with generic colors only (no proprietary textures or logos).
