using System.Collections;
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
    /// Task 4.4: beaker fill + liquid colour, concentration panel (5 % steps, no duplicates), pooled splash.
    public class BeakerTests
    {
        ConcentrationPanel panel;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            foreach (var p in Object.FindObjectsByType<ConcentrationPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)) panel = p;
            Assert.IsNotNull(panel);
        }

        static BeakerController NewBeaker(Vector3 at)
        {
#if UNITY_EDITOR
            var b = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/Beaker.prefab"), at, Quaternion.identity);
            return b.GetComponent<BeakerController>();
#else
            Assert.Ignore("editor only"); return null;
#endif
        }

        static Color LiquidColor(BeakerController b)
        {
            var r = b.transform.Find("Liquid").GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            return block.GetColor("_BaseColor");
        }

        static void AssertColor(Color expected, Color actual, string msg) =>
            Assert.That(Vector4.Distance(expected, actual), Is.LessThan(0.01f), msg + $" expected {expected} got {actual}");

        [UnityTest]
        public IEnumerator EmptyUntilFilledThenColouredByConcentration()
        {
            var b = NewBeaker(new Vector3(0.15f, 0, 0.2f));
            yield return null;
            Assert.IsFalse(b.transform.Find("Liquid").gameObject.activeSelf, "empty beaker");
            Assert.IsFalse(b.Filled);

            b.Fill(0f);
            Assert.IsTrue(b.transform.Find("Liquid").gameObject.activeSelf);
            AssertColor(BeakerController.Water, LiquidColor(b), "clear blue water at 0 %");

            var b2 = NewBeaker(new Vector3(0.55f, 0, 0.2f));
            yield return null;
            b2.Fill(50f);
            AssertColor(BeakerController.Sugar, LiquidColor(b2), "amber at 50 %");
            Assert.AreEqual(50f, b2.Concentration);
        }

        [UnityTest]
        public IEnumerator PanelStepsLabelsAndFills()
        {
            var b = NewBeaker(new Vector3(0.15f, 0, 0.2f));
            yield return null;
            BeakerController filled = null;
            panel.Filled += x => filled = x;

            panel.Open(b);
            Assert.IsTrue(panel.gameObject.activeSelf);
            var slider = panel.GetComponentInChildren<Slider>();
            slider.value = 4;
            Assert.AreEqual(20f, panel.Percent, "5 % steps");
            Assert.AreEqual("20 %", panel.transform.Find("Value").GetComponent<TMP_Text>().text);
            slider.value = 4.4f;
            Assert.AreEqual(4f, slider.value, "whole steps only");

            panel.transform.Find("FillBtn").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(b, filled);
            Assert.AreEqual(20f, b.Concentration);
            Assert.IsFalse(panel.gameObject.activeSelf, "closes after filling");
        }

        [UnityTest]
        public IEnumerator SameConcentrationTwiceRejected()
        {
            var a = NewBeaker(new Vector3(0.15f, 0, 0.2f));
            var b = NewBeaker(new Vector3(0.55f, 0, 0.2f));
            yield return null;
            var slider = panel.GetComponentInChildren<Slider>();

            panel.Open(a); slider.value = 2; panel.Fill();
            panel.Open(b); slider.value = 2; panel.Fill();
            Assert.IsFalse(b.Filled, "duplicate 10 % rejected");
            Assert.IsTrue(panel.gameObject.activeSelf, "panel stays open");
            StringAssert.Contains("different", panel.transform.Find("Message").GetComponent<TMP_Text>().text);

            slider.value = 8;
            Assert.AreEqual("", panel.transform.Find("Message").GetComponent<TMP_Text>().text, "warning clears on a new value");
            panel.Fill();
            Assert.IsTrue(b.Filled);
            Assert.AreEqual(40f, b.Concentration);
        }

        [UnityTest]
        public IEnumerator SplashIsPooled()
        {
            for (int i = 0; i < 10; i++) { Vfx.Splash(Vector3.zero, Color.blue); yield return null; }
            Assert.That(Vfx.PoolSize, Is.InRange(1, 4), "never more than 4 splash systems");
            Assert.AreEqual(Vfx.PoolSize, Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator StripIntoFilledBeakerSplashes()
        {
            var b = NewBeaker(new Vector3(0.15f, 0, 0.2f));
            yield return null;
            b.Fill(0f);
            b.StripTarget.Show();
#if UNITY_EDITOR
            var strip = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/PotatoStrip.prefab")).GetComponent<Draggable>();
#endif
            yield return null;
            strip.SnapTo(b.StripTarget);
            yield return null;
            bool playing = false;
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) playing |= ps.isPlaying;
            Assert.IsTrue(playing, "splash plays");
        }
    }
}
