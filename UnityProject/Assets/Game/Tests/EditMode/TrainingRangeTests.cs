using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class TrainingRangeTests
    {
        [Test]
        public void StandardScenario_ContainsLongTermTestingSamplesWithoutVictoryObjective()
        {
            CombatState state = TrainingRangeScenarioFactory.CreateStandard();
            Assert.That(state.Map.Width, Is.EqualTo(12));
            Assert.That(state.Map.Height, Is.EqualTo(9));
            Assert.That(state.Units.Count, Is.EqualTo(7));
            Assert.That(state.Units.Values.Count(unit => unit.IsHero), Is.EqualTo(2));
            Assert.That(state.Map.GetTile(TrainingRangeScenarioFactory.WaterCell).IsWater, Is.True);
            Assert.That(state.Map.GetTile(TrainingRangeScenarioFactory.DeviceCell).IsDevice, Is.True);
            Assert.That(state.Map.GetTile(TrainingRangeScenarioFactory.ObjectiveCell).IsObjective, Is.True);
            Assert.That(state.Objectives, Is.Empty);
            Assert.That(state.IsVictory, Is.False);
        }

        [Test]
        public void DefaultSession_RegistersAllCurrentSpellsThroughStableProvidersAndPages()
        {
            TrainingRangeSession session = new TrainingRangeSession();
            ArtifactDefinition[] activeArtifacts = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id)).ToArray();
            int fireCount = FireSpellCatalog.All.Count;
            Assert.That(session.Abilities.Count, Is.EqualTo(fireCount + RogueliteSkillCatalog.All.Count + activeArtifacts.Length));
            Assert.That(session.PageCount, Is.EqualTo((session.Abilities.Count + TrainingRangeSession.PageSize - 1) / TrainingRangeSession.PageSize));
            Assert.That(session.Abilities.Take(fireCount).Select(ability => ability.Id), Is.EqualTo(FireSpellCatalog.All.Select(spell => spell.Id)));
            Assert.That(session.Abilities.Skip(fireCount).Take(RogueliteSkillCatalog.All.Count).Select(ability => ability.Id),
                Is.EqualTo(RogueliteSkillCatalog.All.Select(skill => skill.Id)));
            Assert.That(session.Abilities.Skip(fireCount + RogueliteSkillCatalog.All.Count).Select(ability => ability.Id),
                Is.EqualTo(activeArtifacts.Select(artifact => artifact.Id)));
            Assert.That(session.Abilities, Has.All.Matches<TrainingRangeAbilityEntry>(ability =>
                !string.IsNullOrWhiteSpace(ability.Family) && !string.IsNullOrWhiteSpace(ability.Targeting)));
            Assert.That(session.Abilities, Has.All.Matches<TrainingRangeAbilityEntry>(ability =>
                !string.IsNullOrWhiteSpace(ability.IconPath) && Resources.Load<Sprite>(ability.IconPath) != null));
            Assert.That(session.Abilities.Select(ability => ability.IconPath).Distinct(StringComparer.Ordinal).Count(),
                Is.GreaterThanOrEqualTo(39));
        }

        [Test]
        public void EveryRegisteredAbility_HasLegalPreparedCaseAndDeterministicAudit()
        {
            TrainingRangeSession session = new TrainingRangeSession();
            TrainingRangeAuditReport audit = session.RunFullAudit();
            Assert.That(audit.IsSuccess, Is.True, string.Join(Environment.NewLine, audit.Failures));
            Assert.That(audit.Passed, Is.EqualTo(FireSpellCatalog.All.Count + RogueliteSkillCatalog.All.Count + ArtifactCatalog.All.Count(value => ArtifactCatalog.IsCurrentlyUsable(value.Id))));
            Assert.That(audit.IllegalPreviewPassed, Is.EqualTo(FireSpellCatalog.All.Count));
        }

        [Test]
        public void Session_SelectionPreparationPreviewExecutionAndPagingAreIsolated()
        {
            TrainingRangeSession session = new TrainingRangeSession();
            session.Select("F-P-R16");
            ITrainingRangeCase prepared = session.PrepareCurrent();
            Assert.That(session.CurrentFireSpell.Id, Is.EqualTo("F-P-R16"));
            Assert.That(session.LastPreview.CanCommit, Is.True, session.LastPreview.Summary);
            Assert.That(prepared.Combat.GetUnit("range_normal").HasStatus(StatusType.Burning), Is.True);
            Assert.That(session.ExecuteCurrent().Steps, Is.Not.Empty);
            int previousPage = session.CurrentPage;
            session.ShiftPage(1);
            Assert.That(session.CurrentPage, Is.EqualTo((previousPage + 1) % session.PageCount));
            Assert.That(session.CurrentCase, Is.Null);
        }

        [Test]
        public void DestructibleSpell_PreparesObjectAndReportsDurabilityDamage()
        {
            TrainingRangeSession session = new TrainingRangeSession(); session.Select("F-P-R19");
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)session.PrepareCurrent();
            Assert.That(prepared.Combat.Map.GetTile(prepared.RecommendedCell).Durability, Is.GreaterThan(0));
            TrainingRangeExecutionReport result = session.ExecuteCurrent();
            Assert.That(result.Steps.Any(step => step.Contains(nameof(FireRuleKind.DamageDurability))), Is.True);
        }

        [TestCase("phase_step")]
        [TestCase("demolition_charge")]
        [TestCase("thermal_purge")]
        public void GenericSkillProvider_PreparesAndExecutesDifferentTargetContracts(string skillId)
        {
            TrainingRangeSession session = new TrainingRangeSession(); session.Select(skillId);
            ITrainingRangeCase prepared = session.PrepareCurrent();
            Assert.That(session.CurrentSkill.Id, Is.EqualTo(skillId));
            Assert.That(session.LastPreview.CanCommit, Is.True, session.LastPreview.Summary);
            Assert.That(session.ExecuteCurrent().Steps, Is.Not.Empty);
            Assert.That(prepared.Combat.ActiveUnitId, Is.EqualTo("hero"));
        }

        [Test]
        public void ArtifactProvider_LoadsRealLimitedUseDefinitionAndExecutesItsFireDelivery()
        {
            TrainingRangeSession session = new TrainingRangeSession();
            session.Select("F-T01");
            ITrainingRangeCase prepared = session.PrepareCurrent();
            Assert.That(session.CurrentArtifact, Is.SameAs(ArtifactCatalog.DemolitionCanister));
            Assert.That(session.CurrentFireSpell, Is.SameAs(ArtifactCatalog.DemolitionCanister.Spell));
            Assert.That(session.LastPreview.CanCommit, Is.True, session.LastPreview.Summary);
            Assert.That(session.ExecuteCurrent().Steps, Is.Not.Empty);
            Assert.That(prepared.Combat.ActiveUnitId, Is.EqualTo("hero"));
        }
    }
}
