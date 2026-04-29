namespace GradeAssist.Core;

public sealed record KeybindsConfig(
    string SetBenchmark,
    string MoveForward,
    string MoveBackward,
    string MoveLeft,
    string MoveRight,
    string MoveUp,
    string MoveDown,
    string EmergencyDisable,
    string CyclePageForward,
    string CyclePageBackward,
    string PageWork,
    string PageTarget,
    string PageSystem,
    string SettingsIncrement,
    string SettingsDecrement,
    string SettingsApply,
    string SettingsCancel)
{
    public const int MaxKeyNameLength = 64;

    public void Validate()
    {
        ValidateKey(nameof(SetBenchmark), SetBenchmark);
        ValidateKey(nameof(MoveForward), MoveForward);
        ValidateKey(nameof(MoveBackward), MoveBackward);
        ValidateKey(nameof(MoveLeft), MoveLeft);
        ValidateKey(nameof(MoveRight), MoveRight);
        ValidateKey(nameof(MoveUp), MoveUp);
        ValidateKey(nameof(MoveDown), MoveDown);
        ValidateKey(nameof(EmergencyDisable), EmergencyDisable);
        ValidateKey(nameof(CyclePageForward), CyclePageForward);
        ValidateKey(nameof(CyclePageBackward), CyclePageBackward);
        ValidateKey(nameof(PageWork), PageWork);
        ValidateKey(nameof(PageTarget), PageTarget);
        ValidateKey(nameof(PageSystem), PageSystem);
        ValidateKey(nameof(SettingsIncrement), SettingsIncrement);
        ValidateKey(nameof(SettingsDecrement), SettingsDecrement);
        ValidateKey(nameof(SettingsApply), SettingsApply);
        ValidateKey(nameof(SettingsCancel), SettingsCancel);
    }

    private static void ValidateKey(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} must not be empty.", name);
        }

        if (value.Length > MaxKeyNameLength)
        {
            throw new ArgumentException($"{name} exceeds {MaxKeyNameLength} characters.", name);
        }
    }
}
