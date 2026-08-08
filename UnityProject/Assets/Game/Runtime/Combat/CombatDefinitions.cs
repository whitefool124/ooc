using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum DamageType { Physical, Fire, Arcane }
    public enum StatusType { Burning, Slow, Bound, ArmorBreak, Dazzled, Revealed }
    public enum EquipmentSlot { MainHand, OffHand }

    public enum SkillTargetRule { Self, EnemyUnit, AllyUnit, AnyUnit, GridCell, Destructible }
    public enum SkillDeliveryMethod { Direct, Projectile, Area }
    public enum SkillEffectType { Damage, RestoreHealth, RestoreShield, RestoreMana, ApplyStatus, ClearStatus, MoveSource, DamageObject }
    public enum SkillEffectRecipient { PrimaryTarget, Source }
    public enum SkillModifierType { ArmorPierce, InitiativeDelay, Radius, IgnoreLineOfSight }

    public readonly struct SkillEffectDefinition
    {
        public SkillEffectType Type { get; }
        public SkillEffectRecipient Recipient { get; }
        public int Amount { get; }
        public DamageType DamageType { get; }
        public StatusType Status { get; }
        public int Duration { get; }

        private SkillEffectDefinition(SkillEffectType type, int amount, DamageType damageType, StatusType status, int duration, SkillEffectRecipient recipient)
        { Type = type; Amount = amount; DamageType = damageType; Status = status; Duration = duration; Recipient = recipient; }

        public static SkillEffectDefinition Damage(int amount, DamageType type) => new SkillEffectDefinition(SkillEffectType.Damage, amount, type, default, 0, SkillEffectRecipient.PrimaryTarget);
        public static SkillEffectDefinition RestoreHealth(int amount, SkillEffectRecipient recipient = SkillEffectRecipient.PrimaryTarget) => new SkillEffectDefinition(SkillEffectType.RestoreHealth, amount, default, default, 0, recipient);
        public static SkillEffectDefinition RestoreShield(int amount, SkillEffectRecipient recipient = SkillEffectRecipient.PrimaryTarget) => new SkillEffectDefinition(SkillEffectType.RestoreShield, amount, default, default, 0, recipient);
        public static SkillEffectDefinition RestoreMana(int amount, SkillEffectRecipient recipient = SkillEffectRecipient.Source) => new SkillEffectDefinition(SkillEffectType.RestoreMana, amount, default, default, 0, recipient);
        public static SkillEffectDefinition ApplyStatus(StatusType status, int duration) => new SkillEffectDefinition(SkillEffectType.ApplyStatus, 0, default, status, duration, SkillEffectRecipient.PrimaryTarget);
        public static SkillEffectDefinition ClearStatus(StatusType status, SkillEffectRecipient recipient = SkillEffectRecipient.PrimaryTarget) => new SkillEffectDefinition(SkillEffectType.ClearStatus, 0, default, status, 0, recipient);
        public static SkillEffectDefinition MoveSource(int distance) => new SkillEffectDefinition(SkillEffectType.MoveSource, distance, default, default, 0, SkillEffectRecipient.Source);
        public static SkillEffectDefinition DamageObject(int amount) => new SkillEffectDefinition(SkillEffectType.DamageObject, amount, default, default, 0, SkillEffectRecipient.PrimaryTarget);
    }

    public readonly struct SkillModifierDefinition
    {
        public SkillModifierType Type { get; }
        public int Value { get; }
        public SkillModifierDefinition(SkillModifierType type, int value = 1) { Type = type; Value = value; }
    }

    public sealed class WeaponDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public DamageType DamageType { get; }
        public int Damage { get; }
        public int Range { get; }
        public int ArmorPierce { get; }
        public int InitiativeDelay { get; }
        public int ManaCost { get; }

        public WeaponDefinition(string id, string displayName, DamageType damageType, int damage, int range, int armorPierce = 0, int initiativeDelay = 0, int manaCost = 0)
        {
            Id = id; DisplayName = displayName; DamageType = damageType; Damage = damage; Range = range; ArmorPierce = armorPierce; InitiativeDelay = initiativeDelay; ManaCost = manaCost;
        }
    }

    public sealed class SkillDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public DamageType DamageType { get; }
        public int Damage { get; }
        public int Range { get; }
        public int ManaCost { get; }
        public int Cooldown { get; }
        public StatusType? Status { get; }
        public int StatusDuration { get; }
        public int InitiativeDelay { get; }
        public SkillTargetRule TargetRule { get; }
        public SkillDeliveryMethod Delivery { get; }
        public IReadOnlyList<SkillEffectDefinition> Effects { get; }
        public IReadOnlyList<SkillModifierDefinition> Modifiers { get; }
        public CombatFeedbackKind PresentationKind { get; }

        public SkillDefinition(string id, string displayName, DamageType damageType, int damage, int range, int manaCost, int cooldown, StatusType? status = null, int statusDuration = 0, int initiativeDelay = 0)
        {
            Id = id; DisplayName = displayName; DamageType = damageType; Damage = damage; Range = range; ManaCost = manaCost; Cooldown = cooldown; Status = status; StatusDuration = statusDuration; InitiativeDelay = initiativeDelay;
            TargetRule = SkillTargetRule.EnemyUnit;
            Delivery = SkillDeliveryMethod.Projectile;
            List<SkillEffectDefinition> effects = new List<SkillEffectDefinition>();
            if (damage > 0) effects.Add(SkillEffectDefinition.Damage(damage, damageType));
            if (status.HasValue) effects.Add(SkillEffectDefinition.ApplyStatus(status.Value, statusDuration));
            Effects = effects;
            Modifiers = initiativeDelay > 0 ? new[] { new SkillModifierDefinition(SkillModifierType.InitiativeDelay, initiativeDelay) } : Array.Empty<SkillModifierDefinition>();
            PresentationKind = status.HasValue ? CombatFeedbackCatalog.ForStatus(status.Value) : CombatFeedbackKind.Damage;
        }

        public SkillDefinition(string id, string displayName, SkillTargetRule targetRule, SkillDeliveryMethod delivery, int range, int manaCost, int cooldown, CombatFeedbackKind presentationKind, IEnumerable<SkillEffectDefinition> effects, IEnumerable<SkillModifierDefinition> modifiers = null)
        {
            Id = id; DisplayName = displayName; TargetRule = targetRule; Delivery = delivery; Range = range; ManaCost = manaCost; Cooldown = cooldown; PresentationKind = presentationKind;
            Effects = (effects ?? throw new ArgumentNullException(nameof(effects))).ToArray();
            Modifiers = modifiers == null ? Array.Empty<SkillModifierDefinition>() : modifiers.ToArray();
            SkillEffectDefinition? damage = Effects.Where(effect => effect.Type == SkillEffectType.Damage).Select(effect => (SkillEffectDefinition?)effect).FirstOrDefault();
            SkillEffectDefinition? status = Effects.Where(effect => effect.Type == SkillEffectType.ApplyStatus).Select(effect => (SkillEffectDefinition?)effect).FirstOrDefault();
            DamageType = damage?.DamageType ?? DamageType.Physical;
            Damage = damage?.Amount ?? 0;
            Status = status?.Status;
            StatusDuration = status?.Duration ?? 0;
            InitiativeDelay = ModifierValue(SkillModifierType.InitiativeDelay);
        }

        public int ModifierValue(SkillModifierType type) => Modifiers.Where(modifier => modifier.Type == type).Select(modifier => modifier.Value).FirstOrDefault();
        public bool HasModifier(SkillModifierType type) => Modifiers.Any(modifier => modifier.Type == type);
    }

    public sealed class ConsumableDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Heal { get; }
        public int RestoreShield { get; }
        public int RestoreMana { get; }
        public StatusType? ClearStatus { get; }

        public ConsumableDefinition(string id, string displayName, int heal = 0, int restoreShield = 0, int restoreMana = 0, StatusType? clearStatus = null)
        {
            Id = id; DisplayName = displayName; Heal = heal; RestoreShield = restoreShield; RestoreMana = restoreMana; ClearStatus = clearStatus;
        }
    }

    public static class CombatCatalog
    {
        public static readonly WeaponDefinition Rifle = new WeaponDefinition("rifle", "制式步枪", DamageType.Physical, 4, 4);
        public static readonly WeaponDefinition Hammer = new WeaponDefinition("hammer", "破甲战锤", DamageType.Physical, 6, 1, 2, 3);
        public static readonly WeaponDefinition Wand = new WeaponDefinition("wand", "以太手杖", DamageType.Arcane, 3, 3);
        public static readonly WeaponDefinition Shield = new WeaponDefinition("shield", "盾牌撞击", DamageType.Physical, 2, 1);
        public static readonly SkillDefinition FireBolt = new SkillDefinition("fire_bolt", "火矢", DamageType.Fire, 5, 5, 2, 1, StatusType.Burning, 2, 4);
        public static readonly SkillDefinition FrostBind = new SkillDefinition("frost_bind", "冰缚", DamageType.Arcane, 2, 4, 2, 2, StatusType.Bound, 1);
        public static readonly ConsumableDefinition Medkit = new ConsumableDefinition("medkit", "医疗包", heal: 4);
        public static readonly ConsumableDefinition ShieldCell = new ConsumableDefinition("shield_cell", "护盾电池", restoreShield: 4, clearStatus: StatusType.Burning);
    }
}
