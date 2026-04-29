using System.Text.Json;

namespace GradeAssist.Core;

public static class ConfigValidator
{
    public static ValidationResult ValidateGradeTargetJson(string json)
    {
        return ValidateJson<GradeTargetSettingsDto>(json, dto =>
        {
            if (!dto.BenchmarkPoint.IsFinite())
            {
                throw new ArgumentException("benchmarkPoint contains non-finite values.");
            }

            if (!dto.GradeDirectionXZ.IsFinite())
            {
                throw new ArgumentException("gradeDirectionXZ contains non-finite values.");
            }

            if (Math.Abs(dto.GradeDirectionXZ.Y) > 1e-9)
            {
                throw new ArgumentException("gradeDirectionXZ.Y must be 0 (flattened direction vector).");
            }

            var settings = new GradeTargetSettings(
                dto.TargetCutDepthMeters,
                dto.SlopePercent,
                dto.CrossSlopePercent,
                dto.ToleranceMeters);
            settings.Validate();
        });
    }

    public static ValidationResult ValidateMachineJson(string json)
    {
        return ValidateJson<MachineConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateSafetyPolicyJson(string json)
    {
        return ValidateJson<SafetyPolicyConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateAssistTuningJson(string json)
    {
        return ValidateJson<AssistTuningConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateKeybindsJson(string json)
    {
        return ValidateJson<KeybindsConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateUnitySettingsJson(string json)
    {
        return ValidateJson<UnitySettingsConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateMountProfilesJson(string json)
    {
        return ValidateJson<MountProfilesConfig>(json, config => config.Validate());
    }

    public static ValidationResult ValidateRigMapsJson(string json)
    {
        return ValidateJson<RigMapsConfig>(json, config => config.Validate());
    }

    private static ValidationResult ValidateJson<T>(string json, Action<T> validate) where T : notnull
    {
        try
        {
            var config = ConfigJsonSerializer.Deserialize<T>(json);
            validate(config);
            return ValidationResult.Ok();
        }
        catch (JsonException ex)
        {
            return ValidationResult.Fail($"JSON parse error: {ex.Message}");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ValidationResult.Fail($"Validation error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return ValidationResult.Fail($"Validation error: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return ValidationResult.Fail($"Validation error: {ex.Message}");
        }
        catch (NullReferenceException ex)
        {
            return ValidationResult.Fail($"Validation error: {ex.Message}");
        }
    }

    private sealed record GradeTargetSettingsDto(
        Vector3D BenchmarkPoint,
        Vector3D GradeDirectionXZ,
        double TargetCutDepthMeters,
        double SlopePercent,
        double CrossSlopePercent,
        double ToleranceMeters);
}

public sealed record ValidationResult(bool IsValid, string Message)
{
    public static ValidationResult Ok() => new(true, string.Empty);
    public static ValidationResult Fail(string message) => new(false, message);
}
