using UnityEngine;

namespace GradeAssist.Core
{
    public readonly struct GradeError
    {
        public readonly Vector3D ReferencePoint;
        public readonly float TargetY;
        public readonly float ErrorMeters;
        public readonly float ToleranceMeters;

        public GradeStatus Status
        {
            get
            {
                if (Mathf.Abs(ErrorMeters) <= ToleranceMeters)
                {
                    return GradeStatus.OnGrade;
                }
                else if (ErrorMeters > 0)
                {
                    return GradeStatus.AboveGrade;
                }
                else
                {
                    return GradeStatus.BelowGrade;
                }
            }
        }

        public GradeError(Vector3D referencePoint, float targetY, float errorMeters, float toleranceMeters)
        {
            ReferencePoint = referencePoint;
            TargetY = targetY;
            ErrorMeters = errorMeters;
            ToleranceMeters = toleranceMeters;
        }

        public bool Equals(GradeError other)
        {
            return ReferencePoint.Equals(other.ReferencePoint) &&
                   Mathf.Approximately(TargetY, other.TargetY) &&
                   Mathf.Approximately(ErrorMeters, other.ErrorMeters) &&
                   Mathf.Approximately(ToleranceMeters, other.ToleranceMeters);
        }

        public override bool Equals(object obj)
        {
            return obj is GradeError other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = ReferencePoint.GetHashCode();
            hash = hash * 31 + TargetY.GetHashCode();
            hash = hash * 31 + ErrorMeters.GetHashCode();
            hash = hash * 31 + ToleranceMeters.GetHashCode();
            return hash;
        }
    }
}
