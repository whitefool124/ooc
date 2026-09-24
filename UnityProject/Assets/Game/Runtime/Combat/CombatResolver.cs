using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public static class CombatDebugTuning
    {
        public const int EnemyActionPointsPerTurn = 3;

        public static bool TemporaryEnemyAssistEnabled { get; set; }

        public static int ActionPointsFor(UnitState unit, int configuredActionPoints) => configuredActionPoints;

        public static int OutgoingDamageFor(UnitState attacker, int configuredDamage, DamageType type = DamageType.Physical)
        {
            int modifier = attacker == null ? 0 : attacker.StatusStrength(type == DamageType.Physical
                ? StatusType.Strength : StatusType.SpellPower);
            return Math.Max(0, configuredDamage + modifier);
        }
    }

    public static class CombatResolver
    {
        public readonly struct AttackPreview
        {
            public int BaseDamage { get; }
            public int CoverReduction { get; }
            public int ShieldAbsorption { get; }
            public int ArmorReduction { get; }
            public int BlockReduction { get; }
            public int FinalDamage { get; }
            public bool HasLineOfSight { get; }

            public AttackPreview(int baseDamage, int coverReduction, int shieldAbsorption, int armorReduction, int blockReduction, int finalDamage, bool hasLineOfSight)
            { BaseDamage = baseDamage; CoverReduction = coverReduction; ShieldAbsorption = shieldAbsorption; ArmorReduction = armorReduction; BlockReduction = blockReduction; FinalDamage = finalDamage; HasLineOfSight = hasLineOfSight; }
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
            DamageType damageType = isCast ? CombatCatalog.FireBolt.DamageType : source.DamageType;
            DamageParts parts = CalculateDamage(state, attacker, defender, damage, damageType, armorPierce);
            return new AttackPreview(CombatDebugTuning.OutgoingDamageFor(attacker, damage, damageType), parts.Cover, parts.Shield, parts.Armor, parts.Block, parts.Final, state.HasLineOfSight(attacker.Position, defender.Position));
        }

        public static AttackPreview PreviewSkillAttack(CombatState state, string attackerId, string targetId, SkillDefinition skill)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            UnitState attacker = GetUnit(state, attackerId);
            UnitState defender = GetUnit(state, targetId);
            DamageParts parts = CalculateDamage(state, attacker, defender, skill.Damage, skill.DamageType, skill.ModifierValue(SkillModifierType.ArmorPierce));
            bool lineOfSight = attacker.EffectiveRange(skill.Range) <= 1 || skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.HasLineOfSight(attacker.Position, defender.Position);
            return new AttackPreview(CombatDebugTuning.OutgoingDamageFor(attacker, skill.Damage, skill.DamageType), parts.Cover, parts.Shield, parts.Armor, parts.Block, parts.Final, lineOfSight);
        }

        public static CombatEffectExecution BeginTurn(CombatState state, string unitId)
        {
            UnitState unit = GetUnit(state, unitId);
            unit.SetActionValue(Math.Max(CombatActionTimeline.ReadyThreshold, unit.ActionValue));
            int agilityAtTurnStart = unit.StatusStrength(StatusType.Agility);
            state.SetActiveUnit(unitId);
            state.RecordTurnStart();
            state.BeginRogueliteTurn(unit);
            CombatEffectExecution execution = CombatStatusLifecycle.ResolveTurnStart(state, unit);
            LogStatusLifecycle(state, unit, execution);
            state.EvaluateOutcome();
            if (!unit.IsAlive) { if (!state.IsVictory && !state.IsDefeat) AdvanceToNextTurn(state); return execution; }
            unit.BeginTurn(CombatDebugTuning.ActionPointsFor(unit, HeroActionPointsPerTurn), agilityAtTurnStart);
            state.PassiveEffects.ResolveOwnTurnStart(state, unit);
            state.RogueSpells?.BeginOwnTurn(unitId);
            state.AddLog($"{unit.DisplayName} \u5f00\u59cb\u884c\u52a8\uff08{unit.ActionPoints} \u884c\u52a8\u70b9\uff09\u3002");
            return execution;
        }

        public static CombatEffectExecution AdvanceToNextTurn(CombatState state)
        {
            UnitState[] living = state.Units.Values.Where(unit => unit.IsAlive).ToArray();
            if (living.Length > 0 && living.All(unit => unit.ActionValue < CombatActionTimeline.ReadyThreshold))
            {
                int ticks = living.Min(unit => CombatActionTimeline.TicksUntilReady(unit.ActionValue, unit.EffectiveSpeed));
                foreach (UnitState unit in living) unit.ChangeActionValue(ticks * unit.EffectiveSpeed);
                state.SetCurrentTime(state.CurrentTime + ticks);
            }
            UnitState next = CombatActionTimeline.Order(state, living).FirstOrDefault();
            if (next == null) { state.EvaluateOutcome(); return CombatEffectExecution.Empty; }
            return BeginTurn(state, next.Id);
        }

        public static CombatEffectExecution Resolve(CombatState state, CombatCommand command)
        {
            UnitState unit = GetActiveUnit(state, command.UnitId);
            CombatEffectExecution execution;
            switch (command.Type)
            {
                case CombatCommandType.Move: execution = ResolveMove(state, unit, command); break;
                case CombatCommandType.Attack: execution = ResolveWeaponAttack(state, unit, command.TargetUnitId, 0); break;
                case CombatCommandType.Cast: execution = ResolveSkill(state, unit, CombatCatalog.FireBolt, command); break;
                case CombatCommandType.UseSkill:
                    execution = unit.IsHero && state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null
                        ? state.RogueSpells.ExecuteSlot(command.SlotIndex, command).CombatEffects
                        : state.AcademyCoreBoss != null && unit.EnemyArchetypeId == "core_overseer" && command.SlotIndex == 1
                            ? state.AcademyCoreBoss.ResolveTowerPressCommand(state, unit)
                            : ResolveSkill(state, unit, command.SlotIndex == 0 ? unit.SkillOne : unit.SkillTwo, command);
                    break;
                case CombatCommandType.UseQuickbar: execution = UseQuickbar(state, unit, command.SlotIndex); break;
                case CombatCommandType.SearchLoot: execution = SearchLoot(state, unit); break;
                case CombatCommandType.TakeLoot: execution = TakeLoot(state, unit, command.TargetUnitId); break;
                case CombatCommandType.EquipInventoryQuickbar: execution = EquipInventoryQuickbar(state, unit, command.TargetUnitId, command.SlotIndex); break;
                case CombatCommandType.UseInventoryItem: execution = UseInventoryItem(state, unit, command.TargetUnitId); break;
                case CombatCommandType.Loot: execution = ResolveLoot(state, unit); break;
                case CombatCommandType.Interact: execution = ResolveInteract(state, unit, command.Destination); break;
                case CombatCommandType.EndTurn: return EndTurn(state, unit);
                case CombatCommandType.OpenInventory: execution = ResolveOpenInventory(state, unit); break;
                case CombatCommandType.BreachCharge:
                    execution = state.ThreeMaterialPressure?.ResolveCharge(state, unit, command.Destination)
                        ?? throw new InvalidOperationException("当前战斗不支持贯场冲压。");
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(command), command.Type, "Unsupported combat command.");
            }
            ExhaustEnemyActionPoints(state, unit);
            state.AcademyCoreBoss?.ObserveCommand(state, unit, command);
            if (unit.IsHero) state.AcademyFieldEnemy?.ObserveHeroCommand(state, command);
            return execution;
        }

        private static void ExhaustEnemyActionPoints(CombatState state, UnitState unit)
        {
            if (unit.IsHero || unit.ActionPoints <= 0) return;
            CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(unit.ActionPoints));
        }

        private static CombatEffectExecution ResolveOpenInventory(CombatState state, UnitState unit)
        {
            bool equippedBackpack = state.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null &&
                state.RogueEquipment.Equipped.TryGetValue(Roguelite.EquipmentSlot.Backpack, out string backpackId) &&
                !string.IsNullOrEmpty(backpackId);
            bool free = state.InventoryOpenCount == 0 && equippedBackpack;
            if (!free && unit.ActionPoints < BasicActionPointCost) throw new InvalidOperationException("Insufficient action points.");
            state.RecordInventoryOpened();
            return free ? CombatEffectExecution.Empty :
                CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost));
        }

        public static CombatEffectExecution EndTurn(CombatState state, UnitState unit)
        {
            state.EndRogueliteTurn(unit);
            // 燃烧在自身回合结束结算（数据表口径），与规则集无关：剧情与肉鸽共用同一条生命结算。
            CombatEffectExecution burning = CombatStatusLifecycle.ResolveTurnEnd(state, unit);
            foreach (CombatEffectResult result in burning.Results)
                if (result.Kind == CombatEffectKind.DamageHealth && result.AppliedAmount > 0)
                    state.AddLog(unit.DisplayName + "\u7684\u71c3\u70e7\u89e6\u53d1\uff1a" + result.AppliedAmount + " \u4f24\u5bb3\u3002");
            unit.ChangeActionValue(-CombatActionTimeline.ReadyThreshold);
            unit.BeginTurn(0);
            state.SetActiveUnit(null);
            state.AddLog($"{unit.DisplayName} \u7ed3\u675f\u884c\u52a8\u3002");
            state.EvaluateOutcome();
            return !state.IsVictory && !state.IsDefeat ? AdvanceToNextTurn(state) : CombatEffectExecution.Empty;
        }

        public static void WorkshopReset(UnitState hero)
        {
            if (hero == null || !hero.IsHero) throw new ArgumentException("\u53ea\u80fd\u4fee\u6539\u4e3b\u89d2\u914d\u7f6e\u3002", nameof(hero));
            hero.Equip(CombatCatalog.Rifle, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
        }

        public static CombatEffectExecution ResolveWeaponAttack(CombatState state, string attackerId, string targetId,
            int flatIncomingDamageReduction = 0)
        {
            UnitState attacker = GetActiveUnit(state, attackerId);
            return ResolveWeaponAttack(state, attacker, targetId, flatIncomingDamageReduction);
        }

        private static CombatEffectExecution ResolveWeaponAttack(CombatState state, UnitState attacker, string targetId,
            int flatIncomingDamageReduction)
        {
            WeaponDefinition weapon = attacker.MainHand ?? CombatCatalog.Rifle;
            int reducedBaseDamage = state.Ruleset == CombatRuleset.Roguelite
                ? weapon.Damage
                : Math.Max(0, weapon.Damage - Math.Max(0, flatIncomingDamageReduction));
            CombatEffectExecution execution = ResolveDamageAction(state, attacker, targetId, weapon.DisplayName,
                weapon.DamageType, reducedBaseDamage, weapon.MinimumRange, weapon.Range, weapon.ArmorPierce,
                weapon.InitiativeDelay, weapon.ManaCost, null, 0);
            attacker.ClearStatus(StatusType.Prepared);
            state.RogueSpells?.AfterWeaponHit(attacker.Id, state.GetUnit(targetId));
            ExhaustEnemyActionPoints(state, attacker);
            return execution;
        }

        private static CombatEffectExecution ResolveSkill(CombatState state, UnitState attacker, SkillDefinition skill, CombatCommand command)
        {
            if (skill == null) throw new InvalidOperationException("\u672a\u88c5\u5907\u8be5\u6280\u80fd\u3002");
            if (!attacker.IsSkillReady(skill)) throw new InvalidOperationException($"{skill.DisplayName}\u8fd8\u9700\u51b7\u5374 {attacker.Cooldown(skill)} \u56de\u5408\u3002");
            IReadOnlyList<SkillValidationIssue> issues = SkillCatalogValidator.Validate(new[] { skill });
            if (issues.Count > 0) throw new InvalidOperationException($"{skill.DisplayName}\u7684\u7ec4\u5408\u65e0\u6548\uff1a{issues[0].Code}\u3002");

            List<UnitState> targets = ResolveSkillTargets(state, attacker, skill, command);
            if (skill.Damage > 0 && state.Ruleset == CombatRuleset.Roguelite && state.RogueSpells != null)
            {
                foreach (UnitState target in targets.Where(target => attacker.Position.ManhattanDistance(target.Position) > 1))
                    FireSpellEngine.ReduceIncomingDamage(state.RogueSpells.FireBattle, target.Id, attacker.Id, skill.Damage);
            }
            List<CombatEffect> effects = new List<CombatEffect>();
            if (skill.ManaCost > 0) effects.Add(CombatEffect.SpendMana(skill.ManaCost));
            effects.Add(CombatEffect.SpendActionPoints(BasicActionPointCost));

            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                UnitState primary = targets[targetIndex];
                foreach (SkillEffectDefinition definition in skill.Effects)
                {
                    if (definition.Recipient == SkillEffectRecipient.Source && targetIndex > 0) continue;
                    UnitState recipient = definition.Recipient == SkillEffectRecipient.Source ? attacker : primary;
                    AppendSkillEffect(state, attacker, recipient, skill, definition, command, effects);
                }
            }
            if (targets.Count == 0)
                foreach (SkillEffectDefinition definition in skill.Effects)
                    AppendSkillEffect(state, attacker, attacker, skill, definition, command, effects);
            if (skill.InitiativeDelay > 0) effects.Add(CombatEffect.DelayInitiative(skill.InitiativeDelay));

            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, attacker.Id, effects.ToArray());
            attacker.SetCooldown(skill);
            LogSkillExecution(state, attacker, skill, execution);
            state.EvaluateOutcome();
            ExhaustEnemyActionPoints(state, attacker);
            return execution;
        }

        private static List<UnitState> ResolveSkillTargets(CombatState state, UnitState attacker, SkillDefinition skill, CombatCommand command)
        {
            if (skill.TargetRule == SkillTargetRule.Self) return new List<UnitState> { attacker };
            if (skill.TargetRule == SkillTargetRule.GridCell || skill.TargetRule == SkillTargetRule.Destructible)
            {
                ValidateSkillDestination(state, attacker, skill, command.Destination);
                return new List<UnitState>();
            }

            UnitState primary = GetUnit(state, command.TargetUnitId);
            if (!primary.IsAlive || !MatchesTargetRule(attacker, primary, skill.TargetRule)) throw new InvalidOperationException("\u6280\u80fd\u76ee\u6807\u4e0d\u53ef\u7528\u3002");
            if (Manhattan(attacker.Position, primary.Position) < skill.MinimumRange) throw new InvalidOperationException("目标位于技能近身死区。");
            if (Manhattan(attacker.Position, primary.Position) > attacker.EffectiveRange(skill.Range)) throw new InvalidOperationException("\u76ee\u6807\u8d85\u51fa\u6280\u80fd\u5c04\u7a0b\u3002");
            if (attacker.Position.ManhattanDistance(primary.Position) > 1 && !skill.HasModifier(SkillModifierType.IgnoreLineOfSight) && !state.HasLineOfSight(attacker.Position, primary.Position)) throw new InvalidOperationException("重掩体或烟幕阻挡了技能投递。");
            if (skill.Delivery != SkillDeliveryMethod.Area) return new List<UnitState> { primary };

            int radius = skill.ModifierValue(SkillModifierType.Radius);
            return state.Units.Values.Where(unit => unit.IsAlive && MatchesTargetRule(attacker, unit, skill.TargetRule) && Manhattan(unit.Position, primary.Position) <= radius)
                .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToList();
        }

        private static bool MatchesTargetRule(UnitState attacker, UnitState target, SkillTargetRule rule) =>
            rule == SkillTargetRule.AnyUnit ||
            (rule == SkillTargetRule.EnemyUnit && attacker.IsHero != target.IsHero) ||
            (rule == SkillTargetRule.AllyUnit && attacker.IsHero == target.IsHero);

        private static void ValidateSkillDestination(CombatState state, UnitState attacker, SkillDefinition skill, GridPosition destination)
        {
            if (!state.Map.IsInside(destination)) throw new InvalidOperationException("\u6280\u80fd\u76ee\u6807\u683c\u8d85\u51fa\u5730\u56fe\u3002");
            if (Manhattan(attacker.Position, destination) < skill.MinimumRange) throw new InvalidOperationException("目标格位于技能近身死区。");
            if (Manhattan(attacker.Position, destination) > attacker.EffectiveRange(skill.Range)) throw new InvalidOperationException("\u76ee\u6807\u683c\u8d85\u51fa\u6280\u80fd\u5c04\u7a0b\u3002");
            if (skill.TargetRule == SkillTargetRule.GridCell)
            {
                if (state.Map.IsBlocked(destination)) throw new InvalidOperationException("\u76ee\u6807\u683c\u88ab\u963b\u6321\u3002");
                if (state.IsOccupied(destination, attacker.Id)) throw new InvalidOperationException("\u76ee\u6807\u683c\u5df2\u88ab\u5360\u636e\u3002");
            }
            else
            {
                TileState tile = state.Map.GetTile(destination);
                if (tile.Cover == CoverType.None && !tile.IsObjective) throw new InvalidOperationException("\u76ee\u6807\u683c\u6ca1\u6709\u53ef\u7834\u574f\u7269\u4ef6\u3002");
            }
        }

        private static void AppendSkillEffect(CombatState state, UnitState attacker, UnitState recipient, SkillDefinition skill, SkillEffectDefinition definition, CombatCommand command, List<CombatEffect> effects)
        {
            switch (definition.Type)
            {
                case SkillEffectType.Damage:
                    if (skill.Range > 1 && recipient.IsHero)
                    {
                        DamageParts incoming = CalculateDamage(state, attacker, recipient, definition.Amount, definition.DamageType, skill.ModifierValue(SkillModifierType.ArmorPierce));
                        state.ArtifactBattle?.ResolveIncomingRangedHit(recipient.Id, attacker.Id, incoming.Shield + incoming.Final);
                    }
                    DamageParts parts = CalculateDamage(state, attacker, recipient, definition.Amount, definition.DamageType, skill.ModifierValue(SkillModifierType.ArmorPierce));
                    effects.Add(CombatEffect.AbsorbShield(recipient.Id, parts.Shield));
                    effects.Add(CombatEffect.DamageHealth(recipient.Id, parts.Final));
                    break;
                case SkillEffectType.RestoreHealth: effects.Add(CombatEffect.RestoreHealth(recipient.Id, definition.Amount)); break;
                case SkillEffectType.RestoreShield: effects.Add(CombatEffect.RestoreShield(recipient.Id, definition.Amount, skill.Id)); break;
                case SkillEffectType.RestoreMana: effects.Add(CombatEffect.RestoreMana(recipient.Id, definition.Amount)); break;
                case SkillEffectType.ApplyStatus:
                    if (recipient.IsAlive) effects.Add(CombatEffect.ApplyStatus(recipient.Id,
                        definition.Status, definition.Duration, definition.Amount));
                    break;
                case SkillEffectType.ClearStatus: effects.Add(CombatEffect.ClearStatus(recipient.Id, definition.Status)); break;
                case SkillEffectType.MoveSource: effects.Add(CombatEffect.Move(command.Destination)); break;
                case SkillEffectType.DamageObject: effects.Add(CombatEffect.DamageObject(command.Destination, definition.Amount)); break;
                default: throw new ArgumentOutOfRangeException(nameof(definition), definition.Type, "Unsupported skill effect.");
            }
        }

        private static void LogSkillExecution(CombatState state, UnitState attacker, SkillDefinition skill, CombatEffectExecution execution)
        {
            int damage = Applied(execution, CombatEffectKind.DamageHealth);
            int healing = Applied(execution, CombatEffectKind.RestoreHealth);
            int shield = Applied(execution, CombatEffectKind.RestoreShield);
            int mana = Applied(execution, CombatEffectKind.RestoreMana);
            string status = string.Join("\u3001", execution.Results.Where(result => result.Kind == CombatEffectKind.ApplyStatus).Select(result =>
                StatusName(result.Status) + StatusPhaseName(result.StatusPhase) +
                (UnitState.IsAttributeStatus(result.Status) ? Signed(result.StatusStrengthAfter) : result.ValueAfter.ToString())));
            string cleared = string.Join("\u3001", execution.Results.Where(result => result.Kind == CombatEffectKind.ClearStatus && result.AppliedAmount > 0).Select(result => "\u6e05\u9664" + StatusName(result.Status)));
            List<string> summary = new List<string>();
            if (damage > 0) summary.Add(damage + " \u4f24\u5bb3");
            if (healing > 0) summary.Add("\u751f\u547d +" + healing);
            if (shield > 0) summary.Add("\u62a4\u76fe +" + shield);
            if (mana > 0) summary.Add("\u4ee5\u592a +" + mana);
            if (!string.IsNullOrEmpty(status)) summary.Add(status);
            if (!string.IsNullOrEmpty(cleared)) summary.Add(cleared);
            if (execution.Results.Any(result => result.Kind == CombatEffectKind.Move)) summary.Add("\u4f4d\u79fb");
            if (execution.Results.Any(result => result.Kind == CombatEffectKind.DamageObject)) summary.Add("\u7269\u4ef6\u8010\u4e45 -" + Applied(execution, CombatEffectKind.DamageObject));
            state.AddLog($"{attacker.DisplayName}\u4f7f\u7528{skill.DisplayName}\uff1a{(summary.Count == 0 ? "\u6548\u679c\u5df2\u7ed3\u7b97" : string.Join("\uff0c", summary))}\u3002");
        }

        private static CombatEffectExecution ResolveDamageAction(CombatState state, UnitState attacker, string targetId, string sourceName, DamageType damageType, int baseDamage, int minimumRange, int range, int armorPierce, int initiativeDelay, int manaCost, StatusType? status, int statusDuration)
        {
            UnitState defender = GetUnit(state, targetId);
            if (!defender.IsAlive) throw new InvalidOperationException("\u76ee\u6807\u4e0d\u53ef\u7528\u3002");
            if (Manhattan(attacker.Position, defender.Position) < minimumRange) throw new InvalidOperationException("目标位于武器近身死区。");
            if (Manhattan(attacker.Position, defender.Position) > attacker.EffectiveRange(range)) throw new InvalidOperationException("\u76ee\u6807\u8d85\u51fa\u5c04\u7a0b\u3002");
            if (Manhattan(attacker.Position, defender.Position) > 1 && !state.HasLineOfSight(attacker.Position, defender.Position)) throw new InvalidOperationException("重掩体或烟幕阻挡了射线。");
            if (range > 1 && defender.IsHero)
            {
                DamageParts incoming = CalculateDamage(state, attacker, defender, baseDamage, damageType, armorPierce);
                state.ArtifactBattle?.ResolveIncomingRangedHit(defender.Id, attacker.Id, incoming.Shield + incoming.Final);
            }
            DamageParts parts = CalculateDamage(state, attacker, defender, baseDamage, damageType, armorPierce);
            List<CombatEffect> effects = new List<CombatEffect>();
            if (manaCost > 0) effects.Add(CombatEffect.SpendMana(manaCost));
            effects.Add(CombatEffect.SpendActionPoints(BasicActionPointCost));
            effects.Add(CombatEffect.AbsorbShield(defender.Id, parts.Shield));
            effects.Add(CombatEffect.DamageHealth(defender.Id, parts.Final));
            if (status.HasValue && defender.Health > parts.Final) effects.Add(CombatEffect.ApplyStatus(defender.Id,
                status.Value, statusDuration));
            if (initiativeDelay > 0) effects.Add(CombatEffect.DelayInitiative(initiativeDelay));
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, attacker.Id, effects.ToArray());
            int shieldAbsorbed = Applied(execution, CombatEffectKind.AbsorbShield);
            int healthDamage = Applied(execution, CombatEffectKind.DamageHealth);
            CombatEffectResult? statusResult = execution.Results.Where(result => result.Kind == CombatEffectKind.ApplyStatus).Select(result => (CombatEffectResult?)result).FirstOrDefault();
            string statusText = statusResult.HasValue
                ? $"\uff0c{StatusName(statusResult.Value.Status)}{StatusPhaseName(statusResult.Value.StatusPhase)} " +
                    (UnitState.IsAttributeStatus(statusResult.Value.Status) ? Signed(statusResult.Value.StatusStrengthAfter) : statusResult.Value.ValueAfter.ToString())
                : string.Empty;
            state.AddLog($"{attacker.DisplayName}\u4f7f\u7528{sourceName}\u547d\u4e2d{defender.DisplayName}\uff1a{healthDamage} \u4f24\u5bb3\uff08\u76fe {shieldAbsorbed}\uff0c\u7532 {parts.Armor}\uff0c\u6321 {parts.Block}\uff09{statusText}\u3002");
            state.EvaluateOutcome();
            return execution;
        }

        private static CombatEffectExecution UseQuickbar(CombatState state, UnitState unit, int index)
        {
            if (index < 0 || index >= state.ItemQuickbar.Length || string.IsNullOrEmpty(state.ItemQuickbar[index])) throw new InvalidOperationException("\u8be5\u5feb\u6377\u680f\u6ca1\u6709\u53ef\u7528\u7269\u54c1\u3002");
            return UseInventoryItem(state, unit, state.ItemQuickbar[index]);
        }

        private static CombatEffectExecution UseConsumable(CombatState state, UnitState unit, ConsumableDefinition item)
        {
            List<CombatEffect> effects = new List<CombatEffect> { CombatEffect.SpendActionPoints(BasicActionPointCost) };
            if (item.Heal > 0) effects.Add(CombatEffect.RestoreHealth(unit.Id, item.Heal));
            if (item.RestoreShield > 0) effects.Add(CombatEffect.RestoreShield(unit.Id, item.RestoreShield, item.Id));
            if (item.RestoreMana > 0) effects.Add(CombatEffect.RestoreMana(unit.Id, item.RestoreMana));
            if (item.ClearStatus.HasValue) effects.Add(CombatEffect.ClearStatus(unit.Id, item.ClearStatus.Value));
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
            CombatEffectResult? cleared = execution.Results.Where(result => result.Kind == CombatEffectKind.ClearStatus && result.AppliedAmount > 0).Select(result => (CombatEffectResult?)result).FirstOrDefault();
            string clearText = cleared.HasValue ? $"\uff0c\u6e05\u9664{StatusName(cleared.Value.Status)}" : string.Empty;
            state.AddLog($"{unit.DisplayName}\u4f7f\u7528{item.DisplayName}\uff1a\u751f\u547d +{Applied(execution, CombatEffectKind.RestoreHealth)}\uff0c\u62a4\u76fe +{Applied(execution, CombatEffectKind.RestoreShield)}{clearText}\u3002");
            return execution;
        }

        private static CombatEffectExecution ResolveLoot(CombatState state, UnitState unit)
        {
            LootContainer loot = state.Loot;
            if (loot == null || loot.IsLooted) throw new InvalidOperationException("\u6b64\u5904\u6ca1\u6709\u53ef\u641c\u522e\u7684\u6218\u5229\u54c1\u3002");
            if (Manhattan(unit.Position, loot.Position) != 1) throw new InvalidOperationException("\u53ea\u80fd\u641c\u522e\u76f8\u90bb\u7684\u6218\u5229\u54c1\u3002");
            if (!state.Backpack.CanAdd(loot.Item)) throw new InvalidOperationException("\u80cc\u5305\u5df2\u6ee1\uff0c\u9700\u8981\u5148\u8c03\u6574\u73b0\u573a\u7269\u54c1\u3002");
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost));
            state.Backpack.TryAdd(loot.Item);
            loot.MarkLooted();
            state.AddLog($"{unit.DisplayName}\u82b1\u8d39 1 \u884c\u52a8\u70b9\u641c\u522e\u4e86{loot.Item.DisplayName}\u3002");
            return execution;
        }

        private static CombatEffectExecution SearchLoot(CombatState state, UnitState unit)
        {
            LootSourceState loot = state.LootSource;
            if (loot == null || loot.IsComplete) throw new InvalidOperationException("此处没有尚未搜索的战利品。");
            if (Manhattan(unit.Position, loot.Position) != 1) throw new InvalidOperationException("只能搜索相邻的战利品容器。");
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost));
            ItemInstance revealed = loot.RevealNext(); if (revealed == null) throw new InvalidOperationException("容器已经搜索完成。");
            state.AddLog($"{unit.DisplayName}花费 1 行动点搜索容器：发现{ItemCatalog.Get(revealed.DefinitionId).DisplayName}（还有 {loot.HiddenCount} 件未知物品）。");
            return execution;
        }

        private static CombatEffectExecution TakeLoot(CombatState state, UnitState unit, string instanceId)
        {
            LootSourceState loot = state.LootSource;
            if (loot == null) throw new InvalidOperationException("此处没有可搜刮的战利品。");
            if (Manhattan(unit.Position, loot.Position) != 1) throw new InvalidOperationException("只能拿取相邻容器中的物品。");
            ItemInstance revealed = loot.RevealedItems.FirstOrDefault(value => value.InstanceId == instanceId);
            if (revealed != null && state.RogueEquipment != null &&
                Roguelite.RogueContentCatalog.CreateAcademyV01().Equipment.Any(value => value.DefinitionId == revealed.DefinitionId))
            {
                if (!state.RogueEquipment.TryAddEquipmentFromLoot(revealed.InstanceId, revealed.DefinitionId, "combat-loot:" + loot.Id))
                    throw new InvalidOperationException("背包空间不足，需要先整理。");
                loot.ClaimRevealed(instanceId);
                state.AddLog($"{unit.DisplayName}拿取了{ItemCatalog.Get(revealed.DefinitionId).DisplayName}；拿取物品不再消耗行动点。");
                return CombatEffectExecution.Empty;
            }
            InventoryResult result = loot.Take(instanceId, state.ItemInventory);
            if (!result.Success) throw new InvalidOperationException(result.Error == InventoryError.NoSpace ? "背包空间不足，需要先整理。" : "该物品尚未发现或不可拿取。");
            ItemInstance item = state.ItemInventory.Get(instanceId); state.AddLog($"{unit.DisplayName}拿取了{ItemCatalog.Get(item.DefinitionId).DisplayName}；拿取物品不再消耗行动点。");
            return CombatEffectExecution.Empty;
        }

        private static CombatEffectExecution EquipInventoryQuickbar(CombatState state, UnitState unit, string instanceId, int slot)
        {
            CombatState validation = state.Clone(); InventoryResult preview = validation.EquipItemQuickbar(instanceId, slot);
            if (!preview.Success) throw new InvalidOperationException("无法换入快捷栏：" + preview.Error);
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost));
            InventoryResult result = state.EquipItemQuickbar(instanceId, slot); if (!result.Success) throw new InvalidOperationException("无法换入快捷栏：" + result.Error);
            state.AddLog($"{unit.DisplayName}花费 1 行动点，将{ItemCatalog.Get(state.ItemInventory.Get(instanceId).DefinitionId).DisplayName}换入快捷栏 {slot + 1}。");
            return execution;
        }

        private static CombatEffectExecution UseInventoryItem(CombatState state, UnitState unit, string instanceId)
        {
            ItemInstance instance = state.ItemInventory.Get(instanceId); if (instance == null || instance.IsDepleted) throw new InvalidOperationException("该背包物品不可用或已经耗尽。");
            ConsumableDefinition consumable = instance.DefinitionId == "medkit" ? CombatCatalog.Medkit : instance.DefinitionId == "shield_cell" ? CombatCatalog.ShieldCell : null;
            if (consumable == null) throw new InvalidOperationException("该物品需要先进入目标预览，不能直接使用。");
            CombatEffectExecution execution = UseConsumable(state, unit, consumable); state.ConsumeInventoryItem(instanceId); return execution;
        }

        private static CombatEffectExecution ResolveInteract(CombatState state, UnitState unit, GridPosition target)
        {
            if (state.RainLanternCourt?.CanBurn(state, unit, target) == true)
                return state.RainLanternCourt.Burn(state, unit, target);
            if (Manhattan(unit.Position, target) != 1) throw new InvalidOperationException("\u53ea\u80fd\u4e0e\u76f8\u90bb\u683c\u4ea4\u4e92\u3002");
            TileState tile = state.Map.GetTile(target);
            bool investigationTarget = state.Objectives.OfType<InvestigationObjective>().Any(objective => objective.Positions.Contains(target));
            bool destructibleTarget = tile.Durability > 0 && (tile.IsObjective || tile.IsDevice || tile.IsLampVine || tile.Cover != CoverType.None);
            if (!destructibleTarget && !investigationTarget) throw new InvalidOperationException("\u8be5\u683c\u6ca1\u6709\u53ef\u4ea4\u4e92\u76ee\u6807\u3002");
            CombatEffectExecution execution = destructibleTarget
                ? CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost), CombatEffect.DamageObject(target, unit.MainHand?.Damage ?? 0))
                : CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost));
            state.ArtifactBattle?.RefreshDecoyAt(target);
            state.MarkInvestigated(target);
            state.AddLog(destructibleTarget
                ? $"{unit.DisplayName} \u7834\u574f{(tile.IsDecoy ? "诱导灯" : tile.IsObjective ? "\u4e2d\u7ee7\u5668" : "\u63a9\u4f53")}\uff08\u8010\u4e45 {tile.Durability}\uff09\u3002"
                : $"{unit.DisplayName} \u5b8c\u6210\u4e86\u8c03\u67e5\u3002");
            state.EvaluateOutcome();
            return execution;
        }

        private readonly struct DamageParts
        {
            public int Cover { get; }
            public int Shield { get; }
            public int Armor { get; }
            public int Block { get; }
            public int Final { get; }
            public DamageParts(int cover, int shield, int armor, int block, int final) { Cover = cover; Shield = shield; Armor = armor; Block = block; Final = final; }
        }

        private static DamageParts CalculateDamage(CombatState state, UnitState attacker, UnitState defender, int baseDamage, DamageType damageType, int armorPierce)
        {
            baseDamage = CombatDebugTuning.OutgoingDamageFor(attacker, baseDamage, damageType);
            baseDamage = Math.Max(0, baseDamage + defender.StatusStrength(StatusType.DamageTaken));
            if (state.Ruleset == CombatRuleset.Roguelite)
            {
                DamageComponentKind kind = damageType == DamageType.Fire ? DamageComponentKind.Fire
                    : damageType == DamageType.Arcane ? DamageComponentKind.Aether : DamageComponentKind.Physical;
                DamagePacket packet = new DamagePacket("preview", attacker.Id, defender.Id, "combat_action",
                    new[] { new DamageComponent(kind, Math.Max(0, baseDamage)) });
                DamageResolution result = RogueDamageResolver.Resolve(packet, defender.Shield, defender.Health);
                return new DamageParts(0, result.ShieldAbsorbed, 0, 0, result.HealthDamage);
            }
            int cover = damageType == DamageType.Fire ? 0 : state.Map.GetTile(defender.Position).DamageReduction;
            int incoming = Math.Max(0, baseDamage - cover);
            int shield = Math.Min(defender.Shield, incoming); incoming -= shield;
            int armor = Math.Min(incoming, Math.Max(0, defender.EffectiveArmor - armorPierce)); incoming -= armor;
            int block = damageType == DamageType.Physical ? Math.Min(incoming, defender.Block) : 0; incoming -= block;
            return new DamageParts(cover, shield, armor, block, incoming);
        }

        private static CombatEffectExecution ResolveMove(CombatState state, UnitState unit, CombatCommand command)
        {
            if (unit.HasStatus(StatusType.Bound)) throw new InvalidOperationException("\u675f\u7f1a\u72b6\u6001\u4e0b\u65e0\u6cd5\u79fb\u52a8\u3002");
            if (!state.Map.IsInside(command.Destination)) throw new InvalidOperationException("\u76ee\u6807\u683c\u8d85\u51fa\u5730\u56fe\u8303\u56f4\u3002");
            if (state.Map.IsBlocked(command.Destination)) throw new InvalidOperationException("\u76ee\u6807\u683c\u88ab\u963b\u6321\u3002");
            if (state.IsOccupied(command.Destination, unit.Id)) throw new InvalidOperationException("\u76ee\u6807\u683c\u5df2\u88ab\u5355\u4f4d\u5360\u636e\u3002");
            IReadOnlyList<GridPosition> path = CombatMovementQuery.FindPath(state, unit, command.Destination);
            if (path.Count == 0) throw new InvalidOperationException("\u76ee\u6807\u683c\u6ca1\u6709\u53ef\u884c\u79fb\u52a8\u8def\u5f84\u6216\u8d85\u51fa\u79fb\u52a8\u8303\u56f4\u3002");
            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, CombatEffect.SpendActionPoints(BasicActionPointCost), CombatEffect.Move(command.Destination));
            state.AcademyFieldEnemy?.AfterMove(state, unit, path);
            state.RainLanternCourt?.AfterMove(state, unit, path);
            state.GreenhouseCollectionRoom?.AfterMove(state, unit, path);
            state.RogueSpells?.AfterMove(unit.Id, path);
            unit.ClearStatus(StatusType.Prepared);
            if (path.Skip(1).Any(position => state.Map.GetTile(position).IsWater) && unit.HasStatus(StatusType.Burning))
            {
                unit.ClearStatus(StatusType.Burning);
                state.AddLog(unit.DisplayName + "进入浅水，燃烧已移除。");
            }
            state.RogueEquipment?.AfterMove(unit.Id);
            state.ResolveBindingMarkEntry(unit);
            if (!unit.IsHero && state.ArtifactBattle != null)
            {
                ArtifactExecution reaction = state.ArtifactBattle.ResolveEnemyEntered("hero", unit.Id);
                if (reaction.Steps.Count > 0) state.AddLog("截击铃在标记格触发，完成伤害与推离。");
            }
            CombatEffectExecution pressureReaction = unit.IsHero
                ? state.PressureTest?.ResolveHeroMove(state, unit.Position) : null;
            return CombatEffectExecution.Combine(execution, pressureReaction);
        }

        private static int Applied(CombatEffectExecution execution, CombatEffectKind kind) =>
            execution.Results.Where(result => result.Kind == kind).Sum(result => result.AppliedAmount);

        private static void LogStatusLifecycle(CombatState state, UnitState unit, CombatEffectExecution execution)
        {
            foreach (CombatEffectResult result in execution.Results)
            {
                if (result.Kind == CombatEffectKind.DamageHealth && result.AppliedAmount > 0)
                    state.AddLog($"{unit.DisplayName}\u7684\u71c3\u70e7\u89e6\u53d1\uff1a{result.AppliedAmount} \u4f24\u5bb3\u3002");
                else if (result.Kind == CombatEffectKind.ReduceStatusDuration && result.StatusPhase == CombatStatusLifecyclePhase.Expired)
                    state.AddLog($"{unit.DisplayName}\u7684{StatusName(result.Status)}\u5df2\u5230\u671f\u3002");
            }
        }

        private static UnitState GetUnit(CombatState state, string unitId)
        { if (state == null) throw new ArgumentNullException(nameof(state)); return state.GetUnit(unitId) ?? throw new InvalidOperationException("\u5355\u4f4d\u4e0d\u5b58\u5728\u3002"); }
        private static UnitState GetActiveUnit(CombatState state, string unitId)
        { UnitState unit = GetUnit(state, unitId); if (state.ActiveUnitId != unitId) throw new InvalidOperationException("\u53ea\u6709\u5f53\u524d\u884c\u52a8\u5355\u4f4d\u53ef\u4ee5\u6267\u884c\u52a8\u4f5c\u3002"); return unit; }
        private static int Manhattan(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static string StatusName(StatusType type) => type == StatusType.Burning ? "燃烧" :
            type == StatusType.Bound ? "束缚" : type == StatusType.BreakStance ? "破势" :
            type == StatusType.Agility ? "敏捷" : type == StatusType.Strength ? "力量" :
            type == StatusType.SpellPower ? "法强" : type == StatusType.Speed ? "速度" :
            type == StatusType.Range ? "射程" : type == StatusType.DamageTaken ? "承伤" :
            type == StatusType.FiregroundBoost ? "火势" : type == StatusType.FiregroundVulnerable ? "助燃" : type.ToString();
        private static string StatusPhaseName(CombatStatusLifecyclePhase phase) =>
            phase == CombatStatusLifecyclePhase.Refreshed ? "\u5237\u65b0\u81f3" :
            phase == CombatStatusLifecyclePhase.Preserved ? "\u7ef4\u6301" : "\u65bd\u52a0";
        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
