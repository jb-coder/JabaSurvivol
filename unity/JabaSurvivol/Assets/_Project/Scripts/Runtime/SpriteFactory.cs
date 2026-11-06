using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Creates small procedural sprites once so the skeleton has no external
    /// art dependency. Sprite art is replaced in the visual pass.
    /// </summary>
    public static class SpriteFactory
    {
        private const int TextureSize = 64;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _square;

        public static Sprite Circle => _circle ??= CreateCircle(false);

        public static Sprite Ring => _ring ??= CreateCircle(true);

        public static Sprite Square => _square ??= CreateSquare();

        private static Sprite CreateSquare()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "JabaSurvivol Square",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite CreateCircle(bool ring)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = ring ? "JabaSurvivol Ring" : "JabaSurvivol Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[TextureSize * TextureSize];
            var center = (TextureSize - 1) * 0.5f;
            var outer = center - 1f;
            var inner = outer * 0.72f;

            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = ring ? Edge(distance, inner, outer) : Edge(distance, 0f, outer);
                    pixels[(y * TextureSize) + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }

        private static float Edge(float distance, float inner, float outer)
        {
            if (inner > 0f && distance < inner - 0.5f)
            {
                return 0f;
            }

            return Mathf.Clamp01(outer + 0.5f - distance);
        }
    }
}
