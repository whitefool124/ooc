using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public sealed class RogueSpellLoadout
    {
        private readonly HashSet<string> mastered;
        private readonly string[] equipped;
        public IReadOnlyCollection<string> MasteredSpellIds => mastered;
        public string[] EquippedSpellIds => (string[])equipped.Clone();
        public bool IsCombatLocked { get; }

        private RogueSpellLoadout(IEnumerable<string> masteredSpellIds, IEnumerable<string> equippedSpellIds, bool locked)
        {
            mastered = new HashSet<string>(masteredSpellIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            equipped = (equippedSpellIds ?? Array.Empty<string>()).ToArray();
            if (equipped.Length != RogueRuntimeConstants.SpellSlotCount) throw new ArgumentException("Roguelite spell loadout must contain eight slots.");
            IsCombatLocked = locked;
        }

        public static RogueSpellLoadout CreateStarter()
        {
            string[] basics = { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER" };
            string[] slots = Enumerable.Repeat(string.Empty, RogueRuntimeConstants.SpellSlotCount).ToArray();
            Array.Copy(basics, slots, basics.Length);
            return new RogueSpellLoadout(basics, slots, false);
        }

        public void Learn(string spellId)
        { if (string.IsNullOrWhiteSpace(spellId)) throw new ArgumentException("Spell id is required."); mastered.Add(spellId); }

        public void Equip(int slot, string spellId)
        {
            if (IsCombatLocked) throw new InvalidOperationException("Spell loadout is locked during combat.");
            if (slot < 0 || slot >= equipped.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!string.IsNullOrEmpty(spellId) && !mastered.Contains(spellId)) Learn(spellId);
            if (!string.IsNullOrEmpty(spellId) && equipped.Where((value, index) => index != slot).Contains(spellId)) throw new InvalidOperationException("A spell cannot occupy two slots.");
            equipped[slot] = spellId ?? string.Empty;
        }

        public RogueSpellLoadout CreateCombatSnapshot() => new RogueSpellLoadout(mastered, equipped, true);
        public static RogueSpellLoadout Restore(IEnumerable<string> masteredSpellIds, IEnumerable<string> equippedSpellIds, bool locked)
            => new RogueSpellLoadout(masteredSpellIds, equippedSpellIds, locked);
    }

    public sealed class RogueSpellExecution
    {
        public bool Accepted { get; }
        public CombatEffectExecution CombatEffects { get; }
        public FireSpellExecution FireEffects { get; }
        public RogueSpellExecution(CombatEffectExecution combatEffects, FireSpellExecution fireEffects = null)
        { Accepted = true; CombatEffects = combatEffects ?? CombatEffectExecution.Empty; FireEffects = fireEffects; }
    }

    public sealed class RogueSpellCombatRuntime
    {
        private readonly Dictionary<string, int> ownTurnSequences = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> availableAtTurn = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly RogueContentCatalog catalog;
        public CombatState Combat { get; }
        public RogueSpellLoadout Loadout { get; }
        public FireBattleState FireBattle { get; }

        public RogueSpellCombatRuntime(CombatState combat, RogueSpellLoadout combatSnapshot)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (combat.Ruleset != CombatRuleset.Roguelite) throw new InvalidOperationException("Rogue spell runtime requires roguelite rules.");
            Loadout = combatSnapshot ?? throw new ArgumentNullException(nameof(combatSnapshot));
            if (!Loadout.IsCombatLocked) throw new InvalidOperationException("Combat requires a locked spell snapshot.");
            catalog = RogueContentCatalog.CreateAcademyV01(); FireBattle = new FireBattleState(combat);
        }

        public void BeginOwnTurn(string unitId)
        {
            ownTurnSequences[unitId] = ownTurnSequences.TryGetValue(unitId, out int value) ? value + 1 : 1;
            FireBattle.BeginUnitTurn(unitId);
        }

        public bool IsReady(string spellId, string unitId = "hero")
        {
            int turn = ownTurnSequences.TryGetValue(unitId, out int value) ? value : 0;
            return !availableAtTurn.TryGetValue(unitId + "|" + spellId, out int available) || turn >= available;
        }

        public SpellDefinition DefinitionAtSlot(int slot)
        {
            if (slot < 0 || slot >= RogueRuntimeConstants.SpellSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            string id = Loadout.EquippedSpellIds[slot];
            return string.IsNullOrEmpty(id) ? null : catalog.Spells.Single(value => value.DefinitionId == id);
        }

        public int CooldownRemaining(string spellId, string unitId = "hero")
        {
            int turn = ownTurnSequences.TryGetValue(unitId, out int value) ? value : 0;
            return availableAtTurn.TryGetValue(unitId + "|" + spellId, out int available) ? Math.Max(0, available - turn) : 0;
        }

        public RogueSpellExecution ExecuteSlot(int slot, CombatCommand command)
        {
            if (slot < 0 || slot >= RogueRuntimeConstants.SpellSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            string spellId = Loadout.EquippedSpellIds[slot];
            if (string.IsNullOrEmpty(spellId)) throw new InvalidOperationException("Spell slot is empty.");
            if (Combat.ActiveUnitId != command.UnitId) throw new InvalidOperationException("Only the active unit can cast.");
            if (!IsReady(spellId, command.UnitId)) throw new InvalidOperationException("Spell is cooling down.");
            SpellDefinition spell = catalog.Spells.Single(value => value.DefinitionId == spellId);
            UnitState source = Combat.GetUnit(command.UnitId) ?? throw new InvalidOperationException("Source unit does not exist.");
            if (source.ActionPoints < spell.ActionPointCost || source.Mana < spell.ManaCost) throw new InvalidOperationException("Insufficient action points or personal mana.");

            RogueSpellExecution execution = spell.IsBasic ? ExecuteBasic(spell, source, command) : ExecuteFire(spell, source, command);
            if (spell.CooldownOwnTurns > 0)
            {
                int turn = ownTurnSequences.TryGetValue(source.Id, out int value) ? value : 0;
                availableAtTurn[source.Id + "|" + spellId] = turn + spell.CooldownOwnTurns + 1;
            }
            return execution;
        }

        private RogueSpellExecution ExecuteBasic(SpellDefinition spell, UnitState source, CombatCommand command)
        {
            List<CombatEffect> effects = new List<CombatEffect> { CombatEffect.SpendActionPoints(spell.ActionPointCost) };
            if (spell.ManaCost > 0) effects.Add(CombatEffect.SpendMana(spell.ManaCost));
            if (spell.DefinitionId == "BASE-MANA-RECOVER") effects.Add(CombatEffect.RestoreMana(source.Id, 2));
            if (spell.DefinitionId == "BASE-AETHER-SHIELD")
            {
                CombatEffectExecution cost = CombatEffectExecutor.Execute(Combat, source.Id, effects.ToArray());
                Combat.TryGrantRogueliteShield(source.Id, spell.DefinitionId, 6);
                return new RogueSpellExecution(cost);
            }
            if (spell.DefinitionId == "BASE-FIRE-MELEE" || spell.DefinitionId == "BASE-FIRE-RANGED")
            {
                UnitState target = Combat.GetUnit(command.TargetUnitId) ?? throw new InvalidOperationException("Target unit does not exist.");
                int distance = source.Position.ManhattanDistance(target.Position);
                if (source.IsHero == target.IsHero || distance > spell.Range || (spell.Range > 1 && !Combat.Map.HasLineOfSight(source.Position, target.Position))) throw new InvalidOperationException("Spell target is not legal.");
                DamageComponentKind kind = spell.DefinitionId == "BASE-FIRE-MELEE" ? DamageComponentKind.Physical : DamageComponentKind.Fire;
                int raw = spell.DefinitionId == "BASE-FIRE-MELEE" ? 8 : 6;
                DamageResolution damage = RogueDamageResolver.Resolve(new DamagePacket("basic-" + spell.DefinitionId, source.Id, target.Id, spell.DefinitionId,
                    new[] { new DamageComponent(kind, raw) }), target.Shield, target.Health);
                effects.Add(CombatEffect.AbsorbShield(target.Id, damage.ShieldAbsorbed)); effects.Add(CombatEffect.DamageHealth(target.Id, damage.HealthDamage));
            }
            return new RogueSpellExecution(CombatEffectExecutor.Execute(Combat, source.Id, effects.ToArray()));
        }

        private RogueSpellExecution ExecuteFire(SpellDefinition spell, UnitState source, CombatCommand command)
        {
            FireSpellDefinition old = FireSpellCatalog.Get(spell.DefinitionId);
            FireSpellTarget target = !string.IsNullOrEmpty(command.TargetUnitId)
                ? FireSpellTarget.Unit(command.TargetUnitId, command.Facing)
                : FireSpellTarget.At(command.Destination, command.Facing);
            return new RogueSpellExecution(CombatEffectExecution.Empty, FireSpellEngine.Execute(FireBattle, source.Id, old, target));
        }
    }

    public static class RogueSpellRuleInterpreter
    {
        private static readonly string[] AllowedPrefixes =
        { "legacy_rule:", "apply_break_stance", "grant_shield_before_ranged", "clear_one_self_status" };

        public static RogueValidationResult Validate(SpellDefinition spell)
        {
            RogueValidationResult result = new RogueValidationResult();
            if (spell == null) { result.Add("Spell is missing."); return result; }
            foreach (string rule in spell.Rules)
            {
                if (rule.IndexOf("ArmorBreak", StringComparison.OrdinalIgnoreCase) >= 0 || rule.IndexOf("ReduceIncomingDamage", StringComparison.OrdinalIgnoreCase) >= 0 || rule.IndexOf("RepairWeapon", StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add("Removed rule leaked into rogue runtime: " + rule);
                else if (!AllowedPrefixes.Any(prefix => rule.StartsWith(prefix, StringComparison.Ordinal))) result.Add("Unknown rogue rule: " + rule);
            }
            return result;
        }
    }
}
