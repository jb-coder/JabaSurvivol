using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Reward categories offered after a round clear.
    /// </summary>
    public enum RoundRewardType
    {
        Supplies,
        Xp,
        AmmoCrate,
        ArmorPlate
    }

    /// <summary>
    /// One deterministic reward option. Supplies and XP are persistent; ammo
    /// and armor are run-only.
    /// </summary>
    public readonly struct RoundRewardOption
    {
        public RoundRewardOption(RoundRewardType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public RoundRewardType Type { get; }

        public int Amount { get; }

        public bool IsPersistent => Type == RoundRewardType.Supplies || Type == RoundRewardType.Xp;

        public bool IsRunOnly => !IsPersistent;
    }

    /// <summary>
    /// Result of applying one reward option.
    /// </summary>
    public readonly struct RoundRewardResult
    {
        public RoundRewardResult(RoundRewardOption option, int supplies, int xp, int ammo, int armor)
        {
            Option = option;
            Supplies = supplies;
            Xp = xp;
            Ammo = ammo;
            Armor = armor;
        }

        public RoundRewardOption Option { get; }

        public int Supplies { get; }

        public int Xp { get; }

        public int Ammo { get; }

        public int Armor { get; }
    }

    /// <summary>
    /// Deterministic round reward generation and application. The model is
    /// engine-free so tests and balance simulations can assert exact choices.
    /// </summary>
    public static class RoundRewardModel
    {
        private static readonly RoundRewardType[] RewardPool =
        {
            RoundRewardType.Supplies,
            RoundRewardType.Xp,
            RoundRewardType.AmmoCrate,
            RoundRewardType.ArmorPlate
        };

        /// <summary>
        /// Generates three distinct options for a seed. The same round and seed
        /// always produce the same option sequence.
        /// </summary>
        public static RoundRewardOption[] GenerateOptions(int round, int seed)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round));
            }

            var pool = new RoundRewardType[RewardPool.Length];
            Array.Copy(RewardPool, pool, RewardPool.Length);

            var random = new DeterministicRandom(CombineSeed(round, seed));
            for (var i = pool.Length - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                var current = pool[i];
                pool[i] = pool[swapIndex];
                pool[swapIndex] = current;
            }

            var options = new RoundRewardOption[GameBalance.RoundRewardOptionCount];
            for (var i = 0; i < options.Length; i++)
            {
                var type = pool[i];
                options[i] = new RoundRewardOption(type, GetAmount(type, round));
            }

            return options;
        }

        /// <summary>
        /// Applies an option to persistent and run-only state. Persistent
        /// rewards are written to the profile; run-only rewards never touch
        /// persistent currency.
        /// </summary>
        public static RoundRewardResult Apply(RoundRewardOption option, Profile profile, RunState run)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            switch (option.Type)
            {
                case RoundRewardType.Supplies:
                    profile.AddSupplies(option.Amount);
                    return new RoundRewardResult(option, option.Amount, 0, 0, 0);

                case RoundRewardType.Xp:
                    profile.AddXp(option.Amount);
                    return new RoundRewardResult(option, 0, option.Amount, 0, 0);

                case RoundRewardType.AmmoCrate:
                    run.AddAmmo(option.Amount);
                    return new RoundRewardResult(option, 0, 0, option.Amount, 0);

                case RoundRewardType.ArmorPlate:
                    run.AddArmor(option.Amount);
                    return new RoundRewardResult(option, 0, 0, 0, option.Amount);

                default:
                    throw new ArgumentOutOfRangeException(nameof(option));
            }
        }

        public static int GetAmount(RoundRewardType type, int round)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round));
            }

            var steps = round - 1;
            return type switch
            {
                RoundRewardType.Supplies => GameBalance.RoundRewardSuppliesBase + (GameBalance.RoundRewardSuppliesPerRound * steps),
                RoundRewardType.Xp => GameBalance.RoundRewardXpBase + (GameBalance.RoundRewardXpPerRound * steps),
                RoundRewardType.AmmoCrate => GameBalance.AmmoCrateAmount,
                RoundRewardType.ArmorPlate => GameBalance.ArmorPlateAmount,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static uint CombineSeed(int round, int seed)
        {
            unchecked
            {
                var value = ((uint)seed * 2654435761u) ^ ((uint)round * 2246822519u);
                return value == 0u ? 1u : value;
            }
        }

        /// <summary>
        /// Small deterministic xorshift generator so choices do not depend on
        /// the runtime version of <see cref="System.Random"/>.
        /// </summary>
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
