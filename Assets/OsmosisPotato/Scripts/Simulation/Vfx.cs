using System.Collections.Generic;
using UnityEngine;

namespace Osmosis
{
    /// Pooled particle effects (brief: "Pool all VFX"). Lives in the Lab scene; calls are safe no-ops without it.
    public class Vfx : MonoBehaviour
    {
        [SerializeField] ParticleSystem splashPrefab, flowPrefab;
        const int MaxSplashes = 4;

        static Vfx instance;
        readonly List<ParticleSystem> splashes = new List<ParticleSystem>(MaxSplashes);
        ParticleSystem flowInstance;                    // one time-lapse at a time: a pool of one, reused

        public static readonly Color WaterIn = new Color(0.25f, 0.66f, 0.96f), WaterOut = new Color(1f, 0.25f, 0.2f);

        public static int PoolSize => instance ? instance.splashes.Count : 0;

        void Awake()
        {
            instance = this;
            for (int i = 0; i < 2; i++) splashes.Add(Instantiate(splashPrefab, transform));   // pre-warm
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Splash(Vector3 at, Color color)
        {
            if (!instance) return;
            var ps = instance.Get();
            ps.transform.position = at;
            var main = ps.main;
            main.startColor = color;
            ps.Play(true);
        }

        /// Water molecules around a strip: inward (blue, into the cells) or outward (red, out of the cells).
        /// Caller stops it; returns null without a Vfx in the scene.
        public static ParticleSystem Flow(Transform strip, bool inward)
        {
            if (!instance) return null;
            var ps = instance.flowInstance ? instance.flowInstance : instance.flowInstance = Instantiate(instance.flowPrefab, instance.transform);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.SetPositionAndRotation(strip.position, strip.rotation);
            var main = ps.main;
            main.startColor = inward ? WaterIn : WaterOut;
            var vel = ps.velocityOverLifetime;
            vel.radial = (inward ? -20f : 20f) * PotatoStripController.UnitsPerMm;
            var shape = ps.shape;
            shape.scale = (inward ? new Vector3(25f, 60f, 25f) : new Vector3(9f, 46f, 9f)) * PotatoStripController.UnitsPerMm;  // shell around vs inside
            ps.Play(true);
            return ps;
        }

        ParticleSystem Get()
        {
            for (int i = 0; i < splashes.Count; i++) if (!splashes[i].isPlaying) return splashes[i];
            if (splashes.Count < MaxSplashes) { var ps = Instantiate(splashPrefab, transform); splashes.Add(ps); return ps; }
            var oldest = splashes[0];                 // all busy: recycle the oldest
            splashes.RemoveAt(0); splashes.Add(oldest);
            oldest.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return oldest;
        }
    }
}
