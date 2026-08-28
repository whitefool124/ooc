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
            AssertDomain(FormalArtRegistry.Intents);
            AssertDomain(FormalArtRegistry.Statuses);
            AssertDomain(FormalArtRegistry.Environments);
            AssertDomain(FormalArtRegistry.NodeTypes);
            AssertDomain(FormalArtRegistry.Navigation);
            AssertDomain(FormalArtRegistry.Semantics);
            AssertDomain(FormalArtRegistry.Elements);
            AssertDomain(FormalArtRegistry.ResourceMetrics);
            AssertDomain(FormalArtRegistry.EquipmentSlots);
            Assert.That(FormalArtRegistry.EquipmentItems.Select(entry => entry.RuntimeId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(FormalArtRegistry.EquipmentItems.Count));
            AssertDomain(FormalArtRegistry.MapStates);
            AssertDomain(FormalArtRegistry.MapNodeFrames);
            AssertDomain(FormalArtRegistry.MapNodeMarkers);
            AssertDomain(FormalArtRegistry.MapRegions);
            AssertDomain(FormalArtRegistry.MapDecor);
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
            Assert.That(FormalArtRegistry.Intents.Count, Is.EqualTo(5));
            Assert.That(FormalArtRegistry.Environments.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.NodeTypes.Count, Is.EqualTo(9));
            Assert.That(FormalArtRegistry.Navigation.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.Semantics.Count, Is.EqualTo(3));
            Assert.That(FormalArtRegistry.Elements.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.ResourceMetrics.Count, Is.EqualTo(16));
            Assert.That(FormalArtRegistry.EquipmentSlots.Count, Is.EqualTo(11));
            Assert.That(FormalArtRegistry.EquipmentItems.Count, Is.EqualTo(32));
            Assert.That(FormalArtRegistry.MapStates.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapNodeFrames.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapNodeMarkers.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapRegions.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.MapDecor.Count, Is.EqualTo(3));
            Assert.That(FormalArtRegistry.RuntimeSkills.Count, Is.EqualTo(27));
            Assert.That(FormalArtRegistry.FireSpells.Count, Is.EqualTo(60));
            Assert.That(FormalArtRegistry.Items.Count, Is.EqualTo(54));
            Assert.That(FormalArtRegistry.Vfx.Count, Is.EqualTo(39));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.VfxPath("unknown_vfx"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.EquipmentIconPath("unknown_equipment"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.EquipmentFootprintPath("unknown_equipment"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.MapNodeFramePath("unknown_frame"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.MapNodeMarkerPath("unknown_marker"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.MapRegionPath("unknown_region"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.MapDecorPath("unknown_decor"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.ElementPath("unknown_element"));
        }

        private static void AssertDomain(System.Collections.Generic.IReadOnlyList<FormalArtEntry> entries)
        {
            Assert.That(entries.All(entry => !string.IsNullOrWhiteSpace(entry.RuntimeId) && !string.IsNullOrWhiteSpace(entry.ResourcePath)), Is.True);
            Assert.That(entries.Select(entry => entry.RuntimeId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(entries.Count));
        }
    }
}
