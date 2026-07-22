using System;
using System.Linq;

namespace OCC.Combat
{
    public static class CombatResolver
    {
        public readonly struct AttackPreview
        {
            public int BaseDamage { get; }
            public int FacingModifier { get; }
            public int CoverReduction { get; }
            public int ShieldAbsorption { get; }
            public int ArmorReduction { get; }
            public int BlockReduction { get; }
            public int FinalDamage { get; }
            public bool HasLineOfSight { get; }

            public AttackPreview(int baseDamage, int facingModifier, int coverReduction, int shieldAbsorption, int armorReduction, int blockReduction, int finalDamage, bool hasLineOfSight)
            { BaseDamage = baseDamage; FacingModifier = facingModifier; CoverReduction = coverReduction; ShieldAbsorption = shieldAbsorption; ArmorReduction = armorReduction; BlockReduction = blockReduction; FinalDamage = finalDamage; HasLineOfSight = hasLineOfSight; }
        }

        public const int HeroActionPointsPerTurn = 3;
        public const int BasicActionPointCost = 1;

        public static AttackPreview PreviewAttack(CombatState state, string attackerId, string targetId, bool isCast)
        {
            UnitState attacker = GetUnit(state, attackerId);
            UnitState defender = GetUnit(state, targetId);
            WeaponDefinition source = isCast ? null : attacker.MainHand;
            int damage = isCast ? CombatCatalog.FireBolt.Damage : source.Damage;
            int armorPierce = isCast ? 0 : source.ArmorPierce;
            DamageParts parts = CalculateDamage(state, attacker, defender, damage, DamageType.Physical, armorPierce);
            return new AttackPreview(damage, parts.Facing, parts.Cover, parts.Shield, parts.Armor, parts.Block, parts.Final, state.Map.HasLineOfSight(attacker.Position, defender.Position));
        }

        public static void BeginTurn(CombatState state, string unitId)
        {
            UnitState unit = GetUnit(state, unitId);
            state.SetActiveUnit(unitId);
            unit.TickTurnEffects();
            state.EvaluateOutcome();
            if (!unit.IsAlive) { state.AddLog($"{unit.DisplayName} \u88ab\u6301\u7eed\u6548\u679c\u51fb\u5012\u3002"); if (!state.IsVictory && !state.IsDefeat) AdvanceToNextTurn(state); return; }
            unit.BeginTurn(unit.IsHero ? HeroActionPointsPerTurn : 2);
            state.AddLog($"{unit.DisplayName} \u5f00\u59cb\u884c\u52a8\uff08{unit.ActionPoints} \u884c\u52a8\u70b9\uff09\u3002");
        }

        public static void AdvanceToNextTurn(CombatState state)
        {
            UnitState next = state.Units.Values.Where(unit => unit.IsAlive).OrderBy(unit => unit.InitiativeTime).ThenBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
            if (next == null) { state.EvaluateOutcome(); return; }
            state.SetCurrentTime(next.InitiativeTime);
            BeginTurn(state, next.Id);
        }

        public static void Resolve(CombatState state, CombatCommand command)
        {
            UnitState unit = GetActiveUnit(state, command.UnitId);
            switch (command.Type)
            {
                case CombatCommandType.Move: ResolveMove(state, unit, command); break;
                case CombatCommandType.TurnInPlace: unit.SpendActionPoint(BasicActionPointCost); unit.TurnInPlace(command.Facing); break;
                case CombatCommandType.Attack: ResolveWeaponAttack(state, unit, command.TargetUnitId); break;
                case CombatCommandType.Cast: ResolveSkill(state, unit, CombatCatalog.FireBolt, command.TargetUnitId); break;
                case CombatCommandType.UseSkill: ResolveSkill(state, unit, command.SlotIndex == 0 ? unit.SkillOne : unit.SkillTwo, command.TargetUnitId); break;
                case CombatCommandType.UseItem: UseConsumable(state, unit, CombatCatalog.Medkit, false); break;
                case CombatCommandType.UseQuickbar: UseQuickbar(state, unit, command.SlotIndex); break;
                case CombatCommandType.Loot: ResolveLoot(state, unit); break;
                case CombatCommandType.Interact: ResolveInteract(state, unit, command.Destination); break;
                case CombatCommandType.EndTurn: EndTurn(state, unit); break;
                default: throw new ArgumentOutOfRangeException(nameof(command), command.Type, "Unsupported combat command.");
            }
        }

