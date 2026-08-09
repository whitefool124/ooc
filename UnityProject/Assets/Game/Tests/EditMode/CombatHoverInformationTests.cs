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
            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(state, enemy, hero);

            string compact = CombatInformationPresenter.BuildCompactTargetSummary(preview, enemy, intent);

            Assert.That(compact, Does.Contain(enemy.DisplayName));
            Assert.That(compact, Does.Contain("生命"));
            Assert.That(compact, Does.Contain("合法"));
            Assert.That(compact, Does.Contain(intent.CompactText));
            Assert.That(compact, Does.Not.Contain("武器："));
            Assert.That(compact, Does.Not.Contain("技能："));
            Assert.That(compact, Does.Not.Contain("伤害公式："));
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
            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(state, enemy, hero);

            string details = CombatInformationPresenter.BuildTargetDetails(preview, enemy, intent);

            Assert.That(details, Does.Contain("伤害公式："));
            Assert.That(details, Does.Contain("敌人档案"));
            Assert.That(details, Does.Contain(enemy.MainHand.DisplayName));
            Assert.That(details, Does.Contain("真实意图（权威决策）"));
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

            Assert.That(outcome.CompactDetailText, Does.Contain("战前地图存档"));
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
            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(state, enemy, hero);

            string details = CombatInformationPresenter.BuildEnemyHoverDetails(state, enemy, hero);

            Assert.That(details, Does.Contain(enemy.MainHand.DisplayName));
            Assert.That(details, Does.Contain("技能："));
            Assert.That(details, Does.Contain("真实意图：" + intent.DetailedText));
            Assert.That(details, Does.Contain("右键选择"));
        }

        [Test]
        public void EnemyHoverCard_StaysInsideBattlefieldAndAboveCommands()
        {
            Rect card = CombatPrototypeBootstrap.EnemyHoverCardRect(new Vector2(1430f, 890f));

            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(card.xMax, Is.LessThanOrEqualTo(1424f));
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(64f));
            Assert.That(card.yMax, Is.LessThanOrEqualTo(884f));
        }
    }
}
