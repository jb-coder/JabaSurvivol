using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Inputs for a deterministic, engine-free v0.4.0 balance simulation.
    /// </summary>
    public sealed class BalanceSimulationOptions
    {
        public int Seed { get; set; } = 20261106;

        public int StartingRound { get; set; } = 1;

        public int MaxRound { get; set; } = GameBalance.MaxRound;

        public int RunawaySupplyThreshold { get; set; } = BalanceTargets.MaximumSuppliesEarned;

        public float BossMinimumSeconds { get; set; } = BalanceTargets.MinimumBossSeconds;

        public float BossMaximumSeconds { get; set; } = BalanceTargets.MaximumBossSeconds;

        public float AmmoPressureThreshold { get; set; } = BalanceTargets.MaximumAmmoPressure;

        /// <summary>
        /// Optional starting reserve bonus used by tests to exercise ammo
        /// crate effects without changing the default report.
        /// </summary>
        public int StartingAmmoCrateBonus { get; set; }

        /// <summary>
        /// Optional profile used to exercise a specific ranked build. The
        /// simulation mutates this profile as it progresses.
        /// </summary>
        public Profile? StartingProfile { get; set; }
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
            int armorTierAbsorb,
            int totalArmor,
            int strengthRank,
            int vitalityRank,
            int speedRank,
            int precisionRank,
            int enduranceRank,
            int engineeringRank,
            int turretRank,
            int shockwaveRank,
            int dashRank,
            int suppliesSpent,
            float damagePerShot,
            float fireInterval,
            CharacterStats stats,
            CharacterStats rawStats)
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
            ArmorTierAbsorb = armorTierAbsorb;
            TotalArmor = totalArmor;
            StrengthRank = strengthRank;
            VitalityRank = vitalityRank;
            SpeedRank = speedRank;
            PrecisionRank = precisionRank;
            EnduranceRank = enduranceRank;
            EngineeringRank = engineeringRank;
            TurretRank = turretRank;
            ShockwaveRank = shockwaveRank;
            DashRank = dashRank;
            SuppliesSpent = suppliesSpent;
            DamagePerShot = damagePerShot;
            FireInterval = fireInterval;
            Stats = stats;
            RawStats = rawStats;
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

        public int ArmorTierAbsorb { get; }

        public int TotalArmor { get; }

        public int StrengthRank { get; }

        public int VitalityRank { get; }

        public int SpeedRank { get; }

        public int PrecisionRank { get; }

        public int EnduranceRank { get; }

        public int EngineeringRank { get; }

        public int TurretRank { get; }

        public int ShockwaveRank { get; }

        public int DashRank { get; }

        public int SuppliesSpent { get; }

        public float DamagePerShot { get; }

        public float FireInterval { get; }

        public CharacterStats Stats { get; }

        public CharacterStats RawStats { get; }
    }

    /// <summary>
    /// Enemy mix and boss time-to-kill for one round.
    /// </summary>
    public readonly struct RoundContent
    {
        public RoundContent(int walkers, int runners, int brutes, float bossSeconds)
        {
            Walkers = walkers;
            Runners = runners;
            Brutes = brutes;
            BossSeconds = bossSeconds;
        }

        public int Walkers { get; }

        public int Runners { get; }

        public int Brutes { get; }

        public float BossSeconds { get; }
    }

    /// <summary>
    /// Ammunition pressure for one round.
    /// </summary>
    public readonly struct RoundAmmo
    {
        public RoundAmmo(
            int shots,
            int reloads,
            float reloadSeconds,
            int switches,
            float pressure,
            bool starved)
        {
            Shots = shots;
            Reloads = reloads;
            ReloadSeconds = reloadSeconds;
            Switches = switches;
            Pressure = pressure;
            Starved = starved;
        }

        public int Shots { get; }

        public int Reloads { get; }

        public float ReloadSeconds { get; }

        public int Switches { get; }

        public float Pressure { get; }

        public bool Starved { get; }
    }

    /// <summary>
    /// Active-skill and synergy uptime for one round.
    /// </summary>
    public readonly struct RoundSkills
    {
        public RoundSkills(
            SkillNode selected,
            float turretUptime,
            float dashUptime,
            float shockwaveUptime,
            float berserkerUptime,
            float ghostUptime,
            float bastionUptime)
        {
            Selected = selected;
            TurretUptime = turretUptime;
            DashUptime = dashUptime;
            ShockwaveUptime = shockwaveUptime;
            BerserkerUptime = berserkerUptime;
            GhostUptime = ghostUptime;
            BastionUptime = bastionUptime;
        }

        public SkillNode Selected { get; }

        public float TurretUptime { get; }

        public float DashUptime { get; }

        public float ShockwaveUptime { get; }

        public float BerserkerUptime { get; }

        public float GhostUptime { get; }

        public float BastionUptime { get; }
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
            int totalShots,
            float killSeconds,
            float roundSeconds,
            int suppliesEarned,
            int xpEarned,
            RoundRewardOption reward,
            int cumulativeSupplies,
            int cumulativeXp,
            int level,
            int skillPoints,
            BuildSnapshot build,
            RoundContent content,
            RoundAmmo ammo,
            RoundSkills skills)
        {
            Round = round;
            SpawnBudget = spawnBudget;
            ZombieHealth = zombieHealth;
            ZombieSpeed = zombieSpeed;
            ZombieDamage = zombieDamage;
            IsElite = isElite;
            IsBoss = isBoss;
            ShotsToKill = shotsToKill;
            TotalShots = totalShots;
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
            Content = content;
            Ammo = ammo;
            Skills = skills;
        }

        public int Round { get; }

        public int SpawnBudget { get; }

        public float ZombieHealth { get; }

        public float ZombieSpeed { get; }

        public float ZombieDamage { get; }

        public bool IsElite { get; }

        public bool IsBoss { get; }

        public int ShotsToKill { get; }

        public int TotalShots { get; }

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

        public RoundContent Content { get; }

        public RoundAmmo Ammo { get; }

        public RoundSkills Skills { get; }
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
    /// Full-run result for one preferred weapon with every weapon unlocked.
    /// The scenario isolates weapon pacing, ammo pressure, and progression
    /// without changing the default rifle-first report.
    /// </summary>
    public sealed class WeaponScenario
    {
        public WeaponScenario(WeaponId weapon, BalanceSimulationReport report)
        {
            Weapon = weapon;
            Report = report;
        }

        public WeaponId Weapon { get; }

        public BalanceSimulationReport Report { get; }
    }

    /// <summary>
    /// Headless v0.4.0 simulation. It uses the same weapon, upgrade, armor, and
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
            SkillNode.Turret,
            SkillNode.Shockwave,
            SkillNode.Dash
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

            var profile = options.StartingProfile ?? new Profile();
            var run = new RunState(GameBalance.PlayerMaxHealth, options.StartingRound, options.Seed, 0, profile);
            var rounds = new List<RoundSimulation>();
            var outliers = new List<string>();
            var cumulativeXp = 0;
            var totalSuppliesEarned = 0;
            var suppliesSpent = 0;
            var ammoCrateBonus = options.StartingAmmoCrateBonus;

            for (var round = options.StartingRound; round <= options.MaxRound; round++)
            {
                ProgressBuild(profile, round, ref suppliesSpent);
                var build = CreateSnapshot(profile, suppliesSpent);
                run.ApplyMaxStats(build.Stats.MaxHealth, build.Stats.Armor);
                var definition = run.RoundModel.Definition;
                var plan = run.RoundModel.SpawnPlan;
                var walkers = 0;
                var runners = 0;
                var brutes = 0;
                var maxHealth = 0f;
                var maxSpeed = 0f;
                var maxDamage = 0f;
                var totalShots = 0;
                for (var i = 0; i < plan.Count; i++)
                {
                    var archetype = plan[i];
                    switch (archetype)
                    {
                        case ZombieArchetype.Runner:
                            runners++;
                            break;
                        case ZombieArchetype.Brute:
                            brutes++;
                            break;
                        default:
                            walkers++;
                            break;
                    }

                    var enemy = definition.GetEncounter(archetype);
                    maxHealth = Math.Max(maxHealth, enemy.MaxHealth);
                    maxSpeed = Math.Max(maxSpeed, enemy.MoveSpeed);
                    maxDamage = Math.Max(maxDamage, enemy.ContactDamage);
                    totalShots += Math.Max(
                        1,
                        (int)Math.Ceiling(enemy.MaxHealth / build.DamagePerShot));
                }

                var encounter = definition.GetEncounter(ZombieArchetype.Walker);
                var budget = plan.Count;
                var shotsToKill = Math.Max(
                    1,
                    (int)Math.Ceiling(maxHealth / build.DamagePerShot));
                var killSeconds = totalShots * build.FireInterval;

                var ammo = SimulateAmmo(profile, build, totalShots, ammoCrateBonus);
                var baseRoundSeconds =
                    killSeconds + (budget * GameBalance.ZombieRespawnDelay) + ammo.ReloadSeconds;

                var turretStats = SkillCatalog.Get(SkillNode.Turret)
                    .GetActivationStats(profile.GetSkillRank(SkillNode.Turret));
                var dashStats = SkillCatalog.Get(SkillNode.Dash)
                    .GetActivationStats(profile.GetSkillRank(SkillNode.Dash));
                var shockwaveStats = SkillCatalog.Get(SkillNode.Shockwave)
                    .GetActivationStats(profile.GetSkillRank(SkillNode.Shockwave));
                var selected = profile.ActiveSkill;
                var turretUptime = Uptime(
                    profile.GetSkillRank(SkillNode.Turret),
                    turretStats.TurretDuration,
                    turretStats.CooldownSeconds);
                var dashUptime = Uptime(
                    profile.GetSkillRank(SkillNode.Dash),
                    GameBalance.DashInvulnerabilitySeconds,
                    dashStats.CooldownSeconds);
                var shockwaveUptime = Uptime(
                    profile.GetSkillRank(SkillNode.Shockwave),
                    0.1f,
                    shockwaveStats.CooldownSeconds);
                var weaponDps = build.FireInterval <= 0f
                    ? 0f
                    : (build.DamagePerShot / build.FireInterval) * (1f - ammo.Pressure);
                var skillDps = selected switch
                {
                    SkillNode.Turret when profile.GetSkillRank(SkillNode.Turret) > 0 =>
                        turretStats.TurretFireInterval <= 0f
                            ? 0f
                            : (turretStats.TurretDamage / turretStats.TurretFireInterval) * turretUptime,
                    SkillNode.Turret => 0f,
                    SkillNode.Dash => 0f,
                    SkillNode.Shockwave when profile.GetSkillRank(SkillNode.Shockwave) > 0 =>
                        (baseRoundSeconds / Math.Max(0.1f, shockwaveStats.CooldownSeconds)) *
                        shockwaveStats.AreaDamage / Math.Max(1f, baseRoundSeconds),
                    _ => 0f
                };
                var bossSeconds = BossCatalog.BossDefinition.MaxHealth /
                                  Math.Max(1f, weaponDps + skillDps);
                var roundSeconds = baseRoundSeconds + (definition.IsBoss ? bossSeconds : 0f);
                var berserkerUptime = SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker)
                    ? 0.35f
                    : 0f;
                var ghostUptime = SynergyCatalog.IsUnlocked(profile, SynergyId.Ghost)
                    ? Math.Min(1f, (budget * 2f) / Math.Max(1f, roundSeconds))
                    : 0f;
                var bastionUptime = SynergyCatalog.IsUnlocked(profile, SynergyId.Bastion)
                    ? 0.5f
                    : 0f;

                var suppliesEarned = 0;
                var xpEarned = 0;

                for (var i = 0; i < budget; i++)
                {
                    var archetype = i < plan.Count ? plan[i] : ZombieArchetype.Walker;
                    var kill = run.RegisterKill(archetype);
                    suppliesEarned += kill.Supplies;
                    xpEarned += kill.Xp;
                    profile.AddSupplies(kill.Supplies);
                    profile.AddXp(kill.Xp);
                }

                if (definition.IsBoss && run.TrySpawnBoss())
                {
                    if (run.ApplyBossDamage(run.Boss!.MaxHealth, out var bossReward))
                    {
                        suppliesEarned += bossReward.Supplies;
                        xpEarned += bossReward.Xp;
                        profile.AddSupplies(bossReward.Supplies);
                        profile.AddXp(bossReward.Xp);
                    }
                }

                var reward = SelectReward(RoundRewardModel.GenerateOptions(round, options.Seed), round);
                var rewardResult = RoundRewardModel.Apply(reward, profile, run);
                suppliesEarned += rewardResult.Supplies;
                xpEarned += rewardResult.Xp;
                if (reward.Type == RoundRewardType.AmmoCrate)
                {
                    ammoCrateBonus += reward.Amount;
                }

                cumulativeXp += xpEarned;
                totalSuppliesEarned += suppliesEarned;

                rounds.Add(new RoundSimulation(
                    round,
                    budget,
                    maxHealth,
                    maxSpeed,
                    maxDamage,
                    definition.IsElite,
                    definition.IsBoss,
                    shotsToKill,
                    totalShots,
                    killSeconds,
                    roundSeconds,
                    suppliesEarned,
                    xpEarned,
                    reward,
                    profile.Supplies,
                    cumulativeXp,
                    profile.Level,
                    profile.SkillPoints,
                    build,
                    new RoundContent(walkers, runners, brutes, bossSeconds),
                    new RoundAmmo(
                        ammo.Shots,
                        ammo.Reloads,
                        ammo.ReloadSeconds,
                        ammo.Switches,
                        ammo.Pressure,
                        ammo.Starved),
                    new RoundSkills(
                        selected,
                        turretUptime,
                        dashUptime,
                        shockwaveUptime,
                        berserkerUptime,
                        ghostUptime,
                        bastionUptime)));

                if (definition.IsElite && shotsToKill <= 10)
                {
                    outliers.Add(FormattableString.Invariant(
                        $"Round {round} elite encounter is trivial: {shotsToKill} shots per enemy."));
                }

                if (definition.IsBoss && bossSeconds < options.BossMinimumSeconds)
                {
                    outliers.Add(FormattableString.Invariant(
                        $"Boss is too easy: {bossSeconds:0.0} seconds is below {options.BossMinimumSeconds:0.0}."));
                }

                if (definition.IsBoss && bossSeconds > options.BossMaximumSeconds)
                {
                    outliers.Add(FormattableString.Invariant(
                        $"Boss is too hard: {bossSeconds:0.0} seconds is above {options.BossMaximumSeconds:0.0}."));
                }

                if (ammo.Pressure > options.AmmoPressureThreshold || ammo.Starved)
                {
                    outliers.Add(FormattableString.Invariant(
                        $"Round {round} is ammo-starved: {ammo.Pressure:0.00} reload pressure exceeds {options.AmmoPressureThreshold:0.00}."));
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

                var maxUpgradeCost = GetMaxUpgradeCost();
                if (totalSuppliesEarned < maxUpgradeCost)
                {
                    outliers.Add(
                        $"Upgrades are unreachable: {totalSuppliesEarned} earned is below the {maxUpgradeCost} maximum upgrade cost.");
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

                if (!SynergyCatalog.IsUnlocked(profile, SynergyId.Berserker) &&
                    !SynergyCatalog.IsUnlocked(profile, SynergyId.Ghost) &&
                    !SynergyCatalog.IsUnlocked(profile, SynergyId.Bastion))
                {
                    outliers.Add($"No synergy was reachable by round {options.MaxRound}.");
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

        /// <summary>
        /// Runs one full progression scenario per weapon with every weapon
        /// unlocked so the equipped weapon is never replaced by the default
        /// rifle-first strategy.
        /// </summary>
        public static IReadOnlyList<WeaponScenario> RunWeaponScenarios(int seed = 20261106)
        {
            var scenarios = new List<WeaponScenario>(WeaponCatalog.All.Count);
            foreach (var definition in WeaponCatalog.All)
            {
                var profile = new Profile();
                foreach (var weapon in WeaponCatalog.All)
                {
                    profile.UnlockWeapon(weapon.Id);
                }

                profile.EquipWeapon(definition.Id);
                var report = Run(new BalanceSimulationOptions
                {
                    Seed = seed,
                    StartingProfile = profile
                });
                scenarios.Add(new WeaponScenario(definition.Id, report));
            }

            return scenarios;
        }

        private static float Uptime(int rank, float duration, float cooldown)
        {
            if (rank <= 0 || duration <= 0f || cooldown <= 0f)
            {
                return 0f;
            }

            return Math.Min(1f, duration / cooldown);
        }

        private static AmmoSimulation SimulateAmmo(
            Profile profile,
            BuildSnapshot build,
            int totalShots,
            int ammoCrateBonus)
        {
            var loadout = new WeaponLoadout(
                profile,
                build.Stats.AmmoCapacityMultiplier,
                build.Stats.ReloadSpeedMultiplier);
            if (ammoCrateBonus > 0)
            {
                loadout.AddReserve(ammoCrateBonus);
            }

            var shots = 0;
            var reloads = 0;
            var switches = 0;
            var reloadSeconds = 0f;
            var starved = false;

            while (shots < totalShots)
            {
                if (loadout.Current.IsReloading)
                {
                    var duration = loadout.Current.ReloadDuration;
                    loadout.Tick(duration);
                    reloads++;
                    reloadSeconds += duration;
                    continue;
                }

                if (loadout.TryFireCurrent())
                {
                    shots++;
                    continue;
                }

                if (loadout.StartReload())
                {
                    continue;
                }

                if (loadout.SwitchNext())
                {
                    switches++;
                    starved = true;
                    continue;
                }

                break;
            }

            var roundSeconds = build.FireInterval <= 0f
                ? 1f
                : (shots * build.FireInterval) + 1f;
            var pressure = Math.Clamp(reloadSeconds / roundSeconds, 0f, 1f);
            return new AmmoSimulation(shots, reloads, reloadSeconds, switches, pressure, starved);
        }

        private readonly struct AmmoSimulation
        {
            public AmmoSimulation(
                int shots,
                int reloads,
                float reloadSeconds,
                int switches,
                float pressure,
                bool starved)
            {
                Shots = shots;
                Reloads = reloads;
                ReloadSeconds = reloadSeconds;
                Switches = switches;
                Pressure = pressure;
                Starved = starved;
            }

            public int Shots { get; }

            public int Reloads { get; }

            public float ReloadSeconds { get; }

            public int Switches { get; }

            public float Pressure { get; }

            public bool Starved { get; }
        }

        private static void ProgressBuild(Profile profile, int round, ref int suppliesSpent)
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

            TryBuyWeapon(profile, WeaponId.Rifle, ref suppliesSpent);

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

            BuyWeaponUpgrades(profile, round, ref suppliesSpent);
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

        private static void BuyWeaponUpgrades(Profile profile, int round, ref int suppliesSpent)
        {
            // Rotate the starting stat each round so scarce supplies still
            // exercise every documented upgrade path instead of sinking all of
            // them into the first track.
            var start = round % UpgradePriority.Length;
            var bought = true;
            while (bought)
            {
                bought = false;
                for (var i = 0; i < UpgradePriority.Length; i++)
                {
                    var stat = UpgradePriority[(start + i) % UpgradePriority.Length];
                    if (TryBuyNextWeaponUpgrade(profile, stat, ref suppliesSpent))
                    {
                        bought = true;
                    }
                }
            }
        }

        private static bool TryBuyNextWeaponUpgrade(
            Profile profile,
            WeaponUpgradeStat stat,
            ref int suppliesSpent)
        {
            var track = WeaponUpgradeCatalog.Get(profile.EquippedWeapon, stat);
            if (profile.GetWeaponUpgradeTier(profile.EquippedWeapon, stat) >= track.MaxTier)
            {
                return false;
            }

            var cost = WeaponUpgradeService.GetNextCost(profile, profile.EquippedWeapon, stat);
            if (cost <= 0 || profile.Supplies < cost)
            {
                return false;
            }

            var before = profile.Supplies;
            if (WeaponUpgradeService.TryPurchase(profile, profile.EquippedWeapon, stat) !=
                WeaponUpgradePurchaseResult.Purchased)
            {
                return false;
            }

            suppliesSpent += before - profile.Supplies;
            return true;
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
            var rawStats = StatAggregation.ComputeRaw(profile, weaponStats, armorStats);
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
                armorStats.AbsorbAmount,
                stats.Armor,
                profile.GetSkillRank(SkillNode.Strength),
                profile.GetSkillRank(SkillNode.Vitality),
                profile.GetSkillRank(SkillNode.Speed),
                profile.GetSkillRank(SkillNode.Precision),
                profile.GetSkillRank(SkillNode.Endurance),
                profile.GetSkillRank(SkillNode.Engineering),
                profile.GetSkillRank(SkillNode.Turret),
                profile.GetSkillRank(SkillNode.Shockwave),
                profile.GetSkillRank(SkillNode.Dash),
                suppliesSpent,
                damagePerShot,
                weaponStats.FireInterval,
                stats,
                rawStats);
        }

        private static int GetMaxUpgradeCost()
        {
            var maxCost = 0;
            foreach (var weapon in WeaponCatalog.All)
            {
                var tracks = WeaponUpgradeCatalog.GetTracks(weapon.Id);
                foreach (var track in tracks)
                {
                    maxCost = Math.Max(maxCost, track.GetCost(track.MaxTier));
                }
            }

            foreach (var armor in ArmorCatalog.All)
            {
                if (armor.MaxUpgradeTier > 0)
                {
                    maxCost = Math.Max(maxCost, armor.GetUpgradeCost(armor.MaxUpgradeTier));
                }
            }

            return maxCost;
        }

        private static void AddCapOutliers(List<RoundSimulation> rounds, List<string> outliers)
        {
            foreach (var round in rounds)
            {
                if (HasCapViolation(round.Build.RawStats, out var reason))
                {
                    outliers.Add($"Round {round.Round} exceeds the {reason} cap.");
                }
            }
        }

        /// <summary>
        /// Checks raw, uncapped character stats against the documented caps.
        /// </summary>
        public static bool HasCapViolation(CharacterStats stats, out string reason)
        {
            if (stats.WeaponDamageMultiplier > 1f + StatCaps.DamageBonus + 0.0001f)
            {
                reason = "damage";
                return true;
            }

            if (stats.MoveSpeed > GameBalance.PlayerMoveSpeed * (1f + StatCaps.MoveSpeedBonus) + 0.0001f)
            {
                reason = "movement speed";
                return true;
            }

            if (stats.Armor > StatCaps.Armor)
            {
                reason = "armor";
                return true;
            }

            if (stats.CriticalChance > StatCaps.CriticalChance + 0.0001f)
            {
                reason = "critical chance";
                return true;
            }

            if (stats.HeadshotBonus > StatCaps.HeadshotBonus + 0.0001f)
            {
                reason = "headshot";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private static RoundRewardOption SelectReward(RoundRewardOption[] options, int round)
        {
            var desired = (round % 4) switch
            {
                0 => RoundRewardType.AmmoCrate,
                1 => RoundRewardType.Supplies,
                2 => RoundRewardType.Xp,
                _ => RoundRewardType.ArmorPlate
            };

            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].Type == desired)
                {
                    return options[i];
                }
            }

            return options.Length == 0 ? default : options[0];
        }
    }
}
