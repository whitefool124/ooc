using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatActionOrderPresentationTests
    {
        private static void SetValue(UnitState unit, int value) => typeof(UnitState).GetMethod("SetActionValue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unit, new object[] { value });
        private static CombatState State(out UnitState hero, out UnitState slow, out UnitState fast)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0)) { Speed = 6 };
            slow = new UnitState("slow", false, new GridPosition(1, 0)) { Speed = 1 };
            fast = new UnitState("fast", false, new GridPosition(2, 0)) { Speed = 10 };
            var state = new CombatState(new GridMap(4, 2), new[] { hero, slow, fast });
            CombatResolver.BeginTurn(state, hero.Id);
            SetValue(slow, 90);
            SetValue(fast, 60);
            return state;
        }

        [Test]
        public void FasterReadyArrivalBeatsHigherUnreadyValue_WithoutChangingState()
        {
            var state = State(out var hero, out var slow, out var fast);
            int time = state.CurrentTime;
            Assert.That(CombatActionOrderPresentation.Build(state).Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "fast", "slow" }));
            Assert.That(slow.ActionValue, Is.EqualTo(90));
            Assert.That(fast.ActionValue, Is.EqualTo(60));
            Assert.That(state.CurrentTime, Is.EqualTo(time));
            Assert.That(state.ActiveUnitId, Is.EqualTo(hero.Id));
        }

        [Test]
        public void LegalDelayReordersForecast_AndCancellationRestoresIt()
        {
            var state = State(out var hero, out var slow, out var fast);
            SetValue(fast, 95);
            slow.Speed = 4;
            var preview = new CombatActionPreview("delay", "unit", "", "", 1, "", actionValueTargetId: fast.Id, actionValueDelay: 90);
            Assert.That(CombatActionOrderPresentation.Build(state, preview).Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "slow", "fast" }));
            Assert.That(CombatActionOrderPresentation.Build(state).Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "fast", "slow" }));
            Assert.That(fast.ActionValue, Is.EqualTo(95));
        }

        [Test]
        public void ReadyTiesUseValueSpeedAndFixedOrder_AndKeepActorUnique()
        {
            var state = State(out var hero, out var slow, out var fast);
            slow.Speed = fast.Speed = 4;
            SetValue(slow, 120); SetValue(fast, 120);
            var order = CombatActionOrderPresentation.Build(state);
            Assert.That(order.Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "slow", "fast" }));
            Assert.That(order.Select(unit => unit.Id).Distinct().Count(), Is.EqualTo(order.Count));
        }

        [Test]
        public void InvalidPreviewIsIgnored_DeadUnitsDisappear()
        {
            var state = State(out var hero, out var slow, out var fast);
            var preview = new CombatActionPreview("delay", "unit", "", "", 0, "invalid", actionValueTargetId: fast.Id, actionValueDelay: 90);
            Assert.That(CombatActionOrderPresentation.Build(state, preview).Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "fast", "slow" }));
            typeof(UnitState).GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(slow, new object[] { int.MaxValue });
            Assert.That(CombatActionOrderPresentation.Build(state).Select(unit => unit.Id), Is.EqualTo(new[] { "hero", "fast" }));
        }
    }
}
