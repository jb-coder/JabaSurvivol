using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Pooled projectiles with deterministic distance-based collision against a
    /// single target.
    /// </summary>
    public sealed class BulletPool
    {
        private readonly GameObject[] _objects;
        private readonly Vector2[] _positions;
        private readonly Vector2[] _directions;
        private readonly float[] _lifetimes;
        private readonly float[] _speeds;
        private readonly float[] _damages;

        public BulletPool(Transform parent, int size = GameBalance.BulletPoolSize)
        {
            _objects = new GameObject[size];
            _positions = new Vector2[size];
            _directions = new Vector2[size];
            _lifetimes = new float[size];
            _speeds = new float[size];
            _damages = new float[size];

            for (var i = 0; i < size; i++)
            {
                var bullet = new GameObject($"Bullet {i}");
                bullet.transform.SetParent(parent, false);
                bullet.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
                var renderer = bullet.AddComponent<SpriteRenderer>();
                renderer.sprite = SpriteFactory.Circle;
                renderer.color = new Color(1f, 0.87f, 0.35f);
                renderer.sortingOrder = 30;
                bullet.SetActive(false);
                _objects[i] = bullet;
            }
        }

        public int ActiveCount { get; private set; }

        public void Spawn(Vector2 position, Vector2 direction, float speed, float lifetime, float damage)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    continue;
                }

                _positions[i] = position;
                _directions[i] = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
                _lifetimes[i] = lifetime > 0f ? lifetime : GameBalance.BulletLifetime;
                _speeds[i] = speed > 0f ? speed : GameBalance.BulletSpeed;
                _damages[i] = Mathf.Max(0f, damage);
                _objects[i].transform.position = position;
                _objects[i].SetActive(true);
                ActiveCount++;
                return;
            }
        }

        /// <summary>
        /// Moves active bullets and returns the total damage of every hit in
        /// this frame. Multiple pellets may hit the same target in one frame.
        /// </summary>
        public float UpdateAndCheck(
            float deltaTime,
            bool hasTarget,
            Vector2 targetPosition,
            float targetRadius)
        {
            var totalDamage = 0f;

            for (var i = 0; i < _objects.Length; i++)
            {
                if (!_objects[i].activeSelf)
                {
                    continue;
                }

                _lifetimes[i] -= deltaTime;
                if (_lifetimes[i] <= 0f)
                {
                    Deactivate(i);
                    continue;
                }

                _positions[i] += _directions[i] * (_speeds[i] * deltaTime);
                _objects[i].transform.position = _positions[i];

                if (hasTarget && CombatMath.CirclesOverlap(
                        _positions[i].x,
                        _positions[i].y,
                        0.1f,
                        targetPosition.x,
                        targetPosition.y,
                        targetRadius))
                {
                    totalDamage += _damages[i];
                    Deactivate(i);
                }
            }

            return totalDamage;
        }

        public void Clear()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    Deactivate(i);
                }
            }
        }

        private void Deactivate(int index)
        {
            if (!_objects[index].activeSelf)
            {
                return;
            }

            _objects[index].SetActive(false);
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }
    }
}
