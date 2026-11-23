using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Port of SlideshowHandler (Guidance scene "Do You Know?" cards).
    /// Next unlocks only after `readSeconds` per slide (halved once the student has seen them all);
    /// Back is locked on the first slide; the last slide shows the "Got it" button.
    public class SlideshowView : MonoBehaviour
    {
        [SerializeField] GameObject[] slides;
        [SerializeField] Button back, next;
        [SerializeField] GameObject gotIt;           // shown on the last slide; OnClick -> MenuActions.StartLab
        [SerializeField] float readSeconds = 3f;

        int index;
        bool seenAll;

        void Start()
        {
            foreach (var s in slides) s.SetActive(false);
            Show(0);
        }

        public void Next() { if (index < slides.Length - 1) Show(index + 1); }
        public void Back() { if (index > 0) Show(index - 1); }

        void Show(int i)
        {
            slides[index].SetActive(false);
            index = i;
            slides[index].SetActive(true);

            bool last = index == slides.Length - 1;
            if (last) seenAll = true;
            back.interactable = index > 0;
            next.gameObject.SetActive(!last);
            gotIt.SetActive(last);

            StopAllCoroutines();
            next.interactable = false;
            if (!last) StartCoroutine(UnlockNext(seenAll ? readSeconds * 0.5f : readSeconds));
        }

        IEnumerator UnlockNext(float t)
        {
            yield return new WaitForSeconds(t);
            next.interactable = true;
        }
    }
}
