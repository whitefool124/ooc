using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace OCC.Combat.Tests
{
    public sealed class ArtifactContentIntegrationTests
    {
        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal);
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [Test]
        public void Catalog_HasTwentyUniqueFormalArtifactsAndMatchingItems()
        {
            Assert.That(ArtifactCatalog.All.Count, Is.EqualTo(20));
            Assert.That(ArtifactCatalog.All.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            Assert.That(ArtifactCatalog.All.Count(item => item.Element == "通用"), Is.GreaterThanOrEqualTo(15));
            Assert.That(ArtifactCatalog.All.Single(item => item.Id == "F-T01").DisplayName, Is.EqualTo("炎脉封装筒"));
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                ItemDefinition item = ItemCatalog.Get(artifact.Id);
                Assert.That(item.Category, Is.EqualTo(ItemCategory.Artifact), artifact.Id);
                Assert.That(item.IconPath, Is.EqualTo(artifact.IconPath), artifact.Id);
                Assert.That(item.MaximumUses, Is.EqualTo(artifact.MaximumUses), artifact.Id);
                Assert.That(artifact.ContentSources, Is.Not.EqualTo(ArtifactContentSource.None), artifact.Id);
                Assert.That(artifact.PublicCost, Is.Not.Empty, artifact.Id);
                Assert.That(artifact.TargetSummary, Is.Not.Empty, artifact.Id);
                Assert.That(artifact.EffectSummary, Is.Not.Empty, artifact.Id);
                Assert.That(artifact.RiskSummary, Is.Not.Empty, artifact.Id);
                Assert.That(artifact.BuildUse, Is.Not.Empty, artifact.Id);
            }
        }

        [Test]
        public void PublicCatalog_HasNoOutOfEraTerms()
        {
            string[] forbidden = { "步枪", "狙击", "现代枪械", "爆破兵", "电池", "污染变异", "成熟工业军队", "倒计时" };
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                string publicText = string.Join("|", artifact.DisplayName, artifact.Provenance, artifact.PublicCost,
                    artifact.TargetSummary, artifact.EffectSummary, artifact.RiskSummary, artifact.BuildUse);
                foreach (string term in forbidden) Assert.That(publicText, Does.Not.Contain(term), artifact.Id + " / " + term);
            }
        }

        [Test]
        public void NormalEliteTreasureBossShopEventAndLoot_AreRealAndTogetherReachEveryArtifact()
        {
            var nodeSources = new Dictionary<RogueliteMapNodeType, ArtifactContentSource>
            {
                { RogueliteMapNodeType.Combat, ArtifactContentSource.NormalReward },
                { RogueliteMapNodeType.Elite, ArtifactContentSource.EliteReward },
                { RogueliteMapNodeType.Treasure, ArtifactContentSource.Treasure },
                { RogueliteMapNodeType.Finale, ArtifactContentSource.BossReward }
            };
            HashSet<string> reachable = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<RogueliteMapNodeType, ArtifactContentSource> route in nodeSources)
            {
                Assert.That(RogueliteMapCatalog.Nodes.Any(node => node.Type == route.Key), Is.True, route.Key.ToString());
                for (int seed = 0; seed < 1000; seed++)
                {
                    ArtifactDefinition rolled = ArtifactRewardPool.Roll(seed, seed % 19, route.Key);
                    Assert.That((rolled.ContentSources & route.Value) != 0, Is.True, route.Key + " / " + rolled.Id);
                    reachable.Add(rolled.Id);
                }
            }

            AddDirectChoiceArtifacts(RogueliteMapNodeType.Shop, ArtifactContentSource.Shop, reachable);
            AddDirectChoiceArtifacts(RogueliteMapNodeType.Event, ArtifactContentSource.Event, reachable);
            for (int seed = 0; seed < 1000; seed++)
            {
                ArtifactDefinition loot = ArtifactRewardPool.RollLoot(seed, "node-" + (seed % 37));
                Assert.That((loot.ContentSources & ArtifactContentSource.Loot) != 0, Is.True, loot.Id);
                reachable.Add(loot.Id);
            }

            Assert.That(reachable, Is.SupersetOf(ArtifactCatalog.All.Select(artifact => artifact.Id)));
        }

        [Test]
        public void NormalCombatReward_CanBeClaimedIntoInventoryWithoutAutoEquipping()
        {
            RogueliteMapRun run = new RogueliteMapRun(1906, FireRogueliteStarterCatalog.Universal);
            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();
            RogueliteReward reward = run.CurrentRewards.Single(candidate => candidate.Kind == RogueliteRewardKind.Item && candidate.Item.Category == ItemCategory.Artifact);
            int before = run.Inventory.Items.Count;

            run.ClaimReward(reward.Id);

            Assert.That(run.Inventory.Items.Count, Is.EqualTo(before + 1));
            Assert.That(run.Inventory.Items.Count(item => item.DefinitionId == reward.Id), Is.EqualTo(1));
            Assert.That(run.ItemQuickbar, Does.Not.Contain(run.Inventory.Items.Single(item => item.DefinitionId == reward.Id).InstanceId));
            Assert.That(run.AwaitingReward, Is.False);
        }

        [Test]
        public void ShopEventAndLoot_AwardRealIndependentInventoryInstances()
        {
            RogueliteMapRun shopRun = new RogueliteMapRun(1911);
            shopRun.SelectNode("supply_checkpoint");
            shopRun.ChooseCurrentNodeContent("buy_hazard_condenser");
            ItemInstance shopItem = shopRun.Inventory.Items.Single(item => item.DefinitionId == "G-T11");
            Assert.That((shopRun.Parts, shopRun.Aether), Is.EqualTo((1, 1)));

            RogueliteMapRun eventRun = new RogueliteMapRun(1912, FireRogueliteStarterCatalog.Universal);
            eventRun.SelectNode("rail_patrol");
            eventRun.CompleteCurrentCombat();
            eventRun.ClaimReward(eventRun.CurrentRewards.Single().Id);
            eventRun.SelectNode("switchyard");
            eventRun.ChooseCurrentNodeContent("recover_survey_lens");
            ItemInstance eventItem = eventRun.Inventory.Items.Single(item => item.DefinitionId == "G-T04");

            ArtifactDefinition lootDefinition = ArtifactRewardPool.RollLoot(1913, "reachable-crate");
            LootSourceState loot = new LootSourceState("reachable-crate", new GridPosition(1, 0),
                new[] { new ItemInstance("artifact-loot", lootDefinition.Id, 0) });
            InventoryContainerState lootInventory = new InventoryContainerState();
            Assert.That(loot.RevealNext().DefinitionId, Is.EqualTo(lootDefinition.Id));
            Assert.That(loot.Take("artifact-loot", lootInventory).Success, Is.True);
            ItemInstance lootItem = lootInventory.Get("artifact-loot");

            Assert.That(new[] { shopItem.InstanceId, eventItem.InstanceId, lootItem.InstanceId }.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            Assert.That(lootItem.DefinitionId, Is.EqualTo(lootDefinition.Id));
        }

        [Test]
        public void Map10_RoundTripsEveryArtifactInstanceUsesAndQuickbar()
        {
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                RogueliteMapRun run = new RogueliteMapRun(1907);
                ItemInstance item = run.GrantItem(artifact.Id);
                Assert.That(run.EquipInventoryItem(item.InstanceId, 3).Success, Is.True, artifact.Id);
                Assert.That(item.TryConsume(), Is.True, artifact.Id);
                string raw = run.ToJson();
                Assert.That(raw, Does.StartWith("map10|"));
                RogueliteMapRun restored = RogueliteMapRun.FromJson(raw);
                ItemInstance restoredItem = restored.Inventory.Items.Single(value => value.DefinitionId == artifact.Id);
                Assert.That(restoredItem.RemainingUses, Is.EqualTo(artifact.MaximumUses - 1), artifact.Id);
                Assert.That(restored.ItemQuickbar[3], Is.EqualTo(restoredItem.InstanceId), artifact.Id);
            }
        }

        [Test]
        public void Map8_MigratesArtifactInventoryUsesAndQuickbarToMap9()
        {
            RogueliteMapRun run = new RogueliteMapRun(1908);
            ItemInstance artifact = run.GrantItem("G-T09");
            Assert.That(run.EquipInventoryItem(artifact.InstanceId, 3).Success, Is.True);
            Assert.That(artifact.TryConsume(), Is.True);
            string[] fields = run.ToJson().Split('|').Take(31).ToArray();
            fields[0] = "map8";

            RogueliteMapRun restored = RogueliteMapRun.FromJson(string.Join("|", fields));

            Assert.That(restored.ToJson(), Does.StartWith("map10|"));
            Assert.That(restored.Inventory.Get(artifact.InstanceId).DefinitionId, Is.EqualTo("G-T09"));
            Assert.That(restored.Inventory.Get(artifact.InstanceId).RemainingUses, Is.EqualTo(ArtifactCatalog.Get("G-T09").MaximumUses - 1));
            Assert.That(restored.ItemQuickbar[3], Is.EqualTo(artifact.InstanceId));
        }

        [Test]
        public void CorruptMap9ArtifactInventory_IsBackedUpAndNeverOverwrittenImplicitly()
        {
            RogueliteMapRun run = new RogueliteMapRun(1909);
            run.GrantItem("G-T18");
            string[] fields = run.ToJson().Split('|');
            fields[22] = "not-base64";
            string corrupt = string.Join("|", fields);
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = corrupt;
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(gateway.LastLoadStatus, Is.EqualTo(RogueliteSaveLoadStatus.CorruptData));
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo(corrupt));
            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(1910)), Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(corrupt));
        }

        [Test]
        public void FormalArtRegistry_MapsEveryArtifactToItsIndependentFormalIconPath()
        {
            FormalArtEntry[] entries = FormalArtRegistry.Items.Where(entry => ArtifactCatalog.All.Any(artifact => artifact.Id == entry.RuntimeId)).ToArray();
            Assert.That(entries, Has.Length.EqualTo(20));
            Assert.That(entries.Select(entry => entry.ResourcePath).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
                Assert.That(FormalArtRegistry.ItemPath(artifact.Id), Is.EqualTo(artifact.IconPath), artifact.Id);
        }

        [Test]
        public void FormalInventoryRewardAndArchiveUi_ReferenceAllRequiredPublicArtifactFields()
        {
            string[] paths =
            {
                "Assets/Game/Runtime/Presentation/TarkovInventoryPanel.cs",
                "Assets/Game/Runtime/Presentation/RogueliteSettlementPresentation.cs",
                "Assets/Game/Runtime/Presentation/FormalRogueliteUi.cs"
            };
            string[] requiredFields = { "artifact.Provenance", "artifact.PublicCost", "artifact.MaximumUses", "artifact.TargetSummary", "artifact.EffectSummary", "artifact.RiskSummary" };
            foreach (string path in paths)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Assert.That(script, Is.Not.Null, path);
                foreach (string field in requiredFields) Assert.That(script.text, Does.Contain(field), path + " / " + field);
            }
            MonoScript inventory = AssetDatabase.LoadAssetAtPath<MonoScript>(paths[0]);
            Assert.That(inventory.text, Does.Not.Contain("\" // \" + definition.Id"), "Formal inventory must not expose stable development ids.");
            Assert.That(inventory.text, Does.Contain("CategoryName(definition.Category)"), "Non-artifact categories must also use player-facing labels.");
            Assert.That(inventory.text, Does.Contain("\"法宝 · \" + RarityName(definition.Rarity)"),
                "Artifact details must localize category and rarity instead of exposing enum names.");
            MonoScript archive = AssetDatabase.LoadAssetAtPath<MonoScript>(paths[2]);
            Assert.That(archive.text, Does.Contain("run.Inventory.Items.Where"), "Archive must enumerate artifacts actually owned by this run.");
            Assert.That(archive.text, Does.Contain("ItemCategory.Artifact"));
            Assert.That(archive.text, Does.Contain("下一件法宝"), "Archive must allow every owned artifact to be inspected, not just the first one.");
        }

        private static void AddDirectChoiceArtifacts(RogueliteMapNodeType nodeType, ArtifactContentSource source, ISet<string> reachable)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Nodes.First(candidate => candidate.Type == nodeType);
            RogueliteNodeContentChoice[] choices = RogueliteNodeContentCatalog.ChoicesFor(node)
                .Where(choice => choice.Effect == RogueliteNodeContentEffect.Reward && ArtifactCatalog.All.Any(artifact => artifact.Id == choice.RewardId)).ToArray();
            Assert.That(choices, Is.Not.Empty, nodeType.ToString());
            foreach (RogueliteNodeContentChoice choice in choices)
            {
                ArtifactDefinition artifact = ArtifactCatalog.Get(choice.RewardId);
                Assert.That((artifact.ContentSources & source) != 0, Is.True, nodeType + " / " + artifact.Id);
                reachable.Add(artifact.Id);
            }
        }
    }
}
