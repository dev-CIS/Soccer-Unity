using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Shield shaped overall rating badge, coloured gold / silver / bronze by tier.
    /// </summary>
    public class CISRatingBadge : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISRatingBadge, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {
            private readonly UxmlStringAttributeDescription captionAttribute =
                new UxmlStringAttributeDescription { name = "caption", defaultValue = "OVR" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((CISRatingBadge)ve).caption = captionAttribute.GetValueFromBag(bag, cc);
            }
        }

        private readonly Label valueLabel;
        private readonly Label captionLabel;
        private Color fill = CISTeamInfo.Silver;
        private int value;

        public string caption {
            get => captionLabel.text;
            set => captionLabel.text = value;
        }

        public int Value {
            get => value;
            set {
                this.value = value;
                valueLabel.text = value > 0 ? value.ToString() : "--";
                fill = CISTeamInfo.RatingColor(value);
                MarkDirtyRepaint();
            }
        }

        public CISRatingBadge() {
            AddToClassList("cis-rating");
            pickingMode = PickingMode.Ignore;

            valueLabel = CISUI.Text("--", "cis-rating__value", this);
            captionLabel = CISUI.Text("OVR", "cis-rating__caption", this);

            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context) {
            var r = contentRect;
            if (r.width <= 0 || r.height <= 0) {
                return;
            }

            var p = context.painter2D;
            float w = r.width, h = r.height;
            Vector2 P(float x, float y) => new Vector2(r.x + x * w, r.y + y * h);

            p.BeginPath();
            p.MoveTo(P(0.12f, 0f));
            p.LineTo(P(0.88f, 0f));
            p.LineTo(P(1f, 0.12f));
            p.LineTo(P(1f, 0.70f));
            p.LineTo(P(0.5f, 1f));
            p.LineTo(P(0f, 0.70f));
            p.LineTo(P(0f, 0.12f));
            p.ClosePath();
            p.fillColor = fill;
            p.Fill();

            // inner bevel.
            p.BeginPath();
            p.MoveTo(P(0.18f, 0.07f));
            p.LineTo(P(0.82f, 0.07f));
            p.LineTo(P(0.91f, 0.16f));
            p.LineTo(P(0.91f, 0.66f));
            p.LineTo(P(0.5f, 0.91f));
            p.LineTo(P(0.09f, 0.66f));
            p.LineTo(P(0.09f, 0.16f));
            p.ClosePath();
            p.strokeColor = new Color(1f, 1f, 1f, 0.45f);
            p.lineWidth = Mathf.Max(1f, w * 0.03f);
            p.Stroke();
        }
    }
}
