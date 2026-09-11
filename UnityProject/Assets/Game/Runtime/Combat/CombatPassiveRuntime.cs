using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum CombatPassiveSourceKind
    {
        OriginTalent,
        Equipment,
        ActiveSpell,
        PassiveSpell,
        Meal,
        Encounter
    }

    public enum CombatPassiveTrigger
    {
        BattleStart,
        OwnTurnStart,
        AfterActiveMove,
        BeforeWeaponHit,
        AfterWeaponHit,
        AfterPersonalSpellCostPaid,
        AfterPersonalSpellDamage,
        StatusApplied,
        RelevantAction,
        AttributeQuery,
        OwnTurnEnd
    }

    public enum CombatPassiveLimitScope
    {
        None,
        OwnTurn,
        Battle
    }

    public enum CombatOngoingEffectKind
    {
        NextWeaponHitDamage,
        NextTurnActionPoints,
        NextTurnMovementRange,
        NextTurnMana,
        NextTurnShield
    }

    public enum CombatOngoingEffectExpiry
    {
        UntilConsumed,
        OwnTurnEnd,
        NextOwnTurnStart
    }

    public enum CombatStatusBarEntryKind
    {
        Passive,
        OngoingEffect
    }

    public sealed class CombatPassiveDefinition
    {
        public string RuntimeId { get; }
        public string DisplayName { get; }
        public CombatPassiveSourceKind SourceKind { get; }
        public string SourceContentId { get; }
        public CombatPassiveTrigger Trigger { get; }
        public string Detail { get; }
        public int Priority { get; }
        public string ConditionId { get; }
        public string EffectId { get; }
        public int ActivationLimit { get; }
        public CombatPassiveLimitScope LimitScope { get; }

        public CombatPassiveDefinition(string runtimeId, string displayName, CombatPassiveSourceKind sourceKind,
            string sourceContentId, CombatPassiveTrigger trigger, string detail, int priority = 0,
            string conditionId = "always", string effectId = "", int activationLimit = 0,
            CombatPassiveLimitScope limitScope = CombatPassiveLimitScope.None)
        {
            if (string.IsNullOrWhiteSpace(runtimeId)) throw new ArgumentException("A passive runtime id is required.", nameof(runtimeId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A passive display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(sourceContentId)) throw new ArgumentException("A passive source id is required.", nameof(sourceContentId));
            RuntimeId = runtimeId;
            DisplayName = displayName;
            SourceKind = sourceKind;
            SourceContentId = sourceContentId;
            Trigger = trigger;
            Detail = detail ?? string.Empty;
            Priority = priority;
            ConditionId = string.IsNullOrWhiteSpace(conditionId) ? "always" : conditionId;
            EffectId = effectId ?? string.Empty;
            ActivationLimit = Math.Max(0, activationLimit);
            LimitScope = limitScope;
        }
    }

    public sealed class CombatOngoingEffectInstance
    {
        public string InstanceId { get; }
        public string OwnerUnitId { get; }
        public string DisplayName { get; }
        public string Detail { get; }
        public CombatPassiveSourceKind SourceKind { get; }
        public string SourceContentId { get; }
        public CombatOngoingEffectKind Kind { get; }
        public CombatOngoingEffectExpiry Expiry { get; }
        public int Amount { get; }
        public DamageType DamageType { get; }
        public int CreatedSequence { get; }

        internal CombatOngoingEffectInstance(string instanceId, string ownerUnitId, string displayName, string detail,
            CombatPassiveSourceKind sourceKind, string sourceContentId, CombatOngoingEffectKind kind,
            CombatOngoingEffectExpiry expiry, int amount, DamageType damageType, int createdSequence)
        {
            InstanceId = instanceId;
            OwnerUnitId = ownerUnitId;
            DisplayName = displayName;
            Detail = detail ?? string.Empty;
            SourceKind = sourceKind;
            SourceContentId = sourceContentId;
            Kind = kind;
            Expiry = expiry;
            Amount = amount;
            DamageType = damageType;
            CreatedSequence = createdSequence;
        }

        internal CombatOngoingEffectInstance Clone() => new CombatOngoingEffectInstance(InstanceId, OwnerUnitId,
            DisplayName, Detail, SourceKind, SourceContentId, Kind, Expiry, Amount, DamageType, CreatedSequence);
    }

    public sealed class CombatStatusBarEntry
    {
        public CombatStatusBarEntryKind Kind { get; }
        public string RuntimeId { get; }
        public string DisplayName { get; }
        public string Detail { get; }
        public string SourceContentId { get; }
        public string TimingText { get; }
        public int Priority { get; }

        internal CombatStatusBarEntry(CombatStatusBarEntryKind kind, string runtimeId, string displayName,
            string detail, string sourceContentId, string timingText, int priority)
        {
            Kind = kind;
            RuntimeId = runtimeId;
            DisplayName = displayName;
            Detail = detail;
            SourceContentId = sourceContentId;
            TimingText = timingText;
            Priority = priority;
        }
    }

    public readonly struct CombatDamageBonus
    {
        public int Amount { get; }
        public DamageType DamageType { get; }
        public string SourceContentId { get; }
        public string DisplayName { get; }

        internal CombatDamageBonus(CombatOngoingEffectInstance effect)
        {
            Amount = effect.Amount;
            DamageType = effect.DamageType;
            SourceContentId = effect.SourceContentId;
            DisplayName = effect.DisplayName;
        }
    }

    public sealed class CombatPassiveRuntime
    {
        private readonly Dictionary<string, CombatPassiveDefinition> passives =
            new Dictionary<string, CombatPassiveDefinition>(StringComparer.Ordinal);
        private readonly List<CombatOngoingEffectInstance> ongoingEffects = new List<CombatOngoingEffectInstance>();
        private int nextSequence;

        public IReadOnlyList<CombatPassiveDefinition> PassivesFor(string unitId) => passives
            .Where(pair => OwnerFromRuntimeId(pair.Key) == unitId)
            .Select(pair => pair.Value)
            .OrderByDescending(value => value.Priority)
            .ThenBy(value => value.RuntimeId, StringComparer.Ordinal)
            .ToArray();

        public IReadOnlyList<CombatOngoingEffectInstance> OngoingEffectsFor(string unitId) => ongoingEffects
            .Where(value => value.OwnerUnitId == unitId)
            .OrderBy(value => value.CreatedSequence)
            .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
            .ToArray();

        public bool HasOngoingEffect(string unitId, string instanceId) => ongoingEffects.Any(value =>
            value.OwnerUnitId == unitId && value.InstanceId == instanceId);

        public void RegisterPassive(string unitId, CombatPassiveDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("A passive owner is required.", nameof(unitId));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            passives[OwnedRuntimeId(unitId, definition.RuntimeId)] = definition;
        }

        public void RemovePassivesFromSource(string unitId, CombatPassiveSourceKind sourceKind)
        {
            string[] keys = passives.Where(pair => OwnerFromRuntimeId(pair.Key) == unitId && pair.Value.SourceKind == sourceKind)
                .Select(pair => pair.Key).ToArray();
            foreach (string key in keys) passives.Remove(key);
        }

        public void ArmNextWeaponDamage(string instanceId, string unitId, string displayName, string detail,
            CombatPassiveSourceKind sourceKind, string sourceContentId, int amount, DamageType damageType,
            CombatOngoingEffectExpiry expiry = CombatOngoingEffectExpiry.OwnTurnEnd)
        {
            AddOrReplace(instanceId, unitId, displayName, detail, sourceKind, sourceContentId,
                CombatOngoingEffectKind.NextWeaponHitDamage, expiry, amount, damageType);
        }

        public void ScheduleNextTurn(string instanceId, string unitId, string displayName, string detail,
            CombatPassiveSourceKind sourceKind, string sourceContentId, CombatOngoingEffectKind kind, int amount)
        {
            if (kind != CombatOngoingEffectKind.NextTurnActionPoints &&
                kind != CombatOngoingEffectKind.NextTurnMovementRange &&
                kind != CombatOngoingEffectKind.NextTurnMana && kind != CombatOngoingEffectKind.NextTurnShield)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The effect is not a next-turn attribute.");
            AddOrReplace(instanceId, unitId, displayName, detail, sourceKind, sourceContentId, kind,
                CombatOngoingEffectExpiry.NextOwnTurnStart, amount, DamageType.Physical);
        }

        public IReadOnlyList<CombatDamageBonus> ConsumeNextWeaponDamage(string unitId)
        {
            CombatOngoingEffectInstance[] consumed = ongoingEffects
                .Where(value => value.OwnerUnitId == unitId && value.Kind == CombatOngoingEffectKind.NextWeaponHitDamage)
                .OrderBy(value => value.CreatedSequence).ThenBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
            foreach (CombatOngoingEffectInstance effect in consumed) ongoingEffects.Remove(effect);
            return consumed.Select(value => new CombatDamageBonus(value)).ToArray();
        }

        public void ResolveOwnTurnStart(CombatState state, UnitState unit)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            CombatOngoingEffectInstance[] due = ongoingEffects
                .Where(value => value.OwnerUnitId == unit.Id && value.Expiry == CombatOngoingEffectExpiry.NextOwnTurnStart)
                .OrderBy(value => value.CreatedSequence).ThenBy(value => value.InstanceId, StringComparer.Ordinal).ToArray();
            foreach (CombatOngoingEffectInstance effect in due)
            {
                switch (effect.Kind)
                {
                    case CombatOngoingEffectKind.NextTurnActionPoints:
                        unit.GrantBonusActionPoints(effect.Amount);
                        break;
                    case CombatOngoingEffectKind.NextTurnMovementRange:
                        unit.SetMovementRangeForTurn(unit.MovementRangeThisTurn + effect.Amount);
                        break;
                    case CombatOngoingEffectKind.NextTurnMana:
                        unit.RestoreMana(effect.Amount);
                        break;
                    case CombatOngoingEffectKind.NextTurnShield:
                        state.TryGrantRogueliteShield(unit.Id, effect.SourceContentId, effect.Amount);
                        break;
                }
                ongoingEffects.Remove(effect);
                state.AddLog(effect.DisplayName + "触发：" + effect.Detail);
            }
        }

        public void ExpireOwnTurnEnd(string unitId)
        {
            ongoingEffects.RemoveAll(value => value.OwnerUnitId == unitId && value.Expiry == CombatOngoingEffectExpiry.OwnTurnEnd);
        }

        public IReadOnlyList<CombatStatusBarEntry> StatusBarEntriesFor(string unitId)
        {
            List<CombatStatusBarEntry> result = new List<CombatStatusBarEntry>();
            foreach (CombatOngoingEffectInstance effect in OngoingEffectsFor(unitId))
                result.Add(new CombatStatusBarEntry(CombatStatusBarEntryKind.OngoingEffect, effect.InstanceId,
                    effect.DisplayName, effect.Detail, effect.SourceContentId, Timing(effect), 100));
            foreach (CombatPassiveDefinition passive in PassivesFor(unitId))
                result.Add(new CombatStatusBarEntry(CombatStatusBarEntryKind.Passive, passive.RuntimeId,
                    passive.DisplayName, passive.Detail, passive.SourceContentId, Trigger(passive.Trigger), passive.Priority));
            return result.OrderByDescending(value => value.Priority).ThenBy(value => value.RuntimeId, StringComparer.Ordinal).ToArray();
        }

        public CombatPassiveRuntime Clone()
        {
            CombatPassiveRuntime clone = new CombatPassiveRuntime { nextSequence = nextSequence };
            foreach (KeyValuePair<string, CombatPassiveDefinition> pair in passives) clone.passives[pair.Key] = pair.Value;
            clone.ongoingEffects.AddRange(ongoingEffects.Select(value => value.Clone()));
            return clone;
        }

        private void AddOrReplace(string instanceId, string unitId, string displayName, string detail,
            CombatPassiveSourceKind sourceKind, string sourceContentId, CombatOngoingEffectKind kind,
            CombatOngoingEffectExpiry expiry, int amount, DamageType damageType)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("An ongoing effect id is required.", nameof(instanceId));
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("An ongoing effect owner is required.", nameof(unitId));
            if (string.IsNullOrWhiteSpace(sourceContentId)) throw new ArgumentException("An ongoing effect source is required.", nameof(sourceContentId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            ongoingEffects.RemoveAll(value => value.OwnerUnitId == unitId && value.InstanceId == instanceId);
            ongoingEffects.Add(new CombatOngoingEffectInstance(instanceId, unitId, displayName, detail, sourceKind,
                sourceContentId, kind, expiry, amount, damageType, nextSequence++));
        }

        private static string OwnedRuntimeId(string unitId, string runtimeId) => unitId + "|" + runtimeId;
        private static string OwnerFromRuntimeId(string value)
        {
            int separator = value.IndexOf('|');
            return separator < 0 ? string.Empty : value.Substring(0, separator);
        }

        private static string Timing(CombatOngoingEffectInstance effect)
        {
            if (effect.Kind == CombatOngoingEffectKind.NextWeaponHitDamage) return "下次武器命中";
            return "下次自己回合开始";
        }

        private static string Trigger(CombatPassiveTrigger trigger)
        {
            switch (trigger)
            {
                case CombatPassiveTrigger.BattleStart: return "进入战斗";
                case CombatPassiveTrigger.OwnTurnStart: return "自己回合开始";
                case CombatPassiveTrigger.AfterActiveMove: return "主动移动后";
                case CombatPassiveTrigger.BeforeWeaponHit: return "武器命中前";
                case CombatPassiveTrigger.AfterWeaponHit: return "武器命中后";
                case CombatPassiveTrigger.AfterPersonalSpellCostPaid: return "支付个人术式消耗后";
                case CombatPassiveTrigger.AfterPersonalSpellDamage: return "个人术式造成伤害后";
                case CombatPassiveTrigger.StatusApplied: return "受到状态时";
                case CombatPassiveTrigger.RelevantAction: return "相关行动结算时";
                case CombatPassiveTrigger.AttributeQuery: return "计算有效属性时";
                case CombatPassiveTrigger.OwnTurnEnd: return "自己回合结束";
                default: return trigger.ToString();
            }
        }
    }
}
