using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class HelpPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "jabasurvivol-help",
                System.Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            GameBootstrap.SavePathOverride = null;

            if (_bootstrap != null)
            {
                Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator First_launch_shows_help_and_dismissing_marks_it_seen()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            Assert.That(GameSettings.HelpSeen, Is.False);
            Assert.That(session.HelpVisibleForTests, Is.True);
            Assert.That(session.HelpTitleForTests, Is.EqualTo(Localization.Get("help.title")));
            Assert.That(session.HelpBodyForTests, Is.EqualTo(Localization.Get("help.body")));
            Assert.That(session.HelpBodyForTests, Does.Contain("DEATH KEEPS SUPPLIES"));
            Assert.That(session.HelpCloseButtonForTests, Is.Not.Null);

            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.False);
            Assert.That(GameSettings.HelpSeen, Is.True);
            Assert.That(PlayerPrefs.GetInt(GameSettings.HelpSeenKey), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Dismissed_help_does_not_show_on_a_new_bootstrap()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            _bootstrap.Session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(GameSettings.HelpSeen, Is.True);
            Assert.That(_bootstrap.Session.HelpVisibleForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator Help_reopens_from_the_main_menu()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpButtonMainForTests, Is.Not.Null);
            session.HelpButtonMainForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.True);

            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.False);
            Assert.That(session.MainMenuStartLabelForTests, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator Help_reopens_from_settings_and_returns_to_settings()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            session.SettingsButtonMainForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.SettingsPanelVisibleForTests, Is.True);

            Assert.That(session.HelpButtonSettingsForTests, Is.Not.Null);
            session.HelpButtonSettingsForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.True);
            Assert.That(session.SettingsPanelVisibleForTests, Is.True);

            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.False);
            Assert.That(session.SettingsPanelVisibleForTests, Is.True);

            session.SettingsCloseButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.SettingsPanelVisibleForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator Help_blocks_gameplay_input_while_open()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.DismissHelpForTests();
            session.EnemySpawningEnabledForTests = false;
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Move = Vector2.right, Aim = Vector2.right }
            });
            session.StartRun();
            yield return null;

            var beforeHelp = session.PlayerPosition.x;
            session.ShowHelpForTests();
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.HelpVisibleForTests, Is.True);
            var blocked = session.PlayerPosition.x;
            yield return null;
            yield return null;

            Assert.That(session.PlayerPosition.x, Is.EqualTo(blocked).Within(0.001f));

            session.HelpCloseButtonForTests.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(session.HelpVisibleForTests, Is.False);
            Assert.That(session.PlayerPosition.x, Is.GreaterThan(blocked));
            Assert.That(session.PlayerPosition.x, Is.GreaterThan(beforeHelp));
        }

        [UnityTest]
        public IEnumerator Help_fits_phone_and_tablet_in_all_locales()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            Assert.That(session.HelpVisibleForTests, Is.True);

            foreach (var size in new[] { new Vector2(1280f, 720f), new Vector2(1600f, 1200f) })
            {
                foreach (var locale in new[] { "en", "es" })
                {
                    Localization.SetLocale(locale);
                    yield return null;
                    session.UseDesignLayoutForTests(size);
                    yield return null;

                    Assert.That(session.HelpVisibleForTests, Is.True);
                    AssertNoOverflow(session, $"help {locale} {size.x}x{size.y}");
                }

                Localization.UsePseudoLocaleForTests();
                yield return null;
                session.UseDesignLayoutForTests(size);
                yield return null;

                Assert.That(session.HelpVisibleForTests, Is.True);
                AssertNoOverflow(session, $"help {Localization.PseudoLocale} {size.x}x{size.y}");
            }
        }

        private static void AssertNoOverflow(GameSession session, string screen)
        {
            var overflows = session.TextOverflowsForTests;
            Assert.That(overflows, Is.Empty, $"{screen}\n{string.Join("\n", overflows)}");
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Help Test Bootstrap");
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
