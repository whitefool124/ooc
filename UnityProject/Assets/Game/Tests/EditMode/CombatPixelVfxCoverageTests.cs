using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatPixelVfxCoverageTests
    {
        private sealed class Sink : IResolvedCombatFeedbackSink
        {
            public readonly HashSet<string> Effects = new HashSet<string>();
            public void Publish(CombatFeedbackEvent e) { string fx = CombatPixelVfxPolicy.Feedback(e.Kind); if (fx != null) Effects.Add(fx); }
            public void NotifyMovement(string id, GridPosition a, GridPosition b, IReadOnlyList<GridPosition> path) => Effects.Add("path");
            public void NotifyStatusApplied(GridPosition p, StatusType status, int duration) => Publish(new CombatFeedbackEvent(CombatFeedbackCatalog.ForStatus(status), p));
            public void NotifyDestructible(GridPosition p, TileState tile) => Effects.Add(tile.Durability <= 0 ? "object_break" : "object_damage");
            public void NotifyFireSpell(FireSpellExecution e) { foreach (var cue in FireVfxSequence.From(e)) Effects.Add(cue.Effect); }
        }
        [Serializable] private sealed class Row { public string id, category; public bool covered; public string[] effects; }
        [Serializable] private sealed class Report { public int total, covered, eventTotal, eventWithVfx, shieldEventsExcluded; public float percent; public Row[] rows; }

        [Test]
        public void ResolvedAbilities_HaveResultEffectsAboveNinetyPercent_IncludingShieldOnlyInDenominator()
        {
            var rows = new List<Row>();
            foreach (var spell in FireSpellCatalog.All)
            {
                var prepared = new FireSpellTrainingRangeProvider().Prepare(spell.Id);
                var execution = (FireSpellExecution)prepared.Execute().NativeResult;
                foreach (var step in execution.Steps.Where(s => FireVfxSequence.HasVisualResult(s, spell)))
                    Assert.That(FireVfxSequence.ResultEffect(step.Kind, false), Is.Not.Null, spell.Id + " unmapped resolved result " + step.Kind);
                string[] effects = FireVfxSequence.From(execution).Where(c => c.Effect != "fire_cast").Select(c => c.Effect).Distinct().ToArray();
                rows.Add(new Row { id = spell.Id, category = "fire", covered = effects.Length > 0, effects = effects });
            }
            foreach (var skill in RogueliteSkillCatalog.All)
            {
                var prepared = new SkillTrainingRangeProvider().Prepare(skill.Id);
                var result = (CombatEffectExecution)prepared.Execute().NativeResult;
                var sink = new Sink(); new CombatFeedbackPublisher().PublishCombatEffects(prepared.Combat, sink, result);
                // Delivery alone does not prove a result happened.
                rows.Add(new Row { id = skill.Id, category = "generic", covered = sink.Effects.Count > 0, effects = sink.Effects.OrderBy(x => x).ToArray() });
            }
            var artifacts = new ArtifactTrainingRangeProvider();
            foreach (var ability in artifacts.Abilities)
            {
                var prepared = artifacts.Prepare(ability.Id);
                var result = (ArtifactExecution)prepared.Execute().NativeResult;
                var sink = new Sink();
                foreach (var step in result.Steps.Where(s => s.HasResolvedFeedback))
                    foreach (var feedback in step.Feedback) sink.Publish(feedback);
                rows.Add(new Row { id = ability.Id, category = "artifact", covered = sink.Effects.Count > 0, effects = sink.Effects.OrderBy(x => x).ToArray() });
            }
            foreach (Row row in rows)
                foreach (string effect in row.effects)
                    if (!CombatPixelVfxPolicy.IsTechnical(effect))
                    for (int f = 0; f < 6; f++)
                        Assert.That(Resources.Load<Sprite>(FormalArtRegistry.VfxPath(effect) + "/frame_" + f.ToString("00")), Is.Not.Null, row.id + " " + effect);
            var kinds = (CombatFeedbackKind[])Enum.GetValues(typeof(CombatFeedbackKind));
            var report = new Report { total = rows.Count, covered = rows.Count(r => r.covered), eventTotal = kinds.Length,
                eventWithVfx = kinds.Count(k => CombatPixelVfxPolicy.Feedback(k) != null),
                shieldEventsExcluded = kinds.Count(CombatPixelVfxPolicy.IsShieldResource), rows = rows.ToArray() };
            report.percent = report.covered * 100f / report.total;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_战斗特效覆盖"));
            Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "coverage.json"), JsonUtility.ToJson(report, true));
            Assert.That(report.percent, Is.GreaterThanOrEqualTo(90f));
            Assert.That(report.eventWithVfx + report.shieldEventsExcluded, Is.EqualTo(report.eventTotal));
        }

        [Test]
        public void ShieldOnlySkills_KeepNumbersWithoutShellOrTravelingShieldEffects()
        {
            foreach (var kind in ((CombatFeedbackKind[])Enum.GetValues(typeof(CombatFeedbackKind))).Where(CombatPixelVfxPolicy.IsShieldResource))
                Assert.That(CombatPixelVfxPolicy.Feedback(kind), Is.Null);
            foreach (var skill in RogueliteSkillCatalog.All.Where(s => s.Effects.All(e => e.Type == SkillEffectType.RestoreShield)))
                Assert.That(CombatPixelVfxPolicy.Delivery(skill), Is.Null, skill.Id);
        }

        [Test]
        public void ProjectileDelivery_UsesTravelForFireAndNonFireWithoutPretendingImpactIsAStatus()
        {
            foreach (var skill in RogueliteSkillCatalog.All.Where(s => s.Delivery == SkillDeliveryMethod.Projectile))
            {
                Assert.That(CombatPixelVfxPolicy.Delivery(skill), Is.Not.Null, skill.Id);
                Assert.That(CombatPixelVfxPolicy.Travels(skill), Is.True, skill.Id);
            }
            Shader shader = Resources.Load<Shader>("Shaders/CombatPixelLight");
            Assert.That(shader, Is.Not.Null); Assert.That(shader.isSupported, Is.True);
        }
    }
}
