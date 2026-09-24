using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum CombatRuleset { LegacyStory, Roguelite }
    public enum ForcedMoveResult { Moved, Blocked, ObjectCollision }

    public sealed class CombatState
    {
        private readonly Dictionary<string, UnitState> units;
        private readonly Dictionary<string, int> fixedTurnOrder;
        private readonly HashSet<GridPosition> investigated = new HashSet<GridPosition>();
        private readonly Dictionary<string, int> rogueTurnSequences = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> rogueShieldSourceTurns = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> rogueBreakStanceSeenThisTurn = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Roguelite.ShieldSourceRecord> rogueShieldEvents = new List<Roguelite.ShieldSourceRecord>();

        public GridMap Map { get; }
        public string ActiveUnitId { get; private set; }
        public int CurrentTime { get; private set; }
        public int TurnSequence { get; private set; }
        public bool IsVictory { get; private set; }
        public bool IsDefeat { get; private set; }
        public CombatRuleset Ruleset { get; private set; } = CombatRuleset.LegacyStory;
        public List<string> EventLog { get; } = new List<string>();
        public IReadOnlyDictionary<string, UnitState> Units => units;
        public InventoryGrid Backpack { get; private set; } = new InventoryGrid(InventoryContainerState.BaseWidth, InventoryContainerState.BaseHeight);
        public InventoryContainerState ItemInventory { get; private set; } = new InventoryContainerState();
        public LootContainer Loot { get; private set; }
        public LootSourceState LootSource { get; private set; }
        public string[] ItemQuickbar { get; } = new string[Roguelite.RogueRuntimeConstants.ItemQuickbarSize];
        public IReadOnlyList<CombatObjective> Objectives { get; private set; }
        internal ArtifactBattleState ArtifactBattle { get; private set; }
        public Roguelite.RogueSpellCombatRuntime RogueSpells { get; private set; }
        public Roguelite.RogueEquipmentRuntime RogueEquipment { get; private set; }
        public CombatPassiveRuntime PassiveEffects { get; private set; } = new CombatPassiveRuntime();
        public RainLanternCourtRuntime RainLanternCourt { get; private set; }
        public GreenhouseCollectionRoomRuntime GreenhouseCollectionRoom { get; private set; }
        public ThreeMaterialPressureRuntime ThreeMaterialPressure { get; private set; }
        public AcademyCoreBossRuntime AcademyCoreBoss { get; private set; }
        /// <summary>通用场地敌人的公开条件反应（老寻、灯台值守）。</summary>
        public AcademyFieldEnemyRuntime AcademyFieldEnemy { get; private set; }
        /// <summary>全场环境状态：风与光带。不占格，随战斗创建并在克隆时一并复制。</summary>
        public FieldEnvironmentState Environment { get; private set; } = new FieldEnvironmentState();
        public CombatPressureTestRuntime PressureTest { get; private set; }
        public IReadOnlyList<Roguelite.ShieldSourceRecord> RogueShieldEvents => rogueShieldEvents;
        public int InventoryOpenCount { get; private set; }

        public CombatState(GridMap map, IEnumerable<UnitState> units, IEnumerable<CombatObjective> objectives = null)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            UnitState[] orderedUnits = (units ?? throw new ArgumentNullException(nameof(units))).ToArray();
            this.units = orderedUnits.ToDictionary(unit => unit.Id, StringComparer.Ordinal);
            fixedTurnOrder = orderedUnits.Select((unit, index) => new { unit.Id, Index = index })
                .ToDictionary(value => value.Id, value => value.Index, StringComparer.Ordinal);

            if (this.units.Count == 0)
            {
                throw new ArgumentException("Combat requires at least one unit.", nameof(units));
            }

            foreach (UnitState unit in this.units.Values)
            {
                if (!Map.IsInside(unit.Position) || Map.IsBlocked(unit.Position))
                {
                    throw new ArgumentException($"Unit {unit.Id} has an invalid starting position.", nameof(units));
                }
            }

            if (this.units.Values.Select(unit => unit.Position).Distinct().Count() != this.units.Count)
            {
                throw new ArgumentException("Multiple units cannot occupy the same starting position.", nameof(units));
            }
            if (objectives != null) Objectives = objectives.ToList();
            else
            {
                GridPosition[] objectivePositions = Map.PositionsWith(tile => tile.IsObjective).ToArray();
                Objectives = objectivePositions.Length == 0 ? new List<CombatObjective>() : new List<CombatObjective> { new DestructionObjective(objectivePositions) };
            }
        }

        public void ConfigureObjectives(params CombatObjective[] objectives)
        { if (objectives == null || objectives.Length == 0) throw new ArgumentException("At least one objective is required.", nameof(objectives)); Objectives = objectives.ToList(); EvaluateOutcome(); }
        public bool IsInvestigated(GridPosition position) => investigated.Contains(position);
        public void ConfigureRuleset(CombatRuleset ruleset)
        {
            Ruleset = ruleset;
            if (ruleset == CombatRuleset.Roguelite)
                foreach (UnitState unit in units.Values) unit.ClearShield();
        }
        public bool HasLineOfSight(GridPosition from, GridPosition to) => Map.HasLineOfSight(from, to, CurrentTime);
        internal int ResolveDisplacementLanding(UnitState unit, GridPosition previousPosition)
        {
            if (unit == null || unit.Position == previousPosition) return 0;
            TileState landing = Map.GetTile(unit.Position);
            if (landing.IsWater && unit.HasStatus(StatusType.Burning))
            {
                unit.ClearStatus(StatusType.Burning);
                AddLog(unit.DisplayName + "进入浅水，燃烧已移除。");
            }
            return RogueSpells?.FireBattle.ResolveEntry(unit, previousPosition) ?? 0;
        }

        /// <summary>统一推拉结算；全段不可完成时留在位移前格，撞玩法物块则双方结算4点伤害。</summary>
        public ForcedMoveResult ResolveForcedMove(UnitState unit, GridPosition direction, int distance, string sourceId)
        {
            if (unit == null || !unit.IsAlive || distance <= 0 || unit.HasStatus(StatusType.Invulnerable)) return ForcedMoveResult.Blocked;
            int dx = Math.Sign(direction.X), dy = Math.Sign(direction.Y);
            if (dx != 0 && dy != 0)
            {
                if (Math.Abs(direction.X) >= Math.Abs(direction.Y)) dy = 0;
                else dx = 0;
            }
            if (dx == 0 && dy == 0) return ForcedMoveResult.Blocked;

            int reduction = PassiveEffects.ConsumeForcedMoveReduction(unit.Id);
            if (reduction > 0)
            {
                distance = Math.Max(0, distance - reduction);
                AddLog(unit.DisplayName + "的缓冲护幕抵消 " + reduction + " 格强制位移。");
                if (distance == 0) return ForcedMoveResult.Blocked;
            }

            GridPosition origin = unit.Position;
            GridPosition cursor = origin;
            for (int step = 0; step < distance; step++)
            {
                GridPosition next = cursor + new GridPosition(dx, dy);
                if (!Map.IsInside(next) || IsOccupied(next, unit.Id)) return ForcedMoveResult.Blocked;
                if (Map.IsBlocked(next))
                {
                    TileState obstacle = Map.GetTile(next);
                    if (obstacle.BlocksMovement && !string.IsNullOrEmpty(obstacle.ObjectName()))
                    {
                        ResolveForcedMoveObjectCollision(unit, next, sourceId);
                        return ForcedMoveResult.ObjectCollision;
                    }
                    return ForcedMoveResult.Blocked;
                }
                cursor = next;
            }

            unit.MoveTo(cursor);
            ResolveDisplacementLanding(unit, origin);
            return ForcedMoveResult.Moved;
        }

        private void ResolveForcedMoveObjectCollision(UnitState unit, GridPosition obstaclePosition, string sourceId)
        {
            const int damageAmount = 4;
            string damageSource = string.IsNullOrWhiteSpace(sourceId) ? "forced-move-collision" : sourceId + "-collision";
            Roguelite.DamagePacket packet = new Roguelite.DamagePacket(damageSource, string.Empty, unit.Id,
                damageSource, new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Physical, damageAmount) });
            Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, unit.Shield, unit.Health);
            unit.AbsorbShield(damage.ShieldAbsorbed);
            RecordRogueliteShieldAbsorption(unit.Id, damageSource, damage.ShieldAbsorbed);
            unit.TakeDamage(damage.HealthDamage);

            TileState obstacle = Map.GetTile(obstaclePosition);
            string obstacleName = obstacle.ObjectName();
            int durabilityBefore = obstacle.Durability;
            if (durabilityBefore > 0)
            {
                obstacle.Durability = Math.Max(0, durabilityBefore - damageAmount);
                ResolveAetherCrystalDamage(obstaclePosition, durabilityBefore);
            }
            AddLog(unit.DisplayName + "强制位移撞上" + obstacleName + "：单位受到 4 点伤害" +
                (durabilityBefore > 0 ? "，物块耐久 -" + Math.Min(damageAmount, durabilityBefore) : string.Empty) + "，并留在原格。");
            EvaluateOutcome();
        }
        internal void BeginRogueliteTurn(UnitState unit)
        {
            if (unit == null) return;
            if (unit.HasStatus(StatusType.BreakStance)) rogueBreakStanceSeenThisTurn.Add(unit.Id);
            if (Ruleset != CombatRuleset.Roguelite) return;
            foreach (GridPosition position in Map.PositionsWith(tile => tile.SmokeExpiresAt > 0 && tile.SmokeExpiresAt <= CurrentTime).ToArray())
                Map.GetTile(position).SmokeExpiresAt = 0;
            // 主角回合开始即公共回合开始：此时公开风况并让风搬动场地上的松散材料。
            if (unit.IsHero) AdvanceFieldEnvironment();
            if (unit.Shield > 0)
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(unit.Id + ":turn_start", unit.Shield,
                    Roguelite.ShieldEventKind.ClearedAtTurnStart, RogueTurn(unit.Id) + 1));
            unit.ClearShield();
            int turn = rogueTurnSequences.TryGetValue(unit.Id, out int current) ? current + 1 : 1;
            rogueTurnSequences[unit.Id] = turn;

            if (unit.EnemyArchetypeId == "shieldguard")
                TryGrantRogueliteShield(unit.Id, "shieldguard-turn-brace", 2);

            RogueEquipment?.OnTurnStart(this, unit.Id);
            RainLanternCourt?.BeginTurn(unit);
            ThreeMaterialPressure?.BeginTurn(this, unit);
            AcademyCoreBoss?.BeginTurn(this, unit);
            AcademyFieldEnemy?.BeginTurn(this, unit);
            PressureTest?.BeginTurn(unit);

        }
        internal void EndRogueliteTurn(UnitState unit)
        {
            if (unit == null) return;
            if (Ruleset == CombatRuleset.Roguelite)
            {
                GrantCoverShield(unit);
                RogueSpells?.EndOwnTurn(unit.Id);
                RainLanternCourt?.EndTurn(unit);
                AcademyFieldEnemy?.EndTurn(this, unit);
                AcademyCoreBoss?.EndTurn(this, unit);
                ThreeMaterialPressure?.EndTurn(unit);
                PassiveEffects.ExpireOwnTurnEnd(unit.Id);
            }
            if (rogueBreakStanceSeenThisTurn.Remove(unit.Id)) unit.ClearStatus(StatusType.BreakStance);
        }
        /// <summary>单位踏入约束纹：留在原格，本回合不能主动移动。</summary>
        internal void ResolveBindingMarkEntry(UnitState unit)
        {
            if (unit == null || !unit.IsAlive || !Map.IsInside(unit.Position)) return;
            if (!Map.GetTile(unit.Position).IsBindingMark) return;
            unit.ApplyStatus(StatusType.Bound, 1);
            AddLog(unit.DisplayName + "踏入约束纹，留在原格，本回合不能主动移动。");
        }

        private static GridPosition[] Adjacent(GridPosition position) => new[]
        {
            position + new GridPosition(0, 1),
            position + new GridPosition(1, 0),
            position + new GridPosition(0, -1),
            position + new GridPosition(-1, 0)
        };

        /// <summary>公共回合开始的环境结算：公开风况，并按风向逐格搬动松散材料、吹散痕迹。</summary>
        private void AdvanceFieldEnvironment()
        {
            FieldWindState wind = Environment.Wind;
            AddLog("风况：" + wind.PreviewText() + "。");
            if (wind.Level <= 0) return;

            if (wind.Level >= 3)
                foreach (GridPosition position in Map.PositionsWith(tile => tile.HasTrace).ToArray())
                {
                    TileState cleared = Map.GetTile(position).Clone();
                    cleared.HasTrace = false;
                    Map.SetTile(position, cleared);
                    AddLog("强风吹散了 (" + position.X + "," + position.Y + ") 的痕迹。");
                }

            // 逆风向处理：先推动下风处的材料，同一份材料不会被连续推动多格。
            List<GridPosition> sources = Map.PositionsWith(IsLooseMaterial).ToList();
            GridPosition direction = wind.Direction;
            sources.Sort((left, right) => Project(right, direction).CompareTo(Project(left, direction)));
            int moved = 0;
            foreach (GridPosition from in sources)
            {
                GridPosition to = from + direction;
                if (!Map.IsInside(to) || Map.IsBlocked(to) || IsOccupied(to)) continue;
                TileState source = Map.GetTile(from).Clone(), target = Map.GetTile(to).Clone();
                if (IsLooseMaterial(target) || target.HasEffectLayer) continue;
                if (source.IsLoosePaper) { target.IsLoosePaper = true; target.IsPaperSoaked = source.IsPaperSoaked; source.IsLoosePaper = false; source.IsPaperSoaked = false; }
                if (source.IsCrystalShard) { target.IsCrystalShard = true; source.IsCrystalShard = false; }
                if (source.SmokeExpiresAt > 0) { target.SmokeExpiresAt = source.SmokeExpiresAt; source.SmokeExpiresAt = 0; }
                Map.SetTile(from, source);
                Map.SetTile(to, target);
                moved++;
            }
            if (moved > 0) AddLog("风搬动 " + moved + " 处松散材料，方向 " + FieldWindState.DirectionName(direction) + "。");
        }

        private static bool IsLooseMaterial(TileState tile) =>
            tile != null && (tile.IsLoosePaper || tile.IsCrystalShard || tile.SmokeExpiresAt > 0);

        private static int Project(GridPosition position, GridPosition direction) =>
            position.X * direction.X + position.Y * direction.Y;

        private void GrantCoverShield(UnitState unit)
        {
            int coverShield = 0;
            TileState standing = Map.GetTile(unit.Position);
            if (standing.Cover == CoverType.Light && !standing.IsDestroyed) coverShield = 2;
            GridPosition[] adjacent =
            {
                unit.Position + new GridPosition(0, 1),
                unit.Position + new GridPosition(1, 0),
                unit.Position + new GridPosition(0, -1),
                unit.Position + new GridPosition(-1, 0)
            };
            if (adjacent.Any(position => Map.IsInside(position) && Map.GetTile(position).Cover == CoverType.Heavy && !Map.GetTile(position).IsDestroyed))
                coverShield = Math.Max(coverShield, 4);
            if (coverShield > 0) TryGrantRogueliteShield(unit.Id, coverShield == 4 ? "cover-heavy" : "cover-light", coverShield);
            // 护罩发生器：为邻接单位额外提供结构护盾，来源独立、可与掩体护盾叠加；装置被摧毁即不再提供。
            bool wardAdjacent = adjacent.Any(position => Map.IsInside(position) &&
                Map.GetTile(position).IsWardGenerator && !Map.GetTile(position).IsDestroyed);
            if (wardAdjacent) TryGrantRogueliteShield(unit.Id, "ward-generator", 4);
        }
        public bool TryGrantRogueliteShield(string unitId, string sourceId, int amount)
        {
            if (Ruleset != CombatRuleset.Roguelite || amount <= 0 || string.IsNullOrWhiteSpace(sourceId)) return false;
            UnitState unit = GetUnit(unitId);
            if (unit == null || !unit.IsAlive) return false;
            // 老库管的登记：被标记单位下一次获得的护盾被优先清除。
            if (AcademyFieldEnemy != null && AcademyFieldEnemy.ConsumeInspectionMark(unit.Id))
            {
                AddLog("登记生效：" + unit.DisplayName + "本次获得的护盾被优先清除。");
                return false;
            }
            if (unit.HasStatus(StatusType.BreakStance))
            {
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(sourceId, amount,
                    Roguelite.ShieldEventKind.PreventedByBreakStance, RogueTurn(unit.Id)));
                AddLog(unit.DisplayName + "的" + sourceId + "护盾被破势阻止。");
                return false;
            }
            int turn = rogueTurnSequences.TryGetValue(unit.Id, out int current) ? current : 0;
            string key = unit.Id + "|" + sourceId;
            if (rogueShieldSourceTurns.TryGetValue(key, out int claimed) && claimed == turn) return false;
            rogueShieldSourceTurns[key] = turn;
            int shieldBefore = unit.Shield;
            unit.GrantShield(amount);
            int granted = unit.Shield - shieldBefore;
            AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(sourceId, granted,
                Roguelite.ShieldEventKind.Granted, turn));
            AddLog(unit.DisplayName + "从" + sourceId + "获得 " + granted + " 护盾。");
            return true;
        }
        internal void RecordRogueliteShieldAbsorption(string unitId, string sourceId, int amount)
        {
            if (Ruleset != CombatRuleset.Roguelite || amount <= 0) return;
            AddRogueShieldEvent(new Roguelite.ShieldSourceRecord(
                string.IsNullOrWhiteSpace(sourceId) ? "damage" : sourceId, amount,
                Roguelite.ShieldEventKind.Absorbed, RogueTurn(unitId)));
        }
        public void ApplyRogueliteBreakStance(string unitId, string sourceId = null)
        {
            UnitState unit = GetUnit(unitId) ?? throw new InvalidOperationException("Unit does not exist.");
            if (unit.Shield > 0 && Ruleset == CombatRuleset.Roguelite)
                AddRogueShieldEvent(new Roguelite.ShieldSourceRecord("break_stance", unit.Shield,
                    Roguelite.ShieldEventKind.Wasted, RogueTurn(unit.Id)));
            unit.ClearShield(); unit.ApplyStatus(StatusType.BreakStance, 1, 0, sourceId); rogueBreakStanceSeenThisTurn.Remove(unit.Id);
            AddLog(unit.DisplayName + "进入破势：当前护盾清除，至下一次自己回合结束前无法获得护盾。");
        }
        private int RogueTurn(string unitId) => rogueTurnSequences.TryGetValue(unitId, out int turn) ? turn : 0;
        private void AddRogueShieldEvent(Roguelite.ShieldSourceRecord record)
        {
            rogueShieldEvents.Insert(0, record);
            if (rogueShieldEvents.Count > 16) rogueShieldEvents.RemoveAt(rogueShieldEvents.Count - 1);
        }
        internal void MarkInvestigated(GridPosition position) => investigated.Add(position);

        public UnitState GetUnit(string unitId) =>
            string.IsNullOrEmpty(unitId) ? null : units.TryGetValue(unitId, out UnitState unit) ? unit : null;

        public int FixedTurnOrder(string unitId) =>
            !string.IsNullOrEmpty(unitId) && fixedTurnOrder.TryGetValue(unitId, out int order) ? order : int.MaxValue;

        public bool IsOccupied(GridPosition position, string ignoredUnitId = null) =>
            units.Values.Any(unit => unit.IsAlive && unit.Id != ignoredUnitId && unit.Position == position);

        public void SetLoot(LootContainer loot) => Loot = loot;
        internal void AttachArtifactBattle(ArtifactBattleState battle) => ArtifactBattle = battle;
        public void AttachRogueSpellRuntime(Roguelite.RogueSpellCombatRuntime runtime)
        {
            if (runtime == null || runtime.Combat != this) throw new ArgumentException("Rogue spell runtime must belong to this combat.", nameof(runtime));
            RogueSpells = runtime;
        }
        public void AttachRogueEquipmentRuntime(Roguelite.RogueEquipmentRuntime runtime)
        {
            RogueEquipment = runtime ?? throw new ArgumentNullException(nameof(runtime));
            RogueEquipment.AttachToCombat(this);
        }
        public void AttachRainLanternCourt(RainLanternCourtRuntime runtime)
        {
            RainLanternCourt = runtime ?? throw new ArgumentNullException(nameof(runtime));
            RainLanternCourt.Attach(Map);
            PassiveEffects.RegisterPassive("hero", new CombatPassiveDefinition("origin:ORIGIN-TALENT-01",
                "就地接线", CombatPassiveSourceKind.OriginTalent, "ORIGIN-TALENT-01",
                CombatPassiveTrigger.AfterActiveMove,
                "每场战斗第一次移动结束时，若终点正交邻接掩体，获得 2 护盾并恢复 1 点个人魔力。", 30,
                "first_move_ends_adjacent_cover", "grant_shield:2|restore_mana:1", 1,
                CombatPassiveLimitScope.Battle));
        }
        public void AttachGreenhouseCollectionRoom(GreenhouseCollectionRoomRuntime runtime)
        { GreenhouseCollectionRoom = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public void AttachThreeMaterialPressure(ThreeMaterialPressureRuntime runtime)
        { ThreeMaterialPressure = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public void AttachAcademyCoreBoss(AcademyCoreBossRuntime runtime)
        { AcademyCoreBoss = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public void AttachAcademyFieldEnemy(AcademyFieldEnemyRuntime runtime)
        { AcademyFieldEnemy = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        public void AttachPressureTest(CombatPressureTestRuntime runtime)
        { PressureTest = runtime ?? throw new ArgumentNullException(nameof(runtime)); }
        internal void RecordInventoryOpened() { InventoryOpenCount++; }
        public void SetLootSource(LootSourceState loot) => LootSource = loot;
        /// <summary>解析物件被摧毁后的结果。当前覆盖蓄能晶簇爆裂与过载装置引爆。</summary>
        public bool ResolveAetherCrystalDamage(GridPosition position, int durabilityBefore)
        {
            if (ResolveOverloadDeviceDamage(position, durabilityBefore)) return true;
            if (!Map.IsInside(position)) return false;
            TileState crystal = Map.GetTile(position);
            if (!crystal.IsAetherCrystal || durabilityBefore <= 0 || crystal.Durability > 0) return false;

            crystal.IsAetherCrystal = false;
            crystal.IsDevice = false;
            GridPosition[] blast =
            {
                position,
                position + new GridPosition(0, 1),
                position + new GridPosition(1, 0),
                position + new GridPosition(0, -1),
                position + new GridPosition(-1, 0)
            };
            foreach (GridPosition cell in blast.Where(Map.IsInside))
            {
                TileState shard = Map.GetTile(cell).Clone();
                shard.IsCrystalShard = true;
                Map.SetTile(cell, shard);
            }
            foreach (UnitState unit in units.Values.Where(value => value.IsAlive && value.Position.ManhattanDistance(position) == 1))
            {
                Roguelite.DamagePacket packet = new Roguelite.DamagePacket("b2-crystal-burst", string.Empty, unit.Id,
                    "b2-crystal-burst", new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Aether, 8) });
                Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, unit.Shield, unit.Health);
                unit.AbsorbShield(damage.ShieldAbsorbed);
                RecordRogueliteShieldAbsorption(unit.Id, "b2-crystal-burst", damage.ShieldAbsorbed);
                unit.TakeDamage(damage.HealthDamage);
            }
            AddLog("蓄能晶簇爆裂，正交四格受到 8 点以太伤害并留下五格碎晶。");
            EvaluateOutcome();
            return true;
        }

        /// <summary>过载装置耐久归零时引爆，正交四格结算一次不分敌我的以太伤害。</summary>
        public bool ResolveOverloadDeviceDamage(GridPosition position, int durabilityBefore)
        {
            if (!Map.IsInside(position)) return false;
            TileState device = Map.GetTile(position);
            if (!device.IsOverloadDevice || durabilityBefore <= 0 || device.Durability > 0) return false;

            TileState clearedDevice = device.Clone();
            clearedDevice.IsOverloadDevice = false;
            Map.SetTile(position, clearedDevice);
            GridPosition[] blast =
            {
                position + new GridPosition(0, 1),
                position + new GridPosition(1, 0),
                position + new GridPosition(0, -1),
                position + new GridPosition(-1, 0)
            };
            foreach (UnitState unit in units.Values.Where(value => value.IsAlive && blast.Contains(value.Position)))
            {
                Roguelite.DamagePacket packet = new Roguelite.DamagePacket("overload-device", string.Empty, unit.Id,
                    "overload-device", new[] { new Roguelite.DamageComponent(Roguelite.DamageComponentKind.Aether, 8) });
                Roguelite.DamageResolution damage = Roguelite.RogueDamageResolver.Resolve(packet, unit.Shield, unit.Health);
                unit.AbsorbShield(damage.ShieldAbsorbed);
                RecordRogueliteShieldAbsorption(unit.Id, "overload-device", damage.ShieldAbsorbed);
                unit.TakeDamage(damage.HealthDamage);
            }
            AddLog("过载装置引爆，正交四格受到 8 点以太伤害，敌我一致。");
            EvaluateOutcome();
            return true;
        }
        public void ConfigureItemInventory(InventoryContainerState inventory, IEnumerable<string> quickbarIds)
        {
            ItemInventory = (inventory ?? throw new ArgumentNullException(nameof(inventory))).Clone(); Array.Clear(ItemQuickbar, 0, ItemQuickbar.Length);
            if (quickbarIds == null) return; int index = 0; foreach (string id in quickbarIds.Take(ItemQuickbar.Length)) { if (ItemInventory.Get(id) != null) ItemQuickbar[index] = id; index++; }
        }
        public InventoryResult EquipItemQuickbar(string instanceId, int index)
        {
            if (index < 0 || index >= ItemQuickbar.Length) return new InventoryResult(InventoryError.OutOfBounds, instanceId);
            ItemInstance item = ItemInventory.Get(instanceId); if (item == null) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId); if (!definition.CanQuickEquip) return new InventoryResult(InventoryError.Restricted, instanceId);
            string replacedId = ItemQuickbar[index];
            if ((definition.Category == ItemCategory.Scroll || definition.Category == ItemCategory.Artifact) && ItemQuickbar.Where(id => !string.IsNullOrEmpty(id) && id != replacedId && id != instanceId).Select(id => ItemInventory.Get(id)).Where(value => value != null).Count(value =>
            {
                ItemCategory category = ItemCatalog.Get(value.DefinitionId).Category; return category == ItemCategory.Scroll || category == ItemCategory.Artifact;
            }) >= 4) return new InventoryResult(InventoryError.QuickbarFull, instanceId);
            for (int i = 0; i < ItemQuickbar.Length; i++) if (ItemQuickbar[i] == instanceId) ItemQuickbar[i] = null;
            ItemQuickbar[index] = instanceId; return InventoryResult.Ok(instanceId, index, 0);
        }
        public bool ConsumeInventoryItem(string instanceId, int amount = 1)
        {
            ItemInstance item = ItemInventory.Get(instanceId); if (item == null || !item.TryConsume(amount)) return false;
            if (!item.IsDepleted) return true;
            ItemInventory.Remove(instanceId); for (int i = 0; i < ItemQuickbar.Length; i++) if (ItemQuickbar[i] == instanceId) ItemQuickbar[i] = null; return true;
        }

        internal void SetActiveUnit(string unitId) => ActiveUnitId = unitId;
        internal void RecordTurnStart() => TurnSequence++;
        internal void SetCurrentTime(int time) => CurrentTime = time;
        public void AddLog(string message) { EventLog.Insert(0, message); if (EventLog.Count > 8) EventLog.RemoveAt(8); }
        internal void EvaluateOutcome()
        {
            AcademyCoreBoss?.RefreshPhase(this);
            IsDefeat = !units.Values.Any(unit => unit.IsHero && unit.IsAlive) ||
                Objectives != null && Objectives.Any(objective => objective.IsFailed(this));
            IsVictory = !IsDefeat && Objectives != null && Objectives.Count > 0 && Objectives.All(objective => objective.IsComplete(this));
        }
        public void ResolveDebugOutcome(bool victory)
        {
            foreach (UnitState unit in units.Values)
            {
                if (victory && !unit.IsHero) unit.TakeDamage(int.MaxValue);
                if (!victory && unit.IsHero) unit.TakeDamage(int.MaxValue);
            }
            if (victory)
            {
                foreach (DestructionObjective objective in Objectives.OfType<DestructionObjective>())
                    foreach (GridPosition position in objective.Positions) Map.GetTile(position).Durability = 0;
            }
            EvaluateOutcome();
        }
        public CombatState Clone()
        {
            CombatState clone = new CombatState(Map.Clone(), units.Values.OrderBy(unit => FixedTurnOrder(unit.Id)).Select(unit => unit.Clone()), Objectives.Select(objective => objective.Clone()));
            clone.ActiveUnitId = ActiveUnitId; clone.CurrentTime = CurrentTime; clone.TurnSequence = TurnSequence; clone.IsVictory = IsVictory; clone.IsDefeat = IsDefeat; clone.Ruleset = Ruleset; clone.InventoryOpenCount = InventoryOpenCount;
            clone.Backpack = Backpack.Clone(); clone.ItemInventory = ItemInventory.Clone(); clone.Loot = Loot?.Clone(); clone.LootSource = LootSource?.Clone(); Array.Copy(ItemQuickbar, clone.ItemQuickbar, ItemQuickbar.Length);
            foreach (GridPosition position in investigated) clone.investigated.Add(position);
            foreach (KeyValuePair<string, int> pair in rogueTurnSequences) clone.rogueTurnSequences[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in rogueShieldSourceTurns) clone.rogueShieldSourceTurns[pair.Key] = pair.Value;
            foreach (string unitId in rogueBreakStanceSeenThisTurn) clone.rogueBreakStanceSeenThisTurn.Add(unitId);
            clone.rogueShieldEvents.AddRange(rogueShieldEvents);
            clone.PassiveEffects = PassiveEffects.Clone();
            if (RainLanternCourt != null) clone.RainLanternCourt = RainLanternCourt.Clone(clone.Map);
            if (GreenhouseCollectionRoom != null) clone.GreenhouseCollectionRoom = GreenhouseCollectionRoom.Clone();
            if (ThreeMaterialPressure != null) clone.ThreeMaterialPressure = ThreeMaterialPressure.Clone();
            if (AcademyCoreBoss != null) clone.AcademyCoreBoss = AcademyCoreBoss.Clone();
            if (AcademyFieldEnemy != null) clone.AcademyFieldEnemy = AcademyFieldEnemy.Clone();
            clone.Environment = Environment.Clone();
            if (PressureTest != null) clone.PressureTest = PressureTest.Clone();
            clone.EventLog.AddRange(EventLog); return clone;
        }
    }
}
