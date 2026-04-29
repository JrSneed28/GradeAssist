using System.Globalization;

namespace GradeAssist.Replay;

public sealed class CsvReportWriter
{
    public void Write(ReplayReport report, string outputPath)
    {
        var lines = new List<string>
        {
            "timeSeconds,bucketX,bucketY,bucketZ,targetY,errorMeters,status"
        };

        foreach (var r in report.SampleResults)
        {
            lines.Add($"{Escape(Fmt(r.Sample.TimeSeconds))},{Escape(Fmt(r.Sample.BucketX))},{Escape(Fmt(r.Sample.BucketY))},{Escape(Fmt(r.Sample.BucketZ))},{Escape(Fmt(r.TargetY))},{Escape(Fmt(r.ErrorMeters))},{r.Status}");
        }

        lines.Add("");
        lines.Add("# Summary");
        lines.Add($"totalSamples,{report.TotalSamples}");
        lines.Add($"validSamples,{report.ValidSamples}");
        lines.Add($"invalidRows,{report.InvalidRows}");
        lines.Add($"durationSeconds,{Fmt(report.DurationSeconds)}");
        lines.Add($"minError,{Fmt(report.MinError)}");
        lines.Add($"maxError,{Fmt(report.MaxError)}");
        lines.Add($"meanError,{Fmt(report.MeanError)}");
        lines.Add($"aboveGradeCount,{report.AboveGradeCount}");
        lines.Add($"onGradeCount,{report.OnGradeCount}");
        lines.Add($"belowGradeCount,{report.BelowGradeCount}");
        lines.Add($"worstAboveGrade,{Fmt(report.WorstAboveGrade)}");
        lines.Add($"worstOvercut,{Fmt(report.WorstOvercut)}");

        File.WriteAllLines(outputPath, lines);
    }

    private static string Fmt(double value)
    {
        return value.ToString("F6", CultureInfo.InvariantCulture);
    }

    private static string Escape(string field)
    {
        if (field.Length > 0 && (field[0] == '=' || field[0] == '+' || field[0] == '-' || field[0] == '@'))
        {
            return "'" + field;
        }
        return field;
    }
}
