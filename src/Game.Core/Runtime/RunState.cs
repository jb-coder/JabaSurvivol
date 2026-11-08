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
        private readonly Profile _profile;
        private readonly SynergyState _synergy = new SynergyState();
        private BossState? _boss;
        private ActiveSkillRuntime _activeSkill;
        private float _bastionArmorAccumulator;

        public RunState(
            float maxHealth,
            int startingRound = 1,
            int seed = 0,
            int maxArmor = 0,
            Profile? profile = null,
            float ammoCapacityMultiplier = 1f,
            float reloadSpeedMultiplier = 1f)
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
            _profile = profile ?? new Profile();
            Loadout = new WeaponLoadout(
                _profile,
                ammoCapacityMultiplier,
                reloadSpeedMultiplier);
            var activeNode = _profile.ActiveSkill;
            var activeRank = _profile.GetSkillRank(activeNode);
            _activeSkill = new ActiveSkillRuntime(activeNode, activeRank);
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
        /// Run-only ammunition and weapon state. It is never stored in the
        /// profile and resets on death or restart.
        /// </summary>
        public WeaponLoadout Loadout { get; }

        /// <summary>
        /// Run-only active skill state.
        /// </summary>
        public ActiveSkillRuntime ActiveSkill => _activeSkill;

        /// <summary>
        /// Run-only synergy timers.
        /// </summary>
        public SynergyState Synergy => _synergy;

        public WeaponId CurrentWeapon => Loadout.Current.Id;

        public WeaponRuntimeState CurrentWeaponState => Loadout.Current;

        public int Magazine => Loadout.Current.Magazine;

        public int Reserve => Loadout.Current.Reserve;

        public bool IsReloading => Loadout.Current.IsReloading;

        public float ReloadRemaining => Loadout.Current.ReloadRemaining;

        /// <summary>
        /// Total finite reserve across the loadout. The pistol contributes
        /// nothing because its reserve is infinite.
        /// </summary>
        public int Ammo
        {
            get
            {
                var total = 0;
                for (var i = 0; i < Loadout.States.Count; i++)
                {
                    total = SafeAdd(total, Loadout.States[i].Reserve);
                }

                return total;
            }
        }

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
            _synergy.OnKill(_profile);

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
        /// Adds free, drop-based reserve ammunition to every finite-reserve
        /// weapon. The pistol is unaffected.
        /// </summary>
        public int AddAmmo(int amount)
        {
            return amount <= 0 ? 0 : Loadout.AddReserve(amount);
        }

        /// <summary>
        /// Consumes one round from the current weapon. Returns false when the
        /// magazine is empty or the weapon is reloading. An empty magazine
        /// starts a reload automatically.
        /// </summary>
        public bool TryFireCurrentWeapon()
        {
            if (IsGameOver)
            {
                return false;
            }

            if (Loadout.TryFireCurrent())
            {
                return true;
            }

            Loadout.StartReload();
            return false;
        }

        /// <summary>
        /// Starts a manual reload on the current weapon.
        /// </summary>
        public bool StartReload()
        {
            return !IsGameOver && Loadout.StartReload();
        }

        /// <summary>
        /// Cycles to the next unlocked weapon for the rest of the run.
        /// </summary>
        public bool SwitchNextWeapon()
        {
            return !IsGameOver && Loadout.SwitchNext();
        }

        /// <summary>
        /// Switches to a specific unlocked weapon.
        /// </summary>
        public bool SwitchToWeapon(WeaponId weaponId)
        {
            return !IsGameOver && Loadout.TrySwitchTo(weaponId);
        }

        /// <summary>
        /// Advances reload timers for every weapon in the loadout.
        /// </summary>
        public void TickWeapons(float deltaTime)
        {
            if (!IsGameOver)
            {
                Loadout.Tick(deltaTime);
            }
        }

        /// <summary>
        /// Applies Engineering multipliers to magazine capacity and reload
        /// speed without discarding the current run's ammunition.
        /// </summary>
        public void ApplyAmmoStats(float ammoCapacityMultiplier, float reloadSpeedMultiplier)
        {
            Loadout.UpdateMultipliers(ammoCapacityMultiplier, reloadSpeedMultiplier);
        }

        /// <summary>
        /// Re-reads permanent weapon upgrades for every loadout weapon while
        /// preserving the current run's magazines and reserves.
        /// </summary>
        public void RefreshWeaponStats(Profile profile)
        {
            Loadout.RefreshStats(profile);
        }

        /// <summary>
        /// Deploys the selected active skill when it is ready.
        /// </summary>
        public bool TryActivateActiveSkill()
        {
            return !IsGameOver && _activeSkill.TryActivate();
        }

        /// <summary>
        /// Advances active-skill cooldown and duration timers.
        /// </summary>
        public void TickActiveSkill(float deltaTime)
        {
            if (!IsGameOver)
            {
                _activeSkill.Tick(deltaTime);
            }
        }

        /// <summary>
        /// Re-reads the selected active skill and its rank after allocation or
        /// selection changes.
        /// </summary>
        public void RefreshActiveSkill(Profile profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var node = profile.ActiveSkill;
            var rank = profile.GetSkillRank(node);
            if (_activeSkill.Node == node)
            {
                _activeSkill.UpdateRank(rank);
                return;
            }

            _activeSkill.SetNode(node, rank);
        }

        /// <summary>
        /// Advances synergy timers and applies Bastion armor regeneration.
        /// Returns the whole armor points repaired this step.
        /// </summary>
        public int TickSynergies(float deltaTime)
        {
            if (IsGameOver)
            {
                return 0;
            }

            var regen = _synergy.Tick(deltaTime, _profile);
            if (regen <= 0f)
            {
                return 0;
            }

            _bastionArmorAccumulator += regen;
            var whole = (int)_bastionArmorAccumulator;
            if (whole <= 0)
            {
                return 0;
            }

            _bastionArmorAccumulator -= whole;
            return AddArmor(whole);
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
            if (remaining > 0f)
            {
                _synergy.OnDamageTaken();
            }

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
                _activeSkill.Reset();
                _synergy.Reset();
                _bastionArmorAccumulator = 0f;
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
            Loadout.Reset();
            _activeSkill.Reset();
            _synergy.Reset();
            _bastionArmorAccumulator = 0f;
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
