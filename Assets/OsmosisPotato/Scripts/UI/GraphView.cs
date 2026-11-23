using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Osmosis
{
    /// Animated line graph (brief §5): sugar concentration (x, 0–50 %) vs change in length (y, mm), with a zero
    /// line and an optional isotonic marker. One mesh built in OnPopulateMesh: no GameObject per point, no
    /// LayoutGroup. Lives on its own Canvas (canvas splitting) so animating it never rebuilds the HUD.
    public class GraphView : MaskableGraphic
    {
        [SerializeField] TMP_Text[] yLabels;                // top .. bottom: +max, +max/2, 0, -max/2, -max
        [SerializeField] Color lineColor = new Color(0.18f, 0.49f, 0.36f), pointColor = new Color(0.31f, 0.7f, 0.54f),
            axisColor = new Color(0.25f, 0.25f, 0.25f), gridColor = new Color(0.86f, 0.88f, 0.9f), isoColor = new Color(0.9f, 0.22f, 0.21f);
        public const float Pad = 8f;                         // the isotonic slider's handle area uses the same inset
        const float LineWidth = 5f, PointSize = 18f;

        readonly Vector2[] pts = new Vector2[8];
        int count;
        float reveal;                                        // 0..count, animated
        float yMax = 5f;
        float isoX = -1f;

        public int PointCount => count;
        public float RevealedPoints => reveal;
        public float YMax => yMax;

        /// Plot readings (any order); points pop in one by one.
        public void Plot(IReadOnlyList<Reading> readings)
        {
            count = Mathf.Min(readings.Count, pts.Length);
            float big = 0f;
            for (int i = 0; i < count; i++)
            {
                pts[i] = new Vector2(readings[i].concentration, readings[i].Change);
                big = Mathf.Max(big, Mathf.Abs(pts[i].y));
            }
            for (int i = 1; i < count; i++)                  // insertion sort by concentration (n <= 8, no LINQ)
                for (int j = i; j > 0 && pts[j - 1].x > pts[j].x; j--) (pts[j - 1], pts[j]) = (pts[j], pts[j - 1]);

            yMax = Mathf.Max(2f, Mathf.Ceil(big + 0.5f));
            for (int i = 0; i < yLabels.Length; i++)
                yLabels[i].text = (yMax * (1f - i * 2f / (yLabels.Length - 1))).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            isoX = -1f;
            reveal = 0f;
            DOTween.Kill(this);
            DOTween.To(() => reveal, v => { reveal = v; SetVerticesDirty(); }, count, 0.35f * Mathf.Max(1, count))
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this);
            SetVerticesDirty();
        }

        public void MarkIsotonic(float percent) { isoX = percent; SetVerticesDirty(); }

        Vector2 Map(Vector2 p)
        {
            var r = rectTransform.rect;
            return new Vector2(r.xMin + Pad + p.x / OsmosisSolver.MaxPercent * (r.width - 2 * Pad),
                               r.center.y + p.y / yMax * (r.height / 2f - Pad));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int x = 10; x <= 50; x += 10) Line(vh, Map(new Vector2(x, -yMax)), Map(new Vector2(x, yMax)), 2f, gridColor);
            Line(vh, Map(new Vector2(0, -yMax)), Map(new Vector2(0, yMax)), 3f, axisColor);          // y axis
            Line(vh, Map(new Vector2(0, 0)), Map(new Vector2(OsmosisSolver.MaxPercent, 0)), 4f, axisColor); // zero line

            int whole = Mathf.FloorToInt(reveal);
            float part = reveal - whole;
            for (int i = 0; i + 1 < whole; i++)                // segments between fully revealed points
                Line(vh, Map(pts[i]), Map(pts[i + 1]), LineWidth, lineColor);
            if (whole < count && whole > 0)                   // segment growing towards the next point
                Line(vh, Map(pts[whole - 1]), Vector2.Lerp(Map(pts[whole - 1]), Map(pts[whole]), part), LineWidth, lineColor);
            for (int i = 0; i < count && i < Mathf.CeilToInt(reveal); i++)
            {
                float s = i < whole ? 1f : part;              // pop-in
                Square(vh, Map(pts[i]), PointSize * s, pointColor);
            }
            if (isoX >= 0f)
            {
                Line(vh, Map(new Vector2(isoX, -yMax)), Map(new Vector2(isoX, yMax)), 4f, isoColor);
                Square(vh, Map(new Vector2(isoX, 0f)), PointSize * 1.2f, isoColor);
            }
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            Quad(vh, a - n, a + n, b + n, b - n, c);
        }

        static void Square(VertexHelper vh, Vector2 c, float s, Color col)
        {
            float h = s * 0.5f;
            Quad(vh, c + new Vector2(-h, -h), c + new Vector2(-h, h), c + new Vector2(h, h), c + new Vector2(h, -h), col);
        }

        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero);
            vh.AddVert(c, col, Vector2.zero); vh.AddVert(d, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
