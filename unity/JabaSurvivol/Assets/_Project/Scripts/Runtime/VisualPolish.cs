using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Owns the runtime post-processing volume and the small 2D light budget.
    /// The setup is intentionally cheap and can be disabled as one unit.
    /// </summary>
    public sealed class VisualPolish : MonoBehaviour
    {
        public const int MaxActiveLights = 8;

        private VolumeProfile _profile;
        private GameObject _volumeObject;
        private GameObject _globalLightObject;

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
            Enabled = enabled;
            BloomActive = enabled && _profile != null;
            VignetteActive = enabled && _profile != null;
            if (_volumeObject != null)
            {
                _volumeObject.SetActive(enabled);
            }

            if (_globalLightObject != null)
            {
                _globalLightObject.SetActive(enabled);
            }
        }

        private void Awake()
        {
            CreateVolume();
            CreateGlobalLight();
            Configure(false);
        }

        private void CreateVolume()
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "JabaSurvivol Runtime Volume";

            var bloom = _profile.Add<Bloom>(true);
            bloom.intensity.Override(0.45f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);
            BloomActive = false;

            var vignette = _profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.35f);
            vignette.color.Override(new Color(0f, 0f, 0f, 1f));
            VignetteActive = false;

            _volumeObject = new GameObject("Runtime Post Process Volume");
            _volumeObject.transform.SetParent(transform, false);
            var volume = _volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.profile = _profile;
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
        private void OnDestroy()
        {
            if (_profile != null)
            {
                Destroy(_profile);
                _profile = null;
            }
        }
    }
}
