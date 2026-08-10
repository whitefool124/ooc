using NUnit.Framework;
using System.Linq;

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

        [Test]
        public void CombatHudModel_ChangesWhenQuickbarInstanceUsesChange()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[]
            {
                new UnitState("hero", true, new GridPosition(0, 0), Facing.East)
            });
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.AddFirstFit(new ItemInstance("artifact", "F-T01", 0, 2)).Success, Is.True);
            state.ConfigureItemInventory(inventory, new[] { "artifact" });
            CombatResolver.BeginTurn(state, "hero");
            CombatHudPresentationModel before = CombatHudPresentationModel.From(state, "技能1", null, false);

            Assert.That(state.ConsumeInventoryItem("artifact"), Is.True);
            CombatHudPresentationModel after = CombatHudPresentationModel.From(state, "技能1", null, false);

            Assert.That(before.Equals(after), Is.False);
        }

        [Test]
        public void CombatTurnTrack_OrdersLivingUnitsAndMarksTheCurrentActor()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var earlyEnemy = new UnitState("enemy_early", false, new GridPosition(1, 0), Facing.West);
            var lateEnemy = new UnitState("enemy_late", false, new GridPosition(2, 0), Facing.West);
            var state = new CombatState(new GridMap(4, 4), new[] { hero, earlyEnemy, lateEnemy });
            CombatResolver.BeginTurn(state, "hero");

            CombatTurnTrackEntry[] track = CombatTurnTrackPresentation.Build(state, 5).ToArray();

            Assert.That(track.Select(entry => entry.UnitId), Is.EqualTo(new[] { "hero", "enemy_early", "enemy_late" }));
            Assert.That(track.Select(entry => entry.Order), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(track.Single(entry => entry.IsActive).UnitId, Is.EqualTo("hero"));
            Assert.That(track.Single(entry => entry.UnitId == "hero").IsHero, Is.True);
            Assert.That(track.All(entry => entry.VitalityText.Contains("生命")), Is.True);
        }

        [Test]
        public void CombatTurnTrack_RespectsVisibleSlotLimit()
        {
            UnitState[] units = Enumerable.Range(0, 7)
                .Select(index => new UnitState("unit_" + index, index == 0, new GridPosition(index, 0), Facing.East))
                .ToArray();
            var state = new CombatState(new GridMap(8, 2), units);

            Assert.That(CombatTurnTrackPresentation.Build(state, 5).Count, Is.EqualTo(5));
            Assert.That(CombatTurnTrackPresentation.Build(state, 0), Is.Empty);
        }
    }
}
