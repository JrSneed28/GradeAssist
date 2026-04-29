namespace GradeAssist.Core.Controllers;

public sealed class ControllerMockState
{
    public DateTimeOffset CurrentTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastTelemetryTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public Vector3D BucketPosition { get; set; }
    public double ManualInputMagnitude { get; set; }
    public bool Enabled { get; set; }
    public bool Armed { get; set; }
    public bool Active { get; set; }
    public bool EmergencyDisable { get; set; }
    public bool Reset { get; set; }
    public bool ConfigValid { get; set; } = true;

    public GradePlane? GradePlane { get; set; }
    public double? SwingAngleDegrees { get; set; }
    public Vector3D? SwingCenter { get; set; }
    public double? SwingRadius { get; set; }
    public Vector3D? FenceCenter { get; set; }
    public double? FenceRadius { get; set; }
    public double? BucketTargetDepth { get; set; }
}
