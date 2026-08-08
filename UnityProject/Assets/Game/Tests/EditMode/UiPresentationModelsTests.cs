using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class UiPresentationModelsTests
    {
        [Test]
        public void Versions_AdvanceOnlyMarkedArea()
        {
            var versions = new UiPresentationVersions();
            UiPresentationChange received = default;
            versions.Changed += change => received = change;

            versions.Mark(UiPresentationArea.MapResources);

            Assert.That(versions.Version(UiPresentationArea.MapResources), Is.EqualTo(1));
            Assert.That(versions.Version(UiPresentationArea.MapStructure), Is.EqualTo(0));
            Assert.That(received.Area, Is.EqualTo(UiPresentationArea.MapResources));
            Assert.That(received.Version, Is.EqualTo(1));
        }

        [Test]
        public void MapModel_IsSnapshotNotLiveRunReference()
        {
            var run = new RogueliteMapRun(123);
            RogueliteMapPresentationModel before = RogueliteMapPresentationModel.From(run);

            run.SelectNode("rail_patrol");
            RogueliteMapPresentationModel after = RogueliteMapPresentationModel.From(run);

            Assert.That(before.CurrentNodeId, Is.EqualTo("start"));
            Assert.That(after.CurrentNodeId, Is.EqualTo("rail_patrol"));
            Assert.That(before.Equals(after), Is.False);
        }

        [Test]
        public void SettlementModel_ChangesWhenRewardStateOpens()
        {
            var run = new RogueliteMapRun(321);
            run.SelectNode("rail_patrol");
            SettlementPresentationModel before = SettlementPresentationModel.From(run);

            run.CompleteCurrentCombat();
            SettlementPresentationModel after = SettlementPresentationModel.From(run);

            Assert.That(before.Visible, Is.False);
            Assert.That(after.Visible, Is.True);
            Assert.That(after.RewardKey, Is.Not.Empty);
        }
    }
}
