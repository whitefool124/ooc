using System;

namespace OCC.Combat
{
    public sealed class CombatUnitVitalPresentation
    {
        public int Current { get; }
        public int Maximum { get; }
        public int ForecastLoss { get; }
        public int Remaining { get; }
        public bool WillEmpty => Current > 0 && Remaining <= 0 && ForecastLoss > 0;
        public float CurrentRatio => Current / (float)Math.Max(1, Maximum);
        public float RemainingRatio => Remaining / (float)Math.Max(1, Maximum);

        public string CompactText
        {
            get
            {
                if (ForecastLoss <= 0) return Current + "/" + Maximum;
                return Current + " -" + ForecastLoss + " → " + Remaining + "/" + Maximum;
            }
        }

        public CombatUnitVitalPresentation(int current, int maximum, int forecastLoss, int remaining)
        {
            Maximum = Math.Max(0, maximum);
            Current = Math.Max(0, current);
            ForecastLoss = Math.Max(0, Math.Min(Current, forecastLoss));
            Remaining = Math.Max(0, Math.Min(Current, remaining));
        }
    }

    public sealed class CombatUnitVitalsPresentation
    {
        public CombatUnitVitalPresentation Health { get; }
        public CombatUnitVitalPresentation Shield { get; }
        public bool WillDefeat => Health.WillEmpty;

        private CombatUnitVitalsPresentation(CombatUnitVitalPresentation health, CombatUnitVitalPresentation shield)
        {
            Health = health;
            Shield = shield;
        }

        public static CombatUnitVitalsPresentation From(UnitState unit, CombatTargetDamageForecast forecast)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int healthLoss = forecast?.HealthLoss ?? 0;
            int shieldLoss = forecast?.ShieldLoss ?? 0;
            int remainingHealth = forecast?.RemainingHealth ?? unit.Health;
            int remainingShield = forecast?.RemainingShield ?? unit.Shield;
            return new CombatUnitVitalsPresentation(
                new CombatUnitVitalPresentation(unit.Health, unit.MaxHealth, healthLoss, remainingHealth),
                new CombatUnitVitalPresentation(unit.Shield, unit.MaxShield, shieldLoss, remainingShield));
        }
    }

    public sealed class CombatStatusPresentation
    {
        public StatusType Status { get; }
        public string RuntimeId { get; }
        public string DisplayName { get; }
        public int Duration { get; }
        public int Strength { get; }
        public string Detail { get; }
        public string ValueText => Duration.ToString();

        private CombatStatusPresentation(StatusType status, string runtimeId, string displayName, int duration,
            int strength, string detail)
        {
            Status = status;
            RuntimeId = runtimeId;
            DisplayName = displayName;
            Duration = Math.Max(0, duration);
            Strength = Math.Max(0, strength);
            Detail = detail;
        }

        public static CombatStatusPresentation From(UnitState unit, StatusType status)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int duration = unit.StatusDuration(status);
            int strength = unit.StatusStrength(status);
            switch (status)
            {
                case StatusType.Burning:
                    return new CombatStatusPresentation(status, "burning", "燃烧", duration, strength,
                        "回合开始时失去 " + CombatStatusLifecycle.BurningDamagePerTurn + " 点生命，无视护盾。剩余 " + duration + " 回合。");
                case StatusType.Slow:
                    return new CombatStatusPresentation(status, "slow", "迟缓", duration, strength,
                        "速度降低 3。剩余 " + duration + " 回合。");
                case StatusType.Bound:
                    return new CombatStatusPresentation(status, "bound", "束缚", duration, strength,
                        "无法移动。剩余 " + duration + " 回合。");
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown combat status.");
            }
        }
    }
}
