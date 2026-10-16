using System;
using System.Linq;

namespace OCC.Combat.Presentation
{
    public sealed class AcademyStructurePlacement
    {
        public string AssetId { get; }
        public int X { get; }
        public int TopY { get; }
        public int WidthCells { get; }
        public int HeightCells { get; }
        public int QuarterTurns { get; }
        public bool IsGroundAttachment { get; }

        public AcademyStructurePlacement(string assetId, int x, int topY, int widthCells, int heightCells,
            int quarterTurns = 0, bool isGroundAttachment = false)
        {
            AssetId = assetId;
            X = x;
            TopY = topY;
            WidthCells = widthCells;
            HeightCells = heightCells;
            QuarterTurns = ((quarterTurns % 4) + 4) % 4;
            IsGroundAttachment = isGroundAttachment;
        }
    }

    /// <summary>Stable visual grammar for the nine academy-stage battlefields.</summary>
    public static class AcademyBattlefieldLayoutCatalog
    {
        private static readonly string[] GroundAttachmentIds =
        {
            "academy_floor_drain_round", "academy_floor_drain_slot",
            "academy_floor_service_hatch_round", "academy_floor_service_hatch_square",
            "academy_floor_repair_stone", "academy_floor_repair_iron",
            "academy_floor_cable_cap", "academy_floor_pipe_socket",
            "academy_floor_anchor_plate", "academy_floor_inspection_window",
            "academy_floor_mortar_inlay", "academy_floor_threshold_studs",
            "academy_floor_safety_marker", "academy_floor_herb_drain",
            "academy_floor_rain_channel", "academy_floor_conduit_blank",
            "academy_floor_convergence_scribe", "academy_floor_drain_grate",
            "academy_floor_maintenance_hatch", "academy_floor_repair_plate"
        };

        private static readonly string[] LightCoverIds =
        {
            "academy_light_planter_cover_intact", "academy_light_book_crate_intact",
            "academy_light_training_pad_intact", "academy_light_wood_bench_intact"
        };

        private static readonly string[] HeavyCoverIds =
        {
            "academy_heavy_training_barricade_intact"
        };

        private static readonly AcademyStructurePlacement NorthDais =
            new AcademyStructurePlacement("academy_north_dais_6x2", 3, 8, 6, 2);

        public static AcademyStructurePlacement[] Structures(string levelId)
        {
            switch (levelId)
            {
                case "signal_hub":
                    return new[]
                    {
                        new AcademyStructurePlacement("academy_aether_pump_2x2", 3, 8, 2, 2),
                        new AcademyStructurePlacement("academy_wall_end_n", 7, 8, 1, 1),
                        new AcademyStructurePlacement("academy_wall_end_s", 7, 7, 1, 1),
                        new AcademyStructurePlacement("academy_wall_end_n", 8, 8, 1, 1),
                        new AcademyStructurePlacement("academy_wall_end_s", 8, 7, 1, 1)
                    };
                case "gatehouse":
                    return new[] { NorthDais };
                case "rail_patrol":
                case "transmission_tower":
                    return FullNorthBoundary();
                case "elite_foundry":
                    return BrokenNorthBoundary(false).Concat(new[]
                    {
                        new AcademyStructurePlacement("academy_smithing_table_2x1", 2, 4, 2, 1),
                        new AcademyStructurePlacement("academy_tool_cabinet_2x1", 7, 4, 2, 1)
                    }).ToArray();
                case "depot_wreck":
                    return BrokenNorthBoundary(false).Concat(new[]
                    {
                        new AcademyStructurePlacement("academy_low_bookcase_2x1", 3, 2, 2, 1)
                    }).ToArray();
                case "relay_raid":
                case "core_approach":
                case "cliff_relay_survey":
                    return BrokenNorthBoundary(true);
                case "library_discipline":
                    return BrokenNorthBoundary(false).Concat(new[]
                    {
                        new AcademyStructurePlacement("academy_low_bookcase_2x1", 1, 4, 2, 1),
                        new AcademyStructurePlacement("academy_low_bookcase_2x1", 7, 4, 2, 1),
                        new AcademyStructurePlacement("academy_tool_cabinet_2x1", 5, 2, 2, 1)
                    }).ToArray();
                case "outer_ring_clearance":
                    return FullNorthBoundary();
                case "calibration_lockdown":
                    return BrokenNorthBoundary(false).Concat(new[]
                    {
                        new AcademyStructurePlacement("academy_smithing_table_2x1", 4, 3, 2, 1),
                        new AcademyStructurePlacement("academy_tool_cabinet_2x1", 5, 6, 2, 1)
                    }).ToArray();
                case "sealed_vault_certification":
                    return BrokenNorthBoundary(false).Concat(new[]
                    {
                        new AcademyStructurePlacement("academy_low_bookcase_2x1", 4, 4, 2, 1),
                        new AcademyStructurePlacement("academy_tool_cabinet_2x1", 6, 2, 2, 1)
                    }).ToArray();
                case "core_finale":
                    return FullNorthBoundary();
                default:
                    return Array.Empty<AcademyStructurePlacement>();
            }
        }

