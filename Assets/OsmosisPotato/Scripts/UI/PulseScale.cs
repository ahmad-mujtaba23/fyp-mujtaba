using DG.Tweening;
using UnityEngine;

namespace Osmosis
{
    /// Port of ScaleUpDownHandler: gentle breathing scale while enabled
    /// (instruction board after typing, guidance arrows, "tap here" buttons).
    public class PulseScale : MonoBehaviour
    {
        [SerializeField] float amount = 0.08f;
        [SerializeField] float halfPeriod = 0.75f;

        Vector3 baseScale;
        Tween tween;

        void Awake() => baseScale = transform.localScale;

        void OnEnable()
        {
            transform.localScale = baseScale;
            tween = transform.DOScale(baseScale * (1f + amount), halfPeriod)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        void OnDisable()
        {
            tween?.Kill();
            transform.localScale = baseScale;
        }
    }
}
