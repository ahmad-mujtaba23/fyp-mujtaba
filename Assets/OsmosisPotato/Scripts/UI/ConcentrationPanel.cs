using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// HUD panel for the current beaker: slider 0–50 % in 5 % steps, live label, Fill button.
    /// Each beaker must use a different concentration, otherwise the graph can't find the isotonic point.
    public class ConcentrationPanel : MonoBehaviour
    {
        public const float Step = 5f;

        [SerializeField] Slider slider;                 // whole numbers 0..10 (x Step %)
        [SerializeField] TMP_Text valueLabel, message;

        public event Action<BeakerController> Filled;
        public float Percent => slider.value * Step;

        BeakerController target;
        readonly HashSet<float> used = new HashSet<float>();

        void Awake() => slider.onValueChanged.AddListener(_ => Refresh());   // saved inactive; Open() shows it

        public void Open(BeakerController beaker)
        {
            target = beaker;
            message.text = "";
            gameObject.SetActive(true);
            Refresh();
        }

        /// Fill button target.
        public void Fill()
        {
            if (!target) return;
            if (used.Contains(Percent))
            {
                message.text = "Already used. Choose a different concentration.";
                Sfx.Play(Sfx.Wrong);
                return;
            }
            used.Add(Percent);
            var b = target;
            target = null;
            gameObject.SetActive(false);
            b.Fill(Percent);
            Filled?.Invoke(b);
        }

        void Refresh()
        {
            valueLabel.SetText("{0} %", Percent);
            if (message.text.Length > 0 && !used.Contains(Percent)) message.text = "";
        }
    }
}
