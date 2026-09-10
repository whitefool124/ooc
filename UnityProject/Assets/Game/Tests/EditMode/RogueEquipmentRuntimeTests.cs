using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.Tests
{
    public sealed class RogueEquipmentRuntimeTests
    {
        [Test]
        public void M3Loadout_HasNineSlotsAndLegacyOffhandContentIsInactive()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(77);
            RogueEquipmentInstance spear = runtime.CreateInstance("spear", "ACA-EQ-MH02", EquipmentRarity.Uncommon, 0, "starter");
            RogueEquipmentInstance shield = runtime.CreateInstance("shield", "ACA-EQ-OH01", EquipmentRarity.Common, 1, "starter");
            runtime.AddToBackpack(spear); runtime.AddToBackpack(shield);

            Assert.That(runtime.Equipped.Count, Is.EqualTo(9));
            Assert.That(runtime.Equip("spear", RogueEquipmentSlot.Weapon), Is.True);
            Assert.That(runtime.Equip("shield", RogueEquipmentSlot.OffHand), Is.False);
            Assert.That(runtime.Backpack.ContainsKey("shield"), Is.True);
        }

        [Test]
        public void M3EquipAndUnequip_MoveInstanceWithoutDuplicationOrLoss()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(88);
            RogueEquipmentInstance chest = runtime.CreateInstance("chest", "ACA-EQ-CH01", EquipmentRarity.Common, 0, "starter");
            runtime.AddToBackpack(chest);
            Assert.That(runtime.Equip("chest", RogueEquipmentSlot.Chest), Is.True);
            Assert.That(runtime.Backpack.ContainsKey("chest"), Is.False);
            Assert.That(runtime.Unequip(RogueEquipmentSlot.Chest), Is.True);
            Assert.That(runtime.Backpack.ContainsKey("chest"), Is.True);
            Assert.That(runtime.AllInstances.Count(value => value.InstanceId == "chest"), Is.EqualTo(1));
        }

        [Test]
        public void UxLoadout_EquipOrReplaceReturnsPreviousItemToFreedBackpackSpace()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(89);
            RogueEquipmentInstance equipped = runtime.CreateInstance("old-head", "ACA-EQ-HD01", EquipmentRarity.Common, 0, "starter");
            RogueEquipmentInstance replacement = runtime.CreateInstance("new-head", "ACA-EQ-HD02", EquipmentRarity.Rare, 1, "reward");
            Assert.That(runtime.AddToBackpack(equipped), Is.True);
            Assert.That(runtime.AddToBackpack(replacement), Is.True);
            Assert.That(runtime.Equip(equipped.InstanceId, RogueEquipmentSlot.Head), Is.True);
            RogueBackpackPlacement replacementPlacement = runtime.Backpack[replacement.InstanceId];

            Assert.That(runtime.CanEquipOrReplace(replacement.InstanceId, RogueEquipmentSlot.Head), Is.True);
            Assert.That(runtime.EquipOrReplace(replacement.InstanceId, RogueEquipmentSlot.Head), Is.True);

            Assert.That(runtime.Equipped[RogueEquipmentSlot.Head], Is.EqualTo(replacement.InstanceId));
            Assert.That(runtime.Backpack.ContainsKey(replacement.InstanceId), Is.False);
            Assert.That(runtime.Backpack.ContainsKey(equipped.InstanceId), Is.True);
            Assert.That(runtime.AllInstances.Count(value => value.InstanceId == equipped.InstanceId), Is.EqualTo(1));
            Assert.That(runtime.AllInstances.Count(value => value.InstanceId == replacement.InstanceId), Is.EqualTo(1));
            Assert.That(runtime.Backpack[equipped.InstanceId].X, Is.GreaterThanOrEqualTo(0));
            Assert.That(replacementPlacement.X, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void UxLoadout_UnequipToBackpackUsesRequestedLegalCellAndRejectsCollisionAtomically()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(90);
            string chest = runtime.Equipped[RogueEquipmentSlot.Chest];
            Assert.That(runtime.CanUnequipToBackpack(RogueEquipmentSlot.Chest, 3, 6, false), Is.True);
            Assert.That(runtime.UnequipToBackpack(RogueEquipmentSlot.Chest, 3, 6, false), Is.True);
            Assert.That(runtime.Equipped[RogueEquipmentSlot.Chest], Is.Empty);
            Assert.That(runtime.Backpack[chest].X, Is.EqualTo(3));
            Assert.That(runtime.Backpack[chest].Y, Is.EqualTo(6));

            Assert.That(runtime.Equip(chest, RogueEquipmentSlot.Chest), Is.True);
            string backpack = runtime.Equipped[RogueEquipmentSlot.Backpack];
            Assert.That(runtime.UnequipToBackpack(RogueEquipmentSlot.Chest, 0, 0, false), Is.True);
            Assert.That(runtime.CanUnequipToBackpack(RogueEquipmentSlot.Backpack, 0, 0, false), Is.False);
            Assert.That(runtime.UnequipToBackpack(RogueEquipmentSlot.Backpack, 0, 0, false), Is.False);
            Assert.That(runtime.Equipped[RogueEquipmentSlot.Backpack], Is.EqualTo(backpack));
        }

        [Test]
        public void M3TurnShield_StacksAcrossItemsButRejectsSameItemShieldAffix()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(99);
            RogueEquipmentInstance chest = runtime.CreateInstance("chest", "ACA-EQ-CH01", EquipmentRarity.Common, 0, "starter");
            RogueEquipmentInstance head = runtime.CreateInstance("head", "ACA-EQ-HD01", EquipmentRarity.Rare, 1, "reward");
            head.MutableAffixIds.Add("AFF-ROUND-SHIELD-P");
            runtime.AddToBackpack(chest); runtime.AddToBackpack(head);
            runtime.Equip("chest", RogueEquipmentSlot.Chest); runtime.Equip("head", RogueEquipmentSlot.Head);
            Assert.That(runtime.Validate().IsValid, Is.True);

            chest.MutableAffixIds.Add("AFF-ROUND-SHIELD-P");
            Assert.That(runtime.Validate().IsValid, Is.False);
        }

        [Test]
        public void M3Quickbar_IsFourTacticalSlotsAndRejectsEquipment()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(101);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T01", 0, "starter");
            runtime.AddTacticalToBackpack(item);
            Assert.That(runtime.AssignQuickbar(0, "tool"), Is.True);
            Assert.That(runtime.AssignQuickbar(1, runtime.Equipped[RogueEquipmentSlot.Chest]), Is.False);
            Assert.That(runtime.ItemQuickbarInstanceIds.Length, Is.EqualTo(4));
        }

        [Test]
        public void M3Forging_IsDeterministicOneTimeAndHasNoRandomRerollOrDurability()
        {
            RogueEquipmentRuntime first = new RogueEquipmentRuntime(2026);
            RogueEquipmentRuntime second = new RogueEquipmentRuntime(2026);
            RogueEquipmentInstance a = first.CreateInstance("rare", "ACA-EQ-CH01", EquipmentRarity.Rare, 0, "reward");
            RogueEquipmentInstance b = second.CreateInstance("rare", "ACA-EQ-CH01", EquipmentRarity.Rare, 0, "reward");
            first.AddToBackpack(a); second.AddToBackpack(b);
            int goldA = 30, goldB = 30;
            Assert.That(first.TryReforge("rare", ref goldA), Is.False);
            Assert.That(second.TryReforge("rare", ref goldB), Is.False);
            Assert.That(a.MutableAffixIds, Is.Empty);
            Assert.That(goldA, Is.EqualTo(30));
            Assert.That(first.Calibrate("rare", "node1", "turn_shield:+1"), Is.True);
            Assert.That(first.Calibrate("rare", "node1", "first_move:+1"), Is.False);
            Assert.That(typeof(RogueEquipmentInstance).GetProperties().Select(value => value.Name), Has.None.Contains("Durability"));
        }

        [Test]
        public void M7EquipmentAndTacticalCharges_RoundTripThroughRogue11WithoutShieldOrDurability()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("roundtrip", 303); RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(303);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T01", 2, "reward"); runtime.AddTacticalToBackpack(item); runtime.AssignQuickbar(0, item.InstanceId); item.Consume();
            runtime.WriteToDto(dto); RogueRunDto restoredDto = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(dto)); RogueEquipmentRuntime restored = RogueEquipmentRuntime.FromDto(restoredDto);
            Assert.That(restored.Equipped.Count, Is.EqualTo(9)); Assert.That(restored.TacticalItem("tool").ChargesCurrent, Is.EqualTo(item.ChargesMaximum - 1));
            Assert.That(restored.ItemQuickbarInstanceIds[0], Is.EqualTo("tool")); Assert.That(typeof(RogueEquipmentInstance).GetProperty("Durability"), Is.Null);
        }

        [Test]
        public void UxUnifiedInventory_MoveRotateAndPresentationUseOneRogueGridContract()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(404);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T01", 0, "starter");
            runtime.AddTacticalToBackpack(item);
            RogueBackpackPlacement start = runtime.Backpack[item.InstanceId];

            Assert.That(runtime.MoveBackpack(item.InstanceId, 3, 5, false), Is.True);
            Assert.That(runtime.RotateBackpack(item.InstanceId), Is.True);
            RogueInventoryItemPresentation presentation = RogueInventoryPresentation.Build(runtime).Single();

            Assert.That(presentation.X, Is.EqualTo(3)); Assert.That(presentation.Y, Is.EqualTo(5));
            Assert.That(presentation.Rotated, Is.True); Assert.That(presentation.DisplayName, Is.EqualTo("折盾匣"));
            Assert.That(presentation.ChargesMaximum, Is.EqualTo(3)); Assert.That(start.X, Is.Not.EqualTo(presentation.X));
        }

        [Test]
        public void UxUnifiedInventory_InvalidMovePreservesAuthoritativePlacement()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(405);
            string chest = runtime.Equipped[RogueEquipmentSlot.Chest];
            Assert.That(runtime.Unequip(RogueEquipmentSlot.Chest), Is.True);
            RogueBackpackPlacement before = runtime.Backpack[chest];

            Assert.That(runtime.CanMoveBackpack(chest, 5, 9, false), Is.False);
            Assert.That(runtime.MoveBackpack(chest, 5, 9, false), Is.False);
            Assert.That(runtime.Backpack[chest].X, Is.EqualTo(before.X));
            Assert.That(runtime.Backpack[chest].Y, Is.EqualTo(before.Y));
        }

        [Test]
        public void UxRogueQuickbar_TacticalInstanceUsesArtifactDefinitionAndOwnCharges()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(406);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T01", 0, "starter");
            Assert.That(runtime.AddTacticalToBackpack(item), Is.True);
            Assert.That(runtime.AssignQuickbar(0, item.InstanceId), Is.True);

            string quickbarId = runtime.ItemQuickbarInstanceIds[0];
            Assert.That(runtime.TacticalItem(quickbarId), Is.SameAs(item));
            Assert.That(ArtifactCatalog.Get(item.DefinitionId).Id, Is.EqualTo("G-T01"));
            int before = item.ChargesCurrent;
            Assert.That(item.Consume(), Is.True);
            Assert.That(item.ChargesCurrent, Is.EqualTo(before - 1));
        }

        [Test]
        public void UxRegression_NullQuickbarSlotsLoadAsEmptyAndCanClearAnAssignment()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("null-quickbar", 407);
            dto.ItemQuickbarInstanceIds[0] = null;
            RogueEquipmentRuntime restored = null;

            Assert.DoesNotThrow(() => restored = RogueEquipmentRuntime.FromDto(dto));
            Assert.That(restored.ItemQuickbarInstanceIds[0], Is.EqualTo(string.Empty));

            RogueTacticalItemInstance item = restored.CreateTacticalItem("tool", "G-T01", 0, "test");
            Assert.That(restored.AddTacticalToBackpack(item), Is.True);
            Assert.That(restored.AssignQuickbar(0, item.InstanceId), Is.True);
            Assert.That(restored.AssignQuickbar(0, null), Is.True);
            Assert.That(restored.ItemQuickbarInstanceIds[0], Is.EqualTo(string.Empty));
        }

        [Test]
        public void FirstRunLightCourierCoat_AddsOneOnlyToTheFirstMoveEachTurn()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(408);
            RogueEquipmentInstance coat = runtime.CreateInstance("first-run-coat", "ACA-EQ-CH04",
                EquipmentRarity.Uncommon, 0, "first-event");
            Assert.That(runtime.AddToBackpack(coat), Is.True);
            Assert.That(runtime.Equip(coat.InstanceId, RogueEquipmentSlot.Chest), Is.True);
            CombatState combat = BuildCombat(out UnitState hero);
            combat.AttachRogueEquipmentRuntime(runtime);

            CombatResolver.BeginTurn(combat, hero.Id);
            Assert.That(CombatMovementQuery.Budget(combat, hero), Is.EqualTo(UnitState.BaseMovementRange + 1));
            runtime.AfterMove(hero.Id);
            Assert.That(CombatMovementQuery.Budget(combat, hero), Is.EqualTo(UnitState.BaseMovementRange));
            CombatResolver.BeginTurn(combat, hero.Id);
            Assert.That(CombatMovementQuery.Budget(combat, hero), Is.EqualTo(UnitState.BaseMovementRange + 1));
        }

        [Test]
        public void FirstRunSeedbedCore_ReturnsTwoManaOnlyForFirstPaidPersonalSpellInBattle()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(409);
            RogueEquipmentInstance core = runtime.CreateInstance("first-run-core", "ACA-EQ-CR04",
                EquipmentRarity.Uncommon, 0, "battle-two-chest");
            Assert.That(runtime.AddToBackpack(core), Is.True);
            Assert.That(runtime.Equip(core.InstanceId, RogueEquipmentSlot.CastingUnit), Is.True);
            CombatState combat = BuildCombat(out UnitState hero);
            combat.AttachRogueEquipmentRuntime(runtime);

            CombatEffectExecutor.Execute(combat, hero.Id, CombatEffect.SpendMana(3));
            runtime.OnPersonalSpellPaid(combat, hero.Id, 3);
            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana - 1));
            CombatEffectExecutor.Execute(combat, hero.Id, CombatEffect.SpendMana(1));
            runtime.OnPersonalSpellPaid(combat, hero.Id, 1);
            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana - 2));

            Assert.That(runtime.Unequip(RogueEquipmentSlot.CastingUnit), Is.True);
            Assert.That(runtime.Equip(core.InstanceId, RogueEquipmentSlot.CastingUnit), Is.True);
            runtime.OnPersonalSpellPaid(combat, hero.Id, 1);
            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana - 2));
        }

        private static CombatState BuildCombat(out UnitState hero)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState combat = new CombatState(new GridMap(2, 1), new[] { hero, enemy });
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            return combat;
        }
    }
}
