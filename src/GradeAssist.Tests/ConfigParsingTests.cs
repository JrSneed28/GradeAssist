using System.Text.Json;
using GradeAssist.Core;
using Xunit;

namespace GradeAssist.Tests;

public class ConfigParsingTests
{
    private const string ValidMachineJson = """
        {
          "machineId": "mock_excavator_01",
          "displayName": "Mock Excavator",
          "cuttingEdgeReferencePath": "MockExcavator/Boom/Stick/Bucket/CuttingEdgeReference",
          "monitorMountPath": "MockExcavator/Cab/MonitorMount",
          "notes": "External Unity simulator only."
        }
        """;

    private const string ValidGradeTargetJson = """
        {
          "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
          "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
          "targetCutDepthMeters": 1.5,
          "slopePercent": 2.0,
          "crossSlopePercent": 0.0,
          "toleranceMeters": 0.03
        }
        """;

    private const string ValidSafetyPolicyJson = """
        {
          "gameFolderWritesAllowed": false,
          "runtimeInjectionAllowed": false,
          "antiTamperBypassAllowed": false,
          "prohibitedPaths": ["C:/XboxGames/Construction Simulator"],
          "prohibitedTerms": ["anti-tamper bypass"]
        }
        """;

    private const string ValidAssistTuningJson = """
        {
          "simOnly": true,
          "controllersLockedByDefault": true,
          "maxOutput": 0.35,
          "maxOutputChangePerSecond": 0.75,
          "manualOverrideThreshold": 0.15,
          "watchdogTimeoutSeconds": 0.25
        }
        """;

    [Fact]
    public void MachineConfig_ParsesSuccessfully()
    {
        var config = ConfigJsonSerializer.Deserialize<MachineConfig>(ValidMachineJson);
        Assert.Equal("mock_excavator_01", config.MachineId);
        Assert.Equal("Mock Excavator", config.DisplayName);
        Assert.Equal("MockExcavator/Boom/Stick/Bucket/CuttingEdgeReference", config.CuttingEdgeReferencePath);
        Assert.Equal("MockExcavator/Cab/MonitorMount", config.MonitorMountPath);
        Assert.Equal("External Unity simulator only.", config.Notes);
    }

    [Fact]
    public void MachineConfig_ValidationPasses()
    {
        var config = ConfigJsonSerializer.Deserialize<MachineConfig>(ValidMachineJson);
        config.Validate();
    }

