using System;
using System.Linq;

namespace OCC.Combat
{
    // Historical spellings exist only at the save boundary. They are never live resources or gates.
    internal static class AcademyMapSaveMigration
    {
        public static string NodeId(string id) => id == "permit_archive" ? "records_archive" : id;
        public static string ChoiceId(string id) => id == "EV16_permit" ? "EV16_assessment" : id;

        public static System.Collections.Generic.IEnumerable<string> HistoricalProgressMarkers =>
            AcademyNodeContentCatalog.Events.Select(value => "permit:" + value.Id).Concat(new[] { "permit:tower_lift" });

        public static bool IsRetiredProgressMarker(string id) => HistoricalProgressMarkers.Contains(id);

        public static void Normalize(OCC.Combat.Roguelite.RogueRunDto dto)
        {
            dto.CurrentNodeId = NodeId(dto.CurrentNodeId);
            dto.PendingContentChoiceId = ChoiceId(dto.PendingContentChoiceId);
            for (int i = 0; i < dto.VisitedNodeIds.Count; i++) dto.VisitedNodeIds[i] = NodeId(dto.VisitedNodeIds[i]);
            for (int i = 0; i < dto.CompletedNodeIds.Count; i++) dto.CompletedNodeIds[i] = NodeId(dto.CompletedNodeIds[i]);
            for (int i = 0; i < dto.EncounterAssignments.Count; i++) dto.EncounterAssignments[i] = Assignment(dto.EncounterAssignments[i]);
            for (int i = 0; i < dto.NodeContentAssignments.Count; i++) dto.NodeContentAssignments[i] = Assignment(dto.NodeContentAssignments[i]);
            for (int i = 0; i < dto.SettledServiceNodeIds.Count; i++) dto.SettledServiceNodeIds[i] = NodeId(dto.SettledServiceNodeIds[i]);
            dto.ClaimedContentIds.RemoveAll(IsRetiredProgressMarker);
        }

        private static string Assignment(string row)
        {
            int separator = row.IndexOf('=');
            return separator < 0 ? row : NodeId(row.Substring(0, separator)) + row.Substring(separator);
        }

        public static void ValidateHistoricalCounter(string[] fields)
        {
            string version = fields[0];
            int index = version == "map2" || version == "map3" || version == "map4" ? 5 :
                version == "map5" || version == "map6" || version == "map7" || version == "map8" ||
                version == "map9" || version == "map10" ? 6 : -1;
            if (index < 0 || fields.Length <= index) return;
            if (!int.TryParse(fields[index], out int retiredCount) || retiredCount < 0)
                throw new InvalidOperationException("Invalid historical resource counter.");
        }
    }
}
