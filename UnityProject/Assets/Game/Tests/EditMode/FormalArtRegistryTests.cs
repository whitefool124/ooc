using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FormalArtRegistryTests
    {
        [Test]
        public void EveryCombatFeedback_HasLoadableFloatingTextIcon()
        {
            GameObject root = new GameObject("feedback-icon-regression");
            try
            {
                var feedback = root.AddComponent<CombatVisualFeedback>();
                var load = typeof(CombatVisualFeedback).GetMethod("SemanticIcon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                foreach (CombatFeedbackKind kind in Enum.GetValues(typeof(CombatFeedbackKind)))
                    Assert.That(load.Invoke(feedback, new object[] { CombatFeedbackCatalog.For(kind).Key }), Is.Not.Null, kind.ToString());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase("shieldguard")]
        [TestCase("stone_snare")]
        [TestCase("raider")]
        [TestCase("elite_vanguard")]
        [TestCase("rune_arbalist")]
        public void StaticRefresh_DoesNotSwapToLegacyActionArtwork(string id)
        {
            var assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            var unit = new UnitState("enemy", false, new GridPosition(0, 0));
            EnemyArchetypes.Get(id).Apply(unit);
            Texture2D rest = assets.Unit(unit);
            Assert.That(rest, Is.Not.Null);
            Assert.That(rest.width, Is.EqualTo(32));
            Assert.That(rest.height, Is.EqualTo(64));
            Assert.That(assets.Unit(unit, 0), Is.SameAs(rest));
            Assert.That(assets.Unit(unit, 1), Is.SameAs(rest));
            Assert.That(assets.Portrait(unit), Is.Not.SameAs(rest));
            Assert.That(assets.Portrait(unit).width, Is.EqualTo(128));
        }

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
            // 新单位（老寻／灯台值守／小铃／老库管／试制员）正式美术尚未入库，暂时复用最接近的既有剪影；
            // 这五条以外的资源路径必须唯一，正式图入库后应恢复"全部唯一"的口径。
            string[] placeholderIds = { "elder_tracker_hound", "signal_keeper", "wind_librarian", "legacy_storekeeper", "prototype_hand" };
            Assert.That(FormalArtRegistry.Units.Count, Is.EqualTo(17));
            Assert.That(FormalArtRegistry.Units.Select(entry => entry.RuntimeId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(17));
            string[] uniquePaths = FormalArtRegistry.Units.Where(entry => !placeholderIds.Contains(entry.RuntimeId))
                .Select(entry => entry.ResourcePath).ToArray();
            Assert.That(uniquePaths.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(uniquePaths.Length));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => FormalArtRegistry.UnitPath("unknown_unit"));
        }

        [Test]
        public void FrozenCoverageCounts_AreExplicit()
        {
            Assert.That(FormalArtRegistry.Commands.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.Statuses.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.Intents.Count, Is.EqualTo(5));
            Assert.That(FormalArtRegistry.Environments.Count, Is.EqualTo(10));
            Assert.That(FormalArtRegistry.NodeTypes.Count, Is.EqualTo(10));
            Assert.That(FormalArtRegistry.Navigation.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.Semantics.Count, Is.EqualTo(3));
            Assert.That(FormalArtRegistry.Elements.Count, Is.EqualTo(8));
            Assert.That(FormalArtRegistry.ResourceMetrics.Count, Is.EqualTo(15));
            Assert.That(FormalArtRegistry.EquipmentSlots.Count, Is.EqualTo(9));
            Assert.That(FormalArtRegistry.EquipmentItems.Count, Is.EqualTo(33));
            Assert.That(FormalArtRegistry.MapStates.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapNodeFrames.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapNodeMarkers.Count, Is.EqualTo(7));
            Assert.That(FormalArtRegistry.MapRegions.Count, Is.EqualTo(6));
            Assert.That(FormalArtRegistry.MapDecor.Count, Is.EqualTo(3));
            Assert.That(FormalArtRegistry.RuntimeSkills.Count, Is.EqualTo(27));
            Assert.That(FormalArtRegistry.FireSpells.Count, Is.EqualTo(80));
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
