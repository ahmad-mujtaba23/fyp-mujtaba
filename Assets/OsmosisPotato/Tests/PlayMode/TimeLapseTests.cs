using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Osmosis.Tests
{
    /// Task 4.6: Fast Forward runs 30 simulated minutes, deforms the strip, shows the right water flow, ends cleanly.
    public class TimeLapseTests
    {
        TimeLapseManager tl;
        Button ff;
        CountdownTimer timer;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            tl = Object.FindFirstObjectByType<TimeLapseManager>();
            timer = Object.FindFirstObjectByType<CountdownTimer>();
            ff = (Button)typeof(TimeLapseManager).GetField("fastForward", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tl);
            typeof(TimeLapseManager).GetField("realSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tl, 1f);
        }

        static T Spawn<T>(string prefab, Vector3 at) where T : Component
        {
#if UNITY_EDITOR
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/" + prefab + ".prefab"), at, Quaternion.identity).GetComponent<T>();
#else
            Assert.Ignore("editor only"); return null;
#endif
        }

        IEnumerator Soak(float percent, System.Action<PotatoStripController> done)
        {
            var beaker = Spawn<BeakerController>("Beaker", new Vector3(0.15f, 0, 0.2f));
            PotatoStripController.Rng = new System.Random(11);
            var strip = Spawn<PotatoStripController>("PotatoStrip", Vector3.zero);
            yield return null;
            beaker.Fill(percent);
            strip.transform.position = beaker.StripTarget.transform.position;
            tl.Prepare(strip, beaker);
            done(strip);
        }

        static ParticleSystem Flow() => Object.FindFirstObjectByType<Vfx>().transform.Find("WaterFlow(Clone)")?.GetComponent<ParticleSystem>();

        [UnityTest]
        public IEnumerator ButtonHiddenUntilPrepared()
        {
            Assert.IsFalse(ff.gameObject.activeInHierarchy);
            PotatoStripController strip = null;
            yield return Soak(0f, s => strip = s);
            Assert.IsTrue(ff.gameObject.activeInHierarchy && ff.interactable);
        }

        [UnityTest]
        public IEnumerator WaterRunsThirtyMinutesSwellsWithBlueInflow()
        {
            PotatoStripController strip = null;
            yield return Soak(0f, s => strip = s);
            PotatoStripController finished = null;
            int count = 0;
            tl.Finished += s => { finished = s; count++; };

            float homeDist = Vector3.Distance(Camera.main.transform.position, strip.transform.position);
            ff.onClick.Invoke();
            Assert.AreEqual("30:00", timer.GetComponentInChildren<TMP_Text>().text);
            Assert.IsFalse(ff.interactable, "no double press while running");
            yield return new WaitForSeconds(0.4f);
            Assert.That(strip.LengthMm, Is.GreaterThan(strip.InitialLengthMm), "growing during the time-lapse");
            Assert.IsTrue(Flow().isPlaying, "water particles");
            Assert.AreEqual((Color)Vfx.WaterIn, Flow().main.startColor.color, "blue = water moving in");
            Assert.Less(Flow().velocityOverLifetime.radial.constant, 0f, "towards the strip");

            var rig = Camera.main.GetComponent<CameraRig>();
            Assert.IsFalse(rig.AtHome, "camera zoomed onto the soaking beaker");
            Assert.Less(Vector3.Distance(Camera.main.transform.position, strip.transform.position), homeDist - 0.3f, "closer than the bench view");
            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(1, count, "Finished once");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(rig.AtHome, "camera back to the bench view");
            Assert.AreEqual(strip, finished);
            Assert.AreEqual("00:00", timer.GetComponentInChildren<TMP_Text>().text);
            Assert.AreEqual(OsmosisSolver.Length(strip.InitialLengthMm, 0f, 1f), strip.LengthMm, 0.31f, "full 30 min change");
            Assert.IsFalse(Flow().isEmitting, "particles stop");
            Assert.IsFalse(ff.gameObject.activeInHierarchy, "button hides after");
        }

        [UnityTest]
        public IEnumerator StrongSugarShowsRedOutflow()
        {
            PotatoStripController strip = null;
            yield return Soak(40f, s => strip = s);
            ff.onClick.Invoke();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual((Color)Vfx.WaterOut, Flow().main.startColor.color, "red = water moving out");
            Assert.Greater(Flow().velocityOverLifetime.radial.constant, 0f, "away from the strip");
            yield return new WaitForSeconds(1f);
            Assert.Less(strip.LengthMm, strip.InitialLengthMm);
        }

        [UnityTest]
        public IEnumerator IsotonicHasNoNetFlow()
        {
            PotatoStripController strip = null;
            yield return Soak(OsmosisSolver.IsotonicPercent, s => strip = s);
            ff.onClick.Invoke();
            yield return new WaitForSeconds(0.3f);
            var flow = Flow();
            Assert.IsTrue(flow == null || !flow.isEmitting, "no particles at the isotonic point");
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator FastForwardWithoutStripDoesNothing()
        {
            tl.FastForward();
            yield return null;
            Assert.IsFalse(timer.Running);
        }
    }
}
