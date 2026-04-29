namespace GradeAssist.Core;

public readonly record struct Vector3D(double X, double Y, double Z)
{
    public static readonly Vector3D Zero = new(0, 0, 0);
    public static readonly Vector3D Forward = new(0, 0, 1);

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);

    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3D FlattenXZ() => new(X, 0, Z);

    public double MagnitudeSquared => X * X + Y * Y + Z * Z;

    public double Magnitude => Math.Sqrt(MagnitudeSquared);

    public Vector3D NormalizeOr(Vector3D fallback)
    {
        var mag = Magnitude;
        if (!double.IsFinite(mag) || mag < 1e-9)
        {
            return fallback;
        }
        return new Vector3D(X / mag, Y / mag, Z / mag);
    }

    public bool IsFinite() => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
}
