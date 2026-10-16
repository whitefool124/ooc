using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatMotionRefinementTests
    {
        [Test] public void ActualRouteAcceleratesAndBrakesWithoutChangingArrivalTime()
        {
            var playback = new CombatMovementPlayback();
            var end = new GridPosition(4, 0);
            playback.Play("enemy",new GridPosition(0,0),end,new[]{new GridPosition(0,0),new GridPosition(1,0),new GridPosition(2,0),new GridPosition(3,0),end},0);
            float duration=.32f;
            float first=playback.Sample("enemy",end,duration*.1f).Position.x;
            float middle=playback.Sample("enemy",end,duration*.5f).Position.x-
                playback.Sample("enemy",end,duration*.4f).Position.x;
            float last=4-playback.Sample("enemy",end,duration*.9f).Position.x;
            Assert.That(first,Is.LessThan(middle)); Assert.That(last,Is.LessThan(middle));
            int arrivals=0;playback.Arrived+=(id,d,at)=>{arrivals++;Assert.That(id,Is.EqualTo("enemy"));Assert.That(at,Is.EqualTo(duration).Within(.001));};
            Assert.That(playback.Sample("enemy",end,duration).Position,Is.EqualTo(new Vector2(4,0)));
            playback.Sample("enemy",end,1);Assert.That(arrivals,Is.EqualTo(1));
            Assert.That(playback.BodyOffset("enemy",end,1),Is.EqualTo(Vector2.zero));
        }

        [Test] public void PreparationUsesPublicFirstStepAndDoesNotReplaceAHit()
        {
            using(var fixture=new Fixture())
            {
                var enemy=fixture.Host.CurrentState.GetUnit("enemy");var hero=fixture.Host.CurrentState.GetUnit("hero");
                var intent=new EnemyTurnPlanBook().GetPublicIntent(fixture.Host.CurrentState,enemy,hero);
                Assert.That(CombatUnitMotionPolicy.PreparationDirection(enemy,intent).x,Is.LessThan(0));
                fixture.Feedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage,hero.Position,enemy.Position,3));
                fixture.Feedback.BeginEnemyAction(enemy,intent,.5f,CombatCommandType.Move);
                Assert.That(Kind(fixture.Feedback,enemy.Id),Is.EqualTo("Hit"));
                fixture.Feedback.CancelEnemyAction();Assert.That(Kind(fixture.Feedback,enemy.Id),Is.EqualTo("Hit"));
            }
        }

        [TestCase(true)] [TestCase(false)] public void MixedShieldAndHealthContactKeepsHealthMotionInEitherOrder(bool shieldFirst)
        {
            using(var fixture=new Fixture())
            {
                var shield=new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb,new GridPosition(0,0),new GridPosition(3,0),2);
                var health=new CombatFeedbackEvent(CombatFeedbackKind.Damage,new GridPosition(0,0),new GridPosition(3,0),3);
                fixture.Feedback.Publish(shieldFirst?shield:health);fixture.Feedback.Publish(shieldFirst?health:shield);
                Assert.That(Kind(fixture.Feedback,"enemy"),Is.EqualTo("Hit"));
                fixture.Host.UiPreferences.Configure(1,0,false,true,false,false,true);
                Assert.That(fixture.Feedback.UnitPresentationOffset(fixture.Host.CurrentState.GetUnit("enemy")),Is.EqualTo(Vector2.zero));
                Assert.That(Kind(fixture.Feedback,"enemy"),Is.Null);
            }
        }

        [Test] public void ZeroChangeCannotCreateDamageMotionAndShieldStaysLighterAndSilver()
        {
            using(var fixture=new Fixture())
            {
                fixture.Feedback.Publish(new CombatFeedbackEvent(CombatFeedbackKind.Damage,new GridPosition(3,0),0));
                Assert.That(Kind(fixture.Feedback,"enemy"),Is.Null);
            }
            Assert.That(CombatUnitMotionPolicy.Impact(false,Vector2.right,0).magnitude,
                Is.LessThan(CombatUnitMotionPolicy.Impact(true,Vector2.right,0).magnitude));
            Color silver=CombatUnitMotionPolicy.ImpactTint(false,0,1);
            Assert.That(Mathf.Abs(silver.r-silver.b),Is.LessThan(.1f));
            Assert.That(CombatUnitMotionPolicy.Stop(Vector2.left,1),Is.EqualTo(Vector2.zero));
        }

        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static string Kind(CombatVisualFeedback feedback,string id)
        {
            var motions=(System.Collections.IDictionary)typeof(CombatVisualFeedback).GetField("unitMotions",Private).GetValue(feedback);
            var motion=motions[id];return motion?.GetType().GetProperty("Kind").GetValue(motion)?.ToString();
        }
        private sealed class Fixture:System.IDisposable
        {
            private readonly GameObject root=new GameObject("motion-test");
            public readonly Host Host=new Host();public readonly CombatVisualFeedback Feedback;
            public Fixture()
            {
                Feedback=root.AddComponent<CombatVisualFeedback>();
                typeof(CombatVisualFeedback).GetField("bootstrap",Private).SetValue(Feedback,Host);
                typeof(CombatVisualFeedback).GetField("canvas",Private).SetValue(Feedback,root.AddComponent<Canvas>());
            }
            public void Dispose()=>Object.DestroyImmediate(root);
        }
        private sealed class Host:ICombatFeedbackHost
        {
            public RogueliteUiPreferences UiPreferences{get;}=new RogueliteUiPreferences();
            public CombatState CurrentState{get;}=new CombatState(new GridMap(5,2),new[]{new UnitState("hero",true,new GridPosition(0,0)),new UnitState("enemy",false,new GridPosition(3,0))});
            public BattlefieldRect CurrentBattlefieldViewport=>new BattlefieldPresentationAdapter().ViewportRect;
            public bool IsDeveloperCombatActive=>true;
            public Vector2 GridToFeedbackPosition(GridPosition p)=>new Vector2(p.X*64,-p.Y*64);
        }
    }
}
