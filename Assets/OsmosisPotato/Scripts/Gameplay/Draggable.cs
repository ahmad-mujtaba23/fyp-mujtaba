using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Osmosis
{
    public enum ItemKind { PotatoStrip, Beaker }

    /// A 3D apparatus the student drags across the bench (port of DragDropHandler + PositionHandler + MovementHandler).
    /// Follows the finger on a carry plane, then on release snaps (spring) onto the nearest matching SnapTarget
    /// or slides back home. Spawned items that never got placed are destroyed and their shelf slot reopens.
    /// Already-placed items can be dragged again through the camera's PhysicsRaycaster.
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ItemKind kind;
        public bool canDrag = true;

        public const float CarryHeight = 0.45f;            // above the beaker rim, so carried items never pass through glass
        const float SnapReach = 0.32f, FollowSharpness = 18f;
        // ponytail: bench bounds hard-coded to the LabBuilder bench; expose if the bench ever moves
        static readonly Vector2 MinXZ = new Vector2(-1.55f, -0.3f), MaxXZ = new Vector2(1.55f, 0.85f);

        public SnapTarget Current { get; private set; }
        public bool Dragging { get; private set; }
        internal InventorySlot origin;                 // set when spawned from the shelf, until first placed

        Camera cam;
        Vector3 followTarget;
        Vector3 homePos;
        Quaternion homeRot;

        void Awake()
        {
            cam = Camera.main;                          // cached once; never Camera.main per frame
            homePos = transform.position;
            homeRot = transform.rotation;
        }

        /// Shelf spawn: appear under the finger, already being dragged.
        public void SpawnAt(Vector2 screen)
        {
            transform.position = followTarget = PointOnBench(screen);
            homePos = transform.position;
            Dragging = true;
        }

        public void BeginDrag(Vector2 screen)
        {
            if (!canDrag) return;
            transform.DOKill();
            Dragging = true;
            followTarget = PointOnBench(screen);
        }

        public void Drag(Vector2 screen)
        {
            if (Dragging) followTarget = PointOnBench(screen);
        }

        public void EndDrag()
        {
            if (!Dragging) return;
            Dragging = false;
            var target = SnapTarget.Nearest(this, SnapReach);
            if (target && target != Current) SnapTo(target);
            else ReturnHome();                          // miss, or dropped back where it was: no new event
        }

        void Update()
        {
            if (!Dragging) return;
            float k = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);   // frame-rate independent spring-ish follow
            transform.position = Vector3.Lerp(transform.position, followTarget, k);
        }

        public void SnapTo(SnapTarget target)
        {
            if (Current) Current.Release(this);
            Current = target;
            homePos = target.transform.position;
            homeRot = target.transform.rotation;
            transform.DOKill();
            transform.DOMove(homePos, 0.35f).SetEase(Ease.OutBack);
            transform.DORotateQuaternion(homeRot, 0.25f);
            Sfx.Play(Sfx.Snap);
            if (origin) { origin.Placed(); origin = null; }
            target.Accept(this);
        }

        void ReturnHome()
        {
            transform.DOKill();
            var t = transform.DOMove(homePos, 0.3f).SetEase(Ease.OutQuad);
            transform.DORotateQuaternion(homeRot, 0.2f);
            if (origin && !Current)                     // fresh spawn that missed: vanish, the slot stays unlocked
                t.OnComplete(() => Destroy(gameObject));
        }

        Vector3 PointOnBench(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0f, CarryHeight, 0f));
            Vector3 p = plane.Raycast(ray, out float d) && d < 6f ? ray.GetPoint(d) : ray.GetPoint(2.2f);
            p.x = Mathf.Clamp(p.x, MinXZ.x, MaxXZ.x);
            p.z = Mathf.Clamp(p.z, MinXZ.y, MaxXZ.y);
            p.y = CarryHeight;
            return p;
        }

        // Direct touches on an already-placed item (PhysicsRaycaster on the camera).
        public void OnBeginDrag(PointerEventData e) => BeginDrag(e.position);
        public void OnDrag(PointerEventData e) => Drag(e.position);
        public void OnEndDrag(PointerEventData e) => EndDrag();

        void OnDestroy() => transform.DOKill();
    }
}
