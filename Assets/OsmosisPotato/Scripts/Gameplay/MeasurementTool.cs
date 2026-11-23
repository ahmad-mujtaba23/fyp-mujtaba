using System;
using System.Collections;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Osmosis
{
    /// Virtual ruler (brief §4 "Measurement"): zooms the camera onto the strip, slides the ruler so its 0 mark meets
    /// the strip's end, and asks the student to type the length on the numpad. Accepted within ±0.5 mm of the true
    /// length (reference tolerance); the student's own value is what gets recorded.
    public class MeasurementTool : MonoBehaviour
    {
        public const float ToleranceMm = 0.5f;
        const float CmThreshold = 20f;

        [SerializeField] Transform rulerGroup;          // ruler mesh + cm labels; moved as one
        [SerializeField] float zeroX;                   // world X of the 0 mark when rulerGroup is at its build position
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title, display, message;
        [SerializeField] Numpad numpad;
        [SerializeField] ReadingsBadge badge;
        [SerializeField] GameObject feedbackWrong;

        public bool Measuring => strip != null;
        public float TrueLengthMm => strip ? strip.LengthMm : 0f;

        CameraRig rig;
        Vector3 rulerHome;
        PotatoStripController strip;
        Action<float> done;

        void Awake()
        {
            rig = Camera.main.GetComponent<CameraRig>();
            rulerHome = rulerGroup.position;
        }

        /// Start measuring `s` (lying on the ruler target). `what` = "Initial" / "Final".
        public void Begin(PotatoStripController s, string what, Action<float> onRecorded)
        {
            strip = s;
            done = onRecorded;

            // Where the strip comes to rest: its snap spot. Snapped fires while it is still springing into place,
            // so its current position can be anywhere along the drag (bug seen in play, 2026-11-23).
            var d = s.GetComponent<Draggable>();
            Vector3 rest = d && d.Current ? d.Current.transform.position : s.transform.position;

            // ruler 0 mark to the strip's left end (strip lies along X)
            float leftEnd = rest.x - s.LengthMm * 0.5f * PotatoStripController.UnitsPerMm;
            rulerGroup.DOKill();
            rulerGroup.DOMoveX(rulerHome.x + (leftEnd - zeroX), 0.45f).SetEase(Ease.OutBack);

            // close, slightly-from-the-front top view centred on the strip
            rig.Focus(rest + new Vector3(0f, 0f, -0.01f), new Vector3(0f, 0.36f, -0.15f));

            title.text = what + " length of the strip";
            display.text = "";
            message.text = "";
            panel.SetActive(true);
            numpad.Open(display);
        }

        /// Record button target.
        public void Record()
        {
            if (!strip) return;
            if (!float.TryParse(display.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float typed))
            {
                message.text = "Type the length in mm first.";
                Sfx.Play(Sfx.Wrong);
                return;
            }
            // The ruler is numbered in cm; a strip is 40-60 mm, so anything under 20 can only be a cm reading.
            if (typed > 0f && typed < CmThreshold) typed *= 10f;
            if (Mathf.Abs(typed - strip.LengthMm) > ToleranceMm + 1e-4f)
            {
                message.text = "Look again: big numbers are cm, small ticks are mm (4.9 cm = 49 mm).";
                Sfx.Play(Sfx.Wrong);
                if (feedbackWrong) StartCoroutine(Flash(feedbackWrong));
                return;
            }

            Sfx.Play(Sfx.Correct);
            if (badge) badge.Glide(display.rectTransform);
            numpad.Close();
            panel.SetActive(false);
            rig.Home();
            var callback = done;
            strip = null;
            done = null;
            callback?.Invoke(typed);
        }

        static IEnumerator Flash(GameObject go)
        {
            go.SetActive(true);
            go.transform.DOKill(true);
            go.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            yield return new WaitForSeconds(0.9f);
            go.SetActive(false);
        }
    }
}
