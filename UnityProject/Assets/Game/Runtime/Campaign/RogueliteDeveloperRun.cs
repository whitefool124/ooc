using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum RogueliteLaunchKind { StoryChain, TemplateSandbox }

    public sealed class RogueliteMissionDefinition
    {
        public string Id { get; }
        public string TemplateId { get; }
        public CombatObjectiveType ObjectiveType { get; }
        public string ObjectiveSummary { get; }
        public string FailureSummary { get; }
        public string EnemySummary { get; }

        public RogueliteMissionDefinition(string id, string templateId, CombatObjectiveType objectiveType, string objectiveSummary, string failureSummary, string enemySummary)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)); TemplateId = templateId ?? throw new ArgumentNullException(nameof(templateId));
            ObjectiveType = objectiveType; ObjectiveSummary = objectiveSummary ?? string.Empty; FailureSummary = failureSummary ?? string.Empty; EnemySummary = enemySummary ?? string.Empty;
        }
    }

    public static class RogueliteDeveloperCatalog
    {
        private static readonly RogueliteMissionDefinition[] missions =
        {
            new RogueliteMissionDefinition("dead_signal", "elimination_rail", CombatObjectiveType.Elimination, "\u6e05\u9664\u963b\u65ad\u4fe1\u53f7\u7684\u654c\u5bf9\u5355\u4f4d", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u6b65\u67aa\u5175\u3001\u76fe\u536b\u3001\u706b\u672f\u5e08\u3001\u7a81\u88ad\u8005\u3001\u5148\u950b"),
            new RogueliteMissionDefinition("factory_breach", "destruction_factory", CombatObjectiveType.Destruction, "\u7834\u574f\u4ee5\u592a\u4e2d\u7ee7\u5668", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u6b65\u67aa\u5175\u3001\u76fe\u536b\u3001\u706b\u672f\u5e08\u3001\u7a81\u88ad\u8005\u3001\u5148\u950b"),
            new RogueliteMissionDefinition("last_conduit", "elimination_rail", CombatObjectiveType.Elimination, "\u6e05\u9664\u5b88\u5907\u90e8\u961f\u5e76\u5b8c\u6210\u6545\u4e8b\u5305", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u6b65\u67aa\u5175\u3001\u76fe\u536b\u3001\u706b\u672f\u5e08\u3001\u7a81\u88ad\u8005\u3001\u5148\u950b"),
            new RogueliteMissionDefinition("sandbox_elimination", "elimination_rail", CombatObjectiveType.Elimination, "\u6e05\u9664\u5168\u90e8\u654c\u5bf9\u5355\u4f4d", "\u4e3b\u89d2\u5931\u80fd\u5219\u6f14\u7ec3\u5931\u8d25", "\u6807\u51c6\u6f14\u7ec3\u7f16\u6210"),
            new RogueliteMissionDefinition("sandbox_destruction", "destruction_factory", CombatObjectiveType.Destruction, "\u7834\u574f\u5730\u56fe\u4e2d\u7684\u4e2d\u7ee7\u5668", "\u4e3b\u89d2\u5931\u80fd\u5219\u6f14\u7ec3\u5931\u8d25", "\u6807\u51c6\u6f14\u7ec3\u7f16\u6210")
        };

        public static IReadOnlyList<TaskTemplate> OpenSandboxTemplates => TaskTemplateCatalog.All.Where(template => template.Type == CombatObjectiveType.Elimination || template.Type == CombatObjectiveType.Destruction).ToArray();
        public static RogueliteMissionDefinition FindMission(string id) => missions.FirstOrDefault(mission => mission.Id == id) ?? throw new InvalidOperationException("Unknown roguelite mission: " + id);
        public static RogueliteMissionDefinition SandboxForTemplate(string templateId) => templateId == "destruction_factory" ? FindMission("sandbox_destruction") : FindMission("sandbox_elimination");
    }

    public sealed class RogueliteDeveloperRun
    {
        public RogueliteLaunchKind Kind { get; }
        public RogueliteStoryPackage Package { get; }
        public string SandboxTemplateId { get; }
        public RogueliteMissionDefinition CurrentMission => Kind == RogueliteLaunchKind.StoryChain ? RogueliteDeveloperCatalog.FindMission(Package.CurrentMissionId) : RogueliteDeveloperCatalog.SandboxForTemplate(SandboxTemplateId);

        public RogueliteDeveloperRun(RogueliteStoryPackage package)
        { Kind = RogueliteLaunchKind.StoryChain; Package = package ?? throw new ArgumentNullException(nameof(package)); }
        public RogueliteDeveloperRun(string sandboxTemplateId, int seed)
        { if (RogueliteDeveloperCatalog.OpenSandboxTemplates.All(template => template.Id != sandboxTemplateId)) throw new ArgumentException("Template is not open for sandbox testing.", nameof(sandboxTemplateId)); Kind = RogueliteLaunchKind.TemplateSandbox; SandboxTemplateId = sandboxTemplateId; Package = RogueliteStoryCatalog.CreateDefault(seed); }
        public void Complete(string summary)
        { if (Kind == RogueliteLaunchKind.StoryChain) Package.CompleteCurrentMission(summary); }
    }

    // This store deliberately accepts only roguelite package JSON and never references CampaignState.
    public sealed class RogueliteSaveManager
    {
        private readonly Dictionary<string, string> saves = new Dictionary<string, string>(StringComparer.Ordinal);
        public bool HasSave(string packageId) => !string.IsNullOrEmpty(packageId) && saves.ContainsKey(packageId);
        public void Save(RogueliteStoryPackage package) { if (package == null) throw new ArgumentNullException(nameof(package)); saves[package.PackageId] = package.ToJson(); }
        public RogueliteStoryPackage Load(string packageId) => saves.TryGetValue(packageId, out string data) ? RogueliteStoryPackage.FromJson(data) : throw new KeyNotFoundException("Roguelite save not found.");
        public void Delete(string packageId) { saves.Remove(packageId); }
    }
}
