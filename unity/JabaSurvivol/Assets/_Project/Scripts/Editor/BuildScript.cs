using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Batch-mode entry points for Android, Windows, and WebGL builds.
    /// </summary>
    public static class BuildScript
    {
        public static void BuildAndroid()
        {
            WriteBuildInfo();
            ProjectSetup.Configure();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            var output = GetOutputPath("-buildOutput", "artifacts/android/JabaSurvivol.apk");
            Build(BuildTarget.Android, output);
        }

        /// <summary>
        /// Builds an additional APK that also contains x86_64 so it can run on
        /// a local Android emulator. Production Android builds stay ARM64-only.
        /// </summary>
        public static void BuildAndroidEmulator()
        {
            WriteBuildInfo();
            ProjectSetup.Configure();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            var previousVersion = PlayerSettings.bundleVersion;
            var previousCode = PlayerSettings.Android.bundleVersionCode;
            var versionOverride = GetArgument("-bundleVersion");
            if (!string.IsNullOrWhiteSpace(versionOverride))
            {
                PlayerSettings.bundleVersion = versionOverride;
            }

            var codeOverride = GetArgument("-bundleVersionCode");
            if (int.TryParse(codeOverride, out var versionCode))
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            try
            {
                var output = GetOutputPath("-buildOutput", "artifacts/android/JabaSurvivol-emulator.apk");
                Build(BuildTarget.Android, output, BuildOptions.Development);
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
                EditorUserBuildSettings.development = false;
                PlayerSettings.bundleVersion = previousVersion;
                PlayerSettings.Android.bundleVersionCode = previousCode;
            }
        }

        public static void BuildWindows()
        {
            ProjectSetup.Configure();
            var output = GetOutputPath("-buildOutput", "artifacts/windows/JabaSurvivol.exe");
            Build(BuildTarget.StandaloneWindows64, output);
        }

        public static void BuildWebGL()
        {
            ProjectSetup.Configure();
            var output = GetOutputPath("-buildOutput", "artifacts/webgl");
            Build(BuildTarget.WebGL, output);
        }

        private static void WriteBuildInfo()
        {
            var commit = RunGit("rev-parse --short HEAD");
            if (string.IsNullOrWhiteSpace(commit))
            {
                commit = "dev";
            }
            else if (!string.IsNullOrWhiteSpace(RunGit("status --porcelain")))
            {
                commit += "-dirty";
            }

            var resources = Path.Combine(Application.dataPath, "Resources");
            Directory.CreateDirectory(resources);
            File.WriteAllText(Path.Combine(resources, "build-info.txt"), "commit=" + commit);

            var apiBaseUrl = Environment.GetEnvironmentVariable("JABA_API_BASE_URL");
            var apiPath = Path.Combine(resources, "api-base-url.txt");
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                if (File.Exists(apiPath))
                {
                    File.Delete(apiPath);
                }
            }
            else
            {
                File.WriteAllText(apiPath, apiBaseUrl.Trim());
            }
        }

        private static string RunGit(string arguments)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo("git", arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    return string.Empty;
                }

                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static void Build(BuildTarget target, string outputPath, BuildOptions buildOptions = BuildOptions.None)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScenePath },
                locationPathName = outputPath,
                target = target,
                options = buildOptions
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }

            Debug.Log($"Build succeeded: {outputPath} ({report.summary.totalSize} bytes).");
        }

        private static string GetOutputPath(string argumentName, string defaultRelativePath)
        {
            var argument = GetArgument(argumentName);
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
            var path = string.IsNullOrWhiteSpace(argument) ? defaultRelativePath : argument;
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private static string GetArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
