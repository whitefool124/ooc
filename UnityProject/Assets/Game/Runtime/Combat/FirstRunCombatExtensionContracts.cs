using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class EnemyCombatIdentity
    {
        public string ArchetypeId { get; }
        public string AppearanceId { get; }
        public IReadOnlyList<string> SkillEffectIds { get; }
        public string IntentProfileId { get; }
        public string DialogueSetId { get; }

        public EnemyCombatIdentity(string archetypeId, string appearanceId, IEnumerable<string> skillEffectIds,
            string intentProfileId, string dialogueSetId)
        {
            if (string.IsNullOrWhiteSpace(archetypeId) || string.IsNullOrWhiteSpace(appearanceId) ||
                string.IsNullOrWhiteSpace(intentProfileId) || string.IsNullOrWhiteSpace(dialogueSetId))
                throw new ArgumentException("Enemy identity, appearance, intent and dialogue ids are required.");
            ArchetypeId = archetypeId; AppearanceId = appearanceId;
            SkillEffectIds = (skillEffectIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            IntentProfileId = intentProfileId; DialogueSetId = dialogueSetId;
        }
    }

    public sealed class EnemyScriptContext
    {
        public CombatState Combat { get; }
        public UnitState Enemy { get; }
        public FirstRunEncounterSnapshot Encounter { get; }
        public EnemyCombatIdentity Identity { get; }
        public EnemyScriptContext(CombatState combat, UnitState enemy, FirstRunEncounterSnapshot encounter, EnemyCombatIdentity identity)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }

    /// <summary>Encounter-specific AI hook. The default tactics remain untouched until Phase-B content is approved.</summary>
    public interface IFirstRunEnemyScript
    {
        string ScriptId { get; }
        CombatCommand ChooseCommand(EnemyScriptContext context);
    }

    public enum CombatDialogueWindow { EncounterStart, IntentRevealed, SkillUsed, HealthThreshold, Defeated, EncounterEnd }

    public sealed class CombatDialogueEvent
    {
        public string DialogueSetId { get; }
        public string LineId { get; }
        public string SpeakerId { get; }
        public CombatDialogueWindow Window { get; }
        public CombatDialogueEvent(string dialogueSetId, string lineId, string speakerId, CombatDialogueWindow window)
        { DialogueSetId = dialogueSetId ?? string.Empty; LineId = lineId ?? string.Empty; SpeakerId = speakerId ?? string.Empty; Window = window; }
    }

    public sealed class CombatDialogueQueue
    {
        private readonly Queue<CombatDialogueEvent> pending = new Queue<CombatDialogueEvent>();
        public int Count => pending.Count;
        public void Enqueue(CombatDialogueEvent value) { if (value != null) pending.Enqueue(value); }
        public bool TryDequeue(out CombatDialogueEvent value)
        {
            if (pending.Count == 0) { value = null; return false; }
            value = pending.Dequeue(); return true;
        }
    }
}
