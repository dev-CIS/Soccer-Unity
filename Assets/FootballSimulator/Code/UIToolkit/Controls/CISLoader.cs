using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Spinning arc loading indicator. Colour from --ring-color.
    /// </summary>
    public class CISLoader : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISLoader, UxmlTraits> { }

        private static readonly CustomStyleProperty<Color> RingColorProperty = new CustomStyleProperty<Color>("--ring-color");

        private Color color = CISGradients.Accent;
        private float angle;

        public CISLoader() {
            AddToClassList("cis-loader");
            pickingMode = PickingMode.Ignore;

            generateVisualContent += Draw;

            RegisterCallback<CustomStyleResolvedEvent>(evt => {
                if (evt.customStyle.TryGetValue(RingColorProperty, out var resolved)) {
                    color = resolved;
                    MarkDirtyRepaint();
                }
            });

            schedule.Execute(() => {
                if (resolvedStyle.display == DisplayStyle.None) {
                    return;
                }

                angle = (angle + 7.5f) % 360f;
                style.rotate = new Rotate(Angle.Degrees(angle));
            }).Every(16);
        }

        private void Draw(MeshGenerationContext context) {
            var r = contentRect;
            if (r.width <= 0 || r.height <= 0) {
                return;
            }

            var p = context.painter2D;
            var radius = Mathf.Min(r.width, r.height) * 0.5f;
            var thickness = Mathf.Max(2f, radius * 0.16f);
            var center = r.center;

            p.lineWidth = thickness;
            p.lineCap = LineCap.Round;

            p.strokeColor = new Color(color.r, color.g, color.b, 0.15f);
            p.BeginPath();
            p.Arc(center, radius - thickness, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            p.strokeColor = color;
            p.BeginPath();
            p.Arc(center, radius - thickness, Angle.Degrees(-90), Angle.Degrees(160));
            p.Stroke();
        }
    }
}
