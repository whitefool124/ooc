using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCC.Combat.Tests
{
    public sealed class Hd2DWaterShorelineTests
    {
        private static readonly System.Type WaterType=typeof(Hd2DBattlefieldSample).Assembly
            .GetType("OCC.Combat.Presentation.Hd2DCourtyardWaterMaterial",true);

        [OneTimeSetUp]
        public void ConfirmMainProjectAndCleanScenes()
        {
            Assert.That(Application.dataPath.Replace('\\','/'),Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                Debug.Log($"Water shore local check: scene={scene.path}, dirty={scene.isDirty}");
                Assert.That(scene.isDirty,Is.False);
            }
            Debug.Log("Water shore local check: dataPath="+Application.dataPath);
        }

        private static MaterialPropertyBlock Shore(bool[] layout,int x,int y,Vector3 origin=default)
        {
            var block=new MaterialPropertyBlock();
            WaterType.GetMethod("ConfigureShore",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{block,new GridPosition(x,y),origin,layout,3,3});
            return block;
        }

        [Test]
        public void OpenWater_DoesNotCreateBanksAtInternalTileSeams()
        {
            var block=Shore(new[]{true,true,true,true,true,true,true,true,true},1,1);
            Assert.That(block.GetVector("_ShoreEdges"),Is.EqualTo(Vector4.zero));
            Assert.That(block.GetVector("_ShoreCorners"),Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void ConcaveBank_TracksDryDiagonalWithoutClosingOpenEdges()
        {
            var block=Shore(new[]{true,true,true,true,true,true,true,true,false},1,1);
            Assert.That(block.GetVector("_ShoreEdges"),Is.EqualTo(Vector4.zero));
            Assert.That(block.GetVector("_ShoreCorners"),Is.EqualTo(new Vector4(0,0,0,1)));
        }

        [Test]
        public void InteriorIsland_ExposesBankInsideOuterWaterBounds()
        {
            var block=Shore(new[]{true,true,true,true,false,true,true,true,true},0,1);
            Assert.That(block.GetVector("_ShoreEdges"),Is.EqualTo(new Vector4(1,1,0,0)));
            Assert.That(block.GetVector("_ShoreCorners"),Is.EqualTo(new Vector4(1,0,1,0)));
        }

        [Test]
        public void TopologyChange_RemovesBankAndUpdatesWorldCell()
        {
            var layout=new[]{true,true,true,true,true,false,true,true,true};
            Assert.That(Shore(layout,1,1).GetVector("_ShoreEdges").y,Is.EqualTo(1));
            layout[5]=true;
            var block=Shore(layout,1,1,new Vector3(12,4,-7));
            Assert.That(block.GetVector("_ShoreEdges"),Is.EqualTo(Vector4.zero));
            Assert.That(block.GetVector("_WaterCell"),Is.EqualTo(new Vector4(13,-6,1,1)));
        }

        [Test]
        public void WaterShader_CompilesAndKeepsNonBattlefieldDefault()
        {
            var shader=Shader.Find("OCC/HD2D/CourtyardWater");
            Assert.That(shader,Is.Not.Null);
            var material=new Material(shader);
            try
            {
                Assert.That(material.GetFloat("_UseCellShore"),Is.Zero);
                Assert.That(material.SetPass(0),Is.True,"Water forward pass must compile on this device");
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    Assert.That(message.severity.ToString(),Is.Not.EqualTo("Error"),message.message);
            }
            finally{Object.DestroyImmediate(material);}
        }
    }
}
