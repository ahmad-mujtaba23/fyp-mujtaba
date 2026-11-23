using DG.Tweening;
using UnityEngine;

namespace Osmosis
{
    /// Spring-in / fade-out for any window (Exit dialog, Study window, Result card).
    /// The brief asks for spring animation on the result card; the reference just toggled
    /// SetActive, so this is the one deliberate upgrade. Needs a CanvasGroup on the same object.
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField] RectTransform card;     // the panel body that springs; defaults to self
        [SerializeField] float inTime = 0.35f, outTime = 0.15f;

        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (!card) card = (RectTransform)transform;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            group.DOKill();
            card.DOKill();
            group.alpha = 0f;
            card.localScale = Vector3.one * 0.7f;
            group.DOFade(1f, inTime * 0.6f).SetUpdate(true);
            card.DOScale(1f, inTime).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Close()
        {
            group.DOKill();
            group.DOFade(0f, outTime).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
        }
    }
}
