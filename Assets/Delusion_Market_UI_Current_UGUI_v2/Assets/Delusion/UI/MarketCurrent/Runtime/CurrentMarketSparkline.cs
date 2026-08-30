using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Delusion.UI.MarketCurrent
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CurrentMarketSparkline : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float thickness = 2.5f;
        [SerializeField] private Color gridColor = new Color(0.78f, 0.77f, 0.73f, 0.48f);
        private readonly List<float> values = new List<float>();

        public void SetValues(IReadOnlyList<float> source, Color lineColor)
        {
            values.Clear();
            if (source != null)
            {
                for (var i = 0; i < source.Count; i++)
                    values.Add(source[i]);
            }
            color = lineColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            for (var i = 1; i <= 3; i++)
            {
                var y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
                AddSegment(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1f, gridColor);
            }

            if (values.Count < 2)
                return;

            var min = values[0];
            var max = values[0];
            for (var i = 1; i < values.Count; i++)
            {
                min = Mathf.Min(min, values[i]);
                max = Mathf.Max(max, values[i]);
            }

            var span = Mathf.Max(max - min, 0.001f);
            var previous = Vector2.zero;
            const float inset = 5f;
            for (var i = 0; i < values.Count; i++)
            {
                var x = Mathf.Lerp(rect.xMin + inset, rect.xMax - inset, i / (values.Count - 1f));
                var y = Mathf.Lerp(rect.yMin + inset, rect.yMax - inset, (values[i] - min) / span);
                var point = new Vector2(x, y);
                if (i > 0)
                    AddSegment(vh, previous, point, thickness, color);
                previous = point;
            }
            AddDisc(vh, previous, thickness * 1.65f, color);
        }

        private static void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 color)
        {
            var direction = (b - a).normalized;
            var normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            var index = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color32 color)
        {
            const int segments = 12;
            var centerIndex = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0)
                    vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }
    }
}
