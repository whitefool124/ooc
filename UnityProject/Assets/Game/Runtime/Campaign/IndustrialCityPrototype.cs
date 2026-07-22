using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class IndustrialCityPrototype
    {
        public CampaignState State { get; }
        public IReadOnlyList<string> HubServices { get; } = new[] { "workshop", "shop", "training", "storage", "travel", "mission_board" };
        public IndustrialCityPrototype()
        {
            State = new CampaignState("central_depot");
            var factory = new LocationState("ether_factory", true); factory.AddService("mission");
            var market = new LocationState("rail_market", true); market.AddService("shop");
            var training = new LocationState("drill_yard", true); training.AddService("training");
            var archive = new LocationState("sealed_archive"); archive.AddService("investigation");
            State.AddLocation(factory); State.AddLocation(market); State.AddLocation(training); State.AddLocation(archive);
            State.AddRoute("central_depot", "ether_factory"); State.AddRoute("central_depot", "rail_market"); State.AddRoute("central_depot", "drill_yard");
        }
        public void RevealArchive() { State.Discover("sealed_archive"); State.AddRoute("ether_factory", "sealed_archive"); }
        public void CompleteEngineerSideStory() { State.Locations["ether_factory"].SetStatus("stabilized"); State.Locations["central_depot"].SetStatus("workshop_discount"); State.SetQuest("engineer_side_story", "complete"); }
    }

    public sealed class TaskTemplate
    {
        public string Id { get; }
        public CombatObjectiveType Type { get; }
        public string MapId { get; }
        public IReadOnlyList<string> InteractionIds { get; }
        public TaskTemplate(string id, CombatObjectiveType type, string mapId, IEnumerable<string> interactionIds)
        { Id = id; Type = type; MapId = mapId; InteractionIds = (interactionIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray(); }
    }

    public static class TaskTemplateCatalog
    {
        public static IReadOnlyList<TaskTemplate> All { get; } = new[]
        {
            new TaskTemplate("elimination_rail", CombatObjectiveType.Elimination, "rail_yard", new[] { "cover_a" }),
            new TaskTemplate("destruction_factory", CombatObjectiveType.Destruction, "ether_factory", new[] { "relay" }),
            new TaskTemplate("rescue_archive", CombatObjectiveType.Rescue, "sealed_archive", new[] { "captive" }),
            new TaskTemplate("capture_market", CombatObjectiveType.Capture, "rail_market", new[] { "control_node" }),
            new TaskTemplate("extraction_drill", CombatObjectiveType.Extraction, "drill_yard", new[] { "evac_pad" }),
            new TaskTemplate("investigation_archive", CombatObjectiveType.Investigation, "sealed_archive", new[] { "evidence" })
        };
        public static void ValidateNoDuplicateCombinations()
        {
            if (All.GroupBy(t => t.MapId + "|" + t.Type + "|" + string.Join(",", t.InteractionIds)).Any(g => g.Count() > 1)) throw new InvalidOperationException("Task template combination is duplicated.");
        }
    }
}
