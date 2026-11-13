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

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
