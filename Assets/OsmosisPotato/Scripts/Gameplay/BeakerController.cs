using System;
using UnityEngine;

namespace Osmosis
{
    /// Holds the solution concentration and colours the liquid (brief §2.1, §6): clear blue water at 0 %,
    /// towards amber sugar solution at 50 %. Splashes when a strip is dropped into its StripTarget.
    public class BeakerController : MonoBehaviour
    {
        [SerializeField] Renderer liquid;
        [SerializeField] SnapTarget stripTarget;

        public static readonly Color Water = new Color(0.40f, 0.74f, 0.98f, 0.6f);
        public static readonly Color Sugar = new Color(1.00f, 0.70f, 0.18f, 0.75f);

        public bool Filled { get; private set; }
        public float Concentration { get; private set; }
        public SnapTarget StripTarget => stripTarget;
        public event Action<BeakerController> FilledWith;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            liquid.gameObject.SetActive(false);
        }

        void OnEnable() => SnapTarget.Snapped += OnSnapped;
        void OnDisable() => SnapTarget.Snapped -= OnSnapped;

        public static Color LiquidColor(float percent) => Color.Lerp(Water, Sugar, Mathf.Clamp01(percent / OsmosisSolver.MaxPercent));

        public void Fill(float percent)
        {
            Concentration = Mathf.Clamp(percent, 0f, OsmosisSolver.MaxPercent);
            Filled = true;
            liquid.gameObject.SetActive(true);
            block.SetColor(ColorId, LiquidColor(Concentration));
            liquid.SetPropertyBlock(block);
            Sfx.Play(Sfx.Pour);
            FilledWith?.Invoke(this);
        }

        void OnSnapped(SnapTarget t, Draggable d)
        {
            if (t != stripTarget || !Filled) return;
            Vfx.Splash(liquid.bounds.center + Vector3.up * liquid.bounds.extents.y, LiquidColor(Concentration));
            Sfx.Play(Sfx.Pour);
        }
    }
}
