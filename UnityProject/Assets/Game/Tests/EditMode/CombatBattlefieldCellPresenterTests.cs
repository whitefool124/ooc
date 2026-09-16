using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatBattlefieldCellPresenterTests
    {
        [Test]
        public void Build_CombinesFormalAssetsAndReadOnlyHeroState()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            hero.DisplayName = "阿斯特拉";
            CombatState state = new CombatState(new GridMap(12, 9), new[] { hero });
            CombatSelectionController selection = new CombatSelectionController();
            CombatBattlefieldCellPresenter presenter = new CombatBattlefieldCellPresenter(
                new BattlefieldPresentationAdapter(), assets);

            BattlefieldCellPresentation cell = presenter.Build(state, null, new FireBattleState(state),
                selection, false, null, hero.Position, _ => null, (_, __) => null, _ => null, _ => null);

            Assert.That(cell.Unit, Is.SameAs(hero));
            Assert.That(cell.UnitTexture, Is.Not.Null);
            Assert.That(cell.FloorTexture.name, Is.EqualTo("academy_ground_macro_court_3x3"));
            Assert.That(cell.FloorUv, Is.EqualTo(new UnityEngine.Rect(1f / 3f, 1f / 3f, 1f / 3f, 1f / 3f)));
            Assert.That(cell.TerrainBoundaryTexture, Is.Null);
            Assert.That(cell.HoverText, Does.Contain("生命当前 18　上限 18"));
        }

        [Test]
        public void EighthSpell_ShowsItsInvalidInnerRangeAndValidOuterBand()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState adjacent = new UnitState("adjacent", false, new GridPosition(1, 0));
            UnitState band = new UnitState("band", false, new GridPosition(2, 0));
            hero.ConfigureMana(99);
            CombatState state = new CombatState(new GridMap(5, 2), new[] { hero, adjacent, band });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, hero.Id);
            FireBattleState fireBattle = new FireBattleState(state);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R09");
            CombatSelectionController selection = new CombatSelectionController();
            selection.SelectAction("技能8");
            CombatBattlefieldCellPresenter presenter = new CombatBattlefieldCellPresenter(
                new BattlefieldPresentationAdapter(), assets);

            FireSpellPreview PreviewAt(FireSpellDefinition definition, GridPosition position)
            {
                UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.Position == position);
                FireSpellTarget target = unit == null ? FireSpellTarget.At(position, CardinalDirection.East) : FireSpellTarget.Unit(unit.Id);
                return FireSpellEngine.Preview(fireBattle, hero.Id, definition, target);
            }

            BattlefieldCellPresentation inner = presenter.Build(state, null, fireBattle, selection, false, null,
                adjacent.Position, _ => spell, PreviewAt, _ => null, _ => null);
            BattlefieldCellPresentation valid = presenter.Build(state, null, fireBattle, selection, false, null,
                band.Position, _ => spell, PreviewAt, _ => null, _ => null);

            Assert.That(inner.SkillOverlayTexture.name, Is.EqualTo("unreachable"));
            Assert.That(valid.SkillOverlayTexture.name, Is.EqualTo("attack_range"));
        }

        [Test]
        public void HoveredAreaSpell_ShowsEveryAffectedCellBeforeCasting()
        {
            CombatFormalVisualAssets assets = new CombatFormalVisualAssets();
            assets.LoadRuntime();
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState target = new UnitState("target", false, new GridPosition(3, 1));
            hero.ConfigureMana(99);
            CombatState state = new CombatState(new GridMap(5, 3), new[] { hero, target });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state, hero.Id);
            FireBattleState fireBattle = new FireBattleState(state);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R06");
            CombatSelectionController selection = new CombatSelectionController();
            selection.SelectAction("技能1");
            selection.SetPreviewPosition(target.Position);
            CombatBattlefieldCellPresenter presenter = new CombatBattlefieldCellPresenter(
                new BattlefieldPresentationAdapter(), assets);

            FireSpellPreview PreviewAt(FireSpellDefinition definition, GridPosition position)
            {
                UnitState unit = state.Units.Values.FirstOrDefault(candidate => candidate.Position == position);
                FireSpellTarget spellTarget = unit == null
                    ? FireSpellTarget.At(position, CardinalDirection.East)
                    : FireSpellTarget.Unit(unit.Id, CardinalDirection.East);
                return FireSpellEngine.Preview(fireBattle, hero.Id, definition, spellTarget);
            }

            BattlefieldCellPresentation middle = presenter.Build(state, null, fireBattle, selection, false, null,
                new GridPosition(1, 1), _ => spell, PreviewAt, _ => null, _ => null);
            BattlefieldCellPresentation endpoint = presenter.Build(state, null, fireBattle, selection, false, null,
                target.Position, _ => spell, PreviewAt, _ => null, _ => null);

            Assert.That(middle.SkillOverlayTexture.name, Is.EqualTo("attack_range"));
            Assert.That(endpoint.SkillOverlayTexture.name, Is.EqualTo("attack_range"));
        }

        [Test]
        public void FloorKey_UsesContinuousAcademyGroundMacroAndUvSlices()
        {
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 2, 0), Is.EqualTo("academy_ground_macro_court_3x3"));
            Assert.That(CombatBattlefieldCellPresenter.FloorUv(5, 4), Is.EqualTo(new UnityEngine.Rect(2f / 3f, 1f / 3f, 1f / 3f, 1f / 3f)));
        }

        [Test]
        public void ObjectHover_ExplainsCoverInOneSentenceAndOmitsEmptyCategory()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero });
            GridPosition lightPosition = new GridPosition(1, 0);
            GridPosition heavyPosition = new GridPosition(2, 0);
            state.Map.SetTile(lightPosition, new TileState { Cover = CoverType.Light, Durability = 4 });
            state.Map.SetTile(heavyPosition, new TileState { Cover = CoverType.Heavy, Durability = 7 });

            string light = CombatBattlefieldCellPresenter.BuildObjectHover(state,
                state.Map.GetTile(lightPosition), lightPosition);
            string heavy = CombatBattlefieldCellPresenter.BuildObjectHover(state,
                state.Map.GetTile(heavyPosition), heavyPosition);

            Assert.That(light, Does.StartWith("轻掩体"));
            Assert.That(light, Does.Contain("耐久 4"));
            Assert.That(light, Does.Contain("2 护盾"));
            Assert.That(light.Count(value => value == '。'), Is.EqualTo(1));
            Assert.That(heavy, Does.StartWith("重掩体"));
            Assert.That(heavy, Does.Contain("阻挡移动与视线"));
            Assert.That(heavy, Does.Contain("4 护盾"));

            state.Map.GetTile(lightPosition).Durability = 0;
            Assert.That(CombatBattlefieldCellPresenter.BuildObjectHover(state,
                state.Map.GetTile(lightPosition), lightPosition), Does.Contain("已失去防护效果"));
            Assert.That(CombatBattlefieldCellPresenter.BuildObjectHover(state,
                state.Map.GetTile(new GridPosition(0, 1)), new GridPosition(0, 1)), Is.Empty);
        }

        [Test]
        public void ModularFloor_UsesReusableRoadCopingAndMaterialFamilies()
        {
            FirstRegionLevelDefinition rail = FirstRegionLevelCatalog.For("rail_patrol");
            FirstRegionLevelDefinition depot = FirstRegionLevelCatalog.For("depot_wreck");

            Assert.That(CombatBattlefieldCellPresenter.FloorKey(rail, 9, 0, 0), Does.StartWith("academy_ground_macro_earth"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(rail, 9, 5, 4), Does.StartWith("academy_ground_macro_road"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(depot, 9, 0, 0), Does.StartWith("academy_ground_macro_ruin"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(depot, 9, 5, 0), Does.StartWith("academy_ground_macro_road"));
            Assert.That(CombatBattlefieldCellPresenter.FloorRotationDegrees(depot, 5, 0), Is.Zero);
        }

        [Test]
        public void GroundMacros_KeepMaterialFamiliesAndNeverRotate()
        {
            FirstRegionLevelDefinition rail = FirstRegionLevelCatalog.For("rail_patrol");
            FirstRegionLevelDefinition relay = FirstRegionLevelCatalog.For("relay_raid");
            FirstRegionLevelDefinition depot = FirstRegionLevelCatalog.For("depot_wreck");
            FirstRegionLevelDefinition elite = FirstRegionLevelCatalog.For("elite_foundry");

            Assert.That(CombatBattlefieldCellPresenter.FloorKey(rail, 9, 0, 0), Does.StartWith("academy_ground_macro_earth"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(relay, 9, 11, 0), Does.StartWith("academy_ground_macro_earth"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(depot, 9, 0, 0), Does.StartWith("academy_ground_macro_ruin"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(elite, 9, 0, 0), Does.StartWith("academy_ground_macro_ruin"));
            Assert.That(CombatBattlefieldCellPresenter.FloorRotationDegrees(elite, 0, 0), Is.Zero);
        }

        [Test]
        public void NineMaps_HaveDistinctReusableFloorSignatures()
        {
            var signatures = new System.Collections.Generic.HashSet<string>();
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                var cells = new System.Collections.Generic.List<string>();
                for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                    cells.Add(CombatBattlefieldCellPresenter.FloorKey(level, level.Height, x, y) + "@" +
                        CombatBattlefieldCellPresenter.FloorRotationDegrees(level, x, y));
                Assert.That(signatures.Add(string.Join("|", cells)), Is.True, level.Id);
            }

            Assert.That(signatures.Count, Is.EqualTo(9));
        }

        [TestCase(1, "academy_curb_edge", 0)]
        [TestCase(2, "academy_curb_edge", 1)]
        [TestCase(3, "academy_curb_corner", 0)]
        [TestCase(6, "academy_curb_corner", 1)]
        [TestCase(5, "academy_curb_opposite", 0)]
        [TestCase(10, "academy_curb_opposite", 1)]
        [TestCase(7, "academy_curb_three", 0)]
        [TestCase(14, "academy_curb_three", 1)]
        [TestCase(15, "academy_curb_enclosed", 0)]
        public void BoundaryOverlay_SelectsReusableAdjacencyAssetAndRotation(int mask, string assetId, int turns)
        {
            Assert.That(AcademyBattlefieldLayoutCatalog.BoundaryOverlayForMask(mask, out int actualTurns),
                Is.EqualTo(assetId));
            Assert.That(actualTurns, Is.EqualTo(turns));
        }

        [Test]
        public void BoundaryOverlay_IsDisabledForSelfContainedFloorTiles()
        {
            FirstRegionLevelDefinition rail = FirstRegionLevelCatalog.For("rail_patrol");

            Assert.That(AcademyBattlefieldLayoutCatalog.BoundaryOverlay(rail, 0, 0, out _), Is.Null);
            Assert.That(AcademyBattlefieldLayoutCatalog.BoundaryOverlay(rail, 5, 4, out _), Is.Null);
            Assert.That(AcademyBattlefieldLayoutCatalog.BoundaryOverlay(rail, 4, 8, out int turns), Is.Null);
            Assert.That(turns, Is.Zero);
        }

        [Test]
        public void Structures_RecomposeSharedWallEndCornerAndStairModules()
        {
            AcademyStructurePlacement[] full = AcademyBattlefieldLayoutCatalog.Structures("rail_patrol");
            AcademyStructurePlacement[] broken = AcademyBattlefieldLayoutCatalog.Structures("relay_raid");

            Assert.That(full.Length, Is.EqualTo(11));
            Assert.That(full.Count(value => value.AssetId == "academy_wall_straight"), Is.EqualTo(8));
            Assert.That(full.Count(value => value.AssetId == "academy_wall_end_w"), Is.EqualTo(1));
            Assert.That(full.Count(value => value.AssetId == "academy_wall_end_e"), Is.EqualTo(1));
            Assert.That(full.Single(value => value.AssetId == "academy_stairs_2x1").WidthCells, Is.EqualTo(2));
            Assert.That(broken.Any(value => value.AssetId == "academy_stairs_2x1"), Is.True);
            Assert.That(broken.All(value => value.TopY == 8), Is.True);
            Assert.That(full.Concat(broken).All(value => value.QuarterTurns == 0), Is.True);
        }

        [Test]
        public void VisualModules_AddReusableDecorationWithoutChangingLogicalLevelDefinitions()
        {
            FirstRegionLevelDefinition weak = FirstRegionLevelCatalog.For("rail_patrol");
            int terrainCount = weak.Terrain.Count;
            AcademyStructurePlacement[] modules = AcademyBattlefieldLayoutCatalog.VisualModules(weak.Id);

            Assert.That(modules.Count(value => value.AssetId.StartsWith("academy_floor_")), Is.EqualTo(4));
            Assert.That(modules.All(value => value.QuarterTurns == 0), Is.True);
            Assert.That(weak.Terrain.Count, Is.EqualTo(terrainCount));
            Assert.That(AcademyBattlefieldLayoutCatalog.Structures("signal_hub")
                .Single(value => value.AssetId == "academy_aether_pump_2x2").WidthCells, Is.EqualTo(2));
        }

        [Test]
        public void CoverVariant_OnlySelectsVisualAssetForExistingCoverKind()
        {
            GridPosition position = new GridPosition(3, 2);
            Assert.That(AcademyBattlefieldLayoutCatalog.CoverVariant("rail_patrol", position, CoverType.Light),
                Does.StartWith("academy_prop_"));
            Assert.That(AcademyBattlefieldLayoutCatalog.CoverVariant("rail_patrol", position, CoverType.Heavy),
                Does.StartWith("academy_prop_"));
            Assert.That(AcademyBattlefieldLayoutCatalog.CoverVariant("rail_patrol", position, CoverType.None), Is.Null);
            Assert.That(AcademyBattlefieldLayoutCatalog.CoverVisualAssetIds(), Has.Length.EqualTo(20));
        }

        [Test]
        public void ExpandedModules_ReuseExistingVisualSlotsWithoutRotation()
        {
            Assert.That(AcademyBattlefieldLayoutCatalog.Structures("signal_hub")
                .Single(value => value.AssetId == "academy_aether_pump_2x2").WidthCells, Is.EqualTo(2));
            Assert.That(AcademyBattlefieldLayoutCatalog.Structures("elite_foundry")
                .Where(value => value.AssetId.EndsWith("_2x1")).All(value => value.WidthCells == 2), Is.True);
            Assert.That(AcademyBattlefieldLayoutCatalog.VisualModules("core_finale")
                .All(value => value.QuarterTurns == 0), Is.True);
        }

        [Test]
        public void WeaponForecast_ReturnsInitializedFireContextWithoutMutatingTarget()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            int health = enemy.Health;

            CombatTargetForecastResult result = new CombatTargetForecastService().Evaluate(
                new BattlefieldPresentationAdapter(), state, null, "攻击", enemy, null, null, 0);

            Assert.That(result.FireBattle, Is.Not.Null);
            Assert.That(result.Forecast, Is.Not.Null);
            Assert.That(result.Forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(enemy.Health, Is.EqualTo(health));
        }

        [Test]
        public void ArtifactForecast_UsesRealResolverWithoutMutatingTarget()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            int health = enemy.Health;
            ArtifactDefinition artifact = ArtifactCatalog.Get("G-T19");

            CombatTargetForecastResult result = new CombatTargetForecastService().Evaluate(
                new BattlefieldPresentationAdapter(), state, null, "技能1", enemy, null,
                artifact, artifact.MaximumUses);

            Assert.That(result.Forecast, Is.Not.Null);
            Assert.That(result.Forecast.TotalDamage, Is.GreaterThan(0));
            Assert.That(result.Forecast.WillDefeat, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(health));
        }
    }
}
