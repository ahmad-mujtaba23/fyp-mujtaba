using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Osmosis
{
    /// Port of NotificationsHandler + NotificationHandler + TextGlider, merged:
    /// a recorded reading's label glides to the Readings button, then the red count badge
    /// increments with the notification sound. Opening the Readings window calls Clear().
    public class ReadingsBadge : MonoBehaviour
    {
        [SerializeField] GameObject badge;          // red circle, hidden when count == 0
        [SerializeField] TMP_Text countText;
        [SerializeField] float glideTime = 0.8f;
        [SerializeField] Ease glideEase = Ease.InOutQuad;

        int count;

        void Awake() => Clear();

        /// Fly a copy of `label` (any UI element) to this button, then bump the count.
        public void Glide(RectTransform label)
        {
            var ghost = Instantiate(label, label.GetComponentInParent<Canvas>().rootCanvas.transform);
            ghost.position = label.position;
            ghost.DOMove(transform.position, glideTime).SetEase(glideEase);
            ghost.DOScale(0.4f, glideTime).SetEase(glideEase)
                .OnComplete(() => { Destroy(ghost.gameObject); Increment(); });
        }

        public void Increment()
        {
            count++;
            badge.SetActive(true);
            countText.SetText("{0}", count);
            badge.transform.DOKill(true);
            badge.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
            Sfx.Play(Sfx.Notification);
        }

        public void Clear()
        {
            count = 0;
            badge.SetActive(false);
        }
    }
}
