using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Batch and menu entry points for regenerating imports and atlases.
    /// </summary>
    public static class ArtImportMenu
    {
        [MenuItem("JabaSurvivol/Reimport Art")]
        public static void ReimportAll()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)
                ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
            var root = Path.Combine(projectRoot, ArtImportSettings.AssetRoot);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException($"Art root not found: {root}");
            }

            var count = 0;
            foreach (var file in Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                AssetDatabase.ImportAsset(relative, ImportAssetOptions.ForceUpdate);
                count++;
            }

            ArtAtlasBuilder.BuildAtlases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Reimported {count} art sprites and rebuilt atlases.");
        }
    }
}
