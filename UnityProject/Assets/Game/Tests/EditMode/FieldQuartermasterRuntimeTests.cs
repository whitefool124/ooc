using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    /// <summary>老库管（旧脉冲／退件／登记）与试制员（布放／引爆）的场地手段与公开条件反应。</summary>
    public sealed class FieldQuartermasterRuntimeTests
    {
        private static CombatState State(string enemyId, GridPosition enemyCell, GridPosition heroCell, out UnitState enemy)
        {
            UnitState hero = new UnitState("hero", true, heroCell) { DisplayName = "维克多·维恩", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            enemy = new UnitState("enemy_0", false, enemyCell) { DisplayName = "测试单位" };
            EnemyArchetypes.Get(enemyId).Apply(enemy);
            CombatState state = new CombatState(new GridMap(11, 7), new[] { hero, enemy });
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
        public void BothUnits_AreElitesWithOnlyTheirDeclaredAbility()
        {
            EnemyArchetype keeper = EnemyArchetypes.Get("legacy_storekeeper");
            Assert.That(keeper.DisplayName, Is.EqualTo("老库管"));
            Assert.That(keeper.IsElite, Is.True);
            Assert.That(keeper.MaxHealth, Is.EqualTo(18));
            Assert.That(keeper.Weapon.DisplayName, Is.EqualTo("旧式检定器"));
            Assert.That(keeper.PrimarySkill.DisplayName, Is.EqualTo("旧脉冲"));
            Assert.That(keeper.HasSecondarySkill, Is.False);

            EnemyArchetype hand = EnemyArchetypes.Get("prototype_hand");
            Assert.That(hand.DisplayName, Is.EqualTo("试制员"));
            Assert.That(hand.IsElite, Is.True);
            Assert.That(hand.MaxHealth, Is.EqualTo(16));
            Assert.That(hand.Weapon.DisplayName, Is.EqualTo("工具"));
            Assert.That(hand.PrimarySkill.DisplayName, Is.EqualTo("布放"));
            Assert.That(hand.HasSecondarySkill, Is.False);
        }

        [Test]
        public void Storekeeper_PulseHitsEveryUnitOnTheLineAndBreaksStance()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(4, 3), out _);
            UnitState hero = state.GetUnit("hero");
            Assert.That(state.TryGrantRogueliteShield("hero", "test-shield", 4), Is.True);
            Assert.That(hero.Shield, Is.GreaterThan(0));

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(hero.Shield, Is.Zero, "旧脉冲清空护盾。");
            Assert.That(hero.HasStatus(StatusType.BreakStance), Is.True, "旧脉冲施加破势。");
            Assert.That(hero.Health, Is.LessThan(hero.MaxHealth));
            Assert.That(state.EventLog.Any(line => line.Contains("旧脉冲")), Is.True, string.Join(" | ", state.EventLog));
        }

        [Test]
        public void Storekeeper_PulseOutOfRangeFallsBackToRetireOrRegister()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(4, 3), out _);
            // 重掩体截断旧脉冲的直线，主角仍在登记射程（4 格）内。
            Set(state, 3, 3, tile => { tile.Cover = CoverType.Heavy; tile.Durability = TileState.HeavyDurability; });

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.EventLog.Any(line => line.Contains("旧脉冲")), Is.False);
            Assert.That(state.AcademyFieldEnemy.InspectionMarkCount, Is.EqualTo(1), "直线被截断后改为登记主角。");
            Assert.That(state.EventLog.Any(line => line.Contains("登记")), Is.True);
        }

        [Test]
        public void Storekeeper_RetiresTheFieldEffectTheHeroJustCreated()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(9, 5), out _);
            CombatResolver.BeginTurn(state, "enemy_0");
            state.AcademyFieldEnemy.Clone(); // 快照在上一回合结束时已经建立
            CombatResolver.BeginTurn(state, "hero");
            // 退件射程上限 5 格：把新生成的散页放进该范围内。
            Set(state, 5, 3, tile => tile.IsLoosePaper = true);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(Tile(state, 5, 3).IsLoosePaper, Is.False, "主角新生成的散页被优先退掉。");
            Assert.That(state.EventLog.Any(line => line.Contains("退件")), Is.True);
        }

        [Test]
        public void Storekeeper_RegisterClearsTheNextShieldGrant()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(4, 3), out _);
            Set(state, 3, 3, tile => { tile.Cover = CoverType.Heavy; tile.Durability = TileState.HeavyDurability; });
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.InspectionMarkCount, Is.EqualTo(1));

            bool granted = state.TryGrantRogueliteShield("hero", "test-shield", 4);

            Assert.That(granted, Is.False, "被登记单位的下一次护盾被优先清除。");
            Assert.That(state.AcademyFieldEnemy.InspectionMarkCount, Is.Zero, "标记在生效后消耗。");
            Assert.That(state.EventLog.Any(line => line.Contains("登记生效")), Is.True);
        }

        [Test]
        public void AcademyStorekeeper_PulseUsesPublicCommandAndOnlyResolvesOnExecution()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(4, 3), out UnitState storekeeper);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, storekeeper.Id);
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth));

            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, storekeeper, hero);
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.UseSkill));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.StorekeeperPulseSkillIndex));
            Assert.That(plans.GetPublicIntent(state, storekeeper, hero).ActionName, Is.EqualTo("旧脉冲"));

            CombatResolver.Resolve(state, command);
            Assert.That(hero.Health, Is.LessThan(hero.MaxHealth));
            Assert.That(storekeeper.ActionPoints, Is.Zero);
        }

        [Test]
        public void AcademyStorekeeper_BlockedPulsePublishesRegisterInstead()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(4, 3), out UnitState storekeeper);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachAcademyEnemyGrowth(new AcademyEnemyGrowthRuntime());
            Set(state, 3, 3, tile => { tile.Cover = CoverType.Heavy; tile.Durability = TileState.HeavyDurability; });
            CombatResolver.BeginTurn(state, storekeeper.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, storekeeper, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.StorekeeperRegisterSkillIndex));
            Assert.That(plans.GetPublicIntent(state, storekeeper, state.GetUnit("hero")).ActionName, Is.EqualTo("登记"));
            Assert.That(state.AcademyFieldEnemy.InspectionMarkCount, Is.Zero);

            CombatResolver.Resolve(state, command);
            Assert.That(state.AcademyFieldEnemy.InspectionMarkCount, Is.EqualTo(1));
        }

        [Test]
        public void AcademyStorekeeper_RetiresFiregroundThroughPublicAction()
        {
            CombatState state = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(9, 5), out UnitState storekeeper);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            state.AttachRogueSpellRuntime(new OCC.Combat.Roguelite.RogueSpellCombatRuntime(state,
                OCC.Combat.Roguelite.RogueSpellLoadout.Restore(new[] { "BASE-FIRE-MELEE" },
                    new[] { "BASE-FIRE-MELEE", "", "", "", "", "", "", "" }, true)));
            GridPosition fireCell = new GridPosition(5, 3);
            state.RogueSpells.FireBattle.CreateOrRefreshFireground(fireCell, 2, 2, "test-fire", "hero");
            CombatResolver.BeginTurn(state, storekeeper.Id);
            var plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(state, storekeeper, state.GetUnit("hero"));
            Assert.That(command.SlotIndex, Is.EqualTo(AcademyFieldEnemyRuntime.StorekeeperRetireSkillIndex));
            Assert.That(plans.GetPublicIntent(state, storekeeper, state.GetUnit("hero")).ActionName, Is.EqualTo("退件"));

            CombatResolver.Resolve(state, command);
            Assert.That(state.RogueSpells.FireBattle.HasFireground(fireCell), Is.False);
        }

        [Test]
        public void PrototypeHand_DeploysUpToFourPiecesStartingWithAWardGenerator()
        {
            CombatState state = State("prototype_hand", new GridPosition(5, 3), new GridPosition(9, 3), out _);

            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.PrototypePlaced, Is.EqualTo(1));
            Assert.That(state.Map.PositionsWith(tile => tile.IsWardGenerator).Count(), Is.EqualTo(1));

            for (int turn = 0; turn < 4; turn++) CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.AcademyFieldEnemy.PrototypePlaced, Is.EqualTo(4), "试制箱全场共四件。");
            Assert.That(state.AcademyFieldEnemy.PrototypeRemaining, Is.Zero);
            int deployed = state.Map.PositionsWith(tile => tile.IsWardGenerator || tile.IsOverloadDevice).Count();
            Assert.That(deployed, Is.EqualTo(4));
        }

        [Test]
        public void PrototypeHand_DetonatesThePieceTheHeroStandsNextTo()
        {
            CombatState state = State("prototype_hand", new GridPosition(1, 5), new GridPosition(6, 3), out _);
            Set(state, 5, 3, tile => { tile.IsDevice = true; tile.IsOverloadDevice = true; tile.Durability = TileState.StandardDurability; });
            UnitState hero = state.GetUnit("hero");
            int healthBefore = hero.Health;

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(Tile(state, 5, 3).IsOverloadDevice, Is.False, "主角相邻的试制件被引爆。");
            Assert.That(hero.Health, Is.LessThan(healthBefore), "引爆伤害敌我一致。");
            Assert.That(state.EventLog.Any(line => line.Contains("过载装置引爆")), Is.True);
        }

        [Test]
        public void PrototypeHand_ReplenishesAfterTheHeroRemovesAPiece()
        {
            CombatState state = State("prototype_hand", new GridPosition(5, 3), new GridPosition(9, 6), out _);
            CombatResolver.BeginTurn(state, "enemy_0");
            Assert.That(state.AcademyFieldEnemy.PrototypePlaced, Is.EqualTo(1));
            GridPosition deployed = state.Map.PositionsWith(tile => tile.IsWardGenerator).Single();
            TileState cleared = state.Map.GetTile(deployed).Clone();
            cleared.IsWardGenerator = false; cleared.IsDevice = false; cleared.Durability = 0;
            state.Map.SetTile(deployed, cleared);

            CombatResolver.BeginTurn(state, "enemy_0");

            Assert.That(state.AcademyFieldEnemy.PrototypePlaced, Is.EqualTo(2), "被拆除后补放一件。");
            Assert.That(state.EventLog.Any(line => line.Contains("补放")), Is.True, string.Join(" | ", state.EventLog));
        }

        [Test]
        public void BothUnits_PublishTheirWholeFieldKitInIntent()
        {
            CombatState keeperState = State("legacy_storekeeper", new GridPosition(1, 3), new GridPosition(9, 5), out UnitState keeper);
            CombatResolver.BeginTurn(keeperState, "enemy_0");
            EnemyIntentPresentation keeperIntent = keeperState.AcademyFieldEnemy.PresentIntent(keeperState, keeper,
                keeperState.AcademyFieldEnemy.ChooseEnemyCommand(keeperState, keeper, keeperState.GetUnit("hero")));
            Assert.That(keeperIntent.DetailedText, Does.Contain("旧脉冲"));
            Assert.That(keeperIntent.DetailedText, Does.Contain("退件"));
            Assert.That(keeperIntent.DetailedText, Does.Contain("登记"));

            CombatState handState = State("prototype_hand", new GridPosition(5, 3), new GridPosition(9, 3), out UnitState hand);
            CombatResolver.BeginTurn(handState, "enemy_0");
            EnemyIntentPresentation handIntent = handState.AcademyFieldEnemy.PresentIntent(handState, hand,
                handState.AcademyFieldEnemy.ChooseEnemyCommand(handState, hand, handState.GetUnit("hero")));
            Assert.That(handIntent.DetailedText, Does.Contain("布放"));
            Assert.That(handIntent.DetailedText, Does.Contain("引爆"));
            Assert.That(handIntent.DetailedText, Does.Contain("反应"));
        }

        [Test]
        public void Clone_KeepsStockAndMarks()
        {
            CombatState state = State("prototype_hand", new GridPosition(5, 3), new GridPosition(9, 3), out _);
            CombatResolver.BeginTurn(state, "enemy_0");

            CombatState clone = state.Clone();

            Assert.That(clone.AcademyFieldEnemy.PrototypePlaced, Is.EqualTo(state.AcademyFieldEnemy.PrototypePlaced));
            Assert.That(clone.AcademyFieldEnemy.PrototypeRemaining, Is.EqualTo(state.AcademyFieldEnemy.PrototypeRemaining));
            Assert.That(clone.Map.PositionsWith(tile => tile.IsWardGenerator || tile.IsOverloadDevice).Count(),
                Is.EqualTo(state.Map.PositionsWith(tile => tile.IsWardGenerator || tile.IsOverloadDevice).Count()));
        }
    }
}
