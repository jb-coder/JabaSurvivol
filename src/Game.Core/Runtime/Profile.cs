using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Persistent player progression. Death never removes any field of this type.
    /// The type is serializable by JSON-based save stores.
    /// </summary>
    [Serializable]
    public sealed class Profile
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxLevel = 30;
        public const int MaxSkillPoints = 35;

        public int SchemaVersion = CurrentSchemaVersion;
        public int Supplies;
        public int Xp;
        public int Level = 1;
        public int SkillPoints;
        public int HighestRound = 1;
        public int TotalKills;
        public int TotalRuns;
        public int BestKills;

        /// <summary>
        /// XP needed to advance from <paramref name="level"/> to the next level.
        /// Returns zero at the level cap.
        /// </summary>
        public static int XpToNext(int level)
        {
            if (level >= MaxLevel)
            {
                return 0;
            }

            if (level < 1)
            {
                return XpToNext(1);
            }

            if (level <= 5)
            {
                return 100 * level;
            }

            if (level <= 10)
            {
                return 500 + ((level - 5) * 140);
            }

            if (level <= 20)
            {
                return 1200 + ((level - 10) * 280);
            }

            return 4000 + ((level - 20) * 600);
        }

        public int AddSupplies(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            Supplies = SafeAdd(Supplies, amount);
            return amount;
        }

        public bool SpendSupplies(int amount)
        {
            if (amount <= 0 || Supplies < amount)
            {
                return false;
            }

            Supplies -= amount;
            return true;
        }

        /// <summary>
        /// Adds XP and returns the number of levels gained.
        /// </summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel)
            {
                return 0;
            }

            Xp = SafeAdd(Xp, amount);

            var levelsGained = 0;
            while (Level < MaxLevel)
            {
                var required = XpToNext(Level);
                if (required <= 0 || Xp < required)
                {
                    break;
                }

                Xp -= required;
                Level++;
                levelsGained++;
                SkillPoints++;

                if (Level % 5 == 0)
                {
                    SkillPoints++;
                }
            }

            if (Level >= MaxLevel)
            {
                Xp = 0;
            }

            ClampSkillPoints();
            return levelsGained;
        }

        /// <summary>
        /// Records the end of a run without removing any progression.
        /// </summary>
        public void RecordRunEnd(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            TotalRuns++;
            TotalKills = SafeAdd(TotalKills, run.Kills);
            HighestRound = Math.Max(HighestRound, run.Round);
            BestKills = Math.Max(BestKills, run.Kills);
        }

        /// <summary>
        /// Normalizes data loaded from disk. Additive schema changes keep the
        /// current version so older saves remain playable.
        /// </summary>
        public void Validate()
        {
            if (SchemaVersion <= 0 || SchemaVersion > CurrentSchemaVersion)
            {
                SchemaVersion = CurrentSchemaVersion;
            }

            Supplies = Math.Max(0, Supplies);
            Xp = Math.Max(0, Xp);
            Level = Math.Clamp(Level, 1, MaxLevel);

            if (Level >= MaxLevel)
            {
                Xp = 0;
            }

            HighestRound = Math.Max(1, HighestRound);
            TotalKills = Math.Max(0, TotalKills);
            TotalRuns = Math.Max(0, TotalRuns);
            BestKills = Math.Max(0, BestKills);
            ClampSkillPoints();
        }

        private void ClampSkillPoints()
        {
            SkillPoints = Math.Clamp(SkillPoints, 0, MaxSkillPoints);
        }

        private static int SafeAdd(int left, int right)
        {
            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
