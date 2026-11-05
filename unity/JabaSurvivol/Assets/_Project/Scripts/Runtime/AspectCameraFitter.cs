using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Fits the orthographic camera to the arena on both phone and tablet
    /// aspect ratios.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AspectCameraFitter : MonoBehaviour
    {
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            var aspect = Mathf.Max(0.1f, _camera.aspect);
            _camera.orthographicSize = Mathf.Max(
                GameBalance.ArenaHalfHeight + 0.6f,
                (GameBalance.ArenaHalfWidth + 0.6f) / aspect);
        }
    }
}
