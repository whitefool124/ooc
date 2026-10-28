using System;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public enum AutoCombatStyle { Aggressive, Balanced, Cautious }

    // Consent is tied to the equipped instance, so replacing a quickbar item cannot
    // silently authorize the replacement to spend its limited charges.
    public sealed class AutoCombatSettings
    {
        private readonly string[] instanceIds = new string[RogueRuntimeConstants.ItemQuickbarSize];
        private readonly bool[] enabled = new bool[RogueRuntimeConstants.ItemQuickbarSize];
        private readonly int[] reserved = new int[RogueRuntimeConstants.ItemQuickbarSize];

        public AutoCombatStyle Style { get; private set; } = AutoCombatStyle.Balanced;
        public int SlotCount => instanceIds.Length;

        public AutoCombatSettings()
        {
            for (int slot = 0; slot < reserved.Length; slot++) reserved[slot] = 1;
        }

        public void BindQuickbar(CombatState state)
        {
            if (state == null) return;
            string[] equipped = state.RogueEquipment?.ItemQuickbarInstanceIds ?? state.ItemQuickbar;
            for (int slot = 0; slot < instanceIds.Length; slot++)
            {
                string id = slot < equipped.Length ? equipped[slot] ?? string.Empty : string.Empty;
                if (string.Equals(instanceIds[slot], id, StringComparison.Ordinal)) continue;
                instanceIds[slot] = id;
                enabled[slot] = false;
                reserved[slot] = 1;
            }
        }

        public void SetStyle(AutoCombatStyle style)
        {
            if (!Enum.IsDefined(typeof(AutoCombatStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
            Style = style;
        }

        public string InstanceId(int slot) => Valid(slot) ? instanceIds[slot] : string.Empty;
        public bool IsEnabled(int slot) => Valid(slot) && enabled[slot];
        public int ReservedUses(int slot) => Valid(slot) ? reserved[slot] : 0;

        public void SetEnabled(int slot, bool value)
        {
            if (!Valid(slot)) return;
            enabled[slot] = value && !string.IsNullOrEmpty(instanceIds[slot]);
        }

        public void SetReservedUses(int slot, int value, int currentUses)
        {
            if (!Valid(slot)) return;
            reserved[slot] = Math.Max(0, Math.Min(Math.Max(0, currentUses), value));
        }

        public bool MaySpend(int slot, string instanceId, int currentUses) =>
            Valid(slot) && enabled[slot] && currentUses > reserved[slot] &&
            string.Equals(instanceIds[slot], instanceId, StringComparison.Ordinal);

        private bool Valid(int slot) => slot >= 0 && slot < instanceIds.Length;
    }

    public readonly struct AutoCombatDecision
    {
        public CombatCommand Command { get; }
        public int ArtifactSlot { get; }
        public string ArtifactInstanceId { get; }
        public ArtifactTarget ArtifactTarget { get; }
        public bool UsesArtifact => ArtifactSlot >= 0;

        private AutoCombatDecision(CombatCommand command, int artifactSlot, string artifactInstanceId, ArtifactTarget target)
        { Command = command; ArtifactSlot = artifactSlot; ArtifactInstanceId = artifactInstanceId; ArtifactTarget = target; }

        public static AutoCombatDecision FromCommand(CombatCommand command) => new AutoCombatDecision(command, -1, null, default);
        public static AutoCombatDecision FromArtifact(int slot, string instanceId, ArtifactTarget target) =>
            new AutoCombatDecision(default, slot, instanceId, target);
    }
}
