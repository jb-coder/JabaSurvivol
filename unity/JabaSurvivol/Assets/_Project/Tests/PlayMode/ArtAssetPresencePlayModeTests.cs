using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class ArtAssetPresencePlayModeTests
    {
        private static readonly string[] RequiredKeys =
        {
            "Characters/player_torso",
            "Characters/player_head",
            "Characters/player_arm",
            "Characters/player_leg",
            "Characters/zombie_walker_torso",
            "Characters/zombie_runner_torso",
            "Characters/zombie_brute_torso",
            "Characters/boss_warden_torso",
            "Characters/boss_warden_core",
            "Characters/turret_base",
            "Characters/turret_head",
            "Characters/turret_barrel",
            "Weapons/pistol",
            "Weapons/smg",
            "Weapons/shotgun",
            "Weapons/rifle",
            "Weapons/lmg",
            "Projectiles/pistol",
            "Projectiles/pellet",
            "Projectiles/turret",
            "Effects/muzzle_flash",
            "Effects/impact",
            "Effects/death_burst",
            "Effects/elite_aura",
            "UI/weapon_pistol",
            "UI/reward_supplies",
            "UI/skill_turret",
            "Environment/floor_tile",
            "Environment/wall_horizontal",
            "App/icon"
        };

        [Test]
        public void Every_required_runtime_sprite_resolves()
        {
            ArtCatalog.ClearForTests();

            foreach (var key in RequiredKeys)
            {
                Assert.That(ArtCatalog.TryGet(key, out var sprite), Is.True, key);
                Assert.That(sprite, Is.Not.Null, key);
            }
        }

        [Test]
        public void Resources_contains_the_full_committed_art_set()
        {
            var sprites = Resources.LoadAll<Sprite>("Art");

            Assert.That(sprites, Is.Not.Null);
            Assert.That(sprites.Length, Is.GreaterThanOrEqualTo(60));
        }
    }
}
