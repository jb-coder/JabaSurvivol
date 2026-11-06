using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Stable weapon identifiers used by the profile, armory, and combat.
    /// </summary>
    public enum WeaponId
    {
        Pistol = 0,
        Smg = 1,
        Shotgun = 2,
        Rifle = 3,
        Lmg = 4
    }

    /// <summary>
    /// Base combat values for one weapon. Values are data-only so the armory,
    /// combat, and balance simulations read from the same source.
    /// </summary>
    public sealed class WeaponDefinition
    {
        public WeaponDefinition(
            WeaponId id,
            float damage,
            float fireInterval,
            int magazineSize,
            float reloadSeconds,
            float range,
            float projectileSpeed,
            int reserveAmmo,
            int unlockCost,
            int pelletCount = 1,
            float spreadDegrees = 0f)
        {
            if (damage <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            if (fireInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fireInterval));
            }

            if (magazineSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(magazineSize));
            }

            if (reloadSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(reloadSeconds));
            }

            if (range <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(range));
            }

            if (projectileSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(projectileSpeed));
            }

            if (reserveAmmo == 0 || reserveAmmo < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(reserveAmmo));
            }

            if (unlockCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockCost));
            }

            if (pelletCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pelletCount));
            }

            if (spreadDegrees < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spreadDegrees));
            }

            Id = id;
            Stats = new WeaponStats(
                id,
                damage,
                fireInterval,
                magazineSize,
                reloadSeconds,
                range,
                projectileSpeed,
                reserveAmmo,
                unlockCost,
                pelletCount,
                spreadDegrees);
        }

        public WeaponId Id { get; }

        public WeaponStats Stats { get; }

        public float Damage => Stats.Damage;

        public float FireInterval => Stats.FireInterval;

        public int MagazineSize => Stats.MagazineSize;

        public float ReloadSeconds => Stats.ReloadSeconds;

        public float Range => Stats.Range;

        public float ProjectileSpeed => Stats.ProjectileSpeed;

        public int ReserveAmmo => Stats.ReserveAmmo;

        public int UnlockCost => Stats.UnlockCost;

        public int PelletCount => Stats.PelletCount;

        public float SpreadDegrees => Stats.SpreadDegrees;

        public bool HasInfiniteReserve => Stats.HasInfiniteReserve;
    }

    /// <summary>
    /// Combat values after base data and, later, permanent upgrades are
    /// combined. The struct keeps combat reads allocation-free.
    /// </summary>
    public readonly struct WeaponStats
    {
        public WeaponStats(
            WeaponId id,
            float damage,
            float fireInterval,
            int magazineSize,
            float reloadSeconds,
            float range,
            float projectileSpeed,
            int reserveAmmo,
            int unlockCost,
            int pelletCount,
            float spreadDegrees)
        {
            Id = id;
            Damage = damage;
            FireInterval = fireInterval;
            MagazineSize = magazineSize;
            ReloadSeconds = reloadSeconds;
            Range = range;
            ProjectileSpeed = projectileSpeed;
            ReserveAmmo = reserveAmmo;
            UnlockCost = unlockCost;
            PelletCount = pelletCount;
            SpreadDegrees = spreadDegrees;
        }

        public WeaponId Id { get; }

        public float Damage { get; }

        public float FireInterval { get; }

        public int MagazineSize { get; }

        public float ReloadSeconds { get; }

        public float Range { get; }

        public float ProjectileSpeed { get; }

        /// <summary>
        /// Reserve rounds for a run. A value of -1 means infinite reserve.
        /// </summary>
        public int ReserveAmmo { get; }

        public int UnlockCost { get; }

        public int PelletCount { get; }

        public float SpreadDegrees { get; }

        public bool HasInfiniteReserve => ReserveAmmo < 0;

        public float ShotsPerSecond => FireInterval <= 0f ? 0f : 1f / FireInterval;
    }

    /// <summary>
    /// Data-driven catalog for the five MVP weapons. The pistol is always the
    /// starting weapon and has an infinite reserve.
    /// </summary>
    public static class WeaponCatalog
    {
        private static readonly WeaponDefinition[] Weapons =
        {
            new WeaponDefinition(
                WeaponId.Pistol,
                GameBalance.PistolDamage,
                GameBalance.PistolFireInterval,
                magazineSize: 12,
                reloadSeconds: 1.1f,
                range: 9f,
                projectileSpeed: GameBalance.BulletSpeed,
                reserveAmmo: -1,
                unlockCost: 0),
            new WeaponDefinition(
                WeaponId.Smg,
                damage: 9f,
                fireInterval: 0.09f,
                magazineSize: 30,
                reloadSeconds: 1.5f,
                range: 7f,
                projectileSpeed: 15f,
                reserveAmmo: 180,
                unlockCost: 500,
                spreadDegrees: 4f),
            new WeaponDefinition(
                WeaponId.Shotgun,
                damage: 8f,
                fireInterval: 0.8f,
                magazineSize: 6,
                reloadSeconds: 2.2f,
                range: 4.5f,
                projectileSpeed: 12f,
                reserveAmmo: 36,
                unlockCost: 900,
                pelletCount: 6,
                spreadDegrees: 14f),
            new WeaponDefinition(
                WeaponId.Rifle,
                damage: 34f,
                fireInterval: 0.45f,
                magazineSize: 10,
                reloadSeconds: 1.8f,
                range: 12f,
                projectileSpeed: 22f,
                reserveAmmo: 90,
                unlockCost: 1200,
                spreadDegrees: 0.5f),
            new WeaponDefinition(
                WeaponId.Lmg,
                damage: 14f,
                fireInterval: 0.1f,
                magazineSize: 75,
                reloadSeconds: 2.8f,
                range: 9f,
                projectileSpeed: 16f,
                reserveAmmo: 300,
                unlockCost: 2000,
                spreadDegrees: 5f)
        };

        public static IReadOnlyList<WeaponDefinition> All => Weapons;

        public static WeaponDefinition Get(WeaponId id)
        {
            if (!IsDefined(id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return Weapons[(int)id];
        }

        public static WeaponStats GetStats(WeaponId id)
        {
            return Get(id).Stats;
        }

        public static bool IsDefined(WeaponId id)
        {
            return id >= WeaponId.Pistol && id <= WeaponId.Lmg;
        }
    }
}
