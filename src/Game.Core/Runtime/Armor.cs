using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Persistent armor tiers. None means no armor equipped.
    /// </summary>
    public enum ArmorTier
    {
        None = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3
    }

    /// <summary>
    /// Base armor values. Absorb is the run-only pool that absorbs damage
    /// before health. Upgrades are permanent and increase the pool.
    /// </summary>
    public sealed class ArmorDefinition
    {
        public ArmorDefinition(
            ArmorTier tier,
            int absorbAmount,
            int unlockCost,
            int upgradeBaseCost,
            int upgradeCostPerTier,
            int maxUpgradeTier,
            float upgradeEffectPerTier)
        {
            if (absorbAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absorbAmount));
            }

            if (unlockCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockCost));
            }

            if (upgradeBaseCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeBaseCost));
            }

            if (upgradeCostPerTier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeCostPerTier));
            }

            if (maxUpgradeTier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxUpgradeTier));
            }

            if (upgradeEffectPerTier < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeEffectPerTier));
            }

            Tier = tier;
            AbsorbAmount = absorbAmount;
            UnlockCost = unlockCost;
            UpgradeBaseCost = upgradeBaseCost;
            UpgradeCostPerTier = upgradeCostPerTier;
            MaxUpgradeTier = maxUpgradeTier;
            UpgradeEffectPerTier = upgradeEffectPerTier;
        }

        public ArmorTier Tier { get; }

        public int AbsorbAmount { get; }

        public int UnlockCost { get; }

        public int UpgradeBaseCost { get; }

        public int UpgradeCostPerTier { get; }

        public int MaxUpgradeTier { get; }

        public float UpgradeEffectPerTier { get; }

        public bool CanUpgrade => MaxUpgradeTier > 0;

        public int GetUpgradeCost(int nextTier)
        {
            if (nextTier <= 0 || nextTier > MaxUpgradeTier)
            {
                throw new ArgumentOutOfRangeException(nameof(nextTier));
            }

            return UpgradeBaseCost + (UpgradeCostPerTier * (nextTier - 1));
        }
    }

    /// <summary>
    /// Armor values after permanent upgrades are applied.
    /// </summary>
    public readonly struct ArmorStats
    {
        public ArmorStats(
            ArmorTier tier,
            int absorbAmount,
            int baseAbsorbAmount,
            int unlockCost,
            int upgradeTier,
            int maxUpgradeTier,
            int nextUpgradeCost)
        {
            Tier = tier;
            AbsorbAmount = absorbAmount;
            BaseAbsorbAmount = baseAbsorbAmount;
            UnlockCost = unlockCost;
            UpgradeTier = upgradeTier;
            MaxUpgradeTier = maxUpgradeTier;
            NextUpgradeCost = nextUpgradeCost;
        }

        public ArmorTier Tier { get; }

        public int AbsorbAmount { get; }

        public int BaseAbsorbAmount { get; }

        public int UnlockCost { get; }

        public int UpgradeTier { get; }

        public int MaxUpgradeTier { get; }

        public int NextUpgradeCost { get; }

        public bool IsMaxUpgrade => MaxUpgradeTier > 0 && UpgradeTier >= MaxUpgradeTier;
    }

    /// <summary>
    /// Data-driven armor catalog for the three MVP armor tiers.
    /// </summary>
    public static class ArmorCatalog
    {
        private static readonly ArmorDefinition[] Armors =
        {
            new ArmorDefinition(ArmorTier.None, absorbAmount: 0, unlockCost: 0, upgradeBaseCost: 0, upgradeCostPerTier: 0, maxUpgradeTier: 0, upgradeEffectPerTier: 0f),
            new ArmorDefinition(ArmorTier.Light, absorbAmount: 25, unlockCost: 0, upgradeBaseCost: 100, upgradeCostPerTier: 50, maxUpgradeTier: 5, upgradeEffectPerTier: 0.10f),
            new ArmorDefinition(ArmorTier.Medium, absorbAmount: 50, unlockCost: 800, upgradeBaseCost: 200, upgradeCostPerTier: 100, maxUpgradeTier: 5, upgradeEffectPerTier: 0.10f),
            new ArmorDefinition(ArmorTier.Heavy, absorbAmount: 90, unlockCost: 1600, upgradeBaseCost: 350, upgradeCostPerTier: 175, maxUpgradeTier: 5, upgradeEffectPerTier: 0.10f)
        };

        public static System.Collections.Generic.IReadOnlyList<ArmorDefinition> All => Armors;

        public static ArmorDefinition Get(ArmorTier tier)
        {
            if (!IsDefined(tier))
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            return Armors[(int)tier];
        }

        public static bool IsDefined(ArmorTier tier)
        {
            return tier >= ArmorTier.None && tier <= ArmorTier.Heavy;
        }
    }

    /// <summary>
    /// One persisted permanent armor upgrade.
    /// </summary>
    [Serializable]
    public sealed class ArmorUpgrade
    {
        public ArmorTier Tier;
        public int UpgradeTier;

        public ArmorUpgrade()
        {
        }

        public ArmorUpgrade(ArmorTier tier, int upgradeTier)
        {
            Tier = tier;
            UpgradeTier = upgradeTier;
        }
    }

    public enum ArmorPurchaseResult
    {
        Purchased,
        AlreadyOwned,
        InsufficientSupplies,
        UnknownArmor
    }

    public enum ArmorUpgradePurchaseResult
    {
        Purchased,
        MaxTier,
        InsufficientSupplies,
        NotOwned,
        UnknownArmor
    }

    /// <summary>
    /// Engine-free armor unlock, equip, upgrade, and effective-stat rules.
    /// </summary>
    public static class ArmorService
    {
        public static ArmorPurchaseResult PurchaseArmor(Profile profile, ArmorTier tier)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!ArmorCatalog.IsDefined(tier))
            {
                return ArmorPurchaseResult.UnknownArmor;
            }

            if (profile.HasArmor(tier))
            {
                return ArmorPurchaseResult.AlreadyOwned;
            }

            var cost = ArmorCatalog.Get(tier).UnlockCost;
            if (cost > 0 && !profile.SpendSupplies(cost))
            {
                return ArmorPurchaseResult.InsufficientSupplies;
            }

            profile.UnlockArmor(tier);
            return ArmorPurchaseResult.Purchased;
        }

        public static bool EquipArmor(Profile profile, ArmorTier tier)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return profile.EquipArmor(tier);
        }

        public static ArmorUpgradePurchaseResult TryPurchaseUpgrade(Profile profile, ArmorTier tier)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!ArmorCatalog.IsDefined(tier))
            {
                return ArmorUpgradePurchaseResult.UnknownArmor;
            }

            if (!profile.HasArmor(tier))
            {
                return ArmorUpgradePurchaseResult.NotOwned;
            }

            var definition = ArmorCatalog.Get(tier);
            var currentTier = profile.GetArmorUpgradeTier(tier);
            if (currentTier >= definition.MaxUpgradeTier)
            {
                return ArmorUpgradePurchaseResult.MaxTier;
            }

            var cost = definition.GetUpgradeCost(currentTier + 1);
            if (cost > 0 && !profile.SpendSupplies(cost))
            {
                return ArmorUpgradePurchaseResult.InsufficientSupplies;
            }

            profile.SetArmorUpgradeTier(tier, currentTier + 1);
            return ArmorUpgradePurchaseResult.Purchased;
        }

        public static ArmorStats GetEffectiveStats(Profile profile, ArmorTier tier)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var definition = ArmorCatalog.Get(tier);
            var upgradeTier = profile.GetArmorUpgradeTier(tier);
            var absorb = definition.AbsorbAmount;
            if (definition.MaxUpgradeTier > 0)
            {
                absorb = (int)Math.Round(
                    definition.AbsorbAmount * (1f + (definition.UpgradeEffectPerTier * upgradeTier)),
                    MidpointRounding.AwayFromZero);
            }

            var nextCost = upgradeTier >= definition.MaxUpgradeTier
                ? 0
                : definition.GetUpgradeCost(upgradeTier + 1);

            return new ArmorStats(
                tier,
                absorb,
                definition.AbsorbAmount,
                definition.UnlockCost,
                upgradeTier,
                definition.MaxUpgradeTier,
                nextCost);
        }

        public static int GetNextUpgradeCost(Profile profile, ArmorTier tier)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return GetEffectiveStats(profile, tier).NextUpgradeCost;
        }
    }
}
