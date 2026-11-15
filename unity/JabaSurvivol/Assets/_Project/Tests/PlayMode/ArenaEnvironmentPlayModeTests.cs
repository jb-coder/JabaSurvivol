using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            Assert.That(floor.sprite.name, Is.EqualTo("floor_asphalt_a"));
            Assert.That(floor.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(floor.size.x, Is.GreaterThan(1f));
            Assert.That(floor.size.y, Is.GreaterThan(1f));
            Assert.That(_bootstrap.ArenaWallCountForTests, Is.GreaterThanOrEqualTo(4));
            Assert.That(_bootstrap.ArenaPropCountForTests, Is.GreaterThanOrEqualTo(12));
            Assert.That(_bootstrap.ArenaDecalCountForTests, Is.GreaterThanOrEqualTo(10));
            Assert.That(_bootstrap.ArenaLightPoolCountForTests, Is.GreaterThanOrEqualTo(4));

            var world = GameObject.Find("World");
            Assert.That(world, Is.Not.Null);
            var prop = world.transform.Find("Prop Car");
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop.GetComponent<Collider2D>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator Arena_layers_and_positions_are_deterministic()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var first = CaptureDecorations();

            UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;
            var second = CaptureDecorations();

            Assert.That(second.Keys, Is.EquivalentTo(first.Keys));
            foreach (var pair in first)
            {
                Assert.That(second[pair.Key].Position, Is.EqualTo(pair.Value.Position), pair.Key);
                Assert.That(second[pair.Key].SortingOrder, Is.EqualTo(pair.Value.SortingOrder), pair.Key);
            }
        }

        [UnityTest]
        public IEnumerator Arena_layers_use_the_documented_sorting_orders()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var world = GameObject.Find("World");
            Assert.That(world, Is.Not.Null);

            foreach (var renderer in world.GetComponentsInChildren<SpriteRenderer>())
            {
                var name = renderer.gameObject.name;
                if (name == "Floor")
                {
                    Assert.That(renderer.sortingOrder, Is.EqualTo(SortingOrders.Floor), name);
                }
                else if (name == "Floor Overlay")
                {
                    Assert.That(renderer.sortingOrder, Is.EqualTo(SortingOrders.FloorGrid), name);
                }
                else if (name.StartsWith("Decal", StringComparison.Ordinal))
                {
                    Assert.That(renderer.sortingOrder, Is.EqualTo(SortingOrders.FloorDecal), name);
                }
                else if (name.StartsWith("Light ", StringComparison.Ordinal))
                {
                    Assert.That(renderer.sortingOrder, Is.EqualTo(SortingOrders.LightPool), name);
                }
                else if (name.StartsWith("Perimeter", StringComparison.Ordinal))
                {
                    Assert.That(renderer.sortingOrder, Is.EqualTo(SortingOrders.Perimeter), name);
                }
            }

            foreach (var prop in world.GetComponentsInChildren<Transform>()
                         .Where(item => item.name.StartsWith("Prop", StringComparison.Ordinal)))
            {
                Assert.That(prop.GetComponent<Collider2D>(), Is.Null, prop.name);
            }
        }

        private Dictionary<string, DecorationSnapshot> CaptureDecorations()
        {
            var world = GameObject.Find("World");
            Assert.That(world, Is.Not.Null);
            var result = new Dictionary<string, DecorationSnapshot>(StringComparer.Ordinal);
            foreach (var transform in world.GetComponentsInChildren<Transform>())
            {
                var name = transform.name;
                if (!name.StartsWith("Decal", StringComparison.Ordinal) &&
                    !name.StartsWith("Light ", StringComparison.Ordinal) &&
                    !name.StartsWith("Prop", StringComparison.Ordinal) &&
                    !name.StartsWith("Perimeter", StringComparison.Ordinal))
                {
                    continue;
                }

                var renderer = transform.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                result[name] = new DecorationSnapshot(
                    transform.localPosition,
                    renderer.sortingOrder);
            }

            return result;
        }

        private readonly struct DecorationSnapshot
        {
            public DecorationSnapshot(Vector3 position, int sortingOrder)
            {
                Position = position;
                SortingOrder = sortingOrder;
            }

            public Vector3 Position { get; }

            public int SortingOrder { get; }
        }
        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}

