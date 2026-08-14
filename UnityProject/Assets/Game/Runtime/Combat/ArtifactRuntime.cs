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
        public ArtifactStep(int sequence, ArtifactEffectKind kind, string targetId, GridPosition cell, int applied, string detail)
        { Sequence = sequence; Kind = kind; TargetId = targetId; Cell = cell; Applied = applied; Detail = detail; }
        public override string ToString() => Sequence + ":" + Kind + ":" + (TargetId ?? "-") + ":" + Cell + ":" + Applied + ":" + Detail;
    }

    public sealed class ArtifactExecution
    {
        public IReadOnlyList<ArtifactStep> Steps { get; }
        public string Signature => string.Join("|", Steps.Select(step => step.ToString()));
        internal ArtifactExecution(IEnumerable<ArtifactStep> steps) { Steps = steps.ToArray(); }
    }

    public sealed class ArtifactBattleState
    {
        internal readonly Dictionary<string, int> ReservedAp = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> ReservedMana = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly HashSet<string> Anchored = new HashSet<string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, ArtifactReaction> Reactions = new Dictionary<string, ArtifactReaction>(StringComparer.Ordinal);
        internal readonly Dictionary<GridPosition, int> Firegrounds = new Dictionary<GridPosition, int>();
        internal readonly Dictionary<GridPosition, int> Decoys = new Dictionary<GridPosition, int>();
        public CombatState Combat { get; }
        public ArtifactBattleState(CombatState combat)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Combat.AttachArtifactBattle(this);
        }
        public void BeginUnitTurn(string unitId)
        {
            UnitState unit = Combat.GetUnit(unitId); if (unit == null) return;
            if (ReservedAp.TryGetValue(unitId, out int ap)) { unit.GrantActionPoints(ap); ReservedAp.Remove(unitId); }
            if (ReservedMana.TryGetValue(unitId, out int mana)) { unit.RestoreMana(mana); ReservedMana.Remove(unitId); }
            Anchored.Remove(unitId);
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
        public ArtifactExecution ResolveEnemyEntered(string ownerId, string enemyId)
        {
            if (!Reactions.TryGetValue(ownerId, out ArtifactReaction reaction) || reaction.Trigger != ArtifactReactionTrigger.EnemyEnterMarkedCell) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            UnitState owner = Combat.GetUnit(ownerId), enemy = Combat.GetUnit(enemyId);
            if (owner == null || enemy == null || enemy.Position != reaction.MarkedCell || owner.IsHero == enemy.IsHero) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            Reactions.Remove(ownerId); int before = enemy.Health + enemy.Shield; Damage(enemy, reaction.Amount);
            GridPosition pushed = StepAway(owner.Position, enemy.Position, Combat, enemy.Id);
            if (pushed != enemy.Position) enemy.MoveTo(pushed, enemy.Facing);
            return new ArtifactExecution(new[] { new ArtifactStep(0, ArtifactEffectKind.ArmReaction, enemy.Id, enemy.Position, before - enemy.Health - enemy.Shield, "marked_cell_intercept_push") });
        }
        public ArtifactExecution ResolveIncomingRangedHit(string ownerId, string attackerId, int incomingDamage)
        {
            if (!Reactions.TryGetValue(ownerId, out ArtifactReaction reaction) || reaction.Trigger != ArtifactReactionTrigger.IncomingRangedDamage) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            UnitState owner = Combat.GetUnit(ownerId), attacker = Combat.GetUnit(attackerId); if (owner == null || attacker == null) return new ArtifactExecution(Array.Empty<ArtifactStep>());
            Reactions.Remove(ownerId); int prevented = Math.Min(incomingDamage, reaction.Amount); owner.GrantShield(prevented); Damage(attacker, reaction.Duration);
            Combat.AddLog("棱返调节器抵消 " + prevented + " 远程伤害，并向攻击者返还 " + reaction.Duration + " 伤害。");
            return new ArtifactExecution(new[] { new ArtifactStep(0, ArtifactEffectKind.ArmReaction, attacker.Id, attacker.Position, prevented, "ranged_reflect") });
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
        private static readonly StatusType[] NegativeStatuses = { StatusType.Burning, StatusType.Slow, StatusType.Bound, StatusType.ArmorBreak, StatusType.Dazzled };

        public static ArtifactPreview Preview(ArtifactBattleState battle, string sourceId, ArtifactDefinition artifact, ArtifactTarget target, int remainingUses = 1)
        {
            if (battle == null || artifact == null) throw new ArgumentNullException();
            List<string> failures = new List<string>(); CombatState combat = battle.Combat; UnitState source = combat.GetUnit(sourceId);
            if (source == null || !source.IsAlive) failures.Add("施术者不存在或已失去行动能力");
            else { if (source.ActionPoints < artifact.ActionPointCost) failures.Add("行动点不足"); if (source.Mana < artifact.ManaCost) failures.Add("个人魔力不足"); }
            if (remainingUses <= 0) failures.Add("法宝次数已耗尽");
            if (!combat.Map.IsInside(target.Cell)) failures.Add("目标超出地图边界");
            UnitState primary = string.IsNullOrEmpty(target.UnitId) ? combat.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == target.Cell) : combat.GetUnit(target.UnitId);
            if (source != null && combat.Map.IsInside(target.Cell))
            {
                if (ArtifactBattleState.Distance(source.Position, target.Cell) > artifact.Range) failures.Add("目标超出使用范围");
                if (artifact.RequiresLineOfSight && !combat.Map.HasLineOfSight(source.Position, target.Cell)) failures.Add("目标被重掩体遮挡");
            }
            ValidateTarget(battle, source, artifact, target, primary, failures);
            GridPosition[] cells = Selection(combat.Map, target.Cell, artifact.Shape).ToArray();
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
            UnitState secondary = string.IsNullOrEmpty(target.SecondaryUnitId) ? null : battle.Combat.GetUnit(target.SecondaryUnitId);
            source.SpendActionPoint(artifact.ActionPointCost); if (artifact.ManaCost > 0) source.SpendMana(artifact.ManaCost);
            List<ArtifactStep> steps = new List<ArtifactStep>(); int sequence = 0;
            foreach (ArtifactEffectDefinition effect in artifact.Effects)
            {
                IEnumerable<UnitState> targets = Targets(battle.Combat, source, primary, secondary, preview.Cells, effect);
                if (effect.Kind == ArtifactEffectKind.MoveSource || effect.Kind == ArtifactEffectKind.CreateLightCover || effect.Kind == ArtifactEffectKind.DamageObject || effect.Kind == ArtifactEffectKind.DestroyLightCover || effect.Kind == ArtifactEffectKind.CreateFireground || effect.Kind == ArtifactEffectKind.ClearFireground || effect.Kind == ArtifactEffectKind.DeployDecoy)
                { ApplyCellEffect(battle, source, artifact, target.Cell, preview.Cells, effect, steps, ref sequence); continue; }
                foreach (UnitState unit in targets) ApplyUnitEffect(battle, source, unit, secondary, effect, target.Cell, steps, ref sequence);
            }
            battle.Combat.AddLog(artifact.DisplayName + "：产生 " + steps.Count + " 项结果"); battle.Combat.EvaluateOutcome(); return new ArtifactExecution(steps);
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
                ? Selection(combat.Map, target.Cell, artifact.Shape).ToArray()
                : Array.Empty<GridPosition>();
            if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.RestoreMana) && source.Mana >= source.MaxMana)
                failures.Add("个人魔力已满");
            int sourceHealthCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.LoseHealth && effect.Scope == ArtifactEffectScope.Source || effect.Kind == ArtifactEffectKind.BacklashIfTargetSurvives).Sum(effect => effect.Amount);
            if (sourceHealthCost > 0 && source.Health <= sourceHealthCost) failures.Add("生命不足以承担公开代价");
            if (primary != null)
            {
                if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.RestoreHealth) && primary.Health >= primary.MaxHealth) failures.Add("目标未受伤");
                int shieldCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.ConsumeShield).Sum(effect => effect.Amount);
                if (shieldCost > 0 && primary.Shield < shieldCost) failures.Add("目标护盾不足以承担公开代价");
                int targetHealthCost = artifact.Effects.Where(effect => effect.Kind == ArtifactEffectKind.LoseHealth && effect.Scope != ArtifactEffectScope.Source).Sum(effect => effect.Amount);
                if (targetHealthCost > 0 && primary.Health <= targetHealthCost) failures.Add("目标生命不足以承担公开代价");
                if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.ClearNegativeStatuses) && !NegativeStatuses.Any(primary.HasStatus)) failures.Add("目标没有可清除的指定状态");
                if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.TransferShield) && (primary == source || primary.Shield == source.Shield)) failures.Add("需要选择护盾值不同的另一名友军");
            }
            if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.ClearFireground) && !cells.Any(cell => battle.Firegrounds.ContainsKey(cell) || combat.Map.GetTile(cell).SmokeExpiresAt > 0))
                failures.Add("范围内没有临时燃烧地格或烟尘");
            if (artifact.Effects.Any(effect => effect.Kind == ArtifactEffectKind.DelayInitiative && effect.Scope == ArtifactEffectScope.Selection) && !combat.Units.Values.Any(unit => unit.IsAlive && cells.Contains(unit.Position)))
                failures.Add("范围内至少需要一个单位");
        }

        private static IEnumerable<GridPosition> Selection(GridMap map, GridPosition center, ArtifactSelectionShape shape)
        {
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

        private static void ApplyUnitEffect(ArtifactBattleState battle, UnitState source, UnitState target, UnitState secondary, ArtifactEffectDefinition effect, GridPosition cell, List<ArtifactStep> steps, ref int sequence)
        {
            bool heavy = target.EffectiveArmor >= 3; if (effect.Condition == ArtifactEffectCondition.TargetHeavy && !heavy || effect.Condition == ArtifactEffectCondition.TargetLightweight && heavy) return;
            int before = 0, after = 0;
            switch (effect.Kind)
            {
                case ArtifactEffectKind.Damage: before = target.Health + target.Shield; ArtifactBattleState.Damage(target, effect.Amount); after = target.Health + target.Shield; break;
                case ArtifactEffectKind.LoseHealth: before = target.Health; target.TakeDamage(effect.Amount); after = target.Health; break;
                case ArtifactEffectKind.RestoreHealth: before = target.Health; target.Heal(effect.Amount); after = target.Health; break;
                case ArtifactEffectKind.RestoreShield: before = target.Shield; target.GrantShield(effect.Amount); after = target.Shield; break;
                case ArtifactEffectKind.RestoreMana: before = target.Mana; target.RestoreMana(effect.Amount); after = target.Mana; break;
                case ArtifactEffectKind.ConsumeShield: before = target.Shield; target.AbsorbShield(effect.Amount); after = target.Shield; break;
                case ArtifactEffectKind.ApplyStatus: before = target.StatusDuration(effect.Status); target.ApplyStatus(effect.Status, effect.Duration); after = target.StatusDuration(effect.Status); break;
                case ArtifactEffectKind.ClearNegativeStatuses: before = NegativeStatuses.Count(target.HasStatus); foreach (StatusType status in NegativeStatuses) target.ClearStatus(status); after = 0; break;
                case ArtifactEffectKind.ForceMoveTarget:
                    if (battle.TryPreventForcedMove(target.Id)) { before = after = 0; break; }
                    GridPosition destination = StepToward(source.Position, target.Position, effect.Amount, battle.Combat, target.Id); if (destination != target.Position) { before = ArtifactBattleState.Distance(source.Position, target.Position); target.MoveTo(destination, target.Facing); after = ArtifactBattleState.Distance(source.Position, target.Position); } break;
                case ArtifactEffectKind.Reveal: before = target.StatusDuration(StatusType.Revealed); target.ApplyStatus(StatusType.Revealed, effect.Duration); after = target.StatusDuration(StatusType.Revealed); break;
                case ArtifactEffectKind.GrantLightCoverBypass: before = 0; after = effect.Amount; break;
                case ArtifactEffectKind.DelayInitiative: before = target.InitiativeTime; target.SetInitiativeTime(target.InitiativeTime + effect.Amount); after = target.InitiativeTime; break;
                case ArtifactEffectKind.TransferShield:
                    UnitState partner = target; int total = source.Shield + partner.Shield; int sourceShare = total / 2; if ((total & 1) == 1 && source.Shield >= partner.Shield) sourceShare++;
                    int partnerShare = total - sourceShare; before = Math.Abs(source.Shield - partner.Shield); source.AbsorbShield(source.Shield); partner.AbsorbShield(partner.Shield); source.GrantShield(sourceShare); partner.GrantShield(partnerShare); after = Math.Abs(source.Shield - partner.Shield); break;
                case ArtifactEffectKind.ArmReaction: battle.Reactions[source.Id] = new ArtifactReaction(effect.Trigger, effect.Amount, effect.Duration, cell); after = 1; break;
                case ArtifactEffectKind.ArmAnchor: battle.Anchored.Add(source.Id); source.LimitMovementRangeForTurn(1); after = 1; break;
                case ArtifactEffectKind.GrantActionPoints: before = source.ActionPoints; source.GrantActionPoints(effect.Amount); after = source.ActionPoints; break;
                case ArtifactEffectKind.ReserveResources: battle.ReservedAp[source.Id] = effect.Amount; battle.ReservedMana[source.Id] = effect.Duration; after = effect.Amount; break;
                case ArtifactEffectKind.BacklashIfTargetSurvives: if (target.IsAlive) { before = source.Health + source.Shield; ArtifactBattleState.Damage(source, effect.Amount); after = source.Health + source.Shield; } break;
                default: return;
            }
            steps.Add(new ArtifactStep(sequence++, effect.Kind, target.Id, target.Position, Math.Abs(after - before), effect.Condition.ToString()));
        }

        private static void ApplyCellEffect(ArtifactBattleState battle, UnitState source, ArtifactDefinition artifact, GridPosition cell, IReadOnlyList<GridPosition> selection, ArtifactEffectDefinition effect, List<ArtifactStep> steps, ref int sequence)
        {
            if (effect.Kind == ArtifactEffectKind.MoveSource) { GridPosition before = source.Position; source.MoveTo(cell, source.Facing); steps.Add(new ArtifactStep(sequence++, effect.Kind, source.Id, cell, ArtifactBattleState.Distance(before, cell), "move")); return; }
            foreach (GridPosition position in effect.Scope == ArtifactEffectScope.Selection ? selection : new[] { cell })
            {
                TileState tile = battle.Combat.Map.GetTile(position); int applied = 0;
                if (effect.Kind == ArtifactEffectKind.CreateLightCover) { tile = tile.Clone(); tile.Cover = CoverType.Light; tile.Durability = effect.Amount; battle.Combat.Map.SetTile(position, tile); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.DamageObject) { tile.Durability = Math.Max(0, tile.Durability - effect.Amount); applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.DestroyLightCover && tile.Cover == CoverType.Light) { applied = tile.Durability; tile.Durability = 0; }
                else if (effect.Kind == ArtifactEffectKind.CreateFireground) { battle.Firegrounds[position] = effect.Duration; applied = effect.Amount; }
                else if (effect.Kind == ArtifactEffectKind.ClearFireground) { applied = battle.Firegrounds.Remove(position) ? 1 : 0; }
                else if (effect.Kind == ArtifactEffectKind.DeployDecoy) { battle.Decoys[position] = effect.Amount; applied = effect.Amount; }
                steps.Add(new ArtifactStep(sequence++, effect.Kind, null, position, applied, artifact.VfxSemantic));
            }
        }

        private static GridPosition StepToward(GridPosition source, GridPosition target, int distance, CombatState combat, string movingId)
        {
            int dx = Math.Sign(source.X - target.X), dy = Math.Sign(source.Y - target.Y); if (Math.Abs(target.X - source.X) >= Math.Abs(target.Y - source.Y)) dy = 0; else dx = 0;
            GridPosition current = target;
            for (int i = 0; i < distance; i++) { GridPosition next = new GridPosition(current.X + dx, current.Y + dy); if (!combat.Map.IsInside(next) || combat.Map.IsBlocked(next) || combat.IsOccupied(next, movingId) || next == source) break; current = next; }
            return current;
        }
    }
}
