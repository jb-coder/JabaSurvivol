using System.Collections;
using System.Globalization;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class LocalizationUiPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-localization-ui", System.Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, ProfileStore.FileName);
            GameBootstrap.SavePathOverride = _path;
            Localization.SetLocale("en");
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
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
        public IEnumerator Language_selector_updates_main_menu_labels()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            var profile = session.Profile;

            Assert.That(session.MainMenuStartLabelForTests, Is.EqualTo("START RUN"));

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.LanguagePanelVisibleForTests, Is.True);

            session.LanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.LanguagePanelVisibleForTests, Is.False);
            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
            Assert.That(session.MainMenuStartLabelForTests, Is.Not.EqualTo("START RUN"));
            Assert.That(session.MainMenuSubtitleForTests, Is.Not.EqualTo("SURVIVE THE NIGHT. KEEP EVERYTHING."));
            Assert.That(session.Profile, Is.SameAs(profile));
        }

        [UnityTest]
        public IEnumerator Pseudo_locale_main_menu_has_no_text_overflow()
        {
            Localization.UsePseudoLocaleForTests();
            _bootstrap = CreateBootstrap();
            yield return null;
            _bootstrap.Session.UseDesignLayoutForTests();
            yield return null;

            var overflows = _bootstrap.Session.TextOverflowsForTests;
            Assert.That(overflows, Is.Empty, string.Join("\n", overflows));
        }

        [UnityTest]
        public IEnumerator Pseudo_locale_primary_screens_have_no_text_overflow()
        {
            Localization.UsePseudoLocaleForTests();
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.UseDesignLayoutForTests();
            yield return null;

            AssertNoOverflow(session, "main menu");

            session.StartRun();
            yield return null;
            AssertNoOverflow(session, "HUD");

            session.PauseGame();
            yield return null;
            AssertNoOverflow(session, "pause");

            session.ResumeGame();
            yield return null;
            session.ApplyDamage(100000f);
            yield return null;
            AssertNoOverflow(session, "game over");

            session.StartRun();
            yield return null;
            var budget = session.SpawnBudget;
            for (var i = 0; i < budget; i++)
            {
                session.DamageZombieForTests(100000f);
                yield return null;

                if (i < budget - 1)
                {
                    session.SpawnZombieForTests();
                    yield return null;
                }
            }

            Assert.That(session.RoundEndVisible, Is.True);
            AssertNoOverflow(session, "round end");
        }

        [UnityTest]
        public IEnumerator Language_selector_updates_pause_and_hud_without_resetting_the_run()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            var round = session.Run.Round;
            var supplies = session.Profile.Supplies;
            var roundLabel = session.HudRoundLabelForTests;
            session.PauseGame();
            yield return null;

            session.PauseLanguageButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.LanguagePanelVisibleForTests, Is.True);

            session.LanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.PauseTitleForTests, Is.Not.EqualTo("PAUSED"));
            Assert.That(session.HudReloadLabelForTests, Is.Not.EqualTo("RELOAD"));
            Assert.That(session.HudRoundLabelForTests, Is.Not.EqualTo(roundLabel));
            Assert.That(session.Run.Round, Is.EqualTo(round));
            Assert.That(session.Profile.Supplies, Is.EqualTo(supplies));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));

            session.ResumeGame();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Language_choice_applies_to_result_screens()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            session.LanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;

            session.StartRun();
            yield return null;
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.GameOverTitleForTests, Is.Not.EqualTo("YOU DIED"));
        }

        [UnityTest]
        public IEnumerator Language_choice_survives_bootstrap_recreation()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            session.LanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));

            Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
            Assert.That(_bootstrap.Session.MainMenuStartLabelForTests, Is.Not.EqualTo("START RUN"));
        }

        [UnityTest]
        public IEnumerator Language_system_option_clears_the_override()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            session.LanguageSpanishButtonForTests.onClick.Invoke();
            yield return null;

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            session.LanguageSystemButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(
                PlayerPrefs.GetString(Localization.LocalePreferenceKey),
                Is.EqualTo(Localization.SystemLocaleValue));
            Assert.That(
                Localization.CurrentLocale,
                Is.EqualTo(Localization.MapSystemLanguage(
                    Application.systemLanguage,
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)));
        }

        [UnityTest]
        public IEnumerator Language_selector_close_does_not_change_the_locale()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.LanguageButtonForTests.onClick.Invoke();
            yield return null;
            session.LanguageCloseButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.LanguagePanelVisibleForTests, Is.False);
            Assert.That(Localization.CurrentLocale, Is.EqualTo("en"));
        }

        [UnityTest]
        public IEnumerator Round_reward_cards_are_localized()
        {
            Localization.SetLocale("es");
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            var budget = session.SpawnBudget;

            for (var i = 0; i < budget; i++)
            {
                session.DamageZombieForTests(100000f);
                yield return null;

                if (i < budget - 1)
                {
                    session.SpawnZombieForTests();
                    yield return null;
                }
            }

            Assert.That(session.RoundEndVisible, Is.True);
            var labels = session.RoundEndRewardLabelsForTests;
            Assert.That(labels.Length, Is.EqualTo(session.PendingRewardCount));

            for (var i = 0; i < labels.Length; i++)
            {
                var reward = session.PendingRewardAt(i);
                var key = reward.Type switch
                {
                    RoundRewardType.Supplies => "reward.supplies.card",
                    RoundRewardType.Xp => "reward.xp.card",
                    RoundRewardType.AmmoCrate => "reward.ammo.card",
                    _ => "reward.armor.card"
                };
                Assert.That(labels[i], Is.EqualTo(Localization.Format(key, reward.Amount)));
            }
        }

        private static void AssertNoOverflow(GameSession session, string screen)
        {
            var overflows = session.TextOverflowsForTests;
            Assert.That(overflows, Is.Empty, $"{screen}\n{string.Join("\n", overflows)}");
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
