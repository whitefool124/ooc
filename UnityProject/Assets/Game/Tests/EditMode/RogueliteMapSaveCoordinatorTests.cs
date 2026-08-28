using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteMapSaveCoordinatorTests
    {
        [Test]
        public void NewRun_MustPersistBeforeItCanStart()
        {
            MemoryStore store = new MemoryStore { FailWrites = true };
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);

            RogueliteMapStartResult result = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Universal, 301);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Run, Is.Null);
            Assert.That(result.FailureMessage, Is.EqualTo(RogueliteMapSaveCoordinator.NewRunSaveFailure));
            Assert.That(coordinator.LastSaveSucceeded, Is.False);
        }

        [Test]
        public void ContinueMissing_DoesNotCreateOrOverwriteData()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);

            RogueliteMapStartResult result = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 302);

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("没有可继续"));
            Assert.That(store.Values, Is.Empty);
        }

        [Test]
        public void ValidNewRun_CanRoundTripThroughContinue()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Ranged, 303);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);

            Assert.That(created.Success, Is.True);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.Seed, Is.EqualTo(303));
            Assert.That(loaded.Run.StarterId, Is.EqualTo(FireRogueliteStarterCatalog.Ranged));
        }

        [Test]
        public void ReplacingAValidRun_ResetsAllRunScopedResourcesInsteadOfReusingTheActiveDto()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult first = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Ranged, 305);
            Assert.That(first.Success, Is.True);

            first.Run.SelectNode("supply_checkpoint");
            first.Run.ChooseCurrentNodeContent("buy_hazard_condenser");
            Assert.That(first.Run.Gold, Is.EqualTo(3));
            Assert.That(coordinator.Save(first.Run), Is.True);

            RogueliteMapStartResult replacement = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 306);

            Assert.That(replacement.Success, Is.True);
            Assert.That(replacement.Run.Seed, Is.EqualTo(306));
            Assert.That(replacement.Run.CurrentNodeId, Is.EqualTo("start"));
            Assert.That(replacement.Run.Gold, Is.EqualTo(8));
            Assert.That(replacement.Run.StageContribution, Is.Zero);
            Assert.That(replacement.Run.StageTime, Is.Zero);
            Assert.That(replacement.Run.AcademyProgress, Is.Zero);
            Assert.That(replacement.Run.CorePermits, Is.Zero);
        }

        [Test]
        public void CorruptSlot_RequiresExplicitReplacementPreparation()
        {
            MemoryStore store = new MemoryStore();
            store.Values[RogueliteSaveGateway.MapRunKey] = "broken";
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            Assert.That(coordinator.TryStart(true, FireRogueliteStarterCatalog.Universal, 304).Success, Is.False);

            Assert.That(coordinator.PrepareSlotForReplacement(), Is.True);
            Assert.That(store.Values.ContainsKey(RogueliteSaveGateway.MapRunKey), Is.False);
        }

        private static RogueliteMapSaveCoordinator Coordinator(MemoryStore store) =>
            new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
            public bool FailWrites { get; set; }
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") =>
                Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value)
            {
                if (FailWrites) throw new InvalidOperationException("write failed");
                Values[key] = value;
            }
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }
    }
}
