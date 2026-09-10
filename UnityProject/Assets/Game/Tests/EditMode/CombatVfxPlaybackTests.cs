using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class CombatVfxPlaybackTests
    {
        private static readonly GridPosition Source = new GridPosition(1, 1);
        private static readonly GridPosition Target = new GridPosition(3, 3);

        [Test]
        public void ArmedAttachment_DoesNotPlayItsFutureImpactBeforeTheWeaponTrigger()
        {
            var prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-U05");
            var armed = (FireSpellExecution)prepared.Execute().NativeResult;
            string[] before = FireVfxSequence.From(armed).Select(cue => cue.Effect).Distinct().ToArray();
            Assert.That(before, Does.Contain("fire_attachment"));
            Assert.That(before, Does.Not.Contain("fire_impact"));
            Assert.That(before, Does.Not.Contain("fire_projectile"));
            Assert.That(before, Does.Not.Contain("fire_burning_ground"));
            FireSpellExecution triggered = FireSpellEngine.TriggerWeaponAttack(prepared.Battle, "hero", "range_normal").Single();
            string[] after = FireVfxSequence.From(triggered).Select(cue => cue.Effect).ToArray();
            Assert.That(after, Does.Not.Contain("fire_cast"));
            Assert.That(after, Does.Contain("fire_impact"));
        }

        [Test]
        public void ZeroDamageRecoveryAndGroundResults_DoNotClaimThoseEffectsOccurred()
        {
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U01");
            FireSpellExecution result = Execution(spell,
                Step(spell, FireRuleKind.Damage, 0), Step(spell, FireRuleKind.RestoreShield, 0),
                Step(spell, FireRuleKind.CreateFireground, 0), Step(spell, FireRuleKind.ArmTrigger, 1));
            string[] effects = FireVfxSequence.From(result).Select(cue => cue.Effect).ToArray();
            Assert.That(effects, Is.EquivalentTo(new[] { "fire_cast", "fire_attachment" }));
        }

        [Test]
        public void EveryPreparedSpell_UsesAvailableFramesAndNonOverlappingStagesPerCell()
        {
            int durationOnlyStatuses = 0;
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                var prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare(spell.Id);
                var execution = (FireSpellExecution)prepared.Execute().NativeResult;
                var cues = FireVfxSequence.From(execution);
                Assert.That(cues, Is.Not.Empty, spell.Id);
                foreach (FireSpellResultStep step in execution.Steps.Where(step => step.Applied == 0 &&
                    (step.Kind == FireRuleKind.ApplyBreakStance ||
                    ((step.Kind == FireRuleKind.ApplyBurning || step.Kind == FireRuleKind.ApplyArmorBreak) &&
                        spell.Rules.Any(rule => rule.Kind == step.Kind && rule.Duration > 0)))))
                {
                    durationOnlyStatuses++;
                    string expected = step.Kind == FireRuleKind.ApplyBreakStance ? "fire_break_stance" :
                        step.Kind == FireRuleKind.ApplyArmorBreak ? "armor_break" : "burning";
                    Assert.That(cues.Any(cue => cue.Position == step.Cell && cue.Effect == expected), Is.True, spell.Id);
                }
                foreach (var group in cues.GroupBy(cue => cue.Position))
                {
                    float end = 0f;
                    foreach (CombatVfxCue cue in group.OrderBy(cue => cue.Start))
                    {
                        Assert.That(cue.Start + .00001f, Is.GreaterThanOrEqualTo(end), spell.Id + " " + cue.Effect);
                        Assert.That(cue.Duration, Is.GreaterThan(0f));
                        end = cue.End;
                        for (int frame = 0; frame < 6; frame++)
                            Assert.That(Resources.Load<Sprite>(FormalArtRegistry.VfxPath(cue.Effect) + "/frame_" + frame.ToString("00")),
                                Is.Not.Null, spell.Id + " " + cue.Effect);
                    }
                }
            }
            Assert.That(durationOnlyStatuses, Is.GreaterThan(0), "exercise real zero-strength status results");
        }

        [Test]
        public void ReactionDoesNotEraseDeliveryImpactOrGroundStages()
        {
            var playback = new CombatVfxPlayback();
            playback.ReplaceAbility(Sequence(), 10f);
            playback.PlayReaction(Target, "shield_absorb", 50, 10.13f);
            Assert.That(playback.Sample(10.2f).Select(sample => sample.Cue.Effect),
                Is.EquivalentTo(new[] { "fire_projectile", "shield_absorb" }));
            Assert.That(playback.Sample(10.38f).Any(sample => sample.Cue.Effect == "fire_impact"), Is.True);
            Assert.That(playback.Sample(10.6f).Select(sample => sample.Cue.Effect), Is.EquivalentTo(new[] { "fire_burning_ground" }));
            Assert.That(playback.Sample(11f), Is.Empty);
            Assert.That(playback.PendingSlotCount, Is.Zero);
        }

        [Test]
        public void RepeatedAbility_ReplacesOverlappingTracksWithoutBuildingAnUnboundedQueue()
        {
            var playback = new CombatVfxPlayback();
            for (int i = 0; i < 1000; i++) playback.ReplaceAbility(Sequence(), i * .001f);
            Assert.That(playback.PendingSlotCount, Is.EqualTo(2));
            Assert.That(playback.Sample(999 * .001f + .01f).Single().Cue.Effect, Is.EqualTo("fire_cast"));
            playback.Clear();
            Assert.That(playback.Sample(100f), Is.Empty);
        }

        [Test]
        public void Renderer_SamplesFramesTracksViewportAndClearsAllPendingEffectsWhenAnimationIsDisabled()
        {
            WithView((root, feedback, host, playback, refresh) =>
            {
                playback.ReplaceAbility(Sequence(), 0f);
                refresh.Invoke(feedback, new object[] { .22f });
                Image projectile = root.GetComponentsInChildren<Image>().Single(view => view.name == "正式VFX_fire_projectile");
                Assert.That(projectile.sprite, Is.SameAs(Resources.Load<Sprite>(FormalArtRegistry.VfxPath("fire_projectile") + "/frame_03")));
                Assert.That(projectile.rectTransform.sizeDelta, Is.EqualTo(new Vector2(64f, 64f)));
                Vector2 before = projectile.rectTransform.anchoredPosition;
                host.Pan = 128f; host.Cell = 128f;
                refresh.Invoke(feedback, new object[] { .22f });
                Assert.That(root.GetComponentsInChildren<Image>().Single(view => view.name == "正式VFX_fire_projectile"), Is.SameAs(projectile));
                Assert.That(projectile.rectTransform.sizeDelta, Is.EqualTo(new Vector2(128f, 128f)));
                Assert.That(projectile.rectTransform.anchoredPosition.x, Is.GreaterThan(before.x));
                Assert.That(projectile.rectTransform.anchoredPosition.x * .5f % 1f, Is.Zero);
                playback.PlayReaction(Target, "shield_absorb", 50, .22f);
                refresh.Invoke(feedback, new object[] { .22f });
                Assert.That(root.GetComponentsInChildren<Image>().Count(view => view.name.StartsWith("正式VFX_")), Is.EqualTo(2));
                host.UiPreferences.Configure(1f, 0f, false, true, false, false, true);
                refresh.Invoke(feedback, new object[] { .3f });
                Assert.That(playback.PendingSlotCount, Is.Zero);
                Assert.That(root.GetComponentsInChildren<Image>(), Is.Empty);
                host.UiPreferences.Configure(1f, 1f, true, true, false, false, true);
                refresh.Invoke(feedback, new object[] { 10f });
                Assert.That(root.GetComponentsInChildren<Image>(), Is.Empty);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResetOrDisable_RemovesRenderedAndFutureStagesWithoutChangingCombat(bool disable)
        {
            WithView((root, feedback, host, playback, refresh) =>
            {
                string before = StateSignature(host.CurrentState);
                playback.ReplaceAbility(Sequence(), 0f); refresh.Invoke(feedback, new object[] { .22f });
                if (disable) typeof(CombatVisualFeedback).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
                else feedback.ResetBattleFeedback();
                Assert.That(playback.PendingSlotCount, Is.Zero);
                refresh.Invoke(feedback, new object[] { .6f });
                Assert.That(root.GetComponentsInChildren<Image>(), Is.Empty);
                Assert.That(StateSignature(host.CurrentState), Is.EqualTo(before));
            });
        }

        private static CombatVfxCue[] Sequence() => new[]
        {
            new CombatVfxCue(Source, "fire_cast", 0f, .12f),
            new CombatVfxCue(Target, "fire_projectile", .12f, .18f, Source, true),
            new CombatVfxCue(Target, "fire_impact", .3f, .18f),
            new CombatVfxCue(Target, "fire_burning_ground", .48f, .18f)
        };

        private static FireSpellResultStep Step(FireSpellDefinition spell, FireRuleKind kind, int applied) =>
            new FireSpellResultStep(0, spell.Id, kind, "enemy", Target, 5, applied, "test");
        private static FireSpellExecution Execution(FireSpellDefinition spell, params FireSpellResultStep[] steps) =>
            new FireSpellExecution(new FireSpellPreview(spell, Array.Empty<string>(), new[] { Target },
                new[] { "enemy" }, Array.Empty<GridPosition>(), false, false), steps, "hero", Source);
        private static string StateSignature(CombatState state) => string.Join("|", state.Units.Values.Select(unit =>
            unit.Id + ":" + unit.Position + ":" + unit.Health + ":" + unit.Shield + ":" + unit.Mana + ":" + unit.ActionPoints));

        private static void WithView(Action<GameObject, CombatVisualFeedback, FeedbackHost, CombatVfxPlayback, MethodInfo> verify)
        {
            GameObject root = new GameObject("vfx-playback-test", typeof(Canvas), typeof(CombatVisualFeedback));
            try
            {
                var feedback = root.GetComponent<CombatVisualFeedback>(); var host = new FeedbackHost();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CombatVisualFeedback).GetField("canvas", flags).SetValue(feedback, root.GetComponent<Canvas>());
                typeof(CombatVisualFeedback).GetField("bootstrap", flags).SetValue(feedback, host);
                var playback = (CombatVfxPlayback)typeof(CombatVisualFeedback).GetField("vfxPlayback", flags).GetValue(feedback);
                verify(root, feedback, host, playback, typeof(CombatVisualFeedback).GetMethod("RefreshVfx", flags));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class FeedbackHost : ICombatFeedbackHost
        {
            public float Cell = 64f;
            public float Pan;
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences();
            public CombatState CurrentState { get; } = new CombatState(new GridMap(12, 9),
                new[] { new UnitState("hero", true, Source), new UnitState("enemy", false, Target) },
                new CombatObjective[] { new EliminationObjective() });
            public BattlefieldRect CurrentBattlefieldViewport => new BattlefieldPresentationAdapter().ViewportRect;
            public bool IsDeveloperCombatActive => true;
            public Vector2 GridToFeedbackPosition(GridPosition position) => new Vector2(-624f + position.X * Cell + Pan, 260f - position.Y * Cell);
        }
    }
}
