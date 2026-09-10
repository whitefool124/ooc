using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>Frozen encounter rules for first-run battle two, independent of presentation.</summary>
    public sealed class GreenhouseCollectionRoomRuntime
    {
        public const string LevelId = "greenhouse_collection_room";

        private GridPosition calibratedCrystal;
        private bool hasCalibratedCrystal;
        private bool calibratedHeavyPlanned;
        private GridPosition raiderEntrance;
        private bool hasRaiderEntrance;
        private GridPosition raiderLastKnown;
        private bool raiderHidden;

        public bool IsRaiderHidden(CombatState state, UnitState unit)
        {
            if (state == null || unit == null || unit.EnemyArchetypeId != "raider" || !unit.IsAlive || !raiderHidden) return false;
            UnitState hero = state.Units.Values.FirstOrDefault(candidate => candidate.IsHero && candidate.IsAlive);
            return hero == null || unit.Position.ManhattanDistance(hero.Position) > 1;
        }
        public bool HasHiddenRaider(CombatState state) => state != null &&
            state.Units.Values.Any(unit => unit.EnemyArchetypeId == "raider" && IsRaiderHidden(state, unit));
        public GridPosition RaiderLastKnownPosition => raiderLastKnown;

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null || enemy == null || hero == null)
                return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            if (enemy.EnemyArchetypeId == "sigil_mauler") return ChooseMauler(state, enemy, hero);
            if (enemy.EnemyArchetypeId == "raider") return ChooseRaider(state, enemy, hero);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            if (enemy.EnemyArchetypeId == "sigil_mauler" && command.Type == CombatCommandType.EndTurn &&
                hasCalibratedCrystal && IsIntactCrystal(state, calibratedCrystal))
                return new EnemyIntentPresentation("first-b2:mauler:calibrate:" + calibratedCrystal,
                    "贴晶校准", Cell(calibratedCrystal), "邻接完整晶簇，准备借晶强化下一次重击。强化数值尚未冻结，当前不追加伤害。",
                    "defend", true, calibratedCrystal, 0);
            if (enemy.EnemyArchetypeId == "sigil_mauler" && command.Type == CombatCommandType.Move)
                return new EnemyIntentPresentation("first-b2:mauler:approach:" + command.Destination,
                    "贴晶校准", Cell(command.Destination), "接近最近的完整晶簇；邻接后准备下一次重击。",
                    "move", true, command.Destination, 0);
            if (enemy.EnemyArchetypeId == "sigil_mauler" && calibratedHeavyPlanned && command.Type == CombatCommandType.Attack)
                return new EnemyIntentPresentation("first-b2:mauler:heavy:" + enemy.Id,
                    "借晶重击（占位数值）", state.GetUnit(command.TargetUnitId)?.DisplayName ?? "主角",
                    "已完成贴晶校准；强化量未冻结，本次仍按现有 6 点基础重击结算。",
                    "attack", false, default, CombatInformationPresenter.BuildEnemyIntent(state, enemy, command).ExpectedDamage);
            if (enemy.EnemyArchetypeId == "raider" && command.Type == CombatCommandType.Move)
                return new EnemyIntentPresentation("first-b2:raider:flank:" + command.Destination,
                    raiderHidden ? "藤内潜行" : "侧翼入藤", Cell(command.Destination),
                    raiderHidden ? "留在灯藤中；只有本次移动能出藤并贴邻主角时才会显形。" : "前往能通向主角侧面的灯藤入口。",
                    "move", true, command.Destination, 0);
            return CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
        }

        internal void AfterMove(CombatState state, UnitState unit, IReadOnlyList<GridPosition> path)
        {
            if (unit == null || unit.EnemyArchetypeId != "raider" || path == null || path.Count < 2) return;
            bool startedInVine = state.Map.GetTile(path[0]).IsLampVine;
            GridPosition firstVine = path.Skip(1).FirstOrDefault(position => state.Map.GetTile(position).IsLampVine);
            bool enteredVine = path.Skip(1).Any(position => state.Map.GetTile(position).IsLampVine);
            bool endedInVine = state.Map.GetTile(path[path.Count - 1]).IsLampVine;
            if (!startedInVine && enteredVine)
            {
                raiderLastKnown = firstVine;
                raiderHidden = endedInVine;
                state.AddLog(unit.DisplayName + "从 " + Cell(firstVine) + " 进入灯藤，画面只保留最后已知位置。");
            }
            else if (startedInVine && !endedInVine)
            {
                raiderHidden = false;
                state.AddLog(unit.DisplayName + "离开灯藤并显形。");
            }
            else if (endedInVine) raiderHidden = true;
        }

        public GreenhouseCollectionRoomRuntime Clone()
        {
            return new GreenhouseCollectionRoomRuntime
            {
                calibratedCrystal = calibratedCrystal,
                hasCalibratedCrystal = hasCalibratedCrystal,
                calibratedHeavyPlanned = calibratedHeavyPlanned,
                raiderEntrance = raiderEntrance,
                hasRaiderEntrance = hasRaiderEntrance,
                raiderLastKnown = raiderLastKnown,
                raiderHidden = raiderHidden
            };
        }

        private CombatCommand ChooseMauler(CombatState state, UnitState enemy, UnitState hero)
        {
            calibratedHeavyPlanned = false;
            if (enemy.Position.ManhattanDistance(hero.Position) <= (enemy.MainHand ?? CombatCatalog.Hammer).Range)
            {
                calibratedHeavyPlanned = hasCalibratedCrystal && IsIntactCrystal(state, calibratedCrystal);
                hasCalibratedCrystal = false;
                return CombatCommand.Attack(enemy.Id, hero.Id);
            }
            if (hasCalibratedCrystal && !IsIntactCrystal(state, calibratedCrystal))
            {
                hasCalibratedCrystal = false;
                return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));
            }

            GridPosition? target = state.Map.PositionsWith(tile => tile.IsAetherCrystal && !tile.IsDestroyed)
                .OrderBy(position => enemy.Position.ManhattanDistance(position))
                .ThenBy(position => position.Y).ThenBy(position => position.X)
                .Select(position => (GridPosition?)position).FirstOrDefault();
            if (!target.HasValue) return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));
            if (enemy.Position.ManhattanDistance(target.Value) == 1)
            {
                calibratedCrystal = target.Value;
                hasCalibratedCrystal = true;
                return CombatCommand.EndTurn(enemy.Id);
            }
            return MoveToward(state, enemy, OpenNeighborTargets(state, target.Value, enemy.Id));
        }

        private CombatCommand ChooseRaider(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy.Position.ManhattanDistance(hero.Position) == 1)
            {
                raiderHidden = false;
                if (enemy.SkillOne != null && enemy.IsSkillReady(enemy.SkillOne) && enemy.Mana >= enemy.SkillOne.ManaCost &&
                    !hero.HasStatus(StatusType.Bound))
                    return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
                return CombatCommand.Attack(enemy.Id, hero.Id);
            }

            if (state.Map.GetTile(enemy.Position).IsLampVine)
            {
                CombatCommand exit = ExitVineAdjacentToHero(state, enemy, hero);
                if (exit.Type == CombatCommandType.Move) return exit;
                CombatCommand inside = MoveInsideVineTowardHero(state, enemy, hero);
                return inside.Type == CombatCommandType.Move ? inside : CombatCommand.EndTurn(enemy.Id);
            }

            if (!state.Map.PositionsWith(tile => tile.IsLampVine).Any())
                return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));

            if (!hasRaiderEntrance || !IsUsableEntrance(state, raiderEntrance))
            {
                GridPosition? selected = EntranceCandidates(state)
                    .Select(position => new { Position = position, Path = FullPath(state, enemy, position) })
                    .Where(candidate => candidate.Path.Count > 1)
                    .OrderBy(candidate => PathCost(state, candidate.Path))
                    .ThenBy(candidate => candidate.Position.ManhattanDistance(hero.Position))
                    .ThenBy(candidate => candidate.Position.Y).ThenBy(candidate => candidate.Position.X)
                    .Select(candidate => (GridPosition?)candidate.Position).FirstOrDefault();
                if (!selected.HasValue)
                    return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));
                raiderEntrance = selected.Value;
                hasRaiderEntrance = true;
            }

            if (enemy.Position == raiderEntrance)
            {
                GridPosition? vine = Orthogonal(raiderEntrance).Where(state.Map.IsInside)
                    .Where(position => state.Map.GetTile(position).IsLampVine && !state.IsOccupied(position, enemy.Id))
                    .OrderBy(position => position.ManhattanDistance(hero.Position)).Select(position => (GridPosition?)position).FirstOrDefault();
                if (!vine.HasValue) return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));
                return MoveToward(state, enemy, new[] { vine.Value });
            }
            return MoveToward(state, enemy, new[] { raiderEntrance });
        }

        private static CombatCommand ExitVineAdjacentToHero(CombatState state, UnitState enemy, UnitState hero)
        {
            int budget = CombatMovementQuery.Budget(state, enemy);
            IReadOnlyList<GridPosition> path = OpenNeighborTargets(state, hero.Position, enemy.Id)
                .Where(target => !state.Map.GetTile(target).IsLampVine)
                .Select(target => VineConstrainedPath(state, enemy, target, budget))
                .Where(candidate => candidate.Count > 1 && PathCost(state, candidate) <= budget)
                .OrderBy(candidate => PathCost(state, candidate)).ThenBy(candidate => candidate[candidate.Count - 1].Y)
                .ThenBy(candidate => candidate[candidate.Count - 1].X).FirstOrDefault();
            return path == null ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, path[path.Count - 1]);
        }

        private static CombatCommand MoveInsideVineTowardHero(CombatState state, UnitState enemy, UnitState hero)
        {
            int budget = CombatMovementQuery.Budget(state, enemy);
            IReadOnlyList<GridPosition> path = state.Map.PositionsWith(tile => tile.IsLampVine)
                .Where(position => position != enemy.Position && !state.IsOccupied(position, enemy.Id))
                .Select(target => VineConstrainedPath(state, enemy, target, budget))
                .Where(candidate => candidate.Count > 1)
                .OrderBy(candidate => candidate[candidate.Count - 1].ManhattanDistance(hero.Position))
                .ThenBy(candidate => PathCost(state, candidate)).FirstOrDefault();
            return path == null ? CombatCommand.EndTurn(enemy.Id) : CombatCommand.Move(enemy.Id, path[path.Count - 1]);
        }

        private static IReadOnlyList<GridPosition> VineConstrainedPath(CombatState state, UnitState enemy,
            GridPosition target, int budget) => state.Map.FindLowestCostPath(enemy.Position, target, budget,
                position => CombatMovementQuery.EntryCost(state, enemy, position),
                position => state.IsOccupied(position, enemy.Id) || position != target && !state.Map.GetTile(position).IsLampVine);

        private static CombatCommand MoveToward(CombatState state, UnitState unit, IEnumerable<GridPosition> targets)
        {
            IReadOnlyList<GridPosition> best = targets.Select((target, index) => new
                { Index = index, Path = FullPath(state, unit, target) })
                .Where(candidate => candidate.Path.Count > 1)
                .OrderBy(candidate => PathCost(state, candidate.Path)).ThenBy(candidate => candidate.Index)
                .Select(candidate => candidate.Path).FirstOrDefault();
            if (best == null) return CombatCommand.EndTurn(unit.Id);
            int budget = CombatMovementQuery.Budget(state, unit);
            int spent = 0;
            int destinationIndex = 0;
            for (int index = 1; index < best.Count; index++)
            {
                int cost = CombatMovementQuery.EntryCost(state, unit, best[index]);
                if (spent + cost > budget) break;
                spent += cost;
                destinationIndex = index;
            }
            return destinationIndex == 0 ? CombatCommand.EndTurn(unit.Id) : CombatCommand.Move(unit.Id, best[destinationIndex]);
        }

        private static IReadOnlyList<GridPosition> FullPath(CombatState state, UnitState unit, GridPosition target) =>
            state.Map.FindLowestCostPath(unit.Position, target, state.Map.Width * state.Map.Height * 2,
                position => CombatMovementQuery.EntryCost(state, unit, position), position => state.IsOccupied(position, unit.Id));

        private static int PathCost(CombatState state, IReadOnlyList<GridPosition> path) =>
            path.Skip(1).Sum(position => CombatMovementQuery.EntryCost(state, null, position));

        private static IEnumerable<GridPosition> EntranceCandidates(CombatState state) => state.Map.PositionsWith(tile => tile.IsLampVine)
            .SelectMany(Orthogonal).Where(state.Map.IsInside)
            .Where(position => !state.Map.GetTile(position).IsLampVine && !state.Map.IsBlocked(position) && !state.IsOccupied(position))
            .Distinct();

        private static bool IsUsableEntrance(CombatState state, GridPosition position) => state.Map.IsInside(position) &&
            !state.Map.IsBlocked(position) && Orthogonal(position).Any(candidate => state.Map.IsInside(candidate) && state.Map.GetTile(candidate).IsLampVine);

        private static IEnumerable<GridPosition> OpenNeighborTargets(CombatState state, GridPosition target, string movingUnitId) =>
            Orthogonal(target).Where(state.Map.IsInside).Where(position => !state.Map.IsBlocked(position) && !state.IsOccupied(position, movingUnitId));

        private static IEnumerable<GridPosition> Orthogonal(GridPosition position)
        {
            yield return position + new GridPosition(0, 1);
            yield return position + new GridPosition(1, 0);
            yield return position + new GridPosition(0, -1);
            yield return position + new GridPosition(-1, 0);
        }

        private static bool IsIntactCrystal(CombatState state, GridPosition position) => state.Map.IsInside(position) &&
            state.Map.GetTile(position).IsAetherCrystal && !state.Map.GetTile(position).IsDestroyed;
        private static string Cell(GridPosition position) => ((char)('A' + position.X)).ToString() + (position.Y + 1);
    }
}
