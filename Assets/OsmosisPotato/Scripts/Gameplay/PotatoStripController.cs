using UnityEngine;

namespace Osmosis
{
    /// Potato strip deformation (brief §4, §6). Length (Y scale) follows OsmosisSolver; thickness (X/Z) changes half
    /// as much, so swollen strips look plump. Shrinking strips get duller, a little darker and wrinkled (normal map
    /// strength); swelling strips stay smooth and moist. Per-strip values go through a MaterialPropertyBlock.
    public class PotatoStripController : MonoBehaviour
    {
        // World scale for the whole lab (1 unit = 24 cm, real proportions; big enough to read on a phone).
        public const float UnitsPerMm = 0.0042f;
        public const float NominalLengthMm = 50f, ThickMm = 8f;
        const float CutVariationMm = 2f, FinalVariationMm = 0.3f;

        /// Shared randomness; tests replace it with a seeded one.
        public static System.Random Rng = new System.Random();

        [SerializeField] Transform mesh;
        [SerializeField] Renderer meshRenderer;

        public float InitialLengthMm { get; private set; }
        public float LengthMm { get; private set; }
        public float Concentration { get; private set; }
        public bool Soaked { get; private set; }

        static readonly int ColorId = Shader.PropertyToID("_BaseColor"),
            SmoothId = Shader.PropertyToID("_Smoothness"), BumpId = Shader.PropertyToID("_BumpScale");
        static readonly Color Fresh = new Color(0.97f, 0.92f, 0.75f), Limp = new Color(0.85f, 0.77f, 0.54f);
        MaterialPropertyBlock block;
        float finalNoise;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            InitialLengthMm = LengthMm = NominalLengthMm + OsmosisSolver.Jitter(Rng, CutVariationMm);
            finalNoise = OsmosisSolver.Jitter(Rng, FinalVariationMm);
            Apply();
        }

        /// Put the strip in a solution; SetProgress(0..1) then animates the change.
        public void Soak(float percent)
        {
            Concentration = percent;
            Soaked = true;
        }

        public void SetProgress(float t01)
        {
            if (!Soaked) return;
            t01 = Mathf.Clamp01(t01);
            LengthMm = OsmosisSolver.Length(InitialLengthMm, Concentration, t01) + finalNoise * t01;
            Apply();
        }

        void Apply()
        {
            float ratio = LengthMm / InitialLengthMm;                    // > 1 swollen, < 1 shrunk
            float thick = ThickMm * (1f + 0.5f * (ratio - 1f));
            mesh.localScale = new Vector3(thick, LengthMm, thick) * UnitsPerMm;

            float shrink = Mathf.Clamp01((1f - ratio) / OsmosisSolver.MaxShrink);   // 0 fresh/swollen .. 1 fully limp
            float swell = Mathf.Clamp01((ratio - 1f) / 0.08f);
            block.SetColor(ColorId, Color.Lerp(Fresh, Limp, shrink));
            block.SetFloat(SmoothId, Mathf.Lerp(0.45f, 0.15f, shrink) + 0.15f * swell);
            block.SetFloat(BumpId, Mathf.Lerp(0f, 0.4f, shrink));           // gentle wrinkles, only when shrinking
            meshRenderer.SetPropertyBlock(block);                            // ponytail: opts this renderer out of SRP batching; fine for a few strips
        }

        public float Wrinkle => Mathf.Clamp01((1f - LengthMm / InitialLengthMm) / OsmosisSolver.MaxShrink);
    }
}
