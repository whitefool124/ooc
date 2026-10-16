using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class PeripheralUiCommitTests
    {
        private GameObject root;
        private CombatPrototypeBootstrap host;
        private MemoryStore store;
        private RogueliteSaveGateway gateway;
        private string equipment;

        [SetUp] public void PrepareIsolatedWorkshop()
        {
            Assert.That(Application.isPlaying, Is.False);
            root = new GameObject("Isolated service transaction test");
            host = root.AddComponent<CombatPrototypeBootstrap>();
            store = new MemoryStore();
            gateway = new RogueliteSaveGateway(store);
            typeof(CombatPrototypeBootstrap).GetField("mapSaves", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(host, new RogueliteMapSaveCoordinator(gateway));
            var run = RogueliteMapRun.CreateFirstRunV1(7021);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1"); run.CompleteCurrentCombat(); run.ClaimReward("F-P-R19");
            run.SelectNode("EV1"); run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.PreviewUiCaptureNode("W");
            equipment = RogueEquipmentRuntime.FromDto(run.RogueRunState).AllInstances
                .First(item => item.DefinitionId == "ACA-EQ-CH01").InstanceId;
            Assert.That(gateway.SaveNewMapRun(run), Is.True);
            typeof(CombatPrototypeBootstrap).GetProperty("mapRun", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, run);
        }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(root);

        [Test]
        public void ForgeReceipt_IsPublishedAfterStoredResult_AndCannotDuplicate()
        {
            int receipts = 0;
            host.UiVisualEvents.Published += e =>
            {
                if (e.Kind != UiVisualEventKind.ServiceCommitted) return;
                receipts++;
                Assert.That(e.Subject, Is.EqualTo("forge"));
                Assert.That(gateway.TryLoadMapRun(out var saved), Is.True);
                Assert.That(saved.CurrentWorkshopService.ForgeCompleted, Is.True);
                Assert.That(saved.ForgeMaterialCount, Is.Zero);
            };
            host.CompleteFirstRunForge(equipment, AcademyBattleRewardCatalog.ForgeLoad);
            host.CompleteFirstRunForge(equipment, AcademyBattleRewardCatalog.ForgeLoad);
            Assert.That(receipts, Is.EqualTo(1));
        }

        [Test]
        public void FailedSave_ProducesNoSuccessOrResourceEffects_AndRestoresMaterial()
        {
            var events = new List<UiVisualEvent>();
            host.UiVisualEvents.Published += events.Add;
            store.FailWrites = true;
            host.CompleteFirstRunForge(equipment, AcademyBattleRewardCatalog.ForgeLoad);
            Assert.That(events.Any(e => e.Kind == UiVisualEventKind.ServiceCommitted || e.Kind == UiVisualEventKind.ResourceChanged), Is.False);
            Assert.That(host.CurrentMapRun.CurrentWorkshopService.ForgeCompleted, Is.False);
            Assert.That(host.CurrentMapRun.ForgeMaterialCount, Is.EqualTo(1));
        }

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool FailWrites;
            public bool HasKey(string key) => values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value)
            {
                if (FailWrites) throw new InvalidOperationException("Simulated storage failure");
                values[key] = value;
            }
            public void DeleteKey(string key) => values.Remove(key);
            public void Flush() { }
        }
    }
}
