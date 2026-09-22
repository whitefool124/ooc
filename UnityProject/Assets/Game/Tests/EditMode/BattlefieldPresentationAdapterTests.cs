using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class BattlefieldPresentationAdapterTests
    {
        [Test]
        public void BoardRect_PreservesSeventyFivePercentCombatRegion()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            // 定案后战场固定 6 倍档（一格 192），盘面大于一屏时靠平移查看；
            // 这里用一块装得下的小盘面继续守住"棋盘落在 75% 战斗区内"的约束。
            BattlefieldRect board = adapter.BoardRect(6, 3);
            Assert.That(board.Width, Is.EqualTo(1344f));
            Assert.That(board.Height, Is.EqualTo(672f));
            Assert.That(board.X, Is.GreaterThanOrEqualTo(adapter.ViewportRect.X));
            Assert.That(board.Y, Is.GreaterThanOrEqualTo(BattlefieldPresentationAdapter.BoardTop));
            Assert.That(board.XMax, Is.LessThanOrEqualTo(adapter.ViewportRect.XMax));
            Assert.That(board.YMax, Is.LessThanOrEqualTo(adapter.ViewportRect.YMax));
            Assert.That(BattlefieldPresentationAdapter.CellSize % 32f, Is.Zero);
            Assert.That(BattlefieldPresentationAdapter.CellSize, Is.EqualTo(192f), "定案：6 倍档 = 32 原生像素 × 6");
        }

        [Test]
        public void Viewport_DefaultScaleAndFocus_UseOnlyApprovedIntegerSteps()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            Assert.That(viewport.CellSize, Is.EqualTo(192f));
            // 定案档是阶梯下限之上的档位，因此可以再缩小一档看更多盘面。
            Assert.That(viewport.ZoomAt(720f, 450f, -1), Is.True);
            Assert.That(viewport.CellSize, Is.EqualTo(160f));
            viewport.ZoomAt(720f, 450f, 1);
            viewport.ZoomAt(720f, 450f, 1);
            Assert.That(viewport.CellSize, Is.EqualTo(224f));
            viewport.Focus(new GridPosition(6, 4));
            BattlefieldRect focused = viewport.CellRect(new GridPosition(6, 4));
            Assert.That(focused.X, Is.GreaterThanOrEqualTo(viewport.ViewportRect.X - .01f));
            Assert.That(focused.Y, Is.GreaterThanOrEqualTo(viewport.ViewportRect.Y - .01f));
            Assert.That(focused.XMax, Is.LessThanOrEqualTo(viewport.ViewportRect.XMax + .01f));
            Assert.That(focused.YMax, Is.LessThanOrEqualTo(viewport.ViewportRect.YMax + .01f));
        }

        [Test]
        public void Viewport_OpensAtTheClosestIntegerTierThatFitsTheWholeBoard()
        {
            // 定案后开战档不再随盘面缩小：任何盘面都至少 192（6 倍），装不下就靠平移查看。
            foreach (int[] size in new[] { new[] { 10, 7 }, new[] { 11, 8 }, new[] { 12, 9 } })
            {
                BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport(size[0], size[1]);
                Assert.That(viewport.CellSize, Is.EqualTo(192f), size[0] + "x" + size[1]);
                Assert.That(viewport.CellSize % 32f, Is.Zero, "缩放档位必须落在原生 32 格上");
                // 缩放回退回到该盘自己的开战档位，而不是某个全局常量。
                viewport.ZoomAt(700f, 400f, 1);
                Assert.That(viewport.CellSize, Is.EqualTo(224f));
                viewport.ResetOverview();
                Assert.That(viewport.CellSize, Is.EqualTo(192f));
            }
            Assert.That(BattlefieldPresentationAdapter.DefaultCellSize(12, 9), Is.EqualTo(192f));
            // 小盘面仍按最紧的整数档放大，但不会超过最大档。
            Assert.That(BattlefieldPresentationAdapter.DefaultCellSize(1, 1),
                Is.EqualTo(BattlefieldPresentationAdapter.MaximumCellSize));
            Assert.That(BattlefieldPresentationAdapter.DefaultCellSize(6, 3), Is.EqualTo(224f));
        }

        [Test]
        public void Viewport_PanIsClampedToContentBoundsAndFittingBoardsStayCentred()
        {
            // 棋盘装得下时不允许平移，只会居中：这是设计行为，也是落点/范围标记定位的前提。
            // 定案后 6 倍档下只有小盘面装得下，因此这里用小盘面继续守住该行为。
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport(6, 3);
            viewport.Pan(10000f, 10000f);
            Assert.That(viewport.BoardRect.Width, Is.LessThan(viewport.ViewportRect.Width));
            Assert.That(viewport.BoardRect.X, Is.EqualTo(viewport.ViewportRect.X +
                (viewport.ViewportRect.Width - viewport.BoardRect.Width) / 2f).Within(.01f));
            Assert.That(viewport.BoardRect.Y, Is.EqualTo(viewport.ViewportRect.Y +
                (viewport.ViewportRect.Height - viewport.BoardRect.Height) / 2f).Within(.01f));

            // 定案档下 12x9 盘两轴都超出视口，平移才会被夹在越界容差内。
            BattlefieldViewport board = new BattlefieldPresentationAdapter().CreateViewport();
            Assert.That(board.BoardRect.Width, Is.GreaterThan(board.ViewportRect.Width));
            Assert.That(board.BoardRect.Height, Is.GreaterThan(board.ViewportRect.Height));
            while (board.CellSize < BattlefieldPresentationAdapter.MaximumCellSize) board.ZoomAt(720f, 450f, 1);
            Assert.That(board.BoardRect.Width, Is.GreaterThan(board.ViewportRect.Width));
            Assert.That(board.BoardRect.Height, Is.GreaterThan(board.ViewportRect.Height));
            float overscroll = board.CellSize * BattlefieldViewport.EdgeOverscrollCells;
            board.Pan(10000f, 10000f);
            Assert.That(board.BoardRect.X, Is.LessThanOrEqualTo(board.ViewportRect.X + overscroll));
            Assert.That(board.BoardRect.Y, Is.LessThanOrEqualTo(board.ViewportRect.Y + overscroll));
            board.Pan(-10000f, -10000f);
            Assert.That(board.BoardRect.XMax, Is.GreaterThanOrEqualTo(board.ViewportRect.XMax - overscroll));
            Assert.That(board.BoardRect.YMax, Is.GreaterThanOrEqualTo(board.ViewportRect.YMax - overscroll));
        }

        [Test]
        public void Viewport_PanAndZoomRemainPixelAlignedForCrispTileEdges()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            viewport.Pan(13.4f, -8.7f);
            viewport.ZoomAt(517.25f, 366.75f, 1);

            Assert.That(viewport.BoardRect.X % 1f, Is.Zero);
            Assert.That(viewport.BoardRect.Y % 1f, Is.Zero);
            BattlefieldRect cell = viewport.CellRect(new GridPosition(3, 2));
            Assert.That(cell.X % 1f, Is.Zero);
            Assert.That(cell.Y % 1f, Is.Zero);
        }

        [Test]
        public void Viewport_SafeEdgeFollow_DoesNotRecentreAComfortablyVisibleHero()
        {
            // 定案 6 倍档（192）视口只有 4 格高，1.5 格安全内缩后不存在"舒适可见"的格子；
            // 该规则改在 3 倍档（96，视口 8 格高）上验证，行为语义不变。
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            while (viewport.CellSize > 96f) viewport.ZoomAt(720f, 450f, -1);
            Assert.That(viewport.CellSize, Is.EqualTo(96f));
            viewport.Focus(new GridPosition(6, 4));
            Assert.That(viewport.IsNearSafeEdge(new GridPosition(6, 4)), Is.False);
            Assert.That(viewport.IsNearSafeEdge(new GridPosition(0, 8)), Is.True);
        }

        [Test]
        public void Viewport_FocusOnOuterRow_LeavesUnitAndBoundaryBelowTopHud()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            viewport.ZoomAt(720f, 450f, 1);
            viewport.Focus(new GridPosition(5, 8));

            BattlefieldRect topCell = viewport.CellRect(new GridPosition(5, 8));
            Assert.That(topCell.Y, Is.EqualTo(viewport.ViewportRect.Y +
                viewport.CellSize * BattlefieldViewport.EdgeOverscrollCells).Within(.01f));
            Assert.That(topCell.Y, Is.GreaterThan(100f));
        }

        [Test]
        public void ViewportInput_SpaceLeftDragTracksLifecycleAndPans()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            // 拖拽平移要生效，棋盘必须先宽于视口；12x9 盘在 96px 时仍装得下，故拉到最大档。
            while (viewport.CellSize < BattlefieldPresentationAdapter.MaximumCellSize) viewport.ZoomAt(720f, 450f, 1);
            var input = new BattlefieldViewportInputController();
            input.HandleGuiEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space }, viewport, new GridPosition(6, 4));
            Assert.That(input.IsSpaceHeld, Is.True);

            Vector2 pointer = new Vector2(viewport.ViewportRect.X + 200f, viewport.ViewportRect.Y + 200f);
            input.HandleGuiEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = pointer }, viewport, new GridPosition(6, 4));
            Assert.That(input.IsPrimaryPanning, Is.True);
            BattlefieldRect before = viewport.BoardRect;
            input.HandleGuiEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = pointer, delta = new Vector2(32f, -32f) }, viewport, new GridPosition(6, 4));
            Assert.That(viewport.BoardRect.X, Is.Not.EqualTo(before.X));

            input.HandleGuiEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = pointer }, viewport, new GridPosition(6, 4));
            input.HandleGuiEvent(new Event { type = EventType.KeyUp, keyCode = KeyCode.Space }, viewport, new GridPosition(6, 4));
            Assert.That(input.IsPrimaryPanning, Is.False);
            Assert.That(input.IsSpaceHeld, Is.False);
        }

        [Test]
        public void ViewportInput_HomeRefocusesAndSideButtonRequiresPointerInsideViewport()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            BattlefieldViewport viewport = adapter.CreateViewport();
            GridPosition hero = new GridPosition(6, 4);
            viewport.Pan(-1000f, -1000f);
            var input = new BattlefieldViewportInputController();
            input.HandleGuiEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Home }, viewport, hero);
            BattlefieldViewport expected = adapter.CreateViewport();
            expected.Focus(hero);
            Assert.That(viewport.BoardRect.X, Is.EqualTo(expected.BoardRect.X));
            Assert.That(viewport.BoardRect.Y, Is.EqualTo(expected.BoardRect.Y));

            input.UpdateSideButtonPan(viewport, true, true, true, new Vector2(-20f, -20f), new Vector2(24f, 0f), 1f);
            Assert.That(input.IsSideButtonPanning, Is.False);
            Vector2 inside = new Vector2(viewport.ViewportRect.X + 100f, viewport.ViewportRect.Y + 100f);
            input.UpdateSideButtonPan(viewport, true, true, true, inside, new Vector2(24f, 0f), 1f);
            Assert.That(input.IsSideButtonPanning, Is.True);
            input.UpdateSideButtonPan(viewport, true, false, false, inside, Vector2.zero, 1f);
            Assert.That(input.IsSideButtonPanning, Is.False);
        }

        [Test]
        public void ViewportInput_ScreenCoordinatesRespectLetterboxedReferenceCanvas()
        {
            Vector2 reference = BattlefieldViewportInputController.ScreenToReferenceUi(
                new Vector2(960f, 540f), 1920f, 1080f, 1920f, 1080f);
            Assert.That(reference, Is.EqualTo(new Vector2(960f, 540f)));
            Vector2 letterboxed = BattlefieldViewportInputController.ScreenToReferenceUi(
                new Vector2(960f, 720f), 1920f, 1440f, 1920f, 1080f);
            Assert.That(letterboxed, Is.EqualTo(new Vector2(960f, 540f)));
        }

        [TestCase(1f)]
        [TestCase(.5f)]
        public void Overview_AllCellsUnitsAndIntentBadgesFitAndKeepNativePixels(float screenScale)
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            BattlefieldRect safe = viewport.ViewportRect;
            for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++)
            {
                BattlefieldRect cell = viewport.CellRect(new GridPosition(x, y));
                Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
                Rect badge = CombatUnitHudLayout.EnemyIntentBadgeRect(cell, 5);
                Rect shield = CombatUnitHudLayout.UnitShieldBadgeRect(cell);
                // 定案 6 倍档下盘面大于视口，"全部装进视口"的前提不再成立；
                // 改为守住真实不变量：三者都以所属格为锚、越界量有确定上界（单位画布 2×2 格、底部对齐，
                // 意图徽章浮在单位上沿，护盾读数与生命条共用格底基线），且都保持原生像素对齐。
                Assert.That(unit.width, Is.LessThanOrEqualTo(cell.Width * 2f + .01f));
                Assert.That(unit.xMin, Is.GreaterThanOrEqualTo(cell.X - cell.Width * .5f - .01f));
                Assert.That(unit.xMax, Is.LessThanOrEqualTo(cell.XMax + cell.Width * .5f + .01f));
                Assert.That(unit.yMin, Is.GreaterThanOrEqualTo(cell.Y - cell.Height - .01f));
                Assert.That(unit.yMax, Is.LessThanOrEqualTo(cell.YMax + .01f));
                Assert.That(badge.yMin, Is.GreaterThanOrEqualTo(cell.Y - cell.Height - .01f));
                Assert.That(shield.yMax, Is.LessThanOrEqualTo(cell.YMax + cell.Height * .5f + .01f));
                Assert.That(unit.width * screenScale % 1f, Is.Zero);
                Assert.That(cell.Width * screenScale / 32f % 1f, Is.Zero);
                Assert.That(unit.x * screenScale % 1f, Is.Zero);
                Assert.That(unit.y * screenScale % 1f, Is.Zero);
            }
            viewport.ZoomAt(720f, 400f, 1);
            viewport.Pan(17.3f, 27.4f);
            viewport.ResetOverview();
            // 定案 6 倍档下 12x9 盘不可能整屏装下：复位后应回到默认原点对齐、完整覆盖视口，且保持像素对齐。
            Assert.That(viewport.CellSize, Is.EqualTo(BattlefieldPresentationAdapter.CellSize));
            Assert.That(viewport.BoardRect.X % 1f, Is.Zero);
            Assert.That(viewport.BoardRect.Y % 1f, Is.Zero);
            Assert.That(viewport.BoardRect.X, Is.LessThanOrEqualTo(safe.X));
            Assert.That(viewport.BoardRect.XMax, Is.GreaterThanOrEqualTo(safe.XMax));
            Assert.That(viewport.BoardRect.Y, Is.LessThanOrEqualTo(safe.Y));
            Assert.That(viewport.BoardRect.YMax, Is.GreaterThanOrEqualTo(safe.YMax));
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
            Assert.That(adapter.TryResolveCell(board, 12, 9, board.XMax + 1f, centerY, out _), Is.False);
        }

        [Test]
        public void DirectionAndDistance_AreDeterministic()
        {
            GridPosition origin = new GridPosition(2, 2);
            Assert.That(BattlefieldPresentationAdapter.Distance(origin, new GridPosition(5, 3)), Is.EqualTo(4));
            Assert.That(BattlefieldPresentationAdapter.DirectionToward(origin, new GridPosition(5, 3)), Is.EqualTo(CardinalDirection.East));
            Assert.That(BattlefieldPresentationAdapter.StepToward(origin, new GridPosition(1, 5)), Is.EqualTo(new GridPosition(2, 3)));
        }

        [Test]
        public void AttackPreview_ReportsRuleCostAndDeterministicDamage()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 1));
            CombatState state = new CombatState(new GridMap(4, 3), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, "hero");
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();

            CombatActionPreview preview = adapter.BuildPreview(state, "攻击", "enemy");

            Assert.That(preview.TargetRule, Does.Contain("可见敌人"));
            Assert.That(preview.Cost, Is.EqualTo("1 行动点"));
            Assert.That(preview.ExpectedResult, Does.Contain("预计生命"));
            Assert.That(preview.FailureReason, Is.Empty);
        }

        [Test]
        public void EmptyInteractionAndWrongLootCell_HaveExplicitReasons()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
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
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(5, 0));
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

            UnitState manaHero = new UnitState("hero", true, new GridPosition(0, 0));
            SkillDefinition expensiveSkill = new SkillDefinition("expensive", "高耗能术式", DamageType.Fire, 3, 4, 7, 1);
            manaHero.Equip(manaHero.MainHand, manaHero.OffHand, expensiveSkill, manaHero.SkillTwo);
            CombatState manaState = new CombatState(new GridMap(4, 2), new[] { manaHero, new UnitState("enemy", false, new GridPosition(2, 0)) });
            CombatResolver.BeginTurn(manaState, "hero");
            Assert.That(adapter.BuildPreview(manaState, "技能1", "enemy").FailureReason, Does.Contain("以太不足"));

            UnitState cooldownHero = new UnitState("hero", true, new GridPosition(0, 0));
            CombatState cooldownState = new CombatState(new GridMap(4, 2), new[] { cooldownHero, new UnitState("enemy", false, new GridPosition(2, 0)) });
            CombatResolver.BeginTurn(cooldownState, "hero");
            CombatResolver.Resolve(cooldownState, CombatCommand.UseSkill("hero", 0, "enemy"));
            Assert.That(adapter.BuildPreview(cooldownState, "技能1", "enemy").FailureReason, Does.Contain("冷却"));

            GridMap coveredMap = new GridMap(5, 2);
            coveredMap.SetTile(new GridPosition(1, 0), new TileState { Cover = CoverType.Heavy, Durability = 5 });
            UnitState sightHero = new UnitState("hero", true, new GridPosition(0, 0));
            CombatState sightState = new CombatState(coveredMap, new[] { sightHero, new UnitState("enemy", false, new GridPosition(3, 0)) });
            CombatResolver.BeginTurn(sightState, "hero");
            Assert.That(adapter.BuildPreview(sightState, "攻击", "enemy").FailureReason, Does.Contain("挡住了视线"));

            UnitState waitingHero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState activeEnemy = new UnitState("enemy", false, new GridPosition(2, 0));
            CombatState waitingState = new CombatState(new GridMap(4, 2), new[] { waitingHero, activeEnemy });
            CombatResolver.BeginTurn(waitingState, "enemy");
            Assert.That(adapter.BuildPreview(waitingState, "移动", null).FailureReason, Does.Contain("等待敌方行动"));
        }

        [Test]
        public void SkillRule_StatesTheVisibilityRequirementOnlyWhereLineOfSightApplies()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();

            UnitState rangedHero = new UnitState("hero", true, new GridPosition(0, 0));
            SkillDefinition ranged = new SkillDefinition("ranged_probe", "试制远击", DamageType.Fire, 3, 4, 2, 1);
            rangedHero.Equip(rangedHero.MainHand, rangedHero.OffHand, ranged, rangedHero.SkillTwo);
            CombatState rangedState = new CombatState(new GridMap(6, 2), new[]
            {
                rangedHero, new UnitState("enemy", false, new GridPosition(2, 0))
            });
            CombatResolver.BeginTurn(rangedState, "hero");
            Assert.That(adapter.BuildPreview(rangedState, "技能1", null).TargetRule, Does.Contain("可见"),
                "A unit-target skill beyond one cell is blocked by cover, so its rule text must say so.");

            UnitState selfHero = new UnitState("hero", true, new GridPosition(0, 0));
            SkillDefinition selfSkill = new SkillDefinition("self_probe", "试制护幕", SkillTargetRule.Self,
                SkillDeliveryMethod.Direct, 0, 1, 1, CombatFeedbackKind.ShieldRestore,
                new[] { SkillEffectDefinition.RestoreShield(3) });
            selfHero.Equip(selfHero.MainHand, selfHero.OffHand, selfSkill, selfHero.SkillTwo);
            CombatState selfState = new CombatState(new GridMap(6, 2), new[] { selfHero });
            CombatResolver.BeginTurn(selfState, "hero");
            Assert.That(adapter.BuildPreview(selfState, "技能1", null).TargetRule, Does.Not.Contain("可见"),
                "A self skill never needs a sight line.");

            UnitState cellHero = new UnitState("hero", true, new GridPosition(0, 0));
            SkillDefinition cellSkill = new SkillDefinition("cell_probe", "试制相位步", SkillTargetRule.GridCell,
                SkillDeliveryMethod.Direct, 3, 2, 2, CombatFeedbackKind.Movement,
                new[] { SkillEffectDefinition.MoveSource(3) });
            cellHero.Equip(cellHero.MainHand, cellHero.OffHand, cellSkill, cellHero.SkillTwo);
            CombatState cellState = new CombatState(new GridMap(6, 2), new[] { cellHero });
            CombatResolver.BeginTurn(cellState, "hero");
            Assert.That(adapter.BuildPreview(cellState, "技能1", null).TargetRule, Does.Not.Contain("可见"),
                "Cell skills resolve without a sight line, so the label must not claim one.");
        }
    }
}
