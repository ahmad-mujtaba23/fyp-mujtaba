using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Osmosis.Tests
{
    /// M5: Readings table, animated graph, isotonic question, result card + save.
    public class ReadingsTests
    {
        LabFlow flow;
        ReadingsView view;
        GraphView graph;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            flow = Object.FindFirstObjectByType<LabFlow>();
            view = Object.FindFirstObjectByType<ReadingsView>(FindObjectsInactive.Include);
            graph = Object.FindFirstObjectByType<GraphView>(FindObjectsInactive.Include);
        }

        static Reading R(float c, float i, float f) => new Reading { concentration = c, initial = i, final = f };

        void Seed(bool done, params Reading[] rs)
        {
            var list = (List<Reading>)typeof(LabFlow).GetField("readings", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(flow);
            list.Clear(); list.AddRange(rs);
            if (done) typeof(LabFlow).GetField("<Current>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(flow, LabFlow.Step.Done);
        }

        static T Child<T>(Component root, string path) where T : Component => root.transform.Find(path).GetComponent<T>();
        Slider Guess => view.GetComponentInChildren<Slider>(true);
        TMP_Text Message => Child<TMP_Text>(view, "Card/Message");
        GameObject ResultCard => Object.FindFirstObjectByType<ReadingsView>(FindObjectsInactive.Include)
            .GetType().GetField("resultCard", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view) is PopupPanel p ? p.gameObject : null;

        IEnumerator OpenViaButton()
        {
            GameObject.Find("ReadingsBtn").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.4f);                              // popup + graph animation
        }

        [UnityTest]
        public IEnumerator TableAndGraphFromReadings()
        {
            Seed(true, R(0, 50, 53.6f), R(20, 49, 46.6f), R(40, 51, 45f));
            GameObject.Find("ReadingsBtn").GetComponent<ReadingsBadge>().Increment();
            yield return OpenViaButton();

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual("0", Child<TMP_Text>(view, "Card/Row1/C0").text);
            Assert.AreEqual("50.0", Child<TMP_Text>(view, "Card/Row1/C1").text);
            Assert.AreEqual("+3.6", Child<TMP_Text>(view, "Card/Row1/C3").text);
            Assert.AreEqual("-6.0", Child<TMP_Text>(view, "Card/Row3/C3").text);
            Assert.AreEqual("-11.8", Child<TMP_Text>(view, "Card/Row3/C4").text, "change in %");

            Assert.AreEqual(3, graph.PointCount);
            Assert.AreEqual(3f, graph.RevealedPoints, 1e-3f, "all points popped in");
            Assert.AreEqual(7f, graph.YMax, "axis fits the biggest change");
            Assert.AreEqual("7", Child<TMP_Text>(view, "Card/GraphFrame/Y0").text, "plain axis numbers");
            Assert.AreEqual("-3.5", Child<TMP_Text>(view, "Card/GraphFrame/Y3").text);
            Assert.IsTrue(Guess.gameObject.activeSelf, "draggable line on the graph");
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            var pe = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, graph.transform.position) };
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pe, hits);
            Assert.IsNotEmpty(hits);
            Assert.AreEqual(Guess.gameObject, UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(hits[0].gameObject),
                "a touch on the graph reaches the slider, got " + hits[0].gameObject.name);
            Assert.IsTrue(view.transform.Find("Card/Question").gameObject.activeSelf, "isotonic question once all strips are done");
            Assert.IsFalse(GameObject.Find("ReadingsBtn").transform.Find("Badge").gameObject.activeSelf, "badge cleared on open");
        }

        [UnityTest]
        public IEnumerator CorrectIsotonicOpensResultAndSaves()
        {
            Seed(true, R(0, 50, 53.6f), R(20, 49, 46.6f), R(40, 51, 45f));          // crosses zero at 12 %
            yield return OpenViaButton();
            Guess.value = 11.2f;                                                      // dragged to ~11 %
            Assert.AreEqual("11.0 %", Child<TMP_Text>(view, "Card/Question/Guess").text, "reads to the nearest 0.5 %");
            Child<Button>(view, "Card/Question/CheckBtn").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.IsTrue(ResultCard.activeSelf, "result card");
            var body = ResultCard.transform.Find("Card/Body").GetComponent<TMP_Text>().text;
            StringAssert.Contains("Turgid", body);
            StringAssert.Contains("plasmolysed", body);
            StringAssert.Contains("you said 11 %", body);
            StringAssert.Contains("graph gives 12 %", body);
            var saved = SaveService.Load();
            Assert.IsTrue(saved.hasBestIsotonic);
            Assert.AreEqual(11f, saved.bestIsotonic);
            Assert.AreEqual(3, saved.readings.Count);
        }

        [UnityTest]
        public IEnumerator WrongIsotonicRejected()
        {
            Seed(true, R(0, 50, 53.6f), R(20, 49, 46.6f), R(40, 51, 45f));
            yield return OpenViaButton();
            Guess.value = 30f;
            Child<Button>(view, "Card/Question/CheckBtn").onClick.Invoke();
            StringAssert.Contains("Look again", Message.text);
            Assert.IsFalse(ResultCard.activeSelf);
        }

        [UnityTest]
        public IEnumerator ReadingsThatNeverCrossZero()
        {
            Seed(true, R(0, 50, 53.6f), R(5, 50, 51.5f), R(8, 50, 50.6f));
            yield return OpenViaButton();
            Assert.IsFalse(Child<Button>(view, "Card/Question/CheckBtn").interactable);
            Assert.IsFalse(Guess.interactable);
            StringAssert.Contains("never cross zero", Message.text);
        }

        [UnityTest]
        public IEnumerator BeforeTheEndNoQuestion()
        {
            Seed(false, R(0, 50, 53.6f));
            yield return OpenViaButton();
            Assert.AreEqual(1, graph.PointCount);
            Assert.IsFalse(view.transform.Find("Card/Question").gameObject.activeSelf);
            Assert.AreEqual("", Child<TMP_Text>(view, "Card/Row2/C0").text, "empty rows stay blank");
        }

        [Test]
        public void ObservationWords()
        {
            StringAssert.StartsWith("Turgid", ReadingsView.Observation(2f));
            StringAssert.StartsWith("No change", ReadingsView.Observation(0.3f));
            StringAssert.StartsWith("Flaccid", ReadingsView.Observation(-3f));
        }
    }
}
