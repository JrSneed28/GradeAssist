# Test Plan

## Testing philosophy

- Dependency-free, fast regression gates.
- No mocked math: core grade calculations are exercised directly.
- Test files must exist in the source tree; do not reference tests that have not been written.

## Unit tests

Run:

```powershell
pwsh .\scripts\test.ps1
```

Project: `src/GradeAssist.Tests/GradeAssist.Tests.csproj`

Files:

- `GradePlaneTests.cs` — flat benchmark depth, positive cut depth, above-grade / below-grade error, slope percent and direction, cross-slope perpendicular distance, zero-direction fallback, tolerance boundaries, large-coordinate precision, validation rejection of NaN/infinity/negative/out-of-range values.
- `ControllerSafetyTests.cs` — kill switch (emergency disable, reset), output clamps, rate-limiting, watchdog timeout / stale telemetry fault, manual override, state machine (Locked→Ready→Armed→Active), exception handling, and per-controller locked-by-default.
- `ConfigParsingTests.cs` — valid and invalid JSON parsing for MachineConfig, GradeTarget, SafetyPolicy, AssistTuning, Keybinds, UnitySettings, MountProfiles, and RigMaps; boundary checks and rejection of unsafe values.

## Telemetry replay tests

`TelemetryReplayTests.cs` covers:

- CSV parsing with optional fields.
- Bad-row handling (NaN, Infinity, malformed time) with recorded messages.
- Replay engine error computation against a grade plane.
- Report statistics (min/max/mean error, overcut, above-grade counts).
- Markdown and CSV report writers.
- Deterministic fixture generation via `scripts/regen-fixtures.ps1` from sample CSVs in `telemetry/samples/`.
- Note: Tests compute expected values inline; fixture JSON files are generated but not currently consumed by any test.

Fixture regeneration:

```powershell
pwsh .\scripts\regen-fixtures.ps1
```

Options:
- `-DryRun` — preview what would be written without creating files.
- `-Force` — overwrite existing fixtures.

The script reads sample telemetry from `telemetry/samples/` and writes expected JSON outputs to `telemetry/fixtures/expected/`. It operates only inside the repo and never writes to any game folder.

## Unity editor tests

File: `src/GradeAssist.UnitySim/Assets/Tests/Editor/GradePlaneMirrorTests.cs`

Validates `UnityGradePlane` (MonoBehaviour, `Vector3`/`float`) against `GradeAssist.Core.GradePlane` (`Vector3D`/`float`) using identical inputs. Tests are run via the Unity Test Runner (Edit Mode).

Coverage:

- Flat benchmark depth zero.
- Positive cut depth below benchmark.
- Above-grade positive error and below-grade negative error.
- Slope percent conversion and grade direction along-distance.
- Cross-slope perpendicular distance.
- Zero-direction fallback.
- Positive and negative tolerance boundaries.
- Large-coordinate precision.

Results must agree within `0.001 m` and `GradeStatus` must match exactly.

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/test.ps1` | Runs `dotnet test` on `GradeAssist.Tests.csproj` in Release with code coverage collection. |
| `scripts/regen-fixtures.ps1` | Regenerates deterministic telemetry replay fixture expected-output files from `telemetry/samples/`. |
| `scripts/verify-tools.ps1` | Checks that `git`, `dotnet`, and `pwsh` are on PATH. Optional tools (`code`, `blender`, `ffmpeg`) are reported but not required. |

## CI gate

- Build must pass (`pwsh .\scripts\build.ps1`) before any test run.
- All tests must pass before a local release (`pwsh .\scripts\final-verify.ps1`).
