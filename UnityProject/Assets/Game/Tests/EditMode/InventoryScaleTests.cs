using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class InventoryScaleTests
    {
        [Test]
        public void FormalCatalog_UsesSixFootprintSignaturesAndMostlyMultiCellItems()
        {
            IReadOnlyList<ItemDefinition> formal = ItemCatalog.All.Where(item => item.Category == ItemCategory.Artifact ||
                item.Id == "medkit" || item.Id == "shield_cell" || item.Id == "F-S01" || item.Id == "aether_core").ToArray();

            Assert.That(formal.Select(item => item.Width + "x" + item.Height).Distinct().Count(), Is.GreaterThanOrEqualTo(6));
            Assert.That(formal.Count(item => item.Width * item.Height > 1), Is.GreaterThan(formal.Count(item => item.Width * item.Height == 1)));
            Assert.That((ItemCatalog.Medkit.Width, ItemCatalog.Medkit.Height), Is.EqualTo((2, 1)));
            Assert.That((ItemCatalog.ShieldCell.Width, ItemCatalog.ShieldCell.Height), Is.EqualTo((1, 2)));
            Assert.That((ItemCatalog.FirelineScroll.Width, ItemCatalog.FirelineScroll.Height), Is.EqualTo((2, 1)));
            Assert.That((ItemCatalog.AetherCore.Width, ItemCatalog.AetherCore.Height), Is.EqualTo((2, 2)));
        }

        [Test]
        public void FormalCatalog_InventoryArtMatchesFootprintAndPixelImporterContract()
        {
            foreach (ItemDefinition item in ItemCatalog.All.Where(candidate => candidate.Category == ItemCategory.Artifact ||
                         candidate.Id == "medkit" || candidate.Id == "shield_cell" || candidate.Id == "F-S01" || candidate.Id == "aether_core"))
            {
                Texture2D texture = Resources.Load<Texture2D>(item.InventoryArtPath);
                Assert.That(texture, Is.Not.Null, item.Id + " inventory art");
                Assert.That(texture.width, Is.EqualTo(item.Width * 32), item.Id + " width");
                Assert.That(texture.height, Is.EqualTo(item.Height * 32), item.Id + " height");
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                Assert.That(importer, Is.Not.Null, item.Id);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), item.Id);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), item.Id);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), item.Id);
                Assert.That(importer.mipmapEnabled, Is.False, item.Id);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f), item.Id);
            }
        }

        [Test]
        public void LegacyMap9Layout_IsValidatedWithFrozenSizesThenDeterministicallyRepacked()
        {
            string raw = string.Join(";",
                Row("old-a", "G-T01", 0, 3, 0, 0, false),
                Row("old-b", "G-T02", 1, 2, 0, 1, false));

            InventoryContainerState first = InventoryContainerState.FromLegacyMap9DataString(raw);
            InventoryContainerState second = InventoryContainerState.FromLegacyMap9DataString(raw);

            Assert.That(first.Items.Select(item => item.InstanceId), Is.EquivalentTo(new[] { "old-a", "old-b" }));
            Assert.That(first.Get("old-a").RemainingUses, Is.EqualTo(3));
            Assert.That(first.Get("old-b").RemainingUses, Is.EqualTo(2));
            Assert.That(first.ToDataString(), Is.EqualTo(second.ToDataString()));
            Assert.That(first.CanPlace(first.Get("old-a"), first.PlacementOf("old-a").Value.X, first.PlacementOf("old-a").Value.Y, "old-a", first.PlacementOf("old-a").Value.Rotated).Success, Is.True);
        }

        [Test]
        public void LegacyMap9Layout_RejectsDataThatWasAlreadyInvalidUnderFrozenSizes()
        {
            string raw = string.Join(";",
                Row("old-a", "G-T01", 0, 3, 0, 0, false),
                Row("old-b", "G-T02", 1, 2, 0, 0, false));

            Assert.Throws<InvalidOperationException>(() => InventoryContainerState.FromLegacyMap9DataString(raw));
        }

        [Test]
        public void Map9Migration_PreservesInstancesUsesAndQuickbarThenWritesMap10()
        {
            RogueliteMapRun current = new RogueliteMapRun(810);
            string[] fields = current.ToJson().Split('|');
            fields[0] = "map9";
            fields[22] = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";",
                Row("old-med", "medkit", 0, 1, 0, 0, false),
                Row("old-shield", "shield_cell", 1, 1, 0, 1, false))));
            fields[23] = "old-med,old-shield,,,,,,";
            fields[24] = "2";

            RogueliteMapRun migrated = RogueliteMapRun.FromJson(string.Join("|", fields));

            Assert.That(migrated.ToJson(), Does.StartWith("map10|"));
            Assert.That(migrated.Inventory.Get("old-med").RemainingUses, Is.EqualTo(1));
            Assert.That(migrated.Inventory.Get("old-shield").RemainingUses, Is.EqualTo(1));
            Assert.That(migrated.ItemQuickbar[0], Is.EqualTo("old-med"));
            Assert.That(migrated.ItemQuickbar[1], Is.EqualTo("old-shield"));
            Assert.That(RogueliteMapRun.FromJson(migrated.ToJson()).ToJson(), Is.EqualTo(migrated.ToJson()));
        }

        private static string Row(string instanceId, string definitionId, int acquired, int uses, int stability, int x, bool rotated) =>
            Row(instanceId, definitionId, acquired, uses, stability, x, 0, rotated);

        private static string Row(string instanceId, string definitionId, int acquired, int uses, int stability, int x, int y, bool rotated) =>
            string.Join(",", Encode(instanceId), Encode(definitionId), acquired, uses, stability, x, y, rotated ? "1" : "0");

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}
