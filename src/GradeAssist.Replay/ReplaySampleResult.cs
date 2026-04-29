using GradeAssist.Core;

namespace GradeAssist.Replay;

public readonly record struct ReplaySampleResult(
    TelemetrySample Sample,
    double TargetY,
    double ErrorMeters,
    GradeStatus Status);
