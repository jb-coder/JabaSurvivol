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
        public ZombieActor(ZombieArchetype archetype, GameObject gameObject, SpriteRenderer renderer, GameObject eliteRing)
        {
            Archetype = archetype;
            GameObject = gameObject;
            Renderer = renderer;
            EliteRing = eliteRing;
        }

        public ZombieArchetype Archetype { get; }

        public GameObject GameObject { get; }

        public SpriteRenderer Renderer { get; }

        public GameObject EliteRing { get; }

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
            actor.Renderer.color = ColorFor(archetype, isElite);
            actor.EliteRing.SetActive(isElite);
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
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.Circle;
            renderer.color = ColorFor(archetype, false);
            renderer.sortingOrder = 15;

            var eliteRing = new GameObject("Elite Ring");
            eliteRing.transform.SetParent(gameObject.transform, false);
            eliteRing.transform.localScale = Vector3.one * 1.45f;
            var ringRenderer = eliteRing.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = SpriteFactory.Ring;
            ringRenderer.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            ringRenderer.sortingOrder = 16;
            eliteRing.SetActive(false);

            gameObject.SetActive(false);
            return new ZombieActor(archetype, gameObject, renderer, eliteRing);
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
        private readonly float[] _remaining;
        private readonly float[] _startScale;
        private readonly Color[] _colors;

        public DeathEffectPool(Transform parent)
        {
            _objects = new GameObject[PoolSize];
            _renderers = new SpriteRenderer[PoolSize];
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
                _objects[i] = effect;
                _renderers[i] = renderer;
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
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _objects.Length; i++)
            {
                _objects[i].SetActive(false);
            }
        }
    }
}
