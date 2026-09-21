using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>
    /// 学院封存塔首领（塔之守卫）的确定性规则。
    /// 压力来自三道隔离门槛与塔内机关：机关被放行后计入维护链并提供护盾，
    /// 同时放大核心的施术规格；拆掉已放行的机关即切断它的施术介质。
    /// 阶段〇为不可打断的走流程阶段，阶段二为血量 ≤30% 的并链阶段。
    /// </summary>
    public sealed class AcademyCoreBossRuntime
    {
        /// <summary>三道隔离门槛，依次放行护障维护、显影巡查与冲压隔离。</summary>
        public static readonly int[] ThresholdKinds = { 1, 2, 3 };
        /// <summary>阶段〇持续的自身回合数。</summary>
        public const int ThresholdTurns = 3;
        /// <summary>每条已放行的维护链在核心回合开始提供的护盾。</summary>
        public const int MaintenanceShieldPerMechanism = 2;
        /// <summary>显影巡查放行后亮起的光带长度。</summary>
        public const int RevealLaneLength = 5;
        /// <summary>冲压隔离放行后的冲压线长度与回合结束伤害。</summary>
        public const int PressLineLength = 4;
        public const int PressLineDamage = 6;
        /// <summary>并链阶段每次出手时，每组存活机关额外提供的维护护盾。</summary>
        public const int ChainMaintenanceShield = 2;

        private int coreTurns;
        private readonly Dictionary<int, GridPosition> mechanismDirections = new Dictionary<int, GridPosition>();

        /// <summary>机关种类：护障维护／显影巡查／冲压隔离，与地图元素配置表一致。</summary>
        private const int MechanismWard = 1;
        private const int MechanismReveal = 2;
        private const int MechanismPress = 3;

        /// <summary>核心已经历的自身回合数。</summary>
        public int CoreTurns => coreTurns;

        /// <summary>场上仍然完好的机关数（不区分是否已放行）。</summary>
        public int SurvivingMechanismCount(CombatState state) => state.Map
            .PositionsWith(tile => tile.IsTowerMechanism && !tile.IsDestroyed).Count();

        /// <summary>已放行且完好的机关数，即当前维护链数量。</summary>
        public int ReleasedMechanismCount(CombatState state) => state.Map
            .PositionsWith(tile => tile.IsTowerMechanism && tile.IsReleased && !tile.IsDestroyed).Count();

        /// <summary>尚未放行的完好机关数。</summary>
        public int PendingMechanismCount(CombatState state) => state.Map
            .PositionsWith(tile => tile.IsTowerMechanism && !tile.IsReleased && !tile.IsDestroyed).Count();

        /// <summary>阶段：0 走流程 / 1 换装强攻 / 2 血量 ≤30% 并链。</summary>
        public int PhaseFor(CombatState state, UnitState core)
        {
            if (core == null) return 0;
            if (core.Health * 10 <= core.MaxHealth * 3) return 2;
            return coreTurns < ThresholdTurns || core.IsSuperArmored ? 0 : 1;
        }

        /// <summary>阶段〇期间核心不可被打倒：健康伤害被完全吸收。</summary>
        public bool HasSuperArmor(CombatState state, UnitState core) => PhaseFor(state, core) == 0;

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            if (enemy.EnemyArchetypeId != "core_overseer") return EnemyTactics.Choose(state, enemy, hero);

            if (PhaseFor(state, enemy) == 0)
            {
                GridPosition? threshold = NextPendingMechanism(state);
                GridPosition destination = threshold.HasValue ? StepToward(state, enemy.Position, threshold.Value) : enemy.Position;
                return destination == enemy.Position ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, destination);
            }

            int slot = PhaseFor(state, enemy) == 1 ? 0 : 1;
            SkillDefinition skill = slot == 0 ? enemy.SkillOne : enemy.SkillTwo;
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            if (skill != null && distance <= skill.Range && enemy.Mana >= skill.ManaCost && enemy.IsSkillReady(skill) &&
                (skill.Range <= 1 || skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.HasLineOfSight(enemy.Position, hero.Position)))
                return CombatCommand.UseSkill(enemy.Id, slot, hero.Id);
            if (distance <= (enemy.MainHand?.Range ?? 1)) return CombatCommand.Attack(enemy.Id, hero.Id);
            return CombatCommand.Move(enemy.Id, StepToward(state, enemy.Position, hero.Position));
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            EnemyIntentPresentation basic = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (enemy?.EnemyArchetypeId != "core_overseer") return basic;
            int phase = PhaseFor(state, enemy);
            int released = ReleasedMechanismCount(state);
            int pending = PendingMechanismCount(state);
            string phaseRule = phase == 0
                ? "阶段〇：走流程，向前一步并放行一组塔内机关；此阶段无法被打倒。"
                : phase == 1
                    ? "阶段一：使用" + (enemy.SkillOne?.DisplayName ?? "核心定向束") + "；维护单位全灭或核心半血后切换。"
                    : "阶段二：改用" + (enemy.SkillTwo?.DisplayName ?? "核心破势脉冲") + "，并与塔完成接口校准。";
            string maintenance = released == 0
                ? "维护链尚未放行，本回合开始不获得维护护盾。"
                : "已放行 " + released + " 条维护链，本回合开始获得 " + (released * MaintenanceShieldPerMechanism) + " 护盾。";
            string pendingText = pending > 0 ? " 尚有 " + pending + " 组机关未放行。" : " 三道门槛已走完。";
            return new EnemyIntentPresentation("academy-core:p" + phase + ":links" + released + ":" + basic.Signature,
                basic.ActionName, basic.TargetSummary, phaseRule + " " + maintenance + pendingText + " " + basic.ResultSummary,
                basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
        }

        internal void BeginTurn(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId != "core_overseer") return;
            unit.IsSuperArmored = false;
            int phase = PhaseFor(state, unit);
            unit.IsSuperArmored = phase == 0;
            if (phase == 0)
            {
                // 阶段〇走流程：放行一组机关后核心继续前移，方向记录随放行一起更新。
                if (pendingDirection.HasValue) mechanismDirections[0] = pendingDirection.Value;
                ReleaseNextMechanism(state);
                coreTurns++;
            }
            int amount = ReleasedMechanismCount(state) * MaintenanceShieldPerMechanism;
            if (amount > 0) state.TryGrantRogueliteShield(unit.Id, "academy-core-maintenance", amount);
        }

        /// <summary>放行下一组尚未放行的机关，返回其种类；没有可放行的机关时返回 0。</summary>
        public int ReleaseNextMechanism(CombatState state)
        {
            foreach (int kind in ThresholdKinds)
            {
                foreach (GridPosition position in state.Map.PositionsWith(tile => tile.IsTowerMechanism && tile.MechanismKind == kind))
                {
                    TileState tile = state.Map.GetTile(position);
                    if (tile.IsReleased || tile.IsDestroyed) continue;
                    tile.IsReleased = true;
                    ApplyMechanismEffect(state, position, kind);
                    return kind;
                }
            }
            return 0;
        }

        /// <summary>机关被放行时立即生效的公开效果：显影巡查亮起光带，冲压隔离公开冲压线，护障维护靠维护链持续供盾。</summary>
        private void ApplyMechanismEffect(CombatState state, GridPosition position, int kind)
        {
            UnitState hero = state.GetUnit("hero");
            GridPosition direction = hero != null && hero.IsAlive ? DirectionToward(position, hero.Position) : FieldWindState.East;
            mechanismDirections[kind] = direction;
            if (kind == MechanismReveal)
            {
                state.Environment.ReplaceLightLanes("tower-mechanism-reveal",
                    new[] { new FieldLightLaneState("tower-mechanism-reveal", position, direction, RevealLaneLength) });
                int cleared = ClearShieldsOnLane(state, position, direction, RevealLaneLength, "tower-mechanism-reveal");
                state.AddLog("显影巡查放行：亮起" + FieldWindState.DirectionName(direction) + "方向 " + RevealLaneLength +
                    " 格光带，光带上 " + cleared + " 个单位护盾被清除，被遮断处留下暗段。");
            }
            else if (kind == MechanismPress)
                state.AddLog("冲压隔离放行：公开" + FieldWindState.DirectionName(direction) + "方向 " + PressLineLength +
                    " 格冲压线，回合结束时线上单位受到 " + PressLineDamage + " 点伤害。");
            else
                state.AddLog("护障维护放行：维护链开始为核心供盾，核心回合开始获得 " + MaintenanceShieldPerMechanism + " 护盾。");
        }

        /// <summary>并链：阶段二每次出手时，场上存活的已放行机关各同时结算一次。</summary>
        internal void ObserveCommand(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId != "core_overseer" || PhaseFor(state, unit) != 2) return;
            int resolved = 0;
            foreach (GridPosition position in state.Map.PositionsWith(tile => tile.IsTowerMechanism && tile.IsReleased && !tile.IsDestroyed).ToArray())
            {
                int kind = state.Map.GetTile(position).MechanismKind;
                GridPosition direction = mechanismDirections.TryGetValue(kind, out GridPosition stored)
                    ? stored : DirectionToward(position, state.GetUnit("hero")?.Position ?? position);
                if (kind == MechanismWard)
                {
                    if (state.TryGrantRogueliteShield(unit.Id, "academy-core-chain-maintenance", ChainMaintenanceShield)) resolved++;
                }
                else if (kind == MechanismReveal)
                {
                    state.Environment.ReplaceLightLanes("tower-mechanism-reveal",
                        new[] { new FieldLightLaneState("tower-mechanism-reveal", position, direction, RevealLaneLength) });
                    ClearShieldsOnLane(state, position, direction, RevealLaneLength, "tower-mechanism-reveal");
                    resolved++;
                }
                else if (kind == MechanismPress)
                {
                    DamagePressLine(state, unit, position, direction);
                    resolved++;
                }
            }
            if (resolved > 0)
                state.AddLog("并链：核心本次出手同时带动 " + resolved + " 组塔内机关完成结算。");
        }

        /// <summary>核心回合结束：已放行的冲压线对线上单位结算一次伤害。</summary>
        internal void EndTurn(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId != "core_overseer") return;
            foreach (GridPosition position in state.Map.PositionsWith(tile => tile.IsTowerMechanism && tile.IsReleased && !tile.IsDestroyed
                && tile.MechanismKind == MechanismPress).ToArray())
            {
                GridPosition direction = mechanismDirections.TryGetValue(MechanismPress, out GridPosition stored)
                    ? stored : DirectionToward(position, state.GetUnit("hero")?.Position ?? position);
                DamagePressLine(state, unit, position, direction);
            }
        }

        /// <summary>沿光带清除护盾；光带被阻挡攻击线的物块截断，暗段内不受影响。</summary>
        private static int ClearShieldsOnLane(CombatState state, GridPosition origin, GridPosition direction, int length, string sourceId)
        {
            int cleared = 0;
            for (int step = 1; step <= length; step++)
            {
                GridPosition cell = origin + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight) break;
                foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell).ToArray())
                {
                    if (target.Shield <= 0) continue;
                    target.ClearShield();
                    cleared++;
                    state.AddLog(target.DisplayName + "位于显影光带上，护盾被清除。");
                }
            }
            return cleared;
        }

        /// <summary>冲压线：沿公开直线对线上单位结算一次伤害。</summary>
        private static void DamagePressLine(CombatState state, UnitState core, GridPosition origin, GridPosition direction)
        {
            int hits = 0;
            for (int step = 1; step <= PressLineLength; step++)
            {
                GridPosition cell = origin + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight) break;
                foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell)
                    .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray())
                {
                    Roguelite.DamagePacket packet = new Roguelite.DamagePacket("tower-mechanism-press", core.Id, target.Id,
                        "tower-mechanism-press", new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Aether, PressLineDamage) });
                    Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, target.Shield, target.Health);
                    target.AbsorbShield(damage.ShieldAbsorbed);
                    state.RecordRogueliteShieldAbsorption(target.Id, "tower-mechanism-press", damage.ShieldAbsorbed);
                    target.TakeDamage(damage.HealthDamage);
                    hits++;
                    state.AddLog("冲压线结算：" + target.DisplayName + "受到 " + PressLineDamage + " 点伤害。");
                }
            }
            if (hits == 0) state.AddLog("冲压线结算：本次没有单位位于线上。");
        }

        private static GridPosition DirectionToward(GridPosition from, GridPosition to)
        {
            int dx = to.X - from.X, dy = to.Y - from.Y;
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0) return dx > 0 ? FieldWindState.East : FieldWindState.West;
            if (dy != 0) return dy > 0 ? FieldWindState.North : FieldWindState.South;
            return FieldWindState.East;
        }

        private static GridPosition? NextPendingMechanism(CombatState state)
        {
            foreach (int kind in ThresholdKinds)
                foreach (GridPosition position in state.Map.PositionsWith(tile => tile.IsTowerMechanism && tile.MechanismKind == kind))
                {
                    TileState tile = state.Map.GetTile(position);
                    if (!tile.IsReleased && !tile.IsDestroyed) return position;
                }
            return null;
        }

        /// <summary>朝目标走一步；只返回合法空格，横纵都被挡住时原地不动。</summary>
        private static GridPosition StepToward(CombatState state, GridPosition from, GridPosition target)
        {
            int dx = Math.Sign(target.X - from.X);
            int dy = Math.Sign(target.Y - from.Y);
            GridPosition horizontal = new GridPosition(from.X + dx, from.Y);
            GridPosition vertical = new GridPosition(from.X, from.Y + dy);
            if (dx != 0 && IsFree(state, horizontal)) return horizontal;
            if (dy != 0 && IsFree(state, vertical)) return vertical;
            if (dx != 0 && IsFree(state, vertical)) return vertical;
            if (dy != 0 && IsFree(state, horizontal)) return horizontal;
            return from;
        }

        private static bool IsFree(CombatState state, GridPosition position)
        {
            if (!state.Map.IsInside(position) || state.Map.IsBlocked(position)) return false;
            foreach (UnitState unit in state.Units.Values)
                if (unit.IsAlive && unit.Position == position) return false;
            return true;
        }

        /// <summary>阶段〇这一步的方向：核心下一次放行机关时沿用，便于并链阶段复算同一方向。</summary>
        private GridPosition? pendingDirection;

        public AcademyCoreBossRuntime Clone()
        {
            AcademyCoreBossRuntime clone = new AcademyCoreBossRuntime { coreTurns = coreTurns, pendingDirection = pendingDirection };
            foreach (KeyValuePair<int, GridPosition> pair in mechanismDirections) clone.mechanismDirections[pair.Key] = pair.Value;
            return clone;
        }
    }
}
