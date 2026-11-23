using NUnit.Framework;

namespace Osmosis.Tests
{
    public class NumpadTests
    {
        static string Type(string keys, float max = 999f, int dec = 1)
        {
            string s = "";
            foreach (var k in keys) s = Numpad.Apply(s, k.ToString(), max, dec);
            return s;
        }

        [Test] public void Digits() => Assert.AreEqual("52", Type("52"));
        [Test] public void Backspace() => Assert.AreEqual("5", Type("52<"));
        [Test] public void BackspaceOnEmptyIsSafe() => Assert.AreEqual("", Type("<<"));
        [Test] public void LeadingDotBecomesZeroDot() => Assert.AreEqual("0.5", Type(".5"));
        [Test] public void OnlyOneDot() => Assert.AreEqual("5.2", Type("5..2."));
        [Test] public void NoLeadingZeros() => Assert.AreEqual("7", Type("07"));
        [Test] public void DecimalsLimited() => Assert.AreEqual("5.2", Type("5.25"));
        [Test] public void NoDotWhenIntegerOnly() => Assert.AreEqual("52", Type("5.2", dec: 0));
        [Test] public void MaxValueRejectsOverflow() => Assert.AreEqual("99", Type("999", max: 100f));
        [Test] public void UnknownKeyIgnored() => Assert.AreEqual("5", Numpad.Apply("5", "-", 999f, 1));
        [Test] public void NullCurrentSafe() => Assert.AreEqual("3", Numpad.Apply(null, "3", 999f, 1));
    }
}
