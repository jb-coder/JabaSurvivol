using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Pooled muzzle flashes. Each shot reuses one sprite and never allocates.
    /// </summary>
    public sealed class MuzzleFlashPool
    {
        private const int PoolSize = 12;
        private const float Lifetime = 0.07f;

        private readonly GameObject[] _objects;
        private readonly SpriteRenderer[] _renderers;
        private readonly float[] _remaining;
        private readonly Sprite _flash;
        private readonly Sprite _altFlash;

        public MuzzleFlashPool(Transform parent)
        {
            _objects = new GameObject[PoolSize];
            _renderers = new SpriteRenderer[PoolSize];
            _remaining = new float[PoolSize];
            _flash = ArtCatalog.Get("Effects/muzzle_flash");
            _altFlash = ArtCatalog.Get("Effects/muzzle_flash_alt");

            for (var i = 0; i < PoolSize; i++)
            {
                var effect = new GameObject($"Muzzle Flash {i}");
                effect.transform.SetParent(parent, false);
                var renderer = effect.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrders.MuzzleAndImpact;
                effect.SetActive(false);
                _objects[i] = effect;
                _renderers[i] = renderer;
            }
        }

        public int ActiveCount { get; private set; }

        public void Spawn(Vector2 position, float rotationDegrees, bool alt)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    continue;
                }

                _objects[i].transform.position = position;
                _objects[i].transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
                _objects[i].transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                _renderers[i].sprite = alt ? _altFlash : _flash;
                _renderers[i].color = Color.white;
                _remaining[i] = Lifetime;
                _objects[i].SetActive(true);
                ActiveCount++;
                return;
            }
        }

        public void Update(float deltaTime)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (!_objects[i].activeSelf)
                {
                    continue;
                }

                _remaining[i] -= deltaTime;
                if (_remaining[i] <= 0f)
                {
                    _objects[i].SetActive(false);
                    ActiveCount = Mathf.Max(0, ActiveCount - 1);
                    continue;
                }

                var progress = 1f - (_remaining[i] / Lifetime);
                var color = _renderers[i].color;
                color.a = 1f - progress;
                _renderers[i].color = color;
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                _objects[i].SetActive(false);
            }

            ActiveCount = 0;
        }
    }

    /// <summary>
    /// Pooled impact sparks used when a projectile reaches a target.
    /// </summary>
    public sealed class ImpactEffectPool
    {
        private const int PoolSize = 16;
        private const float Lifetime = 0.16f;

        private readonly GameObject[] _objects;
        private readonly SpriteRenderer[] _renderers;
        private readonly float[] _remaining;
        private readonly Sprite _sprite;

        public ImpactEffectPool(Transform parent)
        {
            _objects = new GameObject[PoolSize];
            _renderers = new SpriteRenderer[PoolSize];
            _remaining = new float[PoolSize];
            _sprite = ArtCatalog.Get("Effects/impact");

            for (var i = 0; i < PoolSize; i++)
            {
                var effect = new GameObject($"Impact {i}");
                effect.transform.SetParent(parent, false);
                var renderer = effect.AddComponent<SpriteRenderer>();
                renderer.sprite = _sprite;
                renderer.sortingOrder = SortingOrders.MuzzleAndImpact;
                effect.SetActive(false);
                _objects[i] = effect;
                _renderers[i] = renderer;
            }
        }

        public int ActiveCount { get; private set; }

        public void Spawn(Vector2 position, Vector2 direction, float scale)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    continue;
                }

                _objects[i].transform.position = position;
                _objects[i].transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                _objects[i].transform.localScale = new Vector3(scale, scale, 1f);
                _renderers[i].color = Color.white;
                _remaining[i] = Lifetime;
                _objects[i].SetActive(true);
                ActiveCount++;
                return;
            }
        }

        public void Update(float deltaTime)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (!_objects[i].activeSelf)
                {
                    continue;
                }

                _remaining[i] -= deltaTime;
                if (_remaining[i] <= 0f)
                {
                    _objects[i].SetActive(false);
                    ActiveCount = Mathf.Max(0, ActiveCount - 1);
                    continue;
                }

                var progress = 1f - (_remaining[i] / Lifetime);
                var color = _renderers[i].color;
                color.a = 1f - progress;
                _renderers[i].color = color;
                _objects[i].transform.localScale = Vector3.one * (0.5f + (progress * 0.45f));
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                _objects[i].SetActive(false);
            }

            ActiveCount = 0;
        }
    }
}
