using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class CombatBattlefieldCellPresenter
    {
        private readonly BattlefieldPresentationAdapter battlefield;
        private readonly CombatFormalVisualAssets assets;

        public CombatBattlefieldCellPresenter(BattlefieldPresentationAdapter battlefield,
            CombatFormalVisualAssets assets)
        {
            this.battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public BattlefieldCellPresentation Build(CombatState state, FirstRegionLevelDefinition level,
            FireBattleState fireBattle, CombatSelectionController selection, bool trainingRangeActive,
            CombatVisualFeedback feedback, GridPosition position,
            Func<int, FireSpellDefinition> fireSpellInSlot,
            Func<FireSpellDefinition, GridPosition, FireSpellPreview> firePreviewAt,
            Func<UnitState, CombatTargetDamageForecast> damageForecast,
            Func<UnitState, EnemyIntentPresentation> enemyIntent)
        {
            if (state == null || !state.Map.IsInside(position)) return null;
            TileState tile = feedback?.PresentedTile(position, state.Map.GetTile(position)) ?? state.Map.GetTile(position);
            int environmentFrame = Mathf.FloorToInt(Time.unscaledTime * 8f) % assets.EnvironmentFrameCount;
            bool hasFireground = fireBattle?.HasFireground(position) == true;
            hasFireground = feedback?.PresentedFireground(position, hasFireground) ?? hasFireground;
            Texture2D environment = hasFireground
                ? assets.FiregroundFrame(environmentFrame)
                : tile.SmokeExpiresAt > state.CurrentTime ? assets.SmokeFrame(environmentFrame)
                : tile.IsWater ? assets.Environment("rain_court_water") : null;
            // The right-click menu previews an action before it is committed. While the menu is open its
            // preview replaces the committed action, so the board never mixes two actions' ranges; an
            // empty preview action means only the menu's anchor cell is marked.
            bool menuPreview = selection.HasMenuPreview;
            string rangeAction = menuPreview ? selection.MenuPreviewAction : selection.Action;
            bool showRange = !menuPreview || !string.IsNullOrEmpty(rangeAction);

            bool inMoveRange = showRange && rangeAction == "移动" && battlefield.IsInMoveRange(state, position);
            PressureReactionPreview pressureReaction = inMoveRange
                ? state.PressureTest?.PreviewHeroMove(state, position) : null;
            BattlefieldCellMarker moveMarker = !inMoveRange ? BattlefieldCellMarker.None
                : pressureReaction?.WillTrigger == true ? BattlefieldCellMarker.MovementRisk
                : BattlefieldCellMarker.MovementRange;
            BattlefieldCellMarker attackMarker = BattlefieldCellMarker.None;
            if (showRange && rangeAction == "攻击")
            {
                if (battlefield.IsInAttackRange(state, position))
                    attackMarker = battlefield.IsLegalTarget(state, "攻击", position)
                        ? BattlefieldCellMarker.AttackTarget : BattlefieldCellMarker.AttackEnvelope;
                else if (IsInsideWeaponDeadZone(state, position))
                    attackMarker = BattlefieldCellMarker.Undeliverable;
            }
            BattlefieldCellMarker skillMarker = BattlefieldCellMarker.None;
            int fireSlot = showRange ? FireSpellSlot(rangeAction) : -1;
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : fireSpellInSlot(fireSlot);
            FireSpellPreview firePreview = fireSpell == null ? null : firePreviewAt(fireSpell, position);
            if (firePreview?.CanCommit == true)
                skillMarker = firePreview.FriendlyFireRisk ? BattlefieldCellMarker.SkillRisk : BattlefieldCellMarker.SkillSelectable;
            else if (IsInsideSpellDeadZone(state, fireSpell, position))
                skillMarker = BattlefieldCellMarker.Undeliverable;
            else if (SupportsRangeEnvelope(fireSpell) && IsInsideDeclaredReach(firePreview))
                // A spell with no legal target yet still has to answer "how far does this reach".
                skillMarker = BattlefieldCellMarker.SkillRangeEnvelope;
            GridPosition? previewCenter = menuPreview ? (GridPosition?)selection.MenuPreviewPosition
                : selection.HasPreviewPosition ? selection.PreviewPosition : (GridPosition?)null;
            if (!previewCenter.HasValue && fireSpell != null && !string.IsNullOrEmpty(selection.TargetId))
            {
                UnitState selectedTarget = state.GetUnit(selection.TargetId);
                if (selectedTarget != null) previewCenter = selectedTarget.Position;
            }
            if (fireSpell != null && previewCenter.HasValue)
            {
                FireSpellPreview selectedPreview = firePreviewAt(fireSpell, previewCenter.Value);
                if (selectedPreview?.CanCommit == true && selectedPreview.Cells.Contains(position))
                {
                    UnitState affected = state.Units.Values.FirstOrDefault(candidate => candidate.IsAlive && candidate.Position == position);
                    UnitState source = state.GetUnit("hero");
                    bool friendlyCell = affected != null && source != null && affected.Id != source.Id && affected.IsHero == source.IsHero &&
                        fireSpell.Rules.Any(rule => rule.AffectAllies);
                    skillMarker = friendlyCell ? BattlefieldCellMarker.SkillRisk : BattlefieldCellMarker.SkillCommitted;
                }
            }
            Texture2D move = OverlayFor(moveMarker);
            Texture2D attack = OverlayFor(attackMarker);
            Texture2D skill = OverlayFor(skillMarker);

            UnitState unit = state.Units.Values.Select(candidate => feedback?.PresentedUnit(candidate) ?? candidate).FirstOrDefault(candidate =>
                candidate.IsAlive && candidate.Position == position);
            if (state.GreenhouseCollectionRoom?.IsRaiderHidden(state, unit) == true) unit = null;
            Texture2D unitTexture = assets.Unit(unit, feedback?.EnemyAnimationFrame(unit) ?? -1);
            Vector2 unitOffset = Vector2.zero;
            CombatMovementPose travel = feedback != null ? feedback.UnitTravelPose(unit) :
                new CombatMovementPose(new Vector2(position.X, position.Y));
            // Quantize at the overview scale; the View doubles it for the integer zoom tier.
            Vector2 travelOffset = unit == null ? Vector2.zero : travel.OffsetFrom(position, 64f);
            Color unitTint = Color.white;
            if (unit != null)
            {
                float phase = unit.IsHero ? 0f : unit.Position.X * .71f + unit.Position.Y * .37f;
                unitOffset.y = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 1.8f + phase));
                if (feedback != null)
                {
                    unitOffset += feedback.UnitPresentationOffset(unit);
                    unitOffset.x += feedback.UnitShakeOffset(unit);
                    unitTint = feedback.UnitPresentationTint(unit);
                }
            }

            CombatTargetDamageForecast forecast = unit != null && !unit.IsHero ? damageForecast(unit) : null;
            CombatUnitVitalsPresentation vitals = unit == null ? null : CombatUnitVitalsPresentation.From(unit, forecast,
                state.Ruleset == CombatRuleset.Roguelite);
            List<BattlefieldStatusVisual> statuses = unit == null ? new List<BattlefieldStatusVisual>() :
                unit.Statuses.OrderBy(entry => entry.Key).Take(6)
                    .Select(entry => new BattlefieldStatusVisual(
                        CombatStatusPresentation.From(unit, entry.Key), assets.Status(entry.Key))).ToList();
            EnemyIntentPresentation intent = unit != null && !unit.IsHero ? enemyIntent(unit) : null;
            Texture2D intentTexture = intent == null ? null : assets.Intent(intent.IconId);

            Texture2D objectTexture = null;
            Texture2D objectTextureLow = null;
            string objectLabel = string.Empty;
            Color objectLabelColor = FormalUiTheme.Text;
            if (tile.IsLampVine)
            {
                objectTexture = assets.Environment("lamp_vine");
                objectLabel = state.GreenhouseCollectionRoom?.HasHiddenRaider(state) == true &&
                    state.GreenhouseCollectionRoom.RaiderLastKnownPosition == position ? "侧锋踪迹" : string.Empty;
                objectLabelColor = new Color(.94f, .69f, .28f, .95f);
            }
            else if (tile.IsAetherCrystal)
            {
                bool pressureCrystal = state.ThreeMaterialPressure != null;
                bool damagedCrystal = tile.Durability <= (pressureCrystal ? 16 : 8);
                string crystalKey = pressureCrystal ? "academy_pressure_crystal_" : "academy_aether_crystal_";
                objectTexture = assets.Academy(crystalKey + (damagedCrystal ? "damaged" : "intact"));
                objectLabel = pressureCrystal ? "稳压晶簇" : "晶簇";
                objectLabelColor = new Color(.42f, .88f, 1f, .96f);
            }
            else if (tile.IsCrystalShard)
            {
                objectTexture = assets.Academy("academy_light_planter_rubble");
                objectLabel = "碎晶";
                objectLabelColor = new Color(.52f, .9f, 1f, .9f);
            }
            else if (tile.IsScorched)
            {
                objectTexture = assets.Academy("academy_scorched_lamp_vine");
                objectLabel = "焦痕";
                objectLabelColor = new Color(.48f, .38f, .31f, .9f);
            }
            else if (tile.IsDecoy)
            {
                objectTexture = assets.Academy("academy_decoy_lantern_active");
                objectLabel = "诱导灯";
                objectLabelColor = new Color(1f, .7f, .24f, .96f);
            }
            else if (tile.IsObjective)
            {
                string key = tile.IsDestroyed ? "academy_aether_pillar_rubble" : tile.Durability <= 6 ? "academy_aether_pillar_damaged" : "academy_aether_pillar_intact";
                objectTexture = assets.Academy(key);
                if (!tile.IsDestroyed) objectLabel = state.PressureTest?.HasProtection == true &&
                    state.PressureTest.ProtectedPosition == position ? "保护目标" : "导能柱";
            }
            else if (tile.IsPermanentWall)
            {
                // Temporary: full-cell pillar placeholder until the permanent_wall_32x32
                // generation result lands. Swap this id back to the generated asset then.
                objectTexture = assets.Academy("academy_permanent_pillar_placeholder");
                objectLabel = "永久墙";
                objectLabelColor = new Color(.72f, .78f, .86f, .96f);
            }
            else if (tile.Cover == CoverType.Light)
            {
                string family = (position.X + position.Y) % 2 == 0 ? "academy_light_stone_bench_" : "academy_light_planter_";
                string stateKey = tile.IsDestroyed ? "rubble" : tile.Durability < 4 ? "damaged" : "intact";
                if (CombatTestArenaEntry.IsDedicatedTestArena && stateKey == "intact")
                {
                    objectTexture = assets.Academy("academy_test_book_crate_intact_64");
                    objectTextureLow = assets.Academy("academy_test_book_crate_intact_32");
                }
                else
                {
                string variant = stateKey == "intact"
                    ? AcademyBattlefieldLayoutCatalog.CoverVariant(level?.Id, position, CoverType.Light)
                    : null;
                objectTexture = assets.Academy(variant ?? family + stateKey);
                }
            }
            else if (tile.Cover == CoverType.Heavy)
            {
                string family = (position.X + position.Y) % 2 == 0 ? "academy_heavy_archive_stack_" : "academy_heavy_masonry_screen_";
                string stateKey = tile.IsDestroyed ? "rubble" : tile.Durability < 7 ? "damaged" : "intact";
                if (CombatTestArenaEntry.IsDedicatedTestArena && stateKey == "intact")
                {
                    objectTexture = assets.Academy("academy_test_heavy_cover_intact_64");
                    objectTextureLow = assets.Academy("academy_test_heavy_cover_intact_32");
                }
                else
                {
                    string variant = stateKey == "intact"
                        ? AcademyBattlefieldLayoutCatalog.CoverVariant(level?.Id, position, CoverType.Heavy)
                        : null;
                    objectTexture = assets.Academy(variant ?? family + stateKey);
                }
            }
            else if (trainingRangeActive && tile.IsDevice)
            {
                objectTexture = assets.Relay(tile.IsDestroyed ? "heavy_cover_rubble" : "heavy_cover_intact");
                objectLabel = "设备";
            }
            if (tile.IsWater)
            {
                objectLabel = string.Empty;
                objectLabelColor = new Color(.38f, .82f, .94f, .92f);
            }

            bool legacyLootHere = state.Loot != null && state.Loot.Position == position;
            bool searchableLootHere = state.LootSource != null && state.LootSource.Position == position;
            Texture2D loot = legacyLootHere
                ? assets.Academy(state.Loot.IsLooted ? "academy_loot_chest_empty" : "academy_loot_chest_closed")
                : searchableLootHere ? assets.Academy(state.LootSource.State == LootSearchState.Emptied
                    ? "academy_loot_chest_empty" : "academy_loot_chest_closed") : null;
            bool selected = selection.IsKeyboardTargeting && selection.KeyboardPosition == position ||
                unit != null && unit.Id == selection.TargetId ||
                menuPreview && selection.MenuPreviewPosition == position;
            Texture2D selectionOverlay = selected ? OverlayFor(BattlefieldCellMarker.Focus) : null;
            string surfaceHover = BuildSurfaceHover(level, position);
            string terrainEffectHover = BuildTerrainEffectHover(state, fireBattle, tile, position);
            string objectHover = BuildObjectHover(state, tile, position);
            string reactionHover = pressureReaction?.WillTrigger == true
                ? "警戒反应\n" + pressureReaction.Summary : string.Empty;
            string terrainHover = JoinHoverSections(surfaceHover, terrainEffectHover, objectHover, reactionHover);
            string hover = unit == null ? terrainHover : unit.IsHero
                ? CombatInformationPresenter.BuildHeroDetails(unit)
                : CombatInformationPresenter.BuildEnemyHoverDetails(state, unit, intent) +
                  FixedEncounterEnemyRuleText(state, unit) +
                  (forecast == null ? string.Empty : "\n预计伤害：" + forecast.PlayerSummary);
            string actionHover = BuildActionHover(state, selection, position, unit, forecast, fireSpell, firePreview,
                pressureReaction, move != null, attack != null);
            if (!string.IsNullOrWhiteSpace(actionHover))
                hover = string.IsNullOrWhiteSpace(hover) ? actionHover : actionHover + "\n\n" + hover;
            if (unit != null)
                hover = AppendTerrainHover(hover, terrainHover);
            string floorKey = FloorKey(level, state.Map.Height, position.X, position.Y);
            Texture2D floor = assets.Academy(floorKey);
            string floorLowKey = AcademyBattlefieldLayoutCatalog.FloorLowAsset(level, position.X, position.Y);
            Texture2D floorLow = string.IsNullOrEmpty(floorLowKey) ? null : assets.Academy(floorLowKey);
            Rect floorUv = CombatTestArenaEntry.IsDedicatedTestArena
                ? new Rect(0f, 0f, 1f, 1f) : FloorUv(position.X, position.Y);
            float floorRotation = FloorRotationDegrees(level, position.X, position.Y);
            string boundaryId = AcademyBattlefieldLayoutCatalog.BoundaryOverlay(level, position.X, position.Y,
                out int boundaryTurns);
            Texture2D terrainBoundary = string.IsNullOrEmpty(boundaryId) ? null : assets.Academy(boundaryId);
            float terrainBoundaryRotation = -90f * boundaryTurns;
            Rect uv = unitTexture == null ? new Rect(0f, 0f, 1f, 1f) :
                CombatUnitHudLayout.UnitTextureCropUv(unitTexture.name);
            return new BattlefieldCellPresentation(position, floor, floorUv, floorRotation,
                terrainBoundary, terrainBoundaryRotation, environment, move,
                BattlefieldMarkerLadder.Alpha(moveMarker), attack, BattlefieldMarkerLadder.Alpha(attackMarker),
                skill, selectionOverlay, unitTexture, uv, unitTint, unitOffset, objectTexture, objectLabel,
                objectLabelColor, loot, unit, vitals, statuses, intent, intentTexture, hover, travelOffset,
                tile.IsLampVine ? CombatObjectLayerLayout.LampVineFrontRows : 0,
                surfaceHover, terrainEffectHover, objectHover, floorLow, objectTextureLow,
                BattlefieldMarkerLadder.Alpha(skillMarker));
        }

        private const string OutOfRangeFailure = "超出射程";
        private const string DeadZoneFailure = "目标位于近身死区";
        private const string SightLineFailure = "视线受阻";

        /// <summary>
        /// Only a single-anchor spell can advertise a per-cell reach field. Self spells target the caster,
        /// and shaped spells derive their geometry from a chosen direction, so a cell-by-cell field would
        /// describe a geometry those spells do not have.
        /// </summary>
        private static bool SupportsRangeEnvelope(FireSpellDefinition spell) =>
            spell != null && spell.TargetKind != FireTargetKind.Self && spell.Shape == FireSelectionShape.Single;

        /// <summary>
        /// Reuses the resolver's own preview: a cell is inside the declared reach when the preview fails
        /// only on what it found there, never on distance or the sight line. Classifying by the engine's
        /// own failures keeps the envelope and the resolver on one geometry instead of restating the
        /// range and line-of-sight rules here.
        /// </summary>
        private static bool IsInsideDeclaredReach(FireSpellPreview preview)
        {
            if (preview == null) return false;
            for (int i = 0; i < preview.Failures.Count; i++)
            {
                string failure = preview.Failures[i];
                if (failure.StartsWith(OutOfRangeFailure, StringComparison.Ordinal) ||
                    failure.StartsWith(DeadZoneFailure, StringComparison.Ordinal) ||
                    failure.StartsWith(SightLineFailure, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>Cells nearer than the spell's own minimum range. The caster's own cell is excluded:
        /// it is never a target, so marking it would only add noise.</summary>
        private static bool IsInsideSpellDeadZone(CombatState state, FireSpellDefinition spell, GridPosition position)
        {
            if (spell == null || spell.MinimumRange <= 0) return false;
            UnitState hero = state.GetUnit("hero");
            if (hero == null) return false;
            int distance = hero.Position.ManhattanDistance(position);
            return distance >= 1 && distance < spell.MinimumRange;
        }

        private Texture2D OverlayFor(BattlefieldCellMarker marker)
        {
            string id = BattlefieldMarkerLadder.TextureId(marker);
            return string.IsNullOrEmpty(id) ? null : assets.Overlay(id);
        }

        /// <summary>Cells nearer than the weapon's own minimum range: inside the declared reach but
        /// refused by the resolver, so they are marked undeliverable instead of left unmarked.</summary>
        private static bool IsInsideWeaponDeadZone(CombatState state, GridPosition position)
        {
            UnitState hero = state.GetUnit("hero");
            if (hero == null || hero.MainHand == null) return false;
            int distance = hero.Position.ManhattanDistance(position);
            return distance >= 1 && distance < Math.Max(1, hero.MainHand.MinimumRange);
        }

        private static int FireSpellSlot(string action)        {
            if (string.IsNullOrEmpty(action) || !action.StartsWith("技能", StringComparison.Ordinal) ||
                !int.TryParse(action.Substring(2), out int oneBased)) return -1;
            return oneBased >= 1 && oneBased <= OCC.Combat.Roguelite.RogueRuntimeConstants.SpellSlotCount
                ? oneBased - 1 : -1;
        }

        private static string BuildActionHover(CombatState state, CombatSelectionController selection, GridPosition position,
            UnitState unit, CombatTargetDamageForecast forecast, FireSpellDefinition spell, FireSpellPreview spellPreview,
            PressureReactionPreview pressureReaction, bool canMove, bool canAttack)
        {
            if (selection == null || state == null) return string.Empty;
            // While the right-click menu owns the input, the committed action is not what the player is
            // looking at, so the cell must not advertise it as "当前操作".
            if (selection.HasMenuPreview) return string.Empty;
            if (selection.Action == "移动" && canMove)
            {
                string result = "当前操作\n点击：移动至此";
                if (pressureReaction?.WillTrigger == true) result += "\n警戒：" + pressureReaction.Summary;
                return result;
            }
            if (selection.Action == "攻击" && canAttack && unit != null && !unit.IsHero)
            {
                string result = "当前操作\n点击：攻击「" + unit.DisplayName + "」";
                if (forecast != null) result += "\n预计：" + forecast.PlayerSummary.Replace("\n", "；");
                return result;
            }
            if (spell == null || spellPreview == null) return string.Empty;
            if (!spellPreview.CanCommit)
                return selection.HasPreviewPosition && selection.PreviewPosition == position
                    ? "当前操作\n不能施放：" + string.Join("；", spellPreview.Failures) : string.Empty;

            string effect = RogueliteSettlementPresentation.FireSpellPlayerSummary(spell).TrimEnd('。');
            string preview = "当前操作\n点击：施放「" + spell.DisplayName + "」"
                + "\n影响：" + spellPreview.Cells.Count + " 格"
                + (spellPreview.FriendlyFireRisk ? "，可能波及友军" : string.Empty)
                + "\n效果：" + effect;
            return preview;
        }

        public static string AppendTerrainHover(string unitHover, string terrainHover) =>
            string.IsNullOrWhiteSpace(terrainHover) ? unitHover : unitHover + "\n\n脚下地形\n" + terrainHover;

        public static string BuildSurfaceHover(FirstRegionLevelDefinition level, GridPosition position)
        {
            string floor = FloorKey(level, level?.Height ?? 0, position.X, position.Y);
            if (floor.Contains("_earth_")) return "夯土地坪，可正常通行。";
            if (floor.Contains("_road_")) return "学院道路，可正常通行。";
            if (floor.Contains("_ruin_")) return "受损地坪，可正常通行。";
            return "学院地坪，可正常通行。";
        }

        public static string BuildTerrainEffectHover(CombatState state, FireBattleState fireBattle, TileState tile,
            GridPosition position)
        {
            if (state == null || tile == null) return string.Empty;
            var effects = new List<string>();
            if (tile.IsWater)
                effects.Add("浅水使主角进入消耗 2 移动距离、寻迹兽消耗 1，并在进入时移除燃烧");
            if (fireBattle?.HasFireground(position) == true)
                effects.Add("燃烧地面会在进入或停留时触发火焰伤害");
            if (tile.SmokeExpiresAt > state.CurrentTime)
                effects.Add("烟幕可进入，但会截断双方穿过、射入或射出的远程攻击线，并在第 " + tile.SmokeExpiresAt + " 行动时消散");
            if (tile.IsScorched)
                effects.Add("灯藤焦痕仅作视觉记录，不造成伤害、遮挡或状态");
            if (tile.IsLoosePaper)
                effects.Add("散页使进入消耗 2 移动距离，被浅水打湿后暂时不可燃，被点燃即转为燃烧地格，并可被风逐格搬动");
            if (tile.HasTrace)
                effects.Add("痕迹记录经过的单位，供追踪类单位读取；被浅水、燃烧地格或强风作用时立即移除");
            if (tile.IsBindingMark)
                effects.Add("约束纹使进入者留在原格且本回合不能主动移动；被浅水、燃烧地格或烟尘覆盖即失效");
            return effects.Count == 0 ? string.Empty : string.Join("；", effects) + "。";
        }

        public static string BuildObjectHover(CombatState state, TileState tile, GridPosition position)
        {
            if (state == null || tile == null) return string.Empty;
            var objects = new List<string>();
            if (tile.IsLampVine)
                objects.Add("灯藤可进入，进入消耗 2 移动距离并阻挡双方视线，藤内只能攻击相邻目标，可作用物块的火焰会烧去一格");
            else if (tile.IsAetherCrystal)
                objects.Add("蓄能晶簇不可进入且不阻挡攻击线，耐久 " + tile.Durability + "，摧毁后对正交四格造成 8 点以太伤害并生成五格碎晶");
            else if (tile.IsCrystalShard)
                objects.Add("碎晶可进入，进入消耗 2 移动距离，不阻挡攻击线且不持续造成伤害");
            else if (tile.IsObjective)
            {
                bool protectedTarget = state.PressureTest?.HasProtection == true &&
                    state.PressureTest.ProtectedPosition == position;
                objects.Add(tile.IsDestroyed ? (protectedTarget ? "稳压器已损毁，保护目标失败" : "损毁导能柱已经失效") :
                    protectedTarget ? state.PressureTest.ProtectionSummary(state) + " 可通过拦截、切线、占位、束缚或推拉保护。" :
                    "导能柱耐久 " + tile.Durability + "，可被互动或指定术式影响");
            }
            else if (tile.IsPermanentWall)
                objects.Add("永久墙体阻挡移动与视线，不可破坏");
            else if (tile.IsStakedStructure)
                objects.Add(tile.IsDestroyed ? "标定结构残骸已失去阻挡和防护效果" :
                    "标定结构是现场夯筑的掩体，耐久 " + tile.Durability + "，阻挡移动与视线，与其正交相邻会在自身回合结束获得 4 护盾");
            else if (tile.Cover == CoverType.Light)
                objects.Add(tile.IsDestroyed ? "轻掩体残骸已失去防护效果，可正常通行" :
                    "轻掩体耐久 " + tile.Durability + "，肉鸽战斗中站立其上会在自身回合结束获得 2 护盾");
            else if (tile.Cover == CoverType.Heavy)
                objects.Add(tile.IsDestroyed ? "重掩体残骸已失去阻挡和防护效果" :
                    "重掩体耐久 " + tile.Durability + "，会阻挡移动与视线，肉鸽战斗中与其正交相邻会在自身回合结束获得 4 护盾");
            else if (tile.IsDecoy)
                objects.Add("诱导灯占据该格，耐久 " + tile.Durability + "；5 格内普通敌人会公开改为接近并破坏它，持续到主角下一回合开始");
            else if (tile.IsOverloadDevice)
                objects.Add(tile.IsDestroyed ? "过载装置已引爆，不再有威胁" :
                    "过载装置不可进入，耐久 " + tile.Durability + "，被摧毁时对正交四格结算 8 点以太伤害，敌我一致");
            else if (tile.IsCertifierStand)
                objects.Add(tile.IsDestroyed ? "检定台已损毁，不再提供读数" :
                    "检定台不可进入，耐久 " + tile.Durability + "，单位进入正交邻接时公开邻接单位的耐久与护盾读数，敌我同规则");
            else if (tile.IsWardGenerator)
                objects.Add(tile.IsDestroyed ? "护罩发生器已损毁，不再提供结构护盾" :
                    "护罩发生器不可进入，耐久 " + tile.Durability + "，自身回合结束时为正交相邻单位提供 4 点结构护盾，被摧毁即不再提供");
            else if (tile.IsTowerMechanism)
                objects.Add(tile.IsDestroyed ? "塔内机关已拆除，不再计入维护链" :
                    "塔内机关不可进入，耐久 " + tile.Durability + "，放行前不提供任何效果，放行后计入维护链，双方均可拆除");
            else if (tile.IsDevice)
                objects.Add(tile.IsDestroyed ? "损毁设备已经失效" :
                    "战场设备耐久 " + tile.Durability + "，可被互动、破坏或指定术式影响");
            if (state.Loot != null && state.Loot.Position == position)
                objects.Add(state.Loot.IsLooted ? "战利品箱已经清空" : "战利品箱可在相邻位置打开");
            else if (state.LootSource != null && state.LootSource.Position == position)
                objects.Add(state.LootSource.State == LootSearchState.Emptied ? "检修备件箱已经清空" :
                    "中央检修备件箱可在正交邻格消耗 1 AP 搜刮，固定藏有苗床回流芯");
            return objects.Count == 0 ? string.Empty : string.Join("；", objects) + "。";
        }

        private static string JoinHoverSections(params string[] sections) =>
            string.Join("\n", sections.Where(value => !string.IsNullOrWhiteSpace(value)));

        public static string FloorKey(FirstRegionLevelDefinition level, int mapHeight, int x, int y)
        {
            return AcademyBattlefieldLayoutCatalog.FloorAsset(level, x, y, out _);
        }

        public static float FloorRotationDegrees(FirstRegionLevelDefinition level, int x, int y)
        {
            AcademyBattlefieldLayoutCatalog.FloorAsset(level, x, y, out int quarterTurns);
            return -90f * quarterTurns;
        }

        public static Rect FloorUv(int x, int y)
        {
            const float slice = 1f / 3f;
            int column = ((x % 3) + 3) % 3;
            int row = ((y % 3) + 3) % 3;
            return new Rect(column * slice, row * slice, slice, slice);
        }

        private static string FixedEncounterEnemyRuleText(CombatState state, UnitState unit)
        {
            if (state == null || unit == null) return string.Empty;
            if (state.RainLanternCourt != null && unit.EnemyArchetypeId == "tether_hound")
                return "\n搜索规则：看见主角时追击并优先扑咬；失去视线后会前往灯藤入口，完整嗅探一回合，再沿外围顺时针搜索。";
            if (state.RainLanternCourt != null && unit.EnemyArchetypeId == "pyromancer")
                return "\n固定规则：能合法使用火矢时优先攻击；否则每回合最多一次，依次烧去 F3、F4、F5 的灯藤。";
            if (state.GreenhouseCollectionRoom != null && unit.EnemyArchetypeId == "sigil_mauler")
                return "\n固定规则：无法攻击主角时接近最近完整晶簇并公开贴晶校准；所贴晶簇被毁后正面追击。借晶强化量未冻结，当前按基础重击结算。";
            if (state.GreenhouseCollectionRoom != null && unit.EnemyArchetypeId == "raider")
                return "\n固定规则：相邻时优先钩刃牵制，否则限位钩刃；不相邻时从灯藤侧翼接近，藤内隐藏，能出藤贴邻时才显形。";
            if (state.PressureTest?.HasReaction == true && unit.Id == "enemy_0")
                return "\n警戒反应：主角移动结束进入 2–4 格正交射界时，向射线上首个单位造成 6 伤害并击退 1 格；相邻为死区，每个主角回合限 1 次。";
            if (state.PressureTest?.HasProtection == true && unit.Id == "enemy_0")
                return "\n目标优先级：公开接近并拆毁稳压器；被击倒、束缚、推离或接近格被占用时会改线。";
            return string.Empty;
        }
    }
}
