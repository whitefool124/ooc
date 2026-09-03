using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat.Roguelite
{
    public sealed class EquipmentInstanceDto
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public EquipmentSlot EquippedSlot { get; }
        public EquipmentRarity Rarity { get; }
        public int PowerBand { get; }
        public List<string> MutableAffixIds { get; } = new List<string>();
        public List<string> UpgradeBranchIds { get; } = new List<string>();
        public int ReforgeCount { get; set; }
        public int ResolvedWeight { get; set; }
        public int ResolvedAetherLoad { get; set; }
        public string SourceStage { get; set; } = "academy";
        public string SourceType { get; set; } = "starter";
        public int AcquiredOrder { get; set; }
        public int BackpackX { get; set; } = -1;
        public int BackpackY { get; set; } = -1;
        public bool BackpackRotated { get; set; }

        public EquipmentInstanceDto(string instanceId, string definitionId, EquipmentSlot equippedSlot, EquipmentRarity rarity, int powerBand)
        { InstanceId = instanceId; DefinitionId = definitionId; EquippedSlot = equippedSlot; Rarity = rarity; PowerBand = powerBand; }
    }

    public sealed class TacticalItemInstanceDto
    {
        public string InstanceId { get; set; }
        public string DefinitionId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool Rotated { get; set; }
        public int ChargesCurrent { get; set; }
        public int ChargesMaximum { get; set; }
        public string SourceStage { get; set; }
        public string SourceType { get; set; }
    }

    public sealed class RogueRunDto
    {
        public string SaveVersion { get; set; } = RogueRuntimeConstants.SaveVersion;
        public string RunId { get; set; }
        public int Seed { get; set; }
        public string StageId { get; set; } = "academy";
        public int StageTime { get; set; }
        public string CurrentNodeId { get; set; } = "start";
        public string RegionBossId { get; set; } = string.Empty;
        public string StarterId { get; set; } = string.Empty;
        public List<string> VisitedNodeIds { get; } = new List<string>();
        public List<string> CompletedNodeIds { get; } = new List<string>();
        public List<string> ClaimedContentIds { get; } = new List<string>();
        public bool AwaitingReward { get; set; }
        public string PendingContentChoiceId { get; set; } = string.Empty;
        public string PendingContentCombatMissionId { get; set; } = string.Empty;
        public int Gold { get; set; }
        public int StageContribution { get; set; }
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; }
        public List<string> MasteredSpellIds { get; } = new List<string>();
        public string[] EquippedSpellIds { get; } = new string[RogueRuntimeConstants.SpellSlotCount];
        public List<EquipmentInstanceDto> EquipmentInstances { get; } = new List<EquipmentInstanceDto>();
        public Dictionary<EquipmentSlot, string> EquipmentSlotInstanceIds { get; } = Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().ToDictionary(value => value, value => string.Empty);
        public List<TacticalItemInstanceDto> TacticalItemInstances { get; } = new List<TacticalItemInstanceDto>();
        public string[] ItemQuickbarInstanceIds { get; } = new string[RogueRuntimeConstants.ItemQuickbarSize];
        public int DeterministicCounter { get; set; }
        public List<string> PendingRewardIds { get; } = new List<string>();
        public List<string> ReselectionClaimIds { get; } = new List<string>();
        public string MigrationReportId { get; set; } = string.Empty;
        public List<string> EncounterAssignments { get; } = new List<string>();
        public List<string> NodeContentAssignments { get; } = new List<string>();

        public static RogueRunDto CreateNew(string runId, int seed)
        {
            RogueRunDto dto = new RogueRunDto { RunId = runId, Seed = seed, Gold = 8, StageContribution = 0, CurrentHealth = 18, CurrentMana = 12 };
            for (int index = 0; index < dto.EquippedSpellIds.Length; index++) dto.EquippedSpellIds[index] = string.Empty;
            for (int index = 0; index < dto.ItemQuickbarInstanceIds.Length; index++) dto.ItemQuickbarInstanceIds[index] = string.Empty;
            string[] basics = { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER" };
            foreach (string id in basics) dto.MasteredSpellIds.Add(id);
            Array.Copy(basics, dto.EquippedSpellIds, basics.Length);
            return dto;
        }
    }

    public static class Rogue11Serializer
    {
        public static string Serialize(RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            ValidateShape(dto);
            string spells = JoinStrings(dto.MasteredSpellIds);
            string equipped = JoinStrings(dto.EquippedSpellIds);
            string slots = string.Join(";", Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Select(slot => slot + "," + B(dto.EquipmentSlotInstanceIds[slot])));
            string equipment = string.Join(";", dto.EquipmentInstances.OrderBy(value => value.AcquiredOrder).ThenBy(value => value.InstanceId, StringComparer.Ordinal).Select(value =>
                string.Join(",", B(value.InstanceId), B(value.DefinitionId), value.EquippedSlot, value.Rarity, value.PowerBand,
                    value.ReforgeCount, value.ResolvedWeight, value.ResolvedAetherLoad, B(value.SourceStage), B(value.SourceType), value.AcquiredOrder,
                    B(string.Join("~", value.MutableAffixIds)), B(string.Join("~", value.UpgradeBranchIds)), value.BackpackX, value.BackpackY, value.BackpackRotated ? 1 : 0)));
            string tactical = string.Join(";", dto.TacticalItemInstances.OrderBy(value => value.InstanceId, StringComparer.Ordinal).Select(value =>
                string.Join(",", B(value.InstanceId), B(value.DefinitionId), value.X, value.Y, value.Rotated ? 1 : 0,
                    value.ChargesCurrent, value.ChargesMaximum, B(value.SourceStage), B(value.SourceType))));
            return string.Join("|", RogueRuntimeConstants.SaveVersion, B(dto.RunId), dto.Seed, B(dto.StageId), dto.StageTime,
                dto.Gold, dto.StageContribution, dto.CurrentHealth, dto.CurrentMana, B(spells), B(equipped), B(slots), B(equipment),
                B(tactical), B(JoinStrings(dto.ItemQuickbarInstanceIds)), dto.DeterministicCounter, B(JoinStrings(dto.PendingRewardIds)),
                B(JoinStrings(dto.ReselectionClaimIds)), B(dto.MigrationReportId), B(dto.CurrentNodeId), B(dto.RegionBossId), B(dto.StarterId),
                B(JoinStrings(dto.VisitedNodeIds)), B(JoinStrings(dto.CompletedNodeIds)), B(JoinStrings(dto.ClaimedContentIds)),
                dto.AwaitingReward ? 1 : 0, B(dto.PendingContentChoiceId), B(dto.PendingContentCombatMissionId),
                B(JoinStrings(dto.EncounterAssignments)), B(JoinStrings(dto.NodeContentAssignments)));
        }

        public static RogueRunDto Deserialize(string data)
        {
            string[] fields = (data ?? string.Empty).Split('|');
            if ((fields.Length != 28 && fields.Length != 29 && fields.Length != 30) || fields[0] != RogueRuntimeConstants.SaveVersion) throw new InvalidOperationException("Unsupported or invalid rogue11 save.");
            RogueRunDto dto = new RogueRunDto
            {
                RunId = U(fields[1]), Seed = I(fields[2]), StageId = U(fields[3]), StageTime = I(fields[4]), Gold = I(fields[5]),
                StageContribution = I(fields[6]), CurrentHealth = I(fields[7]), CurrentMana = I(fields[8]), DeterministicCounter = I(fields[15]),
                MigrationReportId = U(fields[18]), CurrentNodeId = U(fields[19]), RegionBossId = U(fields[20]), StarterId = U(fields[21]),
                AwaitingReward = I(fields[25]) == 1, PendingContentChoiceId = U(fields[26]), PendingContentCombatMissionId = U(fields[27])
            };
            dto.MasteredSpellIds.AddRange(SplitStrings(U(fields[9])));
            CopyExact(SplitStrings(U(fields[10])), dto.EquippedSpellIds, RogueRuntimeConstants.SpellSlotCount, "spell slots");
            ParseSlots(U(fields[11]), dto);
            ParseEquipment(U(fields[12]), dto);
            ParseTactical(U(fields[13]), dto);
            CopyExact(SplitStrings(U(fields[14])), dto.ItemQuickbarInstanceIds, RogueRuntimeConstants.ItemQuickbarSize, "quickbar");
            dto.PendingRewardIds.AddRange(SplitStrings(U(fields[16])));
            dto.ReselectionClaimIds.AddRange(SplitStrings(U(fields[17])));
            dto.VisitedNodeIds.AddRange(SplitStrings(U(fields[22])));
            dto.CompletedNodeIds.AddRange(SplitStrings(U(fields[23])));
            dto.ClaimedContentIds.AddRange(SplitStrings(U(fields[24])));
            if (fields.Length >= 29) dto.EncounterAssignments.AddRange(SplitStrings(U(fields[28])));
            if (fields.Length == 30) dto.NodeContentAssignments.AddRange(SplitStrings(U(fields[29])));
            ValidateShape(dto);
            return dto;
        }

        private static void ParseSlots(string raw, RogueRunDto dto)
        {
            string[] rows = Rows(raw);
            if (rows.Length != dto.EquipmentSlotInstanceIds.Count) throw new InvalidOperationException("rogue11 equipment slot set is incomplete.");
            foreach (string row in rows)
            {
                string[] fields = row.Split(',');
                EquipmentSlot slot;
                if (fields.Length != 2 || !Enum.TryParse(fields[0], out slot)) throw new InvalidOperationException("Invalid equipment slot row.");
                dto.EquipmentSlotInstanceIds[slot] = U(fields[1]);
            }
        }

        private static void ParseEquipment(string raw, RogueRunDto dto)
        {
            foreach (string row in Rows(raw))
            {
                string[] f = row.Split(',');
                EquipmentSlot slot; EquipmentRarity rarity;
                if (f.Length != 16 || !Enum.TryParse(f[2], out slot) || !Enum.TryParse(f[3], out rarity)) throw new InvalidOperationException("Invalid equipment instance row.");
                EquipmentInstanceDto value = new EquipmentInstanceDto(U(f[0]), U(f[1]), slot, rarity, I(f[4]))
                { ReforgeCount = I(f[5]), ResolvedWeight = I(f[6]), ResolvedAetherLoad = I(f[7]), SourceStage = U(f[8]), SourceType = U(f[9]), AcquiredOrder = I(f[10]), BackpackX = I(f[13]), BackpackY = I(f[14]), BackpackRotated = I(f[15]) == 1 };
                value.MutableAffixIds.AddRange(SplitTilde(U(f[11]))); value.UpgradeBranchIds.AddRange(SplitTilde(U(f[12]))); dto.EquipmentInstances.Add(value);
            }
        }

        private static void ParseTactical(string raw, RogueRunDto dto)
        {
            foreach (string row in Rows(raw))
            {
                string[] f = row.Split(',');
                if (f.Length != 9) throw new InvalidOperationException("Invalid tactical item row.");
                dto.TacticalItemInstances.Add(new TacticalItemInstanceDto { InstanceId = U(f[0]), DefinitionId = U(f[1]), X = I(f[2]), Y = I(f[3]), Rotated = I(f[4]) == 1, ChargesCurrent = I(f[5]), ChargesMaximum = I(f[6]), SourceStage = U(f[7]), SourceType = U(f[8]) });
            }
        }

        private static void ValidateShape(RogueRunDto dto)
        {
            if (dto.SaveVersion != RogueRuntimeConstants.SaveVersion || string.IsNullOrWhiteSpace(dto.RunId)) throw new InvalidOperationException("Invalid rogue11 identity.");
            if (dto.EquippedSpellIds.Length != RogueRuntimeConstants.SpellSlotCount || dto.ItemQuickbarInstanceIds.Length != RogueRuntimeConstants.ItemQuickbarSize) throw new InvalidOperationException("Invalid rogue11 slot count.");
            if (dto.EquipmentSlotInstanceIds.Count != Enum.GetValues(typeof(EquipmentSlot)).Length) throw new InvalidOperationException("Invalid rogue11 equipment slot set.");
            if (dto.Gold < 0 || dto.StageContribution < 0 || dto.CurrentHealth <= 0 || dto.CurrentMana < 0) throw new InvalidOperationException("Invalid rogue11 resource state.");
        }

        private static string JoinStrings(IEnumerable<string> values) => string.Join(",", (values ?? Array.Empty<string>()).Select(B));
        private static string[] SplitStrings(string value) => string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(new[] { ',' }, StringSplitOptions.None).Select(U).ToArray();
        private static string[] SplitTilde(string value) => string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split('~');
        private static string[] Rows(string value) => string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(';');
        private static int I(string value) => int.Parse(value);
        private static string B(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        private static string U(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
        private static void CopyExact(string[] source, string[] target, int count, string label)
        { if (source.Length != count) throw new InvalidOperationException("Invalid " + label + " count."); Array.Copy(source, target, count); }
    }
}
