using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    public enum SkillBranch
    {
        Strength = 0,
        Agility = 1,
        Survival = 2
    }

    public enum SkillNode
    {
        Strength = 0,
        Vitality = 1,
        Shockwave = 2,
        Speed = 3,
        Precision = 4,
        Dash = 5,
        Endurance = 6,
        Engineering = 7,
        Turret = 8
    }

    /// <summary>
    /// One passive or active skill node. Active nodes expose rank-scaled
    /// activation data through <see cref="SkillActivationStats"/>.
    /// </summary>
    public sealed class SkillNodeDefinition
    {
        public SkillNodeDefinition(
            SkillNode node,
            SkillBranch branch,
            int maxRanks,
            float damageBonusPerRank = 0f,
            float maxHealthPerRank = 0f,
            float moveSpeedBonusPerRank = 0f,
            float criticalChancePerRank = 0f,
            float headshotBonusPerRank = 0f,
            int armorPerRank = 0,
            float reloadSpeedBonusPerRank = 0f,
            float ammoCapacityBonusPerRank = 0f,
            bool isActiveSlot = false,
            float baseCooldownSeconds = 0f,
            float cooldownReductionPerRank = 0f,
            float areaDamagePerRank = 0f,
            float areaRadiusPerRank = 0f,
            float dashDistancePerRank = 0f,
            float turretDamagePerRank = 0f,
            float turretDurationPerRank = 0f,
            float baseAreaDamage = 0f,
            float baseAreaRadius = 0f,
            float baseDashDistance = 0f,
            float baseTurretDamage = 0f,
            float baseTurretDuration = 0f,
            float baseTurretFireInterval = 0f)
        {
            if (maxRanks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRanks));
            }

            Node = node;
            Branch = branch;
            MaxRanks = maxRanks;
            DamageBonusPerRank = damageBonusPerRank;
            MaxHealthPerRank = maxHealthPerRank;
            MoveSpeedBonusPerRank = moveSpeedBonusPerRank;
            CriticalChancePerRank = criticalChancePerRank;
            HeadshotBonusPerRank = headshotBonusPerRank;
            ArmorPerRank = armorPerRank;
            ReloadSpeedBonusPerRank = reloadSpeedBonusPerRank;
            AmmoCapacityBonusPerRank = ammoCapacityBonusPerRank;
            IsActiveSlot = isActiveSlot;
            BaseCooldownSeconds = baseCooldownSeconds;
            CooldownReductionPerRank = cooldownReductionPerRank;
            AreaDamagePerRank = areaDamagePerRank;
            AreaRadiusPerRank = areaRadiusPerRank;
            DashDistancePerRank = dashDistancePerRank;
            TurretDamagePerRank = turretDamagePerRank;
            TurretDurationPerRank = turretDurationPerRank;
            BaseAreaDamage = baseAreaDamage;
            BaseAreaRadius = baseAreaRadius;
            BaseDashDistance = baseDashDistance;
            BaseTurretDamage = baseTurretDamage;
            BaseTurretDuration = baseTurretDuration;
            BaseTurretFireInterval = baseTurretFireInterval;
        }

        public SkillNode Node { get; }

        public SkillBranch Branch { get; }

        public int MaxRanks { get; }

        public float DamageBonusPerRank { get; }

        public float MaxHealthPerRank { get; }

        public float MoveSpeedBonusPerRank { get; }

        public float CriticalChancePerRank { get; }

        public float HeadshotBonusPerRank { get; }

        public int ArmorPerRank { get; }

        public float ReloadSpeedBonusPerRank { get; }

        public float AmmoCapacityBonusPerRank { get; }

        public bool IsActiveSlot { get; }

        public float BaseCooldownSeconds { get; }

        public float CooldownReductionPerRank { get; }

        public float AreaDamagePerRank { get; }

        public float AreaRadiusPerRank { get; }

        public float DashDistancePerRank { get; }

        public float TurretDamagePerRank { get; }

        public float TurretDurationPerRank { get; }

        public float BaseAreaDamage { get; }

        public float BaseAreaRadius { get; }

        public float BaseDashDistance { get; }

        public float BaseTurretDamage { get; }

        public float BaseTurretDuration { get; }

        public float BaseTurretFireInterval { get; }

        /// <summary>
        /// Per-rank activation data for active nodes. Passive nodes return
        /// zero values and a zero cooldown.
        /// </summary>
        public SkillActivationStats GetActivationStats(int rank)
        {
            var clampedRank = Math.Clamp(rank, 0, MaxRanks);
            return new SkillActivationStats(
                BaseAreaDamage + (AreaDamagePerRank * clampedRank),
                BaseAreaRadius + (AreaRadiusPerRank * clampedRank),
                BaseDashDistance + (DashDistancePerRank * clampedRank),
                BaseTurretDamage + (TurretDamagePerRank * clampedRank),
                BaseTurretDuration + (TurretDurationPerRank * clampedRank),
                Math.Max(0f, BaseCooldownSeconds - (CooldownReductionPerRank * clampedRank)),
                BaseTurretFireInterval);
        }
    }

    /// <summary>
    /// Rank-scaled activation values for Shockwave, Dash, and Turret. Turret
    /// activation is implemented; Shockwave and Dash activation are later
    /// scope.
    /// </summary>
    public readonly struct SkillActivationStats
    {
        public SkillActivationStats(
            float areaDamage,
            float areaRadius,
            float dashDistance,
            float turretDamage,
            float turretDuration,
            float cooldownSeconds,
            float turretFireInterval)
        {
            AreaDamage = areaDamage;
            AreaRadius = areaRadius;
            DashDistance = dashDistance;
            TurretDamage = turretDamage;
            TurretDuration = turretDuration;
            CooldownSeconds = cooldownSeconds;
            TurretFireInterval = turretFireInterval;
        }

        public float AreaDamage { get; }

        public float AreaRadius { get; }

        public float DashDistance { get; }

        public float TurretDamage { get; }

        public float TurretDuration { get; }

        public float CooldownSeconds { get; }

        public float TurretFireInterval { get; }
    }

    /// <summary>
    /// One persisted skill rank allocation.
    /// </summary>
    [Serializable]
    public sealed class SkillAllocation
    {
        public SkillNode Node;
        public int Rank;

        public SkillAllocation()
        {
        }

        public SkillAllocation(SkillNode node, int rank)
        {
            Node = node;
            Rank = rank;
        }
    }

    /// <summary>
    /// Data-driven skill tree for the nine MVP nodes.
    /// </summary>
    public static class SkillCatalog
    {
        private static readonly SkillNodeDefinition[] Nodes =
        {
            new SkillNodeDefinition(SkillNode.Strength, SkillBranch.Strength, maxRanks: 5, damageBonusPerRank: 0.03f),
            new SkillNodeDefinition(SkillNode.Vitality, SkillBranch.Strength, maxRanks: 5, maxHealthPerRank: 10f),
            new SkillNodeDefinition(
                SkillNode.Shockwave,
                SkillBranch.Strength,
                maxRanks: 5,
                baseCooldownSeconds: 10f,
                cooldownReductionPerRank: 1f,
                areaDamagePerRank: 10f,
                areaRadiusPerRank: 0.25f,
                baseAreaDamage: 30f,
                baseAreaRadius: 1.75f),
            new SkillNodeDefinition(SkillNode.Speed, SkillBranch.Agility, maxRanks: 5, moveSpeedBonusPerRank: 0.02f),
            new SkillNodeDefinition(SkillNode.Precision, SkillBranch.Agility, maxRanks: 5, criticalChancePerRank: 0.01f, headshotBonusPerRank: 0.05f),
            new SkillNodeDefinition(
                SkillNode.Dash,
                SkillBranch.Agility,
                maxRanks: 5,
                baseCooldownSeconds: 6f,
                cooldownReductionPerRank: 0.5f,
                dashDistancePerRank: 0.5f,
                baseDashDistance: 2.5f),
            new SkillNodeDefinition(SkillNode.Endurance, SkillBranch.Survival, maxRanks: 5, armorPerRank: 5),
            new SkillNodeDefinition(SkillNode.Engineering, SkillBranch.Survival, maxRanks: 5, reloadSpeedBonusPerRank: 0.05f, ammoCapacityBonusPerRank: 0.05f),
            new SkillNodeDefinition(
                SkillNode.Turret,
                SkillBranch.Survival,
                maxRanks: 5,
                isActiveSlot: true,
                baseCooldownSeconds: 20f,
                cooldownReductionPerRank: 1f,
                turretDamagePerRank: 2f,
                turretDurationPerRank: 2f,
                baseTurretDamage: 6f,
                baseTurretDuration: 6f,
                baseTurretFireInterval: 0.5f)
        };

        public static IReadOnlyList<SkillNodeDefinition> All => Nodes;

        public static SkillNodeDefinition Get(SkillNode node)
        {
            if (!IsDefined(node))
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }

            return Nodes[(int)node];
        }

        public static bool IsDefined(SkillNode node)
        {
            return node >= SkillNode.Strength && node <= SkillNode.Turret;
        }

        public static bool IsDefined(SkillBranch branch)
        {
            return branch >= SkillBranch.Strength && branch <= SkillBranch.Survival;
        }
    }

    /// <summary>
    /// Caps from the game design documentation.
    /// </summary>
    public static class StatCaps
    {
        public const float MoveSpeedBonus = 0.5f;
        public const float DamageBonus = 2.0f;
        public const int Armor = 200;
        public const float CriticalChance = 0.5f;
        public const float HeadshotBonus = 1.0f;
    }

    /// <summary>
    /// Non-persistent modifiers such as temporary buffs or future consumables.
    /// </summary>
    public sealed class TemporaryStatModifiers
    {
        public float MoveSpeedBonus { get; set; }

        public float DamageBonus { get; set; }

        public int ArmorBonus { get; set; }

        public float CriticalChanceBonus { get; set; }

        public float HeadshotBonusBonus { get; set; }

        public float MaxHealthBonus { get; set; }

        public float ReloadSpeedBonus { get; set; }

        public float AmmoCapacityBonus { get; set; }
    }

    /// <summary>
    /// Final character combat values after every source is combined.
    /// </summary>
    public readonly struct CharacterStats
    {
        public CharacterStats(
            float maxHealth,
            float moveSpeed,
            float weaponDamageMultiplier,
            float effectiveWeaponDamage,
            float criticalChance,
            float headshotBonus,
            int armor,
            float reloadSpeedMultiplier,
            float ammoCapacityMultiplier)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            WeaponDamageMultiplier = weaponDamageMultiplier;
            EffectiveWeaponDamage = effectiveWeaponDamage;
            CriticalChance = criticalChance;
            HeadshotBonus = headshotBonus;
            Armor = armor;
            ReloadSpeedMultiplier = reloadSpeedMultiplier;
            AmmoCapacityMultiplier = ammoCapacityMultiplier;
        }

        public float MaxHealth { get; }

        public float MoveSpeed { get; }

        public float WeaponDamageMultiplier { get; }

        public float EffectiveWeaponDamage { get; }

        public float CriticalChance { get; }

        public float HeadshotBonus { get; }

        public int Armor { get; }

        /// <summary>
        /// Multiplier on reload speed. Reload time is divided by this value.
        /// </summary>
        public float ReloadSpeedMultiplier { get; }

        public float AmmoCapacityMultiplier { get; }
    }

    /// <summary>
    /// Deterministic stat aggregation: base game values, skill ranks, the
    /// effective weapon (including upgrades), the effective armor, and
    /// optional temporary modifiers.
    /// </summary>
    public static class StatAggregation
    {
        public static CharacterStats Compute(
            Profile profile,
            WeaponStats weaponStats,
            ArmorStats armorStats,
            TemporaryStatModifiers? temporary = null)
        {
            return ComputeInternal(profile, weaponStats, armorStats, temporary, applyCaps: true);
        }

        /// <summary>
        /// Computes the same values without applying the documented caps. Used
        /// by the balance simulation to detect over-cap builds.
        /// </summary>
        public static CharacterStats ComputeRaw(
            Profile profile,
            WeaponStats weaponStats,
            ArmorStats armorStats,
            TemporaryStatModifiers? temporary = null)
        {
            return ComputeInternal(profile, weaponStats, armorStats, temporary, applyCaps: false);
        }

        private static CharacterStats ComputeInternal(
            Profile profile,
            WeaponStats weaponStats,
            ArmorStats armorStats,
            TemporaryStatModifiers? temporary,
            bool applyCaps)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            temporary ??= new TemporaryStatModifiers();

            var strength = profile.GetSkillRank(SkillNode.Strength);
            var vitality = profile.GetSkillRank(SkillNode.Vitality);
            var speed = profile.GetSkillRank(SkillNode.Speed);
            var precision = profile.GetSkillRank(SkillNode.Precision);
            var endurance = profile.GetSkillRank(SkillNode.Endurance);
            var engineering = profile.GetSkillRank(SkillNode.Engineering);

            var maxHealth = GameBalance.PlayerMaxHealth + (vitality * 10f) + temporary.MaxHealthBonus;
            var moveBonus = (speed * 0.02f) + temporary.MoveSpeedBonus;
            var damageBonus = (strength * 0.03f) + temporary.DamageBonus;
            var baseWeaponDamage = WeaponCatalog.GetStats(weaponStats.Id).Damage;
            var upgradeMultiplier = baseWeaponDamage <= 0f
                ? 1f
                : weaponStats.Damage / baseWeaponDamage;
            var damageMultiplier = upgradeMultiplier * (1f + damageBonus);
            var criticalChance = (precision * 0.01f) + temporary.CriticalChanceBonus;
            var headshotBonus = (precision * 0.05f) + temporary.HeadshotBonusBonus;
            var armor = armorStats.AbsorbAmount + (endurance * 5) + temporary.ArmorBonus;

            if (applyCaps)
            {
                moveBonus = Math.Min(StatCaps.MoveSpeedBonus, moveBonus);
                damageMultiplier = Math.Min(1f + StatCaps.DamageBonus, damageMultiplier);
                criticalChance = Math.Min(StatCaps.CriticalChance, criticalChance);
                headshotBonus = Math.Min(StatCaps.HeadshotBonus, headshotBonus);
                armor = Math.Min(StatCaps.Armor, armor);
            }

            return new CharacterStats(
                maxHealth,
                GameBalance.PlayerMoveSpeed * (1f + moveBonus),
                damageMultiplier,
                baseWeaponDamage * damageMultiplier,
                criticalChance,
                headshotBonus,
                armor,
                (engineering * 0.05f) + 1f + temporary.ReloadSpeedBonus,
                (engineering * 0.05f) + 1f + temporary.AmmoCapacityBonus);
        }

        /// <summary>
        /// Computes the character stats as if one additional rank of the node
        /// were allocated. Used for the armory/character-sheet preview.
        /// </summary>
        public static CharacterStats PreviewAddRank(
            Profile profile,
            SkillNode node,
            WeaponStats weaponStats,
            ArmorStats armorStats)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!SkillCatalog.IsDefined(node))
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }

            var definition = SkillCatalog.Get(node);
            var temporary = new TemporaryStatModifiers
            {
                DamageBonus = definition.DamageBonusPerRank,
                MaxHealthBonus = definition.MaxHealthPerRank,
                MoveSpeedBonus = definition.MoveSpeedBonusPerRank,
                CriticalChanceBonus = definition.CriticalChancePerRank,
                HeadshotBonusBonus = definition.HeadshotBonusPerRank,
                ArmorBonus = definition.ArmorPerRank,
                ReloadSpeedBonus = definition.ReloadSpeedBonusPerRank,
                AmmoCapacityBonus = definition.AmmoCapacityBonusPerRank
            };

            return Compute(profile, weaponStats, armorStats, temporary);
        }
    }
}
