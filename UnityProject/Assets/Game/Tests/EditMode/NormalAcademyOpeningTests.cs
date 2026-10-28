using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class NormalAcademyOpeningTests
    {
        [OneTimeSetUp]
        public void VerifyMainProject()
        {
            Assert.That(Application.dataPath.Replace('\\', '/'), Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                TestContext.WriteLine("Scene preflight: " + scene.path + " dirty=" + scene.isDirty);
            }
        }
        [TestCase(303)]
        [TestCase(511)]
        [TestCase(912)]
        public void DefaultNewSlot_PersistsNormalAcademyWithUnmodifiedStarter(int seed)
        {
            var store = new MemoryStore();
            var saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));
            var created = saves.TryStart(false, FireRogueliteStarterCatalog.Universal, seed);
            Assert.That(created.Success, Is.True, created.FailureMessage);
            var run = created.Run;
            Assert.That(run.IsInAcademyLayer, Is.True);
            Assert.That(run.IsTutorialPhase, Is.False);
            Assert.That(run.FirstRunExperience, Is.Null);
            Assert.That(run.DeparturePending, Is.True);
            Assert.That(run.MapNodes.Count, Is.EqualTo(20));
            Assert.That(run.CompletedNodes, Is.Empty);
            Assert.That(run.RogueRunState.Gold, Is.EqualTo(8));
            Assert.That(run.RogueRunState.StageContribution, Is.Zero);
            Assert.That(run.RogueRunState.StageTime, Is.Zero);
            Assert.That(run.CurrentHealth, Is.EqualTo(UnitState.HeroBaseHealth));
            Assert.That(run.CurrentMana, Is.EqualTo(12));
            Assert.That(run.RogueRunState.MasteredSpellIds.Count, Is.EqualTo(4));
            Assert.That(run.RogueRunState.EquipmentInstances.Count, Is.EqualTo(2));
            Assert.That(run.RogueRunState.TacticalItemInstances, Is.Empty);
            // A create-only switch cannot reinterpret an already saved normal run.
            var loaded = saves.TryStart(true, FireRogueliteStarterCatalog.Melee, 1,
                enableFixedFirstExperience: true);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.Seed, Is.EqualTo(seed));
            Assert.That(loaded.Run.FirstRunExperience, Is.Null);
            Assert.That(loaded.Run.MapNodes.Select(n => n.Id), Is.EqualTo(run.MapNodes.Select(n => n.Id)));
        }

        [Test]
        public void ExplicitFixedExperience_ContinuesAsFixedWhenDefaultIsOff()
        {
            var saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(new MemoryStore()));
            var created = saves.TryStart(false, FireRogueliteStarterCatalog.Universal, 7021,
                acknowledgeOriginOnCreate: true, enableFixedFirstExperience: true);
            Assert.That(created.Success, Is.True);
            Assert.That(created.Run.IsTutorialPhase, Is.True);
            Assert.That(created.Run.FirstRunExperience.Origin.Acknowledged, Is.True);
            var loaded = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0);
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.Run.IsTutorialPhase, Is.True);
            Assert.That(loaded.Run.FirstRunExperience.Origin.Acknowledged, Is.True);
        }

        [Test]
        public void NormalReplacementFailure_PreservesThePreviousFixedArchive()
        {
            var store = new MemoryStore();
            var saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));
            Assert.That(saves.TryStart(false, FireRogueliteStarterCatalog.Universal, 7021,
                enableFixedFirstExperience: true).Success, Is.True);
            var original = store.Values[RogueliteSaveGateway.MapRunKey];
            Assert.That(saves.PrepareSlotForReplacement(), Is.True);
            store.FailWrites = true;
            Assert.That(saves.TryStart(false, FireRogueliteStarterCatalog.Universal, 7022).Success, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(original));
            store.FailWrites = false;
            Assert.That(saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run.IsTutorialPhase, Is.True);
        }

        [Test]
        public void ConfigurationSwitch_DefaultsOffAndCannotChangeAfterLeavingConfiguration()
        {
            var go = new GameObject("opening-switch-test");
            try
            {
                var flow = go.AddComponent<FirstExperiencePrototypeController>();
                flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.Configuration);
                Assert.That(flow.FixedFirstExperienceEnabled, Is.False);
                flow.ToggleFixedFirstExperience();
                Assert.That(flow.FixedFirstExperienceEnabled, Is.True);
                Assert.That(flow.CombatTutorialEnabled, Is.False);
                flow.PreviewUiCaptureStage(FirstExperiencePrototypeController.FlowStage.Map);
                flow.ToggleFixedFirstExperience();
                Assert.That(flow.FixedFirstExperienceEnabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [TestCase(303)]
        [TestCase(912)]
        public void StarterOnlyOpeningEncounters_RecordLegalCombatWithoutTutorialRewards(int seed)
        {
            var run = RogueliteMapRun.CreateSubsequentAcademyRun(seed, includeOpeningGift: false);
            run.ConfirmAcademyDeparture();
            var nodes = run.MapNodes.Where(n => n.Type == RogueliteMapNodeType.Combat && run.IsNodeAvailable(n.Id)).Take(2).ToArray();
            Assert.That(nodes, Is.Not.Empty, "No combat is accessible from the generated starting position.");
            var audit = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("OCC.Combat.EditorTools.AcademyCombatAudit"))
                .FirstOrDefault(t => t != null);
            Assert.That(audit, Is.Not.Null);
            foreach (var node in nodes)
            {
                var record = audit.GetMethod("Fight").Invoke(null, new object[] { seed, node.Id, "starter" });
                var json = JsonUtility.ToJson(record, true);
                TestContext.WriteLine(json);
                var errors = (List<string>)record.GetType().GetField("Errors").GetValue(record);
                Assert.That(errors, Is.Empty, json);
                Assert.That((int)record.GetType().GetField("Commands").GetValue(record), Is.GreaterThan(0));
                // Win rate is diagnostic evidence, not an assertion that a heuristic models a novice.
            }
        }

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool FailWrites;
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out var value) ? value : defaultValue;
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
