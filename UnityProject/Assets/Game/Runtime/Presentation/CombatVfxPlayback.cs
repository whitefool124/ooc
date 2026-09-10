using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Presentation
{
    public enum CombatVfxLayer { Ability, Reaction }

    public readonly struct CombatVfxSlot : IEquatable<CombatVfxSlot>
    {
        public GridPosition Position { get; }
        public CombatVfxLayer Layer { get; }
        public CombatVfxSlot(GridPosition position, CombatVfxLayer layer) { Position = position; Layer = layer; }
        public bool Equals(CombatVfxSlot other) => Position == other.Position && Layer == other.Layer;
        public override bool Equals(object obj) => obj is CombatVfxSlot other && Equals(other);
        public override int GetHashCode() => (Position.GetHashCode() * 397) ^ (int)Layer;
    }

    public readonly struct CombatVfxCue
    {
        public GridPosition Position { get; }
        public GridPosition Origin { get; }
        public string Effect { get; }
        public float Start { get; }
        public float Duration { get; }
        public float End => Start + Duration;
        public bool Travels { get; }
        public CombatVfxCue(GridPosition position, string effect, float start, float duration,
            GridPosition? origin = null, bool travels = false)
        {
            Position = position; Origin = origin ?? position; Effect = effect;
            Start = start; Duration = duration; Travels = travels;
        }
    }

    public readonly struct CombatVfxSample
    {
        public CombatVfxSlot Slot { get; }
        public CombatVfxCue Cue { get; }
        public float Progress { get; }
        public CombatVfxSample(CombatVfxSlot slot, CombatVfxCue cue, float progress)
        { Slot = slot; Cue = cue; Progress = progress; }
    }

    // Owns presentation time only. It has no CombatState, resolver, RNG or save-store reference.
    public sealed class CombatVfxPlayback
    {
        private sealed class Track
        {
            public float Started;
            public int Priority;
            public CombatVfxCue[] Cues;
        }
        private readonly Dictionary<CombatVfxSlot, Track> tracks = new Dictionary<CombatVfxSlot, Track>();
        private readonly List<CombatVfxSample> samples = new List<CombatVfxSample>();
        private readonly List<CombatVfxSlot> finished = new List<CombatVfxSlot>();
        public int PendingSlotCount => tracks.Count;

        public void ReplaceAbility(IEnumerable<CombatVfxCue> cues, float now)
        {
            foreach (var group in cues.GroupBy(cue => cue.Position))
                tracks[new CombatVfxSlot(group.Key, CombatVfxLayer.Ability)] = new Track
                { Started = now, Cues = group.OrderBy(cue => cue.Start).ToArray() };
        }

        public void PlayReaction(GridPosition position, string effect, int priority, float now)
        {
            CombatVfxSlot slot = new CombatVfxSlot(position, CombatVfxLayer.Reaction);
            if (tracks.TryGetValue(slot, out Track current) &&
                now < current.Started + current.Cues[current.Cues.Length - 1].End && priority < current.Priority) return;
            tracks[slot] = new Track { Started = now, Priority = priority,
                Cues = new[] { new CombatVfxCue(position, effect, 0f, .42f) } };
        }

        public IReadOnlyList<CombatVfxSample> Sample(float now)
        {
            samples.Clear(); finished.Clear();
            foreach (var entry in tracks)
            {
                Track track = entry.Value;
                float elapsed = now - track.Started;
                if (elapsed >= track.Cues[track.Cues.Length - 1].End) { finished.Add(entry.Key); continue; }
                foreach (CombatVfxCue cue in track.Cues)
                    if (elapsed >= cue.Start && elapsed < cue.End)
                    {
                        samples.Add(new CombatVfxSample(entry.Key, cue, (elapsed - cue.Start) / cue.Duration));
                        break;
                    }
            }
            foreach (CombatVfxSlot slot in finished) tracks.Remove(slot);
            return samples;
        }

        public void Clear() { tracks.Clear(); samples.Clear(); finished.Clear(); }
    }

    public static class FireVfxSequence
    {
        public const float CastDuration = .12f;
        public const float StageDuration = .18f;

        public static IReadOnlyList<CombatVfxCue> From(FireSpellExecution execution)
        {
            var cues = new List<CombatVfxCue>();
            FireSpellDefinition spell = execution?.Preview?.Spell;
            if (spell == null) return cues;
            float start = execution.IsTriggered ? 0f : CastDuration;
            if (!execution.IsTriggered)
                cues.Add(new CombatVfxCue(execution.SourcePosition, "fire_cast", 0f, CastDuration));
            var effects = new Dictionary<GridPosition, List<(string Effect, int Stage)>>();
            foreach (FireSpellResultStep step in execution.Steps.Where(step => HasVisualResult(step, spell)))
            {
                bool damageAtCell = execution.Steps.Any(other => other.Cell == step.Cell && other.Applied > 0 &&
                    (other.Kind == FireRuleKind.Damage || other.Kind == FireRuleKind.WeaponDamage));
                string effect = ResultEffect(step.Kind, damageAtCell);
                if (effect == null) continue;
                if (effect == "fire_impact")
                {
                    if (spell.Shape == FireSelectionShape.Square3 || spell.Shape == FireSelectionShape.CenterAndOrthogonal)
                        effect = "fire_cross_blast";
                    else if (spell.CombatAffinity == FireCombatAffinity.MeleeOnly && spell.DeliveryMode == FireDeliveryMode.ContactConduction)
                        effect = "fire_melee_arc";
                }
                if (effect == "fire_burning_ground" && spell.Shape == FireSelectionShape.ContinuousLine) effect = "fire_wall";
                if (step.Kind == FireRuleKind.LoseHealth &&
                    (spell.Id == "F-P-M19" || spell.Id == "F-P-U20" || spell.Id == "F-P-R20")) effect = "fire_overlimit";
                if (!effects.TryGetValue(step.Cell, out var cellEffects)) effects[step.Cell] = cellEffects = new List<(string, int)>();
                bool projected = !execution.IsTriggered && step.Cell != execution.SourcePosition &&
                    (spell.DeliveryMode == FireDeliveryMode.DetachedProjection || spell.DeliveryMode == FireDeliveryMode.FiregroundManipulation);
                if (projected && step.Kind != FireRuleKind.ArmTrigger)
                {
                    string delivery = spell.Shape == FireSelectionShape.Cone ? "fire_spray" :
                        spell.Shape == FireSelectionShape.Line || spell.Shape == FireSelectionShape.ContinuousLine ? "fire_line" : "fire_projectile";
                    AddUnique(cellEffects, delivery, 0);
                }
                int stage = effect == "path" ? 0 : effect == "fire_burning_ground" || effect == "fire_wall" || effect == "burning" ? 2 : 1;
                AddUnique(cellEffects, effect, stage);
            }
            foreach (var cell in effects.OrderBy(entry => entry.Key.Y).ThenBy(entry => entry.Key.X))
            {
                float time = start;
                foreach (var effect in cell.Value.OrderBy(value => value.Stage))
                {
                    cues.Add(new CombatVfxCue(cell.Key, effect.Effect, time, StageDuration,
                        execution.SourcePosition, effect.Effect == "fire_projectile"));
                    time += StageDuration;
                }
            }
            return cues;
        }

        private static void AddUnique(List<(string Effect, int Stage)> effects, string effect, int stage)
        { if (!effects.Any(value => value.Effect == effect)) effects.Add((effect, stage)); }

        private static bool HasVisualResult(FireSpellResultStep step, FireSpellDefinition spell)
        {
            if (step.Applied > 0 || step.Kind == FireRuleKind.ApplyBreakStance) return true;
            // Status steps store strength in Applied. A duration-only status can have zero strength;
            // its emitted result step and positive rule duration still describe a real application.
            return (step.Kind == FireRuleKind.ApplyBurning || step.Kind == FireRuleKind.ExtendBurning ||
                step.Kind == FireRuleKind.ApplyArmorBreak) &&
                spell.Rules.Any(rule => rule.Kind == step.Kind && rule.Duration > 0);
        }

        private static string ResultEffect(FireRuleKind kind, bool damageAtCell)
        {
            switch (kind)
            {
                case FireRuleKind.Damage: case FireRuleKind.WeaponDamage: case FireRuleKind.LoseHealth: return "fire_impact";
                case FireRuleKind.ApplyBurning: case FireRuleKind.ExtendBurning: case FireRuleKind.SetBurningDuration: return "burning";
                case FireRuleKind.CreateFireground: case FireRuleKind.ExtendFireground: return "fire_burning_ground";
                case FireRuleKind.ConsumeBurning: case FireRuleKind.ConsumeFireground: return damageAtCell ? "fire_detonate" : "fire_absorb";
                case FireRuleKind.RestoreShield: case FireRuleKind.RestoreMana: case FireRuleKind.GrantShieldBeforeRanged: return "fire_absorb";
                case FireRuleKind.ApplyArmorBreak: return "armor_break";
                case FireRuleKind.ApplyBreakStance: return "fire_break_stance";
                case FireRuleKind.DamageDurability: return "object_damage";
                case FireRuleKind.DestroyLightCover: return "object_break";
                case FireRuleKind.OverloadDevice: return "fire_overlimit";
                case FireRuleKind.ClearStatus: case FireRuleKind.ClearOneSelfStatus: return "cleanse";
                case FireRuleKind.MoveSource: case FireRuleKind.MoveAfterAttack: case FireRuleKind.SwapUnits: case FireRuleKind.Push: return "path";
                case FireRuleKind.ArmTrigger: case FireRuleKind.RestoreMovement: case FireRuleKind.AddMovement:
                case FireRuleKind.RepairWeapon: case FireRuleKind.ReduceIncomingDamage: case FireRuleKind.ExtendTriggerToAlly: return "fire_attachment";
                default: return null;
            }
        }
    }
}
