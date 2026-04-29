namespace GradeAssist.Core;

public sealed record MachineConfig(
    string MachineId,
    string DisplayName,
    string? CuttingEdgeReferencePath = null,
    string? MonitorMountPath = null,
    string? Notes = null)
{
    public const int MaxMachineIdLength = 64;
    public const int MaxDisplayNameLength = 128;
    public const int MaxPathLength = 256;
    public const int MaxNotesLength = 512;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MachineId))
        {
            throw new ArgumentException("machineId must not be empty.", nameof(MachineId));
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new ArgumentException("displayName must not be empty.", nameof(DisplayName));
        }

        if (MachineId.Length > MaxMachineIdLength)
        {
            throw new ArgumentException($"machineId exceeds {MaxMachineIdLength} characters.", nameof(MachineId));
        }

        if (DisplayName.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException($"displayName exceeds {MaxDisplayNameLength} characters.", nameof(DisplayName));
        }

        if (MachineId.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            throw new ArgumentException("machineId must contain only letters, digits, and underscores.", nameof(MachineId));
        }

        if (!string.IsNullOrEmpty(CuttingEdgeReferencePath) && CuttingEdgeReferencePath.Length > MaxPathLength)
        {
            throw new ArgumentException($"cuttingEdgeReferencePath exceeds {MaxPathLength} characters.", nameof(CuttingEdgeReferencePath));
        }

        if (!string.IsNullOrEmpty(MonitorMountPath) && MonitorMountPath.Length > MaxPathLength)
        {
            throw new ArgumentException($"monitorMountPath exceeds {MaxPathLength} characters.", nameof(MonitorMountPath));
        }

        if (!string.IsNullOrEmpty(Notes) && Notes.Length > MaxNotesLength)
        {
            throw new ArgumentException($"notes exceeds {MaxNotesLength} characters.", nameof(Notes));
        }
    }
}