        public static AcademyStructurePlacement[] VisualModules(string levelId)
        {
            if (!FirstRegionLevelCatalog.TryFor(levelId, out FirstRegionLevelDefinition level))
                return Array.Empty<AcademyStructurePlacement>();
            // Legacy structure coordinates were authored for a 12x9 board. Only draw a
            // raised module when its entire footprint belongs to the current wall layout.
            return Structures(levelId).Where(value => FitsDeclaredWall(level, value))
                .Concat(GroundAttachments(level)).ToArray();
        }

        private static bool FitsDeclaredWall(FirstRegionLevelDefinition level, AcademyStructurePlacement placement)
        {
            if (placement.X < 0 || placement.TopY - placement.HeightCells + 1 < 0 ||
                placement.X + placement.WidthCells > level.Width ||
                placement.TopY >= level.Height) return false;
            for (int y = placement.TopY - placement.HeightCells + 1; y <= placement.TopY; y++)
            for (int x = placement.X; x < placement.X + placement.WidthCells; x++)
                if (!level.Terrain.Any(value => value.Position.X == x && value.Position.Y == y &&
                                                value.Kind == LevelTerrainKind.PermanentWall)) return false;
            return true;
        }

        public static string CoverVariant(string levelId, GridPosition position, CoverType cover)
        {
            string[] ids = cover == CoverType.Light ? LightCoverIds :
                cover == CoverType.Heavy ? HeavyCoverIds : null;
            return ids == null ? null :
                ids[Math.Abs(StableSeed(levelId) + position.X * 7 + position.Y * 11) % ids.Length];
        }

        private static AcademyStructurePlacement[] GroundAttachments(FirstRegionLevelDefinition level)
        {
            int seed = Math.Abs(StableSeed(level.Id));
            var available = Enumerable.Range(0, level.Height)
                .SelectMany(y => Enumerable.Range(0, level.Width).Select(x => new GridPosition(x, y)))
                .Where(position => !level.Terrain.Any(value => value.Position == position) &&
                    !level.BlockedPositions.Contains(position) && position != level.HeroSpawn &&
                    !level.EnemyPlacements.Any(value => value.Position == position))
                .OrderBy(position => (seed + position.X * 31 + position.Y * 17) % 97)
                .ToArray();
            var chosen = new System.Collections.Generic.List<GridPosition>();
            foreach (GridPosition position in available)
            {
                if (chosen.Any(value => Math.Abs(value.X - position.X) + Math.Abs(value.Y - position.Y) < 2))
                    continue;
                chosen.Add(position);
                if (chosen.Count == 3) break;
            }
            return chosen.Select((position, index) => new AcademyStructurePlacement(
                GroundAttachmentIds[(seed + index * 5) % GroundAttachmentIds.Length],
                position.X, position.Y, 1, 1, isGroundAttachment: true)).ToArray();
        }

        public static string[] CoverVisualAssetIds() =>
            LightCoverIds.Concat(HeavyCoverIds).ToArray();

        private static int StableSeed(string levelId)
        {
            int seed = 0;
            for (int index = 0; index < (levelId?.Length ?? 0); index++) seed += levelId[index];
            return seed;
        }

        private static AcademyStructurePlacement P(string id, int x, int width = 1, int turns = 0) =>
            new AcademyStructurePlacement(id, x, 8, width, 1, turns);

        private static AcademyStructurePlacement[] FullNorthBoundary() => new[]
        {
            P("academy_wall_end_w", 0),
            P("academy_wall_straight", 1), P("academy_wall_straight", 2), P("academy_wall_straight", 3),
            P("academy_wall_straight", 4), P("academy_stairs_2x1", 5, 2), P("academy_wall_straight", 7),
            P("academy_wall_straight", 8), P("academy_wall_straight", 9), P("academy_wall_straight", 10),
            P("academy_wall_end_e", 11)
        };

        private static AcademyStructurePlacement[] BrokenNorthBoundary(bool includeStairs) => includeStairs
            ? new[]
            {
                P("academy_wall_end_w", 0), P("academy_wall_straight", 1), P("academy_wall_end_e", 2),
                P("academy_stairs_2x1", 5, 2),
                P("academy_wall_end_w", 9), P("academy_wall_straight", 10), P("academy_wall_end_e", 11)
            }
            : new[]
            {
                P("academy_wall_end_w", 0), P("academy_wall_straight", 1), P("academy_wall_end_e", 2),
                P("academy_wall_end_w", 9), P("academy_wall_straight", 10), P("academy_wall_end_e", 11)
            };

