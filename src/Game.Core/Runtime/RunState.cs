using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Reward granted when a zombie dies.
    /// </summary>
    public readonly struct KillReward
    {
        public KillReward(int supplies, int xp, bool completedRound, int round, int enemiesRemaining)
        {
            Supplies = supplies;
            Xp = xp;
            CompletedRound = completedRound;
            Round = round;
            EnemiesRemaining = enemiesRemaining;
        }

        public int Supplies { get; }

        public int Xp { get; }

        public bool CompletedRound { get; }

        public int Round { get; }

        public int EnemiesRemaining { get; }
    }

    /// <summary>
    /// State for a single run. Death resets this object only; persistent
    /// progression lives in <see cref="Profile"/>.
    /// </summary>
    public sealed class RunState
    {
        private readonly RoundState _round;

        public RunState(float maxHealth, int startingRound = 1, int seed = 0, int maxArmor = 0)
        {
            if (startingRound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingRound));
            }

            if (maxArmor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArmor));
            }

            Player = new PlayerState(maxHealth);
            StartingRound = startingRound;
            Seed = seed;
            MaxArmor = maxArmor;
            Armor = maxArmor;
            _round = new RoundState(startingRound);
            _round.Start();
        }

        public PlayerState Player { get; }

        public int StartingRound { get; }

        public int Seed { get; }

        public RoundState RoundModel => _round;

        public int Round => _round.RoundNumber;

        public int Kills { get; private set; }

        public int KillsThisRound => _round.KillsThisRound;

        public int EnemiesRemaining => _round.EnemiesRemaining;

        public int SpawnBudget => _round.SpawnBudget;

        public bool IsGameOver { get; private set; }

        /// <summary>
        /// Run-only ammunition. It is never stored in the profile and resets on
        /// death or restart.
        /// </summary>
        public int Ammo { get; private set; }

        /// <summary>
        /// Run-only armor. It is never stored in the profile and resets on
        /// death or restart. It absorbs damage before health.
        /// </summary>
        public int Armor { get; private set; }

        /// <summary>
        /// Maximum armor condition for this run, derived from the equipped
        /// armor tier. Zero means no armor is equipped.
        /// </summary>
        public int MaxArmor { get; }

        /// <summary>
        /// Registers a kill and returns the reward. Persistent progression is
        /// applied by the caller so the core remains deterministic.
        /// </summary>
        public KillReward RegisterKill()
        {
            if (IsGameOver)
            {
                return default;
            }

            var result = _round.RegisterKill();
            if (!result.Accepted)
            {
                return default;
            }

            Kills++;

            var supplies = GameBalance.SuppliesPerKill;
            var xp = GameBalance.XpPerKill;

            if (result.RoundCleared)
            {
                supplies += GameBalance.SuppliesPerRoundClear;
                xp += GameBalance.XpPerRoundClear;
            }

            return new KillReward(supplies, xp, result.RoundCleared, Round, result.EnemiesRemaining);
        }

        /// <summary>
        /// Advances from a cleared round to the next round. The caller controls
        /// when this happens so the reward flow can run first.
        /// </summary>
        public bool AdvanceRound()
        {
            return !IsGameOver && _round.TryAdvanceToNextRound();
        }

        /// <summary>
        /// Adds run-only ammunition for the current run.
        /// </summary>
        public int AddAmmo(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            Ammo = SafeAdd(Ammo, amount);
            return amount;
        }

        /// <summary>
        /// Adds free, drop-based armor condition. Only equipped armor can be
        /// repaired; with no armor equipped the plates have no effect.
        /// </summary>
        public int AddArmor(int amount)
        {
            if (amount <= 0 || MaxArmor <= 0)
            {
                return 0;
            }

            var applied = Math.Min(amount, Math.Max(0, MaxArmor - Armor));
            Armor = SafeAdd(Armor, applied);
            return applied;
        }

        /// <summary>
        /// Applies damage to the player, absorbing whole armor points first,
        /// and returns true when the run ends.
        /// </summary>
        public bool DamagePlayer(float amount)
        {
            if (IsGameOver)
            {
                return true;
            }

            var remaining = Math.Max(0f, amount);
            if (remaining >= 1f && Armor > 0)
            {
                var absorbed = (int)Math.Min((float)Armor, remaining);
                Armor = Math.Max(0, Armor - absorbed);
                remaining -= absorbed;
            }

            if (remaining > 0f)
            {
                Player.ApplyDamage(remaining);
            }

            if (!Player.IsAlive)
            {
                IsGameOver = true;
                _round.Fail();
            }

            return IsGameOver;
        }

        /// <summary>
        /// Starts a fresh run without changing persistent progression.
        /// </summary>
        public void Restart()
        {
            Player.Reset();
            Kills = 0;
            Ammo = 0;
            Armor = MaxArmor;
            IsGameOver = false;
            _round.Reset(StartingRound);
            _round.Start();
        }

        private static int SafeAdd(int left, int right)
        {
            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
