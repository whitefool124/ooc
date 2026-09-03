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
            Assert.That(result.FailureMessage, Is.EqualTo("没有可以继续的存档。请开始新游戏。"));
            Assert.That(store.Values, Is.Empty);
        }

        [TestCase(FireRogueliteStarterCatalog.Melee, "war_hammer")]
        [TestCase(FireRogueliteStarterCatalog.Universal, null)]
        [TestCase(FireRogueliteStarterCatalog.Ranged, "arcane_wand")]
        public void ValidNewRun_CanRoundTripThroughContinue(string starterId, string expectedWeaponId)
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                starterId, 303);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);

            Assert.That(created.Success, Is.True);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.Seed, Is.EqualTo(303));
            Assert.That(loaded.Run.StarterId, Is.EqualTo(starterId));
            Assert.That(loaded.Run.EquippedWeaponId, Is.EqualTo(expectedWeaponId));
        }

        [Test]
        public void MeleeVictorySettlement_SavesAndContinuesAtCompletedCombat()
        {
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator coordinator = Coordinator(store);
            RogueliteMapStartResult created = coordinator.TryStart(false,
                FireRogueliteStarterCatalog.Melee, 307);
            Assert.That(created.Success, Is.True);
            created.Run.SelectNode("rail_patrol");

            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState combat = new CombatState(new GridMap(3, 2), new[] { hero, enemy },
                new CombatObjective[] { new EliminationObjective() });
            combat.ResolveDebugOutcome(true);

            Assert.That(RogueliteCombatSettlement.TrySettleVictory(created.Run, combat), Is.True);
            Assert.That(coordinator.Save(created.Run), Is.True);

            RogueliteMapStartResult loaded = coordinator.TryStart(true,
                FireRogueliteStarterCatalog.Universal, 999);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.CurrentNodeId, Is.EqualTo("rail_patrol"));
            Assert.That(loaded.Run.CompletedNodes, Does.Contain("rail_patrol"));
            Assert.That(loaded.Run.AwaitingReward, Is.True);
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
