namespace OCC.Combat
{
    public enum CoverType { None, Light, Heavy }

    public sealed class TileState
    {
        public static TileState Empty => new TileState();
        public CoverType Cover { get; set; }
        public int Durability { get; set; }
        public bool IsObjective { get; set; }
        public bool IsDevice { get; set; }
        public bool IsWater { get; set; }
        public int SmokeExpiresAt { get; set; }
        public bool IsDestroyed => Durability <= 0 && (Cover != CoverType.None || IsObjective || IsDevice);
        public bool BlocksMovement => Cover == CoverType.Heavy && !IsDestroyed;
        public bool BlocksLineOfSight => Cover == CoverType.Heavy && !IsDestroyed;
        public int DamageReduction => IsDestroyed ? 0 : Cover == CoverType.Light ? 1 : Cover == CoverType.Heavy ? 2 : 0;
        public TileState Clone() => new TileState { Cover = Cover, Durability = Durability, IsObjective = IsObjective, IsDevice = IsDevice, IsWater = IsWater, SmokeExpiresAt = SmokeExpiresAt };
    }
}
