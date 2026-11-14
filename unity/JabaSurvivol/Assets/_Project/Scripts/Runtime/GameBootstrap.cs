using System;
using System.Collections;
using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Scene entry point. Builds the camera, arena, input, and game session at
    /// runtime so the walking skeleton stays easy to bootstrap and test.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static string SavePathOverride;

        public static Func<IAccountService> AccountServiceFactoryOverride;

        public static Func<IAccountService, ICloudSaveService> CloudSaveServiceFactoryOverride;

        public static Func<RemoteConfigDto> RemoteConfigOverride;

        public static Func<IAccountService, ILeaderboardService> LeaderboardServiceFactoryOverride;

        public static Func<IAccountService, ITelemetryService> TelemetryServiceFactoryOverride;

        public static bool OnlineDisabledForTests;

        public static GameBootstrap Instance { get; private set; }

        private Transform _worldRoot;
        private Camera _camera;

        public GameSession Session { get; private set; }

        public SpriteRenderer FloorRendererForTests { get; private set; }

        public int ArenaWallCountForTests { get; private set; }

        public int ArenaPropCountForTests { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            ApiConfig.Initialize();
#if DEVELOPMENT_BUILD
            SeedDevelopmentSession();
            RunSecureStorageProbe();
#endif

            var runtimeRoot = new GameObject("Runtime").transform;
            runtimeRoot.SetParent(transform, false);

            _camera = CreateCamera(runtimeRoot);
            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(runtimeRoot, false);
            BuildArena(_worldRoot);

            var input = gameObject.AddComponent<TwinStickInput>();
            input.Initialize(_camera);

            Session = gameObject.AddComponent<GameSession>();
            Session.Initialize(
                new ProfileStore(SavePathOverride),
                input,
                _worldRoot,
                AccountServiceFactoryOverride?.Invoke());
#if DEVELOPMENT_BUILD
            gameObject.AddComponent<PerformanceProbe>();
            ApplyDebugScenario();
#endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Session?.SaveNow();
                Session?.SyncCloudNow();
                Session?.FlushTelemetryNow();
            }
        }

        private void OnApplicationQuit()
        {
            Session?.SaveNow();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Session?.SaveNow();
            Instance = null;
        }

#if DEVELOPMENT_BUILD
        private void ApplyDebugScenario()
        {
            var scenario = ReadDebugScenario();
            if (string.IsNullOrWhiteSpace(scenario))
            {
                return;
            }

            StartCoroutine(ApplyDebugScenarioAfterFrame(scenario));
        }

        private IEnumerator ApplyDebugScenarioAfterFrame(string scenario)
        {
            yield return null;

            switch (scenario)
            {
                case "boss":
                    Session.StartRunForTests(GameBalance.BossRound);
                    yield return null;
                    var budget = Session.SpawnBudget;
                    for (var i = 0; i < budget; i++)
                    {
                        Session.DamageZombieForTests(100000f);
                        yield return null;
                        if (i < budget - 1)
                        {
                            Session.SpawnZombieForTests();
                            yield return null;
                        }
                    }

                    Session.SpawnBossForTests();
                    break;

                case "reward":
                    Session.StartRun();
                    yield return null;
                    Session.ShowRoundEndForTests(new[]
                    {
                        new RoundRewardOption(RoundRewardType.Supplies, 100),
                        new RoundRewardOption(RoundRewardType.Xp, 50),
                        new RoundRewardOption(RoundRewardType.AmmoCrate, 1)
                    });
                    break;

                case "armory":
                    Session.StartRun();
                    yield return null;
                    Session.PauseGame();
                    Session.ShowArmory();
                    break;

                case "character":
                    Session.StartRun();
                    yield return null;
                    Session.PauseGame();
                    Session.ShowCharacterSheet();
                    break;

                case "pause":
                    Session.StartRun();
                    yield return null;
                    Session.PauseGame();
                    break;
            }
        }

        private static string ReadDebugScenario()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return string.Empty;
            }

            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                return intent?.Call<string>("getStringExtra", "jabaScenario") ?? string.Empty;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Debug scenario read failed: " + exception.GetType().Name);
                return string.Empty;
            }
        }

        private static void SeedDevelopmentSession()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                var token = intent?.Call<string>("getStringExtra", "seedRefreshToken");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    SecureTokenStore.CreateDefault().Save(token);
                    Debug.Log("Development session seeded.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Development session seed failed: " + exception.GetType().Name);
            }
        }

