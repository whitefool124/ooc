using System;
using System.Linq;

namespace OCC.Combat
{
    public sealed class CombatUnitVitalPresentation
    {
        public int Current { get; }
        public int Maximum { get; }
        public int ForecastLoss { get; }
        public int Remaining { get; }
        public bool Uncapped { get; }
        public bool WillEmpty => Current > 0 && Remaining <= 0 && ForecastLoss > 0;
        public float CurrentRatio => Current / (float)Math.Max(1, Maximum);
        public float RemainingRatio => Remaining / (float)Math.Max(1, Maximum);

        public string CompactText
        {
            get
            {
                if (ForecastLoss <= 0) return Uncapped ? Current.ToString() : "当前 " + Current + "　上限 " + Maximum;
                return Current + " -" + ForecastLoss + " → " + Remaining + (Uncapped ? string.Empty : "　上限 " + Maximum);
            }
        }

        public CombatUnitVitalPresentation(int current, int maximum, int forecastLoss, int remaining, bool uncapped = false)
        {
            Maximum = Math.Max(0, maximum);
            Current = Math.Max(0, current);
            ForecastLoss = Math.Max(0, Math.Min(Current, forecastLoss));
            Remaining = Math.Max(0, Math.Min(Current, remaining));
            Uncapped = uncapped;
        }
    }

    public sealed class CombatUnitVitalsPresentation
    {
        public CombatUnitVitalPresentation Health { get; }
        public CombatUnitVitalPresentation Shield { get; }
        public CombatUnitVitalPresentation Mana { get; }
        public bool WillDefeat => Health.WillEmpty;

        private CombatUnitVitalsPresentation(CombatUnitVitalPresentation health, CombatUnitVitalPresentation shield,
            CombatUnitVitalPresentation mana)
        {
            Health = health;
            Shield = shield;
            Mana = mana;
        }

