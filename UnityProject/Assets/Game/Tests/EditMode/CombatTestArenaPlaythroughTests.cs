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
        public void E02PresetRouteTurnsRecurringCoverShieldAndTheSouthFirelineAgainstTheCrosslock()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_e02_crosslock");
            CombatScenarioRouteHarness route = BuildE02PressureRoute();
            UnitState stone = route.State.Units.Values.Single(unit => unit.EnemyArchetypeId == "stone_snare");
            UnitState vanguard = route.Enemy("elite_vanguard");

            Assert.That(stone.IsAlive, Is.False,
                "The controller must be removed before its published Stone Snare decision.");
            Assert.That(vanguard.Position, Is.EqualTo(new GridPosition(3, 5)));
            Assert.That(vanguard.Health, Is.EqualTo(18),
                "Closing C5 must reroute the vanguard through the player's D6 fire line.");
            Assert.That(route.State.Map.GetTile(new GridPosition(2, 4)).Cover, Is.EqualTo(CoverType.Heavy));
            Assert.That(route.Hero.IsAlive, Is.True);

            PlaythroughResult result = RunBaselineRoute(scenario, route.State, 4);
            Assert.That(result.Rejection, Is.Empty, result.Rejection + "\n" + route.TraceSummary);
            Assert.That(result.State.IsVictory, Is.True, result.Summary + "\n" + route.TraceSummary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(12), result.Summary);
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
        public void E03PresetRouteMakesThePublishedChargeOpenItsOwnCrystalBurstFinisher()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_e03_pressure");
            CombatScenarioRouteHarness route = CombatScenarioRouteHarness.Start(scenario.Id);
            UnitState ram = route.Enemy("breach_ram");
            GridPosition crystal = new GridPosition(6, 3);
            int ramHealthBeforeCharge = ram.Health;

            route.Move(new GridPosition(3, 3), "T1进入浅水沟");
            route.SpellAt(4, crystal, CardinalDirection.East, "T1熔障校准晶簇");
            route.Spell(3, "hero", "T1回收施术魔力");
            route.EndHeroTurnAndAdvance();
            Assert.That(route.State.Map.GetTile(crystal).IsAetherCrystal, Is.False,
                "Calibration and the published charge must each remove eight of the crystal's sixteen durability.");
            Assert.That(ram.Health, Is.LessThan(ramHealthBeforeCharge),
                "The charging ram must share the intuitive adjacent crystal burst risk.");
            Assert.That(ram.Shield, Is.Zero, "The dry charge ending must visibly vent the ram shield.");

            route.Artifact(0, ArtifactTarget.Unit(ram.Id, ram.Position), "T2拉近卸压楔角");
            Assert.That(ram.Position, Is.EqualTo(new GridPosition(5, 3)));
            route.SpellAt(5, new GridPosition(4, 3), CardinalDirection.East, "T2震步推回楔角");
            Assert.That(ram.Position, Is.EqualTo(new GridPosition(6, 3)));
            route.Artifact(0, ArtifactTarget.Unit(ram.Id, ram.Position), "T2再次拉回近战位");
            Assert.That(ram.Position, Is.EqualTo(new GridPosition(5, 3)));
            route.EndHeroTurnAndAdvance();
            Assert.That(route.Hero.IsAlive, Is.True);
            Assert.That(route.Hero.Position.ManhattanDistance(ram.Position), Is.EqualTo(1), route.TraceSummary);

            route.Spell(6, ram.Id, "T3烙印启动终结条件");
            route.Spell(7, ram.Id, "T3炉心穿刺终结");
            Assert.That(ram.IsAlive, Is.False, route.TraceSummary);

            PlaythroughResult result = RunBaselineRoute(scenario, route.State, 3);
            Assert.That(result.Rejection, Is.Empty, result.Rejection + "\n" + route.TraceSummary);
            Assert.That(result.State.IsVictory, Is.True, result.Summary + "\n" + route.TraceSummary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(12), result.Summary);
        }

        [Test]
        public void EveryAuthoredBattleHasABoundedRealCommandVictoryRoute()
        {
            CombatTestArenaScenario[] scenarios = CombatTestArenaScenarioCatalog.All.Where(scenario =>
                !scenario.IsSystemTest && !scenario.IsSkillTest).ToArray();
            Assert.That(scenarios.Length, Is.EqualTo(13));

            foreach (CombatTestArenaScenario scenario in scenarios)
            {
                PlaythroughResult result = RunBaselineRoute(scenario);
                Assert.That(result.Rejection, Is.Empty, scenario.Id + ": " + result.Rejection);
                Assert.That(result.State.IsVictory, Is.True,
                    scenario.Id + " did not reach victory through real combat commands; " + result.Summary);
                Assert.That(result.State.GetUnit("hero").IsAlive, Is.True, scenario.Id);
                int ceiling = scenario.Level.IsBoss ? 20 : scenario.Level.IsElite ? 12 : 8;
                Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(ceiling),
                    scenario.Id + " exceeded the broad anti-stall ceiling; " + result.Summary);
            }
        }

        [Test]
        public void N01PresetRoutePullsMarksAndRepositionsBeforeACompleteVictory()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n01_flank");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState shieldguard = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "shieldguard");

            string compassId = state.RogueEquipment.ItemQuickbarInstanceIds[0];
            RogueTacticalItemInstance compass = state.RogueEquipment.TacticalItem(compassId);
            Assert.That(compass.DefinitionId, Is.EqualTo("G-T09"));
            int chargesBefore = compass.ChargesCurrent;
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            ArtifactExecution pull = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(shieldguard.Id, shieldguard.Position), compass.ChargesCurrent);
            Assert.That(compass.Consume(), Is.True);
            Assert.That(pull.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveTarget && step.Applied > 0), Is.True);
            Assert.That(compass.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(shieldguard.Position, Is.EqualTo(new GridPosition(3, 3)));

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 3))), "N01 approach");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 2, hero.Id)), "N01 defensive setup");
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);

            Assert.That(hero.Position.ManhattanDistance(shieldguard.Position), Is.EqualTo(1));
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 4, shieldguard.Id)), "N01 fire seed");
            Assert.That(shieldguard.StatusDuration(StatusType.Burning), Is.EqualTo(2));
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 5, shieldguard.Id)), "N01 fireline mark");
            Assert.That(state.RogueSpells.FireBattle.PendingEffects.Any(effect =>
                effect.Spell.Id == "F-P-U19" && effect.MarkedUnitId == shieldguard.Id), Is.True);
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);

            GridPosition markedPosition = shieldguard.Position;
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Attack(hero.Id, shieldguard.Id)), "N01 marked weapon hit");
            Assert.That(state.RogueSpells.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U19"), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(markedPosition), Is.True,
                "The marked hit must visibly leave fire where the target was engaged.");

            PlaythroughResult result = RunBaselineRoute(scenario, state, 3);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N02PresetRouteLuresOnlyTheNearEnemyAcrossTheCostlyVineSplitBeforeVictory()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n02_tracker");
            CombatScenarioRouteHarness route = CombatScenarioRouteHarness.Start(scenario.Id);
            UnitState hero = route.Hero;
            UnitState hound = route.Enemy("tether_hound");
            UnitState shieldguard = route.Enemy("shieldguard");

            string lanternId = route.State.RogueEquipment.ItemQuickbarInstanceIds[0];
            RogueTacticalItemInstance lantern = route.State.RogueEquipment.TacticalItem(lanternId);
            Assert.That(lantern.DefinitionId, Is.EqualTo("G-T15"));
            GridPosition lanternCell = new GridPosition(2, 1);
            int chargesBefore = lantern.ChargesCurrent;
            ArtifactExecution deployed = route.Artifact(0, ArtifactTarget.At(lanternCell), "T1部署诱导灯");
            Assert.That(deployed.Steps.Any(step => step.Kind == ArtifactEffectKind.DeployDecoy && step.Applied == 12), Is.True);
            Assert.That(lantern.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(hero.ActionPoints, Is.EqualTo(1));

            CombatCommand houndIntent = route.Plans.GetExecutionCommand(route.State, hound, hero);
            EnemyIntentPresentation houndPublic = route.Plans.GetPublicIntent(route.State, hound, hero);
            CombatCommand shieldIntent = route.Plans.GetExecutionCommand(route.State, shieldguard, hero);
            Assert.That(houndIntent.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(houndIntent.Destination.ManhattanDistance(lanternCell),
                Is.LessThan(hound.Position.ManhattanDistance(lanternCell)));
            Assert.That(houndPublic.ActionName, Does.Contain("受诱导"));
            Assert.That(houndPublic.ResultSummary, Does.Contain("诱导灯"));
            Assert.That(shieldIntent.Destination, Is.Not.EqualTo(lanternCell),
                "The distant shieldguard must keep pressuring the hero instead of joining the lure route.");

            route.Move(new GridPosition(2, 3), "T1诱导后短移");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Deploying the lure and taking one positional step must consume the complete three-AP turn.");
            GridPosition houndBefore = hound.Position;
            route.EndHeroTurnAndAdvance();

            Assert.That(hound.Position.ManhattanDistance(lanternCell),
                Is.LessThan(houndBefore.ManhattanDistance(lanternCell)));
            Assert.That(shieldguard.Position.ManhattanDistance(lanternCell), Is.GreaterThan(5));
            Assert.That(route.State.Map.GetTile(new GridPosition(4, 3)).IsLampVine, Is.True);
            Assert.That(CombatMovementQuery.EntryCost(route.State, hero, new GridPosition(4, 3)), Is.EqualTo(2));

            Assert.That(route.State.Map.GetTile(lanternCell).IsDecoy, Is.False,
                "The public one-round lure window must visibly close at the next hero turn.");
            PlaythroughResult result = RunBaselineRoute(scenario, route.State, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary + "\n" + route.TraceSummary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N03PresetRouteBuildsAcrossTheMaintenanceLineAndCreatesARealShieldGapBeforeVictory()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n03_barrier");
            CombatScenarioRouteHarness route = CombatScenarioRouteHarness.Start(scenario.Id);
            UnitState hero = route.Hero;
            UnitState mender = route.Enemy("barrier_mender");
            UnitState shieldguard = route.Enemy("shieldguard");

            CombatCommand openingSupport = route.Plans.GetExecutionCommand(route.State, mender, hero);
            Assert.That(openingSupport.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(openingSupport.TargetUnitId, Is.EqualTo(shieldguard.Id));
            Assert.That(route.State.HasLineOfSight(mender.Position, shieldguard.Position), Is.True);

            route.Move(new GridPosition(4, 4), "T1抵达维护线侧翼");
            string stampId = route.State.RogueEquipment.ItemQuickbarInstanceIds[1];
            RogueTacticalItemInstance stamp = route.State.RogueEquipment.TacticalItem(stampId);
            Assert.That(stamp.DefinitionId, Is.EqualTo("G-T07"));
            int chargesBefore = stamp.ChargesCurrent;
            GridPosition cutCell = new GridPosition(6, 4);
            ArtifactExecution wall = route.Artifact(1, ArtifactTarget.At(cutCell), "T1重柜切断维护线");
            Assert.That(wall.Steps.Any(step => step.Kind == ArtifactEffectKind.CreateHeavyCover && step.Applied == 24), Is.True);
            Assert.That(stamp.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(hero.ActionPoints, Is.Zero,
                "Reaching the line and building the wall must spend the complete three-AP turn.");
            Assert.That(route.State.HasLineOfSight(mender.Position, shieldguard.Position), Is.False);

            CombatCommand cutSupport = route.Plans.GetExecutionCommand(route.State, mender, hero);
            EnemyIntentPresentation cutPublic = route.Plans.GetPublicIntent(route.State, mender, hero);
            Assert.That(cutSupport.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(cutSupport.TargetUnitId, Is.EqualTo(mender.Id),
                "With the frontline link cut, the support may visibly redirect its ward to itself.");
            Assert.That(cutPublic.ActionName, Is.EqualTo("护障续接"));
            Assert.That(cutPublic.TargetSummary, Does.Contain(mender.DisplayName));

            route.EndHeroTurn();
            Assert.That(route.State.ActiveUnitId, Is.EqualTo(mender.Id),
                "The support acts first at the shared speed so the visible cut creates a real maintenance gap.");
            CombatCommand redirected = route.Plans.GetExecutionCommand(route.State, mender, hero);
            Assert.That(redirected.TargetUnitId, Is.EqualTo(mender.Id));
            route.ExecuteActiveEnemyAction("T1补盾助教自护");
            Assert.That(shieldguard.Shield, Is.Zero,
                "The frontline remains unshielded after the support spends its action on itself.");
            route.EndActiveEnemyTurn("T1补盾助教结束回合");
            route.AdvanceEnemiesToHero();

            PlaythroughResult result = RunBaselineRoute(scenario, route.State, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary + "\n" + route.TraceSummary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N04PresetRouteOverwritesWaterClearsACorridorAndPullsPressureBackIntoFireBeforeVictory()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n04_fire");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState raider = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "raider");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkillAt(hero.Id, 2, new GridPosition(2, 3), CardinalDirection.East)),
                "N04 lay fire across the water band");
            foreach (int x in new[] { 2, 3, 4, 5 })
            {
                GridPosition cell = new GridPosition(x, 3);
                Assert.That(state.RogueSpells.FireBattle.HasFireground(cell), Is.True, cell.ToString());
                Assert.That(state.Map.GetTile(cell).IsWater, Is.False,
                    "The later fire effect must visibly replace shallow water without stacking.");
            }

            string condenserId = state.RogueEquipment.ItemQuickbarInstanceIds[0];
            RogueTacticalItemInstance condenser = state.RogueEquipment.TacticalItem(condenserId);
            Assert.That(condenser.DefinitionId, Is.EqualTo("G-T11"));
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(new GridPosition(2, 3)), condenser.ChargesCurrent);
            Assert.That(condenser.Consume(), Is.True);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(2, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(3, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(4, 3)), Is.True);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(5, 3)), Is.True);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 3))), "N04 cross the cleared corridor");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Building, clearing and crossing the fire line must spend the complete three-AP turn.");
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(raider.Position, Is.EqualTo(new GridPosition(6, 3)));

            string canisterId = state.RogueEquipment.ItemQuickbarInstanceIds[3];
            RogueTacticalItemInstance canister = state.RogueEquipment.TacticalItem(canisterId);
            Assert.That(canister.DefinitionId, Is.EqualTo("F-T01"));
            ArtifactExecution blast = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.DemolitionCanister,
                ArtifactTarget.At(new GridPosition(5, 3)), canister.ChargesCurrent);
            Assert.That(canister.Consume(), Is.True);
            Assert.That(blast.Steps.Any(step => step.Kind == ArtifactEffectKind.CreateFireground &&
                step.Cell == new GridPosition(4, 3)), Is.True);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(4, 3)), Is.True);

            string compassId = state.RogueEquipment.ItemQuickbarInstanceIds[2];
            RogueTacticalItemInstance compass = state.RogueEquipment.TacticalItem(compassId);
            Assert.That(compass.DefinitionId, Is.EqualTo("G-T09"));
            int vitalityBefore = raider.Health + raider.Shield;
            ArtifactExecution pull = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(raider.Id, raider.Position), compass.ChargesCurrent);
            Assert.That(compass.Consume(), Is.True);
            Assert.That(pull.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveTarget && step.Applied > 0), Is.True);
            Assert.That(raider.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(vitalityBefore - raider.Health - raider.Shield, Is.EqualTo(System.Math.Min(8, vitalityBefore)),
                "The raider's pincer pressure becomes a visible counterplay resource when pulled onto fire.");

            PlaythroughResult result = RunBaselineRoute(scenario, state, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N05PresetRouteBreachesTheSharedFiringLineAndForcesTheArbalistToRetreatFromItsDeadzone()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n05_arbalist");
            CombatScenarioRouteHarness route = CombatScenarioRouteHarness.Start(scenario.Id);
            UnitState hero = route.Hero;
            UnitState arbalist = route.Enemy("rune_arbalist");
            GridPosition sharedCabinet = new GridPosition(4, 3);

            Assert.That(route.State.Map.GetTile(sharedCabinet).Durability, Is.EqualTo(24));
            route.Move(new GridPosition(3, 3), "T1抵达共用重柜");
            string wedgeId = route.State.RogueEquipment.ItemQuickbarInstanceIds[1];
            RogueTacticalItemInstance wedge = route.State.RogueEquipment.TacticalItem(wedgeId);
            Assert.That(wedge.DefinitionId, Is.EqualTo("G-T08"));
            int chargesBefore = wedge.ChargesCurrent;
            ArtifactExecution breach = route.Artifact(1, ArtifactTarget.At(sharedCabinet), "T1拆开共用重柜");
            Assert.That(breach.Steps.Any(step => step.Kind == ArtifactEffectKind.DamageObject && step.Applied == 24), Is.True);
            Assert.That(wedge.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(route.State.Map.GetTile(sharedCabinet).IsDestroyed, Is.True);
            route.Move(new GridPosition(5, 3), "T1越过新射线");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Approach, breach and crossing must spend the complete three-AP opening turn.");

            EnemyIntentPresentation exposedShot = route.Plans.GetPublicIntent(route.State, arbalist, hero);
            Assert.That(exposedShot.ActionName, Is.EqualTo("绞盘重矢"));
            Assert.That(exposedShot.ExpectedDamage, Is.GreaterThan(0));
            route.EndHeroTurnAndAdvance();
            Assert.That(arbalist.Cooldown(EnemyAbilityCatalog.WindlassBolt), Is.GreaterThan(0),
                "Opening the cabinet must expose the hero to the same real firing line they created.");

            route.Move(new GridPosition(6, 1), "T2进入背弩生死区");
            Assert.That(hero.Position.ManhattanDistance(arbalist.Position), Is.EqualTo(1));
            route.Spell(0, arbalist.Id, "T2贴身攻击一");
            route.Spell(0, arbalist.Id, "T2贴身攻击二");
            Assert.That(hero.ActionPoints, Is.Zero);

            CombatCommand retreat = route.Plans.GetExecutionCommand(route.State, arbalist, hero);
            EnemyIntentPresentation retreatIntent = route.Plans.GetPublicIntent(route.State, arbalist, hero);
            Assert.That(retreat.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(retreat.Destination.ManhattanDistance(hero.Position), Is.EqualTo(2));
            Assert.That(retreatIntent.ActionName, Is.EqualTo("背弩生退距"));
            Assert.That(retreatIntent.ResultSummary, Does.Contain("近身死区"));
            Assert.That(retreatIntent.ExpectedDamage, Is.Zero);

            PlaythroughResult result = RunBaselineRoute(scenario, route.State, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary + "\n" + route.TraceSummary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N06PresetRouteBaitsTheVisibleSnarePullsItsCasterAdjacentAndPaysToBreakFree()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n06_restraint");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState snare = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "stone_snare");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 3))), "N06 enter the visible snare line");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 2, hero.Id)), "N06 prepare generic shield");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 1, snare.Id)), "N06 pressure the snare caster");
            Assert.That(hero.ActionPoints, Is.Zero);
            plans.Invalidate();
            EnemyIntentPresentation incoming = plans.GetPublicIntent(state, snare, hero);
            Assert.That(incoming.ActionName, Is.EqualTo("石索"));
            Assert.That(incoming.ResultSummary, Does.Contain("束缚 3 回合"));

            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(hero.StatusDuration(StatusType.Bound), Is.EqualTo(2),
                "Three status ticks must leave two actual bound decision windows after turn-start decay.");
            CombatCommandExecutionResult blockedMove = commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 2)));
            Assert.That(blockedMove.Accepted, Is.False);
            Assert.That(blockedMove.RejectionReason, Does.Contain("束缚"));

            string compassId = state.RogueEquipment.ItemQuickbarInstanceIds[1];
            RogueTacticalItemInstance compass = state.RogueEquipment.TacticalItem(compassId);
            Assert.That(compass.DefinitionId, Is.EqualTo("G-T09"));
            int chargesBefore = compass.ChargesCurrent;
            ArtifactExecution pull = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(snare.Id, snare.Position), compass.ChargesCurrent);
            Assert.That(compass.Consume(), Is.True);
            Assert.That(pull.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveTarget && step.Applied == 2), Is.True);
            Assert.That(compass.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(snare.Position, Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(hero.Position.ManhattanDistance(snare.Position), Is.EqualTo(1));

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 5, snare.Id)), "N06 generic bound-plus-adjacent release");
            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 2))), "N06 spend the released movement immediately");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Pull, conditional release and reposition must consume the full three-AP response turn.");

            PlaythroughResult result = RunBaselineRoute(scenario, state, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N07PresetRoutePullsTheFrontlineOutOfWardRangeAndLeavesThreeDistinctPublicThreats()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n07_maintenance");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState shieldguard = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "shieldguard");
            UnitState mender = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "barrier_mender");
            UnitState revealer = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "lantern_revealer");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            CombatCommand initialWard = plans.GetExecutionCommand(state, mender, hero);
            Assert.That(initialWard.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(initialWard.TargetUnitId, Is.EqualTo(shieldguard.Id));
            Assert.That(mender.Position.ManhattanDistance(shieldguard.Position), Is.LessThanOrEqualTo(4));

            string compassId = state.RogueEquipment.ItemQuickbarInstanceIds[1];
            RogueTacticalItemInstance compass = state.RogueEquipment.TacticalItem(compassId);
            Assert.That(compass.DefinitionId, Is.EqualTo("G-T09"));
            int chargesBefore = compass.ChargesCurrent;
            ArtifactExecution pull = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(shieldguard.Id, shieldguard.Position), compass.ChargesCurrent);
            Assert.That(compass.Consume(), Is.True);
            Assert.That(pull.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveTarget && step.Applied == 2), Is.True);
            Assert.That(compass.ChargesCurrent, Is.EqualTo(chargesBefore - 1));
            Assert.That(shieldguard.Position, Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(mender.Position.ManhattanDistance(shieldguard.Position), Is.GreaterThan(4));

            plans.Invalidate();
            CombatCommand redirectedWard = plans.GetExecutionCommand(state, mender, hero);
            EnemyIntentPresentation redirectedPublic = plans.GetPublicIntent(state, mender, hero);
            Assert.That(redirectedWard.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(redirectedWard.TargetUnitId, Is.EqualTo(mender.Id));
            Assert.That(redirectedPublic.TargetSummary, Does.Contain(mender.DisplayName));

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 6, shieldguard.Id)), "N07 mark the isolated frontline");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 3))), "N07 occupy the isolated frontline lane");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Pull, pressure and occupation must consume the complete three-AP opening turn.");
            Assert.That(hero.Position.ManhattanDistance(shieldguard.Position), Is.EqualTo(1));

            plans.Invalidate();
            EnemyIntentPresentation frontlineIntent = plans.GetPublicIntent(state, shieldguard, hero);
            EnemyIntentPresentation supportIntent = plans.GetPublicIntent(state, mender, hero);
            EnemyIntentPresentation revealIntent = plans.GetPublicIntent(state, revealer, hero);
            Assert.That(frontlineIntent.ActionName, Is.EqualTo("铭盾冲撞"));
            Assert.That(frontlineIntent.ExpectedDamage, Is.GreaterThan(0));
            Assert.That(supportIntent.ActionName, Is.EqualTo("护障续接"));
            Assert.That(supportIntent.TargetSummary, Does.Contain(mender.DisplayName));
            Assert.That(revealIntent.ActionName, Is.EqualTo("移动"));
            Assert.That(new[] { frontlineIntent.Signature, supportIntent.Signature, revealIntent.Signature }.Distinct().Count(), Is.EqualTo(3));

            PlaythroughResult result = RunBaselineRoute(scenario, state, 1);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N08PresetRouteBindsOnePincerForARealDecisionWindowThenPushesTheOtherAway()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n08_containment");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState hound = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "tether_hound");
            UnitState mauler = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "sigil_mauler");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 2))), "N08 enter the north control lane");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 2, hero.Id)), "N08 prepare for converging pressure");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 1, hound.Id)), "N08 pressure the north controller");
            Assert.That(hero.ActionPoints, Is.Zero);
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(hound.Position, Is.EqualTo(new GridPosition(5, 1)));

            string frameId = state.RogueEquipment.ItemQuickbarInstanceIds[0];
            RogueTacticalItemInstance frame = state.RogueEquipment.TacticalItem(frameId);
            Assert.That(frame.DefinitionId, Is.EqualTo("G-T03"));
            int frameChargesBefore = frame.ChargesCurrent;
            GridPosition heldPosition = hound.Position;
            ArtifactExecution bind = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.BindingFrame,
                ArtifactTarget.Unit(hound.Id, hound.Position), frame.ChargesCurrent);
            Assert.That(frame.Consume(), Is.True);
            Assert.That(bind.Steps.Any(step => step.Kind == ArtifactEffectKind.ApplyStatus && step.Applied == 2), Is.True);
            Assert.That(frame.ChargesCurrent, Is.EqualTo(frameChargesBefore - 1));
            Assert.That(hound.StatusDuration(StatusType.Bound), Is.EqualTo(2),
                "A public one-round bind needs two internal ticks so one tick remains at the target's decision.");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 1, hound.Id)), "N08 spend the remaining AP on the held target");
            Assert.That(hero.ActionPoints, Is.Zero);

            CombatResolver.EndTurn(state, hero);
            Assert.That(state.ActiveUnitId, Is.EqualTo(hound.Id));
            Assert.That(hound.StatusDuration(StatusType.Bound), Is.EqualTo(1));
            plans.Invalidate();
            CombatCommand heldCommand = plans.GetExecutionCommand(state, hound, hero);
            EnemyIntentPresentation heldIntent = plans.GetPublicIntent(state, hound, hero);
            Assert.That(heldCommand.Type, Is.EqualTo(CombatCommandType.EndTurn));
            Assert.That(heldIntent.ActionName, Is.EqualTo("受缚驻留"));
            Assert.That(heldIntent.ResultSummary, Does.Contain("仍可攻击"));
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle, heldCommand), "N08 consume the visible held turn");
            plans.Invalidate();
            Assert.That(hound.Position, Is.EqualTo(heldPosition),
                "The bind must stop one real movement decision rather than expiring before it matters.");
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(mauler.Position, Is.EqualTo(new GridPosition(6, 2)));

            string plumbId = state.RogueEquipment.ItemQuickbarInstanceIds[3];
            RogueTacticalItemInstance plumb = state.RogueEquipment.TacticalItem(plumbId);
            Assert.That(plumb.DefinitionId, Is.EqualTo("G-T17"));
            int plumbChargesBefore = plumb.ChargesCurrent;
            ArtifactExecution push = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.SeismicPlumb,
                ArtifactTarget.At(new GridPosition(5, 2)), plumb.ChargesCurrent);
            Assert.That(plumb.Consume(), Is.True);
            Assert.That(push.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveFromCell && step.Applied == 1), Is.True);
            Assert.That(mauler.Position, Is.EqualTo(new GridPosition(7, 2)));
            Assert.That(plumb.ChargesCurrent, Is.EqualTo(plumbChargesBefore - 1));
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 2))), "N08 withdraw after breaking the pincer");
            Assert.That(hero.ActionPoints, Is.Zero,
                "The two-AP push and one-AP reposition must fill the entire response turn.");

            PlaythroughResult result = RunBaselineRoute(scenario, state, 3);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void N09PresetRouteRewritesWaterThenOffersAnEfficientFinishOrASaferBreach()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_n09_crossfire");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState shieldguard = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "shieldguard");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            GridPosition northCabinet = new GridPosition(4, 2);
            GridPosition southCabinet = new GridPosition(4, 4);

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkillAt(hero.Id, 2, new GridPosition(2, 3), CardinalDirection.East)),
                "N09 replace the shallow-water band and put the waiting shieldguard under visible fire pressure");
            foreach (int x in new[] { 2, 3, 4, 5 })
            {
                GridPosition cell = new GridPosition(x, 3);
                Assert.That(state.RogueSpells.FireBattle.HasFireground(cell), Is.True, cell.ToString());
                Assert.That(state.Map.GetTile(cell).IsWater, Is.False,
                    "The later field effect must replace shallow water rather than stack with it.");
            }

            RogueTacticalItemInstance condenser = state.RogueEquipment.TacticalItem(
                state.RogueEquipment.ItemQuickbarInstanceIds[0]);
            Assert.That(condenser.DefinitionId, Is.EqualTo("G-T11"));
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(new GridPosition(2, 3)), condenser.ChargesCurrent);
            Assert.That(condenser.Consume(), Is.True);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(2, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(3, 3)), Is.False);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(4, 3)), Is.True);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(new GridPosition(5, 3)), Is.True);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 3))), "N09 enter through the cooled half of the line");
            Assert.That(hero.ActionPoints, Is.Zero);
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(shieldguard.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(shieldguard.Health, Is.EqualTo(12));
            Assert.That(shieldguard.Shield, Is.EqualTo(4),
                "The shieldguard starts on F4 fire and loses eight visible vitality before moving to E4.");

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 7, shieldguard.Id)),
                "N09 burst the frontline and splash both adjacent cabinets");
            Assert.That(shieldguard.Health, Is.Zero);
            Assert.That(shieldguard.Shield, Is.Zero);
            Assert.That(state.Map.GetTile(northCabinet).Durability, Is.EqualTo(16));
            Assert.That(state.Map.GetTile(southCabinet).Durability, Is.EqualTo(16));
            Assert.That(shieldguard.IsAlive, Is.False);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(3, 2))),
                "N09 use the remaining AP to stand beside the softened north cabinet");
            Assert.That(hero.ActionPoints, Is.Zero);
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            CombatState efficiencyContinuation = BuildN09OpeningState(scenario);

            RogueTacticalItemInstance wedge = state.RogueEquipment.TacticalItem(
                state.RogueEquipment.ItemQuickbarInstanceIds[1]);
            Assert.That(wedge.DefinitionId, Is.EqualTo("G-T08"));
            ArtifactExecution breach = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.BreachWedge,
                ArtifactTarget.At(northCabinet), wedge.ChargesCurrent);
            Assert.That(wedge.Consume(), Is.True);
            Assert.That(breach.Steps.Any(step => step.Kind == ArtifactEffectKind.DamageObject && step.Applied >= 16), Is.True);
            Assert.That(state.Map.GetTile(northCabinet).IsDestroyed, Is.True);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(5, 2))),
                "N09 cross the newly opened north lane");

            RogueTacticalItemInstance aegis = state.RogueEquipment.TacticalItem(
                state.RogueEquipment.ItemQuickbarInstanceIds[3]);
            Assert.That(aegis.DefinitionId, Is.EqualTo("G-T01"));
            int shieldBefore = hero.Shield;
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.AegisFold,
                ArtifactTarget.Unit(hero.Id, hero.Position), aegis.ChargesCurrent);
            Assert.That(aegis.Consume(), Is.True);
            Assert.That(hero.Shield, Is.EqualTo(shieldBefore + 20));
            Assert.That(hero.ActionPoints, Is.Zero,
                "The safer branch spends its full turn on breach, crossing and defense instead of claiming the short-turn reward.");

            PlaythroughResult result = RunBaselineRoute(scenario, efficiencyContinuation, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(8), result.Summary);
        }

        [Test]
        public void E01PresetRoutePullsTheVanguardOutOfMaintenanceThenConsumesAWeaponAttachment()
        {
            CombatTestArenaScenario scenario = CombatTestArenaScenarioCatalog.Get("arena_e01_maintenance");
            CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState vanguard = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "elite_vanguard");
            UnitState mender = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "barrier_mender");
            ArtifactBattleState artifacts = new ArtifactBattleState(state);

            CombatCommand initialWard = plans.GetExecutionCommand(state, mender, hero);
            Assert.That(initialWard.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(initialWard.TargetUnitId, Is.EqualTo(vanguard.Id));
            Assert.That(mender.Position.ManhattanDistance(vanguard.Position), Is.EqualTo(4));

            RogueTacticalItemInstance compass = state.RogueEquipment.TacticalItem(
                state.RogueEquipment.ItemQuickbarInstanceIds[1]);
            Assert.That(compass.DefinitionId, Is.EqualTo("G-T09"));
            ArtifactExecution pull = ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(vanguard.Id, vanguard.Position), compass.ChargesCurrent);
            Assert.That(compass.Consume(), Is.True);
            Assert.That(pull.Steps.Any(step => step.Kind == ArtifactEffectKind.ForceMoveTarget && step.Applied == 2), Is.True);
            Assert.That(vanguard.Position, Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(mender.Position.ManhattanDistance(vanguard.Position), Is.EqualTo(6));

            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 4, hero.Id)), "E01 arm break-stance calibration");
            Assert.That(state.RogueSpells.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U03"), Is.True);
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(2, 3))), "E01 occupy the isolated vanguard lane");
            Assert.That(hero.ActionPoints, Is.Zero,
                "Compass, weapon attachment and one reposition must fill the three-AP setup turn.");

            plans.Invalidate();
            CombatCommand redirectedWard = plans.GetExecutionCommand(state, mender, hero);
            EnemyIntentPresentation redirectedPublic = plans.GetPublicIntent(state, mender, hero);
            Assert.That(redirectedWard.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(redirectedWard.TargetUnitId, Is.EqualTo(mender.Id));
            Assert.That(redirectedPublic.TargetSummary, Does.Contain(mender.DisplayName));
            CombatResolver.EndTurn(state, hero);
            AdvanceEnemiesToHero(state, commands, plans);
            Assert.That(hero.HasStatus(StatusType.BreakStance), Is.True,
                "The player openly accepts one vanguard pressure hit in exchange for breaking the support distance.");

            int vitalityBeforeWeapon = vanguard.Health + vanguard.Shield;
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Attack(hero.Id, vanguard.Id)), "E01 consume U03 with the actual main-hand attack action");
            Assert.That(vanguard.Health + vanguard.Shield, Is.LessThan(vitalityBeforeWeapon));
            Assert.That(vanguard.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(state.RogueSpells.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U03"), Is.False,
                "Basic personal spells are not weapon attacks; the explicit main-hand action consumes this attachment.");
            AssertAccepted(commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 6, vanguard.Id)), "E01 finish and push the isolated vanguard");
            Assert.That(vanguard.IsAlive, Is.False);
            Assert.That(vanguard.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(hero.ActionPoints, Is.Zero,
                "One weapon attack plus the two-AP heavy strike fills the response turn.");

            PlaythroughResult result = RunBaselineRoute(scenario, state, 2);
            Assert.That(result.Rejection, Is.Empty, result.Rejection);
            Assert.That(result.State.IsVictory, Is.True, result.Summary);
            Assert.That(result.State.GetUnit("hero").IsAlive, Is.True);
            Assert.That(result.HeroTurns, Is.LessThanOrEqualTo(12), result.Summary);
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
