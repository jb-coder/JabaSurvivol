using System.IO;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using UnityEditor;
using UnityEngine;

namespace JabaSurvivol.Editor
{
    /// <summary>
    /// Captures a deterministic rig lineup for visual review and art QA.
    /// </summary>
    public static class VisualShowcase
    {
        private const string OutputPath = "artifacts/visuals/v070-rig-showcase.png";

        [MenuItem("JabaSurvivol/Capture Visual Showcase")]
        public static void Capture()
        {
            var root = new GameObject("Visual Showcase");
            var cameraObject = new GameObject("Showcase Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.022f, 0.038f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            try
            {
                AddPlayer(root.transform, new Vector3(-5.1f, 0f, 0f));
                AddZombie(root.transform, ZombieArchetype.Walker, new Vector3(-3.1f, 0f, 0f), elite: false);
                AddZombie(root.transform, ZombieArchetype.Runner, new Vector3(-1.1f, 0f, 0f), elite: true);
                AddZombie(root.transform, ZombieArchetype.Brute, new Vector3(0.9f, 0f, 0f), elite: false);
                AddBoss(root.transform, new Vector3(3.1f, 0f, 0f));
                AddTurret(root.transform, new Vector3(5.1f, 0f, 0f));

                var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
                texture.Apply();

                var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
                var repositoryRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", ".."));
                var output = Path.Combine(repositoryRoot, OutputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.WriteAllBytes(output, texture.EncodeToPNG());
                Debug.Log($"Visual showcase written to {output}.");

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void AddPlayer(Transform parent, Vector3 position)
        {
            var holder = new GameObject("Player");
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            var visual = CharacterVisual.CreatePlayer(holder.transform);
            visual.SetWeapon(ArtCatalog.Get("Weapons/pistol"));
            visual.SetAim(Vector2.right);
            visual.Tick(0.016f);
        }

        private static void AddZombie(Transform parent, ZombieArchetype archetype, Vector3 position, bool elite)
        {
            var holder = new GameObject($"Zombie {archetype}");
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            var visual = CharacterVisual.CreateZombie(holder.transform, archetype);
            visual.SetElite(elite);
            visual.SetAim(Vector2.right);
            visual.SetMotion(Vector2.right, 1f);
            visual.Tick(0.016f);
        }

        private static void AddBoss(Transform parent, Vector3 position)
        {
            var holder = new GameObject("Warden");
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            holder.transform.localScale = Vector3.one * 1.4f;
            var visual = CharacterVisual.CreateBoss(holder.transform);
            visual.SetAim(Vector2.right);
            visual.SetEnraged(false);
            visual.Tick(0.016f);
        }

        private static void AddTurret(Transform parent, Vector3 position)
        {
            var holder = new GameObject("Turret");
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            var visual = CharacterVisual.CreateTurret(holder.transform);
            visual.SetAim(Vector2.right);
            visual.Tick(0.016f);
        }
    }
}
