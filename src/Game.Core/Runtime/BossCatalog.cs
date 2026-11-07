using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Data-driven round 10 boss. The boss spawns after the regular enemies are
    /// cleared and uses a telegraphed slam attack plus an enrage phase.
    /// </summary>
    public sealed class BossDefinition
    {
        public BossDefinition(
            float maxHealth,
            float moveSpeed,
            float contactDamage,
            float contactInterval,
            float slamDamage,
            float slamRadius,
            float slamTelegraphSeconds,
            float slamIntervalSeconds,
            float enrageHealthFraction,
            float enrageAttackIntervalMultiplier,
            int supplies,
            int xp,
            float visualScale)
        {
            if (maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (moveSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            }

            if (contactDamage <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(contactDamage));
            }

            if (contactInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(contactInterval));
            }

            if (slamDamage <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slamDamage));
            }

            if (slamRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slamRadius));
            }

            if (slamTelegraphSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slamTelegraphSeconds));
            }

            if (slamIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slamIntervalSeconds));
            }

            if (enrageHealthFraction <= 0f || enrageHealthFraction >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(enrageHealthFraction));
            }

            if (enrageAttackIntervalMultiplier <= 0f || enrageAttackIntervalMultiplier > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(enrageAttackIntervalMultiplier));
            }

            if (supplies < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(supplies));
            }

            if (xp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xp));
            }

            if (visualScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(visualScale));
            }

            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ContactInterval = contactInterval;
            SlamDamage = slamDamage;
            SlamRadius = slamRadius;
            SlamTelegraphSeconds = slamTelegraphSeconds;
            SlamIntervalSeconds = slamIntervalSeconds;
            EnrageHealthFraction = enrageHealthFraction;
            EnrageAttackIntervalMultiplier = enrageAttackIntervalMultiplier;
            Supplies = supplies;
            Xp = xp;
            VisualScale = visualScale;
        }

        public float MaxHealth { get; }

        public float MoveSpeed { get; }

        public float ContactDamage { get; }

        public float ContactInterval { get; }

        public float SlamDamage { get; }

        public float SlamRadius { get; }

        public float SlamTelegraphSeconds { get; }

        public float SlamIntervalSeconds { get; }

        public float EnrageHealthFraction { get; }

        public float EnrageAttackIntervalMultiplier { get; }

        public int Supplies { get; }

        public int Xp { get; }

        public float VisualScale { get; }

        public float EnragedSlamIntervalSeconds => SlamIntervalSeconds * EnrageAttackIntervalMultiplier;
    }

    /// <summary>
    /// The single MVP boss definition.
    /// </summary>
    public static class BossCatalog
    {
        private static readonly BossDefinition Boss = new BossDefinition(
            maxHealth: 2400f,
            moveSpeed: 1.15f,
            contactDamage: 30f,
            contactInterval: 0.8f,
            slamDamage: 45f,
            slamRadius: 2.6f,
            slamTelegraphSeconds: 0.9f,
            slamIntervalSeconds: 4.5f,
            enrageHealthFraction: 0.5f,
            enrageAttackIntervalMultiplier: 0.6f,
            supplies: 1000,
            xp: 500,
            visualScale: 2.2f);

        public static BossDefinition BossDefinition => Boss;
    }

    /// <summary>
    /// Run-only boss health and enrage state. Damage application is owned by
    /// the core so Unity, tests, and simulations agree.
    /// </summary>
    public sealed class BossState
    {
        public BossState(BossDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Health = definition.MaxHealth;
        }

        public BossDefinition Definition { get; }

        public float MaxHealth => Definition.MaxHealth;

        public float Health { get; private set; }

        public bool IsAlive => Health > 0f;

        public bool IsEnraged => Health <= MaxHealth * Definition.EnrageHealthFraction;

        public float HealthFraction => MaxHealth <= 0f ? 0f : Math.Max(0f, Health / MaxHealth);

        /// <summary>
        /// Applies damage and returns true when this hit killed the boss.
        /// </summary>
        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return false;
            }

            Health = Math.Max(0f, Health - amount);
            return !IsAlive;
        }
    }
}
