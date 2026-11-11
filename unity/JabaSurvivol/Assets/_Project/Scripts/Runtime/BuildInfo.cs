using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// App version and build revision shown in the main menu. The Android build
    /// writes the commit to <c>Resources/build-info.txt</c>; editor and test
    /// runs fall back to <c>dev</c> when the asset is absent.
    /// </summary>
    public static class BuildInfo
    {
        private static string _commit;

        public static string Version => Application.version;

        public static string Commit
        {
            get
            {
                if (_commit == null)
                {
                    _commit = LoadCommit();
                }

                return _commit;
            }
        }

        private static string LoadCommit()
        {
            var asset = Resources.Load<TextAsset>("build-info");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return "dev";
            }

            foreach (var rawLine in asset.text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("commit="))
                {
                    continue;
                }

                var value = line.Substring("commit=".Length).Trim();
                return string.IsNullOrWhiteSpace(value) ? "dev" : value;
            }

            return "dev";
        }
    }
}
