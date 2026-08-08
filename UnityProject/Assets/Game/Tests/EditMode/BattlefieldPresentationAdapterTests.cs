using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class BattlefieldPresentationAdapterTests
    {
        [Test]
        public void BoardRect_PreservesSeventyFivePercentCombatRegion()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            BattlefieldRect board = adapter.BoardRect();
            Assert.That(board.X, Is.EqualTo(252f));
            Assert.That(board.Width, Is.EqualTo(936f));
            Assert.That(board.Height, Is.EqualTo(702f));
            Assert.That(board.XMax, Is.LessThan(1440f));
        }

        [Test]
        public void TryResolveCell_AccountsForInvertedVisualYAndCellGap()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            BattlefieldRect board = adapter.BoardRect();
            BattlefieldRect target = adapter.CellRect(board, 9, new GridPosition(2, 3));
            float centerX = target.X + target.Width * .5f;
            float centerY = target.Y + target.Height * .5f;
            Assert.That(adapter.TryResolveCell(board, 12, 9, centerX, centerY, out GridPosition resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(adapter.TryResolveCell(board, 12, 9, target.XMax + 1f, centerY, out _), Is.False);
        }

        [Test]
        public void FacingAndDistance_AreDeterministic()
        {
            GridPosition origin = new GridPosition(2, 2);
            Assert.That(BattlefieldPresentationAdapter.Distance(origin, new GridPosition(5, 3)), Is.EqualTo(4));
            Assert.That(BattlefieldPresentationAdapter.FacingToward(origin, new GridPosition(5, 3)), Is.EqualTo(Facing.East));
            Assert.That(BattlefieldPresentationAdapter.StepToward(origin, new GridPosition(1, 5)), Is.EqualTo(new GridPosition(2, 3)));
        }

        [Test]
        public void AttackPreview_ReportsRuleCostAndDeterministicDamage()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 1), Facing.West);
            CombatState state = new CombatState(new GridMap(4, 3), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, "hero");
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();

            CombatActionPreview preview = adapter.BuildPreview(state, "攻击", "enemy");

            Assert.That(preview.TargetRule, Does.Contain("可见敌人"));
            Assert.That(preview.Cost, Is.EqualTo("1 AP"));
            Assert.That(preview.ExpectedResult, Does.Contain("预计生命"));
            Assert.That(preview.FailureReason, Is.Empty);
        }

        [Test]
        public void EmptyInteractionAndWrongLootCell_HaveExplicitReasons()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1), Facing.East);
            CombatState state = new CombatState(new GridMap(4, 3), new[] { hero });
            state.SetLoot(new LootContainer(new GridPosition(2, 1), new InventoryItem("cell", "护盾电池")));
            CombatResolver.BeginTurn(state, "hero");
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();

            Assert.That(adapter.InvalidReasonForCell(state, "互动", new GridPosition(1, 2)), Is.EqualTo("该格没有可互动目标"));
            Assert.That(adapter.InvalidReasonForCell(state, "搜刮", new GridPosition(1, 2)), Is.EqualTo("请选择战利品所在格"));
        }

        [Test]
        public void LockedTargetOutsideWeaponRange_IsPreviewedAsBlocked()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(5, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(6, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, "hero");

            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(state, "攻击", "enemy");

            Assert.That(preview.CanSubmit, Is.False);
            Assert.That(preview.FailureReason, Is.EqualTo("目标超出武器射程"));
        }

        [Test]
        public void Preview_ExplainsResourceCooldownLineOfSightAndEnemyTurnBlocks()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();

            UnitState manaHero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            SkillDefinition expensiveSkill = new SkillDefinition("expensive", "高耗能术式", DamageType.Fire, 3, 4, 7, 1);
            manaHero.Equip(manaHero.MainHand, manaHero.OffHand, expensiveSkill, manaHero.SkillTwo);
            CombatState manaState = new CombatState(new GridMap(4, 2), new[] { manaHero, new UnitState("enemy", false, new GridPosition(2, 0), Facing.West) });
            CombatResolver.BeginTurn(manaState, "hero");
            Assert.That(adapter.BuildPreview(manaState, "技能1", "enemy").FailureReason, Does.Contain("以太不足"));

            UnitState cooldownHero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            CombatState cooldownState = new CombatState(new GridMap(4, 2), new[] { cooldownHero, new UnitState("enemy", false, new GridPosition(2, 0), Facing.West) });
            CombatResolver.BeginTurn(cooldownState, "hero");
            CombatResolver.Resolve(cooldownState, CombatCommand.UseSkill("hero", 0, "enemy"));
            Assert.That(adapter.BuildPreview(cooldownState, "技能1", "enemy").FailureReason, Does.Contain("冷却"));

            GridMap coveredMap = new GridMap(5, 2);
            coveredMap.SetTile(new GridPosition(1, 0), new TileState { Cover = CoverType.Heavy, Durability = 5 });
            UnitState sightHero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            CombatState sightState = new CombatState(coveredMap, new[] { sightHero, new UnitState("enemy", false, new GridPosition(3, 0), Facing.West) });
            CombatResolver.BeginTurn(sightState, "hero");
            Assert.That(adapter.BuildPreview(sightState, "攻击", "enemy").FailureReason, Does.Contain("阻挡"));

            UnitState waitingHero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState activeEnemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            CombatState waitingState = new CombatState(new GridMap(4, 2), new[] { waitingHero, activeEnemy });
            CombatResolver.BeginTurn(waitingState, "enemy");
            Assert.That(adapter.BuildPreview(waitingState, "移动", null).FailureReason, Does.Contain("等待敌方行动"));
        }
    }
}
