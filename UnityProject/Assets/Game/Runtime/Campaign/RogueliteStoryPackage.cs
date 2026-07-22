using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class RogueliteStoryPackage
    {
        private readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> completedMissions = new List<string>();
        public string PackageId { get; }
        public int Seed { get; }
        public string CharacterId { get; }
        public int CurrentMissionIndex { get; private set; }
        public IReadOnlyList<string> MissionChain { get; }
        public IReadOnlyList<string> CompletedMissions => completedMissions;
        public IReadOnlyCollection<string> UnlockedContent => unlocked;
        public string SettlementSummary { get; private set; } = string.Empty;

        public RogueliteStoryPackage(string packageId, string characterId, int seed, IEnumerable<string> missionChain)
        {
            PackageId = string.IsNullOrEmpty(packageId) ? throw new ArgumentException("Package id required.", nameof(packageId)) : packageId;
            CharacterId = string.IsNullOrEmpty(characterId) ? throw new ArgumentException("Character id required.", nameof(characterId)) : characterId;
            Seed = seed;
            MissionChain = (missionChain ?? throw new ArgumentNullException(nameof(missionChain))).Where(id => !string.IsNullOrEmpty(id)).ToArray();
            if (MissionChain.Count == 0) throw new ArgumentException("At least one mission is required.", nameof(missionChain));
        }

        public string CurrentMissionId => CurrentMissionIndex < MissionChain.Count ? MissionChain[CurrentMissionIndex] : null;
        public bool IsComplete => CurrentMissionIndex >= MissionChain.Count;
        public void CompleteCurrentMission(string summary, IEnumerable<string> unlocks = null)
        {
            if (IsComplete) throw new InvalidOperationException("Story package is already complete.");
            completedMissions.Add(CurrentMissionId); CurrentMissionIndex++;
            if (unlocks != null) foreach (string item in unlocks) if (!string.IsNullOrEmpty(item)) unlocked.Add(item);
            SettlementSummary = summary ?? string.Empty;
        }
        public RogueliteStoryPackage Clone()
        {
            var clone = new RogueliteStoryPackage(PackageId, CharacterId, Seed, MissionChain) { CurrentMissionIndex = CurrentMissionIndex, SettlementSummary = SettlementSummary };
            clone.completedMissions.AddRange(completedMissions); foreach (string item in unlocked) clone.unlocked.Add(item); return clone;
        }
        public string ToJson() => string.Join("|", "rogue1", PackageId, CharacterId, Seed, CurrentMissionIndex, string.Join(",", MissionChain), string.Join(",", completedMissions), string.Join(",", unlocked), SettlementSummary.Replace("|", "/"));
        public static RogueliteStoryPackage FromJson(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
            if (parts.Length != 9 || parts[0] != "rogue1") throw new InvalidOperationException("Unsupported roguelite story save version.");
            var package = new RogueliteStoryPackage(parts[1], parts[2], int.Parse(parts[3]), parts[5].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) { CurrentMissionIndex = int.Parse(parts[4]), SettlementSummary = parts[8] };
            if (parts[6].Length > 0) package.completedMissions.AddRange(parts[6].Split(','));
            if (parts[7].Length > 0) foreach (string item in parts[7].Split(',')) package.unlocked.Add(item);
            return package;
        }
    }

    public static class RogueliteStoryCatalog
    {
        public static RogueliteStoryPackage CreateDefault(int seed) => new RogueliteStoryPackage("iron_echoes", "nara_veld", seed, new[] { "dead_signal", "factory_breach", "last_conduit" });
    }
}
