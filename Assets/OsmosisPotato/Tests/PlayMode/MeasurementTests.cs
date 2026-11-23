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
    /// Task 4.7: ruler zero meets the strip end, camera zooms and returns, numpad input checked within ±0.5 mm.
    public class MeasurementTests
    {
        MeasurementTool tool;
        PotatoStripController strip;
        RectTransform card;
        Camera cam;
        Vector3 camHome;
        float recorded;
        int calls;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            Object.FindFirstObjectByType<LabFlow>().enabled = false;                    // drive the tool directly
            tool = Object.FindFirstObjectByType<MeasurementTool>();
            foreach (var r in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.name == "MeasurePanel") card = r;
            cam = Camera.main;
            camHome = cam.transform.position;
            var ruler = GameObject.Find("RulerTarget").transform;
            PotatoStripController.Rng = new System.Random(21);
#if UNITY_EDITOR
            // arrive like a real drag: from mid-air somewhere else, snapping in (Begin runs before it lands)
            strip = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OsmosisPotato/Prefabs/Lab/PotatoStrip.prefab"),
                new Vector3(0.6f, Draggable.CarryHeight, 0.4f), Quaternion.identity).GetComponent<PotatoStripController>();
#endif
            yield return null;
            var target = ruler.GetComponent<SnapTarget>();
            target.Show();
            strip.GetComponent<Draggable>().SnapTo(target);
            calls = 0;
            tool.Begin(strip, "Initial", v => { recorded = v; calls++; });
            yield return new WaitForSeconds(0.7f);
        }

        void Type(string s) => card.Find("Display/Value").GetComponent<TMP_Text>().text = s;
        void Record() => card.Find("RecordBtn").GetComponent<Button>().onClick.Invoke();
        string Message => card.Find("Message").GetComponent<TMP_Text>().text;

        [Test]
        public void OpensCardNumpadAndZooms()
        {
            Assert.IsTrue(card.gameObject.activeSelf);
            StringAssert.Contains("Initial length", card.Find("Title").GetComponent<TMP_Text>().text);
            Assert.IsTrue(Object.FindFirstObjectByType<Numpad>().transform.Find("Panel").gameObject.activeSelf, "numpad opens");
            Assert.Less(Vector3.Distance(cam.transform.position, strip.transform.position), 0.5f, "camera zoomed onto the strip");
        }

        [Test]
        public void RulerZeroMeetsStripLeftEnd()
        {
            var zeroLabel = GameObject.Find("cm0").transform.position.x;
            float leftEnd = strip.transform.position.x - strip.LengthMm * 0.5f * PotatoStripController.UnitsPerMm;
            Assert.AreEqual(leftEnd, zeroLabel, 0.002f, "0 mark under the strip's end");
        }

        [UnityTest]
        public IEnumerator EmptyAndWrongRejectedRightAccepted()
        {
            Type(""); Record();
            Assert.AreEqual(0, calls); StringAssert.Contains("Type the length", Message);

            Type((strip.LengthMm + 2f).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); Record();
            Assert.AreEqual(0, calls, "2 mm off is wrong"); StringAssert.Contains("Look again", Message);
            Assert.IsTrue(card.gameObject.activeSelf, "can try again");

            float read = Mathf.Round(strip.LengthMm * 2f) / 2f;                          // within ±0.25 of true
            Type(read.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); Record();
            Assert.AreEqual(1, calls);
            Assert.AreEqual(read, recorded, 1e-4f, "student's value recorded");
            Assert.IsFalse(card.gameObject.activeSelf, "card closes");
            yield return new WaitForSeconds(1.2f);
            Assert.Less(Vector3.Distance(cam.transform.position, camHome), 0.01f, "camera back to the bench view");
            Assert.AreEqual("1", GameObject.Find("ReadingsBtn").transform.Find("Badge/Count").GetComponent<TMP_Text>().text, "reading glided to the badge");
        }

        [Test]
        public void CentimetreReadingConvertedToMm()
        {
            float cm = Mathf.Round(strip.LengthMm) / 10f;                                // e.g. 4.9 for a 49.2 mm strip
            Type(cm.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); Record();
            Assert.AreEqual(1, calls, "a cm reading off the cm-numbered ruler is accepted");
            Assert.AreEqual(cm * 10f, recorded, 1e-3f, "recorded in mm");
        }

        [Test]
        public void WrongCmReadingStillRejected()
        {
            Type("2.5"); Record();
            Assert.AreEqual(0, calls);
            StringAssert.Contains("cm", Message);
        }

        [Test]
        public void HalfMillimetreBoundaryAccepted()
        {
            Type((strip.LengthMm + 0.49f).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)); Record();
            Assert.AreEqual(1, calls, "just under 0.5 mm off is accepted");
        }
    }
}
