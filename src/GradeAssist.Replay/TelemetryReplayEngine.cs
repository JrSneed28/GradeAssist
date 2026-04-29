using GradeAssist.Core;

namespace GradeAssist.Replay;

public sealed class TelemetryReplayEngine
{
    private readonly GradePlane _plane;

    public TelemetryReplayEngine(GradePlane plane)
    {
        _plane = plane ?? throw new ArgumentNullException(nameof(plane));
    }

    public ReplaySampleResult ProcessSample(TelemetrySample sample)
    {
        var point = new Vector3D(sample.BucketX, sample.BucketY, sample.BucketZ);
        var error = _plane.ComputeError(point);
        return new ReplaySampleResult(sample, error.TargetY, error.ErrorMeters, error.Status);
    }

    public ReplayReport Run(IReadOnlyList<TelemetrySample> samples)
    {
        var results = new List<ReplaySampleResult>(samples.Count);
        foreach (var sample in samples)
        {
            results.Add(ProcessSample(sample));
        }

        var duration = samples.Count > 0 ? samples[^1].TimeSeconds - samples[0].TimeSeconds : 0.0;
        return ReplayReport.Build(results, 0, Array.Empty<string>(), duration);
    }
}
