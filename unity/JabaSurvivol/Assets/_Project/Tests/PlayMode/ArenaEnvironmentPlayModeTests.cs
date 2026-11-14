using System;
using System.Collections;
using System.IO;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class ArenaEnvironmentPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-arena", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, ProfileStore.FileName);
            GameBootstrap.SavePathOverride = _path;
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
                try
                {
                    Directory.Delete(_directory, true);
                }
                catch (IOException)
                {
                }
            }
        }

        [UnityTest]
        public IEnumerator Arena_uses_authored_floor_walls_and_props()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            var floor = _bootstrap.FloorRendererForTests;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.sprite, Is.Not.Null);
            Assert.That(floor.sprite.name, Is.EqualTo("floor_tile"));
            Assert.That(floor.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(floor.size.x, Is.GreaterThan(1f));
            Assert.That(floor.size.y, Is.GreaterThan(1f));
            Assert.That(_bootstrap.ArenaWallCountForTests, Is.EqualTo(4));
            Assert.That(_bootstrap.ArenaPropCountForTests, Is.GreaterThanOrEqualTo(4));

            var world = GameObject.Find("World");
            Assert.That(world, Is.Not.Null);
            var prop = world.transform.Find("Prop Barrel Left");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop.GetComponent<Collider2D>(), Is.Null);
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
