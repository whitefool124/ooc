using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteFireSpellMigrationIntegrationTests
    {
        [Test]
        public void Map7Upgrade_PreservesDirectEquipmentAndCreatesVisibleClaimsWithoutSilentLoss()
        {
            string[] current = new RogueliteMapRun(7123).ToJson().Split('|');
            string[] legacy = current.Take(26).ToArray();
            legacy[0] = "map7";
            legacy[20] = "F-P01,F-P04,F-P38,F-P50,BAD-ID";
            legacy[21] = "F-P01,F-P04";

            RogueliteMapRun upgraded = RogueliteMapRun.FromJson(string.Join("|", legacy));

            Assert.That(upgraded.OwnedFireSpellIds, Is.EqualTo(new[] { "F-P-R01" }));
            Assert.That(upgraded.EquippedFireSpellIds[0], Is.EqualTo("F-P-R01"));
            Assert.That(upgraded.EquippedFireSpellIds[1], Is.Null.Or.Empty);
            Assert.That(upgraded.PendingFireSpellReselections.Select(claim => claim.LegacyId), Is.EqualTo(new[] { "F-P04" }));
            Assert.That(upgraded.FireSpellRetirementCompensations.Select(claim => claim.LegacyId), Is.EqualTo(new[] { "F-P38", "F-P50" }));
            Assert.That(upgraded.FireSpellMigrationWarnings, Does.Contain("unknown_legacy_fire_spell:BAD-ID"));
            Assert.That(upgraded.AwaitingReward, Is.True);

            string map8 = upgraded.ToJson();
            RogueliteMapRun restored = RogueliteMapRun.FromJson(map8);
            Assert.That(restored.ToJson(), Is.EqualTo(map8), "map8 migration state must be idempotent");
        }

        [Test]
        public void ReselectClaim_UsesSameRarityAndRestoresOriginalEmptySlot()
        {
            string[] current = new RogueliteMapRun(99).ToJson().Split('|');
            string[] legacy = current.Take(26).ToArray(); legacy[0] = "map7";
            legacy[20] = "F-P04"; legacy[21] = ",F-P04";
            RogueliteMapRun upgraded = RogueliteMapRun.FromJson(string.Join("|", legacy));

            FireSpellDefinition choice = upgraded.CurrentFireSpellChoices[0];
            Assert.That(choice.Rarity, Is.EqualTo(FireSpellRarity.Rare));
            upgraded.ClaimFireSpell(choice.Id);

            Assert.That(upgraded.PendingFireSpellReselections, Is.Empty);
            Assert.That(upgraded.OwnedFireSpellIds, Does.Contain(choice.Id));
            Assert.That(upgraded.EquippedFireSpellIds[1], Is.EqualTo(choice.Id));
        }
    }
}
