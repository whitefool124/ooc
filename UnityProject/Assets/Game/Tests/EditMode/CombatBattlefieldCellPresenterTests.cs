using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatBattlefieldCellPresenterTests
    {
        [Test]
        public void Build_CombinesFormalAssetsAndReadOnlyHeroState()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1), Facing.East);
            hero.DisplayName = "阿斯特拉";
            CombatState state = new CombatState(new GridMap(12, 9), new[] { hero });
            CombatSelectionController selection = new CombatSelectionController();
            CombatBattlefieldCellPresenter presenter = new CombatBattlefieldCellPresenter(
                new BattlefieldPresentationAdapter(), assets);

            BattlefieldCellPresentation cell = presenter.Build(state, null, new FireBattleState(state),
                selection, false, null, hero.Position, _ => null, (_, __) => null, _ => null, _ => null);

            Assert.That(cell.Unit, Is.SameAs(hero));
            Assert.That(cell.UnitTexture, Is.Not.Null);
            Assert.That(cell.FloorTexture.name, Is.EqualTo("floor_industrial"));
            Assert.That(cell.HoverText, Does.Contain("生命 18/18"));
        }

        [Test]
        public void FloorKey_PreservesPrototypeRailAndWarningPattern()
        {
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 2, 0), Is.EqualTo("rail_horizontal"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 5, 4), Is.EqualTo("floor_warning"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 2, 4), Is.EqualTo("floor_industrial"));
        }

        [Test]
        public void WeaponForecast_ReturnsInitializedFireContextWithoutMutatingTarget()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            int health = enemy.Health;

            CombatTargetForecastResult result = new CombatTargetForecastService().Evaluate(
                new BattlefieldPresentationAdapter(), state, null, "攻击", enemy, null, false);

            Assert.That(result.FireBattle, Is.Not.Null);
            Assert.That(result.Forecast, Is.Not.Null);
            Assert.That(result.Forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(enemy.Health, Is.EqualTo(health));
        }
    }
}
