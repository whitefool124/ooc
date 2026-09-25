using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public readonly struct ArtifactTarget
    {
        public GridPosition Cell { get; }
        public string UnitId { get; }
        public string SecondaryUnitId { get; }
        public ArtifactTarget(GridPosition cell, string unitId = null, string secondaryUnitId = null) { Cell = cell; UnitId = unitId; SecondaryUnitId = secondaryUnitId; }
        public static ArtifactTarget At(GridPosition cell) => new ArtifactTarget(cell);
        public static ArtifactTarget Unit(string id, GridPosition cell) => new ArtifactTarget(cell, id);
        public static ArtifactTarget Pair(string first, string second, GridPosition cell) => new ArtifactTarget(cell, first, second);
    }

    public sealed class ArtifactPreview
    {
        public bool CanCommit => Failures.Count == 0;
        public IReadOnlyList<string> Failures { get; }
        public IReadOnlyList<GridPosition> Cells { get; }
        public IReadOnlyList<string> UnitIds { get; }
        public bool FriendlyFireRisk { get; }
        public string Signature { get; }
        internal ArtifactPreview(IEnumerable<string> failures, IEnumerable<GridPosition> cells, IEnumerable<string> units, bool friendlyFire, string signature)
        { Failures = failures.ToArray(); Cells = cells.ToArray(); UnitIds = units.ToArray(); FriendlyFireRisk = friendlyFire; Signature = signature; }
    }

    public readonly struct ArtifactStep
    {
        public int Sequence { get; }
        public ArtifactEffectKind Kind { get; }
        public string TargetId { get; }
        public GridPosition Cell { get; }
        public int Applied { get; }
        public string Detail { get; }
        public IReadOnlyList<CombatFeedbackEvent> Feedback { get; }
        public bool HasResolvedFeedback { get; }
        public ArtifactStep(int sequence, ArtifactEffectKind kind, string targetId, GridPosition cell, int applied, string detail,
            IEnumerable<CombatFeedbackEvent> feedback = null)
        {
            Sequence = sequence; Kind = kind; TargetId = targetId; Cell = cell; Applied = applied; Detail = detail;
            HasResolvedFeedback = feedback != null;
            Feedback = Array.AsReadOnly((feedback ?? Array.Empty<CombatFeedbackEvent>()).ToArray());
        }
        public override string ToString() => Sequence + ":" + Kind + ":" + (TargetId ?? "-") + ":" + Cell + ":" + Applied + ":" + Detail;
    }

    public sealed class ArtifactExecution
    {
        public IReadOnlyList<ArtifactStep> Steps { get; }
        public string SourceUnitId { get; }
        public GridPosition SourcePosition { get; }
        public bool IsTriggered { get; }
        public string Signature => string.Join("|", Steps.Select(step => step.ToString()));
        internal ArtifactExecution(IEnumerable<ArtifactStep> steps, string sourceUnitId = null, GridPosition sourcePosition = default, bool isTriggered = false)
        { Steps = steps.ToArray(); SourceUnitId = sourceUnitId; SourcePosition = sourcePosition; IsTriggered = isTriggered; }
    }

    public sealed class ArtifactBattleState
    {
        internal readonly Dictionary<string, int> ReservedAp = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> ReservedMana = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly HashSet<string> Anchored = new HashSet<string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, ArtifactReaction> Reactions = new Dictionary<string, ArtifactReaction>(StringComparer.Ordinal);
        internal readonly Dictionary<GridPosition, int> Firegrounds = new Dictionary<GridPosition, int>();
        internal readonly Dictionary<GridPosition, int> Decoys = new Dictionary<GridPosition, int>();
        private readonly List<ArtifactExecution> resolvedReactions = new List<ArtifactExecution>();
        public IReadOnlyList<ArtifactExecution> TakeResolvedReactions()
        { var results = resolvedReactions.ToArray(); resolvedReactions.Clear(); return results; }
        public CombatState Combat { get; }
        public bool HasFireground(GridPosition position) =>
            Combat.RogueSpells?.FireBattle.HasFireground(position) == true || Firegrounds.ContainsKey(position);
        public bool RemoveFireground(GridPosition position)
        {
            bool removed = Firegrounds.Remove(position);
            FireBattleState shared = Combat.RogueSpells?.FireBattle;
            if (shared?.HasFireground(position) == true) { shared.RemoveFireground(position); removed = true; }
            return removed;
        }
        public void CreateOrRefreshFireground(GridPosition position, int damage, int duration, string sourceId, string sourceUnitId = null)
        {
            FireBattleState shared = Combat.RogueSpells?.FireBattle;
            if (shared != null)
            {
                shared.CreateOrRefreshFireground(position, damage, duration, sourceId, sourceUnitId);
                Firegrounds.Remove(position);
                return;
            }
            TileState tile = Combat.Map.GetTile(position);
            tile.IsWater = false;
            tile.SmokeExpiresAt = 0;
            Firegrounds[position] = duration;
        }
        public ArtifactBattleState(CombatState combat)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Combat.AttachArtifactBattle(this);
        }
        public void BeginUnitTurn(string unitId)
        {
            UnitState beginning = Combat.GetUnit(unitId);
            if (beginning?.IsHero == true)
                foreach (GridPosition position in Decoys.Keys.ToArray()) RemoveDecoy(position);
            foreach (GridPosition position in Firegrounds.Keys.ToArray())
            {
                int remaining = Firegrounds[position] - 1;
                if (remaining <= 0) Firegrounds.Remove(position); else Firegrounds[position] = remaining;
            }
            UnitState unit = Combat.GetUnit(unitId); if (unit == null) return;
            if (ReservedAp.TryGetValue(unitId, out int ap)) { unit.GrantActionPoints(ap); ReservedAp.Remove(unitId); }
            if (ReservedMana.TryGetValue(unitId, out int mana)) { unit.RestoreMana(mana); ReservedMana.Remove(unitId); }
            Anchored.Remove(unitId);
        }
        public bool IsActiveDecoy(GridPosition position)
        {
            if (!Decoys.ContainsKey(position) || !Combat.Map.IsInside(position)) return false;
            TileState tile = Combat.Map.GetTile(position);
            if (!tile.IsDecoy || tile.Durability <= 0) { Decoys.Remove(position); return false; }
            return true;
        }
        public bool TryGetLureTarget(UnitState enemy, out GridPosition target)
        {
            target = default;
            if (enemy == null || enemy.IsHero || !enemy.IsAlive) return false;
            GridPosition[] candidates = Decoys.Keys.ToArray().Where(IsActiveDecoy)
                .Where(position => enemy.Position.ManhattanDistance(position) <= 5)
                .OrderBy(position => enemy.Position.ManhattanDistance(position)).ThenBy(position => position.Y).ThenBy(position => position.X).ToArray();
            if (candidates.Length == 0) return false;
            target = candidates[0]; return true;
        }
        public void RefreshDecoyAt(GridPosition position)
        {
            if (Decoys.ContainsKey(position) && (!Combat.Map.GetTile(position).IsDecoy || Combat.Map.GetTile(position).Durability <= 0))
                RemoveDecoy(position);
        }
        private void RemoveDecoy(GridPosition position)
        {
            Decoys.Remove(position);
            if (!Combat.Map.IsInside(position)) return;
            TileState tile = Combat.Map.GetTile(position);
            if (!tile.IsDecoy) return;
            tile = tile.Clone(); tile.IsDecoy = false; tile.IsDevice = false; tile.Durability = 0;
            Combat.Map.SetTile(position, tile);
        }
        public bool TryPreventForcedMove(string unitId)
        {
            if (Anchored.Remove(unitId)) return true;
            UnitState unit = Combat.GetUnit(unitId);
            if (unit == null || !unit.IsHero) return false;
            string passiveId = Combat.ItemQuickbar.FirstOrDefault(instanceId =>
            {
                ItemInstance instance = Combat.ItemInventory.Get(instanceId);
                return instance != null && instance.DefinitionId == "G-T13" && instance.RemainingUses > 0;
            });
            if (string.IsNullOrEmpty(passiveId)) return false;
            Combat.ConsumeInventoryItem(passiveId);
            Combat.AddLog("定锚支架自动咬合，抵消强制位移并消耗 1 次。");
            return true;
        }
        public bool CanPreventForcedMove(string unitId)
        {
            if (Anchored.Contains(unitId)) return true;
            UnitState unit = Combat.GetUnit(unitId);
            if (unit == null || !unit.IsHero) return false;
            return Combat.ItemQuickbar.Any(instanceId =>
            {
                ItemInstance instance = Combat.ItemInventory.Get(instanceId);
                return instance != null && instance.DefinitionId == "G-T13" && instance.RemainingUses > 0;
            });
        }
        public ArtifactExecution ResolveEnemyEntered(string ownerId, string enemyId)
        {
            if (!Reactions.TryGetValue(ownerId, out ArtifactReaction reaction) || reaction.Trigger != ArtifactReactionTrigger.EnemyEnterMarkedCell) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            UnitState owner = Combat.GetUnit(ownerId), enemy = Combat.GetUnit(enemyId);
            if (owner == null || enemy == null || enemy.Position != reaction.MarkedCell || owner.IsHero == enemy.IsHero) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            var feedback = new ArtifactFeedbackCapture(owner, enemy); GridPosition source = owner.Position;
            Reactions.Remove(ownerId); int before = enemy.Health + enemy.Shield; Damage(enemy, reaction.Amount);
            GridPosition pushDirection = new GridPosition(
                Math.Sign(enemy.Position.X - owner.Position.X), Math.Sign(enemy.Position.Y - owner.Position.Y));
            if (TryPreventForcedMove(enemy.Id) != true)
                Combat.ResolveForcedMove(enemy, pushDirection, 1, owner.Id + "-intercept");
            var execution = new ArtifactExecution(new[] { new ArtifactStep(0, ArtifactEffectKind.ArmReaction, enemy.Id, enemy.Position,
                before - enemy.Health - enemy.Shield, "marked_cell_intercept_push", feedback.Finish(ArtifactEffectKind.ArmReaction)) }, owner.Id, source, true);
            resolvedReactions.Add(execution); return execution;
        }
        public ArtifactExecution ResolveIncomingRangedHit(string ownerId, string attackerId, int incomingDamage)
        {
            if (!Reactions.TryGetValue(ownerId, out ArtifactReaction reaction) || reaction.Trigger != ArtifactReactionTrigger.IncomingRangedDamage) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            UnitState owner = Combat.GetUnit(ownerId), attacker = Combat.GetUnit(attackerId); if (owner == null || attacker == null) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            var feedback = new ArtifactFeedbackCapture(owner, owner, attacker); GridPosition source = owner.Position;
            Reactions.Remove(ownerId); int prevented = Math.Min(incomingDamage, reaction.Amount); owner.GrantShield(prevented); Damage(attacker, reaction.Duration);
            Combat.AddLog("棱返调节器抵消 " + prevented + " 远程伤害，并向攻击者返还 " + reaction.Duration + " 伤害。");
            var execution = new ArtifactExecution(new[] { new ArtifactStep(0, ArtifactEffectKind.ArmReaction, attacker.Id, attacker.Position,
                prevented, "ranged_reflect", feedback.Finish(ArtifactEffectKind.ArmReaction)) }, owner.Id, source, true);
            resolvedReactions.Add(execution); return execution;
        }
        internal static void Damage(UnitState unit, int amount) { int shield = unit.AbsorbShield(amount); unit.TakeDamage(Math.Max(0, amount - shield)); }
        internal static int Distance(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static GridPosition StepAway(GridPosition source, GridPosition target, CombatState combat, string movingId)
        {
            int dx = Math.Sign(target.X - source.X), dy = Math.Sign(target.Y - source.Y);
            if (Math.Abs(target.X - source.X) >= Math.Abs(target.Y - source.Y)) dy = 0; else dx = 0;
            GridPosition next = new GridPosition(target.X + dx, target.Y + dy);
            return combat.Map.IsInside(next) && !combat.Map.IsBlocked(next) && !combat.IsOccupied(next, movingId) ? next : target;
        }
    }

    internal readonly struct ArtifactReaction
    {
        public ArtifactReactionTrigger Trigger { get; }
        public int Amount { get; }
        public int Duration { get; }
        public GridPosition MarkedCell { get; }
        public ArtifactReaction(ArtifactReactionTrigger trigger, int amount, int duration, GridPosition markedCell)
        { Trigger = trigger; Amount = amount; Duration = duration; MarkedCell = markedCell; }
    }

    public static class ArtifactEngine
    {
        private static readonly StatusType[] NegativeStatuses = { StatusType.Burning, StatusType.Agility, StatusType.Bound, StatusType.BreakStance, StatusType.Dazzled, StatusType.FiregroundVulnerable };

        public static ArtifactPreview Preview(ArtifactBattleState battle, string sourceId, ArtifactDefinition artifact, ArtifactTarget target, int remainingUses = 1)
        {
            if (battle == null || artifact == null) throw new ArgumentNullException();
            List<string> failures = new List<string>(); CombatState combat = battle.Combat; UnitState source = combat.GetUnit(sourceId);
            if (!ArtifactCatalog.IsCurrentlyUsable(artifact.Id)) failures.Add("该法宝使用已停用，等待内容重写");
            if (source == null || !source.IsAlive) failures.Add("施术者不存在或已失去行动能力");
            else { if (source.ActionPoints < artifact.ActionPointCost) failures.Add("行动点不足"); if (source.Mana < artifact.ManaCost) failures.Add("个人魔力不足"); }
            if (remainingUses <= 0) failures.Add("法宝次数已耗尽");
            if (!combat.Map.IsInside(target.Cell)) failures.Add("目标超出地图边界");
            UnitState primary = string.IsNullOrEmpty(target.UnitId) ? combat.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == target.Cell) : combat.GetUnit(target.UnitId);
            if (source != null && combat.Map.IsInside(target.Cell))
            {
                if (ArtifactBattleState.Distance(source.Position, target.Cell) > artifact.Range) failures.Add("目标超出使用范围");
                if (artifact.RequiresLineOfSight && !combat.HasLineOfSight(source.Position, target.Cell)) failures.Add("目标被重掩体或烟幕遮挡");
            }
            ValidateTarget(battle, source, artifact, target, primary, failures);
            GridPosition[] cells = Selection(combat.Map, source == null ? target.Cell : source.Position, target.Cell, artifact.Shape).ToArray();
            string[] units = combat.Units.Values.Where(unit => unit.IsAlive && cells.Contains(unit.Position)).OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id).ToArray();
            bool friendly = source != null && artifact.Effects.Any(effect => effect.AffectAllies) && units.Any(id => combat.GetUnit(id).IsHero == source.IsHero && id != source.Id);
            string signature = artifact.Id + "|" + sourceId + "|" + target.UnitId + "|" + target.SecondaryUnitId + "|" + target.Cell + "|" + string.Join(",", cells) + "|" + string.Join(",", failures);
            return new ArtifactPreview(failures, cells, units, friendly, signature);
        }

        public static ArtifactPreview PreviewInventory(ArtifactBattleState battle, string sourceId, string instanceId, ArtifactTarget target)
        {
            ItemInstance instance = battle?.Combat.ItemInventory.Get(instanceId);
            if (instance == null) throw new InvalidOperationException("背包中不存在该法宝实例");
            return Preview(battle, sourceId, ArtifactCatalog.Get(instance.DefinitionId), target, instance.RemainingUses);
        }

        public static ArtifactExecution ExecuteInventory(ArtifactBattleState battle, string sourceId, string instanceId, ArtifactTarget target)
        {
            ItemInstance instance = battle?.Combat.ItemInventory.Get(instanceId);
            if (instance == null) throw new InvalidOperationException("背包中不存在该法宝实例");
            ArtifactExecution execution = Execute(battle, sourceId, ArtifactCatalog.Get(instance.DefinitionId), target, instance.RemainingUses);
            if (!battle.Combat.ConsumeInventoryItem(instanceId)) throw new InvalidOperationException("法宝次数扣除失败");
            return execution;
        }

        public static ArtifactExecution Execute(ArtifactBattleState battle, string sourceId, ArtifactDefinition artifact, ArtifactTarget target, int remainingUses = 1)
        {
            ArtifactPreview preview = Preview(battle, sourceId, artifact, target, remainingUses); if (!preview.CanCommit) throw new InvalidOperationException(string.Join("；", preview.Failures));
            UnitState source = battle.Combat.GetUnit(sourceId), primary = string.IsNullOrEmpty(target.UnitId) ? battle.Combat.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == target.Cell) : battle.Combat.GetUnit(target.UnitId);
            Dictionary<string, int> heroEnemyVitals = source?.IsHero == true
                ? AcademyFieldEnemyRuntime.CaptureEnemyVitals(battle.Combat) : null;
            UnitState secondary = string.IsNullOrEmpty(target.SecondaryUnitId) ? null : battle.Combat.GetUnit(target.SecondaryUnitId);
            GridPosition sourcePosition = source.Position;
            source.SpendActionPoint(artifact.ActionPointCost); if (artifact.ManaCost > 0) source.SpendMana(artifact.ManaCost);
            List<ArtifactStep> steps = new List<ArtifactStep>(); int sequence = 0;
            foreach (ArtifactEffectDefinition effect in artifact.Effects)
            {
                IEnumerable<UnitState> targets = Targets(battle.Combat, source, primary, secondary, preview.Cells, effect);
                if (effect.Kind == ArtifactEffectKind.MoveSource || effect.Kind == ArtifactEffectKind.CreateLightCover || effect.Kind == ArtifactEffectKind.CreateHeavyCover || effect.Kind == ArtifactEffectKind.DamageObject || effect.Kind == ArtifactEffectKind.DestroyLightCover || effect.Kind == ArtifactEffectKind.CreateFireground || effect.Kind == ArtifactEffectKind.CreateSmoke || effect.Kind == ArtifactEffectKind.ClearFireground || effect.Kind == ArtifactEffectKind.DeployDecoy)
                { ApplyCellEffect(battle, source, artifact, target.Cell, preview.Cells, effect, steps, ref sequence); continue; }
                foreach (UnitState unit in targets) ApplyUnitEffect(battle, source, unit, primary, secondary, effect, target.Cell, steps, ref sequence);
            }
            battle.Combat.AddLog(artifact.DisplayName + "已经生效。"); battle.Combat.EvaluateOutcome();
            if (heroEnemyVitals != null)
                battle.Combat.AcademyFieldEnemy?.ObserveHeroDamage(battle.Combat, heroEnemyVitals);
            return new ArtifactExecution(steps, source.Id, sourcePosition);
        }

        private static void ValidateTarget(ArtifactBattleState battle, UnitState source, ArtifactDefinition artifact, ArtifactTarget target, UnitState primary, List<string> failures)
        {
            CombatState combat = battle.Combat;
            bool occupied = combat.Units.Values.Any(unit => unit.IsAlive && unit.Position == target.Cell);
            TileState tile = combat.Map.IsInside(target.Cell) ? combat.Map.GetTile(target.Cell) : TileState.Empty;
            switch (artifact.TargetRule)
            {
                case ArtifactTargetRule.Self: if (source != primary && (primary != null || target.Cell != source?.Position)) failures.Add("只能以自身为目标"); break;
                case ArtifactTargetRule.Enemy: if (primary == null || primary.IsHero == source?.IsHero) failures.Add("需要选择敌方单位"); break;
                case ArtifactTargetRule.AllyOrSelf: if (primary == null || primary.IsHero != source?.IsHero) failures.Add("需要选择自身或友军"); break;
                case ArtifactTargetRule.AnyUnit: if (primary == null) failures.Add("需要选择单位"); break;
                case ArtifactTargetRule.EmptyCell: if (occupied || tile.BlocksMovement || tile.IsDevice || tile.IsObjective) failures.Add("需要选择可用空格"); break;
                case ArtifactTargetRule.Destructible: if (tile.Cover == CoverType.None && !tile.IsDevice && !tile.IsObjective) failures.Add("需要选择可破坏物或设备"); break;
                case ArtifactTargetRule.Device: if (!tile.IsDevice) failures.Add("需要选择设备"); break;
                case ArtifactTargetRule.TwoAllies:
                    UnitState second = combat.GetUnit(target.SecondaryUnitId); if (primary == null || second == null || primary.Id == second.Id || primary.IsHero != source?.IsHero || second.IsHero != source?.IsHero) failures.Add("需要选择两名不同友军"); break;
            }
            if (source == null) return;
            IReadOnlyList<GridPosition> cells = combat.Map.IsInside(target.Cell)
                ? Selection(combat.Map, source.Position, target.Cell, artifact.Shape).ToArray()
                : Array.Empty<GridPosition>();
            if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.RestoreMana) && source.Mana >= source.MaxMana)
                failures.Add("个人魔力已满");
            int sourceHealthCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.LoseHealth && effect.Scope == ArtifactEffectScope.Source || effect.Kind == ArtifactEffectKind.BacklashIfTargetSurvives).Sum(effect => effect.Amount);
            if (sourceHealthCost > 0 && source.Health <= sourceHealthCost) failures.Add("生命不足以承担公开代价");
            int sourceShieldCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.ConsumeShield && effect.Scope == ArtifactEffectScope.Source).Sum(effect => effect.Amount);
            if (sourceShieldCost > 0 && source.Shield < sourceShieldCost) failures.Add("自身护盾不足以承担公开代价");
            if (primary != null)
            {
                if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.RestoreHealth) && primary.Health >= primary.MaxHealth) failures.Add("目标未受伤");
                int shieldCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.ConsumeShield).Sum(effect => effect.Amount);
                if (shieldCost > 0 && primary.Shield < shieldCost) failures.Add("目标护盾不足以承担公开代价");
                int targetHealthCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.LoseHealth && effect.Scope != ArtifactEffectScope.Source).Sum(effect => effect.Amount);
                if (targetHealthCost > 0 && primary.Health <= targetHealthCost) failures.Add("目标生命不足以承担公开代价");
                if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.ClearNegativeStatuses) && !NegativeStatuses.Any(primary.HasStatus)) failures.Add("目标没有可清除的指定状态");
            }
            if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.ClearFireground) && !cells.Any(cell => battle.HasFireground(cell) || combat.Map.GetTile(cell).SmokeExpiresAt > 0))
                failures.Add("范围内没有临时燃烧地格或烟尘");
            if (artifact.Effects.Any(effect => (effect.Kind == ArtifactEffectKind.DelayInitiative || effect.Kind == ArtifactEffectKind.ForceMoveFromCell) && effect.Scope == ArtifactEffectScope.Selection) && !combat.Units.Values.Any(unit => unit.IsAlive && cells.Contains(unit.Position)))
                failures.Add("范围内至少需要一个单位");
        }

        private static IEnumerable<GridPosition> Selection(GridMap map, GridPosition origin, GridPosition center, ArtifactSelectionShape shape)
        {
            if (shape == ArtifactSelectionShape.Line)
            {
                int x = origin.X, y = origin.Y;
                int dx = Math.Abs(center.X - x), dy = Math.Abs(center.Y - y);
                int stepX = x < center.X ? 1 : -1, stepY = y < center.Y ? 1 : -1;
                int error = dx - dy;
                while (x != center.X || y != center.Y)
                {
                    int doubled = error * 2;
                    if (doubled > -dy) { error -= dy; x += stepX; }
                    if (doubled < dx) { error += dx; y += stepY; }
                    GridPosition position = new GridPosition(x, y);
                    if (map.IsInside(position)) yield return position;
                }
                yield break;
            }
            yield return center; if (shape == ArtifactSelectionShape.Single) yield break;
            GridPosition[] offsets = { new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1) };
            foreach (GridPosition offset in offsets) { GridPosition value = new GridPosition(center.X + offset.X, center.Y + offset.Y); if (map.IsInside(value)) yield return value; }
            if (shape == ArtifactSelectionShape.RadiusOne)
            {
                GridPosition[] diagonals = { new GridPosition(1, 1), new GridPosition(1, -1), new GridPosition(-1, 1), new GridPosition(-1, -1) };
                foreach (GridPosition offset in diagonals) { GridPosition value = new GridPosition(center.X + offset.X, center.Y + offset.Y); if (map.IsInside(value)) yield return value; }
            }
        }

        private static IEnumerable<UnitState> Targets(CombatState combat, UnitState source, UnitState primary, UnitState secondary, IReadOnlyList<GridPosition> cells, ArtifactEffectDefinition effect)
        {
            if (effect.Scope == ArtifactEffectScope.Source) return new[] { source };
            if (effect.Scope == ArtifactEffectScope.Secondary) return secondary == null ? Array.Empty<UnitState>() : new[] { secondary };
            if (effect.Scope == ArtifactEffectScope.Selection) return combat.Units.Values.Where(unit => unit.IsAlive && cells.Contains(unit.Position) && (effect.AffectAllies || unit.IsHero != source.IsHero)).OrderBy(unit => unit.Id, StringComparer.Ordinal);
            return primary == null ? Array.Empty<UnitState>() : new[] { primary };
        }

        private static void ApplyUnitEffect(ArtifactBattleState battle, UnitState source, UnitState target, UnitState primary, UnitState secondary, ArtifactEffectDefinition effect, GridPosition cell, List<ArtifactStep> steps, ref int sequence)
        {
            bool heavy = target.EffectiveArmor >= 3; if (effect.Condition == ArtifactEffectCondition.TargetHeavy && !heavy || effect.Condition == ArtifactEffectCondition.TargetLightweight && heavy) return;
            if (effect.Condition == ArtifactEffectCondition.TargetSurvives && primary?.IsAlive != true) return;
            var capture = new ArtifactFeedbackCapture(source, source, target);
            int before = 0, after = 0;
            switch (effect.Kind)
            {
                case ArtifactEffectKind.Damage: before = target.Health + target.Shield; ArtifactBattleState.Damage(target, effect.Amount); after = target.Health + target.Shield; break;
                case ArtifactEffectKind.LoseHealth: before = target.Health; target.TakeDamage(effect.Amount); after = target.Health; break;
                case ArtifactEffectKind.RestoreHealth: before = target.Health; target.Heal(effect.Amount); after = target.Health; break;
                case ArtifactEffectKind.RestoreShield: before = target.Shield; target.GrantShield(effect.Amount); after = target.Shield; break;
                case ArtifactEffectKind.RestoreMana: before = target.Mana; target.RestoreMana(effect.Amount); after = target.Mana; break;
                case ArtifactEffectKind.ConsumeShield: before = target.Shield; target.AbsorbShield(effect.Amount); after = target.Shield; break;
                case ArtifactEffectKind.ApplyStatus:
                    before = target.StatusDuration(effect.Status);
                    if (effect.Status == StatusType.BreakStance && battle.Combat.Ruleset == CombatRuleset.Roguelite)
                        battle.Combat.ApplyRogueliteBreakStance(target.Id);
                    else target.ApplyStatus(effect.Status, effect.Duration);
                    after = target.StatusDuration(effect.Status);
                    break;
                case ArtifactEffectKind.ClearNegativeStatuses: before = NegativeStatuses.Count(target.HasStatus); foreach (StatusType status in NegativeStatuses) target.ClearStatus(status); after = 0; break;
                case ArtifactEffectKind.ForceMoveTarget:
                    if (battle.TryPreventForcedMove(target.Id)) { before = after = 0; break; }
                    GridPosition pullDirection = DirectionToward(target.Position, source.Position); before = ArtifactBattleState.Distance(source.Position, target.Position); battle.Combat.ResolveForcedMove(target, pullDirection, effect.Amount, source.Id + "-artifact"); after = ArtifactBattleState.Distance(source.Position, target.Position); break;
                case ArtifactEffectKind.ForceMoveFromCell:
                    if (battle.TryPreventForcedMove(target.Id)) { before = after = 0; break; }
                    GridPosition pushDirection = DirectionAway(cell, target.Position); before = ArtifactBattleState.Distance(cell, target.Position); battle.Combat.ResolveForcedMove(target, pushDirection, effect.Amount, source.Id + "-artifact"); after = ArtifactBattleState.Distance(cell, target.Position);
                    break;
                case ArtifactEffectKind.Reveal: before = target.StatusDuration(StatusType.Marked); target.ApplyStatus(StatusType.Marked, effect.Duration, 0, source.Id); after = target.StatusDuration(StatusType.Marked); break;
                case ArtifactEffectKind.GrantLightCoverBypass: before = 0; after = effect.Amount; break;
                case ArtifactEffectKind.DelayInitiative: before = target.ActionValue; target.ChangeActionValue(-effect.Amount); after = target.ActionValue; break;
                case ArtifactEffectKind.ArmReaction: battle.Reactions[source.Id] = new ArtifactReaction(effect.Trigger, effect.Amount, effect.Duration, cell); after = 1; break;
                case ArtifactEffectKind.ArmAnchor: battle.Anchored.Add(source.Id); source.LimitMovementRangeForTurn(1); after = 1; break;
                case ArtifactEffectKind.GrantActionPoints: before = source.ActionPoints; source.GrantActionPoints(effect.Amount); after = source.ActionPoints; break;
                case ArtifactEffectKind.ReserveResources: battle.ReservedAp[source.Id] = effect.Amount; battle.ReservedMana[source.Id] = effect.Duration; after = effect.Amount; break;
                case ArtifactEffectKind.BacklashIfTargetSurvives: if (primary?.IsAlive == true) { before = source.Health; source.TakeDamage(effect.Amount); after = source.Health; } break;
                default: return;
            }
            var feedback = capture.Finish(effect.Kind);
            string utility = null;
            if (after != before)
            {
                if (effect.Kind == ArtifactEffectKind.ArmReaction) utility = "反应已就绪";
                else if (effect.Kind == ArtifactEffectKind.ArmAnchor) utility = "锚定已就绪";
                else if (effect.Kind == ArtifactEffectKind.ReserveResources) utility = "资源已储备";
                else if (effect.Kind == ArtifactEffectKind.GrantActionPoints) utility = "行动 +" + (after - before);
                else if (effect.Kind == ArtifactEffectKind.DelayInitiative) utility = "行动延后 " + (before - after);
            }
            if (utility != null) feedback.Add(ArtifactFeedbackCapture.Utility(source, target, target.Position, utility));
            steps.Add(new ArtifactStep(sequence++, effect.Kind, target.Id, target.Position, Math.Abs(after - before), effect.Condition.ToString(), feedback));
        }

        private static void ApplyCellEffect(ArtifactBattleState battle, UnitState source, ArtifactDefinition artifact, GridPosition cell, IReadOnlyList<GridPosition> selection, ArtifactEffectDefinition effect, List<ArtifactStep> steps, ref int sequence)
        {
            if (effect.Kind == ArtifactEffectKind.MoveSource)
            {
                var capture = new ArtifactFeedbackCapture(source, source); GridPosition before = source.Position;
                source.MoveTo(cell);
                battle.Combat.ResolveDisplacementLanding(source, before);
                steps.Add(new ArtifactStep(sequence++, effect.Kind, source.Id, cell, ArtifactBattleState.Distance(before, cell), "move", capture.Finish(effect.Kind))); return;
            }
            foreach (GridPosition position in effect.Scope == ArtifactEffectScope.Selection ? selection : new[] { cell })
            {
                TileState tile = battle.Combat.Map.GetTile(position); int applied = 0; int durabilityBefore = tile.Durability;
                if (effect.Kind == ArtifactEffectKind.CreateLightCover) { tile = tile.Clone(); tile.Cover = CoverType.Light; tile.Durability = effect.Amount; tile.StructureOwnerUnitId = null; battle.Combat.Map.SetTile(position, tile); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.CreateHeavyCover) { tile = tile.Clone(); tile.Cover = CoverType.Heavy; tile.Durability = effect.Amount; tile.StructureOwnerUnitId = null; battle.Combat.Map.SetTile(position, tile); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.DamageObject) { tile.Durability = Math.Max(0, tile.Durability - effect.Amount); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.DestroyLightCover && tile.Cover == CoverType.Light) { applied = tile.Durability; tile.Durability = 0; }
                else if (effect.Kind == ArtifactEffectKind.CreateFireground) { battle.CreateOrRefreshFireground(position, effect.Amount, effect.Duration, artifact.Id, source.Id); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.CreateSmoke)
                {
                    tile = tile.Clone();
                    tile.SmokeExpiresAt = Math.Max(tile.SmokeExpiresAt, battle.Combat.CurrentTime + effect.Duration);
                    battle.Combat.Map.SetTile(position, tile);
                    applied = effect.Duration;
                }
                else if (effect.Kind == ArtifactEffectKind.ClearFireground)
                {
                    bool removed = battle.RemoveFireground(position);
                    if (tile.SmokeExpiresAt > 0) { tile.SmokeExpiresAt = 0; removed = true; }
                    applied = removed ? 1 : 0;
                }
                else if (effect.Kind == ArtifactEffectKind.DeployDecoy)
                {
                    tile = tile.Clone(); tile.IsDecoy = true; tile.IsDevice = true; tile.Durability = effect.Amount;
                    battle.Combat.Map.SetTile(position, tile); battle.Decoys[position] = effect.Amount; applied = effect.Amount;
                }
                if (effect.Kind == ArtifactEffectKind.DamageObject || effect.Kind == ArtifactEffectKind.DestroyLightCover)
                    battle.Combat.ResolveAetherCrystalDamage(position, durabilityBefore, source.Id);
                var feedback = new List<CombatFeedbackEvent>();
                if (effect.Kind == ArtifactEffectKind.DamageObject || effect.Kind == ArtifactEffectKind.DestroyLightCover)
                {
                    int lost = Math.Max(0, durabilityBefore - battle.Combat.Map.GetTile(position).Durability);
                    if (lost > 0) feedback.Add(new CombatFeedbackEvent(battle.Combat.Map.GetTile(position).IsDestroyed
                        ? CombatFeedbackKind.DestructibleDestroyed : CombatFeedbackKind.DestructibleDamaged,
                        source.Position, position, lost, sourceUnitId: source.Id));
                }
                else if (applied > 0)
                {
                    string message = effect.Kind == ArtifactEffectKind.CreateLightCover ? "掩体已建立" :
                        effect.Kind == ArtifactEffectKind.CreateHeavyCover ? "重掩体已建立" :
                        effect.Kind == ArtifactEffectKind.CreateFireground ? "火场已生成" :
                        effect.Kind == ArtifactEffectKind.CreateSmoke ? "烟幕已展开" :
                        effect.Kind == ArtifactEffectKind.ClearFireground ? "火场已清除" :
                        effect.Kind == ArtifactEffectKind.DeployDecoy ? "诱导灯已部署" : null;
                    if (message != null) feedback.Add(ArtifactFeedbackCapture.Utility(source, null, position, message));
                }
                steps.Add(new ArtifactStep(sequence++, effect.Kind, null, position, applied, artifact.VfxSemantic, feedback));
            }
        }

        private static GridPosition StepToward(GridPosition source, GridPosition target, int distance, CombatState combat, string movingId)
        {
            int dx = Math.Sign(source.X - target.X), dy = Math.Sign(source.Y - target.Y); if (Math.Abs(target.X - source.X) >= Math.Abs(target.Y - source.Y)) dy = 0; else dx = 0;
            GridPosition current = target;
            for (int i = 0; i < distance; i++) { GridPosition next = new GridPosition(current.X + dx, current.Y + dy); if (!combat.Map.IsInside(next) || combat.Map.IsBlocked(next) || combat.IsOccupied(next, movingId) || next == source) break; current = next; }
            return current;
        }

        private static GridPosition StepAwayFrom(GridPosition center, GridPosition target, int distance, CombatState combat, string movingId)
        {
            int dx = Math.Sign(target.X - center.X), dy = Math.Sign(target.Y - center.Y);
            if (dx != 0 && dy != 0) { if (Math.Abs(target.X - center.X) >= Math.Abs(target.Y - center.Y)) dy = 0; else dx = 0; }
            if (dx == 0 && dy == 0) return target;
            GridPosition current = target;
            for (int i = 0; i < distance; i++)
            {
                GridPosition next = new GridPosition(current.X + dx, current.Y + dy);
                if (!combat.Map.IsInside(next) || combat.Map.IsBlocked(next) || combat.IsOccupied(next, movingId)) break;
                current = next;
            }
            return current;
        }

        private static GridPosition DirectionToward(GridPosition from, GridPosition to)
        {
            int dx = Math.Sign(to.X - from.X), dy = Math.Sign(to.Y - from.Y);
            if (Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y)) dy = 0; else dx = 0;
            return new GridPosition(dx, dy);
        }

        private static GridPosition DirectionAway(GridPosition center, GridPosition target)
        {
            int dx = Math.Sign(target.X - center.X), dy = Math.Sign(target.Y - center.Y);
            if (dx != 0 && dy != 0)
            {
                if (Math.Abs(target.X - center.X) >= Math.Abs(target.Y - center.Y)) dy = 0;
                else dx = 0;
            }
            return new GridPosition(dx, dy);
        }
    }
}
