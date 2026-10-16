using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatIntentChangeTrackerTests
    {
        [Test]
        public void PublicPlanChange_FlashesOnlyAfterBaselineAndExpiresWithoutChangingCombat()
        {
            var hero = new UnitState("hero", true, new GridPosition(4, 1));
            var enemy = new UnitState("enemy", false, new GridPosition(0, 1));
            var state = new CombatState(new GridMap(6, 3), new[] { hero, enemy });
            var first = CombatInformationPresenter.BuildEnemyIntent(state, enemy,
                CombatCommand.Move(enemy.Id, new GridPosition(2, 1)));
            var next = CombatInformationPresenter.BuildEnemyIntent(state, enemy,
                CombatCommand.Move(enemy.Id, new GridPosition(1, 1)));
            var tracker = new CombatIntentChangeTracker();
            Assert.That(tracker.Observe(enemy.Id, first, 10f), Is.False);
            Assert.That(tracker.Observe(enemy.Id, first, 11f), Is.False);
            Assert.That(tracker.Observe(enemy.Id, next, 12f), Is.True);
            Assert.That(tracker.Age(enemy.Id, 12.2f), Is.LessThan(CombatIntentChangeTracker.Duration));
            Assert.That(tracker.Observe(enemy.Id, next, 12.3f), Is.False);
            Assert.That(tracker.Age(enemy.Id, 13f), Is.GreaterThanOrEqualTo(CombatIntentChangeTracker.Duration));
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(0, 1)));
            tracker.Clear();
            Assert.That(tracker.Observe(enemy.Id, next, 20f), Is.False);
        }
    }
}
