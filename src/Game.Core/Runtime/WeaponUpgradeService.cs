using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// One persisted permanent upgrade tier. Unity's JSON serializer supports
    /// this additive record without a schema bump.
    /// </summary>
    [Serializable]
    public sealed class WeaponUpgrade
    {
        public WeaponId WeaponId;
        public WeaponUpgradeStat Stat;
        public int Tier;

        public WeaponUpgrade()
        {
        }

        public WeaponUpgrade(WeaponId weaponId, WeaponUpgradeStat stat, int tier)
        {
            WeaponId = weaponId;
            Stat = stat;
            Tier = tier;
        }
    }

    /// <summary>
    /// Outcome of an upgrade purchase attempt.
    /// </summary>
    public enum WeaponUpgradePurchaseResult
    {
        Purchased,
        MaxTier,
        InsufficientSupplies,
        NotOwned,
        UnknownWeapon,
        UnknownStat
    }

    /// <summary>
    /// Atomic permanent weapon upgrade rules and effective stat aggregation.
    /// </summary>
    public static class WeaponUpgradeService
    {
        public static WeaponUpgradePurchaseResult TryPurchase(
            Profile profile,
            WeaponId weaponId,
            WeaponUpgradeStat stat)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!WeaponCatalog.IsDefined(weaponId))
            {
                return WeaponUpgradePurchaseResult.UnknownWeapon;
            }

            if (!WeaponUpgradeCatalog.IsDefined(stat))
            {
                return WeaponUpgradePurchaseResult.UnknownStat;
            }

            if (!profile.HasWeapon(weaponId))
            {
                return WeaponUpgradePurchaseResult.NotOwned;
            }

            var track = WeaponUpgradeCatalog.Get(weaponId, stat);
            var currentTier = profile.GetWeaponUpgradeTier(weaponId, stat);
            if (currentTier >= track.MaxTier)
            {
                return WeaponUpgradePurchaseResult.MaxTier;
            }

            var cost = track.GetCost(currentTier + 1);
            if (cost > 0 && !profile.SpendSupplies(cost))
            {
                return WeaponUpgradePurchaseResult.InsufficientSupplies;
            }

            profile.SetWeaponUpgradeTier(weaponId, stat, currentTier + 1);
            return WeaponUpgradePurchaseResult.Purchased;
        }

        public static WeaponStats GetEffectiveStats(Profile profile, WeaponId weaponId)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var stats = WeaponCatalog.GetStats(weaponId);
            var tracks = WeaponUpgradeCatalog.GetTracks(weaponId);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                stats = track.Apply(stats, profile.GetWeaponUpgradeTier(weaponId, track.Stat));
            }

            return stats;
        }

        public static int GetNextCost(Profile profile, WeaponId weaponId, WeaponUpgradeStat stat)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var track = WeaponUpgradeCatalog.Get(weaponId, stat);
            var currentTier = profile.GetWeaponUpgradeTier(weaponId, stat);
            return currentTier >= track.MaxTier ? 0 : track.GetCost(currentTier + 1);
        }
    }
}
