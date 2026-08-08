using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class ArtifactRuntimeTests
    {
        [Test]
        public void Catalog_HasTwentyUniqueCompleteArtifacts()
        {
            Assert.That(ArtifactCatalog.All.Count, Is.EqualTo(20));
            Assert.That(ArtifactCatalog.All.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            Assert.That(ArtifactCatalog.All.Count(value => value.Element == "通用"), Is.EqualTo(19));
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                Assert.That(ItemCatalog.Get(artifact.Id).MaximumUses, Is.EqualTo(artifact.MaximumUses), artifact.Id);
                Assert.That(artifact.ContentSources, Is.Not.EqualTo(ArtifactContentSource.None), artifact.Id);
                Assert.That(artifact.Effects, Is.Not.Empty, artifact.Id);
            }
        }

        [Test]
        public void TrainingRange_AllArtifactsPreviewAndExecuteDeterministically()
        {
            ArtifactTrainingRangeProvider provider = new ArtifactTrainingRangeProvider();
            Assert.That(provider.Abilities.Count, Is.EqualTo(20));
            foreach (TrainingRangeAbilityEntry ability in provider.Abilities)
            {
                ITrainingRangeCase first = provider.Prepare(ability.Id), second = provider.Prepare(ability.Id);
                TrainingRangePreviewReport previewA = first.Preview(), previewB = second.Preview();
                Assert.That(previewA.CanCommit, Is.True, ability.Id + ":" + string.Join("/", previewA.Failures));
                Assert.That(previewA.Signature(), Is.EqualTo(previewB.Signature()), ability.Id);
                Assert.That(first.Execute().Signature(), Is.EqualTo(second.Execute().Signature()), ability.Id);
            }
        }

        [Test]
        public void EveryArtifact_RejectsBoundaryAndDepletedUseDeterministically()
        {
            ArtifactTrainingRangeProvider provider = new ArtifactTrainingRangeProvider();
            foreach (TrainingRangeAbilityEntry ability in provider.Abilities)
            {
                ArtifactTrainingRangeCase prepared = (ArtifactTrainingRangeCase)provider.Prepare(ability.Id);
                ArtifactTarget outside = ArtifactTarget.At(new GridPosition(-1, -1));
                ArtifactPreview first = ArtifactEngine.Preview(prepared.Battle, "hero", prepared.Artifact, outside, 0);
                ArtifactPreview second = ArtifactEngine.Preview(prepared.Battle, "hero", prepared.Artifact, outside, 0);
                Assert.That(first.CanCommit, Is.False, ability.Id);
                Assert.That(first.Failures, Does.Contain("法宝次数已耗尽"), ability.Id);
                Assert.That(first.Failures, Does.Contain("目标超出地图边界"), ability.Id);
                Assert.That(first.Signature, Is.EqualTo(second.Signature), ability.Id);
            }
        }

        [Test]
        public void AreaArtifact_ReportsFriendlyFireAndClipsSelectionAtBoundary()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard(); CombatResolver.BeginTurn(combat, "hero");
            ArtifactBattleState battle = new ArtifactBattleState(combat);
            UnitState ally = combat.GetUnit("range_ally");
            ArtifactPreview friendly = ArtifactEngine.Preview(battle, "hero", ArtifactCatalog.SeismicPlumb,
                ArtifactTarget.At(ally.Position), ArtifactCatalog.SeismicPlumb.MaximumUses);
            Assert.That(friendly.CanCommit, Is.True, string.Join("/", friendly.Failures));
            Assert.That(friendly.FriendlyFireRisk, Is.True);

            ArtifactPreview edge = ArtifactEngine.Preview(battle, "hero", ArtifactCatalog.SurveyLens,
                ArtifactTarget.At(new GridPosition(0, 0)), ArtifactCatalog.SurveyLens.MaximumUses);
            Assert.That(edge.Cells.All(combat.Map.IsInside), Is.True);
            Assert.That(edge.Cells.Count, Is.EqualTo(3));
        }

        [Test]
        public void RangedArtifact_RejectsTargetBehindHeavyCover()
        {
            GridMap map = new GridMap(5, 3);
            map.SetTile(new GridPosition(2, 1), new TileState { Cover = CoverType.Heavy, Durability = 20 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 1), Facing.West);
            CombatState combat = new CombatState(map, new[] { hero, enemy });
            CombatResolver.BeginTurn(combat, hero.Id);

            ArtifactPreview preview = ArtifactEngine.Preview(new ArtifactBattleState(combat), hero.Id,
                ArtifactCatalog.BindingFrame, ArtifactTarget.Unit(enemy.Id, enemy.Position), ArtifactCatalog.BindingFrame.MaximumUses);

            Assert.That(preview.CanCommit, Is.False);
            Assert.That(preview.Failures, Does.Contain("目标被重掩体遮挡"));
        }

        [Test]
        public void InventoryExecution_ConsumesAndRemovesLastUse()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard(); CombatResolver.BeginTurn(combat, "hero");
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.AddFirstFit(new ItemInstance("artifact-1", "G-T01", 0, 1)).Success, Is.True);
            combat.ConfigureItemInventory(inventory, new[] { "artifact-1" });
            ArtifactEngine.ExecuteInventory(new ArtifactBattleState(combat), "hero", "artifact-1",
                ArtifactTarget.Unit("range_ally", TrainingRangeScenarioFactory.AllyCell));
            Assert.That(combat.ItemInventory.Get("artifact-1"), Is.Null);
            Assert.That(combat.ItemQuickbar, Does.Not.Contain("artifact-1"));
        }

        [Test]
        public void EnemyEntryReaction_TriggersOnce()
        {
            ArtifactTrainingRangeCase trap = (ArtifactTrainingRangeCase)new ArtifactTrainingRangeProvider().Prepare("G-T10");
            trap.Execute(); UnitState enemy = trap.Combat.GetUnit("range_normal");
            CombatEffectExecutor.Execute(trap.Combat, enemy.Id, CombatEffect.Move(trap.RecommendedCell, enemy.Facing));
            Assert.That(trap.Battle.ResolveEnemyEntered("hero", enemy.Id).Steps, Is.Not.Empty);
            Assert.That(trap.Battle.ResolveEnemyEntered("hero", enemy.Id).Steps, Is.Empty);
        }

        [Test]
        public void PassiveAnchor_ConsumesOnlyWhenForcedMoveIsPrevented()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.AddFirstFit(new ItemInstance("anchor-1", "G-T13", 0, 1)).Success, Is.True);
            combat.ConfigureItemInventory(inventory, new[] { "anchor-1" });
            ArtifactBattleState battle = new ArtifactBattleState(combat);
            Assert.That(combat.ItemInventory.Get("anchor-1").RemainingUses, Is.EqualTo(1));
            Assert.That(battle.TryPreventForcedMove("hero"), Is.True);
            Assert.That(combat.ItemInventory.Get("anchor-1"), Is.Null);
            Assert.That(battle.TryPreventForcedMove("hero"), Is.False);
        }
    }
}
