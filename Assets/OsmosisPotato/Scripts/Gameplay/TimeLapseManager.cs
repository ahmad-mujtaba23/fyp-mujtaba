using System;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Fast Forward (brief §2.3): 30 simulated minutes over a few real seconds. Drives the stopwatch, the strip's
    /// deformation and the water-flow particles (blue in below the isotonic point, red out above it).
    public class TimeLapseManager : MonoBehaviour
    {
        public const float SimulatedMinutes = 30f;

        [SerializeField] CountdownTimer timer;
        [SerializeField] Button fastForward;
        [SerializeField] float realSeconds = 4f;

        public event Action<PotatoStripController> Finished;
        public bool Running => strip && timer.Running;

        PotatoStripController strip;
        BeakerController beaker;
        ParticleSystem flow;
        CameraRig rig;

        void Awake()
        {
            timer.Finished += OnTimerFinished;
            rig = Camera.main ? Camera.main.GetComponent<CameraRig>() : null;
        }

        /// The flow calls this once the strip is in a filled beaker; shows the Fast Forward button.
        public void Prepare(PotatoStripController s, BeakerController beaker)
        {
            strip = s;
            this.beaker = beaker;
            strip.Soak(beaker.Concentration);
            fastForward.gameObject.SetActive(true);
            fastForward.interactable = true;
        }

        /// Fast Forward button target.
        public void FastForward()
        {
            if (!strip || timer.Running) return;                     // nothing prepared, or already running
            fastForward.interactable = false;
            timer.Run(SimulatedMinutes * 60f, realSeconds);
            if (rig) rig.Focus(beaker.transform.position + Vector3.up * 0.33f, new Vector3(0f, 0.16f, -1.3f));   // watch it soak; board text stays below the HUD shelf
            float c = strip.Concentration;
            if (!Mathf.Approximately(c, OsmosisSolver.IsotonicPercent))
                flow = Vfx.Flow(strip.transform, inward: c < OsmosisSolver.IsotonicPercent);
        }

        void Update()
        {
            if (strip && timer.Running) strip.SetProgress(timer.Progress);
        }

        void OnTimerFinished()
        {
            if (!strip) return;
            strip.SetProgress(1f);
            if (flow) { flow.Stop(true, ParticleSystemStopBehavior.StopEmitting); flow = null; }
            fastForward.gameObject.SetActive(false);
            if (rig) rig.Home();
            var done = strip;
            strip = null;
            Finished?.Invoke(done);
        }
    }
}
