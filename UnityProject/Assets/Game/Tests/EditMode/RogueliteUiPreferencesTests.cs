using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteUiPreferencesTests
    {
        [Test]
        public void Preferences_RoundTripAndClampDeterministically()
        {
            var source = new RogueliteUiPreferences().Configure(2f, -.5f, false, false, true, true, false);
            RogueliteUiPreferences restored = RogueliteUiPreferences.FromDataString(source.ToDataString());

            Assert.AreEqual(1f, restored.MasterVolume);
            Assert.AreEqual(0f, restored.AnimationIntensity);
            Assert.IsFalse(restored.ScreenShake);
            Assert.IsFalse(restored.FloatingText);
            Assert.IsTrue(restored.HighContrast);
            Assert.IsTrue(restored.LargeText);
            Assert.IsFalse(restored.KeyHints);
        }

        [Test]
        public void ClearedCombat_RemainsTraversableButDoesNotStartCombat()
        {
            var run = new RogueliteMapRun(19);
            RogueliteMapNode patrol = RogueliteMapCatalog.Node("rail_patrol");
            run.SelectNode(patrol.Id);
            run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentRewards[0].Id);
            run.SelectNode("start");

            Assert.IsTrue(RogueliteUiPreferences.CanTravelTo(run, patrol));
            Assert.IsFalse(RogueliteUiPreferences.StartsCombat(run, patrol));
        }

        [Test]
        public void FreshCombat_IsTraversableAndStartsCombat()
        {
            var run = new RogueliteMapRun(19);
            RogueliteMapNode patrol = RogueliteMapCatalog.Node("rail_patrol");

            Assert.IsTrue(RogueliteUiPreferences.CanTravelTo(run, patrol));
            Assert.IsTrue(RogueliteUiPreferences.StartsCombat(run, patrol));
            Assert.IsTrue(RogueliteUiPreferences.CanOpenCombatBriefing(run, patrol));
        }

        [Test]
        public void CurrentUnfinishedCombat_CanResumeBriefingAfterReloadBoundary()
        {
            var run = new RogueliteMapRun(19);
            RogueliteMapNode patrol = RogueliteMapCatalog.Node("rail_patrol");
            run.SelectNode(patrol.Id);

            Assert.IsFalse(RogueliteUiPreferences.CanTravelTo(run, patrol));
            Assert.IsFalse(RogueliteUiPreferences.StartsCombat(run, patrol));
            Assert.IsTrue(RogueliteUiPreferences.CanOpenCombatBriefing(run, patrol));
        }
    }
}
