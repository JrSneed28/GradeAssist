namespace GradeAssist.Core;

public sealed record GradeTargetSettings(
    double TargetCutDepthMeters,
    double SlopePercent,
    double CrossSlopePercent,
    double ToleranceMeters)
{
    public double SlopeDecimal => SlopePercent / 100.0;
    public double CrossSlopeDecimal => CrossSlopePercent / 100.0;

    public void Validate()
    {
        if (!double.IsFinite(TargetCutDepthMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(TargetCutDepthMeters), "Value must be finite.");
        }
        if (!double.IsFinite(SlopePercent))
        {
            throw new ArgumentOutOfRangeException(nameof(SlopePercent), "Value must be finite.");
        }
        if (!double.IsFinite(CrossSlopePercent))
        {
            throw new ArgumentOutOfRangeException(nameof(CrossSlopePercent), "Value must be finite.");
        }
        if (!double.IsFinite(ToleranceMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(ToleranceMeters), "Value must be finite.");
        }
        if (TargetCutDepthMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetCutDepthMeters), "Use positive targetCutDepthMeters when desired grade is below the benchmark.");
        }
        if (ToleranceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ToleranceMeters), "Tolerance must be non-negative.");
        }
        if (Math.Abs(SlopePercent) > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(SlopePercent), "Slope percent exceeds prototype limit.");
        }
        if (Math.Abs(CrossSlopePercent) > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(CrossSlopePercent), "Cross slope percent exceeds prototype limit.");
        }
    }
}
