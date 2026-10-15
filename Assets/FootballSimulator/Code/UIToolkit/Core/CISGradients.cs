using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// USS has no gradients, so the few the design needs are generated once at
    /// runtime and assigned to elements carrying a marker class:
    ///   cis-grad-accent     neon green -> cyan sweep
    ///   cis-grad-menu       full-screen menu backdrop with glows
    ///   cis-grad-fade-up    transparent top -> dark bottom
    ///   cis-grad-fade-down  dark top -> transparent bottom
    ///   cis-grad-fade-left  dark left -> transparent right
    ///   cis-grad-fade-right transparent left -> dark right
    ///   cis-grad-glow       soft white radial glow (tint it with -unity-background-image-tint-color)
    /// </summary>
    public static class CISGradients {
        public static readonly Color Accent = new Color32(0x00, 0xE5, 0x8F, 0xFF);
        public static readonly Color Accent2 = new Color32(0x00, 0xB4, 0xFF, 0xFF);
        public static readonly Color Background = new Color32(0x05, 0x08, 0x0F, 0xFF);
        public static readonly Color Background2 = new Color32(0x0B, 0x13, 0x24, 0xFF);

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static void Apply(VisualElement root) {
            Assign(root, "cis-grad-accent", AccentSweep);
            Assign(root, "cis-grad-menu", MenuBackdrop);
            Assign(root, "cis-grad-fade-up", FadeUp);
            Assign(root, "cis-grad-fade-down", FadeDown);
            Assign(root, "cis-grad-fade-left", FadeLeft);
            Assign(root, "cis-grad-fade-right", FadeRight);
            Assign(root, "cis-grad-glow", Glow);
        }

        private static void Assign(VisualElement root, string className, Texture2D texture) {
            root.Query(className: className).ForEach(element => SetBackground(element, texture));
        }

        public static void SetBackground(VisualElement element, Texture2D texture) {
            element.style.backgroundImage = new StyleBackground(texture);
#pragma warning disable CS0618 // unityBackgroundScaleMode is the 2022.3 way of stretching backgrounds.
            element.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
#pragma warning restore CS0618
        }

        public static Texture2D AccentSweep => Get("accent", 256, 1, (u, v) => Color.Lerp(Accent, Accent2, u));

        public static Texture2D FadeUp => Get("fadeUp", 1, 128, (u, v) => WithAlpha(Background, Mathf.SmoothStep(0.95f, 0f, v)));

        public static Texture2D FadeDown => Get("fadeDown", 1, 128, (u, v) => WithAlpha(Background, Mathf.SmoothStep(0f, 0.95f, v)));

        public static Texture2D FadeLeft => Get("fadeLeft", 128, 1, (u, v) => WithAlpha(Background, Mathf.SmoothStep(0.95f, 0f, u)));

        public static Texture2D FadeRight => Get("fadeRight", 128, 1, (u, v) => WithAlpha(Background, Mathf.SmoothStep(0f, 0.95f, u)));

        public static Texture2D Glow => Get("glow", 128, 128, (u, v) => {
            var d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            var a = Mathf.Clamp01(1f - d);
            return new Color(1, 1, 1, a * a);
        });

        public static Texture2D MenuBackdrop => Get("menu", 480, 270, (u, v) => {
            // v = 0 is the bottom row of the texture.
            var color = Color.Lerp(Background, Background2, v * 0.85f);

            color = AddGlow(color, Accent, u, v, new Vector2(0.12f, 0.05f), 0.62f, 0.30f);
            color = AddGlow(color, Accent2, u, v, new Vector2(0.92f, 0.95f), 0.55f, 0.22f);
            color = AddGlow(color, Accent, u, v, new Vector2(0.55f, 0.55f), 0.45f, 0.05f);

            // subtle vignette.
            var vignette = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
            color *= Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(vignette * 1.2f));
            color.a = 1f;
            return color;
        });

        private static Color AddGlow(Color baseColor, Color glow, float u, float v, Vector2 center, float radius, float strength) {
            var aspect = 16f / 9f;
            var d = new Vector2((u - center.x) * aspect, v - center.y).magnitude / radius;
            var t = Mathf.Clamp01(1f - d);
            return baseColor + glow * (t * t * strength);
        }

        private static Color WithAlpha(Color color, float alpha) {
            color.a = alpha;
            return color;
        }

        private delegate Color Painter(float u, float v);

        private static Texture2D Get(string key, int width, int height, Painter painter) {
            if (cache.TryGetValue(key, out var existing) && existing != null) {
                return existing;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = "CIS Gradient " + key,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++) {
                var v = height == 1 ? 0f : y / (height - 1f);
                for (int x = 0; x < width; x++) {
                    var u = width == 1 ? 0f : x / (width - 1f);
                    pixels[y * width + x] = painter(u, v);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            cache[key] = texture;
            return texture;
        }
    }
}
