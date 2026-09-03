using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatTargetDamageForecastTests
    {
        [Test]
        public void WeaponForecast_UsesResolvedShieldAndHealthLossWithoutMutatingLiveState()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            int healthBefore = enemy.Health;
            int shieldBefore = enemy.Shield;

            CombatTargetDamageForecast forecast =
                CombatTargetDamageForecaster.WeaponAttack(new FireBattleState(state), hero.Id, enemy.Id);

            Assert.That(forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(forecast.ShieldLoss, Is.LessThanOrEqualTo(shieldBefore));
            Assert.That(forecast.RemainingHealth, Is.EqualTo(healthBefore - forecast.HealthLoss));
            Assert.That(enemy.Health, Is.EqualTo(healthBefore));
            Assert.That(enemy.Shield, Is.EqualTo(shieldBefore));
        }

        [Test]
        public void WeaponForecast_MarksLethalResolvedDamage()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.AbsorbShield(enemy.Id, enemy.Shield),
                CombatEffect.DamageHealth(enemy.Id, enemy.Health - 1));
            CombatResolver.BeginTurn(state, hero.Id);

            CombatTargetDamageForecast forecast =
                CombatTargetDamageForecaster.WeaponAttack(new FireBattleState(state), hero.Id, enemy.Id);

            Assert.That(forecast.WillDefeat, Is.True);
            Assert.That(forecast.RemainingHealth, Is.Zero);
            Assert.That(forecast.PlayerSummary, Does.Contain("可迫使目标认输并退出考核"));
            Assert.That(forecast.PlayerSummary, Does.Not.Contain("击杀"));
        }

        [Test]
        public void FireSpellForecast_IncludesCurrentFiregroundContribution()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(5, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            FireBattleState battle = new FireBattleState(state);
            battle.CreateOrRefreshFireground(enemy.Position, 8, 8, "test-ground");
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R17");
            FireSpellTarget target = FireSpellTarget.Unit(enemy.Id, Facing.East);

            CombatTargetDamageForecast forecast =
                CombatTargetDamageForecaster.FireSpell(battle, hero.Id, spell, target, enemy.Id);

            Assert.That(forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(forecast.EnvironmentDamage, Is.EqualTo(forecast.TotalDamage));
            Assert.That(forecast.PlayerSummary, Does.Contain("环境伤害"));
            Assert.That(battle.HasFireground(enemy.Position), Is.True, "Forecast must not consume the live fireground.");
        }

        [Test]
        public void DamageSkillForecast_UsesTheRealSkillResolver()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            SkillDefinition skill = hero.SkillOne;

            CombatTargetDamageForecast forecast = CombatTargetDamageForecaster.Skill(state,
                CombatCommand.UseSkill(hero.Id, 0, enemy.Id), enemy.Id);

            Assert.That(forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(forecast.RemainingHealth, Is.EqualTo(enemy.Health - forecast.HealthLoss));
            Assert.That(hero.ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana));
        }
    }
}
