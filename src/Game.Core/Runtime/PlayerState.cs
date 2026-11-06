using System;

namespace JabaSurvivol.Core
{

    /// <summary>
    /// Runtime health state for the player. Persistent progression is stored in
    /// <see cref="Profile"/> and is never touched by this type.
    /// </summary>
    public sealed class PlayerState
    {
        public PlayerState(float maxHealth)
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
        /// Applies damage and returns the amount actually removed.
        /// </summary>
        public float ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            var applied = Math.Min(amount, Health);
            Health -= applied;
            return applied;
        }

        /// <summary>
        /// Heals without exceeding the maximum health.
        /// </summary>
        public float Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            var applied = Math.Min(amount, MaxHealth - Health);
            Health += applied;
            return applied;
        }

        /// <summary>
        /// Resets health for a new run.
        /// </summary>
        public void Reset()
        {
            Health = MaxHealth;
        }
    }
}
