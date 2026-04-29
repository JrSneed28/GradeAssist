using GradeAssist.Core;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Editor tests that validate UnityGradePlane (MonoBehaviour, Vector3/float)
/// produces numerically identical results to GradeAssist.Core.GradePlane
/// (Vector3D/float) for the same inputs.
/// </summary>
public class GradePlaneMirrorTests
{
    private const float Tolerance = 0.001f;

    private static UnityGradePlane CreateUnityPlane(
        Vector3 benchmark = default,
        Vector3 dir = default,
        float depth = 0f,
        float slope = 0f,
        float crossSlope = 0f,
        float tolerance = 0.03f)
    {
        if (benchmark == default) benchmark = new Vector3(0, 10, 0);
        if (dir == default) dir = Vector3.forward;

        var go = new GameObject("TestPlane");
        var plane = go.AddComponent<UnityGradePlane>();
        plane.benchmarkPoint = benchmark;
        plane.gradeDirection = dir;
        plane.targetCutDepthMeters = depth;
        plane.slopePercent = slope;
        plane.crossSlopePercent = crossSlope;
        plane.toleranceMeters = tolerance;
        return plane;
    }

    private static GradeAssist.Core.GradePlane CreateCorePlane(
        Vector3D benchmark = default,
        Vector3D dir = default,
        float depth = 0f,
        float slope = 0f,
        float crossSlope = 0f,
        float tolerance = 0.03f)
    {
        if (benchmark.Equals(default(Vector3D))) benchmark = new Vector3D(0, 10, 0);
        if (dir.Equals(default(Vector3D))) dir = Vector3D.Forward;

        var settings = new GradeTargetSettings
        {
            TargetCutDepthMeters = depth,
            SlopePercent = slope,
            CrossSlopePercent = crossSlope,
            ToleranceMeters = tolerance
        };
        return new GradeAssist.Core.GradePlane(benchmark, dir, settings);
    }

    private static void AssertMirror(UnityGradePlane unityPlane, GradeAssist.Core.GradePlane corePlane, Vector3 worldPoint)
    {
        var unityTarget = unityPlane.HeightAt(worldPoint);
        var coreTarget = corePlane.HeightAt(new Vector3D(worldPoint.x, worldPoint.y, worldPoint.z));
        Assert.AreEqual(coreTarget, unityTarget, Tolerance,
            $"HeightAt mismatch at {worldPoint}: Core={coreTarget}, Unity={unityTarget}");

        var unityError = unityPlane.ComputeError(worldPoint);
        var coreError = corePlane.ComputeError(new Vector3D(worldPoint.x, worldPoint.y, worldPoint.z));
        Assert.AreEqual(coreError.TargetY, unityError.TargetY, Tolerance,
            $"TargetY mismatch at {worldPoint}: Core={coreError.TargetY}, Unity={unityError.TargetY}");
        Assert.AreEqual(coreError.ErrorMeters, unityError.ErrorMeters, Tolerance,
            $"ErrorMeters mismatch at {worldPoint}: Core={coreError.ErrorMeters}, Unity={unityError.ErrorMeters}");
        Assert.AreEqual(coreError.Status, unityError.Status,
            $"Status mismatch at {worldPoint}: Core={coreError.Status}, Unity={unityError.Status}");
    }

    [Test]
    public void FlatBenchmarkDepthZeroReturnsOnGrade()
    {
        var up = CreateUnityPlane();
        var cp = CreateCorePlane();
        AssertMirror(up, cp, new Vector3(0, 10, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void PositiveCutDepthMovesTargetBelowBenchmark()
    {
        var up = CreateUnityPlane(depth: 1.5f);
        var cp = CreateCorePlane(depth: 1.5f);
        AssertMirror(up, cp, new Vector3(0, 10, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void PositiveErrorMeansAboveGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 9.2f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void NegativeErrorMeansBelowGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 8.8f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void SlopePercentDividesBy100()
    {
        var up = CreateUnityPlane(depth: 0f, slope: 2.0f);
        var cp = CreateCorePlane(depth: 0f, slope: 2.0f);
        AssertMirror(up, cp, new Vector3(0, 10, 100));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void GradeDirectionControlsAlongDistance()
    {
        var up = CreateUnityPlane(dir: Vector3.right, slope: 5.0f);
        var cp = CreateCorePlane(dir: new Vector3D(1, 0, 0), slope: 5.0f);
        AssertMirror(up, cp, new Vector3(20, 10, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void CrossSlopeAppliesPerpendicular()
    {
        var up = CreateUnityPlane(crossSlope: 2.0f);
        var cp = CreateCorePlane(crossSlope: 2.0f);
        AssertMirror(up, cp, new Vector3(-50, 10, 0));
        AssertMirror(up, cp, new Vector3(50, 10, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void ZeroDirectionFallsBackToForward()
    {
        var up = CreateUnityPlane(dir: Vector3.zero, slope: 1.0f);
        var cp = CreateCorePlane(dir: new Vector3D(0, 0, 0), slope: 1.0f);
        AssertMirror(up, cp, new Vector3(0, 10, 100));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void ExactPositiveToleranceBoundaryReturnsOnGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 9.03f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void ExactNegativeToleranceBoundaryReturnsOnGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 8.97f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void JustAboveToleranceReturnsAboveGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 9.0301f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void JustBelowNegativeToleranceReturnsBelowGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.03f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.03f);
        AssertMirror(up, cp, new Vector3(0, 8.9699f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void CrossSlopeWithNonForwardGradeDirection()
    {
        var up = CreateUnityPlane(dir: Vector3.right, crossSlope: 2.0f);
        var cp = CreateCorePlane(dir: new Vector3D(1, 0, 0), crossSlope: 2.0f);
        AssertMirror(up, cp, new Vector3(0, 10, 50));
        AssertMirror(up, cp, new Vector3(0, 10, -50));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void NegativeSlopePercentDescendsAlongDirection()
    {
        var up = CreateUnityPlane(slope: -2.0f);
        var cp = CreateCorePlane(slope: -2.0f);
        AssertMirror(up, cp, new Vector3(0, 10, 100));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void NegativeCrossSlopeInvertsPerpendicularEffect()
    {
        var up = CreateUnityPlane(crossSlope: -2.0f);
        var cp = CreateCorePlane(crossSlope: -2.0f);
        AssertMirror(up, cp, new Vector3(-50, 10, 0));
        AssertMirror(up, cp, new Vector3(50, 10, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void VeryLargeCoordinatesMaintainPrecision()
    {
        var benchmark = new Vector3(100000, 500, 200000);
        var benchmarkD = new Vector3D(100000, 500, 200000);
        var up = CreateUnityPlane(benchmark: benchmark);
        var cp = CreateCorePlane(benchmark: benchmarkD);
        AssertMirror(up, cp, benchmark);
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void ZeroToleranceAnyErrorIsOffGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.0f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.0f);
        AssertMirror(up, cp, new Vector3(0, 9.001f, 0));
        Object.DestroyImmediate(up.gameObject);
    }

    [Test]
    public void ZeroToleranceExactZeroIsOnGrade()
    {
        var up = CreateUnityPlane(depth: 1.0f, tolerance: 0.0f);
        var cp = CreateCorePlane(depth: 1.0f, tolerance: 0.0f);
        AssertMirror(up, cp, new Vector3(0, 9.0f, 0));
        Object.DestroyImmediate(up.gameObject);
    }
}
