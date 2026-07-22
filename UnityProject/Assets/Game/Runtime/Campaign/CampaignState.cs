using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum SaveVersion { V1 = 1 }

    public sealed class LocationState
    {
        private readonly HashSet<string> services = new HashSet<string>(StringComparer.Ordinal);
        public string Id { get; }
        public bool Discovered { get; private set; }
        public bool Visited { get; private set; }
        public string Status { get; private set; } = "default";
        public IReadOnlyCollection<string> Services => services;
        public LocationState(string id, bool discovered = false) { Id = string.IsNullOrEmpty(id) ? throw new ArgumentException("Location id required.", nameof(id)) : id; Discovered = discovered; }
        public void Discover() { Discovered = true; }
        public void Visit() { if (!Discovered) throw new InvalidOperationException("Location must be discovered before visiting."); Visited = true; }
        public void SetStatus(string status) { Status = string.IsNullOrEmpty(status) ? "default" : status; }
        public void AddService(string service) { if (!string.IsNullOrEmpty(service)) services.Add(service); }
        public LocationState Clone() { var clone = new LocationState(Id, Discovered) { Visited = Visited, Status = Status }; foreach (var service in services) clone.services.Add(service); return clone; }
    }

    public sealed class StoryState
    {
        private readonly Dictionary<string, string> flags = new Dictionary<string, string>(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, string> Flags => flags;
        public void Set(string key, string value) { if (string.IsNullOrEmpty(key)) throw new ArgumentException("Story key required.", nameof(key)); flags[key] = value ?? string.Empty; }
        public string Get(string key) => flags.TryGetValue(key, out var value) ? value : null;
        public StoryState Clone() { var clone = new StoryState(); foreach (var pair in flags) clone.flags[pair.Key] = pair.Value; return clone; }
    }

    public sealed class CampaignState
    {
        private readonly Dictionary<string, LocationState> locations = new Dictionary<string, LocationState>(StringComparer.Ordinal);
        private readonly HashSet<string> routes = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> quests = new Dictionary<string, string>(StringComparer.Ordinal);
        public SaveVersion Version { get; private set; } = SaveVersion.V1;
        public string CurrentLocationId { get; private set; }
        public int Credits { get; private set; }
        public int Aether { get; private set; }
        public int Injury { get; private set; }
        public int Contamination { get; private set; }
        public IReadOnlyDictionary<string, LocationState> Locations => locations;
        public IReadOnlyCollection<string> FastTravelRoutes => routes;
        public IReadOnlyDictionary<string, string> Quests => quests;
        public StoryState Story { get; private set; } = new StoryState();
        public CampaignState(string startingLocationId) { CurrentLocationId = string.IsNullOrEmpty(startingLocationId) ? throw new ArgumentException("Starting location required.", nameof(startingLocationId)) : startingLocationId; AddLocation(new LocationState(startingLocationId, true)); }
        public void AddLocation(LocationState location) { locations[location.Id] = location; }
        public void Discover(string id) { GetLocation(id).Discover(); }
        public void Visit(string id) { GetLocation(id).Visit(); CurrentLocationId = id; }
        public void AddRoute(string from, string to) { if (!locations.ContainsKey(from) || !locations.ContainsKey(to)) throw new InvalidOperationException("Both route endpoints must exist."); routes.Add(RouteKey(from, to)); routes.Add(RouteKey(to, from)); }
        public bool CanTravelTo(string id) => locations.TryGetValue(id, out var location) && location.Discovered && routes.Contains(RouteKey(CurrentLocationId, id));
        public void TravelTo(string id) { if (!CanTravelTo(id)) throw new InvalidOperationException("Location is not connected by a discovered route."); CurrentLocationId = id; locations[id].Visit(); }
        public void SetQuest(string id, string status) { quests[id] = status ?? string.Empty; }
        public void SetResources(int credits, int aether, int injury, int contamination) { Credits = credits; Aether = aether; Injury = injury; Contamination = contamination; }
        public CampaignState Clone() { var clone = new CampaignState(CurrentLocationId) { Version = Version, Credits = Credits, Aether = Aether, Injury = Injury, Contamination = Contamination, Story = Story.Clone() }; clone.locations.Clear(); foreach (var pair in locations) clone.locations[pair.Key] = pair.Value.Clone(); foreach (var route in routes) clone.routes.Add(route); foreach (var quest in quests) clone.quests[quest.Key] = quest.Value; return clone; }
        public string ToJson() { var sb = new StringBuilder(); sb.Append("v1|").Append(CurrentLocationId).Append('|').Append(Credits).Append(',').Append(Aether).Append(',').Append(Injury).Append(',').Append(Contamination).Append('|'); sb.Append(string.Join(";", locations.Values.Select(l => l.Id + "," + (l.Discovered ? 1 : 0) + "," + (l.Visited ? 1 : 0) + "," + l.Status))); sb.Append('|').Append(string.Join(";", routes)); sb.Append('|').Append(string.Join(";", quests.Select(q => q.Key + "=" + q.Value))); sb.Append('|').Append(string.Join(";", Story.Flags.Select(f => f.Key + "=" + f.Value))); return sb.ToString(); }
        public static CampaignState FromJson(string json) { if (string.IsNullOrEmpty(json)) throw new ArgumentException("Save data required.", nameof(json)); var parts = json.Split('|'); if (parts.Length != 7 || parts[0] != "v1") throw new InvalidOperationException("Unsupported save version."); var state = new CampaignState(parts[1]); var resources = parts[2].Split(',').Select(int.Parse).ToArray(); state.SetResources(resources[0], resources[1], resources[2], resources[3]); state.locations.Clear(); foreach (var item in parts[3].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { var values = item.Split(','); var location = new LocationState(values[0], values[1] == "1"); if (values[2] == "1") { location.Discover(); location.Visit(); } location.SetStatus(values[3]); state.locations.Add(location.Id, location); } foreach (var route in parts[4].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) state.routes.Add(route); foreach (var quest in parts[5].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { var pair = quest.Split('='); state.quests[pair[0]] = pair.Length > 1 ? pair[1] : string.Empty; } foreach (var flag in parts[6].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { var pair = flag.Split('='); state.Story.Set(pair[0], pair.Length > 1 ? pair[1] : string.Empty); } return state; }
        private LocationState GetLocation(string id) => locations.TryGetValue(id, out var location) ? location : throw new KeyNotFoundException(id);
        private static string RouteKey(string from, string to) => from + "->" + to;
    }
}
