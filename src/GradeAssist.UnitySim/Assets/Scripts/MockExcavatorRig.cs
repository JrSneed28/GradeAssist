using UnityEngine;

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
    public float bucketSpeedDegreesPerSecond = 25.0f;

    [Header("Kinematic Limits")]
    public float minSwingAngle = -180f;
    public float maxSwingAngle = 180f;
    public float minBoomAngle = -10f;
    public float maxBoomAngle = 70f;
    public float minStickAngle = -130f;
    public float maxStickAngle = 40f;
    public float minBucketAngle = -90f;
    public float maxBucketAngle = 45f;

    public Vector3 CuttingEdgeWorldPosition => cuttingEdgeReference != null ? cuttingEdgeReference.position : transform.position;

    public enum ControlMode { Direct, Kinematic }
    public ControlMode controlMode = ControlMode.Direct;

    private Vector3 directInitialPosition;
    private float swingAngle, boomAngle, stickAngle, bucketAngle;

    private void Reset()
    {
        cuttingEdgeReference = transform;
    }

    private void Start()
    {
        if (cuttingEdgeReference == null) cuttingEdgeReference = transform;
        directInitialPosition = cuttingEdgeReference.position;
    }

    private void Update()
    {
        // Toggle mode
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            controlMode = controlMode == ControlMode.Direct ? ControlMode.Kinematic : ControlMode.Direct;
            Debug.Log($"[MockExcavatorRig] Switched to {controlMode}");
        }

        // Camera presets
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCameraPreset(new Vector3(10, 10, 10), new Vector3(30, -45, 0));
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCameraPreset(new Vector3(0, 20, 0), new Vector3(90, 0, 0));
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetCameraPreset(new Vector3(15, 5, 0), new Vector3(0, -90, 0));

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            swingAngle = 0f;
            boomAngle = 0f;
            stickAngle = 0f;
            bucketAngle = 0f;
            if (swingPivot != null) swingPivot.localRotation = Quaternion.identity;
            if (boomPivot != null) boomPivot.localRotation = Quaternion.identity;
            if (stickPivot != null) stickPivot.localRotation = Quaternion.identity;
            if (bucketPivot != null) bucketPivot.localRotation = Quaternion.identity;
            Debug.Log("[MockExcavatorRig] Reset to origin pose");
        }

        if (controlMode == ControlMode.Direct)
            UpdateDirectMode();
        else
            UpdateKinematicMode();
    }

    private void UpdateDirectMode()
    {
        if (cuttingEdgeReference == null) return;

        var delta = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) delta.z += 1;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) delta.z -= 1;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) delta.x -= 1;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) delta.x += 1;
        if (Input.GetKey(KeyCode.PageUp)) delta.y += 1;
        if (Input.GetKey(KeyCode.PageDown)) delta.y -= 1;

        if (delta.sqrMagnitude > 0)
        {
            float speedMultiplier = 1.0f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speedMultiplier = 0.25f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                speedMultiplier = 4.0f;

            float horizontalSpeed = moveSpeedMetersPerSecond * speedMultiplier;
            float verticalSpeed = verticalSpeedMetersPerSecond * speedMultiplier;

            var move = delta.normalized;
            cuttingEdgeReference.position += new Vector3(move.x * horizontalSpeed, move.y * verticalSpeed, move.z * horizontalSpeed) * Time.deltaTime;
        }
    }

    private void UpdateKinematicMode()
    {
        float dt = Time.deltaTime;

        if (Input.GetKey(KeyCode.Q)) swingAngle -= swingSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.E)) swingAngle += swingSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.W)) boomAngle += boomSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.S)) boomAngle -= boomSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.A)) stickAngle -= stickSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.D)) stickAngle += stickSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.Z)) bucketAngle -= bucketSpeedDegreesPerSecond * dt;
        if (Input.GetKey(KeyCode.C)) bucketAngle += bucketSpeedDegreesPerSecond * dt;

        if (Input.GetKeyDown(KeyCode.R))
        {
            swingAngle = 0f;
            boomAngle = 0f;
            stickAngle = 0f;
            bucketAngle = 0f;
            Debug.Log("[MockExcavatorRig] Reset joints to default");
        }

        swingAngle = Mathf.Clamp(swingAngle, minSwingAngle, maxSwingAngle);
        boomAngle = Mathf.Clamp(boomAngle, minBoomAngle, maxBoomAngle);
        stickAngle = Mathf.Clamp(stickAngle, minStickAngle, maxStickAngle);
        bucketAngle = Mathf.Clamp(bucketAngle, minBucketAngle, maxBucketAngle);

        if (swingPivot != null) swingPivot.localRotation = Quaternion.Euler(0, swingAngle, 0);
        if (boomPivot != null) boomPivot.localRotation = Quaternion.Euler(boomAngle, 0, 0);
        if (stickPivot != null) stickPivot.localRotation = Quaternion.Euler(stickAngle, 0, 0);
        if (bucketPivot != null) bucketPivot.localRotation = Quaternion.Euler(bucketAngle, 0, 0);
    }

    private void SetCameraPreset(Vector3 position, Vector3 eulerAngles)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = position;
        cam.transform.rotation = Quaternion.Euler(eulerAngles);
    }
}
