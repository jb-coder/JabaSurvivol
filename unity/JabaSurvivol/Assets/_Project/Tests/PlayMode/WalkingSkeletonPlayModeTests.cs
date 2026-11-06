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
    public sealed class WalkingSkeletonPlayModeTests
    {
        private string _directory;
        private string _path;
        private GameBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-playmode", Guid.NewGuid().ToString("N"));
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
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_starts_a_run_with_player_and_zombie()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session, Is.Not.Null);

            _bootstrap.Session.StartRun();
            yield return null;

            Assert.That(_bootstrap.Session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_bootstrap.Session.PlayerActive, Is.True);
            Assert.That(_bootstrap.Session.ZombieActive, Is.True);
            Assert.That(_bootstrap.Session.Run.Player.Health, Is.EqualTo(GameBalance.PlayerMaxHealth));
        }

        [UnityTest]
        public IEnumerator Kill_awards_progress_and_death_keeps_it()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();

            session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            yield return null;

            Assert.That(session.Profile.Supplies, Is.EqualTo(GameBalance.SuppliesPerKill));
            Assert.That(File.Exists(_path), Is.True);

            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.PlayerActive, Is.False);
            Assert.That(session.Profile.Supplies, Is.EqualTo(GameBalance.SuppliesPerKill));
            Assert.That(session.Profile.TotalRuns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Restart_resets_health_and_round_but_keeps_progress()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            var supplies = session.Profile.Supplies;
            session.RestartRun();
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.Run.Player.Health, Is.EqualTo(GameBalance.PlayerMaxHealth));
            Assert.That(session.Run.Round, Is.EqualTo(1));
            Assert.That(session.Run.Kills, Is.EqualTo(0));
            Assert.That(session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Profile_survives_bootstrap_recreation()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            _bootstrap.Session.StartRun();
            _bootstrap.Session.DamageZombieForTests(GameBalance.ZombieMaxHealth);
            _bootstrap.Session.SaveNow();
            yield return null;

            var supplies = _bootstrap.Session.Profile.Supplies;
            UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session.Profile.Supplies, Is.EqualTo(supplies));
        }

        [UnityTest]
        public IEnumerator Full_round_clear_advances_round_and_keeps_progress()
        {
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

            Assert.That(session.RoundPhase, Is.EqualTo(RoundPhase.Cleared));
            Assert.That(session.RoundClearPending, Is.True);
            Assert.That(session.RoundEndVisible, Is.True);
            Assert.That(session.PendingRewardCount, Is.EqualTo(GameBalance.RoundRewardOptionCount));

            var suppliesAfterClear = session.Profile.Supplies;
            var xpAfterClear = session.Profile.Xp;
            var reward = session.PendingRewardAt(1);

            Assert.That(session.SelectRoundReward(1), Is.True);
            yield return null;

            var expectedSupplies = suppliesAfterClear + (reward.Type == RoundRewardType.Supplies ? reward.Amount : 0);
            var expectedXp = xpAfterClear + (reward.Type == RoundRewardType.Xp ? reward.Amount : 0);

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.RoundEndVisible, Is.False);
            Assert.That(session.Run.Round, Is.EqualTo(2));
            Assert.That(session.EnemiesRemaining, Is.EqualTo(session.SpawnBudget));
            Assert.That(session.RoundPhase, Is.EqualTo(RoundPhase.Active));
            Assert.That(session.Profile.Supplies, Is.EqualTo(expectedSupplies));
            Assert.That(session.Profile.Xp, Is.EqualTo(expectedXp));
            Assert.That(File.Exists(_path), Is.True);
        }

        [UnityTest]
        public IEnumerator Round_reward_persists_across_bootstrap_recreation()
        {
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

            var rewardIndex = -1;
            RoundRewardOption reward = default;
            for (var i = 0; i < session.PendingRewardCount; i++)
            {
                var option = session.PendingRewardAt(i);
                if (option.Type == RoundRewardType.Supplies || option.Type == RoundRewardType.Xp)
                {
                    rewardIndex = i;
                    reward = option;
                    break;
                }
            }

            Assert.That(rewardIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(session.RoundEndVisible, Is.True);
            Assert.That(session.SelectRoundReward(rewardIndex), Is.True);
            yield return null;
            Assert.That(session.RoundEndVisible, Is.False);

            var supplies = session.Profile.Supplies;
            var xp = session.Profile.Xp;
            session.SaveNow();
            UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session.Profile.Supplies, Is.EqualTo(supplies));
            Assert.That(_bootstrap.Session.Profile.Xp, Is.EqualTo(xp));
            Assert.That(reward.IsPersistent, Is.True);
        }

        [UnityTest]
        public IEnumerator Scripted_fire_spawns_bullets()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.SetInputForTests(new ScriptedInput
            {
                Frame = new InputFrame { Aim = Vector2.right, FireHeld = true }
            });
            session.StartRun();
            yield return null;
            yield return null;

            Assert.That(session.ActiveBullets, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Pause_stops_the_run_and_resume_continues()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();

            session.PauseGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));

            session.ResumeGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
        }

        [UnityTest]
        public IEnumerator Armory_purchases_equips_and_persists_a_weapon()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Smg).UnlockCost);
            session.SaveNow();

            session.ShowArmory();
            yield return null;

            Assert.That(session.ArmoryVisible, Is.True);
            Assert.That(session.TryPurchaseWeapon(WeaponId.Smg), Is.EqualTo(ArmoryPurchaseResult.Purchased));
            Assert.That(session.Profile.HasWeapon(WeaponId.Smg), Is.True);
            Assert.That(session.Profile.Supplies, Is.EqualTo(0));
            Assert.That(session.TryEquipWeapon(WeaponId.Smg), Is.True);
            Assert.That(session.EquippedWeapon, Is.EqualTo(WeaponId.Smg));

            session.CloseArmory();
            yield return null;
            Assert.That(session.ArmoryVisible, Is.False);
            session.SaveNow();

            UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
            _bootstrap = null;
            yield return null;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session.Profile.HasWeapon(WeaponId.Smg), Is.True);
            Assert.That(_bootstrap.Session.Profile.EquippedWeapon, Is.EqualTo(WeaponId.Smg));
        }

        [UnityTest]
        public IEnumerator Armory_rejects_a_purchase_without_supplies()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Smg).UnlockCost - 1);
            session.SaveNow();
            session.ShowArmory();
            yield return null;

            Assert.That(
                session.TryPurchaseWeapon(WeaponId.Smg),
                Is.EqualTo(ArmoryPurchaseResult.InsufficientSupplies));
            Assert.That(session.Profile.HasWeapon(WeaponId.Smg), Is.False);
            Assert.That(
                session.Profile.Supplies,
                Is.EqualTo(WeaponCatalog.Get(WeaponId.Smg).UnlockCost - 1));
        }

        [UnityTest]
        public IEnumerator Purchased_weapon_survives_death()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Rifle).UnlockCost);
            session.TryPurchaseWeapon(WeaponId.Rifle);
            session.TryEquipWeapon(WeaponId.Rifle);
            session.StartRun();
            yield return null;

            session.ApplyDamage(GameBalance.PlayerMaxHealth * 2f);
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(session.Profile.HasWeapon(WeaponId.Rifle), Is.True);
            Assert.That(session.Profile.EquippedWeapon, Is.EqualTo(WeaponId.Rifle));
        }

        [UnityTest]
        public IEnumerator Equipped_weapon_stats_drive_combat()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Shotgun).UnlockCost);
            session.TryPurchaseWeapon(WeaponId.Shotgun);
            session.TryEquipWeapon(WeaponId.Shotgun);
            session.StartRun();
            yield return null;

            session.FireForTests();
            yield return null;

            var shotgun = WeaponCatalog.Get(WeaponId.Shotgun);
            Assert.That(session.LastBulletDamageForTests, Is.EqualTo(shotgun.Damage));
            Assert.That(session.LastBulletDamageForTests, Is.Not.EqualTo(GameBalance.PistolDamage));
            Assert.That(session.ActiveBullets, Is.GreaterThanOrEqualTo(shotgun.PelletCount));
        }

        [UnityTest]
        public IEnumerator Armory_from_pause_restores_the_hud()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            session.PauseGame();
            yield return null;

            session.ShowArmory();
            yield return null;
            Assert.That(session.ArmoryVisible, Is.True);
            Assert.That(session.HudVisible, Is.False);

            session.CloseArmory();
            yield return null;
            Assert.That(session.ArmoryVisible, Is.False);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Paused));
            Assert.That(session.HudVisible, Is.True);

            session.ResumeGame();
            yield return null;
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.HudVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator Single_pellet_weapon_applies_configured_spread()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            var session = _bootstrap.Session;
            session.Profile.AddSupplies(WeaponCatalog.Get(WeaponId.Smg).UnlockCost);
            session.TryPurchaseWeapon(WeaponId.Smg);
            session.TryEquipWeapon(WeaponId.Smg);
            session.StartRun();
            yield return null;

            session.FireForTests();
            yield return null;

            var smg = WeaponCatalog.Get(WeaponId.Smg);
            var angle = Mathf.Atan2(session.LastShotDirectionForTests.y, session.LastShotDirectionForTests.x) *
                        Mathf.Rad2Deg;
            Assert.That(Mathf.Abs(angle), Is.LessThanOrEqualTo((smg.SpreadDegrees * 0.5f) + 0.01f));
            Assert.That(Mathf.Abs(angle), Is.GreaterThan(0.01f));
            Assert.That(session.EquippedWeaponStats.FireInterval, Is.EqualTo(smg.FireInterval));
            Assert.That(session.EquippedWeaponStats.Range, Is.EqualTo(smg.Range));
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
