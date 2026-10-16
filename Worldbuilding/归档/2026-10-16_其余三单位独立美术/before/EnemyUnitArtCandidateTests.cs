using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class EnemyUnitArtCandidateTests
    {
        private bool review;
        [SetUp] public void Before(){review=EnemyUnitArtCandidates.ReviewEnabled;EnemyUnitArtCandidates.ReviewEnabled=false;}
        [TearDown] public void After(){EnemyUnitArtCandidates.ReviewEnabled=review;}

        [Test]
        public void Candidate_OnlyOverridesExplicitEditorReview_NotPortraitOrOtherUnits()
        {
            var assets=new CombatFormalVisualAssets();assets.LoadRuntime();
            var keeper=new UnitState("keeper",false,new GridPosition(1,1));EnemyArchetypes.Get("signal_keeper").Apply(keeper);
            var original=assets.Unit(keeper);var portrait=assets.Portrait(keeper);
            EnemyUnitArtCandidates.ReviewEnabled=true;
            var texture=assets.Unit(keeper);
            Assert.That(texture,Is.Not.SameAs(original));Assert.That(texture.width,Is.EqualTo(32));Assert.That(texture.height,Is.EqualTo(64));
            Assert.That(assets.Unit(keeper,1),Is.SameAs(texture));Assert.That(assets.Portrait(keeper),Is.SameAs(portrait));
            Assert.That(EnemyUnitArtCandidates.Resolve("lantern_revealer"),Is.Null);
            EnemyUnitArtCandidates.ReviewEnabled=false;Assert.That(assets.Unit(keeper),Is.SameAs(original));
        }

        [Test]
        public void ImportedCandidate_PreservesNativeWorldScaleAndPixelSampling()
        {
            const string path="Assets/Game/Resources/Art/EnemyUnitCandidates32x64/signal_keeper.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer,Is.Not.Null);Assert.That(importer.spritePixelsPerUnit,Is.EqualTo(32));
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(AssetDatabase.AssetPathToGUID(path),Is.Not.Empty);
            var sprite=Resources.Load<Sprite>("Art/EnemyUnitCandidates32x64/signal_keeper");
            Assert.That(sprite,Is.Not.Null);Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));
        }

    }
}
