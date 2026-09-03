using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueFixedSeedValidationTests
    {
        [Test]
        public void M7SixSeedsByThreeLoadouts_ProduceCompletePassingRecords()
        {
            RogueFixedSeedValidationResult result = RogueFixedSeedValidationRunner.Run();
            Assert.That(result.Passed, Is.True, string.Join(";", result.Errors)); Assert.That(result.Runs.Count, Is.EqualTo(18));
            Assert.That(result.Runs.Select(value => value.SeedCode).Distinct().Count(), Is.EqualTo(6));
            Assert.That(result.Runs.GroupBy(value => value.SeedCode).All(group => group.Select(value => value.Loadout).Distinct().Count() == 3), Is.True);
            Assert.That(result.Runs.All(value => value.HealthIn > 0 && value.HealthOut > 0 && value.Turns > 0 && value.FireRewardCandidateIds.Count == 2), Is.True);
            Assert.That(result.Runs.Any(value => value.BreakStancePrevented > 0), Is.True);
            Assert.That(result.Runs.Any(value => value.TacticalChargesConsumed > 0 && value.EquipmentReplacements > 0), Is.True);
        }

        [Test]
        public void M7FixedSeedMatrix_IsByteStableAcrossRepeatedRuns()
        {
            string first = string.Join("\n", RogueFixedSeedValidationRunner.Run().Runs.Select(value => value.Signature));
            string second = string.Join("\n", RogueFixedSeedValidationRunner.Run().Runs.Select(value => value.Signature));
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
