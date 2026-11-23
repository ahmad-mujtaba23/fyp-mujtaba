using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Osmosis
{
    /// Port of MyButtonHandler + ButtonsShakeOnClick: a Button that plays the click
    /// sound and gives a small scale punch. Use it in place of Button everywhere.
    public class SfxButton : Button
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable()) return;
            Sfx.Play(Sfx.Click);
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.6f);
            base.OnPointerClick(eventData);
        }
    }
}
