using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Inputs for a deterministic, engine-free balance simulation.
    /// </summary>
    public sealed class BalanceSimulationOptions
    {
        public int Seed { get; set; } = 20261106;

        public int StartingRound { get; set; } = 1;

        public int MaxRound { get; set; } = GameBalance.MaxRound;

        public float PistolDamage { get; set; } = GameBalance.PistolDamage;

        public float FireInterval { get; set; } = GameBalance.PistolFireInterval;

        public int RunawaySupplyThreshold { get; set; } = 6000;

        public int UnreachableUpgradeCost { get; set; } = 2000;
    }

    /// <summary>
    /// One simulated round with pacing and economy values.
    /// </summary>
    public readonly struct RoundSimulation
    {
        public RoundSimulation(
            int round,
            int spawnBudget,
            float zombieHealth,
            float zombieSpeed,
            float zombieDamage,
            bool isElite,
            bool isBoss,
            int shotsToKill,
            float killSeconds,
            float roundSeconds,
            int suppliesEarned,
            int xpEarned,
            RoundRewardOption reward,
            int cumulativeSupplies,
            int cumulativeXp,
            int level,
            int skillPoints)
        {
            Round = round;
            SpawnBudget = spawnBudget;
            ZombieHealth = zombieHealth;
            ZombieSpeed = zombieSpeed;
            ZombieDamage = zombieDamage;
            IsElite = isElite;
            IsBoss = isBoss;
            ShotsToKill = shotsToKill;
            KillSeconds = killSeconds;
            RoundSeconds = roundSeconds;
            SuppliesEarned = suppliesEarned;
            XpEarned = xpEarned;
            Reward = reward;
            CumulativeSupplies = cumulativeSupplies;
            CumulativeXp = cumulativeXp;
            Level = level;
            SkillPoints = skillPoints;
        }

        public int Round { get; }

        public int SpawnBudget { get; }

        public float ZombieHealth { get; }

        public float ZombieSpeed { get; }

        public float ZombieDamage { get; }

        public bool IsElite { get; }

        public bool IsBoss { get; }

        public int ShotsToKill { get; }

        public float KillSeconds { get; }

        public float RoundSeconds { get; }

        public int SuppliesEarned { get; }

        public int XpEarned { get; }

        public RoundRewardOption Reward { get; }

        public int CumulativeSupplies { get; }

        public int CumulativeXp { get; }

        public int Level { get; }

        public int SkillPoints { get; }
    }

    /// <summary>
    /// Full simulation result with per-round rows and outlier flags.
    /// </summary>
    public sealed class BalanceSimulationReport
    {
        private readonly RoundSimulation[] _rounds;

        public BalanceSimulationReport(int seed, RoundSimulation[] rounds, string[] outliers)
        {
            Seed = seed;
            _rounds = rounds;
            Outliers = outliers;
        }

        public int Seed { get; }

        public IReadOnlyList<RoundSimulation> Rounds => _rounds;

        public IReadOnlyList<string> Outliers { get; }

        public bool HasOutliers => Outliers.Count > 0;

        public int TotalSupplies => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].CumulativeSupplies;

        public int TotalXp => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].CumulativeXp;

        public int FinalLevel => _rounds.Length == 0 ? 1 : _rounds[_rounds.Length - 1].Level;

        public int FinalSkillPoints => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].SkillPoints;
    }

    /// <summary>
    /// Headless round-pacing and economy simulation. It uses the same core
    /// rules as the game and never references Unity.
    /// </summary>
    public static class BalanceSimulation
    {
        public static BalanceSimulationReport Run(BalanceSimulationOptions? options = null)
        {
            options ??= new BalanceSimulationOptions();

            if (options.StartingRound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (options.MaxRound < options.StartingRound || options.MaxRound > GameBalance.MaxRound)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (options.PistolDamage <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (options.FireInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            var profile = new Profile();
            var run = new RunState(GameBalance.PlayerMaxHealth, options.StartingRound, options.Seed);
            var rounds = new List<RoundSimulation>();
            var outliers = new List<string>();
            var cumulativeXp = 0;

            for (var round = options.StartingRound; round <= options.MaxRound; round++)
            {
                var definition = run.RoundModel.Definition;
                var budget = definition.SpawnBudget;
                var shotsToKill = Math.Max(1, (int)Math.Ceiling(definition.ZombieMaxHealth / options.PistolDamage));
                var killSeconds = shotsToKill * options.FireInterval;
                var roundSeconds = budget * (killSeconds + GameBalance.ZombieRespawnDelay);

                var suppliesEarned = 0;
                var xpEarned = 0;

                for (var i = 0; i < budget; i++)
                {
                    var kill = run.RegisterKill();
                    suppliesEarned += kill.Supplies;
                    xpEarned += kill.Xp;
                    profile.AddSupplies(kill.Supplies);
                    profile.AddXp(kill.Xp);
                }

                var reward = SelectReward(RoundRewardModel.GenerateOptions(round, options.Seed));
                var rewardResult = RoundRewardModel.Apply(reward, profile, run);
                suppliesEarned += rewardResult.Supplies;
                xpEarned += rewardResult.Xp;
                cumulativeXp += xpEarned;

                rounds.Add(new RoundSimulation(
                    round,
                    budget,
                    definition.ZombieMaxHealth,
                    definition.ZombieMoveSpeed,
                    definition.ZombieContactDamage,
                    definition.IsElite,
                    definition.IsBoss,
                    shotsToKill,
                    killSeconds,
                    roundSeconds,
                    suppliesEarned,
                    xpEarned,
                    reward,
                    profile.Supplies,
                    cumulativeXp,
                    profile.Level,
                    profile.SkillPoints));

                if (shotsToKill <= 1)
                {
                    outliers.Add($"Round {round} is trivial: one pistol shot kills a zombie.");
                }

                if (round < options.MaxRound && !run.AdvanceRound())
                {
                    throw new InvalidOperationException($"Round {round} did not advance during the simulation.");
                }
            }

            if (rounds.Count > 0)
            {
                var last = rounds[rounds.Count - 1];

                if (last.CumulativeSupplies > options.RunawaySupplyThreshold)
                {
                    outliers.Add(
                        $"Supplies are runaway: {last.CumulativeSupplies} total exceeds {options.RunawaySupplyThreshold}.");
                }

                if (last.CumulativeSupplies < options.UnreachableUpgradeCost)
                {
                    outliers.Add(
                        $"Upgrades are unreachable: {last.CumulativeSupplies} total is below {options.UnreachableUpgradeCost}.");
                }

                if (last.Level >= Profile.MaxLevel && options.MaxRound < Profile.MaxLevel)
                {
                    outliers.Add($"Level cap reached before round {options.MaxRound}.");
                }
            }

            return new BalanceSimulationReport(options.Seed, rounds.ToArray(), outliers.ToArray());
        }

        private static RoundRewardOption SelectReward(RoundRewardOption[] options)
        {
            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].Type == RoundRewardType.Supplies)
                {
                    return options[i];
                }
            }

            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].Type == RoundRewardType.Xp)
                {
                    return options[i];
                }
            }

            return options.Length == 0 ? default : options[0];
        }
    }
}
