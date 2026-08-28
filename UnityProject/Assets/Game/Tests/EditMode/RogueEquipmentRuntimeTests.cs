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
        public void M3Loadout_HasElevenSlotsAndTwoHandedMainLocksOffhand()
        {
            RogueEquipmentRuntime runtime = new RogueEquipmentRuntime(77);
            RogueEquipmentInstance spear = runtime.CreateInstance("spear", "ACA-EQ-MH02", EquipmentRarity.Uncommon, 0, "starter");
            RogueEquipmentInstance shield = runtime.CreateInstance("shield", "ACA-EQ-OH01", EquipmentRarity.Common, 1, "starter");
            runtime.AddToBackpack(spear); runtime.AddToBackpack(shield);

            Assert.That(runtime.Equipped.Count, Is.EqualTo(11));
            Assert.That(runtime.Equip("spear", RogueEquipmentSlot.MainHand), Is.True);
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
            string shield = runtime.Equipped[RogueEquipmentSlot.OffHand];
            Assert.That(runtime.UnequipToBackpack(RogueEquipmentSlot.Chest, 0, 0, false), Is.True);
            Assert.That(runtime.CanUnequipToBackpack(RogueEquipmentSlot.OffHand, 0, 0, false), Is.False);
            Assert.That(runtime.UnequipToBackpack(RogueEquipmentSlot.OffHand, 0, 0, false), Is.False);
            Assert.That(runtime.Equipped[RogueEquipmentSlot.OffHand], Is.EqualTo(shield));
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
        public void M3ShieldAction_UsesOneApRarityValueAndLocksFacing()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(100);
            CombatState combat = BuildCombat(out UnitState hero);
            combat.AttachRogueEquipmentRuntime(runtime);
            CombatResolver.BeginTurn(combat, "hero");
            int ap = hero.ActionPoints;

            Assert.That(runtime.UseEquippedShield(combat, "hero", Facing.North), Is.True);
            Assert.That(hero.ActionPoints, Is.EqualTo(ap - 1));
            Assert.That(hero.Shield, Is.EqualTo(6));
            Assert.That(runtime.IsFacingLocked("hero"), Is.True);
        }

        [Test]
        public void M3Quickbar_IsFourTacticalSlotsAndRejectsEquipment()
        {
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(101);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T04", 0, "starter");
            runtime.AddTacticalToBackpack(item);
            Assert.That(runtime.AssignQuickbar(0, "tool"), Is.True);
            Assert.That(runtime.AssignQuickbar(1, runtime.Equipped[RogueEquipmentSlot.Chest]), Is.False);
            Assert.That(runtime.ItemQuickbarInstanceIds.Length, Is.EqualTo(4));
        }

        [Test]
        public void M3ReforgeAndCalibration_AreSeededAndNeverCreateDurability()
        {
            RogueEquipmentRuntime first = new RogueEquipmentRuntime(2026);
            RogueEquipmentRuntime second = new RogueEquipmentRuntime(2026);
            RogueEquipmentInstance a = first.CreateInstance("rare", "ACA-EQ-HD02", EquipmentRarity.Rare, 0, "reward");
            RogueEquipmentInstance b = second.CreateInstance("rare", "ACA-EQ-HD02", EquipmentRarity.Rare, 0, "reward");
            first.AddToBackpack(a); second.AddToBackpack(b);
            int goldA = 30, goldB = 30;
            Assert.That(first.TryReforge("rare", ref goldA), Is.True);
            Assert.That(second.TryReforge("rare", ref goldB), Is.True);
            Assert.That(a.MutableAffixIds, Is.EqualTo(b.MutableAffixIds));
            Assert.That(goldA, Is.EqualTo(24));
            Assert.That(typeof(RogueEquipmentInstance).GetProperties().Select(value => value.Name), Has.None.Contains("Durability"));
        }

        [Test]
        public void M7EquipmentAndTacticalCharges_RoundTripThroughRogue11WithoutShieldOrDurability()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("roundtrip", 303); RogueEquipmentRuntime runtime = RogueEquipmentRuntime.CreateStarter(303);
            RogueTacticalItemInstance item = runtime.CreateTacticalItem("tool", "G-T04", 2, "reward"); runtime.AddTacticalToBackpack(item); runtime.AssignQuickbar(0, item.InstanceId); item.Consume();
            runtime.WriteToDto(dto); RogueRunDto restoredDto = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(dto)); RogueEquipmentRuntime restored = RogueEquipmentRuntime.FromDto(restoredDto);
            Assert.That(restored.Equipped.Count, Is.EqualTo(11)); Assert.That(restored.TacticalItem("tool").ChargesCurrent, Is.EqualTo(item.ChargesMaximum - 1));
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

        private static CombatState BuildCombat(out UnitState hero)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState combat = new CombatState(new GridMap(2, 1), new[] { hero, enemy });
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            return combat;
        }
    }
}
