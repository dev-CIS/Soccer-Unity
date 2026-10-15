using FStudio.Database;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Kit preview: the kit's mask render baked with the kit colours,
    /// or a two-tone vector shirt as a fallback.
    /// </summary>
    public class CISKitView : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISKitView, UxmlTraits> { }

        private bool drawFallback;
        private Color primary = Color.white;
        private Color secondary = Color.gray;

        public CISKitView() {
            AddToClassList("cis-kit");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += DrawFallback;
        }

        public void SetKit(KitEntry kit) {
            if (kit == null) {
                drawFallback = false;
                style.backgroundImage = new StyleBackground(StyleKeyword.None);
                MarkDirtyRepaint();
                return;
            }

            primary = CISTeamInfo.Opaque(kit.Color1);
            secondary = CISTeamInfo.Opaque(kit.Color2);

            var baked = kit.PreviewTexture != null
                ? CISTextureBaker.Colorize(kit.PreviewTexture, primary, secondary, 512)
                : null;

            if (baked != null) {
                drawFallback = false;
                style.backgroundImage = new StyleBackground(Background.FromRenderTexture(baked));
            } else {
                drawFallback = true;
                style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }

            MarkDirtyRepaint();
        }

        private void DrawFallback(MeshGenerationContext context) {
            if (!drawFallback) {
                return;
            }

            var r = contentRect;
            if (r.width <= 0 || r.height <= 0) {
                return;
            }

            var p = context.painter2D;
            var s = Mathf.Min(r.width, r.height) * 0.8f;
            var o = new Vector2(r.x + (r.width - s) * 0.5f, r.y + (r.height - s) * 0.5f);
            Vector2 P(float x, float y) => o + new Vector2(x * s, y * s);

            // body
            p.BeginPath();
            p.MoveTo(P(0.36f, 0.12f));
            p.LineTo(P(0.5f, 0.22f));
            p.LineTo(P(0.64f, 0.12f));
            p.LineTo(P(0.92f, 0.28f));
            p.LineTo(P(0.8f, 0.48f));
            p.LineTo(P(0.72f, 0.42f));
            p.LineTo(P(0.72f, 0.9f));
            p.LineTo(P(0.28f, 0.9f));
            p.LineTo(P(0.28f, 0.42f));
            p.LineTo(P(0.2f, 0.48f));
            p.LineTo(P(0.08f, 0.28f));
            p.ClosePath();
            p.fillColor = primary;
            p.Fill();

            // sleeves + collar trim
            p.strokeColor = secondary;
            p.lineWidth = Mathf.Max(2f, s * 0.04f);
            p.lineJoin = LineJoin.Round;
            p.Stroke();

            p.BeginPath();
            p.MoveTo(P(0.44f, 0.4f));
            p.LineTo(P(0.44f, 0.9f));
            p.MoveTo(P(0.56f, 0.4f));
            p.LineTo(P(0.56f, 0.9f));
            p.lineWidth = Mathf.Max(2f, s * 0.05f);
            p.Stroke();
        }
    }
}
