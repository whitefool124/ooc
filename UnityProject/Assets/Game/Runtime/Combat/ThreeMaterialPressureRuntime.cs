using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    /// <summary>Deterministic first-run elite rules for the three-material pressure arena.</summary>
    public sealed class ThreeMaterialPressureRuntime
    {
        private const int ChargeBudget = 5;
        private int chargeCooldown;
        private bool vented;
        private bool shieldSuppressedThisTurn;

        public int ChargeCooldown => chargeCooldown;
        public bool IsVented => vented || shieldSuppressedThisTurn;

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            if (enemy.EnemyArchetypeId != "breach_ram") return EnemyTactics.Choose(state, enemy, hero);
            if (enemy.Position.ManhattanDistance(hero.Position) == 1) return CombatCommand.Attack(enemy.Id, hero.Id);
            if (chargeCooldown == 0) return CombatCommand.BreachCharge(enemy.Id, hero.Position);
            // 冲压冷却期间只做普通近战或换位；不得回落到通用战法选择，否则楔角会施放文档里不存在的远程术式。
            return StepOrHold(state, enemy, hero);
        }

        /// <summary>冷却期间的普通行动：能打就打，否则朝主角走一格。</summary>
        private static CombatCommand StepOrHold(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            GridPosition[] steps =
            {
                new GridPosition(enemy.Position.X + Math.Sign(hero.Position.X - enemy.Position.X), enemy.Position.Y),
                new GridPosition(enemy.Position.X, enemy.Position.Y + Math.Sign(hero.Position.Y - enemy.Position.Y))
            };
            foreach (GridPosition step in steps)
            {
                if (step == enemy.Position || !state.Map.IsInside(step) || state.Map.IsBlocked(step) || state.IsOccupied(step, enemy.Id)) continue;
                return CombatCommand.Move(enemy.Id, step);
            }
            return CombatCommand.EndTurn(enemy.Id);
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            if (enemy?.EnemyArchetypeId != "breach_ram" || command.Type != CombatCommandType.BreachCharge)
                return CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            IReadOnlyList<GridPosition> path = BuildChargePath(state, enemy, command.Destination);
            GridPosition end = path[path.Count - 1];
            string collision = CollisionSummary(state, enemy, end, command.Destination);
            int spent = PathCost(state, path);
            return new EnemyIntentPresentation("first-x:charge:" + Cell(command.Destination) + ":" + string.Join("-", path),
                "贯场冲压", "锁定 " + Cell(command.Destination),
                "路线 " + string.Join("→", path.Select(Cell)) + "；预算已用 " + spent + "，上限 5；" + collision +
                (state.Map.GetTile(end).IsWater ? "；浅水冷却，不进入卸压" : "；干地结束，清盾并进入卸压"),
                "move", true, end, 8);
        }

        public CombatEffectExecution ResolveCharge(CombatState state, UnitState ram, GridPosition lockedTarget)
        {
            if (ram == null || ram.EnemyArchetypeId != "breach_ram" || chargeCooldown != 0)
                throw new InvalidOperationException("贯场冲压当前不可用。");
            IReadOnlyList<GridPosition> path = BuildChargePath(state, ram, lockedTarget);
            GridPosition final = ram.Position;
            for (int index = 1; index < path.Count; index++)
            {
                GridPosition cell = path[index];
                UnitState collided = state.Units.Values.FirstOrDefault(value => value.IsAlive && value.Id != ram.Id && value.Position == cell);
                if (collided != null)
                {
                    ApplyDamage(state, collided, 8, "breach-ram-charge");
                    TryPush(state, collided, final, cell);
                    state.AddLog("楔角冲压命中" + collided.DisplayName + "，造成 8 点物理伤害并尝试推离。 ");
                    break;
                }
                TileState tile = state.Map.GetTile(cell);
                if (state.Map.IsBlocked(cell))
                {
                    if (tile.IsPermanentWall)
                    {
                        state.AddLog("楔角撞上 " + Cell(cell) + " 的永久墙体，冲压中止。 ");
                        break;
                    }
                    int before = tile.Durability;
                    tile.Durability = Math.Max(0, tile.Durability - 8);
                    state.ResolveAetherCrystalDamage(cell, before);
                    state.AddLog("楔角撞击 " + Cell(cell) + " 的物块，造成 8 点耐久伤害。 ");
                    break;
                }
                ram.MoveTo(cell);
                final = cell;
            }
            chargeCooldown = 2;
            bool cooled = state.Map.GetTile(ram.Position).IsWater;
            if (!cooled)
            {
                ram.ClearShield();
                vented = true;
                state.AddLog("楔角在干地卸压：护盾清空，至下一次自身回合结束前不能获得新护盾。 ");
            }
            else state.AddLog("楔角在浅水中结束冲压，冷却沟泄压。 ");
            state.EvaluateOutcome();
            return CombatEffectExecution.Empty;
        }

        internal void BeginTurn(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId != "breach_ram") return;
            shieldSuppressedThisTurn = vented;
            vented = false;
            if (chargeCooldown > 0) chargeCooldown--;
            if (!shieldSuppressedThisTurn) state.TryGrantRogueliteShield(unit.Id, "breach-ram-turn-pressure", 4);
        }

        internal void EndTurn(UnitState unit)
        {
            if (unit?.EnemyArchetypeId == "breach_ram") shieldSuppressedThisTurn = false;
        }

        public ThreeMaterialPressureRuntime Clone() => new ThreeMaterialPressureRuntime
        { chargeCooldown = chargeCooldown, vented = vented, shieldSuppressedThisTurn = shieldSuppressedThisTurn };

        private static IReadOnlyList<GridPosition> BuildChargePath(CombatState state, UnitState ram, GridPosition target)
        {
            List<GridPosition> path = new List<GridPosition> { ram.Position };
            GridPosition current = ram.Position;
            int spent = 0;
            while (current != target)
            {
                GridPosition next = current.X != target.X
                    ? new GridPosition(current.X + Math.Sign(target.X - current.X), current.Y)
                    : new GridPosition(current.X, current.Y + Math.Sign(target.Y - current.Y));
                if (!state.Map.IsInside(next)) break;
                int cost = CombatMovementQuery.EntryCost(state, ram, next);
                if (spent + cost > ChargeBudget) break;
                spent += cost;
                path.Add(next);
                if (state.Map.IsBlocked(next) || state.IsOccupied(next, ram.Id)) break;
                current = next;
            }
            return path;
        }

        private static int PathCost(CombatState state, IReadOnlyList<GridPosition> path) =>
            path.Skip(1).Sum(position => CombatMovementQuery.EntryCost(state, null, position));

        private static string CollisionSummary(CombatState state, UnitState ram, GridPosition end, GridPosition lockedTarget)
        {
            UnitState unit = state.Units.Values.FirstOrDefault(value => value.IsAlive && value.Id != ram.Id && value.Position == end);
            if (unit != null) return "首个碰撞为" + unit.DisplayName + "，预计 8 伤害并推 1 格";
            if (state.Map.GetTile(end).IsPermanentWall) return "首个碰撞为 " + Cell(end) + " 永久墙体，冲压中止";
            if (state.Map.IsBlocked(end)) return "首个碰撞为 " + Cell(end) + " 物块，预计耐久 -8";
            return end == lockedTarget ? "抵达锁定格" : "预计终点 " + Cell(end);
        }

        private static void ApplyDamage(CombatState state, UnitState target, int amount, string source)
        {
            DamageResolution damage = RogueDamageResolver.Resolve(new DamagePacket(source + "-packet", string.Empty, target.Id, source,
                new[] { new DamageComponent(DamageComponentKind.Physical, amount) }), target.Shield, target.Health);
            target.AbsorbShield(damage.ShieldAbsorbed);
            state.RecordRogueliteShieldAbsorption(target.Id, source, damage.ShieldAbsorbed);
            target.TakeDamage(damage.HealthDamage);
        }

        private static void TryPush(CombatState state, UnitState target, GridPosition previous, GridPosition collision)
        {
            if (state.ArtifactBattle?.TryPreventForcedMove(target.Id) == true) return;
            GridPosition direction = new GridPosition(collision.X - previous.X, collision.Y - previous.Y);
            GridPosition destination = target.Position + direction;
            if (state.Map.IsInside(destination) && !state.Map.IsBlocked(destination) && !state.IsOccupied(destination, target.Id))
                target.MoveTo(destination);
        }

        private static string Cell(GridPosition position) => ((char)('A' + position.X)).ToString() + (position.Y + 1);
    }
}
