using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>学院敌人的范围意图：先公开范围，经过完整主角回合后才结算。</summary>
    public sealed class AcademyEnemyAreaRuntime
    {
        public const int PrepareSkillIndex = 3;
        public const int ResolveSkillIndex = 4;

        private static readonly IReadOnlyDictionary<string, SkillDefinition> skills = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal)
        {
            ["raider"] = new SkillDefinition("SK-AOE-RAIDER", "钩刃扫切", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 1, 0, 2, CombatFeedbackKind.Bound,
                new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["shieldguard"] = new SkillDefinition("SK-AOE-SHIELDGUARD", "盾面横扫", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 1, 0, 2, CombatFeedbackKind.Attribute,
                new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Agility, 1, -1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["pyromancer"] = new SkillDefinition("SK-AOE-PYROMANCER", "爆炎散射", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 5, 0, 2, CombatFeedbackKind.Burning,
                new[] { SkillEffectDefinition.Damage(3, DamageType.Fire), SkillEffectDefinition.ApplyStatus(StatusType.Burning, 2, 2) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["rune_arbalist"] = new SkillDefinition("SK-AOE-ARBALEST", "裂矢", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 5, 0, 2, CombatFeedbackKind.Damage,
                new[] { SkillEffectDefinition.Damage(3, DamageType.Physical) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }, minimumRange: 2),
            ["tether_hound"] = new SkillDefinition("SK-AOE-HOUND", "缚环低吼", SkillTargetRule.Self,
                SkillDeliveryMethod.Area, 0, 0, 2, CombatFeedbackKind.Bound,
                new[] { SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["sigil_mauler"] = new SkillDefinition("SK-AOE-DUMMY", "锤印震环", SkillTargetRule.Self,
                SkillDeliveryMethod.Area, 0, 0, 2, CombatFeedbackKind.BreakStance,
                new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.BreakStance, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["barrier_mender"] = new SkillDefinition("SK-AOE-MENDER", "覆障投射", SkillTargetRule.AllyUnit,
                SkillDeliveryMethod.Area, 4, 0, 2, CombatFeedbackKind.ShieldRestore,
                new[] { SkillEffectDefinition.RestoreShield(2) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["stone_snare"] = new SkillDefinition("SK-AOE-SNARE", "石索封线", SkillTargetRule.GridCell,
                SkillDeliveryMethod.Area, 3, 0, 2, CombatFeedbackKind.Bound,
                new[] { SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["lantern_revealer"] = new SkillDefinition("SK-AOE-REVEALER", "双灯扫照", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 3, 0, 2, CombatFeedbackKind.Attribute,
                new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.Marked, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["signal_keeper"] = new SkillDefinition("SK-AOE-SIGNAL", "转镜扫照", SkillTargetRule.AnyUnit,
                SkillDeliveryMethod.Area, 6, 0, 2, CombatFeedbackKind.Damage,
                new[] { SkillEffectDefinition.Damage(AcademyFieldEnemyRuntime.SpotlightDamage, DamageType.Arcane) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["elite_vanguard"] = new SkillDefinition("SK-AOE-VANGUARD", "封线夯墙", SkillTargetRule.GridCell,
                SkillDeliveryMethod.Area, 3, 0, 2, CombatFeedbackKind.ShieldRestore,
                Array.Empty<SkillEffectDefinition>(),
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["prototype_hand"] = new SkillDefinition("SK-AOE-PROTOTYPE", "连锁过载", SkillTargetRule.GridCell,
                SkillDeliveryMethod.Area, int.MaxValue, 0, 2, CombatFeedbackKind.Damage,
                new[] { SkillEffectDefinition.Damage(8, DamageType.Arcane) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["wind_librarian"] = new SkillDefinition("SK-AOE-WIND", "引火", SkillTargetRule.GridCell,
                SkillDeliveryMethod.Area, int.MaxValue, 0, 2, CombatFeedbackKind.Burning,
                new[] { SkillEffectDefinition.Damage(6, DamageType.Fire) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 1) }),
            ["elder_tracker_hound"] = new SkillDefinition("SK-AOE-ELDER", "环鸣", SkillTargetRule.Self,
                SkillDeliveryMethod.Area, 0, 0, 2, CombatFeedbackKind.Attribute,
                new[] { SkillEffectDefinition.ApplyStatus(StatusType.Marked, 1) },
                new[] { new SkillModifierDefinition(SkillModifierType.Radius, 4) })
        };

        private readonly Dictionary<string, PendingArea> pending = new Dictionary<string, PendingArea>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> prototypeUses = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<GridPosition, TimedFieldLease> bindingMarkLeases = new Dictionary<GridPosition, TimedFieldLease>();
        private readonly Dictionary<GridPosition, TimedFieldLease> traceLeases = new Dictionary<GridPosition, TimedFieldLease>();
        private int completedHeroTurns;

        private readonly struct TimedFieldLease
        {
            public readonly int ExpiresAfterHeroTurns;
            public readonly string SourceId;
            public TimedFieldLease(int expiresAfterHeroTurns, string sourceId)
            { ExpiresAfterHeroTurns = expiresAfterHeroTurns; SourceId = sourceId; }
        }

        public static IReadOnlyCollection<SkillDefinition> All => skills.Values.ToArray();
        public int CompletedHeroTurns => completedHeroTurns;
        public static SkillDefinition For(string archetypeId) => archetypeId != null && skills.TryGetValue(archetypeId, out SkillDefinition skill) ? skill : null;
        public bool HasPending(string unitId) => unitId != null && pending.ContainsKey(unitId);
        public void RegisterMapField(GridPosition cell, bool bindingMark, int duration, string sourceId)
        {
            if (duration <= 0 || string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Map field needs a positive source duration and source id.");
            var lease = new TimedFieldLease(completedHeroTurns + duration, sourceId);
            if (bindingMark) bindingMarkLeases[cell] = lease;
            else traceLeases[cell] = lease;
        }
        public void HeroTurnEnded(CombatState state)
        {
            completedHeroTurns++;
            foreach (KeyValuePair<GridPosition, TimedFieldLease> entry in bindingMarkLeases.ToArray())
            {
                if (completedHeroTurns < entry.Value.ExpiresAfterHeroTurns) continue;
                GridPosition cell = entry.Key;
                if (state.Map.IsInside(cell) && state.Map.GetTile(cell).IsBindingMark &&
                    state.Map.GetTile(cell).EffectSourceId == entry.Value.SourceId)
                {
                    TileState cleared = state.Map.GetTile(cell).Clone();
                    cleared.IsBindingMark = false;
                    cleared.EffectSourceId = null;
                    state.Map.SetTile(cell, cleared);
                }
                bindingMarkLeases.Remove(cell);
            }
            foreach (KeyValuePair<GridPosition, TimedFieldLease> entry in traceLeases.ToArray())
            {
                if (completedHeroTurns < entry.Value.ExpiresAfterHeroTurns) continue;
                GridPosition cell = entry.Key;
                if (state.Map.IsInside(cell) && state.Map.GetTile(cell).HasTrace &&
                    state.Map.GetTile(cell).EffectSourceId == entry.Value.SourceId)
                {
                    TileState cleared = state.Map.GetTile(cell).Clone();
                    cleared.HasTrace = false;
                    cleared.IsDeepTrace = false;
                    cleared.EffectSourceId = null;
                    state.Map.SetTile(cell, cleared);
                }
                traceLeases.Remove(cell);
            }
        }

        public CombatCommand Choose(CombatState state, UnitState enemy, UnitState hero, CombatCommand fallback)
        {
            SkillDefinition skill = For(enemy?.EnemyArchetypeId);
            if (state == null || enemy == null || hero == null || skill == null || !enemy.IsAlive) return fallback;
            if (pending.TryGetValue(enemy.Id, out PendingArea area))
            {
                if (!CanTarget(state, enemy, skill, area.Center))
                {
                    pending.Remove(enemy.Id);
                    return fallback;
                }
                return completedHeroTurns > area.HeroTurnsAtPreparation
                    ? CombatCommand.UseSkillAt(enemy.Id, ResolveSkillIndex, area.Center,
                        skill.Id == "SK-AOE-REVEALER" ? CardinalFor(area.Side) : default)
                    : CombatCommand.EndTurn(enemy.Id);
            }
            if (enemy.EnemyArchetypeId == "rune_arbalist" &&
                (fallback.Type == CombatCommandType.Move || fallback.Type == CombatCommandType.UseSkill &&
                    fallback.SlotIndex == AcademyFieldEnemyRuntime.ArbalistArmSkillIndex))
                return fallback;
            if (enemy.EnemyArchetypeId == "elite_vanguard" && fallback.Type == CombatCommandType.UseSkill &&
                fallback.SlotIndex == AcademyFieldEnemyRuntime.VanguardDismantleSkillIndex)
                return fallback;
            if (enemy.EnemyArchetypeId == "barrier_mender" && fallback.Type == CombatCommandType.UseSkill &&
                fallback.SlotIndex == 0 && state.AcademyFieldEnemy?.IsPriorityMend(enemy.Id, fallback.TargetUnitId) == true)
                return fallback;
            if (enemy.EnemyArchetypeId == "wind_librarian" && fallback.Type == CombatCommandType.UseSkill &&
                fallback.SlotIndex == AcademyFieldEnemyRuntime.WindChangeSkillIndex)
                return fallback;
            if (skill.Id == "SK-AOE-PROTOTYPE" && PrototypeUses(enemy.Id) >= 2) return fallback;
            if (skill.Id == "SK-AOE-WIND" &&
                (state.Environment.Wind.Level <= 0 || state.RogueSpells?.FireBattle == null)) return fallback;
            GridPosition? supportCenter = skill.Id == "SK-AOE-MENDER" ? MenderCenter(state, enemy, skill) : null;
            if (skill.Id == "SK-AOE-MENDER" && !supportCenter.HasValue) return fallback;
            GridPosition? prototypeCenter = skill.Id == "SK-AOE-PROTOTYPE" ? PrototypePrimary(state, hero) : null;
            if (skill.Id == "SK-AOE-PROTOTYPE" && !prototypeCenter.HasValue) return fallback;
            GridPosition? windCenter = skill.Id == "SK-AOE-WIND" ? WindSource(state, hero.Position) : null;
            if (skill.Id == "SK-AOE-WIND" && !windCenter.HasValue) return fallback;
            GridPosition center = supportCenter ?? prototypeCenter ?? windCenter ?? (skill.TargetRule == SkillTargetRule.Self ? enemy.Position :
                IsFrontFan(skill) || skill.Id == "SK-AOE-SNARE" || skill.Id == "SK-AOE-REVEALER" ||
                    skill.Id == "SK-AOE-SIGNAL" || skill.Id == "SK-AOE-VANGUARD"
                    ? enemy.Position + DirectionToward(enemy.Position, hero.Position) : hero.Position);
            GridPosition side = skill.Id == "SK-AOE-REVEALER" ? RevealerSide(enemy.Position, center, hero.Position) :
                skill.Id == "SK-AOE-WIND" ? state.Environment.Wind.Direction : default;
            return enemy.IsSkillReady(skill) && CanTarget(state, enemy, skill, center) &&
                (skill.Id == "SK-AOE-MENDER" || skill.Id == "SK-AOE-VANGUARD" ||
                    skill.Id == "SK-AOE-WIND" && AffectedCells(state, enemy, skill, center, side).Length > 0 ||
                    (skill.Id == "SK-AOE-SNARE" ? IntendedCells(state, enemy, skill, center)
                        : AffectedCells(state, enemy, skill, center, side)).Contains(hero.Position))
                ? CombatCommand.UseSkillAt(enemy.Id, PrepareSkillIndex, center,
                    skill.Id == "SK-AOE-REVEALER" || skill.Id == "SK-AOE-WIND" ? CardinalFor(side) : default)
                : fallback;
        }

        public CombatEffectExecution Resolve(CombatState state, UnitState enemy, CombatCommand command)
        {
            SkillDefinition skill = For(enemy?.EnemyArchetypeId);
            if (state == null || enemy == null || !enemy.IsAlive || enemy.IsHero || skill == null || command.UnitId != enemy.Id)
                throw new InvalidOperationException("This academy area intent is unavailable.");
            if (command.SlotIndex == PrepareSkillIndex)
            {
                if (pending.ContainsKey(enemy.Id) || !enemy.IsSkillReady(skill) ||
                    skill.Id == "SK-AOE-PROTOTYPE" && PrototypeUses(enemy.Id) >= 2 ||
                    !CanTarget(state, enemy, skill, command.Destination))
                    throw new InvalidOperationException("The area intent cannot be prepared.");
                GridPosition side = skill.Id == "SK-AOE-REVEALER" || skill.Id == "SK-AOE-WIND"
                    ? VectorFor(command.AimDirection) : default;
                if (skill.Id == "SK-AOE-WIND" &&
                    (state.Environment.Wind.Level <= 0 || side != state.Environment.Wind.Direction ||
                        AffectedCells(state, enemy, skill, command.Destination, side).Length == 0))
                    throw new InvalidOperationException("引火需要沿当前公开风向预告可达路径。");
                GridPosition? secondary = skill.Id == "SK-AOE-PROTOTYPE"
                    ? PrototypeSecond(state, command.Destination) : null;
                CombatEffectExecution execution = CombatEffectExecutor.Execute(state, enemy.Id,
                    CombatEffect.SpendActionPoints(enemy.ActionPoints));
                pending.Add(enemy.Id, new PendingArea(command.Destination, completedHeroTurns, side, secondary));
                state.AddLog(enemy.DisplayName + "公开" + skill.DisplayName + "落点；主角完成一个回合后才结算。");
                return execution;
            }
            if (command.SlotIndex != ResolveSkillIndex || !pending.TryGetValue(enemy.Id, out PendingArea prepared) ||
                prepared.Center != command.Destination || completedHeroTurns <= prepared.HeroTurnsAtPreparation ||
                !CanTarget(state, enemy, skill, prepared.Center))
                throw new InvalidOperationException("The telegraphed area intent is not ready.");
            CombatEffectExecution resolved = skill.Id == "SK-AOE-SNARE"
                ? ResolveBindingLine(state, enemy, skill, prepared.Center)
                : skill.Id == "SK-AOE-REVEALER"
                    ? ResolveRevealer(state, enemy, skill, prepared.Center, prepared.Side)
                : skill.Id == "SK-AOE-SIGNAL"
                    ? ResolveSignal(state, enemy, skill, prepared.Center)
                : skill.Id == "SK-AOE-VANGUARD"
                    ? ResolveVanguardWall(state, enemy, skill, prepared.Center)
                : skill.Id == "SK-AOE-PROTOTYPE"
                    ? ResolvePrototypeChain(state, enemy, skill, prepared.Center, prepared.Secondary)
                : skill.Id == "SK-AOE-WIND"
                    ? ResolveWindFire(state, enemy, skill, prepared.Center, prepared.Side)
                : CombatResolver.ResolveConfiguredEnemyArea(state, enemy, skill,
                    prepared.Center, AffectedCells(state, enemy, skill, prepared.Center));
            pending.Remove(enemy.Id);
            return resolved;
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            SkillDefinition skill = For(enemy?.EnemyArchetypeId);
            if (state == null || skill == null) return null;
            GridPosition center = pending.TryGetValue(enemy.Id, out PendingArea area) ? area.Center : command.Destination;
            GridPosition side = pending.ContainsKey(enemy.Id) ? area.Side :
                skill.Id == "SK-AOE-REVEALER" || skill.Id == "SK-AOE-WIND"
                    ? VectorFor(command.AimDirection) : default;
            GridPosition[] cells = AffectedCells(state, enemy, skill, center, side);
            GridPosition[] intended = IntendedCells(state, enemy, skill, center, side);
            if (skill.Id == "SK-AOE-PROTOTYPE")
            {
                GridPosition? secondary = pending.ContainsKey(enemy.Id) ? area.Secondary : PrototypeSecond(state, center);
                cells = PrototypeBlastCells(state, center, secondary);
                intended = cells;
            }
            string hitList = skill.Id == "SK-AOE-VANGUARD" ? string.Empty :
                string.Join("、", state.Units.Values.Where(unit => unit.IsAlive && cells.Contains(unit.Position))
                .OrderBy(unit => unit.Id, StringComparer.Ordinal)
                .Select(unit => unit.DisplayName + (skill.Id == "SK-AOE-MENDER"
                    ? "预计获得" + (2 + enemy.StatusStrength(StatusType.ShieldGrant)) + "点护盾"
                    : skill.Id == "SK-AOE-SIGNAL" ? "位于光柱内，将受到5点以太伤害"
                    : skill.Id == "SK-AOE-PROTOTYPE" ? "位于装置正交邻格，每件造成8点以太伤害"
                    : skill.Id == "SK-AOE-WIND" ? "位于引火路径，受到6点火焰伤害"
                    : "预计受到" + (skill.Id == "SK-AOE-REVEALER"
                        ? CombatResolver.PreviewLanternSweepDamage(state, enemy, unit)
                        : CombatResolver.PreviewSkillAttack(state, enemy.Id, unit.Id, skill).FinalDamage) + "点生命伤害")));
            string phase = command.SlotIndex == PrepareSkillIndex ? "本回合只预告；主角完成一个回合后结算" :
                completedHeroTurns <= area.HeroTurnsAtPreparation ? "落点已公开，等待主角完成回合" : "本回合按已公开落点结算";
            string effect = skill.Id == "SK-AOE-PYROMANCER" ? "每个命中单位受3点基础火焰伤害并燃烧2，持续2个自身回合" :
                skill.Id == "SK-AOE-ARBALEST" ? "每个命中单位受3点基础物理伤害" :
                skill.Id == "SK-AOE-RAIDER" ? "扇形内单位各受2点基础物理伤害；中心格单位额外束缚1回合" :
                skill.Id == "SK-AOE-SHIELDGUARD" ? "扇形内单位各受2点基础物理伤害并获得敏捷-1，持续1回合" :
                skill.Id == "SK-AOE-HOUND" ? "正交邻格单位各被束缚1回合" :
                skill.Id == "SK-AOE-DUMMY" ? "正交邻格单位各受2点基础物理伤害并破势" :
                skill.Id == "SK-AOE-MENDER" ? "同一结构旁的友军各获得2点基础护盾，不造成伤害" :
                skill.Id == "SK-AOE-SNARE" ? "直线空格生成持续2个主角回合的约束纹；进入者本回合留在原格" :
                skill.Id == "SK-AOE-REVEALER" ? "两条平行3格光线内单位各受1点基础奥术伤害、护盾清空并标记1回合；遮挡处不生效" :
                skill.Id == "SK-AOE-SIGNAL" ? "直线6格光柱，遮挡后为暗段；本次自身回合结束时照明格内单位各受5点以太伤害" :
                skill.Id == "SK-AOE-VANGUARD" ? "在公开直线的空格内生成最多3段耐久12的临时重掩体；不造成伤害" :
                skill.Id == "SK-AOE-PROTOTYPE" ? "至多引爆2件相邻过载装置；每件对正交邻格单位各造成8点以太伤害，敌我一致；每场最多2次" :
                skill.Id == "SK-AOE-WIND" ? "沿已公开风向移动火场最多3格；路径单位各受6点火焰伤害，只在终点留下火场" :
                "4格内单位各被标记1回合，藏身无效";
            return new EnemyIntentPresentation(skill.Id + ":" + enemy.Id + ":" + center.X + "," + center.Y,
                skill.DisplayName, skill.Id == "SK-AOE-MENDER" ? "以友军为中心，实际受益的结构相邻友军" :
                    skill.Id == "SK-AOE-SNARE" ? "公开直线3格；标出的空格可放置约束纹" :
                    skill.Id == "SK-AOE-REVEALER" ? "两条平行3格光线及遮挡后的实际受照格" :
                    skill.Id == "SK-AOE-SIGNAL" ? "正交直线6格光柱及遮挡后的实际受照格" :
                    skill.Id == "SK-AOE-VANGUARD" ? "公开直线3格；当前空格可夯墙" :
                    skill.Id == "SK-AOE-PROTOTYPE" ? "目标及相邻过载装置的全部爆炸范围" :
                    skill.Id == "SK-AOE-WIND" ? "已公开风向上的最多3格路径与实际影响格" :
                    skill.TargetRule == SkillTargetRule.Self ? "自身周围已标出的格" :
                    IsFrontFan(skill) ? "正前、左前、右前3格" : "目标格及正交相邻4格", effect + "；" + phase +
                    (skill.Id == "SK-AOE-VANGUARD" ? "；当前可夯墙 " + cells.Length + " 格" :
                        hitList.Length == 0 ? "；当前范围内没有单位" : "；当前目标：" + hitList),
                "attack", true, center, skill.Damage, attackRange: intended, affectedCells: cells);
        }

        public AcademyEnemyAreaRuntime Clone()
        {
            AcademyEnemyAreaRuntime copy = new AcademyEnemyAreaRuntime { completedHeroTurns = completedHeroTurns };
            foreach (KeyValuePair<string, PendingArea> entry in pending) copy.pending.Add(entry.Key, entry.Value);
            foreach (KeyValuePair<string, int> entry in prototypeUses) copy.prototypeUses.Add(entry.Key, entry.Value);
            foreach (KeyValuePair<GridPosition, TimedFieldLease> entry in bindingMarkLeases) copy.bindingMarkLeases.Add(entry.Key, entry.Value);
            foreach (KeyValuePair<GridPosition, TimedFieldLease> entry in traceLeases) copy.traceLeases.Add(entry.Key, entry.Value);
            return copy;
        }

        private static bool CanTarget(CombatState state, UnitState enemy, SkillDefinition skill, GridPosition center)
        {
            if (!state.Map.IsInside(center)) return false;
            if (skill.Id == "SK-AOE-PROTOTYPE")
                return state.Map.GetTile(center).IsOverloadDevice && state.Map.GetTile(center).Durability > 0;
            if (skill.Id == "SK-AOE-WIND") return state.RogueSpells?.FireBattle?.HasFireground(center) == true;
            if (skill.TargetRule == SkillTargetRule.Self) return center == enemy.Position;
            if (IsFrontFan(skill)) return enemy.Position.ManhattanDistance(center) == 1;
            if (skill.Id == "SK-AOE-SNARE") return enemy.Position.ManhattanDistance(center) == 1 &&
                AffectedCells(state, enemy, skill, center).Length > 0;
            if (skill.Id == "SK-AOE-REVEALER") return enemy.Position.ManhattanDistance(center) == 1;
            if (skill.Id == "SK-AOE-SIGNAL")
                return enemy.Position.ManhattanDistance(center) == 1 &&
                    AcademyFieldEnemyRuntime.SpotlightCells(state, enemy.Position,
                        new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y)).Length > 0;
            if (skill.Id == "SK-AOE-VANGUARD")
                return enemy.Position.ManhattanDistance(center) == 1 &&
                    AffectedCells(state, enemy, skill, center).Length > 0;
            int distance = enemy.Position.ManhattanDistance(center);
            if (skill.Id == "SK-AOE-MENDER")
                return distance <= enemy.EffectiveRange(skill.Range) &&
                    state.Units.Values.Any(unit => unit.IsAlive && unit.Id != enemy.Id && unit.IsHero == enemy.IsHero &&
                        unit.Position == center) && SharedStructureCells(state, enemy.Position, center).Count > 0;
            return distance >= skill.MinimumRange && distance <= enemy.EffectiveRange(skill.Range) &&
                (distance <= 1 || state.HasLineOfSight(enemy.Position, center));
        }

        private static GridPosition[] AffectedCells(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition center, GridPosition side = default)
        {
            if (skill.Id == "SK-AOE-REVEALER")
            {
                GridPosition direction = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
                return new[] { enemy.Position, enemy.Position + side }
                    .SelectMany(origin => RevealerLane(state, origin, direction)).Distinct().ToArray();
            }
            if (skill.Id == "SK-AOE-SIGNAL")
                return AcademyFieldEnemyRuntime.SpotlightCells(state, enemy.Position,
                    new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y));
            if (skill.Id == "SK-AOE-VANGUARD")
                return IntendedCells(state, enemy, skill, center)
                    .TakeWhile(cell => !state.Map.GetTile(cell).BlocksLineOfSight)
                    .Where(cell => !state.Map.IsBlocked(cell) && !state.IsOccupied(cell) &&
                        !state.Map.GetTile(cell).HasEffectLayer &&
                        state.Map.GetTile(cell).Cover == CoverType.None &&
                        !state.Map.GetTile(cell).IsDeviceLike && !state.Map.GetTile(cell).IsObjective).ToArray();
            if (skill.Id == "SK-AOE-PROTOTYPE")
                return PrototypeBlastCells(state, center, PrototypeSecond(state, center));
            if (skill.Id == "SK-AOE-WIND") return WindCells(state, center, side);
            if (skill.Id == "SK-AOE-SNARE")
                return IntendedCells(state, enemy, skill, center)
                    .TakeWhile(cell => !state.Map.GetTile(cell).BlocksLineOfSight)
                    .Where(cell => !state.Map.IsBlocked(cell) &&
                    !state.IsOccupied(cell) && !state.Map.GetTile(cell).HasEffectLayer &&
                    state.Map.GetTile(cell).Cover == CoverType.None && !state.Map.GetTile(cell).IsDeviceLike &&
                    !state.Map.GetTile(cell).IsObjective).ToArray();
            if (IsFrontFan(skill))
            {
                GridPosition forward = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
                GridPosition flank = new GridPosition(-forward.Y, forward.X);
                return new[] { center, center + flank, center + new GridPosition(-flank.X, -flank.Y) }
                    .Where(state.Map.IsInside).ToArray();
            }
            if (skill.Id == "SK-AOE-ELDER")
                return state.Map.PositionsWith(_ => true)
                    .Where(cell => cell.ManhattanDistance(center) <= 4).ToArray();
            GridPosition[] candidates = { center, center + new GridPosition(1, 0), center + new GridPosition(-1, 0),
                center + new GridPosition(0, 1), center + new GridPosition(0, -1) };
            if (skill.Id == "SK-AOE-MENDER")
            {
                HashSet<GridPosition> shared = SharedStructureCells(state, enemy.Position, center);
                return candidates.Where(state.Map.IsInside).Where(cell => state.Units.Values.Any(unit =>
                    unit.IsAlive && unit.Id != enemy.Id && unit.IsHero == enemy.IsHero && unit.Position == cell &&
                    Adjacent(cell).Any(shared.Contains))).ToArray();
            }
            return candidates.Where(state.Map.IsInside)
                .Where(cell => skill.TargetRule != SkillTargetRule.Self || cell != center).ToArray();
        }

        private static GridPosition[] IntendedCells(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition center, GridPosition side = default)
        {
            if (skill.Id == "SK-AOE-REVEALER")
            {
                GridPosition beamDirection = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
                return new[] { enemy.Position, enemy.Position + side }
                    .SelectMany(origin => Enumerable.Range(1, 3).Select(step => origin +
                        new GridPosition(beamDirection.X * step, beamDirection.Y * step)))
                    .Where(state.Map.IsInside).Distinct().ToArray();
            }
            if (skill.Id == "SK-AOE-SIGNAL")
            {
                GridPosition signalDirection = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
                return Enumerable.Range(1, AcademyFieldEnemyRuntime.SpotlightLength)
                    .Select(step => enemy.Position + new GridPosition(signalDirection.X * step, signalDirection.Y * step))
                    .Where(state.Map.IsInside).ToArray();
            }
            if (skill.Id == "SK-AOE-VANGUARD")
            {
                GridPosition wallDirection = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
                return Enumerable.Range(1, 3)
                    .Select(step => enemy.Position + new GridPosition(wallDirection.X * step, wallDirection.Y * step))
                    .Where(state.Map.IsInside).ToArray();
            }
            if (skill.Id == "SK-AOE-WIND")
                return Enumerable.Range(1, 3)
                    .Select(step => center + new GridPosition(side.X * step, side.Y * step))
                    .Where(state.Map.IsInside).ToArray();
            if (skill.Id != "SK-AOE-SNARE") return AffectedCells(state, enemy, skill, center);
            GridPosition direction = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
            return Enumerable.Range(1, 3).Select(step => enemy.Position +
                new GridPosition(direction.X * step, direction.Y * step)).Where(state.Map.IsInside).ToArray();
        }

        private static IEnumerable<GridPosition> RevealerLane(CombatState state, GridPosition origin, GridPosition direction)
        {
            if (!state.Map.IsInside(origin) || state.Map.GetTile(origin).BlocksLineOfSight ||
                state.Map.GetTile(origin).SmokeExpiresAt > state.CurrentTime) yield break;
            for (int step = 1; step <= 3; step++)
            {
                GridPosition cell = origin + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight ||
                    state.Map.GetTile(cell).SmokeExpiresAt > state.CurrentTime) yield break;
                yield return cell;
            }
        }

        private CombatEffectExecution ResolveRevealer(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition center, GridPosition side)
        {
            GridPosition[] cells = AffectedCells(state, enemy, skill, center, side);
            CombatEffectExecution result = CombatResolver.ResolveLanternSweep(state, enemy, cells);
            GridPosition direction = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
            state.Environment.ReplaceLightLanes(enemy.Id, new[]
            {
                new FieldLightLaneState(enemy.Id, enemy.Position, direction, 3),
                new FieldLightLaneState(enemy.Id, enemy.Position + side, direction, 3)
            });
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "双灯扫照：两条公开光线内 " +
                state.Units.Values.Count(unit => unit.IsAlive && cells.Contains(unit.Position)) + " 个单位受照。");
            return result;
        }

        private static CombatEffectExecution ResolveSignal(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition center)
        {
            if (state.AcademyFieldEnemy == null)
                throw new InvalidOperationException("转镜扫照需要灯台值守场地运行时。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            GridPosition direction = new GridPosition(center.X - enemy.Position.X, center.Y - enemy.Position.Y);
            state.AcademyFieldEnemy.ArmSpotlight(state, enemy, direction);
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "按公开方向转镜扫照；照明格将在本回合结束时结算。");
            return result;
        }

        private static CombatEffectExecution ResolveVanguardWall(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition center)
        {
            GridPosition[] cells = AffectedCells(state, enemy, skill, center);
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            foreach (GridPosition cell in cells)
                state.Map.SetTile(cell, new TileState
                { Cover = CoverType.Heavy, Durability = TileState.TemporaryHeavyCoverDurability,
                    StructureOwnerUnitId = enemy.Id });
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "沿公开直线夯起 " + cells.Length + " 段临时重掩体，耐久 " +
                TileState.TemporaryHeavyCoverDurability + "。");
            return result;
        }

        private int PrototypeUses(string unitId) =>
            prototypeUses.TryGetValue(unitId, out int count) ? count : 0;

        private static GridPosition? PrototypePrimary(CombatState state, UnitState hero) =>
            state.Map.PositionsWith(tile => tile.IsOverloadDevice && tile.Durability > 0)
                .Where(cell => cell.ManhattanDistance(hero.Position) == 1)
                .OrderByDescending(cell => PrototypeSecond(state, cell).HasValue)
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Select(cell => (GridPosition?)cell).FirstOrDefault();

        private static GridPosition? PrototypeSecond(CombatState state, GridPosition first) =>
            Adjacent(first).Where(state.Map.IsInside)
                .Where(cell => state.Map.GetTile(cell).IsOverloadDevice && state.Map.GetTile(cell).Durability > 0)
                .OrderBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Select(cell => (GridPosition?)cell).FirstOrDefault();

        private static GridPosition[] PrototypeBlastCells(CombatState state, GridPosition first, GridPosition? second) =>
            (second.HasValue ? new[] { first, second.Value } : new[] { first })
                .SelectMany(Adjacent).Where(state.Map.IsInside).Distinct().ToArray();

        private CombatEffectExecution ResolvePrototypeChain(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition first, GridPosition? second)
        {
            if (state.AcademyFieldEnemy == null || PrototypeUses(enemy.Id) >= 2)
                throw new InvalidOperationException("连锁过载当前不可用。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            int detonated = 0;
            foreach (GridPosition cell in second.HasValue ? new[] { first, second.Value } : new[] { first })
            {
                if (!state.Map.IsInside(cell) || !state.Map.GetTile(cell).IsOverloadDevice ||
                    state.Map.GetTile(cell).Durability <= 0) continue;
                state.AcademyFieldEnemy.DetonatePrototypeDevice(state, cell);
                detonated++;
            }
            prototypeUses[enemy.Id] = PrototypeUses(enemy.Id) + 1;
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "连锁过载：按公开范围引爆 " + detonated + " 件过载装置；本场剩余 " +
                (2 - PrototypeUses(enemy.Id)) + " 次。");
            return result;
        }

        private static GridPosition? WindSource(CombatState state, GridPosition hero) =>
            state.RogueSpells.FireBattle.Firegrounds.Keys
                .Where(cell => WindCells(state, cell, state.Environment.Wind.Direction).Length > 0)
                .OrderBy(cell => cell.ManhattanDistance(hero))
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Select(cell => (GridPosition?)cell).FirstOrDefault();

        private static GridPosition[] WindCells(CombatState state, GridPosition source, GridPosition direction)
        {
            var cells = new List<GridPosition>();
            for (int step = 1; step <= 3; step++)
            {
                GridPosition cell = source + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.IsBlocked(cell)) break;
                cells.Add(cell);
            }
            return cells.ToArray();
        }

        private static CombatEffectExecution ResolveWindFire(CombatState state, UnitState enemy, SkillDefinition skill,
            GridPosition source, GridPosition direction)
        {
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            GridPosition[] path = WindCells(state, source, direction);
            if (path.Length > 0)
            {
                state.RogueSpells.FireBattle.RemoveFireground(source);
                foreach (GridPosition cell in path)
                    foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell)
                        .OrderBy(unit => unit.Id, StringComparer.Ordinal))
                {
                    var packet = new Roguelite.DamagePacket("wind-librarian-kindled-fire", enemy.Id, target.Id,
                        skill.Id, new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Fire, 6) });
                    Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, target.Shield,
                        target.Health);
                    target.AbsorbShield(damage.ShieldAbsorbed);
                    state.RecordRogueliteShieldAbsorption(target.Id, skill.Id, damage.ShieldAbsorbed);
                    target.TakeDamage(damage.HealthDamage);
                }
                state.RogueSpells.FireBattle.CreateOrRefreshFireground(path[path.Length - 1], 8, 2, skill.Id, enemy.Id);
            }
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "引火：按公开" + FieldWindState.DirectionName(direction) + "风吹出 " +
                path.Length + " 格；" + (path.Length == 0 ? "路径被挡，火场留在原地。" : "仅终点留下火场。"));
            state.EvaluateOutcome();
            return result;
        }

        private static GridPosition RevealerSide(GridPosition origin, GridPosition center, GridPosition hero)
        {
            GridPosition direction = new GridPosition(center.X - origin.X, center.Y - origin.Y);
            GridPosition perpendicular = new GridPosition(-direction.Y, direction.X);
            int lateral = perpendicular.X * (hero.X - origin.X) + perpendicular.Y * (hero.Y - origin.Y);
            return lateral < 0 ? new GridPosition(-perpendicular.X, -perpendicular.Y) : perpendicular;
        }

        private static CardinalDirection CardinalFor(GridPosition direction) => direction.X > 0 ? CardinalDirection.East :
            direction.X < 0 ? CardinalDirection.West : direction.Y > 0 ? CardinalDirection.North : CardinalDirection.South;

        private static GridPosition VectorFor(CardinalDirection direction) => direction == CardinalDirection.East
            ? new GridPosition(1, 0) : direction == CardinalDirection.West ? new GridPosition(-1, 0)
            : direction == CardinalDirection.North ? new GridPosition(0, 1) : new GridPosition(0, -1);

        private CombatEffectExecution ResolveBindingLine(CombatState state, UnitState enemy, SkillDefinition skill, GridPosition center)
        {
            GridPosition[] cells = AffectedCells(state, enemy, skill, center);
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            foreach (GridPosition cell in cells)
            {
                TileState marked = state.Map.GetTile(cell).Clone();
                marked.IsBindingMark = true;
                marked.EffectSourceId = "skill:SK-AOE-SNARE";
                state.Map.SetTile(cell, marked);
                bindingMarkLeases[cell] = new TimedFieldLease(completedHeroTurns + 2, marked.EffectSourceId);
            }
            enemy.SetCooldown(skill);
            state.AddLog(enemy.DisplayName + "沿公开直线放下 " + cells.Length + " 格约束纹，持续2个主角回合。");
            return result;
        }

        private static GridPosition? MenderCenter(CombatState state, UnitState enemy, SkillDefinition skill) =>
            state.Units.Values.Where(unit => unit.IsAlive && unit.Id != enemy.Id && unit.IsHero == enemy.IsHero &&
                unit.Shield < unit.MaxShield && CanTarget(state, enemy, skill, unit.Position))
                .OrderByDescending(unit => unit.MaxShield - unit.Shield).ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .Select(unit => (GridPosition?)unit.Position).FirstOrDefault();

        private static HashSet<GridPosition> SharedStructureCells(CombatState state, GridPosition source, GridPosition target)
        {
            HashSet<GridPosition> shared = new HashSet<GridPosition>();
            HashSet<GridPosition> visited = new HashSet<GridPosition>();
            foreach (GridPosition start in Adjacent(source).Where(state.Map.IsInside).Where(cell => IsStructure(state.Map.GetTile(cell))))
            {
                if (!visited.Add(start)) continue;
                Queue<GridPosition> frontier = new Queue<GridPosition>();
                HashSet<GridPosition> component = new HashSet<GridPosition> { start };
                frontier.Enqueue(start);
                while (frontier.Count > 0)
                    foreach (GridPosition next in Adjacent(frontier.Dequeue()).Where(state.Map.IsInside)
                        .Where(cell => IsStructure(state.Map.GetTile(cell))))
                        if (visited.Add(next)) { frontier.Enqueue(next); component.Add(next); }
                if (Adjacent(target).Any(component.Contains)) shared.UnionWith(component);
            }
            return shared;
        }

        internal static bool SharesStructure(CombatState state, UnitState source, UnitState target) =>
            state != null && source != null && target != null &&
            SharedStructureCells(state, source.Position, target.Position).Count > 0;

        private static bool IsStructure(TileState tile) => tile.IsPermanentWall ||
            tile.Cover != CoverType.None && !tile.IsDestroyed;

        private static GridPosition[] Adjacent(GridPosition cell) => new[]
        { cell + new GridPosition(1, 0), cell + new GridPosition(-1, 0),
            cell + new GridPosition(0, 1), cell + new GridPosition(0, -1) };

        private static bool IsFrontFan(SkillDefinition skill) => skill.Id == "SK-AOE-RAIDER" ||
            skill.Id == "SK-AOE-SHIELDGUARD";

        private static GridPosition DirectionToward(GridPosition from, GridPosition to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            return Math.Abs(dx) >= Math.Abs(dy) && dx != 0
                ? new GridPosition(Math.Sign(dx), 0) : new GridPosition(0, Math.Sign(dy));
        }

        private readonly struct PendingArea
        {
            public GridPosition Center { get; }
            public int HeroTurnsAtPreparation { get; }
            public GridPosition Side { get; }
            public GridPosition? Secondary { get; }
            public PendingArea(GridPosition center, int heroTurnsAtPreparation, GridPosition side = default,
                GridPosition? secondary = null)
            { Center = center; HeroTurnsAtPreparation = heroTurnsAtPreparation; Side = side; Secondary = secondary; }
        }
    }
}
