using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Resolution independent vector icons drawn with Painter2D.
    /// Colour comes from the --icon-color custom USS property.
    /// </summary>
    public class CISIcon : VisualElement {
        public enum Kind {
            None,
            ChevronLeft,
            ChevronRight,
            ChevronUp,
            ChevronDown,
            Play,
            Pause,
            Back,
            Close,
            Check,
            Swap,
            Gear,
            Gamepad,
            Ball,
            Exit,
            Stats,
            Shirt,
            Users
        }

        public new class UxmlFactory : UxmlFactory<CISIcon, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {
            private readonly UxmlEnumAttributeDescription<Kind> kindAttribute =
                new UxmlEnumAttributeDescription<Kind> { name = "kind", defaultValue = Kind.None };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((CISIcon)ve).kind = kindAttribute.GetValueFromBag(bag, cc);
            }
        }

        private static readonly CustomStyleProperty<Color> IconColorProperty = new CustomStyleProperty<Color>("--icon-color");

        private Kind m_Kind;
        private Color color = Color.white;

        public Kind kind {
            get => m_Kind;
            set {
                m_Kind = value;
                MarkDirtyRepaint();
            }
        }

        public Color IconColor {
            get => color;
            set {
                color = value;
                MarkDirtyRepaint();
            }
        }

        public CISIcon() : this(Kind.None) { }

        public CISIcon(Kind kind) {
            AddToClassList("cis-icon");
            pickingMode = PickingMode.Ignore;
            m_Kind = kind;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
            if (evt.customStyle.TryGetValue(IconColorProperty, out var resolved)) {
                color = resolved;
                MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext context) {
            var rect = contentRect;
            var w = rect.width;
            var h = rect.height;

            if (w <= 0 || h <= 0 || m_Kind == Kind.None) {
                return;
            }

            var p = context.painter2D;
            var s = Mathf.Min(w, h);
            var o = new Vector2(rect.x + (w - s) * 0.5f, rect.y + (h - s) * 0.5f);

            Vector2 P(float x, float y) => o + new Vector2(x * s, y * s);

            p.strokeColor = color;
            p.fillColor = color;
            p.lineWidth = Mathf.Max(1.5f, s * 0.11f);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            switch (m_Kind) {
                case Kind.ChevronLeft:
                    Polyline(p, P(0.64f, 0.18f), P(0.32f, 0.5f), P(0.64f, 0.82f));
                    break;

                case Kind.ChevronRight:
                    Polyline(p, P(0.36f, 0.18f), P(0.68f, 0.5f), P(0.36f, 0.82f));
                    break;

                case Kind.ChevronUp:
                    Polyline(p, P(0.18f, 0.64f), P(0.5f, 0.32f), P(0.82f, 0.64f));
                    break;

                case Kind.ChevronDown:
                    Polyline(p, P(0.18f, 0.36f), P(0.5f, 0.68f), P(0.82f, 0.36f));
                    break;

                case Kind.Play:
                    p.BeginPath();
                    p.MoveTo(P(0.26f, 0.14f));
                    p.LineTo(P(0.86f, 0.5f));
                    p.LineTo(P(0.26f, 0.86f));
                    p.ClosePath();
                    p.Fill();
                    break;

                case Kind.Pause:
                    FillRect(p, P(0.22f, 0.16f), P(0.42f, 0.84f));
                    FillRect(p, P(0.58f, 0.16f), P(0.78f, 0.84f));
                    break;

                case Kind.Back:
                    Polyline(p, P(0.84f, 0.5f), P(0.18f, 0.5f));
                    Polyline(p, P(0.44f, 0.22f), P(0.16f, 0.5f), P(0.44f, 0.78f));
                    break;

                case Kind.Close:
                    Polyline(p, P(0.22f, 0.22f), P(0.78f, 0.78f));
                    Polyline(p, P(0.78f, 0.22f), P(0.22f, 0.78f));
                    break;

                case Kind.Check:
                    Polyline(p, P(0.16f, 0.52f), P(0.4f, 0.76f), P(0.86f, 0.26f));
                    break;

                case Kind.Swap:
                    Polyline(p, P(0.14f, 0.34f), P(0.84f, 0.34f));
                    Polyline(p, P(0.64f, 0.14f), P(0.86f, 0.34f), P(0.64f, 0.54f));
                    Polyline(p, P(0.86f, 0.66f), P(0.16f, 0.66f));
                    Polyline(p, P(0.36f, 0.46f), P(0.14f, 0.66f), P(0.36f, 0.86f));
                    break;

                case Kind.Gear: {
                    var c = P(0.5f, 0.5f);
                    p.BeginPath();
                    p.Arc(c, s * 0.24f, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    for (int i = 0; i < 8; i++) {
                        var a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Polyline(p, c + dir * s * 0.32f, c + dir * s * 0.44f);
                    }
                    break;
                }

                case Kind.Gamepad: {
                    p.lineWidth = Mathf.Max(1.5f, s * 0.09f);
                    p.BeginPath();
                    p.MoveTo(P(0.28f, 0.3f));
                    p.LineTo(P(0.72f, 0.3f));
                    p.BezierCurveTo(P(0.9f, 0.3f), P(0.98f, 0.78f), P(0.84f, 0.8f));
                    p.BezierCurveTo(P(0.74f, 0.82f), P(0.68f, 0.64f), P(0.6f, 0.64f));
                    p.LineTo(P(0.4f, 0.64f));
                    p.BezierCurveTo(P(0.32f, 0.64f), P(0.26f, 0.82f), P(0.16f, 0.8f));
                    p.BezierCurveTo(P(0.02f, 0.78f), P(0.1f, 0.3f), P(0.28f, 0.3f));
                    p.ClosePath();
                    p.Stroke();
                    Polyline(p, P(0.24f, 0.47f), P(0.38f, 0.47f));
                    Polyline(p, P(0.31f, 0.4f), P(0.31f, 0.54f));
                    Dot(p, P(0.66f, 0.43f), s * 0.045f);
                    Dot(p, P(0.74f, 0.52f), s * 0.045f);
                    break;
                }

                case Kind.Ball: {
                    var c = P(0.5f, 0.5f);
                    p.lineWidth = Mathf.Max(1.2f, s * 0.08f);
                    p.BeginPath();
                    p.Arc(c, s * 0.4f, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    p.BeginPath();
                    for (int i = 0; i < 5; i++) {
                        var a = (-90 + i * 72) * Mathf.Deg2Rad;
                        var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.16f;
                        if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                    }
                    p.ClosePath();
                    p.Fill();
                    for (int i = 0; i < 5; i++) {
                        var a = (-90 + i * 72) * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Polyline(p, c + dir * s * 0.16f, c + dir * s * 0.38f);
                    }
                    break;
                }

                case Kind.Exit:
                    Polyline(p, P(0.46f, 0.16f), P(0.18f, 0.16f), P(0.18f, 0.84f), P(0.46f, 0.84f));
                    Polyline(p, P(0.4f, 0.5f), P(0.86f, 0.5f));
                    Polyline(p, P(0.66f, 0.3f), P(0.86f, 0.5f), P(0.66f, 0.7f));
                    break;

                case Kind.Stats:
                    FillRect(p, P(0.16f, 0.52f), P(0.32f, 0.84f));
                    FillRect(p, P(0.42f, 0.16f), P(0.58f, 0.84f));
                    FillRect(p, P(0.68f, 0.36f), P(0.84f, 0.84f));
                    break;

                case Kind.Shirt:
                    p.BeginPath();
                    p.MoveTo(P(0.36f, 0.14f));
                    p.LineTo(P(0.5f, 0.24f));
                    p.LineTo(P(0.64f, 0.14f));
                    p.LineTo(P(0.92f, 0.3f));
                    p.LineTo(P(0.8f, 0.48f));
                    p.LineTo(P(0.72f, 0.42f));
                    p.LineTo(P(0.72f, 0.88f));
                    p.LineTo(P(0.28f, 0.88f));
                    p.LineTo(P(0.28f, 0.42f));
                    p.LineTo(P(0.2f, 0.48f));
                    p.LineTo(P(0.08f, 0.3f));
                    p.ClosePath();
                    p.Fill();
                    break;

                case Kind.Users: {
                    p.lineWidth = Mathf.Max(1.5f, s * 0.09f);
                    p.BeginPath();
                    p.Arc(P(0.38f, 0.34f), s * 0.14f, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(P(0.38f, 0.86f), s * 0.26f, Angle.Degrees(180), Angle.Degrees(360));
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(P(0.7f, 0.38f), s * 0.11f, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(P(0.74f, 0.84f), s * 0.18f, Angle.Degrees(200), Angle.Degrees(360));
                    p.Stroke();
                    break;
                }
            }
        }

        private static void Polyline(Painter2D p, params Vector2[] points) {
            p.BeginPath();
            p.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) {
                p.LineTo(points[i]);
            }
            p.Stroke();
        }

        private static void FillRect(Painter2D p, Vector2 min, Vector2 max) {
            p.BeginPath();
            p.MoveTo(min);
            p.LineTo(new Vector2(max.x, min.y));
            p.LineTo(max);
            p.LineTo(new Vector2(min.x, max.y));
            p.ClosePath();
            p.Fill();
        }

        private static void Dot(Painter2D p, Vector2 center, float radius) {
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }
    }
}
