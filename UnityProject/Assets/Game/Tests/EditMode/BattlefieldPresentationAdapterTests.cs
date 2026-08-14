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
            BattlefieldRect board = adapter.BoardRect();
            Assert.That(board.Width, Is.EqualTo(1536f));
            Assert.That(board.Height, Is.EqualTo(1152f));
            Assert.That(board.X, Is.LessThanOrEqualTo(0f));
            Assert.That(board.Y, Is.LessThanOrEqualTo(BattlefieldPresentationAdapter.BoardTop));
            Assert.That(BattlefieldPresentationAdapter.CellSize % 32f, Is.Zero);
        }

        [Test]
        public void Viewport_DefaultScaleAndFocus_UseOnlyApprovedIntegerSteps()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            Assert.That(viewport.CellSize, Is.EqualTo(128f));
            viewport.ZoomAt(720f, 450f, -1);
            Assert.That(viewport.CellSize, Is.EqualTo(96f));
            viewport.ZoomAt(720f, 450f, 1);
            viewport.ZoomAt(720f, 450f, 1);
            Assert.That(viewport.CellSize, Is.EqualTo(160f));
            viewport.Focus(new GridPosition(6, 4));
            Assert.That(viewport.BoardRect.X, Is.InRange(-480f, 0f));
            Assert.That(viewport.BoardRect.Y, Is.InRange(-540f, BattlefieldPresentationAdapter.BoardTop));
        }

        [Test]
        public void Viewport_PanAndZoomAnchor_AreClampedToContentBounds()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            BattlefieldRect before = viewport.CellRect(new GridPosition(4, 4));
            float anchorX = before.X + 32f;
            float anchorY = before.Y + 48f;
            viewport.ZoomAt(anchorX, anchorY, 1);
            BattlefieldRect after = viewport.CellRect(new GridPosition(4, 4));
            Assert.That(after.X + 40f, Is.EqualTo(anchorX).Within(.01f));
            Assert.That(after.Y + 60f, Is.EqualTo(anchorY).Within(.01f));
            viewport.Pan(10000f, 10000f);
            Assert.That(viewport.BoardRect.X, Is.LessThanOrEqualTo(viewport.ViewportRect.X));
            Assert.That(viewport.BoardRect.Y, Is.LessThanOrEqualTo(viewport.ViewportRect.Y));
            viewport.Pan(-10000f, -10000f);
            Assert.That(viewport.BoardRect.XMax, Is.GreaterThanOrEqualTo(viewport.ViewportRect.XMax));
            Assert.That(viewport.BoardRect.YMax, Is.GreaterThanOrEqualTo(viewport.ViewportRect.YMax));
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
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
            viewport.Focus(new GridPosition(6, 4));
            Assert.That(viewport.IsNearSafeEdge(new GridPosition(6, 4)), Is.False);
            Assert.That(viewport.IsNearSafeEdge(new GridPosition(0, 4)), Is.True);
        }

        [Test]
        public void ViewportInput_SpaceLeftDragTracksLifecycleAndPans()
        {
            BattlefieldViewport viewport = new BattlefieldPresentationAdapter().CreateViewport();
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
