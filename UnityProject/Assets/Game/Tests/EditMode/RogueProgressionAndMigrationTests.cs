using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueProgressionAndMigrationTests
    {
        [Test]
        public void M7Rogue11NodeChoices_AreNeverGatedByLegacyPartsOrAether()
        {
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(RogueRunDto.CreateNew("ui-economy", 47));
            RogueliteNodeContentChoice pricedLegacyChoice = RogueliteMapCatalog.Nodes
                .SelectMany(RogueliteNodeContentCatalog.ChoicesFor)
                .First(choice => choice.PartsCost > 0 || choice.AetherCost > 0);

            UiOperationAvailability availability = RogueliteEconomyPresentation.ForNodeChoice(run, pricedLegacyChoice);

            Assert.That(availability.CanExecute, Is.True);
            Assert.That(availability.Reason, Does.Not.Contain("不足"));
        }

        private sealed class Store : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [Test]
        public void RewardPool_FiltersSourceRarityRoleOwnershipAndEquivalenceGroupDeterministically()
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            IReadOnlyList<SpellDefinition> first = RogueRewardPool.RollSpells(catalog, 620, 8, "combat", SpellRarity.Common,
                new[] { "F-P-M01" }, new[] { "melee", "universal", "ranged" });
            IReadOnlyList<SpellDefinition> second = RogueRewardPool.RollSpells(catalog, 620, 8, "combat", SpellRarity.Common,
                new[] { "F-P-M01" }, new[] { "melee", "universal", "ranged" });

            Assert.That(first.Select(value => value.DefinitionId), Is.EqualTo(second.Select(value => value.DefinitionId)));
            Assert.That(first.All(value => value.RewardEligible && value.Rarity == SpellRarity.Common && value.RewardSources.Contains("combat")), Is.True);
            Assert.That(first.Select(value => value.EquivalenceGroupId).Distinct().Count(), Is.EqualTo(first.Count));
            Assert.That(first.Any(value => value.DefinitionId == "F-P-M01"), Is.False);
        }

        [Test]
        public void EncounterBoundary_AdvancesAndRecoversOnlyWhenAlive_AndNeverCarriesShield()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("run", 7); dto.CurrentHealth = 9; dto.CurrentMana = 4;
            RogueStageResolution survived = RogueRunProgression.ResolveEncounter(dto, RogueEncounterOutcome.SurvivedFailure);
            Assert.That(survived.TimeAdvanced, Is.True); Assert.That(dto.StageTime, Is.EqualTo(1));
            Assert.That(dto.CurrentHealth, Is.EqualTo(13)); Assert.That(dto.CurrentMana, Is.EqualTo(5));
            Assert.That(typeof(RogueRunDto).GetProperty("CurrentShield"), Is.Null);
            RogueRunProgression.ResolveZeroTimeFunction(dto); Assert.That(dto.StageTime, Is.EqualTo(1));

            dto.CurrentHealth = 0;
            RogueStageResolution defeated = RogueRunProgression.ResolveEncounter(dto, RogueEncounterOutcome.Defeat);
            Assert.That(defeated.RunSealed, Is.True); Assert.That(defeated.TimeAdvanced, Is.False); Assert.That(dto.StageTime, Is.EqualTo(1));
        }

        [Test]
        public void NewSave_AlwaysWritesRogue11_AndRoundTripsWithoutThirdCurrencyOrEightSlotQuickbar()
        {
            Store store = new Store(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(620)), Is.True, gateway.LastError);
            string raw = store.Values[RogueliteSaveGateway.MapRunKey];
            Assert.That(raw, Does.StartWith("rogue11|")); Assert.That(raw, Does.Not.Contain("map10"));
            Assert.That(gateway.TryLoadRogueRun(out RogueRunDto dto), Is.True, gateway.LastError);
            Assert.That(dto.Gold, Is.EqualTo(8)); Assert.That(dto.StageContribution, Is.Zero);
            Assert.That(dto.ItemQuickbarInstanceIds, Has.Length.EqualTo(4));
        }

        [Test]
        public void Map10Load_CreatesTimestampedBackupReportAndRogue11Replacement()
        {
            Store store = new Store(); string legacy = new RogueliteMapRun(621, FireRogueliteStarterCatalog.Melee).ToJson();
            store.Values[RogueliteSaveGateway.MapRunKey] = legacy;
            DateTime stamp = new DateTime(2026, 8, 16, 7, 0, 0, DateTimeKind.Utc);
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store, () => stamp);

            Assert.That(gateway.TryLoadRogueRun(out RogueRunDto dto), Is.True, gateway.LastError);
            string id = "20260816T070000000Z";
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Does.StartWith("rogue11|"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey + RogueliteSaveGateway.LegacyBackupSuffix + id], Is.EqualTo(legacy));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey + RogueliteSaveGateway.MigrationReportSuffix + id], Does.Contain("shield:reset_to_zero"));
            Assert.That(dto.MigrationReportId, Is.EqualTo(id)); Assert.That(dto.Gold, Is.EqualTo(8)); Assert.That(dto.StageContribution, Is.Zero);
            Assert.That(dto.EquippedSpellIds.Take(4), Is.EqualTo(new[] { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER" }));
            Assert.That(dto.ReselectionClaimIds.Any(value => value.StartsWith("equipment:")), Is.True);
        }

        [Test]
        public void Rogue11MapProjection_ClampsLegacyHealthManaAndStartsCombatWithoutShield()
        {
            string[] fields = new RogueliteMapRun(622).ToJson().Split('|'); fields[33] = "999"; fields[34] = "77"; fields[35] = "99";
            RogueMigrationReport report; RogueRunDto dto = LegacyMap10Migrator.Migrate(RogueliteMapRun.FromJson(string.Join("|", fields)), "m", out report);
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(dto);
            Assert.That(dto.CurrentHealth, Is.EqualTo(18)); Assert.That(dto.CurrentMana, Is.EqualTo(12));
            Assert.That(restored.CurrentShield, Is.Zero);
        }
    }
}
