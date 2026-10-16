using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.Tests
{
    public sealed class DeviceMaterialPresentationTests
    {
        private bool oldReview,oldFields;
        private CombatFormalVisualAssets assets;
        private static readonly GridPosition P=new GridPosition(1,1);
        [SetUp] public void Before()
        {
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
            oldReview=BattlefieldDeviceArtCandidates.ReviewEnabled;oldFields=BattlefieldFieldArtCandidates.ReviewEnabled;
            BattlefieldDeviceArtCandidates.ReviewEnabled=true;BattlefieldFieldArtCandidates.ReviewEnabled=true;
            assets=new CombatFormalVisualAssets();assets.LoadRuntime();
        }
        [TearDown] public void After(){BattlefieldDeviceArtCandidates.ReviewEnabled=oldReview;BattlefieldFieldArtCandidates.ReviewEnabled=oldFields;}
        private BattlefieldCellPresentation Build(TileState tile)
        {
            var state=new CombatState(new GridMap(3,3),new[]{new UnitState("hero",true,new GridPosition(0,0))});state.Map.SetTile(P,tile);
            return new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(),assets).Build(state,null,new FireBattleState(state),new CombatSelectionController(),false,null,P,_=>null,(_,__)=>null,_=>null,_=>null);
        }
        [TestCase(0,"device_overload","过载装置")][TestCase(1,"device_ward_generator","护罩发生器")][TestCase(2,"device_tower_mechanism","塔内机关")]
        public void Devices_KeepIdentityAndDurability_AndReleaseTheCandidateWhenDestroyed(int kind,string id,string label)
        {
            var tile=new TileState{IsOverloadDevice=kind==0,IsWardGenerator=kind==1,IsTowerMechanism=kind==2,Durability=24};
            var intact=Build(tile);Assert.That(intact.ObjectTexture.name,Is.EqualTo(id));Assert.That(intact.ObjectLabel,Is.EqualTo(label));
            if(kind==2){Assert.That(intact.ObjectHoverText,Does.Contain("未放行"));tile.IsReleased=true;Assert.That(Build(tile).ObjectHoverText,Does.Contain("已放行"));}
            tile.Durability=5;Assert.That(Build(tile).ObjectTexture.name,Is.EqualTo(id));
            tile.Durability=0;Assert.That(Build(tile).ObjectTexture.name,Does.Contain("rubble"));
            tile.Durability=24;BattlefieldDeviceArtCandidates.ReviewEnabled=false;
            Assert.That(Build(tile).ObjectTexture.name,Is.EqualTo(id));Assert.That(tile.Durability,Is.EqualTo(24));
        }
        [TestCase(1,"护障维护","临时重掩体")]
        [TestCase(2,"显影巡查","清除线上单位护盾")]
        [TestCase(3,"冲压隔离","6 点伤害")]
        public void TowerMechanisms_ExplainFunctionAndCurrentReleaseState(int kind,string name,string effect)
        {
            var tile=new TileState{IsTowerMechanism=true,MechanismKind=kind,Durability=24};
            var model=Build(tile);Assert.That(model.ObjectLabel,Is.EqualTo(name));
            Assert.That(model.ObjectHoverText,Does.Contain(effect).And.Contain("未放行").And.Contain("暂不提供效果"));
            tile.IsReleased=true;Assert.That(Build(tile).ObjectHoverText,Does.Contain("已放行，计入维护链"));
            tile.Durability=0;Assert.That(Build(tile).ObjectHoverText,Does.Contain("已拆除").And.Not.Contain("放行效果"));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void CrystalMaterial_DoesNotReplaceSameCellObjectOrEffect(int kind)
        {
            var tile=new TileState{IsCrystalShard=true,IsWardGenerator=kind==0,IsPermanentWall=kind==1,
                IsLampVine=kind==2,IsLoosePaper=true,Durability=24,Cover=kind==3?CoverType.Heavy:CoverType.None};
            var model=Build(tile);Assert.That(model.GroundMaterialTexture.name,Is.EqualTo("material_crystal_shards"));
            Assert.That(model.ObjectTexture,Is.Not.Null);Assert.That(model.ObjectTexture.name,Is.Not.EqualTo("academy_light_planter_rubble"));
            Assert.That(model.EnvironmentTexture.name,Is.EqualTo("field_loose_paper"));
            Assert.That(model.GroundLabel,Is.EqualTo("碎晶\n散页"));Assert.That(model.SurfaceHoverText,Does.Contain("碎晶"));
            Assert.That(model.ObjectHoverText,Does.Not.Contain("碎晶"));
            tile.IsCrystalShard=false;var cleared=Build(tile);Assert.That(cleared.GroundMaterialTexture,Is.Null);
            Assert.That(cleared.GroundLabel,Is.EqualTo("散页"));Assert.That(cleared.ObjectTexture,Is.SameAs(model.ObjectTexture));
        }
        [Test] public void UnillustratedMaterial_UsesAnExactNameInsteadOfAPretendObject()
        {
            BattlefieldDeviceArtCandidates.ReviewEnabled=false;
            var model=Build(new TileState{IsCrystalShard=true});
            Assert.That(model.ObjectTexture,Is.Null);Assert.That(model.GroundMaterialTexture.name,Is.EqualTo("material_crystal_shards"));Assert.That(model.GroundLabel,Is.EqualTo("碎晶"));
        }
        [TestCase("device_overload")][TestCase("device_ward_generator")][TestCase("device_tower_mechanism")][TestCase("material_crystal_shards")]
        public void CandidateImports_PreserveCompleteNativeCanvasAndSampling(string id)
        {
            var texture=BattlefieldDeviceArtCandidates.Resolve(id);var sprite=Resources.Load<Sprite>(BattlefieldArtResources.Devices+id);
            Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));Assert.That(texture.width%32,Is.Zero);Assert.That(texture.height%32,Is.Zero);
            Assert.That(texture.width,Is.LessThanOrEqualTo(64));Assert.That(texture.height,Is.LessThanOrEqualTo(64));
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
        }
    }
}
