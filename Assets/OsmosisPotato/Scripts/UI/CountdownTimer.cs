using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Port of CountDownTimerHandler: stopwatch face showing mm:ss of *simulated* time with a
    /// radial fill, ticking sound while running. Used by the Fast Forward time-lapse:
    /// Run(30 * 60, 4f) shows 30:00 -> 00:00 over 4 real seconds.
    public class CountdownTimer : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Image radialFill;

        public event Action Finished;
        public bool Running { get; private set; }
        /// 0 -> 1 progress, for anything that animates alongside (potato strip, particles).
        public float Progress => duration > 0f ? 1f - remaining / duration : 0f;

        float simulatedSeconds, duration, remaining;

        public void Run(float simulatedSeconds, float realSeconds)
        {
            this.simulatedSeconds = simulatedSeconds;
            duration = remaining = realSeconds;
            Running = true;
            Sfx.Play(Sfx.ClockTicking);
            Draw();
        }

        void Update()
        {
            if (!Running) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                Running = false;
                Sfx.Stop(Sfx.ClockTicking);
                Draw();
                Finished?.Invoke();
                return;
            }
            Draw();
        }

        void Draw()
        {
            int sim = Mathf.CeilToInt(simulatedSeconds * (1f - Progress));
            label.SetText("{0:00}:{1:00}", sim / 60, sim % 60);   // TMP SetText: no GC alloc
            if (radialFill) radialFill.fillAmount = 1f - Progress;
        }

        void OnDisable()
        {
            if (Running) Sfx.Stop(Sfx.ClockTicking);
            Running = false;
        }
    }
}
