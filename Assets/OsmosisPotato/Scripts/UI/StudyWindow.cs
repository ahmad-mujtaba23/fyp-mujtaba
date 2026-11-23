using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Port of StudyWindowHandler: Concept / Procedure / Observations tabs over scroll views.
    /// Tab buttons call Select(0|1|2); each page resets to the top when shown.
    /// Tab art comes from the reference (Study/ConOpen.png etc., text baked in), one open/closed pair per tab.
    public class StudyWindow : MonoBehaviour
    {
        [SerializeField] ScrollRect[] pages;            // concept, procedure, observations
        [SerializeField] Image[] tabs;                  // same order as pages
        [SerializeField] Sprite[] tabOpen, tabClosed;   // same order as pages

        void OnEnable() => Select(0);

        public void Select(int page)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                bool on = i == page;
                pages[i].gameObject.SetActive(on);
                if (on) pages[i].verticalNormalizedPosition = 1f;
                tabs[i].sprite = on ? tabOpen[i] : tabClosed[i];
            }
        }
    }
}
