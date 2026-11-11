using System;
using System.Collections;
using System.IO;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class BuildInfoPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-buildinfo", Guid.NewGuid().ToString("N"));
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
        public IEnumerator Main_menu_shows_the_version_and_commit()
        {
            var gameObject = new GameObject("Test Bootstrap");
            _bootstrap = gameObject.AddComponent<GameBootstrap>();
            yield return null;
            yield return null;

            var label = _bootstrap.Session.BuildLabelForTests;
            Assert.That(label, Does.Contain(BuildInfo.Version));
            Assert.That(label, Does.Contain(BuildInfo.Commit));
        }
    }
}
