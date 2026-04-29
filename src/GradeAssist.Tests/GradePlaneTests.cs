using GradeAssist.Core;
using Xunit;

namespace GradeAssist.Tests;

public class GradePlaneTests
{
    private static void Near(double actual, double expected, double tolerance = 1e-9)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new Exception($"Expected {expected}, got {actual}");
        }
    }

    private static void Equal<T>(T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            throw new Exception($"Expected {expected}, got {actual}");
        }
    }

    private static GradePlane Plane(
        Vector3D? benchmark = null,
        Vector3D? dir = null,
        double depth = 0,
        double slope = 0,
        double crossSlope = 0,
        double tolerance = 0.03)
    {
        return new GradePlane(
            benchmark ?? new Vector3D(0, 10, 0),
            dir ?? Vector3D.Forward,
            new GradeTargetSettings(depth, slope, crossSlope, tolerance));
    }

    [Fact]
    public void FlatBenchmarkDepthZeroReturnsOnGrade()
    {
        var plane = Plane();
        var error = plane.ComputeError(new Vector3D(0, 10, 0));
        Near(error.TargetY, 10);
        Near(error.ErrorMeters, 0);
        Equal(error.Status, GradeStatus.OnGrade);
    }

    [Fact]
    public void PositiveCutDepthMovesTargetBelowBenchmark()
    {
        var plane = Plane(depth: 1.5);
        var target = plane.HeightAt(new Vector3D(0, 10, 0));
        Near(target, 8.5);
    }

    [Fact]
    public void PositiveErrorMeansAboveGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 9.2, 0));
        Near(error.TargetY, 9.0);
        Near(error.ErrorMeters, 0.2);
        Equal(error.Status, GradeStatus.AboveGrade);
    }

    [Fact]
    public void NegativeErrorMeansBelowGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 8.8, 0));
        Near(error.TargetY, 9.0);
        Near(error.ErrorMeters, -0.2);
        Equal(error.Status, GradeStatus.BelowGrade);
    }

    [Fact]
    public void SlopePercentDividesBy100()
    {
        var plane = Plane(depth: 0, slope: 2.0);
        var target = plane.HeightAt(new Vector3D(0, 10, 100));
        Near(target, 12.0);
    }

    [Fact]
    public void GradeDirectionControlsAlongDistance()
    {
        var plane = Plane(dir: new Vector3D(1, 0, 0), slope: 5.0);
        var target = plane.HeightAt(new Vector3D(20, 10, 0));
        Near(target, 11.0);
    }

    [Fact]
    public void CrossSlopeAppliesPerpendicular()
    {
        var plane = Plane(dir: Vector3D.Forward, crossSlope: 2.0);
        var targetLeft = plane.HeightAt(new Vector3D(-50, 10, 0));
        var targetRight = plane.HeightAt(new Vector3D(50, 10, 0));
        Near(targetLeft, 11.0);
        Near(targetRight, 9.0);
    }

    [Fact]
    public void ZeroDirectionFallsBackToForward()
    {
        var plane = Plane(dir: Vector3D.Zero, slope: 1.0);
        var target = plane.HeightAt(new Vector3D(0, 10, 100));
        Near(target, 11.0);
    }

    [Fact]
    public void ExactPositiveToleranceBoundaryReturnsOnGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 9.03, 0));
        Near(error.ErrorMeters, 0.03);
        Equal(error.Status, GradeStatus.OnGrade);
    }

    [Fact]
    public void ExactNegativeToleranceBoundaryReturnsOnGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 8.97, 0));
        Near(error.ErrorMeters, -0.03);
        Equal(error.Status, GradeStatus.OnGrade);
    }

    [Fact]
    public void JustAboveToleranceReturnsAboveGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 9.0301, 0));
        Near(error.ErrorMeters, 0.0301, 1e-6);
        Equal(error.Status, GradeStatus.AboveGrade);
    }

    [Fact]
    public void JustBelowNegativeToleranceReturnsBelowGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.03);
        var error = plane.ComputeError(new Vector3D(0, 8.9699, 0));
        Near(error.ErrorMeters, -0.0301, 1e-6);
        Equal(error.Status, GradeStatus.BelowGrade);
    }

    [Fact]
    public void CrossSlopeWithNonForwardGradeDirection()
    {
        var plane = Plane(dir: new Vector3D(1, 0, 0), crossSlope: 2.0);
        var targetForward = plane.HeightAt(new Vector3D(0, 10, 50));
        var targetBack = plane.HeightAt(new Vector3D(0, 10, -50));
        Near(targetForward, 11.0);
        Near(targetBack, 9.0);
    }

    [Fact]
    public void NegativeSlopePercentDescendsAlongDirection()
    {
        var plane = Plane(slope: -2.0);
        var target = plane.HeightAt(new Vector3D(0, 10, 100));
        Near(target, 8.0);
    }

    [Fact]
    public void NegativeCrossSlopeInvertsPerpendicularEffect()
    {
        var plane = Plane(dir: Vector3D.Forward, crossSlope: -2.0);
        var targetLeft = plane.HeightAt(new Vector3D(-50, 10, 0));
        var targetRight = plane.HeightAt(new Vector3D(50, 10, 0));
        Near(targetLeft, 9.0);
        Near(targetRight, 11.0);
    }

    [Fact]
    public void VeryLargeCoordinatesMaintainPrecision()
    {
        var benchmark = new Vector3D(100000, 500, 200000);
        var plane = new GradePlane(benchmark, Vector3D.Forward, new GradeTargetSettings(0, 0, 0, 0.03));
        var error = plane.ComputeError(new Vector3D(100000, 500, 200000));
        Near(error.ErrorMeters, 0, 1e-6);
        Equal(error.Status, GradeStatus.OnGrade);
    }

    [Fact]
    public void ZeroToleranceAnyErrorIsOffGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.0);
        var error = plane.ComputeError(new Vector3D(0, 9.001, 0));
        Near(error.TargetY, 9.0);
        Near(error.ErrorMeters, 0.001, 1e-6);
        Equal(error.Status, GradeStatus.AboveGrade);
    }

    [Fact]
    public void ZeroToleranceExactZeroIsOnGrade()
    {
        var plane = Plane(depth: 1.0, tolerance: 0.0);
        var error = plane.ComputeError(new Vector3D(0, 9.0, 0));
        Near(error.ErrorMeters, 0);
        Equal(error.Status, GradeStatus.OnGrade);
    }

    [Fact]
    public void NegativeCutDepthRejectedByValidation()
    {
        var settings = new GradeTargetSettings(-1.0, 0, 0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void NaNCutDepthRejectedByValidation()
    {
        var settings = new GradeTargetSettings(double.NaN, 0, 0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void InfinitySlopeRejectedByValidation()
    {
        var settings = new GradeTargetSettings(0, double.PositiveInfinity, 0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void NegativeToleranceRejectedByValidation()
    {
        var settings = new GradeTargetSettings(0, 0, 0, -0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void SlopeOver500RejectedByValidation()
    {
        var settings = new GradeTargetSettings(0, 501, 0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void CrossSlopeOver500RejectedByValidation()
    {
        var settings = new GradeTargetSettings(0, 0, -501, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GradePlane(new Vector3D(0, 10, 0), Vector3D.Forward, settings));
    }

    [Fact]
    public void NaNVectorNormalizesToFallback()
    {
        var nanVec = new Vector3D(double.NaN, 0, 0);
        var normalized = nanVec.NormalizeOr(Vector3D.Forward);
        Equal(normalized, Vector3D.Forward);
    }

    [Fact]
    public void GradeErrorNegativeToleranceThrows()
    {
        var error = new GradeError(new Vector3D(0, 10, 0), 9.0, 0.0, -0.03);
        Assert.Throws<InvalidOperationException>(() => error.Status);
    }
}
