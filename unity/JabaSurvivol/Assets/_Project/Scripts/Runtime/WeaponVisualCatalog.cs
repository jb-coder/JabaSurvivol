using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    public enum BulletStyle
    {
        Pistol = 0,
        Smg = 1,
        Shotgun = 2,
        Rifle = 3,
        Lmg = 4,
        Turret = 5
    }

    public readonly struct WeaponVisualProfile
    {
        public WeaponVisualProfile(
            string weaponSprite,
            string projectileSprite,
            string muzzleFlashSprite,
            BulletStyle bulletStyle,
            float muzzleOffset,
            float projectileScale)
        {
            WeaponSprite = weaponSprite;
            ProjectileSprite = projectileSprite;
            MuzzleFlashSprite = muzzleFlashSprite;
            BulletStyle = bulletStyle;
            MuzzleOffset = muzzleOffset;
            ProjectileScale = projectileScale;
        }

        public string WeaponSprite { get; }

        public string ProjectileSprite { get; }

        public string MuzzleFlashSprite { get; }

        public BulletStyle BulletStyle { get; }

        public float MuzzleOffset { get; }

        public float ProjectileScale { get; }
    }

    /// <summary>
    /// Visual-only mapping for weapons, projectiles, and muzzle flashes.
    /// Gameplay stats stay in <see cref="WeaponCatalog"/> and <see cref="WeaponStats"/>.
    /// </summary>
    public static class WeaponVisualCatalog
    {
        public static WeaponVisualProfile Get(WeaponId weaponId)
        {
            return weaponId switch
            {
                WeaponId.Smg => new WeaponVisualProfile(
                    "Weapons/smg",
                    "Projectiles/smg",
                    "Effects/muzzle_flash",
                    BulletStyle.Smg,
                    0.62f,
                    0.68f),
                WeaponId.Shotgun => new WeaponVisualProfile(
                    "Weapons/shotgun",
                    "Projectiles/pellet",
                    "Effects/muzzle_flash",
                    BulletStyle.Shotgun,
                    0.7f,
                    0.72f),
                WeaponId.Rifle => new WeaponVisualProfile(
                    "Weapons/rifle",
                    "Projectiles/rifle",
                    "Effects/muzzle_flash_alt",
                    BulletStyle.Rifle,
                    0.76f,
                    0.92f),
                WeaponId.Lmg => new WeaponVisualProfile(
                    "Weapons/lmg",
                    "Projectiles/lmg",
                    "Effects/muzzle_flash",
                    BulletStyle.Lmg,
                    0.7f,
                    0.95f),
                _ => new WeaponVisualProfile(
                    "Weapons/pistol",
                    "Projectiles/pistol",
                    "Effects/muzzle_flash",
                    BulletStyle.Pistol,
                    0.55f,
                    0.76f)
            };
        }

        public static WeaponVisualProfile Turret => new(
            "Characters/turret_barrel",
            "Projectiles/turret",
            "Effects/muzzle_flash_alt",
            BulletStyle.Turret,
            0.34f,
            0.82f);
    }
}