        public static CombatUnitVitalsPresentation From(UnitState unit, CombatTargetDamageForecast forecast, bool uncappedShield = false)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int healthLoss = forecast?.HealthLoss ?? 0;
            int shieldLoss = forecast?.ShieldLoss ?? 0;
            int remainingHealth = forecast?.RemainingHealth ?? unit.Health;
            int remainingShield = forecast?.RemainingShield ?? unit.Shield;
            return new CombatUnitVitalsPresentation(
                new CombatUnitVitalPresentation(unit.Health, unit.MaxHealth, healthLoss, remainingHealth),
                new CombatUnitVitalPresentation(unit.Shield, uncappedShield ? Math.Max(1, unit.Shield) : unit.MaxShield,
                    shieldLoss, remainingShield, uncappedShield),
                new CombatUnitVitalPresentation(unit.Mana, unit.MaxMana, 0, unit.Mana));
        }
    }

    public sealed class CombatStatusPresentation
    {
        public StatusType Status { get; }
        public string RuntimeId { get; }
        public string DisplayName { get; }
        public int Duration { get; }
        public int Strength { get; }
        private readonly string detail;
        public string SourceText { get; private set; }
        public int TriggerCount { get; private set; }
        public string Detail => detail + "\n来源：" + SourceText + "；已触发 " + TriggerCount + " 次";
        public string ValueText => UnitState.IsAttributeStatus(Status) ? (Strength >= 0 ? "+" : "") + Strength :
            Duration == int.MaxValue ? "本场" : Duration.ToString();

        private CombatStatusPresentation(StatusType status, string runtimeId, string displayName, int duration,
            int strength, string detail)
        {
            Status = status;
            RuntimeId = runtimeId;
            DisplayName = displayName;
            Duration = Math.Max(0, duration);
            Strength = strength;
            this.detail = detail;
        }

        public static CombatStatusPresentation From(UnitState unit, StatusType status, CombatState state = null)
        {
            CombatStatusPresentation presentation = Build(unit, status);
            string sourceId = unit.StatusSource(status);
            presentation.SourceText = string.IsNullOrEmpty(sourceId) ? "未记录" : string.Join("、",
                sourceId.Split('、').Select(id => state?.GetUnit(id)?.DisplayName ?? id));
            presentation.TriggerCount = unit.StatusTriggerCount(status);
            return presentation;
        }

        private static CombatStatusPresentation Build(UnitState unit, StatusType status)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int duration = unit.StatusDuration(status);
            int strength = unit.StatusStrength(status);
            switch (status)
            {
                case StatusType.Burning:
                    strength = unit.StatusStrength(status, CombatStatusLifecycle.BurningDamagePerTurn);
                    return new CombatStatusPresentation(status, "burning", "燃烧", duration, strength,
                        "自身回合结束时失去 " + strength + " 点生命，无视护盾；下次触发：自身回合结束。剩余 " + duration + " 次触发。重复施加只刷新持续量，不叠加强度。");
                case StatusType.Slow:
                    return new CombatStatusPresentation(status, "slow", "迟缓", duration, strength,
                        "速度降低 3，下一次自身行动的移动步数降低。下次计时：自身回合开始。剩余 " + duration + " 回合。");
                case StatusType.Bound:
                    // 束缚在目标自身回合开始衰减：构造值比"实际受缚回合数"多 1，展示时按生效回合数给出。
                    int boundRounds = Math.Max(1, duration - 1);
                    return new CombatStatusPresentation(status, "bound", "束缚", boundRounds, strength,
                        "无法主动移动；目标自身回合开始扣减。剩余 " + boundRounds + " 个可受限回合。");
                case StatusType.ArmorBreak:
                    int armorLoss = unit.StatusStrength(status, 2);
                    return new CombatStatusPresentation(status, "armor_break", "破甲", duration, armorLoss,
                        "护甲降低 " + armorLoss + "。剩余 " + duration + " 回合。");
                case StatusType.Dazzled:
                    return new CombatStatusPresentation(status, "dazzled", "目眩", duration, strength,
                        "受到目眩标记。剩余 " + duration + " 回合。");
                case StatusType.Revealed:
                    return new CombatStatusPresentation(status, "revealed", "显露", duration, strength,
                        "已被侦测。剩余 " + duration + " 回合。");
                case StatusType.BreakStance:
                    return new CombatStatusPresentation(status, "break_stance", "破势", duration, strength,
                        "施加时立即清空护盾；到下一次自身回合结束前不能获得护盾。不降低护甲。");
                case StatusType.FiregroundBoost:
                    return new CombatStatusPresentation(status, "fireground_boost", "火势", duration, strength,
                        "火场来源伤害 +" + strength + "。剩余 " + duration + " 回合。");
                case StatusType.FiregroundVulnerable:
                    return new CombatStatusPresentation(status, "fireground_vulnerable", "助燃", duration, strength,
                        "受到的火场伤害 +" + strength + "。剩余 " + duration + " 回合。");
                case StatusType.Agility: return Attribute(status, "agility", "敏捷", "有效步数", strength, duration, "格");
                case StatusType.Strength: return Attribute(status, "strength", "力量", "物理伤害", strength, duration, "点");
                case StatusType.SpellPower: return Attribute(status, "spell_power", "法强", "奥术、火焰与以太伤害", strength, duration, "点");
                case StatusType.Speed: return Attribute(status, "speed", "速度", "行动条速度", strength, duration, "点");
                case StatusType.Range: return Attribute(status, "range", "射程", "攻击与术式射程", strength, duration, "格");
                case StatusType.DamageTaken: return Attribute(status, "damage_taken", "承伤", "受到的伤害", strength, duration, "点");
                case StatusType.ShieldEfficiency: return Attribute(status, "shield_efficiency", "护盾效能", "自身每次获得护盾", strength, duration, "点");
                case StatusType.ShieldGrant: return Attribute(status, "shield_grant", "护盾授予", "给予他人的护盾", strength, duration, "点");
                case StatusType.Control: return Attribute(status, "control", "控制", "施加的束缚、敏捷与标记时长", strength, duration, "回合");
                case StatusType.Marked:
                    return new CombatStatusPresentation(status, "marked", "标记", duration, strength,
                        "藏身无效，可被标记效果读取。剩余 " + duration + " 个自身回合；到期或净化移除。");
                case StatusType.Prepared:
                    return new CombatStatusPresentation(status, "prepared", "架设", duration, strength,
                        "允许使用要求架设的攻击；攻击后、移动后或主动解除时移除。");
                case StatusType.Invulnerable:
                    return new CombatStatusPresentation(status, "invulnerable", "霸体", duration, strength,
                        "持续期间不会被打倒、推离或阻挡。剩余 " + duration + " 个自身回合。");
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown combat status.");
            }
        }

        private static CombatStatusPresentation Attribute(StatusType status, string id, string name,
            string affected, int strength, int duration, string unitName)
        {
            string signed = (strength >= 0 ? "+" : string.Empty) + strength;
            string timing = duration == int.MaxValue ? "持续至本场战斗结束" : "剩余 " + duration + " 个自身回合，回合结束扣减";
            return new CombatStatusPresentation(status, id, name, duration, strength,
                affected + " " + signed + " " + unitName + "；" + timing + "。同名数值相加。");
        }
    }
}
