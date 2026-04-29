using GradeAssist.Core;

namespace GradeAssist.Replay;

public sealed class ReplayReport
{
    public int TotalSamples { get; init; }
    public int ValidSamples { get; init; }
    public int InvalidRows { get; init; }
    public double DurationSeconds { get; init; }

    public double MinError { get; init; }
    public double MaxError { get; init; }
    public double MeanError { get; init; }

    public int AboveGradeCount { get; init; }
    public int OnGradeCount { get; init; }
    public int BelowGradeCount { get; init; }

    public double WorstOvercut { get; init; }
    public double WorstAboveGrade { get; init; }

    public IReadOnlyList<ReplaySampleResult> SampleResults { get; init; } = Array.Empty<ReplaySampleResult>();
    public IReadOnlyList<string> InvalidRowMessages { get; init; } = Array.Empty<string>();

    public static ReplayReport Build(IEnumerable<ReplaySampleResult> results, int invalidRows, IEnumerable<string> invalidRowMessages, double durationSeconds)
    {
        var sampleList = results.ToList();
        var validCount = sampleList.Count;
        var totalCount = validCount + invalidRows;

        if (validCount == 0)
        {
            return new ReplayReport
            {
                TotalSamples = totalCount,
                ValidSamples = 0,
                InvalidRows = invalidRows,
                DurationSeconds = durationSeconds,
                MinError = double.NaN,
                MaxError = double.NaN,
                MeanError = double.NaN,
                AboveGradeCount = 0,
                OnGradeCount = 0,
                BelowGradeCount = 0,
                WorstOvercut = double.NaN,
                WorstAboveGrade = double.NaN,
                SampleResults = sampleList,
                InvalidRowMessages = invalidRowMessages.ToList()
            };
        }

        var minError = double.PositiveInfinity;
        var maxError = double.NegativeInfinity;
        var sumError = 0.0;
        var aboveCount = 0;
        var onCount = 0;
        var belowCount = 0;
        var worstOvercut = 0.0;
        var worstAboveGrade = 0.0;

        foreach (var r in sampleList)
        {
            var e = r.ErrorMeters;
            if (e < minError) minError = e;
            if (e > maxError) maxError = e;
            sumError += e;

            switch (r.Status)
            {
                case GradeStatus.AboveGrade:
                    aboveCount++;
                    if (e > worstAboveGrade) worstAboveGrade = e;
                    break;
                case GradeStatus.OnGrade:
                    onCount++;
                    break;
                case GradeStatus.BelowGrade:
                    belowCount++;
                    if (Math.Abs(e) > Math.Abs(worstOvercut)) worstOvercut = e;
                    break;
            }
        }

        return new ReplayReport
        {
            TotalSamples = totalCount,
            ValidSamples = validCount,
            InvalidRows = invalidRows,
            DurationSeconds = durationSeconds,
            MinError = minError,
            MaxError = maxError,
            MeanError = sumError / validCount,
            AboveGradeCount = aboveCount,
            OnGradeCount = onCount,
            BelowGradeCount = belowCount,
            WorstOvercut = worstOvercut,
            WorstAboveGrade = worstAboveGrade,
            SampleResults = sampleList,
            InvalidRowMessages = invalidRowMessages.ToList()
        };
    }
}
