using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class CombatActionPlaybackTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static GridPosition P(int x, int y) => new GridPosition(x, y);
        private static CombatState State() => new CombatState(new GridMap(5, 4),
            new[] { new UnitState("hero", true, P(0, 0)), new UnitState("enemy", false, P(1, 0)) });

        [Test]
        public void LethalHit_WaitsForMovementAndContactWhileCostsRemainSettled()
        {
            var state = State(); var hero = state.GetUnit("hero"); var enemy = state.GetUnit("enemy");
            hero.ConfigureVitality(87); hero.ConfigureMana(20); CombatResolver.BeginTurn(state, hero.Id);
            var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, hero.Id, .2f);
            int launches = 0; var shown = new List<CombatFeedbackEvent>();
            capture.AddDelivery(() => launches++); capture.SetImpact(enemy.Position, .3f);
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.SpendActionPoints(1), CombatEffect.SpendMana(2), CombatEffect.DamageHealth(enemy.Id, 99));
            string final = Signature(state); playback.Start(capture, state, 0f, shown.Add);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(playback.Unit(enemy, .49f).IsAlive, Is.True);
            Assert.That(playback.Unit(hero, .01f).MaxHealth, Is.EqualTo(87));
            Assert.That(playback.Unit(hero, .01f).MaxMana, Is.EqualTo(20));
            Assert.That(playback.Unit(hero, .01f).ActionPoints, Is.EqualTo(hero.ActionPoints));
            Assert.That(playback.Unit(hero, .01f).Mana, Is.EqualTo(hero.Mana));
            playback.Advance(.19f); Assert.That(launches, Is.Zero); Assert.That(shown, Is.Empty);
            playback.Advance(.2f); Assert.That(launches, Is.EqualTo(1)); Assert.That(shown, Is.Empty);
            playback.Advance(.5f); Assert.That(playback.Unit(enemy, .5f).IsAlive, Is.False);
            Assert.That(shown.Count(f => f.Kind == CombatFeedbackKind.Damage), Is.EqualTo(1));
            Assert.That(shown.Count(f => f.Kind == CombatFeedbackKind.UnitDefeated), Is.EqualTo(1));
            Assert.That(playback.IsPlaying(.7f), Is.True, "leave time to read the fatal hit before settlement");
            playback.Advance(2f); Assert.That(playback.IsPlaying(2f), Is.False);
            Assert.That(Signature(state), Is.EqualTo(final));
        }

        [Test]
        public void ExplicitMultiHitResults_AreNeitherDuplicatedNorReorderedBySnapshotDiffs()
        {
            var state = State(); var enemy = state.GetUnit("enemy"); var playback = new CombatActionPlayback();
            var capture = playback.Capture(state, null, "hero", 0);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageHealth(enemy.Id, 5));
            capture.AddFeedback(new CombatFeedbackEvent(CombatFeedbackKind.Damage, enemy.Position, 2));
            capture.AddFeedback(new CombatFeedbackEvent(CombatFeedbackKind.Damage, enemy.Position, 3));
            var shown = new List<CombatFeedbackEvent>(); playback.Start(capture, state, 0, shown.Add);
            playback.Advance(.16f); playback.Advance(.17f);
            Assert.That(shown.Where(f => f.Kind == CombatFeedbackKind.Damage).Select(f => f.Amount), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void ActualFireProjectile_ReleasesDamageAtItsActualImpactCue()
        {
            var prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-R01");
            CombatState state = prepared.Battle.Combat;
            var health = state.Units.Values.ToDictionary(u => u.Id, u => u.Health);
            var shield = state.Units.Values.ToDictionary(u => u.Id, u => u.Shield);
            var playback = new CombatActionPlayback(); var capture = playback.Capture(state, prepared.Battle, "hero", 0f);
            var execution = (FireSpellExecution)prepared.Execute().NativeResult;
            var cues = FireVfxSequence.From(execution);
            capture.AddDelivery(() => { }, cues);
            UnitState target = state.Units.Values.First(u => u.Health < health[u.Id] || u.Shield < shield[u.Id]);
            float impact = cues.First(c => c.Position == target.Position && c.Effect == "fire_impact").Start;
            Assert.That(impact, Is.EqualTo(.3f).Within(.001f));
            var shown = new List<CombatFeedbackEvent>(); playback.Start(capture, state, 0, shown.Add);
            playback.Advance(impact - .001f);
            Assert.That(playback.Unit(target, impact - .001f).Health, Is.EqualTo(health[target.Id]));
            Assert.That(shown.Any(f => f.Kind == CombatFeedbackKind.Damage || f.Kind == CombatFeedbackKind.ShieldAbsorb), Is.False);
            playback.Advance(impact);
            Assert.That(playback.Unit(target, impact), Is.SameAs(target));
            Assert.That(shown.Any(f => f.Kind == CombatFeedbackKind.Damage || f.Kind == CombatFeedbackKind.ShieldAbsorb), Is.True);
        }

        [Test]
        public void WeaponTrigger_StartsAtWeaponContactInsteadOfBeforeTheAttack()
        {
            var state = State(); var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", 0f);
            var target = state.GetUnit("enemy"); bool triggered = false;
            capture.AddDelivery(() => triggered = true, new[] { new CombatVfxCue(target.Position, "fire_impact", 0, .18f) }, .16f);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageHealth(target.Id, 3));
            var shown = new List<CombatFeedbackEvent>(); playback.Start(capture, state, 0, shown.Add);
            playback.Advance(.15f); Assert.That(triggered, Is.False); Assert.That(shown, Is.Empty);
            playback.Advance(.16f); Assert.That(triggered, Is.True); Assert.That(shown, Has.Count.EqualTo(1));
        }

        [Test]
        public void TerrainAndFireground_WaitForContactAndClearDoesNotReplayOldCallbacks()
        {
            var state = State(); var cell = P(2, 1); state.Map.SetTile(cell, new TileState { IsLampVine = true });
            var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", 0f);
            capture.SetImpact(cell, .3f); int callbacks = 0; capture.AddDelivery(() => callbacks++);
            state.Map.SetTile(cell, new TileState { IsScorched = true });
            playback.Start(capture, state, 0f, _ => callbacks++);
            Assert.That(playback.Tile(cell, state.Map.GetTile(cell), .29f).IsLampVine, Is.True);
            Assert.That(playback.Fireground(cell, true, .29f), Is.False);
            Assert.That(playback.Tile(cell, state.Map.GetTile(cell), .3f).IsScorched, Is.True);
            Assert.That(playback.Fireground(cell, true, .3f), Is.True);
            playback.Clear(); playback.Advance(9f); Assert.That(callbacks, Is.Zero);
        }

        [Test]
        public void DisabledAnimation_FlushesActualFeedbackOnceAndReleasesAllSnapshots()
        {
            var state = State(); var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", .35f);
            var enemy = state.GetUnit("enemy"); CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageHealth(enemy.Id, 3));
            var shown = new List<CombatFeedbackEvent>(); string final = Signature(state);
            playback.Start(capture, state, 0, shown.Add); playback.Advance(.01f, false); playback.Advance(10f, false);
            Assert.That(shown, Has.Count.EqualTo(1)); Assert.That(shown[0].Amount, Is.EqualTo(3));
            Assert.That(playback.IsPlaying(.01f), Is.False); Assert.That(playback.Unit(enemy, .01f), Is.SameAs(enemy));
            Assert.That(Signature(state), Is.EqualTo(final));
        }

        [Test]
        public void SlowFrame_DispatchesWithScheduledTimeSoExpiredEffectsDoNotRestart()
        {
            var state = State(); var playback = new CombatActionPlayback(); var capture = playback.Capture(state, null, "hero", .2f);
            float? deliveryTime = null;
            capture.AddDelivery(() => deliveryTime = playback.DispatchTime);
            playback.Start(capture, state, 0, _ => { }); playback.Advance(5f);
            Assert.That(deliveryTime, Is.EqualTo(.2f)); Assert.That(playback.DispatchTime, Is.Null); Assert.That(playback.IsPlaying(5f), Is.False);
        }

        [Test]
        public void FeedbackIntegration_CapturesResultAndPreventsTheAutomaticScannerFromPublishingItEarly()
        {
            GameObject root = new GameObject("action-feedback-test", typeof(Canvas), typeof(CombatVisualFeedback));
            try
            {
                var host = new Host(); var feedback = root.GetComponent<CombatVisualFeedback>();
                typeof(CombatVisualFeedback).GetField("bootstrap", Private).SetValue(feedback, host);
                typeof(CombatVisualFeedback).GetField("canvas", Private).SetValue(feedback, root.GetComponent<Canvas>());
                var enemy = host.CurrentState.GetUnit("enemy");
                using (var capture = feedback.BeginResolvedAction("hero"))
                {
                    var result = CombatEffectExecutor.Execute(host.CurrentState, "hero", CombatEffect.DamageHealth(enemy.Id, 3));
                    new CombatFeedbackPublisher().PublishCombatEffects(host.CurrentState, feedback, result);
                    capture.Complete();
                }
                Assert.That(feedback.IsActionPlaying, Is.True);
                Assert.That(feedback.PresentedUnit(enemy).Health, Is.EqualTo(enemy.Health + 3));
                var cache = (Dictionary<string, int>)typeof(CombatVisualFeedback).GetField("healthCache", Private).GetValue(feedback);
                Assert.That(cache[enemy.Id], Is.EqualTo(enemy.Health));
                feedback.ResetBattleFeedback(); Assert.That(feedback.IsActionPlaying, Is.False);
                Assert.That(feedback.PresentedUnit(enemy), Is.SameAs(enemy));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(64f)]
        [TestCase(128f)]
        public void Camera_FollowsCurrentPositionByMinimumDistanceAndKeepsOverviewStable(float size)
        {
            var viewport = new BattlefieldPresentationAdapter().CreateViewport(12, 9);
            if (size == 128f) viewport.ZoomAt(400, 400, 1);
            var before = viewport.BoardRect;
            viewport.FollowVisualPosition(5f, 4f);
            Assert.That(viewport.BoardRect.X, Is.EqualTo(before.X)); Assert.That(viewport.BoardRect.Y, Is.EqualTo(before.Y));
            viewport.FollowVisualPosition(10.5f, .5f);
            if (size == 64f) { Assert.That(viewport.BoardRect.X, Is.EqualTo(before.X)); Assert.That(viewport.BoardRect.Y, Is.EqualTo(before.Y)); }
            else
            {
                Assert.That(viewport.BoardRect.X % 2, Is.Zero); Assert.That(viewport.BoardRect.Y % 2, Is.Zero);
                float pointX = viewport.BoardRect.X + 11f * size;
                Assert.That(pointX, Is.LessThanOrEqualTo(viewport.ViewportRect.XMax - 1.5f * size + 1f));
                var stayed = viewport.BoardRect; viewport.FollowVisualPosition(10.5f, .5f);
                Assert.That(viewport.BoardRect.X, Is.EqualTo(stayed.X)); Assert.That(viewport.BoardRect.Y, Is.EqualTo(stayed.Y));
            }
        }

        private static string Signature(CombatState state) => string.Join("|", state.Units.Values.Select(u =>
            u.Id + ":" + u.Position + ":" + u.Health + ":" + u.Shield + ":" + u.Mana + ":" + u.ActionPoints));

        [Test]
        public void CombatSnapshot_PreservesConfiguredVitalLimitsAndRemainsIndependent()
        {
            var state = State(); var hero = state.GetUnit("hero");
            hero.ConfigureVitality(87); hero.ConfigureMana(20, 15);
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageHealth("hero", 7));
            CombatState clone = state.Clone(); var copy = clone.GetUnit("hero");
            Assert.That(copy.MaxHealth, Is.EqualTo(87)); Assert.That(copy.Health, Is.EqualTo(80));
            Assert.That(copy.MaxMana, Is.EqualTo(20)); Assert.That(copy.Mana, Is.EqualTo(15));
            CombatEffectExecutor.Execute(clone, "hero", CombatEffect.RestoreHealth("hero", 50), CombatEffect.RestoreMana("hero", 50));
            Assert.That(copy.Health, Is.EqualTo(87)); Assert.That(copy.Mana, Is.EqualTo(20));
            Assert.That(hero.Health, Is.EqualTo(80)); Assert.That(hero.Mana, Is.EqualTo(15));
        }
        private sealed class Host : ICombatFeedbackHost
        {
            public CombatState CurrentState { get; } = State();
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences();
            public BattlefieldRect CurrentBattlefieldViewport => new BattlefieldPresentationAdapter().ViewportRect;
            public bool IsDeveloperCombatActive => true;
            public Vector2 GridToFeedbackPosition(GridPosition p) => new Vector2(p.X * 64, -p.Y * 64);
        }
    }
}
