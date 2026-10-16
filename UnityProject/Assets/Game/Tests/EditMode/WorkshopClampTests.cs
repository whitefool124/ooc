using NUnit.Framework;
using OCC.Combat.Presentation;
namespace OCC.Combat.Tests
{
    public sealed class WorkshopClampTests
    {
        [Test] public void RefreshDoesNotRestartSelectionAndReentryIsSettled()
        {
            var m=new WorkshopClampMotion();m.Bind("forge:a",false,0);
            Assert.That(m.JawOffset(0,1),Is.EqualTo(3));
            m.Bind("forge:a",false,.2f);Assert.That(m.JawOffset(.3f,1),Is.Zero);
            m.Bind("forge:b",false,1);Assert.That(m.JawOffset(1,1),Is.EqualTo(3));
            m.Reset();m.Bind("forge:b",true,2);Assert.That(m.JawOffset(2,1),Is.Zero);
            Assert.That(m.CommitCount,Is.Zero);
        }
        [Test] public void OnlyCommittedCompletedTargetRecoilsOnce()
        {
            var m=new WorkshopClampMotion();m.Bind("a",false,0);Assert.That(m.Commit(.1f),Is.False);
            m.Bind("a",true,.1f);Assert.That(m.Commit(.1f),Is.True);
            Assert.That(m.JawOffset(.12f,1),Is.EqualTo(1));
            Assert.That(m.Commit(.2f),Is.False);Assert.That(m.CommitCount,Is.EqualTo(1));
            Assert.That(m.JawOffset(.6f,1),Is.Zero);Assert.That(m.LeverOffset(.6f,1),Is.Zero);
        }
        [Test] public void CancelAndLeaveCannotReplayOldCompletion()
        {
            var m=new WorkshopClampMotion();m.Bind("a",true,0);m.Commit(0);m.CancelTransient();
            Assert.That(m.JawOffset(.02f,1),Is.Zero);Assert.That(m.LeverOffset(.02f,1),Is.Zero);
            m.Reset();Assert.That(m.HasTarget,Is.False);Assert.That(m.JawOffset(1,1),Is.EqualTo(3));
            Assert.That(m.Commit(1),Is.False);
        }
    }
}
