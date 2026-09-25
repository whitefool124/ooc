using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>
    /// 学院场地敌人的公开条件反应与场地夺取。本运行时只服务通用场地库的两名单位：
    /// 老寻（读痕迹、循味、扑咬）与灯台值守（光柱与转向）。
    /// 反应全部由玩家角色的公开行为触发，触发条件、次数与结果都会写进敌人意图。
    /// </summary>
    public sealed class AcademyFieldEnemyRuntime
    {
        public const string TrackerId = "elder_tracker_hound";
        public const string KeeperId = "signal_keeper";
        public const string LibrarianId = "wind_librarian";
        public const string StorekeeperId = "legacy_storekeeper";
        public const string PrototypeHandId = "prototype_hand";
        /// <summary>退件射程上限（技能数据表 SK-SUP-18）。</summary>
        public const int RetireRange = 5;
        /// <summary>登记射程上限（技能数据表 SK-SUP-19）。</summary>
        public const int RegisterRange = 4;
        /// <summary>旧脉冲冷却回合数（技能数据表 SK-SUP-17）。</summary>
        public const int PulseCooldown = 2;
        public const int BindingMarkSkillIndex = 5;
        public const int LanternSweepSkillIndex = 6;
        public const int SpotlightSkillIndex = 7;
        public const int PrototypeDeploySkillIndex = 8;
        public const int PrototypeDetonateSkillIndex = 9;
        public const int WindChangeSkillIndex = 10;
        public const int StorekeeperPulseSkillIndex = 11;
        public const int StorekeeperRetireSkillIndex = 12;
        public const int StorekeeperRegisterSkillIndex = 13;
        public const int WindScreenSkillIndex = 14;
        public const int WindScrollSkillIndex = 15;
        public const int WindPushSkillIndex = 16;
        public const int ArbalistArmSkillIndex = 17;
        public const int VanguardDismantleSkillIndex = 18;

        /// <summary>气味痕持续的主角回合数。</summary>
        public const int TraceRounds = 3;
        /// <summary>循味沿痕迹移动时增加的移动力。</summary>
        public const int SniffMovementBonus = 2;
        /// <summary>光柱长度（从值守所在格向外的直线格数）。</summary>
        public const int SpotlightLength = 6;
        /// <summary>光柱在灯台值守回合结束时对柱上单位结算的伤害。</summary>
        public const int SpotlightDamage = 5;
        /// <summary>反应·借遮蔽绕行的每场次数。</summary>
        public const int RotateUses = 2;

        private int heroRounds;
        private int rotateUses;
        private int windChanges;
        private int pulseCooldown;
        private int registerCooldown;
        private readonly HashSet<string> inspectionMarks = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<GridPosition> knownFieldCells = new HashSet<GridPosition>();
        private readonly HashSet<GridPosition> knownDeviceCells = new HashSet<GridPosition>();
        private readonly Dictionary<GridPosition, int> traceAge = new Dictionary<GridPosition, int>();
        private int snareReactionRound = -1;
        private readonly Dictionary<string, GridPosition> spotlightDirection = new Dictionary<string, GridPosition>(StringComparer.Ordinal);
        private readonly HashSet<string> spotlightArmed = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> arbalistArmed = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> vanguardDismantleCooldown = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, GridPosition> vanguardCoverResponses = new Dictionary<string, GridPosition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> menderPriorityTargets = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>已经完成的主角回合数，用于痕迹计时。</summary>
        public int HeroRounds => heroRounds;
        /// <summary>反应·借遮蔽绕行剩余次数。</summary>
        public int RotateUsesRemaining => Math.Max(0, RotateUses - rotateUses);
        /// <summary>小铃已经改变风向的次数。</summary>
        public int WindChangesUsed => windChanges;
        /// <summary>当前被登记（待检定）的单位数。</summary>
        public int InspectionMarkCount => inspectionMarks.Count;

        public static bool IsTracker(UnitState unit) => unit != null && unit.EnemyArchetypeId == TrackerId;
        public static bool IsKeeper(UnitState unit) => unit != null && unit.EnemyArchetypeId == KeeperId;
        public static bool IsLibrarian(UnitState unit) => unit != null && unit.EnemyArchetypeId == LibrarianId;
        public static bool IsStorekeeper(UnitState unit) => unit != null && unit.EnemyArchetypeId == StorekeeperId;
        public static bool IsPrototypeHand(UnitState unit) => unit != null && unit.EnemyArchetypeId == PrototypeHandId;

        /// <summary>小铃消耗散页的数量：卷页 1 格，扬页 3 格。</summary>
        public const int ScrollCost = 1;
        public const int ScreenCost = 3;
        /// <summary>页幕的直线格数。</summary>
        public const int ScreenLength = 3;
        /// <summary>风刃伤害。</summary>
        public const int WindEdgeDamage = 4;
        /// <summary>引火沿途与被撞单位的火焰伤害。</summary>
        public const int KindledFireDamage = 6;
        /// <summary>换风的公开次数上限。</summary>
        public const int WindChanges = 3;

        /// <summary>场上是否还有存活的相关单位，用于决定是否记录痕迹。</summary>
        public static bool HasLiving(CombatState state, string archetypeId) =>
            state != null && state.Units.Values.Any(unit => unit.IsAlive && unit.EnemyArchetypeId == archetypeId);

        /// <summary>循味：站在痕迹上时移动力提升。</summary>
        public int MovementBonus(CombatState state, UnitState unit)
        {
            if (state == null || !IsTracker(unit)) return 0;
            return state.Map.GetTile(unit.Position).HasTrace ? SniffMovementBonus : 0;
        }

        /// <summary>循味：沿痕迹移动不受浅水与碎晶的额外消耗影响。</summary>
        public int EntryCost(CombatState state, UnitState unit, GridPosition position, int baseCost)
        {
            if (state == null || !IsTracker(unit)) return baseCost;
            return state.Map.GetTile(position).HasTrace ? 1 : baseCost;
        }

        /// <summary>守塔之外的场地敌人是否接管该单位的行动。</summary>
        public static bool Handles(UnitState unit) =>
            IsTracker(unit) || IsKeeper(unit) || IsLibrarian(unit) || IsStorekeeper(unit) || IsPrototypeHand(unit) ||
            unit.EnemyArchetypeId == "rune_arbalist" ||
            unit.EnemyArchetypeId == "elite_vanguard" || unit.EnemyArchetypeId == "stone_snare" ||
            unit.EnemyArchetypeId == "lantern_revealer" || unit.EnemyArchetypeId == "barrier_mender";

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null || state == null) return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            if (enemy.EnemyArchetypeId == "rune_arbalist" && state.AcademyEnemyArea != null)
                return ChooseArbalistCommand(state, enemy, hero);
            if (IsTracker(enemy)) return ChooseTrackerCommand(state, enemy, hero);
            if (IsKeeper(enemy)) return ChooseKeeperCommand(state, enemy, hero);
            if (IsLibrarian(enemy)) return ChooseLibrarianCommand(state, enemy, hero);
            if (IsPrototypeHand(enemy) && state.AcademyEnemyArea != null)
                return ChoosePrototypeCommand(state, enemy, hero);
            if (IsStorekeeper(enemy) && state.AcademyEnemyArea != null)
                return ChooseStorekeeperCommand(state, enemy, hero);
            if (enemy.EnemyArchetypeId == "elite_vanguard" && state.AcademyEnemyArea != null)
                return ChooseVanguardCommand(state, enemy, hero);
            if (IsStorekeeper(enemy) || IsPrototypeHand(enemy)) return ChooseFieldActionOnlyCommand(state, enemy, hero);
            if (enemy.EnemyArchetypeId == "stone_snare" && state.AcademyEnemyArea != null)
                return ChooseSnareCommand(state, enemy, hero);
            if (enemy.EnemyArchetypeId == "lantern_revealer" && state.AcademyEnemyArea != null)
                return ChooseLanternCommand(state, enemy, hero);
            if (enemy.EnemyArchetypeId == "barrier_mender" &&
                menderPriorityTargets.TryGetValue(enemy.Id, out string priorityId) &&
                EnemyTactics.CanMend(state, enemy, state.GetUnit(priorityId)))
                return CombatCommand.UseSkill(enemy.Id, 0, priorityId);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        /// <summary>一次主角命令前记录敌方生命与护盾，供受击友军续盾应对判定。</summary>
        public static Dictionary<string, int> CaptureEnemyVitals(CombatState state) =>
            state == null || state.AcademyFieldEnemy == null || !HasLiving(state, "barrier_mender")
                ? new Dictionary<string, int>(StringComparer.Ordinal)
                : state.Units.Values.Where(unit => unit.IsAlive && !unit.IsHero)
                    .ToDictionary(unit => unit.Id, unit => unit.Health + unit.Shield, StringComparer.Ordinal);

        public void ObserveHeroDamage(CombatState state, IReadOnlyDictionary<string, int> before)
        {
            if (state == null || before == null || before.Count == 0) return;
            UnitState damaged = state.Units.Values.Where(unit => !unit.IsHero &&
                    before.TryGetValue(unit.Id, out int oldValue) && oldValue > unit.Health + unit.Shield)
                .OrderByDescending(unit => before[unit.Id] - unit.Health - unit.Shield)
                .ThenBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
            if (damaged == null) return;
            bool recorded = false;
            foreach (UnitState mender in state.Units.Values.Where(unit => unit.IsAlive &&
                unit.EnemyArchetypeId == "barrier_mender" && unit.Id != damaged.Id))
            {
                if (menderPriorityTargets.TryGetValue(mender.Id, out string existing) && existing == damaged.Id) continue;
                menderPriorityTargets[mender.Id] = damaged.Id;
                recorded = true;
            }
            if (recorded)
                state.AddLog("补盾助教应对·优先为受击友军续盾：下个自身回合优先支援" + damaged.DisplayName + "。");
        }

        internal bool IsPriorityMend(string menderId, string targetId) =>
            !string.IsNullOrEmpty(menderId) && !string.IsNullOrEmpty(targetId) &&
            menderPriorityTargets.TryGetValue(menderId, out string priorityId) && priorityId == targetId;

        private static CombatCommand ChooseLanternCommand(CombatState state, UnitState lantern, UnitState hero)
        {
            GridPosition direction = DirectionToward(lantern.Position, hero.Position);
            GridPosition center = lantern.Position + direction;
            return LanternCells(state, lantern.Position, direction, LanternLaneLength).Length > 0
                ? CombatCommand.UseSkillAt(lantern.Id, LanternSweepSkillIndex, center, default)
                : CombatCommand.EndTurn(lantern.Id);
        }

        private static GridPosition[] LanternCells(CombatState state, GridPosition origin, GridPosition direction, int length)
        {
            var cells = new List<GridPosition>();
            for (int step = 1; step <= length; step++)
            {
                GridPosition cell = origin + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight ||
                    state.Map.GetTile(cell).SmokeExpiresAt > state.CurrentTime) break;
                cells.Add(cell);
            }
            return cells.ToArray();
        }

        internal CombatEffectExecution ResolveLanternSweep(CombatState state, UnitState lantern, CombatCommand command)
        {
            if (state == null || lantern == null || lantern.EnemyArchetypeId != "lantern_revealer" ||
                command.UnitId != lantern.Id || command.SlotIndex != LanternSweepSkillIndex)
                throw new InvalidOperationException("转灯意图不可用。");
            GridPosition direction = new GridPosition(command.Destination.X - lantern.Position.X,
                command.Destination.Y - lantern.Position.Y);
            if (Math.Abs(direction.X) + Math.Abs(direction.Y) != 1)
                throw new InvalidOperationException("转灯只能选择正交方向。");
            GridPosition[] cells = LanternCells(state, lantern.Position, direction, LanternLaneLength);
            if (cells.Length == 0) throw new InvalidOperationException("转灯方向已被遮挡。");
            CombatEffectExecution result = CombatResolver.ResolveLanternSweep(state, lantern, cells);
            state.Environment.ReplaceLightLanes(lantern.Id,
                new[] { new FieldLightLaneState(lantern.Id, lantern.Position, direction, LanternLaneLength) });
            state.AddLog("提灯巡查转灯：公开直线 " + LanternLaneLength + " 格，遮挡后形成暗段。");
            return result;
        }

        private static CombatCommand ChooseSnareCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            CombatCommand primary = EnemyTactics.Choose(state, enemy, hero);
            if (primary.Type == CombatCommandType.UseSkill && primary.SlotIndex == 0) return primary;
            GridPosition? cell = state.Map.PositionsWith(_ => true)
                .Where(position => IsLegalBindingMarkCell(state, enemy, position))
                .OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X)
                .Select(position => (GridPosition?)position).FirstOrDefault();
            return cell.HasValue ? CombatCommand.UseSkillAt(enemy.Id, BindingMarkSkillIndex, cell.Value, default)
                : CombatCommand.EndTurn(enemy.Id);
        }

        private static bool IsLegalBindingMarkCell(CombatState state, UnitState enemy, GridPosition cell) =>
            state.Map.IsInside(cell) && enemy.Position.ManhattanDistance(cell) <= 3 &&
            !state.Map.IsBlocked(cell) && !state.IsOccupied(cell) &&
            !state.Map.GetTile(cell).HasEffectLayer && state.Map.GetTile(cell).Cover == CoverType.None &&
            !state.Map.GetTile(cell).IsDeviceLike && !state.Map.GetTile(cell).IsObjective &&
            state.HasLineOfSight(enemy.Position, cell);

        /// <summary>移动确认前与实际结算共用的绕行补刻判定。</summary>
        public GridPosition? PreviewSnareRouteResponse(CombatState state, IReadOnlyList<GridPosition> path)
        {
            if (state == null || path == null || path.Count < 2 || snareReactionRound == heroRounds) return null;
            if (path.Skip(1).Any(position => state.Map.GetTile(position).IsBindingMark)) return null;
            GridPosition[] marks = state.Map.PositionsWith(tile => tile.IsBindingMark).ToArray();
            if (!path.Skip(1).Any(position => marks.Any(mark => mark.ManhattanDistance(position) == 1))) return null;
            GridPosition end = path[path.Count - 1];
            GridPosition before = path[path.Count - 2];
            GridPosition next = end + new GridPosition(end.X - before.X, end.Y - before.Y);
            return state.Units.Values.Where(unit => unit.IsAlive && unit.EnemyArchetypeId == "stone_snare" &&
                    !unit.HasStatus(StatusType.Bound))
                .OrderBy(unit => unit.Id, StringComparer.Ordinal)
                .Any(unit => IsLegalBindingMarkCell(state, unit, next) ||
                    next == path[0] && IsLegalBindingMarkCellIgnoringHero(state, unit, next))
                ? next : (GridPosition?)null;
        }

        private static bool IsLegalBindingMarkCellIgnoringHero(CombatState state, UnitState enemy, GridPosition cell) =>
            state.Map.IsInside(cell) && enemy.Position.ManhattanDistance(cell) <= 3 &&
            !state.Map.IsBlocked(cell) && !state.IsOccupied(cell, "hero") &&
            !state.Map.GetTile(cell).HasEffectLayer && state.Map.GetTile(cell).Cover == CoverType.None &&
            !state.Map.GetTile(cell).IsDeviceLike && !state.Map.GetTile(cell).IsObjective &&
            state.HasLineOfSight(enemy.Position, cell);

        internal CombatEffectExecution ResolveBindingMark(CombatState state, UnitState enemy, CombatCommand command)
        {
            if (state == null || enemy == null || enemy.EnemyArchetypeId != "stone_snare" ||
                command.UnitId != enemy.Id || command.SlotIndex != BindingMarkSkillIndex ||
                !IsLegalBindingMarkCell(state, enemy, command.Destination))
                throw new InvalidOperationException("刻印目标必须是三格内可见的空格。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, enemy.Id,
                CombatEffect.SpendActionPoints(enemy.ActionPoints));
            TileState marked = state.Map.GetTile(command.Destination).Clone();
            marked.IsBindingMark = true;
            marked.EffectSourceId = "skill:SK-SUP-08";
            state.Map.SetTile(command.Destination, marked);
            markAge[command.Destination] = heroRounds;
            markSource[command.Destination] = marked.EffectSourceId;
            state.AddLog("拴索助教刻印：(" + command.Destination.X + "," + command.Destination.Y +
                ") 生成约束纹，持续3个主角回合。");
            return result;
        }

        /// <summary>学院层小铃的场地手段均作为公开的一次行动。</summary>
        private CombatCommand ChooseLibrarianCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            if (state.AcademyEnemyArea != null)
            {
                if ((state.Environment.Wind.Level == 0 || Firegrounds(state).Length > 0) &&
                    windChanges < WindChanges &&
                    state.Environment.Wind.ChangesRemaining > 0)
                    return CombatCommand.UseSkillAt(enemy.Id, WindChangeSkillIndex,
                        enemy.Position + BlowDirection(state, enemy, hero), default);
                GridPosition direction = DirectionToward(enemy.Position, hero.Position);
                if (state.Map.PositionsWith(tile => tile.IsLoosePaper).Count() >= ScreenCost &&
                    PaperScreenLine(state, enemy.Position, direction).Length == ScreenLength)
                    return CombatCommand.UseSkillAt(enemy.Id, WindScreenSkillIndex,
                        enemy.Position + direction, default);
                if (state.Map.PositionsWith(tile => tile.IsLoosePaper).Any() &&
                    enemy.Position.ManhattanDistance(hero.Position) <= EnemyAbilityCatalog.WindScrollEdge.Range)
                    return CombatCommand.UseSkillAt(enemy.Id, WindScrollSkillIndex, hero.Position, default);
                GridPosition pushCell = hero.Position + direction;
                if (enemy.Position.ManhattanDistance(hero.Position) <= 3 && state.Map.IsInside(pushCell) &&
                    !state.Map.IsBlocked(pushCell) && !state.IsOccupied(pushCell))
                    return CombatCommand.UseSkillAt(enemy.Id, WindPushSkillIndex, hero.Position, default);
                return CombatCommand.EndTurn(enemy.Id);
            }
            int weaponRange = enemy.MainHand?.Range ?? 1;
            if (enemy.Position.ManhattanDistance(hero.Position) <= weaponRange &&
                (weaponRange <= 1 || state.HasLineOfSight(enemy.Position, hero.Position)))
                return CombatCommand.Attack(enemy.Id, hero.Id);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        private static GridPosition[] PaperScreenLine(CombatState state, GridPosition origin, GridPosition direction)
        {
            var cells = new List<GridPosition>();
            for (int step = 1; step <= ScreenLength; step++)
            {
                GridPosition cell = origin + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight ||
                    state.Map.GetTile(cell).IsDeviceLike || state.Map.GetTile(cell).IsObjective) break;
                cells.Add(cell);
            }
            return cells.ToArray();
        }

        private CombatCommand ChooseArbalistCommand(CombatState state, UnitState arbalist, UnitState hero)
        {
            if (arbalist.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(arbalist.Id);
            if (!arbalistArmed.Contains(arbalist.Id))
                return CombatCommand.UseSkill(arbalist.Id, ArbalistArmSkillIndex, arbalist.Id);
            int distance = arbalist.Position.ManhattanDistance(hero.Position);
            SkillDefinition bolt = EnemyAbilityCatalog.WindlassBolt;
            if (distance >= bolt.MinimumRange && distance <= bolt.Range &&
                state.HasLineOfSight(arbalist.Position, hero.Position))
                return arbalist.IsSkillReady(bolt) && arbalist.Mana >= bolt.ManaCost
                    ? CombatCommand.UseSkill(arbalist.Id, 0, hero.Id)
                    : CombatCommand.EndTurn(arbalist.Id);
            GridPosition? retreat = Adjacent(arbalist.Position)
                .Where(cell => state.Map.IsInside(cell) && !state.Map.IsBlocked(cell) &&
                    !state.IsOccupied(cell) && cell.ManhattanDistance(hero.Position) > distance)
                .OrderByDescending(cell => cell.ManhattanDistance(hero.Position))
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Cast<GridPosition?>().FirstOrDefault();
            return retreat.HasValue ? CombatCommand.Move(arbalist.Id, retreat.Value)
                : CombatCommand.EndTurn(arbalist.Id);
        }

        internal CombatEffectExecution ResolveArbalistArm(CombatState state, UnitState arbalist, CombatCommand command)
        {
            if (state == null || arbalist?.EnemyArchetypeId != "rune_arbalist" ||
                state.AcademyEnemyArea == null || command.UnitId != arbalist.Id ||
                command.SlotIndex != ArbalistArmSkillIndex || command.TargetUnitId != arbalist.Id ||
                arbalistArmed.Contains(arbalist.Id))
                throw new InvalidOperationException("背弩生架弩意图不可用。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, arbalist.Id,
                CombatEffect.SpendActionPoints(arbalist.ActionPoints));
            arbalistArmed.Add(arbalist.Id);
            state.AddLog("背弩生架弩：占用整个回合，下一自身回合才能发射重矢。");
            return result;
        }

        private CombatCommand ChooseVanguardCommand(CombatState state, UnitState vanguard, UnitState hero)
        {
            if (vanguard.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(vanguard.Id);
            if (vanguardCoverResponses.TryGetValue(vanguard.Id, out GridPosition response) &&
                state.Map.IsInside(response) && response.ManhattanDistance(vanguard.Position) == 1 &&
                (IsStructure(state.Map.GetTile(response)) &&
                    state.Map.GetTile(response).StructureOwnerUnitId == vanguard.Id ||
                    CanRebuildVanguardWall(state, vanguard, response)))
                return CombatCommand.UseSkillAt(vanguard.Id, VanguardDismantleSkillIndex, response, default);
            if (vanguardDismantleCooldown.TryGetValue(vanguard.Id, out int cooldown) && cooldown > 0)
                return CombatCommand.EndTurn(vanguard.Id);
            GridPosition? wall = Adjacent(vanguard.Position)
                .Where(state.Map.IsInside)
                .Where(cell => IsStructure(state.Map.GetTile(cell)) &&
                    cell.ManhattanDistance(hero.Position) <= 1)
                .OrderBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Cast<GridPosition?>().FirstOrDefault();
            return wall.HasValue
                ? CombatCommand.UseSkillAt(vanguard.Id, VanguardDismantleSkillIndex, wall.Value, default)
                : CombatCommand.EndTurn(vanguard.Id);
        }

        private static bool CanRebuildVanguardWall(CombatState state, UnitState vanguard, GridPosition position)
        {
            if (!state.Map.IsInside(position) || position.ManhattanDistance(vanguard.Position) != 1 ||
                state.IsOccupied(position)) return false;
            TileState wall = state.Map.GetTile(position);
            return wall.Cover == CoverType.Heavy && wall.IsDestroyed &&
                wall.StructureOwnerUnitId == vanguard.Id;
        }

        internal CombatEffectExecution ResolveVanguardDismantle(CombatState state, UnitState vanguard, CombatCommand command)
        {
            bool response = state != null && vanguard != null &&
                vanguardCoverResponses.TryGetValue(vanguard.Id, out GridPosition responseCell) &&
                responseCell == command.Destination;
            bool rebuild = response && CanRebuildVanguardWall(state, vanguard, command.Destination);
            if (state == null || vanguard?.EnemyArchetypeId != "elite_vanguard" ||
                state.AcademyEnemyArea == null || command.UnitId != vanguard.Id ||
                command.SlotIndex != VanguardDismantleSkillIndex ||
                !state.Map.IsInside(command.Destination) ||
                command.Destination.ManhattanDistance(vanguard.Position) != 1 ||
                !rebuild && !IsStructure(state.Map.GetTile(command.Destination)) ||
                !response && vanguardDismantleCooldown.TryGetValue(vanguard.Id, out int cooldown) && cooldown > 0)
                throw new InvalidOperationException("划线教官拆架目标不可用。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, vanguard.Id,
                CombatEffect.SpendActionPoints(vanguard.ActionPoints));
            if (rebuild)
            {
                TileState rebuilt = state.Map.GetTile(command.Destination).Clone();
                rebuilt.Durability = TileState.HeavyDurability;
                state.Map.SetTile(command.Destination, rebuilt);
                vanguardCoverResponses.Remove(vanguard.Id);
                vanguardDismantleCooldown[vanguard.Id] = 2;
                state.AddLog("划线教官应对：在 (" + command.Destination.X + "," + command.Destination.Y +
                    ") 原位重筑加厚墙，耐久 " + TileState.HeavyDurability + "。");
                return result;
            }
            TileState wall = state.Map.GetTile(command.Destination).Clone();
            int durability = wall.Durability;
            wall.Durability = 0;
            state.Map.SetTile(command.Destination, wall);
            state.ResolveAetherCrystalDamage(command.Destination, durability, vanguard.Id);
            if (vanguardCoverResponses.TryGetValue(vanguard.Id, out GridPosition targetCell) &&
                targetCell == command.Destination) vanguardCoverResponses.Remove(vanguard.Id);
            foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive &&
                unit.Position.ManhattanDistance(command.Destination) <= 1))
            {
                target.ClearShield();
                state.ApplyRogueliteBreakStance(target.Id);
            }
            vanguardDismantleCooldown[vanguard.Id] = 2;
            state.AddLog("划线教官拆架：拆除 (" + command.Destination.X + "," + command.Destination.Y +
                ") 的重掩体；贴墙单位护盾清空并破势。");
            return result;
        }

        internal CombatEffectExecution ResolveLibrarianCommand(CombatState state, UnitState librarian, CombatCommand command)
        {
            UnitState hero = state?.GetUnit("hero");
            if (state == null || !IsLibrarian(librarian) || hero == null || !hero.IsAlive ||
                command.UnitId != librarian.Id || librarian.HasStatus(StatusType.Bound) ||
                state.AcademyEnemyArea == null)
                throw new InvalidOperationException("小铃的场地行动不可用。");
            GridPosition direction = DirectionToward(librarian.Position, hero.Position);
            bool legal = command.SlotIndex == WindScreenSkillIndex
                ? command.Destination == librarian.Position + direction &&
                    PaperScreenLine(state, librarian.Position, direction).Length == ScreenLength &&
                    state.Map.PositionsWith(tile => tile.IsLoosePaper).Count() >= ScreenCost
                : command.SlotIndex == WindScrollSkillIndex
                    ? command.Destination == hero.Position &&
                        librarian.Position.ManhattanDistance(hero.Position) <= EnemyAbilityCatalog.WindScrollEdge.Range &&
                        state.Map.PositionsWith(tile => tile.IsLoosePaper).Any()
                    : command.SlotIndex == WindPushSkillIndex && command.Destination == hero.Position &&
                        librarian.Position.ManhattanDistance(hero.Position) <= 3 &&
                        state.Map.IsInside(hero.Position + direction) &&
                        !state.Map.IsBlocked(hero.Position + direction) && !state.IsOccupied(hero.Position + direction);
            if (!legal) throw new InvalidOperationException("小铃的场地目标已不可用。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, librarian.Id,
                CombatEffect.SpendActionPoints(librarian.ActionPoints));
            bool applied = command.SlotIndex == WindScreenSkillIndex
                ? TryRaisePaperScreen(state, librarian, hero)
                : command.SlotIndex == WindScrollSkillIndex
                    ? TryWindEdge(state, librarian, hero)
                    : TryPushUnit(state, librarian, hero);
            if (!applied) throw new InvalidOperationException("小铃的场地行动未能结算。");
            return result;
        }

        /// <summary>老库管与试制员：场地动作在回合开始时结算，之后只用普通攻击或走位。</summary>
        private static CombatCommand ChooseFieldActionOnlyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            int weaponRange = enemy.MainHand?.Range ?? 1;
            if (enemy.Position.ManhattanDistance(hero.Position) <= weaponRange &&
                (weaponRange <= 1 || state.HasLineOfSight(enemy.Position, hero.Position)))
                return CombatCommand.Attack(enemy.Id, hero.Id);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        /// <summary>老寻：能沿痕迹扑击就扑击，否则按普通近战逼近。</summary>
        private static CombatCommand ChooseTrackerCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            bool onTrace = state.Map.GetTile(hero.Position).HasTrace;
            if (distance == 1 && onTrace && enemy.IsSkillReady(enemy.SkillTwo)) return CombatCommand.UseSkill(enemy.Id, 1, hero.Id);
            if (distance == 1 && enemy.IsSkillReady(enemy.SkillOne)) return CombatCommand.UseSkill(enemy.Id, 0, hero.Id);
            if (enemy.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(enemy.Id);
            return EnemyTactics.Choose(state, enemy, hero);
        }

        /// <summary>转镜占用一次行动，光柱在本次自身回合结束时结算。</summary>
        private static CombatCommand ChooseKeeperCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            GridPosition direction = DirectionToward(enemy.Position, hero.Position);
            GridPosition center = enemy.Position + direction;
            return SpotlightCells(state, enemy.Position, direction).Length > 0
                ? CombatCommand.UseSkillAt(enemy.Id, SpotlightSkillIndex, center, default)
                : CombatCommand.EndTurn(enemy.Id);
        }

        internal static GridPosition[] SpotlightCells(CombatState state, GridPosition origin, GridPosition direction) =>
            LanternCells(state, origin, direction, SpotlightLength);

        internal CombatEffectExecution ResolveSpotlightCommand(CombatState state, UnitState keeper, CombatCommand command)
        {
            if (state == null || !IsKeeper(keeper) || command.UnitId != keeper.Id ||
                command.SlotIndex != SpotlightSkillIndex)
                throw new InvalidOperationException("转镜意图不可用。");
            GridPosition direction = new GridPosition(command.Destination.X - keeper.Position.X,
                command.Destination.Y - keeper.Position.Y);
            if (Math.Abs(direction.X) + Math.Abs(direction.Y) != 1 ||
                SpotlightCells(state, keeper.Position, direction).Length == 0)
                throw new InvalidOperationException("转镜方向已被遮挡。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, keeper.Id,
                CombatEffect.SpendActionPoints(keeper.ActionPoints));
            SetSpotlight(state, keeper, direction);
            spotlightArmed.Add(keeper.Id);
            return result;
        }

        internal void ArmSpotlight(CombatState state, UnitState keeper, GridPosition direction)
        {
            SetSpotlight(state, keeper, direction);
            spotlightArmed.Add(keeper.Id);
        }

        private CombatCommand ChoosePrototypeCommand(CombatState state, UnitState hand, UnitState hero)
        {
            GridPosition? target = Adjacent(hero.Position).Where(state.Map.IsInside)
                .Where(cell => state.Map.GetTile(cell).IsOverloadDevice)
                .OrderBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Select(cell => (GridPosition?)cell).FirstOrDefault();
            if (target.HasValue)
                return CombatCommand.UseSkillAt(hand.Id, PrototypeDetonateSkillIndex, target.Value, default);
            GridPosition? deploy = PrototypeDeployCell(state, hand, hero);
            return deploy.HasValue
                ? CombatCommand.UseSkillAt(hand.Id, PrototypeDeploySkillIndex, deploy.Value, default)
                : CombatCommand.EndTurn(hand.Id);
        }

        private GridPosition? PrototypeDeployCell(CombatState state, UnitState hand, UnitState hero)
        {
            if (PrototypeRemaining <= 0) return null;
            bool wantWard = !Adjacent(hand.Position).Any(cell => state.Map.IsInside(cell) &&
                state.Map.GetTile(cell).IsWardGenerator);
            IEnumerable<GridPosition> slots = Adjacent(hand.Position)
                .Where(cell => state.Map.IsInside(cell) && !state.Map.IsBlocked(cell) && !state.IsOccupied(cell) &&
                    !state.Map.GetTile(cell).IsDeviceLike && !state.Map.GetTile(cell).HasEffectLayer &&
                    state.Map.GetTile(cell).Cover == CoverType.None && !state.Map.GetTile(cell).IsObjective);
            return (wantWard ? slots.OrderBy(cell => cell.ManhattanDistance(hero.Position))
                    : slots.OrderByDescending(cell => cell.ManhattanDistance(hero.Position)))
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X)
                .Select(cell => (GridPosition?)cell).FirstOrDefault();
        }

        internal CombatEffectExecution ResolvePrototypeCommand(CombatState state, UnitState hand, CombatCommand command)
        {
            if (state == null || !IsPrototypeHand(hand) || command.UnitId != hand.Id)
                throw new InvalidOperationException("试制员意图不可用。");
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive)
                throw new InvalidOperationException("试制员目标不可用。");
            if (command.SlotIndex == PrototypeDeploySkillIndex)
            {
                GridPosition? pick = PrototypeDeployCell(state, hand, hero);
                if (!pick.HasValue || pick.Value != command.Destination)
                    throw new InvalidOperationException("布放目标不是当前合法空格。");
                CombatEffectExecution result = CombatEffectExecutor.Execute(state, hand.Id,
                    CombatEffect.SpendActionPoints(hand.ActionPoints));
                bool wantWard = !Adjacent(hand.Position).Any(cell => state.Map.IsInside(cell) &&
                    state.Map.GetTile(cell).IsWardGenerator);
                TileState tile = state.Map.GetTile(pick.Value).Clone();
                tile.IsDevice = true;
                tile.Durability = TileState.PrototypeDurability;
                tile.IsWardGenerator = wantWard;
                tile.IsOverloadDevice = !wantWard;
                state.Map.SetTile(pick.Value, tile);
                PrototypePlaced++;
                SnapshotDevices(DeviceCells(state));
                state.AddLog("试制员布放：在 (" + pick.Value.X + "," + pick.Value.Y + ") 放下" +
                    (wantWard ? "护罩发生器" : "过载装置") + "，试制箱剩余 " + PrototypeRemaining + " 件。");
                return result;
            }
            if (command.SlotIndex == PrototypeDetonateSkillIndex && state.Map.IsInside(command.Destination) &&
                command.Destination.ManhattanDistance(hero.Position) == 1 &&
                state.Map.GetTile(command.Destination).IsOverloadDevice)
            {
                CombatEffectExecution result = CombatEffectExecutor.Execute(state, hand.Id,
                    CombatEffect.SpendActionPoints(hand.ActionPoints));
                DetonatePrototypeDevice(state, command.Destination);
                return result;
            }
            throw new InvalidOperationException("引爆目标必须是主角相邻的过载装置。");
        }

        internal CombatEffectExecution ResolveWindChangeCommand(CombatState state, UnitState librarian,
            CombatCommand command)
        {
            if (state == null || !IsLibrarian(librarian) || command.UnitId != librarian.Id ||
                command.SlotIndex != WindChangeSkillIndex || windChanges >= WindChanges ||
                state.Environment.Wind.ChangesRemaining <= 0)
                throw new InvalidOperationException("换风意图不可用。");
            GridPosition direction = new GridPosition(command.Destination.X - librarian.Position.X,
                command.Destination.Y - librarian.Position.Y);
            if (Math.Abs(direction.X) + Math.Abs(direction.Y) != 1)
                throw new InvalidOperationException("换风只支持正交方向。");
            int level = Math.Min(3, 1 + windChanges);
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, librarian.Id,
                CombatEffect.SpendActionPoints(librarian.ActionPoints));
            if (!state.Environment.Wind.TryChange(direction, level))
                throw new InvalidOperationException("当前无法改变风向。");
            windChanges++;
            state.AddLog("小铃换风：风向" + FieldWindState.DirectionName(direction) + "｜风级 " + level +
                "，剩余改变 " + state.Environment.Wind.ChangesRemaining + " 次。");
            return result;
        }

        internal void DetonatePrototypeDevice(CombatState state, GridPosition position)
        {
            int before = state.Map.GetTile(position).Durability;
            TileState tile = state.Map.GetTile(position).Clone();
            tile.Durability = 0;
            state.Map.SetTile(position, tile);
            state.ResolveOverloadDeviceDamage(position, before);
            SnapshotDevices(DeviceCells(state));
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            if (enemy?.EnemyArchetypeId == "rune_arbalist" && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == ArbalistArmSkillIndex)
                return new EnemyIntentPresentation("SK-SUP-04:" + enemy.Id, "架弩", "自身",
                    "占用整个回合完成重弩架设；本回合无法攻击。", "defend", false, default, 0,
                    affectedCells: new[] { enemy.Position });
            if (enemy?.EnemyArchetypeId == "elite_vanguard" && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == VanguardDismantleSkillIndex)
            {
                bool rebuild = vanguardCoverResponses.TryGetValue(enemy.Id, out GridPosition responseCell) &&
                    responseCell == command.Destination && CanRebuildVanguardWall(state, enemy, command.Destination);
                GridPosition[] cells = new[] { command.Destination }.Concat(Adjacent(command.Destination))
                    .Where(state.Map.IsInside).ToArray();
                bool response = vanguardCoverResponses.TryGetValue(enemy.Id, out GridPosition target) && target == command.Destination;
                return new EnemyIntentPresentation((response ? "SK-CORE-05:" : "SK-CORE-03:") + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    rebuild ? "原位重筑" : response ? "拆你所倚" : "拆架", "正交相邻重掩体",
                    rebuild ? "在被拆墙的空置原格重筑24耐久加厚墙，不造成伤害。" :
                        "拆除目标墙段；贴墙单位护盾清空并施加破势，不造成伤害。",
                    "control", true, command.Destination, 0, affectedCells: rebuild ? new[] { command.Destination } : cells);
            }
            if (enemy?.EnemyArchetypeId == "stone_snare" && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == BindingMarkSkillIndex)
                return new EnemyIntentPresentation("SK-SUP-08:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "刻印", "三格内空格 (" + command.Destination.X + "," + command.Destination.Y + ")",
                    "在标出格生成约束纹，持续3个主角回合；进入者本回合留在原格。", "control", true,
                    command.Destination, 0, affectedCells: new[] { command.Destination });
            if (enemy?.EnemyArchetypeId == "lantern_revealer" && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == LanternSweepSkillIndex)
            {
                GridPosition direction = new GridPosition(command.Destination.X - enemy.Position.X,
                    command.Destination.Y - enemy.Position.Y);
                GridPosition[] cells = LanternCells(state, enemy.Position, direction, LanternLaneLength);
                string targets = string.Join("、", state.Units.Values.Where(unit => unit.IsAlive && cells.Contains(unit.Position))
                    .OrderBy(unit => unit.Id, StringComparer.Ordinal)
                    .Select(unit => unit.DisplayName + "预计生命伤害" + CombatResolver.PreviewLanternSweepDamage(state, enemy, unit)));
                return new EnemyIntentPresentation("SK-SUP-11:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "转灯", "正交直线4格", "光带内单位护盾清空、受到1点基础奥术伤害并标记1回合；遮挡处不生效" +
                    (targets.Length == 0 ? string.Empty : "；目标：" + targets), "attack", true,
                    command.Destination, 1, attackRange: cells, affectedCells: cells);
            }
            if (IsKeeper(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == SpotlightSkillIndex)
            {
                GridPosition direction = new GridPosition(command.Destination.X - enemy.Position.X,
                    command.Destination.Y - enemy.Position.Y);
                GridPosition[] cells = SpotlightCells(state, enemy.Position, direction);
                return new EnemyIntentPresentation("SK-SUP-21:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "转镜", "正交直线6格", "本回合转出光柱；回合结束时照明格内单位各受5点以太伤害；遮挡后为暗段。" +
                    "反应：主角结束移动后若离开照明格，可转向，剩余 " + RotateUsesRemaining + " 次。", "attack", true,
                    command.Destination, SpotlightDamage, attackRange: cells, affectedCells: cells);
            }
            if (IsPrototypeHand(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == PrototypeDeploySkillIndex)
                return new EnemyIntentPresentation("SK-SUP-13:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "布放", "正交相邻空格", "本回合布放1件试制件；当前试制箱剩余 " + PrototypeRemaining + " 件。",
                    "defend", true, command.Destination, 0, affectedCells: new[] { command.Destination });
            if (IsPrototypeHand(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == PrototypeDetonateSkillIndex)
            {
                GridPosition[] cells = Adjacent(command.Destination).Where(state.Map.IsInside).ToArray();
                return new EnemyIntentPresentation("SK-SUP-14:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "引爆", "主角相邻过载装置", "装置正交邻格单位各受8点以太伤害，敌我一致。",
                    "attack", true, command.Destination, 8, attackRange: cells, affectedCells: cells);
            }
            if (IsLibrarian(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == WindChangeSkillIndex)
            {
                GridPosition direction = new GridPosition(command.Destination.X - enemy.Position.X,
                    command.Destination.Y - enemy.Position.Y);
                return new EnemyIntentPresentation("SK-CORE-06:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "换风", "全场风向与风级", "本回合改为" + FieldWindState.DirectionName(direction) + "风，风级 " +
                    Math.Min(3, 1 + windChanges) + "；只搬动散页、碎晶与烟尘。", "control", true,
                    command.Destination, 0);
            }
            if (IsLibrarian(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == WindScreenSkillIndex)
            {
                GridPosition direction = new GridPosition(command.Destination.X - enemy.Position.X,
                    command.Destination.Y - enemy.Position.Y);
                GridPosition[] cells = PaperScreenLine(state, enemy.Position, direction);
                return new EnemyIntentPresentation("SK-CORE-07:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "扬页", "前方直线3格", "消耗3格散页生成页幕，持续到下个自身回合开始；页幕截断攻击线。",
                    "control", true, command.Destination, 0, affectedCells: cells);
            }
            if (IsLibrarian(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == WindScrollSkillIndex)
                return new EnemyIntentPresentation("SK-CORE-08:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "卷页", "4格内主角", "消耗离主角最近的1格散页，造成4点以太伤害。",
                    "attack", true, command.Destination, WindEdgeDamage,
                    affectedCells: new[] { command.Destination });
            if (IsLibrarian(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == WindPushSkillIndex)
                return new EnemyIntentPresentation("SK-CORE-10:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "推风", "3格内主角", "沿主轴推开主角1格。",
                    "control", true, command.Destination, 0,
                    affectedCells: new[] { command.Destination, command.Destination + DirectionToward(enemy.Position, command.Destination) });
            if (IsStorekeeper(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == StorekeeperPulseSkillIndex)
            {
                GridPosition[] cells = PulseCells(state, enemy, state.GetUnit("hero"));
                return new EnemyIntentPresentation("SK-SUP-17:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "旧脉冲", "正交直线4格", "直线上单位各受5点以太伤害、护盾清空并施加破势；遮挡截断。",
                    "attack", true, command.Destination, PulseDamage, attackRange: cells, affectedCells: cells);
            }
            if (IsStorekeeper(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == StorekeeperRetireSkillIndex)
                return new EnemyIntentPresentation("SK-SUP-18:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "退件", "5格内场地效果或装置", "清除标记格上的一个场地效果或装置。",
                    "control", true, command.Destination, 0, affectedCells: new[] { command.Destination });
            if (IsStorekeeper(enemy) && command.Type == CombatCommandType.UseSkill &&
                command.SlotIndex == StorekeeperRegisterSkillIndex)
                return new EnemyIntentPresentation("SK-SUP-19:" + enemy.Id + ":" + command.Destination.X + "," + command.Destination.Y,
                    "登记", "4格内主角", "挂待检定标记，下一次获得的护盾被优先清除。",
                    "control", true, command.Destination, 0, affectedCells: new[] { command.Destination });
            EnemyIntentPresentation basic = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (enemy?.EnemyArchetypeId == "barrier_mender" && command.Type == CombatCommandType.UseSkill &&
                menderPriorityTargets.TryGetValue(enemy.Id, out string responseId) && responseId == command.TargetUnitId)
                return new EnemyIntentPresentation("SK-SUP-03:" + enemy.Id + ":" + responseId,
                    "优先续盾", basic.TargetSummary,
                    "主角本回合伤到该友军；补盾助教下个自身回合优先为其续盾。" + basic.ResultSummary,
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage,
                    basic.Route, basic.AttackRange, basic.AffectedCells);
            if (state == null || !Handles(enemy)) return basic;
            if (IsTracker(enemy))
            {
                bool onTrace = state.Map.GetTile(enemy.Position).HasTrace;
                string rule = "循味：站在气味痕上移动力 +" + SniffMovementBonus + "，且浅水与碎晶不额外消耗移动力。" +
                    (onTrace ? " 当前位于气味痕上。" : " 当前不在气味痕上。");
                string bite = TraceCount(state) > 0
                    ? "扑咬：目标位于气味痕上时改为 6 点伤害并束缚 1 回合。"
                    : "场上暂无气味痕，扑咬只结算 3 点伤害。";
                return new EnemyIntentPresentation(basic.Signature,
                    command.Type == CombatCommandType.Move && state.Map.IsInside(command.Destination) &&
                        state.Map.GetTile(command.Destination).HasTrace ? "循味" : basic.ActionName,
                    basic.TargetSummary, rule + " " + bite + " " + basic.ResultSummary,
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
            }
            if (IsStorekeeper(enemy))
            {
                string kit = "可用场地手段：旧脉冲（沿正交直线 " + PulseLength + " 格，各 " + PulseDamage +
                    " 点以太伤害并清空护盾、施加破势）／退件（清除场上的一个场地效果或装置，双方来源都算）／登记（给目标挂待检定标记，" +
                    "下一次获得的护盾被优先清除）。";
                string marks = inspectionMarks.Count == 0 ? "当前没有待检定标记。" : "当前有 " + inspectionMarks.Count + " 个待检定标记。";
                string retireReaction = "反应：主角本回合新生成的效果或装置会被优先退掉。";
                return new EnemyIntentPresentation(basic.Signature,
                    basic.ActionName, basic.TargetSummary, kit + " " + marks + " " + retireReaction + " " + basic.ResultSummary,
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
            }
            if (IsPrototypeHand(enemy))
            {
                string kit = "可用场地手段：布放（在正交相邻空格放下一件试制件；先放护罩发生器，再放过载装置）／引爆（引爆与主角相邻的过载装置，敌我一致）。";
                string stock = "试制箱剩余 " + PrototypeRemaining + " 件（已布放 " + PrototypePlaced + " 件）。";
                string deployReaction = "反应：主角站在试制件相邻时优先引爆该件；主角拆除试制件时优先补放一件。";
                return new EnemyIntentPresentation(basic.Signature,
                    basic.ActionName, basic.TargetSummary, kit + " " + stock + " " + deployReaction + " " + basic.ResultSummary,
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
            }
            if (IsLibrarian(enemy))
            {
                int papers = state.Map.PositionsWith(tile => tile.IsLoosePaper).Count();
                int fires = Firegrounds(state).Length;
                string kit = "可用场地手段：卷页（消耗 " + ScrollCost + " 格散页，风刃 " + WindEdgeDamage + " 点伤害）／扬页（消耗 " +
                    ScreenCost + " 格散页，立 " + ScreenLength + " 格页幕）／引火（把火吹向主角一侧）／推风（推开 3 格内的单位）／换风（已用 " +
                    windChanges + "/" + WindChanges + " 次）。";
                string field = "场上散页 " + papers + " 格，火场 " + fires + " 处。";
                string fireReaction = "反应：场上有火时优先换风，把火吹向主角所在的一侧。";
                return new EnemyIntentPresentation(basic.Signature,
                    basic.ActionName, basic.TargetSummary, kit + " " + field + " " + fireReaction + " " + basic.ResultSummary,
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
            }
            FieldLightLaneState lane = LaneOf(state, enemy);
            string laneText = lane == null
                ? "转镜：投出一条直线 " + SpotlightLength + " 格的光柱，被重掩体、建筑或灯藤遮断处形成暗段。"
                : "转镜：光柱方向 " + FieldWindState.DirectionName(lane.Direction) + "，柱上单位在灯台值守回合结束时受到 " +
                    SpotlightDamage + " 点伤害，暗段内不受影响。";
            string reaction = "反应·绕行转向：玩家角色结束移动时不在光柱照明格内（含借遮蔽形成的暗段）就转向其新路线（剩余 " + RotateUsesRemaining + " 次）。";
            return new EnemyIntentPresentation(basic.Signature,
                basic.ActionName, basic.TargetSummary, laneText + " " + reaction + " " + basic.ResultSummary,
                basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
        }

        internal void BeginTurn(CombatState state, UnitState unit)
        {
            if (state == null || unit == null) return;
            if (unit.IsHero)
            {
                heroRounds++;
                ExpireTraces(state);
                SurveyDwell(state, unit);
                return;
            }
            if (IsLibrarian(unit))
            {
                if (state.AcademyEnemyArea != null) ClearPaperScreens(state);
                else ResolveLibrarianTurn(state, unit, state.GetUnit("hero"));
            }
            if (IsStorekeeper(unit))
            {
                if (state.AcademyEnemyArea == null) ResolveStorekeeperTurn(state, unit, state.GetUnit("hero"));
                else
                {
                    if (pulseCooldown > 0) pulseCooldown--;
                    if (registerCooldown > 0) registerCooldown--;
                }
            }
            if (unit.EnemyArchetypeId == "elite_vanguard" &&
                vanguardDismantleCooldown.TryGetValue(unit.Id, out int dismantleCooldown) && dismantleCooldown > 0)
                vanguardDismantleCooldown[unit.Id] = dismantleCooldown - 1;
            if (IsPrototypeHand(unit) && state.AcademyEnemyArea == null)
                ResolvePrototypeTurn(state, unit, state.GetUnit("hero"));
            if (unit.EnemyArchetypeId == "elite_vanguard" || unit.EnemyArchetypeId == "stone_snare" ||
                unit.EnemyArchetypeId == "lantern_revealer" || unit.EnemyArchetypeId == "barrier_mender")
                ResolveStaffTurn(state, unit, state.GetUnit("hero"));
        }

        internal void EndTurn(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId == "barrier_mender") menderPriorityTargets.Remove(unit.Id);
            if (state != null && IsStorekeeper(unit) && state.AcademyEnemyArea != null)
                SnapshotField(state);
            if (state == null || !IsKeeper(unit) || !spotlightArmed.Remove(unit.Id)) return;
            ResolveSpotlightDamage(state, unit);
        }

        /// <summary>主角每次移动都在路径上留下气味痕；痕迹加深由停留判定。</summary>
        internal void AfterMove(CombatState state, UnitState unit, IReadOnlyList<GridPosition> path)
        {
            if (state?.AcademyEnemyArea != null && unit?.EnemyArchetypeId == "rune_arbalist" &&
                path != null && path.Count > 1)
            {
                arbalistArmed.Remove(unit.Id);
                state.AddLog("背弩生退距：更换架设点，下回合须重新架弩。");
            }
            if (state == null || unit?.IsHero != true || path == null || path.Count == 0) return;
            GridPosition? snareResponse = PreviewSnareRouteResponse(state, path);
            if (snareResponse.HasValue)
            {
                TileState responseMark = state.Map.GetTile(snareResponse.Value).Clone();
                responseMark.IsBindingMark = true;
                responseMark.EffectSourceId = "skill:SK-SUP-10";
                state.Map.SetTile(snareResponse.Value, responseMark);
                markAge[snareResponse.Value] = heroRounds;
                markSource[snareResponse.Value] = responseMark.EffectSourceId;
                snareReactionRound = heroRounds;
                state.AddLog("拴索助教应对·绕行时前移约束纹：在 (" + snareResponse.Value.X + "," +
                    snareResponse.Value.Y + ") 补刻，持续3个主角回合，本回合已使用。");
            }
            if (!HasLiving(state, TrackerId)) return;
            if (state.Environment.Wind.Level >= 3) return;
            int marked = 0;
            foreach (GridPosition position in path)
            {
                if (!state.Map.IsInside(position)) continue;
                TileState tile = state.Map.GetTile(position).Clone();
                if (tile.IsWater || tile.IsScorched) continue;
                if (!tile.HasTrace) marked++;
                if (tile.HasEffectLayer && !tile.HasTrace) tile.ClearEffectLayers();
                tile.HasTrace = true;
                tile.EffectSourceId = "skill:SK-CORE-12";
                state.Map.SetTile(position, tile);
                traceAge[position] = heroRounds;
            }
            if (marked > 0) state.AddLog("主角经过的地面留下气味痕（" + marked + " 格），老寻可循味追踪。");
        }

        /// <summary>反应：主角结束移动后不在光柱照明格内（含借遮蔽绕行）时转向其新路线，每场限 2 次。</summary>
        internal void ObserveHeroCommand(CombatState state, CombatCommand command)
        {
            if (state == null || command.Type != CombatCommandType.Move) return;
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive || RotateUsesRemaining <= 0) return;
            RotateTowardHero(state, hero);
        }

        private void RotateTowardHero(CombatState state, UnitState hero)
        {
            foreach (UnitState keeper in state.Units.Values.Where(IsKeeper).Where(unit => unit.IsAlive).OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                if (state.AcademyEnemyArea?.HasPending(keeper.Id) == true) continue;
                FieldLightLaneState lane = LaneOf(state, keeper);
                if (lane == null) continue;
                if (state.Environment.LitCells(state.Map, lane, state.CurrentTime).Contains(hero.Position)) continue;
                GridPosition direction = DirectionToward(keeper.Position, hero.Position);
                if (direction == lane.Direction) continue;
                state.Environment.ReplaceLightLanes(keeper.Id, new[] { new FieldLightLaneState(keeper.Id, keeper.Position, direction, SpotlightLength) });
                spotlightDirection[keeper.Id] = direction;
                rotateUses++;
                state.AddLog("灯台值守转动观测方向，把光柱转向主角的新路线，剩余 " + RotateUsesRemaining + " 次。");
                return;
            }
        }

        /// <summary>主角在同一格停留超过一个自身回合时，该格痕迹加深并保留到被清除。</summary>
        private void SurveyDwell(CombatState state, UnitState hero)
        {
            if (!HasLiving(state, TrackerId)) return;
            if (state.Environment.Wind.Level >= 3) return;
            if (!lastHeroCell.HasValue || lastHeroCell.Value != hero.Position)
            {
                lastHeroCell = hero.Position;
                return;
            }
            TileState tile = state.Map.GetTile(hero.Position).Clone();
            if (tile.IsWater || tile.IsScorched) return;
            if (tile.IsDeepTrace) return;
            if (tile.HasEffectLayer && !tile.HasTrace) tile.ClearEffectLayers();
            tile.HasTrace = true;
            tile.IsDeepTrace = true;
            tile.EffectSourceId = "skill:SK-CORE-12";
            state.Map.SetTile(hero.Position, tile);
            traceAge[hero.Position] = heroRounds;
            state.AddLog("主角在同格停留超过一个自身回合，该格气味痕加深，老寻可循味直扑。");
        }

        private GridPosition? lastHeroCell;

        private void ExpireTraces(CombatState state)
        {
            foreach (GridPosition position in traceAge.Keys.ToArray())
            {
                if (!state.Map.IsInside(position)) { traceAge.Remove(position); continue; }
                TileState tile = state.Map.GetTile(position);
                if (!tile.HasTrace || tile.EffectSourceId != "skill:SK-CORE-12")
                { traceAge.Remove(position); continue; }
                if (tile.IsDeepTrace) continue;
                if (heroRounds - traceAge[position] < TraceRounds) continue;
                TileState cleared = tile.Clone();
                cleared.HasTrace = false;
                cleared.EffectSourceId = null;
                state.Map.SetTile(position, cleared);
                traceAge.Remove(position);
            }
        }

        private void SetSpotlight(CombatState state, UnitState keeper, GridPosition direction)
        {
            spotlightDirection[keeper.Id] = direction;
            state.Environment.ReplaceLightLanes(keeper.Id, new[] { new FieldLightLaneState(keeper.Id, keeper.Position, direction, SpotlightLength) });
            IReadOnlyList<GridPosition> lit = state.Environment.LitCells(state.Map, LaneOf(state, keeper), state.CurrentTime);
            state.AddLog("灯台值守把灯镜转向" + FieldWindState.DirectionName(direction) + "，照亮 " + lit.Count + " 格，遮断处留下暗段。");
        }

        private void ResolveSpotlightDamage(CombatState state, UnitState keeper)
        {
            FieldLightLaneState lane = LaneOf(state, keeper);
            if (lane == null) return;
            GridPosition[] lit = state.Environment.LitCells(state.Map, lane, state.CurrentTime).ToArray();
            UnitState[] targets = state.Units.Values
                .Where(unit => unit.IsAlive && lit.Contains(unit.Position)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
            if (targets.Length == 0) return;
            foreach (UnitState target in targets)
            {
                Roguelite.DamagePacket packet = new Roguelite.DamagePacket("signal-keeper-spotlight", keeper.Id, target.Id,
                    "signal-keeper-spotlight", new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Aether, SpotlightDamage) });
                Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, target.Shield, target.Health);
                target.AbsorbShield(damage.ShieldAbsorbed);
                state.RecordRogueliteShieldAbsorption(target.Id, "signal-keeper-spotlight", damage.ShieldAbsorbed);
                target.TakeDamage(damage.HealthDamage);
            }
            state.AddLog("光柱结算：" + string.Join("、", targets.Select(target => target.DisplayName)) + " 各受到 " + SpotlightDamage + " 点伤害。");
        }

        private static FieldLightLaneState LaneOf(CombatState state, UnitState keeper) =>
            state.Environment.LightLanes.FirstOrDefault(lane => lane.OwnerId == keeper.Id);

        // ── 老库管：旧脉冲、退件、登记 ──────────────────────────────────────────────

        /// <summary>旧脉冲的直线长度与伤害。</summary>
        public const int PulseLength = 4;
        public const int PulseDamage = 5;

        private CombatCommand ChooseStorekeeperCommand(CombatState state, UnitState storekeeper, UnitState hero)
        {
            if (!hero.IsAlive || storekeeper.HasStatus(StatusType.Bound)) return CombatCommand.EndTurn(storekeeper.Id);
            GridPosition? fresh = FieldCells(state).Where(position => !knownFieldCells.Contains(position))
                .Where(position => position.ManhattanDistance(storekeeper.Position) <= RetireRange)
                .Cast<GridPosition?>().FirstOrDefault();
            if (fresh.HasValue)
                return CombatCommand.UseSkillAt(storekeeper.Id, StorekeeperRetireSkillIndex, fresh.Value, default);
            if (pulseCooldown == 0 && PulseCells(state, storekeeper, hero).Contains(hero.Position))
                return CombatCommand.UseSkillAt(storekeeper.Id, StorekeeperPulseSkillIndex, hero.Position, default);
            GridPosition? nearest = FieldCells(state)
                .Where(position => position.ManhattanDistance(storekeeper.Position) <= RetireRange)
                .OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X)
                .Cast<GridPosition?>().FirstOrDefault();
            if (nearest.HasValue)
                return CombatCommand.UseSkillAt(storekeeper.Id, StorekeeperRetireSkillIndex, nearest.Value, default);
            return registerCooldown == 0 && !inspectionMarks.Contains(hero.Id) &&
                storekeeper.Position.ManhattanDistance(hero.Position) <= RegisterRange
                ? CombatCommand.UseSkillAt(storekeeper.Id, StorekeeperRegisterSkillIndex, hero.Position, default)
                : CombatCommand.EndTurn(storekeeper.Id);
        }

        private static GridPosition[] PulseCells(CombatState state, UnitState storekeeper, UnitState hero)
        {
            if (storekeeper.Position.X != hero.Position.X && storekeeper.Position.Y != hero.Position.Y)
                return Array.Empty<GridPosition>();
            if (storekeeper.Position.ManhattanDistance(hero.Position) > PulseLength)
                return Array.Empty<GridPosition>();
            GridPosition direction = DirectionToward(storekeeper.Position, hero.Position);
            var cells = new List<GridPosition>();
            for (int step = 1; step <= PulseLength; step++)
            {
                GridPosition cell = storekeeper.Position + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight) break;
                cells.Add(cell);
            }
            return cells.ToArray();
        }

        internal CombatEffectExecution ResolveStorekeeperCommand(CombatState state, UnitState storekeeper, CombatCommand command)
        {
            UnitState hero = state.GetUnit("hero");
            if (storekeeper == null || !IsStorekeeper(storekeeper) || hero == null ||
                state.AcademyEnemyArea == null || command.UnitId != storekeeper.Id ||
                storekeeper.HasStatus(StatusType.Bound))
                throw new InvalidOperationException("老库管行动不可用。");
            bool legal = command.SlotIndex == StorekeeperPulseSkillIndex
                ? pulseCooldown == 0 && command.Destination == hero.Position &&
                    PulseCells(state, storekeeper, hero).Contains(hero.Position)
                : command.SlotIndex == StorekeeperRetireSkillIndex
                    ? state.Map.IsInside(command.Destination) &&
                        command.Destination.ManhattanDistance(storekeeper.Position) <= RetireRange &&
                        FieldCells(state).Contains(command.Destination)
                    : command.SlotIndex == StorekeeperRegisterSkillIndex && registerCooldown == 0 &&
                        command.Destination == hero.Position && !inspectionMarks.Contains(hero.Id) &&
                        storekeeper.Position.ManhattanDistance(hero.Position) <= RegisterRange;
            if (!legal) throw new InvalidOperationException("老库管行动目标已不可用。");
            CombatEffectExecution result = CombatEffectExecutor.Execute(state, storekeeper.Id,
                CombatEffect.SpendActionPoints(storekeeper.ActionPoints));
            if (command.SlotIndex == StorekeeperPulseSkillIndex)
            {
                TryLegacyPulse(state, storekeeper, hero);
                pulseCooldown = PulseCooldown;
            }
            else if (command.SlotIndex == StorekeeperRetireSkillIndex)
                Retire(state, storekeeper, command.Destination, "退件");
            else
            {
                TryRegister(state, storekeeper, hero);
                registerCooldown = PulseCooldown;
            }
            SnapshotField(state);
            return result;
        }

        /// <summary>
        /// 老库管每自身回合只做一件事，按公开优先级选取：
        /// 反应·退件（主角本回合新生成的效果或装置）→ 旧脉冲（主角在同一条直线上）→ 退件（场上最近的效果或装置）→ 登记（给主角挂待检定标记）。
        /// </summary>
        private void ResolveStorekeeperTurn(CombatState state, UnitState storekeeper, UnitState hero)
        {
            if (hero == null || !hero.IsAlive) { SnapshotField(state); return; }
            if (TryRetireFreshField(state, storekeeper, hero)) return;
            if (pulseCooldown > 0) pulseCooldown--;
            if (pulseCooldown == 0 && TryLegacyPulse(state, storekeeper, hero)) { pulseCooldown = PulseCooldown; return; }
            if (TryRetireNearestField(state, storekeeper, hero)) return;
            TryRegister(state, storekeeper, hero);
        }

        /// <summary>登记：给目标挂公开的待检定标记；其下一次获得的护盾会被优先清除。</summary>
        private bool TryRegister(CombatState state, UnitState storekeeper, UnitState hero)
        {
            if (inspectionMarks.Contains(hero.Id)) return false;
            if (storekeeper.Position.ManhattanDistance(hero.Position) > RegisterRange) return false;
            inspectionMarks.Add(hero.Id);
            state.AddLog("老库管登记：" + hero.DisplayName + "被挂上待检定标记，下一次获得的护盾会被优先清除。");
            return true;
        }

        /// <summary>供护盾结算查询：目标是否被登记，命中即消耗标记。</summary>
        internal bool ConsumeInspectionMark(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return false;
            return inspectionMarks.Remove(unitId);
        }

        /// <summary>反应·退件：主角本回合新生成的效果或装置优先被退掉。</summary>
        private bool TryRetireFreshField(CombatState state, UnitState storekeeper, UnitState hero)
        {
            GridPosition? fresh = FieldCells(state).Where(position => !knownFieldCells.Contains(position))
                .Where(position => position.ManhattanDistance(storekeeper.Position) <= RetireRange)
                .Cast<GridPosition?>().FirstOrDefault();
            if (!fresh.HasValue) return false;
            return Retire(state, storekeeper, fresh.Value, "反应·退件");
        }

        private bool TryRetireNearestField(CombatState state, UnitState storekeeper, UnitState hero)
        {
            GridPosition[] fields = FieldCells(state);
            if (fields.Length == 0) return false;
            GridPosition[] inRange = fields.Where(position => position.ManhattanDistance(storekeeper.Position) <= RetireRange).ToArray();
            if (inRange.Length == 0) return false;
            GridPosition nearest = inRange.OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X).First();
            return Retire(state, storekeeper, nearest, "退件");
        }

        private bool Retire(CombatState state, UnitState storekeeper, GridPosition position, string reason)
        {
            TileState tile = state.Map.GetTile(position).Clone();
            bool fireground = state.RogueSpells?.FireBattle?.HasFireground(position) == true;
            string name = tile.ObjectName() ?? tile.EffectLayerName() ?? (fireground ? "火场" : "场地元素");
            bool device = tile.IsDeviceLike;
            state.RogueSpells?.FireBattle?.ClearEffectLayer(position);
            tile.ClearEffectLayers();
            if (device)
            {
                tile.IsDevice = false; tile.IsOverloadDevice = false;
                tile.IsWardGenerator = false; tile.IsTowerMechanism = false; tile.IsAetherCrystal = false;
                tile.IsPressureCrystal = false; tile.Durability = 0;
            }
            state.Map.SetTile(position, tile);
            state.AddLog("老库管" + reason + "：清除 (" + position.X + "," + position.Y + ") 的" + name + "。");
            return true;
        }

        /// <summary>旧脉冲：沿通过自身与主角的正交直线结算，清空护盾并施加破势。</summary>
        private bool TryLegacyPulse(CombatState state, UnitState storekeeper, UnitState hero)
        {
            if (storekeeper.Position.ManhattanDistance(hero.Position) > PulseLength) return false;
            GridPosition direction = DirectionToward(storekeeper.Position, hero.Position);
            List<UnitState> targets = new List<UnitState>();
            for (int step = 1; step <= PulseLength; step++)
            {
                GridPosition cell = storekeeper.Position + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight) break;
                targets.AddRange(state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell));
            }
            if (targets.Count == 0) return false;
            foreach (UnitState target in targets.OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                target.ClearShield();
                state.ApplyRogueliteBreakStance(target.Id);
                DealFieldDamage(state, storekeeper.Id, "legacy-storekeeper-pulse", target, PulseDamage, Roguelite.DamageComponentKind.Aether);
            }
            state.AddLog("老库管旧脉冲：沿" + FieldWindState.DirectionName(direction) + "结算 " + targets.Count +
                " 个单位，各受 " + PulseDamage + " 点以太伤害、护盾清空并施加破势。");
            return true;
        }

        /// <summary>场上全部场地效果与装置格（退件的可用目标）。</summary>
        private static GridPosition[] FieldCells(CombatState state) => state.Map
            .PositionsWith(tile => tile.HasEffectLayer || tile.IsDeviceLike)
            .Concat(Firegrounds(state)).Distinct().ToArray();

        /// <summary>记录当前场地效果与装置的分布，用于下一回合识别"主角本回合新生成的东西"。</summary>
        private void SnapshotField(CombatState state)
        {
            knownFieldCells.Clear();
            foreach (GridPosition position in FieldCells(state)) knownFieldCells.Add(position);
        }

        // ── 学院岗位单位的专属机制（划线教官／拴索助教／提灯巡查／补盾助教） ──────

        /// <summary>守位：贴着结构时自身回合开始获得的护盾。</summary>
        public const int PositionGuardShield = 4;
        /// <summary>转灯：显影光带长度。</summary>
        public const int LanternLaneLength = 4;
        /// <summary>借障：每次获得的护盾与每场次数。</summary>
        public const int BorrowShield = 4;
        public const int BorrowUses = 2;
        /// <summary>夯墙：每场最多生成的临时重掩体数量。</summary>
        public const int WallBuildLimit = 2;
        /// <summary>刻印：约束纹持续的主角回合数。</summary>
        public const int MarkRounds = 3;

        private readonly Dictionary<string, int> borrowUses = new Dictionary<string, int>(StringComparer.Ordinal);
        private int wallBuilds;
        private readonly Dictionary<GridPosition, int> markAge = new Dictionary<GridPosition, int>();
        private readonly Dictionary<GridPosition, string> markSource = new Dictionary<GridPosition, string>();
        private GridPosition? coverToBreak;

        private void ResolveStaffTurn(CombatState state, UnitState unit, UnitState hero)
        {
            ExpireMarks(state);
            switch (unit.EnemyArchetypeId)
            {
                case "elite_vanguard": ResolveVanguard(state, unit, hero); break;
                case "stone_snare":
                    if (state.AcademyEnemyArea == null) ResolveSnare(state, unit, hero);
                    break;
                case "lantern_revealer":
                    if (state.AcademyEnemyArea == null) ResolveLantern(state, unit, hero);
                    break;
                case "barrier_mender":
                    // 借墙由掩体被拆除时触发，回合开始没有额外授盾动作。
                    break;
            }
        }

        /// <summary>守位是回合被动；学院范围意图负责夯墙，避免回合开始自动筑墙后再行动。</summary>
        private void ResolveVanguard(CombatState state, UnitState vanguard, UnitState hero)
        {
            if (state.AcademyEnemyArea == null && coverToBreak.HasValue)
            {
                GridPosition target = coverToBreak.Value;
                coverToBreak = null;
                if (state.Map.IsInside(target))
                {
                    TileState cover = state.Map.GetTile(target);
                    if (cover.Cover != CoverType.None && !cover.IsDestroyed)
                    {
                        TileState broken = cover.Clone();
                        broken.Durability = 0;
                        state.Map.SetTile(target, broken);
                        state.ResolveAetherCrystalDamage(target, cover.Durability, vanguard.Id);
                        state.AddLog("划线教官拆你所倚：拆掉主角上一回合取盾所倚的掩体。");
                    }
                }
            }
            if (Adjacent(vanguard.Position).Any(position => state.Map.IsInside(position) &&
                IsStructure(state.Map.GetTile(position)) && state.Map.GetTile(position).StructureOwnerUnitId == vanguard.Id))
            {
                state.TryGrantRogueliteShield(vanguard.Id, "vanguard-position-guard", PositionGuardShield);
                state.AddLog("划线教官守位：贴着结构，回合开始获得 " + PositionGuardShield + " 护盾。");
                return;
            }
            if (state.AcademyEnemyArea == null && wallBuilds < WallBuildLimit)
                BuildTemporaryHeavyCover(state, vanguard);
        }

        private void BuildTemporaryHeavyCover(CombatState state, UnitState unit)
        {
            GridPosition[] slots = Adjacent(unit.Position)
                .Where(position => state.Map.IsInside(position) && !state.Map.IsBlocked(position) && !state.IsOccupied(position) &&
                    !state.Map.GetTile(position).HasEffectLayer && state.Map.GetTile(position).Cover == CoverType.None)
                .ToArray();
            if (slots.Length == 0) return;
            GridPosition pick = slots.OrderBy(position => position.Y).ThenBy(position => position.X).First();
            state.Map.SetTile(pick, new TileState { Cover = CoverType.Heavy,
                Durability = TileState.TemporaryHeavyCoverDurability, StructureOwnerUnitId = unit.Id });
            wallBuilds++;
            state.AddLog("划线教官夯墙：在 (" + pick.X + "," + pick.Y + ") 生成临时重掩体（耐久 " + TileState.TemporaryHeavyCoverDurability + "）。");
        }

        /// <summary>拴索助教：在主角所在格刻下约束纹；主角上一回合移动过则先前移一格。</summary>
        private void ResolveSnare(CombatState state, UnitState snare, UnitState hero)
        {
            if (snare.HasStatus(StatusType.Bound)) return;
            if (hero != null && hero.IsAlive)
            {
                TileState marked = state.Map.GetTile(hero.Position).Clone();
                if (!marked.IsBindingMark)
                {
                    marked.ClearEffectLayers();
                    marked.IsBindingMark = true;
                    marked.EffectSourceId = "skill:SK-SUP-08";
                    state.Map.SetTile(hero.Position, marked);
                    markAge[hero.Position] = heroRounds;
                    markSource[hero.Position] = marked.EffectSourceId;
                    state.AddLog("拴索助教刻印：在主角所在格刻下约束纹，持续 " + MarkRounds + " 个主角回合。");
                }
            }
        }

        /// <summary>提灯巡查：沿观测方向投出 4 格显影光带，光带上单位护盾被清除。</summary>
        private void ResolveLantern(CombatState state, UnitState lantern, UnitState hero)
        {
            GridPosition direction = hero != null && hero.IsAlive ? DirectionToward(lantern.Position, hero.Position)
                : (spotlightDirection.TryGetValue(lantern.Id, out GridPosition stored) ? stored : FieldWindState.East);
            spotlightDirection[lantern.Id] = direction;
            state.Environment.ReplaceLightLanes(lantern.Id,
                new[] { new FieldLightLaneState(lantern.Id, lantern.Position, direction, LanternLaneLength) });
            int cleared = 0;
            for (int step = 1; step <= LanternLaneLength; step++)
            {
                GridPosition cell = lantern.Position + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell) || state.Map.GetTile(cell).BlocksLineOfSight) break;
                foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell).ToArray())
                {
                    if (target.Shield <= 0) continue;
                    target.ClearShield();
                    cleared++;
                    state.AddLog(target.DisplayName + "位于显影光带上，护盾被清除。");
                }
            }
            state.AddLog("提灯巡查转灯：亮起" + FieldWindState.DirectionName(direction) + "方向 " + LanternLaneLength +
                " 格光带，清除 " + cleared + " 个单位的护盾，被遮断处留下暗段。");
        }

        /// <summary>借墙：掩体被拆除时，每名补盾助教每场最多两次获得护盾。</summary>
        internal void OnCoverDestroyed(CombatState state, GridPosition position, string sourceUnitId)
        {
            if (state?.Ruleset != CombatRuleset.Roguelite) return;
            UnitState breaker = state.GetUnit(sourceUnitId);
            TileState destroyed = state.Map.GetTile(position);
            UnitState owner = state.GetUnit(destroyed.StructureOwnerUnitId);
            if (state.AcademyEnemyArea != null && breaker?.IsHero == true &&
                owner?.IsAlive == true && owner.EnemyArchetypeId == "elite_vanguard")
            {
                vanguardCoverResponses[owner.Id] = position;
                state.AddLog("划线教官应对：玩家拆除了其自筑墙段，下回合优先处理该位置。");
            }
            foreach (UnitState mender in state.Units.Values.Where(unit => unit.IsAlive && unit.EnemyArchetypeId == "barrier_mender")
                .OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                int used = borrowUses.TryGetValue(mender.Id, out int current) ? current : 0;
                if (used >= BorrowUses) continue;
                if (!state.TryGrantRogueliteShield(mender.Id, "mender-borrow:" + position.X + "," + position.Y + ":" + used, BorrowShield)) continue;
                borrowUses[mender.Id] = used + 1;
                state.AddLog("补盾助教借墙：掩体被拆除，获得 " + BorrowShield + " 护盾（本场剩余 " +
                    (BorrowUses - used - 1) + " 次）。");
            }
        }

        private static bool IsStructure(TileState tile) => tile.Cover == CoverType.Heavy && !tile.IsDestroyed;

        private void ExpireMarks(CombatState state)
        {
            foreach (GridPosition position in markAge.Keys.ToArray())
            {
                if (!state.Map.IsInside(position)) { markAge.Remove(position); markSource.Remove(position); continue; }
                TileState tile = state.Map.GetTile(position);
                if (!tile.IsBindingMark || !markSource.TryGetValue(position, out string source) ||
                    tile.EffectSourceId != source)
                { markAge.Remove(position); markSource.Remove(position); continue; }
                if (heroRounds - markAge[position] < MarkRounds) continue;
                TileState cleared = tile.Clone();
                cleared.IsBindingMark = false;
                cleared.EffectSourceId = null;
                state.Map.SetTile(position, cleared);
                markAge.Remove(position);
                markSource.Remove(position);
            }
        }

        /// <summary>把主角上一回合取过盾的掩体记为"拆你所倚"的目标。</summary>
        internal void NoteHeroCoverAnchor(CombatState state, UnitState hero)
        {
            if (state == null || hero == null) return;
            if (state.AcademyEnemyArea != null)
            {
                foreach (GridPosition position in Adjacent(hero.Position).Where(state.Map.IsInside)
                    .OrderBy(cell => cell.Y).ThenBy(cell => cell.X))
                {
                    TileState wall = state.Map.GetTile(position);
                    UnitState owner = state.GetUnit(wall.StructureOwnerUnitId);
                    if (!IsStructure(wall) || owner?.IsAlive != true || owner.EnemyArchetypeId != "elite_vanguard") continue;
                    vanguardCoverResponses[owner.Id] = position;
                    state.AddLog("划线教官应对：玩家借其自筑墙获得护盾，下回合优先处理该墙段。");
                }
                return;
            }
            TileState standing = state.Map.GetTile(hero.Position);
            if (IsStructure(standing)) { coverToBreak = hero.Position; return; }
            foreach (GridPosition position in Adjacent(hero.Position))
                if (state.Map.IsInside(position) && IsStructure(state.Map.GetTile(position))) { coverToBreak = position; return; }
        }
        // ── 试制员：布放与引爆 ────────────────────────────────────────────────────

        /// <summary>试制箱容量，全场共四件试制件。</summary>
        public const int PrototypeStock = 4;

        /// <summary>已经布放出去的试制件数量。</summary>
        public int PrototypePlaced { get; private set; }
        /// <summary>试制箱剩余数量。</summary>
        public int PrototypeRemaining => Math.Max(0, PrototypeStock - PrototypePlaced);

        /// <summary>
        /// 试制员每自身回合只做一件事，按公开优先级选取：
        /// 反应·引爆（主角站在试制件正交相邻）→ 反应·补放（上一回合有试制件被拆除）→ 布放（试制箱还有库存）。
        /// </summary>
        private void ResolvePrototypeTurn(CombatState state, UnitState hand, UnitState hero)
        {
            if (TryDetonateNearHero(state, hand, hero)) return;
            GridPosition[] current = DeviceCells(state);
            bool removed = knownDeviceCells.Count > 0 && knownDeviceCells.Any(position => !current.Contains(position));
            SnapshotDevices(current);
            if (removed && PrototypeRemaining > 0 && TryDeploy(state, hand, hero, true)) return;
            if (PrototypeRemaining > 0) TryDeploy(state, hand, hero, false);
        }

        private void SnapshotDevices(IReadOnlyList<GridPosition> current)
        {
            knownDeviceCells.Clear();
            foreach (GridPosition position in current) knownDeviceCells.Add(position);
        }

        private static GridPosition[] DeviceCells(CombatState state) => state.Map
            .PositionsWith(tile => tile.IsOverloadDevice || tile.IsWardGenerator).ToArray();

        /// <summary>反应·引爆：主角与某件试制件正交相邻时优先引爆该件，敌我一致。</summary>
        private bool TryDetonateNearHero(CombatState state, UnitState hand, UnitState hero)
        {
            if (hero == null || !hero.IsAlive) return false;
            GridPosition[] adjacent = Adjacent(hero.Position)
                .Where(position => state.Map.IsInside(position) && state.Map.GetTile(position).IsOverloadDevice).ToArray();
            if (adjacent.Length == 0) return false;
            GridPosition target = adjacent.OrderBy(position => position.Y).ThenBy(position => position.X).First();
            int before = state.Map.GetTile(target).Durability;
            TileState tile = state.Map.GetTile(target).Clone();
            tile.Durability = 0;
            state.Map.SetTile(target, tile);
            state.ResolveOverloadDeviceDamage(target, before);
            SnapshotDevices(DeviceCells(state));
            return true;
        }

        /// <summary>布放：在自身正交相邻的空格放下一件试制件；先放护罩发生器，再放过载装置。</summary>
        private bool TryDeploy(CombatState state, UnitState hand, UnitState hero, bool reactive)
        {
            bool wantWard = !Adjacent(hand.Position).Any(position => state.Map.IsInside(position) && state.Map.GetTile(position).IsWardGenerator);
            GridPosition[] slots = Adjacent(hand.Position)
                .Where(position => state.Map.IsInside(position) && !state.Map.IsBlocked(position) && !state.IsOccupied(position) &&
                    !state.Map.GetTile(position).IsDeviceLike && !state.Map.GetTile(position).HasEffectLayer &&
                    state.Map.GetTile(position).Cover == CoverType.None && !state.Map.GetTile(position).IsObjective)
                .ToArray();
            if (slots.Length == 0) return false;
            GridPosition pick = wantWard
                ? slots.OrderBy(position => position.ManhattanDistance(hero?.Position ?? position)).ThenBy(position => position.Y).ThenBy(position => position.X).First()
                : slots.OrderByDescending(position => position.ManhattanDistance(hero?.Position ?? position)).ThenBy(position => position.Y).ThenBy(position => position.X).First();
            TileState tile = state.Map.GetTile(pick).Clone();
            tile.IsDevice = true;
            tile.Durability = TileState.PrototypeDurability;
            tile.IsWardGenerator = wantWard;
            tile.IsOverloadDevice = !wantWard;
            state.Map.SetTile(pick, tile);
            PrototypePlaced++;
            SnapshotDevices(DeviceCells(state));
            string name = wantWard ? "护罩发生器" : "过载装置";
            state.AddLog("试制员" + (reactive ? "反应·补放" : "布放") + "：在 (" + pick.X + "," + pick.Y + ") 放下" + name +
                "，试制箱剩余 " + PrototypeRemaining + " 件。");
            return true;
        }

        private static GridPosition[] Adjacent(GridPosition position) => new[]
        {
            position + new GridPosition(0, 1),
            position + new GridPosition(1, 0),
            position + new GridPosition(0, -1),
            position + new GridPosition(-1, 0)
        };

        /// <summary>
        /// 小铃每自身回合只做一件事，按公开优先级选取：
        /// 引火（场上有火场时把火吹向主角）→ 扬页（散页足够时立页幕）→ 卷页（有散页时打风刃）→ 推风（主角贴身时推开）→ 换风。
        /// 页幕在她下一次回合开始时清除，期间截断穿过该格的攻击线。
        /// </summary>
        private void ResolveLibrarianTurn(CombatState state, UnitState librarian, UnitState hero)
        {
            ClearPaperScreens(state);
            if (hero == null || !hero.IsAlive) return;
            if (TryKindleFire(state, librarian, hero)) return;
            if (TryRaisePaperScreen(state, librarian, hero)) return;
            if (TryWindEdge(state, librarian, hero)) return;
            if (TryPushUnit(state, librarian, hero)) return;
            ChangeWind(state, librarian, hero);
        }

        private void ClearPaperScreens(CombatState state)
        {
            foreach (GridPosition position in state.Map.PositionsWith(tile => tile.HasPaperScreen).ToArray())
            {
                TileState cleared = state.Map.GetTile(position).Clone();
                cleared.HasPaperScreen = false;
                state.Map.SetTile(position, cleared);
            }
        }

        private static GridPosition[] Firegrounds(CombatState state)
        {
            FireBattleState fire = state.RogueSpells?.FireBattle;
            if (fire == null) return Array.Empty<GridPosition>();
            List<GridPosition> fires = new List<GridPosition>();
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (fire.HasFireground(position)) fires.Add(position);
                }
            return fires.ToArray();
        }

        /// <summary>引火：把最近主角的火场沿风向吹出最多三格，沿途与被撞单位受到火焰伤害。</summary>
        private bool TryKindleFire(CombatState state, UnitState librarian, UnitState hero)
        {
            GridPosition[] fires = Firegrounds(state);
            if (fires.Length == 0) return false;
            GridPosition origin = fires.OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X).First();
            GridPosition direction = BlowDirection(state, librarian, hero);
            List<GridPosition> path = new List<GridPosition>();
            GridPosition cursor = origin;
            for (int step = 0; step < 3; step++)
            {
                GridPosition next = cursor + direction;
                if (!state.Map.IsInside(next) || state.Map.IsBlocked(next)) break;
                path.Add(next);
                cursor = next;
            }
            if (path.Count == 0) return false;
            FireBattleState fire = state.RogueSpells.FireBattle;
            fire.RemoveFireground(origin);
            foreach (GridPosition cell in path)
            {
                foreach (UnitState target in state.Units.Values.Where(unit => unit.IsAlive && unit.Position == cell).OrderBy(unit => unit.Id, StringComparer.Ordinal))
                    DealFieldDamage(state, librarian.Id, "wind-librarian-kindled-fire", target, KindledFireDamage, Roguelite.DamageComponentKind.Fire);
                fire.CreateOrRefreshFireground(cell, KindledFireDamage, 2, "wind-librarian-kindled-fire", librarian.Id);
            }
            state.AddLog("小铃引火：把 (" + origin.X + "," + origin.Y + ") 的火场沿" + FieldWindState.DirectionName(direction) +
                "吹出 " + path.Count + " 格，沿途单位受到 " + KindledFireDamage + " 点火焰伤害。");
            return true;
        }

        /// <summary>推风：把 3 格内的一个单位沿主轴推开 1 格。</summary>
        private bool TryPushUnit(CombatState state, UnitState librarian, UnitState hero)
        {
            if (librarian.Position.ManhattanDistance(hero.Position) > 3) return false;
            GridPosition direction = DirectionToward(librarian.Position, hero.Position);
            if (state.ArtifactBattle?.TryPreventForcedMove(hero.Id) == true)
            {
                state.AddLog("小铃推风被定锚效果抵消。");
                return true;
            }
            ForcedMoveResult result = state.ResolveForcedMove(hero, direction, 1, "wind-librarian-push");
            if (result == ForcedMoveResult.Blocked) return false;
            state.AddLog(result == ForcedMoveResult.Moved
                ? "小铃推风：把" + hero.DisplayName + "推开 1 格。"
                : "小铃推风：" + hero.DisplayName + "撞上物块并留在原格。");
            return true;
        }

        /// <summary>卷页：消耗最近主角的一格散页打出风刃。</summary>
        private bool TryWindEdge(CombatState state, UnitState librarian, UnitState hero)
        {
            GridPosition[] papers = state.Map.PositionsWith(tile => tile.IsLoosePaper).ToArray();
            if (papers.Length < ScrollCost) return false;
            if (librarian.Position.ManhattanDistance(hero.Position) > EnemyAbilityCatalog.WindScrollEdge.Range) return false;
            GridPosition spent = papers.OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X).First();
            TileState tile = state.Map.GetTile(spent).Clone();
            tile.IsLoosePaper = false;
            state.Map.SetTile(spent, tile);
            DealFieldDamage(state, librarian.Id, "wind-librarian-scroll-edge", hero, WindEdgeDamage, Roguelite.DamageComponentKind.Aether);
            state.AddLog("小铃卷页：消耗 (" + spent.X + "," + spent.Y + ") 的散页打出风刃，" + hero.DisplayName + "受到 " +
                WindEdgeDamage + " 点伤害。");
            return true;
        }

        /// <summary>扬页：消耗三格散页在朝主角的方向上立起三格页幕。</summary>
        private bool TryRaisePaperScreen(CombatState state, UnitState librarian, UnitState hero)
        {
            GridPosition[] papers = state.Map.PositionsWith(tile => tile.IsLoosePaper).ToArray();
            if (papers.Length < ScreenCost) return false;
            GridPosition direction = DirectionToward(librarian.Position, hero.Position);
            List<GridPosition> line = new List<GridPosition>();
            for (int step = 1; step <= ScreenLength; step++)
            {
                GridPosition cell = librarian.Position + new GridPosition(direction.X * step, direction.Y * step);
                if (!state.Map.IsInside(cell)) break;
                line.Add(cell);
            }
            if (line.Count < ScreenLength) return false;
            foreach (GridPosition cell in papers.OrderBy(position => position.ManhattanDistance(hero.Position))
                .ThenBy(position => position.Y).ThenBy(position => position.X).Take(ScreenCost).ToArray())
            {
                TileState tile = state.Map.GetTile(cell).Clone();
                tile.IsLoosePaper = false;
                state.Map.SetTile(cell, tile);
            }
            foreach (GridPosition cell in line)
            {
                TileState tile = state.Map.GetTile(cell).Clone();
                tile.ClearEffectLayers();
                tile.HasPaperScreen = true;
                state.Map.SetTile(cell, tile);
            }
            state.AddLog("小铃扬页：消耗 " + ScreenCost + " 格散页立起 " + ScreenLength + " 格页幕，穿过页幕的攻击线失效。");
            return true;
        }

        /// <summary>换风：改变风向与风级并公开；有火时把火吹向主角所在的一侧。</summary>
        private bool ChangeWind(CombatState state, UnitState librarian, UnitState hero)
        {
            if (windChanges >= WindChanges) return false;
            GridPosition direction = BlowDirection(state, librarian, hero);
            int level = Math.Min(3, 1 + windChanges);
            if (!state.Environment.Wind.TryChange(direction, level)) return false;
            windChanges++;
            state.AddLog("小铃换风：风向" + FieldWindState.DirectionName(direction) + "｜风级 " + level +
                "，剩余改变 " + state.Environment.Wind.ChangesRemaining + " 次。");
            return true;
        }

        /// <summary>有火时沿火到主角的方向吹，否则朝主角所在的主轴吹。</summary>
        private static GridPosition BlowDirection(CombatState state, UnitState librarian, UnitState hero)
        {
            GridPosition[] fires = Firegrounds(state);
            if (fires.Length > 0)
            {
                GridPosition nearest = fires.OrderBy(position => position.ManhattanDistance(hero.Position))
                    .ThenBy(position => position.Y).ThenBy(position => position.X).First();
                if (nearest != hero.Position) return DirectionToward(nearest, hero.Position);
            }
            return DirectionToward(librarian.Position, hero.Position);
        }

        private static void DealFieldDamage(CombatState state, string sourceUnitId, string sourceId, UnitState target,
            int amount, Roguelite.DamageComponentKind kind)
        {
            Roguelite.DamagePacket packet = new Roguelite.DamagePacket(sourceId, sourceUnitId, target.Id, sourceId,
                new[] { new Roguelite.DamageComponent(kind, amount) });
            Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, target.Shield, target.Health);
            target.AbsorbShield(damage.ShieldAbsorbed);
            state.RecordRogueliteShieldAbsorption(target.Id, sourceId, damage.ShieldAbsorbed);
            target.TakeDamage(damage.HealthDamage);
        }

        /// <summary>观测方向只取正交主轴：距离更大的轴优先，等距时先横后纵。</summary>
        private static GridPosition DirectionToward(GridPosition from, GridPosition to)
        {
            int dx = to.X - from.X, dy = to.Y - from.Y;
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0) return dx > 0 ? FieldWindState.East : FieldWindState.West;
            if (dy != 0) return dy > 0 ? FieldWindState.North : FieldWindState.South;
            return FieldWindState.East;
        }

        /// <summary>场上现存的气味痕格数。</summary>
        public static int TraceCount(CombatState state) =>
            state == null ? 0 : state.Map.PositionsWith(tile => tile.HasTrace).Count();

        public AcademyFieldEnemyRuntime Clone()
        {
            AcademyFieldEnemyRuntime clone = new AcademyFieldEnemyRuntime { heroRounds = heroRounds, rotateUses = rotateUses, lastHeroCell = lastHeroCell, windChanges = windChanges, PrototypePlaced = PrototypePlaced, pulseCooldown = pulseCooldown, registerCooldown = registerCooldown,
                wallBuilds = wallBuilds, coverToBreak = coverToBreak, snareReactionRound = snareReactionRound };
            foreach (KeyValuePair<string, int> pair in borrowUses) clone.borrowUses.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<GridPosition, int> pair in markAge) clone.markAge[pair.Key] = pair.Value;
            foreach (KeyValuePair<GridPosition, string> pair in markSource) clone.markSource[pair.Key] = pair.Value;
            foreach (string mark in inspectionMarks) clone.inspectionMarks.Add(mark);
            foreach (GridPosition cell in knownFieldCells) clone.knownFieldCells.Add(cell);
            foreach (GridPosition cell in knownDeviceCells) clone.knownDeviceCells.Add(cell);
            foreach (KeyValuePair<GridPosition, int> pair in traceAge) clone.traceAge[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, GridPosition> pair in spotlightDirection) clone.spotlightDirection[pair.Key] = pair.Value;
            foreach (string id in spotlightArmed) clone.spotlightArmed.Add(id);
            foreach (string id in arbalistArmed) clone.arbalistArmed.Add(id);
            foreach (KeyValuePair<string, int> pair in vanguardDismantleCooldown)
                clone.vanguardDismantleCooldown.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<string, GridPosition> pair in vanguardCoverResponses)
                clone.vanguardCoverResponses.Add(pair.Key, pair.Value);
            foreach (KeyValuePair<string, string> pair in menderPriorityTargets)
                clone.menderPriorityTargets.Add(pair.Key, pair.Value);
            return clone;
        }
    }
}
