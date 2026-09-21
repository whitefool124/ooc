using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class RainLanternCourtRuntime
    {
        public const string LevelId = "rain_lantern_court";
        public const string EncounterId = "first_battle_rain_lantern_court";
        public const string OriginSpellId = "ORIGIN-SPELL-01";
        public const string OriginTalentSourceId = "ORIGIN-TALENT-01:first-move-cover";

        public static readonly IReadOnlyList<GridPosition> WaterCells = Row(2, 4, 3);
        public static readonly IReadOnlyList<GridPosition> LampVineCells = Rectangle(3, 0, 2, 4);
        public static readonly IReadOnlyList<GridPosition> BurnOrder = new[]
        {
            new GridPosition(4, 1), new GridPosition(4, 2), new GridPosition(4, 3)
        };
        public static readonly IReadOnlyList<GridPosition> HoundSearchPerimeter = new[]
        {
            new GridPosition(2, 0), new GridPosition(2, 1), new GridPosition(2, 2),
            new GridPosition(2, 3), new GridPosition(2, 4), new GridPosition(3, 4),
            new GridPosition(4, 4), new GridPosition(5, 4), new GridPosition(5, 3),
            new GridPosition(5, 2), new GridPosition(5, 1), new GridPosition(5, 0)
        };
        public static readonly IReadOnlyList<GridPosition> PyromancerShootingPoints = new[]
        {
            new GridPosition(6, 1), new GridPosition(6, 3), new GridPosition(5, 5)
        };
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<GridPosition>> VerificationRoutes =
            new Dictionary<string, IReadOnlyList<GridPosition>>(StringComparer.Ordinal)
            {
                ["FIRST-B1-ROUTE-VINE"] = new[] { new GridPosition(1, 5), new GridPosition(2, 3), new GridPosition(3, 3), new GridPosition(5, 3), new GridPosition(5, 2), new GridPosition(6, 1) },
                ["FIRST-B1-ROUTE-WATER"] = new[] { new GridPosition(1, 5), new GridPosition(2, 4), new GridPosition(3, 4), new GridPosition(4, 4), new GridPosition(5, 4), new GridPosition(5, 3), new GridPosition(6, 1) },
                ["FIRST-B1-ROUTE-COVER"] = new[] { new GridPosition(1, 5), new GridPosition(2, 5), new GridPosition(4, 5), new GridPosition(5, 5), new GridPosition(6, 4), new GridPosition(6, 1) }
            };

        private enum HoundSearchMode { Chase, ApproachEntrance, Sniff, ClockwisePerimeter }
        private HoundSearchMode houndMode;
        private GridPosition houndEntrance;
        private bool hasHoundEntrance;
        private bool firstHeroMoveResolved;
        private bool originSpellCastThisTurn;
        private bool originMoveBonusPending;
        private bool pyromancerBurnedThisTurn;

        public int MovementBudget(UnitState unit) => unit == null ? 0 : unit.MovementRangeThisTurn +
            (unit.IsHero && originMoveBonusPending ? 2 : 0);

        public int EntryCost(UnitState unit, GridPosition position)
        {
            TileState tile = unit == null ? TileState.Empty : combatMap.GetTile(position);
            if (tile.IsLampVine) return 2;
            if (tile.IsWater && unit.IsHero) return 2;
            return 1;
        }

        private GridMap combatMap;

        internal void Attach(GridMap map) => combatMap = map ?? throw new ArgumentNullException(nameof(map));

        public IReadOnlyList<GridPosition> FindPath(CombatState state, UnitState unit, GridPosition destination) =>
            state.Map.FindLowestCostPath(unit.Position, destination, MovementBudget(unit),
                position => EntryCost(unit, position), position => state.IsOccupied(position, unit.Id));

        internal void BeginTurn(UnitState unit)
        {
            if (unit == null) return;
            if (unit.IsHero) originSpellCastThisTurn = false;
            if (unit.EnemyArchetypeId == "pyromancer") pyromancerBurnedThisTurn = false;
            if (unit.EnemyArchetypeId == "tether_hound" && hasHoundEntrance &&
                unit.Position == houndEntrance && houndMode == HoundSearchMode.ApproachEntrance)
                houndMode = HoundSearchMode.Sniff;
        }

        internal void EndTurn(UnitState unit)
        {
            if (unit != null && unit.EnemyArchetypeId == "tether_hound" && houndMode == HoundSearchMode.Sniff)
                houndMode = HoundSearchMode.ClockwisePerimeter;
        }

        internal void AfterMove(CombatState state, UnitState unit, IReadOnlyList<GridPosition> path)
        {
            if (path == null || path.Count < 2) return;
            if (path.Skip(1).Any(position => state.Map.GetTile(position).IsWater) && unit.HasStatus(StatusType.Burning))
            {
                unit.ClearStatus(StatusType.Burning);
                state.AddLog(unit.DisplayName + "踏入积水，燃烧状态立即解除。");
            }
            if (!unit.IsHero) return;

            if (originMoveBonusPending) originMoveBonusPending = false;
            GridPosition destination = path[path.Count - 1];
            if (!firstHeroMoveResolved)
            {
                firstHeroMoveResolved = true;
                if (AdjacentCover(state, destination))
                {
                    state.TryGrantRogueliteShield(unit.Id, OriginTalentSourceId, 2);
                    unit.RestoreMana(1);
                    state.AddLog("出身天赋「就地接线」：首次移动终点邻接掩体，获得 2 护盾并恢复 1 魔力。");
                }
            }

            GridPosition source = path[0];
            if (!state.Map.GetTile(source).IsLampVine && state.Map.GetTile(destination).IsLampVine)
            {
                UnitState hound = state.Units.Values.FirstOrDefault(value => value.EnemyArchetypeId == "tether_hound" && value.IsAlive);
                if (hound != null && !state.HasLineOfSight(hound.Position, destination))
                {
                    hasHoundEntrance = true;
                    houndEntrance = source;
                    houndMode = HoundSearchMode.ApproachEntrance;
                }
            }
        }

        public CombatEffectExecution CastBorrowedCover(CombatState state, UnitState source)
        {
            if (source == null || !source.IsHero) throw new InvalidOperationException("借障导流只能由主角对自身使用。");
            if (originSpellCastThisTurn) throw new InvalidOperationException("借障导流每回合只能使用一次，效果不可叠加。");
            if (!AdjacentCover(state, source.Position)) throw new InvalidOperationException("需要与轻掩体或重掩体正交相邻。");
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, source.Id,
                CombatEffect.SpendActionPoints(1), CombatEffect.SpendMana(1));
            state.TryGrantRogueliteShield(source.Id, OriginSpellId, 4);
            originSpellCastThisTurn = true;
            originMoveBonusPending = true;
            state.AddLog("出身术式「借障导流」：邻接掩体，获得 4 护盾；本回合下一次移动距离 +2。");
            return execution;
        }

        public bool CanBurn(CombatState state, UnitState unit, GridPosition target)
        {
            return unit != null && unit.EnemyArchetypeId == "pyromancer" && unit.ActionPoints >= 1 &&
                !pyromancerBurnedThisTurn && unit.Mana >= 1 && unit.Position.ManhattanDistance(target) <= 5 &&
                state.Map.IsInside(target) && state.Map.GetTile(target).IsLampVine;
        }

        public CombatEffectExecution Burn(CombatState state, UnitState unit, GridPosition target)
        {
            if (!CanBurn(state, unit, target)) throw new InvalidOperationException("当前不能对该灯藤格使用燃开通道。");
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id,
                CombatEffect.SpendActionPoints(1), CombatEffect.SpendMana(1));
            TileState tile = state.Map.GetTile(target);
            tile.IsLampVine = false;
            tile.IsScorched = true;
            tile.Durability = 0;
            pyromancerBurnedThisTurn = true;
            state.AddLog(unit.DisplayName + "使用燃开通道烧去 " + Cell(target) + " 的一格灯藤；没有产生爆炸或伤害。");
            return execution;
        }

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            bool visible = state.HasLineOfSight(enemy.Position, hero.Position);
            if (enemy.EnemyArchetypeId == "tether_hound") return ChooseHound(state, enemy, hero, visible);
            if (enemy.EnemyArchetypeId == "pyromancer") return ChoosePyromancer(state, enemy, hero, visible);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            if (enemy.EnemyArchetypeId == "tether_hound" && houndMode == HoundSearchMode.Sniff)
                return new EnemyIntentPresentation("first-b1:hound:sniff", "停留嗅探", "灯藤入口",
                    "本回合不攻击；以停顿和嗅探动作表示搜索。", "defend", false, default, 0);
            if (enemy.EnemyArchetypeId == "pyromancer" && command.Type == CombatCommandType.Interact &&
                state.Map.GetTile(command.Destination).IsLampVine)
                return new EnemyIntentPresentation("first-b1:pyromancer:burn:" + command.Destination,
                    "燃开通道", Cell(command.Destination), "烧去一格灯藤；不造成爆炸或单位伤害。",
                    "cast", true, command.Destination, 0);
            return CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
        }

        public RainLanternCourtRuntime Clone(GridMap map)
        {
            RainLanternCourtRuntime clone = new RainLanternCourtRuntime
            {
                houndMode = houndMode,
                houndEntrance = houndEntrance,
                hasHoundEntrance = hasHoundEntrance,
                firstHeroMoveResolved = firstHeroMoveResolved,
                originSpellCastThisTurn = originSpellCastThisTurn,
                originMoveBonusPending = originMoveBonusPending,
                pyromancerBurnedThisTurn = pyromancerBurnedThisTurn
            };
            clone.Attach(map);
            return clone;
        }

        private CombatCommand ChooseHound(CombatState state, UnitState enemy, UnitState hero, bool visible)
        {
            if (visible)
            {
                houndMode = HoundSearchMode.Chase;
                int distance = enemy.Position.ManhattanDistance(hero.Position);
                if (distance == 1 && enemy.SkillOne != null && enemy.IsSkillReady(enemy.SkillOne) && enemy.Mana >= enemy.SkillOne.ManaCost)
                    return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
                if (distance == 1) return CombatCommand.Attack(enemy.Id, hero.Id);
                return MoveToward(state, enemy, OpenNeighborTargets(state, hero.Position, enemy.Id));
            }
            if (houndMode == HoundSearchMode.Sniff) return CombatCommand.EndTurn(enemy.Id);
            if (hasHoundEntrance && houndMode == HoundSearchMode.ApproachEntrance)
                return enemy.Position == houndEntrance ? CombatCommand.EndTurn(enemy.Id) : MoveToward(state, enemy, new[] { houndEntrance });

            houndMode = HoundSearchMode.ClockwisePerimeter;
            int current = HoundSearchPerimeter.IndexOf(enemy.Position);
            GridPosition next = HoundSearchPerimeter[current < 0 ? 0 : (current + 1) % HoundSearchPerimeter.Count];
            return MoveToward(state, enemy, new[] { next });
        }

        private CombatCommand ChoosePyromancer(CombatState state, UnitState enemy, UnitState hero, bool visible)
        {
            if (visible && enemy.SkillOne != null && enemy.IsSkillReady(enemy.SkillOne) &&
                enemy.Mana >= enemy.SkillOne.ManaCost && enemy.Position.ManhattanDistance(hero.Position) <= enemy.SkillOne.Range)
                return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            GridPosition? burn = BurnOrder.Cast<GridPosition?>().FirstOrDefault(position =>
                position.HasValue && CanBurn(state, enemy, position.Value));
            if (burn.HasValue) return CombatCommand.Interact(enemy.Id, burn.Value);
            int current = PyromancerShootingPoints.IndexOf(enemy.Position);
            GridPosition next = PyromancerShootingPoints[current < 0 ? 0 : (current + 1) % PyromancerShootingPoints.Count];
            return MoveToward(state, enemy, new[] { next });
        }

        private CombatCommand MoveToward(CombatState state, UnitState unit, IEnumerable<GridPosition> targets)
        {
            int fullSearchBudget = state.Map.Width * state.Map.Height * 2;
            IReadOnlyList<GridPosition> best = targets.Select((target, index) => new
                {
                    Index = index,
                    Path = state.Map.FindLowestCostPath(unit.Position, target, fullSearchBudget,
                        position => EntryCost(unit, position), position => state.IsOccupied(position, unit.Id))
                })
                .Where(candidate => candidate.Path.Count > 1)
                .OrderBy(candidate => PathCost(unit, candidate.Path)).ThenBy(candidate => candidate.Index)
                .Select(candidate => candidate.Path).FirstOrDefault();
            if (best == null) return CombatCommand.EndTurn(unit.Id);
            int spent = 0;
            int destinationIndex = 0;
            for (int index = 1; index < best.Count; index++)
            {
                int cost = EntryCost(unit, best[index]);
                if (spent + cost > MovementBudget(unit)) break;
                spent += cost;
                destinationIndex = index;
            }
            if (destinationIndex == 0) return CombatCommand.EndTurn(unit.Id);
            GridPosition destination = best[destinationIndex];
            return CombatCommand.Move(unit.Id, destination);
        }

        private int PathCost(UnitState unit, IReadOnlyList<GridPosition> path) => path.Skip(1).Sum(position => EntryCost(unit, position));

        private static IEnumerable<GridPosition> OpenNeighborTargets(CombatState state, GridPosition target, string movingUnitId)
        {
            GridPosition[] offsets = { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            return offsets.Select(offset => target + offset).Where(position => state.Map.IsInside(position) &&
                !state.Map.IsBlocked(position) && !state.IsOccupied(position, movingUnitId));
        }

        private static bool AdjacentCover(CombatState state, GridPosition position)
        {
            GridPosition[] offsets = { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            return offsets.Select(offset => position + offset).Where(state.Map.IsInside)
                .Select(state.Map.GetTile).Any(tile => tile.Cover != CoverType.None && !tile.IsDestroyed);
        }

        private static CardinalDirection DirectionToward(GridPosition from, GridPosition to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            if (Math.Abs(dx) >= Math.Abs(dy)) return dx >= 0 ? CardinalDirection.East : CardinalDirection.West;
            return dy >= 0 ? CardinalDirection.North : CardinalDirection.South;
        }

        private static string Cell(GridPosition position) => ((char)('A' + position.X)).ToString() + (position.Y + 1);
        private static IReadOnlyList<GridPosition> Row(int startX, int y, int count) =>
            Enumerable.Range(startX, count).Select(x => new GridPosition(x, y)).ToArray();
        private static IReadOnlyList<GridPosition> Rectangle(int x, int y, int width, int height) =>
            Enumerable.Range(y, height).SelectMany(row => Enumerable.Range(x, width).Select(column => new GridPosition(column, row))).ToArray();
    }

    internal static class GridPositionListExtensions
    {
        public static int IndexOf(this IReadOnlyList<GridPosition> values, GridPosition value)
        {
            for (int index = 0; index < values.Count; index++) if (values[index] == value) return index;
            return -1;
        }
    }
}