#endif

#if DEVELOPMENT_BUILD
        private static void RunSecureStorageProbe()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            try
            {
                var probePath = System.IO.Path.Combine(Application.persistentDataPath, "probe-session.bin");
                var probe = new AndroidKeystoreTokenStore(probePath, "jaba_survivol_probe");
                probe.ResetKey();
                probe.Clear();
                probe.Save("probe-token");
                var restored = probe.Load() == "probe-token";
                probe.Clear();
                probe.ResetKey();
                Debug.Log("SecureTokenStore probe: " + (restored ? "ok" : "failed") + " provider=AndroidKeyStore alias=jaba_survivol_probe");
            }
            catch (Exception exception)
            {
                Debug.LogError("SecureTokenStore probe failed: " + exception);
            }
        }
#endif

        private Camera CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var cameraComponent = cameraObject.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 5.6f;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.015f, 0.022f, 0.038f);
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 100f;

            cameraObject.AddComponent<AspectCameraFitter>();
            return cameraComponent;
        }

        private void BuildArena(Transform parent)
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent, false);
            FloorRendererForTests = floor.AddComponent<SpriteRenderer>();
            FloorRendererForTests.sprite = ArtCatalog.Get("Environment/floor_tile");
            FloorRendererForTests.drawMode = SpriteDrawMode.Tiled;
            FloorRendererForTests.tileMode = SpriteTileMode.Continuous;
            FloorRendererForTests.size = new Vector2(
                GameBalance.ArenaHalfWidth * 2f,
                GameBalance.ArenaHalfHeight * 2f);
            FloorRendererForTests.sortingOrder = SortingOrders.Floor;

            AddWall(
                parent,
                "Arena Wall Top",
                new Vector3(0f, GameBalance.ArenaHalfHeight, 0f),
                new Vector2((GameBalance.ArenaHalfWidth * 2f) + 0.4f, 0.25f),
                "Environment/wall_horizontal");
            AddWall(
                parent,
                "Arena Wall Bottom",
                new Vector3(0f, -GameBalance.ArenaHalfHeight, 0f),
                new Vector2((GameBalance.ArenaHalfWidth * 2f) + 0.4f, 0.25f),
                "Environment/wall_horizontal");
            AddWall(
                parent,
                "Arena Wall Left",
                new Vector3(-GameBalance.ArenaHalfWidth, 0f, 0f),
                new Vector2(0.25f, (GameBalance.ArenaHalfHeight * 2f) + 0.4f),
                "Environment/wall_vertical");
            AddWall(
                parent,
                "Arena Wall Right",
                new Vector3(GameBalance.ArenaHalfWidth, 0f, 0f),
                new Vector2(0.25f, (GameBalance.ArenaHalfHeight * 2f) + 0.4f),
                "Environment/wall_vertical");
            ArenaWallCountForTests = 4;

            var props = new[]
            {
                ("Prop Barrel Left", "Environment/prop_barrel", new Vector2(-6.4f, -3.2f), 0.78f),
                ("Prop Crate Right", "Environment/prop_crate", new Vector2(6.2f, 3.1f), 0.74f),
                ("Prop Debris Left", "Environment/prop_debris", new Vector2(-6.0f, 2.9f), 0.7f),
                ("Prop Barrel Right", "Environment/prop_barrel", new Vector2(6.4f, -3.0f), 0.7f),
                ("Prop Crate Top", "Environment/prop_crate", new Vector2(2.4f, 3.6f), 0.62f),
                ("Prop Debris Bottom", "Environment/prop_debris", new Vector2(-2.6f, -3.7f), 0.6f)
            };

            foreach (var (name, sprite, position, scale) in props)
            {
                AddProp(parent, name, sprite, position, scale);
            }

            ArenaPropCountForTests = props.Length;
        }

        private static void AddWall(
            Transform parent,
            string name,
            Vector3 position,
            Vector2 size,
            string spritePath)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = SortingOrders.WallsAndProps;
        }

        private static void AddProp(
            Transform parent,
            string name,
            string spritePath,
            Vector2 position,
            float scale)
        {
            var prop = new GameObject(name);
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = position;
            prop.transform.localScale = Vector3.one * scale;
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.sortingOrder = SortingOrders.WallsAndProps + 1;
        }
    }
}
