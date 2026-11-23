using System;
using UnityEngine;

namespace Osmosis
{
    /// Analytic osmosis model (brief §4), pure functions, no MonoBehaviour.
    /// TargetLength = Base * (1 - k * (C - Ciso)), eased over the time-lapse.
    public static class OsmosisSolver
    {
        /// Sucrose % (w/v) isotonic with potato cell sap: about 0.3 M, i.e. 10%.
        public const float IsotonicPercent = 10f;
        /// Fractional length change per 1% concentration away from isotonic: 0% water gives +8%.
        public const float K = 0.008f;
        /// Cells can only lose so much water before full plasmolysis, so shrinking plateaus at -15%.
        // ponytail: hard clamp, swap for a smooth saturation curve if the graph's knee looks too sharp
        public const float MaxShrink = 0.15f;
        public const float MaxPercent = 50f;

        /// Strip length after fraction `t01` of the soak at `percent` concentration.
        public static float Length(float baseLength, float percent, float t01)
        {
            percent = Mathf.Clamp(percent, 0f, MaxPercent);
            t01 = Mathf.Clamp01(t01);
            float eased = 1f - (1f - t01) * (1f - t01);                       // fast at first, then slows
            float fullChange = Mathf.Max(-K * (percent - IsotonicPercent), -MaxShrink);
            return baseLength * (1f + fullChange * eased);
        }

        /// Natural strip-to-strip variation, uniform in [-max, max]. Seeded for repeatable tests.
        public static float Jitter(System.Random rng, float max) => (float)(rng.NextDouble() * 2.0 - 1.0) * max;

        /// Concentration where change in length crosses zero, interpolated between the two readings that
        /// bracket it (what a student reads off the graph). False if all readings are on one side.
        public static bool TryIsotonic(Reading[] readings, out float percent)
        {
            percent = 0f;
            if (readings == null || readings.Length == 0) return false;
            var r = (Reading[])readings.Clone();
            Array.Sort(r, (a, b) => a.concentration.CompareTo(b.concentration));

            for (int i = 0; i < r.Length; i++)
            {
                if (Mathf.Approximately(r[i].Change, 0f)) { percent = r[i].concentration; return true; }
                if (i == 0) continue;
                float d0 = r[i - 1].Change, d1 = r[i].Change;
                if (d0 * d1 < 0f)
                {
                    percent = Mathf.Lerp(r[i - 1].concentration, r[i].concentration, d0 / (d0 - d1));
                    return true;
                }
            }
            return false;
        }
    }
}
