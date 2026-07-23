using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class EnemyArchetype
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Armor { get; }
        public int Shield { get; }
        public int Block { get; }
        public int Speed { get; }
        public WeaponDefinition Weapon { get; }
        public bool IsElite { get; }
        public int MaxHealth { get; }

        public EnemyArchetype(string id, string displayName, int armor, int shield, int block, int speed, WeaponDefinition weapon, bool isElite = false, int maxHealth = 12)
        { Id = id; DisplayName = displayName; Armor = armor; Shield = shield; Block = block; Speed = speed; Weapon = weapon; IsElite = isElite; MaxHealth = maxHealth; }

        public void Apply(UnitState unit)
        {
            unit.DisplayName = DisplayName; unit.ConfigureVitality(MaxHealth); unit.Armor = Armor; unit.Block = Block; unit.Speed = Speed;
            unit.Equip(Weapon, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            // Archetype shield values are target totals, not bonuses over UnitState's base shield.
            unit.RestoreShield(Math.Max(0, Shield - unit.Shield));
        }
    }

    public static class EnemyArchetypes
    {
        public static readonly IReadOnlyList<EnemyArchetype> All = new[]
        {
            new EnemyArchetype("rifleman", "步枪兵", 0, 1, 0, 8, CombatCatalog.Rifle),
            new EnemyArchetype("shieldguard", "盾卫", 2, 2, 2, 7, CombatCatalog.Shield),
            new EnemyArchetype("pyromancer", "火术师", 0, 1, 0, 9, CombatCatalog.Wand),
            new EnemyArchetype("raider", "突袭者", 0, 0, 1, 11, CombatCatalog.Hammer),
            new EnemyArchetype("sniper", "狙击手", 0, 1, 0, 6, CombatCatalog.Rifle),
            new EnemyArchetype("breaker", "破甲兵", 1, 0, 0, 8, CombatCatalog.Hammer),
            new EnemyArchetype("warden", "结界卫士", 1, 4, 1, 7, CombatCatalog.Wand),
            new EnemyArchetype("binder", "束缚术士", 0, 2, 0, 8, CombatCatalog.Wand),
            new EnemyArchetype("elite_vanguard", "精英先锋", 2, 4, 2, 10, CombatCatalog.Hammer, true)
            ,new EnemyArchetype("core_overseer", "核心守备监工", 3, 4, 2, 8, CombatCatalog.Hammer, true, 30)
            ,new EnemyArchetype("purifier_overseer", "以太净化监工", 1, 6, 1, 9, CombatCatalog.Wand, true, 26)
        };

        public static EnemyArchetype Get(string id)
        {
            foreach (EnemyArchetype archetype in All) if (string.Equals(archetype.Id, id, StringComparison.Ordinal)) return archetype;
            return All[0];
        }
    }
}
