namespace GradeAssist.Replay;

public readonly record struct TelemetrySample(
    double TimeSeconds,
    double BucketX,
    double BucketY,
    double BucketZ,
    double? BoomAngle = null,
    double? StickAngle = null,
    double? BucketAngle = null,
    double? SwingAngle = null);
