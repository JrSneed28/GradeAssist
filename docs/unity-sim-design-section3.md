# Section 3 — Grade Math Integration and Live Status Display

## 3a. Core-to-Unity Mirroring Strategy

The Unity simulator must produce numerically identical grade results to the Core library. Unity uses `float`, `Vector3`, and `MonoBehaviour`; Core uses `double`, `Vector3D`, and plain C# classes. The mirroring strategy is **structural clone with precision downgrade at boundaries**.

### Mirrored Types

| Core Type | Unity Mirror | Strategy |
|-----------|-------------|----------|
| `Vector3D` | `UnityEngine.Vector3` | Used directly; precision loss from `double` to `float` is acceptable for visual simulation at prototype scale. |
| `GradeTargetSettings` | `GradeTargetSettings` (plain C# class) | Duplicate the record fields and validation rules verbatim. Store as `ScriptableObject` or serializable plain class so it can be edited in Inspector and passed into `UnityGradePlane`. |
| `GradePlane` | `UnityGradePlane` (MonoBehaviour) | Wrap the same math in a `MonoBehaviour` with serializable fields. Does not reference Core assembly directly to keep the Unity project dependency-free. |
| `GradeError` | `GradeError` (readonly struct) | Mirror the struct exactly, using `float` instead of `double`. |
| `GradeStatus` | `GradeStatus` (enum) | Mirror enum values `BelowGrade`, `OnGrade`, `AboveGrade`. |

### Why Not Reference Core Directly?

The Unity simulator is intentionally dependency-free. It may run in contexts where the Core assembly is not available (e.g., standalone player without the full solution). Duplicating the small surface area of grade math (~60 lines) is safer than adding an assembly reference.

### Precision Boundary

All math inside `UnityGradePlane` uses `float`. The benchmark point, grade direction, and world points are `Vector3`. The Core tests use `double` with `Assert.Equal(expected, actual, 3)` (3 decimal places), so `float` precision is well within tolerance for visual simulation.

---

## 3b. `UnityGradePlane` Class Design

`UnityGradePlane` is a `MonoBehaviour` that owns the grade plane state and performs live computation every frame. It is the single source of truth for grade math inside the Unity simulator.

### Serialized Fields

```csharp
public sealed class UnityGradePlane : MonoBehaviour
{
    [Header("Benchmark")]
    public Vector3 benchmarkPoint = Vector3.zero;

    [Header("Grade Direction")]
    public Vector3 gradeDirection = Vector3.forward; // XZ plane direction; Y ignored

    [Header("Target Settings")]
    public float targetCutDepthMeters = 1.5f;        // positive = below benchmark
    public float slopePercent = 0f;                  // main grade slope
    public float crossSlopePercent = 0f;             // perpendicular cross slope
    public float toleranceMeters = 0.03f;            // "on grade" deadband

    // Cached normalized direction (computed in Start / OnValidate)
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

In `Start` and `OnValidate`, compute and cache:

```csharp
var flat = new Vector3(gradeDirection.x, 0, gradeDirection.z);
cachedGradeDirXZ = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
cachedCrossDirXZ = new Vector3(-cachedGradeDirXZ.z, 0, cachedGradeDirXZ.x);
```

The cross-direction is the perpendicular vector in the XZ plane: `(-z, 0, x)` from the normalized grade direction. This matches the Core implementation `new Vector3D(-gradeDir.Z, 0, gradeDir.X)`.

### Inspector Helpers

- `OnDrawGizmosSelected()` draws the benchmark point (sphere), grade direction arrow, and cross-direction arrow in different colors.
- `OnValidate()` clamps `slopePercent` and `crossSlopePercent` to [-500, 500] and rejects non-finite values.

---

## 3c. Live Computation Pipeline

The pipeline runs every frame in the following order:

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
5. **Determine `GradeStatus`** — Compare `error` against `toleranceMeters`:
   - `error > toleranceMeters`  -> `AboveGrade`
   - `error < -toleranceMeters` -> `BelowGrade`
   - otherwise                  -> `OnGrade`

### Update Order

- `MockExcavatorRig` uses `Update()` to apply input-driven movement.
- `UnityGradePlane` uses `LateUpdate()` so it reads the final position after all movement is applied.
- UI reads from `UnityGradePlane` in its own `LateUpdate()` or via UnityEvents fired from `UnityGradePlane`.

---

## 3d. Status Display Mapping with Color-Blind Accessibility

The current `GradeMonitorSimulator` uses only text color and a text label. This fails for users with deuteranopia (red-green color blindness, ~6% of males). The display must combine **shape, motion, and position cues** with color.

### Visual Encoding Table

| GradeStatus | Color | Shape | Motion / Position | Text Label |
|-------------|-------|-------|-------------------|------------|
| `AboveGrade` | Red (#E63946) | Upward arrow (^) | Arrow animates upward; indicator sits above center line | "ABOVE GRADE" |
| `OnGrade` | Green (#1DB954) | Circle / bullseye | Static, centered | "ON GRADE" |
| `BelowGrade` | Blue (#457B9D) | Downward arrow (v) | Arrow animates downward; indicator sits below center line | "BELOW GRADE" |

### UI Layout — Grade2D Panel

```
+---------------------------+
|  GRADE 2D                 |
|                           |
|  [Animated Arrow Icon]      |
|  +0.052 m                 |
|  ABOVE GRADE              |
|                           |
|  Cut: 1.500 m             |
|  Slope: 0.00%             |
|  X-Slope: 0.00%           |
+---------------------------+
```

### Color-Blind Safe Palette

Chosen from a palette that remains distinguishable under protanopia, deuteranopia, and tritanopia simulations:

- Red: `#E63946` — vivid, warm red
- Green: `#1DB954` — bright green, better luminance separation from red for deuteranopia
- Blue: `#457B9D` — muted steel blue

These three colors are separated by > 40 in CIEDE2000 delta-E and have distinct brightness values (red = 0.55, green = 0.60, blue = 0.42 in perceived luminance), so they remain distinguishable in grayscale. Never rely on color alone; always pair with shape and text prefix.

### Implementation Notes

- Use a single `Image` for the status icon and swap its `Sprite` and `Color` based on `GradeStatus`.
- Arrow sprites should have distinct silhouettes (not just rotated versions of the same sprite, to avoid ambiguity).
- Add a subtle vertical bob animation to the arrow: `AboveGrade` bobs up, `BelowGrade` bobs down, `OnGrade` is still.
- The numeric error display always shows sign: `+0.052` or `-0.127` or `0.000`.

---

## 3e. Data Flow Diagram Description

```
+------------------------+        +-----------------------+
| MockExcavatorRig       |        | UnityGradePlane       |
| - cuttingEdgeReference   |------>| - benchmarkPoint      |
| - moveSpeedMetersPerSec  |        | - gradeDirection      |
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

### Component Responsibilities

| Component | Responsibility |
|-----------|--------------|
| `MockExcavatorRig` | Simulates excavator bucket movement via keyboard. Owns the `Transform` that represents the cutting edge. |
| `UnityGradePlane` | Single source of truth for grade math. Computes `GradeError` from rig position. Fires `OnGradeErrorChanged(GradeError)` event. |
| `MonitorPageManager` | Subscribes to `UnityGradePlane.OnGradeErrorChanged`. Formats strings (depth, slope, error, status). Updates `Grade2D` UI elements. |
| `Grade2D` UI | Visual panel with status icon, error magnitude, and labels. Receives data via `MonitorPageManager`; contains no logic. |
| `RenderTextureMonitorBinder` | Renders the UI camera to a `RenderTexture` and applies it to the in-cab monitor screen mesh. Already implemented. |

### Event Contract

```csharp
public readonly struct GradeError
{
    public readonly Vector3 ReferencePoint;
    public readonly float TargetY;
    public readonly float ErrorMeters;
    public readonly float ToleranceMeters;

    public GradeStatus Status => ...
}

// Fired by UnityGradePlane whenever error or status changes
public event Action<GradeError> OnGradeErrorChanged;
```

`MonitorPageManager` registers in `OnEnable`, unregisters in `OnDisable`. This decouples the math engine from the UI and allows multiple listeners (e.g., a debug logger or telemetry recorder) without modifying `UnityGradePlane`.

---

## 3f. Validation Strategy — Unity Math Matches Core Math Exactly

Because `UnityGradePlane` is a hand-written mirror, there is a real risk of drift from the Core library (e.g., the current `GradeMonitorSimulator` omits cross-slope entirely). The validation strategy has three layers.

### Layer 1 — Unit Tests in Core (Already Exists)

The `GradeAssist.Tests` project verifies Core math with deterministic inputs. Key test cases that Unity must replicate:

| Benchmark | GradeDir | CutDepth | Slope% | CrossSlope% | TestPoint | Expected targetY | Expected Status |
|-----------|----------|----------|--------|-------------|-----------|------------------|-----------------|
| (0,0,0) | (1,0,0) | 1.0 | 10 | 0 | (2, -0.5, 0) | -0.8 | OnGrade (tol=0.1) |
| (0,0,0) | (1,0,0) | 1.0 | 0 | 5 | (0, -1.0, 2) | -0.9 | OnGrade (tol=0.1) |
| (0,0,0) | (1,0,0) | 1.0 | 10 | 5 | (2, -0.5, 2) | -0.6 | OnGrade (tol=0.1) |

> Note: The current `GradeMonitorSimulator` would fail the second and third rows because it does not compute cross-slope.

### Layer 2 — Play-Mode Unit Tests in Unity

Add a Unity Test Framework play-mode test that:

1. Creates a `GameObject` with `UnityGradePlane`.
2. Sets the same inputs as a Core test case.
3. Calls `HeightAt()` and `ComputeError()`.
4. Asserts that `targetY` matches the Core expected value within `0.001f` (float tolerance).
5. Asserts that `GradeStatus` matches exactly.

Test path: `src/GradeAssist.UnitySim/Assets/Tests/Runtime/GradePlaneMirrorTests.cs`

```csharp
[Test]
public void HeightAt_MatchesCore_WithCrossSlope()
{
    var go = new GameObject("TestPlane");
    var plane = go.AddComponent<UnityGradePlane>();
    plane.benchmarkPoint = new Vector3(0, 0, 0);
    plane.gradeDirection = new Vector3(1, 0, 0);
    plane.targetCutDepthMeters = 1.0f;
    plane.slopePercent = 10f;
    plane.crossSlopePercent = 5f;

    var targetY = plane.HeightAt(new Vector3(2, 0, 2));
    Assert.AreEqual(-0.6f, targetY, 0.001f);
}
```

### Layer 3 — Runtime Cross-Check Script

Add a debug-only `GradeMathValidator` MonoBehaviour that can be attached during play-mode testing. It:

1. Samples `UnityGradePlane.HeightAt()` for 100 random points.
2. Runs the same calculation with a verbatim copy of the Core `GradePlane` code (using `double` internally).
3. Logs any delta > `1e-4` as a warning.
4. Provides a "Validate Grade Math" button in the Inspector that runs the check on demand.

This is a safety net for manual QA and regression testing when the grade formula is modified.

### Regression Guard

When the Core `GradePlane` formula changes (e.g., adding a new term), the change must be:

1. Implemented in Core with tests.
2. Ported to `UnityGradePlane`.
3. Verified by the play-mode test suite.
4. Documented in the change log with a note that Unity mirror was updated.

Because the formula is small (two dot products and three arithmetic terms), porting is a trivial copy-paste with `float` instead of `double`.

---

## Summary of Changes Required

1. **Create** `UnityGradePlane.cs` — MonoBehaviour with full grade formula including cross-slope.
2. **Create** `GradeError.cs` (Unity) — `readonly struct` mirroring Core.
3. **Create** `GradeStatus.cs` (Unity) — enum mirroring Core.
4. **Create** `GradeTargetSettings.cs` (Unity) — serializable settings class.
5. **Refactor** `GradeMonitorSimulator.cs` — replace inline math with `UnityGradePlane` reference; add `MonitorPageManager` event wiring.
6. **Create** `MonitorPageManager.cs` — listens to `UnityGradePlane` and drives `Grade2D` UI.
7. **Create** `GradePlaneMirrorTests.cs` — Unity play-mode tests verifying numerical parity with Core.
8. **Create** `GradeMathValidator.cs` (optional) — runtime cross-check between Unity and Core math for manual QA.
