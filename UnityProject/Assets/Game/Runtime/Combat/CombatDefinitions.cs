using System;

namespace OCC.Combat
{
    public enum DamageType { Physical, Fire, Arcane }
    public enum StatusType { Burning, Slow, Bound, ArmorBreak }
    public enum EquipmentSlot { MainHand, OffHand }

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

        public SkillDefinition(string id, string displayName, DamageType damageType, int damage, int range, int manaCost, int cooldown, StatusType? status = null, int statusDuration = 0, int initiativeDelay = 0)
        {
            Id = id; DisplayName = displayName; DamageType = damageType; Damage = damage; Range = range; ManaCost = manaCost; Cooldown = cooldown; Status = status; StatusDuration = statusDuration; InitiativeDelay = initiativeDelay;
        }
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
