using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class ItemInventorySystemTests
    {
        [Test]
        public void BaseBackpack_IsSixByTenAndUsesStableFirstFit()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.Width, Is.EqualTo(6)); Assert.That(inventory.Height, Is.EqualTo(10));
            Assert.That(inventory.AddFirstFit(new ItemInstance("a", "F-T01", 0)).Success, Is.True);
            Assert.That(inventory.PlacementOf("a").Value.X, Is.EqualTo(0)); Assert.That(inventory.PlacementOf("a").Value.Y, Is.EqualTo(0));
        }

        [Test]
        public void Rotate_ExchangesWidthAndHeightAndRejectsBlockedOrientation()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            inventory.Place(new ItemInstance("artifact", "F-T01", 0), 5, 0, true);
            Assert.That(inventory.GetAt(5, 1)?.InstanceId, Is.EqualTo("artifact"));
            Assert.That(inventory.Rotate("artifact").Error, Is.EqualTo(InventoryError.OutOfBounds));
        }

        [Test]
        public void MoveSwapRemoveAndClone_AreIndependent()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            inventory.Place(new ItemInstance("a", "medkit", 0), 0, 0); inventory.Place(new ItemInstance("b", "shield_cell", 1), 1, 0);
            Assert.That(inventory.Swap("a", "b").Success, Is.True); Assert.That(inventory.Move("a", 2, 2).Success, Is.True);
            InventoryContainerState clone = inventory.Clone(); Assert.That(clone.Remove("a"), Is.Not.Null); Assert.That(inventory.Get("a"), Is.Not.Null);
        }

        [Test]
        public void Search_IsPureStableAndSupportsCategoryTextAndUses()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            inventory.Place(new ItemInstance("z", "F-T01", 2), 0, 0); inventory.Place(new ItemInstance("a", "F-S01", 1), 2, 0);
            var query = new ItemQuery { Text = "F-", Category = ItemCategory.Scroll, Usable = true, Sort = ItemSort.Name };
            Assert.That(ItemSearchService.Search(inventory, query).Select(i => i.InstanceId), Is.EqualTo(new[] { "a" }));
            Assert.That(inventory.Placements.Count, Is.EqualTo(2));
        }

        [Test]
        public void Serialization_RoundTripsRotationUsesAndCoordinates()
        {
            InventoryContainerState inventory = new InventoryContainerState(); ItemInstance artifact = new ItemInstance("i-01", "F-T01", 4); artifact.TryConsume();
            inventory.Place(artifact, 4, 3, true); InventoryContainerState restored = InventoryContainerState.FromDataString(inventory.ToDataString());
            Assert.That(restored.ToDataString(), Is.EqualTo(inventory.ToDataString())); Assert.That(restored.Get("i-01").RemainingUses, Is.EqualTo(1)); Assert.That(restored.PlacementOf("i-01").Value.Rotated, Is.True);
        }

        [Test]
        public void LootSearch_RevealsStableOrderAndCannotTakeHiddenItems()
        {
            LootSourceState loot = new LootSourceState("crate", new GridPosition(1, 0), new[] { new ItemInstance("b", "F-S01", 1), new ItemInstance("a", "medkit", 0) });
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(loot.Take("b", inventory).Error, Is.EqualTo(InventoryError.MissingInstance));
            Assert.That(loot.RevealNext().InstanceId, Is.EqualTo("a")); Assert.That(loot.State, Is.EqualTo(LootSearchState.Searching));
            Assert.That(loot.Take("a", inventory).Success, Is.True); Assert.That(inventory.Get("a"), Is.Not.Null);
        }

        [Test]
        public void LootSearch_EndsOnlyAfterEveryItemWasRevealedAndTaken()
        {
            LootSourceState loot = new LootSourceState("crate", new GridPosition(1, 0), new[] { new ItemInstance("a", "medkit", 0), new ItemInstance("b", "shield_cell", 1) });
            InventoryContainerState inventory = new InventoryContainerState(); loot.RevealNext(); loot.RevealNext();
            Assert.That(loot.State, Is.EqualTo(LootSearchState.Searched)); Assert.That(loot.TakeAllRevealed(inventory).All(result => result.Success), Is.True); Assert.That(loot.State, Is.EqualTo(LootSearchState.Emptied));
        }

        [Test]
        public void CombatState_RestrictsScrollAndArtifactQuickbarToFourInstances()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
            for (int i = 0; i < 5; i++) state.ItemInventory.AddFirstFit(new ItemInstance("scroll-" + i, "F-S01", i));
            for (int i = 0; i < 4; i++) Assert.That(state.EquipItemQuickbar("scroll-" + i, i).Success, Is.True);
            Assert.That(state.EquipItemQuickbar("scroll-4", 4).Error, Is.EqualTo(InventoryError.QuickbarFull));
        }

        [Test]
        public void Map9_RoundTripsInventoryQuickbarUsesAndRotation()
        {
            RogueliteMapRun run = new RogueliteMapRun(401); ItemInstance artifact = run.GrantItem("F-T01", 2); artifact.TryConsume();
            InventoryPlacement original = run.Inventory.PlacementOf(artifact.InstanceId).Value; run.Inventory.Move(artifact.InstanceId, 4, 4, true);
            string data = run.ToJson(); RogueliteMapRun restored = RogueliteMapRun.FromJson(data);
            Assert.That(data.StartsWith("map9|"), Is.True); Assert.That(restored.ToJson(), Is.EqualTo(data));
            Assert.That(restored.Inventory.Get(artifact.InstanceId).RemainingUses, Is.EqualTo(1)); Assert.That(restored.ItemQuickbar[2], Is.EqualTo(artifact.InstanceId));
        }

        [Test]
        public void Map6_MigratesToMap9WithDeterministicStarterInventory()
        {
            RogueliteMapRun source = new RogueliteMapRun(402); string[] current = source.ToJson().Split('|'); current[0] = "map6";
            string legacy = string.Join("|", current.Take(22)); RogueliteMapRun migrated = RogueliteMapRun.FromJson(legacy);
            Assert.That(migrated.ToJson().StartsWith("map9|"), Is.True); Assert.That(migrated.Inventory.Width, Is.EqualTo(6)); Assert.That(migrated.Inventory.Height, Is.EqualTo(10)); Assert.That(migrated.Inventory.Items.Count, Is.EqualTo(2));
        }

        [Test]
        public void CombatSearch_CostsOneApPerRevealAndTakingCostsNone()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
            state.SetLootSource(new LootSourceState("crate", new GridPosition(1, 0), new[] { new ItemInstance("loot-a", "F-S01", 0), new ItemInstance("loot-b", "F-T01", 1) }));
            CombatResolver.BeginTurn(state, "hero"); CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero"));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(2)); Assert.That(state.LootSource.RevealedItems.Count, Is.EqualTo(1));
            string found = state.LootSource.RevealedItems[0].InstanceId; CombatResolver.Resolve(state, CombatCommand.TakeLoot("hero", found));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(2)); Assert.That(state.ItemInventory.Get(found), Is.Not.Null);
        }

        [Test]
        public void CombatSearch_WithNoApDoesNotRevealHiddenItem()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
            state.SetLootSource(new LootSourceState("crate", new GridPosition(1, 0), new[] { new ItemInstance("loot-a", "F-S01", 0) }));
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.TurnInPlace("hero", Facing.East)); CombatResolver.Resolve(state, CombatCommand.TurnInPlace("hero", Facing.East)); CombatResolver.Resolve(state, CombatCommand.TurnInPlace("hero", Facing.East));
            Assert.Throws<System.InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero")));
            Assert.That(state.LootSource.HiddenCount, Is.EqualTo(1));
        }

        [Test]
        public void ItemRewards_EnterInventoryAsIndependentMap7Instances()
        {
            RogueliteMapRun chosen = null; RogueliteReward reward = null;
            for (int seed = 1; seed <= 100 && reward == null; seed++)
            {
                RogueliteMapRun candidate = new RogueliteMapRun(seed); candidate.SelectNode("rail_patrol"); candidate.CompleteCurrentCombat();
                RogueliteReward item = candidate.CurrentRewards.FirstOrDefault(value => value.Kind == RogueliteRewardKind.Item);
                if (item != null) { chosen = candidate; reward = item; }
            }
            Assert.That(reward, Is.Not.Null); int before = chosen.Inventory.Items.Count; chosen.ClaimReward(reward.Id);
            Assert.That(chosen.Inventory.Items.Count, Is.EqualTo(before + 1)); Assert.That(chosen.Inventory.Items.Last().DefinitionId, Is.EqualTo(reward.Item.Id));
            Assert.That(RogueliteMapRun.FromJson(chosen.ToJson()).Inventory.Items.Last().DefinitionId, Is.EqualTo(reward.Item.Id));
        }

        [Test]
        public void Map7_PersistsContainerSearchAndTakenState()
        {
            RogueliteMapRun run = new RogueliteMapRun(403); CombatState combat = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
            LootSourceState loot = new LootSourceState("node-crate", new GridPosition(1, 0), new[] { new ItemInstance("found", "medkit", 0), new ItemInstance("hidden", "F-S01", 1) }); combat.SetLootSource(loot);
            loot.RevealNext(); loot.Take("found", combat.ItemInventory); run.CaptureCombatInventory(combat);
            RogueliteMapRun restored = RogueliteMapRun.FromJson(run.ToJson()); LootSourceState same = new LootSourceState("node-crate", new GridPosition(1, 0), new[] { new ItemInstance("found", "medkit", 0), new ItemInstance("hidden", "F-S01", 1) }); restored.RestoreLootProgress(same);
            Assert.That(same.HiddenCount, Is.EqualTo(1)); Assert.That(same.RevealedItems, Is.Empty); Assert.That(same.State, Is.EqualTo(LootSearchState.Searching));
        }

        [Test]
        public void FirelineScroll_UsesFrozenFourCellEightDamageFiregroundContract()
        {
            FireSpellDefinition spell = ItemAbilityCatalog.For("F-S01");
            Assert.That(spell.ActionPointCost, Is.EqualTo(1)); Assert.That(spell.ManaCost, Is.Zero); Assert.That(spell.Shape, Is.EqualTo(FireSelectionShape.Line)); Assert.That(spell.ShapeLength, Is.EqualTo(4));
            Assert.That(spell.Rules.Any(rule => rule.Kind == FireRuleKind.Damage && rule.Amount == 8 && rule.Scope == FireRuleScope.Selection), Is.True);
            Assert.That(spell.Rules.Any(rule => rule.Kind == FireRuleKind.CreateFireground && rule.Duration == 4), Is.True);
        }

        [Test]
        public void CombatQuickbarSwap_CostsOneApAndConsumptionIsPerInstance()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
            state.ItemInventory.AddFirstFit(new ItemInstance("artifact", "F-T01", 2)); CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.EquipInventoryQuickbar("hero", "artifact", 3));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(2)); Assert.That(state.ItemQuickbar[3], Is.EqualTo("artifact"));
            Assert.That(state.ConsumeInventoryItem("artifact"), Is.True); Assert.That(state.ItemInventory.Get("artifact").RemainingUses, Is.EqualTo(1));
            Assert.That(state.ConsumeInventoryItem("artifact"), Is.True); Assert.That(state.ItemInventory.Get("artifact"), Is.Null); Assert.That(state.ItemQuickbar[3], Is.Null);
        }

        [Test]
        public void InventoryConsumable_UsesRealEffectPathAndRemovesDepletedInstance()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East), new UnitState("enemy", false, new GridPosition(1, 0), Facing.West) }); UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, "enemy"); CombatResolver.Resolve(state, CombatCommand.Attack("enemy", "hero")); CombatResolver.BeginTurn(state, "hero"); CombatResolver.Resolve(state, CombatCommand.UseInventoryItem("hero", "combat-medkit"));
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth)); Assert.That(hero.ActionPoints, Is.EqualTo(2)); Assert.That(state.ItemInventory.Get("combat-medkit"), Is.Null); Assert.That(state.ItemQuickbar[0], Is.Null);
        }
    }
}
