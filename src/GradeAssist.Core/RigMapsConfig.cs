namespace GradeAssist.Core;

public sealed record RigMapsConfig(IReadOnlyList<RigMap> Rigs)
{
    public void Validate()
    {
        if (Rigs is null || Rigs.Count == 0)
        {
            throw new ArgumentException("rigs must contain at least one entry.");
        }

        foreach (var rig in Rigs)
        {
            rig.Validate();
        }
    }
}

public sealed record RigMap(
    string MachineId,
    string Root,
    string Boom,
    string Stick,
    string Bucket,
    string CuttingEdgeReference)
{
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

        ValidatePath(nameof(Root), Root);
        ValidatePath(nameof(Boom), Boom);
        ValidatePath(nameof(Stick), Stick);
        ValidatePath(nameof(Bucket), Bucket);
        ValidatePath(nameof(CuttingEdgeReference), CuttingEdgeReference);
    }

    private static void ValidatePath(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxPathLength)
        {
            throw new ArgumentException($"{name} must not be empty and must not exceed {MaxPathLength} characters.", name);
        }
    }
}
