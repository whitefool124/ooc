using System;
using System.Collections.Generic;

namespace OCC.Combat.Roguelite
{
    public readonly struct ShieldGrant
    {
        public string SourceId { get; }
        public int Amount { get; }
        public ShieldGrant(string sourceId, int amount) { SourceId = sourceId; Amount = amount; }
    }

    public sealed class RogueCombatantState
    {
        private readonly Dictionary<string, int> sourceTriggerTurns = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<ShieldSourceRecord> shieldEvents = new List<ShieldSourceRecord>();
        public string UnitId { get; }
        public int CurrentHealth { get; private set; }
        public int CurrentShield { get; private set; }
        public BreakStanceState BreakStance { get; private set; }
        public bool IsDefeated => CurrentHealth <= 0;
        public IReadOnlyList<ShieldSourceRecord> ShieldEvents => shieldEvents;

        public RogueCombatantState(string unitId, int currentHealth, int currentShield = 0)
        {
            if (string.IsNullOrWhiteSpace(unitId) || currentHealth < 1 || currentShield < 0) throw new ArgumentOutOfRangeException(nameof(currentHealth));
            UnitId = unitId; CurrentHealth = currentHealth; CurrentShield = currentShield;
        }

        public DamageResolution Apply(DamagePacket packet)
        {
            if (IsDefeated) throw new InvalidOperationException("A defeated combatant cannot receive another damage packet.");
            DamageResolution result = RogueDamageResolver.Resolve(packet, CurrentShield, CurrentHealth);
            if (result.ShieldAbsorbed > 0) shieldEvents.Add(new ShieldSourceRecord(packet.SourceEffectId, result.ShieldAbsorbed, ShieldEventKind.Absorbed));
            CurrentShield -= result.ShieldAbsorbed; CurrentHealth -= result.HealthDamage;
            return result;
        }

        public void BeginOwnTurn(int turnSequence, IEnumerable<ShieldGrant> stableOrderedGrants)
        {
            if (CurrentShield > 0)
            {
                shieldEvents.Add(new ShieldSourceRecord("turn_start", CurrentShield, ShieldEventKind.ClearedAtTurnStart, turnSequence));
                CurrentShield = 0;
            }
            foreach (ShieldGrant grant in stableOrderedGrants ?? Array.Empty<ShieldGrant>()) TryGrantShield(grant.SourceId, grant.Amount, turnSequence);
        }

        public void EndOwnTurn(int turnSequence)
        {
            if (BreakStance != null && BreakStance.IsActive && turnSequence >= BreakStance.ExpiresAfterTurnSequence) BreakStance.Clear();
        }

        public void ApplyBreakStance(int expiresAfterTurnSequence)
        {
            if (BreakStance == null) BreakStance = new BreakStanceState(UnitId, expiresAfterTurnSequence);
            else BreakStance.Refresh(expiresAfterTurnSequence);
            if (CurrentShield > 0)
            {
                shieldEvents.Add(new ShieldSourceRecord("break_stance", CurrentShield, ShieldEventKind.Wasted, expiresAfterTurnSequence));
                CurrentShield = 0;
            }
        }

        public bool TryGrantShield(string sourceId, int amount, int turnSequence)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || amount <= 0) return false;
            if (BreakStance != null && BreakStance.IsActive)
            {
                shieldEvents.Add(new ShieldSourceRecord(sourceId, amount, ShieldEventKind.PreventedByBreakStance, turnSequence));
                return false;
            }
            if (sourceTriggerTurns.TryGetValue(sourceId, out int claimedTurn) && claimedTurn == turnSequence) return false;
            sourceTriggerTurns[sourceId] = turnSequence; CurrentShield += amount;
            shieldEvents.Add(new ShieldSourceRecord(sourceId, amount, ShieldEventKind.Granted, turnSequence));
            return true;
        }
    }
}
