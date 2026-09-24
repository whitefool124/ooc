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
        UnitDefeated,
        ShieldConsumed,
        ShieldTransferredOut,
        ShieldTransferredIn,
        UtilityResolved,
        BreakStance,
        Attribute
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
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public string Message { get; }

        public CombatFeedbackEvent(CombatFeedbackKind kind, GridPosition target, int amount = 0, int duration = 0)
            : this(kind, target, target, amount, duration) { }

        public CombatFeedbackEvent(CombatFeedbackKind kind, GridPosition source, GridPosition target, int amount = 0, int duration = 0,
            string sourceUnitId = null, string targetUnitId = null, string message = null)
        {
            Kind = kind;
            Source = source;
            Target = target;
            Amount = Math.Max(0, amount);
            Duration = Math.Max(0, duration);
            SourceUnitId = sourceUnitId; TargetUnitId = targetUnitId; Message = message;
        }

        public string FloatingText
        {
            get
            {
                if (!string.IsNullOrEmpty(Message)) return Message;
                CombatFeedbackSemantic semantic = CombatFeedbackCatalog.For(Kind);
                if (Kind == CombatFeedbackKind.ShieldConsumed || Kind == CombatFeedbackKind.ShieldTransferredOut) return semantic.ShortLabel + " -" + Amount;
                if (Kind == CombatFeedbackKind.ShieldTransferredIn) return semantic.ShortLabel + " +" + Amount;
                if (Kind == CombatFeedbackKind.Damage) return "-" + Amount;
                if (Kind == CombatFeedbackKind.ShieldAbsorb) return semantic.ShortLabel + " -" + Amount;
                if (Kind == CombatFeedbackKind.Healing || Kind == CombatFeedbackKind.ShieldRestore || Kind == CombatFeedbackKind.ManaRestore) return "+" + Amount;
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
                case CombatFeedbackKind.ShieldAbsorb: return new CombatFeedbackSemantic(kind, "shield_absorb", "盾", "护盾承伤", "#B8C2CC", "skill");
                case CombatFeedbackKind.ArmorBreak: return new CombatFeedbackSemantic(kind, "armor_break", "破甲", "护甲削弱", "#F0B63A", "attack");
                case CombatFeedbackKind.BreakStance: return new CombatFeedbackSemantic(kind, "break_stance", "破势", "清盾禁盾", "#F0B63A", "attack");
                case CombatFeedbackKind.Attribute: return new CombatFeedbackSemantic(kind, "attribute", "属性", "数值状态", "#56CAD8", "skill");
                case CombatFeedbackKind.Burning: return new CombatFeedbackSemantic(kind, "burning", "燃烧", "持续燃烧", "#FF7043", "skill_two");
                case CombatFeedbackKind.Bound: return new CombatFeedbackSemantic(kind, "bound", "束缚", "无法移动", "#52D6FF", "skill");
                case CombatFeedbackKind.Slow: return new CombatFeedbackSemantic(kind, "slow", "迟缓", "速度降低", "#8AA6A0", "move");
                case CombatFeedbackKind.Healing: return new CombatFeedbackSemantic(kind, "healing", "修复", "生命修复", "#67C58B", "loot");
                case CombatFeedbackKind.ShieldRestore: return new CombatFeedbackSemantic(kind, "shield_restore", "护盾", "护盾恢复", "#B8C2CC", "skill");
                case CombatFeedbackKind.ManaRestore: return new CombatFeedbackSemantic(kind, "mana_restore", "以太恢复", "以太资源恢复", "#5BC0EB", "skill_two");
                case CombatFeedbackKind.StatusCleared: return new CombatFeedbackSemantic(kind, "status_cleared", "状态净化", "负面状态已清除", "#B8E986", "interact");
                case CombatFeedbackKind.Movement: return new CombatFeedbackSemantic(kind, "movement", "位移", "位置变化", "#3FDCE8", "move");
                case CombatFeedbackKind.DestructibleDamaged: return new CombatFeedbackSemantic(kind, "object_damaged", "物件受损", "物件耐久下降", "#E0A431", "interact");
                case CombatFeedbackKind.DestructibleDestroyed: return new CombatFeedbackSemantic(kind, "object_destroyed", "物件摧毁", "物件已摧毁", "#FF7A2F", "attack");
                case CombatFeedbackKind.UnitDefeated: return new CombatFeedbackSemantic(kind, "unit_defeated", "目标击破", "单位失去行动能力", "#FFD166", "attack");
                case CombatFeedbackKind.ShieldConsumed: return new CombatFeedbackSemantic(kind, "shield_consumed", "消耗护盾", "护盾支付代价", "#AEB8C2", "skill");
                case CombatFeedbackKind.ShieldTransferredOut: return new CombatFeedbackSemantic(kind, "shield_transfer_out", "转出护盾", "护盾转给友军", "#AEB8C2", "skill");
                case CombatFeedbackKind.ShieldTransferredIn: return new CombatFeedbackSemantic(kind, "shield_transfer_in", "转入护盾", "收到友军护盾", "#C6D0DA", "skill");
                case CombatFeedbackKind.UtilityResolved: return new CombatFeedbackSemantic(kind, "utility_resolved", "效果生效", "辅助效果已结算", "#B4C9C1", "interact");
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
                case StatusType.BreakStance: return CombatFeedbackKind.BreakStance;
                case StatusType.Agility: return CombatFeedbackKind.Attribute;
                case StatusType.Strength:
                case StatusType.SpellPower:
                case StatusType.Speed:
                case StatusType.Range:
                case StatusType.DamageTaken:
                case StatusType.ShieldEfficiency:
                case StatusType.ShieldGrant:
                case StatusType.Control:
                case StatusType.FiregroundBoost:
                case StatusType.FiregroundVulnerable:
                case StatusType.Dazzled:
                case StatusType.Revealed: return CombatFeedbackKind.Attribute;
                case StatusType.Marked:
                case StatusType.Prepared:
                case StatusType.Invulnerable: return CombatFeedbackKind.Attribute;
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status feedback.");
            }
        }

        public static string StatusHudText(StatusType status, int duration, int strength = 0)
        {
            if (UnitState.IsAttributeStatus(status))
            {
                string label = status == StatusType.Agility ? "敏捷" : status == StatusType.Strength ? "力量" :
                    status == StatusType.SpellPower ? "法强" : status == StatusType.Speed ? "速度" :
                    status == StatusType.Range ? "射程" : status == StatusType.DamageTaken ? "承伤" :
                    status == StatusType.ShieldEfficiency ? "护盾效能" : status == StatusType.ShieldGrant ? "护盾授予" : "控制";
                return label + (strength >= 0 ? "+" : string.Empty) + strength + "　剩余 " + Math.Max(0, duration) + " 回合";
            }
            CombatFeedbackSemantic semantic = For(ForStatus(status));
            return semantic.ShortLabel + " " + Math.Max(0, duration) + "　" + semantic.HudLabel;
        }
    }
}
