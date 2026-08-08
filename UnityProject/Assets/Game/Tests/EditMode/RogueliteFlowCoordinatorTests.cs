using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteFlowCoordinatorTests
    {
        [Test]
        public void BeginMapRun_ClosesLandingAndOwnsMapState()
        {
            RogueliteFlowCoordinator flow = new RogueliteFlowCoordinator();
            flow.OpenRogueliteMenu();
            RogueliteMapRun run = new RogueliteMapRun(41);

            flow.BeginMapRun(run);

            Assert.That(flow.MapRun, Is.SameAs(run));
            Assert.That(flow.IsMapMenuOpen, Is.True);
            Assert.That(flow.IsRogueliteMenuOpen, Is.False);
            Assert.That(flow.DeveloperRun, Is.Null);
        }

        [Test]
        public void Reset_ClearsAllRogueliteFlowState()
        {
            RogueliteFlowCoordinator flow = new RogueliteFlowCoordinator();
            flow.BeginMapRun(new RogueliteMapRun(7));

            flow.Reset();

            Assert.That(flow.MapRun, Is.Null);
            Assert.That(flow.DeveloperRun, Is.Null);
            Assert.That(flow.IsMapMenuOpen, Is.False);
            Assert.That(flow.IsRogueliteMenuOpen, Is.False);
        }
    }
}
