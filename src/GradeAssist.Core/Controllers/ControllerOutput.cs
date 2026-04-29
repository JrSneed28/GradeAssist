namespace GradeAssist.Core.Controllers;

public readonly record struct ControllerOutput(double Value, ControllerState State)
{
    public static readonly ControllerOutput Zero = new(0.0, ControllerState.Locked);
}
