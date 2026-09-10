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
            Assert.That(Build().ObjectTexture, Is.Not.Null, "Existing scorch presentation is retained.");
        }
    }
}
