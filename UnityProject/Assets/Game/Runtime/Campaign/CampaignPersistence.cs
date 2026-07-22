using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class SaveSlot
    {
        public int SlotId { get; }
        public string Data { get; private set; }
        public string Label { get; private set; }
        public SaveSlot(int slotId, string data, string label) { SlotId = slotId; Data = data; Label = label ?? string.Empty; }
        public void Replace(string data, string label) { Data = data; Label = label ?? string.Empty; }
    }

    public sealed class CampaignSaveManager
    {
        private readonly Dictionary<int, SaveSlot> slots = new Dictionary<int, SaveSlot>();
        private readonly Dictionary<int, SaveSlot> backups = new Dictionary<int, SaveSlot>();
        public void Save(int slotId, CampaignState state, string label = "manual") { if (state == null) throw new ArgumentNullException(nameof(state)); slots[slotId] = new SaveSlot(slotId, state.ToJson(), label); }
        public void Backup(int slotId, CampaignState state) { SaveBackup(slotId, state, "backup"); }
        private void SaveBackup(int slotId, CampaignState state, string label) { backups[slotId] = new SaveSlot(slotId, state.ToJson(), label); }
        public CampaignState Load(int slotId) => slots.TryGetValue(slotId, out var save) ? CampaignState.FromJson(save.Data) : throw new KeyNotFoundException("Save slot not found.");
        public CampaignState LoadBackup(int slotId) => backups.TryGetValue(slotId, out var save) ? CampaignState.FromJson(save.Data) : throw new KeyNotFoundException("Backup not found.");
        public bool HasSave(int slotId) => slots.ContainsKey(slotId);
        public bool HasBackup(int slotId) => backups.ContainsKey(slotId);
    }

    public sealed class MissionPreparation
    {
        public string MissionId { get; private set; }
        public string RulesSummary { get; private set; }
        public string EnemySummary { get; private set; }
        public MissionPreparation Configure(string missionId, string rulesSummary, string enemySummary) { MissionId = missionId; RulesSummary = rulesSummary; EnemySummary = enemySummary; return this; }
        public MissionPreparation Clone() => new MissionPreparation().Configure(MissionId, RulesSummary, EnemySummary);
    }
}
