using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Tests
{
    public sealed class StageOneRewardCompletionTests
    {
        [OneTimeSetUp]
        public void MainProject() => Assert.That(Application.dataPath.Replace('\\', '/'), Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets").Or.EqualTo("D:/occart/OCC_latest/UnityProject/Assets"));

        [TestCase(BattlefieldChestTier.Common, 2, 8)]
        [TestCase(BattlefieldChestTier.Refined, 3, 12)]
        [TestCase(BattlefieldChestTier.Cache, 4, 16)]
        public void ContainerPools_AreSeededDistinctAndMeetTheirGuarantees(BattlefieldChestTier tier, int count, int hp)
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 0; seed < 48; seed++)
            {
                LootSourceState loot = AcademyLootCatalog.Create(seed, "test", new GridPosition(1, 0), tier);
                string[] ids = loot.Contents.Select(i => i.DefinitionId).ToArray();
                foreach (ItemInstance item in loot.Contents)
                {
                    Assert.That(TarkovInventoryPanel.ContainerItemDetail(item), Is.Not.Empty);
                    Assert.That(Resources.Load<Texture2D>(ItemCatalog.Get(item.DefinitionId).IconPath), Is.Not.Null, item.DefinitionId);
                }
                Assert.That(ids.Length, Is.EqualTo(count)); Assert.That(ids.Distinct().Count(), Is.EqualTo(count));
                Assert.That(loot.Durability, Is.EqualTo(hp));
                Assert.That(loot.ToProgressString(), Is.EqualTo(AcademyLootCatalog.Create(seed, "test", loot.Position, tier).ToProgressString()));
                Assert.That(loot.PublicSummary, Does.Contain(AcademyLootCatalog.Name(tier)));
                if (tier != BattlefieldChestTier.Common) Assert.That(ids.Any(AcademyLootCatalog.IsMaterial), Is.True);
                if (tier == BattlefieldChestTier.Cache)
                {
                    Assert.That(ids.Any(id => id.StartsWith("FORGE-")), Is.True);
                    Assert.That(ids.Any(id => id.StartsWith("SPEC-")), Is.True);
                    Assert.That(ids.Any(id => ItemCatalog.Get(id).Rarity >= ItemRarity.Uncommon && !AcademyLootCatalog.IsMaterial(id)), Is.True);
                }
                seen.Add(string.Join(",", ids));
            }
            Assert.That(seen.Count, Is.GreaterThan(4));
        }

        [Test]
        public void ExhaustedArtifactSources_DoNotBorrowFromOtherPools()
        {
            foreach (RogueliteMapNodeType type in new[] { RogueliteMapNodeType.Combat, RogueliteMapNodeType.Elite, RogueliteMapNodeType.Finale })
                Assert.That(ArtifactRewardPool.Roll(31, 4, type, ArtifactCatalog.All.Select(a => a.Id)), Is.Null);
            foreach (RogueliteMapNode node in RogueliteAcademyMapGenerator.Generate(31).Where(n => n.IsCombat))
            {
                var catalog = RogueContentCatalog.CreateAcademyV01();
                var owned = catalog.Spells.Select(s => s.DefinitionId).Concat(catalog.Equipment.Select(e => e.DefinitionId)).Concat(ArtifactCatalog.All.Select(a => a.Id));
                var package = AcademyBattleRewardCatalog.Roll(31, node.Id, owned);
                Assert.That(package.MainIds.Count, Is.EqualTo(3));
                Assert.That(package.MainIds[2], Is.EqualTo(AcademyBattleRewardCatalog.ForgePair).Or.EqualTo(AcademyBattleRewardCatalog.SpecPair).Or.EqualTo(AcademyBattleRewardCatalog.ScrollPack));
                if (node.Type == RogueliteMapNodeType.Finale)
                    Assert.That(catalog.Spells.Single(s => s.DefinitionId == package.MainIds[0]).Rarity, Is.EqualTo(SpellRarity.Rare));
            }
        }

        [Test]
        public void RewardPools_ExposeEveryEligibleSpellAndEquipmentAcrossSeeds()
        {
            var catalog = RogueContentCatalog.CreateAcademyV01();
            var service = new RogueAcademyContentService();
            int totalSpells = 0, totalEquipment = 0;
            foreach (string source in new[] { "combat", "elite", "boss" })
                foreach (SpellRarity rarity in new[] { SpellRarity.Common, SpellRarity.Uncommon, SpellRarity.Rare })
                {
                    string[] expected = catalog.Spells.Where(s => s.RewardEligible && s.RewardSources.Contains(source) && s.Rarity == rarity)
                        .Select(s => s.DefinitionId).ToArray();
                    var seen = new System.Collections.Generic.HashSet<string>();
                    for (int seed = 0; seed < 512; seed++) foreach (var item in service.Roll(seed * 7919, source, rarity, EquipmentRarity.Common, 1, 0)) seen.Add(item.DefinitionId);
                    Assert.That(seen, Is.EquivalentTo(expected), source + "/" + rarity);
                    totalSpells += seen.Count;
                }
            foreach (string source in new[] { "combat", "boss" })
                foreach (EquipmentRarity rarity in new[] { EquipmentRarity.Common, EquipmentRarity.Uncommon, EquipmentRarity.Rare })
                {
                    string[] expected = catalog.Equipment.Where(e => e.SourceTypes.Contains(source) && e.AllowedRarities.Contains(rarity) &&
                        EquipmentSlotRules.IsActive(e.Slot) && e.Slot != OCC.Combat.Roguelite.EquipmentSlot.Ring1 &&
                        e.Slot != OCC.Combat.Roguelite.EquipmentSlot.Ring2 && e.Slot != OCC.Combat.Roguelite.EquipmentSlot.Necklace).Select(e => e.DefinitionId).ToArray();
                    var seen = new System.Collections.Generic.HashSet<string>();
                    for (int seed = 0; seed < 512; seed++) foreach (var item in service.Roll(seed * 7919, source, SpellRarity.Common, rarity, 0, 1)) seen.Add(item.DefinitionId);
                    Assert.That(seen, Is.EquivalentTo(expected), source + "/" + rarity);
                    totalEquipment += seen.Count;
                }
            TestContext.WriteLine("Reachable spell source/rarity entries: " + totalSpells + "; equipment: " + totalEquipment);
        }

        [Test]
        public void DestroyedContainer_LosesOnlyUntakenContents_CloneAndRestoreAgree()
        {
            CombatState state = Battle();
            LootSourceState loot = AcademyLootCatalog.Create(42, "test", new GridPosition(1, 0), BattlefieldChestTier.Common);
            state.SetLootSource(loot);
            Assert.That(state.Map.IsBlocked(loot.Position), Is.True);
            Assert.That(state.Map.GetTile(loot.Position).BlocksLineOfSight, Is.False);
            loot.RevealNext();
            string taken = loot.RevealedItems[0].InstanceId; Assert.That(loot.ClaimRevealed(taken), Is.Not.Null);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageObject(loot.Position, 8));
            Assert.That(loot.IsDestroyed, Is.True); Assert.That(loot.RevealedItems, Is.Empty);
            Assert.That(loot.TakenInstanceIds, Does.Contain(taken)); Assert.That(loot.ClaimRevealed(taken), Is.Null);
            Assert.That(state.Map.IsBlocked(loot.Position), Is.False);
            Assert.That(state.Clone().LootSource.IsDestroyed, Is.True);
            LootSourceState restored = AcademyLootCatalog.Create(99, "other", loot.Position, BattlefieldChestTier.Cache);
            restored.RestoreProgress(loot.ToProgressString());
            Assert.That(restored.ToProgressString(), Is.EqualTo(loot.ToProgressString()));
        }

        [Test]
        public void SearchOnce_RevealsTheWholeRandomContainer_ForOneActionPoint()
        {
            CombatState state = Battle();
            state.SetLootSource(AcademyLootCatalog.Create(42, "test", new GridPosition(1, 0), BattlefieldChestTier.Cache));
            CombatResolver.BeginTurn(state, "hero"); int ap = state.GetUnit("hero").ActionPoints;
            CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero"));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(ap - 1));
            Assert.That(state.LootSource.RevealedItems.Count, Is.EqualTo(4));
            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero")));
        }

        [Test]
        public void ClaimedResources_SettleOnceAndSurviveRunSerialization()
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(42, includeOpeningGift: false);
            run.ConfirmAcademyDeparture();
            run.RogueRunState.CurrentNodeId = run.MapNodes.First(n => n.IsCombat).Id;
            run = RogueliteMapRun.FromRogue11(run.RogueRunState);
            CombatState state = Battle(); state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.FromDto(run.RogueRunState));
            string spell = RogueContentCatalog.CreateAcademyV01().Spells.First(s => s.RewardEligible && s.Rarity == SpellRarity.Uncommon).DefinitionId;
            LootSourceState loot = new LootSourceState("receipt-test", new GridPosition(1, 0), new[]
            {
                new ItemInstance("coin", AcademyLootCatalog.GoldBag, 0),
                new ItemInstance("material", AcademyBattleRewardCatalog.ForgeCircuit, 1),
                new ItemInstance("spell", AcademyLootCatalog.ManuscriptPrefix + spell, 2),
                new ItemInstance("tool", AcademyBattleRewardCatalog.RecoveryTool, 3)
            }, BattlefieldChestTier.Cache);
            state.SetLootSource(loot); CombatResolver.BeginTurn(state, "hero");
            CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero"));
            int ap = state.GetUnit("hero").ActionPoints, gold = run.Gold, material = run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeCircuit);
            foreach (ItemInstance item in loot.RevealedItems.ToArray()) CombatResolver.Resolve(state, CombatCommand.TakeLoot("hero", item.InstanceId));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(ap));
            run.CaptureCombatInventory(state); run.CaptureCombatInventory(state);
            Assert.That(run.Gold, Is.EqualTo(gold + 2));
            Assert.That(run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeCircuit), Is.EqualTo(material + 1));
            Assert.That(run.RogueRunState.MasteredSpellIds, Does.Contain(spell));
            var dto = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState));
            Assert.That(dto.Gold, Is.EqualTo(gold + 2));
            var equipment = RogueEquipmentRuntime.FromDto(dto);
            Assert.That(equipment.AllTacticalItems.Single(i => i.InstanceId == "tool").ChargesCurrent,
                Is.EqualTo(ArtifactCatalog.Get(AcademyBattleRewardCatalog.RecoveryTool).MaximumUses));
            Assert.That(equipment.Backpack.Keys.Any(id => equipment.MaterialIdFor(id) == AcademyBattleRewardCatalog.ForgeCircuit), Is.True);
        }

        [Test]
        public void FullBackpack_DoesNotConsumeTheOfferedItem()
        {
            CombatState state = Battle(); state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.CreateStarter(42));
            for (int i = 0; i < 200 && state.RogueEquipment.TryAddContainerItem("fill:" + i, AcademyBattleRewardCatalog.ForgeLoad); i++) { }
            var loot = new LootSourceState("full", new GridPosition(1, 0), new[] { new ItemInstance("kept", "G-T06", 0) }, BattlefieldChestTier.Common);
            state.SetLootSource(loot); CombatResolver.BeginTurn(state, "hero"); CombatResolver.Resolve(state, CombatCommand.SearchLoot("hero"));
            Assert.Throws<InvalidOperationException>(() => CombatResolver.Resolve(state, CombatCommand.TakeLoot("hero", "kept")));
            Assert.That(loot.TakenInstanceIds, Is.Empty); Assert.That(loot.RevealedItems.Count, Is.EqualTo(1));
        }

        [Test]
        public void TerrainReplacementAndCrystalBlast_KeepContainerDurabilityLive()
        {
            CombatState state = Battle();
            var loot = AcademyLootCatalog.Create(42, "blast", new GridPosition(1, 0), BattlefieldChestTier.Common);
            state.SetLootSource(loot);
            var tile = state.Map.GetTile(loot.Position).Clone(); tile.IsWater = true; state.Map.SetTile(loot.Position, tile);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageObject(loot.Position, 2));
            Assert.That(loot.Durability, Is.EqualTo(6));
            state.Map.SetTile(new GridPosition(2, 0), new TileState { IsAetherCrystal = true, IsDevice = true, Durability = 6 });
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageObject(new GridPosition(2, 0), 6));
            Assert.That(loot.IsDestroyed, Is.True); Assert.That(loot.Durability, Is.Zero);
        }

        [Test]
        public void SpellAndArtifactPreviews_ShowContainerLossWithoutChangingTheLiveState()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0)); hero.ConfigureMana(99);
            var enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            var state = new CombatState(new GridMap(6, 3), new[] { hero, enemy }); state.ConfigureRuleset(CombatRuleset.Roguelite);
            var loot = AcademyLootCatalog.Create(42, "preview", new GridPosition(1, 0), BattlefieldChestTier.Common);
            state.SetLootSource(loot); CombatResolver.BeginTurn(state, "hero");
            var fire = new FireBattleState(state); var spell = FireSpellCatalog.Get("F-P-R06");
            var preview = FireSpellEngine.Preview(fire, "hero", spell, FireSpellTarget.Unit("enemy", CardinalDirection.East));
            Assert.That(preview.CanCommit, Is.True, string.Join(";", preview.Failures));
            Assert.That(preview.ProjectedContainerDurability[loot.Position], Is.Zero);
            var artifacts = new ArtifactBattleState(state);
            var artifactPreview = ArtifactEngine.Preview(artifacts, "hero", ArtifactCatalog.BreachWedge, ArtifactTarget.At(loot.Position));
            Assert.That(artifactPreview.CanCommit, Is.True); Assert.That(artifactPreview.ContainerWarning, Does.Contain("内容将销毁"));
            Assert.That(loot.Durability, Is.EqualTo(8));
            var assets = new CombatFormalVisualAssets(); assets.LoadRuntime();
            var selection = new CombatSelectionController(); selection.SelectAction("技能1"); selection.SetPreviewPosition(enemy.Position);
            var cell = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets).Build(state, null, fire,
                selection, false, null, loot.Position, _ => spell, (s, p) => preview, _ => null, _ => null);
            Assert.That(cell.ContainerCategoryTextures.Count, Is.EqualTo(2)); Assert.That(cell.ContainerCategoryTextures.All(t => t != null), Is.True);
            Assert.That(cell.ObjectLabel, Does.Contain("补给箱")); Assert.That(cell.HoverText, Does.Contain("内容将销毁"));
            FireSpellEngine.Execute(fire, "hero", spell, FireSpellTarget.Unit("enemy", CardinalDirection.East));
            Assert.That(loot.IsDestroyed, Is.True);
        }

        [Test]
        public void MaterialChoice_GrantsTwoIdenticalMaterialsThroughTheCollectionFlow()
        {
            bool claimed = false;
            for (int seed = 0; seed < 32 && !claimed; seed++)
            {
                var run = RogueliteMapRun.CreateSubsequentAcademyRun(seed + 800, includeOpeningGift: false); run.ConfirmAcademyDeparture();
                var node = run.AvailableNodes.FirstOrDefault(n => n.IsCombat);
                if (node == null) continue;
                run.SelectNode(node.Id); RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
                var reward = run.CurrentRewards.FirstOrDefault(r => r.Id == AcademyBattleRewardCatalog.ForgePair || r.Id == AcademyBattleRewardCatalog.SpecPair);
                if (reward == null) continue;
                string id = reward.Id == AcademyBattleRewardCatalog.ForgePair ? AcademyBattleRewardCatalog.ForgeLoad : AcademyBattleRewardCatalog.SpecAmplify;
                int before = run.AcademyMaterialCount(id);
                run.OpenPendingRewardChoices(); run.SelectPendingReward(reward.Id); run.ClaimLootChoice(reward.Id);
                Assert.That(run.AcademyMaterialCount(id), Is.EqualTo(before + 2));
                Assert.That(run.AcademyMaterialCount(id == AcademyBattleRewardCatalog.ForgeLoad ? AcademyBattleRewardCatalog.ForgeCircuit : AcademyBattleRewardCatalog.SpecEfficient), Is.Zero);
                claimed = true;
            }
            Assert.That(claimed, Is.True);
        }

        [Test]
        public void PreviousSaveShape_LoadsWithoutInventingContainerSnapshots()
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(303, includeOpeningGift: false);
            string data = Rogue11Serializer.Serialize(run.RogueRunState);
            var old = Rogue11Serializer.Deserialize(data.Substring(0, data.LastIndexOf('|')));
            Assert.That(old.BattlefieldContainerRows, Is.Empty);
            run.RogueRunState.BattlefieldContainerRows.Add("invalid:initial=broken");
            Assert.Throws<InvalidOperationException>(() => Rogue11Serializer.Serialize(run.RogueRunState));
        }

        [Test]
        public void SearchAndTakeCommands_ReplayThroughTheProductionSaveGateway()
        {
            for (int seed = 300; seed < 340; seed++)
            {
                var run = RogueliteMapRun.CreateSubsequentAcademyRun(seed, includeOpeningGift: false); run.ConfirmAcademyDeparture();
                var gateway = new RogueliteSaveGateway(new MemoryStore());
                Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError); Assert.That(gateway.TryLoadMapRun(out run), Is.True, gateway.LastError);
                var node = run.AvailableNodes.FirstOrDefault(n => n.IsCombat); if (node == null) continue;
                run.SelectNode(node.Id); run.BeginCombatJournal();
                var builder = new CombatSceneSessionBuilder(); var state = builder.Build(run, null, Array.Empty<CombatSceneMarker>()).State;
                if (state.LootSource == null || state.LootSource.Tier == BattlefieldChestTier.Fixed) continue;
                CombatResolver.AdvanceToNextTurn(state);
                var plans = new EnemyTurnPlanBook(); var executor = new CombatCommandExecutionService();
                for (int step = 0; step < 32 && state.ActiveUnitId != "hero"; step++)
                {
                    var enemy = state.GetUnit(state.ActiveUnitId);
                    var command = plans.GetExecutionCommand(state, enemy, state.GetUnit("hero"));
                    Assert.That(executor.Execute(state, state.RogueSpells.FireBattle, command, true).Accepted, Is.True);
                    run.AppendCombatJournal(CombatJournalEntry.Accepted(command));
                    if (state.ActiveUnitId == enemy.Id)
                    {
                        CombatResolver.EndTurn(state, enemy); run.AppendCombatJournal(CombatJournalEntry.EnemyTurnEnded(enemy.Id));
                    }
                }
                Assert.That(state.ActiveUnitId, Is.EqualTo("hero"));
                var search = CombatCommand.SearchLoot("hero");
                Assert.That(executor.Execute(state, state.RogueSpells.FireBattle, search, true).Accepted, Is.True);
                run.AppendCombatJournal(CombatJournalEntry.Accepted(search));
                var take = CombatCommand.TakeLoot("hero", state.LootSource.RevealedItems[0].InstanceId);
                Assert.That(executor.Execute(state, state.RogueSpells.FireBattle, take, true).Accepted, Is.True);
                run.AppendCombatJournal(CombatJournalEntry.Accepted(take));
                Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError); Assert.That(gateway.TryLoadMapRun(out var restoredRun), Is.True, gateway.LastError);
                var restored = builder.Build(restoredRun, null, Array.Empty<CombatSceneMarker>()).State;
                CombatResolver.AdvanceToNextTurn(restored);
                CombatJournalReplayer.ReplayAfterActivation(restored, restoredRun.CombatJournalRows);
                Assert.That(restored.LootSource.ToProgressString(), Is.EqualTo(state.LootSource.ToProgressString()));
                Assert.That(restored.GetUnit("hero").ActionPoints, Is.EqualTo(state.GetUnit("hero").ActionPoints));
                Assert.That(restored.RogueEquipment.Backpack.Count, Is.EqualTo(state.RogueEquipment.Backpack.Count));
                return;
            }
            Assert.Fail("No normal entry with an optional container was exercised.");
        }

        [Test]
        public void ProductionBuilder_SeedsContainersBeforeCommands_AndRestoresOpeningContents()
        {
            int containers = 0;
            var tiers = new System.Collections.Generic.HashSet<BattlefieldChestTier>();
            for (int seed = 0; seed < 8; seed++)
            {
                var initial = RogueliteMapRun.CreateSubsequentAcademyRun(seed + 300, includeOpeningGift: false); initial.ConfirmAcademyDeparture();
                var gateway = new RogueliteSaveGateway(new MemoryStore());
                Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(initial), Is.True, gateway.LastError);
                Assert.That(gateway.TryLoadMapRun(out initial), Is.True, gateway.LastError);
                foreach (var node in initial.MapNodes.Where(n => n.IsCombat))
                {
                    var dto = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(initial.RogueRunState));
                    dto.CurrentNodeId = node.Id;
                    var run = RogueliteMapRun.FromRogue11(dto); run.BeginCombatJournal();
                    var saved = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState));
                    var state = new CombatSceneSessionBuilder().Build(run, null, Array.Empty<CombatSceneMarker>()).State;
                    if (state.LootSource?.Tier == BattlefieldChestTier.Fixed || state.LootSource == null) continue;
                    containers++;
                    tiers.Add(state.LootSource.Tier);
                    Assert.That(state.LootSource.Position.ManhattanDistance(state.GetUnit("hero").Position), Is.EqualTo(1));
                    Assert.That(state.Map.PositionsWith(t => t.IsLootChest).Count(), Is.EqualTo(1));
                    Assert.That(saved.BattlefieldContainerRows.Any(row => row.StartsWith(state.LootSource.Id + ":initial=", StringComparison.Ordinal)), Is.True);
                    var restored = new CombatSceneSessionBuilder().Build(RogueliteMapRun.FromRogue11(saved), null, Array.Empty<CombatSceneMarker>()).State;
                    Assert.That(restored.LootSource.ToProgressString(), Is.EqualTo(state.LootSource.ToProgressString()));
                }
            }
            Assert.That(containers, Is.GreaterThan(0), "No optional container is reachable through production encounters.");
            Assert.That(tiers.Count, Is.EqualTo(3), "All three tiers must be reachable in real first-stage encounters.");
            TestContext.WriteLine("Production optional containers built: " + containers);
        }

        private static CombatState Battle() => new CombatState(new GridMap(10, 6), new[]
        { new UnitState("hero", true, new GridPosition(0, 0)), new UnitState("enemy", false, new GridPosition(8, 4)) });

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            private readonly System.Collections.Generic.Dictionary<string, string> values = new System.Collections.Generic.Dictionary<string, string>();
            public bool HasKey(string key) => values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => values[key] = value;
            public void DeleteKey(string key) => values.Remove(key);
            public void Flush() { }
        }
    }
}
