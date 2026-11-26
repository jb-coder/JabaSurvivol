using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Adds <c>android:localeConfig</c> to the generated launcher manifest.
    /// The Android manifest merger only accepts this attribute from the main
    /// app module, so the custom manifest that Unity merges into
    /// <c>unityLibrary</c> cannot carry it into the final APK.
    /// </summary>
    public sealed class AndroidLocaleConfigPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string LocaleConfigAttribute = "localeConfig";
        private const string LocaleConfigValue = "@xml/locales_config";
        private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradleRoot = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(gradleRoot))
            {
                return;
            }

            var launcherManifest = Path.Combine(
                gradleRoot,
                "launcher",
                "src",
                "main",
                "AndroidManifest.xml");
            if (!File.Exists(launcherManifest))
            {
                Debug.LogWarning("Launcher manifest not found: " + launcherManifest);
                return;
            }

            var document = XDocument.Load(launcherManifest, LoadOptions.PreserveWhitespace);
            var application = document.Root?.Element("application");
            if (application == null)
            {
                Debug.LogWarning("Launcher manifest has no application element: " + launcherManifest);
                return;
            }

            var attribute = application.Attribute(Android + LocaleConfigAttribute);
            if (attribute != null)
            {
                if (attribute.Value == LocaleConfigValue)
                {
                    return;
                }

                attribute.Value = LocaleConfigValue;
            }
            else
            {
                application.SetAttributeValue(Android + LocaleConfigAttribute, LocaleConfigValue);
            }

            document.Save(launcherManifest);
        }
    }
}
