using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    /// <summary>
    /// Runs authored arena routes through the same command and per-unit lifecycle used by
    /// CombatPrototypeBootstrap.  Scenario tests should describe decisions; this fixture owns
    /// setup, artifact lifetime, enemy execution and diagnostic tracing.
    /// </summary>
    internal sealed class CombatScenarioRouteHarness
    {
        private readonly CombatCommandExecutionService commands = new CombatCommandExecutionService();
        private readonly EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
        private int artifactLifecycleTurn = -1;

        public CombatTestArenaScenario Scenario { get; }
        public CombatState State { get; }
        public ArtifactBattleState Artifacts { get; }
        public UnitState Hero => State.GetUnit("hero");
        public List<string> Trace { get; } = new List<string>();

        private CombatScenarioRouteHarness(CombatTestArenaScenario scenario)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            State = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            Artifacts = new ArtifactBattleState(State);
            CombatResolver.AdvanceToNextTurn(State);
            SyncArtifactLifecycle();
            Record("start");
        }

        public static CombatScenarioRouteHarness Start(string scenarioId) =>
            new CombatScenarioRouteHarness(CombatTestArenaScenarioCatalog.Get(scenarioId));

        public UnitState Enemy(string archetypeId) => State.Units.Values.Single(unit =>
            unit.IsAlive && string.Equals(unit.EnemyArchetypeId, archetypeId, StringComparison.Ordinal));

        public CombatCommandExecutionResult Move(GridPosition destination, string label = "move") =>
            Execute(CombatCommand.Move(Hero.Id, destination), label);

        public CombatCommandExecutionResult Spell(int slot, string targetUnitId, string label = "spell") =>
            Execute(CombatCommand.UseSkill(Hero.Id, slot, targetUnitId), label + "[" + slot + "]");

        public CombatCommandExecutionResult SpellAt(int slot, GridPosition cell, CardinalDirection direction,
            string label = "spell-at") => Execute(CombatCommand.UseSkillAt(Hero.Id, slot, cell, direction),
            label + "[" + slot + "]");

        public CombatCommandExecutionResult Weapon(string targetUnitId, string label = "weapon") =>
            Execute(CombatCommand.Attack(Hero.Id, targetUnitId), label);

        public ArtifactExecution Artifact(int slot, ArtifactTarget target, string label = "artifact")
        {
            RequireHeroTurn(label);
            string[] quickbar = State.RogueEquipment.ItemQuickbarInstanceIds;
            if (slot < 0 || slot >= quickbar.Length) throw Failure(label, "invalid artifact slot " + slot);
            RogueTacticalItemInstance item = State.RogueEquipment.TacticalItem(quickbar[slot]);
            if (item == null) throw Failure(label, "artifact slot is empty");
            ArtifactDefinition definition = ArtifactCatalog.Get(item.DefinitionId);
            ArtifactPreview preview = ArtifactEngine.Preview(Artifacts, Hero.Id, definition, target, item.ChargesCurrent);
            if (!preview.CanCommit) throw Failure(label, string.Join("; ", preview.Failures));
            ArtifactExecution result = ArtifactEngine.Execute(Artifacts, Hero.Id, definition, target, item.ChargesCurrent);
            if (!item.Consume()) throw Failure(label, "charge consumption failed");
            plans.Invalidate();
            Record(label + " " + definition.Id);
            return result;
        }

        public void EndHeroTurnAndAdvance(string label = "end hero turn")
        {
            EndHeroTurn(label);
            AdvanceEnemiesToHero();
        }

        public void EndHeroTurn(string label = "end hero turn")
        {
            RequireHeroTurn(label);
            EndActiveTurn(label);
        }

        public CombatCommandExecutionResult ExecuteActiveEnemyAction(string label = "enemy action")
        {
            if (State.ActiveUnitId == Hero.Id) throw Failure(label, "active actor is the hero");
            UnitState enemy = State.GetUnit(State.ActiveUnitId);
            if (enemy == null || !enemy.IsAlive) throw Failure(label, "active enemy is missing or defeated");
            return Execute(plans.GetExecutionCommand(State, enemy, Hero), label + " " + enemy.EnemyArchetypeId);
        }

        public void EndActiveEnemyTurn(string label = "enemy end")
        {
            if (State.ActiveUnitId == Hero.Id) throw Failure(label, "active actor is the hero");
            EndActiveTurn(label);
        }

        public void AdvanceEnemiesToHero()
        {
            while (!State.IsVictory && !State.IsDefeat && State.ActiveUnitId != Hero.Id)
            {
                UnitState enemy = State.GetUnit(State.ActiveUnitId);
                if (enemy == null || !enemy.IsAlive)
                {
                    CombatResolver.AdvanceToNextTurn(State);
                    SyncArtifactLifecycle();
                    continue;
                }
                CombatCommand intent = plans.GetExecutionCommand(State, enemy, Hero);
                Execute(intent, "enemy " + enemy.EnemyArchetypeId);
                if (!State.IsVictory && !State.IsDefeat && State.ActiveUnitId == enemy.Id)
                    EndActiveTurn("enemy end " + enemy.EnemyArchetypeId);
            }
            if (!State.IsVictory && !State.IsDefeat && State.ActiveUnitId != Hero.Id)
                throw Failure("advance", "did not return to hero");
        }

        public CombatScenarioSnapshot Capture() => CombatScenarioSnapshot.Capture(this);

        public string TraceSummary => string.Join("\n", Trace);

        private CombatCommandExecutionResult Execute(CombatCommand command, string label)
        {
            CombatCommandExecutionResult result = commands.Execute(State, State.RogueSpells.FireBattle, command);
            if (!result.Accepted) throw Failure(label, result.RejectionReason);
            plans.Invalidate();
            Record(label + " " + command.Type);
            return result;
        }

        private void EndActiveTurn(string label)
        {
            UnitState actor = State.GetUnit(State.ActiveUnitId);
            if (actor == null) throw Failure(label, "active actor is missing");
            CombatResolver.EndTurn(State, actor);
            SyncArtifactLifecycle();
            plans.Invalidate();
            Record(label);
        }

        private void SyncArtifactLifecycle()
        {
            if (State.IsVictory || State.IsDefeat || string.IsNullOrEmpty(State.ActiveUnitId) ||
                artifactLifecycleTurn == State.TurnSequence) return;
            Artifacts.BeginUnitTurn(State.ActiveUnitId);
            artifactLifecycleTurn = State.TurnSequence;
        }

        private void RequireHeroTurn(string label)
        {
            if (State.ActiveUnitId != Hero.Id) throw Failure(label, "hero is not active");
        }

        private InvalidOperationException Failure(string label, string reason) => new InvalidOperationException(
            Scenario.Id + " | " + label + " | " + reason + "\n" + TraceSummary);

        private void Record(string action)
        {
            string actor = string.IsNullOrEmpty(State.ActiveUnitId) ? "-" : State.ActiveUnitId;
            string units = string.Join(", ", State.Units.Values.OrderBy(unit => unit.Id).Select(unit =>
                unit.Id + "@" + unit.Position + " hp" + unit.Health + " sh" + unit.Shield));
            Trace.Add("T" + State.TurnSequence + " " + action + " -> " + actor +
                " | hero ap" + Hero.ActionPoints + " mp" + Hero.Mana + " | " + units);
        }

        internal EnemyTurnPlanBook Plans => plans;
    }

    internal sealed class CombatScenarioSnapshot
    {
        public string ScenarioId { get; }
        public IReadOnlyList<string> Units { get; }
        public IReadOnlyList<string> Timeline { get; }
        public IReadOnlyList<string> Intents { get; }
        public IReadOnlyList<string> Terrain { get; }
        public IReadOnlyList<string> ReachableCells { get; }
        public IReadOnlyList<string> LineOfSight { get; }
        public IReadOnlyList<string> Spells { get; }
        public IReadOnlyList<string> Artifacts { get; }

        private CombatScenarioSnapshot(string scenarioId, IEnumerable<string> units,
            IEnumerable<string> timeline, IEnumerable<string> intents, IEnumerable<string> terrain, IEnumerable<string> reachableCells,
            IEnumerable<string> lineOfSight, IEnumerable<string> spells, IEnumerable<string> artifacts)
        {
            ScenarioId = scenarioId;
            Units = units.ToArray(); Timeline = timeline.ToArray(); Intents = intents.ToArray(); Terrain = terrain.ToArray();
            ReachableCells = reachableCells.ToArray(); LineOfSight = lineOfSight.ToArray();
            Spells = spells.ToArray(); Artifacts = artifacts.ToArray();
        }

        public static CombatScenarioSnapshot Capture(CombatScenarioRouteHarness route)
        {
            CombatState state = route.State;
            UnitState hero = route.Hero;
            string[] units = state.Units.Values.OrderBy(unit => unit.Id).Select(unit =>
                unit.Id + "/" + (unit.IsHero ? "hero" : unit.EnemyArchetypeId) +
                " pos=" + unit.Position + " hp=" + unit.Health + "/" + unit.MaxHealth +
                " shield=" + unit.Shield + "/" + unit.MaxShield + " armor=" + unit.Armor +
                " block=" + unit.Block + " speed=" + unit.Speed + " av=" + unit.ActionValue +
                " weapon=" + (unit.MainHand == null ? "-" : unit.MainHand.Id + "[" +
                    unit.MainHand.MinimumRange + "-" + unit.MainHand.Range + "]")).ToArray();
            string[] timeline = CombatActionTimeline.Order(state, state.Units.Values)
                .Select((unit, index) => (index + 1) + ":" + unit.Id + " av=" + unit.ActionValue +
                    " speed=" + unit.EffectiveSpeed).ToArray();
            string[] intents = state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive)
                .OrderBy(unit => unit.Id).Select(unit =>
                {
                    EnemyIntentPresentation visible = route.Plans.GetPublicIntent(state, unit, hero);
                    CombatCommand command = route.Plans.GetExecutionCommand(state, unit, hero);
                    return unit.Id + " " + visible.ActionName + " -> " + visible.TargetSummary +
                        " | " + visible.ResultSummary + " | cmd=" + command.Type +
                        " target=" + (command.TargetUnitId ?? "-") + " cell=" + command.Destination;
                }).ToArray();
            string[] terrain = AllCells(state.Map).Select(cell => new { Cell = cell, Tile = state.Map.GetTile(cell) })
                .Where(value => value.Tile.Cover != CoverType.None || value.Tile.Durability > 0 ||
                    value.Tile.IsWater || value.Tile.IsLampVine || value.Tile.IsAetherCrystal ||
                    value.Tile.IsCrystalShard || value.Tile.IsScorched || value.Tile.SmokeExpiresAt > 0)
                .Select(value => value.Cell + " cover=" + value.Tile.Cover + " durability=" + value.Tile.Durability +
                    " water=" + value.Tile.IsWater + " vine=" + value.Tile.IsLampVine +
                    " crystal=" + value.Tile.IsAetherCrystal + " shard=" + value.Tile.IsCrystalShard +
                    " scorched=" + value.Tile.IsScorched + " smokeUntil=" + value.Tile.SmokeExpiresAt).ToArray();
            string[] reachable = AllCells(state.Map).Where(cell =>
                string.IsNullOrEmpty(CombatMovementQuery.PlayerTargetFailure(state, cell)))
                .Select(cell => cell + "(enter=" + CombatMovementQuery.EntryCost(state, hero, cell) + ")").ToArray();
            string[] sight = state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive).OrderBy(unit => unit.Id)
                .Select(unit => unit.Id + " distance=" + hero.Position.ManhattanDistance(unit.Position) +
                    " los=" + state.HasLineOfSight(hero.Position, unit.Position)).ToArray();
            string[] spells = Enumerable.Range(0, RogueRuntimeConstants.SpellSlotCount).Select(slot =>
            {
                SpellDefinition spell = state.RogueSpells.DefinitionAtSlot(slot);
                string legal = string.Join(",", LegalSpellTargets(route, slot).Take(12));
                return slot + ":" + spell.DefinitionId + "/" + spell.DisplayName +
                    " ap=" + spell.ActionPointCost + " mp=" + spell.ManaCost +
                    " cd=" + spell.CooldownOwnTurns + " range=" + spell.Range +
                    " targeting=" + spell.Targeting + " legal=" + (legal.Length == 0 ? "none" : legal);
            }).ToArray();
            string[] artifacts = Enumerable.Range(0, RogueRuntimeConstants.ItemQuickbarSize).Select(slot =>
            {
                RogueTacticalItemInstance item = state.RogueEquipment.TacticalItem(
                    state.RogueEquipment.ItemQuickbarInstanceIds[slot]);
                ArtifactDefinition definition = ArtifactCatalog.Get(item.DefinitionId);
                string legal = string.Join(",", LegalArtifactTargets(route, definition, item.ChargesCurrent).Take(12));
                return slot + ":" + definition.Id + "/" + definition.DisplayName +
                    " ap=" + definition.ActionPointCost + " mp=" + definition.ManaCost +
                    " uses=" + item.ChargesCurrent + "/" + item.ChargesMaximum +
                    " range=" + definition.Range + " target=" + definition.TargetRule +
                    " legal=" + (legal.Length == 0 ? "none" : legal);
            }).ToArray();
            return new CombatScenarioSnapshot(route.Scenario.Id, units, timeline, intents, terrain, reachable, sight, spells, artifacts);
        }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("SCENARIO " + ScenarioId);
            Append(text, "UNITS", Units); Append(text, "TIMELINE", Timeline); Append(text, "INTENTS", Intents);
            Append(text, "TERRAIN", Terrain);
            Append(text, "REACHABLE", ReachableCells); Append(text, "LOS", LineOfSight);
            Append(text, "SPELLS", Spells); Append(text, "ARTIFACTS", Artifacts);
            return text.ToString().TrimEnd();
        }

        private static IEnumerable<string> LegalSpellTargets(CombatScenarioRouteHarness route, int slot)
        {
            CombatState state = route.State;
            SpellDefinition spell = state.RogueSpells.DefinitionAtSlot(slot);
            if (spell.Role == "passive") yield break;
            if (spell.IsBasic)
            {
                if (spell.DefinitionId == "BASE-AETHER-SHIELD" || spell.DefinitionId == "BASE-MANA-RECOVER")
                { yield return "self"; yield break; }
                foreach (UnitState enemy in state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive))
                    if (route.Hero.Position.ManhattanDistance(enemy.Position) <= spell.Range &&
                        (spell.Range <= 1 || state.HasLineOfSight(route.Hero.Position, enemy.Position)))
                        yield return enemy.Id;
                yield break;
            }
            FireSpellDefinition definition = FireSpellCatalog.Get(spell.DefinitionId);
            HashSet<string> legal = new HashSet<string>(StringComparer.Ordinal);
            CardinalDirection[] directions = { CardinalDirection.North, CardinalDirection.East, CardinalDirection.South, CardinalDirection.West };
            foreach (UnitState unit in state.Units.Values.Where(unit => unit.IsAlive))
                foreach (CardinalDirection direction in directions)
                    if (FireSpellEngine.Preview(state.RogueSpells.FireBattle, route.Hero.Id, definition,
                        FireSpellTarget.Unit(unit.Id, direction)).CanCommit) legal.Add(unit.Id + ":" + direction);
            foreach (GridPosition cell in AllCells(state.Map))
                foreach (CardinalDirection direction in directions)
                    if (FireSpellEngine.Preview(state.RogueSpells.FireBattle, route.Hero.Id, definition,
                        FireSpellTarget.At(cell, direction)).CanCommit) legal.Add(cell + ":" + direction);
            foreach (string value in legal.OrderBy(value => value, StringComparer.Ordinal)) yield return value;
        }

        private static IEnumerable<string> LegalArtifactTargets(CombatScenarioRouteHarness route,
            ArtifactDefinition definition, int uses)
        {
            CombatState state = route.State;
            HashSet<string> legal = new HashSet<string>(StringComparer.Ordinal);
            foreach (GridPosition cell in AllCells(state.Map))
            {
                if (ArtifactEngine.Preview(route.Artifacts, route.Hero.Id, definition,
                    ArtifactTarget.At(cell), uses).CanCommit) legal.Add(cell.ToString());
                foreach (UnitState unit in state.Units.Values.Where(unit => unit.IsAlive))
                    if (ArtifactEngine.Preview(route.Artifacts, route.Hero.Id, definition,
                        ArtifactTarget.Unit(unit.Id, unit.Position), uses).CanCommit) legal.Add(unit.Id);
            }
            UnitState[] allies = state.Units.Values.Where(unit => unit.IsAlive && unit.IsHero == route.Hero.IsHero).ToArray();
            foreach (UnitState first in allies)
                foreach (UnitState second in allies.Where(unit => unit.Id != first.Id))
                    if (ArtifactEngine.Preview(route.Artifacts, route.Hero.Id, definition,
                        ArtifactTarget.Pair(first.Id, second.Id, first.Position), uses).CanCommit)
                        legal.Add(first.Id + "+" + second.Id);
            return legal.OrderBy(value => value, StringComparer.Ordinal);
        }

        private static IEnumerable<GridPosition> AllCells(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    yield return new GridPosition(x, y);
        }

        private static void Append(StringBuilder target, string title, IEnumerable<string> rows)
        {
            target.AppendLine(title);
            foreach (string row in rows) target.AppendLine("  " + row);
        }
    }
}
