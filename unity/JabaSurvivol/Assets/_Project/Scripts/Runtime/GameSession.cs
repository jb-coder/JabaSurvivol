using System;
using JabaSurvivol.Core;
using UnityEngine;

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
        private int _runSuppliesEarned;

        public Profile Profile => _profile;

        public RunState Run => _run;

        public GameSessionState State => _state;

        public bool PlayerActive => _playerObject != null && _playerObject.activeSelf;

        public bool ZombieActive => _zombieActive;

        public int ActiveBullets => _bullets?.ActiveCount ?? 0;

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

            CreateActors();

            _ui = new GameUi(
                transform,
                StartRun,
                RestartRun,
                ReturnToMenu,
                PauseGame,
                ResumeGame);

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

            if (frame.PausePressed)
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
                UpdatePlaying(frame, Time.deltaTime);
                _ui.SetSticks(frame.MoveStick, frame.AimStick);
            }
            else
            {
                _ui.SetSticks(default, default);
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

            var hit = _bullets.UpdateAndCheck(
                deltaTime,
                _zombieActive,
                _zombieObject.transform.position,
                GameBalance.ZombieRadius,
                OnBulletHitZombie);

            if (hit && _state != GameSessionState.Playing)
            {
                return;
            }

            UpdateZombie(deltaTime);
            if (_state != GameSessionState.Playing)
            {
                return;
            }

            _ui.UpdateHud(_run.Player, _run.Round, _run.Kills, _profile);
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
            _fireCooldown = GameBalance.PistolFireInterval;
            var origin = (Vector2)_playerObject.transform.position + (_aimDirection * 0.55f);
            _bullets.Spawn(origin, _aimDirection);
        }

        private void UpdateZombie(float deltaTime)
        {
            if (!_zombieActive)
            {
                if (_run.IsGameOver)
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

            var zombiePosition = (Vector2)_zombieObject.transform.position;
            var playerPosition = (Vector2)_playerObject.transform.position;
            var delta = playerPosition - zombiePosition;

            if (delta.sqrMagnitude > 0.0001f)
            {
                var direction = delta.normalized;
                zombiePosition += direction * (GameBalance.ZombieMoveSpeed * deltaTime);
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
                ApplyDamage(GameBalance.ZombieContactDamage);
            }
        }

        public void StartRun()
        {
            _run = new RunState(GameBalance.PlayerMaxHealth);
            _runSuppliesEarned = 0;
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
            _ui.UpdateHud(_run.Player, _run.Round, _run.Kills, _profile);
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
            _playerObject.SetActive(false);
            _zombieActive = false;
            _zombieObject.SetActive(false);
            _bullets.Clear();
            _ui.ShowMainMenu(_profile);
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
                _ui.UpdateHud(_run.Player, _run.Round, _run.Kills, _profile);
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

        private void OnBulletHitZombie()
        {
            if (!_zombieActive || _zombie == null)
            {
                return;
            }

            if (_zombie.ApplyDamage(GameBalance.PistolDamage))
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
            _store.Save(_profile);

            _ui.UpdateHud(_run.Player, _run.Round, _run.Kills, _profile);
        }

        private void EndRun()
        {
            if (_state == GameSessionState.GameOver)
            {
                return;
            }

            _state = GameSessionState.GameOver;
            _zombieActive = false;
            _zombieObject.SetActive(false);
            _playerObject.SetActive(false);

            _profile.RecordRunEnd(_run);
            _store.Save(_profile);
            _ui.ShowGameOver(_run.Round, _run.Kills, _runSuppliesEarned);
        }

        private void SpawnZombie()
        {
            _zombie = new ZombieState(GameBalance.ZombieMaxHealth);
            _zombieActive = true;
            _zombieObject.SetActive(true);
            _zombieObject.transform.position = PickSpawnPosition();
            _respawnCooldown = 0f;
            _contactCooldown = 0f;
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
