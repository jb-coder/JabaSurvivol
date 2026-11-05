using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Reward granted when a zombie dies.
    /// </summary>
    public readonly struct KillReward
    {
        public KillReward(int supplies, int xp, bool completedRound, int round)
        {
            Supplies = supplies;
            Xp = xp;
            CompletedRound = completedRound;
            Round = round;
        }

        public int Supplies { get; }

        public int Xp { get; }

        public bool CompletedRound { get; }

        public int Round { get; }
    }

    /// <summary>
    /// State for a single run. Death resets this object only; persistent
    /// progression lives in <see cref="Profile"/>.
    /// </summary>
    public sealed class RunState
    {
        public RunState(float maxHealth, int startingRound = 1)
        {
            if (startingRound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingRound));
            }

            Player = new PlayerState(maxHealth);
            StartingRound = startingRound;
            Round = startingRound;
        }

        public PlayerState Player { get; }

        public int StartingRound { get; }

        public int Round { get; private set; }

        public int Kills { get; private set; }

        public int KillsThisRound { get; private set; }

        public bool IsGameOver { get; private set; }

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

            Kills++;
            KillsThisRound++;

            var completedRound = KillsThisRound >= GameBalance.KillsPerRound;
            var supplies = GameBalance.SuppliesPerKill;
            var xp = GameBalance.XpPerKill;

            if (completedRound)
            {
                supplies += GameBalance.SuppliesPerRoundClear;
                xp += GameBalance.XpPerRoundClear;
                Round++;
                KillsThisRound = 0;
            }

            return new KillReward(supplies, xp, completedRound, Round);
        }

        /// <summary>
        /// Applies damage to the player and returns true when the run ends.
        /// </summary>
        public bool DamagePlayer(float amount)
        {
            if (IsGameOver)
            {
                return true;
            }

            Player.ApplyDamage(amount);
            if (!Player.IsAlive)
            {
                IsGameOver = true;
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
            KillsThisRound = 0;
            Round = StartingRound;
            IsGameOver = false;
        }
    }
}
