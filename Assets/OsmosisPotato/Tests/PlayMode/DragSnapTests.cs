using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Osmosis.Tests
{
    /// Task 4.3: shelf spawn -> drag -> ghost snap / return, with simulated touches in the real LabScene.
    public class DragSnapTests
    {
        Camera cam;
        InventorySlot potatoSlot, beakerSlot;
        SnapTarget ruler, beaker1;
        int snapped;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene(Scenes.Lab);
            yield return null;
            yield return null;
            cam = Camera.main;
            potatoSlot = Find<InventorySlot>("PotatoSlot");
            beakerSlot = Find<InventorySlot>("BeakerSlot");
            ruler = Find<SnapTarget>("RulerTarget");
            beaker1 = Find<SnapTarget>("BeakerTarget1");
            // Drag mechanics in isolation: stop the lab flow and reset to an idle bench.
            var flow = Object.FindFirstObjectByType<LabFlow>();
            if (flow) flow.enabled = false;
            potatoSlot.Lock(); beakerSlot.Lock();
            foreach (var t in Object.FindObjectsByType<SnapTarget>(FindObjectsSortMode.None)) t.Hide();
            snapped = 0;
            SnapTarget.Snapped += Count;
        }

        [TearDown] public void Unhook() => SnapTarget.Snapped -= Count;
        void Count(SnapTarget t, Draggable d) => snapped++;

        static T Find<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c;
            Assert.Fail(name + " not found");
            return null;
        }

        static PointerEventData Pe(Vector2 p) => new PointerEventData(EventSystem.current) { position = p };
        Vector2 Screen(Vector3 world) => cam.WorldToScreenPoint(new Vector3(world.x, Draggable.CarryHeight, world.z));
        static Vector2 SlotScreen(InventorySlot s) => RectTransformUtility.WorldToScreenPoint(null, s.transform.position);
        static int Items() => Object.FindObjectsByType<Draggable>(FindObjectsSortMode.None).Length;

        /// Touch the slot, drag over several frames to `to`, release.
        IEnumerator DragFromSlot(InventorySlot slot, Vector2 to)
        {
            var from = SlotScreen(slot);
            slot.OnPointerDown(Pe(from));
            for (int f = 1; f <= 25; f++)
            {
                slot.OnDrag(Pe(Vector2.Lerp(from, to, f / 15f)));
                yield return null;
            }
            var up = Pe(to); up.dragging = true;
            slot.OnEndDrag(up);
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator LockedSlotSpawnsNothing()
        {
            potatoSlot.OnPointerDown(Pe(SlotScreen(potatoSlot)));
            yield return null;
            Assert.AreEqual(0, Items());
        }

        [UnityTest]
        public IEnumerator StripSnapsOntoShownRulerGhost()
        {
            ruler.Show(); potatoSlot.Unlock();
            yield return DragFromSlot(potatoSlot, Screen(ruler.transform.position));

            Assert.IsNotNull(ruler.Occupant, "snapped");
            Assert.AreEqual(1, snapped, "Snapped fired once");
            Assert.Less(Vector3.Distance(ruler.Occupant.transform.position, ruler.transform.position), 0.01f, "sits on the target");
            Assert.IsTrue(potatoSlot.transform.Find("Tick").gameObject.activeSelf, "tick on shelf");
            Assert.IsFalse(potatoSlot.Unlocked, "slot closes after placing");
        }

        [UnityTest]
        public IEnumerator MissSlidesBackAndVanishesSlotStaysOpen()
        {
            ruler.Show(); potatoSlot.Unlock();
            yield return DragFromSlot(potatoSlot, Screen(new Vector3(-0.2f, 0, 0.6f)));   // empty back of the bench, far from every target
            Assert.AreEqual(0, Items(), "spawned strip removed");
            Assert.AreEqual(0, snapped);
            Assert.IsTrue(potatoSlot.Unlocked, "can try again");
        }

        [UnityTest]
        public IEnumerator HiddenTargetIgnored()
        {
            potatoSlot.Unlock();                                                           // ruler ghost NOT shown
            yield return DragFromSlot(potatoSlot, Screen(ruler.transform.position));
            Assert.IsNull(ruler.Occupant);
            Assert.AreEqual(0, Items());
        }

        [UnityTest]
        public IEnumerator WrongKindNotAccepted()
        {
            ruler.Show(); beakerSlot.Unlock();
            yield return DragFromSlot(beakerSlot, Screen(ruler.transform.position));
            Assert.IsNull(ruler.Occupant, "beaker can't go on the ruler target");
            Assert.AreEqual(0, Items());
        }

        [UnityTest]
        public IEnumerator FastDoubleTapSpawnsOne()
        {
            potatoSlot.Unlock();
            var p = Pe(SlotScreen(potatoSlot));
            potatoSlot.OnPointerDown(p);
            potatoSlot.OnPointerDown(p);
            yield return null;
            Assert.AreEqual(1, Items());
        }

        [UnityTest]
        public IEnumerator PlacedStripCanBeMovedIntoBeaker()
        {
            ruler.Show(); potatoSlot.Unlock();
            yield return DragFromSlot(potatoSlot, Screen(ruler.transform.position));
            beaker1.Show(); beakerSlot.Unlock();
            yield return DragFromSlot(beakerSlot, Screen(beaker1.transform.position));
            Assert.IsNotNull(beaker1.Occupant, "beaker placed");

            var inside = beaker1.Occupant.GetComponentInChildren<SnapTarget>();
            inside.Show();
            var strip = ruler.Occupant;
            var from = (Vector2)cam.WorldToScreenPoint(strip.transform.position);
            var to = Screen(inside.transform.position);
            strip.OnBeginDrag(Pe(from));
            for (int f = 1; f <= 25; f++) { strip.OnDrag(Pe(Vector2.Lerp(from, to, f / 15f))); yield return null; }
            strip.OnEndDrag(Pe(to));
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(strip, inside.Occupant, "strip now in the beaker");
            Assert.IsNull(ruler.Occupant, "ruler freed");
            Assert.AreEqual(3, snapped, "ruler, beaker, strip-in-beaker");
        }

        [UnityTest]
        public IEnumerator RedragMissGoesBackWithoutNewEvent()
        {
            ruler.Show(); potatoSlot.Unlock();
            yield return DragFromSlot(potatoSlot, Screen(ruler.transform.position));
            var strip = ruler.Occupant;
            var from = (Vector2)cam.WorldToScreenPoint(strip.transform.position);
            strip.OnBeginDrag(Pe(from));
            for (int f = 1; f <= 10; f++) { strip.OnDrag(Pe(from + new Vector2(400, 0) * f / 10f)); yield return null; }
            strip.OnEndDrag(Pe(from + new Vector2(400, 0)));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(strip, ruler.Occupant, "back on the ruler");
            Assert.Less(Vector3.Distance(strip.transform.position, ruler.transform.position), 0.01f);
            Assert.AreEqual(1, snapped, "no extra Snapped event");
        }
    }
}
