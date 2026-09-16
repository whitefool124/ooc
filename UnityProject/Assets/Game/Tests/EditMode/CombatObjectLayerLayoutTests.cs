using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatObjectLayerLayoutTests
    {
        [TestCase(64f, 1f)]
        [TestCase(64f, .5f)]
        [TestCase(128f, 1f)]
        [TestCase(128f, .5f)]
        public void EveryScreenRowSamplesExactlyTheOriginalSourceRow(float cellSize, float screenScale)
        {
            var split = new CombatObjectLayerLayout(cellSize, 32, CombatObjectLayerLayout.LampVineFrontRows);
            float screenSize = cellSize * screenScale;
            Assert.That(split.BackRect.yMax, Is.EqualTo(split.FrontRect.y));
            Assert.That(split.FrontRect.yMax, Is.EqualTo(cellSize));
            Assert.That(split.BackUv.yMin, Is.EqualTo(split.FrontUv.yMax));
            Assert.That(split.BackRect.height * screenScale % 1f, Is.Zero);
            Assert.That(split.FrontRect.height * screenScale % 1f, Is.Zero);
            for (int y = 0; y < screenSize; y++)
            {
                float sample = (y + .5f) / screenScale;
                Rect rect = sample < split.FrontRect.y ? split.BackRect : split.FrontRect;
                Rect uv = sample < split.FrontRect.y ? split.BackUv : split.FrontUv;
                float v = uv.yMax - (sample - rect.y) / rect.height * uv.height;
                int sampled = Mathf.FloorToInt(v * 32);
                int original = Mathf.FloorToInt((1f - (y + .5f) / screenSize) * 32);
                Assert.That(sampled, Is.EqualTo(original), "Source texel differs at screen row " + y);
            }
        }

        [Test]
        public void FrontLeavesCoverSameRowButRemainBehindNearerActors()
        {
            Vector2 front = new Vector2(64, 128);
            Assert.That(CombatObjectLayerLayout.CompareDepth(front, true, new Vector2(128, 128), false), Is.GreaterThan(0));
            Assert.That(CombatObjectLayerLayout.CompareDepth(front, true, new Vector2(64, 126), false), Is.GreaterThan(0));
            Assert.That(CombatObjectLayerLayout.CompareDepth(front, true, new Vector2(64, 130), false), Is.LessThan(0));
            Assert.That(CombatObjectLayerLayout.CompareDepth(front, false, new Vector2(128, 128), false), Is.LessThan(0));
        }

        [Test]
        public void OrdinaryObjectAndMissingTextureKeepFullUnslicedImage()
        {
            var ordinary = new CombatObjectLayerLayout(64, 32, 0);
            var missing = new CombatObjectLayerLayout(64, 0, 10);
            Assert.That(ordinary.HasFront, Is.False);
            Assert.That(missing.HasFront, Is.False);
            Assert.That(ordinary.BackUv, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(ordinary.BackRect, Is.EqualTo(new Rect(0, 0, 64, 64)));
        }

        [Test]
        public void GroundAttachmentsAreExplicitlySeparatedFromRaisedStructures()
        {
            foreach (string level in new[] { RainLanternCourtRuntime.LevelId, "signal_hub", "gatehouse", "core_finale" })
            {
                int ground = 0;
                foreach (var placement in AcademyBattlefieldLayoutCatalog.VisualModules(level))
                {
                    if (placement.IsGroundAttachment) { ground++; Assert.That(placement.AssetId, Does.StartWith("academy_floor_")); }
                    else Assert.That(placement.AssetId, Does.Not.StartWith("academy_floor_"));
                }
                Assert.That(ground, Is.EqualTo(4));
                foreach (var structure in AcademyBattlefieldLayoutCatalog.Structures(level)) Assert.That(structure.IsGroundAttachment, Is.False);
            }
        }

        [Test]
        public void TerrainPresenterRemovesForegroundWhenVineIsBurned()
        {
            CombatState state = FirstRegionLevelBuilder.Build(RainLanternCourtRuntime.LevelId).State;
            var assets = new CombatFormalVisualAssets(); assets.LoadRuntime();
            var presenter = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets);
            var position = new GridPosition(4, 2);
            var selection = new CombatSelectionController();
            BattlefieldCellPresentation Build() => presenter.Build(state, FirstRegionLevelCatalog.For(RainLanternCourtRuntime.LevelId),
                null, selection, false, null, position, _ => null, (_, __) => null, _ => null, _ => null);
            Assert.That(Build().ObjectForegroundRows, Is.EqualTo(10));
            state.Map.GetTile(position).IsLampVine = false;
            state.Map.GetTile(position).IsScorched = true;
            Assert.That(Build().ObjectForegroundRows, Is.Zero);
            Assert.That(Build().ObjectTexture, Is.Not.Null);
            Assert.That(Build().ObjectTexture.name, Is.EqualTo("academy_scorched_lamp_vine"));
        }

        [Test]
        public void GreenhouseCrystalUsesDedicatedWideVisualWithoutChangingOwningCell()
        {
            CombatState state = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id).State;
            var assets = new CombatFormalVisualAssets(); assets.LoadRuntime();
            var presenter = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets);
            var position = new GridPosition(8, 2);
            var selection = new CombatSelectionController();
            BattlefieldCellPresentation model = presenter.Build(state, FirstRegionLevelCatalog.GreenhouseCollectionRoom,
                null, selection, false, null, position, _ => null, (_, __) => null, _ => null, _ => null);

            Assert.That(model.ObjectTexture, Is.Not.Null);
            Assert.That(model.ObjectTexture.name, Is.EqualTo("academy_aether_crystal_intact"));
            Assert.That(model.ObjectTexture.width, Is.EqualTo(96));
            Assert.That(model.ObjectTexture.height, Is.EqualTo(64));
            Assert.That(model.Position, Is.EqualTo(position), "The wide visual remains owned by its single gameplay cell.");

            var layout = new CombatObjectLayerLayout(64f, model.ObjectTexture.width, model.ObjectTexture.height, 0);
            Assert.That(layout.BackRect.x, Is.EqualTo(-64f));
            Assert.That(layout.BackRect.yMax, Is.EqualTo(64f), "The visual is bottom-aligned to the owning cell.");

            state.Map.GetTile(position).Durability = 8;
            model = presenter.Build(state, FirstRegionLevelCatalog.GreenhouseCollectionRoom,
                null, selection, false, null, position, _ => null, (_, __) => null, _ => null, _ => null);
            Assert.That(model.ObjectTexture.name, Is.EqualTo("academy_aether_crystal_damaged"));
        }

        [Test]
        public void ElitePressureCrystalUsesItsOwnIntactAndDamagedStates()
        {
            CombatState state = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.ThreeMaterialPressure.Id).State;
            var assets = new CombatFormalVisualAssets(); assets.LoadRuntime();
            var presenter = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets);
            var position = new GridPosition(6, 4);
            var selection = new CombatSelectionController();
            BattlefieldCellPresentation Build() => presenter.Build(state, FirstRegionLevelCatalog.ThreeMaterialPressure,
                null, selection, false, null, position, _ => null, (_, __) => null, _ => null, _ => null);

            Assert.That(Build().ObjectTexture.name, Is.EqualTo("academy_pressure_crystal_intact"));
            Assert.That(Build().ObjectLabel, Is.EqualTo("稳压晶簇"));
            state.Map.GetTile(position).Durability = 16;
            Assert.That(Build().ObjectTexture.name, Is.EqualTo("academy_pressure_crystal_damaged"));
        }

        [Test]
        public void DeployedDecoyUsesDedicatedSingleCellBattlefieldVisual()
        {
            CombatState state = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id).State;
            var assets = new CombatFormalVisualAssets(); assets.LoadRuntime();
            var presenter = new CombatBattlefieldCellPresenter(new BattlefieldPresentationAdapter(), assets);
            var position = new GridPosition(1, 1);
            state.Map.SetTile(position, new TileState { IsDecoy = true, IsDevice = true, Durability = 12 });
            var selection = new CombatSelectionController();

            BattlefieldCellPresentation model = presenter.Build(state, FirstRegionLevelCatalog.GreenhouseCollectionRoom,
                null, selection, false, null, position, _ => null, (_, __) => null, _ => null, _ => null);

            Assert.That(model.ObjectTexture, Is.Not.Null);
            Assert.That(model.ObjectTexture.name, Is.EqualTo("academy_decoy_lantern_active"));
            Assert.That(model.ObjectTexture.width, Is.EqualTo(32));
            Assert.That(model.ObjectTexture.height, Is.EqualTo(32));
            Assert.That(model.ObjectForegroundRows, Is.Zero);
            Assert.That(model.ObjectLabel, Is.EqualTo("诱导灯"));
        }
    }
}
