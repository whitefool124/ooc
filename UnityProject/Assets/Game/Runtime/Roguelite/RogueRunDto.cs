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
        public string ForgeMaterialId { get; set; } = string.Empty;

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
        public List<string> RouteHistoryNodeIds { get; } = new List<string>();
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
        public Dictionary<EquipmentSlot, string> EquipmentSlotInstanceIds { get; } = EquipmentSlotRules.ActiveSlots.ToDictionary(value => value, value => string.Empty);
        public List<TacticalItemInstanceDto> TacticalItemInstances { get; } = new List<TacticalItemInstanceDto>();
        public string[] ItemQuickbarInstanceIds { get; } = new string[RogueRuntimeConstants.ItemQuickbarSize];
        public int DeterministicCounter { get; set; }
        public List<string> PendingRewardIds { get; } = new List<string>();
        public List<string> RolledRewardChoiceIds { get; } = new List<string>();
        public List<string> PendingRewardFollowupIds { get; } = new List<string>();
        public string PendingRewardStepId { get; set; } = string.Empty;
        public string PendingResourceReceipt { get; set; } = string.Empty;
        public List<string> ReselectionClaimIds { get; } = new List<string>();
        public string MigrationReportId { get; set; } = string.Empty;
        public List<string> EncounterAssignments { get; } = new List<string>();
        public List<string> NodeContentAssignments { get; } = new List<string>();
        public List<string> GeneratedAcademyMapRows { get; } = new List<string>();
        public bool DeparturePending { get; set; }
        public int AcademyFoodCount { get; set; }
        public int ForgeMaterialCount { get; set; }
        public int SpecializationMaterialCount { get; set; }
        public List<string> MaterialStockRows { get; } = new List<string>();
        public List<string> PendingFixedMaterialIds { get; } = new List<string>();
        public string PendingMainRewardId { get; set; } = string.Empty;
        public int PendingRewardGold { get; set; }
        public int PendingRewardContribution { get; set; }
        public List<string> MaterialPlacementRows { get; } = new List<string>();
        public List<string> LayerServiceRows { get; } = new List<string>();
        public List<string> SettledServiceNodeIds { get; } = new List<string>();
        public string RunProgramId { get; set; } = string.Empty;
        public string RunEndReason { get; set; } = string.Empty;
        public List<string> CombatJournalRows { get; } = new List<string>();
        public string ActiveCombatNodeId { get; set; } = string.Empty;
        public OCC.Combat.FirstRunExperienceState FirstRunExperience { get; set; }

        public static RogueRunDto CreateNew(string runId, int seed)
        {
            RogueRunDto dto = new RogueRunDto { RunId = runId, Seed = seed, Gold = 8, StageContribution = 0, CurrentHealth = UnitState.HeroBaseHealth, CurrentMana = 12 };
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
            OCC.Combat.AcademyMapSaveMigration.Normalize(dto);
            ValidateShape(dto);
            string spells = JoinStrings(dto.MasteredSpellIds);
            string equipped = JoinStrings(dto.EquippedSpellIds);
            string slots = string.Join(";", EquipmentSlotRules.ActiveSlots.Select(slot => slot + "," + B(dto.EquipmentSlotInstanceIds[slot])));
            string equipment = string.Join(";", dto.EquipmentInstances.OrderBy(value => value.AcquiredOrder).ThenBy(value => value.InstanceId, StringComparer.Ordinal).Select(value =>
                string.Join(",", B(value.InstanceId), B(value.DefinitionId), value.EquippedSlot, value.Rarity, value.PowerBand,
                    value.ReforgeCount, value.ResolvedWeight, value.ResolvedAetherLoad, B(value.SourceStage), B(value.SourceType), value.AcquiredOrder,
                    B(string.Join("~", value.MutableAffixIds)), B(string.Join("~", value.UpgradeBranchIds)), value.BackpackX, value.BackpackY,
                    value.BackpackRotated ? 1 : 0, B(value.ForgeMaterialId))));
            string tactical = string.Join(";", dto.TacticalItemInstances.OrderBy(value => value.InstanceId, StringComparer.Ordinal).Select(value =>
                string.Join(",", B(value.InstanceId), B(value.DefinitionId), value.X, value.Y, value.Rotated ? 1 : 0,
                    value.ChargesCurrent, value.ChargesMaximum, B(value.SourceStage), B(value.SourceType))));
            string legacyFields = string.Join("|", RogueRuntimeConstants.SaveVersion, B(dto.RunId), dto.Seed, B(dto.StageId), dto.StageTime,
                dto.Gold, dto.StageContribution, dto.CurrentHealth, dto.CurrentMana, B(spells), B(equipped), B(slots), B(equipment),
                B(tactical), B(JoinStrings(dto.ItemQuickbarInstanceIds)), dto.DeterministicCounter, B(JoinStrings(dto.PendingRewardIds)),
                B(JoinStrings(dto.ReselectionClaimIds)), B(dto.MigrationReportId), B(dto.CurrentNodeId), B(dto.RegionBossId), B(dto.StarterId),
                B(JoinStrings(dto.VisitedNodeIds)), B(JoinStrings(dto.CompletedNodeIds)), B(JoinStrings(dto.ClaimedContentIds)),
                dto.AwaitingReward ? 1 : 0, B(dto.PendingContentChoiceId), B(dto.PendingContentCombatMissionId),
                B(JoinStrings(dto.EncounterAssignments)), B(JoinStrings(dto.NodeContentAssignments)));
            string settledServices = B(JoinStrings(dto.SettledServiceNodeIds));
            return legacyFields + "|" + B(dto.RunProgramId) + "|" +
                B(OCC.Combat.FirstRunExperienceCodec.Serialize(dto.FirstRunExperience)) + "|" +
                settledServices + "|" + B(JoinStrings(dto.RouteHistoryNodeIds)) + "|" + B(dto.RunEndReason) +
                "|" + B(JoinStrings(dto.CombatJournalRows)) + "|" + B(dto.ActiveCombatNodeId) +
                "|" + B(JoinStrings(dto.RolledRewardChoiceIds)) + "|" + B(dto.PendingResourceReceipt) +
                "|" + B(JoinStrings(dto.GeneratedAcademyMapRows)) + "|" + (dto.DeparturePending ? "1" : "0") +
                "|" + dto.AcademyFoodCount + "|" + dto.ForgeMaterialCount + "|" + dto.SpecializationMaterialCount +
                "|" + B(JoinStrings(dto.LayerServiceRows)) + "|" + B(JoinStrings(dto.PendingRewardFollowupIds)) +
                "|" + B(dto.PendingRewardStepId) + "|" + B(JoinStrings(dto.MaterialStockRows)) +
                "|" + B(JoinStrings(dto.PendingFixedMaterialIds)) + "|" + B(dto.PendingMainRewardId) +
                "|" + dto.PendingRewardGold + "|" + dto.PendingRewardContribution +
                "|" + B(JoinStrings(dto.MaterialPlacementRows));
        }

        public static RogueRunDto Deserialize(string data)
        {
            string[] fields = (data ?? string.Empty).Split('|');
            if ((fields.Length < 28 || fields.Length > 53) || fields[0] != RogueRuntimeConstants.SaveVersion) throw new InvalidOperationException("Unsupported or invalid rogue11 save.");
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
            if (fields.Length >= 30) dto.NodeContentAssignments.AddRange(SplitStrings(U(fields[29])));
            if (fields.Length >= 32)
            {
                dto.RunProgramId = U(fields[30]);
                dto.FirstRunExperience = OCC.Combat.FirstRunExperienceCodec.Deserialize(U(fields[31]));
            }
            if (fields.Length == 31) dto.SettledServiceNodeIds.AddRange(SplitStrings(U(fields[30])));
            if (fields.Length >= 33) dto.SettledServiceNodeIds.AddRange(SplitStrings(U(fields[32])));
            if (fields.Length >= 34) dto.RouteHistoryNodeIds.AddRange(SplitStrings(U(fields[33])));
            if (fields.Length >= 35) dto.RunEndReason = U(fields[34]);
            if (fields.Length >= 36) dto.CombatJournalRows.AddRange(SplitStrings(U(fields[35])));
            if (fields.Length >= 37) dto.ActiveCombatNodeId = U(fields[36]);
            if (fields.Length >= 38) dto.RolledRewardChoiceIds.AddRange(SplitStrings(U(fields[37])));
            if (fields.Length >= 39) dto.PendingResourceReceipt = U(fields[38]);
            if (fields.Length >= 40) dto.GeneratedAcademyMapRows.AddRange(SplitStrings(U(fields[39])));
            if (fields.Length >= 41)
            {
                if (fields[40] != "0" && fields[40] != "1") throw new InvalidOperationException("Invalid departure state.");
                dto.DeparturePending = fields[40] == "1";
            }
            if (fields.Length >= 42) dto.AcademyFoodCount = I(fields[41]);
            if (fields.Length >= 43) dto.ForgeMaterialCount = I(fields[42]);
            if (fields.Length >= 44) dto.SpecializationMaterialCount = I(fields[43]);
            if (fields.Length >= 45) dto.LayerServiceRows.AddRange(SplitStrings(U(fields[44])));
            if (fields.Length >= 46) dto.PendingRewardFollowupIds.AddRange(SplitStrings(U(fields[45])));
            if (fields.Length >= 47) dto.PendingRewardStepId = U(fields[46]);
            if (fields.Length >= 48) dto.MaterialStockRows.AddRange(SplitStrings(U(fields[47])));
            if (fields.Length >= 49) dto.PendingFixedMaterialIds.AddRange(SplitStrings(U(fields[48])));
            if (fields.Length >= 50) dto.PendingMainRewardId = U(fields[49]);
            if (fields.Length >= 51) dto.PendingRewardGold = I(fields[50]);
            if (fields.Length >= 52) dto.PendingRewardContribution = I(fields[51]);
            if (fields.Length >= 53) dto.MaterialPlacementRows.AddRange(SplitStrings(U(fields[52])));
            OCC.Combat.AcademyMapSaveMigration.Normalize(dto);
            ValidateShape(dto);
            return dto;
        }

        private static void ParseSlots(string raw, RogueRunDto dto)
        {
            string[] rows = Rows(raw);
            if (rows.Length != 9 && rows.Length != 11) throw new InvalidOperationException("rogue11 equipment slot set is incomplete.");
            foreach (string row in rows)
            {
                string[] fields = row.Split(',');
                EquipmentSlot slot;
                if (fields.Length != 2 || !Enum.TryParse(fields[0], out slot)) throw new InvalidOperationException("Invalid equipment slot row.");
                EquipmentSlot normalized = EquipmentSlotRules.NormalizeLegacy(slot);
                string instanceId = U(fields[1]);
                if (normalized == EquipmentSlot.None || string.IsNullOrEmpty(instanceId)) continue;
                if (dto.EquipmentSlotInstanceIds.ContainsKey(normalized) && string.IsNullOrEmpty(dto.EquipmentSlotInstanceIds[normalized]))
                    dto.EquipmentSlotInstanceIds[normalized] = instanceId;
            }
        }

        private static void ParseEquipment(string raw, RogueRunDto dto)
        {
            foreach (string row in Rows(raw))
            {
                string[] f = row.Split(',');
                EquipmentSlot slot; EquipmentRarity rarity;
                if ((f.Length != 16 && f.Length != 17) || !Enum.TryParse(f[2], out slot) || !Enum.TryParse(f[3], out rarity)) throw new InvalidOperationException("Invalid equipment instance row.");
                EquipmentInstanceDto value = new EquipmentInstanceDto(U(f[0]), U(f[1]), EquipmentSlotRules.NormalizeLegacy(slot), rarity, I(f[4]))
                { ReforgeCount = I(f[5]), ResolvedWeight = I(f[6]), ResolvedAetherLoad = I(f[7]), SourceStage = U(f[8]), SourceType = U(f[9]), AcquiredOrder = I(f[10]), BackpackX = I(f[13]), BackpackY = I(f[14]), BackpackRotated = I(f[15]) == 1 };
                value.ForgeMaterialId = f.Length == 17 ? U(f[16]) : string.Empty;
                if (value.ForgeMaterialId != string.Empty && value.ForgeMaterialId != "FORGE-LOAD" && value.ForgeMaterialId != "FORGE-CIRCUIT")
                    throw new InvalidOperationException("Invalid equipment forge material.");
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
            if (dto.EquipmentSlotInstanceIds.Count != EquipmentSlotRules.ActiveSlots.Count || dto.EquipmentSlotInstanceIds.Keys.Any(value => !EquipmentSlotRules.IsActive(value)))
                throw new InvalidOperationException("Invalid rogue11 equipment slot set.");
            bool sealedDefeat = dto.FirstRunExperience != null && dto.FirstRunExperience.RunSealed &&
                dto.FirstRunExperience.Outcome == OCC.Combat.FirstRunOutcome.EliteDefeat;
            bool death = dto.RunEndReason == "death";
            if (dto.RunEndReason != string.Empty && dto.RunEndReason != "death" && dto.RunEndReason != "abandon" && dto.RunEndReason != "victory")
                throw new InvalidOperationException("Invalid rogue11 run end reason.");
            if (dto.CombatJournalRows.Count > 2000)
                throw new InvalidOperationException("Combat journal is too large.");
            if (dto.RolledRewardChoiceIds.Count > 3 ||
                dto.RolledRewardChoiceIds.Distinct(StringComparer.Ordinal).Count() != dto.RolledRewardChoiceIds.Count)
                throw new InvalidOperationException("Invalid rolled reward choices.");
            if (dto.PendingRewardFollowupIds.Count > 3 ||
                dto.PendingRewardFollowupIds.Distinct(StringComparer.Ordinal).Count() != dto.PendingRewardFollowupIds.Count ||
                dto.PendingRewardStepId != string.Empty && dto.PendingRewardStepId != "main" && dto.PendingRewardStepId != "followup")
                throw new InvalidOperationException("Invalid academy reward steps.");
            if (!string.IsNullOrEmpty(dto.PendingResourceReceipt))
                OCC.Combat.AcademyResourceReceipt.Decode(dto.PendingResourceReceipt);
            foreach (string row in dto.CombatJournalRows) OCC.Combat.CombatJournalEntry.Decode(row);
            if (dto.CombatJournalRows.Count > 0 && string.IsNullOrEmpty(dto.ActiveCombatNodeId))
                throw new InvalidOperationException("Combat journal has no active node.");
            if (dto.Gold < 0 || dto.StageContribution < 0 || dto.CurrentHealth < 0 || (!sealedDefeat && !death && dto.CurrentHealth == 0) || dto.CurrentMana < 0)
                throw new InvalidOperationException("Invalid rogue11 resource state.");
            if (dto.FirstRunExperience != null && !string.Equals(dto.RunProgramId, OCC.Combat.RogueliteRunProgram.FirstRunV1.ToString(), StringComparison.Ordinal))
                throw new InvalidOperationException("First-run snapshot requires the first-run program id.");
            if (dto.GeneratedAcademyMapRows.Count > 0) OCC.Combat.RogueliteAcademyMapGenerator.Decode(dto.GeneratedAcademyMapRows);
            OCC.Combat.RogueliteMapRun.ValidateLayerServiceRows(dto.LayerServiceRows);
            OCC.Combat.RogueliteMapRun.ValidateAcademyMaterialRows(dto.MaterialStockRows);
            if (dto.MaterialPlacementRows.Any(row =>
            {
                string[] parts = row.Split(',');
                return parts.Length != 3 || !parts[0].StartsWith("material:", StringComparison.Ordinal) ||
                    !int.TryParse(parts[1], out int x) || x < 0 ||
                    !int.TryParse(parts[2], out int y) || y < 0;
            }) || dto.MaterialPlacementRows.Select(row => row.Split(',')[0]).Distinct(StringComparer.Ordinal).Count() != dto.MaterialPlacementRows.Count)
                throw new InvalidOperationException("Invalid material backpack placements.");
            if (dto.PendingFixedMaterialIds.Any(id => id != OCC.Combat.AcademyBattleRewardCatalog.ForgeLoad &&
                id != OCC.Combat.AcademyBattleRewardCatalog.ForgeCircuit &&
                id != OCC.Combat.AcademyBattleRewardCatalog.SpecAmplify &&
                id != OCC.Combat.AcademyBattleRewardCatalog.SpecEfficient))
                throw new InvalidOperationException("Invalid pending academy materials.");
            if (dto.PendingFixedMaterialIds.Count > 0 && (!dto.AwaitingReward || string.IsNullOrEmpty(dto.PendingRewardStepId)))
                throw new InvalidOperationException("Pending academy materials require an unresolved reward.");
            if (!string.IsNullOrEmpty(dto.PendingMainRewardId) && (!dto.AwaitingReward || dto.PendingRewardStepId != "followup"))
                throw new InvalidOperationException("Pending academy main choice requires a followup reward.");
            if (dto.PendingRewardGold < 0 || dto.PendingRewardContribution < 0 ||
                (dto.PendingRewardGold > 0 || dto.PendingRewardContribution > 0) &&
                (!dto.AwaitingReward || string.IsNullOrEmpty(dto.PendingRewardStepId)))
                throw new InvalidOperationException("Invalid pending academy reward currency.");
            if (dto.AcademyFoodCount < 0 || dto.ForgeMaterialCount < 0 || dto.SpecializationMaterialCount < 0 ||
                (dto.DeparturePending && (dto.FirstRunExperience != null || dto.RunProgramId != OCC.Combat.RogueliteRunProgram.EvergreenAcademy.ToString())))
                throw new InvalidOperationException("Invalid academy departure resources or program.");
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
