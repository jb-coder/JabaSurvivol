using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class ArenaCameraPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-camera", System.Guid.NewGuid().ToString("N"));
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
        public IEnumerator Camera_follows_the_player_and_clamps_to_the_arena()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var camera = _bootstrap.CameraForTests;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographicSize, Is.EqualTo(GameBalance.ArenaCameraHalfHeight).Within(0.01f));

            session.SetPlayerPositionForTests(new Vector2(5f, 3f));
            yield return new WaitForSeconds(0.6f);

            Assert.That(camera.transform.position.x, Is.EqualTo(5f).Within(0.25f));
            Assert.That(camera.transform.position.y, Is.EqualTo(3f).Within(0.25f));

            session.SetPlayerPositionForTests(new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.6f);

            var halfExtents = _bootstrap.CameraFitterForTests.ViewHalfExtents;
            Assert.That(
                camera.transform.position.x,
                Is.EqualTo(GameBalance.ArenaHalfWidth - halfExtents.x).Within(0.25f));
            Assert.That(
                camera.transform.position.y,
                Is.EqualTo(GameBalance.ArenaHalfHeight - halfExtents.y).Within(0.25f));
            Assert.That(camera.transform.position.z, Is.EqualTo(-10f).Within(0.01f));
        }

        [Test]
        public void Camera_clamp_formula_handles_phone_and_tablet_aspects()
        {
            var phone = AspectCameraFitter.ClampToArena(new Vector2(100f, 100f), 9.96f, 5.6f);
            var tablet = AspectCameraFitter.ClampToArena(new Vector2(100f, 100f), 8.96f, 5.6f);

            Assert.That(phone.x, Is.EqualTo(8.04f).Within(0.01f));
            Assert.That(phone.y, Is.EqualTo(4.4f).Within(0.01f));
            Assert.That(tablet.x, Is.EqualTo(9.04f).Within(0.01f));
            Assert.That(tablet.y, Is.EqualTo(4.4f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Player_can_move_past_the_old_arena_bounds()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Move = Vector2.right }
            });
            session.StartRun();
            yield return new WaitForSeconds(2.1f);

            Assert.That(session.PlayerPosition.x, Is.GreaterThan(8.8f));
        }

        [UnityTest]
        public IEnumerator Zombie_spawns_outside_the_camera_and_inside_the_arena()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;

            var player = session.PlayerPosition;
            var zombie = session.ZombiePosition;
            var distance = Vector2.Distance(player, zombie);

            Assert.That(distance, Is.GreaterThanOrEqualTo(GameBalance.SpawnRingClearance));
            Assert.That(Mathf.Abs(zombie.x), Is.LessThanOrEqualTo(GameBalance.ArenaHalfWidth));
            Assert.That(Mathf.Abs(zombie.y), Is.LessThanOrEqualTo(GameBalance.ArenaHalfHeight));
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }

        private sealed class ScriptedInput : IGameInput
        {
            public InputFrame Frame;

            public InputFrame Read(Vector2 playerWorldPosition)
            {
                return Frame;
            }
        }
    }
}
