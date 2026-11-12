namespace JabaSurvivol.Core
{
    /// <summary>
    /// Documented balance targets for a ten-round run. The deterministic
    /// simulation and the balance tests use these ranges as guard rails so a
    /// data change cannot silently move pacing, rewards, or the boss fight
    /// outside the intended experience.
    /// </summary>
    public static class BalanceTargets
    {
        public const float MinimumRunSeconds = 480f;
        public const float MaximumRunSeconds = 720f;

        public const float EarlyRoundMaximumSeconds = 35f;
        public const float MidRoundMinimumSeconds = 30f;
        public const float MidRoundMaximumSeconds = 85f;
        public const float LateRoundMinimumSeconds = 60f;
        public const float LateRoundMaximumSeconds = 160f;

        public const int MinimumFinalLevel = 8;
        public const int MaximumFinalLevel = 12;

        public const int MinimumSuppliesEarned = 4000;
        public const int MaximumSuppliesEarned = 6000;
        public const int MinimumSuppliesRemaining = 500;
        public const int MaximumSuppliesRemaining = 2500;

        public const float MinimumBossSeconds = 20f;
        public const float MaximumBossSeconds = 60f;
        public const float MaximumAmmoPressure = 0.5f;
    }
}
