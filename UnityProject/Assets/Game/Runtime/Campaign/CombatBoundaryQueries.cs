using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    // Read-only player-facing contract. Presentation asks this service; it never reproduces combat rules.
    public sealed class CombatAvailabilityQuery
    {
        private readonly BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
        public CombatActionPreview Preview(CombatState state, string action, string targetId) => adapter.BuildPreview(state, action, targetId);
        public string CellFailure(CombatState state, string action, GridPosition position) => adapter.InvalidReasonForCell(state, action, position);
    }

    // Keeps executable enemy commands out of presentation while intent and execution share one stable plan.
    public sealed class EnemyTurnPlanBook
    {
        private readonly Dictionary<string, CombatCommand> commands = new Dictionary<string, CombatCommand>(StringComparer.Ordinal);
        private readonly Dictionary<string, CombatState> turnStartPreviews = new Dictionary<string, CombatState>(StringComparer.Ordinal);
        public EnemyIntentPresentation GetPublicIntent(CombatState state, UnitState enemy, UnitState hero)
        {
            if (state == null || enemy == null || hero == null) return null;
            if (state.LampVines.IsHiddenFrom(state, hero, enemy))
                return new EnemyIntentPresentation("concealed:" + enemy.Id + ":" + state.LampVines.PublicCell(enemy.Id), "藤内行动", "已知灯藤格 " + state.LampVines.PublicCell(enemy.Id), "真实位置隐藏；仅保留当前公开候选格。", "move", true, state.LampVines.PublicCell(enemy.Id), 0);
            CombatCommand command = GetOrCreate(state, enemy, hero);
            if (command.Type == CombatCommandType.AttackCell)
                return new EnemyIntentPresentation("vine-search:" + enemy.Id + ":" + command.Destination, "普通攻击搜查", "公开灯藤格 " + command.Destination, "沿用原动作伤害与费用；未摧毁不推进，命中藏身单位则显形。", "attack", true, command.Destination, 0, affectedCells: new[] { command.Destination });
            if (turnStartPreviews.TryGetValue(enemy.Id, out CombatState previewState))
            {
                state = previewState;
                enemy = state.GetUnit(enemy.Id);
                hero = state.GetUnit(hero.Id);
            }
            if (state.AcademyEnemyArea != null &&
                (state.AcademyEnemyArea.HasPending(enemy.Id) || command.Type == CombatCommandType.UseSkill &&
                    (command.SlotIndex == AcademyEnemyAreaRuntime.PrepareSkillIndex || command.SlotIndex == AcademyEnemyAreaRuntime.ResolveSkillIndex)))
                return state.AcademyEnemyArea.PresentIntent(state, enemy, command);
            if (command.Type == CombatCommandType.UseSkill && command.SlotIndex == AcademyEnemyGrowthRuntime.CommandSkillIndex &&
                state.AcademyEnemyGrowth != null)
                return state.AcademyEnemyGrowth.PresentIntent(enemy);
            EnemyIntentPresentation intent = state.RainLanternCourt != null ? state.RainLanternCourt.PresentIntent(state, enemy, command)
                : state.GreenhouseCollectionRoom != null ? state.GreenhouseCollectionRoom.PresentIntent(state, enemy, command)
                : state.ThreeMaterialPressure != null ? state.ThreeMaterialPressure.PresentIntent(state, enemy, command)
                : state.AcademyCoreBoss != null ? state.AcademyCoreBoss.PresentIntent(state, enemy, command)
                : state.AcademyFieldEnemy != null && AcademyFieldEnemyRuntime.Handles(enemy) ? state.AcademyFieldEnemy.PresentIntent(state, enemy, command)
                : state.PressureTest != null ? state.PressureTest.PresentIntent(state, enemy, command)
                : CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (state.AcademyCoreBoss != null && enemy.EnemyArchetypeId == "core_overseer" &&
                command.Type == CombatCommandType.UseSkill) return intent;
            return intent?.WithMovementPreview(state, enemy, command).WithAttackPreview(state, enemy, command);
        }
        public CombatCommand GetExecutionCommand(CombatState state, UnitState enemy, UnitState hero) => GetOrCreate(state, enemy, hero);
        public void Invalidate() { commands.Clear(); turnStartPreviews.Clear(); }
        public bool HasPlanFor(string enemyId) => !string.IsNullOrEmpty(enemyId) && commands.ContainsKey(enemyId);
        private CombatCommand GetOrCreate(CombatState state, UnitState enemy, UnitState hero)
        {
            if (!commands.TryGetValue(enemy.Id, out CombatCommand command))
            {
                if (state.LampVines.IsHiddenFrom(state, enemy, hero) && state.AcademyEnemyArea?.HasPending(enemy.Id) != true)
                {
                    if (state.ActiveUnitId != enemy.Id)
                    {
                        state = state.Clone(); enemy = state.GetUnit(enemy.Id); hero = state.GetUnit(hero.Id);
                        CombatResolver.BeginTurn(state, enemy.Id); turnStartPreviews[enemy.Id] = state;
                    }
                    command = state.LampVines.PlanSearch(state, enemy, hero);
                    commands.Add(enemy.Id, command); return command;
                }
                // The legacy instructor raises its wall at turn start. Plan on the same
                // resulting board, otherwise its advertised route can cross that new wall.
                if (state.ActiveUnitId != enemy.Id && state.Ruleset == CombatRuleset.Roguelite &&
                    state.AcademyEnemyArea == null && state.AcademyFieldEnemy != null &&
                    enemy.EnemyArchetypeId == "elite_vanguard")
                {
                    state = state.Clone();
                    enemy = state.GetUnit(enemy.Id);
                    hero = state.GetUnit(hero.Id);
                    CombatResolver.BeginTurn(state, enemy.Id);
                    turnStartPreviews[enemy.Id] = state;
                }
                command = state.RainLanternCourt != null
                    ? state.RainLanternCourt.ChooseEnemyCommand(state, enemy, hero)
                    : state.GreenhouseCollectionRoom != null
                        ? state.GreenhouseCollectionRoom.ChooseEnemyCommand(state, enemy, hero)
                        : state.ThreeMaterialPressure != null
                            ? state.ThreeMaterialPressure.ChooseEnemyCommand(state, enemy, hero)
                        : state.AcademyCoreBoss != null
                            ? state.AcademyCoreBoss.ChooseEnemyCommand(state, enemy, hero)
                            : state.AcademyFieldEnemy != null && AcademyFieldEnemyRuntime.Handles(enemy)
                                ? state.AcademyFieldEnemy.ChooseEnemyCommand(state, enemy, hero)
                        : state.PressureTest != null
                            ? state.PressureTest.ChooseEnemyCommand(state, enemy, hero)
                        : EnemyTactics.Choose(state, enemy, hero);
                if (state.AcademyEnemyArea != null && state.AcademyEnemyGrowth != null)
                    command = KeepDeclaredAcademyIntent(state, enemy, hero, command);
                if (state.AcademyEnemyArea != null)
                    command = state.AcademyEnemyArea.Choose(state, enemy, hero, command);
                if (state.AcademyEnemyGrowth != null && state.AcademyEnemyArea?.HasPending(enemy.Id) != true)
                    command = state.AcademyEnemyGrowth.Choose(state, enemy, command);
                commands.Add(enemy.Id, command);
            }
            return command;
        }

        public static CombatCommand KeepDeclaredAcademyIntent(CombatState state, UnitState enemy, UnitState hero,
            CombatCommand command)
        {
            if (command.Type == CombatCommandType.Attack)
                return CombatCommand.EndTurn(enemy.Id);
            if (command.Type == CombatCommandType.Move)
            {
                bool arbalistRetreat = enemy.EnemyArchetypeId == "rune_arbalist" &&
                    command.Destination.ManhattanDistance(hero.Position) >
                        enemy.Position.ManhattanDistance(hero.Position) &&
                    (enemy.Position.ManhattanDistance(hero.Position) < EnemyAbilityCatalog.WindlassBolt.MinimumRange ||
                        !state.HasLineOfSight(enemy.Position, hero.Position));
                bool trackerFollowingTrace = enemy.EnemyArchetypeId == "elder_tracker_hound" &&
                    state.Map.IsInside(command.Destination) && state.Map.GetTile(command.Destination).HasTrace;
                bool followingDecoy = state.ArtifactBattle?.TryGetLureTarget(enemy, out _) == true;
                return arbalistRetreat || trackerFollowingTrace || followingDecoy
                    ? command : CombatCommand.EndTurn(enemy.Id);
            }
            if (command.Type == CombatCommandType.UseSkill && command.SlotIndex == 0 &&
                (enemy.EnemyArchetypeId == "elite_vanguard" || enemy.EnemyArchetypeId == "breach_ram" ||
                    enemy.EnemyArchetypeId == "signal_keeper" || enemy.EnemyArchetypeId == "wind_librarian" ||
                    enemy.EnemyArchetypeId == "legacy_storekeeper" || enemy.EnemyArchetypeId == "prototype_hand" ||
                    enemy.EnemyArchetypeId == "lantern_revealer"))
                return CombatCommand.EndTurn(enemy.Id);
            return command;
        }
    }

    // Chooses one legal player action at a time. Limited tactical items require
    // explicit per-instance consent from AutoCombatSettings.
    public static class AutoCombatPlanner
    {
        private static readonly StatusType[] HarmfulStatuses =
        {
            StatusType.Burning, StatusType.Slow, StatusType.Bound, StatusType.ArmorBreak,
            StatusType.Dazzled, StatusType.BreakStance, StatusType.FiregroundVulnerable,
            StatusType.Marked
        };
        private static readonly StatusType[] HelpfulStatuses =
        {
            StatusType.Agility, StatusType.Strength, StatusType.SpellPower, StatusType.Speed,
            StatusType.Range, StatusType.ShieldEfficiency, StatusType.ShieldGrant,
            StatusType.MoveBonus, StatusType.Invulnerable
        };

        public static CombatCommand Choose(CombatState state) => Choose(state, AutoCombatStyle.Balanced);

        public static AutoCombatDecision ChooseDecision(CombatState state, AutoCombatSettings settings)
        {
            if (settings == null) return AutoCombatDecision.FromCommand(Choose(state));
            settings.BindQuickbar(state);
            CombatCommand command = Choose(state, settings.Style);
            if (state == null || state.IsVictory || state.IsDefeat || state.ActiveUnitId != "hero")
                return AutoCombatDecision.FromCommand(command);
            UnitState hero = state.GetUnit("hero");
            if (hero == null || !hero.IsAlive) return AutoCombatDecision.FromCommand(command);

            UnitState[] enemies = state.Units.Values.Where(unit => unit.IsAlive && !unit.IsHero).ToArray();
            GridPosition[] objectives = state.Objectives.OfType<DestructionObjective>()
                .Where(objective => !objective.IsComplete(state)).SelectMany(objective => objective.Positions)
                .Where(position => !state.Map.GetTile(position).IsDestroyed).Distinct().ToArray();
            GridPosition[] investigations = state.Objectives.OfType<InvestigationObjective>()
                .SelectMany(objective => objective.Positions).Where(position => !state.IsInvestigated(position))
                .Distinct().ToArray();
            double normalScore = 0;
            if (command.Type != CombatCommandType.EndTurn)
            {
                CombatState clone = state.Clone();
                if (new CombatCommandExecutionService().Execute(clone, clone.RogueSpells?.FireBattle, command).Accepted &&
                    !UnsafeOutcome(state, clone))
                    normalScore = Math.Max(0, ScoreOutcome(state, clone, enemies, objectives, investigations, settings.Style));
            }
            double bestItemScore = Math.Max(8, normalScore + 8);
            AutoCombatDecision best = AutoCombatDecision.FromCommand(command);
            string[] quickbar = state.RogueEquipment?.ItemQuickbarInstanceIds ?? state.ItemQuickbar;
            for (int slot = 0; slot < settings.SlotCount && slot < quickbar.Length; slot++)
            {
                string instanceId = quickbar[slot];
                var tactical = state.RogueEquipment?.TacticalItem(instanceId);
                ItemInstance inventory = tactical == null ? state.ItemInventory.Get(instanceId) : null;
                int uses = tactical?.ChargesCurrent ?? inventory?.RemainingUses ?? 0;
                if (!settings.MaySpend(slot, instanceId, uses)) continue;
                string definitionId = tactical?.DefinitionId ?? inventory?.DefinitionId;
                if (definitionId == "medkit" || definitionId == "shield_cell")
                {
                    CombatCommand itemCommand = CombatCommand.UseQuickbar(hero.Id, slot);
                    CombatState clone = state.Clone();
                    if (!new CombatCommandExecutionService().Execute(clone, clone.RogueSpells?.FireBattle, itemCommand).Accepted ||
                        UnsafeOutcome(state, clone)) continue;
                    double score = ScoreOutcome(state, clone, enemies, objectives, investigations, settings.Style) -
                        ItemChargeCost(settings.Style);
                    if (score <= bestItemScore) continue;
                    bestItemScore = score;
                    best = AutoCombatDecision.FromCommand(itemCommand);
                    continue;
                }
                // This passive reactor already consumes charges when it blocks a push or pull.
                if (definitionId == "G-T13" || !ArtifactCatalog.All.Any(item => item.Id == definitionId)) continue;
                ArtifactDefinition artifact = ArtifactCatalog.Get(definitionId);
                CombatState probe = state.Clone();
                ArtifactBattleState previewBattle = probe.ArtifactBattle ?? new ArtifactBattleState(probe);
                foreach (ArtifactTarget target in ArtifactTargets(state, hero, enemies, artifact))
                {
                    ArtifactPreview preview = ArtifactEngine.Preview(previewBattle, hero.Id, artifact, target, uses);
                    if (!preview.CanCommit || preview.FriendlyFireRisk || !string.IsNullOrEmpty(preview.ContainerWarning)) continue;
                    CombatState clone = state.Clone();
                    ArtifactBattleState battle = clone.ArtifactBattle ?? new ArtifactBattleState(clone);
                    try { ArtifactEngine.Execute(battle, hero.Id, artifact, target, uses); }
                    catch (InvalidOperationException) { continue; }
                    if (UnsafeOutcome(state, clone)) continue;
                    double score = ScoreOutcome(state, clone, enemies, objectives, investigations, settings.Style) -
                        ItemChargeCost(settings.Style);
                    if (score <= bestItemScore) continue;
                    bestItemScore = score;
                    best = AutoCombatDecision.FromArtifact(slot, instanceId, target);
                }
            }
            return best;
        }

        public static CombatCommand Choose(CombatState state, AutoCombatStyle style)
        {
            UnitState hero = state?.GetUnit("hero");
            if (hero == null || !hero.IsAlive || state.ActiveUnitId != hero.Id ||
                state.IsVictory || state.IsDefeat || hero.ActionPoints < CombatResolver.BasicActionPointCost)
                return CombatCommand.EndTurn("hero");

            UnitState[] enemies = state.Units.Values.Where(unit => unit.IsAlive && !unit.IsHero)
                .OrderBy(unit => hero.Position.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
            GridPosition[] objectiveCells = state.Objectives.OfType<DestructionObjective>()
                .Where(objective => !objective.IsComplete(state)).SelectMany(objective => objective.Positions)
                .Where(position => !state.Map.GetTile(position).IsDestroyed).Distinct().ToArray();
            GridPosition[] investigationCells = state.Objectives.OfType<InvestigationObjective>()
                .SelectMany(objective => objective.Positions).Where(position => !state.IsInvestigated(position))
                .Distinct().ToArray();
            GridPosition[] entryGoals = state.Objectives.OfType<CaptureObjective>()
                .Where(objective => !objective.IsComplete(state)).SelectMany(objective => objective.Positions)
                .Concat(state.Objectives.OfType<ExtractionObjective>()
                    .Where(objective => !objective.IsComplete(state)).SelectMany(objective => objective.Positions))
                .Distinct().ToArray();
            GridPosition[] travelGoals = enemies.Select(enemy => state.LampVines.IsHiddenFrom(state, hero, enemy)
                    ? state.LampVines.PublicCell(enemy.Id) : enemy.Position)
                .Concat(objectiveCells)
                .Concat(entryGoals)
                .Concat(investigationCells)
                .Distinct().ToArray();
            var commands = new List<CombatCommand>();
            foreach (UnitState enemy in enemies)
            {
                if (state.LampVines.IsHiddenFrom(state, hero, enemy))
                    commands.Add(CombatCommand.AttackCell(hero.Id, state.LampVines.PublicCell(enemy.Id)));
                else commands.Add(CombatCommand.Attack(hero.Id, enemy.Id));
            }
            foreach (GridPosition position in objectiveCells)
                if (hero.Position.ManhattanDistance(position) == 1)
                    commands.Add(CombatCommand.Interact(hero.Id, position));
            foreach (GridPosition position in investigationCells)
                if (hero.Position.ManhattanDistance(position) == 1)
                    commands.Add(CombatCommand.Interact(hero.Id, position));

            GridPosition[] spellCells = travelGoals.OrderBy(position => hero.Position.ManhattanDistance(position))
                .Take(12).Concat(new[] { hero.Position }).Distinct().ToArray();
            int spellSlots = state.RogueSpells != null ? Roguelite.RogueRuntimeConstants.SpellSlotCount : 2;
            for (int slot = 0; slot < spellSlots; slot++)
            {
                if (state.RogueSpells != null)
                {
                    var spell = state.RogueSpells.DefinitionAtSlot(slot);
                    if (spell == null || spell.Role == "passive" ||
                        hero.ActionPoints < spell.ActionPointCost ||
                        hero.Mana < state.RogueSpells.EffectiveManaCost(spell) ||
                        state.RogueSpells.CooldownRemaining(spell.DefinitionId) > 0 ||
                        state.RogueSpells.BattleUsesRemaining(spell.DefinitionId) == 0) continue;
                }
                foreach (UnitState enemy in enemies.Where(enemy => !state.LampVines.IsHiddenFrom(state, hero, enemy)))
                    commands.Add(CombatCommand.UseSkill(hero.Id, slot, enemy.Id));
                commands.Add(CombatCommand.UseSkill(hero.Id, slot, hero.Id));
                foreach (GridPosition cell in spellCells)
                    commands.Add(CombatCommand.UseSkillAt(hero.Id, slot, cell, Direction(hero.Position, cell)));
            }

            var executor = new CombatCommandExecutionService();
            CombatCommand best = CombatCommand.EndTurn(hero.Id);
            double bestScore = 0;
            foreach (CombatCommand command in commands)
            {
                CombatState clone = state.Clone();
                CombatCommandExecutionResult result = executor.Execute(clone, clone.RogueSpells?.FireBattle, command);
                if (!result.Accepted || UnsafeOutcome(state, clone)) continue;
                double score = ScoreOutcome(state, clone, enemies, objectiveCells, investigationCells, style);
                if (score > bestScore) { bestScore = score; best = command; }
            }
            if (bestScore > 1) return best;

            // A direct Manhattan step can fail around walls. Find a full route to a
            // legal firing or objective cell, then submit only this turn's segment.
            IReadOnlyList<GridPosition> route = null;
            double routeScore = double.MaxValue;
            foreach (GridPosition target in travelGoals.OrderBy(position => hero.Position.ManhattanDistance(position)).Take(6))
            {
                bool entryGoal = entryGoals.Contains(target);
                bool interactionGoal = objectiveCells.Contains(target) || investigationCells.Contains(target);
                for (int y = 0; y < state.Map.Height; y++)
                    for (int x = 0; x < state.Map.Width; x++)
                    {
                        var cell = new GridPosition(x, y);
                        int distance = cell.ManhattanDistance(target);
                        if (entryGoal ? cell != target : interactionGoal ? distance != 1 :
                            distance < hero.BasicAttack.MinimumRange ||
                            distance > hero.EffectiveRange(hero.BasicAttack.Range) ||
                            distance > 1 && !state.HasLineOfSight(cell, target)) continue;
                        if (cell == hero.Position || state.Map.IsBlocked(cell) || state.IsOccupied(cell, hero.Id)) continue;
                        IReadOnlyList<GridPosition> path = state.Map.FindLowestCostPath(hero.Position, cell,
                            state.Map.Width * state.Map.Height * 4,
                            position => CombatMovementQuery.EntryCost(state, hero, position),
                            position => state.IsOccupied(position, hero.Id));
                        if (path.Count < 2) continue;
                        int cost = path.Skip(1).Sum(position => CombatMovementQuery.EntryCost(state, hero, position));
                        double risk = ThreatAt(state, hero, enemies, cell) *
                            (style == AutoCombatStyle.Cautious ? 4 : style == AutoCombatStyle.Balanced ? 2 : .5);
                        if (cost + risk < routeScore) { route = path; routeScore = cost + risk; }
                    }
            }
            if (route != null)
            {
                int budget = CombatMovementQuery.Budget(state, hero);
                int spent = 0;
                GridPosition destination = hero.Position;
                foreach (GridPosition cell in route.Skip(1))
                {
                    spent += CombatMovementQuery.EntryCost(state, hero, cell);
                    if (spent > budget) break;
                    destination = cell;
                    if (state.Map.GetTile(cell).IsBindingMark) break;
                }
                if (destination != hero.Position &&
                    string.IsNullOrEmpty(CombatMovementQuery.PlayerTargetFailure(state, destination)))
                    return CombatCommand.Move(hero.Id, destination);
            }
            return CombatCommand.EndTurn(hero.Id);
        }

        private static double ScoreOutcome(CombatState before, CombatState after, IReadOnlyList<UnitState> enemies,
            IReadOnlyList<GridPosition> objectiveCells, IReadOnlyList<GridPosition> investigationCells,
            AutoCombatStyle style)
        {
            UnitState hero = before.GetUnit("hero"), result = after.GetUnit("hero");
            if (result == null || !result.IsAlive || after.IsDefeat) return -10000;
            double damageWeight = style == AutoCombatStyle.Aggressive ? 12 : style == AutoCombatStyle.Cautious ? 8 : 10;
            double healthWeight = style == AutoCombatStyle.Aggressive ? 5 : style == AutoCombatStyle.Cautious ? 15 : 9;
            double shieldWeight = style == AutoCombatStyle.Aggressive ? 1.5 : style == AutoCombatStyle.Cautious ? 5 : 3;
            if (hero.Health * 2 < hero.MaxHealth && ThreatAt(before, hero, enemies, hero.Position) > 0)
                shieldWeight += hero.Health * 3 < hero.MaxHealth ? 8 : 4;
            double manaCostWeight = style == AutoCombatStyle.Aggressive ? 1 : style == AutoCombatStyle.Cautious ? 4 : 2.5;
            double score = 0;
            foreach (UnitState enemy in enemies)
            {
                UnitState changed = after.GetUnit(enemy.Id);
                if (changed == null) continue;
                score += Math.Max(0, enemy.Health + enemy.Shield - changed.Health - changed.Shield) * damageWeight;
                if (enemy.IsAlive && !changed.IsAlive) score += 22;
                foreach (StatusType status in HarmfulStatuses)
                    if (changed.StatusDuration(status) > enemy.StatusDuration(status)) score += 7;
            }
            score += objectiveCells.Sum(position => before.Map.GetTile(position).Durability -
                after.Map.GetTile(position).Durability) * 12;
            score += investigationCells.Count(position => !before.IsInvestigated(position) && after.IsInvestigated(position)) * 25;
            score += (result.Health - hero.Health) * healthWeight;
            score += (result.Shield - hero.Shield) * shieldWeight;
            score += Math.Max(0, result.Mana - hero.Mana) * (hero.Mana < hero.MaxMana / 2 ? 4 : 1);
            score -= Math.Max(0, hero.Mana - result.Mana) * manaCostWeight;
            score += Math.Max(0, result.ActionPoints - hero.ActionPoints) *
                (hero.ActionPoints == 0 && enemies.Count > 0 ? 48 : 8);
            score -= Math.Max(0, hero.ActionPoints - result.ActionPoints) * 2;
            foreach (StatusType status in HelpfulStatuses)
                if (result.StatusDuration(status) > hero.StatusDuration(status)) score += 6;
            foreach (StatusType status in HarmfulStatuses)
                if (result.StatusDuration(status) < hero.StatusDuration(status)) score += 8;
            if (result.Position != hero.Position)
            {
                int beforeThreat = ThreatAt(before, hero, enemies, hero.Position);
                int afterThreat = ThreatAt(after, result,
                    enemies.Select(enemy => after.GetUnit(enemy.Id)).Where(enemy => enemy?.IsAlive == true), result.Position);
                score += (beforeThreat - afterThreat) *
                    (style == AutoCombatStyle.Cautious ? 12 : style == AutoCombatStyle.Balanced ? 6 : 2);
            }
            if (after.IsVictory) score += 1000;
            return score;
        }

        private static bool UnsafeOutcome(CombatState before, CombatState after)
        {
            if (after.GetUnit("hero")?.IsAlive != true || after.IsDefeat) return true;
            return before.LootSource != null && !before.LootSource.IsDestroyed &&
                after.LootSource?.IsDestroyed == true;
        }

        private static int ItemChargeCost(AutoCombatStyle style) =>
            style == AutoCombatStyle.Aggressive ? 20 : style == AutoCombatStyle.Cautious ? 10 : 15;

        private static IEnumerable<ArtifactTarget> ArtifactTargets(CombatState state, UnitState hero,
            IEnumerable<UnitState> enemies, ArtifactDefinition artifact)
        {
            if (artifact.TargetRule == ArtifactTargetRule.Self || artifact.TargetRule == ArtifactTargetRule.AllyOrSelf)
            {
                yield return ArtifactTarget.Unit(hero.Id, hero.Position);
                yield break;
            }
            if (artifact.TargetRule == ArtifactTargetRule.AnyUnit)
                yield return ArtifactTarget.Unit(hero.Id, hero.Position);
            if (artifact.TargetRule == ArtifactTargetRule.Enemy || artifact.TargetRule == ArtifactTargetRule.AnyUnit)
            {
                foreach (UnitState enemy in enemies.Where(enemy => !state.LampVines.IsHiddenFrom(state, hero, enemy)))
                    yield return ArtifactTarget.Unit(enemy.Id, enemy.Position);
                yield break;
            }
            if (artifact.TargetRule == ArtifactTargetRule.TwoAllies) yield break;
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    if (hero.Position.ManhattanDistance(cell) > artifact.Range) continue;
                    TileState tile = state.Map.GetTile(cell);
                    if (artifact.TargetRule == ArtifactTargetRule.Device && !tile.IsDevice) continue;
                    if (artifact.TargetRule == ArtifactTargetRule.Destructible &&
                        tile.Cover == CoverType.None && !tile.IsDevice && !tile.IsObjective && !tile.IsDestructibleLoot)
                        continue;
                    UnitState occupant = state.Units.Values.FirstOrDefault(unit => unit.IsAlive && unit.Position == cell);
                    if (state.LampVines.IsHiddenFrom(state, hero, occupant)) occupant = null;
                    yield return occupant == null ? ArtifactTarget.At(cell) : ArtifactTarget.Unit(occupant.Id, cell);
                }
        }

        private static int ThreatAt(CombatState state, UnitState hero, IEnumerable<UnitState> enemies, GridPosition cell)
        {
            int threat = 0;
            foreach (UnitState enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || state.LampVines.IsHiddenFrom(state, hero, enemy)) continue;
                IAttackDefinition attack = enemy.BasicAttack;
                int distance = enemy.Position.ManhattanDistance(cell);
                if (attack != null && distance >= attack.MinimumRange &&
                    distance <= enemy.EffectiveRange(attack.Range) && state.HasLineOfSight(enemy.Position, cell))
                    threat++;
            }
            return threat;
        }

        private static CardinalDirection Direction(GridPosition from, GridPosition to) =>
            Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y)
                ? (to.X >= from.X ? CardinalDirection.East : CardinalDirection.West)
                : (to.Y >= from.Y ? CardinalDirection.North : CardinalDirection.South);
    }
}
