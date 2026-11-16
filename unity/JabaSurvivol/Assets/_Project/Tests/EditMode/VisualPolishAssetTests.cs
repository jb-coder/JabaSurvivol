using System.Linq;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class VisualPolishAssetTests
    {
        [Test]
        public void Color_safe_post_processing_asset_is_installed()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Shaders/NeonPost.mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("JabaSurvivol/NeonPost"));
            Assert.That(material.GetFloat("_BloomIntensity"), Is.GreaterThan(0f));
            Assert.That(material.GetFloat("_VignetteIntensity"), Is.GreaterThan(0f));

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(
                "Assets/Settings/Renderer2D.asset");
            Assert.That(renderer, Is.Not.Null);

            var feature = renderer.rendererFeatures
                .OfType<JabaNeonPostFeature>()
                .FirstOrDefault();
            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.passMaterial, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(feature.passMaterial),
                Is.EqualTo("Assets/_Project/Shaders/NeonPost.mat"));
            Assert.That(
                feature.injectionPoint,
                Is.EqualTo(FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing));
            Assert.That(feature.fetchColorBuffer, Is.True);
        }
    }
}