        public static string FloorAsset(FirstRegionLevelDefinition level, int x, int y, out int quarterTurns)
        {
            quarterTurns = 0;
            if (CombatTestArenaEntry.IsDedicatedTestArena)
            {
                // Test maps are made from the same independent tiles, but each receives
                // its own stable material mix so a selected arena reads at a glance.
                string surface = DedicatedTestGroundSurface(level?.Id, x, y);
                if (y == 0 && (surface.Contains("slate") || surface.Contains("earth")))
                    return surface.Replace("_surface_64", "_edge_s_64");
                return surface;
            }
            // Each logical cell owns one native 32x32 tile with its authored border.
            if (level == null)
                return "academy_block_court_a";

            string family = FloorFamily(level.Id, x, y);
            char variant = (char)('a' + StableVariant(level.Id, x, y));
            return $"academy_block_{family}_{variant}";
        }

        public static string FloorLowAsset(FirstRegionLevelDefinition level, int x, int y)
        {
            if (!CombatTestArenaEntry.IsDedicatedTestArena) return null;
            string high = FloorAsset(level, x, y, out _);
            return high == null ? null : high.Replace("_64", "_32");
        }

        private static string DedicatedTestGroundSurface(string levelId, int x, int y)
        {
            string[] materials =
            {
                "academy_test_ground_theme_slate_surface_64",
                "academy_test_ground_theme_earth_surface_64",
                "academy_test_ground_tile_court_a_64",
                "academy_test_ground_tile_court_b_64",
                "academy_test_ground_tile_court_c_64",
                "academy_test_ground_tile_court_d_64"
            };
            int seed = StableVariant(levelId, 0, 0);
            int primary = seed % materials.Length;
            // A sparse second material makes route surfaces legible without turning
            // the compact board into a noisy checkerboard.
            int accent = (primary + 1 + seed % (materials.Length - 1)) % materials.Length;
            return (x * 3 + y * 5 + seed) % 11 == 0 ? materials[accent] : materials[primary];
        }

        private static int StableVariant(string levelId, int x, int y)
        {
            int seed = 0;
            for (int index = 0; index < (levelId?.Length ?? 0); index++) seed += levelId[index];
            return Math.Abs(seed + x * 17 + y * 31) % 4;
        }

        public static string FloorFamily(string levelId, int x, int y)
        {
            switch (levelId)
            {
                case "rail_patrol": return 4 <= x && x <= 7 || 3 <= y && y <= 5 ? "road" : "earth";
                case "depot_wreck": return 4 <= x && x <= 7 || y == 4 ? "road" : "ruin";
                case "relay_raid": return 1 <= x && x <= 3 || y >= 6 && x <= 9 ? "road" : "earth";
                case "signal_hub": return 4 <= x && x <= 7 || 3 <= y && y <= 5 ? "road" : "court";
                case "gatehouse": return 4 <= x && x <= 8 || y == 1 || y == 7 ? "road" : "court";
                case "transmission_tower": return 3 <= x && x <= 9 && 2 <= y && y <= 6 ? "road" : "court";
                case "elite_foundry": return 2 <= x && x <= 4 || 7 <= x && x <= 9 ? "road" : "ruin";
                case "core_approach": return Math.Abs(x + y - 10) <= 1 || x == 1 || x == 10 ? "road" : "court";
                case "core_finale":
                    int ring = Math.Max(Math.Abs(x - 6), Math.Abs(y - 4));
                    return ring == 2 || ring == 3 ? "road" : "court";
                default: return "court";
            }
        }

        public static string BoundaryOverlay(FirstRegionLevelDefinition level, int x, int y, out int quarterTurns)
        {
            quarterTurns = 0;
            // The independent native tiles own the visible cell edge. The old test-arena
            // earth facade has a different material and scale, so it cannot be mixed in.
            return null;
        }

        public static string BoundaryOverlayForMask(int mask, out int quarterTurns)
        {
            mask &= 15;
            quarterTurns = 0;
            if (mask == 0) return null;
            if (mask == 15) return "academy_curb_enclosed";

            int count = CountBits(mask);
            int canonical = count == 1 ? 1 : count == 3 ? 7 : IsOpposite(mask) ? 5 : 3;
            string asset = count == 1 ? "academy_curb_edge" :
                count == 3 ? "academy_curb_three" :
                IsOpposite(mask) ? "academy_curb_opposite" : "academy_curb_corner";
            for (int turn = 0; turn < 4; turn++)
            {
                if (RotateMaskClockwise(canonical, turn) != mask) continue;
                quarterTurns = turn;
                return asset;
            }
            return null;
        }

        private static bool IsOpposite(int mask) => mask == 5 || mask == 10;

        private static int CountBits(int mask)
        {
            int count = 0;
            for (; mask != 0; mask >>= 1) count += mask & 1;
            return count;
        }

        private static int RotateMaskClockwise(int mask, int turns)
        {
            for (int turn = 0; turn < turns; turn++)
                mask = ((mask & 1) << 1) | ((mask & 2) << 1) | ((mask & 4) << 1) | ((mask & 8) >> 3);
            return mask;
        }

    }
}