    [Fact]
    public void MachineConfig_RejectEmptyMachineId()
    {
        var config = new MachineConfig("", "Mock");
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void MachineConfig_RejectInvalidMachineIdChars()
    {
        var config = new MachineConfig("mock-excavator-01", "Mock");
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void MachineConfig_RejectLongMachineId()
    {
        var config = new MachineConfig(new string('a', MachineConfig.MaxMachineIdLength + 1), "Mock");
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void MachineConfig_RejectEmptyDisplayName()
    {
        var config = new MachineConfig("mock_01", "");
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void MachineConfig_RejectLongPath()
    {
        var config = new MachineConfig("mock_01", "Mock", new string('a', MachineConfig.MaxPathLength + 1));
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void MachineConfig_RejectLongNotes()
    {
        var config = new MachineConfig("mock_01", "Mock", Notes: new string('a', MachineConfig.MaxNotesLength + 1));
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_ParsesSuccessfully()
    {
        var result = ConfigValidator.ValidateGradeTargetJson(ValidGradeTargetJson);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void GradeTarget_RejectNegativeCutDepth()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": -1.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectSlopeOver500()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 501.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectNegativeTolerance()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": -0.01
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectInvalidJson()
    {
        var result = ConfigValidator.ValidateGradeTargetJson("{ bad json");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteBenchmarkPoint()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": "NaN", "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SafetyPolicy_ParsesSuccessfully()
    {
        var config = ConfigJsonSerializer.Deserialize<SafetyPolicyConfig>(ValidSafetyPolicyJson);
        Assert.False(config.GameFolderWritesAllowed);
        Assert.False(config.RuntimeInjectionAllowed);
        Assert.False(config.AntiTamperBypassAllowed);
        Assert.Equal("C:/XboxGames/Construction Simulator", config.ProhibitedPaths![0]);
    }

    [Fact]
    public void SafetyPolicy_ValidationPasses()
    {
        var config = ConfigJsonSerializer.Deserialize<SafetyPolicyConfig>(ValidSafetyPolicyJson);
        config.Validate();
    }

    [Fact]
    public void SafetyPolicy_EnforcesRuntimeInjectionDisabled()
    {
        var json = """
            {
              "gameFolderWritesAllowed": false,
              "runtimeInjectionAllowed": true,
              "antiTamperBypassAllowed": false,
              "prohibitedPaths": [],
              "prohibitedTerms": []
            }
            """;
        var result = ConfigValidator.ValidateSafetyPolicyJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("runtimeInjectionAllowed must be false", result.Message);
    }

    [Fact]
    public void SafetyPolicy_EnforcesAntiTamperBypassDisabled()
    {
        var json = """
            {
              "gameFolderWritesAllowed": false,
              "runtimeInjectionAllowed": false,
              "antiTamperBypassAllowed": true,
              "prohibitedPaths": [],
              "prohibitedTerms": []
            }
            """;
        var result = ConfigValidator.ValidateSafetyPolicyJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("antiTamperBypassAllowed must be false", result.Message);
    }

    [Fact]
    public void SafetyPolicy_EnforcesGameFolderWritesDisabled()
    {
        var json = """
            {
              "gameFolderWritesAllowed": true,
              "runtimeInjectionAllowed": false,
              "antiTamperBypassAllowed": false,
              "prohibitedPaths": [],
              "prohibitedTerms": []
            }
            """;
        var result = ConfigValidator.ValidateSafetyPolicyJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("gameFolderWritesAllowed must be false", result.Message);
    }

    [Fact]
    public void SafetyPolicy_RejectEmptyProhibitedPaths()
    {
        var config = new SafetyPolicyConfig(false, false, false, new[] { "" }, new[] { "term" });
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void SafetyPolicy_RejectEmptyProhibitedTerms()
    {
        var config = new SafetyPolicyConfig(false, false, false, new[] { "path" }, new[] { "" });
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_ParsesSuccessfully()
    {
        var config = ConfigJsonSerializer.Deserialize<AssistTuningConfig>(ValidAssistTuningJson);
        Assert.True(config.SimOnly);
        Assert.True(config.ControllersLockedByDefault);
        Assert.Equal(0.35, config.MaxOutput);
        Assert.Equal(0.75, config.MaxOutputChangePerSecond);
        Assert.Equal(0.15, config.ManualOverrideThreshold);
        Assert.Equal(0.25, config.WatchdogTimeoutSeconds);
    }

    [Fact]
    public void AssistTuning_ValidationPasses()
    {
        var config = ConfigJsonSerializer.Deserialize<AssistTuningConfig>(ValidAssistTuningJson);
        config.Validate();
    }

    [Fact]
    public void AssistTuning_RejectNegativeMaxOutput()
    {
        var config = new AssistTuningConfig(true, true, -0.1, 0.5, 0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectMaxOutputOverOne()
    {
        var config = new AssistTuningConfig(true, true, 1.1, 0.5, 0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNegativeWatchdogTimeout()
    {
        var json = """
            {
              "simOnly": true,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": -0.1
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AssistTuning_RejectSimOnlyFalse()
    {
        var json = """
            {
              "simOnly": false,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": 0.25
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("simOnly must be true", result.Message);
    }

    [Fact]
    public void GradeTarget_RejectSlopeExactlyOver500()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 500.0000001,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectNegativeSlopeOver500()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": -501.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectCrossSlopeOver500()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 501.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectNegativeCrossSlopeOver500()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": -501.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_SlopeAt500BoundaryPasses()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 500.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void GradeTarget_RejectBenchmarkPointInfinity()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": "Infinity", "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectGradeDirectionXZInfinity()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": "Infinity", "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectGradeDirectionXZNan()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": "NaN", "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GradeTarget_RejectGradeDirectionXZYTooLarge()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.000000002, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("gradeDirectionXZ.Y must be 0", result.Message);
    }

    [Fact]
    public void GradeTarget_SlopeAtMinus500BoundaryPasses()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": -500.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void AssistTuning_RejectWatchdogTimeoutOverFive()
    {
        var json = """
            {
              "simOnly": true,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": 5.0000001
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AssistTuning_RejectWatchdogTimeoutBelowOneCentisecond()
    {
        var json = """
            {
              "simOnly": true,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": 0.009999
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AssistTuning_WatchdogTimeoutAtUpperBoundaryPasses()
    {
        var json = """
            {
              "simOnly": true,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": 5.0
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void AssistTuning_WatchdogTimeoutAtLowerBoundaryPasses()
    {
        var json = """
            {
              "simOnly": true,
              "controllersLockedByDefault": true,
              "maxOutput": 0.35,
              "maxOutputChangePerSecond": 0.75,
              "manualOverrideThreshold": 0.15,
              "watchdogTimeoutSeconds": 0.01
            }
            """;
        var result = ConfigValidator.ValidateAssistTuningJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void AssistTuning_RejectNonFiniteMaxOutput()
    {
        var config = new AssistTuningConfig(true, true, double.PositiveInfinity, 0.5, 0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNonFiniteWatchdogTimeout()
    {
        var config = new AssistTuningConfig(true, true, 0.5, 0.5, 0.1, double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNegativeMaxOutputChangePerSecond()
    {
        var config = new AssistTuningConfig(true, true, 0.5, -0.1, 0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNonFiniteMaxOutputChangePerSecond()
    {
        var config = new AssistTuningConfig(true, true, 0.5, double.NegativeInfinity, 0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNegativeManualOverrideThreshold()
    {
        var config = new AssistTuningConfig(true, true, 0.5, 0.5, -0.1, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void AssistTuning_RejectNonFiniteManualOverrideThreshold()
    {
        var config = new AssistTuningConfig(true, true, 0.5, 0.5, double.PositiveInfinity, 0.25);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteTargetCutDepthMeters()
    {
        var config = new GradeTargetSettings(double.NaN, 0.0, 0.0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteSlopePercent()
    {
        var config = new GradeTargetSettings(0.0, double.PositiveInfinity, 0.0, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteCrossSlopePercent()
    {
        var config = new GradeTargetSettings(0.0, 0.0, double.NegativeInfinity, 0.03);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteToleranceMeters()
    {
        var config = new GradeTargetSettings(0.0, 0.0, 0.0, double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_CrossSlopeAtMinus500BoundaryPasses()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": -500.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void GradeTarget_CrossSlopeAt500BoundaryPasses()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 500.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void ConfigValidator_RejectInvalidJsonSyntax()
    {
        var result = ConfigValidator.ValidateMachineJson("not json at all");
        Assert.False(result.IsValid);
        Assert.Contains("JSON parse error", result.Message);
    }

    [Fact]
    public void ConfigValidator_RejectMissingRequiredFields()
    {
        var json = """{ "machineId": "test" }""";
        var result = ConfigValidator.ValidateMachineJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("displayName", result.Message);
    }

    [Fact]
    public void ConfigValidator_RejectUnknownMachineConfigProperties()
    {
        var json = """
            {
              "machineId": "test_01",
              "displayName": "Test",
              "verifiedGamePath": true
            }
            """;
        var result = ConfigValidator.ValidateMachineJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ConfigValidator_RejectUnknownGradeTargetProperties()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 0.0, "z": 1.0 },
              "targetCutDepthMeters": 1.5,
              "slopePercent": 2.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03,
              "extraField": "nope"
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MachineConfig_MissingOptionalFieldsStillValid()
    {
        var json = """{ "machineId": "test_01", "displayName": "Test" }""";
        var config = ConfigJsonSerializer.Deserialize<MachineConfig>(json);
        config.Validate();
        Assert.Null(config.CuttingEdgeReferencePath);
        Assert.Null(config.MonitorMountPath);
    }

    [Fact]
    public void AssistTuning_DefaultsAreSafe()
    {
        var config = new AssistTuningConfig();
        Assert.True(config.SimOnly);
        Assert.True(config.ControllersLockedByDefault);
        Assert.Equal(0.0, config.MaxOutput);
        Assert.Equal(0.0, config.MaxOutputChangePerSecond);
        Assert.Equal(0.0, config.ManualOverrideThreshold);
        Assert.Equal(1.0, config.WatchdogTimeoutSeconds);
    }

    [Fact]
    public void MachineConfig_RejectLongDisplayName()
    {
        var config = new MachineConfig("mock_01", new string('a', MachineConfig.MaxDisplayNameLength + 1));
        Assert.Throws<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void GradeTarget_RejectNonFiniteGradeDirectionXZ()
    {
        var json = """
            {
              "benchmarkPoint": { "x": 0.0, "y": 10.0, "z": 0.0 },
              "gradeDirectionXZ": { "x": 0.0, "y": 1.0, "z": 0.0 },
              "targetCutDepthMeters": 0.0,
              "slopePercent": 0.0,
              "crossSlopePercent": 0.0,
              "toleranceMeters": 0.03
            }
            """;
        var result = ConfigValidator.ValidateGradeTargetJson(json);
        Assert.False(result.IsValid);
        Assert.Contains("gradeDirectionXZ.Y must be 0", result.Message);
    }

    [Fact]
    public void KeybindsConfig_ParsesSuccessfully()
    {
        var json = """
            {
              "setBenchmark": "B",
              "moveForward": "UpArrow",
              "moveBackward": "DownArrow",
              "moveLeft": "LeftArrow",
              "moveRight": "RightArrow",
              "moveUp": "PageUp",
              "moveDown": "PageDown",
              "emergencyDisable": "F10",
              "cyclePageForward": "M",
              "cyclePageBackward": "Shift+M",
              "pageWork": "F1",
              "pageTarget": "F2",
              "pageSystem": "F3",
              "settingsIncrement": "Equals",
              "settingsDecrement": "Minus",
              "settingsApply": "Return",
              "settingsCancel": "Escape"
            }
            """;
        var result = ConfigValidator.ValidateKeybindsJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void KeybindsConfig_RejectEmptyKey()
    {
        var json = """
            {
              "setBenchmark": "",
              "moveForward": "UpArrow",
              "moveBackward": "DownArrow",
              "moveLeft": "LeftArrow",
              "moveRight": "RightArrow",
              "moveUp": "PageUp",
              "moveDown": "PageDown",
              "emergencyDisable": "F10",
              "cyclePageForward": "M",
              "cyclePageBackward": "Shift+M",
              "pageWork": "F1",
              "pageTarget": "F2",
              "pageSystem": "F3",
              "settingsIncrement": "Equals",
              "settingsDecrement": "Minus",
              "settingsApply": "Return",
              "settingsCancel": "Escape"
            }
            """;
        var result = ConfigValidator.ValidateKeybindsJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UnitySettingsConfig_ParsesSuccessfully()
    {
        var json = """
            {
              "simulator": { "targetFps": 60, "timeScale": 1.0 },
              "renderTexture": { "width": 1024, "height": 600 },
              "ui": { "normalPrecisionMeters": 0.01, "diagnosticsPrecisionMeters": 0.001, "defaultOnGradeToleranceMeters": 0.03, "diagnosticsDefaultVisible": false },
              "controls": { "keyboardEnabled": true, "mouseEnabled": false, "gamepadEnabled": false },
              "safety": { "gameIntegrationEnabled": false, "allowExternalWrites": false, "blockedPaths": ["C:\\\\XboxGames\\\\Construction Simulator"] }
            }
            """;
        var result = ConfigValidator.ValidateUnitySettingsJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void UnitySettingsConfig_RejectGameIntegrationEnabled()
    {
        var json = """
            {
              "simulator": { "targetFps": 60, "timeScale": 1.0 },
              "renderTexture": { "width": 1024, "height": 600 },
              "ui": { "normalPrecisionMeters": 0.01, "diagnosticsPrecisionMeters": 0.001, "defaultOnGradeToleranceMeters": 0.03, "diagnosticsDefaultVisible": false },
              "controls": { "keyboardEnabled": true, "mouseEnabled": false, "gamepadEnabled": false },
              "safety": { "gameIntegrationEnabled": true, "allowExternalWrites": false, "blockedPaths": [] }
            }
            """;
        var result = ConfigValidator.ValidateUnitySettingsJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MountProfilesConfig_ParsesSuccessfully()
    {
        var json = """
            {
              "profiles": [
                {
                  "machineId": "mock_excavator_01",
                  "displayName": "Mock Excavator",
                  "attachPath": "MockExcavator/Cab/MonitorMount",
                  "localPosition": [0.42, 1.12, 0.78],
                  "localRotationEuler": [8.0, -18.0, 0.0],
                  "localScale": [1.0, 1.0, 1.0],
                  "screenResolution": [1024, 600],
                  "screenObjectName": "Monitor_Screen"
                }
              ]
            }
            """;
        var result = ConfigValidator.ValidateMountProfilesJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void MountProfilesConfig_RejectMissingField()
    {
        var json = """
            {
              "profiles": [
                {
                  "machineId": "mock_excavator_01",
                  "displayName": "Mock Excavator"
                }
              ]
            }
            """;
        var result = ConfigValidator.ValidateMountProfilesJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RigMapsConfig_ParsesSuccessfully()
    {
        var json = """
            {
              "rigs": [
                {
                  "machineId": "mock_excavator_01",
                  "root": "MockExcavator",
                  "boom": "MockExcavator/Boom",
                  "stick": "MockExcavator/Boom/Stick",
                  "bucket": "MockExcavator/Boom/Stick/Bucket",
                  "cuttingEdgeReference": "MockExcavator/Boom/Stick/Bucket/CuttingEdgeReference"
                }
              ]
            }
            """;
        var result = ConfigValidator.ValidateRigMapsJson(json);
        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void RigMapsConfig_RejectEmptyPath()
    {
        var json = """
            {
              "rigs": [
                {
                  "machineId": "mock_excavator_01",
                  "root": "",
                  "boom": "MockExcavator/Boom",
                  "stick": "MockExcavator/Boom/Stick",
                  "bucket": "MockExcavator/Boom/Stick/Bucket",
                  "cuttingEdgeReference": "MockExcavator/Boom/Stick/Bucket/CuttingEdgeReference"
                }
              ]
            }
            """;
        var result = ConfigValidator.ValidateRigMapsJson(json);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ConfigJsonSerializer_OptionsIsReadOnly()
    {
        Assert.Throws<InvalidOperationException>(() => ConfigJsonSerializer.Options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
    }
}
