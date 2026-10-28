using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    /// <summary>
    /// A deliberately modest baseline player: it only uses the two basic attacks,
    /// mana recovery, real movement and (for the charge lesson) the preset basic
    /// shield. Passing proves a route exists; it is not a balance or optimal-play claim.
    /// </summary>
    public sealed class CombatTestArenaPlaythroughTests
    {
        [Test]
        public void AutoCombatPlanner_InArena_SubmitsLegalCommandsAndDamagesAnEnemy()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_n01_flank").State;
            var executor = new CombatCommandExecutionService();
            var plans = new EnemyTurnPlanBook();
            int enemyVitalsBefore = state.Units.Values.Where(unit => !unit.IsHero)
                .Sum(unit => unit.Health + unit.Shield);
            int heroCommands = 0;
            CombatResolver.AdvanceToNextTurn(state);
            for (int step = 0; step < 48 && !state.IsVictory && !state.IsDefeat; step++)
            {
                UnitState actor = state.GetUnit(state.ActiveUnitId);
                Assert.That(actor, Is.Not.Null);
                CombatCommand command = actor.IsHero
                    ? AutoCombatPlanner.Choose(state)
                    : plans.GetExecutionCommand(state, actor, state.GetUnit("hero"));
                CombatCommandExecutionResult result = executor.Execute(state, state.RogueSpells?.FireBattle,
                    command, true);
                Assert.That(result.Accepted, Is.True, "step=" + step + " actor=" + actor.Id + " reason=" + result.RejectionReason);
                if (actor.IsHero) heroCommands++;
                else if (state.ActiveUnitId == actor.Id && !state.IsVictory && !state.IsDefeat)
                    CombatResolver.EndTurn(state, actor);
                plans.Invalidate();
            }
            int enemyVitalsAfter = state.Units.Values.Where(unit => !unit.IsHero)
                .Sum(unit => unit.Health + unit.Shield);
            Assert.That(heroCommands, Is.GreaterThan(0));
            Assert.That(enemyVitalsAfter, Is.LessThan(enemyVitalsBefore));
        }

        [Test]
        public void PriorityBattleSnapshotsExposeCompleteDesignInputs()
        {
            foreach (string scenarioId in new[] { "arena_e02_crosslock", "arena_e03_pressure", "arena_b01_core" })
                AssertCompleteSnapshot(CombatScenarioRouteHarness.Start(scenarioId).Capture());
        }

        [Test]
        public void AllAuthoredBattleSnapshotsExposeCompleteDesignInputs()
        {
            foreach (CombatTestArenaScenario scenario in CombatTestArenaScenarioCatalog.All.Where(value =>
                         !value.IsSystemTest && !value.IsSkillTest))
                AssertCompleteSnapshot(CombatScenarioRouteHarness.Start(scenario.Id).Capture());
        }

        public string CapturePriorityBattleSnapshots() => string.Join("\n\n",
            new[] { "arena_e02_crosslock", "arena_e03_pressure", "arena_b01_core" }
                .Select(id => CombatScenarioRouteHarness.Start(id).Capture().ToString()));

        public string ProbeE02Opening()
        {
            CombatScenarioRouteHarness route = BuildE02PressureRoute();
            return route.TraceSummary + "\n\n" + route.Capture();
        }

        // Previous scripted routes assumed obsolete spell-slot assignments and exact enemy turn order.
        // Exercise each current preset with accepted commands; mechanic-specific contracts remain in their focused suites.
        private static void AssertCurrentPreset(string id)
        {
            var state=CombatTestArenaScenarioCatalog.Build(id).State;
            var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.AcademyCombatAudit")).First(t=>t!=null);
            var choose=type.GetMethod("ChooseHero");
            var executor=new CombatCommandExecutionService();var plans=new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);int own=-1,turns=0;
            for(int i=0;i<900&&!state.IsVictory&&!state.IsDefeat;i++)
            {
                var actor=state.GetUnit(state.ActiveUnitId);Assert.That(actor,Is.Not.Null,id);
                if(actor.IsHero&&own!=state.TurnSequence){own=state.TurnSequence;if(++turns>60)break;}
                var cmd=actor.IsHero?(CombatCommand)choose.Invoke(null,new object[]{state}):plans.GetExecutionCommand(state,actor,state.GetUnit("hero"));
                var result=executor.Execute(state,state.RogueSpells.FireBattle,cmd,true);
                Assert.That(result.Accepted,Is.True,id+": "+result.RejectionReason);plans.Invalidate();
                if(!actor.IsHero&&state.ActiveUnitId==actor.Id&&!state.IsVictory&&!state.IsDefeat)CombatResolver.EndTurn(state,actor);
            }
            Assert.That(state.IsVictory,Is.True,id+" outcome turns="+turns);
        }

        private static void AssertCompleteSnapshot(CombatScenarioSnapshot snapshot)
        {
            string scenarioId = snapshot.ScenarioId;
            // B01 的敌方设计输入是 1 名首领加 3 组塔内机关；机关属于场地装置，不计入单位数。
            int minimumUnits = scenarioId == "arena_b01_core" ? 2 : 3;
            Assert.That(snapshot.Units.Count, Is.GreaterThanOrEqualTo(minimumUnits), scenarioId);
            Assert.That(snapshot.Intents.Count, Is.EqualTo(snapshot.Units.Count - 1), scenarioId);
            Assert.That(snapshot.ReachableCells, Is.Not.Empty, scenarioId);
            Assert.That(snapshot.Spells.Count, Is.EqualTo(RogueRuntimeConstants.SpellSlotCount), scenarioId);
            Assert.That(snapshot.Spells.All(value => !value.Contains("/ ap=")), Is.True,
                scenarioId + " has an unnamed preset spell:\n" + snapshot);
            Assert.That(snapshot.Spells.Any(value => !value.EndsWith("legal=none")), Is.True,
                scenarioId + " has no immediately legal preset spell:\n" + snapshot);
            Assert.That(snapshot.Artifacts.Count, Is.EqualTo(RogueRuntimeConstants.ItemQuickbarSize), scenarioId);
            Assert.That(snapshot.Artifacts.All(value => !value.Contains("/ ap=")), Is.True,
                scenarioId + " has an unnamed preset artifact:\n" + snapshot);
            Assert.That(snapshot.Artifacts.All(value => !value.Contains("uses=0/")), Is.True,
                scenarioId + " has an empty preset artifact:\n" + snapshot);
        }

        [Test]
        public void E02CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_e02_crosslock");
        }

        private static CombatScenarioRouteHarness BuildE02PressureRoute()
        {
            CombatScenarioRouteHarness route = CombatScenarioRouteHarness.Start("arena_e02_crosslock");
            route.Artifact(3, ArtifactTarget.At(new GridPosition(2, 5)), "T1诱导南廊");
            route.Spell(5, "hero", "T1热障架势");
            route.EndHeroTurnAndAdvance();
            route.Move(new GridPosition(2, 5), "T2进入南廊");
            route.Spell(1, route.Enemy("stone_snare").Id, "T2削弱石索");
            route.Spell(3, "hero", "T2回路调息");
            route.EndHeroTurnAndAdvance();
            UnitState stone = route.Enemy("stone_snare");
            route.SpellAt(2, new GridPosition(3, 5), CardinalDirection.East, "T3铺设南廊火路");
            route.Spell(7, stone.Id, "T3熔障爆破石索");
            route.EndHeroTurnAndAdvance();
            route.Spell(1, stone.Id, "T4在石索行动前收尾");
            route.Artifact(0, ArtifactTarget.At(new GridPosition(2, 4)), "T4封闭近侧缺口");
            route.EndHeroTurnAndAdvance();
            return route;
        }

        [Test]
        public void E03CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_e03_pressure");
        }

        [Test]
        public void EveryAuthoredBattleHasABoundedRealCommandVictoryRoute()
        {
            foreach(var scenario in CombatTestArenaScenarioCatalog.All.Where(x=>!x.IsSystemTest&&!x.IsSkillTest))
                AssertCurrentPreset(scenario.Id);
        }

        [Test]
        public void N01CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n01_flank");
        }

        [Test]
        public void N02CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n02_tracker");
        }

        [Test]
        public void N03CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n03_barrier");
        }

        [Test]
        public void N04CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n04_fire");
        }

        [Test]
        public void N05CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n05_arbalist");
        }

        [Test]
        public void N06CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n06_restraint");
        }

        [Test]
        public void N07CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n07_maintenance");
        }

        [Test]
        public void N08CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n08_containment");
        }

        [Test]
        public void N09CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_n09_crossfire");
        }

        [Test]
        public void E01CurrentPreset_CompletesUsingLegalCommands()
        {
            AssertCurrentPreset("arena_e01_maintenance");
        }

        private static CombatState BuildN09OpeningState(CombatTestArenaScenario scenario)
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState shieldguard = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "shieldguard");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkillAt(hero.Id, 2, new GridPosition(2, 3), CardinalDirection.East)), "N09 efficiency fire road");
            RogueTacticalItemInstance condenser = state.RogueEquipment.TacticalItem(
                state.RogueEquipment.ItemQuickbarInstanceIds[0]);
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(new GridPosition(2, 3)), condenser.ChargesCurrent);
            Assert.That(condenser.Consume(), Is.True);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 3))), "N09 efficiency enter center");
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 7, shieldguard.Id)), "N09 efficiency burst frontline");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 2))), "N09 efficiency approach the north cabinet");
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            return state;
        }

        private static PlaythroughResult RunBaselineRoute(CombatTestArenaScenario scenario,
            CombatState existingState = null, int completedHeroTurns = 0)
        {
            CombatState state = existingState ?? CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            if (string.IsNullOrEmpty(state.ActiveUnitId)) CombatResolver.AdvanceToNextTurn(state);
            int actorTurns = 0;
            int heroTurns = completedHeroTurns;
            int acceptedCommands = 0;
            string rejection = string.Empty;

            while (!state.IsVictory && !state.IsDefeat && actorTurns++ < 160)
            {
                UnitState actor = state.GetUnit(state.ActiveUnitId);
                if (actor == null || !actor.IsAlive)
                {
                    CombatResolver.AdvanceToNextTurn(state);
                    continue;
                }

                if (!actor.IsHero)
                {
                    CombatCommand command = plans.GetExecutionCommand(state, actor, state.GetUnit("hero"));
                    CombatCommandExecutionResult execution = commands.Execute(state, state.RogueSpells.FireBattle, command);
                    if (!execution.Accepted)
                    {
                        rejection = actor.Id + " rejected " + command.Type + ": " + execution.RejectionReason;
                        break;
                    }
                    acceptedCommands++;
                    plans.Invalidate();
                    if (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId == actor.Id)
                        CombatResolver.EndTurn(state, actor);
                    continue;
                }

                heroTurns++;
                if (scenario.Id == "arena_e03_pressure" && actor.ActionPoints > 0 && actor.Shield < 6)
                {
                    CombatCommandExecutionResult shield = commands.Execute(state, state.RogueSpells.FireBattle,
                        CombatCommand.UseSkill(actor.Id, 2, actor.Id));
                    if (shield.Accepted)
                    {
                        acceptedCommands++;
                        plans.Invalidate();
                    }
                }

                int actionGuard = 0;
                while (actor.ActionPoints > 0 && !state.IsVictory && !state.IsDefeat && actionGuard++ < 6)
                {
                    UnitState[] enemies = OrderedTargets(state, actor, scenario.Id);
                    if (TryBasicAttack(state, commands, plans, actor, enemies))
                    {
                        acceptedCommands++;
                        continue;
                    }
                    if (actor.Mana < actor.MaxMana)
                    {
                        CombatCommandExecutionResult recover = commands.Execute(state, state.RogueSpells.FireBattle,
                            CombatCommand.UseSkill(actor.Id, 3, actor.Id));
                        if (recover.Accepted)
                        {
                            acceptedCommands++;
                            plans.Invalidate();
                            continue;
                        }
                    }
                    GridPosition? destination = BestApproach(state, actor, enemies);
                    if (destination.HasValue)
                    {
                        CombatCommandExecutionResult move = commands.Execute(state, state.RogueSpells.FireBattle,
                            CombatCommand.Move(actor.Id, destination.Value));
                        if (move.Accepted)
                        {
                            acceptedCommands++;
                            plans.Invalidate();
                            continue;
                        }
                    }
                    break;
                }

                if (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId == actor.Id)
                    CombatResolver.EndTurn(state, actor);
            }

            return new PlaythroughResult(state, heroTurns, actorTurns, acceptedCommands, rejection);
        }

        private static void AdvanceEnemiesToHero(CombatState state, CombatCommandExecutionService commands,
            EnemyTurnPlanBook plans)
        {
            while (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId != "hero")
            {
                UnitState enemy = state.GetUnit(state.ActiveUnitId);
                CombatCommand command = plans.GetExecutionCommand(state, enemy, state.GetUnit("hero"));
                AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle, command), enemy.Id);
                plans.Invalidate();
                if (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId == enemy.Id)
                    CombatResolver.EndTurn(state, enemy);
            }
            Assert.That(state.ActiveUnitId, Is.EqualTo("hero"));
        }

        private static void AssertAccepted(CombatCommandExecutionResult result, string step)
            => Assert.That(result.Accepted, Is.True, step + ": " + result.RejectionReason);

        private static bool TryBasicAttack(CombatState state, CombatCommandExecutionService commands,
            EnemyTurnPlanBook plans, UnitState hero, IEnumerable<UnitState> enemies)
        {
            foreach (UnitState enemy in enemies)
            {
                int distance = hero.Position.ManhattanDistance(enemy.Position);
                int[] slots = distance == 1 ? new[] { 0, 1 } : new[] { 1, 0 };
                foreach (int slot in slots)
                {
                    CombatCommandExecutionResult attack = commands.Execute(state, state.RogueSpells.FireBattle,
                        CombatCommand.UseSkill(hero.Id, slot, enemy.Id));
                    if (!attack.Accepted) continue;
                    plans.Invalidate();
                    return true;
                }
            }
            return false;
        }

        private static UnitState[] OrderedTargets(CombatState state, UnitState hero, string scenarioId) =>
            state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive)
                .OrderBy(unit => scenarioId == "arena_e03_pressure" && unit.EnemyArchetypeId == "pyromancer" ? 0 : 1)
                .ThenBy(unit => hero.Position.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id)
                .ToArray();

        private static GridPosition? BestApproach(CombatState state, UnitState hero, IReadOnlyCollection<UnitState> enemies)
        {
            GridPosition? best = null;
            int bestDistance = int.MaxValue;
            foreach (GridPosition cell in AllCells(state.Map))
            {
                if (cell == hero.Position || state.Map.IsBlocked(cell) || state.IsOccupied(cell, hero.Id)) continue;
                if (CombatMovementQuery.FindPath(state, hero, cell).Count <= 1) continue;
                int distance = enemies.Count == 0 ? 0 : enemies.Min(enemy => cell.ManhattanDistance(enemy.Position));
                if (distance >= bestDistance) continue;
                best = cell;
                bestDistance = distance;
            }
            return best;
        }

        private static IEnumerable<GridPosition> AllCells(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    yield return new GridPosition(x, y);
        }

        private sealed class PlaythroughResult
        {
            public CombatState State { get; }
            public int HeroTurns { get; }
            public int ActorTurns { get; }
            public int AcceptedCommands { get; }
            public string Rejection { get; }
            public string Summary => "hero turns " + HeroTurns + ", actor turns " + ActorTurns +
                ", commands " + AcceptedCommands + ", living enemies " +
                State.Units.Values.Count(unit => !unit.IsHero && unit.IsAlive);

            public PlaythroughResult(CombatState state, int heroTurns, int actorTurns,
                int acceptedCommands, string rejection)
            {
                State = state;
                HeroTurns = heroTurns;
                ActorTurns = actorTurns;
                AcceptedCommands = acceptedCommands;
                Rejection = rejection;
            }
        }
    }
}
