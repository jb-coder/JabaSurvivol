using System;
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

        public static GameBootstrap Instance { get; private set; }

        private Transform _worldRoot;
        private Camera _camera;

        public GameSession Session { get; private set; }

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
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Session?.SaveNow();
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

        private static void BuildArena(Transform parent)
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent, false);
            floor.transform.localScale = new Vector3(
                GameBalance.ArenaHalfWidth * 2f,
                GameBalance.ArenaHalfHeight * 2f,
                1f);
            var floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = SpriteFactory.Square;
            floorRenderer.color = new Color(0.04f, 0.055f, 0.085f);
            floorRenderer.sortingOrder = -30;

            for (var x = -8; x <= 8; x += 2)
            {
                AddLine(parent, $"Grid X {x}", new Vector3(x, 0f, 0f), new Vector3(0.028f, GameBalance.ArenaHalfHeight * 2f, 1f));
            }

            for (var y = -4; y <= 4; y += 2)
            {
                AddLine(parent, $"Grid Y {y}", new Vector3(0f, y, 0f), new Vector3(GameBalance.ArenaHalfWidth * 2f, 0.028f, 1f));
            }

            AddBorder(parent, "Border Top", new Vector3(0f, GameBalance.ArenaHalfHeight, 0f), new Vector3((GameBalance.ArenaHalfWidth * 2f) + 0.12f, 0.09f, 1f));
            AddBorder(parent, "Border Bottom", new Vector3(0f, -GameBalance.ArenaHalfHeight, 0f), new Vector3((GameBalance.ArenaHalfWidth * 2f) + 0.12f, 0.09f, 1f));
            AddBorder(parent, "Border Left", new Vector3(-GameBalance.ArenaHalfWidth, 0f, 0f), new Vector3(0.09f, (GameBalance.ArenaHalfHeight * 2f) + 0.12f, 1f));
            AddBorder(parent, "Border Right", new Vector3(GameBalance.ArenaHalfWidth, 0f, 0f), new Vector3(0.09f, (GameBalance.ArenaHalfHeight * 2f) + 0.12f, 1f));
        }

        private static void AddLine(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            line.transform.localPosition = position;
            line.transform.localScale = scale;
            var renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Square;
            renderer.color = new Color(0.07f, 0.095f, 0.14f);
            renderer.sortingOrder = -25;
        }

        private static void AddBorder(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var border = new GameObject(name);
            border.transform.SetParent(parent, false);
            border.transform.localPosition = position;
            border.transform.localScale = scale;
            var renderer = border.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Square;
            renderer.color = new Color(0.1f, 0.42f, 0.55f);
            renderer.sortingOrder = -20;
        }
    }
}
