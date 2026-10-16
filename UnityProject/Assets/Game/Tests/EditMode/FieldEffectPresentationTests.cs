using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class FieldEffectPresentationTests
    {
        private bool review;
        private CombatFormalVisualAssets assets;
        private static readonly GridPosition P=new GridPosition(1,1);
        [OneTimeSetUp] public void Identity()
        {
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)Assert.That(SceneManager.GetSceneAt(i).isDirty,Is.False);
        }
        [SetUp] public void Before()
        {
            review=BattlefieldFieldArtCandidates.ReviewEnabled;BattlefieldFieldArtCandidates.ReviewEnabled=false;
            assets=new CombatFormalVisualAssets();assets.LoadRuntime();
        }
        [TearDown] public void After(){BattlefieldFieldArtCandidates.ReviewEnabled=review;}

        private BattlefieldCellPresentation Build(CombatState state,FireBattleState fire=null)
            =>new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(),assets).Build(state,null,
                fire??new FireBattleState(state),new CombatSelectionController(),false,null,P,_=>null,(_,__)=>null,_=>null,_=>null);
        private static CombatState State(TileState tile,bool occupied=false)
        {
            var state=new CombatState(new GridMap(3,3),new[]{new UnitState("hero",true,occupied?P:new GridPosition(0,0))});
            state.Map.SetTile(P,tile);return state;
        }
        private static TileState Effect(int kind)=>new TileState{IsLoosePaper=kind==0,HasTrace=kind==1||kind==4,
            IsBindingMark=kind==2,HasPaperScreen=kind==3,IsDeepTrace=kind==4};

        [TestCase(0,"散页",false)] [TestCase(0,"散页",true)]
        [TestCase(1,"痕迹",false)] [TestCase(1,"痕迹",true)]
        [TestCase(2,"约束纹",false)] [TestCase(2,"约束纹",true)]
        [TestCase(3,"页幕",false)] [TestCase(3,"页幕",true)]
        [TestCase(4,"痕迹",false)]
        public void UnillustratedEffects_KeepTheirExactNamesEvenUnderUnits(int kind,string name,bool occupied)
        {
            var state=State(Effect(kind),occupied);var hero=state.GetUnit("hero");var position=hero.Position;int hp=hero.Health;
            var cell=Build(state);
            Assert.That(cell.FieldEffectLabel,Is.EqualTo(name));Assert.That(cell.TerrainEffectHoverText,Does.Contain(name));
            Assert.That(hero.Health,Is.EqualTo(hp));Assert.That(hero.Position,Is.EqualTo(position));
            Assert.That(cell.Unit!=null,Is.EqualTo(occupied));
        }

        [Test]
        public void EffectLabel_DoesNotReplaceDeviceIdentity_AndClearsWithTheEffect()
        {
            var tile=new TileState{IsWardGenerator=true,Durability=16,IsLoosePaper=true};var state=State(tile);
            var cell=Build(state);Assert.That(cell.ObjectLabel,Is.EqualTo("护罩发生器"));Assert.That(cell.ObjectTexture,Is.Not.Null);
            Assert.That(cell.FieldEffectLabel,Is.EqualTo("散页"));
            tile.IsLoosePaper=false;Assert.That(Build(state).FieldEffectLabel,Is.Empty);
        }

        [Test]
        public void PaperCandidate_IsReversible_AndDoesNotOverrideWaterSmokeOrFire()
        {
            var tile=Effect(0);var state=State(tile);var fire=new FireBattleState(state);
            Assert.That(Build(state,fire).EnvironmentTexture.name,Is.EqualTo("field_loose_paper"));
            BattlefieldFieldArtCandidates.ReviewEnabled=true;
            Assert.That(Build(state,fire).EnvironmentTexture.name,Is.EqualTo("field_loose_paper"));
            Assert.That(Build(state,fire).FieldEffectLabel,Is.EqualTo("散页"));
            tile.IsWater=true;Assert.That(Build(state,fire).EnvironmentTexture.name,Is.EqualTo("rain_court_water"));
            Assert.That(Build(state,fire).FieldEffectLabel,Is.Empty);
            tile.IsWater=false;tile.SmokeExpiresAt=state.CurrentTime+10;
            Assert.That(Build(state,fire).EnvironmentTexture,Is.SameAs(assets.SmokeFrame(Mathf.FloorToInt(Time.unscaledTime*8)%assets.EnvironmentFrameCount)));Assert.That(Build(state,fire).FieldEffectLabel,Is.Empty);
            tile.SmokeExpiresAt=0;fire.CreateOrRefreshFireground(P,8,3,"field-preview","hero");
            Assert.That(Build(state,fire).EnvironmentTexture,Is.SameAs(assets.FiregroundFrame(Mathf.FloorToInt(Time.unscaledTime*8)%assets.EnvironmentFrameCount)));
            Assert.That(Build(state,fire).FieldEffectLabel,Is.Empty);
        }

        [Test]
        public void ProjectedLabels_FollowCameraChanges_ClearAndReuse_WithoutInterceptingInput()
        {
            var go=new GameObject("local-field-labels",typeof(RectTransform));
            try
            {
                var labels=go.AddComponent<Hd2DFieldEffectLabels>();var models=new Dictionary<GridPosition,BattlefieldCellPresentation>{{P,Build(State(Effect(0),true))}};
                labels.Refresh(models,_=>new Vector2(20,40));var text=go.GetComponentInChildren<Text>();
                Assert.That(text.text,Is.EqualTo("散页"));Assert.That(((RectTransform)text.transform.parent).anchoredPosition,Is.EqualTo(new Vector2(20,40)));
                labels.Refresh(models,_=>new Vector2(-15,70));Assert.That(((RectTransform)text.transform.parent).anchoredPosition,Is.EqualTo(new Vector2(-15,70)));
                Assert.That(go.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),Is.True);
                models[P]=Build(State(new TileState()));labels.Refresh(models,_=>Vector2.zero);
                Assert.That(text.transform.parent.gameObject.activeSelf,Is.False);
                models[P]=Build(State(Effect(2)));labels.Refresh(models,_=>Vector2.one);
                Assert.That(text.text,Is.EqualTo("约束纹"));Assert.That(go.GetComponentsInChildren<Text>(true).Length,Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(go);}
        }

        [Test]
        public void PaperImporter_PreservesTheNativeCellAndTransparentSampling()
        {
            var sprite=Resources.Load<Sprite>(BattlefieldArtResources.Fields+"field_loose_paper");
            Assert.That(sprite,Is.Not.Null);Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));Assert.That(sprite.rect.size,Is.EqualTo(new Vector2(32,32)));
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
        }

        [TestCase(1,"field_trace",false)]
        [TestCase(2,"field_binding_mark",false)]
        [TestCase(3,"field_paper_screen",true)]
        public void NewFields_UseTheirOwnArtAndCorrectOrientation_OnlyDuringReview(int kind,string id,bool upright)
        {
            var state=State(Effect(kind));Assert.That(Build(state).EnvironmentTexture.name,Is.EqualTo(id));
            BattlefieldFieldArtCandidates.ReviewEnabled=true;
            var model=Build(state);Assert.That(model.EnvironmentTexture.name,Is.EqualTo(id));Assert.That(model.EnvironmentUpright,Is.EqualTo(upright));
            BattlefieldFieldArtCandidates.ReviewEnabled=false;Assert.That(Build(state).EnvironmentTexture,Is.SameAs(model.EnvironmentTexture));
            Assert.That(state.Map.GetTile(P).EffectLayerName(),Is.Not.Null);Assert.That(state.GetUnit("hero").Health,Is.EqualTo(UnitState.HeroBaseHealth));
        }

        [TestCase("field_smoke")][TestCase("field_burning")]
        public void AnimatedCandidates_KeepFixedCanvasFootAnchorAndAllSixExposures(string id)
        {
            BattlefieldFieldArtCandidates.ReviewEnabled=true;
            var silhouettes=new HashSet<string>();
            for(int frame=0;frame<6;frame++)
            {
                var texture=BattlefieldFieldArtCandidates.Frame(id,frame);Assert.That(texture.width,Is.EqualTo(32));Assert.That(texture.height,Is.EqualTo(32));
                Assert.That(BattlefieldFieldArtCandidates.Upright(texture),Is.True);
                var sprite=Resources.Load<Sprite>(BattlefieldArtResources.Fields+texture.name);Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(importer.mipmapEnabled,Is.False);
                silhouettes.Add(texture.imageContentsHash.ToString());
            }
            Assert.That(silhouettes.Count,Is.GreaterThanOrEqualTo(5));
            Assert.That(BattlefieldFieldArtCandidates.Frame(id,6),Is.SameAs(BattlefieldFieldArtCandidates.Frame(id,0)));
            BattlefieldFieldArtCandidates.ReviewEnabled=false;Assert.That(BattlefieldFieldArtCandidates.Frame(id,0),Is.Null);
        }
    }
}
