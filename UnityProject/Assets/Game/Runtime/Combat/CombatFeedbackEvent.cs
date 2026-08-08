using System;

namespace OCC.Combat
{
    public enum CombatFeedbackKind
    {
        Damage,
        ShieldAbsorb,
        ArmorBreak,
        Burning,
        Bound,
        Slow,
        Healing,
        ShieldRestore,
        ManaRestore,
        StatusCleared,
        Movement,
        DestructibleDamaged,
        DestructibleDestroyed,
        UnitDefeated
    }

    public readonly struct CombatFeedbackSemantic
    {
        public CombatFeedbackKind Kind { get; }
        public string Key { get; }
        public string ShortLabel { get; }
        public string HudLabel { get; }
        public string ColorHex { get; }
        public string IconKey { get; }

        public CombatFeedbackSemantic(CombatFeedbackKind kind, string key, string shortLabel, string hudLabel, string colorHex, string iconKey)
        {
            Kind = kind;
            Key = key;
            ShortLabel = shortLabel;
            HudLabel = hudLabel;
            ColorHex = colorHex;
            IconKey = iconKey;
        }
    }

    // Pure data contract between deterministic combat changes and any UI/audio/VFX consumer.
    public readonly struct CombatFeedbackEvent
    {
        public CombatFeedbackKind Kind { get; }
        public GridPosition Source { get; }
        public GridPosition Target { get; }
        public int Amount { get; }
        public int Duration { get; }

        public CombatFeedbackEvent(CombatFeedbackKind kind, GridPosition target, int amount = 0, int duration = 0)
            : this(kind, target, target, amount, duration) { }

        public CombatFeedbackEvent(CombatFeedbackKind kind, GridPosition source, GridPosition target, int amount = 0, int duration = 0)
        {
            Kind = kind;
            Source = source;
            Target = target;
            Amount = Math.Max(0, amount);
            Duration = Math.Max(0, duration);
        }

        public string FloatingText
        {
            get
            {
                CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(Kind);
                if (Kind == CombatFeedbackKind.Damage) return "-" + Amount + " " + semantic.ShortLabel;
                if (Kind == CombatFeedbackKind.ShieldAbsorb) return semantic.ShortLabel + " -" + Amount;
                if (Kind == CombatFeedbackKind.Healing || Kind == CombatFeedbackKind.ShieldRestore || Kind == CombatFeedbackKind.ManaRestore) return semantic.ShortLabel + " +" + Amount;
                if (Kind == CombatFeedbackKind.Burning || Kind == CombatFeedbackKind.Bound || Kind == CombatFeedbackKind.Slow || Kind == CombatFeedbackKind.ArmorBreak)
                    return Duration > 0 ? semantic.ShortLabel + " " + Duration : semantic.ShortLabel;
                return semantic.ShortLabel;
            }
        }
    }

    public static class CombatFeedbackCatalog
    {
        public static CombatFeedbackSemantic For(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.Damage: return new CombatFeedbackSemantic(kind, "damage", "伤害", "生命损伤", "#E75642", "attack");
                case CombatFeedbackKind.ShieldAbsorb: return new CombatFeedbackSemantic(kind, "shield_absorb", "护盾吸收", "护盾承伤", "#72B8A1", "skill");
                case CombatFeedbackKind.ArmorBreak: return new CombatFeedbackSemantic(kind, "armor_break", "破甲", "护甲削弱", "#F0B63A", "attack");
                case CombatFeedbackKind.Burning: return new CombatFeedbackSemantic(kind, "burning", "燃烧", "持续燃烧", "#FF7043", "skill_two");
                case CombatFeedbackKind.Bound: return new CombatFeedbackSemantic(kind, "bound", "束缚", "无法移动", "#52D6FF", "skill");
                case CombatFeedbackKind.Slow: return new CombatFeedbackSemantic(kind, "slow", "迟缓", "速度降低", "#8AA6A0", "move");
                case CombatFeedbackKind.Healing: return new CombatFeedbackSemantic(kind, "healing", "修复", "生命修复", "#67C58B", "loot");
                case CombatFeedbackKind.ShieldRestore: return new CombatFeedbackSemantic(kind, "shield_restore", "护盾恢复", "护盾恢复", "#92D1B9", "skill");
                case CombatFeedbackKind.ManaRestore: return new CombatFeedbackSemantic(kind, "mana_restore", "以太恢复", "以太资源恢复", "#5BC0EB", "skill_two");
                case CombatFeedbackKind.StatusCleared: return new CombatFeedbackSemantic(kind, "status_cleared", "状态净化", "负面状态已清除", "#B8E986", "interact");
                case CombatFeedbackKind.Movement: return new CombatFeedbackSemantic(kind, "movement", "位移", "位置变化", "#3FDCE8", "move");
                case CombatFeedbackKind.DestructibleDamaged: return new CombatFeedbackSemantic(kind, "object_damaged", "物件受损", "物件耐久下降", "#E0A431", "interact");
                case CombatFeedbackKind.DestructibleDestroyed: return new CombatFeedbackSemantic(kind, "object_destroyed", "物件摧毁", "物件已摧毁", "#FF7A2F", "attack");
                case CombatFeedbackKind.UnitDefeated: return new CombatFeedbackSemantic(kind, "unit_defeated", "目标击破", "单位失去行动能力", "#FFD166", "attack");
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown combat feedback kind.");
            }
        }

        public static CombatFeedbackKind ForStatus(StatusType status)
        {
            switch (status)
            {
                case StatusType.Burning: return CombatFeedbackKind.Burning;
                case StatusType.Bound: return CombatFeedbackKind.Bound;
                case StatusType.Slow: return CombatFeedbackKind.Slow;
                case StatusType.ArmorBreak: return CombatFeedbackKind.ArmorBreak;
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status feedback.");
            }
        }

        public static string StatusHudText(StatusType status, int duration)
        {
            CombatFeedbackSemantic semantic = For(ForStatus(status));
            return semantic.ShortLabel + " " + Math.Max(0, duration) + " // " + semantic.HudLabel;
        }
    }
}
