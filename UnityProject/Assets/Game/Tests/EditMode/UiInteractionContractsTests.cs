using System;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class UiInteractionContractsTests
    {
        [Test]
        public void OverlayBackTakesPriorityAndRestoresStableFocusKey()
        {
            var state = new UiNavigationState(UiScreen.Map, "map.current");
            state.OpenOverlay(UiOverlay.Settings, "map.settings");

            Assert.That(state.ResolveBack(), Is.EqualTo(UiBackAction.CloseOverlay));
            Assert.That(state.CloseOverlay(), Is.EqualTo("map.settings"));
            Assert.That(state.Screen, Is.EqualTo(UiScreen.Map));
            Assert.That(state.Overlay, Is.EqualTo(UiOverlay.None));
        }

        [TestCase(UiScreen.Map, UiBackAction.NavigateLanding)]
        [TestCase(UiScreen.Briefing, UiBackAction.NavigateMap)]
        [TestCase(UiScreen.Combat, UiBackAction.RequestLeaveCombat)]
        [TestCase(UiScreen.Landing, UiBackAction.None)]
        [TestCase(UiScreen.Settlement, UiBackAction.None)]
        public void BackActionIsExplicitForEveryScreen(UiScreen screen, UiBackAction expected)
        {
            var state = new UiNavigationState(screen, "default");
            Assert.That(state.ResolveBack(), Is.EqualTo(expected));
        }

        [Test]
        public void NavigationClearsOverlayAndStaleFocusRestore()
        {
            var state = new UiNavigationState(UiScreen.Landing, "landing.new");
            state.OpenOverlay(UiOverlay.Archive, "landing.archive");
            state.Navigate(UiScreen.Map, "map.current");

            Assert.That(state.Overlay, Is.EqualTo(UiOverlay.None));
            Assert.That(state.RestoreFocusKey, Is.Empty);
            Assert.That(state.DefaultFocusKey, Is.EqualTo("map.current"));
        }

        [Test]
        public void MotionIntensityZeroAndOneKeepValidFinalStateTokens()
        {
            UiMotionProfile off = UiMotionProfile.FromIntensity(0f);
            UiMotionProfile full = UiMotionProfile.FromIntensity(1f);

            Assert.That(off.IsImmediate, Is.True);
            Assert.That(off.QuickDuration, Is.Zero);
            Assert.That(off.StandardDuration, Is.Zero);
            Assert.That(off.PageOffset, Is.Zero);
            Assert.That(full.IsImmediate, Is.False);
            Assert.That(full.QuickDuration, Is.EqualTo(.12f).Within(.0001f));
            Assert.That(full.StandardDuration, Is.EqualTo(.22f).Within(.0001f));
            Assert.That(full.ToastDuration, Is.EqualTo(.28f).Within(.0001f));
            Assert.That(full.PageOffset, Is.LessThanOrEqualTo(6f));
        }

        [Test]
        public void MotionIntensityIsClamped()
        {
            Assert.That(UiMotionProfile.FromIntensity(-2f).Intensity, Is.Zero);
            Assert.That(UiMotionProfile.FromIntensity(3f).Intensity, Is.EqualTo(1f));
        }

        [Test]
        public void FullMotionProfileUsesLocalPageTravel()
        {
            UiMotionProfile full = UiMotionProfile.FromIntensity(1f);
            Assert.That(full.PageOffset, Is.EqualTo(6f).Within(.0001f));
        }

        [Test]
        public void ConfirmationAndFeedbackRejectUnreadableRequests()
        {
            Assert.Throws<ArgumentException>(() => new UiConfirmationRequest(UiConfirmationKind.TacticalRestart, "", "重开", "确认"));
            Assert.Throws<ArgumentException>(() => new UiActionFeedback(UiFeedbackKind.Rejected, ""));
        }
    }
}
