using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Osmosis.Tests
{
    /// Task 4.5: strip length/thickness follow OsmosisSolver; shrinking strips wrinkle, swelling strips stay smooth.
    public class StripDeformTests
    {
        static PotatoStripController NewStrip(int seed)
        {
            PotatoStripController.Rng = new System.Random(seed);
#if UNITY_EDITOR
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/PotatoStrip.prefab"))
                .GetComponent<PotatoStripController>();
#else
            Assert.Ignore("editor only"); return null;
#endif
        }

        static Transform Mesh(PotatoStripController s) => s.transform.Find("Mesh");
        static float Bump(PotatoStripController s)
        {
            var b = new MaterialPropertyBlock();
            Mesh(s).GetComponent<Renderer>().GetPropertyBlock(b);
            return b.GetFloat("_BumpScale");
        }

        [TearDown] public void Clean()
        {
            foreach (var s in Object.FindObjectsByType<PotatoStripController>(FindObjectsSortMode.None)) Object.Destroy(s.gameObject);
            PotatoStripController.Rng = new System.Random();
        }

        [UnityTest]
        public IEnumerator FreshStripIsAboutFiftyMmAndScaledToMatch()
        {
            var s = NewStrip(1);
            yield return null;
            Assert.That(s.InitialLengthMm, Is.InRange(48f, 52f));
            Assert.AreEqual(s.InitialLengthMm * PotatoStripController.UnitsPerMm, Mesh(s).localScale.y, 1e-5f);
            Assert.AreEqual(PotatoStripController.ThickMm * PotatoStripController.UnitsPerMm, Mesh(s).localScale.x, 1e-5f);
            Assert.AreEqual(0f, Bump(s), 1e-4f, "fresh strip is smooth");
        }

        [UnityTest]
        public IEnumerator SameSeedSameCut()
        {
            float a = NewStrip(7).InitialLengthMm, b = NewStrip(7).InitialLengthMm;
            yield return null;
            Assert.AreEqual(a, b);
        }

        [UnityTest]
        public IEnumerator NotSoakedDoesNotChange()
        {
            var s = NewStrip(2);
            yield return null;
            s.SetProgress(1f);
            Assert.AreEqual(s.InitialLengthMm, s.LengthMm);
        }

        [UnityTest]
        public IEnumerator WaterSwellsPlumpAndSmooth()
        {
            var s = NewStrip(3);
            yield return null;
            float thick0 = Mesh(s).localScale.x;
            s.Soak(0f);
            s.SetProgress(0.5f);
            float half = s.LengthMm;
            s.SetProgress(1f);
            Assert.AreEqual(s.InitialLengthMm * 1.08f, s.LengthMm, 0.35f, "about +8 %");
            Assert.That(half, Is.GreaterThan(s.InitialLengthMm).And.LessThan(s.LengthMm), "animates over time");
            Assert.Greater(Mesh(s).localScale.x, thick0, "plumper");
            Assert.AreEqual(s.LengthMm * PotatoStripController.UnitsPerMm, Mesh(s).localScale.y, 1e-5f);
            Assert.AreEqual(0f, Bump(s), 1e-4f, "no wrinkles");
        }

        [UnityTest]
        public IEnumerator StrongSugarShrinksThinAndWrinkled()
        {
            var s = NewStrip(4);
            yield return null;
            float thick0 = Mesh(s).localScale.x;
            s.Soak(40f);
            s.SetProgress(1f);
            Assert.Less(s.LengthMm, s.InitialLengthMm * 0.9f);
            Assert.Less(Mesh(s).localScale.x, thick0, "thinner");
            Assert.Greater(s.Wrinkle, 0.5f);
            Assert.Greater(Bump(s), 0.2f, "normal map strength rises");
        }

        [UnityTest]
        public IEnumerator IsotonicBarelyChanges()
        {
            var s = NewStrip(5);
            yield return null;
            s.Soak(OsmosisSolver.IsotonicPercent);
            s.SetProgress(1f);
            Assert.AreEqual(s.InitialLengthMm, s.LengthMm, 0.31f, "only natural variation");
        }
    }
}
