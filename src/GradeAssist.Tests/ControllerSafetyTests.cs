using GradeAssist.Core;
using GradeAssist.Core.Controllers;
using Xunit;

namespace GradeAssist.Tests;

public class ControllerSafetyTests
{
    private static void Near(double actual, double expected, double tolerance = 1e-9)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new Exception($"Expected {expected}, got {actual}");
        }
    }

    private static ControllerMockState FreshState()
    {
        var now = DateTimeOffset.UtcNow;
        return new ControllerMockState
        {
            CurrentTime = now,
            LastTelemetryTimestamp = now,
            BucketPosition = new Vector3D(0, 10, 0),
            ManualInputMagnitude = 0.0,
            Enabled = false,
            Armed = false,
            Active = false,
            EmergencyDisable = false,
            Reset = false,
            ConfigValid = true
        };
    }

    private static void AdvanceToActive(ControllerMockState state)
    {
        state.Enabled = true;
        state.Armed = true;
        state.Active = true;
    }

    [Fact]
    public void ControllerStartsLocked()
    {
        var ctrl = new SimulatedGradeAssistController();
        Assert.Equal(ControllerState.Locked, ctrl.State);
        Assert.Equal(0.0, ctrl.LastOutput.Value);
    }

    [Fact]
    public void UpdateWithEmptyStateRemainsLocked()
    {
        var ctrl = new SimulatedGradeAssistController();
        var output = ctrl.Update(FreshState());
        Assert.Equal(ControllerState.Locked, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void LockedToReadyRequiresEnable()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        ctrl.Update(state);
        Assert.Equal(ControllerState.Locked, ctrl.State);

        state.Enabled = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Ready, ctrl.State);
    }

    [Fact]
    public void ReadyToArmedRequiresArmedFlag()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        state.Enabled = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Ready, ctrl.State);

        state.Armed = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Armed, ctrl.State);
    }

    [Fact]
    public void ArmedToActiveRequiresActiveFlag()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        state.Enabled = true;
        state.Armed = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Armed, ctrl.State);

        state.Active = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);
    }

    [Fact]
    public void SkipReadyToArmedRejected()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        state.Enabled = false;
        state.Armed = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Locked, ctrl.State);
    }

    [Fact]
    public void SkipArmedToActiveRejected()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        state.Enabled = true;
        state.Armed = false;
        state.Active = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Ready, ctrl.State);
    }

    [Fact]
    public void DisableReturnsToLocked()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        state.Enabled = false;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Locked, ctrl.State);
        Near(ctrl.LastOutput.Value, 0.0);
    }

    [Fact]
    public void ManualInputOverThresholdForcesOverride()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        state.ManualInputMagnitude = 0.5;
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Override, output.State);
    }

    [Fact]
    public void ManualInputBelowThresholdReturnsToArmed()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        state.ManualInputMagnitude = 0.5;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Override, ctrl.State);

        state.ManualInputMagnitude = 0.0;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);
    }

    [Fact]
    public void OverrideOutputIsZero()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);

        state.ManualInputMagnitude = 0.5;
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Override, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void StaleTelemetryFaultsController()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        state.LastTelemetryTimestamp = state.CurrentTime.AddSeconds(-1);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void WatchdogTimeoutMsIsConfigurable()
    {
        var ctrl = new SimulatedGradeAssistController { WatchdogTimeoutMs = 200.0 };
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);

        state.LastTelemetryTimestamp = state.CurrentTime.AddMilliseconds(-250);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, output.State);
    }

    [Fact]
    public void FreshTelemetryWithinTimeoutDoesNotFault()
    {
        var ctrl = new SimulatedGradeAssistController { WatchdogTimeoutMs = 500.0 };
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);

        state.LastTelemetryTimestamp = state.CurrentTime.AddMilliseconds(-100);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
    }

    [Fact]
    public void OutputClampMinWorks()
    {
        var ctrl = new SimulatedGradeAssistController
        {
            OutputClampMin = -0.5,
            OutputClampMax = 0.5,
            ProportionalGain = 10.0
        };
        var state = FreshState();
        state.GradePlane = new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(0, 0, 0, 0.03));
        AdvanceToActive(state);
        state.BucketPosition = new Vector3D(0, 20, 0);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Assert.True(output.Value <= 0.5);
    }

    [Fact]
    public void OutputClampMaxWorks()
    {
        var ctrl = new SimulatedGradeAssistController
        {
            OutputClampMin = -0.5,
            OutputClampMax = 0.5,
            ProportionalGain = 10.0
        };
        var state = FreshState();
        state.GradePlane = new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(0, 0, 0, 0.03));
        AdvanceToActive(state);
        state.BucketPosition = new Vector3D(0, 0, 0);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Assert.True(output.Value >= -0.5);
    }

    [Fact]
    public void RateLimitConstrainsLargeChanges()
    {
        var ctrl = new SimulatedGradeAssistController
        {
            RateLimitPerTick = 0.1,
            ProportionalGain = 10.0
        };
        var state = FreshState();
        state.GradePlane = new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(0, 0, 0, 0.03));
        AdvanceToActive(state);
        state.BucketPosition = new Vector3D(0, 15, 0);
        var output1 = ctrl.Update(state);
        state.BucketPosition = new Vector3D(0, 5, 0);
        var output2 = ctrl.Update(state);
        var delta = Math.Abs(output2.Value - output1.Value);
        Assert.True(delta <= 0.1 + 1e-9, $"Rate limit violated: delta={delta}");
    }

    [Fact]
    public void EmergencyDisableForcesFault()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        state.EmergencyDisable = true;
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void EmergencyDisablePersistsUntilReset()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);

        state.EmergencyDisable = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, ctrl.State);

        state.EmergencyDisable = false;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, ctrl.State);
    }

    [Fact]
    public void ResetClearsFault()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);

        state.EmergencyDisable = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, ctrl.State);

        state.EmergencyDisable = false;
        state.Reset = true;
        ctrl.Update(state);
        Assert.Equal(ControllerState.Locked, ctrl.State);
    }

    [Fact]
    public void BadConfigForcesFault()
    {
        var ctrl = new SimulatedGradeAssistController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        state.ConfigValid = false;
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void ExceptionInComputeDemandSetsFaultAndZeroOutput()
    {
        var ctrl = new ThrowingController();
        var state = FreshState();
        AdvanceToActive(state);
        ctrl.Update(state);
        Assert.Equal(ControllerState.Active, ctrl.State);

        ctrl.ThrowNext = true;
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Fault, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void BucketAssistStartsLocked()
    {
        var ctrl = new SimulatedBucketAssistController();
        Assert.Equal(ControllerState.Locked, ctrl.State);
    }

    [Fact]
    public void SwingAssistStartsLocked()
    {
        var ctrl = new SimulatedSwingAssistController();
        Assert.Equal(ControllerState.Locked, ctrl.State);
    }

    [Fact]
    public void EFenceStartsLocked()
    {
        var ctrl = new SimulatedEFenceController();
        Assert.Equal(ControllerState.Locked, ctrl.State);
    }

    [Fact]
    public void BucketAssistProducesDemandWhenActive()
    {
        var ctrl = new SimulatedBucketAssistController { ProportionalGain = 1.0, RateLimitPerTick = 100.0, OutputClampMin = -10.0, OutputClampMax = 10.0 };
        var state = FreshState();
        state.BucketTargetDepth = 5.0;
        state.BucketPosition = new Vector3D(0, 8, 0);
        AdvanceToActive(state);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Near(output.Value, -3.0);
    }

    [Fact]
    public void GradeAssistProducesDemandWhenActive()
    {
        var ctrl = new SimulatedGradeAssistController { ProportionalGain = 1.0, RateLimitPerTick = 100.0, OutputClampMin = -10.0, OutputClampMax = 10.0 };
        var state = FreshState();
        state.GradePlane = new GradePlane(
            new Vector3D(0, 10, 0),
            Vector3D.Forward,
            new GradeTargetSettings(0, 0, 0, 0.03));
        state.BucketPosition = new Vector3D(0, 12, 0);
        AdvanceToActive(state);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Near(output.Value, -2.0);
    }

    [Fact]
    public void SwingAssistProducesDemandWhenActive()
    {
        var ctrl = new SimulatedSwingAssistController { ProportionalGain = 1.0 };
        var state = FreshState();
        state.SwingCenter = new Vector3D(0, 0, 0);
        state.SwingRadius = 10.0;
        state.SwingAngleDegrees = 0.0;
        state.BucketPosition = new Vector3D(0, 0, 12);
        AdvanceToActive(state);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Assert.True(output.Value < 0.0);
    }

    [Fact]
    public void EFenceZeroInsideRadius()
    {
        var ctrl = new SimulatedEFenceController { ProportionalGain = 2.0 };
        var state = FreshState();
        state.FenceCenter = new Vector3D(0, 0, 0);
        state.FenceRadius = 10.0;
        state.BucketPosition = new Vector3D(0, 0, 5);
        AdvanceToActive(state);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Near(output.Value, 0.0);
    }

    [Fact]
    public void EFenceNegativeOutsideRadius()
    {
        var ctrl = new SimulatedEFenceController { ProportionalGain = 2.0 };
        var state = FreshState();
        state.FenceCenter = new Vector3D(0, 0, 0);
        state.FenceRadius = 10.0;
        state.BucketPosition = new Vector3D(0, 0, 15);
        AdvanceToActive(state);
        var output = ctrl.Update(state);
        Assert.Equal(ControllerState.Active, output.State);
        Assert.True(output.Value < 0.0);
    }

    [Fact]
    public void AllControllersLockedByDefault()
    {
        Assert.Equal(ControllerState.Locked, new SimulatedBucketAssistController().State);
        Assert.Equal(ControllerState.Locked, new SimulatedGradeAssistController().State);
        Assert.Equal(ControllerState.Locked, new SimulatedSwingAssistController().State);
        Assert.Equal(ControllerState.Locked, new SimulatedEFenceController().State);
    }

    private sealed class ThrowingController : SimulatedAssistController
    {
        public bool ThrowNext { get; set; }

        protected override double ComputeDemand(ControllerMockState state)
        {
            if (ThrowNext)
            {
                throw new InvalidOperationException("Simulated fault");
            }
            return 0.0;
        }
    }
}
