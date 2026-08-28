using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatUnitHudPresentationTests
    {
        [Test]
        public void CombatHudTypography_PrioritizesReadableNumbersAndStableAlignment()
        {
            Assert.That(CombatHudTypography.ResourceValueFontSize, Is.GreaterThanOrEqualTo(16));
            Assert.That(CombatHudTypography.TimelineNameFontSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(CombatHudTypography.TimelineDetailFontSize, Is.GreaterThanOrEqualTo(14));
            Assert.That(CombatHudTypography.CostValueFontSize, Is.GreaterThanOrEqualTo(12));
            Assert.That(CombatHudTypography.ResourceValueAlignment, Is.EqualTo(TextAnchor.MiddleRight));
        }

        [Test]
        public void CompactDecisionSummary_FitsTwoPlayerFacingLinesWithoutForecastProse()
        {
            string source = "移动 · 可执行\n目标 · 选择 3 格内可通行空格 · 预计 移动并朝向目标格；无随机判定";

            string result = CombatHudTypography.CompactDecisionSummary(source, null);

            string[] lines = result.Split('\n');
            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(lines[0], Is.EqualTo("移动 · 可执行"));
            Assert.That(lines[1], Is.EqualTo("选择 3 格内可通行空格"));
            Assert.That(result, Does.Not.Contain("预计"));
        }

        [Test]
        public void PlayerEventLine_RemovesInternalSourceIdentifiers()
        {
            string result = CombatHudTypography.PlayerEventLine("阿斯特拉从starter-chest:fixed获得 2 护盾。");

            Assert.That(result, Is.EqualTo("阿斯特拉获得 2 护盾。"));
            Assert.That(result, Does.Not.Contain(":"));
            Assert.That(result, Does.Not.Contain("starter"));
        }

        [Test]
        public void UnitVitalText_UsesLightInkOnDarkBars()
        {
            Assert.That(CombatUnitHudLayout.VitalTextColor(), Is.EqualTo(FormalUiTheme.OnInk));
        }

        [TestCase("hero")]
        [TestCase("shieldguard")]
        [TestCase("sigil_mauler")]
        [TestCase("rune_arbalist")]
        [TestCase("tether_hound")]
        public void FormalUnitPresentation_UsesTheCompleteTextureCanvas(string textureName)
        {
            Assert.That(CombatUnitHudLayout.UnitTextureCropUv(textureName),
                Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void FormalUnitPresentation_FitsTheCompleteSquareCanvasWithoutAspectCropping()
        {
            BattlefieldRect cell = new BattlefieldRect(120f, 240f, 128f, 128f);

            Rect frame = CombatUnitHudLayout.UnitPresentationRect(cell);
            Rect visible = CombatUnitHudLayout.UnitVisibleContentRect(cell, "shieldguard");

            Assert.That(visible, Is.EqualTo(frame));
            Assert.That(visible.width, Is.EqualTo(visible.height));
            Assert.That(visible.width, Is.EqualTo(128f));
            Assert.That(visible.center.x, Is.EqualTo(cell.X + cell.Width * .5f));
        }

        [TestCase(64f, 64f)]
        [TestCase(80f, 128f)]
        [TestCase(96f, 128f)]
        [TestCase(128f, 128f)]
        [TestCase(160f, 192f)]
        public void FormalUnitPresentation_UsesWholeNumberNative64ScaleAndBottomAnchor(float cellSize, float expected)
        {
            BattlefieldRect cell = new BattlefieldRect(20f, 40f, cellSize, cellSize);

            Rect frame = CombatUnitHudLayout.UnitPresentationRect(cell);

            Assert.That(frame.width, Is.EqualTo(expected));
            Assert.That(frame.height, Is.EqualTo(expected));
            Assert.That(frame.yMax, Is.EqualTo(cell.Y + cell.Height));
            Assert.That(frame.center.x, Is.EqualTo(cell.X + cell.Width * .5f));
        }

        [Test]
        public void VitalsPresentation_ExposesCurrentLossRemainingAndLethalState()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.DamageHealth(enemy.Id, enemy.Health - 1));
            CombatTargetDamageForecast forecast = CombatTargetDamageForecaster.WeaponAttack(
                new FireBattleState(state), hero.Id, enemy.Id);

            CombatUnitVitalsPresentation presentation = CombatUnitVitalsPresentation.From(enemy, forecast);

            Assert.That(presentation.Shield.CompactText, Is.EqualTo("2 -2 → 0/6"));
            Assert.That(presentation.Health.CompactText, Is.EqualTo("1 -1 → 0/12"));
            Assert.That(presentation.Health.CurrentRatio, Is.EqualTo(1f / 12f));
            Assert.That(presentation.Health.RemainingRatio, Is.Zero);
            Assert.That(presentation.WillDefeat, Is.True);
        }

        [Test]
        public void RogueliteShieldVital_IsUncappedAndNeverShowsLegacyMaximum()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            CombatState state = new CombatState(new GridMap(1, 1), new[] { hero });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.TryGrantRogueliteShield(hero.Id, "test", 9);

            CombatUnitVitalsPresentation presentation = CombatUnitVitalsPresentation.From(hero, null, true);

            Assert.That(presentation.Shield.Uncapped, Is.True);
            Assert.That(presentation.Shield.CompactText, Is.EqualTo("9"));
            Assert.That(presentation.Shield.CurrentRatio, Is.EqualTo(1f));
        }

        [Test]
        public void StatusPresentation_UsesPlayerFacingEffectAndExactValues()
        {
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            enemy.ApplyStatus(StatusType.Burning, 3);
            enemy.ApplyStatus(StatusType.ArmorBreak, 2, 4);

            CombatStatusPresentation burning = CombatStatusPresentation.From(enemy, StatusType.Burning);
            CombatStatusPresentation armorBreak = CombatStatusPresentation.From(enemy, StatusType.ArmorBreak);

            Assert.That(burning.ValueText, Is.EqualTo("3"));
            Assert.That(burning.Detail, Does.Contain("失去 2 点生命"));
            Assert.That(burning.Detail, Does.Contain("无视护盾"));
            Assert.That(armorBreak.Detail, Does.Contain("护甲降低 4"));
            Assert.That(armorBreak.Detail, Does.Contain("剩余 2 回合"));
        }

        [TestCase(StatusType.Dazzled, "dazzled", "目眩")]
        [TestCase(StatusType.Revealed, "revealed", "显露")]
        public void StatusPresentation_CoversArtifactStatuses(StatusType status, string runtimeId, string name)
        {
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            enemy.ApplyStatus(status, 2);

            CombatStatusPresentation presentation = CombatStatusPresentation.From(enemy, status);

            Assert.That(presentation.RuntimeId, Is.EqualTo(runtimeId));
            Assert.That(presentation.DisplayName, Is.EqualTo(name));
            Assert.That(presentation.Detail, Does.Contain("剩余 2 回合"));
        }
    }
}
