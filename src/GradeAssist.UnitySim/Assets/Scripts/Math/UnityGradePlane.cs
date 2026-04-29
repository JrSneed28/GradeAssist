using UnityEngine;

/// <summary>
/// MonoBehaviour that owns the grade plane state and performs live computation.
/// This is the single source of truth for grade math inside the Unity simulator.
/// Mirrors GradeAssist.Core.GradePlane with float precision.
/// </summary>
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

    public event System.Action<GradeError>? OnGradeErrorChanged;

    private GradeError? lastError;

    private void Start()
    {
        CacheDirections();
    }

    private void OnValidate()
    {
        if (float.IsNaN(targetCutDepthMeters) || float.IsInfinity(targetCutDepthMeters))
        {
            Debug.LogWarning("[UnityGradePlane] targetCutDepthMeters must be finite.");
            targetCutDepthMeters = 1.5f;
        }
        if (float.IsNaN(slopePercent) || float.IsInfinity(slopePercent))
        {
            Debug.LogWarning("[UnityGradePlane] slopePercent must be finite.");
            slopePercent = 0f;
        }
        if (float.IsNaN(crossSlopePercent) || float.IsInfinity(crossSlopePercent))
        {
            Debug.LogWarning("[UnityGradePlane] crossSlopePercent must be finite.");
            crossSlopePercent = 0f;
        }
        if (float.IsNaN(toleranceMeters) || float.IsInfinity(toleranceMeters))
        {
            Debug.LogWarning("[UnityGradePlane] toleranceMeters must be finite.");
            toleranceMeters = 0.03f;
        }
        slopePercent = Mathf.Clamp(slopePercent, -500f, 500f);
        crossSlopePercent = Mathf.Clamp(crossSlopePercent, -500f, 500f);
        if (targetCutDepthMeters < 0) targetCutDepthMeters = 0;
        if (toleranceMeters < 0.001f) toleranceMeters = 0.001f;
        CacheDirections();
    }

    private void CacheDirections()
    {
        var flat = new Vector3(gradeDirection.x, 0, gradeDirection.z);
        cachedGradeDirXZ = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        cachedCrossDirXZ = new Vector3(-cachedGradeDirXZ.z, 0, cachedGradeDirXZ.x);
    }

    /// <summary>
    /// Computes the target Y height at a given world point.
    /// </summary>
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

    /// <summary>
    /// Computes the grade error at a reference point.
    /// </summary>
    public GradeError ComputeError(Vector3 referencePoint)
    {
        var targetY = HeightAt(referencePoint);
        var error = referencePoint.y - targetY;
        return new GradeError(referencePoint, targetY, error, toleranceMeters);
    }

    /// <summary>
    /// Updates the grade plane and fires OnGradeErrorChanged if the error changed.
    /// Call from LateUpdate so it reads the final position after all movement.
    /// </summary>
    public void Evaluate(Vector3 referencePoint)
    {
        var error = ComputeError(referencePoint);
        if (!lastError.HasValue || !lastError.Value.Equals(error))
        {
            lastError = error;
            OnGradeErrorChanged?.Invoke(error);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(benchmarkPoint, 0.1f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(benchmarkPoint, benchmarkPoint + cachedGradeDirXZ * 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(benchmarkPoint, benchmarkPoint + cachedCrossDirXZ * 2f);
    }
}

/// <summary>
/// Immutable grade error result.
/// </summary>
public readonly struct GradeError
{
    public readonly Vector3 ReferencePoint;
    public readonly float TargetY;
    public readonly float ErrorMeters;
    public readonly float ToleranceMeters;

    public GradeStatus Status =>
        Mathf.Abs(ErrorMeters) <= ToleranceMeters ? GradeStatus.OnGrade :
        ErrorMeters > 0 ? GradeStatus.AboveGrade :
        GradeStatus.BelowGrade;

    public GradeError(Vector3 referencePoint, float targetY, float errorMeters, float toleranceMeters)
    {
        ReferencePoint = referencePoint;
        TargetY = targetY;
        ErrorMeters = errorMeters;
        ToleranceMeters = toleranceMeters;
    }

    public bool Equals(GradeError other) =>
        ReferencePoint == other.ReferencePoint &&
        Mathf.Approximately(TargetY, other.TargetY) &&
        Mathf.Approximately(ErrorMeters, other.ErrorMeters) &&
        Mathf.Approximately(ToleranceMeters, other.ToleranceMeters);
}
