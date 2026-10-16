using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class StageSceneryBindingTests
    {
        [TestCase("elite_foundry")][TestCase("depot_wreck")][TestCase("library_discipline")]
        [TestCase("calibration_lockdown")][TestCase("sealed_vault_certification")]
        public void ExistingFurniture_IsLoadedAtNativeScale_OutsideTheTacticalCells(string levelId)
        {
            var level=FirstRegionLevelCatalog.For(levelId);var profile=Hd2DBattlefieldProfile.Resolve(levelId);
            Assert.That(profile.LevelId,Is.EqualTo(levelId));Assert.That(profile.Trees,Is.False);
            var ids=AcademyBattlefieldLayoutCatalog.PeripheralFixtureIds(levelId);Assert.That(ids.Length,Is.InRange(1,2));
            foreach(var id in ids)
            {
                Assert.That(AcademyBattlefieldLayoutCatalog.Structures(levelId).Any(p=>p.AssetId==id),Is.True);
                var texture=Resources.Load<Texture2D>("Art/FormalAcademyStructures32/"+id);Assert.That(texture,Is.Not.Null);
                Assert.That(-2.7f+texture.width/64f,Is.LessThan(0));
                Assert.That(level.Width+2.4f-texture.width/64f,Is.GreaterThan(level.Width));
            }
            var modules=AcademyBattlefieldLayoutCatalog.VisualModules(levelId);
            foreach(var p in modules){Assert.That(p.X,Is.GreaterThanOrEqualTo(0));Assert.That(p.TopY,Is.LessThan(level.Height));}
        }
        [Test] public void OtherEnvironments_DoNotAcquireUnconfiguredFixtures()
        {
            foreach(var level in Hd2DBattlefieldProfile.Levels.Where(l=>l.Id!="elite_foundry"&&l.Id!="depot_wreck"&&l.Id!="library_discipline"&&l.Id!="calibration_lockdown"&&l.Id!="sealed_vault_certification"))
                Assert.That(AcademyBattlefieldLayoutCatalog.PeripheralFixtureIds(level.Id),Is.Empty);
        }
    }
}
