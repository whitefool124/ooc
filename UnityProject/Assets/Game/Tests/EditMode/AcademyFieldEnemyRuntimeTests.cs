using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

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
        public void AcademyArbalist_ArmsBeforeFiringAndRearmsAfterRetreat()
        {
            CombatState state = State("rune_arbalist", new GridPosition(5, 3), out UnitState arbalist);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, arbalist.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand arm = plans.GetExecutionCommand(state, arbalist, hero);
            Assert.That(arm.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.ArbalistArmSkillIndex));
            Assert.That(plans.GetPublicIntent(state, arbalist, hero).ActionName, Is.EqualTo("架弩"));
            CombatResolver.Resolve(state, arm);
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth));

            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(4, 3)));
            CombatResolver.BeginTurn(state, arbalist.Id);
            plans = new EnemyTurnPlanBook();
            CombatCommand retreat = plans.GetExecutionCommand(state, arbalist, hero);
            Assert.That(retreat.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(retreat.Destination.ManhattanDistance(hero.Position), Is.GreaterThan(1));
            CombatResolver.Resolve(state, retreat);

            CombatResolver.BeginTurn(state, arbalist.Id);
            plans = new EnemyTurnPlanBook();
            Assert.That(plans.GetExecutionCommand(state, arbalist, hero).SlotIndex,
                Is.EqualTo(AcademyFieldEnemyRuntime.ArbalistArmSkillIndex));
        }

        [Test]
        public void AcademyVanguard_DismantlesAdjacentWallOnlyAfterPublicAction()
        {
            CombatState state = State("elite_vanguard", new GridPosition(3, 3), out UnitState vanguard);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            Set(state, 2, 3, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
            });
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, vanguard.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, vanguard, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.VanguardDismantleSkillIndex));
            Assert.That(plans.GetPublicIntent(state, vanguard, hero).ActionName, Is.EqualTo("拆架"));
            Assert.That(Tile(state, 2, 3).IsDestroyed, Is.False);

            CombatResolver.Resolve(state, command);
            Assert.That(Tile(state, 2, 3).IsDestroyed, Is.True);
            Assert.That(hero.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(hero.Shield, Is.Zero);
            Assert.That(vanguard.ActionPoints, Is.Zero);
        }

        [Test]
        public void AcademyVanguard_PositionGuardRequiresItsOwnIntactWall()
        {
            CombatState mapWall = State("elite_vanguard", new GridPosition(3, 3), out UnitState first);
            mapWall.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            Set(mapWall, 4, 3, tile => { tile.Cover = CoverType.Heavy; tile.Durability = 24; });
            CombatResolver.BeginTurn(mapWall, first.Id);
            Assert.That(mapWall.RogueShieldEvents.Any(record => record.SourceId == "vanguard-position-guard"), Is.False,
                "地图预置重掩体不能触发本单位所筑墙的守位被动");

            CombatState ownWall = State("elite_vanguard", new GridPosition(3, 3), out UnitState second);
            ownWall.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            Set(ownWall, 4, 3, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
                tile.StructureOwnerUnitId = second.Id;
            });
            CombatResolver.BeginTurn(ownWall, second.Id);
            Assert.That(ownWall.RogueShieldEvents.Any(record => record.SourceId == "vanguard-position-guard" && record.Amount == 4), Is.True);
        }

        [Test]
        public void AcademyVanguard_PrioritizesItsOwnedWallAfterHeroBorrowsShield()
        {
            CombatState state = State("elite_vanguard", new GridPosition(3, 3), out UnitState vanguard,
                new GridPosition(2, 2));
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            Set(state, 2, 3, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
                tile.StructureOwnerUnitId = vanguard.Id;
            });
            Set(state, 3, 2, tile => { tile.Cover = CoverType.Heavy; tile.Durability = 24; });
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.EndTurn(hero.Id));

            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, vanguard, hero);
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.VanguardDismantleSkillIndex));
            Assert.That(command.Destination, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(state.AcademyFieldEnemy.PresentIntent(state, vanguard, command).Signature,
                Does.StartWith("SK-CORE-05:"));
            Assert.That(state.Clone().AcademyFieldEnemy.PresentIntent(state, vanguard, command).Signature,
                Does.StartWith("SK-CORE-05:"));
            CombatResolver.BeginTurn(state, vanguard.Id);
            CombatResolver.Resolve(state, command);
            Assert.That(state.Map.GetTile(new GridPosition(2, 3)).IsDestroyed, Is.True);
            Assert.That(hero.Shield, Is.Zero);
            Assert.That(hero.HasStatus(StatusType.BreakStance), Is.True);
        }

        [Test]
        public void AcademyVanguard_DestroyedWallReactionRequiresHeroAsSource()
        {
            CombatState state = State("elite_vanguard", new GridPosition(3, 3), out UnitState vanguard);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            GridPosition cell = new GridPosition(2, 3);
            Set(state, 2, 3, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
                tile.StructureOwnerUnitId = vanguard.Id;
            });
            int before = state.Map.GetTile(cell).Durability;
            state.Map.GetTile(cell).Durability = 0;
            state.ResolveAetherCrystalDamage(cell, before, vanguard.Id);
            Assert.That(state.EventLog.Any(line => line.Contains("玩家拆除了其自筑墙段")), Is.False);

            Set(state, 2, 3, tile => tile.Durability = TileState.TemporaryHeavyCoverDurability);
            before = state.Map.GetTile(cell).Durability;
            state.Map.GetTile(cell).Durability = 0;
            state.ResolveAetherCrystalDamage(cell, before, "hero");
            Assert.That(state.EventLog.Any(line => line.Contains("玩家拆除了其自筑墙段")), Is.True);
        }

        [Test]
        public void AcademyVanguard_RebuildsHeroDestroyedWallAt24Durability()
        {
            CombatState state = State("elite_vanguard", new GridPosition(3, 3), out UnitState vanguard);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            GridPosition cell = new GridPosition(2, 3);
            Set(state, cell.X, cell.Y, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
                tile.StructureOwnerUnitId = vanguard.Id;
            });
            state.Map.GetTile(cell).Durability = 0;
            state.ResolveAetherCrystalDamage(cell, TileState.TemporaryHeavyCoverDurability, "hero");

            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, vanguard.Id);
            CombatResolver.Resolve(state, CombatCommand.EndTurn(vanguard.Id));
            CombatResolver.BeginTurn(state, vanguard.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, vanguard, hero);
            Assert.That(command.Destination, Is.EqualTo(cell));
            Assert.That(plans.GetPublicIntent(state, vanguard, hero).ActionName, Is.EqualTo("原位重筑"));
            CombatResolver.Resolve(state, command);

            Assert.That(state.Map.GetTile(cell).Durability, Is.EqualTo(TileState.HeavyDurability));
            Assert.That(state.Map.GetTile(cell).StructureOwnerUnitId, Is.EqualTo(vanguard.Id));
            Assert.That(vanguard.ActionPoints, Is.Zero);
            Assert.That(state.EventLog.Any(line => line.Contains("原位重筑加厚墙")), Is.True);
        }

        [Test]
        public void Vanguard_NonAcademyCoverShieldDoesNotActivateAreaResponse()
        {
            CombatState state = State("elite_vanguard", new GridPosition(3, 3), out UnitState vanguard,
                new GridPosition(2, 2));
            Set(state, 2, 3, tile =>
            {
                tile.Cover = CoverType.Heavy;
                tile.Durability = TileState.TemporaryHeavyCoverDurability;
                tile.StructureOwnerUnitId = vanguard.Id;
            });
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.EndTurn("hero"));
            CombatResolver.BeginTurn(state, vanguard.Id);

            Assert.That(state.Map.GetTile(new GridPosition(2, 3)).IsDestroyed, Is.False);
            Assert.That(state.EventLog.Any(line => line.Contains("借其自筑墙获得护盾")), Is.False);
        }

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
        public void Tracker_DoesNotLeaveTraceOnWaterOrFire()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            Set(state, 2, 3, tile => tile.IsWater = true);
            Set(state, 3, 3, tile => tile.IsScorched = true);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));

            Assert.That(Tile(state, 1, 3).HasTrace, Is.True);
            Assert.That(Tile(state, 2, 3).HasTrace, Is.False);
            Assert.That(Tile(state, 3, 3).HasTrace, Is.False);
            CombatResolver.BeginTurn(state, "hero");
            Assert.That(Tile(state, 3, 3).IsDeepTrace, Is.False);
        }

        [Test]
        public void Tracker_StrongWindClearsDeepTraceAndPreventsImmediateRecreation()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.BeginTurn(state, "hero");
            Assert.That(Tile(state, 1, 3).IsDeepTrace, Is.True);

            Assert.That(state.Environment.Wind.TryChange(FieldWindState.East, 3), Is.True);
            CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 1, 3).HasTrace, Is.False);
            Assert.That(Tile(state, 1, 3).IsDeepTrace, Is.False);
        }

        [Test]
        public void Tracker_EnteringBindingMarkStopsAtTheMarkBeforeTraceCoversIt()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            Set(state, 2, 3, tile => tile.IsBindingMark = true);
            CombatResolver.BeginTurn(state, "hero");
            Assert.That(CombatMovementQuery.StopAtBindingMark(state,
                CombatMovementQuery.FindPath(state, state.GetUnit("hero"), new GridPosition(3, 3))).Last(),
                Is.EqualTo(new GridPosition(2, 3)));
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));

            UnitState hero = state.GetUnit("hero");
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.True);
            Assert.That(Tile(state, 2, 3).HasTrace, Is.True);
            Assert.That(Tile(state, 2, 3).IsBindingMark, Is.False);
        }

        [Test]
        public void Tracker_PathTraceReplacesLoosePaperWithoutLayerOverlap()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            Set(state, 2, 3, tile => tile.IsLoosePaper = true);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(2, 3)));

            Assert.That(Tile(state, 2, 3).HasTrace, Is.True);
            Assert.That(Tile(state, 2, 3).IsLoosePaper, Is.False);
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
        public void Tracker_OldTraceTimerDoesNotClearAnotherSourcesReplacement()
        {
            CombatState state = State("elder_tracker_hound", new GridPosition(7, 5), out _);
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));
            Set(state, 1, 3, tile => tile.EffectSourceId = "map:replacement-trace");

            for (int turn = 0; turn < 3; turn++) CombatResolver.BeginTurn(state, "hero");

            Assert.That(Tile(state, 1, 3).HasTrace, Is.True);
            Assert.That(Tile(state, 1, 3).EffectSourceId, Is.EqualTo("map:replacement-trace"));
        }

        [Test]
        public void Snare_OldMarkTimerDoesNotClearAnotherSourcesReplacement()
        {
            CombatState state = State("stone_snare", new GridPosition(4, 3), out UnitState snare);
            GridPosition cell = new GridPosition(2, 3);
            CombatResolver.BeginTurn(state, snare.Id);
            CombatResolver.Resolve(state, CombatCommand.UseSkillAt(snare.Id,
                AcademyFieldEnemyRuntime.BindingMarkSkillIndex, cell, default));
            Set(state, cell.X, cell.Y, tile => tile.EffectSourceId = "map:replacement-mark");

            for (int turn = 0; turn < 3; turn++) CombatResolver.BeginTurn(state, "hero");
            CombatResolver.BeginTurn(state, snare.Id);

            Assert.That(state.Map.GetTile(cell).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(cell).EffectSourceId, Is.EqualTo("map:replacement-mark"));
        }

        [Test]
        public void Snare_RouteResponseUsesPreviewedForwardCellAndOnePerHeroRound()
        {
            CombatState state = State("stone_snare", new GridPosition(4, 4), out _);
            Set(state, 2, 4, tile => { tile.IsBindingMark = true; tile.EffectSourceId = "map:mark"; });
            CombatResolver.BeginTurn(state, "hero");
            IReadOnlyList<GridPosition> path = CombatMovementQuery.FindPath(state, state.GetUnit("hero"), new GridPosition(3, 3));
            Assert.That(state.AcademyFieldEnemy.PreviewSnareRouteResponse(state, path),
                Is.EqualTo(new GridPosition(4, 3)));

            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(3, 3)));
            Assert.That(Tile(state, 4, 3).IsBindingMark, Is.True);
            Assert.That(Tile(state, 4, 3).EffectSourceId, Is.EqualTo("skill:SK-SUP-10"));
            Assert.That(state.AcademyFieldEnemy.PreviewSnareRouteResponse(state,
                new[] { new GridPosition(3, 3), new GridPosition(3, 4) }), Is.Null);
            Assert.That(state.AcademyFieldEnemy.Clone().PreviewSnareRouteResponse(state,
                new[] { new GridPosition(3, 3), new GridPosition(3, 4) }), Is.Null);
        }

        [Test]
        public void Snare_RouteResponseDoesNotTriggerWhenPathEntersMark()
        {
            CombatState state = State("stone_snare", new GridPosition(4, 4), out _);
            Set(state, 2, 3, tile => { tile.IsBindingMark = true; tile.EffectSourceId = "map:mark"; });
            CombatResolver.BeginTurn(state, "hero");
            IReadOnlyList<GridPosition> path = CombatMovementQuery.StopAtBindingMark(state,
                CombatMovementQuery.FindPath(state, state.GetUnit("hero"), new GridPosition(3, 3)));
            Assert.That(state.AcademyFieldEnemy.PreviewSnareRouteResponse(state, path), Is.Null);
        }

        [Test]
        public void Mender_PrioritizesAllyDamagedByHeroOnItsNextTurn()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(6, 1));
            hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            UnitState mender = new UnitState("mender", false, new GridPosition(2, 1));
            UnitState baseline = new UnitState("a_ally", false, new GridPosition(3, 3));
            UnitState wounded = new UnitState("z_ally", false, new GridPosition(3, 1));
            EnemyArchetypes.Get("barrier_mender").Apply(mender);
            GridMap map = new GridMap(8, 6);
            map.SetTile(new GridPosition(2, 2), new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability });
            map.SetTile(new GridPosition(3, 2), new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability });
            CombatState state = new CombatState(map, new[] { hero, mender, baseline, wounded });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            CombatResolver.BeginTurn(state, mender.Id);
            CombatResolver.BeginTurn(state, mender.Id);
            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(EnemyTactics.Choose(state, mender, hero).TargetUnitId, Is.EqualTo(baseline.Id));

            CombatCommandExecutionResult shot = new CombatCommandExecutionService().Execute(state, null,
                CombatCommand.Attack(hero.Id, wounded.Id));
            Assert.That(shot.Accepted, Is.True);
            Assert.That(wounded.Health + wounded.Shield, Is.LessThan(wounded.MaxHealth));
            CombatResolver.BeginTurn(state, mender.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand response = plans.GetExecutionCommand(state, mender, hero);
            Assert.That(response.TargetUnitId, Is.EqualTo(wounded.Id));
            Assert.That(plans.GetPublicIntent(state, mender, hero).Signature,
                Does.StartWith("SK-SUP-03:"));
            Assert.That(state.AcademyFieldEnemy.Clone().ChooseEnemyCommand(state, mender, hero).TargetUnitId,
                Is.EqualTo(wounded.Id));
            CombatResolver.Resolve(state, CombatCommand.EndTurn(mender.Id));
            Assert.That(state.AcademyFieldEnemy.ChooseEnemyCommand(state, mender, hero).TargetUnitId,
                Is.EqualTo(baseline.Id));
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
            Assert.That(state.Environment.LightLanes, Is.Empty, "转镜需要占用行动，回合开始不自动生效。");
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, keeper, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.SpotlightSkillIndex));
            CombatResolver.Resolve(state, command);

            FieldLightLaneState lane = state.Environment.LightLanes.Single();
            Assert.That(lane.Direction, Is.EqualTo(FieldWindState.East));
            Assert.That(state.Environment.LitCells(state.Map, lane, state.CurrentTime).Count, Is.EqualTo(2), "重掩体遮断处留下暗段。");
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
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, keeper, hero);
            CombatResolver.Resolve(state, command);
            Assert.That(hero.Health, Is.EqualTo(healthBefore), "伤害在自身回合结束时结算。");

            CombatResolver.EndTurn(state, keeper);

            Assert.That(state.EventLog.Any(line => line.Contains("光柱结算")), Is.True, string.Join(" | ", state.EventLog));
            Assert.That(hero.Health, Is.LessThan(healthBefore));
        }

        [Test]
        public void Keeper_RotatesTowardTheHeroTwicePerBattle()
        {
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out UnitState keeper);
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatResolver.Resolve(state, state.AcademyFieldEnemy.ChooseEnemyCommand(state, keeper, hero));
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
        public void Prototype_DirectDetonationUsesOverloadDamageAndOneAction()
        {
            CombatState state = State("prototype_hand", new GridPosition(1, 1), out UnitState hand,
                new GridPosition(3, 4));
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            GridPosition device = new GridPosition(3, 3);
            Set(state, device.X, device.Y, tile =>
            { tile.IsDevice = true; tile.IsOverloadDevice = true; tile.Durability = TileState.PrototypeDurability; });
            CombatResolver.BeginTurn(state, hand.Id);
            Assert.That(state.Map.GetTile(device).IsOverloadDevice, Is.True);
            CombatCommand command = state.AcademyFieldEnemy.ChooseEnemyCommand(state, hand, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.PrototypeDetonateSkillIndex));
            int health = state.GetUnit("hero").Health;
            CombatResolver.Resolve(state, command);
            Assert.That(state.Map.GetTile(device).IsOverloadDevice, Is.False);
            Assert.That(state.GetUnit("hero").Health, Is.LessThan(health));
            Assert.That(hand.ActionPoints, Is.Zero);
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
            CombatState state = State("signal_keeper", new GridPosition(1, 5), out UnitState keeper);
            CombatResolver.BeginTurn(state, "enemy_0");
            CombatResolver.Resolve(state, state.AcademyFieldEnemy.ChooseEnemyCommand(state, keeper, state.GetUnit("hero")));
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 1)));

            CombatState clone = state.Clone();

            Assert.That(clone.AcademyFieldEnemy, Is.Not.Null);
            Assert.That(clone.AcademyFieldEnemy.RotateUsesRemaining, Is.EqualTo(state.AcademyFieldEnemy.RotateUsesRemaining));
            Assert.That(clone.Environment.LightLanes.Single().Direction, Is.EqualTo(state.Environment.LightLanes.Single().Direction));
        }
    }
}
