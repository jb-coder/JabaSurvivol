using System;
using System.Collections.Generic;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Run-only magazine and reserve ammunition for one weapon. The type is
    /// engine-free so combat, tests, and simulations share the same rules.
    /// </summary>
    public sealed class WeaponRuntimeState
    {
        private float _ammoCapacityMultiplier;
        private float _reloadSpeedMultiplier;

        public WeaponRuntimeState(
            WeaponStats stats,
            float ammoCapacityMultiplier,
            float reloadSpeedMultiplier)
        {
            Stats = stats;
            _ammoCapacityMultiplier = Math.Max(0.01f, ammoCapacityMultiplier);
            _reloadSpeedMultiplier = Math.Max(0.01f, reloadSpeedMultiplier);
            Magazine = MagazineSize;
            Reserve = Stats.HasInfiniteReserve ? 0 : Stats.ReserveAmmo;
        }

        public WeaponId Id => Stats.Id;

        public WeaponStats Stats { get; private set; }

        public int MagazineSize => Math.Max(
            1,
            (int)Math.Ceiling(Stats.MagazineSize * _ammoCapacityMultiplier));

        public int Magazine { get; private set; }

        /// <summary>
        /// Rounds left in reserve. The pistol has infinite reserve and reports
        /// zero here; use <see cref="HasInfiniteReserve"/> for display.
        /// </summary>
        public int Reserve { get; private set; }

        public bool HasInfiniteReserve => Stats.HasInfiniteReserve;

        public bool IsReloading { get; private set; }

        public float ReloadRemaining { get; private set; }

        public float ReloadDuration => Stats.ReloadSeconds / _reloadSpeedMultiplier;

        public bool IsMagazineFull => Magazine >= MagazineSize;

        public bool CanReload => !IsReloading &&
                                 !IsMagazineFull &&
                                 (HasInfiniteReserve || Reserve > 0);

        public bool TryFire()
        {
            if (IsReloading || Magazine <= 0)
            {
                return false;
            }

            Magazine--;
            return true;
        }

        public bool StartReload()
        {
            if (!CanReload)
            {
                return false;
            }

            IsReloading = true;
            ReloadRemaining = ReloadDuration;
            return true;
        }

        /// <summary>
        /// Advances an active reload and returns true when it completed.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!IsReloading)
            {
                return false;
            }

            ReloadRemaining -= Math.Max(0f, deltaTime);
            if (ReloadRemaining > 0f)
            {
                return false;
            }

            CompleteReload();
            return true;
        }

        /// <summary>
        /// Adds free, drop-based reserve ammunition. Infinite reserves ignore
        /// the addition.
        /// </summary>
        public int AddReserve(int amount)
        {
            if (amount <= 0 || HasInfiniteReserve)
            {
                return 0;
            }

            Reserve = SafeAdd(Reserve, amount);
            return amount;
        }

        public void Reset()
        {
            Magazine = MagazineSize;
            Reserve = HasInfiniteReserve ? 0 : Stats.ReserveAmmo;
            IsReloading = false;
            ReloadRemaining = 0f;
        }

        /// <summary>
        /// Applies new Engineering multipliers to the weapon. Current magazine
        /// contents are preserved and clamped to the new capacity.
        /// </summary>
        public void UpdateMultipliers(float ammoCapacityMultiplier, float reloadSpeedMultiplier)
        {
            _ammoCapacityMultiplier = Math.Max(0.01f, ammoCapacityMultiplier);
            _reloadSpeedMultiplier = Math.Max(0.01f, reloadSpeedMultiplier);
            Magazine = Math.Min(Magazine, MagazineSize);
            if (IsReloading)
            {
                ReloadRemaining = Math.Min(ReloadRemaining, ReloadDuration);
            }
        }

        public void UpdateStats(WeaponStats stats)
        {
            Stats = stats;
            Magazine = Math.Min(Magazine, MagazineSize);
            if (HasInfiniteReserve)
            {
                Reserve = 0;
            }
        }

        private void CompleteReload()
        {
            if (HasInfiniteReserve)
            {
                Magazine = MagazineSize;
            }
            else
            {
                var needed = MagazineSize - Magazine;
                var loaded = Math.Min(needed, Reserve);
                Magazine += loaded;
                Reserve -= loaded;
            }

            IsReloading = false;
            ReloadRemaining = 0f;
        }

        private static int SafeAdd(int left, int right)
        {
            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }

    /// <summary>
    /// Run-only loadout for every weapon the profile has unlocked. Each weapon
    /// keeps its own magazine and reserve for the duration of the run.
    /// </summary>
    public sealed class WeaponLoadout
    {
        private readonly WeaponRuntimeState[] _states;
        private int _currentIndex;

        public WeaponLoadout(
            Profile profile,
            float ammoCapacityMultiplier = 1f,
            float reloadSpeedMultiplier = 1f)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var weapons = new List<WeaponId>();
            if (profile.UnlockedWeapons != null)
            {
                for (var i = 0; i < profile.UnlockedWeapons.Count; i++)
                {
                    var weapon = profile.UnlockedWeapons[i];
                    if (WeaponCatalog.IsDefined(weapon) && !weapons.Contains(weapon))
                    {
                        weapons.Add(weapon);
                    }
                }
            }

            if (!weapons.Contains(WeaponId.Pistol))
            {
                weapons.Insert(0, WeaponId.Pistol);
            }

            _states = new WeaponRuntimeState[weapons.Count];
            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                _states[i] = new WeaponRuntimeState(
                    WeaponUpgradeService.GetEffectiveStats(profile, weapon),
                    ammoCapacityMultiplier,
                    reloadSpeedMultiplier);
            }

            _currentIndex = 0;
            for (var i = 0; i < _states.Length; i++)
            {
                if (_states[i].Id == profile.EquippedWeapon)
                {
                    _currentIndex = i;
                    break;
                }
            }
        }

        public IReadOnlyList<WeaponRuntimeState> States => _states;

        public WeaponRuntimeState Current => _states[_currentIndex];

        public IReadOnlyList<WeaponId> Weapons
        {
            get
            {
                var weapons = new WeaponId[_states.Length];
                for (var i = 0; i < _states.Length; i++)
                {
                    weapons[i] = _states[i].Id;
                }

                return weapons;
            }
        }

        public bool TrySwitchTo(WeaponId weaponId)
        {
            for (var i = 0; i < _states.Length; i++)
            {
                if (_states[i].Id == weaponId)
                {
                    _currentIndex = i;
                    return true;
                }
            }

            return false;
        }

        public bool SwitchNext()
        {
            if (_states.Length <= 1)
            {
                return false;
            }

            _currentIndex = (_currentIndex + 1) % _states.Length;
            return true;
        }

        public bool TryFireCurrent()
        {
            return Current.TryFire();
        }

        public bool StartReload()
        {
            return Current.StartReload();
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _states.Length; i++)
            {
                _states[i].Tick(deltaTime);
            }
        }

        public int AddReserve(int amount)
        {
            var applied = 0;
            for (var i = 0; i < _states.Length; i++)
            {
                applied += _states[i].AddReserve(amount);
            }

            return applied;
        }

        public void Reset()
        {
            for (var i = 0; i < _states.Length; i++)
            {
                _states[i].Reset();
            }
        }

        public void UpdateMultipliers(float ammoCapacityMultiplier, float reloadSpeedMultiplier)
        {
            for (var i = 0; i < _states.Length; i++)
            {
                _states[i].UpdateMultipliers(ammoCapacityMultiplier, reloadSpeedMultiplier);
            }
        }
    }
}
