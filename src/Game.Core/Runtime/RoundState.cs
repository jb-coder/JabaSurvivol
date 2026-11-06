using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Lifecycle phase for a single round.
    /// </summary>
    public enum RoundPhase
    {
        NotStarted,
        Active,
        Cleared,
        Failed
    }

    /// <summary>
    /// Data-driven values for a round. The regular zombie stats are scaled by
    /// the round number; elite and boss flags are exposed for later content.
    /// </summary>
    public readonly struct RoundDefinition
    {
        public RoundDefinition(
            int roundNumber,
            int spawnBudget,
            float zombieMaxHealth,
            float zombieMoveSpeed,
            float zombieContactDamage,
            bool isElite,
            bool isBoss)
        {
            RoundNumber = roundNumber;
            SpawnBudget = spawnBudget;
            ZombieMaxHealth = zombieMaxHealth;
            ZombieMoveSpeed = zombieMoveSpeed;
            ZombieContactDamage = zombieContactDamage;
            IsElite = isElite;
            IsBoss = isBoss;
        }

        public int RoundNumber { get; }

        public int SpawnBudget { get; }

        public float ZombieMaxHealth { get; }

        public float ZombieMoveSpeed { get; }

        public float ZombieContactDamage { get; }

        public bool IsElite { get; }

        public bool IsBoss { get; }
    }

    /// <summary>
    /// Deterministic round balance table. Values stay free of engine types so
    /// the same rules run in Unity, tests, and balance simulations.
    /// </summary>
    public static class RoundTable
    {
        public static RoundDefinition Get(int roundNumber)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            var steps = roundNumber - 1;
            var health = GameBalance.ZombieMaxHealth * (1f + (GameBalance.RoundZombieHealthGrowth * steps));
            var speedMultiplier = Math.Min(
                GameBalance.RoundZombieSpeedCapMultiplier,
                1f + (GameBalance.RoundZombieSpeedGrowth * steps));
            var speed = GameBalance.ZombieMoveSpeed * speedMultiplier;
            var damage = GameBalance.ZombieContactDamage * (1f + (GameBalance.RoundZombieDamageGrowth * steps));
            var budget = GameBalance.RoundSpawnBudgetBase + (GameBalance.RoundSpawnBudgetPerRound * roundNumber);

            return new RoundDefinition(
                roundNumber,
                budget,
                health,
                speed,
                damage,
                roundNumber % GameBalance.EliteRoundInterval == 0 && roundNumber != GameBalance.BossRound,
                roundNumber == GameBalance.BossRound);
        }
    }

    /// <summary>
    /// Result of registering one enemy kill against a round.
    /// </summary>
    public readonly struct RoundKillResult
    {
        public RoundKillResult(bool accepted, bool roundCleared, int enemiesRemaining, RoundPhase phase)
        {
            Accepted = accepted;
            RoundCleared = roundCleared;
            EnemiesRemaining = enemiesRemaining;
            Phase = phase;
        }

        public bool Accepted { get; }

        public bool RoundCleared { get; }

        public int EnemiesRemaining { get; }

        public RoundPhase Phase { get; }
    }

    /// <summary>
    /// Deterministic per-round lifecycle. The caller owns the reward flow and
    /// advances to the next round only after the round has been cleared.
    /// </summary>
    public sealed class RoundState
    {
        private RoundDefinition _definition;

        public RoundState(int startingRound = 1)
        {
            if (startingRound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingRound));
            }

            RoundNumber = startingRound;
            _definition = RoundTable.Get(startingRound);
            EnemiesRemaining = _definition.SpawnBudget;
            Phase = RoundPhase.NotStarted;
        }

        public int RoundNumber { get; private set; }

        public RoundPhase Phase { get; private set; }

        public RoundDefinition Definition => _definition;

        public int SpawnBudget => _definition.SpawnBudget;

        public int EnemiesRemaining { get; private set; }

        public int EnemiesSpawned { get; private set; }

        public int KillsThisRound { get; private set; }

        public bool IsActive => Phase == RoundPhase.Active;

        public bool IsCleared => Phase == RoundPhase.Cleared;

        public bool IsFailed => Phase == RoundPhase.Failed;

        public bool IsFinalRound => RoundNumber >= GameBalance.MaxRound;

        public bool CanSpawnMore => IsActive && EnemiesSpawned < SpawnBudget;

        public void Start()
        {
            if (Phase == RoundPhase.NotStarted)
            {
                Phase = RoundPhase.Active;
            }
        }

        /// <summary>
        /// Registers one enemy kill. A cleared round stays cleared until the
        /// caller advances it, which keeps the reward flow explicit.
        /// </summary>
        public RoundKillResult RegisterKill()
        {
            if (Phase != RoundPhase.Active || EnemiesRemaining <= 0)
            {
                return new RoundKillResult(false, false, EnemiesRemaining, Phase);
            }

            KillsThisRound++;
            EnemiesRemaining--;

            if (EnemiesRemaining <= 0)
            {
                Phase = RoundPhase.Cleared;
            }

            return new RoundKillResult(true, Phase == RoundPhase.Cleared, EnemiesRemaining, Phase);
        }

        /// <summary>
        /// Marks that one enemy has been spawned for this round.
        /// </summary>
        public bool TryMarkSpawned()
        {
            if (!CanSpawnMore)
            {
                return false;
            }

            EnemiesSpawned++;
            return true;
        }

        /// <summary>
        /// Advances a cleared round. Returns false while active, failed, or on
        /// the final round.
        /// </summary>
        public bool TryAdvanceToNextRound()
        {
            if (Phase != RoundPhase.Cleared || IsFinalRound)
            {
                return false;
            }

            RoundNumber++;
            _definition = RoundTable.Get(RoundNumber);
            EnemiesRemaining = _definition.SpawnBudget;
            EnemiesSpawned = 0;
            KillsThisRound = 0;
            Phase = RoundPhase.Active;
            return true;
        }

        /// <summary>
        /// Ends the current round when the player dies.
        /// </summary>
        public void Fail()
        {
            if (Phase != RoundPhase.NotStarted)
            {
                Phase = RoundPhase.Failed;
            }
        }

        /// <summary>
        /// Resets the round model for a fresh run.
        /// </summary>
        public void Reset(int startingRound)
        {
            if (startingRound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingRound));
            }

            RoundNumber = startingRound;
            _definition = RoundTable.Get(startingRound);
            EnemiesRemaining = _definition.SpawnBudget;
            EnemiesSpawned = 0;
            KillsThisRound = 0;
            Phase = RoundPhase.NotStarted;
        }
    }
}
