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

        public AcademyStructurePlacement(string assetId, int x, int topY, int widthCells, int heightCells,
            int quarterTurns = 0)
        {
            AssetId = assetId;
            X = x;
            TopY = topY;
            WidthCells = widthCells;
            HeightCells = heightCells;
            QuarterTurns = ((quarterTurns % 4) + 4) % 4;
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
            "academy_floor_rain_channel", "academy_floor_conduit_blank"
        };

        private static readonly string[] LightCoverIds =
        {
            "academy_prop_wicker_basket", "academy_prop_book_crate", "academy_prop_scroll_case",
            "academy_prop_tool_satchel", "academy_prop_folding_stool", "academy_prop_clay_jar",
            "academy_prop_coal_scuttle", "academy_prop_rope_coil", "academy_prop_practice_shields",
            "academy_prop_stone_planter", "academy_prop_fire_bucket_stand", "academy_prop_medical_chest"
        };

        private static readonly string[] HeavyCoverIds =
        {
            "academy_prop_specimen_cage", "academy_prop_oak_chest", "academy_prop_iron_locker",
            "academy_prop_reagent_cabinet", "academy_prop_field_lectern", "academy_prop_gear_cabinet",
            "academy_prop_warding_post", "academy_prop_sealed_trunk"
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
                    return BrokenNorthBoundary(true);
                case "core_finale":
                    return FullNorthBoundary();
                default:
                    return Array.Empty<AcademyStructurePlacement>();
            }
        }

        public static AcademyStructurePlacement[] VisualModules(string levelId) =>
            Structures(levelId).Concat(GroundAttachments(levelId)).ToArray();

        public static string CoverVariant(string levelId, GridPosition position, CoverType cover)
        {
            string[] ids = cover == CoverType.Light ? LightCoverIds :
                cover == CoverType.Heavy ? HeavyCoverIds : null;
            return ids == null ? null :
                ids[Math.Abs(StableSeed(levelId) + position.X * 7 + position.Y * 11) % ids.Length];
        }

        private static AcademyStructurePlacement[] GroundAttachments(string levelId)
        {
            int seed = Math.Abs(StableSeed(levelId));
            int offset = seed % 3;
            return new[]
            {
                new AcademyStructurePlacement(GroundAttachmentIds[seed % GroundAttachmentIds.Length],
                    1 + offset, 3, 1, 1),
                new AcademyStructurePlacement(GroundAttachmentIds[(seed + 5) % GroundAttachmentIds.Length],
                    10 - offset, 3, 1, 1),
                new AcademyStructurePlacement(GroundAttachmentIds[(seed + 10) % GroundAttachmentIds.Length],
                    4 + offset, 1, 1, 1),
                new AcademyStructurePlacement(GroundAttachmentIds[(seed + 15) % GroundAttachmentIds.Length],
                    7 - offset, 7, 1, 1)
            };
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
            if (level == null)
                return "academy_block_court_a";

            string family = FloorFamily(level.Id, x, y);
            char variant = (char)('a' + StableVariant(level.Id, x, y));
            return "academy_block_" + family + "_" + variant;
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
            // Independent floor tiles own their physical perimeter. The former curb layer used
            // rotated lit PNGs, which both reintroduced cross-tile transitions and rotated shadows.
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
