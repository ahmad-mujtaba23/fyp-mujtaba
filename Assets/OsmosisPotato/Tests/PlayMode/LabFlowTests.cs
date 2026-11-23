using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Osmosis.Tests
{
    /// Task 4.8 (flow skeleton): the lab is playable with mouse/touch from the first frame and runs 3 full strips.
    public class LabFlowTests
    {
        LabFlow flow;
        Camera cam;
        InventorySlot potatoSlot, beakerSlot;
        SnapTarget ruler;
        TMP_Text board;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            flow = Object.FindFirstObjectByType<LabFlow>();
            cam = Camera.main;
            potatoSlot = Find<InventorySlot>("PotatoSlot");
            beakerSlot = Find<InventorySlot>("BeakerSlot");
            ruler = Find<SnapTarget>("RulerTarget");
            board = GameObject.Find("BoardText").GetComponent<TMP_Text>();
            var tl = Object.FindFirstObjectByType<TimeLapseManager>();
            typeof(TimeLapseManager).GetField("realSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tl, 0.3f);
        }

        static T Find<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c;
            Assert.Fail(name + " not found");
            return null;
        }

        static PointerEventData Pe(Vector2 p) => new PointerEventData(EventSystem.current) { position = p };
        Vector2 Screen(Vector3 w) => cam.WorldToScreenPoint(new Vector3(w.x, Draggable.CarryHeight, w.z));
        static Vector2 SlotScreen(InventorySlot s) => RectTransformUtility.WorldToScreenPoint(null, s.transform.position);

        /// Type `mm` (1 decimal) into the measuring card and press Record, like the student does.
        IEnumerator Measure(float mm)
        {
            yield return new WaitForSeconds(0.7f);                                       // camera zoom
            var onRuler = ruler.Occupant.GetComponent<PotatoStripController>();
            float leftEnd = ruler.transform.position.x - onRuler.LengthMm * 0.5f * PotatoStripController.UnitsPerMm;
            Assert.AreEqual(leftEnd, GameObject.Find("cm0").transform.position.x, 0.002f, "ruler 0 mark at the strip's end after a real drag");
            var card = Find<RectTransform>("MeasurePanel");
            Assert.IsTrue(card.gameObject.activeSelf, "measuring card open");
            card.Find("Display/Value").GetComponent<TMP_Text>().text = mm.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            card.Find("RecordBtn").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(0.6f);                                       // camera zooms back out
        }

        /// Real pointer path for a 3D item: raycast like the mouse does, check it hits `item`, then drag it to `to`.
        IEnumerator DragItem(Draggable item, Vector2 to)
        {
            Vector2 from = cam.WorldToScreenPoint(item.transform.position);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(Pe(from), hits);
            Assert.IsNotEmpty(hits, "pointer hits nothing at the item");
            var handler = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
            Assert.AreEqual(item.gameObject, handler, "pointer reaches the item, not " + hits[0].gameObject.name);
            ExecuteEvents.Execute<IBeginDragHandler>(handler, Pe(from), (h, e) => h.OnBeginDrag((PointerEventData)e));
            for (int f = 1; f <= 20; f++)
            {
                var p = Pe(Vector2.Lerp(from, to, f / 12f));
                ExecuteEvents.Execute<IDragHandler>(handler, p, (h, e) => h.OnDrag((PointerEventData)e));
                yield return null;
            }
            ExecuteEvents.Execute<IEndDragHandler>(handler, Pe(to), (h, e) => h.OnEndDrag((PointerEventData)e));
            yield return new WaitForSeconds(0.45f);
        }

        static float ReadTo05(float mm) => Mathf.Round(mm * 2f) / 2f;                   // a student's reading to the nearest half mm

        IEnumerator DragFromSlot(InventorySlot slot, Vector2 to)
        {
            var from = SlotScreen(slot);
            slot.OnPointerDown(Pe(from));
            for (int f = 1; f <= 20; f++) { slot.OnDrag(Pe(Vector2.Lerp(from, to, f / 12f))); yield return null; }
            var up = Pe(to); up.dragging = true;
            slot.OnEndDrag(up);
            yield return new WaitForSeconds(0.45f);
        }

        [UnityTest]
        public IEnumerator MouseReachesAnUnlockedPotatoSlotOnFirstFrame()
        {
            yield return null;
            Assert.AreEqual(LabFlow.Step.StripOnRuler, flow.Current);
            Assert.IsTrue(potatoSlot.Unlocked, "potato slot open at start");
            Assert.IsFalse(beakerSlot.Unlocked, "beaker waits for its step");
            Assert.IsTrue(ruler.Shown, "ruler ghost shown");

            // the real EventSystem raycast (what the mouse uses) lands on something handled by the slot
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(Pe(SlotScreen(potatoSlot)), hits);
            Assert.IsNotEmpty(hits, "nothing under the pointer");
            var handler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject);
            Assert.AreEqual(potatoSlot.gameObject, handler, "pointer-down reaches the potato slot, got " + hits[0].gameObject.name);
        }

        [UnityTest]
        public IEnumerator LockedBeakerSlotIgnoredAtStart()
        {
            beakerSlot.OnPointerDown(Pe(SlotScreen(beakerSlot)));
            yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<BeakerController>());
        }

        [UnityTest]
        public IEnumerator ThreeStripsEndToEnd()
        {
            var panel = Find<ConcentrationPanel>("ConcentrationPanel");
            var targets = new[] { Find<SnapTarget>("BeakerTarget1"), Find<SnapTarget>("BeakerTarget2"), Find<SnapTarget>("BeakerTarget3") };
            var ff = Find<Button>("FastForwardBtn");
            float[] conc = { 0f, 20f, 40f };

            for (int c = 0; c < LabFlow.Strips; c++)
            {
                yield return new WaitForSeconds(1.4f);                                   // previous strip put aside, camera back
                StringAssert.Contains($"Strip {c + 1} of 3", board.text);
                Assert.AreEqual(LabFlow.Step.StripOnRuler, flow.Current);
                yield return DragFromSlot(potatoSlot, Screen(ruler.transform.position));
                Assert.AreEqual(LabFlow.Step.MeasureInitial, flow.Current, "strip on ruler");
                var placed = ruler.Occupant.GetComponent<PotatoStripController>();
                float initialRead = ReadTo05(placed.LengthMm);
                yield return Measure(initialRead);
                Assert.AreEqual(LabFlow.Step.BeakerOnBench, flow.Current, "initial length recorded");
                Assert.IsTrue(beakerSlot.Unlocked);

                yield return DragFromSlot(beakerSlot, Screen(targets[c].transform.position));
                Assert.AreEqual(LabFlow.Step.Fill, flow.Current, "beaker placed");
                Assert.IsTrue(panel.gameObject.activeSelf, "panel opens");

                panel.GetComponentInChildren<Slider>().value = conc[c] / ConcentrationPanel.Step;
                panel.Fill();
                Assert.AreEqual(LabFlow.Step.StripIntoBeaker, flow.Current);

                var strip = ruler.Occupant;
                var beaker = targets[c].Occupant.GetComponent<BeakerController>();
                yield return DragItem(strip, Screen(beaker.StripTarget.transform.position));
                Assert.AreEqual(LabFlow.Step.FastForward, flow.Current, "strip dragged into the beaker");
                Assert.IsTrue(ff.gameObject.activeInHierarchy);

                ff.onClick.Invoke();
                yield return new WaitForSeconds(0.6f);
                Assert.AreEqual(LabFlow.Step.StripBackOnRuler, flow.Current, "time-lapse finished");
                yield return new WaitForSeconds(0.6f);                                   // camera eases back from the beaker close-up

                yield return DragItem(strip, Screen(ruler.transform.position));               // out of the beaker, by pointer
                Assert.AreEqual(LabFlow.Step.MeasureFinal, flow.Current, "strip picked out of the beaker onto the ruler");
                float finalRead = ReadTo05(strip.GetComponent<PotatoStripController>().LengthMm);
                yield return Measure(finalRead);
                Assert.AreEqual(c + 1, flow.Readings.Count);
                var r = flow.Readings[c];
                Assert.AreEqual(conc[c], r.concentration);
                Assert.AreEqual(initialRead, r.initial, 1e-4f, "student's own initial reading");
                Assert.AreEqual(finalRead, r.final, 1e-4f, "student's own final reading");
            }

            Assert.AreEqual(LabFlow.Step.Done, flow.Current);
            Assert.IsTrue(Find<PulseScale>("ReadingsBtn").enabled, "Readings button invites a tap");
            Assert.Greater(flow.Readings[0].Change, 0f, "0 %: longer");
            Assert.Less(flow.Readings[2].Change, 0f, "40 %: shorter");
            StringAssert.Contains("Readings", board.text);
            Assert.AreEqual(3, SaveService.Load().readings.Count, "saved");
        }
    }
}
