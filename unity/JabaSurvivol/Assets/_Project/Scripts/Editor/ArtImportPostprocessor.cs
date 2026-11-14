using System;
using UnityEditor;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Keeps generated sprite imports aligned with the art manifest.
    /// </summary>
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtImportSettings.AssetRoot, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (assetImporter is TextureImporter importer)
            {
                ArtImportSettings.Apply(importer, assetPath);
            }
        }
    }
}
