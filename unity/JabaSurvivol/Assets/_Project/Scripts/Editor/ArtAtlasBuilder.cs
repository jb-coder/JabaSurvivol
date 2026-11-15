using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Builds the small category atlases used by combat and UI art.
    /// </summary>
    public static class ArtAtlasBuilder
    {
        private const string AtlasRoot = "Assets/_Project/Art/Atlas";

        [MenuItem("JabaSurvivol/Rebuild Art Atlases")]
        public static void BuildAtlases()
        {
            if (!AssetDatabase.IsValidFolder(AtlasRoot))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Atlas");
            }

            Build(
                "Characters",
                new[]
                {
                    ArtImportSettings.AssetRoot + "/Characters"
                });

            Build(
                "WeaponsFx",
                new[]
                {
                    ArtImportSettings.AssetRoot + "/Weapons",
                    ArtImportSettings.AssetRoot + "/Projectiles",
                    ArtImportSettings.AssetRoot + "/Effects"
                });

            Build(
                "UI",
                new[]
                {
                    ArtImportSettings.AssetRoot + "/UI"
                });

            Build(
                "Environment",
                new[]
                {
                    ArtImportSettings.AssetRoot + "/Environment"
                },
                compress: true);
        }

        private static void Build(
            string name,
            IReadOnlyList<string> folders,
            bool compress = false)
        {
            var path = $"{AtlasRoot}/{name}.spriteatlas";
            AssetDatabase.DeleteAsset(path);

            var atlas = new SpriteAtlas();
            var packables = new List<UnityEngine.Object>();
            foreach (var folder in folders)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
                if (asset != null)
                {
                    packables.Add(asset);
                }
            }

            if (packables.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(atlas);
                Debug.LogWarning($"No packables found for atlas {name}.");
                return;
            }

            SpriteAtlasExtensions.Add(atlas, packables.ToArray());
            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Bilinear,
                generateMipMaps = false,
                sRGB = true,
                readable = false,
                anisoLevel = 0
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = ArtImportSettings.MaxTextureSize,
                format = compress ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.RGBA32,
                compressionQuality = 100,
                textureCompression = compress
                    ? TextureImporterCompression.CompressedHQ
                    : TextureImporterCompression.Uncompressed
            });
            AssetDatabase.CreateAsset(atlas, path);
        }
    }
}
