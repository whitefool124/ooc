using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class CombatTestArenaScenarioTests
    {
        [Test]
        public void EveryScenarioBuildsACompactConnectedBattleWithFullLoadouts()
        {
            Assert.That(CombatTestArenaScenarioCatalog.All.Length, Is.EqualTo(25));
            Assert.That(CombatTestArenaScenarioCatalog.All.Count(scenario => !scenario.IsHighPressure && !scenario.IsSystemTest && !scenario.IsSkillTest), Is.EqualTo(9));
            Assert.That(CombatTestArenaScenarioCatalog.All.Count(scenario => scenario.IsHighPressure), Is.EqualTo(4));
            Assert.That(CombatTestArenaScenarioCatalog.All.Count(scenario => scenario.IsSystemTest), Is.EqualTo(3));
            Assert.That(CombatTestArenaScenarioCatalog.All.Count(scenario => scenario.IsSkillTest), Is.EqualTo(9));
            foreach (CombatTestArenaScenario scenario in CombatTestArenaScenarioCatalog.All)
            {
                CombatSceneSessionBuild build = CombatTestArenaScenarioCatalog.Build(scenario.Id);
                CombatState state = build.State;
                Assert.That(state.Map.Width, Is.EqualTo(scenario.Level.Width), scenario.Id);
                Assert.That(state.Map.Height, Is.EqualTo(scenario.Level.Height), scenario.Id);
                Assert.That(state.Map.Width, Is.InRange(10, 11), scenario.Id);
                Assert.That(state.Map.Height, Is.InRange(7, 8), scenario.Id);
                Assert.That(state.Ruleset, Is.EqualTo(CombatRuleset.Roguelite), scenario.Id);
                Assert.That(state.Units.Values.Count(unit => unit.IsHero), Is.EqualTo(1), scenario.Id);
                Assert.That(state.Units.Values.Count(unit => !unit.IsHero), Is.EqualTo(scenario.Level.EnemyPlacements.Count), scenario.Id);
                Assert.That(state.RogueSpells.Loadout.EquippedSpellIds.All(id => !string.IsNullOrEmpty(id)), Is.True, scenario.Id);
                Assert.That(state.RogueSpells.Loadout.EquippedSpellIds.Distinct().Count(),
                    Is.EqualTo(RogueRuntimeConstants.SpellSlotCount), scenario.Id + " repeats a spell instead of filling a real eight-spell preset.");
                Assert.That(state.RogueEquipment.ItemQuickbarInstanceIds.All(id => !string.IsNullOrEmpty(id)), Is.True, scenario.Id);
                RogueTacticalItemInstance[] quickbar = state.RogueEquipment.ItemQuickbarInstanceIds
                    .Select(state.RogueEquipment.TacticalItem).ToArray();
                Assert.That(quickbar.All(item => item != null && item.ChargesCurrent == item.ChargesMaximum && item.ChargesMaximum > 0),
                    Is.True, scenario.Id + " must enter with every tactical item at full charges.");
                Assert.That(quickbar.Select(item => item.DefinitionId).ToArray(), Is.EqualTo(scenario.ArtifactIds),
                    scenario.Id + " quickbar order must match the authored preset.");
                Assert.That(quickbar.Select(item => item.DefinitionId).Distinct().Count(),
                    Is.EqualTo(RogueRuntimeConstants.ItemQuickbarSize), scenario.Id + " repeats a tactical item instead of filling four authored roles.");
                TileState[] permanentWalls = state.Map.PositionsWith(tile => tile.IsPermanentWall)
                    .Select(state.Map.GetTile).ToArray();
                Assert.That(permanentWalls.Length, Is.GreaterThanOrEqualTo(3), scenario.Id + " needs several permanent walls to create durable lanes.");
                Assert.That(permanentWalls.All(tile => tile.BlocksMovement && tile.BlocksLineOfSight && !tile.IsDestroyed), Is.True,
                    scenario.Id + " permanent walls must not be movable, transparent, or destructible.");
                AssertAllWalkableCellsConnected(state.Map, state.GetUnit("hero").Position, scenario.Id);
            }
        }

        [Test]
        public void RandomizedArenaEntryBuildsFullDistinctLoadouts()
        {
            for (int index = 0; index < 12; index++)
            {
                CombatSceneSessionBuild build = CombatTestArenaScenarioCatalog.BuildRandomized("arena_n01_flank");
                string[] spells = build.State.RogueSpells.Loadout.EquippedSpellIds.ToArray();
                RogueTacticalItemInstance[] artifacts = build.State.RogueEquipment.ItemQuickbarInstanceIds
                    .Select(build.State.RogueEquipment.TacticalItem).ToArray();
                Assert.That(spells.Length, Is.EqualTo(RogueRuntimeConstants.SpellSlotCount));
                Assert.That(spells.Distinct().Count(), Is.EqualTo(RogueRuntimeConstants.SpellSlotCount));
                Assert.That(artifacts.Length, Is.EqualTo(RogueRuntimeConstants.ItemQuickbarSize));
                Assert.That(artifacts.Select(item => item.DefinitionId).Distinct().Count(), Is.EqualTo(RogueRuntimeConstants.ItemQuickbarSize));
            }
        }

        [Test]
        public void EveryAuthoredBattleStartsWithMoreThanOneReachableMovementLane()
        {
            CombatTestArenaScenario[] authoredBattles = CombatTestArenaScenarioCatalog.All.Where(scenario =>
                !scenario.IsSystemTest && !scenario.IsSkillTest).ToArray();
            Assert.That(authoredBattles.Length, Is.EqualTo(13));
            foreach (CombatTestArenaScenario scenario in authoredBattles)
            {
                CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
                UnitState hero = state.GetUnit("hero");
                List<GridPosition> reachable = new List<GridPosition>();
                for (int y = 0; y < state.Map.Height; y++)
                    for (int x = 0; x < state.Map.Width; x++)
                    {
                        GridPosition destination = new GridPosition(x, y);
                        if (destination == hero.Position || state.Map.IsBlocked(destination) || state.IsOccupied(destination, hero.Id)) continue;
                        if (CombatMovementQuery.FindPath(state, hero, destination).Count > 1) reachable.Add(destination);
                    }
                Assert.That(reachable.Count, Is.GreaterThanOrEqualTo(4),
                    scenario.Id + " offers too few meaningful endpoints inside one real movement action.");
                Assert.That(reachable.Select(position => position.Y.CompareTo(hero.Position.Y)).Distinct().Count(), Is.GreaterThanOrEqualTo(2),
                    scenario.Id + " keeps every reachable endpoint in one horizontal lane instead of exposing a route branch.");
            }
        }

        [Test]
        public void EveryAuthoredEnemyCanExecuteItsOpeningPublicIntent()
        {
            CombatTestArenaScenario[] authoredBattles = CombatTestArenaScenarioCatalog.All.Where(scenario =>
                !scenario.IsSystemTest && !scenario.IsSkillTest).ToArray();
            foreach (CombatTestArenaScenario scenario in authoredBattles)
            {
                string[] enemyIds = CombatTestArenaScenarioCatalog.Build(scenario.Id).State.Units.Values
                    .Where(unit => !unit.IsHero).Select(unit => unit.Id).ToArray();
                foreach (string enemyId in enemyIds)
                {
                    CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
                    UnitState hero = state.GetUnit("hero");
                    UnitState enemy = state.GetUnit(enemyId);
                    EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
                    EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);
                    CombatCommand command = plans.GetExecutionCommand(state, enemy, hero);

                    Assert.That(intent, Is.Not.Null, scenario.Id + "/" + enemyId);
                    Assert.That(intent.ActionName, Is.Not.Empty, scenario.Id + "/" + enemyId);
                    Assert.That(intent.DetailedText, Is.Not.Empty, scenario.Id + "/" + enemyId);
                    bool genericIntent = state.RainLanternCourt == null && state.GreenhouseCollectionRoom == null &&
                        state.ThreeMaterialPressure == null && state.AcademyCoreBoss == null && state.PressureTest == null;
                    if (genericIntent)
                        Assert.That(intent.Signature, Is.EqualTo(CombatInformationPresenter.CommandSignature(command)),
                            scenario.Id + "/" + enemyId + " previews a different command from the one it executes.");
                    else
                        Assert.That(intent.Signature, Is.Not.Empty,
                            scenario.Id + "/" + enemyId + " custom intent needs a stable public signature.");
                    CombatResolver.BeginTurn(state, enemy.Id);
                    Assert.DoesNotThrow(() => CombatResolver.Resolve(state, command),
                        scenario.Id + "/" + enemyId + " authored an illegal opening command: " + intent.DetailedText);
                }
            }
        }

        [Test]
        public void EveryAuthoredBattleSurvivesThreeEnemyReplanCyclesWithoutIllegalOccupancy()
        {
            CombatTestArenaScenario[] authoredBattles = CombatTestArenaScenarioCatalog.All.Where(scenario =>
                !scenario.IsSystemTest && !scenario.IsSkillTest).ToArray();
            foreach (CombatTestArenaScenario scenario in authoredBattles)
            {
                CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
                UnitState hero = state.GetUnit("hero");
                hero.ConfigureVitality(999);
                for (int round = 0; round < 3; round++)
                {
                    string[] enemyIds = state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive)
                        .Select(unit => unit.Id).OrderBy(id => id).ToArray();
                    foreach (string enemyId in enemyIds)
                    {
                        UnitState enemy = state.GetUnit(enemyId);
                        EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
                        EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);
                        CombatCommand command = plans.GetExecutionCommand(state, enemy, hero);
                        CombatResolver.BeginTurn(state, enemy.Id);
                        Assert.DoesNotThrow(() => CombatResolver.Resolve(state, command),
                            scenario.Id + "/round" + (round + 1) + "/" + enemyId + " failed: " + intent.DetailedText);
                        Assert.That(state.Units.Values.Where(unit => unit.IsAlive).Select(unit => unit.Position).Distinct().Count(),
                            Is.EqualTo(state.Units.Values.Count(unit => unit.IsAlive)),
                            scenario.Id + "/round" + (round + 1) + " produced overlapping living units.");
                        Assert.That(state.Units.Values.Where(unit => unit.IsAlive).All(unit =>
                            state.Map.IsInside(unit.Position) && !state.Map.IsBlocked(unit.Position)), Is.True,
                            scenario.Id + "/round" + (round + 1) + " moved a living unit into blocked space.");
                    }
                }
            }
        }

        [Test]
        public void DedicatedSpellLabsCoverEveryPersonalSpellInFullEightSlotLoadouts()
        {
            CombatTestArenaScenario[] labs = CombatTestArenaScenarioCatalog.All.Where(scenario => scenario.IsSkillTest).ToArray();
            string[] covered = labs.SelectMany(scenario => scenario.SpellIds).Distinct().OrderBy(id => id).ToArray();
            string[] required = FireSpellCatalog.All.Select(spell => spell.Id).OrderBy(id => id).ToArray();

            Assert.That(covered, Is.SupersetOf(required));
            Assert.That(labs, Has.All.Matches<CombatTestArenaScenario>(scenario =>
                scenario.SpellIds.Count == RogueRuntimeConstants.SpellSlotCount &&
                scenario.ArtifactIds.Count == RogueRuntimeConstants.ItemQuickbarSize));
        }

        [Test]
        public void DedicatedArenaPresetsCoverEveryArtifact()
        {
            string[] covered = CombatTestArenaScenarioCatalog.All
                .SelectMany(scenario => scenario.ArtifactIds)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            string[] required = ArtifactCatalog.All
                .Select(artifact => artifact.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.That(covered, Is.EqualTo(required));
        }

        [Test]
        public void DedicatedArenaPresetsCoverEveryAcademySpell()
        {
            string[] covered = CombatTestArenaScenarioCatalog.All
                .SelectMany(scenario => scenario.SpellIds)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            string[] required = RogueContentCatalog.CreateAcademyV01().Spells
                .Select(spell => spell.DefinitionId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.That(covered, Is.EqualTo(required));
        }

        [Test]
        public void ConditionalQuickbarToolsHaveAReusableStarterInsideTheSamePreset()
        {
            foreach (CombatTestArenaScenario scenario in CombatTestArenaScenarioCatalog.All)
            {
                if (scenario.ArtifactIds.Contains("G-T06") || scenario.ArtifactIds.Contains("G-T16"))
                    Assert.That(scenario.SpellIds, Does.Contain("F-P-U07"),
                        scenario.Id + " equips a 12-shield consumer without its reusable 12-shield starter.");

                if (scenario.ArtifactIds.Contains("G-T11"))
                {
                    bool createsFireground = scenario.SpellIds.Where(id => id.StartsWith("F-P-"))
                        .Select(FireSpellCatalog.Get)
                        .Any(spell => spell.Rules.Any(rule => rule.Kind == FireRuleKind.CreateFireground));
                    bool createsSmoke = scenario.ArtifactIds.Contains("G-T18");
                    Assert.That(createsFireground || createsSmoke, Is.True,
                        scenario.Id + " equips the hazard condenser without any fireground or smoke source to clear.");
                }
            }
        }

        [Test]
        public void ConditionalPersonalSpellsHaveGenericStartersInsideTheSamePreset()
        {
            foreach (CombatTestArenaScenario scenario in CombatTestArenaScenarioCatalog.All)
            {
                FireSpellDefinition[] spells = scenario.SpellIds.Where(id => id.StartsWith("F-P-"))
                    .Select(FireSpellCatalog.Get).ToArray();
                bool canApplyBurning = spells.Any(spell => spell.Rules.Any(rule => rule.Kind == FireRuleKind.ApplyBurning));
                bool canApplyBreak = spells.Any(spell => spell.Rules.Any(rule =>
                    rule.Kind == FireRuleKind.ApplyBreakStance || rule.Kind == FireRuleKind.ApplyArmorBreak)) ||
                    scenario.ArtifactIds.Contains("G-T04");
                bool canCreateFireground = spells.Any(spell => spell.Rules.Any(rule =>
                    rule.Kind == FireRuleKind.CreateFireground)) || scenario.ArtifactIds.Contains("F-T01");

                foreach (FireSpellDefinition spell in spells)
                {
                    bool needsBurning = spell.TargetKind == FireTargetKind.BurningUnit ||
                        spell.TargetKind == FireTargetKind.BurningEnemy ||
                        spell.TargetKind == FireTargetKind.AdjacentBurningEnemy ||
                        spell.Rules.Any(rule => rule.Condition == FireCondition.TargetBurning ||
                            rule.Condition == FireCondition.TargetBurningAndOnFireground);
                    bool acceptsBurningOrBreak = spell.TargetKind == FireTargetKind.BurningOrArmorBrokenEnemy ||
                        spell.Rules.Any(rule => rule.Condition == FireCondition.TargetBurningOrArmorBroken ||
                            rule.Condition == FireCondition.TargetBurningOrBreakStance);
                    bool needsFireground = spell.TargetKind == FireTargetKind.BurningCell ||
                        spell.Rules.Any(rule => rule.Condition == FireCondition.TargetOnFireground ||
                            rule.Condition == FireCondition.TargetBurningAndOnFireground);

                    if (needsBurning)
                        Assert.That(canApplyBurning, Is.True,
                            scenario.Id + "/" + spell.Id + " consumes burning without a reusable burning starter.");
                    if (acceptsBurningOrBreak)
                        Assert.That(canApplyBurning || canApplyBreak, Is.True,
                            scenario.Id + "/" + spell.Id + " has neither a burning nor break-stance starter.");
                    if (needsFireground)
                        Assert.That(canCreateFireground, Is.True,
                            scenario.Id + "/" + spell.Id + " consumes fireground without a reusable field starter.");
                }
            }
        }

        [Test]
        public void FirePresetCanCreateConsumeAndClearItsSharedBattlefieldEffect()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_n04_fire").State;
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            FireBattleState fire = state.RogueSpells.FireBattle;
            GridPosition firstFireCell = new GridPosition(2, 3);

            FireSpellPreview road = FireSpellEngine.Preview(fire, hero.Id, FireSpellCatalog.Get("F-P-R12"),
                FireSpellTarget.At(firstFireCell, CardinalDirection.East));
            Assert.That(road.CanCommit, Is.True, string.Join("/", road.Failures));
            FireSpellEngine.Execute(fire, hero.Id, FireSpellCatalog.Get("F-P-R12"),
                FireSpellTarget.At(firstFireCell, CardinalDirection.East));
            Assert.That(fire.HasFireground(firstFireCell), Is.True);

            FireSpellPreview reclaim = FireSpellEngine.Preview(fire, hero.Id, FireSpellCatalog.Get("F-P-U11"),
                FireSpellTarget.At(firstFireCell, CardinalDirection.East));
            Assert.That(reclaim.CanCommit, Is.True, string.Join("/", reclaim.Failures));
            FireSpellEngine.Execute(fire, hero.Id, FireSpellCatalog.Get("F-P-U11"),
                FireSpellTarget.At(firstFireCell, CardinalDirection.East));
            Assert.That(fire.HasFireground(firstFireCell), Is.False);

            GridPosition remainingFireCell = new GridPosition(3, 3);
            Assert.That(fire.HasFireground(remainingFireCell), Is.True);
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            ArtifactPreview condenser = ArtifactEngine.Preview(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(remainingFireCell), ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(condenser.CanCommit, Is.True, string.Join("/", condenser.Failures));
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(remainingFireCell), ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(fire.HasFireground(remainingFireCell), Is.False);
        }

        [Test]
        public void ShieldStarterMakesHealingAndCoverConversionPresetsExecutable()
        {
            CombatState healing = CombatTestArenaScenarioCatalog.Build("arena_n06_restraint").State;
            UnitState healingHero = healing.GetUnit("hero");
            CombatResolver.BeginTurn(healing, healingHero.Id);
            CombatEffectExecutor.Execute(healing, healingHero.Id, CombatEffect.DamageHealth(healingHero.Id, 4));
            int injuredHealth = healingHero.Health;
            FireSpellEngine.Execute(healing.RogueSpells.FireBattle, healingHero.Id, FireSpellCatalog.Get("F-P-U07"),
                FireSpellTarget.Unit(healingHero.Id));
            ArtifactPreview healPreview = ArtifactEngine.Preview(new ArtifactBattleState(healing), healingHero.Id,
                ArtifactCatalog.MendingLattice, ArtifactTarget.Unit(healingHero.Id, healingHero.Position),
                ArtifactCatalog.MendingLattice.MaximumUses);
            Assert.That(healPreview.CanCommit, Is.True, string.Join("/", healPreview.Failures));
            ArtifactEngine.Execute(new ArtifactBattleState(healing), healingHero.Id, ArtifactCatalog.MendingLattice,
                ArtifactTarget.Unit(healingHero.Id, healingHero.Position), ArtifactCatalog.MendingLattice.MaximumUses);
            Assert.That(healingHero.Health, Is.GreaterThan(injuredHealth));

            foreach (string scenarioId in new[] { "arena_e01_maintenance", "arena_b01_core" })
            {
                CombatState conversion = CombatTestArenaScenarioCatalog.Build(scenarioId).State;
                UnitState hero = conversion.GetUnit("hero");
                CombatResolver.BeginTurn(conversion, hero.Id);
                FireSpellEngine.Execute(conversion.RogueSpells.FireBattle, hero.Id, FireSpellCatalog.Get("F-P-U07"),
                    FireSpellTarget.Unit(hero.Id));
                ArtifactBattleState artifacts = new ArtifactBattleState(conversion);
                GridPosition destination = AllCells(conversion.Map).First(cell =>
                    ArtifactEngine.Preview(artifacts, hero.Id, ArtifactCatalog.ShieldBalancer, ArtifactTarget.At(cell),
                        ArtifactCatalog.ShieldBalancer.MaximumUses).CanCommit);
                ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.ShieldBalancer, ArtifactTarget.At(destination),
                    ArtifactCatalog.ShieldBalancer.MaximumUses);
                Assert.That(conversion.Map.GetTile(destination).Cover, Is.EqualTo(CoverType.Light), scenarioId);
            }
        }

        [Test]
        public void SpellLabSmokeStarterCanBeClearedByItsPresetCondenser()
        {
            CombatTestArenaScenario lab = CombatTestArenaScenarioCatalog.All.First(scenario => scenario.IsSkillTest);
            CombatState state = CombatTestArenaScenarioCatalog.Build(lab.Id).State;
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            GridPosition smokeCell = AllCells(state.Map).First(cell => ArtifactEngine.Preview(artifacts, hero.Id,
                ArtifactCatalog.NullVeil, ArtifactTarget.At(cell), ArtifactCatalog.NullVeil.MaximumUses).CanCommit);

            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.NullVeil, ArtifactTarget.At(smokeCell),
                ArtifactCatalog.NullVeil.MaximumUses);
            Assert.That(state.Map.GetTile(smokeCell).SmokeExpiresAt, Is.GreaterThan(state.CurrentTime));
            ArtifactPreview clear = ArtifactEngine.Preview(artifacts, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(smokeCell), ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(clear.CanCommit, Is.True, string.Join("/", clear.Failures));
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.HazardCondenser, ArtifactTarget.At(smokeCell),
                ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(state.Map.GetTile(smokeCell).SmokeExpiresAt, Is.Zero);
        }

        [Test]
        public void BossPresetCanBuyTheFourthActionAndCommitItsBurningFinisher()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_b01_core").State;
            CombatCommandExecutionService commands = new CombatCommandExecutionService();
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            UnitState hero = state.GetUnit("hero");
            UnitState core = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "core_overseer");

            CombatCommandExecutionResult approach = commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.Move(hero.Id, new GridPosition(4, 3)));
            Assert.That(approach.Accepted, Is.True, approach.RejectionReason);
            plans.Invalidate();
            CombatResolver.EndTurn(state, hero);
            while (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId != hero.Id)
            {
                UnitState enemy = state.GetUnit(state.ActiveUnitId);
                CombatCommand command = plans.GetExecutionCommand(state, enemy, hero);
                CombatCommandExecutionResult execution = commands.Execute(state, state.RogueSpells.FireBattle, command);
                Assert.That(execution.Accepted, Is.True, enemy.Id + ": " + execution.RejectionReason);
                plans.Invalidate();
                if (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId == enemy.Id)
                    CombatResolver.EndTurn(state, enemy);
            }

            Assert.That(state.ActiveUnitId, Is.EqualTo(hero.Id));
            Assert.That(hero.Position.ManhattanDistance(core.Position), Is.EqualTo(1));
            CombatCommandExecutionResult ignite = commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 1, core.Id));
            Assert.That(ignite.Accepted, Is.True, ignite.RejectionReason);
            Assert.That(core.HasStatus(StatusType.Burning), Is.True);
            Assert.That(hero.ActionPoints, Is.EqualTo(2));

            string ledgerInstanceId = state.RogueEquipment.ItemQuickbarInstanceIds[2];
            RogueTacticalItemInstance ledger = state.RogueEquipment.TacticalItem(ledgerInstanceId);
            Assert.That(ledger.DefinitionId, Is.EqualTo("G-T12"));
            int actionValueBefore = hero.ActionValue;
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.TurnLedger,
                ArtifactTarget.Unit(hero.Id, hero.Position), ledger.ChargesCurrent);
            Assert.That(ledger.Consume(), Is.True);
            Assert.That(hero.ActionPoints, Is.EqualTo(3));
            Assert.That(hero.ActionValue, Is.EqualTo(actionValueBefore - 8));

            int vitalityBefore = core.Health + core.Shield;
            CombatCommandExecutionResult finisher = commands.Execute(state, state.RogueSpells.FireBattle,
                CombatCommand.UseSkill(hero.Id, 7, core.Id));
            Assert.That(finisher.Accepted, Is.True, finisher.RejectionReason);
            Assert.That(hero.ActionPoints, Is.Zero);
            Assert.That(core.Health + core.Shield, Is.LessThan(vitalityBefore));
        }

        [Test]
        public void HighPressureScenariosExposeEliteBossAndChargeRuntimes()
        {
            CombatTestArenaScenario[] highPressure = CombatTestArenaScenarioCatalog.All
                .Where(scenario => scenario.IsHighPressure).ToArray();
            Assert.That(highPressure.Count(scenario => scenario.Level.IsElite), Is.EqualTo(3));
            Assert.That(highPressure.Count(scenario => scenario.Level.IsBoss), Is.EqualTo(1));

            CombatState pressure = CombatTestArenaScenarioCatalog.Build("arena_e03_pressure").State;
            Assert.That(pressure.ThreeMaterialPressure, Is.Not.Null);
            Assert.That(pressure.Units.Values.Any(unit => unit.EnemyArchetypeId == "breach_ram"), Is.True);
            UnitState ram = pressure.Units.Values.Single(unit => unit.EnemyArchetypeId == "breach_ram");
            UnitState pressureHero = pressure.GetUnit("hero");
            CombatCommand charge = pressure.ThreeMaterialPressure.ChooseEnemyCommand(pressure, ram, pressureHero);
            Assert.That(charge.Type, Is.EqualTo(CombatCommandType.BreachCharge));
            Assert.That(pressure.ThreeMaterialPressure.PresentIntent(pressure, ram, charge).DetailedText,
                Does.Contain("路线").And.Contain("上限 5"));
        }

        [Test]
        public void AcademyCoreBossExposesMaintenanceShieldAndReadablePhaseSwitch()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_b01_core").State;
            Assert.That(state.AcademyCoreBoss, Is.Not.Null);
            UnitState core = state.Units.Values.Single(unit => unit.EnemyArchetypeId == "core_overseer");
            UnitState hero = state.GetUnit("hero");

            CombatResolver.BeginTurn(state, core.Id);
            Assert.That(core.Shield, Is.EqualTo(8), "2 点首领基础回合盾加三条维护链各 2 点。 ");
            Assert.That(state.AcademyCoreBoss.PhaseFor(state, core), Is.EqualTo(1));
            EnemyTurnPlanBook phaseOnePlans = new EnemyTurnPlanBook();
            EnemyIntentPresentation phaseOne = phaseOnePlans.GetPublicIntent(state, core, hero);
            Assert.That(phaseOne.ActionName, Is.EqualTo("核心定向束"));
            Assert.That(phaseOne.DetailedText, Does.Contain("阶段一").And.Contain("3 条维护链"));

            UnitState phaseTwoHero = new UnitState("phase_two_hero", true, new GridPosition(0, 2));
            UnitState phaseTwoCore = new UnitState("phase_two_core", false, new GridPosition(2, 2));
            UnitState finalLink = new UnitState("final_link", false, new GridPosition(4, 4));
            EnemyArchetypes.Get("core_overseer").Apply(phaseTwoCore);
            EnemyArchetypes.Get("barrier_mender").Apply(finalLink);
            phaseTwoCore.ConfigureMana(8);
            CombatState phaseTwoState = new CombatState(new GridMap(5, 5),
                new[] { phaseTwoHero, phaseTwoCore, finalLink });
            phaseTwoState.ConfigureRuleset(CombatRuleset.Roguelite);
            phaseTwoState.AttachAcademyCoreBoss(new AcademyCoreBossRuntime());
            Assert.That(phaseTwoState.AcademyCoreBoss.PhaseFor(phaseTwoState, phaseTwoCore), Is.EqualTo(2));
            EnemyTurnPlanBook phaseTwoPlans = new EnemyTurnPlanBook();
            EnemyIntentPresentation phaseTwo = phaseTwoPlans.GetPublicIntent(phaseTwoState, phaseTwoCore, phaseTwoHero);
            Assert.That(phaseTwo.ActionName, Is.EqualTo("核心破势脉冲"));
            Assert.That(phaseTwo.DetailedText, Does.Contain("阶段二").And.Contain("1 条维护链"));
        }

        [Test]
        public void FireAndShallowWaterUseOneLaterEffectWinsContractAcrossSpellsAndArtifacts()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_n04_fire").State;
            GridPosition waterCell = new GridPosition(3, 3);
            FireBattleState fire = state.RogueSpells.FireBattle;
            Assert.That(state.Map.GetTile(waterCell).IsWater, Is.True);

            fire.CreateOrRefreshFireground(waterCell, 8, 3, "test-spell-fire");
            Assert.That(state.Map.GetTile(waterCell).IsWater, Is.False);
            Assert.That(fire.HasFireground(waterCell), Is.True);

            fire.CreateOrRefreshShallowWater(waterCell);
            Assert.That(state.Map.GetTile(waterCell).IsWater, Is.True);
            Assert.That(fire.HasFireground(waterCell), Is.False);

            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            ArtifactBattleState artifacts = new ArtifactBattleState(state);
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.Get("F-T01"), ArtifactTarget.At(waterCell));
            Assert.That(state.Map.GetTile(waterCell).IsWater, Is.False);
            Assert.That(fire.HasFireground(waterCell), Is.True, "法宝生成的火场必须进入术式共用的场地运行时。 ");

            GridPosition smokeCell = new GridPosition(2, 3);
            state.Map.GetTile(smokeCell).SmokeExpiresAt = 99;
            ArtifactEngine.Execute(artifacts, hero.Id, ArtifactCatalog.Get("G-T11"), ArtifactTarget.At(waterCell));
            Assert.That(fire.HasFireground(waterCell), Is.False);
            Assert.That(state.Map.GetTile(smokeCell).SmokeExpiresAt, Is.Zero);
        }

        [Test]
        public void ReactionPressurePreviewsFriendlyFireDeadZoneLimitAndResolvedPush()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_s01_reaction").State;
            Assert.That(state.PressureTest, Is.Not.Null);
            CombatResolver.BeginTurn(state, "hero");
            GridPosition riskyDestination = new GridPosition(3, 3);
            PressureReactionPreview preview = state.PressureTest.PreviewHeroMove(state, riskyDestination);
            Assert.That(preview.WillTrigger, Is.True);
            Assert.That(preview.SourceUnitId, Is.EqualTo("enemy_0"));
            Assert.That(preview.TargetUnitId, Is.EqualTo("enemy_1"));
            Assert.That(preview.FriendlyFire, Is.True);
            Assert.That(preview.Summary, Does.Contain("本回合限 1 次").And.Contain("敌方友伤").And.Contain("E4"));
            Assert.That(state.PressureTest.PreviewHeroMove(state, new GridPosition(6, 3)).WillTrigger, Is.False,
                "警戒者相邻格必须是明确死区。 ");

            UnitState intercepted = state.GetUnit("enemy_1");
            int healthBefore = intercepted.Health;
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(state,
                state.RogueSpells.FireBattle, CombatCommand.Move("hero", riskyDestination));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Execution.Results.Any(effect => effect.Kind == CombatEffectKind.DamageHealth &&
                effect.TargetUnitId == "enemy_1" && effect.AppliedAmount == 6), Is.True);
            Assert.That(result.Execution.Results.Any(effect => effect.Kind == CombatEffectKind.Move &&
                effect.TargetUnitId == "enemy_1" && effect.PositionAfter == new GridPosition(4, 3)), Is.True,
                "反应击退必须进入同一执行结果，供表现层播放。 ");
            Assert.That(intercepted.Health, Is.LessThan(healthBefore));
            Assert.That(intercepted.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(state.PressureTest.ReactionAvailable, Is.False);
            Assert.That(state.PressureTest.PreviewHeroMove(state, new GridPosition(3, 2)).WillTrigger, Is.False);
        }

        [Test]
        public void ProtectionPressureUsesVisibleDurabilityAndFailsOnlyWhenItReachesZero()
        {
            CombatState state = CombatTestArenaScenarioCatalog.Build("arena_s02_protect").State;
            Assert.That(state.PressureTest, Is.Not.Null);
            Assert.That(state.Objectives.OfType<ProtectionObjective>().Count(), Is.EqualTo(1));
            GridPosition protectedPosition = state.PressureTest.ProtectedPosition;
            TileState protectedTile = state.Map.GetTile(protectedPosition);
            Assert.That(protectedTile.Durability, Is.EqualTo(12));
            Assert.That(state.PressureTest.ProtectionSummary(state), Does.Contain("F4").And.Contain("耐久 12"));

            UnitState saboteur = state.GetUnit("enemy_0");
            UnitState hero = state.GetUnit("hero");
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatResolver.BeginTurn(state, saboteur.Id);
            CombatCommand first = plans.GetExecutionCommand(state, saboteur, hero);
            Assert.That(first.Type, Is.EqualTo(CombatCommandType.Interact));
            Assert.That(plans.GetPublicIntent(state, saboteur, hero).DetailedText,
                Does.Contain("耐久 12→6").And.Contain("归零时立即失败"));
            CombatResolver.Resolve(state, first);
            Assert.That(protectedTile.Durability, Is.EqualTo(6));
            Assert.That(state.IsDefeat, Is.False);

            plans.Invalidate();
            CombatResolver.BeginTurn(state, saboteur.Id);
            CombatResolver.Resolve(state, plans.GetExecutionCommand(state, saboteur, hero));
            Assert.That(protectedTile.Durability, Is.Zero);
            Assert.That(state.IsDefeat, Is.True);
            Assert.That(state.IsVictory, Is.False);
        }

        [Test]
        public void EfficiencyPressureRewardsFastWinsButNeverTurnsOvertimeIntoDefeat()
        {
            CombatState fast = CombatTestArenaScenarioCatalog.Build("arena_s03_efficiency").State;
            CombatResolver.BeginTurn(fast, "hero");
            Assert.That(fast.PressureTest.EfficiencySummary(fast), Does.Contain("限 3").And.Contain("当前第 1").And.Contain("仍可达成"));
            CombatEffectExecutor.Execute(fast, "hero", fast.Units.Values.Where(unit => !unit.IsHero)
                .Select(enemy => CombatEffect.DamageHealth(enemy.Id, enemy.Health)).ToArray());
            CombatResolver.BeginTurn(fast, "hero");
            Assert.That(fast.IsVictory, Is.True);
            Assert.That(fast.PressureTest.EfficiencyBonusEarned(fast), Is.True);
            Assert.That(CombatInformationPresenter.BuildOutcome(fast, false).Consequence, Does.Contain("效率奖励已达成"));

            CombatState slow = CombatTestArenaScenarioCatalog.Build("arena_s03_efficiency").State;
            for (int turn = 0; turn < 4; turn++) CombatResolver.BeginTurn(slow, "hero");
            Assert.That(slow.PressureTest.EfficiencySummary(slow), Does.Contain("已错过").And.Contain("正常胜利"));
            CombatEffectExecutor.Execute(slow, "hero", slow.Units.Values.Where(unit => !unit.IsHero)
                .Select(enemy => CombatEffect.DamageHealth(enemy.Id, enemy.Health)).ToArray());
            CombatResolver.BeginTurn(slow, "hero");
            Assert.That(slow.IsVictory, Is.True);
            Assert.That(slow.IsDefeat, Is.False);
            Assert.That(slow.PressureTest.EfficiencyBonusEarned(slow), Is.False);
            Assert.That(CombatInformationPresenter.BuildOutcome(slow, false).Consequence,
                Does.Contain("基础胜利与基础奖励不变"));
        }

        [Test]
        public void EveryScenarioCanSpendARealPresetSpellOnItsFirstTurn()
        {
            foreach (CombatTestArenaScenario scenario in CombatTestArenaScenarioCatalog.All)
            {
                CombatState state = CombatTestArenaScenarioCatalog.Build(scenario.Id).State;
                CombatResolver.BeginTurn(state, "hero");
                UnitState hero = state.GetUnit("hero");
                int actionPointsBefore = hero.ActionPoints;
                int chosenSlot = -1;
                CombatCommand chosenCommand = default;
                for (int slot = 0; slot < scenario.SpellIds.Count && chosenSlot < 0; slot++)
                {
                    string spellId = scenario.SpellIds[slot];
                    if (!spellId.StartsWith("F-P-"))
                    {
                        if (spellId == "BASE-AETHER-SHIELD")
                        {
                            chosenSlot = slot;
                            chosenCommand = CombatCommand.UseSkill(hero.Id, slot, hero.Id);
                        }
                        continue;
                    }
                    FireSpellDefinition spell = FireSpellCatalog.Get(spellId);
                    foreach (UnitState target in state.Units.Values)
                    {
                        FireSpellTarget candidate = FireSpellTarget.Unit(target.Id, CardinalDirection.East);
                        if (!FireSpellEngine.Preview(state.RogueSpells.FireBattle, hero.Id, spell, candidate).CanCommit) continue;
                        chosenSlot = slot;
                        chosenCommand = CombatCommand.UseSkill(hero.Id, slot, target.Id);
                        break;
                    }
                    for (int y = 0; y < state.Map.Height && chosenSlot < 0; y++)
                        for (int x = 0; x < state.Map.Width && chosenSlot < 0; x++)
                            foreach (CardinalDirection direction in new[] { CardinalDirection.East, CardinalDirection.North, CardinalDirection.West, CardinalDirection.South })
                            {
                                GridPosition cell = new GridPosition(x, y);
                                if (!FireSpellEngine.Preview(state.RogueSpells.FireBattle, hero.Id, spell, FireSpellTarget.At(cell, direction)).CanCommit) continue;
                                chosenSlot = slot;
                                chosenCommand = CombatCommand.UseSkillAt(hero.Id, slot, cell, direction);
                                break;
                            }
                }

                Assert.That(chosenSlot, Is.GreaterThanOrEqualTo(0), scenario.Id + " has no legal first-turn preset spell.");
                RogueSpellExecution execution = state.RogueSpells.ExecuteSlot(chosenSlot, chosenCommand);
                Assert.That(execution.Accepted, Is.True, scenario.Id);
                Assert.That(hero.ActionPoints, Is.LessThan(actionPointsBefore), scenario.Id);
            }
        }

        private static void AssertAllWalkableCellsConnected(GridMap map, GridPosition start, string scenarioId)
        {
            HashSet<GridPosition> visited = new HashSet<GridPosition> { start };
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(start);
            GridPosition[] directions =
            {
                new GridPosition(1, 0), new GridPosition(-1, 0),
                new GridPosition(0, 1), new GridPosition(0, -1)
            };
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                foreach (GridPosition direction in directions)
                {
                    GridPosition next = current + direction;
                    if (!map.IsInside(next) || map.IsBlocked(next) || !visited.Add(next)) continue;
                    frontier.Enqueue(next);
                }
            }

            int walkable = 0;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (!map.IsBlocked(new GridPosition(x, y))) walkable++;
            Assert.That(visited.Count, Is.EqualTo(walkable), scenarioId + " contains an unreachable walkable island.");
        }

        private static IEnumerable<GridPosition> AllCells(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    yield return new GridPosition(x, y);
        }
    }
}
