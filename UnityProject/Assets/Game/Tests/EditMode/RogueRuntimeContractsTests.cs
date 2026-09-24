using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.Tests
{
    public sealed class RogueRuntimeContractsTests
    {
        [Test]
        public void M0Catalog_LoadsFrozenContentAndSlotContracts()
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();

            Assert.That(catalog.Spells.Count, Is.EqualTo(87));
            Assert.That(catalog.Spells.Count(value => value.RewardEligible), Is.EqualTo(80));
            Assert.That(catalog.Spells.Count(value => value.IsBasic), Is.EqualTo(4));
            Assert.That(catalog.Spells.Count(value => value.Role == "passive"), Is.EqualTo(3));
            Assert.That(catalog.Equipment.Count, Is.EqualTo(33));
            Assert.That(catalog.Affixes.Count, Is.EqualTo(13));
            Assert.That(catalog.TacticalItems.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(EquipmentSlotRules.ActiveSlots.Count, Is.EqualTo(9));
            Assert.That(RogueRuntimeConstants.SpellSlotCount, Is.EqualTo(8));
            Assert.That(RogueRuntimeConstants.ItemQuickbarSize, Is.EqualTo(4));
        }

        [Test]
        public void M0Catalog_ValidatesStableIdsShieldExclusionAndRewardEquivalence()
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            RogueValidationResult result = RogueContentValidator.Validate(catalog);

            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Errors));
            Assert.That(catalog.Spells.Where(value => value.IsBasic), Has.All.Matches<SpellDefinition>(value => !value.RewardEligible));
            Assert.That(catalog.Spells.Where(value => value.RewardEligible).Select(value => value.DefinitionId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(80));
            Assert.That(catalog.Equipment, Has.All.Matches<EquipmentDefinition>(value => !value.HasDurability && value.Armor == 0 && value.BlockChance == 0));
            Assert.That(catalog.Equipment.Single(value => value.DefinitionId == "ACA-EQ-CH01").TurnStartShield, Is.EqualTo(2));
        }

        [TestCase("armor")]
        [TestCase("block")]
        [TestCase("durability")]
        [TestCase("armor_pierce")]
        [TestCase("maximum_total_shield")]
        public void M0Validator_RejectsRemovedFields(string field)
        {
            RogueValidationResult result = RogueContentValidator.ValidateSerializedFieldNames(new[] { "definition_id", field });
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void M0DamageBreakStanceAndShieldSamples_AreExplicit()
        {
            DamagePacket packet = RogueContentCatalog.CreateDamagePacketSample();
            BreakStanceState breakStance = new BreakStanceState("enemy", 4);
            ShieldSourceRecord shield = new ShieldSourceRecord("equipment:chest-01", 4, ShieldEventKind.Granted);

            Assert.That(packet.Components.Sum(value => value.RawAmount), Is.EqualTo(12));
            Assert.That(packet.SourceEffectId, Is.EqualTo("BASE-FIRE-MELEE"));
            Assert.That(breakStance.TargetUnitId, Is.EqualTo("enemy"));
            Assert.That(shield.SourceId, Does.StartWith("equipment:"));
        }

        [Test]
        public void M0Rogue11Dto_RoundTripsWithoutShieldThirdCurrencyOrDurability()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("run-contract", 1701);
            dto.CurrentHealth = 17;
            dto.CurrentMana = 9;
            dto.Gold = 8;
            dto.StageContribution = 2;
            dto.RouteHistoryNodeIds.AddRange(new[] { "start", "B1", "EV1" });
            dto.EquipmentInstances.Add(new EquipmentInstanceDto("eq-1", "ACA-EQ-CH01", RogueEquipmentSlot.Chest, EquipmentRarity.Common, 0));

            string encoded = Rogue11Serializer.Serialize(dto);
            RogueRunDto restored = Rogue11Serializer.Deserialize(encoded);

            Assert.That(encoded, Does.StartWith("rogue11|"));
            Assert.That(encoded, Does.Not.Contain("shield_balance"));
            Assert.That(encoded, Does.Not.Contain("durability"));
            Assert.That(restored.RunId, Is.EqualTo(dto.RunId));
            Assert.That(restored.EquippedSpellIds, Is.EqualTo(dto.EquippedSpellIds));
            Assert.That(restored.ItemQuickbarInstanceIds.Length, Is.EqualTo(4));
            Assert.That(restored.EquipmentSlotInstanceIds.Count, Is.EqualTo(9));
            Assert.That(restored.EquipmentInstances.Single().DefinitionId, Is.EqualTo("ACA-EQ-CH01"));
            Assert.That(restored.RouteHistoryNodeIds, Is.EqualTo(dto.RouteHistoryNodeIds));
        }
    }
}
