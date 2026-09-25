using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>Creates and restores the saved first-stage academy graph. Template IDs stay stable for content lookup.</summary>
    public static class RogueliteAcademyMapGenerator
    {
        private static readonly string[] NormalPool =
        {
            "tutorial_hall", "dorm_watch", "archive_wing", "dorm_drill", "market_lane", "lecture_annex",
            "clinic_hall", "study_vault", "wilds_path", "seal_bridge", "sparring_ring", "workshop_yard"
        };
        private static readonly string[] ElitePool =
        { "supply_depot", "tower_foyer", "observatory_path", "wilds_camp" };
        private static readonly string[] FixedEvents =
        { "academy_gate", "field_infirmary", "tower_records", "tower_lift" };
        private static readonly string[] FixedServices =
        { "layer_workshop", "layer_medical", "layer_shop" };

        private static readonly IReadOnlyDictionary<string, int> Regions = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["academy_gate"] = 0, ["dorm_watch"] = 0, ["field_infirmary"] = 0, ["layer_medical"] = 0,
            ["tutorial_hall"] = 1, ["sparring_ring"] = 1,
            ["dorm_drill"] = 2, ["archive_wing"] = 2, ["workshop_yard"] = 2,
            ["supply_depot"] = 2, ["tower_foyer"] = 2, ["observatory_path"] = 2, ["layer_workshop"] = 2,
            ["market_lane"] = 3, ["lecture_annex"] = 3, ["layer_shop"] = 3,
            ["clinic_hall"] = 4, ["study_vault"] = 4, ["wilds_path"] = 4, ["wilds_camp"] = 4,
            ["seal_bridge"] = 5, ["tower_records"] = 5, ["tower_lift"] = 5,
            [RogueliteAcademyLayerCatalog.FinaleNodeId] = 6
        };

        public static IReadOnlyList<RogueliteMapNode> Generate(int seed)
        {
            var random = new Random(unchecked(seed ^ 0x4A617065));
            // Keep the two entry choices and one teaching and wilderness battle so every region has content.
            var chosen = new HashSet<string>(FixedEvents.Concat(FixedServices).Concat(new[] { "dorm_drill" }), StringComparer.Ordinal);
            chosen.Add(Choose(new[] { "tutorial_hall", "sparring_ring" }, random));
            chosen.Add(Choose(new[] { "clinic_hall", "study_vault", "wilds_path" }, random));
            foreach (string id in Shuffle(NormalPool.Where(id => !chosen.Contains(id)), random).Take(6)) chosen.Add(id);
            foreach (string id in Shuffle(ElitePool, random).Take(3)) chosen.Add(id);
            if (chosen.Count != 19) throw new InvalidOperationException("Academy map generation did not select 19 regular nodes.");

            var ordered = new List<string> { "academy_gate", "dorm_drill" };
            ordered.AddRange(Enumerable.Range(0, 6).SelectMany(region =>
                Shuffle(chosen.Where(id => id != "academy_gate" && id != "dorm_drill" && Regions[id] == region), random)));
            var links = chosen.ToDictionary(id => id, id => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            links[RogueliteAcademyLayerCatalog.FinaleNodeId] = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ordered.Count - 1; i++) Link(links, ordered[i], ordered[i + 1]);
            for (int i = 0; i + 4 < ordered.Count; i += 4) Link(links, ordered[i], ordered[i + 4]);
            for (int i = 0; i < ordered.Count - 2; i++)
                if (random.Next(2) == 0) Link(links, ordered[i], ordered[i + 2]);
            Link(links, ordered[ordered.Count - 1], RogueliteAcademyLayerCatalog.FinaleNodeId);
            Link(links, ordered[ordered.Count - 2], RogueliteAcademyLayerCatalog.FinaleNodeId);

            var regionRow = new Dictionary<int, int>();
            var nodes = new List<RogueliteMapNode>();
            foreach (string id in ordered.Concat(new[] { RogueliteAcademyLayerCatalog.FinaleNodeId }))
            {
                RogueliteAcademyLayerCatalog.TryResolveLayerNode(id, out RogueliteMapNode template);
                int region = Regions[id];
                int row = regionRow.TryGetValue(region, out int next) ? next : 0;
                regionRow[region] = row + 1;
                nodes.Add(new RogueliteMapNode(id, template.Type, template.DisplayName, template.Summary,
                    region, row, links[id].OrderBy(value => value, StringComparer.Ordinal).ToArray()));
            }
            return nodes;
        }

        public static IReadOnlyList<string> Encode(IReadOnlyList<RogueliteMapNode> nodes) =>
            (nodes ?? Array.Empty<RogueliteMapNode>()).Select(node =>
                node.Id + ":" + node.GridX + ":" + node.GridY + ":" + string.Join("~", node.NextIds)).ToArray();

        public static IReadOnlyList<RogueliteMapNode> Decode(IEnumerable<string> rows)
        {
            var nodes = new List<RogueliteMapNode>();
            foreach (string row in rows ?? Array.Empty<string>())
            {
                string[] fields = row.Split(':');
                if (fields.Length != 4 || !RogueliteAcademyLayerCatalog.TryResolveLayerNode(fields[0], out RogueliteMapNode template) ||
                    !int.TryParse(fields[1], out int x) || !int.TryParse(fields[2], out int y) ||
                    x < 0 || x > 6 || y < 0 || y > 8)
                    throw new InvalidOperationException("Invalid saved academy map node.");
                string[] neighbors = fields[3].Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
                nodes.Add(new RogueliteMapNode(fields[0], template.Type, template.DisplayName, template.Summary,
                    x, y, neighbors));
            }
            if (nodes.Count == 0) return nodes;
            if (nodes.Count != 20 || nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count() != 20 ||
                nodes.Select(node => node.GridX + ":" + node.GridY).Distinct(StringComparer.Ordinal).Count() != 20 ||
                !nodes.Any(node => node.Id == RogueliteAcademyLayerCatalog.FinaleNodeId) ||
                !nodes.Any(node => node.Id == "academy_gate") || !nodes.Any(node => node.Id == "dorm_drill") ||
                nodes.Count(node => node.Type == RogueliteMapNodeType.Combat) != 9 ||
                nodes.Count(node => node.Type == RogueliteMapNodeType.Elite) != 3 ||
                nodes.Count(node => node.Type == RogueliteMapNodeType.Event) != 4 ||
                nodes.Count(node => node.Type == RogueliteMapNodeType.Finale) != 1 ||
                !FixedEvents.Concat(FixedServices).All(id => nodes.Any(node => node.Id == id)))
                throw new InvalidOperationException("Saved academy map has invalid node coverage.");
            var byId = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            foreach (RogueliteMapNode node in nodes)
                foreach (string neighbor in node.NextIds)
                    if (neighbor == node.Id || !byId.TryGetValue(neighbor, out RogueliteMapNode other) ||
                        !other.NextIds.Contains(node.Id))
                        throw new InvalidOperationException("Saved academy map has an invalid edge.");
            var reachable = new HashSet<string>(StringComparer.Ordinal) { "academy_gate" };
            var pending = new Queue<string>(); pending.Enqueue("academy_gate");
            while (pending.Count > 0)
                foreach (string neighbor in byId[pending.Dequeue()].NextIds)
                    if (reachable.Add(neighbor)) pending.Enqueue(neighbor);
            if (reachable.Count != nodes.Count || !Enumerable.Range(0, 6).All(region => nodes.Any(node => Regions[node.Id] == region)))
                throw new InvalidOperationException("Saved academy map is disconnected or misses a region.");
            return nodes;
        }

        private static string Choose(IEnumerable<string> ids, Random random) => Shuffle(ids, random).First();
        private static string[] Shuffle(IEnumerable<string> ids, Random random) =>
            ids.OrderBy(_ => random.Next()).ThenBy(id => id, StringComparer.Ordinal).ToArray();
        private static void Link(IDictionary<string, HashSet<string>> links, string a, string b)
        { links[a].Add(b); links[b].Add(a); }
    }
}