        public static void EndTurn(CombatState state, UnitState unit)
        {
            unit.SetInitiativeTime(Math.Max(unit.InitiativeTime, state.CurrentTime) + Math.Max(1, 12 - unit.EffectiveSpeed));
            unit.BeginTurn(0);
            state.AddLog($"{unit.DisplayName} \u7ed3\u675f\u884c\u52a8\u3002");
            state.EvaluateOutcome();
            if (!state.IsVictory && !state.IsDefeat) AdvanceToNextTurn(state);
        }

        public static void WorkshopReset(UnitState hero)
        {
            if (hero == null || !hero.IsHero) throw new ArgumentException("\u53ea\u80fd\u4fee\u6539\u4e3b\u89d2\u914d\u7f6e\u3002", nameof(hero));
            hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
        }

        private static void ResolveWeaponAttack(CombatState state, UnitState attacker, string targetId)
        {
            WeaponDefinition weapon = attacker.MainHand ?? CombatCatalog.Rifle;
            if (weapon.ManaCost > 0) attacker.SpendMana(weapon.ManaCost);
            ResolveDamageAction(state, attacker, targetId, weapon.DisplayName, weapon.DamageType, weapon.Damage, weapon.Range, weapon.ArmorPierce, weapon.InitiativeDelay, null, 0);
        }

        private static void ResolveSkill(CombatState state, UnitState attacker, SkillDefinition skill, string targetId)
        {
            if (skill == null) throw new InvalidOperationException("\u672a\u88c5\u5907\u8be5\u6280\u80fd\u3002");
            if (!attacker.IsSkillReady(skill)) throw new InvalidOperationException($"{skill.DisplayName}\u8fd8\u9700\u51b7\u5374 {attacker.Cooldown(skill)} \u56de\u5408\u3002");
            attacker.SpendMana(skill.ManaCost);
            ResolveDamageAction(state, attacker, targetId, skill.DisplayName, skill.DamageType, skill.Damage, skill.Range, 0, skill.InitiativeDelay, skill.Status, skill.StatusDuration);
            attacker.SetCooldown(skill);
        }

        private static void ResolveDamageAction(CombatState state, UnitState attacker, string targetId, string sourceName, DamageType damageType, int baseDamage, int range, int armorPierce, int initiativeDelay, StatusType? status, int statusDuration)
        {
            UnitState defender = GetUnit(state, targetId);
            if (!defender.IsAlive) throw new InvalidOperationException("\u76ee\u6807\u4e0d\u53ef\u7528\u3002");
            if (Manhattan(attacker.Position, defender.Position) > range) throw new InvalidOperationException("\u76ee\u6807\u8d85\u51fa\u5c04\u7a0b\u3002");
            if (range > 1 && !state.Map.HasLineOfSight(attacker.Position, defender.Position)) throw new InvalidOperationException("\u91cd\u63a9\u4f53\u963b\u6321\u4e86\u5c04\u7ebf\u3002");
            attacker.SpendActionPoint(BasicActionPointCost);
            DamageParts parts = CalculateDamage(state, attacker, defender, baseDamage, damageType, armorPierce);
            defender.AbsorbShield(parts.Shield);
            defender.TakeDamage(parts.Final);
            if (status.HasValue && defender.IsAlive) defender.ApplyStatus(status.Value, statusDuration);
            if (initiativeDelay > 0) attacker.SetInitiativeTime(attacker.InitiativeTime + initiativeDelay);
            string statusText = status.HasValue ? $"\uff0c{StatusName(status.Value)} {statusDuration}" : string.Empty;
            state.AddLog($"{attacker.DisplayName}\u4f7f\u7528{sourceName}\u547d\u4e2d{defender.DisplayName}\uff1a{parts.Final} \u4f24\u5bb3\uff08\u76fe {parts.Shield}\uff0c\u7532 {parts.Armor}\uff0c\u6321 {parts.Block}\uff09{statusText}\u3002");
            state.EvaluateOutcome();
        }

