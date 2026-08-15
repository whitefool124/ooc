using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatHoverInformationTests
    {
        [Test]
        public void CompactTargetSummary_KeepsDecisionDataButLeavesReferenceDetailsForTooltip()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(state, "攻击", enemy.Id);
            EnemyIntentPresentation intent = new EnemyTurnPlanBook().GetPublicIntent(state, enemy, hero);

            string compact = CombatInformationPresenter.BuildCompactTargetSummary(preview, enemy, intent);

            Assert.That(compact, Does.Contain(enemy.DisplayName));
            Assert.That(compact, Does.Contain("生命"));
            Assert.That(compact, Does.Contain("可以行动"));
            Assert.That(compact, Does.Contain(intent.CompactText));
            Assert.That(compact, Does.Not.Contain("武器："));
            Assert.That(compact, Does.Not.Contain("技能："));
            Assert.That(compact, Does.Not.Contain("伤害公式："));
            Assert.That(compact, Does.Not.Contain("有效格"));
        }

        [Test]
        public void TargetTooltip_ContainsExactPreviewEnemyDossierAndAuthoritativeIntent()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(state, "攻击", enemy.Id);
            EnemyIntentPresentation intent = new EnemyTurnPlanBook().GetPublicIntent(state, enemy, hero);

            string details = CombatInformationPresenter.BuildTargetDetails(preview, enemy, intent);

            Assert.That(details, Does.Contain("伤害："));
            Assert.That(details, Does.Contain("敌人资料"));
            Assert.That(details, Does.Contain(enemy.MainHand.DisplayName));
            Assert.That(details, Does.Contain("敌人打算"));
            Assert.That(details, Does.Not.Contain("权威"));
            Assert.That(details, Does.Contain(intent.DetailedText));
        }

        [Test]
        public void OutcomeSummary_KeepsConsequencesVisibleAndMovesRecentEventsToHoverText()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            state.AddLog("事件0");
            state.AddLog("事件1");
            state.ResolveDebugOutcome(false);

            CombatOutcomePresentation outcome = CombatInformationPresenter.BuildOutcome(state, true);

            Assert.That(outcome.CompactDetailText, Does.Contain("从战斗前继续"));
            Assert.That(outcome.CompactDetailText, Does.Not.Contain("事件0"));
            Assert.That(outcome.RecentEventsText, Does.Contain("事件0"));
        }

        [Test]
        public void TooltipPosition_IsClampedInsideReferenceCanvas()
        {
            Rect bounds = new Rect(-960f, -540f, 1920f, 1080f);
            Vector2 size = new Vector2(420f, 430f);

            Vector2 position = FormalHoverTooltip.ClampLocalPosition(bounds, new Vector2(930f, -520f), size, 24f);

            Assert.That(position.x, Is.GreaterThanOrEqualTo(bounds.xMin + 24f));
            Assert.That(position.x + size.x, Is.LessThanOrEqualTo(bounds.xMax - 24f));
            Assert.That(position.y, Is.LessThanOrEqualTo(bounds.yMax - 24f));
            Assert.That(position.y - size.y, Is.GreaterThanOrEqualTo(bounds.yMin + 24f));
        }

        [Test]
        public void InventoryLauncher_UsesRightHudFootprintWithoutOverlappingCombatCommands()
        {
            Rect commands = new Rect(16f, 900f, 1408f, 164f);
            Rect launcher = TarkovInventoryPanel.LauncherRect;

            Assert.That(launcher.xMin, Is.GreaterThanOrEqualTo(BattlefieldPresentationAdapter.BattlefieldWidth));
            Assert.That(launcher.xMax, Is.LessThanOrEqualTo(1920f));
            Assert.That(launcher.Overlaps(commands), Is.False);
        }

        [Test]
        public void EnemyInspectionTarget_OnlySelectsLivingEnemyCells()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });

            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, enemy.Position), Is.EqualTo(enemy.Id));
            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, hero.Position), Is.Null);
            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, new GridPosition(2, 1)), Is.Null);
        }

        [Test]
        public void EnemyGridHover_ContainsDossierAndAuthoritativeIntent()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);

            string details = CombatInformationPresenter.BuildEnemyHoverDetails(state, enemy, intent);

            Assert.That(details, Does.Contain(enemy.MainHand.DisplayName));
            Assert.That(details, Does.Contain("战法："));
            Assert.That(details, Does.Contain("当前意图：" + intent.DetailedText));
            Assert.That(details, Does.Contain("特点："));
            Assert.That(details, Does.Not.Contain("CD"));
        }

        [Test]
        public void EveryEnemyProfile_UsesPlayerFacingSkillEffects()
        {
            string[] forbidden = { "RestoreHealth", "RestoreShield", "RestoreMana", "ApplyStatus", "ClearStatus", "MoveSource", "DamageObject" };
            foreach (EnemyArchetype archetype in EnemyArchetypes.All)
            {
                UnitState enemy = new UnitState("enemy_" + archetype.Id, false, new GridPosition(1, 0), Facing.West);
                archetype.Apply(enemy);
                string details = CombatInformationPresenter.BuildEnemyInformation(enemy).FullText;
                Assert.That(details, Does.Contain("战法："), archetype.Id);
                foreach (string token in forbidden)
                    Assert.That(details, Does.Not.Contain(token), archetype.Id + " exposed " + token);
            }
        }

        [Test]
        public void EnemyHoverCard_StaysInsideBattlefieldAndAboveCommands()
        {
            Rect card = CombatUnitHudLayout.EnemyHoverCardRect(new Vector2(1430f, 890f));

            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(card.xMax, Is.LessThanOrEqualTo(1424f));
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(64f));
            Assert.That(card.yMax, Is.LessThanOrEqualTo(884f));
        }

        [Test]
        public void EnemyIntentBadge_SitsEntirelyAboveTheHealthBar()
        {
            BattlefieldRect cell = new BattlefieldRect(100f, 120f, 76f, 76f);
            Rect badge = CombatUnitHudLayout.EnemyIntentBadgeRect(cell, 8);
            float healthBarTop = CombatUnitHudLayout.UnitHealthBarRect(cell).yMin;

            Assert.That(badge.yMax, Is.LessThan(healthBarTop));
            Assert.That(badge.height, Is.EqualTo(20f));
            Assert.That(badge.width, Is.EqualTo(43f));
        }

        [Test]
        public void EnlargedUnitAndVitalBars_StayInsideTheirCell()
        {
            BattlefieldRect cell = new BattlefieldRect(100f, 120f, 128f, 128f);
            Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
            Rect health = CombatUnitHudLayout.UnitHealthBarRect(cell);
            Rect shield = CombatUnitHudLayout.UnitShieldBarRect(cell);
            Rect firstStatus = CombatUnitHudLayout.UnitStatusIconRect(cell, 0);
            Rect sixthStatus = CombatUnitHudLayout.UnitStatusIconRect(cell, 5);

            Assert.That(unit.xMin, Is.GreaterThanOrEqualTo(cell.X));
            Assert.That(unit.yMin, Is.GreaterThanOrEqualTo(cell.Y));
            Assert.That(unit.xMax, Is.LessThanOrEqualTo(cell.X + cell.Width));
            Assert.That(unit.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));
            Assert.That(health.Overlaps(shield), Is.False);
            Assert.That(health.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));
            Assert.That(shield.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));
            Assert.That(health.width, Is.EqualTo(108f));
            Assert.That(firstStatus.width, Is.EqualTo(14f));
            Assert.That(firstStatus.xMin, Is.GreaterThanOrEqualTo(cell.X));
            Assert.That(sixthStatus.xMax, Is.LessThanOrEqualTo(cell.X + cell.Width));
        }

        [TestCase(64f)]
        [TestCase(96f)]
        [TestCase(128f)]
        [TestCase(160f)]
        public void UnitAndVitalBars_KeepConstantProportionAtEveryZoomStep(float cellSize)
        {
            BattlefieldRect cell = new BattlefieldRect(0f, 0f, cellSize, cellSize);
            Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
            Rect health = CombatUnitHudLayout.UnitHealthBarRect(cell);
            Rect shield = CombatUnitHudLayout.UnitShieldBarRect(cell);

            Assert.That(unit.width / cellSize, Is.EqualTo(116f / 128f).Within(.0001f));
            Assert.That(unit.height / cellSize, Is.EqualTo(116f / 128f).Within(.0001f));
            Assert.That(unit.width * unit.height / (cellSize * cellSize), Is.GreaterThanOrEqualTo(.8f));
            Assert.That(health.width / cellSize, Is.EqualTo(108f / 128f).Within(.0001f));
            Assert.That(health.height / cellSize, Is.EqualTo(17f / 128f).Within(.0001f));
            Assert.That(shield.width / cellSize, Is.EqualTo(108f / 128f).Within(.0001f));
            Assert.That(shield.height / cellSize, Is.EqualTo(15f / 128f).Within(.0001f));
        }

        [Test]
        public void VitalText_HidesAtOverviewAndPreservesForecastAtReadableZooms()
        {
            var vital = new CombatUnitVitalPresentation(8, 12, 3, 5);

            Assert.That(CombatUnitHudLayout.VitalText(vital, 64f), Is.Empty);
            Assert.That(CombatUnitHudLayout.VitalText(vital, 96f), Is.EqualTo("-3→5"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 128f), Is.EqualTo("8 -3 → 5/12"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 160f), Is.EqualTo("8 -3 → 5/12"));
            Assert.That(CombatUnitHudLayout.VitalFontSize(64f, true), Is.Zero);
            Assert.That(CombatUnitHudLayout.VitalFontSize(96f, false), Is.EqualTo(9));
            Assert.That(CombatUnitHudLayout.VitalFontSize(128f, true), Is.EqualTo(14));
            Assert.That(CombatUnitHudLayout.VitalFontSize(160f, false), Is.EqualTo(14));
        }

        [TestCase("hero")]
        [TestCase("rifleman")]
        [TestCase("shieldguard")]
        [TestCase("pyromancer")]
        [TestCase("raider")]
        [TestCase("elite")]
        [TestCase("barrier_mender")]
        [TestCase("lantern_revealer")]
        [TestCase("rune_arbalist")]
        [TestCase("sigil_mauler")]
        [TestCase("stone_snare")]
        [TestCase("tether_hound")]
        public void FormalUnitCrop_RemovesTransparentCanvasWithoutDistortingAspect(string textureName)
        {
            BattlefieldRect cell = new BattlefieldRect(0f, 0f, 128f, 128f);
            Rect uv = CombatUnitHudLayout.UnitTextureCropUv(textureName);
            Rect visible = CombatUnitHudLayout.UnitVisibleContentRect(cell, textureName);

            Assert.That(uv, Is.Not.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(visible.width / visible.height, Is.EqualTo(uv.width / uv.height).Within(.0001f));
            Assert.That(Mathf.Max(visible.width, visible.height), Is.EqualTo(116f).Within(.0001f));
            Assert.That(visible.xMin, Is.GreaterThanOrEqualTo(cell.X));
            Assert.That(visible.yMin, Is.GreaterThanOrEqualTo(cell.Y));
            Assert.That(visible.xMax, Is.LessThanOrEqualTo(cell.X + cell.Width));
            Assert.That(visible.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));
        }

        [Test]
        public void StatusHoverCard_StaysInsideBattlefieldAndAboveCommands()
        {
            Rect card = CombatUnitHudLayout.StatusHoverCardRect(new Vector2(1430f, 890f));

            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(card.xMax, Is.LessThanOrEqualTo(1424f));
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(64f));
            Assert.That(card.yMax, Is.LessThanOrEqualTo(884f));
        }

        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(0, true)]
        public void BattlefieldPanInput_AcceptsMiddleSideButtonsAndSpaceLeft(int button, bool spaceHeld)
        {
            Assert.That(BattlefieldViewportInputController.IsPanButton(button, spaceHeld), Is.True);
        }

        [TestCase(3)]
        [TestCase(4)]
        public void BattlefieldPanInput_SideButtonsAreStandaloneHoldGestures(int button)
        {
            Assert.That(BattlefieldViewportInputController.IsSidePanButton(button), Is.True);
            Assert.That(BattlefieldViewportInputController.IsPanButton(button, false), Is.True);
        }

        [Test]
        public void PlayerTurnEnd_RequiresAnExplicitPlayerRequest()
        {
            CombatCommand playerEnd = CombatCommand.EndTurn("hero");

            Assert.That(CombatPrototypeBootstrap.CanSubmitTurnCommand(playerEnd, false), Is.False);
            Assert.That(CombatPrototypeBootstrap.CanSubmitTurnCommand(playerEnd, true), Is.True);
            Assert.That(CombatPrototypeBootstrap.CanSubmitTurnCommand(CombatCommand.EndTurn("enemy"), false), Is.True);
            Assert.That(CombatPrototypeBootstrap.CanSubmitTurnCommand(
                CombatCommand.Move("hero", new GridPosition(1, 0), Facing.East), false), Is.True);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        public void BattlefieldPanInput_PreservesCellClickAndInspectionButtons(int button, bool spaceHeld)
        {
            Assert.That(BattlefieldViewportInputController.IsPanButton(button, spaceHeld), Is.False);
        }

        [Test]
        public void HudDecisionSummary_KeepsTargetLegalityAndResultWhileCostLivesInIconChips()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(state, "攻击", enemy.Id);

            string summary = CombatInformationPresenter.BuildHudDecisionSummary(preview, enemy, false);

            Assert.That(summary, Does.Contain("攻击"));
            Assert.That(summary, Does.Not.Contain("AP"));
            Assert.That(summary, Does.Contain("可执行"));
            Assert.That(summary, Does.Contain(enemy.DisplayName));
            Assert.That(summary, Does.Contain("预计"));
            Assert.That(summary.Split('\n').Length, Is.EqualTo(2));
        }

        [Test]
        public void CancelResolution_ClearsInspectionThenActionBeforeRequestingLeave()
        {
            Assert.That(CombatSelectionNavigation.ResolveCancel("攻击", "enemy", true), Is.EqualTo(CombatCancelResolution.ClearTarget));
            Assert.That(CombatSelectionNavigation.ResolveCancel("攻击", null, false), Is.EqualTo(CombatCancelResolution.ResetAction));
            Assert.That(CombatSelectionNavigation.ResolveCancel("移动", null, true), Is.EqualTo(CombatCancelResolution.ResetAction));
            Assert.That(CombatSelectionNavigation.ResolveCancel("移动", null, false), Is.EqualTo(CombatCancelResolution.RequestLeave));
        }

        [Test]
        public void KeyboardTargetNavigation_ClampsMovementAndStopsMutatingAfterCancel()
        {
            var navigation = new CombatTargetNavigationState();
            navigation.Begin(new GridPosition(1, 1), 3, 2);

            navigation.Move(9, 9, 3, 2);
            Assert.That(navigation.Position, Is.EqualTo(new GridPosition(2, 1)));
            navigation.Move(-9, -9, 3, 2);
            Assert.That(navigation.Position, Is.EqualTo(new GridPosition(0, 0)));

            navigation.End();
            navigation.Move(1, 1, 3, 2);
            Assert.That(navigation.Active, Is.False);
            Assert.That(navigation.Position, Is.EqualTo(new GridPosition(0, 0)));
        }
    }
}
