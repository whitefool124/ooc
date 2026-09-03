using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatFormalVisualAssetsTests
    {
        [Test]
        public void LoadRuntime_ResolvesRequiredBattlefieldFamiliesWithPixelImportBehavior()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();

            assets.LoadRuntime();

            Texture2D[] samples =
            {
                assets.Relay("floor_plain"), assets.Relay("relay_intact"), assets.LootClosed,
                assets.Overlay("selected"), assets.Intent("attack"), assets.Status(StatusType.Burning),
                assets.FiregroundFrame(0), assets.SmokeFrame(5)
            };
            foreach (Texture2D texture in samples)
            {
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            }
        }

        [Test]
        public void Unit_UsesFormalHeroAndArchetypeMappings()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            EnemyArchetypes.All[0].Apply(enemy);

            Assert.That(assets.Unit(hero), Is.Not.Null);
            Assert.That(assets.Unit(enemy), Is.Not.Null);
        }
    }
}
