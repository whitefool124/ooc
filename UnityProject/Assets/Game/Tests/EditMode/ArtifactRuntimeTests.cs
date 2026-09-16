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
            Assert.That(provider.Abilities.Count, Is.EqualTo(ArtifactCatalog.All.Count(value => ArtifactCatalog.IsCurrentlyUsable(value.Id))));
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

            GridMap edgeMap = new GridMap(5, 5);
            UnitState edgeHero = new UnitState("edge-hero", true, new GridPosition(1, 1));
            UnitState edgeEnemy = new UnitState("edge-enemy", false, new GridPosition(4, 4));
            CombatState edgeCombat = new CombatState(edgeMap, new[] { edgeHero, edgeEnemy });
            CombatResolver.BeginTurn(edgeCombat, edgeHero.Id);
            edgeMap.SetTile(new GridPosition(0, 0), new TileState { SmokeExpiresAt = edgeCombat.CurrentTime + 8 });
            ArtifactPreview edge = ArtifactEngine.Preview(new ArtifactBattleState(edgeCombat), edgeHero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(new GridPosition(0, 0)), ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(edge.CanCommit, Is.True, string.Join("/", edge.Failures));
            Assert.That(edge.Cells.All(edgeCombat.Map.IsInside), Is.True);
            Assert.That(edge.Cells.Count, Is.EqualTo(3));
        }

        [Test]
        public void RangedArtifact_RejectsTargetBehindHeavyCover()
        {
            GridMap map = new GridMap(5, 3);
            map.SetTile(new GridPosition(2, 1), new TileState { Cover = CoverType.Heavy, Durability = 20 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 1));
            CombatState combat = new CombatState(map, new[] { hero, enemy });
            CombatResolver.BeginTurn(combat, hero.Id);

            ArtifactPreview preview = ArtifactEngine.Preview(new ArtifactBattleState(combat), hero.Id,
                ArtifactCatalog.BindingFrame, ArtifactTarget.Unit(enemy.Id, enemy.Position), ArtifactCatalog.BindingFrame.MaximumUses);

            Assert.That(preview.CanCommit, Is.False);
            Assert.That(preview.Failures, Does.Contain("目标被重掩体或烟幕遮挡"));
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
            CombatEffectExecutor.Execute(trap.Combat, enemy.Id, CombatEffect.Move(trap.RecommendedCell));
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

        [Test]
        public void ShieldBalancer_TransfersPersonalShieldIntoAContestedCoverCell()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            UnitState hero = combat.GetUnit("hero");
            GridPosition cell = new GridPosition(5, 5);
            ArtifactBattleState battle = new ArtifactBattleState(combat);

            ArtifactPreview withoutShield = ArtifactEngine.Preview(battle, hero.Id, ArtifactCatalog.ShieldBalancer,
                ArtifactTarget.At(cell), ArtifactCatalog.ShieldBalancer.MaximumUses);
            Assert.That(withoutShield.CanCommit, Is.False);
            Assert.That(withoutShield.Failures, Does.Contain("自身护盾不足以承担公开代价"));

            Assert.That(combat.TryGrantRogueliteShield(hero.Id, "shield-balancer-test", 16), Is.True);
            ArtifactExecution execution = ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.ShieldBalancer,
                ArtifactTarget.At(cell), ArtifactCatalog.ShieldBalancer.MaximumUses);

            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(combat.Map.GetTile(cell).Cover, Is.EqualTo(CoverType.Light));
            Assert.That(combat.Map.GetTile(cell).Durability, Is.EqualTo(12));
            Assert.That(execution.Steps.Any(step => step.Kind == ArtifactEffectKind.ConsumeShield), Is.True);
            Assert.That(execution.Steps.Any(step => step.Kind == ArtifactEffectKind.CreateLightCover), Is.True);
        }

        [Test]
        public void CoverStamp_CreatesAnIntuitiveHeavyLineBlockingWall()
        {
            GridMap map = new GridMap(5, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 1));
            CombatState combat = new CombatState(map, new[] { hero, enemy });
            CombatResolver.BeginTurn(combat, hero.Id);
            GridPosition wall = new GridPosition(2, 1);

            ArtifactExecution execution = ArtifactEngine.Execute(new ArtifactBattleState(combat), hero.Id,
                ArtifactCatalog.CoverStamp, ArtifactTarget.At(wall), ArtifactCatalog.CoverStamp.MaximumUses);

            TileState tile = combat.Map.GetTile(wall);
            Assert.That(tile.Cover, Is.EqualTo(CoverType.Heavy));
            Assert.That(tile.Durability, Is.EqualTo(TileState.HeavyDurability));
            Assert.That(combat.Map.IsBlocked(wall), Is.True);
            Assert.That(combat.HasLineOfSight(hero.Position, enemy.Position), Is.False);
            Assert.That(execution.Steps.Single().Kind, Is.EqualTo(ArtifactEffectKind.CreateHeavyCover));
        }

        [Test]
        public void SurveyLens_ExposesAShieldedEnemyAndPreventsImmediateReshielding()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            UnitState enemy = combat.GetUnit("range_normal");
            Assert.That(combat.TryGrantRogueliteShield(enemy.Id, "test-maintenance", 12), Is.True);

            ArtifactExecution execution = ArtifactEngine.Execute(new ArtifactBattleState(combat), "hero",
                ArtifactCatalog.SurveyLens, ArtifactTarget.Unit(enemy.Id, enemy.Position), ArtifactCatalog.SurveyLens.MaximumUses);

            Assert.That(enemy.Shield, Is.Zero);
            Assert.That(enemy.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(combat.TryGrantRogueliteShield(enemy.Id, "test-maintenance-2", 12), Is.False);
            Assert.That(execution.Steps.Any(step => step.Kind == ArtifactEffectKind.ApplyStatus && step.Applied > 0), Is.True);
        }

        [Test]
        public void NullVeil_CreatesSymmetricTemporaryLineBlockThatCondenserCanClear()
        {
            GridMap map = new GridMap(7, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(6, 1));
            hero.Equip(CombatCatalog.Rifle, null, null);
            enemy.Equip(EnemyAbilityCatalog.HeavyCrossbow, null, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy });
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            ArtifactBattleState battle = new ArtifactBattleState(combat);
            GridPosition smokeCell = new GridPosition(3, 1);

            Assert.That(combat.HasLineOfSight(hero.Position, enemy.Position), Is.True);
            ArtifactExecution veil = ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.NullVeil,
                ArtifactTarget.At(smokeCell), ArtifactCatalog.NullVeil.MaximumUses);

            Assert.That(combat.Map.GetTile(smokeCell).SmokeExpiresAt, Is.EqualTo(combat.CurrentTime + 8));
            Assert.That(veil.Steps.Single().Kind, Is.EqualTo(ArtifactEffectKind.CreateSmoke));
            Assert.That(CombatResolver.PreviewAttack(combat, hero.Id, enemy.Id, false).HasLineOfSight, Is.False);
            Assert.That(CombatResolver.PreviewAttack(combat, enemy.Id, hero.Id, false).HasLineOfSight, Is.False);

            ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.HazardCondenser,
                ArtifactTarget.At(smokeCell), ArtifactCatalog.HazardCondenser.MaximumUses);
            Assert.That(combat.Map.GetTile(smokeCell).SmokeExpiresAt, Is.Zero);
            Assert.That(combat.HasLineOfSight(hero.Position, enemy.Position), Is.True);
        }

        [Test]
        public void DecoyLantern_ChangesPublicEnemyIntentAndTakesRealObjectDamage()
        {
            GridMap map = new GridMap(7, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 1));
            enemy.Equip(EnemyAbilityCatalog.HeavyCrossbow, null, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy });
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            ArtifactBattleState battle = new ArtifactBattleState(combat);
            GridPosition lantern = new GridPosition(3, 1);
            ArtifactEngine.Execute(battle, hero.Id, ArtifactCatalog.DecoyLantern,
                ArtifactTarget.At(lantern), ArtifactCatalog.DecoyLantern.MaximumUses);

            Assert.That(combat.Map.GetTile(lantern).IsDecoy, Is.True);
            Assert.That(combat.Map.IsBlocked(lantern), Is.True);
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand command = plans.GetExecutionCommand(combat, enemy, hero);
            EnemyIntentPresentation intent = plans.GetPublicIntent(combat, enemy, hero);
            Assert.That(command.Type, Is.EqualTo(CombatCommandType.Interact));
            Assert.That(command.Destination, Is.EqualTo(lantern));
            Assert.That(intent.ActionName, Is.EqualTo("破坏诱导灯"));
            Assert.That(intent.TargetSummary, Does.Contain("诱导灯"));
            Assert.That(intent.ResultSummary, Does.Contain("3 耐久伤害"));

            CombatResolver.BeginTurn(combat, enemy.Id);
            CombatResolver.Resolve(combat, command);
            Assert.That(combat.Map.GetTile(lantern).Durability, Is.EqualTo(9));

            CombatResolver.BeginTurn(combat, hero.Id);
            battle.BeginUnitTurn(hero.Id);
            Assert.That(combat.Map.GetTile(lantern).IsDecoy, Is.False);
            Assert.That(combat.Map.IsBlocked(lantern), Is.False);
        }

        [Test]
        public void SeismicPlumb_PushesEveryAdjacentUnitOutwardWithoutChangingTimeline()
        {
            GridMap map = new GridMap(7, 7);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 3));
            UnitState north = new UnitState("north", false, new GridPosition(3, 2));
            UnitState east = new UnitState("east", false, new GridPosition(4, 3));
            UnitState ally = new UnitState("ally", true, new GridPosition(3, 4));
            CombatState combat = new CombatState(map, new[] { hero, north, east, ally });
            CombatResolver.BeginTurn(combat, hero.Id);
            GridPosition center = new GridPosition(3, 3);
            int northAction = north.ActionValue, eastAction = east.ActionValue, allyAction = ally.ActionValue;

            ArtifactBattleState battle = new ArtifactBattleState(combat);
            ArtifactPreview preview = ArtifactEngine.Preview(battle, hero.Id,
                ArtifactCatalog.SeismicPlumb, ArtifactTarget.At(center), ArtifactCatalog.SeismicPlumb.MaximumUses);
            Assert.That(preview.CanCommit, Is.True, string.Join("/", preview.Failures));
            Assert.That(preview.FriendlyFireRisk, Is.True);
            ArtifactExecution execution = ArtifactEngine.Execute(battle, hero.Id,
                ArtifactCatalog.SeismicPlumb, ArtifactTarget.At(center), ArtifactCatalog.SeismicPlumb.MaximumUses);

            Assert.That(north.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(east.Position, Is.EqualTo(new GridPosition(5, 3)));
            Assert.That(ally.Position, Is.EqualTo(new GridPosition(3, 5)));
            Assert.That((north.ActionValue, east.ActionValue, ally.ActionValue), Is.EqualTo((northAction, eastAction, allyAction)));
            Assert.That(execution.Steps.Count(step => step.Kind == ArtifactEffectKind.ForceMoveFromCell && step.Applied == 1), Is.EqualTo(3));
        }

        [Test]
        public void ForcedMovement_ResolvesTheFinalFireOrShallowWaterLanding()
        {
            GridMap fireMap = new GridMap(7, 3);
            UnitState fireHero = new UnitState("fire-hero", true, new GridPosition(1, 1));
            UnitState fireEnemy = new UnitState("fire-enemy", false, new GridPosition(5, 1));
            CombatState fireCombat = new CombatState(fireMap, new[] { fireHero, fireEnemy });
            fireCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            var fireRuntime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(fireCombat,
                OCC.Combat.Roguelite.RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            fireCombat.AttachRogueSpellRuntime(fireRuntime);
            CombatResolver.BeginTurn(fireCombat, fireHero.Id);
            fireRuntime.FireBattle.CreateOrRefreshFireground(new GridPosition(3, 1), 8, 2, "landing-test");
            int vitalityBefore = fireEnemy.Health + fireEnemy.Shield;

            ArtifactEngine.Execute(new ArtifactBattleState(fireCombat), fireHero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(fireEnemy.Id, fireEnemy.Position), ArtifactCatalog.RelayCompass.MaximumUses);

            Assert.That(fireEnemy.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(vitalityBefore - fireEnemy.Health - fireEnemy.Shield, Is.EqualTo(8),
                "A visible pull onto fire must resolve the same entry damage as ordinary movement.");

            GridMap waterMap = new GridMap(7, 3);
            waterMap.SetTile(new GridPosition(3, 1), new TileState { IsWater = true });
            UnitState waterHero = new UnitState("water-hero", true, new GridPosition(1, 1));
            UnitState waterEnemy = new UnitState("water-enemy", false, new GridPosition(5, 1));
            waterEnemy.ApplyStatus(StatusType.Burning, 2, 8);
            CombatState waterCombat = new CombatState(waterMap, new[] { waterHero, waterEnemy });
            waterCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            var waterRuntime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(waterCombat,
                OCC.Combat.Roguelite.RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            waterCombat.AttachRogueSpellRuntime(waterRuntime);
            CombatResolver.BeginTurn(waterCombat, waterHero.Id);

            ArtifactEngine.Execute(new ArtifactBattleState(waterCombat), waterHero.Id, ArtifactCatalog.RelayCompass,
                ArtifactTarget.Unit(waterEnemy.Id, waterEnemy.Position), ArtifactCatalog.RelayCompass.MaximumUses);

            Assert.That(waterEnemy.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(waterEnemy.HasStatus(StatusType.Burning), Is.False,
                "A forced landing in shallow water must extinguish burning by the same common-sense rule.");
        }
    }
}
