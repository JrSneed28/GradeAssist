namespace GradeAssist.Core;

public sealed record MountProfilesConfig(IReadOnlyList<MountProfile> Profiles)
{
    public void Validate()
    {
        if (Profiles is null || Profiles.Count == 0)
        {
            throw new ArgumentException("profiles must contain at least one entry.");
        }

        foreach (var profile in Profiles)
        {
            profile.Validate();
        }
    }
}

public sealed record MountProfile(
    string MachineId,
    string DisplayName,
    string AttachPath,
    IReadOnlyList<double> LocalPosition,
    IReadOnlyList<double> LocalRotationEuler,
    IReadOnlyList<double> LocalScale,
    IReadOnlyList<int> ScreenResolution,
    string ScreenObjectName)
{
    public const int MaxNameLength = 128;
    public const int MaxPathLength = 256;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MachineId))
        {
            throw new ArgumentException("machineId must not be empty.", nameof(MachineId));
        }

        if (MachineId.Length > 64)
        {
            throw new ArgumentException("machineId exceeds 64 characters.", nameof(MachineId));
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new ArgumentException("displayName must not be empty.", nameof(DisplayName));
        }

        if (DisplayName.Length > MaxNameLength)
        {
            throw new ArgumentException($"displayName exceeds {MaxNameLength} characters.", nameof(DisplayName));
        }

        if (string.IsNullOrWhiteSpace(AttachPath) || AttachPath.Length > MaxPathLength)
        {
            throw new ArgumentException($"attachPath must not be empty and must not exceed {MaxPathLength} characters.", nameof(AttachPath));
        }

        if (LocalPosition is null || LocalPosition.Count != 3)
        {
            throw new ArgumentException("localPosition must contain exactly 3 values.", nameof(LocalPosition));
        }

        if (LocalRotationEuler is null || LocalRotationEuler.Count != 3)
        {
            throw new ArgumentException("localRotationEuler must contain exactly 3 values.", nameof(LocalRotationEuler));
        }

        if (LocalScale is null || LocalScale.Count != 3)
        {
            throw new ArgumentException("localScale must contain exactly 3 values.", nameof(LocalScale));
        }

        if (ScreenResolution is null || ScreenResolution.Count != 2)
        {
            throw new ArgumentException("screenResolution must contain exactly 2 values.", nameof(ScreenResolution));
        }

        if (string.IsNullOrWhiteSpace(ScreenObjectName))
        {
            throw new ArgumentException("screenObjectName must not be empty.", nameof(ScreenObjectName));
        }

        if (ScreenObjectName.Length > MaxNameLength)
        {
            throw new ArgumentException($"screenObjectName exceeds {MaxNameLength} characters.", nameof(ScreenObjectName));
        }
    }
}
