using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteDeveloperRunTests
    {
        [Test]
        public void Catalog_OpensOnlyEliminationAndDestructionSandboxTemplates()
        {
            Assert.That(RogueliteDeveloperCatalog.OpenSandboxTemplates.Select(template => template.Type), Is.EquivalentTo(new[] { CombatObjectiveType.Elimination, CombatObjectiveType.Destruction }));
            Assert.That(RogueliteDeveloperCatalog.FindMission("factory_breach").ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void StoryRun_AdvancesOnlyWhenItsMissionIsCompleted()
        {
            var run = new RogueliteDeveloperRun(RogueliteStoryCatalog.CreateDefault(100));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("dead_signal"));
            run.Complete("signal secured");
            Assert.That(run.Package.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(run.CurrentMission.ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void SandboxRun_DoesNotAdvanceStoryPackage()
        {
            var run = new RogueliteDeveloperRun("elimination_rail", 200);
            run.Complete("sandbox result");
            Assert.That(run.Package.CurrentMissionIndex, Is.EqualTo(0));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("sandbox_elimination"));
        }

        [Test]
        public void RogueliteSaveManager_IsolatedAndRoundTripsPackageOnly()
        {
            var save = new RogueliteSaveManager();
            var package = RogueliteStoryCatalog.CreateDefault(300);
            package.CompleteCurrentMission("first"); save.Save(package);
            Assert.That(save.HasSave("iron_echoes"), Is.True);
            Assert.That(save.Load("iron_echoes").CurrentMissionId, Is.EqualTo("factory_breach"));
            save.Delete("iron_echoes"); Assert.That(save.HasSave("iron_echoes"), Is.False);
        }

        [Test]
        public void DebugOutcome_ProducesBothObjectiveOutcomesWithoutChangingSnapshotData()
        {
            var map = new GridMap(2, 2);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            var victory = new CombatState(map, new[] { hero, enemy }, new CombatObjective[] { new EliminationObjective() });
            victory.ResolveDebugOutcome(true); Assert.That(victory.IsVictory, Is.True);
            var defeat = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East), new UnitState("enemy", false, new GridPosition(1, 0), Facing.West) });
            defeat.ResolveDebugOutcome(false); Assert.That(defeat.IsDefeat, Is.True);
        }
    }
}
