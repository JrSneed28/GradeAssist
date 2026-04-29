using UnityEngine;

namespace GradeAssist.Core
{
    public readonly struct Vector3D
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D FlattenXZ() => new Vector3D(X, 0, Z);

        public Vector3D NormalizeOr(Vector3D fallback)
        {
            float mag = Magnitude;
            if (mag < 0.0001f)
            {
                return fallback;
            }
            return new Vector3D(X / mag, Y / mag, Z / mag);
        }

        public float Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        public static Vector3D operator +(Vector3D a, Vector3D b) =>
            new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3D operator -(Vector3D a, Vector3D b) =>
            new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3D operator *(Vector3D v, float scalar) =>
            new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3D operator /(Vector3D v, float scalar) =>
            new Vector3D(v.X / scalar, v.Y / scalar, v.Z / scalar);

        public static readonly Vector3D Forward = new Vector3D(0, 0, 1);

        public float Magnitude => Mathf.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalized => this / Magnitude;
    }
}
