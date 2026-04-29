# Section 2: Mock Excavator Rig Design

## 2.1 Transform Hierarchy

The mock excavator rig follows a parented kinematic chain rooted at the machine body. This hierarchy supports both direct manual control and future inverse-kinematic (IK) extension for grade-assist automation.

```
MockExcavator (root)
├── Body (visual chassis)
├── Undercarriage (track base, fixed relative to Body)
│   └── LeftTrack / RightTrack (cylinders)
├── SwingPivot (rotates around Y axis; house swing)
│   ├── Cab (cube, local position 0, 1.5, 0)
│   ├── EngineCompartment (cube, local position -1.2, 1.0, 0)
│   ├── BoomPivot (rotates around X axis; boom raise/lower)
│   │   └── BoomGeometry (scaled cube, length ~5 m)
│   │       └── StickPivot (rotates around X axis; stick curl)
│   │           └── StickGeometry (scaled cube, length ~3 m)
│   │               └── BucketPivot (rotates around X axis; bucket curl)
│   │                   └── BucketGeometry (wedge/cube)
│   │                       └── CuttingEdgeReference (empty, tip of bucket)
```

### Suggested Local Positions (meters)

| Transform | Parent | Local Position | Local Rotation | Role |
|-----------|--------|---------------|----------------|------|
| MockExcavator | — | (0, 0, 0) | (0, 0, 0) | Scene root |
| Body | MockExcavator | (0, 0.5, 0) | (0, 0, 0) | Chassis center |
| SwingPivot | Body | (0, 1.0, 0) | (0, 0, 0) | House rotation point |
| BoomPivot | SwingPivot | (0.8, 0.2, 0) | (0, 20, 0) | Boom base hinge |
| StickPivot | BoomPivot | (0, 5.0, 0) | (0, -20, 0) | Stick base hinge |
| BucketPivot | StickPivot | (0, 3.0, 0) | (0, 0, 0) | Bucket base hinge |
| CuttingEdgeReference | BucketPivot | (0, -0.3, 0.5) | (0, 0, 0) | Grade reference point |

> **Note:** The 20-degree offsets on BoomPivot and StickPivot approximate a realistic resting pose so the rig does not start fully vertical.

### Joint Axes

- **SwingPivot:** Rotates around world Y (yaw). Range -180 to +180 degrees.
- **BoomPivot:** Rotates around local X (pitch). Range -30 to +70 degrees from horizontal.
- **StickPivot:** Rotates around local X (pitch). Range -120 to +30 degrees relative to boom.
- **BucketPivot:** Rotates around local X (pitch). Range -90 to +45 degrees relative to stick.

---

## 2.2 Keyboard Control Mapping

Controls are split into two modes: **Direct** (moves the cutting edge directly) and **Kinematic** (controls individual joints). Direct mode is the default for quick grade testing; kinematic mode is for manual pose shaping.

### Direct Mode (default)

| Key(s) | Action |
|--------|--------|
| W / UpArrow | Move cutting edge +Z (forward) |
| S / DownArrow | Move cutting edge -Z (backward) |
| A / LeftArrow | Move cutting edge -X (left) |
| D / RightArrow | Move cutting edge +X (right) |
| PageUp | Move cutting edge +Y (up) |
| PageDown | Move cutting edge -Y (down) |
| LeftShift (hold) | 0.25x speed modifier (fine control) |
| LeftCtrl (hold) | 4x speed modifier (fast traverse) |

### Kinematic Mode (toggle with Tab)

| Key(s) | Action |
|--------|--------|
| Q / E | Swing left / right (SwingPivot Y rotation) |
| W / S | Boom up / down (BoomPivot X rotation) |
| A / D | Stick in / out (StickPivot X rotation) |
| Z / X | Bucket curl in / out (BucketPivot X rotation) |
| R | Reset all joints to default angles |

### Global Shortcuts

| Key | Action |
|-----|--------|
| Tab | Toggle between Direct and Kinematic mode |
| Escape | Reset entire rig to origin pose |
| 1 / 2 / 3 | Preset camera views (Iso / Top / Side) |

---

## 2.3 Proposed `MockExcavatorRig` MonoBehaviour Redesign

