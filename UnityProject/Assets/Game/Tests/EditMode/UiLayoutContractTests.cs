using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class UiLayoutContractTests
    {
        [Test]
        public void ReferenceAndSortingLayers_AreStable()
        {
            Assert.That(UiLayoutContract.ReferenceWidth, Is.EqualTo(1920));
            Assert.That(UiLayoutContract.ReferenceHeight, Is.EqualTo(1080));
            Assert.That(UiLayoutContract.MatchWidthOrHeight, Is.EqualTo(.5f));
            Assert.That(UiLayoutContract.SafeAreaPadding, Is.EqualTo(24));
            Assert.That(UiLayoutContract.CompactHeightThreshold, Is.EqualTo(600));
            Assert.That(UiLayoutContract.HasValidLayerOrder, Is.True);
        }
    }
}
