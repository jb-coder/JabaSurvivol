using System.Collections.Generic;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class WeaponVisualCatalogTests
    {
        [Test]
        public void Every_weapon_has_a_distinct_visual_profile()
        {
            var weaponSprites = new HashSet<string>();
            var projectileSprites = new HashSet<string>();

            foreach (var weapon in WeaponCatalog.All)
            {
                var profile = WeaponVisualCatalog.Get(weapon.Id);

                Assert.That(profile.WeaponSprite, Is.Not.Empty, weapon.Id.ToString());
                Assert.That(profile.ProjectileSprite, Is.Not.Empty, weapon.Id.ToString());
                Assert.That(profile.MuzzleFlashSprite, Is.Not.Empty, weapon.Id.ToString());
                Assert.That(profile.MuzzleOffset, Is.GreaterThan(0f), weapon.Id.ToString());
                Assert.That(profile.ProjectileScale, Is.GreaterThan(0f), weapon.Id.ToString());
                Assert.That(weaponSprites.Add(profile.WeaponSprite), Is.True, weapon.Id.ToString());
                Assert.That(projectileSprites.Add(profile.ProjectileSprite), Is.True, weapon.Id.ToString());
            }
        }

        [Test]
        public void Turret_has_a_distinct_projectile_profile()
        {
            var turret = WeaponVisualCatalog.Turret;

            Assert.That(turret.ProjectileSprite, Is.EqualTo("Projectiles/turret"));
            Assert.That(turret.BulletStyle, Is.EqualTo(BulletStyle.Turret));
            Assert.That(turret.ProjectileSprite, Is.Not.EqualTo(WeaponVisualCatalog.Get(WeaponId.Pistol).ProjectileSprite));
        }
    }
}
