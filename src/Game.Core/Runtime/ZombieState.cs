using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Health state for a single zombie. Rendering and movement stay in Unity.
    /// </summary>
    public sealed class ZombieState
    {
        public ZombieState(float maxHealth)
        {
            if (maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public float MaxHealth { get; }

        public float Health { get; private set; }

        public bool IsAlive => Health > 0f;

        /// <summary>
        /// Applies damage and returns true when this hit killed the zombie.
        /// </summary>
        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return false;
            }

            Health = Math.Max(0f, Health - amount);
            return !IsAlive;
        }
    }
}
