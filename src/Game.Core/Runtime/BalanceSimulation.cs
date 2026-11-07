using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Inputs for a deterministic, engine-free v0.3.0 balance simulation.
    /// </summary>
    public sealed class BalanceSimulationOptions
    {
        public int Seed { get; set; } = 20261106;

        public int StartingRound { get; set; } = 1;

        public int MaxRound { get; set; } = GameBalance.MaxRound;

        public int RunawaySupplyThreshold { get; set; } = 6000;

        public int UnreachableUpgradeCost { get; set; } = 2000;
    }

    /// <summary>
    /// One deterministic build state used for a round.
    /// </summary>
    public readonly struct BuildSnapshot
    {
        public BuildSnapshot(
            WeaponId weapon,
            bool hasSmg,
            bool hasShotgun,
            bool hasRifle,
            bool hasLmg,
            int damageTier,
            int fireRateTier,
            int magazineTier,
            int reloadTier,
            ArmorTier armor,
            int armorAbsorb,
            int strengthRank,
            int vitalityRank,
            int speedRank,
            int precisionRank,
            int enduranceRank,
            int engineeringRank,
            int turretRank,
            int suppliesSpent,
            float damagePerShot,
            float fireInterval,
            CharacterStats stats)
        {
            Weapon = weapon;
            HasSmg = hasSmg;
            HasShotgun = hasShotgun;
            HasRifle = hasRifle;
            HasLmg = hasLmg;
            DamageTier = damageTier;
            FireRateTier = fireRateTier;
            MagazineTier = magazineTier;
            ReloadTier = reloadTier;
            Armor = armor;
            ArmorAbsorb = armorAbsorb;
            StrengthRank = strengthRank;
            VitalityRank = vitalityRank;
            SpeedRank = speedRank;
            PrecisionRank = precisionRank;
            EnduranceRank = enduranceRank;
            EngineeringRank = engineeringRank;
            TurretRank = turretRank;
            SuppliesSpent = suppliesSpent;
            DamagePerShot = damagePerShot;
            FireInterval = fireInterval;
            Stats = stats;
        }

        public WeaponId Weapon { get; }

        public bool HasSmg { get; }

        public bool HasShotgun { get; }

        public bool HasRifle { get; }

        public bool HasLmg { get; }

        public int DamageTier { get; }

        public int FireRateTier { get; }

        public int MagazineTier { get; }

        public int ReloadTier { get; }

        public ArmorTier Armor { get; }

        public int ArmorAbsorb { get; }

        public int StrengthRank { get; }

        public int VitalityRank { get; }

        public int SpeedRank { get; }

        public int PrecisionRank { get; }

        public int EnduranceRank { get; }

        public int EngineeringRank { get; }

        public int TurretRank { get; }

        public int SuppliesSpent { get; }

        public float DamagePerShot { get; }

        public float FireInterval { get; }

        public CharacterStats Stats { get; }
    }

    /// <summary>
    /// One simulated round with pacing, economy, and build values.
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
            int skillPoints,
            BuildSnapshot build)
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
            Build = build;
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

        public BuildSnapshot Build { get; }
    }

    /// <summary>
    /// Full simulation result with per-round rows and outlier flags.
    /// </summary>
    public sealed class BalanceSimulationReport
    {
        private readonly RoundSimulation[] _rounds;

        public BalanceSimulationReport(
            int seed,
            RoundSimulation[] rounds,
            string[] outliers,
            int totalSuppliesEarned,
            int totalSuppliesSpent)
        {
            Seed = seed;
            _rounds = rounds;
            Outliers = outliers;
            TotalSuppliesEarned = totalSuppliesEarned;
            TotalSuppliesSpent = totalSuppliesSpent;
        }

        public int Seed { get; }

        public IReadOnlyList<RoundSimulation> Rounds => _rounds;

        public IReadOnlyList<string> Outliers { get; }

        public bool HasOutliers => Outliers.Count > 0;

        public int TotalSupplies => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].CumulativeSupplies;

        public int TotalSuppliesEarned { get; }

        public int TotalSuppliesSpent { get; }

        public int TotalXp => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].CumulativeXp;

        public int FinalLevel => _rounds.Length == 0 ? 1 : _rounds[_rounds.Length - 1].Level;

        public int FinalSkillPoints => _rounds.Length == 0 ? 0 : _rounds[_rounds.Length - 1].SkillPoints;

        public BuildSnapshot FinalBuild => _rounds.Length == 0 ? default : _rounds[_rounds.Length - 1].Build;
    }

    /// <summary>
    /// Headless v0.3.0 simulation. It uses the same weapon, upgrade, armor, and
    /// skill rules as the game and never references Unity.
    /// </summary>
    public static class BalanceSimulation
    {
        private static readonly WeaponUpgradeStat[] UpgradePriority =
        {
            WeaponUpgradeStat.Damage,
            WeaponUpgradeStat.FireRate,
            WeaponUpgradeStat.MagazineSize,
            WeaponUpgradeStat.ReloadSpeed
        };

        private static readonly SkillNode[] SkillPriority =
        {
            SkillNode.Strength,
            SkillNode.Vitality,
            SkillNode.Endurance,
            SkillNode.Speed,
            SkillNode.Precision,
            SkillNode.Engineering,
            SkillNode.Turret
        };

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

            var profile = new Profile();
            var run = new RunState(GameBalance.PlayerMaxHealth, options.StartingRound, options.Seed);
            var rounds = new List<RoundSimulation>();
            var outliers = new List<string>();
            var cumulativeXp = 0;
            var totalSuppliesEarned = 0;
            var suppliesSpent = 0;

            for (var round = options.StartingRound; round <= options.MaxRound; round++)
            {
                ProgressBuild(profile, ref suppliesSpent);
                var build = CreateSnapshot(profile, suppliesSpent);
                var definition = run.RoundModel.Definition;
                var budget = definition.SpawnBudget;
                var shotsToKill = Math.Max(
                    1,
                    (int)Math.Ceiling(definition.ZombieMaxHealth / build.DamagePerShot));
                var killSeconds = shotsToKill * build.FireInterval;
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
                totalSuppliesEarned += suppliesEarned;

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
                    profile.SkillPoints,
                    build));

                if (shotsToKill <= 1)
                {
                    outliers.Add($"Round {round} is trivial: one shot kills a zombie.");
                }

                if (round < options.MaxRound && !run.AdvanceRound())
                {
                    throw new InvalidOperationException($"Round {round} did not advance during the simulation.");
                }
            }

            if (rounds.Count > 0)
            {
                var last = rounds[rounds.Count - 1];

                if (totalSuppliesEarned > options.RunawaySupplyThreshold)
                {
                    outliers.Add(
                        $"Supplies are runaway: {totalSuppliesEarned} earned exceeds {options.RunawaySupplyThreshold}.");
                }

                if (totalSuppliesEarned < options.UnreachableUpgradeCost)
                {
                    outliers.Add(
                        $"Upgrades are unreachable: {totalSuppliesEarned} earned is below {options.UnreachableUpgradeCost}.");
                }

                if (last.Level >= Profile.MaxLevel && options.MaxRound < Profile.MaxLevel)
                {
                    outliers.Add($"Level cap reached before round {options.MaxRound}.");
                }

                if (last.Build.Weapon == WeaponId.Pistol)
                {
                    outliers.Add($"No weapon unlock was reached by round {options.MaxRound}.");
                }

                if (last.Build.DamageTier <= 0)
                {
                    outliers.Add($"No weapon damage upgrade was reached by round {options.MaxRound}.");
                }

                if (last.Build.Armor == ArmorTier.None)
                {
                    outliers.Add($"No armor unlock was reached by round {options.MaxRound}.");
                }

                AddCapOutliers(rounds, outliers);
            }

            return new BalanceSimulationReport(
                options.Seed,
                rounds.ToArray(),
                outliers.ToArray(),
                totalSuppliesEarned,
                suppliesSpent);
        }

        private static void ProgressBuild(Profile profile, ref int suppliesSpent)
        {
            TryBuyArmor(profile, ArmorTier.Light, ref suppliesSpent);
            EquipBestArmor(profile);

            // The default simulation follows a rifle-first build. Save for the
            // rifle before spending on upgrades so early supplies are not sunk
            // into a weapon that is replaced in the mid-game.
            if (!profile.HasWeapon(WeaponId.Rifle))
            {
                var rifleCost = WeaponCatalog.Get(WeaponId.Rifle).UnlockCost;
                if (profile.Supplies >= rifleCost)
                {
                    TryBuyWeapon(profile, WeaponId.Rifle, ref suppliesSpent);
                }
                else
                {
                    AllocateSkills(profile);
                    return;
                }
            }

            TryBuyWeapon(profile, WeaponId.Lmg, ref suppliesSpent);

            // Save for medium armor next so the report exercises an armor tier
            // beyond the free light armor.
            if (!profile.HasArmor(ArmorTier.Medium))
            {
                var mediumCost = ArmorCatalog.Get(ArmorTier.Medium).UnlockCost;
                if (profile.Supplies >= mediumCost)
                {
                    TryBuyArmor(profile, ArmorTier.Medium, ref suppliesSpent);
                }
                else
                {
                    AllocateSkills(profile);
                    return;
                }
            }

            TryBuyArmor(profile, ArmorTier.Heavy, ref suppliesSpent);
            EquipBestArmor(profile);

            for (var i = 0; i < UpgradePriority.Length; i++)
            {
                BuyWeaponUpgrades(profile, UpgradePriority[i], ref suppliesSpent);
            }

            BuyArmorUpgrades(profile, ref suppliesSpent);
            AllocateSkills(profile);
        }

        private static void EquipBestArmor(Profile profile)
        {
            if (profile.HasArmor(ArmorTier.Heavy))
            {
                ArmorService.EquipArmor(profile, ArmorTier.Heavy);
            }
            else if (profile.HasArmor(ArmorTier.Medium))
            {
                ArmorService.EquipArmor(profile, ArmorTier.Medium);
            }
            else if (profile.HasArmor(ArmorTier.Light))
            {
                ArmorService.EquipArmor(profile, ArmorTier.Light);
            }
        }

        private static void AllocateSkills(Profile profile)
        {
            for (var i = 0; i < SkillPriority.Length; i++)
            {
                while (profile.AllocateSkill(SkillPriority[i]))
                {
                }
            }
        }

        private static void TryBuyArmor(Profile profile, ArmorTier tier, ref int suppliesSpent)
        {
            if (profile.HasArmor(tier))
            {
                return;
            }

            var cost = ArmorCatalog.Get(tier).UnlockCost;
            if (profile.Supplies < cost)
            {
                return;
            }

            var before = profile.Supplies;
            if (ArmorService.PurchaseArmor(profile, tier) == ArmorPurchaseResult.Purchased)
            {
                suppliesSpent += before - profile.Supplies;
            }
        }

        private static void TryBuyWeapon(Profile profile, WeaponId weaponId, ref int suppliesSpent)
        {
            if (profile.HasWeapon(weaponId))
            {
                return;
            }

            var cost = WeaponCatalog.Get(weaponId).UnlockCost;
            if (profile.Supplies < cost)
            {
                return;
            }

            var before = profile.Supplies;
            if (ArmoryService.PurchaseWeapon(profile, weaponId) == ArmoryPurchaseResult.Purchased)
            {
                suppliesSpent += before - profile.Supplies;
                ArmoryService.EquipWeapon(profile, weaponId);
            }
        }

        private static void BuyWeaponUpgrades(Profile profile, WeaponUpgradeStat stat, ref int suppliesSpent)
        {
            var track = WeaponUpgradeCatalog.Get(profile.EquippedWeapon, stat);
            while (profile.GetWeaponUpgradeTier(profile.EquippedWeapon, stat) < track.MaxTier)
            {
                var cost = WeaponUpgradeService.GetNextCost(profile, profile.EquippedWeapon, stat);
                if (cost <= 0 || profile.Supplies < cost)
                {
                    return;
                }

                var before = profile.Supplies;
                if (WeaponUpgradeService.TryPurchase(profile, profile.EquippedWeapon, stat) !=
                    WeaponUpgradePurchaseResult.Purchased)
                {
                    return;
                }

                suppliesSpent += before - profile.Supplies;
            }
        }

        private static void BuyArmorUpgrades(Profile profile, ref int suppliesSpent)
        {
            if (profile.EquippedArmor == ArmorTier.None)
            {
                return;
            }

            var definition = ArmorCatalog.Get(profile.EquippedArmor);
            while (profile.GetArmorUpgradeTier(profile.EquippedArmor) < definition.MaxUpgradeTier)
            {
                var cost = ArmorService.GetNextUpgradeCost(profile, profile.EquippedArmor);
                if (cost <= 0 || profile.Supplies < cost)
                {
                    return;
                }

                var before = profile.Supplies;
                if (ArmorService.TryPurchaseUpgrade(profile, profile.EquippedArmor) !=
                    ArmorUpgradePurchaseResult.Purchased)
                {
                    return;
                }

                suppliesSpent += before - profile.Supplies;
            }
        }

        private static BuildSnapshot CreateSnapshot(Profile profile, int suppliesSpent)
        {
            var weapon = WeaponCatalog.Get(profile.EquippedWeapon);
            var weaponStats = WeaponUpgradeService.GetEffectiveStats(profile, profile.EquippedWeapon);
            var armorStats = ArmorService.GetEffectiveStats(profile, profile.EquippedArmor);
            var stats = StatAggregation.Compute(profile, weaponStats, armorStats);
            var damagePerShot = stats.EffectiveWeaponDamage * weapon.PelletCount;

            return new BuildSnapshot(
                profile.EquippedWeapon,
                profile.HasWeapon(WeaponId.Smg),
                profile.HasWeapon(WeaponId.Shotgun),
                profile.HasWeapon(WeaponId.Rifle),
                profile.HasWeapon(WeaponId.Lmg),
                profile.GetWeaponUpgradeTier(profile.EquippedWeapon, WeaponUpgradeStat.Damage),
                profile.GetWeaponUpgradeTier(profile.EquippedWeapon, WeaponUpgradeStat.FireRate),
                profile.GetWeaponUpgradeTier(profile.EquippedWeapon, WeaponUpgradeStat.MagazineSize),
                profile.GetWeaponUpgradeTier(profile.EquippedWeapon, WeaponUpgradeStat.ReloadSpeed),
                profile.EquippedArmor,
                stats.Armor,
                profile.GetSkillRank(SkillNode.Strength),
                profile.GetSkillRank(SkillNode.Vitality),
                profile.GetSkillRank(SkillNode.Speed),
                profile.GetSkillRank(SkillNode.Precision),
                profile.GetSkillRank(SkillNode.Endurance),
                profile.GetSkillRank(SkillNode.Engineering),
                profile.GetSkillRank(SkillNode.Turret),
                suppliesSpent,
                damagePerShot,
                weaponStats.FireInterval,
                stats);
        }

        private static void AddCapOutliers(List<RoundSimulation> rounds, List<string> outliers)
        {
            foreach (var round in rounds)
            {
                var stats = round.Build.Stats;
                if (stats.WeaponDamageMultiplier > 1f + StatCaps.DamageBonus + 0.0001f)
                {
                    outliers.Add($"Round {round.Round} exceeds the damage cap.");
                }

                if (stats.MoveSpeed > GameBalance.PlayerMoveSpeed * (1f + StatCaps.MoveSpeedBonus) + 0.0001f)
                {
                    outliers.Add($"Round {round.Round} exceeds the movement speed cap.");
                }

                if (stats.Armor > StatCaps.Armor)
                {
                    outliers.Add($"Round {round.Round} exceeds the armor cap.");
                }

                if (stats.CriticalChance > StatCaps.CriticalChance + 0.0001f)
                {
                    outliers.Add($"Round {round.Round} exceeds the critical chance cap.");
                }

                if (stats.HeadshotBonus > StatCaps.HeadshotBonus + 0.0001f)
                {
                    outliers.Add($"Round {round.Round} exceeds the headshot cap.");
                }
            }
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
