namespace GradeAssist.Core;

public sealed record UnitySettingsConfig(
    SimulatorSettings Simulator,
    RenderTextureSettings RenderTexture,
    UiSettings Ui,
    ControlsSettings Controls,
    SafetySettings Safety)
{
    public void Validate()
    {
        if (Simulator is null)
        {
            throw new ArgumentNullException(nameof(Simulator), "simulator settings are required.");
        }

        if (RenderTexture is null)
        {
            throw new ArgumentNullException(nameof(RenderTexture), "renderTexture settings are required.");
        }

        if (Ui is null)
        {
            throw new ArgumentNullException(nameof(Ui), "ui settings are required.");
        }

        if (Controls is null)
        {
            throw new ArgumentNullException(nameof(Controls), "controls settings are required.");
        }

        if (Safety is null)
        {
            throw new ArgumentNullException(nameof(Safety), "safety settings are required.");
        }

        Simulator.Validate();
        RenderTexture.Validate();
        Ui.Validate();
        Controls.Validate();
        Safety.Validate();
    }
}

public sealed record SimulatorSettings(int TargetFps, double TimeScale)
{
    public void Validate()
    {
        if (TargetFps < 1 || TargetFps > 240)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetFps), "targetFps must be between 1 and 240.");
        }

        if (!double.IsFinite(TimeScale) || TimeScale < 0.1 || TimeScale > 10.0)
        {
            throw new ArgumentOutOfRangeException(nameof(TimeScale), "timeScale must be between 0.1 and 10.0.");
        }
    }
}

public sealed record RenderTextureSettings(int Width, int Height)
{
    public void Validate()
    {
        if (Width < 64 || Width > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "width must be between 64 and 4096.");
        }

        if (Height < 64 || Height > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), "height must be between 64 and 4096.");
        }
    }
}

public sealed record UiSettings(
    double NormalPrecisionMeters,
    double DiagnosticsPrecisionMeters,
    double DefaultOnGradeToleranceMeters,
    bool DiagnosticsDefaultVisible)
{
    public void Validate()
    {
        if (!double.IsFinite(NormalPrecisionMeters) || NormalPrecisionMeters < 0.001 || NormalPrecisionMeters > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(NormalPrecisionMeters), "normalPrecisionMeters must be between 0.001 and 1.0.");
        }

        if (!double.IsFinite(DiagnosticsPrecisionMeters) || DiagnosticsPrecisionMeters < 0.0001 || DiagnosticsPrecisionMeters > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DiagnosticsPrecisionMeters), "diagnosticsPrecisionMeters must be between 0.0001 and 1.0.");
        }

        if (!double.IsFinite(DefaultOnGradeToleranceMeters) || DefaultOnGradeToleranceMeters < 0.0 || DefaultOnGradeToleranceMeters > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultOnGradeToleranceMeters), "defaultOnGradeToleranceMeters must be between 0.0 and 1.0.");
        }
    }
}

public sealed record ControlsSettings(bool KeyboardEnabled, bool MouseEnabled, bool GamepadEnabled)
{
    public void Validate()
    {
        // Controls settings have no invalid states; booleans are always valid.
    }
}

public sealed record SafetySettings(bool GameIntegrationEnabled, bool AllowExternalWrites, IReadOnlyList<string> BlockedPaths)
{
    public void Validate()
    {
        if (GameIntegrationEnabled)
        {
            throw new InvalidOperationException("gameIntegrationEnabled must be false.");
        }

        if (AllowExternalWrites)
        {
            throw new InvalidOperationException("allowExternalWrites must be false.");
        }

        if (BlockedPaths is not null)
        {
            foreach (var path in BlockedPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("blockedPaths must not contain empty entries.");
                }
            }
        }
    }
}
