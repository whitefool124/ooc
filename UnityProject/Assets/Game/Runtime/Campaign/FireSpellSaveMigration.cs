using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum FireSpellSaveMigrationKind
    {
        Direct,
        ReselectSameRarity,
        Compensation
    }

    public sealed class FireSpellSaveMigrationEntry
    {
        public string LegacyId { get; }
        public string LegacyName { get; }
        public FireSpellRarity LegacyRarity { get; }
        public FireSpellSaveMigrationKind Kind { get; }
        public string DirectTargetId { get; }
        public IReadOnlyList<string> SemanticReferenceIds { get; }
        public string Reason { get; }

        internal FireSpellSaveMigrationEntry(string legacyId, string legacyName, FireSpellRarity legacyRarity,
            FireSpellSaveMigrationKind kind, string directTargetId, IEnumerable<string> semanticReferenceIds, string reason)
        {
            LegacyId = legacyId;
            LegacyName = legacyName;
            LegacyRarity = legacyRarity;
            Kind = kind;
            DirectTargetId = directTargetId;
            SemanticReferenceIds = (semanticReferenceIds ?? Array.Empty<string>()).ToArray();
            Reason = reason;
        }
    }

    public sealed class FireSpellSaveMigrationClaim
    {
        public string ClaimId { get; }
        public string LegacyId { get; }
        public FireSpellRarity Rarity { get; }
        public FireSpellSaveMigrationKind Kind { get; }
        public IReadOnlyList<int> OriginalEquippedSlots { get; }

        internal FireSpellSaveMigrationClaim(string legacyId, FireSpellRarity rarity,
            FireSpellSaveMigrationKind kind, IEnumerable<int> originalEquippedSlots)
        {
            LegacyId = legacyId;
            Rarity = rarity;
            Kind = kind;
            ClaimId = "fire-personal-spells-v0.2:" +
                (kind == FireSpellSaveMigrationKind.Compensation ? "compensation:" : "reselect:") + legacyId;
            OriginalEquippedSlots = (originalEquippedSlots ?? Array.Empty<int>()).OrderBy(slot => slot).ToArray();
        }
    }

    public sealed class FireSpellSaveMigrationResult
    {
        public IReadOnlyList<string> DirectOwnedIds { get; }
        public IReadOnlyList<string> EquippedNewIds { get; }
        public IReadOnlyList<FireSpellSaveMigrationClaim> ReselectClaims { get; }
        public IReadOnlyList<FireSpellSaveMigrationClaim> CompensationClaims { get; }
        public IReadOnlyList<string> UnknownLegacyIds { get; }
        public IReadOnlyList<int> OrphanedEquippedSlots { get; }

        internal FireSpellSaveMigrationResult(IEnumerable<string> directOwnedIds, IEnumerable<string> equippedNewIds,
            IEnumerable<FireSpellSaveMigrationClaim> reselectClaims, IEnumerable<FireSpellSaveMigrationClaim> compensationClaims,
            IEnumerable<string> unknownLegacyIds, IEnumerable<int> orphanedEquippedSlots)
        {
            DirectOwnedIds = directOwnedIds.ToArray();
            EquippedNewIds = equippedNewIds.ToArray();
            ReselectClaims = reselectClaims.ToArray();
            CompensationClaims = compensationClaims.ToArray();
            UnknownLegacyIds = unknownLegacyIds.ToArray();
            OrphanedEquippedSlots = orphanedEquippedSlots.ToArray();
        }
    }

    public static class FireSpellSaveMigration
    {
        public const string SourceCatalogVersion = "fire-personal-spells-v0.1";
        public const string TargetCatalogVersion = "fire-personal-spells-v0.2";

        private static readonly FireSpellSaveMigrationEntry[] entries =
        {
            D("F-P01", "火弹", FireSpellRarity.Common, "F-P-R01", "成本、射程、目标与伤害一致。"),
            D("F-P02", "火带", FireSpellRarity.Uncommon, "F-P-R11", "连续三格火场的伤害与持续时间一致。"),
            D("F-P03", "烙印", FireSpellRarity.Common, "F-P-R03", "直接伤害与燃烧一致。"),
            R("F-P04", "引爆", FireSpellRarity.Rare, "燃烧来源合法性与消费规则被收窄。", "F-P-R16"),
            D("F-P05", "火矢", FireSpellRarity.Common, "F-P-R02", "成本、射程与伤害一致。"),
            D("F-P06", "火种", FireSpellRarity.Uncommon, "F-P-R04", "无直伤的两回合燃烧一致。"),
            D("F-P07", "焰线", FireSpellRarity.Uncommon, "F-P-R06", "直线伤害与重掩体截断一致。"),
            D("F-P08", "余烬火弹", FireSpellRarity.Uncommon, "F-P-R05", "伤害与燃烧延长规则一致。"),
            D("F-P09", "焰击术", FireSpellRarity.Rare, "F-P-R09", "机械语义一致；目标目录公开重标稀有度。"),
            D("F-P10", "火焰喷射", FireSpellRarity.Uncommon, "F-P-R07", "锥形全单位伤害一致；目标目录公开重标稀有度。"),
            D("F-P11", "点燃喷射", FireSpellRarity.Rare, "F-P-R08", "敌我差异与点燃规则一致。"),
            R("F-P12", "追火术", FireSpellRarity.Uncommon, "独立远程条件伤害被改成武器或近战路径。", "F-P-U14", "F-P-M16"),
            D("F-P13", "火路", FireSpellRarity.Common, "F-P-R12", "直线火场一致。"),
            R("F-P14", "热浪弧", FireSpellRarity.Uncommon, "扇形燃烧地格形状已退役。", "F-P-R11", "F-P-R13"),
            D("F-P15", "灼域火钉", FireSpellRarity.Uncommon, "F-P-R13", "单格高强度火场一致。"),
            R("F-P16", "围炉火环", FireSpellRarity.Rare, "围绕单位且中心安全的正交环没有等价形状。", "F-P-R15", "F-P-R20"),
            R("F-P17", "炉口喷涌", FireSpellRarity.Uncommon, "十字五格短时火场已被合并。", "F-P-R11", "F-P-R13"),
            D("F-P18", "炽焰墙", FireSpellRarity.Rare, "F-P-R14", "五格火墙、持续与延时一致。"),
            R("F-P19", "灰烬复燃", FireSpellRarity.Common, "同名新术式维护单位燃烧而非燃烧地格。", "F-P-U15"),
            R("F-P20", "焦土十字", FireSpellRarity.Rare, "最近条目增加即时单位伤害且改变持续时间。", "F-P-R20"),
            D("F-P21", "熔火领域", FireSpellRarity.Rare, "F-P-R15", "三乘三区域火场一致。"),
            R("F-P22", "烬爆指令", FireSpellRarity.Common, "新单点引爆提升伤害和稀有度，地格引爆又改变消费来源。", "F-P-R16", "F-P-R17"),
            D("F-P23", "地火抽爆", FireSpellRarity.Common, "F-P-R17", "地格上的单位伤害与地格消费一致。"),
            D("F-P24", "爆燃横扫", FireSpellRarity.Uncommon, "F-P-R18", "锥形燃烧消费一致。"),
            R("F-P25", "焚心爆点", FireSpellRarity.Uncommon, "邻接溅射被改为无燃烧消费的武器附着。", "F-P-U05"),
            R("F-P26", "焦土回响", FireSpellRarity.Uncommon, "连通火场取样与仅清中心地格的规则已退役。", "F-P-R18"),
            R("F-P27", "余烬追爆", FireSpellRarity.Uncommon, "固定燃烧余时的独立伤害术被拆成多个武器收益。", "F-P-U12", "F-P-U13", "F-P-U14"),
            R("F-P28", "火场聚爆", FireSpellRarity.Rare, "新区域终结术创建而非读取并消费火场。", "F-P-R20"),
            R("F-P29", "焚烬穿刺", FireSpellRarity.Rare, "直线逐个消费燃烧的结算已退役。", "F-P-R18", "F-P-R20"),
            R("F-P30", "终焰裁决", FireSpellRarity.Rare, "最近终结术改成武器附着且不消费燃烧地格。", "F-P-U20"),
            R("F-P31", "熔甲火钉", FireSpellRarity.Common, "接触附着与远程射流都不能保留原伤害、射程和破甲组合。", "F-P-M06", "F-P-R10"),
            R("F-P32", "赤炼爆点", FireSpellRarity.Uncommon, "破甲被拆成武器附着或不同数值的远程术。", "F-P-U03", "F-P-R10"),
            R("F-P33", "熔切束", FireSpellRarity.Uncommon, "破障改成武器附着或附带邻接伤害。", "F-P-R19", "F-P-U04"),
            R("F-P34", "炉压破门", FireSpellRarity.Uncommon, "轻掩体立即摧毁与重掩体定值伤害的分型规则已退役。", "F-P-R19", "F-P-U04"),
            R("F-P35", "焦甲横扫", FireSpellRarity.Rare, "锥形伤害和敌方破甲被拆成不同路径。", "F-P-R10", "F-P-M08"),
            R("F-P36", "热裂震波", FireSpellRarity.Uncommon, "多目标十字耐久伤害改成单目标破障溅射。", "F-P-R19"),
            R("F-P37", "熔障炮", FireSpellRarity.Rare, "摧毁轻掩体后原格生火的条件规则已退役。", "F-P-R19", "F-P-R20"),
            C("F-P38", "炉芯过热", FireSpellRarity.Rare, "设备归零后的过载能力完全退役。", "F-P-R19"),
            R("F-P39", "焚甲贯矢", FireSpellRarity.Rare, "远程与破甲八被拆到不同接触路径。", "F-P-R10", "F-P-M07"),
            C("F-P40", "高炉断层", FireSpellRarity.Rare, "同一直线分别伤害掩体并同时伤害单位的混合结算无继承者。", "F-P-R19", "F-P-R20"),
            R("F-P41", "灼热疾行", FireSpellRarity.Common, "纯位移、接敌攻击和火场被拆成不同术式。", "F-P-M02", "F-P-M03"),
            R("F-P42", "炽热障壁", FireSpellRarity.Common, "F-P-U07 已由旧炉温护持一对一继承，不能合并两个旧拥有项。", "F-P-U07"),
            R("F-P43", "余烬护甲", FireSpellRarity.Uncommon, "未燃烧时护盾从二十降为十二。", "F-P-U08"),
            D("F-P44", "灼缚解离", FireSpellRarity.Uncommon, "F-P-M14", "施放条件、清束缚与相邻伤害一致。"),
            D("F-P45", "温血苏醒", FireSpellRarity.Common, "F-P-U09", "清迟缓并恢复基础移动语义一致。"),
            D("F-P46", "炉温护持", FireSpellRarity.Uncommon, "F-P-U07", "目标、射程、视线与护盾一致；目标目录公开重标稀有度。"),
            D("F-P47", "焰压退击", FireSpellRarity.Common, "F-P-M15", "相邻伤害与推离一致。"),
            R("F-P48", "焦痕突进", FireSpellRarity.Rare, "路径邻敌伤害和终点生火被改成终点攻击与起点生火。", "F-P-M03", "F-P-M19"),
            D("F-P49", "热源回收", FireSpellRarity.Uncommon, "F-P-U11", "地格消费与回魔一致；目标目录公开重标稀有度。"),
            C("F-P50", "炉火应急", FireSpellRarity.Rare, "清燃烧、护盾和邻格生火三段效果没有同一继承者。", "F-P-U08", "F-P-R20")
        };

        private static readonly IReadOnlyDictionary<string, FireSpellSaveMigrationEntry> byLegacyId;

        static FireSpellSaveMigration()
        {
            ValidateTable(entries);
            byLegacyId = entries.ToDictionary(entry => entry.LegacyId, StringComparer.Ordinal);
        }

        public static IReadOnlyList<FireSpellSaveMigrationEntry> All => entries;

        public static FireSpellSaveMigrationEntry Get(string legacyId)
        {
            if (!TryGet(legacyId, out FireSpellSaveMigrationEntry entry))
                throw new InvalidOperationException("Unknown legacy fire spell id: " + legacyId);
            return entry;
        }

        public static bool TryGet(string legacyId, out FireSpellSaveMigrationEntry entry)
        {
            if (legacyId == null)
            {
                entry = null;
                return false;
            }
            return byLegacyId.TryGetValue(legacyId, out entry);
        }

        public static FireSpellSaveMigrationResult Migrate(IEnumerable<string> legacyOwnedIds,
            IEnumerable<string> legacyEquippedIds)
        {
            string[] equipped = (legacyEquippedIds ?? Array.Empty<string>()).ToArray();
            HashSet<string> rawOwned = new HashSet<string>(legacyOwnedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> knownOwned = new HashSet<string>(rawOwned.Where(id => id != null && byLegacyId.ContainsKey(id)), StringComparer.Ordinal);
            List<string> unknown = rawOwned.Where(id => !string.IsNullOrEmpty(id) && !byLegacyId.ContainsKey(id)).ToList();
            List<int> orphanedSlots = new List<int>();
            string[] migratedEquipped = new string[equipped.Length];

            for (int slot = 0; slot < equipped.Length; slot++)
            {
                string legacyId = equipped[slot];
                if (string.IsNullOrEmpty(legacyId)) continue;
                if (!byLegacyId.ContainsKey(legacyId))
                {
                    unknown.Add(legacyId);
                    orphanedSlots.Add(slot);
                    continue;
                }
                if (!knownOwned.Contains(legacyId))
                {
                    orphanedSlots.Add(slot);
                    continue;
                }
                FireSpellSaveMigrationEntry entry = byLegacyId[legacyId];
                if (entry.Kind == FireSpellSaveMigrationKind.Direct) migratedEquipped[slot] = entry.DirectTargetId;
            }

            List<string> directOwned = new List<string>();
            List<FireSpellSaveMigrationClaim> reselect = new List<FireSpellSaveMigrationClaim>();
            List<FireSpellSaveMigrationClaim> compensation = new List<FireSpellSaveMigrationClaim>();
            foreach (FireSpellSaveMigrationEntry entry in entries)
            {
                if (!knownOwned.Contains(entry.LegacyId)) continue;
                if (entry.Kind == FireSpellSaveMigrationKind.Direct)
                {
                    directOwned.Add(entry.DirectTargetId);
                    continue;
                }

                int[] originalSlots = Enumerable.Range(0, equipped.Length)
                    .Where(slot => string.Equals(equipped[slot], entry.LegacyId, StringComparison.Ordinal)).ToArray();
                FireSpellSaveMigrationClaim claim = new FireSpellSaveMigrationClaim(entry.LegacyId,
                    entry.LegacyRarity, entry.Kind, originalSlots);
                if (entry.Kind == FireSpellSaveMigrationKind.ReselectSameRarity) reselect.Add(claim);
                else compensation.Add(claim);
            }

            return new FireSpellSaveMigrationResult(directOwned, migratedEquipped, reselect, compensation,
                unknown.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal),
                orphanedSlots.Distinct().OrderBy(slot => slot));
        }

        public static void ValidateCoverageAndUniqueness() => ValidateTable(entries);

        private static void ValidateTable(IReadOnlyList<FireSpellSaveMigrationEntry> table)
        {
            if (table == null || table.Count != 50) throw new InvalidOperationException("Fire spell migration must contain exactly 50 entries.");
            string[] expected = Enumerable.Range(1, 50).Select(index => $"F-P{index:00}").ToArray();
            if (!table.Select(entry => entry.LegacyId).SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Fire spell migration legacy ids must be ordered F-P01 through F-P50.");
            if (table.Select(entry => entry.LegacyId).Distinct(StringComparer.Ordinal).Count() != table.Count)
                throw new InvalidOperationException("Fire spell migration contains duplicate legacy ids.");
            string[] directTargets = table.Where(entry => entry.Kind == FireSpellSaveMigrationKind.Direct)
                .Select(entry => entry.DirectTargetId).ToArray();
            if (directTargets.Any(string.IsNullOrWhiteSpace) || directTargets.Distinct(StringComparer.Ordinal).Count() != directTargets.Length)
                throw new InvalidOperationException("Direct fire spell migration targets must be non-empty and unique.");
            if (table.Any(entry => entry.Kind != FireSpellSaveMigrationKind.Direct && !string.IsNullOrEmpty(entry.DirectTargetId)))
                throw new InvalidOperationException("Only direct migration entries may define a direct target.");
            if (table.Any(entry => entry.Kind != FireSpellSaveMigrationKind.Direct && entry.SemanticReferenceIds.Count == 0))
                throw new InvalidOperationException("Non-direct migration entries must expose at least one semantic reference.");
            if (table.Any(entry => string.IsNullOrWhiteSpace(entry.LegacyName) || string.IsNullOrWhiteSpace(entry.Reason)))
                throw new InvalidOperationException("Every fire spell migration entry requires a name and reason.");
        }

        private static FireSpellSaveMigrationEntry D(string id, string name, FireSpellRarity rarity, string target, string reason)
            => new FireSpellSaveMigrationEntry(id, name, rarity, FireSpellSaveMigrationKind.Direct, target, new[] { target }, reason);

        private static FireSpellSaveMigrationEntry R(string id, string name, FireSpellRarity rarity, string reason, params string[] references)
            => new FireSpellSaveMigrationEntry(id, name, rarity, FireSpellSaveMigrationKind.ReselectSameRarity, null, references, reason);

        private static FireSpellSaveMigrationEntry C(string id, string name, FireSpellRarity rarity, string reason, params string[] references)
            => new FireSpellSaveMigrationEntry(id, name, rarity, FireSpellSaveMigrationKind.Compensation, null, references, reason);
    }
}
