using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum RogueliteLaunchKind { StoryChain, TemplateSandbox }
    public enum ShortRoguelitePhase { FirstCombat, Event, Salvage, Upgrade, SecondCombat, Complete }

    // R0 is deliberately small but every non-combat choice changes the second combat.
    public sealed class ShortRogueliteRun
    {
        public int Seed { get; }
        public ShortRoguelitePhase Phase { get; private set; }
        public string EventChoiceId { get; private set; }
        public string SalvageChoiceId { get; private set; }
        public string UpgradeChoiceId { get; private set; }
        public IReadOnlyList<string> Choices => new[] { EventChoiceId, SalvageChoiceId, UpgradeChoiceId }.Where(id => !string.IsNullOrEmpty(id)).ToArray();

        public ShortRogueliteRun(int seed) { Seed = seed; }
        private ShortRogueliteRun(int seed, ShortRoguelitePhase phase, string eventChoiceId, string salvageChoiceId, string upgradeChoiceId)
        { Seed = seed; Phase = phase; EventChoiceId = eventChoiceId; SalvageChoiceId = salvageChoiceId; UpgradeChoiceId = upgradeChoiceId; }
        public string CurrentMissionId => Phase == ShortRoguelitePhase.FirstCombat ? "dead_signal" : Phase == ShortRoguelitePhase.SecondCombat ? "factory_breach" : null;
        public void CompleteCombat()
        {
            if (Phase == ShortRoguelitePhase.FirstCombat) Phase = ShortRoguelitePhase.Event;
            else if (Phase == ShortRoguelitePhase.SecondCombat) Phase = ShortRoguelitePhase.Complete;
            else throw new InvalidOperationException("No combat is active.");
        }
        public void ChooseEvent(string id) { Require(ShortRoguelitePhase.Event); EventChoiceId = id == "field_repair" ? id : throw new ArgumentException("Unknown event choice.", nameof(id)); Phase = ShortRoguelitePhase.Salvage; }
        public void ChooseSalvage(string id) { Require(ShortRoguelitePhase.Salvage); SalvageChoiceId = id == "shield_cell" ? id : throw new ArgumentException("Unknown salvage choice.", nameof(id)); Phase = ShortRoguelitePhase.Upgrade; }
        public void ChooseUpgrade(string id) { Require(ShortRoguelitePhase.Upgrade); UpgradeChoiceId = id == "calibrated_rifle" ? id : throw new ArgumentException("Unknown upgrade choice.", nameof(id)); Phase = ShortRoguelitePhase.SecondCombat; }
        public string ToJson() => string.Join("|", "short1", Seed, (int)Phase, EventChoiceId ?? string.Empty, SalvageChoiceId ?? string.Empty, UpgradeChoiceId ?? string.Empty);
        public static ShortRogueliteRun FromJson(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
            if (parts.Length != 6 || parts[0] != "short1") throw new InvalidOperationException("Unsupported short roguelite save version.");
            return new ShortRogueliteRun(int.Parse(parts[1]), (ShortRoguelitePhase)int.Parse(parts[2]), parts[3], parts[4], parts[5]);
        }
        private void Require(ShortRoguelitePhase phase) { if (Phase != phase) throw new InvalidOperationException("Expected " + phase + ", current " + Phase + "."); }
    }

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
            new RogueliteMissionDefinition("rail_patrol", "elimination_rail", CombatObjectiveType.Elimination, "\u6e05\u9664\u94c1\u8def\u5de1\u903b\u961f", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u6b65\u67aa\u5175\u3001\u76fe\u536b\u3001\u706b\u672f\u5e08"),
            new RogueliteMissionDefinition("relay_raid", "destruction_factory", CombatObjectiveType.Destruction, "\u7834\u574f\u91ce\u6218\u4e2d\u7ee7\u5668", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u6b65\u67aa\u5175\u3001\u7a81\u88ad\u8005\u3001\u5148\u950b"),
            new RogueliteMissionDefinition("core_finale", "elimination_rail", CombatObjectiveType.Elimination, "\u6e05\u9664\u4e3b\u5e72\u7ad9\u7684\u963b\u622a\u961f", "\u4e3b\u89d2\u5931\u80fd\u5219\u4efb\u52a1\u5931\u8d25", "\u76fe\u536b\u3001\u706b\u672f\u5e08\u3001\u5148\u950b"),
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
        public ShortRogueliteRun ShortRun { get; }
        public bool IsShortRun => ShortRun != null;
        public RogueliteMissionDefinition CurrentMission => IsShortRun ? RogueliteDeveloperCatalog.FindMission(ShortRun.CurrentMissionId) : Kind == RogueliteLaunchKind.StoryChain ? RogueliteDeveloperCatalog.FindMission(Package.CurrentMissionId) : RogueliteDeveloperCatalog.SandboxForTemplate(SandboxTemplateId);

        public RogueliteDeveloperRun(RogueliteStoryPackage package)
        { Kind = RogueliteLaunchKind.StoryChain; Package = package ?? throw new ArgumentNullException(nameof(package)); }
        public RogueliteDeveloperRun(string sandboxTemplateId, int seed)
        { if (RogueliteDeveloperCatalog.OpenSandboxTemplates.All(template => template.Id != sandboxTemplateId)) throw new ArgumentException("Template is not open for sandbox testing.", nameof(sandboxTemplateId)); Kind = RogueliteLaunchKind.TemplateSandbox; SandboxTemplateId = sandboxTemplateId; Package = RogueliteStoryCatalog.CreateDefault(seed); }
        public RogueliteDeveloperRun(ShortRogueliteRun shortRun)
        { ShortRun = shortRun ?? throw new ArgumentNullException(nameof(shortRun)); Kind = RogueliteLaunchKind.StoryChain; Package = RogueliteStoryCatalog.CreateDefault(shortRun.Seed); }
        public void Complete(string summary)
        { if (IsShortRun) ShortRun.CompleteCombat(); else if (Kind == RogueliteLaunchKind.StoryChain) Package.CompleteCurrentMission(summary); }
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
