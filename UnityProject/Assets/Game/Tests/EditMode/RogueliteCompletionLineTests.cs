using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteCompletionLineTests
    {
        [TestCase(620, "core_overseer")]
        [TestCase(621, "core_overseer")]
        public void TwoFixedSeeds_CompleteFirstRegionAndRoundTripEveryBoundary(int seed, string expectedBoss)
        {
            RogueliteMapRun run = RoundTrip(new RogueliteMapRun(seed));
            Assert.That(run.RegionBossId, Is.EqualTo(expectedBoss));

            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();
            run = RoundTrip(run);
            SettleUniqueReward(run);
            run = RoundTrip(run);

            run.SelectNode("switchyard");
            run.ChooseCurrentNodeContent("overload");
            run = RoundTrip(run);
            Assert.That(run.HasPendingContentCombat, Is.True);
            run.CompletePendingContentCombat();
            run = RoundTrip(run);

            run.SelectNode("relay_event");
            run.ChooseCurrentNodeContent("survey");
            run.SelectNode("med_bay");
            run.ChooseCurrentNodeContent("field_repair");
            run.SelectNode("permit_archive");
            run.ChooseCurrentNodeContent("survey");
            run = RoundTrip(run);
            Assert.That(run.AccessCards, Is.GreaterThanOrEqualTo(1));

            run.SelectNode("safety_room");
            run.ChooseCurrentNodeContent("scan_routes");
            run.SelectNode("aether_refinery");
            run.ChooseCurrentNodeContent("purify");
            run = RoundTrip(run);

            CompleteCombatAndSettle(ref run, "transmission_tower");
            CompleteCombatAndSettle(ref run, "core_approach");
            CompleteRewardNodeAndSettle(ref run, "core_vault");
            CompleteCombatAndSettle(ref run, "observatory_path");
            CompleteCombatAndSettle(ref run, "wilds_camp");
            run.SelectNode("observatory_path");
            run.SelectNode("core_vault");
            CompleteCombatAndSettle(ref run, "core_finale");

            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.AwaitingReward, Is.False);
            Assert.That(run.ClaimedRewards.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(run.ClaimedRewards.Count));
            Assert.That(run.CompletedNodes, Does.Contain("core_finale"));

            run.SelectNode("core_approach");
            Assert.That(RogueliteUiPreferences.StartsCombat(run, RogueliteMapCatalog.Node("core_approach")), Is.False);
            run.SelectNode("transmission_tower");
            Assert.That(RogueliteUiPreferences.StartsCombat(run, RogueliteMapCatalog.Node("transmission_tower")), Is.False);

            UnitState nextBattleHero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            run.ApplyBuild(nextBattleHero);
            Assert.That(nextBattleHero.Statuses, Is.Empty);
            Assert.That(nextBattleHero.Cooldown(nextBattleHero.SkillOne), Is.Zero);
            Assert.That(RoundTrip(run).ToJson(), Is.EqualTo(run.ToJson()));
        }

        [Test]
        public void FourBuilds_DeterministicallyDefeatBothFirstRegionBossVariants()
        {
            foreach (RogueliteSkillBuild build in RogueliteSkillCatalog.Builds)
            foreach (string bossId in new[] { "core_overseer", "purifier_overseer" })
            {
                string first = DefeatSignature(build, bossId);
                string second = DefeatSignature(build, bossId);
                Assert.That(second, Is.EqualTo(first), build.Id + " / " + bossId);
            }
        }

        [Test]
        public void CompletionLine_HasNoTimePressureOrHiddenChanceContracts()
        {
            string[] mapMembers = typeof(RogueliteMapRun).GetMembers().Select(member => member.Name).ToArray();
            string[] skillMembers = typeof(SkillDefinition).GetMembers().Select(member => member.Name).ToArray();
            Assert.That(mapMembers, Has.None.Contains("Countdown"));
            Assert.That(mapMembers, Has.None.Contains("Pursuit"));
            Assert.That(mapMembers, Has.None.Contains("ThreatAdvance"));
            Assert.That(skillMembers, Has.None.Contains("HitChance"));
            Assert.That(skillMembers, Has.None.Contains("CriticalChance"));
        }

        private static void CompleteCombatAndSettle(ref RogueliteMapRun run, string nodeId)
        {
            run.SelectNode(nodeId);
            run.CompleteCurrentCombat();
            run = RoundTrip(run);
            SettleUniqueReward(run);
            run = RoundTrip(run);
        }

        private static void CompleteRewardNodeAndSettle(ref RogueliteMapRun run, string nodeId)
        {
            run.SelectNode(nodeId);
            run.ChooseCurrentNodeContent("vault_fire_cache");
            run = RoundTrip(run);
            SettleUniqueReward(run);
            run = RoundTrip(run);
        }

        private static void SettleUniqueReward(RogueliteMapRun run)
        {
            FireSpellDefinition fireSpell = run.CurrentFireSpellChoices.FirstOrDefault();
            if (fireSpell != null)
            {
                run.ClaimFireSpell(fireSpell.Id);
                Assert.Throws<InvalidOperationException>(() => run.ClaimFireSpell(fireSpell.Id));
                return;
            }
            RogueliteReward reward = run.CurrentRewards.FirstOrDefault(item => !run.ClaimedRewards.Contains(item.Id));
            Assert.That(reward, Is.Not.Null, "Every first-region combat must offer at least one unowned reward.");
            run.ClaimReward(reward.Id);
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(reward.Id));
        }

        private static RogueliteMapRun RoundTrip(RogueliteMapRun run)
        {
            string data = run.ToJson();
            RogueliteMapRun restored = RogueliteMapRun.FromJson(data);
            Assert.That(restored.ToJson(), Is.EqualTo(data));
            return restored;
        }

        private static string DefeatSignature(RogueliteSkillBuild build, string bossId)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(2, 1), Facing.West);
            UnitState boss = new UnitState("boss", false, new GridPosition(1, 1), Facing.West);
            EnemyArchetypes.Get(bossId).Apply(boss);
            build.Apply(hero);
            CombatState state = new CombatState(new GridMap(4, 3), new[] { hero, boss }, new CombatObjective[] { new EliminationObjective() });

            int turns = 0;
            while (boss.IsAlive && turns < 32)
            {
                CombatResolver.BeginTurn(state, hero.Id);
                if (CanTargetEnemy(hero.SkillOne) && hero.Mana >= hero.SkillOne.ManaCost && hero.Cooldown(hero.SkillOne) == 0)
                    CombatResolver.Resolve(state, CombatCommand.UseSkill(hero.Id, 0, boss.Id));
                while (boss.IsAlive && hero.ActionPoints > 0)
                    CombatResolver.Resolve(state, CombatCommand.Attack(hero.Id, boss.Id));
                turns++;
            }

            Assert.That(state.IsVictory, Is.True, build.Id + " / " + bossId);
            return string.Join("|", build.Id, bossId, turns, boss.Health, hero.Health, string.Join("~", state.EventLog));
        }

        private static bool CanTargetEnemy(SkillDefinition skill)
        {
            return skill.TargetRule == SkillTargetRule.EnemyUnit || skill.TargetRule == SkillTargetRule.AnyUnit;
        }
    }
}
