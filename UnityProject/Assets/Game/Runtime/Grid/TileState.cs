namespace OCC.Combat
{
    public enum CoverType { None, Light, Heavy }

    public sealed class TileState
    {
        public const int FragileDurability = 6;
        public const int LightDurability = 8;
        public const int StandardDurability = 16;
        public const int HeavyDurability = 24;

        public static TileState Empty => new TileState();
        public CoverType Cover { get; set; }
        public int Durability { get; set; }
        public bool IsObjective { get; set; }
        public bool IsDevice { get; set; }
        public bool IsWater { get; set; }
        public bool IsLampVine { get; set; }
        public bool IsAetherCrystal { get; set; }
        public bool IsCrystalShard { get; set; }
        public bool IsScorched { get; set; }
        public int SmokeExpiresAt { get; set; }
        public bool IsDestroyed => Durability <= 0 && (Cover != CoverType.None || IsObjective || IsDevice || IsLampVine || IsScorched);
        public bool BlocksMovement => (Cover == CoverType.Heavy && !IsDestroyed) || (IsAetherCrystal && Durability > 0);
        public bool BlocksLineOfSight => (Cover == CoverType.Heavy && !IsDestroyed) || (IsLampVine && Durability > 0);
        public int DamageReduction => IsDestroyed ? 0 : Cover == CoverType.Light ? 1 : Cover == CoverType.Heavy ? 2 : 0;
        public TileState Clone() => new TileState { Cover = Cover, Durability = Durability, IsObjective = IsObjective, IsDevice = IsDevice,
            IsWater = IsWater, IsLampVine = IsLampVine, IsAetherCrystal = IsAetherCrystal, IsCrystalShard = IsCrystalShard,
            IsScorched = IsScorched, SmokeExpiresAt = SmokeExpiresAt };
    }
}
