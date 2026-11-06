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
        private GameObject _zombieObject;
        private ZombieState _zombie;
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
        private GameSessionState _armoryReturnState;

        public Profile Profile => _profile;

        public RunState Run => _run;

        public GameSessionState State => _state;

        public WeaponId EquippedWeapon => _profile?.EquippedWeapon ?? WeaponId.Pistol;

        public WeaponStats EquippedWeaponStats => _weapon;

        public bool ArmoryVisible => _ui != null && _ui.ArmoryVisible;

        public float LastBulletDamageForTests { get; private set; }

        public bool PlayerActive => _playerObject != null && _playerObject.activeSelf;

        public bool ZombieActive => _zombieActive;

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

        public Vector2 ZombiePosition => _zombieObject != null
            ? (Vector2)_zombieObject.transform.position
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
            _weapon = WeaponCatalog.GetStats(_profile.EquippedWeapon);

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
                CloseArmory);

            _state = GameSessionState.MainMenu;
            _ui.ShowMainMenu(_profile);
            _playerObject.SetActive(false);
            _zombieObject.SetActive(false);
        }

        private void Update()
        {
            if (_input == null || _playerObject == null)
            {
                return;
            }

            var playerPosition = (Vector2)_playerObject.transform.position;
            var frame = _input.Read(playerPosition);

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

            var damage = _bullets.UpdateAndCheck(
                deltaTime,
                _zombieActive,
                _zombieObject.transform.position,
                GameBalance.ZombieRadius);

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

            _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
        }

        private void UpdatePlayer(InputFrame frame, float deltaTime)
        {
            if (frame.Move.sqrMagnitude > 0.0001f)
            {
                var current = (Vector2)_playerObject.transform.position;
                var next = current + (frame.Move * (GameBalance.PlayerMoveSpeed * deltaTime));
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
            LastBulletDamageForTests = _weapon.Damage;

            var origin = (Vector2)_playerObject.transform.position + (_aimDirection * 0.55f);
            var lifetime = _weapon.Range / _weapon.ProjectileSpeed;

            if (_weapon.PelletCount <= 1)
            {
                _bullets.Spawn(origin, _aimDirection, _weapon.ProjectileSpeed, lifetime, _weapon.Damage);
                return;
            }

            for (var i = 0; i < _weapon.PelletCount; i++)
            {
                var step = _weapon.PelletCount == 1
                    ? 0.5f
                    : (float)i / (_weapon.PelletCount - 1);
                var angle = (-_weapon.SpreadDegrees * 0.5f) + (_weapon.SpreadDegrees * step);
                _bullets.Spawn(
                    origin,
                    Rotate(_aimDirection, angle),
                    _weapon.ProjectileSpeed,
                    lifetime,
                    _weapon.Damage);
            }
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

            var definition = _run.RoundModel.Definition;
            var zombiePosition = (Vector2)_zombieObject.transform.position;
            var playerPosition = (Vector2)_playerObject.transform.position;
            var delta = playerPosition - zombiePosition;

            if (delta.sqrMagnitude > 0.0001f)
            {
                var direction = delta.normalized;
                zombiePosition += direction * (definition.ZombieMoveSpeed * deltaTime);
                CombatMath.ClampToArena(
                    zombiePosition.x,
                    zombiePosition.y,
                    GameBalance.ArenaHalfWidth,
                    GameBalance.ArenaHalfHeight,
                    GameBalance.ZombieRadius,
                    out var clampedX,
                    out var clampedY);
                zombiePosition = new Vector2(clampedX, clampedY);
                _zombieObject.transform.position = zombiePosition;
                _zombieObject.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f);
            }

            _contactCooldown = Mathf.Max(0f, _contactCooldown - deltaTime);
            if (_contactCooldown <= 0f && CombatMath.CirclesOverlap(
                    zombiePosition.x,
                    zombiePosition.y,
                    GameBalance.ZombieRadius,
                    playerPosition.x,
                    playerPosition.y,
                    GameBalance.PlayerRadius))
            {
                _contactCooldown = GameBalance.ZombieContactInterval;
                ApplyDamage(definition.ZombieContactDamage);
            }
        }

        public void StartRun()
        {
            _run = new RunState(GameBalance.PlayerMaxHealth, 1, Environment.TickCount);
            _weapon = WeaponCatalog.GetStats(_profile.EquippedWeapon);
            _runSuppliesEarned = 0;
            _runXpEarned = 0;
            _roundClearPending = false;
            _roundRewardOptions = null;
            _state = GameSessionState.Playing;

            _playerObject.SetActive(true);
            _playerObject.transform.position = Vector3.zero;
            _aimDirection = Vector2.right;
            _aimPivot.localRotation = Quaternion.identity;
            _fireCooldown = 0f;
            _contactCooldown = 0f;
            _bullets.Clear();

            SpawnZombie();
            _ui.ShowHud();
            _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
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
            _zombieObject.SetActive(false);
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
                _ui.ShowPaused(true);
                return;
            }

            _state = GameSessionState.MainMenu;
            _ui.ShowMainMenu(_profile);
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

            _weapon = WeaponCatalog.GetStats(weaponId);
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
                _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
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

            _zombieActive = false;
            _zombieObject.SetActive(false);
            _respawnCooldown = GameBalance.ZombieRespawnDelay;

            var reward = _run.RegisterKill();
            _profile.AddSupplies(reward.Supplies);
            _profile.AddXp(reward.Xp);
            _runSuppliesEarned += reward.Supplies;
            _runXpEarned += reward.Xp;
            _store.Save(_profile);

            if (reward.CompletedRound)
            {
                BeginRoundRewardFlow(reward);
                return;
            }

            _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
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
            _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
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

            var result = RoundRewardModel.Apply(_roundRewardOptions[index], _profile, _run);
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

            _ui.UpdateHud(_run.Player, _run.Round, _run.EnemiesRemaining, _run.Kills, _profile);
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
            _zombieActive = false;
            _zombieObject.SetActive(false);
            _playerObject.SetActive(false);

            _profile.RecordRunEnd(_run);
            _store.Save(_profile);
            _ui.ShowGameOver(_run.Round, _run.Kills, _runSuppliesEarned);
        }

        private bool SpawnZombie()
        {
            if (!_run.RoundModel.TryMarkSpawned())
            {
                return false;
            }

            var definition = _run.RoundModel.Definition;
            _zombie = new ZombieState(definition.ZombieMaxHealth);
            _zombieActive = true;
            _zombieObject.SetActive(true);
            _zombieObject.transform.position = PickSpawnPosition();
            _respawnCooldown = 0f;
            _contactCooldown = 0f;
            return true;
        }

        private static Vector2 PickSpawnPosition()
        {
            var edge = UnityEngine.Random.Range(0, 4);
            const float inset = 0.65f;
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

            _zombieObject = new GameObject("Zombie");
            _zombieObject.transform.SetParent(_worldRoot, false);
            var zombieRenderer = _zombieObject.AddComponent<SpriteRenderer>();
            zombieRenderer.sprite = SpriteFactory.Circle;
            zombieRenderer.color = new Color(0.86f, 0.25f, 0.3f);
            zombieRenderer.sortingOrder = 15;

            _bullets = new BulletPool(_worldRoot);
        }
    }
}
