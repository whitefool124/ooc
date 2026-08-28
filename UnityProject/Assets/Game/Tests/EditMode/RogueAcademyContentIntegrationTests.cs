using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueAcademyContentIntegrationTests
    {
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
        public void M6FormalCatalog_HasFrozenCountsAndNoRemovedDefenseOrDurabilityFields()
        {
            RogueAcademyContentService service = new RogueAcademyContentService();
            Assert.That(service.Equipment.Count, Is.EqualTo(32)); Assert.That(service.Affixes.Count, Is.EqualTo(14));
            Assert.That(service.Equipment.Count(value => value.UpgradeNodes.Count > 0), Is.EqualTo(8));
            Assert.That(service.AllEligibleSpellIds.Count, Is.EqualTo(60)); Assert.That(service.AllEligibleSpellIds.Distinct().Count(), Is.EqualTo(60));
            Assert.That(service.Equipment.All(value => !value.HasDurability && value.Armor == 0 && value.BlockChance == 0), Is.True);
            Assert.That(service.Equipment.Any(value => value.DefinitionId.Contains("rifle") || value.DefinitionId.Contains("gun")), Is.False);
        }

        [Test]
        public void M6RewardService_UsesOnlyAcademySpellAndEquipmentDefinitionsDeterministically()
        {
            RogueAcademyContentService service = new RogueAcademyContentService();
            var first = service.Roll(620, "combat", SpellRarity.Common, EquipmentRarity.Common, 3, 2);
            var second = service.Roll(620, "combat", SpellRarity.Common, EquipmentRarity.Common, 3, 2);
            Assert.That(first.Select(value => value.DefinitionId), Is.EqualTo(second.Select(value => value.DefinitionId)));
            Assert.That(first.All(value => value.Kind == "spell" ? service.AllEligibleSpellIds.Contains(value.DefinitionId) : service.Equipment.Any(item => item.DefinitionId == value.DefinitionId)), Is.True);
            Assert.That(first.Select(value => value.EquivalenceGroupId).Where(value => !string.IsNullOrEmpty(value)).Distinct().Count(), Is.EqualTo(first.Count));
        }

        [Test]
        public void M6EnemyBaselines_RemoveArmorAndBlock_AndOnlyNamedSpecialistsStartWithShield()
        {
            RogueAcademyContentService service = new RogueAcademyContentService();
            foreach (EnemyArchetype archetype in EnemyArchetypes.All)
            {
                UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
                UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West); archetype.Apply(enemy);
                CombatState combat = new CombatState(new GridMap(2, 1), new[] { hero, enemy }); combat.ConfigureRuleset(CombatRuleset.Roguelite);
                service.ApplyEnemyBaseline(combat, enemy);
                RogueEnemyBaselineDefinition baseline = service.EnemyBaselines.Single(value => value.ArchetypeId == archetype.Id);
                Assert.That(enemy.Armor, Is.Zero, archetype.Id); Assert.That(enemy.Block, Is.Zero, archetype.Id);
                Assert.That(enemy.Shield, Is.EqualTo(baseline.StartingShield), archetype.Id);
                if (enemy.Shield > 0) Assert.That(combat.EventLog.Any(line => line.Contains(baseline.ShieldSourceId)), Is.True, archetype.Id);
            }
        }

        [Test]
        public void M6LegacyArmorBreakEffects_MapToBreakStanceOnRoguelitePath()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState combat = new CombatState(new GridMap(2, 1), new[] { hero, enemy }); combat.ConfigureRuleset(CombatRuleset.Roguelite);
            combat.TryGrantRogueliteShield("enemy", "test", 5);
            CombatEffectExecutor.Execute(combat, "hero", CombatEffect.ApplyStatus("enemy", StatusType.BreakStance, 1));
            Assert.That(enemy.HasStatus(StatusType.BreakStance), Is.True); Assert.That(enemy.HasStatus(StatusType.ArmorBreak), Is.False); Assert.That(enemy.Shield, Is.Zero);
        }

        [Test]
        public void M6FormalMapSettlement_OffersAcademyEquipmentAndPersistsItAsRogue11Instance()
        {
            Store store = new Store(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store); RogueliteMapRun run = new RogueliteMapRun(620);
            Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError);
            RogueliteMapNode combatNode = run.AvailableNodes.First(value => value.IsCombat); run.SelectNode(combatNode.Id); run.CompleteCurrentCombat();
            RogueliteReward[] rewards = run.CurrentRewards.ToArray();
            Assert.That(rewards.Length, Is.EqualTo(3)); Assert.That(rewards.Count(value => value.Kind == RogueliteRewardKind.Equipment), Is.EqualTo(1));
            Assert.That(rewards.Any(value => value.Id == "war_hammer" || value.Id == "arcane_wand" || value.Id == "medkit" || value.Id == "shield_cell"), Is.False);
            RogueliteReward equipment = rewards.Single(value => value.Kind == RogueliteRewardKind.Equipment); run.ClaimReward(equipment.Id);
            Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError); Assert.That(gateway.TryLoadRogueRun(out RogueRunDto dto), Is.True, gateway.LastError);
            Assert.That(dto.EquipmentInstances.Any(value => value.DefinitionId == equipment.Id), Is.True);
        }
    }
}
