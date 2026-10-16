using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.Tests
{
    public sealed class EnemyUnitArtCandidateTests
    {
        private bool review;
        [OneTimeSetUp]
        public void ConfirmMainProjectAndSceneState()
        {
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
        }
        [SetUp] public void Before(){review=EnemyUnitArtCandidates.ReviewEnabled;EnemyUnitArtCandidates.ReviewEnabled=false;}
        [TearDown] public void After(){EnemyUnitArtCandidates.ReviewEnabled=review;}

        [TestCase("signal_keeper",32,64)]
        [TestCase("elder_tracker_hound",64,32)]
        [TestCase("wind_librarian",32,64)]
        [TestCase("legacy_storekeeper",32,64)]
        public void AdoptedUnit_IsDefaultAndIndependentOfReviewSwitch(string id,int width,int height)
        {
            var assets=new CombatFormalVisualAssets();assets.LoadRuntime();
            var keeper=new UnitState("candidate",false,new GridPosition(1,1));EnemyArchetypes.Get(id).Apply(keeper);
            var original=assets.Unit(keeper);var portrait=assets.Portrait(keeper);
            EnemyUnitArtCandidates.ReviewEnabled=true;
            var texture=assets.Unit(keeper);
            Assert.That(texture,Is.SameAs(original));Assert.That(texture.name,Is.EqualTo(id));Assert.That(texture.width,Is.EqualTo(width));Assert.That(texture.height,Is.EqualTo(height));
            Assert.That(assets.Unit(keeper,1),Is.SameAs(texture));
            Assert.That(assets.Portrait(keeper),Is.SameAs(CombatFormalVisualAssets.DisplayPortrait(keeper)==null?texture:portrait));
            Assert.That(EnemyUnitArtCandidates.Resolve("lantern_revealer"),Is.Null);
            EnemyUnitArtCandidates.ReviewEnabled=false;Assert.That(assets.Unit(keeper),Is.SameAs(original));
        }

        [TestCase("signal_keeper")]
        [TestCase("elder_tracker_hound")]
        [TestCase("wind_librarian")]
        [TestCase("legacy_storekeeper")]
        public void ImportedCandidate_PreservesNativeWorldScaleAndPixelSampling(string id)
        {
            string path="Assets/Game/Resources/Art/FormalUnits64/"+id+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer,Is.Not.Null);Assert.That(importer.spritePixelsPerUnit,Is.EqualTo(32));
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(AssetDatabase.AssetPathToGUID(path),Is.Not.Empty);
            var sprite=Resources.Load<Sprite>("Art/FormalUnits64/"+id);
            Assert.That(sprite,Is.Not.Null);Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));
            Assert.That(sprite.pivot.y,Is.EqualTo(2).Within(.001f));
        }

    }
}
