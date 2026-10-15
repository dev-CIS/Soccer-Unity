using System.Collections.Generic;
using UnityEngine;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Team crests and kit previews are stored as red/blue mask textures that the
    /// old uGUI colourised with a material. UI Toolkit can't use custom materials,
    /// so the mask is baked once per (texture, colours) into a RenderTexture.
    /// </summary>
    public static class CISTextureBaker {
        private const string ShaderPath = CISUI.ResourceRoot + "Shaders/CISMaskColorize";

        private static readonly int FirstColorId = Shader.PropertyToID("_FirstColor");
        private static readonly int SecondColorId = Shader.PropertyToID("_SecondColor");

        private static readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();

        private static Material material;
        private static bool materialMissing;

        public static RenderTexture Colorize(Texture mask, Color first, Color second, int size) {
            if (mask == null || !EnsureMaterial()) {
                return null;
            }

            var key = string.Concat(
                mask.GetInstanceID().ToString(), "_",
                ColorUtility.ToHtmlStringRGBA(first), "_",
                ColorUtility.ToHtmlStringRGBA(second), "_",
                size.ToString());

            if (cache.TryGetValue(key, out var cached) && cached != null && cached.IsCreated()) {
                return cached;
            }

            var aspect = mask.height > 0 ? mask.width / (float)mask.height : 1f;
            var width = size;
            var height = Mathf.Max(1, Mathf.RoundToInt(size / aspect));

            var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) {
                name = "CIS Baked " + mask.name,
                useMipMap = true,
                autoGenerateMips = true,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            target.Create();

            material.SetColor(FirstColorId, first);
            material.SetColor(SecondColorId, second);

            var previous = RenderTexture.active;
            UnityEngine.Graphics.Blit(mask, target, material);
            RenderTexture.active = previous;

            cache[key] = target;
            return target;
        }

        private static bool EnsureMaterial() {
            if (material != null) {
                return true;
            }

            if (materialMissing) {
                return false;
            }

            var shader = Resources.Load<Shader>(ShaderPath);
            if (shader == null || !shader.isSupported) {
                Debug.LogWarning("[CISTextureBaker] Mask colorize shader unavailable, falling back to vector crests.");
                materialMissing = true;
                return false;
            }

            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }
    }
}
