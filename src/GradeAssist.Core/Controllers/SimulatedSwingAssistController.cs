namespace GradeAssist.Core.Controllers;

public sealed class SimulatedSwingAssistController : SimulatedAssistController
{
    public double ProportionalGain { get; set; } = 1.0;

    protected override double ComputeDemand(ControllerMockState state)
    {
        if (state.SwingCenter == null || state.SwingRadius == null || state.SwingAngleDegrees == null)
        {
            return 0.0;
        }

        var center = state.SwingCenter.Value;
        var pos = state.BucketPosition;
        var dx = pos.X - center.X;
        var dz = pos.Z - center.Z;
        var actualRadius = Math.Sqrt(dx * dx + dz * dz);
        var radiusError = actualRadius - state.SwingRadius.Value;

        var actualAngle = Math.Atan2(dx, dz) * 180.0 / Math.PI;
        var angleError = NormalizeAngle(actualAngle - state.SwingAngleDegrees.Value);

        return -ProportionalGain * (radiusError + angleError * 0.01);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > 180.0) angle -= 360.0;
        while (angle < -180.0) angle += 360.0;
        return angle;
    }
}
