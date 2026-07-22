using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteDeveloperRunTests
    {
        [Test]
        public void Catalog_OpensOnlyEliminationAndDestructionSandboxTemplates()
        {
            Assert.That(RogueliteDeveloperCatalog.OpenSandboxTemplates.Select(template => template.Type), Is.EquivalentTo(new[] { CombatObjectiveType.Elimination, CombatObjectiveType.Destruction }));
            Assert.That(RogueliteDeveloperCatalog.FindMission("factory_breach").ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void StoryRun_AdvancesOnlyWhenItsMissionIsCompleted()
        {
            var run = new RogueliteDeveloperRun(RogueliteStoryCatalog.CreateDefault(100));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("dead_signal"));
            run.Complete("signal secured");
            Assert.That(run.Package.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(run.CurrentMission.ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void SandboxRun_DoesNotAdvanceStoryPackage()
        {
            var run = new RogueliteDeveloperRun("elimination_rail", 200);
            run.Complete("sandbox result");
            Assert.That(run.Package.CurrentMissionIndex, Is.EqualTo(0));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("sandbox_elimination"));
        }

        [Test]
        public void RogueliteSaveManager_IsolatedAndRoundTripsPackageOnly()
        {
            var save = new RogueliteSaveManager();
            var package = RogueliteStoryCatalog.CreateDefault(300);
            package.CompleteCurrentMission("first"); save.Save(package);
            Assert.That(save.HasSave("iron_echoes"), Is.True);
            Assert.That(save.Load("iron_echoes").CurrentMissionId, Is.EqualTo("factory_breach"));
            save.Delete("iron_echoes"); Assert.That(save.HasSave("iron_echoes"), Is.False);
        }

        [Test]
        public void DebugOutcome_ProducesBothObjectiveOutcomesWithoutChangingSnapshotData()
        {
            var map = new GridMap(2, 2);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            var victory = new CombatState(map, new[] { hero, enemy }, new CombatObjective[] { new EliminationObjective() });
            victory.ResolveDebugOutcome(true); Assert.That(victory.IsVictory, Is.True);
            var defeat = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East), new UnitState("enemy", false, new GridPosition(1, 0), Facing.West) });
            defeat.ResolveDebugOutcome(false); Assert.That(defeat.IsDefeat, Is.True);

            var objectiveMap = new GridMap(2, 2); objectiveMap.SetTile(new GridPosition(1, 1), new TileState { IsObjective = true, Durability = 6 });
            var destruction = new CombatState(objectiveMap, new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) }, new CombatObjective[] { new DestructionObjective(new[] { new GridPosition(1, 1) }) });
            destruction.ResolveDebugOutcome(true); Assert.That(destruction.IsVictory, Is.True);
        }

        [Test]
        public void ShortRun_RequiresEventSalvageAndUpgradeBeforeSecondCombat()
        {
            var run = new ShortRogueliteRun(777);
            Assert.That(run.CurrentMissionId, Is.EqualTo("dead_signal"));
            run.CompleteCombat(); Assert.That(run.Phase, Is.EqualTo(ShortRoguelitePhase.Event));
            run.ChooseEvent("field_repair"); run.ChooseSalvage("shield_cell"); run.ChooseUpgrade("calibrated_rifle");
            Assert.That(run.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(run.Choices, Is.EquivalentTo(new[] { "field_repair", "shield_cell", "calibrated_rifle" }));
        }

        [Test]
        public void ShortRun_RoundTripsAtEveryInterlude()
        {
            var run = new ShortRogueliteRun(88); run.CompleteCombat(); run.ChooseEvent("field_repair");
            var restored = ShortRogueliteRun.FromJson(run.ToJson());
            Assert.That(restored.Phase, Is.EqualTo(ShortRoguelitePhase.Salvage));
            restored.ChooseSalvage("shield_cell"); restored.ChooseUpgrade("calibrated_rifle"); restored.CompleteCombat();
            Assert.That(restored.Phase, Is.EqualTo(ShortRoguelitePhase.Complete));
        }

        [Test]
        public void MapRun_UnlocksBranchesSettlesAndRoundTrips()
        {
            var run = new RogueliteMapRun(901);
            Assert.That(run.UnlockedNodes, Does.Contain("start"));
            Assert.Throws<InvalidOperationException>(() => run.SelectNode("core_finale"));
            run.SelectNode("rail_patrol"); run.CompleteCurrentCombat();
            Assert.That(run.Level, Is.EqualTo(2)); Assert.That(run.AwaitingReward, Is.True); Assert.That(run.UnlockedNodes, Does.Contain("core_finale"));
            string reward = run.CurrentRewards[0].Id; run.ClaimReward(reward);
            var restored = RogueliteMapRun.FromJson(run.ToJson());
            Assert.That(restored.ClaimedRewards, Does.Contain(reward)); Assert.That(restored.AwaitingReward, Is.False);
        }

        [Test]
        public void MapRun_RewardChoicesAreDeterministicAndApplyToBuild()
        {
            var first = new RogueliteMapRun(77); first.SelectNode("rail_patrol"); first.CompleteCurrentCombat();
            var second = new RogueliteMapRun(77); second.SelectNode("rail_patrol"); second.CompleteCurrentCombat();
            Assert.That(first.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(second.CurrentRewards.Select(reward => reward.Id)));
            string weaponId = first.CurrentRewards.First(reward => reward.Kind == RogueliteRewardKind.Weapon).Id; first.ClaimReward(weaponId);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East); first.ApplyBuild(hero);
            Assert.That(hero.MainHand.Id, Is.EqualTo(RogueliteMapCatalog.Rewards.First(reward => reward.Id == weaponId).Weapon.Id));
        }

        [Test]
        public void MapRun_OffersMixedWeaponAndSpellRewards()
        {
            for (int seed = 1; seed < 20; seed++)
            {
                var run = new RogueliteMapRun(seed); run.SelectNode("rail_patrol"); run.CompleteCurrentCombat();
                Assert.That(run.CurrentRewards.Count, Is.EqualTo(3));
                Assert.That(run.CurrentRewards.Select(reward => reward.Kind).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            }
        }

        [Test]
        public void MapRun_RewardCardsHaveDisplayableCombatStatistics()
        {
            var run = new RogueliteMapRun(321);
            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();

            foreach (RogueliteReward reward in run.CurrentRewards)
            {
                Assert.That(reward.DisplayName, Is.Not.Empty);
                if (reward.Kind == RogueliteRewardKind.Weapon)
                {
                    Assert.That(reward.Weapon.Damage, Is.GreaterThan(0));
                    Assert.That(reward.Weapon.Range, Is.GreaterThan(0));
                }
                else
                {
                    Assert.That(reward.Spell.Damage, Is.GreaterThan(0));
                    Assert.That(reward.Spell.Range, Is.GreaterThan(0));
                }
            }
        }
    }
}
