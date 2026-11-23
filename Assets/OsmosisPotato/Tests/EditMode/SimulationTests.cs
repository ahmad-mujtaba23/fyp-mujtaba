using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Osmosis.Tests
{
    public class OsmosisSolverTests
    {
        const float Base = 50f;   // mm

        [Test] public void NoTimeNoChange() => Assert.AreEqual(Base, OsmosisSolver.Length(Base, 0f, 0f), 1e-4f);

        [Test] public void WaterSwells() => Assert.Greater(OsmosisSolver.Length(Base, 0f, 1f), Base);

        [Test] public void IsotonicUnchanged() =>
            Assert.AreEqual(Base, OsmosisSolver.Length(Base, OsmosisSolver.IsotonicPercent, 1f), 1e-4f);

        [Test] public void StrongSugarShrinks() => Assert.Less(OsmosisSolver.Length(Base, 40f, 1f), Base);

        [Test] public void RealisticMagnitudes()
        {
            Assert.AreEqual(1.08f, OsmosisSolver.Length(1f, 0f, 1f), 0.005f, "about +8% in pure water");
            Assert.AreEqual(1f - OsmosisSolver.MaxShrink, OsmosisSolver.Length(1f, 50f, 1f), 1e-4f, "shrink plateaus (full plasmolysis)");
        }

        [Test] public void MoreConcentratedNeverLonger()
        {
            float prev = float.MaxValue;
            for (float c = 0; c <= 50f; c += 5f)
            {
                float l = OsmosisSolver.Length(Base, c, 1f);
                Assert.LessOrEqual(l, prev + 1e-5f, "monotonic at " + c);
                prev = l;
            }
        }

        [Test] public void ChangeGrowsOverTimeAndEaseOut()
        {
            float early = OsmosisSolver.Length(Base, 0f, 0.25f) - Base;
            float half = OsmosisSolver.Length(Base, 0f, 0.5f) - Base;
            float full = OsmosisSolver.Length(Base, 0f, 1f) - Base;
            Assert.That(early, Is.GreaterThan(0f).And.LessThan(half));
            Assert.Greater(half, full * 0.5f, "fast at first, then slows (ease-out)");
        }

        [Test] public void InputsClamped()
        {
            Assert.AreEqual(OsmosisSolver.Length(Base, 0f, 1f), OsmosisSolver.Length(Base, -10f, 2f), 1e-4f);
            Assert.AreEqual(OsmosisSolver.Length(Base, 50f, 1f), OsmosisSolver.Length(Base, 90f, 1f), 1e-4f);
        }

        [Test] public void JitterIsBoundedAndSeeded()
        {
            var a = new System.Random(7); var b = new System.Random(7);
            for (int i = 0; i < 100; i++)
            {
                float j = OsmosisSolver.Jitter(a, 0.5f);
                Assert.That(j, Is.InRange(-0.5f, 0.5f));
                Assert.AreEqual(j, OsmosisSolver.Jitter(b, 0.5f), "same seed, same sequence");
            }
        }
    }

    public class IsotonicTests
    {
        static Reading R(float c, float initial, float final) => new Reading { concentration = c, initial = initial, final = final };

        [Test]
        public void InterpolatesZeroCrossing()
        {
            var r = new[] { R(0, 50, 54), R(20, 50, 46), R(40, 50, 43) };     // +4 at 0%, -4 at 20%
            Assert.IsTrue(OsmosisSolver.TryIsotonic(r, out var c));
            Assert.AreEqual(10f, c, 1e-4f);
        }

        [Test]
        public void UnsortedInputAndExactZero()
        {
            var r = new[] { R(30, 50, 45), R(10, 50, 50), R(0, 50, 53) };
            Assert.IsTrue(OsmosisSolver.TryIsotonic(r, out var c));
            Assert.AreEqual(10f, c, 1e-4f);
        }

        [Test]
        public void NotFoundWhenAllOneSide()
        {
            Assert.IsFalse(OsmosisSolver.TryIsotonic(new[] { R(0, 50, 54), R(5, 50, 52) }, out _));
            Assert.IsFalse(OsmosisSolver.TryIsotonic(new[] { R(30, 50, 45) }, out _));
            Assert.IsFalse(OsmosisSolver.TryIsotonic(new Reading[0], out _));
            Assert.IsFalse(OsmosisSolver.TryIsotonic(null, out _));
        }

        [Test]
        public void SolverDataFindsItsOwnIsotonicPoint()
        {
            var r = new Reading[6];
            for (int i = 0; i < r.Length; i++) { float c = i * 5f; r[i] = R(c, 50, OsmosisSolver.Length(50, c, 1f)); }
            Assert.IsTrue(OsmosisSolver.TryIsotonic(r, out var iso));
            Assert.AreEqual(OsmosisSolver.IsotonicPercent, iso, 0.01f);
        }

        [Test] public void ChangeIsFinalMinusInitial() => Assert.AreEqual(-2.5f, R(20, 50, 47.5f).Change, 1e-5f);
    }

    public class SaveServiceTests
    {
        string path;

        [SetUp] public void SetUp() => path = Path.Combine(Application.temporaryCachePath, "osmosis_test_save.json");
        [TearDown] public void TearDown() { if (File.Exists(path)) File.Delete(path); }

        [Test]
        public void RoundTrip()
        {
            var d = new SaveData();
            d.readings.Add(new Reading { concentration = 20, initial = 50, final = 46.5f });
            d.hasBestIsotonic = true;
            d.bestIsotonic = 10.2f;
            SaveService.Save(d, path);

            var back = SaveService.Load(path);
            Assert.AreEqual(1, back.readings.Count);
            Assert.AreEqual(46.5f, back.readings[0].final);
            Assert.IsTrue(back.hasBestIsotonic);
            Assert.AreEqual(10.2f, back.bestIsotonic);
        }

        [Test] public void MissingFileGivesEmpty() => Assert.AreEqual(0, SaveService.Load(path).readings.Count);

        [Test]
        public void CorruptFileGivesEmptyWithoutThrowing()
        {
            File.WriteAllText(path, "{ not json ]]");
            SaveData d = null;
            Assert.DoesNotThrow(() => d = SaveService.Load(path));
            Assert.IsNotNull(d);
            Assert.AreEqual(0, d.readings.Count);
        }

        [Test]
        public void SaveOverwritesAndLeavesNoTempFile()
        {
            SaveService.Save(new SaveData { bestIsotonic = 1 }, path);
            SaveService.Save(new SaveData { bestIsotonic = 2 }, path);
            Assert.AreEqual(2f, SaveService.Load(path).bestIsotonic);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }
    }
}
