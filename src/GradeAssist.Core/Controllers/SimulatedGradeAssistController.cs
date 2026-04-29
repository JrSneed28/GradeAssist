namespace GradeAssist.Core.Controllers;

public sealed class SimulatedGradeAssistController : SimulatedAssistController
{
    public double ProportionalGain { get; set; } = 1.0;

    protected override double ComputeDemand(ControllerMockState state)
    {
        if (state.GradePlane == null)
        {
            return 0.0;
        }

        var error = state.GradePlane.ComputeError(state.BucketPosition);
        return -ProportionalGain * error.ErrorMeters;
    }
}