        private static void UseQuickbar(CombatState state, UnitState unit, int index)
        {
            if (index < 0 || index >= state.Quickbar.Length || state.Quickbar[index] == null) throw new InvalidOperationException("\u8be5\u5feb\u6377\u680f\u6ca1\u6709\u53ef\u7528\u7269\u54c1\u3002");
            UseConsumable(state, unit, state.Quickbar[index], true);
            state.ClearQuickbarSlot(index);
        }

        private static void UseConsumable(CombatState state, UnitState unit, ConsumableDefinition item, bool consume)
        {
            unit.SpendActionPoint(BasicActionPointCost);
            unit.Heal(item.Heal); unit.RestoreShield(item.RestoreShield); unit.RestoreMana(item.RestoreMana);
            if (item.ClearStatus.HasValue) unit.ClearStatus(item.ClearStatus.Value);
            state.AddLog($"{unit.DisplayName}\u4f7f\u7528{item.DisplayName}\uff1a\u751f\u547d +{item.Heal}\uff0c\u62a4\u76fe +{item.RestoreShield}\u3002");
        }

        private static void ResolveLoot(CombatState state, UnitState unit)
        {
            LootContainer loot = state.Loot;
            if (loot == null || loot.IsLooted) throw new InvalidOperationException("\u6b64\u5904\u6ca1\u6709\u53ef\u641c\u522e\u7684\u6218\u5229\u54c1\u3002");
            if (Manhattan(unit.Position, loot.Position) != 1) throw new InvalidOperationException("\u53ea\u80fd\u641c\u522e\u76f8\u90bb\u7684\u6218\u5229\u54c1\u3002");
            if (!state.Backpack.CanAdd(loot.Item)) throw new InvalidOperationException("\u80cc\u5305\u5df2\u6ee1\uff0c\u9700\u8981\u5148\u8c03\u6574\u73b0\u573a\u7269\u54c1\u3002");
            unit.SpendActionPoint(BasicActionPointCost);
            state.Backpack.TryAdd(loot.Item);
            loot.MarkLooted();
            state.AddLog($"{unit.DisplayName}\u82b1\u8d39 1 AP \u641c\u522e\u4e86{loot.Item.DisplayName}\u3002");
        }

        private static void ResolveInteract(CombatState state, UnitState unit, GridPosition target)
        {
            if (Manhattan(unit.Position, target) != 1) throw new InvalidOperationException("\u53ea\u80fd\u4e0e\u76f8\u90bb\u683c\u4ea4\u4e92\u3002");
            TileState tile = state.Map.GetTile(target);
            if (!tile.IsObjective && tile.Cover == CoverType.None) throw new InvalidOperationException("\u8be5\u683c\u6ca1\u6709\u53ef\u4ea4\u4e92\u76ee\u6807\u3002");
            unit.SpendActionPoint(BasicActionPointCost); tile.Durability = Math.Max(0, tile.Durability - 3);
            state.MarkInvestigated(target);
            state.AddLog($"{unit.DisplayName} \u7834\u574f{(tile.IsObjective ? "\u4e2d\u7ee7\u5668" : "\u63a9\u4f53")}\uff08\u8010\u4e45 {tile.Durability}\uff09\u3002"); state.EvaluateOutcome();
        }

