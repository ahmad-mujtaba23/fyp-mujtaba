using System;
using System.Collections.Generic;
using UnityEngine;

namespace Osmosis
{
    /// Where an apparatus goes (port of the reference "shadow" ghosts + GuidanceCollisionHandler).
    /// While Shown, a translucent green ghost of the item pulses here; a matching Draggable dropped within
    /// reach snaps in and Snapped fires (the lab flow listens). One occupant at a time.
    public class SnapTarget : MonoBehaviour
    {
        public ItemKind accepts;
        [SerializeField] Renderer ghost;              // child mesh with the Ghost material, no collider

        public static event Action<SnapTarget, Draggable> Snapped;
        public Draggable Occupant { get; private set; }
        public bool Shown { get; private set; }

        static readonly List<SnapTarget> active = new List<SnapTarget>();
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        Color baseColor;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            baseColor = ghost.sharedMaterial.GetColor(ColorId);
            Hide();
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// Show the ghost and accept drops (the lab flow calls this for the current step).
        public void Show() { Shown = true; ghost.enabled = Occupant == null; }
        public void Hide() { Shown = false; ghost.enabled = false; }

        internal void Accept(Draggable d)
        {
            Occupant = d;
            ghost.enabled = false;
            Snapped?.Invoke(this, d);
        }

        internal void Release(Draggable d)
        {
            if (Occupant != d) return;
            Occupant = null;
            if (Shown) ghost.enabled = true;
        }

        /// Nearest shown, free target of the right kind within `reach` (XZ distance), else null.
        public static SnapTarget Nearest(Draggable d, float reach)
        {
            SnapTarget best = null;
            float bestSq = reach * reach;
            Vector3 p = d.transform.position;
            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                if (!t.Shown || t.accepts != d.kind || (t.Occupant && t.Occupant != d)) continue;
                Vector3 q = t.transform.position;
                float dx = p.x - q.x, dz = p.z - q.z, sq = dx * dx + dz * dz;
                if (sq < bestSq) { bestSq = sq; best = t; }
            }
            return best;
        }

        void Update()
        {
            if (!ghost.enabled) return;
            var c = baseColor;
            c.a = baseColor.a * (0.55f + 0.45f * Mathf.Sin(Time.time * 4f));   // pulse, no allocation
            block.SetColor(ColorId, c);
            ghost.SetPropertyBlock(block);
        }
    }
}
