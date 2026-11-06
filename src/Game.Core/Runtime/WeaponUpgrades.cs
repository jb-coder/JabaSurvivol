using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Permanent weapon upgrade stats.
    /// </summary>
    public enum WeaponUpgradeStat
    {
        Damage = 0,
        FireRate = 1,
        MagazineSize = 2,
        ReloadSpeed = 3
    }

    /// <summary>
    /// Data-driven upgrade track for one weapon stat. Costs grow per tier and
    /// the effect is applied to the effective weapon stats.
    /// </summary>
    public readonly struct WeaponUpgradeTrack
    {
        public WeaponUpgradeTrack(
            WeaponUpgradeStat stat,
            int maxTier,
            int baseCost,
            int costPerTier,
            float effectPerTier)
        {
            if (maxTier <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTier));
            }

            if (baseCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCost));
            }

            if (costPerTier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(costPerTier));
            }

            if (effectPerTier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(effectPerTier));
            }

            Stat = stat;
            MaxTier = maxTier;
            BaseCost = baseCost;
            CostPerTier = costPerTier;
            EffectPerTier = effectPerTier;
        }

        public WeaponUpgradeStat Stat { get; }

        public int MaxTier { get; }

        public int BaseCost { get; }

        public int CostPerTier { get; }

        /// <summary>
        /// Fractional effect per tier. Damage adds to damage, fire rate adds
        /// to shots per second, magazine adds to capacity, and reload adds to
        /// reload speed.
        /// </summary>
        public float EffectPerTier { get; }

        public int GetCost(int nextTier)
        {
            if (nextTier <= 0 || nextTier > MaxTier)
            {
                throw new ArgumentOutOfRangeException(nameof(nextTier));
            }

            return BaseCost + (CostPerTier * (nextTier - 1));
        }

        public WeaponStats Apply(WeaponStats stats, int tier)
        {
            if (tier <= 0)
            {
                return stats;
            }

            var clampedTier = Math.Min(tier, MaxTier);
            var bonus = EffectPerTier * clampedTier;

            return Stat switch
            {
                WeaponUpgradeStat.Damage => WithDamage(stats, stats.Damage * (1f + bonus)),
                WeaponUpgradeStat.FireRate => WithFireInterval(stats, stats.FireInterval / (1f + bonus)),
                WeaponUpgradeStat.MagazineSize => WithMagazine(stats, (int)Math.Ceiling(stats.MagazineSize * (1f + bonus))),
                WeaponUpgradeStat.ReloadSpeed => WithReload(stats, stats.ReloadSeconds / (1f + bonus)),
                _ => throw new ArgumentOutOfRangeException(nameof(Stat))
            };
        }

        private static WeaponStats WithDamage(WeaponStats stats, float damage)
        {
            return new WeaponStats(
                stats.Id,
                damage,
                stats.FireInterval,
                stats.MagazineSize,
                stats.ReloadSeconds,
                stats.Range,
                stats.ProjectileSpeed,
                stats.ReserveAmmo,
                stats.UnlockCost,
                stats.PelletCount,
                stats.SpreadDegrees);
        }

        private static WeaponStats WithFireInterval(WeaponStats stats, float fireInterval)
        {
            return new WeaponStats(
                stats.Id,
                stats.Damage,
                fireInterval,
                stats.MagazineSize,
                stats.ReloadSeconds,
                stats.Range,
                stats.ProjectileSpeed,
                stats.ReserveAmmo,
                stats.UnlockCost,
                stats.PelletCount,
                stats.SpreadDegrees);
        }

        private static WeaponStats WithMagazine(WeaponStats stats, int magazineSize)
        {
            return new WeaponStats(
                stats.Id,
                stats.Damage,
                stats.FireInterval,
                magazineSize,
                stats.ReloadSeconds,
                stats.Range,
                stats.ProjectileSpeed,
                stats.ReserveAmmo,
                stats.UnlockCost,
                stats.PelletCount,
                stats.SpreadDegrees);
        }

        private static WeaponStats WithReload(WeaponStats stats, float reloadSeconds)
        {
            return new WeaponStats(
                stats.Id,
                stats.Damage,
                stats.FireInterval,
                stats.MagazineSize,
                reloadSeconds,
                stats.Range,
                stats.ProjectileSpeed,
                stats.ReserveAmmo,
                stats.UnlockCost,
                stats.PelletCount,
                stats.SpreadDegrees);
        }
    }

    /// <summary>
    /// Data-driven upgrade tracks for every weapon and stat.
    /// </summary>
    public static class WeaponUpgradeCatalog
    {
        private static readonly WeaponUpgradeTrack[][] Tracks =
        {
            new[]
            {
                Track(WeaponUpgradeStat.Damage, 60, 30, 0.10f),
                Track(WeaponUpgradeStat.FireRate, 60, 30, 0.08f),
                Track(WeaponUpgradeStat.MagazineSize, 50, 25, 0.15f),
                Track(WeaponUpgradeStat.ReloadSpeed, 50, 25, 0.10f)
            },
            new[]
            {
                Track(WeaponUpgradeStat.Damage, 150, 75, 0.10f),
                Track(WeaponUpgradeStat.FireRate, 150, 75, 0.08f),
                Track(WeaponUpgradeStat.MagazineSize, 120, 60, 0.15f),
                Track(WeaponUpgradeStat.ReloadSpeed, 120, 60, 0.10f)
            },
            new[]
            {
                Track(WeaponUpgradeStat.Damage, 250, 125, 0.10f),
                Track(WeaponUpgradeStat.FireRate, 250, 125, 0.08f),
                Track(WeaponUpgradeStat.MagazineSize, 200, 100, 0.15f),
                Track(WeaponUpgradeStat.ReloadSpeed, 200, 100, 0.10f)
            },
            new[]
            {
                Track(WeaponUpgradeStat.Damage, 300, 150, 0.10f),
                Track(WeaponUpgradeStat.FireRate, 300, 150, 0.08f),
                Track(WeaponUpgradeStat.MagazineSize, 250, 125, 0.15f),
                Track(WeaponUpgradeStat.ReloadSpeed, 250, 125, 0.10f)
            },
            new[]
            {
                Track(WeaponUpgradeStat.Damage, 450, 225, 0.10f),
                Track(WeaponUpgradeStat.FireRate, 450, 225, 0.08f),
                Track(WeaponUpgradeStat.MagazineSize, 350, 175, 0.15f),
                Track(WeaponUpgradeStat.ReloadSpeed, 350, 175, 0.10f)
            }
        };

        public static IReadOnlyList<WeaponUpgradeTrack> GetTracks(WeaponId weaponId)
        {
            if (!WeaponCatalog.IsDefined(weaponId))
            {
                throw new ArgumentOutOfRangeException(nameof(weaponId));
            }

            return Tracks[(int)weaponId];
        }

        public static WeaponUpgradeTrack Get(WeaponId weaponId, WeaponUpgradeStat stat)
        {
            if (!WeaponCatalog.IsDefined(weaponId))
            {
                throw new ArgumentOutOfRangeException(nameof(weaponId));
            }

            if (!IsDefined(stat))
            {
                throw new ArgumentOutOfRangeException(nameof(stat));
            }

            return Tracks[(int)weaponId][(int)stat];
        }

        public static bool IsDefined(WeaponUpgradeStat stat)
        {
            return stat >= WeaponUpgradeStat.Damage && stat <= WeaponUpgradeStat.ReloadSpeed;
        }

        private static WeaponUpgradeTrack Track(
            WeaponUpgradeStat stat,
            int baseCost,
            int costPerTier,
            float effectPerTier)
        {
            return new WeaponUpgradeTrack(stat, maxTier: 5, baseCost, costPerTier, effectPerTier);
        }
    }
}
