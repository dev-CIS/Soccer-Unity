using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Vertical football pitch (attacking upwards) drawn with Painter2D,
    /// with lineup markers placed on top.
    /// </summary>
    public class CISPitch : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISPitch, UxmlTraits> { }

        public struct Marker {
            public Vector2 position;   // x: 0 left .. 1 right, y: 0 own goal .. 1 opponent goal
            public string number;
            public string positionLabel;
            public Color fill;
            public Color outline;
            public Color lineColor;
        }

        private static readonly Color StripeA = new Color32(0x0E, 0x33, 0x26, 0xFF);
        private static readonly Color StripeB = new Color32(0x10, 0x3A, 0x2B, 0xFF);
        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.28f);

        private readonly VisualElement markerLayer;
        private readonly List<VisualElement> markers = new List<VisualElement>();

        public CISPitch() {
            AddToClassList("cis-pitch");
            pickingMode = PickingMode.Ignore;

            markerLayer = CISUI.Element("cis-pitch__markers cis-fill", this);

            generateVisualContent += Draw;
        }

        public void SetMarkers(IList<Marker> lineup) {
            markerLayer.Clear();
            markers.Clear();

            for (int i = 0; i < lineup.Count; i++) {
                var data = lineup[i];

                var marker = CISUI.Element("cis-pitch__marker", markerLayer);
                marker.style.left = Length.Percent(data.position.x * 100f);
                marker.style.top = Length.Percent((1f - data.position.y) * 100f);
                marker.style.backgroundColor = data.fill;
                marker.style.borderTopColor = data.outline;
                marker.style.borderBottomColor = data.outline;
                marker.style.borderLeftColor = data.outline;
                marker.style.borderRightColor = data.outline;

                var number = CISUI.Text(data.number, "cis-pitch__number", marker);
                number.style.color = CISTeamInfo.InkFor(data.fill);

                var tag = CISUI.Text(data.positionLabel, "cis-pitch__tag", marker);
                tag.style.backgroundColor = data.lineColor;

                // stagger the entrance animation.
                var delay = 60 + i * 35;
                marker.schedule.Execute(() => marker.AddToClassList("cis-pitch__marker--in")).StartingIn(delay);

                markers.Add(marker);
            }
        }

        private void Draw(MeshGenerationContext context) {
            var r = contentRect;
            if (r.width <= 0 || r.height <= 0) {
                return;
            }

            var p = context.painter2D;

            // grass stripes
            const int stripes = 10;
            var stripeHeight = r.height / stripes;
            for (int i = 0; i < stripes; i++) {
                p.fillColor = i % 2 == 0 ? StripeA : StripeB;
                p.BeginPath();
                var y0 = r.y + i * stripeHeight;
                p.MoveTo(new Vector2(r.x, y0));
                p.LineTo(new Vector2(r.xMax, y0));
                p.LineTo(new Vector2(r.xMax, y0 + stripeHeight + 0.5f));
                p.LineTo(new Vector2(r.x, y0 + stripeHeight + 0.5f));
                p.ClosePath();
                p.Fill();
            }

            var inset = Mathf.Max(6f, r.width * 0.035f);
            var field = new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2);

            p.strokeColor = LineColor;
            p.lineWidth = 2f;
            p.lineJoin = LineJoin.Miter;

            // outline + halfway line
            StrokeRect(p, field);
            p.BeginPath();
            p.MoveTo(new Vector2(field.x, field.center.y));
            p.LineTo(new Vector2(field.xMax, field.center.y));
            p.Stroke();

            // centre circle + spot
            p.BeginPath();
            p.Arc(field.center, field.width * 0.15f, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();
            Dot(p, field.center, 3f);

            // penalty + goal areas, both ends
            var boxW = field.width * 0.58f;
            var boxH = field.height * 0.15f;
            var goalW = field.width * 0.28f;
            var goalH = field.height * 0.055f;

            StrokeRect(p, new Rect(field.center.x - boxW / 2, field.y, boxW, boxH));
            StrokeRect(p, new Rect(field.center.x - goalW / 2, field.y, goalW, goalH));
            StrokeRect(p, new Rect(field.center.x - boxW / 2, field.yMax - boxH, boxW, boxH));
            StrokeRect(p, new Rect(field.center.x - goalW / 2, field.yMax - goalH, goalW, goalH));

            // penalty arcs
            p.BeginPath();
            p.Arc(new Vector2(field.center.x, field.y + boxH * 0.72f), field.width * 0.12f, Angle.Degrees(35), Angle.Degrees(145));
            p.Stroke();
            p.BeginPath();
            p.Arc(new Vector2(field.center.x, field.yMax - boxH * 0.72f), field.width * 0.12f, Angle.Degrees(215), Angle.Degrees(325));
            p.Stroke();
        }

        private static void StrokeRect(Painter2D p, Rect rect) {
            p.BeginPath();
            p.MoveTo(new Vector2(rect.x, rect.y));
            p.LineTo(new Vector2(rect.xMax, rect.y));
            p.LineTo(new Vector2(rect.xMax, rect.yMax));
            p.LineTo(new Vector2(rect.x, rect.yMax));
            p.ClosePath();
            p.Stroke();
        }

        private static void Dot(Painter2D p, Vector2 center, float radius) {
            p.fillColor = LineColor;
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }
    }
}
