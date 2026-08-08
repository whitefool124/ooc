using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class FormalArtRegistryTests
    {
        [Test]
        public void Registry_HasUniqueAssetIdsAndDomainRuntimeIds()
        {
            Assert.That(FormalArtRegistry.All.Select(entry => entry.AssetId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(FormalArtRegistry.All.Count));
            AssertDomain(FormalArtRegistry.Units);
            AssertDomain(FormalArtRegistry.Commands);
            AssertDomain(FormalArtRegistry.Feedback);
            AssertDomain(FormalArtRegistry.Statuses);
            AssertDomain(FormalArtRegistry.Environments);
            AssertDomain(FormalArtRegistry.NodeTypes);
            AssertDomain(FormalArtRegistry.RuntimeSkills);
            AssertDomain(FormalArtRegistry.FireSpells);
            AssertDomain(FormalArtRegistry.Items);
            AssertDomain(FormalArtRegistry.Vfx);
        }

        [Test]
        public void UnitMappings_AreUniqueAndUnknownIdDoesNotFallback()
        {
            Assert.That(FormalArtRegistry.Units.Count, Is.EqualTo(16));
            Assert.That(FormalArtRegistry.Units.Select(entry => entry.ResourcePath).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(16));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.UnitPath("unknown_unit"));
        }

        [Test]
        public void FrozenCoverageCounts_AreExplicit()
        {
            Assert.That(FormalArtRegistry.Commands.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.Statuses.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.Environments.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.NodeTypes.Count, Is.EqualTo(9));
            Assert.That(FormalArtRegistry.RuntimeSkills.Count, Is.EqualTo(27));
            Assert.That(FormalArtRegistry.FireSpells.Count, Is.EqualTo(60));
            Assert.That(FormalArtRegistry.Items.Count, Is.EqualTo(54));
            Assert.That(FormalArtRegistry.Vfx.Count, Is.EqualTo(30));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.VfxPath("unknown_vfx"));
        }

        private static void AssertDomain(System.Collections.Generic.IReadOnlyList<FormalArtEntry> entries)
        {
            Assert.That(entries.All(entry => !string.IsNullOrWhiteSpace(entry.RuntimeId) && !string.IsNullOrWhiteSpace(entry.ResourcePath)), Is.True);
            Assert.That(entries.Select(entry => entry.RuntimeId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(entries.Count));
        }
    }
}
