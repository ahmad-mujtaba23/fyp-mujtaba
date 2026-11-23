using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Osmosis.Tests
{
    /// Task 6.1 (Editor half): brief rule "zero per-frame allocations". Each per-frame code path is called directly
    /// many times after a warm-up and must allocate 0 bytes. (Editor frames include Editor allocations, so whole-frame
    /// measurements belong on a device with the Profiler; these pin down our own code.)
    public class PerfTests
    {
        const int Calls = 300;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<LabFlow>().enabled = false;
        }

        static long Measure(Action a)
        {
            for (int i = 0; i < 20; i++) a();                                       // warm-up: first-call caches
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Calls; i++) a();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        static Action Update(MonoBehaviour m) => (Action)Delegate.CreateDelegate(typeof(Action), m, "Update");

        static T Spawn<T>(string prefab) where T : Component
        {
#if UNITY_EDITOR
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/" + prefab + ".prefab")).GetComponent<T>();
#else
            Assert.Ignore("editor only"); return null;
#endif
        }

        [UnityTest]
        public IEnumerator StripDeformationAllocatesNothing()
        {
            var s = Spawn<PotatoStripController>("PotatoStrip");
            yield return null;
            s.Soak(40f);
            float t = 0f;
            Assert.AreEqual(0, Measure(() => { t = (t + 0.01f) % 1f; s.SetProgress(t); }));
        }

        [UnityTest]
        public IEnumerator TimeLapseFrameAllocatesNothing()
        {
            var b = Spawn<BeakerController>("Beaker");
            var s = Spawn<PotatoStripController>("PotatoStrip");
            yield return null;
            b.Fill(0f);
            var tl = Object.FindFirstObjectByType<TimeLapseManager>();
            var timer = Object.FindFirstObjectByType<CountdownTimer>();
            typeof(TimeLapseManager).GetField("realSeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tl, 1000f);
            tl.Prepare(s, b);
            tl.FastForward();
            yield return null;
            var tlUpdate = Update(tl);
            var timerUpdate = Update(timer);
            Assert.AreEqual(0, Measure(() => { timerUpdate(); tlUpdate(); }), "stopwatch text + strip + progress per frame");
        }

        [UnityTest]
        public IEnumerator DragFollowAndGhostPulseAllocateNothing()
        {
            var d = Spawn<Draggable>("PotatoStrip");
            yield return null;
            d.SpawnAt(new Vector2(400, 300));
            var ghost = GameObject.Find("RulerTarget").GetComponent<SnapTarget>();
            ghost.Show();
            var drag = Update(d);
            var pulse = Update(ghost);
            var p = new Vector2(400, 300);
            Assert.AreEqual(0, Measure(() => { p.x += 1f; d.Drag(p); drag(); pulse(); }));
        }

        [UnityTest]
        public IEnumerator GraphMeshRebuildAllocatesNothing()
        {
            var graph = Object.FindFirstObjectByType<GraphView>(FindObjectsInactive.Include);
            graph.GetComponentInParent<PopupPanel>(true).gameObject.SetActive(true);   // the Readings window
            graph.Plot(new List<Reading>
            {
                new Reading { concentration = 0, initial = 50, final = 54 },
                new Reading { concentration = 20, initial = 50, final = 47 },
                new Reading { concentration = 40, initial = 50, final = 44 },
            });
            yield return new WaitForSecondsRealtime(1.2f);
            var populate = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), graph, "OnPopulateMesh");
            using var vh = new VertexHelper();
            Assert.AreEqual(0, Measure(() => populate(vh)));
        }
    }
}
