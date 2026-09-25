using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public enum CombatJournalKind { Command, EndEnemyTurn, PresentationTurnStart, Equipment, Artifact, OptionalMove }

    /// <summary>A completed, deterministic combat transition. Rejected commands are never recorded.</summary>
    public readonly struct CombatJournalEntry
    {
        public CombatJournalKind Kind { get; }
        public CombatCommand Command { get; }
        public string Payload { get; }

        private CombatJournalEntry(CombatJournalKind kind, CombatCommand command, string payload = "")
        { Kind = kind; Command = command; Payload = payload ?? string.Empty; }

        public static CombatJournalEntry Accepted(CombatCommand command) =>
            new CombatJournalEntry(CombatJournalKind.Command, command);

        public static CombatJournalEntry EnemyTurnEnded(string unitId) =>
            new CombatJournalEntry(CombatJournalKind.EndEnemyTurn, CombatCommand.EndTurn(unitId));

        public static CombatJournalEntry PresentationTurnStarted(string unitId) =>
            new CombatJournalEntry(CombatJournalKind.PresentationTurnStart, CombatCommand.EndTurn(unitId));

        public static CombatJournalEntry EquipmentChanged(string operation, string instanceId, int slot = 0,
            int x = 0, int y = 0, bool rotated = false) =>
            new CombatJournalEntry(CombatJournalKind.Equipment, CombatCommand.EndTurn("hero"),
                string.Join(",", operation, Text(instanceId), slot.ToString(CultureInfo.InvariantCulture),
                    x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture), rotated ? "1" : "0"));

        public static CombatJournalEntry ArtifactUsed(string artifactId, string instanceId, ArtifactTarget target, int uses) =>
            new CombatJournalEntry(CombatJournalKind.Artifact, CombatCommand.EndTurn("hero"),
                string.Join(",", Text(artifactId), Text(instanceId), Text(target.UnitId), Text(target.SecondaryUnitId),
                    target.Cell.X.ToString(CultureInfo.InvariantCulture), target.Cell.Y.ToString(CultureInfo.InvariantCulture),
                    uses.ToString(CultureInfo.InvariantCulture)));

        public static CombatJournalEntry OptionalMove(string unitId, GridPosition destination) =>
            new CombatJournalEntry(CombatJournalKind.OptionalMove, CombatCommand.Move(unitId, destination));

        public string Encode()
        {
            CombatCommand command = Command;
            return string.Join(";", "2", ((int)Kind).ToString(CultureInfo.InvariantCulture),
                ((int)command.Type).ToString(CultureInfo.InvariantCulture),
                Text(command.UnitId), command.Destination.X.ToString(CultureInfo.InvariantCulture),
                command.Destination.Y.ToString(CultureInfo.InvariantCulture),
                ((int)command.AimDirection).ToString(CultureInfo.InvariantCulture),
                Text(command.TargetUnitId), command.SlotIndex.ToString(CultureInfo.InvariantCulture), Text(Payload));
        }

        public static CombatJournalEntry Decode(string row)
        {
            string[] fields = (row ?? string.Empty).Split(';');
            if (!((fields.Length == 9 && fields[0] == "1") || (fields.Length == 10 && fields[0] == "2")))
                throw new InvalidOperationException("Invalid combat journal row.");
            int kindValue = Parse(fields[1]);
            int typeValue = Parse(fields[2]);
            int directionValue = Parse(fields[6]);
            if (!Enum.IsDefined(typeof(CombatJournalKind), kindValue) ||
                !Enum.IsDefined(typeof(CombatCommandType), typeValue) ||
                !Enum.IsDefined(typeof(CardinalDirection), directionValue))
                throw new InvalidOperationException("Invalid combat journal command.");
            CombatCommand command = CombatCommand.Restore((CombatCommandType)typeValue, UntText(fields[3]),
                new GridPosition(Parse(fields[4]), Parse(fields[5])), (CardinalDirection)directionValue,
                UntText(fields[7]), Parse(fields[8]));
            CombatJournalKind kind = (CombatJournalKind)kindValue;
            if (kind != CombatJournalKind.Command && kind != CombatJournalKind.OptionalMove && command.Type != CombatCommandType.EndTurn)
                throw new InvalidOperationException("Invalid turn journal row.");
            if (kind == CombatJournalKind.OptionalMove && command.Type != CombatCommandType.Move)
                throw new InvalidOperationException("Invalid optional move journal row.");
            string payload = fields.Length == 10 ? UntText(fields[9]) : string.Empty;
            if ((kind == CombatJournalKind.Equipment || kind == CombatJournalKind.Artifact) && string.IsNullOrEmpty(payload))
                throw new InvalidOperationException("Missing combat journal payload.");
            return new CombatJournalEntry(kind, command, payload);
        }

        private static int Parse(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        private static string Text(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        private static string UntText(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    public sealed class CombatJournalReplayResult
    {
        public FireBattleState FireBattle { get; }
        public ArtifactBattleState ArtifactBattle { get; }
        public string ObservedUnitId { get; }

        internal CombatJournalReplayResult(FireBattleState fireBattle, ArtifactBattleState artifactBattle, string observedUnitId)
        { FireBattle = fireBattle; ArtifactBattle = artifactBattle; ObservedUnitId = observedUnitId; }
    }

    public static class CombatJournalReplayer
    {
        /// <summary>Replays only committed transitions on a freshly activated encounter.</summary>
        public static CombatJournalReplayResult ReplayAfterActivation(CombatState state, IEnumerable<string> rows)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            CombatCommandExecutionService executor = new CombatCommandExecutionService();
            FireBattleState fireBattle = new FireBattleState(state);
            ArtifactBattleState artifactBattle = null;
            string observedUnitId = null;
            foreach (string row in rows)
            {
                CombatJournalEntry entry = CombatJournalEntry.Decode(row);
                CombatCommand command = entry.Command;
                UnitState unit = state.GetUnit(command.UnitId);
                if (unit == null || state.ActiveUnitId != unit.Id || state.IsVictory || state.IsDefeat)
                    throw new InvalidOperationException("Combat journal diverged before a command.");
                if (entry.Kind == CombatJournalKind.PresentationTurnStart)
                {
                    if (observedUnitId == unit.Id)
                        throw new InvalidOperationException("Combat journal repeats a turn-start transition.");
                    fireBattle.BeginUnitTurn(unit.Id);
                    if (artifactBattle == null) artifactBattle = new ArtifactBattleState(state);
                    artifactBattle.BeginUnitTurn(unit.Id);
                    observedUnitId = unit.Id;
                    continue;
                }
                if (entry.Kind == CombatJournalKind.EndEnemyTurn)
                {
                    if (unit.IsHero) throw new InvalidOperationException("Hero turn cannot use the enemy journal transition.");
                    CombatResolver.EndTurn(state, unit);
                    observedUnitId = null;
                    continue;
                }
                if (entry.Kind == CombatJournalKind.Equipment)
                {
                    if (!unit.IsHero || state.RogueEquipment == null || !ReplayEquipment(state.RogueEquipment, entry.Payload))
                        throw new InvalidOperationException("Combat equipment journal diverged.");
                    continue;
                }
                if (entry.Kind == CombatJournalKind.Artifact)
                {
                    if (!unit.IsHero || !ReplayArtifact(state, ref artifactBattle, entry.Payload))
                        throw new InvalidOperationException("Combat artifact journal diverged.");
                    continue;
                }
                if (entry.Kind == CombatJournalKind.OptionalMove)
                {
                    if (!unit.IsHero || !fireBattle.OptionalMoves.Any(offer => offer.SourceUnitId == unit.Id && offer.Destination == command.Destination))
                        throw new InvalidOperationException("Combat optional move journal diverged.");
                    FireSpellExecution move = FireSpellEngine.ExecuteOptionalMove(fireBattle, unit.Id, command.Destination);
                    state.AddLog(move.Preview.Spell.DisplayName + "：已选择移动至破口落点。");
                    continue;
                }
                fireBattle?.DeclineOptionalMoves();
                CombatCommandExecutionResult result = executor.Execute(state, fireBattle, command, true);
                if (!result.Accepted)
                    throw new InvalidOperationException("Combat journal command was rejected: " + result.RejectionReason);
                fireBattle = result.FireBattle;
                if (!string.IsNullOrEmpty(result.ActionResult)) state.AddLog(result.ActionResult);
                if (state.ActiveUnitId != observedUnitId) observedUnitId = null;
            }
            return new CombatJournalReplayResult(fireBattle, artifactBattle, observedUnitId);
        }

        private static bool ReplayEquipment(RogueEquipmentRuntime runtime, string payload)
        {
            string[] fields = payload.Split(',');
            if (fields.Length != 6) return false;
            string id = Encoding.UTF8.GetString(Convert.FromBase64String(fields[1]));
            int slot = int.Parse(fields[2], CultureInfo.InvariantCulture);
            int x = int.Parse(fields[3], CultureInfo.InvariantCulture);
            int y = int.Parse(fields[4], CultureInfo.InvariantCulture);
            bool rotated = fields[5] == "1";
            switch (fields[0])
            {
                case "move": return runtime.MoveBackpack(id, x, y, rotated);
                case "rotate": return runtime.RotateBackpack(id);
                case "equip": return runtime.Equip(id, (OCC.Combat.Roguelite.EquipmentSlot)slot);
                case "replace": return runtime.EquipOrReplace(id, (OCC.Combat.Roguelite.EquipmentSlot)slot);
                case "unequip": return runtime.Unequip((OCC.Combat.Roguelite.EquipmentSlot)slot);
                case "unequipTo": return runtime.UnequipToBackpack((OCC.Combat.Roguelite.EquipmentSlot)slot, x, y, rotated);
                case "quickbar": return runtime.AssignQuickbar(slot, id);
                default: return false;
            }
        }

        private static bool ReplayArtifact(CombatState state, ref ArtifactBattleState battle, string payload)
        {
            string[] fields = payload.Split(',');
            if (fields.Length != 7) return false;
            string DecodeText(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
            string artifactId = DecodeText(fields[0]);
            string instanceId = DecodeText(fields[1]);
            ArtifactDefinition definition = ArtifactCatalog.Get(artifactId);
            if (definition == null) return false;
            ArtifactTarget target = new ArtifactTarget(new GridPosition(int.Parse(fields[4], CultureInfo.InvariantCulture),
                int.Parse(fields[5], CultureInfo.InvariantCulture)), DecodeText(fields[2]), DecodeText(fields[3]));
            int uses = int.Parse(fields[6], CultureInfo.InvariantCulture);
            if (battle == null) battle = new ArtifactBattleState(state);
            if (!ArtifactEngine.Preview(battle, "hero", definition, target, uses).CanCommit) return false;
            if (!string.IsNullOrEmpty(instanceId) && state.RogueEquipment?.TacticalItem(instanceId) is RogueTacticalItemInstance tactical)
            {
                if (tactical.DefinitionId != artifactId || tactical.ChargesCurrent != uses) return false;
                ArtifactEngine.Execute(battle, "hero", definition, target, uses);
                if (!tactical.Consume()) return false;
            }
            else if (!string.IsNullOrEmpty(instanceId))
            {
                ItemInstance inventory = state.ItemInventory?.Get(instanceId);
                if (inventory == null || inventory.DefinitionId != artifactId || inventory.RemainingUses != uses) return false;
                ArtifactEngine.ExecuteInventory(battle, "hero", instanceId, target);
            }
            else ArtifactEngine.Execute(battle, "hero", definition, target, uses);
            state.AddLog(definition.DisplayName + "已经生效。");
            return true;
        }
    }
}
