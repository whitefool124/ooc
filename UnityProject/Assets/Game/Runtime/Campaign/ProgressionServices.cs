using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class ServiceLedger
    {
        private readonly HashSet<string> upgrades = new HashSet<string>(StringComparer.Ordinal);
        public int TrainingCount { get; private set; }
        public int WorkshopResets { get; private set; }
        public IReadOnlyCollection<string> Upgrades => upgrades;
        public void Train() { TrainingCount++; }
        public void ResetWorkshop() { WorkshopResets++; }
        public void AddUpgrade(string id) { if (!string.IsNullOrEmpty(id)) upgrades.Add(id); }
        public ServiceLedger Clone() { var clone = new ServiceLedger { TrainingCount = TrainingCount, WorkshopResets = WorkshopResets }; foreach (var item in upgrades) clone.upgrades.Add(item); return clone; }
    }

    public sealed class TaskTemplateValidator
    {
        public static IReadOnlyList<string> RequiredTypes { get; } = Enum.GetNames(typeof(CombatObjectiveType));
        public static void Validate(IReadOnlyList<TaskTemplate> templates)
        {
            if (templates == null || RequiredTypes.Any(type => !templates.Any(template => template.Type.ToString() == type))) throw new InvalidOperationException("All six objective types require a hand-authored validation template.");
            var combinations = templates.Select(template => template.MapId + "|" + template.Type + "|" + string.Join(",", template.InteractionIds));
            if (combinations.Count() != combinations.Distinct(StringComparer.Ordinal).Count()) throw new InvalidOperationException("Task template combination is duplicated.");
        }
    }
}
