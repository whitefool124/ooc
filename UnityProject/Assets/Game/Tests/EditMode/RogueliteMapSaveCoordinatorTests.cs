using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteMapSaveCoordinatorTests
    {
        [Test]
        public void NewRun_MustPersistBeforeItCanStart()
        {
            MemoryStore store = new MemoryStore { FailWrites = true };
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);

            RogueliteMapStartResult result = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Universal, 301);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Run, Is.Null);
            Assert.That(result.FailureMessage, Is.EqualTo(RogueliteMapSaveCoordinator.NewRunSaveFailure));
            Assert.That(coordinator.LastSaveSucceeded, Is.False);
        }

        [Test]
        public void FormalFrontEnd_CreatesAnAcknowledgedOriginInTheFirstVerifiedWrite()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);

            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Universal, 314, acknowledgeOriginOnCreate: true);

            Assert.That(created.Success, Is.True);
            Assert.That(created.Run.FirstRunExperience.Origin.Acknowledged, Is.True);
            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 0);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.FirstRunExperience.Origin.Acknowledged, Is.True);
            Assert.That(loaded.Run.Seed, Is.EqualTo(314));
        }

        [Test]
        public void ReplacementWriteFailure_KeepsThePreviousValidRun()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 312).Success, Is.True);
            string original = store.Values[RogueliteSaveGateway.MapRunKey];
            Assert.That(coordinator.PrepareSlotForReplacement(), Is.True);
            store.FailWrites = true;

            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 313).Success, Is.False);
            Assert.That(coordinator.LastSaveSucceeded, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            store.FailWrites = false;
            Assert.That(coordinator.TryStart(true, FireRogueliteStarterCatalog.Universal, 999).Run.Seed, Is.EqualTo(312));
            Assert.That(coordinator.LastSaveSucceeded, Is.True);
        }

        [Test]
        public void ContinueMissing_DoesNotCreateOrOverwriteData()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);

            RogueliteMapStartResult result = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 302);

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureMessage, Is.EqualTo("没有可以继续的存档。请开始新游戏。"));
            Assert.That(store.Values, Is.Empty);
        }

        [TestCase(FireRogueliteStarterCatalog.Melee)]
        [TestCase(FireRogueliteStarterCatalog.Universal)]
        [TestCase(FireRogueliteStarterCatalog.Ranged)]
        public void ValidNewRun_CanRoundTripThroughContinueAsFixedFirstRun(string starterId)
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                starterId, 303);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);

            Assert.That(created.Success, Is.True);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.Seed, Is.EqualTo(303));
            Assert.That(loaded.Run.StarterId, Is.EqualTo(FireRogueliteStarterCatalog.Universal));
            Assert.That(loaded.Run.IsFirstRunExperience, Is.True);
            Assert.That(loaded.Run.CurrentNodeId, Is.EqualTo("O"));
        }

        [Test]
        public void FirstBattleVictorySettlement_SavesAndContinuesAtCompletedCombat()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 307);
            Assert.That(created.Success, Is.True);
            created.Run.AcknowledgeFirstRunOrigin();
            created.Run.SelectNode("B1");

            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState combat = new CombatState(new GridMap(3, 2), new[] { hero, enemy },
                new CombatObjective[] { new EliminationObjective() });
            combat.ResolveDebugOutcome(true);

            Assert.That(RogueliteCombatSettlement.TrySettleVictory(created.Run, combat), Is.True);
            Assert.That(coordinator.Save(created.Run), Is.True);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.CurrentNodeId, Is.EqualTo("B1"));
            Assert.That(loaded.Run.CompletedNodes, Does.Contain("B1"));
            Assert.That(loaded.Run.AwaitingReward, Is.True);
        }

        [Test]
        public void PendingFirstRunReward_FailedWriteCanRetryWithoutResolvingTheReward()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapRun run = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Universal, 308).Run;
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            Assert.That(coordinator.Save(run), Is.True);

            run.CompleteCurrentCombat();
            string[] choices = run.CurrentRewards.Select(reward => reward.Id).ToArray();
            store.FailWrites = true;
            Assert.That(coordinator.Save(run), Is.False);
            Assert.That(coordinator.LastSaveSucceeded, Is.False);
            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(run.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(choices));

            store.FailWrites = false;
            Assert.That(coordinator.Save(run), Is.True);
            Assert.That(coordinator.LastSaveSucceeded, Is.True);
            RogueliteMapRun restored = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999).Run;
            Assert.That(restored.AwaitingReward, Is.True);
            Assert.That(restored.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(choices));
            Assert.That(restored.ClaimedRewards, Is.Empty);
        }

        [Test]
        public void CombatWriteFailure_RetryAndContinueReplayTheSameTurnWithoutRestarting()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapRun run = coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 7646).Run;
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            run.BeginCombatJournal();
            Assert.That(coordinator.Save(run), Is.True);
            string beforeAction = store.Values[RogueliteSaveGateway.MapRunKey];

            CombatSceneSessionBuilder builder = new CombatSceneSessionBuilder();
            CombatState live = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(live);
            CombatCommand endTurn = CombatCommand.EndTurn("hero");
            Assert.That(new CombatCommandExecutionService().Execute(
                live, new FireBattleState(live), endTurn, true).Accepted, Is.True);
            run.AppendCombatJournal(CombatJournalEntry.Accepted(endTurn));

            store.FailWrites = true;
            Assert.That(coordinator.Save(run), Is.False);
            Assert.That(coordinator.LastSaveSucceeded, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(beforeAction));
            store.FailWrites = false;
            Assert.That(coordinator.Save(run), Is.True);

            RogueliteMapRun reloaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999).Run;
            Assert.That(reloaded.HasActiveCombat, Is.True);
            Assert.That(reloaded.CombatJournalRows.Count, Is.EqualTo(1));
            CombatState restored = builder.Build(reloaded, null, Array.Empty<CombatSceneMarker>()).State;
            CombatResolver.AdvanceToNextTurn(restored);
            CombatJournalReplayer.ReplayAfterActivation(restored, reloaded.CombatJournalRows);

            Assert.That(restored.ActiveUnitId, Is.EqualTo(live.ActiveUnitId));
            Assert.That(restored.TurnSequence, Is.EqualTo(live.TurnSequence));
            Assert.That(restored.GetUnit("hero").ActionPoints, Is.EqualTo(live.GetUnit("hero").ActionPoints));
            Assert.That(restored.GetUnit("hero").Health, Is.EqualTo(live.GetUnit("hero").Health));
            Assert.That(reloaded.CompletedNodes, Does.Not.Contain("B1"));
            Assert.That(reloaded.AwaitingReward, Is.False);
        }

        [Test]
        public void FirstBattleNormalCommands_VictorySettlementPersistsAndReloads()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 911);
            created.Run.AcknowledgeFirstRunOrigin();
            created.Run.SelectNode("B1");

            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                created.Run, null, Array.Empty<CombatSceneMarker>());
            int commandCount = WinRainLanternCourtWithNormalCommands(build.State);

            Assert.That(build.State.IsVictory, Is.True);
            Assert.That(commandCount, Is.LessThan(100));
            CombatOutcomeSettlement settlement = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Victory, build.State, created.Run, null);
            Assert.That(settlement.Persistence, Is.EqualTo(CombatOutcomePersistence.MapRun));
            Assert.That(coordinator.Save(created.Run), Is.True);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.CompletedNodes, Does.Contain("B1"));
            Assert.That(loaded.Run.AwaitingReward, Is.True);
            Assert.That(loaded.Run.CurrentFirstRunRewardIds, Is.Not.Empty);
        }

        [Test]
        public void FirstBattleNormalDefeat_LeavesThePreBattleSaveRestartable()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 912);
            created.Run.AcknowledgeFirstRunOrigin();
            created.Run.SelectNode("B1");
            Assert.That(coordinator.Save(created.Run), Is.True, "战斗开始前先写入可重试快照。");

            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                created.Run, null, Array.Empty<CombatSceneMarker>());
            int commandCount = LoseRainLanternCourtByEndingHeroTurns(build.State);

            Assert.That(build.State.IsDefeat, Is.True);
            Assert.That(commandCount, Is.LessThan(300));
            CombatOutcomeSettlement settlement = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Defeat, build.State, created.Run, null);
            Assert.That(settlement.Persistence, Is.EqualTo(CombatOutcomePersistence.None));
            Assert.That(created.Run.CompletedNodes, Does.Not.Contain("B1"));
            Assert.That(created.Run.AwaitingReward, Is.False);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.CurrentNodeId, Is.EqualTo("B1"));
            Assert.That(loaded.Run.CompletedNodes, Does.Not.Contain("B1"));
            Assert.That(loaded.Run.AwaitingReward, Is.False);
        }

        [TestCase(false, "death", 0)]
        [TestCase(true, "abandon", UnitState.HeroBaseHealth)]
        public void FirstBattleFailure_OnlyClosesAfterConfirmedSettlement(bool abandoned, string reason, int health)
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapRun run = coordinator.TryStart(false, FireRogueliteStarterCatalog.Melee, 913).Run;
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            Assert.That(coordinator.Save(run), Is.True);

            run.CloseAsFailure(abandoned);
            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.CompletedNodes, Does.Not.Contain("B1"));
            Assert.That(coordinator.Save(run), Is.True);

            RogueliteMapStartResult loaded = coordinator.TryStart(true, FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.RunEndReason, Is.EqualTo(reason));
            Assert.That(loaded.Run.CurrentHealth, Is.EqualTo(health));
            Assert.That(loaded.Run.IsComplete, Is.True);
            Assert.That(loaded.Run.AwaitingReward, Is.False);
        }

        [Test]
        public void ReplacingAValidRun_ResetsAllRunScopedResourcesInsteadOfReusingTheActiveDto()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult first = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Ranged, 305);
            Assert.That(first.Success, Is.True);

            first.Run.AcknowledgeFirstRunOrigin();
            first.Run.SelectNode("B1");
            first.Run.CompleteCurrentCombat();
            first.Run.ClaimReward(first.Run.CurrentFirstRunRewardIds[0]);
            Assert.That(coordinator.Save(first.Run), Is.True);

            RogueliteMapStartResult replacement = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 306);

            Assert.That(replacement.Success, Is.True);
            Assert.That(replacement.Run.Seed, Is.EqualTo(306));
            Assert.That(replacement.Run.CurrentNodeId, Is.EqualTo("O"));
            Assert.That(replacement.Run.Gold, Is.EqualTo(8));
            Assert.That(replacement.Run.StageContribution, Is.Zero);
            Assert.That(replacement.Run.StageTime, Is.Zero);
            Assert.That(replacement.Run.AcademyProgress, Is.Zero);
            Assert.That(replacement.Run.FirstRunExperience.Origin.Acknowledged, Is.False);
            Assert.That(replacement.Run.CompletedNodes, Is.Empty);
        }

        [Test]
        public void CorruptSlot_RequiresExplicitReplacementPreparation()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "broken";
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            Assert.That(coordinator.TryStart(true, FireRogueliteStarterCatalog.Universal, 304).Success, Is.False);

            Assert.That(coordinator.PrepareSlotForReplacement(), Is.True);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("broken"));
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)],
                Is.EqualTo("broken"));

            store.FailWrites = true;
            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 305).Success, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo("broken"));
            store.FailWrites = false;
            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 305).Success, Is.True);
        }

        [Test]
        public void ProtectedValidSlot_CannotContinueAndRequiresExplicitReplacement()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 310).Success, Is.True);
            string original = store.Values[RogueliteSaveGateway.MapRunKey];
            store.Values[RogueliteSaveGateway.WriteLockKey(RogueliteSaveGateway.MapRunKey)] = "protected-v1";
            store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)] = "failed-write";

            RogueliteMapStartResult continued = coordinator.TryStart(true, FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(continued.Success, Is.False);
            Assert.That(continued.FailureMessage, Does.Contain("写入保护"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            Assert.That(coordinator.IsWriteProtected, Is.True);

            Assert.That(coordinator.PrepareSlotForReplacement(), Is.True);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            Assert.That(coordinator.IsWriteProtected, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo("failed-write"));
            Assert.That(coordinator.TryStart(false, FireRogueliteStarterCatalog.Universal, 311).Success, Is.True);
        }

        private static RogueliteMapSaveCoordinator Coordinator(MemoryStore store) =>
            new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));

        private static int WinRainLanternCourtWithNormalCommands(CombatState state)
        {
            UnitState hero = state.GetUnit("hero");
            bool victorShieldCast = false;
            int commands = 0;
            CombatResolver.BeginTurn(state, hero.Id);
            while (!state.IsVictory && !state.IsDefeat && commands < 100)
            {
                UnitState unit = state.GetUnit(state.ActiveUnitId);
                if (unit == null || !unit.IsAlive)
                {
                    CombatResolver.AdvanceToNextTurn(state);
                    continue;
                }
                if (unit.ActionPoints <= 0)
                {
                    CombatResolver.EndTurn(state, unit);
                    commands++;
                    continue;
                }
                if (!unit.IsHero)
                {
                    CombatCommand enemyCommand = new EnemyTurnPlanBook().GetExecutionCommand(state, unit, hero);
                    if (enemyCommand.Type == CombatCommandType.EndTurn) CombatResolver.EndTurn(state, unit);
                    else CombatResolver.Resolve(state, enemyCommand);
                    commands++;
                    continue;
                }

                if (!victorShieldCast)
                {
                    int slot = Enumerable.Range(0, Roguelite.RogueRuntimeConstants.SpellSlotCount)
                        .First(index => state.RogueSpells.DefinitionAtSlot(index)?.DefinitionId ==
                            "BASE-AETHER-SHIELD");
                    CombatResolver.Resolve(state, CombatCommand.UseSkill(unit.Id, slot, string.Empty));
                    victorShieldCast = true;
                    commands++;
                    continue;
                }

                UnitState target = state.Units.Values.Where(value => value.IsAlive && !value.IsHero)
                    .OrderBy(value => value.Position.ManhattanDistance(unit.Position))
                    .ThenBy(value => value.Id, StringComparer.Ordinal).FirstOrDefault();
                if (target == null) break;
                int distance = unit.Position.ManhattanDistance(target.Position);
                CombatCommand command;
                WeaponDefinition weapon = unit.MainHand;
                if (distance >= weapon.MinimumRange && distance <= weapon.Range &&
                    CombatResolver.PreviewAttack(state, unit.Id, target.Id, false).HasLineOfSight)
                    command = CombatCommand.Attack(unit.Id, target.Id);
                else
                {
                    IReadOnlyList<GridPosition> path = new[]
                        {
                            new GridPosition(0, 1), new GridPosition(1, 0),
                            new GridPosition(0, -1), new GridPosition(-1, 0)
                        }
                        .Select(offset => target.Position + offset)
                        .Where(position => state.Map.IsInside(position) && !state.Map.IsBlocked(position) &&
                            !state.IsOccupied(position, unit.Id))
                        .OrderBy(position => position.ManhattanDistance(unit.Position))
                        .Select(position => CombatMovementQuery.FindPath(state, unit, position))
                        .FirstOrDefault(candidate => candidate.Count > 1);
                    command = path == null ? CombatCommand.EndTurn(unit.Id) :
                        CombatCommand.Move(unit.Id, path[path.Count - 1]);
                }
                if (command.Type == CombatCommandType.EndTurn) CombatResolver.EndTurn(state, unit);
                else CombatResolver.Resolve(state, command);
                commands++;
            }
            return commands;
        }

        private static int LoseRainLanternCourtByEndingHeroTurns(CombatState state)
        {
            UnitState hero = state.GetUnit("hero");
            int commands = 0;
            CombatResolver.BeginTurn(state, hero.Id);
            while (!state.IsVictory && !state.IsDefeat && commands < 300)
            {
                UnitState unit = state.GetUnit(state.ActiveUnitId);
                if (unit == null || !unit.IsAlive)
                {
                    CombatResolver.AdvanceToNextTurn(state);
                    continue;
                }
                if (unit.IsHero)
                {
                    CombatResolver.EndTurn(state, unit);
                    commands++;
                    continue;
                }

                CombatCommand command = new EnemyTurnPlanBook().GetExecutionCommand(state, unit, hero);
                if (command.Type != CombatCommandType.EndTurn) CombatResolver.Resolve(state, command);
                if (!state.IsVictory && !state.IsDefeat && state.ActiveUnitId == unit.Id)
                    CombatResolver.EndTurn(state, unit);
                commands++;
            }
            return commands;
        }

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
            public bool FailWrites { get; set; }
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") =>
                Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value)
            {
                if (FailWrites) throw new InvalidOperationException("write failed");
                Values[key] = value;
            }
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }
    }
}
