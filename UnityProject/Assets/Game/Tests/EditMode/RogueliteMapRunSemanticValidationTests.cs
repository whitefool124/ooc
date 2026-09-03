using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteMapRunSemanticValidationTests
    {
        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [TestCase("nodes")]
        [TestCase("resources")]
        [TestCase("combat")]
        [TestCase("build")]
        [TestCase("rewards")]
        [TestCase("choice")]
        [TestCase("inventory")]
        [TestCase("quickbar")]
        public void TamperedSemanticCategory_IsProtectedWithoutChangingMainOrFirstBackup(string category)
        {
            string raw = Tamper(new RogueliteMapRun(701).ToJson(), category);
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = raw;
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);

            Assert.That(gateway.TryLoadMapRun(out _), Is.False, category);
            Assert.That(gateway.LastLoadStatus, Is.EqualTo(RogueliteSaveLoadStatus.InvalidSemantics), category);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(raw));
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo(raw));
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.WriteLockKey(RogueliteSaveGateway.MapRunKey)), Is.True);

            store.Values[RogueliteSaveGateway.MapRunKey] = Tamper(new RogueliteMapRun(702).ToJson(), category);
            gateway.TryLoadMapRun(out _);
            Assert.That(store.Values[RogueliteSaveGateway.CorruptBackupKey(RogueliteSaveGateway.MapRunKey)], Is.EqualTo(raw));
        }

        [Test]
        public void ValidMap9_RoundTripIsDeterministicAndSemanticallyValid()
        {
            RogueliteMapRun run = new RogueliteMapRun(703, FireRogueliteStarterCatalog.Universal);
            string first = run.ToJson();
            RogueliteMapRun restored = RogueliteMapRun.FromJson(first);

            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True);
            Assert.That(restored.ToJson(), Is.EqualTo(first));
        }

        [Test]
        public void LegacyMap8_MigratesToValidMap9()
        {
            string[] fields = new RogueliteMapRun(704).ToJson().Split('|').Take(31).ToArray();
            fields[0] = "map8";

            RogueliteMapRun migrated = RogueliteMapRun.FromJson(string.Join("|", fields));

            Assert.That(migrated.ToJson(), Does.StartWith("map10|"));
            Assert.That(RogueliteMapRunValidator.Validate(migrated).IsValid, Is.True);
        }

        [Test]
        public void AcademyMap_HasFortyNodesAndThePublishedNodeMix()
        {
            Assert.That(RogueliteMapCatalog.Nodes.Count, Is.EqualTo(40));
            Assert.That(RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Combat), Is.EqualTo(18));
            Assert.That(RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Elite), Is.EqualTo(6));
            Assert.That(RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Event), Is.EqualTo(8));
            Assert.That(RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Workshop || node.Type == RogueliteMapNodeType.Shop || node.Type == RogueliteMapNodeType.Rest), Is.EqualTo(4));
            Assert.That(RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Treasure), Is.EqualTo(2));
        }

        [Test]
        public void AcademyMap_ConnectionsAreBidirectionalAndExplorationIsVisible()
        {
            RogueliteMapRun run = new RogueliteMapRun(705);

            Assert.That(run.AvailableNodes.Select(node => node.Id), Does.Contain("tutorial_hall"));
            run.SelectNode("tutorial_hall");

            Assert.That(run.AcademyProgress, Is.EqualTo(1));
            Assert.That(run.AcademyPhase, Is.EqualTo(AcademyMapPhase.NormalTerm));
            Assert.That(run.IsTransitionPending, Is.False);
        }

        private static string Tamper(string source, string category)
        {
            string[] fields = source.Split('|');
            switch (category)
            {
                case "nodes": fields[3] = "unknown_node"; break;
                case "resources": fields[9] = "-1"; break;
                case "combat": fields[33] = "99"; break;
                case "build": fields[31] = "unknown_starter"; break;
                case "rewards": fields[18] = "unknown_reward"; break;
                case "choice": fields[14] = "survey"; fields[15] = string.Empty; break;
                case "inventory": fields[22] = TamperInventoryUses(fields[22]); break;
                case "quickbar":
                    string[] slots = fields[23].Split(','); slots[1] = slots[0]; fields[23] = string.Join(",", slots); break;
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
            return string.Join("|", fields);
        }

        private static string TamperInventoryUses(string encoded)
        {
            string raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            string[] rows = raw.Split(';');
            string[] fields = rows[0].Split(',');
            fields[3] = "999";
            rows[0] = string.Join(",", fields);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", rows)));
        }
    }
}
