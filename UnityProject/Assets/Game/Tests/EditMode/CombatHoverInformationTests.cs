using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class CombatHoverInformationTests
    {
        [Test]
        public void FormalHoverTooltip_IsPersistedAsReusableResourcePrefab()
        {
            FormalHoverTooltip prefab = Resources.Load<FormalHoverTooltip>(FormalHoverTooltip.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>(), Is.Null);
            Assert.That(prefab.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void CompactTargetSummary_KeepsDecisionDataButLeavesReferenceDetailsForTooltip()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
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
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
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
            Assert.That(CombatInformationPresenter.BuildActionDetails(preview), Does.Contain("消耗　"));
            Assert.That(CombatInformationPresenter.BuildActionDetails(preview).Split('\n').Count(line => !string.IsNullOrWhiteSpace(line)), Is.LessThanOrEqualTo(7));
        }

        [Test]
        public void RogueSpellTooltipCopy_UsesPlayerLanguageInsteadOfRuntimeRuleIds()
        {
            OCC.Combat.Roguelite.SpellDefinition spell = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01().Spells.Single(value => value.DefinitionId == "BASE-FIRE-MELEE");
            string target = RogueliteSettlementPresentation.RogueSpellTargetSummary(spell);
            string effect = RogueliteSettlementPresentation.RogueSpellPlayerSummary(spell);

            Assert.That(target, Does.Contain("相邻"));
            Assert.That(effect, Does.Contain("8"));
            Assert.That(target + effect, Does.Not.Contain("_"));
            Assert.That(target + effect, Does.Not.Contain("damage:"));
        }

        [Test]
        public void OutcomeSummary_KeepsConsequencesVisibleAndMovesRecentEventsToHoverText()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            state.AddLog("事件0");
            state.AddLog("事件1");
            state.ResolveDebugOutcome(false);

            CombatOutcomePresentation outcome = CombatInformationPresenter.BuildOutcome(state, true);

            Assert.That(outcome.CompactDetailText, Does.Contain("回到地图，或者从头再挑战一次"));
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
        public void HeaderZones_DoNotOverlapAndStayInsideTheHeaderPanel()
        {
            Rect resources = FormalCombatHud.HeaderResourceZone;
            Rect actions = FormalCombatHud.HeaderActionsZone;
            Rect header = new Rect(0f, 0f, FormalCombatHud.HeaderPanelWidth, 56f);

            Assert.That(resources.Overlaps(actions), Is.False, "resource row overlaps the action buttons");
            Assert.That(header.Contains(resources.min) && header.Contains(resources.max), Is.True);
            Assert.That(header.Contains(actions.min) && header.Contains(actions.max), Is.True);
        }

        [Test]
        public void HeaderResourceChips_HaveRaycastHitAreasAndTooltipTriggers()
        {
            GameObject canvasRoot = new GameObject("header-tooltip-canvas", typeof(RectTransform), typeof(Canvas));
            GameObject hudObject = new GameObject("header-tooltip-hud", typeof(FormalCombatHud));
            GameObject header = new GameObject("header", typeof(RectTransform));
            header.transform.SetParent(canvasRoot.transform, false);
            try
            {
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                FormalHoverTooltip tooltip = FormalHoverTooltip.Create(canvasRoot.GetComponent<Canvas>());
                typeof(FormalCombatHud).GetField("tooltip", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(hud, tooltip);
                MethodInfo build = typeof(FormalCombatHud).GetMethod("BuildHeaderResources",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(build, Is.Not.Null);
                build.Invoke(hud, new object[] { header.transform });

                string[] names = { "资源_生命", "资源_金币", "资源_学院贡献", "资源_剩余时间" };
                foreach (string name in names)
                {
                    Transform chip = header.transform.Find(name);
                    Assert.That(chip, Is.Not.Null, name);
                    Assert.That(chip.GetComponent<Image>()?.raycastTarget, Is.True, name + " should receive pointer hover");
                    Assert.That(chip.GetComponent<FormalHoverTooltipTrigger>(), Is.Not.Null, name + " should open the shared tooltip");
                    Assert.That(chip.Find("图标").GetComponent<Image>().raycastTarget, Is.False,
                        name + " glyph must let the parent hit area receive hover");
                }
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void EnemyInspectionTarget_OnlySelectsLivingEnemyCells()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });

            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, enemy.Position), Is.EqualTo(enemy.Id));
            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, hero.Position), Is.Null);
            Assert.That(CombatInformationPresenter.EnemyInspectionTargetAt(state, new GridPosition(2, 1)), Is.Null);
        }

        [Test]
        public void EnemyGridHover_ContainsDossierAndAuthoritativeIntent()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
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
                UnitState enemy = new UnitState("enemy_" + archetype.Id, false, new GridPosition(1, 0));
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
        public void EnemyIntentBadge_IsRaisedIntoTheClearanceAboveTheUnitHead()
        {
            BattlefieldRect cell = new BattlefieldRect(100f, 120f, 76f, 76f);
            Rect badge = CombatUnitHudLayout.EnemyIntentBadgeRect(cell, 8);
            float healthBarTop = CombatUnitHudLayout.UnitHealthBarRect(cell).yMin;

            Assert.That(badge.yMin, Is.LessThan(cell.Y));
            Assert.That(badge.yMax, Is.LessThanOrEqualTo(cell.Y + 28.5f));
            Assert.That(badge.yMax, Is.LessThan(healthBarTop));
            Assert.That(badge.height, Is.EqualTo(56f));
            Assert.That(badge.width, Is.EqualTo(92f));
        }

        [Test]
        public void EnlargedUnit_OverhangsWithoutChangingCellAndVitalsRemainBelowFeet()
        {
            BattlefieldRect cell = new BattlefieldRect(100f, 120f, 128f, 128f);
            Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
            Rect health = CombatUnitHudLayout.UnitHealthBarRect(cell, true);
            Rect shield = CombatUnitHudLayout.UnitShieldBadgeRect(cell);
            Rect firstStatus = CombatUnitHudLayout.UnitStatusIconRect(cell, 0);
            Rect sixthStatus = CombatUnitHudLayout.UnitStatusIconRect(cell, 5);

            Assert.That(unit.center.x, Is.EqualTo(cell.X + cell.Width / 2f));
            Assert.That(unit.yMin, Is.LessThan(cell.Y));
            Assert.That(unit.width, Is.EqualTo(cell.Width * 2f));
            Assert.That(unit.yMax, Is.LessThanOrEqualTo(cell.Y + cell.Height));
            Assert.That(health.Overlaps(shield), Is.False);
            Assert.That(health.yMin, Is.GreaterThanOrEqualTo(unit.yMin + unit.height * 58f / 64f));
            Assert.That(shield.yMin, Is.EqualTo(health.yMin));
            Assert.That(health.width, Is.EqualTo(128f));
            Assert.That(health.center.x, Is.EqualTo(cell.X + cell.Width * .5f));
            Assert.That(firstStatus.width, Is.EqualTo(32f));
            Assert.That(firstStatus.xMin, Is.GreaterThanOrEqualTo(cell.X));
            Assert.That(sixthStatus.xMax, Is.LessThanOrEqualTo(cell.X + cell.Width));
        }

        [TestCase(64f)]
        [TestCase(96f)]
        [TestCase(128f)]
        [TestCase(160f)]
        public void UnitUsesWholeNativeScaleWhileVitalBarsKeepFixedPixelsAtEveryZoomStep(float cellSize)
        {
            BattlefieldRect cell = new BattlefieldRect(0f, 0f, cellSize, cellSize);
            Rect unit = CombatUnitHudLayout.UnitPresentationRect(cell);
            Rect health = CombatUnitHudLayout.UnitHealthBarRect(cell, true);
            Rect shield = CombatUnitHudLayout.UnitShieldBadgeRect(cell);

            Assert.That(unit.width % 64f, Is.Zero.Within(.0001f));
            Assert.That(unit.height, Is.EqualTo(unit.width));
            Assert.That(unit.center.x, Is.EqualTo(cell.X + cell.Width * .5f).Within(.0001f));
            Assert.That(unit.yMax, Is.EqualTo(cell.Y + cell.Height).Within(.0001f));
            Assert.That(health.width, Is.EqualTo(128f).Within(.0001f));
            Assert.That(health.height, Is.EqualTo(16f).Within(.0001f));
            Assert.That(health.center.x, Is.EqualTo(cell.X + cell.Width * .5f).Within(.0001f));
            Rect mana = CombatUnitHudLayout.UnitManaBarRect(cell);
            Assert.That(mana.width, Is.EqualTo(128f).Within(.0001f));
            Assert.That(mana.height, Is.EqualTo(8f).Within(.0001f));
            Assert.That(mana.center.x, Is.EqualTo(health.center.x).Within(.0001f));
            Assert.That(shield.width, Is.EqualTo(32f).Within(.0001f));
            Assert.That(shield.height, Is.EqualTo(16f).Within(.0001f));
        }

        [Test]
        public void VitalText_RemainsVisibleAndFixedAcrossZoomSteps()
        {
            var vital = new CombatUnitVitalPresentation(8, 12, 3, 5);

            Assert.That(CombatUnitHudLayout.VitalText(vital, 64f), Is.EqualTo("5/12"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 96f), Is.EqualTo("5/12"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 128f), Is.EqualTo("5/12"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 160f), Is.EqualTo("5/12"));
            Assert.That(CombatUnitHudLayout.VitalText(vital, 256f), Is.EqualTo("5/12"));
            Assert.That(CombatUnitHudLayout.VitalFontSize(64f, true), Is.EqualTo(12));
            Assert.That(CombatUnitHudLayout.VitalFontSize(256f, false), Is.EqualTo(12));
        }

        [Test]
        public void UnitHealthPalette_DistinguishesEnemyAndKeepsForecastReadable()
        {
            Assert.That(CombatUnitHudLayout.HealthFillColor(true), Is.EqualTo(FormalUiTheme.Health));
            Assert.That(CombatUnitHudLayout.HealthFillColor(false), Is.EqualTo(FormalUiTheme.Danger));
            Assert.That(CombatUnitHudLayout.HealthForecastColor(false), Is.EqualTo(FormalUiTheme.Amber));
        }

        [Test]
        public void BattlefieldCellPointer_UsesRightButtonPressForImmediateInspection()
        {
            Assert.That(FormalBattlefieldView.ShouldInspectOnPointerDown(PointerEventData.InputButton.Right), Is.True);
            Assert.That(FormalBattlefieldView.ShouldInspectOnPointerDown(PointerEventData.InputButton.Left), Is.False);
            Assert.That(FormalBattlefieldView.ShouldInspectOnPointerDown(PointerEventData.InputButton.Middle), Is.False);
        }

        [Test]
        public void BattlefieldHover_RevealsAfterTwoSecondsAndUsesIndependentOrderedWindows()
        {
            Assert.That(FormalBattlefieldView.CellHoverRevealDelaySeconds, Is.EqualTo(.35f));
            Assert.That(FormalBattlefieldView.CellHoverWarmRevealDelaySeconds, Is.EqualTo(.10f));
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(.349f, true, true, false), Is.False);
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(.35f, true, true, false), Is.True);
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(.099f, true, true, false,
                FormalBattlefieldView.CellHoverWarmRevealDelaySeconds), Is.False);
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(.10f, true, true, false,
                FormalBattlefieldView.CellHoverWarmRevealDelaySeconds), Is.True);
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(3f, true, false, false), Is.False);
            Assert.That(FormalBattlefieldView.ShouldRevealCellHover(3f, true, true, true), Is.False);
            GameObject prefab = Resources.Load<GameObject>(BattlefieldHoverCardView.ResourcePath);

            Assert.That(prefab, Is.Not.Null);
            BattlefieldHoverCardView view = prefab.GetComponent<BattlefieldHoverCardView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredBindings, Is.True);
            Assert.That(prefab.transform.GetChild(0).name, Is.EqualTo("单位悬浮窗"));
            Assert.That(prefab.transform.childCount, Is.EqualTo(10));
            Assert.That(prefab.transform.GetChild(1).name, Is.EqualTo("状态悬浮窗 1"));
            Assert.That(prefab.transform.GetChild(6).name, Is.EqualTo("状态悬浮窗 6"));
            Assert.That(prefab.transform.GetChild(7).name, Is.EqualTo("地表瓦片悬浮窗"));
            Assert.That(prefab.transform.GetChild(8).name, Is.EqualTo("地形效果悬浮窗"));
            Assert.That(prefab.transform.GetChild(9).name, Is.EqualTo("物块悬浮窗"));

            RectTransform unit = (RectTransform)prefab.transform.GetChild(0);
            Assert.That(unit.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void BattlefieldHover_TerrainCategoriesUseOneSentenceAndOmitMissingKinds()
        {
            CombatState state = new CombatState(new GridMap(2, 2), new[]
                { new UnitState("hero", true, new GridPosition(0, 0)) });
            GridPosition position = new GridPosition(1, 0);
            TileState tile = state.Map.GetTile(position);
            Assert.That(CombatBattlefieldCellPresenter.BuildSurfaceHover(null, position),
                Is.EqualTo("学院地坪，可正常通行。"));
            Assert.That(CombatBattlefieldCellPresenter.BuildTerrainEffectHover(state, null, tile, position), Is.Empty);
            Assert.That(CombatBattlefieldCellPresenter.BuildObjectHover(state, tile, position), Is.Empty);

            state.Map.SetTile(position, new TileState { IsWater = true });
            tile = state.Map.GetTile(position);
            string effect = CombatBattlefieldCellPresenter.BuildTerrainEffectHover(state, null, tile, position);
            Assert.That(effect, Does.StartWith("浅水"));
            Assert.That(effect.Count(value => value == '。'), Is.EqualTo(1));
        }

        [Test]
        public void CompactEnemyIntent_UsesOneShortActionAndDamageReadout()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            EnemyIntentPresentation intent = new EnemyTurnPlanBook().GetPublicIntent(state, enemy, hero);

            string compact = FormalBattlefieldView.CompactIntent(intent);

            Assert.That(compact, Does.StartWith(intent.ActionName));
            Assert.That(compact, Does.Not.Contain(intent.TargetSummary));
            Assert.That(compact, Does.Not.Contain(intent.ResultSummary));
            Assert.That(compact.Length, Is.LessThanOrEqualTo(intent.ActionName.Length + 4));
        }

        [Test]
        public void MoveIntentDestination_IsCollectedForBattlefieldHighlight()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            GridPosition destination = new GridPosition(1, 0);
            EnemyIntentPresentation move = CombatInformationPresenter.BuildEnemyIntent(state, enemy,
                CombatCommand.Move(enemy.Id, destination));

            HashSet<GridPosition> destinations = FormalBattlefieldView.CollectIntentDestinations(new[] { null, move });

            Assert.That(destinations, Is.EquivalentTo(new[] { destination }));
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
        public void FormalUnitCanvas_PreservesCompleteEquipmentSilhouetteAtSquareAspect(string textureName)
        {
            BattlefieldRect cell = new BattlefieldRect(0f, 0f, 128f, 128f);
            Rect uv = CombatUnitHudLayout.UnitTextureCropUv(textureName);
            Rect visible = CombatUnitHudLayout.UnitVisibleContentRect(cell, textureName);
            Rect frame = CombatUnitHudLayout.UnitPresentationRect(cell);

            Assert.That(uv, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(visible, Is.EqualTo(frame));
            Assert.That(visible.width / visible.height, Is.EqualTo(1f).Within(.0001f));
            Assert.That(Mathf.Max(visible.width, visible.height), Is.EqualTo(256f).Within(.0001f));
            Assert.That(visible.center.x, Is.EqualTo(cell.X + cell.Width * .5f).Within(.0001f));
            Assert.That(visible.yMin, Is.EqualTo(cell.Y - cell.Height));
            Assert.That(visible.xMin, Is.EqualTo(cell.X - cell.Width / 2f).Within(.0001f));
            Assert.That(visible.xMax, Is.EqualTo(cell.X + cell.Width * 1.5f).Within(.0001f));
            Assert.That(visible.yMax, Is.EqualTo(cell.Y + cell.Height).Within(.0001f));
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
                CombatCommand.Move("hero", new GridPosition(1, 0)), false), Is.True);
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
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
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
