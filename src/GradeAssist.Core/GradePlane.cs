namespace GradeAssist.Core;

public sealed class GradePlane
{
    public Vector3D BenchmarkPoint { get; }
    public Vector3D GradeDirectionXZ { get; }
    public GradeTargetSettings Settings { get; }

    public GradePlane(Vector3D benchmarkPoint, Vector3D gradeDirectionXZ, GradeTargetSettings settings)
    {
        settings.Validate();
        BenchmarkPoint = benchmarkPoint;
        GradeDirectionXZ = gradeDirectionXZ.FlattenXZ().NormalizeOr(Vector3D.Forward);
        Settings = settings;
    }

    public double HeightAt(Vector3D worldPoint)
    {
        var gradeDir = GradeDirectionXZ;
        var crossDir = new Vector3D(-gradeDir.Z, 0, gradeDir.X);
        var delta = worldPoint - BenchmarkPoint;

        var alongDistance = delta.Dot(gradeDir);
        var crossDistance = delta.Dot(crossDir);

        return BenchmarkPoint.Y
            - Settings.TargetCutDepthMeters
            + Settings.SlopeDecimal * alongDistance
            + Settings.CrossSlopeDecimal * crossDistance;
    }

    public GradeError ComputeError(Vector3D referencePoint)
    {
        var targetY = HeightAt(referencePoint);
        var error = referencePoint.Y - targetY;
        return new GradeError(referencePoint, targetY, error, Settings.ToleranceMeters);
    }
}
