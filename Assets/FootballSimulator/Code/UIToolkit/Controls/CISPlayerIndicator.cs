using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Marker over the user-controlled footballer. When the player is off
    /// screen it sits on the screen edge and points towards them.
    /// Replaces the old uGUI "UI/InputPointer" prefab. Indicators hide
    /// themselves as soon as they stop being updated (e.g. match unloaded).
    /// </summary>
    public class CISPlayerIndicator {
        private static readonly Color[] PlayerColors = {
            CISGradients.Accent,
            CISGradients.Accent2,
            new Color32(0xFF, 0xC9, 0x3C, 0xFF),
            new Color32(0xFF, 0x5A, 0x5F, 0xFF)
        };

        private class Layer : MonoBehaviour {
            public VisualElement Root;
            public readonly List<CISPlayerIndicator> Indicators = new List<CISPlayerIndicator>();

            private void LateUpdate() {
                var now = Time.unscaledTime;
                foreach (var indicator in Indicators) {
                    if (indicator.visible && now - indicator.lastPlaced > 0.2f) {
                        indicator.SetVisible(false);
                    }
                }
            }
        }

        private static Layer layer;
        private static readonly Dictionary<int, CISPlayerIndicator> byPlayer = new Dictionary<int, CISPlayerIndicator>();

        private readonly Arrow arrow;
        private float lastPlaced;
        private bool visible;

        /// <summary>
        /// One indicator per local player slot; reused across matches.
        /// </summary>
        public static CISPlayerIndicator Create(int playerIndex) {
            if (byPlayer.TryGetValue(playerIndex, out var existing) && existing.arrow.panel != null) {
                return existing;
            }

            var indicator = new CISPlayerIndicator(playerIndex);
            EnsureLayer().Indicators.Add(indicator);
            byPlayer[playerIndex] = indicator;
            return indicator;
        }

        private CISPlayerIndicator(int playerIndex) {
            arrow = new Arrow(PlayerColors[Mathf.Abs(playerIndex) % PlayerColors.Length]);
            EnsureLayer().Root.Add(arrow);
            SetVisible(false);
        }

        /// <summary>
        /// Same data the old pointer used: a screen position (pixels, origin
        /// bottom-left) and a rotation around Z where 0° points right.
        /// </summary>
        public void Place(Vector3 screenPosition, Quaternion rotation) {
            var panel = arrow.panel;
            if (panel == null) {
                return;
            }

            var panelPosition = RuntimePanelUtils.ScreenToPanel(panel,
                new Vector2(screenPosition.x, Screen.height - screenPosition.y));

            arrow.style.left = panelPosition.x;
            arrow.style.top = panelPosition.y;
            // UI Toolkit's y axis points down, so the angle flips.
            arrow.style.rotate = new Rotate(Angle.Degrees(-rotation.eulerAngles.z));

            lastPlaced = Time.unscaledTime;
            SetVisible(true);
        }

        private void SetVisible(bool value) {
            visible = value;
            arrow.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Layer EnsureLayer() {
            if (layer != null) {
                return layer;
            }

            var host = new GameObject("CIS Player Indicators");
            Object.DontDestroyOnLoad(host);

            layer = host.AddComponent<Layer>();

            var document = CISUI.CreateDocument(host.transform, "PlayerIndicators", CISUI.SortMatch - 5);
            layer.Root = document.rootVisualElement;

            return layer;
        }

        /// <summary>
        /// Chevron pointing right (0°) drawn around the element's centre.
        /// </summary>
        private class Arrow : VisualElement {
            private readonly Color color;

            public Arrow(Color color) {
                this.color = color;
                AddToClassList("cis-pointer");
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            private void Draw(MeshGenerationContext context) {
                var r = contentRect;
                if (r.width <= 0 || r.height <= 0) {
                    return;
                }

                var p = context.painter2D;
                Vector2 P(float x, float y) => new Vector2(r.x + x * r.width, r.y + y * r.height);

                p.BeginPath();
                p.MoveTo(P(0.92f, 0.5f));
                p.LineTo(P(0.12f, 0.1f));
                p.LineTo(P(0.34f, 0.5f));
                p.LineTo(P(0.12f, 0.9f));
                p.ClosePath();

                p.fillColor = color;
                p.Fill();

                p.strokeColor = new Color(0.02f, 0.03f, 0.06f, 0.85f);
                p.lineWidth = 2f;
                p.lineJoin = LineJoin.Round;
                p.Stroke();
            }
        }
    }
}
