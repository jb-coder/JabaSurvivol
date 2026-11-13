namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Named world sorting orders so layering changes stay consistent across
    /// pooled actors, weapons, effects, and environment art.
    /// </summary>
    public static class SortingOrders
    {
        public const int Floor = -30;
        public const int FloorGrid = -25;
        public const int WallsAndProps = -20;
        public const int Telegraph = 10;
        public const int DeathEffect = 12;
        public const int EliteAura = 14;
        public const int EnemyShadow = 13;
        public const int Enemy = 15;
        public const int EliteRing = 16;
        public const int BossShadow = 17;
        public const int Boss = 18;
        public const int BossCore = 19;
        public const int PlayerShadow = 19;
        public const int Player = 20;
        public const int PlayerWeapon = 21;
        public const int Turret = 22;
        public const int TurretBarrel = 23;
        public const int Projectile = 30;
        public const int MuzzleAndImpact = 31;
    }
}
