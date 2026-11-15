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

        public int ArenaDecalCountForTests { get; private set; }

        public int ArenaLightPoolCountForTests { get; private set; }

        public Camera CameraForTests => _camera;

        public AspectCameraFitter CameraFitterForTests =>
            _camera != null ? _camera.GetComponent<AspectCameraFitter>() : null;

        public VisualPolish VisualPolishForTests { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            VisualPolishForTests = gameObject.AddComponent<VisualPolish>();
            ApiConfig.Initialize();
#if DEVELOPMENT_BUILD
            SeedDevelopmentSession();
            RunSecureStorageProbe();
#endif

            var runtimeRoot = new GameObject("Runtime").transform;
            runtimeRoot.SetParent(transform, false);

            _camera = CreateCamera(runtimeRoot);
            VisualPolishForTests.ConfigureCamera(_camera);
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
            _camera.GetComponent<AspectCameraFitter>()?.SetTarget(Session.PlayerTransform);
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
            FloorRendererForTests.sprite = ArtCatalog.Get("Environment/floor_asphalt_a");
            FloorRendererForTests.drawMode = SpriteDrawMode.Tiled;
            FloorRendererForTests.tileMode = SpriteTileMode.Continuous;
            FloorRendererForTests.size = new Vector2(
                GameBalance.ArenaHalfWidth * 2f,
                GameBalance.ArenaHalfHeight * 2f);
            FloorRendererForTests.sortingOrder = SortingOrders.Floor;

            var floorOverlay = new GameObject("Floor Overlay");
            floorOverlay.transform.SetParent(parent, false);
            var overlayRenderer = floorOverlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = ArtCatalog.Get("Environment/floor_concrete");
            overlayRenderer.drawMode = SpriteDrawMode.Tiled;
            overlayRenderer.tileMode = SpriteTileMode.Continuous;
            overlayRenderer.size = new Vector2(
                GameBalance.ArenaHalfWidth * 2f,
                GameBalance.ArenaHalfHeight * 2f);
            overlayRenderer.color = new Color(1f, 1f, 1f, 0.12f);
            overlayRenderer.sortingOrder = SortingOrders.FloorGrid;

            var decals = new[]
            {
                ("Decal Road Line Left", "Environment/decal_road_line", new Vector2(-8f, 0f), 1.2f, 0f),
                ("Decal Road Line Right", "Environment/decal_road_line", new Vector2(6f, 0f), 1.2f, 0f),
                ("Decal Road Arrow", "Environment/decal_road_arrow", new Vector2(-14f, 6f), 1f, 20f),
                ("Decal Road Arrow South", "Environment/decal_road_arrow", new Vector2(12f, -6f), 1f, 200f),
                ("Decal Crack", "Environment/decal_crack", new Vector2(-2f, -4f), 1.1f, 15f),
                ("Decal Oil", "Environment/decal_oil", new Vector2(4f, 5f), 1.4f, 0f),
                ("Decal Puddle", "Environment/decal_puddle", new Vector2(-5f, 7f), 1.3f, 0f),
                ("Decal Manhole", "Environment/decal_manhole", new Vector2(10f, 2f), 0.9f, 0f),
                ("Decal Blood", "Environment/decal_blood", new Vector2(-12f, -7f), 0.8f, 0f),
                ("Decal Graffiti", "Environment/decal_graffiti", new Vector2(15f, 8f), 1.1f, 0f),
                ("Decal Paper", "Environment/decal_paper", new Vector2(-16f, -2f), 0.7f, 40f),
                ("Decal Tire Marks", "Environment/decal_tire_marks", new Vector2(0f, 9f), 1.1f, -20f)
            };
            foreach (var (name, sprite, position, scale, rotation) in decals)
            {
                AddDecal(parent, name, sprite, position, scale, rotation);
            }

            var lights = new[]
            {
                ("Light Cyan", "Environment/light_cyan", new Vector2(-14f, 6f), 2.2f),
                ("Light Magenta", "Environment/light_magenta", new Vector2(14f, 5f), 2.4f),
                ("Light Amber", "Environment/light_amber", new Vector2(-15f, -6f), 1.8f),
                ("Light Green", "Environment/light_green", new Vector2(12f, -7f), 1.9f)
            };
            foreach (var (name, sprite, position, scale) in lights)
            {
                AddLightPool(parent, name, sprite, position, scale);
            }

            var propsBehind = new[]
            {
                ("Prop Car", "Environment/prop_car", new Vector2(-12f, -5f), 1f, 10f),
                ("Prop Dumpster", "Environment/prop_dumpster", new Vector2(13f, 6f), 1f, 0f),
                ("Prop Streetlight", "Environment/prop_streetlight", new Vector2(16f, 0f), 0.9f, 0f),
                ("Prop Neon Sign", "Environment/prop_neon_sign", new Vector2(-17f, 0f), 0.9f, 0f),
                ("Prop Barrel North", "Environment/prop_barrel", new Vector2(-9f, 8f), 0.8f, 0f),
                ("Prop Crate North", "Environment/prop_crate", new Vector2(9f, 8f), 0.8f, 0f),
                ("Prop Debris West", "Environment/prop_debris", new Vector2(-16f, -3f), 0.9f, 25f),
                ("Prop Cables", "Environment/prop_cables", new Vector2(2f, 9f), 1.2f, 0f)
            };
            foreach (var (name, sprite, position, scale, rotation) in propsBehind)
            {
                AddProp(parent, name, sprite, position, scale, rotation, SortingOrders.PropsBehind);
            }

            var propsFront = new[]
            {
                ("Prop Barricade", "Environment/prop_barricade", new Vector2(-6f, 3f), 1f, 0f),
                ("Prop Cone A", "Environment/prop_cone", new Vector2(5f, 2f), 1f, 0f),
                ("Prop Cone B", "Environment/prop_cone", new Vector2(6f, 3f), 0.8f, 0f),
                ("Prop Sandbags", "Environment/prop_sandbags", new Vector2(-8f, -2f), 1.1f, 0f),
                ("Prop Barrel South", "Environment/prop_barrel", new Vector2(11f, -3f), 0.8f, 0f),
                ("Prop Crate South", "Environment/prop_crate", new Vector2(-3f, -8f), 0.8f, 0f)
            };
            foreach (var (name, sprite, position, scale, rotation) in propsFront)
            {
                AddProp(parent, name, sprite, position, scale, rotation, SortingOrders.PropsFront);
            }

            var perimeter = new[]
            {
                ("Perimeter Fence North", "Environment/wall_fence_h", new Vector3(0f, GameBalance.ArenaHalfHeight, 0f), new Vector2((GameBalance.ArenaHalfWidth * 2f) + 0.6f, 0.6f)),
                ("Perimeter Fence South", "Environment/wall_fence_h", new Vector3(0f, -GameBalance.ArenaHalfHeight, 0f), new Vector2((GameBalance.ArenaHalfWidth * 2f) + 0.6f, 0.6f)),
                ("Perimeter Fence West", "Environment/wall_fence_v", new Vector3(-GameBalance.ArenaHalfWidth, 0f, 0f), new Vector2(0.6f, (GameBalance.ArenaHalfHeight * 2f) + 0.6f)),
                ("Perimeter Fence East", "Environment/wall_fence_v", new Vector3(GameBalance.ArenaHalfWidth, 0f, 0f), new Vector2(0.6f, (GameBalance.ArenaHalfHeight * 2f) + 0.6f)),
                ("Perimeter Barrier North", "Environment/wall_concrete_h", new Vector3(-9f, GameBalance.ArenaHalfHeight - 0.4f, 0f), new Vector2(14f, 0.5f)),
                ("Perimeter Barrier South", "Environment/wall_concrete_h", new Vector3(9f, -GameBalance.ArenaHalfHeight + 0.4f, 0f), new Vector2(14f, 0.5f)),
                ("Perimeter Barrier West", "Environment/wall_concrete_v", new Vector3(-GameBalance.ArenaHalfWidth + 0.4f, -5f, 0f), new Vector2(0.5f, 8f)),
                ("Perimeter Barrier East", "Environment/wall_concrete_v", new Vector3(GameBalance.ArenaHalfWidth - 0.4f, 5f, 0f), new Vector2(0.5f, 8f))
            };
            foreach (var (name, sprite, position, size) in perimeter)
            {
                AddTiled(parent, name, sprite, position, size, SortingOrders.Perimeter);
            }
            ArenaWallCountForTests = perimeter.Length;

            AddProp(parent, "Storefront North", "Environment/wall_storefront", new Vector2(-10f, 9.2f), 1.2f, 0f, SortingOrders.Perimeter - 1);
            AddProp(parent, "Storefront South", "Environment/wall_storefront", new Vector2(10f, -9.2f), 1.2f, 180f, SortingOrders.Perimeter - 1);

            ArenaDecalCountForTests = decals.Length;
            ArenaLightPoolCountForTests = lights.Length;
            ArenaPropCountForTests = propsBehind.Length + propsFront.Length + 2;
        }

        private static void AddTiled(
            Transform parent,
            string name,
            string spritePath,
            Vector3 position,
            Vector2 size,
            int sortingOrder)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;
        }

        private static void AddDecal(
            Transform parent,
            string name,
            string spritePath,
            Vector2 position,
            float scale,
            float rotation)
        {
            var decal = new GameObject(name);
            decal.transform.SetParent(parent, false);
            decal.transform.localPosition = position;
            decal.transform.localScale = Vector3.one * scale;
            decal.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var renderer = decal.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.sortingOrder = SortingOrders.FloorDecal;
        }

        private static void AddLightPool(
            Transform parent,
            string name,
            string spritePath,
            Vector2 position,
            float scale)
        {
            var light = new GameObject(name);
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.transform.localScale = Vector3.one * scale;
            var renderer = light.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.sortingOrder = SortingOrders.LightPool;
        }

        private static void AddProp(
            Transform parent,
            string name,
            string spritePath,
            Vector2 position,
            float scale,
            float rotation,
            int sortingOrder)
        {
            var prop = new GameObject(name);
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = position;
            prop.transform.localScale = Vector3.one * scale;
            prop.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(spritePath);
            renderer.sortingOrder = sortingOrder;

            if (name == "Prop Neon Sign")
            {
                VisualPolish.AddPointLight(prop, new Color(1f, 0.2f, 0.9f, 1f), 0.7f, 4f);
            }
            else if (name == "Prop Streetlight")
            {
                VisualPolish.AddPointLight(prop, new Color(0.4f, 0.9f, 1f, 1f), 0.8f, 5f);
            }
        }
    }
}
