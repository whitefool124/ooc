using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
namespace OCC.Combat.Tests
{
    public sealed class CombatWaterRippleTests
    {
        private static GridPosition P(int x,int y)=>new GridPosition(x,y);
        [Test] public void Contacts_FollowExecutedCorners_OnceEvenIfLastFrameIsLate()
        {
            var movement=new CombatMovementPlayback();
            var feet=new List<Vector2>(); var times=new List<float>();
            movement.GroundContact+=(foot,at)=>{feet.Add(foot);times.Add(at);};
            movement.Play("hero",P(0,0),P(1,1),new[]{P(0,0),P(1,0),P(1,1)},0);
            movement.Sample("hero",P(1,1),.04f); Assert.That(feet.Count,Is.Zero);
            movement.Sample("hero",P(1,1),.09f); Assert.That(feet,Is.EqualTo(new[]{new Vector2(1,0)}));
            movement.Sample("hero",P(1,1),.09f); Assert.That(feet.Count,Is.EqualTo(1));
            movement.Sample("hero",P(1,1),.3f);
            Assert.That(feet,Is.EqualTo(new[]{new Vector2(1,0),new Vector2(1,1)}));
            Assert.That(times[1],Is.EqualTo(.16f).Within(.001f));
        }
        [Test] public void TeleportOrCancelledRoute_DoesNotInventWaterSteps()
        {
            var movement=new CombatMovementPlayback(); int contacts=0;
            movement.GroundContact+=(foot,at)=>contacts++;
            movement.Play("hero",P(0,0),P(2,0),null,0); movement.Sample("hero",P(2,0),1);
            movement.Play("hero",P(0,0),P(1,0),new[]{P(0,0),P(1,0)},0);
            movement.Sample("hero",P(3,0),1); Assert.That(contacts,Is.Zero);
        }
        [Test] public void DenseContacts_CoalesceAndExpireWithoutLeavingStationaryWaves()
        {
            var water=new CombatWaterRipples(); water.Add(Vector2.zero,0);water.Add(Vector2.zero,.05f);
            Assert.That(water.Waves.Count,Is.EqualTo(1));
            for(int i=1;i<50;i++)water.Add(new Vector2(i,0),0);
            Assert.That(water.Waves.Count,Is.EqualTo(CombatWaterRipples.MaximumCount));
            water.Advance(CombatWaterRipples.Lifetime); Assert.That(water.Waves.Count,Is.Zero);
        }
        [Test] public void ShoreMesh_IsRestrictedToWaterInteriorAndNeverOwnsInput()
        {
            var go=new GameObject("water",typeof(RectTransform),typeof(CanvasRenderer));
            try
            {
                var graphic=go.AddComponent<CombatWaterRippleGraphic>();graphic.rectTransform.sizeDelta=new Vector2(192,192);
                var water=new CombatWaterRipples();water.Add(Vector2.zero,0);
                var build=typeof(CombatWaterRippleGraphic).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
                using(var mesh=new VertexHelper())
                {
                    graphic.Refresh(water,P(0,0),true,.3f);build.Invoke(graphic,new object[]{mesh});Assert.That(mesh.currentVertCount,Is.GreaterThan(0));
                    var vertex=new UIVertex();
                    for(int i=0;i<mesh.currentVertCount;i++){mesh.PopulateUIVertex(ref vertex,i);Assert.That(vertex.position.x,Is.InRange(-90f,90f));Assert.That(vertex.position.y,Is.InRange(-90f,90f));}
                    graphic.Refresh(water,P(0,0),false,.3f);build.Invoke(graphic,new object[]{mesh});Assert.That(mesh.currentVertCount,Is.Zero);
                    graphic.Refresh(water,P(0,0),true,1);build.Invoke(graphic,new object[]{mesh});Assert.That(mesh.currentVertCount,Is.Zero);
                    Assert.That(graphic.raycastTarget,Is.False);
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}