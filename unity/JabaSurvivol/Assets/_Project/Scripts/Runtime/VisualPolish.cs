using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Owns the color-safe bloom and vignette settings plus the small 2D light
    /// budget. The custom full-screen renderer feature is used instead of URP
    /// camera post-processing because the integrated color-grading path is not
    /// reliable on Android GLES.
    /// </summary>
    public sealed class VisualPolish : MonoBehaviour
    {
        public const int MaxActiveLights = 8;
        public const float DefaultBloomIntensity = 0.22f;
        public const float DefaultBloomThreshold = 0.6f;
        public const float DefaultBloomRadius = 3f;
        public const float DefaultVignetteIntensity = 0.22f;
        public const float DefaultVignetteSmoothness = 0.6f;

        private static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        private static readonly int BloomRadiusId = Shader.PropertyToID("_BloomRadius");
        private static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int VignetteSmoothnessId = Shader.PropertyToID("_VignetteSmoothness");

        private GameObject _globalLightObject;

        public static bool PostProcessingActive { get; private set; }

        public bool Enabled { get; private set; }

        public bool BloomActive { get; private set; }

        public bool VignetteActive { get; private set; }

        public Light2D GlobalLight { get; private set; }

        public int StaticLightCount { get; private set; }

        public void ConfigureCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
        }

        public void Configure(bool enabled)
        {
            var active = enabled &&
                ShouldEnablePostProcessing(
                    SystemInfo.processorCount,
                    SystemInfo.systemMemorySize,
                    QualitySettings.GetQualityLevel() >= 2);

            Enabled = enabled;
            PostProcessingActive = active;
            BloomActive = active;
            VignetteActive = active;
            ApplyShaderSettings(active);

            if (_globalLightObject != null)
            {
                _globalLightObject.SetActive(enabled);
            }
        }

        /// <summary>
        /// Chooses whether the color-safe post-processing feature should run.
        /// Very small or low-memory devices keep the unprocessed neon path.
        /// </summary>
        public static bool ShouldEnablePostProcessing(
            int processorCount,
            int systemMemorySizeMegabytes,
            bool highQuality)
        {
            return highQuality &&
                processorCount > 2 &&
                systemMemorySizeMegabytes >= 1536;
        }

        private void Awake()
        {
            CreateGlobalLight();
            Configure(false);
        }

        private static void ApplyShaderSettings(bool active)
        {
            Shader.SetGlobalFloat(
                BloomIntensityId,
                active ? DefaultBloomIntensity : 0f);
            Shader.SetGlobalFloat(BloomThresholdId, DefaultBloomThreshold);
            Shader.SetGlobalFloat(BloomRadiusId, DefaultBloomRadius);
            Shader.SetGlobalFloat(
                VignetteIntensityId,
                active ? DefaultVignetteIntensity : 0f);
            Shader.SetGlobalFloat(VignetteSmoothnessId, DefaultVignetteSmoothness);
        }

        private void CreateGlobalLight()
        {
            _globalLightObject = new GameObject("Global Light 2D");
            _globalLightObject.transform.SetParent(transform, false);
            GlobalLight = _globalLightObject.AddComponent<Light2D>();
            GlobalLight.lightType = Light2D.LightType.Global;
            GlobalLight.color = Color.white;
            GlobalLight.intensity = 0f;
            GlobalLight.blendStyleIndex = 0;
        }

        public static Light2D AddPointLight(
            GameObject target,
            Color color,
            float intensity,
            float outerRadius,
            float innerRadius = 0.2f)
        {
            if (target == null)
            {
                return null;
            }

            var light = target.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = outerRadius;
            light.pointLightInnerRadius = innerRadius;
            light.blendStyleIndex = 0;
            return light;
        }
    }
}
