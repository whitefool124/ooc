using System;

namespace OCC.Combat
{
    public enum CombatCommandType
    {
        Move,
        Attack,
        Cast,
        UseSkill,
        UseQuickbar,
        SearchLoot,
        TakeLoot,
        EquipInventoryQuickbar,
        UseInventoryItem,
        Loot,
        Interact,
        EndTurn,
        OpenInventory,
        BreachCharge
    }

    public readonly struct CombatCommand
    {
        public CombatCommandType Type { get; }
        public string UnitId { get; }
        public GridPosition Destination { get; }
        public CardinalDirection AimDirection { get; }
        public string TargetUnitId { get; }
        public int SlotIndex { get; }

        private CombatCommand(CombatCommandType type, string unitId, GridPosition destination, CardinalDirection aimDirection, string targetUnitId = null, int slotIndex = 0)
        {
            Type = type;
            UnitId = unitId;
            Destination = destination;
            AimDirection = aimDirection;
            TargetUnitId = targetUnitId;
            SlotIndex = slotIndex;
        }

        public static CombatCommand Move(string unitId, GridPosition destination) =>
            new CombatCommand(CombatCommandType.Move, unitId, destination, default);

        public static CombatCommand Attack(string unitId, string targetUnitId) => new CombatCommand(CombatCommandType.Attack, unitId, default, default, targetUnitId);
        public static CombatCommand Cast(string unitId, string targetUnitId) => new CombatCommand(CombatCommandType.Cast, unitId, default, default, targetUnitId);
        public static CombatCommand UseSkill(string unitId, int skillIndex, string targetUnitId) => new CombatCommand(CombatCommandType.UseSkill, unitId, default, default, targetUnitId, skillIndex);
        public static CombatCommand UseSkillAt(string unitId, int skillIndex, GridPosition destination, CardinalDirection aimDirection) => new CombatCommand(CombatCommandType.UseSkill, unitId, destination, aimDirection, null, skillIndex);
        public static CombatCommand UseQuickbar(string unitId, int slotIndex) => new CombatCommand(CombatCommandType.UseQuickbar, unitId, default, default, null, slotIndex);
        public static CombatCommand SearchLoot(string unitId) => new CombatCommand(CombatCommandType.SearchLoot, unitId, default, default);
        public static CombatCommand TakeLoot(string unitId, string instanceId) => new CombatCommand(CombatCommandType.TakeLoot, unitId, default, default, instanceId);
        public static CombatCommand EquipInventoryQuickbar(string unitId, string instanceId, int slotIndex) => new CombatCommand(CombatCommandType.EquipInventoryQuickbar, unitId, default, default, instanceId, slotIndex);
        public static CombatCommand UseInventoryItem(string unitId, string instanceId) => new CombatCommand(CombatCommandType.UseInventoryItem, unitId, default, default, instanceId);
        public static CombatCommand Loot(string unitId) => new CombatCommand(CombatCommandType.Loot, unitId, default, default);
        public static CombatCommand Interact(string unitId, GridPosition target) => new CombatCommand(CombatCommandType.Interact, unitId, target, default);
        public static CombatCommand EndTurn(string unitId) => new CombatCommand(CombatCommandType.EndTurn, unitId, default, default);
        public static CombatCommand OpenInventory(string unitId) => new CombatCommand(CombatCommandType.OpenInventory, unitId, default, default);
        public static CombatCommand BreachCharge(string unitId, GridPosition lockedTarget) =>
            new CombatCommand(CombatCommandType.BreachCharge, unitId, lockedTarget, default);
    }
}