        private readonly struct DamageParts
        {
            public int Facing { get; }
            public int Cover { get; }
            public int Shield { get; }
            public int Armor { get; }
            public int Block { get; }
            public int Final { get; }
            public DamageParts(int facing, int cover, int shield, int armor, int block, int final) { Facing = facing; Cover = cover; Shield = shield; Armor = armor; Block = block; Final = final; }
        }

        private static DamageParts CalculateDamage(CombatState state, UnitState attacker, UnitState defender, int baseDamage, DamageType damageType, int armorPierce)
        {
            int facing = FacingBonus(attacker.Position, defender.Position, defender.Facing);
            int cover = damageType == DamageType.Fire ? 0 : state.Map.GetTile(defender.Position).DamageReduction;
            int incoming = Math.Max(0, baseDamage + facing - cover);
            int shield = Math.Min(defender.Shield, incoming); incoming -= shield;
            int armor = Math.Min(incoming, Math.Max(0, defender.EffectiveArmor - armorPierce)); incoming -= armor;
            int block = damageType == DamageType.Physical && facing == 0 ? Math.Min(incoming, defender.Block) : 0; incoming -= block;
            return new DamageParts(facing, cover, shield, armor, block, incoming);
        }

        private static void ResolveMove(CombatState state, UnitState unit, CombatCommand command)
        {
            if (unit.HasStatus(StatusType.Bound)) throw new InvalidOperationException("\u675f\u7f1a\u72b6\u6001\u4e0b\u65e0\u6cd5\u79fb\u52a8\u3002");
            if (!state.Map.IsInside(command.Destination)) throw new InvalidOperationException("\u76ee\u6807\u683c\u8d85\u51fa\u5730\u56fe\u8303\u56f4\u3002");
            if (state.Map.IsBlocked(command.Destination)) throw new InvalidOperationException("\u76ee\u6807\u683c\u88ab\u963b\u6321\u3002");
            if (state.IsOccupied(command.Destination, unit.Id)) throw new InvalidOperationException("\u76ee\u6807\u683c\u5df2\u88ab\u5355\u4f4d\u5360\u636e\u3002");
            if (Manhattan(command.Destination, unit.Position) > 3) throw new InvalidOperationException("\u76ee\u6807\u683c\u8d85\u51fa\u79fb\u52a8\u8303\u56f4\u3002");
            unit.SpendActionPoint(BasicActionPointCost); unit.MoveTo(command.Destination, command.Facing);
        }

        private static UnitState GetUnit(CombatState state, string unitId)
        { if (state == null) throw new ArgumentNullException(nameof(state)); return state.GetUnit(unitId) ?? throw new InvalidOperationException("\u5355\u4f4d\u4e0d\u5b58\u5728\u3002"); }
        private static UnitState GetActiveUnit(CombatState state, string unitId)
        { UnitState unit = GetUnit(state, unitId); if (state.ActiveUnitId != unitId) throw new InvalidOperationException("\u53ea\u6709\u5f53\u524d\u884c\u52a8\u5355\u4f4d\u53ef\u4ee5\u6267\u884c\u52a8\u4f5c\u3002"); return unit; }
        private static int Manhattan(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static int FacingBonus(GridPosition attacker, GridPosition defender, Facing defenderFacing)
        {
            int dx = attacker.X - defender.X; int dy = attacker.Y - defender.Y;
            bool front = (defenderFacing == Facing.North && dy > 0) || (defenderFacing == Facing.South && dy < 0) || (defenderFacing == Facing.East && dx > 0) || (defenderFacing == Facing.West && dx < 0);
            bool back = (defenderFacing == Facing.North && dy < 0) || (defenderFacing == Facing.South && dy > 0) || (defenderFacing == Facing.East && dx < 0) || (defenderFacing == Facing.West && dx > 0);
            return back ? 2 : front ? 0 : 1;
        }
        private static string StatusName(StatusType type) => type == StatusType.Burning ? "\u71c3\u70e7" : type == StatusType.Slow ? "\u7f13\u6162" : type == StatusType.Bound ? "\u675f\u7f1a" : "\u7834\u7532";
    }
}
