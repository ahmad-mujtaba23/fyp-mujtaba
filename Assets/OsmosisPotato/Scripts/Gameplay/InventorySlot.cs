using UnityEngine;
using UnityEngine.EventSystems;

namespace Osmosis
{
    /// Shelf compartment (port of UIInventoryHandler slots). Unlocked by the lab flow; touching it spawns the
    /// item under the finger and the drag continues on the 3D item. Shows a tick once the item is placed.
    public class InventorySlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] Draggable prefab;
        [SerializeField] GameObject lockOverlay, tick;

        public bool Unlocked { get; private set; }
        Draggable held;

        void Awake() => Lock();

        public void Unlock() { Unlocked = true; lockOverlay.SetActive(false); tick.SetActive(false); }
        public void Lock() { Unlocked = false; lockOverlay.SetActive(true); }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Unlocked || held) return;              // locked, or already holding one (fast double tap)
            held = Instantiate(prefab);
            held.origin = this;
            held.SpawnAt(e.position);
            Sfx.Play(Sfx.Inventory);
        }

        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) { if (held) held.Drag(e.position); }
        public void OnEndDrag(PointerEventData e) => Drop();
        public void OnPointerUp(PointerEventData e) { if (!e.dragging) Drop(); }   // tap without dragging

        void Drop()
        {
            if (!held) return;
            var d = held;
            held = null;
            d.EndDrag();
        }

        /// Item placed: show the tick (not the grey lock) and stop spawning until the flow unlocks it again.
        internal void Placed() { Unlocked = false; tick.SetActive(true); }
    }
}