```csharp
using UnityEngine;

public enum RigControlMode
{
    Direct,      // Move cutting edge directly with WASD + PageUp/Down
    Kinematic,   // Control individual joints with dedicated keys
}

public sealed class MockExcavatorRig : MonoBehaviour
{
    // --- Transform References ---
    [Header("Hierarchy")]
    public Transform swingPivot = null!;
    public Transform boomPivot = null!;
    public Transform stickPivot = null!;
    public Transform bucketPivot = null!;
    public Transform cuttingEdgeReference = null!;

    // --- Movement Speeds ---
    [Header("Direct Mode Speeds (m/s or deg/s)")]
    public float moveSpeedMetersPerSecond = 2.0f;
    public float verticalSpeedMetersPerSecond = 1.0f;

    [Header("Kinematic Mode Speeds (deg/s)")]
    public float swingSpeedDegreesPerSecond = 30.0f;
    public float boomSpeedDegreesPerSecond = 20.0f;
    public float stickSpeedDegreesPerSecond = 25.0f;
    public float bucketSpeedDegreesPerSecond = 35.0f;

    // --- Clamps (degrees) ---
    [Header("Joint Limits")]
    public float swingMin = -180f;
    public float swingMax = 180f;
    public float boomMin = -30f;
    public float boomMax = 70f;
    public float stickMin = -120f;
    public float stickMax = 30f;
    public float bucketMin = -90f;
    public float bucketMax = 45f;

    // --- Mode ---
    [Header("Control")]
    public RigControlMode controlMode = RigControlMode.Direct;

    // --- Read-only grade-assist integration ---
    /// <summary>
    /// World-space position of the cutting edge. Updated every frame.
    /// </summary>
    public Vector3 CuttingEdgeWorldPosition =>
        cuttingEdgeReference != null ? cuttingEdgeReference.position : Vector3.zero;

    private void Reset()
    {
        // Attempt to auto-resolve from hierarchy by name
        swingPivot = transform.Find("SwingPivot");
        boomPivot = swingPivot?.Find("BoomPivot");
        stickPivot = boomPivot?.Find("StickPivot");
        bucketPivot = stickPivot?.Find("BucketPivot");
        cuttingEdgeReference = bucketPivot?.Find("CuttingEdgeReference");
    }

    private void Update()
    {
        if (cuttingEdgeReference == null) return;

        HandleModeToggle();

        switch (controlMode)
        {
            case RigControlMode.Direct:
                UpdateDirectMode();
                break;
            case RigControlMode.Kinematic:
                UpdateKinematicMode();
                break;
        }
    }

    private void HandleModeToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            controlMode = controlMode == RigControlMode.Direct
                ? RigControlMode.Kinematic
                : RigControlMode.Direct;
        }
    }

    private float GetSpeedModifier()
    {
        if (Input.GetKey(KeyCode.LeftShift)) return 0.25f;
        if (Input.GetKey(KeyCode.LeftControl)) return 4.0f;
        return 1.0f;
    }

    private void UpdateDirectMode()
    {
        var delta = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    delta.z += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  delta.z -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  delta.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) delta.x += 1;
        if (Input.GetKey(KeyCode.PageUp))    delta.y += 1;
        if (Input.GetKey(KeyCode.PageDown))  delta.y -= 1;

        if (delta.sqrMagnitude > 0)
        {
            var mod = GetSpeedModifier();
            var horiz = new Vector3(delta.x, 0, delta.z).normalized * moveSpeedMetersPerSecond * mod;
            var vert  = Vector3.up * Mathf.Sign(delta.y) * verticalSpeedMetersPerSecond * mod;
            var move  = (horiz + vert) * Time.deltaTime;
            cuttingEdgeReference.position += move;
        }
    }

    private void UpdateKinematicMode()
    {
        var mod = GetSpeedModifier();
        float dt = Time.deltaTime;

        if (swingPivot != null)
        {
            float swingInput = 0;
            if (Input.GetKey(KeyCode.Q)) swingInput -= 1;
            if (Input.GetKey(KeyCode.E)) swingInput += 1;
            RotateLocalY(swingPivot, swingInput * swingSpeedDegreesPerSecond * mod * dt, swingMin, swingMax);
        }

        if (boomPivot != null)
        {
            float boomInput = 0;
            if (Input.GetKey(KeyCode.W)) boomInput += 1;
            if (Input.GetKey(KeyCode.S)) boomInput -= 1;
            RotateLocalX(boomPivot, boomInput * boomSpeedDegreesPerSecond * mod * dt, boomMin, boomMax);
        }

        if (stickPivot != null)
        {
            float stickInput = 0;
            if (Input.GetKey(KeyCode.A)) stickInput += 1;
            if (Input.GetKey(KeyCode.D)) stickInput -= 1;
            RotateLocalX(stickPivot, stickInput * stickSpeedDegreesPerSecond * mod * dt, stickMin, stickMax);
        }

        if (bucketPivot != null)
        {
            float bucketInput = 0;
            if (Input.GetKey(KeyCode.Z)) bucketInput += 1;
            if (Input.GetKey(KeyCode.X)) bucketInput -= 1;
            RotateLocalX(bucketPivot, bucketInput * bucketSpeedDegreesPerSecond * mod * dt, bucketMin, bucketMax);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetJoints();
        }
    }

    private static void RotateLocalX(Transform t, float delta, float min, float max)
    {
        var angles = t.localEulerAngles;
        float pitch = angles.x;
        pitch = (pitch > 180) ? pitch - 360 : pitch; // normalize to [-180,180]
        pitch = Mathf.Clamp(pitch + delta, min, max);
        t.localEulerAngles = new Vector3(pitch, angles.y, angles.z);
    }

    private static void RotateLocalY(Transform t, float delta, float min, float max)
    {
        var angles = t.localEulerAngles;
        float yaw = angles.y;
        yaw = (yaw > 180) ? yaw - 360 : yaw;
        yaw = Mathf.Clamp(yaw + delta, min, max);
        t.localEulerAngles = new Vector3(angles.x, yaw, angles.z);
    }

    private void ResetJoints()
    {
        if (swingPivot)  swingPivot.localEulerAngles  = Vector3.zero;
        if (boomPivot)   boomPivot.localEulerAngles   = new Vector3(20f, 0, 0);
        if (stickPivot)  stickPivot.localEulerAngles  = new Vector3(-20f, 0, 0);
        if (bucketPivot) bucketPivot.localEulerAngles = Vector3.zero;
    }
}
```

