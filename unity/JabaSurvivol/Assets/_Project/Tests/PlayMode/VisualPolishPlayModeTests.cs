using System.Collections;
using System.IO;
using System.Linq;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class VisualPolishPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-polish", System.Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;

            if (_bootstrap != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Runtime_visual_polish_uses_the_color_safe_feature()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var polish = _bootstrap.VisualPolishForTests;
            Assert.That(polish, Is.Not.Null);
            Assert.That(polish.Enabled, Is.True);
            Assert.That(polish.BloomActive, Is.EqualTo(VisualPolish.PostProcessingActive));
            Assert.That(polish.VignetteActive, Is.EqualTo(VisualPolish.PostProcessingActive));
            Assert.That(polish.GlobalLight, Is.Not.Null);
            Assert.That(polish.GlobalLight.lightType, Is.EqualTo(Light2D.LightType.Global));

            var cameraData = _bootstrap.CameraForTests.GetUniversalAdditionalCameraData();
            Assert.That(cameraData.renderPostProcessing, Is.False);
        }

        [Test]
        public void Low_end_devices_disable_post_processing()
        {
            Assert.That(VisualPolish.ShouldEnablePostProcessing(4, 1024, true), Is.False);
            Assert.That(VisualPolish.ShouldEnablePostProcessing(2, 4096, true), Is.False);
            Assert.That(VisualPolish.ShouldEnablePostProcessing(8, 4096, false), Is.False);
            Assert.That(VisualPolish.ShouldEnablePostProcessing(8, 4096, true), Is.True);
        }

        [UnityTest]
        public IEnumerator Active_light_count_stays_within_the_budget()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var active = UnityEngine.Object
                .FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                .Count(light => light.isActiveAndEnabled);

            Assert.That(active, Is.LessThanOrEqualTo(VisualPolish.MaxActiveLights));
        }

        [UnityTest]
        public IEnumerator Visual_polish_can_be_disabled_as_one_unit()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var polish = _bootstrap.VisualPolishForTests;
            polish.Configure(false);
            Assert.That(polish.Enabled, Is.False);
            Assert.That(polish.BloomActive, Is.False);
            Assert.That(polish.VignetteActive, Is.False);
            Assert.That(VisualPolish.PostProcessingActive, Is.False);

            polish.Configure(true);
            Assert.That(polish.Enabled, Is.True);
            Assert.That(polish.BloomActive, Is.EqualTo(VisualPolish.PostProcessingActive));
            Assert.That(polish.VignetteActive, Is.EqualTo(VisualPolish.PostProcessingActive));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
