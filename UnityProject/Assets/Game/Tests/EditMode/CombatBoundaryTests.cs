using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatBoundaryTests
    {
        [Test]
        public void AvailabilityQuery_DelegatesPreviewToTheAuthoritativeRuleAdapter()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatActionPreview query = new CombatAvailabilityQuery().Preview(state, "移动", null);
            CombatActionPreview authority = new BattlefieldPresentationAdapter().BuildPreview(state, "移动", null);
            Assert.That(query.FailureReason, Is.EqualTo(authority.FailureReason));
            Assert.That(query.ValidCellCount, Is.EqualTo(authority.ValidCellCount));
        }

        [Test]
        public void EnemyPlan_UsesTheSameCommandForPublicIntentAndExecutionUntilInvalidated()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(2, 0), Facing.West);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 0), Facing.East);
            EnemyArchetypes.Get("pyromancer").Apply(enemy);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);
            CombatCommand execution = plans.GetExecutionCommand(state, enemy, hero);
            Assert.That(intent.Signature, Is.EqualTo(CombatInformationPresenter.CommandSignature(execution)));
            Assert.That(plans.HasPlanFor(enemy.Id), Is.True);
            plans.Invalidate();
            Assert.That(plans.HasPlanFor(enemy.Id), Is.False);
        }

        [Test]
        public void DefaultConfiguration_DoesNotEnableDeveloperEntrypoints()
        {
            Assert.That(DeveloperBuildGate.IsEnabled, Is.False);
        }

        [Test]
        public void PublicIntent_DoesNotExposeCombatCommand()
        {
            Assert.That(typeof(EnemyIntentPresentation).GetProperty("Command"), Is.Null);
            Assert.That(typeof(EnemyIntentPresentation).GetProperty("Destination"), Is.Not.Null,
                "Presentation may expose the read-only destination without exposing the executable command.");
            Assert.That(typeof(EnemyIntentPresentation).GetProperty("ExpectedDamage"), Is.Not.Null);
        }

        [Test]
        public void PresentationComponents_DependOnNarrowHostsInsteadOfBootstrap()
        {
            var expectedHosts = new Dictionary<Type, Type>
            {
                { typeof(CombatVisualFeedback), typeof(ICombatFeedbackHost) },
                { typeof(DeveloperConsolePanel), typeof(IDeveloperConsoleHost) },
                { typeof(FormalCombatHud), typeof(ICombatHudHost) },
                { typeof(FormalRogueliteUi), typeof(IRogueliteUiHost) },
                { typeof(FormalStartupPresentation), typeof(IStartupPresentationHost) },
                { typeof(FormalUiInteractionLayer), typeof(IInteractionPresentationHost) },
                { typeof(RogueliteSettlementPresentation), typeof(ISettlementPresentationHost) },
                { typeof(TacticalHudSceneBinder), typeof(ITacticalHudHost) },
                { typeof(TarkovInventoryPanel), typeof(IInventoryPresentationHost) },
                { typeof(FormalBattlefieldView), typeof(IBattlefieldViewHost) }
            };

            foreach (KeyValuePair<Type, Type> pair in expectedHosts)
            {
                FieldInfo hostField = Array.Find(pair.Key.GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
                    field => field.FieldType == pair.Value);
                Assert.That(hostField, Is.Not.Null, pair.Key.Name + " must retain an explicit injected host boundary.");
                Assert.That(hostField.FieldType, Is.EqualTo(pair.Value), pair.Key.Name + " depends on the wrong host contract.");
                Assert.That(hostField.FieldType, Is.Not.EqualTo(typeof(CombatPrototypeBootstrap)));
            }
        }

        [Test]
        public void Bootstrap_ImplementsEveryPresentationHostContract()
        {
            Type bootstrap = typeof(CombatPrototypeBootstrap);
            Type[] contracts =
            {
                typeof(ICombatFeedbackHost), typeof(IDeveloperConsoleHost), typeof(ICombatHudHost),
                typeof(IRogueliteUiHost), typeof(IStartupPresentationHost), typeof(IInteractionPresentationHost),
                typeof(ISettlementPresentationHost), typeof(ITacticalHudHost), typeof(IInventoryPresentationHost),
                typeof(IBattlefieldViewHost)
            };

            foreach (Type contract in contracts)
                Assert.That(contract.IsAssignableFrom(bootstrap), Is.True, contract.Name + " is not wired by the composition root.");
        }

        [Test]
        public void Bootstrap_DelegatesRuntimeComponentOwnershipToCompositionRegistry()
        {
            Type[] presentationComponents =
            {
                typeof(CombatVisualFeedback), typeof(FormalUiInteractionLayer), typeof(RogueliteSettlementPresentation),
                typeof(FormalCombatHud), typeof(FormalRogueliteUi), typeof(FormalStartupPresentation),
                typeof(DeveloperConsolePanel), typeof(TarkovInventoryPanel), typeof(FormalBattlefieldView)
            };
            FieldInfo[] fields = typeof(CombatPrototypeBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Type component in presentationComponents)
                Assert.That(Array.Exists(fields, field => field.FieldType == component), Is.False,
                    "Bootstrap must not own a direct " + component.Name + " field.");

            FieldInfo composition = Array.Find(fields, field => field.FieldType == typeof(CombatPresentationComposition));
            Assert.That(composition, Is.Not.Null);
            MethodInfo attach = typeof(CombatPresentationComposition).GetMethod("Attach", BindingFlags.Public | BindingFlags.Static);
            Assert.That(attach, Is.Not.Null);
            Assert.That(attach.GetParameters()[1].ParameterType, Is.EqualTo(typeof(ICombatPresentationCompositionHost)));
        }

        [Test]
        public void Bootstrap_NoLongerOwnsImmediateModeBattlefieldRendering()
        {
            MethodInfo onGui = typeof(CombatPrototypeBootstrap).GetMethod("OnGUI",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(onGui, Is.Null, "Battlefield rendering must remain exclusively in FormalBattlefieldView.");
        }

        [Test]
        public void Bootstrap_DelegatesEnemyTurnTransientStateToCoordinator()
        {
            FieldInfo[] fields = typeof(CombatPrototypeBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(Array.Exists(fields, field => field.FieldType == typeof(EnemyTurnCoordinator)), Is.True);
            Assert.That(Array.Exists(fields, field => field.FieldType == typeof(EnemyTurnSequence)), Is.False);
            Assert.That(Array.Exists(fields, field => field.Name == "pendingEnemyCommand"), Is.False);
            Assert.That(Array.Exists(fields, field => field.Name == "enemyTurnSequence"), Is.False);
        }

        [Test]
        public void Bootstrap_DelegatesAuthoritativeCommandExecutionToService()
        {
            FieldInfo[] fields = typeof(CombatPrototypeBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(Array.Exists(fields, field => field.FieldType == typeof(CombatCommandExecutionService)), Is.True);
        }

        [Test]
        public void Bootstrap_DelegatesOutcomeIdempotenceToSettlementCoordinator()
        {
            FieldInfo[] fields = typeof(CombatPrototypeBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(Array.Exists(fields, field => field.FieldType == typeof(CombatOutcomeSettlementCoordinator)), Is.True);
            Assert.That(Array.Exists(fields, field => field.Name == "outcomeHandled"), Is.False);
        }

        [Test]
        public void Bootstrap_DelegatesProductionSessionLifecycleToController()
        {
            FieldInfo[] fields = typeof(CombatPrototypeBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(Array.Exists(fields, field => field.FieldType == typeof(CombatSessionLifecycleController)), Is.True);
            Assert.That(Array.Exists(fields, field => field.Name == "fireLifecycleActiveUnitId"), Is.False);
        }
    }
}
