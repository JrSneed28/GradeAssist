using System;
using UnityEngine;

namespace GradeAssist.Core
{
    public sealed class GradeTargetSettings
    {
        public float TargetCutDepthMeters;
        public float SlopePercent;
        public float CrossSlopePercent;
        public float ToleranceMeters;

        public float SlopeDecimal => SlopePercent / 100f;
        public float CrossSlopeDecimal => CrossSlopePercent / 100f;

        public void Validate()
        {
            if (float.IsNaN(TargetCutDepthMeters) || float.IsInfinity(TargetCutDepthMeters))
            {
                throw new ArgumentOutOfRangeException(nameof(TargetCutDepthMeters), "Value must be finite.");
            }
            if (float.IsNaN(SlopePercent) || float.IsInfinity(SlopePercent))
            {
                throw new ArgumentOutOfRangeException(nameof(SlopePercent), "Value must be finite.");
            }
            if (float.IsNaN(CrossSlopePercent) || float.IsInfinity(CrossSlopePercent))
            {
                throw new ArgumentOutOfRangeException(nameof(CrossSlopePercent), "Value must be finite.");
            }
            if (float.IsNaN(ToleranceMeters) || float.IsInfinity(ToleranceMeters))
            {
                throw new ArgumentOutOfRangeException(nameof(ToleranceMeters), "Value must be finite.");
            }

            SlopePercent = Mathf.Clamp(SlopePercent, -500f, 500f);
            CrossSlopePercent = Mathf.Clamp(CrossSlopePercent, -500f, 500f);

            if (ToleranceMeters < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ToleranceMeters), "Tolerance must be non-negative.");
            }
        }
    }
}
