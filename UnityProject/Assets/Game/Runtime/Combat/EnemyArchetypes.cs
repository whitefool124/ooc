using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class EnemyTactics
    {
        public static CombatCommand Choose(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (enemy == null || hero == null) throw new ArgumentNullException(enemy == null ? nameof(enemy) : nameof(hero));
            SkillDefinition skill = enemy.SkillOne;
            if (enemy.EnemyArchetypeId == "barrier_mender" && CanCast(enemy, skill))
            {
                UnitState repairTarget = state.Units.Values.Where(unit => unit.IsAlive && unit.IsHero == enemy.IsHero && unit.MaxShield > unit.Shield &&
                        enemy.Position.ManhattanDistance(unit.Position) <= skill.Range && HasLineOfSight(state, enemy, unit, skill))
                    .OrderByDescending(unit => unit.MaxShield - unit.Shield).ThenBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
                if (repairTarget != null) return CombatCommand.UseSkill(enemy.Id, 0, repairTarget.Id);
                return ChooseWeaponOrMove(enemy, hero);
            }
            StatusType? desiredStatus = DesiredStatus(enemy.EnemyArchetypeId);
            if (desiredStatus.HasValue)
            {
                if (!hero.HasStatus(desiredStatus.Value) && CanTarget(state, enemy, hero, skill))
                    return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
                return ChooseWeaponOrMove(enemy, hero);
            }
            if (CanTarget(state, enemy, hero, skill)) return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            return Choose(enemy, hero);
        }

        public static CombatCommand Choose(UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) throw new ArgumentNullException(enemy == null ? nameof(enemy) : nameof(hero));
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            SkillDefinition skill = enemy.SkillOne;
            if (skill != null && (skill.TargetRule == SkillTargetRule.EnemyUnit || skill.TargetRule == SkillTargetRule.AnyUnit) &&
                distance <= skill.Range && enemy.Mana >= skill.ManaCost && enemy.IsSkillReady(skill))
                return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            return ChooseWeaponOrMove(enemy, hero);
        }

        private static CombatCommand ChooseWeaponOrMove(UnitState enemy, UnitState hero)
        {
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            WeaponDefinition weapon = enemy.MainHand ?? CombatCatalog.Rifle;
            if (distance <= weapon.Range) return CombatCommand.Attack(enemy.Id, hero.Id);
            GridPosition step = new GridPosition(enemy.Position.X + Math.Sign(hero.Position.X - enemy.Position.X), enemy.Position.Y);
            if (step == enemy.Position) step = new GridPosition(enemy.Position.X, enemy.Position.Y + Math.Sign(hero.Position.Y - enemy.Position.Y));
            Facing facing = hero.Position.X > enemy.Position.X ? Facing.East : hero.Position.X < enemy.Position.X ? Facing.West :
                hero.Position.Y > enemy.Position.Y ? Facing.North : Facing.South;
            return CombatCommand.Move(enemy.Id, step, facing);
        }

        private static bool CanCast(UnitState source, SkillDefinition skill) => skill != null && source.Mana >= skill.ManaCost && source.IsSkillReady(skill);
        private static bool CanTarget(CombatState state, UnitState source, UnitState target, SkillDefinition skill) => CanCast(source, skill) &&
            source.Position.ManhattanDistance(target.Position) <= skill.Range && HasLineOfSight(state, source, target, skill);
        private static bool HasLineOfSight(CombatState state, UnitState source, UnitState target, SkillDefinition skill) => skill.Range <= 1 ||
            skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.Map.HasLineOfSight(source.Position, target.Position);
        private static StatusType? DesiredStatus(string archetypeId)
        {
            switch (archetypeId)
            {
                case "shieldguard": return StatusType.Slow;
                case "pyromancer": return StatusType.Burning;
                case "raider": return StatusType.Bound;
                case "elite_vanguard":
                case "sigil_mauler":
                case "lantern_revealer": return StatusType.ArmorBreak;
                case "tether_hound":
                case "stone_snare": return StatusType.Bound;
                default: return null;
            }
        }
    }

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
        public string ArtId { get; }
        public SkillDefinition PrimarySkill { get; }
        public SkillDefinition SecondarySkill { get; }

        public EnemyArchetype(string id, string displayName, int armor, int shield, int block, int speed, WeaponDefinition weapon, bool isElite = false, int maxHealth = 12,
            string artId = null, SkillDefinition primarySkill = null, SkillDefinition secondarySkill = null)
        { Id = id; DisplayName = displayName; Armor = armor; Shield = shield; Block = block; Speed = speed; Weapon = weapon; IsElite = isElite; MaxHealth = maxHealth; ArtId = artId ?? id; PrimarySkill = primarySkill; SecondarySkill = secondarySkill; }

        public void Apply(UnitState unit)
        {
            unit.AssignEnemyArchetype(Id); unit.DisplayName = DisplayName; unit.ConfigureVitality(MaxHealth); unit.Armor = Armor; unit.Block = Block; unit.Speed = Speed;
            unit.Equip(Weapon, CombatCatalog.Shield, PrimarySkill ?? CombatCatalog.FireBolt, SecondarySkill ?? CombatCatalog.FrostBind);
            // Archetype shield values are target totals, not bonuses over UnitState's base shield.
            if (unit.Shield > Shield) unit.AbsorbShield(unit.Shield - Shield);
            else unit.RestoreShield(Shield - unit.Shield);
        }
    }

    public static class EnemyArchetypes
    {
        public static readonly IReadOnlyList<EnemyArchetype> All = new[]
        {
            new EnemyArchetype("shieldguard", "铭盾卫", 2, 2, 2, 7, CombatCatalog.Shield, artId: "shieldguard", primarySkill: EnemyAbilityCatalog.ShieldRam),
            new EnemyArchetype("pyromancer", "火矢术师", 0, 1, 0, 9, CombatCatalog.Wand, artId: "pyromancer", primarySkill: CombatCatalog.FireBolt),
            new EnemyArchetype("raider", "钩刃突袭者", 0, 0, 1, 11, CombatCatalog.Hammer, artId: "raider", primarySkill: EnemyAbilityCatalog.HookingStrike),
            new EnemyArchetype("breaker", "破甲兵", 1, 0, 0, 8, CombatCatalog.Hammer, artId: "raider"),
            new EnemyArchetype("warden", "结界卫士", 1, 4, 1, 7, CombatCatalog.Wand, artId: "shieldguard"),
            new EnemyArchetype("binder", "束缚术士", 0, 2, 0, 8, CombatCatalog.Wand, artId: "pyromancer", primarySkill: CombatCatalog.FrostBind),
            new EnemyArchetype("elite_vanguard", "刻阵先锋", 2, 4, 2, 10, CombatCatalog.Hammer, true, artId: "elite", primarySkill: EnemyAbilityCatalog.VanguardCrush),
            new EnemyArchetype("core_overseer", "核心守备监工", 3, 4, 2, 8, CombatCatalog.Hammer, true, 30, "elite"),
            new EnemyArchetype("purifier_overseer", "以太净化监工", 1, 6, 1, 9, CombatCatalog.Wand, true, 26, "elite"),
            new EnemyArchetype("sigil_mauler", "刻印锤手", 1, 0, 0, 8, CombatCatalog.Hammer, maxHealth: 14, artId: "sigil_mauler", primarySkill: EnemyAbilityCatalog.SunderingSigil),
            new EnemyArchetype("barrier_mender", "屏障修补师", 0, 4, 0, 7, CombatCatalog.Wand, maxHealth: 12, artId: "barrier_mender", primarySkill: EnemyAbilityCatalog.WardMend),
            new EnemyArchetype("tether_hound", "缚环猎兽", 0, 0, 0, 11, EnemyAbilityCatalog.TetherHoundBite, maxHealth: 10, artId: "tether_hound", primarySkill: EnemyAbilityCatalog.TetherPounce),
            new EnemyArchetype("stone_snare", "石索缚师", 0, 1, 0, 8, CombatCatalog.Wand, maxHealth: 11, artId: "stone_snare", primarySkill: EnemyAbilityCatalog.StoneSnare),
            new EnemyArchetype("lantern_revealer", "显影灯使", 0, 2, 0, 9, CombatCatalog.Wand, maxHealth: 11, artId: "lantern_revealer", primarySkill: EnemyAbilityCatalog.RevealingLantern),
            new EnemyArchetype("rune_arbalist", "重弩手", 1, 0, 0, 6, EnemyAbilityCatalog.HeavyCrossbow, maxHealth: 13, artId: "rune_arbalist", primarySkill: EnemyAbilityCatalog.WindlassBolt)
        };

        public static EnemyArchetype Get(string id)
        {
            foreach (EnemyArchetype archetype in All) if (string.Equals(archetype.Id, id, StringComparison.Ordinal)) return archetype;
            throw new KeyNotFoundException("Unknown enemy archetype: " + id);
        }
    }
}
