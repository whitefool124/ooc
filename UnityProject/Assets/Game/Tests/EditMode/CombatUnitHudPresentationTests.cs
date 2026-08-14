using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatUnitHudPresentationTests
    {
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
