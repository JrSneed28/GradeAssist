using System.Globalization;
using GradeAssist.Core;

namespace GradeAssist.Replay;

public class Program
{
    private static readonly string[] ProhibitedPrefixes = new[]
    {
        @"C:\XboxGames",
        @"C:\Program Files (x86)\Steam",
        @"C:\Program Files\Steam",
    };

    public static void Main(string[] args)
    {
        var inputDir = args.Length > 0 ? args[0] : "telemetry/samples";
        var outputDir = args.Length > 1 ? args[1] : "artifacts/replay";

        ValidatePath(inputDir, nameof(inputDir));
        ValidatePath(outputDir, nameof(outputDir));

        if (!Directory.Exists(inputDir))
        {
            Console.WriteLine($"Input directory not found: {inputDir}");
            Environment.Exit(1);
        }

        Directory.CreateDirectory(outputDir);

        var benchmark = new Vector3D(0, 10, 0);
        var gradeDir = Vector3D.Forward;
        var settings = new GradeTargetSettings(1.0, 0.0, 0.0, 0.03);
        var plane = new GradePlane(benchmark, gradeDir, settings);
        var engine = new TelemetryReplayEngine(plane);
        var mdWriter = new MarkdownReportWriter();
        var csvWriter = new CsvReportWriter();

        var csvFiles = Directory.GetFiles(inputDir, "*.csv");
        if (csvFiles.Length == 0)
        {
            Console.WriteLine($"No CSV files found in {inputDir}");
            Environment.Exit(1);
        }

        foreach (var csvFile in csvFiles.OrderBy(f => Path.GetFileName(f)))
        {
            var fileName = Path.GetFileNameWithoutExtension(csvFile);
            Console.WriteLine($"Processing: {fileName}");

            var reader = new CsvTelemetryReader(csvFile);
            var samples = reader.ReadSamples(out var invalidRows);

            var report = engine.Run(samples);
            var duration = samples.Count > 0 ? samples[^1].TimeSeconds - samples[0].TimeSeconds : 0.0;
            report = ReplayReport.Build(report.SampleResults, invalidRows.Count, invalidRows, duration);

            var mdPath = Path.Combine(outputDir, $"{fileName}-report.md");
            var csvPath = Path.Combine(outputDir, $"{fileName}-report.csv");

            mdWriter.Write(report, mdPath);
            csvWriter.Write(report, csvPath);

            Console.WriteLine($"  Valid: {report.ValidSamples}, Invalid: {report.InvalidRows}, Duration: {report.DurationSeconds:F3}s");
        }

        Console.WriteLine($"Reports written to: {outputDir}");
    }

    private static void ValidatePath(string path, string name)
    {
        var full = Path.GetFullPath(path);
        foreach (var bad in ProhibitedPrefixes)
        {
            if (full.StartsWith(bad, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{name} resolves to a prohibited directory: {full}");
            }
        }
    }
}
