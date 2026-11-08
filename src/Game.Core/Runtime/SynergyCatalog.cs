using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    public enum SynergyId
    {
        Berserker = 0,
        Ghost = 1,
        Bastion = 2
    }

    /// <summary>
    /// One passive synergy. Synergies do not cost skill points; they activate
    /// when both prerequisite nodes have the required rank.
    /// </summary>
    public sealed class SynergyDefinition
    {
        public SynergyDefinition(
            SynergyId id,
            SkillNode nodeA,
            int rankA,
            SkillNode nodeB,
            int rankB,
            float damageBonus = 0f,
            float moveSpeedBonus = 0f,
            float durationSeconds = 0f,
            float regenDelaySeconds = 0f,
            float regenPerSecond = 0f)
        {
            Id = id;
            NodeA = nodeA;
            RankA = rankA;
            NodeB = nodeB;
            RankB = rankB;
            DamageBonus = damageBonus;
            MoveSpeedBonus = moveSpeedBonus;
            DurationSeconds = durationSeconds;
            RegenDelaySeconds = regenDelaySeconds;
            RegenPerSecond = regenPerSecond;
        }

        public SynergyId Id { get; }

        public SkillNode NodeA { get; }

        public int RankA { get; }

        public SkillNode NodeB { get; }

        public int RankB { get; }

        public float DamageBonus { get; }

        public float MoveSpeedBonus { get; }

        public float DurationSeconds { get; }

        public float RegenDelaySeconds { get; }

        public float RegenPerSecond { get; }
    }

    /// <summary>
    /// The three documented MVP synergies.
    /// </summary>
    public static class SynergyCatalog
    {
        private static readonly SynergyDefinition[] Synergies =
        {
            new SynergyDefinition(
                SynergyId.Berserker,
                SkillNode.Strength,
                3,
                SkillNode.Vitality,
                3,
                damageBonus: 0.10f),
            new SynergyDefinition(
                SynergyId.Ghost,
                SkillNode.Speed,
                3,
                SkillNode.Precision,
                3,
                moveSpeedBonus: 0.10f,
                durationSeconds: 2f),
            new SynergyDefinition(
                SynergyId.Bastion,
                SkillNode.Endurance,
                3,
                SkillNode.Engineering,
                3,
                regenDelaySeconds: 5f,
                regenPerSecond: 2f)
        };

        public static IReadOnlyList<SynergyDefinition> All => Synergies;

        public static SynergyDefinition Get(SynergyId id)
        {
            if (!IsDefined(id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return Synergies[(int)id];
        }

        public static bool IsDefined(SynergyId id)
        {
            return id >= SynergyId.Berserker && id <= SynergyId.Bastion;
        }

        /// <summary>
        /// Returns true when both prerequisite nodes have the required ranks.
        /// </summary>
        public static bool IsUnlocked(Profile profile, SynergyId id)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var definition = Get(id);
            return profile.GetSkillRank(definition.NodeA) >= definition.RankA &&
                   profile.GetSkillRank(definition.NodeB) >= definition.RankB;
        }
    }

    /// <summary>
    /// Run-only synergy timers. Berserker is derived from the current health
    /// fraction, Ghost uses a short timer after a kill, and Bastion tracks
    /// time since the last damage taken.
    /// </summary>
    public sealed class SynergyState
    {
        private float _outOfCombatSeconds;

        public float GhostRemaining { get; private set; }

        public float OutOfCombatSeconds => _outOfCombatSeconds;

        public bool GhostActive => GhostRemaining > 0f;

        public bool BerserkerActive(Profile profile, float healthFraction)
        {
            return healthFraction < 0.5f &&
                   SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker);
        }

        public bool BastionActive(Profile profile)
        {
            return SynergyCatalog.IsUnlocked(profile, SynergyId.Bastion);
        }

        public void OnKill(Profile profile)
        {
            if (SynergyCatalog.IsUnlocked(profile, SynergyId.Ghost))
            {
                GhostRemaining = SynergyCatalog.Get(SynergyId.Ghost).DurationSeconds;
            }
        }

        public void OnDamageTaken()
        {
            _outOfCombatSeconds = 0f;
        }

        /// <summary>
        /// Advances timers and returns the armor to regenerate this step.
        /// </summary>
        public float Tick(float deltaTime, Profile profile)
        {
            var step = Math.Max(0f, deltaTime);
            GhostRemaining = Math.Max(0f, GhostRemaining - step);
            _outOfCombatSeconds += step;

            if (!SynergyCatalog.IsUnlocked(profile, SynergyId.Bastion))
            {
                return 0f;
            }

            var definition = SynergyCatalog.Get(SynergyId.Bastion);
            return _outOfCombatSeconds < definition.RegenDelaySeconds
                ? 0f
                : definition.RegenPerSecond * step;
        }

        public void Reset()
        {
            GhostRemaining = 0f;
            _outOfCombatSeconds = 0f;
        }
    }

    /// <summary>
    /// Maps active synergies to the temporary modifiers consumed by
    /// <see cref="StatAggregation"/>. Callers pass the result explicitly so
    /// repeated aggregation never double-applies a synergy.
    /// </summary>
    public static class SynergyService
    {
        public static TemporaryStatModifiers GetTemporaryModifiers(
            Profile profile,
            float healthFraction,
            bool ghostActive)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var modifiers = new TemporaryStatModifiers();
            if (healthFraction < 0.5f &&
                SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker))
            {
                modifiers.DamageBonus += SynergyCatalog.Get(SynergyId.Berserker).DamageBonus;
            }

            if (ghostActive &&
                SynergyCatalog.IsUnlocked(profile, SynergyId.Ghost))
            {
                modifiers.MoveSpeedBonus += SynergyCatalog.Get(SynergyId.Ghost).MoveSpeedBonus;
            }

            return modifiers;
        }
    }
}
