using System;
using JabaSurvivol.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JabaSurvivol.Gameplay
{
    public enum GameSessionState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Owns the walking-skeleton run: player, one zombie, bullets, HUD, and
    /// persistent profile writes.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        private ProfileStore _store;
        private Profile _profile;
        private RunState _run;
        private IGameInput _input;
        private Transform _worldRoot;
        private GameUi _ui;
        private GameObject _playerObject;
        private Transform _aimPivot;
        private ZombieActor _zombieActor;
        private ZombieEncounterStats _zombieStats;
        private ZombieState _zombie;
        private ZombieActorPool _zombieActors;
        private DeathEffectPool _deathEffects;
        private BulletPool _bullets;
        private GameSessionState _state;
        private Vector2 _aimDirection = Vector2.right;
        private float _fireCooldown;
        private float _contactCooldown;
        private float _respawnCooldown;
        private bool _zombieActive;
        private bool _roundClearPending;
        private RoundRewardOption[] _roundRewardOptions;
        private int _runSuppliesEarned;
        private int _runXpEarned;
        private WeaponStats _weapon;
        private CharacterStats _stats;
        private GameSessionState _armoryReturnState;
        private int _spreadSequence;
        private float _levelUpRemaining;

        public Profile Profile => _profile;

        public RunState Run => _run;

        public GameSessionState State => _state;

        public CharacterStats Stats => _stats;

        public WeaponId EquippedWeapon => _profile?.EquippedWeapon ?? WeaponId.Pistol;

        public WeaponStats EquippedWeaponStats => _weapon;

        public ArmorTier EquippedArmor => _profile?.EquippedArmor ?? ArmorTier.None;

        public ArmorStats EquippedArmorStats => _profile == null
            ? default
            : ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor);

        public bool ArmoryVisible => _ui != null && _ui.ArmoryVisible;

        public bool CharacterSheetVisible => _ui != null && _ui.CharacterSheetVisible;

        public bool LevelUpVisible => _ui != null && _ui.LevelUpVisible;

        public bool HudVisible => _ui != null && _ui.HudVisible;

        public float LastBulletDamageForTests { get; private set; }

        public Vector2 LastShotDirectionForTests { get; private set; }

        public bool PlayerActive => _playerObject != null && _playerObject.activeSelf;

        public bool ZombieActive => _zombieActive;

        public ZombieArchetype CurrentZombieArchetype => _zombieStats.Archetype;

        public float CurrentZombieMaxHealth => _zombieStats.MaxHealth;

        public bool EliteRound => _run?.RoundModel.Definition.IsElite ?? false;

        public int ZombiePoolCount => _zombieActors?.Count ?? 0;

        public bool RoundModifierVisible => _ui != null && _ui.RoundModifierVisible;

        public int ActiveBullets => _bullets?.ActiveCount ?? 0;

        public int EnemiesRemaining => _run?.EnemiesRemaining ?? 0;

        public int SpawnBudget => _run?.SpawnBudget ?? 0;

        public RoundPhase RoundPhase => _run?.RoundModel.Phase ?? RoundPhase.NotStarted;

        public bool RoundClearPending => _roundClearPending;

        public bool RoundEndVisible => _ui != null && _ui.RoundEndVisible;

        public int PendingRewardCount => _roundRewardOptions?.Length ?? 0;

        public RoundRewardOption PendingRewardAt(int index)
        {
            if (_roundRewardOptions == null || index < 0 || index >= _roundRewardOptions.Length)
            {
                return default;
            }

            return _roundRewardOptions[index];
        }

        public string SavePath => _store?.FilePath;

        public Vector2 PlayerPosition => _playerObject != null
            ? (Vector2)_playerObject.transform.position
            : Vector2.zero;

        public Vector2 ZombiePosition => _zombieActor != null
            ? (Vector2)_zombieActor.GameObject.transform.position
            : Vector2.zero;

        /// <summary>
        /// Test hook that replaces touch input with a scripted source.
        /// </summary>
        public void SetInputForTests(IGameInput input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public void Initialize(ProfileStore store, IGameInput input, Transform worldRoot)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _worldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));

            _profile = _store.Load();
            _profile.Validate();
            _weapon = WeaponUpgradeService.GetEffectiveStats(_profile, _profile.EquippedWeapon);
            _stats = StatAggregation.Compute(
                _profile,
                _weapon,
                ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor));

            CreateActors();

            _ui = new GameUi(
                transform,
                StartRun,
                RestartRun,
                ReturnToMenu,
                PauseGame,
                ResumeGame,
                index => SelectRoundReward(index),
                ShowArmory,
                HandleArmoryAction,
                HandleArmoryUpgrade,
                HandleArmorAction,
                HandleArmorUpgrade,
                ShowCharacterSheet,
                HandleAllocateSkill,
                HandleRespecSkills,
                CloseCharacterSheet,
                CloseArmory);

            _state = GameSessionState.MainMenu;
            _ui.ShowMainMenu(_profile);
            _playerObject.SetActive(false);
            _zombieActors.ReturnAll();
        }

        private void Update()
        {
            if (_input == null || _playerObject == null)
            {
                return;
            }

            if (_levelUpRemaining > 0f)
            {
                _levelUpRemaining -= Time.deltaTime;
                if (_levelUpRemaining <= 0f)
                {
                    _ui.HideLevelUp();
                }
            }

            var playerPosition = (Vector2)_playerObject.transform.position;
            var frame = _input.Read(playerPosition);

            if (_ui.CharacterSheetVisible)
            {
                if (frame.PausePressed)
                {
                    CloseCharacterSheet();
                }

                _ui.SetSticks(default, default);
                return;
            }

            if (_ui.ArmoryVisible)
            {
                if (frame.PausePressed)
                {
                    CloseArmory();
                }

                _ui.SetSticks(default, default);
                return;
            }

            if (frame.PausePressed && !_roundClearPending)
            {
                if (_state == GameSessionState.Playing)
                {
                    PauseGame();
                }
                else if (_state == GameSessionState.Paused)
                {
                    ResumeGame();
                }
            }

            if (_state == GameSessionState.Playing)
            {
                if (_roundClearPending)
                {
                    HandleRewardHotkeys();
                    _ui.SetSticks(default, default);
                    return;
                }

                UpdatePlaying(frame, Time.deltaTime);
                _ui.SetSticks(frame.MoveStick, frame.AimStick);
            }
            else
            {
                _ui.SetSticks(default, default);
            }
        }

        private void HandleRewardHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                SelectRoundReward(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                SelectRoundReward(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                SelectRoundReward(2);
            }
        }

        private void UpdatePlaying(InputFrame frame, float deltaTime)
        {
            if (_run == null)
            {
                return;
            }

            UpdatePlayer(frame, deltaTime);
            if (_state != GameSessionState.Playing)
            {
                return;
            }

            _deathEffects.Update(deltaTime);

            var targetPosition = _zombieActor != null
                ? (Vector2)_zombieActor.GameObject.transform.position
                : Vector2.zero;
            var targetRadius = _zombieActor != null ? _zombieStats.Radius : 0f;
            var damage = _bullets.UpdateAndCheck(
                deltaTime,
                _zombieActive,
                targetPosition,
                targetRadius);

            if (damage > 0f)
            {
                OnBulletHitZombie(damage);
            }

            if (_state != GameSessionState.Playing)
            {
                return;
            }

            UpdateZombie(deltaTime);
            if (_state != GameSessionState.Playing)
            {
                return;
            }

            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
        }

        private void UpdatePlayer(InputFrame frame, float deltaTime)
        {
            if (frame.Move.sqrMagnitude > 0.0001f)
            {
                var current = (Vector2)_playerObject.transform.position;
                var next = current + (frame.Move * (_stats.MoveSpeed * deltaTime));
                CombatMath.ClampToArena(
                    next.x,
                    next.y,
                    GameBalance.ArenaHalfWidth,
                    GameBalance.ArenaHalfHeight,
                    GameBalance.PlayerRadius,
                    out var clampedX,
                    out var clampedY);
                _playerObject.transform.position = new Vector3(clampedX, clampedY, 0f);
            }

            if (frame.Aim.sqrMagnitude > 0.0001f)
            {
                _aimDirection = frame.Aim.normalized;
            }

            _aimPivot.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg);

            _fireCooldown = Mathf.Max(0f, _fireCooldown - deltaTime);
            if (frame.FireHeld && _fireCooldown <= 0f)
            {
                Fire();
            }
        }

        private void Fire()
        {
            _fireCooldown = _weapon.FireInterval;
            var damage = _stats.EffectiveWeaponDamage;
            LastBulletDamageForTests = damage;

            var origin = (Vector2)_playerObject.transform.position + (_aimDirection * 0.55f);
            var lifetime = _weapon.Range / _weapon.ProjectileSpeed;

            if (_weapon.PelletCount <= 1)
            {
                var direction = Rotate(_aimDirection, NextSpreadAngle(_weapon.SpreadDegrees));
                LastShotDirectionForTests = direction;
                _bullets.Spawn(origin, direction, _weapon.ProjectileSpeed, lifetime, damage);
                return;
            }

            for (var i = 0; i < _weapon.PelletCount; i++)
            {
                var step = (float)i / (_weapon.PelletCount - 1);
                var angle = (-_weapon.SpreadDegrees * 0.5f) + (_weapon.SpreadDegrees * step);
                var direction = Rotate(_aimDirection, angle);
                if (i == 0)
                {
                    LastShotDirectionForTests = direction;
                }

                _bullets.Spawn(origin, direction, _weapon.ProjectileSpeed, lifetime, damage);
            }
        }

        private float NextSpreadAngle(float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
            {
                return 0f;
            }

            _spreadSequence = (_spreadSequence + 1) % 97;
            var normalized = _spreadSequence / 97f;
            return (normalized - 0.5f) * spreadDegrees;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(
                (direction.x * cos) - (direction.y * sin),
                (direction.x * sin) + (direction.y * cos));
        }

        /// <summary>
        /// Test hook that fires the equipped weapon without waiting for the
        /// fire cooldown.
        /// </summary>
        public void FireForTests()
        {
            if (_state == GameSessionState.Playing && _playerObject != null && _playerObject.activeSelf)
            {
                Fire();
            }
        }

        private void UpdateZombie(float deltaTime)
        {
            if (!_zombieActive)
            {
                if (_run.IsGameOver || !_run.RoundModel.IsActive || _roundClearPending)
                {
                    return;
                }

                _respawnCooldown -= deltaTime;
                if (_respawnCooldown <= 0f)
                {
                    SpawnZombie();
                }

                return;
            }

            var zombieObject = _zombieActor.GameObject;
            var zombiePosition = (Vector2)zombieObject.transform.position;
            var playerPosition = (Vector2)_playerObject.transform.position;
            var delta = playerPosition - zombiePosition;

            if (delta.sqrMagnitude > 0.0001f)
            {
                var direction = delta.normalized;
                zombiePosition += direction * (_zombieStats.MoveSpeed * deltaTime);
                CombatMath.ClampToArena(
                    zombiePosition.x,
                    zombiePosition.y,
                    GameBalance.ArenaHalfWidth,
                    GameBalance.ArenaHalfHeight,
                    _zombieStats.Radius,
                    out var clampedX,
                    out var clampedY);
                zombiePosition = new Vector2(clampedX, clampedY);
                zombieObject.transform.position = zombiePosition;
                zombieObject.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f);
            }

            _contactCooldown = Mathf.Max(0f, _contactCooldown - deltaTime);
            if (_contactCooldown <= 0f && CombatMath.CirclesOverlap(
                    zombiePosition.x,
                    zombiePosition.y,
                    _zombieStats.Radius,
                    playerPosition.x,
                    playerPosition.y,
                    GameBalance.PlayerRadius))
            {
                _contactCooldown = _zombieStats.ContactInterval;
                ApplyDamage(_zombieStats.ContactDamage);
            }
        }

        public void StartRun()
        {
            StartRunInternal(1);
        }

        /// <summary>
        /// Starts a run at a specific round. Used by automated scenario tests.
        /// </summary>
        public void StartRunForTests(int startingRound)
        {
            StartRunInternal(startingRound);
        }

        private void StartRunInternal(int startingRound)
        {
            var armor = ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor);
            _weapon = WeaponUpgradeService.GetEffectiveStats(_profile, _profile.EquippedWeapon);
            _stats = StatAggregation.Compute(_profile, _weapon, armor);
            _run = new RunState(_stats.MaxHealth, startingRound, Environment.TickCount, _stats.Armor);
            _runSuppliesEarned = 0;
            _runXpEarned = 0;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _spreadSequence = 0;
            _levelUpRemaining = 0f;
            _state = GameSessionState.Playing;

            _playerObject.SetActive(true);
            _playerObject.transform.position = Vector3.zero;
            _aimDirection = Vector2.right;
            _aimPivot.localRotation = Quaternion.identity;
            _fireCooldown = 0f;
            _contactCooldown = 0f;
            _bullets.Clear();
            _deathEffects.Clear();
            _zombieActors.ReturnAll();
            _zombieActive = false;

            SpawnZombie();
            _ui.ShowHud();
            _ui.SetRoundModifier(_run.RoundModel.Definition.IsElite);
            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
            _ui.SetSticks(default, default);
            _store.Save(_profile);
        }

        public void RestartRun()
        {
            StartRun();
        }

        public void PauseGame()
        {
            if (_state != GameSessionState.Playing)
            {
                return;
            }

            _state = GameSessionState.Paused;
            _ui.ShowPaused(true);
            _ui.SetSticks(default, default);
            _store.Save(_profile);
        }

        public void ResumeGame()
        {
            if (_state != GameSessionState.Paused)
            {
                return;
            }

            _state = GameSessionState.Playing;
            _ui.ShowPaused(false);
        }

        public void ReturnToMenu()
        {
            if (_state == GameSessionState.Playing || _state == GameSessionState.Paused)
            {
                if (_run != null)
                {
                    _profile.RecordRunEnd(_run);
                }

                _store.Save(_profile);
            }

            _state = GameSessionState.MainMenu;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _playerObject.SetActive(false);
            _zombieActive = false;
            _zombieActors.ReturnAll();
            _deathEffects.Clear();
            _bullets.Clear();
            _ui.ShowMainMenu(_profile);
        }

        /// <summary>
        /// Opens the armory from the main menu or the pause menu.
        /// </summary>
        public void ShowArmory()
        {
            if (_ui == null || (_state != GameSessionState.MainMenu && _state != GameSessionState.Paused))
            {
                return;
            }

            _armoryReturnState = _state;
            _ui.ShowArmory(_profile);
            _ui.SetSticks(default, default);
        }

        public void CloseArmory()
        {
            if (_ui == null || !_ui.ArmoryVisible)
            {
                return;
            }

            _ui.HideArmory();
            if (_armoryReturnState == GameSessionState.Paused && _state == GameSessionState.Paused)
            {
                _ui.ShowHud();
                _ui.ShowPaused(true);
                return;
            }

            _state = GameSessionState.MainMenu;
            _ui.ShowMainMenu(_profile);
        }

        public void ShowCharacterSheet()
        {
            if (_ui == null)
            {
                return;
            }

            if (_state != GameSessionState.MainMenu &&
                _state != GameSessionState.Paused &&
                !_roundClearPending)
            {
                return;
            }

            RefreshStats();
            var canAllocate = (_ui.ArmoryVisible) ||
                              _state == GameSessionState.Paused ||
                              _roundClearPending;
            var canRespec = _state == GameSessionState.MainMenu;
            _ui.ShowCharacterSheet(
                _profile,
                canAllocate,
                canRespec,
                _weapon,
                ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor));
        }

        public void CloseCharacterSheet()
        {
            _ui?.HideCharacterSheet();
        }

        private void HandleAllocateSkill(SkillNode node)
        {
            if (!CanAllocateSkillsNow())
            {
                _ui.SetCharacterSheetStatus(Localization.Get("character.allocation.blocked"));
                return;
            }

            if (TryAllocateSkill(node))
            {
                RefreshCharacterSheetUi();
                _ui.SetCharacterSheetStatus(
                    Localization.Format(
                        "character.status.allocated",
                        Localization.SkillName(node),
                        _profile.GetSkillRank(node)));
                return;
            }

            var maxRank = SkillCatalog.Get(node).MaxRanks;
            _ui.SetCharacterSheetStatus(
                Localization.Get(
                    _profile.GetSkillRank(node) >= maxRank
                        ? "character.status.maxrank"
                        : "character.status.nopoints"));
        }

        private void HandleRespecSkills()
        {
            var refunded = RespecSkills();
            if (refunded > 0)
            {
                RefreshCharacterSheetUi();
                _ui.SetCharacterSheetStatus(Localization.Format("character.respec.done", refunded));
                return;
            }

            _ui.SetCharacterSheetStatus(
                Localization.Get(
                    _profile.GetSpentSkillPoints() == 0
                        ? "character.respec.empty"
                        : "character.respec.blocked"));
        }

        private void RefreshCharacterSheetUi()
        {
            if (_ui != null && _ui.CharacterSheetVisible)
            {
                var canAllocate = (_ui.ArmoryVisible) ||
                                  _state == GameSessionState.Paused ||
                                  _roundClearPending;
                var canRespec = _state == GameSessionState.MainMenu;
                _ui.RefreshCharacterSheet(
                    _profile,
                    canAllocate,
                    canRespec,
                    _weapon,
                    ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor));
            }
        }

        private void ShowLevelUpNotification()
        {
            _ui.ShowLevelUp(_profile.Level);
            _levelUpRemaining = GameBalance.LevelUpNotificationSeconds;
        }

        private void HandleArmoryAction(WeaponId weaponId)
        {
            if (_profile.HasWeapon(weaponId))
            {
                if (TryEquipWeapon(weaponId))
                {
                    _ui.SetArmoryStatus(
                        Localization.Format("armory.equipped.status", Localization.WeaponName(weaponId)));
                }
            }
            else
            {
                var result = TryPurchaseWeapon(weaponId);
                if (result == ArmoryPurchaseResult.Purchased)
                {
                    TryEquipWeapon(weaponId);
                    _ui.SetArmoryStatus(
                        Localization.Format("armory.purchased", Localization.WeaponName(weaponId)));
                }
                else if (result == ArmoryPurchaseResult.InsufficientSupplies)
                {
                    _ui.SetArmoryStatus(Localization.Get("armory.insufficient"));
                }
                else if (result == ArmoryPurchaseResult.AlreadyOwned && TryEquipWeapon(weaponId))
                {
                    _ui.SetArmoryStatus(
                        Localization.Format("armory.equipped.status", Localization.WeaponName(weaponId)));
                }
            }

            _ui.RefreshArmory(_profile);
        }

        private void HandleArmoryUpgrade(WeaponUpgradeStat stat)
        {
            var result = TryPurchaseWeaponUpgrade(stat);
            if (result == WeaponUpgradePurchaseResult.Purchased)
            {
                _ui.SetArmoryStatus(
                    Localization.Format(
                        "armory.upgrade.purchased",
                        Localization.WeaponUpgradeStatName(stat),
                        _profile.GetWeaponUpgradeTier(_profile.EquippedWeapon, stat)));
            }
            else if (result == WeaponUpgradePurchaseResult.InsufficientSupplies)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.insufficient"));
            }
            else if (result == WeaponUpgradePurchaseResult.MaxTier)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.upgrade.maxed"));
            }

            _ui.RefreshArmory(_profile);
        }

        /// <summary>
        /// Purchases the next permanent tier for the equipped weapon and
        /// refreshes the effective combat stats.
        /// </summary>
        public WeaponUpgradePurchaseResult TryPurchaseWeaponUpgrade(WeaponUpgradeStat stat)
        {
            var result = WeaponUpgradeService.TryPurchase(_profile, _profile.EquippedWeapon, stat);
            if (result == WeaponUpgradePurchaseResult.Purchased)
            {
                RefreshStats();
                _store.Save(_profile);
            }

            return result;
        }

        private void RefreshStats()
        {
            _weapon = WeaponUpgradeService.GetEffectiveStats(_profile, _profile.EquippedWeapon);
            _stats = StatAggregation.Compute(
                _profile,
                _weapon,
                ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor));
        }

        private bool CanAllocateSkillsNow()
        {
            return (_ui != null && _ui.ArmoryVisible) ||
                   _state == GameSessionState.Paused ||
                   _roundClearPending;
        }

        private bool CanRespecSkillsNow()
        {
            return _state == GameSessionState.MainMenu;
        }

        private void ApplyStatsToRun()
        {
            if (_run == null || _run.IsGameOver)
            {
                return;
            }

            _run.ApplyMaxStats(_stats.MaxHealth, _stats.Armor);
            _ui.UpdateHud(
                _run.Player,
                _run.Armor,
                _run.MaxArmor,
                _run.Round,
                _run.EnemiesRemaining,
                _run.Kills,
                _profile);
        }

        /// <summary>
        /// Allocates one skill point. Allocation is only allowed while the
        /// armory or character sheet is open, while paused, or on the
        /// round-end screen.
        /// </summary>
        public bool TryAllocateSkill(SkillNode node)
        {
            if (!CanAllocateSkillsNow() || !_profile.AllocateSkill(node))
            {
                return false;
            }

            RefreshStats();
            ApplyStatsToRun();
            _store.Save(_profile);
            return true;
        }

        /// <summary>
        /// Performs the free respec. Returns the number of refunded points.
        /// </summary>
        public int RespecSkills()
        {
            if (!CanRespecSkillsNow())
            {
                return 0;
            }

            var refunded = _profile.RespecSkills();
            if (refunded == 0)
            {
                return 0;
            }

            RefreshStats();
            ApplyStatsToRun();
            _store.Save(_profile);
            return refunded;
        }

        private void HandleArmorAction(ArmorTier tier)
        {
            if (_profile.HasArmor(tier))
            {
                if (TryEquipArmor(tier))
                {
                    _ui.SetArmoryStatus(
                        Localization.Format("armory.armor.equipped.status", Localization.ArmorName(tier)));
                }
            }
            else
            {
                var result = TryPurchaseArmor(tier);
                if (result == ArmorPurchaseResult.Purchased)
                {
                    TryEquipArmor(tier);
                    _ui.SetArmoryStatus(
                        Localization.Format("armory.armor.purchased", Localization.ArmorName(tier)));
                }
                else if (result == ArmorPurchaseResult.InsufficientSupplies)
                {
                    _ui.SetArmoryStatus(Localization.Get("armory.insufficient"));
                }
            }

            _ui.RefreshArmory(_profile);
        }

        private void HandleArmorUpgrade()
        {
            var result = TryPurchaseArmorUpgrade(_profile.EquippedArmor);
            if (result == ArmorUpgradePurchaseResult.Purchased)
            {
                _ui.SetArmoryStatus(
                    Localization.Format(
                        "armory.armor.upgraded",
                        _profile.GetArmorUpgradeTier(_profile.EquippedArmor)));
            }
            else if (result == ArmorUpgradePurchaseResult.InsufficientSupplies)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.insufficient"));
            }
            else if (result == ArmorUpgradePurchaseResult.MaxTier)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.armor.maxed"));
            }

            _ui.RefreshArmory(_profile);
        }

        public ArmorPurchaseResult TryPurchaseArmor(ArmorTier tier)
        {
            var result = ArmorService.PurchaseArmor(_profile, tier);
            if (result == ArmorPurchaseResult.Purchased)
            {
                _store.Save(_profile);
            }

            return result;
        }

        public bool TryEquipArmor(ArmorTier tier)
        {
            if (!ArmorService.EquipArmor(_profile, tier))
            {
                return false;
            }

            RefreshStats();
            _store.Save(_profile);
            return true;
        }

        public ArmorUpgradePurchaseResult TryPurchaseArmorUpgrade(ArmorTier tier)
        {
            var result = ArmorService.TryPurchaseUpgrade(_profile, tier);
            if (result == ArmorUpgradePurchaseResult.Purchased)
            {
                RefreshStats();
                _store.Save(_profile);
            }

            return result;
        }

        /// <summary>
        /// Purchases a weapon and persists the unlock. Returns the atomic
        /// purchase result so callers can surface insufficient supplies.
        /// </summary>
        public ArmoryPurchaseResult TryPurchaseWeapon(WeaponId weaponId)
        {
            var result = ArmoryService.PurchaseWeapon(_profile, weaponId);
            if (result == ArmoryPurchaseResult.Purchased)
            {
                _store.Save(_profile);
            }

            return result;
        }

        /// <summary>
        /// Equips an owned weapon and persists the choice.
        /// </summary>
        public bool TryEquipWeapon(WeaponId weaponId)
        {
            if (!ArmoryService.EquipWeapon(_profile, weaponId))
            {
                return false;
            }

            RefreshStats();
            _store.Save(_profile);
            return true;
        }

        public void ApplyDamage(float amount)
        {
            if (_state != GameSessionState.Playing || _run == null)
            {
                return;
            }

            if (_run.DamagePlayer(amount))
            {
                EndRun();
            }
            else
            {
                _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
            }
        }

        /// <summary>
        /// Test hook that applies damage to the active zombie.
        /// </summary>
        public void DamageZombieForTests(float amount)
        {
            if (!_zombieActive || _zombie == null)
            {
                return;
            }

            if (_zombie.ApplyDamage(amount))
            {
                OnZombieKilled();
            }
        }

        public void SaveNow()
        {
            if (_store != null && _profile != null)
            {
                _store.Save(_profile);
            }
        }

        private void OnBulletHitZombie(float damage)
        {
            if (!_zombieActive || _zombie == null)
            {
                return;
            }

            if (_zombie.ApplyDamage(damage))
            {
                OnZombieKilled();
            }
        }

        private void OnZombieKilled()
        {
            if (_state != GameSessionState.Playing)
            {
                return;
            }

            var archetype = _zombieStats.Archetype;
            var wasElite = _zombieStats.IsElite;
            var deathPosition = (Vector2)_zombieActor.GameObject.transform.position;
            var deathScale = _zombieStats.VisualScale;

            _zombieActive = false;
            _zombieActors.Return(_zombieActor);
            _zombieActor = null;
            _deathEffects.Spawn(
                deathPosition,
                ZombieActorPool.ColorFor(archetype, wasElite),
                deathScale);
            _respawnCooldown = GameBalance.ZombieRespawnDelay;

            var reward = _run.RegisterKill(archetype);
            _profile.AddSupplies(reward.Supplies);
            var levelsGained = _profile.AddXp(reward.Xp);
            if (levelsGained > 0)
            {
                ShowLevelUpNotification();
            }

            _runSuppliesEarned += reward.Supplies;
            _runXpEarned += reward.Xp;
            _store.Save(_profile);

            if (reward.CompletedRound)
            {
                BeginRoundRewardFlow(reward);
                return;
            }

            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
        }

        /// <summary>
        /// Round clear hook. Generates the deterministic reward choices and
        /// pauses the run until the player picks one.
        /// </summary>
        private void BeginRoundRewardFlow(KillReward reward)
        {
            _roundRewardOptions = RoundRewardModel.GenerateOptions(_run.Round, _run.Seed);
            _roundClearPending = true;
            _ui.ShowRoundEnd(
                reward.Round,
                _run.Kills,
                _runSuppliesEarned,
                _runXpEarned,
                _roundRewardOptions);
            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
        }

        /// <summary>
        /// Applies the selected reward, saves persistent progress, and starts
        /// the next round.
        /// </summary>
        public bool SelectRoundReward(int index)
        {
            if (!_roundClearPending || _state != GameSessionState.Playing ||
                _roundRewardOptions == null || index < 0 || index >= _roundRewardOptions.Length)
            {
                return false;
            }

            var previousLevel = _profile.Level;
            var result = RoundRewardModel.Apply(_roundRewardOptions[index], _profile, _run);
            if (_profile.Level > previousLevel)
            {
                ShowLevelUpNotification();
            }

            _runSuppliesEarned += result.Supplies;
            _runXpEarned += result.Xp;
            _store.Save(_profile);
            CompleteRoundClear();
            return true;
        }

        /// <summary>
        /// Finishes the round-clear flow and starts the next round when one is
        /// available.
        /// </summary>
        public void ContinueAfterRoundClear()
        {
            if (!_roundClearPending || _state != GameSessionState.Playing)
            {
                return;
            }

            if (_roundRewardOptions == null || _roundRewardOptions.Length == 0)
            {
                CompleteRoundClear();
                return;
            }

            SelectRoundReward(0);
        }

        private void CompleteRoundClear()
        {
            _roundClearPending = false;
            _roundRewardOptions = null;
            _ui.HideRoundEnd();

            if (_run.AdvanceRound())
            {
                SpawnZombie();
            }

            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
        }

        /// <summary>
        /// Test hook that spawns the next round enemy immediately.
        /// </summary>
        public void SpawnZombieForTests()
        {
            if (_state == GameSessionState.Playing && !_zombieActive)
            {
                SpawnZombie();
            }
        }

        private void EndRun()
        {
            if (_state == GameSessionState.GameOver)
            {
                return;
            }

            _state = GameSessionState.GameOver;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _ui.HideRoundEnd();
            var wasElite = _zombieStats.IsElite;
            var position = _zombieActor != null ? (Vector2)_zombieActor.GameObject.transform.position : Vector2.zero;
            var color = _zombieActor != null
                ? ZombieActorPool.ColorFor(_zombieStats.Archetype, wasElite)
                : new Color(0.86f, 0.25f, 0.3f);
            _zombieActive = false;
            if (_zombieActor != null)
            {
                _zombieActors.Return(_zombieActor);
                _zombieActor = null;
            }

            _deathEffects.Spawn(position, color, 1f);
            _playerObject.SetActive(false);

            _profile.RecordRunEnd(_run);
            _store.Save(_profile);
            _ui.ShowGameOver(_run.Round, _run.Kills, _runSuppliesEarned);
        }

        private bool SpawnZombie()
        {
            if (!_run.RoundModel.TryMarkSpawned(out var archetype))
            {
                return false;
            }

            _zombieStats = _run.RoundModel.Definition.GetEncounter(archetype);
            _zombie = new ZombieState(_zombieStats.MaxHealth);
            _zombieActor = _zombieActors.Rent(
                archetype,
                _zombieStats.VisualScale,
                _zombieStats.IsElite);
            _zombieActive = true;
            _zombieActor.GameObject.SetActive(true);
            _zombieActor.GameObject.transform.position = PickSpawnPosition(_zombieStats.Radius);
            _respawnCooldown = 0f;
            _contactCooldown = 0f;
            _ui?.SetRoundModifier(_run.RoundModel.Definition.IsElite);
            return true;
        }

        private static Vector2 PickSpawnPosition(float radius)
        {
            var edge = UnityEngine.Random.Range(0, 4);
            var inset = Mathf.Max(0.65f, radius + 0.2f);
            var x = UnityEngine.Random.Range(-GameBalance.ArenaHalfWidth + inset, GameBalance.ArenaHalfWidth - inset);
            var y = UnityEngine.Random.Range(-GameBalance.ArenaHalfHeight + inset, GameBalance.ArenaHalfHeight - inset);

            return edge switch
            {
                0 => new Vector2(-GameBalance.ArenaHalfWidth + inset, y),
                1 => new Vector2(GameBalance.ArenaHalfWidth - inset, y),
                2 => new Vector2(x, GameBalance.ArenaHalfHeight - inset),
                _ => new Vector2(x, -GameBalance.ArenaHalfHeight + inset)
            };
        }

        private void CreateActors()
        {
            _playerObject = new GameObject("Player");
            _playerObject.transform.SetParent(_worldRoot, false);
            _playerObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var playerRenderer = _playerObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = SpriteFactory.Circle;
            playerRenderer.color = new Color(0.24f, 0.86f, 0.98f);
            playerRenderer.sortingOrder = 20;

            _aimPivot = new GameObject("Aim Pivot").transform;
            _aimPivot.SetParent(_playerObject.transform, false);

            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(_aimPivot, false);
            barrel.transform.localPosition = new Vector3(0.36f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.48f, 0.14f, 1f);
            var barrelRenderer = barrel.AddComponent<SpriteRenderer>();
            barrelRenderer.sprite = SpriteFactory.Square;
            barrelRenderer.color = new Color(0.85f, 0.98f, 1f);
            barrelRenderer.sortingOrder = 21;

            _zombieActors = new ZombieActorPool(_worldRoot);
            _deathEffects = new DeathEffectPool(_worldRoot);
            _bullets = new BulletPool(_worldRoot);
        }
    }
}
