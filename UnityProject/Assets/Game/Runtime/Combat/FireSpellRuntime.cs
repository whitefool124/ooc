using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    public sealed class FirePendingEffect
    {
        public FireSpellDefinition Spell { get; }
        public string SourceUnitId { get; }
        public string MarkedUnitId { get; }
        public GridPosition MarkedCell { get; }
        public CardinalDirection Direction { get; }
        public int Stage { get; }

        public FirePendingEffect(FireSpellDefinition spell, string sourceUnitId, string markedUnitId,
            GridPosition markedCell, CardinalDirection direction, int stage = 0)
        {
            Spell = spell ?? throw new ArgumentNullException(nameof(spell));
            SourceUnitId = sourceUnitId; MarkedUnitId = markedUnitId; MarkedCell = markedCell;
            Direction = direction; Stage = stage;
        }

        public FirePendingEffect Clone() => new FirePendingEffect(Spell, SourceUnitId, MarkedUnitId, MarkedCell, Direction, Stage);
    }

    public sealed class FirePursuitPreview
    {
        public GridPosition Destination { get; }
        public bool WillMove { get; }
        public string Reason { get; }
        public string DetailCode { get; }

        public FirePursuitPreview(GridPosition destination, bool willMove, string reason, string detailCode)
        {
            Destination = destination;
            WillMove = willMove;
            Reason = reason ?? string.Empty;
            DetailCode = detailCode ?? string.Empty;
        }
    }

    public sealed class FiregroundState
    {
        public const int BaseDamage = 8;
        public int Damage { get; private set; }
        public int RemainingTurns { get; private set; }
        public string SourceSpellId { get; private set; }
        public string SourceUnitId { get; private set; }
        public FiregroundState(int damage, int remainingTurns, string sourceSpellId, string sourceUnitId = null)
        { Damage = ResolveDamage(damage); RemainingTurns = remainingTurns; SourceSpellId = sourceSpellId; SourceUnitId = sourceUnitId; }
        public void Refresh(int damage, int remainingTurns, string sourceSpellId, string sourceUnitId = null)
        { Damage = ResolveDamage(damage); RemainingTurns = remainingTurns; SourceSpellId = sourceSpellId; SourceUnitId = sourceUnitId; }
        public void Extend(int minimumDamage, int additionalTurns, string sourceSpellId, string sourceUnitId = null)
        { Damage = Math.Max(Damage, ResolveDamage(minimumDamage)); RemainingTurns += additionalTurns; SourceSpellId = sourceSpellId; SourceUnitId = sourceUnitId; }
        public void Tick() => RemainingTurns = Math.Max(0, RemainingTurns - 1);
        public FiregroundState Clone() => new FiregroundState(Damage, RemainingTurns, SourceSpellId, SourceUnitId);

        // 总案 3.5.6.1／3.5.1.2：火场按来源配置伤害（8 或 12）；来源未声明时取基准 8。
        private static int ResolveDamage(int damage) => damage > 0 ? damage : BaseDamage;
    }

    public sealed class MeltBarrierMarkState
    {
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public GridPosition TargetCell { get; }
        public int RemainingSourceTurns { get; private set; }

        public MeltBarrierMarkState(string sourceUnitId, string targetUnitId, GridPosition targetCell, int remainingSourceTurns)
        { SourceUnitId = sourceUnitId; TargetUnitId = targetUnitId ?? string.Empty; TargetCell = targetCell; RemainingSourceTurns = remainingSourceTurns; }
        public void Tick() => RemainingSourceTurns = Math.Max(0, RemainingSourceTurns - 1);
        public MeltBarrierMarkState Clone() => new MeltBarrierMarkState(SourceUnitId, TargetUnitId, TargetCell, RemainingSourceTurns);
    }

    public sealed class FireBattleState
    {
        private readonly Dictionary<GridPosition, FiregroundState> firegrounds = new Dictionary<GridPosition, FiregroundState>();
        private readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<GridPosition> overloadedDevices = new HashSet<GridPosition>();
        private readonly HashSet<string> firegroundTriggeredThisTurn = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<FirePendingEffect> pendingEffects = new List<FirePendingEffect>();
        private readonly Dictionary<string, int> weaponMaintenance = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, MeltBarrierMarkState> meltBarrierMarks = new Dictionary<string, MeltBarrierMarkState>(StringComparer.Ordinal);
        public CombatState Combat { get; }
        public IReadOnlyDictionary<GridPosition, FiregroundState> Firegrounds => firegrounds;
        public IReadOnlyCollection<GridPosition> OverloadedDevices => overloadedDevices;
        public IReadOnlyList<FirePendingEffect> PendingEffects => pendingEffects;
        public IReadOnlyCollection<MeltBarrierMarkState> MeltBarrierMarks => meltBarrierMarks.Values;
        public bool HasReservedNextTurnAction(string unitId) =>
            Combat.PassiveEffects.HasOngoingEffect(unitId, "fire:F-P-U01:next-turn-action");
        public bool IsDeviceOverloaded(GridPosition position) => overloadedDevices.Contains(position);
        public FireBattleState(CombatState combat) => Combat = combat ?? throw new ArgumentNullException(nameof(combat));
        private static string CooldownKey(string unitId, string spellId) => unitId + "|" + spellId;
        public int Cooldown(string unitId, string spellId) => cooldowns.TryGetValue(CooldownKey(unitId, spellId), out int value) ? value : 0;
        internal void SetCooldown(string unitId, string spellId, int turns) { if (turns > 0) cooldowns[CooldownKey(unitId, spellId)] = turns; }
        public void BeginUnitTurn(string unitId)
        {
            firegroundTriggeredThisTurn.Remove(unitId);
            string prefix = unitId + "|";
            foreach (string key in cooldowns.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            { int next = cooldowns[key] - 1; if (next <= 0) cooldowns.Remove(key); else cooldowns[key] = next; }
            UnitState unit = Combat.GetUnit(unitId);
            // OCC is a single-protagonist combat. A field effect measured in
            // "turns" advances once per protagonist turn, not once per unit;
            // otherwise adding enemies silently shortens the same fireground.
            if (unit != null && unit.IsHero)
            {
                foreach (GridPosition position in firegrounds.Keys.ToArray())
                {
                    firegrounds[position].Tick();
                    if (firegrounds[position].RemainingTurns <= 0) firegrounds.Remove(position);
                }
            }
            RemoveExpiredEnvironment(Combat.CurrentTime);
            if (unit != null && firegrounds.TryGetValue(unit.Position, out FiregroundState ground))
            {
                ApplyRawFireDamage(unit, FiregroundDamage(ground, unit), Combat);
                firegroundTriggeredThisTurn.Add(unit.Id);
                Combat.EvaluateOutcome();
            }
            pendingEffects.RemoveAll(effect => effect.SourceUnitId == unitId && effect.Spell.TriggerWindow == FireTriggerWindow.UntilNextAction);
            foreach (string key in meltBarrierMarks.Where(pair => pair.Value.SourceUnitId == unitId).Select(pair => pair.Key).ToArray())
            {
                meltBarrierMarks[key].Tick();
                if (meltBarrierMarks[key].RemainingSourceTurns <= 0) meltBarrierMarks.Remove(key);
            }
        }
        public bool HasFireground(GridPosition position) => firegrounds.ContainsKey(position);
        public void RemoveFireground(GridPosition position) => firegrounds.Remove(position);
        public int ResolveEntry(UnitState unit, GridPosition previousPosition)
        {
            if (unit == null || unit.Position == previousPosition || firegroundTriggeredThisTurn.Contains(unit.Id) || !firegrounds.TryGetValue(unit.Position, out FiregroundState ground)) return 0;
            firegroundTriggeredThisTurn.Add(unit.Id);
            int damage = ApplyRawFireDamage(unit, FiregroundDamage(ground, unit), Combat);
            Combat.EvaluateOutcome();
            return damage;
        }
        public void CreateOrRefreshFireground(GridPosition position, int damage, int duration, string sourceSpellId, string sourceUnitId = null)
        {
            TileState tile = Combat.Map.GetTile(position);
            // 效果层每格至多一个：后生成者覆盖先在者，这里先清空其余效果层。
            // 火场与浅水互为覆盖关系，不生成隐藏的第三种蒸汽状态。
            tile.ClearEffectLayers();
            if (firegrounds.TryGetValue(position, out FiregroundState current)) current.Refresh(damage, duration, sourceSpellId, sourceUnitId);
            else firegrounds[position] = new FiregroundState(damage, duration, sourceSpellId, sourceUnitId);
        }
        public void CreateOrRefreshShallowWater(GridPosition position)
        {
            TileState tile = Combat.Map.GetTile(position);
            firegrounds.Remove(position);
            tile.ClearEffectLayers();
            tile.IsWater = true;
        }
        /// <summary>生成散页。散页是通用可燃材料，覆盖本格其余效果层。</summary>
        public void CreateOrRefreshLoosePaper(GridPosition position)
        {
            TileState tile = Combat.Map.GetTile(position);
            firegrounds.Remove(position);
            tile.ClearEffectLayers();
            tile.IsLoosePaper = true;
        }
        /// <summary>生成痕迹。追踪类单位可读取；被浅水、火场或强风清除。</summary>
        public void CreateOrRefreshTrace(GridPosition position)
        {
            TileState tile = Combat.Map.GetTile(position);
            firegrounds.Remove(position);
            tile.ClearEffectLayers();
            tile.HasTrace = true;
        }
        /// <summary>生成约束纹。进入该格的单位本回合留在原地。</summary>
        public void CreateOrRefreshBindingMark(GridPosition position)
        {
            TileState tile = Combat.Map.GetTile(position);
            firegrounds.Remove(position);
            tile.ClearEffectLayers();
            tile.IsBindingMark = true;
        }
        /// <summary>用浅水把散页打湿：散页保留但暂时不可燃。</summary>
        public bool SoakPaper(GridPosition position)
        {
            TileState tile = Combat.Map.GetTile(position);
            if (!tile.IsLoosePaper) return false;
            tile.IsPaperSoaked = true;
            return true;
        }
        /// <summary>点燃散页：未被打湿的散页转为火场。返回是否真的点燃。</summary>
        public bool IgniteLoosePaper(GridPosition position, int damage, int duration, string sourceSpellId, string sourceUnitId = null)
        {
            TileState tile = Combat.Map.GetTile(position);
            if (!tile.IsLoosePaper || tile.IsPaperSoaked) return false;
            CreateOrRefreshFireground(position, damage, duration, sourceSpellId, sourceUnitId);
            return true;
        }
        /// <summary>清除本格全部效果层（含火场）。封存库退件等"清掉一个场地效果"的效果使用它。</summary>
        public void ClearEffectLayer(GridPosition position)
        {
            firegrounds.Remove(position);
            Combat.Map.GetTile(position).ClearEffectLayers();
        }
        public void ExtendFireground(GridPosition position, int minimumDamage, int duration, string sourceSpellId, string sourceUnitId = null)
        {
            if (firegrounds.TryGetValue(position, out FiregroundState current)) current.Extend(minimumDamage, duration, sourceSpellId, sourceUnitId);
        }
        private int FiregroundDamage(FiregroundState ground, UnitState target)
        {
            UnitState owner = string.IsNullOrWhiteSpace(ground.SourceUnitId) ? null : Combat.GetUnit(ground.SourceUnitId);
            int boost = owner != null && owner.IsHero ? owner.StatusStrength(StatusType.FiregroundBoost) : 0;
            return ground.Damage + boost + target.StatusStrength(StatusType.FiregroundVulnerable);
        }
        private void RemoveExpiredEnvironment(int time)
        {
            foreach (GridPosition position in Combat.Map.PositionsWith(tile => tile.SmokeExpiresAt > 0 && tile.SmokeExpiresAt <= time).ToArray()) Combat.Map.GetTile(position).SmokeExpiresAt = 0;
        }
        internal void Overload(GridPosition position) => overloadedDevices.Add(position);
        internal void Arm(FirePendingEffect effect)
        {
            pendingEffects.RemoveAll(existing => existing.SourceUnitId == effect.SourceUnitId && existing.Spell.Id == effect.Spell.Id);
            pendingEffects.Add(effect);
        }
        internal void Consume(FirePendingEffect effect) => pendingEffects.Remove(effect);
        internal void AddWeaponMaintenance(string unitId, int amount)
        {
            if (amount <= 0) return;
            weaponMaintenance[unitId] = WeaponMaintenance(unitId) + amount;
        }
        public int WeaponMaintenance(string unitId) => weaponMaintenance.TryGetValue(unitId, out int value) ? value : 0;
        internal void MarkMeltBarrier(string sourceUnitId, UnitState target, GridPosition cell, int duration)
        {
            string key = target == null ? "cell:" + cell.X + "," + cell.Y : "unit:" + target.Id;
            meltBarrierMarks[key] = new MeltBarrierMarkState(sourceUnitId, target?.Id, cell, duration);
        }
        internal void ReserveActionNextTurn(string unitId) => Combat.PassiveEffects.ScheduleNextTurn(
            "fire:F-P-U01:next-turn-action", unitId, "脱线疾行", "下次自己回合获得 1 点额外行动力。",
            CombatPassiveSourceKind.ActiveSpell, "F-P-U01", CombatOngoingEffectKind.NextTurnActionPoints, 1);
        public bool IsThreatenedByEnemy(UnitState source, GridPosition cell)
        {
            if (source == null) return false;
            return Combat.Units.Values.Where(unit => unit.IsAlive && unit.IsHero != source.IsHero).Any(enemy =>
            {
                int range = enemy.MainHand?.Range ?? 0;
                foreach (SkillDefinition skill in new[] { enemy.SkillOne, enemy.SkillTwo }.Where(value => value != null && value.Damage > 0))
                    range = Math.Max(range, skill.Range);
                int distance = enemy.Position.ManhattanDistance(cell);
                return distance > 0 && distance <= range && (range <= 1 || Combat.HasLineOfSight(enemy.Position, cell));
            });
        }
        public void ResolveMarkedDestructions()
        {
            foreach (string key in meltBarrierMarks.Keys.ToArray())
            {
                MeltBarrierMarkState mark = meltBarrierMarks[key];
                UnitState target = string.IsNullOrEmpty(mark.TargetUnitId) ? null : Combat.GetUnit(mark.TargetUnitId);
                bool destroyed = target != null ? !target.IsAlive : Combat.Map.GetTile(mark.TargetCell).Durability <= 0;
                if (!destroyed) continue;
                UnitState source = Combat.GetUnit(mark.SourceUnitId);
                if (source != null && source.IsAlive)
                {
                    source.RestoreMana(2);
                    if (Combat.Ruleset == CombatRuleset.Roguelite) Combat.TryGrantRogueliteShield(source.Id, "melt-barrier-mark", 4);
                    else source.GrantShield(4);
                    Combat.AddLog(source.DisplayName + "触发熔障标记：恢复 2 魔力并获得 4 护盾。");
                }
                meltBarrierMarks.Remove(key);
            }
        }
        public void EndDeviceAction(GridPosition position) => overloadedDevices.Remove(position);
        public FireBattleState Clone()
        {
            FireBattleState clone = new FireBattleState(Combat.Clone());
            foreach (var pair in firegrounds) clone.firegrounds[pair.Key] = pair.Value.Clone();
            foreach (var pair in cooldowns) clone.cooldowns[pair.Key] = pair.Value;
            foreach (GridPosition position in overloadedDevices) clone.overloadedDevices.Add(position);
            foreach (string unitId in firegroundTriggeredThisTurn) clone.firegroundTriggeredThisTurn.Add(unitId);
            foreach (FirePendingEffect effect in pendingEffects) clone.pendingEffects.Add(effect.Clone());
            foreach (var pair in weaponMaintenance) clone.weaponMaintenance[pair.Key] = pair.Value;
            foreach (var pair in meltBarrierMarks) clone.meltBarrierMarks[pair.Key] = pair.Value.Clone();
            return clone;
        }
        internal static int ApplyRawFireDamage(UnitState target, int amount, GridMap map = null)
        {
            int cover = map == null ? 0 : map.GetTile(target.Position).DamageReduction * 4;
            int reduction = Math.Min(16, target.EffectiveArmor + cover);
            int effective = Math.Max(4, amount - reduction);
            int absorbed = target.AbsorbShield(effective);
            int health = effective - absorbed;
            target.TakeDamage(health);
            return health;
        }

        internal static int ApplyRawFireDamage(UnitState target, int amount, CombatState combat)
        {
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite) return ApplyRawFireDamage(target, amount, combat?.Map);
            return ApplyRogueliteDamage(target, amount, DamageComponentKind.Fire, "fire_runtime", combat);
        }

        internal static int ApplyRawWeaponDamage(UnitState source, UnitState target, int amount, GridMap map)
        {
            amount = CombatDebugTuning.OutgoingDamageFor(source, amount);
            int cover = map.GetTile(target.Position).DamageReduction;
            int incoming = Math.Max(0, amount - cover);
            int absorbed = target.AbsorbShield(incoming); incoming -= absorbed;
            int armor = Math.Min(incoming, target.EffectiveArmor); incoming -= armor;
            int block = Math.Min(incoming, target.Block); incoming -= block;
            target.TakeDamage(incoming);
            return incoming;
        }

        internal static int ApplyRawWeaponDamage(UnitState source, UnitState target, int amount, CombatState combat)
        {
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite) return ApplyRawWeaponDamage(source, target, amount, combat?.Map);
            return ApplyRogueliteDamage(target, CombatDebugTuning.OutgoingDamageFor(source, amount), DamageComponentKind.Physical, "fire_weapon_rule", combat);
        }

        internal static int ApplyRawDamage(UnitState source, UnitState target, int amount, DamageType damageType,
            string sourceEffectId, CombatState combat)
        {
            if (damageType == DamageType.Fire)
                return combat == null || combat.Ruleset != CombatRuleset.Roguelite
                    ? ApplyRawFireDamage(target, amount, combat?.Map)
                    : ApplyRogueliteDamage(target, CombatDebugTuning.OutgoingDamageFor(source, amount),
                        DamageComponentKind.Fire, sourceEffectId, combat);
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite)
                return ApplyRawWeaponDamage(source, target, amount, combat?.Map);
            DamageComponentKind kind = damageType == DamageType.Arcane ? DamageComponentKind.Aether : DamageComponentKind.Physical;
            return ApplyRogueliteDamage(target, CombatDebugTuning.OutgoingDamageFor(source, amount), kind, sourceEffectId, combat);
        }

        private static int ApplyRogueliteDamage(UnitState target, int amount, DamageComponentKind kind, string sourceEffectId, CombatState combat)
        {
            DamagePacket packet = new DamagePacket(sourceEffectId + "-packet", string.Empty, target.Id, sourceEffectId,
                new[] { new DamageComponent(kind, Math.Max(0, amount)) });
            DamageResolution resolution = RogueDamageResolver.Resolve(packet, target.Shield, target.Health);
            target.AbsorbShield(resolution.ShieldAbsorbed);
            combat.RecordRogueliteShieldAbsorption(target.Id, sourceEffectId, resolution.ShieldAbsorbed);
            target.TakeDamage(resolution.HealthDamage);
            return resolution.HealthDamage;
        }
    }

    public readonly struct FireSpellTarget
    {
        public string UnitId { get; }
        public GridPosition Cell { get; }
        public CardinalDirection Direction { get; }
        public FireSpellTarget(string unitId, GridPosition cell, CardinalDirection direction) { UnitId = unitId; Cell = cell; Direction = direction; }
        public static FireSpellTarget Unit(string id, CardinalDirection direction = CardinalDirection.East) => new FireSpellTarget(id, default, direction);
        public static FireSpellTarget At(GridPosition cell, CardinalDirection direction) => new FireSpellTarget(null, cell, direction);
    }

    public sealed class FireSpellPreview
    {
        public FireSpellDefinition Spell { get; }
        public bool CanCommit => Failures.Count == 0;
        public IReadOnlyList<string> Failures { get; }
        public IReadOnlyList<GridPosition> Cells { get; }
        public IReadOnlyList<string> UnitIds { get; }
        public IReadOnlyList<GridPosition> Destructibles { get; }
        public bool FriendlyFireRisk { get; }
        public bool ConsumesBurning { get; }
        public FireSpellPreview(FireSpellDefinition spell, IEnumerable<string> failures, IEnumerable<GridPosition> cells,
            IEnumerable<string> unitIds, IEnumerable<GridPosition> destructibles, bool friendlyFireRisk, bool consumesBurning)
        { Spell = spell; Failures = failures.ToArray(); Cells = cells.ToArray(); UnitIds = unitIds.ToArray(); Destructibles = destructibles.ToArray(); FriendlyFireRisk = friendlyFireRisk; ConsumesBurning = consumesBurning; }
    }

    public readonly struct FireSpellResultStep
    {
        public int Sequence { get; }
        public string SpellId { get; }
        public FireRuleKind Kind { get; }
        public string TargetId { get; }
        public GridPosition Cell { get; }
        public int Requested { get; }
        public int Applied { get; }
        public string Detail { get; }
        public FireDeliveryMode DeliveryMode { get; }
        public FireTriggerWindow TriggerWindow { get; }
        public FireConsumptionRule ConsumptionRule { get; }
        public FireSpellResultStep(int sequence, string spellId, FireRuleKind kind, string targetId, GridPosition cell, int requested, int applied, string detail)
            : this(sequence, spellId, kind, targetId, cell, requested, applied, detail,
                FireDeliveryMode.DetachedProjection, FireTriggerWindow.Immediate, FireConsumptionRule.OnCast) { }
        public FireSpellResultStep(int sequence, string spellId, FireRuleKind kind, string targetId, GridPosition cell,
            int requested, int applied, string detail, FireDeliveryMode deliveryMode, FireTriggerWindow triggerWindow,
            FireConsumptionRule consumptionRule)
        {
            Sequence = sequence; SpellId = spellId; Kind = kind; TargetId = targetId; Cell = cell;
            Requested = requested; Applied = applied; Detail = detail; DeliveryMode = deliveryMode;
            TriggerWindow = triggerWindow; ConsumptionRule = consumptionRule;
        }
    }

    public sealed class FireSpellExecution
    {
        public FireSpellPreview Preview { get; }
        public IReadOnlyList<FireSpellResultStep> Steps { get; }
        public string SourceUnitId { get; }
        public GridPosition SourcePosition { get; }
        public bool IsTriggered { get; }

        public FireSpellExecution(FireSpellPreview preview, IEnumerable<FireSpellResultStep> steps,
            string sourceUnitId, GridPosition sourcePosition, bool isTriggered = false)
        {
            if (string.IsNullOrEmpty(sourceUnitId)) throw new ArgumentException("A fire execution requires its actual source.", nameof(sourceUnitId));
            Preview = preview; Steps = steps.ToArray();
            SourceUnitId = sourceUnitId; SourcePosition = sourcePosition; IsTriggered = isTriggered;
        }
    }

    public sealed class FireWeaponAttackResolution
    {
        public CombatEffectExecution WeaponExecution { get; }
        public IReadOnlyList<FireSpellExecution> TriggerExecutions { get; }
        public int IncomingDamageReduction { get; }

        public FireWeaponAttackResolution(CombatEffectExecution weaponExecution,
            IEnumerable<FireSpellExecution> triggerExecutions, int incomingDamageReduction)
        {
            WeaponExecution = weaponExecution ?? CombatEffectExecution.Empty;
            TriggerExecutions = (triggerExecutions ?? Array.Empty<FireSpellExecution>()).ToArray();
            IncomingDamageReduction = incomingDamageReduction;
        }
    }

    public static class FireSpellEngine
    {
        public static FireWeaponAttackResolution ResolveWeaponAttack(FireBattleState battle, string attackerUnitId,
            string targetUnitId, GridPosition? followUpCell = null, CardinalDirection followUpDirection = CardinalDirection.East)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            UnitState attacker = battle.Combat.GetUnit(attackerUnitId) ?? throw new InvalidOperationException("Attacker does not exist.");
            UnitState target = battle.Combat.GetUnit(targetUnitId) ?? throw new InvalidOperationException("Target does not exist.");
            WeaponDefinition weapon = attacker.MainHand ?? CombatCatalog.Rifle;
            int reducedDamage = ReduceIncomingDamage(battle, targetUnitId, attackerUnitId, weapon.Damage);
            int reduction = Math.Max(0, weapon.Damage - reducedDamage);
            CombatEffectExecution weaponExecution = CombatResolver.ResolveWeaponAttack(battle.Combat, attackerUnitId,
                targetUnitId, reduction);
            List<FireSpellExecution> triggers = new List<FireSpellExecution>();
            triggers.AddRange(TriggerWeaponAttack(battle, attackerUnitId, targetUnitId, followUpCell, followUpDirection));
            triggers.AddRange(TriggerIncomingAdjacentAttack(battle, attackerUnitId, targetUnitId));
            battle.Combat.EvaluateOutcome();
            return new FireWeaponAttackResolution(weaponExecution, triggers, reduction);
        }

        public static FireSpellPreview Preview(FireBattleState battle, string sourceUnitId, FireSpellDefinition spell, FireSpellTarget target)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (spell == null) throw new ArgumentNullException(nameof(spell));
            UnitState source = battle.Combat.GetUnit(sourceUnitId) ?? throw new InvalidOperationException("Source unit does not exist.");
            UnitState primary = string.IsNullOrEmpty(target.UnitId) ? null : battle.Combat.GetUnit(target.UnitId);
            GridPosition center = primary?.Position ?? (spell.TargetKind == FireTargetKind.Self ? source.Position : target.Cell);
            List<string> failures = new List<string>();
            if (source.ActionPoints < spell.ActionPointCost) failures.Add("行动点不足");
            if (source.Mana < spell.ManaCost) failures.Add("魔力不足");
            if (battle.Cooldown(sourceUnitId, spell.Id) > 0) failures.Add("冷却中");
            int selfLoss = spell.Rules.Where(rule => rule.Kind == FireRuleKind.LoseHealth).Select(rule => rule.Amount).DefaultIfEmpty(0).Max();
            if (selfLoss > 0 && source.Health <= selfLoss) failures.Add("你的生命太低，承受不了这次代价");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.ClearOneSelfStatus) &&
                !new[] { StatusType.Burning, StatusType.Slow, StatusType.Bound, StatusType.BreakStance }.Any(source.HasStatus)) failures.Add("没有可清除的自身状态");
            if (!FireSpellCatalog.IsWeaponCompatible(spell, source.MainHand)) failures.Add("武器要求不符");
            if (spell.TargetKind == FireTargetKind.Self && string.IsNullOrEmpty(target.UnitId) && target.Cell != source.Position)
                failures.Add("只能选择自身");
            int distance = source.Position.ManhattanDistance(center);
            if (distance < spell.MinimumRange && spell.TargetKind != FireTargetKind.Self)
                failures.Add("目标位于近身死区（需至少 " + spell.MinimumRange + " 格）");
            if (distance > spell.Range && spell.TargetKind != FireTargetKind.Self) failures.Add("超出射程");
            if (spell.RequiresLineOfSight && !battle.Combat.HasLineOfSight(source.Position, center)) failures.Add("视线受阻");
            ValidateTarget(battle, source, primary, center, spell, failures);
            List<GridPosition> cells = SelectCells(battle.Combat, source.Position, center, target.Direction, spell).Distinct().Where(battle.Combat.Map.IsInside).ToList();
            if (primary != null && spell.TargetKind == FireTargetKind.Unit && spell.Shape != FireSelectionShape.Single &&
                !cells.Contains(primary.Position)) failures.Add("所选单位不在作用范围内");
            string[] units = battle.Combat.Units.Values.Where(unit => unit.IsAlive && cells.Contains(unit.Position)).OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id).ToArray();
            ValidateConsumption(battle, spell, primary, cells, units, failures);
            GridPosition[] objects = cells.Where(cell => { TileState tile = battle.Combat.Map.GetTile(cell); return tile.Cover != CoverType.None || tile.IsObjective || tile.IsDevice || tile.IsLampVine; }).ToArray();
            bool friendly = units.Select(battle.Combat.GetUnit).Any(unit => unit.IsHero == source.IsHero && unit.Id != source.Id) && spell.Rules.Any(rule => rule.AffectAllies);
            bool consumes = spell.Rules.Any(rule => rule.Kind == FireRuleKind.ConsumeBurning || rule.Kind == FireRuleKind.ConsumeFireground);
            return new FireSpellPreview(spell, failures, cells, units, objects, friendly, consumes);
        }

        public static FireSpellExecution Execute(FireBattleState battle, string sourceUnitId, FireSpellDefinition spell, FireSpellTarget target)
        {
            FireSpellPreview preview = Preview(battle, sourceUnitId, spell, target);
            if (!preview.CanCommit) throw new InvalidOperationException(string.Join("；", preview.Failures));
            UnitState source = battle.Combat.GetUnit(sourceUnitId);
            GridPosition sourceOrigin = source.Position;
            UnitState primary = string.IsNullOrEmpty(target.UnitId) ? null : battle.Combat.GetUnit(target.UnitId);
            GridPosition center = primary?.Position ?? (spell.TargetKind == FireTargetKind.Self ? source.Position : target.Cell);
            List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
            source.SpendActionPoint(spell.ActionPointCost); Add(steps, spell, FireRuleKind.SpendActionPoints, source.Id, source.Position, spell.ActionPointCost, spell.ActionPointCost, "spend_ap");
            source.SpendMana(spell.ManaCost); Add(steps, spell, FireRuleKind.SpendMana, source.Id, source.Position, spell.ManaCost, spell.ManaCost, "spend_mana");
            battle.Combat.RogueEquipment?.OnPersonalSpellPaid(battle.Combat, source.Id, spell.ManaCost);
            foreach (FireSpellRule rule in spell.Rules.Where(rule => rule.Timing == FireRuleTiming.OnCast))
            {
                if (rule.Kind == FireRuleKind.RestoreMovement)
                {
                    source.SetMovementRangeForTurn(rule.Amount);
                    Add(steps, spell, rule.Kind, source.Id, source.Position, rule.Amount, source.MovementRangeThisTurn, "movement_range");
                    continue;
                }
                IEnumerable<GridPosition> cells = CellsForScope(rule.Scope, preview.Cells, sourceOrigin, center, target.Direction, battle.Combat);
                IEnumerable<UnitState> units = UnitsForScope(rule.Scope, preview.UnitIds, source, primary, cells, battle.Combat);
                ApplyRule(battle, source, primary, center, spell, rule, cells, units, target.Direction, steps);
            }
            if (spell.Rules.Any(rule => rule.Timing == FireRuleTiming.OnTrigger))
            {
                battle.Arm(new FirePendingEffect(spell, source.Id, primary?.Id, center, target.Direction));
                Add(steps, spell, FireRuleKind.ArmTrigger, source.Id, center, 1, 1, spell.TriggerWindow.ToString());
            }
            battle.SetCooldown(source.Id, spell.Id, spell.Cooldown);
            if (spell.InitiativeDelay > 0) source.ChangeActionValue(-spell.InitiativeDelay);
            battle.ResolveMarkedDestructions();
            battle.Combat.EvaluateOutcome();
            return new FireSpellExecution(preview, steps, source.Id, sourceOrigin);
        }

        public static IReadOnlyList<FireSpellExecution> TriggerWeaponAttack(FireBattleState battle, string sourceUnitId,
            string targetUnitId, GridPosition? followUpCell = null, CardinalDirection followUpDirection = CardinalDirection.East)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            UnitState source = battle.Combat.GetUnit(sourceUnitId) ?? throw new InvalidOperationException("Source unit does not exist.");
            UnitState target = battle.Combat.GetUnit(targetUnitId) ?? throw new InvalidOperationException("Weapon target does not exist.");
            if (!source.IsAlive || source.IsHero == target.IsHero) throw new InvalidOperationException("Weapon target is not legal.");
            if (source.Position.ManhattanDistance(target.Position) < source.MainHand.MinimumRange) throw new InvalidOperationException("Weapon target is inside the minimum range.");
            if (source.Position.ManhattanDistance(target.Position) > source.MainHand.Range) throw new InvalidOperationException("Weapon target is out of range.");
            if (source.MainHand.Range > 1 && !battle.Combat.HasLineOfSight(source.Position, target.Position)) throw new InvalidOperationException("Weapon line of sight is blocked.");

            List<FireSpellExecution> executions = new List<FireSpellExecution>();
            FirePendingEffect[] matches = battle.PendingEffects.Where(effect => effect.SourceUnitId == sourceUnitId &&
                (effect.Spell.TriggerWindow == FireTriggerWindow.NextLegalWeaponAttack ||
                 effect.Spell.TriggerWindow == FireTriggerWindow.AfterNextWeaponAttack)).ToArray();
            foreach (FirePendingEffect effect in matches)
            {
                if (!FireSpellCatalog.IsWeaponCompatible(effect.Spell, source.MainHand)) continue;
                bool requiresMarkedTarget = effect.Stage > 0 || effect.Spell.TargetKind == FireTargetKind.BurningEnemy;
                if (requiresMarkedTarget && !string.IsNullOrEmpty(effect.MarkedUnitId) && effect.MarkedUnitId != targetUnitId) continue;
                FireSpellRule[] gatedBenefits = effect.Spell.Rules.Where(rule => rule.Timing == FireRuleTiming.OnTrigger &&
                    rule.Condition != FireCondition.Always && rule.Kind != FireRuleKind.ConsumeBurning &&
                    rule.Kind != FireRuleKind.ConsumeFireground).ToArray();
                if (gatedBenefits.Length > 0 && !gatedBenefits.Any(rule =>
                    ConditionMet(battle, source, target, target.Position, rule.Condition))) continue;
                GridPosition sourceOrigin = source.Position;
                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
                if (effect.Stage > 0)
                {
                    int requested = CombatDebugTuning.OutgoingDamageFor(source, 8);
                    int applied = FireBattleState.ApplyRawFireDamage(target, requested, battle.Combat);
                    Add(steps, effect.Spell, FireRuleKind.Damage, target.Id, target.Position, requested, applied, "ally_followup");
                }
                else
                {
                    ApplyTriggeredRules(battle, effect, source, target, target.Position, new[] { target.Position }, new[] { target }, steps,
                        followUpCell, followUpDirection);
                }
                battle.Consume(effect);
                Add(steps, effect.Spell, FireRuleKind.ConsumeTrigger, source.Id, target.Position, 1, 1, "weapon_attack_committed");
                executions.Add(new FireSpellExecution(TriggerPreview(effect.Spell, target.Position, target.Id), steps, source.Id, sourceOrigin, true));
            }
            return executions;
        }

        public static IReadOnlyList<FireSpellExecution> TriggerWeaponAttackAt(FireBattleState battle, string sourceUnitId,
            GridPosition targetCell)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            UnitState source = battle.Combat.GetUnit(sourceUnitId) ?? throw new InvalidOperationException("Source unit does not exist.");
            if (source.Position.ManhattanDistance(targetCell) < source.MainHand.MinimumRange) throw new InvalidOperationException("Weapon target is inside the minimum range.");
            if (source.Position.ManhattanDistance(targetCell) > source.MainHand.Range) throw new InvalidOperationException("Weapon target is out of range.");
            TileState tile = battle.Combat.Map.GetTile(targetCell);
            if (tile == null || tile.IsDestroyed || (tile.Cover == CoverType.None && !tile.IsDevice)) throw new InvalidOperationException("Weapon object target is not legal.");
            List<FireSpellExecution> executions = new List<FireSpellExecution>();
            foreach (FirePendingEffect effect in battle.PendingEffects.Where(value => value.SourceUnitId == sourceUnitId &&
                value.Spell.TriggerWindow == FireTriggerWindow.NextLegalWeaponAttack).ToArray())
            {
                if (!FireSpellCatalog.IsWeaponCompatible(effect.Spell, source.MainHand)) continue;
                GridPosition sourceOrigin = source.Position;
                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
                ApplyTriggeredRules(battle, effect, source, null, targetCell, new[] { targetCell }, Array.Empty<UnitState>(), steps, null, DirectionToward(source.Position, targetCell));
                battle.Consume(effect); Add(steps, effect.Spell, FireRuleKind.ConsumeTrigger, source.Id, targetCell, 1, 1, "weapon_object_attack_committed");
                executions.Add(new FireSpellExecution(TriggerPreview(effect.Spell, targetCell, null), steps, source.Id, sourceOrigin, true));
            }
            return executions;
        }

        public static IReadOnlyList<FireSpellExecution> TriggerIncomingAdjacentAttack(FireBattleState battle,
            string attackerUnitId, string targetUnitId)
        {
            UnitState attacker = battle.Combat.GetUnit(attackerUnitId) ?? throw new InvalidOperationException("Attacker does not exist.");
            UnitState target = battle.Combat.GetUnit(targetUnitId) ?? throw new InvalidOperationException("Target does not exist.");
            if (attacker.Position.ManhattanDistance(target.Position) != 1) return Array.Empty<FireSpellExecution>();
            List<FireSpellExecution> result = new List<FireSpellExecution>();
            foreach (FirePendingEffect effect in battle.PendingEffects.Where(value => value.SourceUnitId == targetUnitId &&
                value.Spell.TriggerWindow == FireTriggerWindow.FirstAdjacentAttack).ToArray())
            {
                GridPosition sourceOrigin = target.Position;
                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
                ApplyTriggeredRules(battle, effect, target, attacker, attacker.Position, new[] { attacker.Position }, new[] { attacker }, steps, null, DirectionToward(target.Position, attacker.Position));
                battle.Consume(effect); Add(steps, effect.Spell, FireRuleKind.ConsumeTrigger, target.Id, attacker.Position, 1, 1, "adjacent_counter");
                result.Add(new FireSpellExecution(TriggerPreview(effect.Spell, attacker.Position, attacker.Id), steps, target.Id, sourceOrigin, true));
            }
            return result;
        }

        public static IReadOnlyList<FireSpellExecution> TriggerMarkedTargetMove(FireBattleState battle,
            string movingUnitId, GridPosition previousPosition)
        {
            UnitState moving = battle.Combat.GetUnit(movingUnitId) ?? throw new InvalidOperationException("Moving unit does not exist.");
            List<FireSpellExecution> result = new List<FireSpellExecution>();
            foreach (FirePendingEffect effect in battle.PendingEffects.Where(value => value.Spell.TriggerWindow == FireTriggerWindow.FirstMarkedTargetMove && value.MarkedUnitId == movingUnitId).ToArray())
            {
                UnitState source = battle.Combat.GetUnit(effect.SourceUnitId); if (source == null || !source.IsAlive) { battle.Consume(effect); continue; }
                FirePursuitPreview pursuit = PreviewMarkedTargetMove(battle, effect, previousPosition);
                GridPosition sourceOrigin = source.Position;
                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
                if (pursuit.WillMove) source.MoveTo(pursuit.Destination);
                Add(steps, effect.Spell, FireRuleKind.MoveSource, source.Id, pursuit.Destination, 1,
                    pursuit.WillMove ? 1 : 0, pursuit.DetailCode);
                battle.Combat.AddLog(effect.Spell.DisplayName + "：" + pursuit.Reason);
                battle.Consume(effect); Add(steps, effect.Spell, FireRuleKind.ConsumeTrigger, source.Id, pursuit.Destination, 1, 1, "marked_target_active_move");
                result.Add(new FireSpellExecution(TriggerPreview(effect.Spell, pursuit.Destination, moving.Id), steps, source.Id, sourceOrigin, true));
            }
            return result;
        }

        public static FirePursuitPreview PreviewMarkedTargetMove(FireBattleState battle,
            FirePendingEffect effect, GridPosition previousPosition)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            UnitState source = battle.Combat.GetUnit(effect.SourceUnitId);
            UnitState moving = battle.Combat.GetUnit(effect.MarkedUnitId);
            if (source == null || !source.IsAlive)
                return new FirePursuitPreview(previousPosition, false, "施术者已无法行动，追步取消。", "pursuit_source_unavailable");
            if (moving == null || !moving.IsAlive)
                return new FirePursuitPreview(previousPosition, false, "标记目标已失效，追步取消。", "pursuit_target_unavailable");
            if (source.Position.ManhattanDistance(moving.Position) <= 1)
                return new FirePursuitPreview(previousPosition, false, "目标移动后仍与施术者相邻，施术者保持原位。", "pursuit_already_adjacent");
            if (source.HasStatus(StatusType.Bound))
                return new FirePursuitPreview(previousPosition, false, "施术者受到束缚，无法进入目标离开的格。", "pursuit_bound");
            if (!battle.Combat.Map.IsInside(previousPosition))
                return new FirePursuitPreview(previousPosition, false, "目标离开的格位于战场外，追步取消。", "pursuit_outside");
            if (battle.Combat.Map.IsBlocked(previousPosition))
                return new FirePursuitPreview(previousPosition, false, "目标离开的格已被阻挡，追步取消。", "pursuit_blocked");
            if (battle.Combat.IsOccupied(previousPosition))
                return new FirePursuitPreview(previousPosition, false, "目标离开的格已被占用，追步取消。", "pursuit_occupied");
            return new FirePursuitPreview(previousPosition, true,
                "施术者进入目标离开的格 " + previousPosition + "。", "pursuit_to_vacated_cell");
        }

        public static IReadOnlyList<FireSpellExecution> TriggerEnemyEntry(FireBattleState battle, string enteringUnitId)
        {
            UnitState entering = battle.Combat.GetUnit(enteringUnitId) ?? throw new InvalidOperationException("Entering unit does not exist.");
            List<FireSpellExecution> result = new List<FireSpellExecution>();
            foreach (FirePendingEffect effect in battle.PendingEffects.Where(value => value.Spell.TriggerWindow == FireTriggerWindow.FirstEnemyEntry && value.MarkedCell == entering.Position).ToArray())
            {
                UnitState source = battle.Combat.GetUnit(effect.SourceUnitId);
                if (source == null || source.IsHero == entering.IsHero || !battle.Combat.HasLineOfSight(source.Position, entering.Position)) continue;
                GridPosition sourceOrigin = source.Position;
                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();
                ApplyTriggeredRules(battle, effect, source, entering, entering.Position, new[] { entering.Position }, new[] { entering }, steps, null, DirectionToward(source.Position, entering.Position));
                battle.Consume(effect); Add(steps, effect.Spell, FireRuleKind.ConsumeTrigger, source.Id, entering.Position, 1, 1, "enemy_entered_marked_cell");
                result.Add(new FireSpellExecution(TriggerPreview(effect.Spell, entering.Position, entering.Id), steps, source.Id, sourceOrigin, true));
            }
            return result;
        }

        public static int ReduceIncomingDamage(FireBattleState battle, string targetUnitId, string attackerUnitId,
            int requestedDamage, bool explosion = false)
        {
            UnitState target = battle.Combat.GetUnit(targetUnitId), attacker = battle.Combat.GetUnit(attackerUnitId);
            if (target == null || attacker == null || requestedDamage <= 0 || explosion || target.Position.ManhattanDistance(attacker.Position) <= 1) return requestedDamage;
            int reduction = 0;
            foreach (FirePendingEffect effect in battle.PendingEffects.Where(value => value.SourceUnitId == targetUnitId && value.Spell.TriggerWindow == FireTriggerWindow.UntilNextAction).ToArray())
            {
                int preHitShield = effect.Spell.Rules.Where(rule => rule.Kind == FireRuleKind.GrantShieldBeforeRanged).Select(rule => rule.Amount).DefaultIfEmpty(0).Max();
                if (preHitShield > 0 && battle.Combat.Ruleset == CombatRuleset.Roguelite)
                {
                    battle.Combat.TryGrantRogueliteShield(target.Id, effect.Spell.Id, preHitShield);
                    battle.Consume(effect);
                    continue;
                }
                reduction = Math.Max(reduction, effect.Spell.Rules.Where(rule => rule.Kind == FireRuleKind.ReduceIncomingDamage).Select(rule => rule.Amount).DefaultIfEmpty(0).Max());
            }
            return Math.Max(0, requestedDamage - reduction);
        }

        public static int PendingPreHitShield(FireBattleState battle, string targetUnitId,
            string attackerUnitId, bool explosion = false)
        {
            if (battle == null || explosion) return 0;
            UnitState target = battle.Combat.GetUnit(targetUnitId);
            UnitState attacker = battle.Combat.GetUnit(attackerUnitId);
            if (target == null || attacker == null || target.Position.ManhattanDistance(attacker.Position) <= 1) return 0;
            return battle.PendingEffects
                .Where(value => value.SourceUnitId == targetUnitId && value.Spell.TriggerWindow == FireTriggerWindow.UntilNextAction)
                .SelectMany(value => value.Spell.Rules)
                .Where(rule => rule.Kind == FireRuleKind.GrantShieldBeforeRanged)
                .Select(rule => rule.Amount)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static void ApplyTriggeredRules(FireBattleState battle, FirePendingEffect effect, UnitState source,
            UnitState primary, GridPosition center, IReadOnlyList<GridPosition> cells, IReadOnlyList<UnitState> units,
            List<FireSpellResultStep> steps, GridPosition? followUpCell, CardinalDirection followUpDirection)
        {
            foreach (FireSpellRule rule in effect.Spell.Rules.Where(rule => rule.Timing == FireRuleTiming.OnTrigger))
            {
                if (rule.Kind == FireRuleKind.MoveAfterAttack)
                {
                    GridPosition destination = followUpCell ?? (primary == null ? source.Position :
                        source.Position + Cardinal(primary.Position, source.Position, followUpDirection) * Math.Max(1, rule.Amount));
                    bool hasDestination = followUpCell.HasValue || primary != null;
                    bool legal = hasDestination &&
                        source.Position.ManhattanDistance(destination) <= rule.Amount && battle.Combat.Map.IsInside(destination) &&
                        !battle.Combat.Map.IsBlocked(destination) && !battle.Combat.IsOccupied(destination, source.Id);
                    GridPosition before = source.Position;
                    if (legal)
                    {
                        source.MoveTo(destination);
                        battle.Combat.ResolveDisplacementLanding(source, before);
                    }
                    Add(steps, effect.Spell, rule.Kind, source.Id, destination, rule.Amount, legal ? before.ManhattanDistance(destination) : 0,
                        legal ? followUpCell.HasValue ? "post_attack_selected_move" : "post_attack_retreat" : "post_attack_retreat_blocked");
                    continue;
                }
                IEnumerable<GridPosition> scopedCells = CellsForScope(rule.Scope, cells, source.Position, center, effect.Direction, battle.Combat);
                IEnumerable<UnitState> scopedUnits = UnitsForScope(rule.Scope, units.Select(unit => unit.Id).ToArray(), source, primary, scopedCells, battle.Combat);
                ApplyRule(battle, source, primary, center, effect.Spell, rule, scopedCells, scopedUnits, effect.Direction, steps);
            }
        }

        private static FireSpellPreview TriggerPreview(FireSpellDefinition spell, GridPosition cell, string targetId) =>
            new FireSpellPreview(spell, Array.Empty<string>(), new[] { cell }, string.IsNullOrEmpty(targetId) ? Array.Empty<string>() : new[] { targetId }, Array.Empty<GridPosition>(), false, false);

        private static void ValidateTarget(FireBattleState battle, UnitState source, UnitState target, GridPosition cell, FireSpellDefinition spell, List<string> failures)
        {
            bool occupied = battle.Combat.IsOccupied(cell);
            switch (spell.TargetKind)
            {
                case FireTargetKind.Self: if (target != null && target.Id != source.Id) failures.Add("只能选择自身"); break;
                case FireTargetKind.Enemy: if (target == null || target.IsHero == source.IsHero) failures.Add("需要敌方单位"); break;
                case FireTargetKind.AllyOrSelf: if (target == null || target.IsHero != source.IsHero) failures.Add("需要自身或友方单位"); break;
                case FireTargetKind.Unit: if (target == null) failures.Add("需要单位目标"); break;
                case FireTargetKind.EmptyCell: if (!battle.Combat.Map.IsInside(cell) || occupied || battle.Combat.Map.IsBlocked(cell)) failures.Add("需要空地"); break;
                case FireTargetKind.BurningUnit: if (target == null || !target.HasStatus(StatusType.Burning)) failures.Add("目标未燃烧"); break;
                case FireTargetKind.BurningEnemy:
                    if (target == null || target.IsHero == source.IsHero || !target.HasStatus(StatusType.Burning)) failures.Add("需要燃烧敌方");
                    break;
                case FireTargetKind.AdjacentBurningEnemy:
                    if (target == null || target.IsHero == source.IsHero || !target.HasStatus(StatusType.Burning) || source.Position.ManhattanDistance(target.Position) != 1) failures.Add("需要相邻燃烧敌方");
                    break;
                case FireTargetKind.BurningOrArmorBrokenEnemy:
                    if (target == null || target.IsHero == source.IsHero || (!target.HasStatus(StatusType.Burning) && !target.HasStatus(battle.Combat.Ruleset == CombatRuleset.Roguelite ? StatusType.BreakStance : StatusType.ArmorBreak))) failures.Add("需要燃烧或破势敌方");
                    break;
                case FireTargetKind.BurningCell: if (!battle.HasFireground(cell)) failures.Add("目标格不是燃烧地格"); break;
                case FireTargetKind.Destructible:
                {
                    TileState tile = battle.Combat.Map.GetTile(cell);
                    bool legal = spell.Rules.Where(IsObjectRule).Any(rule => MatchesObject(tile, rule.DestructibleMask));
                    if (!legal) failures.Add("需要符合术式合同的掩体或设备");
                    break;
                }
                case FireTargetKind.Hittable:
                {
                    TileState tile = battle.Combat.Map.GetTile(cell);
                    bool legalUnit = target != null && target.IsHero != source.IsHero;
                    bool legalObject = spell.Rules.Where(IsObjectRule).Any(rule => MatchesObject(tile, rule.DestructibleMask));
                    if (!legalUnit && !legalObject) failures.Add("需要敌方单位或可破坏物件");
                    break;
                }
                case FireTargetKind.AdjacentEnemy: if (target == null || target.IsHero == source.IsHero || source.Position.ManhattanDistance(target.Position) != 1) failures.Add("需要相邻敌方"); break;
            }
            if (RequiresOnCastCondition(spell, FireCondition.TargetOnFireground) && !battle.HasFireground(cell)) failures.Add("目标不在燃烧地格");
            if (RequiresOnCastCondition(spell, FireCondition.TargetBurningAndOnFireground) && (target == null || !target.HasStatus(StatusType.Burning) || !battle.HasFireground(cell))) failures.Add("需要燃烧单位同时站在燃烧地格");
            if (RequiresOnCastCondition(spell, FireCondition.SourceBurning) && !source.HasStatus(StatusType.Burning)) failures.Add("自身未燃烧");
            if (RequiresOnCastCondition(spell, FireCondition.SourceBound) && !source.HasStatus(StatusType.Bound)) failures.Add("自身未被束缚");
            if (RequiresOnCastCondition(spell, FireCondition.SourceSlowed) && !source.HasStatus(StatusType.Slow)) failures.Add("自身未被迟缓");
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.MoveSource) && source.HasStatus(StatusType.Bound)) failures.Add("束缚时不能移动");
            int selfLoss = spell.Rules.Where(rule => rule.Kind == FireRuleKind.LoseHealth && rule.Scope == FireRuleScope.Source)
                .Select(rule => rule.Amount).DefaultIfEmpty(0).Max();
            if (selfLoss > 0 && source.Health <= selfLoss) failures.Add("生命不足");
            if (spell.CombatAffinity == FireCombatAffinity.MeleeOnly && spell.TargetKind == FireTargetKind.EmptyCell &&
                !battle.Combat.Units.Values.Any(unit => unit.IsAlive && unit.IsHero != source.IsHero && unit.Position.ManhattanDistance(cell) == 1))
                failures.Add("终点必须与敌方相邻");
            if (spell.TargetKind == FireTargetKind.BurningCell && spell.Shape == FireSelectionShape.Path &&
                SelectCells(battle.Combat, source.Position, cell, CardinalDirection.East, spell).Any(pathCell => !battle.HasFireground(pathCell)))
                failures.Add("路径必须连续经过燃烧地格");
            if (spell.TargetKind == FireTargetKind.EmptyCell && spell.Shape == FireSelectionShape.Path && spell.Rules.Any(rule => rule.Kind == FireRuleKind.MoveSource))
            {
                bool axial = source.Position.X == cell.X || source.Position.Y == cell.Y;
                List<GridPosition> path = SelectCells(battle.Combat, source.Position, cell, CardinalDirection.East, spell);
                if (!axial || path.Count != source.Position.ManhattanDistance(cell) || path.Take(Math.Max(0, path.Count - 1)).Any(position => battle.Combat.IsOccupied(position)))
                    failures.Add("突进必须沿无阻挡的四向直线");
            }
            if (spell.Id == "F-P-M19" && target != null)
            {
                bool axial = source.Position.X == target.Position.X || source.Position.Y == target.Position.Y;
                List<GridPosition> path = SelectCells(battle.Combat, source.Position, target.Position, CardinalDirection.East, spell);
                bool reachesTarget = path.Count == source.Position.ManhattanDistance(target.Position) &&
                    path.Count > 0 && path[path.Count - 1] == target.Position;
                bool occupiedBeforeTarget = path.Take(Math.Max(0, path.Count - 1))
                    .Any(position => battle.Combat.IsOccupied(position, source.Id));
                if (!axial) failures.Add("目标必须位于同一横线或竖线");
                else if (!reachesTarget || occupiedBeforeTarget) failures.Add("冲锋路径受阻");
            }
        }

        private static void ValidateConsumption(FireBattleState battle, FireSpellDefinition spell, UnitState primary,
            IReadOnlyCollection<GridPosition> cells, IReadOnlyCollection<string> unitIds, List<string> failures)
        {
            UnitState[] units = unitIds.Select(battle.Combat.GetUnit).Where(unit => unit != null).ToArray();
            bool hasBurning = units.Any(unit => unit.HasStatus(StatusType.Burning)) || (primary != null && primary.HasStatus(StatusType.Burning));
            bool hasGround = cells.Any(battle.HasFireground) || (primary != null && battle.HasFireground(primary.Position));
            foreach (FireSourceConsumption consumption in spell.Rules.Where(rule => rule.Timing == FireRuleTiming.OnCast)
                .Select(rule => rule.Consumption).Where(value => value != FireSourceConsumption.None).Distinct())
            {
                bool legal = consumption == FireSourceConsumption.BurningOnly ? hasBurning :
                    consumption == FireSourceConsumption.GroundOnly ? hasGround :
                    consumption == FireSourceConsumption.BurningAndGround ? hasBurning && hasGround : hasBurning || hasGround;
                if (!legal) { failures.Add("这里没有可供术式引燃的火源"); return; }
            }
        }

        private static List<GridPosition> SelectCells(CombatState state, GridPosition source, GridPosition center, CardinalDirection aimDirection, FireSpellDefinition spell)
        {
            if (spell.Shape == FireSelectionShape.Single) return new List<GridPosition> { center };
            GridPosition direction = Cardinal(source, center, aimDirection);
            if (spell.Shape == FireSelectionShape.Path)
            {
                List<GridPosition> path = new List<GridPosition>(); GridPosition cursor = source;
                while (cursor != center && path.Count < spell.ShapeLength)
                {
                    int dx = center.X - cursor.X, dy = center.Y - cursor.Y;
                    GridPosition step = dx != 0 ? new GridPosition(Math.Sign(dx), 0) : new GridPosition(0, Math.Sign(dy));
                    cursor += step; if (!state.Map.IsInside(cursor) || state.Map.IsBlocked(cursor)) break; path.Add(cursor);
                }
                return path;
            }
            if (spell.Shape == FireSelectionShape.Line || spell.Shape == FireSelectionShape.ContinuousLine)
            {
                int count = spell.ShapeLength;
                GridPosition start = spell.Shape == FireSelectionShape.ContinuousLine ? center : source + direction;
                List<GridPosition> line = new List<GridPosition>();
                for (int i = 0; i < count; i++) { GridPosition cell = start + direction * i; if (!state.Map.IsInside(cell)) break; line.Add(cell); if (spell.HeavyCoverTruncates && state.Map.GetTile(cell).BlocksLineOfSight) break; }
                return line;
            }
            if (spell.Shape == FireSelectionShape.Cone)
            {
                GridPosition side = new GridPosition(-direction.Y, direction.X); List<GridPosition> cone = new List<GridPosition>();
                for (int distance = 1; distance <= spell.ShapeLength; distance++) for (int offset = -(distance - 1); offset <= distance - 1; offset++) cone.Add(source + direction * distance + side * offset);
                IEnumerable<GridPosition> visibleCone = cone.Where(state.Map.IsInside);
                if (spell.HeavyCoverTruncates)
                    visibleCone = visibleCone.Where(cell => state.HasLineOfSight(source, cell));
                return visibleCone.ToList();
            }
            if (spell.Shape == FireSelectionShape.Square3) return Around(center, true, true);
            if (spell.Shape == FireSelectionShape.OrthogonalRing || spell.Shape == FireSelectionShape.AroundUnit) return Around(center, false, false);
            return Around(center, true, false);
        }

        private static List<GridPosition> Around(GridPosition center, bool includeCenter, bool diagonals)
        {
            List<GridPosition> result = new List<GridPosition>();
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            { if (x == 0 && y == 0) { if (includeCenter) result.Add(center); } else if (diagonals || x == 0 || y == 0) result.Add(center + new GridPosition(x, y)); }
            return result;
        }

        private static GridPosition Cardinal(GridPosition source, GridPosition target, CardinalDirection fallbackDirection)
        {
            int dx = target.X - source.X, dy = target.Y - source.Y;
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0) return new GridPosition(Math.Sign(dx), 0);
            if (dy != 0) return new GridPosition(0, Math.Sign(dy));
            if (fallbackDirection == CardinalDirection.North) return new GridPosition(0, 1); if (fallbackDirection == CardinalDirection.South) return new GridPosition(0, -1);
            if (fallbackDirection == CardinalDirection.West) return new GridPosition(-1, 0); return new GridPosition(1, 0);
        }

        private static CardinalDirection DirectionToward(GridPosition source, GridPosition target)
        {
            int dx = target.X - source.X, dy = target.Y - source.Y;
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0) return dx > 0 ? CardinalDirection.East : CardinalDirection.West;
            if (dy != 0) return dy > 0 ? CardinalDirection.North : CardinalDirection.South;
            return CardinalDirection.East;
        }

        private static IEnumerable<GridPosition> CellsForScope(FireRuleScope scope, IReadOnlyList<GridPosition> selected, GridPosition source, GridPosition center, CardinalDirection direction, CombatState combat)
        {
            if (scope == FireRuleScope.SourceCell) return new[] { source };
            if (scope == FireRuleScope.Destination || scope == FireRuleScope.Primary) return new[] { center };
            if (scope == FireRuleScope.OrthogonalNeighbors) return Around(center, false, false).Where(combat.Map.IsInside);
            if (scope == FireRuleScope.PathAdjacentEnemies) return selected.SelectMany(cell => Around(cell, false, false)).Distinct().Where(combat.Map.IsInside);
            if (scope == FireRuleScope.CoveredCells) return selected.Where(cell => combat.Map.GetTile(cell).Cover != CoverType.Heavy);
            return selected;
        }

        private static IEnumerable<UnitState> UnitsForScope(FireRuleScope scope, IReadOnlyList<string> selectedIds, UnitState source, UnitState primary, IEnumerable<GridPosition> cells, CombatState combat)
        {
            if (scope == FireRuleScope.Source) return new[] { source };
            if (scope == FireRuleScope.Primary) return primary == null ? Array.Empty<UnitState>() : new[] { primary };
            HashSet<GridPosition> positions = cells.ToHashSet();
            IEnumerable<UnitState> units = combat.Units.Values.Where(unit => unit.IsAlive && positions.Contains(unit.Position));
            if (scope == FireRuleScope.EnemySelection) units = units.Where(unit => unit.IsHero != source.IsHero);
            if (scope == FireRuleScope.PathAdjacentEnemies) units = units.Where(unit => unit.IsHero != source.IsHero);
            if (scope == FireRuleScope.AllySelection) units = units.Where(unit => unit.IsHero == source.IsHero);
            return units.OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        }

        private static bool ConditionMet(FireBattleState battle, UnitState source, UnitState target, GridPosition cell, FireCondition condition)
        {
            switch (condition)
            {
                case FireCondition.TargetBurning: return target != null && target.HasStatus(StatusType.Burning);
                case FireCondition.TargetOnFireground: return battle.HasFireground(cell);
                case FireCondition.TargetBurningAndOnFireground: return target != null && target.HasStatus(StatusType.Burning) && battle.HasFireground(cell);
                case FireCondition.TargetArmorBroken: return target != null && target.HasStatus(StatusType.ArmorBreak);
                case FireCondition.TargetBurningOrArmorBroken: return target != null && (target.HasStatus(StatusType.Burning) || target.HasStatus(StatusType.ArmorBreak));
                case FireCondition.TargetBreakStance: return target != null && target.HasStatus(StatusType.BreakStance);
                case FireCondition.TargetBurningOrBreakStance: return target != null && (target.HasStatus(StatusType.Burning) || target.HasStatus(StatusType.BreakStance));
                case FireCondition.SourceBreakStance: return source.HasStatus(StatusType.BreakStance);
                case FireCondition.SourceBurning: return source.HasStatus(StatusType.Burning);
                case FireCondition.SourceNotBurning: return !source.HasStatus(StatusType.Burning);
                case FireCondition.SourceBound: return source.HasStatus(StatusType.Bound);
                case FireCondition.SourceSlowed: return source.HasStatus(StatusType.Slow);
                case FireCondition.SourceNotArmorBroken: return !source.HasStatus(StatusType.ArmorBreak);
                case FireCondition.TargetAtWeaponMaxRange:
                    return target != null && source.MainHand != null &&
                        source.Position.ManhattanDistance(target.Position) == source.MainHand.Range;
                case FireCondition.LightCoverDestroyed: { TileState tile = battle.Combat.Map.GetTile(cell); return tile.Cover == CoverType.Light && tile.IsDestroyed; }
                case FireCondition.DurabilityDepleted: return battle.Combat.Map.GetTile(cell).IsDestroyed;
                default: return true;
            }
        }

        private static bool IsPrimaryTargetCondition(FireCondition condition)
        {
            return condition == FireCondition.TargetBurning || condition == FireCondition.TargetOnFireground ||
                condition == FireCondition.TargetBurningAndOnFireground || condition == FireCondition.TargetArmorBroken ||
                condition == FireCondition.TargetBurningOrArmorBroken || condition == FireCondition.TargetBreakStance ||
                condition == FireCondition.TargetBurningOrBreakStance || condition == FireCondition.TargetAtWeaponMaxRange;
        }

        private static bool RequiresOnCastCondition(FireSpellDefinition spell, FireCondition condition)
        {
            FireSpellRule[] onCast = spell.Rules.Where(rule => rule.Timing == FireRuleTiming.OnCast).ToArray();
            return onCast.Length > 0 && onCast.All(rule => rule.Condition == condition);
        }

        // 总案 3.5.6.2／3.6.1：术式伤害在结算单位的同时，让作用几何内的可破坏物扣减同值耐久；
        // 带破障标记的术式使物块耐久伤害翻倍。条件伤害（例："燃烧单位"）不作用于物块，因为物块无法满足单位状态条件。
        private static void ApplyDamageToObjects(FireBattleState battle, FireSpellDefinition spell, FireSpellRule rule,
            IEnumerable<GridPosition> cells, List<FireSpellResultStep> steps)
        {
            if (rule.Kind != FireRuleKind.Damage && rule.Kind != FireRuleKind.WeaponDamage) return;
            if (rule.Condition != FireCondition.Always || rule.Amount <= 0) return;
            int amount = spell.Rules.Any(candidate => candidate.Kind == FireRuleKind.BreakBarrier) ? rule.Amount * 2 : rule.Amount;
            foreach (GridPosition cell in cells)
            {
                TileState tile = battle.Combat.Map.GetTile(cell);
                if (!MatchesObject(tile, rule.DestructibleMask)) continue;
                int before = tile.Durability;
                if (before <= 0) continue;
                tile.Durability = Math.Max(0, before - amount);
                battle.Combat.ResolveAetherCrystalDamage(cell, before);
                Add(steps, spell, FireRuleKind.DamageDurability, null, cell, amount, before - tile.Durability, "durability");
            }
        }

        private static void ApplyRule(FireBattleState battle, UnitState source, UnitState primary, GridPosition center, FireSpellDefinition spell,
            FireSpellRule rule, IEnumerable<GridPosition> cells, IEnumerable<UnitState> units, CardinalDirection aimDirection, List<FireSpellResultStep> steps)
        {
            GridPosition[] cellArray = cells.Distinct().Where(battle.Combat.Map.IsInside).ToArray(); UnitState[] unitArray = units.ToArray();
            if (rule.Kind == FireRuleKind.MoveSource)
            {
                GridPosition before = source.Position;
                GridPosition destination = center;
                if (spell.Id == "F-P-M19" && primary != null)
                {
                    List<GridPosition> chargePath = SelectCells(battle.Combat, before, primary.Position, aimDirection, spell);
                    destination = chargePath.Count >= 2 ? chargePath[chargePath.Count - 2] : before;
                }
                else if (primary != null && battle.Combat.IsOccupied(center, source.Id))
                {
                    GridPosition[] candidates = Around(center, false, false)
                        .Where(battle.Combat.Map.IsInside)
                        .Where(cell => !battle.Combat.Map.IsBlocked(cell) && !battle.Combat.IsOccupied(cell, source.Id))
                        .Where(cell => source.Position.ManhattanDistance(cell) <= Math.Max(1, rule.Amount))
                        .OrderBy(cell => source.Position.ManhattanDistance(cell)).ThenBy(cell => cell.X).ThenBy(cell => cell.Y)
                        .ToArray();
                    if (candidates.Length > 0) destination = candidates[0];
                }
                if (battle.Combat.Map.IsInside(destination) && !battle.Combat.IsOccupied(destination, source.Id) && !battle.Combat.Map.IsBlocked(destination)) source.MoveTo(destination);
                if (!(spell.DeliveryMode == FireDeliveryMode.FiregroundManipulation && spell.TargetKind == FireTargetKind.BurningCell))
                    battle.Combat.ResolveDisplacementLanding(source, before);
                int moved = before.ManhattanDistance(source.Position);
                Add(steps, spell, rule.Kind, source.Id, destination, rule.Amount, moved,
                    spell.Id == "F-P-M19" ? "charge_to_front_cell" : "move");
                return;
            }
            if (rule.Kind == FireRuleKind.SwapUnits && primary != null)
            {
                GridPosition sourceBefore = source.Position, targetBefore = primary.Position;
                source.MoveTo(targetBefore); primary.MoveTo(sourceBefore);
                battle.Combat.ResolveDisplacementLanding(source, sourceBefore);
                battle.Combat.ResolveDisplacementLanding(primary, targetBefore);
                Add(steps, spell, rule.Kind, primary.Id, targetBefore, 1, 1, "swap"); return;
            }
            if (rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.WeaponDamage)
            {
                foreach (UnitState unit in unitArray)
                {
                    if (!rule.AffectAllies && unit.IsHero == source.IsHero && unit.Id != source.Id) continue;
                    if (!ConditionMet(battle, source, unit, unit.Position, rule.Condition)) continue;
                    bool broken = battle.Combat.Ruleset == CombatRuleset.Roguelite ? unit.HasStatus(StatusType.BreakStance) : unit.HasStatus(StatusType.ArmorBreak);
                    bool both = unit.HasStatus(StatusType.Burning) && broken;
                    bool alternate = rule.AlternateAmount > 0 && ((rule.Condition == FireCondition.TargetBurningOrArmorBroken || rule.Condition == FireCondition.TargetBurningOrBreakStance) ? both :
                        rule.Condition == FireCondition.Always ? broken : unit.HasStatus(StatusType.Burning));
                    int requested = alternate ? rule.AlternateAmount : rule.Amount;
                    int applied = rule.Kind == FireRuleKind.WeaponDamage
                        ? FireBattleState.ApplyRawWeaponDamage(source, unit, requested, battle.Combat)
                        : FireBattleState.ApplyRawFireDamage(unit, CombatDebugTuning.OutgoingDamageFor(source, requested), battle.Combat);
                    Add(steps, spell, rule.Kind, unit.Id, unit.Position, CombatDebugTuning.OutgoingDamageFor(source, requested), applied, rule.Kind == FireRuleKind.WeaponDamage ? "weapon_damage" : "fire_damage");
                }
                ApplyDamageToObjects(battle, spell, rule, cellArray, steps);
            }
            else if (rule.Kind == FireRuleKind.ApplyBurning || rule.Kind == FireRuleKind.ExtendBurning || rule.Kind == FireRuleKind.ApplyArmorBreak || rule.Kind == FireRuleKind.ApplyBreakStance || rule.Kind == FireRuleKind.ApplyFiregroundBoost || rule.Kind == FireRuleKind.ApplyFiregroundVulnerability)
            {
                foreach (UnitState unit in unitArray) if (ConditionMet(battle, source, unit, unit.Position, rule.Condition))
                {
                    if (rule.Kind == FireRuleKind.ApplyFiregroundVulnerability && unit.IsHero == source.IsHero) continue;
                    if (!rule.AffectAllies && unit.IsHero == source.IsHero && unit.Id != source.Id) continue;
                    if (rule.Kind == FireRuleKind.ApplyBreakStance) battle.Combat.ApplyRogueliteBreakStance(unit.Id);
                    else unit.ApplyStatus(rule.Kind == FireRuleKind.ApplyArmorBreak ? StatusType.ArmorBreak : rule.Kind == FireRuleKind.ApplyFiregroundBoost ? StatusType.FiregroundBoost : rule.Kind == FireRuleKind.ApplyFiregroundVulnerability ? StatusType.FiregroundVulnerable : StatusType.Burning, rule.Duration, rule.Amount);
                    StatusType status = rule.Kind == FireRuleKind.ApplyBreakStance ? StatusType.BreakStance : rule.Kind == FireRuleKind.ApplyArmorBreak ? StatusType.ArmorBreak : rule.Kind == FireRuleKind.ApplyFiregroundBoost ? StatusType.FiregroundBoost : rule.Kind == FireRuleKind.ApplyFiregroundVulnerability ? StatusType.FiregroundVulnerable : StatusType.Burning;
                    Add(steps, spell, rule.Kind, unit.Id, unit.Position, rule.Amount, rule.Amount, status.ToString());
                }
            }
            else if (rule.Kind == FireRuleKind.SetBurningDuration)
            {
                foreach (UnitState unit in unitArray) if (unit.HasStatus(StatusType.Burning)) { unit.SetStatusDuration(StatusType.Burning, rule.Duration); Add(steps, spell, rule.Kind, unit.Id, unit.Position, rule.Duration, rule.Duration, "Burning"); }
            }
            else if (rule.Kind == FireRuleKind.CreateFireground || rule.Kind == FireRuleKind.ExtendFireground)
            {
                foreach (GridPosition cell in cellArray) if (!battle.Combat.Map.IsBlocked(cell) && ConditionMet(battle, source, primary, cell, rule.Condition)) { if (rule.Kind == FireRuleKind.CreateFireground) battle.CreateOrRefreshFireground(cell, rule.Amount, rule.Duration, spell.Id, source.Id); else battle.ExtendFireground(cell, rule.Amount, rule.Duration, spell.Id, source.Id); Add(steps, spell, rule.Kind, null, cell, rule.Amount, rule.Amount, "fireground"); }
            }
            else if (rule.Kind == FireRuleKind.ConsumeBurning || rule.Kind == FireRuleKind.ConsumeFireground)
            {
                foreach (UnitState unit in unitArray.DefaultIfEmpty(primary).Where(unit => unit != null)) if (ConditionMet(battle, source, unit, unit.Position, rule.Condition)) { bool burn = unit.HasStatus(StatusType.Burning); bool ground = battle.HasFireground(unit.Position); if (rule.Consumption == FireSourceConsumption.BurningOnly || rule.Consumption == FireSourceConsumption.BurningAndGround || (rule.Consumption == FireSourceConsumption.BurningFirstThenGround && burn)) unit.ClearStatus(StatusType.Burning); if (rule.Consumption == FireSourceConsumption.GroundOnly || rule.Consumption == FireSourceConsumption.BurningAndGround || (rule.Consumption == FireSourceConsumption.BurningFirstThenGround && !burn && ground)) battle.RemoveFireground(unit.Position); Add(steps, spell, rule.Kind, unit.Id, unit.Position, 1, 1, "consume"); }
                if (rule.Kind == FireRuleKind.ConsumeFireground && unitArray.Length == 0) foreach (GridPosition cell in cellArray) { battle.RemoveFireground(cell); Add(steps, spell, rule.Kind, null, cell, 1, 1, "consume_ground"); }
            }
            else if (rule.Kind == FireRuleKind.DamageDurability || rule.Kind == FireRuleKind.DestroyLightCover)
            {
                foreach (GridPosition cell in cellArray) { TileState tile = battle.Combat.Map.GetTile(cell); if (!MatchesObject(tile, rule.DestructibleMask)) continue; int before = tile.Durability; int amount = rule.Kind == FireRuleKind.DestroyLightCover ? before : (tile.Cover == CoverType.Heavy && rule.AlternateAmount > 0 ? rule.AlternateAmount : rule.Amount); tile.Durability = Math.Max(0, before - amount); battle.Combat.ResolveAetherCrystalDamage(cell, before); Add(steps, spell, rule.Kind, null, cell, amount, before - tile.Durability, "durability"); }
            }
            else if (rule.Kind == FireRuleKind.RestoreShield) foreach (UnitState unit in unitArray.DefaultIfEmpty(source).Where(unit => unit != null)) { UnitState conditionTarget = IsPrimaryTargetCondition(rule.Condition) && primary != null ? primary : unit; GridPosition conditionCell = conditionTarget.Position; if (!ConditionMet(battle, source, conditionTarget, conditionCell, rule.Condition)) continue; int before = unit.Shield; string shieldSource = spell.Id + (rule.Timing == FireRuleTiming.OnTrigger ? ":trigger" : ":cast"); if (battle.Combat.Ruleset == CombatRuleset.Roguelite) battle.Combat.TryGrantRogueliteShield(unit.Id, shieldSource, rule.Amount); else unit.GrantShield(rule.Amount); Add(steps, spell, rule.Kind, unit.Id, unit.Position, rule.Amount, unit.Shield - before, "shield"); }
            else if (rule.Kind == FireRuleKind.ApplyMeltBarrierMark)
            {
                battle.MarkMeltBarrier(source.Id, primary, center, rule.Duration);
                Add(steps, spell, rule.Kind, primary?.Id, center, rule.Duration, rule.Duration, "melt_barrier_mark");
            }
            else if (rule.Kind == FireRuleKind.ReserveNextTurnAction)
            {
                bool escaped = battle.IsThreatenedByEnemy(source, source.Position) && !battle.IsThreatenedByEnemy(source, center);
                if (escaped) battle.ReserveActionNextTurn(source.Id);
                Add(steps, spell, rule.Kind, source.Id, center, rule.Amount, escaped ? rule.Amount : 0, escaped ? "reserved" : "not_escaped");
            }
            else if (rule.Kind == FireRuleKind.ClearOneSelfStatus)
            {
                StatusType? selected = new[] { StatusType.BreakStance, StatusType.Bound, StatusType.Slow, StatusType.Burning, StatusType.FiregroundVulnerable }.Where(source.HasStatus).Select(value => (StatusType?)value).FirstOrDefault();
                if (selected.HasValue) { source.ClearStatus(selected.Value); Add(steps, spell, rule.Kind, source.Id, source.Position, 1, 1, selected.Value.ToString()); }
            }
            else if (rule.Kind == FireRuleKind.ClearStatus)
            {
                foreach (UnitState unit in unitArray.DefaultIfEmpty(source).Where(unit => unit != null))
                    if (ConditionMet(battle, source, unit, unit.Position, rule.Condition)) { unit.ClearStatus(rule.Status); Add(steps, spell, rule.Kind, unit.Id, unit.Position, 1, 1, rule.Status.ToString()); }
            }
            else if (rule.Kind == FireRuleKind.RestoreMana) { int before = source.Mana; source.RestoreMana(rule.Amount); Add(steps, spell, rule.Kind, source.Id, source.Position, rule.Amount, source.Mana - before, "mana"); }
            else if (rule.Kind == FireRuleKind.AddMovement) { int before = source.MovementRangeThisTurn; source.SetMovementRangeForTurn(before + rule.Amount); Add(steps, spell, rule.Kind, source.Id, source.Position, rule.Amount, source.MovementRangeThisTurn - before, "movement_bonus"); }
            else if (rule.Kind == FireRuleKind.LoseHealth) { int before = source.Health; source.TakeDamage(rule.Amount); Add(steps, spell, rule.Kind, source.Id, source.Position, rule.Amount, before - source.Health, "unshielded_self_loss"); }
            else if (rule.Kind == FireRuleKind.RepairWeapon) { if (!ConditionMet(battle, source, source, source.Position, rule.Condition)) return; battle.AddWeaponMaintenance(source.Id, rule.Amount); Add(steps, spell, rule.Kind, source.Id, source.Position, rule.Amount, rule.Amount, "combat_weapon_durability"); }
            else if (rule.Kind == FireRuleKind.Push && primary != null) { GridPosition before = primary.Position; GridPosition pushDirection = Cardinal(source.Position, primary.Position, aimDirection); GridPosition destination = primary.Position + pushDirection; bool prevented = battle.Combat.ArtifactBattle?.TryPreventForcedMove(primary.Id) == true; bool legal = !prevented && battle.Combat.Map.IsInside(destination) && !battle.Combat.Map.IsBlocked(destination) && !battle.Combat.IsOccupied(destination); if (legal) { primary.MoveTo(destination); battle.Combat.ResolveDisplacementLanding(primary, before); } Add(steps, spell, rule.Kind, primary.Id, destination, rule.Amount, legal ? rule.Amount : 0, legal ? "push" : prevented ? "anchored" : "blocked"); }
            else if (rule.Kind == FireRuleKind.PushAllUnits)
            {
                foreach (UnitState unit in unitArray.Where(value => value.IsAlive))
                {
                    GridPosition before = unit.Position;
                    GridPosition direction = new GridPosition(Math.Sign(before.X - center.X), Math.Sign(before.Y - center.Y));
                    GridPosition destination = before + direction * Math.Max(1, rule.Amount);
                    bool prevented = battle.Combat.ArtifactBattle?.TryPreventForcedMove(unit.Id) == true;
                    bool legal = !prevented && battle.Combat.Map.IsInside(destination) && !battle.Combat.Map.IsBlocked(destination) && !battle.Combat.IsOccupied(destination, unit.Id);
                    if (legal) { unit.MoveTo(destination); battle.Combat.ResolveDisplacementLanding(unit, before); }
                    Add(steps, spell, rule.Kind, unit.Id, destination, rule.Amount, legal ? rule.Amount : 0, legal ? "push" : prevented ? "anchored" : "blocked");
                }
            }
            else if (rule.Kind == FireRuleKind.OverloadDevice) foreach (GridPosition cell in cellArray) { TileState tile = battle.Combat.Map.GetTile(cell); if (tile.IsDevice && (rule.DestructibleMask & FireDestructibleMask.Device) != 0 && ConditionMet(battle, source, primary, cell, rule.Condition)) { battle.Overload(cell); Add(steps, spell, rule.Kind, null, cell, 1, 1, "overload"); } }
        }

        // 总案 3.5.6.2／3.6.1：术式伤害在结算单位的同时让作用几何内的物块扣减同值耐久，
        // 因此带无条件伤害的术式同样把可破坏物件视为合法选定目标（"可受击"目标的物件分支）。
        private static bool IsObjectRule(FireSpellRule rule) =>
            rule.Kind == FireRuleKind.DamageDurability || rule.Kind == FireRuleKind.DestroyLightCover || rule.Kind == FireRuleKind.OverloadDevice ||
            ((rule.Kind == FireRuleKind.Damage || rule.Kind == FireRuleKind.WeaponDamage) && rule.Condition == FireCondition.Always && rule.Amount > 0);
        private static bool MatchesObject(TileState tile, FireDestructibleMask mask)
        {
            if (tile == null || tile.Durability <= 0) return false;
            FireDestructibleMask kind = tile.IsLampVine ? FireDestructibleMask.Plant : tile.IsDevice || tile.IsObjective ? FireDestructibleMask.Device : tile.Cover == CoverType.Light ? FireDestructibleMask.LightCover : tile.Cover == CoverType.Heavy ? FireDestructibleMask.HeavyCover : FireDestructibleMask.None;
            return kind != FireDestructibleMask.None && (mask & kind) != 0;
        }

        private static void Add(List<FireSpellResultStep> steps, FireSpellDefinition spell, FireRuleKind kind, string target, GridPosition cell, int requested, int applied, string detail)
            => steps.Add(new FireSpellResultStep(steps.Count, spell.Id, kind, target, cell, requested, applied, detail,
                spell.DeliveryMode, spell.TriggerWindow, spell.ConsumptionRule));
    }
}
