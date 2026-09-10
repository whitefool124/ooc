using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    /// <summary>Read-only movement paths shared by presentation and command execution.</summary>
    public static class CombatMovementQuery
    {
        public static int Budget(CombatState state, UnitState unit) =>
            state.RainLanternCourt == null ? unit.MovementRangeThisTurn + (state.RogueEquipment?.MovementBonus(unit.Id) ?? 0) : state.RainLanternCourt.MovementBudget(unit);

        public static IReadOnlyList<GridPosition> FindPath(CombatState state, UnitState unit, GridPosition destination) =>
            state.RainLanternCourt == null
                ? state.Map.FindLowestCostPath(unit.Position, destination, Budget(state, unit),
                    position => EntryCost(state, unit, position), position => state.IsOccupied(position, unit.Id))
                : state.RainLanternCourt.FindPath(state, unit, destination);

        public static int EntryCost(CombatState state, UnitState unit, GridPosition position)
        {
            if (state.RainLanternCourt != null) return state.RainLanternCourt.EntryCost(unit, position);
            TileState tile = state.Map.GetTile(position);
            return tile.IsWater || tile.IsLampVine || tile.IsCrystalShard ? 2 : 1;
        }

        public static string PlayerTargetFailure(CombatState state, GridPosition destination, CombatMovementRangeCache cache = null)
        {
            if (state == null) return "战场还在准备";
            if (state.IsVictory || state.IsDefeat) return "战斗已经结束";
            if (state.ActiveUnitId != "hero") return "等待敌方行动结束";
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive) return "主角无法行动";
            if (hero.ActionPoints < CombatResolver.BasicActionPointCost) return "行动点不足";
            if (hero.HasStatus(StatusType.Bound)) return "束缚状态下无法移动";
            if (!state.Map.IsInside(destination)) return "那里已经超出战场边界";
            if (destination == hero.Position) return "主角已在该格";
            // Every step costs at least one; this bounds the number of path searches per board refresh.
            if (hero.Position.ManhattanDistance(destination) > Budget(state, hero)) return "目标格超出当前步数";
            if (state.Map.IsBlocked(destination)) return "目标格被阻挡";
            if (state.IsOccupied(destination, hero.Id)) return "目标格已被单位占据";
            bool reachable = cache == null ? FindPath(state, hero, destination).Count > 1 : cache.Contains(state, hero, destination);
            return reachable ? string.Empty : "没有可行路径，或路径代价超出当前步数";
        }
    }

    /// <summary>Exact input snapshot: no frame clock or event-log version can leave stale ranges.</summary>
    public sealed class CombatMovementRangeCache
    {
        private CombatState cachedState;
        private GridPosition origin;
        private int budget = -1;
        private int[] snapshot = Array.Empty<int>();
        private int[] scratch = Array.Empty<int>();
        private bool[] reachable = Array.Empty<bool>();
        public int RebuildCount { get; private set; }

        public bool Contains(CombatState state, UnitState hero, GridPosition destination)
        {
            int width = state.Map.Width, height = state.Map.Height;
            int currentBudget = CombatMovementQuery.Budget(state, hero);
            bool changed = !ReferenceEquals(cachedState, state) || origin != hero.Position || budget != currentBudget;
            if (snapshot.Length != width * height)
            {
                snapshot = new int[width * height];
                scratch = new int[snapshot.Length];
                reachable = new bool[snapshot.Length];
                changed = true;
            }
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var position = new GridPosition(x, y);
                // A route within the budget cannot leave this diamond, even to go around an obstacle.
                scratch[y * width + x] = hero.Position.ManhattanDistance(position) > currentBudget || state.Map.IsBlocked(position)
                    ? -1 : CombatMovementQuery.EntryCost(state, hero, position);
            }
            foreach (UnitState unit in state.Units.Values)
                if (unit.IsAlive && unit.Id != hero.Id && state.Map.IsInside(unit.Position))
                    scratch[unit.Position.Y * width + unit.Position.X] = -1;
            for (int i = 0; i < snapshot.Length; i++) if (snapshot[i] != scratch[i]) { changed = true; break; }
            if (changed)
            {
                cachedState = state;
                origin = hero.Position;
                budget = currentBudget;
                Array.Copy(scratch, snapshot, snapshot.Length);
                Array.Clear(reachable, 0, reachable.Length);
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    var position = new GridPosition(x, y);
                    if (snapshot[index] > 0 && position != origin)
                        reachable[index] = CombatMovementQuery.FindPath(state, hero, position).Count > 1;
                }
                RebuildCount++;
            }
            return reachable[destination.Y * width + destination.X];
        }
    }
}
