using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Modular paper-doll rig for the player, zombies, the Warden, and the
    /// turret. Parts are authored sprites; walk, aim, recoil, and enrage are
    /// animated procedurally without per-frame allocations.
    /// </summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        private enum VisualKind
        {
            Player,
            Enemy,
            Boss,
            Turret
        }

        private Transform _legs;
        private Transform _legLeft;
        private Transform _legRight;
        private Transform _body;
        private Transform _torso;
        private Transform _head;
        private Transform _armLeft;
        private Transform _armRight;
        private Transform _weaponMount;
        private Transform _core;
        private SpriteRenderer _weaponRenderer;
        private SpriteRenderer _auraRenderer;
        private SpriteRenderer _coreRenderer;
        private Vector3 _legLeftBase;
        private Vector3 _legRightBase;
        private Vector3 _torsoBase;
        private Vector3 _headBase;
        private Vector3 _armLeftBase;
        private Vector3 _armRightBase;
        private Vector3 _weaponBase;
        private Vector3 _coreBaseScale;
        private Color _coreColor;
        private VisualKind _kind;
        private float _partScale = 1f;
        private float _walkPhase;
        private float _speed;
        private float _targetSpeed;
        private float _recoil;
        private float _time;
        private Vector2 _aim = Vector2.right;
        private Vector2 _move = Vector2.right;
        private int _bodyBaseOrder;
        private bool _elite;
        private bool _enraged;

        public Transform AimPivot => _body;

        public Transform WeaponMount => _weaponMount;

        public bool IsElite => _elite;

        public bool IsEnraged => _enraged;

        public bool EliteAuraVisible => _auraRenderer != null && _auraRenderer.gameObject.activeSelf;

        public SpriteRenderer[] Renderers { get; private set; } = System.Array.Empty<SpriteRenderer>();

        public static CharacterVisual CreatePlayer(Transform parent)
        {
            return Create(parent, "player", VisualKind.Player);
        }

        public static CharacterVisual CreateZombie(Transform parent, ZombieArchetype archetype)
        {
            var key = archetype switch
            {
                ZombieArchetype.Runner => "zombie_runner",
                ZombieArchetype.Brute => "zombie_brute",
                _ => "zombie_walker"
            };

            return Create(parent, key, VisualKind.Enemy);
        }

        public static CharacterVisual CreateBoss(Transform parent)
        {
            return Create(parent, "boss_warden", VisualKind.Boss);
        }

        public static CharacterVisual CreateTurret(Transform parent)
        {
            return Create(parent, "turret", VisualKind.Turret);
        }

        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                _aim = direction.normalized;
            }
        }

        public void SetMotion(Vector2 localDirection, float speed01)
        {
            if (localDirection.sqrMagnitude > 0.0001f)
            {
                _move = localDirection.normalized;
            }

            _targetSpeed = Mathf.Clamp01(speed01);
        }

        public void SetRecoil(float amount)
        {
            _recoil = Mathf.Max(_recoil, Mathf.Clamp01(amount));
        }

        public void SetElite(bool isElite)
        {
            _elite = isElite;
            if (_auraRenderer != null)
            {
                _auraRenderer.gameObject.SetActive(isElite);
            }
        }

        public void SetEnraged(bool isEnraged)
        {
            _enraged = isEnraged;
            if (_coreRenderer != null)
            {
                _coreColor = isEnraged
                    ? new Color(1f, 0.18f, 0.24f, 1f)
                    : new Color(1f, 0.84f, 0.32f, 1f);
            }
        }

        public void SetWeapon(Sprite sprite)
        {
            if (_weaponRenderer == null || sprite == null)
            {
                return;
            }

            _weaponRenderer.sprite = sprite;
        }

        public void ResetVisual()
        {
            _walkPhase = 0f;
            _speed = 0f;
            _targetSpeed = 0f;
            _recoil = 0f;
            _aim = Vector2.right;
            _move = Vector2.right;
            _enraged = false;
            _elite = false;
            if (_auraRenderer != null)
            {
                _auraRenderer.gameObject.SetActive(false);
            }

            if (_legLeft != null)
            {
                _legLeft.localRotation = Quaternion.identity;
                _legRight.localRotation = Quaternion.identity;
                _legLeft.localPosition = _legLeftBase;
                _legRight.localPosition = _legRightBase;
            }

            if (_body != null)
            {
                _body.localRotation = Quaternion.identity;
            }

            if (_legs != null)
            {
                _legs.localRotation = Quaternion.identity;
            }
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _time += deltaTime;
            _speed = Mathf.MoveTowards(_speed, _targetSpeed, deltaTime * 6f);
            if (_speed > 0.01f)
            {
                _walkPhase += deltaTime * (4f + (8f * _speed));
            }

            var swing = Mathf.Sin(_walkPhase) * 26f * _speed;
            if (_legLeft != null)
            {
                _legLeft.localRotation = Quaternion.Euler(0f, 0f, swing);
                _legRight.localRotation = Quaternion.Euler(0f, 0f, -swing);
                var lift = Mathf.Abs(Mathf.Sin(_walkPhase)) * 0.025f * _speed;
                _legLeft.localPosition = _legLeftBase + new Vector3(0f, lift, 0f);
                _legRight.localPosition = _legRightBase + new Vector3(0f, -lift * 0.5f, 0f);
            }

            if (_body != null)
            {
                var aimAngle = Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg;
                _body.localRotation = Quaternion.Euler(0f, 0f, aimAngle);

                if (_legs != null && _speed > 0.01f)
                {
                    var moveAngle = Mathf.Atan2(_move.y, _move.x) * Mathf.Rad2Deg;
                    _legs.localRotation = Quaternion.Euler(0f, 0f, moveAngle - aimAngle);
                }
                else if (_legs != null)
                {
                    _legs.localRotation = Quaternion.identity;
                }
            }

            _recoil = Mathf.MoveTowards(_recoil, 0f, deltaTime * 7f);
            if (_weaponMount != null)
            {
                _weaponMount.localPosition = _weaponBase - new Vector3(_recoil * 0.09f, 0f, 0f);
            }

            if (_torso != null && _kind != VisualKind.Turret)
            {
                var bob = Mathf.Sin(_walkPhase * 2f) * 0.018f * _speed;
                _torso.localPosition = _torsoBase + new Vector3(0f, bob, 0f);
                _head.localPosition = _headBase + new Vector3(0f, bob * 0.8f, 0f);
                _armLeft.localPosition = _armLeftBase + new Vector3(0f, bob * 0.6f, 0f);
                _armRight.localPosition = _armRightBase + new Vector3(0f, bob * 0.6f, 0f);
            }

            if (_core != null)
            {
                var pulseSpeed = _enraged ? 8f : 3.5f;
                var pulse = 1f + (Mathf.Sin(_time * pulseSpeed) * 0.08f);
                _core.localScale = _coreBaseScale * pulse;
                if (_coreRenderer != null)
                {
                    _coreRenderer.color = _coreColor;
                }
            }

            if (_auraRenderer != null && _elite)
            {
                _auraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, _time * 24f);
            }
        }

        private static float ScaleFor(VisualKind kind)
        {
            return kind switch
            {
                VisualKind.Boss => 0.5f,
                VisualKind.Turret => 1f,
                _ => 2f
            };
        }

        private static CharacterVisual Create(Transform parent, string key, VisualKind kind)
        {
            var visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.localScale = Vector3.one * ScaleFor(kind);
            var visual = visualObject.AddComponent<CharacterVisual>();
            visual.Build(key, kind);
            return visual;
        }

        private void Build(string key, VisualKind kind)
        {
            _kind = kind;
            _partScale = 1f;
            _bodyBaseOrder = kind switch
            {
                VisualKind.Player => SortingOrders.Player,
                VisualKind.Boss => SortingOrders.Boss,
                VisualKind.Turret => SortingOrders.Turret,
                _ => SortingOrders.Enemy
            };

            var shadowOrder = kind switch
            {
                VisualKind.Player => SortingOrders.PlayerShadow,
                VisualKind.Boss => SortingOrders.BossShadow,
                VisualKind.Turret => SortingOrders.EnemyShadow,
                _ => SortingOrders.EnemyShadow
            };

            var shadowPath = key == "turret"
                ? "Characters/player_shadow"
                : $"Characters/{key}_shadow";
            var shadow = CreateSprite(
                transform,
                "Shadow",
                shadowPath,
                shadowOrder,
                Vector3.zero,
                _partScale);
            shadow.transform.localScale = new Vector3(0.8f, 0.55f, 1f) * _partScale;

            var aura = CreateSprite(
                transform,
                "Elite Aura",
                "Effects/elite_aura",
                SortingOrders.EliteAura,
                Vector3.zero,
                1f);
            _auraRenderer = aura;
            aura.gameObject.SetActive(false);

            if (kind == VisualKind.Turret)
            {
                BuildTurret(key);
                Renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
                return;
            }

            _legs = new GameObject("Legs").transform;
            _legs.SetParent(transform, false);

            _legLeft = CreateSprite(
                _legs,
                "Leg Left",
                $"Characters/{key}_leg",
                _bodyBaseOrder - 1,
                new Vector3(-0.15f, -0.1f, 0f),
                _partScale).transform;
            _legRight = CreateSprite(
                _legs,
                "Leg Right",
                $"Characters/{key}_leg",
                _bodyBaseOrder - 1,
                new Vector3(0.15f, -0.1f, 0f),
                _partScale).transform;
            _legLeft.localScale = new Vector3(-_partScale, _partScale, 1f);
            _legRight.localScale = new Vector3(_partScale, _partScale, 1f);
            _legLeftBase = _legLeft.localPosition;
            _legRightBase = _legRight.localPosition;

            _body = new GameObject("Aim Pivot").transform;
            _body.SetParent(transform, false);

            _torso = CreateSprite(
                _body,
                "Torso",
                $"Characters/{key}_torso",
                _bodyBaseOrder,
                Vector3.zero,
                _partScale).transform;
            _head = CreateSprite(
                _body,
                "Head",
                $"Characters/{key}_head",
                _bodyBaseOrder + 1,
                Vector3.zero,
                _partScale).transform;
            _armLeft = CreateSprite(
                _body,
                "Arm Left",
                $"Characters/{key}_arm",
                _bodyBaseOrder + 1,
                new Vector3(-0.02f, 0.19f, 0f),
                _partScale).transform;
            _armRight = CreateSprite(
                _body,
                "Arm Right",
                $"Characters/{key}_arm",
                _bodyBaseOrder + 1,
                new Vector3(-0.02f, -0.19f, 0f),
                _partScale).transform;
            _armLeft.localScale = new Vector3(_partScale, -_partScale, 1f);
            _armRight.localScale = new Vector3(_partScale, _partScale, 1f);
            _torsoBase = _torso.localPosition;
            _headBase = _head.localPosition;
            _armLeftBase = _armLeft.localPosition;
            _armRightBase = _armRight.localPosition;

            if (key == "boss_warden")
            {
                _core = CreateSprite(
                    _body,
                    "Core",
                    "Characters/boss_warden_core",
                    SortingOrders.BossCore,
                    Vector3.zero,
                    _partScale).transform;
                _coreBaseScale = _core.localScale;
                _coreRenderer = _core.GetComponent<SpriteRenderer>();
                _coreColor = new Color(1f, 0.84f, 0.32f, 1f);
            }

            if (kind == VisualKind.Player)
            {
                var weapon = CreateSprite(
                    _body,
                    "Weapon",
                    "Weapons/pistol",
                    SortingOrders.PlayerWeapon,
                    new Vector3(0.17f, 0.02f, 0f),
                    1f);
                _weaponRenderer = weapon;
                _weaponMount = weapon.transform;
                _weaponBase = _weaponMount.localPosition;
            }
            else
            {
                var mount = new GameObject("Weapon Mount").transform;
                mount.SetParent(_body, false);
                mount.localPosition = new Vector3(0.17f, 0f, 0f);
                _weaponMount = mount;
                _weaponBase = mount.localPosition;
            }

            Renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        }

        private void BuildTurret(string key)
        {
            var baseRenderer = CreateSprite(
                transform,
                "Base",
                "Characters/turret_base",
                SortingOrders.Turret,
                Vector3.zero,
                1f);

            _body = new GameObject("Turret Head").transform;
            _body.SetParent(transform, false);
            _head = CreateSprite(
                _body,
                "Head",
                "Characters/turret_head",
                SortingOrders.Turret,
                Vector3.zero,
                1f).transform;

            var barrel = CreateSprite(
                _body,
                "Barrel",
                "Characters/turret_barrel",
                SortingOrders.TurretBarrel,
                new Vector3(0.14f, 0f, 0f),
                1f);
            _weaponRenderer = barrel;
            _weaponMount = barrel.transform;
            _weaponBase = barrel.transform.localPosition;
            _torsoBase = Vector3.zero;
            _headBase = _head.localPosition;

            // Keep the base renderer referenced so the rig owns it.
            _torso = baseRenderer.transform;
            _partScale = 1f;
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            string resourcePath,
            int sortingOrder,
            Vector3 localPosition,
            float scale)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = localPosition;
            spriteObject.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtCatalog.Get(resourcePath);
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
