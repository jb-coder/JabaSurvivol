using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Pooled projectiles with deterministic distance-based collision against a
    /// single target. Visual profiles are selected per shot without allocations.
    /// </summary>
    public sealed class BulletPool
    {
        private readonly GameObject[] _objects;
        private readonly SpriteRenderer[] _renderers;
        private readonly Vector2[] _positions;
        private readonly Vector2[] _directions;
        private readonly float[] _lifetimes;
        private readonly float[] _speeds;
        private readonly float[] _damages;
        private readonly Sprite[] _sprites;
        private readonly float[] _scales;
        private readonly ImpactEffectPool _impacts;

        public BulletPool(Transform parent, ImpactEffectPool impacts = null, int size = GameBalance.BulletPoolSize)
        {
            _objects = new GameObject[size];
            _renderers = new SpriteRenderer[size];
            _positions = new Vector2[size];
            _directions = new Vector2[size];
            _lifetimes = new float[size];
            _speeds = new float[size];
            _damages = new float[size];
            _sprites = new Sprite[6];
            _scales = new float[6];
            _impacts = impacts;

            LoadVisual(BulletStyle.Pistol, WeaponVisualCatalog.Get(WeaponId.Pistol));
            LoadVisual(BulletStyle.Smg, WeaponVisualCatalog.Get(WeaponId.Smg));
            LoadVisual(BulletStyle.Shotgun, WeaponVisualCatalog.Get(WeaponId.Shotgun));
            LoadVisual(BulletStyle.Rifle, WeaponVisualCatalog.Get(WeaponId.Rifle));
            LoadVisual(BulletStyle.Lmg, WeaponVisualCatalog.Get(WeaponId.Lmg));
            LoadVisual(BulletStyle.Turret, WeaponVisualCatalog.Turret);

            for (var i = 0; i < size; i++)
            {
                var bullet = new GameObject($"Bullet {i}");
                bullet.transform.SetParent(parent, false);
                var renderer = bullet.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrders.Projectile;
                bullet.SetActive(false);
                _objects[i] = bullet;
                _renderers[i] = renderer;
            }
        }

        public int ActiveCount { get; private set; }

        public void Spawn(
            Vector2 position,
            Vector2 direction,
            float speed,
            float lifetime,
            float damage,
            BulletStyle style = BulletStyle.Pistol)
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

                var styleIndex = (int)style;
                _renderers[i].sprite = _sprites[styleIndex];
                var scale = _scales[styleIndex];
                _objects[i].transform.position = position;
                _objects[i].transform.localScale = new Vector3(scale, scale, 1f);
                _objects[i].transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(_directions[i].y, _directions[i].x) * Mathf.Rad2Deg);
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
                    _impacts?.Spawn(_positions[i], _directions[i], 1f);
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

        public int FirstActiveIndexForTests()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    return i;
                }
            }

            return -1;
        }

        public Sprite SpriteForTests(int index)
        {
            return index >= 0 && index < _renderers.Length ? _renderers[index].sprite : null;
        }

        public Quaternion RotationForTests(int index)
        {
            return index >= 0 && index < _objects.Length
                ? _objects[index].transform.localRotation
                : Quaternion.identity;
        }

        private void LoadVisual(BulletStyle style, WeaponVisualProfile profile)
        {
            var index = (int)style;
            _sprites[index] = ArtCatalog.Get(profile.ProjectileSprite);
            _scales[index] = profile.ProjectileScale;
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