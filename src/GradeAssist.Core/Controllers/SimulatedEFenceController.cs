namespace GradeAssist.Core.Controllers;

public sealed class SimulatedEFenceController : SimulatedAssistController
{
    public double ProportionalGain { get; set; } = 2.0;
    public double SoftBufferMeters { get; set; } = 0.5;

    protected override double ComputeDemand(ControllerMockState state)
    {
        if (state.FenceCenter == null || state.FenceRadius == null)
        {
            return 0.0;
        }

        var center = state.FenceCenter.Value;
        var pos = state.BucketPosition;
        var dx = pos.X - center.X;
        var dz = pos.Z - center.Z;
        var distance = Math.Sqrt(dx * dx + dz * dz);
        var radius = state.FenceRadius.Value;

        if (distance <= radius)
        {
            return 0.0;
        }

        var overshoot = distance - radius;
        if (overshoot <= SoftBufferMeters)
        {
            return -ProportionalGain * overshoot / SoftBufferMeters;
        }

        return -ProportionalGain;
    }
}
