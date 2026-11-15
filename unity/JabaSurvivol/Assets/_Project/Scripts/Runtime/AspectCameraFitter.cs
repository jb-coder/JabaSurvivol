using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Keeps a readable camera height, follows the player, and clamps the view
    /// to the extended arena on phone and tablet aspect ratios.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AspectCameraFitter : MonoBehaviour
    {
        private Camera _camera;
        private Transform _target;
        private Vector3 _velocity;

        public bool HasTarget => _target != null;

        public Vector2 ViewHalfExtents
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponent<Camera>();
                }

                var halfHeight = Mathf.Max(0.1f, _camera.orthographicSize);
                return new Vector2(halfHeight * Mathf.Max(0.1f, _camera.aspect), halfHeight);
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographicSize = GameBalance.ArenaCameraHalfHeight;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _camera.orthographicSize = GameBalance.ArenaCameraHalfHeight;
            var halfExtents = ViewHalfExtents;
            var targetPosition = TargetPosition();
            var clamped = ClampToArena(targetPosition, halfExtents.x, halfExtents.y);
            transform.position = new Vector3(clamped.x, clamped.y, -10f);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _camera.orthographicSize = GameBalance.ArenaCameraHalfHeight;
            var halfExtents = ViewHalfExtents;
            var targetPosition = TargetPosition();
            var desired = ClampToArena(targetPosition, halfExtents.x, halfExtents.y);
            var desiredPosition = new Vector3(desired.x, desired.y, -10f);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                GameBalance.ArenaCameraSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Clamps a camera target so the view stays inside the arena. If the
        /// view is wider or taller than the arena, that axis is centered.
        /// </summary>
        public static Vector2 ClampToArena(Vector2 target, float halfWidth, float halfHeight)
        {
            var x = halfWidth >= GameBalance.ArenaHalfWidth
                ? 0f
                : Mathf.Clamp(
                    target.x,
                    -GameBalance.ArenaHalfWidth + halfWidth,
                    GameBalance.ArenaHalfWidth - halfWidth);
            var y = halfHeight >= GameBalance.ArenaHalfHeight
                ? 0f
                : Mathf.Clamp(
                    target.y,
                    -GameBalance.ArenaHalfHeight + halfHeight,
                    GameBalance.ArenaHalfHeight - halfHeight);
            return new Vector2(x, y);
        }

        private Vector3 TargetPosition()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                return Vector3.zero;
            }

            return _target.position;
        }
    }
}
