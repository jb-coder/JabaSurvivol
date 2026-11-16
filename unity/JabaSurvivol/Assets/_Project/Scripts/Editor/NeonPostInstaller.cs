using System.Linq;
using JabaSurvivol.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Installs the color-safe full-screen post-processing material and renderer
    /// feature. The renderer feature replaces URP camera post-processing for the
    /// 2D renderer because the integrated color-grading path is not reliable on
    /// Android GLES.
    /// </summary>
    public static class NeonPostInstaller
    {
        public const string ShaderPath = "Assets/_Project/Shaders/NeonPost.shader";
        public const string MaterialPath = "Assets/_Project/Shaders/NeonPost.mat";
        private const string RendererPath = "Assets/Settings/Renderer2D.asset";

        public static void EnsureInstalled()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    $"Neon post shader not found at {ShaderPath}.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                throw new System.InvalidOperationException(
                    $"Renderer2D asset not found at {RendererPath}.");
            }

            var feature = renderer.rendererFeatures
                .OfType<JabaNeonPostFeature>()
                .FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<JabaNeonPostFeature>();
                feature.name = "Jaba Neon Post";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
            }

            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.bindDepthStencilAttachment = false;
            feature.requirements = ScriptableRenderPassInput.None;
            feature.passIndex = 0;

            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
