using System;
using System.Collections;
using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class ConfirmationPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "jabasurvivol-confirmations",
                Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
            GameSettings.MarkHelpSeen();
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
            GameSettings.ClearPreferencesForTests();
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
        public IEnumerator Respec_confirmation_cancel_keeps_allocated_ranks()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddXp(int.MaxValue);
            session.Profile.AllocateSkill(SkillNode.Strength);
            session.Profile.AllocateSkill(SkillNode.Vitality);
            var pointsBefore = session.Profile.SkillPoints;

            session.ShowCharacterSheet();
            yield return null;

            session.CharacterRespecButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.True);
            Assert.That(
                session.ConfirmationTitleForTests,
                Is.EqualTo(Localization.Get("confirm.respec.title")));
            Assert.That(
                session.ConfirmationBodyForTests,
                Is.EqualTo(Localization.Format("confirm.respec.body", 2)));
            Assert.That(session.ConfirmationCancelLabelForTests, Is.EqualTo("CANCEL"));

            session.ConfirmationCancelButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.CharacterSheetVisible, Is.True);
            Assert.That(session.Profile.GetSkillRank(SkillNode.Strength), Is.EqualTo(1));
            Assert.That(session.Profile.GetSkillRank(SkillNode.Vitality), Is.EqualTo(1));
            Assert.That(session.Profile.SkillPoints, Is.EqualTo(pointsBefore));
        }

        [UnityTest]
        public IEnumerator Respec_confirmation_confirm_refunds_allocated_points()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddXp(int.MaxValue);
            session.Profile.AllocateSkill(SkillNode.Strength);
            session.Profile.AllocateSkill(SkillNode.Vitality);

            session.ShowCharacterSheet();
            yield return null;

            session.CharacterRespecButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.ConfirmationVisibleForTests, Is.True);

            session.ConfirmationConfirmButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.CharacterSheetVisible, Is.True);
            Assert.That(session.Profile.GetSkillRank(SkillNode.Strength), Is.EqualTo(0));
            Assert.That(session.Profile.GetSkillRank(SkillNode.Vitality), Is.EqualTo(0));
            Assert.That(session.Profile.SkillPoints, Is.EqualTo(35));
        }

        [UnityTest]
        public IEnumerator Respec_confirmation_is_not_opened_during_an_active_run()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddXp(int.MaxValue);
            session.Profile.AllocateSkill(SkillNode.Strength);
            session.StartRun();
            yield return null;
            session.PauseGame();
            yield return null;

            session.ShowCharacterSheet();
            yield return null;

            Assert.That(session.CharacterRespecButtonForTests.interactable, Is.False);
            session.CharacterRespecButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.Profile.GetSkillRank(SkillNode.Strength), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Leave_run_confirmation_cancel_returns_to_paused()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(500);
            session.StartRun();
            yield return null;
            var run = session.Run;
            var suppliesBefore = session.Profile.Supplies;
            session.PauseGame();
            yield return null;

            session.PauseMenuButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.True);
            Assert.That(
                session.ConfirmationTitleForTests,
                Is.EqualTo(Localization.Get("confirm.leave.title")));
            Assert.That(
                session.ConfirmationBodyForTests,
                Is.EqualTo(Localization.Get("confirm.leave.body")));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));

            session.ConfirmationCancelButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));
            Assert.That(session.PausePanelVisibleForTests, Is.True);
            Assert.That(session.Run, Is.SameAs(run));
            Assert.That(session.Profile.Supplies, Is.EqualTo(suppliesBefore));
        }

        [UnityTest]
        public IEnumerator Leave_run_confirmation_confirm_keeps_persistent_progression()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            var profile = session.Profile;
            profile.AddSupplies(2000);
            Assert.That(session.TryPurchaseWeapon(WeaponId.Smg), Is.EqualTo(ArmoryPurchaseResult.Purchased));
            Assert.That(session.TryPurchaseArmor(ArmorTier.Light), Is.EqualTo(ArmorPurchaseResult.Purchased));
            profile.AddXp(int.MaxValue);
            profile.AllocateSkill(SkillNode.Strength);
            profile.AllocateSkill(SkillNode.Vitality);
            var suppliesBefore = profile.Supplies;
            var levelBefore = profile.Level;
            var pointsBefore = profile.SkillPoints;

            session.StartRun();
            yield return null;
            session.PauseGame();
            yield return null;

            session.PauseMenuButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(session.ConfirmationVisibleForTests, Is.True);
            Assert.That(
                session.ConfirmationBodyForTests,
                Is.EqualTo(Localization.Get("confirm.leave.body")));

            session.ConfirmationConfirmButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.MainMenu));
            Assert.That(session.HudVisible, Is.False);
            Assert.That(session.PlayerActive, Is.False);
            Assert.That(profile.Supplies, Is.GreaterThanOrEqualTo(suppliesBefore));
            Assert.That(profile.Level, Is.EqualTo(levelBefore));
            Assert.That(profile.SkillPoints, Is.EqualTo(pointsBefore));
            Assert.That(profile.GetSkillRank(SkillNode.Strength), Is.EqualTo(1));
            Assert.That(profile.GetSkillRank(SkillNode.Vitality), Is.EqualTo(1));
            Assert.That(profile.HasWeapon(WeaponId.Smg), Is.True);
            Assert.That(profile.HasArmor(ArmorTier.Light), Is.True);
        }

        [UnityTest]
        public IEnumerator Leave_run_confirmation_is_not_shown_without_an_active_run()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;

            session.PauseMenuButtonForTests.onClick.Invoke();
            yield return null;

            Assert.That(session.ConfirmationVisibleForTests, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.MainMenu));
        }

        [UnityTest]
        public IEnumerator Respec_confirmation_fits_phone_and_tablet_in_all_locales()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddXp(int.MaxValue);
            session.Profile.AllocateSkill(SkillNode.Strength);
            session.ShowCharacterSheet();
            yield return null;

            foreach (var size in new[] { new Vector2(1280f, 720f), new Vector2(1600f, 1200f) })
            {
                foreach (var locale in new[] { "en", "es" })
                {
                    Localization.SetLocale(locale);
                    yield return null;
                    session.UseDesignLayoutForTests(size);
                    yield return null;

                    session.CharacterRespecButtonForTests.onClick.Invoke();
                    yield return null;
                    AssertNoOverflow(session, $"respec {locale} {size.x}x{size.y}");

                    session.ConfirmationCancelButtonForTests.onClick.Invoke();
                    yield return null;
                }

                Localization.UsePseudoLocaleForTests();
                yield return null;
                session.UseDesignLayoutForTests(size);
                yield return null;

                session.CharacterRespecButtonForTests.onClick.Invoke();
                yield return null;
                AssertNoOverflow(session, $"respec {Localization.PseudoLocale} {size.x}x{size.y}");

                session.ConfirmationCancelButtonForTests.onClick.Invoke();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Leave_run_confirmation_fits_phone_and_tablet_in_all_locales()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            yield return null;
            session.PauseGame();
            yield return null;

            foreach (var size in new[] { new Vector2(1280f, 720f), new Vector2(1600f, 1200f) })
            {
                foreach (var locale in new[] { "en", "es" })
                {
                    Localization.SetLocale(locale);
                    yield return null;
                    session.UseDesignLayoutForTests(size);
                    yield return null;

                    session.PauseMenuButtonForTests.onClick.Invoke();
                    yield return null;
                    AssertNoOverflow(session, $"leave {locale} {size.x}x{size.y}");

                    session.ConfirmationCancelButtonForTests.onClick.Invoke();
                    yield return null;
                }

                Localization.UsePseudoLocaleForTests();
                yield return null;
                session.UseDesignLayoutForTests(size);
                yield return null;

                session.PauseMenuButtonForTests.onClick.Invoke();
                yield return null;
                AssertNoOverflow(session, $"leave {Localization.PseudoLocale} {size.x}x{size.y}");

                session.ConfirmationCancelButtonForTests.onClick.Invoke();
                yield return null;
            }
        }

        private static void AssertNoOverflow(GameSession session, string screen)
        {
            var overflows = session.TextOverflowsForTests;
            Assert.That(overflows, Is.Empty, $"{screen}\n{string.Join("\n", overflows)}");
        }

        private static GameBootstrap CreateBootstrap()
        {
            var gameObject = new GameObject("Confirmation Test Bootstrap");
            return gameObject.AddComponent<GameBootstrap>();
        }
    }
}
