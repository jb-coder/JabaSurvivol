using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// One pooled zombie visual. Actors are reused per archetype so spawning
    /// and death do not allocate during a run.
    /// </summary>
    public sealed class ZombieActor
    {
        public ZombieActor(ZombieArchetype archetype, GameObject gameObject, CharacterVisual visual)
        {
            Archetype = archetype;
            GameObject = gameObject;
            Visual = visual;
        }

        public ZombieArchetype Archetype { get; }

        public GameObject GameObject { get; }

        public CharacterVisual Visual { get; }

        public bool Active => GameObject.activeSelf;
    }

    /// <summary>
    /// Pool of zombie actors, one reusable object per archetype. The boss uses
    /// the same pool once its archetype is added.
    /// </summary>
    public sealed class ZombieActorPool
    {
        private readonly ZombieActor[] _actors;

        public ZombieActorPool(Transform parent)
        {
            _actors = new ZombieActor[ZombieCatalog.All.Count];
            for (var i = 0; i < _actors.Length; i++)
            {
                var archetype = (ZombieArchetype)i;
                _actors[i] = Create(parent, archetype);
            }
        }

        public int Count => _actors.Length;

        public ZombieActor Rent(ZombieArchetype archetype, float visualScale, bool isElite)
        {
            var actor = _actors[(int)archetype];
            actor.GameObject.transform.localScale = Vector3.one * visualScale;
            actor.Visual.ResetVisual();
            actor.Visual.SetElite(isElite);
            actor.GameObject.SetActive(true);
            return actor;
        }

        public void Return(ZombieActor actor)
        {
            if (actor != null)
            {
                actor.GameObject.SetActive(false);
            }
        }

        public void ReturnAll()
        {
            for (var i = 0; i < _actors.Length; i++)
            {
                _actors[i].GameObject.SetActive(false);
            }
        }

        public static Color ColorFor(ZombieArchetype archetype, bool isElite)
        {
            var color = archetype switch
            {
                ZombieArchetype.Runner => new Color(0.95f, 0.55f, 0.2f),
                ZombieArchetype.Brute => new Color(0.62f, 0.28f, 0.78f),
                _ => new Color(0.86f, 0.25f, 0.3f)
            };

            if (isElite)
            {
                color = Color.Lerp(color, new Color(1f, 0.92f, 0.45f), 0.35f);
            }

            return color;
        }

        private static ZombieActor Create(Transform parent, ZombieArchetype archetype)
        {
            var gameObject = new GameObject($"Zombie {archetype}");
            gameObject.transform.SetParent(parent, false);
            var visual = CharacterVisual.CreateZombie(gameObject.transform, archetype);
            gameObject.SetActive(false);
            return new ZombieActor(archetype, gameObject, visual);
        }
    }

    /// <summary>
    /// Pooled expanding rings used for zombie death feedback.
    /// </summary>
    public sealed class DeathEffectPool
    {
        private const int PoolSize = 12;
        private const float Lifetime = 0.32f;

        private readonly GameObject[] _objects;
        private readonly SpriteRenderer[] _renderers;
        private readonly SpriteRenderer[] _burstRenderers;
        private readonly Transform[] _burstTransforms;
        private readonly Sprite _burst;
        private readonly float[] _remaining;
        private readonly float[] _startScale;
        private readonly Color[] _colors;

        public DeathEffectPool(Transform parent)
        {
            _objects = new GameObject[PoolSize];
            _renderers = new SpriteRenderer[PoolSize];
            _burstRenderers = new SpriteRenderer[PoolSize];
            _burstTransforms = new Transform[PoolSize];
            _burst = ArtCatalog.Get("Effects/death_burst");
            _remaining = new float[PoolSize];
            _startScale = new float[PoolSize];
            _colors = new Color[PoolSize];

            for (var i = 0; i < PoolSize; i++)
            {
                var effect = new GameObject($"Death Effect {i}");
                effect.transform.SetParent(parent, false);
                var renderer = effect.AddComponent<SpriteRenderer>();
                renderer.sprite = SpriteFactory.Ring;
                renderer.sortingOrder = 14;
                effect.SetActive(false);
                var burst = new GameObject("Burst");
                burst.transform.SetParent(effect.transform, false);
                var burstRenderer = burst.AddComponent<SpriteRenderer>();
                burstRenderer.sprite = _burst;
                burstRenderer.sortingOrder = 15;
                _objects[i] = effect;
                _renderers[i] = renderer;
                _burstRenderers[i] = burstRenderer;
                _burstTransforms[i] = burst.transform;
            }
        }

        public void Spawn(Vector2 position, Color color, float scale)
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                if (_objects[i].activeSelf)
                {
                    continue;
                }

                _objects[i].transform.position = position;
                _objects[i].transform.localScale = Vector3.one * scale;
                _renderers[i].color = color;
                _burstTransforms[i].localRotation = Quaternion.Euler(0f, 0f, i * 37f);
                _burstTransforms[i].localScale = Vector3.one * 0.9f;
                _burstRenderers[i].color = color;
                _remaining[i] = Lifetime;
                _startScale[i] = scale;
                _colors[i] = color;
                _objects[i].SetActive(true);
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
                    continue;
                }

                var progress = 1f - (_remaining[i] / Lifetime);
                _objects[i].transform.localScale = Vector3.one * (_startScale[i] * (1f + progress));
                var color = _colors[i];
                color.a = 1f - progress;
                _renderers[i].color = color;
                var burst = _burstRenderers[i].color;
                burst.a = 1f - progress;
                _burstRenderers[i].color = burst;
                _burstTransforms[i].localScale = Vector3.one * (0.9f + (progress * 0.7f));
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                _objects[i].SetActive(false);
            }
        }

        public int ActiveCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _objects.Length; i++)
                {
                    if (_objects[i].activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
