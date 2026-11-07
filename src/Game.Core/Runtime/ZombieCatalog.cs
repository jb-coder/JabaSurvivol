using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Stable identifiers for the MVP zombie archetypes.
    /// </summary>
    public enum ZombieArchetype
    {
        Walker = 0,
        Runner = 1,
        Brute = 2
    }

    /// <summary>
    /// Data-driven archetype values. Round scaling and elite modifiers are
    /// applied by <see cref="RoundDefinition.GetEncounter"/>.
    /// </summary>
    public sealed class ZombieDefinition
    {
        public ZombieDefinition(
            ZombieArchetype archetype,
            float healthMultiplier,
            float speedMultiplier,
            float contactDamageMultiplier,
            int supplies,
            int xp,
            float visualScale)
        {
            if (healthMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(healthMultiplier));
            }

            if (speedMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            }

            if (contactDamageMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(contactDamageMultiplier));
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

            Archetype = archetype;
            HealthMultiplier = healthMultiplier;
            SpeedMultiplier = speedMultiplier;
            ContactDamageMultiplier = contactDamageMultiplier;
            Supplies = supplies;
            Xp = xp;
            VisualScale = visualScale;
        }

        public ZombieArchetype Archetype { get; }

        public float HealthMultiplier { get; }

        public float SpeedMultiplier { get; }

        public float ContactDamageMultiplier { get; }

        public int Supplies { get; }

        public int Xp { get; }

        public float VisualScale { get; }
    }

    /// <summary>
    /// Fully resolved values for one spawned enemy. The type is engine-free so
    /// combat, tests, and simulations share the same numbers.
    /// </summary>
    public readonly struct ZombieEncounterStats
    {
        public ZombieEncounterStats(
            ZombieArchetype archetype,
            float maxHealth,
            float moveSpeed,
            float contactDamage,
            float contactInterval,
            float radius,
            float visualScale,
            int supplies,
            int xp,
            bool isElite)
        {
            Archetype = archetype;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            ContactInterval = contactInterval;
            Radius = radius;
            VisualScale = visualScale;
            Supplies = supplies;
            Xp = xp;
            IsElite = isElite;
        }

        public ZombieArchetype Archetype { get; }

        public float MaxHealth { get; }

        public float MoveSpeed { get; }

        public float ContactDamage { get; }

        public float ContactInterval { get; }

        public float Radius { get; }

        public float VisualScale { get; }

        public int Supplies { get; }

        public int Xp { get; }

        public bool IsElite { get; }
    }

    /// <summary>
    /// Data-driven catalog for Walker, Runner, and Brute.
    /// </summary>
    public static class ZombieCatalog
    {
        private static readonly ZombieDefinition[] Zombies =
        {
            new ZombieDefinition(
                ZombieArchetype.Walker,
                healthMultiplier: 1f,
                speedMultiplier: 1f,
                contactDamageMultiplier: 1f,
                supplies: 10,
                xp: 10,
                visualScale: 1f),
            new ZombieDefinition(
                ZombieArchetype.Runner,
                healthMultiplier: 0.7f,
                speedMultiplier: 1.7f,
                contactDamageMultiplier: 0.8f,
                supplies: 15,
                xp: 15,
                visualScale: 0.82f),
            new ZombieDefinition(
                ZombieArchetype.Brute,
                healthMultiplier: 2.6f,
                speedMultiplier: 0.7f,
                contactDamageMultiplier: 1.6f,
                supplies: 30,
                xp: 30,
                visualScale: 1.35f)
        };

        public static System.Collections.Generic.IReadOnlyList<ZombieDefinition> All => Zombies;

        public static ZombieDefinition Get(ZombieArchetype archetype)
        {
            if (!IsDefined(archetype))
            {
                throw new ArgumentOutOfRangeException(nameof(archetype));
            }

            return Zombies[(int)archetype];
        }

        public static bool IsDefined(ZombieArchetype archetype)
        {
            return archetype >= ZombieArchetype.Walker && archetype <= ZombieArchetype.Brute;
        }
    }

    /// <summary>
    /// Deterministic per-round spawn composition. The last slots guarantee at
    /// least one Runner from round 2 and one Brute from round 4 so mixed
    /// archetypes become visible as soon as they are unlocked.
    /// </summary>
    public static class ZombieSpawnPlan
    {
        public static ZombieArchetype[] Build(int round, int count, int seed)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var plan = new ZombieArchetype[count];
            if (count == 0)
            {
                return plan;
            }

            var random = new DeterministicRandom(CombineSeed(round, seed));
            for (var i = 0; i < count; i++)
            {
                plan[i] = Pick(round, ref random);
            }

            if (round >= GameBalance.RunnerSpawnRound && count >= 1 && !Contains(plan, ZombieArchetype.Runner))
            {
                plan[0] = ZombieArchetype.Runner;
            }

            if (round >= GameBalance.BruteSpawnRound && count >= 2 && !Contains(plan, ZombieArchetype.Brute))
            {
                plan[count - 1] = ZombieArchetype.Brute;
            }

            return plan;
        }

        private static ZombieArchetype Pick(int round, ref DeterministicRandom random)
        {
            if (round < GameBalance.RunnerSpawnRound)
            {
                return ZombieArchetype.Walker;
            }

            if (round < GameBalance.BruteSpawnRound)
            {
                return random.Next(100) < 30
                    ? ZombieArchetype.Runner
                    : ZombieArchetype.Walker;
            }

            var roll = random.Next(100);
            if (roll < 28)
            {
                return ZombieArchetype.Brute;
            }

            if (roll < 58)
            {
                return ZombieArchetype.Runner;
            }

            return ZombieArchetype.Walker;
        }

        private static bool Contains(ZombieArchetype[] plan, ZombieArchetype archetype)
        {
            for (var i = 0; i < plan.Length; i++)
            {
                if (plan[i] == archetype)
                {
                    return true;
                }
            }

            return false;
        }

        private static uint CombineSeed(int round, int seed)
        {
            unchecked
            {
                var value = ((uint)seed * 2654435761u) ^ ((uint)round * 2246822519u);
                return value == 0u ? 1u : value;
            }
        }

        private struct DeterministicRandom
        {
            private uint _state;

            public DeterministicRandom(uint seed)
            {
                _state = seed == 0u ? 0x9E3779B9u : seed;
            }

            public int Next(int maxExclusive)
            {
                if (maxExclusive <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxExclusive));
                }

                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (int)(_state % (uint)maxExclusive);
            }
        }
    }
}
