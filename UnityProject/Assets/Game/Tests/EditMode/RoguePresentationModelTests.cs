using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RoguePresentationModelTests
    {
        [Test]
        public void UiM0MapStatusAndNodePreview_ExposeOnlyFrozenRogue11Information()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("ui-map", 901); dto.CurrentHealth = 9; dto.CurrentMana = 4; dto.Gold = 31; dto.StageContribution = 12; dto.StageTime = 20;
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(dto);
            RogueMapStatusPresentation status = new RogueMapStatusPresentation(run);
            RogueNodePreviewPresentation elite = new RogueNodePreviewPresentation(run, RogueliteMapCatalog.Nodes.First(value => value.Type == RogueliteMapNodeType.Elite));

            Assert.That(status.Health, Is.EqualTo(9)); Assert.That(status.Mana, Is.EqualTo(4)); Assert.That(status.Gold, Is.EqualTo(31));
            Assert.That(status.ConsolidationTime, Is.EqualTo(21)); Assert.That(status.WarningTime, Is.EqualTo(25)); Assert.That(status.TransitionTime, Is.EqualTo(28));
            Assert.That(elite.TimeCost, Is.EqualTo(3)); Assert.That(elite.ProjectedStageTime, Is.EqualTo(23));
            Assert.That(elite.ExpectedHealthRecovery, Is.EqualTo(9)); Assert.That(elite.ExpectedManaRecovery, Is.EqualTo(3));
            Assert.That(elite.CrossesConsolidation, Is.True); Assert.That(elite.CrossesTransition, Is.False);
            Assert.That(typeof(RogueMapStatusPresentation).GetProperty("Shield"), Is.Null);
            Assert.That(typeof(RogueMapStatusPresentation).GetProperty("Level"), Is.Null);
            Assert.That(typeof(RogueMapStatusPresentation).GetProperty("Experience"), Is.Null);
        }

        [Test]
        public void UiM0EncounterCosts_UsePublicTimeAndPerTimeRecovery()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("ui-time", 902); dto.CurrentHealth = 2; dto.CurrentMana = 1;
            RogueStageResolution result = RogueRunProgression.ResolveEncounter(dto, RogueEncounterOutcome.Success, 3);
            Assert.That(result.TimeCost, Is.EqualTo(3)); Assert.That(dto.StageTime, Is.EqualTo(3));
            Assert.That(dto.CurrentHealth, Is.EqualTo(14)); Assert.That(dto.CurrentMana, Is.EqualTo(4));
            Assert.That(AcademyMapTuning.TimeCost(RogueliteMapNodeType.Combat), Is.EqualTo(2));
            Assert.That(AcademyMapTuning.TimeCost(RogueliteMapNodeType.Event), Is.EqualTo(1));
            Assert.That(AcademyMapTuning.TimeCost(RogueliteMapNodeType.Shop), Is.Zero);
        }
        [Test]
        public void M5Hud_HasEightSpellsFourTacticalSlotsAndFiveDistinctResources()
        {
            CombatState combat = BuildCombat(out UnitState hero); hero.ConfigureMana(12, 7); combat.TryGrantRogueliteShield("hero", "test", 5);
            RogueSpellCombatRuntime spells = new RogueSpellCombatRuntime(combat, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            RogueEquipmentRuntime equipment = new RogueEquipmentRuntime(5); RogueTacticalItemInstance tool = equipment.CreateTacticalItem("tool", "G-T04", 0, "starter");
            equipment.AddTacticalToBackpack(tool); equipment.AssignQuickbar(0, tool.InstanceId);
            combat.AttachRogueSpellRuntime(spells); combat.AttachRogueEquipmentRuntime(equipment);
            RogueRunDto run = RogueRunDto.CreateNew("run", 5); run.Gold = 13; run.StageContribution = 4;

            RogueCombatHudPresentation model = new RogueCombatHudPresentation(combat, run);
            Assert.That(model.SpellSlots.Count, Is.EqualTo(8)); Assert.That(model.Quickbar.Count, Is.EqualTo(4));
            Assert.That((model.Health, model.Shield, model.Mana, model.Gold, model.StageContribution), Is.EqualTo((18, 5, 7, 13, 4)));
            Assert.That(model.SpellSlots.Take(4).All(value => !string.IsNullOrEmpty(value.DefinitionId)), Is.True);
            Assert.That(model.SpellSlots[0].CompactSlotLabel, Is.EqualTo("1"));
            Assert.That(model.SpellSlots[0].CompactSlotLabel, Does.Not.Contain(model.SpellSlots[0].DisplayName));
            Assert.That(RogueInventoryPresentation.ShouldDrawSourceItem("dragged", "dragged"), Is.False);
            Assert.That(RogueInventoryPresentation.ShouldDrawSourceItem("dragged", "other"), Is.True);
        }

        [Test]
        public void LoadoutDrag_MapsPointerToAuthoritativeBackpackAnchorAndRotationFootprint()
        {
            RogueLoadoutGridPoint anchor = RogueLoadoutDragPresentation.AnchorForLocalPointer(139f, -165f, 52f, 1, 1);
            Assert.That(anchor, Is.EqualTo(new RogueLoadoutGridPoint(1, 2)));
            Assert.That(RogueLoadoutDragPresentation.Footprint(1, 3, false), Is.EqualTo(new RogueLoadoutGridPoint(1, 3)));
            Assert.That(RogueLoadoutDragPresentation.Footprint(1, 3, true), Is.EqualTo(new RogueLoadoutGridPoint(3, 1)));
        }

        [Test]
        public void M5DamagePreview_UsesResolutionSequentiallyForEverySegment()
        {
            DamagePacket[] packets =
            {
                new DamagePacket("p0", "hero", "enemy", "s", new[]{new DamageComponent(DamageComponentKind.Fire, 6)}, segmentIndex:0, segmentCount:2),
                new DamagePacket("p1", "hero", "enemy", "s", new[]{new DamageComponent(DamageComponentKind.Fire, 6)}, segmentIndex:1, segmentCount:2)
            };
            var rows = RogueDamagePreviewPresentation.Build(packets, 8, 18);
            Assert.That(rows[0].Resolution.ShieldAbsorbed, Is.EqualTo(6)); Assert.That(rows[0].Resolution.HealthDamage, Is.Zero);
            Assert.That(rows[1].Resolution.ShieldAbsorbed, Is.EqualTo(2)); Assert.That(rows[1].Resolution.HealthDamage, Is.EqualTo(4));
        }

        [Test]
        public void M5ShieldLog_AlwaysNamesSourceAndEventKind()
        {
            string[] localizedKinds = { "获得", "破势阻止", "吸收", "回合开始清空", "破势浪费" };
            int index = 0;
            foreach (ShieldEventKind kind in Enum.GetValues(typeof(ShieldEventKind)))
            {
                string line = RogueShieldLogPresentation.Format(new ShieldSourceRecord("source-1", 4, kind, 2));
                Assert.That(line, Does.Contain("source-1")); Assert.That(line, Does.Contain(localizedKinds[index++]));
            }
        }

        [Test]
        public void M5EquipmentCard_HasNewFieldsAndNoArmorBlockOrDurability()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(6);
            string id = runtime.Equipped[OCC.Combat.Roguelite.EquipmentSlot.Chest];
            RogueEquipmentInstance instance = runtime.AllInstances.Single(value => value.InstanceId == id);
            RogueEquipmentCardPresentation card = new RogueEquipmentCardPresentation(instance, runtime.DefinitionFor(id));
            Assert.That(card.Slot, Is.EqualTo(OCC.Combat.Roguelite.EquipmentSlot.Chest)); Assert.That(card.ShieldSourceIds, Is.Not.Empty);
            string[] forbidden = { "Armor", "Block", "Durability" };
            Assert.That(typeof(RogueEquipmentCardPresentation).GetProperties().Select(value => value.Name), Has.None.Matches<string>(name => forbidden.Any(term => name.Contains(term))));
        }

        private static CombatState BuildCombat(out UnitState hero)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(2, 1), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite); return state;
        }
    }
}
