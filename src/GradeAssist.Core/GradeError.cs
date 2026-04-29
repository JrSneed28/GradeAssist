namespace GradeAssist.Core;

public readonly record struct GradeError(
    Vector3D ReferencePoint,
    double TargetY,
    double ErrorMeters,
    double ToleranceMeters)
{
    public GradeStatus Status
    {
        get
        {
            if (ToleranceMeters < 0)
            {
                throw new InvalidOperationException("Tolerance must be non-negative.");
            }
            if (ErrorMeters > ToleranceMeters) return GradeStatus.AboveGrade;
            if (ErrorMeters < -ToleranceMeters) return GradeStatus.BelowGrade;
            return GradeStatus.OnGrade;
        }
    }
}
