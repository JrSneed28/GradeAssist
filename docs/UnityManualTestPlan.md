# Unity Simulator Manual QA Test Plan

## 1. Prerequisites

- Unity 2022.3 LTS or newer
- Project opened at `src/GradeAssist.UnitySim/`
- Play mode available

## 2. Environment Setup

1. Open scene `Assets/Scenes/GradeAssistSimulator.unity`.
2. Verify `MockExcavator` rig is present in Hierarchy.
3. Verify `CabMonitor` is present in Hierarchy.
4. Enter Play mode.

## 3. Test Cases

| # | Steps | Expected Result | Pass Criteria |
|---|-------|-----------------|---------------|
| 1 | Enter Play mode with scene open. | Scene renders without errors. | Zero exceptions in first 5 seconds. |
| 2 | Inspect `MockExcavator` Hierarchy. | Contains `Body > SwingPivot > BoomPivot > StickPivot > BucketPivot > CuttingEdgeReference`. | All transforms exist and nested correctly. |
| 3 | Hold Arrow keys / PageUp / PageDown in Direct mode. | `CuttingEdgeReference` moves in world space. | Smooth movement, no drift on key release. |
| 4 | Press `B` at arbitrary height. | Monitor updates benchmark to current Y. | `benchmarkY` matches bucket Y within 0.001 m. |
| 5 | Position bucket within tolerance (default 0.03 m). | Status reads `ON GRADE`. | Text/banner shows green. |
| 6 | Raise bucket > 0.03 m above target plane. | Status reads `ABOVE GRADE`. | Error positive; indicator red / up. |
| 7 | Lower bucket > 0.03 m below target plane. | Status reads `BELOW GRADE`. | Error negative; indicator blue / down. |
| 8 | Set `slopePercent` to 10%, move 10 m along grade direction. | `targetY` drops by 1.0 m. | Error within 0.001 m of expected. |
| 9 | Set `crossSlopePercent` to 5%, move 20 m perpendicular. | `targetY` drops by 1.0 m. | Error sign and magnitude match formula. |
| 10 | Press `M` or click tab buttons. | Cycles pages: Grade, Settings, Diagnostics. | One page visible at a time; no exceptions. |
| 11 | Select `Monitor_Screen` in Scene view. | Material shows live RenderTexture. | No pink or black screen. |
| 12 | Inspect `RenderTextureMonitorBinder`. | Resolution is 1024 x 600. | Format ARGB32, depth 24. |
| 13 | Search project for `C:\XboxGames`, `Steam`, or game paths. | Zero hits. | No file writes outside `Application.persistentDataPath`. |
| 14 | Load sample CSV from `telemetry/samples/` via `TelemetryReplayDriver`. | Bucket position driven by timestamps. | Final position matches last sample within 0.001 m. |

> **Note on Test 14:** `TelemetryReplayDriver.cs` must be manually attached to a GameObject in the scene. In the Inspector, wire `targetTransform` to `CuttingEdgeReference` and `monitor` to the `GradeMonitorSimulator` on `Monitor_Canvas`. Set `csvFileName` to a sample under `StreamingAssets/TelemetrySamples/`.

## 4. Sign-Off

| Field | Value |
|-------|-------|
| Tester | |
| Date | |
| Unity version | |
| Result | PASS / FAIL |
| Notes | |

---

*Derived from docs/UnitySimulatorDesign.md Section 5.*
