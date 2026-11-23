using System.Globalization;
using TMPro;
using UnityEngine;

namespace Osmosis
{
    /// Port of the reference Numpad (numpadController + findNumpadController).
    /// Writes into a plain TMP_Text display, not an InputField, so Android never pops its full-screen
    /// keyboard in landscape. A display's Button calls Open(display) (wired in the Inspector, no Find calls).
    /// Fixes the reference: no float.Parse crash on "." or "-", one decimal point max, no listener pile-up.
    public class Numpad : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] float maxValue = 999f;
        [SerializeField] int maxDecimals = 1;      // ruler readings to 0.1 mm

        TMP_Text target;

        public string Value => target ? target.text : "";

        public void Open(TMP_Text display)
        {
            target = display;
            transform.SetAsLastSibling();                  // above whichever window asked for it
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        /// Key button target: "0".."9", "." or "<" (backspace).
        public void Press(string key)
        {
            if (target) target.text = Apply(target.text, key, maxValue, maxDecimals);
        }

        /// Pure input rule, unit-tested. Rejected keys return `current` unchanged.
        public static string Apply(string current, string key, float max, int maxDecimals)
        {
            current ??= "";
            if (key == "<") return current.Length > 0 ? current.Substring(0, current.Length - 1) : current;

            if (key == ".")
            {
                if (maxDecimals <= 0 || current.Contains(".")) return current;
                return current.Length == 0 ? "0." : current + ".";
            }

            if (key.Length != 1 || key[0] < '0' || key[0] > '9') return current;

            string next = current == "0" ? key : current + key;        // no leading zeros like "07"
            int dot = next.IndexOf('.');
            if (dot >= 0 && next.Length - dot - 1 > maxDecimals) return current;
            if (!float.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v > max) return current;
            return next;
        }
    }
}
