using System;
using System.Collections.Generic;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Cached access to the generated art set. Assets are loaded once from
    /// Resources and never allocated again during gameplay.
    /// </summary>
    public static class ArtCatalog
    {
        public const string ResourceRoot = "Art/";

        private static readonly Dictionary<string, Sprite> Sprites = new(StringComparer.Ordinal);
        private static readonly HashSet<string> MissingKeys = new(StringComparer.Ordinal);

        public static Sprite Get(string resourcePath, Sprite fallback = null)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return fallback != null ? fallback : SpriteFactory.Circle;
            }

            var key = Normalize(resourcePath);
            if (Sprites.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(key);
            if (sprite != null)
            {
                Sprites[key] = sprite;
                return sprite;
            }

            if (MissingKeys.Add(key))
            {
                Debug.LogWarning($"Art asset missing: {key}");
            }

            return fallback != null ? fallback : SpriteFactory.Circle;
        }

        public static bool TryGet(string resourcePath, out Sprite sprite)
        {
            var key = Normalize(resourcePath);
            if (Sprites.TryGetValue(key, out sprite))
            {
                return true;
            }

            sprite = Resources.Load<Sprite>(key);
            if (sprite != null)
            {
                Sprites[key] = sprite;
                return true;
            }

            return false;
        }

        public static void ClearForTests()
        {
            Sprites.Clear();
            MissingKeys.Clear();
        }

        public static int LoadedCount => Sprites.Count;

        private static string Normalize(string resourcePath)
        {
            return resourcePath.StartsWith(ResourceRoot, StringComparison.Ordinal)
                ? resourcePath
                : ResourceRoot + resourcePath;
        }
    }
}
