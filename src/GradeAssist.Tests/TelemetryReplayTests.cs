using GradeAssist.Core;
using GradeAssist.Replay;
using Xunit;

namespace GradeAssist.Tests;

public class TelemetryReplayTests
{
    private static void Near(double actual, double expected, double tolerance = 1e-9)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new Exception($"Expected {expected}, got {actual}");
        }
    }

    private static GradePlane DefaultPlane()
    {
        return new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(1.0, 0.0, 0.0, 0.03));
    }

    [Fact]
    public void CsvReaderParsesFlatSample()
    {
        var reader = new CsvTelemetryReader("telemetry/samples/slope-pass.csv");
        var samples = reader.ReadSamples(out var invalid);

        Assert.Equal(3, samples.Count);
        Assert.Empty(invalid);

        Assert.Equal(0.0, samples[0].TimeSeconds);
        Assert.Equal(0.0, samples[0].BucketX);
        Assert.Equal(10.0, samples[0].BucketY);
        Assert.Equal(0.0, samples[0].BucketZ);

        Assert.Equal(1.0, samples[1].TimeSeconds);
        Assert.Equal(0.0, samples[1].BucketX);
        Assert.Equal(11.0, samples[1].BucketY);
        Assert.Equal(50.0, samples[1].BucketZ);
    }

    [Fact]
    public void CsvReaderParsesOptionalFields()
    {
        var reader = new CsvTelemetryReader("telemetry/samples/with-optional-fields.csv");
        var samples = reader.ReadSamples(out var invalid);

        Assert.Equal(3, samples.Count);
        Assert.Empty(invalid);

        Near(samples[0].BoomAngle!.Value, 10.5);
        Near(samples[0].StickAngle!.Value, 20.0);
        Near(samples[0].BucketAngle!.Value, 30.0);
        Near(samples[0].SwingAngle!.Value, 45.0);
    }

    [Fact]
    public void CsvReaderSkipsBadRowsAndRecordsMessages()
    {
        var reader = new CsvTelemetryReader("telemetry/samples/bad-rows.csv");
        var samples = reader.ReadSamples(out var invalid);

        Assert.Equal(4, samples.Count);
        Assert.Equal(3, invalid.Count);

        Assert.Contains(invalid, m => m.Contains("bad_time"));
        Assert.Contains(invalid, m => m.Contains("NaN"));
        Assert.Contains(invalid, m => m.Contains("Infinity"));
    }

    [Fact]
    public void CsvReaderSkipsCommentsAndEmptyLines()
    {
        var reader = new CsvTelemetryReader("telemetry/samples/bad-rows.csv");
        var samples = reader.ReadSamples(out var invalid);

        Assert.Equal(4, samples.Count);
        foreach (var s in samples)
        {
            Assert.True(double.IsFinite(s.BucketX));
            Assert.True(double.IsFinite(s.BucketY));
            Assert.True(double.IsFinite(s.BucketZ));
        }
    }

    [Fact]
    public void ReplayEngineComputesCorrectError()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var sample = new TelemetrySample(0.0, 0, 9.0, 0);
        var result = engine.ProcessSample(sample);

        Near(result.TargetY, 9.0);
        Near(result.ErrorMeters, 0.0);
        Assert.Equal(GradeStatus.OnGrade, result.Status);
    }

    [Fact]
    public void ReplayEngineAboveGradePositiveError()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var sample = new TelemetrySample(0.0, 0, 9.5, 0);
        var result = engine.ProcessSample(sample);

        Near(result.TargetY, 9.0);
        Near(result.ErrorMeters, 0.5);
        Assert.Equal(GradeStatus.AboveGrade, result.Status);
    }

    [Fact]
    public void ReplayEngineBelowGradeNegativeError()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var sample = new TelemetrySample(0.0, 0, 8.8, 0);
        var result = engine.ProcessSample(sample);

        Near(result.TargetY, 9.0);
        Near(result.ErrorMeters, -0.2);
        Assert.Equal(GradeStatus.BelowGrade, result.Status);
    }

    [Fact]
    public void ReplayEngineWithSlope()
    {
        var plane = new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(1.0, 2.0, 0.0, 0.03));
        var engine = new TelemetryReplayEngine(plane);
        var sample = new TelemetrySample(0.0, 0, 10.0, 100);
        var result = engine.ProcessSample(sample);

        Near(result.TargetY, 11.0);
        Near(result.ErrorMeters, -1.0);
    }

    [Fact]
    public void RunProducesReportWithCorrectCounts()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var samples = new List<TelemetrySample>
        {
            new(0.0, 0, 8.8, 0),
            new(0.5, 0, 9.0, 0),
            new(1.0, 0, 9.5, 0),
        };

        var report = engine.Run(samples);

        Assert.Equal(3, report.TotalSamples);
        Assert.Equal(3, report.ValidSamples);
        Assert.Equal(0, report.InvalidRows);

        Assert.Equal(1, report.BelowGradeCount);
        Assert.Equal(1, report.OnGradeCount);
        Assert.Equal(1, report.AboveGradeCount);
    }

    [Fact]
    public void ReportStatisticsAreCorrect()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var samples = new List<TelemetrySample>
        {
            new(0.0, 0, 8.5, 0),
            new(0.5, 0, 9.0, 0),
            new(1.0, 0, 9.5, 0),
        };

        var report = engine.Run(samples);

        Near(report.MinError, -0.5);
        Near(report.MaxError, 0.5);
        Near(report.MeanError, 0.0);
        Near(report.WorstOvercut, -0.5);
        Near(report.WorstAboveGrade, 0.5);
    }

    [Fact]
    public void ReportHandlesNoSamples()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var report = engine.Run(new List<TelemetrySample>());

        Assert.Equal(0, report.TotalSamples);
        Assert.Equal(0, report.ValidSamples);
        Assert.Equal(0, report.AboveGradeCount);
        Assert.Equal(0, report.OnGradeCount);
        Assert.Equal(0, report.BelowGradeCount);
    }

    [Fact]
    public void ReportIncludesInvalidRowsInBuild()
    {
        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var samples = new List<TelemetrySample>
        {
            new(0.0, 0, 9.0, 0),
        };

        var report = ReplayReport.Build(
            new List<ReplaySampleResult> { engine.ProcessSample(samples[0]) },
            2,
            new List<string> { "bad row 1", "bad row 2" },
            0.0);

        Assert.Equal(3, report.TotalSamples);
        Assert.Equal(1, report.ValidSamples);
        Assert.Equal(2, report.InvalidRows);
        Assert.Equal(2, report.InvalidRowMessages.Count);
    }

    [Fact]
    public void MarkdownReportWriterCreatesFile()
    {
        var report = new ReplayReport
        {
            TotalSamples = 1,
            ValidSamples = 1,
            MinError = -0.1,
            MaxError = 0.1,
            MeanError = 0.0,
            AboveGradeCount = 0,
            OnGradeCount = 1,
            BelowGradeCount = 0,
            WorstOvercut = 0.0,
            WorstAboveGrade = 0.0,
            DurationSeconds = 0.5,
            SampleResults = new List<ReplaySampleResult>
            {
                new(new TelemetrySample(0.0, 0, 9.0, 0), 9.0, 0.0, GradeStatus.OnGrade)
            }
        };

        var path = Path.Combine(Path.GetTempPath(), $"md-report-{Guid.NewGuid()}.md");
        var writer = new MarkdownReportWriter();
        writer.Write(report, path);

        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("# Telemetry Replay Report", content);
        Assert.Contains("Total Samples", content);
        File.Delete(path);
    }

    [Fact]
    public void CsvReportWriterCreatesFile()
    {
        var report = new ReplayReport
        {
            TotalSamples = 1,
            ValidSamples = 1,
            MinError = -0.1,
            MaxError = 0.1,
            MeanError = 0.0,
            AboveGradeCount = 0,
            OnGradeCount = 1,
            BelowGradeCount = 0,
            WorstOvercut = 0.0,
            WorstAboveGrade = 0.0,
            DurationSeconds = 0.5,
            SampleResults = new List<ReplaySampleResult>
            {
                new(new TelemetrySample(0.0, 0, 9.0, 0), 9.0, 0.0, GradeStatus.OnGrade)
            }
        };

        var path = Path.Combine(Path.GetTempPath(), $"csv-report-{Guid.NewGuid()}.csv");
        var writer = new CsvReportWriter();
        writer.Write(report, path);

        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("timeSeconds,bucketX,bucketY,bucketZ,targetY,errorMeters,status", content);
        Assert.Contains("# Summary", content);
        File.Delete(path);
    }

    [Fact]
    public void ReplayWithSampleCsvProducesDeterministicOutput()
    {
        var reader = new CsvTelemetryReader("telemetry/samples/simple-flat-pass.csv");
        var samples = reader.ReadSamples(out var invalid);
        Assert.Empty(invalid);

        var plane = DefaultPlane();
        var engine = new TelemetryReplayEngine(plane);
        var report = engine.Run(samples);

        Assert.Equal(3, report.TotalSamples);
        // simple-flat-pass: y values are 8.95, 9.00, 9.05; targetY = 10 - 1.0 = 9.0
        // errors: -0.05, 0.0, 0.05
        Near(report.MinError, -0.05);
        Near(report.MaxError, 0.05);
        Near(report.MeanError, 0.0);
        Assert.Equal(1, report.AboveGradeCount);
        Assert.Equal(1, report.OnGradeCount);
        Assert.Equal(1, report.BelowGradeCount);
    }
}
