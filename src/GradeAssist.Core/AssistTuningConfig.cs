namespace GradeAssist.Core;

public sealed record AssistTuningConfig(
    bool SimOnly = true,
    bool ControllersLockedByDefault = true,
    double MaxOutput = 0.0,
    double MaxOutputChangePerSecond = 0.0,
    double ManualOverrideThreshold = 0.0,
    double WatchdogTimeoutSeconds = 1.0)
{
    public void Validate()
    {
        if (!double.IsFinite(MaxOutput))
        {
            throw new ArgumentOutOfRangeException(nameof(MaxOutput), "maxOutput must be finite.");
        }

        if (MaxOutput < 0 || MaxOutput > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxOutput), "maxOutput must be between 0 and 1.");
        }

        if (!double.IsFinite(MaxOutputChangePerSecond) || MaxOutputChangePerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxOutputChangePerSecond), "maxOutputChangePerSecond must be a non-negative finite value.");
        }

        if (!double.IsFinite(ManualOverrideThreshold) || ManualOverrideThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ManualOverrideThreshold), "manualOverrideThreshold must be a non-negative finite value.");
        }

        if (!double.IsFinite(WatchdogTimeoutSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(WatchdogTimeoutSeconds), "watchdogTimeoutSeconds must be finite.");
        }

        if (WatchdogTimeoutSeconds < 0.01 || WatchdogTimeoutSeconds > 5.0)
        {
            throw new ArgumentOutOfRangeException(nameof(WatchdogTimeoutSeconds), "watchdogTimeoutSeconds must be between 0.01 and 5.0 seconds.");
        }

        if (!SimOnly)
        {
            throw new InvalidOperationException("simOnly must be true: controllers operate in simulation only.");
        }
    }
}
