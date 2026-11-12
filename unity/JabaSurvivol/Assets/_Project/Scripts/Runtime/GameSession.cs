using System;
using System.Threading;
using System.Threading.Tasks;
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
        GameOver,
        Victory
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
        private GameAudio _audio;
        private GameObject _playerObject;
        private Transform _aimPivot;
        private ZombieActor _zombieActor;
        private ZombieEncounterStats _zombieStats;
        private ZombieState _zombie;
        private ZombieActorPool _zombieActors;
        private DeathEffectPool _deathEffects;
        private GameObject _bossObject;
        private SpriteRenderer _bossRenderer;
        private GameObject _bossTelegraphRing;
        private SpriteRenderer _bossTelegraphRenderer;
        private float _bossTelegraphRemaining;
        private float _bossAttackCooldown;
        private bool _bossEnraged;
        private GameObject _turretObject;
        private SpriteRenderer _turretRenderer;
        private Transform _turretAimPivot;
        private Vector2 _turretPosition;
        private int _turretShotsFired;
        private BulletPool _bullets;
        private GameSessionState _state;
        private Vector2 _aimDirection = Vector2.right;
        private Vector2 _lastMoveDirection = Vector2.zero;
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
        private bool _lastBerserkerActive;
        private bool _lastGhostActive;
        private IAccountService _accountService;
        private CancellationTokenSource _accountCancellation;
        private ICloudSaveService _cloudSaveService;
        private RemoteConfigClient _remoteConfigClient;
        private bool _cloudSavesEnabled;
        private bool _cloudConfigLoaded;
        private Guid? _cloudAccountId;
        private bool _leaderboardsEnabled;
        private bool _telemetryEnabled;
        private double _telemetrySampleRate;
        private ILeaderboardService _leaderboardService;
        private ITelemetryService _telemetryService;
        private Guid _currentRunId;
        private DateTime _runStartedAt;
        private bool _runSubmitted;
        private readonly SemaphoreSlim _cloudActionGate = new(1, 1);
        private const float ProfileSaveIntervalSeconds = 0.75f;
        private bool _profileDirty;
        private float _nextProfileSaveTime;

        public Profile Profile => _profile;

        public RunState Run => _run;

        public GameSessionState State => _state;

        public CharacterStats Stats => _stats;

        public WeaponId EquippedWeapon => _profile?.EquippedWeapon ?? WeaponId.Pistol;

        public WeaponId CurrentWeapon => _run?.CurrentWeapon ?? EquippedWeapon;

        public int Magazine => _run?.Magazine ?? 0;

        public int MagazineSize => _run?.CurrentWeaponState.MagazineSize ?? 0;

        public int Reserve => _run?.Reserve ?? 0;

        public bool IsReloading => _run?.IsReloading ?? false;

        public float CurrentReloadDuration => _run?.CurrentWeaponState.ReloadDuration ?? 0f;

        public bool TurretActive => _run?.ActiveSkill.IsActive ?? false;

        public bool TurretVisible => _turretObject != null && _turretObject.activeSelf;

        public int ActiveSkillRank => _run?.ActiveSkill.Rank ?? 0;

        public float ActiveSkillCooldownRemaining => _run?.ActiveSkill.CooldownRemaining ?? 0f;

        public float ActiveSkillCooldownFraction => _run?.ActiveSkill.CooldownFraction ?? 0f;

        public bool ActiveSkillReady => _run?.ActiveSkill.CanActivate ?? false;

        public int TurretShotsFiredForTests => _turretShotsFired;

        public float ZombieHealthForTests => _zombie?.Health ?? 0f;

        public bool EnemySpawningEnabledForTests { get; set; } = true;

        public void SetZombiePositionForTests(Vector2 position)
        {
            if (_zombieActor != null)
            {
                _zombieActor.GameObject.transform.position = position;
            }
        }

        public WeaponStats EquippedWeaponStats => _weapon;

        public ArmorTier EquippedArmor => _profile?.EquippedArmor ?? ArmorTier.None;

        public ArmorStats EquippedArmorStats => _profile == null
            ? default
            : ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor);

        public bool ArmoryVisible => _ui != null && _ui.ArmoryVisible;

        public bool CharacterSheetVisible => _ui != null && _ui.CharacterSheetVisible;

        public bool SynergyTextVisible => _ui != null && _ui.SynergyTextVisible;

        public string SynergyTextForTests => _ui?.SynergyTextForTests ?? string.Empty;

        public bool LevelUpVisible => _ui != null && _ui.LevelUpVisible;

        public bool HudVisible => _ui != null && _ui.HudVisible;

        public GameAudio AudioForTests => _audio;

        public string SoundLabelForTests => _ui?.SoundLabelForTests ?? string.Empty;

        public string BuildLabelForTests => _ui?.BuildLabelForTests ?? string.Empty;

        public UnityEngine.UI.Button SoundButtonForTests => _ui?.SoundButtonForTests;

        public float LastBulletDamageForTests { get; private set; }

        public Vector2 LastShotDirectionForTests { get; private set; }

        public bool PlayerActive => _playerObject != null && _playerObject.activeSelf;

        public bool ZombieActive => _zombieActive;

        public ZombieArchetype CurrentZombieArchetype => _zombieStats.Archetype;

        public float CurrentZombieMaxHealth => _zombieStats.MaxHealth;

        public bool EliteRound => _run?.RoundModel.Definition.IsElite ?? false;

        public bool BossActive => _run?.BossActive ?? false;

        public bool BossSpawnPending => _run?.BossSpawnPending ?? false;

        public bool BossVisible => _ui != null && _ui.BossVisible;

        public bool VictoryVisible => _ui != null && _ui.VictoryVisible;

        public bool IsVictory => _run?.IsVictory ?? false;

        public bool BossEnraged => _run?.Boss?.IsEnraged ?? false;

        public bool BossTelegraphing => _bossTelegraphRemaining > 0f;

        public float BossHealth => _run?.Boss?.Health ?? 0f;

        public float BossMaxHealth => _run?.Boss?.MaxHealth ?? 0f;

        public int ZombiePoolCount => _zombieActors?.Count ?? 0;

        public int CurrentZombieActorId => _zombieActor != null
            ? _zombieActor.GameObject.GetInstanceID()
            : 0;

        public int ActiveDeathEffects => _deathEffects?.ActiveCount ?? 0;

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

        public int ProfileSaveCountForTests => _store?.SaveCount ?? 0;

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

        public void Initialize(
            ProfileStore store,
            IGameInput input,
            Transform worldRoot,
            IAccountService accountService = null,
            ICloudSaveService cloudSaveService = null,
            ILeaderboardService leaderboardService = null,
            ITelemetryService telemetryService = null)
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

            _audio = gameObject.GetComponent<GameAudio>() ?? gameObject.AddComponent<GameAudio>();

            _ui = new GameUi(
                transform,
                StartRun,
                RestartRun,
                ReturnToMenu,
                PauseGame,
                ResumeGame,
                ToggleSound,
                () => ReloadWeapon(),
                () => SwitchWeapon(),
                () => ActivateActiveSkill(),
                HandleSelectActiveSkill,
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
                CloseArmory,
                ShowAccount,
                HandleAccountRegister,
                HandleAccountLogin,
                HandleAccountLogout,
                HideAccount,
                HandleCloudBackup,
                HandleCloudRestore,
                HandleCloudUseCloud,
                HandleCloudKeepLocal,
                ShowLeaderboard,
                ToggleTelemetry,
                HideLeaderboard);

            _accountService = accountService ?? CreateDefaultAccountService();
            _accountService.StateChanged += HandleAccountStateChanged;
            _cloudSaveService = cloudSaveService ??
                GameBootstrap.CloudSaveServiceFactoryOverride?.Invoke(_accountService) ??
                CreateCloudSaveService();
            _cloudSaveService.StateChanged += HandleCloudStateChanged;
            _cloudSaveService.Configure(false);
            _leaderboardService = leaderboardService ??
                GameBootstrap.LeaderboardServiceFactoryOverride?.Invoke(_accountService) ??
                CreateLeaderboardService();
            _telemetryService = telemetryService ??
                GameBootstrap.TelemetryServiceFactoryOverride?.Invoke(_accountService) ??
                CreateTelemetryService();
            _remoteConfigClient = new RemoteConfigClient(ApiConfig.BaseUrl);
            _accountCancellation = new CancellationTokenSource();
            _ui.SetAccountState(_accountService.State, _accountService.Username);
            _ = InitializeAccountAsync();
            _ = InitializeCloudAsync();

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

            FlushProfileIfDue();

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

                if (frame.ReloadPressed)
                {
                    ReloadWeapon();
                }

                if (frame.SwitchWeaponPressed)
                {
                    SwitchWeapon();
                }

                if (frame.ActiveSkillPressed)
                {
                    ActivateActiveSkill();
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
            _run.TickWeapons(deltaTime);
            _run.TickActiveSkill(deltaTime);
            _run.TickSynergies(deltaTime);
            UpdateSynergyStats();
            UpdateTurret(deltaTime);

            if (_run.BossActive)
            {
                var boss = BossCatalog.BossDefinition;
                var bossPosition = (Vector2)_bossObject.transform.position;
                var bossDamage = _bullets.UpdateAndCheck(
                    deltaTime,
                    true,
                    bossPosition,
                    boss.SlamRadius * 0.35f);
                if (bossDamage > 0f)
                {
                    OnBulletHitBoss(bossDamage);
                }

                if (_state != GameSessionState.Playing)
                {
                    return;
                }

                UpdateBoss(deltaTime);
                if (_state != GameSessionState.Playing)
                {
                    return;
                }

                _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
                UpdateWeaponHud();
                return;
            }

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
            UpdateWeaponHud();
        }

        private void UpdatePlayer(InputFrame frame, float deltaTime)
        {
            if (frame.Move.sqrMagnitude > 0.0001f)
            {
                var current = (Vector2)_playerObject.transform.position;
                _lastMoveDirection = frame.Move.normalized;
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
            else
            {
                _lastMoveDirection = Vector2.zero;
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
                if (_run.TryFireCurrentWeapon())
                {
                    Fire();
                }
                else
                {
                    _run.StartReload();
                    _fireCooldown = 0.15f;
                }
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
                if (_run.TryFireCurrentWeapon())
                {
                    Fire();
                }
            }
        }

        /// <summary>
        /// Starts a manual reload on the current weapon.
        /// </summary>
        public bool ReloadWeapon()
        {
            if (_state != GameSessionState.Playing || _run == null)
            {
                return false;
            }

            return _run.StartReload();
        }

        /// <summary>
        /// Cycles to the next unlocked weapon and refreshes combat stats.
        /// </summary>
        public bool SwitchWeapon()
        {
            if (_state != GameSessionState.Playing || _run == null || !_run.SwitchNextWeapon())
            {
                return false;
            }

            _weapon = _run.CurrentWeaponState.Stats;
            RefreshStats();
            _fireCooldown = 0f;
            return true;
        }

        private void UpdateZombie(float deltaTime)
        {
            if (!_zombieActive)
            {
                if (_run.IsGameOver || !_run.RoundModel.IsActive || _roundClearPending)
                {
                    return;
                }

                if (!EnemySpawningEnabledForTests)
                {
                    return;
                }

                if (_run.BossSpawnPending)
                {
                    _respawnCooldown -= deltaTime;
                    if (_respawnCooldown <= 0f)
                    {
                        SpawnBoss();
                    }

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

        private bool SpawnBoss()
        {
            if (!_run.TrySpawnBoss())
            {
                return false;
            }

            var definition = BossCatalog.BossDefinition;
            _bossObject.SetActive(true);
            _bossObject.transform.position = PickSpawnPosition(definition.SlamRadius * 0.5f);
            _bossObject.transform.localScale = Vector3.one * definition.VisualScale;
            _bossRenderer.color = new Color(0.95f, 0.35f, 0.72f);
            _bossTelegraphRemaining = 0f;
            _bossAttackCooldown = definition.SlamIntervalSeconds;
            _bossEnraged = false;
            _bossTelegraphRing.SetActive(false);
            _ui.ShowBoss(Localization.Get("boss.name"));
            _ui.UpdateBoss(definition.MaxHealth, definition.MaxHealth, false);
            return true;
        }

        private void UpdateBoss(float deltaTime)
        {
            var boss = _run.Boss;
            if (boss == null || _bossObject == null || !_bossObject.activeSelf)
            {
                return;
            }

            var definition = BossCatalog.BossDefinition;
            var bossPosition = (Vector2)_bossObject.transform.position;
            var playerPosition = (Vector2)_playerObject.transform.position;

            _bossEnraged = boss.IsEnraged;
            _bossRenderer.color = _bossEnraged
                ? new Color(1f, 0.22f, 0.32f)
                : new Color(0.95f, 0.35f, 0.72f);

            if (_bossTelegraphRemaining > 0f)
            {
                _bossTelegraphRemaining -= deltaTime;
                var progress = 1f - Mathf.Clamp01(_bossTelegraphRemaining / definition.SlamTelegraphSeconds);
                var ringScale = definition.SlamRadius * 2f * (0.35f + (0.65f * progress));
                _bossTelegraphRing.transform.position = bossPosition;
                _bossTelegraphRing.transform.localScale = Vector3.one * ringScale;
                var ringColor = new Color(1f, 0.25f, 0.2f, 0.35f + (0.55f * progress));
                _bossTelegraphRenderer.color = ringColor;

                if (_bossTelegraphRemaining <= 0f)
                {
                    _bossTelegraphRing.SetActive(false);
                    if (CombatMath.CirclesOverlap(
                            bossPosition.x,
                            bossPosition.y,
                            definition.SlamRadius,
                            playerPosition.x,
                            playerPosition.y,
                            GameBalance.PlayerRadius))
                    {
                        ApplyDamage(definition.SlamDamage);
                    }

                    _bossAttackCooldown = _bossEnraged
                        ? definition.EnragedSlamIntervalSeconds
                        : definition.SlamIntervalSeconds;
                }
            }
            else
            {
                _bossAttackCooldown = Mathf.Max(0f, _bossAttackCooldown - deltaTime);
                if (_bossAttackCooldown <= 0f)
                {
                    _bossTelegraphRemaining = definition.SlamTelegraphSeconds;
                    _bossTelegraphRing.transform.position = bossPosition;
                    _bossTelegraphRing.transform.localScale = Vector3.one * (definition.SlamRadius * 2f * 0.35f);
                    _bossTelegraphRenderer.color = new Color(1f, 0.25f, 0.2f, 0.35f);
                    _bossTelegraphRing.SetActive(true);
                }
                else
                {
                    var delta = playerPosition - bossPosition;
                    if (delta.sqrMagnitude > 0.0001f)
                    {
                        var direction = delta.normalized;
                        bossPosition += direction * (definition.MoveSpeed * deltaTime);
                        CombatMath.ClampToArena(
                            bossPosition.x,
                            bossPosition.y,
                            GameBalance.ArenaHalfWidth,
                            GameBalance.ArenaHalfHeight,
                            definition.SlamRadius * 0.35f,
                            out var clampedX,
                            out var clampedY);
                        _bossObject.transform.position = new Vector2(clampedX, clampedY);
                    }
                }
            }

            _contactCooldown = Mathf.Max(0f, _contactCooldown - deltaTime);
            if (_contactCooldown <= 0f && CombatMath.CirclesOverlap(
                    bossPosition.x,
                    bossPosition.y,
                    definition.SlamRadius * 0.35f,
                    playerPosition.x,
                    playerPosition.y,
                    GameBalance.PlayerRadius))
            {
                _contactCooldown = definition.ContactInterval;
                ApplyDamage(definition.ContactDamage);
            }

            if (_state == GameSessionState.Playing)
            {
                _ui.UpdateBoss(boss.Health, boss.MaxHealth, _bossEnraged);
            }
        }

        private void OnBulletHitBoss(float damage)
        {
            if (!_run.BossActive)
            {
                return;
            }

            if (_run.ApplyBossDamage(damage, out var reward))
            {
                OnBossKilled(reward);
                return;
            }

            var boss = _run.Boss;
            if (boss != null)
            {
                _ui.UpdateBoss(boss.Health, boss.MaxHealth, boss.IsEnraged);
            }
        }

        private void OnBossKilled(KillReward reward)
        {
            var position = _bossObject != null
                ? (Vector2)_bossObject.transform.position
                : Vector2.zero;

            if (_bossObject != null)
            {
                _bossObject.SetActive(false);
            }

            if (_bossTelegraphRing != null)
            {
                _bossTelegraphRing.SetActive(false);
            }

            _bossTelegraphRemaining = 0f;
            _deathEffects.Spawn(position, new Color(1f, 0.35f, 0.72f), 2.4f);

            _profile.AddSupplies(reward.Supplies);
            var levelsGained = _profile.AddXp(reward.Xp);
            if (levelsGained > 0)
            {
                ShowLevelUpNotification();
            }

            _runSuppliesEarned += reward.Supplies;
            _runXpEarned += reward.Xp;
            _profile.RecordRunEnd(_run);
            _store.Save(_profile);
            _ = SyncCloudAsync();
            TrySubmitRun();
            if (TelemetryActive)
            {
                EnqueueTelemetry("run_end", $"{{\"highestRound\":{_run.Round},\"totalKills\":{_run.Kills},\"durationSeconds\":{RunDurationSeconds()}}}");
            }
            _ = FlushTelemetryAsync();

            _state = GameSessionState.Victory;
            _zombieActive = false;
            _playerObject.SetActive(false);
            _bullets.Clear();
            _audio?.Play(GameAudioCue.Victory);
            _ui.HideBoss();
            _ui.ShowVictory(_run.Round, _run.Kills, _runSuppliesEarned, _runXpEarned);
        }

        public void SpawnBossForTests()
        {
            if (_state == GameSessionState.Playing && _run != null && _run.BossSpawnPending)
            {
                SpawnBoss();
            }
        }

        public void DamageBossForTests(float amount)
        {
            if (_state == GameSessionState.Playing && _run != null && _run.BossActive)
            {
                OnBulletHitBoss(amount);
            }
        }

        public void SetBossAttackCooldownForTests(float seconds)
        {
            _bossAttackCooldown = Mathf.Max(0f, seconds);
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
            _run = new RunState(
                _stats.MaxHealth,
                startingRound,
                Environment.TickCount,
                _stats.Armor,
                _profile,
                _stats.AmmoCapacityMultiplier,
                _stats.ReloadSpeedMultiplier);
            _weapon = _run.CurrentWeaponState.Stats;
            _runSuppliesEarned = 0;
            _runXpEarned = 0;
            _currentRunId = Guid.NewGuid();
            _runStartedAt = DateTime.UtcNow;
            _runSubmitted = false;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _spreadSequence = 0;
            _levelUpRemaining = 0f;
            _lastBerserkerActive = false;
            _lastGhostActive = false;
            _state = GameSessionState.Playing;

            _playerObject.SetActive(true);
            _playerObject.transform.position = Vector3.zero;
            _aimDirection = Vector2.right;
            _lastMoveDirection = Vector2.zero;
            _aimPivot.localRotation = Quaternion.identity;
            _fireCooldown = 0f;
            _contactCooldown = 0f;
            _bullets.Clear();
            _deathEffects.Clear();
            _zombieActors.ReturnAll();
            _zombieActive = false;
            _bossObject.SetActive(false);
            _bossTelegraphRing.SetActive(false);
            _bossTelegraphRemaining = 0f;
            _bossAttackCooldown = 0f;
            _bossEnraged = false;
            _turretObject.SetActive(false);
            _turretShotsFired = 0;

            SpawnZombie();
            _ui.ShowHud();
            _ui.SetRoundModifier(_run.RoundModel.Definition.IsElite);
            _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
            UpdateWeaponHud();
            _ui.UpdateActiveSkillHud(
                _run.ActiveSkill.Node,
                _run.ActiveSkill.Rank,
                _run.ActiveSkill.CanActivate,
                _run.ActiveSkill.CooldownFraction,
                _run.ActiveSkill.ActiveFraction);
            _ui.SetSticks(default, default);
            _store.Save(_profile);
            if (TelemetryActive)
            {
                EnqueueTelemetry("run_start", $"{{\"startingRound\":{startingRound}}}");
            }
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
            _ui.SetSound(!_audio.IsMuted);
            _ui.SetSticks(default, default);
            _store.Save(_profile);
            _ = SyncCloudAsync();
            _ = FlushTelemetryAsync();
        }

        private void ToggleSound()
        {
            _audio.ToggleMute();
            _ui?.SetSound(!_audio.IsMuted);
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
                _ = SyncCloudAsync();
            }

            _state = GameSessionState.MainMenu;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _playerObject.SetActive(false);
            _zombieActive = false;
            _zombieActors.ReturnAll();
            _deathEffects.Clear();
            _bullets.Clear();
            if (_turretObject != null)
            {
                _turretObject.SetActive(false);
            }

            if (_bossObject != null)
            {
                _bossObject.SetActive(false);
            }

            if (_bossTelegraphRing != null)
            {
                _bossTelegraphRing.SetActive(false);
            }

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
            _ui.ShowArmory(_profile, ArmoryDisplayWeapon());
            _ui.SetSticks(default, default);
        }

        private WeaponId ArmoryDisplayWeapon()
        {
            return _state == GameSessionState.Paused && _run != null && !_run.IsGameOver
                ? _run.CurrentWeapon
                : _profile.EquippedWeapon;
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
                var boss = _run?.Boss;
                if (boss != null && _run.BossActive)
                {
                    _ui.ShowBoss(Localization.Get("boss.name"));
                    _ui.UpdateBoss(boss.Health, boss.MaxHealth, boss.IsEnraged);
                }

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

        private void HandleSelectActiveSkill(SkillNode node)
        {
            if (!TrySelectActiveSkill(node))
            {
                _ui.SetCharacterSheetStatus(Localization.Get("character.active.blocked"));
                return;
            }

            _ui.SetCharacterSheetStatus(
                Localization.Format("character.active.status", Localization.SkillName(node)));
        }

        /// <summary>
        /// Selects an allocated active skill from the armory or character
        /// sheet. The choice is stored in the profile.
        /// </summary>
        public bool TrySelectActiveSkill(SkillNode node)
        {
            if (!CanSelectActiveSkillNow() || !_profile.SetActiveSkill(node))
            {
                return false;
            }

            _run?.RefreshActiveSkill(_profile);
            _store.Save(_profile);
            RefreshCharacterSheetUi();
            if (_run != null)
            {
                _ui.UpdateActiveSkillHud(
                    _run.ActiveSkill.Node,
                    _run.ActiveSkill.Rank,
                    _run.ActiveSkill.CanActivate,
                    _run.ActiveSkill.CooldownFraction,
                    _run.ActiveSkill.ActiveFraction);
            }

            return true;
        }

        /// <summary>
        /// Test hook that presses the character-sheet active-skill button.
        /// </summary>
        public bool ClickSelectActiveSkillButtonForTests(SkillNode node)
        {
            return _ui != null && _ui.ClickSelectActiveSkillForTests(node);
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
            _audio?.Play(GameAudioCue.LevelUp);
            _levelUpRemaining = GameBalance.LevelUpNotificationSeconds;
            if (TelemetryActive)
            {
                EnqueueTelemetry("level_up", $"{{\"level\":{_profile.Level}}}");
            }
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

            _ui.RefreshArmory(_profile, ArmoryDisplayWeapon());
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
                        _profile.GetWeaponUpgradeTier(ArmoryDisplayWeapon(), stat)));
            }
            else if (result == WeaponUpgradePurchaseResult.InsufficientSupplies)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.insufficient"));
            }
            else if (result == WeaponUpgradePurchaseResult.MaxTier)
            {
                _ui.SetArmoryStatus(Localization.Get("armory.upgrade.maxed"));
            }

            _ui.RefreshArmory(_profile, ArmoryDisplayWeapon());
        }

        /// <summary>
        /// Purchases the next permanent tier for the weapon displayed in the
        /// armory and refreshes the effective combat stats.
        /// </summary>
        public WeaponUpgradePurchaseResult TryPurchaseWeaponUpgrade(WeaponUpgradeStat stat)
        {
            var weaponId = ArmoryDisplayWeapon();
            var result = WeaponUpgradeService.TryPurchase(_profile, weaponId, stat);
            if (result == WeaponUpgradePurchaseResult.Purchased)
            {
                RefreshStats();
                _store.Save(_profile);
            }

            return result;
        }

        private void RefreshStats()
        {
            var weaponId = _run != null && !_run.IsGameOver
                ? _run.CurrentWeapon
                : _profile.EquippedWeapon;
            _weapon = WeaponUpgradeService.GetEffectiveStats(_profile, weaponId);
            var healthFraction = _run != null && _run.Player.MaxHealth > 0f
                ? _run.Player.Health / _run.Player.MaxHealth
                : 1f;
            var synergy = SynergyService.GetTemporaryModifiers(
                _profile,
                healthFraction,
                _run?.Synergy.GhostActive ?? false);
            _stats = StatAggregation.Compute(
                _profile,
                _weapon,
                ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor),
                synergy);

            if (_run != null)
            {
                _run.RefreshWeaponStats(_profile);
                _run.RefreshActiveSkill(_profile);
                _run.ApplyAmmoStats(_stats.AmmoCapacityMultiplier, _stats.ReloadSpeedMultiplier);
            }
        }

        private bool CanAllocateSkillsNow()
        {
            return (_ui != null && _ui.ArmoryVisible) ||
                   _state == GameSessionState.Paused ||
                   _roundClearPending;
        }

        private bool CanSelectActiveSkillNow()
        {
            return (_ui != null && (_ui.ArmoryVisible || _ui.CharacterSheetVisible)) ||
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

            _ui.RefreshArmory(_profile, ArmoryDisplayWeapon());
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

            _ui.RefreshArmory(_profile, ArmoryDisplayWeapon());
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

            if (_run.ActiveSkill.Node == SkillNode.Dash && _run.ActiveSkill.IsActive)
            {
                return;
            }

            _audio?.Play(GameAudioCue.Hit);

            if (_run.DamagePlayer(amount))
            {
                EndRun();
            }
            else
            {
                _ui.UpdateHud(_run.Player, _run.Armor, _run.MaxArmor, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
                UpdateWeaponHud();
            }
        }

        /// <summary>
        /// Deploys the selected active skill when it is ready.
        /// </summary>
        public bool ActivateActiveSkill()
        {
            if (_state != GameSessionState.Playing || _run == null)
            {
                return false;
            }

            if (!_run.TryActivateActiveSkill())
            {
                return false;
            }

            var skill = _run.ActiveSkill;
            var playerPosition = (Vector2)_playerObject.transform.position;
            switch (skill.Node)
            {
                case SkillNode.Turret:
                    _turretPosition = playerPosition;
                    _turretObject.transform.position = _turretPosition;
                    _turretObject.SetActive(true);
                    break;

                case SkillNode.Dash:
                    ActivateDash(skill.Stats, playerPosition);
                    break;

                case SkillNode.Shockwave:
                    ActivateShockwave(skill.Stats, playerPosition);
                    break;
            }

            _ui.UpdateActiveSkillHud(
                skill.Node,
                skill.Rank,
                skill.CanActivate,
                skill.CooldownFraction,
                skill.ActiveFraction);
            return true;
        }

        private void ActivateDash(SkillActivationStats stats, Vector2 playerPosition)
        {
            var direction = _lastMoveDirection.sqrMagnitude > 0.0001f
                ? _lastMoveDirection
                : _aimDirection;
            var next = playerPosition + (direction * stats.DashDistance);
            CombatMath.ClampToArena(
                next.x,
                next.y,
                GameBalance.ArenaHalfWidth,
                GameBalance.ArenaHalfHeight,
                GameBalance.PlayerRadius,
                out var clampedX,
                out var clampedY);
            _playerObject.transform.position = new Vector3(clampedX, clampedY, 0f);
            _deathEffects.Spawn(playerPosition, new Color(0.45f, 0.95f, 1f), 0.9f);
        }

        private void ActivateShockwave(SkillActivationStats stats, Vector2 playerPosition)
        {
            if (_zombieActive && _zombieActor != null)
            {
                var zombiePosition = (Vector2)_zombieActor.GameObject.transform.position;
                var delta = zombiePosition - playerPosition;
                if (delta.magnitude <= stats.AreaRadius + _zombieStats.Radius)
                {
                    if (_zombie.ApplyDamage(stats.AreaDamage))
                    {
                        OnZombieKilled();
                    }
                    else if (delta.sqrMagnitude > 0.0001f)
                    {
                        var knocked = zombiePosition +
                                      (delta.normalized * GameBalance.ShockwaveKnockbackDistance);
                        CombatMath.ClampToArena(
                            knocked.x,
                            knocked.y,
                            GameBalance.ArenaHalfWidth,
                            GameBalance.ArenaHalfHeight,
                            _zombieStats.Radius,
                            out var knockedX,
                            out var knockedY);
                        _zombieActor.GameObject.transform.position = new Vector2(knockedX, knockedY);
                    }
                }
            }

            if (_run.BossActive && _bossObject != null)
            {
                var bossPosition = (Vector2)_bossObject.transform.position;
                var bossRadius = BossCatalog.BossDefinition.SlamRadius * 0.35f;
                if ((bossPosition - playerPosition).magnitude <= stats.AreaRadius + bossRadius)
                {
                    if (_run.ApplyBossDamage(stats.AreaDamage, out var reward))
                    {
                        OnBossKilled(reward);
                    }
                }
            }

            _deathEffects.Spawn(
                playerPosition,
                new Color(0.4f, 0.9f, 1f),
                Mathf.Max(0.5f, stats.AreaRadius * 2f));
        }

        private void UpdateTurret(float deltaTime)
        {
            if (_run == null || _turretObject == null)
            {
                return;
            }

            var skill = _run.ActiveSkill;
            if (skill.Node == SkillNode.Turret && skill.IsActive)
            {
                if (!_turretObject.activeSelf)
                {
                    _turretPosition = (Vector2)_playerObject.transform.position;
                    _turretObject.transform.position = _turretPosition;
                    _turretObject.SetActive(true);
                }

                if (TryGetTurretTarget(out var target) && skill.TryConsumeShot(out var damage))
                {
                    var direction = target - _turretPosition;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        direction.Normalize();
                        _turretAimPivot.localRotation = Quaternion.Euler(
                            0f,
                            0f,
                            (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
                        _bullets.Spawn(
                            _turretPosition,
                            direction,
                            GameBalance.TurretProjectileSpeed,
                            GameBalance.TurretRange / GameBalance.TurretProjectileSpeed,
                            damage);
                        _turretShotsFired++;
                    }
                }
            }
            else if (_turretObject.activeSelf)
            {
                _turretObject.SetActive(false);
            }

            _ui.UpdateActiveSkillHud(
                skill.Node,
                skill.Rank,
                skill.CanActivate,
                skill.CooldownFraction,
                skill.ActiveFraction);
        }

        private bool TryGetTurretTarget(out Vector2 target)
        {
            var rangeSquared = GameBalance.TurretRange * GameBalance.TurretRange;

            if (_run.BossActive && _bossObject != null)
            {
                target = _bossObject.transform.position;
                return (target - _turretPosition).sqrMagnitude <= rangeSquared;
            }

            if (_zombieActive && _zombieActor != null)
            {
                target = _zombieActor.GameObject.transform.position;
                return (target - _turretPosition).sqrMagnitude <= rangeSquared;
            }

            target = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Test hook that deploys the selected active skill.
        /// </summary>
        public bool ActivateActiveSkillForTests()
        {
            return ActivateActiveSkill();
        }

        private void UpdateSynergyStats()
        {
            if (_run == null)
            {
                return;
            }

            var healthFraction = _run.Player.MaxHealth <= 0f
                ? 0f
                : _run.Player.Health / _run.Player.MaxHealth;
            var berserker = _run.Synergy.BerserkerActive(_profile, healthFraction);
            var ghost = _run.Synergy.GhostActive;
            if (berserker == _lastBerserkerActive && ghost == _lastGhostActive)
            {
                return;
            }

            _lastBerserkerActive = berserker;
            _lastGhostActive = ghost;
            RefreshStats();
        }

        private void UpdateWeaponHud()        {
            if (_ui == null || _run == null)
            {
                return;
            }

            var state = _run.CurrentWeaponState;
            _ui.UpdateWeaponHud(
                Localization.WeaponName(state.Id),
                state.Magazine,
                state.MagazineSize,
                state.Reserve,
                state.HasInfiniteReserve,
                state.IsReloading,
                state.ReloadRemaining,
                state.ReloadDuration);
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

        private static IAccountService CreateDefaultAccountService()
        {
            if (GameBootstrap.OnlineDisabledForTests && GameBootstrap.AccountServiceFactoryOverride == null)
            {
                return new AccountService(
                    new AuthApiClient(ApiConfig.BaseUrl),
                    new InMemorySecureTokenStore());
            }

            return AccountService.CreateDefault();
        }

        private ILeaderboardService CreateLeaderboardService()
        {
            return new LeaderboardService(
                () => _accountService?.AccessToken,
                token => _accountService != null
                    ? _accountService.RefreshAsync(token)
                    : Task.FromResult(false),
                () => _leaderboardsEnabled,
                ApiConfig.BaseUrl);
        }

        private ITelemetryService CreateTelemetryService()
        {
            return new TelemetryService(
                () => _accountService?.AccessToken,
                token => _accountService != null
                    ? _accountService.RefreshAsync(token)
                    : Task.FromResult(false),
                baseUrl: ApiConfig.BaseUrl);
        }

        private void ShowLeaderboard()
        {
            _ui?.ShowLeaderboard();
            if (!_leaderboardsEnabled)
            {
                _ui?.SetLeaderboardStatus(Localization.Get("leaderboard.disabled"));
                return;
            }

            _ = LoadLeaderboardAsync();
        }

        private void HideLeaderboard()
        {
            _ui?.HideLeaderboard();
        }

        private async Task LoadLeaderboardAsync()
        {
            try
            {
                _ui.SetLeaderboardStatus(Localization.Get("leaderboard.loading"));
                var result = await _leaderboardService.LoadAsync(_accountCancellation.Token);
                if (result.Success)
                {
                    _ui.SetLeaderboardEntries(result.Entries);
                }
                else
                {
                    _ui.SetLeaderboardStatus(result.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui.SetLeaderboardStatus(Localization.Get("leaderboard.error"));
            }
        }

        private void ToggleTelemetry()
        {
            if (!_telemetryEnabled || _telemetryService == null)
            {
                return;
            }

            _telemetryService.SetOptIn(!_telemetryService.OptedIn);
            _ui.SetTelemetry(true, _telemetryService.OptedIn);
            if (_telemetryService.OptedIn)
            {
                _ = FlushTelemetryAsync();
            }
        }

        private bool TelemetryActive =>
            _telemetryEnabled && _telemetryService != null && _telemetryService.OptedIn;

        private void EnqueueTelemetry(string eventType, string payloadJson)
        {
            if (!TelemetryActive)
            {
                return;
            }

            _telemetryService.Enqueue(eventType, payloadJson);
        }

        private static string TelemetryArchetypeName(ZombieArchetype archetype)
        {
            return archetype switch
            {
                ZombieArchetype.Walker => "walker",
                ZombieArchetype.Runner => "runner",
                ZombieArchetype.Brute => "brute",
                _ => "unknown"
            };
        }

        private async Task FlushTelemetryAsync()
        {
            if (_telemetryService == null || !_telemetryService.OptedIn)
            {
                return;
            }

            try
            {
                await _telemetryService.FlushAsync(_accountCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                // Telemetry never blocks gameplay.
            }
        }

        private int RunDurationSeconds()
        {
            if (_runStartedAt == default)
            {
                return 1;
            }

            return Math.Max(1, (int)(DateTime.UtcNow - _runStartedAt).TotalSeconds);
        }

        private void TrySubmitRun()
        {
            if (_runSubmitted || _run == null || !_leaderboardsEnabled)
            {
                return;
            }

            _runSubmitted = true;
            _ = SubmitRunAsync(_run.Round, _run.Kills, RunDurationSeconds());
        }

        private async Task SubmitRunAsync(int highestRound, int totalKills, int durationSeconds)
        {
            try
            {
                await _leaderboardService.SubmitAsync(
                    _currentRunId,
                    highestRound,
                    totalKills,
                    durationSeconds,
                    _accountCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                // Leaderboard submission is non-blocking.
            }
        }
        private ICloudSaveService CreateCloudSaveService()
        {
            var api = new CloudSaveApiClient(
                () => _accountService?.AccessToken,
                token => _accountService != null
                    ? _accountService.RefreshAsync(token)
                    : Task.FromResult(false),
                ApiConfig.BaseUrl);
            return new CloudSaveService(api, _store, _accountService);
        }

        private async Task InitializeCloudAsync()
        {
            try
            {
                var config = GameBootstrap.RemoteConfigOverride?.Invoke();
                if (config is null && GameBootstrap.OnlineDisabledForTests)
                {
                    config = new RemoteConfigDto { cloudSavesEnabled = false, leaderboardsEnabled = false, telemetryEnabled = false };
                }

                config ??= await _remoteConfigClient.GetAsync(_accountCancellation.Token);
                _cloudSavesEnabled = config?.cloudSavesEnabled ?? false;
                _leaderboardsEnabled = config?.leaderboardsEnabled ?? false;
                _telemetryEnabled = config?.telemetryEnabled ?? false;
                _telemetrySampleRate = config?.telemetrySampleRate ?? 0d;
                _telemetryService.Configure(_telemetryEnabled, _telemetrySampleRate);
                _ui.SetTelemetry(_telemetryEnabled, _telemetryService.OptedIn);
                _cloudConfigLoaded = true;
                _cloudSaveService.Configure(_cloudSavesEnabled);
                _ui.SetCloudControls(_accountService.State == AccountState.SignedIn, _cloudSavesEnabled);
                _ui.SetCloudState(_cloudSaveService.State, null);
                if (_accountService.State == AccountState.SignedIn)
                {
                    await SyncCloudAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui?.SetCloudState(CloudSyncState.Offline, null);
            }
        }

        public void SyncCloudNow()
        {
            _ = SyncCloudAsync();
        }

        public void FlushTelemetryNow() => _ = FlushTelemetryAsync();

        public void ShowLeaderboardForTests() => ShowLeaderboard();

        public void ToggleTelemetryForTests() => ToggleTelemetry();

        public bool TelemetryOptedInForTests => _telemetryService != null && _telemetryService.OptedIn;

        public int TelemetryPendingForTests => _telemetryService?.PendingCount ?? 0;

        public bool LeaderboardVisibleForTests => _ui != null && _ui.LeaderboardVisibleForTests;

        public bool TelemetryVisibleForTests => _ui != null && _ui.TelemetryVisibleForTests;

        public ITelemetryService TelemetryServiceForTests => _telemetryService;

        public string LeaderboardStatusForTests => _ui?.LeaderboardStatusForTests ?? string.Empty;

        public string LeaderboardTextForTests => _ui?.LeaderboardTextForTests ?? string.Empty;

        public bool LeaderboardScrollableForTests => _ui != null && _ui.LeaderboardScrollableForTests;

        public void BackupCloudForTests() => HandleCloudBackup();

        public void RestoreCloudForTests() => HandleCloudRestore();

        public void UseCloudForTests() => HandleCloudUseCloud();

        public void KeepLocalForTests() => HandleCloudKeepLocal();

        private async Task SyncCloudAsync()
        {
            if (_cloudSaveService == null ||
                _accountCancellation == null ||
                !_cloudConfigLoaded ||
                !_cloudSavesEnabled ||
                !_cloudSaveService.Enabled ||
                _accountService.State != AccountState.SignedIn)
            {
                return;
            }

            if (!await _cloudActionGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                var result = await _cloudSaveService.SyncAsync(_profile, _accountCancellation.Token);
                ApplyCloudResult(result);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui?.SetCloudState(CloudSyncState.Offline, null);
            }
            finally
            {
                _cloudActionGate.Release();
            }
        }

        private async void HandleCloudBackup()
        {
            await RunCloudAction((profile, token) => _cloudSaveService.BackupAsync(profile, token));
        }

        private async void HandleCloudRestore()
        {
            await RunCloudAction((profile, token) => _cloudSaveService.RestoreAsync(profile, token));
        }

        private async void HandleCloudUseCloud()
        {
            await RunCloudAction((profile, token) => _cloudSaveService.UseCloudAsync(profile, token));
        }

        private async void HandleCloudKeepLocal()
        {
            await RunCloudAction((profile, token) => _cloudSaveService.KeepLocalAsync(profile, token));
        }

        private async Task RunCloudAction(
            Func<Profile, CancellationToken, Task<CloudSyncResult>> action)
        {
            if (_cloudSaveService == null || _accountCancellation == null)
            {
                return;
            }

            await _cloudActionGate.WaitAsync(_accountCancellation.Token);
            try
            {
                _ui.SetCloudState(CloudSyncState.Syncing, null);
                var result = await action(_profile, _accountCancellation.Token);
                ApplyCloudResult(result);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui.SetCloudState(CloudSyncState.Offline, null);
            }
            finally
            {
                _cloudActionGate.Release();
            }
        }

        private void ApplyCloudResult(CloudSyncResult result)
        {
            _ui.SetCloudState(result.State, result.Message);
            if (result.RestoredProfile != null)
            {
                ApplyRestoredProfile(result.RestoredProfile);
            }

            if (result.Conflict)
            {
                _ui.ShowCloudConflict(BuildCloudConflictDetails());
            }
            else if (result.State != CloudSyncState.Conflict)
            {
                _ui.HideCloudConflict();
            }
        }

        private void ApplyRestoredProfile(Profile restored)
        {
            _profile = restored;
            _profile.Validate();
            _store.Save(_profile);
            _weapon = WeaponUpgradeService.GetEffectiveStats(_profile, _profile.EquippedWeapon);
            _stats = StatAggregation.Compute(
                _profile,
                _weapon,
                ArmorService.GetEffectiveStats(_profile, _profile.EquippedArmor));
            _ui.ShowMainMenu(_profile);
            _ui.SetAccountState(_accountService.State, _accountService.Username);
            _ui.SetCloudControls(_accountService.State == AccountState.SignedIn, _cloudSavesEnabled);
        }

        private string BuildCloudConflictDetails()
        {
            return Localization.Format(
                "cloud.conflict.details",
                _profile.CloudSaveRevision,
                _cloudSaveService.CloudRevision,
                _cloudSaveService.CloudUpdatedAt);
        }
        public IAccountService AccountServiceForTests => _accountService;

        public ICloudSaveService CloudSaveServiceForTests => _cloudSaveService;

        public string CloudStatusForTests => _ui?.CloudStatusForTests ?? string.Empty;

        public bool CloudConflictVisibleForTests => _ui != null && _ui.CloudConflictVisibleForTests;

        public bool AccountPanelVisibleForTests => _ui != null && _ui.AccountPanelVisibleForTests;

        public string AccountStatusForTests => _ui?.AccountStatusForTests ?? string.Empty;

        public string MainMenuAccountStateForTests => _ui?.MainMenuAccountStateForTests ?? string.Empty;

        public void ShowAccountForTests() => ShowAccount();

        public void SubmitRegisterForTests(string username, string password) =>
            HandleAccountRegister(username, password);

        public void SubmitLoginForTests(string username, string password) =>
            HandleAccountLogin(username, password);

        public void LogoutForTests() => HandleAccountLogout();

        private void ShowAccount()
        {
            if (_ui == null || _state != GameSessionState.MainMenu)
            {
                return;
            }

            _ui.ShowAccount();
            HandleAccountStateChanged(_accountService.State);
        }

        private void HideAccount()
        {
            _ui?.HideAccount();
        }

        private async void HandleAccountRegister(string username, string password)
        {
            await RunAccountAction(token => _accountService.RegisterAsync(username, password, token));
        }

        private async void HandleAccountLogin(string username, string password)
        {
            await RunAccountAction(token => _accountService.LoginAsync(username, password, token));
        }

        private async void HandleAccountLogout()
        {
            if (_accountService == null)
            {
                return;
            }

            _ui.SetAccountBusy(true);
            try
            {
                await _accountService.LogoutAsync(_accountCancellation.Token);
                _ui.SetAccountStatus(Localization.Get("account.state.signedout"));
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            finally
            {
                _ui.SetAccountBusy(false);
                HandleAccountStateChanged(_accountService.State);
            }
        }

        private async Task RunAccountAction(
            Func<CancellationToken, Task<AccountActionResult>> action)
        {
            if (_accountService == null || _accountCancellation == null)
            {
                return;
            }

            _ui.SetAccountBusy(true);
            try
            {
                var result = await action(_accountCancellation.Token);
                _ui.SetAccountStatus(result.Message);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui.SetAccountStatus(Localization.Get("account.error.generic"));
            }
            finally
            {
                _ui.SetAccountBusy(false);
                HandleAccountStateChanged(_accountService.State);
            }
        }

        private async Task InitializeAccountAsync()
        {
            try
            {
                await _accountService.InitializeAsync(_accountCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // The session is ending.
            }
            catch (Exception)
            {
                _ui?.SetAccountStatus(Localization.Get("account.error.network"));
            }
        }

        private void HandleAccountStateChanged(AccountState state)
        {
            _ui?.SetAccountState(state, _accountService?.Username ?? string.Empty);
            _ui?.SetCloudControls(state == AccountState.SignedIn, _cloudSavesEnabled);

            var accountId = state == AccountState.SignedIn ? _accountService?.AccountId : null;
            if (_cloudSaveService != null && (_cloudAccountId != accountId || state != AccountState.SignedIn))
            {
                _cloudSaveService.Reset();
                _ui?.HideCloudConflict();
                _cloudAccountId = accountId;
            }

            if (state == AccountState.SignedIn && _cloudConfigLoaded)
            {
                _ = SyncCloudAsync();
            }
        }

        private void HandleCloudStateChanged(CloudSyncState state)
        {
            _ui?.SetCloudState(state, null);
            if (state == CloudSyncState.Conflict)
            {
                _ui?.ShowCloudConflict(BuildCloudConflictDetails());
            }
            else if (state != CloudSyncState.Syncing)
            {
                _ui?.HideCloudConflict();
            }
        }

        private void OnDestroy()
        {
            SaveNow();

            if (_accountService != null)
            {
                _accountService.StateChanged -= HandleAccountStateChanged;
                if (_accountService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _accountCancellation?.Cancel();
            _accountCancellation?.Dispose();
            _accountCancellation = null;
            _cloudActionGate.Dispose();
            if (_leaderboardService is IDisposable leaderboardDisposable)
            {
                leaderboardDisposable.Dispose();
            }

            if (_telemetryService is IDisposable telemetryDisposable)
            {
                telemetryDisposable.Dispose();
            }

            _remoteConfigClient?.Dispose();
            _accountService = null;
        }
        public void SaveNow()
        {
            if (_store != null && _profile != null)
            {
                _store.Save(_profile);
            }

            _profileDirty = false;
            _nextProfileSaveTime = 0f;
        }

        private void MarkProfileDirty()
        {
            _profileDirty = true;
            if (_nextProfileSaveTime <= 0f)
            {
                _nextProfileSaveTime = Time.unscaledTime + ProfileSaveIntervalSeconds;
            }
        }

        private void FlushProfileIfDue()
        {
            if (!_profileDirty || Time.unscaledTime < _nextProfileSaveTime)
            {
                return;
            }

            SaveNow();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
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

            _audio?.Play(GameAudioCue.Kill);

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
            MarkProfileDirty();
            if (TelemetryActive)
            {
                EnqueueTelemetry(
                    "zombie_kill",
                    $"{{\"round\":{_run.Round},\"archetype\":\"{TelemetryArchetypeName(archetype)}\"}}");
            }

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
            _audio?.Play(GameAudioCue.Reward);
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
            if (_bossObject != null)
            {
                _bossObject.SetActive(false);
            }

            if (_bossTelegraphRing != null)
            {
                _bossTelegraphRing.SetActive(false);
            }

            _ui.HideBoss();
            if (_turretObject != null)
            {
                _turretObject.SetActive(false);
            }

            _audio?.Play(GameAudioCue.Defeat);
            _profile.RecordRunEnd(_run);
            _store.Save(_profile);
            _ = SyncCloudAsync();
            TrySubmitRun();
            if (TelemetryActive)
            {
                EnqueueTelemetry("run_end", $"{{\"highestRound\":{_run.Round},\"totalKills\":{_run.Kills},\"durationSeconds\":{RunDurationSeconds()}}}");
            }
            _ = FlushTelemetryAsync();
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

            _bossObject = new GameObject("Boss");
            _bossObject.transform.SetParent(_worldRoot, false);
            _bossRenderer = _bossObject.AddComponent<SpriteRenderer>();
            _bossRenderer.sprite = SpriteFactory.Circle;
            _bossRenderer.color = new Color(0.95f, 0.35f, 0.72f);
            _bossRenderer.sortingOrder = 18;

            var bossRing = new GameObject("Boss Ring");
            bossRing.transform.SetParent(_bossObject.transform, false);
            bossRing.transform.localScale = Vector3.one * 1.3f;
            var bossRingRenderer = bossRing.AddComponent<SpriteRenderer>();
            bossRingRenderer.sprite = SpriteFactory.Ring;
            bossRingRenderer.color = new Color(1f, 0.7f, 0.95f, 0.8f);
            bossRingRenderer.sortingOrder = 19;
            _bossObject.SetActive(false);

            _bossTelegraphRing = new GameObject("Boss Telegraph");
            _bossTelegraphRing.transform.SetParent(_worldRoot, false);
            _bossTelegraphRenderer = _bossTelegraphRing.AddComponent<SpriteRenderer>();
            _bossTelegraphRenderer.sprite = SpriteFactory.Ring;
            _bossTelegraphRenderer.color = new Color(1f, 0.25f, 0.2f, 0.4f);
            _bossTelegraphRenderer.sortingOrder = 12;
            _bossTelegraphRing.SetActive(false);

            _turretObject = new GameObject("Turret");
            _turretObject.transform.SetParent(_worldRoot, false);
            _turretObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            _turretRenderer = _turretObject.AddComponent<SpriteRenderer>();
            _turretRenderer.sprite = SpriteFactory.Circle;
            _turretRenderer.color = new Color(0.35f, 1f, 0.72f);
            _turretRenderer.sortingOrder = 22;

            _turretAimPivot = new GameObject("Turret Barrel").transform;
            _turretAimPivot.SetParent(_turretObject.transform, false);
            var turretBarrel = new GameObject("Turret Barrel Visual");
            turretBarrel.transform.SetParent(_turretAimPivot, false);
            turretBarrel.transform.localPosition = new Vector3(0.32f, 0f, 0f);
            turretBarrel.transform.localScale = new Vector3(0.44f, 0.13f, 1f);
            var turretBarrelRenderer = turretBarrel.AddComponent<SpriteRenderer>();
            turretBarrelRenderer.sprite = SpriteFactory.Square;
            turretBarrelRenderer.color = new Color(0.9f, 1f, 0.94f);
            turretBarrelRenderer.sortingOrder = 23;
            _turretObject.SetActive(false);

            _bullets = new BulletPool(_worldRoot);
        }
    }
}
