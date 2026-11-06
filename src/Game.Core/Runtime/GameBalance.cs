namespace JabaSurvivol.Core
{

    /// <summary>
    /// Central balance values for the walking skeleton. Values are intentionally
    /// data-only so later phases can move them into ScriptableObjects.
    /// </summary>
    public static class GameBalance
    {
        public const float PlayerMaxHealth = 100f;
        public const float PlayerMoveSpeed = 4.5f;
        public const float PlayerRadius = 0.45f;

        public const float PistolDamage = 20f;
        public const float PistolFireInterval = 0.25f;
        public const float BulletSpeed = 13f;
        public const float BulletLifetime = 1.35f;
        public const int BulletPoolSize = 48;

        public const float ZombieMaxHealth = 60f;
        public const float ZombieMoveSpeed = 1.9f;
        public const float ZombieRadius = 0.5f;
        public const float ZombieContactDamage = 10f;
        public const float ZombieContactInterval = 0.8f;
        public const float ZombieRespawnDelay = 1.2f;

        public const float ArenaHalfWidth = 8.8f;
        public const float ArenaHalfHeight = 4.7f;

        public const int SuppliesPerKill = 10;
        public const int XpPerKill = 10;
        public const int SuppliesPerRoundClear = 100;
        public const int XpPerRoundClear = 50;
        public const int KillsPerRound = 5;

        public const float StickDeadZone = 0.16f;
        public const float AimStickDeadZone = 0.2f;
    }
}
