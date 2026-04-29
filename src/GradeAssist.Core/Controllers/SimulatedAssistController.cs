namespace GradeAssist.Core.Controllers;

public abstract class SimulatedAssistController
{
    public ControllerState State { get; protected set; } = ControllerState.Locked;
    public ControllerOutput LastOutput { get; protected set; } = ControllerOutput.Zero;

    public double OutputClampMin { get; set; } = -1.0;
    public double OutputClampMax { get; set; } = 1.0;
    public double RateLimitPerTick { get; set; } = 0.5;
    public double ManualOverrideThreshold { get; set; } = 0.1;
    public double WatchdogTimeoutMs { get; set; } = 500.0;

    public bool IsFaulted => State == ControllerState.Fault;
    public bool IsOverridden => State == ControllerState.Override;
    public bool IsActive => State == ControllerState.Active;

    public ControllerOutput Update(ControllerMockState state)
    {
        try
        {
            var output = UpdateInternal(state);
            LastOutput = output;
            return output;
        }
        catch (Exception)
        {
            State = ControllerState.Fault;
            var safe = new ControllerOutput(0.0, ControllerState.Fault);
            LastOutput = safe;
            return safe;
        }
    }

    private ControllerOutput UpdateInternal(ControllerMockState state)
    {
        if (state.EmergencyDisable)
        {
            State = ControllerState.Fault;
            return new ControllerOutput(0.0, ControllerState.Fault);
        }

        if (!state.ConfigValid)
        {
            State = ControllerState.Fault;
            return new ControllerOutput(0.0, ControllerState.Fault);
        }

        var elapsedMs = (state.CurrentTime - state.LastTelemetryTimestamp).TotalMilliseconds;
        if (elapsedMs > WatchdogTimeoutMs)
        {
            State = ControllerState.Fault;
            return new ControllerOutput(0.0, ControllerState.Fault);
        }

        if (state.Reset && State == ControllerState.Fault)
        {
            State = ControllerState.Locked;
            return new ControllerOutput(0.0, ControllerState.Locked);
        }

        if (state.ManualInputMagnitude > ManualOverrideThreshold)
        {
            if (State != ControllerState.Fault)
            {
                State = ControllerState.Override;
            }
            return new ControllerOutput(0.0, State);
        }

        if (State == ControllerState.Override && state.ManualInputMagnitude <= ManualOverrideThreshold)
        {
            State = ControllerState.Armed;
        }

        RunStateMachine(state);

        if (State == ControllerState.Locked || State == ControllerState.Ready || State == ControllerState.Fault)
        {
            return new ControllerOutput(0.0, State);
        }

        var demand = ComputeDemand(state);
        var clamped = Clamp(demand, OutputClampMin, OutputClampMax);
        var rateLimited = ApplyRateLimit(clamped);

        return new ControllerOutput(rateLimited, State);
    }

    private void RunStateMachine(ControllerMockState state)
    {
        bool changed;
        do
        {
            changed = false;
            switch (State)
            {
                case ControllerState.Locked:
                    if (state.Enabled)
                    {
                        State = ControllerState.Ready;
                        changed = true;
                    }
                    break;

                case ControllerState.Ready:
                    if (!state.Enabled)
                    {
                        State = ControllerState.Locked;
                        changed = true;
                    }
                    else if (state.Armed)
                    {
                        State = ControllerState.Armed;
                        changed = true;
                    }
                    break;

                case ControllerState.Armed:
                    if (!state.Enabled)
                    {
                        State = ControllerState.Locked;
                        changed = true;
                    }
                    else if (!state.Armed)
                    {
                        State = ControllerState.Ready;
                        changed = true;
                    }
                    else if (state.Active)
                    {
                        State = ControllerState.Active;
                        changed = true;
                    }
                    break;

                case ControllerState.Active:
                    if (!state.Enabled)
                    {
                        State = ControllerState.Locked;
                        changed = true;
                    }
                    else if (!state.Armed)
                    {
                        State = ControllerState.Ready;
                        changed = true;
                    }
                    else if (!state.Active)
                    {
                        State = ControllerState.Armed;
                        changed = true;
                    }
                    break;
            }
        } while (changed);
    }

    private double ApplyRateLimit(double value)
    {
        if (!double.IsFinite(LastOutput.Value))
        {
            return value;
        }

        var delta = value - LastOutput.Value;
        if (Math.Abs(delta) > RateLimitPerTick)
        {
            delta = Math.Sign(delta) * RateLimitPerTick;
        }
        return LastOutput.Value + delta;
    }

    protected abstract double ComputeDemand(ControllerMockState state);

    protected static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
