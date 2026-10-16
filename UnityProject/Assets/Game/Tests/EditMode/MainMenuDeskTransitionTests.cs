using NUnit.Framework;
using UnityEngine;
using OCC.Combat.Presentation;

namespace OCC.Tests
{
    public sealed class MainMenuDeskTransitionTests
    {
        [Test]
        public void DoubleSubmissionHandsOffOnceAndDisableCancelsPendingHandoff()
        {
            var root = new GameObject("menu-test", typeof(RectTransform));
            try
            {
                var scene = MainMenuDeskScene.Create(root.transform, () => 1f);
                int calls = 0;
                Assert.That(scene.BeginOpen(() => calls++), Is.True);
                Assert.That(scene.BeginOpen(() => calls++), Is.False);
                scene.Advance(MainMenuDeskScene.OpenDuration + .01f);
                scene.Advance(10f);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(scene.BeginOpen(() => calls++), Is.False);
                scene.gameObject.SetActive(false);
                scene.Advance(0f); // EditMode does not dispatch MonoBehaviour lifecycle callbacks.
                scene.gameObject.SetActive(true);
                Assert.That(scene.BeginOpen(() => calls++), Is.True);
                scene.gameObject.SetActive(false);
                scene.Advance(10f);
                Assert.That(calls, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DisabledAnimationDoesNotWaitAndDoesNotLeaveAQueuedAction()
        {
            var root = new GameObject("menu-test", typeof(RectTransform));
            try
            {
                var scene = MainMenuDeskScene.Create(root.transform, () => 0f);
                int calls = 0;
                scene.BeginOpen(() => calls++);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(scene.IsOpening, Is.False);
                scene.Advance(10f);
                Assert.That(calls, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
