using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatMovementRangeTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryHighlightedDestinationMatchesExecutedCommand(int scenario)
        {
            CombatState state = Create(scenario);
            var adapter = new BattlefieldPresentationAdapter();
            int count = 0;
            string before = Signature(state);
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
            {
                var target = new GridPosition(x, y);
                bool highlighted = adapter.IsInMoveRange(state, target);
                Assert.That(string.IsNullOrEmpty(adapter.InvalidReasonForCell(state, "移动", target)), Is.EqualTo(highlighted), target.ToString());
                if (target == state.GetUnit("hero").Position) { Assert.That(highlighted, Is.False); continue; }
                CombatState execution = Create(scenario);
                var result = new CombatCommandExecutionService().Execute(execution, null, CombatCommand.Move("hero", target));
                Assert.That(highlighted, Is.EqualTo(result.Accepted), target + ": " + result.RejectionReason);
                if (highlighted) { count++; Assert.That(execution.GetUnit("hero").Position, Is.EqualTo(target)); }
            }
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(adapter.BuildPreview(state, "移动", null).ValidCellCount, Is.EqualTo(count));
            Assert.That(Signature(state), Is.EqualTo(before), "Read queries must not consume resources or invoke movement effects.");
        }

        [Test]
        public void BoundApEnemyTurnAndBattleEndRemoveAllMoveHighlights()
        {
            var adapter = new BattlefieldPresentationAdapter();
            CombatState state = Create(0);
            Assert.That(adapter.BuildPreview(state, "移动", null).ValidCellCount, Is.GreaterThan(0));
            state.GetUnit("hero").ApplyStatus(StatusType.Bound, 2);
            AssertEmpty(adapter, state, "束缚");
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.ClearStatus("hero", StatusType.Bound));
            Assert.That(adapter.BuildPreview(state, "移动", null).ValidCellCount, Is.GreaterThan(0));
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.SpendActionPoints(3));
            AssertEmpty(adapter, state, "行动点不足");
            state = Create(0);
            CombatResolver.BeginTurn(state, state.Units.Values.First(unit => !unit.IsHero).Id);
            AssertEmpty(adapter, state, "等待敌方");
            state = Create(0);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageHealth("hero", 999));
            CombatResolver.BeginTurn(state, "hero");
            AssertEmpty(adapter, state, "战斗已经结束");
        }

        [Test]
        public void CachedRangeReactsToMutableTerrainAndUnitOccupancyWithoutLogChanges()
        {
            CombatState state = Create(3);
            var adapter = new BattlefieldPresentationAdapter();
            var target = new GridPosition(2, 0);
            Assert.That(adapter.IsInMoveRange(state, target), Is.False, "Wall forces a detour beyond five steps.");
            var door = new TileState { Cover = CoverType.Heavy, Durability = 10 };
            state.Map.SetTile(new GridPosition(1, 0), door);
            Assert.That(adapter.IsInMoveRange(state, target), Is.False);
            door.Durability = 0;
            Assert.That(adapter.IsInMoveRange(state, target), Is.True, "In-place tile mutation invalidates cached paths.");
            CombatEffectExecutor.Execute(state, "enemy", CombatEffect.Move(new GridPosition(1, 0)));
            Assert.That(adapter.IsInMoveRange(state, target), Is.False);
            CombatEffectExecutor.Execute(state, "enemy", CombatEffect.Move(new GridPosition(7, 3)));
            Assert.That(adapter.IsInMoveRange(state, target), Is.True);
        }

        [Test]
        public void VineCostAndOriginBonusChangesInvalidateCache()
        {
            CombatState state = Create(0);
            var adapter = new BattlefieldPresentationAdapter();
            var target = new GridPosition(1, 1);
            Assert.That(adapter.IsInMoveRange(state, target), Is.False);
            state.RainLanternCourt.CastBorrowedCover(state, state.GetUnit("hero"));
            Assert.That(adapter.IsInMoveRange(state, target), Is.True, "Six dry steps become legal with the existing +2 budget.");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 6)));
            Assert.That(adapter.IsInMoveRange(state, new GridPosition(1, 0)), Is.False, "Consumed bonus cannot linger.");
            var vineTarget = new GridPosition(5, 6);
            state.Map.SetTile(new GridPosition(2, 6), new TileState { IsLampVine = true });
            state.Map.SetTile(new GridPosition(3, 6), new TileState { IsLampVine = true });
            Assert.That(adapter.IsInMoveRange(state, vineTarget), Is.False);
            for (int x = 2; x <= 5; x++) state.Map.SetTile(new GridPosition(x, 6), new TileState());
            Assert.That(adapter.IsInMoveRange(state, vineTarget), Is.True);
        }

        [Test]
        public void StableInputReusesPathsWhileNewBattleRebuilds()
        {
            var cache = new CombatMovementRangeCache();
            CombatState state = Create(0);
            var target = new GridPosition(1, 6);
            for (int n = 0; n < 10; n++) Assert.That(cache.Contains(state, state.GetUnit("hero"), target), Is.True);
            Assert.That(cache.RebuildCount, Is.EqualTo(1));
            state = Create(0);
            cache.Contains(state, state.GetUnit("hero"), target);
            Assert.That(cache.RebuildCount, Is.EqualTo(2));
        }

        private static void AssertEmpty(BattlefieldPresentationAdapter adapter, CombatState state, string reason)
        {
            Assert.That(adapter.BuildPreview(state, "移动", null).ValidCellCount, Is.Zero);
            Assert.That(adapter.InvalidReasonForCell(state, "移动", new GridPosition(1, 6)), Does.Contain(reason));
        }

        private static CombatState Create(int scenario)
        {
            CombatState state;
            if (scenario == 3)
            {
                state = new CombatState(new GridMap(8, 4), new[] {
                    new UnitState("hero", true, new GridPosition(0, 0)),
                    new UnitState("enemy", false, new GridPosition(7, 3)) });
                for (int y = 0; y < 3; y++) state.Map.SetTile(new GridPosition(1, y), new TileState { Cover = CoverType.Heavy, Durability = 10 });
            }
            else state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            if (scenario == 1) state.GetUnit("hero").ApplyStatus(StatusType.Slow, 2);
            CombatResolver.BeginTurn(state, "hero");
            if (scenario == 2) state.RainLanternCourt.CastBorrowedCover(state, state.GetUnit("hero"));
            return state;
        }

        private static string Signature(CombatState state) => string.Join("|", state.Units.Values.Select(unit =>
            unit.Id + ":" + unit.Position + ":" + unit.Health + ":" + unit.Shield + ":" + unit.Mana + ":" + unit.ActionPoints))
            + "/" + state.EventLog.Count + "/" + CombatMovementQuery.Budget(state, state.GetUnit("hero"));
    }
}
