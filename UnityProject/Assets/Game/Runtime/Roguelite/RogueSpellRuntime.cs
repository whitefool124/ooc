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
        public static readonly SpellDefinition FirstBattleOriginSpell = new SpellDefinition(
            RainLanternCourtRuntime.OriginSpellId, "借障导流", "aether", "defense", SpellRarity.Basic,
            1, 1, 0, "self_when_adjacent_cover", 0, "not_required",
            new[] { "first_b1_borrow_cover:shield4:next_move2" }, Array.Empty<string>(),
            new[] { "first_battle" }, false, string.Empty, "first-run-v1-b1", true);
        private readonly Dictionary<string, int> ownTurnSequences = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> availableAtTurn = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> temperingTriggeredThisTurn = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> momentumTriggeredThisTurn = new HashSet<string>(StringComparer.Ordinal);
        private readonly RogueContentCatalog catalog;
        private readonly string specializedSpellId;
        public CombatState Combat { get; }
        public RogueSpellLoadout Loadout { get; }
        public FireBattleState FireBattle { get; }

        public RogueSpellCombatRuntime(CombatState combat, RogueSpellLoadout combatSnapshot, string specializedSpellId = "")
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (combat.Ruleset != CombatRuleset.Roguelite) throw new InvalidOperationException("Rogue spell runtime requires roguelite rules.");
            Loadout = combatSnapshot ?? throw new ArgumentNullException(nameof(combatSnapshot));
            if (!Loadout.IsCombatLocked) throw new InvalidOperationException("Combat requires a locked spell snapshot.");
            catalog = RogueContentCatalog.CreateAcademyV01(); FireBattle = new FireBattleState(combat);
            this.specializedSpellId = specializedSpellId ?? string.Empty;
            RegisterPassiveDefinitions();
        }

        public void BeginOwnTurn(string unitId)
        {
            ownTurnSequences[unitId] = ownTurnSequences.TryGetValue(unitId, out int value) ? value + 1 : 1;
            temperingTriggeredThisTurn.Remove(unitId);
            momentumTriggeredThisTurn.Remove(unitId);
            FireBattle.BeginUnitTurn(unitId);
            UnitState unit = Combat.GetUnit(unitId);
            if (unit != null && unit.IsHero && HasEquipped("PASSIVE-ELITE-03") && unit.Shield < 3)
                Combat.TryGrantRogueliteShield(unitId, "PASSIVE-ELITE-03", 3 - unit.Shield);
        }

        public void EndOwnTurn(string unitId) { }

        public void AfterMove(string unitId, IReadOnlyList<GridPosition> path)
        {
            UnitState unit = Combat.GetUnit(unitId);
            if (unit != null && unit.IsHero && HasEquipped("PASSIVE-ELITE-02") &&
                !momentumTriggeredThisTurn.Contains(unitId) && path != null && path.Count >= 4)
                Combat.PassiveEffects.ArmNextWeaponDamage("PASSIVE-ELITE-02:ready", unitId, "动势点火",
                    "下次武器命中额外造成 4 点火焰伤害；本回合结束时失效。",
                    CombatPassiveSourceKind.PassiveSpell, "PASSIVE-ELITE-02", 4, DamageType.Fire);
        }

        public void AfterWeaponHit(string unitId, UnitState target)
        {
            UnitState source = Combat.GetUnit(unitId);
            if (source == null || !source.IsHero || target == null) return;
            IReadOnlyList<CombatDamageBonus> bonuses = Combat.PassiveEffects.ConsumeNextWeaponDamage(unitId);
            if (bonuses.Count == 0) return;
            momentumTriggeredThisTurn.Add(unitId);
            foreach (CombatDamageBonus bonus in bonuses)
            {
                if (!target.IsAlive) break;
                FireBattleState.ApplyRawDamage(source, target, bonus.Amount, bonus.DamageType,
                    bonus.SourceContentId, Combat);
                Combat.AddLog(bonus.DisplayName + "追加 " + bonus.Amount + " 点" +
                    (bonus.DamageType == DamageType.Fire ? "火焰" : bonus.DamageType == DamageType.Arcane ? "以太" : "物理") + "伤害。");
            }
            Combat.EvaluateOutcome();
        }

        private bool HasEquipped(string spellId) => Loadout.EquippedSpellIds.Contains(spellId);

        private void RegisterPassiveDefinitions()
        {
            if (HasEquipped("PASSIVE-ELITE-01"))
                Combat.PassiveEffects.RegisterPassive("hero", new CombatPassiveDefinition("spell:PASSIVE-ELITE-01",
                    "回火导流", CombatPassiveSourceKind.PassiveSpell, "PASSIVE-ELITE-01",
                    CombatPassiveTrigger.AfterPersonalSpellDamage,
                    "每个自己回合第一次由个人术式造成火焰伤害后，恢复 1 点个人魔力。", 20,
                    "personal_fire_damage_applied", "restore_mana:1", 1, CombatPassiveLimitScope.OwnTurn));
            if (HasEquipped("PASSIVE-ELITE-02"))
                Combat.PassiveEffects.RegisterPassive("hero", new CombatPassiveDefinition("spell:PASSIVE-ELITE-02",
                    "动势点火", CombatPassiveSourceKind.PassiveSpell, "PASSIVE-ELITE-02",
                    CombatPassiveTrigger.AfterActiveMove,
                    "主动移动至少 3 格后，本回合下一次武器命中额外造成 4 点火焰伤害。", 20,
                    "active_move_steps>=3", "arm_next_weapon_fire_damage:4", 1, CombatPassiveLimitScope.OwnTurn));
            if (HasEquipped("PASSIVE-ELITE-03"))
                Combat.PassiveEffects.RegisterPassive("hero", new CombatPassiveDefinition("spell:PASSIVE-ELITE-03",
                    "缓冲覆层", CombatPassiveSourceKind.PassiveSpell, "PASSIVE-ELITE-03",
                    CombatPassiveTrigger.OwnTurnStart,
                    "自己回合开始清除旧护盾后，若护盾低于 3，补至 3。", 20,
                    "shield_below:3_after_clear", "grant_shield_to:3", 1, CombatPassiveLimitScope.OwnTurn));
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
            if (string.IsNullOrEmpty(id)) return null;
            return id == RainLanternCourtRuntime.OriginSpellId
                ? FirstBattleOriginSpell
                : catalog.Spells.Single(value => value.DefinitionId == id);
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
            SpellDefinition spell = spellId == RainLanternCourtRuntime.OriginSpellId
                ? FirstBattleOriginSpell
                : catalog.Spells.Single(value => value.DefinitionId == spellId);
            if (spell.Role == "passive") throw new InvalidOperationException("被动术式不能主动施放。");
            UnitState source = Combat.GetUnit(command.UnitId) ?? throw new InvalidOperationException("Source unit does not exist.");
            if (source.ActionPoints < spell.ActionPointCost || source.Mana < spell.ManaCost) throw new InvalidOperationException("Insufficient action points or personal mana.");

            RogueSpellExecution execution = spell.IsBasic ? ExecuteBasic(spell, source, command) : ExecuteFire(spell, source, command);
            FireBattle.ResolveMarkedDestructions();
            bool dealtPersonalFireDamage = execution.FireEffects != null
                ? execution.FireEffects.Steps.Any(step => step.Kind == FireRuleKind.Damage && step.Applied > 0)
                : spell.DefinitionId == "BASE-FIRE-RANGED" && execution.CombatEffects.Results.Any(result =>
                    (result.Kind == CombatEffectKind.AbsorbShield || result.Kind == CombatEffectKind.DamageHealth) && result.AppliedAmount > 0);
            if (spell.Element == "fire" && dealtPersonalFireDamage &&
                HasEquipped("PASSIVE-ELITE-01") && temperingTriggeredThisTurn.Add(source.Id))
            {
                source.RestoreMana(1);
                Combat.AddLog("回火导流恢复 1 点个人魔力。");
            }
            if (spell.CooldownOwnTurns > 0)
            {
                int turn = ownTurnSequences.TryGetValue(source.Id, out int value) ? value : 0;
                availableAtTurn[source.Id + "|" + spellId] = turn + spell.CooldownOwnTurns + 1;
            }
            return execution;
        }

        private RogueSpellExecution ExecuteBasic(SpellDefinition spell, UnitState source, CombatCommand command)
        {
            if (spell.DefinitionId == RainLanternCourtRuntime.OriginSpellId)
            {
                if (Combat.RainLanternCourt == null) throw new InvalidOperationException("借障导流只在首次固定战斗中可用。");
                return new RogueSpellExecution(Combat.RainLanternCourt.CastBorrowedCover(Combat, source));
            }
            List<CombatEffect> effects = new List<CombatEffect> { CombatEffect.SpendActionPoints(spell.ActionPointCost) };
            if (spell.ManaCost > 0) effects.Add(CombatEffect.SpendMana(spell.ManaCost));
            if (spell.DefinitionId == "BASE-MANA-RECOVER") effects.Add(CombatEffect.RestoreMana(source.Id, IsSpecialized(spell.DefinitionId) ? 3 : 2));
            if (spell.DefinitionId == "BASE-AETHER-SHIELD")
            {
                CombatEffectExecution cost = CombatEffectExecutor.Execute(Combat, source.Id, effects.ToArray());
                Combat.TryGrantRogueliteShield(source.Id, spell.DefinitionId, IsSpecialized(spell.DefinitionId) ? 8 : 6);
                return new RogueSpellExecution(cost);
            }
            if (spell.DefinitionId == "BASE-FIRE-MELEE" || spell.DefinitionId == "BASE-FIRE-RANGED")
            {
                UnitState target = string.IsNullOrEmpty(command.TargetUnitId) ? null : Combat.GetUnit(command.TargetUnitId);
                GridPosition targetCell = target?.Position ?? command.Destination;
                TileState targetTile = Combat.Map.GetTile(targetCell);
                bool objectTarget = target == null && targetTile.Durability > 0 && (targetTile.Cover != CoverType.None || targetTile.IsDevice || targetTile.IsObjective || targetTile.IsLampVine);
                int distance = source.Position.ManhattanDistance(targetCell);
                if ((!objectTarget && (target == null || source.IsHero == target.IsHero)) || distance > spell.Range ||
                    (spell.Range > 1 && !Combat.HasLineOfSight(source.Position, targetCell))) throw new InvalidOperationException("Spell target is not legal.");
                int raw = CombatDebugTuning.OutgoingDamageFor(source, (spell.DefinitionId == "BASE-FIRE-MELEE" ? 8 : 6) + (IsSpecialized(spell.DefinitionId) ? 2 : 0));
                if (objectTarget) effects.Add(CombatEffect.DamageObject(targetCell, raw));
                else
                {
                    DamageResolution damage = RogueDamageResolver.Resolve(new DamagePacket("basic-" + spell.DefinitionId, source.Id, target.Id, spell.DefinitionId,
                        new[] { new DamageComponent(DamageComponentKind.Fire, raw) }), target.Shield, target.Health);
                    effects.Add(CombatEffect.AbsorbShield(target.Id, damage.ShieldAbsorbed)); effects.Add(CombatEffect.DamageHealth(target.Id, damage.HealthDamage));
                }
            }
            return new RogueSpellExecution(CombatEffectExecutor.Execute(Combat, source.Id, effects.ToArray()));
        }

        private RogueSpellExecution ExecuteFire(SpellDefinition spell, UnitState source, CombatCommand command)
        {
            FireSpellDefinition old = Specialized(FireSpellCatalog.Get(spell.DefinitionId));
            FireSpellTarget target = !string.IsNullOrEmpty(command.TargetUnitId)
                ? FireSpellTarget.Unit(command.TargetUnitId, command.AimDirection)
                : FireSpellTarget.At(command.Destination, command.AimDirection);
            return new RogueSpellExecution(CombatEffectExecution.Empty, FireSpellEngine.Execute(FireBattle, source.Id, old, target));
        }

        private bool IsSpecialized(string spellId) => string.Equals(specializedSpellId, spellId, StringComparison.Ordinal);

        // 总案 4.2.2.1 的专精表只登记这 12 项。工坊的合法目标、运行时强化共用这一处定义，
        // 避免出现"卡面写了增幅、运行时没有效果"的空结果（例：炉温护持）。
        public static bool SupportsSpecialization(string spellId)
        {
            switch (spellId)
            {
                case "BASE-FIRE-MELEE":
                case "BASE-FIRE-RANGED":
                case "BASE-AETHER-SHIELD":
                case "BASE-MANA-RECOVER":
                case "F-P-M01":
                case "F-P-M03":
                case "F-P-M06":
                case "F-P-U01":
                case "F-P-U04":
                case "F-P-U18":
                case "F-P-R01":
                case "F-P-R19":
                    return true;
                default:
                    return false;
            }
        }

        private FireSpellDefinition Specialized(FireSpellDefinition spell) =>
            IsSpecialized(spell.Id) ? ApplyAmplifySpecialization(spell) : spell;

        // 增幅刻墨的固定结果，逐条对应总案 4.2.2.1 与 OCC_锻造与专精数据表_v1.0.csv。
        public static FireSpellDefinition ApplyAmplifySpecialization(FireSpellDefinition spell)
        {
            if (spell == null || !SupportsSpecialization(spell.Id)) return spell;
            List<FireSpellRule> rules = new List<FireSpellRule>();
            foreach (FireSpellRule rule in spell.Rules)
            {
                switch (spell.Id)
                {
                    case "F-P-M01":
                        // 追加火伤 8 → 10；移动加值不变。
                        rules.Add(rule.Kind == FireRuleKind.Damage && rule.Timing == FireRuleTiming.OnTrigger && rule.Amount == 8
                            ? Copy(rule, 10) : rule);
                        break;
                    case "F-P-M03":
                        // 路径邻接单位与物件伤害 8 → 10；起点火场仍为 8 点。
                        rules.Add((rule.Kind == FireRuleKind.WeaponDamage || rule.Kind == FireRuleKind.DamageDurability) && rule.Amount == 8
                            ? Copy(rule, 10) : rule);
                        break;
                    case "F-P-M06":
                        // 下次近战命中额外造成 2 点火焰伤害。
                        rules.Add(rule);
                        if (rule.Kind == FireRuleKind.ApplyBreakStance && rule.Timing == FireRuleTiming.OnTrigger)
                            rules.Add(new FireSpellRule(FireRuleKind.Damage, 2, timing: FireRuleTiming.OnTrigger));
                        break;
                    case "F-P-U04":
                    case "F-P-U18":
                        // 单位与物件伤害 8 → 10；标记奖励与推位距离不变。
                        rules.Add((rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.DamageDurability) && rule.Amount == 8
                            ? Copy(rule, 10) : rule);
                        break;
                    case "F-P-R01":
                        // 伤害 12 → 14。
                        rules.Add(rule.Kind == FireRuleKind.Damage && rule.Amount == 12 ? Copy(rule, 14) : rule);
                        break;
                    case "F-P-R19":
                        // 中心单位与物件伤害 16 → 20；邻格 8 点不变。
                        rules.Add(rule.Scope == FireRuleScope.Primary &&
                            (rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.DamageDurability) && rule.Amount == 16
                            ? Copy(rule, 20) : rule);
                        break;
                    default:
                        rules.Add(rule);
                        break;
                }
            }
            int range = spell.Id == "F-P-U01" ? spell.Range + 1 : spell.Range;
            int shapeLength = spell.Id == "F-P-U01" ? spell.ShapeLength + 1 : spell.ShapeLength;
            return new FireSpellDefinition(spell, rules, spell.MinimumRange, range, shapeLength);
        }

        private static FireSpellRule Copy(FireSpellRule rule, int amount) =>
            new FireSpellRule(rule.Kind, amount, rule.Duration, rule.Scope, rule.Condition, rule.AlternateAmount,
                rule.AffectAllies, rule.Status, rule.Consumption, rule.DestructibleMask, rule.Timing);

        // 节流刻墨（总案 4.2.2.1）：在已生效的基础费用上再降 1 点魔力（最低 0）；回路调息的节流降的是冷却（1→0）。
        // 基础四式不在个人术式 60 张的下调范围内，因此灼触／火花／以太护幕的节流结果与专精表逐条一致。
        public static bool SupportsThrottleSpecialization(string spellId)
        {
            switch (spellId)
            {
                case "BASE-FIRE-MELEE":
                case "BASE-FIRE-RANGED":
                case "BASE-AETHER-SHIELD":
                case "BASE-MANA-RECOVER":
                case "F-P-M01":
                case "F-P-M06":
                case "F-P-U01":
                case "F-P-R01":
                    return true;
                default:
                    return false;
            }
        }

        public static FireSpellDefinition ApplyThrottleSpecialization(FireSpellDefinition spell)
        {
            if (spell == null || !SupportsThrottleSpecialization(spell.Id)) return spell;
            int cooldown = spell.Id == "BASE-MANA-RECOVER" ? Math.Max(0, spell.Cooldown - 1) : spell.Cooldown;
            int mana = spell.Id == "BASE-MANA-RECOVER" ? spell.ManaCost : Math.Max(0, spell.ManaCost - 1);
            return new FireSpellDefinition(spell, spell.Rules, spell.MinimumRange, spell.Range, spell.ShapeLength, mana, cooldown);
        }
    }

    public static class RogueSpellRuleInterpreter
    {
        private static readonly string[] AllowedPrefixes =
        { "legacy_rule:", "apply_break_stance", "grant_shield_before_ranged", "clear_one_self_status",
          "first_personal_fire_damage_restore_mana:", "move_3_then_weapon_fire_damage:", "turn_start_shield_if_zero:" };

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
