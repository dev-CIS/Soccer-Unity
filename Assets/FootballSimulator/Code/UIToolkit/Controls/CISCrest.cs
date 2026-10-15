using FStudio.Database;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Team crest. Uses the team's real logo mask, baked with its colours;
    /// falls back to a vector shield with initials if baking isn't possible.
    /// </summary>
    public class CISCrest : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISCrest, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {
            private readonly UxmlIntAttributeDescription sizeAttribute =
                new UxmlIntAttributeDescription { name = "bake-size", defaultValue = 256 };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((CISCrest)ve).bakeSize = sizeAttribute.GetValueFromBag(bag, cc);
            }
        }

        public int bakeSize = 256;

        private readonly Label initials;
        private bool drawFallback;
        private Color primary = CISGradients.Accent;
        private Color secondary = CISGradients.Accent2;

        public CISCrest() {
            AddToClassList("cis-crest");
            pickingMode = PickingMode.Ignore;

            initials = CISUI.Text(string.Empty, "cis-crest__initials", this);
            initials.style.display = DisplayStyle.None;

            generateVisualContent += DrawFallback;
            RegisterCallback<GeometryChangedEvent>(evt => {
                initials.style.fontSize = Mathf.Max(10f, evt.newRect.height * 0.32f);
            });
        }

        public void SetTeam(TeamEntry team) {
            var logo = team != null ? team.TeamLogo : null;

            primary = logo != null ? CISTeamInfo.Opaque(logo.TeamLogoColor1) : CISTeamInfo.PrimaryColor(team);
            secondary = logo != null ? CISTeamInfo.Opaque(logo.TeamLogoColor2) : CISTeamInfo.SecondaryColor(team);

            RenderTexture baked = null;
            if (logo != null && logo.TeamLogoMaterial != null) {
                baked = CISTextureBaker.Colorize(logo.TeamLogoMaterial, primary, secondary, bakeSize);
            }

            if (baked != null) {
                drawFallback = false;
                style.backgroundImage = new StyleBackground(Background.FromRenderTexture(baked));
                initials.style.display = DisplayStyle.None;
            } else {
                drawFallback = true;
                style.backgroundImage = new StyleBackground(StyleKeyword.None);
                initials.text = CISTeamInfo.Initials(team);
                initials.style.color = CISTeamInfo.InkFor(primary);
                initials.style.display = DisplayStyle.Flex;
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
            var s = Mathf.Min(r.width, r.height);
            var o = new Vector2(r.x + (r.width - s) * 0.5f, r.y + (r.height - s) * 0.5f);
            Vector2 P(float x, float y) => o + new Vector2(x * s, y * s);

            p.BeginPath();
            p.MoveTo(P(0.14f, 0.08f));
            p.LineTo(P(0.86f, 0.08f));
            p.LineTo(P(0.86f, 0.52f));
            p.BezierCurveTo(P(0.86f, 0.76f), P(0.66f, 0.88f), P(0.5f, 0.95f));
            p.BezierCurveTo(P(0.34f, 0.88f), P(0.14f, 0.76f), P(0.14f, 0.52f));
            p.ClosePath();
            p.fillColor = primary;
            p.Fill();

            p.strokeColor = secondary;
            p.lineWidth = Mathf.Max(2f, s * 0.05f);
            p.Stroke();
        }
    }
}