### Design Decisions

- **Serialized Transform references** allow the rig to work with any naming convention; `Reset()` provides a best-effort auto-discovery.
- **Separate speed fields** for horizontal and vertical direct motion reflect real excavators (boom/stick act much slower than a root transform slide in test mode).
- **Joint clamping** prevents impossible poses and keeps the cutting edge within a predictable workspace.
- **IK vs. direct toggle:** The mode enum leaves room for a future `InverseKinematic` mode without breaking the inspector layout.
- **Read-only `CuttingEdgeWorldPosition`** is the primary integration surface for the grade-math pipeline.

---

## 2.4 Visual Representation with Unity Primitives

Each segment is built from built-in primitives so the simulator has no external mesh dependencies.

| Segment | Primitive | Material Color | Scale (m) | Notes |
|---------|-----------|---------------|-----------|-------|
| Body | Cube | Safety Yellow (#F4C430) | (2.0, 1.0, 3.5) | Main chassis |
| Cab | Cube | Light Gray | (1.2, 1.2, 1.0) | Operator cabin, slightly transparent |
| EngineCompartment | Cube | Dark Gray | (1.5, 0.8, 1.5) | Counterweight |
| LeftTrack / RightTrack | Cylinder (scaled) | Black | (0.4, 3.5, 0.4) | Rotate 90 deg on X, 8 sides |
| BoomGeometry | Cube | Safety Yellow | (0.35, 5.0, 0.35) | Scaled along Y; pivot at bottom |
| StickGeometry | Cube | Safety Yellow | (0.30, 3.0, 0.30) | Scaled along Y; pivot at top |
| BucketGeometry | Wedge (or cube) | Dark Gray | (0.8, 0.4, 0.5) | Open end faces -Z |
| CuttingEdgeReference | Empty / small sphere | Red | (0.05, 0.05, 0.05) | Visible gizmo for reference |

### Assembly Tips

1. Create an empty GameObject named `MockExcavator` at origin.
2. Parent all visuals under `Body` or `SwingPivot` as shown in the hierarchy.
3. Use **local** transforms for pivots; the geometry should be offset so its pivot point matches the hinge location.
4. For the boom, place `BoomGeometry` at local `(0, 2.5, 0)` so its bottom edge sits at the `BoomPivot` origin.
5. Add a `LineRenderer` on `CuttingEdgeReference` pointing down (-Y) to visualize the grade probe ray.

---

## 2.5 Ground Plane and Scale Reference

### Ground Plane

- Create a quad or plane at **Y = 0**.
- Scale to `(100, 1, 100)` for a 100 x 100 meter workspace.
- Material: semi-transparent grid shader or checkerboard texture at 1-meter intervals.
- If a built-in grid texture is unavailable, use a `LineRenderer` grid generated at runtime:
  - Draw lines every 1 m in X and Z from -50 to +50.
  - Major lines every 5 m in a darker color.

### Scale Reference Markers

| Marker | Position | Visual |
|--------|----------|--------|
| Benchmark origin | (0, 0, 0) | Cylinder (radius 0.1, height 0.02), bright green |
| +10 m X | (10, 0, 0) | Small red sphere |
| +10 m Z | (0, 0, 10) | Small blue sphere |
| Grade direction arrow | (0, 0.1, 0) | Arrow primitive pointing +Z, cyan |

### Camera Setup

| Preset | Position | Rotation | Use |
|--------|----------|----------|-----|
| Iso | (20, 20, 20) | (35, -135, 0) | General overview |
| Top | (0, 30, 0) | (90, 0, 0) | Plan-view grade checking |
| Side | (15, 5, 0) | (0, -90, 0) | Depth/slope verification |

---

## 2.6 Future IK Extension

A third control mode, `InverseKinematic`, can be added later:

1. The user defines a target transform (e.g., dragged by mouse or driven by grade-assist output).
2. The solver computes boom, stick, and bucket angles to place `CuttingEdgeReference` at the target.
3. Because this is a 2D planar arm in the local XZ plane of the swing, a simple analytical solver (two-link IK with wrist adjustment) is sufficient; no CCD or FABRIK is required.

The proposed class layout already reserves the enum value and exposes the required read-only world position, so the IK mode can be added without breaking existing callers.
