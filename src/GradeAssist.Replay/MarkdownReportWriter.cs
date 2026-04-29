using System.Globalization;

namespace GradeAssist.Replay;

public sealed class MarkdownReportWriter
{
    public void Write(ReplayReport report, string outputPath)
    {
        var lines = new List<string>
        {
            "# Telemetry Replay Report",
            "",
            $"- **Total Samples**: {report.TotalSamples}",
            $"- **Valid Samples**: {report.ValidSamples}",
            $"- **Invalid Rows**: {report.InvalidRows}",
            $"- **Duration**: {Fmt(report.DurationSeconds)} s",
            "",
            "## Grade Error Summary",
            "",
            $"- **Min Error**: {Fmt(report.MinError)} m",
            $"- **Max Error**: {Fmt(report.MaxError)} m",
            $"- **Mean Error**: {Fmt(report.MeanError)} m",
            "",
            "## Status Counts",
            "",
            $"- **Above Grade**: {report.AboveGradeCount}",
            $"- **On Grade**: {report.OnGradeCount}",
            $"- **Below Grade**: {report.BelowGradeCount}",
            "",
            "## Worst Cases",
            "",
            $"- **Worst Above-Grade**: {Fmt(report.WorstAboveGrade)} m",
            $"- **Worst Overcut**: {Fmt(report.WorstOvercut)} m",
            ""
        };

        if (report.InvalidRowMessages.Count > 0)
        {
            lines.Add("## Invalid Rows");
            lines.Add("");
            foreach (var msg in report.InvalidRowMessages)
            {
                lines.Add($"- {msg}");
            }
            lines.Add("");
        }

        lines.Add("## Per-Sample Results");
        lines.Add("");
        lines.Add("| Time (s) | X | Y | Z | Target Y | Error (m) | Status |");
        lines.Add("|----------|---|---|---|----------|-----------|--------|");

        foreach (var r in report.SampleResults)
        {
            lines.Add($"| {Fmt(r.Sample.TimeSeconds)} | {Fmt(r.Sample.BucketX)} | {Fmt(r.Sample.BucketY)} | {Fmt(r.Sample.BucketZ)} | {Fmt(r.TargetY)} | {Fmt(r.ErrorMeters)} | {r.Status} |");
        }

        lines.Add("");
        File.WriteAllLines(outputPath, lines);
    }

    private static string Fmt(double value)
    {
        if (!double.IsFinite(value)) return "N/A";
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }
}
