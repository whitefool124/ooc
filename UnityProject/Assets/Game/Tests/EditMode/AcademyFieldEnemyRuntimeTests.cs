using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    /// <summary>老寻（痕迹与循味）与灯台值守（光柱与转向）的公开条件反应。</summary>
    public sealed class AcademyFieldEnemyRuntimeTests
    {
        private static CombatState State(string enemyId, GridPosition enemyCell, out UnitState enemy, GridPosition? heroCell = null)
        {
            UnitState hero = new UnitState("hero", true, heroCell ?? new GridPosition(1, 3)) { DisplayName = "维克多·维恩", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            enemy = new UnitState("enemy_0", false, enemyCell) { DisplayName = "测试单位" };
            EnemyArchetypes.Get(enemyId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(9, 7), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            return state;
        }

        private static void Set(CombatState state, int x, int y, System.Action<TileState> edit)
        {
            GridPosition position = new GridPosition(x, y);
            TileState tile = state.Map.GetTile(position).Clone();
            edit(tile);
            state.Map.SetTile(position, tile);
        }

        private static TileState Tile(CombatState state, int x, int y) => state.Map.GetTile(new GridPosition(x, y));

        [Test]
        public void Tracker_LeavesTraceAlongTheHeroPath()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));

            Assert.That(Tile(state, 1, 3).HasTrace, Is.True, "起始格应留下气味痕");
            Assert.That(Tile(state, 3, 3).HasTrace, Is.True, "终点格应留下气味痕");
            Assert.That(state.EventLog.Any(line => line.Contains("气味痕")), Is.True);
        }

        [Test]
        public void Tracker_DoesNotLeaveTraceWithoutALivingTracker()
        {
            CombatState state = State("shieldguard", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));

            Assert.That(Tile(state, 1, 3).HasTrace, Is.False);
            Assert.That(Tile(state, 3, 3).HasTrace, Is.False);
        }

        [Test]
        public void Tracker_TraceExpiresAfterThreeHeroRounds()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));
            Assert.That(Tile(state, 1, 3).HasTrace, Is.True);

            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(5, 3)));
            CombatResolver.BeginTurn(state, "hero");
            Assert.That(Tile(state, 1, 3).HasTrace, Is.True, "三个主角回合内仍然有效。");

            CombatResolver.BeginTurn(state, "hero");
            Assert.That(Tile(state, 1, 3).HasTrace, Is.False, "超过三个主角回合后气味痕失效。");
        }

        [Test]
        public void Tracker_DeepTraceAppearsWhenTheHeroStaysInPlace()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 1, 3).HasTrace, Is.True);
            Assert.That(Tile(state, 1, 3).IsDeepTrace, Is.True);
            Assert.That(state.EventLog.Any(line => line.Contains("加深")), Is.True);
        }

        [Test]
        public void Tracker_SniffAddsMovementAndIgnoresCostlyGround()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(4, 3), out UnitState tracker);
            Set(state, 4, 3, tile => tile.HasTrace = true);
            Set(state, 5, 3, tile => { tile.HasTrace = true; tile.IsWater = true; });
            Set(state, 6, 3, tile => tile.IsWater = true);
            CombatResolver.BeginTurn(state, "enemy_0");

            int budget = CombatMovementQuery.Budget(state, tracker);
            Assert.That(budget, Is.EqualTo(UnitState.BaseMovementRange + AcademyFieldEnemyRuntime.SniffMovementBonus));
            Assert.That(CombatMovementQuery.EntryCost(state, tracker, new GridPosition(5, 3)), Is.EqualTo(1), "沿痕迹进入浅水不额外消耗。");
            Assert.That(CombatMovementQuery.EntryCost(state, tracker, new GridPosition(6, 3)), Is.EqualTo(2), "离开痕迹后仍按浅水结算。");
        }

        [Test]
        public void Tracker_MaulsOnlyWhenTheTargetStandsOnATrace()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(2, 3), out UnitState tracker);
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.ChooseEnemyCommand(state, tracker, state.GetUnit("hero")).SlotIndex,
                Is.EqualTo(0), "目标不在气味痕上时只结算 3 点伤害。");

            Set(state, 1, 3, tile => tile.HasTrace = true);
            Assert.That(state.AcademyFieldEnemy.ChooseEnemyCommand(state, tracker, state.GetUnit("hero")).SlotIndex,
                Is.EqualTo(1), "目标位于气味痕上时改为 6 点伤害并束缚。");
        }

        [Test]
        public void Keeper_TurnsTheMirrorAndStopsAtBlockingCover()
        {
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out UnitState keeper, new GridPosition(5, 5));
            Set(state, 4, 5, tile => { tile.Cover = CoverType.Heavy; tile.Durability = TileState.HeavyDurability; });
            CombatResolver.BeginTurn(state, "enemy_0");

            FieldLightLaneState lane = state.Environment.LightLanes.Single();
            Assert.That(lane.Direction, Is.EqualTo(FieldWindState.East));
            Assert.That(state.Environment.LitCells(state.Map, lane).Count, Is.EqualTo(2), "重掩体遮断处留下暗段。");
            Assert.That(state.EventLog.Any(line => line.Contains("灯镜")), Is.True);
            Assert.That(keeper.Position, Is.EqualTo(new GridPosition(1, 5)));
        }

        [Test]
        public void Keeper_DamagesUnitsOnLitCellsAtItsOwnTurnEnd()
        {
            CombatState state = State("signal_keeper", new GridPosition(3, 3), out UnitState keeper);
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, "enemy_0");
            int healthBefore = hero.Health;

            CombatResolver.EndTurn(state, keeper);

            Assert.That(state.EventLog.Any(line => line.Contains("光柱结算")), Is.True, string.Join(" | ", state.EventLog));
            Assert.That(hero.Health, Is.LessThan(healthBefore));
        }

        [Test]
        public void Keeper_RotatesTowardTheHeroTwicePerBattle()
        {
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out _);
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.Environment.LightLanes.Single().Direction, Is.EqualTo(FieldWindState.South), "灯镜先朝主角所在方向。");

            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(4, 3)));
            Assert.That(state.Environment.LightLanes.Single().Direction, Is.EqualTo(FieldWindState.East), "主角离开照明格后光柱转向其新路线。");
            Assert.That(state.AcademyFieldEnemy.RotateUsesRemaining, Is.EqualTo(1));

            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(4, 1)));
            Assert.That(state.AcademyFieldEnemy.RotateUsesRemaining, Is.EqualTo(0));

            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(7, 1)));
            Assert.That(state.AcademyFieldEnemy.RotateUsesRemaining, Is.EqualTo(0), "反应次数用尽后不再转向。");
        }

        [Test]
        public void Intent_PublishesThePublicConditionReaction()
        {
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out UnitState keeper);
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, keeper, state.GetUnit("hero"));

            EnemyIntentPresentation intent = state.AcademyFieldEnemy.PresentIntent(state, keeper, command);

            Assert.That(intent.DetailedText, Does.Contain("转镜"));
            Assert.That(intent.DetailedText, Does.Contain("反应"));
            Assert.That(intent.DetailedText, Does.Contain("2 次"));
        }

        [Test]
        public void TrackerIntent_PublishesSniffAndBiteConditions()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(2, 3), out UnitState tracker);
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, tracker, state.GetUnit("hero"));

            EnemyIntentPresentation intent = state.AcademyFieldEnemy.PresentIntent(state, tracker, command);

            Assert.That(intent.DetailedText, Does.Contain("循味"));
            Assert.That(intent.DetailedText, Does.Contain("扑咬"));
        }

        [Test]
        public void Clone_KeepsTraceAgeAndRotationUses()
        {
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out _);
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 1)));

            CombatState clone = state.Clone();

            Assert.That(clone.AcademyFieldEnemy, Is.Not.Null);
            Assert.That(clone.AcademyFieldEnemy.RotateUsesRemaining, Is.EqualTo(state.AcademyFieldEnemy.RotateUsesRemaining));
            Assert.That(clone.Environment.LightLanes.Single().Direction, Is.EqualTo(state.Environment.LightLanes.Single().Direction));
        }
    }
}
