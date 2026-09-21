using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>
    /// 固定教学段之后接入的“随机生成的一层”。
    /// 节点 id 保持与 <see cref="RogueliteMapCatalog.Nodes"/> 中的学院 20 节点一致（存档兼容），
    /// 这里只补上两件东西：学院层子图 + “节点 id ↔ 内容表编号”的对应关系。
    /// 对应关系逐条来自 Worldbuilding/数据表/OCC_学院节点内容数据表_v1.0.csv，
    /// 底图与敌群变体来自 OCC_学院遭遇编成数据表_v1.0.csv。
    /// </summary>
    public static class RogueliteAcademyLayerCatalog
    {
        /// <summary>固定教学段收束后玩家最先可选的学院层入口。</summary>
        public static readonly IReadOnlyList<string> EntryNodeIds = new[] { "academy_gate", "dorm_drill" };

        /// <summary>随机层终点。</summary>
        public const string FinaleNodeId = "core_finale";

        public static readonly IReadOnlyList<string> NodeIds = new[]
        {
            "academy_gate", "tutorial_hall", "dorm_watch", "market_lane", "dorm_drill",
            "field_infirmary", "lecture_annex", "study_vault", "archive_wing", "sparring_ring",
            "workshop_yard", "clinic_hall", "supply_depot", "wilds_path", "observatory_path",
            "wilds_camp", "seal_bridge", "tower_foyer", "tower_records", "tower_lift",
            FinaleNodeId
        };

        /// <summary>不在学院层节点集里、但被学院层节点连接的旧郊道节点。</summary>
        private static readonly IReadOnlyList<string> CrossLayerNodeIds = new[]
        {
            "start", "depot_wreck", "switchyard", "signal_hub", "elite_foundry",
            "records_archive", "transmission_tower", "core_approach", "core_vault"
        };

        /// <summary>学院层一层可以走到的全部节点（含少数跨层连接）。</summary>
        public static readonly IReadOnlyList<string> LayerNodeIds = NodeIds.Concat(CrossLayerNodeIds).ToArray();

        private static readonly HashSet<string> NodeIdSet = new HashSet<string>(NodeIds, StringComparer.Ordinal);
        private static readonly HashSet<string> LayerNodeIdSet = new HashSet<string>(LayerNodeIds, StringComparer.Ordinal);

        public static bool IsAcademyLayerNode(string nodeId) => nodeId != null && NodeIdSet.Contains(nodeId);
        public static bool IsLayerNode(string nodeId) => nodeId != null && LayerNodeIdSet.Contains(nodeId);

        /// <summary>学院层 20 个常规节点（不含首领）。</summary>
        public static IReadOnlyList<RogueliteMapNode> RegularNodes =>
            NodeIds.Where(id => id != FinaleNodeId).Select(RogueliteMapCatalog.Node).ToArray();

        public static IReadOnlyList<RogueliteMapNode> LayerNodes =>
            LayerNodeIds.Select(RogueliteMapCatalog.Node).ToArray();

        /// <summary>一层计划走的常规节点数（用于界面文案与验收）。</summary>
        public static int PlannedRegularNodeCount => NodeIds.Count - 1;

        public sealed class AcademyNodeContentMapping
        {
            public string NodeId { get; }
            /// <summary>OCC_学院节点内容数据表 的 ID 列（N01/E01/EV01/S01…）。</summary>
            public string ContentTableId { get; }
            /// <summary>OCC_学院遭遇编成数据表 的变体ID；事件/系统节点为空。</summary>
            public string EncounterVariantId { get; }
            public RogueliteMapNodeType Type { get; }

            public AcademyNodeContentMapping(string nodeId, string contentTableId, string encounterVariantId)
            {
                NodeId = nodeId;
                ContentTableId = contentTableId ?? string.Empty;
                EncounterVariantId = encounterVariantId ?? string.Empty;
                Type = RogueliteMapCatalog.Node(nodeId).Type;
            }
        }

        // 只建立对应关系，不改动任何节点 id。精英节点取 E 系列的 level 变体，
        // 普通战斗节点取 N 系列的 level 变体，事件节点取 EV 系列。
        public static readonly IReadOnlyList<AcademyNodeContentMapping> ContentMappings = new[]
        {
            new AcademyNodeContentMapping("academy_gate", "EV01", string.Empty),
            new AcademyNodeContentMapping("tutorial_hall", "N01", "weak_flank_drill"),
            new AcademyNodeContentMapping("dorm_watch", "N02", "weak_tracker_test"),
            new AcademyNodeContentMapping("market_lane", "N09", "strong_gatehouse_a"),
            new AcademyNodeContentMapping("dorm_drill", "N08", "strong_rail_patrol_a"),
            new AcademyNodeContentMapping("field_infirmary", "EV09", string.Empty),
            new AcademyNodeContentMapping("lecture_annex", "N10", "strong_signal_hub_a"),
            new AcademyNodeContentMapping("study_vault", "N13", "strong_transmission_tower_a"),
            new AcademyNodeContentMapping("archive_wing", "N07", "strong_depot_wreck_a"),
            new AcademyNodeContentMapping("sparring_ring", "N17", "strong_relay_raid_b"),
            new AcademyNodeContentMapping("workshop_yard", "N18", "strong_transmission_tower_b"),
            new AcademyNodeContentMapping("clinic_hall", "N12", "strong_rail_patrol_b"),
            new AcademyNodeContentMapping("supply_depot", "E01", "elite_foundry_a"),
            new AcademyNodeContentMapping("wilds_path", "N14", "strong_depot_wreck_b"),
            new AcademyNodeContentMapping("observatory_path", "E06", "outer_ring_clearance_a"),
            new AcademyNodeContentMapping("wilds_camp", "E03", "cliff_relay_survey_a"),
            new AcademyNodeContentMapping("seal_bridge", "N15", "strong_gatehouse_b"),
            new AcademyNodeContentMapping("tower_foyer", "E04", "library_discipline_a"),
            new AcademyNodeContentMapping("tower_records", "EV08", string.Empty),
            new AcademyNodeContentMapping("tower_lift", "T02", string.Empty),
            new AcademyNodeContentMapping(FinaleNodeId, "B01", "boss_academy_sealed_core")
        };

        private static readonly IReadOnlyDictionary<string, AcademyNodeContentMapping> ByNode =
            ContentMappings.ToDictionary(value => value.NodeId, StringComparer.Ordinal);

        public static AcademyNodeContentMapping Mapping(string nodeId) =>
            ByNode.TryGetValue(nodeId ?? string.Empty, out AcademyNodeContentMapping mapping)
                ? mapping : throw new KeyNotFoundException("Unknown academy layer node: " + nodeId);

        public static bool TryMapping(string nodeId, out AcademyNodeContentMapping mapping) =>
            ByNode.TryGetValue(nodeId ?? string.Empty, out mapping);

        private static readonly HashSet<string> NormalNodeIds = new HashSet<string>(
            ContentMappings.Where(value => value.Type == RogueliteMapNodeType.Combat).Select(value => value.NodeId), StringComparer.Ordinal);
        private static readonly HashSet<string> EliteNodeIds = new HashSet<string>(
            ContentMappings.Where(value => value.Type == RogueliteMapNodeType.Elite).Select(value => value.NodeId), StringComparer.Ordinal);

        /// <summary>这一层会进入战斗的节点（普通 + 精英 + 首领）。</summary>
        public static IReadOnlyList<string> EncounterNodeIds =>
            ContentMappings.Where(value => value.Type == RogueliteMapNodeType.Combat ||
                value.Type == RogueliteMapNodeType.Elite || value.Type == RogueliteMapNodeType.Finale)
                .OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray().Select(value => value.NodeId).ToArray();

        /// <summary>这一层的事件节点（含两个功能型事件）。</summary>
        public static IReadOnlyList<string> EventNodeIds =>
            ContentMappings.Where(value => value.Type == RogueliteMapNodeType.Event)
                .OrderBy(value => value.NodeId, StringComparer.Ordinal).Select(value => value.NodeId).ToArray();

        public static int ExpectedWeakCount => NormalNodeIds.Count(value => value == "tutorial_hall" || value == "dorm_watch");
        public static int ExpectedStrongCount => NormalNodeIds.Count - ExpectedWeakCount;

        public static IReadOnlyList<RogueliteEncounterAssignment> GenerateEncounterAssignments(int seed)
        {
            List<RogueliteEncounterAssignment> result = new List<RogueliteEncounterAssignment>();
            foreach (AcademyNodeContentMapping mapping in ContentMappings)
            {
                if (string.IsNullOrEmpty(mapping.EncounterVariantId)) continue;
                result.Add(new RogueliteEncounterAssignment(mapping.NodeId, mapping.EncounterVariantId));
            }
            if (result.Count != EncounterNodeIds.Count)
                throw new InvalidOperationException("Academy layer encounter coverage does not match its combat node set.");
            return result.OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray();
        }

        public static IReadOnlyList<AcademyEventAssignment> GenerateNodeContentAssignments()
        {
            List<AcademyEventAssignment> result = new List<AcademyEventAssignment>();
            foreach (AcademyNodeContentMapping mapping in ContentMappings.Where(value => value.Type == RogueliteMapNodeType.Event))
            {
                string eventId = ResolveEventId(mapping);
                result.Add(new AcademyEventAssignment(mapping.NodeId, eventId));
            }
            return result.OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray();
        }

        // 内容表编号 ↔ AcademyNodeContentCatalog 的事件定义编号是两套命名，这里给出唯一映射。
        private static readonly IReadOnlyDictionary<string, string> EventIdByContentTableId =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "EV01", "EV01" },
                { "EV09", "EV09" },
                { "EV08", "EV08" },
                { "T02", "EV16" }
            };

        private static string ResolveEventId(AcademyNodeContentMapping mapping)
        {
            if (!EventIdByContentTableId.TryGetValue(mapping.ContentTableId, out string eventId))
                throw new InvalidOperationException("Academy layer event node has no content definition: " + mapping.NodeId);
            return eventId;
        }

        /// <summary>
        /// 一层随机节点里的里程碑点：供地图/存档语义校验确认玩家确实走完了一整层。
        /// </summary>
        public static IReadOnlyList<string> ConsolidationNodeIds => new[] { "supply_depot", "wilds_camp", "tower_foyer" };

        /// <summary>玩家走完这一层常规节点所需的现实进度（BossMinimumProgress 的新口径）。</summary>
        public const int BossGateCompletedNodes = 10;
    }
}
