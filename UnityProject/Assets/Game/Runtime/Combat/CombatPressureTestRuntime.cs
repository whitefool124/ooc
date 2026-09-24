using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed class PressureReactionPreview
    {
        public bool WillTrigger { get; }
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public GridPosition Destination { get; }
        public GridPosition ForcedDestination { get; }
        public int ShieldDamage { get; }
        public int HealthDamage { get; }
        public bool FriendlyFire { get; }
        public bool WillPush { get; }
        public string Summary { get; }

        private PressureReactionPreview(bool willTrigger, string sourceUnitId, string targetUnitId,
            GridPosition destination, GridPosition forcedDestination, int shieldDamage, int healthDamage,
            bool friendlyFire, bool willPush, string summary)
        {
            WillTrigger = willTrigger;
            SourceUnitId = sourceUnitId ?? string.Empty;
            TargetUnitId = targetUnitId ?? string.Empty;
            Destination = destination;
            ForcedDestination = forcedDestination;
            ShieldDamage = Math.Max(0, shieldDamage);
            HealthDamage = Math.Max(0, healthDamage);
            FriendlyFire = friendlyFire;
            WillPush = willPush;
            Summary = summary ?? string.Empty;
        }

        public static PressureReactionPreview None(GridPosition destination, string reason = "") =>
            new PressureReactionPreview(false, string.Empty, string.Empty, destination, destination,
                0, 0, false, false, reason);

        internal static PressureReactionPreview Create(string sourceUnitId, UnitState target,
            GridPosition destination, GridPosition forcedDestination, DamageResolution damage,
            bool friendlyFire, bool willPush, string summary) =>
            new PressureReactionPreview(true, sourceUnitId, target.Id, destination, forcedDestination,
                damage.ShieldAbsorbed, damage.HealthDamage, friendlyFire, willPush, summary);
    }

    /// <summary>
    /// Dedicated test-arena runtime for reusable pressure contracts. It is not attached to the
    /// formal encounter pool. The reaction and protection modes can be tested independently.
    /// </summary>
    public sealed class CombatPressureTestRuntime
    {
        public const int ReactionDamage = 6;
        public const int ReactionMinimumRange = 2;
        public const int ReactionMaximumRange = 4;

        private readonly string reactionSourceUnitId;
        private readonly string protectorUnitId;
        private readonly GridPosition? protectedPosition;
        private readonly int efficiencyTurnLimit;
        private bool reactionAvailable = true;
        private int heroTurnsStarted;

        public bool HasReaction => !string.IsNullOrEmpty(reactionSourceUnitId);
        public bool HasProtection => protectedPosition.HasValue;
        public bool HasEfficiency => efficiencyTurnLimit > 0;
        public bool ReactionAvailable => reactionAvailable;
        public GridPosition ProtectedPosition => protectedPosition ?? default;
        public int EfficiencyTurnLimit => efficiencyTurnLimit;
        public int HeroTurnsStarted => heroTurnsStarted;

        public CombatPressureTestRuntime(string reactionSourceUnitId = null,
            string protectorUnitId = null, GridPosition? protectedPosition = null, int efficiencyTurnLimit = 0)
        {
            this.reactionSourceUnitId = reactionSourceUnitId ?? string.Empty;
            this.protectorUnitId = protectorUnitId ?? string.Empty;
            this.protectedPosition = protectedPosition;
            this.efficiencyTurnLimit = Math.Max(0, efficiencyTurnLimit);
        }

        internal void BeginTurn(UnitState unit)
        {
            if (unit?.IsHero == true)
            {
                reactionAvailable = true;
                if (HasEfficiency) heroTurnsStarted++;
            }
        }

        public PressureReactionPreview PreviewHeroMove(CombatState state, GridPosition destination)
        {
            if (!HasReaction) return PressureReactionPreview.None(destination);
            if (!reactionAvailable) return PressureReactionPreview.None(destination, "本回合警戒反应已经触发。 ");
            UnitState source = state?.GetUnit(reactionSourceUnitId);
            UnitState hero = state?.GetUnit("hero");
            if (source == null || hero == null || !source.IsAlive || !hero.IsAlive)
                return PressureReactionPreview.None(destination, "警戒来源已经失效。 ");
            if (source.HasStatus(StatusType.Bound))
                return PressureReactionPreview.None(destination, "警戒者被束缚，反应已取消。 ");
            if (!state.Map.IsInside(destination)) return PressureReactionPreview.None(destination);

            int distance = source.Position.ManhattanDistance(destination);
            bool orthogonal = source.Position.X == destination.X || source.Position.Y == destination.Y;
            if (!orthogonal || distance < ReactionMinimumRange || distance > ReactionMaximumRange)
                return PressureReactionPreview.None(destination,
                    distance < ReactionMinimumRange ? "相邻格位于背弩生警戒死区。 " : "该格不在背弩生的 2–4 格正交警戒线。 ");
            if (!state.HasLineOfSight(source.Position, destination))
                return PressureReactionPreview.None(destination, "警戒线被重掩体、灯藤或烟幕切断。 ");

            UnitState first = FirstUnitOnLine(state, source, destination, hero);
            if (first == null) return PressureReactionPreview.None(destination);
            DamageResolution damage = RogueDamageResolver.Resolve(new DamagePacket(
                "pressure-reaction-preview", source.Id, first.Id, "pressure-reaction",
                new[] { new DamageComponent(DamageComponentKind.Physical, ReactionDamage) }),
                first.Shield, first.Health);
            GridPosition direction = StepDirection(source.Position, first.Position);
            GridPosition forced = first.Position + direction;
            bool canPush = state.Map.IsInside(forced) && !state.Map.IsBlocked(forced) &&
                !state.IsOccupied(forced, first.Id) && state.ArtifactBattle?.CanPreventForcedMove(first.Id) != true;
            TileState forcedTile = state.Map.IsInside(forced) ? state.Map.GetTile(forced) : null;
            bool hitsObject = state.ArtifactBattle?.CanPreventForcedMove(first.Id) != true &&
                forcedTile?.BlocksMovement == true && !string.IsNullOrEmpty(forcedTile.ObjectName());
            if (hitsObject)
            {
                DamageResolution collision = RogueDamageResolver.Resolve(new DamagePacket(
                    "pressure-reaction-collision-preview", source.Id, first.Id, "forced-move-collision",
                    new[] { new DamageComponent(DamageComponentKind.Physical, 4) }),
                    Math.Max(0, first.Shield - damage.ShieldAbsorbed),
                    Math.Max(0, first.Health - damage.HealthDamage));
                damage = new DamageResolution(damage.RawTotal + collision.RawTotal, 0, 0,
                    damage.AfterReduction + collision.AfterReduction, first.Shield,
                    damage.ShieldAbsorbed + collision.ShieldAbsorbed, first.Health,
                    damage.HealthDamage + collision.HealthDamage,
                    first.Health - damage.HealthDamage - collision.HealthDamage <= 0);
            }
            bool friendlyFire = !first.IsHero;
            string summary = "移动结束将触发" + source.DisplayName + "的警戒反应（本回合限 1 次）：" +
                first.DisplayName + "预计护盾 -" + damage.ShieldAbsorbed + "、生命 -" + damage.HealthDamage +
                (canPush ? "，推至 " + Cell(forced) : hitsObject ? "，撞击" + forcedTile.ObjectName() + "并使其耐久 -4" : "，击退受阻") +
                (friendlyFire ? "；射线先命中敌人，构成敌方友伤" : string.Empty) + "。";
            return PressureReactionPreview.Create(source.Id, first, destination, forced, damage,
                friendlyFire, canPush, summary);
        }

        internal CombatEffectExecution ResolveHeroMove(CombatState state, GridPosition destination)
        {
            PressureReactionPreview preview = PreviewHeroMove(state, destination);
            if (!preview.WillTrigger) return CombatEffectExecution.Empty;
            reactionAvailable = false;
            UnitState source = state.GetUnit(preview.SourceUnitId);
            UnitState target = state.GetUnit(preview.TargetUnitId);
            if (source == null || target == null || !source.IsAlive || !target.IsAlive)
                return CombatEffectExecution.Empty;

            DamageResolution damage = RogueDamageResolver.Resolve(new DamagePacket(
                "pressure-reaction", source.Id, target.Id, "pressure-reaction",
                new[] { new DamageComponent(DamageComponentKind.Physical, ReactionDamage) }),
                target.Shield, target.Health);
            var effects = new List<CombatEffect>();
            if (damage.ShieldAbsorbed > 0) effects.Add(CombatEffect.AbsorbShield(target.Id, damage.ShieldAbsorbed));
            if (damage.HealthDamage > 0) effects.Add(CombatEffect.DamageHealth(target.Id, damage.HealthDamage));
            CombatEffectExecution execution = effects.Count == 0
                ? CombatEffectExecution.Empty
                : CombatEffectExecutor.Execute(state, source.Id, effects.ToArray());
            if (target.IsAlive && state.ArtifactBattle?.TryPreventForcedMove(target.Id) != true)
            {
                GridPosition beforePush = target.Position;
                ForcedMoveResult push = state.ResolveForcedMove(target,
                    StepDirection(source.Position, target.Position), 1, "pressure-reaction");
                if (push == ForcedMoveResult.Moved)
                {
                    CombatEffect move = CombatEffect.Move(target.Id, target.Position);
                    var moveResult = new CombatEffectResult(execution.Results.Count, move, source.Id,
                        target.Id, 0, 0, 0, beforePush, target.Position);
                    execution = CombatEffectExecution.Combine(execution,
                        new CombatEffectExecution(new[] { moveResult }));
                }
            }
            state.AddLog("警戒反应触发：" + target.DisplayName + "受到 " + ReactionDamage + " 点物理冲击" +
                (preview.FriendlyFire ? "（敌方友伤）" : string.Empty) + "。 ");
            state.EvaluateOutcome();
            return execution;
        }

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (HasProtection && enemy?.Id == protectorUnitId && protectedPosition.HasValue)
            {
                GridPosition target = protectedPosition.Value;
                TileState tile = state.Map.GetTile(target);
                if (tile.IsDestroyed) return EnemyTactics.Choose(state, enemy, hero);
                if (enemy.Position.ManhattanDistance(target) == 1)
                    return CombatCommand.Interact(enemy.Id, target);

                GridPosition next = BestApproachCell(state, enemy, target);
                if (next != enemy.Position) return CombatCommand.Move(enemy.Id, next);
            }
            return EnemyTactics.Choose(state, enemy, hero);
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            EnemyIntentPresentation basic = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (HasProtection && enemy?.Id == protectorUnitId && protectedPosition.HasValue)
            {
                GridPosition target = protectedPosition.Value;
                TileState tile = state.Map.GetTile(target);
                if (command.Type == CombatCommandType.Interact)
                {
                    int damage = enemy.MainHand?.Damage ?? 0;
                    return new EnemyIntentPresentation("pressure-protect:break:" + tile.Durability,
                        "拆毁稳压器", Cell(target) + " 保护目标",
                        "耐久 " + tile.Durability + "→" + Math.Max(0, tile.Durability - damage) +
                        "；耐久归零时立即失败。可通过击倒、束缚、推离或占住接近格取消。",
                        "interact_destroy", false, default, 0);
                }
                if (command.Type == CombatCommandType.Move)
                    return new EnemyIntentPresentation("pressure-protect:approach:" + Cell(command.Destination),
                        "逼近稳压器", Cell(command.Destination),
                        "抵达后继续接近 " + Cell(target) + "；下一次相邻行动将公开造成 " +
                        (enemy.MainHand?.Damage ?? 0) + " 点耐久伤害。",
                        "move", true, command.Destination, 0);
            }
            if (HasReaction && enemy?.Id == reactionSourceUnitId)
                return new EnemyIntentPresentation(basic.Signature + ":reaction:" + reactionAvailable,
                    basic.ActionName, basic.TargetSummary,
                    basic.ResultSummary + "；警戒反应：主角移动结束进入 2–4 格正交射界时，向射线上首个单位造成 6 伤害并击退 1 格；相邻为死区，每个主角回合限 1 次。",
                    basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
            return basic;
        }

        public string ProtectionSummary(CombatState state)
        {
            if (!HasProtection || state == null) return string.Empty;
            TileState tile = state.Map.GetTile(ProtectedPosition);
            return "保护目标：稳压器位于 " + Cell(ProtectedPosition) + "，当前耐久 " +
                tile.Durability + "；耐久归零立即失败";
        }

        public bool EfficiencyBonusEarned(CombatState state) =>
            HasEfficiency && state?.IsVictory == true && heroTurnsStarted <= efficiencyTurnLimit;

        public string EfficiencySummary(CombatState state)
        {
            if (!HasEfficiency) return string.Empty;
            if (state?.IsVictory == true)
                return EfficiencyBonusEarned(state)
                    ? "效率奖励已达成：在 " + heroTurnsStarted + "／" + efficiencyTurnLimit + " 次主角回合内完成"
                    : "效率奖励已错过：用了 " + heroTurnsStarted + " 次主角回合；基础胜利与基础奖励不变";
            bool available = heroTurnsStarted <= efficiencyTurnLimit;
            return "效率奖励：限 " + efficiencyTurnLimit + " 次主角回合；当前第 " + Math.Max(1, heroTurnsStarted) +
                " 回合；" + (available ? "仍可达成" : "已错过，继续战斗仍可正常胜利");
        }

        public CombatPressureTestRuntime Clone() => new CombatPressureTestRuntime(
            reactionSourceUnitId, protectorUnitId, protectedPosition, efficiencyTurnLimit)
            { reactionAvailable = reactionAvailable, heroTurnsStarted = heroTurnsStarted };

        private static UnitState FirstUnitOnLine(CombatState state, UnitState source,
            GridPosition destination, UnitState hero)
        {
            GridPosition direction = StepDirection(source.Position, destination);
            GridPosition current = source.Position + direction;
            while (current != destination)
            {
                UnitState occupant = state.Units.Values.FirstOrDefault(unit =>
                    unit.IsAlive && unit.Id != source.Id && unit.Position == current);
                if (occupant != null) return occupant;
                current += direction;
            }
            return hero;
        }

        private static GridPosition BestApproachCell(CombatState state, UnitState enemy, GridPosition target)
        {
            var candidates = new List<Tuple<GridPosition, int, int>>();
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                {
                    GridPosition candidate = new GridPosition(x, y);
                    if (candidate == enemy.Position || state.Map.IsBlocked(candidate) || state.IsOccupied(candidate, enemy.Id)) continue;
                    IReadOnlyList<GridPosition> path = CombatMovementQuery.FindPath(state, enemy, candidate);
                    if (path.Count > 1)
                        candidates.Add(Tuple.Create(candidate, candidate.ManhattanDistance(target), path.Count));
                }
            Tuple<GridPosition, int, int> best = candidates.OrderBy(value => value.Item2).ThenBy(value => value.Item3)
                .ThenBy(value => value.Item1.Y).ThenBy(value => value.Item1.X)
                .FirstOrDefault();
            return best == null ? enemy.Position : best.Item1;
        }

        private static GridPosition StepDirection(GridPosition from, GridPosition to) =>
            new GridPosition(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

        private static string Cell(GridPosition position) =>
            ((char)('A' + position.X)).ToString() + (position.Y + 1);
    }
}
