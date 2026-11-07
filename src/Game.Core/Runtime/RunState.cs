using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Reward granted when a zombie dies.
    /// </summary>
    public readonly struct KillReward
    {
        public KillReward(
            int supplies,
            int xp,
            bool completedRound,
            int round,
            int enemiesRemaining,
            bool isBoss = false)
        {
            Supplies = supplies;
            Xp = xp;
            CompletedRound = completedRound;
            Round = round;
            EnemiesRemaining = enemiesRemaining;
            IsBoss = isBoss;
        }

        public int Supplies { get; }

        public int Xp { get; }

        public bool CompletedRound { get; }

        public int Round { get; }

        public int EnemiesRemaining { get; }

        public bool IsBoss { get; }
    }

    /// <summary>
    /// State for a single run. Death resets this object only; persistent
    /// progression lives in <see cref="Profile"/>.
    /// </summary>
    public sealed class RunState
    {
        private readonly RoundState _round;
        private BossState? _boss;

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
            _round = new RoundState(startingRound, seed);
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

        /// <summary>
        /// Archetype of the most recent kill. Used by visuals and tests.
        /// </summary>
        public ZombieArchetype LastKillArchetype { get; private set; } = ZombieArchetype.Walker;

        public bool IsGameOver { get; private set; }

        /// <summary>
        /// True while the round 10 boss is alive.
        /// </summary>
        public bool BossActive => _boss != null && _boss.IsAlive;

        /// <summary>
        /// True after all regular enemies in the boss round are dead and
        /// before the boss has spawned.
        /// </summary>
        public bool BossSpawnPending => _round.BossSpawnPending;

        public bool BossDefeated => _round.BossDefeated;

        public BossState? Boss => _boss;

        /// <summary>
        /// True after the boss is defeated in the current run.
        /// </summary>
        public bool IsVictory { get; private set; }

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
        public int MaxArmor { get; private set; }

        /// <summary>
        /// Registers a kill and returns the reward. Persistent progression is
        /// applied by the caller so the core remains deterministic.
        /// </summary>
        public KillReward RegisterKill(ZombieArchetype archetype = ZombieArchetype.Walker)
        {
            if (IsGameOver)
            {
                return default;
            }

            var encounter = _round.Definition.GetEncounter(archetype);
            var result = _round.RegisterKill();
            if (!result.Accepted)
            {
                return default;
            }

            Kills++;
            LastKillArchetype = archetype;

            var supplies = encounter.Supplies;
            var xp = encounter.Xp;

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
        /// Spawns the round 10 boss after the regular enemies are cleared.
        /// Returns false when the boss is not pending or already active.
        /// </summary>
        public bool TrySpawnBoss()
        {
            if (IsGameOver || IsVictory || _boss != null || !_round.BossSpawnPending)
            {
                return false;
            }

            _boss = new BossState(BossCatalog.BossDefinition);
            _round.MarkBossSpawned();
            return true;
        }

        /// <summary>
        /// Applies damage to the active boss and returns true when this hit
        /// killed it. The reward is returned through <paramref name="reward"/>.
        /// </summary>
        public bool ApplyBossDamage(float amount, out KillReward reward)
        {
            reward = default;
            if (IsGameOver || _boss == null || !_boss.IsAlive)
            {
                return false;
            }

            if (!_boss.ApplyDamage(amount))
            {
                return false;
            }

            reward = RegisterBossKill();
            return true;
        }

        /// <summary>
        /// Registers the boss kill after its health reaches zero. The reward is
        /// granted once per run and clears the round.
        /// </summary>
        public KillReward RegisterBossKill()
        {
            if (IsGameOver || _boss == null || _boss.IsAlive || IsVictory)
            {
                return default;
            }

            var definition = _boss.Definition;
            _boss = null;
            IsVictory = true;
            Kills++;
            _round.MarkBossDefeated();
            return new KillReward(
                definition.Supplies,
                definition.Xp,
                completedRound: true,
                Round,
                enemiesRemaining: 0,
                isBoss: true);
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
            IsVictory = false;
            _boss = null;
            _round.Reset(StartingRound);
            _round.Start();
        }

        /// <summary>
        /// Applies new maximum health and armor to the active run. Health and
        /// armor increases are added; decreases clamp the current values.
        /// </summary>
        public void ApplyMaxStats(float maxHealth, int maxArmor)
        {
            if (maxArmor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArmor));
            }

            var previousMaxArmor = MaxArmor;
            Player.ApplyMaxHealth(maxHealth, healIncrease: true);
            MaxArmor = maxArmor;
            if (MaxArmor > previousMaxArmor)
            {
                Armor = SafeAdd(Armor, MaxArmor - previousMaxArmor);
            }

            Armor = Math.Min(Armor, MaxArmor);
        }

        private static int SafeAdd(int left, int right)
        {
            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
