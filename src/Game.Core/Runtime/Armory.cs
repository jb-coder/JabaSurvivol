using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Outcome of an armory purchase attempt.
    /// </summary>
    public enum ArmoryPurchaseResult
    {
        Purchased,
        AlreadyOwned,
        InsufficientSupplies,
        UnknownWeapon
    }

    /// <summary>
    /// Engine-free armory rules. Purchases are atomic: supplies are spent only
    /// when the weapon is valid, not owned, and affordable.
    /// </summary>
    public static class ArmoryService
    {
        public static ArmoryPurchaseResult PurchaseWeapon(Profile profile, WeaponId weaponId)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!WeaponCatalog.IsDefined(weaponId))
            {
                return ArmoryPurchaseResult.UnknownWeapon;
            }

            if (profile.HasWeapon(weaponId))
            {
                return ArmoryPurchaseResult.AlreadyOwned;
            }

            var cost = WeaponCatalog.Get(weaponId).UnlockCost;
            if (cost > 0 && !profile.SpendSupplies(cost))
            {
                return ArmoryPurchaseResult.InsufficientSupplies;
            }

            profile.UnlockWeapon(weaponId);
            return ArmoryPurchaseResult.Purchased;
        }

        public static bool EquipWeapon(Profile profile, WeaponId weaponId)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return profile.EquipWeapon(weaponId);
        }
    }
}
