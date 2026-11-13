using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Applies the JabaSurvivol player, rendering, and scene configuration in
    /// batch mode or from the editor menu.
    /// </summary>
    public static class ProjectSetup
    {
        public const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string UniversalRenderPipelinePath = "Assets/Settings/UniversalRP.asset";

        [MenuItem("JabaSurvivol/Configure Project")]
        public static void Configure()
        {
            ConfigurePlayer();
            ConfigureRendering();
            ConfigureScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("JabaSurvivol project configuration complete.");
        }

        [MenuItem("JabaSurvivol/Recreate Main Scene")]
        public static void RecreateMainScene()
        {
            CreateMainScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("JabaSurvivol main scene recreated.");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "JabaSurvivol";
            PlayerSettings.productName = "JabaSurvivol";
            PlayerSettings.bundleVersion = "0.6.0";
            PlayerSettings.Android.bundleVersionCode = 7;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.jabasurvivol.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        private static void ConfigureRendering()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UniversalRenderPipelinePath);
            if (pipeline == null)
            {
                throw new InvalidOperationException(
                    $"Universal Render Pipeline asset not found at {UniversalRenderPipelinePath}.");
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static void ConfigureScene()
        {
            if (!File.Exists(MainScenePath))
            {
                CreateMainScene();
            }

            ConfigureBuildSettings();
        }

        private static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<JabaSurvivol.Gameplay.GameBootstrap>();

            var sceneDirectory = Path.GetDirectoryName(MainScenePath);
            if (!string.IsNullOrEmpty(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            if (!EditorSceneManager.SaveScene(scene, MainScenePath))
            {
                throw new InvalidOperationException($"Could not save main scene at {MainScenePath}.");
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            if (AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.DeleteAsset("Assets/Scenes");
            }
        }
    }
}
