using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatOutcomeSettlementCoordinatorTests
    {
        [Test]
        public void Process_MapVictorySettlesOnceAndRequestsMapPersistence()
        {
            RogueliteMapRun run = new RogueliteMapRun(2211);
            run.SelectNode("rail_patrol");
            CombatState combat = Outcome(true);
            CombatOutcomeSettlementCoordinator coordinator = new CombatOutcomeSettlementCoordinator();

            CombatOutcomeSettlement first = coordinator.Process(CombatFlowPhase.Victory, combat, run, null);
            CombatOutcomeSettlement duplicate = coordinator.Process(CombatFlowPhase.Victory, combat, run, null);

            Assert.That(first.HandledNow, Is.True);
            Assert.That(first.Persistence, Is.EqualTo(CombatOutcomePersistence.MapRun));
            Assert.That(first.RefreshSettlement, Is.True);
            Assert.That(run.CompletedNodes, Does.Contain("rail_patrol"));
            Assert.That(duplicate.HandledNow, Is.False);
        }

        [Test]
        public void Process_DefeatDoesNotMutateOrPersistMapRun()
        {
            RogueliteMapRun run = new RogueliteMapRun(2212);
            string before = run.ToJson();

            CombatOutcomeSettlement result = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Defeat, Outcome(false), run, null);

            Assert.That(result.HandledNow, Is.True);
            Assert.That(result.Victory, Is.False);
            Assert.That(result.Persistence, Is.EqualTo(CombatOutcomePersistence.None));
            Assert.That(run.ToJson(), Is.EqualTo(before));
        }

        [Test]
        public void Process_TemplateSandboxPlaysVictoryWithoutCompletingOrSavingStory()
        {
            RogueliteDeveloperRun run = new RogueliteDeveloperRun("elimination_rail", 2213);
            int missionIndex = run.Package.CurrentMissionIndex;

            CombatOutcomeSettlement result = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Victory, Outcome(true), null, run);

            Assert.That(result.HandledNow, Is.True);
            Assert.That(result.Victory, Is.True);
            Assert.That(result.Persistence, Is.EqualTo(CombatOutcomePersistence.None));
            Assert.That(run.Package.CurrentMissionIndex, Is.EqualTo(missionIndex));
        }

        [Test]
        public void Process_ShortAndStoryRunsReturnTheirOwnPersistencePorts()
        {
            ShortRogueliteRun shortState = new ShortRogueliteRun(2214);
            RogueliteDeveloperRun shortRun = new RogueliteDeveloperRun(shortState);
            RogueliteDeveloperRun storyRun = new RogueliteDeveloperRun(RogueliteStoryCatalog.CreateDefault(2215));

            CombatOutcomeSettlement shortResult = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Victory, Outcome(true), null, shortRun);
            CombatOutcomeSettlement storyResult = new CombatOutcomeSettlementCoordinator().Process(
                CombatFlowPhase.Victory, Outcome(true), null, storyRun);

            Assert.That(shortResult.Persistence, Is.EqualTo(CombatOutcomePersistence.ShortRun));
            Assert.That(shortState.Phase, Is.EqualTo(ShortRoguelitePhase.Event));
            Assert.That(storyResult.Persistence, Is.EqualTo(CombatOutcomePersistence.Story));
            Assert.That(storyRun.Package.CurrentMissionIndex, Is.EqualTo(1));
        }

        [Test]
        public void Reset_AllowsTheNextCombatOutcomeToBeHandled()
        {
            CombatOutcomeSettlementCoordinator coordinator = new CombatOutcomeSettlementCoordinator();
            coordinator.Process(CombatFlowPhase.Defeat, Outcome(false), null, null);

            coordinator.Reset();
            CombatOutcomeSettlement next = coordinator.Process(CombatFlowPhase.Defeat, Outcome(false), null, null);

            Assert.That(next.HandledNow, Is.True);
        }

        private static CombatState Outcome(bool victory)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState combat = new CombatState(new GridMap(3, 2), new[] { hero, enemy },
                new CombatObjective[] { new EliminationObjective() });
            combat.ResolveDebugOutcome(victory);
            return combat;
        }
    }
}
