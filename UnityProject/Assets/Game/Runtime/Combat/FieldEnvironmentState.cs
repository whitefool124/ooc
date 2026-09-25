using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    /// <summary>
    /// 风：全场环境状态，不占格。风向与风级公开；风只搬动松散材料（散页、碎晶、烟尘），
    /// 不改变任何技能的攻击线，也不吹动单位。任何单位都可以改变风向，次数有限且公开。
    /// </summary>
    public sealed class FieldWindState
    {
        public static readonly GridPosition East = new GridPosition(1, 0);
        public static readonly GridPosition West = new GridPosition(-1, 0);
        public static readonly GridPosition North = new GridPosition(0, 1);
        public static readonly GridPosition South = new GridPosition(0, -1);

        public GridPosition Direction { get; private set; } = East;
        /// <summary>风级；0 表示无风。</summary>
        public int Level { get; private set; }
        /// <summary>本场还能改变几次风向。</summary>
        public int ChangesRemaining { get; private set; }

        public FieldWindState(int level = 0, int changes = 3)
        {
            Level = level < 0 ? 0 : level > 3 ? 3 : level;
            ChangesRemaining = Math.Max(0, changes);
        }

        public static string DirectionName(GridPosition direction) =>
            direction == West ? "西" : direction == North ? "北" : direction == South ? "南" : "东";

        /// <summary>改变风向与风级。次数用尽或参数非法时返回 false，且不消耗次数。</summary>
        public bool TryChange(GridPosition direction, int level)
        {
            if (ChangesRemaining <= 0 || level < 0 || level > 3) return false;
            if (direction.X == 0 && direction.Y == 0) return false;
            if (direction.X != 0 && direction.Y != 0) return false;
            if (Level == 0 && level == 0) return false;
            Direction = direction;
            Level = level;
            ChangesRemaining--;
            return true;
        }

        public string PreviewText() => Level <= 0
            ? "无风｜剩余改变 " + ChangesRemaining + " 次"
            : "风向 " + DirectionName(Direction) + "｜风级 " + Level + "｜剩余改变 " + ChangesRemaining + " 次";

        public FieldWindState Clone() => new FieldWindState(Level, ChangesRemaining) { Direction = Direction };
    }

    /// <summary>光带：一条公开的直线照明区。被重掩体、建筑或灯藤遮断处留下暗段，暗段内不受光带影响。</summary>
    public sealed class FieldLightLaneState
    {
        /// <summary>光源所属单位或装置的 Id；用于转向时替换同一条光带。</summary>
        public string OwnerId { get; }
        public GridPosition Origin { get; }
        public GridPosition Direction { get; }
        public int Length { get; }

        public FieldLightLaneState(string ownerId, GridPosition origin, GridPosition direction, int length)
        {
            OwnerId = ownerId ?? string.Empty;
            Origin = origin;
            Direction = direction;
            Length = Math.Max(0, length);
        }

        public IReadOnlyList<GridPosition> Cells()
        {
            List<GridPosition> cells = new List<GridPosition>();
            for (int step = 1; step <= Length; step++)
                cells.Add(new GridPosition(Origin.X + Direction.X * step, Origin.Y + Direction.Y * step));
            return cells;
        }

        public FieldLightLaneState Clone() => new FieldLightLaneState(OwnerId, Origin, Direction, Length);
    }

    /// <summary>全场环境状态容器：风全场唯一，光带可同时存在多条。</summary>
    public sealed class FieldEnvironmentState
    {
        public FieldWindState Wind { get; private set; } = new FieldWindState();

        private readonly List<FieldLightLaneState> lanes = new List<FieldLightLaneState>();
        public IReadOnlyList<FieldLightLaneState> LightLanes => lanes;

        public void AddLightLane(FieldLightLaneState lane) { if (lane != null) lanes.Add(lane); }
        public void ClearLightLanes() => lanes.Clear();

        /// <summary>用同一光源的新光带替换旧光带；光源转向或移动时使用。</summary>
        public void ReplaceLightLanes(string ownerId, IEnumerable<FieldLightLaneState> replacements)
        {
            lanes.RemoveAll(lane => string.Equals(lane.OwnerId, ownerId, StringComparison.Ordinal));
            if (replacements == null) return;
            foreach (FieldLightLaneState lane in replacements) if (lane != null) lanes.Add(lane);
        }

        /// <summary>光带的实际照明格：逐格推进，遇到阻挡攻击线的物块或有效烟尘即停在暗段之前。</summary>
        public IReadOnlyList<GridPosition> LitCells(GridMap map, FieldLightLaneState lane, int currentTime)
        {
            List<GridPosition> lit = new List<GridPosition>();
            if (map == null || lane == null) return lit;
            foreach (GridPosition cell in lane.Cells())
            {
                if (!map.IsInside(cell) || map.GetTile(cell).BlocksLineOfSight ||
                    map.GetTile(cell).SmokeExpiresAt > currentTime) break;
                lit.Add(cell);
            }
            return lit;
        }

        public FieldEnvironmentState Clone()
        {
            FieldEnvironmentState clone = new FieldEnvironmentState { Wind = Wind.Clone() };
            foreach (FieldLightLaneState lane in lanes) clone.lanes.Add(lane.Clone());
            return clone;
        }
    }
}
