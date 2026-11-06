using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Persistent player progression. Death never removes any field of this type.
    /// The type is serializable by JSON-based save stores.
    /// </summary>
    [Serializable]
    public sealed class Profile
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxLevel = 30;
        public const int MaxSkillPoints = 35;

        public int SchemaVersion = CurrentSchemaVersion;
        public int Supplies;
        public int Xp;
        public int Level = 1;
        public int SkillPoints;
        public int HighestRound = 1;
        public int TotalKills;
        public int TotalRuns;
        public int BestKills;

        /// <summary>
        /// Permanently unlocked weapons. The pistol is always available and is
        /// restored by <see cref="Validate"/> for older saves.
        /// </summary>
        public List<WeaponId> UnlockedWeapons = new List<WeaponId> { WeaponId.Pistol };

        /// <summary>
        /// Permanently equipped weapon. Combat reads this value at run start
        /// and after an armory equip action.
        /// </summary>
        public WeaponId EquippedWeapon = WeaponId.Pistol;

        /// <summary>
        /// Permanent weapon upgrade tiers. Additive to schema version 1.
        /// </summary>
        public List<WeaponUpgrade> WeaponUpgrades = new List<WeaponUpgrade>();

        /// <summary>
        /// Permanently unlocked armor tiers. None is always available.
        /// </summary>
        public List<ArmorTier> UnlockedArmor = new List<ArmorTier> { ArmorTier.None };

        /// <summary>
        /// Permanently equipped armor tier. Run armor condition is not stored.
        /// </summary>
        public ArmorTier EquippedArmor = ArmorTier.None;

        /// <summary>
        /// Permanent armor upgrade tiers. Additive to schema version 1.
        /// </summary>
        public List<ArmorUpgrade> ArmorUpgrades = new List<ArmorUpgrade>();

        /// <summary>
        /// XP needed to advance from <paramref name="level"/> to the next level.
        /// Returns zero at the level cap.
        /// </summary>
        public static int XpToNext(int level)
        {
            if (level >= MaxLevel)
            {
                return 0;
            }

            if (level < 1)
            {
                return XpToNext(1);
            }

            if (level <= 5)
            {
                return 100 * level;
            }

            if (level <= 10)
            {
                return 500 + ((level - 5) * 140);
            }

            if (level <= 20)
            {
                return 1200 + ((level - 10) * 280);
            }

            return 4000 + ((level - 20) * 600);
        }

        public int AddSupplies(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            Supplies = SafeAdd(Supplies, amount);
            return amount;
        }

        public bool SpendSupplies(int amount)
        {
            if (amount <= 0 || Supplies < amount)
            {
                return false;
            }

            Supplies -= amount;
            return true;
        }

        public bool HasWeapon(WeaponId weaponId)
        {
            return weaponId == WeaponId.Pistol ||
                   (UnlockedWeapons != null && UnlockedWeapons.Contains(weaponId));
        }

        /// <summary>
        /// Adds a weapon to the permanent unlock list. Returns true only when
        /// the weapon was newly unlocked.
        /// </summary>
        public bool UnlockWeapon(WeaponId weaponId)
        {
            if (!WeaponCatalog.IsDefined(weaponId) || HasWeapon(weaponId))
            {
                return false;
            }

            if (UnlockedWeapons == null)
            {
                UnlockedWeapons = new List<WeaponId>();
            }

            UnlockedWeapons.Add(weaponId);
            return true;
        }

        /// <summary>
        /// Equips a permanently unlocked weapon. Returns false for locked or
        /// invalid weapons.
        /// </summary>
        public bool EquipWeapon(WeaponId weaponId)
        {
            if (!WeaponCatalog.IsDefined(weaponId) || !HasWeapon(weaponId))
            {
                return false;
            }

            EquippedWeapon = weaponId;
            return true;
        }

        public int GetWeaponUpgradeTier(WeaponId weaponId, WeaponUpgradeStat stat)
        {
            if (!WeaponCatalog.IsDefined(weaponId) || !WeaponUpgradeCatalog.IsDefined(stat))
            {
                return 0;
            }

            var maxTier = WeaponUpgradeCatalog.Get(weaponId, stat).MaxTier;
            var tier = 0;
            if (WeaponUpgrades != null)
            {
                for (var i = 0; i < WeaponUpgrades.Count; i++)
                {
                    var upgrade = WeaponUpgrades[i];
                    if (upgrade != null &&
                        upgrade.WeaponId == weaponId &&
                        upgrade.Stat == stat)
                    {
                        tier = Math.Max(tier, upgrade.Tier);
                    }
                }
            }

            return Math.Clamp(tier, 0, maxTier);
        }

        /// <summary>
        /// Sets one permanent upgrade tier. Returns false when the weapon is
        /// invalid, locked, or the stat is unknown.
        /// </summary>
        public bool SetWeaponUpgradeTier(WeaponId weaponId, WeaponUpgradeStat stat, int tier)
        {
            if (!WeaponCatalog.IsDefined(weaponId) ||
                !WeaponUpgradeCatalog.IsDefined(stat) ||
                !HasWeapon(weaponId))
            {
                return false;
            }

            var maxTier = WeaponUpgradeCatalog.Get(weaponId, stat).MaxTier;
            var clampedTier = Math.Clamp(tier, 0, maxTier);
            if (WeaponUpgrades == null)
            {
                WeaponUpgrades = new List<WeaponUpgrade>();
            }

            for (var i = WeaponUpgrades.Count - 1; i >= 0; i--)
            {
                var upgrade = WeaponUpgrades[i];
                if (upgrade != null && upgrade.WeaponId == weaponId && upgrade.Stat == stat)
                {
                    WeaponUpgrades.RemoveAt(i);
                }
            }

            if (clampedTier > 0)
            {
                WeaponUpgrades.Add(new WeaponUpgrade(weaponId, stat, clampedTier));
            }

            return true;
        }

        public bool HasArmor(ArmorTier tier)
        {
            return tier == ArmorTier.None ||
                   (UnlockedArmor != null && UnlockedArmor.Contains(tier));
        }

        public bool UnlockArmor(ArmorTier tier)
        {
            if (!ArmorCatalog.IsDefined(tier) || HasArmor(tier))
            {
                return false;
            }

            if (UnlockedArmor == null)
            {
                UnlockedArmor = new List<ArmorTier>();
            }

            UnlockedArmor.Add(tier);
            return true;
        }

        public bool EquipArmor(ArmorTier tier)
        {
            if (!ArmorCatalog.IsDefined(tier) || !HasArmor(tier))
            {
                return false;
            }

            EquippedArmor = tier;
            return true;
        }

        public int GetArmorUpgradeTier(ArmorTier tier)
        {
            if (!ArmorCatalog.IsDefined(tier))
            {
                return 0;
            }

            var maxTier = ArmorCatalog.Get(tier).MaxUpgradeTier;
            var upgradeTier = 0;
            if (ArmorUpgrades != null)
            {
                for (var i = 0; i < ArmorUpgrades.Count; i++)
                {
                    var upgrade = ArmorUpgrades[i];
                    if (upgrade != null && upgrade.Tier == tier)
                    {
                        upgradeTier = Math.Max(upgradeTier, upgrade.UpgradeTier);
                    }
                }
            }

            return Math.Clamp(upgradeTier, 0, maxTier);
        }

        public bool SetArmorUpgradeTier(ArmorTier tier, int upgradeTier)
        {
            if (!ArmorCatalog.IsDefined(tier) || !HasArmor(tier))
            {
                return false;
            }

            var maxTier = ArmorCatalog.Get(tier).MaxUpgradeTier;
            var clampedTier = Math.Clamp(upgradeTier, 0, maxTier);
            if (ArmorUpgrades == null)
            {
                ArmorUpgrades = new List<ArmorUpgrade>();
            }

            for (var i = ArmorUpgrades.Count - 1; i >= 0; i--)
            {
                var upgrade = ArmorUpgrades[i];
                if (upgrade != null && upgrade.Tier == tier)
                {
                    ArmorUpgrades.RemoveAt(i);
                }
            }

            if (clampedTier > 0)
            {
                ArmorUpgrades.Add(new ArmorUpgrade(tier, clampedTier));
            }

            return true;
        }

        /// <summary>
        /// Adds XP and returns the number of levels gained.
        /// </summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel)
            {
                return 0;
            }

            Xp = SafeAdd(Xp, amount);

            var levelsGained = 0;
            while (Level < MaxLevel)
            {
                var required = XpToNext(Level);
                if (required <= 0 || Xp < required)
                {
                    break;
                }

                Xp -= required;
                Level++;
                levelsGained++;
                SkillPoints++;

                if (Level % 5 == 0)
                {
                    SkillPoints++;
                }
            }

            if (Level >= MaxLevel)
            {
                Xp = 0;
            }

            ClampSkillPoints();
            return levelsGained;
        }

        /// <summary>
        /// Records the end of a run without removing any progression.
        /// </summary>
        public void RecordRunEnd(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            TotalRuns++;
            TotalKills = SafeAdd(TotalKills, run.Kills);
            HighestRound = Math.Max(HighestRound, run.Round);
            BestKills = Math.Max(BestKills, run.Kills);
        }

        /// <summary>
        /// Normalizes data loaded from disk. Additive schema changes keep the
        /// current version so older saves remain playable.
        /// </summary>
        public void Validate()
        {
            if (SchemaVersion <= 0 || SchemaVersion > CurrentSchemaVersion)
            {
                SchemaVersion = CurrentSchemaVersion;
            }

            Supplies = Math.Max(0, Supplies);
            Xp = Math.Max(0, Xp);
            Level = Math.Clamp(Level, 1, MaxLevel);

            if (Level >= MaxLevel)
            {
                Xp = 0;
            }

            HighestRound = Math.Max(1, HighestRound);
            TotalKills = Math.Max(0, TotalKills);
            TotalRuns = Math.Max(0, TotalRuns);
            BestKills = Math.Max(0, BestKills);
            ClampSkillPoints();
            ValidateWeapons();
            ValidateWeaponUpgrades();
            ValidateArmor();
            ValidateArmorUpgrades();
        }

        private void ValidateWeaponUpgrades()
        {
            if (WeaponUpgrades == null)
            {
                WeaponUpgrades = new List<WeaponUpgrade>();
            }

            for (var i = WeaponUpgrades.Count - 1; i >= 0; i--)
            {
                var upgrade = WeaponUpgrades[i];
                if (upgrade == null ||
                    !WeaponCatalog.IsDefined(upgrade.WeaponId) ||
                    !WeaponUpgradeCatalog.IsDefined(upgrade.Stat) ||
                    !HasWeapon(upgrade.WeaponId) ||
                    upgrade.Tier <= 0)
                {
                    WeaponUpgrades.RemoveAt(i);
                    continue;
                }

                var maxTier = WeaponUpgradeCatalog.Get(upgrade.WeaponId, upgrade.Stat).MaxTier;
                upgrade.Tier = Math.Min(upgrade.Tier, maxTier);

                for (var j = 0; j < i; j++)
                {
                    var earlier = WeaponUpgrades[j];
                    if (earlier == null ||
                        earlier.WeaponId != upgrade.WeaponId ||
                        earlier.Stat != upgrade.Stat)
                    {
                        continue;
                    }

                    earlier.Tier = Math.Max(earlier.Tier, upgrade.Tier);
                    WeaponUpgrades.RemoveAt(i);
                    break;
                }
            }
        }

        private void ValidateArmor()
        {
            if (UnlockedArmor == null)
            {
                UnlockedArmor = new List<ArmorTier>();
            }

            for (var i = UnlockedArmor.Count - 1; i >= 0; i--)
            {
                var tier = UnlockedArmor[i];
                if (!ArmorCatalog.IsDefined(tier))
                {
                    UnlockedArmor.RemoveAt(i);
                    continue;
                }

                for (var j = 0; j < i; j++)
                {
                    if (UnlockedArmor[j] == tier)
                    {
                        UnlockedArmor.RemoveAt(i);
                        break;
                    }
                }
            }

            if (!UnlockedArmor.Contains(ArmorTier.None))
            {
                UnlockedArmor.Insert(0, ArmorTier.None);
            }

            if (!ArmorCatalog.IsDefined(EquippedArmor) || !HasArmor(EquippedArmor))
            {
                EquippedArmor = ArmorTier.None;
            }
        }

        private void ValidateArmorUpgrades()
        {
            if (ArmorUpgrades == null)
            {
                ArmorUpgrades = new List<ArmorUpgrade>();
            }

            for (var i = ArmorUpgrades.Count - 1; i >= 0; i--)
            {
                var upgrade = ArmorUpgrades[i];
                if (upgrade == null ||
                    !ArmorCatalog.IsDefined(upgrade.Tier) ||
                    !HasArmor(upgrade.Tier) ||
                    upgrade.UpgradeTier <= 0)
                {
                    ArmorUpgrades.RemoveAt(i);
                    continue;
                }

                var maxTier = ArmorCatalog.Get(upgrade.Tier).MaxUpgradeTier;
                upgrade.UpgradeTier = Math.Min(upgrade.UpgradeTier, maxTier);
                if (upgrade.UpgradeTier <= 0)
                {
                    ArmorUpgrades.RemoveAt(i);
                    continue;
                }

                for (var j = 0; j < i; j++)
                {
                    var earlier = ArmorUpgrades[j];
                    if (earlier == null || earlier.Tier != upgrade.Tier)
                    {
                        continue;
                    }

                    earlier.UpgradeTier = Math.Max(earlier.UpgradeTier, upgrade.UpgradeTier);
                    ArmorUpgrades.RemoveAt(i);
                    break;
                }
            }
        }

        private void ValidateWeapons()
        {
            if (UnlockedWeapons == null)
            {
                UnlockedWeapons = new List<WeaponId>();
            }

            for (var i = UnlockedWeapons.Count - 1; i >= 0; i--)
            {
                var weaponId = UnlockedWeapons[i];
                if (!WeaponCatalog.IsDefined(weaponId))
                {
                    UnlockedWeapons.RemoveAt(i);
                    continue;
                }

                for (var j = 0; j < i; j++)
                {
                    if (UnlockedWeapons[j] == weaponId)
                    {
                        UnlockedWeapons.RemoveAt(i);
                        break;
                    }
                }
            }

            if (!UnlockedWeapons.Contains(WeaponId.Pistol))
            {
                UnlockedWeapons.Insert(0, WeaponId.Pistol);
            }

            if (!WeaponCatalog.IsDefined(EquippedWeapon) || !HasWeapon(EquippedWeapon))
            {
                EquippedWeapon = WeaponId.Pistol;
            }
        }

        private void ClampSkillPoints()
        {
            SkillPoints = Math.Clamp(SkillPoints, 0, MaxSkillPoints);
        }

        private static int SafeAdd(int left, int right)
        {
            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
