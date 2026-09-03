using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    /// <summary>Red-light contracts for M-A18. These stay failing until native, independently authored assets pass offline QA.</summary>
    public sealed class MA18AcademyCombatArtContractTests
    {
        private const string AcademyRoot = "Art/FormalAcademyCombat32/";
        private const string UnitRoot = "Art/FormalUnits64/";
        private const string AnimationRoot = "Art/FormalEnemyAnimations64/";
        private const string VfxRoot = "Art/FormalVfx32/";
        private const string OverlayRoot = "Art/FormalTacticalOverlays32/";

        private static readonly string[] TerrainIds =
        {
            "academy_stone_road_a", "academy_stone_road_b", "academy_stone_road_c", "academy_stone_road_d",
            "academy_courtyard_a", "academy_courtyard_b", "academy_courtyard_c", "academy_courtyard_d",
            "academy_ruins_a", "academy_ruins_b", "academy_ruins_c", "academy_ruins_d",
            "academy_aether_inlay_a", "academy_aether_inlay_b", "academy_aether_inlay_c", "academy_aether_inlay_d",
            "academy_packed_earth_a", "academy_packed_earth_b", "academy_packed_earth_c",
            "academy_grass_edge_n", "academy_grass_edge_e", "academy_grass_edge_s", "academy_grass_edge_w",
            "academy_light_stone_bench_intact", "academy_light_stone_bench_damaged", "academy_light_stone_bench_rubble",
            "academy_light_planter_intact", "academy_light_planter_damaged", "academy_light_planter_rubble",
            "academy_heavy_archive_stack_intact", "academy_heavy_archive_stack_damaged", "academy_heavy_archive_stack_rubble",
            "academy_heavy_masonry_screen_intact", "academy_heavy_masonry_screen_damaged", "academy_heavy_masonry_screen_rubble",
            "academy_aether_pillar_intact", "academy_aether_pillar_damaged", "academy_aether_pillar_rubble",
            "academy_seal_plinth_intact", "academy_seal_plinth_damaged", "academy_seal_plinth_rubble",
            "academy_loot_chest_closed", "academy_loot_chest_open", "academy_loot_chest_empty",
            "academy_aether_line_straight", "academy_aether_line_corner", "academy_aether_line_tee", "academy_aether_line_cross"
        };

        private static readonly string[] AnimationIds =
        {
            "sigil_mauler", "barrier_mender", "tether_hound", "shieldguard", "pyromancer", "raider",
            "elite_vanguard", "stone_snare", "lantern_revealer", "rune_arbalist", "core_overseer", "purifier_overseer"
        };

        private static readonly string[] FireVfxIds =
        {
            "fire_cast", "fire_projectile", "fire_impact", "fire_melee_arc", "fire_attachment", "fire_spray",
            "fire_line", "fire_cross_blast", "fire_detonate", "fire_burning_ground", "fire_wall", "fire_absorb",
            "fire_break_stance", "fire_overlimit"
        };

        [Test]
        public void P0AcademyCourtyard_IsAnIndependent32PixelPointSprite()
        {
            AssertSprite(AcademyRoot + "academy_courtyard_a", new Vector2(32, 32), 32f, false);
        }

        [Test]
        public void AcademyCourtyard_MaterialScaleAndAllVariantEdgesAreSeamless()
        {
            string[] ids = { "academy_courtyard_a", "academy_courtyard_b", "academy_courtyard_c", "academy_courtyard_d" };
            var textures = ids.ToDictionary(id => id, LoadPng);
            try
            {
                foreach (KeyValuePair<string, Texture2D> left in textures)
                foreach (KeyValuePair<string, Texture2D> right in textures)
                {
                    for (int offset = 0; offset < 32; offset++)
                    {
                        Assert.That(left.Value.GetPixel(31, offset), Is.EqualTo(right.Value.GetPixel(0, offset)),
                            left.Key + " -> " + right.Key + " horizontal edge " + offset);
                        Assert.That(left.Value.GetPixel(offset, 31), Is.EqualTo(right.Value.GetPixel(offset, 0)),
                            left.Key + " -> " + right.Key + " vertical edge " + offset);
                    }
                }

                foreach (KeyValuePair<string, Texture2D> pair in textures)
                {
                    Texture2D texture = pair.Value;
                    int[] fullCourseRows = Enumerable.Range(0, 32)
                        .Where(y => Enumerable.Range(1, 31).All(x => texture.GetPixel(x, y) == texture.GetPixel(0, y)))
                        .ToArray();
                    // Texture2D.GetPixel uses a bottom-left origin, so source rows 8/16/24
                    // are observed as zero-based runtime rows 23/15/7.
                    Assert.That(fullCourseRows, Is.EqualTo(new[] { 7, 15, 23 }), pair.Key + " must use quarter-cell masonry courses");
                    Assert.That(texture.GetPixel(8, 0), Is.Not.EqualTo(texture.GetPixel(0, 0)), pair.Key + " top course scale");
                    Assert.That(texture.GetPixel(24, 0), Is.EqualTo(texture.GetPixel(8, 0)), pair.Key + " repeated top course scale");
                    Color middleBase = texture.GetPixel(0, 12);
                    Assert.That(Enumerable.Range(6, 5).Any(x => texture.GetPixel(x, 12) != middleBase), Is.True,
                        pair.Key + " must retain the first half-cell material seam");
                    Assert.That(Enumerable.Range(22, 5).Any(x => texture.GetPixel(x, 12) != middleBase), Is.True,
                        pair.Key + " must retain the second half-cell material seam");
                }
            }
            finally
            {
                foreach (Texture2D texture in textures.Values) Object.DestroyImmediate(texture);
            }
        }

        [TestCase("move_range")]
        [TestCase("attack_range")]
        [TestCase("selected")]
        public void HighFrequencyCellOverlays_UseOnePixelInsetSquareEdges(string id)
        {
            Texture2D texture = LoadResourcePng(OverlayRoot + id);
            try
            {
                int opaque = 0;
                for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool expected = (y == 1 || y == 30) && x >= 1 && x <= 30 ||
                                    (x == 1 || x == 30) && y >= 1 && y <= 30;
                    Assert.That(texture.GetPixel(x, y).a > 0f, Is.EqualTo(expected), id + " pixel " + x + "," + y);
                    if (expected) opaque++;
                }
                Assert.That(opaque, Is.EqualTo(116), id + " one-pixel perimeter count");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [TestCase("core_overseer")]
        [TestCase("purifier_overseer")]
        public void P0Bosses_AreIndependent64PixelFootAnchoredSprites(string id)
        {
            Sprite sprite = AssertSprite(UnitRoot + id, new Vector2(64, 64), 32f, true);
            Assert.That(sprite.pivot.x, Is.InRange(29f, 35f), id + " center X");
            Assert.That(sprite.pivot.y, Is.InRange(57f, 59f), id + " foot Y");
        }

        [TestCase("sigil_mauler")]
        [TestCase("barrier_mender")]
        [TestCase("tether_hound")]
        public void P0SignatureAnimations_HaveTwoIndependent64PixelEndpointStates(string id)
        {
            AssertEndpointFrames(AnimationRoot + id, new Vector2(64, 64), 32f);
        }

        [Test]
        public void P0FireCast_HasSixIndependent32PixelFrames()
        {
            AssertSixFrames(VfxRoot + "fire_cast", new Vector2(32, 32), 32f, false);
        }

        [Test]
        public void FormalAcademyCombat32_ContainsExactlyThe48StableAssets()
        {
            Assert.That(TerrainIds, Has.Length.EqualTo(48));
            foreach (string id in TerrainIds)
                AssertSprite(AcademyRoot + id, new Vector2(32, 32), 32f, false);
        }

        [Test]
        public void FormalEnemyAnimations64_ContainsTwelveTwoStateEndpointActions()
        {
            Assert.That(AnimationIds, Has.Length.EqualTo(12));
            foreach (string id in AnimationIds)
                AssertEndpointFrames(AnimationRoot + id, new Vector2(64, 64), 32f);
        }

        [Test]
        public void RuntimeEndpointPolicy_UsesOneHardSwitchAndBoundedIntegerJitter()
        {
            Assert.That(UnitEndpointAnimationPolicy.EndpointFrameCount, Is.EqualTo(2));
            Assert.That(UnitEndpointAnimationPolicy.FrameIndex(0f), Is.Zero);
            Assert.That(UnitEndpointAnimationPolicy.FrameIndex(.49f), Is.Zero);
            Assert.That(UnitEndpointAnimationPolicy.FrameIndex(.5f), Is.EqualTo(1));
            Assert.That(UnitEndpointAnimationPolicy.FrameIndex(1f), Is.EqualTo(1));

            Vector2 before = UnitEndpointAnimationPolicy.LocalJitter(.2f, Vector2.right, 2);
            Vector2 active = UnitEndpointAnimationPolicy.LocalJitter(.42f, Vector2.right, 2);
            Vector2 after = UnitEndpointAnimationPolicy.LocalJitter(.9f, Vector2.right, 2);
            Assert.That(before, Is.EqualTo(Vector2.zero));
            Assert.That(after, Is.EqualTo(Vector2.zero));
            Assert.That(active.x, Is.EqualTo(Mathf.Round(active.x)));
            Assert.That(active.y, Is.EqualTo(Mathf.Round(active.y)));
            Assert.That(Mathf.Abs(active.x), Is.LessThanOrEqualTo(2f));
            Assert.That(Mathf.Abs(active.y), Is.LessThanOrEqualTo(2f));
        }

        [Test]
        public void FormalVfx32_ContainsFourteenSixFrameFireModules()
        {
            Assert.That(FireVfxIds, Has.Length.EqualTo(14));
            foreach (string id in FireVfxIds)
                AssertSixFrames(VfxRoot + id, new Vector2(32, 32), 32f, false);
        }

        [Test]
        public void RuntimeFloorContract_UsesAcademyStableIds()
        {
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 0, 0), Does.StartWith("academy_"));
            Assert.That(CombatBattlefieldCellPresenter.FloorKey(null, 9, 5, 4), Does.StartWith("academy_"));
        }

        [Test]
        public void RuntimeVfxRegistry_ContainsEveryM_A18FireModule()
        {
            foreach (string id in FireVfxIds)
                Assert.That(FormalArtRegistry.VfxPath(id), Is.EqualTo(VfxRoot + id));
        }

        [Test]
        public void SixtyFireSpells_ReachAllFourteenModulesWithoutChangingRuleContracts()
        {
            HashSet<string> reached = FireSpellCatalog.All
                .SelectMany(CombatVisualFeedback.FireVfxModules)
                .Append("fire_cast")
                .ToHashSet();
            Assert.That(reached, Is.EquivalentTo(FireVfxIds));
            Assert.That(FireSpellCatalog.All.Count, Is.EqualTo(60));
        }

        [Test]
        public void SameCellVfxPriority_PreservesMajorEffectsOverGroundAndCast()
        {
            Assert.That(CombatVisualFeedback.VfxPriority("fire_overlimit"), Is.GreaterThan(CombatVisualFeedback.VfxPriority("fire_detonate")));
            Assert.That(CombatVisualFeedback.VfxPriority("fire_detonate"), Is.GreaterThan(CombatVisualFeedback.VfxPriority("fire_projectile")));
            Assert.That(CombatVisualFeedback.VfxPriority("fire_projectile"), Is.GreaterThan(CombatVisualFeedback.VfxPriority("fire_cast")));
            Assert.That(CombatVisualFeedback.VfxPriority("fire_cast"), Is.GreaterThan(CombatVisualFeedback.VfxPriority("fire_burning_ground")));
        }

        private static void AssertSixFrames(string root, Vector2 size, float ppu, bool unitPivot)
        {
            var paths = new HashSet<string>();
            for (int frame = 0; frame < 6; frame++)
            {
                Sprite sprite = AssertSprite(root + "/frame_" + frame.ToString("00"), size, ppu, unitPivot);
                string path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(paths.Add(path), Is.True, root + " frame " + frame + " must be an independent source asset");
                if (unitPivot)
                {
                    Assert.That(sprite.pivot.x, Is.InRange(29f, 35f), path + " center X");
                    Assert.That(sprite.pivot.y, Is.InRange(57f, 59f), path + " foot Y");
                }
            }
        }

        private static void AssertEndpointFrames(string root, Vector2 size, float ppu)
        {
            Sprite first = AssertSprite(root + "/frame_00", size, ppu, true);
            Sprite last = AssertSprite(root + "/frame_05", size, ppu, true);
            Assert.That(AssetDatabase.GetAssetPath(first), Is.Not.EqualTo(AssetDatabase.GetAssetPath(last)), root);
        }

        private static Sprite AssertSprite(string resourcePath, Vector2 size, float ppu, bool unitPivot)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            Assert.That(sprite, Is.Not.Null, resourcePath);
            Assert.That(sprite.rect.size, Is.EqualTo(size), resourcePath);
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
            Assert.That(importer, Is.Not.Null, resourcePath);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), resourcePath);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), resourcePath);
            Assert.That(importer.mipmapEnabled, Is.False, resourcePath);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(ppu), resourcePath);
            return sprite;
        }

        private static Texture2D LoadPng(string id)
        {
            return LoadResourcePng(AcademyRoot + id);
        }

        private static Texture2D LoadResourcePng(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            Assert.That(sprite, Is.Not.Null, resourcePath);
            string path = AssetDatabase.GetAssetPath(sprite);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            Assert.That(texture.width, Is.EqualTo(32), path);
            Assert.That(texture.height, Is.EqualTo(32), path);
            return texture;
        }
    }
}
