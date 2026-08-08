using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class FireSpellSaveMigrationTests
    {
        [Test]
        public void Table_CoversAllLegacyIdsInOrderWithUniqueDirectTargets()
        {
            FireSpellSaveMigration.ValidateCoverageAndUniqueness();
            Assert.That(FireSpellSaveMigration.All.Select(entry => entry.LegacyId),
                Is.EqualTo(Enumerable.Range(1, 50).Select(index => $"F-P{index:00}")));
            Assert.That(FireSpellSaveMigration.All.Count(entry => entry.Kind == FireSpellSaveMigrationKind.Direct), Is.EqualTo(21));
            Assert.That(FireSpellSaveMigration.All.Count(entry => entry.Kind == FireSpellSaveMigrationKind.ReselectSameRarity), Is.EqualTo(26));
            Assert.That(FireSpellSaveMigration.All.Count(entry => entry.Kind == FireSpellSaveMigrationKind.Compensation), Is.EqualTo(3));
            Assert.That(FireSpellSaveMigration.All.Where(entry => entry.Kind == FireSpellSaveMigrationKind.Direct)
                .Select(entry => entry.DirectTargetId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(21));
        }

        [Test]
        public void Table_DoesNotSilentlyMapKnownSemanticChanges()
        {
            Assert.That(FireSpellSaveMigration.Get("F-P04").Kind, Is.EqualTo(FireSpellSaveMigrationKind.ReselectSameRarity));
            Assert.That(FireSpellSaveMigration.Get("F-P19").Kind, Is.EqualTo(FireSpellSaveMigrationKind.ReselectSameRarity));
            Assert.That(FireSpellSaveMigration.Get("F-P42").Kind, Is.EqualTo(FireSpellSaveMigrationKind.ReselectSameRarity));
            Assert.That(FireSpellSaveMigration.Get("F-P43").Kind, Is.EqualTo(FireSpellSaveMigrationKind.ReselectSameRarity));
            Assert.That(FireSpellSaveMigration.Get("F-P38").Kind, Is.EqualTo(FireSpellSaveMigrationKind.Compensation));
            Assert.That(FireSpellSaveMigration.Get("F-P40").Kind, Is.EqualTo(FireSpellSaveMigrationKind.Compensation));
            Assert.That(FireSpellSaveMigration.Get("F-P50").Kind, Is.EqualTo(FireSpellSaveMigrationKind.Compensation));
        }

        [Test]
        public void Table_OnlyReferencesExistingV02CatalogIds()
        {
            string[] currentIds = FireSpellCatalog.All.Select(spell => spell.Id).ToArray();
            foreach (FireSpellSaveMigrationEntry entry in FireSpellSaveMigration.All)
            {
                if (entry.Kind == FireSpellSaveMigrationKind.Direct)
                    Assert.That(currentIds, Does.Contain(entry.DirectTargetId), entry.LegacyId + " direct target");
                foreach (string referenceId in entry.SemanticReferenceIds)
                    Assert.That(currentIds, Does.Contain(referenceId), entry.LegacyId + " semantic reference");
            }
        }

        [Test]
        public void Migrate_IsPureDeterministicAndPreservesOnlyDirectEquipment()
        {
            string[] owned = { "F-P38", "F-P04", "F-P01", "F-P04", "F-P49" };
            string[] equipped = { "F-P04", "F-P01", "F-P38", "F-P49" };

            FireSpellSaveMigrationResult first = FireSpellSaveMigration.Migrate(owned, equipped);
            FireSpellSaveMigrationResult second = FireSpellSaveMigration.Migrate(owned.Reverse(), equipped);

            Assert.That(first.DirectOwnedIds, Is.EqualTo(new[] { "F-P-R01", "F-P-U11" }));
            Assert.That(first.EquippedNewIds, Is.EqualTo(new string[] { null, "F-P-R01", null, "F-P-U11" }));
            Assert.That(first.ReselectClaims.Select(claim => claim.LegacyId), Is.EqualTo(new[] { "F-P04" }));
            Assert.That(first.ReselectClaims[0].OriginalEquippedSlots, Is.EqualTo(new[] { 0 }));
            Assert.That(first.CompensationClaims.Select(claim => claim.LegacyId), Is.EqualTo(new[] { "F-P38" }));
            Assert.That(first.CompensationClaims[0].OriginalEquippedSlots, Is.EqualTo(new[] { 2 }));
            Assert.That(Signature(second), Is.EqualTo(Signature(first)));
        }

        [Test]
        public void Migrate_AllLegacyEntriesHasCapacitySafeRareOutcomeCounts()
        {
            string[] all = Enumerable.Range(1, 50).Select(index => $"F-P{index:00}").ToArray();
            FireSpellSaveMigrationResult result = FireSpellSaveMigration.Migrate(all, Array.Empty<string>());

            Assert.That(result.DirectOwnedIds.Count, Is.EqualTo(21));
            Assert.That(result.ReselectClaims.Count, Is.EqualTo(26));
            Assert.That(result.CompensationClaims.Count, Is.EqualTo(3));
            Assert.That(result.ReselectClaims.Count(claim => claim.Rarity == FireSpellRarity.Rare), Is.EqualTo(10));
            Assert.That(result.UnknownLegacyIds, Is.Empty);
            Assert.That(result.OrphanedEquippedSlots, Is.Empty);
        }

        [Test]
        public void Migrate_ReportsUnknownAndKnownButUnownedEquippedIdsWithoutInventingOwnership()
        {
            FireSpellSaveMigrationResult result = FireSpellSaveMigration.Migrate(
                new[] { "F-P01", "bad-owned" }, new[] { "F-P02", "bad-equipped", "F-P01" });

            Assert.That(result.DirectOwnedIds, Is.EqualTo(new[] { "F-P-R01" }));
            Assert.That(result.EquippedNewIds, Is.EqualTo(new string[] { null, null, "F-P-R01" }));
            Assert.That(result.UnknownLegacyIds, Is.EqualTo(new[] { "bad-equipped", "bad-owned" }));
            Assert.That(result.OrphanedEquippedSlots, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void Get_RejectsUnknownLegacyId()
        {
            Assert.That(FireSpellSaveMigration.TryGet("F-P01", out FireSpellSaveMigrationEntry entry), Is.True);
            Assert.That(entry.DirectTargetId, Is.EqualTo("F-P-R01"));
            Assert.That(FireSpellSaveMigration.TryGet("F-P-M01", out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => FireSpellSaveMigration.Get("F-P-M01"));
        }

        private static string Signature(FireSpellSaveMigrationResult result)
        {
            return string.Join("|", new[]
            {
                string.Join(",", result.DirectOwnedIds),
                string.Join(",", result.EquippedNewIds.Select(id => id ?? "<empty>")),
                string.Join(",", result.ReselectClaims.Select(claim => claim.ClaimId + "@" + string.Join("+", claim.OriginalEquippedSlots))),
                string.Join(",", result.CompensationClaims.Select(claim => claim.ClaimId + "@" + string.Join("+", claim.OriginalEquippedSlots))),
                string.Join(",", result.UnknownLegacyIds),
                string.Join(",", result.OrphanedEquippedSlots)
            });
        }
    }
}
