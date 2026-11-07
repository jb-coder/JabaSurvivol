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
    /// One passive or active skill node. Nodes without stat fields are
    /// activation-based and remain data-only until their system lands.
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
            float ammoCapacityBonusPerRank = 0f)
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
            new SkillNodeDefinition(SkillNode.Shockwave, SkillBranch.Strength, maxRanks: 5),
            new SkillNodeDefinition(SkillNode.Speed, SkillBranch.Agility, maxRanks: 5, moveSpeedBonusPerRank: 0.02f),
            new SkillNodeDefinition(SkillNode.Precision, SkillBranch.Agility, maxRanks: 5, criticalChancePerRank: 0.01f, headshotBonusPerRank: 0.05f),
            new SkillNodeDefinition(SkillNode.Dash, SkillBranch.Agility, maxRanks: 5),
            new SkillNodeDefinition(SkillNode.Endurance, SkillBranch.Survival, maxRanks: 5, armorPerRank: 5),
            new SkillNodeDefinition(SkillNode.Engineering, SkillBranch.Survival, maxRanks: 5, reloadSpeedBonusPerRank: 0.05f, ammoCapacityBonusPerRank: 0.05f),
            new SkillNodeDefinition(SkillNode.Turret, SkillBranch.Survival, maxRanks: 5)
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

            var maxHealth = GameBalance.PlayerMaxHealth + (vitality * 10f);
            var moveBonus = Math.Min(
                StatCaps.MoveSpeedBonus,
                (speed * 0.02f) + temporary.MoveSpeedBonus);
            var damageBonus = Math.Min(
                StatCaps.DamageBonus,
                (strength * 0.03f) + temporary.DamageBonus);
            var damageMultiplier = 1f + damageBonus;
            var criticalChance = Math.Min(
                StatCaps.CriticalChance,
                (precision * 0.01f) + temporary.CriticalChanceBonus);
            var headshotBonus = Math.Min(
                StatCaps.HeadshotBonus,
                (precision * 0.05f) + temporary.HeadshotBonusBonus);
            var armor = Math.Min(
                StatCaps.Armor,
                armorStats.AbsorbAmount + (endurance * 5) + temporary.ArmorBonus);

            return new CharacterStats(
                maxHealth,
                GameBalance.PlayerMoveSpeed * (1f + moveBonus),
                damageMultiplier,
                weaponStats.Damage * damageMultiplier,
                criticalChance,
                headshotBonus,
                armor,
                (engineering * 0.05f) + 1f,
                (engineering * 0.05f) + 1f);
        }
    }
}
