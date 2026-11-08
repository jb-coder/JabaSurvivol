using System;

namespace JabaSurvivol.Core
{
    /// <summary>
    /// Run-only state for the selected active skill. Turret timing lives here
    /// so deployment, duration, firing cadence, and cooldown are deterministic.
    /// </summary>
    public sealed class ActiveSkillRuntime
    {
        public ActiveSkillRuntime(SkillNode node, int rank)
        {
            if (!SkillCatalog.IsDefined(node))
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }

            Node = node;
            Rank = Math.Clamp(rank, 0, SkillCatalog.Get(node).MaxRanks);
            Stats = SkillCatalog.Get(node).GetActivationStats(Rank);
        }

        public SkillNode Node { get; private set; }

        public int Rank { get; private set; }

        public SkillActivationStats Stats { get; private set; }

        public float CooldownRemaining { get; private set; }

        public float ActiveRemaining { get; private set; }

        public float FireTimer { get; private set; }

        public bool IsActive => ActiveRemaining > 0f;

        public bool IsReady => Rank > 0 && CooldownRemaining <= 0f;

        public bool CanActivate => IsReady && !IsActive;

        public float CooldownFraction => Stats.CooldownSeconds <= 0f
            ? 0f
            : Math.Clamp(CooldownRemaining / Stats.CooldownSeconds, 0f, 1f);

        public float ActiveFraction => Stats.TurretDuration <= 0f
            ? 0f
            : Math.Clamp(ActiveRemaining / Stats.TurretDuration, 0f, 1f);

        /// <summary>
        /// Activates the selected skill. Turret and Dash have an active
        /// duration; Shockwave resolves instantly and only starts cooldown.
        /// </summary>
        public bool TryActivate()
        {
            if (!CanActivate)
            {
                return false;
            }

            CooldownRemaining = Stats.CooldownSeconds;
            FireTimer = 0f;
            ActiveRemaining = Node switch
            {
                SkillNode.Turret => Stats.TurretDuration,
                SkillNode.Dash => GameBalance.DashInvulnerabilitySeconds,
                _ => 0f
            };
            return true;
        }

        public void Tick(float deltaTime)
        {
            var step = Math.Max(0f, deltaTime);
            CooldownRemaining = Math.Max(0f, CooldownRemaining - step);

            if (!IsActive)
            {
                return;
            }

            ActiveRemaining = Math.Max(0f, ActiveRemaining - step);
            if (!IsActive)
            {
                ActiveRemaining = 0f;
                FireTimer = 0f;
                return;
            }

            FireTimer -= step;
        }

        /// <summary>
        /// Consumes one turret shot when the firing cadence allows it.
        /// </summary>
        public bool TryConsumeShot(out float damage)
        {
            if (!IsActive || FireTimer > 0f)
            {
                damage = 0f;
                return false;
            }

            damage = Stats.TurretDamage;
            FireTimer = Stats.TurretFireInterval;
            return true;
        }

        public void UpdateRank(int rank)
        {
            Rank = Math.Clamp(rank, 0, SkillCatalog.Get(Node).MaxRanks);
            Stats = SkillCatalog.Get(Node).GetActivationStats(Rank);
            CooldownRemaining = Math.Min(CooldownRemaining, Stats.CooldownSeconds);
            if (IsActive)
            {
                ActiveRemaining = Math.Min(ActiveRemaining, Stats.TurretDuration);
            }
        }

        public void SetNode(SkillNode node, int rank)
        {
            if (!SkillCatalog.IsDefined(node))
            {
                throw new ArgumentOutOfRangeException(nameof(node));
            }

            var previousCooldown = CooldownRemaining;
            Node = node;
            ActiveRemaining = 0f;
            FireTimer = 0f;
            UpdateRank(rank);
            CooldownRemaining = previousCooldown;
        }

        public void Reset()
        {
            CooldownRemaining = 0f;
            ActiveRemaining = 0f;
            FireTimer = 0f;
        }
    }
}
