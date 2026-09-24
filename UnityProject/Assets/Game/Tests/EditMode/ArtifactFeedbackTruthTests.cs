using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class ArtifactFeedbackTruthTests
    {
        private static GridPosition P(int x, int y) => new GridPosition(x, y);
        private static CombatState State(int enemyHealth = 100)
        {
            var enemy = new UnitState("enemy", false, P(1, 0)); enemy.ConfigureVitality(enemyHealth);
            var state = new CombatState(new GridMap(5, 4), new[] { new UnitState("hero", true, P(0, 0)),
                enemy, new UnitState("ally", true, P(0, 1)) });
            CombatResolver.BeginTurn(state, "hero"); return state;
        }
        private static CombatFeedbackEvent[] Events(ArtifactExecution execution) => execution.Steps.SelectMany(step => step.Feedback).ToArray();

        [Test]
        public void EveryCurrentlyUsableArtifact_AccountsForActualResourcesPerUnit()
        {
            var provider = new ArtifactTrainingRangeProvider(); int checkedArtifacts = 0;
            foreach (TrainingRangeAbilityEntry ability in provider.Abilities)
            {
                var prepared = (ArtifactTrainingRangeCase)provider.Prepare(ability.Id); var state = prepared.Battle.Combat;
                var before = state.Units.Values.ToDictionary(unit => unit.Id, unit => (unit.Health, unit.Shield, unit.Mana));
                var execution = (ArtifactExecution)prepared.Execute().NativeResult;
                Assert.That(execution.SourceUnitId, Is.EqualTo("hero"), ability.Id);
                Assert.That(execution.Steps.All(step => step.HasResolvedFeedback), Is.True, ability.Id);
                CombatFeedbackEvent[] events = Events(execution);
                foreach (UnitState unit in state.Units.Values)
                {
                    var own = events.Where(e => e.TargetUnitId == unit.Id).ToArray();
                    int health = own.Where(e => e.Kind == CombatFeedbackKind.Healing).Sum(e => e.Amount) - own.Where(e => e.Kind == CombatFeedbackKind.Damage).Sum(e => e.Amount);
                    int shield = own.Where(e => e.Kind == CombatFeedbackKind.ShieldRestore || e.Kind == CombatFeedbackKind.ShieldTransferredIn).Sum(e => e.Amount) -
                        own.Where(e => e.Kind == CombatFeedbackKind.ShieldAbsorb || e.Kind == CombatFeedbackKind.ShieldConsumed || e.Kind == CombatFeedbackKind.ShieldTransferredOut).Sum(e => e.Amount);
                    Assert.That(health, Is.EqualTo(unit.Health - before[unit.Id].Health), ability.Id + ": health " + unit.Id);
                    Assert.That(shield, Is.EqualTo(unit.Shield - before[unit.Id].Shield), ability.Id + ": shield " + unit.Id);
                    Assert.That(own.Where(e => e.Kind == CombatFeedbackKind.ManaRestore).Sum(e => e.Amount),
                        Is.EqualTo(Math.Max(0, unit.Mana - before[unit.Id].Mana)), ability.Id + ": mana " + unit.Id);
                }
                checkedArtifacts++;
            }
            Assert.That(checkedArtifacts, Is.EqualTo(20));
        }

        [Test]
        public void ShieldBalancer_ReportsShieldTransferredIntoCoverWithoutInventingDamage()
        {
            CombatState state = State(); state.ConfigureRuleset(CombatRuleset.Roguelite);
            var hero = state.GetUnit("hero"); Assert.That(state.TryGrantRogueliteShield(hero.Id, "test", 12), Is.True);
            var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", 0);
            GridPosition cell = P(2, 1);
            var execution = ArtifactEngine.Execute(new ArtifactBattleState(state), "hero", ArtifactCatalog.ShieldBalancer, ArtifactTarget.At(cell), 3);
            CombatFeedbackEvent[] events = Events(execution);
            Assert.That(hero.Shield, Is.Zero);
            Assert.That(state.Map.GetTile(cell).Cover, Is.EqualTo(CoverType.Light));
            Assert.That(state.Map.GetTile(cell).Durability, Is.EqualTo(12));
            Assert.That(events.Single(e => e.Kind == CombatFeedbackKind.ShieldConsumed).Amount, Is.EqualTo(12));
            foreach (var e in events) capture.AddFeedback(e);
            var shown = new List<CombatFeedbackEvent>(); playback.Start(capture, state, 0f, shown.Add); playback.Advance(1f);
            Assert.That(shown, Has.Count.EqualTo(2));
            Assert.That(shown.Any(e => e.Kind == CombatFeedbackKind.UtilityResolved), Is.True);
            Assert.That(shown.Any(e => e.Kind == CombatFeedbackKind.Damage || e.Kind == CombatFeedbackKind.ShieldAbsorb || e.Kind == CombatFeedbackKind.ShieldRestore), Is.False);
        }

        [TestCase(10, false)]
        [TestCase(100, true)]
        public void FortuneSeal_UsesSurvivingVictimConditionAndBacklashBypassesCasterShield(int enemyHealth, bool backlash)
        {
            var state = State(enemyHealth); var hero = state.GetUnit("hero"); var enemy = state.GetUnit("enemy");
            int startingHealth = hero.Health;
            var execution = ArtifactEngine.Execute(new ArtifactBattleState(state), "hero", ArtifactCatalog.FortuneSeal, ArtifactTarget.Unit("enemy", enemy.Position), 2);
            var events = Events(execution);
            Assert.That(hero.Health, Is.EqualTo(startingHealth - (backlash ? 12 : 0))); Assert.That(hero.Shield, Is.EqualTo(2));
            Assert.That(execution.Steps.Any(s => s.Kind == ArtifactEffectKind.BacklashIfTargetSurvives), Is.EqualTo(backlash));
            Assert.That(events.Where(e => e.TargetUnitId == "hero" && e.Kind == CombatFeedbackKind.Damage).Sum(e => e.Amount), Is.EqualTo(backlash ? 12 : 0));
            Assert.That(events.Any(e => e.TargetUnitId == "hero" && e.Kind == CombatFeedbackKind.ShieldAbsorb), Is.False);
            Assert.That(events.Where(e => e.TargetUnitId == "enemy" && (e.Kind == CombatFeedbackKind.Damage || e.Kind == CombatFeedbackKind.ShieldAbsorb)).Sum(e => e.Amount),
                Is.EqualTo(Math.Min(28, enemyHealth + 2)), "actual loss, including overkill clipping, is counted once");
        }

        [Test]
        public void RepairFrame_ReportsShieldCostWithoutAHitOrAdditionalSnapshotAbsorption()
        {
            var prepared = (ArtifactTrainingRangeCase)new ArtifactTrainingRangeProvider().Prepare("G-T06");
            var state = prepared.Battle.Combat; var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", 0);
            var execution = (ArtifactExecution)prepared.Execute().NativeResult; var events = Events(execution);
            Assert.That(events.Single(e => e.Kind == CombatFeedbackKind.ShieldConsumed).Amount, Is.EqualTo(12));
            Assert.That(events.Any(e => e.Kind == CombatFeedbackKind.ShieldAbsorb), Is.False);
            foreach (var e in events) capture.AddFeedback(e);
            var shown = new List<CombatFeedbackEvent>(); playback.Start(capture, state, 0, shown.Add); playback.Advance(1);
            Assert.That(shown.Count, Is.EqualTo(events.Length));
        }

        [Test]
        public void WeakerExistingStatus_DoesNotProduceFalseStatusFeedback()
        {
            var state = State(); var enemy = state.GetUnit("enemy"); enemy.ApplyStatus(StatusType.Bound, 3);
            var execution = ArtifactEngine.Execute(new ArtifactBattleState(state), "hero", ArtifactCatalog.BindingFrame, ArtifactTarget.Unit("enemy", enemy.Position), 2);
            Assert.That(execution.Steps.Count, Is.EqualTo(1)); Assert.That(execution.Steps[0].Applied, Is.Zero);
            Assert.That(Events(execution), Is.Empty); Assert.That(enemy.StatusDuration(StatusType.Bound), Is.EqualTo(4));
        }

        [Test]
        public void ReflectionReaction_PreservesBothActorsAndCanBeConsumedOnlyOnce()
        {
            var state = State(); var battle = new ArtifactBattleState(state); var hero = state.GetUnit("hero"); var enemy = state.GetUnit("enemy");
            var armed = ArtifactEngine.Execute(battle, "hero", ArtifactCatalog.PrismRegulator, ArtifactTarget.Unit("hero", hero.Position), 2);
            Assert.That(Events(armed).Single().Kind, Is.EqualTo(CombatFeedbackKind.UtilityResolved));
            var reaction = battle.ResolveIncomingRangedHit("hero", "enemy", 5); var events = Events(reaction);
            Assert.That(reaction.IsTriggered, Is.True); Assert.That(reaction.SourceUnitId, Is.EqualTo("hero"));
            Assert.That(events.Single(e => e.Kind == CombatFeedbackKind.ShieldRestore).TargetUnitId, Is.EqualTo("hero"));
            Assert.That(events.Single(e => e.Kind == CombatFeedbackKind.ShieldRestore).Amount, Is.EqualTo(5));
            Assert.That(events.Where(e => e.TargetUnitId == "enemy" && (e.Kind == CombatFeedbackKind.Damage || e.Kind == CombatFeedbackKind.ShieldAbsorb)).Sum(e => e.Amount), Is.EqualTo(8));
            Assert.That(battle.TakeResolvedReactions().Count, Is.EqualTo(1)); Assert.That(battle.TakeResolvedReactions(), Is.Empty);
            Assert.That(battle.ResolveIncomingRangedHit("hero", "enemy", 5).Steps, Is.Empty);
        }

        [Test]
        public void NewFeedbackSemantics_ReuseExistingSpritesAndDoNotPlayFakeImpactVfx()
        {
            GameObject root = new GameObject("artifact-semantics-test", typeof(CombatVisualFeedback));
            try
            {
                var feedback = root.GetComponent<CombatVisualFeedback>();
                MethodInfo icon = typeof(CombatVisualFeedback).GetMethod("SemanticIcon", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo vfx = typeof(CombatVisualFeedback).GetMethod("VfxForFeedback", BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var kind in new[] { CombatFeedbackKind.ShieldConsumed, CombatFeedbackKind.ShieldTransferredOut, CombatFeedbackKind.ShieldTransferredIn, CombatFeedbackKind.UtilityResolved })
                {
                    Assert.That(icon.Invoke(feedback, new object[] { CombatFeedbackCatalog.For(kind).Key }), Is.Not.Null);
                    Assert.That(vfx.Invoke(null, new object[] { kind }), Is.Null);
                }
                Assert.That(ArtifactCatalog.IsCurrentlyUsable("G-T04"), Is.True);
                Assert.That(ArtifactCatalog.IsCurrentlyUsable("G-T18"), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DelayedFeedback_UsesCapturedUnitIdentityAfterAnotherUnitOccupiesItsOriginalCell()
        {
            var state = State(); var enemy = state.GetUnit("enemy"); var ally = state.GetUnit("ally");
            var execution = ArtifactEngine.Execute(new ArtifactBattleState(state), "hero", ArtifactCatalog.FortuneSeal, ArtifactTarget.Unit("enemy", enemy.Position), 2);
            CombatFeedbackEvent damage = Events(execution).Single(e => e.Kind == CombatFeedbackKind.Damage && e.TargetUnitId == "enemy");
            GridPosition capturedCell = damage.Target;
            CombatEffectExecutor.Execute(state, "enemy", CombatEffect.Move(P(2, 0)));
            CombatEffectExecutor.Execute(state, "ally", CombatEffect.Move(capturedCell));
            GameObject root = new GameObject("artifact-identity-test", typeof(UnityEngine.Canvas), typeof(CombatVisualFeedback));
            try
            {
                var feedback = root.GetComponent<CombatVisualFeedback>(); const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CombatVisualFeedback).GetField("bootstrap", flags).SetValue(feedback, new Host(state));
                typeof(CombatVisualFeedback).GetField("canvas", flags).SetValue(feedback, root.GetComponent<UnityEngine.Canvas>());
                feedback.Publish(damage);
                var healthCache = (Dictionary<string, int>)typeof(CombatVisualFeedback).GetField("healthCache", flags).GetValue(feedback);
                Assert.That(healthCache["enemy"], Is.EqualTo(enemy.Health)); Assert.That(healthCache.ContainsKey("ally"), Is.False);
                Assert.That(damage.Target, Is.EqualTo(capturedCell)); Assert.That(damage.TargetUnitId, Is.EqualTo("enemy"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class Host : ICombatFeedbackHost
        {
            public CombatState CurrentState { get; }
            public Host(CombatState state) { CurrentState = state; UiPreferences.Configure(1f, 0f, false, false, false, false, true); }
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences();
            public BattlefieldRect CurrentBattlefieldViewport => new BattlefieldPresentationAdapter().ViewportRect;
            public bool IsDeveloperCombatActive => true;
            public Vector2 GridToFeedbackPosition(GridPosition p) => new Vector2(p.X * 64, -p.Y * 64);
        }
    }
}
