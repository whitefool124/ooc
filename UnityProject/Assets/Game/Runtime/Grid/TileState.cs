namespace OCC.Combat
{
    public enum CoverType { None, Light, Heavy }

    public sealed class TileState
    {
        public const int FragileDurability = 6;
        public const int LightDurability = 8;
        public const int StandardDurability = 16;
        public const int HeavyDurability = 24;
        /// <summary>技能现场生成的临时重掩体耐久，低于地图预设重掩体（技能数据表 SK-CORE-02）。</summary>
        public const int TemporaryHeavyCoverDurability = 12;
        /// <summary>试制件耐久（敌人技能数据表 SK-SUP-13）。</summary>
        public const int PrototypeDurability = 8;

        public static TileState Empty => new TileState();
        public CoverType Cover { get; set; }
        public int Durability { get; set; }
        /// <summary>技能生成的结构所属单位；地图预置掩体没有归属。</summary>
        public string StructureOwnerUnitId { get; set; }
        public bool IsObjective { get; set; }
        public bool IsDevice { get; set; }
        public bool IsWater { get; set; }
        public bool IsLampVine { get; set; }
        public bool IsAetherCrystal { get; set; }
        public bool IsPressureCrystal { get; set; }
        /// <summary>地图配置中的宝箱位置；内容仍由遭遇与奖励系统提供。</summary>
        public bool IsLootChest { get; set; }
        public bool IsDecoy { get; set; }
        public bool IsCrystalShard { get; set; }
        public bool IsPermanentWall { get; set; }
        public bool IsScorched { get; set; }
        public int SmokeExpiresAt { get; set; }
        /// <summary>散页：效果层。移动消耗 2，可被点燃或被风逐格搬动；浅水覆盖时直接清除。</summary>
        public bool IsLoosePaper { get; set; }
        /// <summary>痕迹：效果层。供追踪类单位读取；被浅水、火场或强风清除。</summary>
        public bool HasTrace { get; set; }
        /// <summary>气味痕加深：目标在同一格停留超过一个自身回合后形成，保留到被浅水、火场或强风清除。</summary>
        public bool IsDeepTrace { get; set; }
        /// <summary>页幕：由散页扬起的遮挡，截断穿过该格的攻击线，持续到扬起者下一回合开始。</summary>
        public bool HasPaperScreen { get; set; }
        /// <summary>当前场地效果的来源；用于防止旧来源的到期计时清除后来覆盖的效果。</summary>
        public string EffectSourceId { get; set; }
        /// <summary>约束纹：效果层。进入该格的单位本回合留在原地；被浅水、火场、烟尘覆盖即失效。</summary>
        public bool IsBindingMark { get; set; }
        /// <summary>过载装置：被摧毁时对正交邻格结算一次不分敌我的过载伤害。</summary>
        public bool IsOverloadDevice { get; set; }
        /// <summary>护罩发生器：为配置声明范围内的单位提供结构护盾；装置被摧毁时护盾立即消失。</summary>
        public bool IsWardGenerator { get; set; }
        /// <summary>封存塔内机关。属于通用场地装置，不是敌人单位；被摧毁即切断塔之守卫的施术介质。</summary>
        public bool IsTowerMechanism { get; set; }
        /// <summary>机关是否已被放行。放行前不提供任何效果，放行后才计入维护链。</summary>
        public bool IsReleased { get; set; }
        /// <summary>机关种类：1 护障维护 / 2 显影巡查 / 3 冲压隔离。0 表示不是机关。</summary>
        public int MechanismKind { get; set; }
        /// <summary>是否为任一效果层。每格至多一个，后生成者覆盖先在者。</summary>
        public bool HasEffectLayer => IsWater || IsScorched || SmokeExpiresAt > 0 || IsLoosePaper || HasTrace || IsBindingMark || HasPaperScreen;
        /// <summary>是否为任一可破坏装置（含机关）。</summary>
        public bool IsDeviceLike => IsDevice || IsOverloadDevice || IsWardGenerator || IsTowerMechanism;
        public bool IsDestroyed => !IsPermanentWall && Durability <= 0 && (Cover != CoverType.None || IsObjective || IsDevice
            || IsLampVine || IsDecoy || IsScorched || IsOverloadDevice || IsWardGenerator || IsTowerMechanism);
        public bool BlocksMovement => IsPermanentWall || IsLootChest || (Cover == CoverType.Heavy && !IsDestroyed) || (IsAetherCrystal && Durability > 0)
            || (IsDecoy && Durability > 0)
            || ((IsOverloadDevice || IsWardGenerator) && Durability > 0)
            || (IsTowerMechanism && Durability > 0);
        public bool BlocksLineOfSight => IsPermanentWall || (Cover == CoverType.Heavy && !IsDestroyed) || (IsLampVine && Durability > 0);
        public int DamageReduction => IsDestroyed ? 0 : Cover == CoverType.Light ? 1 : Cover == CoverType.Heavy ? 2 : 0;
        public TileState Clone() => new TileState { Cover = Cover, Durability = Durability, StructureOwnerUnitId = StructureOwnerUnitId, IsObjective = IsObjective, IsDevice = IsDevice,
            IsWater = IsWater, IsLampVine = IsLampVine, IsAetherCrystal = IsAetherCrystal, IsPressureCrystal = IsPressureCrystal, IsLootChest = IsLootChest, IsDecoy = IsDecoy, IsCrystalShard = IsCrystalShard,
            IsPermanentWall = IsPermanentWall,
            IsScorched = IsScorched, SmokeExpiresAt = SmokeExpiresAt,
            IsLoosePaper = IsLoosePaper, HasTrace = HasTrace, IsDeepTrace = IsDeepTrace, IsBindingMark = IsBindingMark,
            HasPaperScreen = HasPaperScreen, EffectSourceId = EffectSourceId,
            IsOverloadDevice = IsOverloadDevice,
            IsWardGenerator = IsWardGenerator,
            IsTowerMechanism = IsTowerMechanism, IsReleased = IsReleased, MechanismKind = MechanismKind };
        /// <summary>清除本格的全部效果层。调用方随后设置新的效果层，即可满足"后生成者覆盖先在者"。</summary>
        public void ClearEffectLayers()
        {
            IsWater = false; IsScorched = false; SmokeExpiresAt = 0;
            IsLoosePaper = false; HasTrace = false; IsDeepTrace = false; IsBindingMark = false;
            HasPaperScreen = false; EffectSourceId = null;
        }
        /// <summary>当前效果层的中文名；无效果层返回 null。美术未就绪时用作汉字占位符。</summary>
        public string EffectLayerName()
        {
            if (IsWater) return "浅水";
            if (IsScorched) return "燃烧地格";
            if (IsLoosePaper) return "散页";
            if (HasTrace) return IsDeepTrace ? "痕迹（加深）" : "痕迹";
            if (IsBindingMark) return "约束纹";
            if (HasPaperScreen) return "页幕";
            if (SmokeExpiresAt > 0) return "烟尘";
            return null;
        }
        /// <summary>地表瓦片的中文名；无特殊瓦片返回 null。</summary>
        public string TileName() => IsCrystalShard ? "碎晶" : null;
        /// <summary>物块或装置的中文名；没有可读对象返回 null。美术未就绪时用作汉字占位符。</summary>
        public string ObjectName()
        {
            if (IsTowerMechanism) return "塔内机关";
            if (IsOverloadDevice) return "过载装置";
            if (IsWardGenerator) return "护罩发生器";
            if (IsObjective) return "任务目标";
            if (IsLampVine) return "灯藤";
            if (IsPressureCrystal) return "精英稳压晶簇";
            if (IsAetherCrystal) return "蓄能晶簇";
            if (IsLootChest) return "宝箱";
            if (IsPermanentWall) return "永久重物块";
            if (Cover == CoverType.Heavy) return "重掩体";
            if (Cover == CoverType.Light) return "轻掩体";
            if (IsDecoy) return "诱导物";
            return null;
        }
    }
}
