using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatTutorialPolicyTests
    {
        [Test]
        public void MovingAndOpeningTheMoveModeAreDifferentCompletionConditions()
        {
            Assert.That(CombatTutorialPolicy.Completes("B1-04", false, CombatCommandType.Move), Is.False);
            Assert.That(CombatTutorialPolicy.Completes("B1-04", true, CombatCommandType.EndTurn), Is.False);
            Assert.That(CombatTutorialPolicy.Completes("B1-04", true, CombatCommandType.UseSkill), Is.False);
            Assert.That(CombatTutorialPolicy.Completes("B1-04", true, CombatCommandType.Move), Is.True);
        }

        [Test]
        public void ExplanationsBlockCommandsAndEnemyTurnWaitsForItsOwnCheckpoint()
        {
            foreach (CombatCommandType type in System.Enum.GetValues(typeof(CombatCommandType)))
                Assert.That(CombatTutorialPolicy.Allows("B1-06", false, type), Is.False);
            var completed = new HashSet<string> { "B1-01", "B1-02", "B1-03" };
            Assert.That(CombatTutorialPolicy.Next(completed, true, "移动", 0, false, false), Is.Empty);
            Assert.That(CombatTutorialPolicy.Next(completed, false, "移动", 0, true, false), Is.EqualTo("B1-06"));
        }

        [Test]
        public void CancelledMovementCanBeDeferredWithoutCountingAsCompleted()
        {
            var completed = new HashSet<string> { "B1-01", "B1-02", "B1-03" };
            Assert.That(CombatTutorialPolicy.Next(completed, true, "移动", 4, false, true), Is.Empty);
            Assert.That(CombatTutorialPolicy.Next(completed, true, "移动", 4, false, false), Is.EqualTo("B1-04"));
            Assert.That(completed.Contains("B1-04"), Is.False);
        }

        [TestCase(CombatCommandType.Attack, true)]
        [TestCase(CombatCommandType.UseSkill, true)]
        [TestCase(CombatCommandType.Move, false)]
        [TestCase(CombatCommandType.EndTurn, false)]
        [TestCase(CombatCommandType.UseInventoryItem, false)]
        public void PreviewTeachingAllowsOnlyTheActionBeingTaught(CombatCommandType type, bool allowed)
            => Assert.That(CombatTutorialPolicy.Completes("B1-05", true, type), Is.EqualTo(allowed));

        [Test]
        public void TutorialCursorAndCompletedIdsRoundTripWithTheFirstRunSnapshot()
        {
            var state = FirstRunExperienceCatalog.CreatePhaseA();
            state.CompletedTutorialIds.UnionWith(new[] { "B1-01", "B1-02", "B1-03" });
            state.PendingTutorialId = "B1-05";
            state.TutorialActionReady = true;
            state.PendingTutorialAction = "技能2";
            var loaded = FirstRunExperienceCodec.Deserialize(FirstRunExperienceCodec.Serialize(state));
            Assert.That(loaded.CompletedTutorialIds, Is.EquivalentTo(state.CompletedTutorialIds));
            Assert.That(loaded.PendingTutorialId, Is.EqualTo("B1-05"));
            Assert.That(loaded.TutorialActionReady, Is.True);
            Assert.That(loaded.PendingTutorialAction, Is.EqualTo("技能2"));
            Assert.That(loaded.Nodes.Select(n => n.Id), Is.EquivalentTo(state.Nodes.Select(n => n.Id)));
        }

        [Test]
        public void EarlierTutorialRowsRetainTheirCursorWithoutAnActionField()
        {
            var state = FirstRunExperienceCatalog.CreatePhaseA();
            state.CompletedTutorialIds.Add("B1-01");
            state.PendingTutorialId = "B1-04";
            state.TutorialActionReady = true;
            string old = string.Join("\n", FirstRunExperienceCodec.Serialize(state).Split('\n')
                .Select(row => row.StartsWith("T\t") ? row.Substring(0, row.LastIndexOf('\t')) : row));
            var loaded = FirstRunExperienceCodec.Deserialize(old);
            Assert.That(loaded.CompletedTutorialIds, Does.Contain("B1-01"));
            Assert.That(loaded.PendingTutorialId, Is.EqualTo("B1-04"));
            Assert.That(loaded.TutorialActionReady, Is.True);
            Assert.That(loaded.PendingTutorialAction, Is.Empty);
        }

        [Test]
        public void OlderSnapshotsWithoutTutorialRowsStillLoad()
        {
            var state = FirstRunExperienceCatalog.CreatePhaseA();
            string old = string.Join("\n", FirstRunExperienceCodec.Serialize(state).Split('\n').Where(row => !row.StartsWith("T\t")));
            var loaded = FirstRunExperienceCodec.Deserialize(old);
            Assert.That(loaded.CompletedTutorialIds, Is.Empty);
            Assert.That(loaded.PendingTutorialId, Is.Empty);
            Assert.That(loaded.TutorialActionReady, Is.False);
            Assert.That(loaded.Nodes.Count, Is.EqualTo(11));
        }
    }
}
