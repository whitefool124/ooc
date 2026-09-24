using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum EnemyResolutionKind
    {
        Generic,
        Student,
        Staff,
        Beast,
        Construct,
        /// <summary>人类高阶守卫：停止阻断后留在现场见证，不按摧毁处理。</summary>
        Witness
    }

    public static class EnemyTactics
    {
        public static CombatCommand Choose(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (enemy == null || hero == null) throw new ArgumentNullException(enemy == null ? nameof(enemy) : nameof(hero));
            if (TryChooseDecoy(state, enemy, out CombatCommand decoyCommand)) return decoyCommand;
            SkillDefinition skill = enemy.SkillOne;
            if (enemy.EnemyArchetypeId == "barrier_mender" && CanCast(enemy, skill))
            {
                UnitState repairTarget = state.Units.Values.Where(unit => unit.IsAlive && unit.IsHero == enemy.IsHero && unit.MaxShield > unit.Shield &&
                        (state.Ruleset != CombatRuleset.Roguelite || !unit.HasStatus(StatusType.BreakStance)) &&
                        enemy.Position.ManhattanDistance(unit.Position) <= enemy.EffectiveRange(skill.Range) && HasLineOfSight(state, enemy, unit, skill))
                    .OrderByDescending(unit => unit.MaxShield - unit.Shield).ThenBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
                if (repairTarget != null) return CombatCommand.UseSkill(enemy.Id, 0, repairTarget.Id);
                return ChooseWeaponOrMove(state, enemy, hero);
            }
            StatusType? desiredStatus = DesiredStatus(enemy.EnemyArchetypeId);
            if (desiredStatus.HasValue)
            {
                bool shouldApply = desiredStatus.Value == StatusType.Agility
                    ? hero.StatusStrength(StatusType.Agility) >= 0 : !hero.HasStatus(desiredStatus.Value);
                if (shouldApply && CanTarget(state, enemy, hero, skill))
                    return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
                return ChooseWeaponOrMove(state, enemy, hero);
            }
            if (CanTarget(state, enemy, hero, skill)) return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            return ChooseWeaponOrMove(state, enemy, hero);
        }

        public static CombatCommand Choose(UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) throw new ArgumentNullException(enemy == null ? nameof(enemy) : nameof(hero));
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            SkillDefinition skill = enemy.SkillOne;
            if (skill != null && (skill.TargetRule == SkillTargetRule.EnemyUnit || skill.TargetRule == SkillTargetRule.AnyUnit) &&
                distance >= skill.MinimumRange && distance <= enemy.EffectiveRange(skill.Range) && enemy.Mana >= skill.ManaCost && enemy.IsSkillReady(skill))
                return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            return ChooseWeaponOrMove(enemy, hero);
        }

        private static CombatCommand ChooseWeaponOrMove(UnitState enemy, UnitState hero)
        {
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            WeaponDefinition weapon = enemy.MainHand ?? CombatCatalog.Rifle;
            if (distance >= weapon.MinimumRange && distance <= enemy.EffectiveRange(weapon.Range)) return CombatCommand.Attack(enemy.Id, hero.Id);
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            GridPosition step = new GridPosition(enemy.Position.X + Math.Sign(hero.Position.X - enemy.Position.X), enemy.Position.Y);
            if (step == enemy.Position) step = new GridPosition(enemy.Position.X, enemy.Position.Y + Math.Sign(hero.Position.Y - enemy.Position.Y));
            return CombatCommand.Move(enemy.Id, step);
        }

        private static CombatCommand ChooseWeaponOrMove(CombatState state, UnitState enemy, UnitState hero)
        {
            WeaponDefinition weapon = enemy.MainHand ?? CombatCatalog.Rifle;
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            if (distance >= weapon.MinimumRange && distance <= enemy.EffectiveRange(weapon.Range) && (weapon.Range <= 1 || state.HasLineOfSight(enemy.Position, hero.Position)))
                return CombatCommand.Attack(enemy.Id, hero.Id);
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);

            int searchBudget = state.Map.Width * state.Map.Height * 4;
            List<IReadOnlyList<GridPosition>> paths = new List<IReadOnlyList<GridPosition>>();
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                {
                    GridPosition candidate = new GridPosition(x, y);
                    if (candidate == enemy.Position || state.Map.IsBlocked(candidate) || state.IsOccupied(candidate, enemy.Id)) continue;
                    IReadOnlyList<GridPosition> path = state.Map.FindLowestCostPath(enemy.Position, candidate, searchBudget,
                        position => CombatMovementQuery.EntryCost(state, enemy, position),
                        position => state.IsOccupied(position, enemy.Id));
                    if (path.Count > 1) paths.Add(path);
                }

            IReadOnlyList<GridPosition> route = paths
                .Where(path => path[path.Count - 1].ManhattanDistance(hero.Position) >= weapon.MinimumRange &&
                    path[path.Count - 1].ManhattanDistance(hero.Position) <= enemy.EffectiveRange(weapon.Range) &&
                    (weapon.Range <= 1 || state.HasLineOfSight(path[path.Count - 1], hero.Position)))
                .OrderBy(path => path.Count)
                .ThenBy(path => path[path.Count - 1].Y)
                .ThenBy(path => path[path.Count - 1].X)
                .FirstOrDefault();
            if (route == null)
                route = paths.OrderBy(path => path[path.Count - 1].ManhattanDistance(hero.Position))
                    .ThenBy(path => path.Count)
                    .ThenBy(path => path[path.Count - 1].Y)
                    .ThenBy(path => path[path.Count - 1].X)
                    .FirstOrDefault();
            return route == null ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, route[1]);
        }

        private static bool TryChooseDecoy(CombatState state, UnitState enemy, out CombatCommand command)
        {
            command = default;
            if (state.ArtifactBattle?.TryGetLureTarget(enemy, out GridPosition decoy) != true) return false;
            if (enemy.Position.ManhattanDistance(decoy) == 1) { command = CombatCommand.Interact(enemy.Id, decoy); return true; }
            GridPosition[] offsets = { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            IReadOnlyList<GridPosition> path = offsets.Select(offset => decoy + offset)
                .Where(position => state.Map.IsInside(position) && !state.Map.IsBlocked(position) && !state.IsOccupied(position, enemy.Id))
                .Select(position => state.Map.FindShortestPath(enemy.Position, position, 12, cell => state.IsOccupied(cell, enemy.Id)))
                .Where(candidate => candidate.Count > 1)
                .OrderBy(candidate => candidate.Count).ThenBy(candidate => candidate[candidate.Count - 1].Y).ThenBy(candidate => candidate[candidate.Count - 1].X)
                .FirstOrDefault();
            if (path == null) return false;
            command = CombatCommand.Move(enemy.Id, path[1]); return true;
        }

        private static bool CanCast(UnitState source, SkillDefinition skill) => skill != null && source.Mana >= skill.ManaCost && source.IsSkillReady(skill);
        private static bool CanTarget(CombatState state, UnitState source, UnitState target, SkillDefinition skill) => CanCast(source, skill) &&
            source.Position.ManhattanDistance(target.Position) >= skill.MinimumRange &&
            source.Position.ManhattanDistance(target.Position) <= source.EffectiveRange(skill.Range) && HasLineOfSight(state, source, target, skill);
        private static bool HasLineOfSight(CombatState state, UnitState source, UnitState target, SkillDefinition skill) => skill.Range <= 1 ||
            skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.HasLineOfSight(source.Position, target.Position);
        private static StatusType? DesiredStatus(string archetypeId)
        {
            switch (archetypeId)
            {
                case "shieldguard": return StatusType.Agility;
                case "pyromancer": return StatusType.Burning;
                case "raider": return StatusType.Bound;
                case "elite_vanguard":
                case "sigil_mauler":
                case "lantern_revealer": return StatusType.BreakStance;
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
        public EnemyResolutionKind ResolutionKind { get; }
        /// <summary>是否配置了第二技能槽。只有一套公开战法的单位不显示默认技能。</summary>
        public bool HasSecondarySkill { get; }

        public EnemyArchetype(string id, string displayName, int armor, int shield, int block, int speed, WeaponDefinition weapon, bool isElite = false, int maxHealth = 12,
            string artId = null, SkillDefinition primarySkill = null, SkillDefinition secondarySkill = null,
            EnemyResolutionKind resolutionKind = EnemyResolutionKind.Generic, bool hasSecondarySkill = false)
        { Id = id; DisplayName = displayName; Armor = armor; Shield = shield; Block = block; Speed = speed; Weapon = weapon; IsElite = isElite; MaxHealth = maxHealth; ArtId = artId ?? id; PrimarySkill = primarySkill; SecondarySkill = secondarySkill; ResolutionKind = resolutionKind; HasSecondarySkill = hasSecondarySkill; }

        public void Apply(UnitState unit)
        {
            unit.AssignEnemyArchetype(Id); unit.DisplayName = DisplayName; unit.ConfigureVitality(MaxHealth); unit.Armor = Armor; unit.Block = Block; unit.Speed = Speed;
            unit.Equip(Weapon, CombatCatalog.Shield, PrimarySkill ?? CombatCatalog.FireBolt, SecondarySkill ?? CombatCatalog.FrostBind);
            // Archetype shield values are target totals, not bonuses over UnitState's base shield.
            if (!HasSecondarySkill) unit.ClearSecondarySkill();
            if (unit.Shield > Shield) unit.AbsorbShield(unit.Shield - Shield);
            else unit.RestoreShield(Shield - unit.Shield);
        }
    }

    public static class EnemyArchetypes
    {
        public static readonly IReadOnlyList<EnemyArchetype> All = new[]
        {
            new EnemyArchetype("shieldguard", "盾术生", 2, 2, 2, 7, CombatCatalog.Shield, maxHealth: 16, artId: "shieldguard", primarySkill: EnemyAbilityCatalog.ShieldRam, resolutionKind: EnemyResolutionKind.Student),
            new EnemyArchetype("pyromancer", "火矢生", 0, 1, 0, 9, CombatCatalog.Wand, maxHealth: 16, artId: "pyromancer", primarySkill: CombatCatalog.FireBolt, resolutionKind: EnemyResolutionKind.Student),
            new EnemyArchetype("raider", "侧锋生", 0, 0, 1, 11, CombatCatalog.Hammer, maxHealth: 16, artId: "raider", primarySkill: EnemyAbilityCatalog.HookingStrike, resolutionKind: EnemyResolutionKind.Student),
            new EnemyArchetype("elite_vanguard", "划线教官", 2, 4, 2, 10, CombatCatalog.Hammer, true, 24, "elite", primarySkill: EnemyAbilityCatalog.VanguardCrush, resolutionKind: EnemyResolutionKind.Staff),
            new EnemyArchetype("core_overseer", "塔之守卫", 3, 4, 2, 8, CombatCatalog.Hammer, true, 36, "elite",
                EnemyAbilityCatalog.BossSunderMaul, EnemyAbilityCatalog.BossTowerPress, EnemyResolutionKind.Witness, hasSecondarySkill: true),
            new EnemyArchetype("sigil_mauler", "替身偶", 1, 0, 0, 8, CombatCatalog.Hammer, maxHealth: 16, artId: "sigil_mauler", primarySkill: EnemyAbilityCatalog.SunderingSigil, resolutionKind: EnemyResolutionKind.Construct),
            new EnemyArchetype("barrier_mender", "补盾助教", 0, 4, 0, 7, CombatCatalog.Wand, maxHealth: 16, artId: "barrier_mender", primarySkill: EnemyAbilityCatalog.WardMend, resolutionKind: EnemyResolutionKind.Staff),
            new EnemyArchetype("tether_hound", "寻迹兽", 0, 0, 0, 10, EnemyAbilityCatalog.TetherHoundBite, maxHealth: 12, artId: "tether_hound", primarySkill: EnemyAbilityCatalog.TetherPounce, resolutionKind: EnemyResolutionKind.Beast),
            new EnemyArchetype("stone_snare", "拴索助教", 0, 1, 0, 8, CombatCatalog.Wand, maxHealth: 16, artId: "stone_snare", primarySkill: EnemyAbilityCatalog.StoneSnare, resolutionKind: EnemyResolutionKind.Staff),
            new EnemyArchetype("lantern_revealer", "提灯巡查", 0, 2, 0, 9, CombatCatalog.Wand, maxHealth: 16, artId: "lantern_revealer", primarySkill: EnemyAbilityCatalog.RevealingLantern, resolutionKind: EnemyResolutionKind.Staff),
            new EnemyArchetype("rune_arbalist", "背弩生", 1, 0, 0, 6, EnemyAbilityCatalog.HeavyCrossbow, maxHealth: 16, artId: "rune_arbalist", primarySkill: EnemyAbilityCatalog.WindlassBolt, resolutionKind: EnemyResolutionKind.Student),
            new EnemyArchetype("breach_ram", "楔角", 0, 8, 0, 9, EnemyAbilityCatalog.BreachRam, true, 36, "sigil_mauler", resolutionKind: EnemyResolutionKind.Construct),
            new EnemyArchetype("elder_tracker_hound", "老寻", 0, 0, 0, 10, EnemyAbilityCatalog.TrackerBite, true, 14, "tether_hound",
                EnemyAbilityCatalog.TrackerSnap, EnemyAbilityCatalog.TrackerMaul, EnemyResolutionKind.Beast, hasSecondarySkill: true),
            new EnemyArchetype("signal_keeper", "灯台值守", 0, 1, 0, 8, EnemyAbilityCatalog.KeeperMirror, maxHealth: 16, artId: "lantern_revealer",
                primarySkill: EnemyAbilityCatalog.SpotlightMirror, resolutionKind: EnemyResolutionKind.Staff, hasSecondarySkill: false),
            new EnemyArchetype("wind_librarian", "小铃", 1, 2, 0, 9, EnemyAbilityCatalog.LibrarianStaff, true, 16, "pyromancer",
                primarySkill: EnemyAbilityCatalog.WindScrollEdge, resolutionKind: EnemyResolutionKind.Staff, hasSecondarySkill: false),
            new EnemyArchetype("legacy_storekeeper", "老库管", 1, 2, 0, 7, EnemyAbilityCatalog.StorekeeperStand, true, 18, "barrier_mender",
                primarySkill: EnemyAbilityCatalog.LegacyPulse, resolutionKind: EnemyResolutionKind.Staff, hasSecondarySkill: false),
            new EnemyArchetype("prototype_hand", "试制员", 1, 2, 0, 8, EnemyAbilityCatalog.PrototypeTools, true, 16, "stone_snare",
                primarySkill: EnemyAbilityCatalog.PrototypeDeploy, resolutionKind: EnemyResolutionKind.Staff, hasSecondarySkill: false)
        };

        public static EnemyArchetype Get(string id)
        {
            foreach (EnemyArchetype archetype in All) if (string.Equals(archetype.Id, id, StringComparison.Ordinal)) return archetype;
            throw new KeyNotFoundException("Unknown enemy archetype: " + id);
        }
    }

    public static class EnemyResolutionSemantics
    {
        public static string Forecast(UnitState unit)
        {
            EnemyResolutionKind kind = KindOf(unit);
            if (kind == EnemyResolutionKind.Student) return "可迫使目标认输并退出考核";
            if (kind == EnemyResolutionKind.Staff) return "可使目标失能并退出冲突";
            if (kind == EnemyResolutionKind.Beast) return "可制服目标并重新约束";
            if (kind == EnemyResolutionKind.Construct) return "可停机、封签并回收目标构造体";
            if (kind == EnemyResolutionKind.Witness) return "可使目标停止阻断并接受现场见证";
            return "可使目标失去行动能力";
        }

        public static string DefeatLog(UnitState unit)
        {
            if (unit == null) return "目标失去行动能力。";
            EnemyResolutionKind kind = KindOf(unit);
            if (kind == EnemyResolutionKind.Student) return unit.DisplayName + "认输并退出考核。";
            if (kind == EnemyResolutionKind.Staff) return unit.DisplayName + "失去战斗能力并退出冲突。";
            if (kind == EnemyResolutionKind.Beast) return unit.DisplayName + "被制服并重新约束。";
            if (kind == EnemyResolutionKind.Construct) return unit.DisplayName + "停机、封签并回收校准。";
            if (kind == EnemyResolutionKind.Witness) return unit.DisplayName + "停止阻断，留在现场见证并签字。";
            return unit.DisplayName + "失去行动能力。";
        }

        private static EnemyResolutionKind KindOf(UnitState unit)
        {
            if (unit == null || unit.IsHero || string.IsNullOrEmpty(unit.EnemyArchetypeId)) return EnemyResolutionKind.Generic;
            try { return EnemyArchetypes.Get(unit.EnemyArchetypeId).ResolutionKind; }
            catch (KeyNotFoundException) { return EnemyResolutionKind.Generic; }
        }
    }
}
