namespace GradeAssist.Core.Controllers;

public sealed class SimulatedBucketAssistController : SimulatedAssistController
{
    public double ProportionalGain { get; set; } = 1.0;

    protected override double ComputeDemand(ControllerMockState state)
    {
        if (state.BucketTargetDepth == null)
        {
            return 0.0;
        }

        var currentDepth = state.BucketPosition.Y;
        var error = state.BucketTargetDepth.Value - currentDepth;
        return ProportionalGain * error;
    }
}
