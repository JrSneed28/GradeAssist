using UnityEngine;

namespace GradeAssist.Core
{
    public sealed class GradePlane
    {
        public Vector3D BenchmarkPoint;
        public Vector3D GradeDirectionXZ;
        public GradeTargetSettings Settings;

        public GradePlane(Vector3D benchmarkPoint, Vector3D gradeDirectionXZ, GradeTargetSettings settings)
        {
            settings.Validate();
            BenchmarkPoint = benchmarkPoint;
            GradeDirectionXZ = gradeDirectionXZ.FlattenXZ().NormalizeOr(Vector3D.Forward);
            Settings = settings;
        }

        public float HeightAt(Vector3D worldPoint)
        {
            Vector3D gradeDir = GradeDirectionXZ;
            Vector3D crossDir = new Vector3D(-gradeDir.Z, 0, gradeDir.X);
            Vector3D delta = worldPoint - BenchmarkPoint;

            float alongDistance = delta.Dot(gradeDir);
            float crossDistance = delta.Dot(crossDir);

            return BenchmarkPoint.Y
                - Settings.TargetCutDepthMeters
                + Settings.SlopeDecimal * alongDistance
                + Settings.CrossSlopeDecimal * crossDistance;
        }

        public GradeError ComputeError(Vector3D referencePoint)
        {
            float targetY = HeightAt(referencePoint);
            float error = referencePoint.Y - targetY;
            return new GradeError(referencePoint, targetY, error, Settings.ToleranceMeters);
        }
    }
}
