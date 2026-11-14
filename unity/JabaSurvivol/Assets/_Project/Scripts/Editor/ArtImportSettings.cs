using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Applies the manifest contract to generated sprite imports.
    /// </summary>
    public static class ArtImportSettings
    {
        public const string AssetRoot = "Assets/Resources/Art";
        public const string ManifestPath = "Assets/_Project/Art/art-manifest.json";
        public const float PixelsPerUnit = 256f;
        public const int MaxTextureSize = 512;

        public static ManifestDto ReadManifest()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)
                ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
            var manifestPath = Path.Combine(projectRoot, ManifestPath);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Art manifest not found.", manifestPath);
            }

            var manifest = JsonUtility.FromJson<ManifestDto>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.assets == null)
            {
                throw new InvalidDataException("Art manifest could not be parsed.");
            }

            return manifest;
        }

        public static ManifestEntry FindEntry(ManifestDto manifest, string assetPath)
        {
            if (!assetPath.StartsWith(AssetRoot, StringComparison.Ordinal))
            {
                return null;
            }

            var relative = assetPath.Substring(AssetRoot.Length + 1).Replace('\\', '/');
            foreach (var entry in manifest.assets)
            {
                if (string.Equals(entry.path, relative, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public static void Apply(TextureImporter importer, string assetPath)
        {
            var manifest = ReadManifest();
            var entry = FindEntry(manifest, assetPath);
            if (entry == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = string.Equals(entry.category, "environment", StringComparison.Ordinal)
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(entry.pivotX, entry.pivotY);
            settings.filterMode = FilterMode.Bilinear;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = MaxTextureSize;

            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = MaxTextureSize;
            android.format = string.Equals(entry.category, "environment", StringComparison.Ordinal)
                ? TextureImporterFormat.ASTC_6x6
                : TextureImporterFormat.RGBA32;
            android.compressionQuality = 100;
            importer.SetPlatformTextureSettings(android);
        }

        [Serializable]
        public sealed class ManifestDto
        {
            public int schemaVersion;
            public string paletteVersion = string.Empty;
            public ManifestEntry[] assets = Array.Empty<ManifestEntry>();
        }

        [Serializable]
        public sealed class ManifestEntry
        {
            public string path = string.Empty;
            public string resourcePath = string.Empty;
            public int width;
            public int height;
            public float pivotX;
            public float pivotY;
            public string category = string.Empty;
            public string paletteVersion = string.Empty;
            public string sha256 = string.Empty;
        }
    }
}
