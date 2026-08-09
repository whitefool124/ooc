using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class FireRogueliteExperienceTests
    {
        [TestCase(FireRogueliteStarterCatalog.Melee, "war_hammer", "F-P-M01", "F-P-M02")]
        [TestCase(FireRogueliteStarterCatalog.Universal, null, "F-P-U01", "F-P-U02")]
        [TestCase(FireRogueliteStarterCatalog.Ranged, "arcane_wand", "F-P-R01", "F-P-R03")]
        public void StarterRoute_FirstBattleHasCompatibleWeaponAndTwoExecutableFireSpells(string starterId, string weaponId, string firstId, string secondId)
        {
            RogueliteMapRun run = new RogueliteMapRun(8401, starterId);
            Assert.That(run.EquippedWeaponId, Is.EqualTo(weaponId));
            Assert.That(run.EquippedFireSpellIds, Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(run.EquippedFireSpellIds.Select(FireSpellCatalog.Get).All(spell => FireSpellCatalog.IsWeaponCompatible(spell, run.EquippedWeapon)), Is.True);

            foreach (string spellId in run.EquippedFireSpellIds)
            {
                TrainingRangeSession session = new TrainingRangeSession(); session.Select(spellId); session.PrepareCurrent();
                Assert.That(session.PreviewCurrent().CanCommit, Is.True, spellId);
                Assert.That(session.ExecuteCurrent().Signature, Is.Not.Empty, spellId);
            }
        }

        [Test]
        public void Map9_RoundTripPersistsHealthShieldManaAndNextBattleDoesNotRefill()
        {
            RogueliteMapRun run = new RogueliteMapRun(8402, FireRogueliteStarterCatalog.Ranged);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East); run.ApplyBuild(hero);
            CombatState combat = new CombatState(new GridMap(4, 4), new[] { hero });
            CombatEffectExecutor.Execute(combat, hero.Id, CombatEffect.DamageHealth(hero.Id, 5), CombatEffect.AbsorbShield(hero.Id, 1), CombatEffect.SpendMana(4));
            run.CaptureCombatInventory(combat);

            string data = run.ToJson(); Assert.That(data, Does.StartWith("map10|"));
            RogueliteMapRun restored = RogueliteMapRun.FromJson(data);
            UnitState next = new UnitState("hero", true, new GridPosition(0, 0), Facing.East); restored.ApplyBuild(next);
            Assert.That((next.Health, next.Shield, next.Mana), Is.EqualTo((13, 1, 8)));
            Assert.That(restored.ToJson(), Is.EqualTo(data));
        }

        [Test]
        public void Map8Upgrade_UsesExplicitNoSnapshotDefaultsAndWritesMap9()
        {
            string[] map9 = new RogueliteMapRun(8403).ToJson().Split('|'); string[] map8 = map9.Take(31).ToArray(); map8[0] = "map8";
            RogueliteMapRun restored = RogueliteMapRun.FromJson(string.Join("|", map8));
            Assert.That(restored.HasCombatSnapshot, Is.False); Assert.That(restored.StarterId, Is.Null.Or.Empty);
            Assert.That(restored.ToJson(), Does.StartWith("map10|"));
        }

        [Test]
        public void WorkshopRejectsWeaponThatWouldInvalidateEquippedFireSpells()
        {
            RogueliteMapRun run = new RogueliteMapRun(8404, FireRogueliteStarterCatalog.Melee);
            run.SelectNode("supply_checkpoint"); run.SelectNode("field_workshop"); run.ChooseCurrentNodeContent("wand_calibration");
            Assert.Throws<InvalidOperationException>(() => run.EquipReward("arcane_wand"));
            Assert.That(run.EquippedWeaponId, Is.EqualTo("war_hammer"));
        }

        [Test]
        public void RestRoomRestoresPublishedVitalsWithoutAutomaticFullRecovery()
        {
            RogueliteMapRun run = new RogueliteMapRun(8405, FireRogueliteStarterCatalog.Universal);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East); run.ApplyBuild(hero);
            CombatState combat = new CombatState(new GridMap(4, 4), new[] { hero });
            CombatEffectExecutor.Execute(combat, hero.Id, CombatEffect.DamageHealth(hero.Id, 10), CombatEffect.AbsorbShield(hero.Id, 2), CombatEffect.SpendMana(8));
            run.CaptureCombatInventory(combat);
            run.SelectNode("supply_checkpoint"); run.ChooseCurrentNodeContent("medical_cache");
            run.SelectNode("field_workshop"); run.ChooseCurrentNodeContent("supply_strip");
            run.SelectNode("med_bay"); run.ChooseCurrentNodeContent("field_repair");
            Assert.That((run.CurrentHealth, run.CurrentShield, run.CurrentMana), Is.EqualTo((14, 2, 8)));
            Assert.That(run.Supplies, Is.EqualTo(3));
        }

        [Test]
        public void EliteRewardIsOneCompatibleRareSpellOneScrollAndOneArtifact()
        {
            RogueliteMapRun run = new RogueliteMapRun(8406, FireRogueliteStarterCatalog.Melee);
            var spells = FireSpellRewardPool.RollPersonalChoices(run.Seed, 3, RogueliteMapNodeType.Elite, run.OwnedFireSpellIds, run.EquippedWeapon);
            var support = RogueliteMapCatalog.RollFireSupportRewards(RogueliteMapNodeType.Elite);
            Assert.That(spells.Count, Is.EqualTo(1)); Assert.That(spells[0].Rarity, Is.EqualTo(FireSpellRarity.Rare));
            Assert.That(FireSpellCatalog.IsWeaponCompatible(spells[0], run.EquippedWeapon), Is.True);
            Assert.That(support.Count, Is.EqualTo(2));
            Assert.That(support[0].Id, Is.EqualTo(ItemCatalog.FirelineScroll.Id));
            Assert.That(support[1].Kind, Is.EqualTo(RogueliteRewardKind.Item));
            Assert.That(support[1].Item.Category, Is.EqualTo(ItemCategory.Artifact));
            Assert.That((ArtifactCatalog.Get(support[1].Id).ContentSources & ArtifactContentSource.EliteReward) != 0, Is.True);
            Assert.That(spells.Count + support.Count, Is.EqualTo(3));
        }

        [TestCase(FireRogueliteStarterCatalog.Melee, 620)]
        [TestCase(FireRogueliteStarterCatalog.Melee, 621)]
        [TestCase(FireRogueliteStarterCatalog.Universal, 620)]
        [TestCase(FireRogueliteStarterCatalog.Universal, 621)]
        [TestCase(FireRogueliteStarterCatalog.Ranged, 620)]
        [TestCase(FireRogueliteStarterCatalog.Ranged, 621)]
        public void ThreeFireStarters_TwoBossSeedsCompleteDeterministicFirstRegion(string starterId, int seed)
        {
            RogueliteMapRun run = RoundTrip(new RogueliteMapRun(seed, starterId));
            CompleteCombat(ref run, "rail_patrol");
            run.SelectNode("switchyard"); run.ChooseCurrentNodeContent("overload"); run.CompletePendingContentCombat(); run = RoundTrip(run);
            run.SelectNode("relay_event"); run.ChooseCurrentNodeContent("survey");
            run.SelectNode("med_bay"); run.ChooseCurrentNodeContent("field_repair");
            run.SelectNode("permit_archive"); run.ChooseCurrentNodeContent("survey");
            run.SelectNode("safety_room"); run.ChooseCurrentNodeContent("scan_routes");
            run.SelectNode("aether_refinery"); run.ChooseCurrentNodeContent("purify");
            CompleteCombat(ref run, "transmission_tower"); CompleteCombat(ref run, "core_approach"); CompleteCombat(ref run, "core_finale");
            Assert.That(run.IsComplete, Is.True); Assert.That(run.StarterId, Is.EqualTo(starterId));
            Assert.That(run.ToJson(), Is.EqualTo(RoundTrip(run).ToJson()));
        }

        private static void CompleteCombat(ref RogueliteMapRun run, string nodeId)
        {
            run.SelectNode(nodeId); run.CompleteCurrentCombat(); run = RoundTrip(run);
            FireSpellDefinition spell = run.CurrentFireSpellChoices.FirstOrDefault();
            if (spell != null) run.ClaimFireSpell(spell.Id); else run.ClaimReward(run.CurrentRewards[0].Id);
            run = RoundTrip(run);
        }

        private static RogueliteMapRun RoundTrip(RogueliteMapRun run) => RogueliteMapRun.FromJson(run.ToJson());
    }
}
